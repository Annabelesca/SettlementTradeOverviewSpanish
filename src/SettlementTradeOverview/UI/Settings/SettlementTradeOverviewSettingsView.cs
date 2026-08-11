using System;
using Escarval.RimWorld.UI;
using SettlementTradeOverview.Settings;
using UnityEngine;
using Verse;

namespace SettlementTradeOverview.UI.Settings
{
    internal sealed class SettlementTradeOverviewSettingsView
    {
        private const float InterfacePanelHeight = 88f;
        private const float SectionTitleHeight = 28f;

        private string _maximumDistanceBuffer;

        public SettlementTradeOverviewSettingsView(SettlementTradeOverviewSettings settings)
        {
            if (settings == null)
                throw new ArgumentNullException(nameof(settings));

            SynchronizeBuffers(settings);
        }

        public void Draw(Rect inRect, SettlementTradeOverviewSettings settings)
        {
            if (settings == null)
                throw new ArgumentNullException(nameof(settings));

            using (ImGuiStateScope.Capture())
            {
                var interfacePanelRect = new Rect(inRect.x, inRect.y, inRect.width, InterfacePanelHeight);

                var eligibilityPanelRect = new Rect(
                    inRect.x,
                    interfacePanelRect.yMax + RimWorldUiStyle.Metrics.SectionGap,
                    inRect.width,
                    Mathf.Max(0f, inRect.yMax - interfacePanelRect.yMax - RimWorldUiStyle.Metrics.SectionGap));

                DrawInterfacePanel(interfacePanelRect, settings);
                DrawEligibilityPanel(eligibilityPanelRect, settings);
            }
        }

        public void SynchronizeBuffers(SettlementTradeOverviewSettings settings)
        {
            if (settings == null)
                throw new ArgumentNullException(nameof(settings));

            _maximumDistanceBuffer = settings.MaximumDistanceInTiles.ToString();
        }

        private static void DrawInterfacePanel(Rect rect, SettlementTradeOverviewSettings settings)
        {
            RimWorldUiWidgets.DrawPanel(rect);

            Rect innerRect = rect.ContractedBy(RimWorldUiStyle.Metrics.PanelPadding);

            DrawSectionTitle(innerRect, "STO.Settings.Section.Interface".Translate().ToString());

            var listingRect = new Rect(
                innerRect.x,
                innerRect.y + SectionTitleHeight,
                innerRect.width,
                Mathf.Max(0f, innerRect.height - SectionTitleHeight));

            var listing = new Listing_Standard();
            listing.Begin(listingRect);

            bool showGlobalOverviewTab = settings.ShowGlobalOverviewTab;

            listing.CheckboxLabeled(
                "STO.Settings.ShowGlobalOverviewTab.Label".Translate().ToString(),
                ref showGlobalOverviewTab,
                "STO.Settings.ShowGlobalOverviewTab.Description".Translate().ToString());

            settings.ShowGlobalOverviewTab = showGlobalOverviewTab;

            listing.End();
        }

        private void DrawEligibilityPanel(Rect rect, SettlementTradeOverviewSettings settings)
        {
            RimWorldUiWidgets.DrawPanel(rect);

            Rect innerRect = rect.ContractedBy(RimWorldUiStyle.Metrics.PanelPadding);

            DrawSectionTitle(innerRect, "STO.Settings.Section.Eligibility".Translate().ToString());

            var listingRect = new Rect(
                innerRect.x,
                innerRect.y + SectionTitleHeight,
                innerRect.width,
                Mathf.Max(0f, innerRect.height - SectionTitleHeight));

            var listing = new Listing_Standard();
            listing.Begin(listingRect);

            bool requirePoweredCommsConsole = settings.RequirePoweredCommsConsole;

            listing.CheckboxLabeled(
                "STO.Settings.RequirePoweredCommsConsole.Label".Translate().ToString(),
                ref requirePoweredCommsConsole,
                "STO.Settings.RequirePoweredCommsConsole.Description".Translate().ToString());

            settings.RequirePoweredCommsConsole = requirePoweredCommsConsole;

            bool requireIndustrialTechnology = settings.RequireIndustrialTechnology;

            listing.CheckboxLabeled(
                "STO.Settings.RequireIndustrialTechnology.Label".Translate().ToString(),
                ref requireIndustrialTechnology,
                "STO.Settings.RequireIndustrialTechnology.Description".Translate().ToString());

            settings.RequireIndustrialTechnology = requireIndustrialTechnology;

            bool limitMaximumDistance = settings.LimitMaximumDistance;

            listing.CheckboxLabeled(
                "STO.Settings.LimitMaximumDistance.Label".Translate().ToString(),
                ref limitMaximumDistance,
                "STO.Settings.LimitMaximumDistance.Description".Translate().ToString());

            settings.LimitMaximumDistance = limitMaximumDistance;

            int maximumDistanceInTiles = settings.MaximumDistanceInTiles;
            Rect maximumDistanceRect = listing.GetRect(Text.LineHeight);
            bool previousGuiEnabled = GUI.enabled;

            try
            {
                GUI.enabled = previousGuiEnabled && settings.LimitMaximumDistance;

                Widgets.TextFieldNumericLabeled(
                    maximumDistanceRect,
                    "STO.Settings.MaximumDistanceInTiles.Label".Translate().ToString(),
                    ref maximumDistanceInTiles,
                    ref _maximumDistanceBuffer,
                    0f,
                    SettlementTradeOverviewSettingsValues.MaximumAllowedDistanceInTiles);
            }
            finally
            {
                GUI.enabled = previousGuiEnabled;
            }

            TooltipHandler.TipRegion(
                maximumDistanceRect,
                "STO.Settings.MaximumDistanceInTiles.Description".Translate().ToString());

            settings.MaximumDistanceInTiles = maximumDistanceInTiles;

            bool requireReachable = settings.RequireReachable;

            listing.CheckboxLabeled(
                "STO.Settings.RequireReachable.Label".Translate().ToString(),
                ref requireReachable,
                "STO.Settings.RequireReachable.Description".Translate().ToString());

            settings.RequireReachable = requireReachable;

            bool requireRoyaltyTradePermission = settings.RequireRoyaltyTradePermission;

            listing.CheckboxLabeled(
                "STO.Settings.RequireRoyaltyTradePermission.Label".Translate().ToString(),
                ref requireRoyaltyTradePermission,
                "STO.Settings.RequireRoyaltyTradePermission.Description".Translate().ToString());

            settings.RequireRoyaltyTradePermission = requireRoyaltyTradePermission;

            listing.End();
        }

        private static void DrawSectionTitle(Rect rect, string label)
        {
            using (ImGuiStateScope.Capture())
            {
                Text.Font = GameFont.Medium;
                Text.Anchor = TextAnchor.UpperLeft;
                GUI.color = RimWorldUiStyle.Colors.PrimaryText;

                Widgets.Label(new Rect(rect.x, rect.y, rect.width, SectionTitleHeight), label);
            }
        }
    }
}