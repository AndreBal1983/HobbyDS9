using System.Reflection;
using HobbyDS9.Application;
using HobbyDS9.Contracts;
using HobbyDS9.TacticalReviewClient;
using HobbyDS9.UnityReview;
using Xunit;

namespace HobbyDS9.UnityReviewAdapter.Tests;

public sealed class EngineeringRefinementHostTests:IDisposable
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
        return refinement?CampaignColonisationCycleApplication.CreateEngineeringRefinementPackage(growth):CampaignColonisationCycleApplication.CreateAuthoredSolPackage(growth);
    }
    static NaturalGalaxySettingsDto Settings()=>new(){Map=new(){Seed="42",SystemCount=30,EmptyRegionCount=30,Width=750000,Height=600000,HomeCivilizationDefinitionIds=new(){"civilization.federation","civilization.cardassian"}}};
    ReviewUiModel Model(bool refinement=false)=>new(Package(refinement),Settings(),codec,new ReviewColonisationCycleSlotStore(directory,codec));
    static PhysicalReviewHost Host(ReviewUiModel m)=>(PhysicalReviewHost)typeof(ReviewUiModel).GetField("host",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(m)!;
    static ICampaignColonisationCycleSession Session(ReviewUiModel m)=>(ICampaignColonisationCycleSession)typeof(PhysicalReviewHost).GetField("session",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(Host(m))!;
    byte[] Wire(ReviewUiModel m)=>codec.EncodeCycleRoot(Session(m).ExportCycleSave());
    static EngineeringRefinementCommandDto Command(ReviewFrame frame)=>new(){ActorId=frame.Actor,CommandId="command.refinement.host",ExpectedTurn=frame.Status.Turn,ExpectedRevision=frame.Status.CampaignRevision,
        Action=EngineeringRefinementAction.Start,ParentVersionId="component.reactor.v1",Goal=EngineeringRefinementGoal.ReduceCapacity,ContextBlueprintId="blueprint.ready.constitution-explorer",ContextBlueprintRevision=1};

    [Fact] public async Task NewEditionPublishesAcceptedCandidateOnlyWithMatchingControllerUpdate()
    {
        var m=new ReviewUiModel(Package(true),Settings(),codec,new ReviewColonisationCycleSlotStore(directory,codec));var host=Host(m);var frame=m.Current.Frame;
        var original=Session(m);var before=Wire(m);Assert.True(host.SupportsEngineeringRefinement);Assert.NotNull(host.CaptureEngineeringRefinement(frame));
        var preview=host.PrepareEngineeringRefinementInteraction(frame,EngineeringRefinementInteractionKind.Preview,Command(frame));
        var checkedCommand=await Task.Run(preview.Execute);Assert.False(checkedCommand.Failed);Assert.True(checkedCommand.Preview!.CanStart,checkedCommand.Preview.Error.ToString());
        Assert.True(host.CompleteEngineeringRefinementInteraction(preview,checkedCommand));Assert.Equal(before,Wire(m));
        var work=host.PrepareEngineeringRefinementInteraction(frame,EngineeringRefinementInteractionKind.Submission,Command(frame));var completion=await Task.Run(work.Execute);
        Assert.False(completion.Failed);Assert.True(completion.Result!.Accepted,completion.Result.Error.ToString());Assert.NotNull(completion.PreparedRefinement);
        Assert.Same(original,Session(m));Assert.Same(frame,m.Current.Frame);Assert.Equal(before,Wire(m));
        Assert.False(host.CompleteEngineeringRefinementInteraction(work,completion));
        var update=m.Current.PrepareEngineeringRefinementInteractionReceive(completion);Assert.True(host.CompleteEngineeringRefinementInteraction(work,completion,update));
        Assert.True(completion.Committed);Assert.NotSame(original,Session(m));Assert.NotSame(frame,m.Current.Frame);Assert.NotEqual(before,Wire(m));
        Assert.Equal(before,codec.EncodeCycleRoot(original.ExportCycleSave()));Assert.False(host.CompleteEngineeringRefinementInteraction(work,completion,update));
        Assert.Single(host.CaptureEngineeringRefinement(m.Current.Frame)!.Projects,p=>p.ProjectId==completion.Result.ProjectId);
        Assert.Throws<InvalidOperationException>(()=>host.CaptureEngineeringRefinement(frame));
    }

    [Fact] public async Task OlderEditionIsUnavailableAndWorkerOwnsClonedCommandAndExclusiveLease()
    {
        var m=Model();var host=Host(m);var frame=m.Current.Frame;var before=Wire(m);
        Assert.False(host.SupportsEngineeringRefinement);var optional=Assert.IsAssignableFrom<ICampaignEngineeringRefinementSession>(Session(m));
        Assert.Null(optional.CaptureEngineeringRefinement(frame.Actor));Assert.Null(host.CaptureEngineeringRefinement(frame));
        var command=Command(frame);var work=host.PrepareEngineeringRefinementInteraction(frame,EngineeringRefinementInteractionKind.Preview,command);
        command.ActorId="civ.cardassian";command.Goal=EngineeringRefinementGoal.IncreasePowerSupply;command.ParentVersionId="component.fake.v1";
        var copy=work.Command;copy.ProjectId="project.fake";Assert.Null(work.Command.ProjectId);
        Assert.Equal(frame.Actor,work.Command.ActorId);Assert.Equal(EngineeringRefinementGoal.ReduceCapacity,work.Command.Goal);Assert.Equal("component.reactor.v1",work.Command.ParentVersionId);
        Assert.Throws<InvalidOperationException>(()=>host.Read(frame.Actor));
        Assert.Throws<InvalidOperationException>(()=>host.PrepareEngineeringRefinementInteraction(frame,EngineeringRefinementInteractionKind.Submission,Command(frame)));
        var completion=await Task.Run(work.Execute);Assert.False(completion.Failed);Assert.Equal(EngineeringRefinementError.Unavailable,completion.Preview!.Error);
        Assert.False(completion.Committed);Assert.Null(completion.PreparedRefinement);Assert.True(host.CompleteEngineeringRefinementInteraction(work,completion));
        Assert.Throws<InvalidOperationException>(()=>work.Execute());Assert.False(host.CompleteEngineeringRefinementInteraction(work,completion));Assert.Equal(before,Wire(m));
        var submission=host.PrepareEngineeringRefinementInteraction(frame,EngineeringRefinementInteractionKind.Submission,Command(frame));
        var rejected=await Task.Run(submission.Execute);Assert.False(rejected.Failed);Assert.Equal(EngineeringRefinementError.Unavailable,rejected.Result!.Error);
        Assert.True(host.CompleteEngineeringRefinementInteraction(submission,rejected));Assert.False(rejected.Committed);Assert.Same(frame,m.Current.Frame);Assert.Equal(before,Wire(m));
    }

    [Fact] public async Task CancelledRunningWorkerKeepsLeaseUntilDrainedAndCannotClearNewWork()
    {
        var m=Model();var host=Host(m);var frame=m.Current.Frame;var before=Wire(m);
        using var entered=new ManualResetEventSlim();using var release=new ManualResetEventSlim();
        var probe=typeof(PhysicalReviewHost).GetProperty("EngineeringRefinementInteractionProbe",BindingFlags.Instance|BindingFlags.NonPublic)!;
        probe.SetValue(host,(Action<string>)(phase=>{if(phase=="before-fork"){entered.Set();if(!release.Wait(TimeSpan.FromSeconds(15)))throw new TimeoutException();}}));
        var old=host.PrepareEngineeringRefinementInteraction(frame,EngineeringRefinementInteractionKind.Preview,Command(frame));var task=Task.Run(old.Execute);
        try
        {
            Assert.True(entered.Wait(TimeSpan.FromSeconds(5)));host.AbandonEngineeringRefinementInteraction(old);Assert.True(old.Cancelled);Assert.False(old.Settled);
            Assert.Throws<InvalidOperationException>(()=>host.Read(frame.Actor));
            release.Set();var done=await task.WaitAsync(TimeSpan.FromSeconds(20));Assert.True(old.Settled);Assert.False(host.CompleteEngineeringRefinementInteraction(old,done));
            Assert.Equal(before,Wire(m));probe.SetValue(host,null);
            var newer=host.PrepareEngineeringRefinementInteraction(frame,EngineeringRefinementInteractionKind.Preview,Command(frame));host.AbandonEngineeringRefinementInteraction(old);
            Assert.Throws<InvalidOperationException>(()=>host.Read(frame.Actor));Assert.True(host.CompleteEngineeringRefinementInteraction(newer,await Task.Run(newer.Execute)));Assert.Equal(before,Wire(m));
        }
        finally{release.Set();await task.WaitAsync(TimeSpan.FromSeconds(20));probe.SetValue(host,null);host.AbandonEngineeringRefinementInteraction(old);}
    }

    [Fact] public void AdmissionRejectsForeignActorStaleCommandsAndStaleObservation()
    {
        var m=Model();var host=Host(m);var frame=m.Current.Frame;
        foreach(var mutate in new Action<EngineeringRefinementCommandDto>[]{c=>c.ActorId="civ.cardassian",c=>c.ExpectedTurn++,c=>c.ExpectedRevision++})
        {var command=Command(frame);mutate(command);Assert.Throws<InvalidOperationException>(()=>host.PrepareEngineeringRefinementInteraction(frame,EngineeringRefinementInteractionKind.Preview,command));}
        Assert.Throws<InvalidOperationException>(()=>host.PrepareEngineeringRefinementInteraction(frame,(EngineeringRefinementInteractionKind)99,Command(frame)));
        var foreign=Model();Assert.Throws<InvalidOperationException>(()=>host.PrepareEngineeringRefinementInteraction(foreign.Current.Frame,EngineeringRefinementInteractionKind.Preview,Command(foreign.Current.Frame)));
        Assert.True(Session(m).SubmitColonisation(new(){ActorId=frame.Actor,CommandId="command.refinement.stale",Turn=frame.Status.Turn,Revision=frame.Status.CampaignRevision,
            Action=ColonisationAction.EnqueueKit,EnqueueKit=new(){ColonyId="colony.sol"}}).Accepted);
        Assert.Throws<InvalidOperationException>(()=>host.CaptureEngineeringRefinement(frame));
        Assert.Throws<InvalidOperationException>(()=>host.PrepareEngineeringRefinementInteraction(frame,EngineeringRefinementInteractionKind.Preview,Command(frame)));
    }
    [Fact] public async Task ModelPreviewAndSubmissionPreserveDraftsAndPublishOnlyOnce()
    {
        var m=Model(true);var frame=m.Current.Frame;var before=Wire(m);
        Assert.True(m.SupportsEngineeringRefinement);Assert.NotNull(m.CaptureEngineeringRefinement(frame.Actor));
        Assert.Null(m.CaptureEngineeringRefinement("civ.cardassian"));
        var draft=m.ResearchInputs()!;draft.SetWeight(ScienceDiscipline.Energy,700);
        var weights=draft.Values().Select(v=>v.Weight).ToArray();Assert.True(draft.Dirty);
        var preview=m.BeginEngineeringRefinementInteraction(EngineeringRefinementInteractionKind.Preview,Command(frame))!;
        Assert.NotNull(preview);Assert.True(m.Busy);Assert.True(m.SupportsEngineeringRefinement);
        Assert.Null(m.CaptureEngineeringRefinement(frame.Actor));
        var checkedGoal=await Task.Run(preview.Execute);Assert.True(checkedGoal.Preview!.CanStart);
        Assert.True(m.CompleteEngineeringRefinementInteraction(preview,checkedGoal),m.Message);
        Assert.False(m.Busy);Assert.Same(frame,m.Current.Frame);Assert.Equal(before,Wire(m));
        var work=m.BeginEngineeringRefinementInteraction(EngineeringRefinementInteractionKind.Submission,Command(frame))!;
        var completion=await Task.Run(work.Execute);Assert.True(completion.Result!.Accepted);
        Assert.True(m.CompleteEngineeringRefinementInteraction(work,completion),m.Message);
        Assert.True(completion.Committed);Assert.False(m.Busy);Assert.NotSame(frame,m.Current.Frame);
        Assert.True(m.ResearchDraft!.Dirty);Assert.True(m.ResearchDraft.Current(m.Current.Frame));
        Assert.Equal(weights,m.ResearchDraft.Values().Select(v=>v.Weight));
        Assert.False(m.CompleteEngineeringRefinementInteraction(work,completion));
        Assert.Single(m.CaptureEngineeringRefinement(m.Current.Frame.Actor)!.Projects);
        Assert.Null(m.BeginEngineeringRefinementInteraction(EngineeringRefinementInteractionKind.Submission,Command(frame)));
        Assert.False(m.Busy);
    }
    [Fact] public async Task ModelCancellationRetainsBusyUntilWorkerDrainsAndCannotPublish()
    {
        var m=Model(true);var host=Host(m);var frame=m.Current.Frame;var before=Wire(m);
        using var entered=new ManualResetEventSlim();using var release=new ManualResetEventSlim();
        var probe=typeof(PhysicalReviewHost).GetProperty("EngineeringRefinementInteractionProbe",BindingFlags.Instance|BindingFlags.NonPublic)!;
        probe.SetValue(host,(Action<string>)(phase=>{if(phase=="before-fork"){entered.Set();if(!release.Wait(TimeSpan.FromSeconds(15)))throw new TimeoutException();}}));
        var work=m.BeginEngineeringRefinementInteraction(EngineeringRefinementInteractionKind.Submission,Command(frame))!;
        var task=Task.Run(work.Execute);
        try
        {
            Assert.True(entered.Wait(TimeSpan.FromSeconds(5)));m.AbandonEngineeringRefinementInteraction(work);
            Assert.True(m.Busy);Assert.Null(m.BeginEngineeringRefinementInteraction(EngineeringRefinementInteractionKind.Preview,Command(frame)));
            release.Set();var completion=await task.WaitAsync(TimeSpan.FromSeconds(20));
            Assert.False(m.CompleteEngineeringRefinementInteraction(work,completion));
            Assert.False(m.Busy);Assert.False(completion.Committed);Assert.Same(frame,m.Current.Frame);Assert.Equal(before,Wire(m));
        }
        finally{release.Set();await task.WaitAsync(TimeSpan.FromSeconds(20));probe.SetValue(host,null);m.AbandonEngineeringRefinementInteraction(work);}
    }
    [Fact] public void ModelKeepsOlderEditionUnavailableWithoutStartingWork()
    {
        var m=Model();var frame=m.Current.Frame;var before=Wire(m);
        Assert.False(m.SupportsEngineeringRefinement);Assert.Null(m.CaptureEngineeringRefinement(frame.Actor));
        Assert.Null(m.BeginEngineeringRefinementInteraction(EngineeringRefinementInteractionKind.Submission,Command(frame)));
        Assert.False(m.Busy);Assert.Same(frame,m.Current.Frame);Assert.Equal(before,Wire(m));
    }
    public void Dispose(){if(Directory.Exists(directory))Directory.Delete(directory,true);}
}
