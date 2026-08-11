using System;
using NUnit.Framework;
using SettlementTradeOverview.Domain.Identity;

namespace SettlementTradeOverview.Tests.Domain.Identity
{
    [TestFixture]
    public sealed class TradeIdentityTests
    {
        [Test]
        public void SettlementIdentity_EqualIds_AreEqual()
        {
            var first = new SettlementIdentity(42);
            var second = new SettlementIdentity(42);

            Assert.That(first.Equals(second), Is.True);
            Assert.That(first.Equals((object)second), Is.True);
            Assert.That(first.GetHashCode(), Is.EqualTo(second.GetHashCode()));
            Assert.That(first.ToString(), Is.EqualTo("42"));
        }

        [Test]
        public void SettlementIdentity_DifferentIds_AreNotEqual()
        {
            var first = new SettlementIdentity(42);
            var second = new SettlementIdentity(43);

            Assert.That(first.Equals(second), Is.False);
        }

        [Test]
        public void SettlementIdentity_NegativeId_Throws()
        {
            Assert.That(
                (Action)(() => { _ = new SettlementIdentity(-1); }),
                Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void TraderIdentity_UsesOrdinalCaseSensitiveEquality()
        {
            var first = new TraderIdentity("Settlement:42");
            var equal = new TraderIdentity("Settlement:42");
            var differentCase = new TraderIdentity("settlement:42");

            Assert.That(first.Equals(equal), Is.True);
            Assert.That(first.GetHashCode(), Is.EqualTo(equal.GetHashCode()));
            Assert.That(first.Equals(differentCase), Is.False);
            Assert.That(first.ToString(), Is.EqualTo("Settlement:42"));
        }

        [Test]
        public void TradeEntryIdentity_UsesOrdinalCaseSensitiveEquality()
        {
            var first = new TradeEntryIdentity("Steel");
            var equal = new TradeEntryIdentity("Steel");
            var differentCase = new TradeEntryIdentity("steel");

            Assert.That(first.Equals(equal), Is.True);
            Assert.That(first.GetHashCode(), Is.EqualTo(equal.GetHashCode()));
            Assert.That(first.Equals(differentCase), Is.False);
            Assert.That(first.ToString(), Is.EqualTo("Steel"));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void TraderIdentity_EmptyValue_Throws(string value)
        {
            Assert.That((Action)(() => { _ = new TraderIdentity(value); }), Throws.TypeOf<ArgumentException>());
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void TradeEntryIdentity_EmptyValue_Throws(string value)
        {
            Assert.That((Action)(() => { _ = new TradeEntryIdentity(value); }), Throws.TypeOf<ArgumentException>());
        }
    }
}