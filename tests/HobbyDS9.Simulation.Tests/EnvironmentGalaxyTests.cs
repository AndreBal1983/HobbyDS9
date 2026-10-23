using System.Globalization;
using System.Text.Json;
using HobbyDS9.Application;
using HobbyDS9.Contracts;
using HobbyDS9.Simulation;
using Xunit;

namespace HobbyDS9.Simulation.Tests;

public sealed class EnvironmentGalaxyTests
{
    static List<CivilizationDefinitionDto> Catalog() => PlayableFleetPackageReader.Read(Path.Combine(AppContext.BaseDirectory, "scenarios/playable-fleet-v1"))
        .Integrated!.Physical!.CorePackage!.Campaign!.Definitions.Civilizations!;
    static EnvironmentGalaxySettingsDto Settings(string seed = "42", int size = 40) => new() {
        Map = new() { Seed = seed, SystemCount = size, HomeCivilizationDefinitionIds = new() { "civilization.federation", "civilization.cardassian" } },
        StellarInfluenceEnabled = true, BlackHoleInfluenceEnabled = true, NebulaInfluenceEnabled = true };
    static EnvironmentGalaxyReview Generate(string seed = "42", int size = 40) => EnvironmentGalaxyApplication.Generate(Settings(seed, size), Catalog());
    static string Json<T>(T value) => JsonSerializer.Serialize(value);
    static EnvironmentGalaxyDocumentDto Clone(EnvironmentGalaxyDocumentDto d) => JsonSerializer.Deserialize<EnvironmentGalaxyDocumentDto>(Json(d))!;

    [Theory]
    [InlineData(30)][InlineData(40)][InlineData(50)]
    public void ManySeedFeatureFirstMapsPreserveAnchorsAndValidConnectedGeometry(int size)
    {
        var hashes = new HashSet<string>();
        for (int seed = 0; seed < 16; seed++)
        {
            var review = Generate(seed.ToString(CultureInfo.InvariantCulture), size); var d = review.Export(); var l = d.Layout;
            Assert.True(hashes.Add(d.ContentHash)); Assert.Equal(2, d.FormatVersion);
            Assert.Equal(size, l.Systems.Count); Assert.Equal(size * 2, l.Regions.Count);
            Assert.Equal(size, l.Regions.Count(r => r.SystemId == null));
            Assert.Equal(size + 5, d.Features.Count); Assert.Equal(l.Regions.Count, d.RegionInfluences.Count);
            Assert.Equal(d.Features.Count, d.Features.Select(f => f.FeatureId).Distinct().Count());
            for (int i = 0; i < l.Regions.Count; i++)
                for (int j = i + 1; j < l.Regions.Count; j++)
                {
                    var a = l.Regions[i].Center; var b = l.Regions[j].Center;
                    Assert.True((long)(a.X - b.X) * (a.X - b.X) + (long)(a.Y - b.Y) * (a.Y - b.Y) >= 30000L * 30000);
                }
            foreach (var f in d.Features)
            {
                Assert.InRange(f.Position.X, d.Tuning.OuterMargin, l.Width - d.Tuning.OuterMargin);
                Assert.InRange(f.Position.Y, d.Tuning.OuterMargin, l.Height - d.Tuning.OuterMargin);
                if (f.Kind == GalaxyEnvironmentFeatureKind.Nebula) { Assert.Null(f.NodeId); Assert.Null(f.SystemId); continue; }
                var region = l.Regions.Single(r => r.NodeId == f.NodeId);
                Assert.Equal(Json(f.Position), Json(region.Center));
                if (f.Kind == GalaxyEnvironmentFeatureKind.Star)
                {
                    Assert.Equal(f.SystemId, region.SystemId);
                    Assert.Contains(l.Systems, s => s.SystemId == f.SystemId && s.NodeId == f.NodeId);
                }
                else { Assert.Null(f.SystemId); Assert.Null(region.SystemId); }
            }
            foreach (var r in l.Regions)
            {
                var route = review.FindRoute(l.Regions[0].NodeId, r.NodeId);
                Assert.True(route.Success); Assert.Equal(route.Steps.Count, route.TotalCost);
                var influence = d.RegionInfluences.Single(x => x.NodeId == r.NodeId);
                Assert.Equal(100, influence.BaseDensity);
                Assert.Equal(Math.Min(1000, influence.BaseDensity + influence.Contributions.Sum(c => c.Amount)), influence.TargetDensity);
                Assert.Equal(influence.Contributions.Count, influence.Contributions.Select(c => c.FeatureId).Distinct().Count());
                Assert.All(influence.Contributions, c => Assert.Contains(d.Features, f => f.FeatureId == c.FeatureId));
                foreach (var f in d.Features)
                    Assert.Equal(ExpectedContribution(f, r.Center), influence.Contributions.SingleOrDefault(c => c.FeatureId == f.FeatureId)?.Amount ?? 0);
            }
            Assert.All(l.Connections, c => { Assert.Equal(1, c.BaseTraversalCost); Assert.Equal(GalaxyEdgeKind.Normal, c.Kind); });
            Assert.True(review.FindRoute(l.HomeCandidates[0].NodeId, l.HomeCandidates[1].NodeId).TotalCost >= 5);
            GeneratedGalaxyTests.ValidateGeometryIndependently(new GeneratedGalaxyDocumentDto {
                Width = l.Width, Height = l.Height, Regions = l.Regions, Systems = l.Systems, Connections = l.Connections, HomeCandidates = l.HomeCandidates });
        }
    }

    static int ExpectedContribution(EnvironmentGalaxyFeatureDto f, GeneratedGalaxyPointDto p)
    {
        var dx = new System.Numerics.BigInteger(p.X - f.Position.X);
        var dy = new System.Numerics.BigInteger(p.Y - f.Position.Y);
        if (System.Numerics.BigInteger.Abs(dx) >= f.RadiusX || System.Numerics.BigInteger.Abs(dy) >= f.RadiusY) return 0;
        var q = dx * dx * 1000000 / ((long)f.RadiusX * f.RadiusX) + dy * dy * 1000000 / ((long)f.RadiusY * f.RadiusY);
        return q >= 1000000 ? 0 : (int)(f.DensityWeight * (1000000 - q) / 1000000);
    }

    [Theory]
    [InlineData(GalaxyEnvironmentFeatureKind.Star)]
    [InlineData(GalaxyEnvironmentFeatureKind.BlackHole)]
    [InlineData(GalaxyEnvironmentFeatureKind.Nebula)]
    public void ControlledFamilyOffKeepsFeaturesFixedAndReducesLocalExtraSiteDensity(GalaxyEnvironmentFeatureKind kind)
    {
        int enabledLocal = 0, disabledLocal = 0;
        for (int seed = 0; seed < 16; seed++)
        {
            var onSettings = Settings(seed.ToString(CultureInfo.InvariantCulture));
            var offSettings = Settings(seed.ToString(CultureInfo.InvariantCulture));
            if (kind == GalaxyEnvironmentFeatureKind.Star) offSettings.StellarInfluenceEnabled = false;
            else if (kind == GalaxyEnvironmentFeatureKind.BlackHole) offSettings.BlackHoleInfluenceEnabled = false;
            else offSettings.NebulaInfluenceEnabled = false;
            var on = EnvironmentGalaxyApplication.Generate(onSettings, Catalog()).Export();
            var off = EnvironmentGalaxyApplication.Generate(offSettings, Catalog()).Export();
            Assert.Equal(Json(on.Features), Json(off.Features)); Assert.Equal(Json(on.Tuning), Json(off.Tuning));
            Assert.Equal(on.Layout.Regions.Count, off.Layout.Regions.Count); Assert.NotEqual(on.ContentHash, off.ContentHash);
            var family = on.Features.Where(f => f.Kind == kind).ToList();
            var anchors = on.Features.Where(f => f.NodeId != null).Select(f => f.NodeId).ToHashSet();
            // Fixed physical measurement area: union of this family's >=half-weight cores,
            // excluding mandatory anchor sites. Same areas/feature inputs in both maps.
            int Count(EnvironmentGalaxyDocumentDto d) => d.Layout.Regions.Count(r => !anchors.Contains(r.NodeId)
                && family.Any(f => ExpectedContribution(f, r.Center) * 2 >= f.DensityWeight));
            enabledLocal += Count(on); disabledLocal += Count(off);
            foreach (var feature in family)
            {
                var enabled = EnvironmentGalaxyGenerator.Sample("", feature.Position, onSettings, on.Tuning, on.Features);
                var disabled = EnvironmentGalaxyGenerator.Sample("", feature.Position, offSettings, off.Tuning, off.Features);
                Assert.True(enabled.TargetDensity >= disabled.TargetDensity);
                Assert.DoesNotContain(disabled.Contributions, c => family.Any(f => f.FeatureId == c.FeatureId));
                Assert.Equal(Json(enabled.Contributions.Where(c => !family.Any(f => f.FeatureId == c.FeatureId))),
                    Json(disabled.Contributions));
            }
        }
        Assert.True(enabledLocal > disabledLocal, $"{kind}: enabled={enabledLocal}, disabled={disabledLocal}. Fixed region budget redistributes sites.");
    }

    [Theory]
    [InlineData("0", 30)][InlineData("18446744073709551615", 50)][InlineData("42", 40)]
    public void RepeatReorderAndFullFeatureLayoutRoundtripPreserveAuthority(string seed, int size)
    {
        var settings = Settings(seed, size); var catalog = Catalog(); var review = EnvironmentGalaxyApplication.Generate(settings, catalog);
        var d = review.Export(); var expected = Json(d);
        settings.Map.HomeCivilizationDefinitionIds.Reverse(); catalog.Reverse();
        Assert.Equal(expected, Json(EnvironmentGalaxyApplication.Generate(settings, catalog).Export()));
        var reordered = Clone(d); reordered.Features.Reverse(); reordered.Layout.Regions.Reverse(); reordered.Layout.Systems.Reverse();
        reordered.Layout.Connections.Reverse(); reordered.Layout.HomeCandidates.Reverse(); reordered.RegionInfluences.Reverse();
        reordered.RegionInfluences.ForEach(r => r.Contributions.Reverse()); reordered.Settings.Map.HomeCivilizationDefinitionIds.Reverse();
        var loaded = EnvironmentGalaxyApplication.Open(reordered, catalog); Assert.Equal(expected, Json(loaded.Export()));
        Assert.Equal(expected, Json(EnvironmentGalaxyJson.Read(EnvironmentGalaxyJson.Write(review), catalog).Export()));
        Assert.Equal(Json(review.FindRoute(d.Layout.HomeCandidates[0].NodeId, d.Layout.HomeCandidates[1].NodeId)),
            Json(loaded.FindRoute(d.Layout.HomeCandidates[0].NodeId, d.Layout.HomeCandidates[1].NodeId)));
        d.Features[0].Position.X++; d.Layout.Regions[0].Boundary.Clear(); d.RegionInfluences[0].Contributions.Clear(); d.Tuning.BaseDensity++;
        settings.Map.Seed = "13";
        Assert.Equal(expected, Json(review.Export())); Assert.Equal(expected, Json(loaded.Export()));
    }

    [Theory]
    [InlineData("format")][InlineData("generator")][InlineData("hash")][InlineData("feature-position")][InlineData("feature-weight")]
    [InlineData("feature-reference")][InlineData("footprint")][InlineData("contribution")][InlineData("tuning")]
    [InlineData("settings")][InlineData("layout")][InlineData("missing")][InlineData("oversized")][InlineData("null")][InlineData("rehashed")]
    public void CorruptEnvironmentCannotReplaceOrMutateCurrentReview(string field)
    {
        var current = Generate(); var before = Json(current.Export()); var d = current.Export();
        switch (field)
        {
            case "format": d.FormatVersion = 1; break;
            case "generator": d.GeneratorVersion = GeneratedGalaxyIdentity.GeneratorVersion; break;
            case "hash": d.ContentHash = new string('0', 64); break;
            case "feature-position": d.Features[0].Position.X++; break;
            case "feature-weight": d.Features[0].DensityWeight++; break;
            case "feature-reference": d.Features[0].NodeId = "nav.missing"; break;
            case "footprint": d.Features[0].RadiusY++; break;
            case "contribution": d.RegionInfluences.First(r => r.Contributions.Count > 0).Contributions[0].Amount++; break;
            case "tuning": d.Tuning.MaximumDensity++; break;
            case "settings": d.Settings.StellarInfluenceEnabled = false; break;
            case "layout": d.Layout.Regions[0].Center.X++; break;
            case "missing": d.Features = null!; break;
            case "oversized": d.RegionInfluences.AddRange(Enumerable.Repeat(d.RegionInfluences[0], 101)); break;
            case "rehashed": d.Features[0].Position.X++; d.ContentHash = EnvironmentGalaxyData.Hash(d); break;
            default: d = null!; break;
        }
        Assert.Throws<ArgumentException>(() => current = EnvironmentGalaxyApplication.Open(d, Catalog()));
        Assert.Equal(before, Json(current.Export()));
    }

    [Fact]
    public void StrictFormat2HostDispatchRejectsMissingDuplicateAndLegacyShapes()
    {
        var review = Generate(); var text = EnvironmentGalaxyJson.Write(review); var before = Json(review.Export());
        foreach (var bad in new[] { "null", "{", "[]", new string(' ', EnvironmentGalaxyJson.MaximumBytes + 1),
            text.Replace("\"FormatVersion\": 2,", "\"FormatVersion\": 2, \"FormatVersion\": 2,"),
            text.Replace("\"StellarInfluenceEnabled\": true,", ""),
            text.Replace("\"DensityWeight\": 650,", "\"DensityWeight\": 650, \"FakeMass\": 1,"),
            GeneratedGalaxyJson.Write(GeneratedGalaxyApplication.Generate(Settings().Map, Catalog())) })
            Assert.Throws<ArgumentException>(() => review = EnvironmentGalaxyJson.Read(bad, Catalog()));
        Assert.Throws<ArgumentException>(() => GeneratedGalaxyJson.Read(text, Catalog()));
        Assert.Equal(before, Json(review.Export()));
    }

    [Fact]
    public void InvalidSettingsAndRouteInputsDoNotChangeExistingCampaign()
    {
        Assert.Throws<ArgumentException>(() => EnvironmentGalaxyApplication.Generate(null!, Catalog()));
        var bad = Settings(); bad.Map.SystemCount = int.MaxValue;
        Assert.Throws<ArgumentException>(() => EnvironmentGalaxyApplication.Generate(bad, Catalog()));
        bad = Settings(); bad.Map.HomeCivilizationDefinitionIds[0] = "civilization.missing";
        Assert.Throws<ArgumentException>(() => EnvironmentGalaxyApplication.Generate(bad, Catalog()));
        var package = PlayableFleetPackageReader.Read(Path.Combine(AppContext.BaseDirectory, "scenarios/playable-fleet-v1"));
        var campaign = CampaignPlayableFleetApplication.StartPackage(package); var before = Json(campaign.ExportPlayableSave());
        var review = Generate(); var d = review.Export();
        var same = review.FindRoute(d.Layout.Regions[0].NodeId, d.Layout.Regions[0].NodeId);
        Assert.True(same.Success); Assert.Equal(0, same.TotalCost); Assert.Empty(same.Steps);
        Assert.Equal(RouteErrorCode.UnknownSystem, review.FindRoute("nav.missing", d.Layout.Regions[0].NodeId).ErrorCode);
        Assert.Equal(RouteErrorCode.InvalidSystemId, review.FindRoute("", d.Layout.Regions[0].NodeId).ErrorCode);
        EnvironmentGalaxyApplication.Open(d, Catalog());
        Assert.Equal(before, Json(campaign.ExportPlayableSave()));
    }

    [Theory]
    [InlineData("42", 40, "404d1e15057f42531a6f1da078efd78663d902dcff22e5484e6e3ba2cc8d68fe")]
    [InlineData("43", 40, "2cbb3c7ec0a8a83868f4d6e79cf9f2c2f4d07ead42cfe9503e6903721ce547fe")]
    [InlineData("12345", 30, "768d23805daf819671ec63a0705b85115aba66eb0a2a2aae98d648a5aaa020a7")]
    [InlineData("20261020", 50, "7717edb8d52de98e20b14574aad33430333cde62397c2e531e4f4deb8d4f6a10")]
    public void EnvironmentVersionHasSeparateCrossPlatformVectors(string seed, int size, string expected)
        => Assert.Equal(expected, Generate(seed, size).Export().ContentHash);

    [Fact]
    public void AllOffComparisonRetainsFeaturesAndOnlyBasePressureThroughRoundtrip()
    {
        var on = Generate().Export(); var settings = Settings();
        settings.StellarInfluenceEnabled = settings.BlackHoleInfluenceEnabled = settings.NebulaInfluenceEnabled = false;
        var review = EnvironmentGalaxyApplication.Generate(settings, Catalog()); var off = review.Export();
        Assert.Equal(Json(on.Features), Json(off.Features)); Assert.Equal(Json(on.Tuning), Json(off.Tuning));
        Assert.All(off.RegionInfluences, r => { Assert.Empty(r.Contributions); Assert.Equal(100, r.TargetDensity); });
        Assert.Equal(Json(off), Json(EnvironmentGalaxyJson.Read(EnvironmentGalaxyJson.Write(review), Catalog()).Export()));
    }
}
