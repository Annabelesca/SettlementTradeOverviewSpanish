using System;
using NUnit.Framework;
using SettlementTradeOverview.Domain.Categories;
using SettlementTradeOverview.Domain.Identity;
using SettlementTradeOverview.Domain.Snapshots;

namespace SettlementTradeOverview.Tests.Domain.Snapshots
{
    [TestFixture]
    public sealed class TradeEntrySnapshotTests
    {
        [Test]
        public void Constructor_ItemSnapshot_PreservesCopiedValues()
        {
            var identity = new TradeEntryIdentity("Steel");

            var snapshot = new TradeEntrySnapshot(
                identity,
                TradeEntryKind.Item,
                TradeCategoryMembership.Items,
                "Steel",
                "Steel",
                250,
                TradePrice.Negotiated(2.4f));

            Assert.That(snapshot.Identity, Is.SameAs(identity));
            Assert.That(snapshot.Kind, Is.EqualTo(TradeEntryKind.Item));
            Assert.That(snapshot.Category, Is.EqualTo(TradeCategory.Items));
            Assert.That(snapshot.DefinitionName, Is.EqualTo("Steel"));
            Assert.That(snapshot.Label, Is.EqualTo("Steel"));
            Assert.That(snapshot.Count, Is.EqualTo(250));
            Assert.That(snapshot.Price.State, Is.EqualTo(TradePriceState.Negotiated));
            Assert.That(snapshot.PawnDetails, Is.Null);
            Assert.That(snapshot.GenepackComposition, Is.Null);
        }

        [Test]
        public void Constructor_ItemWithGenepackComposition_PreservesComposition()
        {
            var composition = new GenepackCompositionSnapshot(new[] { "GeneA", "GeneB" });

            var snapshot = new TradeEntrySnapshot(
                new TradeEntryIdentity("Genepack:123"),
                TradeEntryKind.Item,
                TradeCategoryMembership.Items,
                "Genepack",
                "Genepack",
                1,
                TradePrice.MarketValueFallback(420f),
                genepackComposition: composition);

            Assert.That(snapshot.GenepackComposition, Is.SameAs(composition));
            Assert.That(snapshot.PawnDetails, Is.Null);
        }

        [Test]
        public void Constructor_PawnSnapshot_UsesSameRuntimeFreeContract()
        {
            var pawnDetails = new PawnTradeDetailsSnapshot(PawnTradeDetailKind.JoinsAsColonist);

            var snapshot = new TradeEntrySnapshot(
                new TradeEntryIdentity("Pawn:123"),
                TradeEntryKind.Pawn,
                TradeCategoryMembership.None,
                "Human",
                "Ari",
                1,
                TradePrice.MarketValueFallback(1450f),
                pawnDetails);

            Assert.That(snapshot.Kind, Is.EqualTo(TradeEntryKind.Pawn));
            Assert.That(snapshot.Category, Is.EqualTo(TradeCategory.Pawns));
            Assert.That(snapshot.DefinitionName, Is.EqualTo("Human"));
            Assert.That(snapshot.Label, Is.EqualTo("Ari"));
            Assert.That(snapshot.Count, Is.EqualTo(1));
            Assert.That(snapshot.PawnDetails, Is.SameAs(pawnDetails));
            Assert.That(snapshot.GenepackComposition, Is.Null);
        }

        [Test]
        public void Constructor_ItemWithPawnDetails_Throws()
        {
            var pawnDetails = new PawnTradeDetailsSnapshot(PawnTradeDetailKind.JoinsAsSlave);

            Assert.That(
                (Action)(() =>
                {
                    _ = new TradeEntrySnapshot(
                        new TradeEntryIdentity("Steel"),
                        TradeEntryKind.Item,
                        TradeCategoryMembership.Items,
                        "Steel",
                        "Steel",
                        1,
                        TradePrice.Unavailable,
                        pawnDetails);
                }),
                Throws.TypeOf<ArgumentException>());
        }

        [Test]
        public void Constructor_PawnWithGenepackComposition_Throws()
        {
            var composition = new GenepackCompositionSnapshot(new[] { "GeneA" });

            Assert.That(
                (Action)(() =>
                {
                    _ = new TradeEntrySnapshot(
                        new TradeEntryIdentity("Pawn:123"),
                        TradeEntryKind.Pawn,
                        TradeCategoryMembership.None,
                        "Human",
                        "Ari",
                        1,
                        TradePrice.Unavailable,
                        genepackComposition: composition);
                }),
                Throws.TypeOf<ArgumentException>());
        }

        [Test]
        public void Constructor_UnknownKind_Throws()
        {
            Assert.That(
                (Action)(() =>
                {
                    _ = new TradeEntrySnapshot(
                        new TradeEntryIdentity("Unknown"),
                        TradeEntryKind.Unknown,
                        TradeCategoryMembership.None,
                        "Unknown",
                        "Unknown",
                        1,
                        TradePrice.Unavailable);
                }),
                Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void Constructor_NonPositiveCount_Throws(int count)
        {
            Assert.That(
                (Action)(() =>
                {
                    _ = new TradeEntrySnapshot(
                        new TradeEntryIdentity("Steel"),
                        TradeEntryKind.Item,
                        TradeCategoryMembership.Items,
                        "Steel",
                        "Steel",
                        count,
                        TradePrice.Unavailable);
                }),
                Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void Constructor_EmptyDefinitionName_Throws(string definitionName)
        {
            Assert.That(
                (Action)(() =>
                {
                    _ = new TradeEntrySnapshot(
                        new TradeEntryIdentity("Steel"),
                        TradeEntryKind.Item,
                        TradeCategoryMembership.Items,
                        definitionName,
                        "Steel",
                        1,
                        TradePrice.Unavailable);
                }),
                Throws.TypeOf<ArgumentException>());
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void Constructor_EmptyLabel_Throws(string label)
        {
            Assert.That(
                (Action)(() =>
                {
                    _ = new TradeEntrySnapshot(
                        new TradeEntryIdentity("Steel"),
                        TradeEntryKind.Item,
                        TradeCategoryMembership.Items,
                        "Steel",
                        label,
                        1,
                        TradePrice.Unavailable);
                }),
                Throws.TypeOf<ArgumentException>());
        }

        [Test]
        public void CurrencySnapshot_PreservesCopiedValuesAndContainsNoPriceState()
        {
            var identity = new TradeEntryIdentity("Silver");

            var currency = new TradeCurrencySnapshot(identity, "Silver", "Silver", 1200);

            Assert.That(currency.Identity, Is.SameAs(identity));
            Assert.That(currency.DefinitionName, Is.EqualTo("Silver"));
            Assert.That(currency.Label, Is.EqualTo("Silver"));
            Assert.That(currency.Count, Is.EqualTo(1200));

            Assert.That(typeof(TradeCurrencySnapshot).GetProperty(nameof(TradeEntrySnapshot.Price)), Is.Null);
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void CurrencySnapshot_NonPositiveCount_Throws(int count)
        {
            Assert.That(
                (Action)(() =>
                {
                    _ = new TradeCurrencySnapshot(new TradeEntryIdentity("Silver"), "Silver", "Silver", count);
                }),
                Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void NegotiatorSnapshot_PreservesCopiedValues()
        {
            var negotiator = new TradeNegotiatorSnapshot("Pawn:42", "Morgan", -0.05f);

            Assert.That(negotiator.PawnId, Is.EqualTo("Pawn:42"));
            Assert.That(negotiator.Label, Is.EqualTo("Morgan"));
            Assert.That(negotiator.TradePriceImprovement, Is.EqualTo(-0.05f));
        }

        [Test]
        public void NegotiatorSnapshot_NonFiniteModifier_Throws()
        {
            Assert.That(
                (Action)(() => { _ = new TradeNegotiatorSnapshot("Pawn:42", "Morgan", float.NaN); }),
                Throws.TypeOf<ArgumentOutOfRangeException>());
        }
    }
}