using System;

namespace SettlementTradeOverview.Domain.Eligibility
{
    public sealed class SettlementEligibilityResult
    {
        private SettlementEligibilityResult(bool isEligible, SettlementEligibilityFailureReason failureReason)
        {
            IsEligible = isEligible;
            FailureReason = failureReason;
        }

        public static SettlementEligibilityResult Eligible { get; } =
            new SettlementEligibilityResult(true, SettlementEligibilityFailureReason.None);

        public bool IsEligible { get; }

        public SettlementEligibilityFailureReason FailureReason { get; }

        public static SettlementEligibilityResult Ineligible(SettlementEligibilityFailureReason failureReason)
        {
            if (!Enum.IsDefined(typeof(SettlementEligibilityFailureReason), failureReason) ||
                failureReason == SettlementEligibilityFailureReason.None)
            {
                throw new ArgumentOutOfRangeException(nameof(failureReason));
            }

            return new SettlementEligibilityResult(false, failureReason);
        }
    }
}