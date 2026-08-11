using System;
using Escarval.RimWorld.UI;
using RimWorld;
using RimWorld.Planet;
using SettlementTradeOverview.Application.Settlements;
using SettlementTradeOverview.Domain.Eligibility;
using SettlementTradeOverview.Domain.Identity;
using SettlementTradeOverview.Integration.Context;
using SettlementTradeOverview.Integration.Eligibility;
using SettlementTradeOverview.Settings;
using SettlementTradeOverview.UI.SettlementStock;
using Verse;

namespace SettlementTradeOverview.Integration.Settlements
{
    [StaticConstructorOnStartup]
    internal static class SettlementTradeCommandProvider
    {
        private const string LogPrefix = "[SettlementTradeOverview]";

        private static readonly ReloadableTexture2D _commandIcon =
            new ReloadableTexture2D("UI/Commands/SettlementTradeStock");

        public static bool TryCreate(Settlement settlement, out Command_Action command)
        {
            command = null;

            if (!IsStructurallySupported(settlement) || IsPlayerOwned(settlement))
                return false;

            try
            {
                var settlementIdentity = new SettlementIdentity(settlement.ID);
                string settlementLabel = settlement.LabelCap;

                command = CreateCommand(settlementIdentity, settlementLabel);

                if (!PlayerTradeContextAdapter.TryCreate(out PlayerTradeContext context))
                {
                    command.Disable("STO.SettlementEntryPoint.Disabled.ContextUnavailable".Translate().ToString());

                    return true;
                }

                SettlementEligibilityCriteria criteria =
                    SettlementTradeOverviewSettingsService.CurrentEligibilityCriteria;

                SettlementEligibilityFacts facts = SettlementEligibilityFactsAdapter.Create(
                    settlement,
                    context,
                    criteria);

                SettlementEligibilityResult eligibilityResult = SettlementEligibilityPolicy.Evaluate(facts, criteria);

                SettlementTradeCommandState state = SettlementTradeCommandPolicy.Evaluate(
                    isPotentialTrader: true,
                    eligibilityResult: eligibilityResult);

                switch (state)
                {
                    case SettlementTradeCommandState.Enabled:
                        return true;

                    case SettlementTradeCommandState.Disabled:
                        command.Disable(CreateDisabledReason(eligibilityResult.FailureReason, settlementLabel));
                        return true;

                    case SettlementTradeCommandState.Hidden:
                        command = null;
                        return false;

                    default:
                        throw new ArgumentOutOfRangeException(nameof(state));
                }
            }
            catch (Exception exception)
            {
                command = null;

                Log.Warning(
                    $"{LogPrefix} Failed to create a settlement trade command for {DescribeSettlement(settlement)}." +
                    exception);

                return false;
            }
        }

        private static bool IsStructurallySupported(Settlement settlement)
        {
            return settlement != null && !settlement.Destroyed && settlement.Spawned && settlement.ID >= 0 &&
                   settlement.Tile.Valid && settlement.Faction != null && settlement.TraderKind != null;
        }

        private static bool IsPlayerOwned(Settlement settlement)
        {
            Faction playerFaction = Faction.OfPlayer;

            return playerFaction != null && settlement.Faction == playerFaction;
        }

        private static Command_Action CreateCommand(SettlementIdentity settlementIdentity, string settlementLabel)
        {
            return new Command_Action
            {
                defaultLabel = "STO.SettlementEntryPoint.Label".Translate().ToString(),
                defaultDesc = "STO.SettlementEntryPoint.Description".Translate(settlementLabel).ToString(),
                icon = _commandIcon.Texture,
                action = () => SettlementStockWindowService.Open(settlementIdentity)
            };
        }

        private static string CreateDisabledReason(
            SettlementEligibilityFailureReason failureReason,
            string settlementLabel)
        {
            switch (failureReason)
            {
                case SettlementEligibilityFailureReason.TradeUnavailable:
                    return "STO.SettlementEntryPoint.Disabled.TradeUnavailable".Translate(settlementLabel).ToString();

                case SettlementEligibilityFailureReason.Hostile:
                    return "STO.SettlementEntryPoint.Disabled.Hostile".Translate(settlementLabel).ToString();

                case SettlementEligibilityFailureReason.TechnologyUnavailable:
                    return "STO.SettlementEntryPoint.Disabled.TechnologyUnavailable".Translate().ToString();

                case SettlementEligibilityFailureReason.TechnologyBelowMinimum:
                    return "STO.SettlementEntryPoint.Disabled.TechnologyBelowMinimum".Translate().ToString();

                case SettlementEligibilityFailureReason.PoweredCommsConsoleRequired:
                    return "STO.SettlementEntryPoint.Disabled.PoweredCommsConsoleRequired".Translate().ToString();

                case SettlementEligibilityFailureReason.DistanceUnavailable:
                    return "STO.SettlementEntryPoint.Disabled.DistanceUnavailable".Translate().ToString();

                case SettlementEligibilityFailureReason.BeyondMaximumDistance:
                    return "STO.SettlementEntryPoint.Disabled.BeyondMaximumDistance".Translate().ToString();

                case SettlementEligibilityFailureReason.ReachabilityUnavailable:
                    return "STO.SettlementEntryPoint.Disabled.ReachabilityUnavailable".Translate().ToString();

                case SettlementEligibilityFailureReason.Unreachable:
                    return "STO.SettlementEntryPoint.Disabled.Unreachable".Translate().ToString();

                case SettlementEligibilityFailureReason.RoyaltyTradePermissionUnavailable:
                    return "STO.SettlementEntryPoint.Disabled.RoyaltyTradePermissionUnavailable".Translate().ToString();

                case SettlementEligibilityFailureReason.RoyaltyTradePermissionDenied:
                    return "STO.SettlementEntryPoint.Disabled.RoyaltyTradePermissionDenied".Translate().ToString();

                case SettlementEligibilityFailureReason.PlayerOwned:
                case SettlementEligibilityFailureReason.None:
                default:
                    return "STO.SettlementEntryPoint.Disabled.Fallback".Translate().ToString();
            }
        }

        private static string DescribeSettlement(Settlement settlement)
        {
            if (settlement == null)
                return "a null settlement";

            string typeName = settlement.GetType().FullName ?? settlement.GetType().Name;

            return $"settlement ID={settlement.ID}, Type={typeName}";
        }
    }
}