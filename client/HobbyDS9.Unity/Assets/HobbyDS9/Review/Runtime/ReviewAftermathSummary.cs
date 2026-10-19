#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using HobbyDS9.Contracts;
using HobbyDS9.TacticalReviewClient;

namespace HobbyDS9.UnityReview
{
    // One session-local display receipt, never a save or command/result import.
    // Retains only owned participants; no opponent observations or report trace.
    public sealed class ReviewAftermathSummary
    {
        public string Actor { get; }
        public long Generation { get; }
        public string EncounterId { get; }
        public string Location { get; }
        public int BattleTurn { get; }
        public int PlanningTurn { get; }
        public IReadOnlyList<string> LostShipIds { get; }
        public IReadOnlyList<CampaignTacticalOwnShip> Survivors { get; }
        private readonly IReadOnlyDictionary<string,string> names;
        private ReviewAftermathSummary(ReviewFrame before,ReviewFrame after)
        {
            var battle=before.Battle!;Actor=before.Actor;Generation=before.Generation;EncounterId=battle.EncounterId;
            Location=before.Strategy?.Navigation.Nodes.FirstOrDefault(n=>n.NodeId.Value==battle.LocationNodeId)?.DisplayName??"Known encounter sector";
            BattleTurn=battle.Turn;PlanningTurn=after.Status.Turn;
            var readable=new ReviewDemoCombatText();
            names=battle.OwnShips.ToDictionary(s=>s.Condition.ShipId,s=>readable.Ship(before,s.Condition.ShipId),StringComparer.Ordinal);
            LostShipIds=battle.OwnShips.Where(s=>s.Condition.Hull==0).Select(s=>s.Condition.ShipId).OrderBy(id=>id,StringComparer.Ordinal).ToList().AsReadOnly();
            var ownIds=new HashSet<string>(battle.OwnShips.Where(s=>s.Condition.Hull>0).Select(s=>s.Condition.ShipId),StringComparer.Ordinal);
            Survivors=after.OwnShips.Where(s=>ownIds.Contains(s.Condition.ShipId)).OrderBy(s=>s.Condition.ShipId,StringComparer.Ordinal).ToList().AsReadOnly();
        }
        public string ShipName(string id)=>names.TryGetValue(id,out var name)?name:"Own participant";
        internal bool Applies(ReviewFrame frame,bool closed)=>!closed&&frame.Actor==Actor&&frame.Generation==Generation&&frame.Status.Turn==PlanningTurn&&frame.Status.Phase==SessionPhase.Planning&&frame.Battle==null;
        internal static ReviewAftermathSummary? Confirmed(ReviewFrame before,ReviewFrame after)
        {
            var battle=before.Battle;
            if(battle==null||!battle.Complete||battle.ActorId!=before.Actor||before.Actor!=after.Actor||before.Generation!=after.Generation
                ||before.Status.Turn==int.MaxValue||after.Status.Turn!=before.Status.Turn+1||after.Status.CampaignRevision<=before.Status.CampaignRevision
                ||after.Status.Phase!=SessionPhase.Planning||after.Battle!=null)return null;
            // Confirm the owned selected-encounter consequences, never unrelated fleets or enemies.
            foreach(var own in battle.OwnShips)
            {
                var actual=after.OwnShips.FirstOrDefault(s=>s.Condition.ShipId==own.Condition.ShipId);
                if(own.Condition.Hull==0){if(actual!=null)return null;continue;}
                if(actual==null||!after.OwnStrategicFleets.Any(f=>f.FleetId.Value==actual.Condition.FleetId&&f.LocationNodeId.Value==battle.LocationNodeId)
                    ||actual.Condition.Hull!=own.Condition.Hull||!actual.Condition.Shields.SequenceEqual(own.Condition.Shields)
                    ||own.Weapons.Where(w=>w.Class==TacticalWeaponClass.Torpedo).Any(w=>!actual.Weapons.Any(a=>a.WeaponId==w.WeaponId&&a.Ammunition==w.Ammunition)))return null;
            }
            return new ReviewAftermathSummary(before,after);
        }
    }
}
