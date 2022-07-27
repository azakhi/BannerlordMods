using HarmonyLib;
using System.Reflection;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace OldGeoCalculation
{
    public class SubModule : MBSubModuleBase
    {
        protected override void OnSubModuleLoad()
        {
            base.OnSubModuleLoad();

            Harmony harmony = new Harmony("OldGeoCalculation");

            harmony.Patch(typeof(DefaultSettlementValueModel).GetMethod("GeographicalAdvantageForFaction", BindingFlags.NonPublic | BindingFlags.Instance), null,
                new HarmonyMethod(typeof(GeoPatch).GetMethod(nameof(GeoPatch.GeographicalAdvantageForFactionPostfix))));
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

    public class GeoPatch
    {
        public static void GeographicalAdvantageForFactionPostfix(ref float __result, Settlement settlement, IFaction faction)
        {
            var distanceToMidSettlement = (faction != null) ? Campaign.Current.Models.MapDistanceModel.GetDistance(settlement, faction.FactionMidSettlement) : 0f;
            __result = MathF.Pow(1f - distanceToMidSettlement / Campaign.MapDiagonal, 0.1f);
        }
    }
}