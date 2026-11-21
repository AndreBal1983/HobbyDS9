using System.Text.Json;
using HobbyDS9.Contracts;
using Xunit;
using static HobbyDS9.Simulation.Tests.ColonisationTransportTests;
namespace HobbyDS9.Simulation.Tests;
public sealed class ColonisationGrowthTests
{
    readonly Xunit.Abstractions.ITestOutputHelper output;
    public ColonisationGrowthTests(Xunit.Abstractions.ITestOutputHelper output){this.output=output;}
    internal static CampaignColonisationExpansionPackageDto Package()=>CampaignTacticalReviewSession.CreateGrowthPackage(ColonisationExpansionTests.Package());
    internal static CampaignTacticalReviewSession Fresh(int count=30)=>CampaignTacticalReviewSession.StartGrowthGame(Package(),PublishedConstructionProfileTests.Settings(count));
    static string Json(object o)=>JsonSerializer.Serialize(o);
    [Fact]public void ExplicitSourceInitialReplayForkIsolationAndOldAdmissionRemainClosed()
    {
        var p=Package();var s=CampaignTacticalReviewSession.StartGrowthGame(p,PublishedConstructionProfileTests.Settings(30));var root=s.ExportGrowthSave();var before=Json(root);
        Assert.Equal(12,root.FormatVersion);Assert.Equal(CampaignColonisationGrowthIdentity.Profile,root.ProfileId);
        Assert.Equal(before,Json(CampaignTacticalReviewSession.LoadGrowthSave(p,root).ExportGrowthSave()));
        Assert.Throws<ArgumentException>(()=>s.ExportColonisationExpansionSave());Assert.Throws<ArgumentException>(()=>s.ExportColonisationSave());
        Assert.Throws<ArgumentException>(()=>CampaignTacticalReviewSession.LoadColonisationExpansion(ColonisationExpansionTests.Package(),root));
        Assert.Throws<ArgumentException>(()=>CampaignTacticalReviewSession.StartColonisationExpansion(p,PublishedConstructionProfileTests.Settings(30)));
        Assert.Throws<ArgumentException>(()=>CampaignTacticalReviewSession.LoadGrowthSave(ColonisationExpansionTests.Package(),root));
        var original=CampaignTacticalReviewSession.StartColonisationExpansion(ColonisationExpansionTests.Package(),PublishedConstructionProfileTests.Settings(30)).ExportColonisationExpansionSave();
        Assert.Throws<ArgumentException>(()=>CampaignTacticalReviewSession.LoadGrowthSave(p,original));
        var app=HobbyDS9.Application.CampaignColonisationGrowthApplication.StartGrowthGame(p,PublishedConstructionProfileTests.Settings(30));var stamp=app.CaptureStatus();
        var fork=app.ForkOperations(stamp.Turn,stamp.CampaignRevision);Assert.IsAssignableFrom<HobbyDS9.Application.ICampaignColonisationGrowthSession>(fork);
        Assert.True(fork.AdvanceTurn(stamp.Turn,stamp.CampaignRevision).Accepted);Assert.Equal(before,Json(app.ExportGrowthSave()));
        p.PlannerPolicy!.MaximumAttempts=7;p.BaseRosterV2!.StartingRoster!.ClassId="class.changed";
        Assert.Equal(before,Json(s.ExportGrowthSave()));root.AiPlans![0].Stage=ColonisationAiStage.GrowthClosed;
        Assert.Throws<ArgumentException>(()=>CampaignTacticalReviewSession.LoadGrowthSave(Package(),root));
    }
    [Fact]public void RealTransportLoadsWithoutKitReturnsMergedCohortImmigratesAndReusesForSupplyWithEntireReplay()
    {
        var p=Package();var s=CampaignTacticalReviewSession.StartGrowthGame(p,PublishedConstructionProfileTests.Settings(250));var stamp=s.CaptureStatus();
        Assert.True(s.SubmitActivity(new(){ActorId=Human,CommandId="command.growth.transport",ExpectedTurn=stamp.Turn,ExpectedRevision=stamp.CampaignRevision,Kind=CampaignActivityCommandKind.EnqueueShip,TargetId="colony.mars",DefinitionId="blueprint.ready.galaxy-colony-transport",BlueprintRevision=1}).Accepted,s.LastPhysicalFailure);
        for(int n=0;n<48&&!s.ExportGrowthSave().BaseCampaign!.PublishedBuiltShips!.Any(x=>x.OwnerCivilizationId==Human);n++)Advance(s);
        var before=s.ExportGrowthSave();var ship=Assert.Single(before.BaseCampaign!.PublishedBuiltShips!,x=>x.OwnerCivilizationId==Human).ShipId!;
        var home=Game(before.BaseCampaign).Colonies!.Single(c=>c.ColonyId=="colony.sol");var cohort=home.Cohorts!.First(c=>c.Population>=400);var kits=PopulationRules.Stock(home.Stockpiles!,"resource.settlement-kit");
        ColonisationCommandDto Load(){var c=Command(s,ColonisationAction.LoadPassengers);c.Embark=new(){ShipId=ship,ColonyId=home.ColonyId,CohortId=cohort.CohortId,Population=100,Food=20};return c;}
        var load=Load();
        foreach(var point in new[]{"after-command","before-copy","after-cache-copy"})
        {
            s.PublishedTransactionProbe=x=>{if(x==point)throw new InvalidOperationException("Injected passenger load fault");};
            Assert.False(s.SubmitColonisation(load).Accepted);Assert.Equal(Json(before),Json(s.ExportGrowthSave()));
        }
        s.PublishedTransactionProbe=null;Assert.True(s.SubmitColonisation(load).Accepted,s.LastPhysicalFailure);Assert.False(s.SubmitColonisation(load).Accepted);
        var loaded=s.ExportGrowthSave();var m=Assert.Single(loaded.BaseCampaign!.Colonisation!.ManifestRecords!,x=>x.OwnerId==Human);Assert.Equal(SettlerManifestKind.ImmigrationOnly,m.Kind);
        Assert.Equal(100,m.Passengers!.Population);Assert.Empty(m.Cargo!);Assert.Equal(kits,PopulationRules.Stock(Game(loaded.BaseCampaign).Colonies!.Single(c=>c.ColonyId==home.ColonyId).Stockpiles!,"resource.settlement-kit"));
        Assert.Equal(Json(loaded),Json(CampaignTacticalReviewSession.LoadGrowthSave(p,loaded).ExportGrowthSave()));
        var bad=Command(s,ColonisationAction.RequestDeployment);bad.Deploy=new(){ManifestId=m.ManifestId,PlanetId=home.PlanetId};Assert.False(s.SubmitColonisation(bad).Accepted);
        var unload=Command(s,ColonisationAction.UnloadTransport);unload.Unload=new(){ManifestId=m.ManifestId,ColonyId=home.ColonyId};Assert.True(s.SubmitColonisation(unload).Accepted,s.LastPhysicalFailure);
        var returned=s.ExportGrowthSave();Assert.DoesNotContain(Game(returned.BaseCampaign!).EntityIdentities,x=>x.EntityId==m.Passengers.CohortId);
        Assert.Contains(returned.BaseCampaign!.Colonisation!.LandedCohortLineages!,x=>x.ManifestId==m.ManifestId&&x.CohortId==m.Passengers.CohortId&&x.Population==100);
        Assert.Equal(Json(returned),Json(CampaignTacticalReviewSession.LoadGrowthSave(p,returned).ExportGrowthSave()));
        var marsBefore=Game(returned.BaseCampaign).Colonies!.Single(c=>c.ColonyId=="colony.mars").Cohorts!.Sum(c=>c.Population);
        Assert.True(s.SubmitColonisation(Load()).Accepted,s.LastPhysicalFailure);var again=s.ExportGrowthSave().BaseCampaign!.Colonisation!.ManifestRecords!.Single(x=>x.OwnerId==Human&&x.Stage==SettlerManifestStage.Loaded);
        var arrive=Command(s,ColonisationAction.UnloadTransport);arrive.Unload=new(){ManifestId=again.ManifestId,ColonyId="colony.mars"};Assert.True(s.SubmitColonisation(arrive).Accepted,s.LastPhysicalFailure);
        var arrived=s.ExportGrowthSave();Assert.Equal(marsBefore+100,Game(arrived.BaseCampaign!).Colonies!.Single(c=>c.ColonyId=="colony.mars").Cohorts!.Sum(c=>c.Population));
        var supply=Command(s,ColonisationAction.LoadCarriedSupplies);supply.LoadSupplies=new(){ShipId=ship,SourceColonyId="colony.sol",Food=5};Assert.True(s.SubmitColonisation(supply).Accepted,s.LastPhysicalFailure);
        var v=s.ExportGrowthSave().BaseCampaign!.Colonisation!.ManifestRecords!.Single(x=>x.OwnerId==Human&&x.Stage==SettlerManifestStage.Loaded);
        var deliver=Command(s,ColonisationAction.DeliverCarriedSupplies);deliver.Deliver=new(){ManifestId=v.ManifestId,ColonyId="colony.mars",Food=5};Assert.True(s.SubmitColonisation(deliver).Accepted,s.LastPhysicalFailure);
        var final=s.ExportGrowthSave();Assert.Equal(ship,v.ShipId);Assert.Equal(Json(final),Json(CampaignTacticalReviewSession.LoadGrowthSave(p,final).ExportGrowthSave()));
    }
    [Fact]public void Actual250AiCompletesOnlyOneKitFreeImmigrationTripAndReplaysItsRealCargo()
    {
        var p=Package();var s=CampaignTacticalReviewSession.StartGrowthGame(p,PublishedConstructionProfileTests.Settings(250));var phases=new HashSet<string>();
        for(int n=0;n<160;n++)
        {
            var root=s.ExportGrowthSave();var plan=Assert.Single(root.AiPlans!);var manifests=root.BaseCampaign!.Colonisation!.ManifestRecords!.Where(m=>m.OwnerId==plan.ActorId&&m.Kind==SettlerManifestKind.ImmigrationOnly).ToList();
            output.WriteLine($"{n} {plan.Stage} {plan.HoldReason} cargo={manifests.Count} target={plan.EstablishedColonyId}");Assert.True(manifests.Count<=1);
            if(manifests.Any(m=>m.Stage==SettlerManifestStage.Loaded)&&phases.Add("loaded"))Assert.Equal(Json(root),Json(CampaignTacticalReviewSession.LoadGrowthSave(p,root).ExportGrowthSave()));
            if(plan.Stage==ColonisationAiStage.GrowthClosed)
            {
                var manifest=Assert.Single(manifests);Assert.Equal(SettlerManifestStage.Unloaded,manifest.Stage);Assert.Equal(100,manifest.PopulationTransferred);Assert.Equal(0,manifest.EquipmentConsumed);
                var world=Game(root.BaseCampaign).Colonies!.Single(c=>c.ColonyId==plan.EstablishedColonyId);Assert.Contains(root.BaseCampaign.Colonisation.LandedCohortLineages!,l=>l.ManifestId==manifest.ManifestId&&l.ColonyId==world.ColonyId&&l.Population==100);
                Assert.DoesNotContain(root.BaseCampaign.Colonisation.ManifestOrigins!.Single(o=>o.ManifestId==manifest.ManifestId).LoadedMaterials!,x=>x.ResourceDefinitionId=="resource.settlement-kit");
                Assert.Contains("loaded",phases);Assert.Equal(Json(root),Json(CampaignTacticalReviewSession.LoadGrowthSave(p,root).ExportGrowthSave()));
                for(int i=0;i<3;i++)Advance(s);Assert.Single(s.ExportGrowthSave().BaseCampaign!.Colonisation!.ManifestRecords!,m=>m.OwnerId==plan.ActorId&&m.Kind==SettlerManifestKind.ImmigrationOnly);return;
            }
            Advance(s);
        }
        Assert.Fail("Actual bounded AI journey did not close its immigration trip");
    }
    [Fact]public void GrowthEditionSurvivesEveryPhaseAndLateCopyFaultAtomically()
    {
        var s=Fresh();var before=Json(s.ExportGrowthSave());
        foreach(var phase in Enum.GetValues<StrategicTurnPhase>())
        {
            bool seen=false;s.PhysicalPhaseProbe=p=>{if(p==phase){seen=true;return false;}return true;};var f=s.CaptureStatus();
            Assert.False(s.AdvanceTurn(f.Turn,f.CampaignRevision).Accepted);Assert.True(seen);Assert.Equal(before,Json(s.ExportGrowthSave()));
        }
        s.PhysicalPhaseProbe=null;
        foreach(var point in new[]{"after-command","before-copy","after-transcript-copy","after-cache-copy"})
        {
            bool seen=false;s.PublishedTransactionProbe=p=>{if(p==point){seen=true;throw new InvalidOperationException("Growth fault");}};var f=s.CaptureStatus();
            Assert.False(s.AdvanceTurn(f.Turn,f.CampaignRevision).Accepted);Assert.True(seen);Assert.Equal(before,Json(s.ExportGrowthSave()));
        }
        s.PublishedTransactionProbe=null;Advance(s);var root=s.ExportGrowthSave();Assert.Contains(root.AiPlans![0].LatestActions!,a=>a.Accepted);
        Assert.Equal(Json(root),Json(CampaignTacticalReviewSession.LoadGrowthSave(Package(),root).ExportGrowthSave()));
    }

}
