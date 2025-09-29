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
        private static readonly Type PawnVerbTrackerType = ResolvePawnVerbTrackerType();
        private static readonly FieldInfo PawnVerbTrackerPawnField = PawnVerbTrackerType != null
            ? AccessTools.Field(PawnVerbTrackerType, "pawn")
            : null;
        private static readonly FieldInfo PawnVerbTrackerDirectOwnerField = PawnVerbTrackerType != null
            ? AccessTools.Field(PawnVerbTrackerType, "directOwner")
            : null;
        private static readonly PropertyInfo PawnVerbTrackerPrimaryVerbProperty = PawnVerbTrackerType != null
            ? AccessTools.Property(PawnVerbTrackerType, "PrimaryVerb")
            : null;
        private static readonly FieldInfo PawnVerbTrackerPrimaryVerbField = PawnVerbTrackerType != null
            ? AccessTools.Field(PawnVerbTrackerType, "primaryVerb")
            : null;
        private static readonly FieldInfo HediffSetPawnField = AccessTools.Field(typeof(HediffSet), "pawn");
        private static readonly FieldInfo ThoughtHandlerPawnField = AccessTools.Field(typeof(ThoughtHandler), "pawn");
        private static readonly FieldInfo MemoryThoughtHandlerPawnField = AccessTools.Field(typeof(MemoryThoughtHandler), "pawn");
        private static readonly FieldInfo JobDriverCurToilField = AccessTools.Field(typeof(JobDriver), "curToil");
        private static readonly PropertyInfo JobDriverCurToilProperty = AccessTools.Property(typeof(JobDriver), "CurToil");

        private static Type ResolvePawnVerbTrackerType()
        {
            return AccessTools.TypeByName("VerbTracker")
                   ?? AccessTools.TypeByName("Verse.VerbTracker")
                   ?? AccessTools.TypeByName("RimWorld.VerbTracker")
                   ?? AccessTools.TypeByName("PawnVerbsTracker")
                   ?? AccessTools.TypeByName("Verse.PawnVerbsTracker")
                   ?? AccessTools.TypeByName("RimWorld.PawnVerbsTracker")
                   ?? AccessTools.TypeByName("Pawn_VerbTracker")
                   ?? AccessTools.TypeByName("Verse.Pawn_VerbTracker")
                   ?? AccessTools.TypeByName("RimWorld.Pawn_VerbTracker");
        }

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

            object owner = PawnVerbTrackerDirectOwnerField?.GetValue(tracker);
            if (owner == null)
            {
                return null;
            }

            Pawn pawn = owner as Pawn;
            if (pawn != null)
            {
                return pawn;
            }

            return (owner as IVerbOwner)?.ConstantCaster as Pawn;
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

        private static Verb GetPrimaryVerb(object verbTracker)
        {
            if (verbTracker == null)
            {
                return null;
            }

            if (PawnVerbTrackerPrimaryVerbProperty != null)
            {
                return PawnVerbTrackerPrimaryVerbProperty.GetValue(verbTracker) as Verb;
            }

            return PawnVerbTrackerPrimaryVerbField?.GetValue(verbTracker) as Verb;
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
        private static class PawnVerbTracker_VerbsTick_Patch
        {
            private static MethodBase TargetMethod()
            {
                return PawnVerbTrackerType != null ? AccessTools.Method(PawnVerbTrackerType, "VerbsTick") : null;
            }

            private static void Postfix(object __instance)
            {
                Pawn pawn = GetPawn(__instance, PawnVerbTrackerPawnField);
                if (pawn == null)
                {
                    return;
                }

                Verb verb = GetPrimaryVerb(__instance);
                DynamicPawnStateRegistry.NotifyVerbUsed(pawn, verb);
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
