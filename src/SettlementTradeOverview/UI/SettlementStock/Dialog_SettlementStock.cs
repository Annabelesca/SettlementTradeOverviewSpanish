using System;
using System.Collections.Generic;
using Escarval.RimWorld.UI;
using RimWorld;
using RimWorld.Planet;
using SettlementTradeOverview.Application.Snapshots;
using SettlementTradeOverview.Cache;
using SettlementTradeOverview.Domain.Categories;
using SettlementTradeOverview.Domain.Eligibility;
using SettlementTradeOverview.Domain.Identity;
using SettlementTradeOverview.Domain.Snapshots;
using SettlementTradeOverview.Integration.Navigation;
using SettlementTradeOverview.Integration.Planner;
using SettlementTradeOverview.Integration.Runtime;
using SettlementTradeOverview.Query;
using SettlementTradeOverview.Settings;
using SettlementTradeOverview.UI.TradeList;
using UnityEngine;
using Verse;
using SharedSortDirection = Escarval.RimWorld.UI.SortDirection;

namespace SettlementTradeOverview.UI.SettlementStock
{
    // ReSharper disable once InconsistentNaming
    [StaticConstructorOnStartup]
    internal sealed class Dialog_SettlementStock : Window
    {
        private const float HeaderHeight = 48f;
        private const float RestockSummaryWidth = 200f;

        private static readonly Vector2 _initialSize = new Vector2(1000f, 900f);
        private static readonly ReloadableTexture2D _searchIcon = new ReloadableTexture2D("UI/Icons/Search");
        private static readonly ReloadableTexture2D _clearSearchIcon = new ReloadableTexture2D("UI/Icons/ClearSearch");
        private static readonly ReloadableTexture2D _refreshIcon = new ReloadableTexture2D("UI/Icons/Refresh");

        private static readonly IReadOnlyList<TradeListRowPresentation> _emptyRowPresentations =
            Array.AsReadOnly(Array.Empty<TradeListRowPresentation>());

        private static readonly IReadOnlyList<TradeCategory> _allCategoryOnly = Array.AsReadOnly(
            new[]
            {
                TradeCategory.All
            });

        private readonly SettlementIdentity _settlementIdentity;

        private string _settlementLabel;
        private TradeCategory _activeCategory = TradeCategory.All;
        private string _searchText = string.Empty;

        private SortableHeaderState<TradeSortMode> _sortState =
            new SortableHeaderState<TradeSortMode>(TradeSortMode.Name, SharedSortDirection.Ascending);

        private Vector2 _scrollPosition;

        private SnapshotRequestKind _pendingSnapshotRequest;
        private bool _snapshotRequestLoadingStateDrawn;
        private Exception _snapshotRequestException;
        private SettlementEligibilityCriteria _pendingEligibilityCriteria = SettlementEligibilityCriteria.Default;
        private int _pendingEligibilityRevision;
        private int _processedEligibilityRevision = -1;

        private TradeInventorySnapshot _relevanceSnapshot;
        private TraderSnapshot _relevanceTrader;
        private PlannerTradeRelevanceProjection _relevanceProjection = PlannerTradeRelevanceProjection.Empty;

        private TradeInventorySnapshot _projectedSnapshot;
        private TraderSnapshot _projectedTrader;
        private IReadOnlyList<TradeListRowPresentation> _rowPresentations = _emptyRowPresentations;
        private bool _showDetailsColumn;
        private bool _projectionDirty = true;

        private TraderSnapshot _categoryTrader;
        private string _categorySearchText;
        private IReadOnlyList<TradeCategory> _availableCategories = _allCategoryOnly;
        private bool _categoryAvailabilityDirty = true;

        public Dialog_SettlementStock(SettlementIdentity settlementIdentity)
        {
            _settlementIdentity = settlementIdentity ?? throw new ArgumentNullException(nameof(settlementIdentity));

            doCloseX = true;
            closeOnClickedOutside = false;
            absorbInputAroundWindow = false;
            forcePause = true;
        }

        public override Vector2 InitialSize =>
            _initialSize;

        public override void PreOpen()
        {
            base.PreOpen();

            ResetRelevanceProjection();
            ResolveInitialSettlementLabel();

            SettlementEligibilityCriteria criteria = SettlementTradeOverviewSettingsService.CurrentEligibilityCriteria;

            int eligibilityRevision = SettlementTradeOverviewSettingsService.EligibilityRevision;

            if (TradeInventorySnapshotService.TryReuseLoadedSnapshot(criteria, out TradeInventorySnapshot snapshot))
            {
                TraderSnapshot trader = ResolveTrader(snapshot);

                if (trader != null)
                {
                    _pendingSnapshotRequest = SnapshotRequestKind.None;
                    _pendingEligibilityCriteria = criteria;
                    _pendingEligibilityRevision = eligibilityRevision;
                    _processedEligibilityRevision = eligibilityRevision;
                    _snapshotRequestLoadingStateDrawn = false;
                    _snapshotRequestException = null;
                    _settlementLabel = trader.SettlementLabel;
                    RebuildRelevanceProjection(snapshot, trader);
                    InvalidateProjectionSources(snapshot, trader);
                    return;
                }
            }

            QueueSnapshotRequest(SnapshotRequestKind.GetOrBuild);
        }

        public override void PostClose()
        {
            ResetRelevanceProjection();
            _projectedSnapshot = null;
            _projectedTrader = null;
            _rowPresentations = _emptyRowPresentations;
            _showDetailsColumn = false;

            base.PostClose();
        }

        public override void WindowUpdate()
        {
            base.WindowUpdate();

            SynchronizeSnapshotRequestWithSettings();

            if (_pendingSnapshotRequest == SnapshotRequestKind.None || !_snapshotRequestLoadingStateDrawn)
                return;

            SnapshotRequestKind request = _pendingSnapshotRequest;
            SettlementEligibilityCriteria criteria = _pendingEligibilityCriteria;
            int eligibilityRevision = _pendingEligibilityRevision;

            _pendingSnapshotRequest = SnapshotRequestKind.None;

            try
            {
                TradeInventorySnapshot snapshot;

                if (request == SnapshotRequestKind.Refresh)
                {
                    snapshot = TradeInventorySnapshotService.Refresh(criteria);
                    _scrollPosition = Vector2.zero;
                }
                else
                {
                    snapshot = TradeInventorySnapshotService.GetOrBuild(criteria);
                }

                TraderSnapshot trader = ResolveTrader(snapshot);

                if (trader != null)
                    _settlementLabel = trader.SettlementLabel;

                _snapshotRequestException = null;
                RebuildRelevanceProjection(snapshot, trader);
                InvalidateProjectionSources(snapshot, trader);
            }
            catch (Exception exception)
            {
                _snapshotRequestException = exception;
                ResetRelevanceProjection();

                string operation = request == SnapshotRequestKind.Refresh ? "refresh" : "load";

                Log.Error(
                    "[Settlement Trade Overview] Failed to " + operation + " the settlement stock snapshot: " +
                    exception);
            }
            finally
            {
                _processedEligibilityRevision = eligibilityRevision;
            }
        }

        public override void DoWindowContents(Rect inRect)
        {
            using (ImGuiStateScope.Capture())
            {
                TradeInventorySnapshot snapshot = TradeInventorySnapshotService.CurrentSnapshot;
                TraderSnapshot trader = ResolveTrader(snapshot);

                if (trader != null)
                    _settlementLabel = trader.SettlementLabel;

                var headerRect = new Rect(inRect.x, inRect.y, inRect.width, HeaderHeight);
                DrawHeader(headerRect, trader, snapshot);

                var toolbarRect = new Rect(
                    inRect.x,
                    headerRect.yMax + RimWorldUiStyle.Metrics.SectionGap,
                    inRect.width,
                    RimWorldUiStyle.Metrics.SearchRowHeight);

                DrawToolbar(toolbarRect);

                var contentRect = new Rect(
                    inRect.x,
                    toolbarRect.yMax + RimWorldUiStyle.Metrics.SectionGap,
                    inRect.width,
                    Mathf.Max(0f, inRect.yMax - toolbarRect.yMax - RimWorldUiStyle.Metrics.SectionGap));

                SettlementStockPresentationState state = SettlementStockPresentationStateResolver.Resolve(
                    _pendingSnapshotRequest != SnapshotRequestKind.None,
                    _snapshotRequestException != null,
                    TradeInventorySnapshotService.State,
                    trader);

                if ((TradeInventorySnapshotService.State == TradeInventorySnapshotCacheState.Available ||
                     TradeInventorySnapshotService.State == TradeInventorySnapshotCacheState.Empty ||
                     TradeInventorySnapshotService.State == TradeInventorySnapshotCacheState.Partial) &&
                    snapshot == null)
                {
                    state = SettlementStockPresentationState.Error;
                }

                DrawState(contentRect, state, snapshot, trader);
            }

            if (_pendingSnapshotRequest != SnapshotRequestKind.None && Event.current.type == EventType.Repaint)
                _snapshotRequestLoadingStateDrawn = true;
        }

        private void DrawHeader(Rect rect, TraderSnapshot trader, TradeInventorySnapshot snapshot)
        {
            float iconSize = Mathf.Min(40f, rect.height);
            var iconRect = new Rect(rect.x, rect.y + (rect.height - iconSize) * 0.5f, iconSize, iconSize);

            float textX = rect.x;

            if (SettlementRuntimeAdapter.TryResolve(_settlementIdentity, out Settlement settlement) &&
                settlement.ExpandingIcon != null)
            {
                using (ImGuiStateScope.Capture())
                {
                    GUI.color = settlement.ExpandingIconColor;
                    GUI.DrawTexture(iconRect, settlement.ExpandingIcon, ScaleMode.ScaleToFit, true);
                }

                textX = iconRect.xMax + RimWorldUiStyle.Metrics.ControlGap;
            }

            float restockWidth = trader != null && snapshot != null ? RestockSummaryWidth : 0f;

            var titleRect = new Rect(
                textX,
                rect.y,
                Mathf.Max(0f, rect.xMax - textX - restockWidth),
                trader != null ? rect.height * 0.62f : rect.height);

            string settlementLabel = string.IsNullOrWhiteSpace(_settlementLabel)
                ? "STO.SettlementStock.UnknownSettlement".Translate().ToString()
                : _settlementLabel;

            using (ImGuiStateScope.Capture())
            {
                GUI.color = RimWorldUiStyle.Colors.PrimaryText;

                RimWorldUiWidgets.DrawTruncatedLabelWithTooltip(
                    titleRect,
                    "STO.SettlementStock.Header".Translate(settlementLabel).ToString(),
                    GameFont.Medium,
                    TextAnchor.MiddleLeft);
            }

            if (trader != null)
            {
                var traderRect = new Rect(
                    textX,
                    titleRect.yMax,
                    titleRect.width,
                    Mathf.Max(0f, rect.yMax - titleRect.yMax));

                using (ImGuiStateScope.Capture())
                {
                    GUI.color = RimWorldUiStyle.Colors.MutedText;

                    RimWorldUiWidgets.DrawTruncatedLabelWithTooltip(
                        traderRect,
                        trader.TraderLabel,
                        GameFont.Small,
                        TextAnchor.MiddleLeft);
                }
            }

            if (trader == null || snapshot == null)
                return;

            TradeCellPresentation restock = TradeValuePresentation.CreateRestock(
                trader.Restock,
                snapshot.CapturedAtTick);

            var restockRect = new Rect(rect.xMax - RestockSummaryWidth, rect.y, RestockSummaryWidth, rect.height);

            var restockText = "STO.SettlementStock.RestockSummary".Translate(restock.Text).ToString();

            using (ImGuiStateScope.Capture())
            {
                GUI.color = restock.Color;

                RimWorldUiWidgets.DrawTruncatedLabelWithTooltip(
                    restockRect,
                    restockText,
                    GameFont.Small,
                    TextAnchor.MiddleRight,
                    restock.Tooltip);
            }
        }

        private void DrawToolbar(Rect rect)
        {
            float buttonSize = RimWorldUiStyle.Metrics.IconButtonSize;
            float buttonY = rect.y + (rect.height - buttonSize) * 0.5f;

            var refreshRect = new Rect(rect.xMax - buttonSize, buttonY, buttonSize, buttonSize);

            var clearSearchRect = new Rect(
                refreshRect.x - RimWorldUiStyle.Metrics.IconButtonGap - buttonSize,
                buttonY,
                buttonSize,
                buttonSize);

            var searchRect = new Rect(
                rect.x,
                rect.y,
                Mathf.Max(0f, clearSearchRect.x - rect.x - RimWorldUiStyle.Metrics.IconButtonGap),
                rect.height);

            if (RimWorldUiWidgets.DrawIconSearchField(
                    searchRect,
                    _searchIcon.Texture,
                    RimWorldUiStyle.Colors.MutedText,
                    "STO.GlobalOverview.Search".Translate().ToString(),
                    ref _searchText))
            {
                InvalidateProjection(resetScroll: true, invalidateCategories: true);
            }

            bool canClearSearch = !string.IsNullOrEmpty(_searchText);

            if (RimWorldUiWidgets.DrawIconButton(
                    clearSearchRect,
                    _clearSearchIcon.Texture,
                    RimWorldUiStyle.Colors.MutedText,
                    canClearSearch,
                    "STO.GlobalOverview.ClearSearch".Translate().ToString()))
            {
                _searchText = string.Empty;
                InvalidateProjection(resetScroll: true, invalidateCategories: true);
            }

            bool refreshEnabled = _pendingSnapshotRequest == SnapshotRequestKind.None;

            if (RimWorldUiWidgets.DrawIconButton(
                    refreshRect,
                    _refreshIcon.Texture,
                    RimWorldUiStyle.Colors.Accent,
                    refreshEnabled,
                    "STO.GlobalOverview.Refresh".Translate().ToString()))
            {
                QueueSnapshotRequest(SnapshotRequestKind.Refresh);
            }
        }

        private void DrawState(
            Rect contentRect,
            SettlementStockPresentationState state,
            TradeInventorySnapshot snapshot,
            TraderSnapshot trader)
        {
            switch (state)
            {
                case SettlementStockPresentationState.Loading:
                    DrawMessage(
                        contentRect,
                        "STO.SettlementStock.Loading".Translate().ToString(),
                        RimWorldUiStyle.Colors.MutedText);
                    break;

                case SettlementStockPresentationState.Available:
                    DrawQueryableState(contentRect, snapshot, trader, showPartialWarning: false);
                    break;

                case SettlementStockPresentationState.Empty:
                    DrawQueryableState(
                        contentRect,
                        snapshot,
                        trader,
                        showPartialWarning: false,
                        forcedEmptyMessage: "STO.SettlementStock.Empty".Translate().ToString());
                    break;

                case SettlementStockPresentationState.Unavailable:
                    DrawMessage(
                        contentRect,
                        "STO.SettlementStock.Unavailable".Translate().ToString(),
                        RimWorldUiStyle.Colors.Warning);
                    break;

                case SettlementStockPresentationState.Partial:
                    DrawQueryableState(contentRect, snapshot, trader, showPartialWarning: true);
                    break;

                case SettlementStockPresentationState.Error:
                    DrawMessage(
                        contentRect,
                        "STO.SettlementStock.Error".Translate().ToString(),
                        RimWorldUiStyle.Colors.Negative);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(state));
            }
        }

        private void DrawQueryableState(
            Rect contentRect,
            TradeInventorySnapshot snapshot,
            TraderSnapshot trader,
            bool showPartialWarning,
            string forcedEmptyMessage = null)
        {
            if (snapshot == null || trader == null)
            {
                DrawMessage(
                    contentRect,
                    "STO.SettlementStock.Error".Translate().ToString(),
                    RimWorldUiStyle.Colors.Negative);
                return;
            }

            float currentY = contentRect.y;

            if (showPartialWarning)
            {
                float warningHeight = DrawMeasuredMessage(
                    new Rect(contentRect.x, currentY, contentRect.width, contentRect.height),
                    "STO.SettlementStock.Partial".Translate().ToString(),
                    RimWorldUiStyle.Colors.Warning);

                currentY += warningHeight + RimWorldUiStyle.Metrics.SectionGap;
            }

            float negotiatorHeight = TradeNegotiatorSummaryView.Draw(
                new Rect(contentRect.x, currentY, contentRect.width, Mathf.Max(0f, contentRect.yMax - currentY)),
                snapshot.Negotiator,
                NavigateToNegotiator);

            currentY += negotiatorHeight + RimWorldUiStyle.Metrics.SectionGap;

            float currencyHeight = DrawCurrencyRow(
                new Rect(contentRect.x, currentY, contentRect.width, RimWorldUiStyle.Metrics.StandardRowHeight),
                trader.Currency);

            currentY += currencyHeight + RimWorldUiStyle.Metrics.SectionGap;

            EnsureCategoryAvailability(trader);

            float categoryHeight = TradeCategoryTabsView.Draw(
                new Rect(contentRect.x, currentY, contentRect.width, Mathf.Max(0f, contentRect.yMax - currentY)),
                _availableCategories,
                ref _activeCategory,
                out bool categoryChanged);

            if (categoryChanged)
                InvalidateProjection(resetScroll: true, invalidateCategories: false);

            currentY += categoryHeight + RimWorldUiStyle.Metrics.SectionGap;

            EnsureProjection(snapshot, trader);

            string emptyMessage = forcedEmptyMessage ?? (trader.EntryCount == 0
                ? "STO.SettlementStock.NoTradeEntries".Translate().ToString()
                : "STO.SettlementStock.NoMatches".Translate().ToString());

            var listRect = new Rect(
                contentRect.x,
                currentY,
                contentRect.width,
                Mathf.Max(0f, contentRect.yMax - currentY));

            if (TradeListView.Draw(
                    listRect,
                    _rowPresentations,
                    emptyMessage,
                    TradeListMode.Settlement,
                    _showDetailsColumn,
                    null,
                    ref _scrollPosition,
                    ref _sortState))
            {
                InvalidateProjection(resetScroll: true, invalidateCategories: false);
            }
        }

        private static float DrawCurrencyRow(Rect rect, TradeCurrencySnapshot currency)
        {
            if (currency == null)
            {
                using (ImGuiStateScope.Capture())
                {
                    GUI.color = RimWorldUiStyle.Colors.MutedText;

                    RimWorldUiWidgets.DrawTruncatedLabelWithTooltip(
                        rect,
                        "STO.SettlementStock.CurrencyUnavailable".Translate().ToString(),
                        GameFont.Small,
                        TextAnchor.MiddleLeft);
                }

                DrawCurrencyDivider(rect);
                return rect.height;
            }

            float iconSize = Mathf.Min(RimWorldUiStyle.Metrics.IconButtonSize, rect.height);

            var iconRect = new Rect(rect.x, rect.y + (rect.height - iconSize) * 0.5f, iconSize, iconSize);

            var infoRect = new Rect(
                iconRect.xMax + RimWorldUiStyle.Metrics.IconButtonGap,
                rect.y + (rect.height - iconSize) * 0.5f,
                iconSize,
                iconSize);

            const float CountWidth = 96f;

            var countRect = new Rect(rect.xMax - CountWidth, rect.y, CountWidth, rect.height);

            var labelRect = new Rect(
                infoRect.xMax + RimWorldUiStyle.Metrics.IconButtonGap,
                rect.y,
                Mathf.Max(0f, countRect.x - infoRect.xMax - RimWorldUiStyle.Metrics.ControlGap),
                rect.height);

            bool hasDefinition = TradeEntryRuntimeAdapter.TryResolveDefinition(currency, out ThingDef definition);

            if (hasDefinition)
                Widgets.ThingIcon(iconRect, definition);

            var infoTooltip = "STO.TradeList.Info.Open".Translate(currency.Label).ToString();

            if (RimWorldUiWidgets.DrawIconButton(infoRect, TexButton.Info, hasDefinition, infoTooltip))
                TradeEntryRuntimeAdapter.TryOpenInfoCard(currency);

            TipSignal? nativeTooltip = TradeEntryRuntimeAdapter.TryCreateNativeTooltip(
                currency,
                definition,
                out TipSignal resolvedTooltip)
                ? resolvedTooltip
                : (TipSignal?)null;

            using (ImGuiStateScope.Capture())
            {
                GUI.color = RimWorldUiStyle.Colors.PrimaryText;

                RimWorldUiWidgets.DrawTruncatedLabelWithTooltip(
                    labelRect,
                    currency.Label,
                    GameFont.Small,
                    TextAnchor.MiddleLeft,
                    nativeTooltip);

                RimWorldUiWidgets.DrawTruncatedLabelWithTooltip(
                    countRect,
                    currency.Count.ToString(),
                    GameFont.Small,
                    TextAnchor.MiddleRight);
            }

            DrawCurrencyDivider(rect);
            return rect.height;
        }

        private static void DrawCurrencyDivider(Rect rect)
        {
            RimWorldUiWidgets.DrawTableDivider(
                new Rect(
                    rect.x,
                    rect.yMax - RimWorldUiStyle.Metrics.TableDividerWidth,
                    rect.width,
                    RimWorldUiStyle.Metrics.TableDividerWidth));
        }

        private TraderSnapshot ResolveTrader(TradeInventorySnapshot snapshot)
        {
            if (snapshot == null)
                return null;

            return SettlementTraderSnapshotResolver.TryResolve(snapshot, _settlementIdentity, out TraderSnapshot trader)
                ? trader
                : null;
        }

        private void EnsureCategoryAvailability(TraderSnapshot trader)
        {
            if (!_categoryAvailabilityDirty && ReferenceEquals(_categoryTrader, trader) && string.Equals(
                    _categorySearchText,
                    _searchText,
                    StringComparison.Ordinal))
            {
                return;
            }

            _availableCategories = TradeCategoryAvailability.Resolve(trader, _searchText);
            _categoryTrader = trader;
            _categorySearchText = _searchText;
            _categoryAvailabilityDirty = false;

            if (TradeCategoryAvailability.Contains(_availableCategories, _activeCategory))
                return;

            _activeCategory = TradeCategory.All;
            _projectionDirty = true;
            _scrollPosition = Vector2.zero;
        }

        private void EnsureProjection(TradeInventorySnapshot snapshot, TraderSnapshot trader)
        {
            EnsureRelevanceProjection(snapshot, trader);

            if (!_projectionDirty && ReferenceEquals(_projectedSnapshot, snapshot) &&
                ReferenceEquals(_projectedTrader, trader))
            {
                return;
            }

            var criteria = new TradeQueryCriteria(
                _activeCategory,
                _searchText,
                _sortState.Column,
                ToTradeSortDirection(_sortState.Direction));

            IReadOnlyList<TradeQueryEntry> queryResult = TradeSnapshotQuery.Execute(
                trader,
                criteria,
                _relevanceProjection.GetMatchCount);

            _showDetailsColumn = TradeDetailsColumnPolicy.ShouldShow(
                queryResult,
                _relevanceProjection,
                _sortState.Column == TradeSortMode.Details);
            _rowPresentations = TradeListRowPresentationBuilder.Build(
                queryResult,
                snapshot,
                TradeListMode.Settlement,
                _relevanceProjection);
            _projectedSnapshot = snapshot;
            _projectedTrader = trader;
            _projectionDirty = false;
        }

        private void EnsureRelevanceProjection(TradeInventorySnapshot snapshot, TraderSnapshot trader)
        {
            if (ReferenceEquals(_relevanceSnapshot, snapshot) && ReferenceEquals(_relevanceTrader, trader))
                return;

            RebuildRelevanceProjection(snapshot, trader);
        }

        private void RebuildRelevanceProjection(TradeInventorySnapshot snapshot, TraderSnapshot trader)
        {
            _relevanceProjection = snapshot == null || trader == null
                ? PlannerTradeRelevanceProjection.Empty
                : PlannerTradeRelevanceProjectionBuilder.Build(trader);
            _relevanceSnapshot = snapshot;
            _relevanceTrader = trader;
            _projectionDirty = true;
        }

        private void ResetRelevanceProjection()
        {
            _relevanceSnapshot = null;
            _relevanceTrader = null;
            _relevanceProjection = PlannerTradeRelevanceProjection.Empty;
            _projectionDirty = true;
        }

        private void InvalidateProjectionSources(TradeInventorySnapshot snapshot, TraderSnapshot trader)
        {
            if (!ReferenceEquals(_projectedSnapshot, snapshot) || !ReferenceEquals(_projectedTrader, trader))
                _projectionDirty = true;

            if (!ReferenceEquals(_categoryTrader, trader))
                _categoryAvailabilityDirty = true;
        }

        private void ResolveInitialSettlementLabel()
        {
            if (SettlementRuntimeAdapter.TryResolve(_settlementIdentity, out Settlement settlement))
                _settlementLabel = settlement.LabelCap;
        }

        private void SynchronizeSnapshotRequestWithSettings()
        {
            int currentRevision = SettlementTradeOverviewSettingsService.EligibilityRevision;

            if (_pendingSnapshotRequest == SnapshotRequestKind.None)
            {
                if (_processedEligibilityRevision != currentRevision)
                    QueueSnapshotRequest(SnapshotRequestKind.GetOrBuild);

                return;
            }

            if (_pendingEligibilityRevision == currentRevision)
                return;

            _pendingEligibilityCriteria = SettlementTradeOverviewSettingsService.CurrentEligibilityCriteria;
            _pendingEligibilityRevision = currentRevision;
            _snapshotRequestLoadingStateDrawn = false;
        }

        private void QueueSnapshotRequest(SnapshotRequestKind request)
        {
            if (request == SnapshotRequestKind.None)
                throw new ArgumentOutOfRangeException(nameof(request));

            _pendingSnapshotRequest = request;
            _pendingEligibilityCriteria = SettlementTradeOverviewSettingsService.CurrentEligibilityCriteria;
            _pendingEligibilityRevision = SettlementTradeOverviewSettingsService.EligibilityRevision;
            _snapshotRequestLoadingStateDrawn = false;
            _snapshotRequestException = null;
        }

        private void InvalidateProjection(bool resetScroll, bool invalidateCategories)
        {
            _projectionDirty = true;

            if (invalidateCategories)
                _categoryAvailabilityDirty = true;

            if (resetScroll)
                _scrollPosition = Vector2.zero;
        }

        private static TradeSortDirection ToTradeSortDirection(SharedSortDirection direction)
        {
            switch (direction)
            {
                case SharedSortDirection.Ascending:
                    return TradeSortDirection.Ascending;

                case SharedSortDirection.Descending:
                    return TradeSortDirection.Descending;

                default:
                    throw new ArgumentOutOfRangeException(nameof(direction));
            }
        }

        private void NavigateToNegotiator(TradeNegotiatorSnapshot negotiator)
        {
            if (negotiator == null)
                throw new ArgumentNullException(nameof(negotiator));

            if (PawnNavigationAdapter.TryNavigate(negotiator.PawnId))
            {
                Close(doCloseSound: false);
                return;
            }

            var message = "STO.GlobalOverview.Navigation.NegotiatorUnavailable".Translate(negotiator.Label).ToString();

            Messages.Message(message, MessageTypeDefOf.RejectInput, historical: false);
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

        private static void DrawMessage(Rect rect, string message, Color color)
        {
            using (ImGuiStateScope.Capture())
            {
                Text.Font = GameFont.Small;
                Text.Anchor = TextAnchor.UpperLeft;
                GUI.color = color;

                Widgets.Label(rect, message);
            }
        }

        private enum SnapshotRequestKind
        {
            None,
            GetOrBuild,
            Refresh
        }
    }
}