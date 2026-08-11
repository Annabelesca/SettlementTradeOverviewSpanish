using System;
using SettlementTradeOverview.Domain.Categories;

namespace SettlementTradeOverview.Query
{
    public sealed class TradeQueryCriteria
    {
        public TradeQueryCriteria(
            TradeCategory category = TradeCategory.All,
            string searchText = "",
            TradeSortMode sortMode = TradeSortMode.Name,
            TradeSortDirection sortDirection = TradeSortDirection.Ascending)
        {
            if (!Enum.IsDefined(typeof(TradeCategory), category))
                throw new ArgumentOutOfRangeException(nameof(category));

            if (!Enum.IsDefined(typeof(TradeSortMode), sortMode))
                throw new ArgumentOutOfRangeException(nameof(sortMode));

            if (!Enum.IsDefined(typeof(TradeSortDirection), sortDirection))
                throw new ArgumentOutOfRangeException(nameof(sortDirection));

            Category = category;
            SearchText = (searchText ?? string.Empty).Trim();
            SortMode = sortMode;
            SortDirection = sortDirection;
        }

        public static TradeQueryCriteria Default { get; } = new TradeQueryCriteria();

        public TradeCategory Category { get; }

        public string SearchText { get; }

        public TradeSortMode SortMode { get; }

        public TradeSortDirection SortDirection { get; }
    }
}