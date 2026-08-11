using System;
using NUnit.Framework;
using SettlementTradeOverview.Application.Settlements;
using SettlementTradeOverview.Domain.Eligibility;

namespace SettlementTradeOverview.Tests.Application.Settlements
{
    [TestFixture]
    public sealed class SettlementTradeCommandPolicyTests
    {
        [Test]
        public void Evaluate_NonPotentialTrader_ReturnsHidden()
        {
            SettlementTradeCommandState result = SettlementTradeCommandPolicy.Evaluate(
                isPotentialTrader: false,
                eligibilityResult: SettlementEligibilityResult.Eligible);

            Assert.That(result, Is.EqualTo(SettlementTradeCommandState.Hidden));
        }

        [Test]
        public void Evaluate_EligiblePotentialTrader_ReturnsEnabled()
        {
            SettlementTradeCommandState result = SettlementTradeCommandPolicy.Evaluate(
                isPotentialTrader: true,
                eligibilityResult: SettlementEligibilityResult.Eligible);

            Assert.That(result, Is.EqualTo(SettlementTradeCommandState.Enabled));
        }

        [Test]
        public void Evaluate_PlayerOwnedPotentialTrader_ReturnsHidden()
        {
            var eligibilityResult = SettlementEligibilityResult.Ineligible(
                SettlementEligibilityFailureReason.PlayerOwned);

            SettlementTradeCommandState result = SettlementTradeCommandPolicy.Evaluate(
                isPotentialTrader: true,
                eligibilityResult: eligibilityResult);

            Assert.That(result, Is.EqualTo(SettlementTradeCommandState.Hidden));
        }

        [Test]
        public void Evaluate_EveryOtherDefinedFailure_ReturnsDisabled()
        {
            foreach (SettlementEligibilityFailureReason failureReason in Enum.GetValues(
                         typeof(SettlementEligibilityFailureReason)))
            {
                if (failureReason == SettlementEligibilityFailureReason.None ||
                    failureReason == SettlementEligibilityFailureReason.PlayerOwned)
                {
                    continue;
                }

                var eligibilityResult = SettlementEligibilityResult.Ineligible(failureReason);

                SettlementTradeCommandState result = SettlementTradeCommandPolicy.Evaluate(
                    isPotentialTrader: true,
                    eligibilityResult: eligibilityResult);

                Assert.That(
                    result,
                    Is.EqualTo(SettlementTradeCommandState.Disabled),
                    $"Unexpected command state for failure reason {failureReason}.");
            }
        }

        [Test]
        public void Evaluate_RepeatedEvaluationReturnsEquivalentState()
        {
            var eligibilityResult = SettlementEligibilityResult.Ineligible(
                SettlementEligibilityFailureReason.Unreachable);

            SettlementTradeCommandState first = SettlementTradeCommandPolicy.Evaluate(
                isPotentialTrader: true,
                eligibilityResult: eligibilityResult);

            SettlementTradeCommandState second = SettlementTradeCommandPolicy.Evaluate(
                isPotentialTrader: true,
                eligibilityResult: eligibilityResult);

            Assert.That(first, Is.EqualTo(second));
        }

        [Test]
        public void Evaluate_NullEligibilityResult_Throws()
        {
            Assert.That(
                (Action)(() => SettlementTradeCommandPolicy.Evaluate(isPotentialTrader: true, eligibilityResult: null)),
                Throws.TypeOf<ArgumentNullException>());
        }
    }
}