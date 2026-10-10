using System;
using System.Collections.Generic;
using System.Linq;
using HobbyDS9.Contracts;

namespace HobbyDS9.Simulation
{
    public sealed partial class SimulationSession
    {
        // Outer lists are immutable; DTOs and their nested lists are detached copies.
        public IReadOnlyList<CombatRequestDto> GetPendingCombatRequests()
        {
            lock (gate) return state.PendingCombats!.OrderBy(c => c.Request.LocationSystemId, StringComparer.Ordinal).ThenBy(c => c.Request.EncounterId, StringComparer.Ordinal)
                .Select(c => CombatEncounterScaffold.CopyRequest(c.Request)).ToList().AsReadOnly();
        }
        public IReadOnlyList<ResolvedCombatDto> GetResolvedCombats()
        {
            lock (gate) return state.ResolvedCombats!.OrderBy(c => c.Request.LocationSystemId, StringComparer.Ordinal).ThenBy(c => c.Request.EncounterId, StringComparer.Ordinal)
                .Select(c => new ResolvedCombatDto { Request = CombatEncounterScaffold.CopyRequest(c.Request), Result = CombatEncounterScaffold.CopyResult(c.Result) }).ToList().AsReadOnly();
        }
        public CombatSubmissionResult SubmitCombatResult(CombatResultDto? result, int expectedTurn, long expectedRevision)
        {
            lock (gate)
            {
                if (executingMutation) throw new InvalidOperationException("Nested session mutation is not supported.");
                CombatSubmissionResult Reject(CombatSubmissionError code) => new CombatSubmissionResult(result?.EncounterId, CombatSubmissionStatus.Rejected, code, state.TurnNumber, state.StateRevision);
                if (state.Phase != SessionPhase.AwaitingCombat) return Reject(CombatSubmissionError.WrongPhase);
                if (expectedTurn != state.TurnNumber) return Reject(CombatSubmissionError.StaleTurn);
                if (expectedRevision != state.StateRevision) return Reject(CombatSubmissionError.StaleRevision);
                if (result == null || !EntityId.TryParse(result.EncounterId, out _)) return Reject(CombatSubmissionError.InvalidEncounterId);
                if (state.ResolvedCombats!.Any(c => c.Request.EncounterId == result.EncounterId)) return Reject(CombatSubmissionError.AlreadyResolved);
                var pending = state.PendingCombats!.SingleOrDefault(c => c.Request.EncounterId == result.EncounterId);
                if (pending == null) return Reject(CombatSubmissionError.UnknownEncounter);
                if (result.ResolverId != pending.Request.ResolverId || result.ResolverVersion != pending.Request.ResolverVersion) return Reject(CombatSubmissionError.ResolverMismatch);
                // D1 supports no-change buffers. Generic consequences are connected in D2.
                if (result.ShipChanges == null || result.FleetChanges == null || result.ShipChanges.Count != 0 || result.FleetChanges.Count != 0) return Reject(CombatSubmissionError.MalformedResult);
                if (result.Status != CombatResolutionStatus.Deferred && state.StateRevision == long.MaxValue) return Reject(CombatSubmissionError.RevisionExhausted);
                executingMutation = true;
                try
                {
                    CombatCheckpointDto checkpoint;
                    try { checkpoint = CombatEncounterScaffold.RecordResult(CampaignCombatState.ToCheckpoint(state), result); }
                    catch (ArgumentException) { return Reject(CombatSubmissionError.MalformedResult); }
                    if (result.Status == CombatResolutionStatus.Deferred) return new CombatSubmissionResult(result.EncounterId, CombatSubmissionStatus.Deferred, CombatSubmissionError.None, state.TurnNumber, state.StateRevision);
                    var staging = CopyState(state); CampaignCombatState.SetCheckpoint(staging, checkpoint);
                    FoundationValidator.ValidateSave(ToSave(staging), definitions, compatibility).ThrowIfInvalid(nameof(staging));
                    state = CopyState(staging);
                    return new CombatSubmissionResult(result.EncounterId, CombatSubmissionStatus.Buffered, CombatSubmissionError.None, state.TurnNumber, state.StateRevision);
                }
                finally { executingMutation = false; }
            }
        }
        public TurnResult FinalizeCombat(int expectedTurn, long expectedRevision)
        {
            lock (gate)
            {
                if (executingMutation) throw new InvalidOperationException("Nested session mutation is not supported.");
                TurnResult Reject(TurnErrorCode error) => new TurnResult(TurnStatus.Rejected, error, state.TurnNumber, state.TurnNumber, state.StateRevision, state.Phase!.Value, Array.Empty<StrategicEvent>(), Array.Empty<CommandResult>());
                if (state.Phase != SessionPhase.AwaitingCombat) return Reject(TurnErrorCode.NotAwaitingCombat);
                if (expectedTurn != state.TurnNumber) return Reject(TurnErrorCode.StaleTurn);
                if (expectedRevision != state.StateRevision) return Reject(TurnErrorCode.StaleRevision);
                if (state.PendingCombats!.Count != 0) return Reject(TurnErrorCode.UnresolvedCombats);
                if (state.ResolvedCombats!.Count == 0) return Reject(TurnErrorCode.InvalidCombatBatch);
                if (state.TurnNumber == int.MaxValue) return Reject(TurnErrorCode.TurnNumberExhausted);
                if (state.StateRevision == long.MaxValue) return Reject(TurnErrorCode.RevisionExhausted);
                executingMutation = true;
                try
                {
                    var staging = CopyState(state);
                    foreach (var combat in staging.ResolvedCombats!)
                        if (combat.Result.ShipChanges.Count != 0 || combat.Result.FleetChanges.Count != 0) throw new InvalidOperationException("Generic consequence application belongs to D2.");
                    var completedTurn = staging.TurnNumber;
                    staging.PendingCombats!.Clear(); staging.ResolvedCombats!.Clear(); staging.Phase = SessionPhase.Planning;
                    staging.TurnNumber = checked(completedTurn + 1); staging.StateRevision = checked(staging.StateRevision + 1); staging.AcceptedCommandIds!.Clear();
                    FoundationValidator.ValidateSave(ToSave(staging), definitions, compatibility).ThrowIfInvalid(nameof(staging));
                    state = CopyState(staging);
                    return new TurnResult(TurnStatus.Completed, TurnErrorCode.None, completedTurn, state.TurnNumber, state.StateRevision, state.Phase!.Value, Array.Empty<StrategicEvent>(), Array.Empty<CommandResult>());
                }
                finally { executingMutation = false; }
            }
        }
        private SaveEnvelopeDto ToSave(GameStateDto staging) => new SaveEnvelopeDto { SaveFormatVersion = FoundationVersions.SaveFormatVersion,
            ScenarioId = compatibility.ScenarioId, RulesVersion = compatibility.RulesVersion, ContentHash = compatibility.ContentHash, GameState = staging };
    }
}
