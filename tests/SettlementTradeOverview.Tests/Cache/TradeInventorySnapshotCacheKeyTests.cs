using System.Collections.Generic;
using NUnit.Framework;
using SettlementTradeOverview.Cache;
using SettlementTradeOverview.Domain.Eligibility;
using SettlementTradeOverview.Domain.Identity;
using SettlementTradeOverview.Domain.Snapshots;

namespace SettlementTradeOverview.Tests.Cache
{
    [TestFixture]
    public sealed class TradeInventorySnapshotCacheKeyTests
    {
        [Test]
        public void EquivalentInputs_ProduceEqualKeysAndHashCodes()
        {
            TradeInventorySnapshotCacheKey first = CreateAvailableKey();
            TradeInventorySnapshotCacheKey second = CreateAvailableKey();

            Assert.That(first.Equals(second), Is.True);
            Assert.That(first.Equals((object)second), Is.True);
            Assert.That(first.GetHashCode(), Is.EqualTo(second.GetHashCode()));
        }

        [Test]
        public void MapOrOriginChange_ChangesKey()
        {
            TradeInventorySnapshotCacheKey baseline = CreateAvailableKey();
            TradeInventorySnapshotCacheKey differentMap = CreateAvailableKey(mapId: 2);
            TradeInventorySnapshotCacheKey differentOrigin = CreateAvailableKey(originTile: 11);

            Assert.That(baseline.Equals(differentMap), Is.False);
            Assert.That(baseline.Equals(differentOrigin), Is.False);
        }

        [Test]
        public void NegotiatorPresenceOrIdentityChange_ChangesKey()
        {
            TradeInventorySnapshotCacheKey withoutNegotiator = CreateAvailableKey();

            TradeInventorySnapshotCacheKey firstNegotiator =
                CreateAvailableKey(negotiator: new TradeNegotiatorSnapshot("Pawn:1", "Alex", 0.1f));

            TradeInventorySnapshotCacheKey secondNegotiator =
                CreateAvailableKey(negotiator: new TradeNegotiatorSnapshot("Pawn:2", "Alex", 0.1f));

            Assert.That(withoutNegotiator.Equals(firstNegotiator), Is.False);
            Assert.That(firstNegotiator.Equals(secondNegotiator), Is.False);
        }

        [Test]
        public void NegotiatorLabelOrModifierChange_ChangesKey()
        {
            TradeInventorySnapshotCacheKey baseline =
                CreateAvailableKey(negotiator: new TradeNegotiatorSnapshot("Pawn:1", "Alex", 0.1f));

            TradeInventorySnapshotCacheKey differentLabel =
                CreateAvailableKey(negotiator: new TradeNegotiatorSnapshot("Pawn:1", "Morgan", 0.1f));

            TradeInventorySnapshotCacheKey differentModifier =
                CreateAvailableKey(negotiator: new TradeNegotiatorSnapshot("Pawn:1", "Alex", 0.2f));

            Assert.That(baseline.Equals(differentLabel), Is.False);
            Assert.That(baseline.Equals(differentModifier), Is.False);
        }

        [Test]
        public void EligibilityCriteriaChanges_ChangeKey()
        {
            TradeInventorySnapshotCacheKey baseline = CreateAvailableKey();

            TradeInventorySnapshotCacheKey consoleDisabled = CreateAvailableKey(
                criteria: new SettlementEligibilityCriteria(requirePoweredCommsConsole: false));

            TradeInventorySnapshotCacheKey technologyDisabled = CreateAvailableKey(
                criteria: new SettlementEligibilityCriteria(minimumTechnologyLevel: null));

            TradeInventorySnapshotCacheKey distanceChanged =
                CreateAvailableKey(criteria: new SettlementEligibilityCriteria(maximumDistanceInTiles: 20));

            TradeInventorySnapshotCacheKey reachabilityDisabled =
                CreateAvailableKey(criteria: new SettlementEligibilityCriteria(requireReachable: false));

            TradeInventorySnapshotCacheKey royaltyEnabled = CreateAvailableKey(
                criteria: new SettlementEligibilityCriteria(requireRoyaltyTradePermission: true));

            Assert.That(baseline.Equals(consoleDisabled), Is.False);
            Assert.That(baseline.Equals(technologyDisabled), Is.False);
            Assert.That(baseline.Equals(distanceChanged), Is.False);
            Assert.That(baseline.Equals(reachabilityDisabled), Is.False);
            Assert.That(baseline.Equals(royaltyEnabled), Is.False);
        }

        [Test]
        public void TraderIdentityContentOrOrderChange_ChangesFullKeyButPreservesReuseKey()
        {
            TradeInventorySnapshotCacheKey baseline = CreateAvailableKey(
                traderIdentities: new[]
                {
                    new TraderIdentity("Settlement:1"),
                    new TraderIdentity("Settlement:2")
                });

            TradeInventorySnapshotCacheKey differentContent = CreateAvailableKey(
                traderIdentities: new[]
                {
                    new TraderIdentity("Settlement:1"),
                    new TraderIdentity("Settlement:3")
                });

            TradeInventorySnapshotCacheKey differentOrder = CreateAvailableKey(
                traderIdentities: new[]
                {
                    new TraderIdentity("Settlement:2"),
                    new TraderIdentity("Settlement:1")
                });

            Assert.That(baseline.Equals(differentContent), Is.False);
            Assert.That(baseline.Equals(differentOrder), Is.False);
            Assert.That(baseline.ReuseKey.Equals(differentContent.ReuseKey), Is.True);
            Assert.That(baseline.ReuseKey.Equals(differentOrder.ReuseKey), Is.True);
        }

        [Test]
        public void DiscoveryFailureCountChange_ChangesFullKeyButPreservesReuseKey()
        {
            TradeInventorySnapshotCacheKey baseline = CreateAvailableKey();
            TradeInventorySnapshotCacheKey withFailure = CreateAvailableKey(discoveryFailureCount: 1);

            Assert.That(baseline.Equals(withFailure), Is.False);
            Assert.That(baseline.ReuseKey.Equals(withFailure.ReuseKey), Is.True);
        }

        [Test]
        public void TraderIdentityCollection_IsDefensivelyCopied()
        {
            var identities = new List<TraderIdentity>
            {
                new TraderIdentity("Settlement:1")
            };

            TradeInventorySnapshotReuseKey reuseKey = CreateReuseKey();

            var key = TradeInventorySnapshotCacheKey.CreateAvailableContext(reuseKey, identities, 0);

            identities[0] = new TraderIdentity("Settlement:2");
            identities.Clear();

            Assert.That(key.TraderIdentities.Count, Is.EqualTo(1));
            Assert.That(key.TraderIdentities[0], Is.EqualTo("Settlement:1"));
        }

        [Test]
        public void UnavailableAndAvailableContextKeys_AreDifferent()
        {
            var unavailable =
                TradeInventorySnapshotCacheKey.CreateUnavailableContext(SettlementEligibilityCriteria.Default);

            TradeInventorySnapshotCacheKey available = CreateAvailableKey();

            Assert.That(unavailable.IsContextAvailable, Is.False);
            Assert.That(available.IsContextAvailable, Is.True);
            Assert.That(unavailable.Equals(available), Is.False);
        }

        private static TradeInventorySnapshotCacheKey CreateAvailableKey(
            int mapId = 1,
            int originTile = 10,
            TradeNegotiatorSnapshot negotiator = null,
            SettlementEligibilityCriteria criteria = null,
            IReadOnlyList<TraderIdentity> traderIdentities = null,
            int discoveryFailureCount = 0,
            bool hasPoweredCommsConsole = true,
            bool isRoyaltyActive = false)
        {
            IReadOnlyList<TraderIdentity> effectiveTraderIdentities = traderIdentities ?? new[]
            {
                new TraderIdentity("Settlement:1")
            };

            TradeInventorySnapshotReuseKey reuseKey = CreateReuseKey(
                mapId,
                originTile,
                negotiator,
                criteria,
                hasPoweredCommsConsole,
                isRoyaltyActive);

            return TradeInventorySnapshotCacheKey.CreateAvailableContext(
                reuseKey,
                effectiveTraderIdentities,
                discoveryFailureCount);
        }

        private static TradeInventorySnapshotReuseKey CreateReuseKey(
            int mapId = 1,
            int originTile = 10,
            TradeNegotiatorSnapshot negotiator = null,
            SettlementEligibilityCriteria criteria = null,
            bool hasPoweredCommsConsole = true,
            bool isRoyaltyActive = false)
        {
            return TradeInventorySnapshotReuseKey.CreateAvailableContext(
                mapId,
                originTile,
                negotiator,
                criteria ?? SettlementEligibilityCriteria.Default,
                hasPoweredCommsConsole,
                isRoyaltyActive);
        }
    }
}