using System.Collections.Generic;

namespace HobbyDS9.Contracts
{
    public enum CommandKind
    {
        Unknown = 0, EnqueueProduction = 1, CancelProduction = 2, SetEngineeringTarget = 8, SetFleetRoute = 4, SetWorldGovernancePolicy = 5, SetResearchAllocation = 6, SetScienceFocus = 7
    }

    public enum CommandStatus { Rejected = 0, Accepted = 1 }

    public enum CommandErrorCode
    {
        None = 0, InvalidEnvelope = 1, InvalidCommandId = 2, InvalidActorId = 3,
        UnknownActor = 4, ActorNotControlled = 5, StaleTurn = 6, DuplicateCommand = 7,
        StaleRevision = 8, UnknownCommandKind = 9, InvalidPayload = 10,
        UnsupportedCommand = 11, RevisionExhausted = 12, InvalidTarget = 13, CombatPending = 14, UnknownEngineeringApplication = 15, EngineeringAlreadyMature = 16,
        EngineeringPrerequisiteMissing = 17, EngineeringTargetUnchanged = 18, UnknownColony = 19, ColonyNotOwned = 20, UnknownShipDesign = 21,
        DesignEngineeringPrerequisiteMissing = 22, InsufficientCredits = 23, InsufficientStrategicResource = 24,
        UnknownQueueItem = 25, UnknownFleet = 26, FleetNotOwned = 27, InvalidRoute = 28, ReservedCommandId = 29, ShipbuildingUnavailable = 30, UnknownGovernancePolicy = 31, GovernancePolicyUnchanged = 32, FleetInTransit = 33, NavigationUnknown = 34, InvalidResearchAllocation = 35, ResearchAllocationUnchanged = 36, InvalidScienceDiscipline = 37, ScienceFocusMaxed = 38, ScienceFocusUnchanged = 39, EngineeringScienceRequirementMissing = 40
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
        public SetResearchAllocationPayload? SetResearchAllocation { get; set; }
        public SetScienceFocusPayload? SetScienceFocus { get; set; }
        public SetEngineeringTargetPayload? SetEngineeringTarget { get; set; }
        public SetWorldGovernancePolicyPayload? SetWorldGovernancePolicy { get; set; }
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

    public sealed class SetEngineeringTargetPayload
    {
        public string? TargetDefinitionId { get; set; }
    }

    public sealed class SetFleetRoutePayload
    {
        public string FleetId { get; set; } = string.Empty;
        public List<string>? RouteNodeIds { get; set; }
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
