using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace RimSpine2DFramework
{
    public class DynamicPawnStateController
    {
        private const int VerbRetentionTicks = 120;

        private readonly DynamicObjectInstance instance;
        private readonly DynamicPawnStateMachineDef definition;
        private readonly Pawn pawn;

        private string currentStateId;
        private string forcedStateId;

        private string lastJobDefName;
        private int lastJobStageIndex = -1;
        private string lastJobStageLabel;

        private string lastVerbIdentifier;
        private string lastVerbAbility;
        private int lastVerbTick = -1;

        private bool needsRefresh = true;
        private bool disposed;

        public DynamicPawnStateController(DynamicObjectInstance instance, Pawn pawn, DynamicPawnStateMachineDef definition)
        {
            this.instance = instance ?? throw new ArgumentNullException(nameof(instance));
            this.pawn = pawn ?? throw new ArgumentNullException(nameof(pawn));
            this.definition = definition ?? throw new ArgumentNullException(nameof(definition));

            instance.AttachStateController(this);
        }

        public DynamicObjectInstance Instance => instance;

        public DynamicPawnStateMachineDef Definition => definition;

        public Pawn Pawn => pawn;

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            instance.DetachStateController(this);
            DynamicPawnStateRegistry.NotifyControllerDisposed(this);
        }

        public void RefreshNow()
        {
            needsRefresh = true;
            Tick();
        }

        public void Tick()
        {
            if (disposed)
            {
                return;
            }

            if (pawn == null || pawn.DestroyedOrNull())
            {
                Dispose();
                return;
            }

            if (lastVerbIdentifier != null)
            {
                int ticksGame = Find.TickManager?.TicksGame ?? -1;
                if (ticksGame >= 0 && lastVerbTick >= 0 && ticksGame - lastVerbTick > VerbRetentionTicks)
                {
                    lastVerbIdentifier = null;
                    lastVerbAbility = null;
                    needsRefresh = true;
                }
            }

            if (!needsRefresh)
            {
                return;
            }

            needsRefresh = false;
            EvaluateAndApply();
        }

        public void NotifyJobUpdated(Job job, int stageIndex, string stageLabel)
        {
            if (disposed)
            {
                return;
            }

            lastJobDefName = job?.def?.defName;
            lastJobStageIndex = stageIndex;
            lastJobStageLabel = stageLabel;
            RequestRefresh();
        }

        public void NotifyJobEnded()
        {
            if (disposed)
            {
                return;
            }

            lastJobDefName = null;
            lastJobStageIndex = -1;
            lastJobStageLabel = null;
            RequestRefresh();
        }

        public void NotifyVerbUsed(Verb verb)
        {
            if (disposed)
            {
                return;
            }

            lastVerbIdentifier = ResolveVerbSource(verb, out string abilityDef);
            lastVerbAbility = abilityDef;
            lastVerbTick = Find.TickManager?.TicksGame ?? lastVerbTick;
            RequestRefresh();
        }

        public void NotifyNeedsChanged()
        {
            RequestRefresh();
        }

        public void NotifyHediffsChanged()
        {
            RequestRefresh();
        }

        public void NotifyThoughtsChanged()
        {
            RequestRefresh();
        }

        public void TriggerInteraction()
        {
            if (!string.IsNullOrEmpty(definition.interactionStateId))
            {
                forcedStateId = definition.interactionStateId;
            }

            RequestRefresh();
        }

        private void RequestRefresh()
        {
            needsRefresh = true;
        }

        private void EvaluateAndApply()
        {
            if (!TryEnsureSkeleton(out ISpineRuntimeAdapter adapter))
            {
                return;
            }

            DynamicPawnStateMachineDef.PawnAnimationState state = SelectState();
            if (state == null)
            {
                return;
            }

            bool shouldReplay = state.forceRestart || !string.Equals(currentStateId, state.stateId, StringComparison.OrdinalIgnoreCase);
            if (!shouldReplay)
            {
                return;
            }

            ApplyState(adapter, state);
        }

        private DynamicPawnStateMachineDef.PawnAnimationState SelectState()
        {
            if (!string.IsNullOrEmpty(forcedStateId))
            {
                DynamicPawnStateMachineDef.PawnAnimationState forced = definition.states?.FirstOrDefault(s => s != null && string.Equals(s.stateId, forcedStateId, StringComparison.OrdinalIgnoreCase));
                forcedStateId = null;
                if (forced != null)
                {
                    return forced;
                }
            }

            IEnumerable<DynamicPawnStateMachineDef.PawnAnimationState> states = definition.states?.Where(s => s != null);
            if (states == null)
            {
                return null;
            }

            DynamicPawnStateMachineDef.PawnAnimationState fallback = null;
            foreach (DynamicPawnStateMachineDef.PawnAnimationState state in states.OrderByDescending(s => s.priority))
            {
                if (state.isFallback)
                {
                    fallback ??= state;
                    continue;
                }

                if (state.triggers.NullOrEmpty())
                {
                    return state;
                }

                if (state.triggers.All(MatchesTrigger))
                {
                    return state;
                }
            }

            if (!string.IsNullOrEmpty(definition.defaultStateId))
            {
                DynamicPawnStateMachineDef.PawnAnimationState defaultState = states.FirstOrDefault(s => string.Equals(s.stateId, definition.defaultStateId, StringComparison.OrdinalIgnoreCase));
                if (defaultState != null)
                {
                    return defaultState;
                }
            }

            return fallback;
        }

        private bool MatchesTrigger(DynamicPawnStateMachineDef.PawnStateTrigger trigger)
        {
            if (trigger == null)
            {
                return true;
            }

            switch (trigger.source)
            {
                case DynamicPawnStateMachineDef.PawnStateTriggerSource.Job:
                    return MatchesJobTrigger(trigger);
                case DynamicPawnStateMachineDef.PawnStateTriggerSource.Verb:
                    return MatchesVerbTrigger(trigger);
                case DynamicPawnStateMachineDef.PawnStateTriggerSource.Need:
                    return MatchesNeedTrigger(trigger);
                case DynamicPawnStateMachineDef.PawnStateTriggerSource.Hediff:
                    return MatchesHediffTrigger(trigger);
                case DynamicPawnStateMachineDef.PawnStateTriggerSource.Thought:
                    return MatchesThoughtTrigger(trigger);
                case DynamicPawnStateMachineDef.PawnStateTriggerSource.Duty:
                    return MatchesDutyTrigger(trigger);
                case DynamicPawnStateMachineDef.PawnStateTriggerSource.MentalState:
                    return MatchesMentalStateTrigger(trigger);
                default:
                    return false;
            }
        }

        private bool MatchesJobTrigger(DynamicPawnStateMachineDef.PawnStateTrigger trigger)
        {
            string target = ResolveTriggerDefName(trigger);
            if (!string.IsNullOrEmpty(target))
            {
                if (!string.Equals(lastJobDefName, target, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            if (trigger.stageIndex >= 0 && lastJobStageIndex != trigger.stageIndex)
            {
                return false;
            }

            if (!string.IsNullOrEmpty(trigger.stageName))
            {
                if (string.IsNullOrEmpty(lastJobStageLabel) || !string.Equals(lastJobStageLabel, trigger.stageName, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            return true;
        }

        private bool MatchesVerbTrigger(DynamicPawnStateMachineDef.PawnStateTrigger trigger)
        {
            string target = ResolveTriggerDefName(trigger);
            if (string.IsNullOrEmpty(target))
            {
                return false;
            }

            if (!string.IsNullOrEmpty(lastVerbAbility) && string.Equals(lastVerbAbility, target, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (!string.IsNullOrEmpty(lastVerbIdentifier) && string.Equals(lastVerbIdentifier, target, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return false;
        }

        private bool MatchesNeedTrigger(DynamicPawnStateMachineDef.PawnStateTrigger trigger)
        {
            string target = ResolveTriggerDefName(trigger);
            if (string.IsNullOrEmpty(target))
            {
                return false;
            }

            Need need = pawn.needs?.AllNeeds?.FirstOrDefault(n => string.Equals(n.def?.defName, target, StringComparison.OrdinalIgnoreCase));
            if (need == null)
            {
                return false;
            }

            if (!float.IsNaN(trigger.threshold))
            {
                float value = need.CurLevel;
                bool comparison = trigger.thresholdGreaterOrEqual ? value >= trigger.threshold : value <= trigger.threshold;
                if (!comparison)
                {
                    return false;
                }
            }

            if (trigger.stageIndex >= 0 && (int)need.CurCategory != trigger.stageIndex)
            {
                return false;
            }

            if (!string.IsNullOrEmpty(trigger.stageName))
            {
                if (!string.Equals(need.CurCategory.ToString(), trigger.stageName, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            return true;
        }

        private bool MatchesHediffTrigger(DynamicPawnStateMachineDef.PawnStateTrigger trigger)
        {
            string target = ResolveTriggerDefName(trigger);
            if (string.IsNullOrEmpty(target))
            {
                return false;
            }

            Hediff hediff = pawn.health?.hediffSet?.hediffs?.FirstOrDefault(h => string.Equals(h.def?.defName, target, StringComparison.OrdinalIgnoreCase));
            if (hediff == null)
            {
                return false;
            }

            if (!float.IsNaN(trigger.threshold))
            {
                float severity = hediff.Severity;
                bool comparison = trigger.thresholdGreaterOrEqual ? severity >= trigger.threshold : severity <= trigger.threshold;
                if (!comparison)
                {
                    return false;
                }
            }

            if (trigger.stageIndex >= 0 && hediff.CurStageIndex != trigger.stageIndex)
            {
                return false;
            }

            if (!string.IsNullOrEmpty(trigger.stageName))
            {
                string label = hediff.CurStage?.label;
                if (string.IsNullOrEmpty(label) || !string.Equals(label, trigger.stageName, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            return true;
        }

        private bool MatchesThoughtTrigger(DynamicPawnStateMachineDef.PawnStateTrigger trigger)
        {
            string target = ResolveTriggerDefName(trigger);
            if (string.IsNullOrEmpty(target))
            {
                return false;
            }

            ThoughtHandler handler = pawn.needs?.mood?.thoughts;
            Thought_MemoryHandler memories = handler?.memories;
            List<Thought_Memory> list = memories?.MemoriesListForReading;
            if (list == null)
            {
                return false;
            }

            foreach (Thought_Memory memory in list)
            {
                if (memory?.def == null)
                {
                    continue;
                }

                if (!string.Equals(memory.def.defName, target, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (trigger.stageIndex >= 0 && memory.CurStageIndex != trigger.stageIndex)
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(trigger.stageName))
                {
                    string label = memory.CurStage?.label;
                    if (string.IsNullOrEmpty(label) || !string.Equals(label, trigger.stageName, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }
                }

                return true;
            }

            return false;
        }

        private bool MatchesDutyTrigger(DynamicPawnStateMachineDef.PawnStateTrigger trigger)
        {
            string target = ResolveTriggerDefName(trigger);
            if (string.IsNullOrEmpty(target))
            {
                return false;
            }

            LordDuty duty = pawn.mindState?.duty;
            if (duty?.def == null)
            {
                return false;
            }

            return string.Equals(duty.def.defName, target, StringComparison.OrdinalIgnoreCase);
        }

        private bool MatchesMentalStateTrigger(DynamicPawnStateMachineDef.PawnStateTrigger trigger)
        {
            if (!pawn.InMentalState)
            {
                return false;
            }

            string target = ResolveTriggerDefName(trigger);
            if (string.IsNullOrEmpty(target))
            {
                return true;
            }

            MentalState mentalState = pawn.MentalState;
            if (mentalState?.def == null)
            {
                return false;
            }

            return string.Equals(mentalState.def.defName, target, StringComparison.OrdinalIgnoreCase);
        }

        private void ApplyState(ISpineRuntimeAdapter adapter, DynamicPawnStateMachineDef.PawnAnimationState state)
        {
            if (string.IsNullOrEmpty(state.animationName))
            {
                Log.Warning($"[RimSpine2D] State '{state.stateId}' on '{definition.defName}' has no animationName.");
                return;
            }

            ISpineAnimationStateAdapter animationState = adapter.GetAnimationState(instance);
            if (animationState == null)
            {
                Log.Warning($"[RimSpine2D] Unable to resolve animation state for '{definition.defName}'.");
                return;
            }

            if (state.clearTrack)
            {
                animationState.SetEmptyAnimation(state.trackIndex, state.clearMixDuration);
            }

            ISpineTrackEntryAdapter entry = state.useQueue
                ? animationState.AddAnimation(state.trackIndex, state.animationName, state.loop, state.delay)
                : animationState.SetAnimation(state.trackIndex, state.animationName, state.loop);

            if (entry == null)
            {
                Log.Warning($"[RimSpine2D] Failed to play animation '{state.animationName}' for state '{state.stateId}' on '{definition.defName}'.");
                return;
            }

            if (state.mixDuration > 0f)
            {
                entry.SetMixDuration(state.mixDuration);
            }

            currentStateId = state.stateId;
        }

        private bool TryEnsureSkeleton(out ISpineRuntimeAdapter adapter)
        {
            adapter = null;
            if (!instance.TryGetSpineAdapter(out adapter))
            {
                Log.Warning($"[RimSpine2D] Unable to resolve Spine adapter for instance '{instance.name ?? instance.gameObject?.name}'.");
                return false;
            }

            if (!adapter.HasSkeleton(instance))
            {
                adapter.EnsureSkeleton(instance);
            }

            return true;
        }

        private static string ResolveTriggerDefName(DynamicPawnStateMachineDef.PawnStateTrigger trigger)
        {
            return trigger.def?.defName ?? trigger.defName;
        }

        private static string ResolveVerbSource(Verb verb, out string abilityDef)
        {
            abilityDef = null;
            if (verb == null)
            {
                return null;
            }

            if (verb is Verb_CastAbility abilityVerb && abilityVerb.ability?.def != null)
            {
                abilityDef = abilityVerb.ability.def.defName;
                return abilityDef;
            }

            if (verb.EquipmentSource != null && verb.EquipmentSource.def != null)
            {
                return verb.EquipmentSource.def.defName;
            }

            if (verb.HediffCompSource != null && verb.HediffCompSource.parent?.def != null)
            {
                return verb.HediffCompSource.parent.def.defName;
            }

            return verb.GetType().Name;
        }
    }
}
