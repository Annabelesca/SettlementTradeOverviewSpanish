using System;
using System.Collections.Generic;
using SettlementTradeOverview.Domain.Categories;
using SettlementTradeOverview.Domain.Snapshots;

namespace SettlementTradeOverview.Query
{
    internal static class TradeCategoryAvailability
    {
        private static readonly IReadOnlyList<TradeCategory> _allOnly = Array.AsReadOnly(
            new[]
            {
                TradeCategory.All
            });

        public static IReadOnlyList<TradeCategory> Resolve(TradeInventorySnapshot snapshot, string searchText)
        {
            if (snapshot == null)
                throw new ArgumentNullException(nameof(snapshot));

            if (!TradeSnapshotQuery.HasQueryableData(snapshot.Availability))
                return _allOnly;

            string normalizedSearch = NormalizeSearchText(searchText);
            var presentCategories = new HashSet<TradeCategory>();

            foreach (TraderSnapshot trader in snapshot.Traders)
            {
                if (!TradeSnapshotQuery.HasQueryableData(trader.Availability))
                    continue;

                CollectCategories(trader, normalizedSearch, presentCategories);
            }

            return CreateResult(presentCategories);
        }

        public static IReadOnlyList<TradeCategory> Resolve(TraderSnapshot trader, string searchText)
        {
            if (trader == null)
                throw new ArgumentNullException(nameof(trader));

            if (!TradeSnapshotQuery.HasQueryableData(trader.Availability))
                return _allOnly;

            string normalizedSearch = NormalizeSearchText(searchText);
            var presentCategories = new HashSet<TradeCategory>();

            CollectCategories(trader, normalizedSearch, presentCategories);

            return CreateResult(presentCategories);
        }

        public static bool Contains(IReadOnlyList<TradeCategory> categories, TradeCategory category)
        {
            if (categories == null)
                throw new ArgumentNullException(nameof(categories));

            foreach (TradeCategory t in categories)
            {
                if (t == category)
                    return true;
            }

            return false;
        }

        private static string NormalizeSearchText(string searchText)
        {
            return new TradeQueryCriteria(searchText: searchText).SearchText;
        }

        private static void CollectCategories(
            TraderSnapshot trader,
            string searchText,
            ISet<TradeCategory> presentCategories)
        {
            foreach (TradeEntrySnapshot entry in trader.Entries)
            {
                if (TradeSnapshotQuery.MatchesSearch(trader, entry, searchText))
                    presentCategories.Add(entry.Category);
            }
        }

        private static IReadOnlyList<TradeCategory> CreateResult(ISet<TradeCategory> presentCategories)
        {
            if (presentCategories.Count == 0)
                return _allOnly;

            var result = new List<TradeCategory>
            {
                TradeCategory.All
            };

            foreach (TradeCategory category in Enum.GetValues(typeof(TradeCategory)))
            {
                if (category != TradeCategory.All && presentCategories.Contains(category))
                    result.Add(category);
            }

            return result.AsReadOnly();
        }
    }
}