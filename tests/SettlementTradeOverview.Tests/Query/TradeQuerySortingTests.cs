using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SettlementTradeOverview.Domain.Categories;
using SettlementTradeOverview.Domain.Identity;
using SettlementTradeOverview.Domain.Snapshots;
using SettlementTradeOverview.Query;

namespace SettlementTradeOverview.Tests.Query
{
    [TestFixture]
    public sealed class TradeQuerySortingTests
    {
        [Test]
        public void Execute_NameSort_SupportsAscendingAndDescendingDirections()
        {
            TraderSnapshot trader = CreateTrader(
                "Trader:1",
                1,
                "Alpha",
                TradeDistance.Reachable(10),
                TradeRestock.Scheduled(1000),
                CreateEntry("Bravo", "Bravo"),
                CreateEntry("Alpha", "Alpha"),
                CreateEntry("Charlie", "Charlie"));

            AssertOrder(Execute(TradeSortMode.Name, TradeSortDirection.Ascending, trader), "Alpha", "Bravo", "Charlie");

            AssertOrder(
                Execute(TradeSortMode.Name, TradeSortDirection.Descending, trader),
                "Charlie",
                "Bravo",
                "Alpha");
        }

        [Test]
        public void Execute_SettlementSort_SupportsAscendingAndDescendingDirections()
        {
            TraderSnapshot beta = CreateTrader(
                "Trader:2",
                2,
                "Beta",
                TradeDistance.Reachable(10),
                TradeRestock.Scheduled(1000),
                CreateEntry("BetaEntry", "Item"));

            TraderSnapshot alpha = CreateTrader(
                "Trader:1",
                1,
                "Alpha",
                TradeDistance.Reachable(10),
                TradeRestock.Scheduled(1000),
                CreateEntry("AlphaEntry", "Item"));

            TraderSnapshot gamma = CreateTrader(
                "Trader:3",
                3,
                "Gamma",
                TradeDistance.Reachable(10),
                TradeRestock.Scheduled(1000),
                CreateEntry("GammaEntry", "Item"));

            AssertOrder(
                Execute(TradeSortMode.Settlement, TradeSortDirection.Ascending, beta, gamma, alpha),
                "AlphaEntry",
                "BetaEntry",
                "GammaEntry");

            AssertOrder(
                Execute(TradeSortMode.Settlement, TradeSortDirection.Descending, beta, gamma, alpha),
                "GammaEntry",
                "BetaEntry",
                "AlphaEntry");
        }

        [Test]
        public void Execute_DistanceSort_SupportsAscendingAndDescendingDirections()
        {
            TraderSnapshot far = CreateTrader(
                "Trader:3",
                3,
                "Far",
                TradeDistance.Reachable(30),
                TradeRestock.Scheduled(1000),
                CreateEntry("Far", "Item"));

            TraderSnapshot near = CreateTrader(
                "Trader:1",
                1,
                "Near",
                TradeDistance.Reachable(10),
                TradeRestock.Scheduled(1000),
                CreateEntry("Near", "Item"));

            TraderSnapshot middle = CreateTrader(
                "Trader:2",
                2,
                "Middle",
                TradeDistance.Reachable(20),
                TradeRestock.Scheduled(1000),
                CreateEntry("Middle", "Item"));

            AssertOrder(
                Execute(TradeSortMode.Distance, TradeSortDirection.Ascending, far, near, middle),
                "Near",
                "Middle",
                "Far");

            AssertOrder(
                Execute(TradeSortMode.Distance, TradeSortDirection.Descending, far, near, middle),
                "Far",
                "Middle",
                "Near");
        }

        [Test]
        public void Execute_PriceSort_SupportsAscendingAndDescendingDirections()
        {
            TraderSnapshot trader = CreateTrader(
                "Trader:1",
                1,
                "Alpha",
                TradeDistance.Reachable(10),
                TradeRestock.Scheduled(1000),
                CreateEntry("High", "High", price: TradePrice.Negotiated(30f)),
                CreateEntry("Low", "Low", price: TradePrice.MarketValueFallback(10f)),
                CreateEntry("Middle", "Middle", price: TradePrice.Negotiated(20f)));

            AssertOrder(Execute(TradeSortMode.Price, TradeSortDirection.Ascending, trader), "Low", "Middle", "High");

            AssertOrder(Execute(TradeSortMode.Price, TradeSortDirection.Descending, trader), "High", "Middle", "Low");
        }

        [Test]
        public void Execute_RestockSort_SupportsAscendingAndDescendingDirections()
        {
            TraderSnapshot late = CreateTrader(
                "Trader:3",
                3,
                "Late",
                TradeDistance.Reachable(10),
                TradeRestock.Scheduled(3000),
                CreateEntry("Late", "Item"));

            TraderSnapshot early = CreateTrader(
                "Trader:1",
                1,
                "Early",
                TradeDistance.Reachable(10),
                TradeRestock.Scheduled(1000),
                CreateEntry("Early", "Item"));

            TraderSnapshot middle = CreateTrader(
                "Trader:2",
                2,
                "Middle",
                TradeDistance.Reachable(10),
                TradeRestock.Scheduled(2000),
                CreateEntry("Middle", "Item"));

            AssertOrder(
                Execute(TradeSortMode.RestockTime, TradeSortDirection.Ascending, late, early, middle),
                "Early",
                "Middle",
                "Late");

            AssertOrder(
                Execute(TradeSortMode.RestockTime, TradeSortDirection.Descending, late, early, middle),
                "Late",
                "Middle",
                "Early");
        }

        [Test]
        public void Execute_CountSort_SupportsAscendingAndDescendingDirections()
        {
            TraderSnapshot trader = CreateTrader(
                "Trader:1",
                1,
                "Alpha",
                TradeDistance.Reachable(10),
                TradeRestock.Scheduled(1000),
                CreateEntry("High", "High", count: 30),
                CreateEntry("Low", "Low", count: 10),
                CreateEntry("Middle", "Middle", count: 20));

            AssertOrder(Execute(TradeSortMode.Count, TradeSortDirection.Ascending, trader), "Low", "Middle", "High");

            AssertOrder(Execute(TradeSortMode.Count, TradeSortDirection.Descending, trader), "High", "Middle", "Low");
        }

        [Test]
        public void Execute_DistanceSort_UsesKnownTileDistanceRegardlessOfRouteState()
        {
            TraderSnapshot unavailable = CreateTrader(
                "Trader:5",
                5,
                "Unavailable",
                TradeDistance.Unavailable,
                TradeRestock.Scheduled(1000),
                CreateEntry("Unavailable", "Item"));

            TraderSnapshot unknownRoute = CreateTrader(
                "Trader:4",
                4,
                "UnknownRoute",
                TradeDistance.WithUnavailableRoute(40),
                TradeRestock.Scheduled(1000),
                CreateEntry("UnknownRoute", "Item"));

            TraderSnapshot unreachable = CreateTrader(
                "Trader:3",
                3,
                "Unreachable",
                TradeDistance.Unreachable(20),
                TradeRestock.Scheduled(1000),
                CreateEntry("Unreachable", "Item"));

            TraderSnapshot far = CreateTrader(
                "Trader:2",
                2,
                "Far",
                TradeDistance.Reachable(50),
                TradeRestock.Scheduled(1000),
                CreateEntry("Far", "Item"));

            TraderSnapshot near = CreateTrader(
                "Trader:1",
                1,
                "Near",
                TradeDistance.Reachable(10),
                TradeRestock.Scheduled(1000),
                CreateEntry("Near", "Item"));

            AssertOrder(
                Execute(
                    TradeSortMode.Distance,
                    TradeSortDirection.Ascending,
                    unavailable,
                    far,
                    unknownRoute,
                    unreachable,
                    near),
                "Near",
                "Unreachable",
                "UnknownRoute",
                "Far",
                "Unavailable");

            AssertOrder(
                Execute(
                    TradeSortMode.Distance,
                    TradeSortDirection.Descending,
                    unavailable,
                    far,
                    unknownRoute,
                    unreachable,
                    near),
                "Far",
                "UnknownRoute",
                "Unreachable",
                "Near",
                "Unavailable");
        }

        [Test]
        public void Execute_DistanceSort_UsesRouteStateAsFixedTieBreakerForEqualDistances()
        {
            TraderSnapshot unavailableRoute = CreateTrader(
                "Trader:3",
                3,
                "UnavailableRoute",
                TradeDistance.WithUnavailableRoute(20),
                TradeRestock.Scheduled(1000),
                CreateEntry("UnavailableRoute", "Item"));

            TraderSnapshot unreachable = CreateTrader(
                "Trader:2",
                2,
                "Unreachable",
                TradeDistance.Unreachable(20),
                TradeRestock.Scheduled(1000),
                CreateEntry("Unreachable", "Item"));

            TraderSnapshot reachable = CreateTrader(
                "Trader:1",
                1,
                "Reachable",
                TradeDistance.Reachable(20),
                TradeRestock.Scheduled(1000),
                CreateEntry("Reachable", "Item"));

            AssertOrder(
                Execute(TradeSortMode.Distance, TradeSortDirection.Ascending, unavailableRoute, unreachable, reachable),
                "Reachable",
                "Unreachable",
                "UnavailableRoute");

            AssertOrder(
                Execute(
                    TradeSortMode.Distance,
                    TradeSortDirection.Descending,
                    unavailableRoute,
                    unreachable,
                    reachable),
                "Reachable",
                "Unreachable",
                "UnavailableRoute");
        }

        [Test]
        public void Execute_PriceSort_PlacesUnavailablePricesLastInBothDirections()
        {
            TraderSnapshot trader = CreateTrader(
                "Trader:1",
                1,
                "Alpha",
                TradeDistance.Reachable(10),
                TradeRestock.Scheduled(1000),
                CreateEntry("Unavailable", "Unavailable", price: TradePrice.Unavailable),
                CreateEntry("High", "High", price: TradePrice.Negotiated(20f)),
                CreateEntry("Low", "Low", price: TradePrice.MarketValueFallback(10f)));

            AssertOrder(
                Execute(TradeSortMode.Price, TradeSortDirection.Ascending, trader),
                "Low",
                "High",
                "Unavailable");

            AssertOrder(
                Execute(TradeSortMode.Price, TradeSortDirection.Descending, trader),
                "High",
                "Low",
                "Unavailable");
        }

        [Test]
        public void Execute_RestockSort_PlacesPendingAndUnavailableAfterScheduledValues()
        {
            TraderSnapshot unavailable = CreateTrader(
                "Trader:4",
                4,
                "Unavailable",
                TradeDistance.Reachable(10),
                TradeRestock.Unavailable,
                CreateEntry("Unavailable", "Item"));

            TraderSnapshot pending = CreateTrader(
                "Trader:3",
                3,
                "Pending",
                TradeDistance.Reachable(10),
                TradeRestock.PendingGeneration,
                CreateEntry("Pending", "Item"));

            TraderSnapshot late = CreateTrader(
                "Trader:2",
                2,
                "Late",
                TradeDistance.Reachable(10),
                TradeRestock.Scheduled(2000),
                CreateEntry("Late", "Item"));

            TraderSnapshot early = CreateTrader(
                "Trader:1",
                1,
                "Early",
                TradeDistance.Reachable(10),
                TradeRestock.Scheduled(1000),
                CreateEntry("Early", "Item"));

            AssertOrder(
                Execute(TradeSortMode.RestockTime, TradeSortDirection.Ascending, unavailable, pending, late, early),
                "Early",
                "Late",
                "Pending",
                "Unavailable");

            AssertOrder(
                Execute(TradeSortMode.RestockTime, TradeSortDirection.Descending, unavailable, pending, late, early),
                "Late",
                "Early",
                "Pending",
                "Unavailable");
        }

        [Test]
        public void Execute_NameSort_UsesSettlementTraderAndEntryIdentitiesAsTieBreakers()
        {
            TraderSnapshot beta = CreateTrader(
                "Trader:B",
                4,
                "Beta",
                TradeDistance.Reachable(10),
                TradeRestock.Scheduled(1000),
                CreateEntry("Entry:D", "Same"));

            TraderSnapshot traderZ = CreateTrader(
                "Trader:Z",
                3,
                "Alpha",
                TradeDistance.Reachable(10),
                TradeRestock.Scheduled(1000),
                CreateEntry("Entry:C", "Same"));

            TraderSnapshot traderA = CreateTrader(
                "Trader:A",
                2,
                "Alpha",
                TradeDistance.Reachable(10),
                TradeRestock.Scheduled(1000),
                CreateEntry("Entry:B", "Same"),
                CreateEntry("Entry:A", "Same"));

            AssertOrder(
                Execute(TradeSortMode.Name, TradeSortDirection.Ascending, beta, traderZ, traderA),
                "Entry:A",
                "Entry:B",
                "Entry:C",
                "Entry:D");
        }

        [Test]
        public void Execute_StringComparison_UsesOrdinalTieBreakerAfterIgnoringCase()
        {
            TraderSnapshot trader = CreateTrader(
                "Trader:1",
                1,
                "Alpha",
                TradeDistance.Reachable(10),
                TradeRestock.Scheduled(1000),
                CreateEntry("Lower", "alpha"),
                CreateEntry("Upper", "Alpha"));

            AssertOrder(Execute(TradeSortMode.Name, TradeSortDirection.Ascending, trader), "Upper", "Lower");
        }

        [Test]
        public void Execute_DescendingPrimarySort_KeepsTieBreakersAscending()
        {
            TraderSnapshot beta = CreateTrader(
                "Trader:2",
                2,
                "Beta",
                TradeDistance.Reachable(10),
                TradeRestock.Scheduled(1000),
                CreateEntry("Beta", "Same", count: 10));

            TraderSnapshot alpha = CreateTrader(
                "Trader:1",
                1,
                "Alpha",
                TradeDistance.Reachable(10),
                TradeRestock.Scheduled(1000),
                CreateEntry("Alpha", "Same", count: 10));

            AssertOrder(Execute(TradeSortMode.Count, TradeSortDirection.Descending, beta, alpha), "Alpha", "Beta");
        }

        [Test]
        public void Execute_RepeatedQuery_ReturnsIdenticalOrder()
        {
            TraderSnapshot trader = CreateTrader(
                "Trader:1",
                1,
                "Alpha",
                TradeDistance.Reachable(10),
                TradeRestock.Scheduled(1000),
                CreateEntry("C", "Same"),
                CreateEntry("A", "Same"),
                CreateEntry("B", "Same"));

            IReadOnlyList<TradeQueryEntry> first = Execute(TradeSortMode.Name, TradeSortDirection.Ascending, trader);

            IReadOnlyList<TradeQueryEntry> second = Execute(TradeSortMode.Name, TradeSortDirection.Ascending, trader);

            Assert.That(GetIdentityOrder(first), Is.EqualTo(GetIdentityOrder(second)));
        }

        [Test]
        public void Execute_InputPermutation_DoesNotChangeDeterministicOrder()
        {
            TraderSnapshot firstTraderFirstOrder = CreateTrader(
                "Trader:B",
                2,
                "Same",
                TradeDistance.Reachable(10),
                TradeRestock.Scheduled(1000),
                CreateEntry("Entry:D", "Same"),
                CreateEntry("Entry:C", "Same"));

            TraderSnapshot secondTraderFirstOrder = CreateTrader(
                "Trader:A",
                1,
                "Same",
                TradeDistance.Reachable(10),
                TradeRestock.Scheduled(1000),
                CreateEntry("Entry:B", "Same"),
                CreateEntry("Entry:A", "Same"));

            TraderSnapshot firstTraderSecondOrder = CreateTrader(
                "Trader:A",
                1,
                "Same",
                TradeDistance.Reachable(10),
                TradeRestock.Scheduled(1000),
                CreateEntry("Entry:A", "Same"),
                CreateEntry("Entry:B", "Same"));

            TraderSnapshot secondTraderSecondOrder = CreateTrader(
                "Trader:B",
                2,
                "Same",
                TradeDistance.Reachable(10),
                TradeRestock.Scheduled(1000),
                CreateEntry("Entry:C", "Same"),
                CreateEntry("Entry:D", "Same"));

            IReadOnlyList<TradeQueryEntry> first = Execute(
                TradeSortMode.Name,
                TradeSortDirection.Ascending,
                firstTraderFirstOrder,
                secondTraderFirstOrder);

            IReadOnlyList<TradeQueryEntry> second = Execute(
                TradeSortMode.Name,
                TradeSortDirection.Ascending,
                firstTraderSecondOrder,
                secondTraderSecondOrder);

            Assert.That(GetIdentityOrder(first), Is.EqualTo(GetIdentityOrder(second)));

            Assert.That(
                GetIdentityOrder(first),
                Is.EqualTo(
                    new[]
                    {
                        "Entry:A",
                        "Entry:B",
                        "Entry:C",
                        "Entry:D"
                    }));
        }

        [Test]
        public void Execute_DetailsSort_PlacesRelevantRowsFirstAndSortsCountsAscending()
        {
            TraderSnapshot trader = CreateTrader(
                "Trader:1",
                1,
                "Alpha",
                TradeDistance.Reachable(10),
                TradeRestock.Scheduled(1000),
                CreateEntry("Neutral", "Neutral"),
                CreateEntry("Three", "Three"),
                CreateEntry("One", "One"),
                CreateEntry("Two", "Two"));

            var counts = new Dictionary<string, int>
            {
                { "One", 1 },
                { "Two", 2 },
                { "Three", 3 }
            };

            AssertOrder(
                ExecuteWithRelevanceResolver(
                    TradeSortDirection.Ascending,
                    identity => counts.TryGetValue(identity.Value, out int count) ? count : (int?)null,
                    trader),
                "One",
                "Two",
                "Three",
                "Neutral");
        }

        [Test]
        public void Execute_DetailsSort_PlacesRelevantRowsFirstAndSortsCountsDescending()
        {
            TraderSnapshot trader = CreateTrader(
                "Trader:1",
                1,
                "Alpha",
                TradeDistance.Reachable(10),
                TradeRestock.Scheduled(1000),
                CreateEntry("Neutral", "Neutral"),
                CreateEntry("One", "One"),
                CreateEntry("Three", "Three"),
                CreateEntry("Two", "Two"));

            var counts = new Dictionary<string, int>
            {
                { "One", 1 },
                { "Two", 2 },
                { "Three", 3 }
            };

            AssertOrder(
                ExecuteWithRelevanceResolver(
                    TradeSortDirection.Descending,
                    identity => counts.TryGetValue(identity.Value, out int count) ? count : (int?)null,
                    trader),
                "Three",
                "Two",
                "One",
                "Neutral");
        }

        [Test]
        public void Execute_DetailsSort_EqualCountsUseAscendingTieBreakers()
        {
            TraderSnapshot beta = CreateTrader(
                "Trader:2",
                2,
                "Beta",
                TradeDistance.Reachable(10),
                TradeRestock.Scheduled(1000),
                CreateEntry("Beta", "Same"));

            TraderSnapshot alpha = CreateTrader(
                "Trader:1",
                1,
                "Alpha",
                TradeDistance.Reachable(10),
                TradeRestock.Scheduled(1000),
                CreateEntry("Alpha", "Same"));

            AssertOrder(
                ExecuteWithRelevanceResolver(TradeSortDirection.Descending, _ => 2, beta, alpha),
                "Alpha",
                "Beta");
        }

        [Test]
        public void Execute_DetailsSort_AllNeutralRowsRemainDeterministic()
        {
            TraderSnapshot trader = CreateTrader(
                "Trader:1",
                1,
                "Alpha",
                TradeDistance.Reachable(10),
                TradeRestock.Scheduled(1000),
                CreateEntry("C", "Same"),
                CreateEntry("A", "Same"),
                CreateEntry("B", "Same"));

            AssertOrder(ExecuteWithRelevanceResolver(TradeSortDirection.Ascending, _ => null, trader), "A", "B", "C");
        }

        [Test]
        public void Execute_NonDetailsSort_DoesNotInvokeRelevanceResolver()
        {
            TraderSnapshot trader = CreateTrader(
                "Trader:1",
                1,
                "Alpha",
                TradeDistance.Reachable(10),
                TradeRestock.Scheduled(1000),
                CreateEntry("B", "B", count: 1),
                CreateEntry("A", "A", count: 2));

            var resolverCallCount = 0;
            var snapshot = new TradeInventorySnapshot(SnapshotAvailability.Available, 1000, 10, null, new[] { trader });

            IReadOnlyList<TradeQueryEntry> result = TradeSnapshotQuery.Execute(
                snapshot,
                new TradeQueryCriteria(sortMode: TradeSortMode.Count, sortDirection: TradeSortDirection.Descending),
                _ =>
                {
                    resolverCallCount++;
                    return 1;
                });

            AssertOrder(result, "A", "B");
            Assert.That(resolverCallCount, Is.Zero);
        }

        [Test]
        public void Execute_RepeatedDetailsQuery_ReturnsIdenticalOrder()
        {
            TraderSnapshot trader = CreateTrader(
                "Trader:1",
                1,
                "Alpha",
                TradeDistance.Reachable(10),
                TradeRestock.Scheduled(1000),
                CreateEntry("C", "C"),
                CreateEntry("A", "A"),
                CreateEntry("B", "B"));

            Func<TradeEntryIdentity, int?> resolver = identity => identity.Value == "C" ? 2 : 1;

            IReadOnlyList<TradeQueryEntry> first = ExecuteWithRelevanceResolver(
                TradeSortDirection.Ascending,
                resolver,
                trader);

            IReadOnlyList<TradeQueryEntry> second = ExecuteWithRelevanceResolver(
                TradeSortDirection.Ascending,
                resolver,
                trader);

            Assert.That(GetIdentityOrder(first), Is.EqualTo(GetIdentityOrder(second)));
        }

        [Test]
        public void Execute_DetailsSort_UsesFixedGroupOrderInBothDirections()
        {
            TraderSnapshot trader = CreateTrader(
                "Trader:1",
                1,
                "Alpha",
                TradeDistance.Reachable(10),
                TradeRestock.Scheduled(1000),
                CreateEntry("Neutral", "Neutral"),
                CreatePawnEntry(
                    "Rideable",
                    "Rideable",
                    new PawnTradeDetailsSnapshot(PawnTradeDetailKind.Rideable, 1.5f)),
                CreatePawnEntry("Slave", "Slave", new PawnTradeDetailsSnapshot(PawnTradeDetailKind.JoinsAsSlave)),
                CreatePawnEntry(
                    "Colonist",
                    "Colonist",
                    new PawnTradeDetailsSnapshot(PawnTradeDetailKind.JoinsAsColonist)),
                CreateEntry("Relevant", "Relevant"));

            Func<TradeEntryIdentity, int?> resolver = identity => identity.Value == "Relevant" ? 2 : (int?)null;

            AssertOrder(
                ExecuteWithRelevanceResolver(TradeSortDirection.Ascending, resolver, trader),
                "Relevant",
                "Colonist",
                "Slave",
                "Rideable",
                "Neutral");

            AssertOrder(
                ExecuteWithRelevanceResolver(TradeSortDirection.Descending, resolver, trader),
                "Relevant",
                "Colonist",
                "Slave",
                "Rideable",
                "Neutral");
        }

        [Test]
        public void Execute_DetailsSort_SortsRideableFactorsByDirection()
        {
            TraderSnapshot trader = CreateTrader(
                "Trader:1",
                1,
                "Alpha",
                TradeDistance.Reachable(10),
                TradeRestock.Scheduled(1000),
                CreatePawnEntry("High", "High", new PawnTradeDetailsSnapshot(PawnTradeDetailKind.Rideable, 2f)),
                CreatePawnEntry("Low", "Low", new PawnTradeDetailsSnapshot(PawnTradeDetailKind.Rideable, 1.1f)),
                CreatePawnEntry("Middle", "Middle", new PawnTradeDetailsSnapshot(PawnTradeDetailKind.Rideable, 1.5f)));

            AssertOrder(
                ExecuteWithRelevanceResolver(TradeSortDirection.Ascending, _ => null, trader),
                "Low",
                "Middle",
                "High");

            AssertOrder(
                ExecuteWithRelevanceResolver(TradeSortDirection.Descending, _ => null, trader),
                "High",
                "Middle",
                "Low");
        }

        [Test]
        public void Execute_DetailsSort_RelevanceOverridesPawnDetailKind()
        {
            TraderSnapshot trader = CreateTrader(
                "Trader:1",
                1,
                "Alpha",
                TradeDistance.Reachable(10),
                TradeRestock.Scheduled(1000),
                CreatePawnEntry(
                    "RelevantPawn",
                    "RelevantPawn",
                    new PawnTradeDetailsSnapshot(PawnTradeDetailKind.Rideable, 2f)),
                CreatePawnEntry(
                    "Colonist",
                    "Colonist",
                    new PawnTradeDetailsSnapshot(PawnTradeDetailKind.JoinsAsColonist)));

            AssertOrder(
                ExecuteWithRelevanceResolver(
                    TradeSortDirection.Ascending,
                    identity => identity.Value == "RelevantPawn" ? 1 : (int?)null,
                    trader),
                "RelevantPawn",
                "Colonist");
        }

        [Test]
        public void Execute_DetailsSort_PawnNoneRemainsInNeutralGroup()
        {
            TraderSnapshot trader = CreateTrader(
                "Trader:1",
                1,
                "Alpha",
                TradeDistance.Reachable(10),
                TradeRestock.Scheduled(1000),
                CreateEntry("Item", "B"),
                CreatePawnEntry("Pawn", "A", new PawnTradeDetailsSnapshot(PawnTradeDetailKind.None)));

            AssertOrder(ExecuteWithRelevanceResolver(TradeSortDirection.Ascending, _ => null, trader), "Pawn", "Item");
        }

        private static IReadOnlyList<TradeQueryEntry> ExecuteWithRelevanceResolver(
            TradeSortDirection direction,
            Func<TradeEntryIdentity, int?> resolver,
            params TraderSnapshot[] traders)
        {
            var snapshot = new TradeInventorySnapshot(SnapshotAvailability.Available, 1000, 10, null, traders);

            return TradeSnapshotQuery.Execute(
                snapshot,
                new TradeQueryCriteria(TradeCategory.All, string.Empty, TradeSortMode.Details, direction),
                resolver);
        }

        private static IReadOnlyList<TradeQueryEntry> Execute(
            TradeSortMode mode,
            TradeSortDirection direction,
            params TraderSnapshot[] traders)
        {
            var snapshot = new TradeInventorySnapshot(SnapshotAvailability.Available, 1000, 10, null, traders);

            return TradeSnapshotQuery.Execute(
                snapshot,
                new TradeQueryCriteria(TradeCategory.All, string.Empty, mode, direction));
        }

        private static void AssertOrder(IReadOnlyList<TradeQueryEntry> result, params string[] expectedIdentities)
        {
            Assert.That(GetIdentityOrder(result), Is.EqualTo(expectedIdentities));
        }

        private static string[] GetIdentityOrder(IReadOnlyList<TradeQueryEntry> result)
        {
            return result.Select(row => row.Entry.Identity.Value).ToArray();
        }

        private static TraderSnapshot CreateTrader(
            string traderId,
            int settlementId,
            string settlementLabel,
            TradeDistance distance,
            TradeRestock restock,
            params TradeEntrySnapshot[] entries)
        {
            return new TraderSnapshot(
                new TraderIdentity(traderId),
                new SettlementIdentity(settlementId),
                "Trader",
                settlementLabel,
                SnapshotAvailability.Available,
                distance,
                restock,
                entries,
                null);
        }

        private static TradeEntrySnapshot CreatePawnEntry(
            string identity,
            string label,
            PawnTradeDetailsSnapshot pawnDetails)
        {
            return new TradeEntrySnapshot(
                new TradeEntryIdentity(identity),
                TradeEntryKind.Pawn,
                TradeCategoryMembership.None,
                "Human",
                label,
                1,
                TradePrice.Negotiated(10f),
                pawnDetails);
        }

        private static TradeEntrySnapshot CreateEntry(
            string identity,
            string label,
            int count = 1,
            TradePrice? price = null)
        {
            return new TradeEntrySnapshot(
                new TradeEntryIdentity(identity),
                TradeEntryKind.Item,
                TradeCategoryMembership.Items,
                identity,
                label,
                count,
                price ?? TradePrice.Negotiated(10f));
        }
    }
}