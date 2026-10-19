using System.Collections.Generic;
using System.Linq;

namespace HobbyDS9.Contracts
{
    // Provisional opt-in review policy, not a planetary survey or sensor model.
    public static class CampaignFrontierIdentity
    {
        public const int Format = 5;
        public const string Profile = "campaign-tactical-v2-frontier-review-1";
        public const string Policy = "arrival-public-lanes-provisional-v1";
        public const string EmbeddedCoreProfile = "campaign-tactical-v2-frontier-owned-core-1";
        public const int TranscriptLimit = 4096;
    }
    public sealed class CampaignFrontierPackageDto
    {
        public int? FormatVersion { get; set; }
        public string? ProfileId { get; set; }
        public string? PackageHash { get; set; }
        public string? PolicyId { get; set; }
        public string? PolicyStatus { get; set; }
        public bool? DefaultActivationAllowed { get; set; }
        public CampaignIntegratedPlaytestPackageDto? Integrated { get; set; }
        public List<string>? PublicLaneIds { get; set; }
    }
    public sealed class CampaignFrontierReceiptDto
    {
        public string ActorId { get; set; } = "";
        public string FleetId { get; set; } = "";
        public string ArrivalNodeId { get; set; } = "";
        public int Turn { get; set; }
        public List<string> ChartedNodeIds { get; set; } = null!;
        public List<string> ChartedLaneIds { get; set; } = null!;
    }
    public enum FrontierOperationKind { Travel = 1, Strategic = 2, Cloak = 3, Advance = 4, Round = 5, Finalize = 6 }
    // Trusted host carrier, replayed through actual authority on load. No world snapshots.
    public sealed class CampaignFrontierOperationDto
    {
        public FrontierOperationKind Kind { get; set; }
        public SubmitCampaignTravelOrderDto? Travel { get; set; }
        public CommandEnvelope? Strategic { get; set; }
        public CommitCampaignTacticalRoundDto? Round { get; set; }
        public string? ActorId { get; set; }
        public string? CommandId { get; set; }
        public string? FleetId { get; set; }
        public bool? Enabled { get; set; }
        public int? Turn { get; set; }
        public long? Revision { get; set; }
    }
    public sealed class CampaignFrontierSaveDto
    {
        public int? FormatVersion { get; set; }
        public string? ProfileId { get; set; }
        public string? PackageHash { get; set; }
        public string? PolicyId { get; set; }
        public CampaignIntegratedPlaytestSaveDto? Integrated { get; set; }
        public List<CampaignFrontierReceiptDto>? Discoveries { get; set; }
        public List<CampaignFrontierOperationDto>? Transcript { get; set; }
    }
    public enum FrontierNodeKind { Waypoint = 1, System = 2 }
    public sealed class CampaignFrontierNode
    {
        public string NodeId { get; }
        public string DisplayName { get; }
        public FrontierNodeKind Kind { get; }
        public CampaignFrontierNode(string id,string name,FrontierNodeKind kind) { NodeId=id;DisplayName=name;Kind=kind; }
    }
    public sealed class CampaignFrontierLane
    {
        public string LaneId { get; }
        public string EndpointA { get; }
        public string EndpointB { get; }
        public long Cost { get; }
        public CampaignFrontierLane(string id,string a,string b,long cost) { LaneId=id;EndpointA=a;EndpointB=b;Cost=cost; }
    }
    public sealed class CampaignFrontierReceipt
    {
        public string FleetId { get; }
        public string ArrivalNodeId { get; }
        public int Turn { get; }
        public IReadOnlyList<string> ChartedNodeIds { get; }
        public IReadOnlyList<string> ChartedLaneIds { get; }
        public CampaignFrontierReceipt(CampaignFrontierReceiptDto r)
        { FleetId=r.FleetId;ArrivalNodeId=r.ArrivalNodeId;Turn=r.Turn;
            ChartedNodeIds=r.ChartedNodeIds.ToList().AsReadOnly();ChartedLaneIds=r.ChartedLaneIds.ToList().AsReadOnly(); }
    }
    public sealed class CampaignDiscoveryOverview
    {
        public string ActorId { get; }
        public string PolicyId => CampaignFrontierIdentity.Policy;
        public IReadOnlyList<CampaignFrontierNode> Nodes { get; }
        public IReadOnlyList<CampaignFrontierLane> Lanes { get; }
        public IReadOnlyList<CampaignFrontierReceipt> Receipts { get; }
        public CampaignDiscoveryOverview(string actor,IEnumerable<CampaignFrontierNode> nodes,IEnumerable<CampaignFrontierLane> lanes,
            IEnumerable<CampaignFrontierReceipt> receipts)
        { ActorId=actor;Nodes=nodes.ToList().AsReadOnly();Lanes=lanes.ToList().AsReadOnly();Receipts=receipts.ToList().AsReadOnly(); }
    }
}
