using System;
using RimWorld;
using RimWorld.Planet;
using SettlementTradeOverview.Domain.Eligibility;
using SettlementTradeOverview.Integration.Context;

namespace SettlementTradeOverview.Integration.Eligibility
{
    internal static class SettlementEligibilityFactsAdapter
    {
        public static SettlementEligibilityFacts Create(
            Settlement settlement,
            PlayerTradeContext context,
            SettlementEligibilityCriteria criteria)
        {
            if (settlement == null)
                throw new ArgumentNullException(nameof(settlement));

            if (context == null)
                throw new ArgumentNullException(nameof(context));

            if (criteria == null)
                throw new ArgumentNullException(nameof(criteria));

            Faction settlementFaction = settlement.Faction;
            Faction playerFaction = Faction.OfPlayer;

            bool isPlayerOwned = playerFaction != null && settlementFaction == playerFaction;

            bool isTradeAvailable =
                settlementFaction != null && settlement.TraderKind != null && settlement.CanTradeNow;

            bool isHostileToPlayer = settlementFaction == null || playerFaction == null ||
                                     settlementFaction.HostileTo(playerFaction);

            SettlementTechnologyLevel technologyLevel = settlementFaction?.def == null
                ? SettlementTechnologyLevel.Unavailable
                : SettlementTechnologyLevelAdapter.Convert(settlementFaction.def.techLevel);

            int? distanceInTiles = CalculateDistance(settlement, context, criteria);

            SettlementReachabilityState reachability = CalculateReachability(settlement, context, criteria);

            SettlementRoyaltyTradePermissionState royaltyTradePermission = criteria.RequireRoyaltyTradePermission
                ? SettlementRoyaltyTradePermissionAdapter.Evaluate(
                    settlement,
                    context.Colonists,
                    context.IsRoyaltyActive)
                : SettlementRoyaltyTradePermissionState.NotApplicable;

            return new SettlementEligibilityFacts(
                isPlayerOwned,
                isTradeAvailable,
                isHostileToPlayer,
                technologyLevel,
                context.HasPoweredCommsConsole,
                distanceInTiles,
                reachability,
                context.IsRoyaltyActive,
                royaltyTradePermission);
        }

        private static int? CalculateDistance(
            Settlement settlement,
            PlayerTradeContext context,
            SettlementEligibilityCriteria criteria)
        {
            if (!criteria.MaximumDistanceInTiles.HasValue)
                return null;

            int settlementTile = settlement.Tile.tileId;

            if (settlementTile < 0 || context.OriginTile < 0)
                return null;

            float distance = context.WorldGrid.ApproxDistanceInTiles(settlementTile, context.OriginTile);

            if (float.IsNaN(distance) || float.IsInfinity(distance) || distance < 0f || distance > int.MaxValue)
            {
                return null;
            }

            return (int)distance;
        }

        private static SettlementReachabilityState CalculateReachability(
            Settlement settlement,
            PlayerTradeContext context,
            SettlementEligibilityCriteria criteria)
        {
            if (!criteria.RequireReachable)
                return SettlementReachabilityState.Unavailable;

            int settlementTile = settlement.Tile.tileId;

            if (settlementTile < 0 || context.OriginTile < 0)
                return SettlementReachabilityState.Unavailable;

            int traversalDistance =
                context.WorldGrid.TraversalDistanceBetween(settlementTile, context.OriginTile, false);

            if (traversalDistance == int.MaxValue)
                return SettlementReachabilityState.Unreachable;

            if (traversalDistance < 0)
                return SettlementReachabilityState.Unavailable;

            return SettlementReachabilityState.Reachable;
        }
    }
}