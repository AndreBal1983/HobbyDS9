using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using HobbyDS9.Application;
using HobbyDS9.Contracts;

internal static class NaturalGalaxyCli
{
    internal static bool Run(string[] args)
    {
        if(args.Length==0||!new[]{"--natural-galaxy","--natural-galaxy-report","--natural-galaxy-open"}.Contains(args[0]))return false;
        var catalog=PlayableFleetPackageReader.Read(Path.Combine(AppContext.BaseDirectory,"scenarios/playable-fleet-v1"))
            .Integrated!.Physical!.CorePackage!.Campaign!.Definitions.Civilizations!;
        if(args[0]=="--natural-galaxy-open"){
            if(args.Length!=2)throw new ArgumentException("Expected natural map path.");var file=new FileInfo(args[1]);
            if(file.Length>NaturalGalaxyJson.MaximumBytes)throw new ArgumentException("Natural map exceeds byte cap.");
            Console.WriteLine("natural-map-sha256="+NaturalGalaxyJson.Read(File.ReadAllText(file.FullName),catalog).Export().ContentHash);return true;
        }
        if(args.Length<3||args.Length>4)throw new ArgumentException("Expected seed, size250/1000 and optional all/none/no-stars/no-black-holes/no-nebulae.");
        int n=int.Parse(args[2],CultureInfo.InvariantCulture);if(n!=250&&n!=1000)throw new ArgumentException("Natural CLI presets250/1000 only.");
        string mode=args.Length==4?args[3]:"all";if(!new[]{"all","none","no-stars","no-black-holes","no-nebulae"}.Contains(mode))throw new ArgumentException("Unknown comparison mode.");
        var settings=new NaturalGalaxySettingsDto{Map=new(){Seed=args[1],SystemCount=n,EmptyRegionCount=n,Width=n==250?2500000:5000000,Height=n==250?2000000:4000000,
            HomeCivilizationDefinitionIds=new(){"civilization.federation","civilization.cardassian"},StellarInfluenceEnabled=mode!="none"&&mode!="no-stars",
            BlackHoleInfluenceEnabled=mode!="none"&&mode!="no-black-holes",NebulaInfluenceEnabled=mode!="none"&&mode!="no-nebulae"}};
        var watch=Stopwatch.StartNew();var review=NaturalGalaxyApplication.Generate(settings,catalog);double generationMs=watch.Elapsed.TotalMilliseconds;
        string json=NaturalGalaxyJson.Write(review);if(args[0]=="--natural-galaxy"){Console.WriteLine(json);return true;}
        watch.Restart();var opened=NaturalGalaxyJson.Read(json,catalog);double openMs=watch.Elapsed.TotalMilliseconds;
        double Reject(NaturalGalaxyDocumentDto invalid){watch.Restart();try{NaturalGalaxyApplication.Open(invalid,catalog);}catch(ArgumentException){return watch.Elapsed.TotalMilliseconds;}throw new InvalidOperationException("Expected rejection.");}
        var malformed=review.Export();malformed.StarGroups[0].Center=null!;double malformedMs=Reject(malformed);
        var wrong=review.Export();wrong.ContentHash=new string('0',64);double wrongMs=Reject(wrong);
        bool preserved=json==NaturalGalaxyJson.Write(review);if(!preserved)throw new InvalidOperationException("Failed load changed current review.");
        var d=review.Export();var homes=d.Layout.HomeCandidates;
        var request=new GalaxyReviewRouteQueryDto{MapContentHash=d.ContentHash,OriginNodeId=homes[0].NodeId,
            Targets=new(){new(){NodeId=homes[1].NodeId,Intent=GalaxyReviewTargetIntent.SystemLocation,SystemId=homes[1].SystemId}},
            Filter=new(){PermittedNodeIds=d.Layout.Regions.Select(r=>r.NodeId).ToList(),PermittedConnectionIds=d.Layout.Connections.Select(e=>e.ConnectionId).ToList()}};
        var plan=review.FindRoutePlan(request);var diagnostics=NaturalGalaxyDiagnostics.Measure(d);var process=Process.GetCurrentProcess();process.Refresh();
        Console.WriteLine(JsonSerializer.Serialize(new{d.ContentHash,d.Settings,d.Tuning,GenerateMs=generationMs,StrictOpenMs=openMs,
            ReopenedHash=opened.Export().ContentHash,MalformedRejectMs=malformedMs,WrongHashRejectMs=wrongMs,FailedOpenPreservedReview=preserved,
            IndentedJsonBytes=System.Text.Encoding.UTF8.GetByteCount(json),PeakWorkingSetBytes=process.PeakWorkingSet64,
            Query=request,Plan=plan,Diagnostics=diagnostics},new JsonSerializerOptions{WriteIndented=true}));return true;
    }
}
