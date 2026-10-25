using System.Text.Json;
using HobbyDS9.Contracts;
using Xunit;

namespace HobbyDS9.Simulation.Tests;

public sealed class CampaignActivitiesTests
{
    const string Human="civ.federation";
    static string Json(object value)=>JsonSerializer.Serialize(value);
    static CampaignActivitiesSaveDto Root(CampaignTacticalReviewSession s)=>s.ExportActivitiesSave();
    internal static CampaignActivitiesPackageDto Package()
    {
        var template=PlayableFleetTests.Package();template.ReplayOperationLimit=65536;
        return CampaignTacticalReviewSession.CreateActivitiesPackage(template,ActivityReadyClassTests.Catalogue(),ActivityReadyClassTests.Ready(),ActivityReadyClassTests.Support(),
            ActivityReadyClassTests.Read<CampaignActivityContentDto>("activities.json"),2048);
    }
    static CampaignActivityCommandDto Command(CampaignTacticalReviewSession s,CampaignActivityCommandKind kind,string target,string definition="",int revision=0)
    {var stamp=s.CaptureStatus();return new(){ActorId=Human,CommandId="command.activity."+stamp.CampaignRevision,ExpectedTurn=stamp.Turn,ExpectedRevision=stamp.CampaignRevision,
        Kind=kind,TargetId=target,DefinitionId=definition,BlueprintRevision=revision};}
    [Fact] public void StartsThreeTruthfulReadyClassesAndOnePaidMixedStream()
    {
        var s=CampaignTacticalReviewSession.StartActivities(Package(),PublishedConstructionProfileTests.Settings());
        var command=Command(s,CampaignActivityCommandKind.EnqueueStructure,"colony.sol","infrastructure.fabrication-annex.v1");
        var before=Root(s);var beforeCredits=before.Campaign!.Integrated!.Physical!.Core.Campaign.GameState.Civilizations!.Single(c=>c.CivilizationId==Human).Credits;
        var paid=s.SubmitActivity(command);Assert.True(paid.Accepted,paid.Error+" "+s.LastPhysicalFailure);
        var ship=s.SubmitActivity(Command(s,CampaignActivityCommandKind.EnqueueShip,"colony.sol","blueprint.ready.constitution-explorer",1));
        Assert.True(ship.Accepted,ship.Error+" "+s.LastPhysicalFailure);
        var queued=Root(s);Assert.Equal(2,queued.Activities!.Production!.Count(q=>q.ColonyId=="colony.sol"));
        Assert.Empty(queued.Campaign!.Integrated!.Physical!.Core.Campaign.GameState.Colonies!.Single(c=>c.ColonyId=="colony.sol").ProductionQueue!);
        Assert.Equal(beforeCredits-60-74,queued.Campaign.Integrated.Physical.Core.Campaign.GameState.Civilizations!.Single(c=>c.CivilizationId==Human).Credits);
        s=CampaignTacticalReviewSession.LoadActivities(Package(),queued);Assert.Equal(Json(queued),Json(Root(s)));
        for(int i=0;i<64&&!Root(s).PublishedBuiltShips!.Any(r=>r.OwnerCivilizationId==Human);i++){var status=s.CaptureStatus();var advanced=s.AdvanceTurn(status.Turn,status.CampaignRevision);Assert.True(advanced.Accepted,advanced.Error+" "+s.LastPhysicalFailure);}
        var completed=Root(s);Assert.Contains(completed.Activities!.Structures!,r=>r.OwnerId==Human&&r.DefinitionId=="infrastructure.fabrication-annex.v1");
        Assert.Contains(completed.PublishedBuiltShips!,r=>r.OwnerCivilizationId==Human&&r.BlueprintRevision==1);
        Assert.Equal(Json(completed),Json(Root(CampaignTacticalReviewSession.LoadActivities(Package(),completed))));
    }
    [Fact] public void FrozenPaidRevisionAndCandidateFailureKeepEveryRootFactAtomic()
    {
        var s=CampaignTacticalReviewSession.StartActivities(Package(),PublishedConstructionProfileTests.Settings());
        var initial=Json(Root(s));var queue=Command(s,CampaignActivityCommandKind.EnqueueShip,"colony.sol","blueprint.ready.constitution-explorer",1);
        Assert.True(s.EvaluateActivity(queue).Accepted);Assert.Equal(initial,Json(Root(s)));
        s.PublishedTransactionProbe=stage=>{if(stage=="after-cache-copy")throw new InvalidOperationException("Late activity failure.");};
        Assert.False(s.SubmitActivity(queue).Accepted);Assert.Equal(initial,Json(Root(s)));s.PublishedTransactionProbe=null;
        Assert.True(s.SubmitActivity(queue).Accepted);var draft=ActivityReadyClassTests.Ready()[0];draft.DisplayName="Explorer second revision";
        var status=s.CaptureStatus();var publication=s.SubmitStrategicCommand(new(){ActorCivilizationId=Human,CommandId="command.activity.revision2",ExpectedTurn=status.Turn,
            ExpectedRevision=status.CampaignRevision,Kind=CommandKind.PublishShipBlueprint,PublishShipBlueprint=new(){BlueprintId="blueprint.ready.constitution-explorer",Revision=2,Draft=draft}});
        Assert.True(publication.Accepted,publication.Error+" "+s.LastPhysicalFailure);
        Assert.Equal(1,Root(s).Activities!.Production!.Single().Paid!.Blueprint!.Revision);
        for(int i=0;i<48&&!Root(s).PublishedBuiltShips!.Any(r=>r.OwnerCivilizationId==Human);i++)
        {status=s.CaptureStatus();var advance=s.AdvanceTurn(status.Turn,status.CampaignRevision);Assert.True(advance.Accepted,advance.Error+" "+s.LastPhysicalFailure);}
        Assert.Equal(1,Root(s).PublishedBuiltShips!.Single(r=>r.OwnerCivilizationId==Human).BlueprintRevision);
        var good=Root(s);Assert.Equal(Json(good),Json(Root(CampaignTacticalReviewSession.LoadActivities(Package(),good))));
        var bad=JsonSerializer.Deserialize<CampaignActivitiesSaveDto>(Json(good))!;bad.Activities!.Production!.Clear();bad.Activities.Structures!.Add(new(){ColonyId="colony.sol",DefinitionId="infrastructure.fabrication-annex.v1"});
        Assert.Throws<ArgumentException>(()=>CampaignTacticalReviewSession.LoadActivities(Package(),bad));Assert.Equal(Json(good),Json(Root(s)));
    }
    [Fact] public void OldProfilesAndMalformedContentAreNotSilentlyActivated()
    {
        var p=Package();p.Content!.Sites![0].Choices!.Add(null!);
        Assert.Throws<ArgumentException>(()=>CampaignTacticalReviewSession.StartActivities(p,PublishedConstructionProfileTests.Settings()));
    }
    static void Travel(CampaignTacticalReviewSession s,string fleet,List<string> route)
    {var status=s.CaptureStatus();var r=s.SubmitTravelOrder(new(){ActorId=Human,CommandId="command.activity.travel."+status.CampaignRevision,ExpectedTurn=status.Turn,ExpectedRevision=status.CampaignRevision,
        Order=new(){FleetId=fleet,Intent=CampaignTravelIntent.Leave,RouteNodeIds=route}});Assert.True(r.Accepted,r.Error+" "+s.LastPhysicalFailure);}
    static List<string> KnownPath(CampaignGeneratedMapDto map,string origin,string target)
    {
        var previous=new Dictionary<string,string?>{{origin,null}};var pending=new Queue<string>();pending.Enqueue(origin);
        while(pending.Count>0){var current=pending.Dequeue();foreach(var edge in map.Connections.Where(e=>e.EndpointA==current||e.EndpointB==current).OrderBy(e=>e.ConnectionId))
            {var next=edge.EndpointA==current?edge.EndpointB:edge.EndpointA;if(previous.TryAdd(next,current))pending.Enqueue(next);}}
        if(!previous.ContainsKey(target))return new();var path=new List<string>();for(string? n=target;n!=origin;n=previous[n!])path.Add(n!);path.Reverse();return path;
    }
    static ActivityKnownSiteDto Discover(CampaignTacticalReviewSession s,bool minor)
    {
        var visited=new HashSet<string>();
        for(int i=0;i<48;i++)
        {
            var fleet=s.OwnStrategicFleets(Human).First();visited.Add(fleet.LocationNodeId.Value);
            var found=s.CaptureActivities(Human).Sites.FirstOrDefault(site=>(site.Minor!=null)==minor);
            if(found!=null)return found;
            if(fleet.RouteNodeIds.Count==0&&fleet.TransitTotalCost==0)
            {
                var map=s.CaptureGeneratedMap(Human);
                var path=map.Regions.Where(r=>!visited.Contains(r.NodeId)).Select(r=>KnownPath(map,fleet.LocationNodeId.Value,r.NodeId))
                    .Where(r=>r.Count>0).OrderBy(r=>r.Count).ThenBy(r=>r.Last(),StringComparer.Ordinal).First();Travel(s,fleet.FleetId.Value,path);
            }
            var status=s.CaptureStatus();var result=s.AdvanceTurn(status.Turn,status.CampaignRevision);Assert.True(result.Accepted,result.Error+" "+s.LastPhysicalFailure);
            Assert.Equal(SessionPhase.Planning,s.CaptureStatus().Phase);
        }
        throw new InvalidOperationException("No legitimately observed site in bounded exploration.");
    }
    [Fact] public void ActualMinorConsentGatesFutureChoicesAndIndependentFactsReplay()
    {
        var s=CampaignTacticalReviewSession.StartActivities(Package(),PublishedConstructionProfileTests.Settings(250));
        var site=Discover(s,true);Assert.NotNull(site.Minor);Assert.NotNull(site.MinorDefinition);Assert.Null(site.MinorDefinition.Encounter);Assert.Empty(site.Definition.Choices!);
        Assert.Equal(ActivityMinorRelation.Observed,site.Relation);Assert.DoesNotContain(site.Choices,c=>c.ChoiceId.EndsWith(".aid")||c.ChoiceId.EndsWith(".exchange"));
        var ship=s.CaptureActivities(Human).Ships.First();
        if(ship.NodeId!=site.Site.NodeId){Travel(s,ship.FleetId,KnownPath(s.CaptureGeneratedMap(Human),ship.NodeId,site.Site.NodeId));
            for(int i=0;i<20&&s.CaptureActivities(Human).Ships.First().NodeId!=site.Site.NodeId;i++){var stamp=s.CaptureStatus();Assert.True(s.AdvanceTurn(stamp.Turn,stamp.CampaignRevision).Accepted);}}
        var channel=site.Choices.Single(c=>c.ChoiceId.EndsWith(".channel"));var command=Command(s,CampaignActivityCommandKind.StartStudy,site.Site.SiteId,channel.ChoiceId);command.ShipId=ship.ShipId;
        Assert.True(s.SubmitActivity(command).Accepted);
        var premature=Command(s,CampaignActivityCommandKind.ClaimFinding,site.Site.SiteId,channel.ChoiceId);var root=Json(Root(s));Assert.False(s.SubmitActivity(premature).Accepted);Assert.Equal(root,Json(Root(s)));
        var status=s.CaptureStatus();Assert.True(s.AdvanceTurn(status.Turn,status.CampaignRevision).Accepted);
        var claim=s.SubmitActivity(Command(s,CampaignActivityCommandKind.ClaimFinding,site.Site.SiteId,channel.ChoiceId));Assert.True(claim.Accepted,claim.Error+" "+s.LastPhysicalFailure);
        var current=s.CaptureActivities(Human).Sites.Single(x=>x.Site.SiteId==site.Site.SiteId);
        Assert.Contains(current.Relation!.Value,new[]{ActivityMinorRelation.Accepted,ActivityMinorRelation.Refused});
        Assert.Equal(current.Relation==ActivityMinorRelation.Accepted,current.Choices.Any(c=>c.ChoiceId.EndsWith(".aid")||c.ChoiceId.EndsWith(".exchange")));
        var completed=Root(s);Assert.Equal(Json(completed),Json(Root(CampaignTacticalReviewSession.LoadActivities(Package(),completed))));
        var own=s.CaptureActivities(Human);own.Sites.Clear();own.Findings.Clear();Assert.NotEmpty(s.CaptureActivities(Human).Sites);
    }
    [Fact] public void LeavingAcceptedStudyTerminatesAttemptAndNeverGrantsUnearnedReward()
    {
        var s=CampaignTacticalReviewSession.StartActivities(Package(),PublishedConstructionProfileTests.Settings(250));var site=Discover(s,false);
        var ship=s.CaptureActivities(Human).Ships.First();
        Assert.Equal(site.Site.NodeId,ship.NodeId);
        var choice=site.Choices.First(c=>c.Work>0&&c.RequiredStudy<=3);
        var command=Command(s,CampaignActivityCommandKind.StartStudy,site.Site.SiteId,choice.ChoiceId);command.ShipId=ship.ShipId;
        Assert.True(s.SubmitActivity(command).Accepted);
        var map=s.CaptureGeneratedMap(Human);var adjacent=map.Connections.First(e=>e.EndpointA==ship.NodeId||e.EndpointB==ship.NodeId);
        Travel(s,ship.FleetId,new(){adjacent.EndpointA==ship.NodeId?adjacent.EndpointB:adjacent.EndpointA});
        var finding=s.CaptureActivities(Human).Findings.Single();Assert.Equal(ActivityProgressStage.Aborted,finding.Stage);Assert.Equal(0,finding.InsightApplied);
        var good=Root(s);Assert.False(s.SubmitActivity(Command(s,CampaignActivityCommandKind.ClaimFinding,site.Site.SiteId,choice.ChoiceId)).Accepted);
        Assert.Equal(Json(good),Json(Root(s)));Assert.Equal(Json(good),Json(Root(CampaignTacticalReviewSession.LoadActivities(Package(),good))));
    }
    [Fact] public void LawfulExplorationFindsFiniteScienceAndReplaysClaimWithoutLeakingUnseenSites()
    {
        var s=CampaignTacticalReviewSession.StartActivities(Package(),PublishedConstructionProfileTests.Settings(250));
        Assert.Empty(s.CaptureActivities(Human).Sites);
        var visited=new HashSet<string>();ActivityKnownSiteDto? found=null;
        for(int i=0;i<40&&found==null;i++)
        {
            var fleet=s.OwnStrategicFleets(Human).First();visited.Add(fleet.LocationNodeId.Value);
            found=s.CaptureActivities(Human).Sites.FirstOrDefault(site=>site.Minor==null&&site.Choices.Any(c=>c.RequiredStudy<=3));
            if(found!=null)break;
            var map=s.CaptureGeneratedMap(Human);
            var path=map.Regions.Where(r=>!visited.Contains(r.NodeId)).Select(r=>KnownPath(map,fleet.LocationNodeId.Value,r.NodeId))
                .Where(r=>r.Count>0).OrderBy(r=>r.Count).ThenBy(r=>r.Last(),StringComparer.Ordinal).First();
            Travel(s,fleet.FleetId.Value,path);
            var status=s.CaptureStatus();var result=s.AdvanceTurn(status.Turn,status.CampaignRevision);Assert.True(result.Accepted,result.Error+" "+s.LastPhysicalFailure);
            Assert.Equal(SessionPhase.Planning,s.CaptureStatus().Phase);
        }
        Assert.NotNull(found);
        var own=s.OwnStrategicFleets(Human).First();
        if(own.LocationNodeId.Value!=found.Site.NodeId)
        {Travel(s,own.FleetId.Value,KnownPath(s.CaptureGeneratedMap(Human),own.LocationNodeId.Value,found.Site.NodeId));while(s.OwnStrategicFleets(Human).First().LocationNodeId.Value!=found.Site.NodeId)
            {var status=s.CaptureStatus();Assert.True(s.AdvanceTurn(status.Turn,status.CampaignRevision).Accepted);}}
        var ship=own.ShipIds.First().Value;var choice=found.Choices.First(c=>c.RequiredStudy<=3);
        var start=Command(s,CampaignActivityCommandKind.StartStudy,found.Site.SiteId,choice.ChoiceId);start.ShipId=ship;
        Assert.True(s.SubmitActivity(start).Accepted);
        for(int i=0;i<3&&s.CaptureActivities(Human).Findings.Last().Stage==ActivityProgressStage.Active;i++)
        {var status=s.CaptureStatus();var result=s.AdvanceTurn(status.Turn,status.CampaignRevision);Assert.True(result.Accepted,result.Error+" "+s.LastPhysicalFailure);}
        Assert.Equal(ActivityProgressStage.AwaitingClaim,s.CaptureActivities(Human).Findings.Last().Stage);
        var before=Root(s);s=CampaignTacticalReviewSession.LoadActivities(Package(),before);Assert.Equal(Json(before),Json(Root(s)));
        var claimed=s.SubmitActivity(Command(s,CampaignActivityCommandKind.ClaimFinding,found.Site.SiteId,choice.ChoiceId));Assert.True(claimed.Accepted,claimed.Error+" "+s.LastPhysicalFailure);
        var finding=s.CaptureActivities(Human).Findings.Last();Assert.Equal(ActivityProgressStage.Completed,finding.Stage);Assert.True(finding.InsightApplied>0);
        Assert.False(s.SubmitActivity(Command(s,CampaignActivityCommandKind.StartStudy,found.Site.SiteId,choice.ChoiceId)).Accepted);
        var after=Root(s);Assert.Equal(Json(after),Json(Root(CampaignTacticalReviewSession.LoadActivities(Package(),after))));
    }
}
