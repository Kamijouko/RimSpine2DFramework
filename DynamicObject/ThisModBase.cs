using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HugsLib;
using Verse;
using RimWorld;
using UnityEngine;

namespace DynamicObject
{
    public class ThisModBase : ModBase
    {
        public override string ModIdentifier { get; } = "DynamicObject.NazunaRei.kamijouko";

        public override void DefsLoaded()
        {
            base.DefsLoaded();
            ModStaticMethod.ThisMod = this;
            LoadAndResolveAllDynamicDefs();
            ResolveAllStoryTellerCameras();
            ModStaticMethod.AllLevelsLoaded = true;
        }


        public void LoadAndResolveAllDynamicDefs()
        {
            List<DynamicObjectDef> list = DefDatabase<DynamicObjectDef>.AllDefsListForReading;
            if (list.NullOrEmpty())
                return;
            AssetBundlesDef bundlesDef = AssetBundlesDefOf.Lib_AssetBundles;
            Dictionary<string, AssetBundle> bundles = new Dictionary<string, AssetBundle>();

            if (!bundlesDef.assetBundles.NullOrEmpty())
            {
                foreach (string bundleName in bundlesDef.assetBundles)
                {
                    if (!bundles.ContainsKey(bundleName))
                    {
                        AssetBundle ab = AssetBundle.LoadFromFile(Path.Combine(ModStaticMethod.RootDir, "Assets", bundleName));
                        bundles.Add(bundleName, ab);
                        Shader[] shaders = ab.LoadAllAssets<Shader>();
                        foreach (Shader s in shaders)
                        {
                            if (!ModDynamicObjectManager.spineShaderDatabase.ContainsKey(s.name))
                                ModDynamicObjectManager.spineShaderDatabase.Add(s.name, s);
                        }
                    }
                }
            }
            foreach (DynamicObjectDef def in list)
            {
                if (def.spine == null)
                    continue;
                TextAsset atlasTxt;
                TextAsset skeletonByte;
                Material[] materialPropertySource;
                AssetBundle ab;

                if (def.importMode == ImportMode.AssetBundle)
                {
                    if (!bundles.ContainsKey(def.spine.assetBundleName))
                    {
                        ab = AssetBundle.LoadFromFile(Path.Combine(ModStaticMethod.RootDir, "Assets", def.spine.assetBundleName));
                        bundles.Add(def.spine.assetBundleName, ab);
                    }
                    else
                        ab = bundles[def.spine.assetBundleName];
                    atlasTxt = ab.LoadAsset<TextAsset>(def.spine.atlasPath);
                    skeletonByte = ab.LoadAsset<TextAsset>(def.spine.skeletonPath);
                    materialPropertySource = ab.LoadAllAssets<Material>().Where(x => def.spine.materialNames.Contains(x.name)).ToArray();
                }
                else
                {
                    string txt = File.ReadAllText(Path.Combine(ModStaticMethod.RootDir, def.spine.atlasPath));
                    byte[] bytes = File.ReadAllBytes(Path.Combine(ModStaticMethod.RootDir, def.spine.skeletonPath));

                    atlasTxt = new TextAsset(txt);
                    skeletonByte = new TextAsset(Convert.ToBase64String(bytes));
                    materialPropertySource = def.spine.textures.Select(x => x.DynamicGraphic.MatSingle).ToArray();
                } 

                if (def.spine.ver == "3.5" && !ModDynamicObjectManager.spine35Database.ContainsKey(def.defName))
                {
                    Spine35.Unity.AtlasAsset runtimeAtlasAsset = Spine35.Unity.AtlasAsset.CreateRuntimeInstance(atlasTxt, materialPropertySource, true);
                    Spine35.Unity.SkeletonDataAsset runtimeSkeletonDataAsset = Spine35.Unity.SkeletonDataAsset.CreateRuntimeInstance(skeletonByte, runtimeAtlasAsset, true);
                    ModDynamicObjectManager.spine35Database.Add(def.defName, runtimeSkeletonDataAsset);
                }
                if (def.spine.ver == "3.8" && !ModDynamicObjectManager.spine38Database.ContainsKey(def.defName))
                {
                    Spine38.Unity.SpineAtlasAsset runtimeAtlasAsset = Spine38.Unity.SpineAtlasAsset.CreateRuntimeInstance(atlasTxt, materialPropertySource, true);
                    Spine38.Unity.SkeletonDataAsset runtimeSkeletonDataAsset = Spine38.Unity.SkeletonDataAsset.CreateRuntimeInstance(skeletonByte, runtimeAtlasAsset, true);
                    ModDynamicObjectManager.spine38Database.Add(def.defName, runtimeSkeletonDataAsset);
                }
                if (!bundles.NullOrEmpty())
                {
                    foreach (AssetBundle bundle in bundles.Values)
                    {
                        bundle.Unload(false);
                    }
                    bundles.Clear();
                }
            }
            
        }

        public void ResolveAllStoryTellerCameras()
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
                Camera cam = obj.AddComponent<Camera>();
                cam.fieldOfView = 40;
                cam.clearFlags = CameraClearFlags.Color;
                cam.backgroundColor = new Color(0f, 0f, 0f, 0f);
                cam.useOcclusionCulling = false;
                cam.renderingPath = RenderingPath.Forward;
                cam.nearClipPlane = 0.3f;
                cam.farClipPlane = 10f;
                cam.depth = Current.Camera.depth - 1;
                cam.targetTexture = new RenderTexture(580, 620, 24, RenderTextureFormat.ARGB32, 0);

                if (def.dynamicObject.spine.ver == "3.5" )
                {
                    if (!ModDynamicObjectManager.spine35Database.ContainsKey(def.dynamicObject.defName))
                        continue;
                    instance.ver = "3.5";
                    instance.spine35skeleton = Spine35.Unity.SkeletonAnimation.NewSkeletonAnimationGameObject(ModDynamicObjectManager.spine35Database[def.dynamicObject.defName]);
                    instance.spine35skeleton.transform.parent = instance.gameObject.transform;
                    instance.spine35skeleton.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
                    instance.spine35skeleton.transform.position = new Vector3(0f, -1.6f, 5f);
                    instance.spine35skeleton.skeleton.SetSkin(def.skin);
                    instance.spine35skeleton.AnimationState.SetAnimation(0, def.animationName, def.loop);
                    instance.spine35skeleton.Initialize(false);
                }
                else
                {
                    if (!ModDynamicObjectManager.spine38Database.ContainsKey(def.dynamicObject.defName))
                        continue;
                    instance.ver = "3.8";
                    instance.spine38skeleton = Spine38.Unity.SkeletonAnimation.NewSkeletonAnimationGameObject(ModDynamicObjectManager.spine38Database[def.dynamicObject.defName]);
                    instance.spine38skeleton.transform.parent = instance.gameObject.transform;
                    instance.spine38skeleton.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
                    instance.spine38skeleton.transform.position = new Vector3(0f, -1.6f, 5f);
                    instance.spine38skeleton.skeleton.SetSkin(def.skin);
                    instance.spine38skeleton.AnimationState.SetAnimation(0, def.animationName, def.loop);
                    instance.spine38skeleton.Initialize(false);
                }
                UnityEngine.Object.DontDestroyOnLoad(obj);
                obj.SetActive(false);
                ModDynamicObjectManager.DynamicStoryTellerDatabase.Add(def.defName, obj);
            }
        }
    }
}
