using System;
using System.Collections.Generic;
using System.Linq;
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
        private static readonly FieldInfo HediffSetPawnField = AccessTools.Field(typeof(HediffSet), "pawn");
        private static readonly FieldInfo ThoughtHandlerPawnField = AccessTools.Field(typeof(ThoughtHandler), "pawn");
        private static readonly FieldInfo MemoryThoughtHandlerPawnField = AccessTools.Field(typeof(MemoryThoughtHandler), "pawn");
        private static readonly FieldInfo JobDriverCurToilField = AccessTools.Field(typeof(JobDriver), "curToil");
        private static readonly PropertyInfo JobDriverCurToilProperty = AccessTools.Property(typeof(JobDriver), "CurToil");

        private static Pawn GetPawn(object tracker, FieldInfo pawnField)
        {
            if (tracker == null)
            {
                return null;
            }

            if (pawnField != null)
            {
                return pawnField.GetValue(tracker) as Pawn;
            }

            return null;
        }

        private static Pawn GetPawn(Pawn_JobTracker tracker)
        {
            return GetPawn(tracker, PawnJobTrackerPawnField);
        }

        private static Pawn GetPawn(Pawn_NeedsTracker tracker)
        {
            return GetPawn(tracker, PawnNeedsTrackerPawnField);
        }

        private static Pawn GetPawn(HediffSet hediffSet)
        {
            return GetPawn(hediffSet, HediffSetPawnField);
        }

        private static Pawn GetPawn(ThoughtHandler handler)
        {
            return GetPawn(handler, ThoughtHandlerPawnField);
        }

        private static Pawn GetPawn(MemoryThoughtHandler handler)
        {
            return GetPawn(handler, MemoryThoughtHandlerPawnField);
        }

        private static Job GetCurrentJob(Pawn_JobTracker tracker)
        {
            return CurJobField?.GetValue(tracker) as Job;
        }

        private static JobDriver GetCurrentDriver(Pawn_JobTracker tracker)
        {
            return CurDriverField?.GetValue(tracker) as JobDriver;
        }

        private static Toil GetCurrentToil(JobDriver driver)
        {
            if (driver == null)
            {
                return null;
            }

            return JobDriverCurToilField?.GetValue(driver) as Toil
                ?? JobDriverCurToilProperty?.GetValue(driver) as Toil;
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
            Toil currentToil = GetCurrentToil(driver);
            string stageLabel = currentToil?.debugName ?? currentToil?.ToString();
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

        [HarmonyPatch]
        private static class Verb_TryStartCastOn_Patch
        {
            private static MethodInfo targetMethod;

            private static bool Prepare()
            {
                targetMethod = AccessTools.GetDeclaredMethods(typeof(Verb))
                    .Where(method => method.Name == nameof(Verb.TryStartCastOn))
                    .OrderByDescending(method => method.GetParameters().Length)
                    .FirstOrDefault();
                return targetMethod != null;
            }

            private static IEnumerable<MethodBase> TargetMethods()
            {
                yield return targetMethod;
            }

            private static void Postfix(Verb __instance, bool __result)
            {
                if (!__result)
                {
                    return;
                }

                Pawn casterPawn = __instance.CasterPawn;
                if (casterPawn == null)
                {
                    return;
                }

                DynamicPawnStateRegistry.NotifyVerbUsed(casterPawn, __instance);
            }
        }

        [HarmonyPatch(typeof(Pawn_NeedsTracker), "NeedsTrackerTickInterval")]
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

        [HarmonyPatch]
        private static class MemoryThoughtHandler_TryGainMemory_Patch
        {
            private static MethodInfo[] targetMethods;

            private static bool Prepare()
            {
                targetMethods = AccessTools.GetDeclaredMethods(typeof(MemoryThoughtHandler))
                    .Where(method => method.Name.IndexOf("TryGainMemory", StringComparison.Ordinal) >= 0)
                    .ToArray();
                return targetMethods.Length > 0;
            }

            private static IEnumerable<MethodBase> TargetMethods()
            {
                return targetMethods;
            }

            private static void Postfix(MemoryThoughtHandler __instance)
            {
                NotifyThoughtsChanged(__instance);
            }
        }

        [HarmonyPatch]
        private static class MemoryThoughtHandler_RemoveMemory_Patch
        {
            private static MethodInfo[] targetMethods;

            private static bool Prepare()
            {
                targetMethods = AccessTools.GetDeclaredMethods(typeof(MemoryThoughtHandler))
                    .Where(method => method.Name.IndexOf("RemoveMemory", StringComparison.Ordinal) >= 0)
                    .ToArray();
                return targetMethods.Length > 0;
            }

            private static IEnumerable<MethodBase> TargetMethods()
            {
                return targetMethods;
            }

            private static void Postfix(MemoryThoughtHandler __instance)
            {
                NotifyThoughtsChanged(__instance);
            }
        }

        [HarmonyPatch(typeof(ThoughtHandler), "ThoughtInterval")]
        private static class ThoughtHandler_ThoughtInterval_Patch
        {
            private static void Postfix(ThoughtHandler __instance)
            {
                NotifyThoughtsChanged(__instance);
            }
        }

        private static void NotifyThoughtsChanged(MemoryThoughtHandler handler)
        {
            Pawn pawn = GetPawn(handler);
            if (pawn == null)
            {
                return;
            }

            DynamicPawnStateRegistry.NotifyThoughtsChanged(pawn);
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
