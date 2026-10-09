using System;
using System.Collections.Generic;
using System.Linq;
using HobbyDS9.Contracts;

namespace HobbyDS9.Simulation
{
    public sealed class SimulationSession
    {
        private readonly object gate = new object();
        private bool executingMutation;
        private readonly Func<StrategicTurnPhase, GameStateDto, bool>? turnPhaseProbe;
        private GameStateDto state;
        private readonly DefinitionCatalogDto definitions;
        private readonly SaveCompatibility compatibility;
        public GalaxyGraph Galaxy { get; }
        private readonly Dictionary<CommandKind, ICommandHandler> handlers;

        public SimulationSession(SaveEnvelopeDto save, DefinitionCatalogDto definitions, SaveCompatibility compatibility)
            : this(save, definitions, compatibility,
                new Dictionary<CommandKind, ICommandHandler>()) { }

        internal SimulationSession(SaveEnvelopeDto save, DefinitionCatalogDto definitions, SaveCompatibility compatibility,
            IReadOnlyDictionary<CommandKind, ICommandHandler> commandHandlers,
            Func<StrategicTurnPhase, GameStateDto, bool>? turnPhaseProbe = null)
        {
            FoundationValidator.ValidateSave(save, definitions, compatibility).ThrowIfInvalid(nameof(save));
            this.compatibility = compatibility;
            this.turnPhaseProbe = turnPhaseProbe;
            this.definitions = CopyDefinitions(definitions);
            state = CopyState(save.GameState);
            Galaxy = new GalaxyGraph(state.StarSystems!.Select(n => new EntityId(n.SystemId)), this.definitions.GalaxyEdges!);
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
                if (executingMutation) throw new InvalidOperationException("Nested session mutation is not supported.");
                var command = CopyCommand(envelope);
                var commandId = command?.CommandId;
                CommandResult Reject(CommandErrorCode code) => new CommandResult(commandId, CommandStatus.Rejected,
                    code, state.TurnNumber, state.StateRevision);
                if (command == null || !command.ExpectedTurn.HasValue || !command.ExpectedRevision.HasValue)
                    return Reject(CommandErrorCode.InvalidEnvelope);
                if (!EntityId.TryParse(command.CommandId, out var parsedCommandId)) return Reject(CommandErrorCode.InvalidCommandId);
                if (!EntityId.TryParse(command.ActorCivilizationId, out var actor)) return Reject(CommandErrorCode.InvalidActorId);
                var civilization = state.Civilizations!.SingleOrDefault(c => c.CivilizationId == actor.Value);
                if (civilization == null) return Reject(CommandErrorCode.UnknownActor);
                if (civilization.ControllerKind != ControllerKind.Human) return Reject(CommandErrorCode.ActorNotControlled);
                if (state.Phase != SessionPhase.Planning) return Reject(CommandErrorCode.CombatPending);
                if (command.ExpectedTurn.Value != state.TurnNumber) return Reject(CommandErrorCode.StaleTurn);
                // A replay of the original envelope reports DuplicateCommand even
                // though its original revision is now stale. Rejections reserve no IDs.
                if (state.AcceptedCommandIds!.Contains(parsedCommandId.Value)) return Reject(CommandErrorCode.DuplicateCommand);
                if (command.ExpectedRevision.Value != state.StateRevision) return Reject(CommandErrorCode.StaleRevision);
                if (!IsKnownKind(command.Kind)) return Reject(CommandErrorCode.UnknownCommandKind);
                if (!HasValidPayload(command)) return Reject(CommandErrorCode.InvalidPayload);
                if (!handlers.TryGetValue(command.Kind, out var handler)) return Reject(CommandErrorCode.UnsupportedCommand);
                if (state.StateRevision == long.MaxValue) return Reject(CommandErrorCode.RevisionExhausted);

                executingMutation = true;
                try
                {
                    var staging = CopyState(state);
                    var error = handler.Execute(staging, command);
                    if (!Enum.IsDefined(typeof(CommandErrorCode), error)) throw new InvalidOperationException("Handler returned an unknown error code.");
                    if (error != CommandErrorCode.None) return Reject(error);
                    if (staging.Phase != state.Phase || staging.TurnNumber != state.TurnNumber || staging.StateRevision != state.StateRevision
                        || staging.AcceptedCommandIds == null || !staging.AcceptedCommandIds.SequenceEqual(state.AcceptedCommandIds!))
                        throw new InvalidOperationException("Handlers cannot change phase, turn, revision or command deduplication bookkeeping.");
                    var validation = FoundationValidator.ValidateGameState(staging, definitions);
                    if (!validation.IsValid) throw new InvalidOperationException("Handler violated state invariants: " + validation.Issues[0].Code);
                    if (!staging.Civilizations!.Select(c => c.CivilizationId).SequenceEqual(state.Civilizations!.Select(c => c.CivilizationId))
                        || !staging.StarSystems!.Select(c => c.SystemId).SequenceEqual(state.StarSystems!.Select(c => c.SystemId)))
                        throw new InvalidOperationException("Foundation handlers cannot change civilization or graph node membership.");
                    staging.StateRevision = state.StateRevision + 1;
                    staging.AcceptedCommandIds.Add(parsedCommandId.Value);
                    // Clone again at commit: a retained staging reference cannot mutate
                    // authoritative state after the handler returns.
                    state = CopyState(staging);
                    return new CommandResult(parsedCommandId.Value, CommandStatus.Accepted, CommandErrorCode.None,
                        state.TurnNumber, state.StateRevision);
                }
                finally { executingMutation = false; }
            }
        }

        public TurnResult AdvanceTurn(int expectedTurn, long expectedRevision)
        {
            lock (gate)
            {
                if (executingMutation) throw new InvalidOperationException("Nested session mutation is not supported.");
                var attemptedTurn = state.TurnNumber;
                TurnResult Reject(TurnErrorCode error) => new TurnResult(TurnStatus.Rejected, error, attemptedTurn,
                    state.TurnNumber, state.StateRevision, state.Phase!.Value,
                    Array.Empty<StrategicEvent>(), Array.Empty<CommandResult>());
                if (state.Phase != SessionPhase.Planning) return Reject(TurnErrorCode.CombatPending);
                if (expectedTurn != state.TurnNumber) return Reject(TurnErrorCode.StaleTurn);
                if (expectedRevision != state.StateRevision) return Reject(TurnErrorCode.StaleRevision);
                if (state.TurnNumber == int.MaxValue) return Reject(TurnErrorCode.TurnNumberExhausted);
                if (state.StateRevision == long.MaxValue) return Reject(TurnErrorCode.RevisionExhausted);

                executingMutation = true;
                try
                {
                    var staging = CopyState(state);
                    if (!StrategicTurnPhases.Execute(staging, definitions, turnPhaseProbe)) return Reject(TurnErrorCode.PhaseFailed);
                    var validation = FoundationValidator.ValidateGameState(staging, definitions);
                    if (!validation.IsValid) throw new InvalidOperationException("Turn phase violated state invariants: " + validation.Issues[0].Code);
                    // Copy again before committing: retained phase references are
                    // never authoritative. Empty phases must preserve all facts.
                    var candidate = CopyState(staging);
                    RequirePhaseChanges(state, candidate, allowEconomy: true);
                    candidate.TurnNumber = checked(attemptedTurn + 1);
                    candidate.StateRevision = checked(state.StateRevision + 1);
                    candidate.AcceptedCommandIds!.Clear();
                    candidate.Phase = SessionPhase.Planning;
                    FoundationValidator.ValidateGameState(candidate, definitions).ThrowIfInvalid(nameof(candidate));
                    var result = new TurnResult(TurnStatus.Completed, TurnErrorCode.None, attemptedTurn,
                        candidate.TurnNumber, candidate.StateRevision, candidate.Phase.Value,
                        Array.Empty<StrategicEvent>(), Array.Empty<CommandResult>());
                    state = candidate;
                    return result;
                }
                finally { executingMutation = false; }
            }
        }

        // Only Economy may change credits and colony population/stocks.
        internal static void RequirePhaseChanges(GameStateDto state, GameStateDto candidate, bool allowEconomy)
        {
            if (candidate.Phase != state.Phase || candidate.TurnNumber != state.TurnNumber
                || candidate.StateRevision != state.StateRevision || candidate.NextEntitySequence != state.NextEntitySequence
                || candidate.RngState.AlgorithmId != state.RngState.AlgorithmId
                || candidate.RngState.AlgorithmVersion != state.RngState.AlgorithmVersion
                || candidate.RngState.State != state.RngState.State
                || !candidate.AcceptedCommandIds!.SequenceEqual(state.AcceptedCommandIds!)
                || !candidate.EntityIdentities.Select(e => (e.EntityId, e.DefinitionId))
                    .SequenceEqual(state.EntityIdentities.Select(e => (e.EntityId, e.DefinitionId)))
                || !candidate.Civilizations!.Select(c => (c.CivilizationId, c.DefinitionId, c.ControllerKind, allowEconomy ? 0L : c.Credits))
                    .SequenceEqual(state.Civilizations!.Select(c => (c.CivilizationId, c.DefinitionId, c.ControllerKind, allowEconomy ? 0L : c.Credits)))
                || !candidate.StarSystems!.Select(n => (n.SystemId, n.DefinitionId, n.OwnerCivilizationId))
                    .SequenceEqual(state.StarSystems!.Select(n => (n.SystemId, n.DefinitionId, n.OwnerCivilizationId)))
                || !candidate.Colonies!.Select(c => (c.ColonyId, c.SystemId, c.OwnerCivilizationId, c.EconomicProfileDefinitionId,
                    c.PopulationCapacity, allowEconomy ? 0L : c.Population, allowEconomy ? 0L : c.Food, allowEconomy ? 0L : c.Materials))
                    .SequenceEqual(state.Colonies!.Select(c => (c.ColonyId, c.SystemId, c.OwnerCivilizationId, c.EconomicProfileDefinitionId,
                    c.PopulationCapacity, allowEconomy ? 0L : c.Population, allowEconomy ? 0L : c.Food, allowEconomy ? 0L : c.Materials))))
                throw new InvalidOperationException("Strategic phase changed protected campaign facts or bookkeeping.");
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
                    state.Civilizations!.Select(c => new ActorSnapshot(new EntityId(c.CivilizationId), c.ControllerKind!.Value)),
                    state.AcceptedCommandIds!.Select(c => new EntityId(c)),
                    state.Civilizations!.Select(c => new CivilizationSnapshot(new EntityId(c.CivilizationId), new DefinitionId(c.DefinitionId), c.ControllerKind!.Value, c.Credits)),
                    state.StarSystems!.Select(n => new StarSystemSnapshot(new EntityId(n.SystemId), new DefinitionId(n.DefinitionId),
                        n.OwnerCivilizationId == null ? (EntityId?)null : new EntityId(n.OwnerCivilizationId))), Galaxy.Edges, state.Phase!.Value, state.Colonies!.Select(c => new ColonySnapshot(new EntityId(c.ColonyId),
                        new EntityId(c.SystemId), c.OwnerCivilizationId == null ? (EntityId?)null : new EntityId(c.OwnerCivilizationId),
                        new DefinitionId(c.EconomicProfileDefinitionId), c.Population, c.PopulationCapacity, c.Food, c.Materials)));
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

        private static DefinitionCatalogDto CopyDefinitions(DefinitionCatalogDto source) => new DefinitionCatalogDto
        {
            ContentFormatVersion = source.ContentFormatVersion,
            DefinitionIds = source.DefinitionIds.OrderBy(d => d, StringComparer.Ordinal).ToList(),
            Civilizations = source.Civilizations!.OrderBy(c => c.DefinitionId, StringComparer.Ordinal)
                .Select(c => new CivilizationDefinitionDto { DefinitionId = c.DefinitionId, DisplayName = c.DisplayName }).ToList(),
            StarSystems = source.StarSystems!.OrderBy(n => n.DefinitionId, StringComparer.Ordinal)
                .Select(n => new StarSystemDefinitionDto { DefinitionId = n.DefinitionId, DisplayName = n.DisplayName }).ToList(),
            EconomicProfiles = source.EconomicProfiles!.OrderBy(p => p.DefinitionId, StringComparer.Ordinal).Select(p => new EconomicProfileDefinitionDto
            {
                DefinitionId = p.DefinitionId, DisplayName = p.DisplayName, FoodOutput = p.FoodOutput,
                PopulationPerFood = p.PopulationPerFood, MaterialOutput = p.MaterialOutput, PopulationPerCredit = p.PopulationPerCredit,
                BaseIndustry = p.BaseIndustry, PopulationPerIndustry = p.PopulationPerIndustry, GrowthPerTurn = p.GrowthPerTurn,
                StarvationLossPerTurn = p.StarvationLossPerTurn, ResearchOutput = p.ResearchOutput
            }).ToList(),
            GalaxyEdges = source.GalaxyEdges!.Select(GalaxyGraph.CanonicalEdge).OrderBy(e => e.EndpointA).ThenBy(e => e.EndpointB)
                .Select(e => new GalaxyEdgeDto { EndpointA = e.EndpointA.Value, EndpointB = e.EndpointB.Value, Kind = e.Kind }).ToList()
        };

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

        internal static GameStateDto CopyState(GameStateDto source) => new GameStateDto
        {
            Phase = source.Phase, TurnNumber = source.TurnNumber, StateRevision = source.StateRevision, NextEntitySequence = source.NextEntitySequence,
            RngState = new RngStateDto { AlgorithmId = source.RngState.AlgorithmId,
                AlgorithmVersion = source.RngState.AlgorithmVersion, State = source.RngState.State },
            EntityIdentities = source.EntityIdentities.OrderBy(e => e.EntityId, StringComparer.Ordinal)
                .Select(e => new EntityIdentityDto { EntityId = e.EntityId, DefinitionId = e.DefinitionId }).ToList(),
            AcceptedCommandIds = source.AcceptedCommandIds!.OrderBy(c => c, StringComparer.Ordinal).ToList(),
            Civilizations = source.Civilizations!.OrderBy(c => c.CivilizationId, StringComparer.Ordinal)
                .Select(c => new CivilizationStateDto { CivilizationId = c.CivilizationId, DefinitionId = c.DefinitionId, ControllerKind = c.ControllerKind, Credits = c.Credits }).ToList(),
            StarSystems = source.StarSystems!.OrderBy(n => n.SystemId, StringComparer.Ordinal)
                .Select(n => new StarSystemStateDto { SystemId = n.SystemId, DefinitionId = n.DefinitionId, OwnerCivilizationId = n.OwnerCivilizationId }).ToList(),
            Colonies = source.Colonies!.OrderBy(c => c.ColonyId, StringComparer.Ordinal).Select(c => new ColonyStateDto
            {
                ColonyId = c.ColonyId, SystemId = c.SystemId, OwnerCivilizationId = c.OwnerCivilizationId,
                EconomicProfileDefinitionId = c.EconomicProfileDefinitionId, Population = c.Population,
                PopulationCapacity = c.PopulationCapacity, Food = c.Food, Materials = c.Materials
            }).ToList()
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
