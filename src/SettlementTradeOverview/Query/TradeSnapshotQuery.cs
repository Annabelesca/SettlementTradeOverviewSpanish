using System;
using System.Collections.Generic;
using SettlementTradeOverview.Domain.Categories;
using SettlementTradeOverview.Domain.Identity;
using SettlementTradeOverview.Domain.Snapshots;

namespace SettlementTradeOverview.Query
{
    public static class TradeSnapshotQuery
    {
        private static readonly IReadOnlyList<TradeQueryEntry> _emptyResult =
            Array.AsReadOnly(Array.Empty<TradeQueryEntry>());

        private static readonly Func<TradeEntryIdentity, int?> _noRelevanceMatchCountResolver = _ => null;

        public static IReadOnlyList<TradeQueryEntry> Execute(
            TradeInventorySnapshot snapshot,
            TradeQueryCriteria criteria)
        {
            return Execute(snapshot, criteria, _noRelevanceMatchCountResolver);
        }

        internal static IReadOnlyList<TradeQueryEntry> Execute(
            TradeInventorySnapshot snapshot,
            TradeQueryCriteria criteria,
            Func<TradeEntryIdentity, int?> relevanceMatchCountResolver)
        {
            if (snapshot == null)
                throw new ArgumentNullException(nameof(snapshot));

            if (relevanceMatchCountResolver == null)
                throw new ArgumentNullException(nameof(relevanceMatchCountResolver));

            if (!HasQueryableData(snapshot.Availability))
                return _emptyResult;

            return ExecuteCore(snapshot.Traders, criteria ?? TradeQueryCriteria.Default, relevanceMatchCountResolver);
        }

        public static IReadOnlyList<TradeQueryEntry> Execute(TraderSnapshot snapshot, TradeQueryCriteria criteria)
        {
            return Execute(snapshot, criteria, _noRelevanceMatchCountResolver);
        }

        internal static IReadOnlyList<TradeQueryEntry> Execute(
            TraderSnapshot snapshot,
            TradeQueryCriteria criteria,
            Func<TradeEntryIdentity, int?> relevanceMatchCountResolver)
        {
            if (snapshot == null)
                throw new ArgumentNullException(nameof(snapshot));

            if (relevanceMatchCountResolver == null)
                throw new ArgumentNullException(nameof(relevanceMatchCountResolver));

            if (!HasQueryableData(snapshot.Availability))
                return _emptyResult;

            TraderSnapshot[] traders = new[]
            {
                snapshot
            };

            return ExecuteCore(traders, criteria ?? TradeQueryCriteria.Default, relevanceMatchCountResolver);
        }

        private static IReadOnlyList<TradeQueryEntry> ExecuteCore(
            IReadOnlyList<TraderSnapshot> traders,
            TradeQueryCriteria criteria,
            Func<TradeEntryIdentity, int?> relevanceMatchCountResolver)
        {
            var result = new List<TradeQueryEntry>();

            foreach (TraderSnapshot trader in traders)
            {
                if (!HasQueryableData(trader.Availability))
                    continue;

                foreach (TradeEntrySnapshot entry in trader.Entries)
                {
                    if (!MatchesCategory(entry, criteria.Category))
                        continue;

                    if (!MatchesSearch(trader, entry, criteria.SearchText))
                        continue;

                    result.Add(new TradeQueryEntry(trader, entry));
                }
            }

            if (result.Count == 0)
                return _emptyResult;

            result.Sort(
                new TradeQueryEntryComparer(criteria.SortMode, criteria.SortDirection, relevanceMatchCountResolver));

            return result.AsReadOnly();
        }

        private static bool MatchesCategory(TradeEntrySnapshot entry, TradeCategory category)
        {
            return category == TradeCategory.All || entry.Category == category;
        }

        internal static bool MatchesSearch(TraderSnapshot trader, TradeEntrySnapshot entry, string searchText)
        {
            if (searchText.Length == 0)
                return true;

            return entry.Label.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0 ||
                   trader.SettlementLabel.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        internal static bool HasQueryableData(SnapshotAvailability availability)
        {
            switch (availability)
            {
                case SnapshotAvailability.Available:
                case SnapshotAvailability.Partial:
                    return true;

                case SnapshotAvailability.Empty:
                case SnapshotAvailability.Unavailable:
                case SnapshotAvailability.Failed:
                    return false;

                default:
                    throw new ArgumentOutOfRangeException(nameof(availability));
            }
        }
    }
}