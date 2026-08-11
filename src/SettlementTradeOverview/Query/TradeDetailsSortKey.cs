using System;
using SettlementTradeOverview.Domain.Snapshots;

namespace SettlementTradeOverview.Query
{
    internal enum TradeDetailsSortKind
    {
        PlannerRelevance,
        JoinsAsColonist,
        JoinsAsSlave,
        Rideable,
        None
    }

    internal readonly struct TradeDetailsSortKey
    {
        private TradeDetailsSortKey(TradeDetailsSortKind kind, double? numericValue)
        {
            Kind = kind;
            NumericValue = numericValue;
        }

        public TradeDetailsSortKind Kind { get; }

        public double? NumericValue { get; }

        public bool HasNumericValue =>
            NumericValue.HasValue;

        public static TradeDetailsSortKey Create(TradeEntrySnapshot entry, int? relevanceMatchCount)
        {
            if (entry == null)
                throw new ArgumentNullException(nameof(entry));

            if (relevanceMatchCount.HasValue && relevanceMatchCount.Value > 0)
            {
                return new TradeDetailsSortKey(TradeDetailsSortKind.PlannerRelevance, relevanceMatchCount.Value);
            }

            PawnTradeDetailsSnapshot pawnDetails = entry.PawnDetails;

            if (pawnDetails == null)
                return new TradeDetailsSortKey(TradeDetailsSortKind.None, null);

            switch (pawnDetails.Kind)
            {
                case PawnTradeDetailKind.None:
                    return new TradeDetailsSortKey(TradeDetailsSortKind.None, null);

                case PawnTradeDetailKind.JoinsAsColonist:
                    return new TradeDetailsSortKey(TradeDetailsSortKind.JoinsAsColonist, null);

                case PawnTradeDetailKind.JoinsAsSlave:
                    return new TradeDetailsSortKey(TradeDetailsSortKind.JoinsAsSlave, null);

                case PawnTradeDetailKind.Rideable:
                    return new TradeDetailsSortKey(
                        TradeDetailsSortKind.Rideable,
                        pawnDetails.CaravanRidingSpeedFactor.GetValueOrDefault());

                default:
                    throw new ArgumentOutOfRangeException(nameof(pawnDetails.Kind));
            }
        }
    }
}