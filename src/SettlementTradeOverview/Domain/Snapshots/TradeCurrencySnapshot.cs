using System;
using SettlementTradeOverview.Domain.Identity;

namespace SettlementTradeOverview.Domain.Snapshots
{
    public sealed class TradeCurrencySnapshot
    {
        public TradeCurrencySnapshot(TradeEntryIdentity identity, string definitionName, string label, int count)
        {
            if (string.IsNullOrWhiteSpace(definitionName))
            {
                throw new ArgumentException("Definition name cannot be empty.", nameof(definitionName));
            }

            if (string.IsNullOrWhiteSpace(label))
                throw new ArgumentException("Label cannot be empty.", nameof(label));

            if (count <= 0)
                throw new ArgumentOutOfRangeException(nameof(count));

            Identity = identity ?? throw new ArgumentNullException(nameof(identity));
            DefinitionName = definitionName;
            Label = label;
            Count = count;
        }

        public TradeEntryIdentity Identity { get; }

        public string DefinitionName { get; }

        public string Label { get; }

        public int Count { get; }
    }
}