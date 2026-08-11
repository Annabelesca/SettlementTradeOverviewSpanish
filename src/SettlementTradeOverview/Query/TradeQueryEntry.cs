using System;
using SettlementTradeOverview.Domain.Snapshots;

namespace SettlementTradeOverview.Query
{
    public sealed class TradeQueryEntry
    {
        public TradeQueryEntry(TraderSnapshot trader, TradeEntrySnapshot entry)
        {
            Trader = trader ?? throw new ArgumentNullException(nameof(trader));
            Entry = entry ?? throw new ArgumentNullException(nameof(entry));
        }

        public TraderSnapshot Trader { get; }

        public TradeEntrySnapshot Entry { get; }
    }
}