using System;
using SettlementTradeOverview.Cache;
using SettlementTradeOverview.Domain.Snapshots;

namespace SettlementTradeOverview.UI.SettlementStock
{
    internal enum SettlementStockPresentationState
    {
        Loading,
        Available,
        Empty,
        Unavailable,
        Partial,
        Error
    }

    internal static class SettlementStockPresentationStateResolver
    {
        public static SettlementStockPresentationState Resolve(
            bool isSnapshotRequestPending,
            bool hasUnexpectedRequestFailure,
            TradeInventorySnapshotCacheState cacheState,
            TraderSnapshot trader)
        {
            if (hasUnexpectedRequestFailure)
                return SettlementStockPresentationState.Error;

            if (isSnapshotRequestPending)
                return SettlementStockPresentationState.Loading;

            switch (cacheState)
            {
                case TradeInventorySnapshotCacheState.NotLoaded:
                case TradeInventorySnapshotCacheState.Loading:
                    return SettlementStockPresentationState.Loading;

                case TradeInventorySnapshotCacheState.Unavailable:
                    return SettlementStockPresentationState.Unavailable;

                case TradeInventorySnapshotCacheState.Failed:
                    return SettlementStockPresentationState.Error;

                case TradeInventorySnapshotCacheState.Available:
                case TradeInventorySnapshotCacheState.Empty:
                case TradeInventorySnapshotCacheState.Partial:
                    return ResolveTrader(trader);

                default:
                    throw new ArgumentOutOfRangeException(nameof(cacheState));
            }
        }

        private static SettlementStockPresentationState ResolveTrader(TraderSnapshot trader)
        {
            if (trader == null)
                return SettlementStockPresentationState.Unavailable;

            switch (trader.Availability)
            {
                case SnapshotAvailability.Available:
                    return SettlementStockPresentationState.Available;

                case SnapshotAvailability.Empty:
                    return SettlementStockPresentationState.Empty;

                case SnapshotAvailability.Unavailable:
                    return SettlementStockPresentationState.Unavailable;

                case SnapshotAvailability.Partial:
                    return SettlementStockPresentationState.Partial;

                case SnapshotAvailability.Failed:
                    return SettlementStockPresentationState.Error;

                default:
                    throw new ArgumentOutOfRangeException(nameof(trader));
            }
        }
    }
}