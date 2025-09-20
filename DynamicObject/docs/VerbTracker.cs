using System;
using System.Collections.Generic;
using RimWorld;

namespace Verse
{
	// Token: 0x02000D7E RID: 3454
	public class VerbTracker : IExposable
	{
		// Token: 0x17000E03 RID: 3587
		// (get) Token: 0x06005816 RID: 22550 RVA: 0x001C5DDB File Offset: 0x001C3FDB
		public List<Verb> AllVerbs
		{
			get
			{
				if (this.verbs == null)
				{
					this.InitVerbsFromZero();
				}
				return this.verbs;
			}
		}

		// Token: 0x17000E04 RID: 3588
		// (get) Token: 0x06005817 RID: 22551 RVA: 0x001C5DF4 File Offset: 0x001C3FF4
		public Verb PrimaryVerb
		{
			get
			{
				if (this.verbs == null)
				{
					this.InitVerbsFromZero();
				}
				for (int i = 0; i < this.verbs.Count; i++)
				{
					if (this.verbs[i].verbProps.isPrimary)
					{
						return this.verbs[i];
					}
				}
				return null;
			}
		}

		// Token: 0x17000E05 RID: 3589
		// (get) Token: 0x06005818 RID: 22552 RVA: 0x001C5E4C File Offset: 0x001C404C
		public bool AnyVerbBursting
		{
			get
			{
				for (int i = 0; i < this.verbs.Count; i++)
				{
					if (this.verbs[i].state == VerbState.Bursting)
					{
						return true;
					}
				}
				return false;
			}
		}

		// Token: 0x06005819 RID: 22553 RVA: 0x001C5E86 File Offset: 0x001C4086
		public VerbTracker(IVerbOwner directOwner)
		{
			this.directOwner = directOwner;
		}

		// Token: 0x0600581A RID: 22554 RVA: 0x001C5E98 File Offset: 0x001C4098
		public void VerbsTick()
		{
			if (this.verbs == null)
			{
				return;
			}
			for (int i = 0; i < this.verbs.Count; i++)
			{
				this.verbs[i].VerbTick();
			}
		}

		// Token: 0x0600581B RID: 22555 RVA: 0x001C5ED5 File Offset: 0x001C40D5
		public IEnumerable<Command> GetVerbsCommands()
		{
			IVerbOwner verbOwner = this.directOwner;
			CompEquippable ce = verbOwner as CompEquippable;
			if (ce == null)
			{
				yield break;
			}
			Thing ownerThing = ce.parent;
			List<Verb> verbs = this.AllVerbs;
			int num;
			for (int i = 0; i < verbs.Count; i = num + 1)
			{
				Verb verb = verbs[i];
				if (verb.verbProps.hasStandardCommand)
				{
					yield return this.CreateVerbTargetCommand(ownerThing, verb);
				}
				num = i;
			}
			if (!this.directOwner.Tools.NullOrEmpty<Tool>() && ce != null && ce.parent.def.IsMeleeWeapon)
			{
				yield return this.CreateVerbTargetCommand(ownerThing, verbs.FirstOrDefault((Verb v) => v.verbProps.IsMeleeAttack));
			}
			yield break;
		}

		// Token: 0x0600581C RID: 22556 RVA: 0x001C5EE8 File Offset: 0x001C40E8
		private Command_VerbTarget CreateVerbTargetCommand(Thing ownerThing, Verb verb)
		{
			Command_VerbTarget command_VerbTarget = new Command_VerbTarget();
			command_VerbTarget.defaultDesc = ownerThing.LabelCap + ": " + ownerThing.def.description.CapitalizeFirst();
			command_VerbTarget.ownerThing = ownerThing;
			command_VerbTarget.tutorTag = "VerbTarget";
			command_VerbTarget.verb = verb;
			if (verb.caster.Faction != Faction.OfPlayer && !DebugSettings.ShowDevGizmos)
			{
				command_VerbTarget.Disable("CannotOrderNonControlled".Translate());
			}
			else if (verb.CasterIsPawn)
			{
				string text;
				if (verb.CasterPawn.RaceProps.IsMechanoid && !MechanitorUtility.EverControllable(verb.CasterPawn) && !DebugSettings.ShowDevGizmos)
				{
					command_VerbTarget.Disable("CannotOrderNonControlled".Translate());
				}
				else if (verb.CasterPawn.WorkTagIsDisabled(WorkTags.Violent))
				{
					command_VerbTarget.Disable("IsIncapableOfViolence".Translate(verb.CasterPawn.LabelShort, verb.CasterPawn));
				}
				else if (!verb.CasterPawn.Drafted && !DebugSettings.ShowDevGizmos)
				{
					command_VerbTarget.Disable("IsNotDrafted".Translate(verb.CasterPawn.LabelShort, verb.CasterPawn));
				}
				else if (verb is Verb_LaunchProjectile)
				{
					Apparel apparel = verb.FirstApparelPreventingShooting();
					if (apparel != null)
					{
						command_VerbTarget.Disable("ApparelPreventsShooting".Translate(verb.CasterPawn.Named("PAWN"), apparel.Named("APPAREL")).CapitalizeFirst());
					}
				}
				else if (EquipmentUtility.RolePreventsFromUsing(verb.CasterPawn, verb.EquipmentSource, out text))
				{
					command_VerbTarget.Disable(text);
				}
			}
			CompUniqueWeapon compUniqueWeapon;
			if ((!verb.EquipmentSource.TryGetComp(out compUniqueWeapon) || !compUniqueWeapon.IgnoreAccuracyMaluses) && verb.caster.Spawned && verb.caster.Map.weatherManager.CurWeatherMaxRangeCap >= 0f)
			{
				command_VerbTarget.defaultDescPostfix = "\n\n" + ("WeatherMaxRangeCap".Translate() + ": " + verb.caster.Map.weatherManager.curWeather.LabelCap).Colorize(ColoredText.WarningColor);
			}
			return command_VerbTarget;
		}

		// Token: 0x0600581D RID: 22557 RVA: 0x001C613C File Offset: 0x001C433C
		public Verb GetVerb(VerbCategory category)
		{
			List<Verb> allVerbs = this.AllVerbs;
			if (allVerbs != null)
			{
				for (int i = 0; i < allVerbs.Count; i++)
				{
					if (allVerbs[i].verbProps.category == category)
					{
						return allVerbs[i];
					}
				}
			}
			return null;
		}

		// Token: 0x0600581E RID: 22558 RVA: 0x001C6184 File Offset: 0x001C4384
		public void ExposeData()
		{
			Scribe_Collections.Look<Verb>(ref this.verbs, "verbs", LookMode.Deep, Array.Empty<object>());
			if (Scribe.mode == LoadSaveMode.ResolvingCrossRefs && this.verbs != null)
			{
				if (this.verbs.RemoveAll((Verb x) => x == null) != 0)
				{
					Log.Error("Some verbs were null after loading. directOwner=" + this.directOwner.ToStringSafe<IVerbOwner>());
				}
				List<Verb> sources = this.verbs;
				this.verbs = new List<Verb>();
				this.InitVerbs(delegate(Type type, string id)
				{
					Verb verb = sources.FirstOrDefault((Verb v) => v.loadID == id && v.GetType() == type);
					if (verb == null)
					{
						Log.Warning(string.Format("Replaced verb {0}/{1}; may have been changed through a version update or a mod change", type, id));
						verb = (Verb)Activator.CreateInstance(type);
					}
					this.verbs.Add(verb);
					return verb;
				});
			}
		}

		// Token: 0x0600581F RID: 22559 RVA: 0x001C6233 File Offset: 0x001C4433
		public void InitVerbsFromZero()
		{
			this.verbs = new List<Verb>();
			this.InitVerbs(delegate(Type type, string id)
			{
				Verb verb = (Verb)Activator.CreateInstance(type);
				this.verbs.Add(verb);
				return verb;
			});
		}

		// Token: 0x06005820 RID: 22560 RVA: 0x001C6254 File Offset: 0x001C4454
		private void InitVerbs(Func<Type, string, Verb> creator)
		{
			List<VerbProperties> verbProperties = this.directOwner.VerbProperties;
			if (verbProperties != null)
			{
				for (int i = 0; i < verbProperties.Count; i++)
				{
					try
					{
						VerbProperties verbProperties2 = verbProperties[i];
						string text = Verb.CalculateUniqueLoadID(this.directOwner, i);
						this.InitVerb(creator(verbProperties2.verbClass, text), verbProperties2, null, null, text);
					}
					catch (Exception ex)
					{
						string text2 = "Could not instantiate Verb (directOwner=";
						string text3 = this.directOwner.ToStringSafe<IVerbOwner>();
						string text4 = "): ";
						Exception ex2 = ex;
						Log.Error(text2 + text3 + text4 + ((ex2 != null) ? ex2.ToString() : null));
					}
				}
			}
			List<Tool> tools = this.directOwner.Tools;
			if (tools != null)
			{
				for (int j = 0; j < tools.Count; j++)
				{
					Tool tool = tools[j];
					foreach (ManeuverDef maneuverDef in tool.Maneuvers)
					{
						try
						{
							VerbProperties verb = maneuverDef.verb;
							string text5 = Verb.CalculateUniqueLoadID(this.directOwner, tool, maneuverDef);
							this.InitVerb(creator(verb.verbClass, text5), verb, tool, maneuverDef, text5);
						}
						catch (Exception ex3)
						{
							string text6 = "Could not instantiate Verb (directOwner=";
							string text7 = this.directOwner.ToStringSafe<IVerbOwner>();
							string text8 = "): ";
							Exception ex4 = ex3;
							Log.Error(text6 + text7 + text8 + ((ex4 != null) ? ex4.ToString() : null));
						}
					}
				}
			}
		}

		// Token: 0x06005821 RID: 22561 RVA: 0x001C63E0 File Offset: 0x001C45E0
		private void InitVerb(Verb verb, VerbProperties properties, Tool tool, ManeuverDef maneuver, string id)
		{
			verb.loadID = id;
			verb.verbProps = properties;
			verb.verbTracker = this;
			verb.tool = tool;
			verb.maneuver = maneuver;
			verb.caster = this.directOwner.ConstantCaster;
		}

		// Token: 0x06005822 RID: 22562 RVA: 0x001C6418 File Offset: 0x001C4618
		public void VerbsNeedReinitOnLoad()
		{
			this.verbs = null;
		}

		// Token: 0x04004037 RID: 16439
		public IVerbOwner directOwner;

		// Token: 0x04004038 RID: 16440
		private List<Verb> verbs;
	}
}
