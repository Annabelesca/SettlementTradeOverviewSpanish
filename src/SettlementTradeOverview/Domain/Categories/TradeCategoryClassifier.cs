using System;
using SettlementTradeOverview.Domain.Snapshots;

namespace SettlementTradeOverview.Domain.Categories
{
    public static class TradeCategoryClassifier
    {
        private const TradeCategoryMembership SupportedMemberships = TradeCategoryMembership.Foods |
                                                                     TradeCategoryMembership.ResourcesRaw |
                                                                     TradeCategoryMembership.Manufactured |
                                                                     TradeCategoryMembership.Apparel |
                                                                     TradeCategoryMembership.Weapons |
                                                                     TradeCategoryMembership.Items |
                                                                     TradeCategoryMembership.Buildings;

        public static TradeCategory Classify(TradeEntryKind kind, TradeCategoryMembership membership)
        {
            if (!Enum.IsDefined(typeof(TradeEntryKind), kind) || kind == TradeEntryKind.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(kind));
            }

            if ((membership & ~SupportedMemberships) != TradeCategoryMembership.None)
                throw new ArgumentOutOfRangeException(nameof(membership));

            if (kind == TradeEntryKind.Pawn)
                return TradeCategory.Pawns;

            if ((membership & TradeCategoryMembership.Foods) != 0)
                return TradeCategory.Foods;

            if ((membership & TradeCategoryMembership.ResourcesRaw) != 0)
                return TradeCategory.ResourcesRaw;

            if ((membership & TradeCategoryMembership.Manufactured) != 0)
                return TradeCategory.Manufactured;

            if ((membership & TradeCategoryMembership.Apparel) != 0)
                return TradeCategory.Apparel;

            if ((membership & TradeCategoryMembership.Weapons) != 0)
                return TradeCategory.Weapons;

            if ((membership & TradeCategoryMembership.Items) != 0)
                return TradeCategory.Items;

            if ((membership & TradeCategoryMembership.Buildings) != 0)
                return TradeCategory.Buildings;

            return TradeCategory.Other;
        }
    }
}