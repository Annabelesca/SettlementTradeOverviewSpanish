using System;

namespace SettlementTradeOverview.Domain.Snapshots
{
    public enum TradeRestockState
    {
        Unavailable,
        Scheduled,
        PendingGeneration
    }

    public readonly struct TradeRestock
    {
        private TradeRestock(TradeRestockState state, int? nextRestockTick, TradeRestockMoment? expectedMoment)
        {
            State = state;
            NextRestockTick = nextRestockTick;
            ExpectedMoment = expectedMoment;
        }

        public static TradeRestock Unavailable =>
            new TradeRestock(TradeRestockState.Unavailable, null, null);

        public static TradeRestock PendingGeneration =>
            new TradeRestock(TradeRestockState.PendingGeneration, null, null);

        public TradeRestockState State { get; }

        public int? NextRestockTick { get; }

        public TradeRestockMoment? ExpectedMoment { get; }

        public bool HasNextRestockTick =>
            NextRestockTick.HasValue;

        public bool HasExpectedMoment =>
            ExpectedMoment.HasValue;

        public static TradeRestock Scheduled(int nextRestockTick, TradeRestockMoment? expectedMoment = null)
        {
            if (nextRestockTick < 0)
                throw new ArgumentOutOfRangeException(nameof(nextRestockTick));

            return new TradeRestock(TradeRestockState.Scheduled, nextRestockTick, expectedMoment);
        }
    }
}