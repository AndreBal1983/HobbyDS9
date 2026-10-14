using System;
using System.Collections.Generic;
using System.Linq;
using HobbyDS9.Contracts;

namespace HobbyDS9.Simulation
{
    // Private, frozen source-derived profile; never a player parameter or prototype conversion.
    internal sealed class CampaignRoundPolicy
    {
        internal HashSet<string> Hidden { get; }
        private readonly IReadOnlyDictionary<string, AuthoredTacticalCapabilitiesDto> capabilities;
        internal CampaignRoundPolicy(IEnumerable<string> hidden, IReadOnlyDictionary<string, AuthoredTacticalCapabilitiesDto> capabilities)
        { Hidden = new HashSet<string>(hidden, StringComparer.Ordinal); this.capabilities = capabilities; }
        internal int Mobility(string ship) => (capabilities[ship].Speed!.Value + capabilities[ship].Agility!.Value) / 2;
        internal int Sensors(string ship) => capabilities[ship].Sensors!.Value;
        internal TacticalGeometryV2Dto LocalGeometry(TacticalShipV2Dto ship, TacticalFormationV2Dto own, TacticalFormationV2Dto enemy, TacticalGeometryV2Dto pair)
        {
            var result = new TacticalGeometryV2Dto { FormationA = pair.FormationA, FormationB = pair.FormationB,
                Range = pair.Range, FacingA = pair.FacingA, FacingB = pair.FacingB };
            var speeds = enemy.Ships.Where(s => s.Hull > 0).Select(s => Mobility(s.ShipId)).OrderBy(s => s).ToList();
            // Proposed median opposition baseline, per-ship lateral access. No minimum-speed clamp.
            if (own.StandingOrder.Maneuver == TacticalManeuver.Flank && speeds.Count > 0 && Mobility(ship.ShipId) > speeds[(speeds.Count - 1) / 2])
            {
                if (own.FormationId == result.FormationA) { result.FacingA = TacticalSector.Fore; result.FacingB = TacticalSector.Port; }
                else { result.FacingB = TacticalSector.Fore; result.FacingA = TacticalSector.Starboard; }
            }
            return result;
        }
    }
}
