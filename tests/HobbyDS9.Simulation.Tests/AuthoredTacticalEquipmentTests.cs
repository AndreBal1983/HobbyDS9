using System.Collections;
using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using HobbyDS9.Application;
using HobbyDS9.Contracts;
using HobbyDS9.Contracts.ShipDesign;
using HobbyDS9.Simulation;
using Xunit;

namespace HobbyDS9.Simulation.Tests;

public sealed class AuthoredTacticalEquipmentTests
{
    private const string Actor = "civ.federation", Cruiser = "ship.equipment-cruiser";
    private static ScenarioDto Scenario(string package = "tactical-v2-equipment-source")
    {
        var bytes = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "scenarios", package, "scenario.json"));
        var scenario = JsonSerializer.Deserialize<ScenarioDto>(bytes)!;
        scenario.ContentHash = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
        return scenario;
    }
    private static AuthoredTacticalEquipmentCatalogueDto Catalogue(ScenarioDto scenario) => scenario.Definitions.AuthoredTacticalEquipment!;
    private static AuthoredTacticalFitDto Fit(ScenarioDto scenario) => Catalogue(scenario).Fits!.Single(f => f.DefinitionId == "fit.provisional-cruiser");
    private static AuthoredTacticalShipProvisionDto Provision(ScenarioDto scenario) => Catalogue(scenario).InitialShipProvisions!.Single(p => p.ShipId == Cruiser);
    private static CampaignTacticalV2EligibilitySnapshot Inspect(ICampaignSession app, string actor = Actor)
        => ((ICampaignTacticalV2Inspection)app).InspectCampaignTacticalV2(actor);
    private static AuthoredTacticalEquipmentSnapshot Equipment(ICampaignSession app) => Inspect(app).OwnedShips.Single(s => s.Ship.ShipId.Value == Cruiser).AuthoredEquipment!;
    private static string Json<T>(T value) => JsonSerializer.Serialize(value);
    private static T Clone<T>(T value) => JsonSerializer.Deserialize<T>(Json(value))!;

    [Fact]
    public void RealScenarioSessionShowsCompleteAuthoredAndIneligibleLegacySourcesWithoutActivation()
    {
        var scenario = Scenario(); Assert.Empty(CampaignApplication.ValidateScenario(scenario));
        var app = CampaignApplication.StartScenario(scenario); var before = Json(app.ExportSave()); var inspection = Inspect(app);
        Assert.False(inspection.CanActivate); Assert.Equal(CampaignCombatRules.ResolverId, inspection.CampaignResolverId);
        Assert.Equal(3, inspection.OwnedShips.Count);
        Assert.Equal(2, inspection.OwnedShips.Count(s => s.CanProjectTacticalV2));
        Assert.All(inspection.OwnedShips.Where(s => s.CanProjectTacticalV2), s =>
        { Assert.Empty(s.MissingFitSemantics); Assert.Equal(CampaignTacticalSourceKind.AuthoredEquipmentV2, s.SourceKind); Assert.Null(s.FrozenBlueprint); });
        Assert.Equal(CampaignTacticalSourceKind.LegacyStatic, inspection.OwnedShips.Single(s => s.Ship.ShipId.Value == "ship.federation-scout").SourceKind);
        Assert.Contains(CampaignTacticalV2Blocker.SessionRoundCheckpointAndOrderAuthorityRequired, inspection.Blockers);
        Assert.Contains(CampaignTacticalV2Blocker.PersistentShipResourcesAndRecoveryPolicyRequired, inspection.Blockers);
        Assert.Contains(CampaignTacticalV2Blocker.FormationCapabilityDerivationRequired, inspection.Blockers);
        Assert.DoesNotContain(inspection.OwnedShips, s => s.Ship.OwnerCivilizationId.Value != Actor);
        Assert.Equal(before, Json(app.ExportSave()));
    }

    [Fact]
    public void CompleteOnlyOwnedShipsRemoveEquipmentGapsButRetainNamedLifecycleGates()
    {
        var scenario = Scenario(); scenario.InitialState.Ships!.RemoveAll(s => s.ShipId == "ship.federation-scout");
        scenario.InitialState.EntityIdentities.RemoveAll(i => i.EntityId == "ship.federation-scout");
        scenario.InitialState.Fleets!.Single(f => f.OwnerCivilizationId == Actor).ShipIds!.Remove("ship.federation-scout");
        var report = Inspect(CampaignApplication.StartScenario(scenario)); Assert.False(report.CanActivate);
        Assert.DoesNotContain(CampaignTacticalV2Blocker.ExplicitEquipmentSchemaRequired, report.Blockers);
        Assert.DoesNotContain(CampaignTacticalV2Blocker.ArmorDefinitionRequired, report.Blockers);
        Assert.Equal(6, report.Blockers.Count); Assert.All(report.OwnedShips, s => Assert.True(s.CanProjectTacticalV2));
    }

    [Fact]
    public void EssentialReservationsReduceTheCombatBudgetBeforeWeaponAndRechargeAllocations()
    {
        var source = Equipment(CampaignApplication.StartScenario(Scenario()));
        Assert.Equal(100, source.ReactorOutput); Assert.Equal(80, source.DistributionLimit);
        Assert.Equal(26, source.EssentialPowerReserved); Assert.Equal(54, source.EquipmentPowerBudget);
        Assert.Equal(36, source.EquipmentPowerAllocated); Assert.Equal(4, source.ShieldRechargePerRound);
        Assert.Equal(new long[] { 4, 4, 12, 6 }, source.EssentialReservations.Select(r => r.AllocatedPower));
        Assert.Equal(7, source.Weapons.Single(w => w.Class == TacticalWeaponClass.Torpedo).InitialAmmunition);
        Assert.Equal(new long[] { 18, 25, 12, 0 }, source.InitialShields.Select(s => s.InitialCurrent));
        Assert.Equal(new long[] { 40, 25, 25, 20 }, source.InitialShields.Select(s => s.Maximum));
    }

    private static object Target(ScenarioDto scenario, string target) => target switch
    {
        "catalogue" => Catalogue(scenario), "ammo-definition" => Catalogue(scenario).AmmunitionDefinitions![0],
        "fit" => Fit(scenario), "capabilities" => Fit(scenario).Capabilities!,
        "reservation" => Fit(scenario).EssentialReservations![0], "limit" => Fit(scenario).ShieldLimits![0],
        "mount" => Fit(scenario).Mounts![0], "weapon" => Fit(scenario).Weapons![0],
        "provision" => Provision(scenario), "shield-stock" => Provision(scenario).InitialShields![0],
        "ammo-stock" => Provision(scenario).InitialAmmunition![0], _ => throw new ArgumentException(target)
    };
    public static IEnumerable<object[]> RequiredFields()
    {
        var scenario = Scenario();
        foreach (var target in new[] { "catalogue", "ammo-definition", "fit", "capabilities", "reservation", "limit", "mount", "weapon", "provision", "shield-stock", "ammo-stock" })
            foreach (var property in Target(scenario, target).GetType().GetProperties())
                yield return new object[] { target, property.Name };
    }

    [Theory]
    [MemberData(nameof(RequiredFields))]
    public void OmittedRequiredSourceFieldCannotBecomeZeroOrAnEmptyFit(string target, string property)
    {
        var scenario = Scenario(); var value = Target(scenario, target); value.GetType().GetProperty(property)!.SetValue(value, null);
        var before = Json(scenario); Assert.NotEmpty(CampaignApplication.ValidateScenario(scenario));
        Assert.Throws<ArgumentException>(() => CampaignApplication.StartScenario(scenario)); Assert.Equal(before, Json(scenario));
    }

    [Fact]
    public void ExplicitUnarmedSourceIsCompleteAndCarriesNoInventedWeaponsOrAmmunition()
    {
        var source = Inspect(CampaignApplication.StartScenario(Scenario())).OwnedShips.Single(s => s.Ship.ShipId.Value == "ship.equipment-surveyor");
        Assert.True(source.CanProjectTacticalV2); var equipment = source.AuthoredEquipment!;
        Assert.Empty(equipment.Weapons); Assert.Empty(equipment.Mounts); Assert.Equal(0, equipment.Armor);
        Assert.Equal(0, equipment.EssentialReservations.Single(r => r.System == TacticalEssentialSystem.Targeting).AllocatedPower);
    }

    [Fact]
    public void ExplicitZeroStockDamageRatingsAndShieldLimitsAreNotMissingValues()
    {
        var scenario = Scenario(); var fit = Fit(scenario); var provision = Provision(scenario);
        fit.Armor = 0; fit.EvasionBps = 0; fit.Capabilities = new() { Speed = 0, Agility = 0, Sensors = 0, Command = 0, Cohesion = 0 };
        fit.ShieldLimits!.ForEach(s => s.Maximum = 0); fit.ShieldRechargeRequiredPower = 0; fit.ShieldRechargeAllocatedPower = 0; fit.ShieldRechargePerRound = 0;
        fit.Weapons!.ForEach(w => { w.Damage = 0; w.AccuracyBps = 0; w.TrackingBps = 0; w.MagazineCapacity = 0; w.AllocatedPower = 0; });
        foreach (var p in Catalogue(scenario).InitialShipProvisions!.Where(p => p.FitDefinitionId == fit.DefinitionId))
        { p.InitialShields!.ForEach(s => s.Current = 0); p.InitialAmmunition!.ForEach(a => a.Stock = 0); }
        Assert.Empty(CampaignApplication.ValidateScenario(scenario)); var source = Equipment(CampaignApplication.StartScenario(scenario));
        Assert.All(source.Weapons, w => { Assert.Equal(0, w.Damage); Assert.False(w.Powered); }); Assert.Equal(0, source.EquipmentPowerAllocated);
    }

    [Theory]
    [InlineData("negative-armor")]
    [InlineData("capability-over-cap")]
    [InlineData("unknown-role")]
    [InlineData("unknown-weapon-class")]
    [InlineData("unknown-arc")]
    [InlineData("empty-arc")]
    [InlineData("accuracy-over-cap")]
    [InlineData("negative-tracking")]
    [InlineData("zero-opportunities")]
    [InlineData("opportunities-over-cap")]
    [InlineData("zero-cadence")]
    [InlineData("cadence-over-cap")]
    [InlineData("invalid-fire-round")]
    [InlineData("stock-over-cap")]
    [InlineData("negative-stock")]
    [InlineData("sector-over-cap")]
    [InlineData("negative-sector")]
    [InlineData("energy-magazine")]
    [InlineData("unknown-ammunition")]
    public void InvalidEquipmentAndInitialStocksRejectTheActualScenario(string invalid)
    {
        var scenario = Scenario(); var f = Fit(scenario); var w = f.Weapons![0]; var p = Provision(scenario);
        switch (invalid)
        {
            case "negative-armor": f.Armor = -1; break;
            case "capability-over-cap": f.Capabilities!.Speed = 101; break;
            case "unknown-role": f.Role = (TacticalShipRole)999; break;
            case "unknown-weapon-class": w.Class = (TacticalWeaponClass)999; break;
            case "unknown-arc": w.Coverage = (TacticalArc)32; break;
            case "empty-arc": w.Coverage = TacticalArc.None; break;
            case "accuracy-over-cap": w.AccuracyBps = 10001; break;
            case "negative-tracking": w.TrackingBps = -1; break;
            case "zero-opportunities": w.OpportunitiesPerRound = 0; break;
            case "opportunities-over-cap": w.OpportunitiesPerRound = 65; break;
            case "zero-cadence": w.CadenceRounds = 0; break;
            case "cadence-over-cap": w.CadenceRounds = 4; break;
            case "invalid-fire-round": w.InitialNextFireRound = 2; break;
            case "stock-over-cap": p.InitialAmmunition![0].Stock = 13; break;
            case "negative-stock": p.InitialAmmunition![0].Stock = -1; break;
            case "sector-over-cap": p.InitialShields![0].Current = 41; break;
            case "negative-sector": p.InitialShields![0].Current = -1; break;
            case "energy-magazine": w.MagazineCapacity = 1; break;
            case "unknown-ammunition": f.Weapons!.Single(v => v.Class == TacticalWeaponClass.Torpedo).AmmunitionDefinitionId = "ammo.unknown"; break;
        }
        Assert.Throws<ArgumentException>(() => CampaignApplication.StartScenario(scenario));
    }

    [Theory]
    [InlineData("mount-class")]
    [InlineData("mount-coverage")]
    [InlineData("mount-capacity")]
    [InlineData("missing-mount")]
    [InlineData("duplicate-mount")]
    [InlineData("duplicate-weapon")]
    [InlineData("duplicate-sector")]
    [InlineData("missing-sector")]
    [InlineData("duplicate-stock")]
    [InlineData("extra-energy-stock")]
    [InlineData("missing-torpedo-stock")]
    public void PackageBindingsCoverageAndExplicitProvisionListsAreValidated(string invalid)
    {
        var scenario = Scenario(); var f = Fit(scenario); var p = Provision(scenario);
        switch (invalid)
        {
            case "mount-class": f.Mounts![0].AllowedWeaponClasses = new() { TacticalWeaponClass.Torpedo }; break;
            case "mount-coverage": f.Mounts![0].Coverage = TacticalArc.Fore; break;
            case "mount-capacity": f.Weapons![0].SlotsRequired = 3; break;
            case "missing-mount": f.Weapons![0].MountId = "mount.unknown"; break;
            case "duplicate-mount": f.Mounts!.Add(Clone(f.Mounts[0])); break;
            case "duplicate-weapon": f.Weapons!.Add(Clone(f.Weapons[0])); break;
            case "duplicate-sector": f.ShieldLimits![1].Sector = TacticalSector.Fore; break;
            case "missing-sector": p.InitialShields!.RemoveAt(0); break;
            case "duplicate-stock": p.InitialAmmunition!.Add(Clone(p.InitialAmmunition[0])); break;
            case "extra-energy-stock": p.InitialAmmunition!.Add(new() { WeaponId = "weapon.beam", AmmunitionDefinitionId = "ammo.provisional-torpedo", Stock = 0 }); break;
            case "missing-torpedo-stock": p.InitialAmmunition!.Clear(); break;
        }
        Assert.Throws<ArgumentException>(() => CampaignApplication.StartScenario(scenario));
    }

    [Theory]
    [InlineData("reactor")]
    [InlineData("distribution")]
    [InlineData("essential-underallocation")]
    [InlineData("missing-essential")]
    [InlineData("duplicate-essential")]
    [InlineData("no-targeting")]
    [InlineData("partial-weapon")]
    [InlineData("partial-recharge")]
    [InlineData("free-recharge")]
    [InlineData("essential-overflow")]
    [InlineData("weapon-overflow")]
    public void EssentialPowerAndOverflowCannotGrantTheWholeReactorToWeapons(string invalid)
    {
        var scenario = Scenario(); var f = Fit(scenario);
        switch (invalid)
        {
            case "reactor": f.ReactorOutput = 35; break;
            case "distribution": f.DistributionLimit = 35; break;
            case "essential-underallocation": f.EssentialReservations![0].AllocatedPower = 0; break;
            case "missing-essential": f.EssentialReservations!.RemoveAt(0); break;
            case "duplicate-essential": f.EssentialReservations!.Add(Clone(f.EssentialReservations[0])); break;
            case "no-targeting": f.EssentialReservations![3].RequiredPower = f.EssentialReservations[3].AllocatedPower = 0; break;
            case "partial-weapon": f.Weapons![0].AllocatedPower = 5; break;
            case "partial-recharge": f.ShieldRechargeAllocatedPower = 3; break;
            case "free-recharge": f.ShieldRechargeRequiredPower = f.ShieldRechargeAllocatedPower = 0; break;
            case "essential-overflow": f.ReactorOutput = f.DistributionLimit = long.MaxValue; f.EssentialReservations!.ForEach(r => r.RequiredPower = r.AllocatedPower = long.MaxValue); break;
            case "weapon-overflow": f.ReactorOutput = f.DistributionLimit = long.MaxValue; f.Weapons!.ForEach(w => w.RequiredPower = w.AllocatedPower = long.MaxValue); break;
        }
        Assert.Contains(CampaignApplication.ValidateScenario(scenario), i => i.Code == ValidationCode.InvalidTacticalEquipmentPower
            || i.Code == ValidationCode.MissingField || (invalid == "duplicate-essential" && i.Code == ValidationCode.DuplicateId));
        Assert.Throws<ArgumentException>(() => CampaignApplication.StartScenario(scenario));
    }

    [Fact]
    public void DisabledPackagesKeepTheirAuthoredStatisticsAndDoNotGainScaledOutput()
    {
        var scenario = Scenario(); Fit(scenario).Weapons![0].AllocatedPower = 0;
        var source = Equipment(CampaignApplication.StartScenario(scenario)); var beam = source.Weapons.Single(w => w.Class == TacticalWeaponClass.Beam);
        Assert.False(beam.Powered); Assert.Equal(16, beam.Damage); Assert.Equal(26, source.EquipmentPowerAllocated);
    }

    [Theory]
    [InlineData("foreign-owner")]
    [InlineData("unknown-ship")]
    [InlineData("wrong-design")]
    [InlineData("wrong-fit-revision")]
    [InlineData("duplicate-provision")]
    [InlineData("unregistered-fit")]
    [InlineData("duplicate-fit")]
    [InlineData("hull-mismatch")]
    [InlineData("legacy-format-with-equipment")]
    [InlineData("new-format-without-equipment")]
    [InlineData("future-content")]
    [InlineData("future-source-schema")]
    public void ContentVersionsProvenanceAndScenarioOwnedShipBindingsReject(string invalid)
    {
        var scenario = Scenario(); var p = Provision(scenario);
        switch (invalid)
        {
            case "foreign-owner": p.OwnerCivilizationId = "civ.dominion"; break;
            case "unknown-ship": p.ShipId = "ship.unknown"; break;
            case "wrong-design": p.ShipDesignDefinitionId = "ship-design.scout"; break;
            case "wrong-fit-revision": p.FitRevision = 2; break;
            case "duplicate-provision": Catalogue(scenario).InitialShipProvisions!.Add(Clone(p)); break;
            case "unregistered-fit": scenario.Definitions.DefinitionIds.Remove(Fit(scenario).DefinitionId!); break;
            case "duplicate-fit": Catalogue(scenario).Fits!.Add(Clone(Fit(scenario))); break;
            case "hull-mismatch": Fit(scenario).MaxHull = 221; break;
            case "legacy-format-with-equipment": scenario.Definitions.ContentFormatVersion = 10; break;
            case "new-format-without-equipment": scenario.Definitions.AuthoredTacticalEquipment = null; break;
            case "future-content": scenario.Definitions.ContentFormatVersion = 12; break;
            case "future-source-schema": Catalogue(scenario).FormatVersion = 2; break;
        }
        Assert.Throws<ArgumentException>(() => CampaignApplication.StartScenario(scenario));
    }

    [Theory]
    [InlineData(null, CampaignTacticalInspectionError.InvalidActor)]
    [InlineData("civ.unknown", CampaignTacticalInspectionError.UnknownActor)]
    [InlineData("civ.cardassian", CampaignTacticalInspectionError.ActorNotControlled)]
    public void InvalidOrForeignAiActorCannotObtainAuthoredEquipment(string? actor, CampaignTacticalInspectionError error)
    {
        var app = CampaignApplication.StartScenario(Scenario()); var before = Json(app.ExportSave());
        var report = ((ICampaignTacticalV2Inspection)app).InspectCampaignTacticalV2(actor);
        Assert.Equal(error, report.ErrorCode); Assert.Empty(report.OwnedShips); Assert.Null(report.CurrentCatalogue);
        Assert.False(report.CanActivate); Assert.Equal(before, Json(app.ExportSave()));
    }

    [Fact]
    public void CallerContentAndReadOnlyProjectionCannotMutateOwnedSourceOrInitialStocks()
    {
        var scenario = Scenario(); var app = CampaignApplication.StartScenario(scenario); var old = Equipment(app); var expected = Json(old);
        Fit(scenario).Weapons![0].Damage = 9999; Provision(scenario).InitialAmmunition![0].Stock = 12;
        Fit(scenario).EssentialReservations![0].AllocatedPower = 9999; Catalogue(scenario).Fits!.Clear();
        Assert.Equal(expected, Json(Equipment(app))); Assert.Equal(expected, Json(old));
        Assert.Throws<NotSupportedException>(() => ((IList)old.Weapons).Clear());
        Assert.Throws<NotSupportedException>(() => ((IList)old.InitialShields).Clear());
        Assert.Throws<NotSupportedException>(() => ((IList)old.Mounts[0].AllowedWeaponClasses).Clear());
        Assert.All(typeof(AuthoredTacticalEquipmentSnapshot).GetProperties(), p => Assert.Null(p.SetMethod));
    }

    [Theory]
    [InlineData("en-US")]
    [InlineData("nl-NL")]
    [InlineData("tr-TR")]
    public void ReorderedSourceContentAndCultureProduceIdenticalDetachedEquipment(string culture)
    {
        var scenario = Scenario(); var expected = Json(Inspect(CampaignApplication.StartScenario(scenario)));
        var oldCulture = CultureInfo.CurrentCulture; var oldUi = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(culture);
            scenario.Definitions.DefinitionIds.Reverse(); scenario.InitialState.Ships!.Reverse();
            var c = Catalogue(scenario); c.Fits!.Reverse(); c.InitialShipProvisions!.Reverse(); c.AmmunitionDefinitions!.Reverse();
            foreach (var f in c.Fits) { f.Weapons!.Reverse(); f.Mounts!.Reverse(); f.ShieldLimits!.Reverse(); f.EssentialReservations!.Reverse(); foreach (var m in f.Mounts) m.AllowedWeaponClasses!.Reverse(); }
            foreach (var p in c.InitialShipProvisions) { p.InitialShields!.Reverse(); p.InitialAmmunition!.Reverse(); }
            Assert.Equal(expected, Json(Inspect(CampaignApplication.StartScenario(scenario))));
        }
        finally { CultureInfo.CurrentCulture = oldCulture; CultureInfo.CurrentUICulture = oldUi; }
    }

    [Fact]
    public void ScenarioContentAndSaveRoundTripsRetainSourcesWithoutAddingRuntimeResourceFields()
    {
        var scenario = Scenario(); var app = CampaignApplication.StartScenario(scenario); var before = Json(app.ExportSave());
        for (var i = 0; i < 10; i++) { Assert.Equal(before, Json(app.ExportSave())); Assert.Equal(Json(Inspect(app)), Json(Inspect(app))); }
        var restored = CampaignApplication.LoadSave(Clone(scenario), Clone(app.ExportSave()));
        Assert.Equal(Json(Inspect(app)), Json(Inspect(restored))); Assert.Equal(before, Json(restored.ExportSave()));
        Assert.Equal(14, restored.ExportSave().SaveFormatVersion); Assert.Equal(11, scenario.Definitions.ContentFormatVersion);
        Assert.DoesNotContain("Ammunition", before); Assert.DoesNotContain("InitialShields", before);
        Assert.Equal(7, Equipment(restored).Weapons.Single(w => w.Class == TacticalWeaponClass.Torpedo).InitialAmmunition);
        var alteredContent = Clone(scenario); alteredContent.ContentHash = new string('c', 64);
        Assert.Throws<ArgumentException>(() => CampaignApplication.LoadSave(alteredContent, app.ExportSave()));
    }

    [Fact]
    public void FrozenV1PublicationNeverAcquiresAuthoredEquipmentFromNamesOrTheLatestFit()
    {
        var scenario = Scenario(); var app = CampaignApplication.StartScenario(scenario);
        using var fixture = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "scenarios/ship-design-fixtures/fixture.json")));
        var draft = fixture.RootElement.GetProperty("Drafts").EnumerateArray().Single(d => d.GetProperty("ClassId").GetString() == "class.escort").Deserialize<ShipClassDraftDto>()!;
        draft.DisplayName = "fit.provisional-cruiser"; var stamp = app.CaptureSnapshot().Campaign;
        Assert.True(app.SubmitCommand(new() { CommandId = "cmd.equipment-publish", ActorCivilizationId = Actor,
            ExpectedTurn = stamp.TurnNumber, ExpectedRevision = stamp.StateRevision, Kind = CommandKind.PublishShipBlueprint,
            PublishShipBlueprint = new() { BlueprintId = "blueprint.equipment-test", Revision = 1, Draft = draft } }).Accepted);
        var frozen = Json(app.GetPublishedShipBlueprints()); var report = Inspect(app); Assert.Single(report.OwnedBlueprintRevisions);
        Assert.Equal(ShipDesignVersions.Compiler, report.OwnedBlueprintRevisions[0].Blueprint.RulesIdentity);
        Assert.Equal(frozen, Json(app.GetPublishedShipBlueprints()));
        var restored = CampaignApplication.LoadSave(scenario, Clone(app.ExportSave()));
        Assert.Equal(frozen, Json(restored.GetPublishedShipBlueprints()));
        Assert.Contains(CampaignTacticalV2Blocker.ExplicitEquipmentSchemaRequired, Inspect(restored).Blockers);
        Assert.False(Inspect(restored).CanActivate);
    }

    [Fact]
    public void NonemptyCommandLedgerAndRejectedCommandsAreUnchangedByRepeatedInspection()
    {
        var scenario = Scenario(); var app = CampaignApplication.StartScenario(scenario); var stamp = app.CaptureSnapshot().Campaign;
        var command = new CommandEnvelope { CommandId = "cmd.source-research", ActorCivilizationId = Actor,
            ExpectedTurn = stamp.TurnNumber, ExpectedRevision = stamp.StateRevision, Kind = CommandKind.SetResearchAllocation,
            SetResearchAllocation = new() { ScienceAllocationUnits = 360 } };
        Assert.True(app.SubmitCommand(command).Accepted); var before = Json(app.ExportSave());
        Assert.Contains(command.CommandId, app.ExportSave().GameState.AcceptedCommandIds!);
        for (var i = 0; i < 10; i++) { Assert.False(Inspect(app).CanActivate); Assert.Equal(before, Json(app.ExportSave())); }
        command.CommandId = "cmd.source-stale"; Assert.Equal(CommandErrorCode.StaleRevision, app.SubmitCommand(command).ErrorCode);
        Assert.Equal(before, Json(app.ExportSave())); Assert.Equal(7, Equipment(app).Weapons.Single(w => w.Class == TacticalWeaponClass.Torpedo).InitialAmmunition);
    }

    [Fact]
    public void DestroyedAuthoredShipDoesNotReappearOrReceiveNewStocksOnLoad()
    {
        var scenario = Scenario(); var app = CampaignApplication.StartScenario(scenario); var save = app.ExportSave();
        save.GameState.Ships!.RemoveAll(s => s.ShipId == Cruiser); save.GameState.EntityIdentities.RemoveAll(i => i.EntityId == Cruiser);
        save.GameState.Fleets!.Single(f => f.OwnerCivilizationId == Actor).ShipIds!.Remove(Cruiser);
        var loaded = CampaignApplication.LoadSave(scenario, save); Assert.DoesNotContain(Inspect(loaded).OwnedShips, s => s.Ship.ShipId.Value == Cruiser);
        Assert.DoesNotContain(loaded.ExportSave().GameState.Ships!, s => s.ShipId == Cruiser);
    }

    [Fact]
    public void NewLegacyProductionOfTheSameStaticDesignDoesNotInheritAnInitialShipsEquipment()
    {
        var scenario = Scenario(); var design = scenario.Definitions.ShipDesigns!.Single(d => d.DefinitionId == "ship-design.source-cruiser");
        // Test-only affordable existing v1 production, not v2 construction/provisioning.
        design.CreditCost = 0; design.ResourceCosts = new(); design.IndustryCost = 1;
        var app = CampaignApplication.StartScenario(scenario); var stamp = app.CaptureSnapshot().Campaign;
        Assert.True(app.SubmitCommand(new() { CommandId = "cmd.source-static-build", ActorCivilizationId = Actor,
            ExpectedTurn = stamp.TurnNumber, ExpectedRevision = stamp.StateRevision, Kind = CommandKind.EnqueueProduction,
            EnqueueProduction = new() { ColonyId = "colony.sol", DefinitionId = design.DefinitionId } }).Accepted);
        stamp = app.CaptureSnapshot().Campaign; var turn = app.AdvanceTurn(stamp.TurnNumber, stamp.StateRevision);
        if (turn.Status == TurnStatus.AwaitingCombat)
        {
            foreach (var request in app.GetPendingCombatRequests()) { stamp = app.CaptureSnapshot().Campaign; Assert.Equal(CombatSubmissionStatus.Buffered, app.SubmitCombatResult(app.ResolveCombat(request), stamp.TurnNumber, stamp.StateRevision).Status); }
            stamp = app.CaptureSnapshot().Campaign; Assert.Equal(TurnStatus.Completed, app.FinalizeCombat(stamp.TurnNumber, stamp.StateRevision).Status);
        }
        else Assert.Equal(TurnStatus.Completed, turn.Status);
        var built = Inspect(app).OwnedShips.Single(s => s.Ship.ShipId.Value.StartsWith("ship.spawn.", StringComparison.Ordinal));
        Assert.Equal(design.DefinitionId, built.Ship.ShipDesignDefinitionId.Value); Assert.False(built.CanProjectTacticalV2);
        Assert.Null(built.AuthoredEquipment); Assert.Equal(CampaignTacticalSourceKind.LegacyStatic, built.SourceKind);
    }

    [Fact]
    public void SupportedInt64EquipmentExtremesRemainFiniteAndDoNotOverflowProjection()
    {
        var scenario = Scenario(); var fit = Fit(scenario); fit.Armor = long.MaxValue; fit.ShieldRechargePerRound = long.MaxValue;
        fit.ShieldLimits!.ForEach(s => s.Maximum = long.MaxValue);
        foreach (var weapon in fit.Weapons!) { weapon.Damage = long.MaxValue; weapon.AccuracyBps = weapon.TrackingBps = 10000; weapon.OpportunitiesPerRound = 64; weapon.CadenceRounds = weapon.InitialNextFireRound = 3; }
        fit.Weapons.Single(w => w.Class == TacticalWeaponClass.Torpedo).MagazineCapacity = long.MaxValue;
        Assert.Empty(CampaignApplication.ValidateScenario(scenario)); var source = Equipment(CampaignApplication.StartScenario(scenario));
        Assert.Equal(long.MaxValue, source.Armor); Assert.All(source.InitialShields, s => Assert.Equal(long.MaxValue, s.Maximum));
        Assert.Equal(7, source.Weapons.Single(w => w.Class == TacticalWeaponClass.Torpedo).InitialAmmunition);
        Assert.Equal(36, source.EquipmentPowerAllocated);
    }

    [Theory]
    [InlineData("fit")]
    [InlineData("provision")]
    [InlineData("ammo-definition")]
    [InlineData("reservation")]
    [InlineData("limit")]
    [InlineData("mount")]
    [InlineData("weapon")]
    [InlineData("shield-stock")]
    [InlineData("ammo-stock")]
    public void NullContentRowsReturnTypedValidationRatherThanDereferencingMalformedInput(string row)
    {
        var scenario = Scenario(); var f = Fit(scenario); var p = Provision(scenario); var c = Catalogue(scenario);
        switch (row)
        {
            case "fit": c.Fits!.Add(null!); break; case "provision": c.InitialShipProvisions!.Add(null!); break;
            case "ammo-definition": c.AmmunitionDefinitions!.Add(null!); break;
            case "reservation": f.EssentialReservations!.Add(null!); break; case "limit": f.ShieldLimits!.Add(null!); break;
            case "mount": f.Mounts!.Add(null!); break; case "weapon": f.Weapons!.Add(null!); break;
            case "shield-stock": p.InitialShields!.Add(null!); break; case "ammo-stock": p.InitialAmmunition!.Add(null!); break;
        }
        Assert.Contains(CampaignApplication.ValidateScenario(scenario), i => i.Code == ValidationCode.MissingField);
        Assert.Throws<ArgumentException>(() => CampaignApplication.StartScenario(scenario));
    }
}
