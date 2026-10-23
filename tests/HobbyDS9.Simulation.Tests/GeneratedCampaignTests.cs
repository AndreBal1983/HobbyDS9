using System.Diagnostics;
using System.Text.Json;
using HobbyDS9.Application;
using HobbyDS9.Contracts;
using HobbyDS9.Simulation;
using Xunit;

namespace HobbyDS9.Simulation.Tests;

public sealed class GeneratedCampaignTests
{
    const string Human = "civ.federation";
    static string Json<T>(T value) => JsonSerializer.Serialize(value);
    static T Clone<T>(T value) => JsonSerializer.Deserialize<T>(Json(value))!;
    static CampaignPlayableFleetPackageDto Template() => PlayableFleetTests.Package();
    static NaturalGalaxySettingsDto Settings(int count = 250, string seed = "42") => new() { Map = new() {
        Seed = seed, SystemCount = count, EmptyRegionCount = count, Width = count == 1000 ? 5000000 : 2500000, Height = count == 1000 ? 4000000 : 2000000,
        HomeCivilizationDefinitionIds = new() { "civilization.federation", "civilization.cardassian" } } };
    static ICampaignGeneratedSession Start(int count = 250) => CampaignGeneratedApplication.StartGame(Template(), Settings(count));
    static CampaignTacticalReviewSession Backend(ICampaignGeneratedSession app) => (CampaignTacticalReviewSession)typeof(CampaignIntegratedPlaytestApplication)
        .GetField("session", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.GetValue(app)!;
    static void Advance(ICampaignGeneratedSession app)
    {
        var status = app.CaptureStatus(); var result = app.AdvanceTurn(status.Turn, status.CampaignRevision);
        Assert.True(result.Accepted, result.Error + " " + Backend(app).LastPhysicalFailure);
    }
    static void Explore(ICampaignGeneratedSession app, HashSet<string> visited)
    {
        foreach(var fleet in app.GetOwnStrategicFleets(Human))
        {
            string node=fleet.LocationNodeId.Value; visited.Add(node);
            if(fleet.TransitTotalCost>0||fleet.RouteNodeIds.Count>0)continue;
            var destination=app.CaptureGeneratedMap(Human).Regions.Where(r=>!visited.Contains(r.NodeId))
                .Select(r=>app.FindKnownRoute(Human,node,r.NodeId)).Where(r=>r.Success&&r.Steps.Count>0)
                .OrderBy(r=>r.TotalCost).ThenBy(r=>r.Steps.Last(),StringComparer.Ordinal).FirstOrDefault();
            if(app.GetKnownContacts(Human).Any(c=>c.Fresh&&c.LastSeenNodeId==node))destination=null;
            var status=app.CaptureStatus();
            Assert.True(app.SubmitTravelOrder(new(){ActorId=Human,CommandId="command.explore."+status.CampaignRevision,
                ExpectedTurn=status.Turn,ExpectedRevision=status.CampaignRevision,Order=new(){FleetId=fleet.FleetId.Value,
                    Intent=destination==null?CampaignTravelIntent.StayEngage:CampaignTravelIntent.Leave,
                    RouteNodeIds=destination?.Steps.ToList()??new()}}).Accepted);
        }
    }

    [Fact]
    public void Actual250StartsWithGeneratedGraphAndDetachedKnownGeometryAndWholeSave()
    {
        var app = Start(); var save = app.ExportGeneratedSave();
        var game = save.Campaign.Integrated.Physical!.Core.Campaign.GameState;
        Assert.Equal(250, game.StarSystems!.Count); Assert.Equal(2, game.Civilizations!.Count);
        Assert.DoesNotContain(game.Civilizations, c => c.CivilizationId == "civ.dominion");
        Assert.Equal(500, save.Map.Layout.Regions.Count);
        var map = app.CaptureGeneratedMap(Human);
        Assert.InRange(map.Regions.Count, 2, 20); Assert.Empty(app.GetKnownContacts(Human));
        Assert.All(map.Connections, e => { Assert.Equal(1, e.BaseTraversalCost); Assert.Contains(map.Regions, r => r.NodeId == e.EndpointA); Assert.Contains(map.Regions, r => r.NodeId == e.EndpointB); });
        Assert.All(map.Features, f => Assert.Contains(map.Regions, r => r.NodeId == f.NodeId));
        Assert.Equal(app.CaptureStrategy(Human).Navigation.Nodes.Count, map.Regions.Count);
        Assert.Throws<ArgumentException>(() => app.CaptureGeneratedMap("civ.cardassian"));
        Assert.Throws<InvalidOperationException>(() => app.ExportPlayableSave());
        var bytes = Json(save); map.Regions[0].Center.X++; map.Regions.Clear(); save.Map.Layout.Regions.Clear();
        Assert.Equal(bytes, Json(app.ExportGeneratedSave()));
        var loaded = CampaignGeneratedApplication.LoadSave(Template(), JsonSerializer.Deserialize<CampaignGeneratedSaveDto>(bytes)!);
        Assert.Equal(bytes, Json(loaded.ExportGeneratedSave()));
    }

    [Fact]
    public void PaidProductionResearchAiExplorationAndReloadUseActualGeneratedLoop()
    {
        var app = Start(); var before = app.CaptureStrategy(Human);
        var status = app.CaptureStatus();
        Assert.True(app.SubmitStrategicCommand(new CommandEnvelope { ActorCivilizationId = Human, CommandId = "command.generated.buy",
            Kind = CommandKind.EnqueueProduction, ExpectedTurn = status.Turn, ExpectedRevision = status.CampaignRevision,
            EnqueueProduction = new() { ColonyId = "colony.sol", DefinitionId = "ship-design.source-cruiser" } }).Accepted);
        for (int i = 0; i < 5; i++) Advance(app);
        var save = app.ExportGeneratedSave();
        Assert.Contains(save.Campaign.Integrated.BuiltShips!, s => s.OwnerCivilizationId == Human);
        Assert.Contains(save.Discoveries, d => d.ActorId == "civ.cardassian");
        Assert.NotEqual(Json(before.Research), Json(app.CaptureStrategy(Human).Research));
        var loaded = CampaignGeneratedApplication.LoadSave(Template(), Clone(save));
        Assert.Equal(Json(save), Json(loaded.ExportGeneratedSave()));
        Advance(app); Advance(loaded);
        Assert.Equal(Json(app.ExportGeneratedSave()), Json(loaded.ExportGeneratedSave()));
    }

    [Fact]
    public void Actual250ExplorationMeetsLawfulAiAndCombatReloadsThroughAftermath()
    {
        var app=Start(); var visited=new HashSet<string>(StringComparer.Ordinal);
        for(int i=0;i<500&&app.CaptureStatus().Phase==SessionPhase.Planning;i++) { Explore(app,visited); Advance(app); }
        Assert.Equal(SessionPhase.AwaitingCombat,app.CaptureStatus().Phase);
        var pending=app.ExportGeneratedSave();
        var loaded=CampaignGeneratedApplication.LoadSave(Template(),Clone(pending));
        Assert.Equal(Json(pending),Json(loaded.ExportGeneratedSave()));
        foreach(string encounter in app.GetEncounters(Human))
        {
            while(!app.Observe(encounter,Human).Complete)
            {
                var observation=app.Observe(encounter,Human);
                var command=new CommitCampaignTacticalRoundDto{ActorId=Human,CommandId="command.generated.round."+app.CaptureStatus().CampaignRevision,
                    EncounterId=encounter,ExpectedTurn=observation.Turn,ExpectedRound=observation.Round,ExpectedBoundaryRevision=observation.BoundaryRevision,
                    Replacements=observation.StandingOrders.Select(s=>new CampaignTacticalFleetOrderDto{FleetId=s.FleetId,
                        Maneuver=CampaignFleetManeuver.Hold,FocusFleetId=observation.LegalFocusFleetIds.FirstOrDefault()}).ToList()};
                Assert.True(app.CommitRound(command).Accepted);Assert.True(loaded.CommitRound(Clone(command)).Accepted);
                Assert.Equal(Json(app.ExportGeneratedSave()),Json(loaded.ExportGeneratedSave()));
            }
        }
        var status=app.CaptureStatus();Assert.True(app.FinalizeBatch(status.Turn,status.CampaignRevision).Accepted);
        Assert.True(loaded.FinalizeBatch(status.Turn,status.CampaignRevision).Accepted);
        Assert.NotEmpty(app.GetBattleHistory(Human));Assert.Equal(Json(app.ExportGeneratedSave()),Json(loaded.ExportGeneratedSave()));
        var aftermath=CampaignGeneratedApplication.LoadSave(Template(),Clone(app.ExportGeneratedSave()));
        Assert.Equal(Json(app.ExportGeneratedSave()),Json(aftermath.ExportGeneratedSave()));
    }
}
