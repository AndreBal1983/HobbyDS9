using System.Text.Json;
using HobbyDS9.Contracts;
using Xunit;
namespace HobbyDS9.Simulation.Tests;
public sealed class ColonisationExpansionTests
{
    private readonly Xunit.Abstractions.ITestOutputHelper output;
    public ColonisationExpansionTests(Xunit.Abstractions.ITestOutputHelper output){this.output=output;}
    internal static CampaignColonisationExpansionPackageDto Package()=>CampaignTacticalReviewSession.CreateColonisationExpansionPackage(
        CampaignTacticalReviewSession.CreateColonisationRosterPackage(ColonisationInventoryTests.Package()));
    internal static string Json(object o)=>JsonSerializer.Serialize(o);
    [Fact]public void ClosedSourceInitialCompleteReplayAndDetachedPlanPreserveV2Boundary()
    {
        var p=Package();var s=CampaignTacticalReviewSession.StartColonisationExpansion(p,PublishedConstructionProfileTests.Settings(30));
        var root=s.ExportColonisationExpansionSave();Assert.Equal(12,root.FormatVersion);var before=Json(root);var plan=Assert.Single(root.AiPlans!);
        Assert.Equal("civ.cardassian",plan.ActorId);Assert.Equal(ColonisationAiStage.Preparing,plan.Stage);Assert.Empty(plan.LatestActions!);
        Assert.Equal(before,Json(CampaignTacticalReviewSession.LoadColonisationExpansion(p,root).ExportColonisationExpansionSave()));
        foreach(var export in new Func<object>[] { ()=>s.ExportSave(),()=>s.ExportTravelSave(),()=>s.ExportIntegratedSave(),()=>s.ExportDemoSave(),()=>s.ExportFrontierSave(),()=>s.ExportGeneratedSave(),()=>s.ExportPublishedConstructionSave(),()=>s.ExportActivitiesSave(),()=>s.ExportStreamedActivitiesSave(),()=>s.ExportPlayableSave() })
        {var error=Record.Exception(()=>export());Assert.True(error is ArgumentException||error is InvalidOperationException,error?.ToString()??"Partial root escaped.");}
        Assert.Throws<ArgumentException>(()=>s.ExportColonisationSave());Assert.Throws<ArgumentException>(()=>CampaignTacticalReviewSession.LoadColonisationRoster(p.BaseRosterV2!,root.BaseCampaign!));
        root.AiPlans![0].HomeColonyId="foreign";Assert.Equal(before,Json(s.ExportColonisationExpansionSave()));
        Assert.Throws<ArgumentException>(()=>CampaignTacticalReviewSession.LoadColonisationExpansion(p,root));
        var stamp=s.CaptureStatus();var rejected=s.SubmitColonisation(new(){ActorId=plan.ActorId,CommandId="command.forged-ai",Turn=stamp.Turn,Revision=stamp.CampaignRevision,
            Action=ColonisationAction.EnqueueKit,EnqueueKit=new(){ColonyId="colony.cardassia"}});
        Assert.False(rejected.Accepted);Assert.Equal(CampaignTacticalError.ActorNotControlled,rejected.Error);Assert.Equal(before,Json(s.ExportColonisationExpansionSave()));
        p.PlannerPolicy!.MaximumAttempts=7;Assert.Equal(before,Json(s.ExportColonisationExpansionSave()));
        Assert.Throws<ArgumentException>(()=>CampaignTacticalReviewSession.StartColonisationExpansion(p,PublishedConstructionProfileTests.Settings(30)));
    }

    [Fact]public void Actual250AiManufacturesSurveysLoadsAndEstablishesThroughPublicAdvances()
    {
        var package=Package();var session=CampaignTacticalReviewSession.StartColonisationExpansion(package,PublishedConstructionProfileTests.Settings(250));
        var snapshots=new HashSet<string>();var trace=new List<string>();
        for(var i=0;i<160;i++)
        {
            var root=session.ExportColonisationExpansionSave();var p=Assert.Single(root.AiPlans!);var game=ColonisationTransportTests.Game(root.BaseCampaign!);
            var row=$"{game.TurnNumber} {p.Stage} {p.HoldReason} target={p.TargetPlanetId} scout={p.ScoutShipId} transport={p.TransportShipId} q={root.BaseCampaign!.Production!.Count} manifest={p.ManifestId} camp={p.CampId}";
            trace.Add(row);output.WriteLine(row);
            Assert.True(p.LatestActions!.Count<=8);Assert.True(p.LatestActions.Count(a=>a.Accepted)<=4);
            if(p.Stage==ColonisationAiStage.Loaded||p.Stage==ColonisationAiStage.Deploying||p.CampId!=null||root.BaseCampaign.Production!.Any(q=>q.OwnerId==p.ActorId))
            {
                var key=p.CampId==null?p.ManifestId==null?"queue":"loaded":p.EstablishedColonyId==null?"founding":"established";
                if(snapshots.Add(key))Assert.Equal(Json(root),Json(CampaignTacticalReviewSession.LoadColonisationExpansion(package,root).ExportColonisationExpansionSave()));
            }
            if(p.EstablishedColonyId!=null)
            {
                Assert.Contains(root.BaseCampaign.PublishedBuiltShips!,s=>s.OwnerCivilizationId==p.ActorId&&s.BlueprintId!.Contains("galaxy-colony-transport"));
                var camp=root.BaseCampaign.Colonisation!.CampSources!.Single(c=>c.CampId==p.CampId);Assert.True(camp.FedWeeks>=4);Assert.Equal(16,camp.CompletedIndustry);
                Assert.Equal(0,camp.Deaths);Assert.Equal(100,game.Colonies!.Single(c=>c.ColonyId==camp.ColonyId).Cohorts!.Sum(c=>c.Population));
                var settlers=root.BaseCampaign.Colonisation.ManifestRecords!.Single(m=>m.ManifestId==camp.ManifestId);Assert.Equal(100,settlers.PopulationTransferred);Assert.Equal(1,settlers.EquipmentConsumed);
                Assert.Contains("loaded",snapshots);Assert.Contains("founding",snapshots);Assert.Contains("established",snapshots);
                var supplies=Assert.Single(root.BaseCampaign.Colonisation.ManifestRecords!,m=>m.OwnerId==p.ActorId&&m.Kind==SettlerManifestKind.SupplyOnly);
                Assert.Null(supplies.Passengers);Assert.Equal(p.TransportShipId,supplies.ShipId);Assert.Equal(SettlerManifestStage.Delivered,supplies.Stage);Assert.True(supplies.FoodDelivered>0);
                Assert.Contains(root.BaseCampaign.Colonisation.ManifestOrigins!,o=>o.ManifestId==supplies.ManifestId&&o.Kind==ManifestOriginKind.Supply&&o.EmbarkedPopulation==0);
                return;
            }
            var stamp=session.CaptureStatus();Assert.Equal(SessionPhase.Planning,stamp.Phase);
            var result=session.AdvanceTurn(stamp.Turn,stamp.CampaignRevision);Assert.True(result.Accepted,result.Error+" "+session.LastPhysicalFailure+"\n"+string.Join("\n",trace.TakeLast(10)));
        }
        
        Assert.Fail(string.Join("\n",trace.TakeLast(30)));
    }

    [Fact]public void EveryPhaseAndLatePublicationFaultPreservesWholeOuterRootAndAiActions()
    {
        var session=CampaignTacticalReviewSession.StartColonisationExpansion(Package(),PublishedConstructionProfileTests.Settings(30));
        var before=Json(session.ExportColonisationExpansionSave());
        foreach(var phase in Enum.GetValues<StrategicTurnPhase>())
        {
            var seen=false;session.PhysicalPhaseProbe=p=>{if(p==phase){seen=true;return false;}return true;};var f=session.CaptureStatus();
            Assert.False(session.AdvanceTurn(f.Turn,f.CampaignRevision).Accepted);Assert.True(seen);Assert.Equal(before,Json(session.ExportColonisationExpansionSave()));
        }
        session.PhysicalPhaseProbe=null;
        foreach(var point in new[]{"after-command","before-copy","after-transcript-copy","after-cache-copy"})
        {
            var seen=false;session.PublishedTransactionProbe=p=>{if(p==point){seen=true;throw new InvalidOperationException("Injected late expansion failure.");}};
            var f=session.CaptureStatus();Assert.False(session.AdvanceTurn(f.Turn,f.CampaignRevision).Accepted);Assert.True(seen);Assert.Equal(before,Json(session.ExportColonisationExpansionSave()));
        }
        session.PublishedTransactionProbe=null;ColonisationTransportTests.Advance(session);var root=session.ExportColonisationExpansionSave();var ai=Assert.Single(root.AiPlans!);
        Assert.Contains(ai.LatestActions!,a=>a.Accepted&&a.Action==ColonisationAiAction.Ship);Assert.Contains(ai.LatestActions!,a=>a.Accepted&&a.Action==ColonisationAiAction.Kit);
        var game=ColonisationTransportTests.Game(root.BaseCampaign!);var humanBefore=ColonisationTransportTests.Game(System.Text.Json.JsonSerializer.Deserialize<CampaignColonisationExpansionSaveDto>(before)!.BaseCampaign!).Civilizations!.Single(c=>c.CivilizationId==ColonisationTransportTests.Human);
        Assert.True(game.Civilizations!.Single(c=>c.CivilizationId==humanBefore.CivilizationId).Credits>=humanBefore.Credits);
        Assert.DoesNotContain(root.BaseCampaign!.Journal!,j=>j.Colonisation?.ActorId==ai.ActorId||j.Existing?.Activity?.ActorId==ai.ActorId);
        Assert.Equal(Json(root),Json(CampaignTacticalReviewSession.LoadColonisationExpansion(Package(),root).ExportColonisationExpansionSave()));
    }
    [Fact]public void AggregatePreflightAndLiveSemanticsRejectForgedPlanWithoutAdopting()
    {
        var package=Package();var session=CampaignTacticalReviewSession.StartColonisationExpansion(package,PublishedConstructionProfileTests.Settings(30));ColonisationTransportTests.Advance(session);
        var before=Json(session.ExportColonisationExpansionSave());
        foreach(var corrupt in new Action<ColonisationAiPlanDto>[] {
            p=>p.SourceCohortId="cohort.foreign",p=>p.TargetPlanetId="planet.unknown",p=>p.TargetNodeId="node.unknown",p=>p.HomeColonyId="colony.sol",
            p=>p.Stage=ColonisationAiStage.Supporting,p=>p.LatestActions![0].AfterRevision=long.MaxValue,p=>p.LatestActions![0].Action=(ColonisationAiAction)999,
            p=>p.LatestActions!.Add(p.LatestActions[0]),p=>p.OwnAttemptedTargets=Enumerable.Range(0,9).Select(n=>"planet."+n).ToList(),p=>p.HoldReason=new string('x',10000000) })
        {
            var root=JsonSerializer.Deserialize<CampaignColonisationExpansionSaveDto>(before)!;corrupt(root.AiPlans![0]);
            Assert.Throws<ArgumentException>(()=>CampaignTacticalReviewSession.LoadColonisationExpansion(package,root));Assert.Equal(before,Json(session.ExportColonisationExpansionSave()));
        }
        var owned=typeof(CampaignTacticalReviewSession).GetField("expansion",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance)!.GetValue(session)!;
        var plans=(List<ColonisationAiPlanDto>)owned.GetType().GetField("Plans",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance)!.GetValue(owned)!;
        var original=plans[0].SourceCohortId;plans[0].SourceCohortId="cohort.foreign";
        Assert.Throws<ArgumentException>(()=>session.ExportColonisationExpansionSave());plans[0].SourceCohortId=original;Assert.Equal(before,Json(session.ExportColonisationExpansionSave()));
    }
    [Fact]public void TypedApplicationForkIsDetachedAndOldExportsCannotAdmitPartialExpansion()
    {
        var package=Package();var app=HobbyDS9.Application.CampaignColonisationExpansionApplication.StartGame(package,PublishedConstructionProfileTests.Settings(30));
        var before=Json(app.ExportColonisationExpansionSave());var status=app.CaptureStatus();var detached=app.ForkInteraction(status.Turn,status.CampaignRevision);
        Assert.True(detached.AdvanceTurn(status.Turn,status.CampaignRevision).Accepted);Assert.Equal(before,Json(app.ExportColonisationExpansionSave()));
        var saved=detached.ExportColonisationExpansionSave();Assert.Equal(Json(saved),Json(HobbyDS9.Application.CampaignColonisationExpansionApplication.LoadSave(package,saved).ExportColonisationExpansionSave()));
        Assert.Throws<InvalidOperationException>(()=>app.ExportPlayableSave());Assert.Throws<ArgumentException>(()=>app.CaptureColonisation("civ.cardassian"));
        Assert.All(app.CaptureColonisation(ColonisationTransportTests.Human).Worlds,w=>Assert.DoesNotContain("cardassia",w.Id));
    }
}
