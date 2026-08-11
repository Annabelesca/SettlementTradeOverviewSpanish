using System;
using System.Collections.Generic;
using NUnit.Framework;
using SettlementTradeOverview.Domain.Categories;
using SettlementTradeOverview.Domain.Identity;
using SettlementTradeOverview.Domain.Snapshots;
using SettlementTradeOverview.Integration.Snapshots;

namespace SettlementTradeOverview.Tests.Integration.Snapshots
{
    [TestFixture]
    public sealed class SnapshotAvailabilityResolverTests
    {
        [Test]
        public void ResolveTrader_NoStockAndNoFailures_ReturnsEmpty()
        {
            SnapshotAvailability result = SnapshotAvailabilityResolver.ResolveTrader(false, 0);

            Assert.That(result, Is.EqualTo(SnapshotAvailability.Empty));
        }

        [Test]
        public void ResolveTrader_StockAndNoFailures_ReturnsAvailable()
        {
            SnapshotAvailability result = SnapshotAvailabilityResolver.ResolveTrader(true, 0);

            Assert.That(result, Is.EqualTo(SnapshotAvailability.Available));
        }

        [Test]
        public void ResolveTrader_StockAndFailures_ReturnsPartial()
        {
            SnapshotAvailability result = SnapshotAvailabilityResolver.ResolveTrader(true, 1);

            Assert.That(result, Is.EqualTo(SnapshotAvailability.Partial));
        }

        [Test]
        public void ResolveTrader_NoStockAndFailures_ReturnsFailed()
        {
            SnapshotAvailability result = SnapshotAvailabilityResolver.ResolveTrader(false, 1);

            Assert.That(result, Is.EqualTo(SnapshotAvailability.Failed));
        }

        [Test]
        public void ResolveInventory_AllTradersEmptyAndNoFailures_ReturnsEmpty()
        {
            SnapshotAvailability result = SnapshotAvailabilityResolver.ResolveInventory(
                false,
                new[]
                {
                    CreateEmptyTrader("Trader:1", 1),
                    CreateEmptyTrader("Trader:2", 2)
                });

            Assert.That(result, Is.EqualTo(SnapshotAvailability.Empty));
        }

        [Test]
        public void ResolveInventory_TraderWithEntry_ReturnsAvailable()
        {
            SnapshotAvailability result = SnapshotAvailabilityResolver.ResolveInventory(
                false,
                new[]
                {
                    CreateAvailableTraderWithEntry("Trader:1", 1)
                });

            Assert.That(result, Is.EqualTo(SnapshotAvailability.Available));
        }

        [Test]
        public void ResolveInventory_CurrencyOnlyTrader_ReturnsAvailable()
        {
            SnapshotAvailability result = SnapshotAvailabilityResolver.ResolveInventory(
                false,
                new[]
                {
                    CreateAvailableTraderWithCurrency("Trader:1", 1)
                });

            Assert.That(result, Is.EqualTo(SnapshotAvailability.Available));
        }

        [Test]
        public void ResolveInventory_DiscoveryFailure_ReturnsPartial()
        {
            SnapshotAvailability result = SnapshotAvailabilityResolver.ResolveInventory(
                true,
                Array.Empty<TraderSnapshot>());

            Assert.That(result, Is.EqualTo(SnapshotAvailability.Partial));
        }

        [Test]
        public void ResolveInventory_PartialTrader_ReturnsPartial()
        {
            SnapshotAvailability result = SnapshotAvailabilityResolver.ResolveInventory(
                false,
                new[]
                {
                    CreatePartialTrader("Trader:1", 1)
                });

            Assert.That(result, Is.EqualTo(SnapshotAvailability.Partial));
        }

        [Test]
        public void ResolveInventory_FailedTraderAlongsideAvailableTrader_ReturnsPartial()
        {
            SnapshotAvailability result = SnapshotAvailabilityResolver.ResolveInventory(
                false,
                new[]
                {
                    CreateAvailableTraderWithEntry("Trader:1", 1),
                    CreateFailedTrader("Trader:2", 2)
                });

            Assert.That(result, Is.EqualTo(SnapshotAvailability.Partial));
        }

        [Test]
        public void ResolveInventory_OnlyFailedTraders_ReturnsPartial()
        {
            SnapshotAvailability result = SnapshotAvailabilityResolver.ResolveInventory(
                false,
                new[]
                {
                    CreateFailedTrader("Trader:1", 1),
                    CreateFailedTrader("Trader:2", 2)
                });

            Assert.That(result, Is.EqualTo(SnapshotAvailability.Partial));
        }

        [Test]
        public void ResolveTrader_NegativeFailureCount_Throws()
        {
            Assert.That(
                (Action)(() => { _ = SnapshotAvailabilityResolver.ResolveTrader(false, -1); }),
                Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        private static TraderSnapshot CreateEmptyTrader(string traderId, int settlementId)
        {
            return CreateTrader(
                traderId,
                settlementId,
                SnapshotAvailability.Empty,
                Array.Empty<TradeEntrySnapshot>(),
                null);
        }

        private static TraderSnapshot CreateAvailableTraderWithEntry(string traderId, int settlementId)
        {
            return CreateTrader(
                traderId,
                settlementId,
                SnapshotAvailability.Available,
                new[]
                {
                    CreateEntry(traderId + ":Entry")
                },
                null);
        }

        private static TraderSnapshot CreateAvailableTraderWithCurrency(string traderId, int settlementId)
        {
            return CreateTrader(
                traderId,
                settlementId,
                SnapshotAvailability.Available,
                Array.Empty<TradeEntrySnapshot>(),
                new TradeCurrencySnapshot(new TradeEntryIdentity("Currency:Silver"), "Silver", "Silver", 100));
        }

        private static TraderSnapshot CreatePartialTrader(string traderId, int settlementId)
        {
            return CreateTrader(
                traderId,
                settlementId,
                SnapshotAvailability.Partial,
                new[]
                {
                    CreateEntry(traderId + ":Entry")
                },
                null);
        }

        private static TraderSnapshot CreateFailedTrader(string traderId, int settlementId)
        {
            return CreateTrader(
                traderId,
                settlementId,
                SnapshotAvailability.Failed,
                Array.Empty<TradeEntrySnapshot>(),
                null);
        }

        private static TraderSnapshot CreateTrader(
            string traderId,
            int settlementId,
            SnapshotAvailability availability,
            IReadOnlyList<TradeEntrySnapshot> entries,
            TradeCurrencySnapshot currency)
        {
            return new TraderSnapshot(
                new TraderIdentity(traderId),
                new SettlementIdentity(settlementId),
                "Trader",
                "Settlement",
                availability,
                TradeDistance.Unavailable,
                TradeRestock.Unavailable,
                entries,
                currency);
        }

        private static TradeEntrySnapshot CreateEntry(string identity)
        {
            return new TradeEntrySnapshot(
                new TradeEntryIdentity(identity),
                TradeEntryKind.Item,
                TradeCategoryMembership.Items,
                "Steel",
                "Steel",
                1,
                TradePrice.Negotiated(1f));
        }
    }
}