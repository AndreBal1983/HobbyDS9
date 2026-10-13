using System;
using System.Collections.Generic;
using System.Linq;
using HobbyDS9.Contracts;
using HobbyDS9.Simulation;

namespace HobbyDS9.Application
{
    // Synchronous local application boundary. Simulation alone owns mutable state.
    // Hosts own JSON, files, content hashing, UI selection and scheduling.
    public sealed partial class CampaignApplication : ICampaignSession
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
        public CampaignShipDesignCatalogueSnapshot? GetShipDesignCatalogue(EntityId actor) => session.CaptureShipDesignCatalogue(actor.Value);
        public IReadOnlyList<CampaignBlueprintSnapshot> GetPublishedShipBlueprints() => session.CaptureBlueprints();
        public IReadOnlyList<BlueprintConstructionSnapshot> GetBlueprintConstructionOptions(EntityId actor, EntityId colony) => session.CaptureBlueprintConstruction(actor.Value, colony.Value);
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
        private CombatRequestDto PlayerRequest(string encounterId, EntityId actorId)
        {
            var actor = session.CaptureSnapshot().Actors.SingleOrDefault(c => c.CivilizationId == actorId);
            if (actor == null || actor.ControllerKind != ControllerKind.Human)
                throw new ArgumentException("Only a persisted human actor can supply player combat plans.", nameof(actorId));
            return session.GetPendingCombatRequests().SingleOrDefault(r => r.EncounterId == encounterId)
                ?? throw new ArgumentException("Encounter is not pending.", nameof(encounterId));
        }
        public IReadOnlyList<FleetCombatPlanDto> GetPlayerCombatPlans(string encounterId, EntityId actorId)
        {
            var request = PlayerRequest(encounterId, actorId);
            var owned = request.Participants.Where(p => p.CivilizationId == actorId.Value).Select(p => p.FleetId).ToList();
            return DeterministicPhasedCombatResolver.CreateDefaultPlans(request).Where(p => owned.Contains(p.FleetId))
                .OrderBy(p => p.FleetId, StringComparer.Ordinal).ToList().AsReadOnly();
        }
        public CombatResultDto ResolvePlayerCombat(string encounterId, EntityId actorId, IReadOnlyList<FleetCombatPlanDto>? humanPlans = null)
        {
            // Always resolve the frozen authoritative request, never a client-edited DTO.
            var request = PlayerRequest(encounterId, actorId);
            var owned = new HashSet<string>(request.Participants.Where(p => p.CivilizationId == actorId.Value).Select(p => p.FleetId), StringComparer.Ordinal);
            return ResolveOwned(request, owned, humanPlans);
        }
        private CombatRequestDto PendingRequest(string encounterId) => session.GetPendingCombatRequests()
            .SingleOrDefault(r => r.EncounterId == encounterId) ?? throw new ArgumentException("Encounter is not pending.", nameof(encounterId));
        private HashSet<string> HumanFleets(CombatRequestDto request)
        {
            var humans = new HashSet<string>(session.CaptureSnapshot().Actors.Where(a => a.ControllerKind == ControllerKind.Human)
                .Select(a => a.CivilizationId.Value), StringComparer.Ordinal);
            return new HashSet<string>(request.Participants.Where(p => humans.Contains(p.CivilizationId)).Select(p => p.FleetId), StringComparer.Ordinal);
        }
        public IReadOnlyList<FleetCombatPlanDto> GetHumanCombatPlans(string encounterId)
        {
            var request = PendingRequest(encounterId); var owned = HumanFleets(request);
            return DeterministicPhasedCombatResolver.CreateDefaultPlans(request).Where(p => owned.Contains(p.FleetId))
                .OrderBy(p => p.FleetId, StringComparer.Ordinal).ToList().AsReadOnly();
        }
        public CombatResultDto ResolveHumanCombat(string encounterId, IReadOnlyList<FleetCombatPlanDto>? humanPlans = null)
        {
            var request = PendingRequest(encounterId);
            return ResolveOwned(request, HumanFleets(request), humanPlans);
        }
        private CombatResultDto ResolveOwned(CombatRequestDto request, HashSet<string> owned, IReadOnlyList<FleetCombatPlanDto>? humanPlans)
        {
            var overrides = new Dictionary<string, FleetCombatPlanDto>(StringComparer.Ordinal);
            if (humanPlans != null)
                foreach (var plan in humanPlans)
                {
                    if (plan == null || !owned.Contains(plan.FleetId) || overrides.ContainsKey(plan.FleetId))
                        throw new ArgumentException("Player plans must uniquely identify fleets owned by the human actor.", nameof(humanPlans));
                    overrides.Add(plan.FleetId, new FleetCombatPlanDto { FleetId = plan.FleetId,
                        LongRangeTactic = plan.LongRangeTactic, MediumRangeTactic = plan.MediumRangeTactic,
                        CloseRangeTactic = plan.CloseRangeTactic, TargetPriority = plan.TargetPriority,
                        RetreatHullPercent = plan.RetreatHullPercent, RetreatNodeId = plan.RetreatNodeId });
                }
            var composed = DeterministicPhasedCombatResolver.CreateDefaultPlans(request)
                .Select(plan => overrides.TryGetValue(plan.FleetId, out var selected) ? selected : plan).ToList();
            return resolver.Resolve(request, composed);
        }
        public IReadOnlyList<BattleReportSnapshot> GetBattleReports() => session.GetResolvedCombats()
            .OrderBy(c => c.Request.EncounterId, StringComparer.Ordinal).Select(c => new BattleReportSnapshot(c)).ToList().AsReadOnly();
        public CombatSubmissionResult SubmitCombatResult(CombatResultDto? result, int expectedTurn, long expectedRevision) => session.SubmitCombatResult(result, expectedTurn, expectedRevision);
        public TurnResult FinalizeCombat(int expectedTurn, long expectedRevision) => session.FinalizeCombat(expectedTurn, expectedRevision);
        public SaveEnvelopeDto ExportSave() => session.ExportSave();
    }
}
