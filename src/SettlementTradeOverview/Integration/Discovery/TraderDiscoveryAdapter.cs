using System;
using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using SettlementTradeOverview.Domain.Eligibility;
using SettlementTradeOverview.Integration.Context;
using SettlementTradeOverview.Integration.Eligibility;
using Verse;

namespace SettlementTradeOverview.Integration.Discovery
{
    internal static class TraderDiscoveryAdapter
    {
        private const string LogPrefix = "[SettlementTradeOverview]";

        public static TraderDiscoveryResult Discover(SettlementEligibilityCriteria criteria)
        {
            if (criteria == null)
                throw new ArgumentNullException(nameof(criteria));

            if (!PlayerTradeContextAdapter.TryCreate(out PlayerTradeContext context))
                return TraderDiscoveryResult.ContextUnavailable;

            return Discover(context, criteria);
        }

        public static TraderDiscoveryResult Discover(PlayerTradeContext context, SettlementEligibilityCriteria criteria)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            if (criteria == null)
                throw new ArgumentNullException(nameof(criteria));

            var sources = new List<DiscoveredTraderSource>();
            int candidateCount = context.Settlements.Count;
            var rejectedCandidateCount = 0;
            var failedCandidateCount = 0;

            foreach (Settlement settlement in context.Settlements)
            {
                try
                {
                    if (!TryValidateCandidate(settlement, out ITrader trader, out string validationFailure))
                    {
                        failedCandidateCount++;

                        Log.Warning(
                            $"{LogPrefix} Trader discovery skipped malformed settlement candidate " +
                            $"({DescribeCandidate(settlement)}): {validationFailure}.");

                        continue;
                    }

                    SettlementEligibilityFacts facts =
                        SettlementEligibilityFactsAdapter.Create(settlement, context, criteria);

                    SettlementEligibilityResult eligibilityResult =
                        SettlementEligibilityPolicy.Evaluate(facts, criteria);

                    if (!eligibilityResult.IsEligible)
                    {
                        rejectedCandidateCount++;
                        continue;
                    }

                    sources.Add(new DiscoveredTraderSource(settlement, trader, facts));
                }
                catch (Exception exception)
                {
                    failedCandidateCount++;

                    Log.Warning(
                        $"{LogPrefix} Trader discovery failed for settlement candidate " +
                        $"({DescribeCandidate(settlement)}). The remaining settlements will still be processed.\n" +
                        exception);
                }
            }

            return new TraderDiscoveryResult(
                true,
                sources,
                candidateCount,
                rejectedCandidateCount,
                failedCandidateCount);
        }

        private static bool TryValidateCandidate(Settlement settlement, out ITrader trader, out string failureReason)
        {
            trader = null;
            failureReason = null;

            if (settlement == null)
            {
                failureReason = "the candidate is null";
                return false;
            }

            if (settlement.Destroyed)
            {
                failureReason = "the settlement has been destroyed";
                return false;
            }

            if (settlement.ID < 0)
            {
                failureReason = "the settlement has an invalid world object ID";
                return false;
            }

            trader = settlement;
            return true;
        }

        private static string DescribeCandidate(Settlement settlement)
        {
            if (settlement == null)
                return "null";

            string typeName = settlement.GetType().FullName ?? settlement.GetType().Name;

            return $"ID={settlement.ID}, Type={typeName}";
        }
    }
}