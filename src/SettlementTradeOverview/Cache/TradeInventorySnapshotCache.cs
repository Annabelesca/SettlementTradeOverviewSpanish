using System;
using SettlementTradeOverview.Domain.Snapshots;

namespace SettlementTradeOverview.Cache
{
    internal sealed class TradeInventorySnapshotCache
    {
        private TradeInventorySnapshotCacheKey _key;
        private TradeInventorySnapshot _snapshot;

        public TradeInventorySnapshotCacheState State { get; private set; } =
            TradeInventorySnapshotCacheState.NotLoaded;

        public TradeInventorySnapshot Snapshot =>
            _snapshot;

        public bool HasSnapshot =>
            _snapshot != null;

        public bool TryGetReusableSnapshot(TradeInventorySnapshotReuseKey reuseKey, out TradeInventorySnapshot snapshot)
        {
            if (reuseKey == null)
                throw new ArgumentNullException(nameof(reuseKey));

            snapshot = null;

            if (_snapshot == null || _key == null || !_key.ReuseKey.Equals(reuseKey))
                return false;

            snapshot = _snapshot;
            return true;
        }

        public TradeInventorySnapshot GetOrBuild(
            TradeInventorySnapshotCacheKey key,
            Func<TradeInventorySnapshot> snapshotFactory)
        {
            ValidateArguments(key, snapshotFactory);

            if (_snapshot != null && _key != null && _key.Equals(key))
                return _snapshot;

            return Build(key, snapshotFactory);
        }

        public TradeInventorySnapshot Refresh(
            TradeInventorySnapshotCacheKey key,
            Func<TradeInventorySnapshot> snapshotFactory)
        {
            ValidateArguments(key, snapshotFactory);

            Invalidate();

            return Build(key, snapshotFactory);
        }

        public void Invalidate()
        {
            _key = null;
            _snapshot = null;
            State = TradeInventorySnapshotCacheState.NotLoaded;
        }

        private TradeInventorySnapshot Build(
            TradeInventorySnapshotCacheKey key,
            Func<TradeInventorySnapshot> snapshotFactory)
        {
            _key = null;
            _snapshot = null;
            State = TradeInventorySnapshotCacheState.Loading;

            try
            {
                TradeInventorySnapshot snapshot = snapshotFactory();

                _key = key;
                _snapshot = snapshot ?? throw new InvalidOperationException("The snapshot factory returned null.");
                State = ResolveState(snapshot.Availability);

                return snapshot;
            }
            catch
            {
                _key = null;
                _snapshot = null;
                State = TradeInventorySnapshotCacheState.Failed;

                throw;
            }
        }

        private static TradeInventorySnapshotCacheState ResolveState(SnapshotAvailability availability)
        {
            switch (availability)
            {
                case SnapshotAvailability.Available:
                    return TradeInventorySnapshotCacheState.Available;

                case SnapshotAvailability.Empty:
                    return TradeInventorySnapshotCacheState.Empty;

                case SnapshotAvailability.Unavailable:
                    return TradeInventorySnapshotCacheState.Unavailable;

                case SnapshotAvailability.Partial:
                    return TradeInventorySnapshotCacheState.Partial;

                case SnapshotAvailability.Failed:
                    return TradeInventorySnapshotCacheState.Failed;

                default:
                    throw new ArgumentOutOfRangeException(nameof(availability));
            }
        }

        private static void ValidateArguments(
            TradeInventorySnapshotCacheKey key,
            Func<TradeInventorySnapshot> snapshotFactory)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            if (snapshotFactory == null)
                throw new ArgumentNullException(nameof(snapshotFactory));
        }
    }
}