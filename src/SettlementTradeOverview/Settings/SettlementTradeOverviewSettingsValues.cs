namespace SettlementTradeOverview.Settings
{
    internal sealed class SettlementTradeOverviewSettingsValues
    {
        public const bool DefaultShowGlobalOverviewTab = true;
        public const bool DefaultRequirePoweredCommsConsole = true;
        public const bool DefaultRequireIndustrialTechnology = true;
        public const bool DefaultLimitMaximumDistance = true;
        public const int DefaultMaximumDistanceInTiles = 40;
        public const int MaximumAllowedDistanceInTiles = 3000;
        public const bool DefaultRequireReachable = true;
        public const bool DefaultRequireRoyaltyTradePermission = false;

        public bool ShowGlobalOverviewTab { get; set; } = DefaultShowGlobalOverviewTab;

        public bool RequirePoweredCommsConsole { get; set; } = DefaultRequirePoweredCommsConsole;

        public bool RequireIndustrialTechnology { get; set; } = DefaultRequireIndustrialTechnology;

        public bool LimitMaximumDistance { get; set; } = DefaultLimitMaximumDistance;

        public int MaximumDistanceInTiles { get; set; } = DefaultMaximumDistanceInTiles;

        public bool RequireReachable { get; set; } = DefaultRequireReachable;

        public bool RequireRoyaltyTradePermission { get; set; } = DefaultRequireRoyaltyTradePermission;
    }
}