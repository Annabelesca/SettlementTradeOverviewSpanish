namespace SettlementTradeOverview.Cache
{
    internal enum TradeInventorySnapshotCacheState
    {
        NotLoaded,
        Loading,
        Available,
        Empty,
        Unavailable,
        Partial,
        Failed
    }
}