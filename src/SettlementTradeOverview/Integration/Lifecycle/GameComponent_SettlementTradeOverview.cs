using SettlementTradeOverview.Application.Snapshots;
using Verse;

namespace SettlementTradeOverview.Integration.Lifecycle
{
    internal sealed class GameComponent_SettlementTradeOverview : GameComponent
    {
        public GameComponent_SettlementTradeOverview(Game _)
        {
        }

        public override void StartedNewGame()
        {
            TradeInventorySnapshotService.Invalidate();
        }

        public override void LoadedGame()
        {
            TradeInventorySnapshotService.Invalidate();
        }
    }
}