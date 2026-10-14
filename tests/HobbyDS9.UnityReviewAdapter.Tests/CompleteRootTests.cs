using System.Text;
using System.Text.Json;
using HobbyDS9.Application;
using HobbyDS9.Contracts;
using HobbyDS9.UnityReview;
using Newtonsoft.Json.Linq;
using Xunit;
using static HobbyDS9.UnityReviewAdapter.Tests.UnityReviewFixture;

namespace HobbyDS9.UnityReviewAdapter.Tests;

public sealed class CompleteRootTests
{
    [Fact]
    public void ActualNewtonsoftCompleteRootResumesWaitingAndResolvedThenDepletedSecondBattle()
    {
        using var control=new UnityReviewFixture();using var resumed=new UnityReviewFixture();
        var planning=resumed.RootSave();Assert.Null(planning.MovementProof);Assert.Equal(2,planning.FormatVersion);
        resumed.Reopen("snapshot");control.Pursue();resumed.Pursue();
        var pending=resumed.RootSave("pending");Assert.NotNull(pending.MovementProof);Assert.NotEmpty(pending.Requests);
        Assert.Equal(SessionPhase.AwaitingCombat,pending.Phase);Assert.Equal(SessionPhase.Planning,pending.Core.Campaign.GameState.Phase);
        resumed.Commit(resumed.Own);var waiting=resumed.RootSave("waiting");Assert.Single(waiting.Core.Battles.Single().Commitments);
        resumed.Reopen("waiting");resumed.Finish(true);control.Finish();
        Assert.Equal(JsonSerializer.Serialize(control.RootSave()),JsonSerializer.Serialize(resumed.RootSave()));
        Assert.Equal(3,resumed.Own.Frame.OwnShips.Single().Weapons.Single(w=>w.Class==TacticalWeaponClass.Torpedo).Ammunition);
        resumed.Reopen("snapshot");control.Reengage();resumed.Reengage();control.Finish();resumed.Finish(true);
        Assert.Equal(0,resumed.Own.Frame.OwnShips.Single().Weapons.Single(w=>w.Class==TacticalWeaponClass.Torpedo).Ammunition);
        Assert.Equal("a6a2522a4f389b0bb8aa562d6273b0f02b3ea1fdeb57c7fe21d8368e4fa4d177",resumed.CanonicalVector());
        Assert.Equal(control.CanonicalVector(),resumed.CanonicalVector());
    }
    [Fact]
    public void SlowerCompletePhysicalRootRetainsWeightedProgressGoldenAndLawfulLastSeen()
    {
        using var f=new UnityReviewFixture("slower");f.Pursue();f.RootSave();f.Reopen("snapshot");
        Assert.Equal("77073cc154b78dbca2e5f6fa1a618cae490d11410f51f4cb2bb9e640a33210c4",f.CanonicalVector());
        Assert.Equal(2,Assert.Single(f.Own.Frame.Travel!.OwnReceipts).TransitProgress);
        var text=HobbyDS9.TacticalReviewClient.ReviewTextRenderer.Render(f.Own);
        Assert.Contains("LAST SEEN nav.sol, turn 1",text);Assert.DoesNotContain("nav.bajoran-wormhole",text);
    }
    [Theory]
    [InlineData("format")][InlineData("phase")][InlineData("pool")][InlineData("proof")][InlineData("core")]
    public void ActualApplicationRejectsSemanticallyCorruptRoot(string corruption)
    {
        using var f=new UnityReviewFixture();f.Pursue();var root=f.RootSave();
        switch(corruption){case "format":root.FormatVersion=1;break;case "phase":root.Phase=(SessionPhase)999;break;
            case "pool":root.Core.Pools[0].FitRevision=-1;break;case "proof":root.MovementProof!.Start.TurnNumber++;break;
            case "core":root.Core=null!;break;}
        var decoded=f.Codec.DecodeRoot(f.Codec.EncodeRoot(root));
        Assert.ThrowsAny<Exception>(()=>CampaignTacticalTravelApplication.LoadReviewSave(f.Package,decoded));
    }
    [Theory]
    [InlineData("missing")][InlineData("case")][InlineData("unknown")][InlineData("numeric-string")][InlineData("fraction")][InlineData("null-scalar")][InlineData("enum-string")][InlineData("duplicate")][InlineData("trailing")][InlineData("wrong-array")]
    public void CodecRejectsMalformedShapeWithoutPrivateDiagnostics(string mutation)
    {
        using var f=new UnityReviewFixture();var obj=JObject.Parse(Encoding.UTF8.GetString(f.Codec.EncodeRoot(f.RootSave())));
        switch(mutation){case "missing":obj.Remove("FormatVersion");break;case "case":obj["formatVersion"]=obj["FormatVersion"];obj.Remove("FormatVersion");break;
            case "unknown":obj["PrivateUnknown"]="secret";break;case "numeric-string":obj["FormatVersion"]="2";break;
            case "fraction":obj["FormatVersion"]=2.0;break;case "null-scalar":obj["Phase"]=null;break;case "enum-string":obj["Phase"]="Planning";break;
            case "wrong-array":obj["Orders"]=new JObject();break;}
        var json=obj.ToString(Newtonsoft.Json.Formatting.None);
        if(mutation=="duplicate")json=json.Insert(1,"\"FormatVersion\":2,");if(mutation=="trailing")json+="{}";
        var ex=Assert.Throws<InvalidDataException>(()=>f.Codec.DecodeRoot(Encoding.UTF8.GetBytes(json)));
        Assert.Equal("Invalid physical review data.",ex.Message);Assert.Null(ex.InnerException);
    }
    [Fact]
    public void Int64UInt64NullableZeroFalseAndNullSurviveWithoutFloatingPointConversion()
    {
        using var f=new UnityReviewFixture();var root=f.RootSave();
        root.Core.Campaign.GameState.StateRevision=long.MaxValue;root.Core.Campaign.GameState.NextEntitySequence=ulong.MaxValue;
        root.Core.Campaign.GameState.RngState.State=ulong.MaxValue;root.Orders[0].ExpectedContactSeenTurn=0;
        root.Core.FleetSettings[0].ApproachCloak=false;root.MovementProof=null;
        var copy=f.Codec.CopySave(root);Assert.Equal(long.MaxValue,copy.Core.Campaign.GameState.StateRevision);
        Assert.Equal(ulong.MaxValue,copy.Core.Campaign.GameState.NextEntitySequence);Assert.Equal(ulong.MaxValue,copy.Core.Campaign.GameState.RngState.State);
        Assert.Equal(0,copy.Orders[0].ExpectedContactSeenTurn);Assert.False(copy.Core.FleetSettings[0].ApproachCloak);Assert.Null(copy.MovementProof);
        root.Orders[0].ExpectedContactSeenTurn=null;Assert.Null(f.Codec.CopySave(root).Orders[0].ExpectedContactSeenTurn);
    }
    [Fact]
    public void HashesUseExactSourceBytesAndWhitespaceChangedContentIsNotSamePackage()
    {
        using var f=new UnityReviewFixture();var dir=Path.Combine(AppContext.BaseDirectory,"scenarios","tactical-v2-physical-review","faster");
        Assert.Equal("49e54967f76f8d84822731f8a31b715775ca673ea034d9643079979098735541",f.Package.PackageHash);
        var p=File.ReadAllBytes(Path.Combine(dir,"package.json"));var c=File.ReadAllBytes(Path.Combine(dir,"core.json"));var s=File.ReadAllBytes(Path.Combine(dir,"scenario.json"));
        var changed=ReviewExactPackageLoader.Load(p,c,s.Concat(new byte[]{10}).ToArray(),f.Codec);
        Assert.NotEqual(f.Package.PackageHash,changed.PackageHash);Assert.NotEqual(f.Package.CorePackage!.Campaign!.ContentHash,changed.CorePackage!.Campaign!.ContentHash);
        Assert.ThrowsAny<Exception>(()=>CampaignTacticalTravelApplication.LoadReviewSave(changed,f.RootSave()));
    }
}
