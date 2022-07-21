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
            harmony.Patch(typeof(DefaultCharacterDevelopmentModel).GetMethod("CalculateLearningLimit", BindingFlags.Public | BindingFlags.Instance), null,
                new HarmonyMethod(typeof(SkillXpPatch).GetMethod(nameof(SkillXpPatch.CalculateLearningLimitPostfix))));
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

        [SettingProperty("{=LC_Settings_Name_567E405D89}Apply To", RequireRestart = false, HintText = "{=LC_Settings_Desc_567E405D89}Select which heroes general customizations should apply to.", Order = 1)]
        [SettingPropertyGroup("{=LC_Settings_Group_0DB377921F}General", GroupOrder = 1)]
        public DropdownDefault<string> GeneralApplyTo { get; set; } = new DropdownDefault<string>(new string[] { "All Heroes", "Player", "Player & Companions" }, selectedIndex: 0);

        [SettingProperty("{=LC_Settings_Name_1122D38AAE}Levelling XP Multiplier", 0f, 100f, RequireRestart = false, HintText = "{=LC_Settings_Desc_1122D38AAE}The multiplier for levelling XP gained.", Order = 2)]
        [SettingPropertyGroup("{=LC_Settings_Group_0DB377921F}General", GroupOrder = 1)]
        public float LevellingXPMultiplier { get; set; } = 1f;

        [SettingProperty("{=LC_Settings_Name_BC96BC6657}Levelling Smoothing Level", -40, 40, RequireRestart = false, HintText = "{=LC_Settings_Desc_BC96BC6657}The level of exponential smoothing of Character Level requirement.", Order = 3)]
        [SettingPropertyGroup("{=LC_Settings_Group_0DB377921F}General", GroupOrder = 1)]
        public int LevellingSmoothingLevel { get; set; } = 0;

        [SettingProperty("{=LC_Settings_Name_FF9DE8BCF2}Skill XP Multiplier", 0f, 100f, RequireRestart = false, HintText = "{=LC_Settings_Desc_FF9DE8BCF2}The multiplier for all skill XP gained.", Order = 4)]
        [SettingPropertyGroup("{=LC_Settings_Group_0DB377921F}General", GroupOrder = 1)]
        public float SkillXPMultiplier { get; set; } = 1f;

        [SettingProperty("{=LC_Settings_Name_0700EF4DA7}Skill Smoothing Level", -40, 40, RequireRestart = false, HintText = "{=LC_Settings_Desc_0700EF4DA7}The level of exponential smoothing of Skill Level requirement.", Order = 5)]
        [SettingPropertyGroup("{=LC_Settings_Group_0DB377921F}General", GroupOrder = 1)]
        public int SkillSmoothingLevel { get; set; } = 0;

        #endregion

        #region Attributes & Focus Points

        [SettingProperty("{=LC_Settings_Name_567E405D89}Apply To", RequireRestart = false, HintText = "{=LC_Settings_Desc_567E405D89}Select which heroes attribute & focus points customizations should apply to.", Order = 1)]
        [SettingPropertyGroup("{=LC_Settings_Group_BABCC52183}Attributes & Focus Points", GroupOrder = 2)]
        public DropdownDefault<string> AttrFocusApplyTo { get; set; } = new DropdownDefault<string>(new string[] { "All Heroes", "Player", "Player & Companions" }, selectedIndex: 0);

        [SettingProperty("{=LC_Settings_Name_BAF9A66B74}Extra Learning Rate Per Attribute", -100f, 100f, RequireRestart = false, HintText = "{=LC_Settings_Desc_BAF9A66B74}This is added to base game value.", Order = 2)]
        [SettingPropertyGroup("{=LC_Settings_Group_BABCC52183}Attributes & Focus Points", GroupOrder = 2)]
        public float AttrExtraLearningRate { get; set; } = 0f;

        [SettingProperty("{=LC_Settings_Name_BF0CD17D57}Extra Learning Rate Per Focus Point", -100f, 100f, RequireRestart = false, HintText = "{=LC_Settings_Desc_BF0CD17D57}This is added to base game value.", Order = 3)]
        [SettingPropertyGroup("{=LC_Settings_Group_BABCC52183}Attributes & Focus Points", GroupOrder = 2)]
        public float FocusExtraLearningRate { get; set; } = 0f;

        #endregion

        #region Learning Limit (Applies to All Heroes)

        [SettingProperty("{=LC_Settings_Name_26DF5A5313}Extra Learning Limit Per Attribute", -100, 100, RequireRestart = false, HintText = "{=LC_Settings_Desc_26DF5A5313}This is added to base game value. Per point above 1, similar to the game.", Order = 1)]
        [SettingPropertyGroup("{=LC_Settings_Group_FE1E9D5DE8}Learning Limit (Applies to All Heroes)", GroupOrder = 3)]
        public int AttrExtraLearningLimit { get; set; } = 0;

        [SettingProperty("{=LC_Settings_Name_EF0584D028}Extra Learning Limit Per Focus Point", -100, 100, RequireRestart = false, HintText = "{=LC_Settings_Desc_EF0584D028}This is added to base game value.", Order = 2)]
        [SettingPropertyGroup("{=LC_Settings_Group_FE1E9D5DE8}Learning Limit (Applies to All Heroes)", GroupOrder = 3)]
        public int FocusExtraLearningLimit { get; set; } = 0;

        #endregion

        #region Skill Specific

        [SettingProperty("{=LC_Settings_Name_567E405D89}Apply To", RequireRestart = false, HintText = "{=LC_Settings_Desc_567E405D89}Select which heroes skill specific customizations should apply to.", Order = 1)]
        [SettingPropertyGroup("{=LC_Settings_Group_A4D2C2B871}Skill Specific", GroupOrder = 4)]
        public DropdownDefault<string> SkillApplyTo { get; set; } = new DropdownDefault<string>(new string[] { "All Heroes", "Player", "Player & Companions" }, selectedIndex: 0);

        [SettingProperty("{=LC_Settings_Name_47CF2A6D2A}One Handed XP Multiplier", 0f, 100f, RequireRestart = false, HintText = "{=LC_Settings_Desc_47CF2A6D2A}The multiplier for all One Handed skill XP gained.", Order = 2)]
        [SettingPropertyGroup("{=LC_Settings_Group_A4D2C2B871}Skill Specific", GroupOrder = 4)]
        public float OneHandedMultiplier { get; set; } = 1f;

        [SettingProperty("{=LC_Settings_Name_9C6F1469A2}Two Handed XP Multiplier", 0f, 100f, RequireRestart = false, HintText = "{=LC_Settings_Desc_9C6F1469A2}The multiplier for all Two Handed skill XP gained.", Order = 3)]
        [SettingPropertyGroup("{=LC_Settings_Group_A4D2C2B871}Skill Specific", GroupOrder = 4)]
        public float TwoHandedMultiplier { get; set; } = 1f;

        [SettingProperty("{=LC_Settings_Name_204AB46677}Polearm XP Multiplier", 0f, 100f, RequireRestart = false, HintText = "{=LC_Settings_Desc_204AB46677}The multiplier for all Polearm skill XP gained.", Order = 4)]
        [SettingPropertyGroup("{=LC_Settings_Group_A4D2C2B871}Skill Specific", GroupOrder = 4)]
        public float PolearmMultiplier { get; set; } = 1f;

        [SettingProperty("{=LC_Settings_Name_D53C239005}Bow XP Multiplier", 0f, 100f, RequireRestart = false, HintText = "{=LC_Settings_Desc_D53C239005}The multiplier for all Bow skill XP gained.", Order = 5)]
        [SettingPropertyGroup("{=LC_Settings_Group_A4D2C2B871}Skill Specific", GroupOrder = 4)]
        public float BowMultiplier { get; set; } = 1f;

        [SettingProperty("{=LC_Settings_Name_EDCAE8AB36}Crossbow XP Multiplier", 0f, 100f, RequireRestart = false, HintText = "{=LC_Settings_Desc_EDCAE8AB36}The multiplier for all Crossbow skill XP gained.", Order = 6)]
        [SettingPropertyGroup("{=LC_Settings_Group_A4D2C2B871}Skill Specific", GroupOrder = 4)]
        public float CrossbowMultiplier { get; set; } = 1f;

        [SettingProperty("{=LC_Settings_Name_58A5B85B28}Throwing XP Multiplier", 0f, 100f, RequireRestart = false, HintText = "{=LC_Settings_Desc_58A5B85B28}The multiplier for all Throwing skill XP gained.", Order = 7)]
        [SettingPropertyGroup("{=LC_Settings_Group_A4D2C2B871}Skill Specific", GroupOrder = 4)]
        public float ThrowingMultiplier { get; set; } = 1f;

        [SettingProperty("{=LC_Settings_Name_EF9AECE92E}Riding XP Multiplier", 0f, 100f, RequireRestart = false, HintText = "{=LC_Settings_Desc_EF9AECE92E}The multiplier for all Riding skill XP gained.", Order = 8)]
        [SettingPropertyGroup("{=LC_Settings_Group_A4D2C2B871}Skill Specific", GroupOrder = 4)]
        public float RidingMultiplier { get; set; } = 1f;

        [SettingProperty("{=LC_Settings_Name_B43BFA5644}Athletics XP Multiplier", 0f, 100f, RequireRestart = false, HintText = "{=LC_Settings_Desc_B43BFA5644}The multiplier for all Athletics skill XP gained.", Order = 9)]
        [SettingPropertyGroup("{=LC_Settings_Group_A4D2C2B871}Skill Specific", GroupOrder = 4)]
        public float AthleticsMultiplier { get; set; } = 1f;

        [SettingProperty("{=LC_Settings_Name_65C93EC53A}Smithing XP Multiplier", 0f, 100f, RequireRestart = false, HintText = "{=LC_Settings_Desc_65C93EC53A}The multiplier for all Smithing skill XP gained.", Order = 10)]
        [SettingPropertyGroup("{=LC_Settings_Group_A4D2C2B871}Skill Specific", GroupOrder = 4)]
        public float SmithingMultiplier { get; set; } = 1f;

        [SettingProperty("{=LC_Settings_Name_7C01F561FC}Scouting XP Multiplier", 0f, 100f, RequireRestart = false, HintText = "{=LC_Settings_Desc_7C01F561FC}The multiplier for all Scouting skill XP gained.", Order = 11)]
        [SettingPropertyGroup("{=LC_Settings_Group_A4D2C2B871}Skill Specific", GroupOrder = 4)]
        public float ScoutingMultiplier { get; set; } = 1f;

        [SettingProperty("{=LC_Settings_Name_D7341675DF}Tactics XP Multiplier", 0f, 100f, RequireRestart = false, HintText = "{=LC_Settings_Desc_D7341675DF}The multiplier for all Tactics skill XP gained.", Order = 12)]
        [SettingPropertyGroup("{=LC_Settings_Group_A4D2C2B871}Skill Specific", GroupOrder = 4)]
        public float TacticsMultiplier { get; set; } = 1f;

        [SettingProperty("{=LC_Settings_Name_B2264D9242}Roguery XP Multiplier", 0f, 100f, RequireRestart = false, HintText = "{=LC_Settings_Desc_B2264D9242}The multiplier for all Roguery skill XP gained.", Order = 13)]
        [SettingPropertyGroup("{=LC_Settings_Group_A4D2C2B871}Skill Specific", GroupOrder = 4)]
        public float RogueryMultiplier { get; set; } = 1f;

        [SettingProperty("{=LC_Settings_Name_A5B8DB588D}Charm XP Multiplier", 0f, 100f, RequireRestart = false, HintText = "{=LC_Settings_Desc_A5B8DB588D}The multiplier for all Charm skill XP gained.", Order = 14)]
        [SettingPropertyGroup("{=LC_Settings_Group_A4D2C2B871}Skill Specific", GroupOrder = 4)]
        public float CharmMultiplier { get; set; } = 1f;

        [SettingProperty("{=LC_Settings_Name_7A66BFE971}Leadership XP Multiplier", 0f, 100f, RequireRestart = false, HintText = "{=LC_Settings_Desc_7A66BFE971}The multiplier for all Leadership skill XP gained.", Order = 15)]
        [SettingPropertyGroup("{=LC_Settings_Group_A4D2C2B871}Skill Specific", GroupOrder = 4)]
        public float LeadershipMultiplier { get; set; } = 1f;

        [SettingProperty("{=LC_Settings_Name_C641645018}Trade XP Multiplier", 0f, 100f, RequireRestart = false, HintText = "{=LC_Settings_Desc_C641645018}The multiplier for all Trade skill XP gained.", Order = 16)]
        [SettingPropertyGroup("{=LC_Settings_Group_A4D2C2B871}Skill Specific", GroupOrder = 4)]
        public float TradeMultiplier { get; set; } = 1f;

        [SettingProperty("{=LC_Settings_Name_3C1619BE9F}Steward XP Multiplier", 0f, 100f, RequireRestart = false, HintText = "{=LC_Settings_Desc_3C1619BE9F}The multiplier for all Steward skill XP gained.", Order = 17)]
        [SettingPropertyGroup("{=LC_Settings_Group_A4D2C2B871}Skill Specific", GroupOrder = 4)]
        public float StewardMultiplier { get; set; } = 1f;

        [SettingProperty("{=LC_Settings_Name_0C2BE0D112}Medicine XP Multiplier", 0f, 100f, RequireRestart = false, HintText = "{=LC_Settings_Desc_0C2BE0D112}The multiplier for all Medicine skill XP gained.", Order = 18)]
        [SettingPropertyGroup("{=LC_Settings_Group_A4D2C2B871}Skill Specific", GroupOrder = 4)]
        public float MedicineMultiplier { get; set; } = 1f;

        [SettingProperty("{=LC_Settings_Name_D13C1A1B68}Engineering XP Multiplier", 0f, 100f, RequireRestart = false, HintText = "{=LC_Settings_Desc_D13C1A1B68}The multiplier for all Engineering skill XP gained.", Order = 19)]
        [SettingPropertyGroup("{=LC_Settings_Group_A4D2C2B871}Skill Specific", GroupOrder = 4)]
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

        public static void CalculateLearningLimitPostfix(ref ExplainedNumber __result, int attributeValue, int focusValue, TextObject attributeName, bool includeDescriptions = false)
        {
            var attrExtraLearningLimit = MySettings.Instance?.AttrExtraLearningLimit ?? 0;
            var focusExtraLearningLimit = MySettings.Instance?.FocusExtraLearningLimit ?? 0;
            __result.Add((attributeValue - 1) * attrExtraLearningLimit, new TextObject("{=LC_Attr_Learning_Limit_Add}(Mod) Attribute", null), null);
            __result.Add(focusValue * focusExtraLearningLimit, new TextObject("{=LC_Focus_Learning_Limit_Add}(Mod) Focus", null), null);
        }

        public static void CalculateLearningRatePostfix(ref float __result, Hero hero, SkillObject skill)
        {
            var skillValue = hero.GetSkillValue(skill);
            var attributeValue = hero.GetAttributeValue(skill.CharacterAttribute);
            var focusValue = hero.HeroDeveloper.GetFocus(skill);
            GetAttrFocusExtraLearningRate(hero, out float attrExtraLearningRate, out float focusExtraLearningRate, attributeValue, focusValue);
            __result += attrExtraLearningRate + focusExtraLearningRate;
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
            GetAttrFocusExtraLearningRate(hero, out float attrExtraLearningRate, out float focusExtraLearningRate, attributeValue, focusValue);
            learningRate.AddFactor(attrExtraLearningRate / learningRate.BaseNumber, new TextObject("{=LC_Attr_Learning_Rate_Factor}(Mod) Attribute", null));
            learningRate.AddFactor(focusExtraLearningRate / learningRate.BaseNumber, new TextObject("{=LC_Focus_Learning_Rate_Factor}(Mod) Focus", null));
            var multiplier = 0f;
            if (learningRate.ResultNumber > 0f)
            {
                multiplier = (learningRate.ResultNumber * (CalculateSkillXpMultiplier(hero, skill, skillValue) - 1f)) / learningRate.BaseNumber;
            }

            learningRate.AddFactor(multiplier, new TextObject("{=LC_Learning_Rate_Factor}(Mod) Levelling Customizer", null));
            return learningRate;
        }

        public static void GetAttrFocusExtraLearningRate(Hero hero, out float attrExtraLearningRate, out float focusExtraLearningRate, int attributeValue, int focusValue)
        {
            attrExtraLearningRate = 0f;
            focusExtraLearningRate = 0f;
            var applyTo = MySettings.Instance?.AttrFocusApplyTo?.SelectedIndex ?? 0;
            if ((applyTo == 1 && !hero.IsHumanPlayerCharacter) || (applyTo == 2 && !hero.IsHumanPlayerCharacter && !hero.IsPlayerCompanion))
            {
                return;
            }

            attrExtraLearningRate = (MySettings.Instance?.AttrExtraLearningRate ?? 0f) * attributeValue;
            focusExtraLearningRate = (MySettings.Instance?.FocusExtraLearningRate ?? 0f) * focusValue;
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