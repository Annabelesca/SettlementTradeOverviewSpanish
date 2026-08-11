using System;
using System.Collections.Generic;
using SettlementTradeOverview.Domain.Snapshots;
using SettlementTradeOverview.Integration.Planner;
using SettlementTradeOverview.Query;
using Verse;

namespace SettlementTradeOverview.UI.TradeList
{
    internal sealed class TradeListRowPresentation
    {
        private readonly TradeQueryEntry _queryEntry;

        public TradeListRowPresentation(
            TradeQueryEntry queryEntry,
            string countText,
            string infoTooltip,
            string settlementNavigationTooltip,
            TradeCellPresentation price,
            TradeCellPresentation distance,
            TradeCellPresentation restock,
            PlannerTradeEntryRelevance relevance,
            string relevanceTooltip)
        {
            _queryEntry = queryEntry ?? throw new ArgumentNullException(nameof(queryEntry));
            CountText = countText ?? throw new ArgumentNullException(nameof(countText));
            InfoTooltip = infoTooltip ?? throw new ArgumentNullException(nameof(infoTooltip));
            SettlementNavigationTooltip = settlementNavigationTooltip;
            Price = price;
            Distance = distance;
            Restock = restock;
            Relevance = relevance;
            RelevanceTooltip = relevanceTooltip;
        }

        public TraderSnapshot Trader =>
            _queryEntry.Trader;

        public TradeEntrySnapshot Entry =>
            _queryEntry.Entry;

        public string CountText { get; }

        public string InfoTooltip { get; }

        public string SettlementNavigationTooltip { get; }

        public TradeCellPresentation Price { get; }

        public TradeCellPresentation Distance { get; }

        public TradeCellPresentation Restock { get; }

        public PlannerTradeEntryRelevance Relevance { get; }

        public string RelevanceTooltip { get; }
    }

    internal static class TradeListRowPresentationBuilder
    {
        private static readonly IReadOnlyList<TradeListRowPresentation> _emptyRows =
            Array.AsReadOnly(Array.Empty<TradeListRowPresentation>());

        public static IReadOnlyList<TradeListRowPresentation> Build(
            IReadOnlyList<TradeQueryEntry> entries,
            TradeInventorySnapshot snapshot,
            TradeListMode mode,
            PlannerTradeRelevanceProjection relevanceProjection)
        {
            if (entries == null)
                throw new ArgumentNullException(nameof(entries));

            if (snapshot == null)
                throw new ArgumentNullException(nameof(snapshot));

            if (relevanceProjection == null)
                throw new ArgumentNullException(nameof(relevanceProjection));

            if (!Enum.IsDefined(typeof(TradeListMode), mode))
                throw new ArgumentOutOfRangeException(nameof(mode));

            if (entries.Count == 0)
                return _emptyRows;

            switch (mode)
            {
                case TradeListMode.Global:
                    return BuildGlobalRows(entries, snapshot, relevanceProjection);

                case TradeListMode.Settlement:
                    return BuildSettlementRows(entries, snapshot, relevanceProjection);

                default:
                    throw new ArgumentOutOfRangeException(nameof(mode));
            }
        }

        private static IReadOnlyList<TradeListRowPresentation> BuildGlobalRows(
            IReadOnlyList<TradeQueryEntry> entries,
            TradeInventorySnapshot snapshot,
            PlannerTradeRelevanceProjection relevanceProjection)
        {
            var rows = new List<TradeListRowPresentation>(entries.Count);

            var traderPresentations = new Dictionary<TraderSnapshot, TraderRowPresentation>();

            foreach (TradeQueryEntry queryEntry in entries)
            {
                ValidateQueryEntry(queryEntry, nameof(entries));

                if (!traderPresentations.TryGetValue(queryEntry.Trader, out TraderRowPresentation traderPresentation))
                {
                    traderPresentation = CreateTraderPresentation(queryEntry.Trader, snapshot.CapturedAtTick);
                    traderPresentations.Add(queryEntry.Trader, traderPresentation);
                }

                rows.Add(
                    CreateRow(
                        queryEntry,
                        snapshot,
                        traderPresentation.NavigationTooltip,
                        traderPresentation.Distance,
                        traderPresentation.Restock,
                        relevanceProjection));
            }

            return rows.AsReadOnly();
        }

        private static IReadOnlyList<TradeListRowPresentation> BuildSettlementRows(
            IReadOnlyList<TradeQueryEntry> entries,
            TradeInventorySnapshot snapshot,
            PlannerTradeRelevanceProjection relevanceProjection)
        {
            var rows = new List<TradeListRowPresentation>(entries.Count);

            foreach (TradeQueryEntry queryEntry in entries)
            {
                ValidateQueryEntry(queryEntry, nameof(entries));

                rows.Add(
                    CreateRow(
                        queryEntry,
                        snapshot,
                        null,
                        default(TradeCellPresentation),
                        default(TradeCellPresentation),
                        relevanceProjection));
            }

            return rows.AsReadOnly();
        }

        private static TradeListRowPresentation CreateRow(
            TradeQueryEntry queryEntry,
            TradeInventorySnapshot snapshot,
            string settlementNavigationTooltip,
            TradeCellPresentation distance,
            TradeCellPresentation restock,
            PlannerTradeRelevanceProjection relevanceProjection)
        {
            TradeCellPresentation price = TradeValuePresentation.CreatePrice(
                queryEntry.Entry.Price,
                snapshot.Negotiator);

            relevanceProjection.TryGet(queryEntry.Entry.Identity, out PlannerTradeEntryRelevance relevance);

            string relevanceTooltip = relevance == null
                ? null
                : TradeRelevanceTooltipPresentationBuilder.BuildLocalizedTooltip(relevance);

            return new TradeListRowPresentation(
                queryEntry,
                queryEntry.Entry.Count.ToString(),
                "STO.TradeList.Info.Open".Translate(queryEntry.Entry.Label).ToString(),
                settlementNavigationTooltip,
                price,
                distance,
                restock,
                relevance,
                relevanceTooltip);
        }

        private static TraderRowPresentation CreateTraderPresentation(TraderSnapshot trader, int capturedAtTick)
        {
            return new TraderRowPresentation(
                TradeValuePresentation.CreateDistance(trader.Distance),
                TradeValuePresentation.CreateRestock(trader.Restock, capturedAtTick),
                "STO.TradeList.Navigation.JumpToSettlement".Translate(trader.SettlementLabel).ToString());
        }

        private static void ValidateQueryEntry(TradeQueryEntry queryEntry, string parameterName)
        {
            if (queryEntry == null)
            {
                throw new ArgumentException("Trade query entries cannot contain null values.", parameterName);
            }
        }

        private readonly struct TraderRowPresentation
        {
            public TraderRowPresentation(
                TradeCellPresentation distance,
                TradeCellPresentation restock,
                string navigationTooltip)
            {
                Distance = distance;
                Restock = restock;
                NavigationTooltip = navigationTooltip ?? throw new ArgumentNullException(nameof(navigationTooltip));
            }

            public TradeCellPresentation Distance { get; }

            public TradeCellPresentation Restock { get; }

            public string NavigationTooltip { get; }
        }
    }
}