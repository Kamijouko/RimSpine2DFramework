using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;
using UnityEngine;
using System.Threading;
using HarmonyLib;
using System.Collections;
using System.Reflection;

namespace RimSpine2DFramework
{

    [StaticConstructorOnStartup]
    internal static class StaticInitializer
    {
        static StaticInitializer()
        {
            ThisModBase.Instance.LateInitialize();
        }
    }

    public class ThisModBase : Mod
    {
        //public override string ModIdentifier { get; } = "RimSpine2DFramework.NazunaRei.kamijouko";

        public ThisModBase(ModContentPack content) : base(content) 
        {
            Instance = this;
            ModStaticMethod.ThisMod = this;
            harmonyInstance = new Harmony("RimSpine2DFramework.NazunaRei.kamijouko");
            harmonyInstance.PatchAll(Assembly.GetExecutingAssembly());
            Log.Message("[RimSpine2DFramework] PatchAll done.");
        }

        public static ThisModBase Instance;
        public Harmony harmonyInstance;


        internal void LateInitialize()
        {
            try
            {
                LongEventHandler.QueueLongEvent(LoadInitialize, "resolving all dynamic defs", false, null);
            }
            catch (Exception e)
            {
                LogSimple.Message("An exception occurred during late initialization: " + e);
            }
        }

        internal static void LoadInitialize()
        {
            if (!ModStaticMethod.AllLevelsLoaded)
            {
                LoadAndResolveAllDynamicDefs();
                ResolveAllStoryTellerCameras();
                ModStaticMethod.message = "loaded";
                ModStaticMethod.AllLevelsLoaded = true;
                //Log.Warning(ModStaticMethod.message);
            }
        }

        public static void LoadAndResolveAllDynamicDefs()
        {
            List<DynamicObjectDef> list = DefDatabase<DynamicObjectDef>.AllDefsListForReading;
            if (list.NullOrEmpty())
                return;

            List<ModContentPack> mods = LoadedModManager.RunningModsListForReading;
            string[] folderAbsDir = mods.Select(x => Path.Combine(x.RootDir, "Spines")).Where(x => Directory.Exists(x)).ToArray();
            List<AssetBundle> loadedAllAssetBundle = mods.SelectMany(x => x.assetBundles.loadedAssetBundles).ToList();
            string[] assetBundleAbsDir = mods.Select(x => Path.Combine(x.RootDir, "AssetBundles")).Where(x => Directory.Exists(x)).ToArray();

            foreach (DynamicObjectDef def in list)
            {
                if (def.spine == null)
                    continue;
                if (def.spine.ver == null)
                    def.spine.ver = "3.8";
                TextAsset atlasAsset;
                TextAsset skeletonAsset;
                byte[] skeletonBytes = null;
                Material[] materials = null;
                Texture2D[] textures = null;
                Shader shader = ShaderTypeDefOf.Cutout.Shader;
                AssetBundle ab;

                if (def.importMode == ImportMode.AssetBundle)
                {
                    if (!loadedAllAssetBundle.Exists(x => x.name == def.spine.assetBundleName))
                    {
                        string abPath = assetBundleAbsDir.FirstOrDefault(x => File.Exists(Path.Combine(x, def.spine.assetBundleName)));
                        if (abPath == null)
                            continue;
                        ab = AssetBundle.LoadFromFile(Path.Combine(abPath, def.spine.assetBundleName));
                    }
                    else
                        ab = loadedAllAssetBundle.First(x => x.name == def.spine.assetBundleName);

                    atlasAsset = ab.LoadAsset<TextAsset>(def.spine.atlasPath);
                    skeletonAsset = ab.LoadAsset<TextAsset>(def.spine.skeletonPath);

                    materials = ab.LoadAllAssets<Material>();
                    if (!materials.NullOrEmpty())
                        materials = materials.Where(x => def.spine.materialNames.Contains(x.name)).ToArray();
                    //Log.Warning(materials.Length.ToString());


                }
                else
                {
                    string txtPath = folderAbsDir.FirstOrDefault(x => File.Exists(Path.Combine(x, def.spine.atlasPath)));
                    string jsonPath = folderAbsDir.FirstOrDefault(x => File.Exists(Path.Combine(x, def.spine.skeletonPath)));
                    if (txtPath == null || jsonPath == null)
                        continue;

                    string atlasFullPath = Path.Combine(txtPath, def.spine.atlasPath);
                    string skeletonFullPath = Path.Combine(jsonPath, def.spine.skeletonPath);

                    string txt = File.ReadAllText(atlasFullPath);
                    atlasAsset = new TextAsset(txt);
                    atlasAsset.name = Path.GetFileName(def.spine.atlasPath);
                    List<string> atlasPageNames = ExtractAtlasPageNames(txt);

                    string skeletonExtension = Path.GetExtension(def.spine.skeletonPath);
                    bool isBinarySkeleton = skeletonExtension.Equals(".skel", StringComparison.OrdinalIgnoreCase)
                        || def.spine.skeletonPath.EndsWith(".skel", StringComparison.OrdinalIgnoreCase)
                        || def.spine.skeletonPath.EndsWith(".skel.bytes", StringComparison.OrdinalIgnoreCase);

                    if (isBinarySkeleton)
                    {
                        try
                        {
                            skeletonBytes = File.ReadAllBytes(skeletonFullPath);
                            skeletonAsset = new TextAsset(string.Empty);
                        }
                        catch (Exception ex)
                        {
                            Log.Error($"[RimSpine2DFramework] Failed to load binary skeleton '{def.spine.skeletonPath}': {ex}");
                            continue;
                        }
                    }
                    else
                    {
                        string json = File.ReadAllText(skeletonFullPath);
                        skeletonAsset = new TextAsset(json);
                    }

                    skeletonAsset.name = Path.GetFileName(def.spine.skeletonPath);
                    if (def.spine.shaderName == "Spine-Skeleton.shader")
                    {
                        string spineAB;
                        switch (def.spine.ver)
                        {
                            case "3.5": spineAB = "spine35"; break;
                            case "3.8": spineAB = "spine38"; break;
                            case "4.0": spineAB = "spine40"; break;
                            case "4.1": spineAB = "spine41"; break;
                            default: spineAB = "spine38"; break;
                        }
                        AssetBundle bund = loadedAllAssetBundle.FirstOrDefault(x => x.name == spineAB);
                        Shader shade = bund.LoadAsset<Shader>(def.spine.shaderName);
                        if (shade != null)
                            shader = shade;
                    }
                    else
                    {
                        foreach (AssetBundle bund in loadedAllAssetBundle)
                        {
                            Shader shade = bund.LoadAsset<Shader>(def.spine.shaderName);
                            if (shade == null)
                                continue;
                            shader = shade;
                            break;
                        }
                    }
                    //Log.Warning(shader.name);
                    List<Texture2D> loadedTextures = new List<Texture2D>(def.spine.textures.Count);
                    Dictionary<string, Texture2D> textureLookup = new Dictionary<string, Texture2D>(StringComparer.OrdinalIgnoreCase);
                    for (int i = 0; i < def.spine.textures.Count; i++)
                    {
                        string texturePath = def.spine.textures[i].texPath;
                        Texture2D texture = ContentFinder<Texture2D>.Get(texturePath);
                        if (texture == null)
                        {
                            Log.Error($"[RimSpine2DFramework] Failed to load texture '{texturePath}' for def '{def.defName}'.");
                            loadedTextures.Add(null);
                            continue;
                        }

                        string normalizedName = NormalizeTextureKey(texturePath);
                        if (string.IsNullOrEmpty(normalizedName))
                        {
                            Log.Error($"[RimSpine2DFramework] Unable to determine texture name for '{texturePath}' while loading '{def.defName}'.");
                        }
                        else
                        {
                            texture.name = normalizedName;
                            if (textureLookup.ContainsKey(normalizedName))
                            {
                                Log.Warning($"[RimSpine2DFramework] Duplicate texture name '{normalizedName}' detected while loading '{def.defName}'. Using the last occurrence.");
                                textureLookup[normalizedName] = texture;
                            }
                            else
                            {
                                textureLookup.Add(normalizedName, texture);
                            }
                        }

                        loadedTextures.Add(texture);
                    }

                    if (atlasPageNames.Count > 0)
                    {
                        Texture2D[] orderedTextures = new Texture2D[atlasPageNames.Count];
                        bool missingTexture = false;
                        HashSet<string> normalizedPageSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                        for (int i = 0; i < atlasPageNames.Count; i++)
                        {
                            string pageName = atlasPageNames[i];
                            string normalizedPageName = NormalizeTextureKey(pageName);
                            if (string.IsNullOrEmpty(normalizedPageName))
                            {
                                Log.Error($"[RimSpine2DFramework] Unable to normalize atlas page name '{pageName}' from '{def.spine.atlasPath}' while loading '{def.defName}'.");
                                missingTexture = true;
                                continue;
                            }

                            normalizedPageSet.Add(normalizedPageName);

                            if (!textureLookup.TryGetValue(normalizedPageName, out Texture2D matchedTexture) || matchedTexture == null)
                            {
                                Log.Error($"[RimSpine2DFramework] Texture matching atlas page '{pageName}' (normalized '{normalizedPageName}') was not found for '{def.defName}'.");
                                missingTexture = true;
                                continue;
                            }

                            matchedTexture.name = normalizedPageName;
                            orderedTextures[i] = matchedTexture;
                        }

                        foreach (KeyValuePair<string, Texture2D> kvp in textureLookup)
                        {
                            if (!normalizedPageSet.Contains(kvp.Key))
                            {
                                Log.Warning($"[RimSpine2DFramework] Texture '{kvp.Key}' loaded for '{def.defName}' is not referenced by atlas '{def.spine.atlasPath}'.");
                            }
                        }

                        if (missingTexture || orderedTextures.Any(t => t == null))
                        {
                            Log.Error($"[RimSpine2DFramework] Failed to build a complete texture set for '{def.defName}'. Skipping definition.");
                            continue;
                        }

                        textures = orderedTextures;
                    }
                    else
                    {
                        textures = loadedTextures.ToArray();
                        if (textures.Any(t => t == null))
                        {
                            Log.Error($"[RimSpine2DFramework] Some textures failed to load for '{def.defName}' and atlas order could not be determined. Skipping definition.");
                            continue;
                        }
                    }
                    //Log.Warning(textures.Length.ToString());
                }

                SpineTextAssetData data = new SpineTextAssetData(atlasAsset, skeletonAsset, materials, textures, shader, skeletonBytes);
                if (def.spine.ver == "3.5" && !ModDynamicObjectManager.spine35Database.ContainsKey(def.defName))
                {
                    ModDynamicObjectManager.spine35Database.Add(def.defName, data);
                }
                else if (def.spine.ver == "3.8" && !ModDynamicObjectManager.spine38Database.ContainsKey(def.defName))
                {
                    ModDynamicObjectManager.spine38Database.Add(def.defName, data);
                }
                else if (def.spine.ver == "4.0" && !ModDynamicObjectManager.spine40Database.ContainsKey(def.defName))
                {
                    ModDynamicObjectManager.spine40Database.Add(def.defName, data);
                }
                else if (!ModDynamicObjectManager.spine41Database.ContainsKey(def.defName))
                {
                    ModDynamicObjectManager.spine41Database.Add(def.defName, data);
                }
            }
            DynamicPawnStateRegistry.ReloadDefinitions();
        }

        public static void ResolveAllStoryTellerCameras()
        {
            List<DynamicStoryTellerDef> list = DefDatabase<DynamicStoryTellerDef>.AllDefsListForReading;
            if (list.NullOrEmpty())
                return;

            foreach (DynamicStoryTellerDef def in list)
            {
                if (def.dynamicObject == null || ModDynamicObjectManager.DynamicStoryTellerDatabase.ContainsKey(def.defName))
                    continue;
                GameObject obj = new GameObject(def.defName);
                DynamicObjectInstance instance = obj.AddComponent<DynamicObjectInstance>();
                if (def.dynamicObject.spine.ver == "3.5")
                {
                    if (!ModDynamicObjectManager.spine35Database.ContainsKey(def.dynamicObject.defName))
                        continue;
                    instance.ver = "3.5";
                }
                else if (def.dynamicObject.spine.ver == "3.8")
                {
                    if (!ModDynamicObjectManager.spine38Database.ContainsKey(def.dynamicObject.defName))
                        continue;
                    instance.ver = "3.8";
                }
                else if (def.dynamicObject.spine.ver == "4.0")
                {
                    if (!ModDynamicObjectManager.spine40Database.ContainsKey(def.dynamicObject.defName))
                        continue;
                    instance.ver = "4.0";
                }
                else
                {
                    if (!ModDynamicObjectManager.spine41Database.ContainsKey(def.dynamicObject.defName))
                        continue;
                    instance.ver = "4.1";
                }
                instance.key = def.dynamicObject;
                instance.def = def;
                Camera cam = obj.AddComponent<Camera>();
                cam.fieldOfView = 40;
                cam.clearFlags = CameraClearFlags.Color;
                cam.backgroundColor = new Color(0f, 0f, 0f, 0f);
                cam.useOcclusionCulling = false;
                cam.renderingPath = RenderingPath.Forward;
                cam.nearClipPlane = 0.3f;
                cam.farClipPlane = 10f;
                cam.depth = Current.Camera.depth - 1;
                cam.targetTexture = new RenderTexture((int)def.windowScale.x, (int)def.windowScale.y, 24, RenderTextureFormat.ARGB32, 0);
                UnityEngine.Object.DontDestroyOnLoad(obj);
                obj.SetActive(false);
                ModDynamicObjectManager.DynamicStoryTellerDatabase.Add(def.defName, obj);
            }
        }

        private static string NormalizeTextureKey(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return string.Empty;

            string normalized = Path.GetFileNameWithoutExtension(name);
            if (string.IsNullOrWhiteSpace(normalized))
                normalized = Path.GetFileName(name);

            return string.IsNullOrWhiteSpace(normalized) ? string.Empty : normalized.Trim();
        }

        private static List<string> ExtractAtlasPageNames(string atlasText)
        {
            List<string> pageNames = new List<string>();
            if (string.IsNullOrEmpty(atlasText))
                return pageNames;

            string[] lines = atlasText.Split(new[] { '\r', '\n' }, StringSplitOptions.None);
            for (int i = 0; i < lines.Length; i++)
            {
                string current = lines[i].Trim();
                if (string.IsNullOrEmpty(current) || current.Contains(":"))
                    continue;

                string nextNonEmpty = string.Empty;
                for (int j = i + 1; j < lines.Length; j++)
                {
                    string candidate = lines[j].Trim();
                    if (string.IsNullOrEmpty(candidate))
                        continue;

                    nextNonEmpty = candidate;
                    break;
                }

                if (string.IsNullOrEmpty(nextNonEmpty))
                    continue;

                if (IsAtlasPagePropertyLine(nextNonEmpty))
                    pageNames.Add(current);
            }

            return pageNames;
        }

        private static bool IsAtlasPagePropertyLine(string line)
        {
            if (string.IsNullOrEmpty(line))
                return false;

            return line.StartsWith("size:", StringComparison.OrdinalIgnoreCase)
                || line.StartsWith("format:", StringComparison.OrdinalIgnoreCase)
                || line.StartsWith("filter:", StringComparison.OrdinalIgnoreCase)
                || line.StartsWith("repeat:", StringComparison.OrdinalIgnoreCase);
        }
    }
}
