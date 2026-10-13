using System.Diagnostics;
using HobbyDS9.TacticalReviewShell;
using HobbyDS9.TacticalReviewStorage;

namespace HobbyDS9.TacticalReviewShell.Tests;

internal sealed class ShellFixture : IDisposable
{
    public string Root { get; }=Path.Combine(Path.GetTempPath(),"hobby-review-shell-test-"+Guid.NewGuid().ToString("N"));
    public string DirectoryPath=>Path.Combine(Root,"review-only");
    public string Folder { get; }
    public string ProfilePath=>Path.Combine(AppContext.BaseDirectory,"scenarios","tactical-v2-physical-review",Folder);
    public TrustedReviewShell Shell { get; }
    public ShellFixture(string profile="faster")
    {
        Folder=profile;
        var p=PhysicalReviewPackageFiles.Load(Path.Combine(ProfilePath,"package.json"),Path.Combine(ProfilePath,"core.json"),Path.Combine(ProfilePath,"scenario.json"));
        var codec=new JsonPhysicalReviewCodec();
        Shell=new TrustedReviewShell(p,codec,new DiskPhysicalReviewCheckpointStore(DirectoryPath,codec),"civ.federation");
    }
    public string PathFor(string slot)=>Path.Combine(DirectoryPath,"physical-review-checkpoint-"+slot+".json");
    public async Task<(int Exit,string Output,string Error)> RunCli(bool demo,string input="")
    {
        var start=new ProcessStartInfo { FileName=Environment.GetEnvironmentVariable("DOTNET_HOST_PATH")??"dotnet",
            UseShellExecute=false,CreateNoWindow=true,RedirectStandardInput=true,RedirectStandardOutput=true,RedirectStandardError=true };
        start.ArgumentList.Add(Path.Combine(AppContext.BaseDirectory,"shell","HobbyDS9.TacticalReviewShell.dll"));
        foreach(var arg in new[]{ProfilePath,DirectoryPath,"civ.federation"}) start.ArgumentList.Add(arg);
        if(demo) start.ArgumentList.Add("--demo");
        using var p=Process.Start(start)!; var stdout=p.StandardOutput.ReadToEndAsync(); var stderr=p.StandardError.ReadToEndAsync();
        await p.StandardInput.WriteAsync(input); p.StandardInput.Close();
        using var timeout=new CancellationTokenSource(TimeSpan.FromSeconds(45));
        try { await p.WaitForExitAsync(timeout.Token); } catch(OperationCanceledException) { p.Kill(true); throw; }
        return (p.ExitCode,await stdout,await stderr);
    }
    public void Dispose()
    {
        var resolved=Path.GetFullPath(Root); var basis=Path.TrimEndingDirectorySeparator(Path.GetFullPath(Path.GetTempPath()));
        if(!resolved.StartsWith(basis+Path.DirectorySeparatorChar,StringComparison.Ordinal)
            || !Path.GetFileName(resolved).StartsWith("hobby-review-shell-test-",StringComparison.Ordinal))
            throw new InvalidOperationException("Refusing cleanup outside task-owned directory.");
        if(Directory.Exists(resolved)) Directory.Delete(resolved,true);
    }
}
