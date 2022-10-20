using HarmonyLib;
using MCM.Abstractions.Attributes;
using MCM.Abstractions.Attributes.v1;
using MCM.Abstractions.Settings.Base.Global;
using System;
using System.Collections.Generic;
using System.Reflection;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Inventory;
using TaleWorlds.CampaignSystem.Overlay;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Workshops;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.SaveSystem;
using static TaleWorlds.CampaignSystem.Settlements.Workshops.WorkshopType;

namespace WorkshopsAdvanced
{
    public class SubModule : MBSubModuleBase
    {
        protected override void OnSubModuleLoad()
        {
            base.OnSubModuleLoad();

            Harmony harmony = new Harmony("WorkshopsAdvanced");
            //Harmony.DEBUG = true;

            harmony.Patch(typeof(Workshop).GetMethod("get_Expense", BindingFlags.Public | BindingFlags.Instance), null,
                new HarmonyMethod(typeof(WorkshopBehaviourPatch).GetMethod(nameof(WorkshopBehaviourPatch.ExpensePostfix))));
            harmony.Patch(typeof(Workshop).GetMethod("get_IsRunning", BindingFlags.Public | BindingFlags.Instance), null,
                new HarmonyMethod(typeof(WorkshopBehaviourPatch).GetMethod(nameof(WorkshopBehaviourPatch.IsRunningPostfix))));
            harmony.Patch(typeof(DefaultWorkshopModel).GetMethod("GetMaxWorkshopCountForTier", BindingFlags.Public | BindingFlags.Instance), null,
                new HarmonyMethod(typeof(WorkshopBehaviourPatch).GetMethod(nameof(WorkshopBehaviourPatch.GetMaxWorkshopCountForTierPostfix))));
            harmony.Patch(typeof(DefaultWorkshopModel).GetMethod("GetBuyingCostForPlayer", BindingFlags.Public | BindingFlags.Instance), null,
                new HarmonyMethod(typeof(WorkshopBehaviourPatch).GetMethod(nameof(WorkshopBehaviourPatch.GetBuyingCostForPlayerPostfix))));
            harmony.Patch(typeof(DefaultWorkshopModel).GetMethod("GetSellingCost", BindingFlags.Public | BindingFlags.Instance), null,
                new HarmonyMethod(typeof(WorkshopBehaviourPatch).GetMethod(nameof(WorkshopBehaviourPatch.GetSellingCostPostfix))));
            harmony.Patch(typeof(Production).GetMethod("get_ConversionSpeed", BindingFlags.Public | BindingFlags.Instance), null,
                new HarmonyMethod(typeof(WorkshopBehaviourPatch).GetMethod(nameof(WorkshopBehaviourPatch.ConversionSpeedPostfix))));

            harmony.Patch(typeof(DefaultClanFinanceModel).GetMethod("CalculateClanExpensesInternal", BindingFlags.Public | BindingFlags.Instance), null,
                new HarmonyMethod(typeof(WorkshopBehaviourPatch).GetMethod(nameof(WorkshopBehaviourPatch.CalculateClanExpensesInternalPostfix))));

            harmony.Patch(typeof(WorkshopsCampaignBehavior).GetMethod("RunTownWorkshop", BindingFlags.NonPublic | BindingFlags.Instance),
                new HarmonyMethod(typeof(WorkshopBehaviourPatch).GetMethod(nameof(WorkshopBehaviourPatch.RunTownWorkshopPrefix))));
            //harmony.Patch(typeof(WorkshopsCampaignBehavior).GetMethod("DoProduction", BindingFlags.NonPublic | BindingFlags.Instance),
            //    new HarmonyMethod(typeof(WorkshopBehaviourPatch).GetMethod(nameof(WorkshopBehaviourPatch.DoProductionPrefix))));
            harmony.Patch(typeof(WorkshopsCampaignBehavior).GetMethod("DetermineTownHasSufficientInputs", BindingFlags.NonPublic | BindingFlags.Static), null,
                new HarmonyMethod(typeof(WorkshopBehaviourPatch).GetMethod(nameof(WorkshopBehaviourPatch.DetermineTownHasSufficientInputsPostfix))));
            harmony.Patch(typeof(WorkshopsCampaignBehavior).GetMethod("ProduceOutput", BindingFlags.NonPublic | BindingFlags.Static),
                new HarmonyMethod(typeof(WorkshopBehaviourPatch).GetMethod(nameof(WorkshopBehaviourPatch.ProduceOutputPrefix))));
            harmony.Patch(typeof(WorkshopsCampaignBehavior).GetMethod("ConsumeInput", BindingFlags.NonPublic | BindingFlags.Static),
                new HarmonyMethod(typeof(WorkshopBehaviourPatch).GetMethod(nameof(WorkshopBehaviourPatch.ConsumeInputPrefix))));
            harmony.Patch(typeof(WorkshopsCampaignBehavior).GetMethod("HandleDailyExpense", BindingFlags.NonPublic | BindingFlags.Instance),
                new HarmonyMethod(typeof(WorkshopBehaviourPatch).GetMethod(nameof(WorkshopBehaviourPatch.HandleDailyExpensePrefix))));

            harmony.Patch(typeof(CaravansCampaignBehavior).GetMethod("OnSettlementEntered", BindingFlags.Public | BindingFlags.Instance),
                new HarmonyMethod(typeof(WorkshopBehaviourPatch).GetMethod(nameof(WorkshopBehaviourPatch.OnSettlementEnteredPrefix))));

            harmony.Patch(typeof(DefaultClanFinanceModel).GetMethod("CalculateHeroIncomeFromWorkshops", BindingFlags.NonPublic | BindingFlags.Instance),
                new HarmonyMethod(typeof(WorkshopBehaviourPatch).GetMethod(nameof(WorkshopBehaviourPatch.CalculateHeroIncomeFromWorkshopsPrefix))));
            harmony.Patch(typeof(DefaultClanFinanceModel).GetMethod("CalculateHeroIncomeFromWorkshops", BindingFlags.NonPublic | BindingFlags.Instance), null,
                new HarmonyMethod(typeof(WorkshopBehaviourPatch).GetMethod(nameof(WorkshopBehaviourPatch.CalculateHeroIncomeFromWorkshopsPostfix))));

            harmony.Patch(typeof(ChangeOwnerOfWorkshopAction).GetMethod("ApplyByWarDeclaration", BindingFlags.Public | BindingFlags.Static),
                new HarmonyMethod(typeof(WorkshopBehaviourPatch).GetMethod(nameof(WorkshopBehaviourPatch.ApplyByWarDeclarationPrefix))));

#if DEBUG
            harmony.Patch(typeof(DefaultDisguiseDetectionModel).GetMethod("CalculateDisguiseDetectionProbability", BindingFlags.Public | BindingFlags.Instance), null,
                new HarmonyMethod(typeof(DebugPatch).GetMethod(nameof(DebugPatch.CalculateDisguiseDetectionProbabilityPostfix))));
            harmony.Patch(typeof(MobileParty).GetMethod("CanAttack", BindingFlags.NonPublic | BindingFlags.Instance), null,
                new HarmonyMethod(typeof(DebugPatch).GetMethod(nameof(DebugPatch.CanAttackPostfix))));
#endif
        }

        protected override void OnSubModuleUnloaded()
        {
            base.OnSubModuleUnloaded();

        }

        protected override void OnBeforeInitialModuleScreenSetAsRoot()
        {
            base.OnBeforeInitialModuleScreenSetAsRoot();

        }

        protected override void OnGameStart(Game game, IGameStarter gameStarterObject)
        {
            if (game.GameType is Campaign)
            {
                var campaignBehaviour = new WorkshopsAdvancedCampaignBehaviour();
                ((CampaignGameStarter)gameStarterObject).AddBehavior(campaignBehaviour);
            }
        }
    }

#if DEBUG
    public class DebugPatch
    {
        public static void CalculateDisguiseDetectionProbabilityPostfix(ref float __result, Settlement settlement)
        {
            __result = 1f;
        }

        public static void CanAttackPostfix(ref bool __result, MobileParty targetParty)
        {
            if (targetParty.IsMainParty)
            {
                __result = false;
            }
        }
    }

    public class DebugHelpers
    {
        [CommandLineFunctionality.CommandLineArgumentFunction("give_player_gold", "workshopsadvanced")]
        public static string GivePlayerGold(List<string> strings)
        {
            GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, 100000, true);
            return "done";
        }

        [CommandLineFunctionality.CommandLineArgumentFunction("declare_war", "workshopsadvanced")]
        public static string DeclareWar(List<string> strings)
        {
            if (strings.Count == 0)
            {
                return "A target is needed";
            }

            var name = string.Join(" ", strings).ToLower();

            foreach (Kingdom kingdom in Kingdom.All)
            {
                if (Hero.MainHero.MapFaction == kingdom)
                {
                    continue;
                }

                if (name == kingdom.Name.ToString().ToLower())
                {
                    DeclareWarAction.Apply(kingdom, Hero.MainHero.MapFaction);
                    return "done";
                }
            }

            return "Couldn't find kingdom";
        }

        [CommandLineFunctionality.CommandLineArgumentFunction("declare_peace", "workshopsadvanced")]
        public static string DeclarePeace(List<string> strings)
        {
            if (strings.Count == 0)
            {
                return "A target is needed";
            }

            var name = string.Join(" ", strings).ToLower();

            foreach (Kingdom kingdom in Kingdom.All)
            {
                if (Hero.MainHero.MapFaction == kingdom)
                {
                    continue;
                }

                if (name == kingdom.Name.ToString().ToLower())
                {
                    if (kingdom.IsAtWarWith(Hero.MainHero.MapFaction))
                    {
                        MakePeaceAction.Apply(kingdom, Hero.MainHero.MapFaction, 0);
                        return "done";
                    }
                    else
                    {
                        return "Not at war with the kingdom";
                    }
                }
            }

            return "Couldn't find kingdom";
        }
    }
#endif

    public class WorkshopBehaviourPatch
    {
        private static Workshop? _workshop;
        private static float _previousGold;

        public static void ExpensePostfix(Workshop __instance, ref int __result)
        {
            __result = MathF.Round(__result * Helper.GetWorkshopWageMultiplier(__instance));
        }

        public static void IsRunningPostfix(Workshop __instance, ref bool __result)
        {
            __result = __result && Helper.GetIsWorkshopRunning(__instance);
        }

        public static void GetMaxWorkshopCountForTierPostfix(ref int __result, int tier)
        {
            __result += Helper.GetExtraWorkshopCountForTier(tier);
        }

        public static void GetBuyingCostForPlayerPostfix(ref int __result, Workshop workshop)
        {
            __result = Helper.GetWorkshopBuyingCost(__result);
        }

        public static void GetSellingCostPostfix(ref int __result, Workshop workshop)
        {
            if (workshop?.Owner == Hero.MainHero)
            {
                __result = Helper.GetWorkshopBuyingCost(__result);
            }
        }

        public static void ConversionSpeedPostfix(ref float __result)
        {
            var workshop = _workshop;
            if (workshop == null)
            {
                return;
            }

            __result *= Helper.GetConversionSpeedMultiplier(workshop);
        }

        public static void CalculateClanExpensesInternalPostfix(Clan clan, ref ExplainedNumber goldChange, bool applyWithdrawals = false)
        {
            if (clan == Clan.PlayerClan)
            {
                Helper.GetWarehouseRent(out var rentedCount, out var totalRent);
                goldChange.Add(-totalRent, GameTexts.FindText("WA_Warehouse_Rent_Tooltip").SetTextVariable("RENTEDCOUNT", rentedCount));
            }
        }

        public static bool RunTownWorkshopPrefix(Town townComponent, Workshop workshop, bool willBeSold = true)
        {
            _workshop = workshop;
            return true;
        }

        public static bool DoProductionPrefix(ref bool __result, WorkshopType.Production production, Workshop workshop, Town town)
        {
            if (!workshop.Owner.IsHumanPlayerCharacter)
            {
                return true;
            }

            var customizationData = WorkshopsAdvancedCampaignBehaviour.Instance.GetWorkshopCustomizationData(workshop);
            if (!customizationData.IsWorking)
            {
                __result = false;
                return false;
            }

            return true;
        }

        public static void DetermineTownHasSufficientInputsPostfix(ref bool __result, WorkshopType.Production production, Town town, ref int inputMaterialCost)
        {
            var workshop = _workshop;
            if (workshop == null)
            {
                return;
            }

            if (!workshop.Owner.IsHumanPlayerCharacter)
            {
                return;
            }

            var settlementCustomizationData = WorkshopsAdvancedCampaignBehaviour.Instance.GetSettlementCustomizationData(workshop.Settlement);
            if (!settlementCustomizationData.IsRentingWarehouse)
            {
                return;
            }

            var workshopCustomizationData = WorkshopsAdvancedCampaignBehaviour.Instance.GetWorkshopCustomizationData(workshop);
            if (__result && workshopCustomizationData.IsBuyingFromMarket)
            {
                return;
            }

            __result = Helper.DetermineWarehouseHasSufficientInputs(production, town, settlementCustomizationData.Warehouse, out inputMaterialCost);
        }

        public static bool ProduceOutputPrefix(EquipmentElement outputItem, Town town, Workshop workshop, ref int count, bool doNotEffectCapital)
        {
            if (!workshop.Owner.IsHumanPlayerCharacter)
            {
                return true;
            }

            var extraProduct = Helper.CheckAndGetWorkshopExtraProduct(workshop, count);
            count += extraProduct;

            var settlementCustomizationData = WorkshopsAdvancedCampaignBehaviour.Instance.GetSettlementCustomizationData(workshop.Settlement);
            var workshopCustomizationData = WorkshopsAdvancedCampaignBehaviour.Instance.GetWorkshopCustomizationData(workshop);
            if (!settlementCustomizationData.IsRentingWarehouse || workshopCustomizationData.IsSellingToMarket)
            {
                var directSellCount = Helper.GetWorkshopDirectSellCount(workshop, count);
                if (directSellCount > 0)
                {
                    var price = town.GetItemPrice(outputItem, null, false);
                    if (Campaign.Current.GameStarted && !doNotEffectCapital && price < 1000)
                    {
                        workshop.ChangeGold(directSellCount * price);
                        count -= directSellCount;
                    }
                }

                if (count <= 0)
                {
                    CampaignEventDispatcher.Instance.OnItemProduced(outputItem.Item, town.Owner.Settlement, directSellCount);
                    return false;
                }

                return true;
            }

            var ignoreNonTrade = MySettings.Instance?.NonTradeIgnore ?? true;
            if (ignoreNonTrade && !outputItem.Item.IsTradeGood)
            {
                return true;
            }

            settlementCustomizationData.Warehouse.AddToCounts(outputItem, count);
            CampaignEventDispatcher.Instance.OnItemProduced(outputItem.Item, town.Owner.Settlement, count);
            return false;
        }

        public static bool ConsumeInputPrefix(ItemCategory productionInput, Town town, Workshop workshop, bool doNotEffectCapital)
        {
            if (!workshop.Owner.IsHumanPlayerCharacter)
            {
                return true;
            }

            var customizationData = WorkshopsAdvancedCampaignBehaviour.Instance.GetSettlementCustomizationData(workshop.Settlement);
            if (!customizationData.IsRentingWarehouse)
            {
                return true;
            }

            var inputIndex = customizationData.Warehouse.FindIndex((ItemObject x) => x.ItemCategory == productionInput);
            if (inputIndex < 0)
            {
                return true;
            }

            var itemAtIndex = customizationData.Warehouse.GetItemAtIndex(inputIndex);
            customizationData.Warehouse.AddToCounts(itemAtIndex, -1);
            CampaignEventDispatcher.Instance.OnItemConsumed(itemAtIndex, town.Owner.Settlement, 1);
            return false;
        }

        public static bool HandleDailyExpensePrefix(Workshop shop)
        {
            if (!shop.Owner.IsHumanPlayerCharacter)
            {
                return true;
            }

            var minCapital = MySettings.Instance?.WorkshopMinCapital ?? 0;
            var diff = minCapital - shop.Capital;
            if (diff > 0 && shop.Owner.Gold >= diff)
            {
                shop.Owner.Gold -= diff;
                shop.ChangeGold(diff);
            }

            return true;
        }

        public static bool OnSettlementEnteredPrefix(MobileParty mobileParty, Settlement settlement, Hero hero)
        {
            if (mobileParty == null || !mobileParty.IsCaravan)
            {
                return true;
            }

            if (mobileParty.IsCaravan && mobileParty.LeaderHero != null && mobileParty.LeaderHero.Clan == Clan.PlayerClan)
            {
                Helper.CheckAndSellFromWarehouseToCaravan(settlement, mobileParty);
            }

            return true;
        }

        public static bool CalculateHeroIncomeFromWorkshopsPrefix(Hero hero, ref ExplainedNumber goldChange, bool applyWithdrawals)
        {
            _previousGold = goldChange.ResultNumber;
            return true;
        }

        public static void CalculateHeroIncomeFromWorkshopsPostfix(Hero hero, ref ExplainedNumber goldChange, bool applyWithdrawals)
        {
            if (!applyWithdrawals || hero == null || !hero.IsHumanPlayerCharacter)
            {
                return;
            }

            var tradeXPMult = MySettings.Instance?.ProfitTradeXPMult ?? 0f;
            var change = MathF.Max(0f, goldChange.ResultNumber - _previousGold) * tradeXPMult;
            if (change > 0f)
            {
                hero.HeroDeveloper.AddSkillXp(DefaultSkills.Trade, change);
            }
        }

        public static bool ApplyByWarDeclarationPrefix(Workshop workshop)
        {
            var preventLose = MySettings.Instance?.PreventWarLose ?? false;
            return !preventLose || workshop.Owner != Hero.MainHero;
        }
    }

    [Flags]
    public enum WorkshopUpgrades
    {
        None = 0,
        Extension = 1,
        Tools = 2,
        Stall = 4,
        Furniture = 8,
    }

    public static class Helper
    {
        public class UpgradeInfo
        {
            public float Value { get; set; }
            public float Percentage => Value * 100f;
            public int Cost { get; set; }
        }

        public static Dictionary<WorkshopUpgrades, UpgradeInfo> UpgradesInfo = new Dictionary<WorkshopUpgrades, UpgradeInfo>()
        {
            { WorkshopUpgrades.Extension, new UpgradeInfo() { Value = 0.2f, Cost = 5000 } },
            { WorkshopUpgrades.Tools, new UpgradeInfo() { Value = 0.2f, Cost = 8000 } },
            { WorkshopUpgrades.Stall, new UpgradeInfo() { Value = 0.4f, Cost = 4000 } },
            { WorkshopUpgrades.Furniture, new UpgradeInfo() { Value = -0.4f, Cost = 3500 } },
        };

        public static float GetWorkshopWageMultiplier(Workshop workshop)
        {
            var multiplier = MySettings.Instance?.WageMultiplier ?? 1f;
            if (workshop.Owner.IsHumanPlayerCharacter)
            {
                var lowWage = MySettings.Instance?.WorkforceLowWage ?? 0.6f;
                var highWage = MySettings.Instance?.WorkforceHighWage ?? 2f;
                var maxWage = MySettings.Instance?.WorkforceMaxWage ?? 3f;

                var customizationData = WorkshopsAdvancedCampaignBehaviour.Instance.GetWorkshopCustomizationData(workshop);
                if (customizationData.HasUpgrade(WorkshopUpgrades.Furniture))
                {
                    multiplier *= 1f + UpgradesInfo[WorkshopUpgrades.Furniture].Value;
                }

                var level = customizationData.WorkforceLevel;
                if (level < 0) return lowWage * multiplier;
                if (level == 1) return highWage * multiplier;
                if (level > 1) return maxWage * multiplier;
            }

            return multiplier;
        }

        public static bool GetIsWorkshopRunning(Workshop workshop)
        {
            if (!workshop.Owner.IsHumanPlayerCharacter)
            {
                return true;
            }

            var customizationData = WorkshopsAdvancedCampaignBehaviour.Instance.GetWorkshopCustomizationData(workshop);
            if (!customizationData.IsWorking)
            {
                return false;
            }

            var preventLose = MySettings.Instance?.PreventWarLose ?? false;
            if (!preventLose)
            {
                return true;
            }

            return !workshop.Settlement.MapFaction.IsAtWarWith(workshop.Owner.MapFaction);
        }

        public static int GetExtraWorkshopCountForTier(int tier)
        {
            var extraStartingCount = MySettings.Instance?.ExtraStartingCount ?? 0;
            var extraCountPerTier = MySettings.Instance?.ExtraCountPerTier ?? 0;

            return extraStartingCount + (extraCountPerTier * tier);
        }

        public static int GetWorkshopBuyingCost(int originalCost)
        {
            var multiplier = MySettings.Instance?.PriceMultiplier ?? 1f;
            if (multiplier != 1f)
            {
                return MathF.Round(originalCost * multiplier);
            }

            return originalCost;
        }

        public static float GetConversionSpeedMultiplier(Workshop workshop)
        {
            var productionMultiplier = MySettings.Instance?.ProductionMultiplier ?? 1f;
            if (workshop.Owner.IsHumanPlayerCharacter)
            {
                var lowEfficiency = MySettings.Instance?.WorkforceLowEfficinecy ?? 0.75f;
                var highEfficiency = MySettings.Instance?.WorkforceHighEfficinecy ?? 1.5f;
                var maxEfficiency = MySettings.Instance?.WorkforceMaxEfficinecy ?? 1.8f;

                var customizationData = WorkshopsAdvancedCampaignBehaviour.Instance.GetWorkshopCustomizationData(workshop);
                if (customizationData.HasUpgrade(WorkshopUpgrades.Extension))
                {
                    productionMultiplier *= 1f + UpgradesInfo[WorkshopUpgrades.Extension].Value;
                }

                var level = customizationData.WorkforceLevel;
                if (level < 0) return lowEfficiency * productionMultiplier;
                if (level == 1) return highEfficiency * productionMultiplier;
                if (level > 1) return maxEfficiency * productionMultiplier;
            }

            return productionMultiplier;
        }

        public static void GetWarehouseRent(out int rentedCount, out int totalRent)
        {
            rentedCount = 0;
            totalRent = 0;

            var minRent = MySettings.Instance?.WarehouseMinRent ?? 10;
            var maxRent = MySettings.Instance?.WarehouseMaxRent ?? 50;
            var weightThreshold = MySettings.Instance?.WarehouseWeightThreshold ?? 2000;

            var allSettlements = Settlement.All;
            foreach (var settlement in allSettlements)
            {
                var customizationData = WorkshopsAdvancedCampaignBehaviour.Instance.GetSettlementCustomizationData(settlement);
                if (!customizationData.IsRentingWarehouse)
                {
                    continue;
                }

                if (FactionManager.IsAtWarAgainstFaction(Clan.PlayerClan, settlement.MapFaction))
                {
                    continue;
                }

                rentedCount++;
                totalRent += minRent;

                if (minRent < maxRent)
                {
                    var diff = maxRent - minRent;
                    var totalWeight = customizationData.Warehouse.TotalWeight;
                    totalRent += Math.Min(diff, MathF.Round(diff * (totalWeight / weightThreshold)));
                }
            }
        }

        public static bool DetermineWarehouseHasSufficientInputs(WorkshopType.Production production, Town town, ItemRoster warehouse, out int inputMaterialCost)
        {
            IEnumerable<ValueTuple<ItemCategory, int>> inputs = production.Inputs;
            inputMaterialCost = 0;
            foreach (ValueTuple<ItemCategory, int> valueTuple in inputs)
            {
                ItemCategory item = valueTuple.Item1;
                int num = valueTuple.Item2;
                ItemRoster itemRoster = warehouse;
                for (int i = 0; i < itemRoster.Count; i++)
                {
                    ItemObject itemAtIndex = itemRoster.GetItemAtIndex(i);
                    if (itemAtIndex.ItemCategory == item)
                    {
                        int elementNumber = itemRoster.GetElementNumber(i);
                        int num2 = MathF.Min(num, elementNumber);
                        num -= num2;
                        inputMaterialCost += town.GetItemPrice(itemAtIndex, null, false) * num2;
                    }
                }
                if (num > 0)
                {
                    return false;
                }
            }
            return true;
        }

        public static void SellWarehouseContent(Settlement settlement)
        {
            var customizationData = WorkshopsAdvancedCampaignBehaviour.Instance.GetSettlementCustomizationData(settlement);
            foreach (var rosterElement in customizationData.Warehouse)
            {
                SellItemsAction.Apply(PartyBase.MainParty, settlement.Party, rosterElement, rosterElement.Amount, settlement);
            }
        }

        public static void CheckAndSellFromWarehouseToCaravan(Settlement settlement, MobileParty caravan)
        {
            var customizationData = WorkshopsAdvancedCampaignBehaviour.Instance.GetSettlementCustomizationData(settlement);
            if (!customizationData.IsRentingWarehouse)
            {
                return;
            }

            var town = settlement.Town;
            var warehouse = customizationData.Warehouse;
            var caravanBudget = MySettings.Instance?.CaravanBudget ?? 0;
            var pricePercentage = (MySettings.Instance?.CaravanPrice ?? 50) / 100f;
            var budget = MathF.Min(caravanBudget, (int)(caravan.PartyTradeGold * 0.5f));

            while (budget > 0)
            {
                var itemIndex = GetSellableOutputIndexFromWarehouse(settlement, null, true, out var workshopIndex);
                if (itemIndex < 0)
                {
                    return;
                }

                var workshop = town.Workshops[workshopIndex];
                var item = warehouse.GetItemAtIndex(itemIndex);
                var available = warehouse.GetElementNumber(itemIndex);
                var itemPrice = (int)(town.GetItemPrice(item, null, false) * pricePercentage);

                var caravanCapacity = MathF.Floor((caravan.InventoryCapacity * 0.9f - caravan.TotalWeightCarried) / item.Weight);
                var sellCount = MathF.Min(available, MathF.Min(caravanCapacity, budget / itemPrice));
                if (sellCount <= 0)
                {
                    return;
                }

                var cost = sellCount * itemPrice;
                caravan.ItemRoster.AddToCounts(item, sellCount);
                warehouse.AddToCounts(item, -sellCount);
                caravan.PartyTradeGold -= cost;
                workshop.ChangeGold(cost);
                budget -= cost;
            }
        }

        public static int GetSellableOutputIndexFromWarehouse(Settlement settlement, ItemCategory? itemCategory, bool isCaravan, out int workshopIndex)
        {
            workshopIndex = -1;
            var customizationData = WorkshopsAdvancedCampaignBehaviour.Instance.GetSettlementCustomizationData(settlement);
            if (!customizationData.IsRentingWarehouse)
            {
                return -1;
            }

            var ignoreNonTrade = MySettings.Instance?.NonTradeIgnore ?? true;
            var warehouse = customizationData.Warehouse;
            var offset = MBRandom.RandomInt(0, settlement.Town.Workshops.Length);
            for (var i = 0; i < settlement.Town.Workshops.Length; i++)
            {
                workshopIndex = (i + offset) % settlement.Town.Workshops.Length;
                var workshop = settlement.Town.Workshops[workshopIndex];
                if (!workshop.Owner.IsHumanPlayerCharacter)
                {
                    continue;
                }

                var workshopCustomizationData = WorkshopsAdvancedCampaignBehaviour.Instance.GetWorkshopCustomizationData(workshop);
                var isSelling = isCaravan ? workshopCustomizationData.IsSellingToCaravan : workshopCustomizationData.IsSellingToMarket;
                if (!isSelling)
                {
                    continue;
                }

                foreach (var production in workshop.WorkshopType.Productions)
                {
                    foreach (var output in production.Outputs)
                    {
                        if (itemCategory == null || output.Item1 == itemCategory)
                        {
                            for (var j = 0; j < warehouse.Count; j++)
                            {
                                var item = warehouse[j].EquipmentElement.Item;
                                if (item.ItemCategory == output.Item1 && (!ignoreNonTrade || item.IsTradeGood))
                                {
                                    return j;
                                }
                            }
                        }
                    }
                }
            }

            return -1;
        }

        public static int CheckAndGetWorkshopExtraProduct(Workshop workshop, int produced)
        {
            if (!workshop.Owner.IsHumanPlayerCharacter)
            {
                return 0;
            }

            var customizationData = WorkshopsAdvancedCampaignBehaviour.Instance.GetWorkshopCustomizationData(workshop);
            if (!customizationData.HasUpgrade(WorkshopUpgrades.Tools))
            {
                return 0;
            }

            var total = customizationData.LeftOverProduct + (produced * UpgradesInfo[WorkshopUpgrades.Tools].Value);
            var extra = MathF.Floor(total);
            customizationData.LeftOverProduct = total - extra;
            return extra;
        }

        public static int GetWorkshopDirectSellCount(Workshop workshop, int total)
        {
            if (!workshop.Owner.IsHumanPlayerCharacter)
            {
                return 0;
            }

            var customizationData = WorkshopsAdvancedCampaignBehaviour.Instance.GetWorkshopCustomizationData(workshop);
            if (!customizationData.HasUpgrade(WorkshopUpgrades.Stall))
            {
                return 0;
            }

            var sold = 0;
            var chance = UpgradesInfo[WorkshopUpgrades.Stall].Value;
            for (var i = 0; i < total; i++)
            {
                if (MBRandom.RandomFloat <= chance)
                {
                    sold++;
                }
            }

            return sold;
        }

        public static int GetWorkshopUpgradeCost(WorkshopUpgrades upgrade)
        {
            if (UpgradesInfo.TryGetValue(upgrade, out var upgradeInfo))
            {
                return upgradeInfo.Cost;
            }

            return 0;
        }

        public static float GetWorkshopUpgradePercentage(WorkshopUpgrades upgrade)
        {
            if (UpgradesInfo.TryGetValue(upgrade, out var upgradeInfo))
            {
                return upgradeInfo.Percentage;
            }

            return 0;
        }

        public static bool CanBuyWorkshopUpgrade(WorkshopUpgrades upgrade, out int cost)
        {
            cost = 0;
            if (!UpgradesInfo.TryGetValue(upgrade, out var upgradeInfo))
            {
                return false;
            }

            cost = upgradeInfo.Cost;
            return Hero.MainHero.Gold >= upgradeInfo.Cost;
        }

        public static void BuyWorkshopUpgrade(Workshop workshop, WorkshopUpgrades upgrade)
        {
            var customizationData = WorkshopsAdvancedCampaignBehaviour.Instance.GetWorkshopCustomizationData(workshop);
            if (customizationData != null && !customizationData.HasUpgrade(upgrade) && CanBuyWorkshopUpgrade(upgrade, out var cost))
            {
                GiveGoldAction.ApplyBetweenCharacters(Hero.MainHero, null, cost, false);
                customizationData.AddUpgrade(upgrade);
            }
        }

        public static void RemoveWorkshopUpgrade(Workshop workshop, WorkshopUpgrades upgrade)
        {
            var customizationData = WorkshopsAdvancedCampaignBehaviour.Instance.GetWorkshopCustomizationData(workshop);
            if (customizationData != null)
            {
                customizationData.RemoveUpgrade(upgrade);
                if (upgrade == WorkshopUpgrades.Tools)
                {
                    customizationData.LeftOverProduct = 0f;
                }
            }
        }

        public static void OnWorkshopChanged(Workshop workshop, Hero owner, WorkshopType type)
        {
            if (workshop.Owner == owner || owner != Hero.MainHero)
            {
                return;
            }

            var customizationData = WorkshopsAdvancedCampaignBehaviour.Instance.GetWorkshopCustomizationData(workshop);
            if (customizationData != null)
            {
                customizationData.RemoveAllUpgrades();
                customizationData.LeftOverProduct = 0f;
            }
        }

        public static void DisplayWarning(string str)
        {
            InformationManager.DisplayMessage(new InformationMessage(str, new Color(1f, 0.2f, 0.2f)));
        }
    }

    public class MySettings : AttributeGlobalSettings<MySettings>
    {
        public override string Id => "WorkshopsAdvancedSettings";
        public override string DisplayName => new TextObject("{=AA178D5CAE}Workshops Advanced").ToString();
        public override string FolderName => "WorkshopsAdvanced";
        public override string FormatType => "json";

        #region String Definitions
        private const string StrGlobalGroupName = "{=3603CC01E5}Global Customizations";
        private const string StrExtraStartingCount = "{=AFB8C07111}Extra Starting Workshop Count";
        private const string StrExtraStartingCountDesc = "{=330973B628}Additional workshop count at the start of the game. Added to the base value.";
        private const string StrExtraCountPerTier = "{=AA8CC40514}Extra Workshop Count Per Tier";
        private const string StrExtraCountPerTierDesc = "{=D36A864461}Additional workshop count per clan tier. Added to the base value.";
        private const string StrProductionMultiplier = "{=EC07A2D5C2}Production Multiplier";
        private const string StrProductionMultiplierDesc = "{=AE6E81AE28}Production speed multiplier for workshops. Use this if you want to adjust profitability.";
        private const string StrWageMultiplier = "{=E444B78BDF}Wage Multiplier";
        private const string StrWageMultiplierDesc = "{=BF94C60D4B}Wage multiplier for all workforce levels and base value. Use this if you want to adjust profitability.";
        private const string StrPriceMultiplier = "{=2056F4F0DA}Workshop Price Multiplier";
        private const string StrPriceMultiplierDesc = "{=505F34D1FB}Price multiplier for workshops for player.";
        private const string StrNonTradeIgnore = "{=9456B57ACA}Ignore Non-trade Goods";
        private const string StrNonTradeIgnoreDesc = "{=E0C53CB714}Ignores non-trade when not selling to market. Recommended for a balanced game.";
        private const string StrProfitTradeXPMult = "{=346A22CEB9}Profit Trade XP Multiplier";
        private const string StrProfitTradeXPMultDesc = "{=E66DA0670B}Multiplier for Trade skill XP gained per daily profit from workshops.";
        private const string StrWorkshopMinCapital = "{=364475D5BF}Min Capital To Support Workshop";
        private const string StrWorkshopMinCapitalDesc = "{=2AFDF66BC5}Workshop is supplied money from player when capital is under this value to prevent bankruptcy.";
        private const string StrPreventWarLose = "{=C99B931D10}Prevent Losing At War";
        private const string StrPreventWarLoseDesc = "{=FD66009D89}Prevents player losing the workshop if war is declared. The workshop will stop working instead";

        private const string StrWarehouseGroupName = "{=6416E8CB5F}Warehouse";
        private const string StrWarehouseMinRent = "{=992858B61F}Minimum Warehouse Rent";
        private const string StrWarehouseMinRentDesc = "{=0109B196BF}Minimum rent to be paid if the rented warehouse is empty.";
        private const string StrWarehouseMaxRent = "{=26FCC570D2}Maximum Warehouse Rent";
        private const string StrWarehouseMaxRentDesc = "{=7E16171552}Maximum rent to be paid if the rented warehouse is above the weight threshold.";
        private const string StrWarehouseWeightThreshold = "{=8F958065C4}Weight Threshold For Max Rent";
        private const string StrWarehouseWeightThresholdDesc = "{=C283F85181}You will pay maximum rent if total weight is above this.";

        private const string StrWorkforceGroupName = "{=D3C0E12F08}Workshop Workforce";
        private const string StrWorkforceLowWage = "{=EF210D8D5E}Lowered Wage Multiplier";
        private const string StrWorkforceLowEfficinecy = "{=0E9719477C}Lowered Efficiency";
        private const string StrWorkforceHighWage = "{=274FD83FBF}High Wage Multiplier";
        private const string StrWorkforceHighEfficinecy = "{=801F547A51}High Efficiency";
        private const string StrWorkforceMaxWage = "{=0CDCA05A51}Max Wage Multiplier";
        private const string StrWorkforceMaxEfficinecy = "{=3F90F0A421}Max Efficiency";

        private const string StrCaravanGroupName = "{=FE3195AA9E}Your Caravans";
        private const string StrCaravanBudget = "{=1D792BC143}Max Caravan Budget For Workshops";
        private const string StrCaravanBudgetDesc = "{=396F65EBFA}Maximum caravan budget to be spent on buying workshop outputs when they enter the settlement.";
        private const string StrCaravanPrice = "{=B9A8A86817}Price Percentage";
        private const string StrCaravanPriceDesc = "{=5D2C722382}Percentage of price your caravans will need to pay to your workshops.";
        #endregion

        #region Global
        [SettingProperty(StrExtraStartingCount, 0, 100, RequireRestart = false, HintText = StrExtraStartingCountDesc, Order = 1)]
        [SettingPropertyGroup(StrGlobalGroupName, GroupOrder = 1)]
        public int ExtraStartingCount { get; set; } = 0;

        [SettingProperty(StrExtraCountPerTier, 0, 100, RequireRestart = false, HintText = StrExtraCountPerTierDesc, Order = 2)]
        [SettingPropertyGroup(StrGlobalGroupName, GroupOrder = 1)]
        public int ExtraCountPerTier { get; set; } = 0;

        [SettingProperty(StrProductionMultiplier, 0f, 10f, RequireRestart = false, HintText = StrProductionMultiplierDesc, Order = 3)]
        [SettingPropertyGroup(StrGlobalGroupName, GroupOrder = 1)]
        public float ProductionMultiplier { get; set; } = 1f;

        [SettingProperty(StrWageMultiplier, 0f, 10f, RequireRestart = false, HintText = StrWageMultiplierDesc, Order = 4)]
        [SettingPropertyGroup(StrGlobalGroupName, GroupOrder = 1)]
        public float WageMultiplier { get; set; } = 1f;

        [SettingProperty(StrPriceMultiplier, 0f, 10f, RequireRestart = false, HintText = StrPriceMultiplierDesc, Order = 5)]
        [SettingPropertyGroup(StrGlobalGroupName, GroupOrder = 1)]
        public float PriceMultiplier { get; set; } = 1f;

        [SettingProperty(StrNonTradeIgnore, RequireRestart = false, HintText = StrNonTradeIgnoreDesc, Order = 6)]
        [SettingPropertyGroup(StrGlobalGroupName, GroupOrder = 1)]
        public bool NonTradeIgnore { get; set; } = true;

        [SettingProperty(StrProfitTradeXPMult, 0f, 100f, RequireRestart = false, HintText = StrProfitTradeXPMultDesc, Order = 7)]
        [SettingPropertyGroup(StrGlobalGroupName, GroupOrder = 1)]
        public float ProfitTradeXPMult { get; set; } = 0f;

        [SettingProperty(StrWorkshopMinCapital, 0, 20000, RequireRestart = false, HintText = StrWorkshopMinCapitalDesc, Order = 8)]
        [SettingPropertyGroup(StrGlobalGroupName, GroupOrder = 1)]
        public int WorkshopMinCapital { get; set; } = 3000;

        [SettingProperty(StrPreventWarLose, RequireRestart = false, HintText = StrPreventWarLoseDesc, Order = 9)]
        [SettingPropertyGroup(StrGlobalGroupName, GroupOrder = 1)]
        public bool PreventWarLose { get; set; } = false;
        #endregion

        #region Warehouse
        [SettingProperty(StrWarehouseMinRent, 0, 1000, RequireRestart = false, HintText = StrWarehouseMinRentDesc, Order = 1)]
        [SettingPropertyGroup(StrWarehouseGroupName, GroupOrder = 2)]
        public int WarehouseMinRent { get; set; } = 10;

        [SettingProperty(StrWarehouseMaxRent, 0, 1000, RequireRestart = false, HintText = StrWarehouseMaxRentDesc, Order = 2)]
        [SettingPropertyGroup(StrWarehouseGroupName, GroupOrder = 2)]
        public int WarehouseMaxRent { get; set; } = 50;

        [SettingProperty(StrWarehouseWeightThreshold, 0, 100000, RequireRestart = false, HintText = StrWarehouseWeightThresholdDesc, Order = 3)]
        [SettingPropertyGroup(StrWarehouseGroupName, GroupOrder = 2)]
        public int WarehouseWeightThreshold { get; set; } = 2000;
        #endregion

        #region Workforce
        [SettingProperty(StrWorkforceLowWage, 0f, 10f, RequireRestart = false, Order = 1)]
        [SettingPropertyGroup(StrWorkforceGroupName, GroupOrder = 3)]
        public float WorkforceLowWage { get; set; } = 0.6f;

        [SettingProperty(StrWorkforceLowEfficinecy, 0f, 10f, RequireRestart = false, Order = 2)]
        [SettingPropertyGroup(StrWorkforceGroupName, GroupOrder = 3)]
        public float WorkforceLowEfficinecy { get; set; } = 0.75f;

        [SettingProperty(StrWorkforceHighWage, 0f, 10f, RequireRestart = false, Order = 3)]
        [SettingPropertyGroup(StrWorkforceGroupName, GroupOrder = 3)]
        public float WorkforceHighWage { get; set; } = 2f;

        [SettingProperty(StrWorkforceHighEfficinecy, 0f, 10f, RequireRestart = false, Order = 4)]
        [SettingPropertyGroup(StrWorkforceGroupName, GroupOrder = 3)]
        public float WorkforceHighEfficinecy { get; set; } = 1.5f;

        [SettingProperty(StrWorkforceMaxWage, 0f, 10f, RequireRestart = false, Order = 5)]
        [SettingPropertyGroup(StrWorkforceGroupName, GroupOrder = 3)]
        public float WorkforceMaxWage { get; set; } = 3f;

        [SettingProperty(StrWorkforceMaxEfficinecy, 0f, 10f, RequireRestart = false, Order = 6)]
        [SettingPropertyGroup(StrWorkforceGroupName, GroupOrder = 3)]
        public float WorkforceMaxEfficinecy { get; set; } = 1.8f;
        #endregion

        #region Caravans
        [SettingProperty(StrCaravanBudget, 0, 10000, RequireRestart = false, HintText = StrCaravanBudgetDesc, Order = 1)]
        [SettingPropertyGroup(StrCaravanGroupName, GroupOrder = 4)]
        public int CaravanBudget { get; set; } = 1500;

        [SettingProperty(StrCaravanPrice, 0, 100, RequireRestart = false, HintText = StrCaravanPriceDesc, Order = 2)]
        [SettingPropertyGroup(StrCaravanGroupName, GroupOrder = 4)]
        public int CaravanPrice { get; set; } = 50;
        #endregion
    }

    [SaveableRootClass(1)]
    internal class SettlementCustomizationData
    {
        [SaveableProperty(1)]
        internal bool IsRentingWarehouse { get; set; } = false;

        [SaveableProperty(2)]
        internal ItemRoster Warehouse { get; set; } = new ItemRoster();

        [SaveableProperty(3)]
        internal Dictionary<ItemObject, ItemPriceData> PriceDataDict { get; set; } = new Dictionary<ItemObject, ItemPriceData>();
    }

    [SaveableRootClass(2)]
    internal class WorkshopCustomizationData
    {
        [SaveableProperty(1)]
        internal bool IsWorking { get; set; } = true;

        [SaveableProperty(2)]
        internal bool IsBuyingFromMarket { get; set; } = true;

        [SaveableProperty(3)]
        internal bool IsSellingToMarket { get; set; } = true;

        [SaveableProperty(4)]
        internal int WorkforceLevel { get; set; } = 0;

        [SaveableProperty(5)]
        internal bool IsSellingToCaravan { get; set; } = true;

        [SaveableProperty(6)]
        internal int Upgrades { get; set; } = (int)WorkshopUpgrades.None;

        [SaveableProperty(7)]
        internal float LeftOverProduct { get; set; } = 0f;

        public bool HasUpgrade(WorkshopUpgrades upgrade)
        {
            var upgradeEnum = (WorkshopUpgrades)Upgrades;
            return upgradeEnum.HasFlag(upgrade);
        }

        public void AddUpgrade(WorkshopUpgrades upgrade)
        {
            var upgradeEnum = (WorkshopUpgrades)Upgrades;
            upgradeEnum |= upgrade;
            Upgrades = (int)upgradeEnum;
        }

        public void RemoveUpgrade(WorkshopUpgrades upgrade)
        {
            var upgradeEnum = (WorkshopUpgrades)Upgrades;
            upgradeEnum &= ~upgrade;
            Upgrades = (int)upgradeEnum;
        }

        public void RemoveAllUpgrades()
        {
            Upgrades = (int)WorkshopUpgrades.None;
        }
    }

    [SaveableRootClass(3)]
    internal class ItemPriceData
    {
        [SaveableProperty(1)]
        internal int LastUpdatedDay { get; private set; } = 0;

        [SaveableProperty(2)]
        internal float AveragePrice { get; private set; } = 0f;

        public void RegisterPrice(int price)
        {
            var currentDay = MathF.Floor(CampaignTime.Now.ToDays);
            if (LastUpdatedDay != currentDay)
            {
                AveragePrice = (currentDay - LastUpdatedDay) < 10 ? (AveragePrice * 0.8f) + (price * 0.2f) : price;
                LastUpdatedDay = currentDay;
            }
        }
    }

    internal class WorkshopsAdvancedCampaignBehaviour : CampaignBehaviorBase
    {
        private struct UpgradeInfo
        {
            public string MenuId { get; set; }
            public TextObject Name { get; set; }
            public string Description { get; set; }
            public GameMenuOption.LeaveType LeaveType { get; set; }
        }

        internal static WorkshopsAdvancedCampaignBehaviour Instance { get; private set; }

        static WorkshopsAdvancedCampaignBehaviour()
        {
            Instance = new WorkshopsAdvancedCampaignBehaviour();
        }

        internal WorkshopsAdvancedCampaignBehaviour()
        {
            Instance = this;
        }

        #region Menu Strings
        private readonly TextObject MenuGoBack = GameTexts.FindText("WA_Menu_Go_Back");
        private readonly TextObject ManageWorkshopsMenuName = GameTexts.FindText("WA_Manage_Workshops");
        private readonly TextObject ManageWorkshopsRentWarehouse = GameTexts.FindText("WA_Manage_Workshops_Rent_Warehouse");
        private readonly TextObject ManageWorkshopsStopRenting = GameTexts.FindText("WA_Manage_Workshops_Stop_Renting");
        private readonly TextObject ManageWorkshopsShowWarehouse = GameTexts.FindText("WA_Manage_Workshops_Show_Warehouse");
        private readonly TextObject ManageWorkshopsNotRenting = GameTexts.FindText("WA_Manage_Workshops_Not_Renting");
        private readonly TextObject ManageTownWorkshopStopWorking = GameTexts.FindText("WA_Manage_Town_Workshop_Stop_Working");
        private readonly TextObject ManageTownWorkshopContinueWorking = GameTexts.FindText("WA_Manage_Town_Workshop_Continue_Working");
        private readonly TextObject ManageTownWorkshopDoNotBuy = GameTexts.FindText("WA_Manage_Town_Workshop_Do_Not_Buy");
        private readonly TextObject ManageTownWorkshopBuyFrom = GameTexts.FindText("WA_Manage_Town_Workshop_Buy_From");
        private readonly TextObject ManageTownWorkshopDoNotSell = GameTexts.FindText("WA_Manage_Town_Workshop_Do_Not_Sell");
        private readonly TextObject ManageTownWorkshopSellTo = GameTexts.FindText("WA_Manage_Town_Workshop_Sell_To");
        private readonly TextObject ManageTownWorkshopDoNotCaravan = GameTexts.FindText("WA_Manage_Town_Workshop_Do_Not_Caravan");
        private readonly TextObject ManageTownWorkshopSellCaravan = GameTexts.FindText("WA_Manage_Town_Workshop_Sell_Caravan");
        private readonly TextObject ManageTownWorkshopNeedWarehouse = GameTexts.FindText("WA_Manage_Town_Workshop_Need_Warehouse");
        private readonly TextObject AdjustWorkforceMenuName = GameTexts.FindText("WA_Adjust_Workforce_Menu_Name");
        private readonly TextObject AdjustWorkforceSelected = GameTexts.FindText("WA_Adjust_Selected");
        private readonly TextObject InquiryStopRentingTitle = GameTexts.FindText("WA_Inquiry_Stop_Renting");
        private readonly TextObject InquiryStopRentingDesc = GameTexts.FindText("WA_Inquiry_Stop_Renting_Desc");
        private readonly TextObject UpgradesMenuName = GameTexts.FindText("WA_Up_Name");
        private readonly TextObject UpgradesBuy = GameTexts.FindText("WA_Up_Buy");
        private readonly TextObject UpgradesBuyDesc = GameTexts.FindText("WA_Up_Buy_Desc");
        private readonly TextObject UpgradesBuyNoMoney = GameTexts.FindText("WA_Up_Buy_No_Money");
        private readonly TextObject UpgradesRemove = GameTexts.FindText("WA_Up_Remove");
        private readonly TextObject UpgradesRemoveDesc = GameTexts.FindText("WA_Up_Remove_Desc");
        private readonly TextObject UpgradesNone = GameTexts.FindText("WA_Up_None");
        private const string ManageWorkshopsDesc = "{=FCC955FAD7}Manage owned workshops.";
        private const string ManageTownWorkshopDesc = "{=0F7F695CD1}You can manage your workshop ({WORKSHOPNAME}) here. Expected profit: {PROFIT}";
        private const string AdjustWorkforceDesc = "{=BE032A38D8}Adjust workforce of your workshop";
        private const string AdjustWorkforceLow = "{=DA35F5B05D}Lowered";
        private const string AdjustWorkforceNormal = "{=1C490DD392}Normal (Default)";
        private const string AdjustWorkforceHigh = "{=655D20C1CA}High";
        private const string AdjustWorkforceMax = "{=6A061313D2}Max";
        private const string UpgradesMenuDesc = "{=6E0AD7BDF8}You can upgrade your workshop in various ways to increase profit. Currently you have: {UPGRADES}";
        private const string MenuGoBackStr = "{=4F2F5E1D6E}Go Back";

        private const string ManageWorkshopsId = "manage_workshops";
        private const string ManageWorkshopsRentWarehouseId = "manage_workshops_rent_warehouse";
        private const string ManageWorkshopsShowWarehouseId = "manage_workshops_show_warehouse";
        private const string ManageTownWorkshopObject = "manage_town_workshop_object";
        private const string ManageWorkshopGoBackId = "manage_workshops_go_back";
        private const string ManageTownWorkshopIdPrefix = "manage_town_workshop_";
        private const string ManageTownWorkshopProductionId = "manage_town_workshop_production";
        private const string ManageTownWorkshopBuyFromMarketId = "manage_town_workshop_buy_from_market";
        private const string ManageTownWorkshopSellToMarketId = "manage_town_workshop_sell_to_market";
        private const string ManageTownWorkshopSellToCaravanId = "manage_town_workshop_sell_to_caravan";
        private const string ManageTownWorkshopGoBackId = "manage_town_workshop_go_back";
        private const string AdjustWorkforceId = "adjust_workforce";
        private const string AdjustWorkforceLowId = "adjust_workforce_low";
        private const string AdjustWorkforceNormalId = "adjust_workforce_normal";
        private const string AdjustWorkforceHighId = "adjust_workforce_high";
        private const string AdjustWorkforceMaxId = "adjust_workforce_max";
        private const string AdjustWorkforceGoBackId = "adjust_workforce_go_back";
        private const string UpgradesId = "workshop_upgrades";
        private const string UpgradesGoBackId = "workshop_upgrades_go_back";
        private const string UpgradesBuyId = "workshop_upgrades_buy";
        private const string UpgradesRemoveId = "workshop_upgrades_remove";

        private static string UpgradesExtensionDesc = "{=024B8CA99E}Add a building extension to your workshop. This will cost {UP_COST}. Extension will let your workers be more efficient, resulting in higher production speed ({UPGRADE_VALUE}%). Be aware though higher production will cause higher consumption of materials and providing more products to the market. This will likely affect prices.";
        private static string UpgradesToolsDesc = "{=AB17A0D1E1}Buy extra tools for your workers. This will cost {UP_COST}. Extra tools will improve production process, resulting in higher amount of product from same amount of materials ({UPGRADE_VALUE}%). Note that if the bonus is not a whole number, it will be saved until it can be used to produce an extra product.";
        private static string UpgradesStallDesc = "{=E46084C797}Buy a market stall for your products. This will cost {UP_COST}. Market stall will give you a chance ({UPGRADE_VALUE}%) to sell your products directly to townsfolk instead of town market. Selling directly to townsfolk will decrease supply and help with keeping prices higher. Note that the chance of selling to townsfolk is calculated separately for every product.";
        private static string UpgradesFurnitureDesc = "{=04FD0F131A}Provide better furniture for your workers. This will cost {UP_COST}. Providing better furniture will increase happiness of your workers, resulting in lower wages ({UPGRADE_VALUE}%).";
        #endregion

        private readonly Dictionary<WorkshopUpgrades, UpgradeInfo> UpgradeNames = new Dictionary<WorkshopUpgrades, UpgradeInfo>()
        {
            { WorkshopUpgrades.Extension, new UpgradeInfo() { MenuId = WorkshopUpgrades.Extension + "_id", Name = GameTexts.FindText("WA_Up_Extension"), Description = UpgradesExtensionDesc, LeaveType = GameMenuOption.LeaveType.Manage} },
            { WorkshopUpgrades.Tools, new UpgradeInfo() { MenuId = WorkshopUpgrades.Tools + "_id", Name = GameTexts.FindText("WA_Up_Tools"), Description = UpgradesToolsDesc, LeaveType = GameMenuOption.LeaveType.Craft} },
            { WorkshopUpgrades.Stall, new UpgradeInfo() { MenuId = WorkshopUpgrades.Stall + "_id", Name = GameTexts.FindText("WA_Up_Stall"), Description = UpgradesStallDesc, LeaveType = GameMenuOption.LeaveType.Trade} },
            { WorkshopUpgrades.Furniture, new UpgradeInfo() { MenuId = WorkshopUpgrades.Furniture + "_id", Name = GameTexts.FindText("WA_Up_Furniture"), Description = UpgradesFurnitureDesc, LeaveType = GameMenuOption.LeaveType.Manage} },
        };

        [SaveableField(1)]
        internal Dictionary<Settlement, SettlementCustomizationData> SettlementDataDict = new Dictionary<Settlement, SettlementCustomizationData>();

        [SaveableField(2)]
        internal Dictionary<Workshop, WorkshopCustomizationData> WorkshopDataDict = new Dictionary<Workshop, WorkshopCustomizationData>();

        public override void RegisterEvents()
        {
            CampaignEvents.OnWorkshopChangedEvent.AddNonSerializedListener(this, new Action<Workshop, Hero, WorkshopType>(OnWorkshopChanged));
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(AddWorkshopMenus));
        }

        public override void SyncData(IDataStore dataStore)
        {
            try
            {
                dataStore.SyncData("SettlementDataDict", ref SettlementDataDict);
                dataStore.SyncData("WorkshopDataDict", ref WorkshopDataDict);
            }
            catch (Exception)
            {

            }
            finally
            {
                if (SettlementDataDict == null) SettlementDataDict = new Dictionary<Settlement, SettlementCustomizationData>();
                if (WorkshopDataDict == null) WorkshopDataDict = new Dictionary<Workshop, WorkshopCustomizationData>();
            }
        }

        protected void OnWorkshopChanged(Workshop workshop, Hero owner, WorkshopType type)
        {
            Helper.OnWorkshopChanged(workshop, owner, type);
        }

        protected void AddWorkshopMenus(CampaignGameStarter campaignGameStarter)
        {
            campaignGameStarter.AddGameMenu(ManageWorkshopsId, ManageWorkshopsDesc,
                new OnInitDelegate((callbackArgs) =>
                {
                    AddManageWorkshopsMenuOptions(Settlement.CurrentSettlement);
                    callbackArgs.optionLeaveType = GameMenuOption.LeaveType.Leave;
                }), GameOverlays.MenuOverlayType.SettlementWithBoth);

            var townMenu = Campaign.Current.SandBoxManager.GameStarter.GetPresumedGameMenu("town");
            AddGameMenuOptionWithRelatedObject(townMenu, ManageWorkshopsId, ManageWorkshopsMenuName,
                new GameMenuOption.OnConditionDelegate((callbackArgs) =>
                {
                    callbackArgs.optionLeaveType = GameMenuOption.LeaveType.Submenu;
                    callbackArgs.IsEnabled = !FactionManager.IsAtWarAgainstFaction(Clan.PlayerClan, Settlement.CurrentSettlement.MapFaction);
                    return true;
                }),
                new GameMenuOption.OnConsequenceDelegate((callbackArgs) =>
                {
                    GameMenu.SwitchToMenu(ManageWorkshopsId);
                }), 5, false, false, null);
        }

        private void AddManageWorkshopsMenuOptions(Settlement settlement)
        {
            var settlementCustomizationData = GetSettlementCustomizationData(settlement);
            var manageWorkshopsGameMenu = Campaign.Current.SandBoxManager.GameStarter.GetPresumedGameMenu(ManageWorkshopsId);

            Campaign.Current.GameMenuManager.RemoveRelatedGameMenuOptions(ManageWorkshopsRentWarehouseId);
            AddGameMenuOptionWithRelatedObject(manageWorkshopsGameMenu, ManageWorkshopsRentWarehouseId, settlementCustomizationData.IsRentingWarehouse ? ManageWorkshopsStopRenting : ManageWorkshopsRentWarehouse,
                new GameMenuOption.OnConditionDelegate((callbackArgs) =>
                {
                    callbackArgs.optionLeaveType = GameMenuOption.LeaveType.Manage;
                    callbackArgs.IsEnabled = true;
                    return true;
                }),
                new GameMenuOption.OnConsequenceDelegate((callbackArgs) =>
                {
                    if (settlementCustomizationData.IsRentingWarehouse)
                    {
                        InformationManager.ShowInquiry(new InquiryData(InquiryStopRentingTitle.ToString(), InquiryStopRentingDesc.ToString(),
                            true, true, GameTexts.FindText("str_accept", null).ToString(), GameTexts.FindText("str_reject", null).ToString(), () =>
                            {
                                Helper.SellWarehouseContent(settlement);
                                settlementCustomizationData.IsRentingWarehouse = false;
                                callbackArgs.MenuContext.Refresh();
                            }, null), false, false);
                    }
                    else
                    {
                        settlementCustomizationData.IsRentingWarehouse = true;
                        callbackArgs.MenuContext.Refresh();
                    }
                }), -1, false, false, ManageWorkshopsRentWarehouseId);

            Campaign.Current.GameMenuManager.RemoveRelatedGameMenuOptions(ManageWorkshopsShowWarehouseId);
            AddGameMenuOptionWithRelatedObject(manageWorkshopsGameMenu, ManageWorkshopsShowWarehouseId, settlementCustomizationData.IsRentingWarehouse ? ManageWorkshopsShowWarehouse : ManageWorkshopsNotRenting,
                new GameMenuOption.OnConditionDelegate((callbackArgs) =>
                {
                    callbackArgs.optionLeaveType = GameMenuOption.LeaveType.Manage;
                    callbackArgs.IsEnabled = settlementCustomizationData.IsRentingWarehouse;
                    return true;
                }),
                new GameMenuOption.OnConsequenceDelegate((callbackArgs) =>
                {
                    InventoryManager.OpenScreenAsStash(settlementCustomizationData.Warehouse);
                }), -1, false, false, ManageWorkshopsShowWarehouseId);

            var workshops = settlement.Town.Workshops;
            for (var i = 0; i < workshops.Length; i++)
            {
                if (workshops[i].WorkshopType.IsHidden)
                {
                    continue;
                }

                Campaign.Current.GameMenuManager.RemoveRelatedGameMenus(ManageTownWorkshopObject);
                Campaign.Current.GameMenuManager.RemoveRelatedGameMenuOptions(ManageTownWorkshopObject);
            }

            for (var i = 0; i < workshops.Length; i++)
            {
                if (workshops[i].WorkshopType.IsHidden)
                {
                    continue;
                }

                var workshop = workshops[i];
                var workshopMenuId = ManageTownWorkshopIdPrefix + i;
                Campaign.Current.SandBoxManager.GameStarter.AddGameMenu(workshopMenuId, ManageTownWorkshopDesc,
                    new OnInitDelegate((callbackArgs) =>
                    {
                        var profit = Campaign.Current.Models.ClanFinanceModel.CalculateOwnerIncomeFromWorkshop(workshop);
                        callbackArgs.MenuContext.GameMenu.GetText().SetTextVariable("WORKSHOPNAME", workshop.Name).SetTextVariable("PROFIT", profit);
                        callbackArgs.IsEnabled = true;
                        callbackArgs.optionLeaveType = GameMenuOption.LeaveType.Leave;
                        AddManageTownWorkshopMenuOptions(workshopMenuId, workshop);
                    }),
                    GameOverlays.MenuOverlayType.SettlementWithBoth, relatedObject: ManageTownWorkshopObject);

                AddGameMenuOptionWithRelatedObject(manageWorkshopsGameMenu, workshopMenuId, workshop.Name,
                    new GameMenuOption.OnConditionDelegate((callbackArgs) =>
                    {
                        callbackArgs.optionLeaveType = GameMenuOption.LeaveType.Craft;
                        callbackArgs.IsEnabled = workshop.Owner.IsHumanPlayerCharacter;
                        return true;
                    }),
                    new GameMenuOption.OnConsequenceDelegate((callbackArgs) =>
                    {
                        GameMenu.SwitchToMenu(workshopMenuId);
                    }), -1, false, false, ManageTownWorkshopObject);
            }

            Campaign.Current.GameMenuManager.RemoveRelatedGameMenuOptions(ManageWorkshopGoBackId);
            AddGameMenuOptionWithRelatedObject(manageWorkshopsGameMenu, ManageWorkshopGoBackId, MenuGoBack,
                new GameMenuOption.OnConditionDelegate((callbackArgs) =>
                {
                    callbackArgs.optionLeaveType = GameMenuOption.LeaveType.Leave;
                    callbackArgs.IsEnabled = true;
                    return true;
                }),
                new GameMenuOption.OnConsequenceDelegate((callbackArgs) =>
                {
                    GameMenu.SwitchToMenu("town");
                }), -1, false, false, ManageWorkshopGoBackId);
        }

        private void AddManageTownWorkshopMenuOptions(string workshopMenuId, Workshop workshop)
        {
            var workshopCustomizationData = GetWorkshopCustomizationData(workshop);
            var settlementCustomizationData = GetSettlementCustomizationData(workshop.Settlement);
            var workshopGameMenu = Campaign.Current.SandBoxManager.GameStarter.GetPresumedGameMenu(workshopMenuId);
            AddAdjustWorkforceMenu(workshopGameMenu, workshop);
            AddUpgradesMenu(workshopGameMenu, workshop);

            Campaign.Current.GameMenuManager.RemoveRelatedGameMenuOptions(ManageTownWorkshopProductionId);
            AddGameMenuOptionWithRelatedObject(workshopGameMenu, ManageTownWorkshopProductionId, workshopCustomizationData.IsWorking ? ManageTownWorkshopStopWorking : ManageTownWorkshopContinueWorking,
                new GameMenuOption.OnConditionDelegate((callbackArgs) =>
                {
                    callbackArgs.optionLeaveType = GameMenuOption.LeaveType.Craft;
                    callbackArgs.IsEnabled = true;
                    return true;
                }),
                new GameMenuOption.OnConsequenceDelegate((callbackArgs) =>
                {
                    workshopCustomizationData.IsWorking = !workshopCustomizationData.IsWorking;
                    callbackArgs.MenuContext.Refresh();
                }), -1, false, false, ManageTownWorkshopProductionId);

            var isBuyingFromMarket = workshopCustomizationData.IsBuyingFromMarket || !settlementCustomizationData.IsRentingWarehouse;
            Campaign.Current.GameMenuManager.RemoveRelatedGameMenuOptions(ManageTownWorkshopBuyFromMarketId);
            AddGameMenuOptionWithRelatedObject(workshopGameMenu, ManageTownWorkshopBuyFromMarketId, isBuyingFromMarket ? ManageTownWorkshopDoNotBuy : ManageTownWorkshopBuyFrom,
                new GameMenuOption.OnConditionDelegate((callbackArgs) =>
                {
                    if (!settlementCustomizationData.IsRentingWarehouse)
                    {
                        callbackArgs.Tooltip = ManageTownWorkshopNeedWarehouse;
                    }

                    callbackArgs.optionLeaveType = GameMenuOption.LeaveType.Trade;
                    callbackArgs.IsEnabled = settlementCustomizationData.IsRentingWarehouse;
                    return true;
                }),
                new GameMenuOption.OnConsequenceDelegate((callbackArgs) =>
                {
                    workshopCustomizationData.IsBuyingFromMarket = !workshopCustomizationData.IsBuyingFromMarket;
                    callbackArgs.MenuContext.Refresh();
                }), -1, false, false, ManageTownWorkshopBuyFromMarketId);

            var isSellingToMarket = workshopCustomizationData.IsSellingToMarket || !settlementCustomizationData.IsRentingWarehouse;
            Campaign.Current.GameMenuManager.RemoveRelatedGameMenuOptions(ManageTownWorkshopSellToMarketId);
            AddGameMenuOptionWithRelatedObject(workshopGameMenu, ManageTownWorkshopSellToMarketId, isSellingToMarket ? ManageTownWorkshopDoNotSell : ManageTownWorkshopSellTo,
                new GameMenuOption.OnConditionDelegate((callbackArgs) =>
                {
                    if (!settlementCustomizationData.IsRentingWarehouse)
                    {
                        callbackArgs.Tooltip = ManageTownWorkshopNeedWarehouse;
                    }

                    callbackArgs.optionLeaveType = GameMenuOption.LeaveType.Trade;
                    callbackArgs.IsEnabled = settlementCustomizationData.IsRentingWarehouse;
                    return true;
                }),
                new GameMenuOption.OnConsequenceDelegate((callbackArgs) =>
                {
                    workshopCustomizationData.IsSellingToMarket = !workshopCustomizationData.IsSellingToMarket;
                    callbackArgs.MenuContext.Refresh();
                }), -1, false, false, ManageTownWorkshopSellToMarketId);

            var isSellingToCaravan = workshopCustomizationData.IsSellingToCaravan || !settlementCustomizationData.IsRentingWarehouse;
            Campaign.Current.GameMenuManager.RemoveRelatedGameMenuOptions(ManageTownWorkshopSellToCaravanId);
            AddGameMenuOptionWithRelatedObject(workshopGameMenu, ManageTownWorkshopSellToCaravanId, isSellingToCaravan ? ManageTownWorkshopDoNotCaravan : ManageTownWorkshopSellCaravan,
                new GameMenuOption.OnConditionDelegate((callbackArgs) =>
                {
                    if (!settlementCustomizationData.IsRentingWarehouse)
                    {
                        callbackArgs.Tooltip = ManageTownWorkshopNeedWarehouse;
                    }

                    callbackArgs.optionLeaveType = GameMenuOption.LeaveType.Trade;
                    callbackArgs.IsEnabled = settlementCustomizationData.IsRentingWarehouse;
                    return true;
                }),
                new GameMenuOption.OnConsequenceDelegate((callbackArgs) =>
                {
                    workshopCustomizationData.IsSellingToCaravan = !workshopCustomizationData.IsSellingToCaravan;
                    callbackArgs.MenuContext.Refresh();
                }), -1, false, false, ManageTownWorkshopSellToCaravanId);

            Campaign.Current.GameMenuManager.RemoveRelatedGameMenuOptions(ManageTownWorkshopGoBackId);
            AddGameMenuOptionWithRelatedObject(workshopGameMenu, ManageTownWorkshopGoBackId, MenuGoBack,
                new GameMenuOption.OnConditionDelegate((callbackArgs) =>
                {
                    callbackArgs.optionLeaveType = GameMenuOption.LeaveType.Leave;
                    callbackArgs.IsEnabled = true;
                    return true;
                }),
                new GameMenuOption.OnConsequenceDelegate((callbackArgs) =>
                {
                    GameMenu.SwitchToMenu(ManageWorkshopsId);
                }), -1, false, false, ManageTownWorkshopGoBackId);
        }

        private void AddAdjustWorkforceMenu(GameMenu workshopGameMenu, Workshop workshop)
        {
            Campaign.Current.GameMenuManager.RemoveRelatedGameMenus(AdjustWorkforceId);
            Campaign.Current.SandBoxManager.GameStarter.AddGameMenu(AdjustWorkforceId, AdjustWorkforceDesc,
                new OnInitDelegate((callbackArgs) =>
                {
                    callbackArgs.IsEnabled = true;
                    callbackArgs.optionLeaveType = GameMenuOption.LeaveType.Leave;
                }),
                GameOverlays.MenuOverlayType.SettlementWithBoth, relatedObject: AdjustWorkforceId);

            Campaign.Current.GameMenuManager.RemoveRelatedGameMenuOptions(AdjustWorkforceId);
            AddGameMenuOptionWithRelatedObject(workshopGameMenu, AdjustWorkforceId, AdjustWorkforceMenuName,
                new GameMenuOption.OnConditionDelegate((callbackArgs) =>
                {
                    callbackArgs.optionLeaveType = GameMenuOption.LeaveType.Submenu;
                    callbackArgs.IsEnabled = true;
                    return true;
                }),
                new GameMenuOption.OnConsequenceDelegate((callbackArgs) =>
                {
                    GameMenu.SwitchToMenu(AdjustWorkforceId);
                }), -1, false, false, AdjustWorkforceId);

            var workshopCustomizationData = GetWorkshopCustomizationData(workshop);
            Campaign.Current.SandBoxManager.GameStarter.AddGameMenuOption(AdjustWorkforceId, AdjustWorkforceLowId, AdjustWorkforceLow,
                new GameMenuOption.OnConditionDelegate((callbackArgs) =>
                {
                    var isSelected = workshopCustomizationData.WorkforceLevel < 0;
                    if (isSelected)
                    {
                        callbackArgs.Tooltip = AdjustWorkforceSelected;
                    }

                    callbackArgs.optionLeaveType = GameMenuOption.LeaveType.Recruit;
                    callbackArgs.IsEnabled = !isSelected;
                    return true;
                }),
                new GameMenuOption.OnConsequenceDelegate((callbackArgs) =>
                {
                    workshopCustomizationData.WorkforceLevel = -1;
                    callbackArgs.MenuContext.Refresh();
                }));

            Campaign.Current.SandBoxManager.GameStarter.AddGameMenuOption(AdjustWorkforceId, AdjustWorkforceNormalId, AdjustWorkforceNormal,
                new GameMenuOption.OnConditionDelegate((callbackArgs) =>
                {
                    var isSelected = workshopCustomizationData.WorkforceLevel == 0;
                    if (isSelected)
                    {
                        callbackArgs.Tooltip = AdjustWorkforceSelected;
                    }

                    callbackArgs.optionLeaveType = GameMenuOption.LeaveType.Recruit;
                    callbackArgs.IsEnabled = !isSelected;
                    return true;
                }),
                new GameMenuOption.OnConsequenceDelegate((callbackArgs) =>
                {
                    workshopCustomizationData.WorkforceLevel = 0;
                    callbackArgs.MenuContext.Refresh();
                }));

            Campaign.Current.SandBoxManager.GameStarter.AddGameMenuOption(AdjustWorkforceId, AdjustWorkforceHighId, AdjustWorkforceHigh,
                new GameMenuOption.OnConditionDelegate((callbackArgs) =>
                {
                    var isSelected = workshopCustomizationData.WorkforceLevel == 1;
                    if (isSelected)
                    {
                        callbackArgs.Tooltip = AdjustWorkforceSelected;
                    }

                    callbackArgs.optionLeaveType = GameMenuOption.LeaveType.Recruit;
                    callbackArgs.IsEnabled = !isSelected;
                    return true;
                }),
                new GameMenuOption.OnConsequenceDelegate((callbackArgs) =>
                {
                    workshopCustomizationData.WorkforceLevel = 1;
                    callbackArgs.MenuContext.Refresh();
                }));

            Campaign.Current.SandBoxManager.GameStarter.AddGameMenuOption(AdjustWorkforceId, AdjustWorkforceMaxId, AdjustWorkforceMax,
                new GameMenuOption.OnConditionDelegate((callbackArgs) =>
                {
                    var isSelected = workshopCustomizationData.WorkforceLevel > 1;
                    if (isSelected)
                    {
                        callbackArgs.Tooltip = AdjustWorkforceSelected;
                    }

                    callbackArgs.optionLeaveType = GameMenuOption.LeaveType.Recruit;
                    callbackArgs.IsEnabled = !isSelected;
                    return true;
                }),
                new GameMenuOption.OnConsequenceDelegate((callbackArgs) =>
                {
                    workshopCustomizationData.WorkforceLevel = 2;
                    callbackArgs.MenuContext.Refresh();
                }));

            Campaign.Current.SandBoxManager.GameStarter.AddGameMenuOption(AdjustWorkforceId, AdjustWorkforceGoBackId, MenuGoBackStr,
                new GameMenuOption.OnConditionDelegate((callbackArgs) =>
                {
                    callbackArgs.optionLeaveType = GameMenuOption.LeaveType.Leave;
                    callbackArgs.IsEnabled = true;
                    return true;
                }),
                new GameMenuOption.OnConsequenceDelegate((callbackArgs) =>
                {
                    GameMenu.SwitchToMenu(workshopGameMenu.StringId);
                }));
        }

        private void AddUpgradesMenu(GameMenu workshopGameMenu, Workshop workshop)
        {
            Campaign.Current.GameMenuManager.RemoveRelatedGameMenus(UpgradesId);
            Campaign.Current.SandBoxManager.GameStarter.AddGameMenu(UpgradesId, UpgradesMenuDesc,
                new OnInitDelegate((callbackArgs) =>
                {
                    callbackArgs.IsEnabled = true;
                    callbackArgs.MenuContext.GameMenu.GetText().SetTextVariable("UPGRADES", GetWorkshopUpgrades(workshop));
                    callbackArgs.optionLeaveType = GameMenuOption.LeaveType.Leave;
                }),
                GameOverlays.MenuOverlayType.SettlementWithBoth, relatedObject: UpgradesId);

            Campaign.Current.GameMenuManager.RemoveRelatedGameMenuOptions(UpgradesId);
            AddGameMenuOptionWithRelatedObject(workshopGameMenu, UpgradesId, UpgradesMenuName,
                new GameMenuOption.OnConditionDelegate((callbackArgs) =>
                {
                    callbackArgs.optionLeaveType = GameMenuOption.LeaveType.Submenu;
                    callbackArgs.IsEnabled = true;
                    return true;
                }),
                new GameMenuOption.OnConsequenceDelegate((callbackArgs) =>
                {
                    GameMenu.SwitchToMenu(UpgradesId);
                }), -1, false, false, UpgradesId);

            var upgradesMenu = Campaign.Current.SandBoxManager.GameStarter.GetPresumedGameMenu(UpgradesId);
            foreach (var kvp in UpgradeNames)
            {
                AddUpgradeOptionMenu(upgradesMenu, kvp.Key, workshop);
            }

            Campaign.Current.SandBoxManager.GameStarter.AddGameMenuOption(UpgradesId, UpgradesGoBackId, MenuGoBackStr,
                new GameMenuOption.OnConditionDelegate((callbackArgs) =>
                {
                    callbackArgs.optionLeaveType = GameMenuOption.LeaveType.Leave;
                    callbackArgs.IsEnabled = true;
                    return true;
                }),
                new GameMenuOption.OnConsequenceDelegate((callbackArgs) =>
                {
                    GameMenu.SwitchToMenu(workshopGameMenu.StringId);
                }));
        }

        private void AddUpgradeOptionMenu(GameMenu upgradesMenu, WorkshopUpgrades upgrade, Workshop workshop)
        {
            var customizationData = GetWorkshopCustomizationData(workshop);
            var upgradeInfo = UpgradeNames[upgrade];
            var menuId = upgradeInfo.MenuId;
            var menuGoBackId = upgrade + "_go_back_id";
            var upgradeValue = Helper.GetWorkshopUpgradePercentage(upgrade);

            Campaign.Current.GameMenuManager.RemoveRelatedGameMenus(menuId);
            Campaign.Current.SandBoxManager.GameStarter.AddGameMenu(menuId, upgradeInfo.Description,
                new OnInitDelegate((callbackArgs) =>
                {
                    var cost = Helper.GetWorkshopUpgradeCost(upgrade);
                    callbackArgs.IsEnabled = true;
                    callbackArgs.MenuContext.GameMenu.GetText().SetTextVariable("UP_COST", cost).SetTextVariable("UPGRADE_VALUE", upgradeValue);
                    callbackArgs.optionLeaveType = GameMenuOption.LeaveType.Leave;
                }),
                GameOverlays.MenuOverlayType.SettlementWithBoth, relatedObject: menuId);

            Campaign.Current.GameMenuManager.RemoveRelatedGameMenuOptions(menuId);
            AddGameMenuOptionWithRelatedObject(upgradesMenu, menuId, upgradeInfo.Name,
                new GameMenuOption.OnConditionDelegate((callbackArgs) =>
                {
                    callbackArgs.optionLeaveType = upgradeInfo.LeaveType;
                    callbackArgs.IsEnabled = true;
                    return true;
                }),
                new GameMenuOption.OnConsequenceDelegate((callbackArgs) =>
                {
                    GameMenu.SwitchToMenu(menuId);
                }), -1, false, false, menuId);

            Campaign.Current.SandBoxManager.GameStarter.AddGameMenuOption(menuId, UpgradesBuyId, UpgradesBuy.ToString(),
                new GameMenuOption.OnConditionDelegate((callbackArgs) =>
                {
                    var hasUpgrade = customizationData.HasUpgrade(upgrade);
                    callbackArgs.optionLeaveType = GameMenuOption.LeaveType.Trade;
                    callbackArgs.IsEnabled = !hasUpgrade;
                    return true;
                }),
                new GameMenuOption.OnConsequenceDelegate((callbackArgs) =>
                {
                    var canBuy = Helper.CanBuyWorkshopUpgrade(upgrade, out var cost);
                    if (canBuy)
                    {
                        InformationManager.ShowInquiry(new InquiryData(UpgradesBuy.ToString(), UpgradesBuyDesc.SetTextVariable("UP_COST", cost).ToString(),
                            true, true, GameTexts.FindText("str_accept", null).ToString(), GameTexts.FindText("str_reject", null).ToString(), () =>
                            {
                                Helper.BuyWorkshopUpgrade(workshop, upgrade);
                                callbackArgs.MenuContext.Refresh();
                            }, null), false, false);
                    }
                    else
                    {
                        InformationManager.ShowInquiry(new InquiryData(UpgradesBuy.ToString(), UpgradesBuyNoMoney.SetTextVariable("UP_COST", cost).ToString(),
                            true, false, GameTexts.FindText("str_accept", null).ToString(), null, null, null), false, false);
                    }
                }));

            Campaign.Current.SandBoxManager.GameStarter.AddGameMenuOption(menuId, UpgradesRemoveId, UpgradesRemove.ToString(),
                new GameMenuOption.OnConditionDelegate((callbackArgs) =>
                {
                    var hasUpgrade = customizationData.HasUpgrade(upgrade);
                    callbackArgs.optionLeaveType = GameMenuOption.LeaveType.Default;
                    callbackArgs.IsEnabled = hasUpgrade;
                    return true;
                }),
                new GameMenuOption.OnConsequenceDelegate((callbackArgs) =>
                {
                    InformationManager.ShowInquiry(new InquiryData(UpgradesRemove.ToString(), UpgradesRemoveDesc.ToString(),
                        true, true, GameTexts.FindText("str_accept", null).ToString(), GameTexts.FindText("str_reject", null).ToString(), () =>
                        {
                            Helper.RemoveWorkshopUpgrade(workshop, upgrade);
                            callbackArgs.MenuContext.Refresh();
                        }, null), false, false);
                }));

            Campaign.Current.SandBoxManager.GameStarter.AddGameMenuOption(menuId, menuGoBackId, MenuGoBackStr,
                new GameMenuOption.OnConditionDelegate((callbackArgs) =>
                {
                    callbackArgs.optionLeaveType = GameMenuOption.LeaveType.Leave;
                    callbackArgs.IsEnabled = true;
                    return true;
                }),
                new GameMenuOption.OnConsequenceDelegate((callbackArgs) =>
                {
                    GameMenu.SwitchToMenu(upgradesMenu.StringId);
                }));
        }

        public SettlementCustomizationData GetSettlementCustomizationData(Settlement settlement)
        {
            if (!SettlementDataDict.TryGetValue(settlement, out var result))
            {
                result = new SettlementCustomizationData();
                SettlementDataDict.Add(settlement, result);
            }

            return result;
        }

        public WorkshopCustomizationData GetWorkshopCustomizationData(Workshop workshop)
        {
            if (!WorkshopDataDict.TryGetValue(workshop, out var result))
            {
                result = new WorkshopCustomizationData();
                WorkshopDataDict.Add(workshop, result);
            }

            return result;
        }

        private string GetWorkshopUpgrades(Workshop workshop)
        {
            if (!workshop.Owner.IsHumanPlayerCharacter)
            {
                return "";
            }

            var customizationData = Instance.GetWorkshopCustomizationData(workshop);
            if (customizationData == null)
            {
                return "";
            }

            var names = new List<string>();
            foreach (var kvp in UpgradeNames)
            {
                if (customizationData.HasUpgrade(kvp.Key))
                {
                    names.Add(kvp.Value.Name.ToString());
                }
            }

            return names.Count > 0 ? string.Join(", ", names) : UpgradesNone.ToString();
        }

        // Temporary fix until game code is fixed, hopefully
        public static void AddGameMenuOptionWithRelatedObject(GameMenu gameMenu, string optionId, TextObject optionText, GameMenuOption.OnConditionDelegate condition, GameMenuOption.OnConsequenceDelegate consequence, int index = -1, bool isLeave = false, bool isRepeatable = false, object relatedObject = null)
        {
            typeof(GameMenu).GetMethod("AddOption", BindingFlags.NonPublic | BindingFlags.Instance, null,
                new Type[] { typeof(string), typeof(TextObject), typeof(GameMenuOption.OnConditionDelegate), typeof(GameMenuOption.OnConsequenceDelegate), typeof(int), typeof(bool), typeof(bool), typeof(object) }, null)
                .Invoke(gameMenu, new object[] { optionId, optionText, condition, consequence, index, isLeave, isRepeatable, relatedObject });
        }
    }

    public class WASaveDefiner : SaveableTypeDefiner
    {
        public WASaveDefiner() : base(234692234) { }

        protected override void DefineClassTypes()
        {
            AddClassDefinition(typeof(SettlementCustomizationData), 1);
            AddClassDefinition(typeof(WorkshopCustomizationData), 2);
            AddClassDefinition(typeof(ItemPriceData), 3);
        }

        protected override void DefineContainerDefinitions()
        {
            ConstructContainerDefinition(typeof(Dictionary<Settlement, SettlementCustomizationData>));
            ConstructContainerDefinition(typeof(Dictionary<Workshop, WorkshopCustomizationData>));
            ConstructContainerDefinition(typeof(Dictionary<ItemObject, ItemPriceData>));
        }
    }
}