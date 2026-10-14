using HobbyDS9.TacticalReviewShell;
using HobbyDS9.TacticalReviewStorage;

try
{
    if(args.Length != 3 && (args.Length != 4 || args[3] != "--demo"))
    {
        Console.WriteLine("Usage: <exact-physical-profile-directory> <absolute-review-only-directory> <controlled-actor-id> [--demo]");
        return 2;
    }
    var source=Path.GetFullPath(args[0]);
    var package=PhysicalReviewPackageFiles.Load(Path.Combine(source,"package.json"),Path.Combine(source,"core.json"),Path.Combine(source,"scenario.json"));
    var codec=new JsonPhysicalReviewCodec(); var store=new DiskPhysicalReviewCheckpointStore(args[1],codec);
    var shell=new TrustedReviewShell(package,codec,store,args[2]);
    if(args.Length == 4) ScriptedReviewDemo.Run(shell,Console.Out);
    else shell.Run(Console.In,Console.Out);
    return 0;
}
catch(Exception)
{
    Console.WriteLine("Trusted review shell unavailable. Check explicit inputs; no private diagnostic details are displayed.");
    return 3;
}
