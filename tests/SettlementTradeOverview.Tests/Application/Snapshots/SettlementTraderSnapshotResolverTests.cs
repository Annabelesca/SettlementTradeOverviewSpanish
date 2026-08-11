using System;
using NUnit.Framework;
using SettlementTradeOverview.Application.Snapshots;
using SettlementTradeOverview.Domain.Categories;
using SettlementTradeOverview.Domain.Identity;
using SettlementTradeOverview.Domain.Snapshots;

namespace SettlementTradeOverview.Tests.Application.Snapshots
{
    [TestFixture]
    public sealed class SettlementTraderSnapshotResolverTests
    {
        [Test]
        public void TryResolve_MatchingSettlement_ReturnsTrader()
        {
            TraderSnapshot first = CreateTrader("Trader:1", 1, "Alpha", SnapshotAvailability.Available);
            TraderSnapshot second = CreateTrader("Trader:2", 2, "Beta", SnapshotAvailability.Empty);
            TradeInventorySnapshot snapshot = CreateInventory(first, second);

            bool resolved = SettlementTraderSnapshotResolver.TryResolve(
                snapshot,
                new SettlementIdentity(2),
                out TraderSnapshot trader);

            Assert.That(resolved, Is.True);
            Assert.That(trader, Is.SameAs(second));
        }

        [Test]
        public void TryResolve_TraderOrderDoesNotAffectIdentitySelection()
        {
            TraderSnapshot first = CreateTrader("Trader:1", 1, "Alpha", SnapshotAvailability.Empty);
            TraderSnapshot second = CreateTrader("Trader:2", 2, "Beta", SnapshotAvailability.Available);
            TradeInventorySnapshot snapshot = CreateInventory(second, first);

            bool resolved = SettlementTraderSnapshotResolver.TryResolve(
                snapshot,
                new SettlementIdentity(1),
                out TraderSnapshot trader);

            Assert.That(resolved, Is.True);
            Assert.That(trader, Is.SameAs(first));
        }

        [Test]
        public void TryResolve_MissingSettlement_ReturnsFalse()
        {
            TradeInventorySnapshot snapshot = CreateInventory(
                CreateTrader("Trader:1", 1, "Alpha", SnapshotAvailability.Available));

            bool resolved = SettlementTraderSnapshotResolver.TryResolve(
                snapshot,
                new SettlementIdentity(99),
                out TraderSnapshot trader);

            Assert.That(resolved, Is.False);
            Assert.That(trader, Is.Null);
        }

        [TestCase(SnapshotAvailability.Available)]
        [TestCase(SnapshotAvailability.Empty)]
        [TestCase(SnapshotAvailability.Partial)]
        [TestCase(SnapshotAvailability.Failed)]
        public void TryResolve_PreservesTraderAvailability(SnapshotAvailability availability)
        {
            TraderSnapshot expected = CreateTrader("Trader:1", 1, "Alpha", availability);
            TradeInventorySnapshot snapshot = CreateInventory(expected);

            bool resolved = SettlementTraderSnapshotResolver.TryResolve(
                snapshot,
                new SettlementIdentity(1),
                out TraderSnapshot trader);

            Assert.That(resolved, Is.True);
            Assert.That(trader, Is.SameAs(expected));
            Assert.That(trader.Availability, Is.EqualTo(availability));
        }

        [Test]
        public void TryResolve_DoesNotModifySnapshot()
        {
            TraderSnapshot expected = CreateTrader("Trader:1", 1, "Alpha", SnapshotAvailability.Available);
            TradeInventorySnapshot snapshot = CreateInventory(expected);

            _ = SettlementTraderSnapshotResolver.TryResolve(snapshot, new SettlementIdentity(1), out _);

            Assert.That(snapshot.TraderCount, Is.EqualTo(1));
            Assert.That(snapshot.Traders[0], Is.SameAs(expected));
        }

        [Test]
        public void TryResolve_NullArguments_Throw()
        {
            TradeInventorySnapshot snapshot = CreateInventory(
                CreateTrader("Trader:1", 1, "Alpha", SnapshotAvailability.Available));

            Assert.That(
                (Action)(() => SettlementTraderSnapshotResolver.TryResolve(null, new SettlementIdentity(1), out _)),
                Throws.TypeOf<ArgumentNullException>());

            Assert.That(
                (Action)(() => SettlementTraderSnapshotResolver.TryResolve(snapshot, null, out _)),
                Throws.TypeOf<ArgumentNullException>());
        }

        private static TradeInventorySnapshot CreateInventory(params TraderSnapshot[] traders)
        {
            SnapshotAvailability availability = SnapshotAvailability.Partial;

            return new TradeInventorySnapshot(availability, 1000, 10, null, traders);
        }

        private static TraderSnapshot CreateTrader(
            string traderId,
            int settlementId,
            string settlementLabel,
            SnapshotAvailability availability)
        {
            TradeEntrySnapshot[] entries;

            switch (availability)
            {
                case SnapshotAvailability.Available:
                case SnapshotAvailability.Partial:
                    entries = new[]
                    {
                        new TradeEntrySnapshot(
                            new TradeEntryIdentity(traderId + ":Steel"),
                            TradeEntryKind.Item,
                            TradeCategoryMembership.Items,
                            "Steel",
                            "Steel",
                            1,
                            TradePrice.Negotiated(10f))
                    };
                    break;

                case SnapshotAvailability.Empty:
                case SnapshotAvailability.Failed:
                    entries = Array.Empty<TradeEntrySnapshot>();
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(availability));
            }

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
    }
}