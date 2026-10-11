#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using HobbyDS9.Application;
using HobbyDS9.Contracts;

namespace HobbyDS9.UnityClient
{
    public sealed class ClientCommandIds
    {
        private ulong next;
        // Presentation-local counter, not the campaign entity allocator or RNG.
        // Reloads may restart it: the persisted planning ledger is always checked.
        public EntityId Next(CampaignSnapshot observed)
        {
            var accepted = new HashSet<EntityId>(observed.AcceptedPlayerCommandIds);
            EntityId candidate;
            do
            {
                candidate = new EntityId("player-ui." + next.ToString(CultureInfo.InvariantCulture));
                next = checked(next + 1);
            } while (accepted.Contains(candidate));
            return candidate;
        }
    }

    public sealed class RoutePreview
    {
        public EntityId FleetId { get; }
        public EntityId ActorId { get; }
        public NavigationNodeId Origin { get; }
        public NavigationNodeId Destination { get; }
        public NavigationRoute Route { get; }
        public CampaignSnapshot Observed { get; }
        public FleetTravelSnapshot? Travel { get; }
        internal RoutePreview(EntityId fleet, EntityId actor, NavigationNodeId origin, NavigationNodeId destination,
            NavigationRoute route, CampaignSnapshot observed, FleetTravelSnapshot? travel)
        { FleetId = fleet; ActorId = actor; Origin = origin; Destination = destination; Route = route; Observed = observed; Travel = travel; }
    }

    // Pure presentation controller, shared by Unity and ordinary .NET tests.
    // All methods take/retain the snapshot the caller actually saw. No retry.
    public sealed class StrategicClient
    {
        private ICampaignSession session;
        private readonly Func<SaveEnvelopeDto, ICampaignSession> load;
        private readonly ClientCommandIds ids = new ClientCommandIds();
        public CampaignSnapshot Snapshot { get; private set; }
        public StrategicClient(ICampaignSession session, Func<SaveEnvelopeDto, ICampaignSession> load)
        { this.session = session; this.load = load; Snapshot = session.CaptureSnapshot(); }
        public void Refresh() => Snapshot = session.CaptureSnapshot();
        public SaveEnvelopeDto ExportSave() => session.ExportSave();
        public void Load(SaveEnvelopeDto save)
        {
            // Validate/create/capture before publishing either reference.
            var replacement = load(save);
            var snapshot = replacement.CaptureSnapshot();
            session = replacement; Snapshot = snapshot;
        }
        public CommandResult Research(CampaignSnapshot observed, EntityId actor, DefinitionId target)
        {
            var command = Envelope(observed, actor, CommandKind.SetResearchTarget);
            command.SetResearchTarget = new SetResearchTargetPayload { TargetDefinitionId = target.Value };
            return Submit(command);
        }
        public CommandResult Enqueue(CampaignSnapshot observed, EntityId actor, EntityId colony, DefinitionId design)
        {
            var command = Envelope(observed, actor, CommandKind.EnqueueProduction);
            command.EnqueueProduction = new EnqueueProductionPayload { ColonyId = colony.Value, DefinitionId = design.Value };
            return Submit(command);
        }
        public CommandResult Cancel(CampaignSnapshot observed, EntityId actor, EntityId colony, EntityId queueItem)
        {
            var command = Envelope(observed, actor, CommandKind.CancelProduction);
            command.CancelProduction = new CancelProductionPayload { ColonyId = colony.Value, QueueItemId = queueItem.Value };
            return Submit(command);
        }
        public CommandResult Governance(CampaignSnapshot observed, EntityId actor, EntityId colony, DefinitionId policy)
        {
            var command = Envelope(observed, actor, CommandKind.SetWorldGovernancePolicy);
            command.SetWorldGovernancePolicy = new SetWorldGovernancePolicyPayload { ColonyId=colony.Value, PolicyDefinitionId=policy.Value };
            return Submit(command);
        }
        private CommandEnvelope Envelope(CampaignSnapshot observed, EntityId actor, CommandKind kind) => new CommandEnvelope
        {
            CommandId = ids.Next(Snapshot).Value, ActorCivilizationId = actor.Value, Kind = kind,
            ExpectedTurn = observed.Campaign.TurnNumber, ExpectedRevision = observed.Campaign.StateRevision
        };
        private CommandResult Submit(CommandEnvelope command)
        { var result = session.SubmitCommand(command); Refresh(); return result; }
        public RoutePreview Preview(CampaignSnapshot observed, EntityId fleetId, NavigationNodeId destination)
        {
            var fleet = observed.Campaign.Fleets.Single(f => f.FleetId == fleetId);
            var origin = new NavigationNodeId(fleet.LocationNodeId.Value);
            var route=session.FindRoute(origin,destination);
            return new RoutePreview(fleetId, fleet.OwnerCivilizationId, origin, destination, route, observed, route.Success?session.PreviewFleetTravel(fleetId,route.Steps):null);
        }
        public CommandResult SubmitRoute(RoutePreview preview)
        {
            // Passing null for failed previews lets the normal command validator reject.
            var result = session.SubmitFleetRoute(ids.Next(Snapshot), preview.ActorId,
                preview.Observed.Campaign.TurnNumber, preview.Observed.Campaign.StateRevision,
                preview.FleetId, preview.Route.Success ? preview.Route.Steps : null);
            Refresh(); return result;
        }
        public TurnResult EndTurn(CampaignSnapshot observed)
        { var result = session.AdvanceTurn(observed.Campaign.TurnNumber, observed.Campaign.StateRevision); Refresh(); return result; }
        public IReadOnlyList<CombatRequestDto> Pending() => session.GetPendingCombatRequests();
        public CombatSubmissionResult Resolve(CampaignSnapshot observed, CombatRequestDto request, IReadOnlyList<FleetCombatPlanDto>? plans = null)
        {
            var result = plans == null ? session.ResolveCombat(request) : session.ResolveCombat(request, plans);
            var submitted = session.SubmitCombatResult(result, observed.Campaign.TurnNumber, observed.Campaign.StateRevision);
            Refresh(); return submitted;
        }
        public TurnResult FinalizeCombats(CampaignSnapshot observed)
        { var result = session.FinalizeCombat(observed.Campaign.TurnNumber, observed.Campaign.StateRevision); Refresh(); return result; }
    }

    public static class CommandFeedback
    {
        public static string Describe(CommandResult result) => result.Accepted ? "Order accepted." : Explain(result.ErrorCode) + " [" + result.ErrorCode + "]";
        public static string Explain(CommandErrorCode code)
        {
            switch (code)
            {
                case CommandErrorCode.None: return "Available";
                case CommandErrorCode.StaleRevision: case CommandErrorCode.StaleTurn: return "The campaign changed. Review the refreshed state and issue the order again.";
                case CommandErrorCode.CombatPending: return "Complete the pending battles before issuing strategic orders.";
                case CommandErrorCode.ResearchAlreadyUnlocked: return "Research already completed.";
                case CommandErrorCode.ResearchTargetUnchanged: return "Already the current research target.";
                case CommandErrorCode.ResearchPrerequisiteMissing: case CommandErrorCode.DesignResearchPrerequisiteMissing: return "Required research has not been completed.";
                case CommandErrorCode.InsufficientCredits: return "Not enough civilization credits.";
                case CommandErrorCode.InsufficientStrategicResource: return "A required strategic resource is unavailable locally. Check the delivery report.";
                case CommandErrorCode.ShipbuildingUnavailable: return "This world has no shipbuilding infrastructure.";
                case CommandErrorCode.ColonyNotOwned: return "This colony is not controlled by the selected civilization.";
                case CommandErrorCode.FleetNotOwned: return "This fleet is not controlled by the selected civilization.";
                case CommandErrorCode.ActorNotControlled: return "Only human-controlled civilizations can receive player orders.";
                case CommandErrorCode.FleetInTransit: return "This fleet is already traversing an edge; change its route after arrival.";
                case CommandErrorCode.GovernancePolicyUnchanged: return "This governance policy is already active.";
                case CommandErrorCode.UnknownGovernancePolicy: return "This governance policy is unavailable.";
                case CommandErrorCode.InvalidRoute: return "This route cannot be used by the fleet.";
                case CommandErrorCode.UnknownQueueItem: return "This queue item no longer exists.";
                case CommandErrorCode.DuplicateCommand: return "This order ID was already accepted; no order was repeated.";
                case CommandErrorCode.UnknownColony: case CommandErrorCode.UnknownFleet: case CommandErrorCode.InvalidTarget: return "The selected object is no longer available.";
                default: return "The order was rejected. Review the selection and current campaign state.";
            }
        }
    }
}
