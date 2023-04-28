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

        public void Update()
        {
            //特殊动作循环，
            //根据IdleTimes（当前循环次数）和def里设置的特殊动画间隔次数来判断是否执行特殊动画，
            //执行特殊动画逻辑与HarmonyMain中注释的点击动画逻辑一致
            if (canInteract == true && IdleTimes >= def.specialAnimationLoopForIdleAnimationTimes)
            {
                IdleTimes = 0;
                canInteract = false;
                if (ver == "3.8")
                {
                    Spine38.TrackEntry track = spine38skeleton.AnimationState.AddAnimation(0, def.specialAnimationName, false, 0f);
                    track.Complete += delegate (Spine38.TrackEntry t)
                    {
                        if (track.Animation.Name == def.specialAnimationName)
                            canInteract = true;
                    };
                    Spine38.TrackEntry track2 = spine38skeleton.AnimationState.AddAnimation(0, def.idleAnimationName, def.loop, 0f);
                    track2.Complete += delegate (Spine38.TrackEntry t)
                    {
                        if (canInteract == true && track2.Animation.Name == def.idleAnimationName)
                            IdleTimes++;
                    };
                }
                else if (ver == "3.5")
                {
                    Spine35.TrackEntry track = spine35skeleton.AnimationState.AddAnimation(0, def.specialAnimationName, false, 0f);
                    track.Complete += delegate (Spine35.TrackEntry t)
                    {
                        if (track.Animation.Name == def.specialAnimationName)
                            canInteract = true;
                    };
                    Spine35.TrackEntry track2 = spine35skeleton.AnimationState.AddAnimation(0, def.idleAnimationName, def.loop, 0f);
                    track2.Complete += delegate (Spine35.TrackEntry t)
                    {
                        if (canInteract == true && track2.Animation.Name == def.idleAnimationName)
                            IdleTimes++;
                    };
                }
                else
                {
                    Spine41.TrackEntry track = spine41skeleton.AnimationState.AddAnimation(0, def.specialAnimationName, false, 0f);
                    track.Complete += delegate (Spine41.TrackEntry t)
                    {
                        if (track.Animation.Name == def.specialAnimationName)
                            canInteract = true;
                    };
                    Spine41.TrackEntry track2 = spine41skeleton.AnimationState.AddAnimation(0, def.idleAnimationName, def.loop, 0f);
                    track2.Complete += delegate (Spine41.TrackEntry t)
                    {
                        if (canInteract == true && track2.Animation.Name == def.idleAnimationName)
                            IdleTimes++;
                    };
                } 
                
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
                //设置为循环Idle动画，并且添加在单次循环完成后属性IdleTimes加1的事件（记录循环次数）
                Spine38.TrackEntry track = spine38skeleton.AnimationState.SetAnimation(0, def.idleAnimationName, def.loop);
                track.Complete += delegate (Spine38.TrackEntry t)
                {
                    if (canInteract == true && track.Animation.Name == def.idleAnimationName)
                        IdleTimes++;
                };
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
