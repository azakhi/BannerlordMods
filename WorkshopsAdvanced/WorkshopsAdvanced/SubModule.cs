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
            harmony.Patch(typeof(DefaultWorkshopModel).GetMethod("GetMaxWorkshopCountForTier", BindingFlags.Public | BindingFlags.Instance), null,
                new HarmonyMethod(typeof(WorkshopBehaviourPatch).GetMethod(nameof(WorkshopBehaviourPatch.GetMaxWorkshopCountForTierPostfix))));
            harmony.Patch(typeof(Production).GetMethod("get_ConversionSpeed", BindingFlags.Public | BindingFlags.Instance), null,
                new HarmonyMethod(typeof(WorkshopBehaviourPatch).GetMethod(nameof(WorkshopBehaviourPatch.ConversionSpeedPostfix))));

            harmony.Patch(typeof(DefaultClanFinanceModel).GetMethod("CalculateClanExpensesInternal", BindingFlags.Public | BindingFlags.Instance), null,
                new HarmonyMethod(typeof(WorkshopBehaviourPatch).GetMethod(nameof(WorkshopBehaviourPatch.CalculateClanExpensesInternalPostfix))));

            harmony.Patch(typeof(WorkshopsCampaignBehavior).GetMethod("RunTownWorkshop", BindingFlags.NonPublic | BindingFlags.Instance),
                new HarmonyMethod(typeof(WorkshopBehaviourPatch).GetMethod(nameof(WorkshopBehaviourPatch.RunTownWorkshopPrefix))));
            harmony.Patch(typeof(WorkshopsCampaignBehavior).GetMethod("DoProduction", BindingFlags.NonPublic | BindingFlags.Instance),
                new HarmonyMethod(typeof(WorkshopBehaviourPatch).GetMethod(nameof(WorkshopBehaviourPatch.DoProductionPrefix))));
            harmony.Patch(typeof(WorkshopsCampaignBehavior).GetMethod("DetermineTownHasSufficientInputs", BindingFlags.NonPublic | BindingFlags.Static), null,
                new HarmonyMethod(typeof(WorkshopBehaviourPatch).GetMethod(nameof(WorkshopBehaviourPatch.DetermineTownHasSufficientInputsPostfix))));
            harmony.Patch(typeof(WorkshopsCampaignBehavior).GetMethod("ProduceOutput", BindingFlags.NonPublic | BindingFlags.Static),
                new HarmonyMethod(typeof(WorkshopBehaviourPatch).GetMethod(nameof(WorkshopBehaviourPatch.ProduceOutputPrefix))));
            harmony.Patch(typeof(WorkshopsCampaignBehavior).GetMethod("ConsumeInput", BindingFlags.NonPublic | BindingFlags.Static),
                new HarmonyMethod(typeof(WorkshopBehaviourPatch).GetMethod(nameof(WorkshopBehaviourPatch.ConsumeInputPrefix))));

            harmony.Patch(typeof(CaravansCampaignBehavior).GetMethod("OnSettlementEntered", BindingFlags.Public | BindingFlags.Instance),
                new HarmonyMethod(typeof(WorkshopBehaviourPatch).GetMethod(nameof(WorkshopBehaviourPatch.OnSettlementEnteredPrefix))));
            harmony.Patch(typeof(SellItemsAction).GetMethod("Apply", BindingFlags.Public | BindingFlags.Static), null,
                new HarmonyMethod(typeof(WorkshopBehaviourPatch).GetMethod(nameof(WorkshopBehaviourPatch.ApplyPostfix))));
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

    public class WorkshopBehaviourPatch
    {
        private static Workshop? _workshop;

        public static void ExpensePostfix(Workshop __instance, ref int __result)
        {
            __result = MathF.Round(__result * Helper.GetWorkshopWageMultiplier(__instance));
        }

        public static void GetMaxWorkshopCountForTierPostfix(ref int __result, int tier)
        {
            __result += Helper.GetExtraWorkshopCountForTier(tier);
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
                goldChange.Add(-totalRent, new TextObject("{=WA_Warehouse_Rent_Tooltip}Warehouse Rent ({RENTEDCOUNT})", new Dictionary<string, object>() { { "RENTEDCOUNT", rentedCount } }));
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

        public static bool ProduceOutputPrefix(ItemObject outputItem, Town town, Workshop workshop, int count, bool doNotEffectCapital)
        {
            if (!workshop.Owner.IsHumanPlayerCharacter)
            {
                return true;
            }

            if (!outputItem.IsTradeGood)
            {
                return true;
            }

            var settlementCustomizationData = WorkshopsAdvancedCampaignBehaviour.Instance.GetSettlementCustomizationData(workshop.Settlement);
            if (!settlementCustomizationData.IsRentingWarehouse)
            {
                return true;
            }

            settlementCustomizationData.Warehouse.AddToCounts(outputItem, count);

            var workshopCustomizationData = WorkshopsAdvancedCampaignBehaviour.Instance.GetWorkshopCustomizationData(workshop);
            if (workshopCustomizationData.IsSellingToMarket)
            {
                var isEnabled = MySettings.Instance?.SmartEnable ?? false;
                if (isEnabled)
                {
                    Helper.CheckAndSellWarehouseItem(workshop, outputItem, count);
                }
                else
                {
                    Helper.SellFromWarehouseToTown(settlementCustomizationData.Warehouse, workshop, outputItem, count, doNotEffectCapital);
                }
            }

            CampaignEventDispatcher.Instance.OnItemProduced(outputItem, town.Owner.Settlement, count);
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

            Helper.CheckAndBuyInput(productionInput, workshop, doNotEffectCapital);
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

            Helper.CheckAndSellWorkshopOutputs(settlement, null);
            return true;
        }

        public static void ApplyPostfix(PartyBase receiverParty, PartyBase payerParty, ItemRosterElement subject, int number, Settlement currentSettlement = null)
        {
            if (subject.EquipmentElement.Item == null || !subject.EquipmentElement.Item.IsTradeGood || receiverParty == null || !receiverParty.IsSettlement)
            {
                return;
            }

            Helper.CheckAndSellWorkshopOutputs(receiverParty.Settlement, subject.EquipmentElement.Item.ItemCategory);
        }
    }

    public static class Helper
    {
        public static float GetWorkshopWageMultiplier(Workshop workshop)
        {
            if (workshop.Owner.IsHumanPlayerCharacter)
            {
                var lowWage = MySettings.Instance?.WorkforceLowWage ?? 0.6f;
                var highWage = MySettings.Instance?.WorkforceHighWage ?? 2f;
                var maxWage = MySettings.Instance?.WorkforceMaxWage ?? 3f;

                var customizationData = WorkshopsAdvancedCampaignBehaviour.Instance.GetWorkshopCustomizationData(workshop);
                var level = customizationData.WorkforceLevel;
                if (level < 0) return lowWage;
                if (level == 1) return highWage;
                if (level > 1) return maxWage;
            }

            return 1f;
        }

        public static int GetExtraWorkshopCountForTier(int tier)
        {
            var extraStartingCount = MySettings.Instance?.ExtraStartingCount ?? 0;
            var extraCountPerTier = MySettings.Instance?.ExtraCountPerTier ?? 0;

            return extraStartingCount + (extraCountPerTier * tier);
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

        public static void CheckAndSellWarehouseItem(Workshop workshop, ItemObject item, int amount)
        {
            var isPriceGood = IsPriceGood(workshop.Settlement, item);
            if (!isPriceGood)
            {
                return;
            }

            var customizationData = WorkshopsAdvancedCampaignBehaviour.Instance.GetSettlementCustomizationData(workshop.Settlement);
            var extraSellCount = MySettings.Instance?.SmartExtraSell ?? 0;
            var available = customizationData.Warehouse.GetItemNumber(item);
            var toSellCount = MathF.Min(available, amount + extraSellCount);
            if (toSellCount > 0)
            {
                SellFromWarehouseToTown(customizationData.Warehouse, workshop, item, toSellCount);
            }
        }

        public static void CheckAndSellWorkshopOutputs(Settlement settlement, ItemCategory? itemCategory)
        {
            var customizationData = WorkshopsAdvancedCampaignBehaviour.Instance.GetSettlementCustomizationData(settlement);
            if (!customizationData.IsRentingWarehouse)
            {
                return;
            }

            var itemIndex = GetSellableOutputIndexFromWarehouse(settlement, itemCategory, true, out var workshopIndex);
            if (itemIndex >= 0)
            {
                var isPriceGood = IsPriceGood(settlement, customizationData.Warehouse.GetItemAtIndex(itemIndex));
                if (!isPriceGood)
                {
                    return;
                }

                var item = customizationData.Warehouse.GetItemAtIndex(itemIndex);
                var sellCount = MySettings.Instance?.SmartSellOnChange ?? 0;
                var available = customizationData.Warehouse.GetItemNumber(item);
                var toSellCount = MathF.Min(available, sellCount);
                if (toSellCount > 0)
                {
                    SellFromWarehouseToTown(customizationData.Warehouse, settlement.Town.Workshops[workshopIndex], item, toSellCount);
                }
            }
        }

        public static void SellFromWarehouseToTown(ItemRoster warehouse, Workshop workshop, ItemObject item, int amount, bool doNotEffectCapital = false)
        {
            var town = workshop.Settlement.Town;
            town.Owner.ItemRoster.AddToCounts(item, amount);
            warehouse.AddToCounts(item, -amount);
            if (!doNotEffectCapital)
            {
                var totalCost = amount * town.GetItemPrice(item, null, false);
                town.ChangeGold(-totalCost);
                workshop.ChangeGold(totalCost);
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

        public static int GetSellableOutputIndexFromWarehouse(Settlement settlement, ItemCategory? itemCategory, bool onlyTradeGoods, out int workshopIndex)
        {
            workshopIndex = -1;
            var customizationData = WorkshopsAdvancedCampaignBehaviour.Instance.GetSettlementCustomizationData(settlement);
            if (!customizationData.IsRentingWarehouse)
            {
                return -1;
            }

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
                if (!workshopCustomizationData.IsSellingToMarket)
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
                                if (item.ItemCategory == output.Item1 && (!onlyTradeGoods || item.IsTradeGood))
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

        public static bool IsPriceGood(Settlement settlement, ItemObject item)
        {
            if (!item.IsTradeGood)
            {
                // Non-trade items should never be part of smart selling
                return false;
            }

            var customizationData = WorkshopsAdvancedCampaignBehaviour.Instance.GetSettlementCustomizationData(settlement);
            if (customizationData.PriceDataDict == null)
            {
                customizationData.PriceDataDict = new Dictionary<ItemObject, ItemPriceData>();
            }

            var itemPrice = settlement.Town.GetItemPrice(item, null, false);
            if (!customizationData.PriceDataDict.TryGetValue(item, out var priceData))
            {
                priceData = new ItemPriceData();
                customizationData.PriceDataDict.Add(item, priceData);
            }

            priceData.RegisterPrice(itemPrice);
            var averagePrice = priceData.AveragePrice;

            // Leave fail returns to after price calculation so price is tracked even when Smart Workshops isn't enabled

            var stock = customizationData.Warehouse.GetItemNumber(item);
            if (stock <= 0)
            {
                return false;
            }

            var isEnabled = MySettings.Instance?.SmartEnable ?? false;
            if (!isEnabled)
            {
                return false;
            }

            var pricePercentage = averagePrice > 0.01f ? itemPrice / averagePrice : float.MaxValue;

            var minSellPercentage = (MySettings.Instance?.SmartMinSell ?? 0) / 100f;
            if (pricePercentage < minSellPercentage)
            {
                return false;
            }

            var maxSellPercentage = (MySettings.Instance?.SmartMaxSell ?? 0) / 100f;
            maxSellPercentage = MathF.Max(maxSellPercentage, minSellPercentage);
            if (pricePercentage >= maxSellPercentage)
            {
                return true;
            }

            var minOutputStock = (MySettings.Instance?.SmartMinOutput ?? 0) / itemPrice;
            var maxOutputStock = (MySettings.Instance?.SmartMaxOutput ?? 0) / itemPrice;

            var desiredPercentage = stock >= maxOutputStock ? minSellPercentage : maxSellPercentage;
            if (maxOutputStock > minOutputStock && stock > minOutputStock && stock < maxOutputStock && maxSellPercentage > minSellPercentage)
            {
                var stockDiff = stock - minOutputStock;
                var targetStockDiff = maxOutputStock - minOutputStock;
                var percentageDiff = maxSellPercentage - minSellPercentage;
                desiredPercentage = minSellPercentage + (percentageDiff * (1f - (stockDiff / (float)targetStockDiff)));
            }

            var result = pricePercentage >= desiredPercentage;
            //DisplayWarning("Item: " + item.Name + ", Stock: " + stock +  ", Pr: " + itemPrice + ", Avg: " + averagePrice.ToString("0.00") + ", PP: " + pricePercentage.ToString("0.00") + ", DP: " + desiredPercentage.ToString("0.00") + ", Result: " + result);
            return result;
        }

        public static void CheckAndBuyInput(ItemCategory productionInput, Workshop workshop, bool doNotEffectCapital = false)
        {
            var isEnabled = MySettings.Instance?.SmartEnable ?? false;
            if (!isEnabled)
            {
                return;
            }

            var customizationData = WorkshopsAdvancedCampaignBehaviour.Instance.GetSettlementCustomizationData(workshop.Settlement);
            if (!customizationData.IsRentingWarehouse)
            {
                return;
            }

            var workshopCustomizationData = WorkshopsAdvancedCampaignBehaviour.Instance.GetWorkshopCustomizationData(workshop);
            if (!workshopCustomizationData.IsBuyingFromMarket)
            {
                return;
            }

            var townRoster = workshop.Settlement.Town.Owner.ItemRoster;
            var targetStock = MySettings.Instance?.SmartStock ?? 0;
            var extraBuyCount = MySettings.Instance?.SmartExtraBuy ?? 0;
            var inputIndex = customizationData.Warehouse.FindIndex((ItemObject x) => x.ItemCategory == productionInput);
            var currentStock = inputIndex >= 0 ? customizationData.Warehouse.GetElementNumber(inputIndex) : 0;
            var townItemIndex = townRoster.FindIndex((ItemObject x) => x.ItemCategory == productionInput);
            var available = townItemIndex >= 0 ? townRoster.GetElementNumber(townItemIndex) : 0;

            if (currentStock >= targetStock || available <= 0)
            {
                return;
            }

            var toBuyCount = MathF.Min(available, extraBuyCount + 1);
            var townItem = townRoster.GetItemAtIndex(townItemIndex);
            var cost = toBuyCount * workshop.Settlement.Town.GetItemPrice(townItem, null, false);
            if (toBuyCount <= 0 || (!doNotEffectCapital && cost > workshop.Capital))
            {
                return;
            }

            townRoster.AddToCounts(townItem, -toBuyCount);
            customizationData.Warehouse.AddToCounts(townItem, toBuyCount);
            if (!doNotEffectCapital)
            {
                workshop.ChangeGold(-cost);
                workshop.Settlement.Town.ChangeGold(cost);
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
        public override string DisplayName => new TextObject("{=WA_Mod_Title}Workshops Advanced").ToString();
        public override string FolderName => "WorkshopsAdvanced";
        public override string FormatType => "json";

        #region String Definitions
        private const string StrGlobalGroupName = "{=WA_Settings_Global}Global Customizations";
        private const string StrExtraStartingCount = "{=WA_Settings_Extra_Starting_Count}Extra Starting Workshop Count";
        private const string StrExtraStartingCountDesc = "{=WA_Settings_Extra_Starting_Count_Desc}Additional workshop count at the start of the game. Added to the base value.";
        private const string StrExtraCountPerTier = "{=WA_Settings_Extra_Count_Per_Tier}Extra Workshop Count Per Tier";
        private const string StrExtraCountPerTierDesc = "{=WA_Settings_Extra_Count_Per_Tier_Desc}Additional workshop count per clan tier. Added to the base value.";
        private const string StrProductionMultiplier = "{=WA_Settings_Production_Mult}Production Multiplier";
        private const string StrProductionMultiplierDesc = "{=WA_Settings_Production_Mult_Desc}Production speed multiplier for workshops. Use this if you want to adjust profitability.";

        private const string StrWarehouseGroupName = "{=WA_Settings_Group_Warehouse}Warehouse";
        private const string StrWarehouseMinRent = "{=WA_Settings_Warehouse_Min_Rent}Minimum Warehouse Rent";
        private const string StrWarehouseMinRentDesc = "{=WA_Settings_Warehouse_Min_Rent_Desc}Minimum rent to be paid if the rented warehouse is empty.";
        private const string StrWarehouseMaxRent = "{=WA_Settings_Warehouse_Max_Rent}Maximum Warehouse Rent";
        private const string StrWarehouseMaxRentDesc = "{=WA_Settings_Warehouse_Max_Rent_Desc}Maximum rent to be paid if the rented warehouse is above the weight threshold.";
        private const string StrWarehouseWeightThreshold = "{=WA_Settings_Warehouse_Weight_Threshold}Weight Threshold For Max Rent";
        private const string StrWarehouseWeightThresholdDesc = "{=WA_Settings_Warehouse_Weight_Threshold_Desc}You will pay maximum rent if total weight is above this.";

        private const string StrWorkforceGroupName = "{=WA_Settings_Group_Workforce}Workshop Workforce";
        private const string StrWorkforceLowWage = "{=WA_Settings_Workforce_Low_Wage}Lowered Wage Multiplier";
        private const string StrWorkforceLowEfficinecy = "{=WA_Settings_Workforce_Low_Efficiency}Lowered Efficiency";
        private const string StrWorkforceHighWage = "{=WA_Settings_Workforce_High_Wage}High Wage Multiplier";
        private const string StrWorkforceHighEfficinecy = "{=WA_Settings_Workforce_High_Efficiency}High Efficiency";
        private const string StrWorkforceMaxWage = "{=WA_Settings_Workforce_Max_Wage}Max Wage Multiplier";
        private const string StrWorkforceMaxEfficinecy = "{=WA_Settings_Workforce_Max_Efficiency}Max Efficiency";

        private const string StrCaravanGroupName = "{=WA_Settings_Group_Caravan}Your Caravans";
        private const string StrCaravanBudget = "{=WA_Settings_Caravan_Budget}Max Caravan Budget For Workshops";
        private const string StrCaravanBudgetDesc = "{=WA_Settings_Caravan_Budget_Desc}Maximum caravan budget to be spent on buying workshop outputs when they enter the settlement.";
        private const string StrCaravanPrice = "{=WA_Settings_Caravan_Price}Price Percentage";
        private const string StrCaravanPriceDesc = "{=WA_Settings_Caravan_Price_Desc}Percentage of price your caravans will need to pay to your workshops.";

        private const string StrSmartGroupName = "{=WA_Settings_Group_Smart}Smarter Workshops";
        private const string StrSmartEnable = "{=WA_Settings_Smart_Enable}Enable";
        private const string StrSmartExtraBuy = "{=WA_Settings_Smart_Extra_Buy}Extra Buy Count";
        private const string StrSmartExtraBuyDesc = "{=WA_Settings_Smart_Extra_Buy_Desc}Additional amount to be bought from market when input stock is under threshold. Requires warehouse.";
        private const string StrSmartStock = "{=WA_Settings_Smart_Stock}Input Stock";
        private const string StrSmartStockDesc = "{=WA_Settings_Smart_Stock_Desc}Input stock threshold to determine when to buy extra from market.";
        private const string StrSmartExtraSell = "{=WA_Settings_Smart_Extra_Sell}Extra Amount To Be Sold";
        private const string StrSmartExtraSellDesc = "{=WA_Settings_Smart_Extra_Sell_Desc}Additional amount to be sold from warehouse when price is determined to be good. Requires warehouse.";
        private const string StrSmartSellOnChange = "{=WA_Settings_Smart_Sell_On_Change}Amount To Be Sold On Market Change";
        private const string StrSmartSellOnChangeDesc = "{=WA_Settings_Smart_Sell_On_Change_Desc}Amount to be sold when market change is detected and price is determined to be good. Requires warehouse.";
        private const string StrSmartMinOutput = "{=WA_Settings_Smart_Min_Output}Minimum Output Stock Value";
        private const string StrSmartMinOutputDesc = "{=WA_Settings_Smart_Min_Output_Desc}Target minimum output stock value where selling price percentage starts dropping.";
        private const string StrSmartMaxOutput = "{=WA_Settings_Smart_Max_Output}Maximum Output Stock Value";
        private const string StrSmartMaxOutputDesc = "{=WA_Settings_Smart_Max_Output_Desc}Target maximum output stock value where selling price percentage is the minimum given.";
        private const string StrSmartMinSell = "{=WA_Settings_Smart_Min_Sell}Min Required Price Percentage";
        private const string StrSmartMinSellDesc = "{=WA_Settings_Smart_Min_Sell_Desc}Output items will not be sold at any price percentage under this regardless of stock.";
        private const string StrSmartMaxSell = "{=WA_Settings_Smart_Max_Sell}Max Required Price Percentage";
        private const string StrSmartMaxSellDesc = "{=WA_Settings_Smart_Max_Sell_Desc}Output items will be sold at any price percentage above this regardless of stock.";
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
        #endregion

        #region Warehouse
        [SettingProperty(StrWarehouseMinRent, 0, 1000, RequireRestart = false, HintText = StrWarehouseMinRentDesc, Order = 1)]
        [SettingPropertyGroup(StrWarehouseGroupName, GroupOrder = 2)]
        public int WarehouseMinRent { get; set; } = 5;

        [SettingProperty(StrWarehouseMaxRent, 0, 1000, RequireRestart = false, HintText = StrWarehouseMaxRentDesc, Order = 2)]
        [SettingPropertyGroup(StrWarehouseGroupName, GroupOrder = 2)]
        public int WarehouseMaxRent { get; set; } = 30;

        [SettingProperty(StrWarehouseWeightThreshold, 0, 100000, RequireRestart = false, HintText = StrWarehouseWeightThresholdDesc, Order = 3)]
        [SettingPropertyGroup(StrWarehouseGroupName, GroupOrder = 2)]
        public int WarehouseWeightThreshold { get; set; } = 3000;
        #endregion

        #region Workforce
        [SettingProperty(StrWorkforceLowWage, 0f, 10f, RequireRestart = false, Order = 1)]
        [SettingPropertyGroup(StrWorkforceGroupName, GroupOrder = 3)]
        public float WorkforceLowWage { get; set; } = 0.5f;

        [SettingProperty(StrWorkforceLowEfficinecy, 0f, 10f, RequireRestart = false, Order = 2)]
        [SettingPropertyGroup(StrWorkforceGroupName, GroupOrder = 3)]
        public float WorkforceLowEfficinecy { get; set; } = 0.5f;

        [SettingProperty(StrWorkforceHighWage, 0f, 10f, RequireRestart = false, Order = 3)]
        [SettingPropertyGroup(StrWorkforceGroupName, GroupOrder = 3)]
        public float WorkforceHighWage { get; set; } = 3f;

        [SettingProperty(StrWorkforceHighEfficinecy, 0f, 10f, RequireRestart = false, Order = 4)]
        [SettingPropertyGroup(StrWorkforceGroupName, GroupOrder = 3)]
        public float WorkforceHighEfficinecy { get; set; } = 1.5f;

        [SettingProperty(StrWorkforceMaxWage, 0f, 10f, RequireRestart = false, Order = 5)]
        [SettingPropertyGroup(StrWorkforceGroupName, GroupOrder = 3)]
        public float WorkforceMaxWage { get; set; } = 5f;

        [SettingProperty(StrWorkforceMaxEfficinecy, 0f, 10f, RequireRestart = false, Order = 6)]
        [SettingPropertyGroup(StrWorkforceGroupName, GroupOrder = 3)]
        public float WorkforceMaxEfficinecy { get; set; } = 1.8f;
        #endregion

        #region Caravans
        [SettingProperty(StrCaravanBudget, 0, 10000, RequireRestart = false, HintText = StrCaravanBudgetDesc, Order = 1)]
        [SettingPropertyGroup(StrCaravanGroupName, GroupOrder = 4)]
        public int CaravanBudget { get; set; } = 1000;

        [SettingProperty(StrCaravanPrice, 0, 100, RequireRestart = false, HintText = StrCaravanPriceDesc, Order = 2)]
        [SettingPropertyGroup(StrCaravanGroupName, GroupOrder = 4)]
        public int CaravanPrice { get; set; } = 50;
        #endregion

        #region Smart Workshops
        [SettingProperty(StrSmartEnable, RequireRestart = false, Order = 1)]
        [SettingPropertyGroup(StrSmartGroupName, GroupOrder = 5)]
        public bool SmartEnable { get; set; } = false;

        [SettingProperty(StrSmartExtraBuy, 0, 100, RequireRestart = false, HintText = StrSmartExtraBuyDesc, Order = 2)]
        [SettingPropertyGroup(StrSmartGroupName, GroupOrder = 5)]
        public int SmartExtraBuy { get; set; } = 2;

        [SettingProperty(StrSmartStock, 0, 1000, RequireRestart = false, HintText = StrSmartStockDesc, Order = 3)]
        [SettingPropertyGroup(StrSmartGroupName, GroupOrder = 5)]
        public int SmartStock { get; set; } = 10;

        [SettingProperty(StrSmartExtraSell, 0, 100, RequireRestart = false, HintText = StrSmartExtraSellDesc, Order = 4)]
        [SettingPropertyGroup(StrSmartGroupName, GroupOrder = 5)]
        public int SmartExtraSell { get; set; } = 1;

        [SettingProperty(StrSmartSellOnChange, 0, 100, RequireRestart = false, HintText = StrSmartSellOnChangeDesc, Order = 5)]
        [SettingPropertyGroup(StrSmartGroupName, GroupOrder = 5)]
        public int SmartSellOnChange { get; set; } = 1;

        [SettingProperty(StrSmartMinOutput, 0, 30000, RequireRestart = false, HintText = StrSmartMinOutputDesc, Order = 6)]
        [SettingPropertyGroup(StrSmartGroupName, GroupOrder = 5)]
        public int SmartMinOutput { get; set; } = 0;

        [SettingProperty(StrSmartMaxOutput, 0, 30000, RequireRestart = false, HintText = StrSmartMaxOutputDesc, Order = 7)]
        [SettingPropertyGroup(StrSmartGroupName, GroupOrder = 5)]
        public int SmartMaxOutput { get; set; } = 1000;

        [SettingProperty(StrSmartMinSell, 0, 200, RequireRestart = false, HintText = StrSmartMinSellDesc, Order = 8)]
        [SettingPropertyGroup(StrSmartGroupName, GroupOrder = 5)]
        public int SmartMinSell { get; set; } = 80;

        [SettingProperty(StrSmartMaxSell, 0, 200, RequireRestart = false, HintText = StrSmartMaxSellDesc, Order = 9)]
        [SettingPropertyGroup(StrSmartGroupName, GroupOrder = 5)]
        public int SmartMaxSell { get; set; } = 120;
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
        private readonly TextObject MenuGoBack = new TextObject("{=WA_Menu_Go_Back}Go Back", null);
        private readonly TextObject ManageWorkshopsDesc = new TextObject("{=WA_Manage_Workshops_Desc}Manage owned workshops.", null);
        private readonly TextObject ManageWorkshopsMenuName = new TextObject("{=WA_Manage_Workshops}Manage Workshops", null);
        private readonly TextObject ManageWorkshopsRentWarehouse = new TextObject("{=WA_Manage_Workshops_Rent_Warehouse}Rent Warehouse", null);
        private readonly TextObject ManageWorkshopsStopRenting = new TextObject("{=WA_Manage_Workshops_Stop_Renting}Stop Renting Warehouse", null);
        private readonly TextObject ManageWorkshopsShowWarehouse = new TextObject("{=WA_Manage_Workshops_Show_Warehouse}Show Warehouse", null);
        private readonly TextObject ManageWorkshopsNotRenting = new TextObject("{=WA_Manage_Workshops_Not_Renting}Not Renting Warehouse", null);
        private readonly TextObject ManageTownWorkshopDesc = new TextObject("{=WA_Manage_Town_Workshop_Desc}Manage workshop behaviour.", null);
        private readonly TextObject ManageTownWorkshopStopWorking = new TextObject("{=WA_Manage_Town_Workshop_Stop_Working}Stop Working", null);
        private readonly TextObject ManageTownWorkshopContinueWorking = new TextObject("{=WA_Manage_Town_Workshop_Continue_Working}Continue Working", null);
        private readonly TextObject ManageTownWorkshopDoNotBuy = new TextObject("{=WA_Manage_Town_Workshop_Do_Not_Buy}Do Not Buy From Market", null);
        private readonly TextObject ManageTownWorkshopBuyFrom = new TextObject("{=WA_Manage_Town_Workshop_Buy_From}Buy From Market", null);
        private readonly TextObject ManageTownWorkshopDoNotSell = new TextObject("{=WA_Manage_Town_Workshop_Do_Not_Sell}Do Not Sell To Market", null);
        private readonly TextObject ManageTownWorkshopSellTo = new TextObject("{=WA_Manage_Town_Workshop_Sell_To}Sell To Market", null);
        private readonly TextObject ManageTownWorkshopNeedWarehouse = new TextObject("{=WA_Manage_Town_Workshop_Need_Warehouse}You need to rent a warehouse to change these.", null);
        private readonly TextObject AdjustWorkforceMenuName = new TextObject("{=WA_Adjust_Workforce_Menu_Name}Adjust Workforce", null);
        private readonly TextObject AdjustWorkforceDesc = new TextObject("{=WA_Adjust_Workforce_Desc}Adjust workforce of your workshop", null);
        private readonly TextObject AdjustWorkforceLow = new TextObject("{=WA_Adjust_Workforce_Low}Lowered", null);
        private readonly TextObject AdjustWorkforceNormal = new TextObject("{=WA_Adjust_Workforce_Normal}Normal (Default)", null);
        private readonly TextObject AdjustWorkforceHigh = new TextObject("{=WA_Adjust_Workforce_High}High", null);
        private readonly TextObject AdjustWorkforceMax = new TextObject("{=WA_Adjust_Workforce_Max}Max", null);
        private readonly TextObject AdjustWorkforceSelected = new TextObject("{=WA_Adjust_Selected}Already selected.", null);
        private readonly TextObject InquiryStopRentingTitle = new TextObject("{=WA_Inquiry_Stop_Renting}Stop Renting Warehouse", null);
        private readonly TextObject InquiryStopRentingDesc = new TextObject("{=WA_Inquiry_Stop_Renting_Desc}If you stop renting warehouse, every item in it will be sold to town. Are you sure?", null);

        private const string ManageWorkshopsId = "manage_workshops";
        private const string ManageWorkshopsRentWarehouseId = "manage_workshops_rent_warehouse";
        private const string ManageWorkshopsShowWarehouseId = "manage_workshops_show_warehouse";
        private const string ManageTownWorkshopObject = "manage_town_workshop_object";
        private const string ManageWorkshopGoBackId = "manage_workshops_go_back";
        private const string ManageTownWorkshopIdPrefix = "manage_town_workshop_";
        private const string ManageTownWorkshopProductionId = "manage_town_workshop_production";
        private const string ManageTownWorkshopBuyFromMarketId = "manage_town_workshop_buy_from_market";
        private const string ManageTownWorkshopSellToMarketId = "manage_town_workshop_sell_to_market";
        private const string ManageTownWorkshopGoBackId = "manage_town_workshop_go_back";
        private const string AdjustWorkforceId = "adjust_workforce";
        private const string AdjustWorkforceLowId = "adjust_workforce_low";
        private const string AdjustWorkforceNormalId = "adjust_workforce_normal";
        private const string AdjustWorkforceHighId = "adjust_workforce_high";
        private const string AdjustWorkforceMaxId = "adjust_workforce_max";
        private const string AdjustWorkforceGoBackId = "adjust_workforce_go_back";
        #endregion

        [SaveableField(1)]
        internal Dictionary<Settlement, SettlementCustomizationData> SettlementDataDict = new Dictionary<Settlement, SettlementCustomizationData>();

        [SaveableField(2)]
        internal Dictionary<Workshop, WorkshopCustomizationData> WorkshopDataDict = new Dictionary<Workshop, WorkshopCustomizationData>();

        public override void RegisterEvents()
        {
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

        protected void AddWorkshopMenus(CampaignGameStarter campaignGameStarter)
        {
            campaignGameStarter.AddGameMenu(ManageWorkshopsId, ManageWorkshopsDesc.ToString(),
                new OnInitDelegate((callbackArgs) =>
                {
                    AddManageWorkshopsMenuOptions(Settlement.CurrentSettlement);
                    callbackArgs.optionLeaveType = GameMenuOption.LeaveType.Leave;
                }), GameOverlays.MenuOverlayType.SettlementWithBoth);

            campaignGameStarter.AddGameMenuOption("town", ManageWorkshopsId, ManageWorkshopsMenuName.ToString(),
                new GameMenuOption.OnConditionDelegate((callbackArgs) =>
                {
                    callbackArgs.optionLeaveType = GameMenuOption.LeaveType.Submenu;
                    callbackArgs.IsEnabled = true;
                    return true;
                }), new GameMenuOption.OnConsequenceDelegate((callbackArgs) =>
                {
                    GameMenu.SwitchToMenu(ManageWorkshopsId);
                }), false, 5, false);
        }

        private void AddManageWorkshopsMenuOptions(Settlement settlement)
        {
            var settlementCustomizationData = GetSettlementCustomizationData(settlement);
            var manageWorkshopsGameMenu = GetGameMenuWithId(Campaign.Current.GameMenuManager, ManageWorkshopsId);

            if (manageWorkshopsGameMenu == null)
            {
                throw new Exception("manageWorkshopsGameMenu is null");
            }

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
                            }, null), false);
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
                Campaign.Current.SandBoxManager.GameStarter.AddGameMenu(workshopMenuId, ManageTownWorkshopDesc.ToString(),
                    new OnInitDelegate((callbackArgs) =>
                    {
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
            var workshopGameMenu = GetGameMenuWithId(Campaign.Current.GameMenuManager, workshopMenuId);

            if (workshopGameMenu == null)
            {
                throw new Exception("workshopGameMenu is null");
            }

            AddAdjustWorkforceMenu(workshopGameMenu, workshop);

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
            Campaign.Current.SandBoxManager.GameStarter.AddGameMenu(AdjustWorkforceId, AdjustWorkforceDesc.ToString(),
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
            Campaign.Current.SandBoxManager.GameStarter.AddGameMenuOption(AdjustWorkforceId, AdjustWorkforceLowId, AdjustWorkforceLow.ToString(),
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

            Campaign.Current.SandBoxManager.GameStarter.AddGameMenuOption(AdjustWorkforceId, AdjustWorkforceNormalId, AdjustWorkforceNormal.ToString(),
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

            Campaign.Current.SandBoxManager.GameStarter.AddGameMenuOption(AdjustWorkforceId, AdjustWorkforceHighId, AdjustWorkforceHigh.ToString(),
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

            Campaign.Current.SandBoxManager.GameStarter.AddGameMenuOption(AdjustWorkforceId, AdjustWorkforceMaxId, AdjustWorkforceMax.ToString(),
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

            Campaign.Current.SandBoxManager.GameStarter.AddGameMenuOption(AdjustWorkforceId, AdjustWorkforceGoBackId, MenuGoBack.ToString(),
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

        // Temporary fix until game code is fixed, hopefully
        public static void AddGameMenuOptionWithRelatedObject(GameMenu gameMenu, string optionId, TextObject optionText, GameMenuOption.OnConditionDelegate condition, GameMenuOption.OnConsequenceDelegate consequence, int index = -1, bool isLeave = false, bool isRepeatable = false, object relatedObject = null)
        {
            typeof(GameMenu).GetMethod("AddOption", BindingFlags.NonPublic | BindingFlags.Instance, null,
                new Type[] { typeof(string), typeof(TextObject), typeof(GameMenuOption.OnConditionDelegate), typeof(GameMenuOption.OnConsequenceDelegate), typeof(int), typeof(bool), typeof(bool), typeof(object) }, null)
                .Invoke(gameMenu, new object[] { optionId, optionText, condition, consequence, index, isLeave, isRepeatable, relatedObject });
        }

        // 1.7.2 fix
        public static GameMenu? GetGameMenuWithId(GameMenuManager manager, string menuId)
        {
            var result = typeof(GameMenuManager).GetMethod("GetGameMenu", BindingFlags.NonPublic | BindingFlags.Instance, null, new Type[] { typeof(string) }, null).Invoke(manager, new object[] { menuId });
            return result as GameMenu;
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