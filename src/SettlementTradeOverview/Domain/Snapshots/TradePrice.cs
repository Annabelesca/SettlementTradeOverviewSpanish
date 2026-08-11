using System;

namespace SettlementTradeOverview.Domain.Snapshots
{
    public enum TradePriceState
    {
        Unavailable,
        Negotiated,
        MarketValueFallback
    }

    public readonly struct TradePrice
    {
        private TradePrice(TradePriceState state, float? value)
        {
            State = state;
            Value = value;
        }

        public static TradePrice Unavailable =>
            new TradePrice(TradePriceState.Unavailable, null);

        public TradePriceState State { get; }

        public float? Value { get; }

        public bool HasValue =>
            Value.HasValue;

        public static TradePrice Negotiated(float value)
        {
            ValidateValue(value);

            return new TradePrice(TradePriceState.Negotiated, value);
        }

        public static TradePrice MarketValueFallback(float value)
        {
            ValidateValue(value);

            return new TradePrice(TradePriceState.MarketValueFallback, value);
        }

        private static void ValidateValue(float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value) || value < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }
        }
    }
}