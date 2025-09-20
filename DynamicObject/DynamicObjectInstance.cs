using System;
using System.Collections.Generic;
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

        private const int InteractionTrackIndex = 1;
        private const float InteractionFadeInMixDuration = 0.2f;
        private const float InteractionFadeOutMixDuration = 0.4f;

        private DynamicObjectStateController stateController;

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

        public Pawn boundPawn;

        public bool IsNull
        {
            get
            {
                return spine35skeleton == null && spine38skeleton == null && spine40skeleton == null && spine41skeleton == null && gObject == null;
            }
        }

        public void Update()
        {
            if (def == null)
            {
                return;
            }

            EnsureStateController();
            stateController?.Evaluate(boundPawn);
            stateController?.ApplyState(this);
        }

        public void CreateSpineAnimation()
        {
            if (!gameObject.activeInHierarchy)
            {
                gameObject.SetActive(true);
            }

            if (def == null)
            {
                throw new InvalidOperationException("DynamicObjectInstance definition is not initialized.");
            }

            EnsureStateController();

            ISpineRuntimeAdapter adapter = GetAdapter();
            if (adapter.HasSkeleton(this))
            {
                return;
            }

            adapter.EnsureSkeleton(this);
            stateController?.OnSkeletonReady();
            stateController?.ApplyState(this);
        }

        public void PlayInteractionAnimation()
        {
            if (!canInteract || def == null)
            {
                return;
            }

            if (!TryGetAdapter(out ISpineRuntimeAdapter adapter) || !adapter.HasSkeleton(this))
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

        internal bool TryGetAdapter(out ISpineRuntimeAdapter adapter)
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

        internal void AttachIdleCompletion(ISpineTrackEntryAdapter trackEntry)
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

        internal void AttachReenableInteraction(ISpineTrackEntryAdapter trackEntry)
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

        public void BindPawn(Pawn pawn)
        {
            boundPawn = pawn;
            EnsureStateController();
            stateController?.BindPawn(pawn);
        }

        public void UnbindPawn()
        {
            stateController?.UnbindPawn(boundPawn);
            boundPawn = null;
        }

        private void EnsureStateController()
        {
            if (stateController != null || def == null)
            {
                return;
            }

            stateController = new DynamicObjectStateController(this);
            if (boundPawn != null)
            {
                stateController.BindPawn(boundPawn);
            }
        }

        private void OnDestroy()
        {
            stateController?.UnbindPawn(boundPawn);
        }
    }
}
