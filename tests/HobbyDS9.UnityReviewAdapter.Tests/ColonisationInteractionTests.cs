using System.Reflection;
using HobbyDS9.Application;
using HobbyDS9.Contracts;
using HobbyDS9.TacticalReviewClient;
using HobbyDS9.UnityReview;
using Xunit;
namespace HobbyDS9.UnityReviewAdapter.Tests;
public sealed class ColonisationInteractionTests:IDisposable
{
    readonly string directory=Path.Combine(Path.GetTempPath(),"HobbyDS9-Colonisation-Interaction-"+Guid.NewGuid().ToString("N"));
    readonly ReviewNewtonsoftCodec codec=new();
    CampaignColonisationPackageDto Package()
    {
        byte[] Read(string folder,string name)=>File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,"scenarios",folder,name+".json"));
        var template=PlayableFleetPackageReader.Read(Path.Combine(AppContext.BaseDirectory,"scenarios/playable-fleet-v1"));
        var activities=ActivitiesPlaytestOptions.CreatePackage(template,new[]{"catalogue","ready-classes","support","activities"}.Select(n=>Read("activities-v1",n)).ToArray(),codec);
        return CampaignColonisationApplication.CreatePackage(activities,codec.DecodeColonisationCatalogue(Read("colonisation-v1","catalogue")),codec.DecodeColonisationReadyClasses(Read("colonisation-v1","ready-classes")),codec.DecodeColonisationSupport(Read("colonisation-v1","support")),codec.DecodeColonisationPolicy(Read("colonisation-v1","settlement")));
    }
    static NaturalGalaxySettingsDto Settings()=>new(){Map=new(){Seed="42",SystemCount=30,EmptyRegionCount=30,Width=750000,Height=600000,HomeCivilizationDefinitionIds=new(){"civilization.federation","civilization.cardassian"}}};
    ReviewColonisationSlotStore Store()=>new(directory,codec);
    ReviewUiModel Model()=>new(Package(),Settings(),codec,Store());
    static PhysicalReviewHost Host(ReviewUiModel m)=>(PhysicalReviewHost)typeof(ReviewUiModel).GetField("host",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(m)!;
    static ICampaignColonisationSession Session(ReviewUiModel m)=>(ICampaignColonisationSession)typeof(PhysicalReviewHost).GetField("session",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(Host(m))!;
    byte[] Root(ReviewUiModel m)=>codec.EncodeColonisationRoot(Session(m).ExportColonisationSave());
    static ColonisationCommandDto Kit(ReviewUiModel m)=>new(){ActorId=m.Current.Frame.Actor,CommandId="command.player.kit",Turn=m.Current.Frame.Status.Turn,Revision=m.Current.Frame.Status.CampaignRevision,Action=ColonisationAction.EnqueueKit,EnqueueKit=new(){ColonyId="colony.sol"}};
    static void Probe(ReviewUiModel m,Action<string>? probe)=>typeof(PhysicalReviewHost).GetProperty("ColonisationInteractionProbe",BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(Host(m),probe);
    [Fact]public async Task RejectedConstructionKeepsSpecificCurrentFeedbackInsteadOfGenericColonisationDescription()
    {
        var m=Model();var before=Root(m);var controller=m.Current;var frame=controller.Frame;
        var option=m.ColonisationActivities().BuildOptions.First(o=>o.Kind==ActivityProductionKind.PublishedShip&&o.Supported);
        var command=new CampaignActivityCommandDto{ActorId=frame.Actor,CommandId="command.ship.unsupported-revision",ExpectedTurn=frame.Status.Turn,ExpectedRevision=frame.Status.CampaignRevision,
            Kind=CampaignActivityCommandKind.EnqueueShip,TargetId="colony.sol",DefinitionId=option.DefinitionId,BlueprintRevision=int.MaxValue};
        var work=m.BeginColonisationInteraction(ColonisationInteractionKind.Submission,command)!;var result=await Task.Run(work.Execute);
        Assert.True(m.CompleteColonisationInteraction(work,result));Assert.False(result.Result!.Accepted);Assert.Equal(CampaignTacticalError.UnsupportedSource,result.Result.Error);
        Assert.Equal("That class or construction site is not supported here.",m.Message);Assert.Equal(m.Message,m.ColonisationActionStatus());
        Assert.Equal(m.Message,m.ColonisationStatusFor(controller,frame,"colony.sol"));Assert.Null(m.ColonisationStatusFor(controller,frame,"colony.mars"));
        Assert.False(m.Busy);Assert.Same(frame,m.Current.Frame);Assert.Equal(before,Root(m));
    }
    [Fact]public async Task PreviewSubmitAdvanceAndTypedContinuePreserveEntireCanonicalRoot()
    {
        var m=Model();var sync=CampaignColonisationApplication.StartGame(Package(),Settings());var before=Root(m);var command=Kit(m);
        var preview=m.BeginColonisationInteraction(ColonisationInteractionKind.Preview,colonyCommand:command)!;
        var check=await Task.Run(preview.Execute);Assert.True(check.ColonisationEligibility!.Available);Assert.True(m.CompleteColonisationInteraction(preview,check));Assert.Equal(before,Root(m));
        var work=m.BeginColonisationInteraction(ColonisationInteractionKind.Submission,colonyCommand:command)!;command.EnqueueKit!.ColonyId="not-a-world";
        var done=await Task.Run(work.Execute);Assert.True(m.CompleteColonisationInteraction(work,done),m.Message);Assert.True(done.Committed);Assert.False(m.CompleteColonisationInteraction(work,done));Assert.Throws<InvalidOperationException>(()=>work.Execute());
        Assert.True(sync.SubmitColonisation(Kit(Model())).Accepted);Assert.Equal(codec.EncodeColonisationRoot(sync.ExportColonisationSave()),Root(m));
        var advance=m.BeginColonisationInteraction(ColonisationInteractionKind.PlanningAdvance)!;Assert.True(m.CompleteColonisationInteraction(advance,await Task.Run(advance.Execute)),m.Message);
        var stamp=sync.CaptureStatus();Assert.True(sync.AdvanceTurn(stamp.Turn,stamp.CampaignRevision).Accepted);Assert.Equal(codec.EncodeColonisationRoot(sync.ExportColonisationSave()),Root(m));
        var save=m.BeginStaticCheckpointOperation("typed",false)!;Assert.True(m.CompleteStaticCheckpointOperation(save,await Task.Run(save.Execute)),m.Message);
        var reopened=ReviewUiModel.ContinueColonisation(Package(),codec,Store(),"typed");Assert.Equal(Root(m),Root(reopened));Assert.Equal(3,Assert.Single(reopened.ColonisationActivities().Ships).StudyWork);Assert.Equal(1,Assert.Single(reopened.ColonisationActivities().Ships).ContactCapacity);Assert.Equal(m.FrontierCommission().CompletedObjectives,reopened.FrontierCommission().CompletedObjectives);
    }
    [Fact]public async Task CancelledSourceReaderKeepsBusyPairAndBlocksEveryWriterUntilDrain()
    {
        var m=Model();var before=Root(m);var frame=m.Current.Frame;var pair=m.GeneratedPresentation();
        using var entered=new ManualResetEventSlim();using var release=new ManualResetEventSlim();
        Probe(m,phase=>{if(phase=="before-fork"){entered.Set();if(!release.Wait(TimeSpan.FromSeconds(15)))throw new TimeoutException();}});
        var work=m.BeginColonisationInteraction(ColonisationInteractionKind.Submission,colonyCommand:Kit(m))!;var task=Task.Run(work.Execute);
        try
        {
            Assert.True(entered.Wait(TimeSpan.FromSeconds(5)));m.AbandonColonisationInteraction(work);Assert.True(m.Busy);Assert.Same(pair,Host(m).CaptureGeneratedPresentation(frame));Assert.True(m.IsCurrentGeneratedPresentation(pair));
            Assert.Throws<InvalidOperationException>(()=>Host(m).Read(frame.Actor));Assert.Throws<InvalidOperationException>(()=>Host(m).AdvancePlanning(frame));Assert.Throws<InvalidOperationException>(()=>Host(m).SaveCommitted());Assert.Throws<InvalidOperationException>(()=>Host(m).SubmitTravelOrder(new()));
            Assert.Null(m.BeginColonisationInteraction(ColonisationInteractionKind.Preview,colonyCommand:Kit(m)));
            release.Set();var completion=await task.WaitAsync(TimeSpan.FromSeconds(20));Assert.False(m.CompleteColonisationInteraction(work,completion));m.AbandonColonisationInteraction(work);Assert.False(m.Busy);Assert.Equal(before,Root(m));
        }
        finally{release.Set();await task.WaitAsync(TimeSpan.FromSeconds(20));Probe(m,null);m.AbandonColonisationInteraction(work);}
    }
    [Fact]public async Task LateFaultAndReentrantCancelDoNotPublishAndStaleFinallyDoesNotClearNewWork()
    {
        var m=Model();var before=Root(m);Probe(m,p=>{if(p=="after-capture")throw new InvalidOperationException("late test fault");});
        var failed=m.BeginColonisationInteraction(ColonisationInteractionKind.Submission,colonyCommand:Kit(m))!;Assert.False(m.CompleteColonisationInteraction(failed,await Task.Run(failed.Execute)));Assert.Equal(before,Root(m));Probe(m,null);
        var old=m.BeginColonisationInteraction(ColonisationInteractionKind.Submission,colonyCommand:Kit(m))!;var completion=await Task.Run(old.Execute);ColonisationInteractionWork? newer=null;
        Probe(m,p=>{if(p=="before-publication"){m.AbandonColonisationInteraction(old);newer=m.BeginColonisationInteraction(ColonisationInteractionKind.Preview,colonyCommand:Kit(m));}});
        Assert.False(m.CompleteColonisationInteraction(old,completion));Assert.NotNull(newer);Assert.True(m.Busy);Probe(m,null);m.AbandonColonisationInteraction(old);Assert.True(m.Busy);
        Assert.True(m.CompleteColonisationInteraction(newer!,await Task.Run(newer!.Execute)));Assert.Equal(before,Root(m));
    }
    [Fact]public async Task TypedMapCurrentStampCachedViewsAndRejectedLoadPreserveLiveOwnerAndDrafts()
    {
        var m=Model();var frame=m.Current.Frame;Assert.Equal(3,Assert.Single(m.ColonisationActivities().Ships).StudyWork);Assert.Equal(1,Assert.Single(m.ColonisationActivities().Ships).ContactCapacity);Assert.Equal(frame.Status.Turn,m.GeneratedMap().ObservationTurn);
        var first=m.Colonisation();Assert.Equal("blueprint.ready.constitution-explorer",Assert.Single(first.OwnShipSources).BlueprintId);Assert.Equal(1,Assert.Single(first.OwnShipSources).Revision);Assert.Same(first,m.Colonisation());Assert.Same(m.ColonisationPlanets(),m.ColonisationPlanets());
        var command=Kit(m);var work=m.BeginColonisationInteraction(ColonisationInteractionKind.Submission,colonyCommand:command)!;
        Assert.True(m.CompleteColonisationInteraction(work,await Task.Run(work.Execute)));Assert.Throws<InvalidOperationException>(()=>Host(m).CaptureGeneratedMap(frame));
        Assert.Equal(m.Current.Frame.Status.CampaignRevision,m.GeneratedMap().CampaignRevision);Assert.NotSame(first,m.Colonisation());
        var before=Root(m);var controller=m.Current;var current=m.Current.Frame;Assert.True(m.Save("good"));
        var invalid=Store().Read("good");invalid.Production![0].CompletedIndustry++;Store().Write("forged",invalid,false);
        m.LocalDesignDirty=true;Assert.Null(m.BeginStaticCheckpointOperation("forged",true));Assert.True(m.LocalDesignDirty);
        var load=m.BeginStaticCheckpointOperation("forged",true,discard:true)!;Assert.False(m.CompleteStaticCheckpointOperation(load,await Task.Run(load.Execute)));
        Assert.Same(controller,m.Current);Assert.Same(current,m.Current.Frame);Assert.True(m.LocalDesignDirty);Assert.Equal(before,Root(m));
        m.LocalDesignDirty=false;m.Current.MarkStrategicFailure(CampaignTacticalError.InvalidBatch);
        Assert.Null(m.BeginColonisationInteraction(ColonisationInteractionKind.Submission,colonyCommand:Kit(m)));Assert.Equal(before,Root(m));
    }
    [Fact]public async Task SurveyDepartureWarningUsesOwnActiveSurveyAndOrdinaryKnownRouteOnly()
    {
        var m=Model();var ships=m.ColonisationActivities().Ships;Assert.True(ships.Any(s=>s.StudyWork>0&&s.Stationary),Newtonsoft.Json.JsonConvert.SerializeObject(ships));var scout=ships.First(s=>s.StudyWork>0&&s.Stationary);var bodies=m.ColonisationPlanets();Assert.True(bodies.Any(p=>p.SystemId==m.Colonisation().Worlds.Single(w=>w.Id=="colony.sol").SystemId),Newtonsoft.Json.JsonConvert.SerializeObject(bodies));var home=bodies.First(p=>p.SystemId==m.Colonisation().Worlds.Single(w=>w.Id=="colony.sol").SystemId);
        var command=new ColonisationCommandDto{ActorId=m.Current.Frame.Actor,CommandId="command.player.survey",Turn=m.Current.Frame.Status.Turn,Revision=m.Current.Frame.Status.CampaignRevision,Action=ColonisationAction.StartPlanetSurvey,StartSurvey=new(){ShipId=scout.ShipId,PlanetId=home.PlanetId}};
        var work=m.BeginColonisationInteraction(ColonisationInteractionKind.Submission,colonyCommand:command)!;var result=await Task.Run(work.Execute);Assert.True(m.CompleteColonisationInteraction(work,result));Assert.True(result.Committed);
        var node=m.Current.Frame.Strategy!.Navigation.Nodes.First(n=>n.NodeId.Value!=scout.NodeId);
        var offer=Host(m).FindKnownRoute(m.Current.Frame,scout.FleetId,node.NodeId.Value);Assert.True(offer.Route.Success);Assert.True(m.DepartureInterruptsPlanetSurvey(offer));Assert.True(m.DepartureInterruptsExpedition(offer));
        var before=Root(m);Assert.True(m.DepartureInterruptsPlanetSurvey(offer));Assert.Equal(before,Root(m));
        var survey=m.Colonisation().Surveys.Single(x=>x.Stage==PlanetSurveyStage.Active);var cancel=new ColonisationCommandDto{ActorId=m.Current.Frame.Actor,CommandId="command.player.survey.cancel",Turn=m.Current.Frame.Status.Turn,Revision=m.Current.Frame.Status.CampaignRevision,Action=ColonisationAction.CancelPlanetSurvey,CancelSurvey=new(){SurveyId=survey.Id}};
        var cancelWork=m.BeginColonisationInteraction(ColonisationInteractionKind.Submission,colonyCommand:cancel)!;Assert.True(m.CompleteColonisationInteraction(cancelWork,await Task.Run(cancelWork.Execute)));Assert.False(m.DepartureInterruptsPlanetSurvey(offer));
    }
    [Fact]public void PinnedCompositionAndImmutablePlanetViewsRejectChangedBytesAndCallerAliasing()
    {
        var original=Package();var bytes=new[]{"catalogue","ready-classes","support","settlement"}.Select(n=>File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,"scenarios/colonisation-v1",n+".json"))).ToArray();
        var composed=ColonisationPlaytestOptions.CreatePackage(original.Activities!,bytes,codec);Assert.Equal(original.PackageHash,composed.PackageHash);
        bytes[0][0]^=1;Assert.Throws<ArgumentException>(()=>ColonisationPlaytestOptions.CreatePackage(original.Activities!,bytes,codec));
        var dto=new ColonisationPlanetObservationDto{DisplayName="Known body",Environment=new(){PlanetClass="temperate",Deposits=new(){new(){ResourceDefinitionId="resource.dilithium",Amount=120}}},KnownControl=new(){Kind=PlanetControllerKind.Unclaimed}};
        var view=new ColonisationPlanetSnapshot(dto);dto.Environment.Deposits![0].Amount=999;dto.Environment.PlanetClass="changed";dto.KnownControl.Kind=PlanetControllerKind.Empire;
        Assert.Equal("temperate",view.PlanetClass);Assert.Equal(120,Assert.Single(view.InitialDeposits).Amount);Assert.Equal(PlanetControllerKind.Unclaimed,view.KnownControl);
    }
    public void Dispose(){if(Directory.Exists(directory))Directory.Delete(directory,true);}
}
