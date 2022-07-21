using System;
using System.Collections.Generic;
using System.Reflection;

using HarmonyLib;
using MCM.Abstractions.Attributes;
using MCM.Abstractions.Attributes.v1;
using MCM.Abstractions.Dropdown;
using MCM.Abstractions.Settings.Base;
using MCM.Abstractions.Settings.Base.Global;

using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.ViewModelCollection;
using TaleWorlds.CampaignSystem.ViewModelCollection.CharacterDeveloper;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace LevellingCustomizer
{
    public class SubModule : MBSubModuleBase
    {
        protected override void OnSubModuleLoad()
        {
            base.OnSubModuleLoad();

            Harmony harmony = new Harmony("LevellingCustomizer");
            //Harmony.DEBUG = true;

            harmony.Patch(typeof(HeroDeveloper).GetMethod("GainRawXp", BindingFlags.NonPublic | BindingFlags.Instance),
                new HarmonyMethod(typeof(LevelXpPatch).GetMethod(nameof(LevelXpPatch.GainRawXpPrefix))));
            harmony.Patch(typeof(CharacterVM).GetMethod("RefreshCharacterValues", BindingFlags.Public | BindingFlags.Instance), null,
                new HarmonyMethod(typeof(LevelXpPatch).GetMethod(nameof(LevelXpPatch.RefreshCharacterValuesPostfix))));

            harmony.Patch(typeof(SkillVM).GetMethod("InitializeValues", BindingFlags.Public | BindingFlags.Instance), null,
                new HarmonyMethod(typeof(SkillXpPatch).GetMethod(nameof(SkillXpPatch.InitializeValuesPostfix))));
            harmony.Patch(typeof(SkillVM).GetMethod("RefreshWithCurrentValues", BindingFlags.Public | BindingFlags.Instance), null,
                new HarmonyMethod(typeof(SkillXpPatch).GetMethod(nameof(SkillXpPatch.RefreshWithCurrentValuesPostfix))));
            harmony.Patch(typeof(DefaultCharacterDevelopmentModel).GetMethod("CalculateLearningRate", new Type[] {typeof(Hero), typeof(SkillObject)}), null,
                new HarmonyMethod(typeof(SkillXpPatch).GetMethod(nameof(SkillXpPatch.CalculateLearningRatePostfix))));
        }

        protected override void OnSubModuleUnloaded()
        {
            base.OnSubModuleUnloaded();

        }

        protected override void OnBeforeInitialModuleScreenSetAsRoot()
        {
            base.OnBeforeInitialModuleScreenSetAsRoot();

        }
    }

    public class MySettings : AttributeGlobalSettings<MySettings>
    {
        public override string Id => "LevellingCustomizerSettings";
        public override string DisplayName => new TextObject("{=LC_Mod_Title}Levelling Customizer").ToString();
        public override string FolderName => "LevellingCustomizer";
        public override string FormatType => "json";

        #region General

        [SettingProperty("{=LC_Settings_Name_DE1F5FA}Apply To", RequireRestart = false, HintText = "{=LC_Settings_Desc_DE1F5FA}Select which heroes general customizations should apply to.", Order = 1)]
        [SettingPropertyGroup("{=LC_Settings_Group_7B73632D}General", GroupOrder = 1)]
        public DropdownDefault<string> GeneralApplyTo { get; set; } = new DropdownDefault<string>(new string[] { "All Heroes", "Player", "Player & Companions" }, selectedIndex: 0);

        [SettingProperty("{=LC_Settings_Name_B9759480}Levelling XP Multiplier", 0f, 100f, RequireRestart = false, HintText = "{=LC_Settings_Desc_B9759480}The multiplier for levelling XP gained.", Order = 2)]
        [SettingPropertyGroup("{=LC_Settings_Group_7B73632D}General", GroupOrder = 1)]
        public float LevellingXPMultiplier { get; set; } = 1f;

        [SettingProperty("{=LC_Settings_Name_77E9E64B}Levelling Smoothing Level", -40, 40, RequireRestart = false, HintText = "{=LC_Settings_Desc_77E9E64B}The level of exponential smoothing of Character Level requirement.", Order = 3)]
        [SettingPropertyGroup("{=LC_Settings_Group_7B73632D}General", GroupOrder = 1)]
        public int LevellingSmoothingLevel { get; set; } = 0;

        [SettingProperty("{=LC_Settings_Name_3BFC8463}Skill XP Multiplier", 0f, 100f, RequireRestart = false, HintText = "{=LC_Settings_Desc_3BFC8463}The multiplier for all skill XP gained.", Order = 4)]
        [SettingPropertyGroup("{=LC_Settings_Group_7B73632D}General", GroupOrder = 1)]
        public float SkillXPMultiplier { get; set; } = 1f;

        [SettingProperty("{=LC_Settings_Name_6BBC6D7A}Skill Smoothing Level", -40, 40, RequireRestart = false, HintText = "{=LC_Settings_Desc_6BBC6D7A}The level of exponential smoothing of Skill Level requirement.", Order = 5)]
        [SettingPropertyGroup("{=LC_Settings_Group_7B73632D}General", GroupOrder = 1)]
        public int SkillSmoothingLevel { get; set; } = 0;

        #endregion

        #region Skill Specific

        [SettingProperty("{=LC_Settings_Name_DE1F5FA}Apply To", RequireRestart = false, HintText = "{=LC_Settings_Desc_DE1F5FA}Select which heroes skill specific customizations should apply to.", Order = 1)]
        [SettingPropertyGroup("{=LC_Settings_Group_57915826}Skill Specific", GroupOrder = 2)]
        public DropdownDefault<string> SkillApplyTo { get; set; } = new DropdownDefault<string>(new string[] { "All Heroes", "Player", "Player & Companions" }, selectedIndex: 0);

        [SettingProperty("{=LC_Settings_Name_1F582182}One Handed XP Multiplier", 0f, 100f, RequireRestart = false, HintText = "{=LC_Settings_Desc_1F582182}The multiplier for all One Handed skill XP gained.", Order = 2)]
        [SettingPropertyGroup("{=LC_Settings_Group_57915826}Skill Specific", GroupOrder = 2)]
        public float OneHandedMultiplier { get; set; } = 1f;

        [SettingProperty("{=LC_Settings_Name_9D0F64A9}Two Handed XP Multiplier", 0f, 100f, RequireRestart = false, HintText = "{=LC_Settings_Desc_9D0F64A9}The multiplier for all Two Handed skill XP gained.", Order = 3)]
        [SettingPropertyGroup("{=LC_Settings_Group_57915826}Skill Specific", GroupOrder = 2)]
        public float TwoHandedMultiplier { get; set; } = 1f;

        [SettingProperty("{=LC_Settings_Name_453DE0D2}Polearm XP Multiplier", 0f, 100f, RequireRestart = false, HintText = "{=LC_Settings_Desc_453DE0D2}The multiplier for all Polearm skill XP gained.", Order = 4)]
        [SettingPropertyGroup("{=LC_Settings_Group_57915826}Skill Specific", GroupOrder = 2)]
        public float PolearmMultiplier { get; set; } = 1f;

        [SettingProperty("{=LC_Settings_Name_E5AF882D}Bow XP Multiplier", 0f, 100f, RequireRestart = false, HintText = "{=LC_Settings_Desc_E5AF882D}The multiplier for all Bow skill XP gained.", Order = 5)]
        [SettingPropertyGroup("{=LC_Settings_Group_57915826}Skill Specific", GroupOrder = 2)]
        public float BowMultiplier { get; set; } = 1f;

        [SettingProperty("{=LC_Settings_Name_7093A51C}Crossbow XP Multiplier", 0f, 100f, RequireRestart = false, HintText = "{=LC_Settings_Desc_7093A51C}The multiplier for all Crossbow skill XP gained.", Order = 6)]
        [SettingPropertyGroup("{=LC_Settings_Group_57915826}Skill Specific", GroupOrder = 2)]
        public float CrossbowMultiplier { get; set; } = 1f;

        [SettingProperty("{=LC_Settings_Name_FF6EF4CD}Throwing XP Multiplier", 0f, 100f, RequireRestart = false, HintText = "{=LC_Settings_Desc_FF6EF4CD}The multiplier for all Throwing skill XP gained.", Order = 7)]
        [SettingPropertyGroup("{=LC_Settings_Group_57915826}Skill Specific", GroupOrder = 2)]
        public float ThrowingMultiplier { get; set; } = 1f;

        [SettingProperty("{=LC_Settings_Name_E21B2F33}Riding XP Multiplier", 0f, 100f, RequireRestart = false, HintText = "{=LC_Settings_Desc_E21B2F33}The multiplier for all Riding skill XP gained.", Order = 8)]
        [SettingPropertyGroup("{=LC_Settings_Group_57915826}Skill Specific", GroupOrder = 2)]
        public float RidingMultiplier { get; set; } = 1f;

        [SettingProperty("{=LC_Settings_Name_96B72755}Athletics XP Multiplier", 0f, 100f, RequireRestart = false, HintText = "{=LC_Settings_Desc_96B72755}The multiplier for all Athletics skill XP gained.", Order = 9)]
        [SettingPropertyGroup("{=LC_Settings_Group_57915826}Skill Specific", GroupOrder = 2)]
        public float AthleticsMultiplier { get; set; } = 1f;

        [SettingProperty("{=LC_Settings_Name_49DC4B4}Smithing XP Multiplier", 0f, 100f, RequireRestart = false, HintText = "{=LC_Settings_Desc_49DC4B4}The multiplier for all Smithing skill XP gained.", Order = 10)]
        [SettingPropertyGroup("{=LC_Settings_Group_57915826}Skill Specific", GroupOrder = 2)]
        public float SmithingMultiplier { get; set; } = 1f;

        [SettingProperty("{=LC_Settings_Name_B5D922C0}Scouting XP Multiplier", 0f, 100f, RequireRestart = false, HintText = "{=LC_Settings_Desc_B5D922C0}The multiplier for all Scouting skill XP gained.", Order = 11)]
        [SettingPropertyGroup("{=LC_Settings_Group_57915826}Skill Specific", GroupOrder = 2)]
        public float ScoutingMultiplier { get; set; } = 1f;

        [SettingProperty("{=LC_Settings_Name_8599339C}Tactics XP Multiplier", 0f, 100f, RequireRestart = false, HintText = "{=LC_Settings_Desc_8599339C}The multiplier for all Tactics skill XP gained.", Order = 12)]
        [SettingPropertyGroup("{=LC_Settings_Group_57915826}Skill Specific", GroupOrder = 2)]
        public float TacticsMultiplier { get; set; } = 1f;

        [SettingProperty("{=LC_Settings_Name_90CFE18A}Roguery XP Multiplier", 0f, 100f, RequireRestart = false, HintText = "{=LC_Settings_Desc_90CFE18A}The multiplier for all Roguery skill XP gained.", Order = 13)]
        [SettingPropertyGroup("{=LC_Settings_Group_57915826}Skill Specific", GroupOrder = 2)]
        public float RogueryMultiplier { get; set; } = 1f;

        [SettingProperty("{=LC_Settings_Name_D6E4C0CB}Charm XP Multiplier", 0f, 100f, RequireRestart = false, HintText = "{=LC_Settings_Desc_D6E4C0CB}The multiplier for all Charm skill XP gained.", Order = 14)]
        [SettingPropertyGroup("{=LC_Settings_Group_57915826}Skill Specific", GroupOrder = 2)]
        public float CharmMultiplier { get; set; } = 1f;

        [SettingProperty("{=LC_Settings_Name_F81E1FCB}Leadership XP Multiplier", 0f, 100f, RequireRestart = false, HintText = "{=LC_Settings_Desc_F81E1FCB}The multiplier for all Leadership skill XP gained.", Order = 15)]
        [SettingPropertyGroup("{=LC_Settings_Group_57915826}Skill Specific", GroupOrder = 2)]
        public float LeadershipMultiplier { get; set; } = 1f;

        [SettingProperty("{=LC_Settings_Name_FFECEB6}Trade XP Multiplier", 0f, 100f, RequireRestart = false, HintText = "{=LC_Settings_Desc_FFECEB6}The multiplier for all Trade skill XP gained.", Order = 16)]
        [SettingPropertyGroup("{=LC_Settings_Group_57915826}Skill Specific", GroupOrder = 2)]
        public float TradeMultiplier { get; set; } = 1f;

        [SettingProperty("{=LC_Settings_Name_A60B9ADB}Steward XP Multiplier", 0f, 100f, RequireRestart = false, HintText = "{=LC_Settings_Desc_A60B9ADB}The multiplier for all Steward skill XP gained.", Order = 17)]
        [SettingPropertyGroup("{=LC_Settings_Group_57915826}Skill Specific", GroupOrder = 2)]
        public float StewardMultiplier { get; set; } = 1f;

        [SettingProperty("{=LC_Settings_Name_75F8A597}Medicine XP Multiplier", 0f, 100f, RequireRestart = false, HintText = "{=LC_Settings_Desc_75F8A597}The multiplier for all Medicine skill XP gained.", Order = 18)]
        [SettingPropertyGroup("{=LC_Settings_Group_57915826}Skill Specific", GroupOrder = 2)]
        public float MedicineMultiplier { get; set; } = 1f;

        [SettingProperty("{=LC_Settings_Name_BA2AB631}Engineering XP Multiplier", 0f, 100f, RequireRestart = false, HintText = "{=LC_Settings_Desc_BA2AB631}The multiplier for all Engineering skill XP gained.", Order = 19)]
        [SettingPropertyGroup("{=LC_Settings_Group_57915826}Skill Specific", GroupOrder = 2)]
        public float EngineeringMultiplier { get; set; } = 1f;

        #endregion

        public override IDictionary<string, Func<BaseSettings>> GetAvailablePresets()
        {
            var basePresets = base.GetAvailablePresets(); // include the 'Default' preset that MCM provides
            basePresets.Add("Smoother Levelling", () => new MySettings()
            {
                LevellingSmoothingLevel = 30,
                SkillSmoothingLevel = 20,
            });
            return basePresets;
        }
    }

    public class LevelXpPatch
    {
        private static readonly TextObject _levellingRateStr = new TextObject("{=LC_Levelling_Rate}(Mod) Levelling Rate", null);

        public static void GainRawXpPrefix(HeroDeveloper __instance, ref float rawXp, bool shouldNotify)
        {
            rawXp *= CalculateLevellingXpMultiplier(__instance.Hero);
        }

        public static void RefreshCharacterValuesPostfix(CharacterVM __instance)
        {
            GameTexts.SetVariable("STR1", _levellingRateStr.ToString() + ": " + CalculateLevellingXpMultiplier(__instance.Hero).ToString("0.00"));
            GameTexts.SetVariable("STR2", __instance.LevelHint.HintText.ToString());
            var str = GameTexts.FindText("str_string_newline_string", null).ToString();
            __instance.LevelHint.HintText = new TextObject("{=!}" + str, null);
        }

        public static float CalculateLevellingXpMultiplier(Hero hero)
        {
            var applyTo = MySettings.Instance?.GeneralApplyTo?.SelectedIndex ?? 0;
            if ((applyTo == 1 && !hero.IsHumanPlayerCharacter) || (applyTo == 2 && !hero.IsHumanPlayerCharacter && !hero.IsPlayerCompanion))
            {
                return 1f;
            }

            var levellingXPMultiplier = MySettings.Instance?.LevellingXPMultiplier ?? 1f;
            var levellingSmoothingLevel = MySettings.Instance?.LevellingSmoothingLevel ?? 0;
            if (levellingSmoothingLevel != 0)
            {
                levellingXPMultiplier *= MathF.Pow(1f + (levellingSmoothingLevel / 1000f), hero.Level);
            }

            return levellingXPMultiplier;
        }
    }

    public static class SkillXpPatch
    {
        private static readonly TextObject _learningRateStr = new TextObject("{=q1J4a8rr}Learning Rate", null);

        public static void InitializeValuesPostfix(SkillVM __instance, CharacterVM ____developerVM)
        {
            __instance.LearningRateTooltip = new BasicTooltipViewModel(() => GetLearningRateTooltip(__instance, ____developerVM));
        }

        public static void RefreshWithCurrentValuesPostfix(SkillVM __instance, CharacterVM ____developerVM)
        {
            var learningRate = CalculateLearningRate(__instance, ____developerVM, false).ResultNumber;
            GameTexts.SetVariable("COUNT", learningRate.ToString("0.00"));
            __instance.CurrentLearningRateText = GameTexts.FindText("str_learning_rate_COUNT", null).ToString();
            __instance.LearningRate = learningRate;
        }

        public static void CalculateLearningRatePostfix(ref float __result, Hero hero, SkillObject skill)
        {
            var skillValue = hero.GetSkillValue(skill);
            var multiplier = CalculateSkillXpMultiplier(hero, skill, skillValue);
            __result *= multiplier;
        }

        public static List<TooltipProperty> GetLearningRateTooltip(SkillVM skillVM, CharacterVM developerVM)
        {
            var learningRate = CalculateLearningRate(skillVM, developerVM, true);
            return CampaignUIHelper.GetTooltipForAccumulatingPropertyWithResult(_learningRateStr.ToString(), learningRate.ResultNumber, ref learningRate);
        }

        public static ExplainedNumber CalculateLearningRate(SkillVM skillVM, CharacterVM developerVM, bool includeDescriptions = false)
        {
            var level = developerVM.Hero.CharacterObject.Level;
            var attributeValue = developerVM.GetCurrentAttributePoint(skillVM.Skill.CharacterAttribute);
            var focus = skillVM.CurrentFocusLevel;
            var skillValue = skillVM.Level;
            var learningRate = CalculateLearningRate(developerVM.Hero, skillVM.Skill, attributeValue, focus, skillValue, level, includeDescriptions);
            return learningRate;
        }

        public static ExplainedNumber CalculateLearningRate(Hero hero, SkillObject skill, int attributeValue, int focusValue, int skillValue, int characterLevel, bool includeDescriptions = false)
        {
            var attributeName = skill.CharacterAttribute.Name;
            var learningRate = Campaign.Current.Models.CharacterDevelopmentModel.CalculateLearningRate(attributeValue, focusValue, skillValue, characterLevel, attributeName, includeDescriptions);
            var multiplier = 0f;
            if (learningRate.ResultNumber > 0f)
            {
                multiplier = (learningRate.ResultNumber * (CalculateSkillXpMultiplier(hero, skill, skillValue) - 1f)) / learningRate.BaseNumber;
            }

            learningRate.AddFactor(multiplier, new TextObject("{=LC_Learning_Rate_Factor}(Mod) Levelling Customizer", null));
            return learningRate;
        }

        public static float CalculateSkillXpMultiplier(Hero hero, SkillObject skill, int skillValue)
        {
            var applyTo = MySettings.Instance?.SkillApplyTo?.SelectedIndex ?? 0;
            if ((applyTo == 1 && !hero.IsHumanPlayerCharacter) || (applyTo == 2 && !hero.IsHumanPlayerCharacter && !hero.IsPlayerCompanion))
            {
                return 1f;
            }

            var skillSpecificMultiplier = 1f;
            if (skill == DefaultSkills.OneHanded) skillSpecificMultiplier = MySettings.Instance?.OneHandedMultiplier ?? 1f;
            else if (skill == DefaultSkills.TwoHanded) skillSpecificMultiplier = MySettings.Instance?.TwoHandedMultiplier ?? 1f;
            else if (skill == DefaultSkills.Polearm) skillSpecificMultiplier = MySettings.Instance?.PolearmMultiplier ?? 1f;
            else if (skill == DefaultSkills.Bow) skillSpecificMultiplier = MySettings.Instance?.BowMultiplier ?? 1f;
            else if (skill == DefaultSkills.Crossbow) skillSpecificMultiplier = MySettings.Instance?.CrossbowMultiplier ?? 1f;
            else if (skill == DefaultSkills.Throwing) skillSpecificMultiplier = MySettings.Instance?.ThrowingMultiplier ?? 1f;
            else if (skill == DefaultSkills.Riding) skillSpecificMultiplier = MySettings.Instance?.RidingMultiplier ?? 1f;
            else if (skill == DefaultSkills.Athletics) skillSpecificMultiplier = MySettings.Instance?.AthleticsMultiplier ?? 1f;
            else if (skill == DefaultSkills.Crafting) skillSpecificMultiplier = MySettings.Instance?.SmithingMultiplier ?? 1f;
            else if (skill == DefaultSkills.Scouting) skillSpecificMultiplier = MySettings.Instance?.ScoutingMultiplier ?? 1f;
            else if (skill == DefaultSkills.Tactics) skillSpecificMultiplier = MySettings.Instance?.TacticsMultiplier ?? 1f;
            else if (skill == DefaultSkills.Roguery) skillSpecificMultiplier = MySettings.Instance?.RogueryMultiplier ?? 1f;
            else if (skill == DefaultSkills.Charm) skillSpecificMultiplier = MySettings.Instance?.CharmMultiplier ?? 1f;
            else if (skill == DefaultSkills.Leadership) skillSpecificMultiplier = MySettings.Instance?.LeadershipMultiplier ?? 1f;
            else if (skill == DefaultSkills.Trade) skillSpecificMultiplier = MySettings.Instance?.TradeMultiplier ?? 1f;
            else if (skill == DefaultSkills.Steward) skillSpecificMultiplier = MySettings.Instance?.StewardMultiplier ?? 1f;
            else if (skill == DefaultSkills.Medicine) skillSpecificMultiplier = MySettings.Instance?.MedicineMultiplier ?? 1f;
            else if (skill == DefaultSkills.Engineering) skillSpecificMultiplier = MySettings.Instance?.EngineeringMultiplier ?? 1f;

            var skillXPMultiplier = MySettings.Instance?.SkillXPMultiplier ?? 1f;
            skillXPMultiplier *= skillSpecificMultiplier;
            var skillSmoothingLevel = MySettings.Instance?.SkillSmoothingLevel ?? 0;
            if (skillSmoothingLevel != 0)
            {
                skillXPMultiplier *= MathF.Pow(1f + (skillSmoothingLevel / 10000f), skillValue);
            }

            return skillXPMultiplier;
        }
    }
}