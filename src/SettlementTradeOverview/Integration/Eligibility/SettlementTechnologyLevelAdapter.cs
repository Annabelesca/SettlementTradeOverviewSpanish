using RimWorld;
using SettlementTradeOverview.Domain.Eligibility;

namespace SettlementTradeOverview.Integration.Eligibility
{
    internal static class SettlementTechnologyLevelAdapter
    {
        public static SettlementTechnologyLevel Convert(TechLevel technologyLevel)
        {
            switch (technologyLevel)
            {
                case TechLevel.Animal:
                    return SettlementTechnologyLevel.Animal;

                case TechLevel.Neolithic:
                    return SettlementTechnologyLevel.Neolithic;

                case TechLevel.Medieval:
                    return SettlementTechnologyLevel.Medieval;

                case TechLevel.Industrial:
                    return SettlementTechnologyLevel.Industrial;

                case TechLevel.Spacer:
                    return SettlementTechnologyLevel.Spacer;

                case TechLevel.Ultra:
                    return SettlementTechnologyLevel.Ultra;

                case TechLevel.Archotech:
                    return SettlementTechnologyLevel.Archotech;

                case TechLevel.Undefined:
                default:
                    return SettlementTechnologyLevel.Unavailable;
            }
        }
    }
}