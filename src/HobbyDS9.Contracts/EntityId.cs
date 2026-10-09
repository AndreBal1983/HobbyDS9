using System;

namespace HobbyDS9.Contracts
{
    public readonly struct EntityId : IEquatable<EntityId>, IComparable<EntityId>
    {
        private readonly string? value;
        public bool IsValid => value != null;
        public string Value => value ?? throw new InvalidOperationException("Uninitialized EntityId.");
        public bool UsesAllocationNamespace => value != null && CanonicalId.IsReserved(value);

        public EntityId(string value)
        {
            if (!CanonicalId.IsValid(value)) throw new ArgumentException("Invalid canonical entity ID.", nameof(value));
            this.value = value;
        }

        public static bool TryParse(string? value, out EntityId id)
        {
            id = default;
            if (!CanonicalId.IsValid(value)) return false;
            id = new EntityId(value!);
            return true;
        }

        public static EntityId FromAuthored(string value)
        {
            var id = new EntityId(value);
            if (id.UsesAllocationNamespace) throw new ArgumentException("Authored IDs cannot use .spawn. namespace.", nameof(value));
            return id;
        }

        public bool TryGetAllocationSequence(out ulong sequence)
        {
            sequence = 0;
            return value != null && CanonicalId.TryGetSequence(value, out sequence);
        }

        public bool Equals(EntityId other) => StringComparer.Ordinal.Equals(value, other.value);
        public override bool Equals(object? obj) => obj is EntityId other && Equals(other);
        public override int GetHashCode() => CanonicalId.Hash(value);
        public int CompareTo(EntityId other) => StringComparer.Ordinal.Compare(Value, other.Value);
        public override string ToString() => value ?? string.Empty;
        public static bool operator ==(EntityId left, EntityId right) => left.Equals(right);
        public static bool operator !=(EntityId left, EntityId right) => !left.Equals(right);
    }
}
