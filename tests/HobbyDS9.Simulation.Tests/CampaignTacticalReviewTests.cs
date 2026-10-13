using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using HobbyDS9.Application;
using HobbyDS9.Contracts;
using HobbyDS9.Simulation;
using Xunit;

namespace HobbyDS9.Simulation.Tests;

public sealed class CampaignTacticalReviewTests
{
    private const string A = "civ.federation", B = "civ.cardassian";
    internal static CampaignTacticalReviewPackageDto Package()
    {
        var folder = Path.Combine(AppContext.BaseDirectory, "scenarios", "tactical-v2-campaign-review");
        var sourceBytes = File.ReadAllBytes(Path.Combine(folder, "scenario.json"));
        var profileBytes = File.ReadAllBytes(Path.Combine(folder, "package.json"));
        var package = JsonSerializer.Deserialize<CampaignTacticalReviewPackageDto>(profileBytes)!;
        package.Campaign = JsonSerializer.Deserialize<ScenarioDto>(sourceBytes)!;
        var sourceHash = Convert.ToHexString(SHA256.HashData(sourceBytes)).ToLowerInvariant();
        var profileHash = Convert.ToHexString(SHA256.HashData(profileBytes)).ToLowerInvariant();
        package.Campaign.ContentHash = sourceHash;
        package.PackageHash = Convert.ToHexString(SHA256.HashData(Encoding.ASCII.GetBytes("campaign-tactical-review-package-v1\n" + profileHash + "\n" + sourceHash + "\n"))).ToLowerInvariant();
        return package;
    }
    private static string Json<T>(T value) => JsonSerializer.Serialize(value);
    private static T Clone<T>(T value) => JsonSerializer.Deserialize<T>(Json(value))!;
    private static ICampaignTacticalReviewSession Start(CampaignTacticalReviewPackageDto? p = null) => CampaignTacticalReviewApplication.StartReviewPackage(p ?? Package());
    private static string Allocate(ICampaignTacticalReviewSession app)
    {
        var s = app.CaptureStatus(); Assert.True(app.AdvanceTurn(s.Turn, s.CampaignRevision).Accepted);
        return Assert.Single(app.GetEncounters(A));
    }
    private static CommitCampaignTacticalRoundDto Command(ICampaignTacticalReviewSession app, string id, string actor, CampaignFleetManeuver? maneuver = null)
    {
        var b = app.Observe(id, actor);
        return new CommitCampaignTacticalRoundDto { ActorId = actor, CommandId = "command." + actor + "." + id + "." + b.Round,
            EncounterId = id, ExpectedTurn = b.Turn, ExpectedRound = b.Round, ExpectedBoundaryRevision = b.BoundaryRevision,
            Replacements = maneuver.HasValue ? b.StandingOrders.Where(o => b.OwnShips.Any(s => s.Condition.FleetId == o.FleetId && s.Condition.Hull > 0))
                .Select(o => new CampaignTacticalFleetOrderDto { FleetId = o.FleetId, Maneuver = maneuver.Value, FocusFleetId = o.FocusFleetId }).ToList()
                : new List<CampaignTacticalFleetOrderDto>() };
    }
    private static void Round(ICampaignTacticalReviewSession app, string id, bool reverse = false, bool reload = false,
        CampaignTacticalReviewPackageDto? package = null, Action<ICampaignTacticalReviewSession>? replaced = null, CampaignFleetManeuver? maneuver = null)
    {
        var a = Command(app, id, A, maneuver); var b = Command(app, id, B, maneuver);
        Assert.True(app.CommitRound(reverse ? b : a).Accepted);
        if (reload) { app = CampaignTacticalReviewApplication.LoadReviewSave(package!, Clone(app.ExportSave())); replaced!(app); }
        Assert.True(app.CommitRound(reverse ? a : b).Accepted);
    }
    [Fact]
    public void ActualScenarioApplicationOwnsExactNonFullPoolsAndRemainsGated()
    {
        var p = Package(); var app = Start(p); var ships = app.InspectOwnShips(A);
        Assert.Equal(2, ships.Count);
        var cruiser = ships.Single(s => s.Condition.ShipId == "ship.equipment-cruiser");
        Assert.Equal(new long[] { 18, 25, 12, 0 }, cruiser.Condition.Shields);
        Assert.Equal(7, cruiser.Weapons.Single(w => w.Class == TacticalWeaponClass.Torpedo).Ammunition);
        Assert.Equal("fit.provisional-cruiser", cruiser.FitId); Assert.Equal(1, cruiser.FitRevision);
        var legacy = CampaignApplication.StartScenario(p.Campaign!);
        Assert.False(((ICampaignTacticalV2Inspection)legacy).InspectCampaignTacticalV2(A).CanActivate);
        var id = Allocate(app); Assert.False(app.Observe(id, A).ProductionActivationAllowed);
        Assert.All(app.ExportSave().Battles[0].State.Geometry, g => Assert.Equal(TacticalRange.Distant, g.Range));
        Assert.Equal(SessionPhase.AwaitingCombat, app.CaptureStatus().Phase);
    }
    [Fact]
    public void InitialProvisioningIsOnceAndInspectionExportInputAreDetached()
    {
        var p = Package(); var app = Start(p); var before = Json(app.ExportSave());
        p.Campaign!.Definitions.AuthoredTacticalEquipment!.InitialShipProvisions![0].InitialAmmunition![0].Stock = 999;
        p.Rules!.ArmorNumerator = 99; p.CloakSources![0].ConcurrentPowerRequired = 999;
        var save = app.ExportSave(); save.Pools[0].Shields[0].Current = 999;
        var view = app.InspectOwnShips(A); Assert.Throws<NotSupportedException>(() => ((IList<long>)view[0].Condition.Shields)[0] = 999);
        for (var i = 0; i < 10; i++) app.InspectOwnShips(A);
        Assert.Equal(before, Json(app.ExportSave()));
    }
    [Fact]
    public void PrivateCommitmentUsesTheSameBoundaryAndDoesNotChangeOpposingObservation()
    {
        var app = Start(); var id = Allocate(app);
        var before = Json(app.Observe(id, B)); var b = Command(app, id, B);
        var global = app.CaptureStatus().CampaignRevision;
        Assert.True(app.CommitRound(Command(app, id, A, CampaignFleetManeuver.Flank)).Accepted);
        Assert.Equal(global + 1, app.CaptureStatus().CampaignRevision);
        Assert.Equal(before, Json(app.Observe(id, B)));
        Assert.Equal(1, app.Observe(id, A).Round); Assert.True(app.Observe(id, A).OwnCommitmentAccepted);
        Assert.True(app.CommitRound(b).Accepted); Assert.Equal(2, app.Observe(id, A).Round);
    }
    [Theory]
    [InlineData("stale-turn")]
    [InlineData("stale-round")]
    [InlineData("stale-boundary")]
    [InlineData("actor")]
    [InlineData("foreign-fleet")]
    [InlineData("exit")]
    [InlineData("duplicate")]
    [InlineData("missing-orders")]
    [InlineData("missing-stamp")]
    [InlineData("unknown-focus")]
    [InlineData("same-side-focus")]
    public void RejectedCommitmentIsAtomicBeforeRngResourcesLedgerOrRevision(string defect)
    {
        var app = Start(); var id = Allocate(app); var c = Command(app, id, A, CampaignFleetManeuver.Screen);
        switch (defect)
        {
            case "stale-turn": c.ExpectedTurn++; break;
            case "stale-round": c.ExpectedRound++; break;
            case "stale-boundary": c.ExpectedBoundaryRevision++; break;
            case "actor": c.ActorId = "civ.unknown"; break;
            case "foreign-fleet": c.Replacements![0].FleetId = "fleet.cardassian-scout"; break;
            case "exit": c.Replacements![0].Maneuver = (CampaignFleetManeuver)99; break;
            case "duplicate": c.Replacements!.Add(c.Replacements[0]); break;
            case "missing-orders": c.Replacements = null; break;
            case "missing-stamp": c.ExpectedRound = null; break;
            case "unknown-focus": c.Replacements![0].FocusFleetId = "fleet.unknown"; break;
            case "same-side-focus": c.Replacements![0].FocusFleetId = "fleet.federation-scout"; break;
        }
        var before = Json(app.ExportSave()); Assert.False(app.CommitRound(c).Accepted); Assert.Equal(before, Json(app.ExportSave()));
    }
    [Fact]
    public void DuplicateAndRepeatedWaitingCommitmentsCannotResolveTwice()
    {
        var app = Start(); var id = Allocate(app); var c = Command(app, id, A);
        Assert.True(app.CommitRound(c).Accepted); var before = Json(app.ExportSave());
        Assert.Equal(CampaignTacticalError.DuplicateCommand, app.CommitRound(c).Error);
        c.CommandId = "command.other"; Assert.Equal(CampaignTacticalError.AlreadyCommitted, app.CommitRound(c).Error);
        Assert.Equal(before, Json(app.ExportSave()));
    }
    [Fact]
    public void RoundOneCloakUsesActualCapabilityAndNormalFireThenRevealsWithoutRefill()
    {
        var app = Start(); var s = app.CaptureStatus();
        Assert.True(app.SetApproachCloak(A, "command.cloak", "fleet.federation-scout", true, s.Turn, s.CampaignRevision).Accepted);
        var id = Allocate(app);
        var a = app.Observe(id, A); var b = app.Observe(id, B);
        Assert.True(a.OwnShips.Single(x => x.Condition.ShipId == "ship.equipment-cruiser").Cloaked);
        Assert.False(a.OwnShips.Single(x => x.Condition.ShipId == "ship.equipment-surveyor").Cloaked);
        Assert.Equal("ship.equipment-surveyor", Assert.Single(b.VisibleEnemies).ShipId);
        Round(app, id);
        Assert.All(app.Observe(id, A).OwnShips, own => Assert.False(own.Cloaked));
        Assert.True(app.Observe(id, A).OwnShips.Single(x => x.Condition.ShipId == "ship.equipment-cruiser")
            .Weapons.Single(w => w.Class == TacticalWeaponClass.Torpedo).Ammunition < 7);
        Assert.DoesNotContain(app.Observe(id, B).Reports, e => e.ShipId == "ship.equipment-cruiser");
        Assert.Contains(app.Observe(id, B).Reports, e => e.Kind == "IncomingTorpedo");
        Assert.All(app.Observe(id, B).Reports.Where(e => e.OwnWeaponId != null),
            e => Assert.Contains(app.Observe(id, B).OwnShips, ship => ship.Condition.ShipId == e.ShipId && ship.Weapons.Any(w => w.WeaponId == e.OwnWeaponId)));
    }
    [Fact]
    public void FullyMutualCloakSpendsNoOffensiveResourcesOrRngOnMissingTargets()
    {
        var p = Package(); RemoveSurveyor(p); var app = Start(p);
        foreach (var (actor,fleet) in new[] { (A,"fleet.federation-scout"), (B,"fleet.cardassian-scout") })
        { var s = app.CaptureStatus(); Assert.True(app.SetApproachCloak(actor, "command.cloak." + actor, fleet, true, s.Turn, s.CampaignRevision).Accepted); }
        var id = Allocate(app); var before = app.ExportSave().Battles[0].State.RandomState;
        Assert.Empty(app.Observe(id, A).VisibleEnemies); Assert.Empty(app.Observe(id, B).VisibleEnemies);
        Assert.Empty(app.Observe(id,A).Relations);Assert.Empty(app.Observe(id,B).Relations);
        Round(app, id);
        Assert.Equal(before, app.ExportSave().Battles[0].State.RandomState);
        Assert.Equal(7, app.Observe(id, A).OwnShips[0].Weapons.Single(w => w.Class == TacticalWeaponClass.Torpedo).Ammunition);
        Assert.Single(app.Observe(id, A).VisibleEnemies); Assert.Equal(2, app.Observe(id, A).Round);
    }
    private static void RemoveSurveyor(CampaignTacticalReviewPackageDto p)
    {
        var game = p.Campaign!.InitialState; game.Ships!.RemoveAll(s => s.ShipId == "ship.equipment-surveyor");
        game.EntityIdentities.RemoveAll(s => s.EntityId == "ship.equipment-surveyor"); game.Fleets!.Single(f => f.OwnerCivilizationId == A).ShipIds!.Remove("ship.equipment-surveyor");
        p.Campaign.Definitions.AuthoredTacticalEquipment!.InitialShipProvisions!.RemoveAll(s => s.ShipId == "ship.equipment-surveyor");
    }
    [Theory]
    [InlineData("en-US")]
    [InlineData("nl-NL")]
    [InlineData("tr-TR")]
    public void ActualCampaignRoundsCanonicalReverseRepeatedReloadAndCulturesAgree(string culture)
    {
        var previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);
            var p = Package(); var canonical = Start(p); var reverse = Start(p); var repeated = Start(p); var resumed = Start(p);
            var id = Allocate(canonical); Assert.Equal(id, Allocate(reverse)); Assert.Equal(id, Allocate(repeated)); Assert.Equal(id, Allocate(resumed));
            for (var r = 0; r < 3 && !canonical.Observe(id, A).Complete; r++)
            {
                Round(canonical, id, maneuver: r == 1 ? CampaignFleetManeuver.Flank : null);
                Round(reverse, id, true, maneuver: r == 1 ? CampaignFleetManeuver.Flank : null);
                Round(repeated, id, maneuver: r == 1 ? CampaignFleetManeuver.Flank : null);
                Round(resumed, id, reload: true, package: p, replaced: next => resumed = next, maneuver: r == 1 ? CampaignFleetManeuver.Flank : null);
                Assert.Equal(Json(canonical.ExportSave()), Json(reverse.ExportSave()));
                Assert.Equal(Json(canonical.ExportSave()), Json(repeated.ExportSave()));
                Assert.Equal(Json(canonical.ExportSave()), Json(resumed.ExportSave()));
            }
            Assert.True(canonical.Observe(id, A).Complete);
            var snapshot = canonical.CaptureStatus(); Assert.True(canonical.FinalizeBatch(snapshot.Turn, snapshot.CampaignRevision).Accepted);
            Assert.Equal(snapshot.Turn + 1, canonical.CaptureStatus().Turn);
            var retained = Json(canonical.InspectOwnShips(A)); resumed = CampaignTacticalReviewApplication.LoadReviewSave(p, Clone(canonical.ExportSave()));
            Assert.Equal(retained, Json(resumed.InspectOwnShips(A)));
            var nextId = Allocate(resumed); Assert.NotEqual(id, nextId);
            Assert.True(resumed.Observe(nextId, A).OwnShips.SelectMany(s => s.Weapons).Where(w => w.Class == TacticalWeaponClass.Torpedo).All(w => w.Ammunition < 7));
        }
        finally { CultureInfo.CurrentCulture = previous; }
    }
    [Theory]
    [InlineData("pool-missing")]
    [InlineData("pool-duplicate")]
    [InlineData("provenance")]
    [InlineData("fit-revision")]
    [InlineData("ammo-refill")]
    [InlineData("shield-overflow")]
    [InlineData("forged-damage")]
    [InlineData("cross-fleet")]
    [InlineData("exit")]
    [InlineData("foreign-commitment")]
    [InlineData("hidden-round-two")]
    [InlineData("rules")]
    [InlineData("identity")]
    [InlineData("opening-refill")]
    [InlineData("opening-geometry")]
    [InlineData("opening-seed")]
    public void UntrustedSaveCannotChangeSourceHardwareOwnershipGatesOrPools(string defect)
    {
        var p = Package(); var app = Start(p); var id = Allocate(app);
        var saved = app.ExportSave();
        switch (defect)
        {
            case "pool-missing": saved.Pools.RemoveAt(0); break;
            case "pool-duplicate": saved.Pools.Add(saved.Pools[0]); break;
            case "provenance": saved.Pools[0].ProvenanceId = "source.foreign"; break;
            case "fit-revision": saved.Pools[0].FitRevision++; break;
            case "ammo-refill": saved.Pools.First(q => q.Ammunition.Count > 0).Ammunition[0].Stock = 12; break;
            case "shield-overflow": saved.Pools[0].Shields[0].Current = long.MaxValue; break;
            case "forged-damage": saved.Battles[0].State.Formations.SelectMany(f => f.Ships).First(s => s.Weapons.Count > 0).Weapons[0].Damage++; break;
            case "cross-fleet": saved.Battles[0].State.Formations[0].StandingOrder.ProtectedFormationId = "fleet.federation-scout"; break;
            case "exit": saved.Battles[0].State.Formations[0].StandingOrder.Maneuver = TacticalManeuver.Disengage; break;
            case "foreign-commitment": saved.Battles[0].Commitments.Add(new CampaignTacticalCommitmentDto { ActorId = A,
                Orders = new List<CampaignTacticalFleetOrderDto> { new() { FleetId = "fleet.cardassian-scout", Maneuver = CampaignFleetManeuver.Hold } } }); break;
            case "hidden-round-two":
                Round(app, id); saved = app.ExportSave(); saved.Battles[0].HiddenShipIds.Add("ship.equipment-cruiser"); break;
            case "rules": saved.Battles[0].State.Rules.ArmorNumerator++; break;
            case "identity": saved.ResolverId = TacticalCombatV2Identity.ResolverId; break;
            case "opening-refill": saved.Battles[0].State.Formations[0].Ships[0].Shields[0].Current++; break;
            case "opening-geometry": saved.Battles[0].State.Geometry[0].Range = TacticalRange.Close; break;
            case "opening-seed": saved.Battles[0].State.RandomState++; break;
        }
        var before = Json(app.ExportSave()); Assert.Throws<ArgumentException>(() => CampaignTacticalReviewApplication.LoadReviewSave(p, Clone(saved)));
        Assert.Equal(before, Json(app.ExportSave()));
    }
    [Theory]
    [InlineData("activation")]
    [InlineData("missing-power")]
    [InlineData("power-budget")]
    [InlineData("capability")]
    [InlineData("format")]
    [InlineData("profile")]
    [InlineData("unbound-legacy")]
    public void PackageGatesAndConcurrentCloakAreValidatedBeforeProvisioning(string defect)
    {
        var p = Package();
        switch (defect)
        {
            case "activation": p.DefaultActivationAllowed = true; break;
            case "missing-power": p.CloakSources![0].ConcurrentPowerRequired = null; break;
            case "power-budget": p.CloakSources![0].ConcurrentPowerRequired = long.MaxValue; break;
            case "capability": p.CloakSources![0].Capable = null; break;
            case "format": p.FormatVersion++; break;
            case "profile": p.ProfileId = "approved"; break;
            case "unbound-legacy": p.Campaign!.Definitions.AuthoredTacticalEquipment!.InitialShipProvisions!.RemoveAt(0); break;
        }
        Assert.Throws<ArgumentException>(() => Start(p));
    }
    [Fact]
    public void BaselineAiIsPrivateAndUsesTheSameLawfulOrderValidator()
    {
        var p = Package(); var card = p.Campaign!.InitialState.Civilizations!.Single(c => c.CivilizationId == B);
        card.ControllerKind = ControllerKind.AI; card.Credits = 0;
        var app = Start(p); var id = Allocate(app);
        Assert.Throws<ArgumentException>(() => app.Observe(id, B));
        Assert.Contains(app.ExportSave().Battles[0].Commitments, c => c.ActorId == B && c.Orders.All(o => o.Maneuver == CampaignFleetManeuver.Hold));
        Assert.True(app.CommitRound(Command(app, id, A, CampaignFleetManeuver.Flank)).Accepted);
        Assert.Equal(2, app.Observe(id, A).Round);
        Assert.Equal(CampaignTacticalError.ActorNotControlled, app.CommitRound(new CommitCampaignTacticalRoundDto { ActorId = B }).Error);
    }
    [Fact]
    public void NoResultSubmissionOrPrototypeImportAndPrivacyShapesAreImmutable()
    {
        Assert.DoesNotContain(typeof(ICampaignTacticalReviewSession).GetMethods(), m => m.GetParameters().Any(p => p.ParameterType == typeof(TacticalEncounterV2Dto)
            || p.ParameterType == typeof(TacticalRoundV2Result) || p.ParameterType == typeof(CombatResultDto)));
        Assert.All(typeof(CampaignTacticalCondition).GetProperties(), p => Assert.False(p.CanWrite));
        Assert.DoesNotContain(typeof(CampaignTacticalCondition).GetProperties(), p => p.Name.Contains("Ammo") || p.Name.Contains("Order") || p.Name.Contains("Fit"));
        Assert.DoesNotContain(typeof(CampaignTacticalFleetOrderDto).GetProperties(), p => p.Name.Contains("Protect") || p.Name.Contains("Ship"));
        Assert.DoesNotContain(typeof(CampaignTacticalReviewSession).Assembly.GetReferencedAssemblies(), r => r.Name == "HobbyDS9.CampaignTacticalV2.Prototypes");
        Assert.DoesNotContain(typeof(ICampaignTacticalReviewSession).GetMethods(),m=>m.ReturnType==typeof(SessionSnapshot));
        var app=Start();Assert.All(app.GetOwnStrategicFleets(A),f=>Assert.Equal(A,f.OwnerCivilizationId.Value));
    }
    [Theory]
    [InlineData(true, 2)]
    [InlineData(false, 0)]
    public void ActualInstalledBeamCanScreenOnlyWithinItsStrategicFleet(bool sameFleet, int attempts)
    {
        var p = Package(); var g = p.Campaign!.InitialState;
        g.Ships!.Single(s => s.ShipId == "ship.equipment-surveyor").CurrentHull = 50;
        if (!sameFleet)
        {
            var fleet = Clone(g.Fleets!.Single(f => f.OwnerCivilizationId == A));
            fleet.FleetId = "fleet.separate-escort"; fleet.ShipIds = new List<string> { "ship.equipment-cruiser" }; g.Fleets!.Add(fleet);
            g.Fleets!.Single(f => f.FleetId == "fleet.federation-scout").ShipIds!.Remove("ship.equipment-cruiser");
            g.Ships!.Single(s => s.ShipId == "ship.equipment-cruiser").FleetId = fleet.FleetId;
            var identity = Clone(g.EntityIdentities.Single(e => e.EntityId == "fleet.federation-scout"));
            identity.EntityId = fleet.FleetId; g.EntityIdentities.Add(identity);
        }
        var app = Start(p); var id = Allocate(app);
        var a = Command(app, id, A, CampaignFleetManeuver.Screen);
        var b = Command(app, id, B, CampaignFleetManeuver.Hold); b.Replacements![0].FocusFleetId = "fleet.federation-scout";
        Assert.True(app.CommitRound(a).Accepted); Assert.True(app.CommitRound(b).Accepted);
        Assert.Equal(attempts, app.Observe(id, A).Reports.Count(r => r.Kind == "InterceptionAttempt" && r.ShipId == "ship.equipment-cruiser"));
        if (!sameFleet) Assert.Equal(2, app.Observe(id, A).StandingOrders.Count);
    }
    [Fact]
    public void ANewHiddenFocusAndLoadedHiddenOpeningCannotBypassAcquisition()
    {
        var p = Package(); var app = Start(p); var s = app.CaptureStatus();
        Assert.True(app.SetApproachCloak(B, "command.card-cloak", "fleet.cardassian-scout", true, s.Turn, s.CampaignRevision).Accepted);
        var id = Allocate(app); var command = Command(app, id, A, CampaignFleetManeuver.Hold);
        command.Replacements![0].FocusFleetId = "fleet.cardassian-scout";
        var before = Json(app.ExportSave()); Assert.False(app.CommitRound(command).Accepted); Assert.Equal(before, Json(app.ExportSave()));
        var forged = app.ExportSave(); forged.Battles[0].State.Formations.Single(f => f.SideId == A).StandingOrder.TargetFormationId = "fleet.cardassian-scout";
        Assert.Throws<ArgumentException>(() => CampaignTacticalReviewApplication.LoadReviewSave(p, forged));
    }
    [Fact]
    public void AllAiRoundsResolvePrivatelyWithoutHumanCommands()
    {
        var p = Package();
        foreach (var c in p.Campaign!.InitialState.Civilizations!) { c.ControllerKind = ControllerKind.AI; c.Credits = 0; }
        var app = Start(p); var s = app.CaptureStatus(); Assert.True(app.AdvanceTurn(s.Turn, s.CampaignRevision).Accepted);
        Assert.All(app.ExportSave().Battles, b => Assert.NotEqual(TacticalCompletion.None, b.State.Completion));
        Assert.Throws<ArgumentException>(() => app.GetEncounters(A));
        s = app.CaptureStatus(); Assert.True(app.FinalizeBatch(s.Turn, s.CampaignRevision).Accepted);
        Assert.Equal(SessionPhase.Planning, app.CaptureStatus().Phase);
    }
    [Fact]
    public void PersistedReportCannotNameHiddenRoundOneOpponentOrDiscloseItsPackage()
    {
        var p = Package(); var app = Start(p); var s = app.CaptureStatus();
        Assert.True(app.SetApproachCloak(A, "command.hide", "fleet.federation-scout", true, s.Turn, s.CampaignRevision).Accepted);
        var id = Allocate(app); Round(app,id);
        var save = app.ExportSave(); save.Battles[0].Reports.Add(new CampaignTacticalActorReportDto {
            ActorId = B, Round = 1, Kind = "WeaponDischarged", ShipId = "ship.equipment-cruiser", OwnWeaponId = "weapon.torpedo" });
        Assert.Throws<ArgumentException>(() => CampaignTacticalReviewApplication.LoadReviewSave(p, save));
    }
    [Fact]
    public void LaterAtomicAftermathFailurePreservesAllCampaignPoolsAndStagedRounds()
    {
        var p = Package(); var g = p.Campaign!.InitialState; var catalogue = p.Campaign.Definitions.AuthoredTacticalEquipment!;
        foreach (var (actor, originalFleet, originalShip) in new[] { (A,"fleet.federation-scout","ship.equipment-surveyor"), (B,"fleet.cardassian-scout","ship.cardassian-scout") })
        {
            var ship = Clone(g.Ships!.Single(s => s.ShipId == originalShip)); ship.ShipId += ".second"; ship.FleetId = originalFleet + ".second"; g.Ships!.Add(ship);
            var fleet = Clone(g.Fleets!.Single(f => f.FleetId == originalFleet)); fleet.FleetId = ship.FleetId;
            fleet.LocationNodeId = "nav.cardassia"; fleet.ShipIds = new List<string> { ship.ShipId }; g.Fleets!.Add(fleet);
            foreach (var (oldId,newId) in new[] { (originalFleet,fleet.FleetId), (originalShip,ship.ShipId) })
            { var identity = Clone(g.EntityIdentities.Single(e => e.EntityId == oldId)); identity.EntityId = newId; g.EntityIdentities.Add(identity); }
            var provision = Clone(catalogue.InitialShipProvisions!.Single(a => a.ShipId == originalShip)); provision.ShipId = ship.ShipId; catalogue.InitialShipProvisions!.Add(provision);
        }
        var sim = CampaignTacticalReviewSession.Start(p); var initial = sim.CaptureStatus();
        Assert.True(sim.AdvanceTurn(initial.Turn, initial.CampaignRevision).Accepted); Assert.Equal(2, sim.Encounters(A).Count);
        foreach (var id in sim.Encounters(A).Reverse())
            while (!sim.Observe(id,A).Complete)
                foreach (var actor in new[]{A,B})
                {
                    var view = sim.Observe(id,actor);
                    Assert.True(sim.CommitRound(new CommitCampaignTacticalRoundDto { ActorId=actor,CommandId="command."+actor+"."+id+"."+view.Round,
                        EncounterId=id,ExpectedTurn=view.Turn,ExpectedRound=view.Round,ExpectedBoundaryRevision=view.BoundaryRevision,
                        Replacements=new List<CampaignTacticalFleetOrderDto>() }).Accepted);
                }
        var before = Json(sim.ExportSave()); var current = sim.CaptureStatus();
        sim.FinalizationProbe = index => { if (index == 1) throw new InvalidOperationException("Late staged failure."); };
        Assert.Equal(CampaignTacticalError.InvalidBatch,sim.FinalizeBatch(current.Turn,current.CampaignRevision).Error);
        Assert.Equal(before,Json(sim.ExportSave()));
        sim.FinalizationProbe = null; Assert.True(sim.FinalizeBatch(current.Turn,current.CampaignRevision).Accepted);
    }
    [Fact]
    public void ActualShipLocalFlankAccessSurvivesASlowerSharedFleetMedian()
    {
        var p = Package(); var catalogue = p.Campaign!.Definitions.AuthoredTacticalEquipment!;
        var fit = Clone(catalogue.Fits!.Single(f => f.DefinitionId == "fit.provisional-cruiser"));
        fit.DefinitionId = "fit.fast-escort"; fit.ProvenanceId = "source.fast-escort"; fit.Capabilities!.Speed = 90; fit.Capabilities.Agility = 80;
        foreach (var weapon in fit.Weapons!) weapon.AccuracyBps = 10000;
        catalogue.Fits!.Add(fit); p.Campaign.Definitions.DefinitionIds.Add(fit.DefinitionId);
        catalogue.Fits!.Single(f => f.DefinitionId == "fit.provisional-cruiser").EvasionBps = 0;
        catalogue.InitialShipProvisions!.Single(s => s.ShipId == "ship.equipment-cruiser").FitDefinitionId = fit.DefinitionId;
        p.CloakSources!.Add(new CampaignTacticalCloakSourceDto { FitId = fit.DefinitionId, FitRevision = 1, Capable = true, ConcurrentPowerRequired = 6 });
        p.Rules!.DistantTorpedoHitPenaltyBps = 0;
        var app = Start(p); var id = Allocate(app); Round(app,id,maneuver:CampaignFleetManeuver.Flank);
        var enemy = app.Observe(id,A).VisibleEnemies.Single(s => s.ShipId == "ship.cardassian-scout");
        Assert.Equal(40,enemy.Shields[0]); Assert.True(enemy.Shields[2] < 25);
        Assert.Single(app.Observe(id,A).StandingOrders);
        Assert.Equal(TacticalSector.Fore,app.ExportSave().Battles[0].State.Geometry[0].FacingA);
    }
    [Fact]
    public void ActualFocusDestroyedFallsBackToVisibleHostileWhileStandingIntentPersists()
    {
        var p = Package(); var g = p.Campaign!.InitialState; var catalogue = p.Campaign.Definitions.AuthoredTacticalEquipment!;
        var ship = Clone(g.Ships!.Single(s => s.ShipId == "ship.cardassian-scout"));
        ship.ShipId = "ship.additional-opponent"; ship.OwnerCivilizationId = "civ.dominion"; ship.FleetId = "fleet.additional-opponent"; g.Ships!.Add(ship);
        var fleet = Clone(g.Fleets!.Single(f => f.OwnerCivilizationId == B));
        fleet.FleetId = ship.FleetId; fleet.OwnerCivilizationId = ship.OwnerCivilizationId; fleet.ShipIds = new List<string> { ship.ShipId }; g.Fleets!.Add(fleet);
        foreach (var (oldId,newId) in new[] { ("ship.cardassian-scout",ship.ShipId),("fleet.cardassian-scout",fleet.FleetId) })
        { var identity = Clone(g.EntityIdentities.Single(e => e.EntityId == oldId)); identity.EntityId = newId; g.EntityIdentities.Add(identity); }
        var provision = Clone(catalogue.InitialShipProvisions!.Single(s => s.ShipId == "ship.cardassian-scout"));
        provision.ShipId = ship.ShipId; provision.OwnerCivilizationId = ship.OwnerCivilizationId; catalogue.InitialShipProvisions!.Add(provision);
        g.Ships!.Single(s => s.ShipId == "ship.cardassian-scout").CurrentHull = 1;
        foreach (var fit in catalogue.Fits!) { fit.EvasionBps = 0; foreach (var w in fit.Weapons!) { w.AccuracyBps=10000; w.CadenceRounds=1; } }
        p.Rules!.DistantTorpedoHitPenaltyBps=0;p.Rules.HoldDefenseBps=0;
        var app=Start(p);var id=Allocate(app);
        var a=Command(app,id,A,CampaignFleetManeuver.Hold);a.Replacements![0].FocusFleetId="fleet.cardassian-scout";
        Assert.True(app.CommitRound(a).Accepted);Assert.True(app.CommitRound(Command(app,id,B)).Accepted);
        Assert.True(app.CommitRound(Command(app,id,"civ.dominion")).Accepted);
        Assert.DoesNotContain(app.Observe(id,A).VisibleEnemies,s=>s.ShipId=="ship.cardassian-scout");
        Assert.True(app.CommitRound(Command(app,id,A)).Accepted);
        Assert.True(app.CommitRound(Command(app,id,"civ.dominion")).Accepted);
        Assert.Contains(app.Observe(id,A).Reports,r=>r.Kind=="FocusFallback" && r.TargetShipId=="ship.additional-opponent");
        Assert.Equal("fleet.cardassian-scout",app.Observe(id,A).StandingOrders[0].FocusFleetId);
    }
    [Fact]
    public void FinalizationRejectsIncompleteStaleAndRepeatedBatchesWithoutMutation()
    {
        var app = Start(); var id = Allocate(app); var initial = app.CaptureStatus(); var before = Json(app.ExportSave());
        Assert.False(app.FinalizeBatch(initial.Turn, initial.CampaignRevision).Accepted); Assert.Equal(before, Json(app.ExportSave()));
        for (var round = 0; round < 3 && !app.Observe(id, A).Complete; round++) Round(app, id);
        var complete = app.CaptureStatus(); var staged = Json(app.ExportSave());
        Assert.False(app.FinalizeBatch(complete.Turn, complete.CampaignRevision - 1).Accepted); Assert.Equal(staged, Json(app.ExportSave()));
        Assert.True(app.FinalizeBatch(complete.Turn, complete.CampaignRevision).Accepted);
        var finalized = Json(app.ExportSave()); Assert.False(app.FinalizeBatch(complete.Turn, complete.CampaignRevision).Accepted);
        Assert.Equal(finalized, Json(app.ExportSave())); Assert.All(app.GetOwnStrategicFleets(A), f => Assert.Empty(f.RouteNodeIds));
    }
    [Fact]
    public void OwnWaitingReceiptReloadKeepsAcceptedDetailsWhileOppositeActorSeesNone()
    {
        var p=Package();var app=Start(p);var id=Allocate(app);
        var c=Command(app,id,A,CampaignFleetManeuver.Flank);c.Replacements![0].FocusFleetId="fleet.cardassian-scout";
        Assert.True(app.CommitRound(c).Accepted);
        app=CampaignTacticalReviewApplication.LoadReviewSave(p,Clone(app.ExportSave()));
        var own=app.Observe(id,A);var opposite=app.Observe(id,B);
        Assert.Equal(c.CommandId,own.OwnAcceptedCommandId);
        Assert.Equal(CampaignFleetManeuver.Flank,Assert.Single(own.OwnCommittedOrders).Maneuver);
        Assert.Equal("fleet.cardassian-scout",own.OwnCommittedOrders[0].FocusFleetId);
        Assert.Empty(opposite.OwnCommittedOrders);Assert.Null(opposite.OwnAcceptedCommandId);
        Assert.Equal(CampaignFleetManeuver.Hold,own.StandingOrders[0].Maneuver);
        Assert.Equal(5,own.LegalManeuvers.Count);Assert.Equal(new[]{"fleet.cardassian-scout"},own.LegalFocusFleetIds);
        Assert.NotEqual(own.BoundaryRevision,app.CaptureStatus().CampaignRevision);
    }
    [Fact]
    public void ActorLocalReportSequenceIsDenseStableOnReplayAndSavedReload()
    {
        var p=Package();var app=Start(p);var id=Allocate(app);Round(app,id);
        var original=Json(app.Observe(id,A).Reports);
        var rows=app.Observe(id,A).Reports;
        Assert.Equal(Enumerable.Range(0,rows.Count),rows.Select(r=>r.Sequence));
        for(var i=0;i<10;i++) Assert.Equal(original,Json(app.Observe(id,A).Reports));
        app=CampaignTacticalReviewApplication.LoadReviewSave(p,Clone(app.ExportSave()));
        Assert.Equal(original,Json(app.Observe(id,A).Reports));
        var invalid=app.ExportSave();invalid.Battles[0].Reports.First(r=>r.ActorId==A).Sequence++;
        Assert.Throws<ArgumentException>(()=>CampaignTacticalReviewApplication.LoadReviewSave(p,invalid));
    }
    [Fact]
    public void ActualBatchStatusAndPersistentCloakSettingDoNotClaimReviewedTravel()
    {
        var p=Package();var app=Start(p);var initial=app.CaptureStatus();
        Assert.False(initial.CanFinalizeBatch);Assert.False(initial.ProductionActivationAllowed);
        Assert.Contains("pending",initial.TravelModel);
        var setting=Assert.Single(app.GetFleetCloakSettings(A));
        Assert.Equal(new[]{"ship.equipment-cruiser"},setting.CapableShipIds);
        Assert.True(app.SetApproachCloak(A,"command.persist-cloak",setting.FleetId,true,initial.Turn,initial.CampaignRevision).Accepted);
        var id=Allocate(app);Assert.False(app.GetFleetCloakSettings(A)[0].CanChange);
        while(!app.Observe(id,A).Complete) Round(app,id);
        Assert.True(app.CaptureStatus().CanFinalizeBatch);
        var status=app.CaptureStatus();Assert.True(app.FinalizeBatch(status.Turn,status.CampaignRevision).Accepted);
        app=CampaignTacticalReviewApplication.LoadReviewSave(p,Clone(app.ExportSave()));
        Assert.True(app.GetFleetCloakSettings(A)[0].Enabled);Assert.True(app.GetFleetCloakSettings(A)[0].CanChange);
    }
    [Theory]
    [InlineData("hull")]
    [InlineData("rng")]
    [InlineData("shield")]
    [InlineData("missing-history")]
    [InlineData("orders")]
    [InlineData("controller")]
    public void ForgedInBoundsRoundResultCannotReplaceReplayedCampaignAuthority(string defect)
    {
        var p=Package();var app=Start(p);var id=Allocate(app);Round(app,id);
        var saved=app.ExportSave();var b=saved.Battles[0];
        switch(defect)
        {
            case "hull":b.State.Formations[0].Ships[0].Hull--;break;
            case "rng":b.State.RandomState++;break;
            case "shield":b.State.Formations[0].Ships[0].Shields[0].Current--;break;
            case "missing-history":b.ResolvedRounds.Clear();break;
            case "orders":b.ResolvedRounds[0].Commitments[0].Orders[0].Maneuver=CampaignFleetManeuver.Charge;break;
            case "controller":saved.Campaign.GameState.Civilizations!.Single(c=>c.CivilizationId==B).ControllerKind=ControllerKind.AI;break;
        }
        var unchanged=Json(app.ExportSave());
        Assert.Throws<ArgumentException>(()=>CampaignTacticalReviewApplication.LoadReviewSave(p,saved));
        Assert.Equal(unchanged,Json(app.ExportSave()));
    }
    [Fact]
    public void NonsemanticSavedSourceAndStateListsMayBeReorderedWithoutChangingAuthority()
    {
        var p=Package();var app=Start(p);var id=Allocate(app);Round(app,id);
        var original=Json(app.ExportSave());var save=app.ExportSave();
        save.Pools.Reverse();save.FleetSettings.Reverse();save.Campaign.GameState.Ships!.Reverse();
        foreach(var pool in save.Pools){pool.Shields.Reverse();pool.Ammunition.Reverse();}
        foreach(var b in save.Battles)
        {
            b.State.Formations.Reverse();b.State.Geometry.Reverse();
            foreach(var f in b.State.Formations){f.Ships.Reverse();foreach(var ship in f.Ships){ship.Shields.Reverse();ship.Mounts.Reverse();ship.Weapons.Reverse();}}
        }
        Assert.Equal(original,Json(CampaignTacticalReviewApplication.LoadReviewSave(p,save).ExportSave()));
    }
    [Fact]
    public void OwnFullInspectionUsesCurrentPackagesAndExplicitConcurrentPower()
    {
        var app=Start();var status=app.CaptureStatus();
        Assert.True(app.SetApproachCloak(A,"command.inspect-cloak","fleet.federation-scout",true,status.Turn,status.CampaignRevision).Accepted);
        var id=Allocate(app);var own=app.Observe(id,A).OwnShips.Single(s=>s.Condition.ShipId=="ship.equipment-cruiser");
        Assert.Equal(80,own.EffectivePower);Assert.Equal(6,own.CloakPowerReserved);Assert.Equal(48,own.EquipmentPowerBudget);
        Assert.Equal(new long[]{40,25,25,20},own.ShieldMaximum);
        Assert.Equal(70,own.Capabilities.Speed);Assert.Equal(10,own.Armor);
        var torpedo=own.Weapons.Single(w=>w.Class==TacticalWeaponClass.Torpedo);
        Assert.Equal("equipment.torpedo.v1",torpedo.PackageVersionId);Assert.Equal(40,torpedo.Damage);Assert.Equal(2,torpedo.CadenceRounds);
        Assert.Equal(7,torpedo.Ammunition);Assert.True(torpedo.Powered);Assert.Equal(4,own.EssentialReservations.Count);
    }
    [Fact]
    public void ThousandActualAuthoredShipsRemainTwoCommandUnitsAndRoundReloadIsPortable()
    {
        var p=Package();var game=p.Campaign!.InitialState;var ships=game.Ships!;var fleets=game.Fleets!;
        var catalogue=p.Campaign.Definitions.AuthoredTacticalEquipment!;
        var template=Clone(ships.Single(s=>s.ShipId=="ship.equipment-surveyor"));
        var provision=Clone(catalogue.InitialShipProvisions!.Single(s=>s.ShipId==template.ShipId));
        var header=Clone(game.EntityIdentities.Single(e=>e.EntityId==template.ShipId));
        var oldIds=ships.Select(s=>s.ShipId).ToHashSet(StringComparer.Ordinal);
        game.EntityIdentities.RemoveAll(e=>oldIds.Contains(e.EntityId));ships.Clear();catalogue.InitialShipProvisions!.Clear();
        foreach(var fleet in fleets)fleet.ShipIds!.Clear();
        for(var i=0;i<1000;i++)
        {
            var owner=i%2==0?A:B;var fleet=fleets.Single(f=>f.OwnerCivilizationId==owner);
            var ship=Clone(template);ship.ShipId="ship.stress."+i.ToString("D4",CultureInfo.InvariantCulture);ship.OwnerCivilizationId=owner;ship.FleetId=fleet.FleetId;
            ships.Add(ship);fleet.ShipIds!.Add(ship.ShipId);
            var identity=Clone(header);identity.EntityId=ship.ShipId;game.EntityIdentities.Add(identity);
            var source=Clone(provision);source.ShipId=ship.ShipId;source.OwnerCivilizationId=owner;catalogue.InitialShipProvisions.Add(source);
        }
        var app=Start(p);var id=Allocate(app);Assert.Single(app.Observe(id,A).StandingOrders);
        Assert.Equal(500,app.Observe(id,A).OwnShips.Count);Round(app,id);
        var before=Json(app.ExportSave());app=CampaignTacticalReviewApplication.LoadReviewSave(p,Clone(app.ExportSave()));
        Assert.Equal(before,Json(app.ExportSave()));Assert.Equal(1000,app.ExportSave().Pools.Count);
    }
}
