using System.Text.Json;
using System.Security.Cryptography;
using HobbyDS9.Application;
using HobbyDS9.Contracts;
using HobbyDS9.UnityClient;
using Xunit;

namespace HobbyDS9.Simulation.Tests;

public sealed class ShipDesignDraftTests
{
    private static ShipDesignDraft Start() => new(new FixtureShipDesignPreviewProvider());
    [Fact]
    public void BothTemplatesShareOneHullAndProfileInspectionIsNotADesignEdit()
    {
        var draft=Start(); Assert.Single(draft.Provider.Hulls);
        Assert.Single(draft.Provider.Templates.Select(t=>t.Fit.HullId).Distinct());
        foreach(var mode in Enum.GetValues<DesignerProfile>()) { draft.SelectProfile(mode); Assert.False(draft.Modified); }
        Assert.Equal(DesignerStatus.Valid,draft.Preview(true).Status);
        draft.RequestTemplate("demo.escort"); Assert.False(draft.Modified); Assert.Equal("Escort",draft.Current.Role);
    }
    [Fact]
    public void EditingQuantitiesReplacingAndRemovingNeverAliasesStartingOrEarlierSnapshots()
    {
        var draft=Start(); var original=draft.Current;
        draft.Add("demo.labs"); Assert.Equal(2,draft.Current.Installed["demo.labs"]);
        Assert.Equal(1,original.Installed["demo.labs"]); Assert.Equal(1,draft.Starting.Installed["demo.labs"]);
        draft.Replace("demo.labs","demo.bank"); Assert.Equal(2,draft.Current.Installed["demo.bank"]); Assert.False(draft.Current.Installed.ContainsKey("demo.labs"));
        draft.Add("demo.shield"); draft.Replace("demo.bank","demo.shield"); Assert.Equal(4,draft.Current.Installed["demo.shield"]);
        draft.Remove("demo.shield"); Assert.False(draft.Current.Installed.ContainsKey("demo.shield"));
        Assert.Throws<NotSupportedException>(()=>((IDictionary<string,int>)original.Installed).Add("demo.other",1));
        Assert.Equal(1,draft.Starting.Installed["demo.shield"]);
    }
    [Fact]
    public void RevertingActualEditsClearsModifiedFlagAndResetRequiresConfirmation()
    {
        var draft=Start(); draft.Rename("Custom class"); draft.SetQuantity("demo.labs",2);
        draft.RequestReset(); Assert.True(draft.ConfirmationPending); Assert.Equal("Custom class",draft.Current.Name);
        draft.CancelDiscard(); Assert.True(draft.Modified); Assert.False(draft.ConfirmationPending);
        draft.RequestReset(); draft.ConfirmDiscard(); Assert.False(draft.Modified); Assert.Equal("Pathfinder",draft.Current.Name);
        draft.Add("demo.bank"); draft.Remove("demo.bank"); Assert.False(draft.Modified);
    }
    [Fact]
    public void ModifiedTemplateChangeWaitsForConsentAndCancelledChangeRetainsDraft()
    {
        var draft=Start(); draft.Rename("Local surveyor"); draft.RequestTemplate("demo.escort");
        Assert.Equal("demo.surveyor",draft.TemplateId); Assert.True(draft.ConfirmationPending);
        draft.CancelDiscard(); Assert.Equal("Local surveyor",draft.Current.Name);
        draft.RequestTemplate("demo.escort"); draft.ConfirmDiscard();
        Assert.Equal("demo.escort",draft.TemplateId); Assert.Equal("Sentinel",draft.Current.Name); Assert.False(draft.Modified);
    }
    [Fact]
    public void DepartureExecutesExactlyOnceAfterExplicitDiscardOrImmediatelyWhenClean()
    {
        var draft=Start(); var exits=0; draft.Rename("Edited");
        draft.RequestDiscard(()=>exits++); Assert.Equal(0,exits); draft.CancelDiscard(); Assert.Equal("Edited",draft.Current.Name);
        draft.RequestDiscard(()=>exits++); draft.ConfirmDiscard(); draft.ConfirmDiscard(); Assert.Equal(1,exits); Assert.False(draft.Modified);
        draft.RequestDiscard(()=>exits++); Assert.Equal(2,exits);
    }
    [Theory]
    [InlineData("demo.reactor")]
    [InlineData("demo.drive")]
    [InlineData("demo.crew")]
    [InlineData("demo.command")]
    public void AuthoredMissingSystemCaseTargetsAffectedComponent(string id)
    {
        var draft=Start(); draft.Remove(id); var preview=draft.Preview();
        Assert.Equal(DesignerStatus.HardError,preview.Status); Assert.Equal(id,Assert.Single(preview.Issues).SystemId);
        Assert.Null(preview.Power.FirstOrDefault()); draft.Add(id); Assert.Equal(DesignerStatus.Valid,draft.Preview().Status);
    }
    [Fact]
    public void CapacityAndMountDemoErrorsAreAuthoredCasesAndArbitraryFitsAreNotEvaluated()
    {
        var draft=Start(); draft.SetQuantity("demo.labs",2); Assert.Equal(DesignerStatus.HardError,draft.Preview().Status);
        Assert.Equal(-4,draft.Preview().FreeCapacity);
        draft.SetQuantity("demo.labs",3); Assert.Equal(DesignerStatus.NotEvaluated,draft.Preview().Status); Assert.Null(draft.Preview().UsedCapacity);
        Assert.Empty(draft.Preview().Costs); draft.RequestTemplate("demo.escort"); draft.ConfirmDiscard(); draft.SetQuantity("demo.bank",3);
        Assert.Equal("demo.bank",Assert.Single(draft.Preview().Issues).SystemId); Assert.Equal(DesignerStatus.HardError,draft.Preview().Status);
    }
    [Fact]
    public void UnarmedSurveyorIsValidAndEscortCombatShowsReducedOutputAsWarning()
    {
        var draft=Start(); Assert.Equal(DesignerStatus.Valid,draft.Preview().Status); Assert.False(draft.Current.Installed.ContainsKey("demo.bank"));
        draft.SelectProfile(DesignerProfile.Combat); Assert.Equal(DesignerStatus.Warning,draft.Preview().Status);
        draft.RequestTemplate("demo.escort"); draft.SelectProfile(DesignerProfile.Combat);
        var preview=draft.Preview(); Assert.Equal(DesignerStatus.Warning,preview.Status);
        Assert.Contains(preview.Power,m=>m.Value=="60 / 66"); Assert.Contains(preview.Power,m=>m.Caption=="Actual allocation"&&m.Value=="60");
        Assert.Contains(preview.Capabilities,m=>m.Value.Contains("75% requested output"));
    }
    [Fact]
    public void LookupIsOrderIndependentAndComparisonUsesSameProfileWithoutMutatingBaseline()
    {
        var draft=Start(); var provider=draft.Provider;
        var reordered=new DesignerFit("Renamed",draft.Current.HullId,draft.Current.Role,draft.Current.Installed.Reverse());
        Assert.Same(draft.Preview(),provider.Preview(reordered,DesignerProfile.Cruise));
        draft.Add("demo.labs"); draft.SelectProfile(DesignerProfile.Survey);
        Assert.Equal(DesignerStatus.Valid,draft.Preview(true).Status); Assert.Equal(DesignerStatus.HardError,draft.Preview().Status);
        Assert.Equal(1,draft.Starting.Installed["demo.labs"]);
    }
    [Fact]
    public void InvalidLocalEditsAreRejectedAtomicallyWithoutPretendingToValidateGameplay()
    {
        var draft=Start(); var before=draft.Current;
        Assert.Throws<ArgumentException>(()=>draft.Replace("demo.labs","unknown"));
        Assert.Throws<ArgumentOutOfRangeException>(()=>draft.SetQuantity("demo.labs",-1));
        Assert.Throws<ArgumentException>(()=>draft.SetHull("unknown")); Assert.True(before.SameAs(draft.Current));
        draft.SetRole("Custom"); Assert.Equal(DesignerStatus.NotEvaluated,draft.Preview().Status);
    }
    [Fact]
    public void AllDraftInteractionsLeaveActualCampaignSaveBytesUntouched()
    {
        var bytes=File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,"scenarios","ds9-slice","scenario.json"));
        var scenario=JsonSerializer.Deserialize<ScenarioDto>(bytes)!; scenario.ContentHash=Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
        var campaign=CampaignApplication.StartScenario(scenario); var before=JsonSerializer.Serialize(campaign.ExportSave());
        var draft=Start(); draft.Rename("Preview only"); draft.Add("demo.bank"); draft.Replace("demo.bank","demo.launcher");
        draft.SelectProfile(DesignerProfile.Combat); _=draft.Preview(); _=draft.Preview(true);
        draft.RequestReset(); draft.CancelDiscard(); draft.RequestTemplate("demo.escort"); draft.ConfirmDiscard();
        Assert.Equal(before,JsonSerializer.Serialize(campaign.ExportSave()));
    }
}
