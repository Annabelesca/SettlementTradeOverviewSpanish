using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace SettlementTradeOverview.Domain.Snapshots
{
    public sealed class TradeInventorySnapshot
    {
        private readonly ReadOnlyCollection<TraderSnapshot> _traders;

        public TradeInventorySnapshot(
            SnapshotAvailability availability,
            int capturedAtTick,
            int? originTile,
            TradeNegotiatorSnapshot negotiator,
            IReadOnlyList<TraderSnapshot> traders)
        {
            if (capturedAtTick < 0)
                throw new ArgumentOutOfRangeException(nameof(capturedAtTick));

            if (originTile.HasValue && originTile.Value < 0)
                throw new ArgumentOutOfRangeException(nameof(originTile));

            if (traders == null)
                throw new ArgumentNullException(nameof(traders));

            var copiedTraders = new TraderSnapshot[traders.Count];
            var entryCount = 0;
            var hasStockData = false;

            for (var index = 0; index < traders.Count; index++)
            {
                TraderSnapshot trader = traders[index];

                copiedTraders[index] = trader ?? throw new ArgumentException(
                    "Traders cannot contain null values.",
                    nameof(traders));
                entryCount = checked(entryCount + trader.EntryCount);

                if (trader.EntryCount > 0 || trader.Currency != null)
                    hasStockData = true;
            }

            ValidateAvailability(availability, copiedTraders.Length, hasStockData);

            Availability = availability;
            CapturedAtTick = capturedAtTick;
            OriginTile = originTile;
            Negotiator = negotiator;
            _traders = Array.AsReadOnly(copiedTraders);
            EntryCount = entryCount;
        }

        public SnapshotAvailability Availability { get; }

        public int CapturedAtTick { get; }

        public int? OriginTile { get; }

        public TradeNegotiatorSnapshot Negotiator { get; }

        public IReadOnlyList<TraderSnapshot> Traders =>
            _traders;

        public int TraderCount =>
            _traders.Count;

        public int EntryCount { get; }

        private static void ValidateAvailability(SnapshotAvailability availability, int traderCount, bool hasStockData)
        {
            switch (availability)
            {
                case SnapshotAvailability.Available:
                    if (!hasStockData)
                    {
                        throw new ArgumentException(
                            "An available inventory snapshot must contain stock data.",
                            nameof(availability));
                    }

                    break;

                case SnapshotAvailability.Empty:
                    if (hasStockData)
                    {
                        throw new ArgumentException(
                            "An empty inventory snapshot cannot contain stock data.",
                            nameof(availability));
                    }

                    break;

                case SnapshotAvailability.Unavailable:
                case SnapshotAvailability.Failed:
                    if (traderCount > 0)
                    {
                        throw new ArgumentException(
                            "The selected availability state cannot contain trader snapshots.",
                            nameof(availability));
                    }

                    break;

                case SnapshotAvailability.Partial:
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(availability));
            }
        }
    }
}