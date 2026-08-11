using System;
using SettlementTradeOverview.Domain.Identity;
using SettlementTradeOverview.Domain.Snapshots;

namespace SettlementTradeOverview.Application.Snapshots
{
    internal static class SettlementTraderSnapshotResolver
    {
        public static bool TryResolve(
            TradeInventorySnapshot snapshot,
            SettlementIdentity settlementIdentity,
            out TraderSnapshot trader)
        {
            if (snapshot == null)
                throw new ArgumentNullException(nameof(snapshot));

            if (settlementIdentity == null)
                throw new ArgumentNullException(nameof(settlementIdentity));

            foreach (TraderSnapshot candidate in snapshot.Traders)
            {
                if (!candidate.SettlementIdentity.Equals(settlementIdentity))
                    continue;

                trader = candidate;
                return true;
            }

            trader = null;
            return false;
        }
    }
}