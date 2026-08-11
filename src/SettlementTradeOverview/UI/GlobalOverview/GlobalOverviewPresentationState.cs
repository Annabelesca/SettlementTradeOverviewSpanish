using System;
using SettlementTradeOverview.Cache;

namespace SettlementTradeOverview.UI.GlobalOverview
{
    internal enum GlobalOverviewPresentationState
    {
        Loading,
        Available,
        Empty,
        Unavailable,
        Partial,
        Error
    }

    internal static class GlobalOverviewPresentationStateResolver
    {
        public static GlobalOverviewPresentationState Resolve(
            bool isSnapshotRequestPending,
            bool hasUnexpectedLoadFailure,
            TradeInventorySnapshotCacheState cacheState)
        {
            if (hasUnexpectedLoadFailure)
                return GlobalOverviewPresentationState.Error;

            if (isSnapshotRequestPending)
                return GlobalOverviewPresentationState.Loading;

            switch (cacheState)
            {
                case TradeInventorySnapshotCacheState.NotLoaded:
                case TradeInventorySnapshotCacheState.Loading:
                    return GlobalOverviewPresentationState.Loading;

                case TradeInventorySnapshotCacheState.Available:
                    return GlobalOverviewPresentationState.Available;

                case TradeInventorySnapshotCacheState.Empty:
                    return GlobalOverviewPresentationState.Empty;

                case TradeInventorySnapshotCacheState.Unavailable:
                    return GlobalOverviewPresentationState.Unavailable;

                case TradeInventorySnapshotCacheState.Partial:
                    return GlobalOverviewPresentationState.Partial;

                case TradeInventorySnapshotCacheState.Failed:
                    return GlobalOverviewPresentationState.Error;

                default:
                    throw new ArgumentOutOfRangeException(nameof(cacheState));
            }
        }
    }
}