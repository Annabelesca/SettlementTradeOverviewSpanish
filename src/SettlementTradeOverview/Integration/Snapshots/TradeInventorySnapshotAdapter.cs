using System;
using System.Collections.Generic;
using SettlementTradeOverview.Domain.Eligibility;
using SettlementTradeOverview.Domain.Snapshots;
using SettlementTradeOverview.Integration.Context;
using SettlementTradeOverview.Integration.Discovery;
using Verse;

namespace SettlementTradeOverview.Integration.Snapshots
{
    internal static class TradeInventorySnapshotAdapter
    {
        private const string LogPrefix = "[SettlementTradeOverview]";

        public static TradeInventorySnapshot Build(SettlementEligibilityCriteria criteria)
        {
            if (criteria == null)
                throw new ArgumentNullException(nameof(criteria));

            int capturedAtTick = GetCapturedAtTick();
            PlayerTradeContext context = null;
            var negotiatorSelection = default(TradeNegotiatorSelection);

            try
            {
                if (!PlayerTradeContextAdapter.TryCreate(out context))
                {
                    return new TradeInventorySnapshot(
                        SnapshotAvailability.Unavailable,
                        capturedAtTick,
                        null,
                        null,
                        Array.Empty<TraderSnapshot>());
                }

                negotiatorSelection = TradeNegotiatorAdapter.Select(context);

                TraderDiscoveryResult discovery = TraderDiscoveryAdapter.Discover(context, criteria);

                return BuildCore(context, negotiatorSelection, discovery, capturedAtTick);
            }
            catch (Exception exception)
            {
                Log.Warning($"{LogPrefix} Trade inventory snapshot build failed.\n" + exception);

                return new TradeInventorySnapshot(
                    SnapshotAvailability.Failed,
                    capturedAtTick,
                    context?.OriginTile,
                    negotiatorSelection.Snapshot,
                    Array.Empty<TraderSnapshot>());
            }
        }

        public static TradeInventorySnapshot Build(
            PlayerTradeContext context,
            TradeNegotiatorSelection negotiatorSelection,
            TraderDiscoveryResult discovery)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            if (discovery == null)
                throw new ArgumentNullException(nameof(discovery));

            int capturedAtTick = GetCapturedAtTick();

            try
            {
                return BuildCore(context, negotiatorSelection, discovery, capturedAtTick);
            }
            catch (Exception exception)
            {
                Log.Warning($"{LogPrefix} Prepared trade inventory snapshot build failed.\n" + exception);

                return new TradeInventorySnapshot(
                    SnapshotAvailability.Failed,
                    capturedAtTick,
                    context.OriginTile,
                    negotiatorSelection.Snapshot,
                    Array.Empty<TraderSnapshot>());
            }
        }

        private static TradeInventorySnapshot BuildCore(
            PlayerTradeContext context,
            TradeNegotiatorSelection negotiatorSelection,
            TraderDiscoveryResult discovery,
            int capturedAtTick)
        {
            if (!discovery.IsContextAvailable)
            {
                return new TradeInventorySnapshot(
                    SnapshotAvailability.Unavailable,
                    capturedAtTick,
                    context.OriginTile,
                    negotiatorSelection.Snapshot,
                    Array.Empty<TraderSnapshot>());
            }

            var traderSnapshots = new List<TraderSnapshot>(discovery.Sources.Count);

            foreach (DiscoveredTraderSource source in discovery.Sources)
            {
                TraderSnapshot traderSnapshot;

                try
                {
                    traderSnapshot = TraderSnapshotAdapter.Build(source, context, negotiatorSelection.Negotiator);
                }
                catch (Exception exception)
                {
                    Log.Warning(
                        $"{LogPrefix} Unexpected trader snapshot failure for " +
                        $"{source.TraderIdentity}. The remaining traders will still be processed.\n" + exception);

                    traderSnapshot = TraderSnapshotAdapter.CreateFailedSnapshot(
                        source,
                        TradeDistance.Unavailable,
                        TradeRestock.Unavailable);
                }

                traderSnapshots.Add(traderSnapshot);
            }

            SnapshotAvailability availability =
                SnapshotAvailabilityResolver.ResolveInventory(discovery.HasFailures, traderSnapshots);

            return new TradeInventorySnapshot(
                availability,
                capturedAtTick,
                context.OriginTile,
                negotiatorSelection.Snapshot,
                traderSnapshots);
        }

        private static int GetCapturedAtTick()
        {
            try
            {
                int ticksGame = Find.TickManager?.TicksGame ?? 0;
                return Math.Max(0, ticksGame);
            }
            catch (Exception)
            {
                return 0;
            }
        }
    }

    internal static class SnapshotAvailabilityResolver
    {
        public static SnapshotAvailability ResolveTrader(bool hasStockData, int failedConversionCount)
        {
            if (failedConversionCount < 0)
                throw new ArgumentOutOfRangeException(nameof(failedConversionCount));

            if (failedConversionCount > 0)
            {
                return hasStockData ? SnapshotAvailability.Partial : SnapshotAvailability.Failed;
            }

            return hasStockData ? SnapshotAvailability.Available : SnapshotAvailability.Empty;
        }

        public static SnapshotAvailability ResolveInventory(
            bool hasDiscoveryFailures,
            IReadOnlyList<TraderSnapshot> traders)
        {
            if (traders == null)
                throw new ArgumentNullException(nameof(traders));

            var hasStockData = false;
            bool hasSnapshotFailures = hasDiscoveryFailures;

            foreach (TraderSnapshot candidate in traders)
            {
                TraderSnapshot trader = candidate ?? throw new ArgumentException(
                    "Trader snapshots cannot contain null values.",
                    nameof(traders));

                if (trader.EntryCount > 0 || trader.Currency != null)
                    hasStockData = true;

                if (trader.Availability == SnapshotAvailability.Partial ||
                    trader.Availability == SnapshotAvailability.Failed ||
                    trader.Availability == SnapshotAvailability.Unavailable)
                {
                    hasSnapshotFailures = true;
                }
            }

            if (hasSnapshotFailures)
                return SnapshotAvailability.Partial;

            return hasStockData ? SnapshotAvailability.Available : SnapshotAvailability.Empty;
        }
    }
}