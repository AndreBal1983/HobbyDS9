using System;
using System.Collections.Generic;
using System.Linq;
using HobbyDS9.Contracts;

namespace HobbyDS9.Simulation
{
    public sealed partial class SimulationSession
    {
        private readonly object gate = new object();
        private bool executingMutation;
        private readonly Func<StrategicTurnPhase, GameStateDto, bool>? turnPhaseProbe;
        private readonly Func<GameStateDto, bool>? combatCheckpointProbe;
        private readonly Action<int, GameStateDto>? combatApplicationProbe;
        private readonly Action<GameStateDto>? combatFinalizationProbe;
        private GameStateDto state;
        private readonly DefinitionCatalogDto definitions;
        private readonly SaveCompatibility compatibility;
        public WeightedNavigationGraph Navigation { get; }
        public GalaxyGraph Galaxy { get; }
        private readonly Dictionary<CommandKind, ICommandHandler> handlers;

        public SimulationSession(SaveEnvelopeDto save, DefinitionCatalogDto definitions, SaveCompatibility compatibility)
            : this(save, definitions, compatibility,
                new Dictionary<CommandKind, ICommandHandler>())
        { }

        internal SimulationSession(SaveEnvelopeDto save, DefinitionCatalogDto definitions, SaveCompatibility compatibility,
            IReadOnlyDictionary<CommandKind, ICommandHandler> commandHandlers,
            Func<StrategicTurnPhase, GameStateDto, bool>? turnPhaseProbe = null, Func<GameStateDto, bool>? combatCheckpointProbe = null, Action<int, GameStateDto>? combatApplicationProbe = null, Action<GameStateDto>? combatFinalizationProbe = null)
        {
            FoundationValidator.ValidateSave(save, definitions, compatibility).ThrowIfInvalid(nameof(save));
            this.compatibility = compatibility;
            this.turnPhaseProbe = turnPhaseProbe;
            this.combatCheckpointProbe = combatCheckpointProbe;
            this.combatApplicationProbe = combatApplicationProbe;
            this.combatFinalizationProbe = combatFinalizationProbe;
            this.definitions = CopyDefinitions(definitions);
            state = CopyState(save.GameState);
            Galaxy = new GalaxyGraph(state.StarSystems!.Select(n => new EntityId(n.SystemId)), this.definitions.GalaxyEdges!);
            Navigation = new WeightedNavigationGraph(this.definitions);
            if (commandHandlers == null) throw new ArgumentNullException(nameof(commandHandlers));
            handlers = new Dictionary<CommandKind, ICommandHandler>
            {
                [CommandKind.SetResearchAllocation] = new ScienceCommandHandler(),
                [CommandKind.SetScienceAllocation] = new ScienceCommandHandler(),
                [CommandKind.SetEngineeringTarget] = new EngineeringTargetHandler(this.definitions),
                [CommandKind.EnqueueProduction] = new EnqueueProductionHandler(this.definitions),
                [CommandKind.PublishShipBlueprint] = new PublishShipBlueprintHandler(this.definitions, compatibility),
                [CommandKind.EnqueueBlueprintConstruction] = new EnqueueBlueprintConstructionHandler(this.definitions),
                [CommandKind.CancelProduction] = new CancelProductionHandler(),
                [CommandKind.SetFleetRoute] = new SetFleetRouteHandler(Navigation),
                [CommandKind.SetWorldGovernancePolicy] = new GovernancePolicyHandler(this.definitions)
            };
            foreach (var pair in commandHandlers)
            {
                if (!IsKnownKind(pair.Key) || pair.Value == null) throw new ArgumentException("Invalid internal handler registration.");
                handlers[pair.Key] = pair.Value;
            }
        }

        public CommandResult SubmitCommand(CommandEnvelope? envelope)
        {
            lock (gate)
            {
                if (executingMutation) throw new InvalidOperationException("Nested session mutation is not supported.");
                executingMutation = true;
                try { return ExecuteCommand(ref state, envelope, trustedAi: false); }
                finally { executingMutation = false; }
            }
        }

        // The same staging/validation/payment/allocation path serves humans and
        // trusted AI. Only the required persisted controller differs.
        private CommandResult ExecuteCommand(ref GameStateDto target, CommandEnvelope? envelope, bool trustedAi)
        {
            var current = target;
            var command = CopyCommand(envelope);
            var commandId = command?.CommandId;
            CommandResult Reject(CommandErrorCode code) => new CommandResult(commandId, CommandStatus.Rejected,
                code, current.TurnNumber, current.StateRevision);
            if (command == null || !command.ExpectedTurn.HasValue || !command.ExpectedRevision.HasValue)
                return Reject(CommandErrorCode.InvalidEnvelope);
            if (!EntityId.TryParse(command.CommandId, out var parsedCommandId)) return Reject(CommandErrorCode.InvalidCommandId);
            if (!trustedAi && command.CommandId.StartsWith("ai-command.", StringComparison.Ordinal)) return Reject(CommandErrorCode.ReservedCommandId);
            if (!EntityId.TryParse(command.ActorCivilizationId, out var actor)) return Reject(CommandErrorCode.InvalidActorId);
            var civilization = current.Civilizations!.SingleOrDefault(c => c.CivilizationId == actor.Value);
            if (civilization == null) return Reject(CommandErrorCode.UnknownActor);
            if (civilization.ControllerKind != (trustedAi ? ControllerKind.AI : ControllerKind.Human)) return Reject(CommandErrorCode.ActorNotControlled);
            if (current.Phase != SessionPhase.Planning) return Reject(CommandErrorCode.CombatPending);
            if (command.ExpectedTurn.Value != current.TurnNumber) return Reject(CommandErrorCode.StaleTurn);
            // A replay of the original envelope reports DuplicateCommand even
            // though its original revision is now stale. Rejections reserve no IDs.
            if (current.AcceptedCommandIds!.Contains(parsedCommandId.Value)) return Reject(CommandErrorCode.DuplicateCommand);
            if (command.ExpectedRevision.Value != current.StateRevision) return Reject(CommandErrorCode.StaleRevision);
            if (!IsKnownKind(command.Kind)) return Reject(CommandErrorCode.UnknownCommandKind);
            if (!HasValidPayload(command)) return Reject(CommandErrorCode.InvalidPayload);
            if (!handlers.TryGetValue(command.Kind, out var handler)) return Reject(CommandErrorCode.UnsupportedCommand);
            if (current.StateRevision == long.MaxValue) return Reject(CommandErrorCode.RevisionExhausted);

            var staging = CopyState(current);
            var error = handler.Execute(staging, command);
            if (!Enum.IsDefined(typeof(CommandErrorCode), error)) throw new InvalidOperationException("Handler returned an unknown error code.");
            if (error != CommandErrorCode.None) return Reject(error);
            if (current.Blueprints!.Any(old => !staging.Blueprints!.Any(next => BlueprintCopies.Same(old, next)))
                || (command.Kind != CommandKind.PublishShipBlueprint && current.Blueprints!.Count != staging.Blueprints!.Count))
                throw new InvalidOperationException("Commands cannot rewrite immutable blueprint history.");
            if (staging.Phase != current.Phase || staging.TurnNumber != current.TurnNumber || staging.StateRevision != current.StateRevision
                || staging.AcceptedCommandIds == null || !staging.AcceptedCommandIds.SequenceEqual(current.AcceptedCommandIds!))
                throw new InvalidOperationException("Handlers cannot change phase, turn, revision or command deduplication bookkeeping.");
            var validation = FoundationValidator.ValidateGameState(staging, definitions);
            if (!validation.IsValid) throw new InvalidOperationException("Handler violated state invariants: " + validation.Issues[0].Code);
            if (!staging.Civilizations!.Select(c => c.CivilizationId).SequenceEqual(current.Civilizations!.Select(c => c.CivilizationId))
                || !staging.StarSystems!.Select(c => c.SystemId).SequenceEqual(current.StarSystems!.Select(c => c.SystemId)))
                throw new InvalidOperationException("Foundation handlers cannot change civilization or graph node membership.");
            staging.StateRevision = current.StateRevision + 1;
            staging.AcceptedCommandIds.Add(parsedCommandId.Value);
            // Clone again at commit: a retained staging reference cannot mutate
            // authoritative state after the handler returns.
            target = CopyState(staging);
            return new CommandResult(parsedCommandId.Value, CommandStatus.Accepted, CommandErrorCode.None,
                target.TurnNumber, target.StateRevision);
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
                    var commandResults = new List<CommandResult>();
                    GameStateDto? afterAi = null;
                    Action aiPlanning = () =>
                    {
                        AiPlanningPhase.Execute(() => staging, definitions, Navigation,
                            command => ExecuteCommand(ref staging, command, trustedAi: true), commandResults);
                        afterAi = CopyState(staging);
                    };
                    Action encounterCreation = () => CampaignCombatState.Detect(staging, definitions, compatibility);
                    if (!StrategicTurnPhases.Execute(ref staging, definitions, turnPhaseProbe, aiPlanning, encounterCreation)) return Reject(TurnErrorCode.PhaseFailed);
                    if (combatCheckpointProbe != null)
                    {
                        var beforeProbe = CopyState(staging);
                        if (!combatCheckpointProbe(staging)) return Reject(TurnErrorCode.PhaseFailed);
                        RequirePhaseChanges(beforeProbe, CopyState(staging), allowEconomy: false);
                    }
                    var validation = FoundationValidator.ValidateGameState(staging, definitions);
                    if (!validation.IsValid) throw new InvalidOperationException("Turn phase violated state invariants: " + validation.Issues[0].Code);
                    // Copy again before committing: retained phase references are
                    // never authoritative. Empty phases must preserve all facts.
                    var candidate = CopyState(staging);
                    var protectedCandidate = CopyState(candidate);
                    protectedCandidate.Phase = afterAi!.Phase; protectedCandidate.RngState = afterAi.RngState;
                    protectedCandidate.PendingCombats = afterAi.PendingCombats; protectedCandidate.ResolvedCombats = afterAi.ResolvedCombats;
                    RequirePhaseChanges(afterAi, protectedCandidate, allowEconomy: true, allowResearch: true, allowProduction: true, allowMovement: true);
                    var awaiting = candidate.Phase == SessionPhase.AwaitingCombat;
                    candidate.TurnNumber = awaiting ? attemptedTurn : checked(attemptedTurn + 1);
                    candidate.StateRevision = checked(staging.StateRevision + 1);
                    if (!awaiting) candidate.AcceptedCommandIds!.Clear();
                    FoundationValidator.ValidateGameState(candidate, definitions).ThrowIfInvalid(nameof(candidate));
                    var result = new TurnResult(awaiting ? TurnStatus.AwaitingCombat : TurnStatus.Completed, TurnErrorCode.None, attemptedTurn,
                        candidate.TurnNumber, candidate.StateRevision, candidate.Phase!.Value,
                        Array.Empty<StrategicEvent>(), commandResults);
                    state = candidate;
                    return result;
                }
                finally { executingMutation = false; }
            }
        }

        // Economy and Research each have a distinct allowed mutation surface.
        internal static void RequirePhaseChanges(GameStateDto state, GameStateDto candidate, bool allowEconomy, bool allowResearch = false, bool allowProduction = false, bool allowMovement = false)
        {
            if (!state.Blueprints!.Zip(candidate.Blueprints!, (a,b) => BlueprintCopies.Same(a,b)).All(v => v) || state.Blueprints!.Count != candidate.Blueprints!.Count || !ScienceData.Same(state,candidate,allowResearch) || candidate.Phase != state.Phase || candidate.TurnNumber != state.TurnNumber
                || candidate.StateRevision != state.StateRevision || (!allowProduction && !allowEconomy && candidate.NextEntitySequence != state.NextEntitySequence)
                || candidate.RngState.AlgorithmId != state.RngState.AlgorithmId
                || candidate.RngState.AlgorithmVersion != state.RngState.AlgorithmVersion
                || candidate.RngState.State != state.RngState.State
                || !candidate.AcceptedCommandIds!.SequenceEqual(state.AcceptedCommandIds!)
                || (!allowProduction && !allowEconomy && !candidate.EntityIdentities.Select(e => (e.EntityId, e.DefinitionId))
                    .SequenceEqual(state.EntityIdentities.Select(e => (e.EntityId, e.DefinitionId))))
                || !candidate.Civilizations!.Select(c => (c.CivilizationId, c.DefinitionId, c.ControllerKind, allowEconomy ? 0L : c.Credits,
                        allowResearch ? 0L : c.EngineeringReserve, allowResearch ? null : c.EngineeringTargetDefinitionId))
                    .SequenceEqual(state.Civilizations!.Select(c => (c.CivilizationId, c.DefinitionId, c.ControllerKind, allowEconomy ? 0L : c.Credits,
                        allowResearch ? 0L : c.EngineeringReserve, allowResearch ? null : c.EngineeringTargetDefinitionId)))
                || (!allowResearch && candidate.Civilizations!.Zip(state.Civilizations!, (a, b) =>
                    a.MatureEngineeringApplicationDefinitionIds!.SequenceEqual(b.MatureEngineeringApplicationDefinitionIds!)).Any(equal => !equal))
                || !candidate.StarSystems!.Select(n => (n.SystemId, n.DefinitionId, n.OwnerCivilizationId))
                    .SequenceEqual(state.StarSystems!.Select(n => (n.SystemId, n.DefinitionId, n.OwnerCivilizationId)))
                || !PopulationGuards.Same(state, candidate, allowEconomy, allowProduction)
                || !NavigationStateGuards.Same(state,candidate,allowMovement))
                throw new InvalidOperationException("Strategic phase changed protected campaign facts or bookkeeping.");
            ProductionFleetGuards.Require(state, candidate, allowProduction, allowMovement, allowEconomy);
            if (!CampaignCombatState.SameBatch(state, candidate)) throw new InvalidOperationException("Phase changed protected combat state.");
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
                    state.Civilizations!.Select(c => new CivilizationSnapshot(new EntityId(c.CivilizationId), new DefinitionId(c.DefinitionId), c.ControllerKind!.Value, c.Credits, c.EngineeringReserve,
                        c.EngineeringTargetDefinitionId == null ? (DefinitionId?)null : new DefinitionId(c.EngineeringTargetDefinitionId),
                        c.MatureEngineeringApplicationDefinitionIds!.Select(id => new DefinitionId(id)), new ScienceEngineeringSnapshot(c))),
                    state.StarSystems!.Select(n => new StarSystemSnapshot(new EntityId(n.SystemId), new DefinitionId(n.DefinitionId),
                        n.OwnerCivilizationId == null ? (EntityId?)null : new EntityId(n.OwnerCivilizationId))), Galaxy.Edges, state.Phase!.Value, state.Colonies!.Select(c => new ColonySnapshot(c, WorldEconomy.Planet(c, state, definitions), WorldEconomy.Project(c, state, definitions))),
                    state.Ships!.Select(s => new ShipSnapshot(s, CampaignShipDesignResolver.Ship(state, definitions, s))), state.Fleets!.Select(f => new FleetSnapshot(f)), definitions.ShipDesigns!.Select(d => new ShipDesignSnapshot(d)),
                    CampaignCombatState.AllRequests(state).Select(r => new CombatEncounterSnapshot(r, state.ResolvedCombats!.Any(c => c.Request.EncounterId == r.EncounterId))),
                    state.Planets!.Select(p => new PlanetStateSnapshot(p)), state.Displacements!.Select(r => new DisplacementSnapshot(r)),
                    state.LastDeliveries!.Select(r => new LogisticsDeliverySnapshot(r)), new PopulationStatisticsSnapshot(state.PopulationStatistics!), new NavigationStatisticsSnapshot(state.NavigationStatistics!), state.ResearchPaceFactor!.Value, state.LastResearchReceipts!.Select(r=>new ResearchReceiptSnapshot(r)));
            }
        }

        // The host serializes this detached DTO. JSON/files never enter simulation.
        public SaveEnvelopeDto ExportSave()
        {
            lock (gate)
            {
                return new SaveEnvelopeDto
                {
                    SaveFormatVersion = FoundationVersions.SaveFormatVersion,
                    ScenarioId = compatibility.ScenarioId,
                    RulesVersion = compatibility.RulesVersion,
                    ContentHash = compatibility.ContentHash,
                    GameState = CopyState(state)
                };
            }
        }

        private static DefinitionCatalogDto CopyDefinitions(DefinitionCatalogDto source) => new DefinitionCatalogDto
        {
            ContentFormatVersion = source.ContentFormatVersion,
            ShipDesignCatalogue = source.ShipDesignCatalogue == null ? null : BlueprintCopies.Copy(source.ShipDesignCatalogue),
            ShipDesignVersionAccess = source.ShipDesignVersionAccess!.OrderBy(g => g.VersionId, StringComparer.Ordinal).Select(BlueprintCopies.Copy).ToList(),
            DefinitionIds = source.DefinitionIds.OrderBy(d => d, StringComparer.Ordinal).ToList(),
            Civilizations = source.Civilizations!.OrderBy(c => c.DefinitionId, StringComparer.Ordinal)
                .Select(c => new CivilizationDefinitionDto { DefinitionId = c.DefinitionId, DisplayName = c.DisplayName }).ToList(),
            StarSystems = source.StarSystems!.OrderBy(n => n.DefinitionId, StringComparer.Ordinal)
                .Select(n => new StarSystemDefinitionDto { DefinitionId = n.DefinitionId, DisplayName = n.DisplayName }).ToList(),
            ShipDesigns = source.ShipDesigns!.OrderBy(d => d.DefinitionId, StringComparer.Ordinal).Select(d => new ShipDesignDefinitionDto
            {
                DefinitionId = d.DefinitionId,
                DisplayName = d.DisplayName,
                MovementBudget=d.MovementBudget,
                IndustryCost = d.IndustryCost,
                ResourceCosts = d.ResourceCosts!.OrderBy(r => r.ResourceDefinitionId, StringComparer.Ordinal).Select(PopulationData.Copy).ToList(),
                CreditCost = d.CreditCost,
                MaxHull = d.MaxHull,
                CombatProfile = CombatProfiles.Copy(d.CombatProfile!),
                RequiredEngineeringApplicationDefinitionIds = d.RequiredEngineeringApplicationDefinitionIds!.OrderBy(id => id, StringComparer.Ordinal).ToList()
            }).ToList(),
            EngineeringApplications = source.EngineeringApplications!.OrderBy(n => n.DefinitionId, StringComparer.Ordinal).Select(n => new EngineeringApplicationDefinitionDto
            {
                DefinitionId = n.DefinitionId,
                DisplayName = n.DisplayName,
                BaseEngineeringCost = n.BaseEngineeringCost, Kind=n.Kind, EffectSummary=n.EffectSummary, ParameterChange=n.ParameterChange==null?null:new EngineeringParameterChangeDto{ParameterDefinitionId=n.ParameterChange.ParameterDefinitionId,Value=n.ParameterChange.Value},
                ScienceRequirements = n.ScienceRequirements!.OrderBy(q => q.Discipline).ThenBy(q => q.IntervalStartLevel).ThenBy(q => q.RequiredBreakthroughOrdinal).Select(q => new EngineeringScienceRequirementDto { Discipline=q.Discipline, IntervalStartLevel=q.IntervalStartLevel, RequiredBreakthroughOrdinal=q.RequiredBreakthroughOrdinal }).ToList(),
                PrerequisiteDefinitionIds = n.PrerequisiteDefinitionIds!.OrderBy(id => id, StringComparer.Ordinal).ToList()
            }).ToList(),
            GovernancePolicies = source.GovernancePolicies!.OrderBy(p=>p.DefinitionId,StringComparer.Ordinal).Select(PopulationData.Copy).ToList(),
            PoliticalMemoryEffects = source.PoliticalMemoryEffects!.OrderBy(p=>p.DefinitionId,StringComparer.Ordinal).Select(PopulationData.Copy).ToList(),
            NavigationRegions=source.NavigationRegions!.OrderBy(n=>n.NodeId,StringComparer.Ordinal).Select(PopulationData.Copy).ToList(),
            NavigationConnections=source.NavigationConnections!.OrderBy(e=>e.ConnectionId,StringComparer.Ordinal).Select(PopulationData.Copy).ToList(),
            Phenomena=source.Phenomena!.OrderBy(p=>p.DefinitionId,StringComparer.Ordinal).Select(PopulationData.Copy).ToList(),
            Species = source.Species!.OrderBy(d => d.DefinitionId, StringComparer.Ordinal).Select(PopulationData.Copy).ToList(),
            Planets = source.Planets!.OrderBy(d => d.DefinitionId, StringComparer.Ordinal).Select(PopulationData.Copy).ToList(),
            Resources = source.Resources!.OrderBy(d => d.DefinitionId, StringComparer.Ordinal).Select(PopulationData.Copy).ToList(),
            Infrastructure = source.Infrastructure!.OrderBy(d => d.DefinitionId, StringComparer.Ordinal).Select(PopulationData.Copy).ToList(),
            GalaxyEdges = source.GalaxyEdges!.Select(GalaxyGraph.CanonicalEdge).OrderBy(e => e.EndpointA).ThenBy(e => e.EndpointB)
                .Select(e => new GalaxyEdgeDto { EndpointA = e.EndpointA.Value, EndpointB = e.EndpointB.Value, Kind = e.Kind }).ToList()
        };

        private static bool IsKnownKind(CommandKind kind) => kind == CommandKind.PublishShipBlueprint || kind == CommandKind.EnqueueBlueprintConstruction || kind == CommandKind.EnqueueProduction
            || kind == CommandKind.CancelProduction || kind == CommandKind.SetEngineeringTarget || kind == CommandKind.SetFleetRoute || kind == CommandKind.SetWorldGovernancePolicy || kind == CommandKind.SetResearchAllocation || kind == CommandKind.SetScienceAllocation;

        private static bool HasValidPayload(CommandEnvelope command)
        {
            var count = (command.PublishShipBlueprint == null ? 0 : 1) + (command.EnqueueBlueprintConstruction == null ? 0 : 1) + (command.SetResearchAllocation == null ? 0 : 1) + (command.SetScienceAllocation == null ? 0 : 1) + (command.EnqueueProduction == null ? 0 : 1) + (command.CancelProduction == null ? 0 : 1)
                + (command.SetEngineeringTarget == null ? 0 : 1) + (command.SetFleetRoute == null ? 0 : 1) + (command.SetWorldGovernancePolicy == null ? 0 : 1);
            if (count != 1) return false;
            switch (command.Kind)
            {
                case CommandKind.PublishShipBlueprint:
                    return command.PublishShipBlueprint != null && command.PublishShipBlueprint.Draft != null && DefinitionId.TryParse(command.PublishShipBlueprint.BlueprintId, out _) && command.PublishShipBlueprint.Revision > 0;
                case CommandKind.EnqueueBlueprintConstruction:
                    return command.EnqueueBlueprintConstruction != null && DefinitionId.TryParse(command.EnqueueBlueprintConstruction.BlueprintId, out _) && command.EnqueueBlueprintConstruction.Revision > 0 && EntityId.TryParse(command.EnqueueBlueprintConstruction.ColonyId, out _);
                case CommandKind.EnqueueProduction:
                    return command.EnqueueProduction != null && EntityId.TryParse(command.EnqueueProduction.ColonyId, out _)
                        && DefinitionId.TryParse(command.EnqueueProduction.DefinitionId, out _);
                case CommandKind.CancelProduction:
                    return command.CancelProduction != null && EntityId.TryParse(command.CancelProduction.ColonyId, out _)
                        && EntityId.TryParse(command.CancelProduction.QueueItemId, out _);
                case CommandKind.SetResearchAllocation: return command.SetResearchAllocation != null;
                case CommandKind.SetScienceAllocation: return command.SetScienceAllocation != null;
                case CommandKind.SetEngineeringTarget:
                    return command.SetEngineeringTarget != null && (command.SetEngineeringTarget.TargetDefinitionId == null || DefinitionId.TryParse(command.SetEngineeringTarget.TargetDefinitionId, out _));
                case CommandKind.SetWorldGovernancePolicy:
                    return command.SetWorldGovernancePolicy != null && EntityId.TryParse(command.SetWorldGovernancePolicy.ColonyId,out _) && DefinitionId.TryParse(command.SetWorldGovernancePolicy.PolicyDefinitionId,out _);
                case CommandKind.SetFleetRoute:
                    return command.SetFleetRoute != null && EntityId.TryParse(command.SetFleetRoute.FleetId, out _)
                        && command.SetFleetRoute.RouteNodeIds != null && command.SetFleetRoute.RouteNodeIds.All(id => EntityId.TryParse(id, out _));
                default: return false;
            }
        }

        internal static GameStateDto CopyState(GameStateDto source) => new GameStateDto
        {
            Blueprints = source.Blueprints!.OrderBy(b => b.BlueprintId, StringComparer.Ordinal).ThenBy(b => b.Revision).Select(BlueprintCopies.Copy).ToList(),
            PendingCombats = source.PendingCombats!.OrderBy(c => c.Request.LocationNodeId, StringComparer.Ordinal).ThenBy(c => c.Request.EncounterId, StringComparer.Ordinal)
                .Select(c => new PendingCombatDto { Request = CombatEncounterScaffold.CopyRequest(c.Request) }).ToList(),
            ResolvedCombats = source.ResolvedCombats!.OrderBy(c => c.Request.LocationNodeId, StringComparer.Ordinal).ThenBy(c => c.Request.EncounterId, StringComparer.Ordinal)
                .Select(c => new ResolvedCombatDto { Request = CombatEncounterScaffold.CopyRequest(c.Request), Result = CombatEncounterScaffold.CopyResult(c.Result) }).ToList(),
            ResearchPaceFactor = source.ResearchPaceFactor,
            LastResearchReceipts = source.LastResearchReceipts!.Select(ScienceData.Copy).ToList(),
            Phase = source.Phase,
            TurnNumber = source.TurnNumber,
            StateRevision = source.StateRevision,
            NextEntitySequence = source.NextEntitySequence,
            RngState = new RngStateDto
            {
                AlgorithmId = source.RngState.AlgorithmId,
                AlgorithmVersion = source.RngState.AlgorithmVersion,
                State = source.RngState.State
            },
            EntityIdentities = source.EntityIdentities.OrderBy(e => e.EntityId, StringComparer.Ordinal)
                .Select(e => new EntityIdentityDto { EntityId = e.EntityId, DefinitionId = e.DefinitionId }).ToList(),
            AcceptedCommandIds = source.AcceptedCommandIds!.OrderBy(c => c, StringComparer.Ordinal).ToList(),
            Civilizations = source.Civilizations!.OrderBy(c => c.CivilizationId, StringComparer.Ordinal)
                .Select(ScienceData.Copy).ToList(),
            StarSystems = source.StarSystems!.OrderBy(n => n.SystemId, StringComparer.Ordinal)
                .Select(n => new StarSystemStateDto { SystemId = n.SystemId, DefinitionId = n.DefinitionId, OwnerCivilizationId = n.OwnerCivilizationId }).ToList(),
            Colonies = source.Colonies!.OrderBy(c => c.ColonyId, StringComparer.Ordinal).Select(PopulationData.Copy).ToList(),
            NavigationKnowledge=source.NavigationKnowledge!.OrderBy(k=>k.CivilizationId,StringComparer.Ordinal).Select(PopulationData.Copy).ToList(),
            TraversalEffects=source.TraversalEffects!.OrderBy(e=>e.EffectId,StringComparer.Ordinal).Select(PopulationData.Copy).ToList(),
            NavigationStatistics=PopulationData.Copy(source.NavigationStatistics!),
            Planets = source.Planets!.OrderBy(p => p.PlanetId, StringComparer.Ordinal).Select(PopulationData.Copy).ToList(),
            Displacements = source.Displacements!.OrderBy(r => r.DisplacementId, StringComparer.Ordinal).Select(PopulationData.Copy).ToList(),
            LastDeliveries = source.LastDeliveries!.OrderBy(r => r.DestinationPlanetId, StringComparer.Ordinal).ThenBy(r => r.ResourceDefinitionId, StringComparer.Ordinal).ThenBy(r => r.OriginPlanetId, StringComparer.Ordinal).Select(PopulationData.Copy).ToList(),
            PopulationStatistics = PopulationData.Copy(source.PopulationStatistics!),
            Ships = source.Ships!.OrderBy(s => s.ShipId, StringComparer.Ordinal).Select(s => new ShipStateDto
            {
                ShipId = s.ShipId,
                Blueprint = s.Blueprint == null ? null : BlueprintCopies.Copy(s.Blueprint),
                ShipDesignDefinitionId = s.ShipDesignDefinitionId,
                OwnerCivilizationId = s.OwnerCivilizationId,
                FleetId = s.FleetId,
                CurrentHull = s.CurrentHull
            }).ToList(),
            Fleets = source.Fleets!.OrderBy(f => f.FleetId, StringComparer.Ordinal).Select(f => new FleetStateDto
            {
                FleetId = f.FleetId,
                OwnerCivilizationId = f.OwnerCivilizationId,
                TransitTotalCost=f.TransitTotalCost,TransitProgress=f.TransitProgress,
                LocationNodeId = f.LocationNodeId,
                ShipIds = new List<string>(f.ShipIds!),
                RouteNodeIds = new List<string>(f.RouteNodeIds!)
            }).ToList()
        };

        private static CommandEnvelope? CopyCommand(CommandEnvelope? source)
        {
            if (source == null) return null;
            return new CommandEnvelope
            {
                CommandId = source.CommandId,
                ActorCivilizationId = source.ActorCivilizationId,
                ExpectedTurn = source.ExpectedTurn,
                ExpectedRevision = source.ExpectedRevision,
                Kind = source.Kind,
                PublishShipBlueprint = source.PublishShipBlueprint == null ? null : new PublishShipBlueprintPayload { BlueprintId = source.PublishShipBlueprint.BlueprintId, Revision = source.PublishShipBlueprint.Revision, Draft = source.PublishShipBlueprint.Draft == null ? null : BlueprintCopies.Copy(source.PublishShipBlueprint.Draft) },
                EnqueueBlueprintConstruction = source.EnqueueBlueprintConstruction == null ? null : new EnqueueBlueprintConstructionPayload { ColonyId = source.EnqueueBlueprintConstruction.ColonyId, BlueprintId = source.EnqueueBlueprintConstruction.BlueprintId, Revision = source.EnqueueBlueprintConstruction.Revision },
                SetWorldGovernancePolicy = source.SetWorldGovernancePolicy == null ? null : new SetWorldGovernancePolicyPayload { ColonyId=source.SetWorldGovernancePolicy.ColonyId, PolicyDefinitionId=source.SetWorldGovernancePolicy.PolicyDefinitionId },
                EnqueueProduction = source.EnqueueProduction == null ? null : new EnqueueProductionPayload
                { ColonyId = source.EnqueueProduction.ColonyId, DefinitionId = source.EnqueueProduction.DefinitionId },
                CancelProduction = source.CancelProduction == null ? null : new CancelProductionPayload
                { ColonyId = source.CancelProduction.ColonyId, QueueItemId = source.CancelProduction.QueueItemId },
                SetResearchAllocation = source.SetResearchAllocation == null ? null : new SetResearchAllocationPayload { ScienceAllocationUnits = source.SetResearchAllocation.ScienceAllocationUnits },
                SetScienceAllocation = source.SetScienceAllocation == null ? null : new SetScienceAllocationPayload { Allocations = source.SetScienceAllocation.Allocations?.Select(a=>a==null?null!:new ScienceAllocationDto{Discipline=a.Discipline,Weight=a.Weight}).ToList() },
                SetEngineeringTarget = source.SetEngineeringTarget == null ? null : new SetEngineeringTargetPayload
                { TargetDefinitionId = source.SetEngineeringTarget.TargetDefinitionId },
                SetFleetRoute = source.SetFleetRoute == null ? null : new SetFleetRoutePayload
                {
                    FleetId = source.SetFleetRoute.FleetId,
                    RouteNodeIds = source.SetFleetRoute.RouteNodeIds == null
                        ? null : new List<string>(source.SetFleetRoute.RouteNodeIds)
                }
            };
        }
    }
}
