using System;
using System.Collections.Generic;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace RimSpine2DFramework
{
    public class DynamicObjectInstance : MonoBehaviour
    {
        internal Spine35.Unity.SkeletonAnimation spine35skeleton;
        internal Spine38.Unity.SkeletonAnimation spine38skeleton;
        internal Spine40.Unity.SkeletonAnimation spine40skeleton;
        internal Spine41.Unity.SkeletonAnimation spine41skeleton;

        public GameObject gObject;

        public string ver = "3.8";

        public DynamicObjectDef key;

        public DynamicStoryTellerDef def;

        public Vector3 scale = new Vector3(0.2f, 0.2f, 1f);

        public Vector3 position = new Vector3(0f, -1.6f, 5f);

        public bool canInteract = true;

        public int IdleTimes = 0;

        private Pawn curPawn;

        private DynamicPawnStateController pawnStateController;
        private SkeletonConfiguration? pawnSkeletonConfiguration;
        private Vector3 pawnPositionOffset;
        private bool pawnPositionOffsetInitialized;

        private const int InteractionTrackIndex = 1;
        private const float InteractionFadeInMixDuration = 0.2f;
        private const float InteractionFadeOutMixDuration = 0.4f;

        private static readonly ISpineRuntimeAdapter Spine35Adapter = new Spine35RuntimeAdapter();
        private static readonly ISpineRuntimeAdapter Spine38Adapter = new Spine38RuntimeAdapter();
        private static readonly ISpineRuntimeAdapter Spine40Adapter = new Spine40RuntimeAdapter();
        private static readonly ISpineRuntimeAdapter Spine41Adapter = new Spine41RuntimeAdapter();

        private static readonly Dictionary<Tuple<ImportMode, string>, ISpineRuntimeAdapter> AdapterLookup = new Dictionary<Tuple<ImportMode, string>, ISpineRuntimeAdapter>
        {
            { Tuple.Create(ImportMode.File, Spine35Adapter.Version), Spine35Adapter },
            { Tuple.Create(ImportMode.AssetBundle, Spine35Adapter.Version), Spine35Adapter },
            { Tuple.Create(ImportMode.File, Spine38Adapter.Version), Spine38Adapter },
            { Tuple.Create(ImportMode.AssetBundle, Spine38Adapter.Version), Spine38Adapter },
            { Tuple.Create(ImportMode.File, Spine40Adapter.Version), Spine40Adapter },
            { Tuple.Create(ImportMode.AssetBundle, Spine40Adapter.Version), Spine40Adapter },
            { Tuple.Create(ImportMode.File, Spine41Adapter.Version), Spine41Adapter },
            { Tuple.Create(ImportMode.AssetBundle, Spine41Adapter.Version), Spine41Adapter }
        };

        public bool IsNull
        {
            get
            {
                return spine35skeleton == null && spine38skeleton == null && spine40skeleton == null && spine41skeleton == null && gObject == null;
            }
        }

        internal DynamicPawnStateController PawnStateController => pawnStateController;

        internal struct SkeletonConfiguration
        {
            public Vector2 Scale;
            public Vector2 Offset;
            public Vector3 Rotation;
            public float CameraDistance;
            public string Skin;

            public static SkeletonConfiguration Default => new SkeletonConfiguration
            {
                Scale = Vector2.one,
                Offset = Vector2.zero,
                Rotation = Vector3.zero,
                CameraDistance = 1f,
                Skin = "default"
            };
        }

        internal void AttachPawn(Pawn pawn)
        {
            curPawn = pawn;
        }

        internal void DetachPawn(Pawn pawn)
        {
            if (curPawn == pawn)
            {
                if (pawnPositionOffsetInitialized)
                {
                    position = pawnPositionOffset;
                }
                curPawn = null;
                pawnPositionOffsetInitialized = false;
            }
        }

        internal void AttachStateController(DynamicPawnStateController controller)
        {
            pawnStateController = controller;
        }

        internal void DetachStateController(DynamicPawnStateController controller)
        {
            if (pawnStateController == controller)
            {
                pawnStateController = null;
                pawnSkeletonConfiguration = null;
            }
        }

        internal void ApplyPawnSkeletonSettings(DynamicPawnStateMachineDef.PawnSkeletonSettings settings)
        {
            if (settings == null)
            {
                pawnSkeletonConfiguration = null;
                return;
            }

            SkeletonConfiguration configuration = new SkeletonConfiguration
            {
                Scale = settings.scale == Vector2.zero ? Vector2.one : settings.scale,
                Offset = settings.offset,
                Rotation = settings.rotation,
                CameraDistance = settings.cameraDistance < 0f ? 0f : settings.cameraDistance,
                Skin = string.IsNullOrEmpty(settings.defaultSkin) ? SkeletonConfiguration.Default.Skin : settings.defaultSkin
            };

            pawnSkeletonConfiguration = configuration;
        }

        internal void ClearPawnSkeletonSettings()
        {
            pawnSkeletonConfiguration = null;
        }

        internal SkeletonConfiguration GetEffectiveSkeletonConfiguration()
        {
            if (def != null)
            {
                float cameraY = def.cameraDistance <= 0f ? 1f : def.cameraDistance;
                return new SkeletonConfiguration
                {
                    Scale = def.scale == Vector2.zero ? Vector2.one : def.scale,
                    Offset = new Vector2(def.offset.x, cameraY),
                    Rotation = def.rotation,
                    CameraDistance = def.offset.y,
                    Skin = string.IsNullOrEmpty(def.skin) ? SkeletonConfiguration.Default.Skin : def.skin
                };
            }

            return pawnSkeletonConfiguration ?? SkeletonConfiguration.Default;
        }

        internal string GetDefaultSkinName()
        {
            SkeletonConfiguration configuration = GetEffectiveSkeletonConfiguration();
            return string.IsNullOrEmpty(configuration.Skin) ? SkeletonConfiguration.Default.Skin : configuration.Skin;
        }

        internal bool TryGetSpineAdapter(out ISpineRuntimeAdapter adapter)
        {
            return TryResolveAdapter(out adapter);
        }

        public bool TryBindPawn(Pawn pawn)
        {
            if (pawn == null)
            {
                DynamicPawnStateRegistry.Unbind(this);
                return false;
            }

            if (DynamicPawnStateRegistry.TryBind(this, pawn))
            {
                return pawnStateController != null;
            }

            DynamicPawnStateRegistry.Unbind(this);
            return false;
        }

        internal bool TryGetCurrentDrawPosition(out Vector3 drawPosition)
        {
            drawPosition = default;

            if (curPawn == null)
            {
                return false;
            }

            if (!curPawn.DestroyedOrNull())
            {
                drawPosition = curPawn.DrawPos;
                return true;
            }

            Corpse corpse = curPawn.Corpse;

            if (corpse == null || !corpse.Spawned)
            {
                return false;
            }

            Map currentMap = Find.CurrentMap;

            if (currentMap == null || corpse.Map != currentMap)
            {
                return false;
            }

            if (corpse.ParentHolder != null && corpse.ParentHolder != currentMap)
            {
                return false;
            }

            drawPosition = corpse.DrawPos;
            return true;
        }

        internal void SyncWithPawnPosition()
        {
            if (!TryGetCurrentDrawPosition(out Vector3 drawPosition))
            {
                return;
            }

            if (!pawnPositionOffsetInitialized)
            {
                pawnPositionOffset = position;
                pawnPositionOffsetInitialized = true;
            }

            Vector3 targetPosition = drawPosition + pawnPositionOffset;
            transform.position = targetPosition;
            position = targetPosition;

            if (curPawn != null && !curPawn.DestroyedOrNull())
            {
                if (curPawn.Rotation == Rot4.East) transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.LookRotation(Vector3.forward, Vector3.up), 0.6f);
                if (curPawn.Rotation == Rot4.West) transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.LookRotation(Vector3.forward, Vector3.down), 0.6f);
            }
        }

        private void UpdateAnimationTimeScale(bool isPaused)
        {
            float targetTimeScale = isPaused ? 0f : 1f;

            if (spine35skeleton != null && !Mathf.Approximately(spine35skeleton.timeScale, targetTimeScale))
            {
                spine35skeleton.timeScale = targetTimeScale;
            }

            if (spine38skeleton != null && !Mathf.Approximately(spine38skeleton.timeScale, targetTimeScale))
            {
                spine38skeleton.timeScale = targetTimeScale;
            }

            if (spine40skeleton != null && !Mathf.Approximately(spine40skeleton.timeScale, targetTimeScale))
            {
                spine40skeleton.timeScale = targetTimeScale;
            }

            if (spine41skeleton != null && !Mathf.Approximately(spine41skeleton.timeScale, targetTimeScale))
            {
                spine41skeleton.timeScale = targetTimeScale;
            }
        }

        public void Update()
        {
            bool shouldRender = RefreshMapVisibility();
            bool allowPawnRenderingWhileDestroyed = pawnStateController != null && pawnStateController.ShouldRenderWhilePawnDestroyed;
            bool isPaused = Find.TickManager?.Paused ?? false;

            if (pawnStateController != null || curPawn != null)
            {
                UpdateAnimationTimeScale(isPaused);
            }

            if (pawnStateController != null)
            {
                if (shouldRender || allowPawnRenderingWhileDestroyed)
                {
                    SyncWithPawnPosition();
                }

                pawnStateController.Tick();
                return;
            }

            if (!shouldRender)
            {
                return;
            }

            if (curPawn != null && !curPawn.DestroyedOrNull())
            {
                SyncWithPawnPosition();
            }

            if (!canInteract || def == null)
            {
                return;
            }

            if (IdleTimes < def.specialAnimationLoopForIdleAnimationTimes)
            {
                return;
            }

            if (!TryResolveAdapter(out ISpineRuntimeAdapter adapter) || !adapter.HasSkeleton(this))
            {
                return;
            }

            IdleTimes = 0;
            //canInteract = false;

            ISpineAnimationStateAdapter state = adapter.GetAnimationState(this);
            AttachReenableInteraction(state.AddAnimation(0, def.specialAnimationName, false, 0f));
            AttachIdleCompletion(state.AddAnimation(0, def.idleAnimationName, def.loop, 0f));
        }

        private bool ShouldRenderOnCurrentMap()
        {
            if (WorldRendererUtility.WorldRenderedNow)
            {
                return false;
            }

            if (!WorldRendererUtility.DrawingMap)
            {
                return false;
            }

            if (curPawn == null)
            {
                return true;
            }

            Map currentMap = Find.CurrentMap;

            if (curPawn.DestroyedOrNull())
            {
                Corpse corpse = curPawn.Corpse;

                if (corpse == null || !corpse.Spawned)
                {
                    return false;
                }

                if (currentMap == null || corpse.Map != currentMap)
                {
                    return false;
                }

                if (corpse.ParentHolder != null && corpse.ParentHolder != currentMap)
                {
                    return false;
                }

                return true;
            }

            Map pawnMap = curPawn.MapHeld;

            if (pawnMap == null || currentMap == null)
            {
                return true;
            }

            return pawnMap == currentMap;
        }

        private void UpdateSkeletonVisibility(bool visible)
        {
            void SetActiveIfNeeded(Behaviour skeleton)
            {
                if (skeleton == null)
                {
                    return;
                }

                GameObject skeletonObject = skeleton.gameObject;
                if (skeletonObject != null && skeletonObject.activeSelf != visible)
                {
                    skeletonObject.SetActive(visible);
                }
            }

            SetActiveIfNeeded(spine35skeleton);
            SetActiveIfNeeded(spine38skeleton);
            SetActiveIfNeeded(spine40skeleton);
            SetActiveIfNeeded(spine41skeleton);
        }

        internal bool RefreshMapVisibility()
        {
            bool shouldRender = ShouldRenderOnCurrentMap();
            UpdateSkeletonVisibility(shouldRender);
            return shouldRender;
        }

        public void CreateSpineAnimation()
        {
            if (!gameObject.activeInHierarchy)
            {
                gameObject.SetActive(true);
            }

            if (pawnStateController != null)
            {
                if (TryGetSpineAdapter(out ISpineRuntimeAdapter stateAdapter))
                {
                    if (!stateAdapter.HasSkeleton(this))
                    {
                        stateAdapter.EnsureSkeleton(this);
                    }

                    pawnStateController.RefreshNow();
                }

                return;
            }

            if (def == null)
            {
                throw new InvalidOperationException("DynamicObjectInstance definition is not initialized.");
            }

            ISpineRuntimeAdapter adapter = GetAdapter();
            if (adapter.HasSkeleton(this))
            {
                return;
            }

            adapter.EnsureSkeleton(this);
            AttachIdleCompletion(adapter.GetAnimationState(this).SetAnimation(0, def.idleAnimationName, def.loop));
        }

        public void PlayInteractionAnimation()
        {
            if (pawnStateController != null)
            {
                pawnStateController.TriggerInteraction();
                return;
            }

            if (!canInteract || def == null)
            {
                return;
            }

            if (!TryResolveAdapter(out ISpineRuntimeAdapter adapter) || !adapter.HasSkeleton(this))
            {
                return;
            }

            canInteract = false;

            ISpineAnimationStateAdapter state = adapter.GetAnimationState(this);
            state.SetEmptyAnimation(InteractionTrackIndex, InteractionFadeInMixDuration);
            ISpineTrackEntryAdapter interactionEntry = state.AddAnimation(InteractionTrackIndex, def.interactAnimationName, false, 0f);
            interactionEntry?.SetMixDuration(InteractionFadeInMixDuration);
            ISpineTrackEntryAdapter emptyEntry = state.AddEmptyAnimation(InteractionTrackIndex, InteractionFadeOutMixDuration, 0f);
            AttachReenableInteraction(emptyEntry ?? interactionEntry);
        }

        private ISpineRuntimeAdapter GetAdapter()
        {
            if (key == null)
            {
                throw new InvalidOperationException("DynamicObjectInstance key is not initialized.");
            }

            string version = GetNormalizedVersion();
            if (version == null)
            {
                throw new InvalidOperationException("DynamicObjectInstance version is not set.");
            }

            Tuple<ImportMode, string> adapterKey = Tuple.Create(key.importMode, version);
            if (!AdapterLookup.TryGetValue(adapterKey, out ISpineRuntimeAdapter adapter))
            {
                throw new InvalidOperationException($"No Spine runtime adapter registered for version '{version}' and import mode '{key.importMode}'.");
            }

            return adapter;
        }

        private bool TryResolveAdapter(out ISpineRuntimeAdapter adapter)
        {
            adapter = null;
            if (key == null)
            {
                return false;
            }

            string version = GetNormalizedVersion();
            if (version == null)
            {
                return false;
            }

            return AdapterLookup.TryGetValue(Tuple.Create(key.importMode, version), out adapter);
        }

        private string GetNormalizedVersion()
        {
            return string.IsNullOrWhiteSpace(ver) ? null : ver.Trim();
        }

        private void AttachIdleCompletion(ISpineTrackEntryAdapter trackEntry)
        {
            if (trackEntry == null)
            {
                return;
            }

            trackEntry.OnComplete(() =>
            {
                if (canInteract)
                {
                    IdleTimes++;
                }
            });
        }

        private void AttachReenableInteraction(ISpineTrackEntryAdapter trackEntry)
        {
            if (trackEntry == null)
            {
                return;
            }

            trackEntry.OnComplete(() =>
            {
                canInteract = true;
            });
        }
    }
}
