using System;
using NUnit.Framework;
using SettlementTradeOverview.Cache;
using SettlementTradeOverview.UI.GlobalOverview;

namespace SettlementTradeOverview.Tests.UI.GlobalOverview
{
    [TestFixture]
    public sealed class GlobalOverviewPresentationStateTests
    {
        [Test]
        public void Resolve_SnapshotRequestPending_ReturnsLoading()
        {
            GlobalOverviewPresentationState result = GlobalOverviewPresentationStateResolver.Resolve(
                true,
                false,
                TradeInventorySnapshotCacheState.NotLoaded);

            Assert.That(result, Is.EqualTo(GlobalOverviewPresentationState.Loading));
        }

        [Test]
        public void Resolve_RefreshPending_OverridesAvailableCacheState()
        {
            GlobalOverviewPresentationState result = GlobalOverviewPresentationStateResolver.Resolve(
                true,
                false,
                TradeInventorySnapshotCacheState.Available);

            Assert.That(result, Is.EqualTo(GlobalOverviewPresentationState.Loading));
        }

        [TestCase((int)TradeInventorySnapshotCacheState.NotLoaded, (int)GlobalOverviewPresentationState.Loading)]
        [TestCase((int)TradeInventorySnapshotCacheState.Loading, (int)GlobalOverviewPresentationState.Loading)]
        [TestCase((int)TradeInventorySnapshotCacheState.Available, (int)GlobalOverviewPresentationState.Available)]
        [TestCase((int)TradeInventorySnapshotCacheState.Empty, (int)GlobalOverviewPresentationState.Empty)]
        [TestCase((int)TradeInventorySnapshotCacheState.Unavailable, (int)GlobalOverviewPresentationState.Unavailable)]
        [TestCase((int)TradeInventorySnapshotCacheState.Partial, (int)GlobalOverviewPresentationState.Partial)]
        [TestCase((int)TradeInventorySnapshotCacheState.Failed, (int)GlobalOverviewPresentationState.Error)]
        public void Resolve_CompletedSnapshotRequest_MapsCacheState(int cacheStateValue, int expectedStateValue)
        {
            var cacheState = (TradeInventorySnapshotCacheState)cacheStateValue;
            var expectedState = (GlobalOverviewPresentationState)expectedStateValue;

            GlobalOverviewPresentationState result = GlobalOverviewPresentationStateResolver.Resolve(
                false,
                false,
                cacheState);

            Assert.That(result, Is.EqualTo(expectedState));
        }

        [Test]
        public void Resolve_UnexpectedLoadFailure_ReturnsErrorRegardlessOfCacheState()
        {
            GlobalOverviewPresentationState result = GlobalOverviewPresentationStateResolver.Resolve(
                true,
                true,
                TradeInventorySnapshotCacheState.Available);

            Assert.That(result, Is.EqualTo(GlobalOverviewPresentationState.Error));
        }

        [Test]
        public void Resolve_InvalidCacheState_Throws()
        {
            Assert.That(
                (Action)(() =>
                {
                    _ = GlobalOverviewPresentationStateResolver.Resolve(
                        false,
                        false,
                        (TradeInventorySnapshotCacheState)100);
                }),
                Throws.TypeOf<ArgumentOutOfRangeException>());
        }
    }
}