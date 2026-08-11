using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SettlementTradeOverview.Domain.Eligibility;

namespace SettlementTradeOverview.Tests.Domain.Eligibility
{
    [TestFixture]
    public sealed class SettlementEligibilityRuntimeBoundaryTests
    {
        private static readonly Type[] _contractTypes =
        {
            typeof(SettlementTechnologyLevel),
            typeof(SettlementReachabilityState),
            typeof(SettlementRoyaltyTradePermissionState),
            typeof(SettlementEligibilityCriteria),
            typeof(SettlementEligibilityFacts),
            typeof(SettlementEligibilityFailureReason),
            typeof(SettlementEligibilityResult),
            typeof(SettlementEligibilityPolicy)
        };

        private static readonly string[] _forbiddenNamespacePrefixes =
        {
            "RimWorld",
            "Verse",
            "UnityEngine",
            "Escarval.RimWorld.UI"
        };

        [Test]
        public void EligibilityPublicContracts_DoNotExposeRuntimeOrUiTypes()
        {
            foreach (Type contractType in _contractTypes)
            {
                ValidateProperties(contractType);
                ValidateConstructors(contractType);
                ValidateMethods(contractType);
            }
        }

        private static void ValidateProperties(Type contractType)
        {
            PropertyInfo[] properties = contractType.GetProperties(
                BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.DeclaredOnly);

            foreach (PropertyInfo property in properties)
            {
                AssertTypeIsAllowed(contractType, $"property '{property.Name}'", property.PropertyType);
            }
        }

        private static void ValidateConstructors(Type contractType)
        {
            ConstructorInfo[] constructors = contractType.GetConstructors(BindingFlags.Instance | BindingFlags.Public);

            foreach (ConstructorInfo constructor in constructors)
            {
                foreach (ParameterInfo parameter in constructor.GetParameters())
                {
                    AssertTypeIsAllowed(
                        contractType,
                        $"constructor parameter '{parameter.Name}'",
                        parameter.ParameterType);
                }
            }
        }

        private static void ValidateMethods(Type contractType)
        {
            MethodInfo[] methods = contractType.GetMethods(
                BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.DeclaredOnly);

            foreach (MethodInfo method in methods)
            {
                AssertTypeIsAllowed(contractType, $"method '{method.Name}' return value", method.ReturnType);

                foreach (ParameterInfo parameter in method.GetParameters())
                {
                    AssertTypeIsAllowed(
                        contractType,
                        $"method '{method.Name}' parameter '{parameter.Name}'",
                        parameter.ParameterType);
                }
            }
        }

        private static void AssertTypeIsAllowed(Type ownerType, string memberDescription, Type exposedType)
        {
            foreach (Type type in ExpandType(exposedType))
            {
                string typeNamespace = type.Namespace ?? string.Empty;

                bool forbidden = _forbiddenNamespacePrefixes.Any(prefix =>
                    typeNamespace.Equals(prefix, StringComparison.Ordinal) ||
                    typeNamespace.StartsWith(prefix + ".", StringComparison.Ordinal));

                Assert.That(
                    forbidden,
                    Is.False,
                    $"{ownerType.FullName} exposes forbidden type " + $"{type.FullName} through {memberDescription}.");
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