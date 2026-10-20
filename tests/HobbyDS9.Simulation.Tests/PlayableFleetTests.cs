using System.Diagnostics;
using System.Text.Json;
using HobbyDS9.Application;
using HobbyDS9.Contracts;
using HobbyDS9.Simulation;
using Xunit;

namespace HobbyDS9.Simulation.Tests;

public sealed partial class PlayableFleetTests
{
    const string Human = "civ.federation";
    static string Json<T>(T value) => JsonSerializer.Serialize(value);
    static T Clone<T>(T value) => JsonSerializer.Deserialize<T>(Json(value))!;
    internal static CampaignPlayableFleetPackageDto Package() => PlayableFleetPackageReader.Read(
        Path.Combine(AppContext.BaseDirectory, "scenarios/playable-fleet-v1"));
    static ICampaignPlayableFleetSession Start() => CampaignPlayableFleetApplication.StartPackage(Package());
    static CampaignTacticalReviewSession Backend(ICampaignPlayableFleetSession app) =>
        (CampaignTacticalReviewSession)typeof(CampaignIntegratedPlaytestApplication)
            .GetField("session", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.GetValue(app)!;
    static GameStateDto Game(ICampaignPlayableFleetSession app) => app.ExportPlayableSave().Integrated!.Physical!.Core.Campaign.GameState;
    static void Advance(ICampaignPlayableFleetSession app)
    {
        var s = app.CaptureStatus();
        var result = app.AdvanceTurn(s.Turn, s.CampaignRevision);
        Assert.True(result.Accepted, result.Error.ToString());
    }
    static SubmitCampaignFleetOrganizationDto Organization(ICampaignPlayableFleetSession app, IEnumerable<string> ships,
        CampaignFleetOrganizationKind kind = CampaignFleetOrganizationKind.Create, string? target = null, string? name = "Task Force")
    {
        var s = app.CaptureStatus();
        return new SubmitCampaignFleetOrganizationDto { ActorId = Human, CommandId = "command.organize." + s.CampaignRevision,
            ExpectedTurn = s.Turn, ExpectedRevision = s.CampaignRevision, Kind = kind,
            ShipIds = ships.ToList(), TargetFleetId = target, DisplayName = name };
    }
    static void Buy(ICampaignPlayableFleetSession app)
    {
        var s = app.CaptureStatus();
        Assert.True(app.SubmitStrategicCommand(new CommandEnvelope { ActorCivilizationId = Human,
            CommandId = "command.buy." + s.CampaignRevision, ExpectedTurn = s.Turn, ExpectedRevision = s.CampaignRevision,
            Kind = CommandKind.EnqueueProduction, EnqueueProduction = new EnqueueProductionPayload {
                ColonyId = "colony.sol", DefinitionId = "ship-design.source-cruiser" } }).Accepted);
    }
    static ICampaignPlayableFleetSession Built()
    {
        var app = Start(); Buy(app); Buy(app);
        for (int i = 0; i < 4; i++) Advance(app);
        Assert.Equal(2, app.ExportPlayableSave().Integrated!.BuiltShips!.Count(s => s.OwnerCivilizationId == Human));
        return app;
    }
    [Fact]
    public void ActualPaidFleetFollowCombatPlaybackAndAftermathAreDeterministic()
    {
        var p = Package();
        var expected = Json(PlayableFleetWalkthrough.Run(p));
        Assert.Equal(expected, Json(PlayableFleetWalkthrough.Run(p, true)));
        Assert.Equal(expected, Json(PlayableFleetWalkthrough.Run(p, false, true)));
        Assert.Equal(expected, Json(PlayableFleetWalkthrough.Run(p)));
        var reversed = Clone(p);
        reversed.Integrated!.Physical!.CorePackage!.Campaign!.InitialState.Fleets!.Reverse();
        reversed.Integrated.Physical.CorePackage.Campaign.InitialState.Civilizations!.Reverse();
        reversed.Integrated.Physical.CorePackage.Campaign.Definitions.ShipDesigns!.Reverse();
        reversed.Integrated.Physical.InitialContacts!.Reverse();
        Assert.Equal(expected, Json(PlayableFleetWalkthrough.Run(reversed)));
    }
    [Fact]
    public void PaidShipsCanFormNamedFleetWithoutResettingIdentityEquipmentStocksOrRng()
    {
        var app = Built(); var before = app.ExportPlayableSave();
        var ships = Game(app).Ships!.Where(s => s.OwnerCivilizationId == Human).Select(s => s.ShipId).ToList();
        var result = app.SubmitFleetOrganization(Organization(app, ships));
        Assert.True(result.Accepted, result.Error.ToString());
        var fleet = Assert.Single(app.CaptureFleetOrganization(Human));
        Assert.Equal(result.FleetId, fleet.Fleet.FleetId.Value); Assert.Equal("Task Force", fleet.DisplayName);
        Assert.True(fleet.CanOrganize);
        var after = app.ExportPlayableSave();
        Assert.Equal(Json(before.Integrated!.BuiltShips), Json(after.Integrated!.BuiltShips));
        Assert.Equal(Json(before.Integrated.Physical!.Core.Pools), Json(after.Integrated.Physical!.Core.Pools));
        Assert.Equal(Json(before.Integrated.Physical.Core.Campaign.GameState.RngState), Json(after.Integrated.Physical.Core.Campaign.GameState.RngState));
        var expected = before.Integrated.Physical.Core.Campaign.GameState.Ships!;
        foreach (var ship in expected) if (ship.OwnerCivilizationId == Human) ship.FleetId = result.FleetId!;
        Assert.Equal(Json(expected), Json(after.Integrated.Physical.Core.Campaign.GameState.Ships));
        Assert.Equal(Json(after), Json(CampaignPlayableFleetApplication.LoadSave(Package(), Clone(after)).ExportPlayableSave()));
    }
    [Fact]
    public void TransferRetiresEmptyFleetAndRenamePersistsAcrossReload()
    {
        var app = Built();
        var ships = Game(app).Ships!.Where(s => s.OwnerCivilizationId == Human).Select(s => s.ShipId).ToList();
        var first = app.SubmitFleetOrganization(Organization(app, ships.Take(1), name: "First"));
        var second = app.SubmitFleetOrganization(Organization(app, ships.Skip(1), name: "Second"));
        Assert.True(first.Accepted); Assert.True(second.Accepted);
        var transfer = app.SubmitFleetOrganization(Organization(app, ships.Take(1), CampaignFleetOrganizationKind.Transfer, second.FleetId, null));
        Assert.True(transfer.Accepted, transfer.Error.ToString());
        Assert.DoesNotContain(Game(app).Fleets!, f => f.FleetId == first.FleetId);
        Assert.True(app.SubmitFleetOrganization(Organization(app, Array.Empty<string>(), CampaignFleetOrganizationKind.Rename, second.FleetId, "United")).Accepted);
        var save = app.ExportPlayableSave();
        var restored = CampaignPlayableFleetApplication.LoadSave(Package(), Clone(save));
        Assert.Equal("United", Assert.Single(restored.CaptureFleetOrganization(Human)).DisplayName);
        Assert.Equal(Json(save), Json(restored.ExportPlayableSave()));
    }
    [Theory]
    [InlineData("foreign")][InlineData("stale")][InlineData("duplicate-ship")][InlineData("bad-name")][InlineData("empty")]
    public void RejectedOrganizationIsAnExactNoOp(string reason)
    {
        var app = Start(); var ships = Game(app).Ships!;
        var command = Organization(app, ships.Where(s => s.OwnerCivilizationId == Human).Select(s => s.ShipId));
        switch (reason)
        {
            case "foreign": command.ShipIds = ships.Where(s => s.OwnerCivilizationId != Human).Select(s => s.ShipId).ToList(); break;
            case "stale": command.ExpectedRevision--; break;
            case "duplicate-ship": command.ShipIds!.Add(command.ShipIds[0]); break;
            case "bad-name": command.DisplayName = " Leading"; break;
            default: command.ShipIds!.Clear(); break;
        }
        var before = Json(app.ExportPlayableSave());
        Assert.False(app.SubmitFleetOrganization(command).Accepted);
        Assert.Equal(before, Json(app.ExportPlayableSave()));
    }
    [Theory]
    [InlineData("names")][InlineData("membership")][InlineData("transcript")][InlineData("profile")][InlineData("closed-operation")]
    public void ForgedSaveCannotBecomeAuthority(string field)
    {
        var app = Start(); var ship = Game(app).Ships!.First(s => s.OwnerCivilizationId == Human);
        Assert.True(app.SubmitFleetOrganization(Organization(app, new[] { ship.ShipId })).Accepted);
        var save = app.ExportPlayableSave();
        switch (field)
        {
            case "names": save.Names![0].DisplayName = "Forged"; break;
            case "membership": save.Integrated!.Physical!.Core.Campaign.GameState.Ships!.First(s => s.OwnerCivilizationId == Human).FleetId = "fleet.forged"; break;
            case "transcript": save.Transcript!.Clear(); break;
            case "profile": save.ProfileId = CampaignFrontierIdentity.Profile; break;
            default: save.Transcript![0].Common = new CampaignFrontierOperationDto(); break;
        }
        Assert.Throws<ArgumentException>(() => CampaignPlayableFleetApplication.LoadSave(Package(), save));
    }
    [Fact]
    public void ExplicitReplayBudgetRejectsMutationWithoutChangingState()
    {
        var package = Package(); package.ReplayOperationLimit = 1;
        var app = CampaignPlayableFleetApplication.StartPackage(package);
        Buy(app); Assert.Equal(0, app.RemainingPlayableOperations);
        var before = Json(app.ExportPlayableSave()); var s = app.CaptureStatus();
        Assert.False(app.AdvanceTurn(s.Turn, s.CampaignRevision).Accepted);
        Assert.False(app.SubmitFleetOrganization(Organization(app, Game(app).Ships!.Where(x => x.OwnerCivilizationId == Human).Select(x => x.ShipId))).Accepted);
        Assert.Equal(before, Json(app.ExportPlayableSave()));
        Assert.Equal(before, Json(CampaignPlayableFleetApplication.LoadSave(package, app.ExportPlayableSave()).ExportPlayableSave()));
    }
    static void Travel(ICampaignPlayableFleetSession app, CampaignTravelIntent intent)
    {
        var s = app.CaptureStatus();
        Assert.True(app.SubmitTravelOrder(new SubmitCampaignTravelOrderDto { ActorId = Human,
            CommandId = "command.travel." + s.CampaignRevision, ExpectedTurn = s.Turn, ExpectedRevision = s.CampaignRevision,
            Order = new CampaignTravelOrderDto { FleetId = app.GetOwnStrategicFleets(Human).Single().FleetId.Value,
                Intent = intent, RouteNodeIds = intent == CampaignTravelIntent.Leave ? new() { "nav.bajor" } : new() } }).Accepted);
    }
    [Fact]
    public void AcceptedRouteMustBeExplicitlyClearedAndPartialTravelAndCombatBlockOrganization()
    {
        var p = Package();
        p.Integrated!.Physical!.CorePackage!.Campaign!.Definitions.NavigationConnections!.Single(c => c.ConnectionId == "connection.sol-bajor").BaseTraversalCost = 12;
        var app = CampaignPlayableFleetApplication.StartPackage(p);
        var ships = Game(app).Ships!.Where(s => s.OwnerCivilizationId == Human).Select(s => s.ShipId).ToList();
        Travel(app, CampaignTravelIntent.Leave);
        var before = Json(app.ExportPlayableSave());
        Assert.Equal(CampaignFleetOrganizationError.FleetHasOrders, app.SubmitFleetOrganization(Organization(app, ships)).Error);
        Assert.Equal(before, Json(app.ExportPlayableSave()));
        Assert.Equal(CampaignFleetOrganizationError.FleetHasOrders, Assert.Single(app.CaptureFleetOrganization(Human)).UnavailableReason);
        Travel(app, CampaignTravelIntent.Stay);
        Assert.True(Assert.Single(app.CaptureFleetOrganization(Human)).CanOrganize);
        Travel(app, CampaignTravelIntent.Leave); Advance(app);
        Assert.True(Game(app).Fleets!.Single(f => f.OwnerCivilizationId == Human).TransitProgress > 0);
        before = Json(app.ExportPlayableSave());
        Assert.Equal(CampaignFleetOrganizationError.FleetInTransit, app.SubmitFleetOrganization(Organization(app, ships)).Error);
        Assert.Equal(before, Json(app.ExportPlayableSave()));
        for (int i = 0; i < 12 && app.CaptureStatus().Phase == SessionPhase.Planning; i++) Advance(app);
        Assert.Equal(SessionPhase.AwaitingCombat, app.CaptureStatus().Phase);
        Assert.Equal(CampaignFleetOrganizationError.WrongPhase, app.SubmitFleetOrganization(Organization(app, ships)).Error);
        var restored = CampaignPlayableFleetApplication.LoadSave(p, app.ExportPlayableSave());
        var id = Assert.Single(restored.GetEncounters(Human));
        Assert.Empty(restored.CaptureBattlePlayback(id, Human).Rounds);
        Assert.Equal(1, restored.Observe(id, Human).Round);
    }
    [Fact]
    public void LateOrganizationFailureRollsBackAllocationMembershipNamesAndReplay()
    {
        var app = Built(); var backend = Backend(app);
        var before = Json(app.ExportPlayableSave());
        var command = Organization(app, Game(app).Ships!.Where(s => s.OwnerCivilizationId == Human).Select(s => s.ShipId));
        backend.PlayableOrganizationProbe = () => throw new InvalidOperationException("Injected after full validation.");
        Assert.False(app.SubmitFleetOrganization(command).Accepted);
        Assert.Equal(before, Json(app.ExportPlayableSave()));
        backend.PlayableOrganizationProbe = null;
        var result = app.SubmitFleetOrganization(command);
        Assert.True(result.Accepted);
        Assert.Equal(Json(app.ExportPlayableSave()), Json(CampaignPlayableFleetApplication.LoadSave(Package(), app.ExportPlayableSave()).ExportPlayableSave()));
        command.ExpectedRevision = app.CaptureStatus().CampaignRevision;
        Assert.Equal(CampaignFleetOrganizationError.DuplicateCommand, app.SubmitFleetOrganization(command).Error);
    }
    [Fact]
    public void OldLoadersRejectTruncatedNewRootsAndNewProfileNeverExportsThem()
    {
        var p = Package(); var app = CampaignPlayableFleetApplication.StartPackage(p);
        var s = app.ExportPlayableSave();
        Assert.Throws<ArgumentException>(() => app.ExportIntegratedSave());
        Assert.Throws<InvalidOperationException>(() => app.ExportSave());
        Assert.Throws<ArgumentException>(() => Backend(app).ExportTravelSave());
        Assert.Throws<ArgumentException>(() => CampaignIntegratedPlaytestApplication.LoadSave(p.Integrated!, s.Integrated!));
        Assert.Throws<ArgumentException>(() => CampaignTacticalReviewSession.Load(p.Integrated!.Physical!.CorePackage!, s.Integrated!.Physical!.Core));
    }
    [Fact]
    public void FullExplicitReplayBudgetRoundTripsAndDoesNotSpendRng()
    {
        var p = Package(); var app = CampaignPlayableFleetApplication.StartPackage(p);
        var fleet = Assert.Single(app.GetOwnStrategicFleets(Human)).FleetId.Value;
        var rng = Json(Game(app).RngState);
        for (int i = 0; i < p.ReplayOperationLimit; i++)
            Assert.True(app.SubmitFleetOrganization(Organization(app, Array.Empty<string>(), CampaignFleetOrganizationKind.Rename, fleet, "Fleet " + i)).Accepted);
        Assert.Equal(0, app.RemainingPlayableOperations);
        Assert.Equal(rng, Json(Game(app).RngState));
        var save = app.ExportPlayableSave();
        var timer = Stopwatch.StartNew();
        var loaded = CampaignPlayableFleetApplication.LoadSave(p, save);
        timer.Stop();
        Assert.Equal(Json(save), Json(loaded.ExportPlayableSave()));
        // Diagnostic measurement, not a machine-dependent timing assertion.
        Console.WriteLine("Playable 2048-operation reload milliseconds=" + timer.ElapsedMilliseconds + " bytes=" + Json(save).Length);
    }
    static ICampaignPlayableFleetSession Pending()
    {
        var app = Start(); Travel(app, CampaignTravelIntent.Leave);
        for (int i = 0; i < 6 && app.CaptureStatus().Phase == SessionPhase.Planning; i++) Advance(app);
        Assert.Equal(SessionPhase.AwaitingCombat, app.CaptureStatus().Phase); return app;
    }
    static void Round(ICampaignPlayableFleetSession app, string id)
    {
        var o = app.Observe(id, Human);
        Assert.True(app.CommitRound(new CommitCampaignTacticalRoundDto {
            ActorId = Human, CommandId = "command.round." + app.CaptureStatus().CampaignRevision + "." + o.Round, EncounterId = id, ExpectedTurn = o.Turn,
            ExpectedRound = o.Round, ExpectedBoundaryRevision = o.BoundaryRevision,
            Replacements = o.StandingOrders.Select(s => new CampaignTacticalFleetOrderDto { FleetId = s.FleetId,
                Maneuver = CampaignFleetManeuver.Charge, FocusFleetId = o.LegalFocusFleetIds.FirstOrDefault() }).ToList()
        }).Accepted);
    }
    [Fact]
    public void PlaybackContainsActualRoundConditionDeltasAndFinalizationFailureCannotPublishHistory()
    {
        var app = Pending(); var id = Assert.Single(app.GetEncounters(Human));
        Assert.Throws<ArgumentException>(() => app.CaptureBattlePlayback(id, "civ.cardassian"));
        Assert.Throws<ArgumentException>(() => app.GetBattleHistory("civ.cardassian"));
        while (!app.Observe(id, Human).Complete) Round(app, id);
        var result = app.CaptureBattlePlayback(id, Human);
        Assert.InRange(result.Rounds.Count, 1, 3); Assert.True(result.Complete);
        Assert.Contains(result.Rounds.SelectMany(r => r.Events), e => e.Kind == CampaignPlaybackEventKind.WeaponFired);
        foreach (var round in result.Rounds)
        {
            Assert.Equal(Enumerable.Range(0, round.Events.Count), round.Events.Select(e => e.Sequence));
            foreach (var ship in round.Before.OwnShips.Concat(round.Before.VisibleEnemies))
            {
                var after = round.After.OwnShips.Concat(round.After.VisibleEnemies).Single(s => s.ShipId == ship.ShipId);
                Assert.Equal((decimal)(ship.Hull - after.Hull), round.Events.Where(e => e.Kind == CampaignPlaybackEventKind.HullDamage && e.TargetShipId == ship.ShipId).Sum(e => (decimal)e.Amount));
                Assert.Equal(ship.Shields.Sum(s => (decimal)s) - after.Shields.Sum(s => (decimal)s),
                    round.Events.Where(e => e.TargetShipId == ship.ShipId).Sum(e => e.Kind == CampaignPlaybackEventKind.ShieldDamage
                        ? (decimal)e.Amount : e.Kind == CampaignPlaybackEventKind.ShieldRecharged ? -(decimal)e.Amount : 0));
            }
        }
        var before = Json(app.ExportPlayableSave()); var status = app.CaptureStatus();
        Backend(app).FinalizationProbe = _ => throw new InvalidOperationException("Late aftermath failure.");
        Assert.False(app.FinalizeBatch(status.Turn, status.CampaignRevision).Accepted);
        Assert.Equal(before, Json(app.ExportPlayableSave())); Assert.Empty(app.GetBattleHistory(Human));
        Assert.Equal(Json(result), Json(app.CaptureBattlePlayback(id, Human)));
        Backend(app).FinalizationProbe = null;
        Assert.True(app.FinalizeBatch(status.Turn, status.CampaignRevision).Accepted);
        Assert.Equal(Json(result), Json(Assert.Single(app.GetBattleHistory(Human))));
        var finalized = Json(app.ExportPlayableSave());
        Assert.False(app.FinalizeBatch(status.Turn, status.CampaignRevision).Accepted);
        Assert.Equal(finalized, Json(app.ExportPlayableSave()));
        var loaded = CampaignPlayableFleetApplication.LoadSave(Package(), app.ExportPlayableSave());
        Assert.Equal(Json(result), Json(Assert.Single(loaded.GetBattleHistory(Human))));
    }
    [Fact]
    public void HiddenLauncherProjectionOmitsIdentityRangePrivateWeaponAndOffensiveDetails()
    {
        // Isolated projection fixture. This never imports a caller-supplied tactical result into the campaign.
        var app = Pending(); var battle = app.ExportPlayableSave().Integrated!.Physical!.Core.Battles.Single().State;
        var ownFleet = battle.Formations.Single(f => f.SideId == Human); var own = ownFleet.Ships.First();
        var enemyFleet = battle.Formations.Single(f => f.SideId != Human); var enemy = enemyFleet.Ships.First();
        var raw = new[] {
            new TacticalEventV2(10, 1, TacticalEventKind.WeaponDischarged, enemyFleet.FormationId, enemy.ShipId,
                ownFleet.FormationId, own.ShipId, "private.launcher", TacticalSector.Fore, TacticalRange.Distant, 999, 7500, "Torpedo"),
            new TacticalEventV2(11, 1, TacticalEventKind.AttackHit, enemyFleet.FormationId, enemy.ShipId,
                ownFleet.FormationId, own.ShipId, "private.launcher", TacticalSector.Fore, TacticalRange.Distant, 999, 7500, "private-roll"),
            new TacticalEventV2(12, 1, TacticalEventKind.HullDamage, ownFleet.FormationId, own.ShipId,
                null, null, null, null, null, 7, 0, "private-mitigation"),
            new TacticalEventV2(13, 1, TacticalEventKind.OrderCommitted, enemyFleet.FormationId, null,
                null, null, null, null, null, 0, 0, "private-order")
        };
        var method = typeof(CampaignTacticalReviewSession).GetMethod("PlaybackEvents", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)!;
        var events = ((IEnumerable<CampaignPlaybackEvent>)method.Invoke(null, new object[] {
            battle, new[] { enemy.ShipId }, Human, raw })!).ToList();
        Assert.Equal(3, events.Count);
        Assert.Equal(CampaignPlaybackEventKind.IncomingTorpedo, events[0].Kind);
        Assert.All(events, e => { Assert.Null(e.SourceShipId); Assert.Null(e.SourceFleetId); Assert.Null(e.Range); });
        Assert.All(events, e => Assert.Equal(own.ShipId, e.TargetShipId));
        Assert.Equal(0, events[0].Amount); Assert.Equal(0, events[1].Amount); Assert.Equal(7, events[2].Amount);
        var serialized = Json(events);
        Assert.DoesNotContain(enemy.ShipId, serialized); Assert.DoesNotContain("private", serialized);
        Assert.DoesNotContain("Chance", serialized); Assert.DoesNotContain("WeaponId", serialized);
        Assert.Equal(new[] { 0, 1, 2 }, events.Select(e => e.Sequence));
    }
    [Fact]
    public void ObservedDamageTotalsRemainExactBeyondInt64()
    {
        var events = Enumerable.Range(0, 3).Select(i => new CampaignPlaybackEvent("combat.example", i + 1, 0,
            CampaignPlaybackEventKind.ShieldDamage, null, null, "ship.example", "fleet.example",
            null, TacticalSector.Fore, null, long.MaxValue)).ToList();
        var stats = new CampaignBattleShipStatistics("ship.example", "fleet.example", true, events);
        Assert.Equal(3m * long.MaxValue, stats.ShieldDamageObserved);
    }
    [Fact]
    public void ExhaustedIdentityAllocatorCannotPublishPartialFleet()
    {
        var app = Start();
        // Fault injection at the owned counter; initial-content validation correctly requires a fresh counter,
        // and the new loader must never accept a forged counter just to manufacture this test condition.
        var owned = (CampaignTacticalReviewSaveDto)typeof(CampaignTacticalReviewSession)
            .GetField("state", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.GetValue(Backend(app))!;
        owned.Campaign.GameState.NextEntitySequence = ulong.MaxValue;
        var before = Json(app.ExportPlayableSave());
        var result = app.SubmitFleetOrganization(Organization(app, Game(app).Ships!.Where(s => s.OwnerCivilizationId == Human).Select(s => s.ShipId)));
        Assert.Equal(CampaignFleetOrganizationError.Overflow, result.Error);
        Assert.Equal(before, Json(app.ExportPlayableSave()));
    }
    [Fact]
    public void LaterPaidConstructionCanJoinAnExistingCreatedFleetWithoutRewritingBirthRecords()
    {
        var app = Start();
        var fleet = app.SubmitFleetOrganization(Organization(app, Game(app).Ships!.Where(s => s.OwnerCivilizationId == Human).Select(s => s.ShipId)));
        Assert.True(fleet.Accepted);
        Buy(app); Advance(app); Advance(app);
        var save = app.ExportPlayableSave();
        var built = Assert.Single(save.Integrated!.BuiltShips!, s => s.OwnerCivilizationId == Human);
        Assert.Equal(fleet.FleetId, built.FleetIdAtConstruction);
        Assert.Equal(Json(save), Json(CampaignPlayableFleetApplication.LoadSave(Package(), save).ExportPlayableSave()));
    }
    [Theory]
    [InlineData(null)][InlineData(0)][InlineData(-1)][InlineData(65537)]
    public void ReplayBudgetHasNoImplicitDefault(int? limit)
    {
        var p = Package(); p.ReplayOperationLimit = limit;
        Assert.Throws<ArgumentException>(() => CampaignPlayableFleetApplication.StartPackage(p));
    }
}
