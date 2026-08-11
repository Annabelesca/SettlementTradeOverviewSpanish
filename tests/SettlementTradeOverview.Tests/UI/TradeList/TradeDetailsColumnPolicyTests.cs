using System;
using NUnit.Framework;
using SettlementTradeOverview.Domain.Categories;
using SettlementTradeOverview.Domain.Identity;
using SettlementTradeOverview.Domain.Snapshots;
using SettlementTradeOverview.Integration.Planner;
using SettlementTradeOverview.Query;
using SettlementTradeOverview.UI.TradeList;

namespace SettlementTradeOverview.Tests.UI.TradeList
{
    [TestFixture]
    public sealed class TradeDetailsColumnPolicyTests
    {
        [Test]
        public void ShouldShow_EmptyResultAndInactiveSort_ReturnsFalse()
        {
            bool result = TradeDetailsColumnPolicy.ShouldShow(
                Array.Empty<TradeQueryEntry>(),
                PlannerTradeRelevanceProjection.Empty,
                isDetailsSortActive: false);

            Assert.That(result, Is.False);
        }

        [Test]
        public void ShouldShow_OrdinaryEntry_ReturnsFalse()
        {
            TradeQueryEntry queryEntry = CreateItemQueryEntry("Thing:Steel");

            bool result = TradeDetailsColumnPolicy.ShouldShow(
                new[] { queryEntry },
                PlannerTradeRelevanceProjection.Empty,
                isDetailsSortActive: false);

            Assert.That(result, Is.False);
        }

        [Test]
        public void ShouldShow_PawnNoneDetails_ReturnsFalse()
        {
            TradeQueryEntry queryEntry = CreatePawnQueryEntry(
                "Pawn:1",
                new PawnTradeDetailsSnapshot(PawnTradeDetailKind.None));

            bool result = TradeDetailsColumnPolicy.ShouldShow(
                new[] { queryEntry },
                PlannerTradeRelevanceProjection.Empty,
                isDetailsSortActive: false);

            Assert.That(result, Is.False);
        }

        [TestCase(PawnTradeDetailKind.JoinsAsColonist)]
        [TestCase(PawnTradeDetailKind.JoinsAsSlave)]
        public void ShouldShow_PurchaseOutcomeInCurrentResult_ReturnsTrue(PawnTradeDetailKind kind)
        {
            TradeQueryEntry queryEntry = CreatePawnQueryEntry("Pawn:1", new PawnTradeDetailsSnapshot(kind));

            bool result = TradeDetailsColumnPolicy.ShouldShow(
                new[] { queryEntry },
                PlannerTradeRelevanceProjection.Empty,
                isDetailsSortActive: false);

            Assert.That(result, Is.True);
        }

        [Test]
        public void ShouldShow_RideableInCurrentResult_ReturnsTrue()
        {
            TradeQueryEntry queryEntry = CreatePawnQueryEntry(
                "Pawn:1",
                new PawnTradeDetailsSnapshot(PawnTradeDetailKind.Rideable, 1.6f));

            bool result = TradeDetailsColumnPolicy.ShouldShow(
                new[] { queryEntry },
                PlannerTradeRelevanceProjection.Empty,
                isDetailsSortActive: false);

            Assert.That(result, Is.True);
        }

        [Test]
        public void ShouldShow_RelevanceInCurrentResult_ReturnsTrue()
        {
            TradeQueryEntry queryEntry = CreateItemQueryEntry("Thing:Genepack:1");
            PlannerTradeRelevanceProjection projection = CreateProjection(queryEntry.Entry.Identity);

            bool result = TradeDetailsColumnPolicy.ShouldShow(
                new[] { queryEntry },
                projection,
                isDetailsSortActive: false);

            Assert.That(result, Is.True);
        }

        [Test]
        public void ShouldShow_RelevanceForFilteredOutEntry_ReturnsFalse()
        {
            TradeQueryEntry visibleEntry = CreateItemQueryEntry("Thing:Steel");
            PlannerTradeRelevanceProjection projection = CreateProjection(
                new TradeEntryIdentity("Thing:Genepack:Filtered"));

            bool result = TradeDetailsColumnPolicy.ShouldShow(
                new[] { visibleEntry },
                projection,
                isDetailsSortActive: false);

            Assert.That(result, Is.False);
        }

        [Test]
        public void ShouldShow_ActiveDetailsSortWithoutDetails_ReturnsTrue()
        {
            bool result = TradeDetailsColumnPolicy.ShouldShow(
                Array.Empty<TradeQueryEntry>(),
                PlannerTradeRelevanceProjection.Empty,
                isDetailsSortActive: true);

            Assert.That(result, Is.True);
        }

        [Test]
        public void ShouldShow_DoesNotModifyProjection()
        {
            TradeQueryEntry queryEntry = CreateItemQueryEntry("Thing:Genepack:1");
            PlannerTradeRelevanceProjection projection = CreateProjection(queryEntry.Entry.Identity);

            _ = TradeDetailsColumnPolicy.ShouldShow(new[] { queryEntry }, projection, isDetailsSortActive: false);

            Assert.That(projection.Count, Is.EqualTo(1));
            Assert.That(projection.GetMatchCount(queryEntry.Entry.Identity), Is.EqualTo(1));
        }

        [Test]
        public void ShouldShow_NullArguments_Throw()
        {
            Assert.That(
                (Action)(() => TradeDetailsColumnPolicy.ShouldShow(
                    null,
                    PlannerTradeRelevanceProjection.Empty,
                    isDetailsSortActive: false)),
                Throws.TypeOf<ArgumentNullException>());

            Assert.That(
                (Action)(() => TradeDetailsColumnPolicy.ShouldShow(
                    Array.Empty<TradeQueryEntry>(),
                    null,
                    isDetailsSortActive: false)),
                Throws.TypeOf<ArgumentNullException>());
        }

        private static PlannerTradeRelevanceProjection CreateProjection(TradeEntryIdentity identity)
        {
            return new PlannerTradeRelevanceProjection(
                new[]
                {
                    new PlannerTradeEntryRelevance(
                        identity,
                        new[]
                        {
                            new PlannerGenepackRelevancePlanMatch("Plan:1", "First plan")
                        })
                });
        }

        private static TradeQueryEntry CreateItemQueryEntry(string identity)
        {
            var entry = new TradeEntrySnapshot(
                new TradeEntryIdentity(identity),
                TradeEntryKind.Item,
                TradeCategoryMembership.Items,
                "Item",
                "Item",
                1,
                TradePrice.Negotiated(100f));

            return CreateQueryEntry(entry);
        }

        private static TradeQueryEntry CreatePawnQueryEntry(string identity, PawnTradeDetailsSnapshot pawnDetails)
        {
            var entry = new TradeEntrySnapshot(
                new TradeEntryIdentity(identity),
                TradeEntryKind.Pawn,
                TradeCategoryMembership.None,
                "Human",
                "Ari",
                1,
                TradePrice.Negotiated(100f),
                pawnDetails);

            return CreateQueryEntry(entry);
        }

        private static TradeQueryEntry CreateQueryEntry(TradeEntrySnapshot entry)
        {
            var trader = new TraderSnapshot(
                new TraderIdentity("Trader:1"),
                new SettlementIdentity(1),
                "Trader",
                "Alpha",
                SnapshotAvailability.Available,
                TradeDistance.Reachable(10),
                TradeRestock.Scheduled(2000),
                new[] { entry },
                null);

            return new TradeQueryEntry(trader, entry);
        }
    }
}