using NUnit.Framework;
using SettlementTradeOverview.Domain.Snapshots;
using SettlementTradeOverview.Integration.Snapshots;

namespace SettlementTradeOverview.Tests.Integration.Snapshots
{
    [TestFixture]
    public sealed class TradeRestockAdapterTests
    {
        [Test]
        public void Resolve_ValidTickWithoutMoment_ReturnsScheduled()
        {
            TradeRestock result = TradeRestockStateResolver.Resolve(true, 120000);

            Assert.That(result.State, Is.EqualTo(TradeRestockState.Scheduled));
            Assert.That(result.NextRestockTick, Is.EqualTo(120000));
            Assert.That(result.HasExpectedMoment, Is.False);
        }

        [Test]
        public void Resolve_ValidTickWithMoment_PreservesExpectedMoment()
        {
            var moment = new TradeRestockMoment(250000, 42.5f, -18.25f);

            TradeRestock result = TradeRestockStateResolver.Resolve(true, 120000, moment);

            Assert.That(result.State, Is.EqualTo(TradeRestockState.Scheduled));
            Assert.That(result.ExpectedMoment, Is.EqualTo(moment));
        }

        [Test]
        public void Resolve_VanillaPendingSentinel_ReturnsPendingGenerationWithoutMoment()
        {
            var moment = new TradeRestockMoment(250000, 42.5f, -18.25f);

            TradeRestock result = TradeRestockStateResolver.Resolve(true, -1, moment);

            Assert.That(result.State, Is.EqualTo(TradeRestockState.PendingGeneration));
            Assert.That(result.HasNextRestockTick, Is.False);
            Assert.That(result.HasExpectedMoment, Is.False);
        }

        [Test]
        public void Resolve_MissingProvider_ReturnsUnavailableWithoutMoment()
        {
            var moment = new TradeRestockMoment(250000, 42.5f, -18.25f);

            TradeRestock result = TradeRestockStateResolver.Resolve(false, 120000, moment);

            Assert.That(result.State, Is.EqualTo(TradeRestockState.Unavailable));
            Assert.That(result.HasExpectedMoment, Is.False);
        }

        [Test]
        public void Resolve_UnexpectedNegativeTick_ReturnsUnavailableWithoutMoment()
        {
            var moment = new TradeRestockMoment(250000, 42.5f, -18.25f);

            TradeRestock result = TradeRestockStateResolver.Resolve(true, -2, moment);

            Assert.That(result.State, Is.EqualTo(TradeRestockState.Unavailable));
            Assert.That(result.HasExpectedMoment, Is.False);
        }
    }
}