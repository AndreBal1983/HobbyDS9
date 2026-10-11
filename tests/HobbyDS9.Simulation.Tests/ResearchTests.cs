using System.Security.Cryptography;
using System.Text.Json;
using HobbyDS9.Contracts;
using HobbyDS9.Simulation;
using Xunit;

namespace HobbyDS9.Simulation.Tests;

public sealed class ResearchTests
{
    private static readonly SaveCompatibility Compatibility = new("scenario.research", "research-fixture-v1", new string('a', 64));
    private static EngineeringApplicationDefinitionDto Node(string id, long cost, params string[] prerequisites) =>
        new() { DefinitionId = id, DisplayName = id, BaseEngineeringCost = cost, ScienceRequirements = new(), PrerequisiteDefinitionIds = prerequisites.ToList() };
    private static DefinitionCatalogDto Definitions() => PopulationFixture.Content(NavigationFixture.Definitions(new()
    {
        ContentFormatVersion = FoundationVersions.ContentFormatVersion,
        DefinitionIds = new() { "civilization.fixture", "system.fixture", "economy.fixture", "engineering.gamma", "engineering.beta", "engineering.alt", "engineering.alpha" },
        Civilizations = new() { new() { DefinitionId = "civilization.fixture", DisplayName = "Fixture" } },
        StarSystems = new() { new() { DefinitionId = "system.fixture", DisplayName = "Fixture system" } },
        GalaxyEdges = new(),
        ShipDesigns = new(),
        EngineeringApplications = new() { Node("engineering.gamma", 6, "engineering.alt", "engineering.alpha"), Node("engineering.beta", 4, "engineering.alpha"),
            Node("engineering.alt", 5), Node("engineering.alpha", 9) }
    }, "node.z"));
    private static SaveEnvelopeDto Save() => PopulationFixture.Save(NavigationFixture.Save(new()
    {
        SaveFormatVersion = FoundationVersions.SaveFormatVersion,
        ScenarioId = Compatibility.ScenarioId,
        RulesVersion = Compatibility.RulesVersion,
        ContentHash = Compatibility.ContentHash,
        GameState = new()
        {
            Planets = new(),
            Displacements = new(),
            LastDeliveries = new(),
            PopulationStatistics = new(),
            PendingCombats = new(),
            ResolvedCombats = new(),
            Phase = SessionPhase.Planning, ResearchPaceFactor = 10000, LastResearchReceipts = new(),
            TurnNumber = 1,
            StateRevision = 0,
            NextEntitySequence = 1,
            RngState = new SplitMix64Random(47).ToDto(),
            AcceptedCommandIds = new(),
            Civilizations = new()
            {
                new() { CivilizationId = "civ.z", DefinitionId = "civilization.fixture", ControllerKind = ControllerKind.Human, Credits = 50, MatureEngineeringApplicationDefinitionIds = new() },
                new() { CivilizationId = "civ.a", DefinitionId = "civilization.fixture", ControllerKind = ControllerKind.AI, Credits = 50, MatureEngineeringApplicationDefinitionIds = new() }
            },
            StarSystems = new() { new() { SystemId = "node.z", DefinitionId = "system.fixture", OwnerCivilizationId = "civ.z" } },
            Ships = new(),
            Fleets = new(),
            Colonies = new() { PopulationFixture.Colony("colony.z", "node.z", "civ.z") },
            EntityIdentities = new()
            {
                new() { EntityId = "civ.z", DefinitionId = "civilization.fixture" }, new() { EntityId = "civ.a", DefinitionId = "civilization.fixture" },
                new() { EntityId = "node.z", DefinitionId = "system.fixture" }, new() { EntityId = "colony.z", DefinitionId = null }
            }
        }
    }, Definitions()));
    private static CivilizationStateDto Human(SaveEnvelopeDto save) => save.GameState.Civilizations!.Single(c => c.CivilizationId == "civ.z");
    private static CivilizationStateDto Human(SimulationSession session) => Human(session.ExportSave());
    private static SimulationSession Session(SaveEnvelopeDto? save = null, DefinitionCatalogDto? definitions = null,
        Func<StrategicTurnPhase, GameStateDto, bool>? probe = null)
    {
        var s = save ?? Save(); var d = definitions ?? Definitions();
        NavigationFixture.Definitions(d, s.GameState.StarSystems!.Select(n=>n.SystemId).ToArray()); NavigationFixture.State(s.GameState,d);
        return new(s,d,Compatibility,new Dictionary<CommandKind,ICommandHandler>(),probe);
    }
    private static string StateJson(SimulationSession session) => JsonSerializer.Serialize(session.ExportSave());
    private static CommandEnvelope Command(SimulationSession session, string target, string id = "cmd.target", string actor = "civ.z")
    {
        var snapshot = session.CaptureSnapshot();
        return new CommandEnvelope
        {
            CommandId = id,
            ActorCivilizationId = actor,
            ExpectedTurn = snapshot.TurnNumber,
            ExpectedRevision = snapshot.StateRevision,
            Kind = CommandKind.SetEngineeringTarget,
            SetEngineeringTarget = new() { TargetDefinitionId = target }
        };
    }
    private static TurnResult Advance(SimulationSession session)
    { var snapshot = session.CaptureSnapshot(); return session.AdvanceTurn(snapshot.TurnNumber, snapshot.StateRevision); }
    private static void AddColony(SaveEnvelopeDto save, string key, string? owner)
    {
        save.GameState.EntityIdentities.Add(new() { EntityId = "node." + key, DefinitionId = "system.fixture" });
        save.GameState.EntityIdentities.Add(new() { EntityId = "colony." + key, DefinitionId = null });
        save.GameState.StarSystems!.Add(new() { SystemId = "node." + key, DefinitionId = "system.fixture", OwnerCivilizationId = owner });
        var colony = PopulationFixture.Colony("colony." + key, "node." + key, owner);
        save.GameState.Colonies!.Add(colony); PopulationFixture.RegisterWorld(save.GameState, colony);
    }

    [Fact]
    public void AcceptedTargetChangesOnlyOwnTargetRevisionAndLedgerAndValidatesStaleAndDuplicateCommands()
    {
        var session = Session(); var expected = session.ExportSave(); var old = session.CaptureSnapshot();
        Human(expected).EngineeringTargetDefinitionId = "engineering.alpha"; expected.GameState.StateRevision = 1; expected.GameState.AcceptedCommandIds!.Add("cmd.target");
        var command = Command(session, "engineering.alpha"); var result = session.SubmitCommand(command);
        Assert.True(result.Accepted); Assert.Equal(1, result.StateRevision); Assert.Equal(CommandErrorCode.None, result.ErrorCode);
        Assert.Equal(JsonSerializer.Serialize(expected), StateJson(session));
        Assert.Null(old.Civilizations.Single(c => c.CivilizationId.Value == "civ.z").EngineeringTargetDefinitionId);
        var before = StateJson(session); Assert.Equal(CommandErrorCode.DuplicateCommand, session.SubmitCommand(command).ErrorCode);
        command.CommandId = "cmd.stale"; Assert.Equal(CommandErrorCode.StaleRevision, session.SubmitCommand(command).ErrorCode);
        Assert.Equal(before, StateJson(session));
    }

    [Theory]
    [InlineData("engineering.absent", CommandErrorCode.UnknownEngineeringApplication)]
    [InlineData("economy.fixture", CommandErrorCode.UnknownEngineeringApplication)]
    [InlineData("system.fixture", CommandErrorCode.UnknownEngineeringApplication)]
    [InlineData("engineering.beta", CommandErrorCode.EngineeringPrerequisiteMissing)]
    [InlineData("engineering.gamma", CommandErrorCode.EngineeringPrerequisiteMissing)]
    public void OrdinaryUnknownOrPrerequisiteErrorsRejectWithoutAnyMutation(string target, CommandErrorCode error)
    {
        var session = Session(); var before = StateJson(session);
        Assert.Equal(error, session.SubmitCommand(Command(session, target)).ErrorCode);
        Assert.Equal(before, StateJson(session));
        Assert.True(session.SubmitCommand(Command(session, "engineering.alpha")).Accepted); // Rejected IDs are retryable.
    }

    [Fact]
    public void AlreadyUnlockedAndSameTargetHaveExplicitCodesWithoutRevisionOrIdReservation()
    {
        var save = Save(); Human(save).MatureEngineeringApplicationDefinitionIds!.Add("engineering.alpha"); Human(save).EngineeringProgress!.Add(new(){DefinitionId="engineering.alpha",Progress=9}); var session = Session(save);
        var before = StateJson(session);
        Assert.Equal(CommandErrorCode.EngineeringAlreadyMature, session.SubmitCommand(Command(session, "engineering.alpha")).ErrorCode);
        Assert.Equal(before, StateJson(session)); Assert.True(session.SubmitCommand(Command(session, "engineering.beta")).Accepted);
        before = StateJson(session);
        Assert.Equal(CommandErrorCode.EngineeringTargetUnchanged, session.SubmitCommand(Command(session, "engineering.beta", "cmd.reusable")).ErrorCode);
        Assert.Equal(before, StateJson(session));
        Assert.True(session.SubmitCommand(Command(session, "engineering.alt", "cmd.reusable")).Accepted);
        Assert.Equal(2, session.CaptureSnapshot().StateRevision);
    }

    [Fact]
    public void RetargetingKeepsReserveAndOnlyCompletionSpendsPoints()
    {
        var save = Save(); Human(save).EngineeringReserve = 7; var session = Session(save);
        Assert.True(session.SubmitCommand(Command(session, "engineering.alpha")).Accepted);
        Assert.Equal(7, Human(session).EngineeringReserve);
        Assert.True(session.SubmitCommand(Command(session, "engineering.alt", "cmd.retarget")).Accepted);
        Assert.Equal(7, Human(session).EngineeringReserve); Assert.Equal("engineering.alt", Human(session).EngineeringTargetDefinitionId);
        Advance(session); Assert.Equal(5, Human(session).EngineeringReserve); Assert.Null(Human(session).EngineeringTargetDefinitionId);
        Assert.Equal(new[] { "engineering.alt" }, Human(session).MatureEngineeringApplicationDefinitionIds);
    }

    [Fact]
    public void RealDs9ApplicationStartsScienceLockedWithoutForcedHumanUnlock()
    {
        var scenario=VerticalSliceFixture.Scenario(false,weighted:true);
        var app=Assert.Single(scenario.Definitions.EngineeringApplications!);
        Assert.Equal(60,app.BaseEngineeringCost);
        Assert.Equal(2,app.ScienceRequirements!.Count);
        var session=VerticalSliceFixture.Session(scenario);
        var before=StateJson(session);
        Assert.Equal(CommandErrorCode.EngineeringScienceRequirementMissing,session.SubmitCommand(Command(session,app.DefinitionId,actor:"civ.federation")).ErrorCode);
        Assert.Equal(before,StateJson(session));
        foreach(var c in scenario.InitialState.Civilizations!)
        { Assert.Equal(7000,c.ScienceAllocationBps);Assert.Equal(6,c.Disciplines!.Count);Assert.All(c.Disciplines,d=>Assert.Equal(1,d.Level)); }
    }

    [Fact]
    public void OwnedOutputsAggregateNeutralOutputDoesNotAndNoTargetAccumulatesReserve()
    {
        var save = Save(); AddColony(save, "other", "civ.z"); AddColony(save, "neutral", null); var session = Session(save);
        Advance(session); Assert.Equal(6, Human(session).EngineeringReserve); Assert.Empty(Human(session).MatureEngineeringApplicationDefinitionIds!);
        Assert.Equal(0, session.ExportSave().GameState.Civilizations!.Single(c => c.CivilizationId == "civ.a").EngineeringReserve);
        Advance(session); Assert.Equal(12, Human(session).EngineeringReserve);
        Assert.True(session.SubmitCommand(Command(session, "engineering.alpha")).Accepted); Advance(session);
        Assert.Equal(9, Human(session).EngineeringReserve); Assert.Equal(new[] { "engineering.alpha" }, Human(session).MatureEngineeringApplicationDefinitionIds);
    }

    [Fact]
    public void SurplusUnlocksOneNodeOnlyAndPrerequisiteChainRequiresNextPlanningCommands()
    {
        var save = Save(); Human(save).EngineeringReserve = 20; var session = Session(save);
        Assert.True(session.SubmitCommand(Command(session, "engineering.alpha")).Accepted); Advance(session);
        Assert.Equal(14, Human(session).EngineeringReserve); Assert.Equal(new[] { "engineering.alpha" }, Human(session).MatureEngineeringApplicationDefinitionIds);
        Assert.Null(Human(session).EngineeringTargetDefinitionId);
        Advance(session); Assert.Equal(17, Human(session).EngineeringReserve); Assert.Single(Human(session).MatureEngineeringApplicationDefinitionIds!);
        Assert.True(session.SubmitCommand(Command(session, "engineering.beta")).Accepted); Advance(session);
        Assert.Equal(16, Human(session).EngineeringReserve); Assert.Equal(new[] { "engineering.alpha", "engineering.beta" }, Human(session).MatureEngineeringApplicationDefinitionIds);
        Assert.Equal(CommandErrorCode.EngineeringPrerequisiteMissing, session.SubmitCommand(Command(session, "engineering.gamma")).ErrorCode);
        Assert.True(session.SubmitCommand(Command(session, "engineering.alt")).Accepted); Advance(session);
        Assert.True(session.SubmitCommand(Command(session, "engineering.gamma")).Accepted); Advance(session);
        Assert.Equal(new[] { "engineering.alpha", "engineering.alt", "engineering.beta", "engineering.gamma" }, Human(session).MatureEngineeringApplicationDefinitionIds);
    }

    [Theory]
    [InlineData("missing-nodes", ValidationCode.MissingField)]
    [InlineData("null-node", ValidationCode.MissingField)]
    [InlineData("duplicate-node", ValidationCode.DuplicateId)]
    [InlineData("zero-cost", ValidationCode.InvalidResearchValue)]
    [InlineData("negative-cost", ValidationCode.InvalidResearchValue)]
    [InlineData("missing-label", ValidationCode.MissingField)]
    [InlineData("missing-prerequisite-list", ValidationCode.MissingField)]
    [InlineData("unknown-prerequisite", ValidationCode.MissingDefinition)]
    [InlineData("non-research-prerequisite", ValidationCode.MissingDefinition)]
    [InlineData("invalid-prerequisite", ValidationCode.InvalidId)]
    [InlineData("null-prerequisite", ValidationCode.InvalidId)]
    [InlineData("duplicate-prerequisite", ValidationCode.DuplicateId)]
    [InlineData("self-prerequisite", ValidationCode.SelfPrerequisite)]
    [InlineData("cycle", ValidationCode.ResearchCycle)]
    [InlineData("negative-reserve", ValidationCode.InvalidResearchValue)]
    [InlineData("missing-unlocks", ValidationCode.MissingField)]
    [InlineData("duplicate-unlock", ValidationCode.DuplicateId)]
    [InlineData("invalid-unlock", ValidationCode.InvalidId)]
    [InlineData("unknown-unlock", ValidationCode.MissingDefinition)]
    [InlineData("unknown-target", ValidationCode.MissingDefinition)]
    [InlineData("invalid-target", ValidationCode.InvalidId)]
    [InlineData("unlocked-target", ValidationCode.EngineeringAlreadyMature)]
    [InlineData("target-prerequisite", ValidationCode.EngineeringPrerequisiteMissing)]
    [InlineData("old-save", ValidationCode.UnsupportedSaveFormat)]
    [InlineData("old-content", ValidationCode.UnsupportedContentFormat)]
    public void InvalidResearchContentAndStateRejectWithoutChangingInputs(string error, ValidationCode expected)
    {
        var save = Save(); var defs = Definitions(); var node = defs.EngineeringApplications!.Single(n => n.DefinitionId == "engineering.alpha"); var human = Human(save);
        switch (error)
        {
            case "missing-nodes": defs.EngineeringApplications = null; break;
            case "null-node": defs.EngineeringApplications!.Add(null!); break;
            case "duplicate-node": defs.EngineeringApplications!.Add(node); break;
            case "zero-cost": node.BaseEngineeringCost = 0; break;
            case "negative-cost": node.BaseEngineeringCost = -1; break;
            case "missing-label": node.DisplayName = " "; break;
            case "missing-prerequisite-list": node.PrerequisiteDefinitionIds = null; break;
            case "unknown-prerequisite": node.PrerequisiteDefinitionIds!.Add("engineering.absent"); break;
            case "non-research-prerequisite": node.PrerequisiteDefinitionIds!.Add("economy.fixture"); break;
            case "invalid-prerequisite": node.PrerequisiteDefinitionIds!.Add("Bad"); break;
            case "null-prerequisite": node.PrerequisiteDefinitionIds!.Add(null!); break;
            case "duplicate-prerequisite": defs.EngineeringApplications!.Single(n => n.DefinitionId == "engineering.beta").PrerequisiteDefinitionIds!.Add("engineering.alpha"); break;
            case "self-prerequisite": node.PrerequisiteDefinitionIds!.Add(node.DefinitionId); break;
            case "cycle": node.PrerequisiteDefinitionIds!.Add("engineering.beta"); break;
            case "negative-reserve": human.EngineeringReserve = -1; break;
            case "missing-unlocks": human.MatureEngineeringApplicationDefinitionIds = null; break;
            case "duplicate-unlock": human.MatureEngineeringApplicationDefinitionIds = new() { "engineering.alpha", "engineering.alpha" }; break;
            case "invalid-unlock": human.MatureEngineeringApplicationDefinitionIds!.Add("Bad"); break;
            case "unknown-unlock": human.MatureEngineeringApplicationDefinitionIds!.Add("engineering.absent"); break;
            case "unknown-target": human.EngineeringTargetDefinitionId = "engineering.absent"; break;
            case "invalid-target": human.EngineeringTargetDefinitionId = "Bad"; break;
            case "unlocked-target": human.MatureEngineeringApplicationDefinitionIds!.Add("engineering.alpha"); human.EngineeringTargetDefinitionId = "engineering.alpha"; break;
            case "target-prerequisite": human.EngineeringTargetDefinitionId = "engineering.beta"; break;
            case "old-save": save.SaveFormatVersion = 5; break;
            case "old-content": defs.ContentFormatVersion = 2; break;
        }
        var before = JsonSerializer.Serialize(new { save, defs });
        Assert.Contains(FoundationValidator.ValidateSave(save, defs, Compatibility).Issues, i => i.Code == expected);
        Assert.Throws<ArgumentException>(() => Session(save, defs)); Assert.Equal(before, JsonSerializer.Serialize(new { save, defs }));
    }

    [Fact]
    public void DeepPrerequisiteGraphsValidateWithoutRecursiveTraversal()
    {
        var defs = Definitions(); defs.EngineeringApplications = new(); defs.DefinitionIds.RemoveAll(id => id.StartsWith("engineering.", StringComparison.Ordinal));
        for (var i = 0; i < 1200; i++)
        {
            var id = "engineering.chain-" + i.ToString(System.Globalization.CultureInfo.InvariantCulture);
            defs.EngineeringApplications!.Add(Node(id, 1, i == 0 ? Array.Empty<string>() : new[] { "engineering.chain-" + (i - 1).ToString(System.Globalization.CultureInfo.InvariantCulture) }));
            defs.DefinitionIds.Add(id);
        }
        defs.EngineeringApplications.Reverse(); Assert.True(FoundationValidator.ValidateSave(Save(), defs, Compatibility).IsValid);
        defs.EngineeringApplications.Last().PrerequisiteDefinitionIds!.Add("engineering.chain-1199");
        Assert.Contains(FoundationValidator.ValidateSave(Save(), defs, Compatibility).Issues, i => i.Code == ValidationCode.ResearchCycle);
    }

    [Fact]
    public void ReorderedDefinitionsPrerequisitesUnlocksAndStateHaveIdenticalContinuation()
    {
        var save = Save(); AddColony(save, "a", "civ.z"); var defs = Definitions();
        Human(save).MatureEngineeringApplicationDefinitionIds = new() { "engineering.alt", "engineering.alpha" }; Human(save).EngineeringTargetDefinitionId = "engineering.gamma"; Human(save).EngineeringProgress=new(){new(){DefinitionId="engineering.alt",Progress=5},new(){DefinitionId="engineering.alpha",Progress=9}};
        var baseline = Session(save, defs);
        defs.EngineeringApplications!.Reverse(); foreach (var node in defs.EngineeringApplications) node.PrerequisiteDefinitionIds!.Reverse();
        defs.DefinitionIds.Reverse(); save.GameState.Civilizations!.Reverse(); save.GameState.EntityIdentities.Reverse(); save.GameState.Colonies!.Reverse();
        Human(save).MatureEngineeringApplicationDefinitionIds!.Reverse(); var reordered = Session(save, defs);
        for (var i = 0; i < 10; i++)
        {
            Assert.Equal(JsonSerializer.Serialize(Advance(baseline)), JsonSerializer.Serialize(Advance(reordered)));
            Assert.Equal(StateJson(baseline), StateJson(reordered));
            Assert.Equal(JsonSerializer.Serialize(baseline.CaptureSnapshot()), JsonSerializer.Serialize(reordered.CaptureSnapshot()));
        }
        Assert.Equal(new[] { "engineering.alpha", "engineering.alt", "engineering.gamma" }, Human(baseline).MatureEngineeringApplicationDefinitionIds);
    }

    [Fact]
    public void MidResearchSaveReloadContinuesTargetsReserveAndDuplicateDetectionExactly()
    {
        var session = Session(); Assert.True(session.SubmitCommand(Command(session, "engineering.alpha")).Accepted); Advance(session);
        Assert.Equal(0, Human(session).EngineeringReserve);
        Assert.True(session.SubmitCommand(Command(session, "engineering.alt", "cmd.retarget")).Accepted);
        var command = Command(session, "engineering.alt", "cmd.retarget");
        var resumed = Session(JsonSerializer.Deserialize<SaveEnvelopeDto>(StateJson(session)), JsonSerializer.Deserialize<DefinitionCatalogDto>(JsonSerializer.Serialize(Definitions())));
        Assert.Equal(CommandErrorCode.DuplicateCommand, resumed.SubmitCommand(command).ErrorCode);
        for (var i = 0; i < 4; i++)
        { Assert.Equal(JsonSerializer.Serialize(Advance(session)), JsonSerializer.Serialize(Advance(resumed))); Assert.Equal(StateJson(session), StateJson(resumed)); }
        Assert.Equal(new[] { "engineering.alt" }, Human(resumed).MatureEngineeringApplicationDefinitionIds);
    }

    [Theory]
    [InlineData("reserve")]
    [InlineData("sum")]
    public void ResearchArithmeticOverflowRollsBackEconomyLedgerRngAllocationAndAllCampaignFacts(string error)
    {
        var save = Save(); var defs = Definitions(); AddColony(save, "a", "civ.a");
        save.GameState.StateRevision = 5; save.GameState.AcceptedCommandIds = new() { "cmd.old" }; save.GameState.NextEntitySequence = 17;
        if (error == "reserve") Human(save).EngineeringReserve = long.MaxValue;
        else { AddColony(save, "other", "civ.z"); defs.Infrastructure![0].Research = long.MaxValue / 2; }
        var session = Session(save, defs); var before = StateJson(session);
        Assert.Throws<OverflowException>(() => Advance(session)); Assert.Equal(before, StateJson(session));
        var staging = JsonSerializer.Deserialize<SaveEnvelopeDto>(JsonSerializer.Serialize(save))!.GameState;
        EconomyPhase.Execute(staging, defs); Assert.Equal(1005, PopulationRules.Population(staging.Colonies![0]));
        Assert.Throws<OverflowException>(() => ResearchPhase.Execute(staging, defs));
        // Raw DTO puts civ.z first, yet civ.a was processed before civ.z failed.
        Assert.Equal(error == "reserve" ? 3 : long.MaxValue / 2 + 2, staging.Civilizations!.Single(c => c.CivilizationId == "civ.a").EngineeringReserve);
        Assert.Equal(before, StateJson(session));
    }

    [Theory]
    [InlineData("false")]
    [InlineData("exception")]
    [InlineData("invariant")]
    public void ResearchAndLaterFailuresDiscardEarlierEconomicAndResearchChanges(string failure)
    {
        var save = Save(); Human(save).EngineeringReserve = 6; var failed = true; GameStateDto? retained = null;
        var session = Session(save, probe: (phase, s) =>
        {
            retained = s;
            if (phase == StrategicTurnPhase.Research && failed)
            {
                Assert.Equal(1005, PopulationRules.Population(s.Colonies![0]));
                if (failure == "false") return false;
                if (failure == "exception") throw new ApplicationException("Injected Research failure.");
            }
            if (phase == StrategicTurnPhase.Production && failed && failure == "invariant")
            { Assert.Contains("engineering.alpha", s.Civilizations!.Single(c => c.CivilizationId == "civ.z").MatureEngineeringApplicationDefinitionIds!); s.Civilizations![0].Credits++; }
            return true;
        });
        Assert.True(session.SubmitCommand(Command(session, "engineering.alpha")).Accepted); var before = StateJson(session);
        if (failure == "false") Assert.Equal(TurnErrorCode.PhaseFailed, Advance(session).ErrorCode);
        else if (failure == "exception") Assert.Throws<ApplicationException>(() => Advance(session));
        else Assert.Throws<InvalidOperationException>(() => Advance(session));
        Assert.Equal(before, StateJson(session)); retained!.Civilizations![0].EngineeringReserve = 0; retained.Civilizations[0].MatureEngineeringApplicationDefinitionIds!.Clear();
        Assert.Equal(before, StateJson(session)); failed = false; Assert.Equal(TurnStatus.Completed, Advance(session).Status);
        var after = StateJson(session); retained!.Civilizations![0].MatureEngineeringApplicationDefinitionIds!.Add("engineering.fake"); Assert.Equal(after, StateJson(session));
        Assert.Equal(0, Human(session).EngineeringReserve); Assert.Equal(new[] { "engineering.alpha" }, Human(session).MatureEngineeringApplicationDefinitionIds);
        Assert.Empty(session.CaptureSnapshot().AcceptedCommandIds);
    }

    public static IEnumerable<object[]> PhaseIsolation()
    {
        foreach (var phase in new[] { 0, 1, 3, 4, 5 })
            foreach (var field in new[] { "reserve", "target", "unlocks" }) yield return new object[] { phase, field };
    }
    [Theory]
    [MemberData(nameof(PhaseIsolation))]
    public void NonResearchPhasesCannotChangeResearchFields(int phase, string field)
    {
        var session = Session(probe: (current, s) =>
        {
            if ((int)current == phase)
            {
                var c = s.Civilizations!.Single(c => c.CivilizationId == "civ.z");
                if (field == "reserve") c.EngineeringReserve++;
                if (field == "target") c.EngineeringTargetDefinitionId = "engineering.alpha";
                if (field == "unlocks") c.MatureEngineeringApplicationDefinitionIds!.Add("engineering.alpha");
            }
            return true;
        });
        var before = StateJson(session); Assert.Throws<InvalidOperationException>(() => Advance(session)); Assert.Equal(before, StateJson(session));
    }

    [Fact]
    public void ResearchStateSnapshotsAndDefinitionPrerequisitesAreOwnedCopies()
    {
        var save = Save(); var defs = Definitions(); var session = Session(save, defs); var old = session.CaptureSnapshot();
        var alpha = defs.EngineeringApplications!.Single(n => n.DefinitionId == "engineering.alpha"); alpha.BaseEngineeringCost = 1; alpha.PrerequisiteDefinitionIds!.Add("engineering.gamma");
        defs.EngineeringApplications!.Clear(); Human(save).MatureEngineeringApplicationDefinitionIds!.Add("engineering.alpha"); Human(save).EngineeringReserve = 100;
        Assert.True(session.SubmitCommand(Command(session, "engineering.alpha")).Accepted); Advance(session); Assert.Equal(0, Human(session).EngineeringReserve);
        var snapshot = session.CaptureSnapshot(); var human = snapshot.Civilizations.Single(c => c.CivilizationId.Value == "civ.z");
        Assert.Equal(0, human.EngineeringReserve); Assert.Equal(new DefinitionId("engineering.alpha"), human.EngineeringTargetDefinitionId);
        Assert.Throws<NotSupportedException>(() => ((IList<DefinitionId>)human.MatureEngineeringApplicationDefinitionIds).Clear());
        Assert.Equal(0, old.Civilizations.Single(c => c.CivilizationId.Value == "civ.z").EngineeringReserve);
        var before = StateJson(session); var exported = session.ExportSave(); Human(exported).MatureEngineeringApplicationDefinitionIds!.Add("engineering.fake"); Human(exported).EngineeringTargetDefinitionId = null;
        Assert.Equal(before, StateJson(session)); Advance(session); Advance(session);
        Assert.Equal(new[] { "engineering.alpha" }, Human(session).MatureEngineeringApplicationDefinitionIds);
        Assert.Null(JsonSerializer.Deserialize<CivilizationStateDto>("{}")!.MatureEngineeringApplicationDefinitionIds);
        Assert.Null(JsonSerializer.Deserialize<EngineeringApplicationDefinitionDto>("{}")!.PrerequisiteDefinitionIds);
        Assert.Null(JsonSerializer.Deserialize<DefinitionCatalogDto>("{}")!.EngineeringApplications);
    }

    [Fact]
    public void PublicTargetCommandChecksControllerAndPhaseWithoutAutomaticAiTargets()
    {
        var session = Session(); var before = StateJson(session);
        Assert.Equal(CommandErrorCode.ActorNotControlled, session.SubmitCommand(Command(session, "engineering.alpha", actor: "civ.a")).ErrorCode);
        Assert.Equal(before, StateJson(session));
        var combatScenario = CampaignCombatFixture.Scenario(); session = VerticalSliceFixture.Session(combatScenario); session.AdvanceTurn(1, 0); before = StateJson(session);
        Assert.Equal(CommandErrorCode.CombatPending, session.SubmitCommand(Command(session, "engineering.advanced-starship-engineering", actor: "civ.federation")).ErrorCode); Assert.Equal(before, StateJson(session));
    }
}
