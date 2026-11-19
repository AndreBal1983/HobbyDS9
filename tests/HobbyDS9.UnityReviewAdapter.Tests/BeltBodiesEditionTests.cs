using System.Reflection;
using System.Text.Json;
using HobbyDS9.Application;
using HobbyDS9.Contracts;
using HobbyDS9.Contracts.ShipDesign;
using HobbyDS9.Simulation;
using HobbyDS9.UnityReview;
using Newtonsoft.Json.Linq;
using Xunit;
namespace HobbyDS9.UnityReviewAdapter.Tests;
public sealed class BeltBodiesEditionTests
{
    const string Human="civ.federation";
    readonly ReviewNewtonsoftCodec codec=new();
    static byte[] Read(string folder,string name)=>File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,"scenarios",folder,name+".json"));
    static string Json(object? x)=>JsonSerializer.Serialize(x,x?.GetType()??typeof(object));
    CampaignColonisationExpansionPackageDto Growth()
    {
        var t=ReviewExactPackageLoader.LoadPlayableFleet(Read("playable-fleet-v1","playable"),Read("playable-fleet-v1","package"),Read("playable-fleet-v1","physical"),Read("playable-fleet-v1","core"),Read("sol-eight-planets-v1","scenario"),codec);
        var a=ActivitiesPlaytestOptions.CreatePackage(t,new[]{"catalogue","ready-classes","support","activities"}.Select(n=>Read("activities-v1",n)).ToArray(),codec);
        var c=ColonisationPlaytestOptions.CreatePackage(a,new[]{"catalogue","ready-classes","support","settlement"}.Select(n=>Read("colonisation-v1",n)).ToArray(),codec);
        var r=ColonisationPlaytestOptions.CreateRosterPackage(c,Read("colonisation-roster-v2","roster"),codec);
        return ColonisationGrowthPlaytestOptions.CreatePackage(r,Read("colonisation-expansion-v1","planner"),codec);
    }
    static NaturalGalaxySettingsDto Settings(int size)=>new(){Map=new(){Seed="42",SystemCount=size,EmptyRegionCount=size,Width=size==30?750000:size==1000?5000000:2500000,Height=size==30?600000:size==1000?4000000:2000000,HomeCivilizationDefinitionIds=new(){"civilization.federation","civilization.cardassian"}}};
    static GameStateDto Game(CampaignColonisationCycleSaveDto r)=>r.BaseCampaign!.Campaign!.Integrated!.Physical!.Core.Campaign.GameState;
    static CampaignTacticalReviewSession Backend(ICampaignColonisationCycleSession s)=>(CampaignTacticalReviewSession)typeof(CampaignIntegratedPlaytestApplication).GetField("session",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(s)!;
    byte[] Wire(ICampaignColonisationCycleSession s)=>codec.EncodeCycleRoot(s.ExportCycleSave());
    [Theory][InlineData(30)][InlineData(250)][InlineData(1000)]
    public void ExplicitEditionPreservesPlanetsVisualsRngRefinementAndTwentyProjectsWhileAddingFiniteBodies(int size)
    {
        var growth=Growth();var input=Json(growth);var p17=EngineeringRefinementPlaytestOptions.CreatePackage(growth);var p18=BeltBodiesPlaytestOptions.CreatePackage(growth);
        Assert.Equal(input,Json(growth));Assert.NotEqual(p17.PackageHash,p18.PackageHash);Assert.Equal(5,p18.FormatVersion);Assert.Equal(ColonisationRefinementSupportIdentity.Format,p18.BaseGrowth!.BaseRosterV2!.BaseV1!.Support!.FormatVersion);
        Assert.IsType<CampaignBeltBodiesPackageDto>(codec.CopyCyclePackage(p18));
        var old=CampaignColonisationCycleApplication.StartCycleGame(p17,Settings(size));var current=CampaignColonisationCycleApplication.StartCycleGame(p18,Settings(size));
        var before=Assert.IsType<CampaignEngineeringRefinementSaveDto>(old.ExportCycleSave());var root=Assert.IsType<CampaignBeltBodiesSaveDto>(current.ExportCycleSave());
        Assert.Equal(18,root.FormatVersion);Assert.Equal(BeltBodiesEditionIdentity.Profile,root.ProfileId);Assert.Equal(Json(before.SystemBelts),Json(root.SystemBelts));
        Assert.Equal(Json(before.BaseCampaign!.FrozenPlanets),Json(root.BaseCampaign!.FrozenPlanets));Assert.Equal(Json(Game(before).Planets),Json(Game(root).Planets));
        Assert.Equal(Json(Game(before).RngState),Json(Game(root).RngState));Assert.Equal(Game(before).NextEntitySequence,Game(root).NextEntitySequence);Assert.Equal(Json(before.Refinement),Json(root.Refinement));
        var bodies=root.BeltBodies!;Assert.Equal(root.SystemBelts!.Count,bodies.Definitions!.Count);Assert.Equal(bodies.Definitions.Count,bodies.Deposits!.Count);
        Assert.Equal(root.SystemBelts.Select(b=>b.FeatureId).OrderBy(x=>x,StringComparer.Ordinal),bodies.Definitions.Select(b=>b.FeatureId).OrderBy(x=>x,StringComparer.Ordinal));
        Assert.All(bodies.Definitions,d=>{Assert.Equal("body."+d.FeatureId,d.BodyId);Assert.InRange(d.HabitabilityBps,0,10000);Assert.Equal(BeltMiningPolicy.NaturalHabitabilityBps,d.HabitabilityBps);Assert.Equal(BeltMiningPolicy.Resource,d.ResourceDefinitionId);Assert.True(d.InitialDeposit>0);var deposit=bodies.Deposits.Single(x=>x.BodyId==d.BodyId);Assert.Equal(d.InitialDeposit,deposit.Remaining);Assert.Equal(0,deposit.LocalStock);Assert.DoesNotContain(Game(root).Planets!,p=>p.PlanetId==d.BodyId);});
        var sol=Assert.Single(bodies.Definitions,d=>d.FeatureId==CampaignAuthoredSolIdentity.FeatureId);Assert.True(sol.InitialDeposit>0);
        Assert.Empty(bodies.Surveys!);Assert.Empty(bodies.Mines!);Assert.Empty(bodies.LastShipments!);Assert.Empty(bodies.Operations!);
        var view=Backend(current).CaptureBeltBodies(Human)!;Assert.Contains(view.Bodies,b=>b.BodyId==sol.BodyId);Assert.All(view.Bodies,b=>{Assert.False(b.Surveyed);Assert.Null(b.HabitabilityBps);Assert.Null(b.ResourceDefinitionId);Assert.Null(b.ObservedRemaining);Assert.False(b.SettlementSupported);});
        var previous=old.CaptureStrategy(Human).Research.Engineering;var next=current.CaptureStrategy(Human).Research.Engineering;Assert.Equal(20,next.Count);Assert.Equal(previous.Select(c=>c.Definition.DefinitionId),next.Select(c=>c.Definition.DefinitionId));
        Assert.ThrowsAny<Exception>(()=>CampaignColonisationCycleApplication.LoadCycleSave(p17,root));Assert.ThrowsAny<Exception>(()=>CampaignColonisationCycleApplication.LoadCycleSave(p18,before));
        // Old checkpoints are untouched and retain the exact old typed carrier.
        var oldBytes=Wire(old);Assert.Equal(oldBytes,Wire(CampaignColonisationCycleApplication.LoadCycleSave(p17,codec.DecodeCycleRoot(oldBytes))));
        var bytes=Wire(current);Assert.IsType<CampaignBeltBodiesSaveDto>(codec.DecodeCycleRoot(bytes));Assert.Equal(bytes,Wire(CampaignColonisationCycleApplication.LoadCycleSave(p18,codec.DecodeCycleRoot(bytes))));
        if(size==30){var repeated=CampaignColonisationCycleApplication.StartCycleGame(p18,Settings(size));Assert.Equal(Json(root.BeltBodies),Json(Assert.IsType<CampaignBeltBodiesSaveDto>(repeated.ExportCycleSave()).BeltBodies));}
    }
    static EngineeringRefinementCommandDto Refine(ICampaignColonisationCycleSession s,string id,EngineeringRefinementAction action=EngineeringRefinementAction.Start,string? project=null)
    {var f=s.CaptureStatus();return new(){ActorId=Human,CommandId=id,ExpectedTurn=f.Turn,ExpectedRevision=f.CampaignRevision,Action=action,ParentVersionId=action==EngineeringRefinementAction.Start?"component.reactor.v1":null,Goal=EngineeringRefinementGoal.ReduceCapacity,ProjectId=project,ContextBlueprintId=action==EngineeringRefinementAction.Start?"blueprint.ready.constitution-explorer":null,ContextBlueprintRevision=1};}
    static BeltBodyCommandDto Survey(ICampaignColonisationCycleSession s,string body,string ship)
    {var f=s.CaptureStatus();return new(){ActorId=Human,CommandId="command.belt.edition.survey",ExpectedTurn=f.Turn,ExpectedRevision=f.CampaignRevision,Action=BeltBodyAction.StartSurvey,BodyId=body,ShipId=ship};}
    [Fact] public void CrossTypeSameOffsetCommandsReplayInRevisionOrderAndTamperingCannotReorderThem()
    {
        var p=BeltBodiesPlaytestOptions.CreatePackage(Growth());var s=CampaignColonisationCycleApplication.StartCycleGame(p,Settings(30));var backend=Backend(s);
        var initial=Wire(s);var refine=backend.SubmitEngineeringRefinement(Refine(s,"command.belt.edition.refine"));Assert.True(refine.Accepted,refine.Error.ToString());
        var body=Assert.Single(backend.CaptureBeltBodies(Human)!.Bodies,b=>b.FeatureId==CampaignAuthoredSolIdentity.FeatureId);var ship=body.SurveyShipIds.First();
        var survey=backend.SubmitBeltCommand(Survey(s,body.BodyId,ship));Assert.True(survey.Accepted,survey.Error.ToString());
        var pause=backend.SubmitEngineeringRefinement(Refine(s,"command.belt.edition.pause",EngineeringRefinementAction.Pause,refine.ProjectId));Assert.True(pause.Accepted,pause.Error.ToString());
        var f=s.CaptureStatus();var ordinary=s.SubmitColonisation(new(){ActorId=Human,CommandId="command.belt.edition.kit",Turn=f.Turn,Revision=f.CampaignRevision,Action=ColonisationAction.EnqueueKit,EnqueueKit=new(){ColonyId="colony.sol"}});Assert.True(ordinary.Accepted,ordinary.Error.ToString());
        var bytes=Wire(s);var root=Assert.IsType<CampaignBeltBodiesSaveDto>(codec.DecodeCycleRoot(bytes));Assert.NotEqual(initial,bytes);
        Assert.Equal(new[]{0,0},root.Refinement!.Operations!.Select(o=>o.BaseJournalOffset));Assert.Equal(0,Assert.Single(root.BeltBodies!.Operations!).BaseJournalOffset);
        Assert.Equal(0,root.Refinement!.Operations![0].Command!.ExpectedRevision);Assert.Equal(1,root.BeltBodies!.Operations![0].Command!.ExpectedRevision);Assert.Equal(2,root.Refinement!.Operations![1].Command!.ExpectedRevision);
        Assert.Equal(bytes,Wire(CampaignColonisationCycleApplication.LoadCycleSave(p,codec.DecodeCycleRoot(bytes))));
        var reversed=Assert.IsType<CampaignBeltBodiesSaveDto>(codec.CopyCycleSave(root));reversed.Refinement!.Operations!.Reverse();Assert.ThrowsAny<Exception>(()=>CampaignColonisationCycleApplication.LoadCycleSave(p,reversed));
        var duplicate=Assert.IsType<CampaignBeltBodiesSaveDto>(codec.CopyCycleSave(root));duplicate.BeltBodies!.Operations![0].Command!.ExpectedRevision=0;Assert.ThrowsAny<Exception>(()=>CampaignColonisationCycleApplication.LoadCycleSave(p,duplicate));
        var wrongOffset=Assert.IsType<CampaignBeltBodiesSaveDto>(codec.CopyCycleSave(root));wrongOffset.BeltBodies!.Operations![0].BaseJournalOffset=1;Assert.ThrowsAny<Exception>(()=>CampaignColonisationCycleApplication.LoadCycleSave(p,wrongOffset));
        Assert.Equal(bytes,Wire(s));
    }
    [Fact] public void ClosedCarrierRejectsMissingStateOversizedListsAndChangedPhysicalInventory()
    {
        var p=BeltBodiesPlaytestOptions.CreatePackage(Growth());var s=CampaignColonisationCycleApplication.StartCycleGame(p,Settings(30));var bytes=Wire(s);
        foreach(var change in new Action<JObject>[] {
            w=>w.Remove("BeltBodies"),w=>((JObject)w["BeltBodies"]!).Remove("LastShipments"),
            w=>{var a=(JArray)w["BeltBodies"]!["Definitions"]!;while(a.Count<=BeltBodiesEditionIdentity.MaximumBodies)a.Add(a[0].DeepClone());},
            w=>{var a=(JArray)w["BeltBodies"]!["Mines"]!;while(a.Count<=BeltBodiesEditionIdentity.MaximumMines)a.Add(new JObject());},
            w=>{var a=(JArray)w["BeltBodies"]!["Operations"]!;while(a.Count<=BeltBodiesEditionIdentity.MaximumCommands)a.Add(new JObject());},
            w=>{var a=(JArray)w["BeltBodies"]!["LastShipments"]!;while(a.Count<=BeltBodiesEditionIdentity.MaximumLastShipments)a.Add(new JObject());}})
        {var wire=JObject.Parse(System.Text.Encoding.UTF8.GetString(bytes));change(wire);Assert.ThrowsAny<Exception>(()=>codec.DecodeCycleRoot(System.Text.Encoding.UTF8.GetBytes(wire.ToString())));}
        var root=Assert.IsType<CampaignBeltBodiesSaveDto>(codec.DecodeCycleRoot(bytes));root.BeltBodies!.Deposits![0].Remaining++;Assert.ThrowsAny<Exception>(()=>CampaignColonisationCycleApplication.LoadCycleSave(p,root));
        root=Assert.IsType<CampaignBeltBodiesSaveDto>(codec.DecodeCycleRoot(bytes));root.BeltBodies!.Definitions![0].HabitabilityBps=10000;Assert.ThrowsAny<Exception>(()=>CampaignColonisationCycleApplication.LoadCycleSave(p,root));
        root=Assert.IsType<CampaignBeltBodiesSaveDto>(codec.DecodeCycleRoot(bytes));root.BeltBodies!.Deposits=Enumerable.Repeat(root.BeltBodies.Deposits![0],BeltBodiesEditionIdentity.MaximumBodies+1).ToList();Assert.ThrowsAny<Exception>(()=>CampaignColonisationCycleApplication.LoadCycleSave(p,root));
        Assert.Equal(bytes,Wire(s));
    }
}
