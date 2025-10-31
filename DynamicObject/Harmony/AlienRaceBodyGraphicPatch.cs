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
            return ModsConfig.ActiveModsInLoadOrder.Any(mod => mod?.PackageIdPlayerFacing != null && mod.PackageIdLowerCase == AlienRacePackageId);
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
        private static bool Prefix(object __instance, Pawn pawn, ref Graphic __result)
        {
            if (__instance is Chibi_PawnRenderNode_Body chibiNode && chibiNode.TryResolveChibiGraphic(pawn, out Graphic graphic))
            {
                __result = graphic;
                return false;
            }

            return true;
        }
    }
}
