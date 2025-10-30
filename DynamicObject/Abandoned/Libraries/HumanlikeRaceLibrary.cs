using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Verse;

namespace RimSpine2DFramework
{
    public static class HumanlikeRaceLibrary
    {
        private const string EmbeddedRaceXml = @"<?xml version=""1.0"" encoding=""utf-8"" ?>
<Defs>

  <ThingDef ParentName=""BasePawn"" Name=""SpecialAMYThingBasea"" Abstract=""True"">
    <statBases>
      <LeatherAmount>0</LeatherAmount>
      <ToxicResistance>1</ToxicResistance>
    </statBases>
    <race>
      <thinkTreeMain>Humanlike</thinkTreeMain>
      <thinkTreeConstant>AnimalConstant</thinkTreeConstant>
      <hasGenders>false</hasGenders>
      <nameGenerator>NamerAnimalGenericMale</nameGenerator>
      <nameGeneratorFemale>NamerAnimalGenericFemale</nameGeneratorFemale>
      <trainability>None</trainability>
      <bloodDef>Filth_Blood</bloodDef>
      <bloodSmearDef>Filth_BloodSmear</bloodSmearDef>
      <canBecomeShambler>true</canBecomeShambler>
      <renderTree>Animal</renderTree>
      <hediffGiverSets>
        <li>OrganicStandard</li>
		<li>Human</li>
      </hediffGiverSets>
    </race>
    <thingCategories>
      <li>Animals</li>
    </thingCategories>
    <recipes>
      <li>ExciseCarcinoma</li>
      <li>AdministerMechSerumHealer</li>
      <li>RemoveBodyPart</li>
      <li>Euthanize</li>
      <li>Anesthetize</li>
      <li>CureScaria</li>
      <li>Sterilize</li>
      <li MayRequire=""Ludeon.RimWorld.Royalty"">CureBloodRot</li>
      <li MayRequire=""Ludeon.RimWorld.Biotech"">TerminatePregnancy</li>
      <li MayRequire=""Ludeon.RimWorld.Anomaly"">SurgicalInspection</li>
      <li MayRequire=""Ludeon.RimWorld.Odyssey"">RemovePorcupineQuill</li>
    </recipes>
    <comps>
      <!-- For shambler animals -->
      <li Class=""CompProperties_HoldingPlatformTarget"" MayRequire=""Ludeon.RimWorld.Anomaly"">
        <baseEscapeIntervalMtbDays>120</baseEscapeIntervalMtbDays>
        <getsColdContainmentBonus>true</getsColdContainmentBonus>
      </li>
      <li MayRequire=""Ludeon.RimWorld.Anomaly"" Class=""CompProperties_Studiable"">
        <frequencyTicks>120000</frequencyTicks>
        <knowledgeCategory MayRequire=""Ludeon.RimWorld.Anomaly"">Basic</knowledgeCategory>
        <anomalyKnowledge>1</anomalyKnowledge>
        <requiresImprisonment>true</requiresImprisonment>
        <minMonolithLevelForStudy>1</minMonolithLevelForStudy>
      </li>
    </comps>
  </ThingDef>

  <ThingDef Abstract=""True"" ParentName=""SpecialAMYThingBasea"" Name=""BaseAMYa"">
    <devNote>squirrel-1</devNote>
    <statBases>
      <MoveSpeed>3.0</MoveSpeed>
      <MarketValue>0</MarketValue>
      <ComfyTemperatureMin>-30</ComfyTemperatureMin>
      <Wildness>0.98</Wildness>
    </statBases>
    <tools>
      <li>
        <label>left fist</label>
        <labelNoLocation>fist</labelNoLocation>
        <capacities>
          <li>Blunt</li>
        </capacities>
        <power>8.2</power>
        <cooldownTime>2</cooldownTime>
        <linkedBodyPartsGroup>LeftHand</linkedBodyPartsGroup>
        <surpriseAttack>
          <extraMeleeDamages>
            <li>
              <def>Stun</def>
              <amount>14</amount>
            </li>
          </extraMeleeDamages>
        </surpriseAttack>
      </li>
      <li>
        <label>right fist</label>
        <labelNoLocation>fist</labelNoLocation>
        <capacities>
          <li>Blunt</li>
        </capacities>
        <power>8.2</power>
        <cooldownTime>2</cooldownTime>
        <linkedBodyPartsGroup>RightHand</linkedBodyPartsGroup>
        <surpriseAttack>
          <extraMeleeDamages>
            <li>
              <def>Stun</def>
              <amount>14</amount>
            </li>
          </extraMeleeDamages>
        </surpriseAttack>
      </li>
    </tools>
    <race>
      <body>Human</body>
      <baseBodySize>1</baseBodySize>
      <baseHungerRate>0.115</baseHungerRate>
      <baseHealthScale>10</baseHealthScale>
      <foodType>VegetarianRoughAnimal</foodType>
      <leatherDef>Leather_Light</leatherDef>
      <trainability>Advanced</trainability>
      <petness>0.08</petness>
      <mateMtbHours>8</mateMtbHours>
      <gestationPeriodDays>18</gestationPeriodDays>
      <soundEating>Rodent_Eat</soundEating>
      <litterSizeCurve>
        <points>
          <li>(1.0, 0)</li>
          <li>(1.5, 1)</li>
          <li>(2.0, 1)</li>
          <li>(2.5, 0)</li>
        </points>
      </litterSizeCurve>
      <lifeExpectancy>8</lifeExpectancy>
      <lifeStageAges>
        <li>
          <def>AnimalBaby</def>
          <minAge>0</minAge>
        </li>
        <li>
          <def>AnimalJuvenile</def>
          <minAge>0.1</minAge>
        </li>
        <li>
          <def>AnimalAdult</def>
          <minAge>0.2222</minAge>
          <soundWounded>Pawn_Hare_Wounded</soundWounded>
          <soundDeath>Pawn_Hare_Death</soundDeath>
          <soundCall>Pawn_Hare_Call</soundCall>
          <soundAngry>Pawn_Hare_Angry</soundAngry>
        </li>
      </lifeStageAges>
      <soundMeleeHitPawn>Pawn_Melee_SmallScratch_HitPawn</soundMeleeHitPawn>
      <soundMeleeHitBuilding>Pawn_Melee_SmallScratch_HitBuilding</soundMeleeHitBuilding>
      <soundMeleeMiss>Pawn_Melee_SmallScratch_Miss</soundMeleeMiss>
    </race>
  </ThingDef>

  <ThingDef ParentName=""BaseAMYa"">
    <defName>AmiyaFurnacesFinale_Racea</defName>
    <label>“阿米娅”，炉芯终曲</label>
    <description>存在于每个故事尽头，带走每位角色，封闭每种可能，停止每段讲述。它是对终结的想象，亦是所有想象的终结，它是一切，唯独不是你熟悉的人。</description>
    <uiIconScale>1.5</uiIconScale>
	<comps>
	  <li Class=""RimSpine2DFramework.DynamicPawnComp_Properties"">
	  </li>
	</comps>
  </ThingDef>
</Defs>";

        public static void PopulateTerraHumanlikeRaces(EmbeddedDefLoader loader, EmbeddedDefDatabase database)
        {
            if (loader == null)
            {
                throw new ArgumentNullException(nameof(loader));
            }

            if (database == null)
            {
                throw new ArgumentNullException(nameof(database));
            }

            var document = new XmlDocument();
            document.LoadXml(EmbeddedRaceXml);

            var root = document.DocumentElement;
            if (root == null)
            {
                return;
            }

            try
            {
                loader.LoadFromXmlDocument(document, "EmbeddedHumanlikeRaces", ModStaticMethod.ThisMod?.Content, true);
                loader.FinalizeLoading();
            }
            catch (Exception ex)
            {
                Log.Warning($"[RimSpine2DFramework] Failed to load embedded humanlike races: {ex}");
            }
            finally
            {
                loader.RevertGlobalRegistrations();
            }

            foreach (var def in database.AllDefs())
            {
                if (def is ThingDef thingDef)
                {
                    ModDynamicObjectManager.tmpRaceDatabase[thingDef.defName] = thingDef;
                    Log.Warning($"tmpRace: {def.defName}");
                }
            }
        }

        public static bool TryGetHumanlikeRace(string defName, out ThingDef def) =>
            ModDynamicObjectManager.tmpRaceDatabase.TryGetValue(defName, out def);
    }
}
