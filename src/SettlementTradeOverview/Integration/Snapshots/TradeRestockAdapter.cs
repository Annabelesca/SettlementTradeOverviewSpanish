using System;
using RimWorld;
using SettlementTradeOverview.Domain.Snapshots;
using SettlementTradeOverview.Integration.Context;
using SettlementTradeOverview.Integration.Discovery;
using UnityEngine;

namespace SettlementTradeOverview.Integration.Snapshots
{
    internal static class TradeRestockAdapter
    {
        public static TradeRestock Create(DiscoveredTraderSource source, PlayerTradeContext context)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            if (context == null)
                throw new ArgumentNullException(nameof(context));

            if (!(source.Trader is ITraderRestockingInfoProvider provider))
                return TradeRestock.Unavailable;

            try
            {
                int nextRestockTick = provider.NextRestockTick;
                TradeRestockMoment? expectedMoment = TryCreateExpectedMoment(context, nextRestockTick);

                return TradeRestockStateResolver.Resolve(true, nextRestockTick, expectedMoment);
            }
            catch
            {
                return TradeRestock.Unavailable;
            }
        }

        private static TradeRestockMoment? TryCreateExpectedMoment(PlayerTradeContext context, int nextRestockTick)
        {
            if (nextRestockTick < 0)
                return null;

            try
            {
                int absoluteTick = GenDate.TickGameToAbs(nextRestockTick);
                Vector2 longLat = context.WorldGrid.LongLatOf(context.OriginMap.Tile);

                return new TradeRestockMoment(absoluteTick, longLat.x, longLat.y);
            }
            catch
            {
                return null;
            }
        }
    }

    internal static class TradeRestockStateResolver
    {
        public static TradeRestock Resolve(
            bool hasProvider,
            int nextRestockTick,
            TradeRestockMoment? expectedMoment = null)
        {
            if (!hasProvider)
                return TradeRestock.Unavailable;

            if (nextRestockTick >= 0)
                return TradeRestock.Scheduled(nextRestockTick, expectedMoment);

            return nextRestockTick == -1 ? TradeRestock.PendingGeneration : TradeRestock.Unavailable;
        }
    }
}