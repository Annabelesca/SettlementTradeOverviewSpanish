using System;
using Verse;

namespace SettlementTradeOverview.Settings
{
    public sealed class SettlementTradeOverviewSettings : ModSettings
    {
        private bool _showGlobalOverviewTab = SettlementTradeOverviewSettingsValues.DefaultShowGlobalOverviewTab;

        private bool _requirePoweredCommsConsole =
            SettlementTradeOverviewSettingsValues.DefaultRequirePoweredCommsConsole;

        private bool _requireIndustrialTechnology =
            SettlementTradeOverviewSettingsValues.DefaultRequireIndustrialTechnology;

        private bool _limitMaximumDistance = SettlementTradeOverviewSettingsValues.DefaultLimitMaximumDistance;

        private int _maximumDistanceInTiles = SettlementTradeOverviewSettingsValues.DefaultMaximumDistanceInTiles;

        private bool _requireReachable = SettlementTradeOverviewSettingsValues.DefaultRequireReachable;

        private bool _requireRoyaltyTradePermission =
            SettlementTradeOverviewSettingsValues.DefaultRequireRoyaltyTradePermission;

        public bool ShowGlobalOverviewTab
        {
            get => _showGlobalOverviewTab;
            set => _showGlobalOverviewTab = value;
        }

        public bool RequirePoweredCommsConsole
        {
            get => _requirePoweredCommsConsole;
            set => _requirePoweredCommsConsole = value;
        }

        public bool RequireIndustrialTechnology
        {
            get => _requireIndustrialTechnology;
            set => _requireIndustrialTechnology = value;
        }

        public bool LimitMaximumDistance
        {
            get => _limitMaximumDistance;
            set => _limitMaximumDistance = value;
        }

        public int MaximumDistanceInTiles
        {
            get => _maximumDistanceInTiles;
            set => _maximumDistanceInTiles = value;
        }

        public bool RequireReachable
        {
            get => _requireReachable;
            set => _requireReachable = value;
        }

        public bool RequireRoyaltyTradePermission
        {
            get => _requireRoyaltyTradePermission;
            set => _requireRoyaltyTradePermission = value;
        }

        internal SettlementTradeOverviewSettingsValues CaptureValues()
        {
            return new SettlementTradeOverviewSettingsValues
            {
                ShowGlobalOverviewTab = ShowGlobalOverviewTab,
                RequirePoweredCommsConsole = RequirePoweredCommsConsole,
                RequireIndustrialTechnology = RequireIndustrialTechnology,
                LimitMaximumDistance = LimitMaximumDistance,
                MaximumDistanceInTiles = MaximumDistanceInTiles,
                RequireReachable = RequireReachable,
                RequireRoyaltyTradePermission = RequireRoyaltyTradePermission
            };
        }

        internal void ApplyValues(SettlementTradeOverviewSettingsValues values)
        {
            if (values == null)
                throw new ArgumentNullException(nameof(values));

            ShowGlobalOverviewTab = values.ShowGlobalOverviewTab;
            RequirePoweredCommsConsole = values.RequirePoweredCommsConsole;
            RequireIndustrialTechnology = values.RequireIndustrialTechnology;
            LimitMaximumDistance = values.LimitMaximumDistance;
            MaximumDistanceInTiles = values.MaximumDistanceInTiles;
            RequireReachable = values.RequireReachable;
            RequireRoyaltyTradePermission = values.RequireRoyaltyTradePermission;
        }

        public override void ExposeData()
        {
            Scribe_Values.Look(
                ref _showGlobalOverviewTab,
                "showGlobalOverviewTab",
                SettlementTradeOverviewSettingsValues.DefaultShowGlobalOverviewTab);

            Scribe_Values.Look(
                ref _requirePoweredCommsConsole,
                "requirePoweredCommsConsole",
                SettlementTradeOverviewSettingsValues.DefaultRequirePoweredCommsConsole);

            Scribe_Values.Look(
                ref _requireIndustrialTechnology,
                "requireIndustrialTechnology",
                SettlementTradeOverviewSettingsValues.DefaultRequireIndustrialTechnology);

            Scribe_Values.Look(
                ref _limitMaximumDistance,
                "limitMaximumDistance",
                SettlementTradeOverviewSettingsValues.DefaultLimitMaximumDistance);

            Scribe_Values.Look(
                ref _maximumDistanceInTiles,
                "maximumDistanceInTiles",
                SettlementTradeOverviewSettingsValues.DefaultMaximumDistanceInTiles);

            Scribe_Values.Look(
                ref _requireReachable,
                "requireReachable",
                SettlementTradeOverviewSettingsValues.DefaultRequireReachable);

            Scribe_Values.Look(ref _requireRoyaltyTradePermission, "requireRoyaltyTradePermission");

            base.ExposeData();
        }
    }
}