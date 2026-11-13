using System.Text;
using HobbyDS9.Contracts;
using HobbyDS9.UnityReview;
using Newtonsoft.Json.Linq;
using Xunit;

namespace HobbyDS9.UnityReviewAdapter.Tests;

public sealed class GeneratedGalaxyRouteDraftStoreTests : IDisposable
{
    private readonly string directory=Path.Combine(Path.GetTempPath(),"HobbyDS9-DraftStore-"+Guid.NewGuid().ToString("N"));
    private readonly ReviewNewtonsoftCodec codec=new();
    private GeneratedGalaxyWorkspace Workspace()
    {
        var workspace=new GeneratedGalaxyWorkspace(codec.DecodeScenario(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,
            "scenarios/playable-fleet-v1/scenario.json"))).Definitions.Civilizations!,new GeneratedGalaxyMapStore(Path.Combine(directory,"maps"))){Seed="42",Size=50};
        Assert.True(workspace.Generate());return workspace;
    }
    private string DraftDirectory=>Path.Combine(directory,"drafts");
    private string SavedFile=>Path.Combine(DraftDirectory,"generated-map-1.json");
    public void Dispose(){if(Directory.Exists(directory))Directory.Delete(directory,true);}
    private static GalaxyReviewRouteFilterDto Full(GeneratedGalaxyMapData map)=>new(){PermittedNodeIds=map.Regions.Select(r=>r.NodeId).ToList(),PermittedConnectionIds=map.Connections.Select(c=>c.ConnectionId).ToList()};
    private static GeneratedGalaxyRouteDraft Draft(GeneratedGalaxyMapData map)
    {
        var result=new GeneratedGalaxyRouteDraft(map.ContentHash,GalaxyReviewRoutePolicy.MaximumTargets);
        result.SetStart(new(map.Systems[0].NodeId,GeneratedGalaxyDraftAnchorKind.StarLocation));
        result.SetTarget(new(map.Systems[1].NodeId,GeneratedGalaxyDraftAnchorKind.StarLocation),true);
        result.SetTarget(new(map.Systems[1].NodeId,GeneratedGalaxyDraftAnchorKind.RegionTransit),true);
        result.SetTarget(new(map.Systems[0].NodeId,GeneratedGalaxyDraftAnchorKind.RegionTransit),true);return result;
    }
    [Fact] public void RoundTripRevalidatesTypedRepeatedAndBacktrackingLegsWithoutChangingMapOrSavingPermissions()
    {
        var workspace=Workspace();var map=workspace.Document!;var before=workspace.ExportBytes();var draft=Draft(map);
        var store=new GeneratedGalaxyRouteDraftStore(DraftDirectory);store.Save(1,draft,false);
        var text=File.ReadAllText(SavedFile);Assert.DoesNotContain("Permitted",text);Assert.DoesNotContain("Filter",text);
        var loaded=store.Load(1,workspace,Full(map));Assert.Equal(draft.MapIdentity,loaded.Draft.MapIdentity);
        Assert.Equal(draft.Targets.Select(t=>(t.NodeId,t.Kind)),loaded.Draft.Targets.Select(t=>(t.NodeId,t.Kind)));
        Assert.Equal(3,loaded.Plan!.Legs.Count);Assert.Empty(loaded.Plan.Legs[1].Route.Steps);
        Assert.Equal(before,workspace.ExportBytes());Assert.Equal(3,GeneratedGalaxyRoutePath.BuildPlan(map.Regions.ToDictionary(r=>r.NodeId),loaded.Plan).Count);
        draft.Clear();store.Save(2,draft,false);var startOnly=store.Load(2,workspace,Full(map));Assert.Null(startOnly.Plan);Assert.Empty(startOnly.Draft.Targets);
    }
    [Fact] public void FreshDeniedOrBlockedMiddleLegNeverPublishesASuccessPrefixOrMutatesCurrentDraft()
    {
        var workspace=Workspace();var map=workspace.Document!;var current=Draft(map);var original=current.Targets.ToArray();var bytes=workspace.ExportBytes();
        var edge=map.Connections[0];var third=map.Regions.First(r=>r.NodeId!=edge.EndpointA&&r.NodeId!=edge.EndpointB).NodeId;
        var saved=new GeneratedGalaxyRouteDraft(map.ContentHash,64);saved.SetStart(new(edge.EndpointA,GeneratedGalaxyDraftAnchorKind.RegionTransit));
        saved.SetTarget(new(edge.EndpointB,GeneratedGalaxyDraftAnchorKind.RegionTransit),true);saved.SetTarget(new(third,GeneratedGalaxyDraftAnchorKind.RegionTransit),true);
        var store=new GeneratedGalaxyRouteDraftStore(DraftDirectory);store.Save(1,saved,false);
        var blocked=new GalaxyReviewRouteFilterDto{PermittedNodeIds=new(){edge.EndpointA,edge.EndpointB,third},PermittedConnectionIds=new(){edge.ConnectionId}};
        Assert.Throws<InvalidDataException>(()=>store.Load(1,workspace,blocked));
        Assert.Throws<InvalidDataException>(()=>store.Load(1,workspace,new(){PermittedNodeIds=new(),PermittedConnectionIds=new()}));
        Assert.Equal(original,current.Targets);Assert.Same(map,workspace.Document);Assert.Equal(bytes,workspace.ExportBytes());
    }
    [Theory][InlineData("wrong-map")][InlineData("policy")][InlineData("missing")][InlineData("unknown")][InlineData("unavailable")][InlineData("too-many")]
    public void InvalidCompanionCannotReplaceCurrentDraftOrMap(string corruption)
    {
        var workspace=Workspace();var map=workspace.Document!;var draft=Draft(map);var before=workspace.ExportBytes();var original=draft.Targets.ToArray();
        var store=new GeneratedGalaxyRouteDraftStore(DraftDirectory);store.Save(1,draft,false);var root=JObject.Parse(File.ReadAllText(SavedFile));
        switch(corruption)
        {
            case "wrong-map":root["MapContentHash"]=new string('a',64);break;
            case "policy":root["PolicyVersion"]="unsupported";break;
            case "missing":root.Remove("Targets");break;
            case "unknown":root["Filter"]=new JObject();break;
            case "unavailable":root["Targets"]![0]!["NodeId"]="nav.unavailable";break;
            case "too-many":root["Targets"]=new JArray(Enumerable.Range(0,65).Select(_=>root["Targets"]![0]!.DeepClone()));break;
        }
        File.WriteAllText(SavedFile,root.ToString());Assert.Throws<InvalidDataException>(()=>store.Load(1,workspace,Full(map)));
        Assert.Equal(original,draft.Targets);Assert.Same(map,workspace.Document);Assert.Equal(before,workspace.ExportBytes());
    }
    [Fact] public void InterruptedCompanionOverwritePreservesPriorFileAndCleansTemporaryPayload()
    {
        var workspace=Workspace();var draft=Draft(workspace.Document!);var store=new GeneratedGalaxyRouteDraftStore(DraftDirectory);store.Save(1,draft,false);
        var original=File.ReadAllBytes(SavedFile);draft.Clear();
        var faulted=new GeneratedGalaxyRouteDraftStore(DraftDirectory,stage=>{if(stage==UnityReviewWriteStage.AfterPartialWrite)throw new IOException("Injected interruption");});
        Assert.Throws<IOException>(()=>faulted.Save(1,draft,true));Assert.Equal(original,File.ReadAllBytes(SavedFile));
        Assert.Single(Directory.GetFiles(DraftDirectory));
    }
    [Fact] public void CompanionHasFiniteReadBoundAndRejectsTrailingIdentifierNewline()
    {
        var workspace=Workspace();var store=new GeneratedGalaxyRouteDraftStore(DraftDirectory);store.Save(1,Draft(workspace.Document!),false);
        var root=JObject.Parse(File.ReadAllText(SavedFile));root["Start"]!["NodeId"]="nav.a\n";
        Assert.Throws<InvalidDataException>(()=>codec.DecodeRouteDraft(Encoding.UTF8.GetBytes(root.ToString())));
        File.WriteAllBytes(SavedFile,new byte[GeneratedGalaxyRouteDraftStore.MaximumBytes+1]);Assert.Throws<InvalidDataException>(()=>store.Load(1,workspace,Full(workspace.Document!)));
    }
}
