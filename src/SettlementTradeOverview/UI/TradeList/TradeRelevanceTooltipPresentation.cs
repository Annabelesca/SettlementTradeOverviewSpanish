using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using SettlementTradeOverview.Integration.Planner;
using Verse;

namespace SettlementTradeOverview.UI.TradeList
{
    internal sealed class TradeRelevanceTooltipPresentation
    {
        private readonly ReadOnlyCollection<string> _displayNames;

        public TradeRelevanceTooltipPresentation(IEnumerable<string> displayNames, int omittedCount)
        {
            if (displayNames == null)
                throw new ArgumentNullException(nameof(displayNames));

            if (omittedCount < 0)
                throw new ArgumentOutOfRangeException(nameof(omittedCount));

            var copiedDisplayNames = new List<string>();

            foreach (string displayName in displayNames)
            {
                if (string.IsNullOrWhiteSpace(displayName))
                {
                    throw new ArgumentException(
                        "Relevance tooltip display names cannot contain empty values.",
                        nameof(displayNames));
                }

                copiedDisplayNames.Add(displayName);
            }

            if (copiedDisplayNames.Count == 0)
            {
                throw new ArgumentException(
                    "Relevance tooltip must contain at least one display name.",
                    nameof(displayNames));
            }

            _displayNames = copiedDisplayNames.AsReadOnly();
            OmittedCount = omittedCount;
        }

        public IReadOnlyList<string> DisplayNames =>
            _displayNames;

        public int OmittedCount { get; }

        public string ToLocalizedText()
        {
            var lines = new List<string>(_displayNames.Count + 2)
            {
                "STO.TradeList.Relevance.Tooltip.Header".Translate().ToString()
            };

            foreach (string displayName in _displayNames)
                lines.Add("  - " + displayName);

            if (OmittedCount > 0)
            {
                lines.Add("STO.TradeList.Relevance.Tooltip.More".Translate(OmittedCount).ToString());
            }

            return string.Join("\n", lines);
        }
    }

    internal static class TradeRelevanceTooltipPresentationBuilder
    {
        public const int DefaultMaximumDisplayNames = 5;

        public static TradeRelevanceTooltipPresentation Create(
            PlannerTradeEntryRelevance relevance,
            int maximumDisplayNames = DefaultMaximumDisplayNames)
        {
            if (relevance == null)
                throw new ArgumentNullException(nameof(relevance));

            if (maximumDisplayNames <= 0)
                throw new ArgumentOutOfRangeException(nameof(maximumDisplayNames));

            int visibleCount = Math.Min(relevance.MatchCount, maximumDisplayNames);
            var displayNames = new List<string>(visibleCount);

            for (var index = 0; index < visibleCount; index++)
                displayNames.Add(relevance.Matches[index].DisplayName);

            return new TradeRelevanceTooltipPresentation(displayNames, relevance.MatchCount - visibleCount);
        }

        public static string BuildLocalizedTooltip(PlannerTradeEntryRelevance relevance)
        {
            return Create(relevance).ToLocalizedText();
        }
    }
}