using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using HobbyDS9.Contracts;
using HobbyDS9.TacticalReviewClient;
using Xunit;

namespace HobbyDS9.TacticalReviewClient.Tests;

// Real authored package and Application factories. No mocked session, receipt or authority.
internal sealed class JsonHostCodec : IReviewHostCodec
{
    public bool FailSaveCopy { get; set; }
    public bool ForgeSaveIdentity { get; set; }
    public string? LastPrivateSerializedSave { get; private set; }
    public CampaignTacticalReviewPackageDto CopyPackage(CampaignTacticalReviewPackageDto package)
        => JsonSerializer.Deserialize<CampaignTacticalReviewPackageDto>(JsonSerializer.Serialize(package))!;
    public CampaignTacticalReviewSaveDto CopySave(CampaignTacticalReviewSaveDto save)
    {
        if(FailSaveCopy) throw new IOException("Injected host serialization failure.");
        LastPrivateSerializedSave=JsonSerializer.Serialize(save);
        var copy=JsonSerializer.Deserialize<CampaignTacticalReviewSaveDto>(LastPrivateSerializedSave)!;
        if(ForgeSaveIdentity) copy.PackageHash="wrong-review-package";
        return copy;
    }
}
internal sealed class ActualReviewFixture
{
    internal const string ActorA="civ.federation", ActorB="civ.cardassian";
    internal const string FleetA="fleet.federation-scout", FleetB="fleet.cardassian-scout";
    public JsonHostCodec Codec { get; } = new JsonHostCodec();
    public ReviewHost Host { get; }
    public ReviewController A { get; private set; }
    public ReviewController B { get; private set; }
    public ActualReviewFixture(Action<CampaignTacticalReviewPackageDto>? adjust = null)
    {
        var package=Package(); adjust?.Invoke(package);
        Host=ReviewHost.Start(package,Codec);
        A=new ReviewController(Host,ActorA); B=new ReviewController(Host,ActorB);
    }
    public static CampaignTacticalReviewPackageDto Package()
    {
        var folder=Path.Combine(AppContext.BaseDirectory,"scenarios","tactical-v2-campaign-review");
        var source=File.ReadAllBytes(Path.Combine(folder,"scenario.json"));
        var profile=File.ReadAllBytes(Path.Combine(folder,"package.json"));
        var p=JsonSerializer.Deserialize<CampaignTacticalReviewPackageDto>(profile)!;
        p.Campaign=JsonSerializer.Deserialize<ScenarioDto>(source)!;
        p.Campaign.ContentHash=Hash(source);
        p.PackageHash=Hash(Encoding.ASCII.GetBytes("campaign-tactical-review-package-v1\n"+Hash(profile)+"\n"+Hash(source)+"\n"));
        return p;
    }
    private static string Hash(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
    public string Allocate()
    {
        Assert.True(Host.AdvancePlanning(A.Frame).Accepted);
        Assert.True(A.Refresh()); Assert.True(B.Refresh());
        var encounter=Assert.Single(A.Frame.Encounters);
        Assert.True(A.SelectEncounter(encounter)); Assert.True(B.SelectEncounter(encounter));
        return encounter;
    }
    public static ReviewWork Begin(ReviewController c, string? commandId = null)
    {
        Assert.True(c.ReviewRound());
        return Assert.IsType<ReviewWork>(c.ConfirmRound(commandId ?? "command.client."+c.Frame.Actor+"."+c.Frame.Battle!.EncounterId+"."+c.Frame.Battle.Round));
    }
    public ReviewReceipt Send(ReviewController c)
    {
        var receipt=Host.Execute(Begin(c));
        Assert.True(receipt.Result.Accepted); Assert.True(c.Receive(receipt));
        return receipt;
    }
    public void Round(bool reverse=false, CampaignFleetManeuver? maneuver=null)
    {
        Assert.True(A.Refresh()); Assert.True(B.Refresh());
        if(maneuver.HasValue)
        {
            foreach(var c in new[]{A,B})
            foreach(var o in c.Frame.Battle!.StandingOrders.Where(o=>o.CanOrder))
                Assert.True(c.EditFleet(o.FleetId,maneuver.Value,o.FocusFleetId));
        }
        Send(reverse ? B : A); Send(reverse ? A : B);
        Assert.True(A.Refresh()); Assert.True(B.Refresh());
    }
    public void Complete(bool reverse=false)
    {
        while(!A.Frame.Battle!.Complete) Round(reverse,A.Frame.Battle.Round == 2 ? CampaignFleetManeuver.Flank : null);
    }
    public ReviewReceipt Finalize()
    {
        Assert.True(A.ReviewCompletion());
        var work=Assert.IsType<ReviewWork>(A.ConfirmCompletion());
        var receipt=Host.Execute(work); Assert.True(receipt.Result.Accepted); Assert.True(A.Receive(receipt));
        return receipt;
    }
    public void RebindB()
    {
        B=new ReviewController(Host,ActorB);
        if(B.Frame.Encounters.Count > 0) Assert.True(B.SelectEncounter(Assert.Single(B.Frame.Encounters)));
    }
    public void Cloak(string actor, string fleet)
    {
        Assert.True(Host.SetApproachCloak(Host.Read(actor),"command.cloak."+actor,fleet,true).Accepted);
        Assert.True(A.Refresh()); Assert.True(B.Refresh());
    }
    public static void RemoveSurveyor(CampaignTacticalReviewPackageDto p)
    {
        var s=p.Campaign!.InitialState;
        s.Ships!.RemoveAll(x=>x.ShipId == "ship.equipment-surveyor");
        s.EntityIdentities.RemoveAll(x=>x.EntityId == "ship.equipment-surveyor");
        s.Fleets!.Single(f=>f.OwnerCivilizationId == ActorA).ShipIds!.Remove("ship.equipment-surveyor");
        p.Campaign.Definitions.AuthoredTacticalEquipment!.InitialShipProvisions!.RemoveAll(x=>x.ShipId == "ship.equipment-surveyor");
    }
}
