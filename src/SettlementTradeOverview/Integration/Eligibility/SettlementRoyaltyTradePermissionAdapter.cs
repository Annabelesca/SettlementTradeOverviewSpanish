using System;
using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using SettlementTradeOverview.Domain.Eligibility;
using Verse;

namespace SettlementTradeOverview.Integration.Eligibility
{
    internal static class SettlementRoyaltyTradePermissionAdapter
    {
        public static SettlementRoyaltyTradePermissionState Evaluate(
            Settlement settlement,
            IReadOnlyList<Pawn> colonists,
            bool isRoyaltyActive)
        {
            if (settlement == null)
                throw new ArgumentNullException(nameof(settlement));

            if (colonists == null)
                throw new ArgumentNullException(nameof(colonists));

            if (!isRoyaltyActive)
                return SettlementRoyaltyTradePermissionState.NotApplicable;

            RoyalTitleDef requiredTitle = settlement.TraderKind?.TitleRequiredToTrade;

            if (requiredTitle == null)
                return SettlementRoyaltyTradePermissionState.NotApplicable;

            Faction faction = settlement.Faction;

            if (faction == null)
                return SettlementRoyaltyTradePermissionState.Unavailable;

            foreach (Pawn pawn in colonists)
            {
                if (pawn?.royalty == null)
                    continue;

                RoyalTitleDef currentTitle = pawn.royalty.GetCurrentTitle(faction);

                if (currentTitle != null && currentTitle.seniority >= requiredTitle.seniority)
                {
                    return SettlementRoyaltyTradePermissionState.Allowed;
                }
            }

            return SettlementRoyaltyTradePermissionState.Denied;
        }
    }
}