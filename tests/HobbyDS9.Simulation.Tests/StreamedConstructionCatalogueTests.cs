using System.Text.Json;
using HobbyDS9.Contracts;
using Xunit;

namespace HobbyDS9.Simulation.Tests;

public sealed class StreamedConstructionCatalogueTests
{
    const string Human="civ.federation",Colony="colony.sol",Annex="infrastructure.fabrication-annex.v1",Lab="infrastructure.research-laboratory.v1";
    static string Json(object value)=>JsonSerializer.Serialize(value);
    static CampaignStreamedActivitiesPackageDto Package()=>CampaignTacticalReviewSession.CreateStreamedActivitiesPackage(CampaignActivitiesTests.Package());
    static void Rehash(CampaignStreamedActivitiesPackageDto p)
    {var c=p.ContentPackage!;p.ContentPackage=CampaignTacticalReviewSession.CreateActivitiesPackage(c.BaseTemplate!,c.Catalogue!,c.ReadyClasses!,c.Support!,c.Content!,c.RecordBudget);p.PackageHash=CampaignStreamedActivitiesData.PackageHash(p);}
    static CampaignTacticalReviewSession Start(CampaignStreamedActivitiesPackageDto p)=>CampaignTacticalReviewSession.StartStreamedActivities(p,PublishedConstructionProfileTests.Settings());
    static GameStateDto Game(CampaignStreamedActivitiesSaveDto root)=>root.Campaign!.Integrated!.Physical!.Core.Campaign.GameState;
    static CampaignActivityCommandDto Command(CampaignTacticalReviewSession s,CampaignActivityCommandKind kind,string target,string definition="")
    {var status=s.CaptureStatus();return new(){ActorId=Human,CommandId="command.catalogue."+status.CampaignRevision,ExpectedTurn=status.Turn,ExpectedRevision=status.CampaignRevision,Kind=kind,TargetId=target,DefinitionId=definition};}
    static void Enqueue(CampaignTacticalReviewSession s,string definition)
    {var result=s.SubmitActivity(Command(s,CampaignActivityCommandKind.EnqueueStructure,Colony,definition));Assert.True(result.Accepted,result.Error+" "+s.LastPhysicalFailure);}
    static StreamedColonyConstructionDto Row(CampaignTacticalReviewSession s)=>s.CaptureStreamedActivities(Human).Construction.Single(c=>c.ColonyId==Colony);
    [Fact] public void QueryIsOwnOnlyDetachedAndDoesNotChangeAnyRootOrLedger()
    {
        var s=Start(Package());var root=s.ExportStreamedActivitiesSave();var before=Json(root);var observed=s.CaptureStreamedActivities(Human);
        var own=Game(root).Colonies!.Where(c=>c.OwnerCivilizationId==Human).Select(c=>c.ColonyId).Order().ToArray();
        Assert.Equal(own,observed.Construction.Select(c=>c.ColonyId));Assert.DoesNotContain(observed.Construction,c=>Game(root).Colonies!.Single(x=>x.ColonyId==c.ColonyId).OwnerCivilizationId!=Human);
        Assert.Equal(3,Row(s).Buildings.Count);Assert.All(Row(s).Buildings,b=>{Assert.Equal(0,b.BuiltCount);Assert.Equal(0,b.QueuedCount);});
        var expected=Json(s.CaptureStreamedActivities(Human));observed.Construction[0].Current.Industry=-999;observed.Construction[0].Buildings[0].BuiltCount=99;
        observed.Construction[0].AfterPendingStructures.Output!.Research=-1;observed.Construction[0].Buildings.Clear();
        Assert.Equal(expected,Json(s.CaptureStreamedActivities(Human)));Assert.Equal(before,Json(s.ExportStreamedActivitiesSave()));
    }
    [Fact] public void CombinedProjectionMatchesRealCompletionWithNonlinearPowerAndNeverCountsQueuedAsBuilt()
    {
        var p=Package();var structures=p.ContentPackage!.Content!.Structures!;
        foreach(var item in structures.Where(s=>s.DefinitionId==Annex||s.DefinitionId==Lab)){item.PowerDemand=100;item.Industry=50;item.Cost!.Industry=1;item.Cost.Resources!.Clear();}
        Rehash(p);var s=Start(p);var initial=Row(s);
        var naive=initial.Current.Industry+initial.Buildings.Single(b=>b.DefinitionId==Annex).AfterOne!.Output!.Industry-initial.Current.Industry
            +initial.Buildings.Single(b=>b.DefinitionId==Lab).AfterOne!.Output!.Industry-initial.Current.Industry;
        Enqueue(s,Annex);Enqueue(s,Lab);var pending=Row(s);var forecast=pending.AfterPendingStructures.Output!;
        Assert.NotEqual(naive,forecast.Industry);
        foreach(var id in new[]{Annex,Lab})
        {
            var fact=pending.Buildings.Single(b=>b.DefinitionId==id);Assert.Equal(0,fact.BuiltCount);Assert.Equal(1,fact.QueuedCount);
            Assert.Equal(StreamedBuildingUnavailable.AlreadyQueued,fact.Unavailable);Assert.Null(fact.AfterOne);
        }
        Assert.Equal(Json(initial.Current),Json(pending.Current));var queued=s.ExportStreamedActivitiesSave();var rejected=s.SubmitActivity(Command(s,CampaignActivityCommandKind.EnqueueStructure,Colony,Annex));
        Assert.False(rejected.Accepted);Assert.Equal(Json(queued),Json(s.ExportStreamedActivitiesSave()));
        for(int i=0;i<8&&s.CaptureStreamedActivities(Human).Production.Any(q=>q.ColonyId==Colony);i++)
        {var status=s.CaptureStatus();Assert.True(s.AdvanceTurn(status.Turn,status.CampaignRevision).Accepted,s.LastPhysicalFailure);}
        Assert.DoesNotContain(s.CaptureStreamedActivities(Human).Production,q=>q.ColonyId==Colony);
        var completed=Row(s);Assert.Equal(forecast.Industry,completed.Current.Industry);Assert.Equal(forecast.Food,completed.Current.Food);
        // Research also contains evolving population/stability: re-evaluate the
        // frozen pre-queue determinants, rather than pretending it is an ETA.
        var frozen=Game(queued);var local=frozen.Colonies!.Single(c=>c.ColonyId==Colony);
        local.Infrastructure!.Add(new(){DefinitionId=Annex,Count=1});local.Infrastructure.Add(new(){DefinitionId=Lab,Count=1});
        // Read-only test audit of the actual retained definition owner. No DTO is
        // injected into the session and no live definitions are modified.
        var definitions=(DefinitionCatalogDto)typeof(CampaignTacticalReviewSession).GetField("definitions",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)!.GetValue(s)!;
        var projected=WorldEconomy.Project(local,frozen,definitions);Assert.Equal(projected.ResearchOutput,forecast.Research);
        foreach(var id in new[]{Annex,Lab}){var fact=completed.Buildings.Single(b=>b.DefinitionId==id);Assert.Equal(1,fact.BuiltCount);Assert.Equal(0,fact.QueuedCount);Assert.Equal(StreamedBuildingUnavailable.AlreadyBuilt,fact.Unavailable);}
        var saved=s.ExportStreamedActivitiesSave();var reopened=CampaignTacticalReviewSession.LoadStreamedActivities(p,saved);Assert.Equal(Json(saved),Json(reopened.ExportStreamedActivitiesSave()));Assert.Equal(Json(completed),Json(Row(reopened)));
    }
    [Fact] public void CancelReleasesQueuedCountWithoutAddingBuiltCapacityOrChangingCredits()
    {
        var s=Start(Package());var initial=Row(s);var credits=s.CaptureActivities(Human).Credits;Enqueue(s,Annex);
        var queued=s.CaptureStreamedActivities(Human).Production.Single(q=>q.ColonyId==Colony);
        Assert.True(s.SubmitActivity(Command(s,CampaignActivityCommandKind.CancelProduction,queued.QueueItemId)).Accepted);
        Assert.Equal(credits,s.CaptureActivities(Human).Credits);Assert.Equal(Json(initial),Json(Row(s)));
        var root=s.ExportStreamedActivitiesSave();Assert.False(root.ProductionHistory!.Single().Completed);Assert.Equal(Json(root),Json(CampaignTacticalReviewSession.LoadStreamedActivities(Package(),root).ExportStreamedActivitiesSave()));
    }
    [Fact] public void ValidCurrentSurvivesIndependentHypotheticalArithmeticOverflowAndDoesNotChangeAdmission()
    {
        var p=Package();p.ContentPackage!.Content!.Structures!.Single(s=>s.DefinitionId==Lab).Research=long.MaxValue;Rehash(p);
        var s=Start(p);var before=s.ExportStreamedActivitiesSave();var observed=Row(s);Assert.True(observed.Current.Research>=0);
        Assert.Equal(StreamedConstructionProjectionStatus.Available,observed.AfterPendingStructures.Status);
        var bad=observed.Buildings.Single(b=>b.DefinitionId==Lab);Assert.Equal(StreamedBuildingUnavailable.None,bad.Unavailable);
        Assert.Equal(StreamedConstructionProjectionStatus.ArithmeticOverflow,bad.AfterOne!.Status);Assert.Null(bad.AfterOne.Output);
        Assert.Equal(StreamedConstructionProjectionStatus.ArithmeticOverflow,bad.AfterPendingAndOne!.Status);Assert.NotEmpty(bad.AfterOne.Reason);
        Assert.Equal(StreamedConstructionProjectionStatus.Available,observed.Buildings.Single(b=>b.DefinitionId==Annex).AfterOne!.Status);
        Assert.Equal(Json(before),Json(s.ExportStreamedActivitiesSave()));
        Enqueue(s,Lab);var queued=s.ExportStreamedActivitiesSave();var after=Row(s);
        Assert.Equal(Json(observed.Current),Json(after.Current));Assert.Equal(StreamedConstructionProjectionStatus.ArithmeticOverflow,after.AfterPendingStructures.Status);
        var annex=after.Buildings.Single(b=>b.DefinitionId==Annex);Assert.Equal(StreamedConstructionProjectionStatus.Available,annex.AfterOne!.Status);
        Assert.Equal(StreamedConstructionProjectionStatus.ArithmeticOverflow,annex.AfterPendingAndOne!.Status);
        after.AfterPendingStructures.Reason="mutated";after.Buildings[0].BuiltCount=999;
        Assert.Equal(Json(queued),Json(s.ExportStreamedActivitiesSave()));Assert.Equal(Json(queued),Json(CampaignTacticalReviewSession.LoadStreamedActivities(p,queued).ExportStreamedActivitiesSave()));
    }
}
