using System;
using SettlementTradeOverview.Domain.Eligibility;
using SettlementTradeOverview.Domain.Snapshots;
using SettlementTradeOverview.Integration.Context;
using SettlementTradeOverview.Integration.Discovery;

namespace SettlementTradeOverview.Integration.Snapshots
{
    internal static class TradeDistanceAdapter
    {
        public static TradeDistance Create(DiscoveredTraderSource source, PlayerTradeContext context)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            if (context == null)
                throw new ArgumentNullException(nameof(context));

            int? eligibilityDistance = source.EligibilityFacts.DistanceInTiles;
            int? calculatedDistance = eligibilityDistance.HasValue ? null : TryCalculateDistance(source, context);

            SettlementReachabilityState reachability = source.EligibilityFacts.Reachability;

            if (reachability == SettlementReachabilityState.Unavailable)
                reachability = TryCalculateReachability(source, context);

            return TradeDistanceStateResolver.Resolve(eligibilityDistance, calculatedDistance, reachability);
        }

        private static int? TryCalculateDistance(DiscoveredTraderSource source, PlayerTradeContext context)
        {
            try
            {
                int settlementTile = source.Settlement.Tile.tileId;

                if (settlementTile < 0 || context.OriginTile < 0)
                    return null;

                float distance = context.WorldGrid.ApproxDistanceInTiles(settlementTile, context.OriginTile);

                if (float.IsNaN(distance) || float.IsInfinity(distance) || distance < 0f || distance > int.MaxValue)
                {
                    return null;
                }

                return (int)distance;
            }
            catch
            {
                return null;
            }
        }

        private static SettlementReachabilityState TryCalculateReachability(
            DiscoveredTraderSource source,
            PlayerTradeContext context)
        {
            try
            {
                int settlementTile = source.Settlement.Tile.tileId;

                if (settlementTile < 0 || context.OriginTile < 0)
                    return SettlementReachabilityState.Unavailable;

                int traversalDistance = context.WorldGrid.TraversalDistanceBetween(
                    settlementTile,
                    context.OriginTile,
                    false);

                if (traversalDistance == int.MaxValue)
                    return SettlementReachabilityState.Unreachable;

                return traversalDistance < 0
                    ? SettlementReachabilityState.Unavailable
                    : SettlementReachabilityState.Reachable;
            }
            catch
            {
                return SettlementReachabilityState.Unavailable;
            }
        }
    }

    internal static class TradeDistanceStateResolver
    {
        public static TradeDistance Resolve(
            int? eligibilityDistance,
            int? calculatedDistance,
            SettlementReachabilityState reachability)
        {
            int? distance = IsValidDistance(eligibilityDistance) ? eligibilityDistance :
                IsValidDistance(calculatedDistance) ? calculatedDistance : null;

            if (!distance.HasValue)
                return TradeDistance.Unavailable;

            switch (reachability)
            {
                case SettlementReachabilityState.Reachable:
                    return TradeDistance.Reachable(distance.Value);

                case SettlementReachabilityState.Unreachable:
                    return TradeDistance.Unreachable(distance.Value);

                case SettlementReachabilityState.Unavailable:
                    return TradeDistance.WithUnavailableRoute(distance.Value);

                default:
                    throw new ArgumentOutOfRangeException(nameof(reachability));
            }
        }

        private static bool IsValidDistance(int? distance)
        {
            return distance.HasValue && distance.Value >= 0;
        }
    }
}