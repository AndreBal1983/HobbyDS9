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
        {
            var input = GeneratedGalaxyData.Settings(settings, admittedCivilizations);
            // Private generation stream. Never consumes live campaign RNG.
            var random = new SplitMix64Random(ulong.Parse(input.Seed, CultureInfo.InvariantCulture) ^ 0x47414c4158594d31UL);
            for (int attempt = 0; attempt < 8; attempt++)
            {
                try { return Build(input, random); }
                catch (InvalidOperationException) { /* Bounded retry for a quantization degeneracy only. */ }
            }
            throw new ArgumentException("This seed could not produce valid review geometry within eight attempts.");
        }

        private static GeneratedGalaxyDocumentDto Build(GeneratedGalaxySettingsDto settings, SplitMix64Random random)
        {
            const int width = 1000000, columns = 10, pitch = 100000;
            int count = settings.SystemCount * 2, height = count / columns * pitch;
            // Fixed draw count, explicitly versioned modulo sampling; no unbounded rejection loop.
            var sites = Enumerable.Range(0, count).Select(i => new GeneratedGalaxyPointDto {
                X = i % columns * pitch + 20000 + (int)(random.NextUInt64() % 60001),
                Y = i / columns * pitch + 20000 + (int)(random.NextUInt64() % 60001) }).ToList();
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
                GeneratorVersion = GeneratedGalaxyIdentity.GeneratorVersion, TuningVersion = GeneratedGalaxyIdentity.TuningVersion,
                Settings = settings, Width = width, Height = height, Regions = regions, Systems = systems, Connections = connections, HomeCandidates = homes };
            result.ContentHash = GeneratedGalaxyData.Hash(result);
            return result;
        }

        public static GeneratedGalaxyDocumentDto ValidateAndCopy(GeneratedGalaxyDocumentDto document, IEnumerable<CivilizationDefinitionDto> admittedCivilizations)
        {
            GeneratedGalaxyData.CheckShape(document);
            var expected = Generate(document.Settings, admittedCivilizations);
            if (document.ContentHash != expected.ContentHash || GeneratedGalaxyData.Hash(document) != expected.ContentHash)
                throw new ArgumentException("Generated map content does not match its seed, settings and supported generator version.");
            return expected;
        }
    }
}
