using System;
using System.Collections.Generic;
using System.Linq;
using HobbyDS9.Contracts;

namespace HobbyDS9.Simulation
{
    public sealed class SimulationSession
    {
        private readonly object gate = new object();
        private bool executingCommand;
        private GameStateDto state;
        private readonly DefinitionCatalogDto definitions;
        private readonly SaveCompatibility compatibility;
        private readonly HashSet<EntityId> actors;
        private readonly HashSet<EntityId> controlledActors;
        private readonly Dictionary<CommandKind, ICommandHandler> handlers;

        public SimulationSession(SaveEnvelopeDto save, DefinitionCatalogDto definitions, SaveCompatibility compatibility,
            IEnumerable<EntityId> actorCivilizationIds, IEnumerable<EntityId> controlledActorCivilizationIds)
            : this(save, definitions, compatibility, actorCivilizationIds, controlledActorCivilizationIds,
                new Dictionary<CommandKind, ICommandHandler>()) { }

        internal SimulationSession(SaveEnvelopeDto save, DefinitionCatalogDto definitions, SaveCompatibility compatibility,
            IEnumerable<EntityId> actorCivilizationIds, IEnumerable<EntityId> controlledActorCivilizationIds,
            IReadOnlyDictionary<CommandKind, ICommandHandler> commandHandlers)
        {
            FoundationValidator.ValidateSave(save, definitions, compatibility).ThrowIfInvalid(nameof(save));
            this.compatibility = compatibility;
            this.definitions = new DefinitionCatalogDto { DefinitionIds = new List<string>(definitions.DefinitionIds) };
            state = CopyState(save.GameState);
            actors = CopyActors(actorCivilizationIds, nameof(actorCivilizationIds));
            controlledActors = CopyActors(controlledActorCivilizationIds, nameof(controlledActorCivilizationIds));
            var entities = new HashSet<EntityId>(state.EntityIdentities.Select(e => new EntityId(e.EntityId)));
            if (!actors.IsSubsetOf(entities) || !controlledActors.IsSubsetOf(actors))
                throw new ArgumentException("Actor registry must reference existing identities; control must be a subset.");
            if (commandHandlers == null) throw new ArgumentNullException(nameof(commandHandlers));
            handlers = new Dictionary<CommandKind, ICommandHandler>();
            foreach (var pair in commandHandlers)
            {
                if (!IsKnownKind(pair.Key) || pair.Value == null) throw new ArgumentException("Invalid internal handler registration.");
                handlers.Add(pair.Key, pair.Value);
            }
        }

        public CommandResult SubmitCommand(CommandEnvelope? envelope)
        {
            lock (gate)
            {
                if (executingCommand) throw new InvalidOperationException("Nested command execution is not supported.");
                var command = CopyCommand(envelope);
                var commandId = command?.CommandId;
                CommandResult Reject(CommandErrorCode code) => new CommandResult(commandId, CommandStatus.Rejected,
                    code, state.TurnNumber, state.StateRevision);
                if (command == null || !command.ExpectedTurn.HasValue || !command.ExpectedRevision.HasValue)
                    return Reject(CommandErrorCode.InvalidEnvelope);
                if (!EntityId.TryParse(command.CommandId, out var parsedCommandId)) return Reject(CommandErrorCode.InvalidCommandId);
                if (!EntityId.TryParse(command.ActorCivilizationId, out var actor)) return Reject(CommandErrorCode.InvalidActorId);
                if (!actors.Contains(actor)) return Reject(CommandErrorCode.UnknownActor);
                if (!controlledActors.Contains(actor)) return Reject(CommandErrorCode.ActorNotControlled);
                if (command.ExpectedTurn.Value != state.TurnNumber) return Reject(CommandErrorCode.StaleTurn);
                // A replay of the original envelope reports DuplicateCommand even
                // though its original revision is now stale. Rejections reserve no IDs.
                if (state.AcceptedCommandIds!.Contains(parsedCommandId.Value)) return Reject(CommandErrorCode.DuplicateCommand);
                if (command.ExpectedRevision.Value != state.StateRevision) return Reject(CommandErrorCode.StaleRevision);
                if (!IsKnownKind(command.Kind)) return Reject(CommandErrorCode.UnknownCommandKind);
                if (!HasValidPayload(command)) return Reject(CommandErrorCode.InvalidPayload);
                if (!handlers.TryGetValue(command.Kind, out var handler)) return Reject(CommandErrorCode.UnsupportedCommand);
                if (state.StateRevision == long.MaxValue) return Reject(CommandErrorCode.RevisionExhausted);

                executingCommand = true;
                try
                {
                    var staging = CopyState(state);
                    var error = handler.Execute(staging, command);
                    if (!Enum.IsDefined(typeof(CommandErrorCode), error)) throw new InvalidOperationException("Handler returned an unknown error code.");
                    if (error != CommandErrorCode.None) return Reject(error);
                    if (staging.TurnNumber != state.TurnNumber || staging.StateRevision != state.StateRevision
                        || staging.AcceptedCommandIds == null || !staging.AcceptedCommandIds.SequenceEqual(state.AcceptedCommandIds!))
                        throw new InvalidOperationException("Handlers cannot change turn, revision or command deduplication bookkeeping.");
                    var validation = FoundationValidator.ValidateGameState(staging, definitions);
                    if (!validation.IsValid) throw new InvalidOperationException("Handler violated state invariants: " + validation.Issues[0].Code);
                    var entityIds = new HashSet<EntityId>(staging.EntityIdentities.Select(e => new EntityId(e.EntityId)));
                    if (!actors.IsSubsetOf(entityIds)) throw new InvalidOperationException("Handler removed a registered actor identity.");
                    staging.StateRevision = state.StateRevision + 1;
                    staging.AcceptedCommandIds.Add(parsedCommandId.Value);
                    // Clone again at commit: a retained staging reference cannot mutate
                    // authoritative state after the handler returns.
                    state = CopyState(staging);
                    return new CommandResult(parsedCommandId.Value, CommandStatus.Accepted, CommandErrorCode.None,
                        state.TurnNumber, state.StateRevision);
                }
                finally { executingCommand = false; }
            }
        }

        public SessionSnapshot CaptureSnapshot()
        {
            lock (gate)
            {
                return new SessionSnapshot(compatibility.ScenarioId, compatibility.RulesVersion, compatibility.ContentHash,
                    state.TurnNumber, state.StateRevision, state.NextEntitySequence,
                    new RngSnapshot(state.RngState.AlgorithmId, state.RngState.AlgorithmVersion, state.RngState.State!.Value),
                    state.EntityIdentities.Select(e => new EntityIdentitySnapshot(new EntityId(e.EntityId),
                        e.DefinitionId == null ? (DefinitionId?)null : new DefinitionId(e.DefinitionId))),
                    definitions.DefinitionIds.Select(d => new DefinitionId(d)).OrderBy(d => d),
                    actors.OrderBy(a => a).Select(a => new ActorSnapshot(a, controlledActors.Contains(a))),
                    state.AcceptedCommandIds!.Select(c => new EntityId(c)));
            }
        }

        // The host serializes this detached DTO. JSON/files never enter simulation.
        public SaveEnvelopeDto ExportSave()
        {
            lock (gate)
            {
                return new SaveEnvelopeDto { SaveFormatVersion = FoundationVersions.SaveFormatVersion,
                    ScenarioId = compatibility.ScenarioId, RulesVersion = compatibility.RulesVersion,
                    ContentHash = compatibility.ContentHash, GameState = CopyState(state) };
            }
        }

        private static HashSet<EntityId> CopyActors(IEnumerable<EntityId> source, string parameter)
        {
            if (source == null) throw new ArgumentNullException(parameter);
            var result = new HashSet<EntityId>();
            foreach (var id in source)
                if (!id.IsValid || !result.Add(id)) throw new ArgumentException("Invalid or duplicate actor ID.", parameter);
            return result;
        }

        private static bool IsKnownKind(CommandKind kind) => kind == CommandKind.EnqueueProduction
            || kind == CommandKind.CancelProduction || kind == CommandKind.SetResearchTarget || kind == CommandKind.SetFleetRoute;

        private static bool HasValidPayload(CommandEnvelope command)
        {
            var count = (command.EnqueueProduction == null ? 0 : 1) + (command.CancelProduction == null ? 0 : 1)
                + (command.SetResearchTarget == null ? 0 : 1) + (command.SetFleetRoute == null ? 0 : 1);
            if (count != 1) return false;
            switch (command.Kind)
            {
                case CommandKind.EnqueueProduction:
                    return command.EnqueueProduction != null && EntityId.TryParse(command.EnqueueProduction.ColonyId, out _)
                        && DefinitionId.TryParse(command.EnqueueProduction.DefinitionId, out _);
                case CommandKind.CancelProduction:
                    return command.CancelProduction != null && EntityId.TryParse(command.CancelProduction.ColonyId, out _)
                        && EntityId.TryParse(command.CancelProduction.QueueItemId, out _);
                case CommandKind.SetResearchTarget:
                    return command.SetResearchTarget != null && DefinitionId.TryParse(command.SetResearchTarget.TargetDefinitionId, out _);
                case CommandKind.SetFleetRoute:
                    return command.SetFleetRoute != null && EntityId.TryParse(command.SetFleetRoute.FleetId, out _)
                        && command.SetFleetRoute.RouteSystemIds != null && command.SetFleetRoute.RouteSystemIds.All(id => EntityId.TryParse(id, out _));
                default: return false;
            }
        }

        private static GameStateDto CopyState(GameStateDto source) => new GameStateDto
        {
            TurnNumber = source.TurnNumber, StateRevision = source.StateRevision, NextEntitySequence = source.NextEntitySequence,
            RngState = new RngStateDto { AlgorithmId = source.RngState.AlgorithmId,
                AlgorithmVersion = source.RngState.AlgorithmVersion, State = source.RngState.State },
            EntityIdentities = source.EntityIdentities.OrderBy(e => e.EntityId, StringComparer.Ordinal)
                .Select(e => new EntityIdentityDto { EntityId = e.EntityId, DefinitionId = e.DefinitionId }).ToList(),
            AcceptedCommandIds = source.AcceptedCommandIds!.OrderBy(c => c, StringComparer.Ordinal).ToList()
        };

        private static CommandEnvelope? CopyCommand(CommandEnvelope? source)
        {
            if (source == null) return null;
            return new CommandEnvelope
            {
                CommandId = source.CommandId, ActorCivilizationId = source.ActorCivilizationId,
                ExpectedTurn = source.ExpectedTurn, ExpectedRevision = source.ExpectedRevision, Kind = source.Kind,
                EnqueueProduction = source.EnqueueProduction == null ? null : new EnqueueProductionPayload
                    { ColonyId = source.EnqueueProduction.ColonyId, DefinitionId = source.EnqueueProduction.DefinitionId },
                CancelProduction = source.CancelProduction == null ? null : new CancelProductionPayload
                    { ColonyId = source.CancelProduction.ColonyId, QueueItemId = source.CancelProduction.QueueItemId },
                SetResearchTarget = source.SetResearchTarget == null ? null : new SetResearchTargetPayload
                    { TargetDefinitionId = source.SetResearchTarget.TargetDefinitionId },
                SetFleetRoute = source.SetFleetRoute == null ? null : new SetFleetRoutePayload
                    { FleetId = source.SetFleetRoute.FleetId, RouteSystemIds = source.SetFleetRoute.RouteSystemIds == null
                        ? null : new List<string>(source.SetFleetRoute.RouteSystemIds) }
            };
        }
    }
}
