using System;

namespace SettlementTradeOverview.Domain.Categories
{
    [Flags]
    public enum TradeCategoryMembership
    {
        None = 0,
        Foods = 1 << 0,
        ResourcesRaw = 1 << 1,
        Manufactured = 1 << 2,
        Apparel = 1 << 3,
        Weapons = 1 << 4,
        Items = 1 << 5,
        Buildings = 1 << 6
    }
}