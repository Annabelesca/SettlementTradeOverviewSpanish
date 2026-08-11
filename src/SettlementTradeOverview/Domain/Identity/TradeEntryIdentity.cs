using System;

namespace SettlementTradeOverview.Domain.Identity
{
    public sealed class TradeEntryIdentity : IEquatable<TradeEntryIdentity>
    {
        public TradeEntryIdentity(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Trade entry identity cannot be empty.", nameof(value));

            Value = value;
        }

        public string Value { get; }

        public bool Equals(TradeEntryIdentity other)
        {
            return !ReferenceEquals(other, null) && StringComparer.Ordinal.Equals(Value, other.Value);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as TradeEntryIdentity);
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