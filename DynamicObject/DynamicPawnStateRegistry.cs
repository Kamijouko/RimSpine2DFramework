using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace RimSpine2DFramework
{
    public static class DynamicPawnStateRegistry
    {
        private static readonly Dictionary<string, List<DynamicPawnStateMachineDef>> StateMachinesByObject = new Dictionary<string, List<DynamicPawnStateMachineDef>>(StringComparer.OrdinalIgnoreCase);

        private static readonly Dictionary<Pawn, DynamicPawnStateController> ControllersByPawn = new Dictionary<Pawn, DynamicPawnStateController>();

        private static readonly Dictionary<DynamicObjectInstance, DynamicPawnStateController> ControllersByInstance = new Dictionary<DynamicObjectInstance, DynamicPawnStateController>();

        public static IReadOnlyDictionary<Pawn, DynamicPawnStateController> ActiveControllers => ControllersByPawn;

        public static void ReloadDefinitions()
        {
            StateMachinesByObject.Clear();
            List<DynamicPawnStateMachineDef> defs = DefDatabase<DynamicPawnStateMachineDef>.AllDefsListForReading;
            if (defs.NullOrEmpty())
            {
                return;
            }

            foreach (DynamicPawnStateMachineDef def in defs)
            {
                if (def == null)
                {
                    continue;
                }

                if (def.targetDynamicObject == null)
                {
                    Log.Error($"[RimSpine2D] DynamicPawnStateMachineDef '{def.defName}' is missing a targetDynamicObject reference.");
                    continue;
                }

                if (!StateMachinesByObject.TryGetValue(def.targetDynamicObject.defName, out List<DynamicPawnStateMachineDef> list))
                {
                    list = new List<DynamicPawnStateMachineDef>();
                    StateMachinesByObject.Add(def.targetDynamicObject.defName, list);
                }

                list.Add(def);
                ValidateDefinition(def);
            }

            foreach (List<DynamicPawnStateMachineDef> list in StateMachinesByObject.Values)
            {
                list.Sort((a, b) => b.priority.CompareTo(a.priority));
            }

            int totalStates = StateMachinesByObject.Sum(kv => kv.Value.Count);
            if (totalStates > 0)
            {
                Log.Message($"[RimSpine2D] Loaded {totalStates} pawn state machine definitions for {StateMachinesByObject.Count} dynamic objects.");
            }
        }

        private static void ValidateDefinition(DynamicPawnStateMachineDef def)
        {
            if (def.states.NullOrEmpty())
            {
                Log.Warning($"[RimSpine2D] DynamicPawnStateMachineDef '{def.defName}' has no state entries defined.");
                return;
            }

            HashSet<string> stateIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (DynamicPawnStateMachineDef.PawnAnimationState state in def.states)
            {
                if (state == null)
                {
                    Log.Warning($"[RimSpine2D] DynamicPawnStateMachineDef '{def.defName}' contains a null state entry.");
                    continue;
                }

                if (state.stateId.NullOrEmpty())
                {
                    Log.Warning($"[RimSpine2D] DynamicPawnStateMachineDef '{def.defName}' defines a state without stateId.");
                }
                else if (!stateIds.Add(state.stateId))
                {
                    Log.Warning($"[RimSpine2D] DynamicPawnStateMachineDef '{def.defName}' contains duplicate stateId '{state.stateId}'.");
                }

                if (state.animationName.NullOrEmpty())
                {
                    Log.Warning($"[RimSpine2D] DynamicPawnStateMachineDef '{def.defName}' state '{state.stateId}' is missing animationName.");
                }

                if (state.triggers.NullOrEmpty())
                {
                    continue;
                }

                foreach (DynamicPawnStateMachineDef.PawnStateTrigger trigger in state.triggers)
                {
                    if (trigger == null)
                    {
                        Log.Warning($"[RimSpine2D] DynamicPawnStateMachineDef '{def.defName}' state '{state.stateId}' contains a null trigger.");
                        continue;
                    }

                    switch (trigger.source)
                    {
                        case DynamicPawnStateMachineDef.PawnStateTriggerSource.Job:
                        case DynamicPawnStateMachineDef.PawnStateTriggerSource.Need:
                        case DynamicPawnStateMachineDef.PawnStateTriggerSource.Hediff:
                        case DynamicPawnStateMachineDef.PawnStateTriggerSource.Thought:
                        case DynamicPawnStateMachineDef.PawnStateTriggerSource.Verb:
                        {
                            string resolvedDef = trigger.def?.defName ?? trigger.defName;
                            if (resolvedDef.NullOrEmpty())
                            {
                                Log.Warning($"[RimSpine2D] DynamicPawnStateMachineDef '{def.defName}' state '{state.stateId}' defines a trigger without defName.");
                            }

                            break;
                        }

                        case DynamicPawnStateMachineDef.PawnStateTriggerSource.Movement:
                        {
                            if (!trigger.isMoving.HasValue)
                            {
                                Log.Warning($"[RimSpine2D] DynamicPawnStateMachineDef '{def.defName}' state '{state.stateId}' uses a Movement trigger without specifying isMoving. Set isMoving=true or isMoving=false.");
                            }

                            break;
                        }
                    }
                }
            }
        }

        public static bool TryBind(DynamicObjectInstance instance, Pawn pawn)
        {
            if (instance?.key == null || pawn == null)
            {
                return false;
            }

            if (!StateMachinesByObject.TryGetValue(instance.key.defName, out List<DynamicPawnStateMachineDef> defs) || defs.Count == 0)
            {
                return false;
            }

            DynamicPawnStateMachineDef match = null;
            foreach (DynamicPawnStateMachineDef def in defs)
            {
                if (def.binding == null || def.binding.Matches(pawn))
                {
                    match = def;
                    break;
                }
            }

            if (match == null)
            {
                return false;
            }

            if (ControllersByPawn.TryGetValue(pawn, out DynamicPawnStateController existing))
            {
                if (existing.Instance == instance && existing.Definition == match)
                {
                    return true;
                }

                existing.Dispose();
            }

            DynamicPawnStateController controller = new DynamicPawnStateController(instance, pawn, match);
            ControllersByPawn[pawn] = controller;
            ControllersByInstance[instance] = controller;
            controller.RefreshNow();
            return true;
        }

        public static void Unbind(DynamicObjectInstance instance)
        {
            if (instance == null)
            {
                return;
            }

            if (ControllersByInstance.TryGetValue(instance, out DynamicPawnStateController controller))
            {
                ControllersByInstance.Remove(instance);
                if (controller.Pawn != null && ControllersByPawn.TryGetValue(controller.Pawn, out DynamicPawnStateController existing) && existing == controller)
                {
                    ControllersByPawn.Remove(controller.Pawn);
                }

                controller.Dispose();
            }
        }

        internal static void NotifyControllerDisposed(DynamicPawnStateController controller)
        {
            if (controller == null)
            {
                return;
            }

            ControllersByInstance.Remove(controller.Instance);
            if (controller.Pawn != null && ControllersByPawn.TryGetValue(controller.Pawn, out DynamicPawnStateController existing) && existing == controller)
            {
                ControllersByPawn.Remove(controller.Pawn);
            }
        }

        public static DynamicPawnStateController GetController(Pawn pawn)
        {
            if (pawn == null)
            {
                return null;
            }

            ControllersByPawn.TryGetValue(pawn, out DynamicPawnStateController controller);
            return controller;
        }

        public static void NotifyJobUpdate(Pawn pawn, Job job, int stageIndex, string stageLabel)
        {
            DynamicPawnStateController controller = GetController(pawn);
            controller?.NotifyJobUpdated(job, stageIndex, stageLabel);
        }

        public static void NotifyJobEnded(Pawn pawn)
        {
            DynamicPawnStateController controller = GetController(pawn);
            controller?.NotifyJobEnded();
        }

        public static void NotifyVerbUsed(Pawn pawn, Verb verb)
        {
            DynamicPawnStateController controller = GetController(pawn);
            controller?.NotifyVerbUsed(verb);
        }

        public static void NotifyNeedsChanged(Pawn pawn)
        {
            DynamicPawnStateController controller = GetController(pawn);
            controller?.NotifyNeedsChanged();
        }

        public static void NotifyHediffsChanged(Pawn pawn)
        {
            DynamicPawnStateController controller = GetController(pawn);
            controller?.NotifyHediffsChanged();
        }

        public static void NotifyThoughtsChanged(Pawn pawn)
        {
            DynamicPawnStateController controller = GetController(pawn);
            controller?.NotifyThoughtsChanged();
        }
    }
}
