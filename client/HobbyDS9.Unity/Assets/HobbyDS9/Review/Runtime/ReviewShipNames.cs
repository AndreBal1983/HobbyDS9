#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using HobbyDS9.Contracts;
using Newtonsoft.Json;

namespace HobbyDS9.UnityReview
{
    public sealed class ReviewShipIdentity
    {
        public string Name { get; }
        public string Registry { get; }
        public string Label => Name + " · " + Registry;
        internal ReviewShipIdentity(string name, string registry) { Name = name; Registry = registry; }
    }

    // Presentation identities keyed by the immutable construction allocation, never by
    // roster position, surviving ship count, current fleet, blueprint or gameplay RNG.
    // v1 is append-frozen: changing its ordering would rename existing vessels.
    public static class ReviewShipNames
    {
        private sealed class Entry
        {
            public string Name { get; set; } = "";
            public string ReferenceRegistry { get; set; } = "";
            public string Source { get; set; } = "";
        }
        private static readonly Entry[] entries = ReadCatalogue();
        private static Entry[] ReadCatalogue()
        {
            string json;
#if UNITY_5_3_OR_NEWER || UNITY_6000_0_OR_NEWER
            var asset = UnityEngine.Resources.Load<UnityEngine.TextAsset>("ShipNames/starfleet-v1");
            if (asset == null) throw new InvalidOperationException("Missing Starfleet name catalogue.");
            json = asset.text;
#else
            using (var stream = typeof(ReviewShipNames).Assembly.GetManifestResourceStream("HobbyDS9.ShipNames.starfleet-v1.json")
                ?? throw new InvalidOperationException("Missing Starfleet name catalogue."))
            using (var reader = new StreamReader(stream)) json = reader.ReadToEnd();
#endif
            var result = JsonConvert.DeserializeObject<Entry[]>(json) ?? Array.Empty<Entry>();
            if (result.Length == 0 || result.Any(e => string.IsNullOrWhiteSpace(e.Name) || string.IsNullOrWhiteSpace(e.Source))
                || result.Select(e => e.Name).Distinct(StringComparer.Ordinal).Count() != result.Length)
                throw new InvalidOperationException("Invalid Starfleet name catalogue.");
            return result;
        }

        public static ReviewShipIdentity? Ship(string? shipId, string owner)
        {
            if (owner != "civ.federation" && owner != "civilization.federation") return null;
            // The authored starting vessel occupies slot zero. Spawn allocations start
            // at one and remain globally unique even after destruction or transfer.
            ulong serial;
            if (shipId == "ship.equipment-cruiser") serial = 0;
            else if (!SpawnSequence(shipId, "ship", out serial)) return null;
            var slot = (int)(serial % (ulong)entries.Length);
            var cycle = serial / (ulong)entries.Length;
            var name = "U.S.S. " + entries[slot].Name;
            if (cycle != 0) name += " " + (cycle + 1).ToString(CultureInfo.InvariantCulture);
            // A game convention, not a reconstructed canonical production formula.
            // Decimal text addition avoids overflow for the complete UInt64 ID range.
            var registry = "NCC-" + (new System.Numerics.BigInteger(serial) + 100000).ToString(CultureInfo.InvariantCulture);
            return new ReviewShipIdentity(name, registry);
        }

        private static bool SpawnSequence(string? id, string kind, out ulong serial)
        {
            var prefix = kind + ".spawn.";
            serial = 0;
            return id != null && id.StartsWith(prefix, StringComparison.Ordinal)
                && EntityId.TryParse(id, out var entity) && entity.TryGetAllocationSequence(out serial);
        }

        public static string Fleet(CampaignNamedFleet fleet, IEnumerable<ShipSnapshot> ships)
        {
            if (fleet.DisplayName != fleet.Fleet.FleetId.Value) return fleet.DisplayName;
            var first = ships.Where(s => s.FleetId == fleet.Fleet.FleetId)
                .OrderBy(s => s.ShipId.Value, StringComparer.Ordinal).FirstOrDefault();
            var identity = first == null ? null : Ship(first.ShipId.Value, first.OwnerCivilizationId.Value);
            if (identity != null) return fleet.Fleet.ShipIds.Count == 1 ? identity.Label : identity.Name + " group";
            return SpawnSequence(fleet.Fleet.FleetId.Value, "fleet", out var serial)
                ? "Task group " + serial.ToString(CultureInfo.InvariantCulture) : "Task group";
        }
    }
}
