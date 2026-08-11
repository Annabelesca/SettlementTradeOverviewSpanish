using System;
using NUnit.Framework;
using SettlementTradeOverview.Domain.Categories;
using SettlementTradeOverview.Domain.Snapshots;

namespace SettlementTradeOverview.Tests.Domain.Categories
{
    [TestFixture]
    public sealed class TradeCategoryClassifierTests
    {
        [TestCase(TradeCategoryMembership.Foods, TradeCategory.Foods)]
        [TestCase(TradeCategoryMembership.ResourcesRaw, TradeCategory.ResourcesRaw)]
        [TestCase(TradeCategoryMembership.Manufactured, TradeCategory.Manufactured)]
        [TestCase(TradeCategoryMembership.Apparel, TradeCategory.Apparel)]
        [TestCase(TradeCategoryMembership.Weapons, TradeCategory.Weapons)]
        [TestCase(TradeCategoryMembership.Items, TradeCategory.Items)]
        [TestCase(TradeCategoryMembership.Buildings, TradeCategory.Buildings)]
        [TestCase(TradeCategoryMembership.None, TradeCategory.Other)]
        public void Classify_Item_ReturnsExpectedCategory(TradeCategoryMembership membership, TradeCategory expected)
        {
            TradeCategory result = TradeCategoryClassifier.Classify(TradeEntryKind.Item, membership);

            Assert.That(result, Is.EqualTo(expected));
            Assert.That(result, Is.Not.EqualTo(TradeCategory.All));
        }

        [Test]
        public void Classify_Pawn_AlwaysReturnsPawns()
        {
            TradeCategory result = TradeCategoryClassifier.Classify(
                TradeEntryKind.Pawn,
                TradeCategoryMembership.Foods | TradeCategoryMembership.Weapons | TradeCategoryMembership.Buildings);

            Assert.That(result, Is.EqualTo(TradeCategory.Pawns));
        }

        [TestCase(TradeCategoryMembership.Foods | TradeCategoryMembership.Buildings, TradeCategory.Foods)]
        [TestCase(
            TradeCategoryMembership.ResourcesRaw | TradeCategoryMembership.Manufactured,
            TradeCategory.ResourcesRaw)]
        [TestCase(TradeCategoryMembership.Manufactured | TradeCategoryMembership.Apparel, TradeCategory.Manufactured)]
        [TestCase(TradeCategoryMembership.Apparel | TradeCategoryMembership.Weapons, TradeCategory.Apparel)]
        [TestCase(TradeCategoryMembership.Weapons | TradeCategoryMembership.Items, TradeCategory.Weapons)]
        [TestCase(TradeCategoryMembership.Items | TradeCategoryMembership.Buildings, TradeCategory.Items)]
        public void Classify_MultipleMemberships_UsesDefinedPriority(
            TradeCategoryMembership membership,
            TradeCategory expected)
        {
            TradeCategory result = TradeCategoryClassifier.Classify(TradeEntryKind.Item, membership);

            Assert.That(result, Is.EqualTo(expected));
        }

        [Test]
        public void Classify_UnknownKind_Throws()
        {
            Assert.That(
                (Action)(() =>
                {
                    _ = TradeCategoryClassifier.Classify(TradeEntryKind.Unknown, TradeCategoryMembership.None);
                }),
                Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void Classify_InvalidKindValue_Throws()
        {
            Assert.That(
                (Action)(() =>
                {
                    _ = TradeCategoryClassifier.Classify((TradeEntryKind)100, TradeCategoryMembership.None);
                }),
                Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void Classify_UnknownMembershipBits_Throws()
        {
            Assert.That(
                (Action)(() =>
                {
                    _ = TradeCategoryClassifier.Classify(TradeEntryKind.Item, (TradeCategoryMembership)(1 << 20));
                }),
                Throws.TypeOf<ArgumentOutOfRangeException>());
        }
    }
}