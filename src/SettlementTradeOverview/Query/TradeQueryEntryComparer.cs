using System;
using System.Collections.Generic;
using SettlementTradeOverview.Domain.Identity;
using SettlementTradeOverview.Domain.Snapshots;

namespace SettlementTradeOverview.Query
{
    internal sealed class TradeQueryEntryComparer : IComparer<TradeQueryEntry>
    {
        private readonly TradeSortMode _mode;
        private readonly TradeSortDirection _direction;
        private readonly Func<TradeEntryIdentity, int?> _relevanceMatchCountResolver;

        public TradeQueryEntryComparer(
            TradeSortMode mode,
            TradeSortDirection direction,
            Func<TradeEntryIdentity, int?> relevanceMatchCountResolver)
        {
            if (!Enum.IsDefined(typeof(TradeSortMode), mode))
                throw new ArgumentOutOfRangeException(nameof(mode));

            if (!Enum.IsDefined(typeof(TradeSortDirection), direction))
                throw new ArgumentOutOfRangeException(nameof(direction));

            _relevanceMatchCountResolver = relevanceMatchCountResolver ??
                                           throw new ArgumentNullException(nameof(relevanceMatchCountResolver));
            _mode = mode;
            _direction = direction;
        }

        public int Compare(TradeQueryEntry x, TradeQueryEntry y)
        {
            if (ReferenceEquals(x, y))
                return 0;

            if (ReferenceEquals(x, null))
                return 1;

            if (ReferenceEquals(y, null))
                return -1;

            int comparison = ComparePrimary(x, y);

            if (comparison != 0)
                return comparison;

            comparison = CompareTieBreakers(x, y);

            if (comparison != 0)
                return comparison;

            return 0;
        }

        private int ComparePrimary(TradeQueryEntry x, TradeQueryEntry y)
        {
            switch (_mode)
            {
                case TradeSortMode.Name:
                    return ApplyDirection(CompareStrings(x.Entry.Label, y.Entry.Label));

                case TradeSortMode.Details:
                    return CompareDetails(x, y);

                case TradeSortMode.Settlement:
                    return ApplyDirection(CompareStrings(x.Trader.SettlementLabel, y.Trader.SettlementLabel));

                case TradeSortMode.Distance:
                    return CompareDistance(x.Trader.Distance, y.Trader.Distance);

                case TradeSortMode.Price:
                    return ComparePrice(x.Entry.Price, y.Entry.Price);

                case TradeSortMode.RestockTime:
                    return CompareRestock(x.Trader.Restock, y.Trader.Restock);

                case TradeSortMode.Count:
                    return ApplyDirection(x.Entry.Count.CompareTo(y.Entry.Count));

                default:
                    throw new ArgumentOutOfRangeException(nameof(_mode));
            }
        }

        private int CompareTieBreakers(TradeQueryEntry x, TradeQueryEntry y)
        {
            int comparison;

            switch (_mode)
            {
                case TradeSortMode.Name:
                    comparison = CompareStrings(x.Trader.SettlementLabel, y.Trader.SettlementLabel);
                    break;

                case TradeSortMode.Settlement:
                    comparison = CompareStrings(x.Entry.Label, y.Entry.Label);
                    break;

                case TradeSortMode.Distance:
                    comparison = CompareStrings(x.Trader.SettlementLabel, y.Trader.SettlementLabel);

                    if (comparison == 0)
                        comparison = CompareStrings(x.Entry.Label, y.Entry.Label);

                    break;

                case TradeSortMode.Details:
                case TradeSortMode.Price:
                case TradeSortMode.RestockTime:
                case TradeSortMode.Count:
                    comparison = CompareStrings(x.Entry.Label, y.Entry.Label);

                    if (comparison == 0)
                        comparison = CompareStrings(x.Trader.SettlementLabel, y.Trader.SettlementLabel);

                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(_mode));
            }

            if (comparison != 0)
                return comparison;

            comparison = CompareStrings(x.Trader.TraderIdentity.Value, y.Trader.TraderIdentity.Value);

            if (comparison != 0)
                return comparison;

            return CompareStrings(x.Entry.Identity.Value, y.Entry.Identity.Value);
        }

        private int CompareDetails(TradeQueryEntry x, TradeQueryEntry y)
        {
            int? xMatchCount = _relevanceMatchCountResolver(x.Entry.Identity);
            int? yMatchCount = _relevanceMatchCountResolver(y.Entry.Identity);

            var xKey = TradeDetailsSortKey.Create(x.Entry, xMatchCount);
            var yKey = TradeDetailsSortKey.Create(y.Entry, yMatchCount);

            int kindComparison = xKey.Kind.CompareTo(yKey.Kind);

            if (kindComparison != 0)
                return kindComparison;

            if (!xKey.HasNumericValue || !yKey.HasNumericValue)
                return 0;

            return ApplyDirection(
                xKey.NumericValue.GetValueOrDefault().CompareTo(yKey.NumericValue.GetValueOrDefault()));
        }

        private int CompareDistance(TradeDistance x, TradeDistance y)
        {
            int availabilityComparison = GetDistanceAvailabilityRank(x).CompareTo(GetDistanceAvailabilityRank(y));

            if (availabilityComparison != 0)
                return availabilityComparison;

            if (!x.HasTileDistance)
                return 0;

            int distanceComparison = ApplyDirection(x.Tiles.GetValueOrDefault().CompareTo(y.Tiles.GetValueOrDefault()));

            if (distanceComparison != 0)
                return distanceComparison;

            return GetRouteStateRank(x.RouteState).CompareTo(GetRouteStateRank(y.RouteState));
        }

        private int ComparePrice(TradePrice x, TradePrice y)
        {
            int xRank = GetPriceRank(x.State);
            int yRank = GetPriceRank(y.State);
            int rankComparison = xRank.CompareTo(yRank);

            if (rankComparison != 0)
                return rankComparison;

            if (!x.HasValue)
                return 0;

            return ApplyDirection(x.Value.GetValueOrDefault().CompareTo(y.Value.GetValueOrDefault()));
        }

        private int CompareRestock(TradeRestock x, TradeRestock y)
        {
            int xRank = GetRestockRank(x.State);
            int yRank = GetRestockRank(y.State);
            int rankComparison = xRank.CompareTo(yRank);

            if (rankComparison != 0)
                return rankComparison;

            if (x.State != TradeRestockState.Scheduled)
                return 0;

            return ApplyDirection(
                x.NextRestockTick.GetValueOrDefault().CompareTo(y.NextRestockTick.GetValueOrDefault()));
        }

        private int ApplyDirection(int comparison)
        {
            if (comparison == 0)
                return 0;

            return _direction == TradeSortDirection.Ascending ? comparison : -comparison;
        }

        private static int CompareStrings(string x, string y)
        {
            int comparison = StringComparer.OrdinalIgnoreCase.Compare(x, y);

            if (comparison != 0)
                return comparison;

            return StringComparer.Ordinal.Compare(x, y);
        }

        private static int GetDistanceAvailabilityRank(TradeDistance distance)
        {
            return distance.HasTileDistance ? 0 : 1;
        }

        private static int GetRouteStateRank(TradeRouteState routeState)
        {
            switch (routeState)
            {
                case TradeRouteState.Reachable:
                    return 0;

                case TradeRouteState.Unreachable:
                    return 1;

                case TradeRouteState.Unavailable:
                    return 2;

                default:
                    throw new ArgumentOutOfRangeException(nameof(routeState));
            }
        }

        private static int GetPriceRank(TradePriceState state)
        {
            switch (state)
            {
                case TradePriceState.Negotiated:
                case TradePriceState.MarketValueFallback:
                    return 0;

                case TradePriceState.Unavailable:
                    return 1;

                default:
                    throw new ArgumentOutOfRangeException(nameof(state));
            }
        }

        private static int GetRestockRank(TradeRestockState state)
        {
            switch (state)
            {
                case TradeRestockState.Scheduled:
                    return 0;

                case TradeRestockState.PendingGeneration:
                    return 1;

                case TradeRestockState.Unavailable:
                    return 2;

                default:
                    throw new ArgumentOutOfRangeException(nameof(state));
            }
        }
    }
}