using System.Security.Cryptography;
using HobbyDS9.Contracts;
using HobbyDS9.TacticalReviewClient;
using Xunit;

namespace HobbyDS9.TacticalReviewShell.Tests;

public sealed class ReviewShellTests
{
    [Theory]
    [InlineData("faster","a6a2522a4f389b0bb8aa562d6273b0f02b3ea1fdeb57c7fe21d8368e4fa4d177")]
    [InlineData("slower","77073cc154b78dbca2e5f6fa1a618cae490d11410f51f4cb2bb9e640a33210c4")]
    public async Task RunnableScriptedDemoUsesTheRealShellAndPreservesPhysicalRootVectors(string profile,string expected)
    {
        using var f=new ShellFixture(profile); var cli=await f.RunCli(true);
        Assert.Equal(0,cli.Exit); Assert.Empty(cli.Error);
        Assert.Equal(expected,Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(f.PathFor("demo-final")))).ToLowerInvariant());
        Assert.Contains("SCRIPTED HUMAN CHOICE",cli.Output); Assert.Contains("ACTOR VIEW: civ.federation",cli.Output);
        Assert.Contains("ACTOR VIEW: civ.cardassian",cli.Output); Assert.Contains("production activation",cli.Output,StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Own availability OutsideEncounter",cli.Output);
        Assert.DoesNotContain("MovementProof",cli.Output); Assert.DoesNotContain("RandomState",cli.Output);
        if(profile == "faster")
        {
            Assert.Contains("no shot promised",cli.Output); Assert.Contains("EncounterComplete",cli.Output);
            Assert.True(File.Exists(f.PathFor("demo-waiting")));
        }
    }
    [Fact]
    public void HotseatSwitchRequiresDiscardAndClearsInspectionOnlyAfterValidActorConstruction()
    {
        using var f=new ShellFixture(); var shell=f.Shell;
        Assert.True(shell.Execute("inspect ship.equipment-cruiser").Succeeded);
        Assert.True(shell.Execute("travel fleet.federation-scout Follow nav.bajor fleet.cardassian-scout").Succeeded);
        var prior=shell.Current; Assert.True(prior.Dirty); Assert.NotNull(prior.SelectedShip);
        var denied=shell.Execute("actor civ.cardassian");
        Assert.False(denied.Succeeded); Assert.Same(prior,shell.Current);
        Assert.StartsWith("ACTOR VIEW: civ.federation",denied.Text);
        Assert.DoesNotContain("Own travel fleet.cardassian-scout",denied.Text);
        Assert.False(shell.Execute("actor civ.unknown discard").Succeeded); Assert.Same(prior,shell.Current); Assert.True(prior.Dirty);
        var changed=shell.Execute("actor civ.cardassian discard");
        Assert.True(changed.Succeeded); Assert.False(shell.Current.Dirty); Assert.Null(shell.Current.SelectedShip);
        Assert.StartsWith("ACTOR VIEW: civ.cardassian",changed.Text);
        Assert.Contains("LOCAL TRUSTED HOTSEAT",changed.Text);
    }
    [Fact]
    public void SaveKeepsDraftAndLoadOpenRequireConsentWhileInvalidFilesRetainCurrentView()
    {
        using var f=new ShellFixture(); var shell=f.Shell;
        shell.Execute("travel fleet.federation-scout Follow nav.bajor fleet.cardassian-scout");
        Assert.True(shell.Execute("save local").Succeeded); Assert.True(shell.Current.Dirty);
        Assert.False(shell.Execute("load local").Succeeded); Assert.False(shell.Execute("open local").Succeeded);
        var current=shell.Current;
        File.WriteAllText(f.PathFor("bad"),"{ PRIVATE enemy orders ");
        var result=shell.Execute("open bad discard");
        Assert.False(result.Succeeded); Assert.Same(current,shell.Current); Assert.True(shell.Current.Dirty);
        Assert.DoesNotContain("PRIVATE",result.Text); Assert.DoesNotContain(f.Root,result.Text);
        Assert.True(shell.Execute("open local discard").Succeeded); Assert.False(shell.Current.Dirty);
        Assert.Equal(CampaignTravelIntent.Stay,shell.Current.Frame.Travel!.OwnOrders.Single().Intent);
    }
    [Fact]
    public void AdvanceRejectsLocalDraftAndActualDuplicateCommandErrorsAreDisplayedWithoutRetry()
    {
        using var f=new ShellFixture(); var shell=f.Shell;
        shell.Execute("travel fleet.federation-scout Follow nav.bajor fleet.cardassian-scout");
        Assert.False(shell.Execute("advance").Succeeded); Assert.True(shell.Current.Dirty);
        Assert.True(shell.Execute("send-travel command.one").Succeeded);
        shell.Execute("travel fleet.federation-scout Follow nav.bajor fleet.cardassian-scout");
        var duplicate=shell.Execute("send-travel command.one");
        Assert.False(duplicate.Succeeded); Assert.Contains("Authoritative result: DuplicateCommand",duplicate.Text);
        Assert.True(shell.Current.Dirty); Assert.True(shell.Current.RefreshRequired);
        Assert.False(shell.Execute("send-travel command.retry").Succeeded);
    }
    [Theory]
    [InlineData("unknown")]
    [InlineData("report 4")]
    [InlineData("fleet fleet.federation-scout 99 -")]
    [InlineData("travel fleet.federation-scout 99")]
    [InlineData("save ../outside")]
    public void InvalidCommandsAndPathsReturnSafeFixedMessagesWithoutStateMutation(string command)
    {
        using var f=new ShellFixture(); var frame=f.Shell.Current.Frame;
        var result=f.Shell.Execute(command); Assert.False(result.Succeeded);
        Assert.Same(frame,f.Shell.Current.Frame); Assert.DoesNotContain(f.Root,result.Text);
        Assert.False(Directory.Exists(f.Root)); Assert.False(f.Shell.Current.Dirty);
    }
    [Fact]
    public async Task ExplicitStdinEntryPointDisplaysHelpFactsAndExitsWithoutDesktopControl()
    {
        using var f=new ShellFixture();
        var cli=await f.RunCli(false,"show\ninspect ship.equipment-cruiser\nexit\n");
        Assert.Equal(0,cli.Exit); Assert.Empty(cli.Error); Assert.Contains("Review shell closed",cli.Output);
        Assert.Contains("Authoritative Unarmed=False",cli.Output); Assert.Contains("Outside encounter",cli.Output);
        Assert.DoesNotContain("RandomState",cli.Output);
    }
    [Fact]
    public void RouteAndInspectionQueriesDoNotIssueCommandsOrForecastShots()
    {
        using var f=new ShellFixture(); var revision=f.Shell.Current.Frame.Status.CampaignRevision;
        for(int i=0;i<5;i++)
        {
            Assert.True(f.Shell.Execute("route fleet.federation-scout nav.bajor").Succeeded);
            Assert.True(f.Shell.Execute("inspect ship.equipment-cruiser").Succeeded);
            f.Shell.Execute("show"); f.Shell.Execute("report");
        }
        Assert.Equal(revision,f.Shell.Current.Frame.Status.CampaignRevision);
        Assert.False(f.Shell.Current.Dirty); Assert.False(f.Shell.Current.HasPendingWork);
    }
    [Fact]
    public void SelectedHotseatViewHidesCloakedEnemyInspectionReservesAndFreshOrders()
    {
        using var f=new ShellFixture(); var shell=f.Shell;
        foreach(var command in new[]{
            "cloak fleet.federation-scout true command.cloak",
            "travel fleet.federation-scout Follow nav.bajor fleet.cardassian-scout",
            "send-travel command.travel.a","actor civ.cardassian",
            "travel fleet.cardassian-scout Leave nav.bajoran-wormhole",
            "send-travel command.travel.b","actor civ.federation","advance",
            "fleet fleet.federation-scout Hold -","round command.round.a","actor civ.cardassian"})
            Assert.True(shell.Execute(command).Succeeded,command);
        var reply=shell.Execute("show");
        Assert.StartsWith("ACTOR VIEW: civ.cardassian",reply.Text);
        Assert.DoesNotContain("ship.equipment-cruiser",reply.Text);
        Assert.DoesNotContain("Own accepted fleet.federation-scout",reply.Text);
        Assert.DoesNotContain("Own travel fleet.federation-scout",reply.Text);
        Assert.False(shell.Execute("inspect ship.equipment-cruiser").Succeeded);
        Assert.False(shell.Execute("fleet fleet.federation-scout Hold -").Succeeded);
        Assert.Contains("no shot promised",reply.Text);
    }
}
