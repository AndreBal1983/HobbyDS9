using System;
using System.Collections.Generic;

namespace HobbyDS9.Contracts
{
    public enum SessionPhase { Planning = 0, AwaitingCombat = 1 }
    public enum TurnStatus { Rejected = 0, Completed = 1, AwaitingCombat = 2 }
    public enum TurnErrorCode
    {
        None = 0, CombatPending = 1, StaleTurn = 2, StaleRevision = 3,
        TurnNumberExhausted = 4, RevisionExhausted = 5, PhaseFailed = 6, NotAwaitingCombat = 7, UnresolvedCombats = 8, InvalidCombatBatch = 9
    }

    // An ordered, immutable event carrier. Phase 3B emits no events; future
    // systems will define stable event codes without CLR type discriminators.
    public sealed class StrategicEvent
    {
        public int Sequence { get; }
        public int TurnNumber { get; }
        public string EventCode { get; }
        public string? SubjectId { get; }

        public StrategicEvent(int sequence, int turnNumber, string eventCode, string? subjectId)
        {
            if (sequence < 0 || turnNumber <= 0) throw new ArgumentOutOfRangeException(nameof(sequence));
            EntityId.FromAuthored(eventCode);
            if (subjectId != null && !EntityId.TryParse(subjectId, out _)) throw new ArgumentException("Invalid event subject ID.", nameof(subjectId));
            Sequence = sequence; TurnNumber = turnNumber; EventCode = eventCode; SubjectId = subjectId;
        }
    }

    public sealed class TurnResult
    {
        public TurnStatus Status { get; }
        public TurnErrorCode ErrorCode { get; }
        // The authoritative turn at entry, even when a stale request is rejected.
        public int AttemptedTurnNumber { get; }
        // Authoritative stamps/phase after the operation. AwaitingCombat will
        // retain AttemptedTurnNumber; Completed opens AttemptedTurnNumber + 1.
        public int TurnNumber { get; }
        public long StateRevision { get; }
        public SessionPhase Phase { get; }
        public int? CompletedTurnNumber => Status == TurnStatus.Completed ? AttemptedTurnNumber : (int?)null;
        public IReadOnlyList<StrategicEvent> Events { get; }
        public IReadOnlyList<CommandResult> CommandResults { get; }

        public TurnResult(TurnStatus status, TurnErrorCode errorCode, int attemptedTurnNumber, int turnNumber,
            long stateRevision, SessionPhase phase, IReadOnlyList<StrategicEvent> events, IReadOnlyList<CommandResult> commandResults)
        {
            Status = status; ErrorCode = errorCode; AttemptedTurnNumber = attemptedTurnNumber;
            TurnNumber = turnNumber; StateRevision = stateRevision; Phase = phase;
            Events = new List<StrategicEvent>(events).AsReadOnly();
            CommandResults = new List<CommandResult>(commandResults).AsReadOnly();
        }
    }
}
