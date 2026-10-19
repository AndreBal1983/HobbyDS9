using System.Text.Json;
using HobbyDS9.Contracts;
using HobbyDS9.TacticalReviewClient;
using Xunit;

namespace HobbyDS9.TacticalTravelClient.Tests;

public sealed class FrontierHostTests
{
    private const string A="civ.federation",B="civ.cardassian",Fleet="fleet.federation-scout",OtherFleet="fleet.cardassian-scout",Slot="frontier-only";
    private static string Json<T>(T x)=>JsonSerializer.Serialize(x);
    private static T Clone<T>(T x)=>JsonSerializer.Deserialize<T>(Json(x))!;
    private static CampaignFrontierPackageDto Package()=>FrontierPackageReader.Read(Path.Combine(AppContext.BaseDirectory,"scenarios/frontier-reconnaissance-v1"));
    private sealed class Codec : IFrontierReviewHostCodec
    {
        public bool FailSave,StripTranscript;public int Copies;
        public CampaignFrontierPackageDto CopyPackage(CampaignFrontierPackageDto p)=>Clone(p);
        public CampaignFrontierSaveDto CopySave(CampaignFrontierSaveDto s){Copies++;if(FailSave)throw new InvalidOperationException("copy failed");var copy=Clone(s);if(StripTranscript)copy.Transcript=null;return copy;}
        public CampaignIntegratedPlaytestPackageDto CopyPackage(CampaignIntegratedPlaytestPackageDto p)=>throw new InvalidOperationException("Nested3 package called");
        public CampaignIntegratedPlaytestSaveDto CopySave(CampaignIntegratedPlaytestSaveDto s)=>throw new InvalidOperationException("Nested3 save called");
        public CampaignTacticalTravelPackageDto CopyPackage(CampaignTacticalTravelPackageDto p)=>throw new InvalidOperationException("Nested2 package called");
        public CampaignTacticalTravelSaveDto CopySave(CampaignTacticalTravelSaveDto s)=>throw new InvalidOperationException("Nested2 save called");
    }
    private sealed class Store : IFrontierReviewCheckpointStore
    {
        public CampaignFrontierSaveDto? Save;public bool FailRead,FailWrite;
        public void Write(string slot,CampaignFrontierSaveDto root,bool overwrite)
        {if(FailWrite)throw new IOException();if(Save!=null&&!overwrite)throw new ReviewCheckpointStoreException(ReviewCheckpointError.SlotOccupied);Save=Clone(root);}
        public CampaignFrontierSaveDto Read(string slot)
        {if(FailRead)throw new IOException();return Save==null?throw new ReviewCheckpointStoreException(ReviewCheckpointError.NotFound):Clone(Save);}
    }
    private static void Route(ReviewController c,string target="nav.bajor")
    {
        var offer=c.FindKnownRoute(Fleet,target);Assert.NotNull(offer);Assert.True(offer.Route.Success);
        Assert.True(c.EditTravel(Fleet,CampaignTravelIntent.Leave,offer));Assert.True(c.ReviewTravel());
        var work=c.ConfirmTravel("command.frontier.host.route."+c.Frame.Status.CampaignRevision)!;
        var receipt=c.ExecutePending(work);Assert.True(receipt.Result.Accepted);Assert.True(c.Receive(receipt));Assert.False(c.RefreshRequired);
    }
    private static void Advance(PhysicalReviewHost host,ReviewController c)
    {Assert.True(host.AdvancePlanning(c.Frame).Accepted);Assert.True(c.Refresh());}
    private static CampaignFrontierPackageDto CombatPackage(bool ai=true)
    {
        var p=Package();var physical=p.Integrated!.Physical!;var scenario=physical.CorePackage!.Campaign!;
        scenario.InitialState.Civilizations!.Single(c=>c.CivilizationId==B).ControllerKind=ai?ControllerKind.AI:ControllerKind.Human;
        scenario.InitialState.Fleets!.Single(f=>f.FleetId==OtherFleet).LocationNodeId="nav.bajor";
        scenario.Definitions.NavigationConnections!.Single(e=>e.ConnectionId=="connection.sol-bajor").BaseTraversalCost=1;
        var other=scenario.InitialState.NavigationKnowledge!.Single(k=>k.CivilizationId==B);
        foreach(var n in other.Nodes!)n.Level=NavigationKnowledgeLevel.Charted;
        foreach(var n in other.Connections!)n.Level=NavigationKnowledgeLevel.Charted;
        physical.InitialOrders!.Single(o=>o.FleetId==OtherFleet).Intent=CampaignTravelIntent.StayEngage;
        physical.InitialContacts!.Add(new(){ActorId=B,FleetId=Fleet,LastSeenNodeId="nav.sol",LastSeenTurn=1,TrackingNodeIds=other.Nodes!.Select(n=>n.Id).ToList()});
        return p;
    }
    private static void Pending(PhysicalReviewHost host,ReviewController c)
    {Route(c);Advance(host,c);Assert.Equal(SessionPhase.AwaitingCombat,c.Frame.Status.Phase);Assert.True(c.SelectEncounter(Assert.Single(c.Frame.Encounters)));}
    private static void CommitRound(ReviewController c,string suffix="")
    {
        Assert.True(c.ReviewRound());var work=c.ConfirmRound("command.frontier.host.round."+c.Frame.Actor+"."+c.Frame.Battle!.Round+suffix)!;
        var receipt=c.ExecutePending(work);Assert.True(receipt.Result.Accepted);Assert.True(c.Receive(receipt));Assert.False(c.RefreshRequired);
    }
    [Fact] public void ActualKnownGraphAndDiscoveryUpdateOnlyAfterCompletedPhysicalArrival()
    {
        var host=PhysicalReviewHost.StartFrontier(Package(),new Codec());var c=new ReviewController(host,A);var first=c.Frame;
        Assert.Null(first.Demo);Assert.NotNull(first.Strategy);Assert.Equal(2,first.Frontier!.Nodes.Count);Assert.Empty(first.Frontier.Receipts);
        Assert.Equal(new[]{"nav.bajor","nav.sol"},first.Frontier.Nodes.Select(n=>n.NodeId));
        var unknown=c.FindKnownRoute(Fleet,"nav.bajoran-wormhole")!;Assert.False(unknown.Route.Success);Assert.False(c.EditTravel(Fleet,CampaignTravelIntent.Leave,unknown));
        Route(c);Advance(host,c);Assert.Equal(0,c.Frame.Travel!.OwnReceipts.Single().EdgesCompleted);
        Assert.Equal(2,c.Frame.Frontier!.Nodes.Count);Assert.Empty(c.Frame.Frontier.Receipts);
        Advance(host,c);Assert.Contains(c.Frame.Frontier!.Nodes,n=>n.NodeId=="nav.bajoran-wormhole");Assert.Single(c.Frame.Frontier.Receipts);
        Assert.True(c.FindKnownRoute(Fleet,"nav.bajoran-wormhole")!.Route.Success);
        Assert.Equal(2,first.Frontier.Nodes.Count);Assert.Empty(first.Frontier.Receipts);
        Assert.DoesNotContain(c.Frame.Frontier.Nodes,n=>n.NodeId=="nav.gamma-exit");
    }
    [Fact] public void ExactComplete5OpaqueSlotAndReopenKeepTranscriptDiscoveriesAndNestedState()
    {
        var codec=new Codec();var store=new Store();var host=PhysicalReviewHost.StartFrontier(Package(),codec,store);var c=new ReviewController(host,A);
        Route(c);Advance(host,c);Advance(host,c);var frame=c.Frame;var handle=c.SaveCommitted();Assert.True(c.SaveCommittedToSlot(Slot).Succeeded);
        var saved=Json(store.Save);Assert.Equal(5,store.Save!.FormatVersion);Assert.Equal(3,store.Save.Integrated!.FormatVersion);Assert.NotEmpty(store.Save.Transcript!);Assert.NotEmpty(store.Save.Discoveries!);
        Advance(host,c);Assert.True(c.LoadCommitted(handle));Assert.True(c.Frame.Generation>frame.Generation);
        Assert.Equal(Json(frame.Frontier),Json(c.Frame.Frontier));Assert.True(c.SaveCommittedToSlot(Slot,true).Succeeded);Assert.Equal(saved,Json(store.Save));
        Assert.True(c.ReopenFromSlot(Slot).Succeeded);Assert.Equal(Json(frame.Frontier),Json(c.Frame.Frontier));
        Assert.True(c.SaveCommittedToSlot(Slot,true).Succeeded);Assert.Equal(saved,Json(store.Save));Assert.True(codec.Copies>0);
        var opened=PhysicalReviewHost.OpenFrontierFromSlot(Package(),codec,store,Slot,A);Assert.True(opened.Succeeded);Assert.NotNull(opened.Frame!.Frontier);
    }
    [Theory][InlineData("read")][InlineData("copy")][InlineData("format")][InlineData("nested")][InlineData("transcript")][InlineData("knowledge")][InlineData("receipts")]
    public void FailedSlotLoadPreservesGenerationFrameAndLocalTravelDraft(string fault)
    {
        var codec=new Codec();var store=new Store();var host=PhysicalReviewHost.StartFrontier(Package(),codec,store);var c=new ReviewController(host,A);
        Assert.True(c.SaveCommittedToSlot(Slot).Succeeded);Assert.True(c.EditTravel(Fleet,CampaignTravelIntent.Stay));
        var before=c.Frame;var draft=c.TravelDraft;
        switch(fault){case "read":store.FailRead=true;break;case "copy":codec.FailSave=true;break;
            case "format":store.Save!.FormatVersion=4;break;case "nested":store.Save!.Integrated!.Physical!.Core.ProfileId=CampaignTacticalReviewIdentity.Profile;break;
            case "transcript":store.Save!.Transcript!.Add(new(){Kind=FrontierOperationKind.Advance,Turn=1,Revision=0});break;
            case "knowledge":store.Save!.Integrated!.Physical!.Core.Campaign.GameState.NavigationKnowledge!.Single(k=>k.CivilizationId==A).Nodes!.Single(n=>n.Id=="nav.gamma-exit").Level=NavigationKnowledgeLevel.Charted;break;
            default:store.Save!.Discoveries!.Add(new(){ActorId=A,FleetId=Fleet,ArrivalNodeId="nav.bajor",Turn=1,ChartedNodeIds=new(){"nav.gamma-exit"},ChartedLaneIds=new()});break;}
        Assert.False(c.LoadCommittedFromSlot(Slot,true).Succeeded);Assert.Same(before,c.Frame);Assert.Same(draft,c.TravelDraft);Assert.True(c.Dirty);
        Assert.Equal(before.Generation,host.Read(A).Generation);Assert.Equal(Json(before.Frontier),Json(host.Read(A).Frontier));
        Assert.False(c.ReopenFromSlot(Slot,true).Succeeded);Assert.Same(before,c.Frame);Assert.Same(draft,c.TravelDraft);
    }
    [Theory][InlineData("copy")][InlineData("write")][InlineData("occupied")][InlineData("strip")]
    public void FailedSaveKeepsOldStoredRootAndUnsentDraft(string fault)
    {
        var codec=new Codec();var store=new Store();var host=PhysicalReviewHost.StartFrontier(Package(),codec,store);var c=new ReviewController(host,A);
        Assert.True(c.SaveCommittedToSlot(Slot).Succeeded);var before=Json(store.Save);Assert.True(c.EditTravel(Fleet,CampaignTravelIntent.Stay));var draft=c.TravelDraft;var frame=c.Frame;
        if(fault=="copy")codec.FailSave=true;else if(fault=="write")store.FailWrite=true;else if(fault=="strip")codec.StripTranscript=true;
        Assert.False(c.SaveCommittedToSlot(Slot,fault!="occupied").Succeeded);Assert.Same(frame,c.Frame);Assert.Same(draft,c.TravelDraft);Assert.Equal(before,Json(store.Save));
    }
    [Fact] public void NoStoreUsesOnlyOpaqueComplete5AndForeignHandleCannotLoad()
    {
        var host=PhysicalReviewHost.StartFrontier(Package(),new Codec());var c=new ReviewController(host,A);var handle=c.SaveCommitted();
        Assert.Equal(ReviewCheckpointError.NoStore,c.SaveCommittedToSlot(Slot).Error);Assert.Equal(ReviewCheckpointError.NoStore,c.LoadCommittedFromSlot(Slot).Error);
        Assert.Equal(ReviewCheckpointError.NoStore,c.ReopenFromSlot(Slot).Error);Assert.True(c.LoadCommitted(handle));Assert.NotNull(c.Frame.Frontier);
        var foreign=new ReviewController(PhysicalReviewHost.StartFrontier(Package(),new Codec()),A);var before=foreign.Frame;
        Assert.Throws<ArgumentException>(()=>foreign.LoadCommitted(handle));Assert.Same(before,foreign.Frame);
    }
    [Fact] public void SuccessfulLoadRejectsOldOfferFramePendingWorkAndReceiptWithoutDroppingDraftOnFailedRecovery()
    {
        var host=PhysicalReviewHost.StartFrontier(Package(),new Codec(),new Store());var c=new ReviewController(host,A);var offer=c.FindKnownRoute(Fleet,"nav.bajor")!;
        var before=c.Frame;Assert.True(c.SaveCommittedToSlot(Slot).Succeeded);Assert.True(c.EditTravel(Fleet,CampaignTravelIntent.Leave,offer));Assert.True(c.ReviewTravel());
        var work=c.ConfirmTravel("command.frontier.host.stale")!;var draft=c.TravelDraft;
        var oldReceipt=host.Execute(work);Assert.True(oldReceipt.Result.Accepted);
        var second=new ReviewController(host,A);Assert.True(second.LoadCommittedFromSlot(Slot).Succeeded);
        Assert.Throws<InvalidOperationException>(()=>host.AdvancePlanning(before));Assert.Throws<InvalidOperationException>(()=>host.Execute(work));
        Assert.Throws<InvalidOperationException>(()=>c.ExecutePending(work));Assert.False(c.HasPendingWork);Assert.True(c.RefreshRequired);Assert.Same(draft,c.TravelDraft);
        Assert.Equal(ReviewCheckpointError.DirtyDraft,c.LoadCommittedFromSlot(Slot).Error);Assert.Same(draft,c.TravelDraft);
        Assert.True(c.LoadCommittedFromSlot(Slot,true).Succeeded);Assert.False(c.Dirty);Assert.False(c.EditTravel(Fleet,CampaignTravelIntent.Leave,offer));
        Assert.False(c.Receive(oldReceipt));
    }
    [Fact] public void FrontierFrameIsDetachedActorFilteredAndAbsentOnOlderPhysicalAndDemoHosts()
    {
        var package=Package();var host=PhysicalReviewHost.StartFrontier(package,new Codec());var first=host.Read(A);
        package.Integrated!.Physical!.CorePackage!.Campaign!.Definitions.NavigationRegions!.Single(n=>n.NodeId=="nav.sol").DisplayName="Changed caller package";
        Assert.Equal(Json(first.Frontier),Json(host.Read(A).Frontier));Assert.DoesNotContain(first.Frontier!.Nodes,n=>n.NodeId=="nav.cardassia");
        Assert.Throws<ArgumentException>(()=>host.Read(B));Assert.Throws<ArgumentException>(()=>new ReviewController(host,B));
        var json=Json(first);Assert.DoesNotContain("Transcript",json);Assert.DoesNotContain("MovementProof",json);Assert.DoesNotContain("OpponentPlan",json);
        Assert.False(first.Frontier.Nodes is List<CampaignFrontierNode>);
        using var old=new PhysicalFixture();Assert.Null(old.Host.Read(A).Frontier);
        Assert.Null(PhysicalReviewHost.StartDemo(SingleplayerDemoPackageReader.Read(Path.Combine(AppContext.BaseDirectory,"scenarios/singleplayer-demo-v1")),new DemoCodec()).Read(A).Frontier);
    }
    [Fact] public void RawUnknownRouteSubmissionRejectsBeforeAnyCompleteRootChange()
    {
        var store=new Store();var host=PhysicalReviewHost.StartFrontier(Package(),new Codec(),store);var frame=host.Read(A);
        Assert.True(host.SaveCommittedToSlot(Slot).Succeeded);var before=Json(store.Save);
        var result=host.SubmitTravelOrder(new(){ActorId=A,CommandId="command.frontier.host.unknown",ExpectedTurn=frame.Status.Turn,ExpectedRevision=frame.Status.CampaignRevision,
            Order=new(){FleetId=Fleet,Intent=CampaignTravelIntent.Leave,RouteNodeIds=new(){"nav.bajor","nav.gamma-exit"}}});
        Assert.False(result.Accepted);Assert.Equal(CampaignTacticalError.InvalidOrder,result.Error);
        Assert.True(host.SaveCommittedToSlot(Slot,true).Succeeded);Assert.Equal(before,Json(store.Save));
    }
    [Fact] public void ControllerReopenRoutesWorkThroughNewHostAndRejectsObsoleteExternalPointer()
    {
        var store=new Store();var host=PhysicalReviewHost.StartFrontier(Package(),new Codec(),store);var c=new ReviewController(host,A);
        Assert.True(c.SaveCommittedToSlot(Slot).Succeeded);var before=Json(store.Save);Assert.True(c.ReopenFromSlot(Slot).Succeeded);
        var offer=c.FindKnownRoute(Fleet,"nav.bajor")!;Assert.True(c.EditTravel(Fleet,CampaignTravelIntent.Leave,offer));Assert.True(c.ReviewTravel());
        var work=c.ConfirmTravel("command.frontier.host.reopened")!;
        Assert.Throws<InvalidOperationException>(()=>host.Execute(work));var receipt=c.ExecutePending(work);Assert.True(receipt.Result.Accepted);Assert.True(c.Receive(receipt));
        Assert.True(c.SaveCommittedToSlot(Slot,true).Succeeded);Assert.NotEqual(before,Json(store.Save));
        Assert.Equal(new[]{"nav.bajor"},store.Save!.Integrated!.Physical!.Core.Campaign.GameState.Fleets!.Single(f=>f.FleetId==Fleet).RouteNodeIds);
        Assert.Empty(host.Read(A).Travel!.OwnOrders.Single(o=>o.FleetId==Fleet).RouteNodeIds);
    }
    [Fact] public void ForeignOfferAndControllersWorkCannotReplaceOrExecuteRealPendingRequest()
    {
        var store=new Store();var host=PhysicalReviewHost.StartFrontier(Package(),new Codec(),store);var a=new ReviewController(host,A);var b=new ReviewController(host,A);
        var foreign=new ReviewController(PhysicalReviewHost.StartFrontier(Package(),new Codec()),A).FindKnownRoute(Fleet,"nav.bajor")!;
        Assert.False(a.EditTravel(Fleet,CampaignTravelIntent.Leave,foreign));
        foreach(var c in new[]{a,b}){Assert.True(c.EditTravel(Fleet,CampaignTravelIntent.Stay));Assert.True(c.ReviewTravel());}
        var own=a.ConfirmTravel("command.frontier.host.own")!;var other=b.ConfirmTravel("command.frontier.host.other")!;
        Assert.True(host.SaveCommittedToSlot(Slot).Succeeded);var before=Json(store.Save);
        Assert.Throws<InvalidOperationException>(()=>a.ExecutePending(other));Assert.True(a.HasPendingWork);
        Assert.True(host.SaveCommittedToSlot(Slot,true).Succeeded);Assert.Equal(before,Json(store.Save));
        var receipt=a.ExecutePending(own);Assert.True(receipt.Result.Accepted);Assert.True(a.Receive(receipt));Assert.False(a.Receive(receipt));
    }
    [Theory][InlineData(B)][InlineData("civ.missing")]
    public void ReopeningAsUncontrolledActorDoesNotPublishHostOrAlterPrivateRoot(string actor)
    {
        var codec=new Codec();var store=new Store();var host=PhysicalReviewHost.StartFrontier(Package(),codec,store);var c=new ReviewController(host,A);
        Assert.True(c.SaveCommittedToSlot(Slot).Succeeded);Assert.True(c.EditTravel(Fleet,CampaignTravelIntent.Stay));var draft=c.TravelDraft;var frame=c.Frame;var root=Json(store.Save);
        var result=PhysicalReviewHost.OpenFrontierFromSlot(Package(),codec,store,Slot,actor);
        Assert.Equal(ReviewCheckpointError.InvalidCheckpoint,result.Error);Assert.Null(result.Host);Assert.Null(result.Frame);
        Assert.Equal(root,Json(store.Save));Assert.Same(frame,c.Frame);Assert.Same(draft,c.TravelDraft);
    }
    [Fact] public void FailedOpaqueCopyDoesNotPublishCandidateOrDiscardExplicitlyRetainedDraft()
    {
        var codec=new Codec();var host=PhysicalReviewHost.StartFrontier(Package(),codec);var c=new ReviewController(host,A);var handle=c.SaveCommitted();
        Assert.True(c.EditTravel(Fleet,CampaignTravelIntent.Stay));var frame=c.Frame;var draft=c.TravelDraft;codec.FailSave=true;
        Assert.Throws<InvalidOperationException>(()=>c.LoadCommitted(handle,true));Assert.Same(frame,c.Frame);Assert.Same(draft,c.TravelDraft);Assert.Equal(frame.Generation,host.Read(A).Generation);
        codec.FailSave=false;Assert.True(c.LoadCommitted(handle,true));Assert.False(c.Dirty);Assert.True(c.Frame.Generation>frame.Generation);
    }
    [Fact] public void FailedPendingLoadKeepsTacticalDraftAndCommittedBoundary()
    {
        var store=new Store();var host=PhysicalReviewHost.StartFrontier(CombatPackage(),new Codec(),store);var c=new ReviewController(host,A);Pending(host,c);
        Assert.True(c.SaveCommittedToSlot(Slot).Succeeded);Assert.True(c.EditFleet(Fleet,CampaignFleetManeuver.Flank,null));var frame=c.Frame;var draft=Assert.Single(c.Draft);
        store.Save!.Integrated!.Physical!.Core.Battles.Single().Commitments.Clear();
        Assert.False(c.LoadCommittedFromSlot(Slot,true).Succeeded);Assert.Same(frame,c.Frame);Assert.Same(draft,Assert.Single(c.Draft));Assert.False(c.Frame.Battle!.OwnCommitmentAccepted);
    }
    [Fact] public void WaitingHumanCommitmentPersistsWithoutResolvingOtherHumanOrPublishingDiscovery()
    {
        var p=CombatPackage(false);var codec=new Codec();var store=new Store();var host=PhysicalReviewHost.StartFrontier(p,codec,store);var c=new ReviewController(host,A);Pending(host,c);CommitRound(c);
        Assert.True(c.Frame.Battle!.OwnCommitmentAccepted);Assert.Equal(1,c.Frame.Battle.Round);Assert.Empty(c.Frame.Frontier!.Receipts);
        Assert.True(c.SaveCommittedToSlot(Slot).Succeeded);var before=Json(store.Save);Assert.Equal(A,Assert.Single(store.Save!.Integrated!.Physical!.Core.Battles.Single().Commitments).ActorId);
        var opened=PhysicalReviewHost.OpenFrontierFromSlot(p,codec,store,Slot,A);Assert.True(opened.Succeeded);host=opened.Host!;c=new ReviewController(host,A);Assert.True(c.SelectEncounter(Assert.Single(c.Frame.Encounters)));
        Assert.True(c.Frame.Battle!.OwnCommitmentAccepted);Assert.False(c.ReviewRound());Assert.Empty(c.Frame.Frontier!.Receipts);
        Assert.True(c.SaveCommittedToSlot(Slot,true).Succeeded);Assert.Equal(before,Json(store.Save));
        var other=new ReviewController(host,B);Assert.True(other.SelectEncounter(Assert.Single(other.Frame.Encounters)));CommitRound(other);Assert.True(c.Refresh());
        Assert.Equal(2,c.Frame.Battle!.Round);Assert.False(c.Frame.Battle.OwnCommitmentAccepted);Assert.Empty(c.Frame.Frontier!.Receipts);
    }
    private sealed class DemoCodec : ISingleplayerDemoHostCodec
    {
        public CampaignSingleplayerDemoPackageDto CopyPackage(CampaignSingleplayerDemoPackageDto p)=>Clone(p);
        public CampaignSingleplayerDemoSaveDto CopySave(CampaignSingleplayerDemoSaveDto s)=>Clone(s);
        public CampaignIntegratedPlaytestPackageDto CopyPackage(CampaignIntegratedPlaytestPackageDto p)=>Clone(p);
        public CampaignIntegratedPlaytestSaveDto CopySave(CampaignIntegratedPlaytestSaveDto s)=>Clone(s);
        public CampaignTacticalTravelPackageDto CopyPackage(CampaignTacticalTravelPackageDto p)=>Clone(p);
        public CampaignTacticalTravelSaveDto CopySave(CampaignTacticalTravelSaveDto s)=>Clone(s);
    }
    [Theory][InlineData(true)][InlineData(false)]
    public void PendingHumanAndAiRoundsRestoreAndAftermathPublishesDiscoveryExactlyOnce(bool ai)
    {
        var p=CombatPackage(ai);var codec=new Codec();var store=new Store();var host=PhysicalReviewHost.StartFrontier(p,codec,store);var c=new ReviewController(host,A);Pending(host,c);
        Assert.Empty(c.Frame.Frontier!.Receipts);Assert.True(c.SaveCommittedToSlot(Slot).Succeeded);var pending=Json(store.Save);
        Assert.Equal(ai?1:0,store.Save!.Integrated!.Physical!.Core.Battles.Single().Commitments.Count);
        Assert.True(c.ReopenFromSlot(Slot).Succeeded);Assert.True(c.SaveCommittedToSlot(Slot,true).Succeeded);Assert.Equal(pending,Json(store.Save));
        var opened=PhysicalReviewHost.OpenFrontierFromSlot(p,codec,store,Slot,A);Assert.True(opened.Succeeded);host=opened.Host!;c=new ReviewController(host,A);Assert.True(c.SelectEncounter(Assert.Single(c.Frame.Encounters)));
        while(!c.Frame.Battle!.Complete)
        {
            ReviewController? other=null;if(!ai){other=new ReviewController(host,B);Assert.True(other.SelectEncounter(Assert.Single(other.Frame.Encounters)));}
            CommitRound(c);if(other!=null){CommitRound(other);Assert.True(c.Refresh());}
            Assert.Empty(c.Frame.Frontier!.Receipts);Assert.True(c.SaveCommittedToSlot(Slot,true).Succeeded);
            var saved=Json(store.Save);Assert.True(c.LoadCommittedFromSlot(Slot).Succeeded);Assert.Equal(saved,Json(store.Save));
        }
        Assert.True(c.ReviewCompletion());var work=c.ConfirmCompletion()!;var receipt=c.ExecutePending(work);Assert.True(receipt.Result.Accepted);Assert.True(c.Receive(receipt));
        Assert.Single(c.Frame.Frontier!.Receipts);Assert.Contains(c.Frame.Frontier.Nodes,n=>n.NodeId=="nav.bajoran-wormhole");
        Assert.True(c.SaveCommittedToSlot(Slot,true).Succeeded);var complete=Json(store.Save);Assert.False(c.Receive(receipt));Assert.Throws<InvalidOperationException>(()=>c.ExecutePending(work));
        Assert.True(c.ReopenFromSlot(Slot).Succeeded);Assert.Single(c.Frame.Frontier!.Receipts);Assert.True(c.SaveCommittedToSlot(Slot,true).Succeeded);Assert.Equal(complete,Json(store.Save));
    }
}
