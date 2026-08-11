using System;
using System.Collections.Generic;
using RimWorld;
using SettlementTradeOverview.Domain.Snapshots;
using Verse;

namespace SettlementTradeOverview.Integration.Snapshots
{
    internal static class GenepackCompositionSnapshotAdapter
    {
        public static GenepackCompositionSnapshot Create(Thing representative)
        {
            if (!(representative is Genepack genepack))
                return null;

            try
            {
                GeneSet geneSet = genepack.GeneSet;
                List<GeneDef> genes = geneSet?.GenesListForReading;

                if (genes == null)
                    return null;

                var geneDefNames = new List<string>(genes.Count);

                foreach (GeneDef t in genes)
                    geneDefNames.Add(t?.defName);

                return GenepackCompositionSnapshotPolicy.CreateSnapshot(geneDefNames);
            }
            catch
            {
                return null;
            }
        }
    }

    internal static class GenepackCompositionSnapshotPolicy
    {
        public static GenepackCompositionSnapshot CreateSnapshot(IEnumerable<string> geneDefNames)
        {
            if (geneDefNames == null)
                return null;

            var canonicalGeneDefNames = new HashSet<string>(StringComparer.Ordinal);

            foreach (string geneDefName in geneDefNames)
            {
                if (!string.IsNullOrWhiteSpace(geneDefName))
                    canonicalGeneDefNames.Add(geneDefName);
            }

            if (canonicalGeneDefNames.Count == 0)
                return null;

            var orderedGeneDefNames = new List<string>(canonicalGeneDefNames);
            orderedGeneDefNames.Sort(StringComparer.Ordinal);

            return new GenepackCompositionSnapshot(orderedGeneDefNames);
        }
    }
}