using System.Diagnostics;
using HobbyDS9.Contracts;
using HobbyDS9.UnityReview;
using Newtonsoft.Json.Linq;
using Xunit;

namespace HobbyDS9.UnityReviewAdapter.Tests;

public sealed class ScaledGalaxyWorkspaceTests : IDisposable
{
    private readonly string directory=Path.Combine(Path.GetTempPath(),"HobbyDS9-ScaledWorkspace-"+Guid.NewGuid().ToString("N"));
    private readonly ReviewNewtonsoftCodec codec=new();
    private readonly Xunit.Abstractions.ITestOutputHelper output;
    public ScaledGalaxyWorkspaceTests(Xunit.Abstractions.ITestOutputHelper output){this.output=output;}
    public void Dispose(){if(Directory.Exists(directory))Directory.Delete(directory,true);}
    private GeneratedGalaxyWorkspace Workspace()=>new(codec.DecodeScenario(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,
        "scenarios/playable-fleet-v1/scenario.json"))).Definitions.Civilizations!,new GeneratedGalaxyMapStore(directory));

    [Theory][InlineData("42",250)][InlineData("43",250)][InlineData("42",1000)]
    public void ActualScaledGenerateOpenExportRouteAndSiteSummaryMatchCommittedPortableSamples(string seed,int size)
    {
        var path=Path.Combine(AppContext.BaseDirectory,"galaxy-samples/scaled/seed-"+seed+"-systems-"+size);
        var expected=codec.DecodeScaledGalaxy(File.ReadAllBytes(path+".scaled.json"));var report=JObject.Parse(File.ReadAllText(path+".report.json"));
        var workspace=Workspace();workspace.Seed=seed;workspace.Size=size;
        var timer=Stopwatch.StartNew();Assert.True(workspace.Generate(),workspace.Notice);timer.Stop();
        var document=workspace.Document!;Assert.Equal(expected.ContentHash,document.ContentHash);Assert.NotNull(document.Scaled);Assert.Null(document.Environment);
        output.WriteLine("Seed {0} / {1}: Generate {2:F1}ms",seed,size,timer.Elapsed.TotalMilliseconds);
        Assert.Equal(codec.EncodeScaledGalaxy(expected),workspace.ExportBytes());
        foreach(var star in expected.Features.Where(f=>f.Kind==GalaxyEnvironmentFeatureKind.Star))
        {Assert.Equal(star.Position.X,document.SystemPosition(star.SystemId!).X);Assert.Equal(star.Position.Y,document.SystemPosition(star.SystemId!).Y);}
        var summary=document.ScaleSummary!;var measured=report["Diagnostics"]!;
        Assert.Equal(measured["QuietEmptyNodeIds"]!.Values<string>(),summary.QuietNodes);
        Assert.Equal(measured["DisturbedEmptyNodeIds"]!.Values<string>(),summary.DisturbedNodes);
        Assert.Equal(measured["QuietComponentCount"]!.Value<int>(),summary.QuietComponentCount);
        Assert.Equal(measured["QuietEmptySystemMeanAreaRatio"]!.Value<double>(),summary.QuietToSystemMeanArea!.Value,10);
        Assert.Equal(measured["AllEmptyAreaShare"]!.Value<double>(),summary.EmptyAreaShare,10);
        string origin=report["OriginNodeId"]!.Value<string>()!,destination=report["DestinationNodeId"]!.Value<string>()!;
        var route=workspace.FindRoute(origin,destination);Assert.Equal(report["Route"]!["Steps"]!.Values<string>(),route.Steps);
        Assert.Equal(report["Route"]!["TotalCost"]!.Value<long>(),route.TotalCost);
        var portals=GeneratedGalaxyRoutePath.Build(document.Regions.ToDictionary(r=>r.NodeId),origin,route.Steps);Assert.Equal(1+2*route.Steps.Count,portals.Count);
        Assert.True(workspace.Export(false));var reopened=Workspace();var openTimer=Stopwatch.StartNew();Assert.True(reopened.Open(),reopened.Notice);openTimer.Stop();
        output.WriteLine("Host strict Open {0:F1}ms; compact bytes {1}; hash {2}",openTimer.Elapsed.TotalMilliseconds,workspace.ExportBytes().Length,document.ContentHash);
        Assert.Equal(workspace.ExportBytes(),reopened.ExportBytes());Assert.Equal(route.Steps,reopened.FindRoute(origin,destination).Steps);
        // A semantically corrupt scaled file must preserve current map, draft, route and export.
        var bytes=workspace.ExportBytes();var broken=codec.DecodeScaledGalaxy(bytes);broken.Features[0].Position.X++;
        File.WriteAllBytes(Path.Combine(directory,"generated-map-1.json"),codec.EncodeScaledGalaxy(broken));
        workspace.Seed="98765";workspace.Size=1000;workspace.QuietEmptyAreaTargetPermille=3000;
        Assert.False(workspace.Open());Assert.Same(document,workspace.Document);Assert.Equal("98765",workspace.Seed);Assert.Equal(1000,workspace.Size);
        Assert.Equal(3000,workspace.QuietEmptyAreaTargetPermille);Assert.Equal(bytes,workspace.ExportBytes());Assert.Equal(route.Steps,workspace.FindRoute(origin,destination).Steps);
        Assert.True(timer.Elapsed<TimeSpan.FromSeconds(30),"Bounded CPU preparation watchdog, not Unity acceptance");
    }

    [Fact] public async Task BackgroundPreparationCannotPublishUntilRequestedAndStaleCompletionIsRejected()
    {
        var live=Workspace();live.Seed="42";Assert.True(live.Generate());var original=live.Document;var bytes=live.ExportBytes();
        var operation=live.CaptureOperation();operation.Seed="43";
        Assert.True(await Task.Run(()=>operation.Generate()));Assert.Same(original,live.Document);Assert.Equal(bytes,live.ExportBytes());
        Assert.True(live.PublishOperation(operation,original));Assert.Same(operation.Document,live.Document);
        var replacement=live.Document;
        Assert.False(live.PublishOperation(operation,original));Assert.Same(replacement,live.Document);
        var failed=live.CaptureOperation();failed.Seed="invalid";Assert.False(await Task.Run(()=>failed.Generate()));
        Assert.False(live.PublishOperation(failed,replacement));Assert.Same(replacement,live.Document);Assert.Equal("43",live.Seed);
    }

    [Fact] public void NearestActualStarWinsIndependentlyOfInsertionOrderAndStableTies()
    {
        var stars=new[]{("b",new GeneratedGalaxyPointDto{X=10,Y=10}),("a",new GeneratedGalaxyPointDto{X=12,Y=10}),
            ("c",new GeneratedGalaxyPointDto{X=5000000,Y=4000000})};
        foreach(var order in new[]{stars,stars.Reverse().ToArray()})
        {
            var index=new GeneratedGalaxyStarIndex(order,5000000,4000000);
            Assert.Equal("a",index.Nearest(12,10,13));Assert.Equal("b",index.Nearest(10,10,13));Assert.Equal("a",index.Nearest(11,10,13));
            Assert.Equal("c",index.Nearest(5000000,4000000,1));Assert.Null(index.Nearest(100000,100000,13));
            Assert.Null(index.Nearest(double.NaN,0,13));
        }
    }
}
