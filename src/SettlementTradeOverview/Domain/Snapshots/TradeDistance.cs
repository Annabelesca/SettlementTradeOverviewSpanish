using System;

namespace SettlementTradeOverview.Domain.Snapshots
{
    public enum TradeRouteState
    {
        Unavailable,
        Reachable,
        Unreachable
    }

    public readonly struct TradeDistance
    {
        private TradeDistance(int? tiles, TradeRouteState routeState)
        {
            if (tiles.HasValue && tiles.Value < 0)
                throw new ArgumentOutOfRangeException(nameof(tiles));

            if (!Enum.IsDefined(typeof(TradeRouteState), routeState))
                throw new ArgumentOutOfRangeException(nameof(routeState));

            Tiles = tiles;
            RouteState = routeState;
        }

        public static TradeDistance Unavailable =>
            new TradeDistance(null, TradeRouteState.Unavailable);

        public int? Tiles { get; }

        public TradeRouteState RouteState { get; }

        public bool HasTileDistance =>
            Tiles.HasValue;

        public static TradeDistance Reachable(int tiles)
        {
            return new TradeDistance(tiles, TradeRouteState.Reachable);
        }

        public static TradeDistance Unreachable(int tiles)
        {
            return new TradeDistance(tiles, TradeRouteState.Unreachable);
        }

        public static TradeDistance WithUnavailableRoute(int tiles)
        {
            return new TradeDistance(tiles, TradeRouteState.Unavailable);
        }
    }
}