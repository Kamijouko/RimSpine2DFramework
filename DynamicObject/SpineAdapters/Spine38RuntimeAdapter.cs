using System;
using UnityEngine;

namespace RimSpine2DFramework
{
    internal class Spine38RuntimeAdapter : ISpineRuntimeAdapter
    {
        public string Version => "3.8";

        public bool HasSkeleton(DynamicObjectInstance instance)
        {
            return instance.spine38skeleton != null;
        }

        public void EnsureSkeleton(DynamicObjectInstance instance)
        {
            if (instance.spine38skeleton != null)
            {
                return;
            }

            SpineTextAssetData data = ModDynamicObjectManager.spine38Database[instance.key.defName];
            Spine38.Unity.SpineAtlasAsset atlas;
            Spine38.Unity.SkeletonDataAsset skeleton;
            if (instance.key.importMode == ImportMode.File)
            {
                atlas = Spine38.Unity.SpineAtlasAsset.CreateRuntimeInstance(data.atlasTxt, data.textures, data.shader, true);
                skeleton = Spine38.Unity.SkeletonDataAsset.CreateRuntimeInstance(data.skeletonByte, atlas, true);
            }
            else
            {
                atlas = Spine38.Unity.SpineAtlasAsset.CreateRuntimeInstance(data.atlasTxt, data.materials, true);
                skeleton = Spine38.Unity.SkeletonDataAsset.CreateRuntimeInstance(data.skeletonByte, atlas, true);
            }

            Spine38.Unity.SkeletonAnimation skeletonAnimation = Spine38.Unity.SkeletonAnimation.NewSkeletonAnimationGameObject(skeleton);
            instance.spine38skeleton = skeletonAnimation;
            ConfigureSkeleton(instance, instance.spine38skeleton);
        }

        private static void ConfigureSkeleton(DynamicObjectInstance instance, Spine38.Unity.SkeletonAnimation skeleton)
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
            if (instance.spine38skeleton == null)
            {
                throw new InvalidOperationException("Spine 3.8 skeleton is not initialized.");
            }

            return new Spine38AnimationStateAdapter(instance.spine38skeleton.AnimationState);
        }

        private class Spine38AnimationStateAdapter : ISpineAnimationStateAdapter
        {
            private readonly Spine38.AnimationState animationState;

            public Spine38AnimationStateAdapter(Spine38.AnimationState animationState)
            {
                this.animationState = animationState;
            }

            public ISpineTrackEntryAdapter SetAnimation(int trackIndex, string animationName, bool loop)
            {
                return new Spine38TrackEntryAdapter(animationState.SetAnimation(trackIndex, animationName, loop));
            }

            public ISpineTrackEntryAdapter AddAnimation(int trackIndex, string animationName, bool loop, float delay)
            {
                return new Spine38TrackEntryAdapter(animationState.AddAnimation(trackIndex, animationName, loop, delay));
            }

            public ISpineTrackEntryAdapter AddEmptyAnimation(int trackIndex, float mixDuration, float delay)
            {
                Spine38.TrackEntry trackEntry = animationState.AddEmptyAnimation(trackIndex, mixDuration, delay);
                return trackEntry == null ? null : new Spine38TrackEntryAdapter(trackEntry);
            }
        }

        private class Spine38TrackEntryAdapter : ISpineTrackEntryAdapter
        {
            private readonly Spine38.TrackEntry trackEntry;

            public Spine38TrackEntryAdapter(Spine38.TrackEntry trackEntry)
            {
                this.trackEntry = trackEntry;
            }

            public string AnimationName => trackEntry.Animation?.Name;

            public void OnComplete(Action action)
            {
                void Handler(Spine38.TrackEntry entry)
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
