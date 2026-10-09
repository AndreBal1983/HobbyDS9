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
            var random = new EncounterRandomStream(campaignRandomState);
            var checkpoint = new CombatCheckpointDto
            {
                Phase = requests.Count == 0 ? SessionPhase.Planning : SessionPhase.AwaitingCombat,
                TurnNumber = turnNumber, StateRevision = revision, NextEntitySequence = nextEntitySequence
            };
            foreach (var request in requests)
            {
                Require(checkpoint.NextEntitySequence < ulong.MaxValue, "Entity allocation overflow.");
                request.EncounterId = "encounter.spawn." + checkpoint.NextEntitySequence.ToString("x16", CultureInfo.InvariantCulture);
                checkpoint.NextEntitySequence++;
                request.TurnNumber = turnNumber;
                request.RandomAlgorithmId = EncounterRandomStream.AlgorithmId;
                request.RandomState = random.NextUInt64();
                checkpoint.PendingCombats.Add(new PendingCombatDto { Request = request });
            }
            checkpoint.CampaignRandomState = random.State;
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
            // Remain AwaitingCombat even when ready: only the future campaign
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

        private static void ValidateRequest(CombatRequestDto request, bool allocated)
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
            foreach (var participant in request.Participants!)
            {
                Require(participant != null, "Missing participant.");
                ValidateId(participant!.CivilizationId);
                ValidateId(participant.FleetId);
                civilizations.Add(participant.CivilizationId);
                Require(participant.Ships != null && participant.Ships.Count > 0, "Empty combat fleet.");
                foreach (var ship in participant.Ships!)
                {
                    Require(ship != null, "Missing ship.");
                    ValidateId(ship!.ShipId);
                    ValidateId(ship.DesignId);
                    Require(ship.Hull > 0, "Nonpositive combat hull.");
                }
            }
            Require(civilizations.Count >= 2, "Encounter requires opposing civilizations.");
        }

        private static void ValidateResult(CombatCheckpointDto checkpoint, CombatRequestDto request, CombatResultDto result)
        {
            Require(result != null && result.EncounterId == request.EncounterId && result.ResolverId == request.ResolverId
                && result.ResolverVersion == request.ResolverVersion, "Result identity mismatch.");
            Require(Enum.IsDefined(typeof(CombatResolutionStatus), result!.Status), "Unknown resolution status.");
            Require(result.ShipChanges != null && result.FleetChanges != null && result.Events != null, "Missing result lists.");
            if (result.Status == CombatResolutionStatus.Deferred)
            {
                Require(result.ShipChanges.Count == 0 && result.FleetChanges.Count == 0 && result.Events.Count == 0
                    && result.FinalRandomState == request.RandomState, "Deferred result must not have consequences.");
                return;
            }
            var ships = request.Participants.SelectMany(p => p.Ships).ToDictionary(s => s.ShipId, StringComparer.Ordinal);
            var fleets = new HashSet<string>(request.Participants.Select(p => p.FleetId), StringComparer.Ordinal);
            var changedShips = new HashSet<string>(StringComparer.Ordinal);
            foreach (var change in result.ShipChanges)
                Require(change != null && ships.TryGetValue(change.ShipId, out var ship) && changedShips.Add(change.ShipId)
                    && change.RemainingHull >= 0 && change.RemainingHull <= ship.Hull, "Invalid ship change.");
            var changedFleets = new HashSet<string>(StringComparer.Ordinal);
            var battleLocations = new HashSet<string>(AllRequests(checkpoint).Select(r => r.LocationSystemId), StringComparer.Ordinal);
            foreach (var change in result.FleetChanges)
            {
                Require(change != null && fleets.Contains(change.FleetId) && changedFleets.Add(change.FleetId)
                    && Enum.IsDefined(typeof(FleetCombatDisposition), change.Disposition), "Invalid fleet change.");
                if (change!.Disposition == FleetCombatDisposition.Retreated)
                {
                    ValidateId(change.RetreatSystemId!);
                    Require(!battleLocations.Contains(change.RetreatSystemId!), "Retreat cannot enter any encounter location this turn.");
                }
                else Require(change.RetreatSystemId == null, "Non-retreat result has retreat destination.");
            }
            for (var i = 0; i < result.Events.Count; i++)
            {
                var combatEvent = result.Events[i];
                Require(combatEvent != null && combatEvent.Sequence == i && Enum.IsDefined(typeof(CombatEventKind), combatEvent.Kind),
                    "Invalid combat event sequence/kind.");
                Require((combatEvent!.ShipId == null || ships.ContainsKey(combatEvent.ShipId))
                    && (combatEvent.FleetId == null || fleets.Contains(combatEvent.FleetId)), "Event references nonparticipant.");
            }
            Require(result.Events.Count > 0 && result.Events[result.Events.Count - 1].Kind == CombatEventKind.EncounterResolved
                && result.Events.Count(e => e.Kind == CombatEventKind.EncounterResolved) == 1, "Missing/duplicate terminal event.");
        }

        private static void ValidateId(string id)
        {
            Require(id != null && id.Length > 0 && id.Length <= 128 && id[0] >= 'a' && id[0] <= 'z', "Invalid canonical ID.");
            foreach (var c in id!)
                Require((c >= 'a' && c <= 'z') || (c >= '0' && c <= '9') || c == '.' || c == '_' || c == '-', "Invalid canonical ID.");
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

        private static CombatCheckpointDto CopyCheckpoint(CombatCheckpointDto checkpoint) => new CombatCheckpointDto
        {
            Phase = checkpoint.Phase, TurnNumber = checkpoint.TurnNumber, StateRevision = checkpoint.StateRevision,
            CampaignRandomState = checkpoint.CampaignRandomState, NextEntitySequence = checkpoint.NextEntitySequence,
            PendingCombats = checkpoint.PendingCombats.Select(p => new PendingCombatDto { Request = CopyRequest(p.Request) }).ToList(),
            ResolvedCombats = checkpoint.ResolvedCombats.Select(r => new ResolvedCombatDto
                { Request = CopyRequest(r.Request), Result = CopyResult(r.Result) }).ToList()
        };

        private static CombatRequestDto CopyRequest(CombatRequestDto request) => new CombatRequestDto
        {
            EncounterId = request.EncounterId, TurnNumber = request.TurnNumber, LocationSystemId = request.LocationSystemId,
            RulesVersion = request.RulesVersion, ContentHash = request.ContentHash, ResolverId = request.ResolverId,
            ResolverVersion = request.ResolverVersion, RandomAlgorithmId = request.RandomAlgorithmId, RandomState = request.RandomState,
            Participants = request.Participants.OrderBy(p => p.CivilizationId, StringComparer.Ordinal).ThenBy(p => p.FleetId, StringComparer.Ordinal)
                .Select(p => new CombatParticipantDto { CivilizationId = p.CivilizationId, FleetId = p.FleetId,
                    Ships = p.Ships.OrderBy(s => s.ShipId, StringComparer.Ordinal).Select(s => new CombatShipDto
                        { ShipId = s.ShipId, DesignId = s.DesignId, Hull = s.Hull }).ToList() }).ToList()
        };

        private static CombatResultDto CopyResult(CombatResultDto result) => new CombatResultDto
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
