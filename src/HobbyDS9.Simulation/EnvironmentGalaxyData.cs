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
    internal static class EnvironmentGalaxyData
    {
        private static void Require([DoesNotReturnIf(false)] bool condition, string reason)
        { if (!condition) throw new ArgumentException(reason); }

        // Internal reuse of canonical topology encoding/deep copy only. This is NOT a persisted
        // nested legacy document and is never submitted to a legacy Open/validation entry point.
        private static GeneratedGalaxyDocumentDto TopologyEnvelope(EnvironmentGalaxyDocumentDto d) => new GeneratedGalaxyDocumentDto {
            FormatVersion = d.FormatVersion, ProfileId = d.ProfileId, GeneratorVersion = d.GeneratorVersion, TuningVersion = d.TuningVersion,
            Settings = d.Settings.Map, Width = d.Layout.Width, Height = d.Layout.Height, Regions = d.Layout.Regions, Systems = d.Layout.Systems,
            Connections = d.Layout.Connections, HomeCandidates = d.Layout.HomeCandidates };

        internal static string Hash(EnvironmentGalaxyDocumentDto d)
        {
            using (var stream = new MemoryStream())
            {
                using (var w = new BinaryWriter(stream, Encoding.UTF8, true))
                {
                    w.Write("HobbyDS9 environment map canonical v1");
                    // Covers header, Map settings and every layout field with the established encoding.
                    w.Write(GeneratedGalaxyData.Hash(TopologyEnvelope(d)));
                    w.Write(d.Settings.StellarInfluenceEnabled); w.Write(d.Settings.BlackHoleInfluenceEnabled); w.Write(d.Settings.NebulaInfluenceEnabled);
                    var t = d.Tuning;
                    w.Write(t.BaseDensity); w.Write(t.MaximumDensity); w.Write(t.MinimumSiteSeparation); w.Write(t.MinimumStarSeparation);
                    w.Write(t.OuterMargin); w.Write(t.BlackHoleCount); w.Write(t.NebulaCount); w.Write(t.CandidateDrawsPerRegion); w.Write(t.PlacementAttempts);
                    w.Write(d.Features.Count);
                    foreach (var f in d.Features.OrderBy(f => f.FeatureId, StringComparer.Ordinal))
                    {
                        w.Write(f.FeatureId); w.Write((int)f.Kind); w.Write(f.DisplayName); w.Write(f.Position.X); w.Write(f.Position.Y);
                        w.Write(f.RadiusX); w.Write(f.RadiusY); w.Write(f.DensityWeight);
                        w.Write(f.SystemId != null); if (f.SystemId != null) w.Write(f.SystemId);
                        w.Write(f.NodeId != null); if (f.NodeId != null) w.Write(f.NodeId);
                    }
                    w.Write(d.RegionInfluences.Count);
                    foreach (var r in d.RegionInfluences.OrderBy(r => r.NodeId, StringComparer.Ordinal))
                    {
                        w.Write(r.NodeId); w.Write(r.BaseDensity); w.Write(r.TargetDensity); w.Write(r.Contributions.Count);
                        foreach (var c in r.Contributions.OrderBy(c => c.FeatureId, StringComparer.Ordinal)) { w.Write(c.FeatureId); w.Write(c.Amount); }
                    }
                }
                using (var sha = SHA256.Create())
                    return string.Concat(sha.ComputeHash(stream.ToArray()).Select(b => b.ToString("x2", CultureInfo.InvariantCulture)));
            }
        }

        internal static EnvironmentGalaxyDocumentDto ValidateAndReproduce(EnvironmentGalaxyDocumentDto d, IEnumerable<CivilizationDefinitionDto> catalog)
        {
            Require(d != null && d.FormatVersion == EnvironmentGalaxyIdentity.FormatVersion && d.ProfileId == EnvironmentGalaxyIdentity.Profile
                && d.GeneratorVersion == EnvironmentGalaxyIdentity.GeneratorVersion && d.TuningVersion == EnvironmentGalaxyIdentity.TuningVersion,
                "Unsupported environment-map format, profile, generator or tuning. No migration is available.");
            Require(d.ContentHash != null && d.ContentHash.Length == 64 && d.ContentHash.All(c => c >= '0' && c <= '9' || c >= 'a' && c <= 'f'),
                "Invalid environment-map content hash.");
            Require(d.Settings != null && d.Settings.Map != null && d.Tuning != null && d.Layout != null && d.Features != null && d.RegionInfluences != null,
                "Environment map is missing required content.");
            var l = d.Layout;
            Require(l.Regions != null && l.Systems != null && l.Connections != null && l.HomeCandidates != null,
                "Environment layout is missing collections.");
            Require(l.Regions.Count >= 60 && l.Regions.Count <= 100 && l.Systems.Count >= 30 && l.Systems.Count <= 50
                && l.Connections.Count > 0 && l.Connections.Count <= 300 && l.HomeCandidates.Count == 2
                && d.Features.Count >= 35 && d.Features.Count <= 55 && d.RegionInfluences.Count >= 60 && d.RegionInfluences.Count <= 100,
                "Environment map exceeds collection bounds.");
            bool Text(string? s) => s != null && s.Length > 0 && s.Length <= 128;
            bool Point(GeneratedGalaxyPointDto? p) => p != null && p.X >= 0 && p.X <= 1000000 && p.Y >= 0 && p.Y <= 1000000;
            Require(l.Regions.All(r => r != null && Text(r.NodeId) && Text(r.DisplayName) && (r.SystemId == null || Text(r.SystemId))
                && Point(r.Center) && r.Boundary != null && r.Boundary.Count >= 3 && r.Boundary.Count <= 104 && r.Boundary.All(Point))
                && l.Systems.All(s => s != null && Text(s.SystemId) && Text(s.NodeId) && Text(s.DisplayName))
                && l.Connections.All(c => c != null && Text(c.ConnectionId) && Text(c.EndpointA) && Text(c.EndpointB))
                && l.HomeCandidates.All(h => h != null && Text(h.CivilizationDefinitionId) && Text(h.SystemId) && Text(h.NodeId)),
                "Invalid environment topology row.");
            Require(d.Features.All(f => f != null && Text(f.FeatureId) && Text(f.DisplayName) && Point(f.Position)
                && Enum.IsDefined(typeof(GalaxyEnvironmentFeatureKind), f.Kind) && f.RadiusX > 0 && f.RadiusX <= 300000
                && f.RadiusY > 0 && f.RadiusY <= 300000 && f.DensityWeight > 0 && f.DensityWeight <= 1000
                && (f.SystemId == null || Text(f.SystemId)) && (f.NodeId == null || Text(f.NodeId))), "Invalid environment feature.");
            Require(d.RegionInfluences.All(r => r != null && Text(r.NodeId) && r.Contributions != null && r.Contributions.Count <= 55
                && r.Contributions.All(c => c != null && Text(c.FeatureId) && c.Amount > 0 && c.Amount <= 1000)), "Invalid environment influence observation.");
            var expected = EnvironmentGalaxyGenerator.Generate(d.Settings, catalog);
            Require(d.ContentHash == expected.ContentHash && Hash(d) == expected.ContentHash,
                "Environment map does not match its seed, resolved features, settings and supported version.");
            return expected;
        }

        internal static EnvironmentGalaxyDocumentDto Copy(EnvironmentGalaxyDocumentDto d)
        {
            var topology = GeneratedGalaxyData.Copy(TopologyEnvelope(d));
            var t = d.Tuning;
            return new EnvironmentGalaxyDocumentDto { FormatVersion = d.FormatVersion, ProfileId = d.ProfileId,
                GeneratorVersion = d.GeneratorVersion, TuningVersion = d.TuningVersion, ContentHash = d.ContentHash,
                Settings = new EnvironmentGalaxySettingsDto { Map = topology.Settings, StellarInfluenceEnabled = d.Settings.StellarInfluenceEnabled,
                    BlackHoleInfluenceEnabled = d.Settings.BlackHoleInfluenceEnabled, NebulaInfluenceEnabled = d.Settings.NebulaInfluenceEnabled },
                Tuning = new EnvironmentGalaxyTuningDto { BaseDensity = t.BaseDensity, MaximumDensity = t.MaximumDensity,
                    MinimumSiteSeparation = t.MinimumSiteSeparation, MinimumStarSeparation = t.MinimumStarSeparation, OuterMargin = t.OuterMargin,
                    BlackHoleCount = t.BlackHoleCount, NebulaCount = t.NebulaCount, CandidateDrawsPerRegion = t.CandidateDrawsPerRegion, PlacementAttempts = t.PlacementAttempts },
                Layout = new EnvironmentGalaxyLayoutDto { Width = topology.Width, Height = topology.Height, Regions = topology.Regions,
                    Systems = topology.Systems, Connections = topology.Connections, HomeCandidates = topology.HomeCandidates },
                Features = d.Features.Select(f => new EnvironmentGalaxyFeatureDto { FeatureId = f.FeatureId, Kind = f.Kind, DisplayName = f.DisplayName,
                    Position = new GeneratedGalaxyPointDto { X = f.Position.X, Y = f.Position.Y }, RadiusX = f.RadiusX, RadiusY = f.RadiusY,
                    DensityWeight = f.DensityWeight, SystemId = f.SystemId, NodeId = f.NodeId }).ToList(),
                RegionInfluences = d.RegionInfluences.Select(r => new EnvironmentGalaxyRegionInfluenceDto { NodeId = r.NodeId,
                    BaseDensity = r.BaseDensity, TargetDensity = r.TargetDensity, Contributions = r.Contributions.Select(c =>
                        new EnvironmentGalaxyContributionDto { FeatureId = c.FeatureId, Amount = c.Amount }).ToList() }).ToList()
            };
        }
    }
}
