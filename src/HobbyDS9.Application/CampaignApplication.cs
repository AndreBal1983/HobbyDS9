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
        private readonly IPlannedCombatResolver resolver = new DeterministicPhasedCombatResolver();

        private CampaignApplication(ScenarioDto scenario, SaveEnvelopeDto save)
        {
            session = new SimulationSession(save, scenario.Definitions,
                new SaveCompatibility(scenario.ScenarioId, scenario.RulesVersion, scenario.ContentHash));
            content = new ScenarioContentSnapshot(scenario.Definitions);

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
        private string Viewer => session.CaptureSnapshot().Actors.FirstOrDefault(a => a.ControllerKind == ControllerKind.Human)?.CivilizationId.Value
            ?? session.CaptureSnapshot().Actors.First().CivilizationId.Value;
        public CampaignSnapshot CaptureSnapshot()
        {
            var projection = session.CaptureClientProjection();
            var view = projection.Navigation;
            var navigation = new NavigationMapSnapshot(view.Nodes.Select(n => new NavigationNodeSnapshot(new NavigationNodeId(n.NodeId),
                n.AssociatedSystemId == null ? (EntityId?)null : new EntityId(n.AssociatedSystemId), n.DisplayName, n.PhenomenonDefinitionIds)),
                view.Connections.Select(e => new NavigationLinkSnapshot(new NavigationNodeId(e.FromNodeId), new NavigationNodeId(e.ToNodeId), e.Kind, e.EffectiveCost,
                    e.BaseCost, e.PhenomenonCost, e.ArtificialCost)), view.Phenomena);
            var campaign = projection.Campaign;
            return new CampaignSnapshot(campaign, content, navigation, projection.FleetTravel, projection.Knowledge);
        }
        public FleetTravelSnapshot GetFleetTravel(EntityId fleetId) => session.CaptureFleetTravel(fleetId.Value);
        public FleetTravelSnapshot? PreviewFleetTravel(EntityId fleetId, IReadOnlyList<NavigationNodeId> steps)
        {
            var fleet = session.CaptureSnapshot().Fleets.SingleOrDefault(f => f.FleetId == fleetId);
            if (fleet == null || fleet.TransitTotalCost > 0) return null;
            var previous = fleet.LocationNodeId.Value;
            var knowledge = session.CaptureNavigation(Viewer);
            foreach (var next in steps)
            {
                if (!knowledge.Connections.Any(e => (e.FromNodeId == previous && e.ToNodeId == next.Value) || (e.ToNodeId == previous && e.FromNodeId == next.Value))) return null;
                previous = next.Value;
            }
            return session.CaptureFleetTravel(fleetId.Value, steps.Select(n => n.Value).ToList());
        }
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
                    RouteNodeIds = steps?.Select(n => n.Value).ToList() }
            });
        }

        public NavigationRoute FindRoute(NavigationNodeId origin, NavigationNodeId destination)
        {
            NavigationRoute Fail(NavigationRouteError error) => new NavigationRoute(error, Array.Empty<NavigationNodeId>(), null);
            if (!origin.IsValid || !destination.IsValid) return Fail(NavigationRouteError.InvalidNode);
            var route = session.FindNavigationRoute(Viewer, origin.Value, destination.Value);
            if (!route.Success) return Fail(route.ErrorCode == RouteErrorCode.UnknownSystem ? NavigationRouteError.UnknownNode
                : route.ErrorCode == RouteErrorCode.CostOverflow ? NavigationRouteError.CostOverflow : NavigationRouteError.Unreachable);
            return new NavigationRoute(NavigationRouteError.None, route.Steps.Select(id => new NavigationNodeId(id)), route.TotalCost, route.Costs);
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
