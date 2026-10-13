#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using HobbyDS9.Application;
using HobbyDS9.Contracts;
using HobbyDS9.Contracts.ShipDesign;

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
        public long SessionGeneration { get; private set; }
        private readonly List<RecentCombatReport> recent = new List<RecentCombatReport>();
        public IReadOnlyList<RecentCombatReport> RecentReports => recent.ToList().AsReadOnly();
        public StrategicClient(ICampaignSession session, Func<SaveEnvelopeDto, ICampaignSession> load)
        { this.session = session; this.load = load; Snapshot = session.CaptureSnapshot(); RememberBuffered(); }
        public void Refresh() { Snapshot = session.CaptureSnapshot(); RememberBuffered(); }
        public SaveEnvelopeDto ExportSave() => session.ExportSave();
        public CampaignShipDesignCatalogueSnapshot? ShipDesignCatalogue(EntityId actor) => session.GetShipDesignCatalogue(actor);
        public IReadOnlyList<CampaignBlueprintSnapshot> Blueprints() => session.GetPublishedShipBlueprints();
        public IReadOnlyList<BlueprintConstructionSnapshot> BlueprintOptions(EntityId actor, EntityId colony) => session.GetBlueprintConstructionOptions(actor,colony);
        public CommandResult PublishBlueprint(CampaignSnapshot observed, EntityId actor, string id, int revision, ShipClassDraftDto? draft)
        {
            var command=Envelope(observed,actor,CommandKind.PublishShipBlueprint);
            command.PublishShipBlueprint=new PublishShipBlueprintPayload { BlueprintId=id,Revision=revision,Draft=CopyDraft(draft) };
            return Submit(command);
        }
        // Preserve malformed inputs as malformed inputs for authoritative rejection.
        private static ShipClassDraftDto? CopyDraft(ShipClassDraftDto? d) => d==null?null:new ShipClassDraftDto {
            ClassId=d.ClassId,DisplayName=d.DisplayName,HullVersionId=d.HullVersionId,RoleId=d.RoleId,EmbarkedCrew=d.EmbarkedCrew,
            Installations=d.Installations?.Select(i=>i==null?null!:new ShipInstallationDto { InstallationId=i.InstallationId,ComponentVersionId=i.ComponentVersionId,Quantity=i.Quantity }).ToList(),
            Profiles=d.Profiles?.Select(p=>p==null?null!:new ShipOperatingProfileDto { Kind=p.Kind,Allocations=p.Allocations?.Select(a=>a==null?null!:new ProfilePowerAllocationDto { InstallationId=a.InstallationId,RequestedPower=a.RequestedPower,AllocatedPower=a.AllocatedPower }).ToList() }).ToList()
        };
        public CommandResult EnqueueBlueprint(CampaignSnapshot observed, EntityId actor, EntityId colony, string id, int revision)
        {
            var command=Envelope(observed,actor,CommandKind.EnqueueBlueprintConstruction);
            command.EnqueueBlueprintConstruction=new EnqueueBlueprintConstructionPayload { ColonyId=colony.Value,BlueprintId=id,Revision=revision };
            return Submit(command);
        }
        public void Load(SaveEnvelopeDto save)
        {
            // Validate/create/capture before publishing either reference.
            var replacement = load(save);
            var snapshot = replacement.CaptureSnapshot();
            var restored = replacement.GetBattleReports().Select(report => new RecentCombatReport(report,
                report.Fleets.Any(f => snapshot.Campaign.Civilizations.Any(c => c.CivilizationId.Value == f.CivilizationId && c.ControllerKind == ControllerKind.Human)))).ToList();
            session = replacement; Snapshot = snapshot; SessionGeneration++; recent.Clear(); recent.AddRange(restored); TrimHistory();
        }
        public CommandResult Engineering(CampaignSnapshot observed, EntityId actor, DefinitionId? target)
        {
            var command = Envelope(observed, actor, CommandKind.SetEngineeringTarget);
            command.SetEngineeringTarget = new SetEngineeringTargetPayload { TargetDefinitionId = target?.Value };
            return Submit(command);
        }
        public CommandResult AllocateResearch(CampaignSnapshot observed, EntityId actor, int scienceUnits)
        {
            var command=Envelope(observed,actor,CommandKind.SetResearchAllocation);
            command.SetResearchAllocation=new SetResearchAllocationPayload{ScienceAllocationUnits=scienceUnits};return Submit(command);
        }
        public CommandResult AllocateScience(CampaignSnapshot observed, EntityId actor, IReadOnlyList<ScienceAllocationDto>? allocations)
        {
            var command=Envelope(observed,actor,CommandKind.SetScienceAllocation);
            command.SetScienceAllocation=new SetScienceAllocationPayload{Allocations=allocations?.Select(a=>a==null?null!:new ScienceAllocationDto{Discipline=a.Discipline,Weight=a.Weight}).ToList()};return Submit(command);
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
        public IReadOnlyList<BattleReportSnapshot> Reports() => session.GetBattleReports();
        public IReadOnlyList<FleetCombatPlanDto> PlayerPlans(CampaignSnapshot observed, CombatRequestDto request)
            => session.GetHumanCombatPlans(request.EncounterId);
        public CombatSubmissionResult Resolve(CampaignSnapshot observed, CombatRequestDto request, IReadOnlyList<FleetCombatPlanDto>? plans = null)
            => ResolveDetailed(observed, request, plans).Submission;
        public CombatResolutionReceipt ResolveDetailed(CampaignSnapshot observed, CombatRequestDto request, IReadOnlyList<FleetCombatPlanDto>? plans = null)
        {
            // Resolve the frozen pending request by ID; even caller-edited requests
            // cannot forge participation, stats, seeds or editable AI fleets.
            var frozen = session.GetPendingCombatRequests().Single(r => r.EncounterId == request.EncounterId);
            var result = session.ResolveHumanCombat(frozen.EncounterId, plans);
            var submitted = session.SubmitCombatResult(result, observed.Campaign.TurnNumber, observed.Campaign.StateRevision);
            var report = submitted.Status == CombatSubmissionStatus.Buffered
                ? new BattleReportSnapshot(new ResolvedCombatDto { Request = frozen, Result = result }) : null;
            Refresh();
            return new CombatResolutionReceipt(submitted, report, CombatOrchestrator.HumanInvolved(Snapshot, frozen));
        }
        private void RememberBuffered()
        {
            foreach (var report in session.GetBattleReports())
            {
                if (recent.Any(r => r.Report.EncounterId == report.EncounterId)) continue;
                var humans = Snapshot.Campaign.Civilizations.Where(c => c.ControllerKind == ControllerKind.Human)
                    .Select(c => c.CivilizationId.Value).ToList();
                recent.Add(new RecentCombatReport(report, report.Fleets.Any(f => humans.Contains(f.CivilizationId))));
            }
            TrimHistory();
        }
        private void TrimHistory()
        {
            recent.Sort((a,b) => { var turn = b.Report.TurnNumber.CompareTo(a.Report.TurnNumber);
                return turn != 0 ? turn : StringComparer.Ordinal.Compare(a.Report.EncounterId, b.Report.EncounterId); });
            // Keep every encounter from the latest turn, even if one huge checkpoint
            // exceeds the normal 50-report bound. Never drop a current human report.
            if (recent.Count > 50)
            {
                var latest = recent[0].Report.TurnNumber;
                var keep = Math.Max(50, recent.Count(r => r.Report.TurnNumber == latest));
                if (recent.Count > keep) recent.RemoveRange(keep, recent.Count - keep);
            }
        }
        public TurnResult FinalizeCombats(CampaignSnapshot observed)
        { var result = session.FinalizeCombat(observed.Campaign.TurnNumber, observed.Campaign.StateRevision); Refresh(); return result; }
    }

    // Selection-independent presentation model. All facts are detached Application
    // projections; commands retain the exact turn/revision displayed by this model.
    public sealed class ProductionOverview
    {
        private readonly StrategicClient client;
        public CampaignSnapshot Observed { get; }
        public CivilizationSnapshot? Actor { get; }
        public IReadOnlyList<ColonySnapshot> Worlds { get; }
        private readonly Dictionary<EntityId,IReadOnlyList<BlueprintConstructionSnapshot>> blueprints = new Dictionary<EntityId,IReadOnlyList<BlueprintConstructionSnapshot>>();
        private readonly long generation;
        private readonly HashSet<string> submitted = new HashSet<string>(StringComparer.Ordinal);
        public ProductionOverview(StrategicClient client, CampaignSnapshot observed, EntityId? actor=null)
        {
            this.client = client; Observed = observed; generation=client.SessionGeneration;
            Actor = observed.Campaign.Civilizations.Where(c => c.ControllerKind == ControllerKind.Human)
                .OrderBy(c => c.CivilizationId.Value, StringComparer.Ordinal).FirstOrDefault(c=>!actor.HasValue || c.CivilizationId==actor.Value);
            Worlds = observed.Campaign.Colonies.Where(c => Actor != null && c.OwnerCivilizationId == Actor.CivilizationId)
                .OrderBy(c => c.ColonyId.Value, StringComparer.Ordinal).ToList().AsReadOnly();
            foreach(var world in Worlds)blueprints[world.ColonyId]=client.BlueprintOptions(Actor!.CivilizationId,world.ColonyId);
        }
        public IReadOnlyList<BlueprintConstructionSnapshot> Blueprints(EntityId world) => blueprints.TryGetValue(world,out var options)?options:Array.Empty<BlueprintConstructionSnapshot>();
        public CommandResult EnqueueBlueprint(EntityId world, BlueprintConstructionSnapshot option)
        {
            if(generation!=client.SessionGeneration)throw new InvalidOperationException("Campaign replaced. Review current production options.");
            if(!Blueprints(world).Contains(option))throw new InvalidOperationException("Select an owned colony and its observed blueprint option.");
            var key=world.Value+"/"+option.Blueprint.Blueprint.BlueprintId+"/"+option.Blueprint.Blueprint.Revision;
            if(!submitted.Add(key))throw new InvalidOperationException("This displayed order was already submitted. Review production before ordering again.");
            return client.EnqueueBlueprint(Observed,Actor!.CivilizationId,world,option.Blueprint.Blueprint.BlueprintId,option.Blueprint.Blueprint.Revision);
        }
        public IReadOnlyList<ProductionOption> Designs(EntityId world) => Actor == null
            ? Array.Empty<ProductionOption>() : StrategicOptions.Production(Observed, Actor.CivilizationId, world);
        public bool CanCancel => Actor != null && StrategicOptions.PlanningAccess(Observed, Actor.CivilizationId) == CommandErrorCode.None;
        public CommandResult Enqueue(EntityId world, DefinitionId design) => client.Enqueue(Observed, Actor!.CivilizationId, world, design);
        public CommandResult Cancel(EntityId world, EntityId item) => client.Cancel(Observed, Actor!.CivilizationId, world, item);
    }

    public static class CombatPresentation
    {
        public static string OwnerName(CampaignSnapshot snapshot, string id)
        {
            var actor = snapshot.Campaign.Civilizations.Single(c => c.CivilizationId.Value == id);
            return snapshot.Content.Civilizations.Single(c => c.DefinitionId == actor.DefinitionId).DisplayName;
        }
        public static string Heading(CampaignSnapshot snapshot, IEnumerable<string> participants)
        {
            var ids = participants.Distinct(StringComparer.Ordinal).OrderBy(id => snapshot.Campaign.Civilizations.Single(c => c.CivilizationId.Value == id).ControllerKind == ControllerKind.Human ? 0 : 1)
                .ThenBy(id => id, StringComparer.Ordinal).ToList();
            return ids.Count + "-way engagement: " + string.Join(" vs ", ids.Select(id => OwnerName(snapshot, id)));
        }
    }

    public static class CommandFeedback
    {
        public static string Describe(CommandResult result) => result.Accepted ? "Order accepted." : Explain(result.ErrorCode) + " [" + result.ErrorCode + "]";
        public static string Explain(CommandErrorCode code)
        {
            switch (code)
            {
                case CommandErrorCode.None: return "Available";
                case CommandErrorCode.BlueprintRevisionConflict: return "Blueprint history changed. Refresh and review the next revision.";
                case CommandErrorCode.BlueprintNotOwned: return "This blueprint belongs to another civilization.";
                case CommandErrorCode.InvalidShipBlueprint: return "The backend rejected this fit. Review all profiles and validation issues.";
                case CommandErrorCode.ShipDesignVersionUnavailable: return "A hull or package requires unavailable Mature Engineering.";
                case CommandErrorCode.UnknownBlueprint: return "This exact blueprint revision is unavailable.";
                case CommandErrorCode.ShipDesignCatalogueUnavailable: return "This campaign has no ship design catalogue.";
                case CommandErrorCode.StaleRevision: case CommandErrorCode.StaleTurn: return "The campaign changed. Review the refreshed state and issue the order again.";
                case CommandErrorCode.CombatPending: return "Complete the pending battles before issuing strategic orders.";
                case CommandErrorCode.EngineeringAlreadyMature: return "Engineering application is already Mature.";
                case CommandErrorCode.EngineeringTargetUnchanged: return "Already the current Engineering target.";
                case CommandErrorCode.EngineeringPrerequisiteMissing: case CommandErrorCode.DesignEngineeringPrerequisiteMissing: return "Required Engineering applications must be Mature.";
                case CommandErrorCode.EngineeringScienceRequirementMissing: return "Required Science breakthroughs have not been achieved.";
                case CommandErrorCode.ResearchAllocationUnchanged: return "This allocation is already active.";
                case CommandErrorCode.InvalidResearchAllocation: return "Science share must be between 0 and 600 allocation units.";
                case CommandErrorCode.ScienceAllocationUnchanged: return "This Science distribution is already active.";
                case CommandErrorCode.InvalidScienceAllocation: return "Provide all six Science disciplines with nonnegative weights totaling 600.";
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
