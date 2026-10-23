using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using HobbyDS9.Application;
using HobbyDS9.Contracts;
using Xunit;
using Xunit.Abstractions;

namespace HobbyDS9.Simulation.Tests;

public sealed class NaturalQuietTargetAdmissionTests
{
    readonly ITestOutputHelper output;
    public NaturalQuietTargetAdmissionTests(ITestOutputHelper output) => this.output = output;
    static List<CivilizationDefinitionDto> Catalog() => new() {
        new() { DefinitionId = "civilization.federation" }, new() { DefinitionId = "civilization.cardassian" } };
    static NaturalGalaxySettingsDto Settings(int target) => new() { Map = new() {
        Seed = "42", SystemCount = 30, EmptyRegionCount = 30, Width = 750000, Height = 600000,
        QuietEmptyAreaTargetPermille = target, HomeCivilizationDefinitionIds = Catalog().Select(c => c.DefinitionId).ToList() } };
    static string Json<T>(T value) => JsonSerializer.Serialize(value);
    static string ByteHash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();

    [Theory]
    [InlineData(1000)][InlineData(2000)][InlineData(3000)]
    public void LegacyProfilesHaveStableBytes(int target)
    {
        var catalog = Catalog();
        var natural = NaturalGalaxyApplication.Generate(Settings(target), catalog);
        var scaled = ScaledGalaxyApplication.Generate(Settings(target).Map, catalog);
        string naturalJson = NaturalGalaxyJson.Write(natural), scaledJson = ScaledGalaxyJson.Write(scaled);
        // Captured before the admission change on exact 9d0ddbe; never regenerate these to hide drift.
        var expected = target switch {
            1000 => ("4f0aeffb501da74e1d22388c76710f63960c48aad394e64eafadee8b72f0ca62", "17b901e0c1c7c535f06fb22ed52f5e7e0d9b25851862ba1fa1a993dc7a8dfa52"),
            2000 => ("832ad0d9c5bc2c8e1225f46193e1377c18527ac415990788357f75f26988c9a7", "ce4cbf7ce1b66db1d11fcdbea8ff7694543324b853bb88bee3298f27a87adc0a"),
            _ => ("ac7f1ad54b481c2e5ad9e5ed97eaadb8751b9b283f213a901b2adb859b62fc51", "c08a4ac63f8e4026392c655dcc50ddc75ba3d8942fea654a0252b2d8e50a8584") };
        Assert.Equal(expected.Item1, ByteHash(naturalJson));
        Assert.Equal(expected.Item2, ByteHash(scaledJson));
        output.WriteLine($"target={target} natural={ByteHash(naturalJson)} scaled={ByteHash(scaledJson)}");
        Assert.Equal(naturalJson, NaturalGalaxyJson.Write(NaturalGalaxyJson.Read(naturalJson, catalog)));
        Assert.Equal(scaledJson, ScaledGalaxyJson.Write(ScaledGalaxyJson.Read(scaledJson, catalog)));
    }

    [Theory]
    [InlineData(500)][InlineData(750)][InlineData(1000)][InlineData(1250)][InlineData(1500)][InlineData(1750)][InlineData(2000)]
    public void SevenNaturalChoicesRepeatReorderAndStrictRoundtripExactly(int target)
    {
        var settings = Settings(target); var catalog = Catalog();
        var review = NaturalGalaxyApplication.Generate(settings, catalog); var document = review.Export();
        string bytes = NaturalGalaxyJson.Write(review);
        Assert.Equal(target, document.Settings.Map.QuietEmptyAreaTargetPermille);
        Assert.Equal(target / 2, document.Tuning.StellarPlacementPressurePermille);
        Assert.Equal(4, document.FormatVersion);
        Assert.Equal("natural-environment-regions-v5", document.GeneratorVersion);
        Assert.Equal("provisional-natural-star-field-v1", document.TuningVersion);
        Assert.Equal(bytes, NaturalGalaxyJson.Write(NaturalGalaxyApplication.Generate(settings, catalog)));
        settings.Map.HomeCivilizationDefinitionIds.Reverse(); catalog.Reverse();
        Assert.Equal(bytes, NaturalGalaxyJson.Write(NaturalGalaxyApplication.Generate(settings, catalog)));
        document.Features.Reverse(); document.StarGroups.Reverse();
        foreach (var group in document.StarGroups) group.StarFeatureIds.Reverse();
        document.Layout.Regions.Reverse(); document.Layout.Systems.Reverse(); document.Layout.Connections.Reverse();
        document.Layout.HomeCandidates.Reverse(); document.RegionInfluences.Reverse();
        foreach (var row in document.RegionInfluences) row.Contributions.Reverse();
        Assert.Equal(bytes, NaturalGalaxyJson.Write(NaturalGalaxyJson.Read(Json(document), catalog)));
        Assert.Equal(bytes, NaturalGalaxyJson.Write(NaturalGalaxyJson.Read(bytes, catalog)));
        var original = NaturalGalaxyApplication.Generate(Settings(2000), catalog).Export();
        Assert.Equal(Json(original.Features), Json(review.Export().Features));
        Assert.Equal(Json(original.StarGroups), Json(review.Export().StarGroups));
    }

    [Theory]
    [InlineData(int.MinValue)][InlineData(-1)][InlineData(0)][InlineData(499)][InlineData(501)]
    [InlineData(749)][InlineData(751)][InlineData(1999)][InlineData(2001)][InlineData(2250)]
    [InlineData(2500)][InlineData(2750)][InlineData(2999)][InlineData(3001)][InlineData(int.MaxValue)]
    public void NaturalRejectsUnsupportedTargetsBeforeGenerationOrOpen(int target)
    {
        Assert.Contains("Unsupported quiet-empty target", Assert.Throws<ArgumentException>(
            () => NaturalGalaxyApplication.Generate(Settings(target), Catalog())).Message);
        var document = NaturalGalaxyApplication.Generate(Settings(2000), Catalog()).Export();
        document.Settings.Map.QuietEmptyAreaTargetPermille = target;
        Assert.Contains("Unsupported quiet-empty target", Assert.Throws<ArgumentException>(
            () => NaturalGalaxyApplication.Open(document, Catalog())).Message);
    }

    [Theory]
    [InlineData(500)][InlineData(750)][InlineData(1250)][InlineData(1500)][InlineData(1750)]
    public void NewNaturalTargetsCannotLeakIntoLegacyScaledAdmission(int target)
    {
        Assert.Contains("Unsupported quiet-empty target", Assert.Throws<ArgumentException>(
            () => ScaledGalaxyApplication.Generate(Settings(target).Map, Catalog())).Message);
        var document = ScaledGalaxyApplication.Generate(Settings(2000).Map, Catalog()).Export();
        document.Settings.QuietEmptyAreaTargetPermille = target;
        Assert.Contains("Unsupported quiet-empty target", Assert.Throws<ArgumentException>(
            () => ScaledGalaxyApplication.Open(document, Catalog())).Message);
    }

    [Theory]
    [InlineData("1000.5")][InlineData("\"1000\"")]
    public void StrictCodecRejectsFractionalOrStringPermille(string replacement)
    {
        var document = NaturalGalaxyApplication.Generate(Settings(1000), Catalog()).Export();
        string invalid = Json(document).Replace("\"QuietEmptyAreaTargetPermille\":1000", "\"QuietEmptyAreaTargetPermille\":" + replacement);
        Assert.Throws<ArgumentException>(() => NaturalGalaxyJson.Read(invalid, Catalog()));
    }
}
