using System;
using SettlementTradeOverview.Integration.Runtime;
using Verse;

namespace SettlementTradeOverview.Integration.Navigation
{
    internal static class PawnNavigationAdapter
    {
        private const string LogPrefix = "[SettlementTradeOverview]";

        public static bool TryNavigate(string pawnId)
        {
            if (string.IsNullOrWhiteSpace(pawnId))
                throw new ArgumentException("Pawn identity cannot be empty.", nameof(pawnId));

            try
            {
                if (!PawnRuntimeAdapter.TryResolve(pawnId, out Pawn pawn) || pawn.Destroyed || !pawn.Spawned ||
                    pawn.MapHeld == null)
                {
                    return false;
                }

                Find.MainTabsRoot?.EscapeCurrentTab(playSound: false);
                CameraJumper.TryJumpAndSelect(pawn);
                return true;
            }
            catch (Exception exception)
            {
                Log.Warning($"{LogPrefix} Failed to navigate to pawn '{pawnId}'.\n" + exception);
                return false;
            }
        }
    }
}