using System.Security.Cryptography;
using System.Text.Json;
using HobbyDS9.Application;
using HobbyDS9.Contracts;
using HobbyDS9.UnityClient;
using Xunit;

namespace HobbyDS9.Simulation.Tests;

public sealed class StrategicScreenPresentationTests
{
    [Fact]
    public void MarsInitialFocusUsesRealWorldWithoutIssuingOrdersOrChangingSave()
    {
        var bytes = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "scenarios", "ds9-slice", "scenario.json"));
        var scenario = JsonSerializer.Deserialize<ScenarioDto>(bytes)!;
        scenario.ContentHash = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
        var session = CampaignApplication.StartScenario(scenario);
        var before = JsonSerializer.Serialize(session.ExportSave());
        var snapshot = session.CaptureSnapshot();
        var mars = StrategicScreenPresentation.InitialWorld(snapshot);
        Assert.NotNull(mars);
        Assert.Equal("planet.mars", mars.PlanetId.Value);
        Assert.Equal("system.sol", mars.SystemId.Value);
        Assert.Contains(snapshot.Navigation.Nodes, n => n.AssociatedSystemId == mars.SystemId);
        Assert.Equal(before, JsonSerializer.Serialize(session.ExportSave()));
    }
    [Theory]
    [InlineData("CombatPending", "Complete the pending battles")]
    [InlineData("NavigationUnknown", "choose a known region")]
    [InlineData("Rejected [InsufficientStrategicResource]", "unavailable locally")]
    [InlineData("Rejected [DesignResearchPrerequisiteMissing]", "research has not been completed")]
    [InlineData("StaleRevision", "Review the refreshed state")]
    [InlineData("UnresolvedCombats", "Resolve every pending battle")]
    [InlineData("Combat rejected: InvalidRetreat", "Choose an allowed retreat destination")]
    [InlineData("Automatic combat finalization rejected: InvalidCombatBatch", "Review the pending battles")]
    [InlineData("Combat rejected: UnknownEncounter", "no longer pending")]
    public void RawAndBracketedCommandErrorsBecomePlayerFacingReasons(string input, string expected)
    {
        var text = StrategicScreenPresentation.PlayerMessage(input);
        Assert.Contains(expected, text); Assert.DoesNotContain("[", text);
    }
    [Theory]
    [InlineData("Turn completed.")]
    [InlineData("Mars selected • Live colony data")]
    [InlineData("Scenario missing. Run PrepareUnity.proj.")]
    public void OrdinaryConfirmationsAndSetupFailuresKeepTheirSpecificMeaning(string input)
        => Assert.Equal(input, StrategicScreenPresentation.PlayerMessage(input));
    [Theory]
    [InlineData(1920,1080)]
    [InlineData(3440,1440)]
    public void TargetViewportsRetainMapMajorityWithInitialProductionDockClosed(double width, double height)
    {
        var plan = ResponsiveLayoutPolicy.ForViewport(width, height);
        var logicalWidth = width / (height / LcarsMetrics.ReferenceHeight);
        // Shell padding + rail/map gaps + bounded inspector. Production controls
        // live in the inspector, so its empire dock need not consume map space.
        var map = logicalWidth - plan.RailWidth - plan.DetailsWidth - 40;
        Assert.True(map > logicalWidth * .60);
        Assert.True(plan.RailWidth >= 180);
        Assert.False(plan.ContextDrawer);
    }
}
