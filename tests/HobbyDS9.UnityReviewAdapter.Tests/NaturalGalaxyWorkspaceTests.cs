using System.Diagnostics;
using System.Text;
using HobbyDS9.Contracts;
using HobbyDS9.UnityReview;
using Newtonsoft.Json.Linq;
using Xunit;

namespace HobbyDS9.UnityReviewAdapter.Tests;

public sealed class NaturalGalaxyWorkspaceTests : IDisposable
{
    private readonly string directory=Path.Combine(Path.GetTempPath(),"HobbyDS9-NaturalHost-"+Guid.NewGuid().ToString("N"));
    private readonly ReviewNewtonsoftCodec codec=new();
    private readonly Xunit.Abstractions.ITestOutputHelper output;
    public NaturalGalaxyWorkspaceTests(Xunit.Abstractions.ITestOutputHelper output){this.output=output;}
    public void Dispose(){if(Directory.Exists(directory))Directory.Delete(directory,true);}
    private GeneratedGalaxyWorkspace Workspace()=>new(codec.DecodeScenario(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,
        "scenarios/playable-fleet-v1/scenario.json"))).Definitions.Civilizations!,new GeneratedGalaxyMapStore(directory));

    [Theory][InlineData("42",250)][InlineData("43",250)][InlineData("42",1000)]
    public void NaturalGenerationStrictOpenPlanAndMeasurementsMatchActualSamples(string seed,int size)
    {
        var prefix=Path.Combine(AppContext.BaseDirectory,"galaxy-samples/natural/seed-"+seed+"-systems-"+size);
        var expected=codec.DecodeNaturalGalaxy(File.ReadAllBytes(prefix+".natural.json"));
        var report=JObject.Parse(File.ReadAllText(prefix+".report.json"));
        var workspace=Workspace();workspace.NaturalGeneration=true;workspace.Seed=seed;workspace.Size=size;
        var timer=Stopwatch.StartNew();Assert.True(workspace.Generate(),workspace.Notice);timer.Stop();
        output.WriteLine("Natural {0}/{1} Generate {2:F1}ms",seed,size,timer.Elapsed.TotalMilliseconds);
        Assert.True(timer.Elapsed<TimeSpan.FromSeconds(90));
        var document=workspace.Document!;Assert.Equal(expected.ContentHash,document.ContentHash);Assert.Equal(4,document.FormatVersion);
        var geometry=report["Diagnostics"]!["GeometryAndQuietSpace"]!;var summary=document.ScaleSummary!;
        Assert.Equal(geometry["QuietEmptyNodeIds"]!.Values<string>(),summary.QuietNodes);
        Assert.Equal(geometry["DisturbedEmptyNodeIds"]!.Values<string>(),summary.DisturbedNodes);
        Assert.Equal(geometry["QuietComponentCount"]!.Value<int>(),summary.QuietComponentCount);
        Assert.Equal(geometry["QuietEmptySystemMeanAreaRatio"]!.Value<double>(),summary.QuietToSystemMeanArea!.Value,10);
        Assert.Equal(codec.EncodeNaturalGalaxy(expected),workspace.ExportBytes());
        var query=report["Query"]!.ToObject<GalaxyReviewRouteQueryDto>()!;var plan=workspace.FindRoutePlan(query);
        Assert.True(plan.Success);Assert.Equal(report["Plan"]!["Steps"]!.Values<string>(),plan.Steps);
        Assert.Equal(report["Plan"]!["TotalCost"]!.Value<long>(),plan.TotalCost);
        var before=workspace.ExportBytes();var paths=GeneratedGalaxyRoutePath.BuildPlan(document.Regions.ToDictionary(r=>r.NodeId),plan);
        Assert.Equal(plan.Legs.Count,paths.Count);Assert.Equal(before,workspace.ExportBytes());
        Assert.True(workspace.Export(false));var reopened=Workspace();timer.Restart();Assert.True(reopened.Open(),reopened.Notice);timer.Stop();
        output.WriteLine("Natural strict Open {0:F1}ms",timer.Elapsed.TotalMilliseconds);Assert.True(timer.Elapsed<TimeSpan.FromSeconds(90));
        Assert.Equal(before,reopened.ExportBytes());Assert.Equal(plan.Steps,reopened.FindRoutePlan(query).Steps);
        var broken=JObject.Parse(Encoding.UTF8.GetString(before));broken["Features"]![0]!["Position"]!["X"]=broken["Features"]![0]!["Position"]!["X"]!.Value<int>()+1;
        File.WriteAllText(Path.Combine(directory,"generated-map-1.json"),broken.ToString());
        var live=reopened.Document;Assert.False(reopened.Open());Assert.Same(live,reopened.Document);Assert.Equal(before,reopened.ExportBytes());
        Assert.Equal(plan.Steps,reopened.FindRoutePlan(query).Steps);
    }

    [Fact] public void NaturalWireAdmissionRejectsUnknownMissingAndOversizedGroupsBeforePortableOpen()
    {
        var sample=File.ReadAllText(Path.Combine(AppContext.BaseDirectory,"galaxy-samples/natural/seed-42-systems-250.natural.json"));
        void Reject(Action<JObject> change){var root=JObject.Parse(sample);change(root);Assert.Throws<InvalidDataException>(()=>codec.DecodeNaturalGalaxy(Encoding.UTF8.GetBytes(root.ToString())));}
        Reject(r=>r["Ignored"]=1);Reject(r=>r.Remove("StarGroups"));Reject(r=>r["Settings"]!["Ignored"]=1);
        Reject(r=>r["StarGroups"]![0]!["StarFeatureIds"]=new JArray(Enumerable.Repeat("feature.fake",10)));
        Reject(r=>r["StarGroups"]![0]!["Ignored"]=1);
        Reject(r=>r["FormatVersion"]=3);
    }
    [Fact] public void GenerationSelectorFollowsSuccessfulFormatSwitchAndFailureKeepsUserDraft()
    {
        var workspace=Workspace();var store=new GeneratedGalaxyMapStore(directory);
        byte[] Sample(string path)=>File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,path));
        store.Write(1,Sample("galaxy-samples/natural/seed-42-systems-250.natural.json"),false);
        Assert.False(workspace.NaturalGeneration);Assert.True(workspace.Open());Assert.True(workspace.NaturalGeneration);
        store.Write(1,Sample("galaxy-samples/scaled/seed-42-systems-250.scaled.json"),true);
        Assert.True(workspace.Open());Assert.False(workspace.NaturalGeneration);var map=workspace.Document;
        workspace.NaturalGeneration=true;store.Write(1,Encoding.UTF8.GetBytes("{}"),true);
        Assert.False(workspace.Open());Assert.True(workspace.NaturalGeneration);Assert.Same(map,workspace.Document);
    }

    [Fact] public void QuietTargetDraftChoicesAreInvariantAndNeverMutateTheLoadedMap()
    {
        var workspace=Workspace();workspace.NaturalGeneration=true;workspace.Size=250;
        var oldCulture=System.Globalization.CultureInfo.CurrentCulture;
        try
        {
            System.Globalization.CultureInfo.CurrentCulture=new System.Globalization.CultureInfo("nl-NL");
            Assert.Equal(new[]{500,750,1000,1250,1500,1750,2000},GeneratedGalaxyWorkspace.QuietTargetChoices(true));
            Assert.Equal(new[]{"0.50×","0.75×","1.00×","1.25×","1.50×","1.75×","2.00×"},
                GeneratedGalaxyWorkspace.QuietTargetChoices(true).Select(GeneratedGalaxyWorkspace.QuietTargetLabel));
            Assert.Equal(new[]{1000,2000,3000},GeneratedGalaxyWorkspace.QuietTargetChoices(false));
            workspace.QuietEmptyAreaTargetPermille=3000;Assert.False(workspace.CanGenerateQuietTarget);
            Assert.Contains("retained",workspace.QuietTargetDraftLabel);
            Assert.False(workspace.Generate());Assert.Null(workspace.Document);
            workspace.NaturalGeneration=false;Assert.True(workspace.CanGenerateQuietTarget);
            workspace.QuietEmptyAreaTargetPermille=750;Assert.False(workspace.CanGenerateQuietTarget);
            Assert.Equal("0.75× (retained)",workspace.QuietTargetDraftLabel);
            workspace.NaturalGeneration=true;Assert.True(workspace.CanGenerateQuietTarget);
            workspace.Size=50;workspace.QuietEmptyAreaTargetPermille=3000;Assert.True(workspace.CanGenerateQuietTarget);
        }
        finally{System.Globalization.CultureInfo.CurrentCulture=oldCulture;}
    }

    [Theory][InlineData(500)][InlineData(750)][InlineData(1000)][InlineData(1250)][InlineData(1500)][InlineData(1750)][InlineData(2000)]
    public void NaturalQuietTargetDraftGeneratesAndRoundTripsExactFractionalSetting(int target)
    {
        var workspace=Workspace();workspace.NaturalGeneration=true;workspace.Size=250;workspace.Seed="42";
        workspace.QuietEmptyAreaTargetPermille=target;
        Assert.True(workspace.CanGenerateQuietTarget);Assert.True(workspace.Generate(),workspace.Notice);
        Assert.Equal(target,workspace.Document!.Natural!.Settings.Map.QuietEmptyAreaTargetPermille);
        var bytes=workspace.ExportBytes();Assert.True(workspace.Export(false));var opened=Workspace();
        Assert.True(opened.Open(),opened.Notice);Assert.True(opened.NaturalGeneration);
        Assert.Equal(target,opened.QuietEmptyAreaTargetPermille);Assert.Equal(bytes,opened.ExportBytes());
        Assert.Equal(workspace.QuietTargetDraftLabel,opened.QuietTargetDraftLabel);
    }
    [Fact] public void HistoricalNaturalThreeOpensExactlyButNeedsExplicitNewGenerationChoice()
    {
        var source=codec.DecodeNaturalGalaxy(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,"galaxy-samples/natural/seed-42-systems-250.natural.json")));
        source.Settings.Map.QuietEmptyAreaTargetPermille=3000;
        var catalog=codec.DecodeScenario(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,"scenarios/playable-fleet-v1/scenario.json"))).Definitions.Civilizations!;
        var historic=HobbyDS9.Application.NaturalGalaxyApplication.Generate(source.Settings,catalog).Export();
        var bytes=codec.EncodeNaturalGalaxy(historic);var store=new GeneratedGalaxyMapStore(directory);store.Write(1,bytes,false);
        var workspace=Workspace();Assert.True(workspace.Open(),workspace.Notice);var map=workspace.Document;
        Assert.Equal(3000,workspace.QuietEmptyAreaTargetPermille);Assert.Contains("retained",workspace.QuietTargetDraftLabel);
        Assert.False(workspace.CanGenerateQuietTarget);Assert.False(workspace.Generate());
        Assert.Same(map,workspace.Document);Assert.Equal(bytes,workspace.ExportBytes());
        workspace.QuietEmptyAreaTargetPermille=500;Assert.True(workspace.CanGenerateQuietTarget);
        Assert.Equal(3000,workspace.Document!.Natural!.Settings.Map.QuietEmptyAreaTargetPermille);Assert.Equal(bytes,workspace.ExportBytes());
        store.Write(1,Encoding.UTF8.GetBytes("{}"),true);Assert.False(workspace.Open());
        Assert.Equal(500,workspace.QuietEmptyAreaTargetPermille);Assert.Same(map,workspace.Document);Assert.Equal(bytes,workspace.ExportBytes());
    }
}
