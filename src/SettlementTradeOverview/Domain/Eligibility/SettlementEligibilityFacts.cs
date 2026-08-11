using System;

namespace SettlementTradeOverview.Domain.Eligibility
{
    public sealed class SettlementEligibilityFacts
    {
        public SettlementEligibilityFacts(
            bool isPlayerOwned,
            bool isTradeAvailable,
            bool isHostileToPlayer,
            SettlementTechnologyLevel technologyLevel,
            bool hasPoweredCommsConsole,
            int? distanceInTiles,
            SettlementReachabilityState reachability,
            bool isRoyaltyActive,
            SettlementRoyaltyTradePermissionState royaltyTradePermission)
        {
            if (!Enum.IsDefined(typeof(SettlementTechnologyLevel), technologyLevel))
                throw new ArgumentOutOfRangeException(nameof(technologyLevel));

            if (distanceInTiles.HasValue && distanceInTiles.Value < 0)
                throw new ArgumentOutOfRangeException(nameof(distanceInTiles));

            if (!Enum.IsDefined(typeof(SettlementReachabilityState), reachability))
                throw new ArgumentOutOfRangeException(nameof(reachability));

            if (!Enum.IsDefined(typeof(SettlementRoyaltyTradePermissionState), royaltyTradePermission))
            {
                throw new ArgumentOutOfRangeException(nameof(royaltyTradePermission));
            }

            IsPlayerOwned = isPlayerOwned;
            IsTradeAvailable = isTradeAvailable;
            IsHostileToPlayer = isHostileToPlayer;
            TechnologyLevel = technologyLevel;
            HasPoweredCommsConsole = hasPoweredCommsConsole;
            DistanceInTiles = distanceInTiles;
            Reachability = reachability;
            IsRoyaltyActive = isRoyaltyActive;
            RoyaltyTradePermission = royaltyTradePermission;
        }

        public bool IsPlayerOwned { get; }

        public bool IsTradeAvailable { get; }

        public bool IsHostileToPlayer { get; }

        public SettlementTechnologyLevel TechnologyLevel { get; }

        public bool HasPoweredCommsConsole { get; }

        public int? DistanceInTiles { get; }

        public SettlementReachabilityState Reachability { get; }

        public bool IsRoyaltyActive { get; }

        public SettlementRoyaltyTradePermissionState RoyaltyTradePermission { get; }
    }
}