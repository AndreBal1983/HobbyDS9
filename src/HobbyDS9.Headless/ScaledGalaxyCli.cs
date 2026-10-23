using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using HobbyDS9.Application;
using HobbyDS9.Contracts;

internal static class ScaledGalaxyCli
{
    internal static bool Run(string[] args)
    {
        if (args.Length == 0 || !new[] { "--scaled-galaxy", "--scaled-galaxy-report", "--scaled-galaxy-open" }.Contains(args[0])) return false;
        var admitted = PlayableFleetPackageReader.Read(Path.Combine(AppContext.BaseDirectory,"scenarios/playable-fleet-v1"))
            .Integrated!.Physical!.CorePackage!.Campaign!.Definitions.Civilizations!;
        if(args[0] == "--scaled-galaxy-open") {
            if(args.Length != 2) throw new ArgumentException("Expected scaled-map file.");
            var file = new FileInfo(args[1]);
            if(file.Length > ScaledGalaxyIdentity.MaximumJsonBytes) throw new ArgumentException("Scaled-map file exceeds byte bound.");
            Console.WriteLine("scaled-map-sha256="+ScaledGalaxyJson.Read(File.ReadAllText(file.FullName),admitted).Export().ContentHash); return true;
        }
        if(args.Length < 3 || args.Length > 4) throw new ArgumentException("Expected seed, size250/1000 and optional comparison mode.");
        int count = int.Parse(args[2],CultureInfo.InvariantCulture);
        if(count != 250 && count != 1000) throw new ArgumentException("CLI review presets are250/1000; API admits explicit bounded settings.");
        string mode=args.Length == 4 ? args[3] : "all";
        if(!new[] {"all","none","no-stars","no-black-holes","no-nebulae"}.Contains(mode)) throw new ArgumentException("Unsupported comparison mode.");
        var settings = new ScaledGalaxySettingsDto { Seed=args[1],SystemCount=count,EmptyRegionCount=count,Width=count==250?2500000:5000000,Height=count==250?2000000:4000000,
            HomeCivilizationDefinitionIds=new() {"civilization.federation","civilization.cardassian"},StellarInfluenceEnabled=mode!="none"&&mode!="no-stars",
            BlackHoleInfluenceEnabled=mode!="none"&&mode!="no-black-holes",NebulaInfluenceEnabled=mode!="none"&&mode!="no-nebulae" };
        var watch=Stopwatch.StartNew(); var review=ScaledGalaxyApplication.Generate(settings,admitted); double generationMs=watch.Elapsed.TotalMilliseconds;
        string json=ScaledGalaxyJson.Write(review);
        if(args[0]=="--scaled-galaxy") {Console.WriteLine(json);return true;}
        watch.Restart(); var reopened=ScaledGalaxyJson.Read(json,admitted); double openMs=watch.Elapsed.TotalMilliseconds;
        var d=review.Export(); var homes=d.Layout.HomeCandidates;
        var diagnostics=ScaledGalaxyDiagnostics.Measure(d);
        var process=Process.GetCurrentProcess();process.Refresh();
        Console.WriteLine(JsonSerializer.Serialize(new {SourceStatus="Supported full Generate/strict JSON/Open; no Unity/native claim",d.ContentHash,d.Settings,d.Tuning,
            GenerateMs=generationMs,StrictOpenMs=openMs,IndentedJsonBytes=System.Text.Encoding.UTF8.GetByteCount(json),ReopenedHash=reopened.Export().ContentHash,
            PeakWorkingSetBytes=process.PeakWorkingSet64,OriginNodeId=homes[0].NodeId,DestinationNodeId=homes[1].NodeId,
            Route=review.FindRoute(homes[0].NodeId,homes[1].NodeId),Diagnostics=diagnostics},new JsonSerializerOptions {WriteIndented=true}));return true;
    }
}
