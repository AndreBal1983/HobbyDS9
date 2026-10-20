using System;
using System.Collections.Generic;
using System.Linq;
using HobbyDS9.Contracts;
using static HobbyDS9.Simulation.CampaignTacticalReviewData;

namespace HobbyDS9.Simulation
{
    public sealed partial class CampaignTacticalReviewSession
    {
        private string? playableHash;
        private int playableOperationLimit;
        private string? playableOpponent;
        private string? playableDefenseNode;
        private readonly List<CampaignPlayableOperationDto> playableTranscript = new List<CampaignPlayableOperationDto>();
        private Dictionary<string, string> playableNames = new Dictionary<string, string>(StringComparer.Ordinal);
        private Dictionary<string, string> playableCreatedOwners = new Dictionary<string, string>(StringComparer.Ordinal);
        private readonly HashSet<string> playableRetiredFleets = new HashSet<string>(StringComparer.Ordinal);
        internal Action? PlayableOrganizationProbe { get; set; }
        private string OwnedCoreProfile => playableHash != null ? CampaignPlayableFleetIdentity.EmbeddedCoreProfile
            : frontier != null ? CampaignFrontierIdentity.EmbeddedCoreProfile : CampaignTacticalReviewIdentity.Profile;
        public int RemainingPlayableOperations { get { lock (gate) return playableOperationLimit - playableTranscript.Count; } }

        private static void ValidatePlayablePackage(CampaignPlayableFleetPackageDto p)
        {
            Require(p != null && p.FormatVersion == CampaignPlayableFleetIdentity.Format
                && p.ProfileId == CampaignPlayableFleetIdentity.Profile && Hash(p.PackageHash)
                && p.PolicyStatus == "proposed" && p.DefaultActivationAllowed == false
                && p.ReplayOperationLimit.HasValue && p.ReplayOperationLimit > 0 && p.ReplayOperationLimit <= 65536
                && p.Integrated != null, "Unsupported playable-fleet package or explicit technical replay budget.");
            ValidateIntegratedPackage(p.Integrated!);
            var game = p.Integrated!.Physical!.CorePackage!.Campaign!.InitialState;
            Require(game.Civilizations!.Count(c => c.ControllerKind == ControllerKind.Human) == 1
                && game.Civilizations.Any(c => c.CivilizationId == p.OpponentActorId && c.ControllerKind == ControllerKind.AI)
                && p.DefenseNodeId != null && game.NavigationKnowledge!.All(k => k.Nodes!.Any(n => n.Id == p.DefenseNodeId && n.Level != NavigationKnowledgeLevel.Unknown)),
                "Playable fixture requires one human, an authored AI opponent and an already-known defense node.");
        }
        public static CampaignTacticalReviewSession StartPlayable(CampaignPlayableFleetPackageDto p)
        {
            ValidatePlayablePackage(p);
            var i = p.Integrated!;
            return new CampaignTacticalReviewSession(i.Physical!.CorePackage!, null, i.Physical, null, i, null, null, null, p);
        }
        private static SubmitCampaignFleetOrganizationDto CopyOrganization(SubmitCampaignFleetOrganizationDto c)
            => new SubmitCampaignFleetOrganizationDto {
                ActorId = c.ActorId, CommandId = c.CommandId, ExpectedTurn = c.ExpectedTurn,
                ExpectedRevision = c.ExpectedRevision, Kind = c.Kind, TargetFleetId = c.TargetFleetId,
                DisplayName = c.DisplayName, ShipIds = c.ShipIds?.ToList()
            };
        private static CampaignPlayableOperationDto CopyPlayableOperation(CampaignPlayableOperationDto o)
            => new CampaignPlayableOperationDto {
                Common = o.Common == null ? null : CopyFrontierOperation(o.Common),
                Organization = o.Organization == null ? null : CopyOrganization(o.Organization)
            };
        public static CampaignTacticalReviewSession LoadPlayable(CampaignPlayableFleetPackageDto p, CampaignPlayableFleetSaveDto s)
        {
            ValidatePlayablePackage(p);
            Require(s != null && s.FormatVersion == CampaignPlayableFleetIdentity.Format && s.ProfileId == p.ProfileId
                && s.PackageHash == p.PackageHash && s.Integrated != null && s.Names != null && s.Transcript != null
                && s.Transcript.Count <= p.ReplayOperationLimit, "Unsupported/incomplete playable save; no migration.");
            var replay = StartPlayable(p);
            foreach (var operation in s.Transcript!)
            {
                Require(operation != null && (operation.Common == null) != (operation.Organization == null), "Closed playable operation required.");
                var expected = CopyPlayableOperation(operation);
                bool accepted;
                if (operation.Organization != null) accepted = replay.SubmitFleetOrganization(operation.Organization).Accepted;
                else
                {
                    var op = operation.Common!;
                    CampaignTacticalMutationResult? result = null;
                    switch (op.Kind)
                    {
                        case FrontierOperationKind.Strategic: result = replay.SubmitStrategicCommand(op.Strategic); break;
                        case FrontierOperationKind.Travel: result = replay.SubmitTravelOrder(op.Travel); break;
                        case FrontierOperationKind.Round: result = replay.CommitRound(op.Round); break;
                        case FrontierOperationKind.Cloak:
                            Require(op.ActorId != null && op.CommandId != null && op.FleetId != null && op.Enabled.HasValue
                                && op.Turn.HasValue && op.Revision.HasValue, "Incomplete cloak operation.");
                            result = replay.SetApproachCloak(op.ActorId!, op.CommandId!, op.FleetId!, op.Enabled!.Value, op.Turn!.Value, op.Revision!.Value); break;
                        case FrontierOperationKind.Advance:
                        case FrontierOperationKind.Finalize:
                            Require(op.Turn.HasValue && op.Revision.HasValue, "Incomplete boundary operation.");
                            result = op.Kind == FrontierOperationKind.Advance ? replay.AdvanceTurn(op.Turn!.Value, op.Revision!.Value)
                                : replay.FinalizeBatch(op.Turn!.Value, op.Revision!.Value); break;
                    }
                    accepted = result != null && result.Accepted;
                }
                Require(accepted && FrontierSame(expected, replay.playableTranscript.Last()), "Illegal/reordered/extra playable operation.");
            }
            Require(FrontierSame(replay.ExportPlayableSave(), s), "Playable checkpoint/names differ from accepted-operation replay.");
            return replay;
        }
        public CampaignPlayableFleetSaveDto ExportPlayableSave()
        {
            lock (gate)
            {
                Require(playableHash != null, "Not a playable-fleet session.");
                return new CampaignPlayableFleetSaveDto {
                    FormatVersion = CampaignPlayableFleetIdentity.Format, ProfileId = CampaignPlayableFleetIdentity.Profile,
                    PackageHash = playableHash!, Integrated = ExportIntegratedCore(),
                    Names = playableNames.OrderBy(p => p.Key, StringComparer.Ordinal)
                        .Select(p => new CampaignFleetNameDto { FleetId = p.Key, DisplayName = p.Value }).ToList(),
                    Transcript = playableTranscript.Select(CopyPlayableOperation).ToList()
                };
            }
        }
        private CampaignFleetOrganizationError FleetOrganizationBlocker(FleetStateDto fleet)
        {
            if (FrontierTranscriptFull) return CampaignFleetOrganizationError.Overflow;
            if (CurrentPhase != SessionPhase.Planning) return CampaignFleetOrganizationError.WrongPhase;
            if (fleet.TransitTotalCost != 0 || fleet.TransitProgress != 0) return CampaignFleetOrganizationError.FleetInTransit;
            var order = travel!.Orders.Single(o => o.FleetId == fleet.FleetId);
            return fleet.RouteNodeIds!.Count != 0 || order.Intent != CampaignTravelIntent.Stay
                ? CampaignFleetOrganizationError.FleetHasOrders : CampaignFleetOrganizationError.None;
        }
        public IReadOnlyList<CampaignNamedFleet> CaptureFleetOrganization(string actor)
        {
            lock (gate)
            {
                Require(playableHash != null && Actor(actor) == CampaignTacticalError.None, "Only the controlled actor can inspect playable fleets.");
                return state.Campaign.GameState.Fleets!.Where(f => f.OwnerCivilizationId == actor)
                    .OrderBy(f => f.FleetId, StringComparer.Ordinal).Select(f => new CampaignNamedFleet(new FleetSnapshot(f),
                        playableNames.TryGetValue(f.FleetId, out var name) ? name : f.FleetId, FleetOrganizationBlocker(f))).ToList().AsReadOnly();
            }
        }
        private static bool ValidFleetName(string? name) => name != null && name.Length > 0 && name.Length <= 80
            && name == name.Trim() && !name.Any(char.IsControl);
        public CampaignFleetOrganizationResult SubmitFleetOrganization(SubmitCampaignFleetOrganizationDto? command)
        {
            lock (gate)
            {
                var game = state.Campaign.GameState;
                CampaignFleetOrganizationResult Fail(CampaignFleetOrganizationError e) => new CampaignFleetOrganizationResult(e, game.StateRevision);
                if (playableHash == null) return Fail(CampaignFleetOrganizationError.UnsupportedProfile);
                if (FrontierTranscriptFull) return Fail(CampaignFleetOrganizationError.Overflow);
                if (command == null || command.ShipIds == null || !Enum.IsDefined(typeof(CampaignFleetOrganizationKind), command.Kind))
                    return Fail(CampaignFleetOrganizationError.InvalidOrder);
                var c = CopyOrganization(command);
                var actor = Actor(c.ActorId);
                if (actor != CampaignTacticalError.None) return Fail(actor == CampaignTacticalError.UnknownActor
                    ? CampaignFleetOrganizationError.UnknownActor : CampaignFleetOrganizationError.ActorNotControlled);
                if (CurrentPhase != SessionPhase.Planning) return Fail(CampaignFleetOrganizationError.WrongPhase);
                if (c.ExpectedTurn != game.TurnNumber) return Fail(CampaignFleetOrganizationError.StaleTurn);
                if (c.ExpectedRevision != game.StateRevision) return Fail(CampaignFleetOrganizationError.StaleRevision);
                if (!EntityId.TryParse(c.CommandId, out _) || c.CommandId.StartsWith("ai-command.", StringComparison.Ordinal))
                    return Fail(CampaignFleetOrganizationError.InvalidOrder);
                if (state.AcceptedRoundCommands.Contains(c.CommandId, StringComparer.Ordinal) || game.AcceptedCommandIds!.Contains(c.CommandId))
                    return Fail(CampaignFleetOrganizationError.DuplicateCommand);
                if (game.StateRevision == long.MaxValue) return Fail(CampaignFleetOrganizationError.Overflow);
                if ((c.Kind == CampaignFleetOrganizationKind.Transfer ? c.DisplayName != null : !ValidFleetName(c.DisplayName))
                    || (c.Kind == CampaignFleetOrganizationKind.Create ? c.TargetFleetId != null : c.TargetFleetId == null))
                    return Fail(CampaignFleetOrganizationError.InvalidName);
                if (c.ShipIds!.Count != c.ShipIds.Distinct(StringComparer.Ordinal).Count()
                    || (c.Kind == CampaignFleetOrganizationKind.Rename ? c.ShipIds.Count != 0 : c.ShipIds.Count == 0))
                    return Fail(CampaignFleetOrganizationError.InvalidOrder);
                c.ShipIds.Sort(StringComparer.Ordinal);
                var affected = new List<FleetStateDto>();
                FleetStateDto? target = null;
                if (c.TargetFleetId != null)
                {
                    target = game.Fleets!.SingleOrDefault(f => f.FleetId == c.TargetFleetId);
                    if (target == null) return Fail(CampaignFleetOrganizationError.UnknownFleet);
                    if (target.OwnerCivilizationId != c.ActorId) return Fail(CampaignFleetOrganizationError.FleetNotOwned);
                    affected.Add(target);
                }
                foreach (var id in c.ShipIds)
                {
                    var ship = game.Ships!.SingleOrDefault(s => s.ShipId == id);
                    if (ship == null) return Fail(CampaignFleetOrganizationError.UnknownShip);
                    if (ship.OwnerCivilizationId != c.ActorId) return Fail(CampaignFleetOrganizationError.ShipNotOwned);
                    if (ship.FleetId == c.TargetFleetId) return Fail(CampaignFleetOrganizationError.InvalidOrder);
                    affected.Add(game.Fleets!.Single(f => f.FleetId == ship.FleetId));
                }
                foreach (var f in affected)
                {
                    var blocker = FleetOrganizationBlocker(f);
                    if (blocker != CampaignFleetOrganizationError.None) return Fail(blocker);
                }
                if (affected.Select(f => f.LocationNodeId).Distinct(StringComparer.Ordinal).Count() != 1)
                    return Fail(CampaignFleetOrganizationError.NotColocated);
                if (c.Kind == CampaignFleetOrganizationKind.Rename && playableNames.TryGetValue(c.TargetFleetId!, out var oldName) && oldName == c.DisplayName)
                    return Fail(CampaignFleetOrganizationError.InvalidOrder);
                var next = TravelCopy();
                var staged = next.Core.Campaign.GameState;
                var names = new Dictionary<string, string>(playableNames, StringComparer.Ordinal);
                var owners = new Dictionary<string, string>(playableCreatedOwners, StringComparer.Ordinal);
                var oldOwners = playableCreatedOwners;
                var retired = new List<string>();
                string destination = c.TargetFleetId ?? "";
                try
                {
                    if (c.Kind == CampaignFleetOrganizationKind.Create)
                    {
                        destination = CampaignAllocation.Allocate(staged, "fleet", null);
                        owners.Add(destination, c.ActorId);
                        staged.Fleets!.Add(new FleetStateDto { FleetId = destination, OwnerCivilizationId = c.ActorId,
                            LocationNodeId = affected[0].LocationNodeId, ShipIds = new List<string>(), RouteNodeIds = new List<string>() });
                        next.Core.FleetSettings.Add(new CampaignTacticalFleetSettingDto { FleetId = destination });
                        next.Orders.Add(new CampaignTravelOrderDto { FleetId = destination, Intent = CampaignTravelIntent.Stay, RouteNodeIds = new List<string>() });
                    }
                    var destinationFleet = staged.Fleets!.Single(f => f.FleetId == destination);
                    foreach (var id in c.ShipIds.OrderBy(s => s, StringComparer.Ordinal))
                    {
                        var ship = staged.Ships!.Single(s => s.ShipId == id);
                        staged.Fleets.Single(f => f.FleetId == ship.FleetId).ShipIds!.Remove(id);
                        ship.FleetId = destination; destinationFleet.ShipIds!.Add(id);
                    }
                    destinationFleet.ShipIds!.Sort(StringComparer.Ordinal);
                    foreach (var empty in staged.Fleets.Where(f => f.ShipIds!.Count == 0).ToList())
                    {
                        retired.Add(empty.FleetId); staged.Fleets!.Remove(empty);
                        staged.EntityIdentities!.RemoveAll(i => i.EntityId == empty.FleetId);
                        next.Core.FleetSettings!.RemoveAll(f => f.FleetId == empty.FleetId);
                        next.Orders.RemoveAll(o => o.FleetId == empty.FleetId);
                    }
                    if (c.DisplayName != null) names[destination] = c.DisplayName;
                    staged.StateRevision++; next.Core.AcceptedRoundCommands.Add(c.CommandId);
                    playableCreatedOwners = owners;
                    foreach (var id in retired) playableRetiredFleets.Add(id);
                    ValidateTravelSave(next);
                    PlayableOrganizationProbe?.Invoke();
                }
                catch (Exception e) when (e is ArgumentException || e is InvalidOperationException || e is OverflowException)
                {
                    playableCreatedOwners = oldOwners;
                    foreach (var id in retired) playableRetiredFleets.Remove(id);
                    return Fail(e is OverflowException ? CampaignFleetOrganizationError.Overflow : CampaignFleetOrganizationError.InvalidOrder);
                }
                travel = CampaignTacticalTravelData.Copy(next); state = travel.Core; playableNames = names;
                playableTranscript.Add(new CampaignPlayableOperationDto { Organization = c });
                return new CampaignFleetOrganizationResult(CampaignFleetOrganizationError.None, state.Campaign.GameState.StateRevision, destination);
            }
        }
    }
}
