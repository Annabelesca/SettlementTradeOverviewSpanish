using System;
using NUnit.Framework;
using SettlementTradeOverview.Cache;
using SettlementTradeOverview.Domain.Categories;
using SettlementTradeOverview.Domain.Eligibility;
using SettlementTradeOverview.Domain.Identity;
using SettlementTradeOverview.Domain.Snapshots;

namespace SettlementTradeOverview.Tests.Cache
{
    [TestFixture]
    public sealed class TradeInventorySnapshotCacheTests
    {
        [Test]
        public void Constructor_StartsNotLoadedWithoutSnapshot()
        {
            var cache = new TradeInventorySnapshotCache();

            Assert.That(cache.State, Is.EqualTo(TradeInventorySnapshotCacheState.NotLoaded));

            Assert.That(cache.HasSnapshot, Is.False);
            Assert.That(cache.Snapshot, Is.Null);
        }

        [Test]
        public void TryGetReusableSnapshot_NotLoaded_ReturnsFalse()
        {
            var cache = new TradeInventorySnapshotCache();

            bool reused = cache.TryGetReusableSnapshot(CreateReuseKey(1), out TradeInventorySnapshot snapshot);

            Assert.That(reused, Is.False);
            Assert.That(snapshot, Is.Null);
            Assert.That(cache.State, Is.EqualTo(TradeInventorySnapshotCacheState.NotLoaded));
        }

        [Test]
        public void TryGetReusableSnapshot_MatchingReuseKey_ReturnsSameSnapshotWithoutChangingState()
        {
            var cache = new TradeInventorySnapshotCache();
            TradeInventorySnapshot expected = CreateAvailableSnapshot(100);

            cache.GetOrBuild(CreateKey(1), () => expected);
            TradeInventorySnapshotCacheState stateBeforeReuse = cache.State;

            bool reused = cache.TryGetReusableSnapshot(CreateReuseKey(1), out TradeInventorySnapshot snapshot);

            Assert.That(reused, Is.True);
            Assert.That(snapshot, Is.SameAs(expected));
            Assert.That(cache.State, Is.EqualTo(stateBeforeReuse));
        }

        [Test]
        public void TryGetReusableSnapshot_DoesNotInvokeSnapshotFactory()
        {
            var cache = new TradeInventorySnapshotCache();
            var factoryCallCount = 0;

            cache.GetOrBuild(
                CreateKey(1),
                () =>
                {
                    factoryCallCount++;
                    return CreateAvailableSnapshot(100);
                });

            bool reused = cache.TryGetReusableSnapshot(CreateReuseKey(1), out _);

            Assert.That(reused, Is.True);
            Assert.That(factoryCallCount, Is.EqualTo(1));
        }

        [Test]
        public void TryGetReusableSnapshot_DifferentFullTraderIdentitiesStillReuseSameContext()
        {
            var cache = new TradeInventorySnapshotCache();
            TradeInventorySnapshotReuseKey reuseKey = CreateReuseKey(1);

            var fullKey = TradeInventorySnapshotCacheKey.CreateAvailableContext(
                reuseKey,
                new[]
                {
                    new TraderIdentity("Settlement:1"),
                    new TraderIdentity("Settlement:2")
                },
                1);

            TradeInventorySnapshot expected = CreateAvailableSnapshot(100);
            cache.GetOrBuild(fullKey, () => expected);

            bool reused = cache.TryGetReusableSnapshot(CreateReuseKey(1), out TradeInventorySnapshot snapshot);

            Assert.That(reused, Is.True);
            Assert.That(snapshot, Is.SameAs(expected));
        }

        [Test]
        public void TryGetReusableSnapshot_IncompatibleReuseKey_ReturnsFalse()
        {
            var cache = new TradeInventorySnapshotCache();
            cache.GetOrBuild(CreateKey(1), () => CreateAvailableSnapshot(100));

            bool reused = cache.TryGetReusableSnapshot(CreateReuseKey(2), out TradeInventorySnapshot snapshot);

            Assert.That(reused, Is.False);
            Assert.That(snapshot, Is.Null);
            Assert.That(cache.State, Is.EqualTo(TradeInventorySnapshotCacheState.Available));
        }

        [Test]
        public void TryGetReusableSnapshot_AfterInvalidate_ReturnsFalse()
        {
            var cache = new TradeInventorySnapshotCache();
            cache.GetOrBuild(CreateKey(1), () => CreateAvailableSnapshot(100));

            cache.Invalidate();

            bool reused = cache.TryGetReusableSnapshot(CreateReuseKey(1), out TradeInventorySnapshot snapshot);

            Assert.That(reused, Is.False);
            Assert.That(snapshot, Is.Null);
            Assert.That(cache.State, Is.EqualTo(TradeInventorySnapshotCacheState.NotLoaded));
        }

        [Test]
        public void GetOrBuild_FirstRequest_InvokesFactoryAndCachesSnapshot()
        {
            var cache = new TradeInventorySnapshotCache();
            TradeInventorySnapshotCacheKey key = CreateKey(1);
            TradeInventorySnapshot expected = CreateAvailableSnapshot(100);
            var factoryCallCount = 0;

            TradeInventorySnapshot result = cache.GetOrBuild(
                key,
                () =>
                {
                    factoryCallCount++;
                    return expected;
                });

            Assert.That(result, Is.SameAs(expected));
            Assert.That(cache.Snapshot, Is.SameAs(expected));
            Assert.That(cache.HasSnapshot, Is.True);

            Assert.That(cache.State, Is.EqualTo(TradeInventorySnapshotCacheState.Available));

            Assert.That(factoryCallCount, Is.EqualTo(1));
        }

        [Test]
        public void GetOrBuild_SameKey_ReusesSnapshotWithoutCallingFactory()
        {
            var cache = new TradeInventorySnapshotCache();
            TradeInventorySnapshotCacheKey key = CreateKey(1);
            TradeInventorySnapshot expected = CreateAvailableSnapshot(100);
            var factoryCallCount = 0;

            TradeInventorySnapshot first = cache.GetOrBuild(
                key,
                () =>
                {
                    factoryCallCount++;
                    return expected;
                });

            TradeInventorySnapshot second = cache.GetOrBuild(
                CreateKey(1),
                () =>
                {
                    factoryCallCount++;
                    return CreateAvailableSnapshot(200);
                });

            Assert.That(second, Is.SameAs(first));
            Assert.That(factoryCallCount, Is.EqualTo(1));
        }

        [Test]
        public void GetOrBuild_DifferentKey_ReplacesPreviousSnapshot()
        {
            var cache = new TradeInventorySnapshotCache();
            TradeInventorySnapshot firstSnapshot = CreateAvailableSnapshot(100);

            TradeInventorySnapshot secondSnapshot = CreateAvailableSnapshot(200);

            cache.GetOrBuild(CreateKey(1), () => firstSnapshot);

            TradeInventorySnapshot result = cache.GetOrBuild(CreateKey(2), () => secondSnapshot);

            Assert.That(result, Is.SameAs(secondSnapshot));
            Assert.That(cache.Snapshot, Is.SameAs(secondSnapshot));
            Assert.That(cache.Snapshot, Is.Not.SameAs(firstSnapshot));
        }

        [Test]
        public void Refresh_SameKey_AlwaysBuildsNewSnapshot()
        {
            var cache = new TradeInventorySnapshotCache();
            TradeInventorySnapshotCacheKey key = CreateKey(1);
            TradeInventorySnapshot firstSnapshot = CreateAvailableSnapshot(100);

            TradeInventorySnapshot secondSnapshot = CreateAvailableSnapshot(200);

            var factoryCallCount = 0;

            cache.GetOrBuild(
                key,
                () =>
                {
                    factoryCallCount++;
                    return firstSnapshot;
                });

            TradeInventorySnapshot result = cache.Refresh(
                CreateKey(1),
                () =>
                {
                    factoryCallCount++;
                    return secondSnapshot;
                });

            Assert.That(result, Is.SameAs(secondSnapshot));
            Assert.That(result, Is.Not.SameAs(firstSnapshot));
            Assert.That(factoryCallCount, Is.EqualTo(2));
        }

        [Test]
        public void Invalidate_ClearsSnapshotAndReturnsToNotLoaded()
        {
            var cache = new TradeInventorySnapshotCache();

            cache.GetOrBuild(CreateKey(1), () => CreateAvailableSnapshot(100));

            cache.Invalidate();

            Assert.That(cache.Snapshot, Is.Null);
            Assert.That(cache.HasSnapshot, Is.False);

            Assert.That(cache.State, Is.EqualTo(TradeInventorySnapshotCacheState.NotLoaded));
        }

        [Test]
        public void BuildFactory_ObservesLoadingStateWithoutStaleSnapshot()
        {
            var cache = new TradeInventorySnapshotCache();
            TradeInventorySnapshot previous = CreateAvailableSnapshot(100);

            cache.GetOrBuild(CreateKey(1), () => previous);

            TradeInventorySnapshot replacement = CreateAvailableSnapshot(200);

            cache.GetOrBuild(
                CreateKey(2),
                () =>
                {
                    Assert.That(cache.Snapshot, Is.Null);
                    Assert.That(cache.HasSnapshot, Is.False);

                    Assert.That(cache.State, Is.EqualTo(TradeInventorySnapshotCacheState.Loading));

                    return replacement;
                });

            Assert.That(cache.Snapshot, Is.SameAs(replacement));
        }

        [TestCase(SnapshotAvailability.Available, (int)TradeInventorySnapshotCacheState.Available)]
        [TestCase(SnapshotAvailability.Empty, (int)TradeInventorySnapshotCacheState.Empty)]
        [TestCase(SnapshotAvailability.Unavailable, (int)TradeInventorySnapshotCacheState.Unavailable)]
        [TestCase(SnapshotAvailability.Partial, (int)TradeInventorySnapshotCacheState.Partial)]
        [TestCase(SnapshotAvailability.Failed, (int)TradeInventorySnapshotCacheState.Failed)]
        public void CompletedBuild_MapsSnapshotAvailabilityToCacheState(
            SnapshotAvailability availability,
            int expectedStateValue)
        {
            var cache = new TradeInventorySnapshotCache();

            TradeInventorySnapshot snapshot = CreateSnapshot(availability, 100);

            cache.GetOrBuild(CreateKey(1), () => snapshot);

            var expectedState = (TradeInventorySnapshotCacheState)expectedStateValue;

            Assert.That(cache.State, Is.EqualTo(expectedState));

            Assert.That(cache.Snapshot, Is.SameAs(snapshot));
        }

        [Test]
        public void FailedSnapshot_IsReusedForEquivalentKey()
        {
            var cache = new TradeInventorySnapshotCache();
            TradeInventorySnapshot failedSnapshot = CreateSnapshot(SnapshotAvailability.Failed, 100);

            var factoryCallCount = 0;

            cache.GetOrBuild(
                CreateKey(1),
                () =>
                {
                    factoryCallCount++;
                    return failedSnapshot;
                });

            TradeInventorySnapshot result = cache.GetOrBuild(
                CreateKey(1),
                () =>
                {
                    factoryCallCount++;

                    return CreateAvailableSnapshot(200);
                });

            Assert.That(result, Is.SameAs(failedSnapshot));
            Assert.That(factoryCallCount, Is.EqualTo(1));
        }

        [Test]
        public void PartialSnapshot_IsPreservedAndReused()
        {
            var cache = new TradeInventorySnapshotCache();

            TradeInventorySnapshot partialSnapshot = CreateSnapshot(SnapshotAvailability.Partial, 100);

            TradeInventorySnapshot first = cache.GetOrBuild(CreateKey(1), () => partialSnapshot);

            TradeInventorySnapshot second = cache.GetOrBuild(CreateKey(1), () => CreateAvailableSnapshot(200));

            Assert.That(first, Is.SameAs(partialSnapshot));
            Assert.That(second, Is.SameAs(partialSnapshot));
            Assert.That(second.TraderCount, Is.EqualTo(1));
            Assert.That(second.EntryCount, Is.EqualTo(1));
        }

        [Test]
        public void FactoryException_RemovesStaleSnapshotAndSetsFailedState()
        {
            var cache = new TradeInventorySnapshotCache();

            cache.GetOrBuild(CreateKey(1), () => CreateAvailableSnapshot(100));

            Assert.That(
                (Action)(() =>
                {
                    _ = cache.GetOrBuild(CreateKey(2), () => throw new InvalidOperationException("Build failed."));
                }),
                Throws.TypeOf<InvalidOperationException>());

            Assert.That(cache.Snapshot, Is.Null);
            Assert.That(cache.HasSnapshot, Is.False);

            Assert.That(cache.State, Is.EqualTo(TradeInventorySnapshotCacheState.Failed));
        }

        [Test]
        public void FactoryException_AllowsSubsequentSuccessfulBuild()
        {
            var cache = new TradeInventorySnapshotCache();
            TradeInventorySnapshotCacheKey key = CreateKey(1);

            Assert.That(
                (Action)(() =>
                {
                    _ = cache.GetOrBuild(key, () => throw new InvalidOperationException("Build failed."));
                }),
                Throws.TypeOf<InvalidOperationException>());

            TradeInventorySnapshot expected = CreateAvailableSnapshot(200);

            TradeInventorySnapshot result = cache.GetOrBuild(key, () => expected);

            Assert.That(result, Is.SameAs(expected));

            Assert.That(cache.State, Is.EqualTo(TradeInventorySnapshotCacheState.Available));
        }

        [Test]
        public void NullFactoryResult_IsTreatedAsFailedBuild()
        {
            var cache = new TradeInventorySnapshotCache();

            Assert.That(
                (Action)(() => { _ = cache.GetOrBuild(CreateKey(1), () => null); }),
                Throws.TypeOf<InvalidOperationException>());

            Assert.That(cache.Snapshot, Is.Null);

            Assert.That(cache.State, Is.EqualTo(TradeInventorySnapshotCacheState.Failed));
        }

        private static TradeInventorySnapshotCacheKey CreateKey(int mapId)
        {
            return TradeInventorySnapshotCacheKey.CreateAvailableContext(
                CreateReuseKey(mapId),
                new[]
                {
                    new TraderIdentity("Settlement:1")
                },
                0);
        }

        private static TradeInventorySnapshotReuseKey CreateReuseKey(int mapId)
        {
            return TradeInventorySnapshotReuseKey.CreateAvailableContext(
                mapId,
                10,
                null,
                SettlementEligibilityCriteria.Default,
                hasPoweredCommsConsole: true,
                isRoyaltyActive: false);
        }

        private static TradeInventorySnapshot CreateAvailableSnapshot(int capturedAtTick)
        {
            return CreateSnapshot(SnapshotAvailability.Available, capturedAtTick);
        }

        private static TradeInventorySnapshot CreateSnapshot(SnapshotAvailability availability, int capturedAtTick)
        {
            switch (availability)
            {
                case SnapshotAvailability.Available:
                case SnapshotAvailability.Partial:
                    return new TradeInventorySnapshot(
                        availability,
                        capturedAtTick,
                        10,
                        null,
                        new[]
                        {
                            CreateTrader(
                                availability == SnapshotAvailability.Partial
                                    ? SnapshotAvailability.Partial
                                    : SnapshotAvailability.Available)
                        });

                case SnapshotAvailability.Empty:
                    return new TradeInventorySnapshot(
                        SnapshotAvailability.Empty,
                        capturedAtTick,
                        10,
                        null,
                        Array.Empty<TraderSnapshot>());

                case SnapshotAvailability.Unavailable:
                    return new TradeInventorySnapshot(
                        SnapshotAvailability.Unavailable,
                        capturedAtTick,
                        null,
                        null,
                        Array.Empty<TraderSnapshot>());

                case SnapshotAvailability.Failed:
                    return new TradeInventorySnapshot(
                        SnapshotAvailability.Failed,
                        capturedAtTick,
                        null,
                        null,
                        Array.Empty<TraderSnapshot>());

                default:
                    throw new ArgumentOutOfRangeException(nameof(availability));
            }
        }

        private static TraderSnapshot CreateTrader(SnapshotAvailability availability)
        {
            return new TraderSnapshot(
                new TraderIdentity("Settlement:1"),
                new SettlementIdentity(1),
                "Trader",
                "Settlement",
                availability,
                TradeDistance.Reachable(10),
                TradeRestock.Scheduled(1000),
                new[]
                {
                    new TradeEntrySnapshot(
                        new TradeEntryIdentity("Thing:Steel"),
                        TradeEntryKind.Item,
                        TradeCategoryMembership.Items,
                        "Steel",
                        "Steel",
                        10,
                        TradePrice.Negotiated(2f))
                },
                null);
        }
    }
}