using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace RimSpine2DFramework
{
    [HarmonyPatch]
    internal static class AlienRaceBodyGraphicPatch
    {
        private const string AlienRacePackageId = "erdelf.humanoidalienraces";

        private static bool Prepare()
        {
            return ModsConfig.ActiveModsInLoadOrder.Any(mod => mod?.PackageIdPlayerFacing != null && mod.packageIdLowerCase == AlienRacePackageId);
        }

        private static MethodBase TargetMethod()
        {
            Type patchType = AccessTools.TypeByName("AlienRace.AlienRenderTreePatches");
            if (patchType == null)
            {
                return null;
            }

            return AccessTools.Method(patchType, "BodyGraphicForPrefix");
        }

        [HarmonyPriority(Priority.First)]
        private static bool Prefix(PawnRenderNode_Body __instance, Pawn pawn, [HarmonyArgument(2)] ref Graphic bodyGraphic, ref bool __result)
        {
            if (__instance is Chibi_PawnRenderNode_Body chibiNode && chibiNode.TryResolveChibiGraphic(pawn, out Graphic graphic))
            {
                bodyGraphic = graphic;
                __result = false;
                return false;
            }

            return true;
        }
    }
}
