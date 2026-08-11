using Escarval.RimWorld.UI;
using UnityEngine;
using Verse;

namespace SettlementTradeOverview.UI.TradeList
{
    [StaticConstructorOnStartup]
    internal static class TradeRelevanceDetailsView
    {
        private static readonly ReloadableTexture2D _relevanceIcon =
            new ReloadableTexture2D("UI/Icons/PlannerRelevance");

        public static void Draw(Rect rect, string tooltip)
        {
            if (string.IsNullOrWhiteSpace(tooltip))
                return;

            float iconSize = Mathf.Min(RimWorldUiStyle.Metrics.IconButtonSize, rect.height);

            var iconRect = new Rect(
                rect.x + (rect.width - iconSize) * 0.5f,
                rect.y + (rect.height - iconSize) * 0.5f,
                iconSize,
                iconSize);

            RimWorldUiWidgets.DrawIcon(iconRect, _relevanceIcon.Texture, Color.white);

            if (Mouse.IsOver(iconRect))
                TooltipHandler.TipRegion(iconRect, tooltip);
        }
    }
}