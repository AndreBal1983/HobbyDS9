using HobbyDS9.Contracts;
using HobbyDS9.UnityReview;
using Xunit;

namespace HobbyDS9.UnityReviewAdapter.Tests;

public sealed class GeneratedGalaxyWorkspaceTests : IDisposable
{
    private readonly string directory=Path.Combine(Path.GetTempPath(),"HobbyDS9-GalaxyWorkspace-"+Guid.NewGuid().ToString("N"));
    private static IReadOnlyList<CivilizationDefinitionDto> Catalog()
    {
        var path=Path.Combine(AppContext.BaseDirectory,"scenarios/playable-fleet-v1/scenario.json");
        return new ReviewNewtonsoftCodec().DecodeScenario(File.ReadAllBytes(path)).Definitions.Civilizations!;
    }
    private GeneratedGalaxyWorkspace Workspace()=>new(Catalog(),new GeneratedGalaxyMapStore(directory));
    public void Dispose(){if(Directory.Exists(directory))Directory.Delete(directory,true);}

    [Theory]
    [InlineData("42",40,"6e306b02c09344db583fd0871f806e42549e81a06697f53dd124fa1305660a8f")]
    [InlineData("12345",30,"85dac90f91180327f0fad3b915ca3145fdd8a1d1108d7b4f543c8c300d655884")]
    [InlineData("20261020",50,"c6d6dfcbee54c17607d5090f47ad403f5ba158a7d96efeadcc764da7a2d8b6cd")]
    public void RealGenerateExportReopenMatchesPrimaryDataAndGraph(string seed,int size,string hash)
    {
        var workspace=Workspace();workspace.Seed=seed;workspace.Size=size;Assert.True(workspace.Generate(),workspace.Notice);
        var before=workspace.Document!;Assert.Equal(hash,before.ContentHash);Assert.Equal(size,before.Systems.Count);Assert.Equal(size*2,before.Regions.Count);
        var source=before.HomeCandidates[0].NodeId;var target=before.HomeCandidates[1].NodeId;
        var route=workspace.FindRoute(source,target);Assert.True(route.Success);Assert.Equal(target,route.Steps.Last());
        Assert.NotEqual(source,route.Steps[0]);Assert.Equal(route.Steps.Count,route.TotalCost);
        Assert.True(workspace.Unexported);Assert.True(workspace.Export(false));Assert.False(workspace.Unexported);
        var reopened=Workspace();Assert.True(reopened.Open(),reopened.Notice);
        Assert.Equal(hash,reopened.Document!.ContentHash);Assert.Equal(seed,reopened.Seed);Assert.Equal(size,reopened.Size);
        Assert.Equal(route.Steps,reopened.FindRoute(source,target).Steps);
        Assert.Equal(new ReviewNewtonsoftCodec().EncodeGeneratedGalaxy(before),new ReviewNewtonsoftCodec().EncodeGeneratedGalaxy(reopened.Document));
        var same=reopened.FindRoute(source,source);Assert.True(same.Success);Assert.Empty(same.Steps);Assert.Equal(0L,same.TotalCost);
    }

    [Fact] public void FailedGenerateAndOpenKeepCurrentMapAndUnexportedWork()
    {
        var workspace=Workspace();workspace.Seed="42";Assert.True(workspace.Generate());var before=workspace.Document;
        workspace.Seed="-1";Assert.False(workspace.Generate());Assert.Same(before,workspace.Document);Assert.True(workspace.Unexported);
        Assert.False(workspace.Open());Assert.Same(before,workspace.Document);
        Directory.CreateDirectory(directory);
        var path=Path.Combine(directory,"generated-map-1.json");var corrupted=new ReviewNewtonsoftCodec().EncodeGeneratedGalaxy(before!);
        File.WriteAllBytes(path,corrupted);
        var text=File.ReadAllText(path).Replace("\"Width\":1000000","\"Width\":999999");File.WriteAllText(path,text);
        Assert.False(workspace.Open());Assert.Same(before,workspace.Document);Assert.True(workspace.Unexported);
        Assert.Equal(text,File.ReadAllText(path));
    }

    [Fact] public void MissingNullableFieldIsRejectedAndFailedExportPreservesDirtyState()
    {
        var workspace=Workspace();Assert.True(workspace.Generate());var before=workspace.Document!;
        var codec=new ReviewNewtonsoftCodec();var text=System.Text.Encoding.UTF8.GetString(codec.EncodeGeneratedGalaxy(before));
        Assert.Contains("\"SystemId\":null,",text);
        Assert.Throws<InvalidDataException>(()=>codec.DecodeGeneratedGalaxy(System.Text.Encoding.UTF8.GetBytes(text.Replace("\"SystemId\":null,",""))));
        Directory.CreateDirectory(directory);Directory.CreateDirectory(Path.Combine(directory,"generated-map-1.json"));
        Assert.False(workspace.Export(true));Assert.True(workspace.Unexported);Assert.Same(before,workspace.Document);
    }
}
