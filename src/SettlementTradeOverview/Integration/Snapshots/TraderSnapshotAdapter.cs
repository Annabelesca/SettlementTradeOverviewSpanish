using System;
using System.Collections.Generic;
using RimWorld;
using SettlementTradeOverview.Domain.Identity;
using SettlementTradeOverview.Domain.Snapshots;
using SettlementTradeOverview.Integration.Context;
using SettlementTradeOverview.Integration.Discovery;
using SettlementTradeOverview.Integration.Runtime;
using Verse;

namespace SettlementTradeOverview.Integration.Snapshots
{
    internal static class TraderSnapshotAdapter
    {
        private const string LogPrefix = "[SettlementTradeOverview]";

        public static TraderSnapshot Build(DiscoveredTraderSource source, PlayerTradeContext context, Pawn negotiator)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            if (context == null)
                throw new ArgumentNullException(nameof(context));

            TradeDistance distance = TradeDistanceAdapter.Create(source, context);
            TradeRestock restock = TradeRestock.Unavailable;

            try
            {
                IEnumerable<Thing> goods = source.Trader.Goods;

                TraderGoodsCaptureResult capture = TraderGoodsCaptureAdapter.Capture(source.Trader, goods);

                var entries = new List<TradeEntrySnapshot>(capture.Tradeables.Count);
                var runtimeTargets = new List<KeyValuePair<TradeEntryIdentity, Thing>>(capture.Tradeables.Count);
                TradeCurrencySnapshot currency = null;
                int failedConversionCount = capture.FailureCount;

                foreach (Tradeable tradeable in capture.Tradeables)
                {
                    try
                    {
                        bool useAsPrimaryCurrency = TradeEntrySnapshotPolicy.IsPrimaryCurrency(
                            tradeable.IsCurrency,
                            tradeable.IsFavor,
                            currency != null);

                        if (useAsPrimaryCurrency)
                        {
                            currency = TradeEntrySnapshotAdapter.CreateCurrency(tradeable);
                        }
                        else
                        {
                            TradeEntrySnapshot entry = TradeEntrySnapshotAdapter.CreateEntry(
                                tradeable,
                                source.Trader,
                                negotiator);

                            entries.Add(entry);

                            if (entry.Kind == TradeEntryKind.Item && tradeable.AnyThing is Thing representative)
                            {
                                runtimeTargets.Add(
                                    new KeyValuePair<TradeEntryIdentity, Thing>(entry.Identity, representative));
                            }
                        }
                    }
                    catch (Exception exception)
                    {
                        failedConversionCount++;

                        Log.Warning(
                            $"{LogPrefix} Failed to convert a tradeable group for {source.TraderIdentity}. " +
                            "The remaining groups will still be processed.\n" + exception);
                    }
                }

                restock = TradeRestockAdapter.Create(source, context);

                bool hasStockData = entries.Count > 0 || currency != null;
                SnapshotAvailability availability = SnapshotAvailabilityResolver.ResolveTrader(
                    hasStockData,
                    failedConversionCount);

                var snapshot = new TraderSnapshot(
                    source.TraderIdentity,
                    source.SettlementIdentity,
                    GetTraderLabel(source),
                    GetSettlementLabel(source),
                    availability,
                    distance,
                    restock,
                    entries,
                    currency);

                foreach (KeyValuePair<TradeEntryIdentity, Thing> runtimeTarget in runtimeTargets)
                {
                    TradeEntryRuntimeTargetCache.TryRegister(runtimeTarget.Key, runtimeTarget.Value);
                }

                return snapshot;
            }
            catch (Exception exception)
            {
                Log.Warning(
                    $"{LogPrefix} Failed to capture public trader goods for {source.TraderIdentity}.\n" + exception);

                return CreateFailedSnapshot(source, distance, restock);
            }
        }

        public static TraderSnapshot CreateFailedSnapshot(
            DiscoveredTraderSource source,
            TradeDistance distance,
            TradeRestock restock)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            return new TraderSnapshot(
                source.TraderIdentity,
                source.SettlementIdentity,
                GetTraderLabel(source),
                GetSettlementLabel(source),
                SnapshotAvailability.Failed,
                distance,
                restock,
                Array.Empty<TradeEntrySnapshot>(),
                null);
        }

        private static string GetTraderLabel(DiscoveredTraderSource source)
        {
            string traderName = TryReadLabel(() => source.Trader.TraderName);

            if (!string.IsNullOrWhiteSpace(traderName))
                return traderName;

            string traderKindLabel = TryReadLabel(() => source.Settlement.TraderKind?.label);

            if (!string.IsNullOrWhiteSpace(traderKindLabel))
                return traderKindLabel;

            return source.TraderIdentity.Value;
        }

        private static string GetSettlementLabel(DiscoveredTraderSource source)
        {
            string settlementName = TryReadLabel(() => source.Settlement.Name);

            if (!string.IsNullOrWhiteSpace(settlementName))
                return settlementName;

            string settlementLabel = TryReadLabel(() => source.Settlement.LabelCap);

            if (!string.IsNullOrWhiteSpace(settlementLabel))
                return settlementLabel;

            return source.SettlementIdentity.ToString();
        }

        private static string TryReadLabel(Func<string> labelFactory)
        {
            try
            {
                return labelFactory();
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}