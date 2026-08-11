using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using SettlementTradeOverview.Domain.Identity;

namespace SettlementTradeOverview.Domain.Snapshots
{
    public sealed class TraderSnapshot
    {
        private readonly ReadOnlyCollection<TradeEntrySnapshot> _entries;

        public TraderSnapshot(
            TraderIdentity traderIdentity,
            SettlementIdentity settlementIdentity,
            string traderLabel,
            string settlementLabel,
            SnapshotAvailability availability,
            TradeDistance distance,
            TradeRestock restock,
            IReadOnlyList<TradeEntrySnapshot> entries,
            TradeCurrencySnapshot currency)
        {
            if (traderIdentity == null)
                throw new ArgumentNullException(nameof(traderIdentity));

            if (settlementIdentity == null)
                throw new ArgumentNullException(nameof(settlementIdentity));

            if (string.IsNullOrWhiteSpace(traderLabel))
                throw new ArgumentException("Trader label cannot be empty.", nameof(traderLabel));

            if (string.IsNullOrWhiteSpace(settlementLabel))
            {
                throw new ArgumentException("Settlement label cannot be empty.", nameof(settlementLabel));
            }

            if (entries == null)
                throw new ArgumentNullException(nameof(entries));

            var copiedEntries = new TradeEntrySnapshot[entries.Count];

            for (var index = 0; index < entries.Count; index++)
            {
                TradeEntrySnapshot entry = entries[index];

                copiedEntries[index] = entry ?? throw new ArgumentException(
                    "Entries cannot contain null values.",
                    nameof(entries));
            }

            ValidateAvailability(availability, copiedEntries.Length, currency != null);

            TraderIdentity = traderIdentity;
            SettlementIdentity = settlementIdentity;
            TraderLabel = traderLabel;
            SettlementLabel = settlementLabel;
            Availability = availability;
            Distance = distance;
            Restock = restock;
            _entries = Array.AsReadOnly(copiedEntries);
            Currency = currency;
        }

        public TraderIdentity TraderIdentity { get; }

        public SettlementIdentity SettlementIdentity { get; }

        public string TraderLabel { get; }

        public string SettlementLabel { get; }

        public SnapshotAvailability Availability { get; }

        public TradeDistance Distance { get; }

        public TradeRestock Restock { get; }

        public IReadOnlyList<TradeEntrySnapshot> Entries =>
            _entries;

        public TradeCurrencySnapshot Currency { get; }

        public int EntryCount =>
            _entries.Count;

        private static void ValidateAvailability(SnapshotAvailability availability, int entryCount, bool hasCurrency)
        {
            switch (availability)
            {
                case SnapshotAvailability.Available:
                    if (entryCount == 0 && !hasCurrency)
                    {
                        throw new ArgumentException(
                            "An available trader snapshot must contain stock data.",
                            nameof(availability));
                    }

                    break;

                case SnapshotAvailability.Empty:
                case SnapshotAvailability.Unavailable:
                case SnapshotAvailability.Failed:
                    if (entryCount > 0 || hasCurrency)
                    {
                        throw new ArgumentException(
                            "The selected availability state cannot contain stock data.",
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