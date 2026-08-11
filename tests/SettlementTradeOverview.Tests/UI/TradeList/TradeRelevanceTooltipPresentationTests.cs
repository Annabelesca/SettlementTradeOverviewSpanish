using System;
using System.Collections.Generic;
using NUnit.Framework;
using SettlementTradeOverview.Domain.Identity;
using SettlementTradeOverview.Integration.Planner;
using SettlementTradeOverview.UI.TradeList;

namespace SettlementTradeOverview.Tests.UI.TradeList
{
    [TestFixture]
    public sealed class TradeRelevanceTooltipPresentationTests
    {
        [Test]
        public void Create_OneMatch_PreservesDisplayNameWithoutOmissions()
        {
            PlannerTradeEntryRelevance relevance = CreateRelevance(CreateMatch("Plan:1", "First plan"));

            TradeRelevanceTooltipPresentation presentation = TradeRelevanceTooltipPresentationBuilder.Create(relevance);

            Assert.That(presentation.DisplayNames, Is.EqualTo(new[] { "First plan" }));
            Assert.That(presentation.OmittedCount, Is.Zero);
        }

        [Test]
        public void Create_FiveMatches_PreservesAllDisplayNames()
        {
            PlannerTradeEntryRelevance relevance = CreateRelevance(
                CreateMatch("Plan:1", "First"),
                CreateMatch("Plan:2", "Second"),
                CreateMatch("Plan:3", "Third"),
                CreateMatch("Plan:4", "Fourth"),
                CreateMatch("Plan:5", "Fifth"));

            TradeRelevanceTooltipPresentation presentation = TradeRelevanceTooltipPresentationBuilder.Create(relevance);

            Assert.That(presentation.DisplayNames, Is.EqualTo(new[] { "First", "Second", "Third", "Fourth", "Fifth" }));
            Assert.That(presentation.OmittedCount, Is.Zero);
        }

        [Test]
        public void Create_SixMatches_LimitsDisplayNamesAndReportsOneOmission()
        {
            PlannerTradeEntryRelevance relevance = CreateRelevance(
                CreateMatch("Plan:1", "First"),
                CreateMatch("Plan:2", "Second"),
                CreateMatch("Plan:3", "Third"),
                CreateMatch("Plan:4", "Fourth"),
                CreateMatch("Plan:5", "Fifth"),
                CreateMatch("Plan:6", "Sixth"));

            TradeRelevanceTooltipPresentation presentation = TradeRelevanceTooltipPresentationBuilder.Create(relevance);

            Assert.That(presentation.DisplayNames, Is.EqualTo(new[] { "First", "Second", "Third", "Fourth", "Fifth" }));
            Assert.That(presentation.OmittedCount, Is.EqualTo(1));
        }

        [Test]
        public void Create_PreservesPlannerMatchOrder()
        {
            PlannerTradeEntryRelevance relevance = CreateRelevance(
                CreateMatch("Plan:C", "Charlie"),
                CreateMatch("Plan:A", "Alpha"),
                CreateMatch("Plan:B", "Bravo"));

            TradeRelevanceTooltipPresentation presentation = TradeRelevanceTooltipPresentationBuilder.Create(relevance);

            Assert.That(presentation.DisplayNames, Is.EqualTo(new[] { "Charlie", "Alpha", "Bravo" }));
        }

        [Test]
        public void Create_UsesDisplayNamesInsteadOfPlanIds()
        {
            PlannerTradeEntryRelevance relevance = CreateRelevance(CreateMatch("SecretStableId", "Visible plan"));

            TradeRelevanceTooltipPresentation presentation = TradeRelevanceTooltipPresentationBuilder.Create(relevance);

            Assert.That(presentation.DisplayNames, Does.Contain("Visible plan"));
            Assert.That(presentation.DisplayNames, Does.Not.Contain("SecretStableId"));
        }

        [Test]
        public void Create_DoesNotModifySourceRelevance()
        {
            PlannerTradeEntryRelevance relevance = CreateRelevance(
                CreateMatch("Plan:1", "First"),
                CreateMatch("Plan:2", "Second"));

            _ = TradeRelevanceTooltipPresentationBuilder.Create(relevance, maximumDisplayNames: 1);

            Assert.That(relevance.MatchCount, Is.EqualTo(2));
            Assert.That(relevance.Matches[0].DisplayName, Is.EqualTo("First"));
            Assert.That(relevance.Matches[1].DisplayName, Is.EqualTo("Second"));
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void Create_InvalidMaximumDisplayNames_Throws(int maximumDisplayNames)
        {
            PlannerTradeEntryRelevance relevance = CreateRelevance(CreateMatch("Plan:1", "First"));

            Assert.That(
                (Action)(() => TradeRelevanceTooltipPresentationBuilder.Create(relevance, maximumDisplayNames)),
                Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void Create_NullRelevance_Throws()
        {
            Assert.That(
                (Action)(() => TradeRelevanceTooltipPresentationBuilder.Create(null)),
                Throws.TypeOf<ArgumentNullException>());
        }

        [Test]
        public void Presentation_ExposedDisplayNames_AreReadOnly()
        {
            PlannerTradeEntryRelevance relevance = CreateRelevance(CreateMatch("Plan:1", "First"));

            TradeRelevanceTooltipPresentation presentation = TradeRelevanceTooltipPresentationBuilder.Create(relevance);

            var displayNames = (IList<string>)presentation.DisplayNames;

            Assert.That((Action)(() => displayNames.Add("Second")), Throws.TypeOf<NotSupportedException>());
        }

        private static PlannerTradeEntryRelevance CreateRelevance(params PlannerGenepackRelevancePlanMatch[] matches)
        {
            return new PlannerTradeEntryRelevance(new TradeEntryIdentity("Thing:Genepack:1"), matches);
        }

        private static PlannerGenepackRelevancePlanMatch CreateMatch(string planId, string displayName)
        {
            return new PlannerGenepackRelevancePlanMatch(planId, displayName);
        }
    }
}