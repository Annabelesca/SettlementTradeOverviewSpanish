using System.Collections.Generic;
using RimWorld.Planet;
using Verse;

namespace SettlementTradeOverview.Integration.Settlements
{
    // ReSharper disable once InconsistentNaming
    public sealed class WorldObjectComp_SettlementTradeOverview : WorldObjectComp
    {
        public override IEnumerable<Gizmo> GetGizmos()
        {
            if (!(parent is Settlement settlement))
                yield break;

            if (!SettlementTradeCommandProvider.TryCreate(settlement, out Command_Action command))
                yield break;

            yield return command;
        }
    }
}