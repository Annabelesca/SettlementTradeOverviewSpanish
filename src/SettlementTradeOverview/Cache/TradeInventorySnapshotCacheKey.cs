using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using SettlementTradeOverview.Domain.Eligibility;
using SettlementTradeOverview.Domain.Identity;

namespace SettlementTradeOverview.Cache
{
    internal sealed class TradeInventorySnapshotCacheKey : IEquatable<TradeInventorySnapshotCacheKey>
    {
        private readonly ReadOnlyCollection<string> _traderIdentities;

        private TradeInventorySnapshotCacheKey(
            TradeInventorySnapshotReuseKey reuseKey,
            IReadOnlyList<string> traderIdentities,
            int discoveryFailureCount)
        {
            ReuseKey = reuseKey ?? throw new ArgumentNullException(nameof(reuseKey));

            if (traderIdentities == null)
                throw new ArgumentNullException(nameof(traderIdentities));

            if (discoveryFailureCount < 0)
                throw new ArgumentOutOfRangeException(nameof(discoveryFailureCount));

            var copiedTraderIdentities = new string[traderIdentities.Count];

            for (var index = 0; index < traderIdentities.Count; index++)
            {
                string identity = traderIdentities[index];

                if (string.IsNullOrWhiteSpace(identity))
                {
                    throw new ArgumentException(
                        "Trader identities cannot contain empty values.",
                        nameof(traderIdentities));
                }

                copiedTraderIdentities[index] = identity;
            }

            _traderIdentities = Array.AsReadOnly(copiedTraderIdentities);
            DiscoveryFailureCount = discoveryFailureCount;
        }

        public TradeInventorySnapshotReuseKey ReuseKey { get; }

        public bool IsContextAvailable =>
            ReuseKey.IsContextAvailable;

        public int? MapId =>
            ReuseKey.MapId;

        public int? OriginTile =>
            ReuseKey.OriginTile;

        public bool HasNegotiator =>
            ReuseKey.HasNegotiator;

        public string NegotiatorPawnId =>
            ReuseKey.NegotiatorPawnId;

        public string NegotiatorLabel =>
            ReuseKey.NegotiatorLabel;

        public float? NegotiatorTradePriceImprovement =>
            ReuseKey.NegotiatorTradePriceImprovement;

        public bool RequirePoweredCommsConsole =>
            ReuseKey.RequirePoweredCommsConsole;

        public SettlementTechnologyLevel? MinimumTechnologyLevel =>
            ReuseKey.MinimumTechnologyLevel;

        public int? MaximumDistanceInTiles =>
            ReuseKey.MaximumDistanceInTiles;

        public bool RequireReachable =>
            ReuseKey.RequireReachable;

        public bool RequireRoyaltyTradePermission =>
            ReuseKey.RequireRoyaltyTradePermission;

        public IReadOnlyList<string> TraderIdentities =>
            _traderIdentities;

        public int DiscoveryFailureCount { get; }

        public static TradeInventorySnapshotCacheKey CreateUnavailableContext(SettlementEligibilityCriteria criteria)
        {
            return new TradeInventorySnapshotCacheKey(
                TradeInventorySnapshotReuseKey.CreateUnavailableContext(criteria),
                Array.Empty<string>(),
                0);
        }

        public static TradeInventorySnapshotCacheKey CreateAvailableContext(
            TradeInventorySnapshotReuseKey reuseKey,
            IReadOnlyList<TraderIdentity> traderIdentities,
            int discoveryFailureCount)
        {
            if (reuseKey == null)
                throw new ArgumentNullException(nameof(reuseKey));

            if (!reuseKey.IsContextAvailable)
            {
                throw new ArgumentException(
                    "An available full cache key requires an available reuse key.",
                    nameof(reuseKey));
            }

            if (traderIdentities == null)
                throw new ArgumentNullException(nameof(traderIdentities));

            var copiedIdentityValues = new string[traderIdentities.Count];

            for (var index = 0; index < traderIdentities.Count; index++)
            {
                TraderIdentity identity = traderIdentities[index] ?? throw new ArgumentException(
                    "Trader identities cannot contain null values.",
                    nameof(traderIdentities));

                copiedIdentityValues[index] = identity.Value;
            }

            return new TradeInventorySnapshotCacheKey(reuseKey, copiedIdentityValues, discoveryFailureCount);
        }

        public bool Equals(TradeInventorySnapshotCacheKey other)
        {
            if (ReferenceEquals(this, other))
                return true;

            if (ReferenceEquals(other, null))
                return false;

            if (!ReuseKey.Equals(other.ReuseKey) || DiscoveryFailureCount != other.DiscoveryFailureCount ||
                _traderIdentities.Count != other._traderIdentities.Count)
            {
                return false;
            }

            for (var index = 0; index < _traderIdentities.Count; index++)
            {
                if (!StringComparer.Ordinal.Equals(_traderIdentities[index], other._traderIdentities[index]))
                    return false;
            }

            return true;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as TradeInventorySnapshotCacheKey);
        }

        public override int GetHashCode()
        {
            var hashCode = 17;

            hashCode = CombineHashCode(hashCode, ReuseKey.GetHashCode());
            hashCode = CombineHashCode(hashCode, DiscoveryFailureCount);

            foreach (string identity in _traderIdentities)
                hashCode = CombineHashCode(hashCode, StringComparer.Ordinal.GetHashCode(identity));

            return hashCode;
        }

        private static int CombineHashCode(int currentHashCode, int valueHashCode)
        {
            unchecked
            {
                return currentHashCode * 397 ^ valueHashCode;
            }
        }
    }
}