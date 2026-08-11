using System;
using System.Collections.Generic;
using SettlementTradeOverview.Domain.Identity;
using Verse;

namespace SettlementTradeOverview.Integration.Runtime
{
    internal static class TradeEntryRuntimeTargetCache
    {
        private static readonly TradeEntryRuntimeTargetRegistry<Thing> _registry =
            new TradeEntryRuntimeTargetRegistry<Thing>(IsValidRepresentative);

        public static bool TryRegister(TradeEntryIdentity identity, Thing representative)
        {
            try
            {
                return _registry.TryRegister(identity, representative);
            }
            catch
            {
                return false;
            }
        }

        public static bool TryResolve(TradeEntryIdentity identity, out Thing representative)
        {
            try
            {
                return _registry.TryResolve(identity, out representative);
            }
            catch
            {
                representative = null;
                return false;
            }
        }

        public static void Clear()
        {
            _registry.Clear();
        }

        private static bool IsValidRepresentative(Thing representative)
        {
            return representative != null && !representative.Destroyed;
        }
    }

    internal sealed class TradeEntryRuntimeTargetRegistry<TTarget> where TTarget : class
    {
        private readonly Dictionary<TradeEntryIdentity, TTarget> _targets =
            new Dictionary<TradeEntryIdentity, TTarget>();

        private readonly Func<TTarget, bool> _isValid;

        public TradeEntryRuntimeTargetRegistry(Func<TTarget, bool> isValid)
        {
            _isValid = isValid ?? throw new ArgumentNullException(nameof(isValid));
        }

        public bool TryRegister(TradeEntryIdentity identity, TTarget target)
        {
            if (identity == null || target == null || !_isValid(target))
                return false;

            _targets[identity] = target;
            return true;
        }

        public bool TryResolve(TradeEntryIdentity identity, out TTarget target)
        {
            target = null;

            if (identity == null || !_targets.TryGetValue(identity, out TTarget storedTarget))
                return false;

            if (_isValid(storedTarget))
            {
                target = storedTarget;
                return true;
            }

            _targets.Remove(identity);
            return false;
        }

        public void Clear()
        {
            _targets.Clear();
        }
    }
}