using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SettlementTradeOverview.Domain.Snapshots;

namespace SettlementTradeOverview.Tests.Domain.Snapshots
{
    [TestFixture]
    public sealed class SnapshotRuntimeBoundaryTests
    {
        private static readonly Type[] _snapshotTypes =
        {
            typeof(PawnTradeDetailKind),
            typeof(PawnTradeDetailsSnapshot),
            typeof(GenepackCompositionSnapshot),
            typeof(TradeRestockMoment),
            typeof(TradeRestock),
            typeof(TradeEntrySnapshot),
            typeof(TradeCurrencySnapshot),
            typeof(TradeNegotiatorSnapshot),
            typeof(TraderSnapshot),
            typeof(TradeInventorySnapshot)
        };

        private static readonly string[] _forbiddenNamespacePrefixes =
        {
            "RimWorld",
            "Verse",
            "UnityEngine",
            "Escarval.RimWorld.UI"
        };

        [Test]
        public void SnapshotPublicProperties_DoNotExposeRuntimeOrUiTypes()
        {
            foreach (Type snapshotType in _snapshotTypes)
            {
                PropertyInfo[] properties = snapshotType.GetProperties(BindingFlags.Instance | BindingFlags.Public);

                foreach (PropertyInfo property in properties)
                {
                    AssertTypeIsAllowed(snapshotType, $"property '{property.Name}'", property.PropertyType);
                }
            }
        }

        [Test]
        public void SnapshotPublicConstructors_DoNotAcceptRuntimeOrUiTypes()
        {
            foreach (Type snapshotType in _snapshotTypes)
            {
                ConstructorInfo[] constructors = snapshotType.GetConstructors(
                    BindingFlags.Instance | BindingFlags.Public);

                foreach (ConstructorInfo constructor in constructors)
                {
                    foreach (ParameterInfo parameter in constructor.GetParameters())
                    {
                        AssertTypeIsAllowed(
                            snapshotType,
                            $"constructor parameter '{parameter.Name}'",
                            parameter.ParameterType);
                    }
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
                    $"{ownerType.FullName} exposes forbidden type {type.FullName} through {memberDescription}.");
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