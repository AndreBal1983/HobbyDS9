using System.Reflection;
using HobbyDS9.Contracts;
using Xunit;
using static HobbyDS9.Simulation.Tests.ColonisationTransportTests;

namespace HobbyDS9.Simulation.Tests;

// Deliberately detached kernel fixtures, NOT AI-controller journal authority or
// claimed playable AI expeditions. Ordinary public journeys are tested separately.
public sealed class ColonisationKernelTests
{
    const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    static object? Call(CampaignTacticalReviewSession s,string method,params object[] args)=>typeof(CampaignTacticalReviewSession).GetMethod(method,Private)!.Invoke(s,args);
    static T Field<T>(object value,string field)=>(T)value.GetType().GetField(field,Private)!.GetValue(value)!;
    static (CampaignTacticalReviewSession Session,GameStateDto Game,CampaignColonisationStateDto Context,DefinitionCatalogDto Definitions) Fixture()
    {
        var made=Manufacture();var s=made.Session;var kit=Command(s,ColonisationAction.EnqueueKit);kit.EnqueueKit=new(){ColonyId="colony.sol"};Assert.True(s.SubmitColonisation(kit).Accepted);
        for(int i=0;i<32&&s.ExportColonisationSave().Production!.Any(q=>q.OwnerId==Human);i++)Advance(s);
        var game=Field<CampaignTacticalReviewSaveDto>(s,"state").Campaign.GameState;var defs=Field<DefinitionCatalogDto>(s,"definitions");var context=Field<object>(s,"colonisation");var a=Field<CampaignColonisationStateDto>(context,"State");
        // The kernel must also work for non-hostile simultaneous contenders. This
        // fixture changes only the detached test world's diplomatic posture.
        Field<List<CampaignDeclaredHostilityDto>>(s,"declaredHostilities").Clear();
        var human=game.Colonies!.Single(c=>c.ColonyId=="colony.sol");var ai=game.Colonies!.Single(c=>c.OwnerCivilizationId!=Human);var actor=ai.OwnerCivilizationId!;
        var source=game.Blueprints!.Single(b=>b.OwnerCivilizationId==actor&&b.BlueprintId=="blueprint.ready.galaxy-colony-transport.ai"&&b.Revision==1);
        var aiShip=ProductionPhase.Spawn(game,defs,ai,"",new(){BlueprintId=source.BlueprintId,Revision=1},source.CompiledDesign!);
        // Two actually manufactured kits are physically allocated to the two
        // origins; no kit/population/food is duplicated by fixture preparation.
        PopulationRules.AddStock(human.Stockpiles!,"resource.settlement-kit",-1);PopulationRules.AddStock(ai.Stockpiles!,"resource.settlement-kit",1);
        foreach(var pair in new[]{(World:human,Ship:made.Ship),(World:ai,Ship:aiShip.ShipId)})
            Assert.Equal(CampaignTacticalError.None,Call(s,"EmbarkSettlers",game,pair.World.OwnerCivilizationId!,new EmbarkSettlersDto{ShipId=pair.Ship,ColonyId=pair.World.ColonyId,
                CohortId=pair.World.Cohorts!.First(c=>c.Population>=400).CohortId,Population=100,Food=20,Materials=new()}));
        var species=game.Colonies!.SelectMany(c=>c.Cohorts!).Select(c=>c.SpeciesDefinitionId).Distinct().Select(id=>defs.Species!.Single(d=>d.DefinitionId==id)).ToList();
        var target=game.Planets!.First(p=>a.PlanetControls!.Single(c=>c.PlanetId==p.PlanetId).Kind==PlanetControllerKind.Unclaimed&&defs.Planets!.Single(d=>d.DefinitionId==p.DefinitionId) is PlanetDefinitionDto environment
            &&environment.Habitats!.Any(h=>h.NaturalCapacity>=100&&species.All(sp=>WorldEconomy.Suitability(sp,environment,h.Kind)>0)));
        var node=defs.NavigationRegions!.Single(n=>n.AssociatedSystemId==target.SystemId).NodeId;
        foreach(var fleet in game.Fleets!){fleet.RouteNodeIds!.Clear();fleet.TransitTotalCost=0;fleet.TransitProgress=0;fleet.LocationNodeId=node;}
        foreach(var knowledge in game.NavigationKnowledge!){var entry=knowledge.Nodes!.SingleOrDefault(n=>n.Id==node);if(entry==null)knowledge.Nodes!.Add(new(){Id=node,Level=NavigationKnowledgeLevel.Surveyed});else entry.Level=NavigationKnowledgeLevel.Surveyed;}
        foreach(var actorId in new[]{Human,actor})
        {
            var explorer=game.Ships!.First(ship=>ship.OwnerCivilizationId==actorId&&Call(s,"ActivityCapability",game,ship) is object facts&&Field<int>(facts,"StudyWork")>0);
            Assert.Equal(CampaignTacticalError.None,Call(s,"StartPlanetSurvey",game,actorId,new PlanetSurveyOrderDto{ShipId=explorer.ShipId,PlanetId=target.PlanetId}));
        }
        for(int i=0;i<2;i++){game.TurnNumber++;Call(s,"TickPlanetSurveys",game,a);}
        foreach(var manifest in a.ManifestRecords!)Assert.Equal(CampaignTacticalError.None,Call(s,"RequestDeployment",game,manifest.OwnerId,new DeploySettlersDto{ManifestId=manifest.ManifestId,PlanetId=target.PlanetId}));
        var ownership=PlanetaryOwnershipContext.CreateForColonisation(defs,game,a,Field<CampaignActivityStateDto>(Field<object>(s,"activities"),"State").Minors!);
        var validation=FoundationValidator.ValidatePlanetaryGameState(game,defs,ownership);Assert.True(validation.IsValid,Json(validation.Issues));
        Call(s,"ValidateColonisationLineages",game);Call(s,"ValidateColonisationConservation",game);
        return(s,game,a,defs);
    }
    [Fact]public void DetachedValidatedAllActorCensusIsOrderIndependentAndRetainsDisputedCargo()
    {
        var f=Fixture();var left=SimulationSession.CopyState(f.Game);var right=SimulationSession.CopyState(f.Game);var a=ColonisationData.Copy(f.Context);var b=ColonisationData.Copy(f.Context);b.DeploymentIntents!.Reverse();
        var ra=(List<ColonisationDeploymentReceiptDto>)Call(f.Session,"ApplyColonisationWeek",left,a)!;var rb=(List<ColonisationDeploymentReceiptDto>)Call(f.Session,"ApplyColonisationWeek",right,b)!;
        Assert.Equal(2,ra.Count);Assert.All(ra,r=>Assert.Equal(DeploymentBoundaryOutcome.Disputed,r.Outcome));Assert.Equal(Json(ra),Json(rb));Assert.Equal(Json(left),Json(right));
        Assert.Equal(2,a.DeploymentIntents!.Count);Assert.Empty(a.CampSources!);Assert.All(a.ManifestRecords!,m=>Assert.Equal(SettlerManifestStage.Loaded,m.Stage));
        Assert.Equal(a.ManifestRecords!.OrderBy(m=>m.ManifestId).Select(Json),b.ManifestRecords!.OrderBy(m=>m.ManifestId).Select(Json));
    }
    [Fact]public void DetachedCompleteCensusIncludesInterruptedContenderAndExtinctionAccountsFinalDeath()
    {
        var f=Fixture();var human=f.Context.ManifestRecords!.Single(m=>m.OwnerId==Human);human.FoodLost=human.Food;human.Food=0;
        var results=(List<ColonisationDeploymentReceiptDto>)Call(f.Session,"ApplyColonisationWeek",f.Game,f.Context)!;
        Assert.Equal(2,results.Count);Assert.Equal(DeploymentBoundaryOutcome.Interrupted,results.Single(r=>r.Intent!.ActorId==Human).Outcome);
        var deployed=results.Single(r=>r.Intent!.ActorId!=Human);Assert.Equal(DeploymentBoundaryOutcome.Deployed,deployed.Outcome);
        var camp=f.Context.CampSources!.Single();var colony=f.Game.Colonies!.Single(c=>c.ColonyId==camp.ColonyId);
        // Isolate the terminal ecological boundary: one surviving founder after
        // 99 already accounted deaths. This is not an invented replay checkpoint.
        colony.Cohorts!.Single().Population=1;camp.Deaths=99;colony.FoodShortageTurns=PopulationRules.ShortageGrace;
        f.Game.PopulationStatistics!.Deaths+=99;colony.Cohorts!.Single().Population=0;f.Game.PopulationStatistics.Deaths++;
        Call(f.Session,"TickSettlementCamps",f.Game,f.Context);Assert.Equal(100,camp.Deaths);Assert.Equal(ColonyEstablishmentStage.EmptyOutpost,camp.Stage);
        Assert.Equal(SettlementTerminalKind.Extinct,f.Context.CampTerminals!.Single().Kind);Assert.Empty(colony.Cohorts!);
    }
    [Fact]public void DetachedActualEconomyExtractsOnlyFiniteDepositsAndStopsAtZero()
    {
        var s=CampaignTacticalReviewSession.StartColonisation(ColonisationInventoryTests.Package(),PublishedConstructionProfileTests.Settings());
        var game=Field<CampaignTacticalReviewSaveDto>(s,"state").Campaign.GameState;var definitions=Field<DefinitionCatalogDto>(s,"definitions");var context=Field<CampaignColonisationStateDto>(Field<object>(s,"colonisation"),"State");
        var colony=game.Colonies!.Single(c=>c.ColonyId=="colony.sol");var planet=game.Planets!.Single(p=>p.PlanetId==colony.PlanetId);
        foreach(var p in game.Planets!)foreach(var r in p.RemainingDeposits!)r.Amount=0;
        planet.RemainingDeposits=new(){new(){ResourceDefinitionId="resource.dilithium",Amount=3}};
        colony.Infrastructure!.Add(new(){DefinitionId="infrastructure.frontier-mine.v1",Count=1});
        var stock=game.Colonies!.Sum(c=>PopulationRules.Stock(c.Stockpiles!,"resource.dilithium"));var extracted=game.PopulationStatistics!.ResourceExtracted;
        var minors=Field<CampaignActivityStateDto>(Field<object>(s,"activities"),"State").Minors!;
        for(int i=0;i<4;i++){EconomyPhase.Execute(game,definitions,null,PlanetaryOwnershipContext.CreateForColonisation(definitions,game,context,minors));game.TurnNumber++;}
        Assert.Equal(0,planet.RemainingDeposits.Single().Amount);Assert.Equal(3,game.Colonies!.Sum(c=>PopulationRules.Stock(c.Stockpiles!,"resource.dilithium"))-stock);
        Assert.Equal(3,game.PopulationStatistics.ResourceExtracted-extracted);
        var emptyStock=game.Colonies!.Sum(c=>PopulationRules.Stock(c.Stockpiles!,"resource.dilithium"));EconomyPhase.Execute(game,definitions,null,PlanetaryOwnershipContext.CreateForColonisation(definitions,game,context,minors));
        Assert.Equal(emptyStock,game.Colonies!.Sum(c=>PopulationRules.Stock(c.Stockpiles!,"resource.dilithium")));
    }
}
