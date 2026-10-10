using System.Security.Cryptography;
using System.Text.Json;
using HobbyDS9.Contracts;
using HobbyDS9.Simulation;
using Xunit;

namespace HobbyDS9.Simulation.Tests;

public sealed class EconomyTests
{
    private static readonly SaveCompatibility Compatibility = new("scenario.economy", "economy-fixture-v1", new string('a', 64));
    private static EconomicProfileDefinitionDto Profile() => new()
    {
        DefinitionId = "economy.fixture", DisplayName = "Fixture profile", FoodOutput = 12, PopulationPerFood = 100,
        MaterialOutput = 4, PopulationPerCredit = 100, BaseIndustry = 4, PopulationPerIndustry = 250,
        GrowthPerTurn = 50, StarvationLossPerTurn = 50, ResearchOutput = 3
    };
    private static DefinitionCatalogDto Definitions() => new()
    {
        ContentFormatVersion = FoundationVersions.ContentFormatVersion, ShipDesigns = new(), ResearchNodes = new(),
        DefinitionIds = new() { "civilization.fixture", "system.fixture", "economy.fixture" },
        Civilizations = new() { new() { DefinitionId = "civilization.fixture", DisplayName = "Fixture" } },
        StarSystems = new() { new() { DefinitionId = "system.fixture", DisplayName = "Fixture system" } },
        GalaxyEdges = new(), EconomicProfiles = new() { Profile() }
    };
    private static SaveEnvelopeDto Save() => new()
    {
        SaveFormatVersion = FoundationVersions.SaveFormatVersion, ScenarioId = Compatibility.ScenarioId,
        RulesVersion = Compatibility.RulesVersion, ContentHash = Compatibility.ContentHash,
        GameState = new()
        {
            PendingCombats = new(), ResolvedCombats = new(), Phase = SessionPhase.Planning, TurnNumber = 1, StateRevision = 0, NextEntitySequence = 1,
            RngState = new SplitMix64Random(47).ToDto(), AcceptedCommandIds = new(),
            Civilizations = new() { new() { CivilizationId = "civ.fixture", DefinitionId = "civilization.fixture", UnlockedResearchDefinitionIds = new(), ControllerKind = ControllerKind.Human, Credits = 50 } },
            StarSystems = new() { new() { SystemId = "node.z", DefinitionId = "system.fixture", OwnerCivilizationId = "civ.fixture" } },
            Ships = new(), Fleets = new(), Colonies = new() { new() { ColonyId = "colony.z", SystemId = "node.z", OwnerCivilizationId = "civ.fixture", ProductionQueue = new(), EconomicProfileDefinitionId = "economy.fixture",
                Population = 1000, PopulationCapacity = 2000, Food = 20, Materials = 10 } },
            EntityIdentities = new()
            {
                new() { EntityId = "civ.fixture", DefinitionId = "civilization.fixture" },
                new() { EntityId = "node.z", DefinitionId = "system.fixture" },
                new() { EntityId = "colony.z", DefinitionId = "economy.fixture" }
            }
        }
    };
    private static SimulationSession Session(SaveEnvelopeDto? save = null, DefinitionCatalogDto? definitions = null,
        Func<StrategicTurnPhase, GameStateDto, bool>? probe = null) => new(save ?? Save(), definitions ?? Definitions(), Compatibility,
            new Dictionary<CommandKind, ICommandHandler>(), probe);
    private static string StateJson(SimulationSession session) => JsonSerializer.Serialize(session.ExportSave());
    private static void AddSecondColony(SaveEnvelopeDto save, bool owned = false)
    {
        var owner = owned ? "civ.fixture" : null;
        save.GameState.EntityIdentities.Add(new() { EntityId = "node.a", DefinitionId = "system.fixture" });
        save.GameState.EntityIdentities.Add(new() { EntityId = "colony.a", DefinitionId = "economy.fixture" });
        save.GameState.StarSystems!.Add(new() { SystemId = "node.a", DefinitionId = "system.fixture", OwnerCivilizationId = owner });
        save.GameState.Colonies!.Add(new() { ColonyId = "colony.a", SystemId = "node.a", OwnerCivilizationId = owner,
            ProductionQueue = new(), EconomicProfileDefinitionId = "economy.fixture", Population = 1000, PopulationCapacity = 2000, Food = 20, Materials = 10 });
    }

    [Fact]
    public void Ds9ContentHasExactStartingEconomyAndOneTurnResultsIncludingNeutralBajor()
    {
        var bytes = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "scenarios", "ds9-slice", "scenario.json"));
        var scenario = JsonSerializer.Deserialize<ScenarioDto>(bytes)!;
        scenario.ContentHash = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
        Assert.True(FoundationValidator.ValidateScenario(scenario).IsValid);
        Assert.Equal(4, scenario.Definitions.ContentFormatVersion);
        var profile = Assert.Single(scenario.Definitions.EconomicProfiles!);
        Assert.Equal(new long[] { 12, 100, 4, 100, 4, 250, 50, 50, 3 }, new[] { profile.FoodOutput, profile.PopulationPerFood,
            profile.MaterialOutput, profile.PopulationPerCredit, profile.BaseIndustry, profile.PopulationPerIndustry,
            profile.GrowthPerTurn, profile.StarvationLossPerTurn, profile.ResearchOutput });
        Assert.Equal(new[] { "colony.bajor", "colony.cardassia", "colony.founders-homeworld", "colony.sol" },
            scenario.InitialState.Colonies!.Select(c => c.ColonyId).OrderBy(c => c, StringComparer.Ordinal));
        Assert.All(scenario.InitialState.Civilizations!, c => Assert.Equal(50, c.Credits));
        Assert.All(scenario.InitialState.Colonies!, c =>
        { Assert.Equal(1000, c.Population); Assert.Equal(2000, c.PopulationCapacity); Assert.Equal(20, c.Food); Assert.Equal(10, c.Materials); });
        // Keep this arithmetic-only test isolated from the now-implemented AI policy.
        // Autonomous controllers are covered by AiPlanningTests and LongRunAcceptanceTests.
        foreach (var c in scenario.InitialState.Civilizations!) c.ControllerKind = ControllerKind.Human;
        var session = new SimulationSession(new SaveEnvelopeDto { SaveFormatVersion = FoundationVersions.SaveFormatVersion,
            ScenarioId = scenario.ScenarioId, RulesVersion = scenario.RulesVersion, ContentHash = scenario.ContentHash, GameState = scenario.InitialState },
            scenario.Definitions, new SaveCompatibility(scenario.ScenarioId, scenario.RulesVersion, scenario.ContentHash));
        var result = session.AdvanceTurn(1, 0); var state = session.ExportSave().GameState;
        Assert.Equal(TurnStatus.Completed, result.Status); Assert.Empty(result.Events); Assert.Empty(result.CommandResults);
        Assert.Equal(2, state.TurnNumber); Assert.Equal(1, state.StateRevision);
        Assert.All(state.Colonies!, c => { Assert.Equal(1050, c.Population); Assert.Equal(22, c.Food); Assert.Equal(14, c.Materials); });
        Assert.All(state.Civilizations!, c => Assert.Equal(60, c.Credits)); // Exactly three owned contributions, not four.
        Assert.Null(state.Colonies!.Single(c => c.ColonyId == "colony.bajor").OwnerCivilizationId);
        Assert.Equal("civ.federation", state.Colonies!.Single(c => c.ColonyId == "colony.sol").OwnerCivilizationId);
        Assert.Equal("civ.cardassian", state.Colonies!.Single(c => c.ColonyId == "colony.cardassia").OwnerCivilizationId);
        Assert.Equal("civ.dominion", state.Colonies!.Single(c => c.ColonyId == "colony.founders-homeworld").OwnerCivilizationId);
        Assert.Equal(47UL, state.RngState.State); Assert.Equal(1UL, state.NextEntitySequence);
    }

    [Theory]
    [InlineData(1000, 2000, 20, 12, 50, 50, 1050, 22, 60)]
    [InlineData(1980, 2000, 40, 12, 50, 50, 2000, 32, 69)]
    [InlineData(2000, 2000, 40, 12, 50, 50, 2000, 32, 70)]
    [InlineData(1000, 2000, 10, 0, 50, 50, 1050, 0, 60)]
    [InlineData(1001, 2000, 11, 0, 50, 50, 1051, 0, 60)]
    [InlineData(1001, 2000, 0, 10, 50, 50, 951, 0, 60)]
    [InlineData(1000, 2000, 5, 0, 50, 50, 950, 0, 60)]
    [InlineData(3, 2000, 0, 0, 50, 50, 1, 0, 50)]
    [InlineData(1, 2000, 0, 0, 50, 50, 1, 0, 50)]
    [InlineData(1000, 2000, 20, 12, 0, 50, 1000, 22, 60)]
    [InlineData(1000, 2000, 0, 0, 50, 0, 1000, 0, 60)]
    public void FoodDemandGrowthStarvationAndIncomeUseExactIntegerRules(long population, long capacity, long food,
        long output, long growth, long loss, long expectedPopulation, long expectedFood, long expectedCredits)
    {
        var save = Save(); var c = save.GameState.Colonies![0]; c.Population = population; c.PopulationCapacity = capacity; c.Food = food;
        var definitions = Definitions(); var p = definitions.EconomicProfiles![0]; p.FoodOutput = output; p.GrowthPerTurn = growth; p.StarvationLossPerTurn = loss;
        var session = Session(save, definitions); session.AdvanceTurn(1, 0);
        var state = session.ExportSave().GameState; c = Assert.Single(state.Colonies!);
        Assert.Equal(expectedPopulation, c.Population); Assert.Equal(expectedFood, c.Food); Assert.Equal(14, c.Materials);
        Assert.Equal(expectedCredits, state.Civilizations![0].Credits);
    }

    [Fact]
    public void NeutralColonyHasLocalEconomyAndMultipleOwnedColoniesAggregateCredits()
    {
        var save = Save(); AddSecondColony(save); var session = Session(save); session.AdvanceTurn(1, 0);
        var state = session.ExportSave().GameState;
        Assert.Equal(60, state.Civilizations![0].Credits); Assert.Equal(2, state.Colonies!.Count);
        Assert.All(state.Colonies, c => { Assert.Equal(1050, c.Population); Assert.Equal(22, c.Food); Assert.Equal(14, c.Materials); });
        save = Save(); AddSecondColony(save, owned: true); session = Session(save); session.AdvanceTurn(1, 0);
        Assert.Equal(70, session.ExportSave().GameState.Civilizations![0].Credits);
    }

    [Fact]
    public void ConsecutiveTurnsHaveKnownGrowthAndStarvationValuesAndResumeExactly()
    {
        var populations = new long[] { 1050, 1100, 1150, 1200, 1250, 1300, 1350, 1400, 1450, 1500, 1550, 1600, 1650, 1600, 1550 };
        var food = new long[] { 22, 23, 24, 24, 24, 23, 22, 20, 18, 15, 12, 8, 4, 0, 0 };
        var credits = new long[] { 60, 70, 81, 92, 104, 116, 129, 142, 156, 170, 185, 200, 216, 232, 248 };
        var session = Session(); var resumed = Session(JsonSerializer.Deserialize<SaveEnvelopeDto>(StateJson(session)));
        for (var i = 0; i < populations.Length; i++)
        {
            var result = session.AdvanceTurn(i + 1, i); var resumedResult = resumed.AdvanceTurn(i + 1, i);
            Assert.Equal(JsonSerializer.Serialize(result), JsonSerializer.Serialize(resumedResult));
            var state = session.ExportSave().GameState; var c = Assert.Single(state.Colonies!);
            Assert.Equal(populations[i], c.Population); Assert.Equal(food[i], c.Food); Assert.Equal(10 + (i + 1) * 4, c.Materials);
            Assert.Equal(credits[i], state.Civilizations![0].Credits); Assert.Equal(i + 2, state.TurnNumber); Assert.Equal(i + 1, state.StateRevision);
            Assert.Equal(47UL, state.RngState.State); Assert.Equal(1UL, state.NextEntitySequence); Assert.Empty(result.Events);
            Assert.Equal(StateJson(session), StateJson(resumed));
            resumed = Session(JsonSerializer.Deserialize<SaveEnvelopeDto>(StateJson(resumed)));
        }
    }

    [Fact]
    public void ReorderedColoniesDefinitionsAndEquivalentInputGiveIdenticalEconomyResults()
    {
        var save = Save(); AddSecondColony(save, owned: true); var defs = Definitions();
        var other = Profile(); other.DefinitionId = "economy.other"; other.MaterialOutput = 7;
        defs.EconomicProfiles!.Add(other); defs.DefinitionIds.Add(other.DefinitionId);
        var c = save.GameState.Colonies!.Single(c => c.ColonyId == "colony.a"); c.EconomicProfileDefinitionId = other.DefinitionId;
        save.GameState.EntityIdentities.Single(e => e.EntityId == c.ColonyId).DefinitionId = other.DefinitionId;
        var baseline = Session(save, defs);
        save.GameState.Colonies!.Reverse(); save.GameState.StarSystems!.Reverse(); save.GameState.EntityIdentities.Reverse();
        defs.DefinitionIds.Reverse(); defs.EconomicProfiles.Reverse(); var reordered = Session(save, defs);
        for (var i = 0; i < 20; i++)
        {
            Assert.Equal(JsonSerializer.Serialize(baseline.AdvanceTurn(i + 1, i)), JsonSerializer.Serialize(reordered.AdvanceTurn(i + 1, i)));
            Assert.Equal(StateJson(baseline), StateJson(reordered));
            Assert.Equal(JsonSerializer.Serialize(baseline.CaptureSnapshot()), JsonSerializer.Serialize(reordered.CaptureSnapshot()));
        }
    }

    public static IEnumerable<object[]> InvalidNumbers()
    {
        foreach (var name in new[] { "FoodOutput", "MaterialOutput", "BaseIndustry", "GrowthPerTurn", "StarvationLossPerTurn", "ResearchOutput" })
            yield return new object[] { "profile", name, -1L };
        foreach (var name in new[] { "PopulationPerFood", "PopulationPerCredit", "PopulationPerIndustry" })
        foreach (var value in new long[] { 0, -1 }) yield return new object[] { "profile", name, value };
        foreach (var name in new[] { "Population", "PopulationCapacity" })
        foreach (var value in new long[] { 0, -1 }) yield return new object[] { "colony", name, value };
        yield return new object[] { "colony", "Population", 2001L };
        foreach (var name in new[] { "Food", "Materials" }) yield return new object[] { "colony", name, -1L };
        yield return new object[] { "civilization", "Credits", -1L };
    }
    [Theory]
    [MemberData(nameof(InvalidNumbers))]
    public void InvalidEconomicValuesAreRejectedWithoutMutation(string scope, string field, long value)
    {
        var save = Save(); var defs = Definitions();
        object target = scope == "profile" ? defs.EconomicProfiles![0] : scope == "colony" ? save.GameState.Colonies![0] : save.GameState.Civilizations![0];
        target.GetType().GetProperty(field)!.SetValue(target, value);
        var before = JsonSerializer.Serialize(new { save, defs });
        Assert.Contains(FoundationValidator.ValidateSave(save, defs, Compatibility).Issues, i => i.Code == ValidationCode.InvalidEconomicValue && i.Path.EndsWith("." + field, StringComparison.Ordinal));
        Assert.Throws<ArgumentException>(() => Session(save, defs));
        Assert.Equal(before, JsonSerializer.Serialize(new { save, defs }));
    }

    [Theory]
    [InlineData("owner", ValidationCode.MissingCivilization)]
    [InlineData("system", ValidationCode.MissingSystem)]
    [InlineData("profile", ValidationCode.MissingDefinition)]
    [InlineData("wrong-profile-kind", ValidationCode.MissingDefinition)]
    [InlineData("duplicate-colony", ValidationCode.DuplicateId)]
    [InlineData("duplicate-system", ValidationCode.DuplicateColonySystem)]
    [InlineData("owned-mismatch", ValidationCode.OwnershipMismatch)]
    [InlineData("neutral-mismatch", ValidationCode.OwnershipMismatch)]
    [InlineData("null-colony", ValidationCode.MissingField)]
    [InlineData("missing-colonies", ValidationCode.MissingField)]
    [InlineData("missing-profiles", ValidationCode.MissingField)]
    [InlineData("null-profile", ValidationCode.MissingField)]
    [InlineData("duplicate-profile", ValidationCode.DuplicateId)]
    [InlineData("missing-header", ValidationCode.MissingEntity)]
    [InlineData("header-mismatch", ValidationCode.IdentityMismatch)]
    [InlineData("bad-colony-id", ValidationCode.InvalidId)]
    [InlineData("bad-owner-id", ValidationCode.InvalidId)]
    [InlineData("bad-system-id", ValidationCode.InvalidId)]
    [InlineData("bad-profile-id", ValidationCode.InvalidId)]
    [InlineData("missing-profile-label", ValidationCode.MissingField)]
    [InlineData("old-save", ValidationCode.UnsupportedSaveFormat)]
    [InlineData("old-content", ValidationCode.UnsupportedContentFormat)]
    public void IngressValidatesReferencesOwnershipUniquenessAndSchema(string error, ValidationCode expected)
    {
        var save = Save(); var defs = Definitions(); var c = save.GameState.Colonies![0];
        switch (error)
        {
            case "owner": c.OwnerCivilizationId = "civ.absent"; break;
            case "system": c.SystemId = "node.absent"; break;
            case "profile": c.EconomicProfileDefinitionId = "economy.absent"; break;
            case "wrong-profile-kind": c.EconomicProfileDefinitionId = "system.fixture"; break;
            case "duplicate-colony": save.GameState.Colonies.Add(c); break;
            case "duplicate-system": AddSecondColony(save); save.GameState.Colonies[1].SystemId = c.SystemId; break;
            case "owned-mismatch": save.GameState.StarSystems![0].OwnerCivilizationId = null; break;
            case "neutral-mismatch": c.OwnerCivilizationId = null; break;
            case "null-colony": save.GameState.Colonies.Add(null!); break;
            case "missing-colonies": save.GameState.Colonies = null; break;
            case "missing-profiles": defs.EconomicProfiles = null; break;
            case "null-profile": defs.EconomicProfiles!.Add(null!); break;
            case "duplicate-profile": defs.EconomicProfiles!.Add(defs.EconomicProfiles[0]); break;
            case "missing-header": save.GameState.EntityIdentities.RemoveAll(e => e.EntityId == c.ColonyId); break;
            case "header-mismatch": save.GameState.EntityIdentities.Single(e => e.EntityId == c.ColonyId).DefinitionId = null; break;
            case "bad-colony-id": c.ColonyId = "Bad"; break;
            case "bad-owner-id": c.OwnerCivilizationId = "Bad"; break;
            case "bad-system-id": c.SystemId = "Bad"; break;
            case "bad-profile-id": c.EconomicProfileDefinitionId = "Bad"; break;
            case "missing-profile-label": defs.EconomicProfiles![0].DisplayName = ""; break;
            case "old-save": save.SaveFormatVersion = 4; break;
            case "old-content": defs.ContentFormatVersion = 1; break;
        }
        var before = JsonSerializer.Serialize(new { save, defs });
        Assert.Contains(FoundationValidator.ValidateSave(save, defs, Compatibility).Issues, i => i.Code == expected);
        Assert.Throws<ArgumentException>(() => Session(save, defs)); Assert.Equal(before, JsonSerializer.Serialize(new { save, defs }));
    }

    [Theory]
    [InlineData("food")]
    [InlineData("materials")]
    [InlineData("credits")]
    public void OverflowAfterEarlierColonyWorkRollsBackTheEntireTurn(string field)
    {
        var save = Save(); AddSecondColony(save);
        save.GameState.TurnNumber = 7; save.GameState.StateRevision = 5; save.GameState.AcceptedCommandIds = new() { "cmd.z", "cmd.a" };
        save.GameState.NextEntitySequence = 17; var colony = save.GameState.Colonies![0];
        if (field == "food") colony.Food = long.MaxValue;
        if (field == "materials") colony.Materials = long.MaxValue;
        if (field == "credits") save.GameState.Civilizations![0].Credits = long.MaxValue;
        var session = Session(save); var before = StateJson(session);
        Assert.Throws<OverflowException>(() => session.AdvanceTurn(7, 5)); Assert.Equal(before, StateJson(session));
        var staging = JsonSerializer.Deserialize<SaveEnvelopeDto>(JsonSerializer.Serialize(save))!.GameState;
        Assert.Throws<OverflowException>(() => EconomyPhase.Execute(staging, Definitions()));
        // Input colony.z appears first, but canonical colony.a ran before the failure.
        Assert.Equal(1050, staging.Colonies!.Single(c => c.ColonyId == "colony.a").Population);
        Assert.Equal(22, staging.Colonies!.Single(c => c.ColonyId == "colony.a").Food);
        Assert.Equal(before, StateJson(session));
    }

    [Fact]
    public void ClampAndCeilingCalculationsDoNotOverflowAtValidInt64Boundaries()
    {
        var save = Save(); var defs = Definitions(); var c = save.GameState.Colonies![0]; var p = defs.EconomicProfiles![0];
        c.Population = long.MaxValue - 1; c.PopulationCapacity = long.MaxValue; c.Food = 1; c.Materials = 0;
        p.FoodOutput = 0; p.MaterialOutput = 0; p.GrowthPerTurn = long.MaxValue; p.PopulationPerFood = long.MaxValue; p.PopulationPerCredit = long.MaxValue;
        var session = Session(save, defs); session.AdvanceTurn(1, 0); Assert.Equal(long.MaxValue, session.ExportSave().GameState.Colonies![0].Population);
        c.Population = long.MaxValue; c.Food = long.MaxValue; p.PopulationPerFood = 2;
        session = Session(save, defs); session.AdvanceTurn(1, 0);
        Assert.Equal(long.MaxValue / 2, session.ExportSave().GameState.Colonies![0].Food);
        c.Population = 1; c.Food = 0; p.StarvationLossPerTurn = long.MaxValue;
        session = Session(save, defs); session.AdvanceTurn(1, 0); Assert.Equal(1, session.ExportSave().GameState.Colonies![0].Population);
    }

    [Theory]
    [InlineData(0)] // AI planning
    [InlineData(2)] // Research
    [InlineData(3)] // Production
    [InlineData(4)] // Movement
    [InlineData(5)] // Encounter creation
    public void OtherPhasesCannotMutateEvenPermittedEconomicFields(int phaseNumber)
    {
        var session = Session(probe: (phase, s) =>
        {
            if ((int)phase == phaseNumber) { s.Colonies![0].Food++; s.Civilizations![0].Credits++; }
            return true;
        });
        var before = StateJson(session); Assert.Throws<InvalidOperationException>(() => session.AdvanceTurn(1, 0));
        Assert.Equal(before, StateJson(session));
    }

    [Theory]
    [InlineData("false")]
    [InlineData("exception")]
    [InlineData("invariant")]
    public void FailuresAfterEconomyDiscardEconomicChangesAndRetainedStagingReferences(string failure)
    {
        var fail = true; GameStateDto? retained = null;
        var session = Session(probe: (phase, staging) =>
        {
            retained = staging;
            if (phase == StrategicTurnPhase.Research)
            {
                Assert.Equal(1050, staging.Colonies![0].Population); Assert.Equal(60, staging.Civilizations![0].Credits);
                if (fail && failure == "false") return false;
                if (fail && failure == "exception") throw new ApplicationException("Injected after economy.");
                if (fail && failure == "invariant") staging.Colonies[0].PopulationCapacity++;
            }
            return true;
        });
        var before = StateJson(session);
        if (failure == "false") Assert.Equal(TurnErrorCode.PhaseFailed, session.AdvanceTurn(1, 0).ErrorCode);
        else if (failure == "exception") Assert.Throws<ApplicationException>(() => session.AdvanceTurn(1, 0));
        else Assert.Throws<InvalidOperationException>(() => session.AdvanceTurn(1, 0));
        Assert.Equal(before, StateJson(session)); retained!.Colonies![0].Population = 1; retained.Civilizations![0].Credits = 0;
        Assert.Equal(before, StateJson(session)); fail = false;
        Assert.Equal(TurnStatus.Completed, session.AdvanceTurn(1, 0).Status);
        var after = StateJson(session); retained!.Colonies![0].Food = 0; retained.Civilizations![0].Credits = 0;
        Assert.Equal(after, StateJson(session));
    }

    [Fact]
    public void ColonySnapshotsStateExportsAndProfilesAreDetachedAndNoDerivedOutputsPersist()
    {
        var save = Save(); var defs = Definitions(); var session = Session(save, defs); var old = session.CaptureSnapshot();
        defs.EconomicProfiles![0].FoodOutput = 999; defs.EconomicProfiles.Clear();
        save.GameState.Colonies![0].Population = 1; save.GameState.Colonies.Clear(); save.GameState.Civilizations![0].Credits = 0;
        session.AdvanceTurn(1, 0); var snapshot = session.CaptureSnapshot(); var c = Assert.Single(snapshot.Colonies);
        Assert.Equal(1050, c.Population); Assert.Equal(22, c.Food); Assert.Equal(14, c.Materials); Assert.Equal(60, snapshot.Civilizations[0].Credits);
        Assert.Equal(1000, old.Colonies[0].Population); Assert.Equal(50, old.Civilizations[0].Credits);
        Assert.Throws<NotSupportedException>(() => ((IList<ColonySnapshot>)snapshot.Colonies).Clear());
        Assert.All(typeof(ColonySnapshot).GetProperties(), p => Assert.Null(p.SetMethod));
        var before = StateJson(session); var exported = session.ExportSave(); exported.GameState.Colonies![0].Food = -1; exported.GameState.Colonies.Clear();
        Assert.Equal(before, StateJson(session));
        Assert.DoesNotContain("Industry", before); Assert.DoesNotContain("ResearchOutput", before);
        Assert.Null(JsonSerializer.Deserialize<GameStateDto>("{}")!.Colonies);
        Assert.Null(JsonSerializer.Deserialize<DefinitionCatalogDto>("{}")!.EconomicProfiles);
    }
}
