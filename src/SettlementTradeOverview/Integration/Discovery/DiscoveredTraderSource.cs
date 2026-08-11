using System;
using System.Globalization;
using RimWorld;
using RimWorld.Planet;
using SettlementTradeOverview.Domain.Eligibility;
using SettlementTradeOverview.Domain.Identity;

namespace SettlementTradeOverview.Integration.Discovery
{
    internal sealed class DiscoveredTraderSource
    {
        public DiscoveredTraderSource(
            Settlement settlement,
            ITrader trader,
            SettlementEligibilityFacts eligibilityFacts)
        {
            if (settlement == null)
                throw new ArgumentNullException(nameof(settlement));

            if (trader == null)
                throw new ArgumentNullException(nameof(trader));

            if (settlement.ID < 0)
                throw new ArgumentOutOfRangeException(nameof(settlement));

            if (!ReferenceEquals(settlement, trader))
            {
                throw new ArgumentException("Trader must reference the supplied settlement.", nameof(trader));
            }

            SettlementIdentity = new SettlementIdentity(settlement.ID);

            TraderIdentity = new TraderIdentity("Settlement:" + settlement.ID.ToString(CultureInfo.InvariantCulture));

            EligibilityFacts = eligibilityFacts ?? throw new ArgumentNullException(nameof(eligibilityFacts));
            Settlement = settlement;
            Trader = trader;
        }

        public SettlementIdentity SettlementIdentity { get; }

        public TraderIdentity TraderIdentity { get; }

        public SettlementEligibilityFacts EligibilityFacts { get; }

        public Settlement Settlement { get; }

        public ITrader Trader { get; }
    }
}