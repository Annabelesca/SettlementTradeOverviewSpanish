using System;
using System.Collections.Generic;
using System.Globalization;
using RimWorld;
using SettlementTradeOverview.Domain.Snapshots;
using SettlementTradeOverview.Integration.Context;
using Verse;

namespace SettlementTradeOverview.Integration.Snapshots
{
    internal readonly struct TradeNegotiatorSelection
    {
        public TradeNegotiatorSelection(Pawn negotiator, TradeNegotiatorSnapshot snapshot)
        {
            Negotiator = negotiator;
            Snapshot = snapshot;
        }

        public Pawn Negotiator { get; }

        public TradeNegotiatorSnapshot Snapshot { get; }
    }

    internal static class TradeNegotiatorAdapter
    {
        public static TradeNegotiatorSelection Select(PlayerTradeContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            return SelectCore(context.Colonists);
        }

        public static TradeNegotiatorSelection Select(PlayerTradeReuseContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            return SelectCore(context.Colonists);
        }

        private static TradeNegotiatorSelection SelectCore(IReadOnlyList<Pawn> colonists)
        {
            Pawn bestPawn = null;
            float bestStat = float.MinValue;

            foreach (Pawn pawn in colonists)
            {
                if (!IsCandidate(pawn))
                    continue;

                float statValue;

                try
                {
                    statValue = pawn.GetStatValue(StatDefOf.TradePriceImprovement);
                }
                catch
                {
                    continue;
                }

                if (float.IsNaN(statValue) || float.IsInfinity(statValue))
                    continue;

                if (bestPawn == null || statValue > bestStat ||
                    (statValue.Equals(bestStat) && pawn.thingIDNumber < bestPawn.thingIDNumber))
                {
                    bestPawn = pawn;
                    bestStat = statValue;
                }
            }

            if (bestPawn == null)
                return default(TradeNegotiatorSelection);

            string pawnId = bestPawn.ThingID;

            if (string.IsNullOrWhiteSpace(pawnId))
                pawnId = "Pawn:" + bestPawn.thingIDNumber.ToString(CultureInfo.InvariantCulture);

            string label = GetDisplayLabel(bestPawn);

            var snapshot = new TradeNegotiatorSnapshot(pawnId, label, bestStat);

            return new TradeNegotiatorSelection(bestPawn, snapshot);
        }

        private static bool IsCandidate(Pawn pawn)
        {
            if (pawn == null || pawn.Destroyed || pawn.Dead || !pawn.IsColonist)
                return false;

            try
            {
                return !pawn.WorkTagIsDisabled(WorkTags.Social);
            }
            catch
            {
                return false;
            }
        }

        private static string GetDisplayLabel(Pawn pawn)
        {
            string fullName = pawn.Name?.ToStringFull;

            if (!string.IsNullOrWhiteSpace(fullName))
                return fullName;

            string label = pawn.LabelCap;

            if (!string.IsNullOrWhiteSpace(label))
                return label;

            if (!string.IsNullOrWhiteSpace(pawn.ThingID))
                return pawn.ThingID;

            return "Pawn:" + pawn.thingIDNumber.ToString(CultureInfo.InvariantCulture);
        }
    }
}