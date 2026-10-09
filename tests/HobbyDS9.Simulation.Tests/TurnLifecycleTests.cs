using System.Text.Json;
using HobbyDS9.Contracts;
using HobbyDS9.Simulation;
using Xunit;

namespace HobbyDS9.Simulation.Tests;

public sealed class TurnLifecycleTests
{
    private static readonly SaveCompatibility Compatibility = new("scenario.turn-fixture", "turn-foundation-v1", new string('a', 64));
    private static readonly StrategicTurnPhase[] PhaseOrder =
    {
        StrategicTurnPhase.AiPlanning, StrategicTurnPhase.Economy, StrategicTurnPhase.Research,
        StrategicTurnPhase.Production, StrategicTurnPhase.Movement, StrategicTurnPhase.EncounterCreation
    };

    private static DefinitionCatalogDto Definitions() => new()
    {
        ContentFormatVersion = FoundationVersions.ContentFormatVersion, EconomicProfiles = new(),
        DefinitionIds = new() { "system.beta", "civilization.ai", "system.alpha", "civilization.human" },
        Civilizations = new()
        {
            new() { DefinitionId = "civilization.human", DisplayName = "Human fixture" },
            new() { DefinitionId = "civilization.ai", DisplayName = "AI fixture" }
        },
        StarSystems = new()
        {
            new() { DefinitionId = "system.beta", DisplayName = "Beta" },
            new() { DefinitionId = "system.alpha", DisplayName = "Alpha" }
        },
        GalaxyEdges = new() { new() { EndpointA = "node.beta", EndpointB = "node.alpha", Kind = GalaxyEdgeKind.Wormhole } }
    };
    private static SaveEnvelopeDto Save(int turn = 7, long revision = 5, SessionPhase phase = SessionPhase.Planning) => new()
    {
        SaveFormatVersion = FoundationVersions.SaveFormatVersion, ScenarioId = Compatibility.ScenarioId,
        RulesVersion = Compatibility.RulesVersion, ContentHash = Compatibility.ContentHash,
        GameState = new()
        {
            Phase = phase, TurnNumber = turn, StateRevision = revision, NextEntitySequence = 17,
            RngState = new SplitMix64Random(47).ToDto(),
            Colonies = new(), AcceptedCommandIds = revision >= 2 ? new() { "cmd.z", "cmd.a" } : new(),
            Civilizations = new()
            {
                new() { CivilizationId = "civ.human", DefinitionId = "civilization.human", ControllerKind = ControllerKind.Human },
                new() { CivilizationId = "civ.ai", DefinitionId = "civilization.ai", ControllerKind = ControllerKind.AI }
            },
            StarSystems = new()
            {
                new() { SystemId = "node.beta", DefinitionId = "system.beta" },
                new() { SystemId = "node.alpha", DefinitionId = "system.alpha", OwnerCivilizationId = "civ.human" }
            },
            EntityIdentities = new()
            {
                new() { EntityId = "node.beta", DefinitionId = "system.beta" },
                new() { EntityId = "civ.human", DefinitionId = "civilization.human" },
                new() { EntityId = "token.spawn.0000000000000010" },
                new() { EntityId = "civ.ai", DefinitionId = "civilization.ai" },
                new() { EntityId = "node.alpha", DefinitionId = "system.alpha" }
            }
        }
    };
    private static SimulationSession Session(SaveEnvelopeDto? save = null, Func<StrategicTurnPhase, GameStateDto, bool>? probe = null,
        ICommandHandler? handler = null)
    {
        var handlers = new Dictionary<CommandKind, ICommandHandler>();
        if (handler != null) handlers.Add(CommandKind.SetResearchTarget, handler);
        return new SimulationSession(save ?? Save(), Definitions(), Compatibility, handlers, probe);
    }
    private static CommandEnvelope Command(int turn = 7, long revision = 5, string commandId = "cmd.new") => new()
    {
        CommandId = commandId, ActorCivilizationId = "civ.human", ExpectedTurn = turn, ExpectedRevision = revision,
        Kind = CommandKind.SetResearchTarget, SetResearchTarget = new() { TargetDefinitionId = "definition.future" }
    };
    private static string StateJson(SimulationSession session) => JsonSerializer.Serialize(session.ExportSave());
    private sealed class CommandProbe : ICommandHandler
    {
        public Func<GameStateDto, CommandEnvelope, CommandErrorCode> Action { get; set; } = (_, _) => CommandErrorCode.None;
        public int Calls { get; private set; }
        public CommandErrorCode Execute(GameStateDto state, CommandEnvelope command) { Calls++; return Action(state, command); }
    }

    [Fact]
    public void SuccessfulEmptyTurnCompletesExactlyCurrentTurnAndChangesOnlyBookkeeping()
    {
        var session = Session(); var old = session.CaptureSnapshot(); var expected = session.ExportSave();
        expected.GameState.TurnNumber++; expected.GameState.StateRevision++; expected.GameState.AcceptedCommandIds!.Clear();
        var result = session.AdvanceTurn(7, 5);
        Assert.Equal(TurnStatus.Completed, result.Status); Assert.Equal(TurnErrorCode.None, result.ErrorCode);
        Assert.Equal(7, result.AttemptedTurnNumber); Assert.Equal(7, result.CompletedTurnNumber);
        Assert.Equal(8, result.TurnNumber); Assert.Equal(6, result.StateRevision); Assert.Equal(SessionPhase.Planning, result.Phase);
        Assert.Empty(result.Events); Assert.Empty(result.CommandResults);
        Assert.Equal(JsonSerializer.Serialize(expected), StateJson(session));
        Assert.Equal(SessionPhase.Planning, session.CaptureSnapshot().Phase);
        Assert.Equal(7, old.TurnNumber); Assert.Equal(5, old.StateRevision); Assert.Equal(2, old.AcceptedCommandIds.Count);
        Assert.Equal(47UL, session.CaptureSnapshot().RngState.State);
        Assert.Equal(17UL, session.CaptureSnapshot().NextEntitySequence);
    }

    [Fact]
    public void EmptyTurnInvokesEachBoundaryOnceInTheFixedOrderOnOneDeepCopy()
    {
        var phases = new List<StrategicTurnPhase>(); GameStateDto? retained = null;
        SimulationSession? session = null;
        session = Session(probe: (phase, staging) =>
        {
            phases.Add(phase);
            if (retained != null) Assert.Same(retained, staging);
            retained = staging;
            var authoritative = session!.ExportSave().GameState;
            Assert.NotSame(authoritative, staging);
            Assert.NotSame(authoritative.RngState, staging.RngState);
            Assert.NotSame(authoritative.EntityIdentities, staging.EntityIdentities);
            Assert.NotSame(authoritative.EntityIdentities[0], staging.EntityIdentities[0]);
            Assert.NotSame(authoritative.Civilizations![0], staging.Civilizations![0]);
            Assert.NotSame(authoritative.StarSystems![0], staging.StarSystems![0]);
            Assert.NotSame(authoritative.AcceptedCommandIds, staging.AcceptedCommandIds);
            Assert.Equal(7, staging.TurnNumber); Assert.Equal(5, staging.StateRevision); Assert.Equal(2, staging.AcceptedCommandIds!.Count);
            Assert.Equal(7, session.CaptureSnapshot().TurnNumber); // No partial turn visible even inside a phase.
            return true;
        });
        session.AdvanceTurn(7, 5);
        Assert.Equal(PhaseOrder, phases);
        var committed = StateJson(session);
        retained!.RngState.State = 99; retained.NextEntitySequence = 99;
        retained.Phase = SessionPhase.AwaitingCombat; retained.TurnNumber = 99; retained.StateRevision = 99;
        retained.EntityIdentities[0].DefinitionId = null; retained.EntityIdentities.Clear();
        retained.Civilizations![0].ControllerKind = ControllerKind.AI; retained.Civilizations.Clear();
        retained.StarSystems![0].OwnerCivilizationId = null; retained.StarSystems.Clear();
        retained.AcceptedCommandIds!.Add("cmd.fake");
        Assert.Equal(committed, StateJson(session));
    }

    [Theory]
    [InlineData(6, 5, TurnErrorCode.StaleTurn)]
    [InlineData(8, 5, TurnErrorCode.StaleTurn)]
    [InlineData(7, 4, TurnErrorCode.StaleRevision)]
    [InlineData(7, 6, TurnErrorCode.StaleRevision)]
    public void StaleAdvanceRequestsRejectBeforeAnyPhaseAndPreserveState(int turn, long revision, TurnErrorCode error)
    {
        var calls = 0; var session = Session(probe: (_, _) => { calls++; return true; }); var before = StateJson(session);
        var result = session.AdvanceTurn(turn, revision);
        Assert.Equal(TurnStatus.Rejected, result.Status); Assert.Equal(error, result.ErrorCode);
        Assert.Equal(7, result.TurnNumber); Assert.Equal(5, result.StateRevision); Assert.Null(result.CompletedTurnNumber);
        Assert.Empty(result.Events); Assert.Empty(result.CommandResults); Assert.Equal(0, calls);
        Assert.Equal(before, StateJson(session));
    }

    [Fact]
    public void LedgerClearsOnlyOnSuccessfulCompletionAndSameIdCanBeUsedNextTurn()
    {
        var fail = true; var handler = new CommandProbe();
        var session = Session(probe: (_, _) => !fail, handler: handler);
        var command = Command(); Assert.True(session.SubmitCommand(command).Accepted);
        Assert.Equal(6, session.CaptureSnapshot().StateRevision);
        var before = StateJson(session);
        Assert.Equal(TurnErrorCode.StaleRevision, session.AdvanceTurn(7, 5).ErrorCode);
        Assert.Equal(TurnErrorCode.PhaseFailed, session.AdvanceTurn(7, 6).ErrorCode);
        Assert.Equal(before, StateJson(session));
        Assert.Equal(CommandErrorCode.DuplicateCommand, session.SubmitCommand(command).ErrorCode);
        fail = false; var result = session.AdvanceTurn(7, 6);
        Assert.Equal(7, result.StateRevision); Assert.Empty(session.CaptureSnapshot().AcceptedCommandIds);
        Assert.Equal(CommandErrorCode.StaleTurn, session.SubmitCommand(command).ErrorCode);
        command.ExpectedTurn = 8;
        Assert.Equal(CommandErrorCode.StaleRevision, session.SubmitCommand(command).ErrorCode);
        command.ExpectedRevision = 7;
        Assert.True(session.SubmitCommand(command).Accepted);
        Assert.Equal(8, session.CaptureSnapshot().StateRevision);
        Assert.Equal(new[] { new EntityId("cmd.new") }, session.CaptureSnapshot().AcceptedCommandIds);
        Assert.Equal(2, handler.Calls);
    }

    [Fact]
    public void AwaitingCombatPersistsAsBlockedMarkerAndNeverReopensPlanning()
    {
        var save = Save(phase: SessionPhase.AwaitingCombat); var handler = new CommandProbe(); var calls = 0;
        var session = Session(save, (_, _) => { calls++; return true; }, handler);
        var before = StateJson(session);
        Assert.Equal(SessionPhase.AwaitingCombat, session.CaptureSnapshot().Phase);
        Assert.Equal(CommandErrorCode.CombatPending, session.SubmitCommand(Command()).ErrorCode);
        Assert.Equal(CommandErrorCode.CombatPending, session.SubmitCommand(Command(turn: 6, revision: 0)).ErrorCode);
        var result = session.AdvanceTurn(7, 5);
        Assert.Equal(TurnStatus.Rejected, result.Status); Assert.Equal(TurnErrorCode.CombatPending, result.ErrorCode);
        Assert.Equal(SessionPhase.AwaitingCombat, result.Phase); Assert.Null(result.CompletedTurnNumber);
        Assert.Equal(TurnErrorCode.CombatPending, session.AdvanceTurn(0, 0).ErrorCode);
        Assert.Equal(0, handler.Calls); Assert.Equal(0, calls); Assert.Equal(before, StateJson(session));
        var loaded = Session(JsonSerializer.Deserialize<SaveEnvelopeDto>(before));
        Assert.Equal(before, StateJson(loaded));
        Assert.Equal(SessionPhase.AwaitingCombat, loaded.CaptureSnapshot().Phase);
        Assert.Equal(TurnErrorCode.CombatPending, loaded.AdvanceTurn(7, 5).ErrorCode);
    }

    public static IEnumerable<object[]> PhaseFailures()
    {
        foreach (var phase in PhaseOrder)
        foreach (var mode in new[] { "return-failure", "exception", "invariant" }) yield return new object[] { (int)phase, mode };
    }
    [Theory]
    [MemberData(nameof(PhaseFailures))]
    public void EveryPhaseFailureRollsBackAllFactsAndCanBeRetried(int phaseNumber, string mode)
    {
        var phase = (StrategicTurnPhase)phaseNumber; var fail = true; var seen = new List<StrategicTurnPhase>();
        GameStateDto? retained = null; SimulationSession? session = null;
        session = Session(probe: (current, staging) =>
        {
            seen.Add(current); retained = staging;
            if (!fail || current != phase) return true;
            var before = StateJson(session!);
            var random = SplitMix64Random.FromDto(staging.RngState); random.NextUInt64(); staging.RngState = random.ToDto();
            var allocator = new EntityIdAllocator(staging.NextEntitySequence);
            staging.EntityIdentities.Add(new() { EntityId = allocator.Allocate("token").Value });
            staging.NextEntitySequence = allocator.NextEntitySequence;
            staging.Civilizations![0].ControllerKind = ControllerKind.Human;
            staging.StarSystems![0].OwnerCivilizationId = null;
            staging.AcceptedCommandIds!.Clear(); staging.Phase = SessionPhase.AwaitingCombat;
            staging.TurnNumber = 99; staging.StateRevision = 999;
            Assert.Equal(before, StateJson(session!));
            if (mode == "exception") throw new ApplicationException("Deliberate test phase failure.");
            return mode != "return-failure";
        });
        var authoritative = StateJson(session);
        if (mode == "return-failure")
        {
            var result = session.AdvanceTurn(7, 5);
            Assert.Equal(TurnStatus.Rejected, result.Status); Assert.Equal(TurnErrorCode.PhaseFailed, result.ErrorCode);
            Assert.Equal(SessionPhase.Planning, result.Phase); Assert.Equal(5, result.StateRevision);
            Assert.Empty(result.Events); Assert.Empty(result.CommandResults);
        }
        else if (mode == "exception") Assert.Throws<ApplicationException>(() => session.AdvanceTurn(7, 5));
        else Assert.Throws<InvalidOperationException>(() => session.AdvanceTurn(7, 5));
        Assert.Equal(authoritative, StateJson(session));
        Assert.Equal(PhaseOrder.Take(phaseNumber + 1), seen);
        retained!.RngState.State = 100; retained.AcceptedCommandIds!.Add("cmd.late"); retained.EntityIdentities.Clear();
        Assert.Equal(authoritative, StateJson(session));
        fail = false; seen.Clear();
        Assert.Equal(TurnStatus.Completed, session.AdvanceTurn(7, 5).Status); Assert.Equal(PhaseOrder, seen);
        Assert.Empty(session.CaptureSnapshot().AcceptedCommandIds);
    }

    [Theory]
    [InlineData("phase")]
    [InlineData("null-phase")]
    [InlineData("unknown-phase")]
    [InlineData("turn")]
    [InlineData("revision")]
    [InlineData("ledger")]
    [InlineData("rng")]
    [InlineData("allocation")]
    [InlineData("controller")]
    [InlineData("owner")]
    [InlineData("identity")]
    [InlineData("null-rng")]
    [InlineData("remove-node")]
    public void InvariantFailuresCannotCommitEvenOtherwiseValidFoundationChanges(string corruption)
    {
        var session = Session(probe: (phase, s) =>
        {
            if (phase != StrategicTurnPhase.EncounterCreation) return true;
            switch (corruption)
            {
                case "phase": s.Phase = SessionPhase.AwaitingCombat; break;
                case "null-phase": s.Phase = null; break;
                case "unknown-phase": s.Phase = (SessionPhase)99; break;
                case "turn": s.TurnNumber++; break;
                case "revision": s.StateRevision++; break;
                case "ledger": s.AcceptedCommandIds!.Clear(); break;
                case "rng": s.RngState.State = 48; break;
                case "allocation": s.NextEntitySequence++; break;
                case "controller": s.Civilizations![0].ControllerKind = ControllerKind.Human; break;
                case "owner": s.StarSystems![0].OwnerCivilizationId = null; break;
                case "identity": s.EntityIdentities.Single(e => e.EntityId.StartsWith("token.", StringComparison.Ordinal)).DefinitionId = "civilization.ai"; break;
                case "null-rng": s.RngState = null!; break;
                case "remove-node": s.StarSystems!.RemoveAt(0); break;
            }
            return true;
        });
        var before = StateJson(session);
        Assert.Throws<InvalidOperationException>(() => session.AdvanceTurn(7, 5));
        Assert.Equal(before, StateJson(session));
    }

    [Fact]
    public void CommandHandlersCannotChangeAuthoritativePhase()
    {
        var probe = new CommandProbe { Action = (s, _) => { s.Phase = SessionPhase.AwaitingCombat; return CommandErrorCode.None; } };
        var session = Session(handler: probe); var before = StateJson(session);
        Assert.Throws<InvalidOperationException>(() => session.SubmitCommand(Command()));
        Assert.Equal(before, StateJson(session));
    }

    [Theory]
    [InlineData("turn", TurnErrorCode.TurnNumberExhausted)]
    [InlineData("revision", TurnErrorCode.RevisionExhausted)]
    public void CounterExhaustionRejectsBeforePhasesWithoutOverflowOrLedgerClearing(string counter, TurnErrorCode error)
    {
        var save = Save(turn: counter == "turn" ? int.MaxValue : 7, revision: counter == "revision" ? long.MaxValue : 5);
        var calls = 0; var session = Session(save, (_, _) => { calls++; return true; }); var before = StateJson(session);
        Assert.Equal(error, session.AdvanceTurn(save.GameState.TurnNumber, save.GameState.StateRevision).ErrorCode);
        Assert.Equal(0, calls); Assert.Equal(before, StateJson(session));
        save = Save(turn: int.MaxValue - 1, revision: long.MaxValue - 1); session = Session(save);
        var result = session.AdvanceTurn(int.MaxValue - 1, long.MaxValue - 1);
        Assert.Equal(TurnStatus.Completed, result.Status); Assert.Equal(int.MaxValue, result.TurnNumber); Assert.Equal(long.MaxValue, result.StateRevision);
        Assert.Equal(TurnErrorCode.TurnNumberExhausted, session.AdvanceTurn(int.MaxValue, long.MaxValue).ErrorCode);
    }

    [Fact]
    public void PlanningSavesBeforeAndAfterAdvanceResumeTheSameConsecutiveTurns()
    {
        var uninterrupted = Session();
        var reloaded = Session(JsonSerializer.Deserialize<SaveEnvelopeDto>(StateJson(uninterrupted)));
        for (var i = 0; i < 40; i++)
        {
            var result = uninterrupted.AdvanceTurn(7 + i, 5 + i);
            var resumed = reloaded.AdvanceTurn(7 + i, 5 + i);
            Assert.Equal(TurnStatus.Completed, result.Status);
            Assert.Equal(7 + i, result.CompletedTurnNumber); Assert.Equal(8 + i, result.TurnNumber); Assert.Equal(6 + i, result.StateRevision);
            Assert.Equal(JsonSerializer.Serialize(result), JsonSerializer.Serialize(resumed));
            Assert.Equal(StateJson(uninterrupted), StateJson(reloaded));
            Assert.Equal(SessionPhase.Planning, reloaded.CaptureSnapshot().Phase);
            Assert.Equal(47UL, reloaded.CaptureSnapshot().RngState.State); Assert.Equal(17UL, reloaded.CaptureSnapshot().NextEntitySequence);
            Assert.Empty(result.Events); Assert.Empty(result.CommandResults);
            Assert.True(FoundationValidator.ValidateSave(reloaded.ExportSave(), Definitions(), Compatibility).IsValid);
            reloaded = Session(JsonSerializer.Deserialize<SaveEnvelopeDto>(StateJson(reloaded)));
        }
    }

    [Fact]
    public void EquivalentReorderedInputProducesIdenticalTurnResultsAndSavedState()
    {
        var save = Save(); var definitions = Definitions(); var baseline = Session(save);
        save.GameState.EntityIdentities.Reverse(); save.GameState.Civilizations!.Reverse(); save.GameState.StarSystems!.Reverse();
        save.GameState.AcceptedCommandIds!.Reverse(); definitions.DefinitionIds.Reverse(); definitions.Civilizations!.Reverse(); definitions.StarSystems!.Reverse();
        var edge = definitions.GalaxyEdges![0]; (edge.EndpointA, edge.EndpointB) = (edge.EndpointB, edge.EndpointA);
        var reordered = new SimulationSession(save, definitions, Compatibility);
        for (var i = 0; i < 20; i++)
        {
            Assert.Equal(JsonSerializer.Serialize(baseline.AdvanceTurn(7 + i, 5 + i)), JsonSerializer.Serialize(reordered.AdvanceTurn(7 + i, 5 + i)));
            Assert.Equal(StateJson(baseline), StateJson(reordered));
            Assert.Equal(JsonSerializer.Serialize(baseline.CaptureSnapshot()), JsonSerializer.Serialize(reordered.CaptureSnapshot()));
        }
    }

    [Theory]
    [InlineData("advance-to-advance")]
    [InlineData("advance-to-command")]
    [InlineData("command-to-advance")]
    public void NestedMutationsThrowAndLeaveAuthorityUnchanged(string nesting)
    {
        var nested = true; SimulationSession? session = null; var handler = new CommandProbe();
        if (nesting == "command-to-advance") handler.Action = (_, _) => { if (nested) session!.AdvanceTurn(7, 5); return CommandErrorCode.None; };
        session = Session(probe: (_, _) =>
        {
            if (nested && nesting == "advance-to-advance") session!.AdvanceTurn(7, 5);
            if (nested && nesting == "advance-to-command") session!.SubmitCommand(Command());
            return true;
        }, handler: handler);
        var before = StateJson(session);
        if (nesting == "command-to-advance") Assert.Throws<InvalidOperationException>(() => session.SubmitCommand(Command()));
        else Assert.Throws<InvalidOperationException>(() => session.AdvanceTurn(7, 5));
        Assert.Equal(before, StateJson(session));
        nested = false; Assert.Equal(TurnStatus.Completed, session.AdvanceTurn(7, 5).Status);
    }

    [Fact]
    public void ConcurrentDuplicateAdvanceRequestsCannotCompleteTwoTurns()
    {
        var session = Session(); var results = new TurnResult[2];
        Parallel.Invoke(() => results[0] = session.AdvanceTurn(7, 5), () => results[1] = session.AdvanceTurn(7, 5));
        Assert.Single(results, r => r.Status == TurnStatus.Completed);
        Assert.Equal(TurnErrorCode.StaleTurn, Assert.Single(results, r => r.Status == TurnStatus.Rejected).ErrorCode);
        Assert.Equal(8, session.CaptureSnapshot().TurnNumber); Assert.Equal(6, session.CaptureSnapshot().StateRevision);
    }

    [Fact]
    public void PhaseIsRequiredValidatedDetachedAndAuthoredScenariosMustStartPlanning()
    {
        Assert.Null(JsonSerializer.Deserialize<GameStateDto>("{}")!.Phase);
        var save = Save(); save.GameState.Phase = null;
        Assert.Contains(FoundationValidator.ValidateSave(save, Definitions(), Compatibility).Issues,
            i => i.Code == ValidationCode.MissingField && i.Path == "$.GameState.Phase");
        Assert.Throws<ArgumentException>(() => Session(save));
        save.GameState.Phase = (SessionPhase)99;
        Assert.Contains(FoundationValidator.ValidateSave(save, Definitions(), Compatibility).Issues, i => i.Code == ValidationCode.InvalidSessionPhase);
        Assert.Throws<ArgumentException>(() => Session(save));
        save = Save(turn: 1, revision: 0); save.GameState.NextEntitySequence = 1;
        save.GameState.EntityIdentities.RemoveAll(e => e.EntityId.StartsWith("token.", StringComparison.Ordinal));
        var scenario = new ScenarioDto { ScenarioId = Compatibility.ScenarioId, RulesVersion = Compatibility.RulesVersion,
            ContentHash = Compatibility.ContentHash, Definitions = Definitions(), InitialState = save.GameState };
        Assert.True(FoundationValidator.ValidateScenario(scenario).IsValid);
        scenario.InitialState.Phase = SessionPhase.AwaitingCombat;
        Assert.Contains(FoundationValidator.ValidateScenario(scenario).Issues, i => i.Code == ValidationCode.InvalidSessionPhase);
        save = Save(); save.SaveFormatVersion = 3;
        Assert.Contains(FoundationValidator.ValidateSave(save, Definitions(), Compatibility).Issues, i => i.Code == ValidationCode.UnsupportedSaveFormat);
        var session = Session(); var exported = session.ExportSave(); exported.GameState.Phase = SessionPhase.AwaitingCombat;
        Assert.Equal(SessionPhase.Planning, session.CaptureSnapshot().Phase);
    }

    [Fact]
    public void TurnResultsRoundTripInHostJsonAndContainReadOnlyOrderedCollections()
    {
        var result = Session().AdvanceTurn(7, 5); var json = JsonSerializer.Serialize(result);
        Assert.Equal(json, JsonSerializer.Serialize(JsonSerializer.Deserialize<TurnResult>(json)));
        Assert.Throws<NotSupportedException>(() => ((IList<StrategicEvent>)result.Events).Clear());
        Assert.Throws<NotSupportedException>(() => ((IList<CommandResult>)result.CommandResults).Clear());
        foreach (var type in new[] { typeof(TurnResult), typeof(StrategicEvent) })
            Assert.All(type.GetProperties(), p => Assert.Null(p.SetMethod));
        var events = new List<StrategicEvent>
        {
            new(0, 7, "fixture.first", "node.beta"), new(1, 7, "fixture.second", "node.alpha")
        };
        var commands = new List<CommandResult>
        {
            new("cmd.z", CommandStatus.Rejected, CommandErrorCode.UnsupportedCommand, 7, 5),
            new("cmd.a", CommandStatus.Rejected, CommandErrorCode.UnsupportedCommand, 7, 5)
        };
        var awaiting = new TurnResult(TurnStatus.AwaitingCombat, TurnErrorCode.None, 7, 7, 6, SessionPhase.AwaitingCombat, events, commands);
        events.Add(new StrategicEvent(0, 7, "fixture.event", null));
        commands.Add(new CommandResult("cmd.fixture", CommandStatus.Rejected, CommandErrorCode.UnsupportedCommand, 7, 5));
        Assert.Equal(new[] { "fixture.first", "fixture.second" }, awaiting.Events.Select(e => e.EventCode));
        Assert.Equal(new[] { "cmd.z", "cmd.a" }, awaiting.CommandResults.Select(c => c.CommandId));
        Assert.Equal("node.beta", awaiting.Events[0].SubjectId);
        var awaitingJson = JsonSerializer.Serialize(awaiting);
        Assert.Equal(awaitingJson, JsonSerializer.Serialize(JsonSerializer.Deserialize<TurnResult>(awaitingJson)));
        Assert.Null(awaiting.CompletedTurnNumber);
        Assert.Equal(TurnStatus.AwaitingCombat, awaiting.Status); // Contract only; engine never produces this yet.
    }
}
