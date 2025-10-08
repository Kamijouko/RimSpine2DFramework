using System;
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
            nameof(DirectXmlCrossRefLoader.ResolveAllWantedCrossReferences), BindingFlags.Static | BindingFlags.Public);

        private static readonly MethodInfo ResetCrossRefsMethod = typeof(DirectXmlCrossRefLoader).GetMethod(
            "Reset", BindingFlags.Static | BindingFlags.Public);

        private static readonly MethodInfo GiveShortHashMethod = AccessTools.Method("Verse.ShortHashGiver:GiveShortHash")
            ?? AccessTools.Method("RimWorld.ShortHashGiver:GiveShortHash");

        private static readonly FieldInfo ShortHashField = AccessTools.Field(typeof(Def), "shortHash");

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

            if (GiveShortHashMethod != null)
            {
                try
                {
                    GiveShortHashMethod.Invoke(null, new object[] { def, def.GetType() });
                    return;
                }
                catch (Exception ex)
                {
                    Log.Warning($"[RimSpine2DFramework] Failed to invoke ShortHashGiver.GiveShortHash for '{def.defName}': {ex.Message}");
                }
            }

            if (ShortHashField != null)
            {
                try
                {
                    int stableHash = GenText.StableStringHash(def.defName);
                    if (stableHash < 0)
                    {
                        stableHash = -stableHash;
                    }

                    ushort hashValue = (ushort)(stableHash % ushort.MaxValue);
                    if (hashValue == 0)
                    {
                        hashValue = 1;
                    }

                    ShortHashField.SetValue(def, hashValue);
                    return;
                }
                catch (Exception ex)
                {
                    Log.Warning($"[RimSpine2DFramework] Failed to assign short hash fallback for '{def.defName}': {ex.Message}");
                }
            }

            if (!shortHashWarned)
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
                MethodInfo addMethod = genericDatabaseType.GetMethod("Add", BindingFlags.Static | BindingFlags.Public);
                addMethod?.Invoke(null, new object[] { def });
                return addMethod != null;
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
                MethodInfo removeMethod = genericDatabaseType.GetMethod("Remove", BindingFlags.Static | BindingFlags.Public);
                removeMethod?.Invoke(null, new object[] { def });
            }
            catch (Exception ex)
            {
                Log.Warning($"[RimSpine2DFramework] Failed to remove mirrored def '{def.defName}' from global database: {ex.Message}");
            }
        }

        private static void ResolveCrossReferences()
        {
            if (ResolveCrossRefsMethod != null)
            {
                try
                {
                    ResolveCrossRefsMethod.Invoke(null, Array.Empty<object>());
                }
                catch (Exception ex)
                {
                    Log.Error($"[RimSpine2DFramework] Error resolving cross references for embedded defs: {ex}");
                }
            }

            if (ResetCrossRefsMethod != null)
            {
                try
                {
                    ResetCrossRefsMethod.Invoke(null, Array.Empty<object>());
                }
                catch (Exception ex)
                {
                    Log.Warning($"[RimSpine2DFramework] Failed to reset cross reference loader: {ex.Message}");
                }
            }
        }
    }
}
