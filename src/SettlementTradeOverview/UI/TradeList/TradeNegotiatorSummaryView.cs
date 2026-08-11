using System;
using Escarval.RimWorld.UI;
using SettlementTradeOverview.Domain.Snapshots;
using SettlementTradeOverview.Integration.Runtime;
using UnityEngine;
using Verse;

namespace SettlementTradeOverview.UI.TradeList
{
    internal static class TradeNegotiatorSummaryView
    {
        public static float Draw(
            Rect rect,
            TradeNegotiatorSnapshot negotiator,
            Action<TradeNegotiatorSnapshot> navigateToNegotiator)
        {
            if (negotiator == null)
            {
                return DrawMeasuredMessage(
                    rect,
                    "STO.GlobalOverview.NoNegotiatorSummary".Translate().ToString(),
                    RimWorldUiStyle.Colors.Warning);
            }

            if (navigateToNegotiator == null)
                throw new ArgumentNullException(nameof(navigateToNegotiator));

            var summary = "STO.GlobalOverview.NegotiatorSummary".Translate(
                negotiator.Label,
                negotiator.TradePriceImprovement.ToStringPercent()).ToString();

            float iconSize = RimWorldUiStyle.Metrics.IconButtonSize;
            float leftWidth = iconSize * 2f + RimWorldUiStyle.Metrics.IconButtonGap * 2f;
            float labelWidth = Mathf.Max(0f, rect.width - leftWidth);

            using (ImGuiStateScope.Capture())
            {
                Text.Font = GameFont.Small;
                float rowHeight = Mathf.Max(
                    RimWorldUiStyle.Metrics.StandardRowHeight,
                    Text.CalcHeight(summary, labelWidth));

                var rowRect = new Rect(rect.x, rect.y, rect.width, rowHeight);

                var iconRect = new Rect(rowRect.x, rowRect.y + (rowRect.height - iconSize) * 0.5f, iconSize, iconSize);

                var infoRect = new Rect(
                    iconRect.xMax + RimWorldUiStyle.Metrics.IconButtonGap,
                    rowRect.y + (rowRect.height - iconSize) * 0.5f,
                    iconSize,
                    iconSize);

                var labelRect = new Rect(
                    infoRect.xMax + RimWorldUiStyle.Metrics.IconButtonGap,
                    rowRect.y,
                    Mathf.Max(0f, rowRect.xMax - infoRect.xMax - RimWorldUiStyle.Metrics.IconButtonGap),
                    rowRect.height);

                bool hasPawn = PawnRuntimeAdapter.TryResolve(negotiator.PawnId, out Pawn pawn);
                bool hovered = Mouse.IsOver(iconRect) || Mouse.IsOver(labelRect);
                var navigationTooltip = "STO.GlobalOverview.Navigation.JumpToNegotiator".Translate(negotiator.Label)
                    .ToString();

                if (hasPawn)
                    Widgets.ThingIcon(iconRect, pawn);

                var infoTooltip = "STO.TradeList.Info.Open".Translate(negotiator.Label).ToString();

                if (RimWorldUiWidgets.DrawIconButton(infoRect, TexButton.Info, hasPawn, infoTooltip))
                    PawnRuntimeAdapter.TryOpenInfoCard(negotiator.PawnId);

                GUI.color = hovered ? RimWorldUiStyle.Colors.Accent : RimWorldUiStyle.Colors.MutedText;

                RimWorldUiWidgets.DrawTruncatedLabelWithTooltip(
                    labelRect,
                    summary,
                    GameFont.Small,
                    TextAnchor.MiddleLeft,
                    navigationTooltip);

                TooltipHandler.TipRegion(iconRect, navigationTooltip);

                bool iconClicked = Event.current.button == 0 && Widgets.ButtonInvisible(iconRect);
                bool labelClicked = Event.current.button == 0 && Widgets.ButtonInvisible(labelRect);

                if (iconClicked || labelClicked)
                    navigateToNegotiator(negotiator);

                return rowHeight;
            }
        }

        private static float DrawMeasuredMessage(Rect rect, string message, Color color)
        {
            using (ImGuiStateScope.Capture())
            {
                Text.Font = GameFont.Small;
                Text.Anchor = TextAnchor.UpperLeft;
                GUI.color = color;

                float height = Text.CalcHeight(message, rect.width);
                Widgets.Label(new Rect(rect.x, rect.y, rect.width, height), message);
                return height;
            }
        }
    }
}