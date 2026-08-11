using System;
using SettlementTradeOverview.Domain.Eligibility;

namespace SettlementTradeOverview.Settings
{
    internal static class SettlementTradeOverviewSettingsPolicy
    {
        public static void Normalize(SettlementTradeOverviewSettingsValues values)
        {
            if (values == null)
                throw new ArgumentNullException(nameof(values));

            if (values.MaximumDistanceInTiles < 0)
            {
                values.MaximumDistanceInTiles = SettlementTradeOverviewSettingsValues.DefaultMaximumDistanceInTiles;
            }
            else if (values.MaximumDistanceInTiles >
                     SettlementTradeOverviewSettingsValues.MaximumAllowedDistanceInTiles)
            {
                values.MaximumDistanceInTiles = SettlementTradeOverviewSettingsValues.MaximumAllowedDistanceInTiles;
            }
        }

        public static SettlementEligibilityCriteria CreateEligibilityCriteria(
            SettlementTradeOverviewSettingsValues values)
        {
            if (values == null)
                throw new ArgumentNullException(nameof(values));

            Normalize(values);

            SettlementTechnologyLevel? minimumTechnologyLevel = values.RequireIndustrialTechnology
                ? SettlementTechnologyLevel.Industrial
                : (SettlementTechnologyLevel?)null;

            int? maximumDistanceInTiles = values.LimitMaximumDistance ? values.MaximumDistanceInTiles : (int?)null;

            return new SettlementEligibilityCriteria(
                values.RequirePoweredCommsConsole,
                minimumTechnologyLevel,
                maximumDistanceInTiles,
                values.RequireReachable,
                values.RequireRoyaltyTradePermission);
        }

        public static bool AreEquivalent(SettlementEligibilityCriteria first, SettlementEligibilityCriteria second)
        {
            if (ReferenceEquals(first, second))
                return true;

            if (first == null || second == null)
                return false;

            return first.RequirePoweredCommsConsole == second.RequirePoweredCommsConsole &&
                   first.MinimumTechnologyLevel == second.MinimumTechnologyLevel &&
                   first.MaximumDistanceInTiles == second.MaximumDistanceInTiles &&
                   first.RequireReachable == second.RequireReachable &&
                   first.RequireRoyaltyTradePermission == second.RequireRoyaltyTradePermission;
        }
    }
}