using System;

namespace SettlementTradeOverview.Domain.Identity
{
    public sealed class TraderIdentity : IEquatable<TraderIdentity>
    {
        public TraderIdentity(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Trader identity cannot be empty.", nameof(value));

            Value = value;
        }

        public string Value { get; }

        public bool Equals(TraderIdentity other)
        {
            return !ReferenceEquals(other, null) && StringComparer.Ordinal.Equals(Value, other.Value);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as TraderIdentity);
        }

        public override int GetHashCode()
        {
            return StringComparer.Ordinal.GetHashCode(Value);
        }

        public override string ToString()
        {
            return Value;
        }
    }
}