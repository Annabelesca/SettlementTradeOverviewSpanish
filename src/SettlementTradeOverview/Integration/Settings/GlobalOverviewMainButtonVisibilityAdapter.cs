using RimWorld;
using Verse;

namespace SettlementTradeOverview.Integration.Settings
{
    internal static class GlobalOverviewMainButtonVisibilityAdapter
    {
        private const string MainButtonDefName = "STO_GlobalOverview";
        private const string LogPrefix = "[Settlement Trade Overview]";

        public static void Apply(bool visible)
        {
            MainButtonDef mainButtonDef = DefDatabase<MainButtonDef>.GetNamedSilentFail(MainButtonDefName);

            if (mainButtonDef == null)
            {
                Log.Error(
                    $"{LogPrefix} Could not apply the global overview tab visibility setting because " +
                    $"MainButtonDef '{MainButtonDefName}' was not found.");
                return;
            }

            mainButtonDef.buttonVisible = visible;
        }
    }
}