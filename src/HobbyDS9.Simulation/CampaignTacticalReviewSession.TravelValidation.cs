using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using HobbyDS9.Contracts;
using static HobbyDS9.Simulation.CampaignTacticalReviewData;

namespace HobbyDS9.Simulation
{
    public sealed partial class CampaignTacticalReviewSession
    {
        private static void RequireTravelShape(CampaignTacticalTravelSaveDto s)
        {
            Require(s!=null&&s.FormatVersion==CampaignTacticalTravelIdentity.SaveFormat&&s.ProfileId==CampaignTacticalTravelIdentity.Profile&&Hash(s.PackageHash)
                &&s.Core!=null&&s.Core.Campaign!=null&&s.Core.Pools!=null&&s.Core.FleetSettings!=null&&s.Core.Battles!=null&&s.Core.AcceptedRoundCommands!=null
                &&s.Requests!=null&&s.Orders!=null&&s.Contacts!=null&&s.LastMovement!=null&&Enum.IsDefined(typeof(SessionPhase),s.Phase)
                &&s.Core.Battles.All(b=>b!=null&&b.State!=null&&b.HiddenShipIds!=null&&b.Commitments!=null&&b.Reports!=null&&b.ResolvedRounds!=null)
                &&s.Core.Pools.All(p=>p!=null&&p.Shields!=null&&p.Ammunition!=null)
                &&s.Orders.All(o=>o!=null&&o.RouteNodeIds!=null)&&s.Contacts.All(k=>k!=null&&k.TrackingNodeIds!=null)
                &&s.LastMovement.All(r=>r!=null),"Malformed physical save.");
            foreach(var request in s.Requests)CombatEncounterScaffold.ValidateRequest(request,true);
            if(s.MovementProof!=null)Require(s.MovementProof.Start!=null&&s.MovementProof.Orders!=null&&s.MovementProof.Contacts!=null
                &&s.MovementProof.Orders.All(o=>o!=null&&o.RouteNodeIds!=null)&&s.MovementProof.Contacts.All(k=>k!=null&&k.TrackingNodeIds!=null),"Malformed movement proof.");
        }
        private void ValidateContactRows(GameStateDto game,IReadOnlyList<CampaignContactKnowledgeDto> rows)
        {
            Require(rows.Count==authoredContactGrids!.Count&&rows.Select(k=>k.ActorId+"|"+k.FleetId).Distinct(StringComparer.Ordinal).Count()==rows.Count,"Missing/duplicate lawful contact grid.");
            var graph=new WeightedNavigationGraph(definitions);
            foreach(var k in rows)
            {
                var source=authoredContactGrids.SingleOrDefault(p=>p.ActorId==k.ActorId&&p.FleetId==k.FleetId);
                Require(source!=null&&k.LastSeenTurn>=source.LastSeenTurn&&k.LastSeenTurn<=game.TurnNumber&&graph.KnownNode(game,k.ActorId,k.LastSeenNodeId)
                    &&k.TrackingNodeIds.OrderBy(n=>n,StringComparer.Ordinal).SequenceEqual(source.TrackingNodeIds.OrderBy(n=>n,StringComparer.Ordinal)),"Unlawful/forged contact knowledge.");
            }
        }
        private void ValidateTravelSave(CampaignTacticalTravelSaveDto s)
        {
            RequireTravelShape(s);
            var game=s.Core.Campaign.GameState;
            ValidateContactRows(game,s.Contacts);
            Require(s.Orders.Count==game.Fleets!.Count&&s.Orders.Select(o=>o.FleetId).Distinct(StringComparer.Ordinal).Count()==s.Orders.Count
                &&s.Orders.All(o=>ValidTravelOrder(game,o,s.Contacts,allowStale:true)&&o.RouteNodeIds.SequenceEqual(game.Fleets.Single(f=>f.FleetId==o.FleetId).RouteNodeIds!)),
                "Invalid persisted travel intentions.");
            Require(s.Phase==SessionPhase.Planning?s.Requests.Count==0&&s.Core.Battles.Count==0:s.Requests.Count>0&&s.Core.Battles.Count==s.Requests.Count,
                "Physical phase/batch mismatch.");
            var fleetIds=new HashSet<string>(StringComparer.Ordinal);var encounterIds=new HashSet<string>(StringComparer.Ordinal);
            foreach(var request in s.Requests)
            {
                CombatEncounterScaffold.ValidateRequest(request,true);
                Require(encounterIds.Add(request.EncounterId)&&new EntityId(request.EncounterId).TryGetAllocationSequence(out var sequence)
                    &&sequence<game.NextEntitySequence&&!game.EntityIdentities.Any(e=>new EntityId(e.EntityId).TryGetAllocationSequence(out var allocated)&&allocated==sequence)
                    &&request.TurnNumber==game.TurnNumber&&request.ContentHash==compatibility.ContentHash&&request.RulesVersion==compatibility.RulesVersion
                    &&request.ResolverId==CampaignCombatRules.ResolverId&&request.ResolverVersion==CampaignCombatRules.ResolverVersion,"Invalid physical request identity.");
                foreach(var participant in request.Participants)
                {
                    var f=game.Fleets.SingleOrDefault(p=>p.FleetId==participant.FleetId);
                    Require(f!=null&&fleetIds.Add(f.FleetId)&&f.TransitTotalCost==0&&f.LocationNodeId==request.LocationNodeId
                        &&f.OwnerCivilizationId==participant.CivilizationId&&f.RouteNodeIds!.Count==0
                        &&f.ShipIds!.OrderBy(id=>id,StringComparer.Ordinal).SequenceEqual(participant.Ships.Select(p=>p.ShipId).OrderBy(id=>id,StringComparer.Ordinal)),"Invalid/still-moving physical participant.");
                    foreach(var ship in participant.Ships)
                    {
                        var actual=game.Ships!.Single(p=>p.ShipId==ship.ShipId);var design=CampaignShipDesignResolver.Ship(game,definitions,actual);
                        Require(actual.CurrentHull==ship.Hull&&design.DefinitionId==ship.DesignId&&design.MaxHull==ship.MaxHull
                            &&CombatProfiles.Equal(design.CombatProfile,ship.CombatProfile),"Forged frozen physical ship.");
                    }
                }
                var owners=request.Participants.Select(p=>p.CivilizationId).Distinct(StringComparer.Ordinal).ToList();
                Require(owners.All(a=>owners.All(b=>a==b||CampaignPhysicalMovement.Hostile(declaredHostilities!,a,b))),"Tactical owner/side contract cannot represent neutral connected components.");
            }
            Require(s.LastMovement.Select(r=>r.FleetId).Distinct(StringComparer.Ordinal).Count()==s.LastMovement.Count,"Duplicate movement receipt.");
            foreach(var r in s.LastMovement)
            {
                Require(KnownFleetOwner(r.FleetId) is string owner&&owner==r.OwnerId&&r.Turn>=1&&r.Turn<=game.TurnNumber
                    &&r.CostPaid>=0&&r.EdgesCompleted>=0&&r.TransitProgress>=0&&r.TransitTotalCost>=0&&r.TransitProgress<r.TransitTotalCost+new BigInteger(1)
                    &&BigInteger.TryParse(r.StopTimeNumerator,NumberStyles.None,CultureInfo.InvariantCulture,out var n)
                    &&BigInteger.TryParse(r.StopTimeDenominator,NumberStyles.None,CultureInfo.InvariantCulture,out var d)&&d>0&&n>=0&&n<=d
                    &&BigInteger.GreatestCommonDivisor(n,d)==1,"Invalid exact movement receipt.");
                if(r.StoppedByEncounter)Require(r.TransitTotalCost==0&&r.TransitProgress==0&&r.TransitDestinationId==null&&r.EncounterId!=null,"Invalid participant movement stop.");
                var current=game.Fleets.SingleOrDefault(f=>f.FleetId==r.FleetId);
                Require(current==null?s.Phase==SessionPhase.Planning&&(r.StoppedByEncounter || playableHash!=null&&playableRetiredFleets.Contains(r.FleetId)):
                    current.LocationNodeId==r.EndNodeId&&current.TransitTotalCost==r.TransitTotalCost&&current.TransitProgress==r.TransitProgress
                    &&(current.TransitTotalCost==0?r.TransitDestinationId==null:current.RouteNodeIds!.FirstOrDefault()==r.TransitDestinationId),
                    "Campaign location differs from authoritative physical receipt.");
            }
            ValidateSave(s.Core,s.Requests);
            if(s.MovementProof==null)Require(s.LastMovement.Count==0&&s.Requests.Count==0&&game.TurnNumber==initialTurn
                &&ContactKeys(s.Contacts).SequenceEqual(ContactKeys(authoredContactGrids!)),"Missing authoritative movement/contact proof.");
            else
            {
                var proof=s.MovementProof;
                FoundationValidator.ValidateGameState(proof.Start,definitions).ThrowIfInvalid(nameof(proof));
                Require((long)proof.Start.TurnNumber+(s.Phase==SessionPhase.Planning?1:0)==game.TurnNumber,
                    "Movement proof belongs to a different strategic turn.");
                ValidateContactRows(proof.Start,proof.Contacts);
                Require(proof.Orders.Count==proof.Start.Fleets!.Count&&proof.Orders.Select(o=>o.FleetId).Distinct(StringComparer.Ordinal).Count()==proof.Orders.Count
                    &&proof.Orders.All(o=>ValidTravelOrder(proof.Start,o,proof.Contacts,allowStale:true)
                        &&o.RouteNodeIds.SequenceEqual(proof.Start.Fleets.Single(f=>f.FleetId==o.FleetId).RouteNodeIds!)),"Invalid movement input provenance.");
                var replay=SimulationSession.CopyState(proof.Start);
                var motion=CampaignPhysicalMovement.Execute(replay,definitions,proof.Orders,proof.Contacts,declaredHostilities!,contactFreshness);
                var expectedRequests=FreezePhysical(replay,motion);
                var expectedContacts=proof.Contacts.Select(CampaignTacticalTravelData.Copy).ToList();
                RefreshPhysicalContacts(expectedRequests,proof.Start.TurnNumber,expectedContacts);
                Require(ContactKeys(expectedContacts).SequenceEqual(ContactKeys(s.Contacts)),"Forged contact freshness/location.");
                Require(motion.Receipts.Select(ReceiptKey).SequenceEqual(s.LastMovement.Select(ReceiptKey)),"Forged movement/stop receipts.");
                if(s.Phase==SessionPhase.AwaitingCombat)
                {
                    Require(expectedRequests.Count==s.Requests.Count&&expectedRequests.Zip(s.Requests.OrderBy(r=>r.EncounterId,StringComparer.Ordinal),SameRequest).All(v=>v),
                        "Unlawful/forged physical encounter batch.");
                    var adjusted=SimulationSession.CopyState(game);adjusted.StateRevision=replay.StateRevision;
                    SimulationSession.RequirePhaseChanges(replay,adjusted,allowEconomy:false);
                }
            }
        }
        private static IEnumerable<(string,string,string,int)> ContactKeys(IEnumerable<CampaignContactKnowledgeDto> contacts)=>
            contacts.OrderBy(k=>k.ActorId,StringComparer.Ordinal).ThenBy(k=>k.FleetId,StringComparer.Ordinal).Select(k=>(k.ActorId,k.FleetId,k.LastSeenNodeId,k.LastSeenTurn));
        private static string ReceiptKey(CampaignFleetTravelReceiptDto r)=>string.Join("|",r.FleetId,r.OwnerId,r.Turn.ToString(CultureInfo.InvariantCulture),
            r.EndNodeId,r.TransitDestinationId,r.TransitTotalCost.ToString(CultureInfo.InvariantCulture),r.TransitProgress.ToString(CultureInfo.InvariantCulture),
            r.CostPaid.ToString(CultureInfo.InvariantCulture),r.EdgesCompleted.ToString(CultureInfo.InvariantCulture),r.StoppedByEncounter?"1":"0",r.StopTimeNumerator,r.StopTimeDenominator,r.EncounterId);
        private static bool SameRequest(CombatRequestDto a,CombatRequestDto b)
            =>a.EncounterId==b.EncounterId&&a.LocationNodeId==b.LocationNodeId&&a.RandomState==b.RandomState
            &&a.Participants.Select(p=>(p.FleetId,p.CivilizationId)).SequenceEqual(b.Participants.Select(p=>(p.FleetId,p.CivilizationId)))
            &&a.Participants.SelectMany(p=>p.Ships).Select(p=>(p.ShipId,p.DesignId,p.Hull,p.MaxHull)).SequenceEqual(b.Participants.SelectMany(p=>p.Ships).Select(p=>(p.ShipId,p.DesignId,p.Hull,p.MaxHull)));
    }
}
