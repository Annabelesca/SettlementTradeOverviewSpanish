using System;
using NUnit.Framework;
using SettlementTradeOverview.Domain.Eligibility;

namespace SettlementTradeOverview.Tests.Domain.Eligibility
{
    [TestFixture]
    public sealed class SettlementEligibilityFactsTests
    {
        [Test]
        public void Constructor_PreservesSuppliedFacts()
        {
            var facts = new SettlementEligibilityFacts(
                isPlayerOwned: true,
                isTradeAvailable: false,
                isHostileToPlayer: true,
                technologyLevel: SettlementTechnologyLevel.Spacer,
                hasPoweredCommsConsole: false,
                distanceInTiles: 17,
                reachability: SettlementReachabilityState.Unreachable,
                isRoyaltyActive: true,
                royaltyTradePermission: SettlementRoyaltyTradePermissionState.Denied);

            Assert.That(facts.IsPlayerOwned, Is.True);
            Assert.That(facts.IsTradeAvailable, Is.False);
            Assert.That(facts.IsHostileToPlayer, Is.True);

            Assert.That(facts.TechnologyLevel, Is.EqualTo(SettlementTechnologyLevel.Spacer));

            Assert.That(facts.HasPoweredCommsConsole, Is.False);
            Assert.That(facts.DistanceInTiles, Is.EqualTo(17));

            Assert.That(facts.Reachability, Is.EqualTo(SettlementReachabilityState.Unreachable));

            Assert.That(facts.IsRoyaltyActive, Is.True);

            Assert.That(facts.RoyaltyTradePermission, Is.EqualTo(SettlementRoyaltyTradePermissionState.Denied));
        }

        [Test]
        public void Constructor_InactiveRoyalty_AllowsUnavailablePermissionFact()
        {
            var facts = new SettlementEligibilityFacts(
                isPlayerOwned: false,
                isTradeAvailable: true,
                isHostileToPlayer: false,
                technologyLevel: SettlementTechnologyLevel.Industrial,
                hasPoweredCommsConsole: true,
                distanceInTiles: 10,
                reachability: SettlementReachabilityState.Reachable,
                isRoyaltyActive: false,
                royaltyTradePermission: SettlementRoyaltyTradePermissionState.Unavailable);

            Assert.That(facts.IsRoyaltyActive, Is.False);

            Assert.That(facts.RoyaltyTradePermission, Is.EqualTo(SettlementRoyaltyTradePermissionState.Unavailable));
        }

        [Test]
        public void Constructor_NegativeDistance_Throws()
        {
            Assert.That(
                (Action)(() => { _ = CreateFacts(distanceInTiles: -1); }),
                Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void Constructor_InvalidTechnologyLevel_Throws()
        {
            Assert.That(
                (Action)(() => { _ = CreateFacts(technologyLevel: (SettlementTechnologyLevel)100); }),
                Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void Constructor_InvalidReachability_Throws()
        {
            Assert.That(
                (Action)(() => { _ = CreateFacts(reachability: (SettlementReachabilityState)100); }),
                Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void Constructor_InvalidRoyaltyPermission_Throws()
        {
            Assert.That(
                (Action)(() =>
                {
                    _ = CreateFacts(royaltyTradePermission: (SettlementRoyaltyTradePermissionState)100);
                }),
                Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        private static SettlementEligibilityFacts CreateFacts(
            SettlementTechnologyLevel technologyLevel = SettlementTechnologyLevel.Industrial,
            int? distanceInTiles = 10,
            SettlementReachabilityState reachability = SettlementReachabilityState.Reachable,
            SettlementRoyaltyTradePermissionState royaltyTradePermission =
                SettlementRoyaltyTradePermissionState.NotApplicable)
        {
            return new SettlementEligibilityFacts(
                isPlayerOwned: false,
                isTradeAvailable: true,
                isHostileToPlayer: false,
                technologyLevel: technologyLevel,
                hasPoweredCommsConsole: true,
                distanceInTiles: distanceInTiles,
                reachability: reachability,
                isRoyaltyActive: false,
                royaltyTradePermission: royaltyTradePermission);
        }
    }
}