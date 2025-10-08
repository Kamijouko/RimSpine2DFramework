using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Xml;
using HarmonyLib;
using RimWorld;
using Verse;

namespace RimSpine2DFramework
{
    /// <summary>
    /// Lightweight in-memory replacement for RimWorld's global DefDatabase.
    /// Stores defs by (base) type and name, without touching the vanilla static registries.
    /// </summary>
    public sealed class EmbeddedDefDatabase
    {
        private readonly Dictionary<Type, Dictionary<string, Def>> defsByType = new Dictionary<Type, Dictionary<string, Def>>();

        private readonly List<Def> orderedDefs = new List<Def>();

        private static readonly StringComparer NameComparer = StringComparer.OrdinalIgnoreCase;

        /// <summary>
        /// Registers the def in the embedded database (no interaction with vanilla DefDatabase).
        /// </summary>
        public void Register(Def def)
        {
            if (def == null)
            {
                throw new ArgumentNullException(nameof(def));
            }

            if (def.defName.NullOrEmpty())
            {
                throw new ArgumentException("Cannot register an unnamed def", nameof(def));
            }

            foreach (var type in EnumerateDefTypes(def.GetType()))
            {
                if (!defsByType.TryGetValue(type, out var map))
                {
                    map = new Dictionary<string, Def>(NameComparer);
                    defsByType[type] = map;
                }

                map[def.defName] = def;
            }

            orderedDefs.Add(def);
        }

        /// <summary>
        /// Retrieves a def by name. Returns false if not found.
        /// </summary>
        public bool TryGet<TDef>(string defName, out TDef def) where TDef : Def
        {
            def = null;
            if (defName.NullOrEmpty())
            {
                return false;
            }

            if (defsByType.TryGetValue(typeof(TDef), out var map) && map.TryGetValue(defName, out var value))
            {
                def = value as TDef;
                return def != null;
            }

            return false;
        }

        /// <summary>
        /// Enumerates all defs (in registration order).
        /// </summary>
        public IEnumerable<Def> AllDefs() => orderedDefs;

        private static IEnumerable<Type> EnumerateDefTypes(Type type)
        {
            while (type != null && typeof(Def).IsAssignableFrom(type))
            {
                yield return type;
                type = type.BaseType;
            }
        }
    }

    /// <summary>
    /// Utility that mimics the vanilla loading pipeline for XML defs while keeping data inside an EmbeddedDefDatabase.
    /// </summary>
    public sealed class EmbeddedDefLoader
    {
        private readonly EmbeddedDefDatabase database;

        private readonly List<Def> localDefs = new List<Def>();
        private readonly List<Def> defsPushedToGlobal = new List<Def>();

        private static readonly MethodInfo ResolveCrossRefsMethod = typeof(DirectXmlCrossRefLoader).GetMethod(
            nameof(DirectXmlCrossRefLoader.ResolveAllWantedCrossReferences), BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

        private static readonly MethodInfo ResetCrossRefsMethod = typeof(DirectXmlCrossRefLoader).GetMethod(
            "Reset", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

        private static readonly MethodInfo GiveShortHashMethod = AccessTools.Method("Verse.ShortHashGiver:GiveShortHash")
            ?? AccessTools.Method("RimWorld.ShortHashGiver:GiveShortHash");

        private static readonly FieldInfo ShortHashField = AccessTools.Field(typeof(Def), "shortHash");

        private static readonly ConcurrentDictionary<Type, HashSet<ushort>> ReservedShortHashes = new ConcurrentDictionary<Type, HashSet<ushort>>();
        private static readonly ConcurrentDictionary<Type, byte> ShortHashSnapshotFailures = new ConcurrentDictionary<Type, byte>();
        private static readonly ConcurrentDictionary<string, byte> GlobalDatabaseInvocationFailures = new ConcurrentDictionary<string, byte>();

        private static bool shortHashWarned;

        public EmbeddedDefLoader(EmbeddedDefDatabase database)
        {
            this.database = database ?? throw new ArgumentNullException(nameof(database));
        }

        /// <summary>
        /// Loads all def nodes that exist under the given xmlRoot.
        /// The method does not resolve cross references; call <see cref="FinalizeLoading"/> afterwards.
        /// </summary>
        public void LoadFromXmlDocument(XmlDocument document, string assetName, ModContentPack modContentPack, bool mirrorIntoGlobalDatabase = false)
        {
            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            var root = document.DocumentElement;
            if (root == null)
            {
                return;
            }

            foreach (XmlNode node in root.ChildNodes)
            {
                if (!(node is XmlElement element))
                {
                    continue;
                }

                LoadableXmlAsset asset = new LoadableXmlAsset(assetName ?? "Embedded", element.OuterXml);
                Def def = DirectXmlLoader.DefFromNode(element, asset);
                if (def == null)
                {
                    continue;
                }

                PrepareDef(def, assetName, modContentPack);

                database.Register(def);
                localDefs.Add(def);

                if (mirrorIntoGlobalDatabase)
                {
                    if (TryAddToGlobalDatabase(def))
                    {
                        defsPushedToGlobal.Add(def);
                    }
                }
            }
        }

        /// <summary>
        /// Resolves cross references and calls ResolveReferences on all loaded defs.
        /// </summary>
        public void FinalizeLoading()
        {
            ResolveCrossReferences();

            foreach (Def def in localDefs)
            {
                try
                {
                    def.ResolveReferences();
                }
                catch (Exception ex)
                {
                    Log.Error($"[RimSpine2DFramework] Failed to resolve references for def '{def.defName}': {ex}");
                }
            }
        }

        /// <summary>
        /// Removes any defs that have been temporarily injected into the vanilla DefDatabase.
        /// </summary>
        public void RevertGlobalRegistrations()
        {
            foreach (var def in defsPushedToGlobal)
            {
                TryRemoveFromGlobalDatabase(def);
            }
            defsPushedToGlobal.Clear();
        }

        private static void PrepareDef(Def def, string assetName, ModContentPack modContentPack)
        {
            def.fileName = assetName ?? "Embedded";
            def.modContentPack = modContentPack;
            def.PostLoad();
            AssignShortHash(def);
        }

        private static void AssignShortHash(Def def)
        {
            if (def == null)
            {
                return;
            }

            bool assigned = TryAssignFallbackShortHash(def);

            if (!assigned && GiveShortHashMethod != null && TryInvokeShortHash(def))
            {
                assigned = true;
            }

            if (!assigned && !shortHashWarned)
            {
                shortHashWarned = true;
                Log.Warning("[RimSpine2DFramework] Unable to assign short hashes to embedded defs; duplicates may break cross references.");
            }
        }

        private static bool TryAddToGlobalDatabase(Def def)
        {
            try
            {
                Type defType = def.GetType();
                Type genericDatabaseType = typeof(DefDatabase<>).MakeGenericType(defType);
                MethodInfo addMethod = AccessTools.Method(genericDatabaseType, "Add", new[] { defType })
                    ?? AccessTools.Method(genericDatabaseType, "Add", new[] { typeof(Def) });
                if (addMethod == null)
                {
                    addMethod = FindSingleParameterMethod(genericDatabaseType, "Add", defType);
                }

                if (addMethod != null)
                {
                    object[] args = BuildInvocationArguments(addMethod.GetParameters(), def, out bool assignedDef);
                    if (assignedDef)
                    {
                        addMethod.Invoke(null, args);
                        return true;
                    }
                    else
                    {
                        if (GlobalDatabaseInvocationFailures.TryAdd($"{genericDatabaseType.FullName}.Add", 0))
                        {
                            Log.Warning($"[RimSpine2DFramework] Unable to find a parameter slot for def '{def.defName}' when invoking '{genericDatabaseType.FullName}.Add'.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warning($"[RimSpine2DFramework] Failed to mirror def '{def.defName}' into global database: {ex.Message}");
                return false;
            }
        }

        private static void TryRemoveFromGlobalDatabase(Def def)
        {
            try
            {
                Type defType = def.GetType();
                Type genericDatabaseType = typeof(DefDatabase<>).MakeGenericType(defType);
                MethodInfo removeMethod = AccessTools.Method(genericDatabaseType, "Remove", new[] { defType })
                    ?? AccessTools.Method(genericDatabaseType, "Remove", new[] { typeof(Def) });
                if (removeMethod == null)
                {
                    removeMethod = FindSingleParameterMethod(genericDatabaseType, "Remove", defType);
                }

                if (removeMethod != null)
                {
                    object[] args = BuildInvocationArguments(removeMethod.GetParameters(), def, out bool assignedDef);
                    if (assignedDef)
                    {
                        removeMethod.Invoke(null, args);
                    }
                    else if (GlobalDatabaseInvocationFailures.TryAdd($"{genericDatabaseType.FullName}.Remove", 0))
                    {
                        Log.Warning($"[RimSpine2DFramework] Unable to find a parameter slot for def '{def.defName}' when invoking '{genericDatabaseType.FullName}.Remove'.");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warning($"[RimSpine2DFramework] Failed to remove mirrored def '{def.defName}' from global database: {ex.Message}");
            }
        }

        private static void ResolveCrossReferences()
        {
            InvokeStaticWithDefaults(ResolveCrossRefsMethod, errorLog: true);
            InvokeStaticWithDefaults(ResetCrossRefsMethod, errorLog: false);
        }

        private static bool TryInvokeShortHash(Def def)
        {
            try
            {
                if (GiveShortHashMethod == null)
                {
                    return false;
                }

                ParameterInfo[] parameters = GiveShortHashMethod.GetParameters();
                object[] args = new object[parameters.Length];
                bool assignedDef = false;

                for (int i = 0; i < parameters.Length; i++)
                {
                    ParameterInfo parameter = parameters[i];
                    Type parameterType = parameter.ParameterType;

                    if (!assignedDef && parameterType.IsAssignableFrom(def.GetType()))
                    {
                        args[i] = def;
                        assignedDef = true;
                        continue;
                    }

                    if (parameterType == typeof(Def))
                    {
                        args[i] = def;
                        assignedDef = true;
                        continue;
                    }

                    if (parameterType == typeof(Type))
                    {
                        args[i] = def.GetType();
                        continue;
                    }

                    if (parameter.HasDefaultValue)
                    {
                        args[i] = parameter.DefaultValue;
                        continue;
                    }

                    args[i] = GetDefault(parameterType);
                }

                if (!assignedDef)
                {
                    return false;
                }

                GiveShortHashMethod.Invoke(null, args);
                if (ShortHashField != null && (ushort)(ShortHashField.GetValue(def) ?? 0) == 0)
                {
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                string detail = ex.InnerException != null
                    ? $"{ex.InnerException.GetType().Name}: {ex.InnerException.Message}"
                    : ex.Message;
                Log.Warning($"[RimSpine2DFramework] Failed to invoke ShortHashGiver.GiveShortHash for '{def.defName}': {detail}");
                return false;
            }
        }

        private static bool TryAssignFallbackShortHash(Def def)
        {
            if (ShortHashField == null)
            {
                return false;
            }

            try
            {
                ushort current = (ushort)(ShortHashField.GetValue(def) ?? 0);
                if (current != 0)
                {
                    return true;
                }

                int stableHash = GenText.StableStringHash(def.defName);
                if (stableHash == int.MinValue)
                {
                    stableHash = 0;
                }
                else if (stableHash < 0)
                {
                    stableHash = -stableHash;
                }

                HashSet<ushort> reserved = ReservedShortHashes.GetOrAdd(def.GetType(), CreateReservedHashSet);

                ushort candidate = NormalizeHash(stableHash);
                int safety = 0;
                while (candidate == 0 || reserved.Contains(candidate))
                {
                    candidate = NormalizeHash(++stableHash);
                    if (++safety > ushort.MaxValue)
                    {
                        return false;
                    }
                }

                reserved.Add(candidate);
                ShortHashField.SetValue(def, candidate);
                return true;
            }
            catch (Exception ex)
            {
                Log.Warning($"[RimSpine2DFramework] Failed to assign short hash fallback for '{def.defName}': {ex.Message}");
                return false;
            }
        }

        private static ushort NormalizeHash(int value)
        {
            uint normalized = (uint)value % ushort.MaxValue;
            ushort candidate = (ushort)normalized;
            if (candidate == 0)
            {
                candidate = 1;
            }

            return candidate;
        }

        private static HashSet<ushort> CreateReservedHashSet(Type defType)
        {
            var set = new HashSet<ushort>();

            if (ShortHashField == null)
            {
                return set;
            }

            try
            {
                Type genericDatabaseType = typeof(DefDatabase<>).MakeGenericType(defType);
                PropertyInfo allDefsProperty = genericDatabaseType.GetProperty("AllDefsListForReading", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                if (allDefsProperty != null)
                {
                    if (allDefsProperty.GetValue(null) is IEnumerable enumerable)
                    {
                        foreach (object entry in enumerable)
                        {
                            if (entry is Def existingDef)
                            {
                                ushort value = (ushort)(ShortHashField.GetValue(existingDef) ?? 0);
                                if (value != 0)
                                {
                                    set.Add(value);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if (ShortHashSnapshotFailures.TryAdd(defType, 0))
                {
                    Log.Warning($"[RimSpine2DFramework] Failed to snapshot existing short hashes for '{defType.FullName}': {ex.Message}");
                }
            }

            return set;
        }

        private static MethodInfo FindSingleParameterMethod(Type type, string methodName, Type parameterType)
        {
            foreach (MethodInfo method in type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (method.Name != methodName)
                {
                    continue;
                }

                ParameterInfo[] parameters = method.GetParameters();
                if (parameters.Length == 1 && parameters[0].ParameterType.IsAssignableFrom(parameterType))
                {
                    return method;
                }
            }

            return null;
        }

        private static object[] BuildInvocationArguments(ParameterInfo[] parameters, Def def, out bool assignedDef)
        {
            object[] args = new object[parameters.Length];
            assignedDef = false;

            for (int i = 0; i < parameters.Length; i++)
            {
                ParameterInfo parameter = parameters[i];
                Type parameterType = parameter.ParameterType;

                if (!assignedDef && parameterType.IsAssignableFrom(def.GetType()))
                {
                    args[i] = def;
                    assignedDef = true;
                    continue;
                }

                if (parameterType == typeof(Def))
                {
                    args[i] = def;
                    assignedDef = true;
                    continue;
                }

                if (parameterType == typeof(Type))
                {
                    args[i] = def.GetType();
                    continue;
                }

                args[i] = parameter.HasDefaultValue ? parameter.DefaultValue : GetDefault(parameterType);
            }

            return args;
        }

        private static void InvokeStaticWithDefaults(MethodInfo method, bool errorLog)
        {
            if (method == null)
            {
                return;
            }

            try
            {
                ParameterInfo[] parameters = method.GetParameters();
                object[] args = new object[parameters.Length];
                for (int i = 0; i < parameters.Length; i++)
                {
                    ParameterInfo parameter = parameters[i];
                    args[i] = parameter.HasDefaultValue ? parameter.DefaultValue : GetDefault(parameter.ParameterType);
                }

                method.Invoke(null, args);
            }
            catch (Exception ex)
            {
                string message = errorLog
                    ? $"[RimSpine2DFramework] Error resolving cross references for embedded defs: {ex}"
                    : $"[RimSpine2DFramework] Failed to reset cross reference loader: {ex.Message}";
                if (errorLog)
                {
                    Log.Error(message);
                }
                else
                {
                    Log.Warning(message);
                }
            }
        }

        private static object GetDefault(Type type)
        {
            return type.IsValueType ? Activator.CreateInstance(type) : null;
        }
    }
}
