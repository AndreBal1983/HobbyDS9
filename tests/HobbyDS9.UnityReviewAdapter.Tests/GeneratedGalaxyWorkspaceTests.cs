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
    [InlineData("42",40,"404d1e15057f42531a6f1da078efd78663d902dcff22e5484e6e3ba2cc8d68fe")]
    [InlineData("12345",30,"768d23805daf819671ec63a0705b85115aba66eb0a2a2aae98d648a5aaa020a7")]
    [InlineData("20261020",50,"7717edb8d52de98e20b14574aad33430333cde62397c2e531e4f4deb8d4f6a10")]
    public void RealGenerateExportReopenMatchesPrimaryDataAndGraph(string seed,int size,string expectedHash)
    {
        var workspace=Workspace();workspace.Seed=seed;workspace.Size=size;Assert.True(workspace.Generate(),workspace.Notice);
        var before=workspace.Document!;var hash=before.ContentHash;Assert.Equal(expectedHash,hash);var bytes=workspace.ExportBytes();
        Assert.Equal(2,before.FormatVersion);Assert.NotNull(before.Environment);Assert.Equal(size,before.Systems.Count);Assert.Equal(size*2,before.Regions.Count);
        Assert.Equal(size+5,before.Environment.Features.Count);
        var source=before.HomeCandidates[0].NodeId;var target=before.HomeCandidates[1].NodeId;
        var route=workspace.FindRoute(source,target);Assert.True(route.Success);Assert.Equal(target,route.Steps.Last());
        Assert.NotEqual(source,route.Steps[0]);Assert.Equal(route.Steps.Count,route.TotalCost);
        Assert.True(workspace.Unexported);Assert.True(workspace.Export(false));Assert.False(workspace.Unexported);
        var reopened=Workspace();Assert.True(reopened.Open(),reopened.Notice);
        Assert.Equal(hash,reopened.Document!.ContentHash);Assert.Equal(seed,reopened.Seed);Assert.Equal(size,reopened.Size);
        Assert.Equal(route.Steps,reopened.FindRoute(source,target).Steps);
        Assert.Equal(bytes,reopened.ExportBytes());
        var same=reopened.FindRoute(source,source);Assert.True(same.Success);Assert.Empty(same.Steps);Assert.Equal(0L,same.TotalCost);
    }

    [Fact] public void FailedGenerateAndOpenKeepCurrentMapAndUnexportedWork()
    {
        var workspace=Workspace();workspace.Seed="42";Assert.True(workspace.Generate());var before=workspace.Document;
        workspace.Seed="-1";Assert.False(workspace.Generate());Assert.Same(before,workspace.Document);Assert.True(workspace.Unexported);
        Assert.False(workspace.Open());Assert.Same(before,workspace.Document);
        Directory.CreateDirectory(directory);
        var path=Path.Combine(directory,"generated-map-1.json");var corrupted=workspace.ExportBytes();
        File.WriteAllBytes(path,corrupted);
        var text=File.ReadAllText(path).Replace("\"Width\":1000000","\"Width\":999999");File.WriteAllText(path,text);
        Assert.False(workspace.Open());Assert.Same(before,workspace.Document);Assert.True(workspace.Unexported);
        Assert.Equal(text,File.ReadAllText(path));
    }

    [Fact] public void MissingNullableFieldIsRejectedAndFailedExportPreservesDirtyState()
    {
        var workspace=Workspace();Assert.True(workspace.Generate());var before=workspace.Document!;
        var codec=new ReviewNewtonsoftCodec();var text=System.Text.Encoding.UTF8.GetString(workspace.ExportBytes());
        Assert.Contains("\"SystemId\":null,",text);
        Assert.Throws<InvalidDataException>(()=>codec.DecodeEnvironmentGalaxy(System.Text.Encoding.UTF8.GetBytes(text.Replace("\"SystemId\":null,",""))));
        Directory.CreateDirectory(directory);Directory.CreateDirectory(Path.Combine(directory,"generated-map-1.json"));
        Assert.False(workspace.Export(true));Assert.True(workspace.Unexported);Assert.Same(before,workspace.Document);
    }

    [Fact] public void EveryRetainedV1V2ExportOpensWithoutMigrationAndKeepsOriginalBytes()
    {
        var samples=Directory.GetFiles(Path.Combine(AppContext.BaseDirectory,"galaxy-samples"),"*.galaxy.json",SearchOption.AllDirectories);
        Assert.Equal(7,samples.Length);var codec=new ReviewNewtonsoftCodec();Directory.CreateDirectory(directory);
        foreach(var sample in samples)
        {
            var original=File.ReadAllBytes(sample);var expected=codec.DecodeGeneratedGalaxy(original);
            var path=Path.Combine(directory,"generated-map-1.json");File.WriteAllBytes(path,original);
            var workspace=Workspace();Assert.True(workspace.Open(),workspace.Notice);Assert.Equal(1,workspace.Document!.FormatVersion);
            Assert.Null(workspace.Document.Environment);Assert.Equal(expected.ContentHash,workspace.Document.ContentHash);
            Assert.Equal(codec.EncodeGeneratedGalaxy(expected),workspace.ExportBytes());Assert.Equal(original,File.ReadAllBytes(path));
            workspace.Slot=2;Assert.True(workspace.Export(true));Assert.True(workspace.Open());
            Assert.Equal(expected.GeneratorVersion,workspace.Document!.GeneratorVersion);Assert.Equal(original,File.ReadAllBytes(path));
        }
    }

    [Theory]
    [InlineData("feature")]
    [InlineData("contribution")]
    [InlineData("format")]
    [InlineData("malformed-json")]
    [InlineData("missing-nullable")]
    public void FailedEnvironmentOpenPreservesAuthorityDraftInputsAndExport(string mutation)
    {
        var workspace=Workspace();workspace.Seed="42";Assert.True(workspace.Generate());var before=workspace.Document!;
        var bytes=workspace.ExportBytes();var codec=new ReviewNewtonsoftCodec();var value=codec.DecodeEnvironmentGalaxy(bytes);
        if(mutation=="feature")value.Features[0].Position.X++;
        if(mutation=="contribution")value.RegionInfluences.First(i=>i.Contributions.Count>0).Contributions[0].Amount++;
        if(mutation=="format")value.FormatVersion=1;
        Directory.CreateDirectory(directory);var path=Path.Combine(directory,"generated-map-1.json");
        File.WriteAllBytes(path,codec.EncodeEnvironmentGalaxy(value));
        if(mutation=="malformed-json")File.WriteAllText(path,"{\"FormatVersion\":2,");
        if(mutation=="missing-nullable")File.WriteAllText(path,File.ReadAllText(path).Replace("\"SystemId\":null,",""));
        var invalid=File.ReadAllBytes(path);
        workspace.Seed="77";workspace.Size=30;
        var from=before.HomeCandidates[0].NodeId;var to=before.HomeCandidates[1].NodeId;var route=workspace.FindRoute(from,to);
        Assert.False(workspace.Open());Assert.Same(before,workspace.Document);Assert.Equal("77",workspace.Seed);Assert.Equal(30,workspace.Size);
        Assert.True(workspace.Unexported);Assert.Equal(bytes,workspace.ExportBytes());Assert.Equal(invalid,File.ReadAllBytes(path));
        Assert.Equal(route.Steps,workspace.FindRoute(from,to).Steps);
    }
}
