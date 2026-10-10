using System.Text.Json;
using HobbyDS9.Contracts;
using HobbyDS9.Simulation;
using Xunit;
using static HobbyDS9.Simulation.Tests.VerticalSliceFixture;

namespace HobbyDS9.Simulation.Tests;

public sealed class CampaignEncounterTests
{
    [Theory]
    [InlineData(0, false, 0)] [InlineData(1, false, 1)] [InlineData(1, true, 1)] [InlineData(3, true, 3)]
    public void ContestedLocationsCreateExactlyOneEncounterWithAllFleets(int battles, bool three, int expected)
    {
        var s = Session(CampaignCombatFixture.Scenario(battles, three)); var result = s.AdvanceTurn(1, 0);
        Assert.Equal(expected == 0 ? TurnStatus.Completed : TurnStatus.AwaitingCombat, result.Status);
        Assert.Equal(expected == 0 ? 2 : 1, result.TurnNumber); Assert.Equal(1, result.StateRevision); Assert.Null(expected == 0 ? null : result.CompletedTurnNumber);
        var requests = s.GetPendingCombatRequests(); Assert.Equal(expected, requests.Count); Assert.Empty(result.Events);
        Assert.All(requests, r => { Assert.Equal(three ? 3 : 2, r.Participants.Count); Assert.Equal(1, r.TurnNumber); Assert.Equal(CampaignCombatRules.ResolverId, r.ResolverId); Assert.Equal(CampaignCombatRules.ResolverVersion, r.ResolverVersion); });
        Assert.Equal((ulong)expected + 1, s.CaptureSnapshot().NextEntitySequence);
        var rng = new SplitMix64Random(47); Assert.Equal(Enumerable.Range(0, expected).Select(_ => rng.NextUInt64()).ToArray(), requests.Select(r => r.RandomState)); Assert.Equal(rng.State, s.CaptureSnapshot().RngState.State);
        Assert.DoesNotContain(s.ExportSave().GameState.EntityIdentities, e => e.EntityId.StartsWith("encounter.", StringComparison.Ordinal));
    }
    [Fact]
    public void SameCivilizationFleetsDoNotFightAndConsumeNoEncounterIdsOrDraws()
    {
        var scenario = CampaignCombatFixture.Scenario();
        foreach (var ship in scenario.InitialState.Ships!) ship.OwnerCivilizationId = "civ.federation";
        foreach (var fleet in scenario.InitialState.Fleets!) fleet.OwnerCivilizationId = "civ.federation";
        var s = Session(scenario); Assert.Equal(TurnStatus.Completed, s.AdvanceTurn(1, 0).Status); Assert.Empty(s.GetPendingCombatRequests());
        Assert.Equal(47UL, s.CaptureSnapshot().RngState.State); Assert.Equal(1UL, s.CaptureSnapshot().NextEntitySequence);
    }
    [Fact]
    public void CreationAndReloadAreCanonicalDetachedAndIndependentOfEntityInputOrder()
    {
        var scenario = CampaignCombatFixture.Scenario(3, true); var a = Session(scenario);
        scenario.InitialState.Fleets!.Reverse(); scenario.InitialState.Ships!.Reverse(); scenario.InitialState.EntityIdentities.Reverse(); scenario.InitialState.Civilizations!.Reverse();
        var b = Session(scenario); Assert.Equal(JsonSerializer.Serialize(a.AdvanceTurn(1, 0)), JsonSerializer.Serialize(b.AdvanceTurn(1, 0))); Assert.Equal(Json(a), Json(b));
        var requests = a.GetPendingCombatRequests();
        Assert.Equal(new[] { "encounter.spawn.0000000000000001", "encounter.spawn.0000000000000002", "encounter.spawn.0000000000000003" }, requests.Select(r => r.EncounterId));
        Assert.All(requests, r => Assert.Equal(r.Participants.Select(p => p.CivilizationId).OrderBy(id => id, StringComparer.Ordinal), r.Participants.Select(p => p.CivilizationId)));
        var before = Json(a); requests[0].Participants[0].Ships[0].Hull = 1; requests[0].RandomState = 0; requests[0].Participants.Clear();
        Assert.Throws<NotSupportedException>(() => ((IList<CombatRequestDto>)requests).Clear()); Assert.Equal(before, Json(a));
        var snapshot = a.CaptureSnapshot(); Assert.Throws<NotSupportedException>(() => ((IList<CombatEncounterSnapshot>)snapshot.Combats).Clear());
        Assert.Throws<NotSupportedException>(() => ((IList<CombatParticipantSnapshot>)snapshot.Combats[0].Participants).Clear());
        var loaded = new SimulationSession(JsonSerializer.Deserialize<SaveEnvelopeDto>(before)!, scenario.Definitions, Compatibility(scenario));
        Assert.Equal(before, Json(loaded)); Assert.Equal(JsonSerializer.Serialize(a.GetPendingCombatRequests()), JsonSerializer.Serialize(loaded.GetPendingCombatRequests()));
    }
    [Fact]
    public void CheckpointKeepsLedgerAndStrategicEffectsWithoutReexecutingOrAdvancing()
    {
        var s = Session(CampaignCombatFixture.Scenario()); Assert.True(Enqueue(s).Accepted); var before = s.CaptureSnapshot(); var result = s.AdvanceTurn(1, before.StateRevision);
        Assert.Equal(TurnStatus.AwaitingCombat, result.Status); Assert.Equal(1, result.TurnNumber); Assert.Equal(2, result.StateRevision);
        Assert.Equal(8, Colony(s).ProductionQueue![0].CompletedIndustry); Assert.Equal(1050, Colony(s).Population); Assert.Equal(new[] { new EntityId("cmd.enqueue") }, s.CaptureSnapshot().AcceptedCommandIds);
        var checkpoint = Json(s); Assert.Equal(TurnErrorCode.CombatPending, s.AdvanceTurn(1, result.StateRevision).ErrorCode); Assert.Equal(checkpoint, Json(s));
        Assert.Equal(CommandErrorCode.CombatPending, Enqueue(s, "cmd.blocked").ErrorCode); Assert.Equal(checkpoint, Json(s));
        Assert.Equal(TurnErrorCode.UnresolvedCombats, s.FinalizeCombat(1, result.StateRevision).ErrorCode); Assert.Equal(checkpoint, Json(s));
    }
    [Theory]
    [InlineData("allocation")] [InlineData("checkpoint-failure")] [InlineData("checkpoint-mutation")]
    public void EncounterFailureDiscardsEveryEarlierPhaseAndPartialSeedsAndIds(string fault)
    {
        var scenario = CampaignCombatFixture.Scenario(3); if (fault == "allocation") scenario.InitialState.NextEntitySequence = ulong.MaxValue - 1;
        var s = new SimulationSession(Save(scenario), scenario.Definitions, Compatibility(scenario), new Dictionary<CommandKind, ICommandHandler>(), combatCheckpointProbe: state =>
        {
            Assert.Equal(3, state.PendingCombats!.Count); Assert.Equal(1050, state.Colonies![0].Population);
            if (fault == "checkpoint-failure") return false;
            if (fault == "checkpoint-mutation") state.PendingCombats[0].Request.RandomState++;
            return true;
        });
        var before = Json(s);
        if (fault == "allocation") Assert.Throws<ArgumentException>(() => s.AdvanceTurn(1, 0));
        else if (fault == "checkpoint-failure") Assert.Equal(TurnErrorCode.PhaseFailed, s.AdvanceTurn(1, 0).ErrorCode);
        else Assert.Throws<InvalidOperationException>(() => s.AdvanceTurn(1, 0));
        Assert.Equal(before, Json(s));
    }
    [Theory]
    [InlineData("pending-null")] [InlineData("resolved-null")] [InlineData("empty-batch")] [InlineData("wrong-phase")]
    [InlineData("turn")] [InlineData("rules")] [InlineData("hash")] [InlineData("resolver")] [InlineData("rng")]
    [InlineData("duplicate")] [InlineData("location")] [InlineData("owner")] [InlineData("hull")] [InlineData("design")] [InlineData("omitted-fleet")]
    [InlineData("sequence-reuse")] [InlineData("old-save")]
    public void InvalidCampaignCheckpointAndFrozenReferencesRejectWithoutChangingInputs(string error)
    {
        var scenario = CampaignCombatFixture.Scenario(); var s = Session(scenario); s.AdvanceTurn(1, 0); var save = s.ExportSave(); var state = save.GameState; var request = state.PendingCombats![0].Request;
        switch (error)
        {
            case "pending-null": state.PendingCombats = null; break;
            case "resolved-null": state.ResolvedCombats = null; break;
            case "empty-batch": state.PendingCombats.Clear(); break;
            case "wrong-phase": state.Phase = SessionPhase.Planning; break;
            case "turn": request.TurnNumber++; break;
            case "rules": request.RulesVersion = "other"; break;
            case "hash": request.ContentHash = new string('c', 64); break;
            case "resolver": request.ResolverId = "resolver.other"; break;
            case "rng": request.RandomAlgorithmId = "other"; break;
            case "duplicate": state.PendingCombats.Add(state.PendingCombats[0]); break;
            case "location": request.LocationSystemId = "system.sol"; break;
            case "owner": request.Participants[0].CivilizationId = "civ.dominion"; break;
            case "hull": request.Participants[0].Ships[0].Hull--; break;
            case "design": request.Participants[0].Ships[0].DesignId = "ship-design.advanced-scout"; break;
            case "omitted-fleet": request.Participants.RemoveAt(0); break;
            case "sequence-reuse": state.EntityIdentities.Add(new() { EntityId = "token.spawn.0000000000000001" }); break;
            case "old-save": save.SaveFormatVersion = 7; break;
        }
        var before = JsonSerializer.Serialize(save); Assert.False(FoundationValidator.ValidateSave(save, scenario.Definitions, Compatibility(scenario)).IsValid);
        Assert.Throws<ArgumentException>(() => new SimulationSession(save, scenario.Definitions, Compatibility(scenario))); Assert.Equal(before, JsonSerializer.Serialize(save));
    }
    [Fact]
    public void FrozenHullSupportsTheEntireCampaignInt64Domain()
    {
        var scenario = CampaignCombatFixture.Scenario(); scenario.Definitions.ShipDesigns![0].MaxHull = long.MaxValue;
        foreach (var ship in scenario.InitialState.Ships!) ship.CurrentHull = long.MaxValue;
        var s = Session(scenario); s.AdvanceTurn(1, 0); Assert.All(s.GetPendingCombatRequests().Single().Participants.SelectMany(p => p.Ships), ship => Assert.Equal(long.MaxValue, ship.Hull));
    }
}
