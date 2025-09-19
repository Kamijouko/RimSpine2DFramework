using System;
using UnityEngine;

namespace RimSpine2DFramework
{
    internal class Spine35RuntimeAdapter : ISpineRuntimeAdapter
    {
        public string Version => "3.5";

        public bool HasSkeleton(DynamicObjectInstance instance)
        {
            return instance.spine35skeleton != null;
        }

        public void EnsureSkeleton(DynamicObjectInstance instance)
        {
            if (instance.spine35skeleton != null)
            {
                return;
            }

            SpineTextAssetData data = ModDynamicObjectManager.spine35Database[instance.key.defName];
            Spine35.Unity.AtlasAsset atlas;
            Spine35.Unity.SkeletonDataAsset skeleton;
            if (instance.key.importMode == ImportMode.File)
            {
                atlas = Spine35.Unity.AtlasAsset.CreateRuntimeInstance(data.atlasTxt, data.textures, data.shader, true);
                skeleton = Spine35.Unity.SkeletonDataAsset.CreateRuntimeInstance(data.skeletonByte, atlas, true);
            }
            else
            {
                atlas = Spine35.Unity.AtlasAsset.CreateRuntimeInstance(data.atlasTxt, data.materials, true);
                skeleton = Spine35.Unity.SkeletonDataAsset.CreateRuntimeInstance(data.skeletonByte, atlas, true);
            }

            Spine35.Unity.SkeletonAnimation skeletonAnimation = Spine35.Unity.SkeletonAnimation.NewSkeletonAnimationGameObject(skeleton);
            ConfigureSkeleton(instance, skeletonAnimation);
            instance.spine35skeleton = skeletonAnimation;
        }

        private static void ConfigureSkeleton(DynamicObjectInstance instance, Spine35.Unity.SkeletonAnimation skeleton)
        {
            skeleton.transform.parent = instance.gameObject.transform;
            skeleton.transform.localScale = new Vector3(instance.scale.x * instance.def.scale.x, instance.scale.y * instance.def.scale.y, instance.scale.z);
            skeleton.transform.rotation = Quaternion.Euler(instance.def.rotation);
            skeleton.transform.position = new Vector3(instance.position.x + instance.def.offset.x, instance.position.y + instance.def.offset.y, instance.position.z + instance.def.cameraDistance);
            skeleton.skeleton.SetSkin(instance.def.skin);
            skeleton.Initialize(false);
        }

        public ISpineAnimationStateAdapter GetAnimationState(DynamicObjectInstance instance)
        {
            if (instance.spine35skeleton == null)
            {
                throw new InvalidOperationException("Spine 3.5 skeleton is not initialized.");
            }

            return new Spine35AnimationStateAdapter(instance.spine35skeleton.AnimationState);
        }

        private class Spine35AnimationStateAdapter : ISpineAnimationStateAdapter
        {
            private readonly Spine35.AnimationState animationState;

            public Spine35AnimationStateAdapter(Spine35.AnimationState animationState)
            {
                this.animationState = animationState;
            }

            public ISpineTrackEntryAdapter SetAnimation(int trackIndex, string animationName, bool loop)
            {
                return new Spine35TrackEntryAdapter(animationState.SetAnimation(trackIndex, animationName, loop));
            }

            public ISpineTrackEntryAdapter AddAnimation(int trackIndex, string animationName, bool loop, float delay)
            {
                return new Spine35TrackEntryAdapter(animationState.AddAnimation(trackIndex, animationName, loop, delay));
            }
        }

        private class Spine35TrackEntryAdapter : ISpineTrackEntryAdapter
        {
            private readonly Spine35.TrackEntry trackEntry;

            public Spine35TrackEntryAdapter(Spine35.TrackEntry trackEntry)
            {
                this.trackEntry = trackEntry;
            }

            public string AnimationName => trackEntry.Animation?.Name;

            public void OnComplete(Action action)
            {
                void Handler(Spine35.TrackEntry entry)
                {
                    if (entry == trackEntry)
                    {
                        trackEntry.Complete -= Handler;
                        action();
                    }
                }

                trackEntry.Complete += Handler;
            }
        }
    }
}
