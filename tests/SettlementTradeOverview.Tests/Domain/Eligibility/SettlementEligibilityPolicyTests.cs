using System;
using NUnit.Framework;
using SettlementTradeOverview.Domain.Eligibility;

namespace SettlementTradeOverview.Tests.Domain.Eligibility
{
    [TestFixture]
    public sealed class SettlementEligibilityPolicyTests
    {
        [Test]
        public void Evaluate_DefaultEligibleFacts_ReturnsEligibleResult()
        {
            SettlementEligibilityResult result = SettlementEligibilityPolicy.Evaluate(
                CreateEligibleFacts(),
                SettlementEligibilityCriteria.Default);

            AssertEligible(result);
        }

        [Test]
        public void Evaluate_PlayerOwnedSettlement_ReturnsPlayerOwned()
        {
            SettlementEligibilityResult result = SettlementEligibilityPolicy.Evaluate(
                CreateEligibleFacts(isPlayerOwned: true),
                SettlementEligibilityCriteria.Default);

            AssertIneligible(result, SettlementEligibilityFailureReason.PlayerOwned);
        }

        [Test]
        public void Evaluate_TradeUnavailable_ReturnsTradeUnavailable()
        {
            SettlementEligibilityResult result = SettlementEligibilityPolicy.Evaluate(
                CreateEligibleFacts(isTradeAvailable: false),
                SettlementEligibilityCriteria.Default);

            AssertIneligible(result, SettlementEligibilityFailureReason.TradeUnavailable);
        }

        [Test]
        public void Evaluate_HostileSettlement_ReturnsHostile()
        {
            SettlementEligibilityResult result = SettlementEligibilityPolicy.Evaluate(
                CreateEligibleFacts(isHostileToPlayer: true),
                SettlementEligibilityCriteria.Default);

            AssertIneligible(result, SettlementEligibilityFailureReason.Hostile);
        }

        [Test]
        public void Evaluate_HardRulesRemainActiveWhenConfigurableFiltersAreDisabled()
        {
            var criteria = new SettlementEligibilityCriteria(
                requirePoweredCommsConsole: false,
                minimumTechnologyLevel: null,
                maximumDistanceInTiles: null,
                requireReachable: false);

            SettlementEligibilityResult result = SettlementEligibilityPolicy.Evaluate(
                CreateEligibleFacts(isTradeAvailable: false),
                criteria);

            AssertIneligible(result, SettlementEligibilityFailureReason.TradeUnavailable);
        }

        [Test]
        public void Evaluate_TechnologyAtMinimum_IsEligible()
        {
            SettlementEligibilityResult result = SettlementEligibilityPolicy.Evaluate(
                CreateEligibleFacts(technologyLevel: SettlementTechnologyLevel.Industrial),
                SettlementEligibilityCriteria.Default);

            AssertEligible(result);
        }

        [Test]
        public void Evaluate_TechnologyAboveMinimum_IsEligible()
        {
            SettlementEligibilityResult result = SettlementEligibilityPolicy.Evaluate(
                CreateEligibleFacts(technologyLevel: SettlementTechnologyLevel.Spacer),
                SettlementEligibilityCriteria.Default);

            AssertEligible(result);
        }

        [Test]
        public void Evaluate_TechnologyBelowMinimum_ReturnsTechnologyBelowMinimum()
        {
            SettlementEligibilityResult result = SettlementEligibilityPolicy.Evaluate(
                CreateEligibleFacts(technologyLevel: SettlementTechnologyLevel.Medieval),
                SettlementEligibilityCriteria.Default);

            AssertIneligible(result, SettlementEligibilityFailureReason.TechnologyBelowMinimum);
        }

        [Test]
        public void Evaluate_UnavailableTechnology_ReturnsTechnologyUnavailable()
        {
            SettlementEligibilityResult result = SettlementEligibilityPolicy.Evaluate(
                CreateEligibleFacts(technologyLevel: SettlementTechnologyLevel.Unavailable),
                SettlementEligibilityCriteria.Default);

            AssertIneligible(result, SettlementEligibilityFailureReason.TechnologyUnavailable);
        }

        [Test]
        public void Evaluate_DisabledTechnologyFilter_IgnoresUnavailableTechnology()
        {
            var criteria = new SettlementEligibilityCriteria(minimumTechnologyLevel: null);

            SettlementEligibilityResult result = SettlementEligibilityPolicy.Evaluate(
                CreateEligibleFacts(technologyLevel: SettlementTechnologyLevel.Unavailable),
                criteria);

            AssertEligible(result);
        }

        [Test]
        public void Evaluate_TechnologyFilter_IsIndependentFromConsoleFilter()
        {
            var criteria = new SettlementEligibilityCriteria(requirePoweredCommsConsole: false);

            SettlementEligibilityResult result = SettlementEligibilityPolicy.Evaluate(
                CreateEligibleFacts(technologyLevel: SettlementTechnologyLevel.Medieval, hasPoweredCommsConsole: false),
                criteria);

            AssertIneligible(result, SettlementEligibilityFailureReason.TechnologyBelowMinimum);
        }

        [Test]
        public void Evaluate_MissingPoweredConsole_ReturnsConsoleRequired()
        {
            SettlementEligibilityResult result = SettlementEligibilityPolicy.Evaluate(
                CreateEligibleFacts(hasPoweredCommsConsole: false),
                SettlementEligibilityCriteria.Default);

            AssertIneligible(result, SettlementEligibilityFailureReason.PoweredCommsConsoleRequired);
        }

        [Test]
        public void Evaluate_DisabledConsoleFilter_IgnoresConsoleState()
        {
            var criteria = new SettlementEligibilityCriteria(requirePoweredCommsConsole: false);

            SettlementEligibilityResult result = SettlementEligibilityPolicy.Evaluate(
                CreateEligibleFacts(hasPoweredCommsConsole: false),
                criteria);

            AssertEligible(result);
        }

        [Test]
        public void Evaluate_DistanceBelowMaximum_IsEligible()
        {
            SettlementEligibilityResult result = SettlementEligibilityPolicy.Evaluate(
                CreateEligibleFacts(distanceInTiles: 39),
                SettlementEligibilityCriteria.Default);

            AssertEligible(result);
        }

        [Test]
        public void Evaluate_DistanceAtMaximum_IsEligible()
        {
            SettlementEligibilityResult result = SettlementEligibilityPolicy.Evaluate(
                CreateEligibleFacts(distanceInTiles: 40),
                SettlementEligibilityCriteria.Default);

            AssertEligible(result);
        }

        [Test]
        public void Evaluate_DistanceAboveMaximum_ReturnsBeyondMaximumDistance()
        {
            SettlementEligibilityResult result = SettlementEligibilityPolicy.Evaluate(
                CreateEligibleFacts(distanceInTiles: 41),
                SettlementEligibilityCriteria.Default);

            AssertIneligible(result, SettlementEligibilityFailureReason.BeyondMaximumDistance);
        }

        [Test]
        public void Evaluate_MissingDistance_ReturnsDistanceUnavailable()
        {
            SettlementEligibilityResult result = SettlementEligibilityPolicy.Evaluate(
                CreateEligibleFacts(distanceInTiles: null),
                SettlementEligibilityCriteria.Default);

            AssertIneligible(result, SettlementEligibilityFailureReason.DistanceUnavailable);
        }

        [Test]
        public void Evaluate_DisabledDistanceFilter_IgnoresMissingDistance()
        {
            var criteria = new SettlementEligibilityCriteria(maximumDistanceInTiles: null);

            SettlementEligibilityResult result = SettlementEligibilityPolicy.Evaluate(
                CreateEligibleFacts(distanceInTiles: null),
                criteria);

            AssertEligible(result);
        }

        [Test]
        public void Evaluate_UnreachableSettlement_ReturnsUnreachable()
        {
            SettlementEligibilityResult result = SettlementEligibilityPolicy.Evaluate(
                CreateEligibleFacts(reachability: SettlementReachabilityState.Unreachable),
                SettlementEligibilityCriteria.Default);

            AssertIneligible(result, SettlementEligibilityFailureReason.Unreachable);
        }

        [Test]
        public void Evaluate_UnavailableReachability_ReturnsReachabilityUnavailable()
        {
            SettlementEligibilityResult result = SettlementEligibilityPolicy.Evaluate(
                CreateEligibleFacts(reachability: SettlementReachabilityState.Unavailable),
                SettlementEligibilityCriteria.Default);

            AssertIneligible(result, SettlementEligibilityFailureReason.ReachabilityUnavailable);
        }

        [Test]
        public void Evaluate_DisabledReachabilityFilter_IgnoresReachability()
        {
            var criteria = new SettlementEligibilityCriteria(requireReachable: false);

            SettlementEligibilityResult result = SettlementEligibilityPolicy.Evaluate(
                CreateEligibleFacts(reachability: SettlementReachabilityState.Unavailable),
                criteria);

            AssertEligible(result);
        }

        [Test]
        public void Evaluate_DistanceFilter_IsIndependentFromReachabilityFilter()
        {
            var criteria = new SettlementEligibilityCriteria(requireReachable: false);

            SettlementEligibilityResult result = SettlementEligibilityPolicy.Evaluate(
                CreateEligibleFacts(distanceInTiles: 41, reachability: SettlementReachabilityState.Unavailable),
                criteria);

            AssertIneligible(result, SettlementEligibilityFailureReason.BeyondMaximumDistance);
        }

        [Test]
        public void Evaluate_InactiveRoyalty_IgnoresDeniedPermission()
        {
            var criteria = new SettlementEligibilityCriteria(requireRoyaltyTradePermission: true);

            SettlementEligibilityResult result = SettlementEligibilityPolicy.Evaluate(
                CreateEligibleFacts(
                    isRoyaltyActive: false,
                    royaltyTradePermission: SettlementRoyaltyTradePermissionState.Denied),
                criteria);

            AssertEligible(result);
        }

        [Test]
        public void Evaluate_DisabledRoyaltyFilter_IgnoresDeniedPermission()
        {
            SettlementEligibilityResult result = SettlementEligibilityPolicy.Evaluate(
                CreateEligibleFacts(
                    isRoyaltyActive: true,
                    royaltyTradePermission: SettlementRoyaltyTradePermissionState.Denied),
                SettlementEligibilityCriteria.Default);

            AssertEligible(result);
        }

        [TestCase(SettlementRoyaltyTradePermissionState.NotApplicable)]
        [TestCase(SettlementRoyaltyTradePermissionState.Allowed)]
        public void Evaluate_AllowedRoyaltyStates_AreEligible(SettlementRoyaltyTradePermissionState permission)
        {
            var criteria = new SettlementEligibilityCriteria(requireRoyaltyTradePermission: true);

            SettlementEligibilityResult result = SettlementEligibilityPolicy.Evaluate(
                CreateEligibleFacts(isRoyaltyActive: true, royaltyTradePermission: permission),
                criteria);

            AssertEligible(result);
        }

        [Test]
        public void Evaluate_DeniedRoyaltyPermission_ReturnsDenied()
        {
            var criteria = new SettlementEligibilityCriteria(requireRoyaltyTradePermission: true);

            SettlementEligibilityResult result = SettlementEligibilityPolicy.Evaluate(
                CreateEligibleFacts(
                    isRoyaltyActive: true,
                    royaltyTradePermission: SettlementRoyaltyTradePermissionState.Denied),
                criteria);

            AssertIneligible(result, SettlementEligibilityFailureReason.RoyaltyTradePermissionDenied);
        }

        [Test]
        public void Evaluate_UnavailableRoyaltyPermission_ReturnsUnavailable()
        {
            var criteria = new SettlementEligibilityCriteria(requireRoyaltyTradePermission: true);

            SettlementEligibilityResult result = SettlementEligibilityPolicy.Evaluate(
                CreateEligibleFacts(
                    isRoyaltyActive: true,
                    royaltyTradePermission: SettlementRoyaltyTradePermissionState.Unavailable),
                criteria);

            AssertIneligible(result, SettlementEligibilityFailureReason.RoyaltyTradePermissionUnavailable);
        }

        [Test]
        public void Evaluate_MultipleHardFailures_ReturnsFirstReason()
        {
            SettlementEligibilityResult result = SettlementEligibilityPolicy.Evaluate(
                CreateEligibleFacts(isPlayerOwned: true, isTradeAvailable: false, isHostileToPlayer: true),
                SettlementEligibilityCriteria.Default);

            AssertIneligible(result, SettlementEligibilityFailureReason.PlayerOwned);
        }

        [Test]
        public void Evaluate_TradeUnavailableTakesPriorityOverHostility()
        {
            SettlementEligibilityResult result = SettlementEligibilityPolicy.Evaluate(
                CreateEligibleFacts(isTradeAvailable: false, isHostileToPlayer: true),
                SettlementEligibilityCriteria.Default);

            AssertIneligible(result, SettlementEligibilityFailureReason.TradeUnavailable);
        }

        [Test]
        public void Evaluate_HostilityTakesPriorityOverConfigurableFailures()
        {
            SettlementEligibilityResult result = SettlementEligibilityPolicy.Evaluate(
                CreateEligibleFacts(
                    isHostileToPlayer: true,
                    technologyLevel: SettlementTechnologyLevel.Unavailable,
                    hasPoweredCommsConsole: false,
                    distanceInTiles: null,
                    reachability: SettlementReachabilityState.Unavailable),
                SettlementEligibilityCriteria.Default);

            AssertIneligible(result, SettlementEligibilityFailureReason.Hostile);
        }

        [Test]
        public void Evaluate_ConfigurableFailuresUseDefinedPriority()
        {
            var criteria = new SettlementEligibilityCriteria(requireRoyaltyTradePermission: true);

            SettlementEligibilityResult result = SettlementEligibilityPolicy.Evaluate(
                CreateEligibleFacts(
                    technologyLevel: SettlementTechnologyLevel.Unavailable,
                    hasPoweredCommsConsole: false,
                    distanceInTiles: null,
                    reachability: SettlementReachabilityState.Unavailable,
                    isRoyaltyActive: true,
                    royaltyTradePermission: SettlementRoyaltyTradePermissionState.Denied),
                criteria);

            AssertIneligible(result, SettlementEligibilityFailureReason.TechnologyUnavailable);
        }

        [Test]
        public void Evaluate_RepeatedEvaluationReturnsEquivalentResult()
        {
            SettlementEligibilityFacts facts = CreateEligibleFacts(distanceInTiles: 41);

            SettlementEligibilityCriteria criteria = SettlementEligibilityCriteria.Default;

            SettlementEligibilityResult first = SettlementEligibilityPolicy.Evaluate(facts, criteria);

            SettlementEligibilityResult second = SettlementEligibilityPolicy.Evaluate(facts, criteria);

            Assert.That(first.IsEligible, Is.EqualTo(second.IsEligible));

            Assert.That(first.FailureReason, Is.EqualTo(second.FailureReason));

            Assert.That(facts.DistanceInTiles, Is.EqualTo(41));
            Assert.That(criteria.MaximumDistanceInTiles, Is.EqualTo(40));
        }

        [Test]
        public void Evaluate_NullArguments_Throw()
        {
            Assert.That(
                (Action)(() =>
                {
                    _ = SettlementEligibilityPolicy.Evaluate(null, SettlementEligibilityCriteria.Default);
                }),
                Throws.TypeOf<ArgumentNullException>());

            Assert.That(
                (Action)(() => { _ = SettlementEligibilityPolicy.Evaluate(CreateEligibleFacts(), null); }),
                Throws.TypeOf<ArgumentNullException>());
        }

        [Test]
        public void Ineligible_InvalidFailureReason_Throws()
        {
            Assert.That(
                (Action)(() =>
                {
                    _ = SettlementEligibilityResult.Ineligible(SettlementEligibilityFailureReason.None);
                }),
                Throws.TypeOf<ArgumentOutOfRangeException>());

            Assert.That(
                (Action)(() =>
                {
                    _ = SettlementEligibilityResult.Ineligible((SettlementEligibilityFailureReason)100);
                }),
                Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        private static SettlementEligibilityFacts CreateEligibleFacts(
            bool isPlayerOwned = false,
            bool isTradeAvailable = true,
            bool isHostileToPlayer = false,
            SettlementTechnologyLevel technologyLevel = SettlementTechnologyLevel.Industrial,
            bool hasPoweredCommsConsole = true,
            int? distanceInTiles = 40,
            SettlementReachabilityState reachability = SettlementReachabilityState.Reachable,
            bool isRoyaltyActive = false,
            SettlementRoyaltyTradePermissionState royaltyTradePermission =
                SettlementRoyaltyTradePermissionState.NotApplicable)
        {
            return new SettlementEligibilityFacts(
                isPlayerOwned,
                isTradeAvailable,
                isHostileToPlayer,
                technologyLevel,
                hasPoweredCommsConsole,
                distanceInTiles,
                reachability,
                isRoyaltyActive,
                royaltyTradePermission);
        }

        private static void AssertEligible(SettlementEligibilityResult result)
        {
            Assert.That(result.IsEligible, Is.True);

            Assert.That(result.FailureReason, Is.EqualTo(SettlementEligibilityFailureReason.None));
        }

        private static void AssertIneligible(
            SettlementEligibilityResult result,
            SettlementEligibilityFailureReason expectedReason)
        {
            Assert.That(result.IsEligible, Is.False);
            Assert.That(result.FailureReason, Is.EqualTo(expectedReason));
        }
    }
}