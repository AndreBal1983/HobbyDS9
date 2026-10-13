using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using HobbyDS9.Application;
using HobbyDS9.Contracts;

namespace HobbyDS9.EquipmentIntegrityProbe
{
    internal static class Program
    {
        private const string Actor = "civ.federation", Cruiser = "ship.equipment-cruiser";
        private static ScenarioDto Scenario()
        {
            var bytes = File.ReadAllBytes("scenarios/tactical-v2-equipment-source/scenario.json");
            var result = JsonSerializer.Deserialize<ScenarioDto>(bytes)!;
            result.ContentHash = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
            return result;
        }
        private static string Json<T>(T value) => JsonSerializer.Serialize(value);
        private static T Clone<T>(T value) => JsonSerializer.Deserialize<T>(Json(value))!;
        private static void Check(bool condition, string message)
        { if (!condition) throw new InvalidOperationException(message); }
        private static CampaignTacticalV2EligibilitySnapshot Inspect(ICampaignSession session)
            => ((ICampaignTacticalV2Inspection)session).InspectCampaignTacticalV2(Actor);
        private static AuthoredTacticalEquipmentSnapshot Equipment(ICampaignSession session)
            => Inspect(session).OwnedShips.Single(s => s.Ship.ShipId.Value == Cruiser).AuthoredEquipment!;
        private static void MustReject(ScenarioDto scenario, SaveEnvelopeDto save, string label)
        {
            var issues = CampaignApplication.ValidateSave(scenario, save);
            Check(issues.Any(i => i.Code == ValidationCode.InvalidTacticalEquipmentBinding), label + ": no binding-specific issue");
            try { CampaignApplication.LoadSave(scenario, save); }
            catch (ArgumentException) { return; }
            throw new InvalidOperationException(label + ": corrupted save loaded");
        }
        private static void Probe(string name, Action action)
        { action(); Console.WriteLine("PASS " + name); }
        public static int Main()
        {
            try
            {
                Inventory();
                Probe("P1 post-LoadSave nested input/export ownership and stable immutable source projection", () =>
                {
                    var scenario = Scenario(); var original = CampaignApplication.StartScenario(scenario);
                    var inputSave = Clone(original.ExportSave()); var loaded = CampaignApplication.LoadSave(scenario, inputSave);
                    var cached = Equipment(loaded); var equipmentJson = Json(cached); var saveJson = Json(loaded.ExportSave());
                    var fit = scenario.Definitions.AuthoredTacticalEquipment!.Fits!.Single(f => f.DefinitionId == "fit.provisional-cruiser");
                    var provision = scenario.Definitions.AuthoredTacticalEquipment.InitialShipProvisions!.Single(p => p.ShipId == Cruiser);
                    fit.Capabilities!.Speed = 0; fit.EssentialReservations![0].AllocatedPower = 9999;
                    fit.ShieldLimits![0].Maximum = 1; fit.Mounts![0].AllowedWeaponClasses!.Clear();
                    fit.Weapons![0].Damage = 9999; provision.InitialShields![0].Current = 0;
                    provision.InitialAmmunition![0].Stock = 12; fit.Revision = 2;
                    scenario.Definitions.AuthoredTacticalEquipment.Fits!.Clear();
                    inputSave.GameState.Ships!.Single(s => s.ShipId == Cruiser).CurrentHull = 1;
                    var exported = loaded.ExportSave(); exported.GameState.Ships!.Clear();
                    Check(Json(Equipment(loaded)) == equipmentJson, "caller source alias changed loaded inspection");
                    Check(Json(cached) == equipmentJson, "cached snapshot alias changed");
                    Check(Json(loaded.ExportSave()) == saveJson, "caller save/export alias changed loaded state");
                    var cachedLists = new IList[] { (IList)cached.Weapons, (IList)cached.InitialShields,
                        (IList)cached.EssentialReservations, (IList)cached.Mounts[0].AllowedWeaponClasses };
                    foreach (var list in cachedLists)
                    {
                        var rejected = false; try { list.Clear(); } catch (NotSupportedException) { rejected = true; }
                        Check(rejected, "source projection collection mutable");
                    }
                    Check(cached.Weapons.Single(p => p.Class == TacticalWeaponClass.Torpedo).InitialAmmunition == 7,
                        "initial ammunition reinterpreted after load");
                });
                Probe("P2 corrupted saved owner cannot rebind source even with consistent fleet membership", () =>
                {
                    var scenario = Scenario(); var live = CampaignApplication.StartScenario(scenario); var before = Json(live.ExportSave());
                    var save = Clone(live.ExportSave()); var ship = save.GameState.Ships!.Single(s => s.ShipId == Cruiser);
                    foreach (var fleet in save.GameState.Fleets!) fleet.ShipIds!.Remove(Cruiser);
                    ship.OwnerCivilizationId = "civ.dominion";
                    save.GameState.Fleets.Single(f => f.OwnerCivilizationId == "civ.dominion").ShipIds!.Add(Cruiser);
                    MustReject(scenario, save, "saved owner");
                    Check(before == Json(live.ExportSave()), "failed new-session load mutated old session");
                });
                Probe("P3 corrupted saved static design cannot rebind source even with matching identity header", () =>
                {
                    var scenario = Scenario(); var live = CampaignApplication.StartScenario(scenario); var before = Json(live.ExportSave());
                    var save = Clone(live.ExportSave()); var ship = save.GameState.Ships!.Single(s => s.ShipId == Cruiser);
                    ship.ShipDesignDefinitionId = "ship-design.scout"; ship.CurrentHull = 9;
                    save.GameState.EntityIdentities.Single(i => i.EntityId == Cruiser).DefinitionId = "ship-design.scout";
                    MustReject(scenario, save, "saved design");
                    Check(before == Json(live.ExportSave()), "failed design load mutated old session");
                });
                Probe("P4 all authored sources absent after destruction stay absent through repeated reload/inspection", () =>
                {
                    var scenario = Scenario(); var live = CampaignApplication.StartScenario(scenario);
                    var ids = scenario.Definitions.AuthoredTacticalEquipment!.InitialShipProvisions!.Select(p => p.ShipId!).ToHashSet(StringComparer.Ordinal);
                    var save = Clone(live.ExportSave());
                    save.GameState.Ships!.RemoveAll(s => ids.Contains(s.ShipId));
                    foreach (var fleet in save.GameState.Fleets!) fleet.ShipIds!.RemoveAll(ids.Contains);
                    var emptyFleets = save.GameState.Fleets.Where(f => f.ShipIds!.Count == 0).Select(f => f.FleetId).ToHashSet(StringComparer.Ordinal);
                    save.GameState.Fleets.RemoveAll(f => emptyFleets.Contains(f.FleetId));
                    save.GameState.EntityIdentities.RemoveAll(i => ids.Contains(i.EntityId) || emptyFleets.Contains(i.EntityId));
                    for (var i = 0; i < 3; i++)
                    {
                        var loaded = CampaignApplication.LoadSave(scenario, save);
                        var before = Json(loaded.ExportSave()); var report = Inspect(loaded);
                        Check(report.OwnedShips.All(s => s.AuthoredEquipment == null), "destroyed source projected again");
                        Check(loaded.ExportSave().GameState.Ships!.All(s => !ids.Contains(s.ShipId)), "authored ship recreated");
                        Check(!report.CanActivate, "inspection activated v2");
                        Check(before == Json(loaded.ExportSave()), "inspection wrote pools/state");
                        save = Clone(loaded.ExportSave());
                    }
                });
                Console.WriteLine("RESULT 4 focused probes passed; no production source edits or full-suite rerun.");
                return 0;
            }
            catch (Exception e) { Console.WriteLine("FAIL " + e.GetType().Name + ": " + e.Message); return 1; }
        }
        private static void Inventory()
        {
            var scenario = Scenario(); var catalogue = scenario.Definitions.AuthoredTacticalEquipment!;
            var fit = catalogue.Fits!.Single(f => f.DefinitionId == "fit.provisional-cruiser");
            var provision = catalogue.InitialShipProvisions!.Single(p => p.ShipId == Cruiser);
            var targets = new object[] { catalogue, catalogue.AmmunitionDefinitions![0], fit, fit.Capabilities!,
                fit.EssentialReservations![0], fit.ShieldLimits![0], fit.Mounts![0], fit.Weapons![0], provision,
                provision.InitialShields![0], provision.InitialAmmunition![0] };
            var source = File.ReadAllText("tests/HobbyDS9.Simulation.Tests/AuthoredTacticalEquipmentTests.cs");
            var facts = Regex.Matches(source, @"\[Fact\]").Count;
            var inline = Regex.Matches(source, @"\[InlineData\(").Count;
            var missing = targets.Sum(o => o.GetType().GetProperties().Length);
            Check(facts + inline + missing == 149 && missing == 68, "existing source test inventory changed");
            Console.WriteLine("READ-ONLY INVENTORY " + (facts + inline + missing) + " existing equipment cases; "
                + missing + " missing-field rows; tests inspected, not rerun.");
        }
    }
}
