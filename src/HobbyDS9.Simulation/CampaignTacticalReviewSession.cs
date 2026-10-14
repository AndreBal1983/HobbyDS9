using System;
using System.Collections.Generic;
using System.Linq;
using HobbyDS9.Contracts;
using static HobbyDS9.Simulation.CampaignTacticalReviewData;

namespace HobbyDS9.Simulation
{
    // Authoritative owned campaign layer, reachable only through the explicit review factory.
    // Default v1 sessions and source eligibility remain closed. Travel integration is still gated.
    public sealed partial class CampaignTacticalReviewSession
    {
        private readonly object gate = new object();
        private readonly DefinitionCatalogDto definitions;
        private readonly SaveCompatibility compatibility;
        private readonly TacticalRulesV2Dto rules;
        private readonly List<CampaignTacticalCloakSourceDto> cloak;
        private readonly Dictionary<string,ControllerKind> controllers;
        private readonly int initialTurn;
        private CampaignTacticalReviewSaveDto state;
        private CampaignTacticalTravelSaveDto? travel;
        private List<CampaignDeclaredHostilityDto>? declaredHostilities;
        private List<CampaignContactKnowledgeDto>? authoredContactGrids;
        private int contactFreshness;
        private Dictionary<string,string>? authoredFleetOwners;
        private SessionPhase CurrentPhase=>travel?.Phase??state.Campaign.GameState.Phase!.Value;
        private IReadOnlyList<CombatRequestDto> CurrentRequests=>travel?.Requests??state.Campaign.GameState.PendingCombats!.Select(p=>p.Request).ToList();
        internal Action<int>? FinalizationProbe { get; set; }
        private CampaignTacticalReviewSession(CampaignTacticalReviewPackageDto package, CampaignTacticalReviewSaveDto? save,
            CampaignTacticalTravelPackageDto? physicalPackage=null,CampaignTacticalTravelSaveDto? physicalSave=null,
            CampaignIntegratedPlaytestPackageDto? integratedPackage=null,CampaignIntegratedPlaytestSaveDto? integratedSave=null)
        {
            ValidatePackage(package);
            definitions = SimulationSession.CopyDefinitions(package.Campaign!.Definitions);
            controllers = package.Campaign.InitialState.Civilizations!.ToDictionary(c => c.CivilizationId,c => c.ControllerKind!.Value,StringComparer.Ordinal);
            initialTurn = package.Campaign.InitialState.TurnNumber;
            if (integratedPackage != null)
            {
                integratedHash = integratedPackage.PackageHash;
                initialProductionCompleted = package.Campaign.InitialState.PopulationStatistics!.ProductionCompleted;
                constructionTemplates = integratedPackage.ConstructionTemplates!.Select(t => new CampaignIntegratedConstructionTemplateDto {
                    ShipDesignDefinitionId=t.ShipDesignDefinitionId,SourceProvisionShipId=t.SourceProvisionShipId }).ToList();
                builtShips = integratedSave == null ? new List<CampaignIntegratedBuiltShipDto>() : integratedSave.BuiltShips!.Select(CopyBuild).ToList();
            }
            compatibility = new SaveCompatibility(package.Campaign.ScenarioId, package.Campaign.RulesVersion, package.Campaign.ContentHash);
            rules = CopyRules(package.Rules!);
            cloak = package.CloakSources!.OrderBy(c => c.FitId, StringComparer.Ordinal).Select(c => new CampaignTacticalCloakSourceDto {
                FitId = c.FitId, FitRevision = c.FitRevision, Capable = c.Capable, ConcurrentPowerRequired = c.ConcurrentPowerRequired }).ToList();
            if(physicalPackage!=null)
            {
                CampaignTacticalTravelData.ValidatePackage(physicalPackage);
                declaredHostilities=physicalPackage.Hostilities!.Select(h=>new CampaignDeclaredHostilityDto { CivilizationA=h.CivilizationA,CivilizationB=h.CivilizationB }).ToList();
                authoredContactGrids=physicalPackage.InitialContacts!.Select(CampaignTacticalTravelData.Copy).ToList();
                authoredFleetOwners=package.Campaign.InitialState.Fleets!.ToDictionary(f=>f.FleetId,f=>f.OwnerCivilizationId,StringComparer.Ordinal);
                contactFreshness=physicalPackage.ContactFreshnessTurns!.Value;
                travel=physicalSave==null?new CampaignTacticalTravelSaveDto { FormatVersion=CampaignTacticalTravelIdentity.SaveFormat,
                    ProfileId=CampaignTacticalTravelIdentity.Profile,PackageHash=physicalPackage.PackageHash!,Phase=SessionPhase.Planning,
                    Requests=new List<CombatRequestDto>(),Orders=physicalPackage.InitialOrders!.Select(CampaignTacticalTravelData.Copy).ToList(),
                    Contacts=physicalPackage.InitialContacts!.Select(CampaignTacticalTravelData.Copy).ToList(),LastMovement=new List<CampaignFleetTravelReceiptDto>() }
                    :new CampaignTacticalTravelSaveDto { FormatVersion=physicalSave.FormatVersion,ProfileId=physicalSave.ProfileId,
                        PackageHash=physicalSave.PackageHash,Phase=physicalSave.Phase,Core=physicalSave.Core,Requests=physicalSave.Requests,
                        Orders=physicalSave.Orders,Contacts=physicalSave.Contacts,LastMovement=physicalSave.LastMovement,MovementProof=physicalSave.MovementProof };
                Require(travel.PackageHash==physicalPackage.PackageHash,"Different physical package.");
            }
            if (save == null)
            {
                var initial = new SaveEnvelopeDto { SaveFormatVersion = FoundationVersions.SaveFormatVersion,
                    ScenarioId = compatibility.ScenarioId, RulesVersion = compatibility.RulesVersion, ContentHash = compatibility.ContentHash,
                    GameState = SimulationSession.CopyState(package.Campaign.InitialState) };
                state = new CampaignTacticalReviewSaveDto { FormatVersion = CampaignTacticalReviewIdentity.SaveFormat,
                    ProfileId = CampaignTacticalReviewIdentity.Profile, ResolverId = CampaignTacticalReviewIdentity.Resolver,
                    ResolverVersion = CampaignTacticalReviewIdentity.ResolverVersion, PackageHash = package.PackageHash!, Campaign = initial,
                    Pools = initial.GameState.Ships!.Select(s => InitialPool(s.ShipId)).ToList(),
                    FleetSettings = initial.GameState.Fleets!.Select(f => new CampaignTacticalFleetSettingDto { FleetId = f.FleetId }).ToList(),
                    Battles = new List<CampaignTacticalBattleCheckpointDto>(), AcceptedRoundCommands = new List<string>() };
            }
            else
            {
                Require(save.PackageHash == package.PackageHash, "Different review package.");
                ValidateSave(save); state = Copy(save);
            }
            ValidateSave(state); state = Copy(state);
            if(travel!=null)
            {
                travel.Core=state;
                if(physicalSave==null)foreach(var o in travel.Orders)state.Campaign.GameState.Fleets!.Single(f=>f.FleetId==o.FleetId).RouteNodeIds=new List<string>(o.RouteNodeIds);
                ValidateTravelSave(travel);travel=CampaignTacticalTravelData.Copy(travel);state=travel.Core;
            }
        }
        public static CampaignTacticalReviewSession Start(CampaignTacticalReviewPackageDto package) => new CampaignTacticalReviewSession(package, null);
        public static CampaignTacticalReviewSession Load(CampaignTacticalReviewPackageDto package, CampaignTacticalReviewSaveDto save)
        { if (save == null) throw new ArgumentNullException(nameof(save)); return new CampaignTacticalReviewSession(package, save); }
        public CampaignTacticalReviewSaveDto ExportSave() { lock (gate) return Copy(state); }
        internal SessionSnapshot CaptureCampaign() { lock (gate) return Legacy(state).CaptureSnapshot(); }
        private SimulationSession Legacy(CampaignTacticalReviewSaveDto s) => new SimulationSession(s.Campaign, definitions, compatibility);
        private AuthoredTacticalShipProvisionDto Provision(string id) => ResolveProvision(id);
        private AuthoredTacticalFitDto Fit(string ship) { var p = Provision(ship); return definitions.AuthoredTacticalEquipment!.Fits!.Single(f => f.DefinitionId == p.FitDefinitionId && f.Revision == p.FitRevision); }
        private CampaignTacticalPoolDto InitialPool(string ship)
        {
            var p = Provision(ship); var f = Fit(ship);
            return Copy(new CampaignTacticalPoolDto { ShipId = ship, FitId = f.DefinitionId!, FitRevision = f.Revision!.Value, ProvenanceId = f.ProvenanceId!,
                Shields = p.InitialShields!, Ammunition = p.InitialAmmunition! });
        }
        private CampaignTacticalMutationResult Result(CampaignTacticalError error) => new CampaignTacticalMutationResult(error, state.Campaign.GameState.StateRevision);
        private CampaignTacticalError Actor(string id, bool human = true)
        {
            var actor = state.Campaign.GameState.Civilizations!.SingleOrDefault(c => c.CivilizationId == id);
            return actor == null ? CampaignTacticalError.UnknownActor : human && actor.ControllerKind != ControllerKind.Human
                ? CampaignTacticalError.ActorNotControlled : CampaignTacticalError.None;
        }
        public IReadOnlyList<string> Encounters(string actor)
        {
            lock (gate)
            {
                Require(Actor(actor) == CampaignTacticalError.None, "Only a controlled human actor can inspect encounters.");
                return state.Battles.Where(b => b.State.Formations.Any(f => f.SideId == actor)).Select(b => b.State.EncounterId).ToList().AsReadOnly();
            }
        }
        public CampaignTacticalMutationResult SetApproachCloak(string actor, string commandId, string fleet, bool enabled, int turn, long revision)
        {
            lock (gate)
            {
                var game = state.Campaign.GameState; var a = Actor(actor); if (a != CampaignTacticalError.None) return Result(a);
                if (CurrentPhase != SessionPhase.Planning) return Result(CampaignTacticalError.WrongPhase);
                if (turn != game.TurnNumber) return Result(CampaignTacticalError.StaleTurn);
                if (revision != game.StateRevision) return Result(CampaignTacticalError.StaleRevision);
                if (!EntityId.TryParse(commandId, out _) || commandId.StartsWith("ai-command.", StringComparison.Ordinal)) return Result(CampaignTacticalError.InvalidOrder);
                if (state.AcceptedRoundCommands.Contains(commandId, StringComparer.Ordinal) || game.AcceptedCommandIds!.Contains(commandId)) return Result(CampaignTacticalError.DuplicateCommand);
                if (!game.Fleets!.Any(f => f.FleetId == fleet && f.OwnerCivilizationId == actor)) return Result(CampaignTacticalError.InvalidOrder);
                if (revision == long.MaxValue) return Result(CampaignTacticalError.Overflow);
                var staged = Copy(state); staged.FleetSettings.Single(f => f.FleetId == fleet).ApproachCloak = enabled;
                staged.AcceptedRoundCommands.Add(commandId); staged.Campaign.GameState.StateRevision++;
                ValidateSave(staged); state = Copy(staged); return Result(CampaignTacticalError.None);
            }
        }
        public CampaignTacticalMutationResult SubmitStrategicCommand(CommandEnvelope? command)
        {
            lock (gate)
            {
                if(CurrentPhase!=SessionPhase.Planning)return Result(CampaignTacticalError.WrongPhase);
                if(travel!=null&&command?.Kind==CommandKind.SetFleetRoute)return Result(CampaignTacticalError.InvalidOrder);
                if (command != null && state.AcceptedRoundCommands.Contains(command.CommandId, StringComparer.Ordinal)) return Result(CampaignTacticalError.DuplicateCommand);
                if (command != null && (command.Kind == CommandKind.EnqueueProduction || command.Kind == CommandKind.EnqueueBlueprintConstruction) && !AdmittedConstruction(command))
                    return Result(CampaignTacticalError.UnsupportedSource); // no authored-v2 construction/provisioning contract.
                var session = Legacy(state); var result = session.SubmitCommand(command);
                if (!result.Accepted)
                    return Result(result.ErrorCode == CommandErrorCode.StaleTurn ? CampaignTacticalError.StaleTurn
                        : result.ErrorCode == CommandErrorCode.StaleRevision ? CampaignTacticalError.StaleRevision
                        : result.ErrorCode == CommandErrorCode.CombatPending ? CampaignTacticalError.WrongPhase
                        : result.ErrorCode == CommandErrorCode.DuplicateCommand ? CampaignTacticalError.DuplicateCommand
                        : result.ErrorCode == CommandErrorCode.ActorNotControlled ? CampaignTacticalError.ActorNotControlled
                        : result.ErrorCode == CommandErrorCode.UnknownActor ? CampaignTacticalError.UnknownActor
                        : result.ErrorCode == CommandErrorCode.RevisionExhausted ? CampaignTacticalError.Overflow : CampaignTacticalError.InvalidOrder);
                var next = Copy(state); next.Campaign = session.ExportSave(); ValidateSave(next); state = Copy(next); return Result(CampaignTacticalError.None);
            }
        }
        public CampaignTacticalMutationResult AdvanceTurn(int turn, long revision)
        {
            lock (gate)
            {
                if(travel!=null)return AdvancePhysicalTurn(turn,revision);
                if (state.Campaign.GameState.Phase != SessionPhase.Planning) return Result(CampaignTacticalError.WrongPhase);
                if (turn != state.Campaign.GameState.TurnNumber) return Result(CampaignTacticalError.StaleTurn);
                if (revision != state.Campaign.GameState.StateRevision) return Result(CampaignTacticalError.StaleRevision);
                var session = Legacy(state); var result = session.AdvanceTurn(turn, revision);
                if (result.Status == TurnStatus.Rejected) return Result(CampaignTacticalError.WrongPhase);
                var next = Copy(state); next.Campaign = session.ExportSave();
                if (next.Campaign.GameState.Ships!.Any(s => !next.Pools.Any(p => p.ShipId == s.ShipId))) return Result(CampaignTacticalError.UnsupportedSource);
                if (next.Campaign.GameState.Phase == SessionPhase.AwaitingCombat)
                {
                    foreach (var request in next.Campaign.GameState.PendingCombats!.Select(p => p.Request))
                    {
                        next.Battles.Add(Allocate(next, request));
                        foreach (var participant in request.Participants) next.Campaign.GameState.Fleets!.Single(f => f.FleetId == participant.FleetId).RouteNodeIds!.Clear();
                    }
                    foreach (var battle in next.Battles)
                    {
                        EnsureAi(next, battle);
                        while (battle.State.Completion == TacticalCompletion.None
                            && battle.State.Formations.Where(f => f.Status == TacticalFormationStatus.Active).All(f =>
                                next.Campaign.GameState.Civilizations!.Single(c => c.CivilizationId == f.SideId).ControllerKind == ControllerKind.AI))
                        {
                            if (next.Campaign.GameState.StateRevision == long.MaxValue) return Result(CampaignTacticalError.Overflow);
                            next.Campaign.GameState.StateRevision++; ResolveJoint(next, battle);
                        }
                    }
                }
                ValidateSave(next); state = Copy(next); return Result(CampaignTacticalError.None);
            }
        }
        private CampaignTacticalBattleCheckpointDto Allocate(CampaignTacticalReviewSaveDto save, CombatRequestDto request)
        {
            var game = save.Campaign.GameState;
            var battle = new TacticalEncounterV2Dto { EncounterId = request.EncounterId, RandomState = request.RandomState, Rules = CopyRules(rules) };
            foreach (var p in request.Participants.OrderBy(p => p.FleetId, StringComparer.Ordinal))
            {
                var fits = p.Ships.Select(s => Fit(s.ShipId)).ToList();
                int Median(Func<AuthoredTacticalFitDto, int> value) { var values = fits.Select(value).OrderBy(v => v).ToList(); return values[(values.Count - 1) / 2]; }
                battle.Formations.Add(new TacticalFormationV2Dto { FormationId = p.FleetId, SideId = p.CivilizationId,
                    Speed = Median(f => f.Capabilities!.Speed!.Value), Agility = Median(f => f.Capabilities!.Agility!.Value),
                    Sensors = fits.Max(f => f.Capabilities!.Sensors!.Value), Command = 0, Cohesion = 0, Objective = TacticalObjective.Defeat,
                    StandingOrder = new TacticalOrderV2Dto { FormationId = p.FleetId, Maneuver = TacticalManeuver.MaintainDistance,
                        PreferredRange = TacticalRange.Distant, TargetPriority = TacticalTargetPriority.LowestHullRatio },
                    Ships = p.Ships.OrderBy(s => s.ShipId, StringComparer.Ordinal).Select(s => Compile(game.Ships!.Single(c => c.ShipId == s.ShipId),
                        Fit(s.ShipId), save.Pools.Single(q => q.ShipId == s.ShipId))).ToList() });
            }
            foreach (var a in battle.Formations)
            foreach (var b in battle.Formations.Where(b => b.SideId != a.SideId && StringComparer.Ordinal.Compare(a.FormationId, b.FormationId) < 0))
                battle.Geometry.Add(new TacticalGeometryV2Dto { FormationA = a.FormationId, FormationB = b.FormationId,
                    Range = TacticalRange.Distant, FacingA = TacticalSector.Fore, FacingB = TacticalSector.Fore });
            var hidden = battle.Formations.Where(f => save.FleetSettings.Single(s => s.FleetId == f.FormationId).ApproachCloak)
                .SelectMany(f => f.Ships).Where(s => cloak.Single(c => c.FitId == Fit(s.ShipId).DefinitionId).Capable == true).Select(s => s.ShipId).ToList();
            return new CampaignTacticalBattleCheckpointDto { State = TacticalCombatV2State.Copy(battle), HiddenShipIds = hidden,
                BoundaryRevision = 0, Commitments = new List<CampaignTacticalCommitmentDto>(), Reports = new List<CampaignTacticalActorReportDto>(),
                ResolvedRounds = new List<CampaignTacticalResolvedRoundDto>() };
        }
        private bool ValidOrder(CampaignTacticalBattleCheckpointDto b, string actor, CampaignTacticalFleetOrderDto? o)
        {
            if (o == null || !Enum.IsDefined(typeof(CampaignFleetManeuver), o.Maneuver)) return false;
            var own = b.State.Formations.SingleOrDefault(f => f.FormationId == o.FleetId && f.SideId == actor && f.Status == TacticalFormationStatus.Active);
            return own != null && (o.FocusFleetId == null || o.FocusFleetId == own.StandingOrder.TargetFormationId
                || b.State.Formations.Any(f => f.FormationId == o.FocusFleetId && f.SideId != actor && f.Status == TacticalFormationStatus.Active
                    && f.Ships.Any(s => s.Hull > 0 && !b.HiddenShipIds.Contains(s.ShipId, StringComparer.Ordinal))));
        }
        private List<CampaignTacticalFleetOrderDto> Orders(CampaignTacticalBattleCheckpointDto b, string actor, IEnumerable<CampaignTacticalFleetOrderDto> replacements)
        {
            var result = b.State.Formations.Where(f => f.SideId == actor && f.Status == TacticalFormationStatus.Active).Select(f =>
                new CampaignTacticalFleetOrderDto { FleetId = f.FormationId, Maneuver = Maneuver(f.StandingOrder.Maneuver), FocusFleetId = f.StandingOrder.TargetFormationId }).ToList();
            foreach (var o in replacements) { var index = result.FindIndex(p => p.FleetId == o.FleetId); result[index] = Copy(o); }
            return result.OrderBy(o => o.FleetId, StringComparer.Ordinal).ToList();
        }
        private void EnsureAi(CampaignTacticalReviewSaveDto save, CampaignTacticalBattleCheckpointDto b)
        {
            foreach (var actor in b.State.Formations.Where(f => f.Status == TacticalFormationStatus.Active).Select(f => f.SideId).Distinct(StringComparer.Ordinal).OrderBy(s => s, StringComparer.Ordinal))
            {
                if (save.Campaign.GameState.Civilizations!.Single(c => c.CivilizationId == actor).ControllerKind != ControllerKind.AI
                    || b.Commitments.Any(c => c.ActorId == actor) || b.State.Completion != TacticalCompletion.None) continue;
                var visible = b.State.Formations.Where(f => f.SideId != actor && f.Status == TacticalFormationStatus.Active
                    && f.Ships.Any(s => s.Hull > 0 && !b.HiddenShipIds.Contains(s.ShipId, StringComparer.Ordinal))).OrderBy(f => f.FormationId, StringComparer.Ordinal).FirstOrDefault();
                var orders = b.State.Formations.Where(f => f.SideId == actor && f.Status == TacticalFormationStatus.Active)
                    .Select(f => new CampaignTacticalFleetOrderDto { FleetId = f.FormationId, Maneuver = CampaignFleetManeuver.Hold, FocusFleetId = visible?.FormationId }).ToList();
                Require(orders.All(o => ValidOrder(b, actor, o)), "Invalid baseline AI commitment.");
                b.Commitments.Add(new CampaignTacticalCommitmentDto { ActorId = actor,
                    CommandId = "ai-command.review." + actor + "." + b.State.EncounterId + "." + b.State.NextRound, Orders = orders });
            }
        }
        public CampaignTacticalMutationResult CommitRound(CommitCampaignTacticalRoundDto? command)
        {
            lock (gate)
            {
                if (command == null) return Result(CampaignTacticalError.InvalidOrder);
                command = new CommitCampaignTacticalRoundDto { ActorId = command.ActorId, CommandId = command.CommandId,
                    EncounterId = command.EncounterId, ExpectedTurn = command.ExpectedTurn, ExpectedRound = command.ExpectedRound,
                    ExpectedBoundaryRevision = command.ExpectedBoundaryRevision,
                    Replacements = command.Replacements?.Select(o => o == null ? null! : Copy(o)).ToList() };
                var actor = Actor(command.ActorId); if (actor != CampaignTacticalError.None) return Result(actor);
                var game = state.Campaign.GameState;
                if (CurrentPhase != SessionPhase.AwaitingCombat) return Result(CampaignTacticalError.WrongPhase);
                if (!EntityId.TryParse(command.CommandId, out _) || command.CommandId.StartsWith("ai-command.", StringComparison.Ordinal)) return Result(CampaignTacticalError.InvalidOrder);
                if (state.AcceptedRoundCommands.Contains(command.CommandId, StringComparer.Ordinal) || game.AcceptedCommandIds!.Contains(command.CommandId))
                    return Result(CampaignTacticalError.DuplicateCommand);
                var battle = state.Battles.SingleOrDefault(b => b.State.EncounterId == command.EncounterId);
                if (battle == null) return Result(CampaignTacticalError.UnknownEncounter);
                if (command.ExpectedTurn != game.TurnNumber) return Result(CampaignTacticalError.StaleTurn);
                if (command.ExpectedRound != battle.State.NextRound || command.ExpectedBoundaryRevision != battle.BoundaryRevision
                    || battle.State.Completion != TacticalCompletion.None) return Result(CampaignTacticalError.StaleBoundary);
                if (!battle.State.Formations.Any(f => f.SideId == command.ActorId && f.Status == TacticalFormationStatus.Active)) return Result(CampaignTacticalError.InvalidOrder);
                if (battle.Commitments.Any(c => c.ActorId == command.ActorId)) return Result(CampaignTacticalError.AlreadyCommitted);
                if (command.Replacements == null || command.Replacements.Any(o => !ValidOrder(battle, command.ActorId, o))
                    || command.Replacements.Select(o => o.FleetId).Distinct(StringComparer.Ordinal).Count() != command.Replacements.Count) return Result(CampaignTacticalError.InvalidOrder);
                if (game.StateRevision == long.MaxValue) return Result(CampaignTacticalError.Overflow);
                var next = Copy(state); var b2 = next.Battles.Single(b => b.State.EncounterId == command.EncounterId);
                b2.Commitments.Add(new CampaignTacticalCommitmentDto { ActorId = command.ActorId, CommandId = command.CommandId, Orders = Orders(b2, command.ActorId, command.Replacements) });
                next.AcceptedRoundCommands.Add(command.CommandId); next.Campaign.GameState.StateRevision++;
                EnsureAi(next, b2);
                if (b2.State.Formations.Where(f => f.Status == TacticalFormationStatus.Active).All(f => b2.Commitments.Any(c => c.ActorId == f.SideId))) ResolveJoint(next, b2);
                ValidateSave(next); state = Copy(next); return Result(CampaignTacticalError.None);
            }
        }
        private void ResolveJoint(CampaignTacticalReviewSaveDto save, CampaignTacticalBattleCheckpointDto b)
        {
            b.ResolvedRounds.Add(new CampaignTacticalResolvedRoundDto { Round = b.State.NextRound,
                Commitments = b.Commitments.OrderBy(c => c.ActorId,StringComparer.Ordinal).Select(Copy).ToList() });
            foreach (var commitment in b.Commitments)
            foreach (var o in commitment.Orders)
            {
                var standing = b.State.Formations.Single(f => f.FormationId == o.FleetId).StandingOrder;
                standing.Maneuver = Maneuver(o.Maneuver); standing.TargetFormationId = o.FocusFleetId;
            }
            var caps = b.State.Formations.SelectMany(f => f.Ships).ToDictionary(s => s.ShipId, s => Fit(s.ShipId).Capabilities!, StringComparer.Ordinal);
            var result = new DeterministicTacticalCombatV2Resolver().ResolveCampaignReviewRound(b.State, new CampaignRoundPolicy(b.HiddenShipIds, caps));
            AddReports(b, result.Events); b.State = result.NextState;
            if (b.State.NextRound >= 2) b.HiddenShipIds.Clear();
            b.Commitments.Clear(); b.BoundaryRevision = b.State.NextRound - 1;
            EnsureAi(save, b);
        }
        public CampaignTacticalMutationResult FinalizeBatch(int turn, long revision)
        {
            lock (gate)
            {
                var game = state.Campaign.GameState;
                if (CurrentPhase != SessionPhase.AwaitingCombat) return Result(CampaignTacticalError.WrongPhase);
                if (turn != game.TurnNumber) return Result(CampaignTacticalError.StaleTurn);
                if (revision != game.StateRevision) return Result(CampaignTacticalError.StaleRevision);
                if (state.Battles.Count == 0 || state.Battles.Any(b => b.State.Completion == TacticalCompletion.None)) return Result(CampaignTacticalError.IncompleteBatch);
                if (revision == long.MaxValue || turn == int.MaxValue) return Result(CampaignTacticalError.Overflow);
                var next = Copy(state); var staged = next.Campaign.GameState;
                CampaignTacticalTravelSaveDto? physicalNext = null;
                try
                {
                    int index = 0;
                    foreach (var b in next.Battles.OrderBy(b => b.State.EncounterId, StringComparer.Ordinal))
                    {
                        foreach (var ship in b.State.Formations.SelectMany(f => f.Ships))
                        {
                            var actual = staged.Ships!.Single(s => s.ShipId == ship.ShipId);
                            if (ship.Hull == 0)
                            {
                                staged.Ships!.Remove(actual); staged.Fleets!.Single(f => f.FleetId == actual.FleetId).ShipIds!.Remove(actual.ShipId);
                                staged.EntityIdentities.RemoveAll(e => e.EntityId == actual.ShipId); next.Pools.RemoveAll(p => p.ShipId == actual.ShipId);
                            }
                            else
                            {
                                actual.CurrentHull = ship.Hull;
                                var pool = next.Pools.Single(p => p.ShipId == actual.ShipId);
                                foreach (var q in pool.Shields) q.Current = ship.Shields.Single(s => s.Sector == q.Sector).Current;
                                foreach (var a in pool.Ammunition) a.Stock = ship.Weapons.Single(w => w.WeaponId == a.WeaponId).Ammunition;
                            }
                        }
                        FinalizationProbe?.Invoke(index++);
                    }
                    foreach (var fleet in staged.Fleets!.Where(f => f.ShipIds!.Count == 0).ToList())
                    { staged.Fleets!.Remove(fleet); staged.EntityIdentities.RemoveAll(e => e.EntityId == fleet.FleetId); next.FleetSettings.RemoveAll(f => f.FleetId == fleet.FleetId); }
                    staged.PendingCombats!.Clear(); staged.ResolvedCombats!.Clear(); staged.Phase = SessionPhase.Planning;
                    staged.TurnNumber++; staged.StateRevision++; staged.AcceptedCommandIds!.Clear(); next.Battles.Clear();
                    ValidateSave(next,travel!=null?Array.Empty<CombatRequestDto>():null);
                    if (travel != null)
                    {
                        physicalNext = TravelCopy(); physicalNext.Core = next;
                        physicalNext.Phase = SessionPhase.Planning; physicalNext.Requests.Clear();
                        physicalNext.Orders.RemoveAll(o => !staged.Fleets!.Any(f => f.FleetId == o.FleetId));
                        foreach (var o in physicalNext.Orders.Where(o => physicalNext.LastMovement.Any(r => r.FleetId == o.FleetId && r.StoppedByEncounter)))
                        { o.Intent = CampaignTravelIntent.Stay; o.ContactFleetId = null; o.ExpectedContactSeenTurn = null; o.RouteNodeIds.Clear(); }
                        ValidateTravelSave(physicalNext);
                    }
                }
                catch (Exception e) when (e is ArgumentException || e is InvalidOperationException || e is OverflowException) { return Result(CampaignTacticalError.InvalidBatch); }
                state = Copy(next);
                if (physicalNext != null) { travel = CampaignTacticalTravelData.Copy(physicalNext); state = travel.Core; }
                return Result(CampaignTacticalError.None);
            }
        }
    }
}
