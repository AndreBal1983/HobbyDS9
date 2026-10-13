using System;
using System.Collections.Generic;
using System.Linq;
using HobbyDS9.Contracts;
using static HobbyDS9.Simulation.CampaignTacticalReviewData;

namespace HobbyDS9.Simulation
{
    internal static class CampaignTacticalTravelData
    {
        internal static CampaignTravelOrderDto Copy(CampaignTravelOrderDto o)=>new CampaignTravelOrderDto { FleetId=o.FleetId,Intent=o.Intent,
            ContactFleetId=o.ContactFleetId,ExpectedContactSeenTurn=o.ExpectedContactSeenTurn,RouteNodeIds=new List<string>(o.RouteNodeIds) };
        internal static CampaignContactKnowledgeDto Copy(CampaignContactKnowledgeDto k)=>new CampaignContactKnowledgeDto { ActorId=k.ActorId,FleetId=k.FleetId,
            LastSeenNodeId=k.LastSeenNodeId,LastSeenTurn=k.LastSeenTurn,TrackingNodeIds=k.TrackingNodeIds.OrderBy(n=>n,StringComparer.Ordinal).ToList() };
        internal static CampaignFleetTravelReceiptDto Copy(CampaignFleetTravelReceiptDto r)=>new CampaignFleetTravelReceiptDto { FleetId=r.FleetId,OwnerId=r.OwnerId,
            Turn=r.Turn,EndNodeId=r.EndNodeId,TransitDestinationId=r.TransitDestinationId,TransitTotalCost=r.TransitTotalCost,TransitProgress=r.TransitProgress,
            CostPaid=r.CostPaid,EdgesCompleted=r.EdgesCompleted,StoppedByEncounter=r.StoppedByEncounter,StopTimeNumerator=r.StopTimeNumerator,StopTimeDenominator=r.StopTimeDenominator,EncounterId=r.EncounterId };
        internal static CampaignTacticalTravelSaveDto Copy(CampaignTacticalTravelSaveDto s)=>new CampaignTacticalTravelSaveDto {
            FormatVersion=s.FormatVersion,ProfileId=s.ProfileId,PackageHash=s.PackageHash,Phase=s.Phase,Core=CampaignTacticalReviewData.Copy(s.Core),
            Requests=s.Requests.OrderBy(r=>r.EncounterId,StringComparer.Ordinal).Select(CombatEncounterScaffold.CopyRequest).ToList(),
            Orders=s.Orders.OrderBy(o=>o.FleetId,StringComparer.Ordinal).Select(Copy).ToList(),
            Contacts=s.Contacts.OrderBy(k=>k.ActorId,StringComparer.Ordinal).ThenBy(k=>k.FleetId,StringComparer.Ordinal).Select(Copy).ToList(),
            LastMovement=s.LastMovement.OrderBy(r=>r.FleetId,StringComparer.Ordinal).Select(Copy).ToList(),
            MovementProof=s.MovementProof==null?null:new CampaignPhysicalMovementProofDto { Start=SimulationSession.CopyState(s.MovementProof.Start),
                Orders=s.MovementProof.Orders.OrderBy(o=>o.FleetId,StringComparer.Ordinal).Select(Copy).ToList(),
                Contacts=s.MovementProof.Contacts.OrderBy(k=>k.ActorId,StringComparer.Ordinal).ThenBy(k=>k.FleetId,StringComparer.Ordinal).Select(Copy).ToList() } };
        internal static void ValidatePackage(CampaignTacticalTravelPackageDto p)
        {
            Require(p!=null&&p.FormatVersion==CampaignTacticalTravelIdentity.PackageFormat&&p.ProfileId==CampaignTacticalTravelIdentity.Profile&&Hash(p.PackageHash)
                &&p.PolicyStatus=="proposed"&&p.DefaultActivationAllowed==false
                &&p.BoundaryPolicy=="arrivals-contact-departures-v1"&&p.SharedStartPolicy=="departure-first-start-v1"
                &&p.LaterArrivalPolicy=="join-connected-before-allocation-v1"&&p.ContactFreshnessTurns.HasValue&&p.ContactFreshnessTurns>=0
                &&p.ContactFreshnessTurns<=100000&&p.CorePackage!=null&&p.Hostilities!=null&&p.InitialContacts!=null&&p.InitialOrders!=null,"Unknown/ungated physical package.");
            CampaignTacticalReviewData.ValidatePackage(p.CorePackage);
            var game=p.CorePackage.Campaign!.InitialState;var defs=p.CorePackage.Campaign.Definitions;
            var graph=new WeightedNavigationGraph(defs);
            Require(p.Hostilities.All(h=>h!=null&&h.CivilizationA!=h.CivilizationB&&game.Civilizations!.Any(c=>c.CivilizationId==h.CivilizationA)
                &&game.Civilizations.Any(c=>c.CivilizationId==h.CivilizationB)),"Invalid declared hostility.");
            var keys=p.Hostilities.Select(h=>StringComparer.Ordinal.Compare(h.CivilizationA,h.CivilizationB)<0?h.CivilizationA+"|"+h.CivilizationB:h.CivilizationB+"|"+h.CivilizationA);
            Require(keys.Distinct(StringComparer.Ordinal).Count()==p.Hostilities.Count,"Duplicate declared hostility.");
            Require(p.InitialContacts.All(k=>k!=null&&k.TrackingNodeIds!=null&&game.Civilizations!.Any(c=>c.CivilizationId==k.ActorId)
                &&game.Fleets!.Any(f=>f.FleetId==k.FleetId&&CampaignPhysicalMovement.Hostile(p.Hostilities,k.ActorId,f.OwnerCivilizationId))
                &&k.LastSeenTurn>=1&&k.LastSeenTurn<=game.TurnNumber&&graph.KnownNode(game,k.ActorId,k.LastSeenNodeId)
                &&k.TrackingNodeIds.Count>0&&k.TrackingNodeIds.All(n=>graph.KnownNode(game,k.ActorId,n))
                &&k.TrackingNodeIds.Distinct(StringComparer.Ordinal).Count()==k.TrackingNodeIds.Count)
                &&p.InitialContacts.Select(k=>k.ActorId+"|"+k.FleetId).Distinct(StringComparer.Ordinal).Count()==p.InitialContacts.Count,"Invalid author-owned lawful contact grid.");
            Require(p.InitialOrders.Count==game.Fleets!.Count&&p.InitialOrders.All(o=>o!=null&&o.RouteNodeIds!=null)
                &&p.InitialOrders.Select(o=>o.FleetId).Distinct(StringComparer.Ordinal).Count()==p.InitialOrders.Count,"Missing/duplicate initial travel intentions.");
        }
    }
}
