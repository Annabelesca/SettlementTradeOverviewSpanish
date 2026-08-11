using System;
using System.Collections.Generic;
using NUnit.Framework;
using SettlementTradeOverview.Domain.Identity;
using SettlementTradeOverview.Integration.Planner;

namespace SettlementTradeOverview.Tests.Integration.Planner
{
    [TestFixture]
    public sealed class PlannerTradeRelevanceProjectionTests
    {
        [Test]
        public void EntryRelevance_ValidInput_PreservesIdentityAndMatchOrder()
        {
            var identity = new TradeEntryIdentity("Thing:Genepack:1");
            PlannerGenepackRelevancePlanMatch first = CreateMatch("Plan:1", "First");
            PlannerGenepackRelevancePlanMatch second = CreateMatch("Plan:2", "Second");

            var relevance = new PlannerTradeEntryRelevance(identity, new[] { first, second });

            Assert.That(relevance.EntryIdentity, Is.SameAs(identity));
            Assert.That(relevance.MatchCount, Is.EqualTo(2));
            Assert.That(relevance.Matches[0], Is.SameAs(first));
            Assert.That(relevance.Matches[1], Is.SameAs(second));
            Assert.That(relevance.Matches[0].PlanId, Is.EqualTo("Plan:1"));
            Assert.That(relevance.Matches[1].DisplayName, Is.EqualTo("Second"));
        }

        [Test]
        public void EntryRelevance_MutableSourceCollection_IsDefensivelyCopied()
        {
            var matches = new List<PlannerGenepackRelevancePlanMatch>
            {
                CreateMatch("Plan:1", "First")
            };

            var relevance = new PlannerTradeEntryRelevance(new TradeEntryIdentity("Thing:Genepack:1"), matches);

            matches[0] = CreateMatch("Plan:2", "Second");
            matches.Clear();

            Assert.That(relevance.MatchCount, Is.EqualTo(1));
            Assert.That(relevance.Matches[0].PlanId, Is.EqualTo("Plan:1"));
        }

        [Test]
        public void EntryRelevance_ExposedMatchCollection_IsReadOnly()
        {
            var relevance = new PlannerTradeEntryRelevance(
                new TradeEntryIdentity("Thing:Genepack:1"),
                new[] { CreateMatch("Plan:1", "First") });

            var exposedMatches = (IList<PlannerGenepackRelevancePlanMatch>)relevance.Matches;

            Assert.That(
                (Action)(() => exposedMatches.Add(CreateMatch("Plan:2", "Second"))),
                Throws.TypeOf<NotSupportedException>());
        }

        [Test]
        public void EntryRelevance_NullIdentity_Throws()
        {
            Assert.That(
                (Action)(() => { _ = new PlannerTradeEntryRelevance(null, new[] { CreateMatch("Plan:1", "First") }); }),
                Throws.TypeOf<ArgumentNullException>());
        }

        [Test]
        public void EntryRelevance_NullMatchCollection_Throws()
        {
            Assert.That(
                (Action)(() =>
                {
                    _ = new PlannerTradeEntryRelevance(new TradeEntryIdentity("Thing:Genepack:1"), null);
                }),
                Throws.TypeOf<ArgumentNullException>());
        }

        [Test]
        public void EntryRelevance_EmptyMatchCollection_Throws()
        {
            Assert.That(
                (Action)(() =>
                {
                    _ = new PlannerTradeEntryRelevance(
                        new TradeEntryIdentity("Thing:Genepack:1"),
                        Array.Empty<PlannerGenepackRelevancePlanMatch>());
                }),
                Throws.TypeOf<ArgumentException>());
        }

        [Test]
        public void EntryRelevance_NullMatch_Throws()
        {
            Assert.That(
                (Action)(() =>
                {
                    _ = new PlannerTradeEntryRelevance(
                        new TradeEntryIdentity("Thing:Genepack:1"),
                        new PlannerGenepackRelevancePlanMatch[] { null });
                }),
                Throws.TypeOf<ArgumentException>());
        }

        [Test]
        public void Projection_EquivalentIdentity_ResolvesStoredRelevance()
        {
            var stored = new PlannerTradeEntryRelevance(
                new TradeEntryIdentity("Thing:Genepack:1"),
                new[] { CreateMatch("Plan:1", "First") });

            var projection = new PlannerTradeRelevanceProjection(new[] { stored });

            bool found = projection.TryGet(
                new TradeEntryIdentity("Thing:Genepack:1"),
                out PlannerTradeEntryRelevance resolved);

            Assert.That(found, Is.True);
            Assert.That(resolved, Is.SameAs(stored));
            Assert.That(projection.Count, Is.EqualTo(1));
        }

        [Test]
        public void Projection_MissingIdentity_ReturnsFalse()
        {
            var projection = new PlannerTradeRelevanceProjection(
                new[]
                {
                    new PlannerTradeEntryRelevance(
                        new TradeEntryIdentity("Thing:Genepack:1"),
                        new[] { CreateMatch("Plan:1", "First") })
                });

            bool found = projection.TryGet(
                new TradeEntryIdentity("Thing:Genepack:2"),
                out PlannerTradeEntryRelevance relevance);

            Assert.That(found, Is.False);
            Assert.That(relevance, Is.Null);
        }

        [Test]
        public void Projection_Empty_HasNoEntries()
        {
            Assert.That(PlannerTradeRelevanceProjection.Empty.Count, Is.Zero);

            bool found = PlannerTradeRelevanceProjection.Empty.TryGet(
                new TradeEntryIdentity("Thing:Genepack:1"),
                out PlannerTradeEntryRelevance relevance);

            Assert.That(found, Is.False);
            Assert.That(relevance, Is.Null);
        }

        [Test]
        public void Projection_MutableSourceCollection_IsDefensivelyCopied()
        {
            var stored = new PlannerTradeEntryRelevance(
                new TradeEntryIdentity("Thing:Genepack:1"),
                new[] { CreateMatch("Plan:1", "First") });

            var entries = new List<PlannerTradeEntryRelevance> { stored };
            var projection = new PlannerTradeRelevanceProjection(entries);

            entries[0] = new PlannerTradeEntryRelevance(
                new TradeEntryIdentity("Thing:Genepack:2"),
                new[] { CreateMatch("Plan:2", "Second") });
            entries.Clear();

            Assert.That(projection.Count, Is.EqualTo(1));
            Assert.That(projection.TryGet(new TradeEntryIdentity("Thing:Genepack:1"), out _), Is.True);
        }

        [Test]
        public void Projection_DuplicateEntryIdentity_Throws()
        {
            var first = new PlannerTradeEntryRelevance(
                new TradeEntryIdentity("Thing:Genepack:1"),
                new[] { CreateMatch("Plan:1", "First") });

            var second = new PlannerTradeEntryRelevance(
                new TradeEntryIdentity("Thing:Genepack:1"),
                new[] { CreateMatch("Plan:2", "Second") });

            Assert.That(
                (Action)(() => { _ = new PlannerTradeRelevanceProjection(new[] { first, second }); }),
                Throws.TypeOf<ArgumentException>());
        }

        [Test]
        public void Projection_NullCollectionOrEntry_Throws()
        {
            Assert.That(
                (Action)(() => { _ = new PlannerTradeRelevanceProjection(null); }),
                Throws.TypeOf<ArgumentNullException>());

            Assert.That(
                (Action)(() => { _ = new PlannerTradeRelevanceProjection(new PlannerTradeEntryRelevance[] { null }); }),
                Throws.TypeOf<ArgumentException>());
        }

        [Test]
        public void Projection_NullLookupIdentity_Throws()
        {
            Assert.That(
                (Action)(() => PlannerTradeRelevanceProjection.Empty.TryGet(null, out _)),
                Throws.TypeOf<ArgumentNullException>());
        }

        [Test]
        public void Projection_GetMatchCount_ExistingIdentity_ReturnsStoredCount()
        {
            var projection = new PlannerTradeRelevanceProjection(
                new[]
                {
                    new PlannerTradeEntryRelevance(
                        new TradeEntryIdentity("Thing:Genepack:1"),
                        new[]
                        {
                            CreateMatch("Plan:1", "First"),
                            CreateMatch("Plan:2", "Second")
                        })
                });

            int? count = projection.GetMatchCount(new TradeEntryIdentity("Thing:Genepack:1"));

            Assert.That(count, Is.EqualTo(2));
            Assert.That(projection.Count, Is.EqualTo(1));
        }

        [Test]
        public void Projection_GetMatchCount_MissingIdentity_ReturnsNull()
        {
            var projection = new PlannerTradeRelevanceProjection(
                new[]
                {
                    new PlannerTradeEntryRelevance(
                        new TradeEntryIdentity("Thing:Genepack:1"),
                        new[] { CreateMatch("Plan:1", "First") })
                });

            int? count = projection.GetMatchCount(new TradeEntryIdentity("Thing:Genepack:2"));

            Assert.That(count, Is.Null);
            Assert.That(projection.Count, Is.EqualTo(1));
        }

        [Test]
        public void Projection_GetMatchCount_NullIdentity_Throws()
        {
            Assert.That(
                (Action)(() => PlannerTradeRelevanceProjection.Empty.GetMatchCount(null)),
                Throws.TypeOf<ArgumentNullException>());
        }

        private static PlannerGenepackRelevancePlanMatch CreateMatch(string planId, string displayName)
        {
            return new PlannerGenepackRelevancePlanMatch(planId, displayName);
        }
    }
}