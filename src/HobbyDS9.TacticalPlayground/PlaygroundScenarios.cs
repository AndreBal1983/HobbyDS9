using HobbyDS9.Contracts;
using HobbyDS9.Simulation;

namespace HobbyDS9.TacticalPlayground;

public sealed class PlaygroundScenario
{
    public string Id { get; }
    public string Title { get; }
    public string Briefing { get; }
    public string OpponentScript { get; }
    internal PlaygroundScenario(string id, string title, string briefing, string script)
    { Id = id; Title = title; Briefing = briefing; OpponentScript = script; }
}

// Authored sandbox content, NOT campaign blueprints or faction-stat conditionals.
public static class PlaygroundScenarios
{
    public const string PlayerSide = "side.human";
    public const int FixtureVersion = 1;
    public static IReadOnlyList<PlaygroundScenario> All { get; } = Array.AsReadOnly(new[]
    {
        new PlaygroundScenario("cruiser", "Federation-style cruiser vs forward attack wing",
            "Try Flank to expose lateral shields and deny forward cannons; compare Charge with the same seed.", "Charge / Charge / FightingWithdrawal"),
        new PlaygroundScenario("screen", "Escort and transport vs torpedo salvo",
            "Your escort starts screening the transport. Compare Screen with Charge: beam defense costs offensive opportunities.", "MaintainDistance, concentrating on transport, all rounds"),
        new PlaygroundScenario("breakthrough", "Breakthrough against a defending line",
            "Two successful Breakthrough rounds at Intermixed achieve the local goal. Holding or flanking alone does not cross the line.", "Screen / Pursuit / Pursuit")
    });
    public static PlaygroundScenario Find(string id) => All.FirstOrDefault(s => s.Id.Equals(id, StringComparison.OrdinalIgnoreCase))
        ?? throw new ArgumentException("Unknown scenario. Choose cruiser, screen or breakthrough.");
    public static TacticalEncounterV2Dto Create(string id, ulong seed)
    {
        Find(id); id = Find(id).Id;
        var state = new TacticalEncounterV2Dto { EncounterId = "encounter.playground-" + id, RandomState = seed };
        if (id == "cruiser")
        {
            var cruiser = Formation("cruiser", true, 70, 80);
            var ship = Ship("cruiser", 220, 10, 70, 25, 25, 35); ship.ShieldRechargePerRound = 6;
            Weapon(ship, "array", TacticalWeaponClass.Beam, 22, 2, TacticalArc.All);
            Weapon(ship, "photons", TacticalWeaponClass.Torpedo, 35, 1, TacticalArc.All, 4, 2);
            cruiser.Ships.Add(ship); state.Formations.Add(cruiser);
            var wing = Formation("attack-wing", false, 60, 40);
            foreach (var suffix in new[] { "attack-a", "attack-b" })
            {
                var attacker = Ship(suffix, 120, 5, 45, 15, 15, 20);
                Weapon(attacker, "pulse", TacticalWeaponClass.Cannon, 38, 2, TacticalArc.Fore);
                Weapon(attacker, "torpedo", TacticalWeaponClass.Torpedo, 28, 1, TacticalArc.Fore, 3, 2);
                wing.Ships.Add(attacker);
            }
            wing.StandingOrder.Maneuver = TacticalManeuver.Charge; state.Formations.Add(wing);
        }
        else if (id == "screen")
        {
            var escort = Formation("escort", true, 65, 65); escort.StandingOrder.Maneuver = TacticalManeuver.Screen;
            escort.StandingOrder.ProtectedFormationId = "formation.transport";
            var ship = Ship("escort", 180, 5, 40, 20, 20, 30); ship.Role = TacticalShipRole.Escort; ship.ShieldRechargePerRound = 6;
            Weapon(ship, "defense-array", TacticalWeaponClass.Beam, 14, 4, TacticalArc.All).TrackingBps = 5000;
            escort.Ships.Add(ship); state.Formations.Add(escort);
            var transport = Formation("transport", true, 30, 25);
            transport.Ships.Add(Ship("transport", 200, 10, 50, 30, 30, 30)); state.Formations.Add(transport);
            var raider = Formation("raider", false, 55, 50);
            var enemy = Ship("raider", 220, 10, 50, 30, 30, 30);
            Weapon(enemy, "salvo", TacticalWeaponClass.Torpedo, 40, 5, TacticalArc.All, 10);
            Weapon(enemy, "array", TacticalWeaponClass.Beam, 15, 2, TacticalArc.All);
            raider.Ships.Add(enemy); raider.StandingOrder.TargetFormationId = transport.FormationId; state.Formations.Add(raider);
        }
        else
        {
            var runner = Formation("runner", true, 90, 80); runner.Objective = TacticalObjective.Breakthrough;
            runner.StandingOrder.Maneuver = TacticalManeuver.Breakthrough;
            var ship = Ship("runner", 240, 10, 80, 30, 30, 20); ship.ShieldRechargePerRound = 4;
            Weapon(ship, "array", TacticalWeaponClass.Beam, 15, 2, TacticalArc.All);
            Weapon(ship, "pulse", TacticalWeaponClass.Cannon, 30, 1, TacticalArc.Fore);
            Weapon(ship, "torpedo", TacticalWeaponClass.Torpedo, 25, 1, TacticalArc.All, 4);
            runner.Ships.Add(ship); state.Formations.Add(runner);
            var blockade = Formation("blockade", false, 55, 45); blockade.Objective = TacticalObjective.Hold; blockade.ObjectiveRequiredRounds = 3;
            var enemy = Ship("blockade", 260, 20, 60, 60, 60, 60);
            Weapon(enemy, "array", TacticalWeaponClass.Beam, 15, 2, TacticalArc.All);
            Weapon(enemy, "pulse", TacticalWeaponClass.Cannon, 40, 1, TacticalArc.Fore);
            blockade.Ships.Add(enemy); blockade.StandingOrder.Maneuver = TacticalManeuver.Screen; state.Formations.Add(blockade);
        }
        state.Formations = state.Formations.OrderBy(f => f.FormationId, StringComparer.Ordinal).ToList();
        for (int a = 0; a < state.Formations.Count; a++) for (int b = a + 1; b < state.Formations.Count; b++)
            if (state.Formations[a].SideId != state.Formations[b].SideId)
                state.Geometry.Add(new TacticalGeometryV2Dto { FormationA = state.Formations[a].FormationId, FormationB = state.Formations[b].FormationId,
                    Range = id == "breakthrough" ? TacticalRange.Close : TacticalRange.Engagement });
        return TacticalCombatV2State.Copy(state);
    }
    internal static List<TacticalOrderV2Dto> OpposingOrders(string scenario, TacticalEncounterV2Dto state)
    {
        // This sequence has NO access to newly chosen human orders.
        return state.Formations.Where(f => f.SideId != PlayerSide && f.Status == TacticalFormationStatus.Active).Select(f =>
        {
            var order = CloneOrder(f.StandingOrder);
            order.Maneuver = scenario == "cruiser" ? (state.NextRound == 3 ? TacticalManeuver.FightingWithdrawal : TacticalManeuver.Charge)
                : scenario == "breakthrough" ? (state.NextRound == 1 ? TacticalManeuver.Screen : TacticalManeuver.Pursuit) : TacticalManeuver.MaintainDistance;
            return order;
        }).ToList();
    }
    public static TacticalOrderV2Dto CloneOrder(TacticalOrderV2Dto order) => new()
    { FormationId = order.FormationId, Maneuver = order.Maneuver, TargetPriority = order.TargetPriority, PreferredRange = order.PreferredRange,
        TargetFormationId = order.TargetFormationId, ProtectedFormationId = order.ProtectedFormationId };
    private static TacticalFormationV2Dto Formation(string name, bool player, int speed, int agility) => new()
    { FormationId = "formation." + name, SideId = player ? PlayerSide : "side.script", Speed = speed, Agility = agility,
        StandingOrder = new TacticalOrderV2Dto { FormationId = "formation." + name } };
    private static TacticalShipV2Dto Ship(string name, long hull, long armor, long fore, long port, long starboard, long aft) => new()
    {
        ShipId = "ship." + name, Hull = hull, MaxHull = hull, Armor = armor, ShieldPowerRequired = 3, ShieldPowerAllocated = 3,
        Shields = new[] { fore, port, starboard, aft }.Select((amount, sector) => new TacticalShieldV2Dto { Sector = (TacticalSector)sector, Maximum = amount, Current = amount }).ToList()
    };
    private static TacticalWeaponV2Dto Weapon(TacticalShipV2Dto ship, string id, TacticalWeaponClass kind, long damage, int opportunities, TacticalArc arc, long ammo = 0, int cadence = 1)
    {
        ship.Mounts.Add(new TacticalMountV2Dto { MountId = "mount." + id, WeaponClass = kind, Coverage = arc });
        var weapon = new TacticalWeaponV2Dto { WeaponId = "weapon." + id, MountId = "mount." + id, Class = kind, Coverage = arc, RequiredPower = 2, AllocatedPower = 2,
            Damage = damage, OpportunitiesPerRound = opportunities, AccuracyBps = 9000, TrackingBps = 3000, MagazineCapacity = ammo, Ammunition = ammo, CadenceRounds = cadence };
        ship.Weapons.Add(weapon); return weapon;
    }
}
