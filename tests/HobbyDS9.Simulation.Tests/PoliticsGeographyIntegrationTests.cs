using System.Text.Json;
using HobbyDS9.Application;
using HobbyDS9.Contracts;
using HobbyDS9.Simulation;
using HobbyDS9.UnityClient;
using Xunit;
namespace HobbyDS9.Simulation.Tests;
public sealed class PoliticsGeographyIntegrationTests
{
    private static ScenarioDto Scenario()=>VerticalSliceFixture.Scenario();
    private static ColonyStateDto Earth(ScenarioDto s)=>s.InitialState.Colonies!.Single(c=>c.ColonyId=="colony.sol");
    private static string Json(object o)=>JsonSerializer.Serialize(o);
    [Theory]
    [InlineData(ResistanceState.Quiet,10000,10000)]
    [InlineData(ResistanceState.Tense,10000,10000)]
    [InlineData(ResistanceState.Unrest,9000,8000)]
    [InlineData(ResistanceState.OrganizedResistance,7500,6500)]
    public void ResistanceHasExplicitLocalOutputAndFreightPenalties(ResistanceState resistance,int industry,int logistics)
    {var s=Scenario();var c=Earth(s);c.Politics!.Resistance=resistance;var neutral=WorldEconomy.Project(c,s.InitialState,s.Definitions);
     Assert.Equal(industry,neutral.Politics.IndustryFactor);Assert.Equal(logistics,neutral.Politics.LogisticsFactor);
     c.Politics.Resistance=ResistanceState.Quiet;var quiet=WorldEconomy.Project(c,s.InitialState,s.Definitions);
     Assert.Equal(PopulationRules.ScaleBy(quiet.Industry,industry),neutral.Industry);Assert.Equal(PopulationRules.ScaleBy(quiet.LogisticsCapacity,logistics),neutral.LogisticsCapacity);}
    [Fact] public void ActiveDisruptionReducesExtractionWithoutConsumingRngOrCreatingResources()
    {var s=Scenario();var c=Earth(s);c.Politics!.Resistance=ResistanceState.OrganizedResistance;c.Politics.DisruptionRemainingTurns=2;
     var before=WorldEconomy.Project(c,s.InitialState,s.Definitions);Assert.Equal(3750,before.Politics.ExtractionFactor);Assert.Equal(2600,before.Politics.LogisticsFactor);
     var planet=s.InitialState.Planets!.Single(p=>p.PlanetId==c.PlanetId);var deposit=PopulationRules.Stock(planet.RemainingDeposits!,"resource.dilithium");var stock=PopulationFixture.Dilithium(c);var rng=s.InitialState.RngState.State;
     EconomyPhase.Execute(s.InitialState,s.Definitions);var removed=deposit-PopulationRules.Stock(planet.RemainingDeposits!,"resource.dilithium");
     Assert.Equal(1,removed);Assert.True(PopulationFixture.Dilithium(c)<=stock+removed);Assert.Equal(rng,s.InitialState.RngState.State);}
    [Fact] public void PoliciesHaveEconomicTradeoffsAndAuthoritativeRefugeeAcceptance()
    {var s=Scenario();var c=Earth(s);var normal=WorldEconomy.Project(c,s.InitialState,s.Definitions);c.Politics!.GovernancePolicyDefinitionId="governance.extraction";var extraction=WorldEconomy.Project(c,s.InitialState,s.Definitions);
     Assert.True(extraction.CreditOutput>=normal.CreditOutput);Assert.Equal(11000,extraction.Politics.ExtractionFactor);Assert.False(extraction.Politics.AcceptsRefugees);
     c.Politics.GovernancePolicyDefinitionId="governance.autonomy";var autonomy=WorldEconomy.Project(c,s.InitialState,s.Definitions);Assert.True(autonomy.CreditOutput<normal.CreditOutput);Assert.True(autonomy.Politics.AcceptsRefugees);}
    [Fact] public void AiPolicySelectionUsesStampedSharedHandlerAndNeverAutomatesHuman()
    {var s=Scenario();var ai=s.InitialState.Civilizations!.Single(c=>c.CivilizationId=="civ.cardassian");ai.ControllerKind=ControllerKind.AI;
     s.InitialState.Colonies!.Single(c=>c.OwnerCivilizationId==ai.CivilizationId).Politics!.Pressure=6500;Earth(s).Politics!.GovernancePolicyDefinitionId="governance.extraction";
     var session=VerticalSliceFixture.Session(s);var result=VerticalSliceFixture.Advance(session);var policy=Assert.Single(result.CommandResults,r=>r.CommandId!.Contains(".k5."));Assert.True(policy.Accepted);
     Assert.Equal("governance.security",session.ExportSave().GameState.Colonies!.Single(c=>c.ColonyId=="colony.cardassia").Politics!.GovernancePolicyDefinitionId);
     Assert.Equal("governance.extraction",session.ExportSave().GameState.Colonies!.Single(c=>c.ColonyId=="colony.sol").Politics!.GovernancePolicyDefinitionId);
     Assert.Empty(session.CaptureSnapshot().AcceptedCommandIds);}
    [Fact] public void PolicyViewModelTranslationRetainsStaleStampAndIsDetachedAcrossReload()
    {var s=Scenario();var client=new StrategicClient(CampaignApplication.StartScenario(s),save=>CampaignApplication.LoadSave(s,save));var before=client.Snapshot;var options=StrategicOptions.Governance(before,new("civ.federation"),new("colony.sol"));
     Assert.True(options.Single(o=>o.Policy.DefinitionId=="governance.autonomy").CanSelect);Assert.False(options.Single(o=>o.Policy.DefinitionId=="governance.integration").CanSelect);
     Assert.True(client.Governance(before,new("civ.federation"),new("colony.sol"),new("governance.autonomy")).Accepted);var accepted=Json(client.ExportSave());
     Assert.Equal(CommandErrorCode.StaleRevision,client.Governance(before,new("civ.federation"),new("colony.sol"),new("governance.security")).ErrorCode);Assert.Equal(accepted,Json(client.ExportSave()));
     Assert.Equal("governance.integration",before.Campaign.Colonies.Single(c=>c.ColonyId.Value=="colony.sol").State.Politics.GovernancePolicyDefinitionId);
     Assert.Throws<NotSupportedException>(()=>((IList<GovernanceOption>)options).Clear());}
    [Fact] public void AuthoredScenarioUsesIndependentIdsAndEmptyHazardAndWeightedEta()
    {var s=VerticalSliceFixture.Scenario(false,weighted:true);var app=CampaignApplication.StartScenario(s);var view=app.CaptureSnapshot();
     Assert.Contains(view.Navigation.Nodes,n=>n.NodeId.Value=="nav.badlands"&&!n.AssociatedSystemId.HasValue);Assert.Contains(view.Navigation.Nodes,n=>n.NodeId.Value=="nav.sol"&&n.AssociatedSystemId!.Value.Value=="system.sol");
     var route=app.FindRoute(new("nav.sol"),new("nav.gamma-exit"));Assert.Equal(9,route.TraversalCost);Assert.Equal(3,app.PreviewFleetTravel(new("fleet.federation-scout"),route.Steps)!.EstimatedTurns);
     Assert.Contains(view.Navigation.Links,e=>e.PhenomenonCost==2);Assert.All(view.FleetTravel,t=>Assert.Equal(3,t.MovementBudget));}
    [Fact] public void LogisticsUsesWeightedPhysicalRouteNotFleetMovementBudget()
    {var s=Scenario();s.InitialState.Fleets!.Clear();var d=s.Definitions;var from=d.NavigationRegions!.Single(n=>n.AssociatedSystemId=="system.sol");var to=d.NavigationRegions!.Single(n=>n.AssociatedSystemId=="system.bajor");
     d.NavigationRegions!.Add(new(){NodeId="nav.freight",DisplayName="Freight junction",PhenomenonDefinitionIds=new()});d.NavigationConnections!.Clear();
     d.NavigationConnections.Add(new(){ConnectionId="edge.expensive",EndpointA=from.NodeId,EndpointB=to.NodeId,BaseTraversalCost=9,Kind=GalaxyEdgeKind.Normal});
     d.NavigationConnections.Add(new(){ConnectionId="edge.freight-a",EndpointA=from.NodeId,EndpointB="nav.freight",BaseTraversalCost=2,Kind=GalaxyEdgeKind.Normal});
     d.NavigationConnections.Add(new(){ConnectionId="edge.freight-b",EndpointA="nav.freight",EndpointB=to.NodeId,BaseTraversalCost=2,Kind=GalaxyEdgeKind.Normal});
     NavigationFixture.State(s.InitialState,d);var graph=new WeightedNavigationGraph(d);var route=PopulationLogistics.Path(s.InitialState,graph,"system.sol","system.bajor","civ.federation");
     Assert.Equal(new[]{from.NodeId,"nav.freight",to.NodeId},route.path);Assert.Equal(4,route.cost);
     d.ShipDesigns![0].MovementBudget=1;var same=PopulationLogistics.Path(s.InitialState,graph,"system.sol","system.bajor","civ.federation");Assert.Equal(route.path,same.path);Assert.Equal(route.cost,same.cost);Assert.Equal(route.integrity,same.integrity);}
}
