using System;
using System.Collections.Generic;
using System.Linq;
using HobbyDS9.Contracts;
using HobbyDS9.Simulation;

namespace HobbyDS9.Application
{
    // Synchronous local application boundary. Simulation alone owns mutable state.
    // Hosts own JSON, files, content hashing, UI selection and scheduling.
    public sealed class CampaignApplication : ICampaignSession
    {
        private readonly SimulationSession session;
        private readonly ScenarioContentSnapshot content;
        private readonly NavigationMapSnapshot navigation;
        private readonly IReadOnlyDictionary<NavigationNodeId, EntityId> systemsByNode;
        private readonly IReadOnlyDictionary<EntityId, NavigationNodeId> nodesBySystem;
        private readonly IPlannedCombatResolver resolver = new DeterministicPhasedCombatResolver();

        private CampaignApplication(ScenarioDto scenario, SaveEnvelopeDto save)
        {
            session = new SimulationSession(save, scenario.Definitions,
                new SaveCompatibility(scenario.ScenarioId, scenario.RulesVersion, scenario.ContentHash));
            content = new ScenarioContentSnapshot(scenario.Definitions);
            // Compatibility adapter only. No navigation topology or gameplay changes.
            nodesBySystem = session.Galaxy.Nodes.ToDictionary(id => id, id => new NavigationNodeId(id.Value));
            systemsByNode = nodesBySystem.ToDictionary(pair => pair.Value, pair => pair.Key);
            navigation = new NavigationMapSnapshot(session.Galaxy.Nodes.Select(id => new NavigationNodeSnapshot(nodesBySystem[id], id)),
                session.Galaxy.Edges.Select(e => new NavigationLinkSnapshot(nodesBySystem[e.EndpointA], nodesBySystem[e.EndpointB], e.Kind, e.TraversalCost)));
        }

        public static ICampaignSession StartScenario(ScenarioDto scenario)
        {
            FoundationValidator.ValidateScenario(scenario).ThrowIfInvalid(nameof(scenario));
            return new CampaignApplication(scenario, new SaveEnvelopeDto
            {
                SaveFormatVersion = FoundationVersions.SaveFormatVersion, ScenarioId = scenario.ScenarioId,
                RulesVersion = scenario.RulesVersion, ContentHash = scenario.ContentHash, GameState = scenario.InitialState
            });
        }

        // Returns a new session; a failed load cannot replace or mutate a live session.
        public static ICampaignSession LoadSave(ScenarioDto scenario, SaveEnvelopeDto save)
        {
            FoundationValidator.ValidateScenario(scenario).ThrowIfInvalid(nameof(scenario));
            return new CampaignApplication(scenario, save);
        }

        public static IReadOnlyList<ValidationIssue> ValidateScenario(ScenarioDto? scenario) => FoundationValidator.ValidateScenario(scenario).Issues;
        public static IReadOnlyList<ValidationIssue> ValidateSave(ScenarioDto scenario, SaveEnvelopeDto? save)
        {
            var issues = ValidateScenario(scenario);
            if (issues.Count != 0) return issues;
            return FoundationValidator.ValidateSave(save, scenario.Definitions,
                new SaveCompatibility(scenario.ScenarioId, scenario.RulesVersion, scenario.ContentHash)).Issues;
        }
        public CampaignSnapshot CaptureSnapshot() => new CampaignSnapshot(session.CaptureSnapshot(), content, navigation);
        public CommandResult SubmitCommand(CommandEnvelope? command) => session.SubmitCommand(command);
        public CommandResult SubmitFleetRoute(EntityId commandId, EntityId actorCivilizationId, int expectedTurn,
            long expectedRevision, EntityId fleetId, IReadOnlyList<NavigationNodeId>? steps)
        {
            // Unknown nodes translate to an invalid route payload, then pass through
            // the same phase/stamp/control/route validation as every player command.
            return SubmitCommand(new CommandEnvelope
            {
                CommandId = commandId.ToString(), ActorCivilizationId = actorCivilizationId.ToString(),
                ExpectedTurn = expectedTurn, ExpectedRevision = expectedRevision, Kind = CommandKind.SetFleetRoute,
                SetFleetRoute = new SetFleetRoutePayload { FleetId = fleetId.ToString(),
                    RouteSystemIds = steps?.Select(n => systemsByNode.TryGetValue(n, out var id) ? id.Value : string.Empty).ToList() }
            });
        }

        public NavigationRoute FindRoute(NavigationNodeId origin, NavigationNodeId destination)
        {
            NavigationRoute Fail(NavigationRouteError error) => new NavigationRoute(error, Array.Empty<NavigationNodeId>(), null);
            if (!origin.IsValid || !destination.IsValid) return Fail(NavigationRouteError.InvalidNode);
            if (!systemsByNode.TryGetValue(origin, out var from) || !systemsByNode.TryGetValue(destination, out var to)) return Fail(NavigationRouteError.UnknownNode);
            var route = session.Galaxy.FindRoute(from, to);
            if (!route.Success) return Fail(NavigationRouteError.Unreachable);
            // Current graph is unit-cost. The boundary explicitly reports cost,
            // without promising that step count predicts future movement timing.
            return new NavigationRoute(NavigationRouteError.None, route.Steps.Select(id => nodesBySystem[id]), route.Steps.Count);
        }

        public TurnResult AdvanceTurn(int expectedTurn, long expectedRevision) => session.AdvanceTurn(expectedTurn, expectedRevision);
        public IReadOnlyList<CombatRequestDto> GetPendingCombatRequests() => session.GetPendingCombatRequests();
        public IReadOnlyList<ResolvedCombatDto> GetResolvedCombats() => session.GetResolvedCombats();
        // Pure resolution is intentionally separate from stamped result submission.
        // Invalid resolver inputs throw, without changing campaign state.
        public CombatResultDto ResolveCombat(CombatRequestDto request) => resolver.Resolve(request);
        public CombatResultDto ResolveCombat(CombatRequestDto request, IReadOnlyList<FleetCombatPlanDto> plans) => resolver.Resolve(request, plans);
        public CombatSubmissionResult SubmitCombatResult(CombatResultDto? result, int expectedTurn, long expectedRevision) => session.SubmitCombatResult(result, expectedTurn, expectedRevision);
        public TurnResult FinalizeCombat(int expectedTurn, long expectedRevision) => session.FinalizeCombat(expectedTurn, expectedRevision);
        public SaveEnvelopeDto ExportSave() => session.ExportSave();
    }
}
