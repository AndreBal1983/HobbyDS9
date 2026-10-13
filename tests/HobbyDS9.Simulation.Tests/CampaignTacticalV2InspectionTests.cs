using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using HobbyDS9.Application;
using HobbyDS9.Contracts;
using HobbyDS9.Contracts.ShipDesign;
using HobbyDS9.Simulation;
using Xunit;
using Xunit.Abstractions;
using static HobbyDS9.Simulation.Tests.VerticalSliceFixture;

namespace HobbyDS9.Simulation.Tests;

public sealed class CampaignTacticalV2InspectionTests
{
    private readonly ITestOutputHelper output;
    public CampaignTacticalV2InspectionTests(ITestOutputHelper output) => this.output = output;
    private const string Actor = "civ.federation";
    private static T Clone<T>(T value) => JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(value))!;
    private static string Report(SimulationSession s) => JsonSerializer.Serialize(s.InspectCampaignTacticalV2(Actor));

    [Theory]
    [InlineData(null, CampaignTacticalInspectionError.InvalidActor)]
    [InlineData("", CampaignTacticalInspectionError.InvalidActor)]
    [InlineData("CIV.FEDERATION", CampaignTacticalInspectionError.InvalidActor)]
    [InlineData("civ.unknown", CampaignTacticalInspectionError.UnknownActor)]
    public void UnauthorizedInspectionHasNoShipOrEquipmentDisclosure(string? actor, CampaignTacticalInspectionError error)
    {
        var s = Session(); var before = Json(s); var report = s.InspectCampaignTacticalV2(actor);
        Assert.Equal(error, report.ErrorCode); Assert.False(report.CanActivate);
        Assert.Empty(report.OwnedShips); Assert.Empty(report.OwnedBlueprintRevisions); Assert.Null(report.CurrentCatalogue);
        Assert.Equal(before, Json(s));
    }

    [Fact]
    public void AiActorCannotUseTheHumanInspectionCapability()
    {
        var scenario = Scenario(humans: false); var ai = scenario.InitialState.Civilizations!.First(c => c.ControllerKind == ControllerKind.AI);
        var s = Session(scenario); var before = Json(s); var report = s.InspectCampaignTacticalV2(ai.CivilizationId);
        Assert.Equal(CampaignTacticalInspectionError.ActorNotControlled, report.ErrorCode);
        Assert.Null(report.CurrentCatalogue); Assert.Empty(report.OwnedShips); Assert.Equal(before, Json(s));
    }

    [Theory]
    [InlineData("civ.federation")]
    [InlineData("civ.cardassian")]
    [InlineData("civ.dominion")]
    public void HotSeatHumanInspectionOnlyShowsThatActorsAuthoritativeSources(string actor)
    {
        var scenario = Scenario(); var s = Session(scenario); var report = s.InspectCampaignTacticalV2(actor);
        Assert.Equal(CampaignTacticalInspectionError.None, report.ErrorCode);
        Assert.Equal(scenario.InitialState.Ships!.Count(v => v.OwnerCivilizationId == actor), report.OwnedShips.Count);
        Assert.All(report.OwnedShips, v => Assert.Equal(actor, v.Ship.OwnerCivilizationId.Value));
        Assert.All(report.OwnedShips, v => { Assert.Null(v.FrozenBlueprint); Assert.False(v.CanProjectTacticalV2); });
        Assert.False(report.CanActivate); Assert.Equal(CampaignCombatRules.ResolverId, report.CampaignResolverId);
    }

    [Theory]
    [InlineData(0UL)]
    [InlineData(1UL)]
    [InlineData(47UL)]
    [InlineData(ulong.MaxValue)]
    public void InspectionConsumesNoRandomnessEntityIdsOrRevisionAcrossSeeds(ulong seed)
    {
        var scenario = Scenario(); scenario.InitialState.RngState.State = seed;
        var s = Session(scenario); var before = Json(s); var a = Report(s); Assert.Equal(a, Report(s));
        Assert.Equal(before, Json(s)); Assert.Equal(seed, s.ExportSave().GameState.RngState.State);
    }

    [Fact]
    public void EmptyFleetCannotBypassTheUnimplementedLifecycleGate()
    {
        var scenario = Scenario(); var ids = scenario.InitialState.Ships!.Select(s => s.ShipId)
            .Concat(scenario.InitialState.Fleets!.Select(f => f.FleetId)).ToHashSet();
        scenario.InitialState.EntityIdentities.RemoveAll(v => ids.Contains(v.EntityId));
        scenario.InitialState.Ships!.Clear(); scenario.InitialState.Fleets!.Clear();
        var report = Session(scenario).InspectCampaignTacticalV2(Actor);
        Assert.Empty(report.OwnedShips); Assert.False(report.CanActivate);
        Assert.Contains(CampaignTacticalV2Blocker.PersistentShipResourcesAndRecoveryPolicyRequired, report.Blockers);
        Assert.Contains(CampaignTacticalV2Blocker.StrategicRetreatAndObjectivePolicyRequired, report.Blockers);
        Assert.Contains(CampaignTacticalV2Blocker.VersionedCampaignActivationAndSaveContractRequired, report.Blockers);
    }

    [Fact]
    public void StaticDesignNamesAndScalarValuesNeverCreateAWeaponFit()
    {
        var scenario = Scenario(); var design = scenario.Definitions.ShipDesigns!.First();
        design.DisplayName = "Beam Cannon Torpedo Fore Armor 100";
        design.CombatProfile!.ShieldCapacity = 400; design.CombatProfile.LongRangeAttack = 99;
        var s = Session(scenario); var report = s.InspectCampaignTacticalV2(Actor);
        Assert.All(report.OwnedShips, source =>
        {
            Assert.Null(source.FrozenBlueprint); Assert.False(source.CanProjectTacticalV2);
            Assert.Contains(CampaignTacticalV2Blocker.WeaponClassesAndArcsRequired, source.MissingFitSemantics);
            Assert.Contains(CampaignTacticalV2Blocker.ArmorDefinitionRequired, source.MissingFitSemantics);
        });
    }

    private sealed class DraftFixture { public List<ShipClassDraftDto> Drafts { get; set; } = new(); }
    private static ShipClassDraftDto Draft() => JsonSerializer.Deserialize<DraftFixture>(File.ReadAllText(Path.Combine(
        AppContext.BaseDirectory, "scenarios/ship-design-fixtures/fixture.json")))!.Drafts.Single(d => d.ClassId == "class.escort");
    private static void Publish(SimulationSession s, int revision)
    {
        var command = Command(s, CommandKind.PublishShipBlueprint, "cmd.publish." + revision.ToString(CultureInfo.InvariantCulture));
        var draft = Draft(); draft.DisplayName = "Revision " + revision.ToString(CultureInfo.InvariantCulture);
        command.PublishShipBlueprint = new() { BlueprintId = "blueprint.escort", Revision = revision, Draft = draft };
        Assert.True(s.SubmitCommand(command).Accepted);
    }
    private static (SimulationSession session, ScenarioDto scenario) Built()
    {
        var scenario = Scenario(weighted: true);
        scenario.InitialState.Civilizations!.Single(c => c.CivilizationId == Actor).Credits = 1000;
        PopulationFixture.SetDilithium(scenario.InitialState.Colonies!.Single(c => c.ColonyId == "colony.sol"), 100);
        var s = Session(scenario); Publish(s, 1);
        var command = Command(s, CommandKind.EnqueueBlueprintConstruction, "cmd.build");
        command.EnqueueBlueprintConstruction = new() { BlueprintId = "blueprint.escort", Revision = 1, ColonyId = "colony.sol" };
        Assert.True(s.SubmitCommand(command).Accepted);
        // Fixture accelerates already-paid work; production still creates the actual ship.
        var save = s.ExportSave(); var q = save.GameState.Colonies!.Single(c => c.ColonyId == "colony.sol").ProductionQueue!.Single();
        q.CompletedIndustry = q.RequiredIndustry - 1;
        s = new(save, scenario.Definitions, Compatibility(scenario)); Advance(s); Publish(s, 2);
        return (s, scenario);
    }

    [Fact]
    public void ProductionShipInspectionUsesExactFrozenRevisionAndRealPowerAllocations()
    {
        var (s, _) = Built(); var before = Json(s); var report = s.InspectCampaignTacticalV2(Actor);
        var source = Assert.Single(report.OwnedShips, v => v.Ship.Blueprint != null);
        var frozen = source.FrozenBlueprint!;
        Assert.Equal(1, frozen.Blueprint.Revision); Assert.Equal("Revision 1", frozen.Blueprint.Fit.DisplayName);
        Assert.False(frozen.IsLatestRevision); Assert.Equal(2, report.OwnedBlueprintRevisions.Count);
        Assert.Equal(source.Ship.ResolvedDesign!.MaxHull, frozen.Blueprint.Performance.Compiled!.MaxHull);
        Assert.Equal(source.Ship.ResolvedDesign.CombatProfile.MediumRangeAttack, frozen.Blueprint.Performance.Compiled.Combat.MediumRangeAttack);
        Assert.NotEmpty(frozen.Blueprint.Hull.Mounts); Assert.NotEmpty(frozen.Blueprint.Components);
        var combat = frozen.Blueprint.Performance.Profiles.Single(p => p.Kind == OperatingProfileKind.Combat);
        Assert.True(combat.AllocatedPower <= combat.AvailablePower); Assert.NotEmpty(combat.Installations);
        Assert.False(source.CanProjectTacticalV2); Assert.Equal(before, Json(s));
    }

    [Fact]
    public void CurrentCatalogueAndLaterEngineeringAccessDoNotRecompileAnOldShipsSource()
    {
        var (s, scenario) = Built(); var old = s.InspectCampaignTacticalV2(Actor).OwnedShips.Single(v => v.Ship.Blueprint != null);
        var changed = Clone(scenario);
        changed.Definitions.ShipDesignCatalogue!.Hulls!.Single(h => h.VersionId == old.FrozenBlueprint!.Blueprint.Fit.HullVersionId).MaxHull += 100;
        changed.Definitions.ShipDesignVersionAccess!.ForEach(g => g.RequiredEngineeringApplicationDefinitionIds!.Add("engineering.advanced-starship-engineering"));
        var loaded = new SimulationSession(Clone(s.ExportSave()), changed.Definitions, Compatibility(changed));
        var report = loaded.InspectCampaignTacticalV2(Actor); var source = report.OwnedShips.Single(v => v.Ship.Blueprint != null);
        Assert.Empty(report.CurrentCatalogue!.AvailableVersionIds);
        Assert.Equal(JsonSerializer.Serialize(old), JsonSerializer.Serialize(source));
        Assert.Equal(old.FrozenBlueprint!.SourceContentHash, source.FrozenBlueprint!.SourceContentHash);
    }

    [Fact]
    public void InspectionRemainsCanonicalAcrossInputPermutationAndCulture()
    {
        var (s, scenario) = Built(); var saved = s.ExportSave(); var altered = Clone(saved);
        altered.GameState.Ships!.Reverse(); altered.GameState.Blueprints!.Reverse(); altered.GameState.Fleets!.Reverse();
        altered.GameState.Blueprints.ForEach(b => { b.Fit!.Installations!.Reverse(); b.Fit.Profiles!.Reverse(); b.FrozenCatalogue!.Components!.Reverse(); });
        var other = new SimulationSession(altered, scenario.Definitions, Compatibility(scenario));
        var culture = CultureInfo.CurrentCulture;
        try { CultureInfo.CurrentCulture = new("tr-TR"); Assert.Equal(Report(s), Report(other)); }
        finally { CultureInfo.CurrentCulture = culture; }
    }

    [Fact]
    public void ExportedSaveAndContentMutationsCannotAlterAReportOrAuthoritativeSources()
    {
        var (s, scenario) = Built(); var report = s.InspectCampaignTacticalV2(Actor); var expected = JsonSerializer.Serialize(report);
        var saved = s.ExportSave(); saved.GameState.Blueprints![0].Fit!.Installations!.Clear(); saved.GameState.Ships!.Clear();
        scenario.Definitions.ShipDesignCatalogue!.Components!.Clear();
        Assert.Equal(expected, JsonSerializer.Serialize(report)); Assert.Equal(expected, Report(s));
        Assert.Throws<NotSupportedException>(() => ((IList<CampaignTacticalShipSourceSnapshot>)report.OwnedShips).Clear());
        Assert.Throws<NotSupportedException>(() => ((IList<CampaignTacticalV2Blocker>)report.Blockers).Clear());
    }

    [Fact]
    public void ApplicationCapabilitySurvivesSaveLoadWithoutChangingExistingSessionInterface()
    {
        var (s, scenario) = Built(); var app = CampaignApplication.LoadSave(scenario, Clone(s.ExportSave()));
        var before = JsonSerializer.Serialize(app.ExportSave());
        var report = ((ICampaignTacticalV2Inspection)app).InspectCampaignTacticalV2(Actor);
        Assert.Equal(Report(s), JsonSerializer.Serialize(report)); Assert.Equal(before, JsonSerializer.Serialize(app.ExportSave()));
        Assert.Equal(14, app.ExportSave().SaveFormatVersion); Assert.Equal(10, scenario.Definitions.ContentFormatVersion);
        Assert.Equal(ShipDesignVersions.Compiler, report.OwnedBlueprintRevisions[0].Blueprint.RulesIdentity);
    }

    [Fact]
    public void PendingBufferedReloadAndFinalizedV1BattlesStayV1AfterInspection()
    {
        var scenario = CampaignCombatFixture.Scenario(3); var app = CampaignApplication.StartScenario(scenario);
        var stamp = app.CaptureSnapshot().Campaign;
        Assert.Equal(TurnStatus.AwaitingCombat, app.AdvanceTurn(stamp.TurnNumber, stamp.StateRevision).Status);
        foreach (var request in app.GetPendingCombatRequests().Reverse())
        {
            var before = JsonSerializer.Serialize(app.ExportSave());
            var report = ((ICampaignTacticalV2Inspection)app).InspectCampaignTacticalV2(Actor);
            Assert.Equal(SessionPhase.AwaitingCombat, report.Phase); Assert.False(report.CanActivate);
            Assert.Equal(before, JsonSerializer.Serialize(app.ExportSave()));
            stamp = app.CaptureSnapshot().Campaign;
            Assert.Equal(CombatSubmissionStatus.Buffered, app.SubmitCombatResult(app.ResolveCombat(request), stamp.TurnNumber, stamp.StateRevision).Status);
            app = CampaignApplication.LoadSave(scenario, Clone(app.ExportSave()));
        }
        stamp = app.CaptureSnapshot().Campaign;
        var finalReport = ((ICampaignTacticalV2Inspection)app).InspectCampaignTacticalV2(Actor);
        Assert.Equal(stamp.StateRevision, finalReport.StateRevision); Assert.Equal(stamp.TurnNumber, finalReport.TurnNumber);
        Assert.Equal(TurnStatus.Completed, app.FinalizeCombat(stamp.TurnNumber, stamp.StateRevision).Status);
        Assert.Equal(SessionPhase.Planning, ((ICampaignTacticalV2Inspection)app).InspectCampaignTacticalV2(Actor).Phase);
    }

    [Fact]
    public void ForeignFrozenBlueprintsNeverEnterAnActorsSourceInventory()
    {
        var s = Session(); Publish(s, 1);
        var command = Command(s, CommandKind.PublishShipBlueprint, "cmd.foreign", "civ.cardassian");
        command.PublishShipBlueprint = new() { BlueprintId = "blueprint.cardassian", Revision = 1, Draft = Draft() };
        Assert.True(s.SubmitCommand(command).Accepted);
        var federation = s.InspectCampaignTacticalV2(Actor);
        var cardassian = s.InspectCampaignTacticalV2("civ.cardassian");
        Assert.Equal("blueprint.escort", Assert.Single(federation.OwnedBlueprintRevisions).Blueprint.BlueprintId);
        Assert.Equal("blueprint.cardassian", Assert.Single(cardassian.OwnedBlueprintRevisions).Blueprint.BlueprintId);
    }

    [Fact]
    public void LegacyStaticCampaignWithoutAnEquipmentCatalogueRemainsInspectableAndIneligible()
    {
        var scenario = Scenario(); scenario.Definitions.ShipDesignCatalogue = null;
        scenario.Definitions.ShipDesignVersionAccess!.Clear();
        var s = Session(scenario); var before = Json(s); var report = s.InspectCampaignTacticalV2(Actor);
        Assert.Equal(CampaignTacticalInspectionError.None, report.ErrorCode); Assert.Null(report.CurrentCatalogue);
        Assert.NotEmpty(report.OwnedShips); Assert.False(report.CanActivate); Assert.Equal(before, Json(s));
    }

    [Fact]
    public void InspectionCannotAuthorizeAResolverV2ResultIntoAV1CampaignCheckpoint()
    {
        var s = Session(CampaignCombatFixture.Scenario()); var stamp = s.CaptureSnapshot();
        Assert.Equal(TurnStatus.AwaitingCombat, s.AdvanceTurn(stamp.TurnNumber, stamp.StateRevision).Status);
        var request = Assert.Single(s.GetPendingCombatRequests()); var report = s.InspectCampaignTacticalV2(Actor);
        var forged = CampaignCombatFixture.NoChanges(request);
        forged.ResolverId = TacticalCombatV2Identity.ResolverId; forged.ResolverVersion = TacticalCombatV2Identity.ResolverVersion;
        var before = Json(s);
        Assert.Equal(CombatSubmissionError.ResolverMismatch,
            s.SubmitCombatResult(forged, report.TurnNumber, report.StateRevision).ErrorCode);
        Assert.Equal(before, Json(s));
        Assert.Equal(CombatSubmissionStatus.Buffered,
            s.SubmitCombatResult(CampaignCombatFixture.NoChanges(request), report.TurnNumber, report.StateRevision).Status);
    }

    [Fact]
    public void ThousandProductionShipsShareTheSameImmutableFrozenRevisionWithoutChangingState()
    {
        var (s, scenario) = Built(); var save = s.ExportSave(); var original = save.GameState.Ships!.Single(v => v.Blueprint != null);
        var fleet = save.GameState.Fleets!.Single(v => v.FleetId == original.FleetId);
        for (var i = 1; i < 1000; i++)
        {
            var ship = Clone(original); ship.ShipId = "ship.inspection." + i.ToString("D4", CultureInfo.InvariantCulture);
            save.GameState.Ships!.Add(ship); fleet.ShipIds!.Add(ship.ShipId);
            save.GameState.EntityIdentities.Add(new() { EntityId = ship.ShipId });
        }
        var large = new SimulationSession(save, scenario.Definitions, Compatibility(scenario)); var before = Json(large);
        var allocated = GC.GetAllocatedBytesForCurrentThread(); var timer = Stopwatch.StartNew();
        var report = large.InspectCampaignTacticalV2(Actor); timer.Stop(); allocated = GC.GetAllocatedBytesForCurrentThread() - allocated;
        var sources = report.OwnedShips.Where(v => v.Ship.Blueprint != null).ToList(); Assert.Equal(1000, sources.Count);
        Assert.All(sources, v => Assert.Same(sources[0].FrozenBlueprint, v.FrozenBlueprint)); Assert.Equal(before, Json(large));
        output.WriteLine(FormattableString.Invariant($"inspection ships=1000, revisions=2, elapsed-ms={timer.Elapsed.TotalMilliseconds:F3}, allocated-bytes={allocated}"));
    }
}
