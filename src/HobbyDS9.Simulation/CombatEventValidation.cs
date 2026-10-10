using System;
using System.Collections.Generic;
using System.Linq;
using HobbyDS9.Contracts;

namespace HobbyDS9.Simulation
{
    internal static class CombatEventValidation
    {
        internal static CombatSubmissionError Validate(CombatRequestDto request, CombatResultDto result)
        {
            var ships = request.Participants.SelectMany(p => p.Ships.Select(s => (ship: s, fleet: p))).ToDictionary(x => x.ship.ShipId, StringComparer.Ordinal);
            var fleets = new HashSet<string>(request.Participants.Select(p => p.FleetId), StringComparer.Ordinal);
            for (var i = 0; i < result.Events.Count; i++)
            {
                var e = result.Events[i];
                if (e == null || e.Sequence != i || !Enum.IsDefined(typeof(CombatEventKind), e.Kind)
                    || (e.Phase.HasValue && !Enum.IsDefined(typeof(CombatPhaseKind), e.Phase.Value)) || e.Amount < 0) return CombatSubmissionError.InvalidEvents;
                if ((e.ShipId != null && !ships.ContainsKey(e.ShipId)) || (e.SourceShipId != null && !ships.ContainsKey(e.SourceShipId))
                    || (e.FleetId != null && !fleets.Contains(e.FleetId)) || (e.SourceFleetId != null && !fleets.Contains(e.SourceFleetId))) return CombatSubmissionError.Nonparticipant;
                if ((e.ShipId != null && e.FleetId != null && ships[e.ShipId].fleet.FleetId != e.FleetId)
                    || (e.SourceShipId != null && e.SourceFleetId != null && ships[e.SourceShipId].fleet.FleetId != e.SourceFleetId)) return CombatSubmissionError.InvalidEvents;
                var attack = e.Kind == CombatEventKind.AttackHit || e.Kind == CombatEventKind.AttackMissed;
                if (!attack && (e.SourceShipId != null || e.SourceFleetId != null)) return CombatSubmissionError.InvalidEvents;
                switch (e.Kind)
                {
                    case CombatEventKind.EncounterResolved:
                        if (i != result.Events.Count - 1 || e.Phase != null || e.ShipId != null || e.FleetId != null || e.Amount != null) return CombatSubmissionError.InvalidEvents;
                        break;
                    case CombatEventKind.PhaseStarted:
                        if (e.Phase == null || e.ShipId != null || e.FleetId != null || e.Amount != null) return CombatSubmissionError.InvalidEvents;
                        break;
                    case CombatEventKind.AttackHit:
                    case CombatEventKind.AttackMissed:
                        if (e.Phase == null || e.SourceShipId == null || e.ShipId == null || e.Amount != null
                            || ships[e.SourceShipId].fleet.CivilizationId == ships[e.ShipId].fleet.CivilizationId) return CombatSubmissionError.InvalidEvents;
                        break;
                    case CombatEventKind.ShieldDamaged:
                        if (e.Phase == null || e.ShipId == null || !e.Amount.HasValue || e.Amount <= 0) return CombatSubmissionError.InvalidEvents;
                        break;
                    case CombatEventKind.ShipDamaged:
                        if (e.ShipId == null || !e.Amount.HasValue || e.Amount <= 0 || !result.ShipChanges.Any(c => c.ShipId == e.ShipId && c.RemainingHull < ships[e.ShipId].ship.Hull)) return CombatSubmissionError.InvalidEvents;
                        break;
                    case CombatEventKind.ShipDestroyed:
                        if (e.ShipId == null || e.Amount != null || !result.ShipChanges.Any(c => c.ShipId == e.ShipId && c.RemainingHull == 0)) return CombatSubmissionError.InvalidEvents;
                        break;
                    case CombatEventKind.FleetRetreated:
                        if (e.FleetId == null || e.ShipId != null || e.Amount != null || !result.FleetChanges.Any(c => c.FleetId == e.FleetId && c.Disposition == FleetCombatDisposition.Retreated)) return CombatSubmissionError.InvalidEvents;
                        break;
                }
            }
            return result.Events.Count > 0 && result.Events[result.Events.Count - 1].Kind == CombatEventKind.EncounterResolved
                ? CombatSubmissionError.None : CombatSubmissionError.InvalidEvents;
        }
    }
}
