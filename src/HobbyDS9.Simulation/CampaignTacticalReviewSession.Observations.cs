using System;
using System.Collections.Generic;
using System.Linq;
using HobbyDS9.Contracts;
using static HobbyDS9.Simulation.CampaignTacticalReviewData;

namespace HobbyDS9.Simulation
{
    public sealed partial class CampaignTacticalReviewSession
    {
        public IReadOnlyList<FleetSnapshot> OwnStrategicFleets(string actor)
        {
            lock(gate)
            {
                Require(Actor(actor)==CampaignTacticalError.None,"Only a controlled human actor can inspect strategic fleets.");
                return Legacy(state).CaptureSnapshot().Fleets.Where(f=>f.OwnerCivilizationId.Value==actor).ToList().AsReadOnly();
            }
        }
        public CampaignTacticalReviewStatus CaptureStatus()
        {
            lock (gate) return new CampaignTacticalReviewStatus(state.Campaign.GameState.TurnNumber, state.Campaign.GameState.StateRevision,
                CurrentPhase, state.Battles.Count > 0 && state.Battles.All(b => b.State.Completion != TacticalCompletion.None),travel!=null);
        }
        public IReadOnlyList<CampaignTacticalFleetCloakSetting> FleetCloakSettings(string actor)
        {
            lock (gate)
            {
                Require(Actor(actor) == CampaignTacticalError.None, "Only a controlled human actor can inspect fleet settings.");
                return state.Campaign.GameState.Fleets!.Where(f => f.OwnerCivilizationId == actor).Select(f =>
                    new CampaignTacticalFleetCloakSetting(f.FleetId,state.FleetSettings.Single(s => s.FleetId == f.FleetId).ApproachCloak,
                        CurrentPhase == SessionPhase.Planning, f.ShipIds!.Where(id => cloak.Single(c => c.FitId == Fit(id).DefinitionId).Capable == true)))
                    .OrderBy(f => f.FleetId,StringComparer.Ordinal).ToList().AsReadOnly();
            }
        }
        private CampaignTacticalCondition Condition(string fleet, TacticalShipV2Dto ship)
        {
            var design = Provision(ship.ShipId).ShipDesignDefinitionId;
            return new CampaignTacticalCondition(ship.ShipId, fleet, definitions.ShipDesigns!.Single(d => d.DefinitionId == design).DisplayName,
                ship.Hull, ship.MaxHull, ship.Shields.OrderBy(q => q.Sector).Select(q => q.Current));
        }
        public IReadOnlyList<CampaignTacticalOwnShip> InspectOwnShips(string actor)
        {
            lock (gate)
            {
                Require(Actor(actor) == CampaignTacticalError.None, "Only a controlled human actor can inspect own ships.");
                return state.Campaign.GameState.Ships!.Where(s => s.OwnerCivilizationId == actor).Select(s => {
                    var pool = state.Pools.Single(p => p.ShipId == s.ShipId); var compiled = Compile(s, Fit(s.ShipId), pool);
                    var fit = Fit(s.ShipId); var cl = cloak.Single(c => c.FitId == fit.DefinitionId);
                    return new CampaignTacticalOwnShip(Condition(s.FleetId, compiled), pool, false, compiled, fit, cl.Capable!.Value, cl.ConcurrentPowerRequired!.Value);
                }).OrderBy(s => s.Condition.ShipId, StringComparer.Ordinal).ToList().AsReadOnly();
            }
        }
        public CampaignTacticalObservation Observe(string encounter, string actor)
        {
            lock (gate)
            {
                Require(Actor(actor) == CampaignTacticalError.None, "Only a controlled human actor can observe tactical state.");
                var b = state.Battles.SingleOrDefault(p => p.State.EncounterId == encounter);
                Require(b != null && b.State.Formations.Any(f => f.SideId == actor), "Actor does not own an encounter participant.");
                var own = b.State.Formations.Where(f => f.SideId == actor).SelectMany(f => f.Ships.Select(s => {
                    var fit = Fit(s.ShipId); var cl = cloak.Single(c => c.FitId == fit.DefinitionId);
                    return new CampaignTacticalOwnShip(Condition(f.FormationId,s),state.Pools.Single(p => p.ShipId == s.ShipId),
                        b.HiddenShipIds.Contains(s.ShipId,StringComparer.Ordinal),s,fit,cl.Capable!.Value,cl.ConcurrentPowerRequired!.Value);
                })).ToList();
                var visible = b.State.Formations.Where(f => f.SideId != actor).SelectMany(f => f.Ships.Where(s => s.Hull > 0
                    && !b.HiddenShipIds.Contains(s.ShipId, StringComparer.Ordinal)).Select(s => Condition(f.FormationId, s))).ToList();
                var orders = b.State.Formations.Where(f => f.SideId == actor).Select(f => new CampaignTacticalStandingOrder(
                    f.FormationId, Maneuver(f.StandingOrder.Maneuver), f.StandingOrder.TargetFormationId,
                    b.State.Completion == TacticalCompletion.None && f.Status == TacticalFormationStatus.Active && !b.Commitments.Any(c => c.ActorId == actor)));
                var commitment = b.Commitments.SingleOrDefault(c => c.ActorId == actor);
                var relations = b.State.Geometry.Where(g => b.State.Formations.Any(f => f.SideId == actor && (f.FormationId == g.FormationA || f.FormationId == g.FormationB)))
                    .Select(g => {
                        bool ownA = b.State.Formations.Single(f => f.FormationId == g.FormationA).SideId == actor;
                        return new CampaignTacticalRelation(ownA ? g.FormationA : g.FormationB,ownA ? g.FormationB : g.FormationA,
                            g.Range,ownA ? g.FacingA : g.FacingB,ownA ? g.FacingB : g.FacingA);
                    }).Where(r => visible.Any(e => e.FleetId == r.VisibleEnemyFleetId));
                return new CampaignTacticalObservation(encounter, state.Campaign.GameState.TurnNumber, b.State.NextRound,
                    b.BoundaryRevision, b.Commitments.Any(c => c.ActorId == actor), b.State.Completion != TacticalCompletion.None,
                    own, visible, orders, b.Reports.Where(r => r.ActorId == actor).Select(r => new CampaignTacticalReportEntry(r)),
                    commitment?.CommandId, commitment?.Orders.Select(o => new CampaignTacticalStandingOrder(o.FleetId, o.Maneuver, o.FocusFleetId,false))
                        ?? Array.Empty<CampaignTacticalStandingOrder>(),relations,actor,
                    CurrentRequests.Single(p => p.EncounterId == encounter).LocationNodeId);
            }
        }
        private void AddReports(CampaignTacticalBattleCheckpointDto b, IReadOnlyList<TacticalEventV2> events)
        {
            var owners = b.State.Formations.SelectMany(f => f.Ships.Select(s => (s.ShipId, f.SideId))).ToDictionary(s => s.ShipId, s => s.SideId, StringComparer.Ordinal);
            var fleets = b.State.Formations.ToDictionary(f => f.FormationId, f => f.SideId, StringComparer.Ordinal);
            foreach (var actor in fleets.Values.Distinct(StringComparer.Ordinal).OrderBy(s => s, StringComparer.Ordinal))
            foreach (var e in events)
            {
                bool own = e.ShipId != null && owners[e.ShipId] == actor;
                bool visible = e.ShipId != null && !b.HiddenShipIds.Contains(e.ShipId, StringComparer.Ordinal);
                if (e.Kind == TacticalEventKind.OrderCommitted && (e.FormationId == null || fleets[e.FormationId] != actor)) continue;
                if (e.Kind == TacticalEventKind.Maneuver || e.Kind == TacticalEventKind.ArmorMitigation) continue;
                if (e.Kind == TacticalEventKind.FiringOpportunity && !own) continue;
                if (e.Kind == TacticalEventKind.WeaponDischarged && !own && !visible)
                {
                    if (e.TargetShipId != null && owners[e.TargetShipId] == actor && e.Detail == TacticalWeaponClass.Torpedo.ToString())
                        b.Reports.Add(new CampaignTacticalActorReportDto { ActorId = actor, Sequence = b.Reports.Count(r => r.ActorId == actor),
                            Round = e.Round, Kind = "IncomingTorpedo", TargetShipId = e.TargetShipId });
                    continue;
                }
                if (e.ShipId != null && !own && !visible) continue;
                // No raw Detail/chance/opponent package IDs or reserve totals reach player reports.
                b.Reports.Add(new CampaignTacticalActorReportDto { ActorId = actor, Sequence = b.Reports.Count(r => r.ActorId == actor), Round = e.Round,
                    Kind = e.Detail.StartsWith("focus-fallback:", StringComparison.Ordinal) ? "FocusFallback" : e.Kind.ToString(),
                    ShipId = e.ShipId,
                    TargetShipId = e.TargetShipId != null && (owners[e.TargetShipId] == actor || !b.HiddenShipIds.Contains(e.TargetShipId, StringComparer.Ordinal)) ? e.TargetShipId : null,
                    OwnWeaponId = own ? e.WeaponId : null,
                    Sector = e.Sector,
                    Range = e.TargetShipId != null && !b.HiddenShipIds.Contains(e.TargetShipId,StringComparer.Ordinal) ? e.Range : null,
                    Amount = own || e.Kind == TacticalEventKind.ShieldImpact || e.Kind == TacticalEventKind.HullDamage ? e.Amount : 0,
                    Detail = e.Detail.StartsWith("focus-fallback:",StringComparison.Ordinal) ? "Preferred fleet unavailable for this opportunity; a visible hostile alternative was acquired." : "" });
            }
        }
    }
}
