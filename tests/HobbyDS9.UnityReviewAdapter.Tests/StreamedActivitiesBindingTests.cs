using System.Reflection;
using HobbyDS9.Application;
using HobbyDS9.Contracts;
using HobbyDS9.TacticalReviewClient;
using HobbyDS9.UnityReview;
using Xunit;

namespace HobbyDS9.UnityReviewAdapter.Tests;

public sealed class StreamedActivitiesBindingTests:IDisposable
{
    const string Human="civ.federation";
    readonly string directory=Path.Combine(Path.GetTempPath(),"HobbyDS9-Streamed-Binding-"+Guid.NewGuid().ToString("N"));
    readonly ReviewNewtonsoftCodec codec=new();
    CampaignStreamedActivitiesPackageDto Package()
    {
        var template=PlayableFleetPackageReader.Read(Path.Combine(AppContext.BaseDirectory,"scenarios/playable-fleet-v1"));
        return CampaignStreamedActivitiesApplication.CreatePackage(ActivitiesPlaytestOptions.CreatePackage(template,
            new[]{"catalogue","ready-classes","support","activities"}.Select(n=>File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,"scenarios/activities-v1",n+".json"))).ToArray(),codec));
    }
    static NaturalGalaxySettingsDto Settings()=>new(){Map=new(){Seed="42",SystemCount=30,EmptyRegionCount=30,Width=750000,Height=600000,HomeCivilizationDefinitionIds=new(){"civilization.federation","civilization.cardassian"}}};
    ReviewStreamedActivitiesSlotStore Store(Action<UnityReviewWriteStage>? probe=null)=>new(directory,codec,probe);
    ReviewUiModel Model(Action<UnityReviewWriteStage>? probe=null)=>new(Package(),Settings(),codec,Store(probe));
    static PhysicalReviewHost Host(ReviewUiModel m)=>(PhysicalReviewHost)typeof(ReviewUiModel).GetField("host",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(m)!;
    static PhysicalReviewHost Host(ReviewController m)=>(PhysicalReviewHost)typeof(ReviewController).GetField("host",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(m)!;
    static CampaignActivityCommandDto Queue(ReviewUiModel m)
    {var o=m.Activities();return new(){ActorId=o.ActorId,ExpectedTurn=o.Turn,ExpectedRevision=o.Revision,CommandId=ReviewUiModel.NewCommandId(),Kind=CampaignActivityCommandKind.EnqueueShip,TargetId="colony.sol",DefinitionId="blueprint.ready.constitution-explorer",BlueprintRevision=1};}
    [Fact] public void CurrentAdvisoryAndSubmissionKeepZeroChargeAndResearchDraftWhileRepeatedOfferCannotDuplicate()
    {
        var model=Model();var money=model.Current.Frame.Strategy!.Civilization.Credits;var draft=model.ResearchInputs()!;draft.ScienceUnits=draft.ScienceUnits==600?300:600;
        var command=Queue(model);Assert.True(model.ActivityEligibility(command).Accepted);Assert.True(model.ActivityEligibility(command).Accepted);
        Assert.True(model.SubmitActivity(command),model.Message);Assert.Equal(money,model.Current.Frame.Strategy!.Civilization.Credits);
        Assert.Single(model.StreamedActivities().Production);Assert.True(model.ResearchInputs()!.Dirty);
        Assert.False(model.SubmitActivity(command));Assert.Single(model.StreamedActivities().Production);
        var observation=model.StreamedActivities();observation.Supply[0].Stocks.Clear();observation.Production[0].ConsumedResources!.Clear();
        Assert.NotEmpty(model.StreamedActivities().Supply[0].Stocks);Assert.NotEmpty(model.StreamedActivities().Production[0].ConsumedResources!);
    }
    [Fact] public void TypedContinueAndInterfaceReopenRestoreExactStreamedRootAndInvalidateOldHandle()
    {
        var model=Model();Assert.True(model.SubmitActivity(Queue(model)));Assert.True(model.Advance(),model.Message);
        var old=model.Current.SaveCommitted();Assert.True(model.Save("slot",false),model.Message);var root=Store().Read("slot");var bytes=codec.EncodeStreamedActivitiesRoot(root);
        var next=ReviewUiModel.ContinueStreamedActivities(Package(),codec,Store(),"slot");Assert.True(next.StreamedActivitiesCampaign);Assert.True(next.Save("second",false));
        Assert.Equal(bytes,codec.EncodeStreamedActivitiesRoot(Store().Read("second")));
        var pair=next.GeneratedPresentation();var frame=next.Current.Frame;Assert.True(next.Load("slot",true));Assert.False(next.IsCurrentGeneratedPresentation(pair));Assert.NotSame(frame,next.Current.Frame);
        Assert.True(next.Current.ReopenFromSlot("slot").Succeeded);Assert.Throws<ArgumentException>(()=>Host(next.Current).LoadCommitted(old,Human,null));
    }
    [Fact] public async Task HeldTypedWorkerKeepsPresentationReadableAndStaleCompletionCannotClearNewLease()
    {
        using var entered=new ManualResetEventSlim();using var release=new ManualResetEventSlim();
        var model=Model(stage=>{if(stage==UnityReviewWriteStage.AfterPartialWrite){entered.Set();release.Wait(TimeSpan.FromSeconds(20));}});
        var frame=model.Current.Frame;var pair=model.GeneratedPresentation();var op=model.BeginStaticCheckpointOperation("held",false)!;Assert.NotNull(op);var worker=Task.Run(op.Execute);
        try
        {
            Assert.True(entered.Wait(TimeSpan.FromSeconds(5)));await Task.Run(()=>{Host(model).Read(Human,null);Assert.True(model.IsCurrentGeneratedPresentation(pair));}).WaitAsync(TimeSpan.FromSeconds(5));
            Assert.Null(model.BeginStaticCheckpointOperation("duplicate",false));Assert.True(model.TryCheckWriteAdmission("other",out var error));Assert.Equal(ReviewCheckpointError.Busy,error);
            model.AbandonStaticCheckpointOperation(op);var newer=model.BeginStaticCheckpointOperation("next",false)!;Assert.NotNull(newer);
            release.Set();var result=await worker.WaitAsync(TimeSpan.FromSeconds(15));Assert.False(model.CompleteStaticCheckpointOperation(op,result));Assert.True(model.Busy);
            model.AbandonStaticCheckpointOperation(newer);Assert.Same(frame,model.Current.Frame);Assert.False(model.Busy);
        }
        finally{release.Set();await worker.WaitAsync(TimeSpan.FromSeconds(15));}
    }
    [Fact] public void FailedFullReplayNeverReplacesFrameDraftOrKnownPresentation()
    {
        var model=Model();Assert.True(model.SubmitActivity(Queue(model)));Assert.True(model.Save("slot"));var root=Store().Read("slot");root.Production![0].CompletedIndustry++;
        Store().Write("slot",root,true);var frame=model.Current.Frame;var pair=model.GeneratedPresentation();var draft=model.ResearchInputs()!;draft.ScienceUnits=draft.ScienceUnits==600?300:600;
        var op=model.BeginStaticCheckpointOperation("slot",true,discard:true)!;var completion=op.Execute();Assert.False(model.CompleteStaticCheckpointOperation(op,completion));
        Assert.Same(frame,model.Current.Frame);Assert.Same(draft,model.ResearchInputs());Assert.True(model.IsCurrentGeneratedPresentation(pair));Assert.True(draft.Dirty);
    }
    [Fact] public void OwnObservedExpeditionDepartureWarnsWithoutMutationThenCapturedTravelAbortsOnlyOnce()
    {
        var model=Model();var visited=new HashSet<string>();ActivityKnownSiteDto? site=null;
        for(int i=0;i<48;i++)
        {
            site=model.Activities().Sites.FirstOrDefault(s=>s.Minor==null&&s.Choices.Any(c=>c.Work>0&&c.RequiredStudy<=3));
            var fleet=model.Current.Frame.OwnStrategicFleets.First();visited.Add(fleet.LocationNodeId.Value);
            if(site!=null&&fleet.LocationNodeId.Value==site.Site.NodeId&&fleet.RouteNodeIds.Count==0)break;
            if(fleet.RouteNodeIds.Count==0&&fleet.TransitTotalCost==0)
            {
                var offer=site!=null?model.Current.FindKnownRoute(fleet.FleetId.Value,site.Site.NodeId):model.GeneratedMap().Regions.Where(r=>!visited.Contains(r.NodeId))
                    .Select(r=>model.Current.FindKnownRoute(fleet.FleetId.Value,r.NodeId)).Where(r=>r!=null&&r.Route.Success&&r.Route.TotalCost>0)
                    .OrderBy(r=>r!.Route.TotalCost).ThenBy(r=>r!.Destination,StringComparer.Ordinal).First();
                Assert.NotNull(offer);Assert.False(model.DepartureInterruptsExpedition(offer));Assert.True(model.SendPreviewedTravel(offer,ReviewUiModel.NewCommandId()),model.Message);
            }
            Assert.True(model.Advance(),model.Message);
        }
        Assert.NotNull(site);var own=model.Activities().Ships.First();Assert.Equal(site.Site.NodeId,own.NodeId);var choice=site.Choices.First(c=>c.Work>0&&c.RequiredStudy<=3);
        var view=model.Activities();Assert.True(model.SubmitActivity(new(){ActorId=Human,ExpectedTurn=view.Turn,ExpectedRevision=view.Revision,CommandId=ReviewUiModel.NewCommandId(),
            Kind=CampaignActivityCommandKind.StartStudy,TargetId=site.Site.SiteId,DefinitionId=choice.ChoiceId,ShipId=own.ShipId}),model.Message);
        var route=model.GeneratedMap().Connections.First(e=>e.EndpointA==own.NodeId||e.EndpointB==own.NodeId);var destination=route.EndpointA==own.NodeId?route.EndpointB:route.EndpointA;
        var leaving=model.Current.FindKnownRoute(own.FleetId,destination)!;Assert.True(model.DepartureInterruptsExpedition(leaving));
        Assert.True(model.Save("before"));var bytes=codec.EncodeStreamedActivitiesRoot(Store().Read("before"));
        // A dismissed review invokes no command, even if the map/popup asks for the notice again.
        Assert.True(model.DepartureInterruptsExpedition(leaving));Assert.True(model.Save("canceled"));Assert.Equal(bytes,codec.EncodeStreamedActivitiesRoot(Store().Read("canceled")));
        Assert.True(model.SendPreviewedTravel(leaving,ReviewUiModel.NewCommandId()),model.Message);Assert.Equal(ActivityProgressStage.Aborted,model.Activities().Findings.Single().Stage);
        Assert.False(model.SendPreviewedTravel(leaving,ReviewUiModel.NewCommandId()));Assert.Equal(0,model.Activities().Findings.Single().InsightApplied);
    }
    public void Dispose()
    {
        if(!Directory.Exists(directory))return;var full=Path.GetFullPath(directory);
        if(!string.Equals(Path.GetDirectoryName(full),Path.GetTempPath().TrimEnd(Path.DirectorySeparatorChar),StringComparison.OrdinalIgnoreCase)
            ||!Path.GetFileName(full).StartsWith("HobbyDS9-Streamed-Binding-",StringComparison.Ordinal))throw new InvalidOperationException("Unsafe test cleanup");
        foreach(var file in Directory.GetFiles(full))File.Delete(file);Directory.Delete(full,false);
    }
}
