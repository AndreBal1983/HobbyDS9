using System.Collections.Generic;

namespace HobbyDS9.Contracts
{
    public enum CombatResolutionStatus { Deferred, Resolved }
    public enum CombatEventKind { EncounterResolved, ShipDamaged, ShipDestroyed, FleetRetreated }
    public enum FleetCombatDisposition { Held, Retreated, Destroyed }

    // Concrete serialization contracts only. The future session owns and validates
    // copies; callers and resolvers never receive mutable authoritative state.
    public sealed class CombatCheckpointDto
    {
        public SessionPhase Phase { get; set; }
        public int TurnNumber { get; set; }
        public long StateRevision { get; set; }
        public ulong CampaignRandomState { get; set; }
        public ulong NextEntitySequence { get; set; } = 1;
        public List<PendingCombatDto> PendingCombats { get; set; } = new List<PendingCombatDto>();
        public List<ResolvedCombatDto> ResolvedCombats { get; set; } = new List<ResolvedCombatDto>();
    }

    public sealed class PendingCombatDto
    {
        public CombatRequestDto Request { get; set; } = new CombatRequestDto();
    }

    // Buffered results are persisted until the entire batch can be applied
    // canonically. Campaign consequences never depend on submission order.
    public sealed class ResolvedCombatDto
    {
        public CombatRequestDto Request { get; set; } = new CombatRequestDto();
        public CombatResultDto Result { get; set; } = new CombatResultDto();
    }

    public sealed class CombatRequestDto
    {
        public string EncounterId { get; set; } = string.Empty;
        public int TurnNumber { get; set; }
        public string LocationSystemId { get; set; } = string.Empty;
        public string RulesVersion { get; set; } = string.Empty;
        public string ContentHash { get; set; } = string.Empty;
        public string ResolverId { get; set; } = string.Empty;
        public int ResolverVersion { get; set; }
        public string RandomAlgorithmId { get; set; } = string.Empty;
        // Encounter-local initial stream state allocated at creation, never
        // a shared reference to the campaign RNG. Frozen across retries/views.
        public ulong RandomState { get; set; }
        public List<CombatParticipantDto> Participants { get; set; } = new List<CombatParticipantDto>();
    }

    public sealed class CombatParticipantDto
    {
        public string CivilizationId { get; set; } = string.Empty;
        public string FleetId { get; set; } = string.Empty;
        public List<CombatShipDto> Ships { get; set; } = new List<CombatShipDto>();
    }

    public sealed class CombatShipDto
    {
        public string ShipId { get; set; } = string.Empty;
        public string DesignId { get; set; } = string.Empty;
        public int Hull { get; set; }
    }

    public sealed class CombatResultDto
    {
        public string EncounterId { get; set; } = string.Empty;
        public string ResolverId { get; set; } = string.Empty;
        public int ResolverVersion { get; set; }
        public CombatResolutionStatus Status { get; set; }
        public ulong FinalRandomState { get; set; }
        public List<ShipCombatChangeDto> ShipChanges { get; set; } = new List<ShipCombatChangeDto>();
        public List<FleetCombatChangeDto> FleetChanges { get; set; } = new List<FleetCombatChangeDto>();
        public List<CombatEventDto> Events { get; set; } = new List<CombatEventDto>();
    }

    public sealed class ShipCombatChangeDto
    {
        public string ShipId { get; set; } = string.Empty;
        public int RemainingHull { get; set; }
    }

    public sealed class FleetCombatChangeDto
    {
        public string FleetId { get; set; } = string.Empty;
        public FleetCombatDisposition Disposition { get; set; }
        public string? RetreatSystemId { get; set; }
    }

    public sealed class CombatEventDto
    {
        public int Sequence { get; set; }
        public CombatEventKind Kind { get; set; }
        public string? FleetId { get; set; }
        public string? ShipId { get; set; }
    }

    public interface ICombatResolver
    {
        // Pure, synchronous boundary: a detached snapshot in, ordered data out.
        // Determinism includes the resolver/rules versions and request RNG state.
        CombatResultDto Resolve(CombatRequestDto request);
    }
}
