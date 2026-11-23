using HobbyDS9.Application;
using HobbyDS9.Contracts;
using Xunit;

namespace HobbyDS9.Simulation.Tests;

public sealed partial class PlayableFleetTests
{
    static (CampaignPlayableFleetSaveDto Before, CampaignPlayableFleetSaveDto Pending) BeforeCapacityEncounter(CampaignPlayableFleetPackageDto p)
    {
        var app = CampaignPlayableFleetApplication.StartPackage(p);
        Buy(app); Travel(app, CampaignTravelIntent.Leave);
        for (int i = 0; i < 12; i++)
        {
            var before = app.ExportPlayableSave(); Advance(app);
            if (app.CaptureStatus().Phase == SessionPhase.AwaitingCombat) return (before, app.ExportPlayableSave());
        }
        throw new InvalidOperationException("Capacity fixture did not reach a battle.");
    }
    static int Continuations(CampaignPlayableFleetSaveDto save) => 1 + save.Integrated!.Physical!.Core.Battles
        .Where(b => b.State.Completion == TacticalCompletion.None).Sum(b => 4 - b.State.NextRound);
    static ICampaignPlayableFleetSession Reload(CampaignPlayableFleetPackageDto p, ICampaignPlayableFleetSession app)
    {
        var save = app.ExportPlayableSave();
        var restored = CampaignPlayableFleetApplication.LoadSave(p, Clone(save));
        Assert.Equal(Json(save), Json(restored.ExportPlayableSave())); return restored;
    }
    static void FinishAtCapacity(CampaignPlayableFleetPackageDto p, ICampaignPlayableFleetSession app)
    {
        foreach (var id in app.GetEncounters(Human).Reverse())
        {
            while (!app.Observe(id, Human).Complete)
            {
                var before = Json(app.ExportPlayableSave()); var status = app.CaptureStatus();
                var fleet = app.GetOwnStrategicFleets(Human).First().FleetId.Value;
                // All nonprogress APIs and malformed/stale round submissions must preserve the reserve.
                Assert.False(app.AdvanceTurn(status.Turn, status.CampaignRevision).Accepted);
                Assert.False(app.FinalizeBatch(status.Turn, status.CampaignRevision).Accepted);
                Assert.False(app.SubmitFleetOrganization(Organization(app, Array.Empty<string>(), CampaignFleetOrganizationKind.Rename, fleet, "Spam")).Accepted);
                Assert.False(app.SetApproachCloak(Human, "command.spam.cloak", fleet, true, status.Turn, status.CampaignRevision).Accepted);
                Assert.False(app.SubmitStrategicCommand(new CommandEnvelope { ActorCivilizationId = Human, CommandId = "command.spam",
                    ExpectedTurn = status.Turn, ExpectedRevision = status.CampaignRevision, Kind = CommandKind.EnqueueProduction,
                    EnqueueProduction = new EnqueueProductionPayload { ColonyId = "colony.sol", DefinitionId = "ship-design.source-cruiser" } }).Accepted);
                Assert.False(app.SubmitTravelOrder(new SubmitCampaignTravelOrderDto { ActorId = Human, CommandId = "command.spam.travel",
                    ExpectedTurn = status.Turn, ExpectedRevision = status.CampaignRevision,
                    Order = new CampaignTravelOrderDto { FleetId = fleet, Intent = CampaignTravelIntent.Stay, RouteNodeIds = new() } }).Accepted);
                var view = app.Observe(id, Human);
                var invalid = new CommitCampaignTacticalRoundDto { ActorId = Human, CommandId = "command.invalid.round",
                    EncounterId = id, ExpectedTurn = view.Turn, ExpectedRound = view.Round - 1,
                    ExpectedBoundaryRevision = view.BoundaryRevision, Replacements = new() };
                Assert.False(app.CommitRound(invalid).Accepted);
                invalid.ExpectedRound = view.Round;
                invalid.Replacements!.Add(new CampaignTacticalFleetOrderDto { FleetId = "fleet.foreign" });
                Assert.False(app.CommitRound(invalid).Accepted);
                Assert.Equal(before, Json(app.ExportPlayableSave()));
                var remaining = app.RemainingPlayableOperations;
                Round(app, id);
                Assert.Equal(remaining - 1, app.RemainingPlayableOperations);
                var accepted = app.ExportPlayableSave().Transcript.Last().Common!.Round!;
                before = Json(app.ExportPlayableSave());
                Assert.False(app.CommitRound(accepted).Accepted);
                Assert.Equal(before, Json(app.ExportPlayableSave()));
                app = Reload(p, app);
            }
        }
        var s = app.CaptureStatus();
        Assert.True(s.CanFinalizeBatch);
        Assert.True(app.RemainingPlayableOperations >= 1);
        Assert.True(app.FinalizeBatch(s.Turn, s.CampaignRevision).Accepted);
        app = Reload(p, app);
        Assert.Equal(SessionPhase.Planning, app.CaptureStatus().Phase);
        Assert.False(app.FinalizeBatch(s.Turn, s.CampaignRevision).Accepted);
    }
    [Fact]
    public void ExactCapacityAdmissionReservesEveryRoundAndFinalizationAcrossReload()
    {
        var p = Package();
        p.Integrated!.Physical!.CorePackage!.Campaign!.Definitions.NavigationConnections!
            .Single(c => c.ConnectionId == "connection.sol-bajor").BaseTraversalCost = 12;
        var boundary = BeforeCapacityEncounter(p);
        p.ReplayOperationLimit = boundary.Pending.Transcript.Count + Continuations(boundary.Pending);
        var app = CampaignPlayableFleetApplication.LoadSave(p, boundary.Before);
        Advance(app);
        Assert.Equal(Continuations(boundary.Pending), app.RemainingPlayableOperations);
        Assert.Equal(Json(boundary.Pending), Json(app.ExportPlayableSave()));
        FinishAtCapacity(p, Reload(p, app));
    }
    [Fact]
    public void OneShortAdmissionRollsBackPaidCompletionMovementAiRngIdsAndHistory()
    {
        var p = Package();
        p.Integrated!.Physical!.CorePackage!.Campaign!.Definitions.NavigationConnections!
            .Single(c => c.ConnectionId == "connection.sol-bajor").BaseTraversalCost = 12;
        // Fixture-only timing: Earth's eight industry completes the paid hull on the third turn,
        // when the original two forces actually meet. The supplied scenario cost stays unchanged.
        p.Integrated.Physical.CorePackage.Campaign.Definitions.ShipDesigns!
            .Single(d => d.DefinitionId == "ship-design.source-cruiser").IndustryCost = 24;
        var boundary = BeforeCapacityEncounter(p);
        Assert.True(boundary.Pending.Integrated!.BuiltShips!.Count > boundary.Before.Integrated!.BuiltShips!.Count, "Fixture turn=" + boundary.Pending.Integrated.Physical!.Core.Campaign.GameState.TurnNumber + " before=" + boundary.Before.Integrated.BuiltShips.Count + " after=" + boundary.Pending.Integrated.BuiltShips.Count);
        var exact = boundary.Pending.Transcript.Count + Continuations(boundary.Pending);
        p.ReplayOperationLimit = exact - 1;
        var app = CampaignPlayableFleetApplication.LoadSave(p, boundary.Before);
        var before = Json(app.ExportPlayableSave()); var status = app.CaptureStatus();
        Assert.Equal(CampaignTacticalError.Overflow, app.AdvanceTurn(status.Turn, status.CampaignRevision).Error);
        Assert.Equal(before, Json(app.ExportPlayableSave())); Assert.Empty(app.GetBattleHistory(Human));
        Assert.Equal(SessionPhase.Planning, Reload(p, app).CaptureStatus().Phase);
        // The old unreviewed candidate could publish this unsafe boundary. It now fails closed.
        Assert.Throws<ArgumentException>(() => CampaignPlayableFleetApplication.LoadSave(p, boundary.Pending));
        p.ReplayOperationLimit = exact;
        app = CampaignPlayableFleetApplication.LoadSave(p, boundary.Before);
        var fleet = app.GetOwnStrategicFleets(Human).First().FleetId.Value;
        Assert.True(app.SubmitFleetOrganization(Organization(app, Array.Empty<string>(),
            CampaignFleetOrganizationKind.Rename, fleet, "Planning consumes a slot")).Accepted);
        before = Json(app.ExportPlayableSave()); status = app.CaptureStatus();
        Assert.Equal(CampaignTacticalError.Overflow, app.AdvanceTurn(status.Turn, status.CampaignRevision).Error);
        Assert.Equal(before, Json(app.ExportPlayableSave()));
        app = CampaignPlayableFleetApplication.LoadSave(p, boundary.Before);
        Advance(app); Assert.Equal(Json(boundary.Pending), Json(app.ExportPlayableSave()));
    }
    static CampaignPlayableFleetPackageDto MultiEncounterPackage(bool aiOnly)
    {
        // Independent admission fixture: a dormant authored AI fights at a second location.
        // No scenario, balance or new gameplay class is added to the delivered package.
        var p = Package(); var physical = p.Integrated!.Physical!; var campaign = physical.CorePackage!.Campaign!;
        var game = campaign.InitialState; var equipment = campaign.Definitions.AuthoredTacticalEquipment!;
        game.Fleets!.Single(f => f.FleetId == "fleet.cardassian-scout").LocationNodeId = "nav.bajor";
        game.Fleets!.Single(f => f.FleetId == "fleet.federation-scout").LocationNodeId = aiOnly ? "nav.sol" : "nav.bajor";
        foreach (var original in game.Ships!.ToList())
        {
            if (aiOnly && original.OwnerCivilizationId == Human) continue;
            var ship = Clone(original); ship.ShipId += ".capacity"; ship.FleetId += ".capacity";
            if (ship.OwnerCivilizationId != Human) ship.OwnerCivilizationId = "civ.dominion";
            var fleet = Clone(game.Fleets!.Single(f => f.FleetId == original.FleetId));
            fleet.FleetId = ship.FleetId; fleet.OwnerCivilizationId = ship.OwnerCivilizationId;
            fleet.LocationNodeId = aiOnly ? "nav.bajor" : "nav.sol"; fleet.ShipIds = new() { ship.ShipId };
            game.Ships!.Add(ship); game.Fleets!.Add(fleet);
            foreach (var pair in new[] { (original.ShipId, ship.ShipId), (original.FleetId, fleet.FleetId) })
            {
                var identity = Clone(game.EntityIdentities.Single(i => i.EntityId == pair.Item1));
                identity.EntityId = pair.Item2; game.EntityIdentities.Add(identity);
            }
            var provision = Clone(equipment.InitialShipProvisions!.Single(s => s.ShipId == original.ShipId));
            provision.ShipId = ship.ShipId; provision.OwnerCivilizationId = ship.OwnerCivilizationId;
            equipment.InitialShipProvisions!.Add(provision);
            physical.InitialOrders!.Add(new CampaignTravelOrderDto { FleetId = fleet.FleetId,
                Intent = CampaignTravelIntent.StayEngage, RouteNodeIds = new() });
        }
        physical.InitialOrders!.ForEach(o => o.Intent = CampaignTravelIntent.StayEngage);
        physical.Hostilities!.Add(new CampaignDeclaredHostilityDto {
            CivilizationA = aiOnly ? "civ.cardassian" : Human, CivilizationB = "civ.dominion" });
        // Contact grids are authored lawful fixture knowledge, including both independent locations.
        physical.InitialContacts!.Clear();
        foreach (var fleet in game.Fleets!)
        foreach (var civ in game.Civilizations!)
            if (physical.Hostilities.Any(h => h.CivilizationA == civ.CivilizationId && h.CivilizationB == fleet.OwnerCivilizationId
                || h.CivilizationB == civ.CivilizationId && h.CivilizationA == fleet.OwnerCivilizationId))
                physical.InitialContacts.Add(new CampaignContactKnowledgeDto {
                    ActorId = civ.CivilizationId, FleetId = fleet.FleetId, LastSeenNodeId = fleet.LocationNodeId, LastSeenTurn = 1,
                    TrackingNodeIds = campaign.Definitions.NavigationRegions!.Select(n => n.NodeId).ToList() });
        return p;
    }
    [Fact]
    public void MultipleEncountersReserveIndependentlyAndFinishInReverseOrder()
    {
        var p = MultiEncounterPackage(false); p.ReplayOperationLimit = 8; // advance + 3+3 rounds + finalization
        var app = CampaignPlayableFleetApplication.StartPackage(p);
        Advance(app); Assert.Equal(2, app.GetEncounters(Human).Count);
        Assert.Equal(7, app.RemainingPlayableOperations);
        FinishAtCapacity(p, Reload(p, app));
        p.ReplayOperationLimit = 7;
        app = CampaignPlayableFleetApplication.StartPackage(p);
        var before = Json(app.ExportPlayableSave()); var s = app.CaptureStatus();
        Assert.Equal(CampaignTacticalError.Overflow, app.AdvanceTurn(s.Turn, s.CampaignRevision).Error);
        Assert.Equal(before, Json(app.ExportPlayableSave()));
    }
    [Fact]
    public void CompletedAiOnlyBatchStillReservesOneFinalizationSlot()
    {
        var p = MultiEncounterPackage(true); p.ReplayOperationLimit = 2;
        var app = CampaignPlayableFleetApplication.StartPackage(p); Advance(app);
        Assert.Empty(app.GetEncounters(Human)); Assert.True(app.CaptureStatus().CanFinalizeBatch);
        Assert.Equal(1, app.RemainingPlayableOperations);
        FinishAtCapacity(p, Reload(p, app));
        p.ReplayOperationLimit = 1; app = CampaignPlayableFleetApplication.StartPackage(p);
        var before = Json(app.ExportPlayableSave()); var s = app.CaptureStatus();
        Assert.Equal(CampaignTacticalError.Overflow, app.AdvanceTurn(s.Turn, s.CampaignRevision).Error);
        Assert.Equal(before, Json(app.ExportPlayableSave()));
    }
    static CampaignPlayableFleetPackageDto ThreeSidePackage(bool eliminate)
    {
        var p = MultiEncounterPackage(true); p.ReplayOperationLimit = 5;
        var physical = p.Integrated!.Physical!; var campaign = physical.CorePackage!.Campaign!;
        var game = campaign.InitialState;
        game.Fleets!.Single(f => f.OwnerCivilizationId == Human).LocationNodeId = "nav.bajor";
        if (eliminate) game.Ships!.Single(s => s.OwnerCivilizationId == Human).CurrentHull = 1;
        var equipment = campaign.Definitions.AuthoredTacticalEquipment!;
        if (eliminate) equipment.InitialShipProvisions!.Single(s => s.OwnerCivilizationId == Human).InitialShields!.ForEach(s => s.Current = 0);
        equipment.Fits!.ForEach(f => { f.EvasionBps = 0; f.Weapons!.ForEach(w => w.AccuracyBps = 10000); });
        physical.Hostilities!.Add(new CampaignDeclaredHostilityDto { CivilizationA = Human, CivilizationB = "civ.dominion" });
        foreach (var pair in new[] { (Human, "fleet.cardassian-scout.capacity"), ("civ.dominion", "fleet.federation-scout") })
            physical.InitialContacts!.Add(new CampaignContactKnowledgeDto {
                ActorId = pair.Item1, FleetId = pair.Item2, LastSeenNodeId = "nav.bajor", LastSeenTurn = 1,
                TrackingNodeIds = campaign.Definitions.NavigationRegions!.Select(n => n.NodeId).ToList() });
        return p;
    }
    [Theory]
    [InlineData(true)][InlineData(false)]
    public void RemainingAiRoundsAutoResolveOnlyAfterEveryHumanFormationIsEliminated(bool eliminate)
    {
        var p = ThreeSidePackage(eliminate);
        var app = CampaignPlayableFleetApplication.StartPackage(p); Advance(app);
        var id = Assert.Single(app.GetEncounters(Human)); Round(app, id);
        var battle = app.ExportPlayableSave().Integrated!.Physical!.Core.Battles.Single();
        Assert.Equal(eliminate, battle.State.Formations.Single(f => f.SideId == Human).Ships.Single().Hull == 0);
        Assert.Equal(eliminate, app.Observe(id, Human).Complete);
        var playback = app.CaptureBattlePlayback(id, Human);
        Assert.Equal(eliminate ? 3 : 1, playback.Rounds.Count);
        var known = playback.Initial.OwnShips.Concat(playback.Initial.VisibleEnemies).Select(s => s.ShipId).ToHashSet();
        Assert.All(playback.Rounds.SelectMany(r => r.Events), e => {
            if (e.SourceShipId != null) Assert.Contains(e.SourceShipId, known);
            if (e.TargetShipId != null) Assert.Contains(e.TargetShipId, known);
        });
        if (eliminate)
            Assert.All(playback.Rounds.Skip(1), r => {
                Assert.All(r.Before.OwnShips, s => Assert.Equal(0, s.Hull));
                Assert.DoesNotContain(r.Events, e => e.Kind == CampaignPlaybackEventKind.OwnOrderCommitted);
                Assert.DoesNotContain(r.Events, e => e.SourceFleetId == "fleet.federation-scout");
            });
        FinishAtCapacity(p, Reload(p, app));
    }
    [Fact]
    public void AiOnlyRemainderRevisionOverflowRollsBackTheWholeHumanCommand()
    {
        var app = CampaignPlayableFleetApplication.StartPackage(ThreeSidePackage(true)); Advance(app);
        var id = Assert.Single(app.GetEncounters(Human)); var view = app.Observe(id, Human);
        // Owned-counter fault injection; this intentionally is not a valid imported/replayed save.
        var owned = (CampaignTacticalReviewSaveDto)typeof(HobbyDS9.Simulation.CampaignTacticalReviewSession)
            .GetField("state", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.GetValue(Backend(app))!;
        owned.Campaign.GameState.StateRevision = long.MaxValue - 1;
        var before = Json(app.ExportPlayableSave());
        var command = new CommitCampaignTacticalRoundDto { ActorId = Human, CommandId = "command.near-overflow",
            EncounterId = id, ExpectedTurn = view.Turn, ExpectedRound = view.Round,
            ExpectedBoundaryRevision = view.BoundaryRevision, Replacements = new() };
        Assert.Equal(CampaignTacticalError.Overflow, app.CommitRound(command).Error);
        Assert.Equal(before, Json(app.ExportPlayableSave()));
        Assert.Empty(app.CaptureBattlePlayback(id, Human).Rounds);
        Assert.Empty(app.GetBattleHistory(Human));
    }
    [Fact]
    public void LateAiRemainderFailureDoesNotPublishHumanRoundOrAiDamage()
    {
        var p = ThreeSidePackage(true);
        var app = CampaignPlayableFleetApplication.StartPackage(p); Advance(app);
        var id = Assert.Single(app.GetEncounters(Human)); var view = app.Observe(id, Human);
        var before = Json(app.ExportPlayableSave());
        var command = new CommitCampaignTacticalRoundDto { ActorId = Human, CommandId = "command.remainder-failure",
            EncounterId = id, ExpectedTurn = view.Turn, ExpectedRound = view.Round,
            ExpectedBoundaryRevision = view.BoundaryRevision, Replacements = new() };
        Backend(app).PlayableAiRemainderProbe = () => throw new InvalidOperationException("Late AI remainder fault.");
        Assert.Equal(CampaignTacticalError.InvalidBatch, app.CommitRound(command).Error);
        Assert.Equal(before, Json(app.ExportPlayableSave())); Assert.Empty(app.CaptureBattlePlayback(id, Human).Rounds);
        Backend(app).PlayableAiRemainderProbe = null;
        Assert.True(app.CommitRound(command).Accepted);
        Assert.True(app.Observe(id, Human).Complete);
        FinishAtCapacity(p, Reload(p, app));
    }
    [Fact]
    public void EliminatedHumanEncounterCannotAdvanceAnotherIndependentHumanEncounter()
    {
        var p = ThreeSidePackage(true); p.ReplayOperationLimit = 8;
        var physical = p.Integrated!.Physical!; var campaign = physical.CorePackage!.Campaign!;
        var game = campaign.InitialState; var equipment = campaign.Definitions.AuthoredTacticalEquipment!;
        foreach (var actor in new[] { Human, "civ.dominion" })
        {
            var original = game.Ships!.Single(s => s.OwnerCivilizationId == actor);
            var ship = Clone(original); ship.ShipId += ".independent"; ship.FleetId += ".independent";
            ship.CurrentHull = campaign.Definitions.ShipDesigns!.Single(d => d.DefinitionId == ship.ShipDesignDefinitionId).MaxHull;
            var fleet = Clone(game.Fleets!.Single(f => f.FleetId == original.FleetId));
            fleet.FleetId = ship.FleetId; fleet.LocationNodeId = "nav.sol"; fleet.ShipIds = new() { ship.ShipId };
            game.Ships!.Add(ship); game.Fleets!.Add(fleet);
            foreach (var pair in new[] { (original.ShipId, ship.ShipId), (original.FleetId, fleet.FleetId) })
            {
                var identity = Clone(game.EntityIdentities.Single(i => i.EntityId == pair.Item1));
                identity.EntityId = pair.Item2; game.EntityIdentities.Add(identity);
            }
            var provision = Clone(equipment.InitialShipProvisions!.Single(s => s.ShipId == original.ShipId));
            provision.ShipId = ship.ShipId; equipment.InitialShipProvisions!.Add(provision);
            physical.InitialOrders!.Add(new CampaignTravelOrderDto { FleetId = fleet.FleetId,
                Intent = CampaignTravelIntent.StayEngage, RouteNodeIds = new() });
            physical.InitialContacts!.Add(new CampaignContactKnowledgeDto {
                ActorId = actor == Human ? "civ.dominion" : Human, FleetId = fleet.FleetId, LastSeenNodeId = "nav.sol",
                LastSeenTurn = 1, TrackingNodeIds = campaign.Definitions.NavigationRegions!.Select(n => n.NodeId).ToList() });
        }
        var app = CampaignPlayableFleetApplication.StartPackage(p); Advance(app);
        var battles = app.ExportPlayableSave().Integrated!.Physical!.Core.Battles;
        Assert.Equal(2, battles.Count);
        var loss = battles.Single(b => b.State.Formations.Count == 3).State.EncounterId;
        var independent = battles.Single(b => b.State.Formations.Count == 2).State.EncounterId;
        var untouched = Json(battles.Single(b => b.State.EncounterId == independent));
        Round(app, loss);
        Assert.True(app.Observe(loss, Human).Complete);
        Assert.False(app.Observe(independent, Human).Complete);
        Assert.Equal(untouched, Json(app.ExportPlayableSave().Integrated!.Physical!.Core.Battles.Single(b => b.State.EncounterId == independent)));
        Assert.Empty(app.CaptureBattlePlayback(independent, Human).Rounds);
        Assert.False(app.CaptureStatus().CanFinalizeBatch);
        FinishAtCapacity(p, Reload(p, app));
    }
}
