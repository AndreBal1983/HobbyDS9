using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using HobbyDS9.Contracts;

namespace HobbyDS9.Simulation
{
    internal static class NaturalGalaxyGenerator
    {
        internal static NaturalGalaxyTuningDto Tuning(NaturalGalaxySettingsDto s) => new NaturalGalaxyTuningDto {
            Environment = ScaledGalaxyGenerator.Tuning(s.Map).Environment, IsolatedStarSeparation = 120000,
            MinimumAssociationSize = 2, MaximumAssociationSize = 9,
            MinimumAssociationRadius = 22000 * s.AssociationSpreadPermille / 1000,
            MaximumAssociationRadius = 75000 * s.AssociationSpreadPermille / 1000,
            MinimumAssociationSeparation = 45000, MaximumAssociationSeparation = 160000,
            ExtraSiteSeparation = 30000, StellarPlacementPressurePermille = s.Map.QuietEmptyAreaTargetPermille / 2,
            QuietDisturbanceThreshold = 100 };

        internal static NaturalGalaxyDocumentDto Generate(NaturalGalaxySettingsDto input, IEnumerable<CivilizationDefinitionDto> catalog)
        {
            var settings = NaturalGalaxyData.Settings(input, catalog); var tuning = Tuning(settings);
            ulong seed = ulong.Parse(settings.Map.Seed, CultureInfo.InvariantCulture);
            var groups = new List<NaturalStarGroupDto>();
            var features = Features(settings, tuning, seed, groups);
            var sites = new SplitMix64Random(seed ^ 0x4e41545553495445UL);
            for (int attempt = 0; attempt < tuning.Environment.PlacementAttempts; attempt++)
            {
                try { return Build(settings, tuning, features, groups, sites); }
                catch (InvalidOperationException) { /* All features/group provenance stay fixed. */ }
            }
            throw new ArgumentException("Natural map exhausted eight bounded topology attempts.");
        }

        private static string Number(int i) => (i + 1).ToString("D4", CultureInfo.InvariantCulture);
        private static string Id(string prefix, int i) => prefix + ".natural." + Number(i);
        private static long Distance(GeneratedGalaxyPointDto a, GeneratedGalaxyPointDto b)
            => (long)(a.X - b.X) * (a.X - b.X) + (long)(a.Y - b.Y) * (a.Y - b.Y);
        private static int Draw(SplitMix64Random r, int min, int max) => min + (int)(r.NextUInt64() % (ulong)(max - min + 1));
        private static GeneratedGalaxyPointDto Point(SplitMix64Random r, int width, int height, int mx, int my)
            => new GeneratedGalaxyPointDto { X = Draw(r, mx, width - mx), Y = Draw(r, my, height - my) };

        private static List<EnvironmentGalaxyFeatureDto> Features(NaturalGalaxySettingsDto settings, NaturalGalaxyTuningDto t, ulong seed,
            List<NaturalStarGroupDto> groups)
        {
            var s = settings.Map; var e = t.Environment;
            var stars = new SplitMix64Random(seed ^ 0x4e41545553544152UL);
            var holes = new SplitMix64Random(seed ^ 0x4e415455484f4c45UL);
            var nebulae = new SplitMix64Random(seed ^ 0x4e4154554e454255UL);
            var features = new List<EnvironmentGalaxyFeatureDto>(); int budget = s.SystemCount * e.CandidateDrawsPerRegion;
            int isolated = (s.SystemCount * settings.IsolatedStarSharePermille + 999) / 1000;
            long isolatedDistance = (long)t.IsolatedStarSeparation * t.IsolatedStarSeparation;
            void Star(GeneratedGalaxyPointDto p, NaturalStarGroupDto group)
            {
                int index = features.Count, band = Draw(stars, 0, 2);
                var f = new EnvironmentGalaxyFeatureDto { FeatureId = Id("feature.star", index), Kind = GalaxyEnvironmentFeatureKind.Star,
                    DisplayName = "Star " + Number(index), Position = p, RadiusX = 70000 + band * 25000, RadiusY = 70000 + band * 25000,
                    DensityWeight = 120 + band * 80, SystemId = Id("system", index), NodeId = Id("nav", index) };
                features.Add(f); group.StarFeatureIds.Add(f.FeatureId);
            }
            for (int i = 0; i < isolated; i++)
            {
                GeneratedGalaxyPointDto? point = null;
                for (int draw = 0; draw < budget; draw++)
                {
                    var p = Point(stars, s.Width, s.Height, e.OuterMargin, e.OuterMargin);
                    if (features.All(f => Distance(f.Position, p) >= isolatedDistance)) { point = p; break; }
                }
                if (point == null) throw new ArgumentException("Isolated stars exhausted bounded placement.");
                var group = new NaturalStarGroupDto { GroupId = Id("group", groups.Count), Kind = NaturalStarGroupKind.Isolated,
                    Center = new GeneratedGalaxyPointDto { X = point.X, Y = point.Y }, StarFeatureIds = new List<string>() };
                Star(point, group); groups.Add(group);
            }
            while (features.Count < s.SystemCount)
            {
                int remaining = s.SystemCount - features.Count;
                int size = Math.Min(remaining, Draw(stars, t.MinimumAssociationSize, t.MaximumAssociationSize));
                if (remaining - size == 1) size += size < t.MaximumAssociationSize ? 1 : -1;
                int minimumRadius = Math.Min(t.MaximumAssociationRadius, t.MinimumAssociationRadius + (size - 2) * 3000 * settings.AssociationSpreadPermille / 1000);
                int rx = Draw(stars, minimumRadius, t.MaximumAssociationRadius), ry = Draw(stars, minimumRadius, t.MaximumAssociationRadius);
                int separation = Draw(stars, t.MinimumAssociationSeparation, t.MaximumAssociationSeparation);
                GeneratedGalaxyPointDto? center = null;
                for (int draw = 0; draw < budget; draw++)
                {
                    var p = Point(stars, s.Width, s.Height, rx + e.OuterMargin, ry + e.OuterMargin);
                    if (features.Take(isolated).All(f => Distance(f.Position, p) >= isolatedDistance)
                        && groups.Where(g => g.Kind == NaturalStarGroupKind.LooseAssociation).All(g => Distance(g.Center, p) >= (long)separation * separation))
                    { center = p; break; }
                }
                if (center == null) throw new ArgumentException("Loose associations exhausted bounded center placement.");
                var group = new NaturalStarGroupDto { GroupId = Id("group", groups.Count), Kind = NaturalStarGroupKind.LooseAssociation,
                    Center = center, RadiusX = rx, RadiusY = ry, StarFeatureIds = new List<string>() };
                for (int member = 0; member < size; member++)
                {
                    GeneratedGalaxyPointDto? position = null;
                    for (int draw = 0; draw < budget; draw++)
                    {
                        int dx = Draw(stars, -rx, rx), dy = Draw(stars, -ry, ry);
                        if ((long)dx * dx * 1000000 / ((long)rx * rx) + (long)dy * dy * 1000000 / ((long)ry * ry) >= 1000000) continue;
                        var p = new GeneratedGalaxyPointDto { X = center.X + dx, Y = center.Y + dy };
                        if (features.Take(isolated).All(f => Distance(f.Position, p) >= isolatedDistance)
                            && features.All(f => Distance(f.Position, p) >= (long)e.MinimumStarSeparation * e.MinimumStarSeparation)) { position = p; break; }
                    }
                    if (position == null) throw new ArgumentException("Association stars exhausted bounded placement.");
                    Star(position, group);
                }
                groups.Add(group);
            }
            for (int i = 0; i < e.BlackHoleCount; i++)
            {
                GeneratedGalaxyPointDto? position = null;
                for (int draw = 0; draw < budget; draw++)
                {
                    var p = Point(holes, s.Width, s.Height, e.OuterMargin, e.OuterMargin);
                    if (features.All(f => Distance(f.Position, p) >= (long)t.ExtraSiteSeparation * t.ExtraSiteSeparation)) { position = p; break; }
                }
                if (position == null) throw new ArgumentException("Natural black holes exhausted bounded placement.");
                features.Add(new EnvironmentGalaxyFeatureDto { FeatureId = Id("feature.black-hole", i), Kind = GalaxyEnvironmentFeatureKind.BlackHole,
                    DisplayName = "Black hole " + Number(i), Position = position, RadiusX = 160000, RadiusY = 160000, DensityWeight = 650, NodeId = Id("nav", s.SystemCount + i) });
            }
            for (int i = 0; i < e.NebulaCount; i++)
            {
                int rx = Math.Min(Draw(nebulae, 180000, 240000), s.Width / 2 - e.OuterMargin), ry = Math.Min(Draw(nebulae, 100000, 160000), s.Height / 2 - e.OuterMargin);
                features.Add(new EnvironmentGalaxyFeatureDto { FeatureId = Id("feature.nebula", i), Kind = GalaxyEnvironmentFeatureKind.Nebula,
                    DisplayName = "Nebula " + Number(i), Position = Point(nebulae, s.Width, s.Height, rx + e.OuterMargin, ry + e.OuterMargin), RadiusX = rx, RadiusY = ry, DensityWeight = 420 });
            }
            return features.OrderBy(f => f.FeatureId, StringComparer.Ordinal).ToList();
        }

        private static EnvironmentGalaxyRegionInfluenceDto Sample(string node, GeneratedGalaxyPointDto point, NaturalGalaxySettingsDto s,
            NaturalGalaxyTuningDto t, List<EnvironmentGalaxyFeatureDto> features, HashSet<string> stars)
        {
            var row = EnvironmentGalaxyGenerator.Sample(node, point, ScaledGalaxyData.EnvironmentSettings(s.Map), t.Environment, features);
            foreach (var c in row.Contributions) if (stars.Contains(c.FeatureId)) c.Amount = c.Amount * t.StellarPlacementPressurePermille / 1000;
            row.Contributions.RemoveAll(c => c.Amount == 0); row.TargetDensity = Math.Min(t.Environment.MaximumDensity, row.BaseDensity + row.Contributions.Sum(c => c.Amount));
            return row;
        }

        private static NaturalGalaxyDocumentDto Build(NaturalGalaxySettingsDto settings, NaturalGalaxyTuningDto t,
            List<EnvironmentGalaxyFeatureDto> features, List<NaturalStarGroupDto> groups, SplitMix64Random random)
        {
            var s = settings.Map; var e = t.Environment; int count = s.SystemCount + s.EmptyRegionCount;
            var starIds = new HashSet<string>(features.Where(f => f.Kind == GalaxyEnvironmentFeatureKind.Star).Select(f => f.FeatureId), StringComparer.Ordinal);
            var sites = features.Where(f => f.NodeId != null).OrderBy(f => f.NodeId, StringComparer.Ordinal)
                .Select(f => new GeneratedGalaxyPointDto { X = f.Position.X, Y = f.Position.Y }).ToList();
            for (int draw = 0; draw < count * e.CandidateDrawsPerRegion && sites.Count < count; draw++)
            {
                var candidate = Point(random, s.Width, s.Height, e.OuterMargin, e.OuterMargin); int roll = Draw(random, 0, e.MaximumDensity - 1);
                int density = Sample("", candidate, settings, t, features, starIds).TargetDensity;
                if (roll < density && sites.All(p => Distance(p, candidate) >= (long)t.ExtraSiteSeparation * t.ExtraSiteSeparation)) sites.Add(candidate);
            }
            if (sites.Count != count) throw new InvalidOperationException("Natural extra-site budget exhausted.");
            var systems = features.Where(f => f.Kind == GalaxyEnvironmentFeatureKind.Star).OrderBy(f => f.SystemId, StringComparer.Ordinal)
                .Select((f, i) => new GeneratedGalaxySystemDto { SystemId = f.SystemId!, NodeId = f.NodeId!, DisplayName = "System " + Number(i) }).ToList();
            var systemNodes = systems.ToDictionary(v => v.NodeId, v => v.SystemId, StringComparer.Ordinal);
            var regions = sites.Select((p, i) => new GeneratedGalaxyRegionDto { NodeId = Id("nav", i), DisplayName = "Region " + Number(i),
                SystemId = systemNodes.TryGetValue(Id("nav", i), out var system) ? system : null, Center = p,
                Boundary = GeneratedGalaxyGeometry.Cell(sites, i, s.Width, s.Height) }).ToList();
            if (regions.Any(r => r.Boundary.Count > ScaledGalaxyIdentity.MaximumVerticesPerRegion)
                || regions.Sum(r => r.Boundary.Count) > ScaledGalaxyIdentity.MaximumTotalVertices) throw new InvalidOperationException("Natural vertex budget exceeded.");
            var connections = GeneratedGalaxyGeometry.Connect(regions, s.Width, s.Height); var neighbors = GeneratedGalaxyData.Neighbors(regions, connections);
            GeneratedGalaxySystemDto? first = null, second = null; int separation = -1;
            foreach (var a in systems)
            {
                var distances = GeneratedGalaxyData.Distances(neighbors, a.NodeId);
                if (distances.Count != count) throw new InvalidOperationException("Disconnected natural graph.");
                foreach (var b in systems.Where(b => StringComparer.Ordinal.Compare(a.SystemId, b.SystemId) < 0))
                    if (distances[b.NodeId] > separation) { separation = distances[b.NodeId]; first = a; second = b; }
            }
            if (separation < 5) throw new InvalidOperationException("Natural homes need five crossings.");
            var d = new NaturalGalaxyDocumentDto { FormatVersion = NaturalGalaxyIdentity.FormatVersion, ProfileId = NaturalGalaxyIdentity.Profile,
                GeneratorVersion = NaturalGalaxyIdentity.GeneratorVersion, TuningVersion = NaturalGalaxyIdentity.TuningVersion,
                ContentHash = new string('0', 64), Settings = settings, Tuning = t, Features = features, StarGroups = groups,
                Layout = new EnvironmentGalaxyLayoutDto { Width = s.Width, Height = s.Height, Regions = regions, Systems = systems, Connections = connections,
                    HomeCandidates = new[] { first!, second! }.Select((v, i) => new GeneratedGalaxyHomeCandidateDto {
                        CivilizationDefinitionId = s.HomeCivilizationDefinitionIds[i], SystemId = v.SystemId, NodeId = v.NodeId }).ToList() },
                RegionInfluences = regions.Select(r => Sample(r.NodeId, r.Center, settings, t, features, starIds)).ToList() };
            NaturalGalaxyData.Shape(d); d.ContentHash = NaturalGalaxyData.Hash(d); return d;
        }
    }
}
