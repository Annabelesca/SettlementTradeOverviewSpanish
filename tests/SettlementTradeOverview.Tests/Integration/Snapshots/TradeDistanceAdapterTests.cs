using NUnit.Framework;
using SettlementTradeOverview.Domain.Eligibility;
using SettlementTradeOverview.Domain.Snapshots;
using SettlementTradeOverview.Integration.Snapshots;

namespace SettlementTradeOverview.Tests.Integration.Snapshots
{
    [TestFixture]
    public sealed class TradeDistanceAdapterTests
    {
        [Test]
        public void Resolve_EligibilityDistanceAndReachableState_ReturnsReachable()
        {
            TradeDistance result = TradeDistanceStateResolver.Resolve(12, 20, SettlementReachabilityState.Reachable);

            Assert.That(result.RouteState, Is.EqualTo(TradeRouteState.Reachable));
            Assert.That(result.Tiles, Is.EqualTo(12));
        }

        [Test]
        public void Resolve_MissingEligibilityDistance_UsesCalculatedDistance()
        {
            TradeDistance result = TradeDistanceStateResolver.Resolve(null, 20, SettlementReachabilityState.Reachable);

            Assert.That(result.RouteState, Is.EqualTo(TradeRouteState.Reachable));
            Assert.That(result.Tiles, Is.EqualTo(20));
        }

        [Test]
        public void Resolve_UnreachableState_PreservesEligibilityDistance()
        {
            TradeDistance result = TradeDistanceStateResolver.Resolve(12, 20, SettlementReachabilityState.Unreachable);

            Assert.That(result.RouteState, Is.EqualTo(TradeRouteState.Unreachable));
            Assert.That(result.Tiles, Is.EqualTo(12));
        }

        [Test]
        public void Resolve_UnreachableState_UsesCalculatedDistanceWhenEligibilityDistanceIsMissing()
        {
            TradeDistance result = TradeDistanceStateResolver.Resolve(
                null,
                20,
                SettlementReachabilityState.Unreachable);

            Assert.That(result.RouteState, Is.EqualTo(TradeRouteState.Unreachable));
            Assert.That(result.Tiles, Is.EqualTo(20));
        }

        [Test]
        public void Resolve_UnavailableReachability_PreservesKnownDistance()
        {
            TradeDistance result = TradeDistanceStateResolver.Resolve(
                12,
                null,
                SettlementReachabilityState.Unavailable);

            Assert.That(result.RouteState, Is.EqualTo(TradeRouteState.Unavailable));
            Assert.That(result.Tiles, Is.EqualTo(12));
        }

        [Test]
        public void Resolve_ReachableWithoutValidDistance_ReturnsUnavailable()
        {
            TradeDistance result = TradeDistanceStateResolver.Resolve(
                null,
                null,
                SettlementReachabilityState.Reachable);

            Assert.That(result.RouteState, Is.EqualTo(TradeRouteState.Unavailable));
            Assert.That(result.HasTileDistance, Is.False);
        }

        [Test]
        public void Resolve_NegativeCalculatedDistance_ReturnsUnavailable()
        {
            TradeDistance result = TradeDistanceStateResolver.Resolve(null, -1, SettlementReachabilityState.Reachable);

            Assert.That(result.RouteState, Is.EqualTo(TradeRouteState.Unavailable));
            Assert.That(result.HasTileDistance, Is.False);
        }
    }
}