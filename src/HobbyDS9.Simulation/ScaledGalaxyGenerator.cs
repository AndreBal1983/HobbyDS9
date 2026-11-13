using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using HobbyDS9.Contracts;

namespace HobbyDS9.Simulation
{
    internal static class ScaledGalaxyGenerator
    {
        internal static ScaledGalaxyTuningDto Tuning(ScaledGalaxySettingsDto s)
        {
            long area = (long)s.Width * s.Height;
            int Count(int density) => (int)((area * density + 500000000000L) / 1000000000000L);
            return new ScaledGalaxyTuningDto { Environment = new EnvironmentGalaxyTuningDto {
                BaseDensity = 100, MaximumDensity = 1000, MinimumSiteSeparation = 8000, MinimumStarSeparation = 12000,
                OuterMargin = 20000, BlackHoleCount = Count(s.BlackHolesPerBaselineArea), NebulaCount = Count(s.NebulaePerBaselineArea),
                CandidateDrawsPerRegion = 1024, PlacementAttempts = 8 }, StarClusterSize = 1 + s.QuietEmptyAreaTargetPermille / 1000,
                ClusterRadius = 30000, MinimumClusterSeparation = 100000, ExtraSiteSeparation = 30000, QuietDisturbanceThreshold = 100 };
        }

        internal static ScaledGalaxyDocumentDto Generate(ScaledGalaxySettingsDto settings, IEnumerable<CivilizationDefinitionDto> catalog)
        {
            var s = ScaledGalaxyData.Settings(settings, catalog); var t = Tuning(s);
            ulong seed = ulong.Parse(s.Seed, CultureInfo.InvariantCulture);
            var features = Features(s, t, seed);
            var random = new SplitMix64Random(seed ^ 0x5343414c53495445UL);
            for (int attempt = 0; attempt < t.Environment.PlacementAttempts; attempt++)
            {
                try { return Build(s, t, features, random); }
                catch (InvalidOperationException) { /* Features remain fixed; only extra-site stream advances. */ }
            }
            throw new ArgumentException("Scaled map exhausted eight bounded placement/geometry attempts.");
        }

        private static long Distance(GeneratedGalaxyPointDto a, GeneratedGalaxyPointDto b)
            => (long)(a.X - b.X) * (a.X - b.X) + (long)(a.Y - b.Y) * (a.Y - b.Y);
        private static string Number(int i) => (i + 1).ToString("D4", CultureInfo.InvariantCulture);
        private static string Id(string prefix, int i) => prefix + ".scaled." + Number(i);
        private static GeneratedGalaxyPointDto Point(SplitMix64Random r, int width, int height, int mx, int my)
            => new GeneratedGalaxyPointDto { X = mx + (int)(r.NextUInt64() % (ulong)(width - 2 * mx + 1)),
                Y = my + (int)(r.NextUInt64() % (ulong)(height - 2 * my + 1)) };

        private static List<EnvironmentGalaxyFeatureDto> Features(ScaledGalaxySettingsDto s, ScaledGalaxyTuningDto t, ulong seed)
        {
            var stars = new SplitMix64Random(seed ^ 0x5343414c53544152UL);
            var holes = new SplitMix64Random(seed ^ 0x5343414c484f4c45UL);
            var nebulae = new SplitMix64Random(seed ^ 0x5343414c4e454255UL);
            var features = new List<EnvironmentGalaxyFeatureDto>(); var centers = new List<GeneratedGalaxyPointDto>();
            var e = t.Environment; int budget = s.SystemCount * e.CandidateDrawsPerRegion;
            for (int index = 0; index < s.SystemCount;)
            {
                GeneratedGalaxyPointDto? center = null;
                for (int draw = 0; draw < budget; draw++)
                {
                    var p = Point(stars, s.Width, s.Height, e.OuterMargin + t.ClusterRadius, e.OuterMargin + t.ClusterRadius);
                    if (centers.All(c => Distance(c, p) >= (long)t.MinimumClusterSeparation * t.MinimumClusterSeparation)) { center = p; break; }
                }
                if (center == null) throw new ArgumentException("Star clusters exhausted their finite placement budget.");
                centers.Add(center);
                int end = Math.Min(s.SystemCount, index + t.StarClusterSize);
                for (; index < end; index++)
                {
                    GeneratedGalaxyPointDto? position = null;
                    for (int draw = 0; draw < budget; draw++)
                    {
                        int dx = (int)(stars.NextUInt64() % (ulong)(2 * t.ClusterRadius + 1)) - t.ClusterRadius;
                        int dy = (int)(stars.NextUInt64() % (ulong)(2 * t.ClusterRadius + 1)) - t.ClusterRadius;
                        if ((long)dx * dx + (long)dy * dy > (long)t.ClusterRadius * t.ClusterRadius) continue;
                        var p = new GeneratedGalaxyPointDto { X = center.X + dx, Y = center.Y + dy };
                        if (features.All(f => Distance(f.Position, p) >= (long)e.MinimumStarSeparation * e.MinimumStarSeparation)) { position = p; break; }
                    }
                    if (position == null) throw new ArgumentException("Cluster stars exhausted their finite placement budget.");
                    int band = (int)(stars.NextUInt64() % 3);
                    features.Add(new EnvironmentGalaxyFeatureDto { FeatureId = Id("feature.star", index), Kind = GalaxyEnvironmentFeatureKind.Star,
                        DisplayName = "Star " + Number(index), Position = position, RadiusX = 70000 + band * 25000, RadiusY = 70000 + band * 25000,
                        DensityWeight = 120 + band * 80, SystemId = Id("system", index), NodeId = Id("nav", index) });
                }
            }
            for (int index = 0; index < e.BlackHoleCount; index++)
            {
                GeneratedGalaxyPointDto? position = null;
                for (int draw = 0; draw < budget; draw++)
                {
                    var p = Point(holes, s.Width, s.Height, e.OuterMargin, e.OuterMargin);
                    if (features.All(f => Distance(f.Position, p) >= (long)t.ExtraSiteSeparation * t.ExtraSiteSeparation)) { position = p; break; }
                }
                if (position == null) throw new ArgumentException("Black holes exhausted their finite placement budget.");
                features.Add(new EnvironmentGalaxyFeatureDto { FeatureId = Id("feature.black-hole", index), Kind = GalaxyEnvironmentFeatureKind.BlackHole,
                    DisplayName = "Black hole " + Number(index), Position = position, RadiusX = 160000, RadiusY = 160000, DensityWeight = 650,
                    NodeId = Id("nav", s.SystemCount + index) });
            }
            for (int index = 0; index < e.NebulaCount; index++)
            {
                int rx = Math.Min(180000 + (int)(nebulae.NextUInt64() % 60001), s.Width / 2 - e.OuterMargin);
                int ry = Math.Min(100000 + (int)(nebulae.NextUInt64() % 60001), s.Height / 2 - e.OuterMargin);
                features.Add(new EnvironmentGalaxyFeatureDto { FeatureId = Id("feature.nebula", index), Kind = GalaxyEnvironmentFeatureKind.Nebula,
                    DisplayName = "Nebula " + Number(index), Position = Point(nebulae, s.Width, s.Height, rx + e.OuterMargin, ry + e.OuterMargin),
                    RadiusX = rx, RadiusY = ry, DensityWeight = 420 });
            }
            return features.OrderBy(f => f.FeatureId, StringComparer.Ordinal).ToList();
        }

        private static ScaledGalaxyDocumentDto Build(ScaledGalaxySettingsDto s, ScaledGalaxyTuningDto t, List<EnvironmentGalaxyFeatureDto> features, SplitMix64Random random)
        {
            var e = t.Environment; var environment = ScaledGalaxyData.EnvironmentSettings(s);
            int count = s.SystemCount + s.EmptyRegionCount;
            var sites = features.Where(f => f.NodeId != null).OrderBy(f => f.NodeId, StringComparer.Ordinal)
                .Select(f => new GeneratedGalaxyPointDto { X = f.Position.X, Y = f.Position.Y }).ToList();
            for (int draw = 0; draw < count * e.CandidateDrawsPerRegion && sites.Count < count; draw++)
            {
                var candidate = Point(random, s.Width, s.Height, e.OuterMargin, e.OuterMargin);
                int roll = (int)(random.NextUInt64() % (ulong)e.MaximumDensity);
                int density = EnvironmentGalaxyGenerator.Sample("", candidate, environment, e, features).TargetDensity;
                if (roll < density && sites.All(p => Distance(p, candidate) >= (long)t.ExtraSiteSeparation * t.ExtraSiteSeparation)) sites.Add(candidate);
            }
            if (sites.Count != count) throw new InvalidOperationException("Extra sites exhausted bounded candidate budget.");
            var systems = features.Where(f => f.Kind == GalaxyEnvironmentFeatureKind.Star).OrderBy(f => f.SystemId, StringComparer.Ordinal)
                .Select((f, i) => new GeneratedGalaxySystemDto { SystemId = f.SystemId!, NodeId = f.NodeId!, DisplayName = "System " + Number(i) }).ToList();
            var systemsByNode = systems.ToDictionary(v => v.NodeId, v => v.SystemId, StringComparer.Ordinal);
            var regions = sites.Select((p, i) => new GeneratedGalaxyRegionDto { NodeId = Id("nav", i), DisplayName = "Region " + Number(i),
                SystemId = systemsByNode.TryGetValue(Id("nav", i), out var system) ? system : null, Center = p,
                Boundary = GeneratedGalaxyGeometry.Cell(sites, i, s.Width, s.Height) }).ToList();
            if (regions.Any(r => r.Boundary.Count > ScaledGalaxyIdentity.MaximumVerticesPerRegion)
                || regions.Sum(r => r.Boundary.Count) > ScaledGalaxyIdentity.MaximumTotalVertices) throw new InvalidOperationException("Vertex budget exceeded.");
            var connections = GeneratedGalaxyGeometry.Connect(regions, s.Width, s.Height);
            var neighbors = GeneratedGalaxyData.Neighbors(regions, connections);
            GeneratedGalaxySystemDto? first = null, second = null; int separation = -1;
            foreach (var a in systems)
            {
                var distances = GeneratedGalaxyData.Distances(neighbors, a.NodeId);
                if (distances.Count != count) throw new InvalidOperationException("Disconnected scaled graph.");
                foreach (var b in systems.Where(b => StringComparer.Ordinal.Compare(a.SystemId, b.SystemId) < 0))
                    if (distances[b.NodeId] > separation) { separation = distances[b.NodeId]; first = a; second = b; }
            }
            if (separation < 5) throw new InvalidOperationException("Homes need at least five ordinary crossings.");
            var layout = new EnvironmentGalaxyLayoutDto { Width = s.Width, Height = s.Height, Regions = regions, Systems = systems, Connections = connections,
                HomeCandidates = new[] { first!, second! }.Select((v, i) => new GeneratedGalaxyHomeCandidateDto {
                    CivilizationDefinitionId = s.HomeCivilizationDefinitionIds[i], SystemId = v.SystemId, NodeId = v.NodeId }).ToList() };
            var d = new ScaledGalaxyDocumentDto { FormatVersion = ScaledGalaxyIdentity.FormatVersion, ProfileId = ScaledGalaxyIdentity.Profile,
                GeneratorVersion = ScaledGalaxyIdentity.GeneratorVersion, TuningVersion = ScaledGalaxyIdentity.TuningVersion,
                Settings = s, Tuning = t, Features = features, Layout = layout,
                RegionInfluences = regions.Select(r => EnvironmentGalaxyGenerator.Sample(r.NodeId, r.Center, environment, e, features)).ToList(),
                ContentHash = new string('0', 64) };
            ScaledGalaxyData.Shape(d);
            d.ContentHash = ScaledGalaxyData.Hash(d);
            return d;
        }
    }
}
