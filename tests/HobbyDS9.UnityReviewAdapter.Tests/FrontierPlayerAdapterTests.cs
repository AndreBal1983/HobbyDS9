using System.Diagnostics;
using System.Text;
using System.Text.Json;
using HobbyDS9.Contracts;
using HobbyDS9.TacticalReviewClient;
using HobbyDS9.UnityReview;
using Xunit;

namespace HobbyDS9.UnityReviewAdapter.Tests;

public sealed class FrontierPlayerAdapterTests
{
    private const string A="civ.federation",B="civ.cardassian",Fleet="fleet.federation-scout",Other="fleet.cardassian-scout",Slot="frontier-1";
    private static readonly string[] Inputs={"frontier","package","physical","core","scenario"};
    private static string Json<T>(T x)=>JsonSerializer.Serialize(x);
    private static byte[] Input(string name)=>File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,"scenarios/frontier-reconnaissance-v1",name+".json"));
    private static CampaignFrontierPackageDto Package(ReviewNewtonsoftCodec codec)=>ReviewExactPackageLoader.LoadFrontier(Input("frontier"),Input("package"),Input("physical"),Input("core"),Input("scenario"),codec);
    private sealed class Fixture : IDisposable
    {
        private readonly string basis=Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar,Path.AltDirectorySeparatorChar);
        public string Root { get; }
        public string DirectoryPath=>Path.Combine(Root,"HobbyDS9FrontierPreviewOnly-v5");
        public ReviewNewtonsoftCodec Codec { get; }
        public CampaignFrontierPackageDto Package { get; }
        public ReviewFrontierSlotStore Store { get; }
        public ReviewUiModel Model { get; }
        public Fixture(CampaignFrontierPackageDto? package=null,ReviewNewtonsoftCodec? codec=null,Action<UnityReviewWriteStage>? probe=null)
        {
            Root=Path.GetFullPath(Path.Combine(basis,"hobby-frontier-player-"+Guid.NewGuid().ToString("N")));
            Codec=codec??new();Package=package??FrontierPlayerAdapterTests.Package(Codec);Store=new(DirectoryPath,Codec,probe);Model=new(Package,Codec,Store);
        }
        public string PathFor(string slot=Slot)=>Path.Combine(DirectoryPath,"unity-frontier-review-v5-"+slot+".json");
        public CampaignFrontierSaveDto Save()
        {Assert.True(Model.Save(Slot,true));return Store.Read(Slot);}
        public void Route()
        {Assert.True(Model.DraftTravel(Fleet,CampaignTravelIntent.Leave,"nav.bajor",null));Assert.True(Model.SendTravel("command.frontier.player.route."+Model.Current.Frame.Status.CampaignRevision));}
        public void Dispose()
        {
            var target=Path.GetFullPath(Root);
            if(target!=Root||!string.Equals(Path.GetDirectoryName(target),basis,StringComparison.OrdinalIgnoreCase)
                ||!Path.GetFileName(target).StartsWith("hobby-frontier-player-",StringComparison.Ordinal))throw new InvalidOperationException("Unsafe test cleanup target");
            if(Directory.Exists(target))Directory.Delete(target,true);
        }
    }
    [Fact] public void ExactRawLoaderMatchesHeadlessHashLadderAndWhitespaceIdentity()
    {
        var codec=new ReviewNewtonsoftCodec();var package=Package(codec);
        var expected=FrontierPackageReader.Read(Path.Combine(AppContext.BaseDirectory,"scenarios/frontier-reconnaissance-v1"));
        Assert.Equal(Json(expected),Json(package));Assert.Equal("8af5a37850685afd665131bec6cd12eb575cfc883b5f745b9507263790fcbb18",package.PackageHash);
        var outerChanged=ReviewExactPackageLoader.LoadFrontier(Input("frontier").Concat(new byte[]{10}).ToArray(),Input("package"),Input("physical"),Input("core"),Input("scenario"),codec);
        Assert.NotEqual(package.PackageHash,outerChanged.PackageHash);Assert.Equal(package.Integrated!.PackageHash,outerChanged.Integrated!.PackageHash);
        var scenarioChanged=ReviewExactPackageLoader.LoadFrontier(Input("frontier"),Input("package"),Input("physical"),Input("core"),Input("scenario").Concat(new byte[]{10}).ToArray(),codec);
        Assert.NotEqual(package.PackageHash,scenarioChanged.PackageHash);Assert.NotEqual(package.Integrated.PackageHash,scenarioChanged.Integrated!.PackageHash);
        Assert.NotEqual(package.Integrated.Physical!.CorePackage!.Campaign!.ContentHash,scenarioChanged.Integrated.Physical!.CorePackage!.Campaign!.ContentHash);
    }
    [Fact] public void TypedModelSelectsActualSingleHumanAndCannotSwitchToOtherActors()
    {
        using var f=new Fixture();var m=f.Model;Assert.True(m.FrontierPreview);Assert.True(m.Integrated);Assert.False(m.SingleplayerDemo);
        Assert.Equal(A,m.Current.Frame.Actor);Assert.Null(m.Current.Frame.Demo);Assert.Equal(A,m.Current.Frame.Frontier!.ActorId);
        var frame=m.Current.Frame;Assert.False(m.SwitchActor(B,true));Assert.False(m.SwitchActor("civ.missing",true));Assert.Same(frame,m.Current.Frame);
        Assert.DoesNotContain("Transcript",m.Facts());Assert.DoesNotContain("MovementProof",m.Facts());Assert.DoesNotContain("nav.gamma-exit",m.Facts());
    }
    [Theory][InlineData(0)][InlineData(2)]
    public void MissingOrAmbiguousHumanNeverCreatesASelectablePlayerModel(int count)
    {
        using var f=new Fixture();var p=f.Codec.CopyPackage(f.Package);var civs=p.Integrated!.Physical!.CorePackage!.Campaign!.InitialState.Civilizations!;
        foreach(var c in civs)c.ControllerKind=ControllerKind.AI;
        if(count>0)civs.Single(c=>c.CivilizationId==A).ControllerKind=ControllerKind.Human;
        if(count>1)civs.Single(c=>c.CivilizationId==B).ControllerKind=ControllerKind.Human;
        var old=f.Model.Current;Assert.Throws<ArgumentException>(()=>new ReviewUiModel(p,f.Codec,f.Store));Assert.Same(old,f.Model.Current);
    }
    [Fact] public void HumanSelectionFollowsPersistedControlRatherThanFactionName()
    {
        var codec=new ReviewNewtonsoftCodec();var p=Package(codec);var civs=p.Integrated!.Physical!.CorePackage!.Campaign!.InitialState.Civilizations!;
        civs.Single(c=>c.CivilizationId==A).ControllerKind=ControllerKind.AI;civs.Single(c=>c.CivilizationId==B).ControllerKind=ControllerKind.Human;
        using var f=new Fixture(p,codec);Assert.Equal(B,f.Model.Current.Frame.Actor);Assert.Equal(B,f.Model.Current.Frame.Frontier!.ActorId);
        Assert.False(f.Model.SwitchActor(A,true));Assert.True(f.Model.Save(Slot));Assert.Equal(5,f.Store.Read(Slot).FormatVersion);
    }
    [Fact] public void WrongRawFrontierProfileCannotBecomeDemoFourOrReplaceExistingModel()
    {
        using var f=new Fixture();var frame=f.Model.Current;
        var bad=Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(Input("frontier")).Replace("\"FormatVersion\": 5","\"FormatVersion\": 4"));
        var p=ReviewExactPackageLoader.LoadFrontier(bad,Input("package"),Input("physical"),Input("core"),Input("scenario"),f.Codec);
        Assert.Equal(4,p.FormatVersion);Assert.Throws<ArgumentException>(()=>new ReviewUiModel(p,f.Codec,f.Store));Assert.Same(frame,f.Model.Current);Assert.True(f.Model.FrontierPreview);
    }
    [Fact] public void StationaryRefreshAndPreviewDoNotDiscoverPartialArrivalPublishesNextPlanning()
    {
        using var f=new Fixture();var m=f.Model;var initial=m.Current.Frame.Frontier!;
        var before=Json(f.Save());var offer=m.Current.FindKnownRoute(Fleet,"nav.bajor")!;Assert.True(offer.Route.Success);
        Assert.True(m.Refresh());Assert.Equal(before,Json(f.Save()));Assert.Empty(m.Current.Frame.Frontier!.Receipts);
        Assert.True(m.Advance());Assert.Equal(2,m.Current.Frame.Frontier.Nodes.Count);Assert.Empty(m.Current.Frame.Frontier.Receipts);
        Assert.False(m.DraftTravel(Fleet,CampaignTravelIntent.Leave,"nav.gamma-exit",null));Assert.False(m.Current.Dirty);
        f.Route();Assert.True(m.Advance());Assert.Equal(0,m.Current.Frame.Travel!.OwnReceipts.Single().EdgesCompleted);Assert.Empty(m.Current.Frame.Frontier.Receipts);
        Assert.True(m.Advance());Assert.Equal(SessionPhase.Planning,m.Current.Frame.Status.Phase);Assert.Single(m.Current.Frame.Frontier.Receipts);
        Assert.Contains(m.Current.Frame.Frontier.Nodes,n=>n.NodeId=="nav.bajoran-wormhole");Assert.Contains(m.Current.Frame.Frontier.Nodes,n=>n.NodeId=="nav.badlands");
        Assert.DoesNotContain(m.Current.Frame.Frontier.Nodes,n=>n.NodeId=="nav.gamma-exit");Assert.True(m.Current.FindKnownRoute(Fleet,"nav.bajoran-wormhole")!.Route.Success);
        Assert.Empty(initial.Receipts);Assert.Equal(2,initial.Nodes.Count);
    }
    [Fact] public void RealPartialAndAfterDiscoveryLoadReplaceHostAndControllerTogether()
    {
        using var f=new Fixture();var m=f.Model;f.Route();Assert.True(m.Advance());var partial=f.Save();var old=m.Current;
        Assert.True(m.Load(Slot));Assert.NotSame(old,m.Current);Assert.Equal(Json(partial),Json(f.Save()));Assert.Empty(m.Current.Frame.Frontier!.Receipts);
        Assert.True(m.Advance());Assert.Single(m.Current.Frame.Frontier.Receipts);var discovered=f.Save();old=m.Current;
        var offer=old.FindKnownRoute(Fleet,"nav.bajoran-wormhole")!;Assert.True(m.Load(Slot));Assert.NotSame(old,m.Current);
        Assert.False(m.Current.EditTravel(Fleet,CampaignTravelIntent.Leave,offer));Assert.Equal(Json(discovered),Json(f.Save()));
        Assert.True(m.AllocateResearch(ResearchUnits.Scale,"command.frontier.player.split.after-load"));
        Assert.True(m.SetCloak(Fleet,false,"command.frontier.player.cloak.after-load"));
        Assert.True(m.DraftTravel(Fleet,CampaignTravelIntent.Leave,"nav.bajoran-wormhole",null));Assert.True(m.SendTravel("command.frontier.player.branch.after-load"));
        Assert.True(m.Advance());Assert.Contains(m.Current.Frame.Frontier.Nodes,n=>n.NodeId=="nav.gamma-exit");
    }
    [Theory][InlineData("malformed")][InlineData("demo4")][InlineData("missing-transcript")][InlineData("forged-history")][InlineData("forged-rng")]
    public void FailedFiveLoadPreservesControllerTravelResearchAndLocalDesignDrafts(string fault)
    {
        using var f=new Fixture();var m=f.Model;var save=f.Save();Assert.True(m.DraftTravel(Fleet,CampaignTravelIntent.Stay,null,null));
        var old=m.Current;var travel=old.TravelDraft;var research=m.ResearchInputs()!;research.ScienceUnits=ResearchUnits.Scale;
        m.LocalDesignDirty=true;Action reset=()=>{};m.ResetLocalDesign=reset;
        switch(fault){case "malformed":File.WriteAllText(f.PathFor(),"{\"private-corrupt\":true}");break;
            case "demo4":File.WriteAllText(f.PathFor(),"{\"FormatVersion\":4}");break;
            case "missing-transcript":File.WriteAllText(f.PathFor(),Encoding.UTF8.GetString(f.Codec.EncodeFrontierRoot(save)).Replace("\"Transcript\":[]","\"Transcript\":null"));break;
            case "forged-history":save.Transcript!.Add(new(){Kind=FrontierOperationKind.Advance,Turn=1,Revision=0});f.Store.Write(Slot,save,true);break;
            default:save.Integrated!.Physical!.Core.Campaign.GameState.RngState.State++;f.Store.Write(Slot,save,true);break;}
        Assert.False(m.Load(Slot,true));Assert.Same(old,m.Current);Assert.Same(travel,m.Current.TravelDraft);Assert.Same(research,m.ResearchDraft);
        Assert.True(m.LocalDesignDirty);Assert.Same(reset,m.ResetLocalDesign);Assert.True(m.HasLocalEdits);
        Assert.DoesNotContain("private-corrupt",m.Message);Assert.DoesNotContain(f.Root,m.Message);
    }
    [Fact] public void SuccessfulLoadRequiresDiscardAndNeverSerializesUnsentEdits()
    {
        using var f=new Fixture();var m=f.Model;var initial=f.Save();Assert.True(m.DraftTravel(Fleet,CampaignTravelIntent.Stay,null,null));
        m.ResearchInputs()!.ScienceUnits=ResearchUnits.Scale;m.LocalDesignDirty=true;var old=m.Current;
        Assert.True(m.Save("frontier-2"));Assert.Equal(Json(initial),Json(f.Store.Read("frontier-2")));
        Assert.False(m.Load(Slot));Assert.Same(old,m.Current);Assert.True(m.HasLocalEdits);
        Assert.True(m.Load(Slot,true));Assert.NotSame(old,m.Current);Assert.False(m.HasLocalEdits);Assert.Null(m.ResearchDraft);
        Assert.True(m.Advance()); // Proves cached host was replaced together with the controller.
    }
    [Fact] public void PendingAndResolvedFiveCheckpointsKeepAiRoundAndExplicitAftermath()
    {
        var p=CombatPackage();using var f=new Fixture(p);var m=f.Model;f.Route();Assert.True(m.Advance());Assert.Equal(SessionPhase.AwaitingCombat,m.Current.Frame.Status.Phase);
        var pending=f.Save();Assert.Single(pending.Integrated!.Physical!.Core.Battles.Single().Commitments);Assert.Empty(m.Current.Frame.Frontier!.Receipts);
        Assert.True(m.Load(Slot));Assert.Equal(Json(pending),Json(f.Save()));
        while(!m.Current.Frame.Battle!.Complete)
        {
            Assert.True(m.CommitRound("command.frontier.player.round."+m.Current.Frame.Battle.Round));Assert.Empty(m.Current.Frame.Frontier.Receipts);
            var saved=f.Save();Assert.True(m.Load(Slot));Assert.Equal(Json(saved),Json(f.Save()));
        }
        Assert.Empty(m.Current.Frame.Frontier.Receipts);Assert.True(m.FinalizeBatch());Assert.Single(m.Current.Frame.Frontier.Receipts);
        var final=f.Save();Assert.False(m.FinalizeBatch());Assert.Equal(Json(final),Json(f.Save()));Assert.True(m.Load(Slot));Assert.Equal(Json(final),Json(f.Save()));
    }
    [Fact] public void FailedRealDiskWriteKeepsModelDraftAndPreviousSlot()
    {
        bool fail=false;using var f=new Fixture(probe:s=>{if(fail&&s==UnityReviewWriteStage.BeforeReplace)throw new IOException();});var m=f.Model;
        f.Save();var bytes=File.ReadAllBytes(f.PathFor());Assert.True(m.DraftTravel(Fleet,CampaignTravelIntent.Stay,null,null));var old=m.Current;var draft=old.TravelDraft;
        fail=true;Assert.False(m.Save(Slot,true));Assert.Same(old,m.Current);Assert.Same(draft,m.Current.TravelDraft);Assert.Equal(bytes,File.ReadAllBytes(f.PathFor()));
    }
    [Fact] public void DemoFourLoaderModelNamespaceAndSaveBytesRemainIndependent()
    {
        using var demo=new DemoUiFixture();Assert.False(demo.Model.FrontierPreview);Assert.True(demo.Model.SingleplayerDemo);Assert.Null(demo.Model.Current.Frame.Frontier);
        Assert.Equal("bb76fb9f03299f92ca009b46bc931ff5bd75a27018c02d31c6f4068b21ed12a4",demo.Package.PackageHash);
        Assert.True(demo.Model.Save("1"));var before=demo.Codec.EncodeDemoRoot(demo.Store.Read("1"));
        using var frontier=new Fixture(codec:demo.Codec);Assert.True(frontier.Model.Save(Slot));Assert.False(frontier.Model.SingleplayerDemo);Assert.True(frontier.Model.FrontierPreview);
        Assert.Equal(5,frontier.Store.Read(Slot).FormatVersion);Assert.Equal(4,demo.Store.Read("1").FormatVersion);Assert.Equal(before,demo.Codec.EncodeDemoRoot(demo.Store.Read("1")));
        Assert.True(demo.Model.Load("1",true));Assert.True(demo.Model.Advance());Assert.NotNull(demo.Model.Current.Frame.Demo);Assert.Null(demo.Model.Current.Frame.Frontier);
    }
    [Fact] public void PreparationCopiesExactlyFiveRawInputsOnlyToExplicitArtifactsDestination()
    {
        using var f=new Fixture();var repo=FindRepo();var output=Path.Combine(f.Root,"prepared");var result=Prepare(repo,output);
        Assert.Equal(0,result.Exit);Assert.Equal(Inputs.Select(n=>n+".json").OrderBy(n=>n),Directory.GetFiles(output).Select(Path.GetFileName).OrderBy(n=>n));
        foreach(var name in Inputs)Assert.Equal(File.ReadAllBytes(Path.Combine(repo,"scenarios/frontier-reconnaissance-v1",name+".json")),File.ReadAllBytes(Path.Combine(output,name+".json")));
        var p=ReviewExactPackageLoader.LoadFrontier(File.ReadAllBytes(Path.Combine(output,"frontier.json")),File.ReadAllBytes(Path.Combine(output,"package.json")),File.ReadAllBytes(Path.Combine(output,"physical.json")),File.ReadAllBytes(Path.Combine(output,"core.json")),File.ReadAllBytes(Path.Combine(output,"scenario.json")),f.Codec);
        Assert.Equal(f.Package.PackageHash,p.PackageHash);Assert.False(Directory.Exists(Path.Combine(output,"Assets")));Assert.False(Directory.Exists(Path.Combine(output,"Library")));
    }
    [Fact] public void MissingPreparationInputFailsBeforeCreatingPartialOutput()
    {
        using var f=new Fixture();var source=Path.Combine(f.Root,"incomplete");Directory.CreateDirectory(source);File.WriteAllBytes(Path.Combine(source,"frontier.json"),Input("frontier"));
        var output=Path.Combine(f.Root,"missing-output");var result=Prepare(FindRepo(),output,source);Assert.NotEqual(0,result.Exit);Assert.False(Directory.Exists(output));
    }
    private static string FindRepo()
    {
        for(var folder=new DirectoryInfo(AppContext.BaseDirectory);folder!=null;folder=folder.Parent)
            if(File.Exists(Path.Combine(folder.FullName,"scripts/PrepareUnityFrontier.proj"))&&File.Exists(Path.Combine(folder.FullName,"global.json")))return folder.FullName;
        throw new InvalidOperationException("Current repository preparation script not found");
    }
    private static (int Exit,string Log) Prepare(string repo,string output,string? source=null)
    {
        var start=new ProcessStartInfo(Environment.GetEnvironmentVariable("DOTNET_HOST_PATH")??"dotnet") {WorkingDirectory=repo,UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true};
        foreach(var arg in new[]{"msbuild",Path.Combine(repo,"scripts/PrepareUnityFrontier.proj"),"-t:Prepare","-p:FrontierOutput="+output})start.ArgumentList.Add(arg);
        if(source!=null)start.ArgumentList.Add("-p:FrontierSource="+source);
        using var process=Process.Start(start)!;var stdout=process.StandardOutput.ReadToEndAsync();var stderr=process.StandardError.ReadToEndAsync();
        Assert.True(process.WaitForExit(60000),"Resource-only preparation exceeded its bound");return(process.ExitCode,stdout.GetAwaiter().GetResult()+stderr.GetAwaiter().GetResult());
    }
    private static CampaignFrontierPackageDto CombatPackage()
    {
        var p=Package(new());var physical=p.Integrated!.Physical!;var scenario=physical.CorePackage!.Campaign!;
        scenario.InitialState.Fleets!.Single(f=>f.FleetId==Other).LocationNodeId="nav.bajor";scenario.Definitions.NavigationConnections!.Single(e=>e.ConnectionId=="connection.sol-bajor").BaseTraversalCost=1;
        var own=scenario.InitialState.NavigationKnowledge!.Single(k=>k.CivilizationId==B);foreach(var n in own.Nodes!)n.Level=NavigationKnowledgeLevel.Charted;foreach(var e in own.Connections!)e.Level=NavigationKnowledgeLevel.Charted;
        physical.InitialOrders!.Single(o=>o.FleetId==Other).Intent=CampaignTravelIntent.StayEngage;physical.InitialContacts!.Add(new(){ActorId=B,FleetId=Fleet,LastSeenNodeId="nav.sol",LastSeenTurn=1,TrackingNodeIds=own.Nodes!.Select(n=>n.Id).ToList()});return p;
    }
}
