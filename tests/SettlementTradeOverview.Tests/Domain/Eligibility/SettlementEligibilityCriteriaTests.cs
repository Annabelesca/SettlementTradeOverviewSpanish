using System;
using NUnit.Framework;
using SettlementTradeOverview.Domain.Eligibility;

namespace SettlementTradeOverview.Tests.Domain.Eligibility
{
    [TestFixture]
    public sealed class SettlementEligibilityCriteriaTests
    {
        [Test]
        public void Default_UsesAcceptedFirstReleaseValues()
        {
            SettlementEligibilityCriteria criteria = SettlementEligibilityCriteria.Default;

            Assert.That(criteria.RequirePoweredCommsConsole, Is.True);

            Assert.That(criteria.MinimumTechnologyLevel, Is.EqualTo(SettlementTechnologyLevel.Industrial));

            Assert.That(criteria.MaximumDistanceInTiles, Is.EqualTo(40));
            Assert.That(criteria.RequireReachable, Is.True);
            Assert.That(criteria.RequireRoyaltyTradePermission, Is.False);
        }

        [Test]
        public void Constructor_NullValues_DisableTechnologyAndDistanceFilters()
        {
            var criteria = new SettlementEligibilityCriteria(
                minimumTechnologyLevel: null,
                maximumDistanceInTiles: null);

            Assert.That(criteria.MinimumTechnologyLevel, Is.Null);
            Assert.That(criteria.MaximumDistanceInTiles, Is.Null);
        }

        [Test]
        public void Constructor_PreservesExplicitValues()
        {
            var criteria = new SettlementEligibilityCriteria(
                requirePoweredCommsConsole: false,
                minimumTechnologyLevel: SettlementTechnologyLevel.Medieval,
                maximumDistanceInTiles: 12,
                requireReachable: false,
                requireRoyaltyTradePermission: true);

            Assert.That(criteria.RequirePoweredCommsConsole, Is.False);

            Assert.That(criteria.MinimumTechnologyLevel, Is.EqualTo(SettlementTechnologyLevel.Medieval));

            Assert.That(criteria.MaximumDistanceInTiles, Is.EqualTo(12));
            Assert.That(criteria.RequireReachable, Is.False);
            Assert.That(criteria.RequireRoyaltyTradePermission, Is.True);
        }

        [Test]
        public void Constructor_NegativeMaximumDistance_Throws()
        {
            Assert.That(
                (Action)(() => { _ = new SettlementEligibilityCriteria(maximumDistanceInTiles: -1); }),
                Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void Constructor_UnavailableMinimumTechnologyLevel_Throws()
        {
            Assert.That(
                (Action)(() =>
                {
                    _ = new SettlementEligibilityCriteria(
                        minimumTechnologyLevel: SettlementTechnologyLevel.Unavailable);
                }),
                Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void Constructor_InvalidMinimumTechnologyLevel_Throws()
        {
            Assert.That(
                (Action)(() =>
                {
                    _ = new SettlementEligibilityCriteria(minimumTechnologyLevel: (SettlementTechnologyLevel)100);
                }),
                Throws.TypeOf<ArgumentOutOfRangeException>());
        }
    }
}