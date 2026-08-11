using System;
using NUnit.Framework;
using SettlementTradeOverview.Domain.Identity;
using SettlementTradeOverview.Integration.Runtime;

namespace SettlementTradeOverview.Tests.Integration.Runtime
{
    [TestFixture]
    public sealed class TradeEntryRuntimeTargetCacheTests
    {
        [Test]
        public void TryRegister_ValidTarget_ResolvesByEquivalentIdentity()
        {
            TradeEntryRuntimeTargetRegistry<TestTarget> registry = CreateRegistry();
            var target = new TestTarget();

            bool registered = registry.TryRegister(new TradeEntryIdentity("Thing:1"), target);
            bool resolved = registry.TryResolve(new TradeEntryIdentity("Thing:1"), out TestTarget result);

            Assert.That(registered, Is.True);
            Assert.That(resolved, Is.True);
            Assert.That(result, Is.SameAs(target));
        }

        [Test]
        public void TryResolve_DifferentIdentity_ReturnsFalse()
        {
            TradeEntryRuntimeTargetRegistry<TestTarget> registry = CreateRegistry();
            registry.TryRegister(new TradeEntryIdentity("Thing:1"), new TestTarget());

            bool resolved = registry.TryResolve(new TradeEntryIdentity("Thing:2"), out TestTarget result);

            Assert.That(resolved, Is.False);
            Assert.That(result, Is.Null);
        }

        [Test]
        public void TryRegister_ExistingIdentity_ReplacesTarget()
        {
            TradeEntryRuntimeTargetRegistry<TestTarget> registry = CreateRegistry();
            var first = new TestTarget();
            var replacement = new TestTarget();
            var identity = new TradeEntryIdentity("Thing:1");

            registry.TryRegister(identity, first);
            bool registered = registry.TryRegister(new TradeEntryIdentity("Thing:1"), replacement);
            bool resolved = registry.TryResolve(identity, out TestTarget result);

            Assert.That(registered, Is.True);
            Assert.That(resolved, Is.True);
            Assert.That(result, Is.SameAs(replacement));
        }

        [Test]
        public void TryRegister_InvalidTarget_ReturnsFalse()
        {
            TradeEntryRuntimeTargetRegistry<TestTarget> registry = CreateRegistry();

            bool registered = registry.TryRegister(
                new TradeEntryIdentity("Thing:1"),
                new TestTarget { IsValid = false });

            Assert.That(registered, Is.False);
            Assert.That(registry.TryResolve(new TradeEntryIdentity("Thing:1"), out _), Is.False);
        }

        [Test]
        public void TryResolve_TargetBecameInvalid_RemovesOnlyThatMapping()
        {
            TradeEntryRuntimeTargetRegistry<TestTarget> registry = CreateRegistry();
            var invalidatedTarget = new TestTarget();
            var validTarget = new TestTarget();

            registry.TryRegister(new TradeEntryIdentity("Thing:1"), invalidatedTarget);
            registry.TryRegister(new TradeEntryIdentity("Thing:2"), validTarget);
            invalidatedTarget.IsValid = false;

            bool invalidResolved = registry.TryResolve(new TradeEntryIdentity("Thing:1"), out TestTarget invalidResult);

            bool validResolved = registry.TryResolve(new TradeEntryIdentity("Thing:2"), out TestTarget validResult);

            Assert.That(invalidResolved, Is.False);
            Assert.That(invalidResult, Is.Null);
            Assert.That(validResolved, Is.True);
            Assert.That(validResult, Is.SameAs(validTarget));
            Assert.That(registry.TryResolve(new TradeEntryIdentity("Thing:1"), out _), Is.False);
        }

        [Test]
        public void Clear_RemovesAllMappingsAndIsSafeWhenRepeated()
        {
            TradeEntryRuntimeTargetRegistry<TestTarget> registry = CreateRegistry();
            registry.TryRegister(new TradeEntryIdentity("Thing:1"), new TestTarget());
            registry.TryRegister(new TradeEntryIdentity("Thing:2"), new TestTarget());

            registry.Clear();
            registry.Clear();

            Assert.That(registry.TryResolve(new TradeEntryIdentity("Thing:1"), out _), Is.False);
            Assert.That(registry.TryResolve(new TradeEntryIdentity("Thing:2"), out _), Is.False);
        }

        [Test]
        public void TryMethods_NullIdentityOrTarget_ReturnFalse()
        {
            TradeEntryRuntimeTargetRegistry<TestTarget> registry = CreateRegistry();

            Assert.That(registry.TryRegister(null, new TestTarget()), Is.False);
            Assert.That(registry.TryRegister(new TradeEntryIdentity("Thing:1"), null), Is.False);
            Assert.That(registry.TryResolve(null, out TestTarget result), Is.False);
            Assert.That(result, Is.Null);
        }

        [Test]
        public void Constructor_NullValidityPredicate_Throws()
        {
            Assert.That(
                (Action)(() => { _ = new TradeEntryRuntimeTargetRegistry<TestTarget>(null); }),
                Throws.TypeOf<ArgumentNullException>());
        }

        private static TradeEntryRuntimeTargetRegistry<TestTarget> CreateRegistry()
        {
            return new TradeEntryRuntimeTargetRegistry<TestTarget>(target => target.IsValid);
        }

        private sealed class TestTarget
        {
            public bool IsValid { get; set; } = true;
        }
    }
}