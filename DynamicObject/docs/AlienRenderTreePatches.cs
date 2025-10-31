using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace AlienRace
{
	// Token: 0x02000008 RID: 8
	public static class AlienRenderTreePatches
	{
		// Token: 0x06000018 RID: 24 RVA: 0x00003B9C File Offset: 0x00001D9C
		public static void HarmonyInit(AlienHarmony harmony)
		{
			harmony.Patch(AccessTools.Method(typeof(HumanlikeMeshPoolUtility), "GetHumanlikeBodySetForPawn", null, null), new HarmonyMethod(AlienRenderTreePatches.patchType, "GetHumanlikeBodySetForPawnPrefix", null), null, null, null);
			harmony.Patch(AccessTools.Method(typeof(HumanlikeMeshPoolUtility), "GetHumanlikeHeadSetForPawn", null, null), new HarmonyMethod(AlienRenderTreePatches.patchType, "GetHumanlikeHeadSetForPawnPrefix", null), null, null, null);
			harmony.Patch(AccessTools.Method(typeof(HumanlikeMeshPoolUtility), "GetHumanlikeHairSetForPawn", null, null), new HarmonyMethod(AlienRenderTreePatches.patchType, "GetHumanlikeHeadSetForPawnPrefix", null), null, null, null);
			harmony.Patch(AccessTools.Method(typeof(HumanlikeMeshPoolUtility), "GetHumanlikeBeardSetForPawn", null, null), new HarmonyMethod(AlienRenderTreePatches.patchType, "GetHumanlikeHeadSetForPawnPrefix", null), null, null, null);
			harmony.Patch(AccessTools.Method(typeof(PawnRenderNode), "GetMesh", null, null), null, null, new HarmonyMethod(AlienRenderTreePatches.patchType, "RenderNodeGetMeshTranspiler", null), null);
			harmony.Patch(AccessTools.Method(typeof(PawnRenderTree), "TrySetupGraphIfNeeded", null, null), new HarmonyMethod(AlienRenderTreePatches.patchType, "TrySetupGraphIfNeededPrefix", null), null, null, null);
			harmony.Patch(AccessTools.Method(typeof(PawnRenderTree), "EnsureInitialized", null, null), null, new HarmonyMethod(AlienRenderTreePatches.patchType, "PawnRenderTreeEnsureInitializedPostfix", null), null, null);
			harmony.Patch(AccessTools.Method(typeof(PawnRenderNode_Body), "GraphicFor", null, null), new HarmonyMethod(AlienRenderTreePatches.patchType, "BodyGraphicForPrefix", null), null, null, null);
			harmony.Patch(AccessTools.Method(typeof(PawnRenderNode_Head), "GraphicFor", null, null), new HarmonyMethod(AlienRenderTreePatches.patchType, "HeadGraphicForPrefix", null), null, null, null);
			harmony.Patch(AccessTools.Method(typeof(PawnRenderNode_Stump), "GraphicFor", null, null), null, null, new HarmonyMethod(AlienRenderTreePatches.patchType, "StumpGraphicForTranspiler", null), null);
			harmony.Patch(AccessTools.Method(typeof(HairDef), "GraphicFor", null, null), null, null, new HarmonyMethod(AlienRenderTreePatches.patchType, "HairDefGraphicForTranspiler", null), null);
			harmony.Patch(AccessTools.Method(typeof(TattooDef), "GraphicFor", null, null), null, null, new HarmonyMethod(AlienRenderTreePatches.patchType, "TattooDefGraphicForTranspiler", null), null);
			harmony.Patch(AccessTools.Method(typeof(BeardDef), "GraphicFor", null, null), null, null, new HarmonyMethod(AlienRenderTreePatches.patchType, "BeardDefGraphicForTranspiler", null), null);
		}

		// Token: 0x06000019 RID: 25 RVA: 0x00003E1C File Offset: 0x0000201C
		public static AlienRenderTreePatches.PawnRenderResolveData RegenerateResolveData(Pawn pawn)
		{
			AlienRenderTreePatches.PawnRenderResolveData pawnRenderResolveData = AlienRenderTreePatches.pawnRenderResolveData;
			if (((pawnRenderResolveData != null) ? pawnRenderResolveData.pawn : null) == pawn)
			{
				return AlienRenderTreePatches.pawnRenderResolveData;
			}
			return AlienRenderTreePatches.pawnRenderResolveData = new AlienRenderTreePatches.PawnRenderResolveData
			{
				pawn = pawn,
				alienProps = (pawn.def as ThingDef_AlienRace),
				alienComp = pawn.GetComp<AlienPartGenerator.AlienComp>(),
				lsaa = (pawn.ageTracker.CurLifeStageRace as LifeStageAgeAlien),
				sharedIndex = 0
			};
		}

		// Token: 0x0600001A RID: 26 RVA: 0x00003E94 File Offset: 0x00002094
		public static bool IsStatuePawn(Pawn pawn)
		{
			return pawn.Drawer.renderer.StatueColor != null;
		}

		// Token: 0x0600001B RID: 27 RVA: 0x00003EBC File Offset: 0x000020BC
		public static Color CheckOverrideColor(Pawn pawn, Color color)
		{
			return pawn.Drawer.renderer.StatueColor.GetValueOrDefault(color);
		}

		// Token: 0x0600001C RID: 28 RVA: 0x00003EE2 File Offset: 0x000020E2
		public static Shader CheckMaskShader(string texPath, Shader shader, bool pathCheckOverride = false)
		{
			if (shader.SupportsMaskTex() || (!pathCheckOverride && !(ContentFinder<Texture2D>.Get(texPath + "_northm", false) != null)))
			{
				return shader;
			}
			return ShaderDatabase.CutoutComplex;
		}

		// Token: 0x0600001D RID: 29 RVA: 0x00003F10 File Offset: 0x00002110
		public static void TrySetupGraphIfNeededPrefix(PawnRenderTree __instance)
		{
			if (__instance.Resolved)
			{
				return;
			}
			Pawn alien = __instance.pawn;
			ThingDef_AlienRace alienProps = alien.def as ThingDef_AlienRace;
			if (alienProps != null && alien.story != null)
			{
				AlienRenderTreePatches.RegenerateResolveData(alien);
				AlienPartGenerator.AlienComp alienComp = AlienRenderTreePatches.pawnRenderResolveData.alienComp;
				if (alienComp != null)
				{
					if (alienComp.fixGenderPostSpawn)
					{
						Info modExtension = alien.kindDef.GetModExtension<Info>();
						float? maleGenderProbability = new float?((modExtension != null) ? modExtension.maleGenderProbability : alienProps.alienRace.generalSettings.maleGenderProbability);
						Pawn pawn = __instance.pawn;
						float value = Rand.Value;
						float? num = maleGenderProbability;
						pawn.gender = (((value >= num.GetValueOrDefault()) & (num != null)) ? Gender.Female : Gender.Male);
						__instance.pawn.Name = PawnBioAndNameGenerator.GeneratePawnName(__instance.pawn, NameStyle.Full, null, false, null);
						alienComp.fixGenderPostSpawn = false;
					}
					LifeStageAgeAlien lsaa = AlienRenderTreePatches.pawnRenderResolveData.lsaa;
					if (alien.gender == Gender.Female)
					{
						alienComp.customDrawSize = (lsaa.customFemaleDrawSize.Equals(Vector2.zero) ? lsaa.customDrawSize : lsaa.customFemaleDrawSize);
						alienComp.customHeadDrawSize = (lsaa.customFemaleHeadDrawSize.Equals(Vector2.zero) ? lsaa.customHeadDrawSize : lsaa.customFemaleHeadDrawSize);
						alienComp.customPortraitDrawSize = (lsaa.customFemalePortraitDrawSize.Equals(Vector2.zero) ? lsaa.customPortraitDrawSize : lsaa.customFemalePortraitDrawSize);
						alienComp.customPortraitHeadDrawSize = (lsaa.customFemalePortraitHeadDrawSize.Equals(Vector2.zero) ? lsaa.customPortraitHeadDrawSize : lsaa.customFemalePortraitHeadDrawSize);
					}
					else
					{
						alienComp.customDrawSize = lsaa.customDrawSize;
						alienComp.customHeadDrawSize = lsaa.customHeadDrawSize;
						alienComp.customPortraitDrawSize = lsaa.customPortraitDrawSize;
						alienComp.customPortraitHeadDrawSize = lsaa.customPortraitHeadDrawSize;
					}
					alienComp.UpdateColors();
					AlienRenderTreePatches.portraitRender = new Pair<WeakReference, bool>(new WeakReference(alien), false);
					return;
				}
			}
			else
			{
				AnimalComp comp = alien.GetComp<AnimalComp>();
				if (comp != null)
				{
					AnimalBodyAddons extension = alien.def.GetModExtension<AnimalBodyAddons>();
					if (extension != null)
					{
						comp.addonGraphics = new List<Graphic>();
						AnimalComp animalComp = comp;
						if (animalComp.addonVariants == null)
						{
							animalComp.addonVariants = new List<int>();
						}
						int sharedIndex = 0;
						for (int i = 0; i < extension.bodyAddons.Count; i++)
						{
							Graphic path = extension.bodyAddons[i].GetGraphic(alien, null, ref sharedIndex, (comp.addonVariants.Count > i) ? new int?(comp.addonVariants[i]) : null, false, null);
							comp.addonGraphics.Add(path);
							if (comp.addonVariants.Count <= i)
							{
								comp.addonVariants.Add(sharedIndex);
							}
						}
					}
				}
			}
		}

		// Token: 0x0600001E RID: 30 RVA: 0x000041BE File Offset: 0x000023BE
		public static void PawnRenderTreeEnsureInitializedPostfix(PawnRenderTree __instance)
		{
			AlienRenderTreePatches.pawnRenderResolveData = null;
		}

		// Token: 0x0600001F RID: 31 RVA: 0x000041C8 File Offset: 0x000023C8
		public static bool BodyGraphicForPrefix(PawnRenderNode_Body __instance, Pawn pawn, ref Graphic __result)
		{
			if (!(pawn.def is ThingDef_AlienRace))
			{
				return true;
			}
			AlienRenderTreePatches.PawnRenderResolveData pawnRenderData = AlienRenderTreePatches.RegenerateResolveData(pawn);
			int sharedIndex = pawnRenderData.sharedIndex;
			GraphicPaths graphicPaths = pawnRenderData.alienProps.alienRace.graphicPaths;
			AlienPartGenerator.AlienComp alienComp = pawnRenderData.alienComp;
			AlienPartGenerator apg = pawnRenderData.alienProps.alienRace.generalSettings.alienPartGenerator;
			string bodyPath = graphicPaths.body.GetPath(pawn, ref sharedIndex, (alienComp.bodyVariant < 0) ? null : new int?(alienComp.bodyVariant), null);
			alienComp.bodyVariant = sharedIndex;
			string bodyMask = graphicPaths.bodyMasks.GetPath(pawn, ref sharedIndex, (alienComp.bodyMaskVariant < 0) ? null : new int?(alienComp.bodyMaskVariant), null);
			alienComp.bodyMaskVariant = sharedIndex;
			pawnRenderData.sharedIndex = sharedIndex;
			Shader shader;
			if (pawn.Drawer.renderer.StatueColor == null)
			{
				ShaderTypeDef skinShader2 = graphicPaths.skinShader;
				shader = ((skinShader2 != null) ? skinShader2.Shader : null) ?? ShaderUtility.GetSkinShader(pawn);
			}
			else
			{
				shader = ShaderDatabase.Cutout;
			}
			Shader skinShader = shader;
			if (skinShader == ShaderDatabase.CutoutSkin && pawn.story.SkinColorOverriden)
			{
				skinShader = ShaderDatabase.CutoutSkinColorOverride;
			}
			if (pawn.Drawer.renderer.CurRotDrawMode == RotDrawMode.Dessicated)
			{
				string skeletonPath = graphicPaths.skeleton.GetPath(pawn, ref sharedIndex, new int?(alienComp.bodyVariant), null);
				__result = ((!skeletonPath.NullOrEmpty()) ? GraphicDatabase.Get<Graphic_Multi>(skeletonPath, ShaderDatabase.Cutout) : null);
				return false;
			}
			__result = ((!bodyPath.NullOrEmpty()) ? CachedData.getInnerGraphic(new GraphicRequest(typeof(Graphic_Multi), bodyPath, AlienRenderTreePatches.CheckMaskShader(bodyPath, skinShader, !bodyMask.NullOrEmpty()), Vector2.one, __instance.ColorFor(pawn), apg.SkinColor(pawn, false), null, 0, graphicPaths.SkinColoringParameter, bodyMask)) : null);
			return false;
		}

		// Token: 0x06000020 RID: 32 RVA: 0x000043A4 File Offset: 0x000025A4
		public static bool HeadGraphicForPrefix(PawnRenderNode_Head __instance, Pawn pawn, ref Graphic __result)
		{
			if (!(pawn.def is ThingDef_AlienRace))
			{
				return true;
			}
			AlienRenderTreePatches.PawnRenderResolveData pawnRenderData = AlienRenderTreePatches.RegenerateResolveData(pawn);
			int sharedIndex = pawnRenderData.sharedIndex;
			GraphicPaths graphicPaths = pawnRenderData.alienProps.alienRace.graphicPaths;
			AlienPartGenerator.AlienComp alienComp = pawnRenderData.alienComp;
			AlienPartGenerator apg = pawnRenderData.alienProps.alienRace.generalSettings.alienPartGenerator;
			string headPath = graphicPaths.head.GetPath(pawn, ref sharedIndex, (alienComp.headVariant < 0) ? null : new int?(alienComp.headVariant), null);
			alienComp.headVariant = sharedIndex;
			string headMask = graphicPaths.headMasks.GetPath(pawn, ref sharedIndex, (alienComp.headMaskVariant < 0) ? null : new int?(alienComp.headMaskVariant), null);
			alienComp.headMaskVariant = sharedIndex;
			pawnRenderData.sharedIndex = sharedIndex;
			Shader shader;
			if (pawn.Drawer.renderer.StatueColor == null)
			{
				ShaderTypeDef skinShader2 = graphicPaths.skinShader;
				shader = ((skinShader2 != null) ? skinShader2.Shader : null) ?? ShaderUtility.GetSkinShader(pawn);
			}
			else
			{
				shader = ShaderDatabase.Cutout;
			}
			Shader skinShader = shader;
			if (skinShader == ShaderDatabase.CutoutSkin && pawn.story.SkinColorOverriden)
			{
				skinShader = ShaderDatabase.CutoutSkinColorOverride;
			}
			if (pawn.Drawer.renderer.CurRotDrawMode == RotDrawMode.Dessicated)
			{
				string skullPath = graphicPaths.skull.GetPath(pawn, ref sharedIndex, new int?(alienComp.headVariant), null);
				__result = ((pawn.health.hediffSet.HasHead && !skullPath.NullOrEmpty()) ? GraphicDatabase.Get<Graphic_Multi>(skullPath, ShaderDatabase.Cutout, Vector2.one, Color.white) : null);
				return false;
			}
			__result = ((pawn.health.hediffSet.HasHead && !headPath.NullOrEmpty()) ? CachedData.getInnerGraphic(new GraphicRequest(typeof(Graphic_Multi), headPath, AlienRenderTreePatches.CheckMaskShader(headPath, skinShader, !headMask.NullOrEmpty()), Vector2.one, __instance.ColorFor(pawn), apg.SkinColor(pawn, false), null, 0, graphicPaths.SkinColoringParameter, headMask)) : null);
			return false;
		}

		// Token: 0x06000021 RID: 33 RVA: 0x000045AB File Offset: 0x000027AB
		public static IEnumerable<CodeInstruction> StumpGraphicForTranspiler(IEnumerable<CodeInstruction> instructions)
		{
			AlienRenderTreePatches.<StumpGraphicForTranspiler>d__12 <StumpGraphicForTranspiler>d__ = new AlienRenderTreePatches.<StumpGraphicForTranspiler>d__12(-2);
			<StumpGraphicForTranspiler>d__.<>3__instructions = instructions;
			return <StumpGraphicForTranspiler>d__;
		}

		// Token: 0x06000022 RID: 34 RVA: 0x000045BC File Offset: 0x000027BC
		public static Graphic StumpGraphicHelper(PawnRenderNode_Stump node, Pawn pawn)
		{
			ThingDef_AlienRace alienProps = AlienRenderTreePatches.pawnRenderResolveData.alienProps;
			string path = ((alienProps != null) ? alienProps.alienRace.graphicPaths.stump.GetPath(pawn, ref AlienRenderTreePatches.pawnRenderResolveData.sharedIndex, new int?(AlienRenderTreePatches.pawnRenderResolveData.alienComp.headVariant), null) : null);
			if (path.NullOrEmpty())
			{
				return null;
			}
			return GraphicDatabase.Get<Graphic_Multi>(path, ShaderDatabase.CutoutComplex, Vector2.one, node.ColorFor(pawn), AlienRenderTreePatches.pawnRenderResolveData.alienProps.alienRace.generalSettings.alienPartGenerator.SkinColor(pawn, false));
		}

		// Token: 0x06000023 RID: 35 RVA: 0x00004650 File Offset: 0x00002850
		public static IEnumerable<CodeInstruction> HairDefGraphicForTranspiler(IEnumerable<CodeInstruction> instructions)
		{
			AlienRenderTreePatches.<HairDefGraphicForTranspiler>d__14 <HairDefGraphicForTranspiler>d__ = new AlienRenderTreePatches.<HairDefGraphicForTranspiler>d__14(-2);
			<HairDefGraphicForTranspiler>d__.<>3__instructions = instructions;
			return <HairDefGraphicForTranspiler>d__;
		}

		// Token: 0x06000024 RID: 36 RVA: 0x00004660 File Offset: 0x00002860
		public static Graphic HairGraphicHelper(string texPath, Shader shader, Vector2 size, Color color, Pawn pawn)
		{
			ThingDef_AlienRace alienProps = AlienRenderTreePatches.RegenerateResolveData(pawn).alienProps;
			Shader shader2;
			if (alienProps == null)
			{
				shader2 = null;
			}
			else
			{
				ShaderTypeDef shader3 = alienProps.alienRace.styleSettings[typeof(HairDef)].shader;
				shader2 = ((shader3 != null) ? shader3.Shader : null);
			}
			Shader shader4 = AlienRenderTreePatches.CheckMaskShader(texPath, shader2 ?? shader, false);
			AlienPartGenerator.AlienComp alienComp = AlienRenderTreePatches.pawnRenderResolveData.alienComp;
			return GraphicDatabase.Get<Graphic_Multi>(texPath, shader4, size, color, AlienRenderTreePatches.CheckOverrideColor(pawn, (alienComp != null) ? alienComp.GetChannel("hair").second : Color.white));
		}

		// Token: 0x06000025 RID: 37 RVA: 0x000046E9 File Offset: 0x000028E9
		public static IEnumerable<CodeInstruction> TattooDefGraphicForTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator ilg)
		{
			AlienRenderTreePatches.<TattooDefGraphicForTranspiler>d__16 <TattooDefGraphicForTranspiler>d__ = new AlienRenderTreePatches.<TattooDefGraphicForTranspiler>d__16(-2);
			<TattooDefGraphicForTranspiler>d__.<>3__instructions = instructions;
			<TattooDefGraphicForTranspiler>d__.<>3__ilg = ilg;
			return <TattooDefGraphicForTranspiler>d__;
		}

		// Token: 0x06000026 RID: 38 RVA: 0x00004700 File Offset: 0x00002900
		public static Shader TattooShaderHelper(Shader shader, StyleSettings style)
		{
			ShaderTypeDef shader2 = style.shader;
			return ((shader2 != null) ? shader2.Shader : null) ?? shader;
		}

		// Token: 0x06000027 RID: 39 RVA: 0x0000471C File Offset: 0x0000291C
		public static string TattooPathHelper(string path, Pawn pawn, bool body)
		{
			string text;
			if (!body)
			{
				Graphic headGraphic = pawn.Drawer.renderer.HeadGraphic;
				text = ((headGraphic != null) ? headGraphic.path : null);
			}
			else
			{
				Graphic bodyGraphic = pawn.Drawer.renderer.BodyGraphic;
				text = ((bodyGraphic != null) ? bodyGraphic.path : null);
			}
			return text ?? path;
		}

		// Token: 0x06000028 RID: 40 RVA: 0x0000476B File Offset: 0x0000296B
		public static IEnumerable<CodeInstruction> BeardDefGraphicForTranspiler(IEnumerable<CodeInstruction> instructions)
		{
			AlienRenderTreePatches.<BeardDefGraphicForTranspiler>d__19 <BeardDefGraphicForTranspiler>d__ = new AlienRenderTreePatches.<BeardDefGraphicForTranspiler>d__19(-2);
			<BeardDefGraphicForTranspiler>d__.<>3__instructions = instructions;
			return <BeardDefGraphicForTranspiler>d__;
		}

		// Token: 0x06000029 RID: 41 RVA: 0x0000477C File Offset: 0x0000297C
		public static Graphic BeardGraphicHelper(string texPath, Shader shader, Vector2 size, Color color, Pawn pawn)
		{
			ThingDef_AlienRace alienProps = AlienRenderTreePatches.RegenerateResolveData(pawn).alienProps;
			Shader shader2;
			if (alienProps == null)
			{
				shader2 = null;
			}
			else
			{
				ShaderTypeDef shader3 = alienProps.alienRace.styleSettings[typeof(BeardDef)].shader;
				shader2 = ((shader3 != null) ? shader3.Shader : null);
			}
			Shader shader4 = AlienRenderTreePatches.CheckMaskShader(texPath, shader2 ?? shader, false);
			AlienPartGenerator.AlienComp alienComp = AlienRenderTreePatches.pawnRenderResolveData.alienComp;
			return GraphicDatabase.Get<Graphic_Multi>(texPath, shader4, size, color, AlienRenderTreePatches.CheckOverrideColor(pawn, (alienComp != null) ? alienComp.GetChannel("hair").second : Color.white));
		}

		// Token: 0x0600002A RID: 42 RVA: 0x00004805 File Offset: 0x00002A05
		public static IEnumerable<CodeInstruction> RenderNodeGetMeshTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator ilg)
		{
			AlienRenderTreePatches.<RenderNodeGetMeshTranspiler>d__21 <RenderNodeGetMeshTranspiler>d__ = new AlienRenderTreePatches.<RenderNodeGetMeshTranspiler>d__21(-2);
			<RenderNodeGetMeshTranspiler>d__.<>3__instructions = instructions;
			return <RenderNodeGetMeshTranspiler>d__;
		}

		// Token: 0x0600002B RID: 43 RVA: 0x00004815 File Offset: 0x00002A15
		public static GraphicMeshSet RenderNodeGetMeshHelper(GraphicMeshSet meshSet, PawnRenderNode node, PawnDrawParms parms)
		{
			AlienRenderTreePatches.portraitRender = new Pair<WeakReference, bool>(new WeakReference(parms.pawn), parms.Portrait);
			if (parms.Portrait)
			{
				return node.MeshSetFor(parms.pawn);
			}
			return meshSet;
		}

		// Token: 0x0600002C RID: 44 RVA: 0x0000484A File Offset: 0x00002A4A
		public static bool IsPortrait(Pawn pawn)
		{
			WeakReference first = AlienRenderTreePatches.portraitRender.First;
			return ((first != null) ? first.Target : null) as Pawn == pawn && AlienRenderTreePatches.portraitRender.Second;
		}

		// Token: 0x0600002D RID: 45 RVA: 0x00004878 File Offset: 0x00002A78
		public static void GetHumanlikeHeadSetForPawnPrefix(Pawn pawn, ref float wFactor, ref float hFactor)
		{
			Vector2? vector;
			if (!AlienRenderTreePatches.IsPortrait(pawn))
			{
				AlienPartGenerator.AlienComp comp = pawn.GetComp<AlienPartGenerator.AlienComp>();
				vector = ((comp != null) ? new Vector2?(comp.customHeadDrawSize) : null);
			}
			else
			{
				AlienPartGenerator.AlienComp comp2 = pawn.GetComp<AlienPartGenerator.AlienComp>();
				vector = ((comp2 != null) ? new Vector2?(comp2.customPortraitHeadDrawSize) : null);
			}
			Vector2 drawSize = vector ?? Vector2.one;
			wFactor *= drawSize.x;
			hFactor *= drawSize.y;
		}

		// Token: 0x0600002E RID: 46 RVA: 0x000048FC File Offset: 0x00002AFC
		public static void GetHumanlikeBodySetForPawnPrefix(Pawn pawn, ref float wFactor, ref float hFactor)
		{
			Vector2? vector;
			if (!AlienRenderTreePatches.IsPortrait(pawn))
			{
				AlienPartGenerator.AlienComp comp = pawn.GetComp<AlienPartGenerator.AlienComp>();
				vector = ((comp != null) ? new Vector2?(comp.customDrawSize) : null);
			}
			else
			{
				AlienPartGenerator.AlienComp comp2 = pawn.GetComp<AlienPartGenerator.AlienComp>();
				vector = ((comp2 != null) ? new Vector2?(comp2.customPortraitDrawSize) : null);
			}
			Vector2 drawSize = vector ?? Vector2.one;
			wFactor *= drawSize.x;
			hFactor *= drawSize.y;
		}

		// Token: 0x04000025 RID: 37
		private static readonly Type patchType = typeof(AlienRenderTreePatches);

		// Token: 0x04000026 RID: 38
		public static AlienRenderTreePatches.PawnRenderResolveData pawnRenderResolveData;

		// Token: 0x04000027 RID: 39
		public static Pair<WeakReference, bool> portraitRender = new Pair<WeakReference, bool>(new WeakReference(new Pawn()), false);

		// Token: 0x02000087 RID: 135
		public class PawnRenderResolveData
		{
			// Token: 0x0400027E RID: 638
			public Pawn pawn;

			// Token: 0x0400027F RID: 639
			public ThingDef_AlienRace alienProps;

			// Token: 0x04000280 RID: 640
			public AlienPartGenerator.AlienComp alienComp;

			// Token: 0x04000281 RID: 641
			public LifeStageAgeAlien lsaa;

			// Token: 0x04000282 RID: 642
			public int sharedIndex;
		}
	}
}
