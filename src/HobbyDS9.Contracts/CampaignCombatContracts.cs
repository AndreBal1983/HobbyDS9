using System.Collections.Generic;

namespace HobbyDS9.Contracts
{
    public static class CampaignCombatRules
    {
        public const string ResolverId = "combat-resolver.no-loss";
        public const int ResolverVersion = 1;
    }
    public enum CombatSubmissionStatus { Rejected = 0, Deferred = 1, Buffered = 2 }
    public enum CombatSubmissionError
    {
        None = 0, WrongPhase = 1, StaleTurn = 2, StaleRevision = 3, InvalidEncounterId = 4,
        UnknownEncounter = 5, AlreadyResolved = 6, ResolverMismatch = 7, MalformedResult = 8,
        Nonparticipant = 9, InvalidHull = 10, InvalidRetreat = 11, InvalidEvents = 12, RevisionExhausted = 13
    }
    public sealed class CombatSubmissionResult
    {
        public string? EncounterId { get; }
        public CombatSubmissionStatus Status { get; }
        public CombatSubmissionError ErrorCode { get; }
        public int TurnNumber { get; }
        public long StateRevision { get; }
        public CombatSubmissionResult(string? encounterId, CombatSubmissionStatus status, CombatSubmissionError errorCode, int turnNumber, long stateRevision)
        { EncounterId = encounterId; Status = status; ErrorCode = errorCode; TurnNumber = turnNumber; StateRevision = stateRevision; }
    }
    public sealed class CombatEncounterSnapshot
    {
        public EntityId EncounterId { get; }
        public EntityId LocationSystemId { get; }
        public int TurnNumber { get; }
        public string ResolverId { get; }
        public int ResolverVersion { get; }
        public ulong RandomState { get; }
        public bool IsResolved { get; }
        public IReadOnlyList<CombatParticipantSnapshot> Participants { get; }
        public CombatEncounterSnapshot(CombatRequestDto request, bool isResolved)
        {
            EncounterId = new EntityId(request.EncounterId); LocationSystemId = new EntityId(request.LocationSystemId);
            TurnNumber = request.TurnNumber; ResolverId = request.ResolverId; ResolverVersion = request.ResolverVersion;
            RandomState = request.RandomState; IsResolved = isResolved;
            Participants = request.Participants.ConvertAll(p => new CombatParticipantSnapshot(p)).AsReadOnly();
        }
    }
    public sealed class CombatParticipantSnapshot
    {
        public EntityId CivilizationId { get; }
        public EntityId FleetId { get; }
        public IReadOnlyList<CombatShipSnapshot> Ships { get; }
        public CombatParticipantSnapshot(CombatParticipantDto participant)
        { CivilizationId = new EntityId(participant.CivilizationId); FleetId = new EntityId(participant.FleetId); Ships = participant.Ships.ConvertAll(s => new CombatShipSnapshot(s)).AsReadOnly(); }
    }
    public sealed class CombatShipSnapshot
    {
        public EntityId ShipId { get; }
        public DefinitionId ShipDesignDefinitionId { get; }
        public long CurrentHull { get; }
        public long MaxHull { get; }
        public ShipCombatProfileSnapshot CombatProfile { get; }
        public CombatShipSnapshot(CombatShipDto ship) { ShipId = new EntityId(ship.ShipId); ShipDesignDefinitionId = new DefinitionId(ship.DesignId); CurrentHull = ship.Hull; MaxHull = ship.MaxHull; CombatProfile = new ShipCombatProfileSnapshot(ship.CombatProfile!); }
    }
}
