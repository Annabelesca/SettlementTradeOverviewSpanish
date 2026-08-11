using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using SettlementTradeOverview.Integration.Planner;

namespace SettlementTradeOverview.Tests.Integration.Planner
{
    [TestFixture]
    public sealed class PlannerGenepackRelevanceResultTests
    {
        [Test]
        public void PlanMatch_ValidValues_PreservesIdentityAndDisplayName()
        {
            var match = new PlannerGenepackRelevancePlanMatch("plan-1", "Alpha");

            Assert.That(match.PlanId, Is.EqualTo("plan-1"));
            Assert.That(match.DisplayName, Is.EqualTo("Alpha"));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void PlanMatch_EmptyPlanId_Throws(string planId)
        {
            Assert.That(
                (Action)(() => { _ = new PlannerGenepackRelevancePlanMatch(planId, "Alpha"); }),
                Throws.TypeOf<ArgumentException>());
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void PlanMatch_EmptyDisplayName_Throws(string displayName)
        {
            Assert.That(
                (Action)(() => { _ = new PlannerGenepackRelevancePlanMatch("plan-1", displayName); }),
                Throws.TypeOf<ArgumentException>());
        }

        [Test]
        public void ItemSuccess_MutableInput_CopiesMatches()
        {
            var source = new List<PlannerGenepackRelevancePlanMatch>
            {
                new PlannerGenepackRelevancePlanMatch("plan-1", "Alpha")
            };

            var result = PlannerGenepackRelevanceItemResult.CreateSuccess(source);

            source.Clear();

            Assert.That(result.Status, Is.EqualTo(PlannerGenepackRelevanceItemStatus.Success));
            Assert.That(result.Matches.Count, Is.EqualTo(1));
            Assert.That(result.Matches[0].PlanId, Is.EqualTo("plan-1"));
        }

        [Test]
        public void ItemSuccess_EmptyMatches_IsValid()
        {
            var result =
                PlannerGenepackRelevanceItemResult.CreateSuccess(Array.Empty<PlannerGenepackRelevancePlanMatch>());

            Assert.That(result.Status, Is.EqualTo(PlannerGenepackRelevanceItemStatus.Success));
            Assert.That(result.Matches, Is.Empty);
        }

        [Test]
        public void ItemSuccess_NullMatch_Throws()
        {
            Assert.That(
                (Action)(() => PlannerGenepackRelevanceItemResult.CreateSuccess(
                    new PlannerGenepackRelevancePlanMatch[] { null })),
                Throws.TypeOf<ArgumentException>());
        }

        [TestCase((int)PlannerGenepackRelevanceItemStatus.InvalidInput)]
        [TestCase((int)PlannerGenepackRelevanceItemStatus.UnknownGeneDef)]
        [TestCase((int)PlannerGenepackRelevanceItemStatus.Failed)]
        public void ItemFailure_FactoryCreatesEmptyResult(int statusValue)
        {
            var status = (PlannerGenepackRelevanceItemStatus)statusValue;
            PlannerGenepackRelevanceItemResult result;

            switch (status)
            {
                case PlannerGenepackRelevanceItemStatus.InvalidInput:
                    result = PlannerGenepackRelevanceItemResult.CreateInvalidInput();
                    break;

                case PlannerGenepackRelevanceItemStatus.UnknownGeneDef:
                    result = PlannerGenepackRelevanceItemResult.CreateUnknownGeneDef();
                    break;

                default:
                    result = PlannerGenepackRelevanceItemResult.CreateFailed();
                    break;
            }

            Assert.That(result.Status, Is.EqualTo(status));
            Assert.That(result.Matches, Is.Empty);
        }

        [Test]
        public void ItemMatches_ExposedCollection_IsReadOnly()
        {
            var result = PlannerGenepackRelevanceItemResult.CreateSuccess(
                new[] { new PlannerGenepackRelevancePlanMatch("plan-1", "Alpha") });

            var matches = (IList<PlannerGenepackRelevancePlanMatch>)result.Matches;

            Assert.That(
                (Action)(() => matches.Add(new PlannerGenepackRelevancePlanMatch("plan-2", "Beta"))),
                Throws.TypeOf<NotSupportedException>());
        }

        [Test]
        public void BatchSuccess_MutableInput_CopiesResultsAndPreservesOrder()
        {
            var first = PlannerGenepackRelevanceItemResult.CreateInvalidInput();

            var second = PlannerGenepackRelevanceItemResult.CreateUnknownGeneDef();

            var source = new List<PlannerGenepackRelevanceItemResult> { first, second };

            var batch = PlannerGenepackRelevanceBatchResult.CreateSuccess(source);

            source.Clear();

            Assert.That(batch.Status, Is.EqualTo(PlannerGenepackRelevanceBatchStatus.Success));
            Assert.That(batch.Results, Is.EqualTo(new[] { first, second }));
        }

        [Test]
        public void BatchSuccess_EmptyResults_IsValid()
        {
            var batch = PlannerGenepackRelevanceBatchResult.CreateSuccess(
                Array.Empty<PlannerGenepackRelevanceItemResult>());

            Assert.That(batch.Status, Is.EqualTo(PlannerGenepackRelevanceBatchStatus.Success));
            Assert.That(batch.Results, Is.Empty);
        }

        [Test]
        public void BatchSuccess_NullResult_Throws()
        {
            Assert.That(
                (Action)(() => PlannerGenepackRelevanceBatchResult.CreateSuccess(
                    new PlannerGenepackRelevanceItemResult[] { null })),
                Throws.TypeOf<ArgumentException>());
        }

        [Test]
        public void BatchUnavailable_ContainsNoResults()
        {
            var batch = PlannerGenepackRelevanceBatchResult.CreateUnavailable();

            Assert.That(batch.Status, Is.EqualTo(PlannerGenepackRelevanceBatchStatus.Unavailable));
            Assert.That(batch.Results, Is.Empty);
        }

        [Test]
        public void BatchResults_ExposedCollection_IsReadOnly()
        {
            var batch = PlannerGenepackRelevanceBatchResult.CreateSuccess(
                Array.Empty<PlannerGenepackRelevanceItemResult>());

            var results = (IList<PlannerGenepackRelevanceItemResult>)batch.Results;

            Assert.That(
                (Action)(() => results.Add(PlannerGenepackRelevanceItemResult.CreateFailed())),
                Throws.TypeOf<NotSupportedException>());
        }

        [Test]
        public void ItemResult_UnknownStatus_Throws()
        {
            ConstructorInfo constructor = typeof(PlannerGenepackRelevanceItemResult).GetConstructor(
                BindingFlags.Instance | BindingFlags.NonPublic,
                binder: null,
                types: new[]
                {
                    typeof(PlannerGenepackRelevanceItemStatus),
                    typeof(IEnumerable<PlannerGenepackRelevancePlanMatch>)
                },
                modifiers: null);

            Assert.That(constructor, Is.Not.Null);

            TargetInvocationException exception = Assert.Throws<TargetInvocationException>(
                (Action)(() => constructor.Invoke(
                    new object[]
                    {
                        (PlannerGenepackRelevanceItemStatus)999,
                        Array.Empty<PlannerGenepackRelevancePlanMatch>()
                    })));

            Assert.That(exception?.InnerException, Is.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void BatchResult_UnknownStatus_Throws()
        {
            ConstructorInfo constructor = typeof(PlannerGenepackRelevanceBatchResult).GetConstructor(
                BindingFlags.Instance | BindingFlags.NonPublic,
                binder: null,
                types: new[]
                {
                    typeof(PlannerGenepackRelevanceBatchStatus),
                    typeof(IEnumerable<PlannerGenepackRelevanceItemResult>)
                },
                modifiers: null);

            Assert.That(constructor, Is.Not.Null);

            TargetInvocationException exception = Assert.Throws<TargetInvocationException>(
                (Action)(() => constructor.Invoke(
                    new object[]
                    {
                        (PlannerGenepackRelevanceBatchStatus)999,
                        Array.Empty<PlannerGenepackRelevanceItemResult>()
                    })));

            Assert.That(exception?.InnerException, Is.TypeOf<ArgumentOutOfRangeException>());
        }
    }
}