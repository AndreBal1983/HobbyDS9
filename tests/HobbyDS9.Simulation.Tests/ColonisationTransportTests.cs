using System.Text.Json;
using HobbyDS9.Contracts;
using Xunit;

namespace HobbyDS9.Simulation.Tests;

public sealed class ColonisationTransportTests
{
    internal const string Human="civ.federation";
    internal static string Json(object x)=>JsonSerializer.Serialize(x);
    internal static GameStateDto Game(CampaignColonisationSaveDto root)=>root.Campaign!.Integrated!.Physical!.Core.Campaign.GameState;
    internal static void Advance(CampaignTacticalReviewSession s)
    {var f=s.CaptureStatus();var r=s.AdvanceTurn(f.Turn,f.CampaignRevision);Assert.True(r.Accepted,r.Error+" "+s.LastPhysicalFailure);}
    internal static ColonisationCommandDto Command(CampaignTacticalReviewSession s,ColonisationAction kind)
    {var f=s.CaptureStatus();return new(){ActorId=Human,CommandId="command.colony."+f.CampaignRevision,Turn=f.Turn,Revision=f.CampaignRevision,Action=kind};}
    internal static (CampaignColonisationPackageDto Package,CampaignTacticalReviewSession Session,string Ship) Manufacture()
    {
        var p=ColonisationInventoryTests.Package();var s=CampaignTacticalReviewSession.StartColonisation(p,PublishedConstructionProfileTests.Settings(250));
        var kit=Command(s,ColonisationAction.EnqueueKit);kit.EnqueueKit=new(){ColonyId="colony.sol"};Assert.True(s.SubmitColonisation(kit).Accepted,s.LastPhysicalFailure);
        var f=s.CaptureStatus();Assert.True(s.SubmitActivity(new(){ActorId=Human,CommandId="command.transport.actual",ExpectedTurn=f.Turn,ExpectedRevision=f.CampaignRevision,
            Kind=CampaignActivityCommandKind.EnqueueShip,TargetId="colony.sol",DefinitionId="blueprint.ready.galaxy-colony-transport",BlueprintRevision=1}).Accepted,s.LastPhysicalFailure);
        for(int i=0;i<48&&!s.ExportColonisationSave().PublishedBuiltShips!.Any(b=>b.OwnerCivilizationId==Human);i++)Advance(s);
        var root=s.ExportColonisationSave();Assert.True(root.PublishedBuiltShips!.Any(b=>b.OwnerCivilizationId==Human),Json(root.Production!));var built=Assert.Single(root.PublishedBuiltShips!,b=>b.OwnerCivilizationId==Human);
        Assert.Equal("blueprint.ready.galaxy-colony-transport",built.BlueprintId);return(p,s,built.ShipId!);
    }
    [Fact] public void ActualManufacturedTransportSplitsAndReturnsPopulationAndReloadsOnlyPhysicalSupplies()
    {
        var x=Manufacture();var s=x.Session;var before=s.ExportColonisationSave();var home=Game(before).Colonies!.Single(c=>c.ColonyId=="colony.sol");
        var cohort=home.Cohorts!.First(c=>c.Population>=400);var command=Command(s,ColonisationAction.EmbarkSettlers);
        command.Embark=new(){ShipId=x.Ship,ColonyId=home.ColonyId,CohortId=cohort.CohortId,Population=100,Food=20,Materials=new(){new(){ResourceDefinitionId="resource.dilithium",Amount=2}}};
        Assert.True(s.SubmitColonisation(command).Accepted,s.LastPhysicalFailure);var loaded=s.ExportColonisationSave();var manifest=Assert.Single(loaded.Colonisation!.ManifestRecords!);
        Assert.Equal(100,manifest.Passengers!.Population);Assert.Equal(home.Cohorts!.Sum(c=>c.Population)-100,Game(loaded).Colonies!.Single(c=>c.ColonyId==home.ColonyId).Cohorts!.Sum(c=>c.Population));
        Assert.Equal(home.Food-20,Game(loaded).Colonies!.Single(c=>c.ColonyId==home.ColonyId).Food);
        Assert.Equal(Json(loaded),Json(CampaignTacticalReviewSession.LoadColonisation(x.Package,loaded).ExportColonisationSave()));
        var unload=Command(s,ColonisationAction.UnloadTransport);unload.Unload=new(){ManifestId=manifest.ManifestId,ColonyId=home.ColonyId};Assert.True(s.SubmitColonisation(unload).Accepted,s.LastPhysicalFailure);
        var returned=s.ExportColonisationSave();Assert.Equal(home.Cohorts!.Sum(c=>c.Population),Game(returned).Colonies!.Single(c=>c.ColonyId==home.ColonyId).Cohorts!.Sum(c=>c.Population));
        Assert.Equal(home.Food,Game(returned).Colonies!.Single(c=>c.ColonyId==home.ColonyId).Food);Assert.Equal(Json(home.Stockpiles!),Json(Game(returned).Colonies!.Single(c=>c.ColonyId==home.ColonyId).Stockpiles!));
        var supply=Command(s,ColonisationAction.LoadCarriedSupplies);supply.LoadSupplies=new(){ShipId=x.Ship,SourceColonyId=home.ColonyId,Food=5,Materials=new(){new(){ResourceDefinitionId="resource.dilithium",Amount=3}}};
        Assert.True(s.SubmitColonisation(supply).Accepted,s.LastPhysicalFailure);var carrying=s.ExportColonisationSave();var supplies=Assert.Single(carrying.Colonisation!.ManifestRecords!,m=>m.Kind==SettlerManifestKind.SupplyOnly);
        Assert.Null(supplies.Passengers);var origin=carrying.Colonisation.ManifestOrigins!.Single(o=>o.ManifestId==supplies.ManifestId);Assert.Equal(ManifestOriginKind.Supply,origin.Kind);Assert.Null(origin.SourceCohortId);Assert.Equal(0,origin.EmbarkedPopulation);
        var deliver=Command(s,ColonisationAction.DeliverCarriedSupplies);deliver.Deliver=new(){ManifestId=supplies.ManifestId,ColonyId=home.ColonyId,Food=5,Materials=new(){new(){ResourceDefinitionId="resource.dilithium",Amount=3}}};
        Assert.True(s.SubmitColonisation(deliver).Accepted,s.LastPhysicalFailure);var final=s.ExportColonisationSave();Assert.Equal(SettlerManifestStage.Delivered,final.Colonisation!.ManifestRecords!.Single(m=>m.ManifestId==supplies.ManifestId).Stage);
        Assert.Equal(Json(final),Json(CampaignTacticalReviewSession.LoadColonisation(x.Package,final).ExportColonisationSave()));
    }
}
