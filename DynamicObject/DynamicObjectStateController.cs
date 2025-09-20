using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using RimWorld;
using Verse;

namespace RimSpine2DFramework
{
    public class DynamicObjectStateController
    {
        private readonly DynamicObjectInstance owner;
        private readonly DynamicStoryTellerDef def;
        private readonly Dictionary<string, DynamicStoryTellerDef.DynamicObjectStateNode> stateLookup;
        private readonly List<DynamicStoryTellerDef.DynamicObjectStateNode> orderedStates;
        private readonly List<AnimationCommand> pendingCommands = new List<AnimationCommand>();
        private DynamicStoryTellerDef.DynamicObjectStateNode currentState;
        private DynamicStoryTellerDef.DynamicObjectStateNode nextState;
        private bool pendingLegacy;
        private Pawn boundPawn;

        private static readonly ConditionalWeakTable<Pawn, DynamicObjectStateController> ControllerRegistry = new ConditionalWeakTable<Pawn, DynamicObjectStateController>();

        private bool HasStateMachine => orderedStates.Count > 0;

        public DynamicObjectStateController(DynamicObjectInstance ownerInstance)
        {
            if (ownerInstance == null)
            {
                throw new ArgumentNullException(nameof(ownerInstance));
            }

            owner = ownerInstance;
            def = owner.def ?? throw new ArgumentNullException(nameof(owner.def));
            IEnumerable<DynamicStoryTellerDef.DynamicObjectStateNode> nodes = (def.stateNodes ?? new List<DynamicStoryTellerDef.DynamicObjectStateNode>())
                .Where(s => s != null && !string.IsNullOrEmpty(s.id) && !string.IsNullOrEmpty(s.animation));
            stateLookup = nodes
                .GroupBy(s => s.id, StringComparer.OrdinalIgnoreCase)
                .Select(g => g.First())
                .ToDictionary(s => s.id, s => s, StringComparer.OrdinalIgnoreCase);
            orderedStates = stateLookup.Values
                .OrderByDescending(s => s.priority)
                .ThenBy(s => s.id, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public void BindPawn(Pawn pawn)
        {
            if (pawn == null)
            {
                UnbindPawn();
                return;
            }

            if (ReferenceEquals(boundPawn, pawn))
            {
                return;
            }

            if (boundPawn != null)
            {
                ControllerRegistry.Remove(boundPawn);
            }

            boundPawn = pawn;
            ControllerRegistry.Remove(pawn);
            ControllerRegistry.Add(pawn, this);
            Evaluate(pawn);
            ApplyState(owner);
        }

        public void UnbindPawn(Pawn pawn = null)
        {
            Pawn target = pawn ?? boundPawn;
            if (target == null)
            {
                return;
            }

            ControllerRegistry.Remove(target);
            if (ReferenceEquals(boundPawn, target))
            {
                boundPawn = null;
            }
        }

        public void OnSkeletonReady()
        {
            currentState = null;
            nextState = null;
            pendingCommands.Clear();
            pendingLegacy = false;

            if (HasStateMachine)
            {
                DynamicStoryTellerDef.DynamicObjectStateNode defaultState = GetDefaultState();
                if (defaultState != null)
                {
                    PrepareCommandsForState(defaultState, true);
                    nextState = defaultState;
                }
            }
            else
            {
                QueueLegacyIdle();
            }
        }

        public void Evaluate(Pawn pawn)
        {
            if (pawn != null)
            {
                boundPawn = pawn;
            }

            bool handledByStateMachine = TryEvaluateStateMachine(boundPawn);
            if (!handledByStateMachine && !HasStateMachine)
            {
                EvaluateLegacy();
            }
        }

        public void ApplyState(DynamicObjectInstance instance)
        {
            _ = instance;

            if (pendingCommands.Count == 0)
            {
                return;
            }

            if (!owner.TryGetAdapter(out ISpineRuntimeAdapter adapter) || !adapter.HasSkeleton(owner))
            {
                return;
            }

            ISpineAnimationStateAdapter state = adapter.GetAnimationState(owner);
            foreach (AnimationCommand command in pendingCommands)
            {
                int trackIndex = Math.Max(0, command.TrackIndex);
                ISpineTrackEntryAdapter entry = command.UseQueue
                    ? state.AddAnimation(trackIndex, command.AnimationName, command.Loop, command.Delay)
                    : state.SetAnimation(trackIndex, command.AnimationName, command.Loop);

                if (entry == null)
                {
                    continue;
                }

                if (command.MixDuration.HasValue)
                {
                    entry.SetMixDuration(command.MixDuration.Value);
                }

                if (command.AttachIdleCompletion)
                {
                    owner.AttachIdleCompletion(entry);
                }

                if (command.AttachReenableInteraction)
                {
                    owner.AttachReenableInteraction(entry);
                }
            }

            pendingCommands.Clear();
            if (!pendingLegacy)
            {
                currentState = nextState ?? currentState;
            }
            nextState = null;
            pendingLegacy = false;
        }

        public static void NotifyPawnStateChanged(Pawn pawn)
        {
            if (pawn == null)
            {
                return;
            }

            if (ControllerRegistry.TryGetValue(pawn, out DynamicObjectStateController controller))
            {
                controller.HandleExternalStateChange(pawn);
            }
        }

        private void HandleExternalStateChange(Pawn pawn)
        {
            Evaluate(pawn);
            ApplyState(owner);
        }

        private bool TryEvaluateStateMachine(Pawn pawn)
        {
            if (!HasStateMachine)
            {
                return false;
            }

            DynamicStoryTellerDef.DynamicObjectStateNode candidate = DetermineTargetState(pawn) ?? GetFallbackTarget();
            if (candidate == null)
            {
                return true;
            }

            if (currentState != null && candidate.id != null && string.Equals(currentState.id, candidate.id, StringComparison.OrdinalIgnoreCase) && pendingCommands.Count == 0)
            {
                return true;
            }

            PrepareCommandsForState(candidate);
            nextState = candidate;
            pendingLegacy = false;
            return true;
        }

        private DynamicStoryTellerDef.DynamicObjectStateNode DetermineTargetState(Pawn pawn)
        {
            foreach (DynamicStoryTellerDef.DynamicObjectStateNode node in orderedStates)
            {
                if (node.triggers == null || node.triggers.Count == 0)
                {
                    return node;
                }

                bool allSatisfied = true;
                foreach (DynamicStoryTellerDef.DynamicObjectStateTrigger trigger in node.triggers)
                {
                    if (!IsTriggerSatisfied(trigger, pawn))
                    {
                        allSatisfied = false;
                        break;
                    }
                }

                if (allSatisfied)
                {
                    return node;
                }
            }

            return null;
        }

        private DynamicStoryTellerDef.DynamicObjectStateNode GetFallbackTarget()
        {
            if (currentState != null)
            {
                DynamicStoryTellerDef.DynamicObjectStateNode fallback = ResolveFallbackFor(currentState);
                if (fallback != null)
                {
                    return fallback;
                }
            }

            return GetDefaultState();
        }

        private DynamicStoryTellerDef.DynamicObjectStateNode GetDefaultState()
        {
            if (!string.IsNullOrEmpty(def.defaultStateId))
            {
                DynamicStoryTellerDef.DynamicObjectStateNode byId = ResolveState(def.defaultStateId);
                if (byId != null)
                {
                    return byId;
                }
            }

            DynamicStoryTellerDef.DynamicObjectStateNode unconditional = orderedStates.FirstOrDefault(s => s.triggers == null || s.triggers.Count == 0);
            return unconditional ?? orderedStates.FirstOrDefault();
        }

        private DynamicStoryTellerDef.DynamicObjectStateNode ResolveState(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return null;
            }

            return stateLookup.TryGetValue(id, out DynamicStoryTellerDef.DynamicObjectStateNode node) ? node : null;
        }

        private DynamicStoryTellerDef.DynamicObjectStateNode ResolveFallbackFor(DynamicStoryTellerDef.DynamicObjectStateNode node)
        {
            if (node == null)
            {
                return null;
            }

            if (!string.IsNullOrEmpty(node.fallbackStateId))
            {
                DynamicStoryTellerDef.DynamicObjectStateNode fallback = ResolveState(node.fallbackStateId);
                if (fallback != null)
                {
                    return fallback;
                }
            }

            if (!node.loop)
            {
                return GetDefaultState();
            }

            return null;
        }

        private void PrepareCommandsForState(DynamicStoryTellerDef.DynamicObjectStateNode node, bool forceSet = false)
        {
            pendingCommands.Clear();
            if (node == null || string.IsNullOrEmpty(node.animation))
            {
                return;
            }

            AnimationCommand command = new AnimationCommand
            {
                TrackIndex = Math.Max(0, node.trackIndex),
                AnimationName = node.animation,
                Loop = node.loop,
                UseQueue = !forceSet && node.queue,
                Delay = (!forceSet && node.queue) ? Math.Max(0f, node.delay) : 0f,
                MixDuration = node.mixDuration >= 0f ? node.mixDuration : (float?)null,
                AttachIdleCompletion = ShouldAttachIdleCompletion(node)
            };
            pendingCommands.Add(command);

            DynamicStoryTellerDef.DynamicObjectStateNode fallback = ResolveFallbackFor(node);
            if (fallback != null && (fallback.id == null || !string.Equals(fallback.id, node.id, StringComparison.OrdinalIgnoreCase)))
            {
                pendingCommands.Add(new AnimationCommand
                {
                    TrackIndex = Math.Max(0, fallback.trackIndex),
                    AnimationName = fallback.animation,
                    Loop = fallback.loop,
                    UseQueue = true,
                    Delay = Math.Max(0f, fallback.delay),
                    MixDuration = fallback.mixDuration >= 0f ? fallback.mixDuration : (float?)null,
                    AttachIdleCompletion = ShouldAttachIdleCompletion(fallback)
                });
            }
        }

        private bool ShouldAttachIdleCompletion(DynamicStoryTellerDef.DynamicObjectStateNode node)
        {
            return node != null && node.trackIndex == 0 && node.loop;
        }

        private void EvaluateLegacy()
        {
            if (def == null || string.IsNullOrEmpty(def.specialAnimationName) || string.IsNullOrEmpty(def.idleAnimationName))
            {
                return;
            }

            if (pendingCommands.Count != 0)
            {
                return;
            }

            if (owner.IdleTimes < def.specialAnimationLoopForIdleAnimationTimes)
            {
                return;
            }

            owner.IdleTimes = 0;
            pendingCommands.Add(new AnimationCommand
            {
                TrackIndex = 0,
                AnimationName = def.specialAnimationName,
                Loop = false,
                UseQueue = true,
                Delay = 0f
            });
            pendingCommands.Add(new AnimationCommand
            {
                TrackIndex = 0,
                AnimationName = def.idleAnimationName,
                Loop = def.loop,
                UseQueue = true,
                Delay = 0f,
                AttachIdleCompletion = true
            });
            pendingLegacy = true;
            nextState = null;
        }

        private void QueueLegacyIdle()
        {
            if (string.IsNullOrEmpty(def.idleAnimationName))
            {
                return;
            }

            pendingCommands.Clear();
            pendingCommands.Add(new AnimationCommand
            {
                TrackIndex = 0,
                AnimationName = def.idleAnimationName,
                Loop = def.loop,
                UseQueue = false,
                Delay = 0f,
                AttachIdleCompletion = true
            });
            pendingLegacy = true;
            owner.IdleTimes = 0;
            nextState = null;
        }

        private bool IsTriggerSatisfied(DynamicStoryTellerDef.DynamicObjectStateTrigger trigger, Pawn pawn)
        {
            if (trigger == null)
            {
                return true;
            }

            bool result;
            switch (trigger.triggerType)
            {
                case DynamicStoryTellerDef.DynamicObjectStateTriggerType.Job:
                    result = EvaluateJobTrigger(trigger, pawn);
                    break;
                case DynamicStoryTellerDef.DynamicObjectStateTriggerType.Verb:
                    result = EvaluateVerbTrigger(trigger, pawn);
                    break;
                case DynamicStoryTellerDef.DynamicObjectStateTriggerType.Need:
                    result = EvaluateNeedTrigger(trigger, pawn);
                    break;
                case DynamicStoryTellerDef.DynamicObjectStateTriggerType.Hediff:
                    result = EvaluateHediffTrigger(trigger, pawn);
                    break;
                case DynamicStoryTellerDef.DynamicObjectStateTriggerType.Thought:
                    result = EvaluateThoughtTrigger(trigger, pawn);
                    break;
                case DynamicStoryTellerDef.DynamicObjectStateTriggerType.Always:
                    result = true;
                    break;
                default:
                    result = false;
                    break;
            }

            return trigger.invert ? !result : result;
        }

        private static bool EvaluateJobTrigger(DynamicStoryTellerDef.DynamicObjectStateTrigger trigger, Pawn pawn)
        {
            if (pawn == null || string.IsNullOrEmpty(trigger.defName))
            {
                return false;
            }

            Job curJob = pawn.CurJob;
            if (curJob?.def != null && NamesEqual(curJob.def.defName, trigger.defName))
            {
                return true;
            }

            if (pawn.jobs?.curDriver != null && NamesEqual(pawn.jobs.curDriver.GetType().Name, trigger.defName))
            {
                return true;
            }

            if (pawn.jobs?.jobQueue != null)
            {
                foreach (QueuedJob queued in pawn.jobs.jobQueue)
                {
                    if (queued?.job?.def != null && NamesEqual(queued.job.def.defName, trigger.defName))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool EvaluateVerbTrigger(DynamicStoryTellerDef.DynamicObjectStateTrigger trigger, Pawn pawn)
        {
            if (pawn == null || string.IsNullOrEmpty(trigger.defName))
            {
                return false;
            }

            if (MatchesVerb(pawn.CurJob?.verbToUse, trigger.defName))
            {
                return true;
            }

            if (MatchesVerb(pawn.equipment?.PrimaryEq?.PrimaryVerb, trigger.defName))
            {
                return true;
            }

            if (pawn.verbTracker != null)
            {
                foreach (Verb verb in pawn.verbTracker.AllVerbs)
                {
                    if (MatchesVerb(verb, trigger.defName))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool MatchesVerb(Verb verb, string defName)
        {
            if (verb == null || string.IsNullOrEmpty(defName))
            {
                return false;
            }

            if (NamesEqual(verb.GetType().Name, defName))
            {
                return true;
            }

            if (verb.verbProps != null)
            {
                if (!string.IsNullOrEmpty(verb.verbProps.label) && NamesEqual(verb.verbProps.label, defName))
                {
                    return true;
                }

                if (verb.verbProps.verbClass != null && NamesEqual(verb.verbProps.verbClass.Name, defName))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool EvaluateNeedTrigger(DynamicStoryTellerDef.DynamicObjectStateTrigger trigger, Pawn pawn)
        {
            if (pawn?.needs == null || string.IsNullOrEmpty(trigger.defName))
            {
                return false;
            }

            Need need = pawn.needs.AllNeeds?.FirstOrDefault(n => n?.def != null && NamesEqual(n.def.defName, trigger.defName));
            if (need == null)
            {
                return false;
            }

            float value = need.CurLevel;
            if (trigger.useThreshold && !Compare(value, trigger.threshold, trigger.comparison))
            {
                return false;
            }

            if (trigger.useUpperThreshold && value > trigger.upperThreshold)
            {
                return false;
            }

            return true;
        }

        private static bool EvaluateHediffTrigger(DynamicStoryTellerDef.DynamicObjectStateTrigger trigger, Pawn pawn)
        {
            if (pawn?.health?.hediffSet == null || string.IsNullOrEmpty(trigger.defName))
            {
                return false;
            }

            IEnumerable<Hediff> hediffs = pawn.health.hediffSet.hediffs.Where(h => h?.def != null && NamesEqual(h.def.defName, trigger.defName));
            foreach (Hediff hediff in hediffs)
            {
                float severity = hediff.Severity;
                if (trigger.useThreshold && !Compare(severity, trigger.threshold, trigger.comparison))
                {
                    continue;
                }

                if (trigger.useUpperThreshold && severity > trigger.upperThreshold)
                {
                    continue;
                }

                return true;
            }

            return false;
        }

        private static bool EvaluateThoughtTrigger(DynamicStoryTellerDef.DynamicObjectStateTrigger trigger, Pawn pawn)
        {
            if (pawn?.needs?.mood?.thoughts?.memories == null || string.IsNullOrEmpty(trigger.defName))
            {
                return false;
            }

            List<Thought_Memory> memories = pawn.needs.mood.thoughts.memories.Memories;
            if (memories == null)
            {
                return false;
            }

            foreach (Thought_Memory memory in memories)
            {
                if (memory?.def == null || !NamesEqual(memory.def.defName, trigger.defName))
                {
                    continue;
                }

                if (trigger.thoughtStageIndex >= 0 && memory.CurStageIndex != trigger.thoughtStageIndex)
                {
                    continue;
                }

                float moodOffset = memory.MoodOffset();
                if (trigger.useThreshold && !Compare(moodOffset, trigger.threshold, trigger.comparison))
                {
                    continue;
                }

                if (trigger.useUpperThreshold && moodOffset > trigger.upperThreshold)
                {
                    continue;
                }

                return true;
            }

            return false;
        }

        private static bool Compare(float value, float threshold, DynamicStoryTellerDef.DynamicObjectStateComparison comparison)
        {
            switch (comparison)
            {
                case DynamicStoryTellerDef.DynamicObjectStateComparison.Greater:
                    return value > threshold;
                case DynamicStoryTellerDef.DynamicObjectStateComparison.GreaterOrEqual:
                    return value >= threshold;
                case DynamicStoryTellerDef.DynamicObjectStateComparison.Less:
                    return value < threshold;
                case DynamicStoryTellerDef.DynamicObjectStateComparison.LessOrEqual:
                    return value <= threshold;
                case DynamicStoryTellerDef.DynamicObjectStateComparison.Equal:
                    return Math.Abs(value - threshold) < 1e-4f;
                case DynamicStoryTellerDef.DynamicObjectStateComparison.NotEqual:
                    return Math.Abs(value - threshold) >= 1e-4f;
                default:
                    return false;
            }
        }

        private static bool NamesEqual(string lhs, string rhs)
        {
            return !string.IsNullOrEmpty(lhs) && !string.IsNullOrEmpty(rhs) && string.Equals(lhs, rhs, StringComparison.OrdinalIgnoreCase);
        }

        private class AnimationCommand
        {
            public int TrackIndex;

            public string AnimationName;

            public bool Loop;

            public bool UseQueue;

            public float Delay;

            public float? MixDuration;

            public bool AttachIdleCompletion;

            public bool AttachReenableInteraction;
        }
    }
}
