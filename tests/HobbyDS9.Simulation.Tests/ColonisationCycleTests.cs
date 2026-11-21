using System.Text.Json;
using HobbyDS9.Contracts;
using Xunit;
using static HobbyDS9.Simulation.Tests.ColonisationTransportTests;
namespace HobbyDS9.Simulation.Tests;
public sealed class ColonisationCycleTests
{
    readonly Xunit.Abstractions.ITestOutputHelper output;
    public ColonisationCycleTests(Xunit.Abstractions.ITestOutputHelper output){this.output=output;}
    internal static CampaignColonisationCyclePackageDto Package()=>CampaignTacticalReviewSession.CreateCyclePackage(ColonisationGrowthTests.Package());
    internal static string Wire(object? o)=>JsonSerializer.Serialize(o);
    internal static T Copy<T>(T o)=>JsonSerializer.Deserialize<T>(Wire(o))!;
    // One ordinary public journey shared by the focused cases; every returned DTO/session is detached.
    internal static readonly Lazy<(Dictionary<string,string> Roots,List<string> Trace)> Journey=new(()=>{
        var s=CampaignTacticalReviewSession.StartCycleGame(Package(),PublishedConstructionProfileTests.Settings(250));var roots=new Dictionary<string,string>();var trace=new List<string>();
        for(int n=0;n<=128;n++)
        {
            var root=s.ExportCycleSave();var actor=Assert.Single(root.CycleActors!);var first=Assert.Single(root.AiPlans!);var game=Game(root.BaseCampaign!);var active=actor.Active;var p=active?.Assignment;
            trace.Add($"turn={game.TurnNumber} bootstrap={first.Stage}/{first.HoldReason} receipts={actor.Receipts!.Count} active={active?.Ordinal}/{p?.Stage}/{p?.HoldReason} manifest={active?.SettlementManifestId} target={p?.TargetPlanetId}");
            void Capture(string name){if(!roots.ContainsKey(name))roots.Add(name,Wire(root));}
            if(actor.Active==null&&root.BaseCampaign!.Colonisation!.ManifestRecords!.Any(m=>m.ManifestId==first.ManifestId&&m.Kind==SettlerManifestKind.Settlement&&m.Stage==SettlerManifestStage.Loaded))Capture("bootstrap-loaded");
            if(first.Stage!=ColonisationAiStage.GrowthClosed&&actor.Active==null&&root.BaseCampaign!.Colonisation!.CampSources!.Any(c=>c.CampId==first.CampId&&c.Stage==ColonyEstablishmentStage.Established)
                &&!root.BaseCampaign.Colonisation.ManifestRecords!.Any(m=>m.OwnerId==actor.ActorId&&m.Kind==SettlerManifestKind.ImmigrationOnly))Capture("before-immigration");
            if(first.Stage==ColonisationAiStage.GrowthClosed)Capture("bootstrap");
            if(active?.Ordinal==1)
            {
                if(root.BaseCampaign!.Production!.Any(q=>q.OwnerId==actor.ActorId&&(q.QueueItemId==p!.KitQueueId||q.QueueItemId==p.TransportQueueId)))Capture("queue");
                if(root.BaseCampaign!.Colonisation!.ManifestRecords!.Any(m=>m.ManifestId==active.SettlementManifestId&&m.Stage==SettlerManifestStage.Loaded))Capture("loaded");
                if(root.BaseCampaign.Colonisation.CampSources!.Any(c=>c.ManifestId==active.SettlementManifestId&&c.Stage==ColonyEstablishmentStage.Founding))Capture("founding");
                if(root.BaseCampaign.Colonisation.ManifestRecords!.Any(m=>m.OwnerId==actor.ActorId&&m.Kind==SettlerManifestKind.SupplyOnly&&m.Stage==SettlerManifestStage.Loaded))Capture("supply");
            }
            if(actor.Receipts.Count==1&&active==null)Capture("closed1");
            if(actor.Receipts.Count==2&&active==null)Capture("closed2");
            Assert.True(first.LatestActions!.Count<=8);Assert.True(first.LatestActions.Count(a=>a.Accepted)<=4);
            if(actor.Receipts.Count(x=>x.Outcome==ColonisationCycleOutcome.Established)>=2&&active?.Ordinal>=3
                &&(active.SettlementManifestId!=null||root.BaseCampaign!.Production!.Any(q=>q.OwnerId==actor.ActorId&&(q.QueueItemId==p!.KitQueueId||q.QueueItemId==p.TransportQueueId))))
            {Capture("next-real");return(roots,trace);}
            if(n<128)Advance(s);
        }
        throw new InvalidOperationException("Ordinary public250 blocked within128: "+string.Join(Environment.NewLine,trace));
    });
    internal static CampaignColonisationCycleSaveDto Root(string name)=>JsonSerializer.Deserialize<CampaignColonisationCycleSaveDto>(Journey.Value.Roots[name])!;
    internal static CampaignTacticalReviewSession Fresh(string name)=>CampaignTacticalReviewSession.LoadCycleSave(Package(),Root(name));
    [Fact]public void Actual250BootstrapTwoCycleEstablishmentsAndFurtherRealCommitWithin128()
    {
        foreach(var line in Journey.Value.Trace)output.WriteLine(line);var root=Root("next-real");var first=Assert.Single(root.AiPlans!);var actor=Assert.Single(root.CycleActors!);
        Assert.Equal(ColonisationAiStage.GrowthClosed,first.Stage);Assert.Equal(2,actor.Receipts!.Count);Assert.Equal(3,actor.Active!.Ordinal);Assert.NotNull(actor.Active.SettlementManifestId);
        Assert.Equal(2,actor.Receipts.Select(x=>x.TargetPlanetId).Distinct().Count());
        foreach(var receipt in actor.Receipts)
        {var m=root.BaseCampaign!.Colonisation!.ManifestRecords!.Single(m=>m.ManifestId==receipt.SettlementManifestId);Assert.Equal(100,m.PopulationTransferred);Assert.Equal(1,m.EquipmentConsumed);
            var origin=root.BaseCampaign.Colonisation.ManifestOrigins!.Single(o=>o.ManifestId==m.ManifestId);Assert.Equal(100,origin.EmbarkedPopulation);Assert.Equal(20,origin.LoadedFood);}
        Assert.Equal(Wire(root),Wire(CampaignTacticalReviewSession.LoadCycleSave(Package(),root).ExportCycleSave()));
    }
    [Fact]public void RealQueueLoadedFoundingSupplyAndTerminalCheckpointsReplayBothHistoryAndCommitments()
    {
        foreach(var name in new[]{"queue","loaded","founding","supply","closed1","closed2"})
        {var root=Root(name);Assert.Equal(Wire(root),Wire(CampaignTacticalReviewSession.LoadCycleSave(Package(),root).ExportCycleSave()));output.WriteLine($"{name} turn={Game(root.BaseCampaign!).TurnNumber} full replay exact");}
        var supply=Root("supply");Assert.NotNull(supply.CycleActors![0].SupplyCampId);
        var rootFinal=Root("next-real");var records=rootFinal.BaseCampaign!.Colonisation!.ManifestRecords!;var ship=records.Single(m=>m.ManifestId==rootFinal.CycleActors![0].Receipts![0].SettlementManifestId).ShipId;
        Assert.All(rootFinal.CycleActors![0].Receipts!,x=>Assert.Equal(ship,records.Single(m=>m.ManifestId==x.SettlementManifestId).ShipId));
    }
    [Fact]public void ExplicitOwnedSourceCopiesAndOldExportsRemainClosed()
    {
        var package=Package();var s=CampaignTacticalReviewSession.StartCycleGame(package,PublishedConstructionProfileTests.Settings(30));var before=Wire(s.ExportCycleSave());
        package.Policy!.ActiveMissions=2;package.BaseGrowth!.PlannerPolicy!.MaximumAccepted=1;Assert.Equal(before,Wire(s.ExportCycleSave()));
        var exported=s.ExportCycleSave();exported.CycleActors![0].SupplyCampId="foreign";exported.AiPlans![0].HoldReason="changed";Assert.Equal(before,Wire(s.ExportCycleSave()));
        Assert.Throws<ArgumentException>(()=>s.ExportRepeatSave());Assert.Throws<ArgumentException>(()=>s.ExportGrowthSave());Assert.Throws<ArgumentException>(()=>s.ExportColonisationExpansionSave());Assert.Throws<ArgumentException>(()=>s.ExportColonisationSave());
        var bad=Package();bad.PackageHash=new string('0',64);Assert.Throws<ArgumentException>(()=>CampaignTacticalReviewSession.StartCycleGame(bad,PublishedConstructionProfileTests.Settings(30)));
        var loaded=CampaignTacticalReviewSession.LoadCycleSave(Package(),s.ExportCycleSave());var detached=loaded.ExportCycleSave();Assert.Equal(before,Wire(detached));
        var app=HobbyDS9.Application.CampaignColonisationCycleApplication.StartCycleGame(Package(),PublishedConstructionProfileTests.Settings(30));var status=app.CaptureStatus();var fork=Assert.IsAssignableFrom<HobbyDS9.Application.ICampaignColonisationCycleSession>(app.ForkOperations(status.Turn,status.CampaignRevision));
        Assert.True(fork.AdvanceTurn(status.Turn,status.CampaignRevision).Accepted);Assert.Equal(before,Wire(app.ExportCycleSave()));
    }
}
