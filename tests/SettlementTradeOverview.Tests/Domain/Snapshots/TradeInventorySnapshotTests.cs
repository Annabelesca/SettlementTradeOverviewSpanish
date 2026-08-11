using System;
using System.Collections.Generic;
using NUnit.Framework;
using SettlementTradeOverview.Domain.Categories;
using SettlementTradeOverview.Domain.Identity;
using SettlementTradeOverview.Domain.Snapshots;

namespace SettlementTradeOverview.Tests.Domain.Snapshots
{
    [TestFixture]
    public sealed class TradeInventorySnapshotTests
    {
        [Test]
        public void Constructor_PreservesTraderOrderAndCalculatesTotals()
        {
            TraderSnapshot first = CreateAvailableTrader("Settlement:1", 1, "Alpha", "Steel");

            TraderSnapshot second = CreateAvailableTrader("Settlement:2", 2, "Beta", "Gold", "MedicineIndustrial");

            var snapshot = new TradeInventorySnapshot(
                SnapshotAvailability.Available,
                120000,
                15,
                null,
                new[]
                {
                    first,
                    second
                });

            Assert.That(snapshot.Traders[0], Is.SameAs(first));
            Assert.That(snapshot.Traders[1], Is.SameAs(second));
            Assert.That(snapshot.TraderCount, Is.EqualTo(2));
            Assert.That(snapshot.EntryCount, Is.EqualTo(3));
        }

        [Test]
        public void Constructor_DefensivelyCopiesMutableTraderList()
        {
            TraderSnapshot first = CreateAvailableTrader("Settlement:1", 1, "Alpha", "Steel");

            var traders = new List<TraderSnapshot>
            {
                first
            };

            var snapshot = new TradeInventorySnapshot(SnapshotAvailability.Available, 120000, 15, null, traders);

            traders[0] = CreateAvailableTrader("Settlement:2", 2, "Beta", "Gold");

            traders.Clear();

            Assert.That(snapshot.TraderCount, Is.EqualTo(1));
            Assert.That(snapshot.Traders[0], Is.SameAs(first));
        }

        [Test]
        public void Traders_ReturnedCollectionCannotBeModified()
        {
            var snapshot = new TradeInventorySnapshot(
                SnapshotAvailability.Available,
                120000,
                15,
                null,
                new[]
                {
                    CreateAvailableTrader("Settlement:1", 1, "Alpha", "Steel")
                });

            var mutableView = (IList<TraderSnapshot>)snapshot.Traders;

            Assert.That((Action)(mutableView.Clear), Throws.TypeOf<NotSupportedException>());
        }

        [Test]
        public void Constructor_PreservesCaptureContextAndNegotiator()
        {
            var negotiator = new TradeNegotiatorSnapshot("Pawn:7", "Alex", 0.18f);

            var snapshot = new TradeInventorySnapshot(
                SnapshotAvailability.Available,
                120000,
                15,
                negotiator,
                new[]
                {
                    CreateAvailableTrader("Settlement:1", 1, "Alpha", "Steel")
                });

            Assert.That(snapshot.CapturedAtTick, Is.EqualTo(120000));
            Assert.That(snapshot.OriginTile, Is.EqualTo(15));
            Assert.That(snapshot.Negotiator, Is.SameAs(negotiator));
        }

        [Test]
        public void Constructor_WithoutNegotiator_IsValid()
        {
            var snapshot = new TradeInventorySnapshot(
                SnapshotAvailability.Available,
                120000,
                null,
                null,
                new[]
                {
                    CreateAvailableTrader("Settlement:1", 1, "Alpha", "Steel")
                });

            Assert.That(snapshot.OriginTile, Is.Null);
            Assert.That(snapshot.Negotiator, Is.Null);
        }

        [Test]
        public void EmptySnapshot_CanContainSuccessfullyCheckedEmptyTraders()
        {
            var snapshot = new TradeInventorySnapshot(
                SnapshotAvailability.Empty,
                120000,
                15,
                null,
                new[]
                {
                    CreateEmptyTrader("Settlement:1", 1, "Alpha"),
                    CreateEmptyTrader("Settlement:2", 2, "Beta")
                });

            Assert.That(snapshot.TraderCount, Is.EqualTo(2));
            Assert.That(snapshot.EntryCount, Is.Zero);
        }

        [Test]
        public void EmptySnapshot_CannotContainStockData()
        {
            Assert.That(
                (Action)(() =>
                {
                    _ = new TradeInventorySnapshot(
                        SnapshotAvailability.Empty,
                        120000,
                        15,
                        null,
                        new[]
                        {
                            CreateAvailableTrader("Settlement:1", 1, "Alpha", "Steel")
                        });
                }),
                Throws.TypeOf<ArgumentException>());
        }

        [TestCase(SnapshotAvailability.Unavailable)]
        [TestCase(SnapshotAvailability.Failed)]
        public void UnavailableOrFailedSnapshot_CannotContainTraders(SnapshotAvailability availability)
        {
            Assert.That(
                (Action)(() =>
                {
                    _ = new TradeInventorySnapshot(
                        availability,
                        120000,
                        15,
                        null,
                        new[]
                        {
                            CreateEmptyTrader("Settlement:1", 1, "Alpha")
                        });
                }),
                Throws.TypeOf<ArgumentException>());
        }

        [Test]
        public void AvailableSnapshot_RequiresStockData()
        {
            Assert.That(
                (Action)(() =>
                {
                    _ = new TradeInventorySnapshot(
                        SnapshotAvailability.Available,
                        120000,
                        15,
                        null,
                        new[]
                        {
                            CreateEmptyTrader("Settlement:1", 1, "Alpha")
                        });
                }),
                Throws.TypeOf<ArgumentException>());
        }

        [Test]
        public void EquivalentInputs_ProduceEquivalentObservableValues()
        {
            TradeInventorySnapshot first = CreateEquivalentSnapshot();
            TradeInventorySnapshot second = CreateEquivalentSnapshot();

            Assert.That(first.Availability, Is.EqualTo(second.Availability));
            Assert.That(first.CapturedAtTick, Is.EqualTo(second.CapturedAtTick));
            Assert.That(first.OriginTile, Is.EqualTo(second.OriginTile));
            Assert.That(first.TraderCount, Is.EqualTo(second.TraderCount));
            Assert.That(first.EntryCount, Is.EqualTo(second.EntryCount));

            Assert.That(first.Traders[0].TraderIdentity, Is.EqualTo(second.Traders[0].TraderIdentity));

            Assert.That(first.Traders[0].Entries[0].Identity, Is.EqualTo(second.Traders[0].Entries[0].Identity));
        }

        [Test]
        public void Constructor_NegativeCaptureTick_Throws()
        {
            Assert.That(
                (Action)(() =>
                {
                    _ = new TradeInventorySnapshot(
                        SnapshotAvailability.Empty,
                        -1,
                        null,
                        null,
                        Array.Empty<TraderSnapshot>());
                }),
                Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void Constructor_NegativeOriginTile_Throws()
        {
            Assert.That(
                (Action)(() =>
                {
                    _ = new TradeInventorySnapshot(
                        SnapshotAvailability.Empty,
                        0,
                        -1,
                        null,
                        Array.Empty<TraderSnapshot>());
                }),
                Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void Constructor_NullTrader_Throws()
        {
            Assert.That(
                (Action)(() =>
                {
                    _ = new TradeInventorySnapshot(
                        SnapshotAvailability.Partial,
                        0,
                        null,
                        null,
                        new TraderSnapshot[]
                        {
                            null
                        });
                }),
                Throws.TypeOf<ArgumentException>());
        }

        private static TradeInventorySnapshot CreateEquivalentSnapshot()
        {
            return new TradeInventorySnapshot(
                SnapshotAvailability.Available,
                120000,
                15,
                new TradeNegotiatorSnapshot("Pawn:7", "Alex", 0.18f),
                new[]
                {
                    CreateAvailableTrader("Settlement:1", 1, "Alpha", "Steel")
                });
        }

        private static TraderSnapshot CreateAvailableTrader(
            string traderId,
            int settlementId,
            string settlementLabel,
            params string[] definitionNames)
        {
            var entries = new TradeEntrySnapshot[definitionNames.Length];

            for (var index = 0; index < definitionNames.Length; index++)
            {
                string definitionName = definitionNames[index];

                entries[index] = new TradeEntrySnapshot(
                    new TradeEntryIdentity(definitionName),
                    TradeEntryKind.Item,
                    TradeCategoryMembership.Items,
                    definitionName,
                    definitionName,
                    1,
                    TradePrice.Negotiated(10f));
            }

            return new TraderSnapshot(
                new TraderIdentity(traderId),
                new SettlementIdentity(settlementId),
                "Trader",
                settlementLabel,
                SnapshotAvailability.Available,
                TradeDistance.Reachable(10),
                TradeRestock.Scheduled(150000),
                entries,
                null);
        }

        private static TraderSnapshot CreateEmptyTrader(string traderId, int settlementId, string settlementLabel)
        {
            return new TraderSnapshot(
                new TraderIdentity(traderId),
                new SettlementIdentity(settlementId),
                "Trader",
                settlementLabel,
                SnapshotAvailability.Empty,
                TradeDistance.Reachable(10),
                TradeRestock.Scheduled(150000),
                Array.Empty<TradeEntrySnapshot>(),
                null);
        }
    }
}