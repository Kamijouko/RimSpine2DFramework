using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
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
        private static readonly FieldInfo PawnHealthTrackerPawnField = AccessTools.Field(typeof(Pawn_HealthTracker), "pawn");
        private static readonly FieldInfo HediffSetPawnField = AccessTools.Field(typeof(HediffSet), "pawn");
        private static readonly FieldInfo ThoughtHandlerPawnField = AccessTools.Field(typeof(ThoughtHandler), "pawn");
        private static readonly FieldInfo MemoryThoughtHandlerPawnField = AccessTools.Field(typeof(MemoryThoughtHandler), "pawn");
        private static readonly FieldInfo JobDriverCurToilField = AccessTools.Field(typeof(JobDriver), "curToil");
        private static readonly PropertyInfo JobDriverCurToilProperty = AccessTools.Property(typeof(JobDriver), "CurToil");
        private static readonly FieldInfo PawnRendererPawnField = AccessTools.Field(typeof(PawnRenderer), "pawn");
        private static readonly Type PawnRendererRenderCacheType = AccessTools.Inner(typeof(PawnRenderer), "RenderCache");
        private static readonly Type PawnRendererPawnCacheEntryType = PawnRendererRenderCacheType != null ? AccessTools.Inner(PawnRendererRenderCacheType, "PawnCacheEntry") : null;
        private static readonly FieldInfo PawnCacheEntryRendererField = PawnRendererPawnCacheEntryType != null ? AccessTools.Field(PawnRendererPawnCacheEntryType, "renderer") : null;
        private static readonly FieldInfo PawnCacheEntryPawnField = PawnRendererPawnCacheEntryType != null
            ? PawnRendererPawnCacheEntryType
                .GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                .FirstOrDefault(field => field.FieldType == typeof(Pawn))
            : null;

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

        private static Pawn GetPawn(Pawn_HealthTracker tracker)
        {
            return GetPawn(tracker, PawnHealthTrackerPawnField);
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

        private static Pawn GetPawn(PawnRenderer renderer)
        {
            if (renderer == null)
            {
                return null;
            }

            if (PawnRendererPawnField != null)
            {
                return PawnRendererPawnField.GetValue(renderer) as Pawn;
            }

            return null;
        }

        private static Pawn GetPawnFromCacheEntry(object cacheEntry)
        {
            if (cacheEntry == null)
            {
                return null;
            }

            if (PawnCacheEntryPawnField != null)
            {
                return PawnCacheEntryPawnField.GetValue(cacheEntry) as Pawn;
            }

            if (PawnCacheEntryRendererField != null)
            {
                PawnRenderer renderer = PawnCacheEntryRendererField.GetValue(cacheEntry) as PawnRenderer;
                return GetPawn(renderer);
            }

            return null;
        }

        private static Pawn GetPawn(Thing thing)
        {
            switch (thing)
            {
                case Pawn pawn:
                    return pawn;
                case Corpse corpse when corpse.InnerPawn != null:
                    return corpse.InnerPawn;
                default:
                    return null;
            }
        }

        private static bool ShouldHideVanillaPawn(Pawn pawn)
        {
            if (pawn == null)
            {
                return false;
            }

            DynamicPawnStateController controller = DynamicPawnStateRegistry.GetController(pawn);
            return controller?.HideVanillaPawn == true;
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

            private static bool Prefix(Verb __instance, ref bool __result, object[] __args)
            {
                if (__instance == null)
                {
                    return true;
                }

                if (DynamicPawnStateRegistry.IsVerbExecutionInProgress(__instance))
                {
                    return true;
                }

                Pawn casterPawn = __instance.CasterPawn;
                if (casterPawn == null)
                {
                    return true;
                }

                DynamicPawnStateController controller = DynamicPawnStateRegistry.GetController(casterPawn);
                if (controller == null || !controller.ShouldDelayVerbExecution(__instance))
                {
                    return true;
                }

                object[] argumentCopy = __args != null && __args.Length > 0 ? (object[])__args.Clone() : Array.Empty<object>();
                if (!DynamicPawnStateRegistry.TryQueueVerbCast(casterPawn, __instance, targetMethod, argumentCopy))
                {
                    return true;
                }

                DynamicPawnStateRegistry.NotifyVerbUsed(casterPawn, __instance);
                __result = false;
                return false;
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

        [HarmonyPatch(typeof(Pawn_CarryTracker), nameof(Pawn_CarryTracker.TryStartCarry), new[] { typeof(Thing) })]
        private static class PawnCarryTracker_TryStartCarry_Patch
        {
            private static void Postfix(bool __result, Thing item)
            {
                if (!__result)
                {
                    return;
                }

                Pawn pawn = GetPawn(item);
                if (pawn == null)
                {
                    return;
                }

                DynamicPawnStateRegistry.NotifyPawnStorageChanged(pawn);
            }
        }

        [HarmonyPatch]
        private static class PawnCarryTracker_TryDropCarriedThing_Patch
        {
            static MethodBase TargetMethod()
            {
                return AccessTools.Method(
                    typeof(Pawn_CarryTracker),
                    nameof(Pawn_CarryTracker.TryDropCarriedThing),
                    new Type[]
                    {
                typeof(IntVec3),
                typeof(ThingPlaceMode),
                typeof(Thing).MakeByRefType(),      // out Thing -> ref
                typeof(Action<Thing, int>)
                    }
                );
            }

            [HarmonyPostfix]
            private static void Postfix(IntVec3 dropLoc, ThingPlaceMode mode, ref Thing resultingThing, Action<Thing, int> placedAction, Pawn_CarryTracker __instance, bool __result)
            {
                if (!__result)
                {
                    return;
                }

                Thing droppedThing = resultingThing ?? __instance?.CarriedThing;
                Pawn pawn = GetPawn(droppedThing);
                if (pawn == null)
                {
                    return;
                }

                DynamicPawnStateRegistry.NotifyPawnStorageChanged(pawn);
            }
        }

        [HarmonyPatch]
        private static class ThingOwner_TryDrop_Patch
        {
            private static MethodBase[] targetMethods;

            private static bool Prepare()
            {
                targetMethods = AccessTools.GetDeclaredMethods(typeof(ThingOwner))
                    .Where(method => method.Name == nameof(ThingOwner.TryDrop))
                    .Where(method =>
                    {
                        ParameterInfo[] parameters = method.GetParameters();
                        if (parameters.Length == 0)
                        {
                            return false;
                        }

                        if (parameters[0].ParameterType != typeof(Thing))
                        {
                            return false;
                        }

                        return parameters.Any(parameter => parameter.ParameterType.IsByRef
                            && parameter.ParameterType.GetElementType() == typeof(Thing));
                    })
                    .Cast<MethodBase>()
                    .ToArray();

                return targetMethods.Length > 0;
            }

            private static IEnumerable<MethodBase> TargetMethods()
            {
                return targetMethods;
            }

            private static void Postfix(ThingOwner __instance, bool __result, MethodBase __originalMethod, object[] __args)
            {
                if (!__result)
                {
                    return;
                }

                if (!(__instance?.Owner is CompTransporter))
                {
                    return;
                }

                Thing droppedThing = GetDroppedThing(__originalMethod, __args);
                if (droppedThing == null)
                {
                    return;
                }

                NotifyTransporterPawnStorageChanged(droppedThing);
            }

            private static Thing GetDroppedThing(MethodBase originalMethod, IReadOnlyList<object> args)
            {
                if (originalMethod == null || args == null)
                {
                    return null;
                }

                ParameterInfo[] parameters = originalMethod.GetParameters();
                Thing inputThing = null;
                Thing resultingThing = null;

                for (int i = 0; i < parameters.Length && i < args.Count; i++)
                {
                    ParameterInfo parameter = parameters[i];
                    object argument = args[i];

                    if (argument is Thing argumentThing)
                    {
                        if (parameter.ParameterType == typeof(Thing) && inputThing == null)
                        {
                            inputThing = argumentThing;
                            continue;
                        }

                        if (parameter.ParameterType.IsByRef && parameter.ParameterType.GetElementType() == typeof(Thing))
                        {
                            resultingThing = argumentThing;
                        }
                    }
                }

                return resultingThing ?? inputThing;
            }
        }

        private static void NotifyTransporterPawnStorageChanged(Thing thing)
        {
            Pawn pawn = GetPawn(thing);
            if (pawn == null)
            {
                return;
            }

            DynamicPawnStateRegistry.NotifyPawnStorageChanged(pawn);
        }

        [HarmonyPatch(typeof(CompTransporter), nameof(CompTransporter.Notify_ThingAdded))]
        private static class CompTransporter_NotifyThingAdded_Patch
        {
            private static void Postfix(Thing t)
            {
                NotifyTransporterPawnStorageChanged(t);
            }
        }

        [HarmonyPatch(typeof(CompTransporter), nameof(CompTransporter.Notify_ThingRemoved))]
        private static class CompTransporter_NotifyThingRemoved_Patch
        {
            private static void Postfix(Thing t)
            {
                NotifyTransporterPawnStorageChanged(t);
            }
        }

        [HarmonyPatch(typeof(CompTransporter), nameof(CompTransporter.Notify_ThingAddedAndMergedWith))]
        private static class CompTransporter_NotifyThingAddedAndMergedWith_Patch
        {
            private static void Postfix(Thing t)
            {
                NotifyTransporterPawnStorageChanged(t);
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

        [HarmonyPatch(typeof(PawnRenderer), "RenderPawnAt")]
        private static class PawnRenderer_RenderPawnAt_Patch
        {
            private static bool Prefix(PawnRenderer __instance)
            {
                Pawn pawn = GetPawn(__instance);
                if (pawn == null)
                {
                    return true;
                }

                return !ShouldHideVanillaPawn(pawn);
            }
        }

        [HarmonyPatch]
        private static class PawnRenderer_RenderCache_PawnCacheEntry_RenderPawn_Patch
        {
            private static MethodInfo targetMethod;

            private static bool Prepare()
            {
                if (PawnRendererPawnCacheEntryType == null)
                {
                    return false;
                }

                targetMethod = AccessTools.Method(PawnRendererPawnCacheEntryType, "RenderPawn");
                return targetMethod != null;
            }

            private static MethodBase TargetMethod()
            {
                return targetMethod;
            }

            private static bool Prefix(object __instance)
            {
                Pawn pawn = GetPawnFromCacheEntry(__instance);
                if (pawn == null)
                {
                    return true;
                }

                return !ShouldHideVanillaPawn(pawn);
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

        [HarmonyPatch(typeof(Pawn_HealthTracker), nameof(Pawn_HealthTracker.SetDead))]
        private static class PawnHealthTracker_SetDead_Patch
        {
            private static void Postfix(Pawn_HealthTracker __instance)
            {
                Pawn pawn = GetPawn(__instance);
                if (pawn == null)
                {
                    return;
                }

                DynamicPawnStateRegistry.NotifyPawnDied(pawn);
            }
        }

        [HarmonyPatch(typeof(ResurrectionUtility), nameof(ResurrectionUtility.TryResurrect))]
        private static class ResurrectionUtility_TryResurrect_Patch
        {
            private static void Postfix(bool __result, Pawn pawn, ResurrectionParams parms = null)
            {
                if (!__result || pawn == null)
                {
                    return;
                }
                DynamicPawnStateRegistry.NotifyPawnResurrected(pawn);
            }
        }

        [HarmonyPatch]
        private static class TargetingParameters_CanTarget_Patch
        {
            [HarmonyTargetMethods]
            private static IEnumerable<MethodBase> TargetMethods()
            {
                Type type = typeof(TargetingParameters);
                string methodName = nameof(TargetingParameters.CanTarget);

                Type[] supportedFirstParameterTypes =
                {
                    typeof(Thing),
                    typeof(TargetInfo),
                    typeof(LocalTargetInfo),
                    typeof(GlobalTargetInfo)
                };

                List<MethodBase> methods = AccessTools
                    .GetDeclaredMethods(type)
                    .Where(method => method.Name == methodName)
                    .Where(method => method.ReturnType == typeof(bool))
                    .Where(method =>
                    {
                        ParameterInfo[] parameters = method.GetParameters();
                        if (parameters.Length == 0)
                        {
                            return false;
                        }

                        Type firstParameterType = parameters[0].ParameterType;
                        if (firstParameterType.IsByRef)
                        {
                            firstParameterType = firstParameterType.GetElementType();
                        }
                        return supportedFirstParameterTypes.Any(supportedType => supportedType.IsAssignableFrom(firstParameterType));
                    })
                    .Cast<MethodBase>()
                    .ToList();

                if (methods.Count == 0)
                {
                    Log.Warning("[RimSpine2DFramework] Failed to find TargetingParameters.CanTarget overloads to patch. Some dynamic pawn selection features may not work as expected.");
                }

                return methods;
            }

            private static bool Prefix(object __0, ref bool __result)
            {
                Pawn pawn = null;

                switch (__0)
                {
                    case Thing thing:
                        pawn = GetPawn(thing);
                        break;
                    case LocalTargetInfo localTarget:
                        pawn = GetPawn(localTarget.Thing);
                        break;
                    case GlobalTargetInfo globalTarget:
                        pawn = GetPawn(globalTarget.Thing);
                        break;
                }

                if (pawn == null)
                {
                    return true;
                }

                if (!DynamicPawnSelectionWrapper.ShouldBlock(pawn))
                {
                    return true;
                }

                __result = false;
                return false;
            }
        }
    }
}
