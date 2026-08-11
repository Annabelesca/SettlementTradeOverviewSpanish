using System;
using RimWorld.Planet;
using SettlementTradeOverview.Domain.Identity;
using SettlementTradeOverview.Integration.Runtime;
using Verse;

namespace SettlementTradeOverview.Integration.Navigation
{
    internal static class SettlementNavigationAdapter
    {
        private const string LogPrefix = "[SettlementTradeOverview]";

        public static bool TryNavigate(SettlementIdentity settlementIdentity)
        {
            if (settlementIdentity == null)
                throw new ArgumentNullException(nameof(settlementIdentity));

            try
            {
                if (!SettlementRuntimeAdapter.TryResolve(settlementIdentity, out Settlement target) ||
                    !target.SelectableNow)
                    return false;

                Find.MainTabsRoot?.EscapeCurrentTab(playSound: false);
                CameraJumper.TryJumpAndSelect(target);
                return true;
            }
            catch (Exception exception)
            {
                Log.Warning(
                    $"{LogPrefix} Failed to navigate to settlement ID {settlementIdentity.WorldObjectId}.\n" +
                    exception);

                return false;
            }
        }
    }
}