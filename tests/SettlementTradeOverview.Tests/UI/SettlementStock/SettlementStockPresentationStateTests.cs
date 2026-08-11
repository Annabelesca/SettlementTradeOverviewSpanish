using System;
using NUnit.Framework;
using SettlementTradeOverview.Cache;
using SettlementTradeOverview.Domain.Categories;
using SettlementTradeOverview.Domain.Identity;
using SettlementTradeOverview.Domain.Snapshots;
using SettlementTradeOverview.UI.SettlementStock;

namespace SettlementTradeOverview.Tests.UI.SettlementStock
{
    [TestFixture]
    public sealed class SettlementStockPresentationStateTests
    {
        [Test]
        public void Resolve_PendingRequest_ReturnsLoading()
        {
            SettlementStockPresentationState result = SettlementStockPresentationStateResolver.Resolve(
                true,
                false,
                TradeInventorySnapshotCacheState.Available,
                CreateTrader(SnapshotAvailability.Available));

            Assert.That(result, Is.EqualTo(SettlementStockPresentationState.Loading));
        }

        [Test]
        public void Resolve_UnexpectedFailure_ReturnsErrorRegardlessOfPendingRequest()
        {
            SettlementStockPresentationState result = SettlementStockPresentationStateResolver.Resolve(
                true,
                true,
                TradeInventorySnapshotCacheState.Available,
                CreateTrader(SnapshotAvailability.Available));

            Assert.That(result, Is.EqualTo(SettlementStockPresentationState.Error));
        }

        [TestCase((int)TradeInventorySnapshotCacheState.NotLoaded, (int)SettlementStockPresentationState.Loading)]
        [TestCase((int)TradeInventorySnapshotCacheState.Loading, (int)SettlementStockPresentationState.Loading)]
        [TestCase((int)TradeInventorySnapshotCacheState.Unavailable, (int)SettlementStockPresentationState.Unavailable)]
        [TestCase((int)TradeInventorySnapshotCacheState.Failed, (int)SettlementStockPresentationState.Error)]
        public void Resolve_NonQueryableCacheState_MapsDirectly(int cacheStateValue, int expectedStateValue)
        {
            var cacheState = (TradeInventorySnapshotCacheState)cacheStateValue;
            var expectedState = (SettlementStockPresentationState)expectedStateValue;

            SettlementStockPresentationState result = SettlementStockPresentationStateResolver.Resolve(
                false,
                false,
                cacheState,
                null);

            Assert.That(result, Is.EqualTo(expectedState));
        }

        [Test]
        public void Resolve_MissingSelectedTrader_ReturnsUnavailable()
        {
            SettlementStockPresentationState result = SettlementStockPresentationStateResolver.Resolve(
                false,
                false,
                TradeInventorySnapshotCacheState.Available,
                null);

            Assert.That(result, Is.EqualTo(SettlementStockPresentationState.Unavailable));
        }

        [Test]
        public void Resolve_SelectedAvailableTraderInPartialInventory_ReturnsAvailable()
        {
            SettlementStockPresentationState result = SettlementStockPresentationStateResolver.Resolve(
                false,
                false,
                TradeInventorySnapshotCacheState.Partial,
                CreateTrader(SnapshotAvailability.Available));

            Assert.That(result, Is.EqualTo(SettlementStockPresentationState.Available));
        }

        [TestCase(SnapshotAvailability.Available, (int)SettlementStockPresentationState.Available)]
        [TestCase(SnapshotAvailability.Empty, (int)SettlementStockPresentationState.Empty)]
        [TestCase(SnapshotAvailability.Unavailable, (int)SettlementStockPresentationState.Unavailable)]
        [TestCase(SnapshotAvailability.Partial, (int)SettlementStockPresentationState.Partial)]
        [TestCase(SnapshotAvailability.Failed, (int)SettlementStockPresentationState.Error)]
        public void Resolve_QueryableCacheState_MapsSelectedTraderAvailability(
            SnapshotAvailability availability,
            int expectedStateValue)
        {
            var expectedState = (SettlementStockPresentationState)expectedStateValue;

            SettlementStockPresentationState result = SettlementStockPresentationStateResolver.Resolve(
                false,
                false,
                TradeInventorySnapshotCacheState.Partial,
                CreateTrader(availability));

            Assert.That(result, Is.EqualTo(expectedState));
        }

        [Test]
        public void Resolve_InvalidCacheState_Throws()
        {
            Assert.That(
                (Action)(() => SettlementStockPresentationStateResolver.Resolve(
                    false,
                    false,
                    (TradeInventorySnapshotCacheState)100,
                    null)),
                Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        private static TraderSnapshot CreateTrader(SnapshotAvailability availability)
        {
            TradeEntrySnapshot[] entries;

            switch (availability)
            {
                case SnapshotAvailability.Available:
                case SnapshotAvailability.Partial:
                    entries = new[]
                    {
                        new TradeEntrySnapshot(
                            new TradeEntryIdentity("Steel"),
                            TradeEntryKind.Item,
                            TradeCategoryMembership.Items,
                            "Steel",
                            "Steel",
                            1,
                            TradePrice.Negotiated(10f))
                    };
                    break;

                case SnapshotAvailability.Empty:
                case SnapshotAvailability.Unavailable:
                case SnapshotAvailability.Failed:
                    entries = Array.Empty<TradeEntrySnapshot>();
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(availability));
            }

            return new TraderSnapshot(
                new TraderIdentity("Trader:1"),
                new SettlementIdentity(1),
                "Trader",
                "Alpha",
                availability,
                TradeDistance.Reachable(10),
                TradeRestock.Scheduled(2000),
                entries,
                null);
        }
    }
}