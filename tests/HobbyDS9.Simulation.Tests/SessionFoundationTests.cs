using System.Text.Json;
using HobbyDS9.Contracts;
using HobbyDS9.Simulation;
using Xunit;

namespace HobbyDS9.Simulation.Tests;

public sealed class SessionFoundationTests
{
    private static readonly EntityId Alpha = new("civ.alpha");
    private static readonly EntityId Beta = new("civ.beta");
    private static readonly SaveCompatibility Compatibility = new("scenario.session", "foundation-v2", new string('a', 64));
    private static DefinitionCatalogDto Definitions() => new() { DefinitionIds = new() { "definition.beta", "definition.alpha" } };
    private static SaveEnvelopeDto Save(long revision = 0) => new()
    {
        SaveFormatVersion = FoundationVersions.SaveFormatVersion, ScenarioId = Compatibility.ScenarioId,
        RulesVersion = Compatibility.RulesVersion, ContentHash = Compatibility.ContentHash,
        GameState = new()
        {
            TurnNumber = 1, StateRevision = revision, NextEntitySequence = 1, RngState = new SplitMix64Random(47).ToDto(),
            AcceptedCommandIds = new(), EntityIdentities = new()
            {
                new() { EntityId = "entity.target", DefinitionId = "definition.alpha" },
                new() { EntityId = Beta.Value }, new() { EntityId = Alpha.Value }
            }
        }
    };

    private static CommandEnvelope Command(string id = "cmd.one", long revision = 0, string? actor = null) => new()
    {
        CommandId = id, ActorCivilizationId = actor ?? Alpha.Value, ExpectedTurn = 1, ExpectedRevision = revision,
        Kind = CommandKind.SetResearchTarget, SetResearchTarget = new() { TargetDefinitionId = "definition.beta" }
    };

    private static SimulationSession Session(Probe? probe = null, SaveEnvelopeDto? save = null, bool controlBoth = false)
    {
        var handlers = new Dictionary<CommandKind, ICommandHandler>();
        if (probe != null) handlers.Add(CommandKind.SetResearchTarget, probe);
        return new SimulationSession(save ?? Save(), Definitions(), Compatibility, new[] { Alpha, Beta },
            controlBoth ? new[] { Alpha, Beta } : new[] { Alpha }, handlers);
    }

    private static string StateJson(SimulationSession session) => JsonSerializer.Serialize(session.ExportSave());

    // Test-only generic identity mutation. No research or other gameplay behavior.
    private sealed class Probe : ICommandHandler
    {
        public int Calls { get; private set; }
        public GameStateDto? RetainedState { get; private set; }
        public CommandEnvelope? RetainedCommand { get; private set; }
        public Func<GameStateDto, CommandEnvelope, CommandErrorCode> Action { get; set; } = (state, _) =>
        {
            state.EntityIdentities.Single(e => e.EntityId == "entity.target").DefinitionId = "definition.beta";
            return CommandErrorCode.None;
        };
        public CommandErrorCode Execute(GameStateDto state, CommandEnvelope command)
        { Calls++; RetainedState = state; RetainedCommand = command; return Action(state, command); }
    }

    [Fact]
    public void SessionCopiesAllInputStateDefinitionsAndActorRegistries()
    {
        var save = Save();
        var definitions = Definitions();
        var actors = new List<EntityId> { Alpha, Beta };
        var controlled = new List<EntityId> { Alpha };
        var session = new SimulationSession(save, definitions, Compatibility, actors, controlled);
        var before = StateJson(session);
        save.GameState.EntityIdentities[0].DefinitionId = "invalid";
        save.GameState.EntityIdentities.Clear();
        save.GameState.RngState.State = 999;
        save.GameState.NextEntitySequence = 99;
        save.GameState.StateRevision = 99;
        save.GameState.TurnNumber = 99;
        save.GameState.AcceptedCommandIds!.Add("cmd.fake");
        save.GameState = null!;
        save.ScenarioId = "scenario.other";
        definitions.DefinitionIds.Clear();
        actors.Clear();
        controlled.Add(Beta);
        Assert.Equal(before, StateJson(session));
        Assert.Equal(2, session.CaptureSnapshot().DefinitionIds.Count);
        Assert.Equal(2, session.CaptureSnapshot().Actors.Count);
        Assert.Equal(CommandErrorCode.ActorNotControlled, session.SubmitCommand(Command(actor: Beta.Value)).ErrorCode);
    }

    [Fact]
    public void ExportedSaveIsDeeplyDetachedAndDoesNotLeakAuthority()
    {
        var session = Session();
        var before = StateJson(session);
        var exported = session.ExportSave();
        exported.GameState.RngState.State = 0;
        exported.GameState.EntityIdentities[0].EntityId = "changed";
        exported.GameState.EntityIdentities.Clear();
        exported.GameState.AcceptedCommandIds!.Add("cmd.changed");
        exported.GameState.NextEntitySequence = 100;
        exported.GameState.TurnNumber = 100;
        exported.GameState.StateRevision = 100;
        exported.ContentHash = "changed";
        Assert.Equal(before, StateJson(session));
        Assert.NotSame(session.ExportSave().GameState.RngState, session.ExportSave().GameState.RngState);
    }

    [Fact]
    public void SnapshotsAreDeeplyReadOnlyDetachedAndCanonicallyOrdered()
    {
        var session = Session(new Probe());
        var old = session.CaptureSnapshot();
        Assert.Equal(new[] { "civ.alpha", "civ.beta", "entity.target" }, old.Entities.Select(e => e.EntityId.Value));
        Assert.Equal(new[] { "definition.alpha", "definition.beta" }, old.DefinitionIds.Select(d => d.Value));
        Assert.Throws<NotSupportedException>(() => ((IList<EntityIdentitySnapshot>)old.Entities).Clear());
        Assert.Throws<NotSupportedException>(() => ((IList<DefinitionId>)old.DefinitionIds).Add(new("definition.fake")));
        Assert.Throws<NotSupportedException>(() => ((IList<ActorSnapshot>)old.Actors).Clear());
        Assert.Throws<NotSupportedException>(() => ((IList<EntityId>)old.AcceptedCommandIds).Add(new("cmd.fake")));
        foreach (var type in new[] { typeof(SessionSnapshot), typeof(EntityIdentitySnapshot), typeof(RngSnapshot), typeof(ActorSnapshot) })
            Assert.All(type.GetProperties(), property => Assert.False(property.CanWrite));
        Assert.True(session.SubmitCommand(Command()).Accepted);
        var current = session.CaptureSnapshot();
        Assert.Equal(0, old.StateRevision);
        Assert.Empty(old.AcceptedCommandIds);
        Assert.Equal("definition.alpha", old.Entities.Single(e => e.EntityId.Value == "entity.target").DefinitionId!.Value.Value);
        Assert.Equal(1, current.StateRevision);
        Assert.Equal("definition.beta", current.Entities.Single(e => e.EntityId.Value == "entity.target").DefinitionId!.Value.Value);
        Assert.NotSame(old.Entities, current.Entities);
        Assert.NotSame(old.RngState, current.RngState);
    }

    [Fact]
    public void SnapshotConstructorsCopyCollectionSources()
    {
        var entities = new List<EntityIdentitySnapshot> { new(new("entity.one"), null) };
        var definitions = new List<DefinitionId> { new("definition.one") };
        var actors = new List<ActorSnapshot> { new(Alpha, true) };
        var commands = new List<EntityId> { new("cmd.one") };
        var snapshot = new SessionSnapshot("scenario.session", "foundation-v2", Compatibility.ContentHash, 1, 1, 1,
            new RngSnapshot(SplitMix64Random.AlgorithmId, SplitMix64Random.AlgorithmVersion, 47), entities, definitions, actors, commands);
        entities.Clear(); definitions.Clear(); actors.Clear(); commands.Clear();
        Assert.Single(snapshot.Entities);
        Assert.Single(snapshot.DefinitionIds);
        Assert.Single(snapshot.Actors);
        Assert.Single(snapshot.AcceptedCommandIds);
    }

    [Fact]
    public void AcceptedMutationsIncrementRevisionExactlyOnceAndPreserveOldResults()
    {
        var probe = new Probe
        {
            Action = (state, _) =>
            {
                var random = SplitMix64Random.FromDto(state.RngState);
                random.NextUInt64();
                state.RngState = random.ToDto();
                var allocator = new EntityIdAllocator(state.NextEntitySequence);
                state.EntityIdentities.Add(new() { EntityId = allocator.Allocate("token").Value });
                state.NextEntitySequence = allocator.NextEntitySequence;
                state.EntityIdentities.Single(e => e.EntityId == "entity.target").DefinitionId = "definition.beta";
                return CommandErrorCode.None;
            }
        };
        var session = Session(probe);
        var first = session.SubmitCommand(Command("cmd.z"));
        Assert.True(first.Accepted);
        Assert.Equal(CommandErrorCode.None, first.ErrorCode);
        Assert.Equal(1, first.StateRevision);
        Assert.Equal(1, first.TurnNumber);
        Assert.True(session.SubmitCommand(Command("cmd.a", 1)).Accepted);
        var snapshot = session.CaptureSnapshot();
        Assert.Equal(2, snapshot.StateRevision);
        Assert.Equal(3UL, snapshot.NextEntitySequence);
        Assert.Equal(2, probe.Calls);
        Assert.Equal(new[] { "cmd.a", "cmd.z" }, snapshot.AcceptedCommandIds.Select(c => c.Value));
        Assert.Equal(1, first.StateRevision);
        Assert.Equal("cmd.z", first.CommandId);
        Assert.True(FoundationValidator.ValidateSave(session.ExportSave(), Definitions(), Compatibility).IsValid);
    }

    [Fact]
    public void RetainedStagingAndCommandReferencesCannotMutateCommittedOrCallerData()
    {
        var probe = new Probe();
        var session = Session(probe);
        var command = Command();
        var original = JsonSerializer.Serialize(command);
        Assert.True(session.SubmitCommand(command).Accepted);
        var before = StateJson(session);
        probe.RetainedState!.EntityIdentities.Clear();
        probe.RetainedState.RngState.State = 0;
        probe.RetainedState.StateRevision = 100;
        probe.RetainedState.NextEntitySequence = 100;
        probe.RetainedState.AcceptedCommandIds!.Clear();
        probe.RetainedCommand!.CommandId = "cmd.hijacked";
        probe.RetainedCommand.SetResearchTarget!.TargetDefinitionId = "definition.hijacked";
        Assert.Equal(before, StateJson(session));
        Assert.Equal(original, JsonSerializer.Serialize(command));
    }

    [Theory]
    [InlineData("null", CommandErrorCode.InvalidEnvelope)]
    [InlineData("missing-turn", CommandErrorCode.InvalidEnvelope)]
    [InlineData("missing-revision", CommandErrorCode.InvalidEnvelope)]
    [InlineData("bad-command-id", CommandErrorCode.InvalidCommandId)]
    [InlineData("null-command-id", CommandErrorCode.InvalidCommandId)]
    [InlineData("bad-actor-id", CommandErrorCode.InvalidActorId)]
    [InlineData("unknown-actor", CommandErrorCode.UnknownActor)]
    [InlineData("non-actor-entity", CommandErrorCode.UnknownActor)]
    [InlineData("not-controlled", CommandErrorCode.ActorNotControlled)]
    [InlineData("past-turn", CommandErrorCode.StaleTurn)]
    [InlineData("future-turn", CommandErrorCode.StaleTurn)]
    [InlineData("past-revision", CommandErrorCode.StaleRevision)]
    [InlineData("future-revision", CommandErrorCode.StaleRevision)]
    [InlineData("unknown-kind", CommandErrorCode.UnknownCommandKind)]
    [InlineData("missing-payload", CommandErrorCode.InvalidPayload)]
    [InlineData("wrong-payload", CommandErrorCode.InvalidPayload)]
    [InlineData("multiple-payloads", CommandErrorCode.InvalidPayload)]
    [InlineData("bad-definition-id", CommandErrorCode.InvalidPayload)]
    [InlineData("null-route", CommandErrorCode.InvalidPayload)]
    [InlineData("bad-route", CommandErrorCode.InvalidPayload)]
    public void CommonRejectionsAreStableAndLeaveEveryStateFieldUnchanged(string example, CommandErrorCode expected)
    {
        var probe = new Probe();
        var session = Session(probe);
        CommandEnvelope? command = Command();
        switch (example)
        {
            case "null": command = null; break;
            case "missing-turn": command.ExpectedTurn = null; break;
            case "missing-revision": command.ExpectedRevision = null; break;
            case "bad-command-id": command.CommandId = "Bad"; break;
            case "null-command-id": command.CommandId = null!; break;
            case "bad-actor-id": command.ActorCivilizationId = "Bad"; break;
            case "unknown-actor": command.ActorCivilizationId = "civ.unknown"; break;
            case "non-actor-entity": command.ActorCivilizationId = "entity.target"; break;
            case "not-controlled": command.ActorCivilizationId = Beta.Value; break;
            case "past-turn": command.ExpectedTurn = 0; break;
            case "future-turn": command.ExpectedTurn = 2; break;
            case "past-revision": command.ExpectedRevision = -1; break;
            case "future-revision": command.ExpectedRevision = 1; break;
            case "unknown-kind": command.Kind = (CommandKind)999; break;
            case "missing-payload": command.SetResearchTarget = null; break;
            case "wrong-payload": command.Kind = CommandKind.CancelProduction; break;
            case "multiple-payloads": command.CancelProduction = new() { ColonyId = "colony.one", QueueItemId = "queue.one" }; break;
            case "bad-definition-id": command.SetResearchTarget!.TargetDefinitionId = "definition.spawn.0000000000000001"; break;
            case "null-route":
            case "bad-route":
                command.Kind = CommandKind.SetFleetRoute; command.SetResearchTarget = null;
                command.SetFleetRoute = new() { FleetId = "fleet.one", RouteSystemIds = example == "null-route" ? null : new() { "Bad" } };
                break;
        }
        var before = StateJson(session);
        var result = session.SubmitCommand(command);
        Assert.False(result.Accepted);
        Assert.Equal(CommandStatus.Rejected, result.Status);
        Assert.Equal(expected, result.ErrorCode);
        Assert.Equal(1, result.TurnNumber);
        Assert.Equal(0, result.StateRevision);
        Assert.Equal(0, probe.Calls);
        Assert.Equal(before, StateJson(session));
    }

    [Fact]
    public void DuplicateDetectionPrecedesStaleRevisionAndIsGlobalAcrossActors()
    {
        var session = Session(new Probe(), controlBoth: true);
        var original = Command();
        Assert.True(session.SubmitCommand(original).Accepted);
        var before = StateJson(session);
        Assert.Equal(CommandErrorCode.DuplicateCommand, session.SubmitCommand(original).ErrorCode);
        Assert.Equal(CommandErrorCode.DuplicateCommand, session.SubmitCommand(Command(revision: 1, actor: Beta.Value)).ErrorCode);
        Assert.Equal(before, StateJson(session));
        Assert.True(session.SubmitCommand(Command("cmd.two", 1, Beta.Value)).Accepted);
        Assert.Equal(2, session.CaptureSnapshot().StateRevision);
    }

    [Fact]
    public void StaleRejectionDoesNotReserveTheIdAndCanBeRetriedAfterRefresh()
    {
        var session = Session(new Probe());
        Assert.True(session.SubmitCommand(Command()).Accepted);
        var stale = Command("cmd.two");
        var before = StateJson(session);
        Assert.Equal(CommandErrorCode.StaleRevision, session.SubmitCommand(stale).ErrorCode);
        Assert.Equal(before, StateJson(session));
        stale.ExpectedRevision = session.CaptureSnapshot().StateRevision;
        Assert.True(session.SubmitCommand(stale).Accepted);
        Assert.Equal(2, session.CaptureSnapshot().StateRevision);
    }

    [Fact]
    public void HandlerRejectionDiscardsRngAllocationIdentityAndBookkeepingChanges()
    {
        var probe = new Probe
        {
            Action = (state, _) =>
            {
                var random = SplitMix64Random.FromDto(state.RngState); random.NextUInt64(); state.RngState = random.ToDto();
                var allocator = new EntityIdAllocator(state.NextEntitySequence);
                state.EntityIdentities.Add(new() { EntityId = allocator.Allocate("token").Value });
                state.NextEntitySequence = allocator.NextEntitySequence;
                state.EntityIdentities[0].DefinitionId = "definition.beta";
                state.AcceptedCommandIds!.Add("cmd.fake"); state.StateRevision = 999; state.TurnNumber = 999;
                return CommandErrorCode.InvalidTarget;
            }
        };
        var session = Session(probe);
        var before = StateJson(session);
        var rejected = session.SubmitCommand(Command());
        Assert.Equal(CommandErrorCode.InvalidTarget, rejected.ErrorCode);
        Assert.Equal(0, rejected.StateRevision);
        Assert.Equal(before, StateJson(session));
        probe.RetainedState!.RngState.State = 0;
        Assert.Equal(before, StateJson(session));
        probe.Action = (_, _) => CommandErrorCode.None;
        Assert.True(session.SubmitCommand(Command()).Accepted);
    }

    [Theory]
    [InlineData("turn")]
    [InlineData("revision")]
    [InlineData("ledger")]
    [InlineData("invalid-state")]
    [InlineData("actor-removal")]
    [InlineData("bad-error-code")]
    [InlineData("exception")]
    public void ProgrammingFaultsDoNotPartiallyCommitAndTheSessionRecovers(string fault)
    {
        var probe = new Probe { Action = (state, _) =>
        {
            state.RngState.State = 0;
            switch (fault)
            {
                case "turn": state.TurnNumber++; break;
                case "revision": state.StateRevision++; break;
                case "ledger": state.AcceptedCommandIds!.Add("cmd.fake"); break;
                case "invalid-state": state.NextEntitySequence = 0; break;
                case "actor-removal": state.EntityIdentities.RemoveAll(e => e.EntityId == Alpha.Value); break;
                case "bad-error-code": return (CommandErrorCode)999;
                case "exception": throw new InvalidOperationException("Injected handler fault");
            }
            return CommandErrorCode.None;
        } };
        var session = Session(probe);
        var before = StateJson(session);
        Assert.Throws<InvalidOperationException>(() => session.SubmitCommand(Command()));
        Assert.Equal(before, StateJson(session));
        probe.Action = (_, _) => CommandErrorCode.None;
        Assert.True(session.SubmitCommand(Command()).Accepted);
    }

    [Fact]
    public void RevisionExhaustionRejectsBeforeCallingTheHandler()
    {
        var probe = new Probe();
        var session = Session(probe, Save(long.MaxValue));
        var before = StateJson(session);
        var result = session.SubmitCommand(Command(revision: long.MaxValue));
        Assert.Equal(CommandErrorCode.RevisionExhausted, result.ErrorCode);
        Assert.Equal(long.MaxValue, result.StateRevision);
        Assert.Equal(0, probe.Calls);
        Assert.Equal(before, StateJson(session));
        session = Session(probe, Save(long.MaxValue - 1));
        Assert.True(session.SubmitCommand(Command(revision: long.MaxValue - 1)).Accepted);
        Assert.Equal(long.MaxValue, session.CaptureSnapshot().StateRevision);
        Assert.Equal(CommandErrorCode.RevisionExhausted, session.SubmitCommand(Command("cmd.two", long.MaxValue)).ErrorCode);
    }

    [Fact]
    public void SaveReloadPreservesActiveTurnDeduplicationAndContinuation()
    {
        var session = Session(new Probe());
        Assert.True(session.SubmitCommand(Command()).Accepted);
        var save = JsonSerializer.Deserialize<SaveEnvelopeDto>(StateJson(session))!;
        var restored = Session(new Probe(), save);
        var before = StateJson(restored);
        Assert.Equal(CommandErrorCode.DuplicateCommand, restored.SubmitCommand(Command()).ErrorCode);
        Assert.Equal(before, StateJson(restored));
        Assert.True(session.SubmitCommand(Command("cmd.two", 1)).Accepted);
        Assert.True(restored.SubmitCommand(Command("cmd.two", 1)).Accepted);
        Assert.Equal(StateJson(session), StateJson(restored));
    }

    [Fact]
    public void PublicSessionsDoNotPretendToImplementAnyGameplayCommand()
    {
        var session = new SimulationSession(Save(), Definitions(), Compatibility, new[] { Alpha, Beta }, new[] { Alpha });
        var commands = new[]
        {
            Command(),
            new CommandEnvelope { CommandId = "cmd.enqueue", ActorCivilizationId = Alpha.Value, ExpectedTurn = 1, ExpectedRevision = 0,
                Kind = CommandKind.EnqueueProduction, EnqueueProduction = new() { ColonyId = "colony.one", DefinitionId = "definition.alpha" } },
            new CommandEnvelope { CommandId = "cmd.cancel", ActorCivilizationId = Alpha.Value, ExpectedTurn = 1, ExpectedRevision = 0,
                Kind = CommandKind.CancelProduction, CancelProduction = new() { ColonyId = "colony.one", QueueItemId = "queue.one" } },
            new CommandEnvelope { CommandId = "cmd.route", ActorCivilizationId = Alpha.Value, ExpectedTurn = 1, ExpectedRevision = 0,
                Kind = CommandKind.SetFleetRoute, SetFleetRoute = new() { FleetId = "fleet.one", RouteSystemIds = new() } }
        };
        var before = StateJson(session);
        foreach (var command in commands)
        {
            var loaded = JsonSerializer.Deserialize<CommandEnvelope>(JsonSerializer.Serialize(command))!;
            var result = session.SubmitCommand(loaded);
            Assert.Equal(CommandErrorCode.UnsupportedCommand, result.ErrorCode);
            var loadedResult = JsonSerializer.Deserialize<CommandResult>(JsonSerializer.Serialize(result))!;
            Assert.Equal(result.CommandId, loadedResult.CommandId);
            Assert.Equal(result.ErrorCode, loadedResult.ErrorCode);
            Assert.Equal(result.StateRevision, loadedResult.StateRevision);
            Assert.Equal(before, StateJson(session));
        }
    }

    [Fact]
    public void ClosedPayloadsAndMissingHeadersCannotBecomeValidByJsonDefaults()
    {
        var session = Session(new Probe());
        var json = JsonSerializer.Serialize(Command()).Replace("\"ExpectedRevision\":0,", "");
        Assert.Equal(CommandErrorCode.InvalidEnvelope, session.SubmitCommand(JsonSerializer.Deserialize<CommandEnvelope>(json)).ErrorCode);
        Assert.Equal(CommandErrorCode.UnknownCommandKind, session.SubmitCommand(new CommandEnvelope
            { CommandId = "cmd.unknown", ActorCivilizationId = Alpha.Value, ExpectedTurn = 1, ExpectedRevision = 0 }).ErrorCode);
    }

    [Fact]
    public void HandlerRegistryAndMutableRoutePayloadsAreAlsoDetached()
    {
        var probe = new Probe { Action = (_, command) =>
        {
            command.SetFleetRoute!.RouteSystemIds!.Add("system.changed");
            command.CommandId = "cmd.changed";
            return CommandErrorCode.None;
        } };
        var handlers = new Dictionary<CommandKind, ICommandHandler> { [CommandKind.SetFleetRoute] = probe };
        var session = new SimulationSession(Save(), Definitions(), Compatibility, new[] { Alpha }, new[] { Alpha }, handlers);
        handlers.Clear();
        var command = new CommandEnvelope { CommandId = "cmd.route", ActorCivilizationId = Alpha.Value, ExpectedTurn = 1, ExpectedRevision = 0,
            Kind = CommandKind.SetFleetRoute, SetFleetRoute = new() { FleetId = "fleet.one", RouteSystemIds = new() { "system.one" } } };
        var original = JsonSerializer.Serialize(command);
        var result = session.SubmitCommand(command);
        Assert.True(result.Accepted);
        Assert.Equal("cmd.route", result.CommandId);
        Assert.Equal(original, JsonSerializer.Serialize(command));
        Assert.Equal("cmd.route", Assert.Single(session.CaptureSnapshot().AcceptedCommandIds).Value);
    }

    [Fact]
    public void ReentrantCommandExecutionCannotCommitInsideAnOuterTransaction()
    {
        SimulationSession? session = null;
        var probe = new Probe { Action = (_, _) => { session!.SubmitCommand(Command("cmd.nested")); return CommandErrorCode.None; } };
        session = Session(probe);
        var before = StateJson(session);
        Assert.Throws<InvalidOperationException>(() => session.SubmitCommand(Command()));
        Assert.Equal(before, StateJson(session));
        probe.Action = (_, _) => CommandErrorCode.None;
        Assert.True(session.SubmitCommand(Command()).Accepted);
    }

    [Fact]
    public void ConcurrentSubmissionsCannotBothAcceptTheSameRevision()
    {
        var session = Session(new Probe());
        var results = new CommandResult[2];
        Parallel.Invoke(() => results[0] = session.SubmitCommand(Command("cmd.one")),
            () => results[1] = session.SubmitCommand(Command("cmd.two")));
        Assert.Single(results, r => r.Accepted);
        Assert.Equal(CommandErrorCode.StaleRevision, Assert.Single(results, r => !r.Accepted).ErrorCode);
        Assert.Equal(1, session.CaptureSnapshot().StateRevision);
        Assert.Single(session.CaptureSnapshot().AcceptedCommandIds);
    }

    [Fact]
    public void InvalidInitializationAndCorruptDeduplicationAreRejected()
    {
        Assert.Throws<ArgumentException>(() => new SimulationSession(Save(), Definitions(), Compatibility,
            new[] { new EntityId("civ.missing") }, new[] { Alpha }));
        Assert.Throws<ArgumentException>(() => new SimulationSession(Save(), Definitions(), Compatibility,
            new[] { Alpha }, new[] { Beta }));
        Assert.Throws<ArgumentException>(() => new SimulationSession(Save(), Definitions(), Compatibility,
            new[] { Alpha, Alpha }, new[] { Alpha }));
        var save = Save(2);
        save.GameState.AcceptedCommandIds = new() { "cmd.one", "cmd.one" };
        Assert.Contains(FoundationValidator.ValidateSave(save, Definitions(), Compatibility).Issues, e => e.Code == ValidationCode.DuplicateId);
        Assert.Throws<ArgumentException>(() => Session(save: save));
        save = Save(); save.GameState.AcceptedCommandIds = null;
        Assert.Throws<ArgumentException>(() => Session(save: save));
        save = Save(); save.GameState.AcceptedCommandIds = new() { "cmd.one" };
        Assert.Contains(FoundationValidator.ValidateSave(save, Definitions(), Compatibility).Issues, e => e.Code == ValidationCode.InvalidCounter);
        save = Save(1); save.GameState.AcceptedCommandIds = new() { "Bad" };
        Assert.Throws<ArgumentException>(() => Session(save: save));
        save = Save(); save.SaveFormatVersion = 1;
        Assert.Contains(FoundationValidator.ValidateSave(save, Definitions(), Compatibility).Issues, e => e.Code == ValidationCode.UnsupportedSaveFormat);
    }
}
