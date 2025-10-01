using System;
using System.Reflection;
using System.Text;
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
                if (data.skeletonBytes != null && data.skeletonBytes.Length > 0)
                {
                    skeleton = CreateSkeletonFromBytes(instance, data, atlas);
                }
                else
                {
                    skeleton = Spine35.Unity.SkeletonDataAsset.CreateRuntimeInstance(data.skeletonByte, atlas, true);
                }
            }
            else
            {
                atlas = Spine35.Unity.AtlasAsset.CreateRuntimeInstance(data.atlasTxt, data.materials, true);
                skeleton = Spine35.Unity.SkeletonDataAsset.CreateRuntimeInstance(data.skeletonByte, atlas, true);
            }

            Spine35.Unity.SkeletonAnimation skeletonAnimation = Spine35.Unity.SkeletonAnimation.NewSkeletonAnimationGameObject(skeleton);
            instance.spine35skeleton = skeletonAnimation;
            ConfigureSkeleton(instance, instance.spine35skeleton);
            
        }

        private static readonly FieldInfo SkeletonDataField = typeof(Spine35.Unity.SkeletonDataAsset).GetField("skeletonData", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo StateDataField = typeof(Spine35.Unity.SkeletonDataAsset).GetField("stateData", BindingFlags.Instance | BindingFlags.NonPublic);

        private static Spine35.Unity.SkeletonDataAsset CreateSkeletonFromBytes(DynamicObjectInstance instance, SpineTextAssetData data, Spine35.Unity.AtlasAsset atlas)
        {
            Spine35.Unity.SkeletonDataAsset skeletonAsset = Spine35.Unity.SkeletonDataAsset.CreateRuntimeInstance(data.skeletonByte, atlas, false);
            Spine35.Atlas atlasInstance = atlas.GetAtlas();
            if (atlasInstance == null || data.skeletonBytes == null || data.skeletonBytes.Length == 0)
            {
                return Spine35.Unity.SkeletonDataAsset.CreateRuntimeInstance(data.skeletonByte, atlas, true);
            }

            Spine35.AttachmentLoader attachmentLoader = new Spine35.AtlasAttachmentLoader(atlasInstance);
            float scale = skeletonAsset.scale;

            string skeletonPath = instance?.key?.spine?.skeletonPath ?? data.skeletonByte?.name ?? string.Empty;
            bool isBinary = skeletonPath.EndsWith(".skel", StringComparison.OrdinalIgnoreCase) || skeletonPath.EndsWith(".skel.bytes", StringComparison.OrdinalIgnoreCase);

            Spine35.SkeletonData skeletonData;
            try
            {
                if (isBinary)
                {
                    skeletonData = Spine35.Unity.SkeletonDataAsset.ReadSkeletonData(data.skeletonBytes, attachmentLoader, scale);
                }
                else
                {
                    string jsonText = data.skeletonByte != null ? data.skeletonByte.text : Encoding.UTF8.GetString(data.skeletonBytes);
                    skeletonData = Spine35.Unity.SkeletonDataAsset.ReadSkeletonData(jsonText, attachmentLoader, scale);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[RimSpine2DFramework] Failed to parse Spine 3.5 skeleton for '{instance?.key?.defName ?? "unknown"}': {ex}");
                return Spine35.Unity.SkeletonDataAsset.CreateRuntimeInstance(data.skeletonByte, atlas, true);
            }

            SkeletonDataField?.SetValue(skeletonAsset, skeletonData);
            Spine35.AnimationStateData stateData = new Spine35.AnimationStateData(skeletonData);
            StateDataField?.SetValue(skeletonAsset, stateData);
            skeletonAsset.FillStateData();
            return skeletonAsset;
        }

        private static void ConfigureSkeleton(DynamicObjectInstance instance, Spine35.Unity.SkeletonAnimation skeleton)
        {
            DynamicObjectInstance.SkeletonConfiguration config = instance.GetEffectiveSkeletonConfiguration();
            skeleton.transform.parent = instance.gameObject.transform;
            skeleton.transform.localScale = new Vector3(instance.scale.x * config.Scale.x, instance.scale.y * config.Scale.y, instance.scale.z);
            Quaternion baseRotation = instance.def == null ? Quaternion.LookRotation(Vector3.down, Vector3.forward) : Quaternion.identity;
            skeleton.transform.rotation = baseRotation * Quaternion.Euler(config.Rotation);
            skeleton.transform.position = new Vector3(instance.position.x + config.Offset.x, instance.position.y + config.Offset.y, instance.position.z + config.CameraDistance);
            if (!string.IsNullOrEmpty(config.Skin))
            {
                skeleton.skeleton.SetSkin(config.Skin);
            }

            skeleton.Initialize(false);
        }

        public void SetSkin(DynamicObjectInstance instance, string skinName)
        {
            if (instance?.spine35skeleton == null || string.IsNullOrEmpty(skinName))
            {
                return;
            }

            Spine35.Unity.SkeletonAnimation skeletonAnimation = instance.spine35skeleton;
            Spine35.Skeleton skeleton = skeletonAnimation.Skeleton;
            if (skeleton == null)
            {
                return;
            }

            if (!string.Equals(skeleton.Skin?.Name, skinName, StringComparison.OrdinalIgnoreCase))
            {
                skeleton.SetSkin(skinName);
            }

            skeleton.SetSlotsToSetupPose();
            skeletonAnimation.AnimationState?.Apply(skeleton);
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

            public ISpineTrackEntryAdapter SetEmptyAnimation(int trackIndex, float mixDuration)
            {
                Spine35.TrackEntry trackEntry = animationState.SetEmptyAnimation(trackIndex, mixDuration);
                return trackEntry == null ? null : new Spine35TrackEntryAdapter(trackEntry);
            }

            public ISpineTrackEntryAdapter AddEmptyAnimation(int trackIndex, float mixDuration, float delay)
            {
                Spine35.TrackEntry trackEntry = animationState.AddEmptyAnimation(trackIndex, mixDuration, delay);
                return trackEntry == null ? null : new Spine35TrackEntryAdapter(trackEntry);
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
                        action();
                    }
                }

                trackEntry.Complete += Handler;
            }

            public void OnEvent(Action<SpineEventArgs> action)
            {
                if (action == null)
                {
                    return;
                }

                void Handler(Spine35.TrackEntry entry, Spine35.Event e)
                {
                    if (entry != trackEntry || e == null)
                    {
                        return;
                    }

                    SpineEventArgs args = new SpineEventArgs(
                        e.Data?.Name,
                        e.Time,
                        e.Int,
                        e.Float,
                        e.String,
                        null,
                        null);

                    action(args);
                }

                trackEntry.Event += Handler;
            }

            public void SetMixDuration(float mixDuration)
            {
                trackEntry.MixDuration = mixDuration;
            }
        }
    }
}
