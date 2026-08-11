using System;
using RimWorld;
using SettlementTradeOverview.Domain.Snapshots;
using Verse;

namespace SettlementTradeOverview.Integration.Snapshots
{
    internal static class TradePriceAdapter
    {
        public static TradePrice Create(Tradeable tradeable, ITrader trader, Pawn negotiator)
        {
            if (tradeable == null || trader == null || !tradeable.IsThing)
            {
                return TradePrice.Unavailable;
            }

            if (negotiator != null && tradeable.AnyThing != null)
            {
                try
                {
                    using (new TradeSessionScope(negotiator, trader, false))
                    {
                        return TradePriceStateResolver.FromNegotiatedValue(
                            tradeable.GetPriceFor(TradeAction.PlayerBuys));
                    }
                }
                catch (Exception)
                {
                    return TradePrice.Unavailable;
                }
            }

            Thing representative = tradeable.AnyThing;

            if (representative != null && TryReadMarketValue(representative, out float marketValue) &&
                TradePriceStateResolver.IsValidValue(marketValue))
            {
                return TradePrice.MarketValueFallback(marketValue);
            }

            ThingDef definition = tradeable.ThingDef ?? representative?.def;

            if (definition != null && TryReadBaseMarketValue(definition, out float baseMarketValue))
            {
                return TradePriceStateResolver.FromMarketValue(baseMarketValue);
            }

            return TradePrice.Unavailable;
        }

        private static bool TryReadMarketValue(Thing thing, out float marketValue)
        {
            try
            {
                marketValue = thing.MarketValue;
                return true;
            }
            catch (Exception)
            {
                marketValue = 0f;
                return false;
            }
        }

        private static bool TryReadBaseMarketValue(ThingDef definition, out float marketValue)
        {
            try
            {
                marketValue = definition.BaseMarketValue;
                return true;
            }
            catch (Exception)
            {
                marketValue = 0f;
                return false;
            }
        }

        private readonly struct TradeSessionScope : IDisposable
        {
            private readonly Pawn _previousNegotiator;
            private readonly ITrader _previousTrader;
            private readonly bool _previousGiftMode;

            public TradeSessionScope(Pawn negotiator, ITrader trader, bool giftMode)
            {
                _previousNegotiator = TradeSession.playerNegotiator;
                _previousTrader = TradeSession.trader;
                _previousGiftMode = TradeSession.giftMode;

                TradeSession.playerNegotiator = negotiator;
                TradeSession.trader = trader;
                TradeSession.giftMode = giftMode;
            }

            public void Dispose()
            {
                TradeSession.playerNegotiator = _previousNegotiator;
                TradeSession.trader = _previousTrader;
                TradeSession.giftMode = _previousGiftMode;
            }
        }
    }

    internal static class TradePriceStateResolver
    {
        public static TradePrice FromNegotiatedValue(float? value)
        {
            return value.HasValue && IsValidValue(value.Value)
                ? TradePrice.Negotiated(value.Value)
                : TradePrice.Unavailable;
        }

        public static TradePrice FromMarketValue(float? value)
        {
            return value.HasValue && IsValidValue(value.Value)
                ? TradePrice.MarketValueFallback(value.Value)
                : TradePrice.Unavailable;
        }

        public static bool IsValidValue(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value) && value >= 0f;
        }
    }
}