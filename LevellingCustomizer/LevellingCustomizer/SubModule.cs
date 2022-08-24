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
            harmony.Patch(typeof(DefaultCharacterDevelopmentModel).GetMethod("CalculateLearningRate", new Type[] {typeof(Hero), typeof(SkillObject)}),
                new HarmonyMethod(typeof(SkillXpPatch).GetMethod(nameof(SkillXpPatch.CalculateLearningRatePrefix))));
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
        public override string DisplayName => new TextObject("{=7DDAA4BD82}Levelling Customizer").ToString();
        public override string FolderName => "LevellingCustomizer";
        public override string FormatType => "json";

        #region String Definitions
        private const string StrGeneral = "{=0DB377921F}General";
        private const string StrGeneralApplyTo = "{=567E405D89}Apply To";
        private const string StrGeneralApplyToDesc = "{=A31DD4A817}Select which heroes general customizations should apply to.";
        private const string StrLevellingXPMultiplier = "{=1122D38AAE}Levelling XP Multiplier";
        private const string StrLevellingXPMultiplierDesc = "{=8AACB0EAB4}The multiplier for levelling XP gained.";
        private const string StrLevellingSmoothingLevel = "{=BC96BC6657}Levelling Smoothing Level";
        private const string StrLevellingSmoothingLevelDesc = "{=D84CC6EB2F}The level of exponential smoothing of Character Level requirement.";
        private const string StrSkillXPMultiplier = "{=FF9DE8BCF2}Skill XP Multiplier";
        private const string StrSkillXPMultiplierDesc = "{=2CEFC4D24B}The multiplier for all skill XP gained.";
        private const string StrSkillSmoothingLevel = "{=0700EF4DA7}Skill Smoothing Level";
        private const string StrSkillSmoothingLevelDesc = "{=71B6CD41BC}The level of exponential smoothing of Skill Level requirement.";

        private const string StrAttrAndFocus = "{=BABCC52183}Attributes & Focus Points";
        private const string StrAttrFocusApplyTo = "{=567E405D89}Apply To";
        private const string StrAttrFocusApplyToDesc = "{=CF3766BA2B}Select which heroes attribute & focus points customizations should apply to.";
        private const string StrAttrExtraLearningRate = "{=BAF9A66B74}Extra Learning Rate Per Attribute";
        private const string StrAttrExtraLearningRateDesc = "{=A546022437}This is added to base game value.";
        private const string StrFocusExtraLearningRate = "{=BF0CD17D57}Extra Learning Rate Per Focus Point";
        private const string StrFocusExtraLearningRateDesc = "{=A546022437}This is added to base game value.";
        private const string StrAttrMinLearningRate = "{=2365E1286F}Min Learning Rate Per Attribute";
        private const string StrAttrMinLearningRateDesc = "{=FC30F87396}Sets minimum learning rate based on attribute. This is added to base game value.";
        private const string StrFocusMinLearningRate = "{=BCC3C27303}Min Learning Rate Per Focus Point";
        private const string StrFocusMinLearningRateDesc = "{=F55354B5E5}Sets minimum learning rate based on focus points. This is added to base game value.";

        private const string StrLearningLimit = "{=FE1E9D5DE8}Learning Limit (Applies to All Heroes)";
        private const string StrAttrExtraLearningLimit = "{=26DF5A5313}Extra Learning Limit Per Attribute";
        private const string StrAttrExtraLearningLimitDesc = "{=640D18E87E}This is added to base game value. Per point above 1, similar to the game.";
        private const string StrFocusExtraLearningLimit = "{=EF0584D028}Extra Learning Limit Per Focus Point";
        private const string StrFocusExtraLearningLimitDesc = "{=A546022437}This is added to base game value.";

        private const string StrSkillSpecific = "{=A4D2C2B871}Skill Specific";
        private const string StrSkillApplyTo = "{=567E405D89}Apply To";
        private const string StrSkillApplyToDesc = "{=3C62892519}Select which heroes skill specific customizations should apply to.";
        private const string StrOneHandedMultiplier = "{=47CF2A6D2A}One Handed XP Multiplier";
        private const string StrOneHandedMultiplierDesc = "{=AE98D5EF09}The multiplier for all One Handed skill XP gained.";
        private const string StrTwoHandedMultiplier = "{=9C6F1469A2}Two Handed XP Multiplier";
        private const string StrTwoHandedMultiplierDesc = "{=D8E16E7BBE}The multiplier for all Two Handed skill XP gained.";
        private const string StrPolearmMultiplier = "{=204AB46677}Polearm XP Multiplier";
        private const string StrPolearmMultiplierDesc = "{=A3B683B902}The multiplier for all Polearm skill XP gained.";
        private const string StrBowMultiplier = "{=D53C239005}Bow XP Multiplier";
        private const string StrBowMultiplierDesc = "{=21BED56921}The multiplier for all Bow skill XP gained.";
        private const string StrCrossbowMultiplier = "{=EDCAE8AB36}Crossbow XP Multiplier";
        private const string StrCrossbowMultiplierDesc = "{=C90A9ADB04}The multiplier for all Crossbow skill XP gained.";
        private const string StrThrowingMultiplier = "{=58A5B85B28}Throwing XP Multiplier";
        private const string StrThrowingMultiplierDesc = "{=F4B946B544}The multiplier for all Throwing skill XP gained.";
        private const string StrRidingMultiplier = "{=EF9AECE92E}Riding XP Multiplier";
        private const string StrRidingMultiplierDesc = "{=D9FB9818B5}The multiplier for all Riding skill XP gained.";
        private const string StrAthleticsMultiplier = "{=B43BFA5644}Athletics XP Multiplier";
        private const string StrAthleticsMultiplierDesc = "{=3E3E4483E8}The multiplier for all Athletics skill XP gained.";
        private const string StrSmithingMultiplier = "{=65C93EC53A}Smithing XP Multiplier";
        private const string StrSmithingMultiplierDesc = "{=0ADC181017}The multiplier for all Smithing skill XP gained.";
        private const string StrScoutingMultiplier = "{=7C01F561FC}Scouting XP Multiplier";
        private const string StrScoutingMultiplierDesc = "{=561FFDA4F3}The multiplier for all Scouting skill XP gained.";
        private const string StrTacticsMultiplier = "{=D7341675DF}Tactics XP Multiplier";
        private const string StrTacticsMultiplierDesc = "{=11E81C20D4}The multiplier for all Tactics skill XP gained.";
        private const string StrRogueryMultiplier = "{=B2264D9242}Roguery XP Multiplier";
        private const string StrRogueryMultiplierDesc = "{=7FBEAD18CC}The multiplier for all Roguery skill XP gained.";
        private const string StrCharmMultiplier = "{=A5B8DB588D}Charm XP Multiplier";
        private const string StrCharmMultiplierDesc = "{=98F9BD8663}The multiplier for all Charm skill XP gained.";
        private const string StrLeadershipMultiplier = "{=7A66BFE971}Leadership XP Multiplier";
        private const string StrLeadershipMultiplierDesc = "{=29844435E4}The multiplier for all Leadership skill XP gained.";
        private const string StrTradeMultiplier = "{=C641645018}Trade XP Multiplier";
        private const string StrTradeMultiplierDesc = "{=8994140CB9}The multiplier for all Trade skill XP gained.";
        private const string StrStewardMultiplier = "{=3C1619BE9F}Steward XP Multiplier";
        private const string StrStewardMultiplierDesc = "{=E4E55C8DD6}The multiplier for all Steward skill XP gained.";
        private const string StrMedicineMultiplier = "{=0C2BE0D112}Medicine XP Multiplier";
        private const string StrMedicineMultiplierDesc = "{=9D7F88794B}The multiplier for all Medicine skill XP gained.";
        private const string StrEngineeringMultiplier = "{=D13C1A1B68}Engineering XP Multiplier";
        private const string StrEngineeringMultiplierDesc = "{=FD0D3A17CB}The multiplier for all Engineering skill XP gained.";

        private const string StrOther = "{=6311AE17C1}Other";
        private const string StrMaxSkillLevel = "{=2A289A7DBA}Max Skill Level";
        private const string StrMaxSkillLevelDesc = "{=401BA742E7}The maximum level skills can reach.";
        #endregion

        #region General

        [SettingProperty(StrGeneralApplyTo, RequireRestart = false, HintText = StrGeneralApplyToDesc, Order = 1)]
        [SettingPropertyGroup(StrGeneral, GroupOrder = 1)]
        public DropdownDefault<string> GeneralApplyTo { get; set; } = new DropdownDefault<string>(new string[] { "{=5F2B080E2E}All Heroes", "{=636DA1D35E}Player", "{=0380FD801C}Player & Player Clan", "{=65C08D4C2A}Player Clan", "{=5016142882}Player Clan Except Companions", "{=3FE8BC9F22}Only Companions" }, selectedIndex: 0);

        [SettingProperty(StrLevellingXPMultiplier, 0f, 100f, RequireRestart = false, HintText = StrLevellingXPMultiplierDesc, Order = 2)]
        [SettingPropertyGroup(StrGeneral, GroupOrder = 1)]
        public float LevellingXPMultiplier { get; set; } = 1f;

        [SettingProperty(StrLevellingSmoothingLevel, -40, 40, RequireRestart = false, HintText = StrLevellingSmoothingLevelDesc, Order = 3)]
        [SettingPropertyGroup(StrGeneral, GroupOrder = 1)]
        public int LevellingSmoothingLevel { get; set; } = 0;

        [SettingProperty(StrSkillXPMultiplier, 0f, 100f, RequireRestart = false, HintText = StrSkillXPMultiplierDesc, Order = 4)]
        [SettingPropertyGroup(StrGeneral, GroupOrder = 1)]
        public float SkillXPMultiplier { get; set; } = 1f;

        [SettingProperty(StrSkillSmoothingLevel, -40, 40, RequireRestart = false, HintText = StrSkillSmoothingLevelDesc, Order = 5)]
        [SettingPropertyGroup(StrGeneral, GroupOrder = 1)]
        public int SkillSmoothingLevel { get; set; } = 0;

        #endregion

        #region Attributes & Focus Points

        [SettingProperty(StrAttrFocusApplyTo, RequireRestart = false, HintText = StrAttrFocusApplyToDesc, Order = 1)]
        [SettingPropertyGroup(StrAttrAndFocus, GroupOrder = 2)]
        public DropdownDefault<string> AttrFocusApplyTo { get; set; } = new DropdownDefault<string>(new string[] { "{=5F2B080E2E}All Heroes", "{=636DA1D35E}Player", "{=0380FD801C}Player & Player Clan", "{=65C08D4C2A}Player Clan", "{=5016142882}Player Clan Except Companions", "{=3FE8BC9F22}Only Companions" }, selectedIndex: 0);

        [SettingProperty(StrAttrExtraLearningRate, -100f, 100f, RequireRestart = false, HintText = StrAttrExtraLearningRateDesc, Order = 2)]
        [SettingPropertyGroup(StrAttrAndFocus, GroupOrder = 2)]
        public float AttrExtraLearningRate { get; set; } = 0f;

        [SettingProperty(StrFocusExtraLearningRate, -100f, 100f, RequireRestart = false, HintText = StrFocusExtraLearningRateDesc, Order = 3)]
        [SettingPropertyGroup(StrAttrAndFocus, GroupOrder = 2)]
        public float FocusExtraLearningRate { get; set; } = 0f;

        [SettingProperty(StrAttrMinLearningRate, -100f, 100f, RequireRestart = false, HintText = StrAttrMinLearningRateDesc, Order = 4)]
        [SettingPropertyGroup(StrAttrAndFocus, GroupOrder = 2)]
        public float AttrMinLearningRate { get; set; } = 0f;

        [SettingProperty(StrFocusMinLearningRate, -100f, 100f, RequireRestart = false, HintText = StrFocusMinLearningRateDesc, Order = 5)]
        [SettingPropertyGroup(StrAttrAndFocus, GroupOrder = 2)]
        public float FocusMinLearningRate { get; set; } = 0f;

        #endregion

        #region Learning Limit (Applies to All Heroes)

        [SettingProperty(StrAttrExtraLearningLimit, -100, 100, RequireRestart = false, HintText = StrAttrExtraLearningLimitDesc, Order = 1)]
        [SettingPropertyGroup(StrLearningLimit, GroupOrder = 3)]
        public int AttrExtraLearningLimit { get; set; } = 0;

        [SettingProperty(StrFocusExtraLearningLimit, -100, 100, RequireRestart = false, HintText = StrFocusExtraLearningLimitDesc, Order = 2)]
        [SettingPropertyGroup(StrLearningLimit, GroupOrder = 3)]
        public int FocusExtraLearningLimit { get; set; } = 0;

        #endregion

        #region Skill Specific

        [SettingProperty(StrSkillApplyTo, RequireRestart = false, HintText = StrSkillApplyToDesc, Order = 1)]
        [SettingPropertyGroup(StrSkillSpecific, GroupOrder = 4)]
        public DropdownDefault<string> SkillApplyTo { get; set; } = new DropdownDefault<string>(new string[] { "{=5F2B080E2E}All Heroes", "{=636DA1D35E}Player", "{=0380FD801C}Player & Player Clan", "{=65C08D4C2A}Player Clan", "{=5016142882}Player Clan Except Companions", "{=3FE8BC9F22}Only Companions" }, selectedIndex: 0);

        [SettingProperty(StrOneHandedMultiplier, 0f, 100f, RequireRestart = false, HintText = StrOneHandedMultiplierDesc, Order = 2)]
        [SettingPropertyGroup(StrSkillSpecific, GroupOrder = 4)]
        public float OneHandedMultiplier { get; set; } = 1f;

        [SettingProperty(StrTwoHandedMultiplier, 0f, 100f, RequireRestart = false, HintText = StrTwoHandedMultiplierDesc, Order = 3)]
        [SettingPropertyGroup(StrSkillSpecific, GroupOrder = 4)]
        public float TwoHandedMultiplier { get; set; } = 1f;

        [SettingProperty(StrPolearmMultiplier, 0f, 100f, RequireRestart = false, HintText = StrPolearmMultiplierDesc, Order = 4)]
        [SettingPropertyGroup(StrSkillSpecific, GroupOrder = 4)]
        public float PolearmMultiplier { get; set; } = 1f;

        [SettingProperty(StrBowMultiplier, 0f, 100f, RequireRestart = false, HintText = StrBowMultiplierDesc, Order = 5)]
        [SettingPropertyGroup(StrSkillSpecific, GroupOrder = 4)]
        public float BowMultiplier { get; set; } = 1f;

        [SettingProperty(StrCrossbowMultiplier, 0f, 100f, RequireRestart = false, HintText = StrCrossbowMultiplierDesc, Order = 6)]
        [SettingPropertyGroup(StrSkillSpecific, GroupOrder = 4)]
        public float CrossbowMultiplier { get; set; } = 1f;

        [SettingProperty(StrThrowingMultiplier, 0f, 100f, RequireRestart = false, HintText = StrThrowingMultiplierDesc, Order = 7)]
        [SettingPropertyGroup(StrSkillSpecific, GroupOrder = 4)]
        public float ThrowingMultiplier { get; set; } = 1f;

        [SettingProperty(StrRidingMultiplier, 0f, 100f, RequireRestart = false, HintText = StrRidingMultiplierDesc, Order = 8)]
        [SettingPropertyGroup(StrSkillSpecific, GroupOrder = 4)]
        public float RidingMultiplier { get; set; } = 1f;

        [SettingProperty(StrAthleticsMultiplier, 0f, 100f, RequireRestart = false, HintText = StrAthleticsMultiplierDesc, Order = 9)]
        [SettingPropertyGroup(StrSkillSpecific, GroupOrder = 4)]
        public float AthleticsMultiplier { get; set; } = 1f;

        [SettingProperty(StrSmithingMultiplier, 0f, 100f, RequireRestart = false, HintText = StrSmithingMultiplierDesc, Order = 10)]
        [SettingPropertyGroup(StrSkillSpecific, GroupOrder = 4)]
        public float SmithingMultiplier { get; set; } = 1f;

        [SettingProperty(StrScoutingMultiplier, 0f, 100f, RequireRestart = false, HintText = StrScoutingMultiplierDesc, Order = 11)]
        [SettingPropertyGroup(StrSkillSpecific, GroupOrder = 4)]
        public float ScoutingMultiplier { get; set; } = 1f;

        [SettingProperty(StrTacticsMultiplier, 0f, 100f, RequireRestart = false, HintText = StrTacticsMultiplierDesc, Order = 12)]
        [SettingPropertyGroup(StrSkillSpecific, GroupOrder = 4)]
        public float TacticsMultiplier { get; set; } = 1f;

        [SettingProperty(StrRogueryMultiplier, 0f, 100f, RequireRestart = false, HintText = StrRogueryMultiplierDesc, Order = 13)]
        [SettingPropertyGroup(StrSkillSpecific, GroupOrder = 4)]
        public float RogueryMultiplier { get; set; } = 1f;

        [SettingProperty(StrCharmMultiplier, 0f, 100f, RequireRestart = false, HintText = StrCharmMultiplierDesc, Order = 14)]
        [SettingPropertyGroup(StrSkillSpecific, GroupOrder = 4)]
        public float CharmMultiplier { get; set; } = 1f;

        [SettingProperty(StrLeadershipMultiplier, 0f, 100f, RequireRestart = false, HintText = StrLeadershipMultiplierDesc, Order = 15)]
        [SettingPropertyGroup(StrSkillSpecific, GroupOrder = 4)]
        public float LeadershipMultiplier { get; set; } = 1f;

        [SettingProperty(StrTradeMultiplier, 0f, 100f, RequireRestart = false, HintText = StrTradeMultiplierDesc, Order = 16)]
        [SettingPropertyGroup(StrSkillSpecific, GroupOrder = 4)]
        public float TradeMultiplier { get; set; } = 1f;

        [SettingProperty(StrStewardMultiplier, 0f, 100f, RequireRestart = false, HintText = StrStewardMultiplierDesc, Order = 17)]
        [SettingPropertyGroup(StrSkillSpecific, GroupOrder = 4)]
        public float StewardMultiplier { get; set; } = 1f;

        [SettingProperty(StrMedicineMultiplier, 0f, 100f, RequireRestart = false, HintText = StrMedicineMultiplierDesc, Order = 18)]
        [SettingPropertyGroup(StrSkillSpecific, GroupOrder = 4)]
        public float MedicineMultiplier { get; set; } = 1f;

        [SettingProperty(StrEngineeringMultiplier, 0f, 100f, RequireRestart = false, HintText = StrEngineeringMultiplierDesc, Order = 19)]
        [SettingPropertyGroup(StrSkillSpecific, GroupOrder = 4)]
        public float EngineeringMultiplier { get; set; } = 1f;

        #endregion

        #region Other

        [SettingProperty(StrMaxSkillLevel, 100, 1000, RequireRestart = false, HintText = StrMaxSkillLevelDesc, Order = 1)]
        [SettingPropertyGroup(StrOther, GroupOrder = 5)]
        public int MaxSkillLevel { get; set; } = 400;

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
        private static readonly TextObject _levellingRateStr = new TextObject("{=F40D851137}(Mod) Levelling Rate", null);

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
            var applyToValue = MySettings.Instance?.GeneralApplyTo?.SelectedIndex ?? 0;
            if (!Helper.ApplyTo(applyToValue, hero))
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
            __result.Add((attributeValue - 1) * attrExtraLearningLimit, new TextObject("{=78726ECC36}(Mod) Attribute", null), null);
            __result.Add(focusValue * focusExtraLearningLimit, new TextObject("{=862FD3C79C}(Mod) Focus", null), null);
        }

        public static bool CalculateLearningRatePrefix(ref float __result, Hero hero, SkillObject skill)
        {
            var skillValue = hero.GetSkillValue(skill);
            var attributeValue = hero.GetAttributeValue(skill.CharacterAttribute);
            var focusValue = hero.HeroDeveloper.GetFocus(skill);
            var learningRate = CalculateLearningRate(hero, skill, attributeValue, focusValue, skillValue, hero.Level, false);
            __result = learningRate.ResultNumber;
            return false;
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
            var maxSkillLevel = MySettings.Instance?.MaxSkillLevel ?? 400;
            if (skillValue >= maxSkillLevel)
            {
                var result = new ExplainedNumber(1.25f, includeDescriptions, null);
                result.AddFactor(-1f, new TextObject("{=BF5B4203BB}(Mod) Max Skill Level Reached", null));
                return result;
            }

            var attributeName = skill.CharacterAttribute.Name;
            var learningRate = Campaign.Current.Models.CharacterDevelopmentModel.CalculateLearningRate(attributeValue, focusValue, skillValue, characterLevel, attributeName, includeDescriptions);
            AddAttrFocusExtraLearningRate(hero, ref learningRate, attributeValue, focusValue);
            var multiplier = 0f;
            if (learningRate.ResultNumber > 0f)
            {
                multiplier = (learningRate.ResultNumber * (CalculateSkillXpMultiplier(hero, skill, skillValue) - 1f)) / learningRate.BaseNumber;
            }

            learningRate.AddFactor(multiplier, new TextObject("{=7BBA0FCCA1}(Mod) Levelling Customizer", null));
            return learningRate;
        }

        public static void AddAttrFocusExtraLearningRate(Hero hero, ref ExplainedNumber learningRate, int attributeValue, int focusValue)
        {
            var applyToValue = MySettings.Instance?.AttrFocusApplyTo?.SelectedIndex ?? 0;
            if (!Helper.ApplyTo(applyToValue, hero))
            {
                return;
            }

            var attrExtraLearningRate = (MySettings.Instance?.AttrExtraLearningRate ?? 0f) * attributeValue;
            var focusExtraLearningRate = (MySettings.Instance?.FocusExtraLearningRate ?? 0f) * focusValue;
            learningRate.AddFactor(attrExtraLearningRate / learningRate.BaseNumber, new TextObject("{=78726ECC36}(Mod) Attribute", null));
            learningRate.AddFactor(focusExtraLearningRate / learningRate.BaseNumber, new TextObject("{=862FD3C79C}(Mod) Focus", null));

            var attrMinLearningRate = (MySettings.Instance?.AttrMinLearningRate ?? 0f) * attributeValue;
            var focusMinLearningRate = (MySettings.Instance?.FocusMinLearningRate ?? 0f) * focusValue;
            learningRate.LimitMin(learningRate.LimitMinValue + attrMinLearningRate + focusMinLearningRate);
        }

        public static float CalculateSkillXpMultiplier(Hero hero, SkillObject skill, int skillValue)
        {
            var applyToValue = MySettings.Instance?.SkillApplyTo?.SelectedIndex ?? 0;
            if (!Helper.ApplyTo(applyToValue, hero))
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

    public static class Helper
    {
        public static bool ApplyTo(int applyToValue, Hero hero)
        {
            if (hero == null)
            {
                return false;
            }

            if (applyToValue < 0 || applyToValue > 5)
            {
                return false;
            }

            if (applyToValue == 0)
            {
                return true;
            }

            if (hero.IsHumanPlayerCharacter)
            {
                return applyToValue == 1 || applyToValue == 2;
            }

            if (hero.IsPlayerCompanion)
            {
                return applyToValue == 2 || applyToValue == 3 || applyToValue == 5;
            }

            if (hero.Clan == Clan.PlayerClan)
            {
                return applyToValue == 2 || applyToValue == 3 || applyToValue == 4;
            }

            return false;
        }
    }
}