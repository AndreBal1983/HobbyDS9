using System;
using System.Collections.Generic;
using System.Linq;
using HobbyDS9.Contracts;
using static HobbyDS9.Simulation.CampaignTacticalReviewData;

namespace HobbyDS9.Simulation
{
    public sealed partial class CampaignTacticalReviewSession
    {
        private readonly List<CampaignBattlePlayback> playableHistory = new List<CampaignBattlePlayback>();
        private CampaignPlaybackBoundary PlaybackBoundary(TacticalEncounterV2Dto battle, IReadOnlyCollection<string> hidden, string actor)
        {
            var own = battle.Formations.Where(f => f.SideId == actor)
                .SelectMany(f => f.Ships.Select(s => Condition(f.FormationId, s))).ToList();
            var enemy = battle.Formations.Where(f => f.SideId != actor)
                .SelectMany(f => f.Ships.Where(s => !hidden.Contains(s.ShipId)).Select(s => Condition(f.FormationId, s))).ToList();
            var relations = battle.Geometry.Select(g => {
                bool a = battle.Formations.Single(f => f.FormationId == g.FormationA).SideId == actor;
                return new CampaignTacticalRelation(a ? g.FormationA : g.FormationB, a ? g.FormationB : g.FormationA,
                    g.Range, a ? g.FacingA : g.FacingB, a ? g.FacingB : g.FacingA);
            }).Where(g => own.Any(s => s.FleetId == g.OwnFleetId) && enemy.Any(s => s.FleetId == g.VisibleEnemyFleetId));
            var identities = own.Concat(enemy).OrderBy(s => s.ShipId, StringComparer.Ordinal).Select(s => {
                var provision = Provision(s.ShipId);
                var civilization = state.Campaign.GameState.Civilizations!.Single(c => c.CivilizationId == provision.OwnerCivilizationId);
                return new CampaignPlaybackShipIdentity(s.ShipId, civilization.DefinitionId, provision.ShipDesignDefinitionId!);
            });
            return new CampaignPlaybackBoundary(own, enemy, relations, identities);
        }
        private static IEnumerable<CampaignPlaybackEvent> PlaybackEvents(TacticalEncounterV2Dto before, IReadOnlyCollection<string> hidden,
            string actor, IReadOnlyList<TacticalEventV2> events)
        {
            var ships = before.Formations.SelectMany(f => f.Ships.Select(s => (Fleet: f, Ship: s))).ToDictionary(x => x.Ship.ShipId, StringComparer.Ordinal);
            bool Own(string? id) => id != null && ships[id].Fleet.SideId == actor;
            bool Visible(string? id) => id != null && (Own(id) || !hidden.Contains(id));
            bool FleetVisible(string? id) => id != null && before.Formations.Any(f => f.FormationId == id
                && (f.SideId == actor || f.Ships.Any(s => s.Hull > 0 && !hidden.Contains(s.ShipId))));
            int sequence = 0;
            foreach (var e in events)
            {
                CampaignPlaybackEventKind? kind = null;
                switch (e.Kind)
                {
                    case TacticalEventKind.RoundStarted: kind = CampaignPlaybackEventKind.RoundStarted; break;
                    case TacticalEventKind.OrderCommitted:
                        if (before.Formations.Any(f => f.FormationId == e.FormationId && f.SideId == actor))
                            kind = CampaignPlaybackEventKind.OwnOrderCommitted;
                        break;
                    case TacticalEventKind.Maneuver:
                        if (FleetVisible(e.FormationId) && FleetVisible(e.TargetFormationId)) kind = CampaignPlaybackEventKind.Maneuver;
                        break;
                    case TacticalEventKind.WeaponDischarged:
                        if (Visible(e.ShipId)) kind = CampaignPlaybackEventKind.WeaponFired;
                        else if (Own(e.TargetShipId) && e.Detail == TacticalWeaponClass.Torpedo.ToString()) kind = CampaignPlaybackEventKind.IncomingTorpedo;
                        break;
                    case TacticalEventKind.InterceptionAttempt: kind = CampaignPlaybackEventKind.InterceptionAttempt; break;
                    case TacticalEventKind.TorpedoIntercepted: kind = CampaignPlaybackEventKind.TorpedoIntercepted; break;
                    case TacticalEventKind.AttackHit: kind = CampaignPlaybackEventKind.Hit; break;
                    case TacticalEventKind.AttackMissed: kind = CampaignPlaybackEventKind.Miss; break;
                    case TacticalEventKind.ShieldImpact: kind = CampaignPlaybackEventKind.ShieldDamage; break;
                    case TacticalEventKind.HullDamage: kind = CampaignPlaybackEventKind.HullDamage; break;
                    case TacticalEventKind.ShipDestroyed: kind = CampaignPlaybackEventKind.ShipDestroyed; break;
                    case TacticalEventKind.ShieldRecharged: kind = CampaignPlaybackEventKind.ShieldRecharged; break;
                    case TacticalEventKind.FormationDestroyed: kind = CampaignPlaybackEventKind.FormationDestroyed; break;
                    case TacticalEventKind.Retreated: kind = CampaignPlaybackEventKind.Retreated; break;
                    case TacticalEventKind.BrokenThrough: kind = CampaignPlaybackEventKind.BrokenThrough; break;
                    case TacticalEventKind.ObjectiveProgress: kind = CampaignPlaybackEventKind.ObjectiveProgress; break;
                    case TacticalEventKind.ObjectiveCompleted: kind = CampaignPlaybackEventKind.ObjectiveCompleted; break;
                    case TacticalEventKind.EncounterCompleted: kind = CampaignPlaybackEventKind.EncounterCompleted; break;
                }
                if (e.Detail.StartsWith("focus-fallback:", StringComparison.Ordinal) && Own(e.ShipId)) kind = CampaignPlaybackEventKind.FocusFallback;
                if (!kind.HasValue) continue;
                if (e.ShipId != null && !Visible(e.ShipId) && !Own(e.TargetShipId)) continue;
                if (e.ShipId == null && e.FormationId != null && !FleetVisible(e.FormationId)) continue;
                var source = Visible(e.ShipId) ? e.ShipId : null;
                var sourceFleet = source != null ? ships[source].Fleet.FormationId : e.ShipId == null && FleetVisible(e.FormationId) ? e.FormationId : null;
                var target = Visible(e.TargetShipId) ? e.TargetShipId : null;
                var targetFleet = target != null ? ships[target].Fleet.FormationId : e.TargetShipId == null && FleetVisible(e.TargetFormationId) ? e.TargetFormationId : null;
                bool condition = e.Kind == TacticalEventKind.ShieldImpact || e.Kind == TacticalEventKind.HullDamage
                    || e.Kind == TacticalEventKind.ShipDestroyed || e.Kind == TacticalEventKind.ShieldRecharged;
                if (condition) { target = source; targetFleet = sourceFleet; source = null; sourceFleet = null; }
                TacticalWeaponClass? weaponClass = null;
                if (kind == CampaignPlaybackEventKind.IncomingTorpedo) weaponClass = TacticalWeaponClass.Torpedo;
                else if (source != null && e.WeaponId != null)
                    weaponClass = ships[source].Ship.Weapons.Single(w => w.WeaponId == e.WeaponId).Class;
                // Exact visible condition deltas only. Attack damage rating/probabilities and opponent reserves are not UI data.
                long amount = condition ? e.Amount : 0;
                yield return new CampaignPlaybackEvent(before.EncounterId, e.Round, sequence++, kind.Value, source, sourceFleet,
                    target, targetFleet, weaponClass, e.Sector,
                    source != null && target != null || kind == CampaignPlaybackEventKind.Maneuver ? e.Range : null, amount);
            }
        }
        public CampaignBattlePlayback CaptureBattlePlayback(string encounter, string actor)
        {
            lock (gate)
            {
                Require(playableHash != null && Actor(actor) == CampaignTacticalError.None, "Only the controlled playable actor can request playback.");
                var actual = state.Battles.SingleOrDefault(b => b.State.EncounterId == encounter);
                if (actual == null)
                {
                    var historical = playableHistory.SingleOrDefault(b => b.EncounterId == encounter && b.ActorId == actor);
                    Require(historical != null, "No owned current or historical encounter.");
                    return historical!;
                }
                Require(actual.State.Formations.Any(f => f.SideId == actor), "No owned encounter participant.");
                var request = CurrentRequests.Single(r => r.EncounterId == encounter);
                var replay = Allocate(state, request);
                var initial = PlaybackBoundary(replay.State, replay.HiddenShipIds, actor);
                var rounds = new List<CampaignRoundPlayback>();
                foreach (var resolved in actual.ResolvedRounds)
                {
                    var before = TacticalCombatV2State.Copy(replay.State);
                    var hidden = replay.HiddenShipIds.ToList();
                    var start = PlaybackBoundary(before, hidden, actor);
                    replay.Commitments = resolved.Commitments.Select(Copy).ToList();
                    IReadOnlyList<TacticalEventV2>? events = null;
                    ResolveJoint(state, replay, result => events = result.Events);
                    rounds.Add(new CampaignRoundPlayback(resolved.Round, start, PlaybackBoundary(replay.State, replay.HiddenShipIds, actor),
                        PlaybackEvents(before, hidden, actor, events!)));
                }
                var all = rounds.SelectMany(r => r.Events).ToList();
                var known = new[] { initial }.Concat(rounds.Select(r => r.After)).SelectMany(b => b.OwnShips.Concat(b.VisibleEnemies))
                    .GroupBy(s => s.ShipId, StringComparer.Ordinal).Select(g => g.Last()).OrderBy(s => s.ShipId, StringComparer.Ordinal);
                var stats = known.Select(s => new CampaignBattleShipStatistics(s.ShipId, s.FleetId,
                    initial.OwnShips.Any(o => o.ShipId == s.ShipId), all));
                return new CampaignBattlePlayback(actor, encounter, request.LocationNodeId, request.TurnNumber,
                    actual.State.Completion != TacticalCompletion.None, initial, rounds, stats, Observe(encounter, actor).OwnShips);
            }
        }
        public IReadOnlyList<CampaignBattlePlayback> GetBattleHistory(string actor)
        {
            lock (gate)
            {
                Require(playableHash != null && Actor(actor) == CampaignTacticalError.None, "Only the controlled playable actor can inspect history.");
                return playableHistory.Where(b => b.ActorId == actor).ToList().AsReadOnly();
            }
        }
        private List<CampaignBattlePlayback> PreparePlayableHistory() => playableHash == null ? new List<CampaignBattlePlayback>()
            : state.Battles.OrderBy(b => b.State.EncounterId, StringComparer.Ordinal).SelectMany(b =>
                b.State.Formations.Select(f => f.SideId).Distinct(StringComparer.Ordinal).OrderBy(a => a, StringComparer.Ordinal)
                    .Where(a => controllers[a] == ControllerKind.Human).Select(a => CaptureBattlePlayback(b.State.EncounterId, a))).ToList();
    }
}
