using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using HobbyDS9.Contracts;
using HobbyDS9.Simulation;
using HobbyDS9.TacticalReviewClient;
using HobbyDS9.UnityReview;

// Isolated measurement client. Does not patch rules, owner saves, or production projects.
const string Human = "civ.federation";
const string Pin = "2fc0dbca46073121379dd9491c859a02a6f22dcd";
var source = Path.GetFullPath(args[0]);
var output = Path.GetFullPath(args[1]);
var size = int.Parse(args[2]);
var sampleCount = args.Length > 3 ? int.Parse(args[3]) : 5;
var mode = args.Length > 4 ? args[4] : "full";
// Fail before measurement if the declared source is a different revision or has tracked edits.
var bundledGit = "C:/Users/ambal/AppData/Local/GitHubDesktop/app-3.6.1/resources/app/git/cmd/git.exe";
var git = Environment.GetEnvironmentVariable("HOBBYDS9_GIT") ?? (File.Exists(bundledGit) ? bundledGit : "git");
string Git(params string[] arguments) {
    var start = new ProcessStartInfo(git) { WorkingDirectory=source, RedirectStandardOutput=true, RedirectStandardError=true, UseShellExecute=false, CreateNoWindow=true };
    foreach(var argument in arguments) start.ArgumentList.Add(argument);
    using var child=Process.Start(start)!; var stdout=child.StandardOutput.ReadToEnd();var stderr=child.StandardError.ReadToEnd();child.WaitForExit();
    if(child.ExitCode!=0)throw new InvalidOperationException("Source identity check failed: "+stderr);return stdout.Trim();
}
var observedRuntimePin=Git("rev-parse","HEAD");var trackedStatus=Git("status","--porcelain","--untracked-files=no");
if(observedRuntimePin!=Pin||trackedStatus.Length!=0)throw new InvalidOperationException("Probe requires clean tracked source at "+Pin+"; observed "+observedRuntimePin+" "+trackedStatus);
var local = Path.Combine(Directory.GetCurrentDirectory(), ".local");
Directory.CreateDirectory(Path.GetDirectoryName(output)!); Directory.CreateDirectory(local);
var measures = new List<object>();
object? world = null; string? failure = null; var total = Stopwatch.StartNew();
var env = new { runtimePin = Pin, observedRuntimePin, trackedSourceClean=trackedStatus.Length==0, source, systemCount = size, seed = "42", mode, runtime = RuntimeInformation.FrameworkDescription,
    os = RuntimeInformation.OSDescription, arch = RuntimeInformation.ProcessArchitecture.ToString(), Environment.ProcessorCount,
    serverGc = System.Runtime.GCSettings.IsServerGC, latencyMode = System.Runtime.GCSettings.LatencyMode.ToString(), configuration = "Release",
    memoryAvailableBytes = GC.GetGCMemoryInfo().TotalAvailableMemoryBytes, utc = DateTimeOffset.UtcNow,
    note = "Serial single-writer probe; no forced GC. Small-n nearest-rank p95/p99 are maxima, not credible population tail estimates. Working/peak process memory includes setup and JIT. No native UI, combat, growing population or concurrent producer benchmark." };
var codec = new ReviewNewtonsoftCodec();
void Write() { var p=Process.GetCurrentProcess(); p.Refresh(); File.WriteAllText(output,JsonSerializer.Serialize(new { environment=env, world, elapsedMs=total.Elapsed.TotalMilliseconds,
    peakWorkingSetBytes=p.PeakWorkingSet64, workingSetBytes=p.WorkingSet64, privateBytes=p.PrivateMemorySize64, failure, measurements=measures }, new JsonSerializerOptions { WriteIndented=true })); }
void Measure(string name, Action action, int n, int warmup=1, Action? prepare=null)
{
    for(int i=0;i<warmup;i++){prepare?.Invoke();action();}
    var rows=new List<object>();var times=new List<double>();
    for(int i=0;i<n;i++) {
        prepare?.Invoke(); var collections=Enumerable.Range(0,3).Select(GC.CollectionCount).ToArray();
        var pause=GC.GetTotalPauseDuration(); var cpu=Process.GetCurrentProcess().TotalProcessorTime; var bytes=GC.GetAllocatedBytesForCurrentThread(); var start=Stopwatch.GetTimestamp();
        action(); var ms=Stopwatch.GetElapsedTime(start).TotalMilliseconds; var allocated=GC.GetAllocatedBytesForCurrentThread()-bytes; times.Add(ms);
        rows.Add(new { index=i, ms, allocatedBytes=allocated,
            gcCollections=Enumerable.Range(0,3).Select(g=>GC.CollectionCount(g)-collections[g]).ToArray(),gcPauseMs=(GC.GetTotalPauseDuration()-pause).TotalMilliseconds,
            processCpuMs=(Process.GetCurrentProcess().TotalProcessorTime-cpu).TotalMilliseconds });
        Console.WriteLine($"{size} {name} {i+1}/{n}: {ms:F2}ms");
        if(total.Elapsed>TimeSpan.FromMinutes(10))throw new TimeoutException("Per-world ten-minute soft budget reached after completed sample.");
    }
    times.Sort(); double Q(double q)=>times[Math.Clamp((int)Math.Ceiling(q*times.Count)-1,0,times.Count-1)];
    measures.Add(new { name, n, warmup, medianMs=times.Count%2==0?(times[times.Count/2-1]+times[times.Count/2])/2:times[times.Count/2],
        p95Ms=Q(.95),p99Ms=Q(.99),minMs=times[0],maxMs=times[^1],samples=rows });Write();
}
try {
    var resources=Path.Combine(source,"client/HobbyDS9.Unity/Assets/HobbyDS9/Review/Resources");
    byte[] Read(string path)=>File.ReadAllBytes(Path.Combine(path.StartsWith("ActivitiesV1/")?Path.Combine(source,"client/HobbyDS9.Unity/Assets/HobbyDS9/Resources"):resources,path+".json"));
    var template=ReviewExactPackageLoader.LoadPlayableFleet(Read("PlayableFleetPackages/v1/playable"),Read("PlayableFleetPackages/v1/package"),Read("PlayableFleetPackages/v1/physical"),Read("PlayableFleetPackages/v1/core"),Read("PlayableFleetPackages/v1/sol08-scenario"),codec);
    byte[][] Assets(string folder,string[] names)=>names.Select(n=>Read(folder+"/"+n)).ToArray();
    var activities=ActivitiesPlaytestOptions.CreatePackage(template,Assets("ActivitiesV1",new[]{"catalogue","ready-classes","support","activities"}),codec);
    var colony=ColonisationPlaytestOptions.CreatePackage(activities,Assets("ColonisationV1",new[]{"catalogue","ready-classes","support","settlement"}),codec);
    var roster=ColonisationPlaytestOptions.CreateRosterPackage(colony,Read("ColonisationRosterV2/roster"),codec);
    var growth=ColonisationGrowthPlaytestOptions.CreatePackage(roster,Read("ColonisationExpansionV1/planner"),codec);
    var package=BeltBodiesPlaytestOptions.CreatePackage(growth);
    if(mode=="validate") {
        foreach(var count in new[]{30,100}) foreach(var suffix in new[]{"baseline","successor"}) {
            var path=Path.Combine(local,$"runtime-{count}-{suffix}.json");var input=File.ReadAllBytes(path);
            var loaded=CampaignTacticalReviewSession.LoadCycleSave(package,codec.DecodeCycleRoot(input));
            var encoded=codec.EncodeCycleRoot(loaded.ExportCycleSave());var same=input.AsSpan().SequenceEqual(encoded);
            measures.Add(new { name="validate_payload", file=Path.GetFileName(path), bytes=input.Length,
                inputSha256=Convert.ToHexString(SHA256.HashData(input)).ToLowerInvariant(),
                reexportSha256=Convert.ToHexString(SHA256.HashData(encoded)).ToLowerInvariant(),
                strictDecodeSucceeded=true, typedLoadReplaySucceeded=true, identicalReexport=same,
                turn=loaded.CaptureStatus().Turn, revision=loaded.CaptureStatus().CampaignRevision });
            Write();if(!same)throw new Exception("Payload reexport differed: "+path);
        }
        return 0;
    }
    var settings=new NaturalGalaxySettingsDto { Map=new() { Seed="42",SystemCount=size,EmptyRegionCount=size,
        Width=size==30?750000:size>=1000?5000000:1250000, Height=size==30?600000:size>=1000?4000000:1200000,
        QuietEmptyAreaTargetPermille=2000,HomeCivilizationDefinitionIds=new(){"civilization.federation","civilization.cardassian"} } };
    CampaignTacticalReviewSession session=null!;
    Measure("start_campaign_including_generation",()=>session=CampaignTacticalReviewSession.StartCycleGame(package,settings),1,0);
    var root=(CampaignBeltBodiesSaveDto)session.ExportCycleSave();
    var game=root.BaseCampaign!.Campaign!.Integrated!.Physical!.Core.Campaign.GameState;
    var bytes=codec.EncodeCycleRoot(root);
    world=new { packageHash=package.PackageHash, root.FormatVersion, root.ProfileId, systems=game.StarSystems!.Count,planets=game.Planets!.Count,
        colonies=game.Colonies!.Count,fleets=game.Fleets!.Count,ships=game.Ships!.Count,belts=root.BeltBodies!.Definitions!.Count,
        journal=root.BaseCampaign.Journal!.Count,game.TurnNumber,game.StateRevision,encodedBytes=bytes.Length,
        payloadSha256=Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant(), settings };
    File.WriteAllBytes(Path.Combine(local,$"runtime-{size}-baseline.json"),bytes);Write();
    if(mode!="worker") {
    int fast=Math.Max(20,sampleCount);
    Measure("capture_status",()=>GC.KeepAlive(session.CaptureStatus()),fast,2);
    Measure("capture_colonisation_actor",()=>GC.KeepAlive(session.CaptureColonisation(Human)),sampleCount);
    Measure("capture_belts_actor",()=>GC.KeepAlive(session.CaptureBeltBodies(Human)),fast,2);
    Measure("capture_refinement_actor",()=>GC.KeepAlive(session.CaptureEngineeringRefinement(Human)),fast,2);
    var flags=BindingFlags.NonPublic|BindingFlags.Instance;
    var copy=typeof(SimulationSession).GetMethod("CopyState",BindingFlags.Static|BindingFlags.NonPublic)!;
    Measure("inner_GameState_CopyState_reflection",()=>GC.KeepAlive(copy.Invoke(null,new object[]{game})),sampleCount);
    var stateField=typeof(CampaignTacticalReviewSession).GetField("state",flags)!;
    var validate=typeof(CampaignTacticalReviewSession).GetMethod("ValidateSave",flags)!;
    Measure("inner_ValidateSave_reflection",()=>validate.Invoke(session,new object?[]{stateField.GetValue(session),null}),sampleCount);
    Measure("export_full_Format18",()=>GC.KeepAlive(session.ExportCycleSave()),sampleCount);
    Measure("encode_full_Format18_Newtonsoft",()=>GC.KeepAlive(codec.EncodeCycleRoot(root)),sampleCount);
    Measure("decode_full_Format18_Newtonsoft_strict",()=>GC.KeepAlive(codec.DecodeCycleRoot(bytes)),sampleCount);
    Measure("codec_CopyCycleSave_encode_decode",()=>GC.KeepAlive(codec.CopyCycleSave(root)),sampleCount);
    Measure("load_replay_from_detached_root",()=>GC.KeepAlive(CampaignTacticalReviewSession.LoadCycleSave(package,root)),sampleCount);
    int intent=0;
    CommandEnvelope Command(bool stale=false) {var status=session.CaptureStatus();return new(){ActorCivilizationId=Human,CommandId="command.probe."+(++intent),ExpectedTurn=status.Turn,ExpectedRevision=status.CampaignRevision-(stale?1:0),
        Kind=CommandKind.SetResearchAllocation,SetResearchAllocation=new(){ScienceAllocationUnits=intent%2==0?300:360}};}
    Measure("rejected_stale_command_real_path",()=>{var result=session.SubmitStrategicCommand(Command(true));if(result.Accepted||result.Error!=CampaignTacticalError.StaleRevision)throw new Exception("Unexpected stale-intent result: "+result.Error);},sampleCount);
    Measure("accepted_research_command_real_path",()=>{var result=session.SubmitStrategicCommand(Command());if(!result.Accepted)throw new Exception(result.Error.ToString());},sampleCount);
    File.WriteAllBytes(Path.Combine(local,$"runtime-{size}-successor.json"),codec.EncodeCycleRoot(session.ExportCycleSave()));
    // Each Advance sample starts at the same legitimate turn-1 root; reset is excluded from timing.
    Measure("advance_one_turn_real_path",()=>{var status=session.CaptureStatus();var result=session.AdvanceTurn(status.Turn,status.CampaignRevision);if(!result.Accepted)throw new Exception(result.Error.ToString());},sampleCount,1,
        ()=>session=CampaignTacticalReviewSession.LoadCycleSave(package,root));
    }
    // Actual host worker, executed synchronously for allocation attribution. Store intentionally excludes encoding/disk; those are separate measurements.
    var host=PhysicalReviewHost.StartColonisationCycle(package,settings,codec,new SinkStore());var frame=host.Read(Human);
    ColonisationCycleCheckpointSaveWork work=null!;
    Measure("host_save_prepare_full_export",()=>work=host.PrepareColonisationCycleCheckpointSave(frame,"probe",true),sampleCount);
    Measure("host_save_worker_copy_verify_no_disk",()=>{var result=work.Execute(()=>false);if(!result.Succeeded)throw new Exception("Worker "+result.Error);},sampleCount,1,
        ()=>work=host.PrepareColonisationCycleCheckpointSave(frame,"probe",true));
} catch(Exception e) { failure=e.ToString(); Console.Error.WriteLine(failure); }
finally { Write(); }
return failure==null?0:1;

sealed class SinkStore:IBackgroundColonisationCycleCheckpointStore
{
    public void Write(string slot,CampaignColonisationCycleSaveDto root,bool overwrite)=>GC.KeepAlive(root);
    public CampaignColonisationCycleSaveDto Read(string slot)=>throw new NotSupportedException();
}
