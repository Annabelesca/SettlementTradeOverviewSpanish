using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using RimWorld;
using Verse;

namespace SettlementTradeOverview.Integration.Snapshots
{
    internal sealed class TraderGoodsCaptureResult
    {
        private readonly ReadOnlyCollection<Tradeable> _tradeables;

        public TraderGoodsCaptureResult(IReadOnlyList<Tradeable> tradeables, int failureCount)
        {
            if (tradeables == null)
                throw new ArgumentNullException(nameof(tradeables));

            if (failureCount < 0)
                throw new ArgumentOutOfRangeException(nameof(failureCount));

            var copiedTradeables = new Tradeable[tradeables.Count];

            for (var index = 0; index < tradeables.Count; index++)
            {
                copiedTradeables[index] = tradeables[index] ?? throw new ArgumentException(
                    "Tradeables cannot contain null values.",
                    nameof(tradeables));
            }

            _tradeables = Array.AsReadOnly(copiedTradeables);
            FailureCount = failureCount;
        }

        public IReadOnlyList<Tradeable> Tradeables =>
            _tradeables;

        public int FailureCount { get; }
    }

    internal static class TraderGoodsCaptureAdapter
    {
        private const string LogPrefix = "[SettlementTradeOverview]";

        public static TraderGoodsCaptureResult Capture(ITrader trader, IEnumerable<Thing> goods)
        {
            if (trader == null)
                throw new ArgumentNullException(nameof(trader));

            if (goods == null)
                throw new ArgumentNullException(nameof(goods));

            if (trader.TraderKind == null)
                throw new InvalidOperationException("Trader kind is unavailable during goods capture.");

            var groupedTradeables = new List<Tradeable>();
            var failureCount = 0;

            foreach (Thing thing in goods)
            {
                if (thing == null || thing.Destroyed)
                {
                    failureCount++;
                    continue;
                }

                Tradeable tradeable = null;
                var createdTradeable = false;

                try
                {
                    tradeable = FindMatchingTradeable(thing, groupedTradeables, trader);

                    if (tradeable == null)
                    {
                        tradeable = thing is Pawn ? new Tradeable_Pawn() : new Tradeable();

                        groupedTradeables.Add(tradeable);
                        createdTradeable = true;
                    }

                    tradeable.AddThing(thing, Transactor.Trader);
                }
                catch (Exception exception)
                {
                    if (createdTradeable)
                        groupedTradeables.Remove(tradeable);

                    failureCount++;

                    Log.Warning(
                        $"{LogPrefix} Failed to group trader good {DescribeThing(thing)}. " +
                        "The remaining goods will still be processed.\n" + exception);
                }
            }

            bool hideNotWillingToTrade = trader.TraderKind.hideThingsNotWillingToTrade;
            var filteredTradeables = new List<Tradeable>(groupedTradeables.Count);

            foreach (Tradeable tradeable in groupedTradeables)
            {
                try
                {
                    bool shouldInclude = tradeable.IsCurrency || trader.TraderKind.WillTrade(tradeable.ThingDef) ||
                                         !hideNotWillingToTrade;

                    if (shouldInclude)
                        filteredTradeables.Add(tradeable);
                }
                catch (Exception exception)
                {
                    failureCount++;

                    Log.Warning(
                        $"{LogPrefix} Failed to validate a grouped tradeable. " +
                        "The remaining groups will still be processed.\n" + exception);
                }
            }

            return new TraderGoodsCaptureResult(filteredTradeables, failureCount);
        }

        private static Tradeable FindMatchingTradeable(Thing thing, IReadOnlyList<Tradeable> tradeables, ITrader trader)
        {
            foreach (Tradeable tradeable in tradeables)
            {
                if (!tradeable.HasAnyThing)
                    continue;

                TransferAsOneMode mode = !trader.TraderKind.WillTrade(tradeable.ThingDef)
                    ? TransferAsOneMode.InactiveTradeable
                    : TransferAsOneMode.Normal;

                if (TransferableUtility.TransferAsOne(thing, tradeable.AnyThing, mode))
                    return tradeable;
            }

            return null;
        }

        private static string DescribeThing(Thing thing)
        {
            if (thing == null)
                return "null";

            string thingId = thing.ThingID;
            string typeName = thing.GetType().FullName ?? thing.GetType().Name;

            return string.IsNullOrWhiteSpace(thingId) ? $"Type={typeName}" : $"ID={thingId}, Type={typeName}";
        }
    }
}