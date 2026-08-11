using System;

namespace SettlementTradeOverview.Domain.Snapshots
{
    public sealed class PawnTradeDetailsSnapshot
    {
        public PawnTradeDetailsSnapshot(PawnTradeDetailKind kind, float? caravanRidingSpeedFactor = null)
        {
            if (!Enum.IsDefined(typeof(PawnTradeDetailKind), kind))
                throw new ArgumentOutOfRangeException(nameof(kind));

            if (kind == PawnTradeDetailKind.Rideable)
            {
                if (!caravanRidingSpeedFactor.HasValue || float.IsNaN(caravanRidingSpeedFactor.Value) ||
                    float.IsInfinity(caravanRidingSpeedFactor.Value) || caravanRidingSpeedFactor.Value <= 0f)
                {
                    throw new ArgumentOutOfRangeException(nameof(caravanRidingSpeedFactor));
                }
            }
            else if (caravanRidingSpeedFactor.HasValue)
            {
                throw new ArgumentException(
                    "A caravan riding speed factor is valid only for rideable pawns.",
                    nameof(caravanRidingSpeedFactor));
            }

            Kind = kind;
            CaravanRidingSpeedFactor = caravanRidingSpeedFactor;
        }

        public PawnTradeDetailKind Kind { get; }

        public float? CaravanRidingSpeedFactor { get; }
    }
}