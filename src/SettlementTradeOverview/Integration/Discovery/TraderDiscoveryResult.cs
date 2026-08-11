using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace SettlementTradeOverview.Integration.Discovery
{
    internal sealed class TraderDiscoveryResult
    {
        private readonly ReadOnlyCollection<DiscoveredTraderSource> _sources;

        public TraderDiscoveryResult(
            bool isContextAvailable,
            IReadOnlyList<DiscoveredTraderSource> sources,
            int candidateCount,
            int rejectedCandidateCount,
            int failedCandidateCount)
        {
            if (sources == null)
                throw new ArgumentNullException(nameof(sources));

            if (candidateCount < 0)
                throw new ArgumentOutOfRangeException(nameof(candidateCount));

            if (rejectedCandidateCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(rejectedCandidateCount));
            }

            if (failedCandidateCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(failedCandidateCount));
            }

            var copiedSources = new DiscoveredTraderSource[sources.Count];

            for (var index = 0; index < sources.Count; index++)
            {
                DiscoveredTraderSource source = sources[index];

                copiedSources[index] = source ?? throw new ArgumentException(
                    "Sources cannot contain null values.",
                    nameof(sources));
            }

            if (!isContextAvailable)
            {
                if (copiedSources.Length > 0 || candidateCount != 0 || rejectedCandidateCount != 0 ||
                    failedCandidateCount != 0)
                {
                    throw new ArgumentException(
                        "A context-unavailable result cannot contain discovery data.",
                        nameof(isContextAvailable));
                }
            }
            else
            {
                int accountedCandidateCount =
                    checked(copiedSources.Length + rejectedCandidateCount + failedCandidateCount);

                if (accountedCandidateCount != candidateCount)
                {
                    throw new ArgumentException(
                        "Accepted, rejected and failed candidate counts must equal the candidate count.",
                        nameof(candidateCount));
                }
            }

            IsContextAvailable = isContextAvailable;
            _sources = Array.AsReadOnly(copiedSources);
            FailedCandidateCount = failedCandidateCount;
        }

        public static TraderDiscoveryResult ContextUnavailable { get; } = new TraderDiscoveryResult(
            false,
            Array.Empty<DiscoveredTraderSource>(),
            0,
            0,
            0);

        public bool IsContextAvailable { get; }

        public IReadOnlyList<DiscoveredTraderSource> Sources =>
            _sources;

        public int FailedCandidateCount { get; }

        public bool HasFailures =>
            FailedCandidateCount > 0;
    }
}