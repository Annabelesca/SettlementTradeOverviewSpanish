using NUnit.Framework;
using SettlementTradeOverview.Domain.Eligibility;
using SettlementTradeOverview.Settings;

namespace SettlementTradeOverview.Tests.Settings
{
    [TestFixture]
    public sealed class SettlementTradeOverviewSettingsValuesTests
    {
        [Test]
        public void Constructor_UsesAcceptedBaselineDefaults()
        {
            var values = new SettlementTradeOverviewSettingsValues();

            Assert.That(values.ShowGlobalOverviewTab, Is.True);
            Assert.That(values.RequirePoweredCommsConsole, Is.True);
            Assert.That(values.RequireIndustrialTechnology, Is.True);
            Assert.That(values.LimitMaximumDistance, Is.True);
            Assert.That(values.MaximumDistanceInTiles, Is.EqualTo(40));
            Assert.That(values.RequireReachable, Is.True);
            Assert.That(values.RequireRoyaltyTradePermission, Is.False);
        }

        [Test]
        public void Normalize_NegativeMaximumDistance_RestoresDefault()
        {
            var values = new SettlementTradeOverviewSettingsValues
            {
                MaximumDistanceInTiles = -1
            };

            SettlementTradeOverviewSettingsPolicy.Normalize(values);

            Assert.That(
                values.MaximumDistanceInTiles,
                Is.EqualTo(SettlementTradeOverviewSettingsValues.DefaultMaximumDistanceInTiles));
        }

        [Test]
        public void Normalize_ZeroMaximumDistance_RemainsValid()
        {
            var values = new SettlementTradeOverviewSettingsValues
            {
                MaximumDistanceInTiles = 0
            };

            SettlementTradeOverviewSettingsPolicy.Normalize(values);

            Assert.That(values.MaximumDistanceInTiles, Is.Zero);
        }

        [Test]
        public void Normalize_MaximumAllowedDistance_RemainsValid()
        {
            var values = new SettlementTradeOverviewSettingsValues
            {
                MaximumDistanceInTiles = SettlementTradeOverviewSettingsValues.MaximumAllowedDistanceInTiles
            };

            SettlementTradeOverviewSettingsPolicy.Normalize(values);

            Assert.That(
                values.MaximumDistanceInTiles,
                Is.EqualTo(SettlementTradeOverviewSettingsValues.MaximumAllowedDistanceInTiles));
        }

        [Test]
        public void Normalize_DistanceAboveMaximum_ClampsToMaximum()
        {
            var values = new SettlementTradeOverviewSettingsValues
            {
                MaximumDistanceInTiles = SettlementTradeOverviewSettingsValues.MaximumAllowedDistanceInTiles + 1
            };

            SettlementTradeOverviewSettingsPolicy.Normalize(values);

            Assert.That(
                values.MaximumDistanceInTiles,
                Is.EqualTo(SettlementTradeOverviewSettingsValues.MaximumAllowedDistanceInTiles));
        }

        [Test]
        public void Normalize_IntMaximumDistance_ClampsToMaximum()
        {
            var values = new SettlementTradeOverviewSettingsValues
            {
                MaximumDistanceInTiles = int.MaxValue
            };

            SettlementTradeOverviewSettingsPolicy.Normalize(values);

            Assert.That(
                values.MaximumDistanceInTiles,
                Is.EqualTo(SettlementTradeOverviewSettingsValues.MaximumAllowedDistanceInTiles));
        }

        [Test]
        public void CreateEligibilityCriteria_DistanceAboveMaximum_UsesClampedValue()
        {
            var values = new SettlementTradeOverviewSettingsValues
            {
                LimitMaximumDistance = true,
                MaximumDistanceInTiles = SettlementTradeOverviewSettingsValues.MaximumAllowedDistanceInTiles + 1
            };

            SettlementEligibilityCriteria criteria =
                SettlementTradeOverviewSettingsPolicy.CreateEligibilityCriteria(values);

            Assert.That(
                values.MaximumDistanceInTiles,
                Is.EqualTo(SettlementTradeOverviewSettingsValues.MaximumAllowedDistanceInTiles));

            Assert.That(
                criteria.MaximumDistanceInTiles,
                Is.EqualTo(SettlementTradeOverviewSettingsValues.MaximumAllowedDistanceInTiles));
        }

        [Test]
        public void DisabledDistanceFilter_PreservesConfiguredDistanceValue()
        {
            var values = new SettlementTradeOverviewSettingsValues
            {
                LimitMaximumDistance = false,
                MaximumDistanceInTiles = 75
            };

            SettlementEligibilityCriteria criteria =
                SettlementTradeOverviewSettingsPolicy.CreateEligibilityCriteria(values);

            Assert.That(values.MaximumDistanceInTiles, Is.EqualTo(75));
            Assert.That(criteria.MaximumDistanceInTiles, Is.Null);
        }

        [Test]
        public void DisabledDistanceFilter_DistanceAboveMaximum_IsClampedButCriteriaHasNoMaximum()
        {
            var values = new SettlementTradeOverviewSettingsValues
            {
                LimitMaximumDistance = false,
                MaximumDistanceInTiles = SettlementTradeOverviewSettingsValues.MaximumAllowedDistanceInTiles + 1
            };

            SettlementEligibilityCriteria criteria =
                SettlementTradeOverviewSettingsPolicy.CreateEligibilityCriteria(values);

            Assert.That(
                values.MaximumDistanceInTiles,
                Is.EqualTo(SettlementTradeOverviewSettingsValues.MaximumAllowedDistanceInTiles));

            Assert.That(criteria.MaximumDistanceInTiles, Is.Null);
        }
    }
}