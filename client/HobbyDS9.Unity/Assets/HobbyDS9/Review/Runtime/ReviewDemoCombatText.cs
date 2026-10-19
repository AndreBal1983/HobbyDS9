#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using HobbyDS9.Contracts;
using HobbyDS9.TacticalReviewClient;

namespace HobbyDS9.UnityReview
{
    // Display aliases and summaries of an already actor-filtered frame only.
    // Never accepts a save, trusted roster, resolver state, private Detail or orders.
    public sealed class ReviewDemoCombatText
    {
        private string actor="";private long generation=-1;
        private readonly Dictionary<string,string> ownFleets=new Dictionary<string,string>(StringComparer.Ordinal);
        private readonly Dictionary<string,string> ownShips=new Dictionary<string,string>(StringComparer.Ordinal);
        private readonly Dictionary<string,string> enemyFleets=new Dictionary<string,string>(StringComparer.Ordinal);
        private readonly Dictionary<string,string> enemyShips=new Dictionary<string,string>(StringComparer.Ordinal);
        private readonly Dictionary<string,string> contacts=new Dictionary<string,string>(StringComparer.Ordinal);
        private void Observe(ReviewFrame frame)
        {
            if(actor!=frame.Actor||generation!=frame.Generation){actor=frame.Actor;generation=frame.Generation;ownFleets.Clear();ownShips.Clear();enemyFleets.Clear();enemyShips.Clear();contacts.Clear();}
            foreach(var id in frame.OwnStrategicFleets.Select(f=>f.FleetId.Value).Concat(frame.OwnShips.Select(s=>s.Condition.FleetId)).Distinct().OrderBy(s=>s,StringComparer.Ordinal))
                if(!ownFleets.ContainsKey(id))ownFleets.Add(id,"Fleet "+(ownFleets.Count+1));
            foreach(var ship in frame.OwnShips.OrderBy(s=>s.Condition.ShipId,StringComparer.Ordinal))
                if(!ownShips.ContainsKey(ship.Condition.ShipId))ownShips.Add(ship.Condition.ShipId,OwnType(frame,ship.Condition)+" "+(ownShips.Count+1));
            foreach(var ship in frame.Battle?.VisibleEnemies.OrderBy(s=>s.ShipId,StringComparer.Ordinal)??Enumerable.Empty<CampaignTacticalCondition>())
            {
                if(!enemyFleets.ContainsKey(ship.FleetId))enemyFleets.Add(ship.FleetId,"Enemy fleet "+(enemyFleets.Count+1));
                if(!enemyShips.ContainsKey(ship.ShipId))enemyShips.Add(ship.ShipId,"Enemy "+PublicType(ship.Type,"contact")+" "+(enemyShips.Count+1));
            }
            foreach(var id in (frame.Travel?.KnownContacts.Select(c=>c.FleetId)??Enumerable.Empty<string>()).OrderBy(s=>s,StringComparer.Ordinal))
                if(!contacts.ContainsKey(id))contacts.Add(id,"Known contact "+(contacts.Count+1));
        }
        private static string PublicType(string type,string fallback)
        {
            // Exact presentation alias for the authored demo descriptor, not a
            // role/faction/stat-based inference or a model/equipment mapping.
            if(type=="PROVISIONAL v2 equipment source cruiser; inert v1 combat fallback"||type=="Cruiser")return "Cruiser";
            if(type=="Scout"||type=="Surveyor"||type=="Transport")return type;
            return fallback;
        }
        private static string OwnType(ReviewFrame frame,CampaignTacticalCondition ship)
        {
            var identity=frame.Strategy?.Ships.FirstOrDefault(s=>s.ShipId.Value==ship.ShipId);
            return identity?.ShipDesignDefinitionId.Value=="ship-design.source-cruiser"?"Cruiser":PublicType(ship.Type,"Ship");
        }
        public string Fleet(ReviewFrame frame,string? id)
        {
            Observe(frame);if(id==null)return "No preference";
            if(ownFleets.TryGetValue(id,out var own))return own;
            if(frame.Battle?.VisibleEnemies.Any(s=>s.FleetId==id)==true&&enemyFleets.TryGetValue(id,out var enemy))return enemy;
            if(frame.Travel?.KnownContacts.Any(c=>c.FleetId==id)==true&&contacts.TryGetValue(id,out var contact))return contact+" (last seen)";
            return "Previously selected fleet (unavailable)";
        }
        public string Ship(ReviewFrame frame,string? id)
        {
            Observe(frame);if(id==null)return "Unidentified source";
            if(ownShips.TryGetValue(id,out var own))return own;
            if(frame.Battle?.VisibleEnemies.Any(s=>s.ShipId==id)==true&&enemyShips.TryGetValue(id,out var enemy))return enemy;
            return "No current ship observation";
        }
        public string Node(ReviewFrame frame,string id)=>frame.Strategy?.Navigation.Nodes.FirstOrDefault(n=>n.NodeId.Value==id)?.DisplayName??"Known sector";
        public string Encounter(ReviewFrame frame,string id)=>"Battle "+(frame.Encounters.ToList().IndexOf(id)+1);
        public static string Maneuver(string value)=>value=="Hold"?"Maintain distance":value=="FightingWithdrawal"?"Fighting withdrawal":value;
        public string Focus(ReviewFrame frame,string value)=>value=="retain standing focus"?"Keep standing preference":value=="clear focus"?"No fleet preference":Fleet(frame,value);
        public string Orders(ReviewController controller)
        {
            var frame=controller.Frame;var text=new StringBuilder("Your fleet orders\n");
            foreach(var order in frame.Battle?.StandingOrders??Array.Empty<CampaignTacticalStandingOrder>())
            {
                text.AppendLine(Fleet(frame,order.FleetId)+" · standing: "+Maneuver(order.Maneuver.ToString())+" · "+Fleet(frame,order.FocusFleetId));
                var edit=controller.Draft.FirstOrDefault(e=>e.Fleet==order.FleetId);
                if(edit!=null)text.AppendLine("Unsent: "+Maneuver(edit.Maneuver.ToString())+" · "+Fleet(frame,edit.Focus));
            }
            return text.Append("Focus is a preference; firing targets are selected automatically.").ToString();
        }
        public string Diagram(ReviewFrame frame)
        {
            var battle=frame.Battle;
            if(battle==null)return frame.Status.Phase==SessionPhase.Planning?"Campaign Planning · no active battle selected. Fleets move on End turn.":"Choose one of your battles. Hidden participants are not inferred.";
            var text=new StringBuilder(Node(frame,battle.LocationNodeId)+" · "+(battle.Complete?"Battle complete":"Round "+battle.Round+" of up to 3")+"\n");
            foreach(var pair in battle.Relations)text.AppendLine(Fleet(frame,pair.OwnFleetId)+" → "+Fleet(frame,pair.VisibleEnemyFleetId)+": "+pair.Range+" · own facing "+pair.OwnFacing);
            if(battle.VisibleEnemies.Count==0)text.AppendLine("No visible enemy. Proceed cautiously; hidden identities and counts are unavailable.");
            return text.ToString();
        }
        public string Stocks(ReviewFrame frame,string? selected=null)
        {
            var text=new StringBuilder(frame.Battle==null?"Current campaign condition\n":"Current battle condition · staged until aftermath\n");
            var own=frame.OwnShips.Where(s=>selected==null||s.Condition.ShipId==selected).ToList();
            foreach(var ship in own)
            {
                text.AppendLine(Ship(frame,ship.Condition.ShipId)+FormattableString.Invariant($" · Hull {ship.Condition.Hull} / {ship.Condition.MaxHull}"));
                for(int i=0;i<ship.Condition.Shields.Count;i++)text.AppendLine(FormattableString.Invariant($"{(TacticalSector)i}: {ship.Condition.Shields[i]} / {ship.ShieldMaximum[i]}"));
                foreach(var weapon in ship.Weapons.Where(w=>w.Class==TacticalWeaponClass.Torpedo))text.AppendLine(FormattableString.Invariant($"Torpedoes: {weapon.Ammunition} / {weapon.MagazineCapacity}"));
            }
            if(own.Count==0)text.AppendLine("No owned ship selected in this observation.");
            return text.Append("Hull, shields and ammunition persist. No automatic between-battle refill.").ToString();
        }
        public string Inspection(ReviewFrame frame,string? selected)
        {
            if(frame.OwnShips.Any(s=>s.Condition.ShipId==selected))return Stocks(frame,selected);
            var enemy=frame.Battle?.VisibleEnemies.FirstOrDefault(s=>s.ShipId==selected);
            if(enemy==null)return "No current lawful ship observation. Hidden identity or condition is not inferred.";
            return Ship(frame,enemy.ShipId)+FormattableString.Invariant($" · Hull {enemy.Hull} / {enemy.MaxHull}\n")+"Fore / Port / Starboard / Aft: "+string.Join(" / ",enemy.Shields)+"\nEnemy ammunition and upcoming orders are not disclosed.";
        }
        public string CompactOwn(ReviewFrame frame,string selected)
        {
            var ship=frame.OwnShips.FirstOrDefault(s=>s.Condition.ShipId==selected);if(ship==null)return "";
            var text=new StringBuilder(Ship(frame,selected)+FormattableString.Invariant($" · Hull {ship.Condition.Hull}/{ship.Condition.MaxHull}\n"));
            for(int i=0;i<ship.Condition.Shields.Count;i++){text.Append(FormattableString.Invariant($"{(TacticalSector)i} {ship.Condition.Shields[i]}/{ship.ShieldMaximum[i]}"));text.Append(i%2==1?"\n":" · ");}
            foreach(var w in ship.Weapons.Where(w=>w.Class==TacticalWeaponClass.Torpedo))text.AppendLine(FormattableString.Invariant($"Torpedoes {w.Ammunition}/{w.MagazineCapacity}"));
            return text.ToString();
        }
        public string LatestRound(ReviewFrame frame)
        {
            Observe(frame);var events=frame.Battle?.Reports;
            if(events==null||events.Count==0)return "No resolved-round report yet. Review your fleet orders, then commit one round.";
            int round=events.Max(e=>e.Round);var latest=events.Where(e=>e.Round==round).ToList();
            BigInteger ownHull=BigInteger.Zero,observedHull=BigInteger.Zero,shieldImpact=BigInteger.Zero;
            foreach(var e in latest)
            {
                if(e.Kind=="HullDamage"){observedHull+=e.Amount;if(e.ShipId!=null&&ownShips.ContainsKey(e.ShipId))ownHull+=e.Amount;}
                if(e.Kind=="ShieldImpact")shieldImpact+=e.Amount;
            }
            var text=new StringBuilder("Round "+round+" resolved\n");
            text.AppendLine(FormattableString.Invariant($"Your hull loss: {ownHull} · total observed hull loss: {observedHull}"));
            text.AppendLine(FormattableString.Invariant($"Observed shield absorption: {shieldImpact} · your recorded shots: {latest.Count(e=>e.Kind=="WeaponDischarged"&&e.OwnWeaponId!=null)}"));
            text.AppendLine("Observed ships destroyed: "+latest.Count(e=>e.Kind=="ShipDestroyed")+" · torpedo interceptions: "+latest.Count(e=>e.Kind=="TorpedoIntercepted"));
            if(latest.Any(e=>e.Kind=="IncomingTorpedo"))text.AppendLine("Incoming torpedoes were reported without exposing hidden launchers.");
            if(latest.Any(e=>e.FocusFallbackCause.HasValue))text.AppendLine("Preferred focus was unavailable for a reported opportunity; a visible alternative was used. Your standing preference is retained.");
            return text.Append("Your filtered report only · recorded results, not a firing forecast.").ToString();
        }
        public string TravelDraft(ReviewController controller)
        {
            var draft=controller.TravelDraft;if(draft==null)return "No unsent strategic order.";
            var intent=draft.Intent==CampaignTravelIntent.Stay?"Remain here":draft.Intent==CampaignTravelIntent.StayEngage?"Stay / seek combat":draft.Intent==CampaignTravelIntent.Leave?"Move along the selected route":"Follow the selected known contact";
            return Fleet(controller.Frame,draft.Fleet)+": "+intent+" · "+string.Join(" → ",draft.Route.Select(n=>Node(controller.Frame,n)));
        }
    }
}
