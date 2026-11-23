using System.Reflection;
using HobbyDS9.Application;
using HobbyDS9.Contracts;
using HobbyDS9.TacticalReviewClient;
using HobbyDS9.UnityReview;
using Xunit;

namespace HobbyDS9.UnityReviewAdapter.Tests;

public sealed class BeltBodyHostTests:IDisposable
{
    readonly string directory=Path.Combine(Path.GetTempPath(),"HobbyDS9-Refinement-Host-"+Guid.NewGuid().ToString("N"));
    readonly ReviewNewtonsoftCodec codec=new();
    static byte[] Read(string folder,string name)=>File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,"scenarios",folder,name+".json"));
    CampaignColonisationCyclePackageDto Package(bool refinement=false)
    {
        var template=ReviewExactPackageLoader.LoadPlayableFleet(Read("playable-fleet-v1","playable"),Read("playable-fleet-v1","package"),Read("playable-fleet-v1","physical"),Read("playable-fleet-v1","core"),Read("sol-eight-planets-v1","scenario"),codec);
        var activities=ActivitiesPlaytestOptions.CreatePackage(template,new[]{"catalogue","ready-classes","support","activities"}.Select(n=>Read("activities-v1",n)).ToArray(),codec);
        var colony=ColonisationPlaytestOptions.CreatePackage(activities,new[]{"catalogue","ready-classes","support","settlement"}.Select(n=>Read("colonisation-v1",n)).ToArray(),codec);
        var roster=ColonisationPlaytestOptions.CreateRosterPackage(colony,Read("colonisation-roster-v2","roster"),codec);
        var growth=ColonisationGrowthPlaytestOptions.CreatePackage(roster,Read("colonisation-expansion-v1","planner"),codec);
        return refinement?CampaignColonisationCycleApplication.CreateBeltBodiesPackage(growth):CampaignColonisationCycleApplication.CreateAuthoredSolPackage(growth);
    }
    static NaturalGalaxySettingsDto Settings()=>new(){Map=new(){Seed="42",SystemCount=30,EmptyRegionCount=30,Width=750000,Height=600000,HomeCivilizationDefinitionIds=new(){"civilization.federation","civilization.cardassian"}}};
    ReviewUiModel Model(bool refinement=false)=>new(Package(refinement),Settings(),codec,new ReviewColonisationCycleSlotStore(directory,codec));
    static PhysicalReviewHost Host(ReviewUiModel m)=>(PhysicalReviewHost)typeof(ReviewUiModel).GetField("host",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(m)!;
    static ICampaignColonisationCycleSession Session(ReviewUiModel m)=>(ICampaignColonisationCycleSession)typeof(PhysicalReviewHost).GetField("session",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(Host(m))!;
    byte[] Wire(ReviewUiModel m)=>codec.EncodeCycleRoot(Session(m).ExportCycleSave());
    static BeltBodyCommandDto Command(ReviewFrame frame)=>new(){ActorId=frame.Actor,CommandId="command.belt.host",ExpectedTurn=frame.Status.Turn,ExpectedRevision=frame.Status.CampaignRevision,
        Action=BeltBodyAction.StartSurvey,BodyId="body."+CampaignAuthoredSolIdentity.FeatureId,ShipId=frame.OwnShips.First().Condition.ShipId};

    [Fact] public async Task NewEditionPublishesAcceptedCandidateOnlyWithMatchingControllerUpdate()
    {
        var m=new ReviewUiModel(Package(true),Settings(),codec,new ReviewColonisationCycleSlotStore(directory,codec));var host=Host(m);var frame=m.Current.Frame;
        var original=Session(m);var before=Wire(m);Assert.True(host.SupportsBeltBodies);Assert.True(host.SupportsEngineeringRefinement);Assert.NotNull(host.CaptureBeltBodies(frame));
        var preview=host.PrepareBeltBodyInteraction(frame,BeltBodyInteractionKind.Preview,Command(frame));
        var checkedCommand=await Task.Run(preview.Execute);Assert.False(checkedCommand.Failed);Assert.True(checkedCommand.Preview!.CanSubmit,checkedCommand.Preview.Error.ToString());
        Assert.True(host.CompleteBeltBodyInteraction(preview,checkedCommand));Assert.Equal(before,Wire(m));
        var work=host.PrepareBeltBodyInteraction(frame,BeltBodyInteractionKind.Submission,Command(frame));var completion=await Task.Run(work.Execute);
        Assert.False(completion.Failed);Assert.True(completion.Result!.Accepted,completion.Result.Error.ToString());Assert.NotNull(completion.PreparedBeltBodies);
        Assert.Same(original,Session(m));Assert.Same(frame,m.Current.Frame);Assert.Equal(before,Wire(m));
        Assert.False(host.CompleteBeltBodyInteraction(work,completion));
        var update=m.Current.PrepareBeltBodyInteractionReceive(completion);Assert.True(host.CompleteBeltBodyInteraction(work,completion,update));
        Assert.True(completion.Committed);Assert.NotSame(original,Session(m));Assert.NotSame(frame,m.Current.Frame);Assert.NotEqual(before,Wire(m));
        Assert.Equal(before,codec.EncodeCycleRoot(original.ExportCycleSave()));Assert.False(host.CompleteBeltBodyInteraction(work,completion,update));
        Assert.Single(host.CaptureBeltBodies(m.Current.Frame)!.Bodies,p=>p.ActiveSurveyId==completion.Result.TargetId);
        Assert.Throws<InvalidOperationException>(()=>host.CaptureBeltBodies(frame));
    }

    [Fact] public async Task OlderEditionIsUnavailableAndWorkerOwnsClonedCommandAndExclusiveLease()
    {
        var m=Model();var host=Host(m);var frame=m.Current.Frame;var before=Wire(m);
        Assert.False(host.SupportsBeltBodies);var optional=Assert.IsAssignableFrom<ICampaignBeltBodiesSession>(Session(m));
        Assert.Null(optional.CaptureBeltBodies(frame.Actor));Assert.Null(host.CaptureBeltBodies(frame));
        var command=Command(frame);var work=host.PrepareBeltBodyInteraction(frame,BeltBodyInteractionKind.Preview,command);
        command.ActorId="civ.cardassian";command.Action=BeltBodyAction.EnqueueMine;command.BodyId="body.fake";
        var copy=work.Command;copy.MineId="mine.fake";Assert.Null(work.Command.MineId);
        Assert.Equal(frame.Actor,work.Command.ActorId);Assert.Equal(BeltBodyAction.StartSurvey,work.Command.Action);Assert.Equal("body."+CampaignAuthoredSolIdentity.FeatureId,work.Command.BodyId);
        Assert.Throws<InvalidOperationException>(()=>host.Read(frame.Actor));
        Assert.Throws<InvalidOperationException>(()=>host.PrepareBeltBodyInteraction(frame,BeltBodyInteractionKind.Submission,Command(frame)));
        var completion=await Task.Run(work.Execute);Assert.False(completion.Failed);Assert.Equal(BeltBodyError.Unavailable,completion.Preview!.Error);
        Assert.False(completion.Committed);Assert.Null(completion.PreparedBeltBodies);Assert.True(host.CompleteBeltBodyInteraction(work,completion));
        Assert.Throws<InvalidOperationException>(()=>work.Execute());Assert.False(host.CompleteBeltBodyInteraction(work,completion));Assert.Equal(before,Wire(m));
        var submission=host.PrepareBeltBodyInteraction(frame,BeltBodyInteractionKind.Submission,Command(frame));
        var rejected=await Task.Run(submission.Execute);Assert.False(rejected.Failed);Assert.Equal(BeltBodyError.Unavailable,rejected.Result!.Error);
        Assert.True(host.CompleteBeltBodyInteraction(submission,rejected));Assert.False(rejected.Committed);Assert.Same(frame,m.Current.Frame);Assert.Equal(before,Wire(m));
    }

    [Fact] public async Task CancelledRunningWorkerKeepsLeaseUntilDrainedAndCannotClearNewWork()
    {
        var m=Model();var host=Host(m);var frame=m.Current.Frame;var before=Wire(m);
        using var entered=new ManualResetEventSlim();using var release=new ManualResetEventSlim();
        var probe=typeof(PhysicalReviewHost).GetProperty("BeltBodyInteractionProbe",BindingFlags.Instance|BindingFlags.NonPublic)!;
        probe.SetValue(host,(Action<string>)(phase=>{if(phase=="before-fork"){entered.Set();if(!release.Wait(TimeSpan.FromSeconds(15)))throw new TimeoutException();}}));
        var old=host.PrepareBeltBodyInteraction(frame,BeltBodyInteractionKind.Preview,Command(frame));var task=Task.Run(old.Execute);
        try
        {
            Assert.True(entered.Wait(TimeSpan.FromSeconds(5)));host.AbandonBeltBodyInteraction(old);Assert.True(old.Cancelled);Assert.False(old.Settled);
            Assert.Throws<InvalidOperationException>(()=>host.Read(frame.Actor));
            release.Set();var done=await task.WaitAsync(TimeSpan.FromSeconds(20));Assert.True(old.Settled);Assert.False(host.CompleteBeltBodyInteraction(old,done));
            Assert.Equal(before,Wire(m));probe.SetValue(host,null);
            var newer=host.PrepareBeltBodyInteraction(frame,BeltBodyInteractionKind.Preview,Command(frame));host.AbandonBeltBodyInteraction(old);
            Assert.Throws<InvalidOperationException>(()=>host.Read(frame.Actor));Assert.True(host.CompleteBeltBodyInteraction(newer,await Task.Run(newer.Execute)));Assert.Equal(before,Wire(m));
        }
        finally{release.Set();await task.WaitAsync(TimeSpan.FromSeconds(20));probe.SetValue(host,null);host.AbandonBeltBodyInteraction(old);}
    }

    [Fact] public void AdmissionRejectsForeignActorStaleCommandsAndStaleObservation()
    {
        var m=Model();var host=Host(m);var frame=m.Current.Frame;
        foreach(var mutate in new Action<BeltBodyCommandDto>[]{c=>c.ActorId="civ.cardassian",c=>c.ExpectedTurn++,c=>c.ExpectedRevision++})
        {var command=Command(frame);mutate(command);Assert.Throws<InvalidOperationException>(()=>host.PrepareBeltBodyInteraction(frame,BeltBodyInteractionKind.Preview,command));}
        Assert.Throws<InvalidOperationException>(()=>host.PrepareBeltBodyInteraction(frame,(BeltBodyInteractionKind)99,Command(frame)));
        var foreign=Model();Assert.Throws<InvalidOperationException>(()=>host.PrepareBeltBodyInteraction(foreign.Current.Frame,BeltBodyInteractionKind.Preview,Command(foreign.Current.Frame)));
        Assert.True(Session(m).SubmitColonisation(new(){ActorId=frame.Actor,CommandId="command.refinement.stale",Turn=frame.Status.Turn,Revision=frame.Status.CampaignRevision,
            Action=ColonisationAction.EnqueueKit,EnqueueKit=new(){ColonyId="colony.sol"}}).Accepted);
        Assert.Throws<InvalidOperationException>(()=>host.CaptureBeltBodies(frame));
        Assert.Throws<InvalidOperationException>(()=>host.PrepareBeltBodyInteraction(frame,BeltBodyInteractionKind.Preview,Command(frame)));
    }
    public void Dispose(){if(Directory.Exists(directory))Directory.Delete(directory,true);}
}
