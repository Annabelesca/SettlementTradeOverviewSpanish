using SettlementTradeOverview.Application.Snapshots;
using SettlementTradeOverview.Integration.Settings;
using SettlementTradeOverview.Settings;
using SettlementTradeOverview.UI.Settings;
using UnityEngine;
using Verse;

namespace SettlementTradeOverview
{
    public sealed class SettlementTradeOverviewMod : Mod
    {
        private readonly SettlementTradeOverviewSettings _settings;
        private readonly SettlementTradeOverviewSettingsView _settingsView;

        public SettlementTradeOverviewMod(ModContentPack content) : base(content)
        {
            _settings = GetSettings<SettlementTradeOverviewSettings>();

            SettlementTradeOverviewSettingsValues values = CaptureNormalizedValues();

            SettlementTradeOverviewSettingsService.Apply(values);

            _settingsView = new SettlementTradeOverviewSettingsView(_settings);

            LongEventHandler.ExecuteWhenFinished(() =>
                GlobalOverviewMainButtonVisibilityAdapter.Apply(
                    SettlementTradeOverviewSettingsService.ShowGlobalOverviewTab));
        }

        public override string SettingsCategory()
        {
            return "STO.Settings.Category".Translate().ToString();
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            _settingsView.Draw(inRect, _settings);
        }

        public override void WriteSettings()
        {
            SettlementTradeOverviewSettingsValues values = CaptureNormalizedValues();

            _settingsView.SynchronizeBuffers(_settings);

            base.WriteSettings();

            SettlementTradeOverviewSettingsApplyResult result = SettlementTradeOverviewSettingsService.Apply(values);

            if (result.EligibilityChanged)
                TradeInventorySnapshotService.Invalidate();

            if (result.GlobalOverviewTabVisibilityChanged)
            {
                GlobalOverviewMainButtonVisibilityAdapter.Apply(
                    SettlementTradeOverviewSettingsService.ShowGlobalOverviewTab);
            }
        }

        private SettlementTradeOverviewSettingsValues CaptureNormalizedValues()
        {
            SettlementTradeOverviewSettingsValues values = _settings.CaptureValues();

            SettlementTradeOverviewSettingsPolicy.Normalize(values);
            _settings.ApplyValues(values);

            return values;
        }
    }
}