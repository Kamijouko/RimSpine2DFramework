using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld.Planet;
using Verse;
using Verse.AI.Group;

namespace RimWorld
{
	// Token: 0x020023D9 RID: 9177
	public static class ResurrectionUtility
	{
		// Token: 0x0600C728 RID: 50984 RVA: 0x003A05E4 File Offset: 0x0039E7E4
		public static bool TryResurrect(Pawn pawn, ResurrectionParams parms = null)
		{
			if (!pawn.Dead)
			{
				Log.Error("Tried to resurrect a pawn who is not dead: " + pawn.ToStringSafe<Pawn>());
				return false;
			}
			if (pawn.Discarded)
			{
				Log.Error("Tried to resurrect a discarded pawn: " + pawn.ToStringSafe<Pawn>());
				return false;
			}
			Corpse corpse = pawn.Corpse;
			bool flag = false;
			IntVec3 intVec = IntVec3.Invalid;
			Map map = null;
			if (ModsConfig.AnomalyActive && corpse is UnnaturalCorpse)
			{
				Messages.Message("MessageUnnaturalCorpseResurrect".Translate(corpse.InnerPawn.Named("PAWN")), corpse, MessageTypeDefOf.NeutralEvent, true);
				return false;
			}
			bool flag2 = Find.Selector.IsSelected(corpse);
			if (corpse != null)
			{
				flag = corpse.SpawnedOrAnyParentSpawned;
				intVec = corpse.PositionHeld;
				map = corpse.MapHeld;
				corpse.InnerPawn = null;
				corpse.Destroy(DestroyMode.Vanish);
			}
			if (flag && pawn.IsWorldPawn())
			{
				Find.WorldPawns.RemovePawn(pawn);
			}
			pawn.ForceSetStateToUnspawned();
			PawnComponentsUtility.CreateInitialComponents(pawn);
			pawn.health.Notify_Resurrected(parms == null || parms.restoreMissingParts, (parms != null) ? parms.gettingScarsChance : 0f);
			if (pawn.Faction != null && pawn.Faction.IsPlayer)
			{
				Pawn_WorkSettings workSettings = pawn.workSettings;
				if (workSettings != null)
				{
					workSettings.EnableAndInitialize();
				}
				Find.StoryWatcher.watcherPopAdaptation.Notify_PawnEvent(pawn, PopAdaptationEvent.GainedColonist);
			}
			if (pawn.RaceProps.IsMechanoid && MechRepairUtility.IsMissingWeapon(pawn))
			{
				MechRepairUtility.GenerateWeapon(pawn);
			}
			if (flag && (parms == null || !parms.dontSpawn))
			{
				GenSpawn.Spawn(pawn, intVec, map, WipeMode.Vanish);
				Lord lord = pawn.GetLord();
				if (lord != null)
				{
					if (lord != null)
					{
						lord.Notify_PawnUndowned(pawn);
					}
				}
				else if (pawn.Faction != null && pawn.Faction != Faction.OfPlayer && pawn.HostileTo(Faction.OfPlayer) && (parms == null || !parms.noLord))
				{
					LordJob_AssaultColony lordJob_AssaultColony;
					if (parms != null)
					{
						lordJob_AssaultColony = new LordJob_AssaultColony(pawn.Faction, parms.canKidnap, parms.canTimeoutOrFlee, parms.sappers, parms.useAvoidGridSmart, parms.canSteal, parms.breachers, parms.canPickUpOpportunisticWeapons);
					}
					else
					{
						lordJob_AssaultColony = new LordJob_AssaultColony(pawn.Faction, true, true, false, false, true, false, false);
					}
					LordMaker.MakeNewLord(pawn.Faction, lordJob_AssaultColony, pawn.Map, Gen.YieldSingle<Pawn>(pawn));
				}
				if (pawn.apparel != null)
				{
					List<Apparel> wornApparel = pawn.apparel.WornApparel;
					for (int i = 0; i < wornApparel.Count; i++)
					{
						wornApparel[i].Notify_PawnResurrected(pawn);
					}
				}
			}
			if (parms != null && parms.removeDiedThoughts)
			{
				PawnDiedOrDownedThoughtsUtility.RemoveDiedThoughts(pawn);
			}
			Pawn_RoyaltyTracker royalty = pawn.royalty;
			if (royalty != null)
			{
				royalty.Notify_Resurrected();
			}
			if (pawn.relations != null)
			{
				pawn.relations.hidePawnRelations = false;
			}
			if (pawn.guest != null && pawn.guest.IsInteractionEnabled(PrisonerInteractionModeDefOf.Execution))
			{
				pawn.guest.SetNoInteraction();
			}
			if (flag2 && pawn != null)
			{
				Find.Selector.Select(pawn, false, false);
			}
			pawn.Drawer.renderer.SetAllGraphicsDirty();
			if (parms != null && parms.invisibleStun)
			{
				pawn.stances.stunner.StunFor(5f.SecondsToTicks(), pawn, false, false, false);
			}
			pawn.needs.AddOrRemoveNeedsAsAppropriate();
			return true;
		}

		// Token: 0x0600C729 RID: 50985 RVA: 0x003A0918 File Offset: 0x0039EB18
		public static bool TryResurrectWithSideEffects(Pawn pawn)
		{
			Corpse corpse = pawn.Corpse;
			float num;
			if (corpse != null)
			{
				num = corpse.GetComp<CompRottable>().RotProgress / 60000f;
			}
			else
			{
				num = 0f;
			}
			if (!ResurrectionUtility.TryResurrect(pawn, null))
			{
				return false;
			}
			BodyPartRecord brain = pawn.health.hediffSet.GetBrain();
			Hediff hediff = HediffMaker.MakeHediff(HediffDefOf.ResurrectionSickness, pawn, null);
			if (!pawn.health.WouldDieAfterAddingHediff(hediff))
			{
				pawn.health.AddHediff(hediff, null, null, null);
			}
			if (Rand.Chance(ResurrectionUtility.DementiaChancePerRotDaysCurve.Evaluate(num)) && brain != null)
			{
				Hediff hediff2 = HediffMaker.MakeHediff(HediffDefOf.Dementia, pawn, brain);
				if (!pawn.health.WouldDieAfterAddingHediff(hediff2))
				{
					pawn.health.AddHediff(hediff2, null, null, null);
				}
			}
			if (Rand.Chance(ResurrectionUtility.BlindnessChancePerRotDaysCurve.Evaluate(num)))
			{
				foreach (BodyPartRecord bodyPartRecord in from x in pawn.health.hediffSet.GetNotMissingParts(BodyPartHeight.Undefined, BodyPartDepth.Undefined, null, null)
					where x.def == BodyPartDefOf.Eye
					select x)
				{
					if (!pawn.health.hediffSet.PartOrAnyAncestorHasDirectlyAddedParts(bodyPartRecord))
					{
						Hediff hediff3 = HediffMaker.MakeHediff(HediffDefOf.Blindness, pawn, bodyPartRecord);
						pawn.health.AddHediff(hediff3, null, null, null);
					}
				}
			}
			if (brain != null && Rand.Chance(ResurrectionUtility.ResurrectionPsychosisChancePerRotDaysCurve.Evaluate(num)))
			{
				Hediff hediff4 = HediffMaker.MakeHediff(HediffDefOf.ResurrectionPsychosis, pawn, brain);
				if (!pawn.health.WouldDieAfterAddingHediff(hediff4))
				{
					pawn.health.AddHediff(hediff4, null, null, null);
				}
			}
			if (pawn.Dead)
			{
				Log.Error("The pawn has died while being resurrected.");
				ResurrectionUtility.TryResurrect(pawn, null);
			}
			return true;
		}

		// Token: 0x040087B8 RID: 34744
		private static SimpleCurve DementiaChancePerRotDaysCurve = new SimpleCurve
		{
			{
				new CurvePoint(0.1f, 0.02f),
				true
			},
			{
				new CurvePoint(5f, 0.8f),
				true
			}
		};

		// Token: 0x040087B9 RID: 34745
		private static SimpleCurve BlindnessChancePerRotDaysCurve = new SimpleCurve
		{
			{
				new CurvePoint(0.1f, 0.02f),
				true
			},
			{
				new CurvePoint(5f, 0.8f),
				true
			}
		};

		// Token: 0x040087BA RID: 34746
		private static SimpleCurve ResurrectionPsychosisChancePerRotDaysCurve = new SimpleCurve
		{
			{
				new CurvePoint(0.1f, 0.02f),
				true
			},
			{
				new CurvePoint(5f, 0.8f),
				true
			}
		};
	}
}
