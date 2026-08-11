using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using SettlementTradeOverview.Domain.Identity;

namespace SettlementTradeOverview.Integration.Planner
{
    internal sealed class PlannerTradeEntryRelevance
    {
        private readonly ReadOnlyCollection<PlannerGenepackRelevancePlanMatch> _matches;

        public PlannerTradeEntryRelevance(
            TradeEntryIdentity entryIdentity,
            IEnumerable<PlannerGenepackRelevancePlanMatch> matches)
        {
            EntryIdentity = entryIdentity ?? throw new ArgumentNullException(nameof(entryIdentity));

            if (matches == null)
                throw new ArgumentNullException(nameof(matches));

            var copiedMatches = new List<PlannerGenepackRelevancePlanMatch>();

            foreach (PlannerGenepackRelevancePlanMatch match in matches)
            {
                if (match == null)
                {
                    throw new ArgumentException("Plan match collection cannot contain null values.", nameof(matches));
                }

                copiedMatches.Add(match);
            }

            if (copiedMatches.Count == 0)
                throw new ArgumentException("Entry relevance must contain at least one plan match.", nameof(matches));

            _matches = copiedMatches.AsReadOnly();
        }

        public TradeEntryIdentity EntryIdentity { get; }

        public IReadOnlyList<PlannerGenepackRelevancePlanMatch> Matches =>
            _matches;

        public int MatchCount =>
            _matches.Count;
    }

    internal sealed class PlannerTradeRelevanceProjection
    {
        private readonly Dictionary<TradeEntryIdentity, PlannerTradeEntryRelevance> _entriesByIdentity;

        public PlannerTradeRelevanceProjection(IEnumerable<PlannerTradeEntryRelevance> entries)
        {
            if (entries == null)
                throw new ArgumentNullException(nameof(entries));

            _entriesByIdentity = new Dictionary<TradeEntryIdentity, PlannerTradeEntryRelevance>();

            foreach (PlannerTradeEntryRelevance entry in entries)
            {
                if (entry == null)
                {
                    throw new ArgumentException("Relevance projection cannot contain null entries.", nameof(entries));
                }

                if (_entriesByIdentity.ContainsKey(entry.EntryIdentity))
                {
                    throw new ArgumentException(
                        "Relevance projection cannot contain duplicate trade entry identities.",
                        nameof(entries));
                }

                _entriesByIdentity.Add(entry.EntryIdentity, entry);
            }
        }

        public static PlannerTradeRelevanceProjection Empty { get; } =
            new PlannerTradeRelevanceProjection(Array.Empty<PlannerTradeEntryRelevance>());

        public int Count =>
            _entriesByIdentity.Count;

        public bool TryGet(TradeEntryIdentity entryIdentity, out PlannerTradeEntryRelevance relevance)
        {
            if (entryIdentity == null)
                throw new ArgumentNullException(nameof(entryIdentity));

            return _entriesByIdentity.TryGetValue(entryIdentity, out relevance);
        }

        public int? GetMatchCount(TradeEntryIdentity entryIdentity)
        {
            if (entryIdentity == null)
                throw new ArgumentNullException(nameof(entryIdentity));

            return _entriesByIdentity.TryGetValue(entryIdentity, out PlannerTradeEntryRelevance relevance)
                ? relevance.MatchCount
                : (int?)null;
        }
    }
}