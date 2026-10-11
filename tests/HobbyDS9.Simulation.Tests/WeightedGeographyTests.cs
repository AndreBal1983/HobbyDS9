using System.Text.Json;
using HobbyDS9.Application;
using HobbyDS9.Contracts;
using HobbyDS9.Simulation;
using Xunit;
namespace HobbyDS9.Simulation.Tests;
public sealed class WeightedGeographyTests
{
    private static ScenarioDto Scenario(long budget=3)
    {
        var s=VerticalSliceFixture.Scenario(); var d=s.Definitions;
        d.NavigationRegions!.Add(new(){NodeId="nav.empty",DisplayName="Empty transit",PhenomenonDefinitionIds=new()});
        d.NavigationRegions.Add(new(){NodeId="nav.isolated",DisplayName="Isolated",PhenomenonDefinitionIds=new()});
        d.NavigationConnections!.Clear();
        void Edge(string id,string a,string b,long cost,GalaxyEdgeKind kind=GalaxyEdgeKind.Normal)=>d.NavigationConnections.Add(new(){ConnectionId=id,EndpointA=a,EndpointB=b,BaseTraversalCost=cost,Kind=kind});
        Edge("edge.direct","system.sol","system.bajor",12);
        Edge("edge.first","system.sol","nav.empty",2);
        Edge("edge.second","nav.empty","system.bajor",2);
        Edge("edge.wormhole","system.bajor","system.gamma-exit",1,GalaxyEdgeKind.Wormhole);
        foreach(var design in d.ShipDesigns!)design.MovementBudget=budget;
        foreach(var k in s.InitialState.NavigationKnowledge!)
        { k.Nodes=d.NavigationRegions.Select(n=>new NavigationKnowledgeEntryDto{Id=n.NodeId,Level=NavigationKnowledgeLevel.Surveyed}).ToList();
          k.Connections=d.NavigationConnections.Select(e=>new NavigationKnowledgeEntryDto{Id=e.ConnectionId,Level=NavigationKnowledgeLevel.Surveyed}).ToList(); }
        return s;
    }
    private static SimulationSession Session(ScenarioDto s)=>VerticalSliceFixture.Session(s);
    private static CommandResult Route(SimulationSession s,params string[] route)
    { var c=VerticalSliceFixture.Command(s,CommandKind.SetFleetRoute,"cmd.route");c.SetFleetRoute=new(){FleetId="fleet.federation-scout",RouteNodeIds=route.ToList()};return s.SubmitCommand(c); }
    private static FleetSnapshot Fleet(SimulationSession s)=>s.CaptureSnapshot().Fleets.Single(f=>f.FleetId.Value=="fleet.federation-scout");
    [Fact] public void WeightedRouteUsesEmptyTransitAndWormholeAndIsDetached()
    { var s=Session(Scenario());var before=VerticalSliceFixture.Json(s);var r=s.FindNavigationRoute("civ.federation","system.sol","system.gamma-exit");
      Assert.Equal(new[]{"nav.empty","system.bajor","system.gamma-exit"},r.Steps);Assert.Equal(5,r.TotalCost);Assert.Equal(GalaxyEdgeKind.Wormhole,r.Costs.Last().Kind);
      Assert.Throws<NotSupportedException>(()=>((IList<string>)r.Steps).Clear());Assert.Equal(before,VerticalSliceFixture.Json(s));Assert.Null(s.Navigation.SystemForNode("nav.empty"));
      Assert.Equal(RouteErrorCode.Unreachable,s.FindNavigationRoute("civ.federation","system.sol","nav.isolated").ErrorCode); }
    [Fact] public void EqualCostTiesIgnoreInputAndEndpointOrdering()
    { var a=Scenario();a.Definitions.NavigationConnections!.Single(e=>e.ConnectionId=="edge.direct").BaseTraversalCost=4;
      var r=Session(a).FindNavigationRoute("civ.federation","system.sol","system.bajor");a.Definitions.NavigationConnections!.Reverse();a.Definitions.NavigationRegions!.Reverse();
      foreach(var e in a.Definitions.NavigationConnections!)(e.EndpointA,e.EndpointB)=(e.EndpointB,e.EndpointA);
      Assert.Equal(JsonSerializer.Serialize(r),JsonSerializer.Serialize(Session(a).FindNavigationRoute("civ.federation","system.sol","system.bajor"))); }
    [Fact] public void MultiEdgeMovementAndPartialProgressRoundTripAndEtaAgree()
    { var content=Scenario();var a=Session(content);Assert.True(Route(a,"nav.empty","system.bajor","system.gamma-exit").Accepted);
      Assert.Equal(2,a.CaptureFleetTravel("fleet.federation-scout").EstimatedTurns);VerticalSliceFixture.Advance(a);
      Assert.Equal("nav.empty",Fleet(a).LocationNodeId.Value);Assert.Equal(1,Fleet(a).TransitProgress);Assert.Equal(2,Fleet(a).TransitTotalCost);
      Assert.Equal(1,a.CaptureFleetTravel("fleet.federation-scout").EstimatedTurns);var b=new SimulationSession(JsonSerializer.Deserialize<SaveEnvelopeDto>(VerticalSliceFixture.Json(a))!,content.Definitions,VerticalSliceFixture.Compatibility(content));
      Assert.Equal(VerticalSliceFixture.Json(a),VerticalSliceFixture.Json(b));VerticalSliceFixture.Advance(a);VerticalSliceFixture.Advance(b);
      Assert.Equal("system.gamma-exit",Fleet(a).LocationNodeId.Value);Assert.Equal(VerticalSliceFixture.Json(a),VerticalSliceFixture.Json(b));Assert.Empty(Fleet(a).RouteNodeIds); }
    [Fact] public void SlowestShipControlsBudgetAndCannotCancelPaidProgress()
    { var content=Scenario(5);content.Definitions.ShipDesigns!.Single(d=>d.DefinitionId=="ship-design.scout").MovementBudget=2;var s=Session(content);
      Assert.Equal(2,s.CaptureFleetTravel("fleet.federation-scout").MovementBudget);Assert.True(Route(s,"system.bajor").Accepted);VerticalSliceFixture.Advance(s);
      Assert.Equal(2,Fleet(s).TransitProgress);var before=VerticalSliceFixture.Json(s);var c=VerticalSliceFixture.Command(s,CommandKind.SetFleetRoute,"cmd.cancel-route");c.SetFleetRoute=new(){FleetId="fleet.federation-scout",RouteNodeIds=new()};
      Assert.Equal(CommandErrorCode.FleetInTransit,s.SubmitCommand(c).ErrorCode);Assert.Equal(before,VerticalSliceFixture.Json(s)); }
    [Fact] public void UnknownShortcutDoesNotLeakToPreviewSubmissionOrMap()
    { var content=Scenario();var knowledge=content.InitialState.NavigationKnowledge!.Single(k=>k.CivilizationId=="civ.federation");knowledge.Connections!.Single(k=>k.Id=="edge.first").Level=NavigationKnowledgeLevel.Unknown;
      var s=Session(content);Assert.Equal(12,s.FindNavigationRoute("civ.federation","system.sol","system.bajor").TotalCost);var before=VerticalSliceFixture.Json(s);
      Assert.Equal(CommandErrorCode.InvalidRoute,Route(s,"nav.empty").ErrorCode);Assert.Equal(before,VerticalSliceFixture.Json(s));var app=CampaignApplication.StartScenario(content);
      Assert.DoesNotContain(app.CaptureSnapshot().Navigation.Links,e=>e.EndpointA.Value=="system.sol"&&e.EndpointB.Value=="nav.empty"); }
    [Fact] public void ScattererRaisesDistanceAndInhibitorLowersBudgetIndependently()
    { var content=Scenario(4);content.InitialState.TraversalEffects!.Add(new(){EffectId="effect.scatter",ConnectionId="edge.first",TraversalCostIncrease=5,StartTurn=1,EndTurn=9});
      content.InitialState.TraversalEffects.Add(new(){EffectId="effect.inhibit",NodeId="system.sol",MovementBudgetPenalty=2,StartTurn=1,EndTurn=9});var s=Session(content);
      Assert.Equal(9,s.FindNavigationRoute("civ.federation","system.sol","system.bajor").TotalCost);Assert.Equal(2,s.CaptureFleetTravel("fleet.federation-scout").MovementBudget); }
    [Fact] public void OpposingStationaryFleetStopsMultiEdgeTraversalAtEmptyRegion()
    { var content=Scenario(9);content.InitialState.Fleets!.Single(f=>f.FleetId=="fleet.cardassian-scout").LocationNodeId="nav.empty";var s=Session(content);Assert.True(Route(s,"nav.empty","system.bajor").Accepted);
      var p=s.CaptureSnapshot();var result=s.AdvanceTurn(p.TurnNumber,p.StateRevision);Assert.Equal(TurnStatus.AwaitingCombat,result.Status);Assert.Equal("nav.empty",Fleet(s).LocationNodeId.Value);
      Assert.Equal("nav.empty",Assert.Single(s.GetPendingCombatRequests()).LocationNodeId);Assert.Equal(new[]{"system.bajor"},Fleet(s).RouteNodeIds.Select(n=>n.Value)); }
    [Theory]
    [InlineData("zero-cost")][InlineData("duplicate-edge")][InlineData("missing-endpoint")][InlineData("bad-progress")][InlineData("unknown-knowledge")][InlineData("duplicate-knowledge")][InlineData("effect-overflow")][InlineData("zero-budget")]
    public void InvalidNavigationStateAndContentAreRejected(string bad)
    { var s=Scenario();var d=s.Definitions;var state=s.InitialState;
      switch(bad){case "zero-cost":d.NavigationConnections![0].BaseTraversalCost=0;break;case "duplicate-edge":d.NavigationConnections!.Add(d.NavigationConnections[0]);break;
      case "missing-endpoint":d.NavigationConnections![0].EndpointB="nav.missing";break;case "bad-progress":state.Fleets![0].TransitProgress=2;break;
      case "unknown-knowledge":state.NavigationKnowledge![0].Nodes![0].Id="nav.unknown";break;case "duplicate-knowledge":state.NavigationKnowledge!.Add(state.NavigationKnowledge[0]);break;
      case "effect-overflow":state.TraversalEffects!.Add(new(){EffectId="effect.overflow",ConnectionId="edge.first",TraversalCostIncrease=long.MaxValue,StartTurn=1,EndTurn=1});break;
      case "zero-budget":d.ShipDesigns![0].MovementBudget=0;break;}
      Assert.False(FoundationValidator.ValidateScenario(s).IsValid); }
    [Fact] public void MovementFailureRollsBackCostProgressRngAndLedger()
    { var content=Scenario();var s=VerticalSliceFixture.Session(content,(phase,_)=>phase!=StrategicTurnPhase.Movement);Assert.True(Route(s,"nav.empty","system.bajor").Accepted);var before=VerticalSliceFixture.Json(s);var p=s.CaptureSnapshot();
      Assert.Equal(TurnStatus.Rejected,s.AdvanceTurn(p.TurnNumber,p.StateRevision).Status);Assert.Equal(before,VerticalSliceFixture.Json(s)); }
    [Fact] public void LockedEdgeCostSurvivesScheduledScattererExpiryAndReload()
    {var content=Scenario(3);content.InitialState.TraversalEffects!.Add(new(){EffectId="effect.short",ConnectionId="edge.direct",TraversalCostIncrease=3,StartTurn=1,EndTurn=1});var s=Session(content);
     Assert.True(Route(s,"system.bajor").Accepted);Assert.Equal(5,s.CaptureFleetTravel("fleet.federation-scout").EstimatedTurns);VerticalSliceFixture.Advance(s);
     Assert.Equal(15,Fleet(s).TransitTotalCost);Assert.Equal(3,Fleet(s).TransitProgress);Assert.Equal(4,s.CaptureFleetTravel("fleet.federation-scout").EstimatedTurns);
     s=new SimulationSession(s.ExportSave(),content.Definitions,VerticalSliceFixture.Compatibility(content));
     for(var i=0;i<4;i++)VerticalSliceFixture.Advance(s);Assert.Equal("system.bajor",Fleet(s).LocationNodeId.Value);}
    [Fact] public void EtaProjectsScheduledInhibitorExpiryUsingSameBudgetsAsMovement()
    {var content=Scenario(4);content.InitialState.TraversalEffects!.Add(new(){EffectId="effect.short",NodeId="system.sol",MovementBudgetPenalty=3,StartTurn=1,EndTurn=2});var s=Session(content);
     Assert.True(Route(s,"system.bajor").Accepted);Assert.Equal(5,s.CaptureFleetTravel("fleet.federation-scout").EstimatedTurns);
     for(var i=0;i<4;i++){VerticalSliceFixture.Advance(s);Assert.NotEqual("system.bajor",Fleet(s).LocationNodeId.Value);}VerticalSliceFixture.Advance(s);Assert.Equal("system.bajor",Fleet(s).LocationNodeId.Value);}
    [Fact] public void LongRouteOverflowIsStableAndRejectedWithoutMutation()
    {var content=Scenario(1);content.Definitions.NavigationConnections!.Single(e=>e.ConnectionId=="edge.first").BaseTraversalCost=long.MaxValue;
     content.Definitions.NavigationConnections!.Single(e=>e.ConnectionId=="edge.direct").BaseTraversalCost=long.MaxValue;
     var s=Session(content);Assert.Equal(RouteErrorCode.CostOverflow,s.FindNavigationRoute("civ.federation","system.sol","system.gamma-exit").ErrorCode);
     var before=VerticalSliceFixture.Json(s);Assert.Equal(CommandErrorCode.InvalidRoute,Route(s,"nav.empty","system.bajor").ErrorCode);Assert.Equal(before,VerticalSliceFixture.Json(s));}
    [Fact] public void FullTurnBudgetCanTraverseThreeCheapEdgesWhenUncontested()
    {var content=Scenario(5);var s=Session(content);Assert.True(Route(s,"nav.empty","system.bajor","system.gamma-exit").Accepted);
     VerticalSliceFixture.Advance(s);Assert.Equal("system.gamma-exit",Fleet(s).LocationNodeId.Value);Assert.Equal(3,s.CaptureSnapshot().NavigationStatistics.EdgesCompleted);Assert.Equal(5,s.CaptureSnapshot().NavigationStatistics.TraversalCostPaid);}
    [Fact] public void SlowShipAddedToFleetSetsBudgetAndLimitingId()
    {var content=Scenario(5);var design=content.Definitions.ShipDesigns!.Last();design.MovementBudget=2;var state=content.InitialState;var fleet=state.Fleets!.Single(f=>f.FleetId=="fleet.federation-scout");
     fleet.ShipIds!.Add("ship.slow");state.Ships!.Add(new(){ShipId="ship.slow",ShipDesignDefinitionId=design.DefinitionId,FleetId=fleet.FleetId,OwnerCivilizationId=fleet.OwnerCivilizationId,CurrentHull=design.MaxHull});state.EntityIdentities.Add(new(){EntityId="ship.slow",DefinitionId=design.DefinitionId});
     var travel=Session(content).CaptureFleetTravel(fleet.FleetId);Assert.Equal(2,travel.MovementBudget);Assert.Equal("ship.slow",travel.LimitingShipId);}

}
