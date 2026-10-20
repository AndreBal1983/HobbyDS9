using HobbyDS9.UnityReview;
using Xunit;

namespace HobbyDS9.UnityReviewAdapter.Tests;

public sealed class GeneratedGalaxyMapStoreTests : IDisposable
{
    private readonly string directory=Path.Combine(Path.GetTempPath(),"HobbyDS9-GalaxyStore-"+Guid.NewGuid().ToString("N"));
    public void Dispose(){if(Directory.Exists(directory))Directory.Delete(directory,true);}
    [Fact] public void ExportsAreSeparateAndOverwriteRequiresExplicitChoice()
    {
        var store=new GeneratedGalaxyMapStore(directory);var original=new byte[]{1,2,3};
        store.Write(1,original,false);Assert.Equal(original,store.Read(1));Assert.False(store.Exists(2));
        Assert.Throws<IOException>(()=>store.Write(1,new byte[]{4},false));Assert.Equal(original,store.Read(1));
        Assert.Throws<ArgumentOutOfRangeException>(()=>store.Write(4,original,false));
        store.Write(1,new byte[]{4},true);Assert.Equal(new byte[]{4},store.Read(1));
        Assert.Single(Directory.GetFiles(directory));
    }
    [Theory]
    [InlineData(UnityReviewWriteStage.AfterPartialWrite)]
    [InlineData(UnityReviewWriteStage.BeforeReplace)]
    public void FailedWriteRetainsPreviousExportAndRemovesItsOwnTemporaryFile(UnityReviewWriteStage failure)
    {
        var store=new GeneratedGalaxyMapStore(directory);store.Write(1,new byte[]{1,2,3},false);
        var failing=new GeneratedGalaxyMapStore(directory,stage=>{if(stage==failure)throw new IOException("Injected write failure.");});
        Assert.Throws<IOException>(()=>failing.Write(1,new byte[]{8,9},true));
        Assert.Equal(new byte[]{1,2,3},store.Read(1));Assert.Single(Directory.GetFiles(directory));
    }
    [Fact] public void OversizedReadRejectedWithoutTouchingOtherFiles()
    {
        Directory.CreateDirectory(directory);var path=Path.Combine(directory,"generated-map-1.json");
        using(var file=File.Create(path))file.SetLength(GeneratedGalaxyMapStore.MaximumBytes+1L);
        var store=new GeneratedGalaxyMapStore(directory);
        Assert.Throws<InvalidDataException>(()=>store.Read(1));Assert.Equal(GeneratedGalaxyMapStore.MaximumBytes+1L,new FileInfo(path).Length);
    }
}
