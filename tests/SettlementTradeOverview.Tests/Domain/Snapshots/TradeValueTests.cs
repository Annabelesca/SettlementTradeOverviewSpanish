using System;
using NUnit.Framework;
using SettlementTradeOverview.Domain.Snapshots;

namespace SettlementTradeOverview.Tests.Domain.Snapshots
{
    [TestFixture]
    public sealed class TradeValueTests
    {
        [Test]
        public void TradePrice_Negotiated_PreservesStateAndValue()
        {
            var price = TradePrice.Negotiated(125.5f);

            Assert.That(price.State, Is.EqualTo(TradePriceState.Negotiated));
            Assert.That(price.HasValue, Is.True);
            Assert.That(price.Value, Is.EqualTo(125.5f));
        }

        [Test]
        public void TradePrice_MarketValueFallback_IsDistinctFromNegotiated()
        {
            var price = TradePrice.MarketValueFallback(90f);

            Assert.That(price.State, Is.EqualTo(TradePriceState.MarketValueFallback));
            Assert.That(price.HasValue, Is.True);
            Assert.That(price.Value, Is.EqualTo(90f));
        }

        [Test]
        public void TradePrice_Unavailable_DoesNotContainValue()
        {
            TradePrice price = TradePrice.Unavailable;

            Assert.That(price.State, Is.EqualTo(TradePriceState.Unavailable));
            Assert.That(price.HasValue, Is.False);
            Assert.That(price.Value, Is.Null);
        }

        [Test]
        public void TradePrice_InvalidValues_Throw()
        {
            Assert.That((Action)(() => TradePrice.Negotiated(-1f)), Throws.TypeOf<ArgumentOutOfRangeException>());

            Assert.That((Action)(() => TradePrice.Negotiated(float.NaN)), Throws.TypeOf<ArgumentOutOfRangeException>());

            Assert.That(
                (Action)(() => TradePrice.MarketValueFallback(float.PositiveInfinity)),
                Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void TradeDistance_Reachable_PreservesTileDistanceAndRouteState()
        {
            var distance = TradeDistance.Reachable(37);

            Assert.That(distance.RouteState, Is.EqualTo(TradeRouteState.Reachable));
            Assert.That(distance.HasTileDistance, Is.True);
            Assert.That(distance.Tiles, Is.EqualTo(37));
        }

        [Test]
        public void TradeDistance_Unreachable_PreservesTileDistanceAndRouteState()
        {
            var distance = TradeDistance.Unreachable(37);

            Assert.That(distance.RouteState, Is.EqualTo(TradeRouteState.Unreachable));
            Assert.That(distance.HasTileDistance, Is.True);
            Assert.That(distance.Tiles, Is.EqualTo(37));
        }

        [Test]
        public void TradeDistance_UnavailableRoute_PreservesTileDistance()
        {
            var distance = TradeDistance.WithUnavailableRoute(37);

            Assert.That(distance.RouteState, Is.EqualTo(TradeRouteState.Unavailable));
            Assert.That(distance.HasTileDistance, Is.True);
            Assert.That(distance.Tiles, Is.EqualTo(37));
        }

        [Test]
        public void TradeDistance_Unavailable_DoesNotContainTileDistance()
        {
            TradeDistance distance = TradeDistance.Unavailable;

            Assert.That(distance.RouteState, Is.EqualTo(TradeRouteState.Unavailable));
            Assert.That(distance.HasTileDistance, Is.False);
            Assert.That(distance.Tiles, Is.Null);
        }

        [Test]
        public void TradeDistance_NegativeTiles_ThrowForEveryKnownDistanceFactory()
        {
            Assert.That((Action)(() => TradeDistance.Reachable(-1)), Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That((Action)(() => TradeDistance.Unreachable(-1)), Throws.TypeOf<ArgumentOutOfRangeException>());

            Assert.That(
                (Action)(() => TradeDistance.WithUnavailableRoute(-1)),
                Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void TradeRestockMoment_Constructor_PreservesRuntimeFreeValues()
        {
            var moment = new TradeRestockMoment(250000, 42.5f, -18.25f);

            Assert.That(moment.AbsoluteTick, Is.EqualTo(250000));
            Assert.That(moment.Longitude, Is.EqualTo(42.5f));
            Assert.That(moment.Latitude, Is.EqualTo(-18.25f));
        }

        [Test]
        public void TradeRestockMoment_InvalidValues_Throw()
        {
            Assert.That(
                (Action)(() => { _ = new TradeRestockMoment(-1, 0f, 0f); }),
                Throws.TypeOf<ArgumentOutOfRangeException>());

            Assert.That(
                (Action)(() => { _ = new TradeRestockMoment(0, float.NaN, 0f); }),
                Throws.TypeOf<ArgumentOutOfRangeException>());

            Assert.That(
                (Action)(() => { _ = new TradeRestockMoment(0, float.PositiveInfinity, 0f); }),
                Throws.TypeOf<ArgumentOutOfRangeException>());

            Assert.That(
                (Action)(() => { _ = new TradeRestockMoment(0, 0f, float.NegativeInfinity); }),
                Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void TradeRestock_Scheduled_PreservesAbsoluteTickWithoutExpectedMoment()
        {
            var restock = TradeRestock.Scheduled(120000);

            Assert.That(restock.State, Is.EqualTo(TradeRestockState.Scheduled));
            Assert.That(restock.HasNextRestockTick, Is.True);
            Assert.That(restock.NextRestockTick, Is.EqualTo(120000));
            Assert.That(restock.HasExpectedMoment, Is.False);
            Assert.That(restock.ExpectedMoment, Is.Null);
        }

        [Test]
        public void TradeRestock_Scheduled_PreservesExpectedMoment()
        {
            var moment = new TradeRestockMoment(250000, 42.5f, -18.25f);
            var restock = TradeRestock.Scheduled(120000, moment);

            Assert.That(restock.State, Is.EqualTo(TradeRestockState.Scheduled));
            Assert.That(restock.HasExpectedMoment, Is.True);
            Assert.That(restock.ExpectedMoment, Is.EqualTo(moment));
        }

        [Test]
        public void TradeRestock_PendingGenerationAndUnavailable_AreDistinctAndContainNoMoment()
        {
            TradeRestock pending = TradeRestock.PendingGeneration;
            TradeRestock unavailable = TradeRestock.Unavailable;

            Assert.That(pending.State, Is.EqualTo(TradeRestockState.PendingGeneration));
            Assert.That(unavailable.State, Is.EqualTo(TradeRestockState.Unavailable));
            Assert.That(pending.HasNextRestockTick, Is.False);
            Assert.That(unavailable.HasNextRestockTick, Is.False);
            Assert.That(pending.HasExpectedMoment, Is.False);
            Assert.That(unavailable.HasExpectedMoment, Is.False);
            Assert.That(pending.ExpectedMoment, Is.Null);
            Assert.That(unavailable.ExpectedMoment, Is.Null);
        }

        [Test]
        public void TradeRestock_NegativeTick_Throws()
        {
            Assert.That((Action)(() => TradeRestock.Scheduled(-1)), Throws.TypeOf<ArgumentOutOfRangeException>());
        }
    }
}