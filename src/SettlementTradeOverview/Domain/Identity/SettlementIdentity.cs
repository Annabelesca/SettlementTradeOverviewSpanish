using System;
using System.Globalization;

namespace SettlementTradeOverview.Domain.Identity
{
    public sealed class SettlementIdentity : IEquatable<SettlementIdentity>
    {
        public SettlementIdentity(int worldObjectId)
        {
            if (worldObjectId < 0)
                throw new ArgumentOutOfRangeException(nameof(worldObjectId));

            WorldObjectId = worldObjectId;
        }

        public int WorldObjectId { get; }

        public bool Equals(SettlementIdentity other)
        {
            return !ReferenceEquals(other, null) && WorldObjectId == other.WorldObjectId;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as SettlementIdentity);
        }

        public override int GetHashCode()
        {
            return WorldObjectId;
        }

        public override string ToString()
        {
            return WorldObjectId.ToString(CultureInfo.InvariantCulture);
        }
    }
}