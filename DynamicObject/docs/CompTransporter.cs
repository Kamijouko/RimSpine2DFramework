using System;
using System.Collections.Generic;
using System.Text;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using Verse.Sound;

namespace RimWorld
{
	// Token: 0x02002EBB RID: 11963
	[StaticConstructorOnStartup]
	public class CompTransporter : ThingComp, IThingHolder, ISearchableContents
	{
		// Token: 0x17002B67 RID: 11111
		// (get) Token: 0x06010DB8 RID: 69048 RVA: 0x004D9F08 File Offset: 0x004D8108
		public CompProperties_Transporter Props
		{
			get
			{
				return (CompProperties_Transporter)this.props;
			}
		}

		// Token: 0x17002B68 RID: 11112
		// (get) Token: 0x06010DB9 RID: 69049 RVA: 0x004D9F15 File Offset: 0x004D8115
		public Map Map
		{
			get
			{
				return this.parent.MapHeld;
			}
		}

		// Token: 0x17002B69 RID: 11113
		// (get) Token: 0x06010DBA RID: 69050 RVA: 0x004D9F22 File Offset: 0x004D8122
		public bool AnythingLeftToLoad
		{
			get
			{
				return this.FirstThingLeftToLoad != null;
			}
		}

		// Token: 0x17002B6A RID: 11114
		// (get) Token: 0x06010DBB RID: 69051 RVA: 0x004D9F2D File Offset: 0x004D812D
		public bool LoadingInProgressOrReadyToLaunch
		{
			get
			{
				return this.groupID >= 0 || this.parent.IsInCaravan();
			}
		}

		// Token: 0x17002B6B RID: 11115
		// (get) Token: 0x06010DBC RID: 69052 RVA: 0x004D9F45 File Offset: 0x004D8145
		public bool AnyInGroupHasAnythingLeftToLoad
		{
			get
			{
				return this.FirstThingLeftToLoadInGroup != null;
			}
		}

		// Token: 0x17002B6C RID: 11116
		// (get) Token: 0x06010DBD RID: 69053 RVA: 0x004D9F50 File Offset: 0x004D8150
		public float MassCapacity
		{
			get
			{
				if (this.massCapacityOverride > 0f)
				{
					return this.massCapacityOverride;
				}
				return this.Props.massCapacity;
			}
		}

		// Token: 0x17002B6D RID: 11117
		// (get) Token: 0x06010DBE RID: 69054 RVA: 0x004D9F71 File Offset: 0x004D8171
		public ThingOwner SearchableContents
		{
			get
			{
				return this.innerContainer;
			}
		}

		// Token: 0x17002B6E RID: 11118
		// (get) Token: 0x06010DBF RID: 69055 RVA: 0x004D9F79 File Offset: 0x004D8179
		public bool Groupable
		{
			get
			{
				return !this.Props.max1PerGroup;
			}
		}

		// Token: 0x17002B6F RID: 11119
		// (get) Token: 0x06010DC0 RID: 69056 RVA: 0x004D9F89 File Offset: 0x004D8189
		public bool OverMassCapacity
		{
			get
			{
				return this.MassUsage > this.MassCapacity;
			}
		}

		// Token: 0x17002B70 RID: 11120
		// (get) Token: 0x06010DC1 RID: 69057 RVA: 0x004D9F99 File Offset: 0x004D8199
		public float MassUsage
		{
			get
			{
				if (this.massUsageDirty)
				{
					this.massUsageDirty = false;
					this.cachedMassUsage = CollectionsMassCalculator.MassUsage(this.innerContainer, IgnorePawnsInventoryMode.IgnoreIfAssignedToUnload, true, false);
				}
				return this.cachedMassUsage;
			}
		}

		// Token: 0x17002B71 RID: 11121
		// (get) Token: 0x06010DC2 RID: 69058 RVA: 0x004D9FC4 File Offset: 0x004D81C4
		public CompLaunchable Launchable
		{
			get
			{
				CompLaunchable compLaunchable;
				if ((compLaunchable = this.cachedCompLaunchable) == null)
				{
					compLaunchable = (this.cachedCompLaunchable = this.parent.GetComp<CompLaunchable>());
				}
				return compLaunchable;
			}
		}

		// Token: 0x17002B72 RID: 11122
		// (get) Token: 0x06010DC3 RID: 69059 RVA: 0x004D9FF0 File Offset: 0x004D81F0
		public CompShuttle Shuttle
		{
			get
			{
				CompShuttle compShuttle;
				if ((compShuttle = this.cachedCompShuttle) == null)
				{
					compShuttle = (this.cachedCompShuttle = this.parent.GetComp<CompShuttle>());
				}
				return compShuttle;
			}
		}

		// Token: 0x17002B73 RID: 11123
		// (get) Token: 0x06010DC4 RID: 69060 RVA: 0x000028E7 File Offset: 0x00000AE7
		public virtual bool RequiresFuelingPort
		{
			get
			{
				return true;
			}
		}

		// Token: 0x17002B74 RID: 11124
		// (get) Token: 0x06010DC5 RID: 69061 RVA: 0x004DA01C File Offset: 0x004D821C
		public Thing FirstThingLeftToLoad
		{
			get
			{
				if (this.leftToLoad == null)
				{
					return null;
				}
				for (int i = 0; i < this.leftToLoad.Count; i++)
				{
					if (this.leftToLoad[i].CountToTransfer != 0 && this.leftToLoad[i].HasAnyThing)
					{
						return this.leftToLoad[i].AnyThing;
					}
				}
				return null;
			}
		}

		// Token: 0x17002B75 RID: 11125
		// (get) Token: 0x06010DC6 RID: 69062 RVA: 0x004DA084 File Offset: 0x004D8284
		public Thing FirstThingLeftToLoadInGroup
		{
			get
			{
				List<CompTransporter> list = this.TransportersInGroup(this.parent.Map);
				if (list == null)
				{
					return null;
				}
				for (int i = 0; i < list.Count; i++)
				{
					Thing firstThingLeftToLoad = list[i].FirstThingLeftToLoad;
					if (firstThingLeftToLoad != null)
					{
						return firstThingLeftToLoad;
					}
				}
				return null;
			}
		}

		// Token: 0x17002B76 RID: 11126
		// (get) Token: 0x06010DC7 RID: 69063 RVA: 0x004DA0CC File Offset: 0x004D82CC
		public bool AnyInGroupNotifiedCantLoadMore
		{
			get
			{
				List<CompTransporter> list = this.TransportersInGroup(this.parent.Map);
				if (list == null)
				{
					return false;
				}
				for (int i = 0; i < list.Count; i++)
				{
					if (list[i].notifiedCantLoadMore)
					{
						return true;
					}
				}
				return false;
			}
		}

		// Token: 0x17002B77 RID: 11127
		// (get) Token: 0x06010DC8 RID: 69064 RVA: 0x004DA114 File Offset: 0x004D8314
		public bool AnyPawnCanLoadAnythingNow
		{
			get
			{
				if (!this.AnythingLeftToLoad)
				{
					return false;
				}
				if (!this.parent.Spawned)
				{
					return false;
				}
				IReadOnlyList<Pawn> allPawnsSpawned = this.parent.Map.mapPawns.AllPawnsSpawned;
				for (int i = 0; i < allPawnsSpawned.Count; i++)
				{
					if (allPawnsSpawned[i].CurJobDef == JobDefOf.HaulToTransporter)
					{
						CompTransporter transporter = ((JobDriver_HaulToTransporter)allPawnsSpawned[i].jobs.curDriver).Transporter;
						if (transporter != null && transporter.groupID == this.groupID)
						{
							return true;
						}
					}
					if (allPawnsSpawned[i].CurJobDef == JobDefOf.EnterTransporter)
					{
						CompTransporter transporter2 = ((JobDriver_EnterTransporter)allPawnsSpawned[i].jobs.curDriver).Transporter;
						if (transporter2 != null && transporter2.groupID == this.groupID)
						{
							return true;
						}
					}
				}
				List<CompTransporter> list = this.TransportersInGroup(this.parent.Map);
				if (list == null)
				{
					return false;
				}
				for (int j = 0; j < allPawnsSpawned.Count; j++)
				{
					if (allPawnsSpawned[j].mindState.duty != null && allPawnsSpawned[j].mindState.duty.transportersGroup == this.groupID)
					{
						CompTransporter compTransporter = JobGiver_EnterTransporter.FindMyTransporter(list, allPawnsSpawned[j]);
						if (compTransporter != null && allPawnsSpawned[j].CanReach(compTransporter.parent, PathEndMode.Touch, Danger.Deadly, false, false, TraverseMode.ByPawn))
						{
							return true;
						}
					}
				}
				for (int k = 0; k < allPawnsSpawned.Count; k++)
				{
					if (allPawnsSpawned[k].IsColonist)
					{
						for (int l = 0; l < list.Count; l++)
						{
							if (LoadTransportersJobUtility.HasJobOnTransporter(allPawnsSpawned[k], list[l]))
							{
								return true;
							}
						}
					}
				}
				return false;
			}
		}

		// Token: 0x06010DC9 RID: 69065 RVA: 0x004DA2D8 File Offset: 0x004D84D8
		public CompTransporter()
		{
			this.innerContainer = new ThingOwner<Thing>(this);
		}

		// Token: 0x06010DCA RID: 69066 RVA: 0x004DA328 File Offset: 0x004D8528
		public override void PostExposeData()
		{
			base.PostExposeData();
			bool flag = !this.parent.SpawnedOrAnyParentSpawned;
			if (flag && Scribe.mode == LoadSaveMode.Saving)
			{
				this.tmpThings.Clear();
				this.tmpThings.AddRange(this.innerContainer);
				this.tmpSavedPawns.Clear();
				for (int i = 0; i < this.tmpThings.Count; i++)
				{
					Pawn pawn = this.tmpThings[i] as Pawn;
					if (pawn != null)
					{
						this.innerContainer.Remove(pawn);
						this.tmpSavedPawns.Add(pawn);
						if (!pawn.IsWorldPawn())
						{
							string text = "Trying to save a non-world pawn (";
							Pawn pawn2 = pawn;
							Log.Error(text + ((pawn2 != null) ? pawn2.ToString() : null) + ") as a reference in a transporter.");
						}
					}
				}
				this.tmpThings.Clear();
			}
			Scribe_Collections.Look<Pawn>(ref this.tmpSavedPawns, "tmpSavedPawns", LookMode.Reference, Array.Empty<object>());
			Scribe_Values.Look<int>(ref this.groupID, "groupID", 0, false);
			Scribe_Deep.Look<ThingOwner>(ref this.innerContainer, "innerContainer", new object[] { this });
			Scribe_Collections.Look<TransferableOneWay>(ref this.leftToLoad, "leftToLoad", LookMode.Deep, Array.Empty<object>());
			Scribe_Values.Look<bool>(ref this.notifiedCantLoadMore, "notifiedCantLoadMore", false, false);
			Scribe_Values.Look<float>(ref this.massCapacityOverride, "massCapacityOverride", 0f, false);
			if (flag && (Scribe.mode == LoadSaveMode.PostLoadInit || Scribe.mode == LoadSaveMode.Saving))
			{
				for (int j = 0; j < this.tmpSavedPawns.Count; j++)
				{
					this.innerContainer.TryAdd(this.tmpSavedPawns[j], true);
				}
				this.tmpSavedPawns.Clear();
			}
		}

		// Token: 0x06010DCB RID: 69067 RVA: 0x004D9F71 File Offset: 0x004D8171
		public ThingOwner GetDirectlyHeldThings()
		{
			return this.innerContainer;
		}

		// Token: 0x06010DCC RID: 69068 RVA: 0x004DA4C7 File Offset: 0x004D86C7
		public void GetChildHolders(List<IThingHolder> outChildren)
		{
			ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, this.GetDirectlyHeldThings());
		}

		// Token: 0x06010DCD RID: 69069 RVA: 0x004DA4D8 File Offset: 0x004D86D8
		public override void CompTick()
		{
			if (this.Props.shouldTickContents)
			{
				this.innerContainer.DoTick();
			}
			else if (this.Props.restEffectiveness != 0f)
			{
				for (int i = 0; i < this.innerContainer.Count; i++)
				{
					Pawn pawn = this.innerContainer[i] as Pawn;
					if (pawn != null && !pawn.Dead && pawn.needs.rest != null)
					{
						pawn.needs.rest.TickResting(this.Props.restEffectiveness);
					}
				}
			}
			if (this.parent.IsHashIntervalTick(60) && this.parent.Spawned)
			{
				CompShuttle shuttle = this.Shuttle;
				if ((shuttle == null || !shuttle.Autoload) && this.LoadingInProgressOrReadyToLaunch && this.AnyInGroupHasAnythingLeftToLoad && !this.AnyInGroupNotifiedCantLoadMore && !this.AnyPawnCanLoadAnythingNow)
				{
					this.notifiedCantLoadMore = true;
					if (this.Shuttle != null)
					{
						Messages.Message("MessageCantLoadMoreIntoShuttle".Translate(this.FirstThingLeftToLoadInGroup.LabelNoCount, Faction.OfPlayer.def.pawnsPlural, this.FirstThingLeftToLoadInGroup), this.parent, MessageTypeDefOf.CautionInput, true);
						return;
					}
					Messages.Message("MessageCantLoadMoreIntoTransporters".Translate(this.FirstThingLeftToLoadInGroup.LabelNoCount, Faction.OfPlayer.def.pawnsPlural, this.FirstThingLeftToLoadInGroup), this.parent, MessageTypeDefOf.CautionInput, true);
				}
			}
		}

		// Token: 0x06010DCE RID: 69070 RVA: 0x004DA688 File Offset: 0x004D8888
		public List<CompTransporter> TransportersInGroup(Map map)
		{
			CompTransporter.tmpTransportersInGroup.Clear();
			if (!this.parent.Spawned)
			{
				CompTransporter.tmpTransportersInGroup.Add(this);
				return CompTransporter.tmpTransportersInGroup;
			}
			if (!this.LoadingInProgressOrReadyToLaunch)
			{
				return null;
			}
			TransporterUtility.GetTransportersInGroup(this.groupID, map, CompTransporter.tmpTransportersInGroup);
			return CompTransporter.tmpTransportersInGroup;
		}

		// Token: 0x06010DCF RID: 69071 RVA: 0x004DA6DD File Offset: 0x004D88DD
		public override IEnumerable<Gizmo> CompGetGizmosExtra()
		{
			foreach (Gizmo gizmo in base.CompGetGizmosExtra())
			{
				yield return gizmo;
			}
			IEnumerator<Gizmo> enumerator = null;
			int num = 0;
			foreach (object obj in Find.Selector.SelectedObjects)
			{
				ThingWithComps thingWithComps = obj as ThingWithComps;
				if (thingWithComps != null && thingWithComps.HasComp<CompTransporter>())
				{
					num++;
				}
			}
			if (this.Shuttle != null && (!this.Shuttle.ShowLoadingGizmos || num > 1))
			{
				yield break;
			}
			if (this.LoadingInProgressOrReadyToLaunch)
			{
				CompShuttle shuttle = this.Shuttle;
				if ((shuttle == null || !shuttle.Autoload) && this.innerContainer.Any)
				{
					TaggedString taggedString = (this.AnythingLeftToLoad ? "CommandCancelLoad".Translate() : "CommandUnload".Translate());
					TaggedString taggedString2 = (this.AnythingLeftToLoad ? "CommandCancelLoadDesc".Translate() : "CommandUnloadDesc".Translate(this.parent.LabelShort));
					Texture2D cancelLoadCommandTex = CompTransporter.CancelLoadCommandTex;
					yield return new Command_Action
					{
						defaultLabel = taggedString,
						defaultDesc = taggedString2,
						icon = cancelLoadCommandTex,
						action = delegate
						{
							SoundDefOf.Designate_Cancel.PlayOneShotOnCamera(null);
							this.CancelLoad();
						}
					};
				}
				if (this.Groupable)
				{
					yield return new Command_Action
					{
						defaultLabel = "CommandSelectPreviousTransporter".Translate(),
						defaultDesc = "CommandSelectPreviousTransporterDesc".Translate(),
						icon = CompTransporter.SelectPreviousInGroupCommandTex,
						action = new Action(this.SelectPreviousInGroup)
					};
					yield return new Command_Action
					{
						defaultLabel = "CommandSelectAllTransporters".Translate(),
						defaultDesc = "CommandSelectAllTransportersDesc".Translate(),
						icon = CompTransporter.SelectAllInGroupCommandTex,
						action = new Action(this.SelectAllInGroup)
					};
					yield return new Command_Action
					{
						defaultLabel = "CommandSelectNextTransporter".Translate(),
						defaultDesc = "CommandSelectNextTransporterDesc".Translate(),
						icon = CompTransporter.SelectNextInGroupCommandTex,
						action = new Action(this.SelectNextInGroup)
					};
				}
				if (this.Props.canChangeAssignedThingsAfterStarting && (this.Shuttle == null || !this.Shuttle.Autoload))
				{
					yield return new Command_LoadToTransporter
					{
						defaultLabel = "CommandSetToLoadTransporter".Translate(),
						defaultDesc = "CommandSetToLoadTransporterDesc".Translate(),
						icon = CompTransporter.LoadCommandTex,
						transComp = this
					};
				}
			}
			else
			{
				Command_LoadToTransporter command_LoadToTransporter = new Command_LoadToTransporter();
				if (!this.Groupable)
				{
					if (this.Props.canChangeAssignedThingsAfterStarting)
					{
						command_LoadToTransporter.defaultLabel = "CommandSetToLoadTransporter".Translate();
						command_LoadToTransporter.defaultDesc = "CommandSetToLoadTransporterDesc".Translate();
					}
					else
					{
						command_LoadToTransporter.defaultLabel = "CommandLoadTransporterSingle".Translate();
						command_LoadToTransporter.defaultDesc = "CommandLoadTransporterSingleDesc".Translate();
					}
				}
				else
				{
					int num2 = 0;
					for (int i = 0; i < Find.Selector.NumSelected; i++)
					{
						Thing thing = Find.Selector.SelectedObjectsListForReading[i] as Thing;
						if (thing != null && thing.def == this.parent.def)
						{
							CompLaunchable compLaunchable = thing.TryGetComp<CompLaunchable>();
							if (compLaunchable != null)
							{
								CompRefuelable refuelable = compLaunchable.Refuelable;
								if (refuelable == null || !refuelable.HasFuel)
								{
									goto IL_04BD;
								}
							}
							num2++;
						}
						IL_04BD:;
					}
					command_LoadToTransporter.defaultLabel = "CommandLoadTransporter".Translate(num2.ToString());
					command_LoadToTransporter.defaultDesc = "CommandLoadTransporterDesc".Translate();
				}
				command_LoadToTransporter.icon = CompTransporter.LoadCommandTex;
				command_LoadToTransporter.transComp = this;
				CompLaunchable launchable = this.Launchable;
				if (launchable != null && launchable.RequiresFuelingPort)
				{
					CompLaunchable_TransportPod compLaunchable_TransportPod = launchable as CompLaunchable_TransportPod;
					if (compLaunchable_TransportPod != null && !compLaunchable_TransportPod.ConnectedToFuelingPort)
					{
						command_LoadToTransporter.Disable("CommandLoadTransporterFailNotConnectedToFuelingPort".Translate());
					}
					else if (!launchable.Refuelable.HasFuel && this.Shuttle == null)
					{
						command_LoadToTransporter.Disable("CommandLoadTransporterFailNoFuel".Translate());
					}
				}
				yield return command_LoadToTransporter;
			}
			yield break;
			yield break;
		}

		// Token: 0x06010DD0 RID: 69072 RVA: 0x004DA6F0 File Offset: 0x004D88F0
		public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
		{
			base.PostDeSpawn(map, mode);
			if (this.parent.BeingTransportedOnGravship)
			{
				return;
			}
			if (this.Launchable != null && this.Launchable.lastLaunchTick == Find.TickManager.TicksGame)
			{
				return;
			}
			if (this.CancelLoad(map) && this.Shuttle == null)
			{
				if (!this.Groupable)
				{
					Messages.Message("MessageTransporterSingleLoadCanceled_TransporterDestroyed".Translate(), MessageTypeDefOf.NegativeEvent, true);
				}
				else
				{
					Messages.Message("MessageTransportersLoadCanceled_TransporterDestroyed".Translate(), MessageTypeDefOf.NegativeEvent, true);
				}
			}
			if (mode != DestroyMode.WillReplace)
			{
				this.innerContainer.TryDropAll(this.parent.Position, map, ThingPlaceMode.Near, null, null, true);
			}
		}

		// Token: 0x06010DD1 RID: 69073 RVA: 0x004DA7A4 File Offset: 0x004D89A4
		public override string CompInspectStringExtra()
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append("Contents".Translate() + ": " + this.innerContainer.ContentsString.CapitalizeFirst());
			if (this.Props.showMassInInspectString)
			{
				TaggedString taggedString = "Mass".Translate() + ": " + this.MassUsage.ToString("F0") + " / " + this.MassCapacity.ToString("F0") + " kg";
				stringBuilder.AppendLine().Append((this.MassUsage > this.MassCapacity) ? taggedString.Colorize(ColorLibrary.RedReadable) : taggedString);
			}
			return stringBuilder.ToString();
		}

		// Token: 0x06010DD2 RID: 69074 RVA: 0x004DA888 File Offset: 0x004D8A88
		public void AddToTheToLoadList(TransferableOneWay t, int count)
		{
			if (!t.HasAnyThing || count <= 0)
			{
				return;
			}
			if (this.leftToLoad == null)
			{
				this.leftToLoad = new List<TransferableOneWay>();
			}
			TransferableOneWay transferableOneWay = TransferableUtility.TransferableMatching<TransferableOneWay>(t.AnyThing, this.leftToLoad, TransferAsOneMode.PodsOrCaravanPacking);
			if (transferableOneWay != null)
			{
				for (int i = 0; i < t.things.Count; i++)
				{
					if (!transferableOneWay.things.Contains(t.things[i]))
					{
						transferableOneWay.things.Add(t.things[i]);
					}
				}
				if (transferableOneWay.CanAdjustBy(count).Accepted)
				{
					transferableOneWay.AdjustBy(count);
					return;
				}
			}
			else
			{
				TransferableOneWay transferableOneWay2 = new TransferableOneWay();
				this.leftToLoad.Add(transferableOneWay2);
				transferableOneWay2.things.AddRange(t.things);
				transferableOneWay2.AdjustTo(count);
			}
		}

		// Token: 0x06010DD3 RID: 69075 RVA: 0x004DA954 File Offset: 0x004D8B54
		public bool LeftToLoadContains(Thing thing)
		{
			if (this.leftToLoad == null)
			{
				return false;
			}
			for (int i = 0; i < this.leftToLoad.Count; i++)
			{
				for (int j = 0; j < this.leftToLoad[i].things.Count; j++)
				{
					if (this.leftToLoad[i].things[j] == thing)
					{
						return true;
					}
				}
			}
			return false;
		}

		// Token: 0x06010DD4 RID: 69076 RVA: 0x004DA9C0 File Offset: 0x004D8BC0
		public void Notify_ThingAdded(Thing t)
		{
			this.SubtractFromToLoadList(t, t.stackCount, true);
			if (this.parent.Spawned && this.Props.pawnLoadedSound != null && t is Pawn)
			{
				this.Props.pawnLoadedSound.PlayOneShot(new TargetInfo(this.parent.Position, this.parent.Map, false));
			}
			Pawn pawn = t as Pawn;
			if (pawn != null && pawn.IsFormingCaravan())
			{
				pawn.GetLord().Notify_PawnLost(pawn, PawnLostCondition.ForcedByPlayerAction, null);
			}
			QuestUtility.SendQuestTargetSignals(this.parent.questTags, "ThingAdded", t.Named("SUBJECT"));
			this.massUsageDirty = true;
		}

		// Token: 0x06010DD5 RID: 69077 RVA: 0x004DAA80 File Offset: 0x004D8C80
		public void Notify_ThingRemoved(Thing t)
		{
			if (this.Props.pawnExitSound != null && t is Pawn)
			{
				this.Props.pawnExitSound.PlayOneShot(new TargetInfo(this.parent.Position, this.parent.Map, false));
			}
			this.massUsageDirty = true;
		}

		// Token: 0x06010DD6 RID: 69078 RVA: 0x004DAADA File Offset: 0x004D8CDA
		public void Notify_ThingAddedAndMergedWith(Thing t, int mergedCount)
		{
			this.SubtractFromToLoadList(t, mergedCount, true);
			this.massUsageDirty = true;
		}

		// Token: 0x06010DD7 RID: 69079 RVA: 0x004DAAED File Offset: 0x004D8CED
		public bool CancelLoad()
		{
			CompShuttle shuttle = this.Shuttle;
			if (((shuttle != null) ? shuttle.shipParent : null) != null)
			{
				this.Shuttle.shipParent.ForceJob_DelayCurrent(ShipJobMaker.MakeShipJob(ShipJobDefOf.Unload));
			}
			return this.CancelLoad(this.Map);
		}

		// Token: 0x06010DD8 RID: 69080 RVA: 0x004DAB2C File Offset: 0x004D8D2C
		public bool CancelLoad(Map map)
		{
			if (!this.LoadingInProgressOrReadyToLaunch)
			{
				return false;
			}
			this.TryRemoveLord(map);
			List<CompTransporter> list = this.TransportersInGroup(map);
			if (list == null)
			{
				return false;
			}
			for (int i = 0; i < list.Count; i++)
			{
				list[i].CleanUpLoadingVars(map);
			}
			this.CleanUpLoadingVars(map);
			return true;
		}

		// Token: 0x06010DD9 RID: 69081 RVA: 0x004DAB80 File Offset: 0x004D8D80
		public void TryRemoveLord(Map map)
		{
			if (!this.LoadingInProgressOrReadyToLaunch)
			{
				return;
			}
			Lord lord = TransporterUtility.FindLord(this.groupID, map);
			if (lord != null)
			{
				foreach (Pawn pawn in lord.ownedPawns)
				{
					if (pawn.IsColonist && map.IsPlayerHome)
					{
						pawn.inventory.UnloadEverything = true;
					}
				}
				map.lordManager.RemoveLord(lord);
			}
		}

		// Token: 0x06010DDA RID: 69082 RVA: 0x004DAC10 File Offset: 0x004D8E10
		public void CleanUpLoadingVars(Map map)
		{
			this.groupID = -1;
			this.innerContainer.TryDropAll(this.parent.Position, map, ThingPlaceMode.Near, null, null, true);
			List<TransferableOneWay> list = this.leftToLoad;
			if (list != null)
			{
				list.Clear();
			}
			CompShuttle shuttle = this.Shuttle;
			if (shuttle != null)
			{
				shuttle.CleanUpLoadingVars();
			}
			this.notifiedCantLoadMore = false;
			this.massUsageDirty = true;
		}

		// Token: 0x06010DDB RID: 69083 RVA: 0x004DAC70 File Offset: 0x004D8E70
		public int SubtractFromToLoadList(Thing t, int count, bool sendMessageOnFinished = true)
		{
			if (this.leftToLoad == null)
			{
				return 0;
			}
			TransferableOneWay transferableOneWay = TransferableUtility.TransferableMatchingDesperate(t, this.leftToLoad, TransferAsOneMode.PodsOrCaravanPacking);
			if (transferableOneWay == null)
			{
				return 0;
			}
			if (transferableOneWay.CountToTransfer <= 0)
			{
				return 0;
			}
			int num = Mathf.Min(count, transferableOneWay.CountToTransfer);
			transferableOneWay.AdjustBy(-num);
			if (transferableOneWay.CountToTransfer <= 0)
			{
				this.leftToLoad.Remove(transferableOneWay);
			}
			if (sendMessageOnFinished && !this.AnyInGroupHasAnythingLeftToLoad)
			{
				CompShuttle comp = this.parent.GetComp<CompShuttle>();
				if (comp == null || comp.AllRequiredThingsLoaded)
				{
					if (comp != null)
					{
						Messages.Message("MessageFinishedLoadingShuttle".Translate(this.parent.Named("SHUTTLE")), this.parent, MessageTypeDefOf.TaskCompletion, true);
					}
					else
					{
						Messages.Message("MessageFinishedLoadingTransporters".Translate(), this.parent, MessageTypeDefOf.TaskCompletion, true);
					}
				}
			}
			return num;
		}

		// Token: 0x06010DDC RID: 69084 RVA: 0x004DAD54 File Offset: 0x004D8F54
		private void SelectPreviousInGroup()
		{
			List<CompTransporter> list = this.TransportersInGroup(this.Map);
			if (list != null)
			{
				int num = list.IndexOf(this);
				CameraJumper.TryJumpAndSelect(list[GenMath.PositiveMod(num - 1, list.Count)].parent, CameraJumper.MovementMode.Pan);
			}
		}

		// Token: 0x06010DDD RID: 69085 RVA: 0x004DADA0 File Offset: 0x004D8FA0
		private void SelectAllInGroup()
		{
			List<CompTransporter> list = this.TransportersInGroup(this.Map);
			if (list == null)
			{
				return;
			}
			Selector selector = Find.Selector;
			selector.ClearSelection();
			for (int i = 0; i < list.Count; i++)
			{
				selector.Select(list[i].parent, true, true);
			}
		}

		// Token: 0x06010DDE RID: 69086 RVA: 0x004DADF0 File Offset: 0x004D8FF0
		private void SelectNextInGroup()
		{
			List<CompTransporter> list = this.TransportersInGroup(this.Map);
			if (list != null)
			{
				int num = list.IndexOf(this);
				CameraJumper.TryJumpAndSelect(list[(num + 1) % list.Count].parent, CameraJumper.MovementMode.Pan);
			}
		}

		// Token: 0x0400AF9E RID: 44958
		public int groupID = -1;

		// Token: 0x0400AF9F RID: 44959
		public ThingOwner innerContainer;

		// Token: 0x0400AFA0 RID: 44960
		public List<TransferableOneWay> leftToLoad;

		// Token: 0x0400AFA1 RID: 44961
		private bool notifiedCantLoadMore;

		// Token: 0x0400AFA2 RID: 44962
		public float massCapacityOverride = -1f;

		// Token: 0x0400AFA3 RID: 44963
		private CompLaunchable cachedCompLaunchable;

		// Token: 0x0400AFA4 RID: 44964
		private CompShuttle cachedCompShuttle;

		// Token: 0x0400AFA5 RID: 44965
		private bool massUsageDirty = true;

		// Token: 0x0400AFA6 RID: 44966
		private float cachedMassUsage;

		// Token: 0x0400AFA7 RID: 44967
		public static readonly Texture2D CancelLoadCommandTex = ContentFinder<Texture2D>.Get("UI/Designators/Cancel", true);

		// Token: 0x0400AFA8 RID: 44968
		private static readonly Texture2D LoadCommandTex = ContentFinder<Texture2D>.Get("UI/Commands/LoadTransporter", true);

		// Token: 0x0400AFA9 RID: 44969
		private static readonly Texture2D SelectPreviousInGroupCommandTex = ContentFinder<Texture2D>.Get("UI/Commands/SelectPreviousTransporter", true);

		// Token: 0x0400AFAA RID: 44970
		private static readonly Texture2D SelectAllInGroupCommandTex = ContentFinder<Texture2D>.Get("UI/Commands/SelectAllTransporters", true);

		// Token: 0x0400AFAB RID: 44971
		private static readonly Texture2D SelectNextInGroupCommandTex = ContentFinder<Texture2D>.Get("UI/Commands/SelectNextTransporter", true);

		// Token: 0x0400AFAC RID: 44972
		private List<Thing> tmpThings = new List<Thing>();

		// Token: 0x0400AFAD RID: 44973
		private List<Pawn> tmpSavedPawns = new List<Pawn>();

		// Token: 0x0400AFAE RID: 44974
		private static List<CompTransporter> tmpTransportersInGroup = new List<CompTransporter>();
	}
}
