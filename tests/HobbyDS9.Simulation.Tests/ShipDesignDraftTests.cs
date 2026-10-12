using System.Text.Json;
using System.Security.Cryptography;
using HobbyDS9.Application;
using HobbyDS9.Contracts;
using HobbyDS9.Contracts.ShipDesign;
using HobbyDS9.UnityClient;
using Xunit;

namespace HobbyDS9.Simulation.Tests;

public sealed class ShipDesignDraftTests
{
    private static DesignerFixture Fixture() => JsonSerializer.Deserialize<DesignerFixture>(File.ReadAllText(Path.Combine(AppContext.BaseDirectory,"scenarios/ship-design-fixtures/fixture.json")))!;
    private static ShipDesignDraft Start() => new(new FixtureShipDesignPreviewProvider(Fixture()));
    private static ShipDesignPreview Evaluate(ShipDesignDraft draft) => ((FixtureShipDesignPreviewProvider)draft.Provider).Evaluate(draft.Current);
    [Theory]
    [InlineData("class.surveyor")]
    [InlineData("class.escort")]
    public void FixtureCreatesRealDesignerAndPreservesEveryAuthoredInstallationAndProfile(string template)
    {
        var fixture=Fixture(); var draft=Start(); draft.RequestTemplate(template);
        var expected=fixture.Drafts.Single(d=>d.ClassId==template);
        Assert.Equal(JsonSerializer.Serialize(expected),JsonSerializer.Serialize(draft.Current.ToDto()));
        Assert.True(Evaluate(draft).IsValid); Assert.False(draft.Provider.FixtureResults);
        foreach(var profile in Enum.GetValues<DesignerProfile>()) { draft.SelectProfile(profile); Assert.False(draft.Modified); }
    }
    [Fact]
    public void EscortCombatIsBackendPowerLimitedAndPresentationShowsActualOutputs()
    {
        var draft=Start(); draft.RequestTemplate("class.escort"); draft.SelectProfile(DesignerProfile.Combat);
        var preview=Evaluate(draft); var combat=preview.Profiles.Single(p=>p.Kind==OperatingProfileKind.Combat);
        Assert.Equal(66,combat.RequestedPower); Assert.Equal(60,combat.AvailablePower); Assert.Equal(60,combat.AllocatedPower);
        var bank=combat.Installations.Single(i=>i.InstallationId=="fit.energy-bank");
        Assert.Equal(16,bank.RequestedPower); Assert.Equal(12,bank.AllocatedPower);
        Assert.Contains(preview.Issues,i=>i.Code==DesignIssueCode.PartialPower);
        var before=combat.Output(ShipOutputKind.MediumRangeAttack);
        draft.SetPower("component.energy-bank.v1",16,16); draft.SetPower("component.launcher.v1",8,8); draft.SetPower("component.escort-shield.v1",14,8);
        Assert.True(Evaluate(draft).IsValid);
        Assert.True(Evaluate(draft).Profiles.Single(p=>p.Kind==OperatingProfileKind.Combat).Output(ShipOutputKind.MediumRangeAttack)>before);
        Assert.Contains(draft.Preview().Capabilities,m=>m.Caption=="MediumRangeAttack");
    }
    [Theory]
    [InlineData("capacity")]
    [InlineData("mount")]
    [InlineData("required")]
    [InlineData("dependency")]
    public void LocalEditsProduceAuthoritativeBackendIssues(string edit)
    {
        var draft=Start(); DesignIssueCode code;
        switch(edit)
        {
            case "capacity": draft.Add("component.labs.v1"); code=DesignIssueCode.CapacityExceeded; break;
            case "mount": draft.RequestTemplate("class.escort"); draft.Add("component.energy-bank.v1"); code=DesignIssueCode.MountExceeded; break;
            case "required": draft.Remove("component.life-support.v1"); code=DesignIssueCode.MissingRequiredSystem; break;
            default: draft.Remove("component.command.v1"); code=DesignIssueCode.DependencyUnsatisfied; break;
        }
        Assert.Contains(Evaluate(draft).Issues,i=>i.Code==code);
        Assert.Equal(DesignerStatus.HardError,draft.Preview().Status);
        Assert.All(Evaluate(draft).Issues,i=>Assert.Contains(draft.Preview().Issues,x=>x.Explanation.StartsWith(i.Message,StringComparison.Ordinal)));
    }
    [Fact]
    public void AccessIsDetachedAndUnavailableVersionsAreBackendRejections()
    {
        var fixture=Fixture(); fixture.Access.AvailableVersionIds!.Remove("component.labs.v1");
        var provider=new FixtureShipDesignPreviewProvider(fixture); var draft=new ShipDesignDraft(provider);
        Assert.Contains(provider.Evaluate(draft.Current).Issues,i=>i.Code==DesignIssueCode.VersionUnavailable);
        var access=provider.Access(); access.AvailableVersionIds!.Clear();
        fixture.Catalogue.Components!.Clear();
        Assert.NotEmpty(provider.Access().AvailableVersionIds!); Assert.NotEmpty(provider.Designer.Catalogue.Components);
    }
    [Fact]
    public void EditsKeepStableIdsAndAllOtherProfileAllocationsWithoutAliasingSnapshots()
    {
        var draft=Start(); var before=draft.Current; var dto=before.ToDto();
        draft.SelectProfile(DesignerProfile.Survey); draft.SetPower("component.labs.v1",24,18);
        draft.Add("component.labs.v1");
        var changed=draft.Current.ToDto();
        Assert.Equal("fit.labs",changed.Installations!.Single(i=>i.ComponentVersionId=="component.labs.v1").InstallationId);
        Assert.Equal(2,changed.Installations!.Single(i=>i.InstallationId=="fit.labs").Quantity);
        foreach(var kind in new[] {OperatingProfileKind.Cruise,OperatingProfileKind.Combat})
            Assert.Equal(JsonSerializer.Serialize(dto.Profiles!.Single(p=>p.Kind==kind)),JsonSerializer.Serialize(changed.Profiles!.Single(p=>p.Kind==kind)));
        Assert.Equal(24,before.Power("component.labs.v1",DesignerProfile.Survey).AllocatedPower);
        dto.Installations!.Clear(); Assert.NotEmpty(before.ToDto().Installations!);
        draft.Add("component.energy-bank.v1");
        var id=draft.Current.ToDto().Installations!.Single(i=>i.ComponentVersionId=="component.energy-bank.v1").InstallationId;
        Assert.All(draft.Current.ToDto().Profiles!,p=> { var a=p.Allocations!.Single(a=>a.InstallationId==id); Assert.Equal(0,a.RequestedPower); Assert.Equal(0,a.AllocatedPower); });
        draft.Remove("component.energy-bank.v1"); draft.Add("component.energy-bank.v1");
        Assert.Equal(id,draft.Current.ToDto().Installations!.Single(i=>i.ComponentVersionId=="component.energy-bank.v1").InstallationId);
    }
    [Theory]
    [InlineData(24,100)]
    [InlineData(-1,-1)]
    public void InvalidPowerIsRetainedWithoutClampingAndBackendExplainsIt(long requested,long allocated)
    {
        var draft=Start(); draft.SelectProfile(DesignerProfile.Survey); draft.SetPower("component.labs.v1",requested,allocated);
        Assert.Equal(allocated,draft.Current.Power("component.labs.v1",DesignerProfile.Survey).AllocatedPower);
        Assert.Contains(Evaluate(draft).Issues,i=>i.Code==DesignIssueCode.InvalidPowerAllocation);
    }
    [Fact]
    public void ReplacementPreservesInstallationIdentityButStartsUnauthoredPowerAtZero()
    {
        var draft=Start(); draft.Replace("component.labs.v1","component.energy-bank.v1");
        Assert.Equal("fit.labs",draft.Current.ToDto().Installations!.Single(i=>i.ComponentVersionId=="component.energy-bank.v1").InstallationId);
        foreach(var profile in Enum.GetValues<DesignerProfile>()) Assert.Equal(0,draft.Current.Power("component.energy-bank.v1",profile).AllocatedPower);
        draft.Add("component.labs.v1");
        var ids=draft.Current.ToDto().Installations!.Select(i=>i.InstallationId).ToList(); Assert.Equal(ids.Count,ids.Distinct().Count());
    }
    [Fact]
    public void ResetDiscardAndCancelledTemplateChangeRestoreCompleteStartingStateExactlyOnce()
    {
        var draft=Start(); var initial=JsonSerializer.Serialize(draft.Current.ToDto());
        draft.SelectProfile(DesignerProfile.Survey); draft.SetPower("component.labs.v1",24,18); draft.Rename("Edited");
        draft.RequestTemplate("class.escort"); Assert.True(draft.ConfirmationPending); draft.CancelDiscard();
        Assert.Equal("class.surveyor",draft.TemplateId); Assert.True(draft.Modified);
        draft.RequestReset(); draft.ConfirmDiscard(); Assert.False(draft.Modified); Assert.Equal(initial,JsonSerializer.Serialize(draft.Current.ToDto()));
        draft.Add("component.energy-bank.v1"); draft.Remove("component.energy-bank.v1"); Assert.False(draft.Modified);
        var exits=0; draft.Rename("Edited"); draft.RequestDiscard(()=>exits++); draft.ConfirmDiscard(); draft.ConfirmDiscard(); Assert.Equal(1,exits);
        draft.RequestTemplate("class.escort"); Assert.Equal("class.escort",draft.TemplateId);
    }
    [Fact]
    public void ActualBackendPreviewsAndEditsLeaveCampaignSaveBytesUntouched()
    {
        var bytes=File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,"scenarios/ds9-slice/scenario.json"));
        var scenario=JsonSerializer.Deserialize<ScenarioDto>(bytes)!; scenario.ContentHash=Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
        var campaign=CampaignApplication.StartScenario(scenario); var before=JsonSerializer.Serialize(campaign.ExportSave());
        var draft=Start(); _=Evaluate(draft); draft.Rename("Preview only"); draft.Add("component.energy-bank.v1");
        _=draft.Preview(); _=draft.Preview(true); draft.RequestTemplate("class.escort"); draft.ConfirmDiscard();
        draft.SelectProfile(DesignerProfile.Combat); draft.SetPower("component.energy-bank.v1",16,9); _=Evaluate(draft);
        Assert.Equal(before,JsonSerializer.Serialize(campaign.ExportSave()));
    }
}
