using System.Text.Json;
using HobbyDS9.Contracts;
using HobbyDS9.TacticalReviewClient;
using HobbyDS9.UnityReview;
using Xunit;

namespace HobbyDS9.UnityReviewAdapter.Tests;

public sealed class GeneratedCampaignBindingTests : IDisposable
{
    private readonly string root=Path.Combine(Path.GetTempPath(),"hobby-generated-binding-"+Guid.NewGuid().ToString("N"));
    private readonly ReviewNewtonsoftCodec codec=new();
    private static CampaignPlayableFleetPackageDto Template()=>PlayableFleetPackageReader.Read(Path.Combine(AppContext.BaseDirectory,"scenarios/playable-fleet-v1"));
    private ReviewGeneratedCampaignSlotStore Store()=>new(root,codec);
    private static NaturalGalaxySettingsDto Settings()=>new(){Map=new(){Seed="42",SystemCount=250,EmptyRegionCount=250,
        Width=2500000,Height=2000000,QuietEmptyAreaTargetPermille=750,
        HomeCivilizationDefinitionIds=new(){"civilization.federation","civilization.cardassian"}}};
    private ReviewUiModel Start()=>new(Template(),Settings(),codec,Store());
    private static string Json<T>(T value)=>JsonSerializer.Serialize(value);
    [Fact] public void ActualGeneratedModelUsesSamePaidConstructionResearchSessionAndFreshContinueFrozenMap()
    {
        var m=Start();Assert.True(m.GeneratedCampaign);Assert.True(m.PlayableFleet);Assert.True(m.Integrated);
        Assert.False(m.SwitchActor("civ.cardassian"));
        var originalMap=m.GeneratedMap();Assert.InRange(originalMap.Regions.Count,2,20);
        var firstGeometry=Json(originalMap);originalMap.Regions.Clear();Assert.NotEmpty(m.GeneratedMap().Regions);
        Assert.Equal(firstGeometry,Json(m.GeneratedMap()));
        var own=m.Current.Frame.Strategy!;var colony=own.Construction.First(c=>c.CanEnqueue&&c.Design.DefinitionId.Value=="ship-design.source-cruiser");
        Assert.True(m.AllocateResearch(300,"command.generated.binding.research"));
        Assert.True(m.Enqueue(colony.ColonyId.Value,"ship-design.source-cruiser","command.generated.binding.build"),m.Message);
        for(int i=0;i<5;i++)Assert.True(m.Advance());
        Assert.True(m.Save("one"));var bytes=File.ReadAllBytes(Path.Combine(root,"unity-generated-campaign-v7-one.json"));
        var saved=Store().Read("one");Assert.Equal(7,saved.FormatVersion);Assert.Equal(250,saved.Map.Layout.Systems.Count);
        Assert.Equal(saved.Map.ContentHash,m.GeneratedMap().MapContentHash);
        Assert.Equal(750,saved.Map.Settings.Map.QuietEmptyAreaTargetPermille);
        Assert.NotEmpty(saved.Campaign.Integrated!.BuiltShips!);
        var fresh=ReviewUiModel.ContinueGeneratedCampaign(Template(),codec,Store(),"one");
        Assert.Equal(Json(m.Current.Frame.Strategy),Json(fresh.Current.Frame.Strategy));
        Assert.Equal(Json(m.GeneratedMap()),Json(fresh.GeneratedMap()));
        Assert.Equal(Json(m.NamedFleets()),Json(fresh.NamedFleets()));
        Assert.True(fresh.Save("two"));Assert.Equal(bytes,File.ReadAllBytes(Path.Combine(root,"unity-generated-campaign-v7-two.json")));
        Assert.True(m.Advance());Assert.True(fresh.Advance());Assert.True(m.Save("one",true));Assert.True(fresh.Save("two",true));
        Assert.Equal(File.ReadAllBytes(Path.Combine(root,"unity-generated-campaign-v7-one.json")),File.ReadAllBytes(Path.Combine(root,"unity-generated-campaign-v7-two.json")));
    }
    [Fact] public void LateFrozenMapValidationFailureKeepsLiveModelDraftAndExistingRoot()
    {
        var m=Start();Assert.True(m.Save("good"));var path=Path.Combine(root,"unity-generated-campaign-v7-good.json");var bytes=File.ReadAllBytes(path);
        var broken=Store().Read("good");broken.Map.Settings.Map.Seed="43";
        Store().Write("invalid",broken,false); // Valid carrier, invalid frozen-map identity.
        var current=m.Current;var draft=m.ResearchInputs()!;draft.Adjust(ScienceDiscipline.Biotech,6);
        var map=Json(m.GeneratedMap());
        Assert.False(m.Load("invalid",true));Assert.Same(current,m.Current);Assert.Same(draft,m.ResearchDraft);Assert.True(m.HasLocalEdits);
        Assert.Equal(map,Json(m.GeneratedMap()));Assert.Equal(bytes,File.ReadAllBytes(path));
        Assert.Throws<InvalidOperationException>(()=>ReviewUiModel.ContinueGeneratedCampaign(Template(),codec,Store(),"invalid"));
        Assert.True(m.Load("good",true));Assert.False(m.HasLocalEdits);Assert.Equal(bytes,File.ReadAllBytes(path));
    }
    public void Dispose(){if(Directory.Exists(root))Directory.Delete(root,true);}
}
