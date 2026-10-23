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
    internal static class NaturalGalaxyData
    {
        private static void Require(bool condition, string message) { if (!condition) throw new ArgumentException(message); }
        internal static NaturalGalaxySettingsDto Settings(NaturalGalaxySettingsDto s, IEnumerable<CivilizationDefinitionDto> catalog)
        {
            if (s == null || s.Map == null) throw new ArgumentException("Missing natural-map settings.");
            var map = ScaledGalaxyData.Settings(s.Map, catalog);
            Require(s.IsolatedStarSharePermille >= 100 && s.IsolatedStarSharePermille <= 300
                && s.AssociationSpreadPermille >= 500 && s.AssociationSpreadPermille <= 1500, "Natural distribution settings exceed bounded review tuning.");
            return new NaturalGalaxySettingsDto { Map = map, IsolatedStarSharePermille = s.IsolatedStarSharePermille,
                AssociationSpreadPermille = s.AssociationSpreadPermille };
        }

        // Reuse only pure canonical topology/feature encoding and detached copies. Never old Open/Generate.
        private static EnvironmentGalaxyDocumentDto EncodingEnvelope(NaturalGalaxyDocumentDto d) => new EnvironmentGalaxyDocumentDto {
            FormatVersion = d.FormatVersion, ProfileId = d.ProfileId, GeneratorVersion = d.GeneratorVersion, TuningVersion = d.TuningVersion,
            ContentHash = d.ContentHash, Settings = ScaledGalaxyData.EnvironmentSettings(d.Settings.Map), Tuning = d.Tuning.Environment,
            Features = d.Features, Layout = d.Layout, RegionInfluences = d.RegionInfluences };

        // Common bounded row validation only, behind the exact natural outer-header check.
        // This temporary adapter is never serialized, returned or submitted to a legacy reader.
        private static ScaledGalaxyDocumentDto BoundsEnvelope(NaturalGalaxyDocumentDto d) => new ScaledGalaxyDocumentDto {
            FormatVersion = ScaledGalaxyIdentity.FormatVersion, ProfileId = ScaledGalaxyIdentity.Profile,
            GeneratorVersion = ScaledGalaxyIdentity.GeneratorVersion, TuningVersion = ScaledGalaxyIdentity.TuningVersion,
            ContentHash = d.ContentHash, Settings = d.Settings.Map,
            Tuning = new ScaledGalaxyTuningDto { Environment = d.Tuning.Environment }, Features = d.Features, Layout = d.Layout, RegionInfluences = d.RegionInfluences };

        internal static string Hash(NaturalGalaxyDocumentDto d)
        {
            using (var bytes = new MemoryStream())
            {
                using (var w = new BinaryWriter(bytes, Encoding.UTF8, true))
                {
                    w.Write("HobbyDS9 natural environment map canonical v1");
                    w.Write(EnvironmentGalaxyData.Hash(EncodingEnvelope(d)));
                    var s = d.Settings; var m = s.Map; var t = d.Tuning;
                    w.Write(m.EmptyRegionCount); w.Write(m.Width); w.Write(m.Height); w.Write(m.QuietEmptyAreaTargetPermille);
                    w.Write(m.BlackHolesPerBaselineArea); w.Write(m.NebulaePerBaselineArea);
                    w.Write(s.IsolatedStarSharePermille); w.Write(s.AssociationSpreadPermille);
                    w.Write(t.IsolatedStarSeparation); w.Write(t.MinimumAssociationSize); w.Write(t.MaximumAssociationSize);
                    w.Write(t.MinimumAssociationRadius); w.Write(t.MaximumAssociationRadius);
                    w.Write(t.MinimumAssociationSeparation); w.Write(t.MaximumAssociationSeparation);
                    w.Write(t.ExtraSiteSeparation); w.Write(t.StellarPlacementPressurePermille); w.Write(t.QuietDisturbanceThreshold);
                    w.Write(d.StarGroups.Count);
                    foreach (var g in d.StarGroups.OrderBy(g => g.GroupId, StringComparer.Ordinal))
                    {
                        w.Write(g.GroupId); w.Write((int)g.Kind); w.Write(g.Center.X); w.Write(g.Center.Y); w.Write(g.RadiusX); w.Write(g.RadiusY);
                        w.Write(g.StarFeatureIds.Count); foreach (var id in g.StarFeatureIds.OrderBy(id => id, StringComparer.Ordinal)) w.Write(id);
                    }
                }
                using (var sha = SHA256.Create())
                    return string.Concat(sha.ComputeHash(bytes.ToArray()).Select(b => b.ToString("x2", CultureInfo.InvariantCulture)));
            }
        }

        internal static void Shape(NaturalGalaxyDocumentDto d)
        {
            if (d == null || d.FormatVersion != NaturalGalaxyIdentity.FormatVersion || d.ProfileId != NaturalGalaxyIdentity.Profile
                || d.GeneratorVersion != NaturalGalaxyIdentity.GeneratorVersion || d.TuningVersion != NaturalGalaxyIdentity.TuningVersion)
                throw new ArgumentException("Unsupported natural-map version. No migration is available.");
            if (d.Settings == null || d.Settings.Map == null || d.Tuning == null || d.Tuning.Environment == null || d.StarGroups == null)
                throw new ArgumentException("Missing natural-map data.");
            ScaledGalaxyData.Shape(BoundsEnvelope(d));
            bool Text(string? s) => s != null && s.Length > 0 && s.Length <= 128;
            Require(d.StarGroups.Count > 0 && d.StarGroups.Count <= ScaledGalaxyIdentity.MaximumSystems, "Natural group budget exceeded.");
            Require(d.StarGroups.All(g => g != null && Text(g.GroupId) && g.Center != null
                && g.Center.X >= 0 && g.Center.X <= d.Layout.Width && g.Center.Y >= 0 && g.Center.Y <= d.Layout.Height
                && g.StarFeatureIds != null && g.StarFeatureIds.Count >= 1 && g.StarFeatureIds.Count <= 9 && g.StarFeatureIds.All(Text)
                && (g.Kind == NaturalStarGroupKind.Isolated && g.RadiusX == 0 && g.RadiusY == 0 && g.StarFeatureIds.Count == 1
                    || g.Kind == NaturalStarGroupKind.LooseAssociation && g.RadiusX > 0 && g.RadiusX <= 200000
                        && g.RadiusY > 0 && g.RadiusY <= 200000 && g.StarFeatureIds.Count >= 2))
                && d.StarGroups.Sum(g => (long)g.StarFeatureIds.Count) <= ScaledGalaxyIdentity.MaximumSystems, "Invalid natural group membership or geometry.");
        }

        internal static NaturalGalaxyDocumentDto Open(NaturalGalaxyDocumentDto d, IEnumerable<CivilizationDefinitionDto> catalog)
        {
            Shape(d); var settings = Settings(d.Settings, catalog); var expected = NaturalGalaxyGenerator.Generate(settings, catalog);
            Require(d.ContentHash == expected.ContentHash && Hash(d) == expected.ContentHash, "Natural map differs from supported deterministic generation.");
            return expected;
        }

        internal static NaturalGalaxyDocumentDto Copy(NaturalGalaxyDocumentDto d)
        {
            var copy = EnvironmentGalaxyData.Copy(EncodingEnvelope(d)); var s = d.Settings; var m = s.Map; var t = d.Tuning;
            return new NaturalGalaxyDocumentDto { FormatVersion = d.FormatVersion, ProfileId = d.ProfileId, GeneratorVersion = d.GeneratorVersion,
                TuningVersion = d.TuningVersion, ContentHash = d.ContentHash,
                Settings = new NaturalGalaxySettingsDto { IsolatedStarSharePermille = s.IsolatedStarSharePermille, AssociationSpreadPermille = s.AssociationSpreadPermille,
                    Map = new ScaledGalaxySettingsDto { Seed = m.Seed, SystemCount = m.SystemCount, EmptyRegionCount = m.EmptyRegionCount,
                        Width = m.Width, Height = m.Height, QuietEmptyAreaTargetPermille = m.QuietEmptyAreaTargetPermille,
                        BlackHolesPerBaselineArea = m.BlackHolesPerBaselineArea, NebulaePerBaselineArea = m.NebulaePerBaselineArea,
                        HomeCivilizationDefinitionIds = copy.Settings.Map.HomeCivilizationDefinitionIds,
                        StellarInfluenceEnabled = m.StellarInfluenceEnabled, BlackHoleInfluenceEnabled = m.BlackHoleInfluenceEnabled, NebulaInfluenceEnabled = m.NebulaInfluenceEnabled } },
                Tuning = new NaturalGalaxyTuningDto { Environment = copy.Tuning, IsolatedStarSeparation = t.IsolatedStarSeparation,
                    MinimumAssociationSize = t.MinimumAssociationSize, MaximumAssociationSize = t.MaximumAssociationSize,
                    MinimumAssociationRadius = t.MinimumAssociationRadius, MaximumAssociationRadius = t.MaximumAssociationRadius,
                    MinimumAssociationSeparation = t.MinimumAssociationSeparation, MaximumAssociationSeparation = t.MaximumAssociationSeparation,
                    ExtraSiteSeparation = t.ExtraSiteSeparation, StellarPlacementPressurePermille = t.StellarPlacementPressurePermille,
                    QuietDisturbanceThreshold = t.QuietDisturbanceThreshold }, Features = copy.Features, Layout = copy.Layout, RegionInfluences = copy.RegionInfluences,
                StarGroups = d.StarGroups.Select(g => new NaturalStarGroupDto { GroupId = g.GroupId, Kind = g.Kind,
                    Center = new GeneratedGalaxyPointDto { X = g.Center.X, Y = g.Center.Y }, RadiusX = g.RadiusX, RadiusY = g.RadiusY,
                    StarFeatureIds = new List<string>(g.StarFeatureIds) }).ToList() };
        }
    }
}
