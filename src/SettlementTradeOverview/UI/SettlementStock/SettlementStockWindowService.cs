using System;
using SettlementTradeOverview.Domain.Identity;
using Verse;

namespace SettlementTradeOverview.UI.SettlementStock
{
    internal static class SettlementStockWindowService
    {
        public static void Open(SettlementIdentity settlementIdentity)
        {
            if (settlementIdentity == null)
                throw new ArgumentNullException(nameof(settlementIdentity));

            if (Find.WindowStack == null)
                return;

            Find.WindowStack.Add(new Dialog_SettlementStock(settlementIdentity));
        }
    }
}