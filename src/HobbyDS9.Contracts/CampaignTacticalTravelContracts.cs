using System.Collections.Generic;
using System.Linq;
using System;

namespace HobbyDS9.Contracts
{
    public static class CampaignTacticalTravelIdentity
    {
        public const int PackageFormat = 2, SaveFormat = 2;
        public const string Profile = "campaign-tactical-v2-physical-review-1";
        public const string MovementResolver = "strategic-travel.exact-review";
    }
    public enum CampaignTravelIntent { Stay = 0, StayEngage = 1, Leave = 2, Follow = 3 }
    public sealed class CampaignTacticalTravelPackageDto
    {
        public int? FormatVersion { get; set; }
        public string? ProfileId { get; set; }
        public string? PackageHash { get; set; }
        public string? PolicyStatus { get; set; }
        public bool? DefaultActivationAllowed { get; set; }
        public string? BoundaryPolicy { get; set; }
        public string? SharedStartPolicy { get; set; }
        public string? LaterArrivalPolicy { get; set; }
        public int? ContactFreshnessTurns { get; set; }
        public CampaignTacticalReviewPackageDto? CorePackage { get; set; }
        public List<CampaignDeclaredHostilityDto>? Hostilities { get; set; }
        public List<CampaignContactKnowledgeDto>? InitialContacts { get; set; }
        public List<CampaignTravelOrderDto>? InitialOrders { get; set; }
    }
    public sealed class CampaignDeclaredHostilityDto
    {
        public string CivilizationA { get; set; } = "";
        public string CivilizationB { get; set; } = "";
    }
    public sealed class CampaignContactKnowledgeDto
    {
        public string ActorId { get; set; } = "";
        public string FleetId { get; set; } = "";
        public string LastSeenNodeId { get; set; } = "";
        public int LastSeenTurn { get; set; }
        public List<string> TrackingNodeIds { get; set; } = null!;
    }
    public sealed class CampaignTravelOrderDto
    {
        public string FleetId { get; set; } = "";
        public CampaignTravelIntent Intent { get; set; }
        public string? ContactFleetId { get; set; }
        public int? ExpectedContactSeenTurn { get; set; }
        public List<string> RouteNodeIds { get; set; } = null!;
    }
    public sealed class SubmitCampaignTravelOrderDto
    {
        public string ActorId { get; set; } = "";
        public string CommandId { get; set; } = "";
        public int? ExpectedTurn { get; set; }
        public long? ExpectedRevision { get; set; }
        public CampaignTravelOrderDto? Order { get; set; }
    }
    public sealed class CampaignFleetTravelReceiptDto
    {
        public string FleetId { get; set; } = "";
        public string OwnerId { get; set; } = "";
        public int Turn { get; set; }
        public string EndNodeId { get; set; } = "";
        public string? TransitDestinationId { get; set; }
        public long TransitTotalCost { get; set; }
        public long TransitProgress { get; set; }
        public long CostPaid { get; set; }
        public long EdgesCompleted { get; set; }
        public bool StoppedByEncounter { get; set; }
        public string StopTimeNumerator { get; set; } = "";
        public string StopTimeDenominator { get; set; } = "";
        public string? EncounterId { get; set; }
    }
    public sealed class CampaignTacticalTravelSaveDto
    {
        public int FormatVersion { get; set; }
        public string ProfileId { get; set; } = "";
        public string PackageHash { get; set; } = "";
        // Complete physical save needs this root; Core alone is not a physical save.
        public CampaignTacticalReviewSaveDto Core { get; set; } = null!;
        public SessionPhase Phase { get; set; }
        public List<CombatRequestDto> Requests { get; set; } = null!;
        public List<CampaignTravelOrderDto> Orders { get; set; } = null!;
        public List<CampaignContactKnowledgeDto> Contacts { get; set; } = null!;
        public List<CampaignFleetTravelReceiptDto> LastMovement { get; set; } = null!;
        public CampaignPhysicalMovementProofDto? MovementProof { get; set; }
    }
    public sealed class CampaignPhysicalMovementProofDto
    {
        public GameStateDto Start { get; set; } = null!;
        public List<CampaignTravelOrderDto> Orders { get; set; } = null!;
        public List<CampaignContactKnowledgeDto> Contacts { get; set; } = null!;
    }
    public sealed class CampaignKnownContact
    {
        public string FleetId { get; }
        public string LastSeenNodeId { get; }
        public int LastSeenTurn { get; }
        public bool Fresh { get; }
        public IReadOnlyList<string> TrackingNodeIds { get; }
        public CampaignKnownContact(CampaignContactKnowledgeDto k, bool fresh)
        { FleetId=k.FleetId;LastSeenNodeId=k.LastSeenNodeId;LastSeenTurn=k.LastSeenTurn;Fresh=fresh;
            TrackingNodeIds=new List<string>(k.TrackingNodeIds).AsReadOnly(); }
    }
    public sealed class CampaignOwnTravelOrder
    {
        public string FleetId { get; }
        public CampaignTravelIntent Intent { get; }
        public string? ContactFleetId { get; }
        public int? ExpectedContactSeenTurn { get; }
        public IReadOnlyList<string> RouteNodeIds { get; }
        public CampaignOwnTravelOrder(CampaignTravelOrderDto o)
        { FleetId=o.FleetId;Intent=o.Intent;ContactFleetId=o.ContactFleetId;ExpectedContactSeenTurn=o.ExpectedContactSeenTurn;
            RouteNodeIds=new List<string>(o.RouteNodeIds).AsReadOnly(); }
    }
    public sealed class CampaignOwnTravelReceipt
    {
        public string FleetId { get; }
        public int Turn { get; }
        public string EndNodeId { get; }
        public string? TransitDestinationId { get; }
        public long TransitTotalCost { get; }
        public long TransitProgress { get; }
        public long CostPaid { get; }
        public long EdgesCompleted { get; }
        public bool StoppedByEncounter { get; }
        public string StopTimeNumerator { get; }
        public string StopTimeDenominator { get; }
        public string? EncounterId { get; }
        public CampaignOwnTravelReceipt(CampaignFleetTravelReceiptDto r)
        { FleetId=r.FleetId;Turn=r.Turn;EndNodeId=r.EndNodeId;TransitDestinationId=r.TransitDestinationId;TransitTotalCost=r.TransitTotalCost;
            TransitProgress=r.TransitProgress;CostPaid=r.CostPaid;EdgesCompleted=r.EdgesCompleted;StoppedByEncounter=r.StoppedByEncounter;
            StopTimeNumerator=r.StopTimeNumerator;StopTimeDenominator=r.StopTimeDenominator;EncounterId=r.EncounterId; }
    }
}
