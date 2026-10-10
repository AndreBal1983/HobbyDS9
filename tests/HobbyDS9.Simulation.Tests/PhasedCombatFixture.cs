using System.Text.Json;
using HobbyDS9.Contracts;

namespace HobbyDS9.Simulation.Tests;

internal static class PhasedCombatFixture
{
    internal static T Clone<T>(T value) => JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(value))!;
    internal static string Json<T>(T value) => JsonSerializer.Serialize(value);
    internal static CombatRequestDto Request(int civilizations = 2, int fleetsPerCivilization = 1, int shipsPerFleet = 1, ulong seed = 47)
    {
        var r = new CombatRequestDto { EncounterId = "encounter.spawn.0000000000000001", LocationSystemId = "node.battle", TurnNumber = 1,
            RulesVersion = "fixture.phased-v1", ContentHash = new string('a', 64), ResolverId = "combat-resolver.phased", ResolverVersion = 1, RandomAlgorithmId = FoundationVersions.RandomAlgorithmId, RandomState = seed };
        for (var c = 0; c < civilizations; c++)
        for (var f = 0; f < fleetsPerCivilization; f++)
        {
            var suffix = c.ToString(System.Globalization.CultureInfo.InvariantCulture) + "-" + f.ToString(System.Globalization.CultureInfo.InvariantCulture);
            var p = new CombatParticipantDto { CivilizationId = "civ." + c.ToString(System.Globalization.CultureInfo.InvariantCulture), FleetId = "fleet." + suffix };
            for (var s = 0; s < shipsPerFleet; s++) p.Ships.Add(new() { ShipId = "ship." + suffix + "-" + s.ToString(System.Globalization.CultureInfo.InvariantCulture), DesignId = "design.test", Hull = 100, MaxHull = 100,
                CombatProfile = new() { AccuracyBps = 10000, LongRangeAttack = 10, MediumRangeAttack = 10, CloseRangeAttack = 10 } });
            r.Participants.Add(p);
        }
        return r;
    }
    internal static List<FleetCombatPlanDto> Plans(CombatRequestDto r, CombatTactic tactic = CombatTactic.Standard) => r.Participants.Select(p => new FleetCombatPlanDto
    { FleetId = p.FleetId, LongRangeTactic = tactic, MediumRangeTactic = tactic, CloseRangeTactic = tactic, TargetPriority = CombatTargetPriority.LowestShipId }).ToList();
    internal static CombatCheckpointDto Checkpoint(CombatRequestDto r) => new() { Phase = SessionPhase.AwaitingCombat, TurnNumber = r.TurnNumber, NextEntitySequence = 2, PendingCombats = new() { new() { Request = r } } };
    internal static CombatResultDto EmptyResult(CombatRequestDto r) => new() { EncounterId = r.EncounterId, ResolverId = r.ResolverId, ResolverVersion = r.ResolverVersion,
        Status = CombatResolutionStatus.Resolved, FinalRandomState = r.RandomState, Events = new() { new() { Kind = CombatEventKind.EncounterResolved } } };
}
