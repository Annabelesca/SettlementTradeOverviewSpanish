using System;
using System.Collections.Generic;
using NUnit.Framework;
using SettlementTradeOverview.Domain.Categories;
using SettlementTradeOverview.Domain.Identity;
using SettlementTradeOverview.Domain.Snapshots;

namespace SettlementTradeOverview.Tests.Domain.Snapshots
{
    [TestFixture]
    public sealed class TraderSnapshotTests
    {
        [Test]
        public void Constructor_PreservesEntryOrderAndMetadata()
        {
            TradeEntrySnapshot first = CreateEntry("Steel");
            TradeEntrySnapshot second = CreateEntry("ComponentIndustrial");

            TraderSnapshot snapshot = CreateTrader(
                SnapshotAvailability.Available,
                new[]
                {
                    first,
                    second
                },
                currency: null,
                distance: TradeDistance.Reachable(18),
                restock: TradeRestock.Scheduled(90000));

            Assert.That(snapshot.Entries[0], Is.SameAs(first));
            Assert.That(snapshot.Entries[1], Is.SameAs(second));
            Assert.That(snapshot.EntryCount, Is.EqualTo(2));

            Assert.That(snapshot.TraderLabel, Is.EqualTo("Exotic goods trader"));
            Assert.That(snapshot.SettlementLabel, Is.EqualTo("New Hope"));

            Assert.That(snapshot.Distance.Tiles, Is.EqualTo(18));
            Assert.That(snapshot.Restock.NextRestockTick, Is.EqualTo(90000));
        }

        [Test]
        public void Constructor_DefensivelyCopiesMutableEntryList()
        {
            TradeEntrySnapshot first = CreateEntry("Steel");
            TradeEntrySnapshot replacement = CreateEntry("Gold");

            var entries = new List<TradeEntrySnapshot>
            {
                first
            };

            TraderSnapshot snapshot = CreateTrader(SnapshotAvailability.Available, entries);

            entries[0] = replacement;
            entries.Clear();

            Assert.That(snapshot.EntryCount, Is.EqualTo(1));
            Assert.That(snapshot.Entries[0], Is.SameAs(first));
        }

        [Test]
        public void Entries_ReturnedCollectionCannotBeModified()
        {
            TraderSnapshot snapshot = CreateTrader(
                SnapshotAvailability.Available,
                new[]
                {
                    CreateEntry("Steel")
                });

            var mutableView = (IList<TradeEntrySnapshot>)snapshot.Entries;

            Assert.That((Action)(() => mutableView.Add(CreateEntry("Gold"))), Throws.TypeOf<NotSupportedException>());
        }

        [Test]
        public void Constructor_NullEntry_Throws()
        {
            Assert.That(
                (Action)(() => CreateTrader(
                    SnapshotAvailability.Available,
                    new TradeEntrySnapshot[]
                    {
                        null
                    })),
                Throws.TypeOf<ArgumentException>());
        }

        [Test]
        public void AvailableSnapshot_RequiresEntryOrCurrency()
        {
            Assert.That(
                (Action)(() => CreateTrader(SnapshotAvailability.Available, Array.Empty<TradeEntrySnapshot>())),
                Throws.TypeOf<ArgumentException>());
        }

        [Test]
        public void AvailableSnapshot_CanContainCurrencyWithoutEntries()
        {
            TraderSnapshot snapshot = CreateTrader(
                SnapshotAvailability.Available,
                Array.Empty<TradeEntrySnapshot>(),
                CreateCurrency());

            Assert.That(snapshot.EntryCount, Is.Zero);
            Assert.That(snapshot.Currency, Is.Not.Null);
        }

        [Test]
        public void EmptySnapshot_RequiresNoStockData()
        {
            TraderSnapshot snapshot = CreateTrader(SnapshotAvailability.Empty, Array.Empty<TradeEntrySnapshot>());

            Assert.That(snapshot.EntryCount, Is.Zero);
            Assert.That(snapshot.Currency, Is.Null);
        }

        [Test]
        public void EmptySnapshot_WithEntries_Throws()
        {
            Assert.That(
                (Action)(() => CreateTrader(
                    SnapshotAvailability.Empty,
                    new[]
                    {
                        CreateEntry("Steel")
                    })),
                Throws.TypeOf<ArgumentException>());
        }

        [Test]
        public void PartialSnapshot_CanPreserveAvailableData()
        {
            TraderSnapshot snapshot = CreateTrader(
                SnapshotAvailability.Partial,
                new[]
                {
                    CreateEntry("Steel")
                },
                CreateCurrency());

            Assert.That(snapshot.Availability, Is.EqualTo(SnapshotAvailability.Partial));
            Assert.That(snapshot.EntryCount, Is.EqualTo(1));
            Assert.That(snapshot.Currency, Is.Not.Null);
        }

        [TestCase(SnapshotAvailability.Unavailable)]
        [TestCase(SnapshotAvailability.Failed)]
        public void UnavailableOrFailedSnapshot_CannotContainStock(SnapshotAvailability availability)
        {
            Assert.That(
                (Action)(() => CreateTrader(
                    availability,
                    new[]
                    {
                        CreateEntry("Steel")
                    })),
                Throws.TypeOf<ArgumentException>());
        }

        private static TraderSnapshot CreateTrader(
            SnapshotAvailability availability,
            IReadOnlyList<TradeEntrySnapshot> entries,
            TradeCurrencySnapshot currency = null,
            TradeDistance? distance = null,
            TradeRestock? restock = null)
        {
            return new TraderSnapshot(
                new TraderIdentity("Settlement:42"),
                new SettlementIdentity(42),
                "Exotic goods trader",
                "New Hope",
                availability,
                distance ?? TradeDistance.Unavailable,
                restock ?? TradeRestock.Unavailable,
                entries,
                currency);
        }

        private static TradeEntrySnapshot CreateEntry(string definitionName)
        {
            return new TradeEntrySnapshot(
                new TradeEntryIdentity(definitionName),
                TradeEntryKind.Item,
                TradeCategoryMembership.Items,
                definitionName,
                definitionName,
                1,
                TradePrice.Negotiated(10f));
        }

        private static TradeCurrencySnapshot CreateCurrency()
        {
            return new TradeCurrencySnapshot(new TradeEntryIdentity("Silver"), "Silver", "Silver", 1000);
        }
    }
}