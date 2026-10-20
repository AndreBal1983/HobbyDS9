using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using HobbyDS9.Contracts;

namespace HobbyDS9.Simulation
{
    internal static class EnvironmentGalaxyGenerator
    {
        internal static EnvironmentGalaxyTuningDto Tuning() => new EnvironmentGalaxyTuningDto {
            BaseDensity = 100, MaximumDensity = 1000, MinimumSiteSeparation = 30000, MinimumStarSeparation = 60000,
            OuterMargin = 15000, BlackHoleCount = 2, NebulaCount = 3, CandidateDrawsPerRegion = 1024, PlacementAttempts = 8 };

        internal static EnvironmentGalaxyDocumentDto Generate(EnvironmentGalaxySettingsDto settings, IEnumerable<CivilizationDefinitionDto> catalog)
        {
            if (settings == null) throw new ArgumentException("Missing environment generation settings.");
            var owned = new EnvironmentGalaxySettingsDto { Map = GeneratedGalaxyData.Settings(settings.Map, catalog),
                StellarInfluenceEnabled = settings.StellarInfluenceEnabled, BlackHoleInfluenceEnabled = settings.BlackHoleInfluenceEnabled,
                NebulaInfluenceEnabled = settings.NebulaInfluenceEnabled };
            var tuning = Tuning();
            int width = 1000000, height = owned.Map.SystemCount * 20000;
            ulong seed = ulong.Parse(owned.Map.Seed, CultureInfo.InvariantCulture);
            // Distinct deterministic streams; toggling influence cannot move/regenerate features.
            var features = Features(owned.Map.SystemCount, width, height, tuning, seed);
            var sitesRandom = new SplitMix64Random(seed ^ 0x454e565349544553UL);
            for (int attempt = 0; attempt < tuning.PlacementAttempts; attempt++)
            {
                try { return Build(owned, tuning, features, width, height, sitesRandom); }
                catch (InvalidOperationException) { /* Bounded placement/rounded geometry retry, fixed features. */ }
            }
            throw new ArgumentException("Environment map could not produce valid geometry within eight placement attempts.");
        }

        private static List<EnvironmentGalaxyFeatureDto> Features(int count, int width, int height, EnvironmentGalaxyTuningDto tuning, ulong seed)
        {
            var stars = new SplitMix64Random(seed ^ 0x454e565354415253UL);
            var holes = new SplitMix64Random(seed ^ 0x454e56484f4c4553UL);
            var nebulae = new SplitMix64Random(seed ^ 0x454e564e4542554cUL);
            var features = new List<EnvironmentGalaxyFeatureDto>();
            GeneratedGalaxyPointDto Anchor(SplitMix64Random random, int separation)
            {
                for (int draw = 0; draw < count * tuning.CandidateDrawsPerRegion; draw++)
                {
                    var p = Point(random, width, height, tuning.OuterMargin, tuning.OuterMargin);
                    if (features.Where(f => f.NodeId != null).All(f => DistanceSquared(f.Position, p) >= (long)separation * separation)) return p;
                }
                throw new ArgumentException("Feature anchors exhausted their bounded placement budget.");
            }
            for (int i = 0; i < count; i++)
            {
                var position = Anchor(stars, tuning.MinimumStarSeparation);
                int band = (int)(stars.NextUInt64() % 3);
                features.Add(new EnvironmentGalaxyFeatureDto { FeatureId = FeatureId("star", i), Kind = GalaxyEnvironmentFeatureKind.Star,
                    DisplayName = "Star " + Number(i), Position = position, RadiusX = 70000 + band * 25000, RadiusY = 70000 + band * 25000,
                    DensityWeight = 120 + band * 80, SystemId = GeneratedGalaxyData.Id("system", i), NodeId = GeneratedGalaxyData.Id("nav", i) });
            }
            for (int i = 0; i < tuning.BlackHoleCount; i++)
                features.Add(new EnvironmentGalaxyFeatureDto { FeatureId = FeatureId("black-hole", i), Kind = GalaxyEnvironmentFeatureKind.BlackHole,
                    DisplayName = "Black hole " + Number(i), Position = Anchor(holes, tuning.MinimumStarSeparation),
                    RadiusX = 160000, RadiusY = 160000, DensityWeight = 650, NodeId = GeneratedGalaxyData.Id("nav", count + i) });
            for (int i = 0; i < tuning.NebulaCount; i++)
            {
                int rx = 180000 + (int)(nebulae.NextUInt64() % 60001), ry = 100000 + (int)(nebulae.NextUInt64() % 60001);
                features.Add(new EnvironmentGalaxyFeatureDto { FeatureId = FeatureId("nebula", i), Kind = GalaxyEnvironmentFeatureKind.Nebula,
                    DisplayName = "Nebula " + Number(i), Position = Point(nebulae, width, height, rx + tuning.OuterMargin, ry + tuning.OuterMargin),
                    RadiusX = rx, RadiusY = ry, DensityWeight = 420 });
            }
            return features.OrderBy(f => f.FeatureId, StringComparer.Ordinal).ToList();
        }

        private static string Number(int i) => (i + 1).ToString("D3", CultureInfo.InvariantCulture);
        private static string FeatureId(string kind, int i) => "feature.generated." + kind + "." + Number(i);
        private static GeneratedGalaxyPointDto Point(SplitMix64Random random, int width, int height, int mx, int my)
            => new GeneratedGalaxyPointDto { X = mx + (int)(random.NextUInt64() % (ulong)(width - 2 * mx + 1)),
                Y = my + (int)(random.NextUInt64() % (ulong)(height - 2 * my + 1)) };
        private static long DistanceSquared(GeneratedGalaxyPointDto a, GeneratedGalaxyPointDto b)
            => (long)(a.X - b.X) * (a.X - b.X) + (long)(a.Y - b.Y) * (a.Y - b.Y);
        private static bool Enabled(EnvironmentGalaxySettingsDto s, GalaxyEnvironmentFeatureKind kind)
            => kind == GalaxyEnvironmentFeatureKind.Star ? s.StellarInfluenceEnabled
                : kind == GalaxyEnvironmentFeatureKind.BlackHole ? s.BlackHoleInfluenceEnabled : s.NebulaInfluenceEnabled;

        internal static int Contribution(EnvironmentGalaxyFeatureDto feature, GeneratedGalaxyPointDto point)
        {
            long dx = point.X - feature.Position.X, dy = point.Y - feature.Position.Y;
            if (Math.Abs(dx) >= feature.RadiusX || Math.Abs(dy) >= feature.RadiusY) return 0;
            const long scale = 1000000;
            long elliptical = dx * dx * scale / ((long)feature.RadiusX * feature.RadiusX)
                + dy * dy * scale / ((long)feature.RadiusY * feature.RadiusY);
            return elliptical >= scale ? 0 : (int)(feature.DensityWeight * (scale - elliptical) / scale);
        }

        internal static EnvironmentGalaxyRegionInfluenceDto Sample(string node, GeneratedGalaxyPointDto point,
            EnvironmentGalaxySettingsDto settings, EnvironmentGalaxyTuningDto tuning, IEnumerable<EnvironmentGalaxyFeatureDto> features)
        {
            var contributions = features.Where(f => Enabled(settings, f.Kind)).Select(f =>
                new EnvironmentGalaxyContributionDto { FeatureId = f.FeatureId, Amount = Contribution(f, point) })
                .Where(c => c.Amount > 0).OrderBy(c => c.FeatureId, StringComparer.Ordinal).ToList();
            return new EnvironmentGalaxyRegionInfluenceDto { NodeId = node, BaseDensity = tuning.BaseDensity,
                TargetDensity = Math.Min(tuning.MaximumDensity, tuning.BaseDensity + contributions.Sum(c => c.Amount)), Contributions = contributions };
        }

        private static EnvironmentGalaxyDocumentDto Build(EnvironmentGalaxySettingsDto settings, EnvironmentGalaxyTuningDto tuning,
            List<EnvironmentGalaxyFeatureDto> features, int width, int height, SplitMix64Random random)
        {
            int count = settings.Map.SystemCount * 2;
            var sites = features.Where(f => f.NodeId != null).OrderBy(f => f.NodeId, StringComparer.Ordinal)
                .Select(f => new GeneratedGalaxyPointDto { X = f.Position.X, Y = f.Position.Y }).ToList();
            for (int draw = 0; draw < count * tuning.CandidateDrawsPerRegion && sites.Count < count; draw++)
            {
                var candidate = Point(random, width, height, tuning.OuterMargin, tuning.OuterMargin);
                int roll = (int)(random.NextUInt64() % (ulong)tuning.MaximumDensity);
                int density = Sample("", candidate, settings, tuning, features).TargetDensity;
                if (roll < density && sites.All(s => DistanceSquared(s, candidate) >= (long)tuning.MinimumSiteSeparation * tuning.MinimumSiteSeparation))
                    sites.Add(candidate);
            }
            if (sites.Count != count) throw new InvalidOperationException("Environment site budget exhausted.");
            var systems = features.Where(f => f.Kind == GalaxyEnvironmentFeatureKind.Star).OrderBy(f => f.SystemId, StringComparer.Ordinal)
                .Select((f, i) => new GeneratedGalaxySystemDto { SystemId = f.SystemId!, NodeId = f.NodeId!, DisplayName = "System " + Number(i) }).ToList();
            var regions = sites.Select((p, i) => {
                var id = GeneratedGalaxyData.Id("nav", i);
                return new GeneratedGalaxyRegionDto { NodeId = id, DisplayName = "Region " + Number(i),
                    SystemId = systems.SingleOrDefault(s => s.NodeId == id)?.SystemId, Center = p,
                    Boundary = GeneratedGalaxyGeometry.Cell(sites, i, width, height) };
            }).ToList();
            var connections = GeneratedGalaxyGeometry.Connect(regions, width, height);
            var neighbors = GeneratedGalaxyData.Neighbors(regions, connections);
            GeneratedGalaxySystemDto? first = null, second = null; int separation = -1;
            foreach (var a in systems)
            {
                var distances = GeneratedGalaxyData.Distances(neighbors, a.NodeId);
                if (distances.Count != count) throw new InvalidOperationException("Disconnected environment graph.");
                foreach (var b in systems.Where(b => StringComparer.Ordinal.Compare(a.SystemId, b.SystemId) < 0))
                    if (distances[b.NodeId] > separation) { separation = distances[b.NodeId]; first = a; second = b; }
            }
            if (separation < 5) throw new InvalidOperationException("Environment homes need five graph crossings.");
            var layout = new EnvironmentGalaxyLayoutDto { Width = width, Height = height, Regions = regions, Systems = systems, Connections = connections,
                HomeCandidates = new[] { first!, second! }.Select((s, i) => new GeneratedGalaxyHomeCandidateDto {
                    CivilizationDefinitionId = settings.Map.HomeCivilizationDefinitionIds[i], SystemId = s.SystemId, NodeId = s.NodeId }).ToList() };
            var document = new EnvironmentGalaxyDocumentDto { FormatVersion = EnvironmentGalaxyIdentity.FormatVersion,
                ProfileId = EnvironmentGalaxyIdentity.Profile, GeneratorVersion = EnvironmentGalaxyIdentity.GeneratorVersion,
                TuningVersion = EnvironmentGalaxyIdentity.TuningVersion, Settings = settings, Tuning = tuning, Features = features, Layout = layout,
                RegionInfluences = regions.Select(r => Sample(r.NodeId, r.Center, settings, tuning, features)).ToList() };
            document.ContentHash = EnvironmentGalaxyData.Hash(document);
            return document;
        }
    }
}
