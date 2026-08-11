using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SettlementTradeOverview.Domain.Categories;
using SettlementTradeOverview.Domain.Identity;
using SettlementTradeOverview.Domain.Snapshots;
using SettlementTradeOverview.Query;

namespace SettlementTradeOverview.Tests.Query
{
    [TestFixture]
    public sealed class TradeSnapshotQueryTests
    {
        private static readonly Type[] _publicContractTypes =
        {
            typeof(TradeCategory),
            typeof(TradeCategoryMembership),
            typeof(TradeCategoryClassifier),
            typeof(TradeSortMode),
            typeof(TradeSortDirection),
            typeof(TradeQueryCriteria),
            typeof(TradeQueryEntry),
            typeof(TradeSnapshotQuery)
        };

        private static readonly string[] _forbiddenNamespacePrefixes =
        {
            "RimWorld",
            "Verse",
            "UnityEngine",
            "Escarval.RimWorld.UI"
        };

        [Test]
        public void Execute_GlobalSnapshot_FlattensEntriesFromAllQueryableTraders()
        {
            TraderSnapshot first = CreateTrader(
                "Trader:1",
                1,
                "Alpha",
                SnapshotAvailability.Available,
                new[]
                {
                    CreateItem("Steel", "Steel"),
                    CreateItem("Gold", "Gold")
                });

            TraderSnapshot second = CreateTrader(
                "Trader:2",
                2,
                "Beta",
                SnapshotAvailability.Available,
                new[]
                {
                    CreatePawn("Pawn:1", "Morgan")
                });

            TradeInventorySnapshot snapshot = CreateInventory(SnapshotAvailability.Available, first, second);

            IReadOnlyList<TradeQueryEntry> result = TradeSnapshotQuery.Execute(snapshot, null);

            Assert.That(result.Count, Is.EqualTo(3));
            Assert.That(
                result.Select(row => row.Entry.Label),
                Is.EqualTo(
                    new[]
                    {
                        "Gold",
                        "Morgan",
                        "Steel"
                    }));

            Assert.That(result.Select(row => row.Trader.SettlementLabel), Does.Contain("Alpha"));

            Assert.That(result.Select(row => row.Trader.SettlementLabel), Does.Contain("Beta"));
        }

        [Test]
        public void Execute_SingleTrader_UsesSameFilteringAndSortingPipeline()
        {
            TraderSnapshot trader = CreateTrader(
                "Trader:1",
                1,
                "Alpha",
                SnapshotAvailability.Available,
                new[]
                {
                    CreateItem("Meal", "Fine meal", TradeCategoryMembership.Foods),
                    CreateItem("Rifle", "Assault rifle", TradeCategoryMembership.Weapons)
                });

            var criteria = new TradeQueryCriteria(TradeCategory.Weapons, "rifle");

            IReadOnlyList<TradeQueryEntry> result = TradeSnapshotQuery.Execute(trader, criteria);

            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result[0].Trader, Is.SameAs(trader));
            Assert.That(result[0].Entry.Identity.Value, Is.EqualTo("Rifle"));
        }

        [Test]
        public void Execute_DoesNotIncludeTraderCurrency()
        {
            TraderSnapshot trader = CreateTrader(
                "Trader:1",
                1,
                "Alpha",
                SnapshotAvailability.Available,
                new[]
                {
                    CreateItem("Steel", "Steel")
                },
                new TradeCurrencySnapshot(new TradeEntryIdentity("Silver"), "Silver", "Silver", 1000));

            IReadOnlyList<TradeQueryEntry> result = TradeSnapshotQuery.Execute(trader, TradeQueryCriteria.Default);

            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result[0].Entry.Identity.Value, Is.EqualTo("Steel"));
        }

        [Test]
        public void Execute_CurrencyOnlyTrader_ReturnsEmptyResult()
        {
            TraderSnapshot trader = CreateTrader(
                "Trader:1",
                1,
                "Alpha",
                SnapshotAvailability.Available,
                Array.Empty<TradeEntrySnapshot>(),
                new TradeCurrencySnapshot(new TradeEntryIdentity("Silver"), "Silver", "Silver", 1000));

            IReadOnlyList<TradeQueryEntry> result = TradeSnapshotQuery.Execute(trader, TradeQueryCriteria.Default);

            Assert.That(result, Is.Empty);
        }

        [Test]
        public void Execute_CategoryAll_ReturnsEntriesFromEveryCategory()
        {
            TraderSnapshot trader = CreateTrader(
                "Trader:1",
                1,
                "Alpha",
                SnapshotAvailability.Available,
                new[]
                {
                    CreateItem("Meal", "Meal", TradeCategoryMembership.Foods),
                    CreateItem("Steel", "Steel", TradeCategoryMembership.ResourcesRaw),
                    CreatePawn("Pawn:1", "Morgan")
                });

            TradeQueryCriteria criteria = TradeQueryCriteria.Default;

            IReadOnlyList<TradeQueryEntry> result = TradeSnapshotQuery.Execute(trader, criteria);

            Assert.That(result.Count, Is.EqualTo(3));
        }

        [Test]
        public void Execute_CategoryFilter_ReturnsOnlyMatchingEntries()
        {
            TraderSnapshot trader = CreateTrader(
                "Trader:1",
                1,
                "Alpha",
                SnapshotAvailability.Available,
                new[]
                {
                    CreateItem("Meal", "Meal", TradeCategoryMembership.Foods),
                    CreateItem("Steel", "Steel", TradeCategoryMembership.ResourcesRaw),
                    CreatePawn("Pawn:1", "Morgan")
                });

            var criteria = new TradeQueryCriteria(TradeCategory.Pawns);

            IReadOnlyList<TradeQueryEntry> result = TradeSnapshotQuery.Execute(trader, criteria);

            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result[0].Entry.Kind, Is.EqualTo(TradeEntryKind.Pawn));
            Assert.That(result[0].Entry.Category, Is.EqualTo(TradeCategory.Pawns));
        }

        [Test]
        public void Execute_SearchMatchesItemPawnAndSettlementLabelsIgnoringCase()
        {
            TraderSnapshot alpha = CreateTrader(
                "Trader:1",
                1,
                "Alpha Colony",
                SnapshotAvailability.Available,
                new[]
                {
                    CreateItem("Component", "Advanced component"),
                    CreatePawn("Pawn:1", "Morgan Bell")
                });

            TraderSnapshot beta = CreateTrader(
                "Trader:2",
                2,
                "Beta Outpost",
                SnapshotAvailability.Available,
                new[]
                {
                    CreateItem("Steel", "Steel")
                });

            TradeInventorySnapshot snapshot = CreateInventory(SnapshotAvailability.Available, alpha, beta);

            IReadOnlyList<TradeQueryEntry> itemResult = TradeSnapshotQuery.Execute(
                snapshot,
                new TradeQueryCriteria(searchText: "COMPONENT"));

            IReadOnlyList<TradeQueryEntry> pawnResult = TradeSnapshotQuery.Execute(
                snapshot,
                new TradeQueryCriteria(searchText: "morgan"));

            IReadOnlyList<TradeQueryEntry> settlementResult = TradeSnapshotQuery.Execute(
                snapshot,
                new TradeQueryCriteria(searchText: "beta"));

            Assert.That(itemResult.Count, Is.EqualTo(1));
            Assert.That(itemResult[0].Entry.Identity.Value, Is.EqualTo("Component"));

            Assert.That(pawnResult.Count, Is.EqualTo(1));
            Assert.That(pawnResult[0].Entry.Identity.Value, Is.EqualTo("Pawn:1"));

            Assert.That(settlementResult.Count, Is.EqualTo(1));
            Assert.That(settlementResult[0].Entry.Identity.Value, Is.EqualTo("Steel"));
        }

        [Test]
        public void Execute_SearchDoesNotUseDefinitionNameOrTraderLabel()
        {
            TraderSnapshot trader = CreateTrader(
                "Trader:1",
                1,
                "Alpha",
                SnapshotAvailability.Available,
                new[]
                {
                    CreateItem("Entry:1", "Visible label", definitionName: "HiddenDefinition")
                },
                traderLabel: "Hidden trader label");

            IReadOnlyList<TradeQueryEntry> definitionResult = TradeSnapshotQuery.Execute(
                trader,
                new TradeQueryCriteria(searchText: "HiddenDefinition"));

            IReadOnlyList<TradeQueryEntry> traderResult = TradeSnapshotQuery.Execute(
                trader,
                new TradeQueryCriteria(searchText: "Hidden trader"));

            Assert.That(definitionResult, Is.Empty);
            Assert.That(traderResult, Is.Empty);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void Execute_EmptySearchText_DoesNotFilterEntries(string searchText)
        {
            TraderSnapshot trader = CreateTrader(
                "Trader:1",
                1,
                "Alpha",
                SnapshotAvailability.Available,
                new[]
                {
                    CreateItem("Steel", "Steel"),
                    CreateItem("Gold", "Gold")
                });

            IReadOnlyList<TradeQueryEntry> result = TradeSnapshotQuery.Execute(
                trader,
                new TradeQueryCriteria(searchText: searchText));

            Assert.That(result.Count, Is.EqualTo(2));
        }

        [TestCase(SnapshotAvailability.Empty)]
        [TestCase(SnapshotAvailability.Unavailable)]
        [TestCase(SnapshotAvailability.Failed)]
        public void Execute_NonQueryableGlobalAvailability_ReturnsEmptyResult(SnapshotAvailability availability)
        {
            var snapshot = new TradeInventorySnapshot(availability, 0, null, null, Array.Empty<TraderSnapshot>());

            IReadOnlyList<TradeQueryEntry> result = TradeSnapshotQuery.Execute(snapshot, TradeQueryCriteria.Default);

            Assert.That(result, Is.Empty);
        }

        [TestCase(SnapshotAvailability.Empty)]
        [TestCase(SnapshotAvailability.Unavailable)]
        [TestCase(SnapshotAvailability.Failed)]
        public void Execute_NonQueryableTraderAvailability_ReturnsEmptyResult(SnapshotAvailability availability)
        {
            TraderSnapshot trader = CreateTrader(
                "Trader:1",
                1,
                "Alpha",
                availability,
                Array.Empty<TradeEntrySnapshot>());

            IReadOnlyList<TradeQueryEntry> result = TradeSnapshotQuery.Execute(trader, TradeQueryCriteria.Default);

            Assert.That(result, Is.Empty);
        }

        [Test]
        public void Execute_PartialSnapshot_ReturnsAvailableEntriesAndSkipsFailedTraders()
        {
            TraderSnapshot available = CreateTrader(
                "Trader:1",
                1,
                "Alpha",
                SnapshotAvailability.Partial,
                new[]
                {
                    CreateItem("Steel", "Steel")
                });

            TraderSnapshot failed = CreateTrader(
                "Trader:2",
                2,
                "Beta",
                SnapshotAvailability.Failed,
                Array.Empty<TradeEntrySnapshot>());

            TradeInventorySnapshot snapshot = CreateInventory(SnapshotAvailability.Partial, available, failed);

            IReadOnlyList<TradeQueryEntry> result = TradeSnapshotQuery.Execute(snapshot, TradeQueryCriteria.Default);

            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result[0].Entry.Identity.Value, Is.EqualTo("Steel"));
        }

        [Test]
        public void Execute_NullCriteria_UsesDefaultCriteria()
        {
            TraderSnapshot trader = CreateTrader(
                "Trader:1",
                1,
                "Alpha",
                SnapshotAvailability.Available,
                new[]
                {
                    CreateItem("Steel", "Steel"),
                    CreateItem("Gold", "Gold")
                });

            IReadOnlyList<TradeQueryEntry> result = TradeSnapshotQuery.Execute(trader, null);

            Assert.That(
                result.Select(row => row.Entry.Label),
                Is.EqualTo(
                    new[]
                    {
                        "Gold",
                        "Steel"
                    }));
        }

        [Test]
        public void Execute_ReturnedCollectionCannotBeModified()
        {
            TraderSnapshot trader = CreateTrader(
                "Trader:1",
                1,
                "Alpha",
                SnapshotAvailability.Available,
                new[]
                {
                    CreateItem("Steel", "Steel")
                });

            IReadOnlyList<TradeQueryEntry> result = TradeSnapshotQuery.Execute(trader, TradeQueryCriteria.Default);

            var mutableView = (IList<TradeQueryEntry>)result;

            Assert.That(
                (Action)(() => mutableView.Add(new TradeQueryEntry(trader, CreateItem("Gold", "Gold")))),
                Throws.TypeOf<NotSupportedException>());
        }

        [Test]
        public void Execute_DoesNotMutateSnapshotOrder()
        {
            TradeEntrySnapshot steel = CreateItem("Steel", "Steel");
            TradeEntrySnapshot gold = CreateItem("Gold", "Gold");

            TraderSnapshot beta = CreateTrader(
                "Trader:2",
                2,
                "Beta",
                SnapshotAvailability.Available,
                new[]
                {
                    steel,
                    gold
                });

            TraderSnapshot alpha = CreateTrader(
                "Trader:1",
                1,
                "Alpha",
                SnapshotAvailability.Available,
                new[]
                {
                    CreateItem("Medicine", "Medicine")
                });

            TradeInventorySnapshot snapshot = CreateInventory(SnapshotAvailability.Available, beta, alpha);

            _ = TradeSnapshotQuery.Execute(snapshot, TradeQueryCriteria.Default);

            Assert.That(snapshot.Traders[0], Is.SameAs(beta));
            Assert.That(snapshot.Traders[1], Is.SameAs(alpha));
            Assert.That(beta.Entries[0], Is.SameAs(steel));
            Assert.That(beta.Entries[1], Is.SameAs(gold));
        }

        [Test]
        public void Execute_NullSnapshots_Throw()
        {
            Assert.That(
                (Action)(() =>
                {
                    _ = TradeSnapshotQuery.Execute((TradeInventorySnapshot)null, TradeQueryCriteria.Default);
                }),
                Throws.TypeOf<ArgumentNullException>());

            Assert.That(
                (Action)(() => { _ = TradeSnapshotQuery.Execute((TraderSnapshot)null, TradeQueryCriteria.Default); }),
                Throws.TypeOf<ArgumentNullException>());
        }

        [Test]
        public void PublicQueryContracts_DoNotExposeRuntimeOrUiTypes()
        {
            foreach (Type contractType in _publicContractTypes)
            {
                foreach (PropertyInfo property in contractType.GetProperties(
                             BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public |
                             BindingFlags.DeclaredOnly))
                {
                    AssertTypeIsAllowed(contractType, $"property '{property.Name}'", property.PropertyType);
                }

                foreach (ConstructorInfo constructor in contractType.GetConstructors(
                             BindingFlags.Instance | BindingFlags.Public))
                {
                    foreach (ParameterInfo parameter in constructor.GetParameters())
                    {
                        AssertTypeIsAllowed(
                            contractType,
                            $"constructor parameter '{parameter.Name}'",
                            parameter.ParameterType);
                    }
                }

                foreach (MethodInfo method in contractType.GetMethods(
                             BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public |
                             BindingFlags.DeclaredOnly))
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

        private static TradeInventorySnapshot CreateInventory(
            SnapshotAvailability availability,
            params TraderSnapshot[] traders)
        {
            return new TradeInventorySnapshot(availability, 1000, 10, null, traders);
        }

        private static TraderSnapshot CreateTrader(
            string traderId,
            int settlementId,
            string settlementLabel,
            SnapshotAvailability availability,
            IReadOnlyList<TradeEntrySnapshot> entries,
            TradeCurrencySnapshot currency = null,
            string traderLabel = "Trader",
            TradeDistance? distance = null,
            TradeRestock? restock = null)
        {
            return new TraderSnapshot(
                new TraderIdentity(traderId),
                new SettlementIdentity(settlementId),
                traderLabel,
                settlementLabel,
                availability,
                distance ?? TradeDistance.Reachable(10),
                restock ?? TradeRestock.Scheduled(2000),
                entries,
                currency);
        }

        private static TradeEntrySnapshot CreateItem(
            string identity,
            string label,
            TradeCategoryMembership membership = TradeCategoryMembership.Items,
            string definitionName = null,
            int count = 1,
            TradePrice? price = null)
        {
            return new TradeEntrySnapshot(
                new TradeEntryIdentity(identity),
                TradeEntryKind.Item,
                membership,
                definitionName ?? identity,
                label,
                count,
                price ?? TradePrice.Negotiated(10f));
        }

        private static TradeEntrySnapshot CreatePawn(string identity, string label)
        {
            return new TradeEntrySnapshot(
                new TradeEntryIdentity(identity),
                TradeEntryKind.Pawn,
                TradeCategoryMembership.None,
                "Human",
                label,
                1,
                TradePrice.MarketValueFallback(1000f));
        }
    }
}