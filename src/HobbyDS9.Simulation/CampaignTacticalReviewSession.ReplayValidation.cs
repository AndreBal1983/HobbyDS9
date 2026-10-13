using System;
using System.Linq;
using HobbyDS9.Contracts;
using static HobbyDS9.Simulation.CampaignTacticalReviewData;

namespace HobbyDS9.Simulation
{
    public sealed partial class CampaignTacticalReviewSession
    {
        // Validate loaded progress by replaying at most three private local rounds from the
        // real frozen campaign request/pools. This never draws from the campaign RNG.
        private void ValidateReplay(CampaignTacticalReviewSaveDto save, CampaignTacticalBattleCheckpointDto actual, CombatRequestDto request)
        {
            Require(actual.ResolvedRounds != null && actual.ResolvedRounds.Count == actual.State.NextRound - 1
                && actual.ResolvedRounds.All(r => r != null && r.Commitments != null), "Missing private resolution provenance.");
            var replay = Allocate(save,request);
            EnsureAi(save,replay);
            foreach (var round in actual.ResolvedRounds)
            {
                Require(round.Round == replay.State.NextRound && replay.State.Completion == TacticalCompletion.None, "Invalid resolved round chronology.");
                var actors = replay.State.Formations.Where(f => f.Status == TacticalFormationStatus.Active).Select(f => f.SideId).Distinct(StringComparer.Ordinal).OrderBy(id => id,StringComparer.Ordinal);
                Require(round.Commitments.All(c => c != null && c.Orders != null)
                    && round.Commitments.Select(c => c.ActorId).OrderBy(id => id,StringComparer.Ordinal).SequenceEqual(actors), "Invalid joint round ownership.");
                foreach(var c in round.Commitments)
                {
                    Require(EntityId.TryParse(c.CommandId,out _), "Invalid historical command.");
                    var owned = replay.State.Formations.Where(f => f.SideId == c.ActorId && f.Status == TacticalFormationStatus.Active)
                        .Select(f => f.FormationId).OrderBy(id => id,StringComparer.Ordinal);
                    Require(c.Orders.All(o => ValidOrder(replay,c.ActorId,o))
                        && c.Orders.Select(o => o.FleetId).OrderBy(id => id,StringComparer.Ordinal).SequenceEqual(owned), "Forged historical fleet order.");
                    var ai = replay.Commitments.SingleOrDefault(a => a.ActorId == c.ActorId);
                    if(ai != null)
                        Require(c.CommandId == ai.CommandId && SameOrders(c.Orders,ai.Orders), "AI reacted to private human input.");
                    else Require(!c.CommandId.StartsWith("ai-command.",StringComparison.Ordinal) && save.AcceptedRoundCommands.Contains(c.CommandId,StringComparer.Ordinal),
                        "Missing accepted human resolution command.");
                }
                replay.Commitments = round.Commitments.OrderBy(c => c.ActorId,StringComparer.Ordinal).Select(Copy).ToList();
                ResolveJoint(save,replay);
            }
            var left = TacticalCombatV2State.Copy(actual.State);var right = replay.State;
            Require(left.RandomState == right.RandomState && left.Completion == right.Completion && left.NextRound == right.NextRound
                && left.Formations.Select(f => (f.FormationId,f.Status)).SequenceEqual(right.Formations.Select(f => (f.FormationId,f.Status)))
                && SameOrders(left.Formations.Select(f => new CampaignTacticalFleetOrderDto { FleetId=f.FormationId,Maneuver=Maneuver(f.StandingOrder.Maneuver),FocusFleetId=f.StandingOrder.TargetFormationId }),
                    right.Formations.Select(f => new CampaignTacticalFleetOrderDto { FleetId=f.FormationId,Maneuver=Maneuver(f.StandingOrder.Maneuver),FocusFleetId=f.StandingOrder.TargetFormationId }))
                && left.Formations.SelectMany(f => f.Ships).Select(s => (s.ShipId,s.Hull)).SequenceEqual(right.Formations.SelectMany(f => f.Ships).Select(s => (s.ShipId,s.Hull)))
                && left.Formations.SelectMany(f => f.Ships).SelectMany(s => s.Shields.Select(q => (s.ShipId,q.Sector,q.Current)))
                    .SequenceEqual(right.Formations.SelectMany(f => f.Ships).SelectMany(s => s.Shields.Select(q => (s.ShipId,q.Sector,q.Current))))
                && left.Formations.SelectMany(f => f.Ships).SelectMany(s => s.Weapons.Select(w => (s.ShipId,w.WeaponId,w.Ammunition,w.NextFireRound)))
                    .SequenceEqual(right.Formations.SelectMany(f => f.Ships).SelectMany(s => s.Weapons.Select(w => (s.ShipId,w.WeaponId,w.Ammunition,w.NextFireRound))))
                && left.Geometry.Select(g => (g.FormationA,g.FormationB,g.Range,g.FacingA,g.FacingB))
                    .SequenceEqual(right.Geometry.Select(g => (g.FormationA,g.FormationB,g.Range,g.FacingA,g.FacingB))), "Forged round state/results/RNG.");
            Require(actual.Reports.Select(r => (r.ActorId,r.Sequence,r.Round,r.Kind,r.ShipId,r.TargetShipId,r.OwnWeaponId,r.Amount,r.Sector,r.Range,r.Detail))
                .SequenceEqual(replay.Reports.Select(r => (r.ActorId,r.Sequence,r.Round,r.Kind,r.ShipId,r.TargetShipId,r.OwnWeaponId,r.Amount,r.Sector,r.Range,r.Detail))), "Forged filtered report.");
        }
        private static bool SameOrders(System.Collections.Generic.IEnumerable<CampaignTacticalFleetOrderDto> a,System.Collections.Generic.IEnumerable<CampaignTacticalFleetOrderDto> b)
            => a.OrderBy(o => o.FleetId,StringComparer.Ordinal).Select(o => (o.FleetId,o.Maneuver,o.FocusFleetId))
                .SequenceEqual(b.OrderBy(o => o.FleetId,StringComparer.Ordinal).Select(o => (o.FleetId,o.Maneuver,o.FocusFleetId)));
    }
}
