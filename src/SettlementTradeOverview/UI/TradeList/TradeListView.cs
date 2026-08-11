using System;
using System.Collections.Generic;
using Escarval.RimWorld.UI;
using RimWorld.Planet;
using SettlementTradeOverview.Domain.Snapshots;
using SettlementTradeOverview.Integration.Runtime;
using SettlementTradeOverview.Query;
using UnityEngine;
using Verse;

namespace SettlementTradeOverview.UI.TradeList
{
    [StaticConstructorOnStartup]
    internal static class TradeListView
    {
        private static readonly ReloadableTexture2D _warningIcon = new ReloadableTexture2D("UI/Icons/Warning");

        private const float DetailsColumnWidth = 144f;
        private const float CountColumnWidth = 72f;
        private const float PriceColumnWidth = 92f;
        private const float DistanceColumnWidth = 120f;
        private const float RestockColumnWidth = 100f;
        private const float GlobalItemColumnShare = 0.56f;

        public static bool Draw(
            Rect rect,
            IReadOnlyList<TradeListRowPresentation> rows,
            string emptyMessage,
            TradeListMode mode,
            bool showDetailsColumn,
            Action<TraderSnapshot> navigateToSettlement,
            ref Vector2 scrollPosition,
            ref SortableHeaderState<TradeSortMode> sortState)
        {
            if (rows == null)
                throw new ArgumentNullException(nameof(rows));

            if (emptyMessage == null)
                throw new ArgumentNullException(nameof(emptyMessage));

            if (!Enum.IsDefined(typeof(TradeListMode), mode))
                throw new ArgumentOutOfRangeException(nameof(mode));

            if (mode == TradeListMode.Global && navigateToSettlement == null)
                throw new ArgumentNullException(nameof(navigateToSettlement));

            if (rect.width <= 0f || rect.height <= 0f)
                return false;

            float tableWidth = Mathf.Max(0f, rect.width - RimWorldUiStyle.Metrics.ScrollbarWidth);
            var headerRect = new Rect(rect.x, rect.y, tableWidth, RimWorldUiStyle.Metrics.TableHeaderHeight);
            var headerLayout = TradeListColumnLayout.Calculate(headerRect, mode, showDetailsColumn);

            bool sortChanged = DrawHeaders(headerLayout, mode, showDetailsColumn, ref sortState);

            var viewportRect = new Rect(
                rect.x,
                headerRect.yMax,
                rect.width,
                Mathf.Max(0f, rect.yMax - headerRect.yMax));

            if (viewportRect.height <= 0f)
                return sortChanged;

            if (rows.Count == 0)
            {
                DrawEmptyMessage(viewportRect, emptyMessage);
                return sortChanged;
            }

            FixedHeightScrollListLayout layout = RimWorldUiWidgets.BeginFixedHeightScrollView(
                viewportRect,
                ref scrollPosition,
                rows.Count,
                RimWorldUiStyle.Metrics.StandardRowHeight,
                out float viewWidth);

            try
            {
                var rowBaseRect = new Rect(0f, 0f, viewWidth, RimWorldUiStyle.Metrics.StandardRowHeight);

                for (int index = layout.FirstVisibleIndex; index < layout.LastVisibleIndexExclusive; index++)
                {
                    Rect rowRect = rowBaseRect;
                    rowRect.y = index * RimWorldUiStyle.Metrics.StandardRowHeight;

                    DrawRow(rowRect, rows[index], index, mode, showDetailsColumn, navigateToSettlement);
                }
            }
            finally
            {
                Widgets.EndScrollView();
            }

            return sortChanged;
        }

        private static bool DrawHeaders(
            TradeListColumnLayout layout,
            TradeListMode mode,
            bool showDetailsColumn,
            ref SortableHeaderState<TradeSortMode> sortState)
        {
            var sortChanged = false;

            sortChanged |= DrawHeader(
                layout.Item,
                "STO.TradeList.Column.Item".Translate().ToString(),
                TradeSortMode.Name,
                ref sortState);

            if (showDetailsColumn)
            {
                sortChanged |= DrawHeader(
                    layout.Details,
                    "STO.TradeList.Column.Details".Translate().ToString(),
                    TradeSortMode.Details,
                    ref sortState,
                    "STO.TradeList.Column.DetailsTooltip".Translate().ToString());
            }

            sortChanged |= DrawHeader(
                layout.Count,
                "STO.TradeList.Column.Count".Translate().ToString(),
                TradeSortMode.Count,
                ref sortState);

            if (mode == TradeListMode.Global)
            {
                sortChanged |= DrawHeader(
                    layout.Settlement,
                    "STO.TradeList.Column.Settlement".Translate().ToString(),
                    TradeSortMode.Settlement,
                    ref sortState);
            }

            sortChanged |= DrawHeader(
                layout.Price,
                "STO.TradeList.Column.Price".Translate().ToString(),
                TradeSortMode.Price,
                ref sortState);

            if (mode == TradeListMode.Global)
            {
                sortChanged |= DrawHeader(
                    layout.Distance,
                    "STO.TradeList.Column.Distance".Translate().ToString(),
                    TradeSortMode.Distance,
                    ref sortState);

                sortChanged |= DrawHeader(
                    layout.Restock,
                    "STO.TradeList.Column.Restock".Translate().ToString(),
                    TradeSortMode.RestockTime,
                    ref sortState);
            }

            DrawColumnDividers(layout, mode, showDetailsColumn);
            return sortChanged;
        }

        private static bool DrawHeader(
            Rect rect,
            string label,
            TradeSortMode mode,
            ref SortableHeaderState<TradeSortMode> sortState,
            string tooltip = null)
        {
            bool clicked = RimWorldUiWidgets.DrawSortableTableHeader(
                rect,
                label,
                sortState.IsActive(mode),
                sortState.Direction,
                tooltip);

            if (!clicked)
                return false;

            sortState = sortState.Toggle(mode);
            return true;
        }

        private static void DrawRow(
            Rect rowRect,
            TradeListRowPresentation row,
            int rowIndex,
            TradeListMode mode,
            bool showDetailsColumn,
            Action<TraderSnapshot> navigateToSettlement)
        {
            bool hovered = Mouse.IsOver(rowRect);

            RimWorldUiWidgets.DrawSelectableRowBackground(
                rowRect,
                rowIndex,
                selected: false,
                hovered: hovered,
                drawAccent: false);

            var layout = TradeListColumnLayout.Calculate(rowRect, mode, showDetailsColumn);

            DrawItemCell(layout.Item, row);

            if (showDetailsColumn)
            {
                Rect detailsRect = GetCellContentRect(layout.Details);

                if (row.Relevance != null)
                    TradeRelevanceDetailsView.Draw(detailsRect, row.RelevanceTooltip);
                else
                    TradePawnDetailsView.Draw(detailsRect, row.Entry.PawnDetails);
            }

            DrawCell(layout.Count, row.CountText, TextAnchor.MiddleRight, RimWorldUiStyle.Colors.PrimaryText);

            if (mode == TradeListMode.Global)
                DrawSettlementCell(layout.Settlement, row, navigateToSettlement);

            DrawCell(layout.Price, row.Price.Text, TextAnchor.MiddleRight, row.Price.Color, row.Price.Tooltip);

            if (mode == TradeListMode.Global)
            {
                DrawDistanceCell(layout.Distance, row.Distance);

                DrawCell(
                    layout.Restock,
                    row.Restock.Text,
                    TextAnchor.MiddleRight,
                    row.Restock.Color,
                    row.Restock.Tooltip);
            }

            DrawColumnDividers(layout, mode, showDetailsColumn);
        }

        private static void DrawItemCell(Rect rect, TradeListRowPresentation row)
        {
            TradeEntrySnapshot entry = row.Entry;
            Rect contentRect = GetCellContentRect(rect);
            float iconSize = Mathf.Min(RimWorldUiStyle.Metrics.IconButtonSize, contentRect.height);

            var iconRect = new Rect(
                contentRect.x,
                contentRect.y + (contentRect.height - iconSize) * 0.5f,
                iconSize,
                iconSize);

            var infoRect = new Rect(
                iconRect.xMax + RimWorldUiStyle.Metrics.IconButtonGap,
                contentRect.y + (contentRect.height - RimWorldUiStyle.Metrics.IconButtonSize) * 0.5f,
                RimWorldUiStyle.Metrics.IconButtonSize,
                RimWorldUiStyle.Metrics.IconButtonSize);

            var labelRect = new Rect(
                infoRect.xMax + RimWorldUiStyle.Metrics.IconButtonGap,
                contentRect.y,
                Mathf.Max(0f, contentRect.xMax - infoRect.xMax - RimWorldUiStyle.Metrics.IconButtonGap),
                contentRect.height);

            bool isPawn = entry.Kind == TradeEntryKind.Pawn;
            Pawn pawn = null;
            Thing representative = null;
            ThingDef definition = null;
            bool hasPawn = isPawn && TradeEntryRuntimeAdapter.TryResolvePawn(entry, out pawn);
            bool hasRepresentative =
                !isPawn && TradeEntryRuntimeAdapter.TryResolveRepresentative(entry, out representative);
            bool hasDefinition = !isPawn && TradeEntryRuntimeAdapter.TryResolveDefinition(entry, out definition);

            if (hasPawn)
                Widgets.ThingIcon(iconRect, pawn);
            else if (hasRepresentative)
                Widgets.ThingIcon(iconRect, representative);
            else if (hasDefinition)
                Widgets.ThingIcon(iconRect, definition);

            bool canOpenInfo = hasPawn || hasRepresentative || hasDefinition;
            string infoTooltip = Mouse.IsOver(infoRect) ? row.InfoTooltip : null;

            if (RimWorldUiWidgets.DrawIconButton(infoRect, TexButton.Info, canOpenInfo, infoTooltip))
                TradeEntryRuntimeAdapter.TryOpenInfoCard(entry);

            TipSignal? nativeTooltip = null;

            if (Mouse.IsOver(labelRect) && TradeEntryRuntimeAdapter.TryCreateNativeTooltip(
                    entry,
                    pawn,
                    representative,
                    definition,
                    out TipSignal resolvedTooltip))
            {
                nativeTooltip = resolvedTooltip;
            }

            using (ImGuiStateScope.Capture())
            {
                GUI.color = RimWorldUiStyle.Colors.PrimaryText;

                RimWorldUiWidgets.DrawTruncatedLabelWithTooltip(
                    labelRect,
                    entry.Label,
                    GameFont.Small,
                    TextAnchor.MiddleLeft,
                    nativeTooltip);
            }
        }

        private static void DrawDistanceCell(Rect rect, TradeCellPresentation presentation)
        {
            if (!presentation.ShowWarningIcon)
            {
                DrawCell(rect, presentation.Text, TextAnchor.MiddleRight, presentation.Color, presentation.Tooltip);

                return;
            }

            Rect contentRect = GetCellContentRect(rect);

            float iconSize = Mathf.Min(RimWorldUiStyle.Metrics.IconButtonSize, contentRect.height);

            float iconGap = RimWorldUiStyle.Metrics.IconButtonGap;

            var iconRect = new Rect(
                contentRect.x,
                contentRect.y + (contentRect.height - iconSize) * 0.5f,
                iconSize,
                iconSize);

            var labelRect = new Rect(
                iconRect.xMax + iconGap,
                contentRect.y,
                Mathf.Max(0f, contentRect.xMax - iconRect.xMax - iconGap),
                contentRect.height);

            string tooltip = string.IsNullOrWhiteSpace(presentation.Tooltip)
                ? presentation.Text
                : presentation.Text + "\n\n" + presentation.Tooltip;

            RimWorldUiWidgets.DrawIcon(iconRect, _warningIcon.Texture, presentation.Color);

            using (ImGuiStateScope.Capture())
            {
                GUI.color = presentation.Color;
                Text.Font = GameFont.Small;
                Text.Anchor = TextAnchor.MiddleRight;

                bool truncated = Text.CalcSize(presentation.Text).x > labelRect.width;

                Widgets.Label(labelRect, truncated ? presentation.Text.Truncate(labelRect.width) : presentation.Text);
            }

            if (!string.IsNullOrWhiteSpace(tooltip))
                TooltipHandler.TipRegion(contentRect, tooltip);
        }

        private static void DrawCell(
            Rect rect,
            string text,
            TextAnchor anchor,
            Color color,
            string contextualTooltip = null)
        {
            Rect contentRect = GetCellContentRect(rect);
            string activeTooltip = Mouse.IsOver(contentRect) ? contextualTooltip : null;

            using (ImGuiStateScope.Capture())
            {
                GUI.color = color;

                RimWorldUiWidgets.DrawTruncatedLabelWithTooltip(
                    contentRect,
                    text,
                    GameFont.Small,
                    anchor,
                    activeTooltip);
            }
        }

        private static void DrawSettlementCell(
            Rect rect,
            TradeListRowPresentation row,
            Action<TraderSnapshot> navigateToSettlement)
        {
            TraderSnapshot trader = row.Trader;
            Rect contentRect = GetCellContentRect(rect);
            float iconSize = Mathf.Min(RimWorldUiStyle.Metrics.IconButtonSize, contentRect.height);

            var iconRect = new Rect(
                contentRect.x,
                contentRect.y + (contentRect.height - iconSize) * 0.5f,
                iconSize,
                iconSize);

            var labelRect = new Rect(
                iconRect.xMax + RimWorldUiStyle.Metrics.IconButtonGap,
                contentRect.y,
                Mathf.Max(0f, contentRect.xMax - iconRect.xMax - RimWorldUiStyle.Metrics.IconButtonGap),
                contentRect.height);

            bool hovered = Mouse.IsOver(contentRect);
            string tooltip = row.SettlementNavigationTooltip;

            if (SettlementRuntimeAdapter.TryResolve(trader.SettlementIdentity, out Settlement settlement) &&
                settlement.ExpandingIcon != null)
            {
                using (ImGuiStateScope.Capture())
                {
                    GUI.color = settlement.ExpandingIconColor;
                    GUI.DrawTexture(iconRect, settlement.ExpandingIcon, ScaleMode.ScaleToFit, true);
                }
            }

            using (ImGuiStateScope.Capture())
            {
                GUI.color = hovered ? RimWorldUiStyle.Colors.Accent : RimWorldUiStyle.Colors.PrimaryText;

                RimWorldUiWidgets.DrawTruncatedLabelWithTooltip(
                    labelRect,
                    trader.SettlementLabel,
                    GameFont.Small,
                    TextAnchor.MiddleLeft,
                    Mouse.IsOver(labelRect) ? tooltip : null);
            }

            if (Mouse.IsOver(iconRect) && !string.IsNullOrWhiteSpace(tooltip))
                TooltipHandler.TipRegion(iconRect, tooltip);

            if (Event.current.button == 0 && Widgets.ButtonInvisible(contentRect))
                navigateToSettlement(trader);
        }

        private static Rect GetCellContentRect(Rect rect)
        {
            float padding = RimWorldUiStyle.Metrics.TableHeaderHorizontalPadding;

            return new Rect(rect.x + padding, rect.y, Mathf.Max(0f, rect.width - padding * 2f), rect.height);
        }

        private static void DrawColumnDividers(TradeListColumnLayout layout, TradeListMode mode, bool showDetailsColumn)
        {
            DrawDivider(layout.Item.xMax, layout.Item.y, layout.Item.height);

            if (showDetailsColumn)
                DrawDivider(layout.Details.xMax, layout.Details.y, layout.Details.height);

            DrawDivider(layout.Count.xMax, layout.Count.y, layout.Count.height);

            if (mode == TradeListMode.Global)
            {
                DrawDivider(layout.Settlement.xMax, layout.Settlement.y, layout.Settlement.height);
                DrawDivider(layout.Price.xMax, layout.Price.y, layout.Price.height);
                DrawDivider(layout.Distance.xMax, layout.Distance.y, layout.Distance.height);
            }
        }

        private static void DrawDivider(float x, float y, float height)
        {
            RimWorldUiWidgets.DrawTableDivider(
                new Rect(
                    x - RimWorldUiStyle.Metrics.TableDividerWidth,
                    y,
                    RimWorldUiStyle.Metrics.TableDividerWidth,
                    height));
        }

        private static void DrawEmptyMessage(Rect rect, string message)
        {
            using (ImGuiStateScope.Capture())
            {
                Text.Font = GameFont.Small;
                Text.Anchor = TextAnchor.UpperLeft;
                GUI.color = RimWorldUiStyle.Colors.MutedText;

                Widgets.Label(rect.ContractedBy(RimWorldUiStyle.Metrics.PanelPadding), message);
            }
        }

        private readonly struct TradeListColumnLayout
        {
            private TradeListColumnLayout(
                Rect item,
                Rect details,
                Rect count,
                Rect settlement,
                Rect price,
                Rect distance,
                Rect restock)
            {
                Item = item;
                Details = details;
                Count = count;
                Settlement = settlement;
                Price = price;
                Distance = distance;
                Restock = restock;
            }

            public Rect Item { get; }

            public Rect Details { get; }

            public Rect Count { get; }

            public Rect Settlement { get; }

            public Rect Price { get; }

            public Rect Distance { get; }

            public Rect Restock { get; }

            public static TradeListColumnLayout Calculate(Rect rect, TradeListMode mode, bool showDetailsColumn)
            {
                switch (mode)
                {
                    case TradeListMode.Global:
                        return CalculateGlobal(rect, showDetailsColumn);

                    case TradeListMode.Settlement:
                        return CalculateSettlement(rect, showDetailsColumn);

                    default:
                        throw new ArgumentOutOfRangeException(nameof(mode));
                }
            }

            private static TradeListColumnLayout CalculateGlobal(Rect rect, bool showDetailsColumn)
            {
                float detailsWidth = showDetailsColumn ? DetailsColumnWidth : 0f;
                float fixedWidth = detailsWidth + CountColumnWidth + PriceColumnWidth + DistanceColumnWidth +
                                   RestockColumnWidth;

                float flexibleWidth = Mathf.Max(0f, rect.width - fixedWidth);
                float itemWidth = flexibleWidth * GlobalItemColumnShare;
                float settlementWidth = Mathf.Max(0f, flexibleWidth - itemWidth);
                float x = rect.x;

                var item = new Rect(x, rect.y, itemWidth, rect.height);
                x = item.xMax;

                var details = default(Rect);

                if (showDetailsColumn)
                {
                    details = new Rect(x, rect.y, DetailsColumnWidth, rect.height);
                    x = details.xMax;
                }

                var count = new Rect(x, rect.y, CountColumnWidth, rect.height);
                x = count.xMax;

                var settlement = new Rect(x, rect.y, settlementWidth, rect.height);
                x = settlement.xMax;

                var price = new Rect(x, rect.y, PriceColumnWidth, rect.height);
                x = price.xMax;

                var distance = new Rect(x, rect.y, DistanceColumnWidth, rect.height);
                x = distance.xMax;

                var restock = new Rect(x, rect.y, Mathf.Max(0f, rect.xMax - x), rect.height);

                return new TradeListColumnLayout(item, details, count, settlement, price, distance, restock);
            }

            private static TradeListColumnLayout CalculateSettlement(Rect rect, bool showDetailsColumn)
            {
                float detailsWidth = showDetailsColumn ? DetailsColumnWidth : 0f;
                float itemWidth = Mathf.Max(0f, rect.width - detailsWidth - CountColumnWidth - PriceColumnWidth);

                float x = rect.x;

                var item = new Rect(x, rect.y, itemWidth, rect.height);
                x = item.xMax;

                var details = default(Rect);

                if (showDetailsColumn)
                {
                    details = new Rect(x, rect.y, DetailsColumnWidth, rect.height);
                    x = details.xMax;
                }

                var count = new Rect(x, rect.y, CountColumnWidth, rect.height);
                x = count.xMax;

                var price = new Rect(x, rect.y, Mathf.Max(0f, rect.xMax - x), rect.height);

                return new TradeListColumnLayout(
                    item,
                    details,
                    count,
                    default(Rect),
                    price,
                    default(Rect),
                    default(Rect));
            }
        }
    }
}