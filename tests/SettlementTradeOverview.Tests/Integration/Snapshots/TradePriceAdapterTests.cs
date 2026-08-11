using NUnit.Framework;
using SettlementTradeOverview.Domain.Snapshots;
using SettlementTradeOverview.Integration.Snapshots;

namespace SettlementTradeOverview.Tests.Integration.Snapshots
{
    [TestFixture]
    public sealed class TradePriceAdapterTests
    {
        [Test]
        public void FromNegotiatedValue_ValidValue_ReturnsNegotiatedPrice()
        {
            TradePrice result = TradePriceStateResolver.FromNegotiatedValue(12.5f);

            Assert.That(result.State, Is.EqualTo(TradePriceState.Negotiated));
            Assert.That(result.Value, Is.EqualTo(12.5f));
        }

        [Test]
        public void FromMarketValue_ValidValue_ReturnsMarketValueFallback()
        {
            TradePrice result = TradePriceStateResolver.FromMarketValue(7.25f);

            Assert.That(result.State, Is.EqualTo(TradePriceState.MarketValueFallback));
            Assert.That(result.Value, Is.EqualTo(7.25f));
        }

        [Test]
        public void FromNegotiatedValue_MissingValue_ReturnsUnavailable()
        {
            TradePrice result = TradePriceStateResolver.FromNegotiatedValue(null);

            Assert.That(result.State, Is.EqualTo(TradePriceState.Unavailable));
            Assert.That(result.HasValue, Is.False);
        }

        [Test]
        public void FromMarketValue_MissingValue_ReturnsUnavailable()
        {
            TradePrice result = TradePriceStateResolver.FromMarketValue(null);

            Assert.That(result.State, Is.EqualTo(TradePriceState.Unavailable));
            Assert.That(result.HasValue, Is.False);
        }

        [TestCase(-1f)]
        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        [TestCase(float.NegativeInfinity)]
        public void FromNegotiatedValue_InvalidValue_ReturnsUnavailable(float value)
        {
            TradePrice result = TradePriceStateResolver.FromNegotiatedValue(value);

            Assert.That(result.State, Is.EqualTo(TradePriceState.Unavailable));
            Assert.That(result.HasValue, Is.False);
        }

        [TestCase(-1f)]
        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        [TestCase(float.NegativeInfinity)]
        public void FromMarketValue_InvalidValue_ReturnsUnavailable(float value)
        {
            TradePrice result = TradePriceStateResolver.FromMarketValue(value);

            Assert.That(result.State, Is.EqualTo(TradePriceState.Unavailable));
            Assert.That(result.HasValue, Is.False);
        }

        [Test]
        public void NegotiatedAndFallbackValues_RemainDistinctStates()
        {
            TradePrice negotiated = TradePriceStateResolver.FromNegotiatedValue(10f);
            TradePrice fallback = TradePriceStateResolver.FromMarketValue(10f);

            Assert.That(negotiated.State, Is.EqualTo(TradePriceState.Negotiated));
            Assert.That(fallback.State, Is.EqualTo(TradePriceState.MarketValueFallback));
        }
    }
}