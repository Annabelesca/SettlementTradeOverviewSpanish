using System;

namespace SettlementTradeOverview.Domain.Eligibility
{
    public sealed class SettlementEligibilityCriteria
    {
        public SettlementEligibilityCriteria(
            bool requirePoweredCommsConsole = true,
            SettlementTechnologyLevel? minimumTechnologyLevel = SettlementTechnologyLevel.Industrial,
            int? maximumDistanceInTiles = 40,
            bool requireReachable = true,
            bool requireRoyaltyTradePermission = false)
        {
            ValidateMinimumTechnologyLevel(minimumTechnologyLevel);

            if (maximumDistanceInTiles.HasValue && maximumDistanceInTiles.Value < 0)
                throw new ArgumentOutOfRangeException(nameof(maximumDistanceInTiles));

            RequirePoweredCommsConsole = requirePoweredCommsConsole;
            MinimumTechnologyLevel = minimumTechnologyLevel;
            MaximumDistanceInTiles = maximumDistanceInTiles;
            RequireReachable = requireReachable;
            RequireRoyaltyTradePermission = requireRoyaltyTradePermission;
        }

        public static SettlementEligibilityCriteria Default { get; } = new SettlementEligibilityCriteria();

        public bool RequirePoweredCommsConsole { get; }

        public SettlementTechnologyLevel? MinimumTechnologyLevel { get; }

        public int? MaximumDistanceInTiles { get; }

        public bool RequireReachable { get; }

        public bool RequireRoyaltyTradePermission { get; }

        private static void ValidateMinimumTechnologyLevel(SettlementTechnologyLevel? minimumTechnologyLevel)
        {
            if (!minimumTechnologyLevel.HasValue)
                return;

            SettlementTechnologyLevel value = minimumTechnologyLevel.Value;

            if (!Enum.IsDefined(typeof(SettlementTechnologyLevel), value) ||
                value == SettlementTechnologyLevel.Unavailable)
            {
                throw new ArgumentOutOfRangeException(nameof(minimumTechnologyLevel));
            }
        }
    }
}