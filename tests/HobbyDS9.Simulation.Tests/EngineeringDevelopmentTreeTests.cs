using System.Text.Json;
using HobbyDS9.Application;
using HobbyDS9.Contracts;
using HobbyDS9.Contracts.ShipDesign;
using HobbyDS9.Simulation;
using Xunit;
using static HobbyDS9.Simulation.Tests.ScienceEngineeringIntegrationTests;

namespace HobbyDS9.Simulation.Tests;

public sealed class EngineeringDevelopmentTreeTests
{
    private static readonly EntityId Actor = new("civ.federation");
    private static ScenarioDto Tree(bool scienceReady = true)
    {
        var scenario = VerticalSliceFixture.Scenario(weighted: true, fullEngineeringTree: true);
        if (scienceReady)
            foreach (var discipline in Human(scenario).Disciplines!) discipline.Level = 10;
        return scenario;
    }

    public static IEnumerable<object[]> Projects() => Tree().Definitions.EngineeringApplications!
        .Select(p => new object[] { p.DefinitionId });

    private static void Prerequisites(ScenarioDto scenario, string id)
    {
        var owner = Human(scenario);
        foreach (var prerequisite in scenario.Definitions.EngineeringApplications!.Single(p => p.DefinitionId == id).PrerequisiteDefinitionIds!)
        {
            Prerequisites(scenario, prerequisite);
            if (owner.MatureEngineeringApplicationDefinitionIds!.Contains(prerequisite)) continue;
            owner.MatureEngineeringApplicationDefinitionIds.Add(prerequisite);
            owner.EngineeringProgress!.Add(new() { DefinitionId = prerequisite,
                Progress = scenario.Definitions.EngineeringApplications!.Single(p => p.DefinitionId == prerequisite).BaseEngineeringCost });
        }
    }

    private static CommandResult Target(ICampaignSession session, string id) => SetTarget(session, id);
    private static CommandResult SetTarget(ICampaignSession session, string? id)
    {
        var command = Command(session, CommandKind.SetEngineeringTarget);
        command.SetEngineeringTarget = new() { TargetDefinitionId = id };
        return session.SubmitCommand(command);
    }
    private static void Advance(ICampaignSession session)
    {
        var stamp = session.CaptureSnapshot().Campaign;
        Assert.Equal(TurnStatus.Completed, session.AdvanceTurn(stamp.TurnNumber, stamp.StateRevision).Status);
    }
    private static string Json(ICampaignSession session) => JsonSerializer.Serialize(session.ExportSave());

    [Fact]
    public void TwentyRealProjectsAreInitiallyScienceLockedAndEveryNewProjectGatesAVersion()
    {
        var scenario = Tree(scienceReady: false);
        Assert.Empty(CampaignApplication.ValidateScenario(scenario));
        var session = CampaignApplication.StartScenario(scenario);
        var cards = ResearchOverviewSnapshot.For(session.CaptureSnapshot(), Actor).Engineering;
        Assert.Equal(20, cards.Count);
        Assert.Equal(20, cards.Select(c => c.Definition.DefinitionId).Distinct().Count());
        Assert.All(cards, c => Assert.Equal(CommandErrorCode.EngineeringScienceRequirementMissing, c.UnavailableReason));
        var initial = Json(session);
        foreach (var card in cards)
        {
            Assert.Equal(CommandErrorCode.EngineeringScienceRequirementMissing, Target(session, card.Definition.DefinitionId.Value).ErrorCode);
            Assert.Equal(initial, Json(session));
        }
        var gates = scenario.Definitions.ShipDesignVersionAccess!;
        Assert.All(scenario.Definitions.EngineeringApplications!.Where(p => p.DefinitionId != "engineering.advanced-starship-engineering"),
            p => Assert.Single(gates, g => g.RequiredEngineeringApplicationDefinitionIds!.Contains(p.DefinitionId)));
        Assert.Equal(14, session.GetShipDesignCatalogue(Actor)!.AvailableVersionIds.Count);
    }

    [Theory]
    [MemberData(nameof(Projects))]
    public void EachProjectCompletesThroughCampaignAuthorityAndSurvivesReload(string id)
    {
        var scenario = Tree();
        Prerequisites(scenario, id);
        var project = scenario.Definitions.EngineeringApplications!.Single(p => p.DefinitionId == id);
        Human(scenario).EngineeringReserve = project.BaseEngineeringCost;
        var session = CampaignApplication.StartScenario(scenario);
        var originalShips = JsonSerializer.Serialize(session.ExportSave().GameState.Ships);
        var oldCatalogue = session.GetShipDesignCatalogue(Actor)!;
        var unlocks = scenario.Definitions.ShipDesignVersionAccess!
            .Where(g => g.RequiredEngineeringApplicationDefinitionIds!.Contains(id)).Select(g => g.VersionId).ToList();
        Assert.All(unlocks, v => Assert.DoesNotContain(v, oldCatalogue.AvailableVersionIds));
        var before = session.ExportSave();
        Assert.True(Target(session, id).Accepted);
        Assert.Equal(before.GameState.RngState.State, session.ExportSave().GameState.RngState.State);
        Advance(session);
        var card = ResearchOverviewSnapshot.For(session.CaptureSnapshot(), Actor).Engineering.Single(c => c.Definition.DefinitionId.Value == id);
        Assert.Equal(100, card.MaturityPercent);
        Assert.Equal(project.BaseEngineeringCost, card.Progress);
        Assert.Null(ResearchOverviewSnapshot.For(session.CaptureSnapshot(), Actor).Target);
        Assert.All(unlocks, v => Assert.Contains(v, session.GetShipDesignCatalogue(Actor)!.AvailableVersionIds));
        Assert.All(unlocks, v => Assert.DoesNotContain(v, oldCatalogue.AvailableVersionIds));
        Assert.Equal(originalShips, JsonSerializer.Serialize(session.ExportSave().GameState.Ships));
        var save = session.ExportSave();
        var restored = CampaignApplication.LoadSave(scenario, JsonSerializer.Deserialize<SaveEnvelopeDto>(JsonSerializer.Serialize(save))!);
        Assert.Equal(Json(session), Json(restored));
        Assert.Equal(session.GetShipDesignCatalogue(Actor)!.AvailableVersionIds, restored.GetShipDesignCatalogue(Actor)!.AvailableVersionIds);
        var frozen = Json(restored);
        Assert.Equal(CommandErrorCode.EngineeringAlreadyMature, Target(restored, id).ErrorCode);
        Assert.Equal(frozen, Json(restored));
    }

    [Fact]
    public void SwitchingProjectsAndReloadingPreservesBothProgressAndPrerequisiteLocks()
    {
        var scenario = Tree();
        var session = CampaignApplication.StartScenario(scenario);
        var before = Json(session);
        Assert.Equal(CommandErrorCode.EngineeringPrerequisiteMissing, Target(session, "engineering.high-speed-drive").ErrorCode);
        Assert.Equal(before, Json(session));
        Assert.True(Target(session, "engineering.efficient-drive").Accepted);
        Advance(session);
        Assert.True(Target(session, "engineering.efficient-shield").Accepted);
        Advance(session);
        var saved = session.ExportSave();
        var progress = saved.GameState.Civilizations!.Single(c => c.CivilizationId == Actor.Value).EngineeringProgress!;
        Assert.Equal(720, progress.Single(p => p.DefinitionId == "engineering.efficient-drive").Progress);
        Assert.Equal(720, progress.Single(p => p.DefinitionId == "engineering.efficient-shield").Progress);
        session = CampaignApplication.LoadSave(scenario, saved);
        Assert.True(Target(session, "engineering.efficient-drive").Accepted);
        Advance(session);
        var cards = ResearchOverviewSnapshot.For(session.CaptureSnapshot(), Actor).Engineering;
        Assert.Equal(1440, cards.Single(c => c.Definition.DefinitionId.Value == "engineering.efficient-drive").Progress);
        Assert.Equal(720, cards.Single(c => c.Definition.DefinitionId.Value == "engineering.efficient-shield").Progress);
        Assert.True(SetTarget(session, null).Accepted);
        Advance(session);
        Assert.Equal(720, ResearchOverviewSnapshot.For(session.CaptureSnapshot(), Actor).EngineeringReserve);
    }

    [Theory]
    [InlineData("missing-output")]
    [InlineData("base-version-only")]
    [InlineData("cycle")]
    [InlineData("unknown-prerequisite")]
    public void InvalidRefinementOrTreeCannotStart(string defect)
    {
        var scenario = Tree();
        var project = scenario.Definitions.EngineeringApplications!.Single(p => p.DefinitionId == "engineering.compact-reactor");
        var gate = scenario.Definitions.ShipDesignVersionAccess!.Single(g => g.VersionId == "component.reactor.v2");
        if (defect == "missing-output") gate.RequiredEngineeringApplicationDefinitionIds!.Clear();
        if (defect == "base-version-only")
        {
            gate.RequiredEngineeringApplicationDefinitionIds!.Clear();
            scenario.Definitions.ShipDesignVersionAccess!.Single(g => g.VersionId == "component.reactor.v1")
                .RequiredEngineeringApplicationDefinitionIds!.Add(project.DefinitionId);
        }
        if (defect == "cycle") project.PrerequisiteDefinitionIds!.Add("engineering.high-output-reactor");
        if (defect == "unknown-prerequisite") project.PrerequisiteDefinitionIds!.Add("engineering.absent");
        Assert.NotEmpty(CampaignApplication.ValidateScenario(scenario));
        Assert.Throws<ArgumentException>(() => CampaignApplication.StartScenario(scenario));
    }

    [Fact]
    public void EngineAndShieldUnlocksAffectNewBlueprintsWithoutChangingOldPublishedPerformance()
    {
        var scenario = Tree();
        Human(scenario).EngineeringReserve = 200000;
        var session = CampaignApplication.StartScenario(scenario);
        var draft = Escort();
        Publish(session, draft, 1);
        var old = session.GetPublishedShipBlueprints().Single().Blueprint;
        var oldHull = old.Performance.Compiled!.MaxHull;
        var oldMovement = old.Performance.Compiled.MovementBudget;
        var oldShields = old.Performance.Compiled.Combat.ShieldCapacity;
        draft.Installations!.Single(i => i.InstallationId == "fit.propulsion").ComponentVersionId = "component.propulsion.v3";
        var locked = Command(session, CommandKind.PublishShipBlueprint, "cmd.publish.locked");
        locked.PublishShipBlueprint = new() { BlueprintId = "blueprint.engineering-test", Revision = 2, Draft = draft };
        Assert.False(session.SubmitCommand(locked).Accepted);
        Assert.True(Target(session, "engineering.efficient-drive").Accepted); Advance(session);
        Assert.True(Target(session, "engineering.high-speed-drive").Accepted); Advance(session);
        Assert.True(Target(session, "engineering.resilient-shield").Accepted); Advance(session);
        draft.Installations!.Single(i => i.InstallationId == "fit.escort-shield").ComponentVersionId = "component.escort-shield.v2";
        // Fund the new versions' real power demands by reducing optional weapons.
        foreach (var profile in draft.Profiles!)
        foreach (var allocation in profile.Allocations!)
        {
            if (allocation.InstallationId == "fit.propulsion" && profile.Kind == OperatingProfileKind.Cruise)
                allocation.RequestedPower = allocation.AllocatedPower = 32;
            if (allocation.InstallationId == "fit.escort-shield" && profile.Kind == OperatingProfileKind.Combat)
                allocation.RequestedPower = allocation.AllocatedPower = 18;
            if (allocation.InstallationId == "fit.energy-bank") allocation.RequestedPower = allocation.AllocatedPower = 0;
        }
        // Remove optional support and one bank to fit the larger drive and shield.
        var removed = draft.Installations!.Where(i => i.InstallationId == "fit.escort-support").Select(i => i.InstallationId).ToHashSet();
        draft.Installations!.RemoveAll(i => removed.Contains(i.InstallationId));
        foreach (var p in draft.Profiles!) p.Allocations!.RemoveAll(a => removed.Contains(a.InstallationId));
        draft.Installations!.Single(i => i.InstallationId == "fit.energy-bank").Quantity = 1;
        Publish(session, draft, 2);
        var records = session.GetPublishedShipBlueprints();
        var upgraded = records.Single(r => r.Blueprint.Revision == 2).Blueprint;
        Assert.Equal(5, upgraded.Performance.Compiled!.MovementBudget);
        Assert.Equal(28, upgraded.Performance.Compiled.Combat.ShieldCapacity);
        var retained = records.Single(r => r.Blueprint.Revision == 1).Blueprint;
        Assert.Equal(oldMovement, retained.Performance.Compiled!.MovementBudget);
        Assert.Equal(oldShields, retained.Performance.Compiled.Combat.ShieldCapacity);
        Assert.Equal(oldHull, retained.Performance.Compiled.MaxHull);
        var restored = CampaignApplication.LoadSave(scenario, session.ExportSave());
        Assert.Equal(Json(session), Json(restored));
    }

    private sealed class DraftFile { public List<ShipClassDraftDto> Drafts { get; set; } = new(); }
    private static ShipClassDraftDto Escort() => JsonSerializer.Deserialize<DraftFile>(File.ReadAllText(
        Path.Combine(AppContext.BaseDirectory, "scenarios/ship-design-fixtures/fixture.json")))!.Drafts.Single(d => d.ClassId == "class.escort");
    private static void Publish(ICampaignSession session, ShipClassDraftDto draft, int revision)
    {
        var command = Command(session, CommandKind.PublishShipBlueprint, "cmd.publish." + revision);
        command.PublishShipBlueprint = new() { BlueprintId = "blueprint.engineering-test", Revision = revision, Draft = draft };
        var result = session.SubmitCommand(command);
        Assert.True(result.Accepted, result.ErrorCode.ToString());
    }
}
