using System.Security.Cryptography;
using System.Text.Json;
using HobbyDS9.Contracts;
using HobbyDS9.Simulation;
using Xunit;

namespace HobbyDS9.Simulation.Tests;

public sealed class ResearchTests
{
    private static readonly SaveCompatibility Compatibility = new("scenario.research", "research-fixture-v1", new string('a', 64));
    private static ResearchNodeDefinitionDto Node(string id, long cost, params string[] prerequisites) =>
        new() { DefinitionId = id, DisplayName = id, ResearchCost = cost, PrerequisiteDefinitionIds = prerequisites.ToList() };
    private static DefinitionCatalogDto Definitions() => PopulationFixture.Content(NavigationFixture.Definitions(new()
    {
        ContentFormatVersion = FoundationVersions.ContentFormatVersion,
        DefinitionIds = new() { "civilization.fixture", "system.fixture", "economy.fixture", "research.gamma", "research.beta", "research.alt", "research.alpha" },
        Civilizations = new() { new() { DefinitionId = "civilization.fixture", DisplayName = "Fixture" } },
        StarSystems = new() { new() { DefinitionId = "system.fixture", DisplayName = "Fixture system" } },
        GalaxyEdges = new(),
        ShipDesigns = new(),
        ResearchNodes = new() { Node("research.gamma", 6, "research.alt", "research.alpha"), Node("research.beta", 4, "research.alpha"),
            Node("research.alt", 5), Node("research.alpha", 9) }
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
            Phase = SessionPhase.Planning,
            TurnNumber = 1,
            StateRevision = 0,
            NextEntitySequence = 1,
            RngState = new SplitMix64Random(47).ToDto(),
            AcceptedCommandIds = new(),
            Civilizations = new()
            {
                new() { CivilizationId = "civ.z", DefinitionId = "civilization.fixture", ControllerKind = ControllerKind.Human, Credits = 50, UnlockedResearchDefinitionIds = new() },
                new() { CivilizationId = "civ.a", DefinitionId = "civilization.fixture", ControllerKind = ControllerKind.AI, Credits = 50, UnlockedResearchDefinitionIds = new() }
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
            Kind = CommandKind.SetResearchTarget,
            SetResearchTarget = new() { TargetDefinitionId = target }
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
        Human(expected).ResearchTargetDefinitionId = "research.alpha"; expected.GameState.StateRevision = 1; expected.GameState.AcceptedCommandIds!.Add("cmd.target");
        var command = Command(session, "research.alpha"); var result = session.SubmitCommand(command);
        Assert.True(result.Accepted); Assert.Equal(1, result.StateRevision); Assert.Equal(CommandErrorCode.None, result.ErrorCode);
        Assert.Equal(JsonSerializer.Serialize(expected), StateJson(session));
        Assert.Null(old.Civilizations.Single(c => c.CivilizationId.Value == "civ.z").ResearchTargetDefinitionId);
        var before = StateJson(session); Assert.Equal(CommandErrorCode.DuplicateCommand, session.SubmitCommand(command).ErrorCode);
        command.CommandId = "cmd.stale"; Assert.Equal(CommandErrorCode.StaleRevision, session.SubmitCommand(command).ErrorCode);
        Assert.Equal(before, StateJson(session));
    }

    [Theory]
    [InlineData("research.absent", CommandErrorCode.UnknownResearchNode)]
    [InlineData("economy.fixture", CommandErrorCode.UnknownResearchNode)]
    [InlineData("system.fixture", CommandErrorCode.UnknownResearchNode)]
    [InlineData("research.beta", CommandErrorCode.ResearchPrerequisiteMissing)]
    [InlineData("research.gamma", CommandErrorCode.ResearchPrerequisiteMissing)]
    public void OrdinaryUnknownOrPrerequisiteErrorsRejectWithoutAnyMutation(string target, CommandErrorCode error)
    {
        var session = Session(); var before = StateJson(session);
        Assert.Equal(error, session.SubmitCommand(Command(session, target)).ErrorCode);
        Assert.Equal(before, StateJson(session));
        Assert.True(session.SubmitCommand(Command(session, "research.alpha")).Accepted); // Rejected IDs are retryable.
    }

    [Fact]
    public void AlreadyUnlockedAndSameTargetHaveExplicitCodesWithoutRevisionOrIdReservation()
    {
        var save = Save(); Human(save).UnlockedResearchDefinitionIds!.Add("research.alpha"); var session = Session(save);
        var before = StateJson(session);
        Assert.Equal(CommandErrorCode.ResearchAlreadyUnlocked, session.SubmitCommand(Command(session, "research.alpha")).ErrorCode);
        Assert.Equal(before, StateJson(session)); Assert.True(session.SubmitCommand(Command(session, "research.beta")).Accepted);
        before = StateJson(session);
        Assert.Equal(CommandErrorCode.ResearchTargetUnchanged, session.SubmitCommand(Command(session, "research.beta", "cmd.reusable")).ErrorCode);
        Assert.Equal(before, StateJson(session));
        Assert.True(session.SubmitCommand(Command(session, "research.alt", "cmd.reusable")).Accepted);
        Assert.Equal(2, session.CaptureSnapshot().StateRevision);
    }

    [Fact]
    public void RetargetingKeepsReserveAndOnlyCompletionSpendsPoints()
    {
        var save = Save(); Human(save).ResearchReserve = 7; var session = Session(save);
        Assert.True(session.SubmitCommand(Command(session, "research.alpha")).Accepted);
        Assert.Equal(7, Human(session).ResearchReserve);
        Assert.True(session.SubmitCommand(Command(session, "research.alt", "cmd.retarget")).Accepted);
        Assert.Equal(7, Human(session).ResearchReserve); Assert.Equal("research.alt", Human(session).ResearchTargetDefinitionId);
        Advance(session); Assert.Equal(5, Human(session).ResearchReserve); Assert.Null(Human(session).ResearchTargetDefinitionId);
        Assert.Equal(new[] { "research.alt" }, Human(session).UnlockedResearchDefinitionIds);
    }

    [Fact]
    public void RealDs9NodeCompletesInExactlyThreeTurnsWithNoNeutralOrAiPolicyContribution()
    {
        var bytes = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "scenarios", "ds9-slice", "scenario.json"));
        var scenario = JsonSerializer.Deserialize<ScenarioDto>(bytes)!;
        scenario.ContentHash = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
        Assert.True(FoundationValidator.ValidateScenario(scenario).IsValid);
        var node = Assert.Single(scenario.Definitions.ResearchNodes!); Assert.Equal(9, node.ResearchCost); Assert.Empty(node.PrerequisiteDefinitionIds!);
        Assert.All(scenario.InitialState.Civilizations!, c => { Assert.Equal(0, c.ResearchReserve); Assert.Null(c.ResearchTargetDefinitionId); Assert.Empty(c.UnlockedResearchDefinitionIds!); });
        // Keep this arithmetic-only test isolated from the now-implemented AI policy.
        // Autonomous controllers are covered by AiPlanningTests and LongRunAcceptanceTests.
        foreach (var c in scenario.InitialState.Civilizations!) c.ControllerKind = ControllerKind.Human;
        var session = new SimulationSession(new SaveEnvelopeDto
        {
            SaveFormatVersion = FoundationVersions.SaveFormatVersion,
            ScenarioId = scenario.ScenarioId,
            RulesVersion = scenario.RulesVersion,
            ContentHash = scenario.ContentHash,
            GameState = scenario.InitialState
        }, scenario.Definitions,
            new SaveCompatibility(scenario.ScenarioId, scenario.RulesVersion, scenario.ContentHash));
        Assert.True(session.SubmitCommand(Command(session, node.DefinitionId, actor: "civ.federation")).Accepted);
        for (var i = 1; i <= 3; i++)
        {
            var result = Advance(session); Assert.Empty(result.Events); Assert.Empty(result.CommandResults);
            var civilizations = session.ExportSave().GameState.Civilizations!; var federation = civilizations.Single(c => c.CivilizationId == "civ.federation");
            Assert.Equal(i < 3 ? i * 4 : 3, federation.ResearchReserve);
            Assert.Equal(i < 3 ? node.DefinitionId : null, federation.ResearchTargetDefinitionId);
            Assert.Equal(i == 3 ? new[] { node.DefinitionId } : Array.Empty<string>(), federation.UnlockedResearchDefinitionIds!);
            Assert.All(civilizations.Where(c => c.CivilizationId != "civ.federation"), c =>
            { Assert.Equal(i * 4, c.ResearchReserve); Assert.Empty(c.UnlockedResearchDefinitionIds!); Assert.Null(c.ResearchTargetDefinitionId); });
            Assert.Equal(47UL, session.CaptureSnapshot().RngState.State); Assert.True(session.CaptureSnapshot().NextEntitySequence >= 1UL);
        }
    }

    [Fact]
    public void OwnedOutputsAggregateNeutralOutputDoesNotAndNoTargetAccumulatesReserve()
    {
        var save = Save(); AddColony(save, "other", "civ.z"); AddColony(save, "neutral", null); var session = Session(save);
        Advance(session); Assert.Equal(6, Human(session).ResearchReserve); Assert.Empty(Human(session).UnlockedResearchDefinitionIds!);
        Assert.Equal(0, session.ExportSave().GameState.Civilizations!.Single(c => c.CivilizationId == "civ.a").ResearchReserve);
        Advance(session); Assert.Equal(12, Human(session).ResearchReserve);
        Assert.True(session.SubmitCommand(Command(session, "research.alpha")).Accepted); Advance(session);
        Assert.Equal(9, Human(session).ResearchReserve); Assert.Equal(new[] { "research.alpha" }, Human(session).UnlockedResearchDefinitionIds);
    }

    [Fact]
    public void SurplusUnlocksOneNodeOnlyAndPrerequisiteChainRequiresNextPlanningCommands()
    {
        var save = Save(); Human(save).ResearchReserve = 20; var session = Session(save);
        Assert.True(session.SubmitCommand(Command(session, "research.alpha")).Accepted); Advance(session);
        Assert.Equal(14, Human(session).ResearchReserve); Assert.Equal(new[] { "research.alpha" }, Human(session).UnlockedResearchDefinitionIds);
        Assert.Null(Human(session).ResearchTargetDefinitionId);
        Advance(session); Assert.Equal(17, Human(session).ResearchReserve); Assert.Single(Human(session).UnlockedResearchDefinitionIds!);
        Assert.True(session.SubmitCommand(Command(session, "research.beta")).Accepted); Advance(session);
        Assert.Equal(16, Human(session).ResearchReserve); Assert.Equal(new[] { "research.alpha", "research.beta" }, Human(session).UnlockedResearchDefinitionIds);
        Assert.Equal(CommandErrorCode.ResearchPrerequisiteMissing, session.SubmitCommand(Command(session, "research.gamma")).ErrorCode);
        Assert.True(session.SubmitCommand(Command(session, "research.alt")).Accepted); Advance(session);
        Assert.True(session.SubmitCommand(Command(session, "research.gamma")).Accepted); Advance(session);
        Assert.Equal(new[] { "research.alpha", "research.alt", "research.beta", "research.gamma" }, Human(session).UnlockedResearchDefinitionIds);
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
    [InlineData("unlocked-target", ValidationCode.ResearchAlreadyUnlocked)]
    [InlineData("target-prerequisite", ValidationCode.ResearchPrerequisiteMissing)]
    [InlineData("old-save", ValidationCode.UnsupportedSaveFormat)]
    [InlineData("old-content", ValidationCode.UnsupportedContentFormat)]
    public void InvalidResearchContentAndStateRejectWithoutChangingInputs(string error, ValidationCode expected)
    {
        var save = Save(); var defs = Definitions(); var node = defs.ResearchNodes!.Single(n => n.DefinitionId == "research.alpha"); var human = Human(save);
        switch (error)
        {
            case "missing-nodes": defs.ResearchNodes = null; break;
            case "null-node": defs.ResearchNodes!.Add(null!); break;
            case "duplicate-node": defs.ResearchNodes!.Add(node); break;
            case "zero-cost": node.ResearchCost = 0; break;
            case "negative-cost": node.ResearchCost = -1; break;
            case "missing-label": node.DisplayName = " "; break;
            case "missing-prerequisite-list": node.PrerequisiteDefinitionIds = null; break;
            case "unknown-prerequisite": node.PrerequisiteDefinitionIds!.Add("research.absent"); break;
            case "non-research-prerequisite": node.PrerequisiteDefinitionIds!.Add("economy.fixture"); break;
            case "invalid-prerequisite": node.PrerequisiteDefinitionIds!.Add("Bad"); break;
            case "null-prerequisite": node.PrerequisiteDefinitionIds!.Add(null!); break;
            case "duplicate-prerequisite": defs.ResearchNodes!.Single(n => n.DefinitionId == "research.beta").PrerequisiteDefinitionIds!.Add("research.alpha"); break;
            case "self-prerequisite": node.PrerequisiteDefinitionIds!.Add(node.DefinitionId); break;
            case "cycle": node.PrerequisiteDefinitionIds!.Add("research.beta"); break;
            case "negative-reserve": human.ResearchReserve = -1; break;
            case "missing-unlocks": human.UnlockedResearchDefinitionIds = null; break;
            case "duplicate-unlock": human.UnlockedResearchDefinitionIds = new() { "research.alpha", "research.alpha" }; break;
            case "invalid-unlock": human.UnlockedResearchDefinitionIds!.Add("Bad"); break;
            case "unknown-unlock": human.UnlockedResearchDefinitionIds!.Add("research.absent"); break;
            case "unknown-target": human.ResearchTargetDefinitionId = "research.absent"; break;
            case "invalid-target": human.ResearchTargetDefinitionId = "Bad"; break;
            case "unlocked-target": human.UnlockedResearchDefinitionIds!.Add("research.alpha"); human.ResearchTargetDefinitionId = "research.alpha"; break;
            case "target-prerequisite": human.ResearchTargetDefinitionId = "research.beta"; break;
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
        var defs = Definitions(); defs.ResearchNodes = new(); defs.DefinitionIds.RemoveAll(id => id.StartsWith("research.", StringComparison.Ordinal));
        for (var i = 0; i < 1200; i++)
        {
            var id = "research.chain-" + i.ToString(System.Globalization.CultureInfo.InvariantCulture);
            defs.ResearchNodes!.Add(Node(id, 1, i == 0 ? Array.Empty<string>() : new[] { "research.chain-" + (i - 1).ToString(System.Globalization.CultureInfo.InvariantCulture) }));
            defs.DefinitionIds.Add(id);
        }
        defs.ResearchNodes.Reverse(); Assert.True(FoundationValidator.ValidateSave(Save(), defs, Compatibility).IsValid);
        defs.ResearchNodes.Last().PrerequisiteDefinitionIds!.Add("research.chain-1199");
        Assert.Contains(FoundationValidator.ValidateSave(Save(), defs, Compatibility).Issues, i => i.Code == ValidationCode.ResearchCycle);
    }

    [Fact]
    public void ReorderedDefinitionsPrerequisitesUnlocksAndStateHaveIdenticalContinuation()
    {
        var save = Save(); AddColony(save, "a", "civ.z"); var defs = Definitions();
        Human(save).UnlockedResearchDefinitionIds = new() { "research.alt", "research.alpha" }; Human(save).ResearchTargetDefinitionId = "research.gamma";
        var baseline = Session(save, defs);
        defs.ResearchNodes!.Reverse(); foreach (var node in defs.ResearchNodes) node.PrerequisiteDefinitionIds!.Reverse();
        defs.DefinitionIds.Reverse(); save.GameState.Civilizations!.Reverse(); save.GameState.EntityIdentities.Reverse(); save.GameState.Colonies!.Reverse();
        Human(save).UnlockedResearchDefinitionIds!.Reverse(); var reordered = Session(save, defs);
        for (var i = 0; i < 10; i++)
        {
            Assert.Equal(JsonSerializer.Serialize(Advance(baseline)), JsonSerializer.Serialize(Advance(reordered)));
            Assert.Equal(StateJson(baseline), StateJson(reordered));
            Assert.Equal(JsonSerializer.Serialize(baseline.CaptureSnapshot()), JsonSerializer.Serialize(reordered.CaptureSnapshot()));
        }
        Assert.Equal(new[] { "research.alpha", "research.alt", "research.gamma" }, Human(baseline).UnlockedResearchDefinitionIds);
    }

    [Fact]
    public void MidResearchSaveReloadContinuesTargetsReserveAndDuplicateDetectionExactly()
    {
        var session = Session(); Assert.True(session.SubmitCommand(Command(session, "research.alpha")).Accepted); Advance(session);
        Assert.Equal(3, Human(session).ResearchReserve);
        Assert.True(session.SubmitCommand(Command(session, "research.alt", "cmd.retarget")).Accepted);
        var command = Command(session, "research.alt", "cmd.retarget");
        var resumed = Session(JsonSerializer.Deserialize<SaveEnvelopeDto>(StateJson(session)), JsonSerializer.Deserialize<DefinitionCatalogDto>(JsonSerializer.Serialize(Definitions())));
        Assert.Equal(CommandErrorCode.DuplicateCommand, resumed.SubmitCommand(command).ErrorCode);
        for (var i = 0; i < 4; i++)
        { Assert.Equal(JsonSerializer.Serialize(Advance(session)), JsonSerializer.Serialize(Advance(resumed))); Assert.Equal(StateJson(session), StateJson(resumed)); }
        Assert.Equal(new[] { "research.alt" }, Human(resumed).UnlockedResearchDefinitionIds);
    }

    [Theory]
    [InlineData("reserve")]
    [InlineData("sum")]
    public void ResearchArithmeticOverflowRollsBackEconomyLedgerRngAllocationAndAllCampaignFacts(string error)
    {
        var save = Save(); var defs = Definitions(); AddColony(save, "a", "civ.a");
        save.GameState.StateRevision = 5; save.GameState.AcceptedCommandIds = new() { "cmd.old" }; save.GameState.NextEntitySequence = 17;
        if (error == "reserve") Human(save).ResearchReserve = long.MaxValue;
        else { AddColony(save, "other", "civ.z"); defs.Infrastructure![0].Research = long.MaxValue / 2; }
        var session = Session(save, defs); var before = StateJson(session);
        Assert.Throws<OverflowException>(() => Advance(session)); Assert.Equal(before, StateJson(session));
        var staging = JsonSerializer.Deserialize<SaveEnvelopeDto>(JsonSerializer.Serialize(save))!.GameState;
        EconomyPhase.Execute(staging, defs); Assert.Equal(1005, PopulationRules.Population(staging.Colonies![0]));
        Assert.Throws<OverflowException>(() => ResearchPhase.Execute(staging, defs));
        // Raw DTO puts civ.z first, yet civ.a was processed before civ.z failed.
        Assert.Equal(error == "reserve" ? 3 : long.MaxValue / 2 + 2, staging.Civilizations!.Single(c => c.CivilizationId == "civ.a").ResearchReserve);
        Assert.Equal(before, StateJson(session));
    }

    [Theory]
    [InlineData("false")]
    [InlineData("exception")]
    [InlineData("invariant")]
    public void ResearchAndLaterFailuresDiscardEarlierEconomicAndResearchChanges(string failure)
    {
        var save = Save(); Human(save).ResearchReserve = 6; var failed = true; GameStateDto? retained = null;
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
            { Assert.Contains("research.alpha", s.Civilizations!.Single(c => c.CivilizationId == "civ.z").UnlockedResearchDefinitionIds!); s.Civilizations![0].Credits++; }
            return true;
        });
        Assert.True(session.SubmitCommand(Command(session, "research.alpha")).Accepted); var before = StateJson(session);
        if (failure == "false") Assert.Equal(TurnErrorCode.PhaseFailed, Advance(session).ErrorCode);
        else if (failure == "exception") Assert.Throws<ApplicationException>(() => Advance(session));
        else Assert.Throws<InvalidOperationException>(() => Advance(session));
        Assert.Equal(before, StateJson(session)); retained!.Civilizations![0].ResearchReserve = 0; retained.Civilizations[0].UnlockedResearchDefinitionIds!.Clear();
        Assert.Equal(before, StateJson(session)); failed = false; Assert.Equal(TurnStatus.Completed, Advance(session).Status);
        var after = StateJson(session); retained!.Civilizations![0].UnlockedResearchDefinitionIds!.Add("research.fake"); Assert.Equal(after, StateJson(session));
        Assert.Equal(0, Human(session).ResearchReserve); Assert.Equal(new[] { "research.alpha" }, Human(session).UnlockedResearchDefinitionIds);
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
                if (field == "reserve") c.ResearchReserve++;
                if (field == "target") c.ResearchTargetDefinitionId = "research.alpha";
                if (field == "unlocks") c.UnlockedResearchDefinitionIds!.Add("research.alpha");
            }
            return true;
        });
        var before = StateJson(session); Assert.Throws<InvalidOperationException>(() => Advance(session)); Assert.Equal(before, StateJson(session));
    }

    [Fact]
    public void ResearchStateSnapshotsAndDefinitionPrerequisitesAreOwnedCopies()
    {
        var save = Save(); var defs = Definitions(); var session = Session(save, defs); var old = session.CaptureSnapshot();
        var alpha = defs.ResearchNodes!.Single(n => n.DefinitionId == "research.alpha"); alpha.ResearchCost = 1; alpha.PrerequisiteDefinitionIds!.Add("research.gamma");
        defs.ResearchNodes!.Clear(); Human(save).UnlockedResearchDefinitionIds!.Add("research.alpha"); Human(save).ResearchReserve = 100;
        Assert.True(session.SubmitCommand(Command(session, "research.alpha")).Accepted); Advance(session); Assert.Equal(3, Human(session).ResearchReserve);
        var snapshot = session.CaptureSnapshot(); var human = snapshot.Civilizations.Single(c => c.CivilizationId.Value == "civ.z");
        Assert.Equal(3, human.ResearchReserve); Assert.Equal(new DefinitionId("research.alpha"), human.ResearchTargetDefinitionId);
        Assert.Throws<NotSupportedException>(() => ((IList<DefinitionId>)human.UnlockedResearchDefinitionIds).Clear());
        Assert.Equal(0, old.Civilizations.Single(c => c.CivilizationId.Value == "civ.z").ResearchReserve);
        var before = StateJson(session); var exported = session.ExportSave(); Human(exported).UnlockedResearchDefinitionIds!.Add("research.fake"); Human(exported).ResearchTargetDefinitionId = null;
        Assert.Equal(before, StateJson(session)); Advance(session); Advance(session);
        Assert.Equal(new[] { "research.alpha" }, Human(session).UnlockedResearchDefinitionIds);
        Assert.Null(JsonSerializer.Deserialize<CivilizationStateDto>("{}")!.UnlockedResearchDefinitionIds);
        Assert.Null(JsonSerializer.Deserialize<ResearchNodeDefinitionDto>("{}")!.PrerequisiteDefinitionIds);
        Assert.Null(JsonSerializer.Deserialize<DefinitionCatalogDto>("{}")!.ResearchNodes);
    }

    [Fact]
    public void PublicTargetCommandChecksControllerAndPhaseWithoutAutomaticAiTargets()
    {
        var session = Session(); var before = StateJson(session);
        Assert.Equal(CommandErrorCode.ActorNotControlled, session.SubmitCommand(Command(session, "research.alpha", actor: "civ.a")).ErrorCode);
        Assert.Equal(before, StateJson(session));
        var combatScenario = CampaignCombatFixture.Scenario(); session = VerticalSliceFixture.Session(combatScenario); session.AdvanceTurn(1, 0); before = StateJson(session);
        Assert.Equal(CommandErrorCode.CombatPending, session.SubmitCommand(Command(session, "research.advanced-engineering", actor: "civ.federation")).ErrorCode); Assert.Equal(before, StateJson(session));
    }
}
