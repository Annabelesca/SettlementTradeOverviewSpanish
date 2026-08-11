using System;
using System.Collections.Generic;
using Escarval.RimWorld.UI;
using RimWorld;
using SettlementTradeOverview.Application.Snapshots;
using SettlementTradeOverview.Domain.Categories;
using SettlementTradeOverview.Domain.Eligibility;
using SettlementTradeOverview.Domain.Snapshots;
using SettlementTradeOverview.Integration.Navigation;
using SettlementTradeOverview.Integration.Planner;
using SettlementTradeOverview.Query;
using SettlementTradeOverview.Settings;
using SettlementTradeOverview.UI.TradeList;
using UnityEngine;
using Verse;
using SharedSortDirection = Escarval.RimWorld.UI.SortDirection;

namespace SettlementTradeOverview.UI.GlobalOverview
{
    // ReSharper disable once InconsistentNaming
    [StaticConstructorOnStartup]
    public sealed class MainTabWindow_SettlementTradeOverview : MainTabWindow
    {
        private static readonly Vector2 _requestedTabSize = new Vector2(1100f, 600f);

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
        private PlannerTradeRelevanceProjection _relevanceProjection = PlannerTradeRelevanceProjection.Empty;

        private TradeInventorySnapshot _projectedSnapshot;
        private IReadOnlyList<TradeListRowPresentation> _rowPresentations = _emptyRowPresentations;
        private bool _showDetailsColumn;
        private bool _projectionDirty = true;

        private TradeInventorySnapshot _categorySnapshot;
        private string _categorySearchText;
        private IReadOnlyList<TradeCategory> _availableCategories = _allCategoryOnly;
        private bool _categoryAvailabilityDirty = true;

        public override Vector2 RequestedTabSize =>
            _requestedTabSize;

        public override void PreOpen()
        {
            base.PreOpen();

            ResetRelevanceProjection();

            SettlementEligibilityCriteria criteria = SettlementTradeOverviewSettingsService.CurrentEligibilityCriteria;

            int eligibilityRevision = SettlementTradeOverviewSettingsService.EligibilityRevision;

            if (TradeInventorySnapshotService.TryReuseLoadedSnapshot(criteria, out TradeInventorySnapshot snapshot))
            {
                _pendingSnapshotRequest = SnapshotRequestKind.None;
                _pendingEligibilityCriteria = criteria;
                _pendingEligibilityRevision = eligibilityRevision;
                _processedEligibilityRevision = eligibilityRevision;
                _snapshotRequestLoadingStateDrawn = false;
                _snapshotRequestException = null;
                RebuildRelevanceProjection(snapshot);
                InvalidateProjectionSources(snapshot);
                return;
            }

            QueueSnapshotRequest(SnapshotRequestKind.GetOrBuild);
        }

        public override void PostClose()
        {
            ResetRelevanceProjection();
            _projectedSnapshot = null;
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

                _snapshotRequestException = null;
                RebuildRelevanceProjection(snapshot);
                InvalidateProjectionSources(snapshot);
            }
            catch (Exception exception)
            {
                _snapshotRequestException = exception;
                ResetRelevanceProjection();

                string operation = request == SnapshotRequestKind.Refresh ? "refresh" : "load";

                Log.Error(
                    "[Settlement Trade Overview] Failed to " + operation + " the global overview snapshot: " +
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
                var toolbarRect = new Rect(inRect.x, inRect.y, inRect.width, RimWorldUiStyle.Metrics.SearchRowHeight);

                DrawToolbar(toolbarRect);

                var contentRect = new Rect(
                    inRect.x,
                    toolbarRect.yMax + RimWorldUiStyle.Metrics.SectionGap,
                    inRect.width,
                    Mathf.Max(0f, inRect.yMax - toolbarRect.yMax - RimWorldUiStyle.Metrics.SectionGap));

                GlobalOverviewPresentationState state = GlobalOverviewPresentationStateResolver.Resolve(
                    _pendingSnapshotRequest != SnapshotRequestKind.None,
                    _snapshotRequestException != null,
                    TradeInventorySnapshotService.State);

                TradeInventorySnapshot snapshot = TradeInventorySnapshotService.CurrentSnapshot;

                if ((state == GlobalOverviewPresentationState.Available ||
                     state == GlobalOverviewPresentationState.Partial) && snapshot == null)
                {
                    state = GlobalOverviewPresentationState.Error;
                }

                DrawState(contentRect, state, snapshot);
            }

            if (_pendingSnapshotRequest != SnapshotRequestKind.None && Event.current.type == EventType.Repaint)
                _snapshotRequestLoadingStateDrawn = true;
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

        private void DrawState(Rect contentRect, GlobalOverviewPresentationState state, TradeInventorySnapshot snapshot)
        {
            switch (state)
            {
                case GlobalOverviewPresentationState.Loading:
                    DrawMessage(
                        contentRect,
                        "STO.GlobalOverview.Loading".Translate().ToString(),
                        RimWorldUiStyle.Colors.MutedText);
                    break;

                case GlobalOverviewPresentationState.Available:
                    DrawQueryableState(contentRect, snapshot, showPartialWarning: false);
                    break;

                case GlobalOverviewPresentationState.Empty:
                    DrawMessage(
                        contentRect,
                        "STO.GlobalOverview.Empty".Translate().ToString(),
                        RimWorldUiStyle.Colors.MutedText);
                    break;

                case GlobalOverviewPresentationState.Unavailable:
                    DrawMessage(
                        contentRect,
                        "STO.GlobalOverview.Unavailable".Translate().ToString(),
                        RimWorldUiStyle.Colors.Warning);
                    break;

                case GlobalOverviewPresentationState.Partial:
                    DrawQueryableState(contentRect, snapshot, showPartialWarning: true);
                    break;

                case GlobalOverviewPresentationState.Error:
                    DrawMessage(
                        contentRect,
                        "STO.GlobalOverview.Error".Translate().ToString(),
                        RimWorldUiStyle.Colors.Negative);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(state));
            }
        }

        private void DrawQueryableState(Rect contentRect, TradeInventorySnapshot snapshot, bool showPartialWarning)
        {
            if (snapshot == null)
            {
                DrawMessage(
                    contentRect,
                    "STO.GlobalOverview.Error".Translate().ToString(),
                    RimWorldUiStyle.Colors.Negative);
                return;
            }

            float currentY = contentRect.y;

            if (showPartialWarning)
            {
                var warning = "STO.GlobalOverview.Partial".Translate().ToString();
                float warningHeight = DrawMeasuredMessage(
                    new Rect(contentRect.x, currentY, contentRect.width, contentRect.height),
                    warning,
                    RimWorldUiStyle.Colors.Warning);

                currentY += warningHeight + RimWorldUiStyle.Metrics.SectionGap;
            }

            var summary = "STO.GlobalOverview.AvailableSummary".Translate(snapshot.TraderCount, snapshot.EntryCount)
                .ToString();

            float summaryHeight = DrawMeasuredMessage(
                new Rect(contentRect.x, currentY, contentRect.width, Mathf.Max(0f, contentRect.yMax - currentY)),
                summary,
                RimWorldUiStyle.Colors.PrimaryText);

            currentY += summaryHeight + RimWorldUiStyle.Metrics.SectionGap;

            float negotiatorSummaryHeight = TradeNegotiatorSummaryView.Draw(
                new Rect(contentRect.x, currentY, contentRect.width, Mathf.Max(0f, contentRect.yMax - currentY)),
                snapshot.Negotiator,
                NavigateToNegotiator);

            currentY += negotiatorSummaryHeight + RimWorldUiStyle.Metrics.SectionGap;

            EnsureCategoryAvailability(snapshot);

            float categoryHeight = TradeCategoryTabsView.Draw(
                new Rect(contentRect.x, currentY, contentRect.width, Mathf.Max(0f, contentRect.yMax - currentY)),
                _availableCategories,
                ref _activeCategory,
                out bool categoryChanged);

            if (categoryChanged)
                InvalidateProjection(resetScroll: true, invalidateCategories: false);

            currentY += categoryHeight + RimWorldUiStyle.Metrics.SectionGap;

            EnsureProjection(snapshot);

            var listRect = new Rect(
                contentRect.x,
                currentY,
                contentRect.width,
                Mathf.Max(0f, contentRect.yMax - currentY));

            if (TradeListView.Draw(
                    listRect,
                    _rowPresentations,
                    "STO.GlobalOverview.NoMatches".Translate().ToString(),
                    TradeListMode.Global,
                    _showDetailsColumn,
                    NavigateToSettlement,
                    ref _scrollPosition,
                    ref _sortState))
            {
                InvalidateProjection(resetScroll: true, invalidateCategories: false);
            }
        }

        private void EnsureCategoryAvailability(TradeInventorySnapshot snapshot)
        {
            if (!_categoryAvailabilityDirty && ReferenceEquals(_categorySnapshot, snapshot) && string.Equals(
                    _categorySearchText,
                    _searchText,
                    StringComparison.Ordinal))
            {
                return;
            }

            _availableCategories = TradeCategoryAvailability.Resolve(snapshot, _searchText);
            _categorySnapshot = snapshot;
            _categorySearchText = _searchText;
            _categoryAvailabilityDirty = false;

            if (TradeCategoryAvailability.Contains(_availableCategories, _activeCategory))
                return;

            _activeCategory = TradeCategory.All;
            _projectionDirty = true;
            _scrollPosition = Vector2.zero;
        }

        private void EnsureProjection(TradeInventorySnapshot snapshot)
        {
            EnsureRelevanceProjection(snapshot);

            if (!_projectionDirty && ReferenceEquals(_projectedSnapshot, snapshot))
                return;

            var criteria = new TradeQueryCriteria(
                _activeCategory,
                _searchText,
                _sortState.Column,
                ToTradeSortDirection(_sortState.Direction));

            IReadOnlyList<TradeQueryEntry> queryResult = TradeSnapshotQuery.Execute(
                snapshot,
                criteria,
                _relevanceProjection.GetMatchCount);

            _showDetailsColumn = TradeDetailsColumnPolicy.ShouldShow(
                queryResult,
                _relevanceProjection,
                _sortState.Column == TradeSortMode.Details);
            _rowPresentations = TradeListRowPresentationBuilder.Build(
                queryResult,
                snapshot,
                TradeListMode.Global,
                _relevanceProjection);
            _projectedSnapshot = snapshot;
            _projectionDirty = false;
        }

        private void EnsureRelevanceProjection(TradeInventorySnapshot snapshot)
        {
            if (ReferenceEquals(_relevanceSnapshot, snapshot))
                return;

            RebuildRelevanceProjection(snapshot);
        }

        private void RebuildRelevanceProjection(TradeInventorySnapshot snapshot)
        {
            _relevanceProjection = snapshot == null
                ? PlannerTradeRelevanceProjection.Empty
                : PlannerTradeRelevanceProjectionBuilder.Build(snapshot);
            _relevanceSnapshot = snapshot;
            _projectionDirty = true;
        }

        private void ResetRelevanceProjection()
        {
            _relevanceSnapshot = null;
            _relevanceProjection = PlannerTradeRelevanceProjection.Empty;
            _projectionDirty = true;
        }

        private void InvalidateProjectionSources(TradeInventorySnapshot snapshot)
        {
            if (!ReferenceEquals(_projectedSnapshot, snapshot))
                _projectionDirty = true;

            if (!ReferenceEquals(_categorySnapshot, snapshot))
                _categoryAvailabilityDirty = true;
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

        private static void NavigateToNegotiator(TradeNegotiatorSnapshot negotiator)
        {
            if (negotiator == null)
                throw new ArgumentNullException(nameof(negotiator));

            if (PawnNavigationAdapter.TryNavigate(negotiator.PawnId))
                return;

            var message = "STO.GlobalOverview.Navigation.NegotiatorUnavailable".Translate(negotiator.Label).ToString();

            Messages.Message(message, MessageTypeDefOf.RejectInput, historical: false);
        }

        private static void NavigateToSettlement(TraderSnapshot trader)
        {
            if (trader == null)
                throw new ArgumentNullException(nameof(trader));

            if (SettlementNavigationAdapter.TryNavigate(trader.SettlementIdentity))
                return;

            var message = "STO.TradeList.Navigation.SettlementUnavailable".Translate(trader.SettlementLabel).ToString();

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

        private static void DrawMessage(Rect contentRect, string message, Color color)
        {
            using (ImGuiStateScope.Capture())
            {
                Text.Font = GameFont.Small;
                Text.Anchor = TextAnchor.UpperLeft;
                GUI.color = color;

                Widgets.Label(contentRect, message);
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