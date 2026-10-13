using System.Diagnostics;
using HobbyDS9.Contracts;
using HobbyDS9.TacticalReviewClient;
using HobbyDS9.TacticalReviewStorage;
using Xunit;

namespace HobbyDS9.TacticalReviewStorage.Tests;

internal sealed class DiskReviewFixture : IDisposable
{
    internal const string ActorA="civ.federation", ActorB="civ.cardassian";
    internal const string FleetA="fleet.federation-scout", FleetB="fleet.cardassian-scout";
    public string Root { get; }=Path.Combine(Path.GetTempPath(),"hobby-review-checkpoint-test-"+Guid.NewGuid().ToString("N"));
    public string ReviewDirectory => Path.Combine(Root,"review-only");
    public string ProfilePath => Path.Combine(AppContext.BaseDirectory,"scenarios","tactical-v2-campaign-review","package.json");
    public string ScenarioPath => Path.Combine(AppContext.BaseDirectory,"scenarios","tactical-v2-campaign-review","scenario.json");
    public CampaignTacticalReviewPackageDto Package { get; }
    public JsonReviewHostCodec Codec { get; }=new JsonReviewHostCodec();
    public DiskReviewCheckpointStore Store { get; }
    public ReviewHost Host { get; }
    public ReviewController A { get; }
    public ReviewController B { get; }
    public DiskReviewFixture(Action<ReviewWriteStage>? probe=null, IReviewFileCodec? codec=null)
    {
        Package=ReviewPackageFiles.Load(ProfilePath,ScenarioPath);
        Store=new DiskReviewCheckpointStore(ReviewDirectory,codec ?? Codec,probe);
        Host=ReviewHost.Start(Package,codec ?? Codec,Store);
        A=new ReviewController(Host,ActorA); B=new ReviewController(Host,ActorB);
    }
    public string PathFor(string slot) => Path.Combine(ReviewDirectory,"review-checkpoint-"+slot+".json");
    public string Allocate()
    {
        Assert.True(Host.AdvancePlanning(A.Frame).Accepted); Assert.True(A.Refresh()); Assert.True(B.Refresh());
        var id=Assert.Single(A.Frame.Encounters); Assert.True(A.SelectEncounter(id)); Assert.True(B.SelectEncounter(id));
        return id;
    }
    public static ReviewWork Begin(ReviewController c)
    {
        Assert.True(c.ReviewRound());
        return Assert.IsType<ReviewWork>(c.ConfirmRound("command.client."+c.Frame.Actor+"."+c.Frame.Battle!.EncounterId+"."+c.Frame.Battle.Round));
    }
    public static ReviewReceipt Send(ReviewHost host,ReviewController c)
    {
        var r=host.Execute(Begin(c)); Assert.True(r.Result.Accepted); Assert.True(c.Receive(r)); return r;
    }
    public void Round(CampaignFleetManeuver? maneuver=null)
    {
        Assert.True(A.Refresh()); Assert.True(B.Refresh());
        if(maneuver.HasValue)
        foreach(var c in new[]{A,B})
        foreach(var o in c.Frame.Battle!.StandingOrders.Where(o=>o.CanOrder))
            Assert.True(c.EditFleet(o.FleetId,maneuver.Value,o.FocusFleetId));
        Send(Host,A); Send(Host,B); Assert.True(A.Refresh()); Assert.True(B.Refresh());
    }
    public string CompleteAndFinalize()
    {
        while(!A.Frame.Battle!.Complete) Round(A.Frame.Battle.Round == 2 ? CampaignFleetManeuver.Flank : null);
        var report=ReviewTextRenderer.RenderReport(A); Assert.True(A.ReviewCompletion());
        var r=Host.Execute(A.ConfirmCompletion()!); Assert.True(r.Result.Accepted); Assert.True(A.Receive(r));
        return report;
    }
    public async Task<(int Exit,string Output,string Error)> Child(string mode,string slot,string output="child-result",string? actor=null)
    {
        var start=new ProcessStartInfo {
            FileName=Environment.GetEnvironmentVariable("DOTNET_HOST_PATH") ?? "dotnet",
            UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true
        };
        start.ArgumentList.Add(Path.Combine(AppContext.BaseDirectory,"probe","HobbyDS9.TacticalReviewStorage.Probe.dll"));
        foreach(var arg in new[]{mode,ReviewDirectory,slot,actor ?? ActorA,ActorB,ProfilePath,ScenarioPath,output}) start.ArgumentList.Add(arg);
        using(var process=Process.Start(start)!)
        {
            var stdout=process.StandardOutput.ReadToEndAsync(); var stderr=process.StandardError.ReadToEndAsync();
            using var timeout=new CancellationTokenSource(TimeSpan.FromSeconds(45));
            try { await process.WaitForExitAsync(timeout.Token); }
            catch(OperationCanceledException) { process.Kill(true); throw; }
            return (process.ExitCode,await stdout,await stderr);
        }
    }
    public void Dispose()
    {
        var expected=Path.GetFullPath(Root); var basePath=Path.TrimEndingDirectorySeparator(Path.GetFullPath(Path.GetTempPath()));
        if(!expected.StartsWith(basePath+Path.DirectorySeparatorChar,StringComparison.Ordinal)
            || !Path.GetFileName(expected).StartsWith("hobby-review-checkpoint-test-",StringComparison.Ordinal))
            throw new InvalidOperationException("Refusing cleanup outside the task test directory.");
        if(Directory.Exists(expected)) Directory.Delete(expected,true);
    }
}
