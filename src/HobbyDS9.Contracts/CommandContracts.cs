using System.Collections.Generic;

namespace HobbyDS9.Contracts
{
    public enum CommandKind
    {
        Unknown = 0, EnqueueProduction = 1, CancelProduction = 2, SetResearchTarget = 3, SetFleetRoute = 4
    }

    public enum CommandStatus { Rejected = 0, Accepted = 1 }

    public enum CommandErrorCode
    {
        None = 0, InvalidEnvelope = 1, InvalidCommandId = 2, InvalidActorId = 3,
        UnknownActor = 4, ActorNotControlled = 5, StaleTurn = 6, DuplicateCommand = 7,
        StaleRevision = 8, UnknownCommandKind = 9, InvalidPayload = 10,
        UnsupportedCommand = 11, RevisionExhausted = 12, InvalidTarget = 13, CombatPending = 14, UnknownResearchNode = 15, ResearchAlreadyUnlocked = 16,
        ResearchPrerequisiteMissing = 17, ResearchTargetUnchanged = 18, UnknownColony = 19, ColonyNotOwned = 20, UnknownShipDesign = 21,
        DesignResearchPrerequisiteMissing = 22, InsufficientCredits = 23, InsufficientMaterials = 24,
        UnknownQueueItem = 25, UnknownFleet = 26, FleetNotOwned = 27, InvalidRoute = 28
    }

    // Closed discriminated union: exactly one payload must match Kind. There
    // are no object payloads, CLR type names or gameplay handlers in this contract.
    public sealed class CommandEnvelope
    {
        public string CommandId { get; set; } = string.Empty;
        public string ActorCivilizationId { get; set; } = string.Empty;
        public int? ExpectedTurn { get; set; }
        public long? ExpectedRevision { get; set; }
        public CommandKind Kind { get; set; }
        public EnqueueProductionPayload? EnqueueProduction { get; set; }
        public CancelProductionPayload? CancelProduction { get; set; }
        public SetResearchTargetPayload? SetResearchTarget { get; set; }
        public SetFleetRoutePayload? SetFleetRoute { get; set; }
    }

    public sealed class EnqueueProductionPayload
    {
        public string ColonyId { get; set; } = string.Empty;
        public string DefinitionId { get; set; } = string.Empty;
    }

    public sealed class CancelProductionPayload
    {
        public string ColonyId { get; set; } = string.Empty;
        public string QueueItemId { get; set; } = string.Empty;
    }

    public sealed class SetResearchTargetPayload
    {
        public string TargetDefinitionId { get; set; } = string.Empty;
    }

    public sealed class SetFleetRoutePayload
    {
        public string FleetId { get; set; } = string.Empty;
        public List<string>? RouteSystemIds { get; set; }
    }

    public sealed class CommandResult
    {
        public string? CommandId { get; }
        public CommandStatus Status { get; }
        public CommandErrorCode ErrorCode { get; }
        public int TurnNumber { get; }
        public long StateRevision { get; }
        public bool Accepted => Status == CommandStatus.Accepted;

        public CommandResult(string? commandId, CommandStatus status, CommandErrorCode errorCode, int turnNumber, long stateRevision)
        {
            CommandId = commandId; Status = status; ErrorCode = errorCode; TurnNumber = turnNumber; StateRevision = stateRevision;
        }
    }
}
