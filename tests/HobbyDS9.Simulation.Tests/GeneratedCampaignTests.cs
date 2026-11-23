using System.Diagnostics;
using System.Text.Json;
using HobbyDS9.Application;
using HobbyDS9.Contracts;
using HobbyDS9.Simulation;
using Xunit;
using Xunit.Abstractions;

namespace HobbyDS9.Simulation.Tests;

public sealed class GeneratedCampaignTests
{
    readonly ITestOutputHelper output;
    public GeneratedCampaignTests(ITestOutputHelper output)=>this.output=output;
    const string Human = "civ.federation";
    static string Json<T>(T value) => JsonSerializer.Serialize(value);
    static T Clone<T>(T value) => JsonSerializer.Deserialize<T>(Json(value))!;
    static CampaignPlayableFleetPackageDto Template() => PlayableFleetTests.Package();
    static NaturalGalaxySettingsDto Settings(int count = 250, string seed = "42") => new() { Map = new() {
        Seed = seed, SystemCount = count, EmptyRegionCount = count, Width = count == 1000 ? 5000000 : count==30?750000:2500000, Height = count == 1000 ? 4000000 : count==30?600000:2000000,
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
            var map=app.CaptureGeneratedMap(Human);
            var neighbors=map.Regions.ToDictionary(r=>r.NodeId,_=>new List<string>(),StringComparer.Ordinal);
            foreach(var edge in map.Connections){neighbors[edge.EndpointA].Add(edge.EndpointB);neighbors[edge.EndpointB].Add(edge.EndpointA);}
            var distances=new Dictionary<string,int>(StringComparer.Ordinal){{node,0}};var pending=new Queue<string>();pending.Enqueue(node);
            while(pending.Count>0){string current=pending.Dequeue();foreach(string next in neighbors[current])if(distances.TryAdd(next,distances[current]+1))pending.Enqueue(next);}
            string? target=distances.Where(d=>d.Value>0&&!visited.Contains(d.Key)).OrderBy(d=>d.Value).ThenBy(d=>d.Key,StringComparer.Ordinal).Select(d=>d.Key).FirstOrDefault();
            var destination=target==null?null:app.FindKnownRoute(Human,node,target);
            var contact=app.GetKnownContacts(Human).Where(c=>c.Fresh).Select(c=>(Contact:c,Route:app.FindKnownRoute(Human,node,c.LastSeenNodeId)))
                .Where(c=>c.Route.Success).OrderBy(c=>c.Route.TotalCost).ThenBy(c=>c.Contact.FleetId,StringComparer.Ordinal).FirstOrDefault();
            if(contact.Contact!=null)destination=contact.Route;
            var status=app.CaptureStatus();
            Assert.True(app.SubmitTravelOrder(new(){ActorId=Human,CommandId="command.explore."+status.CampaignRevision,
                ExpectedTurn=status.Turn,ExpectedRevision=status.CampaignRevision,Order=new(){FleetId=fleet.FleetId.Value,
                    Intent=contact.Contact!=null?CampaignTravelIntent.Follow:destination==null?CampaignTravelIntent.StayEngage:CampaignTravelIntent.Leave,
                    ContactFleetId=contact.Contact?.FleetId,ExpectedContactSeenTurn=contact.Contact?.LastSeenTurn,
                    RouteNodeIds=destination?.Steps.ToList()??new()}}).Accepted);
        }
    }

    [Fact]
    public void VisibleUnvisitedBordersPermitTransitWithoutRevealingUnknownRegionsAndReplayExactly()
    {
        var app=Start(30);var initial=app.ExportGeneratedSave();var game=initial.Campaign.Integrated.Physical!.Core.Campaign.GameState;
        var knowledge=game.NavigationKnowledge!.Single(k=>k.CivilizationId==Human);
        var known=new HashSet<string>(knowledge.Nodes!.Select(n=>n.Id),StringComparer.Ordinal);
        var recorded=new HashSet<string>(knowledge.Connections!.Select(n=>n.Id),StringComparer.Ordinal);
        var border=app.CaptureGeneratedMap(Human).Connections.First(e=>!recorded.Contains(e.ConnectionId));
        var before=Json(initial);
        var route=app.FindKnownRoute(Human,border.EndpointA,border.EndpointB);
        Assert.True(route.Success);Assert.Equal(1,route.TotalCost);Assert.Equal(new[]{border.EndpointB},route.Steps);
        Assert.Equal(before,Json(app.ExportGeneratedSave()));
        var fleet=Assert.Single(app.GetOwnStrategicFleets(Human));var status=app.CaptureStatus();
        var command=new SubmitCampaignTravelOrderDto{ActorId=Human,CommandId="command.visible.border",ExpectedTurn=status.Turn,ExpectedRevision=status.CampaignRevision,
            Order=new(){FleetId=fleet.FleetId.Value,Intent=CampaignTravelIntent.Leave,RouteNodeIds=new(){border.EndpointA,border.EndpointB}}};
        Assert.True(app.SubmitTravelOrder(command).Accepted);
        var saved=app.ExportGeneratedSave();
        Assert.Equal(known.OrderBy(n=>n),saved.Campaign.Integrated.Physical!.Core.Campaign.GameState.NavigationKnowledge!.Single(k=>k.CivilizationId==Human).Nodes!.Select(n=>n.Id).OrderBy(n=>n));
        Assert.Empty(app.GetKnownContacts(Human));
        var reopened=CampaignGeneratedApplication.LoadSave(Template(),Clone(saved));Assert.Equal(Json(saved),Json(reopened.ExportGeneratedSave()));
        Advance(app);Advance(reopened);Assert.Equal(Json(app.ExportGeneratedSave()),Json(reopened.ExportGeneratedSave()));
    }

    [Fact]
    public void UnknownRegionAndFailedGeneratedTransitCannotPublishInferredBorders()
    {
        var app=Start(30);var initial=app.ExportGeneratedSave();var map=app.CaptureGeneratedMap(Human);
        var known=new HashSet<string>(map.Regions.Select(r=>r.NodeId),StringComparer.Ordinal);
        var unknown=initial.Map.Layout.Regions.First(r=>!known.Contains(r.NodeId)).NodeId;
        var fleet=Assert.Single(app.GetOwnStrategicFleets(Human));var status=app.CaptureStatus();var before=Json(initial);
        Assert.False(app.FindKnownRoute(Human,fleet.LocationNodeId.Value,unknown).Success);
        var border=map.Connections.First(e=>e.EndpointA!=fleet.LocationNodeId.Value&&e.EndpointB!=fleet.LocationNodeId.Value);
        Assert.False(app.SubmitTravelOrder(new(){ActorId=Human,CommandId="command.invalid.border",ExpectedTurn=status.Turn,ExpectedRevision=status.CampaignRevision,
            Order=new(){FleetId=fleet.FleetId.Value,Intent=CampaignTravelIntent.Leave,RouteNodeIds=new(){border.EndpointA,border.EndpointB,unknown}}}).Accepted);
        Assert.Equal(before,Json(app.ExportGeneratedSave()));
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
        Assert.Equal(app.CaptureStatus().CampaignRevision,map.CampaignRevision);Assert.Equal(app.CaptureStatus().Turn,map.ObservationTurn);
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
        for(int i=0;i<128&&app.CaptureStatus().Phase==SessionPhase.Planning;i++) { Explore(app,visited); Advance(app); }
        Assert.Equal(SessionPhase.AwaitingCombat,app.CaptureStatus().Phase);
        var pending=app.ExportGeneratedSave();
        output.WriteLine("Actual250 encounter turn="+app.CaptureStatus().Turn+" human visited="+visited.Count+" known regions="+app.CaptureGeneratedMap(Human).Regions.Count+" transcript="+pending.Campaign.Transcript.Count);
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
        output.WriteLine("Actual250 finalized survivor ships="+app.ExportGeneratedSave().Campaign.Integrated.Physical!.Core.Campaign.GameState.Ships!.Count);
    }

    static void ResolveAndFinalize(ICampaignGeneratedSession app)
    {
        foreach(string encounter in app.GetEncounters(Human))while(!app.Observe(encounter,Human).Complete)
        {
            var observation=app.Observe(encounter,Human);
            Assert.True(app.CommitRound(new(){ActorId=Human,CommandId="command.round."+app.CaptureStatus().CampaignRevision,
                EncounterId=encounter,ExpectedTurn=observation.Turn,ExpectedRound=observation.Round,ExpectedBoundaryRevision=observation.BoundaryRevision,
                Replacements=observation.StandingOrders.Select(s=>new CampaignTacticalFleetOrderDto{FleetId=s.FleetId,Maneuver=CampaignFleetManeuver.Charge,
                    FocusFleetId=observation.LegalFocusFleetIds.FirstOrDefault()}).ToList()}).Accepted);
        }
        var status=app.CaptureStatus();Assert.True(app.FinalizeBatch(status.Turn,status.CampaignRevision).Accepted);
    }
    [Fact]
    public void DestroyedObservedEnemyIsRemovedAtAftermathAndCannotBeFollowedOrReloadedAsFresh()
    {
        // Explicit damaged-opponent fixture, not a change to the shipped class or production rules.
        var template=Template();var scenario=template.Integrated!.Physical!.CorePackage!.Campaign!;
        scenario.InitialState.Ships!.Single(s=>s.OwnerCivilizationId=="civ.cardassian").CurrentHull=1;
        foreach(var shield in scenario.Definitions.AuthoredTacticalEquipment!.InitialShipProvisions!.Single(p=>p.ShipId=="ship.cardassian-scout").InitialShields!)shield.Current=0;
        foreach(var colony in scenario.InitialState.Colonies!.Where(c=>c.OwnerCivilizationId=="civ.cardassian"))colony.Infrastructure!.RemoveAll(i=>i.DefinitionId=="infrastructure.shipyard");
        template.PackageHash=Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(Json(template)))).ToLowerInvariant();
        var app=CampaignGeneratedApplication.StartGame(template,Settings(30));var visited=new HashSet<string>(StringComparer.Ordinal);
        for(int i=0;i<256&&app.CaptureStatus().Phase==SessionPhase.Planning;i++){Explore(app,visited);Advance(app);}
        Assert.Equal(SessionPhase.AwaitingCombat,app.CaptureStatus().Phase);
        var contact=Assert.Single(app.GetKnownContacts(Human));Assert.True(contact.Fresh);
        ResolveAndFinalize(app);
        var save=app.ExportGeneratedSave();
        Assert.DoesNotContain(save.Campaign.Integrated.Physical!.Core.Campaign.GameState.Fleets!,f=>f.FleetId==contact.FleetId);
        Assert.DoesNotContain(app.GetKnownContacts(Human),c=>c.FleetId==contact.FleetId);
        var own=app.GetOwnStrategicFleets(Human).First();var status=app.CaptureStatus();
        Assert.False(app.SubmitTravelOrder(new(){ActorId=Human,CommandId="command.follow.destroyed",ExpectedTurn=status.Turn,ExpectedRevision=status.CampaignRevision,
            Order=new(){FleetId=own.FleetId.Value,Intent=CampaignTravelIntent.Follow,ContactFleetId=contact.FleetId,ExpectedContactSeenTurn=contact.LastSeenTurn,RouteNodeIds=new()}}).Accepted);
        Assert.Equal(Json(save),Json(CampaignGeneratedApplication.LoadSave(template,Clone(save)).ExportGeneratedSave()));
    }

    [Theory]
    [InlineData(0)][InlineData(1)][InlineData(2)][InlineData(3)][InlineData(4)][InlineData(5)]
    [InlineData(6)][InlineData(7)][InlineData(8)][InlineData(9)][InlineData(10)][InlineData(11)]
    public void CorruptOrTruncatedCompleteSaveCannotReplaceLiveAuthority(int mutation)
    {
        var app=Start();Advance(app);var good=app.ExportGeneratedSave();string original=Json(good);var bad=Clone(good);
        switch(mutation)
        {
            case 0:bad.FormatVersion=6;break;
            case 1:bad.PolicyId="unknown-policy";break;
            case 2:bad.TemplatePackageHash=new string('1',64);break;
            case 3:bad.Map.ContentHash=new string('1',64);break;
            case 4:bad.Map.Layout.Regions[0].Boundary[0].X++;break;
            case 5:bad.Map.Features[0].NodeId="nav.foreign";break;
            case 6:bad.Campaign.Integrated.Physical!.Core.Campaign.GameState.Civilizations![0].Credits++;break;
            case 7:bad.Campaign.Integrated.Physical!.Core.Campaign.GameState.StateRevision++;break;
            case 8:bad.Campaign.Integrated.Physical!.Contacts.Add(new(){ActorId=Human,FleetId="fleet.cardassian-scout",LastSeenNodeId=bad.Map.Layout.Regions[0].NodeId,LastSeenTurn=1,TrackingNodeIds=new(){bad.Map.Layout.Regions[0].NodeId}});break;
            case 9:bad.Discoveries.Clear();break;
            case 10:bad.Campaign.Integrated.Physical!.Core.ProfileId=CampaignPlayableFleetIdentity.EmbeddedCoreProfile;break;
            case 11:bad.Campaign.Transcript.Clear();break;
        }
        Assert.Throws<ArgumentException>(()=>CampaignGeneratedApplication.LoadSave(Template(),bad));
        Assert.Equal(original,Json(app.ExportGeneratedSave()));
        Assert.Throws<ArgumentException>(()=>CampaignPlayableFleetApplication.LoadSave(Template(),good.Campaign));
        Assert.Equal(original,Json(CampaignGeneratedApplication.LoadSave(Template(),good).ExportGeneratedSave()));
    }

    [Fact]
    public void RepeatedReorderedTemplateAndReloadedGeneratedTurnsAreExact()
    {
        var template=Template();var reverse=Clone(template);var source=reverse.Integrated!.Physical!.CorePackage!.Campaign!;
        source.InitialState.Civilizations!.Reverse();source.InitialState.Fleets!.Reverse();source.InitialState.Ships!.Reverse();
        source.InitialState.Colonies!.Reverse();source.InitialState.EntityIdentities!.Reverse();
        source.Definitions.Civilizations!.Reverse();source.Definitions.ShipDesigns!.Reverse();
        var settings=Settings();settings.Map.HomeCivilizationDefinitionIds.Reverse();
        var apps=new[]{CampaignGeneratedApplication.StartGame(template,Settings()),CampaignGeneratedApplication.StartGame(reverse,settings),Start()};
        foreach(var app in apps)Assert.Equal(Json(apps[0].ExportGeneratedSave()),Json(app.ExportGeneratedSave()));
        for(int turn=0;turn<8;turn++)
        {
            foreach(var app in apps)Advance(app);
            foreach(var app in apps)Assert.Equal(Json(apps[0].ExportGeneratedSave()),Json(app.ExportGeneratedSave()));
            if(turn==3)apps[2]=CampaignGeneratedApplication.LoadSave(template,Clone(apps[2].ExportGeneratedSave()));
        }
    }

    [Theory]
    [InlineData((int)StrategicTurnPhase.Economy)][InlineData((int)StrategicTurnPhase.Movement)]
    public void LateFailedGeneratedAdvanceRollsBackAiResourcesRngAndDiscovery(int failure)
    {
        var app=Start();string original=Json(app.ExportGeneratedSave());
        Backend(app).PhysicalPhaseProbe=phase=>(int)phase!=failure;
        var status=app.CaptureStatus();Assert.False(app.AdvanceTurn(status.Turn,status.CampaignRevision).Accepted);
        Assert.Equal(original,Json(app.ExportGeneratedSave()));
        Backend(app).PhysicalPhaseProbe=null;Advance(app);
        Assert.NotEqual(original,Json(app.ExportGeneratedSave()));
    }

    [Fact]
    public void LawfulBfsNearestTargetMatchesIndependentAllCandidateRouteOracle()
    {
        var app=Start(30);
        for(int turn=0;turn<12;turn++)
        {
            var before=app.ExportGeneratedSave();var game=before.Campaign.Integrated.Physical!.Core.Campaign.GameState;
            var definitions=(DefinitionCatalogDto)typeof(CampaignTacticalReviewSession).GetField("definitions",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)!.GetValue(Backend(app))!;
            var graph=new WeightedNavigationGraph(definitions);
            var visited=before.Discoveries.Select(d=>d.ActorId+"|"+d.ArrivalNodeId).Concat(before.Map.Layout.HomeCandidates
                .Select(h=>game.Civilizations!.Single(c=>c.DefinitionId==h.CivilizationDefinitionId).CivilizationId+"|"+h.NodeId)).ToHashSet(StringComparer.Ordinal);
            var expected=new Dictionary<string,string?>();
            foreach(var fleet in game.Fleets!.Where(f=>f.OwnerCivilizationId=="civ.cardassian"&&f.RouteNodeIds!.Count==0&&f.TransitTotalCost==0))
                expected[fleet.FleetId]=game.NavigationKnowledge!.Single(k=>k.CivilizationId==fleet.OwnerCivilizationId).Nodes!
                    .Where(n=>n.Level!=NavigationKnowledgeLevel.Unknown&&n.Id!=fleet.LocationNodeId&&!visited.Contains(fleet.OwnerCivilizationId+"|"+n.Id))
                    .Select(n=>graph.FindRoute(game,fleet.OwnerCivilizationId,fleet.LocationNodeId,n.Id)).Where(r=>r.Success&&r.Steps.Count>0)
                    .OrderBy(r=>r.TotalCost).ThenBy(r=>r.Steps.Last(),StringComparer.Ordinal).Select(r=>r.Steps.Last()).FirstOrDefault();
            Advance(app);
            var proof=app.ExportGeneratedSave().Campaign.Integrated.Physical!.MovementProof!;
            foreach(var fleet in expected)Assert.Equal(fleet.Value,proof.Orders.Single(o=>o.FleetId==fleet.Key).RouteNodeIds.LastOrDefault());
        }
    }

    [Fact]
    public void UnknownGeometryFeaturesRoutesAndActorsCannotLeakOrMutateAuthority()
    {
        var app=Start();var save=app.ExportGeneratedSave();var view=app.CaptureGeneratedMap(Human);string bytes=Json(view),original=Json(save);
        var known=view.Regions.Select(r=>r.NodeId).ToHashSet(StringComparer.Ordinal);
        foreach(var hidden in save.Map.Layout.Regions.Where(r=>!known.Contains(r.NodeId)))Assert.DoesNotContain("\""+hidden.NodeId+"\"",bytes);
        foreach(var hidden in save.Map.Features.Where(f=>f.NodeId==null||!known.Contains(f.NodeId)))Assert.DoesNotContain("\""+hidden.FeatureId+"\"",bytes);
        Assert.DoesNotContain("HomeCandidates",bytes);Assert.DoesNotContain("Seed",bytes);Assert.DoesNotContain("StarGroups",bytes);
        var fleet=app.GetOwnStrategicFleets(Human).First();string hiddenNode=save.Map.Layout.Regions.First(r=>!known.Contains(r.NodeId)).NodeId;
        Assert.False(app.FindKnownRoute(Human,fleet.LocationNodeId.Value,hiddenNode).Success);
        var status=app.CaptureStatus();Assert.False(app.SubmitTravelOrder(new(){ActorId=Human,CommandId="command.hidden.route",ExpectedTurn=status.Turn,
            ExpectedRevision=status.CampaignRevision,Order=new(){FleetId=fleet.FleetId.Value,Intent=CampaignTravelIntent.Leave,RouteNodeIds=new(){hiddenNode}}}).Accepted);
        Assert.Equal(original,Json(app.ExportGeneratedSave()));
    }

    [Fact]
    public void ThousandSystemCampaignCreatesKnownProjectionAndLoadsStoredMapAtScale()
    {
        var timer=Stopwatch.StartNew();var app=Start(1000);long startMs=timer.ElapsedMilliseconds;
        var save=app.ExportGeneratedSave();Assert.Equal(2000,save.Map.Layout.Regions.Count);Assert.Equal(1000,save.Map.Layout.Systems.Count);
        Assert.InRange(app.CaptureGeneratedMap(Human).Regions.Count,2,20);Advance(app);save=app.ExportGeneratedSave();
        long loadStart=timer.ElapsedMilliseconds;var loaded=CampaignGeneratedApplication.LoadSave(Template(),Clone(save));
        Assert.Equal(Json(save),Json(loaded.ExportGeneratedSave()));
        output.WriteLine("1000systems startMs="+startMs+" loadMs="+(timer.ElapsedMilliseconds-loadStart)+" UTF8bytes="+System.Text.Encoding.UTF8.GetByteCount(Json(save)));
    }

    [Fact]
    public void ActualKnownLongJourneyReloadsWithSameGeometryRouteAndNextTurn()
    {
        var app=Start();var visited=new HashSet<string>(StringComparer.Ordinal);
        for(int i=0;i<20;i++){Explore(app,visited);Advance(app);}
        var fleet=app.GetOwnStrategicFleets(Human).First();
        var route=app.CaptureGeneratedMap(Human).Regions.Select(r=>app.FindKnownRoute(Human,fleet.LocationNodeId.Value,r.NodeId))
            .Where(r=>r.Success).OrderByDescending(r=>r.TotalCost).First();
        Assert.True(route.TotalCost>6);
        var status=app.CaptureStatus();Assert.True(app.SubmitTravelOrder(new(){ActorId=Human,CommandId="command.long.known.journey",
            ExpectedTurn=status.Turn,ExpectedRevision=status.CampaignRevision,Order=new(){FleetId=fleet.FleetId.Value,Intent=CampaignTravelIntent.Leave,RouteNodeIds=route.Steps.ToList()}}).Accepted);
        Advance(app);Assert.NotEmpty(app.GetOwnStrategicFleets(Human).First().RouteNodeIds);
        var save=app.ExportGeneratedSave();var loaded=CampaignGeneratedApplication.LoadSave(Template(),Clone(save));
        Assert.Equal(Json(save),Json(loaded.ExportGeneratedSave()));Advance(app);Advance(loaded);
        Assert.Equal(Json(app.ExportGeneratedSave()),Json(loaded.ExportGeneratedSave()));
    }

    [Fact]
    public void PartialEdgeOriginCannotCreateCurrentSensorCoverageOrFreshContact()
    {
        // Component-level physical representation test; generated unit-cost edges do not naturally leave partial edges.
        var app=Start();var game=app.ExportGeneratedSave().Campaign.Integrated.Physical!.Core.Campaign.GameState;
        var ownSystems=app.CaptureStrategy(Human).Colonies.Select(c=>c.SystemId.Value).ToHashSet(StringComparer.Ordinal);
        string origin=app.CaptureGeneratedMap(Human).Regions.First(r=>r.SystemId==null||!ownSystems.Contains(r.SystemId)).NodeId;
        var fleet=game.Fleets!.Single(f=>f.OwnerCivilizationId==Human);fleet.LocationNodeId=origin;fleet.TransitTotalCost=2;fleet.TransitProgress=1;
        var coverage=typeof(CampaignTacticalReviewSession).GetMethod("GeneratedCoverage",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)!;
        Assert.DoesNotContain(origin,(List<string>)coverage.Invoke(Backend(app),new object[]{game,Human})!);
        var freshness=typeof(CampaignTacticalReviewSession).GetMethod("GeneratedContactFresh",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)!;
        var contact=new CampaignContactKnowledgeDto{ActorId=Human,FleetId="fleet.cardassian-scout",LastSeenNodeId=origin,LastSeenTurn=game.TurnNumber,TrackingNodeIds=new(){origin}};
        Assert.False((bool)freshness.Invoke(Backend(app),new object[]{game,contact})!);
        fleet.TransitTotalCost=0;fleet.TransitProgress=0;
        Assert.Contains(origin,(List<string>)coverage.Invoke(Backend(app),new object[]{game,Human})!);
        Assert.True((bool)freshness.Invoke(Backend(app),new object[]{game,contact})!);
    }
}
