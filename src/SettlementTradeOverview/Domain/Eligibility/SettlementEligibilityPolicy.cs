using System;

namespace SettlementTradeOverview.Domain.Eligibility
{
    public static class SettlementEligibilityPolicy
    {
        public static SettlementEligibilityResult Evaluate(
            SettlementEligibilityFacts facts,
            SettlementEligibilityCriteria criteria)
        {
            if (facts == null)
                throw new ArgumentNullException(nameof(facts));

            if (criteria == null)
                throw new ArgumentNullException(nameof(criteria));

            if (facts.IsPlayerOwned)
            {
                return SettlementEligibilityResult.Ineligible(SettlementEligibilityFailureReason.PlayerOwned);
            }

            if (!facts.IsTradeAvailable)
            {
                return SettlementEligibilityResult.Ineligible(SettlementEligibilityFailureReason.TradeUnavailable);
            }

            if (facts.IsHostileToPlayer)
            {
                return SettlementEligibilityResult.Ineligible(SettlementEligibilityFailureReason.Hostile);
            }

            SettlementEligibilityResult technologyResult = EvaluateTechnology(facts, criteria);

            if (!technologyResult.IsEligible)
                return technologyResult;

            if (criteria.RequirePoweredCommsConsole && !facts.HasPoweredCommsConsole)
            {
                return SettlementEligibilityResult.Ineligible(
                    SettlementEligibilityFailureReason.PoweredCommsConsoleRequired);
            }

            SettlementEligibilityResult distanceResult = EvaluateDistance(facts, criteria);

            if (!distanceResult.IsEligible)
                return distanceResult;

            SettlementEligibilityResult reachabilityResult = EvaluateReachability(facts, criteria);

            if (!reachabilityResult.IsEligible)
                return reachabilityResult;

            SettlementEligibilityResult royaltyResult = EvaluateRoyalty(facts, criteria);

            if (!royaltyResult.IsEligible)
                return royaltyResult;

            return SettlementEligibilityResult.Eligible;
        }

        private static SettlementEligibilityResult EvaluateTechnology(
            SettlementEligibilityFacts facts,
            SettlementEligibilityCriteria criteria)
        {
            if (!criteria.MinimumTechnologyLevel.HasValue)
                return SettlementEligibilityResult.Eligible;

            if (facts.TechnologyLevel == SettlementTechnologyLevel.Unavailable)
            {
                return SettlementEligibilityResult.Ineligible(SettlementEligibilityFailureReason.TechnologyUnavailable);
            }

            if (facts.TechnologyLevel < criteria.MinimumTechnologyLevel.Value)
            {
                return SettlementEligibilityResult.Ineligible(
                    SettlementEligibilityFailureReason.TechnologyBelowMinimum);
            }

            return SettlementEligibilityResult.Eligible;
        }

        private static SettlementEligibilityResult EvaluateDistance(
            SettlementEligibilityFacts facts,
            SettlementEligibilityCriteria criteria)
        {
            if (!criteria.MaximumDistanceInTiles.HasValue)
                return SettlementEligibilityResult.Eligible;

            if (!facts.DistanceInTiles.HasValue)
            {
                return SettlementEligibilityResult.Ineligible(SettlementEligibilityFailureReason.DistanceUnavailable);
            }

            if (facts.DistanceInTiles.Value > criteria.MaximumDistanceInTiles.Value)
            {
                return SettlementEligibilityResult.Ineligible(SettlementEligibilityFailureReason.BeyondMaximumDistance);
            }

            return SettlementEligibilityResult.Eligible;
        }

        private static SettlementEligibilityResult EvaluateReachability(
            SettlementEligibilityFacts facts,
            SettlementEligibilityCriteria criteria)
        {
            if (!criteria.RequireReachable)
                return SettlementEligibilityResult.Eligible;

            switch (facts.Reachability)
            {
                case SettlementReachabilityState.Reachable:
                    return SettlementEligibilityResult.Eligible;

                case SettlementReachabilityState.Unavailable:
                    return SettlementEligibilityResult.Ineligible(
                        SettlementEligibilityFailureReason.ReachabilityUnavailable);

                case SettlementReachabilityState.Unreachable:
                    return SettlementEligibilityResult.Ineligible(SettlementEligibilityFailureReason.Unreachable);

                default:
                    throw new ArgumentOutOfRangeException(nameof(facts.Reachability));
            }
        }

        private static SettlementEligibilityResult EvaluateRoyalty(
            SettlementEligibilityFacts facts,
            SettlementEligibilityCriteria criteria)
        {
            if (!criteria.RequireRoyaltyTradePermission || !facts.IsRoyaltyActive)
            {
                return SettlementEligibilityResult.Eligible;
            }

            switch (facts.RoyaltyTradePermission)
            {
                case SettlementRoyaltyTradePermissionState.NotApplicable:
                case SettlementRoyaltyTradePermissionState.Allowed:
                    return SettlementEligibilityResult.Eligible;

                case SettlementRoyaltyTradePermissionState.Denied:
                    return SettlementEligibilityResult.Ineligible(
                        SettlementEligibilityFailureReason.RoyaltyTradePermissionDenied);

                case SettlementRoyaltyTradePermissionState.Unavailable:
                    return SettlementEligibilityResult.Ineligible(
                        SettlementEligibilityFailureReason.RoyaltyTradePermissionUnavailable);

                default:
                    throw new ArgumentOutOfRangeException(nameof(facts.RoyaltyTradePermission));
            }
        }
    }
}