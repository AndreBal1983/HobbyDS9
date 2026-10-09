using System;
using System.Globalization;

namespace HobbyDS9.Contracts
{
    internal static class CanonicalId
    {
        internal const string SpawnMarker = ".spawn.";

        internal static bool IsValid(string? value)
        {
            if (value == null || value.Length == 0 || value.Length > 128 || value[0] < 'a' || value[0] > 'z') return false;
            foreach (var c in value)
                if (!((c >= 'a' && c <= 'z') || (c >= '0' && c <= '9') || c == '.' || c == '_' || c == '-')) return false;
            return true;
        }

        internal static bool IsReserved(string value) => value.IndexOf(SpawnMarker, StringComparison.Ordinal) >= 0;

        internal static bool TryGetSequence(string value, out ulong sequence)
        {
            sequence = 0;
            var index = value.IndexOf(SpawnMarker, StringComparison.Ordinal);
            if (index <= 0) return false;
            var suffix = value.Substring(index + SpawnMarker.Length);
            if (suffix.Length != 16) return false;
            foreach (var c in suffix)
                if (!((c >= '0' && c <= '9') || (c >= 'a' && c <= 'f'))) return false;
            return ulong.TryParse(suffix, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out sequence) && sequence > 0;
        }

        // Ordinal IDs use ASCII; deterministic FNV-1a also avoids process-specific
        // string hashing. Rule iteration must still sort, never depend on hashes.
        internal static int Hash(string? value)
        {
            if (value == null) return 0;
            unchecked
            {
                uint hash = 2166136261;
                foreach (var c in value) { hash ^= c; hash *= 16777619; }
                return (int)hash;
            }
        }
    }
}
