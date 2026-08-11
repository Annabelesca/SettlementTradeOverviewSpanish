using RimWorld;

namespace SettlementTradeOverview.Integration.Settlements
{
    // ReSharper disable once InconsistentNaming
    public sealed class WorldObjectCompProperties_SettlementTradeOverview : WorldObjectCompProperties
    {
        public WorldObjectCompProperties_SettlementTradeOverview()
        {
            compClass = typeof(WorldObjectComp_SettlementTradeOverview);
        }
    }
}