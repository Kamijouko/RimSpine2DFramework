using System;
using System.Reflection;
using System.Text;
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
                if (data.skeletonBytes != null && data.skeletonBytes.Length > 0)
                {
                    skeleton = CreateSkeletonFromBytes(instance, data, atlas);
                }
                else
                {
                    skeleton = Spine38.Unity.SkeletonDataAsset.CreateRuntimeInstance(data.skeletonByte, atlas, true);
                }
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

        private static readonly FieldInfo SkeletonDataField = typeof(Spine38.Unity.SkeletonDataAsset).GetField("skeletonData", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo StateDataField = typeof(Spine38.Unity.SkeletonDataAsset).GetField("stateData", BindingFlags.Instance | BindingFlags.NonPublic);

        private static Spine38.Unity.SkeletonDataAsset CreateSkeletonFromBytes(DynamicObjectInstance instance, SpineTextAssetData data, Spine38.Unity.SpineAtlasAsset atlas)
        {
            Spine38.Unity.SkeletonDataAsset skeletonAsset = Spine38.Unity.SkeletonDataAsset.CreateRuntimeInstance(data.skeletonByte, atlas, false);
            Spine38.Atlas atlasInstance = atlas.GetAtlas();
            Spine38.AttachmentLoader attachmentLoader = atlasInstance != null
                ? (Spine38.AttachmentLoader)new Spine38.AtlasAttachmentLoader(atlasInstance)
                : new Spine38.Unity.RegionlessAttachmentLoader();

            float scale = skeletonAsset.scale;
            string skeletonPath = instance?.key?.spine?.skeletonPath ?? data.skeletonByte?.name ?? string.Empty;
            bool isBinary = skeletonPath.EndsWith(".skel", StringComparison.OrdinalIgnoreCase) || skeletonPath.EndsWith(".skel.bytes", StringComparison.OrdinalIgnoreCase);

            Spine38.SkeletonData skeletonData;
            try
            {
                if (isBinary)
                {
                    skeletonData = Spine38.Unity.SkeletonDataAsset.ReadSkeletonData(data.skeletonBytes, attachmentLoader, scale);
                }
                else
                {
                    string jsonText = data.skeletonByte != null ? data.skeletonByte.text : Encoding.UTF8.GetString(data.skeletonBytes);
                    skeletonData = Spine38.Unity.SkeletonDataAsset.ReadSkeletonData(jsonText, attachmentLoader, scale);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[RimSpine2DFramework] Failed to parse Spine 3.8 skeleton for '{instance?.key?.defName ?? "unknown"}': {ex}");
                return Spine38.Unity.SkeletonDataAsset.CreateRuntimeInstance(data.skeletonByte, atlas, true);
            }

            if (skeletonAsset.skeletonDataModifiers != null)
            {
                foreach (Spine38.Unity.SkeletonDataModifierAsset modifier in skeletonAsset.skeletonDataModifiers)
                {
                    if (modifier == null)
                    {
                        continue;
                    }

                    if (skeletonAsset.isUpgradingBlendModeMaterials && modifier is Spine38.Unity.BlendModeMaterialsAsset)
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
            Spine38.AnimationStateData stateData = new Spine38.AnimationStateData(skeletonData);
            StateDataField?.SetValue(skeletonAsset, stateData);
            skeletonAsset.FillStateData();
            return skeletonAsset;
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

            public ISpineTrackEntryAdapter SetEmptyAnimation(int trackIndex, float mixDuration)
            {
                Spine38.TrackEntry trackEntry = animationState.SetEmptyAnimation(trackIndex, mixDuration);
                return trackEntry == null ? null : new Spine38TrackEntryAdapter(trackEntry);
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
