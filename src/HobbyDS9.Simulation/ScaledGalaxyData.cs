using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using HobbyDS9.Contracts;

namespace HobbyDS9.Simulation
{
    internal static class ScaledGalaxyData
    {
        internal static void Require(bool condition, string message)
        { if (!condition) throw new ArgumentException(message); }

        internal static ScaledGalaxySettingsDto Settings(ScaledGalaxySettingsDto s, IEnumerable<CivilizationDefinitionDto> catalog)
        {
            if (s == null) throw new ArgumentException("Missing scaled-map settings.");
            // Reuse only seed/catalog admission, with a valid legacy-size sentinel; never generate a legacy map.
            var admitted = GeneratedGalaxyData.Settings(new GeneratedGalaxySettingsDto { Seed = s.Seed, SystemCount = 50,
                HomeCivilizationDefinitionIds = s.HomeCivilizationDefinitionIds }, catalog);
            Require(s.SystemCount >= 30 && s.SystemCount <= ScaledGalaxyIdentity.MaximumSystems
                && s.EmptyRegionCount >= (s.SystemCount + 1) / 2 && s.EmptyRegionCount <= s.SystemCount * 2
                && (long)s.SystemCount + s.EmptyRegionCount <= ScaledGalaxyIdentity.MaximumRegions, "Scaled-map counts exceed supported bounds.");
            Require(s.Width >= 500000 && s.Width <= ScaledGalaxyIdentity.MaximumCoordinate
                && s.Height >= 500000 && s.Height <= ScaledGalaxyIdentity.MaximumCoordinate
                && s.Width <= 2L * s.Height && s.Height <= 2L * s.Width, "Scaled-map extents/aspect exceed supported bounds.");
            long area = (long)s.Width * s.Height;
            Require(area >= s.SystemCount * 10000000000L && area <= s.SystemCount * 30000000000L
                && area >= ((long)s.SystemCount + s.EmptyRegionCount) * 4000000000L, "Scaled-map density exceeds supported bounds.");
            Require(s.QuietEmptyAreaTargetPermille == 1000 || s.QuietEmptyAreaTargetPermille == 2000 || s.QuietEmptyAreaTargetPermille == 3000,
                "Unsupported quiet-empty target profile.");
            Require(s.BlackHolesPerBaselineArea >= 0 && s.BlackHolesPerBaselineArea <= 4
                && s.NebulaePerBaselineArea >= 0 && s.NebulaePerBaselineArea <= 6, "Feature densities exceed supported bounds.");
            return new ScaledGalaxySettingsDto { Seed = admitted.Seed, SystemCount = s.SystemCount, EmptyRegionCount = s.EmptyRegionCount,
                Width = s.Width, Height = s.Height, QuietEmptyAreaTargetPermille = s.QuietEmptyAreaTargetPermille,
                BlackHolesPerBaselineArea = s.BlackHolesPerBaselineArea, NebulaePerBaselineArea = s.NebulaePerBaselineArea,
                HomeCivilizationDefinitionIds = admitted.HomeCivilizationDefinitionIds, StellarInfluenceEnabled = s.StellarInfluenceEnabled,
                BlackHoleInfluenceEnabled = s.BlackHoleInfluenceEnabled, NebulaInfluenceEnabled = s.NebulaInfluenceEnabled };
        }

        internal static EnvironmentGalaxySettingsDto EnvironmentSettings(ScaledGalaxySettingsDto s) => new EnvironmentGalaxySettingsDto {
            Map = new GeneratedGalaxySettingsDto { Seed = s.Seed, SystemCount = s.SystemCount, HomeCivilizationDefinitionIds = s.HomeCivilizationDefinitionIds },
            StellarInfluenceEnabled = s.StellarInfluenceEnabled, BlackHoleInfluenceEnabled = s.BlackHoleInfluenceEnabled, NebulaInfluenceEnabled = s.NebulaInfluenceEnabled };

        // Encoding/copy reuse only. This temporary object is never persisted or submitted to old Open.
        private static EnvironmentGalaxyDocumentDto Envelope(ScaledGalaxyDocumentDto d) => new EnvironmentGalaxyDocumentDto {
            FormatVersion = d.FormatVersion, ProfileId = d.ProfileId, GeneratorVersion = d.GeneratorVersion, TuningVersion = d.TuningVersion,
            ContentHash = d.ContentHash, Settings = EnvironmentSettings(d.Settings), Tuning = d.Tuning.Environment,
            Features = d.Features, Layout = d.Layout, RegionInfluences = d.RegionInfluences };

        internal static string Hash(ScaledGalaxyDocumentDto d)
        {
            using (var bytes = new MemoryStream())
            {
                using (var w = new BinaryWriter(bytes, Encoding.UTF8, true))
                {
                    w.Write("HobbyDS9 scaled environment map canonical v1");
                    w.Write(EnvironmentGalaxyData.Hash(Envelope(d)));
                    var s = d.Settings; var t = d.Tuning;
                    w.Write(s.EmptyRegionCount); w.Write(s.Width); w.Write(s.Height); w.Write(s.QuietEmptyAreaTargetPermille);
                    w.Write(s.BlackHolesPerBaselineArea); w.Write(s.NebulaePerBaselineArea);
                    w.Write(t.StarClusterSize); w.Write(t.ClusterRadius); w.Write(t.MinimumClusterSeparation);
                    w.Write(t.ExtraSiteSeparation); w.Write(t.QuietDisturbanceThreshold);
                }
                using (var sha = SHA256.Create())
                    return string.Concat(sha.ComputeHash(bytes.ToArray()).Select(b => b.ToString("x2", CultureInfo.InvariantCulture)));
            }
        }

        internal static void Shape(ScaledGalaxyDocumentDto d)
        {
            if (d == null || d.FormatVersion != ScaledGalaxyIdentity.FormatVersion || d.ProfileId != ScaledGalaxyIdentity.Profile
                || d.GeneratorVersion != ScaledGalaxyIdentity.GeneratorVersion || d.TuningVersion != ScaledGalaxyIdentity.TuningVersion)
                throw new ArgumentException("Unsupported scaled-map version. No migration is available.");
            if (d.Settings == null || d.Tuning == null || d.Tuning.Environment == null || d.Layout == null
                || d.Layout.Regions == null || d.Layout.Systems == null || d.Layout.Connections == null || d.Layout.HomeCandidates == null
                || d.Features == null || d.RegionInfluences == null) throw new ArgumentException("Missing scaled-map data.");
            var l = d.Layout;
            Require(d.ContentHash != null && d.ContentHash.Length == 64 && d.ContentHash.All(c => c >= '0' && c <= '9' || c >= 'a' && c <= 'f'), "Invalid scaled-map hash.");
            Require(l.Width >= 500000 && l.Width <= ScaledGalaxyIdentity.MaximumCoordinate && l.Height >= 500000 && l.Height <= ScaledGalaxyIdentity.MaximumCoordinate,
                "Invalid layout extents.");
            Require(l.Regions.Count >= 45 && l.Regions.Count <= ScaledGalaxyIdentity.MaximumRegions
                && l.Systems.Count >= 30 && l.Systems.Count <= ScaledGalaxyIdentity.MaximumSystems
                && l.Connections.Count > 0 && l.Connections.Count <= ScaledGalaxyIdentity.MaximumConnections
                && l.HomeCandidates.Count == 2 && d.Features.Count >= 30 && d.Features.Count <= ScaledGalaxyIdentity.MaximumFeatures
                && d.RegionInfluences.Count == l.Regions.Count, "Scaled-map collection budget exceeded.");
            bool Text(string? text) => text != null && text.Length > 0 && text.Length <= 128;
            bool Point(GeneratedGalaxyPointDto? p) => p != null && p.X >= 0 && p.X <= l.Width && p.Y >= 0 && p.Y <= l.Height;
            Require(l.Regions.All(r => r != null && Text(r.NodeId) && Text(r.DisplayName) && (r.SystemId == null || Text(r.SystemId))
                && Point(r.Center) && r.Boundary != null && r.Boundary.Count >= 3 && r.Boundary.Count <= ScaledGalaxyIdentity.MaximumVerticesPerRegion
                && r.Boundary.All(Point)) && l.Regions.Sum(r => (long)r.Boundary.Count) <= ScaledGalaxyIdentity.MaximumTotalVertices, "Invalid scaled region/vertex budget.");
            Require(l.Systems.All(s => s != null && Text(s.SystemId) && Text(s.NodeId) && Text(s.DisplayName))
                && l.Connections.All(c => c != null && Text(c.ConnectionId) && Text(c.EndpointA) && Text(c.EndpointB))
                && l.HomeCandidates.All(h => h != null && Text(h.CivilizationDefinitionId) && Text(h.SystemId) && Text(h.NodeId)), "Invalid scaled topology row.");
            Require(d.Features.All(f => f != null && Text(f.FeatureId) && Text(f.DisplayName) && Point(f.Position)
                && Enum.IsDefined(typeof(GalaxyEnvironmentFeatureKind), f.Kind) && f.RadiusX > 0 && f.RadiusX <= 300000
                && f.RadiusY > 0 && f.RadiusY <= 300000 && f.DensityWeight > 0 && f.DensityWeight <= 1000
                && (f.SystemId == null || Text(f.SystemId)) && (f.NodeId == null || Text(f.NodeId))), "Invalid scaled feature.");
            Require(d.RegionInfluences.All(r => r != null && Text(r.NodeId) && r.Contributions != null && r.Contributions.Count <= d.Features.Count
                && r.Contributions.All(c => c != null && Text(c.FeatureId) && c.Amount > 0 && c.Amount <= 1000))
                && d.RegionInfluences.Sum(r => (long)r.Contributions.Count) <= ScaledGalaxyIdentity.MaximumTotalContributions, "Invalid scaled contribution budget.");
        }

        internal static ScaledGalaxyDocumentDto Open(ScaledGalaxyDocumentDto d, IEnumerable<CivilizationDefinitionDto> catalog)
        {
            Shape(d);
            var settings = Settings(d.Settings, catalog);
            var expected = ScaledGalaxyGenerator.Generate(settings, catalog);
            Require(d.ContentHash == expected.ContentHash && Hash(d) == expected.ContentHash, "Scaled map differs from supported deterministic generation.");
            return expected;
        }

        internal static ScaledGalaxyDocumentDto Copy(ScaledGalaxyDocumentDto d)
        {
            var copied = EnvironmentGalaxyData.Copy(Envelope(d)); var s = d.Settings; var t = d.Tuning;
            return new ScaledGalaxyDocumentDto { FormatVersion = d.FormatVersion, ProfileId = d.ProfileId, GeneratorVersion = d.GeneratorVersion,
                TuningVersion = d.TuningVersion, ContentHash = d.ContentHash,
                Settings = new ScaledGalaxySettingsDto { Seed = s.Seed, SystemCount = s.SystemCount, EmptyRegionCount = s.EmptyRegionCount,
                    Width = s.Width, Height = s.Height, QuietEmptyAreaTargetPermille = s.QuietEmptyAreaTargetPermille,
                    BlackHolesPerBaselineArea = s.BlackHolesPerBaselineArea, NebulaePerBaselineArea = s.NebulaePerBaselineArea,
                    HomeCivilizationDefinitionIds = copied.Settings.Map.HomeCivilizationDefinitionIds,
                    StellarInfluenceEnabled = s.StellarInfluenceEnabled, BlackHoleInfluenceEnabled = s.BlackHoleInfluenceEnabled, NebulaInfluenceEnabled = s.NebulaInfluenceEnabled },
                Tuning = new ScaledGalaxyTuningDto { Environment = copied.Tuning, StarClusterSize = t.StarClusterSize, ClusterRadius = t.ClusterRadius,
                    MinimumClusterSeparation = t.MinimumClusterSeparation, ExtraSiteSeparation = t.ExtraSiteSeparation, QuietDisturbanceThreshold = t.QuietDisturbanceThreshold },
                Features = copied.Features, Layout = copied.Layout, RegionInfluences = copied.RegionInfluences };
        }
    }
}
