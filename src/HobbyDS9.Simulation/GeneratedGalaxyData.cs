using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using HobbyDS9.Contracts;

namespace HobbyDS9.Simulation
{
    internal static class GeneratedGalaxyData
    {
        internal static string Id(string prefix, int index) => prefix + ".generated." + (index + 1).ToString("D3", CultureInfo.InvariantCulture);
        private static void Require([DoesNotReturnIf(false)] bool condition, string reason) { if (!condition) throw new ArgumentException(reason); }
        internal static GeneratedGalaxySettingsDto Settings(GeneratedGalaxySettingsDto s, IEnumerable<CivilizationDefinitionDto> catalog)
        {
            Require(s != null && s.Seed != null && s.Seed.Length <= 20, "Seed must be an unsigned 64-bit decimal number.");
            Require(ulong.TryParse(s.Seed, NumberStyles.None, CultureInfo.InvariantCulture, out var seed)
                && seed.ToString(CultureInfo.InvariantCulture) == s.Seed, "Seed must use canonical unsigned 64-bit decimal notation.");
            Require(s.SystemCount == 30 || s.SystemCount == 40 || s.SystemCount == 50, "Review size must be 30, 40 or 50 systems.");
            Require(s.HomeCivilizationDefinitionIds != null && s.HomeCivilizationDefinitionIds.Count == 2
                && s.HomeCivilizationDefinitionIds.All(id => id != null && id.Length <= 128 && DefinitionId.TryParse(id, out _))
                && s.HomeCivilizationDefinitionIds.Distinct(StringComparer.Ordinal).Count() == 2, "Select exactly two distinct admitted civilization definitions.");
            Require(catalog != null, "Missing admitted civilization catalog.");
            var admitted = catalog.Take(257).ToList();
            Require(admitted.Count <= 256 && admitted.All(c => c != null && c.DefinitionId != null && c.DefinitionId.Length <= 128
                && DefinitionId.TryParse(c.DefinitionId, out _)) && admitted.Select(c => c.DefinitionId).Distinct(StringComparer.Ordinal).Count() == admitted.Count,
                "Invalid or oversized civilization catalog.");
            Require(s.HomeCivilizationDefinitionIds.All(id => admitted.Any(c => c.DefinitionId == id)), "Candidate civilization is not in the admitted catalog.");
            return new GeneratedGalaxySettingsDto { Seed = s.Seed, SystemCount = s.SystemCount,
                HomeCivilizationDefinitionIds = s.HomeCivilizationDefinitionIds.OrderBy(id => id, StringComparer.Ordinal).ToList() };
        }

        internal static void CheckShape(GeneratedGalaxyDocumentDto d)
        {
            Require(d != null, "Missing generated map.");
            Require(d.FormatVersion == GeneratedGalaxyIdentity.FormatVersion && d.ProfileId == GeneratedGalaxyIdentity.Profile
                && GeneratedGalaxyIdentity.Supports(d.GeneratorVersion, d.TuningVersion),
                "Unsupported generated-map format, profile, generator or tuning version. No migration is available.");
            Require(d.ContentHash != null && d.ContentHash.Length == 64 && d.ContentHash.All(c => c >= '0' && c <= '9' || c >= 'a' && c <= 'f'),
                "Missing or invalid map content hash.");
            Require(d.Settings != null && d.Regions != null && d.Systems != null && d.Connections != null && d.HomeCandidates != null,
                "Generated map is missing required collections.");
            Require(d.Regions.Count >= 60 && d.Regions.Count <= 100 && d.Systems.Count >= 30 && d.Systems.Count <= 50
                && d.Connections.Count > 0 && d.Connections.Count <= 300 && d.HomeCandidates.Count == 2, "Generated map exceeds review collection limits.");
            bool Text(string? s) => s != null && s.Length > 0 && s.Length <= 128;
            bool Point(GeneratedGalaxyPointDto? p) => p != null && p.X >= 0 && p.X <= 1000000 && p.Y >= 0 && p.Y <= 1000000;
            Require(d.Regions.All(r => r != null && Text(r.NodeId) && Text(r.DisplayName) && (r.SystemId == null || Text(r.SystemId))
                && Point(r.Center) && r.Boundary != null && r.Boundary.Count >= 3 && r.Boundary.Count <= 104 && r.Boundary.All(Point)),
                "Invalid generated region or polygon.");
            Require(d.Systems.All(s => s != null && Text(s.SystemId) && Text(s.DisplayName) && Text(s.NodeId))
                && d.Connections.All(c => c != null && Text(c.ConnectionId) && Text(c.EndpointA) && Text(c.EndpointB))
                && d.HomeCandidates.All(h => h != null && Text(h.CivilizationDefinitionId) && Text(h.SystemId) && Text(h.NodeId)),
                "Invalid generated system, connection or candidate home.");
        }

        // Canonical length-prefixed UTF8/binary encoding; independent of JSON property order,
        // culture, newline conversion and enumeration order. Hash is not an authentication claim.
        internal static string Hash(GeneratedGalaxyDocumentDto d)
        {
            using (var stream = new MemoryStream())
            {
                using (var w = new BinaryWriter(stream, Encoding.UTF8, true))
                {
                    w.Write("HobbyDS9 generated map canonical v1");
                    w.Write(d.FormatVersion); w.Write(d.ProfileId); w.Write(d.GeneratorVersion); w.Write(d.TuningVersion);
                    w.Write(d.Settings.Seed); w.Write(d.Settings.SystemCount);
                    w.Write(d.Settings.HomeCivilizationDefinitionIds.Count);
                    foreach (var id in d.Settings.HomeCivilizationDefinitionIds.OrderBy(id => id, StringComparer.Ordinal)) w.Write(id);
                    w.Write(d.Width); w.Write(d.Height);
                    w.Write(d.Regions.Count);
                    foreach (var r in d.Regions.OrderBy(r => r.NodeId, StringComparer.Ordinal))
                    {
                        w.Write(r.NodeId); w.Write(r.DisplayName); w.Write(r.SystemId != null); if (r.SystemId != null) w.Write(r.SystemId);
                        w.Write(r.Center.X); w.Write(r.Center.Y); w.Write(r.Boundary.Count);
                        foreach (var p in r.Boundary) { w.Write(p.X); w.Write(p.Y); }
                    }
                    w.Write(d.Systems.Count);
                    foreach (var s in d.Systems.OrderBy(s => s.SystemId, StringComparer.Ordinal)) { w.Write(s.SystemId); w.Write(s.DisplayName); w.Write(s.NodeId); }
                    w.Write(d.Connections.Count);
                    foreach (var c in d.Connections.OrderBy(c => c.ConnectionId, StringComparer.Ordinal))
                    { w.Write(c.ConnectionId); w.Write(c.EndpointA); w.Write(c.EndpointB); w.Write((int)c.Kind); w.Write(c.BaseTraversalCost); }
                    w.Write(d.HomeCandidates.Count);
                    foreach (var h in d.HomeCandidates.OrderBy(h => h.CivilizationDefinitionId, StringComparer.Ordinal))
                    { w.Write(h.CivilizationDefinitionId); w.Write(h.SystemId); w.Write(h.NodeId); }
                }
                using (var sha = SHA256.Create())
                    return string.Concat(sha.ComputeHash(stream.ToArray()).Select(b => b.ToString("x2", CultureInfo.InvariantCulture)));
            }
        }

        internal static Dictionary<string, List<string>> Neighbors(IEnumerable<GeneratedGalaxyRegionDto> regions, IEnumerable<NavigationConnectionDto> edges)
        {
            var result = regions.ToDictionary(r => r.NodeId, _ => new List<string>(), StringComparer.Ordinal);
            foreach (var e in edges) { result[e.EndpointA].Add(e.EndpointB); result[e.EndpointB].Add(e.EndpointA); }
            foreach (var list in result.Values) list.Sort(StringComparer.Ordinal);
            return result;
        }
        internal static Dictionary<string, int> Distances(Dictionary<string, List<string>> neighbors, string origin)
        {
            var distances = new Dictionary<string, int>(StringComparer.Ordinal) { [origin] = 0 };
            var queue = new Queue<string>(); queue.Enqueue(origin);
            while (queue.Count > 0)
            {
                var node = queue.Dequeue();
                foreach (var next in neighbors[node])
                    if (!distances.ContainsKey(next)) { distances[next] = distances[node] + 1; queue.Enqueue(next); }
            }
            return distances;
        }

        internal static GeneratedGalaxyDocumentDto Copy(GeneratedGalaxyDocumentDto d)
        {
            GeneratedGalaxyPointDto Point(GeneratedGalaxyPointDto p) => new GeneratedGalaxyPointDto { X = p.X, Y = p.Y };
            return new GeneratedGalaxyDocumentDto {
                FormatVersion = d.FormatVersion, ProfileId = d.ProfileId, GeneratorVersion = d.GeneratorVersion, TuningVersion = d.TuningVersion,
                ContentHash = d.ContentHash, Width = d.Width, Height = d.Height,
                Settings = new GeneratedGalaxySettingsDto { Seed = d.Settings.Seed, SystemCount = d.Settings.SystemCount,
                    HomeCivilizationDefinitionIds = new List<string>(d.Settings.HomeCivilizationDefinitionIds) },
                Regions = d.Regions.Select(r => new GeneratedGalaxyRegionDto { NodeId = r.NodeId, DisplayName = r.DisplayName,
                    SystemId = r.SystemId, Center = Point(r.Center), Boundary = r.Boundary.Select(Point).ToList() }).ToList(),
                Systems = d.Systems.Select(s => new GeneratedGalaxySystemDto { SystemId = s.SystemId, DisplayName = s.DisplayName, NodeId = s.NodeId }).ToList(),
                Connections = d.Connections.Select(c => new NavigationConnectionDto { ConnectionId = c.ConnectionId, EndpointA = c.EndpointA,
                    EndpointB = c.EndpointB, Kind = c.Kind, BaseTraversalCost = c.BaseTraversalCost }).ToList(),
                HomeCandidates = d.HomeCandidates.Select(h => new GeneratedGalaxyHomeCandidateDto { CivilizationDefinitionId = h.CivilizationDefinitionId,
                    NodeId = h.NodeId, SystemId = h.SystemId }).ToList()
            };
        }
    }
}
