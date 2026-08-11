using System;
using System.Collections.Generic;
using NUnit.Framework;
using SettlementTradeOverview.Domain.Categories;
using SettlementTradeOverview.Domain.Identity;
using SettlementTradeOverview.Domain.Snapshots;
using SettlementTradeOverview.Integration.Planner;

namespace SettlementTradeOverview.Tests.Integration.Planner
{
    [TestFixture]
    public sealed class PlannerTradeRelevanceProjectionBuilderTests
    {
        [Test]
        public void Build_NullSnapshotOrTrader_Throws()
        {
            Assert.That(
                (Action)(() => PlannerTradeRelevanceProjectionBuilder.Build(
                    (TradeInventorySnapshot)null,
                    CreateUnavailableQuery())),
                Throws.TypeOf<ArgumentNullException>());

            Assert.That(
                (Action)(() => PlannerTradeRelevanceProjectionBuilder.Build(
                    (TraderSnapshot)null,
                    CreateUnavailableQuery())),
                Throws.TypeOf<ArgumentNullException>());
        }

        [Test]
        public void Build_NullQuery_Throws()
        {
            TradeInventorySnapshot snapshot = CreateInventory(
                CreateTrader("Trader:1", 1, CreateGenepackEntry("Thing:1", "GeneA")));

            TraderSnapshot trader = snapshot.Traders[0];

            Assert.That(
                (Action)(() => PlannerTradeRelevanceProjectionBuilder.Build(snapshot, null)),
                Throws.TypeOf<ArgumentNullException>());

            Assert.That(
                (Action)(() => PlannerTradeRelevanceProjectionBuilder.Build(trader, null)),
                Throws.TypeOf<ArgumentNullException>());
        }

        [Test]
        public void Build_NoCompositions_ReturnsEmptyWithoutCallingQuery()
        {
            TradeInventorySnapshot snapshot = CreateInventory(
                CreateTrader("Trader:1", 1, CreateOrdinaryEntry("Thing:Steel")));

            var queryCallCount = 0;

            PlannerTradeRelevanceProjection projection = PlannerTradeRelevanceProjectionBuilder.Build(
                snapshot,
                _ =>
                {
                    queryCallCount++;
                    return PlannerGenepackRelevanceBatchResult.CreateUnavailable();
                });

            Assert.That(projection, Is.SameAs(PlannerTradeRelevanceProjection.Empty));
            Assert.That(queryCallCount, Is.Zero);
        }

        [Test]
        public void Build_GlobalSnapshot_SkipsOrdinaryEntriesAndPreservesCandidateOrder()
        {
            TradeInventorySnapshot snapshot = CreateInventory(
                CreateTrader(
                    "Trader:1",
                    1,
                    CreateOrdinaryEntry("Thing:Steel"),
                    CreateGenepackEntry("Thing:Pack:1", "GeneB")),
                CreateTrader("Trader:2", 2, CreateGenepackEntry("Thing:Pack:2", "GeneA", "GeneC")));

            IReadOnlyList<GenepackCompositionSnapshot> capturedCompositions = null;

            PlannerTradeRelevanceProjectionBuilder.Build(
                snapshot,
                compositions =>
                {
                    capturedCompositions = compositions;
                    return PlannerGenepackRelevanceBatchResult.CreateSuccess(
                        new[]
                        {
                            PlannerGenepackRelevanceItemResult.CreateSuccess(
                                Array.Empty<PlannerGenepackRelevancePlanMatch>()),
                            PlannerGenepackRelevanceItemResult.CreateSuccess(
                                Array.Empty<PlannerGenepackRelevancePlanMatch>())
                        });
                });

            Assert.That(capturedCompositions, Is.Not.Null);
            Assert.That(capturedCompositions.Count, Is.EqualTo(2));
            Assert.That(capturedCompositions[0].GeneDefNames, Is.EqualTo(new[] { "GeneB" }));
            Assert.That(capturedCompositions[1].GeneDefNames, Is.EqualTo(new[] { "GeneA", "GeneC" }));
        }

        [Test]
        public void Build_TraderSnapshot_ProcessesOnlySelectedTrader()
        {
            TraderSnapshot first = CreateTrader("Trader:1", 1, CreateGenepackEntry("Thing:Pack:1", "GeneA"));

            TraderSnapshot second = CreateTrader("Trader:2", 2, CreateGenepackEntry("Thing:Pack:2", "GeneB"));

            _ = CreateInventory(first, second);

            IReadOnlyList<GenepackCompositionSnapshot> capturedCompositions = null;

            PlannerTradeRelevanceProjectionBuilder.Build(
                second,
                compositions =>
                {
                    capturedCompositions = compositions;
                    return PlannerGenepackRelevanceBatchResult.CreateSuccess(
                        new[]
                        {
                            PlannerGenepackRelevanceItemResult.CreateSuccess(
                                Array.Empty<PlannerGenepackRelevancePlanMatch>())
                        });
                });

            Assert.That(capturedCompositions.Count, Is.EqualTo(1));
            Assert.That(capturedCompositions[0].GeneDefNames, Is.EqualTo(new[] { "GeneB" }));
        }

        [Test]
        public void Build_CandidatesExist_CallsQueryExactlyOnce()
        {
            TradeInventorySnapshot snapshot = CreateInventory(
                CreateTrader(
                    "Trader:1",
                    1,
                    CreateGenepackEntry("Thing:Pack:1", "GeneA"),
                    CreateGenepackEntry("Thing:Pack:2", "GeneB")));

            var queryCallCount = 0;

            PlannerTradeRelevanceProjectionBuilder.Build(
                snapshot,
                _ =>
                {
                    queryCallCount++;
                    return PlannerGenepackRelevanceBatchResult.CreateSuccess(
                        new[]
                        {
                            PlannerGenepackRelevanceItemResult.CreateSuccess(
                                Array.Empty<PlannerGenepackRelevancePlanMatch>()),
                            PlannerGenepackRelevanceItemResult.CreateSuccess(
                                Array.Empty<PlannerGenepackRelevancePlanMatch>())
                        });
                });

            Assert.That(queryCallCount, Is.EqualTo(1));
        }

        [Test]
        public void Build_SuccessfulMatches_AreBoundToCorrectEntryIdentity()
        {
            TradeEntrySnapshot firstEntry = CreateGenepackEntry("Thing:Pack:1", "GeneA");
            TradeEntrySnapshot secondEntry = CreateGenepackEntry("Thing:Pack:2", "GeneB");

            TradeInventorySnapshot snapshot = CreateInventory(CreateTrader("Trader:1", 1, firstEntry, secondEntry));

            PlannerGenepackRelevancePlanMatch firstMatch = CreateMatch("Plan:1", "First");
            PlannerGenepackRelevancePlanMatch secondMatch = CreateMatch("Plan:2", "Second");

            PlannerTradeRelevanceProjection projection = PlannerTradeRelevanceProjectionBuilder.Build(
                snapshot,
                _ => PlannerGenepackRelevanceBatchResult.CreateSuccess(
                    new[]
                    {
                        PlannerGenepackRelevanceItemResult.CreateSuccess(new[] { firstMatch }),
                        PlannerGenepackRelevanceItemResult.CreateSuccess(new[] { secondMatch })
                    }));

            Assert.That(projection.Count, Is.EqualTo(2));
            Assert.That(projection.TryGet(firstEntry.Identity, out PlannerTradeEntryRelevance first), Is.True);
            Assert.That(projection.TryGet(secondEntry.Identity, out PlannerTradeEntryRelevance second), Is.True);
            Assert.That(first.Matches[0], Is.SameAs(firstMatch));
            Assert.That(second.Matches[0], Is.SameAs(secondMatch));
        }

        [Test]
        public void Build_SuccessfulItem_PreservesPlannerMatchOrder()
        {
            TradeEntrySnapshot entry = CreateGenepackEntry("Thing:Pack:1", "GeneA");
            TradeInventorySnapshot snapshot = CreateInventory(CreateTrader("Trader:1", 1, entry));

            PlannerTradeRelevanceProjection projection = PlannerTradeRelevanceProjectionBuilder.Build(
                snapshot,
                _ => PlannerGenepackRelevanceBatchResult.CreateSuccess(
                    new[]
                    {
                        PlannerGenepackRelevanceItemResult.CreateSuccess(
                            new[]
                            {
                                CreateMatch("Plan:2", "Second"),
                                CreateMatch("Plan:1", "First")
                            })
                    }));

            Assert.That(projection.TryGet(entry.Identity, out PlannerTradeEntryRelevance relevance), Is.True);
            Assert.That(relevance.Matches[0].PlanId, Is.EqualTo("Plan:2"));
            Assert.That(relevance.Matches[1].PlanId, Is.EqualTo("Plan:1"));
        }

        [Test]
        public void Build_SuccessfulItemWithoutMatches_DoesNotCreateProjectionEntry()
        {
            TradeEntrySnapshot entry = CreateGenepackEntry("Thing:Pack:1", "GeneA");
            TradeInventorySnapshot snapshot = CreateInventory(CreateTrader("Trader:1", 1, entry));

            PlannerTradeRelevanceProjection projection = PlannerTradeRelevanceProjectionBuilder.Build(
                snapshot,
                _ => PlannerGenepackRelevanceBatchResult.CreateSuccess(
                    new[]
                    {
                        PlannerGenepackRelevanceItemResult.CreateSuccess(
                            Array.Empty<PlannerGenepackRelevancePlanMatch>())
                    }));

            Assert.That(projection.Count, Is.Zero);
            Assert.That(projection.TryGet(entry.Identity, out _), Is.False);
        }

        [Test]
        public void Build_InvalidInputItem_DoesNotRemoveNeighboringSuccess()
        {
            AssertItemFailureIsIsolated(PlannerGenepackRelevanceItemResult.CreateInvalidInput());
        }

        [Test]
        public void Build_UnknownGeneItem_DoesNotRemoveNeighboringSuccess()
        {
            AssertItemFailureIsIsolated(PlannerGenepackRelevanceItemResult.CreateUnknownGeneDef());
        }

        [Test]
        public void Build_FailedItem_DoesNotRemoveNeighboringSuccess()
        {
            AssertItemFailureIsIsolated(PlannerGenepackRelevanceItemResult.CreateFailed());
        }

        [Test]
        public void Build_UnavailableBatch_ReturnsEmpty()
        {
            TradeInventorySnapshot snapshot = CreateInventory(
                CreateTrader("Trader:1", 1, CreateGenepackEntry("Thing:Pack:1", "GeneA")));

            PlannerTradeRelevanceProjection projection = PlannerTradeRelevanceProjectionBuilder.Build(
                snapshot,
                _ => PlannerGenepackRelevanceBatchResult.CreateUnavailable());

            Assert.That(projection, Is.SameAs(PlannerTradeRelevanceProjection.Empty));
        }

        [Test]
        public void Build_NullBatch_ReturnsEmpty()
        {
            TradeInventorySnapshot snapshot = CreateInventory(
                CreateTrader("Trader:1", 1, CreateGenepackEntry("Thing:Pack:1", "GeneA")));

            PlannerTradeRelevanceProjection projection = PlannerTradeRelevanceProjectionBuilder.Build(
                snapshot,
                _ => null);

            Assert.That(projection, Is.SameAs(PlannerTradeRelevanceProjection.Empty));
        }

        [Test]
        public void Build_ResultCountMismatch_ReturnsEmpty()
        {
            TradeInventorySnapshot snapshot = CreateInventory(
                CreateTrader(
                    "Trader:1",
                    1,
                    CreateGenepackEntry("Thing:Pack:1", "GeneA"),
                    CreateGenepackEntry("Thing:Pack:2", "GeneB")));

            PlannerTradeRelevanceProjection projection = PlannerTradeRelevanceProjectionBuilder.Build(
                snapshot,
                _ => PlannerGenepackRelevanceBatchResult.CreateSuccess(
                    new[]
                    {
                        PlannerGenepackRelevanceItemResult.CreateSuccess(
                            Array.Empty<PlannerGenepackRelevancePlanMatch>())
                    }));

            Assert.That(projection, Is.SameAs(PlannerTradeRelevanceProjection.Empty));
        }

        [Test]
        public void Build_QueryException_ReturnsEmpty()
        {
            TradeInventorySnapshot snapshot = CreateInventory(
                CreateTrader("Trader:1", 1, CreateGenepackEntry("Thing:Pack:1", "GeneA")));

            PlannerTradeRelevanceProjection projection = PlannerTradeRelevanceProjectionBuilder.Build(
                snapshot,
                _ => throw new InvalidOperationException("Planner query failed."));

            Assert.That(projection, Is.SameAs(PlannerTradeRelevanceProjection.Empty));
        }

        [Test]
        public void Build_EqualCompositionsOnDifferentRows_CreateSeparateIdentityBindings()
        {
            TradeEntrySnapshot firstEntry = CreateGenepackEntry("Thing:Pack:1", "GeneA", "GeneB");
            TradeEntrySnapshot secondEntry = CreateGenepackEntry("Thing:Pack:2", "GeneA", "GeneB");

            TradeInventorySnapshot snapshot = CreateInventory(CreateTrader("Trader:1", 1, firstEntry, secondEntry));

            PlannerTradeRelevanceProjection projection = PlannerTradeRelevanceProjectionBuilder.Build(
                snapshot,
                _ => PlannerGenepackRelevanceBatchResult.CreateSuccess(
                    new[]
                    {
                        PlannerGenepackRelevanceItemResult.CreateSuccess(new[] { CreateMatch("Plan:1", "First") }),
                        PlannerGenepackRelevanceItemResult.CreateSuccess(new[] { CreateMatch("Plan:1", "First") })
                    }));

            Assert.That(projection.Count, Is.EqualTo(2));
            Assert.That(projection.TryGet(firstEntry.Identity, out _), Is.True);
            Assert.That(projection.TryGet(secondEntry.Identity, out _), Is.True);
        }

        [Test]
        public void Build_DoesNotModifySourceSnapshots()
        {
            TradeEntrySnapshot entry = CreateGenepackEntry("Thing:Pack:1", "GeneA", "GeneB");
            TraderSnapshot trader = CreateTrader("Trader:1", 1, entry);
            TradeInventorySnapshot snapshot = CreateInventory(trader);

            PlannerTradeRelevanceProjectionBuilder.Build(
                snapshot,
                _ => PlannerGenepackRelevanceBatchResult.CreateSuccess(
                    new[]
                    {
                        PlannerGenepackRelevanceItemResult.CreateSuccess(new[] { CreateMatch("Plan:1", "First") })
                    }));

            Assert.That(snapshot.TraderCount, Is.EqualTo(1));
            Assert.That(snapshot.Traders[0], Is.SameAs(trader));
            Assert.That(trader.EntryCount, Is.EqualTo(1));
            Assert.That(trader.Entries[0], Is.SameAs(entry));
            Assert.That(entry.GenepackComposition.GeneDefNames, Is.EqualTo(new[] { "GeneA", "GeneB" }));
        }

        [Test]
        public void Build_RepeatedBuildCanReflectDifferentPlannerResults()
        {
            TradeEntrySnapshot entry = CreateGenepackEntry("Thing:Pack:1", "GeneA");
            TradeInventorySnapshot snapshot = CreateInventory(CreateTrader("Trader:1", 1, entry));

            PlannerTradeRelevanceProjection first = PlannerTradeRelevanceProjectionBuilder.Build(
                snapshot,
                _ => PlannerGenepackRelevanceBatchResult.CreateSuccess(
                    new[]
                    {
                        PlannerGenepackRelevanceItemResult.CreateSuccess(new[] { CreateMatch("Plan:1", "First") })
                    }));

            PlannerTradeRelevanceProjection second = PlannerTradeRelevanceProjectionBuilder.Build(
                snapshot,
                _ => PlannerGenepackRelevanceBatchResult.CreateSuccess(
                    new[]
                    {
                        PlannerGenepackRelevanceItemResult.CreateSuccess(
                            Array.Empty<PlannerGenepackRelevancePlanMatch>())
                    }));

            Assert.That(first.TryGet(entry.Identity, out _), Is.True);
            Assert.That(second.TryGet(entry.Identity, out _), Is.False);
        }

        private static void AssertItemFailureIsIsolated(PlannerGenepackRelevanceItemResult failedItem)
        {
            TradeEntrySnapshot failedEntry = CreateGenepackEntry("Thing:Pack:1", "GeneA");
            TradeEntrySnapshot successfulEntry = CreateGenepackEntry("Thing:Pack:2", "GeneB");

            TradeInventorySnapshot snapshot = CreateInventory(
                CreateTrader("Trader:1", 1, failedEntry, successfulEntry));

            PlannerTradeRelevanceProjection projection = PlannerTradeRelevanceProjectionBuilder.Build(
                snapshot,
                _ => PlannerGenepackRelevanceBatchResult.CreateSuccess(
                    new[]
                    {
                        failedItem,
                        PlannerGenepackRelevanceItemResult.CreateSuccess(new[] { CreateMatch("Plan:2", "Second") })
                    }));

            Assert.That(projection.TryGet(failedEntry.Identity, out _), Is.False);
            Assert.That(projection.TryGet(successfulEntry.Identity, out _), Is.True);
            Assert.That(projection.Count, Is.EqualTo(1));
        }

        private static Func<IReadOnlyList<GenepackCompositionSnapshot>, PlannerGenepackRelevanceBatchResult>
            CreateUnavailableQuery()
        {
            return _ => PlannerGenepackRelevanceBatchResult.CreateUnavailable();
        }

        private static TradeInventorySnapshot CreateInventory(params TraderSnapshot[] traders)
        {
            return new TradeInventorySnapshot(SnapshotAvailability.Available, 1000, 10, null, traders);
        }

        private static TraderSnapshot CreateTrader(
            string traderId,
            int settlementId,
            params TradeEntrySnapshot[] entries)
        {
            return new TraderSnapshot(
                new TraderIdentity(traderId),
                new SettlementIdentity(settlementId),
                "Trader " + settlementId,
                "Settlement " + settlementId,
                SnapshotAvailability.Available,
                TradeDistance.Reachable(settlementId),
                TradeRestock.Scheduled(2000 + settlementId),
                entries,
                null);
        }

        private static TradeEntrySnapshot CreateOrdinaryEntry(string entryId)
        {
            return new TradeEntrySnapshot(
                new TradeEntryIdentity(entryId),
                TradeEntryKind.Item,
                TradeCategoryMembership.Items,
                "Steel",
                "Steel",
                1,
                TradePrice.Negotiated(1f));
        }

        private static TradeEntrySnapshot CreateGenepackEntry(string entryId, params string[] geneDefNames)
        {
            return new TradeEntrySnapshot(
                new TradeEntryIdentity(entryId),
                TradeEntryKind.Item,
                TradeCategoryMembership.Items,
                "Genepack",
                "Genepack",
                1,
                TradePrice.Negotiated(1f),
                genepackComposition: new GenepackCompositionSnapshot(geneDefNames));
        }

        private static PlannerGenepackRelevancePlanMatch CreateMatch(string planId, string displayName)
        {
            return new PlannerGenepackRelevancePlanMatch(planId, displayName);
        }
    }
}