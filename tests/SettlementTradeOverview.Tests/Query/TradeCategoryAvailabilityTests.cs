using System;
using System.Collections.Generic;
using NUnit.Framework;
using SettlementTradeOverview.Domain.Categories;
using SettlementTradeOverview.Domain.Identity;
using SettlementTradeOverview.Domain.Snapshots;
using SettlementTradeOverview.Query;

namespace SettlementTradeOverview.Tests.Query
{
    [TestFixture]
    public sealed class TradeCategoryAvailabilityTests
    {
        [Test]
        public void Resolve_GlobalSnapshot_ReturnsAllAndOccupiedCategoriesInStableOrder()
        {
            TraderSnapshot trader = CreateTrader(
                "Trader:1",
                1,
                "Alpha",
                SnapshotAvailability.Available,
                CreateItem("Rifle", "Assault rifle", TradeCategoryMembership.Weapons),
                CreateItem("Meal", "Fine meal", TradeCategoryMembership.Foods),
                CreatePawn("Pawn:1", "Morgan"),
                CreateItem("Oddity", "Oddity", TradeCategoryMembership.None));

            TradeInventorySnapshot snapshot = CreateInventory(trader);

            IReadOnlyList<TradeCategory> result = TradeCategoryAvailability.Resolve(snapshot, string.Empty);

            Assert.That(
                result,
                Is.EqualTo(
                    new[]
                    {
                        TradeCategory.All,
                        TradeCategory.Foods,
                        TradeCategory.Weapons,
                        TradeCategory.Pawns,
                        TradeCategory.Other
                    }));
        }

        [Test]
        public void Resolve_SearchFiltersAvailableCategoriesBeforeActiveCategoryIsApplied()
        {
            TraderSnapshot trader = CreateTrader(
                "Trader:1",
                1,
                "Alpha",
                SnapshotAvailability.Available,
                CreateItem("Rifle", "Assault rifle", TradeCategoryMembership.Weapons),
                CreateItem("Meal", "Fine meal", TradeCategoryMembership.Foods),
                CreateItem("Component", "Advanced component", TradeCategoryMembership.Manufactured));

            IReadOnlyList<TradeCategory> result = TradeCategoryAvailability.Resolve(trader, "rifle");

            Assert.That(
                result,
                Is.EqualTo(
                    new[]
                    {
                        TradeCategory.All,
                        TradeCategory.Weapons
                    }));
        }

        [Test]
        public void Resolve_SearchNormalizesWhitespaceAndIgnoresCase()
        {
            TraderSnapshot trader = CreateTrader(
                "Trader:1",
                1,
                "Alpha",
                SnapshotAvailability.Available,
                CreateItem("Rifle", "Assault rifle", TradeCategoryMembership.Weapons),
                CreateItem("Meal", "Fine meal", TradeCategoryMembership.Foods));

            IReadOnlyList<TradeCategory> result = TradeCategoryAvailability.Resolve(trader, "  RIFLE  ");

            Assert.That(
                result,
                Is.EqualTo(
                    new[]
                    {
                        TradeCategory.All,
                        TradeCategory.Weapons
                    }));
        }

        [Test]
        public void Resolve_GlobalSearchBySettlementIncludesCategoriesFromMatchingSettlement()
        {
            TraderSnapshot alpha = CreateTrader(
                "Trader:1",
                1,
                "Alpha Colony",
                SnapshotAvailability.Available,
                CreateItem("Meal", "Fine meal", TradeCategoryMembership.Foods),
                CreateItem("Rifle", "Assault rifle", TradeCategoryMembership.Weapons));

            TraderSnapshot beta = CreateTrader(
                "Trader:2",
                2,
                "Beta Outpost",
                SnapshotAvailability.Available,
                CreateItem("Steel", "Steel", TradeCategoryMembership.ResourcesRaw));

            TradeInventorySnapshot snapshot = CreateInventory(alpha, beta);

            IReadOnlyList<TradeCategory> result = TradeCategoryAvailability.Resolve(snapshot, "alpha");

            Assert.That(
                result,
                Is.EqualTo(
                    new[]
                    {
                        TradeCategory.All,
                        TradeCategory.Foods,
                        TradeCategory.Weapons
                    }));
        }

        [Test]
        public void Resolve_NoMatches_ReturnsAllOnly()
        {
            TraderSnapshot trader = CreateTrader(
                "Trader:1",
                1,
                "Alpha",
                SnapshotAvailability.Available,
                CreateItem("Steel", "Steel", TradeCategoryMembership.ResourcesRaw));

            IReadOnlyList<TradeCategory> result = TradeCategoryAvailability.Resolve(trader, "medicine");

            Assert.That(result, Is.EqualTo(new[] { TradeCategory.All }));
        }

        [Test]
        public void Resolve_CurrencyOnlyTrader_DoesNotCreateCategory()
        {
            TraderSnapshot trader = CreateCurrencyOnlyTrader();

            IReadOnlyList<TradeCategory> result = TradeCategoryAvailability.Resolve(trader, string.Empty);

            Assert.That(result, Is.EqualTo(new[] { TradeCategory.All }));
        }

        [Test]
        public void Resolve_GlobalSnapshot_IgnoresFailedTrader()
        {
            TraderSnapshot available = CreateTrader(
                "Trader:1",
                1,
                "Alpha",
                SnapshotAvailability.Available,
                CreateItem("Steel", "Steel", TradeCategoryMembership.ResourcesRaw));

            TraderSnapshot failed = CreateTrader("Trader:2", 2, "Beta", SnapshotAvailability.Failed);

            TradeInventorySnapshot snapshot = CreateInventory(available, failed);

            IReadOnlyList<TradeCategory> result = TradeCategoryAvailability.Resolve(snapshot, string.Empty);

            Assert.That(
                result,
                Is.EqualTo(
                    new[]
                    {
                        TradeCategory.All,
                        TradeCategory.ResourcesRaw
                    }));
        }

        [Test]
        public void Resolve_RepeatedCallReturnsEquivalentResult()
        {
            TraderSnapshot trader = CreateTrader(
                "Trader:1",
                1,
                "Alpha",
                SnapshotAvailability.Available,
                CreateItem("Steel", "Steel", TradeCategoryMembership.ResourcesRaw));

            IReadOnlyList<TradeCategory> first = TradeCategoryAvailability.Resolve(trader, string.Empty);
            IReadOnlyList<TradeCategory> second = TradeCategoryAvailability.Resolve(trader, string.Empty);

            Assert.That(first, Is.EqualTo(second));
        }

        [Test]
        public void Contains_ReturnsWhetherCategoryIsAvailable()
        {
            TradeCategory[] categories = new[]
            {
                TradeCategory.All,
                TradeCategory.Items
            };

            Assert.That(TradeCategoryAvailability.Contains(categories, TradeCategory.Items), Is.True);
            Assert.That(TradeCategoryAvailability.Contains(categories, TradeCategory.Weapons), Is.False);
        }

        [Test]
        public void Resolve_NullSnapshotOrTrader_Throws()
        {
            Assert.That(
                (Action)(() => TradeCategoryAvailability.Resolve((TradeInventorySnapshot)null, string.Empty)),
                Throws.TypeOf<ArgumentNullException>());

            Assert.That(
                (Action)(() => TradeCategoryAvailability.Resolve((TraderSnapshot)null, string.Empty)),
                Throws.TypeOf<ArgumentNullException>());
        }

        private static TradeInventorySnapshot CreateInventory(params TraderSnapshot[] traders)
        {
            return new TradeInventorySnapshot(SnapshotAvailability.Partial, 1000, 10, null, traders);
        }

        private static TraderSnapshot CreateTrader(
            string traderId,
            int settlementId,
            string settlementLabel,
            SnapshotAvailability availability,
            params TradeEntrySnapshot[] entries)
        {
            return new TraderSnapshot(
                new TraderIdentity(traderId),
                new SettlementIdentity(settlementId),
                "Trader",
                settlementLabel,
                availability,
                TradeDistance.Reachable(10),
                TradeRestock.Scheduled(2000),
                entries,
                null);
        }

        private static TraderSnapshot CreateCurrencyOnlyTrader()
        {
            return new TraderSnapshot(
                new TraderIdentity("Trader:1"),
                new SettlementIdentity(1),
                "Trader",
                "Alpha",
                SnapshotAvailability.Available,
                TradeDistance.Reachable(10),
                TradeRestock.Scheduled(2000),
                Array.Empty<TradeEntrySnapshot>(),
                new TradeCurrencySnapshot(new TradeEntryIdentity("Silver"), "Silver", "Silver", 1000));
        }

        private static TradeEntrySnapshot CreateItem(string identity, string label, TradeCategoryMembership membership)
        {
            return new TradeEntrySnapshot(
                new TradeEntryIdentity(identity),
                TradeEntryKind.Item,
                membership,
                identity,
                label,
                1,
                TradePrice.Negotiated(10f));
        }

        private static TradeEntrySnapshot CreatePawn(string identity, string label)
        {
            return new TradeEntrySnapshot(
                new TradeEntryIdentity(identity),
                TradeEntryKind.Pawn,
                TradeCategoryMembership.None,
                "Human",
                label,
                1,
                TradePrice.Negotiated(10f));
        }
    }
}