using System.Text.Json;
using HobbyDS9.Contracts;
using HobbyDS9.Simulation;
using Xunit;
using static HobbyDS9.Simulation.Tests.VerticalSliceFixture;

namespace HobbyDS9.Simulation.Tests;

public sealed class FleetMovementTests
{
    internal static CommandResult Route(SimulationSession s, string id, params string[] route)
    { var c = Command(s, CommandKind.SetFleetRoute, id); c.SetFleetRoute = new() { FleetId = "fleet.federation-scout", RouteNodeIds = route.ToList() }; return s.SubmitCommand(c); }
    [Fact]
    public void ScenarioStartsWithOneScoutPerFactionAndNoQueuesOrRoutes()
    {
        var s = Session(Scenario(false)); var snapshot = s.CaptureSnapshot(); Assert.Equal(3, snapshot.Fleets.Count); Assert.Equal(3, snapshot.Ships.Count);
        Assert.All(snapshot.Fleets, f => { Assert.Single(f.ShipIds); Assert.Empty(f.RouteNodeIds); Assert.Equal(f.OwnerCivilizationId, snapshot.Ships.Single(ship => ship.ShipId == f.ShipIds[0]).OwnerCivilizationId); });
        Assert.All(snapshot.Colonies, c => Assert.Empty(c.ProductionQueue)); Assert.All(snapshot.Ships, ship => Assert.Equal(10, ship.CurrentHull));
    }
    [Fact]
    public void ExplicitRoutesMoveOneEdgeIncludingWormholeAndEmptyClearsMovement()
    {
        var s = Session(); var old = s.CaptureSnapshot(); Assert.True(Route(s, "cmd.route", "system.bajor", "system.bajoran-wormhole", "system.gamma-exit").Accepted);
        var p = s.CaptureSnapshot(); Assert.Equal(1, p.StateRevision); Assert.Equal(47UL, p.RngState.State); Assert.Equal(1UL, p.NextEntitySequence);
        Assert.Empty(old.Fleets.Single(f => f.FleetId.Value == "fleet.federation-scout").RouteNodeIds);
        foreach (var location in new[] { "system.bajor", "system.bajoran-wormhole", "system.gamma-exit" })
        {
            Assert.Equal(TurnStatus.Completed, Advance(s).Status); Assert.Equal(location, s.CaptureSnapshot().Fleets.Single(f => f.FleetId.Value == "fleet.federation-scout").LocationNodeId.Value);
        }
        Assert.True(Route(s, "cmd.more", "system.bajoran-wormhole").Accepted); Assert.True(Route(s, "cmd.clear").Accepted);
        Advance(s); Assert.Equal("system.gamma-exit", s.CaptureSnapshot().Fleets.Single(f => f.FleetId.Value == "fleet.federation-scout").LocationNodeId.Value);
    }
    [Theory]
    [InlineData("unknown", CommandErrorCode.UnknownFleet)]
    [InlineData("foreign", CommandErrorCode.FleetNotOwned)]
    [InlineData("nonadjacent", CommandErrorCode.InvalidRoute)]
    [InlineData("unknown-node", CommandErrorCode.InvalidRoute)]
    [InlineData("self", CommandErrorCode.InvalidRoute)]
    public void InvalidRouteCommandsRejectWithoutAnyMutation(string error, CommandErrorCode expected)
    {
        var s = Session(); var c = Command(s, CommandKind.SetFleetRoute, "cmd.bad"); c.SetFleetRoute = new()
        {
            FleetId = error == "unknown" ? "fleet.absent" : error == "foreign" ? "fleet.cardassian-scout" : "fleet.federation-scout",
            RouteNodeIds = new() { error == "unknown-node" ? "system.absent" : error == "self" ? "system.sol" : "system.gamma-exit" }
        };
        var before = Json(s); Assert.Equal(expected, s.SubmitCommand(c).ErrorCode); Assert.Equal(before, Json(s));
    }
    [Fact]
    public void CompletionJoinsLowestStationaryFleetButNeverRoutedFleet()
    {
        var scenario = Scenario(); PopulationFixture.RemoveMars(scenario); var state = scenario.InitialState;
        state.Fleets!.Add(new() { FleetId = "fleet.a", OwnerCivilizationId = "civ.federation", LocationNodeId = "system.sol", ShipIds = new() { "ship.a" }, RouteNodeIds = new() });
        state.Ships!.Add(new() { ShipId = "ship.a", ShipDesignDefinitionId = "ship-design.scout", OwnerCivilizationId = "civ.federation", FleetId = "fleet.a", CurrentHull = 10 });
        state.EntityIdentities.Add(new() { EntityId = "ship.a", DefinitionId = "ship-design.scout" }); state.EntityIdentities.Add(new() { EntityId = "fleet.a" });
        state.Fleets.Reverse(); var s = Session(scenario); Assert.True(Enqueue(s).Accepted); Advance(s); Advance(s); Advance(s);
        Assert.Equal("fleet.a", s.CaptureSnapshot().Ships.Single(ship => ship.ShipId.Value == "ship.spawn.0000000000000002").FleetId.Value);
        // Both old fleets depart on the completion turn, so Production must create a third.
        var save = s.ExportSave(); var sol = save.GameState.Colonies!.Single(c => c.ColonyId == "colony.sol"); PopulationFixture.SetDilithium(sol, 100);
        foreach (var f in save.GameState.Fleets!.Where(f => f.OwnerCivilizationId == "civ.federation")) f.RouteNodeIds = new() { "system.bajor" };
        s = new(save, scenario.Definitions, Compatibility(scenario)); Assert.True(Enqueue(s, "cmd.again").Accepted);
        save = s.ExportSave(); save.GameState.Colonies!.Single(c => c.ColonyId == "colony.sol").ProductionQueue![0].CompletedIndustry = 19;
        s = new(save, scenario.Definitions, Compatibility(scenario)); Advance(s);
        var ship = s.CaptureSnapshot().Ships.Single(x => x.ShipId.Value == "ship.spawn.0000000000000004"); Assert.Equal("fleet.spawn.0000000000000005", ship.FleetId.Value);
        var newFleet = s.CaptureSnapshot().Fleets.Single(f => f.FleetId == ship.FleetId); Assert.Equal("system.sol", newFleet.LocationNodeId.Value); Assert.Empty(newFleet.RouteNodeIds); Assert.Equal(6UL, s.CaptureSnapshot().NextEntitySequence);
    }
    [Fact]
    public void RouteAndMembershipSnapshotsAndCommandInputsAreDetachedAndReorderedInputContinuesIdentically()
    {
        var scenario = Scenario(); var s = Session(scenario); var c = Command(s, CommandKind.SetFleetRoute, "cmd.route");
        c.SetFleetRoute = new() { FleetId = "fleet.federation-scout", RouteNodeIds = new() { "system.bajor", "system.bajoran-wormhole", "system.gamma-exit" } };
        Assert.True(s.SubmitCommand(c).Accepted); c.SetFleetRoute.RouteNodeIds.Clear(); Assert.True(Enqueue(s).Accepted); Advance(s);
        var mid = s.ExportSave(); mid.GameState.Ships!.Reverse(); mid.GameState.Fleets!.Reverse(); mid.GameState.EntityIdentities.Reverse(); mid.GameState.Colonies!.Reverse();
        scenario.Definitions.ShipDesigns!.Reverse(); scenario.Definitions.GalaxyEdges!.Reverse();
        var resumed = new SimulationSession(JsonSerializer.Deserialize<SaveEnvelopeDto>(JsonSerializer.Serialize(mid))!, scenario.Definitions, Compatibility(scenario));
        var old = s.CaptureSnapshot(); var f = old.Fleets.Single(x => x.FleetId.Value == "fleet.federation-scout");
        Assert.Throws<NotSupportedException>(() => ((IList<EntityId>)f.RouteNodeIds).Clear()); Assert.Throws<NotSupportedException>(() => ((IList<EntityId>)f.ShipIds).Clear());
        var q = old.Colonies.Single(x => x.ColonyId.Value == "colony.sol"); Assert.Throws<NotSupportedException>(() => ((IList<ProductionQueueItemSnapshot>)q.ProductionQueue).Clear());
        Assert.Throws<NotSupportedException>(() => ((IList<DefinitionId>)old.ShipDesigns[0].RequiredEngineeringApplicationDefinitionIds).Clear());
        var before = Json(s); mid.GameState.Fleets![0].RouteNodeIds!.Clear(); mid.GameState.Ships![0].CurrentHull = 1; Assert.Equal(before, Json(s));
        for (var i = 0; i < 5; i++) { Assert.Equal(JsonSerializer.Serialize(Advance(s)), JsonSerializer.Serialize(Advance(resumed))); Assert.Equal(Json(s), Json(resumed)); }
        Assert.Equal("system.bajor", f.LocationNodeId.Value); Assert.Equal(8, q.ProductionQueue[0].CompletedIndustry);
    }
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    public void InstrumentationCannotChangeFleetRouteOrMembershipInAnyPhase(int target)
    {
        var s = Session(probe: (phase, state) => { if ((int)phase == target) state.Fleets![0].RouteNodeIds!.Add("system.bajor"); return true; });
        var before = Json(s); Assert.Throws<InvalidOperationException>(() => Advance(s)); Assert.Equal(before, Json(s));
    }
    [Fact]
    public void OpposingFleetsAtTheSameLocationGenerateOneFrozenCampaignEncounter()
    {
        var s = Session(); Assert.True(Route(s, "cmd.route", "system.bajor").Accepted);
        var c = Command(s, CommandKind.SetFleetRoute, "cmd.other", "civ.cardassian"); c.SetFleetRoute = new() { FleetId = "fleet.cardassian-scout", RouteNodeIds = new() { "system.bajor" } }; Assert.True(s.SubmitCommand(c).Accepted);
        var p = s.CaptureSnapshot(); var result = s.AdvanceTurn(p.TurnNumber, p.StateRevision); Assert.Equal(SessionPhase.AwaitingCombat, result.Phase); Assert.Empty(result.Events);
        Assert.Single(s.GetPendingCombatRequests());
        Assert.Equal(2, s.CaptureSnapshot().Fleets.Count(f => f.LocationNodeId.Value == "system.bajor"));
    }
}
