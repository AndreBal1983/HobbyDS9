using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;
using HobbyDS9.Contracts;
using HobbyDS9.TacticalReviewClient;
using HobbyDS9.TacticalReviewStorage;
using Xunit;

namespace HobbyDS9.TacticalTravelClient.Tests;

internal sealed class PhysicalFixture : IDisposable
{
    internal const string A="civ.federation",B="civ.cardassian",FA="fleet.federation-scout",FB="fleet.cardassian-scout";
    public string Root { get; }=Path.Combine(Path.GetTempPath(),"hobby-physical-client-test-"+Guid.NewGuid().ToString("N"));
    public string DirectoryPath => Path.Combine(Root,"review-only");
    public string Folder { get; }
    public string SourcePath(string name) => Path.Combine(AppContext.BaseDirectory,"scenarios","tactical-v2-physical-review",Folder,name);
    public CampaignTacticalTravelPackageDto Package { get; }
    public JsonPhysicalReviewCodec Codec { get; }=new JsonPhysicalReviewCodec();
    public DiskPhysicalReviewCheckpointStore Store { get; }
    public PhysicalReviewHost Host { get; }
    public ReviewController Own { get; private set; }
    public ReviewController Other { get; private set; }
    public PhysicalFixture(string profile="faster",Action<CampaignTacticalTravelPackageDto>? adjust=null,Action<ReviewWriteStage>? probe=null)
    {
        Folder=profile; Package=PhysicalReviewPackageFiles.Load(SourcePath("package.json"),SourcePath("core.json"),SourcePath("scenario.json"));
        adjust?.Invoke(Package);
        Store=new DiskPhysicalReviewCheckpointStore(DirectoryPath,Codec,probe);
        Host=PhysicalReviewHost.Start(Package,Codec,Store); Own=new ReviewController(Host,A); Other=new ReviewController(Host,B);
    }
    public string PathFor(string slot) => Path.Combine(DirectoryPath,"physical-review-checkpoint-"+slot+".json");
    public static T Clone<T>(T v) => JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(v))!;
    public static ReviewWork BeginRound(ReviewController c)
    {
        var b=c.Frame.Battle!;
        foreach(var o in b.StandingOrders.Where(o=>o.CanOrder))
            Assert.True(c.EditFleet(o.FleetId,b.Round == 2 ? CampaignFleetManeuver.Flank : b.Round == 3 ? CampaignFleetManeuver.Screen : CampaignFleetManeuver.Hold,o.FocusFleetId));
        Assert.True(c.ReviewRound());
        return Assert.IsType<ReviewWork>(c.ConfirmRound("command.physical.round."+b.EncounterId+"."+c.Frame.Actor+"."+b.Round));
    }
    public static ReviewReceipt Commit(PhysicalReviewHost host,ReviewController c)
    { var r=host.Execute(BeginRound(c)); Assert.True(r.Result.Accepted); Assert.True(c.Receive(r)); return r; }
    public static ReviewReceipt Travel(PhysicalReviewHost host,ReviewController c,CampaignTravelIntent intent,string? destination=null,string? contact=null,string? fleet=null)
    {
        Assert.True(c.Refresh()); var own=fleet ?? c.Frame.OwnStrategicFleets.Single().FleetId.Value;
        var offer=destination == null ? null : c.FindKnownRoute(own,destination);
        if(destination != null) Assert.True(offer!.Route.Success);
        Assert.True(c.EditTravel(own,intent,offer,contact)); Assert.True(c.ReviewTravel());
        var work=c.ConfirmTravel("command.physical."+c.Frame.Actor+"."+c.Frame.Status.Turn+(fleet == null ? "" : "."+fleet))!;
        var r=host.Execute(work); Assert.True(r.Result.Accepted); Assert.True(c.Receive(r)); return r;
    }
    public void StartPursuit(bool cloak=true,bool reverse=false)
    {
        if(cloak) { Assert.True(Host.SetApproachCloak(Own.Frame,"command.physical.cloak",FA,true).Accepted); Own.Refresh(); Other.Refresh(); }
        if(reverse) Travel(Host,Other,CampaignTravelIntent.Leave,"nav.bajoran-wormhole");
        Travel(Host,Own,CampaignTravelIntent.Follow,"nav.bajor",FB);
        if(!reverse) Travel(Host,Other,CampaignTravelIntent.Leave,"nav.bajoran-wormhole");
        Own.Refresh(); Advance();
    }
    public void Advance()
    {
        Assert.True(Host.AdvancePlanning(Own.Frame).Accepted); Assert.True(Own.Refresh()); Assert.True(Other.Refresh());
        if(Own.Frame.Encounters.Count > 0)
        { var id=Assert.Single(Own.Frame.Encounters); Assert.True(Own.SelectEncounter(id)); Assert.True(Other.SelectEncounter(id)); }
    }
    public string Complete(bool reverse=false,bool reload=false)
    {
        while(!Own.Frame.Battle!.Complete)
        {
            Commit(Host,reverse?Other:Own);
            if(reload)
            {
                var h=Own.SaveCommitted(); Own.LoadCommitted(h);
                Other=new ReviewController(Host,B); Other.SelectEncounter(Own.Frame.Battle!.EncounterId);
            }
            Commit(Host,reverse?Own:Other);
            Own.Refresh(); Other.Refresh();
        }
        var report=ReviewTextRenderer.RenderReport(Own);
        Assert.True(Own.ReviewCompletion()); var end=Host.Execute(Own.ConfirmCompletion()!);
        Assert.True(end.Result.Accepted); Assert.True(Own.Receive(end)); Other.Refresh();
        return report;
    }
    public void Reengage()
    {
        Travel(Host,Own,CampaignTravelIntent.StayEngage);
        Travel(Host,Other,CampaignTravelIntent.StayEngage); Own.Refresh(); Advance();
    }
    public string Vector(string slot)
    { Assert.True(Own.SaveCommittedToSlot(slot).Succeeded); return Hash(File.ReadAllBytes(PathFor(slot))); }
    private static string Hash(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
    public async Task<(int Exit,string Output,string Error)> Child(string mode,string slot,string output="child-final",string actor=A)
    {
        var start=new ProcessStartInfo { FileName=Environment.GetEnvironmentVariable("DOTNET_HOST_PATH")??"dotnet",
            UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true };
        start.ArgumentList.Add(Path.Combine(AppContext.BaseDirectory,"physical-probe","HobbyDS9.TacticalTravelClient.Probe.dll"));
        foreach(var arg in new[]{mode,DirectoryPath,slot,actor,B,SourcePath("package.json"),SourcePath("core.json"),SourcePath("scenario.json"),output}) start.ArgumentList.Add(arg);
        using var p=Process.Start(start)!; var stdout=p.StandardOutput.ReadToEndAsync(); var stderr=p.StandardError.ReadToEndAsync();
        using var timeout=new CancellationTokenSource(TimeSpan.FromSeconds(45));
        try { await p.WaitForExitAsync(timeout.Token); } catch(OperationCanceledException) { p.Kill(true); throw; }
        return (p.ExitCode,await stdout,await stderr);
    }
    public void Dispose()
    {
        var resolved=Path.GetFullPath(Root); var basis=Path.TrimEndingDirectorySeparator(Path.GetFullPath(Path.GetTempPath()));
        if(!resolved.StartsWith(basis+Path.DirectorySeparatorChar,StringComparison.Ordinal)
            || !Path.GetFileName(resolved).StartsWith("hobby-physical-client-test-",StringComparison.Ordinal))
            throw new InvalidOperationException("Refusing cleanup outside task-owned test directory.");
        if(Directory.Exists(resolved)) Directory.Delete(resolved,true);
    }
}
