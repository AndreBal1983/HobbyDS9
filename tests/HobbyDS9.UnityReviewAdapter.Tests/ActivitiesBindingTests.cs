using System.Diagnostics;
using System.Reflection;
using HobbyDS9.Contracts;
using HobbyDS9.TacticalReviewClient;
using HobbyDS9.UnityReview;
using Xunit;

namespace HobbyDS9.UnityReviewAdapter.Tests;

public sealed class ActivitiesBindingTests:IDisposable
{
    const string Human="civ.federation";
    readonly string directory=Path.Combine(Path.GetTempPath(),"HobbyDS9-Activities-Binding-"+Guid.NewGuid().ToString("N"));
    readonly ReviewNewtonsoftCodec codec=new();
    static NaturalGalaxySettingsDto Settings()=>new(){Map=new(){Seed="42",SystemCount=30,EmptyRegionCount=30,Width=750000,Height=600000,
        HomeCivilizationDefinitionIds=new(){"civilization.federation","civilization.cardassian"}}};
    CampaignActivitiesPackageDto Package()
    {
        var template=PlayableFleetPackageReader.Read(Path.Combine(AppContext.BaseDirectory,"scenarios/playable-fleet-v1"));
        return ActivitiesPlaytestOptions.CreatePackage(template,new[]{"catalogue","ready-classes","support","activities"}
            .Select(n=>File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,"scenarios/activities-v1",n+".json"))).ToArray(),codec);
    }
    ReviewActivitiesSlotStore Store(Action<UnityReviewWriteStage>? probe=null)=>new(directory,codec,probe);
    ReviewUiModel Model(Action<UnityReviewWriteStage>? probe=null)=>new(Package(),Settings(),codec,Store(probe));
    string FileFor(string slot)=>Path.Combine(directory,"unity-campaign-activities-v1-"+slot+".json");
    static PhysicalReviewHost Host(ReviewUiModel model)=>(PhysicalReviewHost)typeof(ReviewUiModel).GetField("host",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(model)!;
    static PhysicalReviewHost Host(ReviewController controller)=>(PhysicalReviewHost)typeof(ReviewController).GetField("host",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(controller)!;
    static CampaignActivityCommandDto Queue(ReviewUiModel model)
    {var view=model.Activities();return new(){ActorId=view.ActorId,ExpectedTurn=view.Turn,ExpectedRevision=view.Revision,CommandId=ReviewUiModel.NewCommandId(),
        Kind=CampaignActivityCommandKind.EnqueueShip,TargetId=view.Colonies.Single(c=>c.ColonyId=="colony.sol").ColonyId,DefinitionId="blueprint.ready.constitution-explorer",BlueprintRevision=1};}

    [Fact] public void ActualTypedModelAdvisoryRedrawDoesNotInvalidateCapturedPaidCommand()
    {
        var model=Model();var view=model.Activities();Assert.Equal(3,view.BuildOptions.Count(o=>o.Kind==ActivityProductionKind.PublishedShip));
        Assert.All(view.BuildOptions.Where(o=>o.Kind==ActivityProductionKind.PublishedShip),o=>Assert.True(o.Supported));
        var command=Queue(model);var before=model.Current.Frame;var money=before.Strategy!.Civilization.Credits;
        Assert.True(model.ActivityEligibility(command).Accepted);model.Activities();Assert.True(model.ActivityEligibility(command).Accepted);Assert.Same(before,model.Current.Frame);
        var draft=model.ResearchInputs()!;draft.ScienceUnits=draft.ScienceUnits==600?300:600;Assert.True(draft.Dirty);
        Assert.True(model.SubmitActivity(command));Assert.Equal(money-74,model.Current.Frame.Strategy!.Civilization.Credits);
        Assert.True(model.ResearchInputs()!.Dirty);Assert.Equal(draft.ScienceUnits,model.ResearchInputs()!.ScienceUnits);
        Assert.Single(model.Activities().Production);var paid=model.Activities().Production[0].Paid!;Assert.Equal(1,paid.Blueprint!.Revision);
        Assert.False(model.SubmitActivity(command));Assert.Equal(money-74,model.Current.Frame.Strategy!.Civilization.Credits);Assert.Single(model.Activities().Production);
    }
    [Fact] public void TypedContinueAndInterfaceReopenPreserveFullBytesAndInvalidateOldAuthority()
    {
        var model=Model();Assert.True(model.SubmitActivity(Queue(model)));Assert.True(model.Save("one"));var bytes=File.ReadAllBytes(FileFor("one"));
        var prior=model.Current;var presentation=model.GeneratedPresentation();Assert.True(model.Load("one",true));Assert.NotSame(prior,model.Current);Assert.False(model.IsCurrentGeneratedPresentation(presentation));
        Assert.True(model.Save("two"));Assert.Equal(bytes,File.ReadAllBytes(FileFor("two")));
        var old=Host(model.Current);var handle=old.SaveCommitted();var controller=model.Current;Assert.True(controller.ReopenFromSlot("one").Succeeded);
        Assert.NotSame(old,Host(controller));Assert.Throws<ArgumentException>(()=>Host(controller).LoadCommitted(handle,Human,null));
        Assert.True(controller.SaveCommittedToSlot("three").Succeeded);Assert.Equal(bytes,File.ReadAllBytes(FileFor("three")));
    }
    [Fact] public async Task HeldActivitiesWriterLeavesPresentationReadableAndPreservesDraftAndNewLease()
    {
        using var entered=new ManualResetEventSlim();using var release=new ManualResetEventSlim();
        var model=Model(stage=>{if(stage==UnityReviewWriteStage.AfterPartialWrite){entered.Set();if(!release.Wait(TimeSpan.FromSeconds(10)))throw new TimeoutException();}});
        Assert.True(model.SubmitActivity(Queue(model)));var frame=model.Current.Frame;var pair=model.GeneratedPresentation();var draft=model.ResearchInputs()!;
        draft.ScienceUnits=draft.ScienceUnits==600?300:600;model.LocalDesignDirty=true;
        var op=model.BeginStaticCheckpointOperation("held",false)!;Assert.NotNull(op);var worker=Task.Run(op.Execute);
        try
        {
            Assert.True(entered.Wait(TimeSpan.FromSeconds(5)));
            await Task.Run(()=>{Host(model).Read(Human,null);Assert.True(model.IsCurrentGeneratedPresentation(pair));}).WaitAsync(TimeSpan.FromSeconds(5));
            Assert.True(model.TryCheckWriteAdmission("held",out var error));Assert.Equal(ReviewCheckpointError.Busy,error);
            Assert.Null(model.BeginStaticCheckpointOperation("other",false));Assert.False(model.SubmitActivity(QueueFrom(frame)));
            model.AbandonStaticCheckpointOperation(op);var next=model.BeginStaticCheckpointOperation("new",false)!;Assert.NotNull(next);
            release.Set();var done=await worker.WaitAsync(TimeSpan.FromSeconds(10));Assert.False(model.CompleteStaticCheckpointOperation(op,done));Assert.True(model.Busy);
            model.AbandonStaticCheckpointOperation(next);Assert.False(model.Busy);Assert.Same(frame,model.Current.Frame);Assert.Same(draft,model.ResearchInputs());Assert.True(model.LocalDesignDirty);
        }
        finally{release.Set();await worker.WaitAsync(TimeSpan.FromSeconds(10));}
    }
    static CampaignActivityCommandDto QueueFrom(ReviewFrame frame)=>new(){ActorId=frame.Actor,ExpectedTurn=frame.Status.Turn,ExpectedRevision=frame.Status.CampaignRevision,
        CommandId=ReviewUiModel.NewCommandId(),Kind=CampaignActivityCommandKind.EnqueueShip,TargetId="colony.sol",DefinitionId="blueprint.ready.constitution-explorer",BlueprintRevision=1};
    [Fact] public void FailedWorkerLoadCannotPublishCarrierAloneOrDiscardDrafts()
    {
        var model=Model();Assert.True(model.Save("bad"));var root=Store().Read("bad");root.Campaign!.Integrated!.Physical!.Core.Campaign.GameState.Civilizations![0].Credits++;
        Store().Write("bad",root,true);var frame=model.Current.Frame;var draft=model.ResearchInputs()!;draft.ScienceUnits=draft.ScienceUnits==600?300:600;
        var op=model.BeginStaticCheckpointOperation("bad",true,discard:true)!;Assert.NotNull(op);var done=op.Execute();Assert.False(model.CompleteStaticCheckpointOperation(op,done));
        Assert.Same(frame,model.Current.Frame);Assert.Same(draft,model.ResearchInputs());Assert.True(draft.Dirty);Assert.False(model.Busy);
    }
    public void Dispose()
    {
        if(!Directory.Exists(directory))return;var full=Path.GetFullPath(directory);var parent=Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar)+Path.DirectorySeparatorChar;
        if(!full.StartsWith(parent,StringComparison.OrdinalIgnoreCase)||!Path.GetFileName(full).StartsWith("HobbyDS9-Activities-Binding-",StringComparison.Ordinal))throw new InvalidOperationException("Test cleanup boundary.");
        foreach(var file in Directory.GetFiles(full))File.Delete(file);Directory.Delete(full,false);
    }
}
