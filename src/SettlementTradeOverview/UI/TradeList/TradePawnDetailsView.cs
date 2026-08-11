using System;
using Escarval.RimWorld.UI;
using RimWorld;
using SettlementTradeOverview.Domain.Snapshots;
using UnityEngine;
using Verse;

namespace SettlementTradeOverview.UI.TradeList
{
    [StaticConstructorOnStartup]
    internal static class TradePawnDetailsView
    {
        private static readonly ReloadableTexture2D _rideableIcon = new ReloadableTexture2D("UI/Icons/Animal/Rideable");

        public static void Draw(Rect rect, PawnTradeDetailsSnapshot details)
        {
            if (details == null || details.Kind == PawnTradeDetailKind.None)
                return;

            switch (details.Kind)
            {
                case PawnTradeDetailKind.JoinsAsColonist:
                    DrawJoinOutcome(rect, "JoinsAsColonist", "JoinsAsColonistDesc");
                    break;

                case PawnTradeDetailKind.JoinsAsSlave:
                    DrawJoinOutcome(rect, "JoinsAsSlave", "JoinsAsSlaveDesc");
                    break;

                case PawnTradeDetailKind.Rideable:
                    DrawRideable(rect, details);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(details));
            }
        }

        private static void DrawJoinOutcome(Rect rect, string labelKey, string tooltipKey)
        {
            using (ImGuiStateScope.Capture())
            {
                GUI.color = RimWorldUiStyle.Colors.PrimaryText;

                RimWorldUiWidgets.DrawTruncatedLabelWithTooltip(
                    rect,
                    labelKey.Translate().ToString(),
                    GameFont.Small,
                    TextAnchor.MiddleLeft,
                    tooltipKey.Translate().ToString());
            }
        }

        private static void DrawRideable(Rect rect, PawnTradeDetailsSnapshot details)
        {
            float iconSize = Mathf.Min(RimWorldUiStyle.Metrics.IconButtonSize, rect.height);

            var iconRect = new Rect(
                rect.x + (rect.width - iconSize) * 0.5f,
                rect.y + (rect.height - iconSize) * 0.5f,
                iconSize,
                iconSize);

            using (ImGuiStateScope.Capture())
            {
                GUI.color = Color.white;
                GUI.DrawTexture(iconRect, _rideableIcon.Texture, ScaleMode.ScaleToFit, true);
            }

            TooltipHandler.TipRegion(iconRect, CreateRideableTooltip(details));
        }

        private static string CreateRideableTooltip(PawnTradeDetailsSnapshot details)
        {
            var description = "RideableAnimalTip".Translate().ToString();
            var statLabel = StatDefOf.CaravanRidingSpeedFactor.LabelCap.ToString();
            string value = details.CaravanRidingSpeedFactor.GetValueOrDefault().ToStringPercent();

            return description + "\n\n" + statLabel + ": " + value;
        }
    }
}