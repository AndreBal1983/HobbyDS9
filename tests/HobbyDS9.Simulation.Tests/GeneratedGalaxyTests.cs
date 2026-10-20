using System.Globalization;
using System.Text.Json;
using HobbyDS9.Application;
using HobbyDS9.Contracts;
using Xunit;

namespace HobbyDS9.Simulation.Tests;

public sealed class GeneratedGalaxyTests
{
    static List<CivilizationDefinitionDto> Catalog() => PlayableFleetPackageReader.Read(Path.Combine(AppContext.BaseDirectory, "scenarios/playable-fleet-v1"))
        .Integrated!.Physical!.CorePackage!.Campaign!.Definitions.Civilizations!;
    static GeneratedGalaxySettingsDto Settings(string seed = "42", int size = 40) => new() {
        Seed = seed, SystemCount = size, HomeCivilizationDefinitionIds = new() { "civilization.federation", "civilization.cardassian" } };
    static GeneratedGalaxyReview Generate(string seed = "42", int size = 40) => GeneratedGalaxyApplication.Generate(Settings(seed, size), Catalog());
    static string Json<T>(T value) => JsonSerializer.Serialize(value);
    static GeneratedGalaxyDocumentDto Roundtrip(GeneratedGalaxyDocumentDto d) => JsonSerializer.Deserialize<GeneratedGalaxyDocumentDto>(Json(d))!;

    [Theory]
    [InlineData(30)][InlineData(40)][InlineData(50)]
    public void BoundedManySeedMapsHaveRealEmptyRegionsConnectedGraphAndValidTiling(int size)
    {
        var hashes = new HashSet<string>();
        // 32 reproducible seeds per size, with both unsigned64 endpoints separately below.
        for (ulong seed = 0; seed < 32; seed++)
        {
            var review = Generate(seed.ToString(CultureInfo.InvariantCulture), size); var d = review.Export();
            Assert.True(hashes.Add(d.ContentHash));
            Assert.Equal(size, d.Systems.Count); Assert.Equal(size * 2, d.Regions.Count);
            Assert.Equal(size, d.Regions.Count(r => r.SystemId == null));
            Assert.Equal(GeneratedGalaxyIdentity.GeneratorVersion, d.GeneratorVersion);
            Assert.Equal(GeneratedGalaxyIdentity.TuningVersion, d.TuningVersion);
            // v2 must not retain the old one-site-per-100k-cell stratification.
            Assert.True(d.Regions.Select(r => (r.Center.X / 100000, r.Center.Y / 100000)).Distinct().Count() < d.Regions.Count * 9 / 10);
            for (int i = 0; i < d.Regions.Count; i++)
            {
                var a = d.Regions[i].Center;
                Assert.InRange(a.X, 15000, d.Width - 15000); Assert.InRange(a.Y, 15000, d.Height - 15000);
                for (int j = i + 1; j < d.Regions.Count; j++)
                {
                    var b = d.Regions[j].Center;
                    Assert.True((long)(a.X - b.X) * (a.X - b.X) + (long)(a.Y - b.Y) * (a.Y - b.Y) >= 40000L * 40000);
                }
            }
            Assert.Equal(d.Regions.Count, d.Regions.Select(r => r.NodeId).Distinct().Count());
            Assert.Equal(size, d.Systems.Select(s => s.SystemId).Distinct().Count());
            foreach (var s in d.Systems) Assert.Equal(s.SystemId, d.Regions.Single(r => r.NodeId == s.NodeId).SystemId);
            Assert.Equal(d.Connections.Count, d.Connections.Select(e => (e.EndpointA, e.EndpointB)).Distinct().Count());
            foreach (var e in d.Connections)
            {
                Assert.NotEqual(e.EndpointA, e.EndpointB); Assert.Equal(1, e.BaseTraversalCost); Assert.Equal(GalaxyEdgeKind.Normal, e.Kind);
                Assert.Contains(d.Regions, r => r.NodeId == e.EndpointA); Assert.Contains(d.Regions, r => r.NodeId == e.EndpointB);
            }
            foreach (var r in d.Regions)
            {
                var route = review.FindRoute(d.Regions[0].NodeId, r.NodeId);
                Assert.True(route.Success); Assert.Equal(route.Steps.Count, route.TotalCost);
                Assert.Equal(route.Costs.Sum(c => c.EffectiveCost), route.TotalCost);
                if (route.Steps.Count > 0) Assert.Equal(r.NodeId, route.Steps.Last());
            }
            var homeRoute = review.FindRoute(d.HomeCandidates[0].NodeId, d.HomeCandidates[1].NodeId);
            Assert.True(homeRoute.TotalCost >= 5);
            Assert.All(d.HomeCandidates, h => Assert.Contains(d.Systems, s => s.SystemId == h.SystemId && s.NodeId == h.NodeId));
            ValidateGeometryIndependently(d);
        }
    }

    static void ValidateGeometryIndependently(GeneratedGalaxyDocumentDto d)
    {
        long Cross(GeneratedGalaxyPointDto a, GeneratedGalaxyPointDto b, GeneratedGalaxyPointDto c)
            => (long)(b.X - a.X) * (c.Y - a.Y) - (long)(b.Y - a.Y) * (c.X - a.X);
        long area = 0;
        var edgeOwners = new Dictionary<(int, int, int, int), List<string>>();
        foreach (var r in d.Regions)
        {
            long cellArea = 0;
            for (int i = 0; i < r.Boundary.Count; i++)
            {
                var a = r.Boundary[i]; var b = r.Boundary[(i + 1) % r.Boundary.Count];
                Assert.InRange(a.X, 0, d.Width); Assert.InRange(a.Y, 0, d.Height);
                Assert.True(Cross(a, b, r.Center) > 0);
                Assert.All(r.Boundary, p => Assert.True(Cross(a, b, p) >= 0));
                cellArea += (long)a.X * b.Y - (long)a.Y * b.X;
                var key = a.X < b.X || a.X == b.X && a.Y < b.Y ? (a.X, a.Y, b.X, b.Y) : (b.X, b.Y, a.X, a.Y);
                if (!edgeOwners.TryGetValue(key, out var owners)) edgeOwners.Add(key, owners = new());
                owners.Add(r.NodeId);
            }
            Assert.True(cellArea > 0); area += cellArea;
        }
        Assert.Equal(2L * d.Width * d.Height, area);
        var shared = new HashSet<(string, string)>();
        foreach (var edge in edgeOwners)
        {
            var (x1, y1, x2, y2) = edge.Key;
            if (edge.Value.Count == 1)
                Assert.True(x1 == x2 && (x1 == 0 || x1 == d.Width) || y1 == y2 && (y1 == 0 || y1 == d.Height));
            else { Assert.Equal(2, edge.Value.Count); shared.Add((edge.Value[0], edge.Value[1])); }
        }
        Assert.True(shared.SetEquals(d.Connections.Select(e => (e.EndpointA, e.EndpointB))));
        // Separating-axis check: no two convex cell interiors overlap, including non-neighbors.
        bool Separates(GeneratedGalaxyRegionDto a, GeneratedGalaxyRegionDto b)
            => Enumerable.Range(0, a.Boundary.Count).Any(i =>
                b.Boundary.All(p => Cross(a.Boundary[i], a.Boundary[(i + 1) % a.Boundary.Count], p) <= 0));
        for (int i = 0; i < d.Regions.Count; i++)
            for (int j = i + 1; j < d.Regions.Count; j++)
                Assert.True(Separates(d.Regions[i], d.Regions[j]) || Separates(d.Regions[j], d.Regions[i]));
    }

    [Theory]
    [InlineData("0", 30)][InlineData("18446744073709551615", 40)][InlineData("42", 50)]
    public void RepeatReorderSerializationAndDetachedExportsPreserveExactMap(string seed, int size)
    {
        var settings = Settings(seed, size); var catalog = Catalog();
        var review = GeneratedGalaxyApplication.Generate(settings, catalog); var d = review.Export(); var expected = Json(d);
        settings.HomeCivilizationDefinitionIds.Reverse(); catalog.Reverse();
        Assert.Equal(expected, Json(GeneratedGalaxyApplication.Generate(settings, catalog).Export()));
        var reordered = Roundtrip(d); reordered.Regions.Reverse(); reordered.Systems.Reverse(); reordered.Connections.Reverse(); reordered.HomeCandidates.Reverse();
        reordered.Settings.HomeCivilizationDefinitionIds.Reverse();
        var loaded = GeneratedGalaxyApplication.Open(reordered, catalog);
        Assert.Equal(expected, Json(loaded.Export()));
        Assert.Equal(Json(review.FindRoute(d.HomeCandidates[0].NodeId, d.HomeCandidates[1].NodeId)),
            Json(loaded.FindRoute(d.HomeCandidates[0].NodeId, d.HomeCandidates[1].NodeId)));
        d.Regions[0].Boundary[0].X++; d.Systems.Clear(); d.Connections.Clear(); d.Settings.Seed = "11"; settings.Seed = "12";
        Assert.Equal(expected, Json(review.Export())); Assert.Equal(expected, Json(loaded.Export()));
    }

    [Theory]
    [InlineData("")][InlineData("-1")][InlineData("01")][InlineData("+1")][InlineData(" 1")][InlineData("18446744073709551616")][InlineData("1.0")]
    public void InvalidSeedFailsClearly(string seed) => Assert.Throws<ArgumentException>(() => Generate(seed));
    [Theory]
    [InlineData(0)][InlineData(-1)][InlineData(31)][InlineData(100)][InlineData(int.MaxValue)]
    public void UnsupportedSizesFailBeforeGeneration(int size) => Assert.Throws<ArgumentException>(() => Generate("42", size));

    [Fact]
    public void MissingForeignDuplicateAndOversizedCatalogInputsAreRejected()
    {
        Assert.Throws<ArgumentException>(() => GeneratedGalaxyApplication.Generate(null!, Catalog()));
        Assert.Throws<ArgumentException>(() => GeneratedGalaxyApplication.Generate(Settings(), null!));
        var s = Settings(); s.HomeCivilizationDefinitionIds[0] = "civilization.missing";
        Assert.Throws<ArgumentException>(() => GeneratedGalaxyApplication.Generate(s, Catalog()));
        s = Settings(); s.HomeCivilizationDefinitionIds[0] = s.HomeCivilizationDefinitionIds[1];
        Assert.Throws<ArgumentException>(() => GeneratedGalaxyApplication.Generate(s, Catalog()));
        var c = Catalog(); c.Add(c[0]); Assert.Throws<ArgumentException>(() => GeneratedGalaxyApplication.Generate(Settings(), c));
        Assert.Throws<ArgumentException>(() => GeneratedGalaxyApplication.Generate(Settings(), Enumerable.Repeat(Catalog()[0], 257)));
    }

    [Theory]
    [InlineData("version")][InlineData("generator")][InlineData("tuning")][InlineData("hash")][InlineData("seed")]
    [InlineData("point")][InlineData("edge")][InlineData("home")][InlineData("system")][InlineData("duplicate")]
    [InlineData("missing")][InlineData("oversized")][InlineData("null-row")][InlineData("null-settings")]
    public void CorruptMapCannotReplaceOrMutateExistingReview(string field)
    {
        var current = Generate(); var expected = Json(current.Export()); var d = current.Export();
        switch (field)
        {
            case "version": d.FormatVersion++; break;
            case "generator": d.GeneratorVersion += "-next"; break;
            case "tuning": d.TuningVersion += "-next"; break;
            case "hash": d.ContentHash = new string('0', 64); break;
            case "seed": d.Settings.Seed = "43"; break;
            case "point": d.Regions[0].Boundary[0].X++; break;
            case "edge": d.Connections[0].EndpointB = d.Connections[0].EndpointA; break;
            case "home": d.HomeCandidates[0].SystemId = d.HomeCandidates[1].SystemId; break;
            case "system": d.Systems[0].NodeId = d.Systems[1].NodeId; break;
            case "duplicate": d.Regions[1] = d.Regions[0]; break;
            case "missing": d.Connections = null!; break;
            case "oversized": d.Regions.AddRange(Enumerable.Repeat(d.Regions[0], 101)); break;
            case "null-row": d.Regions[0] = null!; break;
            default: d.Settings = null!; break;
        }
        Assert.Throws<ArgumentException>(() => current = GeneratedGalaxyApplication.Open(d, Catalog()));
        Assert.Equal(expected, Json(current.Export()));
    }

    [Fact]
    public void RouteErrorsAndSameNodeAreExplicitAndDoNotMutateMap()
    {
        var review = Generate(); var d = review.Export(); var before = Json(d);
        var same = review.FindRoute(d.Regions[0].NodeId, d.Regions[0].NodeId);
        Assert.True(same.Success); Assert.Empty(same.Steps); Assert.Equal(0, same.TotalCost);
        Assert.Equal(RouteErrorCode.UnknownSystem, review.FindRoute("nav.missing", d.Regions[0].NodeId).ErrorCode);
        Assert.Equal(RouteErrorCode.InvalidSystemId, review.FindRoute("", d.Regions[0].NodeId).ErrorCode);
        Assert.Equal(before, Json(review.Export()));
    }

    [Fact]
    public void HostJsonCodecRejectsMissingDuplicateUnknownMalformedAndOversizedData()
    {
        var current = Generate(); var text = GeneratedGalaxyJson.Write(current); var before = Json(current.Export());
        Assert.Equal(before, Json(GeneratedGalaxyJson.Read(text, Catalog()).Export()));
        foreach (var corrupt in new[] { "null", "{", "[]", new string(' ', GeneratedGalaxyJson.MaximumBytes + 1),
            text.Replace("\"Width\": 1000000,", ""), text.Replace("\"FormatVersion\": 1,", "\"FormatVersion\": 1, \"FormatVersion\": 1,"),
            text.Replace("\"FormatVersion\": 1,", "\"FormatVersion\": 1, \"Unexpected\": true,"),
            text.Replace("\"Seed\": \"42\"", "\"Seed\": 42"), text.Replace("\"Regions\": [", "\"Regions\": null, \"Bad\": [") })
            Assert.Throws<ArgumentException>(() => current = GeneratedGalaxyJson.Read(corrupt, Catalog()));
        Assert.Equal(before, Json(current.Export()));
    }

    [Theory]
    [InlineData("42", 40, "6e306b02c09344db583fd0871f806e42549e81a06697f53dd124fa1305660a8f")]
    [InlineData("12345", 30, "85dac90f91180327f0fad3b915ca3145fdd8a1d1108d7b4f543c8c300d655884")]
    [InlineData("20261020", 50, "c6d6dfcbee54c17607d5090f47ad403f5ba158a7d96efeadcc764da7a2d8b6cd")]
    public void InitialVersionHasExplicitCrossPlatformGoldenVectors(string seed, int size, string hash)
    {
        var json = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "generated-galaxy-v1", $"seed-{seed}-systems-{size}.galaxy.json"));
        var old = JsonSerializer.Deserialize<GeneratedGalaxyDocumentDto>(json)!;
        var reopened = GeneratedGalaxyJson.Read(json, Catalog()).Export();
        Assert.Equal(GeneratedGalaxyIdentity.LegacyGeneratorVersion, reopened.GeneratorVersion);
        Assert.Equal(hash, reopened.ContentHash);
        Assert.Equal(Json(old), Json(reopened));
        Assert.NotEqual(hash, Generate(seed, size).Export().ContentHash);
    }

    [Fact]
    public void GenerationDoesNotConsumeOrRewriteTheExistingPlayableCampaign()
    {
        var package = PlayableFleetPackageReader.Read(Path.Combine(AppContext.BaseDirectory, "scenarios/playable-fleet-v1"));
        var campaign = CampaignPlayableFleetApplication.StartPackage(package);
        var before = Json(campaign.ExportPlayableSave());
        var map = Generate(); GeneratedGalaxyJson.Read(GeneratedGalaxyJson.Write(map), Catalog());
        Assert.Equal(before, Json(campaign.ExportPlayableSave()));
        Assert.Equal(before, Json(CampaignPlayableFleetApplication.LoadSave(package, campaign.ExportPlayableSave()).ExportPlayableSave()));
        Assert.Equal(6, campaign.ExportPlayableSave().FormatVersion);
        Assert.Equal("87e57e759ad9ddbe0eebcecef86fb04a9c55c025ea28031205ac323c2c9c41c7", package.PackageHash);
    }

    [Theory]
    [InlineData("42", 40, "b7f6c3041a330eac4dee6cc2e2e1ad3cfb0e6b77d3c81351835d6a1e6cf0938e")]
    [InlineData("43", 40, "6f2ae2943bb9dd9e69425dbe5956cbca93958e8e3837d8958414c12aa17b3a9c")]
    [InlineData("12345", 30, "e86eed4273cfd59419844a6bfb83ccbc86d59b5d825a5306b74bb924c072e781")]
    [InlineData("20261020", 50, "25a1cc65053574d71648097c6a5d97e4832e5d9112ebb33e51aae71a20b84a25")]
    public void ScatteredVersionHasSeparateDeterministicGoldenVectors(string seed, int size, string hash)
        => Assert.Equal(hash, Generate(seed, size).Export().ContentHash);

    [Theory]
    [InlineData("42", 40)][InlineData("12345", 30)][InlineData("20261020", 50)]
    public void SameSeedCorrectionHasGreaterAreaVariationWithoutRemappingLegacyExport(string seed, int size)
    {
        var text = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "generated-galaxy-v1", $"seed-{seed}-systems-{size}.galaxy.json"));
        var original = GeneratedGalaxyJson.Read(text, Catalog());
        var old = original.Export(); var revised = Generate(seed, size).Export();
        decimal AreaSpread(GeneratedGalaxyDocumentDto map)
        {
            var areas = map.Regions.Select(r => Math.Abs(Enumerable.Range(0, r.Boundary.Count).Sum(i => {
                var a = r.Boundary[i]; var b = r.Boundary[(i + 1) % r.Boundary.Count];
                return (long)a.X * b.Y - (long)a.Y * b.X;
            }))).ToList();
            decimal mean = areas.Average(a => (decimal)a);
            return areas.Average(a => ((decimal)a - mean) * ((decimal)a - mean)) / (mean * mean);
        }
        Assert.True(AreaSpread(revised) > AreaSpread(old) * 2, "Requested sample correction should materially increase cell-area variation.");
        Assert.NotEqual(Json(old.Regions), Json(revised.Regions));
        Assert.Equal(old.Width, revised.Width); Assert.Equal(old.Height, revised.Height);
        Assert.Equal(Json(old), Json(original.Export()));
        Assert.Equal(Json(old), Json(GeneratedGalaxyApplication.Open(Roundtrip(old), Catalog()).Export()));
        Assert.Equal(Json(revised), Json(GeneratedGalaxyApplication.Open(Roundtrip(revised), Catalog()).Export()));
    }

    [Theory]
    [InlineData(true)][InlineData(false)]
    public void AlgorithmAndTuningVersionsMustBeAnExactSupportedPair(bool useLegacyAlgorithm)
    {
        var current = Generate(); var before = Json(current.Export()); var mixed = current.Export();
        if (useLegacyAlgorithm) mixed.GeneratorVersion = GeneratedGalaxyIdentity.LegacyGeneratorVersion;
        else mixed.TuningVersion = GeneratedGalaxyIdentity.LegacyTuningVersion;
        Assert.False(GeneratedGalaxyIdentity.Supports(mixed.GeneratorVersion, mixed.TuningVersion));
        Assert.Contains("Unsupported", Assert.Throws<ArgumentException>(() => current = GeneratedGalaxyApplication.Open(mixed, Catalog())).Message);
        Assert.Equal(before, Json(current.Export()));
    }
}
