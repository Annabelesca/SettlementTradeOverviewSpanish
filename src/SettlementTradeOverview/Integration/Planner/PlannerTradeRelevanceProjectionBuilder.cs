using System;
using System.Collections.Generic;
using SettlementTradeOverview.Domain.Identity;
using SettlementTradeOverview.Domain.Snapshots;

namespace SettlementTradeOverview.Integration.Planner
{
    internal static class PlannerTradeRelevanceProjectionBuilder
    {
        public static PlannerTradeRelevanceProjection Build(TradeInventorySnapshot snapshot)
        {
            return Build(snapshot, PlannerGenepackRelevanceAdapter.Query);
        }

        public static PlannerTradeRelevanceProjection Build(TraderSnapshot trader)
        {
            return Build(trader, PlannerGenepackRelevanceAdapter.Query);
        }

        internal static PlannerTradeRelevanceProjection Build(
            TradeInventorySnapshot snapshot,
            Func<IReadOnlyList<GenepackCompositionSnapshot>, PlannerGenepackRelevanceBatchResult> query)
        {
            if (snapshot == null)
                throw new ArgumentNullException(nameof(snapshot));

            if (query == null)
                throw new ArgumentNullException(nameof(query));

            var candidates = new List<ProjectionCandidate>();

            foreach (TraderSnapshot trader in snapshot.Traders)
                AddCandidates(trader, candidates);

            return BuildCore(candidates, query);
        }

        internal static PlannerTradeRelevanceProjection Build(
            TraderSnapshot trader,
            Func<IReadOnlyList<GenepackCompositionSnapshot>, PlannerGenepackRelevanceBatchResult> query)
        {
            if (trader == null)
                throw new ArgumentNullException(nameof(trader));

            if (query == null)
                throw new ArgumentNullException(nameof(query));

            var candidates = new List<ProjectionCandidate>();
            AddCandidates(trader, candidates);

            return BuildCore(candidates, query);
        }

        private static PlannerTradeRelevanceProjection BuildCore(
            IReadOnlyList<ProjectionCandidate> candidates,
            Func<IReadOnlyList<GenepackCompositionSnapshot>, PlannerGenepackRelevanceBatchResult> query)
        {
            if (candidates.Count == 0)
                return PlannerTradeRelevanceProjection.Empty;

            var compositions = new GenepackCompositionSnapshot[candidates.Count];

            for (var index = 0; index < candidates.Count; index++)
                compositions[index] = candidates[index].Composition;

            PlannerGenepackRelevanceBatchResult batchResult;

            try
            {
                batchResult = query(Array.AsReadOnly(compositions));
            }
            catch
            {
                return PlannerTradeRelevanceProjection.Empty;
            }

            if (batchResult == null || batchResult.Status != PlannerGenepackRelevanceBatchStatus.Success ||
                batchResult.Results.Count != candidates.Count)
            {
                return PlannerTradeRelevanceProjection.Empty;
            }

            var relevantEntries = new List<PlannerTradeEntryRelevance>();

            for (var index = 0; index < candidates.Count; index++)
            {
                PlannerGenepackRelevanceItemResult itemResult = batchResult.Results[index];

                if (itemResult.Status != PlannerGenepackRelevanceItemStatus.Success || itemResult.Matches.Count == 0)
                    continue;

                relevantEntries.Add(
                    new PlannerTradeEntryRelevance(candidates[index].EntryIdentity, itemResult.Matches));
            }

            if (relevantEntries.Count == 0)
                return PlannerTradeRelevanceProjection.Empty;

            try
            {
                return new PlannerTradeRelevanceProjection(relevantEntries);
            }
            catch
            {
                return PlannerTradeRelevanceProjection.Empty;
            }
        }

        private static void AddCandidates(TraderSnapshot trader, ICollection<ProjectionCandidate> candidates)
        {
            foreach (TradeEntrySnapshot entry in trader.Entries)
            {
                if (entry.GenepackComposition == null)
                    continue;

                candidates.Add(new ProjectionCandidate(entry.Identity, entry.GenepackComposition));
            }
        }

        private readonly struct ProjectionCandidate
        {
            public ProjectionCandidate(TradeEntryIdentity entryIdentity, GenepackCompositionSnapshot composition)
            {
                EntryIdentity = entryIdentity ?? throw new ArgumentNullException(nameof(entryIdentity));
                Composition = composition ?? throw new ArgumentNullException(nameof(composition));
            }

            public TradeEntryIdentity EntryIdentity { get; }

            public GenepackCompositionSnapshot Composition { get; }
        }
    }
}