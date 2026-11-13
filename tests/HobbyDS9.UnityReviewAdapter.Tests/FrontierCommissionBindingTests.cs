using System.Reflection;
using HobbyDS9.Application;
using HobbyDS9.Contracts;
using HobbyDS9.TacticalReviewClient;
using HobbyDS9.UnityReview;
using Xunit;

namespace HobbyDS9.UnityReviewAdapter.Tests;

public sealed class FrontierCommissionBindingTests : IDisposable
{
    readonly string directory=Path.Combine(Path.GetTempPath(),"HobbyDS9-Commission-Binding-"+Guid.NewGuid().ToString("N"));
    readonly ReviewNewtonsoftCodec codec=new();
    CampaignPlayableFleetPackageDto Template()=>PlayableFleetPackageReader.Read(Path.Combine(AppContext.BaseDirectory,"scenarios/playable-fleet-v1"));
    CampaignStreamedActivitiesPackageDto Package()=>CampaignStreamedActivitiesApplication.CreatePackage(ActivitiesPlaytestOptions.CreatePackage(Template(),
        new[]{"catalogue","ready-classes","support","activities"}.Select(n=>File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,"scenarios/activities-v1",n+".json"))).ToArray(),codec));
    static NaturalGalaxySettingsDto Settings()=>new(){Map=new(){Seed="42",SystemCount=30,EmptyRegionCount=30,Width=750000,Height=600000,
        HomeCivilizationDefinitionIds=new(){"civilization.federation","civilization.cardassian"}}};
    ReviewStreamedActivitiesSlotStore Store()=>new(directory,codec);
    ReviewUiModel Model()=>new(Package(),Settings(),codec,Store());
    static PhysicalReviewHost Host(ReviewUiModel m)=>(PhysicalReviewHost)typeof(ReviewUiModel).GetField("host",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(m)!;
    [Fact] public void QueryPreservesDirtyDraftFrameAndExactCommittedRoot()
    {
        var m=Model();var draft=m.ResearchInputs()!;draft.ScienceUnits=draft.ScienceUnits==600?300:600;
        var frame=m.Current.Frame;Assert.True(m.Save("before"),m.Message);var before=codec.EncodeStreamedActivitiesRoot(Store().Read("before"));
        var report=m.FrontierCommission();Assert.Equal(FrontierCommissionAvailability.Available,report.Availability);Assert.Equal(frame.Actor,report.Actor);
        Assert.Equal(0,report.CompletedObjectives);Assert.Equal(4,report.Objectives.Count);
        Assert.Same(frame,m.Current.Frame);Assert.Same(draft,m.ResearchInputs());Assert.True(draft.Dirty);
        Assert.True(m.Save("after"),m.Message);Assert.Equal(before,codec.EncodeStreamedActivitiesRoot(Store().Read("after")));
    }
    [Fact] public void OldFrameAndForeignHostCannotReadCurrentCommission()
    {
        var m=Model();var other=Model();var old=m.Current.Frame;var observation=m.Activities();
        Assert.Throws<InvalidOperationException>(()=>Host(other).CaptureFrontierCommission(old));
        Assert.True(m.SubmitActivity(new(){ActorId=observation.ActorId,ExpectedTurn=observation.Turn,ExpectedRevision=observation.Revision,
            CommandId=ReviewUiModel.NewCommandId(),Kind=CampaignActivityCommandKind.EnqueueShip,TargetId="colony.sol",
            DefinitionId="blueprint.ready.constitution-explorer",BlueprintRevision=1}),m.Message);
        Assert.Throws<InvalidOperationException>(()=>Host(m).CaptureFrontierCommission(old));
        Assert.Equal(FrontierCommissionProgress.Underway,m.FrontierCommission().Objectives.Single(o=>o.Id=="ship").Progress);
    }
    [Fact] public void SameNamedCustomContentKeepsSandboxButHidesMissionEvidence()
    {
        var original=Package().ContentPackage!;original.Content!.Sites![0].Title+=" - alternate";
        var activity=CampaignActivitiesApplication.CreatePackage(original.BaseTemplate!,original.Catalogue!,original.ReadyClasses!,original.Support!,original.Content,original.RecordBudget);
        var package=CampaignStreamedActivitiesApplication.CreatePackage(activity);
        var m=new ReviewUiModel(package,Settings(),codec,Store());var report=m.FrontierCommission();
        Assert.Equal(FrontierCommissionAvailability.UnsupportedContent,report.Availability);Assert.Empty(report.Objectives);Assert.False(report.Completed);
        Assert.True(m.Advance(),m.Message);Assert.Equal(FrontierCommissionAvailability.UnsupportedContent,m.FrontierCommission().Availability);
    }
    [Fact] public void BusyClosedAndHistoricalProfileDoNotOfferTheReport()
    {
        var m=Model();var operation=m.BeginStaticCheckpointOperation("held",false)!;Assert.NotNull(operation);Assert.True(m.Busy);
        Assert.Throws<InvalidOperationException>(()=>m.FrontierCommission());m.AbandonStaticCheckpointOperation(operation);
        Assert.Equal(FrontierCommissionAvailability.Available,m.FrontierCommission().Availability);Assert.True(m.Close(true));
        Assert.Throws<InvalidOperationException>(()=>m.FrontierCommission());
        var old=new ReviewUiModel(Template(),Settings(),codec,new ReviewStaticAstronomyCampaignSlotStore(directory,codec));
        Assert.Throws<InvalidOperationException>(()=>old.FrontierCommission());Assert.Throws<InvalidOperationException>(()=>Host(old).CaptureFrontierCommission(old.Current.Frame));
    }
    public void Dispose()
    {
        if(!Directory.Exists(directory))return;var full=Path.GetFullPath(directory);
        if(!string.Equals(Path.GetDirectoryName(full),Path.GetTempPath().TrimEnd(Path.DirectorySeparatorChar),StringComparison.OrdinalIgnoreCase)
            ||!Path.GetFileName(full).StartsWith("HobbyDS9-Commission-Binding-",StringComparison.Ordinal))throw new InvalidOperationException("Unsafe test cleanup");
        foreach(var file in Directory.GetFiles(full))File.Delete(file);Directory.Delete(full,false);
    }
}
