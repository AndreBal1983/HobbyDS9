using HobbyDS9.Contracts;
using HobbyDS9.Simulation;
using Xunit;
using static HobbyDS9.Simulation.Tests.PhasedCombatFixture;

namespace HobbyDS9.Simulation.Tests;

public sealed class CombatProfileContractTests
{
    [Theory]
    [InlineData("missing")]
    [InlineData("shield")]
    [InlineData("long")]
    [InlineData("medium")]
    [InlineData("close")]
    [InlineData("accuracy-low")]
    [InlineData("accuracy-high")]
    [InlineData("evasion-low")]
    [InlineData("evasion-high")]
    public void InvalidContentProfileIsRejectedWithoutChangingInput(string field)
    {
        var scenario = VerticalSliceFixture.Scenario(); var d = scenario.Definitions.ShipDesigns![0]; var p = d.CombatProfile!;
        switch (field)
        {
            case "missing": d.CombatProfile = null; break;
            case "shield": p.ShieldCapacity = -1; break;
            case "long": p.LongRangeAttack = -1; break;
            case "medium": p.MediumRangeAttack = -1; break;
            case "close": p.CloseRangeAttack = -1; break;
            case "accuracy-low": p.AccuracyBps = -1; break;
            case "accuracy-high": p.AccuracyBps = 10001; break;
            case "evasion-low": p.EvasionBps = -1; break;
            case "evasion-high": p.EvasionBps = 10001; break;
        }
        var before = Json(scenario); Assert.Contains(FoundationValidator.ValidateScenario(scenario).Issues, i => i.Code == ValidationCode.InvalidCombatProfile); Assert.Equal(before, Json(scenario));
    }
    [Theory]
    [InlineData("max")]
    [InlineData("shield")]
    [InlineData("long")]
    [InlineData("medium")]
    [InlineData("close")]
    [InlineData("accuracy")]
    [InlineData("evasion")]
    [InlineData("missing")]
    [InlineData("hull-over-max")]
    public void ReloadChecksEveryFrozenStatAgainstAuthoritativeDesign(string field)
    {
        var scenario = CampaignCombatFixture.Scenario(); var s = VerticalSliceFixture.Session(scenario); s.AdvanceTurn(1, 0); var save = s.ExportSave(); var ship = save.GameState.PendingCombats![0].Request.Participants[0].Ships[0];
        switch (field)
        {
            case "max": ship.MaxHull++; break;
            case "shield": ship.CombatProfile!.ShieldCapacity++; break;
            case "long": ship.CombatProfile!.LongRangeAttack++; break;
            case "medium": ship.CombatProfile!.MediumRangeAttack++; break;
            case "close": ship.CombatProfile!.CloseRangeAttack++; break;
            case "accuracy": ship.CombatProfile!.AccuracyBps++; break;
            case "evasion": ship.CombatProfile!.EvasionBps++; break;
            case "missing": ship.CombatProfile = null; break;
            case "hull-over-max": ship.Hull = ship.MaxHull + 1; break;
        }
        var before = Json(save); Assert.Throws<ArgumentException>(() => new SimulationSession(save, scenario.Definitions, VerticalSliceFixture.Compatibility(scenario))); Assert.Equal(before, Json(save));
    }
    [Fact]
    public void FrozenProfilesAreDetachedPersistedAndProtectAllSigned64BitValues()
    {
        var scenario = CampaignCombatFixture.Scenario(); var p = scenario.Definitions.ShipDesigns![0].CombatProfile!;
        p.ShieldCapacity = p.LongRangeAttack = p.MediumRangeAttack = p.CloseRangeAttack = long.MaxValue; p.AccuracyBps = 0; p.EvasionBps = 10000;
        var s = VerticalSliceFixture.Session(scenario); s.AdvanceTurn(1, 0); var before = VerticalSliceFixture.Json(s); var definitions = Clone(scenario.Definitions); var frozen = s.GetPendingCombatRequests()[0].Participants[0].Ships[0];
        Assert.Equal(Json(p), Json(frozen.CombatProfile)); Assert.Equal(10, frozen.MaxHull); frozen.CombatProfile!.ShieldCapacity = 0; p.LongRangeAttack = 0;
        Assert.Equal(before, VerticalSliceFixture.Json(s)); Assert.Equal(long.MaxValue, s.CaptureSnapshot().Combats[0].Participants[0].Ships[0].CombatProfile.ShieldCapacity);
        var saved = Clone(s.ExportSave()); var reloaded = new SimulationSession(saved, definitions, VerticalSliceFixture.Compatibility(scenario)); Assert.Equal(before, VerticalSliceFixture.Json(reloaded));
    }
    [Theory]
    [InlineData("missing")]
    [InlineData("duplicate")]
    [InlineData("unknown")]
    [InlineData("long")]
    [InlineData("medium")]
    [InlineData("close")]
    [InlineData("priority")]
    [InlineData("low")]
    [InlineData("high")]
    [InlineData("destination-missing")]
    [InlineData("destination-disabled")]
    [InlineData("null")]
    public void InvalidPlansAreRejectedWithoutMutation(string fault)
    {
        var r = Request(); var plans = Plans(r); var p = plans[0];
        switch (fault)
        {
            case "missing": plans.RemoveAt(0); break;
            case "duplicate": plans[1] = p; break;
            case "unknown": p.FleetId = "fleet.unknown"; break;
            case "long": p.LongRangeTactic = (CombatTactic)99; break;
            case "medium": p.MediumRangeTactic = 0; break;
            case "close": p.CloseRangeTactic = 0; break;
            case "priority": p.TargetPriority = 0; break;
            case "low": p.RetreatHullPercent = -1; break;
            case "high": p.RetreatHullPercent = 101; break;
            case "destination-missing": p.RetreatHullPercent = 10; break;
            case "destination-disabled": p.RetreatNodeId = "node.retreat"; break;
            case "null": plans[0] = null!; break;
        }
        var before = Json(plans); Assert.Throws<ArgumentException>(() => CombatPlanValidation.Validate(r, plans)); Assert.Equal(before, Json(plans));
    }
    [Fact]
    public void PlanOrderAndValidThresholdBoundariesAreAccepted()
    {
        var r = Request(); var plans = Plans(r); plans[1].RetreatHullPercent = 100; plans[1].RetreatNodeId = "node.exit"; CombatPlanValidation.Validate(r, plans); plans.Reverse(); CombatPlanValidation.Validate(r, plans);
        Assert.Equal(0, (int)CombatEventKind.EncounterResolved); Assert.Equal(1, (int)CombatEventKind.ShipDamaged); Assert.Equal(2, (int)CombatEventKind.ShipDestroyed); Assert.Equal(3, (int)CombatEventKind.FleetRetreated);
        Assert.Equal(12, FoundationVersions.SaveFormatVersion); Assert.Equal(8, FoundationVersions.ContentFormatVersion);
    }
    [Theory]
    [InlineData("phase")]
    [InlineData("source")]
    [InlineData("target")]
    [InlineData("amount")]
    [InlineData("fake-phase-ship")]
    [InlineData("fleet-mismatch")]
    public void RichEventStructureIsSharedAndRejectsInvalidReferences(string fault)
    {
        var r = Request(); var result = EmptyResult(r); var e = new CombatEventDto { Kind = CombatEventKind.AttackHit, Phase = CombatPhaseKind.LongRange, SourceShipId = r.Participants[0].Ships[0].ShipId, ShipId = r.Participants[1].Ships[0].ShipId };
        result.Events.Insert(0, e); result.Events[1].Sequence = 1;
        switch (fault)
        {
            case "phase": e.Phase = (CombatPhaseKind)99; break;
            case "source": e.SourceShipId = "ship.missing"; break;
            case "target": e.ShipId = null; break;
            case "amount": e.Amount = -1; break;
            case "fake-phase-ship": e.Kind = CombatEventKind.PhaseStarted; break;
            case "fleet-mismatch": e.FleetId = r.Participants[0].FleetId; break;
        }
        Assert.NotEqual(CombatSubmissionError.None, CombatEncounterScaffold.GetResultError(Checkpoint(r), r, result));
    }
    [Fact]
    public void RichEventsRoundTripAndDetachedBuffersPreserveEveryField()
    {
        var r = Request(); var result = EmptyResult(r); var source = r.Participants[0]; var target = r.Participants[1]; var ship = target.Ships[0].ShipId;
        result.ShipChanges.Add(new() { ShipId = ship, RemainingHull = 98 });
        result.Events.InsertRange(0, new CombatEventDto[] {
            new() { Kind = CombatEventKind.PhaseStarted, Phase = CombatPhaseKind.LongRange },
            new() { Kind = CombatEventKind.AttackHit, Phase = CombatPhaseKind.LongRange, ShipId = ship, FleetId = target.FleetId, SourceShipId = source.Ships[0].ShipId, SourceFleetId = source.FleetId },
            new() { Kind = CombatEventKind.ShieldDamaged, Phase = CombatPhaseKind.LongRange, ShipId = ship, FleetId = target.FleetId, Amount = 3 },
            new() { Kind = CombatEventKind.ShipDamaged, Phase = CombatPhaseKind.LongRange, ShipId = ship, FleetId = target.FleetId, Amount = 2 } });
        for (var n = 0; n < result.Events.Count; n++) result.Events[n].Sequence = n;
        var checkpoint = CombatEncounterScaffold.RecordResult(Checkpoint(r), result); Assert.Equal(Json(result), Json(checkpoint.ResolvedCombats[0].Result));
        var loaded = Clone(checkpoint); CombatEncounterScaffold.ValidateCheckpoint(loaded); Assert.Equal(Json(checkpoint), Json(loaded));
        result.Events[1].SourceShipId = null; result.Events[2].Amount = 1; Assert.NotEqual(Json(result), Json(loaded.ResolvedCombats[0].Result));
    }
    [Fact]
    public void PreviousSaveAndContentSchemasAreExplicitlyRejected()
    {
        var scenario = VerticalSliceFixture.Scenario(); var save = VerticalSliceFixture.Save(scenario); save.SaveFormatVersion = 8;
        Assert.Contains(FoundationValidator.ValidateSave(save, scenario.Definitions, VerticalSliceFixture.Compatibility(scenario)).Issues, i => i.Code == ValidationCode.UnsupportedSaveFormat);
        scenario.Definitions.ContentFormatVersion = 4;
        Assert.Contains(FoundationValidator.ValidateScenario(scenario).Issues, i => i.Code == ValidationCode.UnsupportedContentFormat);
    }

}
