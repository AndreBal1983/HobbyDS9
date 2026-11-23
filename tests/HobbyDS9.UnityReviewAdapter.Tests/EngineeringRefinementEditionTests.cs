using System.Text.Json;
using HobbyDS9.Application;
using HobbyDS9.Contracts;
using HobbyDS9.Contracts.ShipDesign;
using HobbyDS9.UnityReview;
using Newtonsoft.Json.Linq;
using Xunit;
namespace HobbyDS9.UnityReviewAdapter.Tests;
public sealed class EngineeringRefinementEditionTests
{
    const string Human="civ.federation";
    readonly ReviewNewtonsoftCodec codec=new();
    static byte[] Read(string folder,string name)=>File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,"scenarios",folder,name+".json"));
    static string Json(object? value)=>JsonSerializer.Serialize(value,value?.GetType()??typeof(object));
    CampaignColonisationExpansionPackageDto Growth(bool solEight=true)
    {
        var template=ReviewExactPackageLoader.LoadPlayableFleet(Read("playable-fleet-v1","playable"),Read("playable-fleet-v1","package"),Read("playable-fleet-v1","physical"),Read("playable-fleet-v1","core"),Read(solEight?"sol-eight-planets-v1":"engineering-development-v1","scenario"),codec);
        var activities=ActivitiesPlaytestOptions.CreatePackage(template,new[]{"catalogue","ready-classes","support","activities"}.Select(n=>Read("activities-v1",n)).ToArray(),codec);
        var colony=ColonisationPlaytestOptions.CreatePackage(activities,new[]{"catalogue","ready-classes","support","settlement"}.Select(n=>Read("colonisation-v1",n)).ToArray(),codec);
        var roster=ColonisationPlaytestOptions.CreateRosterPackage(colony,Read("colonisation-roster-v2","roster"),codec);
        return ColonisationGrowthPlaytestOptions.CreatePackage(roster,Read("colonisation-expansion-v1","planner"),codec);
    }
    static NaturalGalaxySettingsDto Settings(int size=30)=>new(){Map=new(){Seed="42",SystemCount=size,EmptyRegionCount=size,
        Width=size==30?750000:2500000,Height=size==30?600000:2000000,HomeCivilizationDefinitionIds=new(){"civilization.federation","civilization.cardassian"}}};
    byte[] Wire(ICampaignColonisationCycleSession session)=>codec.EncodeCycleRoot(session.ExportCycleSave());
    static GameStateDto Game(CampaignColonisationCycleSaveDto root)=>root.BaseCampaign!.Campaign!.Integrated!.Physical!.Core.Campaign.GameState;


    [Fact] public void SourceIsSeparateV5AndRetainsAuthoredSolWorldWithoutMutatingV4()
    {
        var growth=Growth();var before=Json(growth);var old=AuthoredSolPlaytestOptions.CreatePackage(growth);
        var package=EngineeringRefinementPlaytestOptions.CreatePackage(growth);
        Assert.Equal(before,Json(growth));Assert.NotEqual(old.PackageHash,package.PackageHash);
        Assert.Equal(4,growth.BaseRosterV2!.BaseV1!.Support!.FormatVersion);
        Assert.Equal(ColonisationRefinementSupportIdentity.Format,package.BaseGrowth!.BaseRosterV2!.BaseV1!.Support!.FormatVersion);
        Assert.IsType<CampaignEngineeringRefinementPackageDto>(codec.CopyCyclePackage(package));
        var session=CampaignColonisationCycleApplication.StartCycleGame(package,Settings());
        var previous=CampaignColonisationCycleApplication.StartCycleGame(old,Settings());
        var root=Assert.IsType<CampaignEngineeringRefinementSaveDto>(session.ExportCycleSave());
        var oldRoot=Assert.IsType<CampaignAuthoredSolSaveDto>(previous.ExportCycleSave());
        Assert.Equal(17,root.FormatVersion);Assert.Equal(EngineeringRefinementIdentity.Profile,root.ProfileId);
        Assert.Equal(Json(oldRoot.BaseCampaign!.FrozenPlanets),Json(root.BaseCampaign!.FrozenPlanets));
        Assert.Equal(Json(oldRoot.SystemBelts),Json(root.SystemBelts));Assert.Equal(CampaignAuthoredSolIdentity.Generation,root.GenerationVersion);
        Assert.Equal(CampaignAuthoredSolIdentity.Generation,session.CaptureKnownAstronomy(Human).SystemBeltGeneration);
        Assert.Empty(root.Refinement!.Projects!);Assert.Empty(root.Refinement.Operations!);
        Assert.ThrowsAny<Exception>(()=>CampaignColonisationCycleApplication.LoadCycleSave(old,root));
        Assert.ThrowsAny<Exception>(()=>CampaignColonisationCycleApplication.LoadCycleSave(package,oldRoot));
        var forged=codec.CopyCyclePackage(old);forged.BaseGrowth=package.BaseGrowth;
        Assert.ThrowsAny<Exception>(()=>CampaignColonisationCycleApplication.StartCycleGame(forged,Settings()));
        Assert.Equal(Wire(session),Wire(CampaignColonisationCycleApplication.LoadCycleSave(package,codec.DecodeCycleRoot(Wire(session)))));
    }
    [Theory][InlineData(30)][InlineData(250)]
    public void NewPlayableEditionPreservesAllTwentyEngineeringProjects(int size)
    {
        var growth=Growth();var old=CampaignColonisationCycleApplication.StartCycleGame(AuthoredSolPlaytestOptions.CreatePackage(growth),Settings(size));
        var current=CampaignColonisationCycleApplication.StartCycleGame(EngineeringRefinementPlaytestOptions.CreatePackage(growth),Settings(size));
        var prior=old.CaptureStrategy(Human).Research.Engineering;var next=current.CaptureStrategy(Human).Research.Engineering;
        Assert.Equal(20,next.Count);Assert.Equal(prior.Select(c=>c.Definition.DefinitionId),next.Select(c=>c.Definition.DefinitionId));
        Assert.All(next,c=>Assert.Equal(CommandErrorCode.EngineeringScienceRequirementMissing,c.UnavailableReason));
        Assert.Equal(Wire(current),Wire(CampaignColonisationCycleApplication.LoadCycleSave(EngineeringRefinementPlaytestOptions.CreatePackage(growth),codec.DecodeCycleRoot(Wire(current)))));
    }
    static EngineeringRefinementCommandDto Command(ICampaignColonisationCycleSession session,string id,EngineeringRefinementAction action=EngineeringRefinementAction.Start,string? project=null)
    {
        var stamp=session.CaptureStatus();return new(){ActorId=Human,CommandId=id,ExpectedTurn=stamp.Turn,ExpectedRevision=stamp.CampaignRevision,
            Action=action,ParentVersionId=action==EngineeringRefinementAction.Start?"component.reactor.v1":null,Goal=EngineeringRefinementGoal.ReduceCapacity,ProjectId=project,
            ContextBlueprintId=action==EngineeringRefinementAction.Start?"blueprint.ready.constitution-explorer":null,ContextBlueprintRevision=1};
    }
    [Fact] public void WorkingAndPendingStatesReplayCustomCommandsAtOrdinaryOffsetsAndRejectTampering()
    {
        var package=EngineeringRefinementPlaytestOptions.CreatePackage(Growth());var session=CampaignColonisationCycleApplication.StartCycleGame(package,Settings());
        var refinement=Assert.IsAssignableFrom<ICampaignEngineeringRefinementSession>(session);
        var start=refinement.SubmitEngineeringRefinement(Command(session,"command.refinement.persistence.start"));Assert.True(start.Accepted,start.Error.ToString());
        var stamp=session.CaptureStatus();Assert.True(session.SubmitColonisation(new(){ActorId=Human,CommandId="command.refinement.persistence.kit",Turn=stamp.Turn,Revision=stamp.CampaignRevision,
            Action=ColonisationAction.EnqueueKit,EnqueueKit=new(){ColonyId="colony.sol"}}).Accepted);
        foreach(var action in new[]{EngineeringRefinementAction.Pause,EngineeringRefinementAction.Resume})
        {var result=refinement.SubmitEngineeringRefinement(Command(session,"command.refinement.persistence."+action.ToString().ToLowerInvariant(),action,start.ProjectId));Assert.True(result.Accepted,result.Error.ToString());}
        void ReopensExactly()
        {var bytes=Wire(session);var decoded=Assert.IsType<CampaignEngineeringRefinementSaveDto>(codec.DecodeCycleRoot(bytes));Assert.Equal(bytes,Wire(CampaignColonisationCycleApplication.LoadCycleSave(package,decoded)));}
        ReopensExactly();var root=Assert.IsType<CampaignEngineeringRefinementSaveDto>(codec.DecodeCycleRoot(Wire(session)));
        Assert.Equal(new[]{0,1,1},root.Refinement!.Operations!.Select(o=>o.BaseJournalOffset));
        var forged=Assert.IsType<CampaignEngineeringRefinementSaveDto>(codec.CopyCycleSave(root));forged.Refinement!.Operations![0].BaseJournalOffset=1;
        Assert.ThrowsAny<Exception>(()=>CampaignColonisationCycleApplication.LoadCycleSave(package,forged));
        for(int i=0;i<12&&refinement.CaptureEngineeringRefinement(Human)!.Projects.Single().Stage==EngineeringRefinementStage.Working;i++)
        {stamp=session.CaptureStatus();Assert.True(session.AdvanceTurn(stamp.Turn,stamp.CampaignRevision).Accepted);}
        Assert.Equal(EngineeringRefinementStage.Pending,refinement.CaptureEngineeringRefinement(Human)!.Projects.Single().Stage);ReopensExactly();
        root=Assert.IsType<CampaignEngineeringRefinementSaveDto>(codec.DecodeCycleRoot(Wire(session)));
        root.Refinement!.Projects![0].Outcome!.MaturityMilli++;
        Assert.ThrowsAny<Exception>(()=>CampaignColonisationCycleApplication.LoadCycleSave(package,root));
        var accept=refinement.SubmitEngineeringRefinement(Command(session,"command.refinement.persistence.accept",EngineeringRefinementAction.Accept,start.ProjectId));
        Assert.True(accept.Accepted,accept.Error.ToString());ReopensExactly();
    }
    [Fact] public void CarrierRejectsMissingStateAndOversizedProjectCommandAndContextGraphs()
    {
        var package=EngineeringRefinementPlaytestOptions.CreatePackage(Growth());var session=CampaignColonisationCycleApplication.StartCycleGame(package,Settings());
        var refinement=Assert.IsAssignableFrom<ICampaignEngineeringRefinementSession>(session);Assert.True(refinement.SubmitEngineeringRefinement(Command(session,"command.refinement.bounds.start")).Accepted);
        var bytes=Wire(session);
        foreach(var change in new Action<JObject>[] {
            w=>w.Remove("Refinement"),
            w=>{var rows=(JArray)w["Refinement"]!["Projects"]!;while(rows.Count<=64)rows.Add(rows[0].DeepClone());},
            w=>{var rows=(JArray)w["Refinement"]!["Operations"]!;while(rows.Count<=1024)rows.Add(rows[0].DeepClone());},
            w=>{var rows=(JArray)w["Refinement"]!["Projects"]![0]!["Context"]!["FrozenCatalogue"]!["Components"]!;while(rows.Count<=512)rows.Add(rows[0].DeepClone());}})
        {var wire=JObject.Parse(System.Text.Encoding.UTF8.GetString(bytes));change(wire);Assert.ThrowsAny<Exception>(()=>codec.DecodeCycleRoot(System.Text.Encoding.UTF8.GetBytes(wire.ToString())));}
        var root=Assert.IsType<CampaignEngineeringRefinementSaveDto>(codec.DecodeCycleRoot(bytes));
        root.Refinement!.Projects![0].Context!.FrozenCatalogue!.Components=Enumerable.Repeat(root.Refinement.Projects[0].Context!.FrozenCatalogue!.Components![0],513).ToList();
        Assert.ThrowsAny<Exception>(()=>CampaignColonisationCycleApplication.LoadCycleSave(package,root));
        Assert.Equal(bytes,Wire(session));
    }
}
