using NUnit.Framework;
using SettlementTradeOverview.Domain.Categories;
using SettlementTradeOverview.Domain.Identity;
using SettlementTradeOverview.Domain.Snapshots;
using SettlementTradeOverview.Integration.Snapshots;

namespace SettlementTradeOverview.Tests.Integration.Snapshots
{
    [TestFixture]
    public sealed class TradeEntrySnapshotAdapterTests
    {
        [Test]
        public void ResolveKind_ItemAndPawnRemainDistinct()
        {
            Assert.That(TradeEntrySnapshotPolicy.ResolveKind(false), Is.EqualTo(TradeEntryKind.Item));

            Assert.That(TradeEntrySnapshotPolicy.ResolveKind(true), Is.EqualTo(TradeEntryKind.Pawn));
        }

        [Test]
        public void CreateEntryIdentity_ItemAndPawnPrefixesDoNotIntersect()
        {
            string itemIdentity = TradeEntrySnapshotPolicy.CreateEntryIdentity(TradeEntryKind.Item, "Thing_42").Value;

            string pawnIdentity = TradeEntrySnapshotPolicy.CreateEntryIdentity(TradeEntryKind.Pawn, "Thing_42").Value;

            Assert.That(itemIdentity, Is.EqualTo("Thing:Thing_42"));
            Assert.That(pawnIdentity, Is.EqualTo("Pawn:Thing_42"));
            Assert.That(itemIdentity, Is.Not.EqualTo(pawnIdentity));
        }

        [Test]
        public void CreateEntryIdentity_EquivalentInputProducesEquivalentIdentity()
        {
            TradeEntryIdentity first = TradeEntrySnapshotPolicy.CreateEntryIdentity(TradeEntryKind.Item, "Thing_42");

            TradeEntryIdentity second = TradeEntrySnapshotPolicy.CreateEntryIdentity(TradeEntryKind.Item, "Thing_42");

            Assert.That(first, Is.EqualTo(second));
        }

        [Test]
        public void CreateCurrencyIdentity_UsesSeparatePrefix()
        {
            string identity = TradeEntrySnapshotPolicy.CreateCurrencyIdentity("Silver").Value;

            Assert.That(identity, Is.EqualTo("Currency:Silver"));
        }

        [TestCase(true, false, false, true)]
        [TestCase(true, true, false, false)]
        [TestCase(true, false, true, false)]
        [TestCase(false, false, false, false)]
        public void IsPrimaryCurrency_UsesCurrencyFavorAndExistingCurrencyState(
            bool isCurrency,
            bool isFavor,
            bool hasPrimaryCurrency,
            bool expected)
        {
            bool result = TradeEntrySnapshotPolicy.IsPrimaryCurrency(isCurrency, isFavor, hasPrimaryCurrency);

            Assert.That(result, Is.EqualTo(expected));
        }

        [Test]
        public void BuildMembership_NoSupportedCategories_ReturnsNoneAndClassifiesAsOther()
        {
            TradeCategoryMembership membership = TradeEntrySnapshotPolicy.BuildMembership(
                false,
                false,
                false,
                false,
                false,
                false,
                false);

            TradeCategory category = TradeCategoryClassifier.Classify(TradeEntryKind.Item, membership);

            Assert.That(membership, Is.EqualTo(TradeCategoryMembership.None));
            Assert.That(category, Is.EqualTo(TradeCategory.Other));
        }

        [Test]
        public void BuildMembership_PreservesAllMatchingCategoryFlags()
        {
            TradeCategoryMembership membership = TradeEntrySnapshotPolicy.BuildMembership(
                true,
                true,
                false,
                false,
                true,
                false,
                false);

            Assert.That(
                membership,
                Is.EqualTo(
                    TradeCategoryMembership.Foods | TradeCategoryMembership.ResourcesRaw |
                    TradeCategoryMembership.Weapons));

            Assert.That(
                TradeCategoryClassifier.Classify(TradeEntryKind.Item, membership),
                Is.EqualTo(TradeCategory.Foods));
        }
    }
}