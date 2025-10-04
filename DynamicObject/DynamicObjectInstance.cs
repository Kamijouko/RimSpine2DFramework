using System;
using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.AI;

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

        private bool VisibleWhileCarried => pawnStateController?.VisibleWhileCarried ?? false;

        private bool VisibleWhileStored => pawnStateController?.VisibleWhileStored ?? false;

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
                UpdateSkeletonVisibility(false);
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

            if (ShouldSkipRenderingForThing(curPawn))
            {
                return false;
            }

            if (!curPawn.DestroyedOrNull())
            {
                drawPosition = curPawn.DrawPos;
                return true;
            }

            Corpse corpse = curPawn.Corpse;

            if (corpse == null)
            {
                return false;
            }

            if (ShouldSkipRenderingForThing(corpse))
            {
                return false;
            }

            Map currentMap = Find.CurrentMap;

            if (corpse.Spawned)
            {
                if (currentMap == null || corpse.Map != currentMap)
                {
                    return false;
                }

                drawPosition = corpse.DrawPos;
                return true;
            }

            if (!TryGetHeldThingDrawInfo(corpse, out Vector3 holderPosition, out Map holderMap))
            {
                return false;
            }

            if (currentMap == null || holderMap != currentMap)
            {
                return false;
            }

            drawPosition = holderPosition;
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
            /*if (WorldRendererUtility.WorldSelected)
            {
                return false;
            }*/

            if (!WorldRendererUtility.DrawingMap)
            {
                return false;
            }

            if (curPawn == null)
            {
                return def != null;
            }

            Map currentMap = Find.CurrentMap;

            if (ShouldSkipRenderingForThing(curPawn))
            {
                return false;
            }

            if (curPawn.DestroyedOrNull())
            {
                Corpse corpse = curPawn.Corpse;

                if (corpse == null)
                {
                    return false;
                }

                if (ShouldSkipRenderingForThing(corpse))
                {
                    return false;
                }

                Map corpseMap;

                if (corpse.Spawned)
                {
                    corpseMap = corpse.Map;
                }
                else if (!TryGetHeldThingDrawInfo(corpse, out _, out corpseMap))
                {
                    return false;
                }

                if (currentMap == null || corpseMap == null)
                {
                    return false;
                }

                return corpseMap == currentMap;
            }

            Map pawnMap = curPawn.MapHeld;

            if (pawnMap == null)
            {
                if (!TryGetHeldThingDrawInfo(curPawn, out _, out Map holderMap))
                {
                    return false;
                }

                if (holderMap == null || currentMap == null)
                {
                    return false;
                }

                return holderMap == currentMap;
            }

            if (currentMap == null)
            {
                return false;
            }

            return pawnMap == currentMap;
        }

        private bool ShouldSkipRenderingForThing(Thing thing)
        {
            if (thing == null)
            {
                return true;
            }

            IThingHolder holder = GetEffectiveRenderHolder(thing);

            if (holder == null)
            {
                return false;
            }

            if (holder is Pawn_CarryTracker)
            {
                return !VisibleWhileCarried;
            }

            if (IsStoredInHolder(holder))
            {
                return !VisibleWhileStored;
            }

            return false;
        }

        private bool TryGetHeldThingDrawInfo(Thing thing, out Vector3 holderPosition, out Map holderMap)
        {
            holderPosition = default;
            holderMap = null;

            if (thing == null)
            {
                return false;
            }

            IThingHolder holder = GetEffectiveRenderHolder(thing);

            if (holder == null)
            {
                return false;
            }

            if (holder is Pawn_CarryTracker carryTracker)
            {
                if (!VisibleWhileCarried)
                {
                    return false;
                }

                Pawn carrier = carryTracker.pawn;

                if (carrier != null)
                {
                    holderMap = carrier.MapHeld;
                    holderPosition = carrier.Spawned ? carrier.DrawPos : GetHeldThingFallbackPosition(thing);
                }
                else
                {
                    holderMap = GetThingHolderMap(holder);
                    holderPosition = GetHeldThingFallbackPosition(thing);
                }

                if (holderMap == null)
                {
                    holderMap = GetThingHolderMap(holder);
                }

                return holderMap != null;
            }

            if (holder is Map mapHolder)
            {
                holderMap = mapHolder;
                holderPosition = GetHeldThingFallbackPosition(thing);
                return true;
            }

            if (IsStoredInHolder(holder) && !VisibleWhileStored)
            {
                // WorldObject holders (transport pods, shuttles, caravans, etc.) are treated as stored.
                // Returning false here prevents drawing when ResolveRenderHolder resolves to a
                // WorldObject, ensuring we respect visibility while stored for corpses and pawns
                // travelling inside those containers.
                return false;
            }

            if (holder is Thing holderThing)
            {
                holderMap = holderThing.MapHeld;
                holderPosition = holderThing.Spawned ? holderThing.DrawPos : GetHeldThingFallbackPosition(holderThing);

                if (holderMap == null)
                {
                    holderMap = GetThingHolderMap(holder);
                }

                return holderMap != null;
            }

            holderMap = GetThingHolderMap(holder);

            if (holderMap == null)
            {
                return false;
            }

            holderPosition = GetHeldThingFallbackPosition(thing);
            return true;
        }

        private IThingHolder GetEffectiveRenderHolder(Thing thing)
        {
            if (thing == null)
            {
                return null;
            }

            IThingHolder holder = thing.ParentHolder;

            if (thing is Pawn && holder is Corpse corpse)
            {
                return GetCorpseRenderHolder(corpse);
            }

            return ResolveRenderHolder(holder);
        }

        private static IThingHolder GetCorpseRenderHolder(Corpse corpse)
        {
            if (corpse == null)
            {
                return null;
            }

            return ResolveRenderHolder(corpse.ParentHolder);
        }

        private static IThingHolder ResolveRenderHolder(IThingHolder holder)
        {
            while (holder != null)
            {
                if (TryResolveStoredCargoHolder(holder, out IThingHolder storageHolder))
                {
                    return storageHolder;
                }

                switch (holder)
                {
                    case Map _:
                        return holder;
                    case Pawn_CarryTracker _:
                        return holder;
                    case WorldObject _:
                        return holder;
                    case WorldObjectComp worldObjectComp:
                        holder = worldObjectComp.ParentHolder;
                        continue;
                    case Thing thingHolder when thingHolder is Corpse:
                        holder = holder.ParentHolder;
                        continue;
                    case Thing _:
                        return holder;
                }

                holder = holder.ParentHolder;
            }

            return null;
        }

        private static bool TryResolveStoredCargoHolder(IThingHolder holder, out IThingHolder storageHolder)
        {
            storageHolder = null;

            if (holder == null)
            {
                return false;
            }

            switch (holder)
            {
                case Map _:
                case Pawn_CarryTracker _:
                    return false;
                case CompTransporter transporter:
                    storageHolder = (IThingHolder)transporter.parent;
                    return true;
                case ThingOwner thingOwner:
                {
                    IThingHolder owner = thingOwner.Owner;

                    if (owner == null)
                    {
                        storageHolder = (IThingHolder)thingOwner;
                        return true;
                    }

                    if (owner is CompTransporter ownerTransporter)
                    {
                        storageHolder = (IThingHolder)ownerTransporter.parent;
                        return true;
                    }

                    if (owner is Thing ownerThing)
                    {
                        storageHolder = (IThingHolder)ownerThing;
                        return true;
                    }

                    if (!(owner is Map) && !(owner is Pawn_CarryTracker))
                    {
                        storageHolder = owner;
                        return true;
                    }

                    break;
                }
            }

            if (!(holder is Map) && !(holder is Pawn_CarryTracker) && !(holder is WorldObjectComp) && !(holder is ThingOwner))
            {
                if (holder is ThingComp comp && comp.parent != null)
                {
                    storageHolder = (IThingHolder)comp.parent;
                    return true;
                }

                storageHolder = holder;
                return true;
            }

            return false;
        }

        private static Map GetThingHolderMap(IThingHolder holder)
        {
            if (holder == null)
            {
                return null;
            }

            if (holder is Map mapHolder)
            {
                return mapHolder;
            }

            if (holder is Thing holderThing)
            {
                return holderThing.MapHeld ?? ThingOwnerUtility.GetRootMap(holder);
            }

            return ThingOwnerUtility.GetRootMap(holder);
        }

        private static Vector3 GetHeldThingFallbackPosition(Thing thing)
        {
            if (thing == null)
            {
                return Vector3.zero;
            }

            IntVec3 positionHeld = thing.PositionHeld;

            if (positionHeld.IsValid)
            {
                return positionHeld.ToVector3Shifted();
            }

            if (thing.Spawned)
            {
                return thing.DrawPos;
            }

            return GenThing.TrueCenter(thing);
        }

        private static bool IsStoredInHolder(IThingHolder holder)
        {
            if (holder == null)
            {
                return false;
            }

            if (holder is Pawn_CarryTracker)
            {
                return false;
            }

            return !(holder is Map);
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
