using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SettlementTradeOverview.Integration.Planner;

namespace SettlementTradeOverview.Tests.Project
{
    [TestFixture]
    public sealed class OptionalPlannerAssemblyBoundaryTests
    {
        private static readonly Type[] _integrationTypes =
        {
            typeof(PlannerGenepackRelevanceBatchStatus),
            typeof(PlannerGenepackRelevanceItemStatus),
            typeof(PlannerGenepackRelevancePlanMatch),
            typeof(PlannerGenepackRelevanceItemResult),
            typeof(PlannerGenepackRelevanceBatchResult),
            typeof(PlannerTradeEntryRelevance),
            typeof(PlannerTradeRelevanceProjection),
            typeof(PlannerTradeRelevanceProjectionBuilder),
            typeof(XenogermPlannerApiV1Binding),
            typeof(PlannerGenepackRelevanceAdapter)
        };

        [Test]
        public void ProductionAssembly_DoesNotReferenceXenogermPlanner()
        {
            AssemblyName[] referencedAssemblies = typeof(PlannerGenepackRelevanceAdapter)
                .Assembly.GetReferencedAssemblies();

            Assert.That(
                referencedAssemblies.Any(assemblyName => string.Equals(
                    assemblyName.Name,
                    "XenogermPlanner",
                    StringComparison.Ordinal)),
                Is.False);
        }

        [Test]
        public void IntegrationSurface_DoesNotExposePlannerApiTypes()
        {
            foreach (Type integrationType in _integrationTypes)
            {
                foreach (ConstructorInfo constructor in integrationType.GetConstructors(
                             BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                {
                    if (constructor.IsPrivate)
                        continue;

                    foreach (ParameterInfo parameter in constructor.GetParameters())
                    {
                        AssertTypeIsPlannerFree(
                            integrationType,
                            $"constructor parameter '{parameter.Name}'",
                            parameter.ParameterType);
                    }
                }

                const BindingFlags PropertyFlags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public |
                                                   BindingFlags.NonPublic;

                foreach (PropertyInfo property in integrationType.GetProperties(PropertyFlags))
                {
                    MethodInfo getter = property.GetGetMethod(nonPublic: true);
                    MethodInfo setter = property.GetSetMethod(nonPublic: true);

                    if ((getter == null || getter.IsPrivate) && (setter == null || setter.IsPrivate))
                        continue;

                    AssertTypeIsPlannerFree(integrationType, $"property '{property.Name}'", property.PropertyType);
                }

                const BindingFlags MethodFlags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public |
                                                 BindingFlags.NonPublic;

                foreach (MethodInfo method in integrationType.GetMethods(MethodFlags))
                {
                    if (method.IsPrivate || method.IsSpecialName || method.DeclaringType != integrationType)
                        continue;

                    AssertTypeIsPlannerFree(integrationType, $"method '{method.Name}' return type", method.ReturnType);

                    foreach (ParameterInfo parameter in method.GetParameters())
                    {
                        AssertTypeIsPlannerFree(
                            integrationType,
                            $"method '{method.Name}' parameter '{parameter.Name}'",
                            parameter.ParameterType);
                    }
                }
            }
        }

        private static void AssertTypeIsPlannerFree(Type ownerType, string memberDescription, Type exposedType)
        {
            foreach (Type type in ExpandType(exposedType))
            {
                string typeNamespace = type.Namespace ?? string.Empty;

                bool exposesPlannerType = typeNamespace.Equals("XenogermPlanner", StringComparison.Ordinal) ||
                                          typeNamespace.StartsWith("XenogermPlanner.", StringComparison.Ordinal);

                Assert.That(
                    exposesPlannerType,
                    Is.False,
                    $"{ownerType.FullName} exposes Planner type {type.FullName} through {memberDescription}.");
            }
        }

        private static IEnumerable<Type> ExpandType(Type type)
        {
            if (type.IsByRef || type.IsPointer || type.IsArray)
            {
                Type elementType = type.GetElementType();

                if (elementType != null)
                {
                    foreach (Type expandedType in ExpandType(elementType))
                        yield return expandedType;
                }

                yield break;
            }

            yield return type;

            if (!type.IsGenericType)
                yield break;

            foreach (Type genericArgument in type.GetGenericArguments())
            {
                foreach (Type expandedType in ExpandType(genericArgument))
                    yield return expandedType;
            }
        }
    }
}