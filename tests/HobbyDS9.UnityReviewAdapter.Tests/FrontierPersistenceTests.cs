using System.Text;
using System.Text.Json;
using HobbyDS9.Application;
using HobbyDS9.Contracts;
using HobbyDS9.TacticalReviewClient;
using HobbyDS9.UnityReview;
using Newtonsoft.Json.Linq;
using Xunit;

namespace HobbyDS9.UnityReviewAdapter.Tests;

public sealed class FrontierPersistenceTests
{
    private const string A="civ.federation",B="civ.cardassian",Fleet="fleet.federation-scout",Other="fleet.cardassian-scout";
    private static string Json<T>(T x)=>JsonSerializer.Serialize(x);
    private static CampaignFrontierPackageDto Package()=>FrontierPackageReader.Read(Path.Combine(AppContext.BaseDirectory,"scenarios/frontier-reconnaissance-v1"));
    private sealed class Fixture : IDisposable
    {
        private readonly string temporaryParent=Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar,Path.AltDirectorySeparatorChar);
        public string Root { get; }
        public string DirectoryPath=>Path.Combine(Root,"HobbyDS9FrontierReviewOnly-v5");
        public ReviewNewtonsoftCodec Codec { get; }=new();
        public CampaignFrontierPackageDto Package { get; }
        public ReviewFrontierSlotStore Store { get; }
        public PhysicalReviewHost Host { get; private set; }
        public ReviewController Controller { get; private set; }
        public Fixture(CampaignFrontierPackageDto? p=null,Action<UnityReviewWriteStage>? probe=null)
        {
            Root=Path.GetFullPath(Path.Combine(temporaryParent,"hobby-frontier-json-"+Guid.NewGuid().ToString("N")));
            Package=p??FrontierPersistenceTests.Package();Store=new(DirectoryPath,Codec,probe);
            Host=PhysicalReviewHost.StartFrontier(Package,Codec,Store);Controller=new(Host,A);
        }
        public string PathFor(string slot)=>Path.Combine(DirectoryPath,"unity-frontier-review-v5-"+slot+".json");
        public CampaignFrontierSaveDto Save(string slot="one")
        {Assert.True(Controller.SaveCommittedToSlot(slot,true).Succeeded);return Store.Read(slot);}
        public void Reopen(string slot="one")
        {
            var opened=PhysicalReviewHost.OpenFrontierFromSlot(Package,Codec,Store,slot,A);
            Assert.True(opened.Succeeded);Host=opened.Host!;Controller=new(Host,A);
            if(Controller.Frame.Encounters.Count==1)Assert.True(Controller.SelectEncounter(Controller.Frame.Encounters[0]));
        }
        public void Advance(){Assert.True(Host.AdvancePlanning(Controller.Frame).Accepted);Assert.True(Controller.Refresh());}
        public void Route()
        {
            var c=Controller;var offer=c.FindKnownRoute(Fleet,"nav.bajor")!;Assert.True(offer.Route.Success);
            Assert.True(c.EditTravel(Fleet,CampaignTravelIntent.Leave,offer));Assert.True(c.ReviewTravel());
            var work=c.ConfirmTravel("command.frontier.disk.route."+c.Frame.Status.CampaignRevision)!;var receipt=c.ExecutePending(work);
            Assert.True(receipt.Result.Accepted);Assert.True(c.Receive(receipt));
        }
        public void Round()
        {
            var c=Controller;Assert.True(c.ReviewRound());var work=c.ConfirmRound("command.frontier.disk.round."+c.Frame.Battle!.Round)!;
            var receipt=c.ExecutePending(work);Assert.True(receipt.Result.Accepted);Assert.True(c.Receive(receipt));
        }
        public void Dispose()
        {
            // Verify this exact test-owned absolute subtree before recursive deletion.
            var full=Path.GetFullPath(Root);
            if(full!=Root||!string.Equals(Path.GetDirectoryName(full),temporaryParent,StringComparison.OrdinalIgnoreCase)
                ||!Path.GetFileName(full).StartsWith("hobby-frontier-json-",StringComparison.Ordinal))throw new InvalidOperationException("Unsafe test cleanup path");
            if(Directory.Exists(full))Directory.Delete(full,true);
        }
    }
    [Fact] public void ActualCodecAndFilesystemRoundTripCompleteInitialAndDeterministicFinalRoot()
    {
        using var f=new Fixture();var initial=f.Save();Assert.Empty(initial.Transcript!);Assert.Empty(initial.Discoveries!);
        Assert.Null(initial.Integrated!.Physical!.MovementProof);f.Reopen();Assert.Empty(f.Controller.Frame.Frontier!.Receipts);
        var final=FrontierWalkthrough.Run(f.Package,true);f.Store.Write("final",final,false);
        var bytes=File.ReadAllBytes(f.PathFor("final"));Assert.False(bytes.Take(3).SequenceEqual(new byte[]{239,187,191}));
        Assert.Equal(Json(final),Json(f.Codec.DecodeFrontierRoot(bytes)));Assert.Equal(Json(final),Json(f.Store.Read("final")));
        f.Reopen("final");Assert.Equal(8,f.Controller.Frame.Status.Turn);Assert.True(f.Controller.Frame.Frontier!.Receipts.Count>0);
        Assert.Equal(Json(final),Json(f.Save("final")));Assert.Equal(bytes,File.ReadAllBytes(f.PathFor("final")));
        Assert.Equal(13,final.Transcript!.Count);Assert.Equal(9,final.Discoveries!.Count);
        Assert.IsAssignableFrom<IFrontierReviewHostCodec>(f.Codec);
    }
    [Fact] public void ActualCodecKeepsPackageOwnershipIntegerLimitsNullAndFalseWithoutFloatingPointLoss()
    {
        using var f=new Fixture();var p=f.Codec.CopyPackage(f.Package);Assert.Equal(Json(f.Package),Json(p));
        p.PublicLaneIds!.Clear();Assert.NotEmpty(f.Package.PublicLaneIds!);
        var save=f.Save();var game=save.Integrated!.Physical!.Core.Campaign.GameState;
        game.StateRevision=long.MaxValue;game.NextEntitySequence=ulong.MaxValue;game.RngState.State=ulong.MaxValue;
        save.Integrated.Physical.Orders[0].ExpectedContactSeenTurn=0;
        save.Integrated.Physical.Core.FleetSettings[0].ApproachCloak=false;
        var copy=f.Codec.CopySave(save);var copied=copy.Integrated!.Physical!.Core.Campaign.GameState;
        Assert.Equal(long.MaxValue,copied.StateRevision);Assert.Equal(ulong.MaxValue,copied.NextEntitySequence);Assert.Equal(ulong.MaxValue,copied.RngState.State);
        Assert.Equal(0,copy.Integrated.Physical.Orders[0].ExpectedContactSeenTurn);Assert.False(copy.Integrated.Physical.Core.FleetSettings[0].ApproachCloak);
        Assert.Null(copy.Integrated.Physical.MovementProof);copy.Transcript!.Add(new(){Kind=FrontierOperationKind.Advance});Assert.Empty(save.Transcript!);
        var profile=f.Codec.DecodeFrontierProfile(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,"scenarios/frontier-reconnaissance-v1/frontier.json")));
        Assert.Equal(5,profile.FormatVersion);Assert.Null(profile.Integrated);
    }
    [Theory][InlineData("format")][InlineData("profile")][InlineData("policy")][InlineData("hash")][InlineData("transcript")][InlineData("discoveries")]
    [InlineData("nested3")][InlineData("nested2")][InlineData("core")][InlineData("game")][InlineData("orders")][InlineData("null-op")][InlineData("core-format")][InlineData("pools")]
    public void IncompleteOrWrongEnvelopeCannotOverwritePreviousSlot(string fault)
    {
        using var f=new Fixture();var save=f.Save();var old=File.ReadAllBytes(f.PathFor("one"));
        switch(fault){case "format":save.FormatVersion=4;break;case "profile":save.ProfileId=CampaignSingleplayerDemoIdentity.Profile;break;
            case "policy":save.PolicyId="foreign";break;case "hash":save.PackageHash="bad";break;case "transcript":save.Transcript=null;break;
            case "discoveries":save.Discoveries=null;break;case "nested3":save.Integrated=null;break;
            case "nested2":save.Integrated!.Physical=null;break;case "core":save.Integrated!.Physical!.Core.ProfileId=CampaignTacticalReviewIdentity.Profile;break;
            case "game":save.Integrated!.Physical!.Core.Campaign.GameState=null!;break;case "orders":save.Integrated!.Physical!.Orders=null!;break;
            case "core-format":save.Integrated!.Physical!.Core.FormatVersion=999;break;case "pools":save.Integrated!.Physical!.Core.Pools=null!;break;
            default:save.Transcript!.Add(null!);break;}
        var ex=Assert.Throws<ReviewCheckpointStoreException>(()=>f.Store.Write("one",save,true));Assert.Equal(ReviewCheckpointError.SerializationFailed,ex.Error);
        Assert.Equal(old,File.ReadAllBytes(f.PathFor("one")));Assert.Empty(Directory.GetFiles(f.DirectoryPath,"*.tmp"));
    }
    [Theory][InlineData("missing")][InlineData("case")][InlineData("unknown")][InlineData("numeric-string")][InlineData("fraction")]
    [InlineData("enum-string")][InlineData("duplicate")][InlineData("trailing")][InlineData("array")][InlineData("null-transcript")][InlineData("type")][InlineData("invalid-utf8")]
    public void RawMalformedJsonFailsClosedWithoutPrivateDiagnostics(string fault)
    {
        using var f=new Fixture();var obj=JObject.Parse(Encoding.UTF8.GetString(f.Codec.EncodeFrontierRoot(f.Save())));
        switch(fault){case "missing":obj.Remove("FormatVersion");break;case "case":obj["formatVersion"]=obj["FormatVersion"];obj.Remove("FormatVersion");break;
            case "unknown":obj["PrivateSecret"]="secret";break;case "numeric-string":obj["FormatVersion"]="5";break;
            case "fraction":obj["FormatVersion"]=5.0;break;case "enum-string":obj["Integrated"]!["Physical"]!["Phase"]="Planning";break;
            case "array":obj["Transcript"]=new JObject();break;case "null-transcript":obj["Transcript"]=null;break;case "type":obj["$type"]="PrivateSecret, SecretAssembly";break;}
        var json=obj.ToString(Newtonsoft.Json.Formatting.None);if(fault=="duplicate")json=json.Insert(1,"\"FormatVersion\":5,");if(fault=="trailing")json+="{}";
        var bytes=fault=="invalid-utf8"?new byte[]{0xff,0xfe,0xff}:Encoding.UTF8.GetBytes(json);
        var error=Assert.Throws<InvalidDataException>(()=>f.Codec.DecodeFrontierRoot(bytes));Assert.Equal("Invalid frontier review data.",error.Message);Assert.Null(error.InnerException);
        File.WriteAllBytes(f.PathFor("bad"),bytes);Assert.Equal(ReviewCheckpointError.InvalidCheckpoint,Assert.Throws<ReviewCheckpointStoreException>(()=>f.Store.Read("bad")).Error);
    }
    [Theory][InlineData(0)][InlineData(1)][InlineData(2)]
    public void RealPartialWriteFaultsPreserveOldBytesAndCleanOnlyOwnedTemporary(int fault)
    {
        UnityReviewWriteStage? stage=null;using var f=new Fixture(probe:s=>{if(s==stage)throw new IOException("injected-private-path");});
        f.Save();var before=File.ReadAllBytes(f.PathFor("one"));var unrelated=Path.Combine(f.DirectoryPath,".unity-frontier-review-v5-one.foreign.tmp");
        File.WriteAllText(unrelated,"another writer's leftover");f.Route();stage=(UnityReviewWriteStage)fault;
        var frame=f.Controller.Frame;Assert.True(f.Controller.EditTravel(Fleet,CampaignTravelIntent.Stay));var draft=f.Controller.TravelDraft;
        Assert.Equal(ReviewCheckpointError.WriteFailed,f.Controller.SaveCommittedToSlot("one",true).Error);
        Assert.Equal(before,File.ReadAllBytes(f.PathFor("one")));Assert.Same(frame,f.Controller.Frame);Assert.Same(draft,f.Controller.TravelDraft);
        Assert.Equal(new[]{unrelated},Directory.GetFiles(f.DirectoryPath,"*.tmp"));Assert.Equal("another writer's leftover",File.ReadAllText(unrelated));
        stage=null;Assert.True(f.Controller.SaveCommittedToSlot("one",true).Succeeded);Assert.NotEqual(before,File.ReadAllBytes(f.PathFor("one")));
    }
    [Theory][InlineData("")][InlineData("../escape")][InlineData("a/b")][InlineData("a\\b")][InlineData("..")][InlineData("Upper")][InlineData(".tmp")][InlineData("a:b")]
    public void UnsafeSlotNamesCreateNoDirectoryAndNeverReadForeignPath(string slot)
    {
        using var f=new Fixture();var root=CampaignFrontierApplication.StartPackage(f.Package).ExportFrontierSave();
        Assert.Equal(ReviewCheckpointError.InvalidSlot,Assert.Throws<ReviewCheckpointStoreException>(()=>f.Store.Write(slot,root,false)).Error);
        Assert.Equal(ReviewCheckpointError.InvalidSlot,Assert.Throws<ReviewCheckpointStoreException>(()=>f.Store.Read(slot)).Error);Assert.False(Directory.Exists(f.Root));
    }
    [Fact] public void NamespaceOverwriteRootAndForeignDirectoryGuardsUseActualFiles()
    {
        using var f=new Fixture();Directory.CreateDirectory(f.DirectoryPath);
        var oldPaths=new[]{"campaign-one.json","unity-physical-review-one.json","unity-integrated-playtest-one.json","unity-singleplayer-demo-one.json"}.Select(n=>Path.Combine(f.DirectoryPath,n)).ToArray();
        foreach(var path in oldPaths)File.WriteAllText(path,"old-profile-sentinel");
        Assert.Equal(ReviewCheckpointError.NotFound,Assert.Throws<ReviewCheckpointStoreException>(()=>f.Store.Read("one")).Error);var save=f.Save();
        Assert.Equal(ReviewCheckpointError.SlotOccupied,Assert.Throws<ReviewCheckpointStoreException>(()=>f.Store.Write("one",save,false)).Error);
        foreach(var path in oldPaths)Assert.Equal("old-profile-sentinel",File.ReadAllText(path));
        var trap=f.PathFor("directory");Directory.CreateDirectory(trap);Assert.Equal(ReviewCheckpointError.InvalidSlot,Assert.Throws<ReviewCheckpointStoreException>(()=>f.Store.Write("directory",save,true)).Error);
        Assert.Equal(ReviewCheckpointError.InvalidSlot,Assert.Throws<ReviewCheckpointStoreException>(()=>f.Store.Read("directory")).Error);
        foreach(var root in new[]{"relative-directory",Path.GetPathRoot(f.Root)!})
            Assert.Equal(ReviewCheckpointError.InvalidDirectory,Assert.Throws<ReviewCheckpointStoreException>(()=>new ReviewFrontierSlotStore(root,f.Codec)).Error);
        var fileRoot=Path.Combine(f.Root,"file-root");File.WriteAllText(fileRoot,"file");Assert.Equal(ReviewCheckpointError.InvalidDirectory,Assert.Throws<ReviewCheckpointStoreException>(()=>new ReviewFrontierSlotStore(fileRoot,f.Codec)).Error);
    }
    [Fact] public void RealPendingAndResolvedCombatDiskCheckpointsKeepAiStocksAndPublishAftermathOnce()
    {
        var p=CombatPackage();using var control=new Fixture(p);using var resumed=new Fixture(p);
        foreach(var f in new[]{control,resumed}){f.Route();f.Advance();Assert.Equal(SessionPhase.AwaitingCombat,f.Controller.Frame.Status.Phase);Assert.True(f.Controller.SelectEncounter(Assert.Single(f.Controller.Frame.Encounters)));}
        var pending=resumed.Save();Assert.Single(pending.Integrated!.Physical!.Core.Battles.Single().Commitments);Assert.Empty(resumed.Controller.Frame.Frontier!.Receipts);
        resumed.Reopen();Assert.Equal(Json(pending),Json(resumed.Save()));
        while(!control.Controller.Frame.Battle!.Complete)
        {
            control.Round();resumed.Round();var saved=resumed.Save();resumed.Reopen();Assert.Equal(Json(saved),Json(resumed.Save()));
            Assert.Empty(resumed.Controller.Frame.Frontier!.Receipts);
        }
        Assert.Equal(Json(control.Save()),Json(resumed.Save()));
        foreach(var f in new[]{control,resumed})
        {
            Assert.True(f.Controller.ReviewCompletion());var work=f.Controller.ConfirmCompletion()!;var receipt=f.Controller.ExecutePending(work);
            Assert.True(receipt.Result.Accepted);Assert.True(f.Controller.Receive(receipt));Assert.False(f.Controller.Receive(receipt));
        }
        var final=resumed.Save();Assert.Equal(Json(control.Save()),Json(final));Assert.Single(resumed.Controller.Frame.Frontier!.Receipts);
        resumed.Reopen();Assert.Equal(Json(final),Json(resumed.Save()));Assert.Single(resumed.Controller.Frame.Frontier!.Receipts);
    }
    [Theory][InlineData("json")][InlineData("wrong-profile")][InlineData("transcript")][InlineData("forged-receipt")]
    public void FailedFilesystemLoadPreservesHostGenerationAndUnsentDraft(string fault)
    {
        using var f=new Fixture();var save=f.Save();Assert.True(f.Controller.EditTravel(Fleet,CampaignTravelIntent.Stay));var frame=f.Controller.Frame;var draft=f.Controller.TravelDraft;
        if(fault=="json")File.WriteAllText(f.PathFor("one"),"{\"private-corruption\":true}");
        else if(fault=="wrong-profile")File.WriteAllText(f.PathFor("one"),"{\"FormatVersion\":4}");
        else
        {
            if(fault=="transcript")save.Transcript!.Add(new(){Kind=FrontierOperationKind.Advance,Turn=1,Revision=0});
            else save.Discoveries!.Add(new(){ActorId=A,FleetId=Fleet,ArrivalNodeId="nav.bajor",Turn=1,ChartedNodeIds=new(){"nav.gamma-exit"},ChartedLaneIds=new()});
            f.Store.Write("one",save,true); // Structurally valid DTO; backend must reject its claimed history.
        }
        Assert.False(f.Controller.LoadCommittedFromSlot("one",true).Succeeded);Assert.Same(frame,f.Controller.Frame);Assert.Same(draft,f.Controller.TravelDraft);
        Assert.Equal(frame.Generation,f.Host.Read(A).Generation);Assert.False(f.Controller.ReopenFromSlot("one",true).Succeeded);Assert.Same(frame,f.Controller.Frame);Assert.Same(draft,f.Controller.TravelDraft);
    }
    [Fact] public void SuccessfulFilesystemLoadRejectsOldOffersFramesAndPendingWork()
    {
        using var f=new Fixture();f.Save();var old=f.Controller.Frame;var offer=f.Controller.FindKnownRoute(Fleet,"nav.bajor")!;
        Assert.True(f.Controller.EditTravel(Fleet,CampaignTravelIntent.Leave,offer));Assert.True(f.Controller.ReviewTravel());var work=f.Controller.ConfirmTravel("command.frontier.disk.stale")!;var draft=f.Controller.TravelDraft;
        var second=new ReviewController(f.Host,A);Assert.True(second.LoadCommittedFromSlot("one").Succeeded);
        Assert.Throws<InvalidOperationException>(()=>f.Host.AdvancePlanning(old));Assert.Throws<InvalidOperationException>(()=>f.Controller.ExecutePending(work));Assert.Same(draft,f.Controller.TravelDraft);
        Assert.True(f.Controller.LoadCommittedFromSlot("one",true).Succeeded);Assert.False(f.Controller.EditTravel(Fleet,CampaignTravelIntent.Leave,offer));Assert.False(f.Controller.Dirty);
    }
    private static CampaignFrontierPackageDto CombatPackage()
    {
        var p=Package();var physical=p.Integrated!.Physical!;var scenario=physical.CorePackage!.Campaign!;
        scenario.InitialState.Fleets!.Single(f=>f.FleetId==Other).LocationNodeId="nav.bajor";
        scenario.Definitions.NavigationConnections!.Single(e=>e.ConnectionId=="connection.sol-bajor").BaseTraversalCost=1;
        var known=scenario.InitialState.NavigationKnowledge!.Single(k=>k.CivilizationId==B);
        foreach(var n in known.Nodes!)n.Level=NavigationKnowledgeLevel.Charted;foreach(var e in known.Connections!)e.Level=NavigationKnowledgeLevel.Charted;
        physical.InitialOrders!.Single(o=>o.FleetId==Other).Intent=CampaignTravelIntent.StayEngage;
        physical.InitialContacts!.Add(new(){ActorId=B,FleetId=Fleet,LastSeenNodeId="nav.sol",LastSeenTurn=1,TrackingNodeIds=known.Nodes!.Select(n=>n.Id).ToList()});return p;
    }
}
