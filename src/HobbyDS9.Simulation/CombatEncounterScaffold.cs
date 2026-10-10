using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using HobbyDS9.Contracts;

namespace HobbyDS9.Simulation
{
    // Narrow combat-boundary scaffold. This does not process strategic turns or
    // apply campaign damage/retreats. All operations return owned DTO copies.
    public static class CombatEncounterScaffold
    {
        public static CombatCheckpointDto CreateCheckpoint(int turnNumber, long revision,
            ulong campaignRandomState, ulong nextEntitySequence, IEnumerable<CombatRequestDto> templates)
        {
            Require(turnNumber > 0 && revision >= 0 && nextEntitySequence > 0, "Invalid checkpoint counters.");
            if (templates == null) throw new ArgumentNullException(nameof(templates));
            var requests = templates.ToList();
            foreach (var request in requests) ValidateRequest(request, false);
            requests = requests.Select(CopyRequest).OrderBy(r => r.LocationSystemId, StringComparer.Ordinal).ToList();
            var random = new SplitMix64Random(campaignRandomState);
            var allocator = new EntityIdAllocator(nextEntitySequence);
            var checkpoint = new CombatCheckpointDto
            {
                Phase = requests.Count == 0 ? SessionPhase.Planning : SessionPhase.AwaitingCombat,
                TurnNumber = turnNumber, StateRevision = revision, NextEntitySequence = nextEntitySequence
            };
            foreach (var request in requests)
            {
                Require(allocator.NextEntitySequence < ulong.MaxValue, "Entity allocation overflow.");
                request.EncounterId = allocator.Allocate("encounter").Value;
                request.TurnNumber = turnNumber;
                request.RandomAlgorithmId = EncounterRandomStream.AlgorithmId;
                request.RandomState = random.NextUInt64();
                checkpoint.PendingCombats.Add(new PendingCombatDto { Request = request });
            }
            checkpoint.CampaignRandomState = random.State;
            checkpoint.NextEntitySequence = allocator.NextEntitySequence;
            ValidateCheckpoint(checkpoint);
            return checkpoint;
        }

        public static void ValidateCheckpoint(CombatCheckpointDto checkpoint)
        {
            Require(checkpoint != null, "Missing checkpoint.");
            Require(checkpoint!.TurnNumber > 0 && checkpoint.StateRevision >= 0 && checkpoint.NextEntitySequence > 0,
                "Invalid checkpoint counters.");
            Require(checkpoint.PendingCombats != null && checkpoint.ResolvedCombats != null, "Missing encounter lists.");
            Require(Enum.IsDefined(typeof(SessionPhase), checkpoint.Phase), "Unknown session phase.");
            var requests = AllRequests(checkpoint).ToList();
            Require(checkpoint.Phase == SessionPhase.Planning ? requests.Count == 0 : requests.Count > 0,
                "Phase and encounter batch disagree.");
            var encounters = new HashSet<string>(StringComparer.Ordinal);
            var locations = new HashSet<string>(StringComparer.Ordinal);
            var fleets = new HashSet<string>(StringComparer.Ordinal);
            var ships = new HashSet<string>(StringComparer.Ordinal);
            foreach (var request in requests)
            {
                ValidateRequest(request, true);
                Require(request.TurnNumber == checkpoint.TurnNumber, "Encounter turn mismatch.");
                const string prefix = "encounter.spawn.";
                Require(request.EncounterId.StartsWith(prefix, StringComparison.Ordinal)
                    && request.EncounterId.Length == prefix.Length + 16, "Invalid allocated encounter ID.");
                Require(ulong.TryParse(request.EncounterId.Substring(prefix.Length), NumberStyles.HexNumber,
                    CultureInfo.InvariantCulture, out var sequence) && sequence > 0 && sequence < checkpoint.NextEntitySequence,
                    "Encounter ID exceeds allocation state.");
                Require(encounters.Add(request.EncounterId) && locations.Add(request.LocationSystemId), "Duplicate encounter/location.");
                Require(request.RulesVersion == requests[0].RulesVersion && request.ContentHash == requests[0].ContentHash,
                    "Inconsistent campaign rules/content identity.");
                foreach (var participant in request.Participants)
                {
                    Require(fleets.Add(participant.FleetId), "A fleet may belong to only one encounter per turn.");
                    foreach (var ship in participant.Ships)
                        Require(ships.Add(ship.ShipId), "A ship may belong to only one encounter per turn.");
                }
            }
            foreach (var resolved in checkpoint.ResolvedCombats)
            {
                ValidateResult(checkpoint, resolved.Request, resolved.Result);
                Require(resolved.Result.Status == CombatResolutionStatus.Resolved, "Buffered result must be resolved.");
            }
        }

        public static CombatRequestDto GetDetachedRequest(CombatCheckpointDto checkpoint, string encounterId)
        {
            ValidateCheckpoint(checkpoint);
            ValidateId(encounterId);
            var pending = checkpoint.PendingCombats.SingleOrDefault(p => p.Request.EncounterId == encounterId);
            Require(pending != null, "Encounter is not pending.");
            return CopyRequest(pending!.Request);
        }

        public static CombatCheckpointDto RecordResult(CombatCheckpointDto checkpoint, CombatResultDto result)
        {
            ValidateCheckpoint(checkpoint);
            Require(checkpoint.Phase == SessionPhase.AwaitingCombat && result != null, "No combat result expected.");
            var pending = checkpoint.PendingCombats.SingleOrDefault(p => p.Request.EncounterId == result!.EncounterId);
            Require(pending != null, "Unknown or already resolved encounter.");
            ValidateResult(checkpoint, pending!.Request, result!);
            var copy = CopyCheckpoint(checkpoint);
            if (result!.Status == CombatResolutionStatus.Deferred) return copy;
            Require(copy.StateRevision < long.MaxValue, "Revision overflow.");
            var entry = copy.PendingCombats.Single(p => p.Request.EncounterId == result.EncounterId);
            copy.PendingCombats.Remove(entry);
            copy.ResolvedCombats.Add(new ResolvedCombatDto { Request = entry.Request, Result = CopyResult(result) });
            copy.ResolvedCombats = copy.ResolvedCombats.OrderBy(r => r.Request.LocationSystemId, StringComparer.Ordinal).ToList();
            copy.StateRevision++;
            // Remain AwaitingCombat even when ready: only the campaign
            // transaction may apply all consequences and open the next turn.
            ValidateCheckpoint(copy);
            return copy;
        }

        public static bool IsReadyToFinalize(CombatCheckpointDto checkpoint)
        {
            ValidateCheckpoint(checkpoint);
            return checkpoint.Phase == SessionPhase.AwaitingCombat && checkpoint.PendingCombats.Count == 0;
        }

        public static IReadOnlyList<CombatResultDto> GetCanonicalResults(CombatCheckpointDto checkpoint)
        {
            Require(IsReadyToFinalize(checkpoint), "Unresolved encounters block finalization.");
            return checkpoint.ResolvedCombats.OrderBy(r => r.Request.LocationSystemId, StringComparer.Ordinal)
                .Select(r => CopyResult(r.Result)).ToList().AsReadOnly();
        }

        public static void RequirePlanning(CombatCheckpointDto checkpoint)
        {
            ValidateCheckpoint(checkpoint);
            if (checkpoint.Phase != SessionPhase.Planning) throw new InvalidOperationException("CombatPending");
        }

        internal static void ValidateRequest(CombatRequestDto request, bool allocated)
        {
            Require(request != null, "Missing combat request.");
            ValidateId(request!.LocationSystemId);
            ValidateId(request.ResolverId);
            Require(!string.IsNullOrWhiteSpace(request.RulesVersion) && !string.IsNullOrWhiteSpace(request.ContentHash)
                && request.ResolverVersion > 0, "Missing resolver/rules/content identity.");
            if (allocated)
            {
                ValidateId(request.EncounterId);
                Require(request.RandomAlgorithmId == EncounterRandomStream.AlgorithmId, "Unknown encounter RNG algorithm.");
            }
            Require(request.Participants != null && request.Participants.Count >= 2, "Encounter requires opposing participants.");
            var civilizations = new HashSet<string>(StringComparer.Ordinal);
            var fleets = new HashSet<string>(StringComparer.Ordinal);
            var ships = new HashSet<string>(StringComparer.Ordinal);
            foreach (var participant in request.Participants!)
            {
                Require(participant != null, "Missing participant.");
                ValidateId(participant!.CivilizationId);
                ValidateId(participant.FleetId);
                civilizations.Add(participant.CivilizationId);
                Require(fleets.Add(participant.FleetId), "Duplicate participant fleet.");
                Require(participant.Ships != null && participant.Ships.Count > 0, "Empty combat fleet.");
                foreach (var ship in participant.Ships!)
                {
                    Require(ship != null, "Missing ship.");
                    ValidateId(ship!.ShipId);
                    Require(ships.Add(ship.ShipId), "Duplicate participant ship.");
                    Require(DefinitionId.TryParse(ship.DesignId, out _), "Invalid canonical definition ID.");
                    Require(ship.Hull > 0, "Nonpositive combat hull.");
                }
            }
            Require(civilizations.Count >= 2, "Encounter requires opposing civilizations.");
        }

        private static void ValidateResult(CombatCheckpointDto checkpoint, CombatRequestDto request, CombatResultDto result)
        {
            var error = GetResultError(checkpoint, request, result);
            Require(error == CombatSubmissionError.None, "Invalid combat result: " + error);
        }

        internal static CombatSubmissionError GetResultError(CombatCheckpointDto checkpoint, CombatRequestDto request, CombatResultDto? result)
        {
            if (result == null || result.EncounterId != request.EncounterId || result.ResolverId != request.ResolverId || result.ResolverVersion != request.ResolverVersion) return CombatSubmissionError.ResolverMismatch;
            if (!Enum.IsDefined(typeof(CombatResolutionStatus), result.Status) || result.ShipChanges == null || result.FleetChanges == null || result.Events == null) return CombatSubmissionError.MalformedResult;
            if (result.Status == CombatResolutionStatus.Deferred)
                return result.ShipChanges.Count == 0 && result.FleetChanges.Count == 0 && result.Events.Count == 0 && result.FinalRandomState == request.RandomState ? CombatSubmissionError.None : CombatSubmissionError.MalformedResult;
            var ships = request.Participants.SelectMany(p => p.Ships).ToDictionary(s => s.ShipId, StringComparer.Ordinal);
            var fleets = new HashSet<string>(request.Participants.Select(p => p.FleetId), StringComparer.Ordinal);
            var changedShips = new HashSet<string>(StringComparer.Ordinal);
            foreach (var change in result.ShipChanges)
            {
                if (change == null || !EntityId.TryParse(change.ShipId, out _) || !changedShips.Add(change.ShipId)) return CombatSubmissionError.MalformedResult;
                if (!ships.TryGetValue(change.ShipId, out var ship)) return CombatSubmissionError.Nonparticipant;
                if (change.RemainingHull < 0 || change.RemainingHull > ship.Hull) return CombatSubmissionError.InvalidHull;
            }
            var changedFleets = new HashSet<string>(StringComparer.Ordinal);
            var battleLocations = new HashSet<string>(AllRequests(checkpoint).Select(r => r.LocationSystemId), StringComparer.Ordinal);
            foreach (var change in result.FleetChanges)
            {
                if (change == null || !EntityId.TryParse(change.FleetId, out _) || !changedFleets.Add(change.FleetId) || !Enum.IsDefined(typeof(FleetCombatDisposition), change.Disposition)) return CombatSubmissionError.MalformedResult;
                if (!fleets.Contains(change.FleetId)) return CombatSubmissionError.Nonparticipant;
                if (change.Disposition == FleetCombatDisposition.Retreated)
                {
                    if (!EntityId.TryParse(change.RetreatSystemId, out _) || battleLocations.Contains(change.RetreatSystemId!)) return CombatSubmissionError.InvalidRetreat;
                }
                else if (change.RetreatSystemId != null) return CombatSubmissionError.InvalidRetreat;
            }
            for (var i = 0; i < result.Events.Count; i++)
            {
                var e = result.Events[i];
                if (e == null || e.Sequence != i || !Enum.IsDefined(typeof(CombatEventKind), e.Kind)) return CombatSubmissionError.InvalidEvents;
                if ((e.ShipId != null && !ships.ContainsKey(e.ShipId)) || (e.FleetId != null && !fleets.Contains(e.FleetId))) return CombatSubmissionError.Nonparticipant;
            }
            if (result.Events.Count == 0 || result.Events[result.Events.Count - 1].Kind != CombatEventKind.EncounterResolved || result.Events.Count(e => e.Kind == CombatEventKind.EncounterResolved) != 1) return CombatSubmissionError.InvalidEvents;
            return CombatSubmissionError.None;
        }

        private static void ValidateId(string id)
        {
            Require(EntityId.TryParse(id, out _), "Invalid canonical ID.");
        }

        private static IEnumerable<CombatRequestDto> AllRequests(CombatCheckpointDto checkpoint)
        {
            foreach (var pending in checkpoint.PendingCombats)
            {
                Require(pending != null, "Missing pending entry.");
                yield return pending!.Request;
            }
            foreach (var resolved in checkpoint.ResolvedCombats)
            {
                Require(resolved != null, "Missing resolved entry.");
                yield return resolved!.Request;
            }
        }

        internal static CombatCheckpointDto CopyCheckpoint(CombatCheckpointDto checkpoint) => new CombatCheckpointDto
        {
            Phase = checkpoint.Phase, TurnNumber = checkpoint.TurnNumber, StateRevision = checkpoint.StateRevision,
            CampaignRandomState = checkpoint.CampaignRandomState, NextEntitySequence = checkpoint.NextEntitySequence,
            PendingCombats = checkpoint.PendingCombats.Select(p => new PendingCombatDto { Request = CopyRequest(p.Request) }).ToList(),
            ResolvedCombats = checkpoint.ResolvedCombats.Select(r => new ResolvedCombatDto
                { Request = CopyRequest(r.Request), Result = CopyResult(r.Result) }).ToList()
        };

        internal static CombatRequestDto CopyRequest(CombatRequestDto request) => new CombatRequestDto
        {
            EncounterId = request.EncounterId, TurnNumber = request.TurnNumber, LocationSystemId = request.LocationSystemId,
            RulesVersion = request.RulesVersion, ContentHash = request.ContentHash, ResolverId = request.ResolverId,
            ResolverVersion = request.ResolverVersion, RandomAlgorithmId = request.RandomAlgorithmId, RandomState = request.RandomState,
            Participants = request.Participants.OrderBy(p => p.CivilizationId, StringComparer.Ordinal).ThenBy(p => p.FleetId, StringComparer.Ordinal)
                .Select(p => new CombatParticipantDto { CivilizationId = p.CivilizationId, FleetId = p.FleetId,
                    Ships = p.Ships.OrderBy(s => s.ShipId, StringComparer.Ordinal).Select(s => new CombatShipDto
                        { ShipId = s.ShipId, DesignId = s.DesignId, Hull = s.Hull }).ToList() }).ToList()
        };

        internal static CombatResultDto CopyResult(CombatResultDto result) => new CombatResultDto
        {
            EncounterId = result.EncounterId, ResolverId = result.ResolverId, ResolverVersion = result.ResolverVersion,
            Status = result.Status, FinalRandomState = result.FinalRandomState,
            ShipChanges = result.ShipChanges.OrderBy(c => c.ShipId, StringComparer.Ordinal).Select(c => new ShipCombatChangeDto
                { ShipId = c.ShipId, RemainingHull = c.RemainingHull }).ToList(),
            FleetChanges = result.FleetChanges.OrderBy(c => c.FleetId, StringComparer.Ordinal).Select(c => new FleetCombatChangeDto
                { FleetId = c.FleetId, Disposition = c.Disposition, RetreatSystemId = c.RetreatSystemId }).ToList(),
            Events = result.Events.Select(e => new CombatEventDto
                { Sequence = e.Sequence, Kind = e.Kind, FleetId = e.FleetId, ShipId = e.ShipId }).ToList()
        };

        private static void Require([DoesNotReturnIf(false)] bool condition, string message)
        {
            if (!condition) throw new ArgumentException(message);
        }
    }
}
