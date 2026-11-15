using System.Diagnostics;
using HobbyDS9.UnityReview;
using Xunit;
using Xunit.Abstractions;

namespace HobbyDS9.UnityReviewAdapter.Tests;
public sealed class ArchiveMetadataTests:IDisposable
{
    readonly string directory=Path.Combine(Path.GetTempPath(),"HobbyDS9-Archive-Metadata-"+Guid.NewGuid().ToString("N"));
    readonly ITestOutputHelper output;
    public ArchiveMetadataTests(ITestOutputHelper output){this.output=output;Directory.CreateDirectory(directory);}
    KeyValuePair<string,string> Slot(string name)=>new(name,Path.Combine(directory,name+".json"));
    [Fact]public void DiscoveryIsAdvisoryForCorruptBytesAndIsolatesMissingAndUnavailableSlots()
    {
        var corrupt=Slot("corrupt");File.WriteAllText(corrupt.Value,"not a JSON save");
        var blocked=Slot("directory");Directory.CreateDirectory(blocked.Value);var missing=Slot("empty");
        var rows=ReviewArchiveMetadata.Read(new[]{corrupt,blocked,missing},CancellationToken.None);
        Assert.True(rows[0].Occupied);Assert.False(rows[0].Unavailable);Assert.Equal(15,rows[0].Length);
        Assert.True(rows[1].Occupied);Assert.True(rows[1].Unavailable);Assert.False(rows[2].Occupied);
        Assert.Equal(new[]{"corrupt","directory","empty"},rows.Select(r=>r.Slot));
        Assert.Throws<NotSupportedException>(()=>((IList<ReviewArchiveFile>)rows).Clear());
    }
    [Fact]public void FileChangeDeleteAndRecreateInvalidateTheMetadataObservationWithoutTrustingPayload()
    {
        var slot=Slot("one");File.WriteAllText(slot.Value,"first");
        ReviewArchiveFile Read()=>ReviewArchiveMetadata.Read(new[]{slot},CancellationToken.None)[0];
        var first=Read();Assert.True(first.SameObservation(Read()));File.AppendAllText(slot.Value," changed");
        Assert.False(first.SameObservation(Read()));File.Delete(slot.Value);Assert.False(Read().Occupied);
        File.WriteAllText(slot.Value,"new");Assert.False(first.SameObservation(Read()));
        var other=Slot("other");File.WriteAllText(other.Value,"new");Assert.False(Read().SameObservation(ReviewArchiveMetadata.Read(new[]{other},CancellationToken.None)[0]));
    }
    [Fact]public void CancellationReturnsNoPartialResultAndNextRequestRemainsIndependent()
    {
        using var cancellation=new CancellationTokenSource();var files=new[]{Slot("one"),Slot("two")};int reads=0;
        Assert.Throws<OperationCanceledException>(()=>ReviewArchiveMetadata.Read(files,cancellation.Token,_=>{reads++;cancellation.Cancel();}));
        Assert.Equal(1,reads);Assert.Equal(2,ReviewArchiveMetadata.Read(files,CancellationToken.None).Count);
    }
    [Fact]public void LargeUnvalidatedSlotsUseOnlyFileMetadataAndSingleSlotRefreshTouchesOnlyItsPath()
    {
        var files=new[]{Slot("one"),Slot("two"),Slot("three")};
        foreach(var file in files)using(var stream=File.Create(file.Value))stream.SetLength(32*1024*1024);
        var clock=Stopwatch.StartNew();var first=ReviewArchiveMetadata.Read(files,CancellationToken.None);var firstMs=clock.Elapsed.TotalMilliseconds;
        var samples=new List<double>();for(int i=0;i<20;i++){clock.Restart();ReviewArchiveMetadata.Read(files,CancellationToken.None);samples.Add(clock.Elapsed.TotalMilliseconds);}
        var paths=new List<string>();var single=ReviewArchiveMetadata.Read(new[]{files[1]},CancellationToken.None,paths.Add);
        Assert.Equal(new[]{"two"},paths);Assert.Single(single);Assert.All(first,r=>Assert.Equal(32*1024*1024,r.Length));
        output.WriteLine(System.Text.Json.JsonSerializer.Serialize(new{firstObservationMs=firstMs,medianWarmMetadataMs=samples.OrderBy(x=>x).ElementAt(10),slots=3,declaredBytes=96*1024*1024,payloadLoads=0,nativeMenuTiming=false,noThreshold=true}));
    }
    public void Dispose()
    {
        var resolved=Path.GetFullPath(directory);var temporary=Path.GetFullPath(Path.GetTempPath());
        if(!resolved.StartsWith(temporary,StringComparison.OrdinalIgnoreCase)||!Path.GetFileName(resolved).StartsWith("HobbyDS9-Archive-Metadata-",StringComparison.Ordinal))throw new InvalidOperationException();
        Directory.Delete(resolved,true);
    }
}
