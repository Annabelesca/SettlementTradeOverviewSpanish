using System;
using SettlementTradeOverview.Domain.Eligibility;

namespace SettlementTradeOverview.Application.Settlements
{
    internal static class SettlementTradeCommandPolicy
    {
        public static SettlementTradeCommandState Evaluate(
            bool isPotentialTrader,
            SettlementEligibilityResult eligibilityResult)
        {
            if (eligibilityResult == null)
                throw new ArgumentNullException(nameof(eligibilityResult));

            if (!isPotentialTrader)
                return SettlementTradeCommandState.Hidden;

            if (eligibilityResult.IsEligible)
                return SettlementTradeCommandState.Enabled;

            switch (eligibilityResult.FailureReason)
            {
                case SettlementEligibilityFailureReason.PlayerOwned:
                    return SettlementTradeCommandState.Hidden;

                case SettlementEligibilityFailureReason.TradeUnavailable:
                case SettlementEligibilityFailureReason.Hostile:
                case SettlementEligibilityFailureReason.TechnologyUnavailable:
                case SettlementEligibilityFailureReason.TechnologyBelowMinimum:
                case SettlementEligibilityFailureReason.PoweredCommsConsoleRequired:
                case SettlementEligibilityFailureReason.DistanceUnavailable:
                case SettlementEligibilityFailureReason.BeyondMaximumDistance:
                case SettlementEligibilityFailureReason.ReachabilityUnavailable:
                case SettlementEligibilityFailureReason.Unreachable:
                case SettlementEligibilityFailureReason.RoyaltyTradePermissionUnavailable:
                case SettlementEligibilityFailureReason.RoyaltyTradePermissionDenied:
                    return SettlementTradeCommandState.Disabled;

                case SettlementEligibilityFailureReason.None:
                default:
                    throw new ArgumentOutOfRangeException(nameof(eligibilityResult));
            }
        }
    }
}