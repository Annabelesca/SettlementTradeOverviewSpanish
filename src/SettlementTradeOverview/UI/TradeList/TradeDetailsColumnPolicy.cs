using System;
using System.Collections.Generic;
using SettlementTradeOverview.Domain.Snapshots;
using SettlementTradeOverview.Integration.Planner;
using SettlementTradeOverview.Query;

namespace SettlementTradeOverview.UI.TradeList
{
    internal static class TradeDetailsColumnPolicy
    {
        public static bool ShouldShow(
            IReadOnlyList<TradeQueryEntry> entries,
            PlannerTradeRelevanceProjection relevanceProjection,
            bool isDetailsSortActive)
        {
            if (entries == null)
                throw new ArgumentNullException(nameof(entries));

            if (relevanceProjection == null)
                throw new ArgumentNullException(nameof(relevanceProjection));

            if (isDetailsSortActive)
                return true;

            foreach (TradeQueryEntry queryEntry in entries)
            {
                TradeEntrySnapshot entry = queryEntry?.Entry;

                if (entry == null)
                    continue;

                PawnTradeDetailsSnapshot pawnDetails = entry.PawnDetails;

                if (pawnDetails != null && pawnDetails.Kind != PawnTradeDetailKind.None)
                    return true;

                if (relevanceProjection.TryGet(entry.Identity, out _))
                    return true;
            }

            return false;
        }
    }
}