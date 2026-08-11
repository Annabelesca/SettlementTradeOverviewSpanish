using System;

namespace SettlementTradeOverview.Domain.Snapshots
{
    public readonly struct TradeRestockMoment
    {
        public TradeRestockMoment(int absoluteTick, float longitude, float latitude)
        {
            if (absoluteTick < 0)
                throw new ArgumentOutOfRangeException(nameof(absoluteTick));

            if (float.IsNaN(longitude) || float.IsInfinity(longitude))
                throw new ArgumentOutOfRangeException(nameof(longitude));

            if (float.IsNaN(latitude) || float.IsInfinity(latitude))
                throw new ArgumentOutOfRangeException(nameof(latitude));

            AbsoluteTick = absoluteTick;
            Longitude = longitude;
            Latitude = latitude;
        }

        public int AbsoluteTick { get; }

        public float Longitude { get; }

        public float Latitude { get; }
    }
}