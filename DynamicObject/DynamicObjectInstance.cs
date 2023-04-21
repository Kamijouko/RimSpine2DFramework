using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;
using UnityEngine;
using System.IO;
using System.Collections;

namespace DynamicObject
{
    public class DynamicObjectInstance : MonoBehaviour
    {
        public Spine35.Unity.SkeletonAnimation spine35skeleton;

        public Spine38.Unity.SkeletonAnimation spine38skeleton;

        public Spine41.Unity.SkeletonAnimation spine41skeleton;

        public string ver = "3.8";

        public DynamicObjectDef key;

        public DynamicStoryTellerDef def;

        public Vector3 scale = new Vector3(0.2f, 0.2f, 1f);

        public Vector3 position = new Vector3(0f, -1.6f, 5f);

        public bool canInteract = true;

        public int IdleTimes = 0;

        public bool IsNull
        {
            get
            {
                return spine35skeleton == null && spine41skeleton == null && spine41skeleton == null;
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
                Spine38.Unity.SkeletonDataAsset skeleton;
                if (key.importMode == ImportMode.File)
                {
                    atlas = Spine38.Unity.SpineAtlasAsset.CreateRuntimeInstance(data.atlasTxt, data.textures, data.shader, true);
                    skeleton = Spine38.Unity.SkeletonDataAsset.CreateRuntimeInstance(data.skeletonByte, atlas, true);
                }
                else
                {
                    atlas = Spine38.Unity.SpineAtlasAsset.CreateRuntimeInstance(data.atlasTxt, data.materials, true);
                    skeleton = Spine38.Unity.SkeletonDataAsset.CreateRuntimeInstance(data.skeletonByte, atlas, true);
                }
                spine38skeleton = Spine38.Unity.SkeletonAnimation.NewSkeletonAnimationGameObject(skeleton);
                spine38skeleton.transform.parent = gameObject.transform;
                spine38skeleton.transform.localScale = new Vector3(scale.x * def.scale.x, scale.y * def.scale.y, scale.z);
                spine38skeleton.transform.rotation = Quaternion.Euler(def.rotation);
                spine38skeleton.transform.position = new Vector3(position.x + def.offset.x, position.y + def.offset.y, position.z + def.cameraDistance);
                spine38skeleton.skeleton.SetSkin(def.skin);
                spine38skeleton.AnimationState.SetAnimation(0, def.idleAnimationName, def.loop);
                spine38skeleton.Initialize(false);
            }
            else if (ver == "3.5")
            {
                if (spine35skeleton != null)
                    return;
                SpineTextAssetData data = ModDynamicObjectManager.spine35Database[key.defName];
                Spine35.Unity.AtlasAsset atlas;
                Spine35.Unity.SkeletonDataAsset skeleton;
                if (key.importMode == ImportMode.File)
                {
                    atlas = Spine35.Unity.AtlasAsset.CreateRuntimeInstance(data.atlasTxt, data.textures, data.shader, true);
                    skeleton = Spine35.Unity.SkeletonDataAsset.CreateRuntimeInstance(data.skeletonByte, atlas, true);
                }
                else
                {
                    atlas = Spine35.Unity.AtlasAsset.CreateRuntimeInstance(data.atlasTxt, data.materials, true);
                    skeleton = Spine35.Unity.SkeletonDataAsset.CreateRuntimeInstance(data.skeletonByte, atlas, true);
                }
                spine35skeleton = Spine35.Unity.SkeletonAnimation.NewSkeletonAnimationGameObject(skeleton);
                spine35skeleton.transform.parent = gameObject.transform;
                spine35skeleton.transform.localScale = new Vector3(scale.x * def.scale.x, scale.y * def.scale.y, scale.z);
                spine35skeleton.transform.rotation = Quaternion.Euler(def.rotation);
                spine35skeleton.transform.position = new Vector3(position.x + def.offset.x, position.y + def.offset.y, position.z + def.cameraDistance);
                spine35skeleton.skeleton.SetSkin(def.skin);
                spine35skeleton.AnimationState.SetAnimation(0, def.idleAnimationName, def.loop);
                spine35skeleton.Initialize(false);
            }
            else
            {
                if (spine41skeleton != null)
                    return;
                SpineTextAssetData data = ModDynamicObjectManager.spine41Database[key.defName];
                Spine41.Unity.SpineAtlasAsset atlas;
                Spine41.Unity.SkeletonDataAsset skeleton;
                if (key.importMode == ImportMode.File)
                {
                    atlas = Spine41.Unity.SpineAtlasAsset.CreateRuntimeInstance(data.atlasTxt, data.textures, data.shader, true);
                    skeleton = Spine41.Unity.SkeletonDataAsset.CreateRuntimeInstance(data.skeletonByte, atlas, true);
                }
                else
                {
                    atlas = Spine41.Unity.SpineAtlasAsset.CreateRuntimeInstance(data.atlasTxt, data.materials, true);
                    skeleton = Spine41.Unity.SkeletonDataAsset.CreateRuntimeInstance(data.skeletonByte, atlas, true);
                }
                spine41skeleton = Spine41.Unity.SkeletonAnimation.NewSkeletonAnimationGameObject(skeleton);
                spine41skeleton.transform.parent = gameObject.transform;
                spine41skeleton.transform.localScale = new Vector3(scale.x * def.scale.x, scale.y * def.scale.y, scale.z);
                spine41skeleton.transform.rotation = Quaternion.Euler(def.rotation);
                spine41skeleton.transform.position = new Vector3(position.x + def.offset.x, position.y + def.offset.y, position.z + def.cameraDistance);
                spine41skeleton.skeleton.SetSkin(def.skin);
                spine41skeleton.AnimationState.SetAnimation(0, def.idleAnimationName, def.loop);
                spine41skeleton.Initialize(false);
            }
        }
    }
}
