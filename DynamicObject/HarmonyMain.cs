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

namespace DynamicObject
{
    public class HarmonyMain
    {
        public HarmonyMain()
        {
            var harmonyInstance = new Harmony("DynamicObject.NazunaRei.kamijouko");
			harmonyInstance.PatchAll(Assembly.GetExecutingAssembly());
		}

		[HarmonyPatch(typeof(UIRoot_Entry))]
		[HarmonyPatch("DoMainMenu")]
		public class LoadAllPlayDatasPatch
		{
			static bool Prefix(UIRoot_Entry __instance)
			{
				if (!ModStaticMethod.AllLevelsLoaded)
                {
					LoadAndResolveAllDynamicDefs();
					ResolveAllStoryTellerCameras();
					ModStaticMethod.message = "loaded";
					ModStaticMethod.AllLevelsLoaded = true;
					Log.Warning(ModStaticMethod.message);
				}
				return true;
			}

			public static void LoadAndResolveAllDynamicDefs()
			{
				List<DynamicObjectDef> list = DefDatabase<DynamicObjectDef>.AllDefsListForReading;
				if (list.NullOrEmpty())
					return;

                List<ModContentPack> mods = LoadedModManager.RunningModsListForReading;
                string[] folderAbsDir = mods.Select(x => Path.Combine(x.RootDir, "Spines")).Where(x => Directory.Exists(x)).ToArray();
				List<AssetBundle> loadedAllAssetBundle = mods.SelectMany(x => x.assetBundles.loadedAssetBundles).ToList();
				string[] assetBundleAbsDir = mods.Select(x => Path.Combine(x.RootDir, "AssetBundles")).Where(x => Directory.Exists(x)).ToArray();

                foreach (DynamicObjectDef def in list)
				{
					if (def.spine == null)
						continue;
					if (def.spine.ver == null)
						def.spine.ver = "3.8";
					TextAsset atlasAsset;
					TextAsset skeletonAsset;
					Material[] materials = null;
					Texture2D[] textures = null;
					Shader shader = ShaderTypeDefOf.Cutout.Shader;
					AssetBundle ab;

					if (def.importMode == ImportMode.AssetBundle)
					{
						if (!loadedAllAssetBundle.Exists(x => x.name == def.spine.assetBundleName))
						{
							string abPath = assetBundleAbsDir.FirstOrDefault(x => File.Exists(Path.Combine(x, def.spine.assetBundleName)));
							if (abPath == null)
								continue;
                            ab = AssetBundle.LoadFromFile(Path.Combine(abPath, def.spine.assetBundleName));
						}
						else
							ab = loadedAllAssetBundle.First(x => x.name == def.spine.assetBundleName);

						atlasAsset = ab.LoadAsset<TextAsset>(def.spine.atlasPath);
						skeletonAsset = ab.LoadAsset<TextAsset>(def.spine.skeletonPath);

						materials = ab.LoadAllAssets<Material>();
						if (!materials.NullOrEmpty())
							materials = materials.Where(x => def.spine.materialNames.Contains(x.name)).ToArray();
						Log.Warning(materials.Length.ToString());


					}
					else
					{
						string txtPath = folderAbsDir.FirstOrDefault(x => File.Exists(Path.Combine(x, def.spine.atlasPath)));
						string jsonPath = folderAbsDir.FirstOrDefault(x => File.Exists(Path.Combine(x, def.spine.skeletonPath)));
						if (txtPath == null || jsonPath == null)
							continue;

                        string txt = File.ReadAllText(Path.Combine(txtPath, def.spine.atlasPath));
						string json = File.ReadAllText(Path.Combine(jsonPath, def.spine.skeletonPath));

						atlasAsset = new TextAsset(txt);
						skeletonAsset = new TextAsset(json);
						atlasAsset.name = Path.GetFileName(def.spine.atlasPath);
						skeletonAsset.name = Path.GetFileName(def.spine.skeletonPath);
						foreach (AssetBundle bund in ModStaticMethod.ThisMod.ModContentPack.assetBundles.loadedAssetBundles)
                        {
							Shader shade = bund.LoadAsset<Shader>(def.spine.shaderName);
							if (shade == null)
								continue;
							shader = shade;
							break;
						}
						Log.Warning(shader.name);
						textures = new Texture2D[def.spine.textures.Count];
						for (int i = 0; i < def.spine.textures.Count; i++)
						{
							Texture2D texture = ContentFinder<Texture2D>.Get(def.spine.textures[i].texPath);
							textures[i] = texture;
						}
						Log.Warning(textures.Length.ToString());
					}

					SpineTextAssetData data = new SpineTextAssetData(atlasAsset, skeletonAsset, materials, textures, shader);
					if (def.spine.ver == "3.5" && !ModDynamicObjectManager.spine35Database.ContainsKey(def.defName))
					{
						ModDynamicObjectManager.spine35Database.Add(def.defName, data);
					}
					else if (def.spine.ver == "3.8" && !ModDynamicObjectManager.spine38Database.ContainsKey(def.defName))
					{
						ModDynamicObjectManager.spine38Database.Add(def.defName, data);
					}
					else if (!ModDynamicObjectManager.spine41Database.ContainsKey(def.defName))
					{
						ModDynamicObjectManager.spine41Database.Add(def.defName, data);
					}
				}
			}

			public static void ResolveAllStoryTellerCameras()
			{
				List<DynamicStoryTellerDef> list = DefDatabase<DynamicStoryTellerDef>.AllDefsListForReading;
				if (list.NullOrEmpty())
					return;

				foreach (DynamicStoryTellerDef def in list)
				{
					if (def.dynamicObject == null || ModDynamicObjectManager.DynamicStoryTellerDatabase.ContainsKey(def.defName))
						continue;
					GameObject obj = new GameObject(def.defName);
					DynamicObjectInstance instance = obj.AddComponent<DynamicObjectInstance>();
					if (def.dynamicObject.spine.ver == "3.5")
					{
						if (!ModDynamicObjectManager.spine35Database.ContainsKey(def.dynamicObject.defName))
							continue;
						instance.ver = "3.5";
					}
					else if(def.dynamicObject.spine.ver == "3.8")
					{
						if (!ModDynamicObjectManager.spine38Database.ContainsKey(def.dynamicObject.defName))
							continue;
						instance.ver = "3.8";
					}
                    else
                    {
						if (!ModDynamicObjectManager.spine41Database.ContainsKey(def.dynamicObject.defName))
							continue;
						instance.ver = "4.1";
					}
					instance.key = def.dynamicObject;
					instance.def = def;
					Camera cam = obj.AddComponent<Camera>();
					cam.fieldOfView = 40;
					cam.clearFlags = CameraClearFlags.Color;
					cam.backgroundColor = new Color(0f, 0f, 0f, 0f);
					cam.useOcclusionCulling = false;
					cam.renderingPath = RenderingPath.Forward;
					cam.nearClipPlane = 0.3f;
					cam.farClipPlane = 10f;
					cam.depth = Current.Camera.depth - 1;
					cam.targetTexture = new RenderTexture((int)def.windowScale.x, (int)def.windowScale.y, 24, RenderTextureFormat.ARGB32, 0);
					UnityEngine.Object.DontDestroyOnLoad(obj);
					obj.SetActive(false);
					ModDynamicObjectManager.DynamicStoryTellerDatabase.Add(def.defName, obj);
				}
			}
		}

		[HarmonyPatch(typeof(StorytellerUI))]
        [HarmonyPatch("DrawStorytellerSelectionInterface")]
        public class DynamicStoryTellerPatch
        {
            static bool Prefix(Rect rect, ref StorytellerDef chosenStoryteller, ref DifficultyDef difficulty, ref Difficulty difficultyValues, Listing_Standard infoListing, ref Vector2 ___scrollPosition, ref Texture2D ___StorytellerHighlightTex, ref Vector2 ___explanationScrollPosition, ref AnimationCurve ___explanationScrollPositionAnimated, ref Rect ___explanationInnerRect, ref float ___sectionHeightThreats, ref float ___sectionHeightEconomy, ref float ___sectionHeightIdeology, ref float ___sectionHeightBiotech, ref float ___sectionHeightGeneral, ref float ___sectionHeightPlayerTools, ref float ___sectionHeightAdaptation)
            {
				string defName = chosenStoryteller.defName;
				List<DynamicStoryTellerDef> defs = DefDatabase<DynamicStoryTellerDef>.AllDefsListForReading;
				DynamicStoryTellerDef def = defs.FirstOrDefault(x => x.storyTeller.defName == defName);
				if (defs.NullOrEmpty() || def == null || (def != null && !ModDynamicObjectManager.DynamicStoryTellerDatabase.ContainsKey(def.defName)))
                {
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
                Rect rect3 = new Rect(0f, 120f, 290f, 9999f);
                float num = 300f;

                Rect position = new Rect(390f - outRect2.x, rect.height - Storyteller.PortraitSizeLarge.y - 1f, Storyteller.PortraitSizeLarge.x, Storyteller.PortraitSizeLarge.y);
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
                    GUI.DrawTexture(position, ModDynamicObjectManager.DynamicStoryTellerDatabase[def.defName].GetComponent<Camera>().targetTexture);

                    //点击互动逻辑，
                    //点击后DynamicObjectInstance的属性canInteract设为false（不可点击），
                    //然后在当前动画执行完毕后开始执行点击动画，添加动画完成后canInteract设为true的事件，
                    //并且执行完后再次循环Idle动画，添加每次单次循环完成后DynamicObjectInstance的属性IdleTimes加1的事件（记录循环次数）
                    if (Widgets.ButtonText(position, "", false, false) && instance.canInteract)
                    {
                        instance.canInteract = false;
                        if (instance.ver == "3.8")
                        {
                            Spine38.TrackEntry track = instance.spine38skeleton.AnimationState.AddAnimation(0, def.interactAnimationName, false, 0f);
                            track.Complete += delegate (Spine38.TrackEntry t)
                            {
                                if (track.Animation.Name == def.interactAnimationName)
                                    instance.canInteract = true;
                            };
                            Spine38.TrackEntry track2 = instance.spine38skeleton.AnimationState.AddAnimation(0, def.idleAnimationName, def.loop, 0f);
                            track2.Complete += delegate (Spine38.TrackEntry t)
                            {
                                if (instance.canInteract == true && track2.Animation.Name == def.idleAnimationName)
                                    instance.IdleTimes++;
                            };
                        }
                        else if (instance.ver == "3.5")
                        {
                            Spine35.TrackEntry track = instance.spine35skeleton.AnimationState.AddAnimation(0, def.interactAnimationName, false, 0f);
                            track.Complete += delegate (Spine35.TrackEntry t)
                            {
                                if (track.Animation.Name == def.interactAnimationName)
                                    instance.canInteract = true;
                            };
                            instance.spine35skeleton.AnimationState.AddAnimation(0, def.idleAnimationName, def.loop, 0f);
                        }
                        else
                        {
                            Spine41.TrackEntry track = instance.spine41skeleton.AnimationState.AddAnimation(0, def.interactAnimationName, false, 0f);
                            track.Complete += delegate (Spine41.TrackEntry t)
                            {
                                if (track.Animation.Name == def.interactAnimationName)
                                    instance.canInteract = true;
                            };
                            instance.spine38skeleton.AnimationState.AddAnimation(0, def.idleAnimationName, def.loop, 0f);
                        }
                    }
                }

                Widgets.Label(new Rect(0f, 0f, 300f, 999f), "HowStorytellersWork".Translate());
                
                if (chosenStoryteller != null && chosenStoryteller.listVisible)
				{
					Text.Anchor = TextAnchor.UpperLeft;
					infoListing.Begin(rect3);
					Text.Font = GameFont.Medium;
					infoListing.Indent(15f);
					infoListing.Label(chosenStoryteller.label, -1f, null);
					infoListing.Outdent(15f);
					Text.Font = GameFont.Small;
					infoListing.Gap(8f);
					infoListing.Label(chosenStoryteller.description, 160f, null);
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
					}
					num = rect3.y + infoListing.CurHeight;
					infoListing.End();
					if (difficulty != null && difficulty.isCustom)
					{
						if (___explanationScrollPositionAnimated != null)
						{
							float time2 = Time.time;
							if (time2 < ___explanationScrollPositionAnimated.keys.Last<Keyframe>().time)
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
						DrawCustomLeft(listing_Standard, difficultyValues, ref ___sectionHeightEconomy, ref ___sectionHeightThreats, ref ___sectionHeightIdeology, ref ___sectionHeightBiotech);
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

            private static Listing_Standard DrawCustomSectionStart(Listing_Standard listing, float height, string label, string tooltip = null)
            {
                listing.Gap(12f);
                listing.Label(label, -1f, tooltip);
                Listing_Standard listing_Standard = listing.BeginSection(height, 8f, 6f);
                listing_Standard.maxOneColumn = true;
                return listing_Standard;
            }

            private static void DrawCustomSectionEnd(Listing_Standard listing, Listing_Standard section, out float height)
            {
                listing.EndSection(section);
                height = section.CurHeight;
            }


			private static void DrawCustomLeft(Listing_Standard listing, Difficulty difficulty, ref float sectionHeightEconomy, ref float sectionHeightThreats, ref float sectionHeightIdeology, ref float sectionHeightBiotech)
			{
				Listing_Standard listing_Standard = DrawCustomSectionStart(listing, sectionHeightThreats, "DifficultyThreatSection".Translate(), null);
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

		[HarmonyPatch(typeof(PawnGraphicSet))]
		[HarmonyPatch("ResolveAllGraphics")]
		public class TestPatch
        {
			static void Postfix()
            {
				Log.Warning("1");
            }
        }
		[HarmonyPatch(typeof(PawnGraphicSet))]
		[HarmonyPatch("ResolveApparelGraphics")]
		public class TestPatch2
		{
			static void Postfix()
			{
				Log.Warning("2");
			}
		}

	}
}
