namespace SettlementTradeOverview.Domain.Eligibility
{
    public enum SettlementEligibilityFailureReason
    {
        None,
        PlayerOwned,
        TradeUnavailable,
        Hostile,
        TechnologyUnavailable,
        TechnologyBelowMinimum,
        PoweredCommsConsoleRequired,
        DistanceUnavailable,
        BeyondMaximumDistance,
        ReachabilityUnavailable,
        Unreachable,
        RoyaltyTradePermissionUnavailable,
        RoyaltyTradePermissionDenied
    }
}