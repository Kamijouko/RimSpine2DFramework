using System;
using UnityEngine;

namespace RimSpine2DFramework
{
    internal class Spine40RuntimeAdapter : ISpineRuntimeAdapter
    {
        public string Version => "4.0";

        public bool HasSkeleton(DynamicObjectInstance instance)
        {
            return instance.spine40skeleton != null;
        }

        public void EnsureSkeleton(DynamicObjectInstance instance)
        {
            if (instance.spine40skeleton != null)
            {
                return;
            }

            SpineTextAssetData data = ModDynamicObjectManager.spine40Database[instance.key.defName];
            Spine40.Unity.SpineAtlasAsset atlas;
            Spine40.Unity.SkeletonDataAsset skeleton;
            if (instance.key.importMode == ImportMode.File)
            {
                atlas = Spine40.Unity.SpineAtlasAsset.CreateRuntimeInstance(data.atlasTxt, data.textures, data.shader, true);
                skeleton = Spine40.Unity.SkeletonDataAsset.CreateRuntimeInstance(data.skeletonByte, atlas, true);
            }
            else
            {
                atlas = Spine40.Unity.SpineAtlasAsset.CreateRuntimeInstance(data.atlasTxt, data.materials, true);
                skeleton = Spine40.Unity.SkeletonDataAsset.CreateRuntimeInstance(data.skeletonByte, atlas, true);
            }

            Spine40.Unity.SkeletonAnimation skeletonAnimation = Spine40.Unity.SkeletonAnimation.NewSkeletonAnimationGameObject(skeleton);
            instance.spine40skeleton = skeletonAnimation;
            ConfigureSkeleton(instance, instance.spine40skeleton);
            
        }

        private static void ConfigureSkeleton(DynamicObjectInstance instance, Spine40.Unity.SkeletonAnimation skeleton)
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
            if (instance.spine40skeleton == null)
            {
                throw new InvalidOperationException("Spine 4.0 skeleton is not initialized.");
            }

            return new Spine40AnimationStateAdapter(instance.spine40skeleton.AnimationState);
        }

        private class Spine40AnimationStateAdapter : ISpineAnimationStateAdapter
        {
            private readonly Spine40.AnimationState animationState;

            public Spine40AnimationStateAdapter(Spine40.AnimationState animationState)
            {
                this.animationState = animationState;
            }

            public ISpineTrackEntryAdapter SetAnimation(int trackIndex, string animationName, bool loop)
            {
                return new Spine40TrackEntryAdapter(animationState.SetAnimation(trackIndex, animationName, loop));
            }

            public ISpineTrackEntryAdapter AddAnimation(int trackIndex, string animationName, bool loop, float delay)
            {
                return new Spine40TrackEntryAdapter(animationState.AddAnimation(trackIndex, animationName, loop, delay));
            }

            public ISpineTrackEntryAdapter SetEmptyAnimation(int trackIndex, float mixDuration)
            {
                Spine40.TrackEntry trackEntry = animationState.SetEmptyAnimation(trackIndex, mixDuration);
                return trackEntry == null ? null : new Spine40TrackEntryAdapter(trackEntry);
            }

            public ISpineTrackEntryAdapter AddEmptyAnimation(int trackIndex, float mixDuration, float delay)
            {
                Spine40.TrackEntry trackEntry = animationState.AddEmptyAnimation(trackIndex, mixDuration, delay);
                return trackEntry == null ? null : new Spine40TrackEntryAdapter(trackEntry);
            }
        }

        private class Spine40TrackEntryAdapter : ISpineTrackEntryAdapter
        {
            private readonly Spine40.TrackEntry trackEntry;

            public Spine40TrackEntryAdapter(Spine40.TrackEntry trackEntry)
            {
                this.trackEntry = trackEntry;
            }

            public string AnimationName => trackEntry.Animation?.Name;

            public void OnComplete(Action action)
            {
                void Handler(Spine40.TrackEntry entry)
                {
                    if (entry == trackEntry)
                    {
                        trackEntry.Complete -= Handler;
                        action();
                    }
                }

                trackEntry.Complete += Handler;
            }

            public void SetMixDuration(float mixDuration)
            {
                trackEntry.MixDuration = mixDuration;
            }
        }
    }
}
