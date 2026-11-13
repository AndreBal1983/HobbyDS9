using System.Collections.Generic;

namespace HobbyDS9.Contracts
{
    public static class CampaignPlayableFleetIdentity
    {
        public const int Format = 6;
        public const string Profile = "campaign-tactical-v2-playable-fleet-1";
        public const string EmbeddedCoreProfile = "campaign-tactical-v2-playable-fleet-owned-core-1";
    }
    public sealed class CampaignPlayableFleetPackageDto
    {
        public int? FormatVersion { get; set; }
        public string? ProfileId { get; set; }
        public string? PackageHash { get; set; }
        public string? PolicyStatus { get; set; }
        public bool? DefaultActivationAllowed { get; set; }
        // Explicit technical review-package budget, never a default campaign rule.
        public int? ReplayOperationLimit { get; set; }
        public string? OpponentActorId { get; set; }
        public string? DefenseNodeId { get; set; }
        public CampaignIntegratedPlaytestPackageDto? Integrated { get; set; }
    }
    public enum CampaignFleetOrganizationKind { Create = 1, Transfer = 2, Rename = 3 }
    public sealed class SubmitCampaignFleetOrganizationDto
    {
        public string ActorId { get; set; } = "";
        public string CommandId { get; set; } = "";
        public int? ExpectedTurn { get; set; }
        public long? ExpectedRevision { get; set; }
        public CampaignFleetOrganizationKind Kind { get; set; }
        public List<string>? ShipIds { get; set; }
        public string? TargetFleetId { get; set; }
        public string? DisplayName { get; set; }
    }
    public enum CampaignFleetOrganizationError
    {
        None, UnsupportedProfile, UnknownActor, ActorNotControlled, WrongPhase, StaleTurn,
        StaleRevision, DuplicateCommand, InvalidOrder, InvalidName, UnknownFleet, FleetNotOwned,
        UnknownShip, ShipNotOwned, NotColocated, FleetInTransit, FleetHasOrders, Overflow
    }
    public sealed class CampaignFleetOrganizationResult
    {
        public bool Accepted => Error == CampaignFleetOrganizationError.None;
        public CampaignFleetOrganizationError Error { get; }
        public long CampaignRevision { get; }
        public string? FleetId { get; }
        public CampaignFleetOrganizationResult(CampaignFleetOrganizationError error, long revision, string? fleet = null)
        { Error = error; CampaignRevision = revision; FleetId = fleet; }
    }
    public sealed class CampaignFleetNameDto
    {
        public string FleetId { get; set; } = "";
        public string DisplayName { get; set; } = "";
    }
    public sealed class CampaignPlayableOperationDto
    {
        // Exactly one carrier. Existing common operation DTO remains unchanged.
        public CampaignFrontierOperationDto? Common { get; set; }
        public SubmitCampaignFleetOrganizationDto? Organization { get; set; }
    }
    public sealed class CampaignPlayableFleetSaveDto
    {
        public int FormatVersion { get; set; }
        public string ProfileId { get; set; } = "";
        public string PackageHash { get; set; } = "";
        public CampaignIntegratedPlaytestSaveDto Integrated { get; set; } = null!;
        public List<CampaignFleetNameDto> Names { get; set; } = null!;
        public List<CampaignPlayableOperationDto> Transcript { get; set; } = null!;
    }
    public sealed class CampaignNamedFleet
    {
        public FleetSnapshot Fleet { get; }
        public string DisplayName { get; }
        public CampaignFleetOrganizationError UnavailableReason { get; }
        public bool CanOrganize => UnavailableReason == CampaignFleetOrganizationError.None;
        public CampaignNamedFleet(FleetSnapshot fleet, string name, CampaignFleetOrganizationError reason)
        { Fleet = fleet; DisplayName = name; UnavailableReason = reason; }
    }
}
