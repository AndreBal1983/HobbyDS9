using System.Numerics;
using HobbyDS9.Contracts;
using HobbyDS9.Simulation;
using HobbyDS9.TacticalPlayground;
using Xunit;

namespace HobbyDS9.Simulation.Tests;

public sealed class TacticalPlaygroundTests
{
    private static (int Code, string Output, string Error) Run(string[] args, string input = "")
    {
        var output = new StringWriter(); var errors = new StringWriter();
        var code = PlaygroundConsole.Run(args, new StringReader(input), output, errors);
        return (code, output.ToString(), errors.ToString());
    }
    private static TacticalOrderV2Dto Order(PlaygroundSession session, TacticalManeuver maneuver)
    {
        var o = PlaygroundScenarios.CloneOrder(session.State.Formations.First(f => f.SideId == PlaygroundScenarios.PlayerSide).StandingOrder);
        o.Maneuver = maneuver; if (maneuver != TacticalManeuver.Screen) o.ProtectedFormationId = null; return o;
    }
    [Theory]
    [InlineData("cruiser")] [InlineData("screen")] [InlineData("breakthrough")]
    public void ReadyFixturesPlayScriptedWithStableRepeatedReversedOutputs(string scenario)
    {
        var args = new[] { "--scenario", scenario, "--seed", "47", "--script", ";;" };
        var first = Run(args); Assert.Equal(0, first.Code); Assert.Empty(first.Error);
        Assert.Equal(first, Run(args)); Assert.Equal(first, Run(args.Concat(new[] { "--reverse-fixture" }).ToArray()));
        Assert.Contains("OMNISCIENT SANDBOX VIEW", first.Output); Assert.Contains("playground-sha256=", first.Output);
        Assert.Contains("ENCOUNTER OUTCOME", first.Output); Assert.InRange(first.Output.Split("AFTER-ACTION REPORT").Length - 1, 1, 3);
    }
    [Theory]
    [InlineData("--unknown")] [InlineData("--scenario bogus")] [InlineData("--seed -1")] [InlineData("--seed 18446744073709551616")]
    [InlineData("--seed")] [InlineData("--stop-after 3")] [InlineData("--stop-after 1")]
    [InlineData("--resume x --seed 1")] [InlineData("--details --details")]
    public void BadCliInputFailsBeforeResolving(string arguments)
    {
        var result = Run(arguments.Split(' ')); Assert.Equal(2, result.Code); Assert.NotEmpty(result.Error);
        Assert.DoesNotContain("AFTER-ACTION REPORT", result.Output);
    }
    [Fact]
    public void HelpMenuInvalidChoicesAndEofAreForgivingAndNeverAutoCommit()
    {
        Assert.Equal(0, Run(new[] { "--help" }).Code);
        var result = Run(Array.Empty<string>(), "nonsense\n1\n\ninvalid\n3\n\n\n\n");
        Assert.Equal(0, result.Code); Assert.Empty(result.Error); Assert.Contains("READY-TO-PLAY ENGAGEMENTS", result.Output);
        Assert.Contains("Unknown choice", result.Output); Assert.Contains("End of input", result.Output);
        Assert.DoesNotContain("AFTER-ACTION REPORT", result.Output);
    }
    [Fact]
    public void HumanConfirmationResolvesOnlyOneRoundAndBlankOrdersPersist()
    {
        // Enter choose; blank maneuver/range/priority/target; Enter confirm; Q next boundary.
        var result = Run(new[] { "--scenario", "cruiser" }, "\n\n\n\n\n\nq\n");
        Assert.Equal(0, result.Code); Assert.Empty(result.Error);
        Assert.Equal(1, result.Output.Split("AFTER-ACTION REPORT").Length - 1);
        Assert.Contains("next round 2/3", result.Output); Assert.DoesNotContain("ROUND 2", result.Output);
    }
    [Fact]
    public void DecliningConfirmationLeavesOrdersAndRngUntouched()
    {
        var result = Run(new[] { "--scenario", "cruiser" }, "\n3\n\n\n\nn\nq\n");
        Assert.Equal(0, result.Code); Assert.Contains("orders unchanged", result.Output);
        Assert.DoesNotContain("AFTER-ACTION REPORT", result.Output);
        Assert.Contains("Order: MaintainDistance", result.Output);
    }
    [Fact]
    public void FlankAndChargeSameSeedHaveProvenGameplayDifferences()
    {
        var flank = new PlaygroundSession("cruiser", 47); var charge = new PlaygroundSession("cruiser", 47);
        var before = flank.State;
        var fr = flank.Advance(new[] { Order(flank, TacticalManeuver.Flank) }, 1);
        var cr = charge.Advance(new[] { Order(charge, TacticalManeuver.Charge) }, 1);
        Assert.Equal(TacticalRange.Engagement, fr.NextState.Geometry[0].Range); Assert.Equal(TacticalSector.Starboard, fr.NextState.Geometry[0].FacingA);
        Assert.Equal(TacticalRange.Close, cr.NextState.Geometry[0].Range);
        Assert.DoesNotContain(fr.Events, e => e.Kind == TacticalEventKind.WeaponDischarged && e.WeaponId == "weapon.pulse");
        Assert.Contains(cr.Events, e => e.Kind == TacticalEventKind.WeaponDischarged && e.WeaponId == "weapon.pulse");
        Assert.Contains("Engagement -> Engagement", PlaygroundReports.Round(before, fr, false));
        Assert.Contains("Fore/Fore -> Starboard/Fore", PlaygroundReports.Round(before, fr, false));
        var foeFlank = fr.NextState.Formations.Single(f => f.SideId != PlaygroundScenarios.PlayerSide).StandingOrder;
        var foeCharge = cr.NextState.Formations.Single(f => f.SideId != PlaygroundScenarios.PlayerSide).StandingOrder;
        Assert.Equal(PlaygroundFiles.Json(foeFlank), PlaygroundFiles.Json(foeCharge)); // No reactive enemy counter-pick.
    }
    [Fact]
    public void ReferencePlaythroughPinsIndependentSandboxHashAndDifferentOrderOutcomes()
    {
        var flank = new PlaygroundSession("cruiser", 47); var charge = new PlaygroundSession("cruiser", 47);
        for (int round = 1; round <= 3; round++)
        { flank.Advance(new[] { Order(flank, TacticalManeuver.Flank) }, round); charge.Advance(new[] { Order(charge, TacticalManeuver.Charge) }, round); }
        Assert.Equal(220, flank.State.Formations.Single(f => f.SideId == PlaygroundScenarios.PlayerSide).Ships[0].Hull);
        Assert.Equal(63, charge.State.Formations.Single(f => f.SideId == PlaygroundScenarios.PlayerSide).Ships[0].Hull);
        Assert.Equal("23f2fcd1d5a5481cdcb33facb340827c8ea7d696aa9d1fc5e87cf8000f74f640", flank.Checksum());
        Assert.Contains("SAME-SEED COMPARISON", PlaygroundReports.Compare(flank.Export(), charge.Export()));
    }
    [Fact]
    public void SessionRejectsStaleForeignDuplicateAndBadOrdersWithoutPartialMutation()
    {
        var session = new PlaygroundSession("cruiser", 47); var before = session.Checksum(); var order = Order(session, TacticalManeuver.Charge);
        Assert.Throws<ArgumentException>(() => session.Advance(new[] { order }, 2));
        Assert.Throws<ArgumentException>(() => session.Advance(new[] { order, order }, 1));
        order.FormationId = "formation.attack-wing"; Assert.Throws<ArgumentException>(() => session.Advance(new[] { order }, 1));
        Assert.Equal(before, session.Checksum());
    }
    [Fact]
    public void SessionOwnsInputsExportsAndHistory()
    {
        var session = new PlaygroundSession("cruiser", 47); var order = Order(session, TacticalManeuver.Flank);
        session.Advance(new[] { order }, 1); var expected = session.Checksum(); order.Maneuver = TacticalManeuver.Charge;
        var export = session.Export(); export.State.RandomState = 0; export.Rounds[0].PlayerOrders.Clear(); export.State.Formations.Clear();
        Assert.Equal(expected, session.Checksum());
    }
    [Theory]
    [InlineData(1)] [InlineData(2)]
    public void SaveResumeAtEachInteractiveBoundaryIsExactlyEquivalent(int boundary)
    {
        var whole = new PlaygroundSession("cruiser", 47); var split = new PlaygroundSession("cruiser", 47);
        for (int r = 1; r <= 3; r++) whole.Advance(new[] { Order(whole, TacticalManeuver.Flank) }, r);
        for (int r = 1; r <= boundary; r++) split.Advance(new[] { Order(split, TacticalManeuver.Flank) }, r);
        var path = Path.Combine(Path.GetTempPath(), "hobbyds9-playground-" + Guid.NewGuid().ToString("N"), "save.json");
        try
        {
            PlaygroundFiles.Save(path, split); var restored = PlaygroundFiles.Load(path);
            Assert.Equal(split.Checksum(), restored.Checksum());
            for (int r = boundary + 1; r <= 3; r++) restored.Advance(new[] { Order(restored, TacticalManeuver.Flank) }, r);
            Assert.Equal(whole.Checksum(), restored.Checksum());
            PlaygroundFiles.Save(path, restored); Assert.Equal(whole.Checksum(), PlaygroundFiles.Load(path).Checksum());
        }
        finally { Directory.Delete(Path.GetDirectoryName(path)!, recursive: true); }
    }
    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(2)] [InlineData(3)]
    public void CorruptSaveRejectsWithoutReplacingRunningSession(int corruption)
    {
        var session = new PlaygroundSession("cruiser", 47); session.Advance(new[] { Order(session, TacticalManeuver.Flank) }, 1);
        var save = session.Export(); var expected = session.Checksum();
        switch (corruption)
        {
            case 0: save.FormatVersion = 14; break;
            case 1: save.State.RandomState++; break;
            case 2: save.State.Formations[0].Ships[0].Hull++; break;
            case 3: save.Rounds[0].Events.Clear(); break;
        }
        Assert.Throws<ArgumentException>(() => PlaygroundSession.Restore(save)); Assert.Equal(expected, session.Checksum());
    }
    [Fact]
    public void ScriptCliWritesRoundOneAndResumesWithNoInteractiveInput()
    {
        var root = Path.Combine(Path.GetTempPath(), "hobbyds9-script-" + Guid.NewGuid().ToString("N")); var path = Path.Combine(root, "save.json");
        try
        {
            var first = Run(new[] { "--scenario", "cruiser", "--script", "Flank", "--stop-after", "1", "--save", path });
            Assert.Equal(0, first.Code); Assert.Empty(first.Error); Assert.Equal(2, PlaygroundFiles.Load(path).State.NextRound);
            var second = Run(new[] { "--resume", path, "--script", "Flank;Flank", "--save", path });
            Assert.Equal(0, second.Code); Assert.Empty(second.Error);
            Assert.Equal("23f2fcd1d5a5481cdcb33facb340827c8ea7d696aa9d1fc5e87cf8000f74f640", PlaygroundFiles.Load(path).Checksum());
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, recursive: true); }
    }
    [Fact]
    public void JsonScriptsSelectEveryHumanFormationAndValidateEnumAndUnknownFields()
    {
        var session = new PlaygroundSession("screen", 47); var root = Path.Combine(Path.GetTempPath(), "hobbyds9-json-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(root);
        var path = Path.Combine(root, "script.json");
        try
        {
            File.WriteAllText(path, "{\"Rounds\":[{\"Orders\":[{\"FormationId\":\"formation.escort\",\"Maneuver\":\"Screen\",\"ProtectedFormationId\":\"formation.transport\"},{\"FormationId\":\"formation.transport\",\"Maneuver\":\"MaintainDistance\"}]}]}");
            var script = PlaygroundScript.Read(path, session.State); Assert.Equal(2, script.Rounds[0].Orders.Count);
            session.Advance(script.Rounds[0].Orders, 1);
            File.WriteAllText(path, "{\"Rounds\":[{\"Orders\":[{\"Maneuver\":\"FireSubsystem\"}]}]}");
            Assert.Throws<System.Text.Json.JsonException>(() => PlaygroundScript.Read(path, session.State));
            File.WriteAllText(path, "{\"TypoRounds\":[]}"); Assert.Throws<System.Text.Json.JsonException>(() => PlaygroundScript.Read(path, session.State));
        }
        finally { Directory.Delete(root, recursive: true); }
    }
    [Fact]
    public void ReportsUseBackendArmorOverflowAndOpportunityEventsWithoutRecomputingRules()
    {
        var session = new PlaygroundSession("screen", 47); var before = session.State; var result = session.Advance(new[] { Order(session, TacticalManeuver.Screen) }, 1);
        var report = PlaygroundReports.Round(before, result, false); var detailed = PlaygroundReports.Round(before, result, true);
        int attempts = result.Events.Count(e => e.Kind == TacticalEventKind.InterceptionAttempt && e.FormationId == "formation.escort");
        Assert.Contains($"{attempts} beam opportunities spent instead of offense", report);
        Assert.DoesNotContain("DETAILED AUTHORITATIVE EVENT LOG", report); Assert.Contains("DETAILED AUTHORITATIVE EVENT LOG", detailed);
        Assert.Contains("armor removed", report); Assert.Contains("overkill discarded", report);
        Assert.Equal(session.Checksum(), PlaygroundSession.Restore(session.Export()).Checksum());
    }
    [Fact]
    public void LocalBreakthroughStopsAfterTwoRoundsButHoldHasDifferentObjectiveResult()
    {
        var session = new PlaygroundSession("breakthrough", 47);
        session.Advance(new[] { Order(session, TacticalManeuver.Breakthrough) }, 1);
        session.Advance(new[] { Order(session, TacticalManeuver.Breakthrough) }, 2);
        Assert.Equal(TacticalCompletion.Objective, session.State.Completion);
        Assert.Equal(TacticalFormationStatus.BrokenThrough, session.State.Formations.Single(f => f.SideId == PlaygroundScenarios.PlayerSide).Status);
        Assert.Contains("Standalone local outcomes only", PlaygroundReports.Outcome(session));
        var hold = new PlaygroundSession("breakthrough", 47);
        for (int i = 1; i <= 3; i++) hold.Advance(new[] { Order(hold, TacticalManeuver.MaintainDistance) }, i);
        Assert.False(hold.State.Formations.Single(f => f.SideId == PlaygroundScenarios.PlayerSide).ObjectiveCompleted);
    }
    [Fact]
    public void InteractiveReplayUsesSameSeedAndShowsTheDifferentOrderComparison()
    {
        var first = string.Concat(Enumerable.Repeat("\n3\n\n\n\n\n", 3));
        var second = string.Concat(Enumerable.Repeat("\n2\n\n\n\n", 3));
        var result = Run(new[] { "--scenario", "cruiser", "--seed", "47" }, first + "r\n" + second + "q\n");
        Assert.Equal(0, result.Code); Assert.Empty(result.Error); Assert.Contains("SAME-SEED COMPARISON", result.Output);
        Assert.Contains("human survivors=1, hull=220", result.Output); Assert.Contains("human survivors=1, hull=63", result.Output);
        Assert.Equal(6, result.Output.Split("AFTER-ACTION REPORT").Length - 1);
        Assert.DoesNotContain("next round 4/3", result.Output);
    }
    [Fact]
    public void ReportSeparatesActualHullLossFromBackendOverkill()
    {
        var state = TacticalCombatV2Fixture.Encounter(); TacticalCombatV2Fixture.Unarmed(state);
        var target = TacticalCombatV2Fixture.Ship(state, 1); target.Hull = 10; TacticalCombatV2Fixture.Shields(target, 0);
        TacticalCombatV2Fixture.Weapon(TacticalCombatV2Fixture.Ship(state), TacticalWeaponClass.Beam, 1000);
        var result = TacticalCombatV2Fixture.Round(state);
        Assert.Contains("incoming landed 1000; shields absorbed 0; armor removed 0; post-armor 1000; hull lost 10; overkill discarded 990", PlaygroundReports.Round(state, result, false));
    }
    [Fact]
    public void HostJsonUsesCanonicalLfAndAcceptsEquivalentWindowsWhitespace()
    {
        var session = new PlaygroundSession("cruiser", 47); session.Advance(new[] { Order(session, TacticalManeuver.Flank) }, 1);
        var json = PlaygroundFiles.Json(session.Export()); Assert.DoesNotContain("\r", json);
        var windows = json.Replace("\n", "\r\n", StringComparison.Ordinal);
        var restored = System.Text.Json.JsonSerializer.Deserialize<PlaygroundSave>(windows, PlaygroundFiles.Options)!;
        Assert.Equal(session.Checksum(), PlaygroundSession.Restore(restored).Checksum());
    }
}
