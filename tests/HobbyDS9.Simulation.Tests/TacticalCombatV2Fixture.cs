using System.Text.Json;
using HobbyDS9.Contracts;
using HobbyDS9.Simulation;

namespace HobbyDS9.Simulation.Tests;

internal static class TacticalCombatV2Fixture
{
    internal static TacticalEncounterV2Dto Encounter(int formations = 2, int ships = 1, ulong seed = 47)
    {
        var state = new TacticalEncounterV2Dto { EncounterId = "encounter.v2-fixture", RandomState = seed };
        for (int f = 0; f < formations; f++)
        {
            var id = "formation." + f.ToString("d4", System.Globalization.CultureInfo.InvariantCulture);
            var formation = new TacticalFormationV2Dto { FormationId = id, SideId = "side." + f % 2, StandingOrder = new TacticalOrderV2Dto { FormationId = id } };
            for (int s = 0; s < ships; s++)
            {
                var ship = new TacticalShipV2Dto { ShipId = "ship." + f.ToString("d4", System.Globalization.CultureInfo.InvariantCulture) + "." + s.ToString("d4", System.Globalization.CultureInfo.InvariantCulture), Hull = 1000, MaxHull = 1000 };
                foreach (var sector in Enum.GetValues<TacticalSector>()) ship.Shields.Add(new TacticalShieldV2Dto { Sector = sector, Maximum = 50, Current = 50 });
                Weapon(ship, TacticalWeaponClass.Beam, 10);
                formation.Ships.Add(ship);
            }
            state.Formations.Add(formation);
        }
        for (int a = 0; a < formations; a++) for (int b = a + 1; b < formations; b++)
            if (state.Formations[a].SideId != state.Formations[b].SideId)
                state.Geometry.Add(new TacticalGeometryV2Dto { FormationA = state.Formations[a].FormationId, FormationB = state.Formations[b].FormationId });
        return state;
    }
    internal static TacticalWeaponV2Dto Weapon(TacticalShipV2Dto ship, TacticalWeaponClass kind, long damage, int opportunities = 1, TacticalArc arc = TacticalArc.All)
    {
        var id = "weapon." + ship.Weapons.Count;
        ship.Mounts.Add(new TacticalMountV2Dto { MountId = id, WeaponClass = kind, Coverage = arc });
        var w = new TacticalWeaponV2Dto { WeaponId = id, MountId = id, Class = kind, Damage = damage, AccuracyBps = 10000, OpportunitiesPerRound = opportunities, Coverage = arc,
            MagazineCapacity = kind == TacticalWeaponClass.Torpedo ? 10 : 0, Ammunition = kind == TacticalWeaponClass.Torpedo ? 10 : 0 };
        ship.Weapons.Add(w); return w;
    }
    internal static TacticalShipV2Dto Ship(TacticalEncounterV2Dto s, int f = 0, int ship = 0) => s.Formations[f].Ships[ship];
    internal static void Unarmed(TacticalEncounterV2Dto state)
    { foreach (var s in state.Formations.SelectMany(f => f.Ships)) { s.Weapons.Clear(); s.Mounts.Clear(); } }
    internal static void Shields(TacticalShipV2Dto ship, long value)
    { foreach (var q in ship.Shields) { q.Maximum = value; q.Current = value; } }
    internal static TacticalRoundV2Result Round(TacticalEncounterV2Dto s, params TacticalOrderV2Dto[] replacements) => new DeterministicTacticalCombatV2Resolver().ResolveRound(s,
        replacements.Length == 0 ? null : new TacticalOrderBatchV2Dto { EncounterId = s.EncounterId, Round = s.NextRound, Replacements = replacements.ToList() });
    internal static TacticalOrderV2Dto Order(TacticalEncounterV2Dto state, int f, TacticalManeuver maneuver, TacticalRange preferred = TacticalRange.Engagement) =>
        new() { FormationId = state.Formations[f].FormationId, Maneuver = maneuver, PreferredRange = preferred };
    internal static string Json<T>(T value) => JsonSerializer.Serialize(value);
    internal static TacticalEncounterV2Dto Reload(TacticalEncounterV2Dto s) => JsonSerializer.Deserialize<TacticalEncounterV2Dto>(Json(s))!;
    internal static string Trace(TacticalRoundV2Result r) => Json(new { State = r.NextState, r.Events });
}
