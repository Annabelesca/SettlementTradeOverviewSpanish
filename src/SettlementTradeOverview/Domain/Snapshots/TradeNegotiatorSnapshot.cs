using System;

namespace SettlementTradeOverview.Domain.Snapshots
{
    public sealed class TradeNegotiatorSnapshot
    {
        public TradeNegotiatorSnapshot(string pawnId, string label, float tradePriceImprovement)
        {
            if (string.IsNullOrWhiteSpace(pawnId))
                throw new ArgumentException("Pawn identity cannot be empty.", nameof(pawnId));

            if (string.IsNullOrWhiteSpace(label))
                throw new ArgumentException("Label cannot be empty.", nameof(label));

            if (float.IsNaN(tradePriceImprovement) || float.IsInfinity(tradePriceImprovement))
            {
                throw new ArgumentOutOfRangeException(nameof(tradePriceImprovement));
            }

            PawnId = pawnId;
            Label = label;
            TradePriceImprovement = tradePriceImprovement;
        }

        public string PawnId { get; }

        public string Label { get; }

        public float TradePriceImprovement { get; }
    }
}