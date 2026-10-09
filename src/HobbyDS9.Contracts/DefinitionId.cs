using System;

namespace HobbyDS9.Contracts
{
    public readonly struct DefinitionId : IEquatable<DefinitionId>, IComparable<DefinitionId>
    {
        private readonly string? value;
        public bool IsValid => value != null;
        public string Value => value ?? throw new InvalidOperationException("Uninitialized DefinitionId.");

        public DefinitionId(string value)
        {
            if (!CanonicalId.IsValid(value) || CanonicalId.IsReserved(value))
                throw new ArgumentException("Invalid canonical definition ID or reserved namespace.", nameof(value));
            this.value = value;
        }

        public static bool TryParse(string? value, out DefinitionId id)
        {
            id = default;
            if (!CanonicalId.IsValid(value) || CanonicalId.IsReserved(value!)) return false;
            id = new DefinitionId(value!);
            return true;
        }

        public bool Equals(DefinitionId other) => StringComparer.Ordinal.Equals(value, other.value);
        public override bool Equals(object? obj) => obj is DefinitionId other && Equals(other);
        public override int GetHashCode() => CanonicalId.Hash(value);
        public int CompareTo(DefinitionId other) => StringComparer.Ordinal.Compare(Value, other.Value);
        public override string ToString() => value ?? string.Empty;
        public static bool operator ==(DefinitionId left, DefinitionId right) => left.Equals(right);
        public static bool operator !=(DefinitionId left, DefinitionId right) => !left.Equals(right);
    }
}
