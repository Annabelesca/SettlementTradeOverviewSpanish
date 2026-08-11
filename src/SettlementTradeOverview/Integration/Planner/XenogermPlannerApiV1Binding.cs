using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using SettlementTradeOverview.Domain.Snapshots;

namespace SettlementTradeOverview.Integration.Planner
{
    internal sealed class XenogermPlannerApiV1Binding
    {
        internal const string FacadeTypeName = "XenogermPlanner.Api.XenogermPlannerApi, XenogermPlanner";

        internal const string RequestTypeName = "XenogermPlanner.Api.GenepackRelevanceRequest, XenogermPlanner";

        internal const string BatchResultTypeName = "XenogermPlanner.Api.GenepackRelevanceBatchResult, XenogermPlanner";

        internal const string ItemResultTypeName = "XenogermPlanner.Api.GenepackRelevanceItemResult, XenogermPlanner";

        internal const string PlanMatchTypeName = "XenogermPlanner.Api.GenepackRelevancePlanMatch, XenogermPlanner";

        internal const string BatchStatusTypeName = "XenogermPlanner.Api.GenepackRelevanceBatchStatus, XenogermPlanner";

        internal const string UnavailableReasonTypeName =
            "XenogermPlanner.Api.GenepackRelevanceUnavailableReason, XenogermPlanner";

        internal const string ItemStatusTypeName = "XenogermPlanner.Api.GenepackRelevanceItemStatus, XenogermPlanner";

        private readonly Type _batchStatusType;
        private readonly Type _itemResultType;
        private readonly Type _itemStatusType;
        private readonly Type _planMatchType;
        private readonly Type _requestListType;
        private readonly ConstructorInfo _requestConstructor;
        private readonly MethodInfo _queryMethod;
        private readonly PropertyInfo _batchResultsProperty;
        private readonly PropertyInfo _batchStatusProperty;
        private readonly PropertyInfo _itemMatchesProperty;
        private readonly PropertyInfo _itemStatusProperty;
        private readonly PropertyInfo _matchDisplayNameProperty;
        private readonly PropertyInfo _matchPlanIdProperty;

        private XenogermPlannerApiV1Binding(
            Type requestType,
            Type batchStatusType,
            Type itemResultType,
            Type itemStatusType,
            Type planMatchType,
            ConstructorInfo requestConstructor,
            MethodInfo queryMethod,
            PropertyInfo batchStatusProperty,
            PropertyInfo batchResultsProperty,
            PropertyInfo itemStatusProperty,
            PropertyInfo itemMatchesProperty,
            PropertyInfo matchPlanIdProperty,
            PropertyInfo matchDisplayNameProperty)
        {
            _batchStatusType = batchStatusType;
            _itemResultType = itemResultType;
            _itemStatusType = itemStatusType;
            _planMatchType = planMatchType;
            _requestListType = typeof(List<>).MakeGenericType(requestType);
            _requestConstructor = requestConstructor;
            _queryMethod = queryMethod;
            _batchStatusProperty = batchStatusProperty;
            _batchResultsProperty = batchResultsProperty;
            _itemStatusProperty = itemStatusProperty;
            _itemMatchesProperty = itemMatchesProperty;
            _matchPlanIdProperty = matchPlanIdProperty;
            _matchDisplayNameProperty = matchDisplayNameProperty;
        }

        public static bool TryCreate(out XenogermPlannerApiV1Binding binding)
        {
            return TryCreate(typeName => Type.GetType(typeName, throwOnError: false), out binding);
        }

        internal static bool TryCreate(Func<string, Type> typeResolver, out XenogermPlannerApiV1Binding binding)
        {
            if (typeResolver == null)
                throw new ArgumentNullException(nameof(typeResolver));

            binding = null;

            try
            {
                Type facadeType = typeResolver(FacadeTypeName);

                if (!IsPublicApiType(facadeType) || !TryReadApiVersion(facadeType, out int apiVersion))
                    return false;

                if (apiVersion != 1)
                    return false;

                Type requestType = typeResolver(RequestTypeName);
                Type batchResultType = typeResolver(BatchResultTypeName);
                Type itemResultType = typeResolver(ItemResultTypeName);
                Type planMatchType = typeResolver(PlanMatchTypeName);
                Type batchStatusType = typeResolver(BatchStatusTypeName);
                Type unavailableReasonType = typeResolver(UnavailableReasonTypeName);
                Type itemStatusType = typeResolver(ItemStatusTypeName);

                if (!ArePublicApiTypes(
                        requestType,
                        batchResultType,
                        itemResultType,
                        planMatchType,
                        batchStatusType,
                        unavailableReasonType,
                        itemStatusType))
                {
                    return false;
                }

                if (!ValidateEnumValues(batchStatusType, "Success", "InvalidRequest", "Unavailable", "Failed") ||
                    !ValidateEnumValues(
                        unavailableReasonType,
                        "None",
                        "NoGame",
                        "NoActiveMap",
                        "PlannerStateUnavailable") || !ValidateEnumValues(
                        itemStatusType,
                        "Success",
                        "InvalidInput",
                        "UnknownGeneDef",
                        "Failed"))
                {
                    return false;
                }

                ConstructorInfo requestConstructor = requestType.GetConstructor(
                    BindingFlags.Instance | BindingFlags.Public,
                    binder: null,
                    types: new[] { typeof(IEnumerable<string>) },
                    modifiers: null);

                if (requestConstructor == null)
                    return false;

                Type requestListInterface = typeof(IReadOnlyList<>).MakeGenericType(requestType);

                MethodInfo queryMethod = GetExactPublicStaticMethod(
                    facadeType,
                    "QueryGenepackRelevance",
                    requestListInterface,
                    batchResultType);

                if (queryMethod == null)
                    return false;

                PropertyInfo batchStatusProperty = GetReadableInstanceProperty(
                    batchResultType,
                    "Status",
                    batchStatusType);

                PropertyInfo batchUnavailableReasonProperty = GetReadableInstanceProperty(
                    batchResultType,
                    "UnavailableReason",
                    unavailableReasonType);

                PropertyInfo batchResultsProperty = GetReadableInstanceProperty(
                    batchResultType,
                    "Results",
                    typeof(IReadOnlyList<>).MakeGenericType(itemResultType));

                PropertyInfo itemStatusProperty = GetReadableInstanceProperty(itemResultType, "Status", itemStatusType);

                PropertyInfo itemMatchesProperty = GetReadableInstanceProperty(
                    itemResultType,
                    "Matches",
                    typeof(IReadOnlyList<>).MakeGenericType(planMatchType));

                PropertyInfo matchPlanIdProperty = GetReadableInstanceProperty(planMatchType, "PlanId", typeof(string));

                PropertyInfo matchDisplayNameProperty = GetReadableInstanceProperty(
                    planMatchType,
                    "DisplayName",
                    typeof(string));

                if (batchStatusProperty == null || batchUnavailableReasonProperty == null ||
                    batchResultsProperty == null || itemStatusProperty == null || itemMatchesProperty == null ||
                    matchPlanIdProperty == null || matchDisplayNameProperty == null)
                {
                    return false;
                }

                binding = new XenogermPlannerApiV1Binding(
                    requestType,
                    batchStatusType,
                    itemResultType,
                    itemStatusType,
                    planMatchType,
                    requestConstructor,
                    queryMethod,
                    batchStatusProperty,
                    batchResultsProperty,
                    itemStatusProperty,
                    itemMatchesProperty,
                    matchPlanIdProperty,
                    matchDisplayNameProperty);

                return true;
            }
            catch
            {
                binding = null;
                return false;
            }
        }

        public PlannerGenepackRelevanceBatchResult Query(IReadOnlyList<GenepackCompositionSnapshot> compositions)
        {
            if (compositions == null)
                throw new ArgumentNullException(nameof(compositions));

            foreach (GenepackCompositionSnapshot t in compositions)
            {
                if (t == null)
                {
                    throw new ArgumentException(
                        "Composition collection cannot contain null values.",
                        nameof(compositions));
                }
            }

            if (compositions.Count == 0)
            {
                return PlannerGenepackRelevanceBatchResult.CreateSuccess(
                    Array.Empty<PlannerGenepackRelevanceItemResult>());
            }

            try
            {
                var requests = (IList)Activator.CreateInstance(_requestListType);

                foreach (GenepackCompositionSnapshot t in compositions)
                {
                    object request = _requestConstructor.Invoke(new object[] { t.GeneDefNames });

                    if (request == null)
                        return PlannerGenepackRelevanceBatchResult.CreateUnavailable();

                    requests.Add(request);
                }

                object reflectedBatchResult = _queryMethod.Invoke(null, new object[] { requests });

                return ParseBatchResult(reflectedBatchResult, compositions.Count);
            }
            catch
            {
                return PlannerGenepackRelevanceBatchResult.CreateUnavailable();
            }
        }

        private PlannerGenepackRelevanceBatchResult ParseBatchResult(
            object reflectedBatchResult,
            int expectedResultCount)
        {
            if (reflectedBatchResult == null)
                return PlannerGenepackRelevanceBatchResult.CreateUnavailable();

            string statusName = GetEnumName(_batchStatusType, _batchStatusProperty.GetValue(reflectedBatchResult));

            if (!string.Equals(statusName, "Success", StringComparison.Ordinal))
                return PlannerGenepackRelevanceBatchResult.CreateUnavailable();

            object reflectedResults = _batchResultsProperty.GetValue(reflectedBatchResult);

            if (!TryCopySequence(reflectedResults, out List<object> itemResults) ||
                itemResults.Count != expectedResultCount)
            {
                return PlannerGenepackRelevanceBatchResult.CreateUnavailable();
            }

            var results = new List<PlannerGenepackRelevanceItemResult>(itemResults.Count);

            foreach (object itemResult in itemResults)
                results.Add(ParseItemResult(itemResult));

            return PlannerGenepackRelevanceBatchResult.CreateSuccess(results);
        }

        private PlannerGenepackRelevanceItemResult ParseItemResult(object reflectedItemResult)
        {
            if (reflectedItemResult == null || !_itemResultType.IsInstanceOfType(reflectedItemResult))
                return PlannerGenepackRelevanceItemResult.CreateFailed();

            try
            {
                string statusName = GetEnumName(_itemStatusType, _itemStatusProperty.GetValue(reflectedItemResult));

                switch (statusName)
                {
                    case "Success":
                        return ParseSuccessfulItemResult(reflectedItemResult);
                    case "InvalidInput":
                        return PlannerGenepackRelevanceItemResult.CreateInvalidInput();
                    case "UnknownGeneDef":
                        return PlannerGenepackRelevanceItemResult.CreateUnknownGeneDef();
                    default:
                        return PlannerGenepackRelevanceItemResult.CreateFailed();
                }
            }
            catch
            {
                return PlannerGenepackRelevanceItemResult.CreateFailed();
            }
        }

        private PlannerGenepackRelevanceItemResult ParseSuccessfulItemResult(object reflectedItemResult)
        {
            object reflectedMatches = _itemMatchesProperty.GetValue(reflectedItemResult);

            if (!TryCopySequence(reflectedMatches, out List<object> matches))
                return PlannerGenepackRelevanceItemResult.CreateFailed();

            var copiedMatches = new List<PlannerGenepackRelevancePlanMatch>(matches.Count);

            foreach (object reflectedMatch in matches)
            {
                if (reflectedMatch == null || !_planMatchType.IsInstanceOfType(reflectedMatch))
                    return PlannerGenepackRelevanceItemResult.CreateFailed();

                var planId = _matchPlanIdProperty.GetValue(reflectedMatch) as string;
                var displayName = _matchDisplayNameProperty.GetValue(reflectedMatch) as string;

                if (string.IsNullOrWhiteSpace(planId) || string.IsNullOrWhiteSpace(displayName))
                    return PlannerGenepackRelevanceItemResult.CreateFailed();

                copiedMatches.Add(new PlannerGenepackRelevancePlanMatch(planId, displayName));
            }

            return PlannerGenepackRelevanceItemResult.CreateSuccess(copiedMatches);
        }

        private static bool TryReadApiVersion(Type facadeType, out int apiVersion)
        {
            apiVersion = 0;

            PropertyInfo property = facadeType.GetProperty("ApiVersion", BindingFlags.Static | BindingFlags.Public);

            if (property == null || property.PropertyType != typeof(int) || property.GetIndexParameters().Length != 0)
                return false;

            MethodInfo getter = property.GetGetMethod(nonPublic: false);

            if (getter == null || !getter.IsStatic)
                return false;

            object value = property.GetValue(null);

            if (!(value is int resolvedVersion))
                return false;

            apiVersion = resolvedVersion;
            return true;
        }

        private static PropertyInfo GetReadableInstanceProperty(
            Type ownerType,
            string propertyName,
            Type expectedPropertyType)
        {
            PropertyInfo property = ownerType.GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);

            if (property == null || property.PropertyType != expectedPropertyType ||
                property.GetIndexParameters().Length != 0)
            {
                return null;
            }

            MethodInfo getter = property.GetGetMethod(nonPublic: false);

            return getter != null && !getter.IsStatic ? property : null;
        }

        private static MethodInfo GetExactPublicStaticMethod(
            Type ownerType,
            string methodName,
            Type parameterType,
            Type returnType)
        {
            MethodInfo resolvedMethod = null;

            foreach (MethodInfo method in ownerType.GetMethods(
                         BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly))
            {
                if (!string.Equals(method.Name, methodName, StringComparison.Ordinal))
                    continue;

                if (resolvedMethod != null)
                    return null;

                ParameterInfo[] parameters = method.GetParameters();

                if (method.IsGenericMethod || method.ReturnType != returnType || parameters.Length != 1 ||
                    parameters[0].ParameterType != parameterType)
                {
                    return null;
                }

                resolvedMethod = method;
            }

            return resolvedMethod;
        }

        private static bool ValidateEnumValues(Type enumType, params string[] names)
        {
            if (enumType == null || !enumType.IsEnum)
                return false;

            foreach (string name in names)
            {
                if (!Enum.IsDefined(enumType, name))
                    return false;
            }

            return true;
        }

        private static string GetEnumName(Type enumType, object value)
        {
            if (value == null || value.GetType() != enumType)
                return null;

            return Enum.GetName(enumType, value);
        }

        private static bool TryCopySequence(object value, out List<object> items)
        {
            items = null;

            if (!(value is IEnumerable sequence))
                return false;

            var copiedItems = new List<object>();

            foreach (object item in sequence)
                copiedItems.Add(item);

            items = copiedItems;
            return true;
        }

        private static bool ArePublicApiTypes(params Type[] types)
        {
            foreach (Type type in types)
            {
                if (!IsPublicApiType(type))
                    return false;
            }

            return true;
        }

        private static bool IsPublicApiType(Type type)
        {
            return type != null && (type.IsPublic || type.IsNestedPublic);
        }
    }
}