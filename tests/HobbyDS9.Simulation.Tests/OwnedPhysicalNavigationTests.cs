using System.Reflection;
using System.Text.Json;
using HobbyDS9.Contracts;
using HobbyDS9.Simulation;
using Xunit;

namespace HobbyDS9.Simulation.Tests;

public sealed class OwnedPhysicalNavigationTests
{
    const string Actor="civ.federation";
    static T Field<T>(CampaignTacticalReviewSession s,string name)=>(T)typeof(CampaignTacticalReviewSession).GetField(name,BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(s)!;
    static string Json(object value)=>JsonSerializer.Serialize(value,new JsonSerializerOptions{IncludeFields=true});
    [Fact] public void GraphDetachesPublicInputAndInternalEdgeCannotMutateRetainedTopology()
    {
        var p=CampaignPhysicalTravelTests.Package();var definitions=p.CorePackage!.Campaign!.Definitions;
        var graph=new WeightedNavigationGraph(definitions);var original=graph.Edge("nav.sol","nav.bajor")!;
        var cost=original.BaseTraversalCost;original.BaseTraversalCost=987;original.EndpointA="nav.forged";
        definitions.NavigationConnections!.Single(e=>e.ConnectionId==original.ConnectionId).BaseTraversalCost=0;
        definitions.NavigationRegions!.Single(n=>n.NodeId=="nav.sol").DisplayName="Changed input";
        Assert.Equal(cost,graph.Edge("nav.sol","nav.bajor")!.BaseTraversalCost);
        Assert.DoesNotContain(graph.Regions,n=>n.DisplayName=="Changed input");
        Assert.Throws<ArgumentException>(()=>new WeightedNavigationGraph(definitions));
        Assert.Throws<NotSupportedException>(()=>((IList<NavigationConnectionSnapshot>)graph.Connections).Clear());
    }
    [Fact] public void ReusedTopologyReadsCurrentKnowledgeAndTraversalEffectsEveryTime()
    {
        var p=CampaignPhysicalTravelTests.Package();var definitions=p.CorePackage!.Campaign!.Definitions;var game=p.CorePackage.Campaign.InitialState;
        var graph=new WeightedNavigationGraph(definitions);
        var route=graph.FindRoute(game,Actor,"nav.sol","nav.bajor");Assert.True(route.Success);
        var initial=graph.Cost(game,"nav.sol","nav.bajor").EffectiveCost;
        var fleet=game.Fleets!.Single(f=>f.FleetId=="fleet.federation-scout");var budget=graph.Budget(game,fleet,definitions,out _);
        game.TraversalEffects!.Add(new(){EffectId="effect.owned-test",NodeId="nav.sol",StartTurn=game.TurnNumber,EndTurn=game.TurnNumber,TraversalCostIncrease=7,MovementBudgetPenalty=1});
        Assert.Equal(initial+7,graph.Cost(game,"nav.sol","nav.bajor").EffectiveCost);
        Assert.Equal(Math.Max(1,budget-1),graph.Budget(game,fleet,definitions,out _));
        game.TurnNumber++;Assert.Equal(initial,graph.Cost(game,"nav.sol","nav.bajor").EffectiveCost);
        Assert.Equal(budget,graph.Budget(game,fleet,definitions,out _));
        var knowledge=game.NavigationKnowledge!.Single(k=>k.CivilizationId==Actor);
        var known=knowledge.Nodes!.Single(n=>n.Id=="nav.bajor");known.Level=NavigationKnowledgeLevel.Unknown;
        Assert.False(graph.FindRoute(game,Actor,"nav.sol","nav.bajor").Success);
        known.Level=NavigationKnowledgeLevel.Surveyed;Assert.True(graph.FindRoute(game,Actor,"nav.sol","nav.bajor").Success);
    }
    [Fact] public void ExplicitMovementTopologyRejectsForeignOwnerBeforeMutationAndDefaultStillValidates()
    {
        var p=CampaignPhysicalTravelTests.Package();var definitions=p.CorePackage!.Campaign!.Definitions;var game=p.CorePackage.Campaign.InitialState;
        var foreign=SimulationSession.CopyDefinitions(definitions);var graph=new WeightedNavigationGraph(foreign);var before=Json(game);
        Assert.Throws<ArgumentException>(()=>CampaignPhysicalMovement.Execute(game,definitions,p.InitialOrders!,p.InitialContacts!,p.Hostilities!,p.ContactFreshnessTurns!.Value,graph));
        Assert.Equal(before,Json(game));
        var own=new WeightedNavigationGraph(definitions);var left=SimulationSession.CopyState(game);var right=SimulationSession.CopyState(game);
        var a=CampaignPhysicalMovement.Execute(left,definitions,p.InitialOrders!,p.InitialContacts!,p.Hostilities!,p.ContactFreshnessTurns!.Value);
        var b=CampaignPhysicalMovement.Execute(right,definitions,p.InitialOrders!,p.InitialContacts!,p.Hostilities!,p.ContactFreshnessTurns!.Value,own);
        Assert.Equal(Json(left),Json(right));Assert.Equal(Json(a),Json(b));
        definitions.NavigationConnections![0].BaseTraversalCost=0;
        Assert.Throws<ArgumentException>(()=>CampaignPhysicalMovement.Execute(game,definitions,p.InitialOrders!,p.InitialContacts!,p.Hostilities!,p.ContactFreshnessTurns!.Value));
        Assert.Equal(before,Json(game));
    }
    [Fact] public void PhysicalSessionsOwnTopologyAndPublishedForksShareOnlyFixedDefinitions()
    {
        var p=CampaignTacticalReviewSession.CreateStreamedActivitiesPackage(CampaignActivitiesTests.Package());
        var source=CampaignTacticalReviewSession.StartStreamedActivities(p,PublishedConstructionProfileTests.Settings());
        var topology=Field<WeightedNavigationGraph>(source,"physicalNavigation");var definitions=Field<DefinitionCatalogDto>(source,"definitions");
        topology.RequireOwner(definitions);
        var fork=(CampaignTacticalReviewSession)typeof(CampaignTacticalReviewSession).GetMethod("ForkPublished",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(source,null)!;
        Assert.Same(topology,Field<WeightedNavigationGraph>(fork,"physicalNavigation"));Assert.Same(definitions,Field<DefinitionCatalogDto>(fork,"definitions"));
        var other=CampaignTacticalReviewSession.StartStreamedActivities(p,PublishedConstructionProfileTests.Settings());
        Assert.NotSame(topology,Field<WeightedNavigationGraph>(other,"physicalNavigation"));
        Assert.Throws<ArgumentException>(()=>topology.RequireOwner(Field<DefinitionCatalogDto>(other,"definitions")));
        var before=Json(source.ExportStreamedActivitiesSave());
        p.ContentPackage!.BaseTemplate!.Integrated!.Physical!.CorePackage!.Campaign!.Definitions.NavigationConnections![0].BaseTraversalCost=0;
        var stamp=fork.CaptureStatus();var result=fork.SubmitActivity(new(){ActorId=Actor,CommandId="command.owned-fork",ExpectedTurn=stamp.Turn,ExpectedRevision=stamp.CampaignRevision,Kind=CampaignActivityCommandKind.EnqueueStructure,TargetId="colony.sol",DefinitionId="infrastructure.fabrication-annex.v1"});
        Assert.True(result.Accepted,fork.LastPhysicalFailure);Assert.Equal(before,Json(source.ExportStreamedActivitiesSave()));
        Assert.NotEqual(before,Json(fork.ExportStreamedActivitiesSave()));
    }
    [Fact] public void NonPhysicalTacticalSessionDoesNotConstructNavigationTopology()
    {
        var session=CampaignTacticalReviewSession.Start(CampaignTacticalReviewTests.Package());
        Assert.Null(Field<WeightedNavigationGraph?>(session,"physicalNavigation"));
        Assert.NotEmpty(session.ExportSave().Pools);
    }
}

