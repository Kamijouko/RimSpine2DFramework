using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using RimWorld;
using HarmonyLib;
using System.Reflection;
using Verse.Noise;
using System.Xml.Linq;

namespace RimSpine2DFramework
{
    public class HarmonyMain
    {

		/*[HarmonyPatch(typeof(UIRoot_Entry))]
		[HarmonyPatch("DoMainMenu")]
		public class LoadAllPlayDatasPatch
		{
			static bool Prefix(UIRoot_Entry __instance)
			{
				if (!ModStaticMethod.AllLevelsLoaded)
                {
					//LoadAndResolveAllDynamicDefs();
					//ResolveAllStoryTellerCameras();
					//ModStaticMethod.message = "loaded";
					//ModStaticMethod.AllLevelsLoaded = true;
					//Log.Warning(ModStaticMethod.message);
				}
				return true;
			}
		}*/


        [HarmonyPatch(typeof(StorytellerUI))]
        [HarmonyPatch("DrawStorytellerSelectionInterface")]
        public class DynamicStoryTellerPatch
        {
            static bool Prefix(Rect rect, ref StorytellerDef chosenStoryteller, ref DifficultyDef difficulty, 
				ref Difficulty difficultyValues, Listing_Standard infoListing, ref Vector2 ___scrollPosition, 
				ref Texture2D ___StorytellerHighlightTex, ref Vector2 ___explanationScrollPosition, ref AnimationCurve ___explanationScrollPositionAnimated, 
				ref Rect ___explanationInnerRect, ref float ___sectionHeightThreats, ref float ___sectionHeightEconomy, 
				ref float ___sectionHeightIdeology, ref float ___sectionHeightChildren, ref float ___sectionHeightGeneral, 
				ref float ___sectionHeightPlayerTools, ref float ___sectionHeightAdaptation, ref float ___sectionHeightAnomaly)
            {
				string defName = chosenStoryteller.defName;
				List<DynamicStoryTellerDef> defs = DefDatabase<DynamicStoryTellerDef>.AllDefsListForReading;
				DynamicStoryTellerDef def = defs.FirstOrDefault(x => x.storyTeller.defName == defName);
				if (defs.NullOrEmpty() || def == null || (def != null && !ModDynamicObjectManager.DynamicStoryTellerDatabase.ContainsKey(def.defName)))
                {
					if (ModDynamicObjectManager.lastChosenStoryTeller != null) 
						ModDynamicObjectManager.DynamicStoryTellerDatabase[ModDynamicObjectManager.lastChosenStoryTeller].SetActive(false);
						ModDynamicObjectManager.lastChosenStoryTeller = null;
                    return true;
                }

				Widgets.BeginGroup(rect);
				Rect outRect = new Rect(0f, 0f, Storyteller.PortraitSizeTiny.x + 16f, rect.height);
				Rect viewRect = new Rect(0f, 0f, Storyteller.PortraitSizeTiny.x, (float)DefDatabase<StorytellerDef>.AllDefs.Count<StorytellerDef>() * (Storyteller.PortraitSizeTiny.y + 10f));
				Widgets.BeginScrollView(outRect, ref ___scrollPosition, viewRect, true);
				Rect rect2 = new Rect(0f, 0f, Storyteller.PortraitSizeTiny.x, Storyteller.PortraitSizeTiny.y).ContractedBy(4f);
				foreach (StorytellerDef storytellerDef in from tel in DefDatabase<StorytellerDef>.AllDefs
														  orderby tel.listOrder
														  select tel)
				{
					if (storytellerDef.listVisible)
					{
						bool flag = chosenStoryteller == storytellerDef;
						Widgets.DrawOptionBackground(rect2, flag);
						if (Widgets.ButtonImage(rect2, storytellerDef.portraitTinyTex, Color.white, new Color(0.72f, 0.68f, 0.59f), true))
						{
							TutorSystem.Notify_Event("ChooseStoryteller");
							chosenStoryteller = storytellerDef;
						}
						if (flag)
						{
							GUI.DrawTexture(rect2, ___StorytellerHighlightTex);
						}
						rect2.y += rect2.height + 8f;
					}
				}
				Widgets.EndScrollView();
				Rect outRect2 = new Rect(outRect.xMax + 8f, 0f, rect.width - outRect.width - 8f, rect.height);
				___explanationInnerRect.width = outRect2.width - 16f;
				Widgets.BeginScrollView(outRect2, ref ___explanationScrollPosition, ___explanationInnerRect, true);
                Text.Font = GameFont.Small;
                Widgets.Label(new Rect(0f, 0f, 300f, 999f), "HowStorytellersWork".Translate());
                Rect rect3 = new Rect(0f, 120f, 290f, 9999f);
                float num = 300f;

                Rect position = new Rect(445f - outRect2.x, rect.height - def.windowScale.y - 1f, def.windowScale.x, def.windowScale.y);
                //Rect position = new Rect(390f - outRect2.x, rect.height - Storyteller.PortraitSizeLarge.y - 1f, Storyteller.PortraitSizeLarge.x, Storyteller.PortraitSizeLarge.y);
                if (chosenStoryteller != null && chosenStoryteller.listVisible)
				{
                    //绘制叙述者	
                    foreach (string name in ModDynamicObjectManager.DynamicStoryTellerDatabase.Keys)
                    {
                        if (name != def.defName && ModDynamicObjectManager.DynamicStoryTellerDatabase[name].activeSelf)
                            ModDynamicObjectManager.DynamicStoryTellerDatabase[name].SetActive(false);
                    }
                    DynamicObjectInstance instance = ModDynamicObjectManager.DynamicStoryTellerDatabase[def.defName].GetComponent<DynamicObjectInstance>();
                    if (instance.IsNull)
                        instance.CreateSpineAnimation();
                    ModDynamicObjectManager.DynamicStoryTellerDatabase[def.defName].SetActive(true);
                    ModDynamicObjectManager.lastChosenStoryTeller = def.defName;
                    GUI.DrawTexture(position, ModDynamicObjectManager.DynamicStoryTellerDatabase[def.defName].GetComponent<Camera>().targetTexture);
					

                    //点击互动逻辑，
                    //点击后DynamicObjectInstance的属性canInteract设为false（不可点击），
                    //然后在当前动画执行完毕后开始执行点击动画，添加动画完成后canInteract设为true的事件，
                    //并且执行完后再次循环Idle动画，添加每次单次循环完成后DynamicObjectInstance的属性IdleTimes加1的事件（记录循环次数）
                    if (Widgets.ButtonText(position, "", false, false) && instance.canInteract)
                    {
                        instance.PlayInteractionAnimation();
                    }
                }

                
                
                if (chosenStoryteller != null && chosenStoryteller.listVisible)
				{
                    //Rect position = new Rect(390f - outRect2.x, rect.height - Storyteller.PortraitSizeLarge.y - 1f, Storyteller.PortraitSizeLarge.x, Storyteller.PortraitSizeLarge.y);
                    //GUI.DrawTexture(position, chosenStoryteller.portraitLargeTex);
                    Text.Anchor = TextAnchor.UpperLeft;
					infoListing.Begin(rect3);
					Text.Font = GameFont.Medium;
					infoListing.Indent(15f);
					infoListing.Label(chosenStoryteller.label, -1f, tipSignal:null);
					infoListing.Outdent(15f);
					Text.Font = GameFont.Small;
					infoListing.Gap(8f);
					infoListing.Label(chosenStoryteller.description, 160f, tipSignal: null);
					infoListing.Gap(6f);
					foreach (DifficultyDef difficultyDef in DefDatabase<DifficultyDef>.AllDefs)
					{
						TaggedString taggedString = difficultyDef.LabelCap;
						if (difficultyDef.isCustom)
						{
							taggedString += "...";
						}
						if (infoListing.RadioButton(taggedString, difficulty == difficultyDef, 0f, difficultyDef.description.ResolveTags(), new float?(0f)))
						{
							if (!difficultyDef.isCustom)
							{
								difficultyValues.CopyFrom(difficultyDef);
							}
							else if (difficultyDef != difficulty)
							{
								difficultyValues.CopyFrom(DifficultyDefOf.Rough);
								float time = Time.time;
								float num2 = 0.6f;
								___explanationScrollPositionAnimated = AnimationCurve.EaseInOut(time, ___explanationScrollPosition.y, time + num2, ___explanationInnerRect.height);
							}
							difficulty = difficultyDef;
						}
						infoListing.Gap(3f);
					}
					if (Current.ProgramState == ProgramState.Entry)
					{
						infoListing.Gap(25f);
						bool active = Find.GameInitData.permadeathChosen && Find.GameInitData.permadeath;
						bool active2 = Find.GameInitData.permadeathChosen && !Find.GameInitData.permadeath;
						if (infoListing.RadioButton("ReloadAnytimeMode".Translate(), active2, 0f, "ReloadAnytimeModeInfo".Translate(), null))
						{
							Find.GameInitData.permadeathChosen = true;
							Find.GameInitData.permadeath = false;
						}
						infoListing.Gap(3f);
						if (infoListing.RadioButton("CommitmentMode".TranslateWithBackup("PermadeathMode"), active, 0f, "PermadeathModeInfo".Translate(), null))
						{
							Find.GameInitData.permadeathChosen = true;
							Find.GameInitData.permadeath = true;
						}
                        if (ModsConfig.AnomalyActive)
                        {
                            infoListing.Gap(15f);
                            if (infoListing.ButtonText("AnomalySettings".Translate() + "...", null, 1f))
                            {
                                if (difficulty == null)
                                {
                                    Messages.Message("MustChooseDifficulty".Translate(), MessageTypeDefOf.RejectInput, false);
                                }
                                else
                                {
                                    Find.WindowStack.Add(new Dialog_AnomalySettings(difficultyValues));
                                }
                            }
                        }
                    }
					num = rect3.y + infoListing.CurHeight;
					infoListing.End();
					if (difficulty != null && difficulty.isCustom)
					{
						if (___explanationScrollPositionAnimated != null)
						{
							float time2 = Time.time;
							if (time2 < ___explanationScrollPositionAnimated.keys.Last<UnityEngine.Keyframe>().time)
							{
								___explanationScrollPosition.y = ___explanationScrollPositionAnimated.Evaluate(time2);
							}
							else
							{
								___explanationScrollPositionAnimated = null;
							}
						}
						Listing_Standard listing_Standard = new Listing_Standard();
						float num3 = position.xMax - ___explanationInnerRect.x;
						listing_Standard.ColumnWidth = num3 / 2f - 17f;
						Rect rect4 = new Rect(0f, Math.Max(position.yMax, num) - 45f, num3, 9999f);
						listing_Standard.Begin(rect4);
						Text.Font = GameFont.Medium;
						listing_Standard.Indent(15f);
						listing_Standard.Label("DifficultyCustomSectionLabel".Translate(), -1f, null);
						listing_Standard.Outdent(15f);
						Text.Font = GameFont.Small;
						listing_Standard.Gap(12f);
						if (listing_Standard.ButtonText("DifficultyReset".Translate(), null, 1f))
						{
							MakeResetDifficultyFloatMenu(difficultyValues);
						}
						float curHeight = listing_Standard.CurHeight;
						float gapHeight = outRect2.height / 2f;
						DrawCustomLeft(listing_Standard, difficultyValues, ref ___sectionHeightEconomy, ref ___sectionHeightThreats, ref ___sectionHeightIdeology, ref ___sectionHeightChildren, ref ___sectionHeightAnomaly);
						listing_Standard.Gap(gapHeight);
						listing_Standard.NewColumn();
						listing_Standard.Gap(curHeight);
						DrawCustomRight(listing_Standard, difficultyValues, ref ___sectionHeightGeneral, ref ___sectionHeightPlayerTools, ref ___sectionHeightAdaptation);
						listing_Standard.Gap(gapHeight);
						num = rect4.y + listing_Standard.MaxColumnHeightSeen;
						listing_Standard.End();
					}
				}
				___explanationInnerRect.height = num;
				Widgets.EndScrollView();
				Widgets.EndGroup();
				return false;
            }

            private static void MakeResetDifficultyFloatMenu(Difficulty difficultyValues)
            {
                List<FloatMenuOption> list = new List<FloatMenuOption>();
                using (IEnumerator<DifficultyDef> enumerator = DefDatabase<DifficultyDef>.AllDefs.GetEnumerator())
                {
                    while (enumerator.MoveNext())
                    {
                        DifficultyDef d = enumerator.Current;
                        if (!d.isCustom)
                        {
                            list.Add(new FloatMenuOption(d.LabelCap, delegate ()
                            {
                                difficultyValues.CopyFrom(d);
                            }, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0));
                        }
                    }
                }
                Find.WindowStack.Add(new FloatMenu(list));
            }

            private static float Reciprocal(float f, float cutOff)
            {
                cutOff *= 10f;
                if (Mathf.Abs(f) < 0.01f)
                {
                    return cutOff;
                }
                if (f >= 0.99f * cutOff)
                {
                    return 0f;
                }
                return 1f / f;
            }

            private static void DrawCustomDifficultyCheckbox(Listing_Standard listing, string optionName, ref bool value, bool invert = false, bool showTooltip = true)
            {
                string str = invert ? "_Inverted" : "";
                string str2 = optionName.CapitalizeFirst();
                string key = "Difficulty_" + str2 + str + "_Label";
                string key2 = "Difficulty_" + str2 + str + "_Info";
                bool flag = invert ? (!value) : value;
                listing.CheckboxLabeled(key.Translate(), ref flag, showTooltip ? key2.Translate() : null);
                value = (invert ? (!flag) : flag);
            }

			private static void DrawDisabledCustomDifficultySetting(Listing_Standard listing, string optionName, TaggedString disableReason)
			{
				string str = optionName.CapitalizeFirst();
				string key = "Difficulty_" + str + "_Label";
				string key2 = "Difficulty_" + str + "_Info";
				Color color = GUI.color;
				GUI.color = ColoredText.SubtleGrayColor;
				listing.Label(key.Translate(), -1f, (key2.Translate() + "\n\n" + disableReason.Colorize(ColoredText.WarningColor)).ToString());
				GUI.color = color;
			}
            
			private static void DrawCustomDifficultySlider(Listing_Standard listing, string label, string labelSuffix, string tooltip, ref float value, ToStringStyle style, ToStringNumberSense numberSense, float min, float max, float precision = 0.01f, bool reciprocate = false, float reciprocalCutoff = 1000f)
            {
                float num = value;
                if (reciprocate)
                {
                    num = Reciprocal(num, reciprocalCutoff);
                }
                label = label.CapitalizeFirst() + ": " + num.ToStringByStyle(style, numberSense);
                if (!labelSuffix.NullOrEmpty())
                {
                    label = label + " - " + labelSuffix;
                }
                listing.Label(label, -1f, tipSignal:tooltip.CapitalizeFirst());
                float num2 = listing.Slider(num, min, max);
                if (num2 != num)
                {
                    num = GenMath.RoundTo(num2, precision);
                }
                if (reciprocate)
                {
                    num = Reciprocal(num, reciprocalCutoff);
                }
                value = num;
            }
            
			private static void DrawCustomDifficultySlider(Listing_Standard listing, string optionName, ref float value, ToStringStyle style, ToStringNumberSense numberSense, float min, float max, float precision = 0.01f, bool reciprocate = false, float reciprocalCutoff = 1000f)
            {
                string str = reciprocate ? "_Inverted" : "";
                string str2 = optionName.CapitalizeFirst();
                string key = "Difficulty_" + str2 + str + "_Label";
                string key2 = "Difficulty_" + str2 + str + "_Info";
                float num = value;
                if (reciprocate)
                {
                    num = Reciprocal(num, reciprocalCutoff);
                }
                TaggedString label = key.Translate() + ": " + num.ToStringByStyle(style, numberSense);
                listing.Label(label, -1f, key2.Translate());
                float num2 = listing.Slider(num, min, max);
                if (num2 != num)
                {
                    num = GenMath.RoundTo(num2, precision);
                }
                if (reciprocate)
                {
                    num = Reciprocal(num, reciprocalCutoff);
                }
                value = num;
            }

            private static void DrawCustomDifficultySlider(Listing_Standard listing, string label, string tooltip, ref float value, ToStringStyle style, ToStringNumberSense numberSense, float min, float max, float precision = 0.01f, bool reciprocate = false, float reciprocalCutoff = 1000f)
            {
                DrawCustomDifficultySlider(listing, label, null, tooltip, ref value, style, numberSense, min, max, precision, reciprocate, reciprocalCutoff);
            }

            private static Listing_Standard DrawCustomSectionStart(Listing_Standard listing, float height, string label, string tooltip = null)
            {
                listing.Gap(12f);
                listing.Label(label, -1f, tipSignal:tooltip);
                Listing_Standard listing_Standard = listing.BeginSection(height, 8f, 6f);
                listing_Standard.maxOneColumn = true;
                return listing_Standard;
            }

            private static void DrawCustomSectionEnd(Listing_Standard listing, Listing_Standard section, out float height)
            {
                listing.EndSection(section);
                height = section.CurHeight;
            }


			private static void DrawCustomLeft(Listing_Standard listing, Difficulty difficulty, ref float sectionHeightEconomy, ref float sectionHeightThreats, ref float sectionHeightIdeology, ref float sectionHeightChildren, ref float sectionHeightAnomaly)
			{
                /*Listing_Standard listing_Standard = DrawCustomSectionStart(listing, sectionHeightThreats, "DifficultyThreatSection".Translate(), null);
				DrawCustomDifficultySlider(listing_Standard, "threatScale", ref difficulty.threatScale, ToStringStyle.PercentZero, ToStringNumberSense.Absolute, 0f, 5f, 0.01f, false, 1000f);
				DrawCustomDifficultyCheckbox(listing_Standard, "allowBigThreats", ref difficulty.allowBigThreats, false, true);
				DrawCustomDifficultyCheckbox(listing_Standard, "allowViolentQuests", ref difficulty.allowViolentQuests, false, true);
				DrawCustomDifficultyCheckbox(listing_Standard, "allowIntroThreats", ref difficulty.allowIntroThreats, false, true);
				DrawCustomDifficultyCheckbox(listing_Standard, "predatorsHuntHumanlikes", ref difficulty.predatorsHuntHumanlikes, false, true);
				DrawCustomDifficultyCheckbox(listing_Standard, "allowExtremeWeatherIncidents", ref difficulty.allowExtremeWeatherIncidents, false, true);
				DrawCustomSectionEnd(listing, listing_Standard, out sectionHeightThreats);
				listing_Standard = DrawCustomSectionStart(listing, sectionHeightEconomy, "DifficultyEconomySection".Translate(), null);
				DrawCustomDifficultySlider(listing_Standard, "cropYieldFactor", ref difficulty.cropYieldFactor, ToStringStyle.PercentZero, ToStringNumberSense.Absolute, 0f, 5f, 0.01f, false, 1000f);
				DrawCustomDifficultySlider(listing_Standard, "mineYieldFactor", ref difficulty.mineYieldFactor, ToStringStyle.PercentZero, ToStringNumberSense.Absolute, 0f, 5f, 0.01f, false, 1000f);
				DrawCustomDifficultySlider(listing_Standard, "butcherYieldFactor", ref difficulty.butcherYieldFactor, ToStringStyle.PercentZero, ToStringNumberSense.Absolute, 0f, 5f, 0.01f, false, 1000f);
				DrawCustomDifficultySlider(listing_Standard, "researchSpeedFactor", ref difficulty.researchSpeedFactor, ToStringStyle.PercentZero, ToStringNumberSense.Absolute, 0f, 5f, 0.01f, false, 1000f);
				DrawCustomDifficultySlider(listing_Standard, "questRewardValueFactor", ref difficulty.questRewardValueFactor, ToStringStyle.PercentZero, ToStringNumberSense.Absolute, 0f, 5f, 0.01f, false, 1000f);
				DrawCustomDifficultySlider(listing_Standard, "raidLootPointsFactor", ref difficulty.raidLootPointsFactor, ToStringStyle.PercentZero, ToStringNumberSense.Absolute, 0f, 5f, 0.01f, false, 1000f);
				DrawCustomDifficultySlider(listing_Standard, "tradePriceFactorLoss", ref difficulty.tradePriceFactorLoss, ToStringStyle.PercentZero, ToStringNumberSense.Absolute, 0f, 0.5f, 0.01f, false, 1000f);
				DrawCustomDifficultySlider(listing_Standard, "maintenanceCostFactor", ref difficulty.maintenanceCostFactor, ToStringStyle.PercentZero, ToStringNumberSense.Absolute, 0.01f, 1f, 0.01f, false, 1000f);
				DrawCustomDifficultySlider(listing_Standard, "scariaRotChance", ref difficulty.scariaRotChance, ToStringStyle.PercentZero, ToStringNumberSense.Absolute, 0f, 1f, 0.01f, false, 1000f);
				DrawCustomDifficultySlider(listing_Standard, "enemyDeathOnDownedChanceFactor", ref difficulty.enemyDeathOnDownedChanceFactor, ToStringStyle.PercentZero, ToStringNumberSense.Absolute, 0f, 1f, 0.01f, false, 1000f);
				DrawCustomSectionEnd(listing, listing_Standard, out sectionHeightEconomy);
				if (ModsConfig.IdeologyActive)
				{
					listing_Standard = DrawCustomSectionStart(listing, sectionHeightIdeology, "DifficultyIdeologySection".Translate(), null);
					DrawCustomDifficultySlider(listing_Standard, "lowPopConversionBoost", ref difficulty.lowPopConversionBoost, ToStringStyle.Integer, ToStringNumberSense.Factor, 1f, 5f, 1f, false, 1000f);
					DrawCustomSectionEnd(listing, listing_Standard, out sectionHeightIdeology);
				}
				if (ModsConfig.BiotechActive)
				{
					listing_Standard = DrawCustomSectionStart(listing, sectionHeightBiotech, "DifficultyBiotechSection".Translate(), null);
					DrawCustomDifficultyCheckbox(listing_Standard, "noBabiesOrChildren", ref difficulty.noBabiesOrChildren, false, true);
					DrawCustomDifficultyCheckbox(listing_Standard, "babiesAreHealthy", ref difficulty.babiesAreHealthy, false, true);
					if (!difficulty.noBabiesOrChildren)
					{
						DrawCustomDifficultyCheckbox(listing_Standard, "childRaidersAllowed", ref difficulty.childRaidersAllowed, false, true);
					}
					else
					{
						DrawDisabledCustomDifficultySetting(listing_Standard, "childRaidersAllowed", "BabiesAreHealthyDisableReason".Translate());
					}
					DrawCustomDifficultySlider(listing_Standard, "childAgingRate", ref difficulty.childAgingRate, ToStringStyle.Integer, ToStringNumberSense.Factor, 1f, 6f, 1f, false, 1000f);
					DrawCustomDifficultySlider(listing_Standard, "adultAgingRate", ref difficulty.adultAgingRate, ToStringStyle.Integer, ToStringNumberSense.Factor, 1f, 6f, 1f, false, 1000f);
					DrawCustomDifficultySlider(listing_Standard, "wastepackInfestationChanceFactor", ref difficulty.wastepackInfestationChanceFactor, ToStringStyle.PercentZero, ToStringNumberSense.Absolute, 0f, 5f, 0.01f, false, 1000f);
					DrawCustomSectionEnd(listing, listing_Standard, out sectionHeightBiotech);
				}*/

                Listing_Standard listing_Standard = DrawCustomSectionStart(listing, sectionHeightThreats, "DifficultyThreatSection".Translate(), null);
                DrawCustomDifficultySlider(listing_Standard, "threatScale", ref difficulty.threatScale, ToStringStyle.PercentZero, ToStringNumberSense.Absolute, 0f, 5f, 0.01f, false, 1000f);
                DrawCustomDifficultyCheckbox(listing_Standard, "allowBigThreats", ref difficulty.allowBigThreats, false, true);
                DrawCustomDifficultyCheckbox(listing_Standard, "allowViolentQuests", ref difficulty.allowViolentQuests, false, true);
                DrawCustomDifficultyCheckbox(listing_Standard, "allowIntroThreats", ref difficulty.allowIntroThreats, false, true);
                DrawCustomDifficultyCheckbox(listing_Standard, "predatorsHuntHumanlikes", ref difficulty.predatorsHuntHumanlikes, false, true);
                DrawCustomDifficultyCheckbox(listing_Standard, "allowExtremeWeatherIncidents", ref difficulty.allowExtremeWeatherIncidents, false, true);
                if (ModsConfig.BiotechActive)
                {
                    DrawCustomDifficultySlider(listing_Standard, "wastepackInfestationChanceFactor", ref difficulty.wastepackInfestationChanceFactor, ToStringStyle.PercentZero, ToStringNumberSense.Absolute, 0f, 5f, 0.01f, false, 1000f);
                }
                DrawCustomSectionEnd(listing, listing_Standard, out sectionHeightThreats);
                listing_Standard = DrawCustomSectionStart(listing, sectionHeightEconomy, "DifficultyEconomySection".Translate(), null);
                DrawCustomDifficultySlider(listing_Standard, "cropYieldFactor", ref difficulty.cropYieldFactor, ToStringStyle.PercentZero, ToStringNumberSense.Absolute, 0f, 5f, 0.01f, false, 1000f);
                DrawCustomDifficultySlider(listing_Standard, "mineYieldFactor", ref difficulty.mineYieldFactor, ToStringStyle.PercentZero, ToStringNumberSense.Absolute, 0f, 5f, 0.01f, false, 1000f);
                DrawCustomDifficultySlider(listing_Standard, "butcherYieldFactor", ref difficulty.butcherYieldFactor, ToStringStyle.PercentZero, ToStringNumberSense.Absolute, 0f, 5f, 0.01f, false, 1000f);
                DrawCustomDifficultySlider(listing_Standard, "researchSpeedFactor", ref difficulty.researchSpeedFactor, ToStringStyle.PercentZero, ToStringNumberSense.Absolute, 0f, 5f, 0.01f, false, 1000f);
                DrawCustomDifficultySlider(listing_Standard, "questRewardValueFactor", ref difficulty.questRewardValueFactor, ToStringStyle.PercentZero, ToStringNumberSense.Absolute, 0f, 5f, 0.01f, false, 1000f);
                DrawCustomDifficultySlider(listing_Standard, "raidLootPointsFactor", ref difficulty.raidLootPointsFactor, ToStringStyle.PercentZero, ToStringNumberSense.Absolute, 0f, 5f, 0.01f, false, 1000f);
                DrawCustomDifficultySlider(listing_Standard, "tradePriceFactorLoss", ref difficulty.tradePriceFactorLoss, ToStringStyle.PercentZero, ToStringNumberSense.Absolute, 0f, 0.5f, 0.01f, false, 1000f);
                DrawCustomDifficultySlider(listing_Standard, "maintenanceCostFactor", ref difficulty.maintenanceCostFactor, ToStringStyle.PercentZero, ToStringNumberSense.Absolute, 0.01f, 1f, 0.01f, false, 1000f);
                DrawCustomDifficultySlider(listing_Standard, "scariaRotChance", ref difficulty.scariaRotChance, ToStringStyle.PercentZero, ToStringNumberSense.Absolute, 0f, 1f, 0.01f, false, 1000f);
                DrawCustomDifficultySlider(listing_Standard, "enemyDeathOnDownedChanceFactor", ref difficulty.enemyDeathOnDownedChanceFactor, ToStringStyle.PercentZero, ToStringNumberSense.Absolute, 0f, 1f, 0.01f, false, 1000f);
                DrawCustomSectionEnd(listing, listing_Standard, out sectionHeightEconomy);
                if (ModsConfig.IdeologyActive)
                {
                    listing_Standard = DrawCustomSectionStart(listing, sectionHeightIdeology, "DifficultyIdeologySection".Translate(), null);
                    DrawCustomDifficultySlider(listing_Standard, "lowPopConversionBoost", ref difficulty.lowPopConversionBoost, ToStringStyle.Integer, ToStringNumberSense.Factor, 1f, 5f, 1f, false, 1000f);
                    DrawCustomSectionEnd(listing, listing_Standard, out sectionHeightIdeology);
                }
                if (ModsConfig.AnomalyActive && difficulty.AnomalyPlaystyleDef.enableAnomalyContent)
                {
                    listing_Standard = DrawCustomSectionStart(listing, sectionHeightAnomaly, "DifficultyAnomalySection".Translate(), null);
                    if (difficulty.AnomalyPlaystyleDef.overrideThreatFraction)
                    {
                        float num = difficulty.overrideAnomalyThreatsFraction ?? 0.15f;
                        DrawCustomDifficultySlider(listing_Standard, "Difficulty_AnomalyThreats_Label".Translate(), Dialog_AnomalySettings.GetFrequencyLabel(num), "Difficulty_AnomalyThreats_Info".Translate(), ref num, ToStringStyle.PercentZero, ToStringNumberSense.Absolute, 0f, 1f, 0.01f, false, 1000f);
                        difficulty.overrideAnomalyThreatsFraction = new float?(num);
                    }
                    else
                    {
                        DrawCustomDifficultySlider(listing_Standard, "Difficulty_AnomalyThreatsInactive_Label".Translate(), Dialog_AnomalySettings.GetFrequencyLabel(difficulty.anomalyThreatsInactiveFraction), "Difficulty_AnomalyThreatsInactive_Info".Translate(), ref difficulty.anomalyThreatsInactiveFraction, ToStringStyle.PercentZero, ToStringNumberSense.Absolute, 0f, 1f, 0.01f, false, 1000f);
                        float anomalyThreatsActiveFraction = difficulty.anomalyThreatsActiveFraction;
                        DrawCustomDifficultySlider(listing_Standard, "Difficulty_AnomalyThreatsActive_Label".Translate(), Dialog_AnomalySettings.GetFrequencyLabel(anomalyThreatsActiveFraction), "Difficulty_AnomalyThreatsActive_Info".Translate(Mathf.Clamp01(anomalyThreatsActiveFraction).ToStringPercent(), Mathf.Clamp01(anomalyThreatsActiveFraction * 1.5f).ToStringPercent()), ref difficulty.anomalyThreatsActiveFraction, ToStringStyle.PercentZero, ToStringNumberSense.Absolute, 0.1f, 1f, 0.01f, false, 1000f);
                    }
                    DrawCustomDifficultySlider(listing_Standard, "Difficulty_StudyEfficiency_Label".Translate(), "Difficulty_StudyEfficiency_Info".Translate(), ref difficulty.studyEfficiencyFactor, ToStringStyle.PercentZero, ToStringNumberSense.Absolute, 0f, 5f, 0.01f, false, 1000f);
                    DrawCustomSectionEnd(listing, listing_Standard, out sectionHeightAnomaly);
                }
                if (ModsConfig.BiotechActive)
                {
                    listing_Standard = DrawCustomSectionStart(listing, sectionHeightChildren, "DifficultyChildrenSection".Translate(), null);
                    DrawCustomDifficultyCheckbox(listing_Standard, "noBabiesOrChildren", ref difficulty.noBabiesOrChildren, false, true);
                    DrawCustomDifficultyCheckbox(listing_Standard, "babiesAreHealthy", ref difficulty.babiesAreHealthy, false, true);
                    if (!difficulty.noBabiesOrChildren)
                    {
                        DrawCustomDifficultyCheckbox(listing_Standard, "childRaidersAllowed", ref difficulty.childRaidersAllowed, false, true);
                        if (ModsConfig.AnomalyActive)
                        {
                            DrawCustomDifficultyCheckbox(listing_Standard, "childShamblersAllowed", ref difficulty.childShamblersAllowed, false, true);
                        }
                    }
                    else
                    {
                        DrawDisabledCustomDifficultySetting(listing_Standard, "childRaidersAllowed", "BabiesAreHealthyDisableReason".Translate());
                        if (ModsConfig.AnomalyActive)
                        {
                            DrawDisabledCustomDifficultySetting(listing_Standard, "childShamblersAllowed", "BabiesAreHealthyDisableReason".Translate());
                        }
                    }
                    DrawCustomDifficultySlider(listing_Standard, "childAgingRate", ref difficulty.childAgingRate, ToStringStyle.Integer, ToStringNumberSense.Factor, 1f, 6f, 1f, false, 1000f);
                    DrawCustomDifficultySlider(listing_Standard, "adultAgingRate", ref difficulty.adultAgingRate, ToStringStyle.Integer, ToStringNumberSense.Factor, 1f, 6f, 1f, false, 1000f);
                    DrawCustomSectionEnd(listing, listing_Standard, out sectionHeightChildren);
                }
            }

			private static void DrawCustomRight(Listing_Standard listing, Difficulty difficulty, ref float sectionHeightGeneral, ref float sectionHeightPlayerTools, ref float sectionHeightAdaptation)
			{
				Listing_Standard listing_Standard = DrawCustomSectionStart(listing, sectionHeightGeneral, "DifficultyGeneralSection".Translate(), null);
				DrawCustomDifficultySlider(listing_Standard, "colonistMoodOffset", ref difficulty.colonistMoodOffset, ToStringStyle.Integer, ToStringNumberSense.Offset, -20f, 20f, 1f, false, 1000f);
				DrawCustomDifficultySlider(listing_Standard, "foodPoisonChanceFactor", ref difficulty.foodPoisonChanceFactor, ToStringStyle.PercentZero, ToStringNumberSense.Absolute, 0f, 5f, 0.01f, false, 1000f);
				DrawCustomDifficultySlider(listing_Standard, "manhunterChanceOnDamageFactor", ref difficulty.manhunterChanceOnDamageFactor, ToStringStyle.PercentZero, ToStringNumberSense.Absolute, 0f, 5f, 0.01f, false, 1000f);
				DrawCustomDifficultySlider(listing_Standard, "playerPawnInfectionChanceFactor", ref difficulty.playerPawnInfectionChanceFactor, ToStringStyle.PercentZero, ToStringNumberSense.Absolute, 0f, 5f, 0.01f, false, 1000f);
				DrawCustomDifficultySlider(listing_Standard, "diseaseIntervalFactor", ref difficulty.diseaseIntervalFactor, ToStringStyle.PercentZero, ToStringNumberSense.Absolute, 0f, 5f, 0.01f, true, 100f);
				DrawCustomDifficultySlider(listing_Standard, "enemyReproductionRateFactor", ref difficulty.enemyReproductionRateFactor, ToStringStyle.PercentZero, ToStringNumberSense.Absolute, 0f, 5f, 0.01f, false, 1000f);
				DrawCustomDifficultySlider(listing_Standard, "deepDrillInfestationChanceFactor", ref difficulty.deepDrillInfestationChanceFactor, ToStringStyle.PercentZero, ToStringNumberSense.Absolute, 0f, 5f, 0.01f, false, 1000f);
				DrawCustomDifficultySlider(listing_Standard, "friendlyFireChanceFactor", ref difficulty.friendlyFireChanceFactor, ToStringStyle.PercentZero, ToStringNumberSense.Absolute, 0f, 1f, 0.01f, false, 1000f);
				DrawCustomDifficultySlider(listing_Standard, "allowInstantKillChance", ref difficulty.allowInstantKillChance, ToStringStyle.PercentZero, ToStringNumberSense.Absolute, 0f, 1f, 0.01f, false, 1000f);
				DrawCustomDifficultyCheckbox(listing_Standard, "peacefulTemples", ref difficulty.peacefulTemples, true, true);
				DrawCustomDifficultyCheckbox(listing_Standard, "allowCaveHives", ref difficulty.allowCaveHives, false, true);
				DrawCustomDifficultyCheckbox(listing_Standard, "unwaveringPrisoners", ref difficulty.unwaveringPrisoners, false, true);
				DrawCustomSectionEnd(listing, listing_Standard, out sectionHeightGeneral);
				listing_Standard = DrawCustomSectionStart(listing, sectionHeightPlayerTools, "DifficultyPlayerToolsSection".Translate(), null);
				DrawCustomDifficultyCheckbox(listing_Standard, "allowTraps", ref difficulty.allowTraps, false, true);
				DrawCustomDifficultyCheckbox(listing_Standard, "allowTurrets", ref difficulty.allowTurrets, false, true);
				DrawCustomDifficultyCheckbox(listing_Standard, "allowMortars", ref difficulty.allowMortars, false, true);
				DrawCustomDifficultyCheckbox(listing_Standard, "classicMortars", ref difficulty.classicMortars, false, true);
				DrawCustomSectionEnd(listing, listing_Standard, out sectionHeightPlayerTools);
				listing_Standard = DrawCustomSectionStart(listing, sectionHeightAdaptation, "DifficultyAdaptationSection".Translate(), null);
				DrawCustomDifficultySlider(listing_Standard, "adaptationGrowthRateFactorOverZero", ref difficulty.adaptationGrowthRateFactorOverZero, ToStringStyle.PercentZero, ToStringNumberSense.Absolute, 0f, 1f, 0.01f, false, 1000f);
				DrawCustomDifficultySlider(listing_Standard, "adaptationEffectFactor", ref difficulty.adaptationEffectFactor, ToStringStyle.PercentZero, ToStringNumberSense.Absolute, 0f, 1f, 0.01f, false, 1000f);
				DrawCustomDifficultyCheckbox(listing_Standard, "fixedWealthMode", ref difficulty.fixedWealthMode, false, true);
				GUI.enabled = difficulty.fixedWealthMode;
				float num = Mathf.Round(12f / difficulty.fixedWealthTimeFactor);
				DrawCustomDifficultySlider(listing_Standard, "fixedWealthTimeFactor", ref num, ToStringStyle.Integer, ToStringNumberSense.Absolute, 1f, 20f, 1f, false, 1000f);
				difficulty.fixedWealthTimeFactor = 12f / num;
				GUI.enabled = true;
				DrawCustomSectionEnd(listing, listing_Standard, out sectionHeightAdaptation);
			}
		}


        [HarmonyPatch]
        public static class LanguageReloadPatch
        {
            private static readonly string[] TargetMethodNames = new[]
            {
                "SelectLanguage",
                "SetActiveLanguage",
                "TrySetActiveLanguage",
                "TrySelectActiveLanguage"
            };

            static IEnumerable<MethodBase> TargetMethods()
            {
                Type languageDatabaseType = typeof(LanguageDatabase);
                foreach (string methodName in TargetMethodNames)
                {
                    MethodInfo method = AccessTools.Method(languageDatabaseType, methodName);
                    if (method != null)
                    {
                        yield return method;
                    }
                }
            }

            static void Postfix()
            {
                ThisModBase.QueueReloadForLanguageChange();
            }
        }

		/*[HarmonyPatch(typeof(PawnGraphicSet))]
		[HarmonyPatch("ResolveAllGraphics")]
		public class TestPatch
        {
			static void Postfix()
            {
				Log.Warning("1");
            }
        }*/
		/*[HarmonyPatch(typeof(PawnGraphicSet))]
		[HarmonyPatch("ResolveApparelGraphics")]
		public class TestPatch2
		{
			static void Postfix()
			{
				Log.Warning("2");
			}
		}*/

	}
}
