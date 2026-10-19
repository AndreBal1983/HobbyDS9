using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using HobbyDS9.Application;
using HobbyDS9.Contracts;
using HobbyDS9.Simulation;
using Xunit;

namespace HobbyDS9.Simulation.Tests;

public sealed class FrontierTests
{
    private const string A="civ.federation",B="civ.cardassian",FA="fleet.federation-scout",FB="fleet.cardassian-scout";
    private static string Json<T>(T x)=>JsonSerializer.Serialize(x);
    private static T Clone<T>(T x)=>JsonSerializer.Deserialize<T>(Json(x))!;
    internal static CampaignFrontierPackageDto Package()=>FrontierPackageReader.Read(Path.Combine(AppContext.BaseDirectory,"scenarios/frontier-reconnaissance-v1"));
    private static CampaignTacticalReviewSession Backend(ICampaignFrontierSession app)=>(CampaignTacticalReviewSession)app.GetType()
        .GetField("session",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)!.GetValue(app)!;
    private static void Advance(ICampaignFrontierSession app)
    {var s=app.CaptureStatus();Assert.True(app.AdvanceTurn(s.Turn,s.CampaignRevision).Accepted,Backend(app).LastPhysicalFailure);}
    private static SubmitCampaignTravelOrderDto Order(ICampaignFrontierSession app,params string[] nodes)
    {var s=app.CaptureStatus();return new(){ActorId=A,CommandId="command.frontier.test."+s.Turn+"."+s.CampaignRevision,
        ExpectedTurn=s.Turn,ExpectedRevision=s.CampaignRevision,Order=new(){FleetId=FA,Intent=nodes.Length==0?CampaignTravelIntent.Stay:CampaignTravelIntent.Leave,RouteNodeIds=nodes.ToList()}};}
    private static void Route(ICampaignFrontierSession app,params string[] nodes)=>Assert.True(app.SubmitTravelOrder(Order(app,nodes)).Accepted);
    private static ICampaignFrontierSession Arrived(CampaignFrontierPackageDto p)
    {var app=CampaignFrontierApplication.StartPackage(p);Route(app,"nav.bajor");Advance(app);Advance(app);return app;}
    private static IEnumerable<CampaignFrontierReceiptDto> Receipts(ICampaignFrontierSession app)=>app.ExportFrontierSave().Discoveries!;

    [Fact] public void RealBranchingScenarioIsCanonicalRepeatedReorderedAndReloadEquivalent()
    {
        var p=Package();var json=Json(FrontierWalkthrough.Run(p));
        Assert.Equal(json,Json(FrontierWalkthrough.Run(p,true)));
        Assert.Equal(json,Json(FrontierWalkthrough.Run(p,false,true)));
        Assert.Equal(json,Json(FrontierWalkthrough.Run(p)));
        Assert.Equal("8af5a37850685afd665131bec6cd12eb575cfc883b5f745b9507263790fcbb18",p.PackageHash);
        Assert.Equal("332817bbf6383fc464e04445b49bc020be77a0abd3f8d71048ca0b295e99a588",Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json))).ToLowerInvariant());
    }
    [Fact] public void UnknownBranchCannotBeQueriedOrSubmittedAndSnapshotsStayDetached()
    {
        var p=Package();var app=CampaignFrontierApplication.StartPackage(p);var view=app.CaptureDiscovery(A);
        Assert.Equal(new[]{"nav.bajor","nav.sol"},view.Nodes.Select(n=>n.NodeId));Assert.Single(view.Lanes);Assert.Empty(view.Receipts);
        Assert.False(app.FindKnownRoute(A,"nav.sol","nav.bajoran-wormhole").Success);
        var before=Json(app.ExportFrontierSave());Assert.False(app.SubmitTravelOrder(Order(app,"nav.bajor","nav.bajoran-wormhole")).Accepted);
        Assert.Equal(before,Json(app.ExportFrontierSave()));
        Route(app,"nav.bajor");Advance(app);Advance(app);
        Assert.Equal(2,view.Nodes.Count);Assert.Empty(view.Receipts);
        Assert.Contains(app.CaptureDiscovery(A).Nodes,n=>n.NodeId=="nav.bajoran-wormhole");
        Assert.True(app.FindKnownRoute(A,"nav.bajor","nav.bajoran-wormhole").Success);
        Assert.DoesNotContain(app.CaptureDiscovery(A).Nodes,n=>n.NodeId=="nav.gamma-exit");
        Assert.Throws<ArgumentException>(()=>app.CaptureDiscovery(B));Assert.Throws<ArgumentException>(()=>app.CaptureDiscovery("civ.missing"));
    }
    [Fact] public void PartialEdgeAndStationaryPresenceDoNotDiscover()
    {
        var app=CampaignFrontierApplication.StartPackage(Package());Advance(app);
        Assert.Empty(app.CaptureDiscovery(A).Receipts);Assert.Equal(2,app.CaptureDiscovery(A).Nodes.Count);
        Route(app,"nav.bajor");Advance(app);
        var travel=app.GetOwnTravelReceipts(A).Single();Assert.Equal(0,travel.EdgesCompleted);Assert.True(travel.TransitProgress>0);
        Assert.Empty(app.CaptureDiscovery(A).Receipts);Assert.Equal(2,app.CaptureDiscovery(A).Nodes.Count);
        Advance(app);Assert.Single(app.CaptureDiscovery(A).Receipts);
    }
    [Fact] public void ActorLocalAiArrivalsUseRealSharedStampedValidationWithoutPublishingHumanKnowledge()
    {
        var p=Package();var app=CampaignFrontierApplication.StartPackage(p);Advance(app);
        Assert.Empty(app.CaptureDiscovery(A).Receipts);Assert.Equal(2,app.CaptureDiscovery(A).Nodes.Count);
        var saved=app.ExportFrontierSave();var start=saved.Integrated!.Physical!.MovementProof!.Start;
        Assert.Contains(start.AcceptedCommandIds!,id=>id.StartsWith("ai-command.frontier.",StringComparison.Ordinal));
        Assert.Contains(Receipts(app),r=>r.ActorId==B&&r.ArrivalNodeId=="nav.badlands");
        Assert.Contains(saved.Integrated.Physical.Core.Campaign.GameState.NavigationKnowledge!.Single(k=>k.CivilizationId==B).Nodes!,n=>n.Id=="nav.bajor"&&n.Level==NavigationKnowledgeLevel.Charted);
        Assert.All(start.Fleets!.Single(f=>f.FleetId==FB).RouteNodeIds!,node=>Assert.Contains(start.NavigationKnowledge!.Single(k=>k.CivilizationId==B).Nodes!,k=>k.Id==node&&k.Level!=NavigationKnowledgeLevel.Unknown));
        // A hidden node's label/cost cannot affect this turn's chosen known route.
        var changed=Clone(p);var d=changed.Integrated!.Physical!.CorePackage!.Campaign!.Definitions;
        d.NavigationRegions!.Single(n=>n.NodeId=="nav.gamma-exit").DisplayName="Unseen secret label";
        d.NavigationConnections!.Single(e=>e.ConnectionId=="connection.gamma-founders").BaseTraversalCost=999;
        var other=CampaignFrontierApplication.StartPackage(changed);Advance(other);
        Assert.Equal(Json(start.Fleets!.Single(f=>f.FleetId==FB).RouteNodeIds),Json(other.ExportFrontierSave().Integrated!.Physical!.MovementProof!.Start.Fleets!.Single(f=>f.FleetId==FB).RouteNodeIds));
    }
    [Theory][InlineData(false)][InlineData(true)]
    public void HumanAndTrustedAiUnknownRoutesShareGameplayLegality(bool ai)
    {
        var p=Package();if(ai)p.Integrated!.Physical!.CorePackage!.Campaign!.InitialState.Civilizations!.Single(c=>c.CivilizationId==A).ControllerKind=ControllerKind.AI;
        var app=CampaignFrontierApplication.StartPackage(p);var order=Order(app,"nav.bajor","nav.gamma-exit");
        if(ai)order.CommandId="ai-command.frontier.invalid";
        var method=typeof(CampaignTacticalReviewSession).GetMethod("ValidateTravelCommand",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance)!;
        Assert.Equal(CampaignTacticalError.InvalidOrder,(CampaignTacticalError)method.Invoke(Backend(app),new object[]{order,app.ExportFrontierSave().Integrated!.Physical!,ai})!);
    }
    [Fact] public void FirstArrivalsAreIdempotentAcrossReturnVisitsAndRepeatedLoads()
    {
        var p=Package();var app=Arrived(p);Route(app,"nav.sol");Advance(app);Advance(app);
        var rows=Json(app.CaptureDiscovery(A).Receipts);Route(app,"nav.bajor");Advance(app);Advance(app);
        Assert.Equal(rows,Json(app.CaptureDiscovery(A).Receipts));
        for(int i=0;i<3;i++){app=CampaignFrontierApplication.LoadSave(p,Clone(app.ExportFrontierSave()));Assert.Equal(rows,Json(app.CaptureDiscovery(A).Receipts));}
        var own=app.ExportFrontierSave().Integrated!.Physical!.Core.Campaign.GameState.NavigationKnowledge!.Single(k=>k.CivilizationId==A);
        Assert.DoesNotContain(own.Nodes!,n=>n.Level==NavigationKnowledgeLevel.Surveyed);
    }
    [Theory][InlineData(0)][InlineData(1)][InlineData(2)][InlineData(3)][InlineData(4)][InlineData(5)][InlineData(6)]
    public void FailedPhaseOrPostChartingCommitRollsBackAllAuthority(int phase)
    {
        var p=Package();p.Integrated!.Physical!.CorePackage!.Campaign!.Definitions.NavigationConnections!.Single(e=>e.ConnectionId=="connection.sol-bajor").BaseTraversalCost=1;
        var app=CampaignFrontierApplication.StartPackage(p);Route(app,"nav.bajor");var before=Json(app.ExportFrontierSave());
        if(phase==6)Backend(app).FrontierCommitProbe=()=>throw new InvalidOperationException("after discovery");
        else Backend(app).PhysicalPhaseProbe=p=>(int)p!=phase;
        var s=app.CaptureStatus();Assert.False(app.AdvanceTurn(s.Turn,s.CampaignRevision).Accepted);Assert.Equal(before,Json(app.ExportFrontierSave()));
        Backend(app).PhysicalPhaseProbe=null;Backend(app).FrontierCommitProbe=null;Advance(app);Assert.Single(app.CaptureDiscovery(A).Receipts);
    }
    [Theory][InlineData("format")][InlineData("policy")][InlineData("hash")][InlineData("proofs")][InlineData("knowledge")]
    [InlineData("receipt")][InlineData("owner")][InlineData("route")][InlineData("prior-knowledge")][InlineData("duplicate")][InlineData("turn")][InlineData("truncated")]
    public void LoaderFailsClosedOnForgedKnowledgeProofReceiptOrBoundary(string field)
    {
        var p=Package();var save=Arrived(p).ExportFrontierSave();var receipt=save.Discoveries!.Last();
        switch(field)
        {
            case "format":save.FormatVersion=4;break;
            case "policy":save.PolicyId="invented";break;
            case "hash":save.PackageHash=new string('0',64);break;
            case "proofs":save.Transcript!.Clear();break;
            case "knowledge":save.Integrated!.Physical!.Core.Campaign.GameState.NavigationKnowledge!.Single(k=>k.CivilizationId==A).Nodes!.Single(n=>n.Id=="nav.gamma-exit").Level=NavigationKnowledgeLevel.Charted;break;
            case "receipt":receipt.ChartedNodeIds.Add("nav.gamma-exit");break;
            case "owner":receipt.ActorId="civ.dominion";break;
            case "route":save.Transcript![0].Travel!.Order!.RouteNodeIds.Clear();break;
            case "prior-knowledge":save.Integrated!.Physical!.MovementProof!.Start.NavigationKnowledge!.Single(k=>k.CivilizationId==A).Nodes!.Single(n=>n.Id=="nav.gamma-exit").Level=NavigationKnowledgeLevel.Charted;break;
            case "duplicate":save.Discoveries!.Add(Clone(receipt));break;
            case "turn":save.Transcript!.Last().Turn=99;break;
            default:save.Integrated!.Physical!.Core.ProfileId=CampaignTacticalReviewIdentity.Profile;break;
        }
        Assert.Throws<ArgumentException>(()=>CampaignFrontierApplication.LoadSave(p,save));
    }
    [Fact] public void CompleteProfileIsRequiredAtEveryExportAndNestedLoader()
    {
        var p=Package();var app=Arrived(p);var backend=Backend(app);var saved=app.ExportFrontierSave();
        Assert.Throws<ArgumentException>(()=>app.ExportIntegratedSave());Assert.Throws<InvalidOperationException>(()=>app.ExportSave());
        Assert.Throws<ArgumentException>(()=>backend.ExportSave());Assert.Throws<ArgumentException>(()=>backend.ExportTravelSave());
        Assert.Throws<ArgumentException>(()=>CampaignIntegratedPlaytestApplication.LoadSave(p.Integrated!,saved.Integrated!));
        Assert.Throws<ArgumentException>(()=>CampaignTacticalTravelApplication.LoadReviewSave(p.Integrated!.Physical!,saved.Integrated!.Physical!));
        Assert.Throws<ArgumentException>(()=>CampaignTacticalReviewSession.Load(p.Integrated!.Physical!.CorePackage!,saved.Integrated!.Physical!.Core));
        Assert.False(CampaignIntegratedPlaytestApplication.StartPackage(p.Integrated!) is ICampaignFrontierSession);
    }
    [Theory][InlineData("activation")][InlineData("policy")][InlineData("lane")][InlineData("hazard")][InlineData("wormhole")]
    public void PackageRejectsDefaultActivationOrOutOfScopeNavigation(string field)
    {
        var p=Package();switch(field){case "activation":p.DefaultActivationAllowed=true;break;case "policy":p.PolicyStatus="agreed";break;
            case "lane":p.PublicLaneIds!.Add("connection.fake");break;
            case "hazard":p.Integrated!.Physical!.CorePackage!.Campaign!.Definitions.NavigationRegions![0].PhenomenonDefinitionIds!.Add("phenomenon.badlands");break;
            default:p.Integrated!.Physical!.CorePackage!.Campaign!.Definitions.NavigationConnections![0].Kind=GalaxyEdgeKind.Wormhole;break;}
        Assert.Throws<ArgumentException>(()=>CampaignFrontierApplication.StartPackage(p));
    }
    private static CampaignFrontierPackageDto CombatPackage()
    {
        var p=Package();var physical=p.Integrated!.Physical!;var scenario=physical.CorePackage!.Campaign!;
        scenario.InitialState.Civilizations!.Single(c=>c.CivilizationId==B).ControllerKind=ControllerKind.Human;
        scenario.InitialState.Fleets!.Single(f=>f.FleetId==FB).LocationNodeId="nav.bajor";
        scenario.Definitions.NavigationConnections!.Single(e=>e.ConnectionId=="connection.sol-bajor").BaseTraversalCost=1;
        var own=scenario.InitialState.NavigationKnowledge!.Single(k=>k.CivilizationId==A);
        own.Nodes!.Single(n=>n.Id=="nav.badlands").Level=NavigationKnowledgeLevel.Charted;
        own.Connections!.Single(n=>n.Id=="connection.badlands-bajor").Level=NavigationKnowledgeLevel.Charted;
        var other=scenario.InitialState.NavigationKnowledge!.Single(k=>k.CivilizationId==B);
        foreach(var n in other.Nodes!)n.Level=NavigationKnowledgeLevel.Charted;
        foreach(var e in other.Connections!)e.Level=NavigationKnowledgeLevel.Charted;
        physical.InitialOrders!.Single(o=>o.FleetId==FB).Intent=CampaignTravelIntent.StayEngage;
        physical.InitialContacts!.Add(new(){ActorId=B,FleetId=FA,LastSeenNodeId="nav.sol",LastSeenTurn=1,TrackingNodeIds=other.Nodes!.Select(n=>n.Id).ToList()});
        return p;
    }
    private static void ResolveRounds(ICampaignFrontierSession app,bool reverse=false)
    {
        foreach(var id in app.GetEncounters(A))while(!app.Observe(id,A).Complete)
        {
            var commands=new[]{A,B}.Select(actor=>{var v=app.Observe(id,actor);return new CommitCampaignTacticalRoundDto {ActorId=actor,EncounterId=id,
                CommandId="command.frontier.combat."+actor+"."+v.Round,ExpectedTurn=v.Turn,ExpectedRound=v.Round,ExpectedBoundaryRevision=v.BoundaryRevision,
                Replacements=new()};}).ToArray();
            foreach(var c in reverse?commands.Reverse():commands)Assert.True(app.CommitRound(c).Accepted);
        }
    }
    [Theory][InlineData(false)][InlineData(true)]
    public void EncounterStopChartsOnlyActualArrivedPrefixAtAtomicNextPlanning(bool reverse)
    {
        var p=CombatPackage();var app=CampaignFrontierApplication.StartPackage(p);Route(app,"nav.bajor","nav.badlands");Advance(app);
        Assert.Equal(SessionPhase.AwaitingCombat,app.CaptureStatus().Phase);
        Assert.Equal("nav.bajor",app.GetOwnTravelReceipts(A).Single().EndNodeId);Assert.True(app.GetOwnTravelReceipts(A).Single().StoppedByEncounter);
        Assert.Empty(app.CaptureDiscovery(A).Receipts);Assert.DoesNotContain(app.CaptureDiscovery(A).Nodes,n=>n.NodeId=="nav.bajoran-wormhole");
        app=CampaignFrontierApplication.LoadSave(p,Clone(app.ExportFrontierSave()));ResolveRounds(app,reverse);
        Assert.Empty(app.CaptureDiscovery(A).Receipts);var before=Json(app.ExportFrontierSave());
        Backend(app).FrontierCommitProbe=()=>throw new InvalidOperationException("aftermath discovery failure");
        var stamp=app.CaptureStatus();Assert.False(app.FinalizeBatch(stamp.Turn,stamp.CampaignRevision).Accepted);Assert.Equal(before,Json(app.ExportFrontierSave()));
        Backend(app).FrontierCommitProbe=null;Assert.True(app.FinalizeBatch(stamp.Turn,stamp.CampaignRevision).Accepted);
        Assert.Equal(SessionPhase.Planning,app.CaptureStatus().Phase);Assert.Equal(2,app.CaptureStatus().Turn);
        Assert.Single(app.CaptureDiscovery(A).Receipts);Assert.Contains(app.CaptureDiscovery(A).Nodes,n=>n.NodeId=="nav.bajoran-wormhole");
        Assert.DoesNotContain(app.CaptureDiscovery(A).Nodes,n=>n.NodeId=="nav.cardassia");
        var save=Json(app.ExportFrontierSave());app=CampaignFrontierApplication.LoadSave(p,Clone(app.ExportFrontierSave()));Assert.Equal(save,Json(app.ExportFrontierSave()));
    }
    [Fact] public void InventedOlderArrivalAndCoordinatedKnowledgeReceiptTamperingCannotReplaceActualHistory()
    {
        var p=Package();var idle=CampaignFrontierApplication.StartPackage(p);Advance(idle);Advance(idle);Advance(idle);
        var saved=idle.ExportFrontierSave();var donor=Arrived(p).ExportFrontierSave();
        // The previously vulnerable attack: plausible older arrival plus consistent
        // latest knowledge, while the actual human fleet stayed at Sol.
        saved.Discoveries=Clone(donor.Discoveries);
        var donorKnowledge=donor.Integrated!.Physical!.Core.Campaign.GameState.NavigationKnowledge!.Single(k=>k.CivilizationId==A);
        var current=saved.Integrated!.Physical!.Core.Campaign.GameState;
        current.NavigationKnowledge![current.NavigationKnowledge.FindIndex(k=>k.CivilizationId==A)]=Clone(donorKnowledge);
        var start=saved.Integrated.Physical.MovementProof!.Start;
        start.NavigationKnowledge![start.NavigationKnowledge.FindIndex(k=>k.CivilizationId==A)]=Clone(donorKnowledge);
        Assert.Equal("nav.sol",current.Fleets!.Single(f=>f.FleetId==FA).LocationNodeId);
        Assert.Throws<ArgumentException>(()=>CampaignFrontierApplication.LoadSave(p,saved));
        // Actual legal arrival operations still cannot explain unchanged current facts.
        saved.Transcript=Clone(donor.Transcript);
        Assert.Throws<ArgumentException>(()=>CampaignFrontierApplication.LoadSave(p,saved));
        // Complete alternative legally replayable histories are intentionally valid.
        Assert.Equal(Json(donor),Json(CampaignFrontierApplication.LoadSave(p,donor).ExportFrontierSave()));
    }
    [Theory][InlineData("omit")][InlineData("reorder")][InlineData("extra")][InlineData("stamp")][InlineData("null")]
    public void TranscriptRejectsOmittedReorderedExtraPayloadOrStaleOperations(string kind)
    {
        var p=Package();var saved=Arrived(p).ExportFrontierSave();
        switch(kind){case "omit":saved.Transcript!.RemoveAt(1);break;
            case "reorder":(saved.Transcript![0],saved.Transcript[1])=(saved.Transcript[1],saved.Transcript[0]);break;
            case "extra":saved.Transcript![1].ActorId=A;break;
            case "stamp":saved.Transcript![1].Revision=999;break;
            default:saved.Transcript![0]=null!;break;}
        Assert.Throws<ArgumentException>(()=>CampaignFrontierApplication.LoadSave(p,saved));
    }
    [Fact] public void TranscriptOwnsInputAndExportAndRejectedReplayDoesNotAppendOperations()
    {
        var p=Package();var app=CampaignFrontierApplication.StartPackage(p);var command=Order(app,"nav.bajor");
        Assert.True(app.SubmitTravelOrder(command).Accepted);var before=Json(app.ExportFrontierSave());
        Assert.False(app.SubmitTravelOrder(command).Accepted);Assert.Equal(before,Json(app.ExportFrontierSave()));
        command.Order!.RouteNodeIds.Clear();command.ActorId=B;
        var copy=app.ExportFrontierSave();copy.Transcript![0].Travel!.Order!.RouteNodeIds.Clear();copy.Discoveries!.Clear();
        Assert.Equal(before,Json(app.ExportFrontierSave()));
        Assert.Equal(before,Json(CampaignFrontierApplication.LoadSave(p,app.ExportFrontierSave()).ExportFrontierSave()));
    }
    [Fact] public void TranscriptLimitRejects4097thOperationBeforeAnyMutationAndStillLoads4096()
    {
        var p=Package();var app=CampaignFrontierApplication.StartPackage(p);
        for(int n=0;n<CampaignFrontierIdentity.TranscriptLimit;n++)
        {
            var s=app.CaptureStatus();
            Assert.True(app.SetApproachCloak(A,"command.frontier.limit."+n,FA,false,s.Turn,s.CampaignRevision).Accepted);
        }
        var save=app.ExportFrontierSave();Assert.Equal(4096,save.Transcript!.Count);var before=Json(save);var stamp=app.CaptureStatus();
        Assert.Equal(CampaignTacticalError.Overflow,app.AdvanceTurn(stamp.Turn,stamp.CampaignRevision).Error);
        Assert.Equal(CampaignTacticalError.Overflow,app.SubmitTravelOrder(Order(app,"nav.bajor")).Error);
        Assert.Equal(CampaignTacticalError.Overflow,app.SetApproachCloak(A,"command.frontier.limit.extra",FA,false,stamp.Turn,stamp.CampaignRevision).Error);
        Assert.Equal(CampaignTacticalError.Overflow,app.SubmitStrategicCommand(null).Error);
        Assert.Equal(CampaignTacticalError.Overflow,app.CommitRound(null).Error);
        Assert.Equal(CampaignTacticalError.Overflow,app.FinalizeBatch(stamp.Turn,stamp.CampaignRevision).Error);
        Assert.Equal(before,Json(app.ExportFrontierSave()));
        Assert.Equal(before,Json(CampaignFrontierApplication.LoadSave(p,save).ExportFrontierSave()));
        save.Transcript.Add(Clone(save.Transcript[0]));Assert.Throws<ArgumentException>(()=>CampaignFrontierApplication.LoadSave(p,save));
    }
    [Fact] public void PendingAiCommitmentsReloadExactlyAndAftermathPublishesOnce()
    {
        var p=CombatPackage();p.Integrated!.Physical!.CorePackage!.Campaign!.InitialState.Civilizations!.Single(c=>c.CivilizationId==B).ControllerKind=ControllerKind.AI;
        var app=CampaignFrontierApplication.StartPackage(p);Route(app,"nav.bajor","nav.badlands");Advance(app);
        var saved=app.ExportFrontierSave();Assert.Single(Assert.Single(saved.Integrated!.Physical!.Core.Battles).Commitments);
        app=CampaignFrontierApplication.LoadSave(p,Clone(saved));Assert.Equal(Json(saved),Json(app.ExportFrontierSave()));
        var id=Assert.Single(app.GetEncounters(A));
        while(!app.Observe(id,A).Complete)
        {
            var v=app.Observe(id,A);Assert.True(app.CommitRound(new(){ActorId=A,EncounterId=id,CommandId="command.frontier.ai-round."+v.Round,
                ExpectedTurn=v.Turn,ExpectedRound=v.Round,ExpectedBoundaryRevision=v.BoundaryRevision,Replacements=new()}).Accepted);
            saved=app.ExportFrontierSave();app=CampaignFrontierApplication.LoadSave(p,Clone(saved));Assert.Equal(Json(saved),Json(app.ExportFrontierSave()));
            Assert.Empty(app.CaptureDiscovery(A).Receipts);
        }
        var s=app.CaptureStatus();Assert.True(app.FinalizeBatch(s.Turn,s.CampaignRevision).Accepted);Assert.Single(app.CaptureDiscovery(A).Receipts);
        var before=Json(app.ExportFrontierSave());Assert.False(app.FinalizeBatch(s.Turn,s.CampaignRevision).Accepted);Assert.Equal(before,Json(app.ExportFrontierSave()));
    }
    [Fact] public void AcceptedStrategicPayloadIsDeepCopiedAndReplayedThroughActualValidationAndStamps()
    {
        var p=Package();var app=CampaignFrontierApplication.StartPackage(p);var s=app.CaptureStatus();
        var command=new CommandEnvelope {CommandId="command.frontier.research",ActorCivilizationId=A,Kind=CommandKind.SetResearchAllocation,
            ExpectedTurn=s.Turn,ExpectedRevision=s.CampaignRevision,SetResearchAllocation=new(){ScienceAllocationUnits=ResearchUnits.Scale*6/10}};
        Assert.True(app.SubmitStrategicCommand(command).Accepted);var before=Json(app.ExportFrontierSave());
        command.SetResearchAllocation.ScienceAllocationUnits=0;
        Assert.Equal(before,Json(app.ExportFrontierSave()));Assert.Equal(before,Json(CampaignFrontierApplication.LoadSave(p,app.ExportFrontierSave()).ExportFrontierSave()));
        Advance(app);var saved=app.ExportFrontierSave();Assert.Equal(Json(saved),Json(CampaignFrontierApplication.LoadSave(p,saved).ExportFrontierSave()));
    }
    [Fact] public void StartingSectorCombatWithoutPhysicalArrivalChartsNothing()
    {
        var p=CombatPackage();var physical=p.Integrated!.Physical!;
        physical.CorePackage!.Campaign!.InitialState.Fleets!.Single(f=>f.FleetId==FA).LocationNodeId="nav.bajor";
        var app=CampaignFrontierApplication.StartPackage(p);Advance(app);ResolveRounds(app);
        var s=app.CaptureStatus();Assert.True(app.FinalizeBatch(s.Turn,s.CampaignRevision).Accepted);Assert.Empty(app.CaptureDiscovery(A).Receipts);
        Assert.DoesNotContain(app.CaptureDiscovery(A).Nodes,n=>n.NodeId=="nav.bajoran-wormhole");
    }
}
