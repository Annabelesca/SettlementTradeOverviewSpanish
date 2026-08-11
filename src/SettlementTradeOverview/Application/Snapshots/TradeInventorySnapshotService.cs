using System;
using SettlementTradeOverview.Cache;
using SettlementTradeOverview.Domain.Eligibility;
using SettlementTradeOverview.Domain.Identity;
using SettlementTradeOverview.Domain.Snapshots;
using SettlementTradeOverview.Integration.Context;
using SettlementTradeOverview.Integration.Discovery;
using SettlementTradeOverview.Integration.Runtime;
using SettlementTradeOverview.Integration.Snapshots;

namespace SettlementTradeOverview.Application.Snapshots
{
    internal static class TradeInventorySnapshotService
    {
        private static readonly TradeInventorySnapshotCache _cache = new TradeInventorySnapshotCache();

        public static TradeInventorySnapshotCacheState State =>
            _cache.State;

        public static TradeInventorySnapshot CurrentSnapshot =>
            _cache.Snapshot;

        public static bool HasSnapshot =>
            _cache.HasSnapshot;

        public static bool TryReuseLoadedSnapshot(
            SettlementEligibilityCriteria criteria,
            out TradeInventorySnapshot snapshot)
        {
            if (criteria == null)
                throw new ArgumentNullException(nameof(criteria));

            snapshot = null;

            if (!_cache.HasSnapshot)
                return false;

            try
            {
                TradeInventorySnapshotReuseKey reuseKey;

                if (!PlayerTradeContextAdapter.TryCreateReuseContext(out PlayerTradeReuseContext context))
                {
                    reuseKey = TradeInventorySnapshotReuseKey.CreateUnavailableContext(criteria);
                }
                else
                {
                    TradeNegotiatorSelection negotiatorSelection = TradeNegotiatorAdapter.Select(context);

                    reuseKey = TradeInventorySnapshotReuseKey.CreateAvailableContext(
                        context.OriginMap.uniqueID,
                        context.OriginTile,
                        negotiatorSelection.Snapshot,
                        criteria,
                        context.HasPoweredCommsConsole,
                        context.IsRoyaltyActive);
                }

                if (!_cache.TryGetReusableSnapshot(reuseKey, out TradeInventorySnapshot reusableSnapshot))
                    return false;

                if (reuseKey.IsContextAvailable && !AreCachedTradersAvailable(reusableSnapshot))
                    return false;

                snapshot = reusableSnapshot;
                return true;
            }
            catch
            {
                snapshot = null;
                return false;
            }
        }

        public static TradeInventorySnapshot GetOrBuild(SettlementEligibilityCriteria criteria)
        {
            if (criteria == null)
                throw new ArgumentNullException(nameof(criteria));

            return ResolveSnapshot(criteria, forceRefresh: false);
        }

        public static TradeInventorySnapshot Refresh(SettlementEligibilityCriteria criteria)
        {
            if (criteria == null)
                throw new ArgumentNullException(nameof(criteria));

            _cache.Invalidate();
            TradeEntryRuntimeTargetCache.Clear();

            return ResolveSnapshot(criteria, forceRefresh: true);
        }

        public static void Invalidate()
        {
            _cache.Invalidate();
            TradeEntryRuntimeTargetCache.Clear();
        }

        private static TradeInventorySnapshot ResolveSnapshot(SettlementEligibilityCriteria criteria, bool forceRefresh)
        {
            if (!PlayerTradeContextAdapter.TryCreate(out PlayerTradeContext context))
            {
                var unavailableKey = TradeInventorySnapshotCacheKey.CreateUnavailableContext(criteria);

                return ResolveFromCache(
                    unavailableKey,
                    () => TradeInventorySnapshotAdapter.Build(criteria),
                    forceRefresh);
            }

            TradeNegotiatorSelection negotiatorSelection = TradeNegotiatorAdapter.Select(context);

            TraderDiscoveryResult discovery = TraderDiscoveryAdapter.Discover(context, criteria);

            var traderIdentities = new TraderIdentity[discovery.Sources.Count];

            for (var index = 0; index < discovery.Sources.Count; index++)
                traderIdentities[index] = discovery.Sources[index].TraderIdentity;

            var reuseKey = TradeInventorySnapshotReuseKey.CreateAvailableContext(
                context.OriginMap.uniqueID,
                context.OriginTile,
                negotiatorSelection.Snapshot,
                criteria,
                context.HasPoweredCommsConsole,
                context.IsRoyaltyActive);

            var key = TradeInventorySnapshotCacheKey.CreateAvailableContext(
                reuseKey,
                traderIdentities,
                discovery.FailedCandidateCount);

            return ResolveFromCache(
                key,
                () => TradeInventorySnapshotAdapter.Build(context, negotiatorSelection, discovery),
                forceRefresh);
        }

        private static bool AreCachedTradersAvailable(TradeInventorySnapshot snapshot)
        {
            foreach (TraderSnapshot trader in snapshot.Traders)
            {
                if (!SettlementRuntimeAdapter.TryResolve(trader.SettlementIdentity, out _))
                    return false;
            }

            return true;
        }

        private static TradeInventorySnapshot ResolveFromCache(
            TradeInventorySnapshotCacheKey key,
            Func<TradeInventorySnapshot> snapshotFactory,
            bool forceRefresh)
        {
            Func<TradeInventorySnapshot> guardedFactory = () => BuildWithFreshRuntimeTargets(snapshotFactory);

            return forceRefresh ? _cache.Refresh(key, guardedFactory) : _cache.GetOrBuild(key, guardedFactory);
        }

        private static TradeInventorySnapshot BuildWithFreshRuntimeTargets(Func<TradeInventorySnapshot> snapshotFactory)
        {
            TradeEntryRuntimeTargetCache.Clear();

            try
            {
                TradeInventorySnapshot snapshot = snapshotFactory();

                if (snapshot == null || !ContainsItemEntries(snapshot))
                    TradeEntryRuntimeTargetCache.Clear();

                return snapshot;
            }
            catch
            {
                TradeEntryRuntimeTargetCache.Clear();
                throw;
            }
        }

        private static bool ContainsItemEntries(TradeInventorySnapshot snapshot)
        {
            foreach (TraderSnapshot trader in snapshot.Traders)
            {
                foreach (TradeEntrySnapshot entry in trader.Entries)
                {
                    if (entry.Kind == TradeEntryKind.Item)
                        return true;
                }
            }

            return false;
        }
    }
}