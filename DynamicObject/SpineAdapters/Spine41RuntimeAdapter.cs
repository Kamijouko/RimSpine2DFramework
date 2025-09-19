using System;
using UnityEngine;

namespace RimSpine2DFramework
{
    internal class Spine41RuntimeAdapter : ISpineRuntimeAdapter
    {
        public string Version => "4.1";

        public bool HasSkeleton(DynamicObjectInstance instance)
        {
            return instance.spine41skeleton != null;
        }

        public void EnsureSkeleton(DynamicObjectInstance instance)
        {
            if (instance.spine41skeleton != null)
            {
                return;
            }

            SpineTextAssetData data = ModDynamicObjectManager.spine41Database[instance.key.defName];
            Spine41.Unity.SpineAtlasAsset atlas;
            Spine41.Unity.SkeletonDataAsset skeleton;
            if (instance.key.importMode == ImportMode.File)
            {
                atlas = Spine41.Unity.SpineAtlasAsset.CreateRuntimeInstance(data.atlasTxt, data.textures, data.shader, true);
                skeleton = Spine41.Unity.SkeletonDataAsset.CreateRuntimeInstance(data.skeletonByte, atlas, true);
            }
            else
            {
                atlas = Spine41.Unity.SpineAtlasAsset.CreateRuntimeInstance(data.atlasTxt, data.materials, true);
                skeleton = Spine41.Unity.SkeletonDataAsset.CreateRuntimeInstance(data.skeletonByte, atlas, true);
            }

            Spine41.Unity.SkeletonAnimation skeletonAnimation = Spine41.Unity.SkeletonAnimation.NewSkeletonAnimationGameObject(skeleton);
            ConfigureSkeleton(instance, skeletonAnimation);
            instance.spine41skeleton = skeletonAnimation;
        }

        private static void ConfigureSkeleton(DynamicObjectInstance instance, Spine41.Unity.SkeletonAnimation skeleton)
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
            if (instance.spine41skeleton == null)
            {
                throw new InvalidOperationException("Spine 4.1 skeleton is not initialized.");
            }

            return new Spine41AnimationStateAdapter(instance.spine41skeleton.AnimationState);
        }

        private class Spine41AnimationStateAdapter : ISpineAnimationStateAdapter
        {
            private readonly Spine41.AnimationState animationState;

            public Spine41AnimationStateAdapter(Spine41.AnimationState animationState)
            {
                this.animationState = animationState;
            }

            public ISpineTrackEntryAdapter SetAnimation(int trackIndex, string animationName, bool loop)
            {
                return new Spine41TrackEntryAdapter(animationState.SetAnimation(trackIndex, animationName, loop));
            }

            public ISpineTrackEntryAdapter AddAnimation(int trackIndex, string animationName, bool loop, float delay)
            {
                return new Spine41TrackEntryAdapter(animationState.AddAnimation(trackIndex, animationName, loop, delay));
            }

            public ISpineTrackEntryAdapter AddEmptyAnimation(int trackIndex, float mixDuration, float delay)
            {
                Spine41.TrackEntry trackEntry = animationState.AddEmptyAnimation(trackIndex, mixDuration, delay);
                return trackEntry == null ? null : new Spine41TrackEntryAdapter(trackEntry);
            }
        }

        private class Spine41TrackEntryAdapter : ISpineTrackEntryAdapter
        {
            private readonly Spine41.TrackEntry trackEntry;

            public Spine41TrackEntryAdapter(Spine41.TrackEntry trackEntry)
            {
                this.trackEntry = trackEntry;
            }

            public string AnimationName => trackEntry.Animation?.Name;

            public void OnComplete(Action action)
            {
                void Handler(Spine41.TrackEntry entry)
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
