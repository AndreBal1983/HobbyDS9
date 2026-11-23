using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using HobbyDS9.Contracts;

namespace HobbyDS9.Simulation
{
    internal static class GeneratedGalaxyGenerator
    {
        public static GeneratedGalaxyDocumentDto Generate(GeneratedGalaxySettingsDto settings, IEnumerable<CivilizationDefinitionDto> admittedCivilizations)
            => GenerateVersion(settings, admittedCivilizations, false);

        private static GeneratedGalaxyDocumentDto GenerateVersion(GeneratedGalaxySettingsDto settings,
            IEnumerable<CivilizationDefinitionDto> admittedCivilizations, bool legacy)
        {
            var input = GeneratedGalaxyData.Settings(settings, admittedCivilizations);
            // Private generation stream. Never consumes live campaign RNG.
            var random = new SplitMix64Random(ulong.Parse(input.Seed, CultureInfo.InvariantCulture)
                ^ (legacy ? 0x47414c4158594d31UL : 0x47414c4158594d32UL));
            for (int attempt = 0; attempt < 8; attempt++)
            {
                try { return Build(input, random, legacy); }
                catch (InvalidOperationException) { /* Bounded retry for placement or geometry degeneracy. */ }
            }
            throw new ArgumentException("This seed could not produce valid review geometry within eight attempts.");
        }

        private static GeneratedGalaxyDocumentDto Build(GeneratedGalaxySettingsDto settings, SplitMix64Random random, bool legacy)
        {
            const int width = 1000000, columns = 10, pitch = 100000;
            int count = settings.SystemCount * 2, height = count / columns * pitch;
            // Fixed draw count, explicitly versioned modulo sampling; no unbounded rejection loop.
            // Preserve v1 draw order and placement exactly for existing exported documents.
            var sites = legacy ? Enumerable.Range(0, count).Select(i => new GeneratedGalaxyPointDto {
                X = i % columns * pitch + 20000 + (int)(random.NextUInt64() % 60001),
                Y = i / columns * pitch + 20000 + (int)(random.NextUInt64() % 60001) }).ToList()
                : ScatteredSites(count, width, height, random);
            var regions = sites.Select((p, i) => new GeneratedGalaxyRegionDto {
                NodeId = GeneratedGalaxyData.Id("nav", i), DisplayName = "Region " + (i + 1).ToString("D3", CultureInfo.InvariantCulture),
                Center = p, Boundary = GeneratedGalaxyGeometry.Cell(sites, i, width, height) }).ToList();
            var connections = GeneratedGalaxyGeometry.Connect(regions, width, height);
            var shuffled = Enumerable.Range(0, count).ToArray();
            for (int i = count - 1; i > 0; i--) { int j = (int)(random.NextUInt64() % (ulong)(i + 1)); var n = shuffled[i]; shuffled[i] = shuffled[j]; shuffled[j] = n; }
            var systems = shuffled.Take(settings.SystemCount).OrderBy(i => i).Select((region, i) => {
                var id = GeneratedGalaxyData.Id("system", i); regions[region].SystemId = id;
                return new GeneratedGalaxySystemDto { SystemId = id, DisplayName = "System " + (i + 1).ToString("D3", CultureInfo.InvariantCulture), NodeId = regions[region].NodeId };
            }).ToList();
            var neighbors = GeneratedGalaxyData.Neighbors(regions, connections);
            GeneratedGalaxySystemDto? first = null, second = null; int separation = -1;
            foreach (var a in systems)
            {
                var distances = GeneratedGalaxyData.Distances(neighbors, a.NodeId);
                if (distances.Count != count) throw new InvalidOperationException("Disconnected generated graph.");
                foreach (var b in systems.Where(b => StringComparer.Ordinal.Compare(a.SystemId, b.SystemId) < 0))
                    if (distances[b.NodeId] > separation) { separation = distances[b.NodeId]; first = a; second = b; }
            }
            if (separation < 5) throw new InvalidOperationException("Candidate homes need at least five graph crossings.");
            var homes = new[] { first!, second! }.Select((s, i) => new GeneratedGalaxyHomeCandidateDto {
                CivilizationDefinitionId = settings.HomeCivilizationDefinitionIds[i], NodeId = s.NodeId, SystemId = s.SystemId }).ToList();
            var result = new GeneratedGalaxyDocumentDto { FormatVersion = GeneratedGalaxyIdentity.FormatVersion, ProfileId = GeneratedGalaxyIdentity.Profile,
                GeneratorVersion = legacy ? GeneratedGalaxyIdentity.LegacyGeneratorVersion : GeneratedGalaxyIdentity.GeneratorVersion,
                TuningVersion = legacy ? GeneratedGalaxyIdentity.LegacyTuningVersion : GeneratedGalaxyIdentity.TuningVersion,
                Settings = settings, Width = width, Height = height, Regions = regions, Systems = systems, Connections = connections, HomeCandidates = homes };
            result.ContentHash = GeneratedGalaxyData.Hash(result);
            return result;
        }

        private static List<GeneratedGalaxyPointDto> ScatteredSites(int count, int width, int height, SplitMix64Random random)
        {
            // Provisional v2 review tuning: a hard spacing floor protects close-up selection,
            // but no grid slots or relaxation regularize the natural dense/sparse patches.
            const int margin = 15000, minimumSeparation = 40000;
            var sites = new List<GeneratedGalaxyPointDto>();
            for (int draw = 0; draw < count * 256 && sites.Count < count; draw++)
            {
                var p = new GeneratedGalaxyPointDto {
                    X = margin + (int)(random.NextUInt64() % (ulong)(width - 2 * margin + 1)),
                    Y = margin + (int)(random.NextUInt64() % (ulong)(height - 2 * margin + 1)) };
                if (sites.All(s => (long)(s.X - p.X) * (s.X - p.X) + (long)(s.Y - p.Y) * (s.Y - p.Y)
                    >= (long)minimumSeparation * minimumSeparation)) sites.Add(p);
            }
            if (sites.Count != count) throw new InvalidOperationException("Site placement exhausted its bounded draw budget.");
            return sites;
        }

        public static GeneratedGalaxyDocumentDto ValidateAndCopy(GeneratedGalaxyDocumentDto document, IEnumerable<CivilizationDefinitionDto> admittedCivilizations)
        {
            GeneratedGalaxyData.CheckShape(document);
            var expected = GenerateVersion(document.Settings, admittedCivilizations,
                document.GeneratorVersion == GeneratedGalaxyIdentity.LegacyGeneratorVersion);
            if (document.ContentHash != expected.ContentHash || GeneratedGalaxyData.Hash(document) != expected.ContentHash)
                throw new ArgumentException("Generated map content does not match its seed, settings and supported generator version.");
            return expected;
        }
    }
}
