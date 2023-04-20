using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;
using UnityEngine;
using System.IO;

namespace DynamicObject
{
    public class DynamicObjectInstance : MonoBehaviour
    {
        public Spine35.Unity.SkeletonAnimation spine35skeleton;

        public Spine38.Unity.SkeletonAnimation spine38skeleton;

        public string ver = "3.8";

        public DynamicObjectDef key;

        public DynamicStoryTellerDef def;

        public bool IsNull
        {
            get
            {
                return spine35skeleton == null && spine38skeleton == null;
            }
        }

        public void CreateSpineAnimation()
        {
            if (!gameObject.activeInHierarchy)
                gameObject.SetActive(true);

            if (ver == "3.8")
            {
                if (spine38skeleton != null)
                    return;
                SpineTextAssetData data = ModDynamicObjectManager.spine38Database[key.defName];
                Spine38.Unity.SpineAtlasAsset atlas;
                if (key.importMode == ImportMode.File)
                    atlas = Spine38.Unity.SpineAtlasAsset.CreateRuntimeInstance(data.atlasTxt, data.textures, data.shader, true);
                else
                    atlas = Spine38.Unity.SpineAtlasAsset.CreateRuntimeInstance(data.atlasTxt, data.materials, true);
                Spine38.Unity.SkeletonDataAsset skeleton = Spine38.Unity.SkeletonDataAsset.CreateRuntimeInstance(data.skeletonByte, atlas, true);
                spine38skeleton = Spine38.Unity.SkeletonAnimation.NewSkeletonAnimationGameObject(skeleton);
                spine38skeleton.transform.parent = gameObject.transform;
                spine38skeleton.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
                spine38skeleton.transform.position = new Vector3(0f, -1.6f, 5f);
                spine38skeleton.skeleton.SetSkin(def.skin);
                spine38skeleton.AnimationState.SetAnimation(0, def.animationName, def.loop);
                spine38skeleton.Initialize(false);
            }
            else
            {
                if (spine35skeleton != null)
                    return;
                SpineTextAssetData data = ModDynamicObjectManager.spine35Database[key.defName];
                Spine35.Unity.AtlasAsset atlas;
                if (key.importMode == ImportMode.File)
                    atlas = Spine35.Unity.AtlasAsset.CreateRuntimeInstance(data.atlasTxt, data.textures, data.shader, true);
                else
                    atlas = Spine35.Unity.AtlasAsset.CreateRuntimeInstance(data.atlasTxt, data.materials, true);
                Spine35.Unity.SkeletonDataAsset skeleton = Spine35.Unity.SkeletonDataAsset.CreateRuntimeInstance(data.skeletonByte, atlas, true);
                spine35skeleton = Spine35.Unity.SkeletonAnimation.NewSkeletonAnimationGameObject(skeleton);
                spine35skeleton.transform.parent = gameObject.transform;
                spine35skeleton.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
                spine35skeleton.transform.position = new Vector3(0f, -1.6f, 5f);
                spine35skeleton.skeleton.SetSkin(def.skin);
                spine35skeleton.AnimationState.SetAnimation(0, def.animationName, def.loop);
                spine35skeleton.Initialize(false);
            }
        }
    }
}
