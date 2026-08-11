using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace SettlementTradeOverview.Domain.Snapshots
{
    public sealed class GenepackCompositionSnapshot
    {
        private readonly ReadOnlyCollection<string> _geneDefNames;

        public GenepackCompositionSnapshot(IReadOnlyList<string> geneDefNames)
        {
            if (geneDefNames == null)
                throw new ArgumentNullException(nameof(geneDefNames));

            if (geneDefNames.Count == 0)
                throw new ArgumentException("Gene definition names cannot be empty.", nameof(geneDefNames));

            var copiedGeneDefNames = new List<string>(geneDefNames.Count);

            foreach (string geneDefName in geneDefNames)
            {
                if (string.IsNullOrWhiteSpace(geneDefName))
                {
                    throw new ArgumentException(
                        "Gene definition names cannot contain empty values.",
                        nameof(geneDefNames));
                }

                if (copiedGeneDefNames.Count > 0 && StringComparer.Ordinal.Compare(
                        copiedGeneDefNames[copiedGeneDefNames.Count - 1],
                        geneDefName) >= 0)
                {
                    throw new ArgumentException(
                        "Gene definition names must be distinct and sorted in ordinal order.",
                        nameof(geneDefNames));
                }

                copiedGeneDefNames.Add(geneDefName);
            }

            _geneDefNames = copiedGeneDefNames.AsReadOnly();
        }

        public IReadOnlyList<string> GeneDefNames =>
            _geneDefNames;

        public int Count =>
            _geneDefNames.Count;
    }
}