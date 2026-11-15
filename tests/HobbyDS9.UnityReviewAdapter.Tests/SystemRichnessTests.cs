using System.Text.Json;
using HobbyDS9.Application;
using HobbyDS9.Contracts;
using HobbyDS9.UnityReview;
using Newtonsoft.Json.Linq;
using Xunit;
using Xunit.Abstractions;
namespace HobbyDS9.UnityReviewAdapter.Tests;

public sealed class SystemRichnessTests
{
    const string Human="civ.federation";
    readonly ReviewNewtonsoftCodec codec=new();
    readonly ITestOutputHelper output;
    public SystemRichnessTests(ITestOutputHelper output)=>this.output=output;
    static byte[] Read(string folder,string name)=>File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,"scenarios",folder,name+".json"));
    static string Json(object? o)=>JsonSerializer.Serialize(o,o?.GetType()??typeof(object));
    CampaignColonisationExpansionPackageDto Growth(byte[]? scenario=null)
    {
        var template=ReviewExactPackageLoader.LoadPlayableFleet(Read("playable-fleet-v1","playable"),Read("playable-fleet-v1","package"),Read("playable-fleet-v1","physical"),Read("playable-fleet-v1","core"),scenario??Read("sol-eight-planets-v1","scenario"),codec);
        var activities=ActivitiesPlaytestOptions.CreatePackage(template,new[]{"catalogue","ready-classes","support","activities"}.Select(n=>Read("activities-v1",n)).ToArray(),codec);
        var colony=ColonisationPlaytestOptions.CreatePackage(activities,new[]{"catalogue","ready-classes","support","settlement"}.Select(n=>Read("colonisation-v1",n)).ToArray(),codec);
        var roster=ColonisationPlaytestOptions.CreateRosterPackage(colony,Read("colonisation-roster-v2","roster"),codec);
        return ColonisationGrowthPlaytestOptions.CreatePackage(roster,Read("colonisation-expansion-v1","planner"),codec);
    }
    CampaignSystemRichnessPackageDto Package(byte[]? scenario=null)=>ColonisationCyclePlaytestOptions.CreateRichnessPackage(Growth(scenario),Read("system-richness-v1","policy"),codec);
    static NaturalGalaxySettingsDto Settings(int size=30)=>new(){Map=new(){Seed="42",SystemCount=size,EmptyRegionCount=size,
        Width=size==30?750000:size==250?2500000:5000000,Height=size==30?600000:size==250?2000000:4000000,HomeCivilizationDefinitionIds=new(){"civilization.federation","civilization.cardassian"}}};
    byte[] Wire(ICampaignColonisationCycleSession s)=>codec.EncodeCycleRoot(s.ExportCycleSave());
    sealed class ForeignPackage:CampaignColonisationCyclePackageDto {public int Extra {get;set;}}
    sealed class ForeignSave:CampaignColonisationCycleSaveDto {public int Extra {get;set;}}
    static GameStateDto Game(CampaignColonisationCycleSaveDto root)=>root.BaseCampaign!.Campaign!.Integrated!.Physical!.Core.Campaign.GameState;

    [Fact]
    public void TypedHostAndSeparateSlotStoreRetainTheCompleteRichnessCarrier()
    {
        var package=Package();var session=CampaignColonisationCycleApplication.StartCycleGame(package,Settings());
        var directory=Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"richness-store-"+Guid.NewGuid().ToString("N")));
        var store=new ReviewColonisationCycleSlotStore(directory,codec);
        var model=new ReviewUiModel(package,Settings(),codec,store);
        Assert.True(model.ColonisationCycleCampaign);
        store.Write("richness",session.ExportCycleSave(),false);
        var saved=Assert.IsType<CampaignSystemRichnessSaveDto>(store.Read("richness"));
        Assert.Equal(Wire(session),codec.EncodeCycleRoot(saved));
        var continued=ReviewUiModel.ContinueColonisationCycle(package,codec,store,"richness");
        Assert.True(continued.ColonisationCycleCampaign);
        Assert.Equal(model.ColonisationPlanets().Select(p=>p.PlanetId),continued.ColonisationPlanets().Select(p=>p.PlanetId));
        Assert.Equal(codec.EncodeCycleRoot(saved),codec.EncodeCycleRoot(store.Read("richness")));
    }

    [Fact]
    public void RecipeAndPolymorphicCarrierAdmissionRemainClosed()
    {
        var growth=Growth();
        Assert.ThrowsAny<Exception>(()=>CampaignColonisationCycleApplication.CreateSystemRichnessPackage(growth,new(){MinimumPlanets=1}));
        Assert.ThrowsAny<Exception>(()=>CampaignColonisationCycleApplication.CreateSystemRichnessPackage(growth,new(){BeltChanceBps=10000}));
        Assert.ThrowsAny<Exception>(()=>codec.CopyCyclePackage(new ForeignPackage{BaseGrowth=growth}));
        Assert.ThrowsAny<Exception>(()=>codec.EncodeCycleRoot(new ForeignSave()));
        var root=Assert.IsType<CampaignSystemRichnessSaveDto>(CampaignColonisationCycleApplication.StartCycleGame(Package(),Settings()).ExportCycleSave());
        root.SystemBelts=Enumerable.Range(0,1001).Select(i=>new SystemAsteroidBeltDto()).ToList();
        Assert.ThrowsAny<Exception>(()=>codec.EncodeCycleRoot(root));
    }

    [Fact]
    public void RichEditionAddsRealBodiesButPreservesMapExistingPlanetFactsColoniesAndCampaignRng()
    {
        var growth=Growth();var oldPackage=CampaignColonisationCycleApplication.CreateCyclePackage(growth);var richPackage=Package();
        var old=CampaignColonisationCycleApplication.StartCycleGame(oldPackage,Settings());
        var rich=CampaignColonisationCycleApplication.StartCycleGame(richPackage,Settings());
        var before=old.ExportCycleSave();var after=Assert.IsType<CampaignSystemRichnessSaveDto>(rich.ExportCycleSave());
        Assert.NotEqual(oldPackage.PackageHash,richPackage.PackageHash);
        Assert.Equal(Json(before.BaseCampaign!.Map),Json(after.BaseCampaign!.Map));
        Assert.Equal(Json(Game(before).RngState),Json(Game(after).RngState));
        Assert.Equal(Game(before).NextEntitySequence,Game(after).NextEntitySequence);
        Assert.Equal(Json(Game(before).Colonies),Json(Game(after).Colonies));
        Assert.Equal(Json(Game(before).Civilizations),Json(Game(after).Civilizations));
        var original=before.BaseCampaign.FrozenPlanets!;var added=after.BaseCampaign.FrozenPlanets!;
        foreach(var planet in original.InitialPlanets!)Assert.Equal(Json(planet),Json(added.InitialPlanets!.Single(p=>p.PlanetId==planet.PlanetId)));
        foreach(var d in original.Definitions!)Assert.Equal(Json(d),Json(added.Definitions!.Single(p=>p.DefinitionId==d.DefinitionId)));
        var sol=rich.CaptureColonisationPlanets(Human).Single(p=>p.PlanetId=="planet.sol").SystemId;
        Assert.Equal(8,added.InitialPlanets!.Count(p=>p.SystemId==sol));
        var homes=after.BaseCampaign!.Map!.Layout.HomeCandidates.Select(h=>h.SystemId).ToHashSet();
        Assert.All(added.InitialPlanets!.GroupBy(p=>p.SystemId).Where(g=>!homes.Contains(g.Key)),g=>Assert.InRange(g.Count(),3,6));
        Assert.Contains(added.Definitions!,p=>p.PlanetClass=="Vacuum"&&p.Habitats!.All(h=>h.NaturalCapacity==0)&&p.Deposits!.Count==0);
        Assert.Contains(added.Definitions!,p=>p.Deposits!.Count>0);
        var originalIds=original.InitialPlanets!.Select(p=>p.PlanetId).ToHashSet();
        Assert.All(added.GenesisControls!.Where(c=>!originalIds.Contains(c.PlanetId)),c=>{Assert.Equal(PlanetControllerKind.Unclaimed,c.Kind);Assert.Null(c.ActorId);});
        Assert.Empty(rich.CaptureKnownAstronomy(Human).SystemBelts); // A charted neighbour is not physical system disclosure.
        var wire=Wire(rich);
        for(int i=0;i<5;i++){rich.CaptureKnownAstronomy(Human);rich.CaptureColonisationPlanets(Human);rich.CaptureGeneratedMap(Human);}
        Assert.Equal(wire,Wire(rich));
        Assert.Equal(20,rich.CaptureStrategy(Human).Research.Engineering.Count);
    }

    [Fact]
    public void CompleteRichCarrierRoundtripsRejectsTamperingAndDoesNotAcceptEarlierOrTruncatedEditions()
    {
        var package=Package();Assert.IsType<CampaignSystemRichnessPackageDto>(codec.CopyCyclePackage(package));
        var session=CampaignColonisationCycleApplication.StartCycleGame(package,Settings());
        var bytes=Wire(session);var root=Assert.IsType<CampaignSystemRichnessSaveDto>(codec.DecodeCycleRoot(bytes));
        Assert.Equal(15,root.FormatVersion);Assert.Equal(CampaignSystemRichnessIdentity.Inventory,root.BaseCampaign!.FrozenPlanets!.PolicyId);
        Assert.NotEmpty(root.SystemBelts!);
        var reopened=CampaignColonisationCycleApplication.LoadCycleSave(package,root);Assert.Equal(bytes,Wire(reopened));
        var bad=Assert.IsType<CampaignSystemRichnessSaveDto>(codec.CopyCycleSave(root));bad.SystemBelts![0].VisualSeed=new string('0',64);
        Assert.ThrowsAny<Exception>(()=>CampaignColonisationCycleApplication.LoadCycleSave(package,bad));
        bad=Assert.IsType<CampaignSystemRichnessSaveDto>(codec.CopyCycleSave(root));bad.SystemBelts!.Add(bad.SystemBelts[0]);
        Assert.ThrowsAny<Exception>(()=>CampaignColonisationCycleApplication.LoadCycleSave(package,bad));
        var oldPackage=CampaignColonisationCycleApplication.CreateCyclePackage(Growth());var old=CampaignColonisationCycleApplication.StartCycleGame(oldPackage,Settings());
        Assert.ThrowsAny<Exception>(()=>CampaignColonisationCycleApplication.LoadCycleSave(oldPackage,root));
        Assert.ThrowsAny<Exception>(()=>CampaignColonisationCycleApplication.LoadCycleSave(package,old.ExportCycleSave()));
        var truncated=JObject.Parse(System.Text.Encoding.UTF8.GetString(bytes));truncated.Remove("SystemBelts");
        Assert.ThrowsAny<Exception>(()=>codec.DecodeCycleRoot(System.Text.Encoding.UTF8.GetBytes(truncated.ToString())));
        Assert.Equal(bytes,Wire(session));
        var stamp=session.CaptureStatus();Assert.True(session.AdvanceTurn(stamp.Turn,stamp.CampaignRevision).Accepted);
        var next=Wire(session);Assert.Equal(next,Wire(CampaignColonisationCycleApplication.LoadCycleSave(package,codec.DecodeCycleRoot(next))));
    }

    [Fact]
    public void BeltsBecomeVisibleOnlyAfterLawfulSystemArrivalAndRemainKnownAfterDeparture()
    {
        var package=Package();var session=CampaignColonisationCycleApplication.StartCycleGame(package,Settings());
        var root=Assert.IsType<CampaignSystemRichnessSaveDto>(session.ExportCycleSave());
        var knownNodes=session.CaptureGeneratedMap(Human).Regions.Select(r=>r.NodeId).ToHashSet();
        var beltSystems=root.SystemBelts!.Select(b=>b.SystemId).ToHashSet();
        var target=root.BaseCampaign!.Map!.Layout.Systems.First(s=>knownNodes.Contains(s.NodeId)&&beltSystems.Contains(s.SystemId));
        var fleet=Assert.Single(session.GetOwnStrategicFleets(Human));var home=fleet.LocationNodeId.Value;
        void Travel(string node,string id)
        {
            var here=Assert.Single(session.GetOwnStrategicFleets(Human)).LocationNodeId.Value;
            var route=session.FindKnownRoute(Human,here,node);Assert.True(route.Success);
            var stamp=session.CaptureStatus();
            Assert.True(session.SubmitTravelOrder(new(){ActorId=Human,CommandId=id,ExpectedTurn=stamp.Turn,ExpectedRevision=stamp.CampaignRevision,
                Order=new(){FleetId=fleet.FleetId.Value,Intent=CampaignTravelIntent.Leave,RouteNodeIds=route.Steps.ToList()}}).Accepted);
            for(int i=0;i<4&&Assert.Single(session.GetOwnStrategicFleets(Human)).LocationNodeId.Value!=node;i++)
            {stamp=session.CaptureStatus();Assert.True(session.AdvanceTurn(stamp.Turn,stamp.CampaignRevision).Accepted);}
            Assert.Equal(node,Assert.Single(session.GetOwnStrategicFleets(Human)).LocationNodeId.Value);
        }
        Assert.Empty(session.CaptureKnownAstronomy(Human).SystemBelts);
        Travel(target.NodeId,"command.richness.arrive");
        var visible=Assert.Single(session.CaptureKnownAstronomy(Human).SystemBelts,b=>b.SystemId==target.SystemId);
        var frozen=root.SystemBelts!.Single(b=>b.SystemId==target.SystemId);
        Assert.Equal(frozen.VisualSeed,visible.VisualSeed);Assert.Equal(frozen.InnerRadiusMilliAu,visible.InnerRadiusMilliAu);
        Assert.Equal(CampaignSystemRichnessIdentity.Disclosure,visible.DisclosurePolicy);
        Assert.All(session.CaptureColonisationPlanets(Human).Where(p=>p.SystemId==target.SystemId),p=>{Assert.False(p.Surveyed);Assert.Null(p.Environment);});
        Travel(home,"command.richness.depart");
        Assert.Contains(session.CaptureKnownAstronomy(Human).SystemBelts,b=>b.FeatureId==visible.FeatureId);
        var save=Wire(session);var loaded=CampaignColonisationCycleApplication.LoadCycleSave(package,codec.DecodeCycleRoot(save));
        Assert.Equal(save,Wire(loaded));Assert.Equal(Json(session.CaptureKnownAstronomy(Human).SystemBelts),Json(loaded.CaptureKnownAstronomy(Human).SystemBelts));
    }

    [Fact]
    public void ReorderedSourceBodyCollectionsDoNotShufflePhysicalGenerationOrBelts()
    {
        var raw=JObject.Parse(System.Text.Encoding.UTF8.GetString(Read("sol-eight-planets-v1","scenario")));
        foreach(var path in new[]{"Definitions.Planets","InitialState.Planets","InitialState.Colonies"})
        {var a=(JArray)raw.SelectToken(path)!;var values=a.Reverse().Select(x=>x.DeepClone()).ToArray();a.Replace(new JArray(values));}
        var first=Assert.IsType<CampaignSystemRichnessSaveDto>(CampaignColonisationCycleApplication.StartCycleGame(Package(),Settings()).ExportCycleSave());
        var second=Assert.IsType<CampaignSystemRichnessSaveDto>(CampaignColonisationCycleApplication.StartCycleGame(Package(System.Text.Encoding.UTF8.GetBytes(raw.ToString())),Settings()).ExportCycleSave());
        Assert.Equal(first.BaseCampaign!.FrozenPlanets!.ContentHash,second.BaseCampaign!.FrozenPlanets!.ContentHash);
        Assert.Equal(Json(first.SystemBelts),Json(second.SystemBelts));
    }

    [Theory][InlineData(250)][InlineData(1000)]
    public void BoundedDefaultDistributionAndMaximumCarrierStayLoadable(int size)
    {
        var watch=System.Diagnostics.Stopwatch.StartNew();
        var package=Package();var session=CampaignColonisationCycleApplication.StartCycleGame(package,Settings(size));
        var root=Assert.IsType<CampaignSystemRichnessSaveDto>(session.ExportCycleSave());
        var homes=root.BaseCampaign!.Map!.Layout.HomeCandidates.Select(h=>h.SystemId).ToHashSet();
        var counts=root.BaseCampaign.FrozenPlanets!.InitialPlanets!.GroupBy(p=>p.SystemId).Where(g=>!homes.Contains(g.Key)).Select(g=>g.Count()).ToArray();
        Assert.Equal(size-2,counts.Length);Assert.All(counts,n=>Assert.InRange(n,3,6));
        Assert.InRange(counts.Average(),4.2,4.8);Assert.Equal(new[]{3,4,5,6},counts.Distinct().OrderBy(n=>n));
        Assert.InRange(root.SystemBelts!.Count,(int)((size-2)*.6),(int)((size-2)*.9));
        Assert.True(root.BaseCampaign.FrozenPlanets!.InitialPlanets!.Count<=6040);
        var bytes=Wire(session);Assert.True(bytes.Length<ReviewColonisationCycleSlotStore.MaximumBytes);
        var loaded=CampaignColonisationCycleApplication.LoadCycleSave(package,codec.DecodeCycleRoot(bytes));Assert.Equal(bytes,Wire(loaded));
        output.WriteLine(Json(new{Systems=size,Planets=root.BaseCampaign.FrozenPlanets.InitialPlanets.Count,MeanGenerated=counts.Average(),Distribution=counts.GroupBy(n=>n).OrderBy(g=>g.Key).ToDictionary(g=>g.Key,g=>g.Count()),Belts=root.SystemBelts.Count,Bytes=bytes.Length,ElapsedMilliseconds=watch.ElapsedMilliseconds}));
    }
}
