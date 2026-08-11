using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace SettlementTradeOverview.Integration.Planner
{
    internal enum PlannerGenepackRelevanceBatchStatus
    {
        Success,
        Unavailable
    }

    internal enum PlannerGenepackRelevanceItemStatus
    {
        Success,
        InvalidInput,
        UnknownGeneDef,
        Failed
    }

    internal sealed class PlannerGenepackRelevancePlanMatch
    {
        public PlannerGenepackRelevancePlanMatch(string planId, string displayName)
        {
            if (string.IsNullOrWhiteSpace(planId))
                throw new ArgumentException("Plan ID cannot be empty.", nameof(planId));

            if (string.IsNullOrWhiteSpace(displayName))
                throw new ArgumentException("Plan display name cannot be empty.", nameof(displayName));

            PlanId = planId;
            DisplayName = displayName;
        }

        public string PlanId { get; }

        public string DisplayName { get; }
    }

    internal sealed class PlannerGenepackRelevanceItemResult
    {
        private readonly ReadOnlyCollection<PlannerGenepackRelevancePlanMatch> _matches;

        private PlannerGenepackRelevanceItemResult(
            PlannerGenepackRelevanceItemStatus status,
            IEnumerable<PlannerGenepackRelevancePlanMatch> matches)
        {
            ValidateStatus(status);

            List<PlannerGenepackRelevancePlanMatch> copiedMatches = CopyMatches(matches);

            if (status != PlannerGenepackRelevanceItemStatus.Success && copiedMatches.Count > 0)
            {
                throw new ArgumentException(
                    "Only a successful relevance item result can contain plan matches.",
                    nameof(matches));
            }

            Status = status;
            _matches = copiedMatches.AsReadOnly();
        }

        public PlannerGenepackRelevanceItemStatus Status { get; }

        public IReadOnlyList<PlannerGenepackRelevancePlanMatch> Matches =>
            _matches;

        public static PlannerGenepackRelevanceItemResult CreateSuccess(
            IEnumerable<PlannerGenepackRelevancePlanMatch> matches)
        {
            return new PlannerGenepackRelevanceItemResult(PlannerGenepackRelevanceItemStatus.Success, matches);
        }

        public static PlannerGenepackRelevanceItemResult CreateInvalidInput()
        {
            return new PlannerGenepackRelevanceItemResult(
                PlannerGenepackRelevanceItemStatus.InvalidInput,
                Array.Empty<PlannerGenepackRelevancePlanMatch>());
        }

        public static PlannerGenepackRelevanceItemResult CreateUnknownGeneDef()
        {
            return new PlannerGenepackRelevanceItemResult(
                PlannerGenepackRelevanceItemStatus.UnknownGeneDef,
                Array.Empty<PlannerGenepackRelevancePlanMatch>());
        }

        public static PlannerGenepackRelevanceItemResult CreateFailed()
        {
            return new PlannerGenepackRelevanceItemResult(
                PlannerGenepackRelevanceItemStatus.Failed,
                Array.Empty<PlannerGenepackRelevancePlanMatch>());
        }

        private static List<PlannerGenepackRelevancePlanMatch> CopyMatches(
            IEnumerable<PlannerGenepackRelevancePlanMatch> matches)
        {
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

            return copiedMatches;
        }

        private static void ValidateStatus(PlannerGenepackRelevanceItemStatus status)
        {
            if (status != PlannerGenepackRelevanceItemStatus.Success &&
                status != PlannerGenepackRelevanceItemStatus.InvalidInput &&
                status != PlannerGenepackRelevanceItemStatus.UnknownGeneDef &&
                status != PlannerGenepackRelevanceItemStatus.Failed)
            {
                throw new ArgumentOutOfRangeException(nameof(status), status, "Unsupported relevance item status.");
            }
        }
    }

    internal sealed class PlannerGenepackRelevanceBatchResult
    {
        private readonly ReadOnlyCollection<PlannerGenepackRelevanceItemResult> _results;

        private PlannerGenepackRelevanceBatchResult(
            PlannerGenepackRelevanceBatchStatus status,
            IEnumerable<PlannerGenepackRelevanceItemResult> results)
        {
            ValidateStatus(status);

            List<PlannerGenepackRelevanceItemResult> copiedResults = CopyResults(results);

            if (status != PlannerGenepackRelevanceBatchStatus.Success && copiedResults.Count > 0)
            {
                throw new ArgumentException(
                    "Only a successful relevance batch result can contain item results.",
                    nameof(results));
            }

            Status = status;
            _results = copiedResults.AsReadOnly();
        }

        public PlannerGenepackRelevanceBatchStatus Status { get; }

        public IReadOnlyList<PlannerGenepackRelevanceItemResult> Results =>
            _results;

        public static PlannerGenepackRelevanceBatchResult CreateSuccess(
            IEnumerable<PlannerGenepackRelevanceItemResult> results)
        {
            return new PlannerGenepackRelevanceBatchResult(PlannerGenepackRelevanceBatchStatus.Success, results);
        }

        public static PlannerGenepackRelevanceBatchResult CreateUnavailable()
        {
            return new PlannerGenepackRelevanceBatchResult(
                PlannerGenepackRelevanceBatchStatus.Unavailable,
                Array.Empty<PlannerGenepackRelevanceItemResult>());
        }

        private static List<PlannerGenepackRelevanceItemResult> CopyResults(
            IEnumerable<PlannerGenepackRelevanceItemResult> results)
        {
            if (results == null)
                throw new ArgumentNullException(nameof(results));

            var copiedResults = new List<PlannerGenepackRelevanceItemResult>();

            foreach (PlannerGenepackRelevanceItemResult result in results)
            {
                if (result == null)
                {
                    throw new ArgumentException(
                        "Relevance item result collection cannot contain null values.",
                        nameof(results));
                }

                copiedResults.Add(result);
            }

            return copiedResults;
        }

        private static void ValidateStatus(PlannerGenepackRelevanceBatchStatus status)
        {
            if (status != PlannerGenepackRelevanceBatchStatus.Success &&
                status != PlannerGenepackRelevanceBatchStatus.Unavailable)
            {
                throw new ArgumentOutOfRangeException(nameof(status), status, "Unsupported relevance batch status.");
            }
        }
    }
}