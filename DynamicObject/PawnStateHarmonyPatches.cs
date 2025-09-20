using System;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace RimSpine2DFramework
{
    [HarmonyPatch]
    internal static class PawnStateHarmonyPatches
    {
        private static readonly FieldInfo CurJobField = AccessTools.Field(typeof(Pawn_JobTracker), "curJob");
        private static readonly FieldInfo CurDriverField = AccessTools.Field(typeof(Pawn_JobTracker), "curDriver");
        private static readonly FieldInfo PawnJobTrackerPawnField = AccessTools.Field(typeof(Pawn_JobTracker), "pawn");
        private static readonly FieldInfo PawnNeedsTrackerPawnField = AccessTools.Field(typeof(Pawn_NeedsTracker), "pawn");
        private static readonly FieldInfo PawnVerbTrackerPawnField = AccessTools.Field(typeof(Pawn_VerbTracker), "pawn");
        private static readonly FieldInfo HediffSetPawnField = AccessTools.Field(typeof(HediffSet), "pawn");
        private static readonly FieldInfo ThoughtHandlerPawnField = AccessTools.Field(typeof(ThoughtHandler), "pawn");

        private static Pawn GetPawn(Pawn_JobTracker tracker)
        {
            return tracker != null ? PawnJobTrackerPawnField?.GetValue(tracker) as Pawn : null;
        }

        private static Pawn GetPawn(Pawn_NeedsTracker tracker)
        {
            return tracker != null ? PawnNeedsTrackerPawnField?.GetValue(tracker) as Pawn : null;
        }

        private static Pawn GetPawn(Pawn_VerbTracker tracker)
        {
            return tracker != null ? PawnVerbTrackerPawnField?.GetValue(tracker) as Pawn : null;
        }

        private static Pawn GetPawn(HediffSet hediffSet)
        {
            return hediffSet != null ? HediffSetPawnField?.GetValue(hediffSet) as Pawn : null;
        }

        private static Pawn GetPawn(ThoughtHandler handler)
        {
            return handler != null ? ThoughtHandlerPawnField?.GetValue(handler) as Pawn : null;
        }

        private static Job GetCurrentJob(Pawn_JobTracker tracker)
        {
            return CurJobField?.GetValue(tracker) as Job;
        }

        private static JobDriver GetCurrentDriver(Pawn_JobTracker tracker)
        {
            return CurDriverField?.GetValue(tracker) as JobDriver;
        }

        private static void NotifyJobUpdate(Pawn_JobTracker tracker, Job jobOverride = null)
        {
            Pawn pawn = GetPawn(tracker);
            if (pawn == null)
            {
                return;
            }

            Job job = jobOverride ?? GetCurrentJob(tracker);
            JobDriver driver = GetCurrentDriver(tracker);
            int stageIndex = driver?.CurToilIndex ?? -1;
            string stageLabel = driver?.CurToil?.debugName ?? driver?.CurToil?.ToString();
            DynamicPawnStateRegistry.NotifyJobUpdate(pawn, job, stageIndex, stageLabel);
        }

        [HarmonyPatch(typeof(Pawn_JobTracker), "StartJob")]
        private static class PawnJobTracker_StartJob_Patch
        {
            private static void Postfix(Pawn_JobTracker __instance, Job newJob)
            {
                NotifyJobUpdate(__instance, newJob);
            }
        }

        [HarmonyPatch(typeof(Pawn_JobTracker), "EndCurrentJob")]
        private static class PawnJobTracker_EndCurrentJob_Patch
        {
            private static void Postfix(Pawn_JobTracker __instance)
            {
                Pawn pawn = GetPawn(__instance);
                if (pawn == null)
                {
                    return;
                }

                DynamicPawnStateRegistry.NotifyJobEnded(pawn);
            }
        }

        [HarmonyPatch(typeof(Pawn_JobTracker), "JobTrackerTick")]
        private static class PawnJobTracker_JobTrackerTick_Patch
        {
            private static void Postfix(Pawn_JobTracker __instance)
            {
                NotifyJobUpdate(__instance);
            }
        }

        [HarmonyPatch(typeof(Pawn_VerbTracker), "VerbsTick")]
        private static class PawnVerbTracker_VerbsTick_Patch
        {
            private static void Postfix(Pawn_VerbTracker __instance)
            {
                Pawn pawn = GetPawn(__instance);
                if (pawn == null)
                {
                    return;
                }

                Verb verb = __instance.PrimaryVerb;
                DynamicPawnStateRegistry.NotifyVerbUsed(pawn, verb);
            }
        }

        [HarmonyPatch(typeof(Pawn_NeedsTracker), "NeedIntervalTick")]
        private static class PawnNeedsTracker_NeedIntervalTick_Patch
        {
            private static void Postfix(Pawn_NeedsTracker __instance)
            {
                Pawn pawn = GetPawn(__instance);
                if (pawn == null)
                {
                    return;
                }

                DynamicPawnStateRegistry.NotifyNeedsChanged(pawn);
            }
        }

        [HarmonyPatch(typeof(HediffSet), "DirtyCache")]
        private static class HediffSet_DirtyCache_Patch
        {
            private static void Postfix(HediffSet __instance)
            {
                Pawn pawn = GetPawn(__instance);
                if (pawn == null)
                {
                    return;
                }

                DynamicPawnStateRegistry.NotifyHediffsChanged(pawn);
            }
        }

        [HarmonyPatch(typeof(ThoughtHandler), "TryGainMemory", new Type[] { typeof(Thought_Memory), typeof(Pawn), typeof(Precept) })]
        private static class ThoughtHandler_TryGainMemory_Patch
        {
            private static bool Prepare()
            {
                return AccessTools.Method(typeof(ThoughtHandler), "TryGainMemory", new Type[] { typeof(Thought_Memory), typeof(Pawn), typeof(Precept) }) != null;
            }

            private static void Postfix(ThoughtHandler __instance)
            {
                NotifyThoughtsChanged(__instance);
            }
        }

        [HarmonyPatch(typeof(ThoughtHandler), "TryGainMemory", new Type[] { typeof(Thought_Memory), typeof(Pawn), typeof(Precept), typeof(bool), typeof(bool) })]
        private static class ThoughtHandler_TryGainMemoryWithFlags_Patch
        {
            private static bool Prepare()
            {
                return AccessTools.Method(typeof(ThoughtHandler), "TryGainMemory", new Type[] { typeof(Thought_Memory), typeof(Pawn), typeof(Precept), typeof(bool), typeof(bool) }) != null;
            }

            private static void Postfix(ThoughtHandler __instance)
            {
                NotifyThoughtsChanged(__instance);
            }
        }

        [HarmonyPatch(typeof(ThoughtHandler), "RemoveMemory")]
        private static class ThoughtHandler_RemoveMemory_Patch
        {
            private static void Postfix(ThoughtHandler __instance)
            {
                NotifyThoughtsChanged(__instance);
            }
        }

        [HarmonyPatch(typeof(ThoughtHandler), "ThoughtIntervalTick")]
        private static class ThoughtHandler_ThoughtIntervalTick_Patch
        {
            private static void Postfix(ThoughtHandler __instance)
            {
                NotifyThoughtsChanged(__instance);
            }
        }

        private static void NotifyThoughtsChanged(ThoughtHandler handler)
        {
            Pawn pawn = GetPawn(handler);
            if (pawn == null)
            {
                return;
            }

            DynamicPawnStateRegistry.NotifyThoughtsChanged(pawn);
        }
    }
}
