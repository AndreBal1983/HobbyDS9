using HobbyDS9.Contracts;
using Xunit;
using static HobbyDS9.Simulation.Tests.ColonisationTransportTests;

namespace HobbyDS9.Simulation.Tests;

public sealed class ColonisationExpeditionTests
{
    static void Move(CampaignTacticalReviewSession s,string ship,string node)
    {
        var own=s.CaptureActivities(Human).Ships.Single(x=>x.ShipId==ship);
        if(own.NodeId==node&&own.Stationary)return;
        var route=s.FindKnownTravelRoute(Human,own.NodeId,node);Assert.True(route.Success);
        CampaignActivitiesTests.Travel(s,own.FleetId,route.Steps.ToList());
        for(int i=0;i<16&&!s.CaptureActivities(Human).Ships.Single(x=>x.ShipId==ship).Stationary;i++)Advance(s);
        own=s.CaptureActivities(Human).Ships.Single(x=>x.ShipId==ship);Assert.True(own.Stationary);Assert.Equal(node,own.NodeId);
    }
    static ColonisationPlanetObservationDto Survey(CampaignTacticalReviewSession s,string ship,string planet,bool cancel)
    {
        void Start(){var c=Command(s,ColonisationAction.StartPlanetSurvey);c.StartSurvey=new(){ShipId=ship,PlanetId=planet};Assert.True(s.SubmitColonisation(c).Accepted,s.LastPhysicalFailure);}
        Start();
        if(cancel){var active=s.CaptureColonisationPlanets(Human).Single(p=>p.PlanetId==planet);var c=Command(s,ColonisationAction.CancelPlanetSurvey);c.CancelSurvey=new(){SurveyId=active.ActiveSurveyId!};
            Assert.True(s.SubmitColonisation(c).Accepted,s.LastPhysicalFailure);Assert.False(s.CaptureColonisationPlanets(Human).Single(p=>p.PlanetId==planet).Surveyed);Start();}
        Advance(s);Advance(s);return s.CaptureColonisationPlanets(Human).Single(p=>p.PlanetId==planet);
    }
    [Fact] public void RealObservedExpeditionLandsEstablishesAndReplaysWithoutSystemOwnership()
    {
        var x=Manufacture();var s=x.Session;var explorer=s.CaptureActivities(Human).Ships.First(p=>p.StudyWork>0).ShipId;
        var species=x.Package.Activities!.BaseTemplate!.Integrated!.Physical!.CorePackage!.Campaign!.Definitions.Species!.Single(p=>p.DefinitionId==Game(s.ExportColonisationSave()).Colonies!.Single(c=>c.ColonyId=="colony.sol").Cohorts!.First().SpeciesDefinitionId);
        var visited=new HashSet<string>();ColonisationPlanetObservationDto? destination=null;string? node=null;
        for(int step=0;step<20&&destination==null;step++)
        {
            var own=s.CaptureActivities(Human).Ships.Single(p=>p.ShipId==explorer);visited.Add(own.NodeId);
            foreach(var planet in s.CaptureColonisationPlanets(Human).Where(p=>!p.Surveyed&&p.SystemId==s.CaptureGeneratedMap(Human).Regions.Single(r=>r.NodeId==own.NodeId).SystemId))
            {
                var surveyed=Survey(s,explorer,planet.PlanetId,step==0);
                if(surveyed.KnownControl?.Kind==PlanetControllerKind.Unclaimed&&surveyed.Environment!.Deposits!.Any(d=>d.Amount>0)&&surveyed.Environment.Habitats!.Any(h=>h.NaturalCapacity>=100&&WorldEconomy.Suitability(species,surveyed.Environment,h.Kind)>0))
                {destination=surveyed;node=own.NodeId;break;}
            }
            if(destination!=null)break;
            var map=s.CaptureGeneratedMap(Human);var next=map.Regions.Where(r=>!visited.Contains(r.NodeId)).Select(r=>(r.NodeId,Path:CampaignActivitiesTests.KnownPath(map,own.NodeId,r.NodeId)))
                .Where(r=>r.Path.Count>0).OrderBy(r=>r.Path.Count).ThenBy(r=>r.NodeId,StringComparer.Ordinal).First();Move(s,explorer,next.NodeId);
        }
        Assert.NotNull(destination);Assert.NotNull(node);
        var before=s.ExportColonisationSave();var home=Game(before).Colonies!.Single(c=>c.ColonyId=="colony.sol");
        Move(s,x.Ship,s.CaptureGeneratedMap(Human).Regions.Single(r=>r.SystemId==home.SystemId).NodeId);
        var embark=Command(s,ColonisationAction.EmbarkSettlers);embark.Embark=new(){ShipId=x.Ship,ColonyId=home.ColonyId,CohortId=home.Cohorts!.First(c=>c.Population>=400).CohortId,Population=100,Food=20,Materials=new()};
        var embarkResult=s.SubmitColonisation(embark);Assert.True(embarkResult.Accepted,embarkResult.Error+" "+s.LastPhysicalFailure+" "+Json(home)+" "+Json(s.CaptureActivities(Human).Ships));Move(s,x.Ship,node!);
        var transit=s.ExportColonisationSave();Assert.Equal(Json(transit),Json(CampaignTacticalReviewSession.LoadColonisation(x.Package,transit).ExportColonisationSave()));
        var manifest=transit.Colonisation!.ManifestRecords!.Single(m=>m.Stage==SettlerManifestStage.Loaded);
        var deploy=Command(s,ColonisationAction.RequestDeployment);deploy.Deploy=new(){ManifestId=manifest.ManifestId,PlanetId=destination!.PlanetId};
        var deployed=s.SubmitColonisation(deploy);Assert.True(deployed.Accepted,deployed.Error+" "+s.LastPhysicalFailure+" "+Json(destination)+" "+Json(manifest));Advance(s);
        var landed=s.ExportColonisationSave();var camp=Assert.Single(landed.Colonisation!.CampSources!);Assert.Equal(ColonyEstablishmentStage.Founding,camp.Stage);
        var landing=Assert.Single(s.CaptureColonisation(Human).LatestDeployments);Assert.Equal(DeploymentBoundaryOutcome.Deployed,landing.Outcome);Assert.Equal(manifest.ManifestId,landing.ManifestId);Assert.False(landing.StillAboard);
        Assert.Equal(Json(landed),Json(s.ExportColonisationSave()));
        Assert.Equal(destination.PlanetId,Game(landed).Colonies!.Single(c=>c.ColonyId==camp.ColonyId).PlanetId);Assert.All(Game(landed).StarSystems!,s=>Assert.Null(s.OwnerCivilizationId));
        var reopened=CampaignTacticalReviewSession.LoadColonisation(x.Package,landed);Assert.Equal(Json(landed),Json(reopened.ExportColonisationSave()));Assert.Equal(Json(s.CaptureColonisation(Human).LatestDeployments),Json(reopened.CaptureColonisation(Human).LatestDeployments));
        foreach(var removeIntent in new[]{true,false})
        {
            var wrong=System.Text.Json.JsonSerializer.Deserialize<CampaignColonisationSaveDto>(Json(landed))!;
            if(removeIntent)wrong.LastSettlementProof!.BeforeColonisation!.DeploymentIntents!.Clear();
            else wrong.MovementOwnership!.ColonyStages!.Add(new(){ColonyId=camp.ColonyId,Stage=ColonyEstablishmentStage.Founding});
            Assert.Throws<ArgumentException>(()=>CampaignTacticalReviewSession.LoadColonisation(x.Package,wrong));
            Assert.Equal(Json(landed),Json(s.ExportColonisationSave()));
        }
        // A separate official replay branch proves return-only evacuation and reuse;
        // the original branch below continues establishment without pretending both histories occurred.
        EvacuateAndRecommission(x.Package,landed,x.Ship,home.ColonyId,node!);
        for(int i=0;i<12&&s.ExportColonisationSave().Colonisation!.CampSources!.Single().Stage==ColonyEstablishmentStage.Founding;i++)Advance(s);
        var final=s.ExportColonisationSave();Assert.Equal(ColonyEstablishmentStage.Established,final.Colonisation!.CampSources!.Single().Stage);
        Assert.Equal(Json(final),Json(CampaignTacticalReviewSession.LoadColonisation(x.Package,final).ExportColonisationSave()));
        var frontier=s.CaptureColonisation(Human).Worlds.Single(w=>w.Id==camp.ColonyId);
        Assert.False(frontier.Shipbuilding);Assert.Equal("Requires a shipyard at this world",frontier.ConstructionUnavailableReason(true));
        Assert.Null(frontier.ConstructionUnavailableReason(false));Assert.True(s.CaptureColonisation(Human).Worlds.Single(w=>w.Id==home.ColonyId).Shipbuilding);
        var shipOption=s.CaptureActivities(Human).BuildOptions.First(o=>o.Kind==ActivityProductionKind.PublishedShip&&o.Supported);
        var shipStamp=s.CaptureStatus();var denied=s.SubmitActivity(new(){ActorId=Human,CommandId="command.frontier.no-yard",ExpectedTurn=shipStamp.Turn,ExpectedRevision=shipStamp.CampaignRevision,
            Kind=CampaignActivityCommandKind.EnqueueShip,TargetId=camp.ColonyId,DefinitionId=shipOption.DefinitionId,BlueprintRevision=shipOption.BlueprintRevision});
        Assert.False(denied.Accepted);Assert.Equal(CampaignTacticalError.UnsupportedSource,denied.Error);Assert.Equal(Json(final),Json(s.ExportColonisationSave()));
        var frontierKit=Command(s,ColonisationAction.EnqueueKit);frontierKit.EnqueueKit=new(){ColonyId=camp.ColonyId};
        Assert.True(s.EvaluateColonisation(frontierKit).Available);Assert.Equal(Json(final),Json(s.ExportColonisationSave()));
        Move(s,x.Ship,s.CaptureGeneratedMap(Human).Regions.Single(r=>r.SystemId==home.SystemId).NodeId);
        var supplies=Command(s,ColonisationAction.LoadCarriedSupplies);supplies.LoadSupplies=new(){ShipId=x.Ship,SourceColonyId=home.ColonyId,Food=5,Materials=new()};
        Assert.True(s.SubmitColonisation(supplies).Accepted,s.LastPhysicalFailure);Move(s,x.Ship,node!);
        var supply=s.ExportColonisationSave().Colonisation!.ManifestRecords!.Single(m=>m.Stage==SettlerManifestStage.Loaded);
        Assert.Equal(SettlerManifestKind.SupplyOnly,supply.Kind);Assert.Null(supply.Passengers);
        var deliver=Command(s,ColonisationAction.DeliverCarriedSupplies);deliver.Deliver=new(){ManifestId=supply.ManifestId,ColonyId=camp.ColonyId,Food=5,Materials=new()};
        Assert.True(s.SubmitColonisation(deliver).Accepted,s.LastPhysicalFailure);
        var beforeMine=s.ExportColonisationSave();var treasury=Game(beforeMine).Civilizations!.Single(c=>c.CivilizationId==Human).Credits;var status=s.CaptureStatus();
        Assert.True(s.SubmitActivity(new(){ActorId=Human,CommandId="command.actual.mine",ExpectedTurn=status.Turn,ExpectedRevision=status.CampaignRevision,
            Kind=CampaignActivityCommandKind.EnqueueStructure,TargetId=camp.ColonyId,DefinitionId="infrastructure.frontier-mine.v1"}).Accepted,s.LastPhysicalFailure);
        Assert.Equal(treasury,Game(s.ExportColonisationSave()).Civilizations!.Single(c=>c.CivilizationId==Human).Credits);
        for(int i=0;i<64&&!s.ExportColonisationSave().Activities!.Structures!.Any(r=>r.ColonyId==camp.ColonyId);i++)Advance(s);
        var mined=s.ExportColonisationSave();Assert.Contains(mined.Activities!.Structures!,r=>r.ColonyId==camp.ColonyId&&r.DefinitionId=="infrastructure.frontier-mine.v1");
        var deposit=Game(mined).Planets!.Single(p=>p.PlanetId==destination.PlanetId).RemainingDeposits!.Sum(r=>r.Amount);var local=Game(mined).Colonies!.Single(c=>c.ColonyId==camp.ColonyId).Stockpiles!.Sum(r=>r.Amount);
        Advance(s);var extracted=s.ExportColonisationSave();var remaining=Game(extracted).Planets!.Single(p=>p.PlanetId==destination.PlanetId).RemainingDeposits!.Sum(r=>r.Amount);
        var stock=Game(extracted).Colonies!.Single(c=>c.ColonyId==camp.ColonyId).Stockpiles!.Sum(r=>r.Amount);Assert.InRange(deposit-remaining,1,3);Assert.Equal(deposit-remaining,stock-local);
        Assert.Equal(Json(extracted),Json(CampaignTacticalReviewSession.LoadColonisation(x.Package,extracted).ExportColonisationSave()));
    }
    static void EvacuateAndRecommission(CampaignColonisationPackageDto package,CampaignColonisationSaveDto landed,string ship,string home,string targetNode)
    {
        var s=CampaignTacticalReviewSession.LoadColonisation(package,landed);var camp=landed.Colonisation!.CampSources!.Single();
        var evacuate=Command(s,ColonisationAction.EvacuateColony);evacuate.Evacuate=new(){CampId=camp.CampId,ShipId=ship};Assert.True(s.SubmitColonisation(evacuate).Accepted,s.LastPhysicalFailure);
        var empty=s.ExportColonisationSave();Assert.Equal(ColonyEstablishmentStage.EmptyOutpost,empty.Colonisation!.CampSources!.Single().Stage);
        var returning=empty.Colonisation.ManifestRecords!.Single(m=>m.Stage==SettlerManifestStage.Loaded);Assert.Equal(SettlerManifestKind.ReturnOnly,returning.Kind);
        var bad=Command(s,ColonisationAction.UnloadTransport);bad.Unload=new(){ManifestId=returning.ManifestId,ColonyId=camp.ColonyId};var unchanged=Json(empty);
        Assert.False(s.SubmitColonisation(bad).Accepted);Assert.Equal(unchanged,Json(s.ExportColonisationSave()));
        var system=Game(empty).Colonies!.Single(c=>c.ColonyId==home).SystemId;Move(s,ship,s.CaptureGeneratedMap(Human).Regions.Single(r=>r.SystemId==system).NodeId);
        var unload=Command(s,ColonisationAction.UnloadTransport);unload.Unload=new(){ManifestId=returning.ManifestId,ColonyId=home};Assert.True(s.SubmitColonisation(unload).Accepted,s.LastPhysicalFailure);
        var kit=Command(s,ColonisationAction.EnqueueKit);kit.EnqueueKit=new(){ColonyId=home};Assert.True(s.SubmitColonisation(kit).Accepted,s.LastPhysicalFailure);
        for(int i=0;i<32&&s.ExportColonisationSave().Production!.Any(q=>q.OwnerId==Human);i++)Advance(s);
        var world=Game(s.ExportColonisationSave()).Colonies!.Single(c=>c.ColonyId==home);var embark=Command(s,ColonisationAction.EmbarkSettlers);
        embark.Embark=new(){ShipId=ship,ColonyId=home,CohortId=world.Cohorts!.First(c=>c.Population>=400).CohortId,Population=100,Food=20,Materials=new()};Assert.True(s.SubmitColonisation(embark).Accepted,s.LastPhysicalFailure);
        Move(s,ship,targetNode);var m=s.ExportColonisationSave().Colonisation!.ManifestRecords!.Single(m=>m.Stage==SettlerManifestStage.Loaded);
        var reject=Command(s,ColonisationAction.UnloadTransport);reject.Unload=new(){ManifestId=m.ManifestId,ColonyId=camp.ColonyId};var beforeReject=Json(s.ExportColonisationSave());
        Assert.False(s.SubmitColonisation(reject).Accepted);Assert.Equal(beforeReject,Json(s.ExportColonisationSave()));
        var deploy=Command(s,ColonisationAction.RequestDeployment);deploy.Deploy=new(){ManifestId=m.ManifestId,PlanetId=camp.ColonyId==""?"":Game(landed).Colonies!.Single(c=>c.ColonyId==camp.ColonyId).PlanetId};
        Assert.True(s.SubmitColonisation(deploy).Accepted,s.LastPhysicalFailure);Advance(s);var restored=s.ExportColonisationSave();
        Assert.Single(restored.Colonisation!.ColonyOrigins!);Assert.Equal(2,restored.Colonisation.CampSources!.Count);Assert.Single(Game(restored).Colonies!,c=>c.ColonyId==camp.ColonyId);
        Assert.Equal(Json(restored),Json(CampaignTacticalReviewSession.LoadColonisation(package,restored).ExportColonisationSave()));
    }
}
