using System.Text.Json;
using HobbyDS9.Contracts;
using HobbyDS9.TacticalReviewClient;
using Xunit;
namespace HobbyDS9.TacticalTravelClient.Tests;
public sealed class SingleplayerDemoHostTests
{
    private const string A="civ.federation",Fleet="fleet.federation-scout",Slot="demo-only";
    private static T Clone<T>(T x)=>JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(x))!;
    private static CampaignSingleplayerDemoPackageDto Package()=>SingleplayerDemoPackageReader.Read(Path.Combine(AppContext.BaseDirectory,"scenarios/singleplayer-demo-v1"));
    private sealed class Codec : ISingleplayerDemoHostCodec
    {
        public bool FailDemoSave;public int DemoCopies;
        public CampaignSingleplayerDemoPackageDto CopyPackage(CampaignSingleplayerDemoPackageDto p)=>Clone(p);
        public CampaignSingleplayerDemoSaveDto CopySave(CampaignSingleplayerDemoSaveDto s){DemoCopies++;if(FailDemoSave)throw new InvalidOperationException();return Clone(s);}
        public CampaignIntegratedPlaytestPackageDto CopyPackage(CampaignIntegratedPlaytestPackageDto p)=>Clone(p);
        public CampaignIntegratedPlaytestSaveDto CopySave(CampaignIntegratedPlaytestSaveDto s)=>throw new InvalidOperationException("Nested3 export called");
        public CampaignTacticalTravelPackageDto CopyPackage(CampaignTacticalTravelPackageDto p)=>Clone(p);
        public CampaignTacticalTravelSaveDto CopySave(CampaignTacticalTravelSaveDto s)=>throw new InvalidOperationException("Nested2 export called");
    }
    private sealed class Store : ISingleplayerDemoCheckpointStore
    {
        public CampaignSingleplayerDemoSaveDto? Save;public bool ReadFail,WriteFail;
        public void Write(string slot,CampaignSingleplayerDemoSaveDto root,bool overwrite)
        {if(WriteFail)throw new IOException();if(Save!=null&&!overwrite)throw new ReviewCheckpointStoreException(ReviewCheckpointError.SlotOccupied);Save=Clone(root);}
        public CampaignSingleplayerDemoSaveDto Read(string slot)
        {if(ReadFail)throw new IOException();return Save==null?throw new ReviewCheckpointStoreException(ReviewCheckpointError.NotFound):Clone(Save);}
    }
    private static void Route(PhysicalReviewHost host,ReviewController c,string fleet=Fleet)
    {var offer=c.FindKnownRoute(fleet,"nav.bajor");Assert.NotNull(offer);Assert.True(c.EditTravel(fleet,CampaignTravelIntent.Leave,offer));Assert.True(c.ReviewTravel());
        var work=c.ConfirmTravel("command.demo.host.travel."+c.Frame.Status.CampaignRevision)!;Assert.True(c.Receive(host.Execute(work)));Assert.False(c.RefreshRequired);}
    private static void Advance(PhysicalReviewHost host,ReviewController c)
    {Assert.True(host.AdvancePlanning(c.Frame).Accepted);Assert.True(c.Refresh());}
    private static void Pending(PhysicalReviewHost host,ReviewController c)
    {Route(host,c);for(int i=0;i<6&&c.Frame.Status.Phase==SessionPhase.Planning;i++)Advance(host,c);Assert.Equal(SessionPhase.AwaitingCombat,c.Frame.Status.Phase);Assert.True(c.SelectEncounter(c.Frame.Encounters.Single()));}
    [Fact] public void ActualControllerDrivesOneHumanDemoThroughBuildBattleAftermathAndTerminal()
    {
        var codec=new Codec();var store=new Store();var host=PhysicalReviewHost.StartDemo(Package(),codec,store);var c=new ReviewController(host,A);int steps=0;
        Assert.True(host.SubmitStrategicCommand(c.Frame,new CommandEnvelope {ActorCivilizationId=A,CommandId="command.demo.host.research",Kind=CommandKind.SetResearchAllocation,
            ExpectedTurn=c.Frame.Status.Turn,ExpectedRevision=c.Frame.Status.CampaignRevision,SetResearchAllocation=new SetResearchAllocationPayload {ScienceAllocationUnits=450}}).Accepted);Assert.True(c.Refresh());
        for(int n=0;n<2;n++){var f=c.Frame;Assert.True(host.SubmitStrategicCommand(f,new CommandEnvelope {ActorCivilizationId=A,CommandId="command.demo.host.build."+n,
            Kind=CommandKind.EnqueueProduction,ExpectedTurn=f.Status.Turn,ExpectedRevision=f.Status.CampaignRevision,
            EnqueueProduction=new EnqueueProductionPayload {ColonyId="colony.sol",DefinitionId="ship-design.source-cruiser"}}).Accepted);Assert.True(c.Refresh());}
        while(c.Frame.Demo!.Outcome==CampaignDemoOutcome.Active)
        {
            Assert.True(++steps<100,"Controller deadlock budget exceeded");
            if(c.Frame.Status.Phase==SessionPhase.Planning)
            {if(c.Frame.Status.Turn>=4)foreach(var fleet in c.Frame.OwnStrategicFleets.Where(f=>f.LocationNodeId.Value!="nav.bajor").ToList())Route(host,c,fleet.FleetId.Value);Advance(host,c);}
            else
            {
                if(c.Frame.Battle==null)Assert.True(c.SelectEncounter(c.Frame.Encounters.Single()));
                if(!c.Frame.Battle!.Complete)
                {Assert.True(c.ReviewRound());var work=c.ConfirmRound("command.demo.host.round."+steps)!;Assert.True(c.Receive(host.Execute(work)));Assert.False(c.RefreshRequired);}
                else{Assert.True(c.ReviewCompletion());var work=c.ConfirmCompletion()!;Assert.True(c.Receive(host.Execute(work)));Assert.False(c.RefreshRequired);}
            }
        }
        Assert.True(c.SaveCommittedToSlot(Slot).Succeeded);Assert.Equal(4,store.Save!.FormatVersion);Assert.Equal(3,store.Save.Integrated!.FormatVersion);
        Assert.Equal(2,store.Save.Integrated.BuiltShips!.Count(b=>b.OwnerCivilizationId==A));Assert.True(codec.DemoCopies>0);
        var outcome=c.Frame.Demo.Outcome;Assert.True(c.ReopenFromSlot(Slot).Succeeded);Assert.Equal(outcome,c.Frame.Demo!.Outcome);
        Assert.False(c.EditTravel(Fleet,CampaignTravelIntent.Stay));
        var terminal=PhysicalReviewHost.OpenDemoFromSlot(Package(),codec,store,Slot,A);Assert.True(terminal.Succeeded);
        Assert.False(terminal.Host!.AdvancePlanning(terminal.Frame!).Accepted);
    }
    [Fact] public void CompleteDemo4OpaqueAndSlotCheckpointsRestorePendingAiBoundary()
    {
        var host=PhysicalReviewHost.StartDemo(Package(),new Codec(),new Store());var c=new ReviewController(host,A);Pending(host,c);
        var before=c.Frame;var handle=c.SaveCommitted();Assert.True(c.SaveCommittedToSlot(Slot).Succeeded);
        Assert.True(c.ReviewRound());Assert.True(c.CancelReview());Assert.True(c.LoadCommitted(handle));Assert.True(c.Frame.Generation>before.Generation);
        Assert.Equal(1,c.Frame.Battle!.Round);Assert.False(c.Frame.Battle.OwnCommitmentAccepted);Assert.True(c.ReopenFromSlot(Slot).Succeeded);Assert.Equal(1,c.Frame.Battle!.Round);
        Assert.True(c.ReviewRound());var work=c.ConfirmRound("command.demo.host.after-reopen")!;
        Assert.Throws<InvalidOperationException>(()=>host.Execute(work));Assert.True(c.Receive(c.ExecutePending(work)));Assert.Equal(2,c.Frame.Battle!.Round);
    }
    [Theory][InlineData("read")][InlineData("nested")][InlineData("foreign")][InlineData("ai")][InlineData("missing")]
    public void FailedLoadKeepsHostGenerationFrameAndDraft(string fault)
    {
        var store=new Store();var host=PhysicalReviewHost.StartDemo(Package(),new Codec(),store);var c=new ReviewController(host,A);Pending(host,c);
        Assert.True(c.SaveCommittedToSlot(Slot).Succeeded);var fleet=c.Frame.Battle!.StandingOrders.First().FleetId;Assert.True(c.EditFleet(fleet,CampaignFleetManeuver.Flank,null));
        var before=c.Frame;var draft=c.Draft.Single();
        switch(fault){case "read":store.ReadFail=true;break;case "nested":store.Save!.FormatVersion=3;break;case "foreign":store.Save!.PackageHash=new string('a',64);break;
            case "ai":store.Save!.Integrated!.Physical!.Core.Battles.Single().Commitments.Single().Orders[0].Maneuver=CampaignFleetManeuver.Flank;break;
            default:store.Save!.Integrated!.Physical!.Core.Battles.Single().Commitments.Clear();break;}
        Assert.False(c.LoadCommittedFromSlot(Slot,true).Succeeded);Assert.Same(before,c.Frame);Assert.Same(draft,c.Draft.Single());Assert.True(c.Dirty);
    }
    [Fact] public void SuccessfulLoadInvalidatesOffersAndOldControllersButFailedLoadDoesNot()
    {
        var store=new Store();var host=PhysicalReviewHost.StartDemo(Package(),new Codec(),store);var c=new ReviewController(host,A);
        var oldFrame=c.Frame;var offer=c.FindKnownRoute(Fleet,"nav.bajor")!;Assert.True(c.SaveCommittedToSlot(Slot).Succeeded);
        Assert.True(c.LoadCommittedFromSlot(Slot).Succeeded);Assert.False(c.EditTravel(Fleet,CampaignTravelIntent.Leave,offer));
        Assert.Throws<InvalidOperationException>(()=>host.AdvancePlanning(oldFrame));
        Assert.True(c.EditTravel(Fleet,CampaignTravelIntent.Stay));
        Assert.Equal(ReviewCheckpointError.DirtyDraft,c.LoadCommittedFromSlot(Slot).Error);
        Assert.True(c.Dirty);Assert.True(c.LoadCommittedFromSlot(Slot,true).Succeeded);Assert.False(c.Dirty);
    }
    [Fact] public void DemoActorCannotBeSwitchedToAiAndLegacyFrameCapabilityIsAbsent()
    {
        var host=PhysicalReviewHost.StartDemo(Package(),new Codec());Assert.Throws<ArgumentException>(()=>host.Read("civ.cardassian"));
        Assert.Throws<ArgumentException>(()=>new ReviewController(host,"civ.cardassian"));Assert.NotNull(host.Read(A).Demo);
        using var legacy=new PhysicalFixture();Assert.Null(legacy.Host.Read(A).Demo);
    }
    [Theory][InlineData("serialization")][InlineData("write")]
    public void SaveFailureKeepsOldStoredSaveAndCurrentDraft(string fault)
    {
        var codec=new Codec();var store=new Store();var host=PhysicalReviewHost.StartDemo(Package(),codec,store);var c=new ReviewController(host,A);Assert.True(c.SaveCommittedToSlot(Slot).Succeeded);
        Assert.True(c.EditTravel(Fleet,CampaignTravelIntent.Stay));var draft=c.TravelDraft;var frame=c.Frame;var save=JsonSerializer.Serialize(store.Save);
        if(fault=="serialization")codec.FailDemoSave=true;else store.WriteFail=true;
        Assert.False(c.SaveCommittedToSlot(Slot,true).Succeeded);Assert.Same(frame,c.Frame);Assert.Same(draft,c.TravelDraft);Assert.Equal(save,JsonSerializer.Serialize(store.Save));
    }
    [Fact] public void CancelReviewKeepsDraftAndDoesNotChangeAuthoritativeCheckpoint()
    {
        var store=new Store();var host=PhysicalReviewHost.StartDemo(Package(),new Codec(),store);var c=new ReviewController(host,A);Pending(host,c);
        Assert.True(c.SaveCommittedToSlot(Slot).Succeeded);var before=JsonSerializer.Serialize(store.Save);
        Assert.True(c.EditFleet(c.Frame.Battle!.StandingOrders.First().FleetId,CampaignFleetManeuver.Flank,null));var edit=c.Draft.Single();
        Assert.True(c.ReviewRound());Assert.True(c.CancelReview());Assert.True(c.Dirty);Assert.Same(edit,c.Draft.Single());
        Assert.True(c.SaveCommittedToSlot(Slot,true).Succeeded);Assert.Equal(before,JsonSerializer.Serialize(store.Save));
    }
    [Fact] public void NoStoreSupportsOpaqueDemo4ButNotSlotFallback()
    {
        var host=PhysicalReviewHost.StartDemo(Package(),new Codec());var c=new ReviewController(host,A);var handle=c.SaveCommitted();
        Assert.Equal(ReviewCheckpointError.NoStore,c.SaveCommittedToSlot(Slot).Error);Assert.Equal(ReviewCheckpointError.NoStore,c.ReopenFromSlot(Slot).Error);
        Assert.True(c.LoadCommitted(handle));Assert.NotNull(c.Frame.Demo);
        var foreign=new ReviewController(PhysicalReviewHost.StartDemo(Package(),new Codec()),A);Assert.Throws<ArgumentException>(()=>foreign.LoadCommitted(handle));
    }
    [Fact] public void ReopeningAsForeignActorNeverPublishesCandidate()
    {
        var store=new Store();var codec=new Codec();var host=PhysicalReviewHost.StartDemo(Package(),codec,store);Assert.True(host.SaveCommittedToSlot(Slot).Succeeded);
        var save=JsonSerializer.Serialize(store.Save);var result=PhysicalReviewHost.OpenDemoFromSlot(Package(),codec,store,Slot,"civ.cardassian");
        Assert.Equal(ReviewCheckpointError.InvalidCheckpoint,result.Error);Assert.Null(result.Host);Assert.Null(result.Frame);Assert.Equal(save,JsonSerializer.Serialize(store.Save));
    }
    [Fact] public void PendingExecutionRejectsAnotherControllersWorkWithoutMutation()
    {
        var store=new Store();var host=PhysicalReviewHost.StartDemo(Package(),new Codec(),store);var first=new ReviewController(host,A);Pending(host,first);
        var second=new ReviewController(host,A);Assert.True(second.SelectEncounter(second.Frame.Encounters.Single()));
        Assert.True(first.ReviewRound());var own=first.ConfirmRound("command.demo.host.pending-a")!;
        Assert.True(second.ReviewRound());var other=second.ConfirmRound("command.demo.host.pending-b")!;
        Assert.True(host.SaveCommittedToSlot(Slot).Succeeded);var before=JsonSerializer.Serialize(store.Save);
        Assert.Throws<InvalidOperationException>(()=>first.ExecutePending(other));Assert.True(host.SaveCommittedToSlot(Slot,true).Succeeded);Assert.Equal(before,JsonSerializer.Serialize(store.Save));
        var receipt=first.ExecutePending(own);Assert.True(first.Receive(receipt));Assert.False(first.Receive(receipt));
        Assert.Throws<InvalidOperationException>(()=>first.ExecutePending(own));
    }
    [Fact] public void ObsoletePendingWorkCanRecoverByExplicitLoadWithoutLosingDraftOnFailure()
    {
        var host=PhysicalReviewHost.StartDemo(Package(),new Codec(),new Store());var first=new ReviewController(host,A);Pending(host,first);
        Assert.True(first.EditFleet(first.Frame.Battle!.StandingOrders.First().FleetId,CampaignFleetManeuver.Flank,null));
        var draft=first.Draft.Single();Assert.True(first.ReviewRound());var work=first.ConfirmRound("command.demo.host.stale")!;
        var second=new ReviewController(host,A);Assert.True(second.SelectEncounter(second.Frame.Encounters.Single()));
        Assert.True(second.SaveCommittedToSlot(Slot).Succeeded);Assert.True(second.LoadCommittedFromSlot(Slot).Succeeded);
        Assert.Throws<InvalidOperationException>(()=>first.ExecutePending(work));Assert.False(first.HasPendingWork);Assert.True(first.RefreshRequired);Assert.Same(draft,first.Draft.Single());
        Assert.Equal(ReviewCheckpointError.DirtyDraft,first.LoadCommittedFromSlot(Slot).Error);Assert.Same(draft,first.Draft.Single());
        Assert.True(first.LoadCommittedFromSlot(Slot,true).Succeeded);Assert.False(first.Dirty);Assert.False(first.RefreshRequired);Assert.Equal(1,first.Frame.Battle!.Round);
    }
}
