using System.Text;
using HobbyDS9.Application;
using HobbyDS9.Contracts;
using HobbyDS9.UnityReview;
using Xunit;
namespace HobbyDS9.UnityReviewAdapter.Tests;
public sealed class DemoUiFixture : IDisposable
{
    public string Folder {get;}=Path.Combine(Path.GetTempPath(),"hobby-singleplayer-ui-"+Guid.NewGuid().ToString("N"));
    public ReviewNewtonsoftCodec Codec {get;}=new();
    public CampaignSingleplayerDemoPackageDto Package {get;}
    public ReviewDemoSlotStore Store {get;}
    public ReviewUiModel Model {get;}
    public static byte[] Input(string name)=>File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,"scenarios","singleplayer-demo-v1",name+".json"));
    public DemoUiFixture(Action<UnityReviewWriteStage>? fault=null)
    {Package=ReviewExactPackageLoader.LoadDemo(Input("demo"),Input("package"),Input("physical"),Input("core"),Input("scenario"),Codec);Store=new ReviewDemoSlotStore(Folder,Codec,fault);Model=new ReviewUiModel(Package,Codec,Store);}
    public void Dispose()
    {if(Directory.Exists(Folder)&&Path.GetFullPath(Folder).StartsWith(Path.Combine(Path.GetTempPath(),"hobby-singleplayer-ui-"),StringComparison.Ordinal))Directory.Delete(Folder,true);}
}
public sealed class SingleplayerDemoUiTests
{
    [Fact] public void ExplicitLaunchCannotSelectAnOpponentOrFallBackToAnotherProfile()
    {
        Assert.False(SingleplayerDemoLaunchOptions.DefaultEnabled);Assert.False(SingleplayerDemoLaunchOptions.TryParse(Array.Empty<string>(),out _));
        Assert.True(SingleplayerDemoLaunchOptions.TryParse(new[]{SingleplayerDemoLaunchOptions.Flag,"-screen-width","1280"},out _));
        foreach(var forbidden in new[]{"--hobbyds9-physical-review","--hobbyds9-integrated-playtest","--hobbyds9-review-actor","--hobbyds9-integrated-actor",SingleplayerDemoLaunchOptions.Flag})
            Assert.False(SingleplayerDemoLaunchOptions.TryParse(new[]{SingleplayerDemoLaunchOptions.Flag,forbidden,"civ.cardassian"},out _));
    }
    [Fact] public void ModelUsesRealSingleHumanPaidProductionAndCompleteFourSave()
    {
        using var f=new DemoUiFixture();var m=f.Model;var initial=m.Current.Frame.Strategy!.Ships.Select(s=>s.ShipId.Value).ToArray();
        Assert.True(m.SingleplayerDemo);Assert.Equal("civ.federation",m.Current.Frame.Demo!.HumanActorId);Assert.False(m.SwitchActor("civ.cardassian"));
        Assert.Equal(300,m.Current.Frame.Strategy.Civilization.Credits);Assert.True(m.Enqueue("colony.sol","ship-design.source-cruiser",ReviewUiModel.NewCommandId()));
        Assert.Equal(180,m.Current.Frame.Strategy.Civilization.Credits);Assert.True(m.Advance());Assert.True(m.Advance());
        Assert.Single(m.Current.Frame.Strategy.Ships,s=>!initial.Contains(s.ShipId.Value));
        Assert.True(m.Save("demo"));var saved=f.Store.Read("demo");Assert.Equal(4,saved.FormatVersion);Assert.NotNull(saved.OpponentPlan);
        Assert.Single(saved.Integrated!.BuiltShips!,s=>s.OwnerCivilizationId=="civ.federation");Assert.Contains(saved.Integrated.BuiltShips!,s=>s.OwnerCivilizationId=="civ.cardassian");
        Assert.True(File.Exists(Path.Combine(f.Folder,"unity-singleplayer-demo-demo.json")));Assert.False(File.Exists(Path.Combine(f.Folder,"unity-integrated-playtest-demo.json")));
    }
    [Fact] public void FailedLoadsKeepDraftButSuccessfulLoadReplacesHostAndControllerTogether()
    {
        using var f=new DemoUiFixture();var m=f.Model;Assert.True(m.Save("resume"));var old=m.Current;
        m.ResearchInputs()!.ScienceUnits=600;Assert.True(m.Save("draft-kept"));Assert.True(m.HasLocalEdits);
        Assert.False(m.Load("missing",true));Assert.Same(old,m.Current);Assert.True(m.HasLocalEdits);
        Assert.True(m.Load("resume",true));Assert.NotSame(old,m.Current);Assert.False(m.HasLocalEdits);
        Assert.True(m.AllocateResearch(600,ReviewUiModel.NewCommandId()));Assert.True(m.Enqueue("colony.sol","ship-design.source-cruiser",ReviewUiModel.NewCommandId()));
        Assert.Equal(180,m.Current.Frame.Strategy!.Civilization.Credits);Assert.True(m.Save("after-reopen"));
        Assert.Single(f.Store.Read("after-reopen").Integrated!.Physical!.Core.Campaign.GameState.Colonies!.Single(c=>c.ColonyId=="colony.sol").ProductionQueue!);
    }
    [Fact] public void CodecAndHostNeverTreatNestedIntegratedOrIncompleteFourAsDemo()
    {
        using var f=new DemoUiFixture();Assert.True(f.Model.Save("full"));var full=f.Store.Read("full");
        Assert.Throws<InvalidDataException>(()=>f.Codec.DecodeDemoRoot(f.Codec.EncodeIntegratedRoot(full.Integrated!)));
        Assert.Throws<InvalidDataException>(()=>f.Codec.EncodeDemoRoot(new CampaignSingleplayerDemoSaveDto{FormatVersion=4,ProfileId=CampaignSingleplayerDemoIdentity.Profile,Integrated=full.Integrated}));
        var json=Encoding.UTF8.GetString(f.Codec.EncodeDemoRoot(full));Assert.Throws<InvalidDataException>(()=>f.Codec.DecodeDemoRoot(Encoding.UTF8.GetBytes(json.Replace("\"FormatVersion\":4","\"FormatVersion\":3"))));
        var app=CampaignSingleplayerDemoApplication.StartPackage(f.Package);Assert.Throws<ArgumentException>(()=>app.ExportIntegratedSave());Assert.Throws<InvalidOperationException>(()=>app.ExportSave());
    }
    [Theory][InlineData("demo")][InlineData("package")][InlineData("physical")][InlineData("core")][InlineData("scenario")]
    public void EveryExactRawInputIsBoundToDemoSaveIdentity(string changed)
    {
        using var f=new DemoUiFixture();Assert.True(f.Model.Save("bound"));var inputs=new[]{"demo","package","physical","core","scenario"}.ToDictionary(s=>s,DemoUiFixture.Input);
        inputs[changed]=inputs[changed].Concat(new byte[]{32}).ToArray();var other=ReviewExactPackageLoader.LoadDemo(inputs["demo"],inputs["package"],inputs["physical"],inputs["core"],inputs["scenario"],f.Codec);
        Assert.NotEqual(f.Package.PackageHash,other.PackageHash);var candidate=new ReviewUiModel(other,f.Codec,f.Store);var before=candidate.Current;
        Assert.False(candidate.Load("bound",true));Assert.Same(before,candidate.Current);
    }
    [Theory][InlineData(UnityReviewWriteStage.BeforeWrite)][InlineData(UnityReviewWriteStage.AfterPartialWrite)][InlineData(UnityReviewWriteStage.BeforeReplace)]
    public void AtomicFailurePreservesPriorCompleteFourAndUnsentResearch(UnityReviewWriteStage stage)
    {
        bool fail=false;using var f=new DemoUiFixture(s=>{if(fail&&s==stage)throw new IOException("private diagnostic");});var m=f.Model;Assert.True(m.Save("atomic"));
        var path=Path.Combine(f.Folder,"unity-singleplayer-demo-atomic.json");var before=File.ReadAllBytes(path);m.ResearchInputs()!.ScienceUnits=600;fail=true;
        Assert.False(m.Save("atomic",true));Assert.Equal(before,File.ReadAllBytes(path));Assert.True(m.HasLocalEdits);Assert.DoesNotContain("private diagnostic",m.Message);
    }
    [Fact] public void PendingBattleResumeAndHumanCommitUseCurrentRealAiHost()
    {
        using var f=new DemoUiFixture();var m=f.Model;Assert.True(m.DraftTravel("fleet.federation-scout",CampaignTravelIntent.Leave,"nav.bajor",null));Assert.True(m.SendTravel(ReviewUiModel.NewCommandId()));
        int limit=0;while(m.Current.Frame.Status.Phase==SessionPhase.Planning&&m.Current.Frame.OwnStrategicFleets.Single().LocationNodeId.Value!="nav.bajor"&&limit++<5)Assert.True(m.Advance());
        if(m.Current.Frame.Status.Phase==SessionPhase.Planning){Assert.True(m.DraftTravel("fleet.federation-scout",CampaignTravelIntent.StayEngage,null,null));Assert.True(m.SendTravel(ReviewUiModel.NewCommandId()));Assert.True(m.Advance());}
        Assert.NotNull(m.Current.Frame.Battle);var round=m.Current.Frame.Battle!.Round;Assert.True(m.Save("pending"));Assert.True(m.Load("pending",true));Assert.Equal(round,m.Current.Frame.Battle!.Round);
        Assert.True(m.CommitRound(ReviewUiModel.NewCommandId()));Assert.False(m.Current.Frame.Battle!.OwnCommitmentAccepted);Assert.True(m.Current.Frame.Battle.Reports.Count>0);
    }
}
