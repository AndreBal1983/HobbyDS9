using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using HobbyDS9.Contracts;
using HobbyDS9.UnityReview;
using Xunit;
using static HobbyDS9.UnityReviewAdapter.Tests.UnityReviewFixture;

namespace HobbyDS9.UnityReviewAdapter.Tests;
public sealed class IntegratedUiFixture : IDisposable
{
    public string Folder { get; }=Path.Combine(Path.GetTempPath(),"hobby-integrated-ui-"+Guid.NewGuid().ToString("N"));
    public ReviewNewtonsoftCodec Codec { get; }=new();
    public CampaignIntegratedPlaytestPackageDto Package { get; }
    public ReviewIntegratedSlotStore Store { get; }
    public ReviewUiModel Model { get; }
    public static byte[] Input(string name)=>File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,"scenarios","integrated-playtest-v1",name+".json"));
    public IntegratedUiFixture(Action<UnityReviewWriteStage>? fault=null)
    {Package=ReviewExactPackageLoader.LoadIntegrated(Input("package"),Input("physical"),Input("core"),Input("scenario"),Codec);Store=new ReviewIntegratedSlotStore(Folder,Codec,fault);Model=new ReviewUiModel(Package,Codec,Store,A);}
    public void Dispose(){if(Directory.Exists(Folder))Directory.Delete(Folder,true);}
}
public sealed class IntegratedUiTests
{
    [Fact]
    public void ChangingMapDestinationInvalidatesPreviewAndExactOfferSubmissionNeverRecomputesIt()
    {
        using var f=new IntegratedUiFixture();var m=f.Model;var workspace=new ReviewWorkspace(new ReviewCombatFlow(m));
        workspace.SelectedFleet="fleet.federation-scout";workspace.SelectedNode="nav.bajor";
        var offer=m.Current.FindKnownRoute(workspace.SelectedFleet,workspace.SelectedNode)!;workspace.RoutePreview=offer;
        Assert.True(workspace.RoutePreviewMatchesCurrent);Assert.True(workspace.Navigate(ReviewWorkspacePage.Map));
        workspace.SelectedNode="nav.cardassia";Assert.Null(workspace.RoutePreview);Assert.False(workspace.RoutePreviewMatchesCurrent);
        Assert.True(workspace.Navigate(ReviewWorkspacePage.FleetOrders));Assert.False(workspace.RoutePreviewMatchesCurrent);
        // Even a frozen old callback carries B's exact offer; it never replans silently to C.
        Assert.True(m.SendPreviewedTravel(offer,ReviewUiModel.NewCommandId()));
        Assert.Equal(offer.Route.Steps,m.Current.Frame.OwnStrategicFleets.Single().RouteNodeIds.Select(n=>n.Value));
        var frame=m.Current.Frame;Assert.False(m.SendPreviewedTravel(offer,ReviewUiModel.NewCommandId()));Assert.Same(frame,m.Current.Frame);
    }
    [Fact]
    public void LocalDesignDraftBlocksAdvanceTravelAndActorReplacementButSaveKeepsIt()
    {
        using var f=new IntegratedUiFixture();var m=f.Model;var frame=m.Current.Frame;
        m.LocalDesignDirty=true;bool reset=false;m.ResetLocalDesign=()=>reset=true;
        Assert.True(m.HasLocalEdits);Assert.False(m.Advance());
        Assert.False(m.DraftTravel("fleet.federation-scout",CampaignTravelIntent.Leave,"nav.bajor",null));
        Assert.False(m.SwitchActor(B));Assert.Same(frame,m.Current.Frame);
        Assert.True(m.Save("design-local"));Assert.True(m.LocalDesignDirty);Assert.False(reset);
        Assert.False(m.Load("missing",true));Assert.Same(frame,m.Current.Frame);Assert.True(m.LocalDesignDirty);
        Assert.True(m.Reset());Assert.True(reset);Assert.False(m.HasLocalEdits);
        m.LocalDesignDirty=true;Assert.True(m.Load("design-local",true));Assert.False(m.LocalDesignDirty);Assert.Null(m.ResetLocalDesign);
    }
    [Fact]
    public void OwnedDesignCatalogueAndPublishedLibraryUseTheSameScopedHostWithoutMutation()
    {
        using var f=new IntegratedUiFixture();var m=f.Model;var frame=m.Current.Frame;
        var library=m.DesignLibrary();Assert.Equal(A,library.Actor);Assert.NotNull(library.Catalogue);
        Assert.False(library.CanConstructPublishedBlueprints);Assert.Same(frame,m.Current.Frame);
        var workspace=new ReviewWorkspace(new ReviewCombatFlow(m)){SelectedColony="colony.sol"};
        Assert.True(workspace.Navigate(ReviewWorkspacePage.Worlds));Assert.True(workspace.Navigate(ReviewWorkspacePage.Production));
        Assert.Equal("colony.sol",workspace.SelectedColony);Assert.Same(frame,m.Current.Frame);
        Assert.True(m.SwitchActor(B));_ =workspace.Page;Assert.Null(workspace.SelectedColony);
        Assert.Equal(B,m.DesignLibrary().Actor);
    }
    [Theory][InlineData("none",false)][InlineData("physical",false)][InlineData("missing",false)][InlineData("invalid",false)][InlineData("both",false)][InlineData("valid",true)]
    public void ExplicitIntegratedSelectorDoesNotReusePhysicalFlagsOrEnableDefault(string scenario,bool accepted)
    {
        var args=scenario=="none"?Array.Empty<string>():scenario=="physical"?new[]{"--hobbyds9-physical-review","--hobbyds9-review-profile","faster","--hobbyds9-review-actor",A}:scenario=="missing"?new[]{"--hobbyds9-integrated-playtest"}:scenario=="invalid"?new[]{"--hobbyds9-integrated-playtest","--hobbyds9-integrated-actor","../bad"}:scenario=="both"?new[]{"--hobbyds9-integrated-playtest","--hobbyds9-physical-review","--hobbyds9-integrated-actor",A}:new[]{"--hobbyds9-integrated-playtest","--hobbyds9-integrated-actor",A};
        Assert.False(IntegratedLaunchOptions.DefaultEnabled);Assert.Equal(accepted,IntegratedLaunchOptions.TryParse(args,out var options));
        if(accepted){Assert.Equal(A,options!.Actor);Assert.False(ReviewLaunchOptions.TryParse(args,out _));}
    }
    [Theory][InlineData("package")][InlineData("physical")][InlineData("core")][InlineData("scenario")]
    public void ExactRawSourceBytesAllParticipateInIntegratedIdentity(string altered)
    {
        using var f=new IntegratedUiFixture();var values=new[]{"package","physical","core","scenario"}.ToDictionary(n=>n,IntegratedUiFixture.Input);
        values[altered]=values[altered].Concat(new byte[]{32}).ToArray();
        var other=ReviewExactPackageLoader.LoadIntegrated(values["package"],values["physical"],values["core"],values["scenario"],f.Codec);
        Assert.NotEqual(f.Package.PackageHash,other.PackageHash);Assert.True(f.Model.Save("bound"));
        var candidate=HobbyDS9.TacticalReviewClient.PhysicalReviewHost.OpenIntegratedFromSlot(other,f.Codec,f.Store,"bound",A);
        Assert.False(candidate.Succeeded);Assert.Null(candidate.Host);
    }
    private static string Id(ReviewUiModel m)=>"command.walkthrough."+m.Current.Frame.Status.Turn+"."+m.Current.Frame.Status.CampaignRevision;
    private static void Travel(ReviewUiModel m,string actor,string fleet,CampaignTravelIntent intent,string? destination=null)
    {Assert.True(m.SwitchActor(actor));Assert.True(m.DraftTravel(fleet,intent,destination,null));Assert.True(m.SendTravel("command.walkthrough.travel."+m.Current.Frame.Status.CampaignRevision));}
    private static void Advance(ReviewUiModel m){Assert.True(m.Advance());}
    [Fact]
    public void ActualOneHostResearchPaidConstructionProducedShipBattleAftermathAndWholeRootResume()
    {
        using var f=new IntegratedUiFixture();var m=f.Model;var workspace=new ReviewWorkspace(new ReviewCombatFlow(m));
        var original=m.Current;var initial=m.Current.Frame.Strategy!.Ships.Select(s=>s.ShipId.Value).ToHashSet();
        foreach(var page in Enum.GetValues<ReviewWorkspacePage>()){Assert.True(workspace.Navigate(page));Assert.Same(original,m.Current);}
        Assert.Equal("88a919f6f51d244f79bb638ba933ff3c17305f25231a6b793aa6d53fca5f688b",f.Package.PackageHash);
        Assert.True(m.AllocateScience(Enum.GetValues<ScienceDiscipline>().Select((d,i)=>new ScienceAllocationDto{Discipline=d,Weight=i==0?150:i==1?50:100}).ToList(),Id(m)));
        Assert.True(m.AllocateResearch(600,Id(m)));
        Assert.True(m.Enqueue("colony.sol","ship-design.source-cruiser",Id(m)));
        Assert.Equal(50,m.Current.Frame.Strategy!.Civilization.Credits);var item=Assert.Single(m.Current.Frame.Strategy.Colonies.Single(c=>c.ColonyId.Value=="colony.sol").ProductionQueue);
        Assert.Equal(200,item.PaidCredits);Assert.Equal(5,Assert.Single(item.PaidResources).Amount);Assert.Equal(100,item.RequiredIndustry);
        var before=m.Current.Frame.Strategy.Research.Science.Select(s=>s.Progress).ToArray();int limit=0;
        while(m.Current.Frame.Strategy!.Ships.All(s=>initial.Contains(s.ShipId.Value))&&limit++<20)Advance(m);
        var produced=Assert.Single(m.Current.Frame.Strategy!.Ships,s=>!initial.Contains(s.ShipId.Value));
        Assert.Equal(14,m.Current.Frame.Status.Turn);Assert.Equal(220,produced.CurrentHull);Assert.NotEqual(before,m.Current.Frame.Strategy.Research.Science.Select(s=>s.Progress).ToArray());
        Assert.Equal(7,m.Current.Frame.OwnShips.Single(s=>s.Condition.ShipId==produced.ShipId.Value).Weapons.Single(w=>w.Class==TacticalWeaponClass.Torpedo).Ammunition);
        Travel(m,A,FA,CampaignTravelIntent.Leave,"nav.bajor");Travel(m,B,FB,CampaignTravelIntent.Leave,"nav.bajor");
        while(m.Current.Frame.Status.Phase==SessionPhase.Planning&&m.Current.Frame.OwnStrategicFleets.Single().LocationNodeId.Value!="nav.bajor")Advance(m);
        if(m.Current.Frame.Status.Phase==SessionPhase.Planning){Travel(m,A,FA,CampaignTravelIntent.StayEngage);Advance(m);}
        Assert.Equal(SessionPhase.AwaitingCombat,m.Current.Frame.Status.Phase);Assert.True(m.SwitchActor(A));var flow=workspace.Combat;
        Assert.Contains(m.Current.Frame.Battle!.OwnShips,s=>s.Condition.ShipId==produced.ShipId.Value);
        while(!m.Current.Frame.Battle!.Complete)
        {
            var encounter=m.Current.Frame.Battle.EncounterId;int round=m.Current.Frame.Battle.Round;
            foreach(var actor in new[]{A,B})
            {
                Assert.True(m.SwitchActor(actor));if(flow.Stage==ReviewCombatStage.RoundResult)Assert.True(flow.PlanNextRound());
                Assert.True(flow.CommitRound("command.walkthrough.round."+actor+"."+encounter+"."+round,true));
                if(actor==A){Assert.True(m.Save("waiting",true));Assert.True(m.Load("waiting",true));Assert.Equal(ReviewCombatStage.Waiting,flow.Stage);}
            }
        }
        Assert.True(flow.FinalizeAftermath(true));Assert.False(flow.FinalizeAftermath(true));Assert.True(m.SwitchActor(A));
        Assert.True(workspace.Navigate(ReviewWorkspacePage.Map));Assert.Equal(SessionPhase.Planning,m.Current.Frame.Status.Phase);
        var own=m.Current.Frame.OwnShips.Single(s=>s.Condition.ShipId==produced.ShipId.Value);Assert.Equal(3,own.Weapons.Single(w=>w.Class==TacticalWeaponClass.Torpedo).Ammunition);
        var hull=own.Condition.Hull;var shields=own.Condition.Shields.ToArray();Assert.True(m.Save("after-battle"));Assert.True(m.Load("after-battle",true));
        own=m.Current.Frame.OwnShips.Single(s=>s.Condition.ShipId==produced.ShipId.Value);Assert.Equal(hull,own.Condition.Hull);Assert.Equal(shields,own.Condition.Shields);
        while(!m.Current.Frame.Strategy!.Research.Engineering.Single().CanTarget)Advance(m);
        var project=m.Current.Frame.Strategy!.Research.Engineering.Single();Assert.True(m.Engineering(project.Definition.DefinitionId.Value,Id(m)));Assert.True(m.AllocateResearch(0,Id(m)));
        while(m.Current.Frame.Strategy!.Research.Engineering.Single().Band!=EngineeringMaturityBand.Mature)Advance(m);
        Assert.Equal(131,m.Current.Frame.Status.Turn);Advance(m);Assert.True(m.Save("final"));
        var saved=f.Store.Read("final");Assert.Equal(3,saved.FormatVersion);Assert.Single(saved.BuiltShips!);Assert.Equal(produced.ShipId.Value,saved.BuiltShips![0].ShipId);
        var vector=Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(saved))).ToLowerInvariant();Assert.Equal("417d1852e052b4fe1f4f2b5f611ba49b82b7d76cd6154540a58bcaefc474e865",vector);
    }
    [Theory][InlineData("hash")][InlineData("format")][InlineData("provenance")]
    public void InvalidIntegratedCandidateRetainsControllerAndUnsentResearch(string corruption)
    {
        using var f=new IntegratedUiFixture();var m=f.Model;var count=m.Current.Frame.Strategy!.Ships.Count;Assert.True(m.Enqueue("colony.sol","ship-design.source-cruiser",Id(m)));while(m.Current.Frame.Strategy!.Ships.Count==count)Advance(m);
        Assert.True(m.Save("valid"));var bad=f.Store.Read("valid");
        if(corruption=="hash")bad.PackageHash="wrong";else if(corruption=="format")bad.FormatVersion=2;else bad.BuiltShips!.Clear();
        f.Store.Write("bad",bad,false);var controller=m.Current;var frame=controller.Frame;var draft=m.ResearchInputs()!;draft.ScienceUnits=300;
        Assert.False(m.Load("bad",true));Assert.Same(controller,m.Current);Assert.Same(frame,m.Current.Frame);Assert.Same(draft,m.ResearchDraft);Assert.True(m.HasLocalEdits);
    }
    [Fact]
    public void ResearchDraftNavigationSavePartialApplyAndExplicitDiscardAreSafe()
    {
        using var f=new IntegratedUiFixture();var m=f.Model;var workspace=new ReviewWorkspace(new ReviewCombatFlow(m));var frame=m.Current.Frame;
        var draft=m.ResearchInputs()!;draft.ScienceUnits=300;draft.Adjust(ScienceDiscipline.Construction,30);
        foreach(var page in Enum.GetValues<ReviewWorkspacePage>())Assert.True(workspace.Navigate(page));
        Assert.Same(frame,m.Current.Frame);Assert.False(m.Advance());Assert.False(m.SwitchActor(B));Assert.False(m.DraftTravel(FA,CampaignTravelIntent.StayEngage,null,null));
        Assert.True(m.Save("research-draft"));Assert.True(m.HasLocalEdits);Assert.False(m.Load("research-draft"));
        var values=draft.Values();Assert.True(m.AllocateResearch(draft.ScienceUnits,Id(m)));Assert.True(m.HasLocalEdits);Assert.Equal(values.Select(v=>v.Weight),m.ResearchDraft!.Values().Select(v=>v.Weight));
        Assert.True(m.AllocateScience(m.ResearchDraft.Values(),Id(m)));Assert.False(m.HasLocalEdits);Assert.Equal(300,m.Current.Frame.Strategy!.Research.ScienceAllocationUnits);
        Assert.True(m.Load("research-draft",true));Assert.Equal(420,m.Current.Frame.Strategy!.Research.ScienceAllocationUnits);Assert.Null(m.ResearchDraft);
    }
    [Theory][InlineData("stale")][InlineData("foreign")][InlineData("duplicate")][InlineData("unsupported")][InlineData("invalid-shares")]
    public void RejectedRealStrategicCommandsPreserveFrameStocksAndDraft(string failure)
    {
        using var f=new IntegratedUiFixture();var m=f.Model;var c=m.StrategicEnvelope(CommandKind.SetResearchAllocation,"command.integrated.error");c.SetResearchAllocation=new(){ScienceAllocationUnits=300};
        if(failure=="stale")c.ExpectedRevision--;
        if(failure=="foreign")c.ActorCivilizationId="civ.klingon";
        if(failure=="duplicate"){Assert.True(m.Strategic(c));c.ExpectedRevision=m.Current.Frame.Status.CampaignRevision;}
        if(failure=="unsupported"){c=m.StrategicEnvelope(CommandKind.EnqueueProduction,"command.unsupported");c.EnqueueProduction=new(){ColonyId="colony.sol",DefinitionId="ship-design.advanced-scout"};}
        if(failure=="invalid-shares"){c=m.StrategicEnvelope(CommandKind.SetScienceAllocation,"command.invalid-shares");c.SetScienceAllocation=new(){Allocations=new()};}
        var frame=m.Current.Frame;var draft=m.ResearchInputs()!;draft.Adjust(ScienceDiscipline.Construction,30);var credits=frame.Strategy!.Civilization.Credits;
        Assert.False(m.Strategic(c));Assert.Same(frame,m.Current.Frame);Assert.Same(draft,m.ResearchDraft);Assert.True(m.Current.RefreshRequired);Assert.Equal(credits,m.Current.Frame.Strategy!.Civilization.Credits);
    }
    [Fact]
    public void ScopedStrategyAndCodecNeverSubstitutePhysicalOrLegacyRoot()
    {
        using var f=new IntegratedUiFixture();var m=f.Model;Assert.All(m.Current.Frame.Strategy!.Ships,s=>Assert.Equal(A,s.OwnerCivilizationId.Value));
        Assert.All(m.Current.Frame.Strategy.Colonies,c=>Assert.Equal(A,c.OwnerCivilizationId!.Value.Value));Assert.DoesNotContain(m.Current.Frame.Strategy.Fleets,s=>s.FleetId.Value==FB);
        Assert.True(m.Save("shape"));var full=f.Store.Read("shape");Assert.Throws<InvalidDataException>(()=>f.Codec.DecodeRoot(f.Codec.EncodeIntegratedRoot(full)));
        Assert.Throws<InvalidDataException>(()=>f.Codec.DecodeIntegratedRoot(f.Codec.EncodeRoot(full.Physical!)));
        var bytes=f.Codec.EncodeIntegratedRoot(full);var text=Encoding.UTF8.GetString(bytes);Assert.Throws<InvalidDataException>(()=>f.Codec.DecodeIntegratedRoot(Encoding.UTF8.GetBytes(text.Replace("\"FormatVersion\":3,",""))));
        Assert.Throws<InvalidDataException>(()=>f.Codec.DecodeIntegratedRoot(Encoding.UTF8.GetBytes(text.Insert(1,"\"FormatVersion\":3,"))));
    }
    [Theory][InlineData(UnityReviewWriteStage.BeforeWrite)][InlineData(UnityReviewWriteStage.AfterPartialWrite)][InlineData(UnityReviewWriteStage.BeforeReplace)]
    public void IntegratedAtomicWriteFailuresKeepPriorSlotAndResearchEdits(UnityReviewWriteStage failAt)
    {
        bool fail=false;using var f=new IntegratedUiFixture(stage=>{if(fail&&stage==failAt)throw new IOException("private failure");});var m=f.Model;
        Assert.True(m.Save("atomic"));var before=File.ReadAllBytes(Path.Combine(f.Folder,"unity-integrated-playtest-atomic.json"));m.ResearchInputs()!.ScienceUnits=300;fail=true;
        Assert.False(m.Save("atomic",true));Assert.Equal(before,File.ReadAllBytes(Path.Combine(f.Folder,"unity-integrated-playtest-atomic.json")));Assert.True(m.HasLocalEdits);
        Assert.DoesNotContain("private failure",m.Message);Assert.DoesNotContain(f.Folder,m.Message);
    }
}
