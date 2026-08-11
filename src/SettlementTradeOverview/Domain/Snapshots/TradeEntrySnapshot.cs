using System;
using SettlementTradeOverview.Domain.Categories;
using SettlementTradeOverview.Domain.Identity;

namespace SettlementTradeOverview.Domain.Snapshots
{
    public sealed class TradeEntrySnapshot
    {
        public TradeEntrySnapshot(
            TradeEntryIdentity identity,
            TradeEntryKind kind,
            TradeCategoryMembership categoryMembership,
            string definitionName,
            string label,
            int count,
            TradePrice price,
            PawnTradeDetailsSnapshot pawnDetails = null,
            GenepackCompositionSnapshot genepackComposition = null)
        {
            if (string.IsNullOrWhiteSpace(definitionName))
            {
                throw new ArgumentException("Definition name cannot be empty.", nameof(definitionName));
            }

            if (string.IsNullOrWhiteSpace(label))
                throw new ArgumentException("Label cannot be empty.", nameof(label));

            if (count <= 0)
                throw new ArgumentOutOfRangeException(nameof(count));

            if (kind != TradeEntryKind.Pawn && pawnDetails != null)
            {
                throw new ArgumentException(
                    "Pawn trade details can be assigned only to pawn entries.",
                    nameof(pawnDetails));
            }

            if (kind == TradeEntryKind.Pawn && genepackComposition != null)
            {
                throw new ArgumentException(
                    "Genepack composition cannot be assigned to pawn entries.",
                    nameof(genepackComposition));
            }

            Identity = identity ?? throw new ArgumentNullException(nameof(identity));
            Kind = kind;
            Category = TradeCategoryClassifier.Classify(kind, categoryMembership);
            DefinitionName = definitionName;
            Label = label;
            Count = count;
            Price = price;
            PawnDetails = pawnDetails;
            GenepackComposition = genepackComposition;
        }

        public TradeEntryIdentity Identity { get; }

        public TradeEntryKind Kind { get; }

        public TradeCategory Category { get; }

        public string DefinitionName { get; }

        public string Label { get; }

        public int Count { get; }

        public TradePrice Price { get; }

        public PawnTradeDetailsSnapshot PawnDetails { get; }

        public GenepackCompositionSnapshot GenepackComposition { get; }
    }
}