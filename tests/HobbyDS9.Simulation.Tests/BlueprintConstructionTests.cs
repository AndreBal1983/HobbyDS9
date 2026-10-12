using System.Text.Json;
using HobbyDS9.Application;
using HobbyDS9.Contracts;
using HobbyDS9.Contracts.ShipDesign;
using HobbyDS9.Simulation;
using Xunit;
using static HobbyDS9.Simulation.Tests.VerticalSliceFixture;

namespace HobbyDS9.Simulation.Tests;

public sealed class BlueprintConstructionTests
{
    private const string Id = "blueprint.escort";
    private static T Clone<T>(T value) => JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(value))!;
    private sealed class Fixture { public List<ShipClassDraftDto> Drafts { get; set; } = new(); }
    private static ShipClassDraftDto Draft() => JsonSerializer.Deserialize<Fixture>(File.ReadAllText(Path.Combine(AppContext.BaseDirectory,
        "scenarios/ship-design-fixtures/fixture.json")))!.Drafts.Single(d => d.ClassId == "class.escort");
    private static ScenarioDto Content(bool empty = false)
    {
        var s = Scenario(weighted: true);
        s.InitialState.Civilizations!.Single(c => c.CivilizationId == "civ.federation").Credits = 1000;
        PopulationFixture.SetDilithium(s.InitialState.Colonies!.Single(c => c.ColonyId == "colony.sol"), 100);
        if (empty)
        {
            var ids = s.InitialState.Ships!.Select(x => x.ShipId).Concat(s.InitialState.Fleets!.Select(f => f.FleetId)).ToHashSet();
            s.InitialState.EntityIdentities.RemoveAll(e => ids.Contains(e.EntityId)); s.InitialState.Ships!.Clear(); s.InitialState.Fleets!.Clear();
        }
        return s;
    }
    private static CommandEnvelope Publication(SimulationSession s, int revision = 1, ShipClassDraftDto? draft = null, string id = "cmd.publish", string actor = "civ.federation")
    {
        var c = Command(s, CommandKind.PublishShipBlueprint, id, actor);
        c.PublishShipBlueprint = new() { BlueprintId = Id, Revision = revision, Draft = draft ?? Draft() }; return c;
    }
    private static CommandEnvelope Construction(SimulationSession s, int revision = 1, string colony = "colony.sol", string actor = "civ.federation")
    {
        var c = Command(s, CommandKind.EnqueueBlueprintConstruction, "cmd.build", actor);
        c.EnqueueBlueprintConstruction = new() { ColonyId = colony, BlueprintId = Id, Revision = revision }; return c;
    }
    private static SimulationSession Reload(SimulationSession s, ScenarioDto scenario) => new(Clone(s.ExportSave()), scenario.Definitions, Compatibility(scenario));
    private static SimulationSession Built(ScenarioDto content)
    {
        var s = Session(content); Assert.True(s.SubmitCommand(Publication(s)).Accepted); Assert.True(s.SubmitCommand(Construction(s)).Accepted);
        var save = s.ExportSave(); var q = save.GameState.Colonies!.Single(c => c.ColonyId == "colony.sol").ProductionQueue!.Single();
        q.CompletedIndustry = q.RequiredIndustry - 1;
        s = new(save, content.Definitions, Compatibility(content)); Advance(s); return s;
    }

    [Fact]
    public void PublicationIsAuthoritativeImmutableAndConsumesNoRandomnessOrEntityIds()
    {
        var s = Session(Content()); var before = s.ExportSave(); var draft = Draft(); var command = Publication(s, draft: draft);
        Assert.True(s.SubmitCommand(command).Accepted); draft.Installations!.Clear(); command.PublishShipBlueprint!.Draft!.Profiles!.Clear();
        var record = Assert.Single(s.CaptureBlueprints()); var blueprint = record.Blueprint;
        Assert.Equal(1, blueprint.Revision); Assert.Equal("class.escort", blueprint.Fit.ClassId); Assert.Equal(30, blueprint.Performance.Compiled!.MaxHull);
        Assert.Equal(4, blueprint.Performance.Compiled.MovementBudget); Assert.Equal(15, blueprint.Performance.Compiled.Combat.MediumRangeAttack);
        Assert.Equal(ShipDesignVersions.Compiler, blueprint.RulesIdentity); Assert.Equal(before.RulesVersion, record.RulesVersion);
        Assert.Equal(before.ContentHash, record.SourceContentHash); Assert.True(record.IsLatestRevision);
        Assert.Equal(before.GameState.RngState.State, s.ExportSave().GameState.RngState.State);
        Assert.Equal(before.GameState.NextEntitySequence, s.ExportSave().GameState.NextEntitySequence);
        Assert.Equal(before.GameState.Civilizations!.Single(c => c.CivilizationId == "civ.federation").Credits, s.CaptureSnapshot().Civilizations.Single(c => c.CivilizationId.Value == "civ.federation").Credits);
        var exported = s.ExportSave(); exported.GameState.Blueprints![0].FrozenCatalogue!.Components!.Clear(); exported.GameState.Blueprints[0].Fit!.DisplayName = "Edited";
        Assert.Equal("Escort", s.CaptureBlueprints().Single().Blueprint.Fit.DisplayName);
    }

    [Theory]
    [InlineData("capacity", CommandErrorCode.InvalidShipBlueprint)]
    [InlineData("power", CommandErrorCode.InvalidShipBlueprint)]
    [InlineData("missing", CommandErrorCode.InvalidShipBlueprint)]
    [InlineData("unavailable", CommandErrorCode.ShipDesignVersionUnavailable)]
    [InlineData("controller", CommandErrorCode.ActorNotControlled)]
    [InlineData("turn", CommandErrorCode.StaleTurn)]
    [InlineData("revision", CommandErrorCode.StaleRevision)]
    [InlineData("identity", CommandErrorCode.InvalidPayload)]
    [InlineData("null", CommandErrorCode.InvalidPayload)]
    [InlineData("union", CommandErrorCode.InvalidPayload)]
    [InlineData("catalogue", CommandErrorCode.ShipDesignCatalogueUnavailable)]
    public void PublicationRejectionsLeaveCompleteStateUntouched(string error, CommandErrorCode expected)
    {
        var content = Content();
        if (error == "unavailable") content.Definitions.ShipDesignVersionAccess!.Single(g => g.VersionId == "component.energy-bank.v1")
            .RequiredEngineeringApplicationDefinitionIds!.Add("engineering.advanced-starship-engineering");
        if (error == "controller") content.InitialState.Civilizations!.Single(c => c.CivilizationId == "civ.federation").ControllerKind = ControllerKind.AI;
        if (error == "catalogue") { content.Definitions.ShipDesignCatalogue = null; content.Definitions.ShipDesignVersionAccess!.Clear(); }
        var s = Session(content); var c = Publication(s);
        var draft = c.PublishShipBlueprint!.Draft!;
        if (error == "capacity") draft.Installations!.Single(i => i.ComponentVersionId == "component.energy-bank.v1").Quantity = 100;
        if (error == "power") draft.Profiles![0].Allocations![0].AllocatedPower = 100;
        if (error == "missing") { var removed = draft.Installations!.First(); draft.Installations!.Remove(removed); }
        if (error == "turn") c.ExpectedTurn++;
        if (error == "revision") c.ExpectedRevision++;
        if (error == "identity") c.PublishShipBlueprint.BlueprintId = "BAD ID";
        if (error == "null") c.PublishShipBlueprint.Draft = null;
        if (error == "union") c.EnqueueProduction = new() { ColonyId = "colony.sol", DefinitionId = "ship-design.scout" };
        var before = Json(s); Assert.Equal(expected, s.SubmitCommand(c).ErrorCode); Assert.Equal(before, Json(s));
    }

    [Fact]
    public void RevisionHistoryIsMonotonicOwnedAndSaveStable()
    {
        var content = Content(); var s = Session(content); Assert.True(s.SubmitCommand(Publication(s)).Accepted);
        var old = JsonSerializer.Serialize(s.ExportSave().GameState.Blueprints!.Single());
        var c = Publication(s, id: "cmd.duplicate"); Assert.Equal(CommandErrorCode.BlueprintRevisionConflict, s.SubmitCommand(c).ErrorCode);
        Assert.Equal(CommandErrorCode.BlueprintRevisionConflict, s.SubmitCommand(Publication(s, 3, id: "cmd.gap")).ErrorCode);
        Assert.Equal(CommandErrorCode.BlueprintNotOwned, s.SubmitCommand(Publication(s, 2, id: "cmd.foreign", actor: "civ.cardassian")).ErrorCode);
        var draft = Draft(); draft.DisplayName = "Escort II";
        Assert.True(s.SubmitCommand(Publication(s, 2, draft, "cmd.second")).Accepted);
        Assert.Equal(old, JsonSerializer.Serialize(s.ExportSave().GameState.Blueprints!.Single(b => b.Revision == 1)));
        Assert.Equal(new[] { false, true }, s.CaptureBlueprints().Select(b => b.IsLatestRevision));
        Assert.Equal(Json(s), Json(Reload(s, content))); Assert.Equal("Escort", s.CaptureBlueprints()[0].Blueprint.Fit.DisplayName);
        Assert.Equal("Escort II", s.CaptureBlueprints()[1].Blueprint.Fit.DisplayName);
    }

    [Fact]
    public void ReorderedInputsAndCultureProduceIdenticalPublicationAndFrozenState()
    {
        var content = Content(); var reordered = Clone(content);
        reordered.Definitions.ShipDesignCatalogue!.Hulls!.Reverse(); reordered.Definitions.ShipDesignCatalogue.Components!.Reverse();
        reordered.Definitions.ShipDesignVersionAccess!.Reverse();
        foreach (var component in reordered.Definitions.ShipDesignCatalogue.Components) { component.Modes!.Reverse(); component.Tags!.Reverse(); foreach (var mode in component.Modes) mode.Outputs!.Reverse(); }
        var a = Session(content); var b = Session(reordered); var draft = Draft(); draft.Installations!.Reverse(); draft.Profiles!.Reverse(); foreach (var p in draft.Profiles) p.Allocations!.Reverse();
        Assert.True(a.SubmitCommand(Publication(a)).Accepted);
        var culture = System.Globalization.CultureInfo.CurrentCulture;
        try { System.Globalization.CultureInfo.CurrentCulture = new("tr-TR"); Assert.True(b.SubmitCommand(Publication(b, draft: draft)).Accepted); }
        finally { System.Globalization.CultureInfo.CurrentCulture = culture; }
        Assert.Equal(Json(a), Json(b));
    }

    [Fact]
    public void EnqueueUsesFrozenPricesAndExactRevisionWithProportionalCancellation()
    {
        var content = Content(); var s = Session(content); Assert.True(s.SubmitCommand(Publication(s)).Accepted);
        var b = s.CaptureBlueprints().Single().Blueprint; var cost = b.Performance.Cost!; var before = s.ExportSave();
        Assert.True(s.SubmitCommand(Construction(s)).Accepted); var q = Colony(s).ProductionQueue!.Single();
        Assert.Equal(Id, q.Blueprint!.BlueprintId); Assert.Equal(1, q.Blueprint.Revision); Assert.Equal(string.Empty, q.ShipDesignDefinitionId);
        Assert.Equal(cost.Industry, q.RequiredIndustry); Assert.Equal(cost.Credits, q.PaidCredits); Assert.Equal(1000-cost.Credits, s.CaptureSnapshot().Civilizations.Single(c => c.CivilizationId.Value == "civ.federation").Credits);
        Assert.Equal(100-cost.Resources.Single().Amount, PopulationFixture.Dilithium(Colony(s)));
        var draft = Draft(); draft.DisplayName = "New revision"; Assert.True(s.SubmitCommand(Publication(s, 2, draft, "cmd.second")).Accepted);
        Assert.Equal(1, Colony(s).ProductionQueue![0].Blueprint!.Revision); s = Reload(s, content);
        var save = s.ExportSave(); save.GameState.Colonies!.Single(c => c.ColonyId == "colony.sol").ProductionQueue![0].CompletedIndustry = 10;
        s = new(save, content.Definitions, Compatibility(content)); var paid = Colony(s).ProductionQueue!.Single();
        Assert.True(Cancel(s, paid.QueueItemId).Accepted);
        Assert.Equal(1000-cost.Credits+RefundArithmetic.Unused(cost.Credits, cost.Industry, 10), s.CaptureSnapshot().Civilizations.Single(c => c.CivilizationId.Value == "civ.federation").Credits);
        Assert.Empty(Colony(s).ProductionQueue!); Assert.Equal(before.GameState.RngState.State, s.ExportSave().GameState.RngState.State);
        Assert.Equal(993, s.CaptureSnapshot().Civilizations.Single(c => c.CivilizationId.Value == "civ.federation").Credits);
        Assert.Equal(99, PopulationFixture.Dilithium(Colony(s)));
    }

    [Theory]
    [InlineData("unknown", CommandErrorCode.UnknownBlueprint)]
    [InlineData("revision", CommandErrorCode.UnknownBlueprint)]
    [InlineData("owner", CommandErrorCode.BlueprintNotOwned)]
    [InlineData("colony", CommandErrorCode.ColonyNotOwned)]
    [InlineData("absent", CommandErrorCode.UnknownColony)]
    [InlineData("yard", CommandErrorCode.ShipbuildingUnavailable)]
    [InlineData("credits", CommandErrorCode.InsufficientCredits)]
    [InlineData("resource", CommandErrorCode.InsufficientStrategicResource)]
    public void ConstructionRejectionsAreTransactional(string error, CommandErrorCode expected)
    {
        var content = Content();
        if (error == "yard") content.InitialState.Colonies!.Single(c => c.ColonyId == "colony.sol").Infrastructure!.RemoveAll(i => i.DefinitionId == "infrastructure.shipyard");
        if (error == "credits") content.InitialState.Civilizations!.Single(c => c.CivilizationId == "civ.federation").Credits = 0;
        if (error == "resource") PopulationFixture.SetDilithium(content.InitialState.Colonies!.Single(c => c.ColonyId == "colony.sol"), 0);
        var s = Session(content); Assert.True(s.SubmitCommand(Publication(s)).Accepted); var c = Construction(s);
        if (error == "unknown") c.EnqueueBlueprintConstruction!.BlueprintId = "blueprint.absent";
        if (error == "revision") c.EnqueueBlueprintConstruction!.Revision = 2;
        if (error == "owner") c.ActorCivilizationId = "civ.cardassian";
        if (error == "colony") c.EnqueueBlueprintConstruction!.ColonyId = "colony.cardassia";
        if (error == "absent") c.EnqueueBlueprintConstruction!.ColonyId = "colony.absent";
        var before = Json(s); Assert.Equal(expected, s.SubmitCommand(c).ErrorCode); Assert.Equal(before, Json(s));
    }

    [Fact]
    public void BuiltShipMovementCombatAndProvenanceSurviveReloadAndLaterPublication()
    {
        var content = Content(empty: true); var s = Built(content); var ship = Assert.Single(s.CaptureSnapshot().Ships);
        Assert.Equal(30, ship.CurrentHull); Assert.Equal(30, ship.ResolvedDesign!.MaxHull); Assert.Equal(4, ship.ResolvedDesign.MovementBudget);
        Assert.Equal(15, ship.ResolvedDesign.CombatProfile.MediumRangeAttack); Assert.Equal(1, ship.Blueprint!.Revision);
        Assert.Equal(4, s.CaptureFleetTravel(ship.FleetId.Value).MovementBudget);
        var route = Command(s, CommandKind.SetFleetRoute, "cmd.route"); route.SetFleetRoute = new() { FleetId = ship.FleetId.Value, RouteNodeIds = new() { "nav.bajor" } };
        Assert.True(s.SubmitCommand(route).Accepted); Advance(s);
        Assert.Equal(4, s.CaptureSnapshot().Fleets.Single().TransitProgress); s = Reload(s, content);
        Assert.Equal(1, s.CaptureSnapshot().Ships.Single().Blueprint!.Revision);
        var draft = Draft(); draft.DisplayName = "Replacement"; Assert.True(s.SubmitCommand(Publication(s, 2, draft, "cmd.second")).Accepted);
        Assert.Equal("Escort", s.CaptureSnapshot().Ships.Single().ResolvedDesign!.DisplayName);
        Assert.Equal(Json(s), Json(Reload(s, content)));
    }

    [Fact]
    public void CustomCombatRequestPinsCompiledProfileAndCurrentHull()
    {
        var content = Content(empty: true); var s = Built(content); var save = s.ExportSave(); var custom = save.GameState.Ships!.Single();
        var enemy = new ShipStateDto { ShipId = "ship.enemy", ShipDesignDefinitionId = "ship-design.scout", OwnerCivilizationId = "civ.cardassian", FleetId = "fleet.enemy", CurrentHull = 10 };
        save.GameState.Ships!.Add(enemy); save.GameState.EntityIdentities.Add(new() { EntityId = enemy.ShipId, DefinitionId = enemy.ShipDesignDefinitionId });
        save.GameState.EntityIdentities.Add(new() { EntityId = "fleet.enemy" });
        save.GameState.Fleets!.Add(new() { FleetId = "fleet.enemy", OwnerCivilizationId = "civ.cardassian", LocationNodeId = "nav.sol", ShipIds = new() { enemy.ShipId }, RouteNodeIds = new() });
        s = new(save, content.Definitions, Compatibility(content)); var stamp = s.CaptureSnapshot(); Assert.Equal(TurnStatus.AwaitingCombat, s.AdvanceTurn(stamp.TurnNumber, stamp.StateRevision).Status);
        var request = Assert.Single(s.GetPendingCombatRequests()); var frozen = request.Participants.SelectMany(p => p.Ships).Single(x => x.ShipId == custom.ShipId);
        Assert.Equal(Id, frozen.DesignId); Assert.Equal(30, frozen.MaxHull); Assert.Equal(15, frozen.CombatProfile!.MediumRangeAttack);
        s = Reload(s, content); Assert.Equal(JsonSerializer.Serialize(request), JsonSerializer.Serialize(s.GetPendingCombatRequests().Single()));
        var before = Json(s); Assert.Equal(CommandErrorCode.CombatPending, s.SubmitCommand(Publication(s, 2)).ErrorCode); Assert.Equal(before, Json(s));
        foreach (var pending in s.GetPendingCombatRequests()) { var now = s.CaptureSnapshot(); Assert.Equal(CombatSubmissionStatus.Buffered, s.SubmitCombatResult(new DeterministicPhasedCombatResolver().Resolve(pending), now.TurnNumber, now.StateRevision).Status); } var final = s.CaptureSnapshot(); Assert.Equal(TurnStatus.Completed, s.FinalizeCombat(final.TurnNumber, final.StateRevision).Status);
    }

    [Theory]
    [InlineData("duplicate")]
    [InlineData("gap")]
    [InlineData("compiler")]
    [InlineData("performance")]
    [InlineData("cost")]
    [InlineData("owner")]
    [InlineData("frozen")]
    [InlineData("null")]
    [InlineData("rules")]
    [InlineData("hash")]
    public void CorruptPublicationSavesAreRejected(string error)
    {
        var content = Content(); var s = Session(content); Assert.True(s.SubmitCommand(Publication(s)).Accepted); var save = s.ExportSave(); var b = save.GameState.Blueprints!.Single();
        if (error == "duplicate") save.GameState.Blueprints!.Add(Clone(b));
        if (error == "gap") b.Revision = 2;
        if (error == "compiler") b.CompilerVersion = "other";
        if (error == "performance") b.CompiledDesign!.MaxHull++;
        if (error == "cost") b.Cost!.Credits++;
        if (error == "owner") b.OwnerCivilizationId = "civ.absent";
        if (error == "frozen") b.FrozenCatalogue!.Components!.Clear();
        if (error == "null") save.GameState.Blueprints = null;
        if (error == "rules") b.RulesVersion = "other-rules";
        if (error == "hash") b.SourceContentHash = new string('a', 64);
        Assert.False(FoundationValidator.ValidateSave(save, content.Definitions, Compatibility(content)).IsValid);
        Assert.Throws<ArgumentException>(() => new SimulationSession(save, content.Definitions, Compatibility(content)));
    }

    [Fact]
    public void ApplicationRegistryCatalogueEligibilityAndShipProjectionsAreDetached()
    {
        var content = Content(); var app = CampaignApplication.StartScenario(content); var stamp = app.CaptureSnapshot().Campaign;
        var c = new CommandEnvelope { CommandId = "cmd.publish", ActorCivilizationId = "civ.federation", ExpectedTurn = stamp.TurnNumber, ExpectedRevision = stamp.StateRevision,
            Kind = CommandKind.PublishShipBlueprint, PublishShipBlueprint = new() { BlueprintId = Id, Revision = 1, Draft = Draft() } };
        var catalogue = app.GetShipDesignCatalogue(new("civ.federation"))!;
        Assert.True(app.SubmitCommand(c).Accepted); var registry = app.GetPublishedShipBlueprints(); var options = app.GetBlueprintConstructionOptions(new("civ.federation"), new("colony.sol"));
        Assert.True(Assert.Single(options).CanBuild); Assert.Single(registry); Assert.Contains("component.energy-bank.v1", catalogue.AvailableVersionIds);
        content.Definitions.ShipDesignCatalogue!.Components!.Clear(); c.PublishShipBlueprint!.Draft!.Installations!.Clear();
        Assert.Single(app.GetPublishedShipBlueprints()); Assert.NotEmpty(catalogue.Catalogue.Components); Assert.NotEmpty(registry[0].Blueprint.Components);
        Assert.Equal(CommandErrorCode.ColonyNotOwned, app.GetBlueprintConstructionOptions(new("civ.federation"), new("colony.cardassia"))[0].ErrorCode);
        var exported = app.ExportSave(); exported.GameState.Blueprints!.Clear(); Assert.Single(app.GetPublishedShipBlueprints());
    }

    [Fact]
    public void LaterEngineeredVersionDoesNotChangeFrozenQueueOrBuiltShip()
    {
        var content = Content(empty: true); var catalogue = content.Definitions.ShipDesignCatalogue!;
        var hull = Clone(catalogue.Hulls!.Single()); hull.VersionId = "hull.utility.v2"; hull.ParentVersionId = "hull.utility.v1"; hull.VersionNumber = 2; hull.MaxHull = 45;
        catalogue.Hulls!.Add(hull);
        var propulsion = Clone(catalogue.Components!.Single(c => c.Kind == ComponentKind.Propulsion));
        propulsion.VersionId = "component.propulsion.v2"; propulsion.ParentVersionId = "component.propulsion.v1"; propulsion.VersionNumber = 2;
        propulsion.Modes!.Single(m => m.Profile == OperatingProfileKind.Cruise).Outputs!.Single(o => o.Kind == ShipOutputKind.MovementBudget).Amount = 6;
        catalogue.Components!.Add(propulsion);
        foreach (var id in new[] { hull.VersionId, propulsion.VersionId }) content.Definitions.ShipDesignVersionAccess!.Add(new() { VersionId = id, RequiredEngineeringApplicationDefinitionIds = new() });
        var s = Session(content); Assert.True(s.SubmitCommand(Publication(s)).Accepted); Assert.True(s.SubmitCommand(Construction(s)).Accepted);
        var old = JsonSerializer.Serialize(s.ExportSave().GameState.Blueprints![0]); var draft = Draft(); draft.HullVersionId = hull.VersionId;
        draft.Installations!.Single(i => i.ComponentVersionId == "component.propulsion.v1").ComponentVersionId = propulsion.VersionId;
        Assert.True(s.SubmitCommand(Publication(s, 2, draft, "cmd.second")).Accepted);
        Assert.Equal(45, s.CaptureBlueprints()[1].Blueprint.Performance.Compiled!.MaxHull); Assert.Equal(6, s.CaptureBlueprints()[1].Blueprint.Performance.Compiled!.MovementBudget);
        Assert.Equal(old, JsonSerializer.Serialize(s.ExportSave().GameState.Blueprints![0])); Assert.Equal(1, Colony(s).ProductionQueue!.Single().Blueprint!.Revision);
        var save = s.ExportSave(); save.GameState.Colonies!.Single(c => c.ColonyId == "colony.sol").ProductionQueue![0].CompletedIndustry = 55;
        s = new(save, content.Definitions, Compatibility(content)); Advance(s); s = Reload(s, content);
        var ship = Assert.Single(s.CaptureSnapshot().Ships); Assert.Equal(1, ship.Blueprint!.Revision); Assert.Equal(30, ship.CurrentHull); Assert.Equal(4, ship.ResolvedDesign!.MovementBudget);
        Assert.Equal(30, ship.ResolvedDesign.MaxHull);
    }

    [Fact]
    public void AuthoritativeEngineeringUnlockControlsVersionAccess()
    {
        var content = Content(); var gate = content.Definitions.ShipDesignVersionAccess!.Single(g => g.VersionId == "component.energy-bank.v1");
        gate.RequiredEngineeringApplicationDefinitionIds!.Add("engineering.advanced-starship-engineering");
        content.Definitions.EngineeringApplications!.Single().ScienceRequirements = new();
        content.Definitions.EngineeringApplications!.Single().BaseEngineeringCost = 600;
        content.InitialState.Civilizations!.Single(c => c.CivilizationId == "civ.federation").ScienceAllocationUnits = 0;
        var s = Session(content); Assert.DoesNotContain(gate.VersionId, s.CaptureShipDesignCatalogue("civ.federation")!.AvailableVersionIds);
        Assert.Equal(CommandErrorCode.ShipDesignVersionUnavailable, s.SubmitCommand(Publication(s)).ErrorCode);
        var target = Command(s, CommandKind.SetEngineeringTarget, "cmd.unlock"); target.SetEngineeringTarget = new() { TargetDefinitionId = "engineering.advanced-starship-engineering" };
        Assert.True(s.SubmitCommand(target).Accepted); Advance(s);
        Assert.Contains(gate.VersionId, s.CaptureShipDesignCatalogue("civ.federation")!.AvailableVersionIds); Assert.True(s.SubmitCommand(Publication(s)).Accepted);
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("duplicate")]
    [InlineData("prerequisite")]
    [InlineData("resource")]
    public void ContentDeploymentGatesCannotInventUnavailableVersions(string error)
    {
        var content = Content(); var gates = content.Definitions.ShipDesignVersionAccess!;
        if (error == "missing") gates.RemoveAt(0);
        if (error == "duplicate") gates.Add(Clone(gates[0]));
        if (error == "prerequisite") gates[0].RequiredEngineeringApplicationDefinitionIds!.Add("engineering.absent");
        if (error == "resource") content.Definitions.ShipDesignCatalogue!.Hulls![0].Cost!.Resources!.Add(new() { ResourceDefinitionId = "resource.absent", Amount = 1 });
        Assert.False(FoundationValidator.ValidateScenario(content).IsValid);
    }

    [Theory]
    [InlineData("queue")]
    [InlineData("ship")]
    [InlineData("both")]
    [InlineData("owner")]
    public void DanglingOrAmbiguousShipProvenanceCannotLoad(string error)
    {
        var content = Content(empty: true); var s = Built(content); var save = s.ExportSave(); var ship = save.GameState.Ships!.Single();
        if (error == "ship") ship.Blueprint!.Revision = 2;
        if (error == "both") ship.ShipDesignDefinitionId = "ship-design.scout";
        if (error == "owner") save.GameState.Blueprints![0].OwnerCivilizationId = "civ.cardassian";
        if (error == "queue") { Assert.True(s.SubmitCommand(Construction(s)).Accepted); save = s.ExportSave(); save.GameState.Colonies!.Single(c => c.ColonyId == "colony.sol").ProductionQueue![0].Blueprint!.Revision = 2; }
        Assert.False(FoundationValidator.ValidateSave(save, content.Definitions, Compatibility(content)).IsValid);
    }

    [Fact]
    public void CustomAndStaticShipsCoexistAndUseSlowestFleetBudget()
    {
        var content = Content(); var s = Built(content); var fleet = s.CaptureSnapshot().Fleets.Single(f => f.FleetId.Value == "fleet.federation-scout");
        var ships = s.CaptureSnapshot().Ships.Where(x => x.FleetId == fleet.FleetId).ToList(); Assert.Equal(2, ships.Count);
        Assert.Single(ships, x => x.Blueprint != null); Assert.Single(ships, x => x.Blueprint == null);
        Assert.Equal(3, s.CaptureFleetTravel(fleet.FleetId.Value).MovementBudget);
        Assert.Equal(4, ships.Single(x => x.Blueprint != null).ResolvedDesign!.MovementBudget); Assert.Equal(10, ships.Single(x => x.Blueprint == null).ResolvedDesign!.MaxHull);
    }
}
