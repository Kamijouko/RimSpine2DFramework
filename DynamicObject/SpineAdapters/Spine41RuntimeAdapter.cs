using System;
using System.Reflection;
using System.Text;
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
                if (data.skeletonBytes != null && data.skeletonBytes.Length > 0)
                {
                    skeleton = CreateSkeletonFromBytes(instance, data, atlas);
                }
                else
                {
                    skeleton = Spine41.Unity.SkeletonDataAsset.CreateRuntimeInstance(data.skeletonByte, atlas, true);
                }
            }
            else
            {
                atlas = Spine41.Unity.SpineAtlasAsset.CreateRuntimeInstance(data.atlasTxt, data.materials, true);
                skeleton = Spine41.Unity.SkeletonDataAsset.CreateRuntimeInstance(data.skeletonByte, atlas, true);
            }

            Spine41.Unity.SkeletonAnimation skeletonAnimation = Spine41.Unity.SkeletonAnimation.NewSkeletonAnimationGameObject(skeleton);
            instance.spine41skeleton = skeletonAnimation;
            ConfigureSkeleton(instance, instance.spine41skeleton);
            
        }

        private static readonly FieldInfo SkeletonDataField = typeof(Spine41.Unity.SkeletonDataAsset).GetField("skeletonData", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo StateDataField = typeof(Spine41.Unity.SkeletonDataAsset).GetField("stateData", BindingFlags.Instance | BindingFlags.NonPublic);

        private static Spine41.Unity.SkeletonDataAsset CreateSkeletonFromBytes(DynamicObjectInstance instance, SpineTextAssetData data, Spine41.Unity.SpineAtlasAsset atlas)
        {
            Spine41.Unity.SkeletonDataAsset skeletonAsset = Spine41.Unity.SkeletonDataAsset.CreateRuntimeInstance(data.skeletonByte, atlas, false);
            Spine41.Atlas atlasInstance = atlas.GetAtlas();
            Spine41.AttachmentLoader attachmentLoader = atlasInstance != null
                ? (Spine41.AttachmentLoader)new Spine41.AtlasAttachmentLoader(atlasInstance)
                : new Spine41.Unity.RegionlessAttachmentLoader();

            float scale = skeletonAsset.scale;
            string skeletonPath = instance?.key?.spine?.skeletonPath ?? data.skeletonByte?.name ?? string.Empty;
            bool isBinary = skeletonPath.EndsWith(".skel", StringComparison.OrdinalIgnoreCase) || skeletonPath.EndsWith(".skel.bytes", StringComparison.OrdinalIgnoreCase);

            Spine41.SkeletonData skeletonData;
            try
            {
                if (isBinary)
                {
                    skeletonData = Spine41.Unity.SkeletonDataAsset.ReadSkeletonData(data.skeletonBytes, attachmentLoader, scale);
                }
                else
                {
                    string jsonText = data.skeletonByte != null ? data.skeletonByte.text : Encoding.UTF8.GetString(data.skeletonBytes);
                    skeletonData = Spine41.Unity.SkeletonDataAsset.ReadSkeletonData(jsonText, attachmentLoader, scale);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[RimSpine2DFramework] Failed to parse Spine 4.1 skeleton for '{instance?.key?.defName ?? "unknown"}': {ex}");
                return Spine41.Unity.SkeletonDataAsset.CreateRuntimeInstance(data.skeletonByte, atlas, true);
            }

            if (skeletonAsset.skeletonDataModifiers != null)
            {
                foreach (Spine41.Unity.SkeletonDataModifierAsset modifier in skeletonAsset.skeletonDataModifiers)
                {
                    if (modifier == null)
                    {
                        continue;
                    }

                    if (skeletonAsset.isUpgradingBlendModeMaterials && modifier is Spine41.Unity.BlendModeMaterialsAsset)
                    {
                        continue;
                    }

                    modifier.Apply(skeletonData);
                }
            }

            if (!skeletonAsset.isUpgradingBlendModeMaterials)
            {
                skeletonAsset.blendModeMaterials.ApplyMaterials(skeletonData);
            }

            SkeletonDataField?.SetValue(skeletonAsset, skeletonData);
            Spine41.AnimationStateData stateData = new Spine41.AnimationStateData(skeletonData);
            StateDataField?.SetValue(skeletonAsset, stateData);
            skeletonAsset.FillStateData();
            return skeletonAsset;
        }

        private static void ConfigureSkeleton(DynamicObjectInstance instance, Spine41.Unity.SkeletonAnimation skeleton)
        {
            DynamicObjectInstance.SkeletonConfiguration config = instance.GetEffectiveSkeletonConfiguration();
            skeleton.transform.parent = instance.gameObject.transform;
            skeleton.transform.localScale = new Vector3(instance.scale.x * config.Scale.x, instance.scale.y * config.Scale.y, instance.scale.z);
            Quaternion baseRotation = instance.def == null ? Quaternion.Euler(90f, 0f, 0f) : Quaternion.identity;
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
            if (instance?.spine41skeleton == null || string.IsNullOrEmpty(skinName))
            {
                return;
            }

            Spine41.Unity.SkeletonAnimation skeletonAnimation = instance.spine41skeleton;
            Spine41.Skeleton skeleton = skeletonAnimation.Skeleton;
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

            public ISpineTrackEntryAdapter SetEmptyAnimation(int trackIndex, float mixDuration)
            {
                Spine41.TrackEntry trackEntry = animationState.SetEmptyAnimation(trackIndex, mixDuration);
                return trackEntry == null ? null : new Spine41TrackEntryAdapter(trackEntry);
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
