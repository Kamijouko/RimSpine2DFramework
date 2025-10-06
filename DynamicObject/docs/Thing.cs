using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse.AI;
using Verse.Sound;

namespace Verse
{
	// Token: 0x020008D7 RID: 2263
	public class Thing : Entity, ISelectable, ILoadReferenceable, ISignalReceiver, IExposable, IEquatable<Thing>
	{
		// Token: 0x17000A93 RID: 2707
		// (get) Token: 0x0600392A RID: 14634 RVA: 0x00130C98 File Offset: 0x0012EE98
		// (set) Token: 0x0600392B RID: 14635 RVA: 0x00130CA0 File Offset: 0x0012EEA0
		public virtual int HitPoints
		{
			get
			{
				return this.hitPointsInt;
			}
			set
			{
				this.hitPointsInt = value;
			}
		}

		// Token: 0x17000A94 RID: 2708
		// (get) Token: 0x0600392C RID: 14636 RVA: 0x00130CA9 File Offset: 0x0012EEA9
		public int MaxHitPoints
		{
			get
			{
				return Mathf.RoundToInt(this.GetStatValue(StatDefOf.MaxHitPoints, true, 10));
			}
		}

		// Token: 0x17000A95 RID: 2709
		// (get) Token: 0x0600392D RID: 14637 RVA: 0x00130CBE File Offset: 0x0012EEBE
		public virtual float MarketValue
		{
			get
			{
				return this.GetStatValue(StatDefOf.MarketValue, true, -1);
			}
		}

		// Token: 0x17000A96 RID: 2710
		// (get) Token: 0x0600392E RID: 14638 RVA: 0x00130CCD File Offset: 0x0012EECD
		public virtual float RoyalFavorValue
		{
			get
			{
				return this.GetStatValue(StatDefOf.RoyalFavorValue, true, -1);
			}
		}

		// Token: 0x17000A97 RID: 2711
		// (get) Token: 0x0600392F RID: 14639 RVA: 0x00130CDC File Offset: 0x0012EEDC
		public virtual int? OverrideGraphicIndex
		{
			get
			{
				return this.overrideGraphicIndex;
			}
		}

		// Token: 0x17000A98 RID: 2712
		// (get) Token: 0x06003930 RID: 14640 RVA: 0x00002C42 File Offset: 0x00000E42
		public virtual Texture UIIconOverride
		{
			get
			{
				return null;
			}
		}

		// Token: 0x17000A99 RID: 2713
		// (get) Token: 0x06003931 RID: 14641 RVA: 0x00130CE4 File Offset: 0x0012EEE4
		// (set) Token: 0x06003932 RID: 14642 RVA: 0x00130CEC File Offset: 0x0012EEEC
		public bool EverSeenByPlayer
		{
			get
			{
				return this.GetEverSeenByPlayer();
			}
			set
			{
				this.SetEverSeenByPlayer(value);
			}
		}

		// Token: 0x17000A9A RID: 2714
		// (get) Token: 0x06003933 RID: 14643 RVA: 0x00130CF5 File Offset: 0x0012EEF5
		// (set) Token: 0x06003934 RID: 14644 RVA: 0x00130CFD File Offset: 0x0012EEFD
		public virtual ThingStyleDef StyleDef
		{
			get
			{
				return this.GetStyleDef();
			}
			set
			{
				this.styleGraphicInt = null;
				this.SetStyleDef(value);
			}
		}

		// Token: 0x17000A9B RID: 2715
		// (get) Token: 0x06003935 RID: 14645 RVA: 0x00130D0D File Offset: 0x0012EF0D
		// (set) Token: 0x06003936 RID: 14646 RVA: 0x00130D15 File Offset: 0x0012EF15
		public Precept_ThingStyle StyleSourcePrecept
		{
			get
			{
				return this.GetStyleSourcePrecept();
			}
			set
			{
				this.SetStyleSourcePrecept(value);
			}
		}

		// Token: 0x17000A9C RID: 2716
		// (get) Token: 0x06003937 RID: 14647 RVA: 0x00130D20 File Offset: 0x0012EF20
		public bool FlammableNow
		{
			get
			{
				if (this.GetStatValue(StatDefOf.Flammability, true, -1) < 0.01f)
				{
					return false;
				}
				if (this.Spawned && !this.FireBulwark)
				{
					List<Thing> thingList = this.Position.GetThingList(this.Map);
					if (thingList != null)
					{
						for (int i = 0; i < thingList.Count; i++)
						{
							if (thingList[i].FireBulwark)
							{
								return false;
							}
						}
					}
				}
				return true;
			}
		}

		// Token: 0x17000A9D RID: 2717
		// (get) Token: 0x06003938 RID: 14648 RVA: 0x00130D8A File Offset: 0x0012EF8A
		public virtual bool FireBulwark
		{
			get
			{
				return this.def.Fillage == FillCategory.Full;
			}
		}

		// Token: 0x17000A9E RID: 2718
		// (get) Token: 0x06003939 RID: 14649 RVA: 0x00130D9A File Offset: 0x0012EF9A
		public bool Destroyed
		{
			get
			{
				return this.mapIndexOrState == -2 || this.mapIndexOrState == -3;
			}
		}

		// Token: 0x17000A9F RID: 2719
		// (get) Token: 0x0600393A RID: 14650 RVA: 0x00130DB2 File Offset: 0x0012EFB2
		public bool Discarded
		{
			get
			{
				return this.mapIndexOrState == -3;
			}
		}

		// Token: 0x17000AA0 RID: 2720
		// (get) Token: 0x0600393B RID: 14651 RVA: 0x00130DC0 File Offset: 0x0012EFC0
		public bool Spawned
		{
			get
			{
				if (this.mapIndexOrState < 0 || Find.Maps == null)
				{
					return false;
				}
				if ((int)this.mapIndexOrState < Find.Maps.Count)
				{
					return true;
				}
				Log.ErrorOnce(string.Format("Thing {0} is associated with invalid map index {1}", this.ThingID, this.mapIndexOrState), 64664487);
				return false;
			}
		}

		// Token: 0x17000AA1 RID: 2721
		// (get) Token: 0x0600393C RID: 14652 RVA: 0x00130E19 File Offset: 0x0012F019
		public bool SpawnedOrAnyParentSpawned
		{
			get
			{
				return this.SpawnedParentOrMe != null;
			}
		}

		// Token: 0x17000AA2 RID: 2722
		// (get) Token: 0x0600393D RID: 14653 RVA: 0x00130E24 File Offset: 0x0012F024
		public Thing SpawnedParentOrMe
		{
			get
			{
				if (this.Spawned)
				{
					return this;
				}
				if (this.ParentHolder != null)
				{
					return ThingOwnerUtility.SpawnedParentOrMe(this.ParentHolder);
				}
				return null;
			}
		}

		// Token: 0x17000AA3 RID: 2723
		// (get) Token: 0x0600393E RID: 14654 RVA: 0x00130E45 File Offset: 0x0012F045
		public int TickSpawned
		{
			get
			{
				return this.spawnedTick;
			}
		}

		// Token: 0x17000AA4 RID: 2724
		// (get) Token: 0x0600393F RID: 14655 RVA: 0x00130E4D File Offset: 0x0012F04D
		public int TickDeSpawned
		{
			get
			{
				return this.despawnedTick;
			}
		}

		// Token: 0x17000AA5 RID: 2725
		// (get) Token: 0x06003940 RID: 14656 RVA: 0x00130E55 File Offset: 0x0012F055
		public Map Map
		{
			get
			{
				if (this.mapIndexOrState < 0)
				{
					return null;
				}
				List<Map> maps = Find.Maps;
				if (maps == null)
				{
					return null;
				}
				return maps[(int)this.mapIndexOrState];
			}
		}

		// Token: 0x17000AA6 RID: 2726
		// (get) Token: 0x06003941 RID: 14657 RVA: 0x00130E78 File Offset: 0x0012F078
		public Map MapHeld
		{
			get
			{
				if (this.Spawned)
				{
					return this.Map;
				}
				if (this.ParentHolder == null)
				{
					return null;
				}
				return ThingOwnerUtility.GetRootMap(this.ParentHolder);
			}
		}

		// Token: 0x17000AA7 RID: 2727
		// (get) Token: 0x06003942 RID: 14658 RVA: 0x00130E9E File Offset: 0x0012F09E
		// (set) Token: 0x06003943 RID: 14659 RVA: 0x00130EA8 File Offset: 0x0012F0A8
		public IntVec3 Position
		{
			get
			{
				return this.positionInt;
			}
			set
			{
				if (value == this.positionInt)
				{
					return;
				}
				if (this.Spawned)
				{
					if (this.def.AffectsRegions)
					{
						Log.Warning("Changed position of a spawned thing which affects regions. This is not supported.");
					}
					this.DirtyMapMesh(this.Map);
					RegionListersUpdater.DeregisterInRegions(this, this.Map);
					this.Map.thingGrid.Deregister(this, false);
					this.Map.coverGrid.DeRegister(this);
				}
				this.positionInt = value;
				if (this.Spawned)
				{
					this.Map.thingGrid.Register(this);
					this.Map.coverGrid.Register(this);
					this.Map.gasGrid.Notify_ThingSpawned(this);
					RegionListersUpdater.RegisterInRegions(this, this.Map);
					this.DirtyMapMesh(this.Map);
					if (this.def.AffectsReachability)
					{
						this.Map.reachability.ClearCache();
					}
				}
			}
		}

		// Token: 0x17000AA8 RID: 2728
		// (get) Token: 0x06003944 RID: 14660 RVA: 0x00130F98 File Offset: 0x0012F198
		public IntVec3 PositionHeld
		{
			get
			{
				if (this.Spawned)
				{
					return this.Position;
				}
				IntVec3 rootPosition = ThingOwnerUtility.GetRootPosition(this.ParentHolder);
				if (rootPosition.IsValid)
				{
					return rootPosition;
				}
				return this.Position;
			}
		}

		// Token: 0x17000AA9 RID: 2729
		// (get) Token: 0x06003945 RID: 14661 RVA: 0x00130FD1 File Offset: 0x0012F1D1
		// (set) Token: 0x06003946 RID: 14662 RVA: 0x00130FDC File Offset: 0x0012F1DC
		public Rot4 Rotation
		{
			get
			{
				return this.rotationInt;
			}
			set
			{
				if (value == this.rotationInt || this.debugRotLocked)
				{
					return;
				}
				if (this.Spawned && (this.def.size.x != 1 || this.def.size.z != 1))
				{
					if (this.def.AffectsRegions)
					{
						Log.Warning("Changed rotation of a spawned non-single-cell thing which affects regions. This is not supported.");
					}
					RegionListersUpdater.DeregisterInRegions(this, this.Map);
					this.Map.thingGrid.Deregister(this, false);
				}
				this.rotationInt = value;
				if (this.Spawned && (this.def.size.x != 1 || this.def.size.z != 1))
				{
					this.Map.thingGrid.Register(this);
					RegionListersUpdater.RegisterInRegions(this, this.Map);
					this.Map.gasGrid.Notify_ThingSpawned(this);
					if (this.def.AffectsReachability)
					{
						this.Map.reachability.ClearCache();
					}
				}
			}
		}

		// Token: 0x17000AAA RID: 2730
		// (get) Token: 0x06003947 RID: 14663 RVA: 0x001310E3 File Offset: 0x0012F2E3
		public bool Smeltable
		{
			get
			{
				return !this.IsRelic() && this.def.smeltable && (!this.def.MadeFromStuff || this.Stuff.smeltable);
			}
		}

		// Token: 0x17000AAB RID: 2731
		// (get) Token: 0x06003948 RID: 14664 RVA: 0x00131118 File Offset: 0x0012F318
		public bool BurnableByRecipe
		{
			get
			{
				return this.def.burnableByRecipe && (!this.def.MadeFromStuff || this.Stuff.burnableByRecipe);
			}
		}

		// Token: 0x17000AAC RID: 2732
		// (get) Token: 0x06003949 RID: 14665 RVA: 0x00131143 File Offset: 0x0012F343
		public IThingHolder ParentHolder
		{
			get
			{
				ThingOwner thingOwner = this.holdingOwner;
				if (thingOwner == null)
				{
					return null;
				}
				return thingOwner.Owner;
			}
		}

		// Token: 0x17000AAD RID: 2733
		// (get) Token: 0x0600394A RID: 14666 RVA: 0x00131156 File Offset: 0x0012F356
		public Faction Faction
		{
			get
			{
				return this.factionInt;
			}
		}

		// Token: 0x17000AAE RID: 2734
		// (get) Token: 0x0600394B RID: 14667 RVA: 0x0013115E File Offset: 0x0012F35E
		// (set) Token: 0x0600394C RID: 14668 RVA: 0x00131194 File Offset: 0x0012F394
		public string ThingID
		{
			get
			{
				if (this.def.HasThingIDNumber)
				{
					return this.def.defName + this.thingIDNumber.ToString();
				}
				return this.def.defName;
			}
			set
			{
				this.thingIDNumber = Thing.IDNumberFromThingID(value);
			}
		}

		// Token: 0x0600394D RID: 14669 RVA: 0x001311A4 File Offset: 0x0012F3A4
		public static int IDNumberFromThingID(string thingID)
		{
			string value = Regex.Match(thingID, "\\d+$").Value;
			int num = 0;
			try
			{
				CultureInfo invariantCulture = CultureInfo.InvariantCulture;
				num = Convert.ToInt32(value, invariantCulture);
			}
			catch (Exception ex)
			{
				string[] array = new string[6];
				array[0] = "Could not convert id number from thingID=";
				array[1] = thingID;
				array[2] = ", numString=";
				array[3] = value;
				array[4] = " Exception=";
				int num2 = 5;
				Exception ex2 = ex;
				array[num2] = ((ex2 != null) ? ex2.ToString() : null);
				Log.Error(string.Concat(array));
			}
			return num;
		}

		// Token: 0x17000AAF RID: 2735
		// (get) Token: 0x0600394E RID: 14670 RVA: 0x00131228 File Offset: 0x0012F428
		public IntVec2 RotatedSize
		{
			get
			{
				if (!this.rotationInt.IsHorizontal)
				{
					return this.def.size;
				}
				return new IntVec2(this.def.size.z, this.def.size.x);
			}
		}

		// Token: 0x17000AB0 RID: 2736
		// (get) Token: 0x0600394F RID: 14671 RVA: 0x00131268 File Offset: 0x0012F468
		public virtual CellRect? CustomRectForSelector
		{
			get
			{
				return null;
			}
		}

		// Token: 0x17000AB1 RID: 2737
		// (get) Token: 0x06003950 RID: 14672 RVA: 0x0013127E File Offset: 0x0012F47E
		public override string Label
		{
			get
			{
				if (this.stackCount > 1)
				{
					return this.LabelNoCount + " x" + this.stackCount.ToStringCached();
				}
				return this.LabelNoCount;
			}
		}

		// Token: 0x17000AB2 RID: 2738
		// (get) Token: 0x06003951 RID: 14673 RVA: 0x001312AB File Offset: 0x0012F4AB
		public virtual string LabelNoCount
		{
			get
			{
				return GenLabel.ThingLabel(this, 1, true, true);
			}
		}

		// Token: 0x17000AB3 RID: 2739
		// (get) Token: 0x06003952 RID: 14674 RVA: 0x001312B6 File Offset: 0x0012F4B6
		public override string LabelCap
		{
			get
			{
				return this.Label.CapitalizeFirst(this.def);
			}
		}

		// Token: 0x17000AB4 RID: 2740
		// (get) Token: 0x06003953 RID: 14675 RVA: 0x001312C9 File Offset: 0x0012F4C9
		public virtual string LabelCapNoCount
		{
			get
			{
				return this.LabelNoCount.CapitalizeFirst(this.def);
			}
		}

		// Token: 0x17000AB5 RID: 2741
		// (get) Token: 0x06003954 RID: 14676 RVA: 0x001312DC File Offset: 0x0012F4DC
		public override string LabelShort
		{
			get
			{
				return this.LabelNoCount;
			}
		}

		// Token: 0x17000AB6 RID: 2742
		// (get) Token: 0x06003955 RID: 14677 RVA: 0x001312E4 File Offset: 0x0012F4E4
		public virtual string LabelNoParenthesis
		{
			get
			{
				return GenLabel.ThingLabel(this, 1, false, false);
			}
		}

		// Token: 0x17000AB7 RID: 2743
		// (get) Token: 0x06003956 RID: 14678 RVA: 0x001312EF File Offset: 0x0012F4EF
		public string LabelNoParenthesisCap
		{
			get
			{
				return this.LabelNoParenthesis.CapitalizeFirst();
			}
		}

		// Token: 0x17000AB8 RID: 2744
		// (get) Token: 0x06003957 RID: 14679 RVA: 0x001312FC File Offset: 0x0012F4FC
		public virtual ModContentPack ContentSource
		{
			get
			{
				return this.def.modContentPack;
			}
		}

		// Token: 0x17000AB9 RID: 2745
		// (get) Token: 0x06003958 RID: 14680 RVA: 0x00131309 File Offset: 0x0012F509
		public virtual bool IngestibleNow
		{
			get
			{
				return !this.IsBurning() && this.def.IsIngestible;
			}
		}

		// Token: 0x17000ABA RID: 2746
		// (get) Token: 0x06003959 RID: 14681 RVA: 0x00131320 File Offset: 0x0012F520
		public ThingDef Stuff
		{
			get
			{
				return this.stuffInt;
			}
		}

		// Token: 0x17000ABB RID: 2747
		// (get) Token: 0x0600395A RID: 14682 RVA: 0x00131328 File Offset: 0x0012F528
		public Graphic DefaultGraphic
		{
			get
			{
				if (this.graphicInt == null)
				{
					if (this.def.graphicData == null)
					{
						return BaseContent.BadGraphic;
					}
					this.graphicInt = this.def.graphicData.GraphicColoredFor(this);
				}
				return this.graphicInt;
			}
		}

		// Token: 0x17000ABC RID: 2748
		// (get) Token: 0x0600395B RID: 14683 RVA: 0x00131364 File Offset: 0x0012F564
		public virtual Graphic Graphic
		{
			get
			{
				ThingStyleDef styleDef = this.StyleDef;
				if (((styleDef != null) ? styleDef.Graphic : null) != null)
				{
					if (this.styleGraphicInt == null)
					{
						if (styleDef.graphicData != null)
						{
							this.styleGraphicInt = styleDef.graphicData.GraphicColoredFor(this);
						}
						else
						{
							this.styleGraphicInt = styleDef.Graphic;
						}
					}
					return this.styleGraphicInt;
				}
				return this.DefaultGraphic;
			}
		}

		// Token: 0x17000ABD RID: 2749
		// (get) Token: 0x0600395C RID: 14684 RVA: 0x001313C3 File Offset: 0x0012F5C3
		public virtual List<IntVec3> InteractionCells
		{
			get
			{
				return ThingUtility.InteractionCellsWhenAt(this.def, this.Position, this.Rotation, this.Map, true);
			}
		}

		// Token: 0x17000ABE RID: 2750
		// (get) Token: 0x0600395D RID: 14685 RVA: 0x001313E3 File Offset: 0x0012F5E3
		public virtual IntVec3 InteractionCell
		{
			get
			{
				return ThingUtility.InteractionCellWhenAt(this.def, this.Position, this.Rotation, this.Map);
			}
		}

		// Token: 0x17000ABF RID: 2751
		// (get) Token: 0x0600395E RID: 14686 RVA: 0x00131404 File Offset: 0x0012F604
		public float AmbientTemperature
		{
			get
			{
				if (this.Spawned)
				{
					return GenTemperature.GetTemperatureForCell(this.Position, this.Map);
				}
				if (this.ParentHolder != null)
				{
					for (IThingHolder thingHolder = this.ParentHolder; thingHolder != null; thingHolder = thingHolder.ParentHolder)
					{
						float num;
						if (ThingOwnerUtility.TryGetFixedTemperature(thingHolder, this, out num))
						{
							return num;
						}
					}
				}
				if (this.SpawnedOrAnyParentSpawned)
				{
					return GenTemperature.GetTemperatureForCell(this.PositionHeld, this.MapHeld);
				}
				if (this.Tile.Valid)
				{
					return GenTemperature.GetTemperatureAtTile(this.Tile);
				}
				return 21f;
			}
		}

		// Token: 0x17000AC0 RID: 2752
		// (get) Token: 0x0600395F RID: 14687 RVA: 0x0013148E File Offset: 0x0012F68E
		public PlanetTile Tile
		{
			get
			{
				if (this.Spawned)
				{
					return this.Map.Tile;
				}
				if (this.ParentHolder != null)
				{
					return ThingOwnerUtility.GetRootTile(this.ParentHolder);
				}
				return PlanetTile.Invalid;
			}
		}

		// Token: 0x17000AC1 RID: 2753
		// (get) Token: 0x06003960 RID: 14688 RVA: 0x001314BD File Offset: 0x0012F6BD
		public virtual bool Suspended
		{
			get
			{
				return !this.Spawned && this.ParentHolder != null && ThingOwnerUtility.ContentsSuspended(this.ParentHolder);
			}
		}

		// Token: 0x17000AC2 RID: 2754
		// (get) Token: 0x06003961 RID: 14689 RVA: 0x001314DE File Offset: 0x0012F6DE
		public bool InCryptosleep
		{
			get
			{
				return !this.Spawned && this.ParentHolder != null && ThingOwnerUtility.ContentsInCryptosleep(this.ParentHolder);
			}
		}

		// Token: 0x17000AC3 RID: 2755
		// (get) Token: 0x06003962 RID: 14690 RVA: 0x001314FF File Offset: 0x0012F6FF
		public virtual string DescriptionDetailed
		{
			get
			{
				return this.def.DescriptionDetailed;
			}
		}

		// Token: 0x17000AC4 RID: 2756
		// (get) Token: 0x06003963 RID: 14691 RVA: 0x0013150C File Offset: 0x0012F70C
		public virtual string DescriptionFlavor
		{
			get
			{
				return this.def.description;
			}
		}

		// Token: 0x17000AC5 RID: 2757
		// (get) Token: 0x06003964 RID: 14692 RVA: 0x00131519 File Offset: 0x0012F719
		public bool IsOnHoldingPlatform
		{
			get
			{
				return ModsConfig.AnomalyActive && this.ParentHolder is Building_HoldingPlatform;
			}
		}

		// Token: 0x17000AC6 RID: 2758
		// (get) Token: 0x06003965 RID: 14693 RVA: 0x00131532 File Offset: 0x0012F732
		public TerrainAffordanceDef TerrainAffordanceNeeded
		{
			get
			{
				return this.def.GetTerrainAffordanceNeed(this.stuffInt);
			}
		}

		// Token: 0x17000AC7 RID: 2759
		// (get) Token: 0x06003966 RID: 14694 RVA: 0x00131545 File Offset: 0x0012F745
		public bool BeingTransportedOnGravship
		{
			get
			{
				return this.beingTransportedOnGravship;
			}
		}

		// Token: 0x17000AC8 RID: 2760
		// (get) Token: 0x06003967 RID: 14695 RVA: 0x000028E7 File Offset: 0x00000AE7
		protected virtual int MinTickIntervalRate
		{
			get
			{
				return 1;
			}
		}

		// Token: 0x17000AC9 RID: 2761
		// (get) Token: 0x06003968 RID: 14696 RVA: 0x0013154D File Offset: 0x0012F74D
		protected virtual int MaxTickIntervalRate
		{
			get
			{
				return 15;
			}
		}

		// Token: 0x17000ACA RID: 2762
		// (get) Token: 0x06003969 RID: 14697 RVA: 0x00131551 File Offset: 0x0012F751
		protected virtual int UpdateRateTickOffset
		{
			get
			{
				return this.HashOffset();
			}
		}

		// Token: 0x17000ACB RID: 2763
		// (get) Token: 0x0600396A RID: 14698 RVA: 0x00131559 File Offset: 0x0012F759
		public virtual int UpdateRateTicks
		{
			get
			{
				return GenTicks.GetCameraUpdateRate(this);
			}
		}

		// Token: 0x0600396B RID: 14699 RVA: 0x00131564 File Offset: 0x0012F764
		public void DoTick()
		{
			if (this.Destroyed)
			{
				return;
			}
			if (!this.cached)
			{
				this.cached = true;
				this.cachedHolder = this as IThingHolder;
				this.cachedTickable = this as IThingHolderTickable;
				this.cachedIsHolder = this.cachedHolder != null;
			}
			bool flag = this.holdingOwner != null && !this.Spawned;
			if (this.def.tickerType == TickerType.Normal)
			{
				using (ProfilerBlock.Scope("DoTick()"))
				{
					using (ProfilerBlock.Scope("Tick()"))
					{
						this.Tick();
					}
					if (this.Destroyed)
					{
						return;
					}
					this.tickDelta++;
					int num = Mathf.Min(Mathf.Max(this.UpdateRateTicks, this.MinTickIntervalRate), this.MaxTickIntervalRate);
					if (this.tickDelta >= num || GenTicks.IsTickInterval(this.UpdateRateTickOffset, num))
					{
						using (ProfilerBlock.Scope("TickInterval()"))
						{
							this.TickInterval(this.tickDelta);
						}
						this.tickDelta = 0;
					}
					goto IL_01AF;
				}
			}
			if (this.def.tickerType == TickerType.Rare && ((!this.cachedIsHolder && !flag) || this.IsHashIntervalTick(250)))
			{
				using (ProfilerBlock.Scope("TickRare()"))
				{
					this.TickRare();
					goto IL_01AF;
				}
			}
			if (this.def.tickerType == TickerType.Long && ((!this.cachedIsHolder && !flag) || this.IsHashIntervalTick(2000)))
			{
				using (ProfilerBlock.Scope("TickLong()"))
				{
					this.TickLong();
				}
			}
			IL_01AF:
			if (this.Destroyed)
			{
				return;
			}
			if (!this.cachedIsHolder)
			{
				return;
			}
			if (this.cachedTickable == null || this.cachedTickable.ShouldTickContents)
			{
				if (this.tmpHolders == null)
				{
					this.tmpHolders = new List<IThingHolder>(8);
				}
				this.tmpHolders.Add(this.cachedHolder);
				this.cachedHolder.GetChildHolders(this.tmpHolders);
				for (int i = 0; i < this.tmpHolders.Count; i++)
				{
					ThingOwner directlyHeldThings = this.tmpHolders[i].GetDirectlyHeldThings();
					if (directlyHeldThings != null)
					{
						directlyHeldThings.DoTick();
						if (this.Destroyed)
						{
							break;
						}
					}
				}
				this.tmpHolders.Clear();
			}
		}

		// Token: 0x0600396C RID: 14700 RVA: 0x0013180C File Offset: 0x0012FA0C
		public virtual void PostMake()
		{
			ThingIDMaker.GiveIDTo(this);
			if (this.def.useHitPoints)
			{
				this.HitPoints = Mathf.RoundToInt((float)this.MaxHitPoints * Mathf.Clamp01(this.def.startingHpRange.RandomInRange));
			}
		}

		// Token: 0x0600396D RID: 14701 RVA: 0x0013184C File Offset: 0x0012FA4C
		public virtual void PostPostMake()
		{
			if (!this.def.randomStyle.NullOrEmpty<ThingStyleChance>() && Rand.Chance(this.def.randomStyleChance))
			{
				this.StyleDef = this.def.randomStyle.RandomElementByWeight((ThingStyleChance x) => x.Chance).StyleDef;
			}
		}

		// Token: 0x0600396E RID: 14702 RVA: 0x000026EA File Offset: 0x000008EA
		public virtual void PostQualitySet()
		{
		}

		// Token: 0x0600396F RID: 14703 RVA: 0x001318B7 File Offset: 0x0012FAB7
		public string GetUniqueLoadID()
		{
			return "Thing_" + this.ThingID;
		}

		// Token: 0x06003970 RID: 14704 RVA: 0x001318CC File Offset: 0x0012FACC
		public override void SpawnSetup(Map map, bool respawningAfterLoad)
		{
			if (this.Destroyed)
			{
				Log.Error(string.Concat(new string[]
				{
					"Spawning destroyed thing ",
					(this != null) ? this.ToString() : null,
					" at ",
					this.Position.ToString(),
					". Correcting."
				}));
				this.mapIndexOrState = -1;
				if (this.HitPoints <= 0 && this.def.useHitPoints)
				{
					this.HitPoints = 1;
				}
			}
			if (this.Spawned)
			{
				Log.Error("Tried to spawn already-spawned thing " + ((this != null) ? this.ToString() : null) + " at " + this.Position.ToString());
				return;
			}
			int num = Find.Maps.IndexOf(map);
			if (num < 0)
			{
				Log.Error("Tried to spawn thing " + ((this != null) ? this.ToString() : null) + ", but the map provided does not exist.");
				return;
			}
			if (this.stackCount > this.def.stackLimit)
			{
				Log.Error(string.Concat(new string[]
				{
					"Spawned ",
					(this != null) ? this.ToString() : null,
					" with stackCount ",
					this.stackCount.ToString(),
					" but stackLimit is ",
					this.def.stackLimit.ToString(),
					". Truncating."
				}));
				this.stackCount = this.def.stackLimit;
			}
			this.mapIndexOrState = (sbyte)num;
			RegionListersUpdater.RegisterInRegions(this, map);
			if (!map.spawnedThings.TryAdd(this, false))
			{
				Log.Error("Couldn't add thing " + ((this != null) ? this.ToString() : null) + " to spawned things.");
			}
			map.listerThings.Add(this);
			map.thingGrid.Register(this);
			map.gasGrid.Notify_ThingSpawned(this);
			map.mapTemperature.Notify_ThingSpawned(this);
			if (map.IsPlayerHome)
			{
				this.EverSeenByPlayer = true;
			}
			if (Find.TickManager != null)
			{
				Find.TickManager.RegisterAllTickabilityFor(this);
			}
			this.DirtyMapMesh(map);
			if (this.def.drawerType != DrawerType.MapMeshOnly)
			{
				map.dynamicDrawManager.RegisterDrawable(this);
			}
			map.tooltipGiverList.Notify_ThingSpawned(this);
			if (this.def.CanAffectLinker)
			{
				map.linkGrid.Notify_LinkerCreatedOrDestroyed(this);
				map.mapDrawer.MapMeshDirty(this.Position, MapMeshFlagDefOf.Things, true, false);
			}
			if (!this.def.CanOverlapZones)
			{
				map.zoneManager.Notify_NoZoneOverlapThingSpawned(this);
			}
			if (this.def.AffectsRegions)
			{
				map.regionDirtyer.Notify_ThingAffectingRegionsSpawned(this);
			}
			if (this.def.pathCost != 0 || this.def.passability == Traversability.Impassable)
			{
				map.pathing.RecalculatePerceivedPathCostUnderThing(this);
			}
			if (this.def.AffectsReachability)
			{
				map.reachability.ClearCache();
			}
			map.coverGrid.Register(this);
			if (this.def.category == ThingCategory.Item)
			{
				map.listerHaulables.Notify_Spawned(this);
				map.listerMergeables.Notify_Spawned(this);
			}
			map.attackTargetsCache.Notify_ThingSpawned(this);
			Region validRegionAt_NoRebuild = map.regionGrid.GetValidRegionAt_NoRebuild(this.Position);
			if (validRegionAt_NoRebuild != null)
			{
				Room room = validRegionAt_NoRebuild.Room;
				if (room != null)
				{
					room.Notify_ContainedThingSpawnedOrDespawned(this);
				}
			}
			StealAIDebugDrawer.Notify_ThingChanged(this);
			IHaulDestination haulDestination = this as IHaulDestination;
			if (haulDestination != null)
			{
				map.haulDestinationManager.AddHaulDestination(haulDestination);
			}
			IHaulSource haulSource = this as IHaulSource;
			if (haulSource != null)
			{
				map.haulDestinationManager.AddHaulSource(haulSource);
			}
			if (this is IThingHolder && Find.ColonistBar != null)
			{
				Find.ColonistBar.MarkColonistsDirty();
			}
			if (this.def.category == ThingCategory.Item)
			{
				SlotGroup slotGroup = this.Position.GetSlotGroup(map);
				ISlotGroupParent slotGroupParent = ((slotGroup != null) ? slotGroup.parent : null);
				if (slotGroupParent != null)
				{
					slotGroupParent.Notify_ReceivedThing(this);
					GenThing.TryDirtyAdjacentGroupContainers(slotGroupParent, map);
				}
			}
			if (this.def.receivesSignals)
			{
				Find.SignalManager.RegisterReceiver(this);
			}
			if (!this.BeingTransportedOnGravship)
			{
				SoundDef soundSpawned = this.def.soundSpawned;
				if (soundSpawned != null)
				{
					soundSpawned.PlayOneShot(this);
				}
				if (!respawningAfterLoad)
				{
					QuestUtility.SendQuestTargetSignals(this.questTags, "Spawned", this.Named("SUBJECT"));
					this.spawnedTick = Find.TickManager.TicksGame;
					this.despawnedTick = -1;
					List<EntityCodexEntryDef> list;
					if (AnomalyUtility.ShouldNotifyCodex(this, EntityDiscoveryType.Spawn, out list))
					{
						Find.EntityCodex.SetDiscovered(list, this.def, this);
					}
					else
					{
						Find.HiddenItemsManager.SetDiscovered(this.def);
					}
				}
			}
			map.events.Notify_ThingSpawned(this);
		}

		// Token: 0x06003971 RID: 14705 RVA: 0x00131D4C File Offset: 0x0012FF4C
		public bool DeSpawnOrDeselect(DestroyMode mode = DestroyMode.Vanish)
		{
			bool flag = Current.ProgramState == ProgramState.Playing && Find.Selector.IsSelected(this);
			if (this.Spawned)
			{
				this.DeSpawn(mode);
			}
			else if (flag)
			{
				Find.Selector.Deselect(this);
				Find.MainButtonsRoot.tabs.Notify_SelectedObjectDespawned();
			}
			return flag;
		}

		// Token: 0x06003972 RID: 14706 RVA: 0x00131DA0 File Offset: 0x0012FFA0
		public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
		{
			if (this.Destroyed)
			{
				Log.Error("Tried to despawn " + this.ToStringSafe<Thing>() + " which is already destroyed.");
				return;
			}
			if (!this.Spawned)
			{
				Log.Error("Tried to despawn " + this.ToStringSafe<Thing>() + " which is not spawned.");
				return;
			}
			Map map = this.Map;
			map.overlayDrawer.DisposeHandle(this);
			RegionListersUpdater.DeregisterInRegions(this, map);
			map.spawnedThings.Remove(this);
			map.listerThings.Remove(this);
			map.thingGrid.Deregister(this, false);
			map.coverGrid.DeRegister(this);
			if (this.def.receivesSignals)
			{
				Find.SignalManager.DeregisterReceiver(this);
			}
			map.tooltipGiverList.Notify_ThingDespawned(this);
			if (this.def.CanAffectLinker)
			{
				map.linkGrid.Notify_LinkerCreatedOrDestroyed(this);
				map.mapDrawer.MapMeshDirty(this.Position, MapMeshFlagDefOf.Things, true, false);
			}
			if (Find.Selector.IsSelected(this))
			{
				Find.Selector.Deselect(this);
				Find.MainButtonsRoot.tabs.Notify_SelectedObjectDespawned();
			}
			this.DirtyMapMesh(map);
			if (this.def.drawerType != DrawerType.MapMeshOnly)
			{
				map.dynamicDrawManager.DeRegisterDrawable(this);
			}
			Region validRegionAt_NoRebuild = map.regionGrid.GetValidRegionAt_NoRebuild(this.Position);
			if (validRegionAt_NoRebuild != null)
			{
				Room room = validRegionAt_NoRebuild.Room;
				if (room != null)
				{
					room.Notify_ContainedThingSpawnedOrDespawned(this);
				}
			}
			if (this.def.AffectsRegions)
			{
				map.regionDirtyer.Notify_ThingAffectingRegionsDespawned(this);
			}
			if (this.def.pathCost != 0 || this.def.passability == Traversability.Impassable)
			{
				map.pathing.RecalculatePerceivedPathCostUnderThing(this);
			}
			if (this.def.AffectsReachability)
			{
				map.reachability.ClearCache();
			}
			Find.TickManager.DeRegisterAllTickabilityFor(this);
			this.mapIndexOrState = -1;
			if (this.def.category == ThingCategory.Item)
			{
				map.listerHaulables.Notify_DeSpawned(this);
				map.listerMergeables.Notify_DeSpawned(this);
			}
			map.attackTargetsCache.Notify_ThingDespawned(this);
			map.physicalInteractionReservationManager.ReleaseAllForTarget(this);
			IHaulEnroute haulEnroute = this as IHaulEnroute;
			if (haulEnroute != null)
			{
				map.enrouteManager.Notify_ContainerDespawned(haulEnroute);
			}
			StealAIDebugDrawer.Notify_ThingChanged(this);
			IHaulDestination haulDestination = this as IHaulDestination;
			if (haulDestination != null)
			{
				map.haulDestinationManager.RemoveHaulDestination(haulDestination);
			}
			IHaulSource haulSource = this as IHaulSource;
			if (haulSource != null)
			{
				map.haulDestinationManager.RemoveHaulSource(haulSource);
			}
			if (this is IThingHolder && Find.ColonistBar != null)
			{
				Find.ColonistBar.MarkColonistsDirty();
			}
			if (this.def.category == ThingCategory.Item)
			{
				SlotGroup slotGroup = this.Position.GetSlotGroup(map);
				ISlotGroupParent slotGroupParent = ((slotGroup != null) ? slotGroup.parent : null);
				if (slotGroupParent != null)
				{
					slotGroupParent.Notify_LostThing(this);
					GenThing.TryDirtyAdjacentGroupContainers(slotGroupParent, map);
				}
			}
			QuestUtility.SendQuestTargetSignals(this.questTags, "Despawned", this.Named("SUBJECT"));
			this.spawnedTick = -1;
			this.despawnedTick = Find.TickManager.TicksGame;
			map.events.Notify_ThingDespawned(this);
		}

		// Token: 0x06003973 RID: 14707 RVA: 0x00132091 File Offset: 0x00130291
		public virtual void Kill(DamageInfo? dinfo = null, Hediff exactCulprit = null)
		{
			this.Destroy(DestroyMode.KillFinalize);
		}

		// Token: 0x06003974 RID: 14708 RVA: 0x0013209C File Offset: 0x0013029C
		public virtual void Destroy(DestroyMode mode = DestroyMode.Vanish)
		{
			if (!Thing.allowDestroyNonDestroyable && !this.def.destroyable)
			{
				Log.Error("Tried to destroy non-destroyable thing " + ((this != null) ? this.ToString() : null));
				return;
			}
			if (this.Destroyed)
			{
				Log.Error("Tried to destroy already-destroyed thing " + ((this != null) ? this.ToString() : null));
				return;
			}
			bool spawned = this.Spawned;
			Map map = this.Map;
			if (this.StyleSourcePrecept != null)
			{
				this.StyleSourcePrecept.Notify_ThingLost(this, spawned);
			}
			if (this.Spawned)
			{
				this.DeSpawn(mode);
			}
			else if (Current.ProgramState == ProgramState.Playing && Find.Selector.IsSelected(this))
			{
				Find.Selector.Deselect(this);
				Find.MainButtonsRoot.tabs.Notify_SelectedObjectDespawned();
			}
			this.mapIndexOrState = -2;
			if (this.def.DiscardOnDestroyed)
			{
				this.Discard(false);
			}
			CompExplosive compExplosive = this.TryGetComp<CompExplosive>();
			if (spawned)
			{
				List<Thing> list = new List<Thing>();
				GenLeaving.DoLeavingsFor(this, map, mode, list);
				if (compExplosive != null)
				{
					compExplosive.AddThingsIgnoredByExplosion(list);
				}
				this.Notify_KilledLeavingsLeft(list);
			}
			if (this.holdingOwner != null)
			{
				this.holdingOwner.Notify_ContainedItemDestroyed(this);
			}
			this.RemoveAllReservationsAndDesignationsOnThis();
			if (!(this is Pawn))
			{
				this.stackCount = 0;
			}
			if (mode != DestroyMode.QuestLogic)
			{
				QuestUtility.SendQuestTargetSignals(this.questTags, "Destroyed", this.Named("SUBJECT"));
			}
			if (mode == DestroyMode.KillFinalize)
			{
				QuestUtility.SendQuestTargetSignals(this.questTags, "Killed", this.Named("SUBJECT"), map.Named("MAP"));
			}
		}

		// Token: 0x06003975 RID: 14709 RVA: 0x000026EA File Offset: 0x000008EA
		public virtual void PreTraded(TradeAction action, Pawn playerNegotiator, ITrader trader)
		{
		}

		// Token: 0x06003976 RID: 14710 RVA: 0x0013221A File Offset: 0x0013041A
		public virtual void PostGeneratedForTrader(TraderKindDef trader, PlanetTile forTile, Faction forFaction)
		{
			if (this.def.colorGeneratorInTraderStock != null)
			{
				this.SetColor(this.def.colorGeneratorInTraderStock.NewRandomizedColor(), true);
			}
		}

		// Token: 0x06003977 RID: 14711 RVA: 0x00132240 File Offset: 0x00130440
		public virtual float GetBeauty(bool outside)
		{
			if (!outside || !this.def.StatBaseDefined(StatDefOf.BeautyOutdoors))
			{
				return this.GetStatValue(StatDefOf.Beauty, true, -1);
			}
			return this.GetStatValue(StatDefOf.BeautyOutdoors, true, -1);
		}

		// Token: 0x06003978 RID: 14712 RVA: 0x00132274 File Offset: 0x00130474
		public virtual void Notify_MyMapRemoved()
		{
			if (this.def.receivesSignals)
			{
				Find.SignalManager.DeregisterReceiver(this);
			}
			if (this.StyleSourcePrecept != null)
			{
				this.StyleSourcePrecept.Notify_ThingLost(this, false);
			}
			if (!ThingOwnerUtility.AnyParentIs<Pawn>(this))
			{
				this.mapIndexOrState = -3;
			}
			ThingOwner thingOwner = this.holdingOwner;
			if (thingOwner != null && thingOwner.Owner is Map)
			{
				this.holdingOwner = null;
			}
			this.RemoveAllReservationsAndDesignationsOnThis();
		}

		// Token: 0x06003979 RID: 14713 RVA: 0x000026EA File Offset: 0x000008EA
		public virtual void Notify_LordDestroyed()
		{
		}

		// Token: 0x0600397A RID: 14714 RVA: 0x000026EA File Offset: 0x000008EA
		public virtual void Notify_AbandonedAtTile(PlanetTile tile)
		{
		}

		// Token: 0x0600397B RID: 14715 RVA: 0x000026EA File Offset: 0x000008EA
		public virtual void Notify_KilledLeavingsLeft(List<Thing> leavings)
		{
		}

		// Token: 0x0600397C RID: 14716 RVA: 0x000026EA File Offset: 0x000008EA
		public virtual void Notify_Studied(Pawn studier, float amount, KnowledgeCategoryDef category = null)
		{
		}

		// Token: 0x0600397D RID: 14717 RVA: 0x001322E4 File Offset: 0x001304E4
		public virtual void Notify_Unfogged()
		{
			if (this.beenRevealed)
			{
				return;
			}
			this.beenRevealed = true;
			List<EntityCodexEntryDef> list;
			if (ModsConfig.AnomalyActive && AnomalyUtility.ShouldNotifyCodex(this, EntityDiscoveryType.Unfog, out list))
			{
				Find.EntityCodex.SetDiscovered(list, this.def, this);
			}
			else
			{
				Find.HiddenItemsManager.SetDiscovered(this.def);
			}
			QuestUtility.SendQuestTargetSignals(this.questTags, "Unfogged", this);
			CompLetterOnRevealed compLetterOnRevealed = this.TryGetComp<CompLetterOnRevealed>();
			if (compLetterOnRevealed != null)
			{
				Find.LetterStack.ReceiveLetter(compLetterOnRevealed.Props.label, compLetterOnRevealed.Props.text, compLetterOnRevealed.Props.letterDef, this, null, null, null, null, 0, true);
			}
		}

		// Token: 0x0600397E RID: 14718 RVA: 0x00132398 File Offset: 0x00130598
		public void ForceSetStateToUnspawned()
		{
			this.mapIndexOrState = -1;
		}

		// Token: 0x0600397F RID: 14719 RVA: 0x001323A4 File Offset: 0x001305A4
		public void DecrementMapIndex()
		{
			if (this.mapIndexOrState <= 0)
			{
				Log.Warning("Tried to decrement map index for " + ((this != null) ? this.ToString() : null) + ", but mapIndexOrState=" + this.mapIndexOrState.ToString());
				return;
			}
			this.mapIndexOrState -= 1;
		}

		// Token: 0x06003980 RID: 14720 RVA: 0x001323F8 File Offset: 0x001305F8
		private void RemoveAllReservationsAndDesignationsOnThis()
		{
			if (this.def.category == ThingCategory.Mote)
			{
				return;
			}
			List<Map> maps = Find.Maps;
			for (int i = 0; i < maps.Count; i++)
			{
				maps[i].reservationManager.ReleaseAllForTarget(this);
				maps[i].physicalInteractionReservationManager.ReleaseAllForTarget(this);
				IAttackTarget attackTarget = this as IAttackTarget;
				if (attackTarget != null)
				{
					maps[i].attackTargetReservationManager.ReleaseAllForTarget(attackTarget);
				}
				maps[i].designationManager.RemoveAllDesignationsOn(this, false);
			}
		}

		// Token: 0x06003981 RID: 14721 RVA: 0x00132484 File Offset: 0x00130684
		public virtual void ExposeData()
		{
			Scribe_Defs.Look<ThingDef>(ref this.def, "def");
			Scribe_Values.Look<int>(ref this.tickDelta, "tickDelta", 0, false);
			if (this.def.HasThingIDNumber)
			{
				string thingID = this.ThingID;
				Scribe_Values.Look<string>(ref thingID, "id", null, false);
				if (Scribe.mode != LoadSaveMode.Saving)
				{
					this.ThingID = thingID;
				}
			}
			Scribe_Values.Look<sbyte>(ref this.mapIndexOrState, "map", -1, false);
			if (Scribe.mode == LoadSaveMode.LoadingVars && this.mapIndexOrState >= 0)
			{
				this.mapIndexOrState = -1;
			}
			Scribe_Values.Look<IntVec3>(ref this.positionInt, "pos", IntVec3.Invalid, false);
			Scribe_Values.Look<Rot4>(ref this.rotationInt, "rot", Rot4.North, false);
			Scribe_Values.Look<bool>(ref this.debugRotLocked, "debugRotLocked", false, false);
			if (this.def.useHitPoints)
			{
				Scribe_Values.Look<int>(ref this.hitPointsInt, "health", -1, false);
			}
			bool flag = this.def.tradeability != Tradeability.None && this.def.category == ThingCategory.Item;
			if (this.def.stackLimit > 1 || flag)
			{
				Scribe_Values.Look<int>(ref this.stackCount, "stackCount", 0, true);
			}
			Scribe_Defs.Look<ThingDef>(ref this.stuffInt, "stuff");
			string facID = ((this.factionInt != null) ? this.factionInt.GetUniqueLoadID() : "null");
			Scribe_Values.Look<string>(ref facID, "faction", "null", false);
			if (Scribe.mode == LoadSaveMode.LoadingVars)
			{
				if (facID == "null")
				{
					this.factionInt = null;
				}
				else if (Find.World != null && Find.FactionManager != null)
				{
					this.factionInt = Find.FactionManager.AllFactions.FirstOrDefault((Faction fa) => fa.GetUniqueLoadID() == facID);
				}
				else
				{
					Thing.facIDsCached.SetOrAdd(this, facID);
				}
			}
			if (Scribe.mode == LoadSaveMode.ResolvingCrossRefs)
			{
				if (facID == "null" && Thing.facIDsCached.TryGetValue(this, out facID))
				{
					Thing.facIDsCached.Remove(this);
				}
				if (facID != "null")
				{
					this.factionInt = Find.FactionManager.AllFactions.FirstOrDefault((Faction fa) => fa.GetUniqueLoadID() == facID);
				}
			}
			if (Scribe.mode == LoadSaveMode.PostLoadInit)
			{
				Thing.facIDsCached.Clear();
				if (this.def.MadeFromStuff && this.Stuff == null)
				{
					Log.Error(string.Format("{0} is made from stuff but has no stuff set. Setting default stuff.", this));
					this.SetStuffDirect(GenStuff.DefaultStuffFor(this.def));
					if (this.Stuff == null)
					{
						Log.Error(string.Format("Failed to find stuff for {0} after loading.", this));
					}
				}
			}
			Scribe_Collections.Look<string>(ref this.questTags, "questTags", LookMode.Value, Array.Empty<object>());
			Scribe_Values.Look<int?>(ref this.overrideGraphicIndex, "overrideGraphicIndex", null, false);
			Scribe_Values.Look<int>(ref this.spawnedTick, "spawnedTick", -1, false);
			Scribe_Values.Look<int>(ref this.despawnedTick, "despawnedTick", 0, false);
			Scribe_Values.Look<bool>(ref this.beenRevealed, "beenRevealed", false, false);
			BackCompatibility.PostExposeData(this);
		}

		// Token: 0x06003982 RID: 14722 RVA: 0x000026EA File Offset: 0x000008EA
		public virtual void PostMapInit()
		{
		}

		// Token: 0x17000ACC RID: 2764
		// (get) Token: 0x06003983 RID: 14723 RVA: 0x00132798 File Offset: 0x00130998
		public Vector3? DrawPosHeld
		{
			get
			{
				if (this.Spawned)
				{
					return new Vector3?(this.DrawPos);
				}
				Thing thing = ThingOwnerUtility.SpawnedParentOrMe(this.ParentHolder);
				if (thing == null)
				{
					return null;
				}
				return new Vector3?(thing.DrawPos);
			}
		}

		// Token: 0x17000ACD RID: 2765
		// (get) Token: 0x06003984 RID: 14724 RVA: 0x001327DC File Offset: 0x001309DC
		public virtual Vector3 DrawPos
		{
			get
			{
				return this.TrueCenter();
			}
		}

		// Token: 0x17000ACE RID: 2766
		// (get) Token: 0x06003985 RID: 14725 RVA: 0x001327E4 File Offset: 0x001309E4
		public virtual Vector2 DrawSize
		{
			get
			{
				if (this.def.graphicData != null)
				{
					return this.def.graphicData.drawSize;
				}
				return Vector2.one;
			}
		}

		// Token: 0x17000ACF RID: 2767
		// (get) Token: 0x06003986 RID: 14726 RVA: 0x00132809 File Offset: 0x00130A09
		// (set) Token: 0x06003987 RID: 14727 RVA: 0x00132848 File Offset: 0x00130A48
		public virtual Color DrawColor
		{
			get
			{
				if (this.Stuff != null)
				{
					return this.def.GetColorForStuff(this.Stuff);
				}
				if (this.def.graphicData != null)
				{
					return this.def.graphicData.color;
				}
				return Color.white;
			}
			set
			{
				Log.Error(string.Format("Cannot set instance color on non-ThingWithComps {0} at {1}.", this.LabelCap, this.Position));
			}
		}

		// Token: 0x17000AD0 RID: 2768
		// (get) Token: 0x06003988 RID: 14728 RVA: 0x0013286A File Offset: 0x00130A6A
		public virtual Color DrawColorTwo
		{
			get
			{
				if (this.def.graphicData != null)
				{
					return this.def.graphicData.colorTwo;
				}
				return Color.white;
			}
		}

		// Token: 0x06003989 RID: 14729 RVA: 0x0013288F File Offset: 0x00130A8F
		public void DrawNowAt(Vector3 drawLoc, bool flip = false)
		{
			this.DynamicDrawPhaseAt(DrawPhase.Draw, drawLoc, flip);
		}

		// Token: 0x0600398A RID: 14730 RVA: 0x0013289A File Offset: 0x00130A9A
		public void DynamicDrawPhase(DrawPhase phase)
		{
			if (this.def.drawerType == DrawerType.MapMeshOnly)
			{
				return;
			}
			this.DynamicDrawPhaseAt(phase, this.DrawPos, false);
		}

		// Token: 0x0600398B RID: 14731 RVA: 0x001328B9 File Offset: 0x00130AB9
		public virtual void DynamicDrawPhaseAt(DrawPhase phase, Vector3 drawLoc, bool flip = false)
		{
			if (phase == DrawPhase.Draw)
			{
				this.DrawAt(drawLoc, flip);
			}
		}

		// Token: 0x0600398C RID: 14732 RVA: 0x001328C8 File Offset: 0x00130AC8
		protected virtual void DrawAt(Vector3 drawLoc, bool flip = false)
		{
			if (this.def.drawerType == DrawerType.RealtimeOnly || !this.Spawned)
			{
				this.Graphic.Draw(drawLoc, flip ? this.Rotation.Opposite : this.Rotation, this, 0f);
			}
			SilhouetteUtility.DrawGraphicSilhouette(this, drawLoc);
		}

		// Token: 0x0600398D RID: 14733 RVA: 0x0013291D File Offset: 0x00130B1D
		public virtual void Print(SectionLayer layer)
		{
			if (!this.def.dontPrint)
			{
				this.Graphic.Print(layer, this, 0f);
			}
		}

		// Token: 0x0600398E RID: 14734 RVA: 0x00132940 File Offset: 0x00130B40
		public void DirtyMapMesh(Map map)
		{
			if (this.def.drawerType != DrawerType.RealtimeOnly)
			{
				foreach (IntVec3 intVec in this.OccupiedRect())
				{
					map.mapDrawer.MapMeshDirty(intVec, MapMeshFlagDefOf.Things);
				}
			}
		}

		// Token: 0x0600398F RID: 14735 RVA: 0x001329B4 File Offset: 0x00130BB4
		public virtual void DrawGUIOverlay()
		{
			if (Find.CameraDriver.CurrentZoom == CameraZoomRange.Closest)
			{
				if (this.def.stackLimit > 1)
				{
					GenMapUI.DrawThingLabel(this, this.stackCount.ToStringCached());
					return;
				}
				QualityCategory qualityCategory;
				if (this.def.drawGUIOverlayQuality && this.TryGetQuality(out qualityCategory))
				{
					GenMapUI.DrawThingLabel(this, qualityCategory.GetLabelShort());
				}
			}
		}

		// Token: 0x06003990 RID: 14736 RVA: 0x00132A10 File Offset: 0x00130C10
		public virtual void DrawExtraSelectionOverlays()
		{
			if (this.def.specialDisplayRadius > 0.1f)
			{
				GenDraw.DrawRadiusRing(this.Position, this.def.specialDisplayRadius);
			}
			if (this.def.drawPlaceWorkersWhileSelected && this.def.PlaceWorkers != null)
			{
				for (int i = 0; i < this.def.PlaceWorkers.Count; i++)
				{
					this.def.PlaceWorkers[i].DrawGhost(this.def, this.Position, this.Rotation, Color.white, this);
				}
			}
			GenDraw.DrawInteractionCells(this.def, this.Position, this.rotationInt);
		}

		// Token: 0x06003991 RID: 14737 RVA: 0x00132ABF File Offset: 0x00130CBF
		public virtual string GetInspectString()
		{
			StringBuilder stringBuilder = new StringBuilder();
			QuestUtility.AppendInspectStringsFromQuestParts(stringBuilder, this);
			return stringBuilder.ToString();
		}

		// Token: 0x06003992 RID: 14738 RVA: 0x00132AD4 File Offset: 0x00130CD4
		public virtual string GetInspectStringLowPriority()
		{
			string text = null;
			Thing.tmpDeteriorationReasons.Clear();
			float num = SteadyEnvironmentEffects.FinalDeteriorationRate(this, Thing.tmpDeteriorationReasons);
			if (Thing.tmpDeteriorationReasons.Count != 0)
			{
				text = string.Format("{0}: {1} ({2})", "DeterioratingBecauseOf".Translate(), Thing.tmpDeteriorationReasons.ToCommaList(false, false).CapitalizeFirst(), "PerDay".Translate(num.ToStringByStyle(ToStringStyle.FloatMaxTwo, ToStringNumberSense.Absolute)));
			}
			return text;
		}

		// Token: 0x06003993 RID: 14739 RVA: 0x00132B4D File Offset: 0x00130D4D
		public virtual IEnumerable<Gizmo> GetGizmos()
		{
			Gizmo gizmo = ContainingSelectionUtility.SelectContainingThingGizmo(this);
			if (gizmo != null)
			{
				yield return gizmo;
			}
			Thing.showingGizmosForRitualsTmp.Clear();
			foreach (Ideo ideo in Faction.OfPlayer.ideos.AllIdeos)
			{
				int i = 0;
				while (i < ideo.PreceptsListForReading.Count)
				{
					Precept precept = ideo.PreceptsListForReading[i];
					Precept_Ritual ritual = precept as Precept_Ritual;
					if (ritual == null)
					{
						goto IL_019D;
					}
					if (!precept.def.mergeRitualGizmosFromAllIdeos || !Thing.showingGizmosForRitualsTmp.Contains(ritual.sourcePattern))
					{
						if (ritual.ShouldShowGizmo(this))
						{
							foreach (Gizmo gizmo2 in ritual.GetGizmoFor(this))
							{
								yield return gizmo2;
								Thing.showingGizmosForRitualsTmp.Add(ritual.sourcePattern);
							}
							IEnumerator<Gizmo> enumerator2 = null;
							goto IL_019D;
						}
						goto IL_019D;
					}
					IL_01A4:
					int num = i;
					i = num + 1;
					continue;
					IL_019D:
					ritual = null;
					goto IL_01A4;
				}
				ideo = null;
			}
			IEnumerator<Ideo> enumerator = null;
			List<LordJob_Ritual> activeRituals = Find.IdeoManager.GetActiveRituals(this.MapHeld);
			foreach (LordJob_Ritual lordJob_Ritual in activeRituals)
			{
				if (lordJob_Ritual.selectedTarget == this)
				{
					yield return lordJob_Ritual.GetCancelGizmo();
				}
			}
			List<LordJob_Ritual>.Enumerator enumerator3 = default(List<LordJob_Ritual>.Enumerator);
			if (ModsConfig.AnomalyActive)
			{
				Gizmo gizmo3 = AnomalyUtility.OpenCodexGizmo(this);
				if (gizmo3 != null)
				{
					yield return gizmo3;
				}
			}
			if (DebugSettings.ShowDevGizmos && this.HasAttachment(ThingDefOf.Fire))
			{
				yield return new Command_Action
				{
					defaultLabel = "DEV: Extinguish",
					action = delegate
					{
						Thing attachment = this.GetAttachment(ThingDefOf.Fire);
						if (attachment == null)
						{
							return;
						}
						attachment.Destroy(DestroyMode.Vanish);
					}
				};
			}
			yield break;
			yield break;
		}

		// Token: 0x06003994 RID: 14740 RVA: 0x000FBCE0 File Offset: 0x000F9EE0
		public virtual IEnumerable<FloatMenuOption> GetFloatMenuOptions(Pawn selPawn)
		{
			return Enumerable.Empty<FloatMenuOption>();
		}

		// Token: 0x06003995 RID: 14741 RVA: 0x000FBCE0 File Offset: 0x000F9EE0
		public virtual IEnumerable<FloatMenuOption> GetMultiSelectFloatMenuOptions(IEnumerable<Pawn> selPawns)
		{
			return Enumerable.Empty<FloatMenuOption>();
		}

		// Token: 0x06003996 RID: 14742 RVA: 0x00132B5D File Offset: 0x00130D5D
		public virtual IEnumerable<InspectTabBase> GetInspectTabs()
		{
			return this.def.inspectorTabsResolved;
		}

		// Token: 0x06003997 RID: 14743 RVA: 0x00132B6A File Offset: 0x00130D6A
		public virtual string GetCustomLabelNoCount(bool includeHp = true)
		{
			return GenLabel.ThingLabel(this, 1, includeHp, true);
		}

		// Token: 0x06003998 RID: 14744 RVA: 0x00132B78 File Offset: 0x00130D78
		public DamageWorker.DamageResult TakeDamage(DamageInfo dinfo)
		{
			if (this.Destroyed)
			{
				return new DamageWorker.DamageResult();
			}
			if (dinfo.Amount == 0f)
			{
				return new DamageWorker.DamageResult();
			}
			if (this.def.damageMultipliers != null)
			{
				for (int i = 0; i < this.def.damageMultipliers.Count; i++)
				{
					if (this.def.damageMultipliers[i].damageDef == dinfo.Def)
					{
						int num = Mathf.RoundToInt(dinfo.Amount * this.def.damageMultipliers[i].multiplier);
						dinfo.SetAmount((float)num);
					}
				}
			}
			bool flag;
			this.PreApplyDamage(ref dinfo, out flag);
			if (flag)
			{
				return new DamageWorker.DamageResult();
			}
			bool spawnedOrAnyParentSpawned = this.SpawnedOrAnyParentSpawned;
			Map mapHeld = this.MapHeld;
			DamageWorker.DamageResult damageResult = dinfo.Def.Worker.Apply(dinfo, this);
			if (dinfo.Def.harmsHealth && spawnedOrAnyParentSpawned)
			{
				mapHeld.damageWatcher.Notify_DamageTaken(this, damageResult.totalDamageDealt);
			}
			Pawn pawn = dinfo.Instigator as Pawn;
			if (pawn != null)
			{
				foreach (Hediff hediff in pawn.health.hediffSet.hediffs)
				{
					hediff.Notify_PawnDamagedThing(this, dinfo, damageResult);
				}
			}
			if (dinfo.Def.ExternalViolenceFor(this))
			{
				if (dinfo.SpawnFilth)
				{
					GenLeaving.DropFilthDueToDamage(this, damageResult.totalDamageDealt);
				}
				if (dinfo.Instigator != null)
				{
					Pawn pawn2 = dinfo.Instigator as Pawn;
					if (pawn2 != null)
					{
						pawn2.records.AddTo(RecordDefOf.DamageDealt, damageResult.totalDamageDealt);
					}
					if (dinfo.Instigator.Faction == Faction.OfPlayer)
					{
						QuestUtility.SendQuestTargetSignals(this.questTags, "TookDamageFromPlayer", this.Named("SUBJECT"), dinfo.Instigator.Named("INSTIGATOR"));
					}
				}
				QuestUtility.SendQuestTargetSignals(this.questTags, "TookDamage", this.Named("SUBJECT"), dinfo.Instigator.Named("INSTIGATOR"), mapHeld.Named("MAP"));
			}
			if (!this.Destroyed && this.FlammableNow && dinfo.Def.igniteChanceByTargetFlammability != null && Rand.Chance(dinfo.Def.igniteChanceByTargetFlammability.Evaluate(this.GetStatValue(StatDefOf.Flammability, true, -1))))
			{
				this.TryAttachFire(Rand.Range(0.55f, 0.85f), dinfo.Instigator);
			}
			this.PostApplyDamage(dinfo, damageResult.totalDamageDealt);
			return damageResult;
		}

		// Token: 0x06003999 RID: 14745 RVA: 0x00132E1C File Offset: 0x0013101C
		public virtual void PreApplyDamage(ref DamageInfo dinfo, out bool absorbed)
		{
			absorbed = false;
		}

		// Token: 0x0600399A RID: 14746 RVA: 0x000026EA File Offset: 0x000008EA
		public virtual void PostApplyDamage(DamageInfo dinfo, float totalDamageDealt)
		{
		}

		// Token: 0x0600399B RID: 14747 RVA: 0x00132E24 File Offset: 0x00131024
		public virtual bool CanStackWith(Thing other)
		{
			return !this.Destroyed && !other.Destroyed && this.def.category == ThingCategory.Item && !this.IsRelic() && !other.IsRelic() && this.def == other.def && this.Stuff == other.Stuff;
		}

		// Token: 0x0600399C RID: 14748 RVA: 0x00132E84 File Offset: 0x00131084
		public virtual bool TryAbsorbStack(Thing other, bool respectStackLimit)
		{
			if (!this.CanStackWith(other))
			{
				return false;
			}
			int num = ThingUtility.TryAbsorbStackNumToTake(this, other, respectStackLimit);
			if (this.def.useHitPoints)
			{
				this.HitPoints = Mathf.CeilToInt((float)(this.HitPoints * this.stackCount + other.HitPoints * num) / (float)(this.stackCount + num));
			}
			this.stackCount += num;
			other.stackCount -= num;
			if (this.Map != null)
			{
				this.DirtyMapMesh(this.Map);
			}
			StealAIDebugDrawer.Notify_ThingChanged(this);
			if (this.Spawned)
			{
				this.Map.listerMergeables.Notify_ThingStackChanged(this);
			}
			if (other.stackCount <= 0)
			{
				other.Destroy(DestroyMode.Vanish);
				return true;
			}
			return false;
		}

		// Token: 0x0600399D RID: 14749 RVA: 0x00132F40 File Offset: 0x00131140
		public virtual Thing SplitOff(int count)
		{
			if (count <= 0)
			{
				throw new ArgumentException("SplitOff with count <= 0", "count");
			}
			if (count >= this.stackCount)
			{
				if (count > this.stackCount)
				{
					Log.Error(string.Concat(new string[]
					{
						"Tried to split off ",
						count.ToString(),
						" of ",
						(this != null) ? this.ToString() : null,
						" but there are only ",
						this.stackCount.ToString()
					}));
				}
				this.DeSpawnOrDeselect(DestroyMode.Vanish);
				ThingOwner thingOwner = this.holdingOwner;
				if (thingOwner != null)
				{
					thingOwner.Remove(this);
				}
				return this;
			}
			Thing thing = ThingMaker.MakeThing(this.def, this.Stuff);
			thing.stackCount = count;
			this.stackCount -= count;
			if (this.Map != null)
			{
				this.DirtyMapMesh(this.Map);
			}
			if (this.Spawned)
			{
				this.Map.listerMergeables.Notify_ThingStackChanged(this);
			}
			if (this.def.useHitPoints)
			{
				thing.HitPoints = this.HitPoints;
			}
			return thing;
		}

		// Token: 0x0600399E RID: 14750 RVA: 0x0013304E File Offset: 0x0013124E
		public virtual IEnumerable<StatDrawEntry> SpecialDisplayStats()
		{
			if (this.Stuff != null)
			{
				yield return new StatDrawEntry(StatCategoryDefOf.BasicsImportant, "Stat_Stuff_Name".Translate(), this.Stuff.LabelCap, "Stat_Stuff_Desc".Translate(), 1100, null, new Dialog_InfoCard.Hyperlink[]
				{
					new Dialog_InfoCard.Hyperlink(this.Stuff, -1)
				}, false, false);
			}
			if (ModsConfig.IdeologyActive && !Find.IdeoManager.classicMode)
			{
				Thing.tmpIdeoNames.Clear();
				ThingStyleDef styleDef = this.StyleDef;
				StyleCategoryDef styleCategoryDef = ((styleDef != null) ? styleDef.Category : null) ?? this.def.dominantStyleCategory;
				if (styleCategoryDef != null)
				{
					foreach (Ideo ideo in Find.IdeoManager.IdeosListForReading)
					{
						if (IdeoUtility.ThingSatisfiesIdeo(this, ideo))
						{
							Thing.tmpIdeoNames.Add(ideo.name.Colorize(ideo.Color));
						}
					}
					yield return new StatDrawEntry(StatCategoryDefOf.BasicsNonPawn, "Stat_Thing_StyleDominanceCategory".Translate(), styleCategoryDef.LabelCap, "Stat_Thing_StyleDominanceCategoryDesc".Translate() + "\n\n" + "Stat_Thing_IdeosSatisfied".Translate() + ":" + "\n" + Thing.tmpIdeoNames.ToLineList("  - "), 6005, null, null, false, false);
				}
			}
			yield break;
		}

		// Token: 0x17000AD1 RID: 2769
		// (get) Token: 0x0600399F RID: 14751 RVA: 0x0013305E File Offset: 0x0013125E
		public virtual IEnumerable<DefHyperlink> DescriptionHyperlinks
		{
			get
			{
				if (this.def.descriptionHyperlinks != null)
				{
					int num;
					for (int i = 0; i < this.def.descriptionHyperlinks.Count; i = num + 1)
					{
						yield return this.def.descriptionHyperlinks[i];
						num = i;
					}
				}
				yield break;
			}
		}

		// Token: 0x060039A0 RID: 14752 RVA: 0x00133070 File Offset: 0x00131270
		public virtual void Notify_ColorChanged()
		{
			this.graphicInt = null;
			this.styleGraphicInt = null;
			if (this.Spawned && (this.def.drawerType == DrawerType.MapMeshOnly || this.def.drawerType == DrawerType.MapMeshAndRealTime))
			{
				this.Map.mapDrawer.MapMeshDirty(this.Position, MapMeshFlagDefOf.Things);
			}
		}

		// Token: 0x060039A1 RID: 14753 RVA: 0x000026EA File Offset: 0x000008EA
		public virtual void Notify_Equipped(Pawn pawn)
		{
		}

		// Token: 0x060039A2 RID: 14754 RVA: 0x000026EA File Offset: 0x000008EA
		public virtual void Notify_Unequipped(Pawn pawn)
		{
		}

		// Token: 0x060039A3 RID: 14755 RVA: 0x000026EA File Offset: 0x000008EA
		public virtual void Notify_UsedVerb(Pawn pawn, Verb verb)
		{
		}

		// Token: 0x060039A4 RID: 14756 RVA: 0x000026EA File Offset: 0x000008EA
		public virtual void Notify_UsedWeapon(Pawn pawn)
		{
		}

		// Token: 0x060039A5 RID: 14757 RVA: 0x000026EA File Offset: 0x000008EA
		public virtual void Notify_DebugSpawned()
		{
		}

		// Token: 0x060039A6 RID: 14758 RVA: 0x000026EA File Offset: 0x000008EA
		public virtual void Notify_MinifiedThingAboutToBeDestroyed(DestroyMode mode)
		{
		}

		// Token: 0x060039A7 RID: 14759 RVA: 0x000026EA File Offset: 0x000008EA
		public virtual void Notify_RecipeProduced(Pawn pawn)
		{
		}

		// Token: 0x060039A8 RID: 14760 RVA: 0x000026EA File Offset: 0x000008EA
		public virtual void Notify_SignalReceived(Signal signal)
		{
		}

		// Token: 0x060039A9 RID: 14761 RVA: 0x000026EA File Offset: 0x000008EA
		public virtual void Notify_Explosion(Explosion explosion)
		{
		}

		// Token: 0x060039AA RID: 14762 RVA: 0x000026EA File Offset: 0x000008EA
		public virtual void Notify_BulletImpactNearby(BulletImpactData impactData)
		{
		}

		// Token: 0x060039AB RID: 14763 RVA: 0x000026EA File Offset: 0x000008EA
		public virtual void Notify_ThingSelected()
		{
		}

		// Token: 0x060039AC RID: 14764 RVA: 0x001330D0 File Offset: 0x001312D0
		public virtual TipSignal GetTooltip()
		{
			string text = this.LabelCap;
			if (this.def.useHitPoints)
			{
				text = string.Concat(new string[]
				{
					text,
					"\n",
					this.HitPoints.ToString(),
					" / ",
					this.MaxHitPoints.ToString()
				});
			}
			return new TipSignal(text, this.thingIDNumber * 251235);
		}

		// Token: 0x060039AD RID: 14765 RVA: 0x00133145 File Offset: 0x00131345
		public virtual bool BlocksPawn(Pawn p)
		{
			return this.def.passability == Traversability.Impassable || (this.def.IsFence && p.FenceBlocked);
		}

		// Token: 0x060039AE RID: 14766 RVA: 0x0013316F File Offset: 0x0013136F
		public void SetFactionDirect(Faction newFaction)
		{
			if (!this.def.CanHaveFaction)
			{
				Log.Error("Tried to SetFactionDirect on " + ((this != null) ? this.ToString() : null) + " which cannot have a faction.");
				return;
			}
			this.factionInt = newFaction;
		}

		// Token: 0x060039AF RID: 14767 RVA: 0x001331A8 File Offset: 0x001313A8
		public virtual void SetFaction(Faction newFaction, Pawn recruiter = null)
		{
			if (!this.def.CanHaveFaction)
			{
				Log.Error("Tried to SetFaction on " + ((this != null) ? this.ToString() : null) + " which cannot have a faction.");
				return;
			}
			Faction faction = this.factionInt;
			this.factionInt = newFaction;
			if (this.Spawned)
			{
				IAttackTarget attackTarget = this as IAttackTarget;
				if (attackTarget != null)
				{
					this.Map.attackTargetsCache.UpdateTarget(attackTarget);
				}
			}
			QuestUtility.SendQuestTargetSignals(this.questTags, "ChangedFaction", this.Named("SUBJECT"), newFaction.Named("FACTION"));
			if (newFaction != Faction.OfPlayer)
			{
				QuestUtility.SendQuestTargetSignals(this.questTags, "ChangedFactionToNonPlayer", this.Named("SUBJECT"), newFaction.Named("FACTION"));
			}
			else
			{
				QuestUtility.SendQuestTargetSignals(this.questTags, "ChangedFactionToPlayer", this.Named("SUBJECT"), newFaction.Named("FACTION"));
			}
			if (this.Spawned)
			{
				this.Map.events.Notify_ThingFactionChanged(faction, this.factionInt);
			}
		}

		// Token: 0x060039B0 RID: 14768 RVA: 0x000FA51B File Offset: 0x000F871B
		public virtual AcceptanceReport ClaimableBy(Faction by)
		{
			return false;
		}

		// Token: 0x060039B1 RID: 14769 RVA: 0x00002501 File Offset: 0x00000701
		public virtual bool AdoptableBy(Faction by, StringBuilder reason = null)
		{
			return false;
		}

		// Token: 0x060039B2 RID: 14770 RVA: 0x001332B0 File Offset: 0x001314B0
		public bool FactionPreventsClaimingOrAdopting(Faction faction, bool forClaim, out string reason)
		{
			reason = null;
			if (faction == null)
			{
				return false;
			}
			if (faction == Faction.OfInsects)
			{
				if (HiveUtility.AnyHivePreventsClaiming(this))
				{
					return true;
				}
			}
			else
			{
				if (faction == Faction.OfMechanoids)
				{
					if (MechClusterUtility.PartOfActiveMechCluster(this))
					{
						return true;
					}
					using (HashSet<IAttackTarget>.Enumerator enumerator = this.MapHeld.attackTargetsCache.TargetsHostileToFaction(Faction.OfPlayer).GetEnumerator())
					{
						while (enumerator.MoveNext())
						{
							IAttackTarget attackTarget = enumerator.Current;
							if (attackTarget.Thing != null && attackTarget.Thing.Faction == faction)
							{
								Pawn pawn = attackTarget.Thing as Pawn;
								if (pawn == null)
								{
									if (forClaim)
									{
										reason = "MessageCannotClaimWhenThreatsAreNear".Translate(this.Named("CLAIMABLE"), attackTarget.Named("THREAT"));
									}
									else
									{
										reason = "MessageCannotAdoptWhileThreatsAreNear".Translate(this.Named("CLAIMABLE"), attackTarget.Named("THREAT"));
									}
									return true;
								}
								if (GenHostility.IsActiveThreatToPlayer(pawn, false))
								{
									if (forClaim)
									{
										reason = "MessageCannotClaimWhenPawnThreatsAreNear".Translate(this.Named("CLAIMABLE"), pawn.Named("THREAT"));
									}
									else
									{
										reason = "MessageCannotAdoptWhilePawnThreatsAreNear".Translate(this.Named("CLAIMABLE"), pawn.Named("THREAT"));
									}
									return true;
								}
							}
						}
						return false;
					}
				}
				if (faction == Faction.OfAncients && this.Spawned && !this.Map.IsPlayerHome && GenHostility.AnyHostileActiveThreatToPlayer(this.Map, true, true))
				{
					return true;
				}
				if (this.Spawned && faction != Faction.OfPlayer)
				{
					List<Pawn> list = this.Map.mapPawns.SpawnedPawnsInFaction(faction);
					for (int i = 0; i < list.Count; i++)
					{
						if (list[i].RaceProps.ToolUser && GenHostility.IsPotentialThreat(list[i]))
						{
							if (forClaim)
							{
								reason = "MessageCannotClaimWhenThreatsAreNear".Translate(this.Named("CLAIMABLE"), list[i].Named("THREAT"));
							}
							else
							{
								reason = "MessageCannotAdoptWhileThreatsAreNear".Translate(this.Named("CLAIMABLE"), list[i].Named("THREAT"));
							}
							return true;
						}
					}
				}
			}
			return false;
		}

		// Token: 0x060039B3 RID: 14771 RVA: 0x00133540 File Offset: 0x00131740
		public void SetPositionDirect(IntVec3 newPos)
		{
			this.positionInt = newPos;
		}

		// Token: 0x060039B4 RID: 14772 RVA: 0x00133549 File Offset: 0x00131749
		public void SetStuffDirect(ThingDef newStuff)
		{
			this.stuffInt = newStuff;
		}

		// Token: 0x060039B5 RID: 14773 RVA: 0x00133552 File Offset: 0x00131752
		public override string ToString()
		{
			if (this.def != null)
			{
				return this.ThingID;
			}
			return base.GetType().ToString();
		}

		// Token: 0x060039B6 RID: 14774 RVA: 0x0013356E File Offset: 0x0013176E
		public bool Equals(Thing other)
		{
			if (other == null)
			{
				return false;
			}
			if (this.def.category == ThingCategory.Mote)
			{
				return this == other;
			}
			return this.thingIDNumber == other.thingIDNumber && this.def.Equals(other.def);
		}

		// Token: 0x060039B7 RID: 14775 RVA: 0x001335AA File Offset: 0x001317AA
		public override int GetHashCode()
		{
			if (this.thingIDNumber == -1)
			{
				return base.GetHashCode();
			}
			return this.thingIDNumber;
		}

		// Token: 0x060039B8 RID: 14776 RVA: 0x001335C4 File Offset: 0x001317C4
		public virtual void Discard(bool silentlyRemoveReferences = false)
		{
			if (this.mapIndexOrState != -2)
			{
				Log.Warning(string.Concat(new string[]
				{
					"Tried to discard ",
					(this != null) ? this.ToString() : null,
					" whose state is ",
					this.mapIndexOrState.ToString(),
					"."
				}));
				return;
			}
			this.mapIndexOrState = -3;
		}

		// Token: 0x060039B9 RID: 14777 RVA: 0x0013362A File Offset: 0x0013182A
		public virtual void Notify_DefsHotReloaded()
		{
			this.graphicInt = null;
		}

		// Token: 0x060039BA RID: 14778 RVA: 0x00133633 File Offset: 0x00131833
		public virtual IEnumerable<Thing> ButcherProducts(Pawn butcher, float efficiency)
		{
			if (this.def.butcherProducts != null)
			{
				int num2;
				for (int i = 0; i < this.def.butcherProducts.Count; i = num2 + 1)
				{
					ThingDefCountClass thingDefCountClass = this.def.butcherProducts[i];
					int num = GenMath.RoundRandom((float)thingDefCountClass.count * efficiency);
					num = GenMath.RoundRandom((float)num * Find.Storyteller.difficulty.butcherYieldFactor);
					if (num > 0)
					{
						Thing thing = ThingMaker.MakeThing(thingDefCountClass.thingDef, null);
						thing.stackCount = num;
						yield return thing;
					}
					num2 = i;
				}
			}
			yield break;
		}

		// Token: 0x060039BB RID: 14779 RVA: 0x0013364A File Offset: 0x0013184A
		public virtual IEnumerable<Thing> SmeltProducts(float efficiency)
		{
			List<ThingDefCountClass> costListAdj = this.def.CostListAdjusted(this.Stuff, true);
			int num2;
			for (int i = 0; i < costListAdj.Count; i = num2 + 1)
			{
				if (!costListAdj[i].thingDef.intricate && costListAdj[i].thingDef.smeltable)
				{
					int num = GenMath.RoundRandom((float)costListAdj[i].count * 0.25f);
					if (num > 0)
					{
						Thing thing = ThingMaker.MakeThing(costListAdj[i].thingDef, null);
						thing.stackCount = num;
						yield return thing;
					}
				}
				num2 = i;
			}
			if (this.def.smeltProducts != null)
			{
				for (int i = 0; i < this.def.smeltProducts.Count; i = num2 + 1)
				{
					ThingDefCountClass thingDefCountClass = this.def.smeltProducts[i];
					Thing thing2 = ThingMaker.MakeThing(thingDefCountClass.thingDef, null);
					thing2.stackCount = thingDefCountClass.count;
					yield return thing2;
					num2 = i;
				}
			}
			yield break;
		}

		// Token: 0x060039BC RID: 14780 RVA: 0x0013365C File Offset: 0x0013185C
		public float Ingested(Pawn ingester, float nutritionWanted)
		{
			if (this.Destroyed)
			{
				Log.Error(((ingester != null) ? ingester.ToString() : null) + " ingested destroyed thing " + ((this != null) ? this.ToString() : null));
				return 0f;
			}
			if (!this.IngestibleNow)
			{
				Log.Error(((ingester != null) ? ingester.ToString() : null) + " ingested IngestibleNow=false thing " + ((this != null) ? this.ToString() : null));
				return 0f;
			}
			ingester.mindState.lastIngestTick = Find.TickManager.TicksGame;
			if (ingester.needs.mood != null)
			{
				List<FoodUtility.ThoughtFromIngesting> list = FoodUtility.ThoughtsFromIngesting(ingester, this, this.def);
				for (int i = 0; i < list.Count; i++)
				{
					Thought_Memory thought_Memory = ThoughtMaker.MakeThought(list[i].thought, list[i].fromPrecept);
					Thought_FoodEaten thought_FoodEaten = thought_Memory as Thought_FoodEaten;
					if (thought_FoodEaten != null)
					{
						thought_FoodEaten.SetFood(this);
					}
					ingester.needs.mood.thoughts.memories.TryGainMemory(thought_Memory, null);
				}
			}
			Need_Chemical_Any drugsDesire = ingester.needs.drugsDesire;
			if (drugsDesire != null)
			{
				drugsDesire.Notify_IngestedDrug(this);
			}
			bool flag = FoodUtility.IsHumanlikeCorpseOrHumanlikeMeat(this, this.def);
			bool flag2 = FoodUtility.IsHumanlikeCorpseOrHumanlikeMeatOrIngredient(this);
			if (flag && ingester.IsColonist)
			{
				TaleRecorder.RecordTale(TaleDefOf.AteRawHumanlikeMeat, new object[] { ingester });
			}
			if (flag2)
			{
				ingester.mindState.lastHumanMeatIngestedTick = Find.TickManager.TicksGame;
				Find.HistoryEventsManager.RecordEvent(new HistoryEvent(HistoryEventDefOf.AteHumanMeat, ingester.Named(HistoryEventArgsNames.Doer)), false);
				if (flag)
				{
					Find.HistoryEventsManager.RecordEvent(new HistoryEvent(HistoryEventDefOf.AteHumanMeatDirect, ingester.Named(HistoryEventArgsNames.Doer)), false);
				}
			}
			else if (ModsConfig.IdeologyActive && !FoodUtility.AcceptableCannibalNonHumanlikeMeatFood(this.def))
			{
				Find.HistoryEventsManager.RecordEvent(new HistoryEvent(HistoryEventDefOf.AteNonCannibalFood, ingester.Named(HistoryEventArgsNames.Doer)), false);
			}
			if (this.def.ingestible.ateEvent != null)
			{
				Find.HistoryEventsManager.RecordEvent(new HistoryEvent(this.def.ingestible.ateEvent, ingester.Named(HistoryEventArgsNames.Doer)), false);
			}
			if (ModsConfig.IdeologyActive)
			{
				FoodKind foodKind = FoodUtility.GetFoodKind(this);
				if (foodKind != FoodKind.Any && !this.def.IsProcessedFood)
				{
					if (foodKind == FoodKind.Meat)
					{
						if (!flag2)
						{
							Find.HistoryEventsManager.RecordEvent(new HistoryEvent(HistoryEventDefOf.AteMeat, ingester.Named(HistoryEventArgsNames.Doer)), false);
						}
					}
					else if (!this.def.IsDrug && this.def.ingestible.CachedNutrition > 0f)
					{
						Find.HistoryEventsManager.RecordEvent(new HistoryEvent(HistoryEventDefOf.AteNonMeat, ingester.Named(HistoryEventArgsNames.Doer)), false);
					}
				}
				if (FoodUtility.IsVeneratedAnimalMeatOrCorpseOrHasIngredients(this, ingester))
				{
					Find.HistoryEventsManager.RecordEvent(new HistoryEvent(HistoryEventDefOf.AteVeneratedAnimalMeat, ingester.Named(HistoryEventArgsNames.Doer)), false);
				}
				if (this.def.thingCategories != null && this.def.thingCategories.Contains(ThingCategoryDefOf.PlantFoodRaw))
				{
					if (this.def.IsFungus)
					{
						Find.HistoryEventsManager.RecordEvent(new HistoryEvent(HistoryEventDefOf.AteFungus, ingester.Named(HistoryEventArgsNames.Doer)), false);
					}
					else
					{
						Find.HistoryEventsManager.RecordEvent(new HistoryEvent(HistoryEventDefOf.AteNonFungusPlant, ingester.Named(HistoryEventArgsNames.Doer)), false);
					}
				}
			}
			CompIngredients compIngredients = this.TryGetComp<CompIngredients>();
			if (compIngredients != null)
			{
				bool flag3 = false;
				bool flag4 = false;
				bool flag5 = false;
				bool flag6 = false;
				bool flag7 = false;
				for (int j = 0; j < compIngredients.ingredients.Count; j++)
				{
					if (!flag3 && FoodUtility.GetMeatSourceCategory(compIngredients.ingredients[j]) == MeatSourceCategory.Humanlike)
					{
						ingester.mindState.lastHumanMeatIngestedTick = Find.TickManager.TicksGame;
						Find.HistoryEventsManager.RecordEvent(new HistoryEvent(HistoryEventDefOf.AteHumanMeatAsIngredient, ingester.Named(HistoryEventArgsNames.Doer)), false);
						flag3 = true;
					}
					else if (!flag4 && ingester.Ideo != null && compIngredients.ingredients[j].IsMeat && ingester.Ideo.IsVeneratedAnimal(compIngredients.ingredients[j].ingestible.sourceDef))
					{
						Find.HistoryEventsManager.RecordEvent(new HistoryEvent(HistoryEventDefOf.AteVeneratedAnimalMeat, ingester.Named(HistoryEventArgsNames.Doer)), false);
						flag4 = true;
					}
					if (!flag5 && FoodUtility.GetMeatSourceCategory(compIngredients.ingredients[j]) == MeatSourceCategory.Insect)
					{
						Find.HistoryEventsManager.RecordEvent(new HistoryEvent(HistoryEventDefOf.AteInsectMeatAsIngredient, ingester.Named(HistoryEventArgsNames.Doer)), false);
						flag5 = true;
					}
					if (ModsConfig.IdeologyActive && !flag6 && compIngredients.ingredients[j].thingCategories.Contains(ThingCategoryDefOf.PlantFoodRaw))
					{
						if (compIngredients.ingredients[j].IsFungus)
						{
							Find.HistoryEventsManager.RecordEvent(new HistoryEvent(HistoryEventDefOf.AteFungusAsIngredient, ingester.Named(HistoryEventArgsNames.Doer)), false);
							flag6 = true;
						}
						else
						{
							flag7 = true;
						}
					}
				}
				if (ModsConfig.IdeologyActive && !flag6 && flag7)
				{
					Find.HistoryEventsManager.RecordEvent(new HistoryEvent(HistoryEventDefOf.AteNonFungusMealWithPlants, ingester.Named(HistoryEventArgsNames.Doer)), false);
				}
			}
			int num;
			float num2;
			this.IngestedCalculateAmounts(ingester, nutritionWanted, out num, out num2);
			if (!ingester.Dead && ingester.needs.joy != null && Mathf.Abs(this.def.ingestible.joy) > 0.0001f && num > 0)
			{
				ingester.needs.joy.GainJoy((float)num * this.def.ingestible.joy, this.def.ingestible.joyKind ?? JoyKindDefOf.Gluttonous);
			}
			float num4;
			float num3 = (FoodUtility.TryGetFoodPoisoningChanceOverrideFromTraits(ingester, this, out num4) ? num4 : (this.GetStatValue(StatDefOf.FoodPoisonChanceFixedHuman, true, -1) * FoodUtility.GetFoodPoisonChanceFactor(ingester)));
			if (ingester.RaceProps.Humanlike && Rand.Chance(num3))
			{
				FoodUtility.AddFoodPoisoningHediff(ingester, this, FoodPoisonCause.DangerousFoodType);
			}
			List<Hediff> hediffs = ingester.health.hediffSet.hediffs;
			for (int k = 0; k < hediffs.Count; k++)
			{
				hediffs[k].Notify_IngestedThing(this, num);
			}
			Pawn_GeneTracker genes = ingester.genes;
			if (genes != null)
			{
				genes.Notify_IngestedThing(this, num);
			}
			bool flag8 = false;
			if (num > 0)
			{
				if (this.stackCount == 0)
				{
					Log.Error(((this != null) ? this.ToString() : null) + " stack count is 0.");
				}
				if (num == this.stackCount)
				{
					flag8 = true;
				}
				else
				{
					this.SplitOff(num);
				}
			}
			this.PrePostIngested(ingester);
			if (flag8)
			{
				ingester.carryTracker.innerContainer.Remove(this);
			}
			if (this.def.ingestible.outcomeDoers != null)
			{
				for (int l = 0; l < this.def.ingestible.outcomeDoers.Count; l++)
				{
					this.def.ingestible.outcomeDoers[l].DoIngestionOutcome(ingester, this, num);
				}
			}
			if (flag8 && !this.Destroyed)
			{
				this.Destroy(DestroyMode.Vanish);
			}
			this.PostIngested(ingester);
			return num2;
		}

		// Token: 0x060039BD RID: 14781 RVA: 0x000026EA File Offset: 0x000008EA
		protected virtual void PrePostIngested(Pawn ingester)
		{
		}

		// Token: 0x060039BE RID: 14782 RVA: 0x000026EA File Offset: 0x000008EA
		protected virtual void PostIngested(Pawn ingester)
		{
		}

		// Token: 0x060039BF RID: 14783 RVA: 0x00133D60 File Offset: 0x00131F60
		protected virtual void IngestedCalculateAmounts(Pawn ingester, float nutritionWanted, out int numTaken, out float nutritionIngested)
		{
			float num = FoodUtility.NutritionForEater(ingester, this);
			numTaken = Mathf.CeilToInt(nutritionWanted / num);
			numTaken = Mathf.Min(numTaken, this.stackCount);
			if (this.def.ingestible.maxNumToIngestAtOnce > 0)
			{
				numTaken = Mathf.Min(numTaken, this.def.ingestible.maxNumToIngestAtOnce);
			}
			numTaken = Mathf.Max(numTaken, 1);
			nutritionIngested = (float)numTaken * num;
		}

		// Token: 0x060039C0 RID: 14784 RVA: 0x00133DCC File Offset: 0x00131FCC
		public virtual bool PreventPlayerSellingThingsNearby(out string reason)
		{
			reason = null;
			return false;
		}

		// Token: 0x060039C1 RID: 14785 RVA: 0x00133DD2 File Offset: 0x00131FD2
		public virtual void PreSwapMap()
		{
			this.beingTransportedOnGravship = true;
		}

		// Token: 0x060039C2 RID: 14786 RVA: 0x00133DDB File Offset: 0x00131FDB
		public virtual void PostSwapMap()
		{
			this.beingTransportedOnGravship = false;
			QuestUtility.SendQuestTargetSignals(this.questTags, "SwappedMap", this.Named("SUBJECT"));
		}

		// Token: 0x060039C3 RID: 14787 RVA: 0x00133E00 File Offset: 0x00132000
		public virtual void Notify_LeftBehind()
		{
			QuestUtility.SendQuestTargetSignals(this.questTags, "LeftBehind", this.Named("SUBJECT"));
			IThingHolder thingHolder = this as IThingHolder;
			if (thingHolder != null && thingHolder.GetDirectlyHeldThings() != null)
			{
				foreach (Thing thing in thingHolder.GetDirectlyHeldThings().ToList<Thing>())
				{
					thing.Notify_LeftBehind();
				}
			}
		}

		// Token: 0x04002A69 RID: 10857
		public ThingDef def;

		// Token: 0x04002A6A RID: 10858
		public int thingIDNumber = -1;

		// Token: 0x04002A6B RID: 10859
		private sbyte mapIndexOrState = -1;

		// Token: 0x04002A6C RID: 10860
		private IntVec3 positionInt = IntVec3.Invalid;

		// Token: 0x04002A6D RID: 10861
		private Rot4 rotationInt = Rot4.North;

		// Token: 0x04002A6E RID: 10862
		public int stackCount = 1;

		// Token: 0x04002A6F RID: 10863
		protected Faction factionInt;

		// Token: 0x04002A70 RID: 10864
		private ThingDef stuffInt;

		// Token: 0x04002A71 RID: 10865
		private Graphic graphicInt;

		// Token: 0x04002A72 RID: 10866
		protected Graphic styleGraphicInt;

		// Token: 0x04002A73 RID: 10867
		private int hitPointsInt = -1;

		// Token: 0x04002A74 RID: 10868
		public ThingOwner holdingOwner;

		// Token: 0x04002A75 RID: 10869
		public List<string> questTags;

		// Token: 0x04002A76 RID: 10870
		public int spawnedTick = -1;

		// Token: 0x04002A77 RID: 10871
		public int despawnedTick = -1;

		// Token: 0x04002A78 RID: 10872
		public int? overrideGraphicIndex;

		// Token: 0x04002A79 RID: 10873
		public bool debugRotLocked;

		// Token: 0x04002A7A RID: 10874
		private bool beingTransportedOnGravship;

		// Token: 0x04002A7B RID: 10875
		private int tickDelta;

		// Token: 0x04002A7C RID: 10876
		private bool beenRevealed;

		// Token: 0x04002A7D RID: 10877
		public bool shouldHighlightCached;

		// Token: 0x04002A7E RID: 10878
		public int shouldHighlightCachedTick;

		// Token: 0x04002A7F RID: 10879
		public Color highlightColorCached;

		// Token: 0x04002A80 RID: 10880
		public int highlightColorCachedTick;

		// Token: 0x04002A81 RID: 10881
		protected const sbyte UnspawnedState = -1;

		// Token: 0x04002A82 RID: 10882
		private const sbyte MemoryState = -2;

		// Token: 0x04002A83 RID: 10883
		private const sbyte DiscardedState = -3;

		// Token: 0x04002A84 RID: 10884
		private List<IThingHolder> tmpHolders;

		// Token: 0x04002A85 RID: 10885
		private bool cached;

		// Token: 0x04002A86 RID: 10886
		private bool cachedIsHolder;

		// Token: 0x04002A87 RID: 10887
		private IThingHolder cachedHolder;

		// Token: 0x04002A88 RID: 10888
		private IThingHolderTickable cachedTickable;

		// Token: 0x04002A89 RID: 10889
		public static bool allowDestroyNonDestroyable = false;

		// Token: 0x04002A8A RID: 10890
		private static Dictionary<Thing, string> facIDsCached = new Dictionary<Thing, string>();

		// Token: 0x04002A8B RID: 10891
		private static List<string> tmpDeteriorationReasons = new List<string>();

		// Token: 0x04002A8C RID: 10892
		public static HashSet<RitualPatternDef> showingGizmosForRitualsTmp = new HashSet<RitualPatternDef>();

		// Token: 0x04002A8D RID: 10893
		private static List<string> tmpIdeoNames = new List<string>();

		// Token: 0x04002A8E RID: 10894
		public const float SmeltCostRecoverFraction = 0.25f;
	}
}
