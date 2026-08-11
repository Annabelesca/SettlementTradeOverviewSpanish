using System;
using NUnit.Framework;
using SettlementTradeOverview.Domain.Eligibility;
using SettlementTradeOverview.Settings;

namespace SettlementTradeOverview.Tests.Settings
{
    [TestFixture]
    public sealed class SettlementTradeOverviewSettingsPolicyTests
    {
        [Test]
        public void CreateEligibilityCriteria_DefaultSettings_MatchesCurrentBaseline()
        {
            SettlementEligibilityCriteria result =
                SettlementTradeOverviewSettingsPolicy.CreateEligibilityCriteria(
                    new SettlementTradeOverviewSettingsValues());

            Assert.That(
                SettlementTradeOverviewSettingsPolicy.AreEquivalent(result, SettlementEligibilityCriteria.Default),
                Is.True);
        }

        [Test]
        public void CreateEligibilityCriteria_MapsEverySupportedSetting()
        {
            var values = new SettlementTradeOverviewSettingsValues
            {
                RequirePoweredCommsConsole = false,
                RequireIndustrialTechnology = false,
                LimitMaximumDistance = true,
                MaximumDistanceInTiles = 18,
                RequireReachable = false,
                RequireRoyaltyTradePermission = true
            };

            SettlementEligibilityCriteria result =
                SettlementTradeOverviewSettingsPolicy.CreateEligibilityCriteria(values);

            Assert.That(result.RequirePoweredCommsConsole, Is.False);
            Assert.That(result.MinimumTechnologyLevel, Is.Null);
            Assert.That(result.MaximumDistanceInTiles, Is.EqualTo(18));
            Assert.That(result.RequireReachable, Is.False);
            Assert.That(result.RequireRoyaltyTradePermission, Is.True);
        }

        [Test]
        public void CreateEligibilityCriteria_EnabledTechnologyFilter_UsesIndustrialMinimum()
        {
            var values = new SettlementTradeOverviewSettingsValues
            {
                RequireIndustrialTechnology = true
            };

            SettlementEligibilityCriteria result =
                SettlementTradeOverviewSettingsPolicy.CreateEligibilityCriteria(values);

            Assert.That(result.MinimumTechnologyLevel, Is.EqualTo(SettlementTechnologyLevel.Industrial));
        }

        [Test]
        public void CreateEligibilityCriteria_DisabledDistanceFilter_UsesNoMaximum()
        {
            var values = new SettlementTradeOverviewSettingsValues
            {
                LimitMaximumDistance = false,
                MaximumDistanceInTiles = 18
            };

            SettlementEligibilityCriteria result =
                SettlementTradeOverviewSettingsPolicy.CreateEligibilityCriteria(values);

            Assert.That(result.MaximumDistanceInTiles, Is.Null);
            Assert.That(values.MaximumDistanceInTiles, Is.EqualTo(18));
        }

        [Test]
        public void AreEquivalent_AnyEligibilityDifference_ReturnsFalse()
        {
            SettlementEligibilityCriteria baseline = SettlementEligibilityCriteria.Default;

            SettlementEligibilityCriteria[] alternatives =
            {
                new SettlementEligibilityCriteria(requirePoweredCommsConsole: false),
                new SettlementEligibilityCriteria(minimumTechnologyLevel: null),
                new SettlementEligibilityCriteria(maximumDistanceInTiles: 20),
                new SettlementEligibilityCriteria(requireReachable: false),
                new SettlementEligibilityCriteria(requireRoyaltyTradePermission: true)
            };

            foreach (SettlementEligibilityCriteria alternative in alternatives)
            {
                Assert.That(SettlementTradeOverviewSettingsPolicy.AreEquivalent(baseline, alternative), Is.False);
            }
        }

        [Test]
        public void AreEquivalent_EquivalentValues_ReturnsTrue()
        {
            var first = new SettlementEligibilityCriteria(
                requirePoweredCommsConsole: false,
                minimumTechnologyLevel: null,
                maximumDistanceInTiles: 12,
                requireReachable: false,
                requireRoyaltyTradePermission: true);

            var second = new SettlementEligibilityCriteria(
                requirePoweredCommsConsole: false,
                minimumTechnologyLevel: null,
                maximumDistanceInTiles: 12,
                requireReachable: false,
                requireRoyaltyTradePermission: true);

            Assert.That(SettlementTradeOverviewSettingsPolicy.AreEquivalent(first, second), Is.True);
        }

        [Test]
        public void NullArguments_Throw()
        {
            Assert.That(
                (Action)(() => SettlementTradeOverviewSettingsPolicy.Normalize(null)),
                Throws.TypeOf<ArgumentNullException>());

            Assert.That(
                (Action)(() => SettlementTradeOverviewSettingsPolicy.CreateEligibilityCriteria(null)),
                Throws.TypeOf<ArgumentNullException>());
        }
    }

    [TestFixture]
    [NonParallelizable]
    public sealed class SettlementTradeOverviewSettingsServiceTests
    {
        [SetUp]
        public void SetUp()
        {
            SettlementTradeOverviewSettingsService.Apply(new SettlementTradeOverviewSettingsValues());
        }

        [TearDown]
        public void TearDown()
        {
            SettlementTradeOverviewSettingsService.Apply(new SettlementTradeOverviewSettingsValues());
        }

        [Test]
        public void Apply_EquivalentEligibilitySettings_DoesNotAdvanceRevision()
        {
            int revision = SettlementTradeOverviewSettingsService.EligibilityRevision;

            SettlementTradeOverviewSettingsApplyResult result =
                SettlementTradeOverviewSettingsService.Apply(new SettlementTradeOverviewSettingsValues());

            Assert.That(result.EligibilityChanged, Is.False);
            Assert.That(SettlementTradeOverviewSettingsService.EligibilityRevision, Is.EqualTo(revision));
        }

        [Test]
        public void Apply_EligibilityChange_AdvancesRevisionAndUpdatesCriteria()
        {
            int revision = SettlementTradeOverviewSettingsService.EligibilityRevision;

            var changedValues = new SettlementTradeOverviewSettingsValues
            {
                RequirePoweredCommsConsole = false
            };

            SettlementTradeOverviewSettingsApplyResult result =
                SettlementTradeOverviewSettingsService.Apply(changedValues);

            Assert.That(result.EligibilityChanged, Is.True);
            Assert.That(SettlementTradeOverviewSettingsService.EligibilityRevision, Is.EqualTo(revision + 1));

            Assert.That(
                SettlementTradeOverviewSettingsService.CurrentEligibilityCriteria.RequirePoweredCommsConsole,
                Is.False);
        }

        [Test]
        public void Apply_OnlyTabVisibilityChange_DoesNotAdvanceEligibilityRevision()
        {
            int revision = SettlementTradeOverviewSettingsService.EligibilityRevision;

            var changedValues = new SettlementTradeOverviewSettingsValues
            {
                ShowGlobalOverviewTab = false
            };

            SettlementTradeOverviewSettingsApplyResult result =
                SettlementTradeOverviewSettingsService.Apply(changedValues);

            Assert.That(result.EligibilityChanged, Is.False);
            Assert.That(result.GlobalOverviewTabVisibilityChanged, Is.True);
            Assert.That(SettlementTradeOverviewSettingsService.EligibilityRevision, Is.EqualTo(revision));
            Assert.That(SettlementTradeOverviewSettingsService.ShowGlobalOverviewTab, Is.False);
        }
    }
}