using System;
using System.Collections.Generic;
using NUnit.Framework;
using SettlementTradeOverview.Domain.Snapshots;
using SettlementTradeOverview.Integration.Planner;

namespace SettlementTradeOverview.Tests.Integration.Planner
{
    [TestFixture]
    public sealed class XenogermPlannerApiV1BindingTests
    {
        [SetUp]
        public void SetUp()
        {
            ValidFacade.QueryHandler = null;
        }

        [Test]
        public void TryCreate_MissingFacade_ReturnsFalse()
        {
            bool created = XenogermPlannerApiV1Binding.TryCreate(_ => null, out XenogermPlannerApiV1Binding binding);

            Assert.That(created, Is.False);
            Assert.That(binding, Is.Null);
        }

        [Test]
        public void TryCreate_ApiVersionOne_CreatesBinding()
        {
            bool created = XenogermPlannerApiV1Binding.TryCreate(
                CreateResolver(typeof(ValidFacade)),
                out XenogermPlannerApiV1Binding binding);

            Assert.That(created, Is.True);
            Assert.That(binding, Is.Not.Null);
        }

        [TestCase(typeof(VersionZeroFacade))]
        [TestCase(typeof(VersionTwoFacade))]
        public void TryCreate_UnsupportedVersion_ReturnsFalse(Type facadeType)
        {
            bool created = XenogermPlannerApiV1Binding.TryCreate(
                CreateResolver(facadeType),
                out XenogermPlannerApiV1Binding binding);

            Assert.That(created, Is.False);
            Assert.That(binding, Is.Null);
        }

        [Test]
        public void TryCreate_UnsupportedVersion_DoesNotResolveDtoTypes()
        {
            var requestedTypeNames = new List<string>();

            Type Resolver(string typeName)
            {
                requestedTypeNames.Add(typeName);

                if (typeName == XenogermPlannerApiV1Binding.FacadeTypeName) return typeof(VersionTwoFacade);

                throw new InvalidOperationException("DTO types must not be resolved for an unsupported API version.");
            }

            bool created = XenogermPlannerApiV1Binding.TryCreate(Resolver, out XenogermPlannerApiV1Binding binding);

            Assert.That(created, Is.False);
            Assert.That(binding, Is.Null);
            Assert.That(requestedTypeNames, Is.EqualTo(new[] { XenogermPlannerApiV1Binding.FacadeTypeName }));
        }

        [Test]
        public void TryCreate_MissingVersionProperty_ReturnsFalse()
        {
            bool created = XenogermPlannerApiV1Binding.TryCreate(
                CreateResolver(typeof(MissingVersionFacade)),
                out XenogermPlannerApiV1Binding binding);

            Assert.That(created, Is.False);
            Assert.That(binding, Is.Null);
        }

        [Test]
        public void TryCreate_RequestWithoutExpectedConstructor_ReturnsFalse()
        {
            bool created = XenogermPlannerApiV1Binding.TryCreate(
                CreateResolver(
                    typeof(RequestWithoutExpectedConstructorFacade),
                    requestType: typeof(RequestWithoutExpectedConstructor)),
                out XenogermPlannerApiV1Binding binding);

            Assert.That(created, Is.False);
            Assert.That(binding, Is.Null);
        }

        [Test]
        public void TryCreate_WrongQuerySignature_ReturnsFalse()
        {
            bool created = XenogermPlannerApiV1Binding.TryCreate(
                CreateResolver(typeof(WrongQuerySignatureFacade)),
                out XenogermPlannerApiV1Binding binding);

            Assert.That(created, Is.False);
            Assert.That(binding, Is.Null);
        }

        [Test]
        public void TryCreate_WrongQueryReturnType_ReturnsFalse()
        {
            bool created = XenogermPlannerApiV1Binding.TryCreate(
                CreateResolver(typeof(WrongQueryReturnTypeFacade)),
                out XenogermPlannerApiV1Binding binding);

            Assert.That(created, Is.False);
            Assert.That(binding, Is.Null);
        }

        [Test]
        public void TryCreate_BatchWithoutResultsProperty_ReturnsFalse()
        {
            bool created = XenogermPlannerApiV1Binding.TryCreate(
                CreateResolver(typeof(BatchWithoutResultsFacade), batchResultType: typeof(BatchWithoutResults)),
                out XenogermPlannerApiV1Binding binding);

            Assert.That(created, Is.False);
            Assert.That(binding, Is.Null);
        }

        [Test]
        public void Query_Compositions_CreateRequestsInInputOrder()
        {
            XenogermPlannerApiV1Binding binding = CreateValidBinding();
            IReadOnlyList<ValidRequest> capturedRequests = null;

            ValidFacade.QueryHandler = requests =>
            {
                capturedRequests = requests;

                return new ValidBatchResult(
                    ValidBatchStatus.Success,
                    ValidUnavailableReason.None,
                    new[]
                    {
                        ValidItemResult.Success(),
                        ValidItemResult.Success()
                    });
            };

            var first = new GenepackCompositionSnapshot(new[] { "GeneA", "GeneB" });
            var second = new GenepackCompositionSnapshot(new[] { "GeneC" });

            PlannerGenepackRelevanceBatchResult result = binding.Query(new[] { first, second });

            Assert.That(result.Status, Is.EqualTo(PlannerGenepackRelevanceBatchStatus.Success));
            Assert.That(capturedRequests, Is.Not.Null);
            Assert.That(capturedRequests.Count, Is.EqualTo(2));
            Assert.That(capturedRequests[0].GeneDefNames, Is.EqualTo(new[] { "GeneA", "GeneB" }));
            Assert.That(capturedRequests[1].GeneDefNames, Is.EqualTo(new[] { "GeneC" }));
            Assert.That(capturedRequests[0], Is.Not.SameAs(first));
            Assert.That(capturedRequests[1], Is.Not.SameAs(second));
        }

        [Test]
        public void Query_SuccessfulEmptyBatch_ReturnsSuccessfulEmptyResult()
        {
            XenogermPlannerApiV1Binding binding = CreateValidBinding();

            PlannerGenepackRelevanceBatchResult result = binding.Query(Array.Empty<GenepackCompositionSnapshot>());

            Assert.That(result.Status, Is.EqualTo(PlannerGenepackRelevanceBatchStatus.Success));
            Assert.That(result.Results, Is.Empty);
        }

        [Test]
        public void Query_SuccessfulItemWithoutMatches_PreservesSuccess()
        {
            XenogermPlannerApiV1Binding binding = CreateValidBinding();

            ValidFacade.QueryHandler = _ => new ValidBatchResult(
                ValidBatchStatus.Success,
                ValidUnavailableReason.None,
                new[] { ValidItemResult.Success() });

            PlannerGenepackRelevanceBatchResult result = binding.Query(new[] { CreateComposition("GeneA") });

            Assert.That(result.Status, Is.EqualTo(PlannerGenepackRelevanceBatchStatus.Success));
            Assert.That(result.Results.Count, Is.EqualTo(1));
            Assert.That(result.Results[0].Status, Is.EqualTo(PlannerGenepackRelevanceItemStatus.Success));
            Assert.That(result.Results[0].Matches, Is.Empty);
        }

        [Test]
        public void Query_SuccessfulItem_CopiesMatchesInPlannerOrder()
        {
            XenogermPlannerApiV1Binding binding = CreateValidBinding();

            ValidFacade.QueryHandler = _ => new ValidBatchResult(
                ValidBatchStatus.Success,
                ValidUnavailableReason.None,
                new[]
                {
                    ValidItemResult.Success(new ValidMatch("plan-2", "Beta"), new ValidMatch("plan-1", "Alpha"))
                });

            PlannerGenepackRelevanceBatchResult result = binding.Query(new[] { CreateComposition("GeneA") });

            Assert.That(result.Results[0].Matches.Count, Is.EqualTo(2));
            Assert.That(result.Results[0].Matches[0].PlanId, Is.EqualTo("plan-2"));
            Assert.That(result.Results[0].Matches[0].DisplayName, Is.EqualTo("Beta"));
            Assert.That(result.Results[0].Matches[1].PlanId, Is.EqualTo("plan-1"));
            Assert.That(result.Results[0].Matches[1].DisplayName, Is.EqualTo("Alpha"));
        }

        [TestCase(ValidBatchStatus.InvalidRequest)]
        [TestCase(ValidBatchStatus.Failed)]
        public void Query_NonSuccessfulBatch_ReturnsUnavailable(ValidBatchStatus status)
        {
            XenogermPlannerApiV1Binding binding = CreateValidBinding();

            ValidFacade.QueryHandler = _ => new ValidBatchResult(
                status,
                ValidUnavailableReason.None,
                Array.Empty<ValidItemResult>());

            PlannerGenepackRelevanceBatchResult result = binding.Query(new[] { CreateComposition("GeneA") });

            Assert.That(result.Status, Is.EqualTo(PlannerGenepackRelevanceBatchStatus.Unavailable));
            Assert.That(result.Results, Is.Empty);
        }

        [TestCase(ValidUnavailableReason.NoGame)]
        [TestCase(ValidUnavailableReason.NoActiveMap)]
        [TestCase(ValidUnavailableReason.PlannerStateUnavailable)]
        public void Query_UnavailableBatchReason_ReturnsNeutralUnavailable(ValidUnavailableReason unavailableReason)
        {
            XenogermPlannerApiV1Binding binding = CreateValidBinding();

            ValidFacade.QueryHandler = _ => new ValidBatchResult(
                ValidBatchStatus.Unavailable,
                unavailableReason,
                Array.Empty<ValidItemResult>());

            PlannerGenepackRelevanceBatchResult result = binding.Query(new[] { CreateComposition("GeneA") });

            Assert.That(result.Status, Is.EqualTo(PlannerGenepackRelevanceBatchStatus.Unavailable));
            Assert.That(result.Results, Is.Empty);
        }

        [Test]
        public void Query_UnknownBatchStatus_ReturnsUnavailable()
        {
            XenogermPlannerApiV1Binding binding = CreateValidBinding();

            ValidFacade.QueryHandler = _ => new ValidBatchResult(
                (ValidBatchStatus)999,
                ValidUnavailableReason.None,
                Array.Empty<ValidItemResult>());

            PlannerGenepackRelevanceBatchResult result = binding.Query(new[] { CreateComposition("GeneA") });

            Assert.That(result.Status, Is.EqualTo(PlannerGenepackRelevanceBatchStatus.Unavailable));
        }

        [Test]
        public void Query_NullBatchResult_ReturnsUnavailable()
        {
            XenogermPlannerApiV1Binding binding = CreateValidBinding();
            ValidFacade.QueryHandler = _ => null;

            PlannerGenepackRelevanceBatchResult result = binding.Query(new[] { CreateComposition("GeneA") });

            Assert.That(result.Status, Is.EqualTo(PlannerGenepackRelevanceBatchStatus.Unavailable));
        }

        [Test]
        public void Query_ResultCountMismatch_ReturnsUnavailable()
        {
            XenogermPlannerApiV1Binding binding = CreateValidBinding();

            ValidFacade.QueryHandler = _ => new ValidBatchResult(
                ValidBatchStatus.Success,
                ValidUnavailableReason.None,
                Array.Empty<ValidItemResult>());

            PlannerGenepackRelevanceBatchResult result = binding.Query(new[] { CreateComposition("GeneA") });

            Assert.That(result.Status, Is.EqualTo(PlannerGenepackRelevanceBatchStatus.Unavailable));
        }

        [TestCase(ValidItemStatus.InvalidInput, (int)PlannerGenepackRelevanceItemStatus.InvalidInput)]
        [TestCase(ValidItemStatus.UnknownGeneDef, (int)PlannerGenepackRelevanceItemStatus.UnknownGeneDef)]
        [TestCase(ValidItemStatus.Failed, (int)PlannerGenepackRelevanceItemStatus.Failed)]
        public void Query_ItemFailureStatus_IsMapped(ValidItemStatus reflectedStatus, int expectedStatusValue)
        {
            var expectedStatus = (PlannerGenepackRelevanceItemStatus)expectedStatusValue;
            XenogermPlannerApiV1Binding binding = CreateValidBinding();

            ValidFacade.QueryHandler = _ => new ValidBatchResult(
                ValidBatchStatus.Success,
                ValidUnavailableReason.None,
                new[] { new ValidItemResult(reflectedStatus, Array.Empty<ValidMatch>()) });

            PlannerGenepackRelevanceBatchResult result = binding.Query(new[] { CreateComposition("GeneA") });

            Assert.That(result.Results[0].Status, Is.EqualTo(expectedStatus));
            Assert.That(result.Results[0].Matches, Is.Empty);
        }

        [Test]
        public void Query_UnknownItemStatus_ProducesFailedItem()
        {
            XenogermPlannerApiV1Binding binding = CreateValidBinding();

            ValidFacade.QueryHandler = _ => new ValidBatchResult(
                ValidBatchStatus.Success,
                ValidUnavailableReason.None,
                new[]
                {
                    new ValidItemResult((ValidItemStatus)999, Array.Empty<ValidMatch>())
                });

            PlannerGenepackRelevanceBatchResult result = binding.Query(new[] { CreateComposition("GeneA") });

            Assert.That(result.Status, Is.EqualTo(PlannerGenepackRelevanceBatchStatus.Success));
            Assert.That(result.Results[0].Status, Is.EqualTo(PlannerGenepackRelevanceItemStatus.Failed));
        }

        [Test]
        public void Query_MalformedItem_DoesNotDiscardNeighboringResults()
        {
            XenogermPlannerApiV1Binding binding = CreateValidBinding();

            ValidFacade.QueryHandler = _ => new ValidBatchResult(
                ValidBatchStatus.Success,
                ValidUnavailableReason.None,
                new[]
                {
                    ValidItemResult.WithThrowingStatus(),
                    ValidItemResult.Success(new ValidMatch("plan-1", "Alpha"))
                });

            PlannerGenepackRelevanceBatchResult result = binding.Query(
                new[]
                {
                    CreateComposition("GeneA"),
                    CreateComposition("GeneB")
                });

            Assert.That(result.Status, Is.EqualTo(PlannerGenepackRelevanceBatchStatus.Success));
            Assert.That(result.Results[0].Status, Is.EqualTo(PlannerGenepackRelevanceItemStatus.Failed));
            Assert.That(result.Results[1].Status, Is.EqualTo(PlannerGenepackRelevanceItemStatus.Success));
            Assert.That(result.Results[1].Matches[0].PlanId, Is.EqualTo("plan-1"));
        }

        [Test]
        public void Query_MalformedMatch_ProducesFailedItemWithoutPartialMatches()
        {
            XenogermPlannerApiV1Binding binding = CreateValidBinding();

            ValidFacade.QueryHandler = _ => new ValidBatchResult(
                ValidBatchStatus.Success,
                ValidUnavailableReason.None,
                new[]
                {
                    ValidItemResult.Success(new ValidMatch("plan-1", "Alpha"), new ValidMatch(string.Empty, "Invalid"))
                });

            PlannerGenepackRelevanceBatchResult result = binding.Query(new[] { CreateComposition("GeneA") });

            Assert.That(result.Results[0].Status, Is.EqualTo(PlannerGenepackRelevanceItemStatus.Failed));
            Assert.That(result.Results[0].Matches, Is.Empty);
        }

        [Test]
        public void Query_InvocationThrows_ReturnsUnavailable()
        {
            XenogermPlannerApiV1Binding binding = CreateValidBinding();
            ValidFacade.QueryHandler = _ => throw new InvalidOperationException("Failure from fake API.");

            PlannerGenepackRelevanceBatchResult result = binding.Query(new[] { CreateComposition("GeneA") });

            Assert.That(result.Status, Is.EqualTo(PlannerGenepackRelevanceBatchStatus.Unavailable));
            Assert.That(result.Results, Is.Empty);
        }

        [Test]
        public void Query_NullComposition_Throws()
        {
            XenogermPlannerApiV1Binding binding = CreateValidBinding();

            Assert.That(
                (Action)(() => binding.Query(new GenepackCompositionSnapshot[] { null })),
                Throws.TypeOf<ArgumentException>());
        }

        [Test]
        public void TryCreate_OverloadedQueryMethod_ReturnsFalse()
        {
            bool created = XenogermPlannerApiV1Binding.TryCreate(
                CreateResolver(typeof(OverloadedQueryFacade)),
                out XenogermPlannerApiV1Binding binding);

            Assert.That(created, Is.False);
            Assert.That(binding, Is.Null);
        }

        private static XenogermPlannerApiV1Binding CreateValidBinding()
        {
            bool created = XenogermPlannerApiV1Binding.TryCreate(
                CreateResolver(typeof(ValidFacade)),
                out XenogermPlannerApiV1Binding binding);

            Assert.That(created, Is.True);
            Assert.That(binding, Is.Not.Null);
            return binding;
        }

        private static GenepackCompositionSnapshot CreateComposition(string geneDefName)
        {
            return new GenepackCompositionSnapshot(new[] { geneDefName });
        }

        private static Func<string, Type> CreateResolver(
            Type facadeType,
            Type requestType = null,
            Type batchResultType = null)
        {
            requestType = requestType ?? typeof(ValidRequest);
            batchResultType = batchResultType ?? typeof(ValidBatchResult);

            return typeName =>
            {
                if (typeName == XenogermPlannerApiV1Binding.FacadeTypeName)
                    return facadeType;

                if (typeName == XenogermPlannerApiV1Binding.RequestTypeName)
                    return requestType;

                if (typeName == XenogermPlannerApiV1Binding.BatchResultTypeName)
                    return batchResultType;

                if (typeName == XenogermPlannerApiV1Binding.ItemResultTypeName)
                    return typeof(ValidItemResult);

                if (typeName == XenogermPlannerApiV1Binding.PlanMatchTypeName)
                    return typeof(ValidMatch);

                if (typeName == XenogermPlannerApiV1Binding.BatchStatusTypeName)
                    return typeof(ValidBatchStatus);

                if (typeName == XenogermPlannerApiV1Binding.UnavailableReasonTypeName)
                    return typeof(ValidUnavailableReason);

                if (typeName == XenogermPlannerApiV1Binding.ItemStatusTypeName)
                    return typeof(ValidItemStatus);

                return null;
            };
        }

        public enum ValidBatchStatus
        {
            Success,
            InvalidRequest,
            Unavailable,
            Failed
        }

        public enum ValidUnavailableReason
        {
            None,
            NoGame,
            NoActiveMap,
            PlannerStateUnavailable
        }

        public enum ValidItemStatus
        {
            Success,
            InvalidInput,
            UnknownGeneDef,
            Failed
        }

        public sealed class ValidRequest
        {
            public ValidRequest(IEnumerable<string> geneDefNames)
            {
                GeneDefNames = new List<string>(geneDefNames).AsReadOnly();
            }

            public IReadOnlyList<string> GeneDefNames { get; }
        }

        public sealed class ValidMatch
        {
            public ValidMatch(string planId, string displayName)
            {
                PlanId = planId;
                DisplayName = displayName;
            }

            // ReSharper disable once UnusedAutoPropertyAccessor.Global
            public string PlanId { get; }

            // ReSharper disable once UnusedAutoPropertyAccessor.Global
            public string DisplayName { get; }
        }

        public sealed class ValidItemResult
        {
            private readonly ValidItemStatus _status;
            private readonly bool _throwOnStatusRead;

            public ValidItemResult(
                ValidItemStatus status,
                IReadOnlyList<ValidMatch> matches,
                bool throwOnStatusRead = false)
            {
                _status = status;
                Matches = matches;
                _throwOnStatusRead = throwOnStatusRead;
            }

            public ValidItemStatus Status
            {
                get
                {
                    if (_throwOnStatusRead)
                        throw new InvalidOperationException("Malformed item status.");

                    return _status;
                }
            }

            // ReSharper disable once UnusedAutoPropertyAccessor.Global
            public IReadOnlyList<ValidMatch> Matches { get; }

            public static ValidItemResult Success(params ValidMatch[] matches)
            {
                return new ValidItemResult(ValidItemStatus.Success, matches);
            }

            public static ValidItemResult WithThrowingStatus()
            {
                return new ValidItemResult(ValidItemStatus.Success, Array.Empty<ValidMatch>(), throwOnStatusRead: true);
            }
        }

        public sealed class ValidBatchResult
        {
            public ValidBatchResult(
                ValidBatchStatus status,
                ValidUnavailableReason unavailableReason,
                IReadOnlyList<ValidItemResult> results)
            {
                Status = status;
                UnavailableReason = unavailableReason;
                Results = results;
            }

            // ReSharper disable once UnusedAutoPropertyAccessor.Global
            public ValidBatchStatus Status { get; }

            // ReSharper disable once UnusedAutoPropertyAccessor.Global
            public ValidUnavailableReason UnavailableReason { get; }

            // ReSharper disable once UnusedAutoPropertyAccessor.Global
            public IReadOnlyList<ValidItemResult> Results { get; }
        }

        public static class ValidFacade
        {
            public static Func<IReadOnlyList<ValidRequest>, ValidBatchResult> QueryHandler { get; set; }

            public static int ApiVersion => 1;

            public static ValidBatchResult QueryGenepackRelevance(IReadOnlyList<ValidRequest> requests)
            {
                if (QueryHandler == null)
                    throw new InvalidOperationException("A fake query handler was not configured.");

                return QueryHandler(requests);
            }
        }

        public static class VersionZeroFacade
        {
            public static int ApiVersion => 0;
        }

        public static class VersionTwoFacade
        {
            public static int ApiVersion => 2;
        }

        public static class MissingVersionFacade
        {
        }

        public static class OverloadedQueryFacade
        {
            public static int ApiVersion => 1;

            public static ValidBatchResult QueryGenepackRelevance(IReadOnlyList<ValidRequest> _)
            {
                return null;
            }

            public static ValidBatchResult QueryGenepackRelevance(IEnumerable<ValidRequest> _)
            {
                return null;
            }
        }

        public sealed class RequestWithoutExpectedConstructor
        {
        }

        public static class RequestWithoutExpectedConstructorFacade
        {
            public static int ApiVersion => 1;

            public static ValidBatchResult QueryGenepackRelevance(IReadOnlyList<RequestWithoutExpectedConstructor> _)
            {
                return null;
            }
        }

        public static class WrongQuerySignatureFacade
        {
            public static int ApiVersion => 1;

            public static ValidBatchResult QueryGenepackRelevance(IEnumerable<ValidRequest> _)
            {
                return null;
            }
        }

        public static class WrongQueryReturnTypeFacade
        {
            public static int ApiVersion => 1;

            public static object QueryGenepackRelevance(IReadOnlyList<ValidRequest> _)
            {
                return null;
            }
        }

        public sealed class BatchWithoutResults
        {
            public ValidBatchStatus Status =>
                ValidBatchStatus.Success;

            public ValidUnavailableReason UnavailableReason =>
                ValidUnavailableReason.None;
        }

        public static class BatchWithoutResultsFacade
        {
            public static int ApiVersion => 1;

            public static BatchWithoutResults QueryGenepackRelevance(IReadOnlyList<ValidRequest> _)
            {
                return null;
            }
        }
    }
}