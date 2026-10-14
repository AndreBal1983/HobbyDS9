using System.Security.Cryptography;
using System.Text.Json;
using HobbyDS9.Contracts;
using HobbyDS9.TacticalReviewClient;
using HobbyDS9.UnityReview;
using Xunit;

namespace HobbyDS9.UnityReviewAdapter.Tests;

internal sealed class UnityReviewFixture : IDisposable
{
    internal const string A="civ.federation",B="civ.cardassian",FA="fleet.federation-scout",FB="fleet.cardassian-scout";
    public string Root { get; }=Path.Combine(Path.GetTempPath(),"hobby-unity-adapter-"+Guid.NewGuid().ToString("N"));
    public string DirectoryPath => Path.Combine(Root,"review-only");
    public ReviewNewtonsoftCodec Codec { get; }=new ReviewNewtonsoftCodec();
    public CampaignTacticalTravelPackageDto Package { get; }
    public ReviewUnitySlotStore Store { get; }
    public PhysicalReviewHost Host { get; private set; }
    public ReviewController Own { get; private set; }
    public ReviewController Other { get; private set; }
    public UnityReviewFixture(string profile="faster",Action<UnityReviewWriteStage>? probe=null)
    {
        var dir=Path.Combine(AppContext.BaseDirectory,"scenarios","tactical-v2-physical-review",profile);
        Package=ReviewExactPackageLoader.Load(File.ReadAllBytes(Path.Combine(dir,"package.json")),File.ReadAllBytes(Path.Combine(dir,"core.json")),File.ReadAllBytes(Path.Combine(dir,"scenario.json")),Codec);
        Store=new ReviewUnitySlotStore(DirectoryPath,Codec,probe);
        Host=PhysicalReviewHost.Start(Package,Codec,Store);Own=new ReviewController(Host,A);Other=new ReviewController(Host,B);
    }
    public string PathFor(string slot)=>Path.Combine(DirectoryPath,"unity-physical-review-"+slot+".json");
    public CampaignTacticalTravelSaveDto RootSave(string slot="snapshot")
    {Assert.True(Own.SaveCommittedToSlot(slot,true).Succeeded);return Store.Read(slot);}
    public void Reopen(string slot)
    {
        var opened=PhysicalReviewHost.OpenFromSlot(Package,Codec,Store,slot,A,Own.Frame.Battle?.EncounterId);
        Assert.True(opened.Succeeded);Host=opened.Host!;Own=new ReviewController(Host,A);Other=new ReviewController(Host,B);
        Select();
    }
    private void Select()
    {if(Own.Frame.Encounters.Count==1){Assert.True(Own.SelectEncounter(Own.Frame.Encounters[0]));Assert.True(Other.SelectEncounter(Own.Frame.Encounters[0]));}}
    public static void Travel(PhysicalReviewHost host,ReviewController c,CampaignTravelIntent intent,string? dest=null,string? contact=null)
    {
        Assert.True(c.Refresh());var f=c.Frame.OwnStrategicFleets.Single().FleetId.Value;
        var route=dest==null?null:c.FindKnownRoute(f,dest);Assert.True(c.EditTravel(f,intent,route,contact));Assert.True(c.ReviewTravel());
        var r=host.Execute(c.ConfirmTravel("command.physical."+c.Frame.Actor+"."+c.Frame.Status.Turn)!);
        Assert.True(r.Result.Accepted);Assert.True(c.Receive(r));
    }
    public void Pursue()
    {
        Assert.True(Host.SetApproachCloak(Own.Frame,"command.physical.cloak",FA,true).Accepted);Own.Refresh();Other.Refresh();
        Travel(Host,Own,CampaignTravelIntent.Follow,"nav.bajor",FB);Travel(Host,Other,CampaignTravelIntent.Leave,"nav.bajoran-wormhole");Advance();
    }
    public void Advance()
    {Own.Refresh();Assert.True(Host.AdvancePlanning(Own.Frame).Accepted);Own.Refresh();Other.Refresh();Select();}
    public ReviewReceipt Commit(ReviewController c)
    {
        var b=c.Frame.Battle!;
        foreach(var o in b.StandingOrders.Where(o=>o.CanOrder))
            Assert.True(c.EditFleet(o.FleetId,b.Round==2?CampaignFleetManeuver.Flank:b.Round==3?CampaignFleetManeuver.Screen:CampaignFleetManeuver.Hold,o.FocusFleetId));
        Assert.True(c.ReviewRound());var r=Host.Execute(c.ConfirmRound("command.physical.round."+b.EncounterId+"."+c.Frame.Actor+"."+b.Round)!);
        Assert.True(r.Result.Accepted);Assert.True(c.Receive(r));return r;
    }
    public void Finish(bool reload=false)
    {
        while(!Own.Frame.Battle!.Complete)
        {
            if(!Own.Frame.Battle.OwnCommitmentAccepted)Commit(Own);
            if(reload){RootSave("waiting");Reopen("waiting");}
            if(!Other.Frame.Battle!.OwnCommitmentAccepted)Commit(Other);
            Own.Refresh();Other.Refresh();
            if(reload){RootSave("resolved");Reopen("resolved");}
        }
        Assert.True(Own.ReviewCompletion());var r=Host.Execute(Own.ConfirmCompletion()!);Assert.True(r.Result.Accepted);Assert.True(Own.Receive(r));Other.Refresh();
    }
    public void Reengage()
    {Travel(Host,Own,CampaignTravelIntent.StayEngage);Travel(Host,Other,CampaignTravelIntent.StayEngage);Advance();}
    public string CanonicalVector()=>Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(RootSave()))).ToLowerInvariant();
    public void Dispose()
    {
        var full=Path.GetFullPath(Root);var basis=Path.TrimEndingDirectorySeparator(Path.GetFullPath(Path.GetTempPath()));
        if(!full.StartsWith(basis+Path.DirectorySeparatorChar,StringComparison.Ordinal)||!Path.GetFileName(full).StartsWith("hobby-unity-adapter-",StringComparison.Ordinal))throw new InvalidOperationException("Invalid test cleanup path.");
        if(Directory.Exists(full))Directory.Delete(full,true);
    }
}
