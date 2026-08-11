using System;
using SettlementTradeOverview.Domain.Eligibility;
using SettlementTradeOverview.Domain.Snapshots;

namespace SettlementTradeOverview.Cache
{
    internal sealed class TradeInventorySnapshotReuseKey : IEquatable<TradeInventorySnapshotReuseKey>
    {
        private TradeInventorySnapshotReuseKey(
            bool isContextAvailable,
            int? mapId,
            int? originTile,
            TradeNegotiatorSnapshot negotiator,
            SettlementEligibilityCriteria criteria,
            bool hasPoweredCommsConsole,
            bool isRoyaltyActive)
        {
            if (criteria == null)
                throw new ArgumentNullException(nameof(criteria));

            IsContextAvailable = isContextAvailable;
            MapId = mapId;
            OriginTile = originTile;

            HasNegotiator = negotiator != null;
            NegotiatorPawnId = negotiator?.PawnId;
            NegotiatorLabel = negotiator?.Label;
            NegotiatorTradePriceImprovement = negotiator?.TradePriceImprovement;

            RequirePoweredCommsConsole = criteria.RequirePoweredCommsConsole;
            MinimumTechnologyLevel = criteria.MinimumTechnologyLevel;
            MaximumDistanceInTiles = criteria.MaximumDistanceInTiles;
            RequireReachable = criteria.RequireReachable;
            RequireRoyaltyTradePermission = criteria.RequireRoyaltyTradePermission;

            HasPoweredCommsConsole = criteria.RequirePoweredCommsConsole && hasPoweredCommsConsole;
            IsRoyaltyActive = criteria.RequireRoyaltyTradePermission && isRoyaltyActive;
        }

        public bool IsContextAvailable { get; }

        public int? MapId { get; }

        public int? OriginTile { get; }

        public bool HasNegotiator { get; }

        public string NegotiatorPawnId { get; }

        public string NegotiatorLabel { get; }

        public float? NegotiatorTradePriceImprovement { get; }

        public bool RequirePoweredCommsConsole { get; }

        public SettlementTechnologyLevel? MinimumTechnologyLevel { get; }

        public int? MaximumDistanceInTiles { get; }

        public bool RequireReachable { get; }

        public bool RequireRoyaltyTradePermission { get; }

        public bool HasPoweredCommsConsole { get; }

        public bool IsRoyaltyActive { get; }

        public static TradeInventorySnapshotReuseKey CreateUnavailableContext(SettlementEligibilityCriteria criteria)
        {
            return new TradeInventorySnapshotReuseKey(
                false,
                null,
                null,
                null,
                criteria,
                hasPoweredCommsConsole: false,
                isRoyaltyActive: false);
        }

        public static TradeInventorySnapshotReuseKey CreateAvailableContext(
            int mapId,
            int originTile,
            TradeNegotiatorSnapshot negotiator,
            SettlementEligibilityCriteria criteria,
            bool hasPoweredCommsConsole,
            bool isRoyaltyActive)
        {
            if (mapId < 0)
                throw new ArgumentOutOfRangeException(nameof(mapId));

            if (originTile < 0)
                throw new ArgumentOutOfRangeException(nameof(originTile));

            return new TradeInventorySnapshotReuseKey(
                true,
                mapId,
                originTile,
                negotiator,
                criteria,
                hasPoweredCommsConsole,
                isRoyaltyActive);
        }

        public bool Equals(TradeInventorySnapshotReuseKey other)
        {
            if (ReferenceEquals(this, other))
                return true;

            if (ReferenceEquals(other, null))
                return false;

            return IsContextAvailable == other.IsContextAvailable && MapId == other.MapId &&
                   OriginTile == other.OriginTile && HasNegotiator == other.HasNegotiator &&
                   StringComparer.Ordinal.Equals(NegotiatorPawnId, other.NegotiatorPawnId) &&
                   StringComparer.Ordinal.Equals(NegotiatorLabel, other.NegotiatorLabel) &&
                   Nullable.Equals(NegotiatorTradePriceImprovement, other.NegotiatorTradePriceImprovement) &&
                   RequirePoweredCommsConsole == other.RequirePoweredCommsConsole &&
                   MinimumTechnologyLevel == other.MinimumTechnologyLevel &&
                   MaximumDistanceInTiles == other.MaximumDistanceInTiles &&
                   RequireReachable == other.RequireReachable &&
                   RequireRoyaltyTradePermission == other.RequireRoyaltyTradePermission &&
                   HasPoweredCommsConsole == other.HasPoweredCommsConsole && IsRoyaltyActive == other.IsRoyaltyActive;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as TradeInventorySnapshotReuseKey);
        }

        public override int GetHashCode()
        {
            var hashCode = 17;

            hashCode = CombineHashCode(hashCode, IsContextAvailable.GetHashCode());
            hashCode = CombineHashCode(hashCode, MapId.GetHashCode());
            hashCode = CombineHashCode(hashCode, OriginTile.GetHashCode());
            hashCode = CombineHashCode(hashCode, HasNegotiator.GetHashCode());
            hashCode = CombineHashCode(hashCode, GetStringHashCode(NegotiatorPawnId));
            hashCode = CombineHashCode(hashCode, GetStringHashCode(NegotiatorLabel));
            hashCode = CombineHashCode(hashCode, NegotiatorTradePriceImprovement.GetHashCode());
            hashCode = CombineHashCode(hashCode, RequirePoweredCommsConsole.GetHashCode());
            hashCode = CombineHashCode(hashCode, MinimumTechnologyLevel.GetHashCode());
            hashCode = CombineHashCode(hashCode, MaximumDistanceInTiles.GetHashCode());
            hashCode = CombineHashCode(hashCode, RequireReachable.GetHashCode());
            hashCode = CombineHashCode(hashCode, RequireRoyaltyTradePermission.GetHashCode());
            hashCode = CombineHashCode(hashCode, HasPoweredCommsConsole.GetHashCode());
            hashCode = CombineHashCode(hashCode, IsRoyaltyActive.GetHashCode());

            return hashCode;
        }

        private static int GetStringHashCode(string value)
        {
            return value == null ? 0 : StringComparer.Ordinal.GetHashCode(value);
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