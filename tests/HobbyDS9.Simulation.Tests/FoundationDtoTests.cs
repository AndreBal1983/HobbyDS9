using System.Text.Json;
using HobbyDS9.Contracts;
using HobbyDS9.Simulation;
using Xunit;

namespace HobbyDS9.Simulation.Tests;

public sealed class FoundationDtoTests
{
    private const string Rules = "foundation-v1";
    private static readonly string ContentHash = new('a', 64);
    private static readonly SaveCompatibility Compatibility = new("scenario.fixture", Rules, ContentHash);
    private static DefinitionCatalogDto Definitions() => new() { ContentFormatVersion = FoundationVersions.ContentFormatVersion, Civilizations = new(), StarSystems = new(), GalaxyEdges = new(), DefinitionIds = new() { "definition.alpha", "definition.beta" } };

    private static GameStateDto State() => new()
    {
        Phase = SessionPhase.Planning, TurnNumber = 1, StateRevision = 0, NextEntitySequence = 1, RngState = new SplitMix64Random(47).ToDto(), AcceptedCommandIds = new(), Civilizations = new(), StarSystems = new(),
        EntityIdentities = new()
        {
            new() { EntityId = "entity.initial", DefinitionId = "definition.alpha" },
            new() { EntityId = "entity.untyped" }
        }
    };

    private static SaveEnvelopeDto Save(GameStateDto? state = null) => new()
    {
        SaveFormatVersion = FoundationVersions.SaveFormatVersion, RulesVersion = Rules,
        ScenarioId = "scenario.fixture", ContentHash = ContentHash, GameState = state ?? State()
    };

    [Fact]
    public void ExplicitSaveReloadContinuesAllocationAndCampaignRandomness()
    {
        var state = State();
        var allocator = new EntityIdAllocator();
        var random = new SplitMix64Random(47);
        for (var i = 0; i < 12; i++)
        {
            random.NextBelow(101);
            state.EntityIdentities.Add(new() { EntityId = allocator.Allocate(i % 2 == 0 ? "entity" : "token").Value,
                DefinitionId = "definition.beta" });
        }
        state.NextEntitySequence = allocator.NextEntitySequence;
        state.RngState = random.ToDto();
        var json = JsonSerializer.Serialize(Save(state));
        Assert.DoesNotContain("$type", json);
        var loaded = JsonSerializer.Deserialize<SaveEnvelopeDto>(json)!;
        Assert.True(FoundationValidator.ValidateSave(loaded, Definitions(), Compatibility).IsValid);
        Assert.Equal(json, JsonSerializer.Serialize(loaded));
        var resumedRandom = SplitMix64Random.FromDto(loaded.GameState.RngState);
        var resumedAllocator = EntityIdAllocator.FromDto(new EntityAllocationStateDto { NextEntitySequence = loaded.GameState.NextEntitySequence });
        for (var i = 0; i < 100; i++)
        {
            Assert.Equal(random.NextUInt64(), resumedRandom.NextUInt64());
            Assert.Equal(random.NextBelow(0x8000000000000001UL), resumedRandom.NextBelow(0x8000000000000001UL));
            Assert.Equal(allocator.Allocate("entity"), resumedAllocator.Allocate("entity"));
        }
        Assert.Equal(random.State, resumedRandom.State);
        Assert.Equal(allocator.NextEntitySequence, resumedAllocator.NextEntitySequence);
    }

    [Fact]
    public void MissingOrNullPayloadsReturnValidationIssuesWithoutMutatingInput()
    {
        Assert.False(FoundationValidator.ValidateSave(null, Definitions(), Compatibility).IsValid);
        var empty = JsonSerializer.Deserialize<SaveEnvelopeDto>("{}")!;
        var before = JsonSerializer.Serialize(empty);
        var report = FoundationValidator.ValidateSave(empty, Definitions(), Compatibility);
        Assert.Contains(report.Issues, e => e.Code == ValidationCode.UnsupportedSaveFormat);
        Assert.Contains(report.Issues, e => e.Code == ValidationCode.InvalidCounter);
        Assert.Contains(report.Issues, e => e.Code == ValidationCode.MissingField && e.Path.EndsWith(".State"));
        Assert.Equal(before, JsonSerializer.Serialize(empty));
        var save = Save();
        save.GameState = null!;
        Assert.Contains(FoundationValidator.ValidateSave(save, null, Compatibility).Issues,
            e => e.Code == ValidationCode.MissingField && e.Path == "$.GameState");
        var state = State();
        state.EntityIdentities = null!;
        state.RngState = null!;
        Assert.False(FoundationValidator.ValidateGameState(state, Definitions()).IsValid);
        state = State();
        state.EntityIdentities.Add(null!);
        Assert.Contains(FoundationValidator.ValidateGameState(state, Definitions()).Issues, e => e.Code == ValidationCode.MissingField);
        Assert.False(FoundationValidator.ValidateScenario(null).IsValid);
    }

    [Fact]
    public void SaveCompatibilityRejectsUnknownFormatsRulesContentAndScenario()
    {
        var save = Save();
        save.SaveFormatVersion++;
        save.RulesVersion = "other-rules";
        save.ContentHash = new string('b', 64);
        save.ScenarioId = "scenario.other";
        var report = FoundationValidator.ValidateSave(save, Definitions(), Compatibility);
        Assert.Equal(new[] { ValidationCode.UnsupportedSaveFormat, ValidationCode.ScenarioMismatch,
            ValidationCode.RulesMismatch, ValidationCode.ContentMismatch }, report.Issues.Select(i => i.Code));
        Assert.Throws<ArgumentException>(() => report.ThrowIfInvalid("save"));
        save.ContentHash = "not-a-content-hash";
        Assert.Contains(FoundationValidator.ValidateSave(save, Definitions(), Compatibility).Issues, e => e.Code == ValidationCode.InvalidContentHash);
    }

    [Fact]
    public void IdsDefinitionsAndReferencesAreValidatedWithPrecisePaths()
    {
        var state = State();
        state.EntityIdentities.Add(new() { EntityId = "entity.initial", DefinitionId = "definition.missing" });
        state.EntityIdentities.Add(new() { EntityId = "Invalid" });
        var definitions = Definitions();
        definitions.DefinitionIds.Add("definition.alpha");
        definitions.DefinitionIds.Add("definition.spawn.0000000000000001");
        var report = FoundationValidator.ValidateGameState(state, definitions);
        Assert.Contains(report.Issues, e => e.Code == ValidationCode.DuplicateId && e.Path == "$.Definitions.DefinitionIds[2]");
        Assert.Contains(report.Issues, e => e.Code == ValidationCode.DuplicateId && e.Path == "$.GameState.EntityIdentities[2].EntityId");
        Assert.Contains(report.Issues, e => e.Code == ValidationCode.MissingDefinition && e.Path == "$.GameState.EntityIdentities[2].DefinitionId");
        Assert.Contains(report.Issues, e => e.Code == ValidationCode.InvalidId);
        Assert.Contains(report.Issues, e => e.Code == ValidationCode.ReservedIdNamespace);
    }

    [Fact]
    public void AllocatedIdentitiesMustAgreeWithTheGlobalCounter()
    {
        var state = State();
        state.NextEntitySequence = 2;
        state.EntityIdentities.Add(new() { EntityId = "entity.spawn.0000000000000001" });
        Assert.True(FoundationValidator.ValidateGameState(state, Definitions()).IsValid);
        state.EntityIdentities.Add(new() { EntityId = "token.spawn.0000000000000001" });
        Assert.Contains(FoundationValidator.ValidateGameState(state, Definitions()).Issues, e => e.Code == ValidationCode.DuplicateId);
        state.EntityIdentities.RemoveAt(state.EntityIdentities.Count - 1);
        state.NextEntitySequence = 1;
        Assert.Contains(FoundationValidator.ValidateGameState(state, Definitions()).Issues, e => e.Code == ValidationCode.AllocationSequenceMismatch);
        state.EntityIdentities[state.EntityIdentities.Count - 1].EntityId = "entity.spawn.not-hex";
        Assert.Contains(FoundationValidator.ValidateGameState(state, Definitions()).Issues, e => e.Code == ValidationCode.InvalidAllocatedId);
        state.TurnNumber = 0;
        state.StateRevision = -1;
        state.NextEntitySequence = 0;
        Assert.Equal(3, FoundationValidator.ValidateGameState(state, Definitions()).Issues.Count(e => e.Code == ValidationCode.InvalidCounter));
    }

    [Fact]
    public void ScenarioHeadersRoundTripAndCannotClaimTheRuntimeNamespace()
    {
        var scenario = new ScenarioDto { ScenarioId = "scenario.fixture", RulesVersion = Rules,
            ContentHash = ContentHash, Definitions = Definitions(), InitialState = State() };
        var loaded = JsonSerializer.Deserialize<ScenarioDto>(JsonSerializer.Serialize(scenario))!;
        Assert.True(FoundationValidator.ValidateScenario(loaded).IsValid);
        loaded.InitialState.EntityIdentities[0].EntityId = "entity.spawn.0000000000000001";
        loaded.InitialState.NextEntitySequence = 2;
        Assert.Contains(FoundationValidator.ValidateScenario(loaded).Issues, e => e.Code == ValidationCode.ReservedIdNamespace);
    }

    [Fact]
    public void ReorderedIdentityListsAndExhaustedCountersRemainValid()
    {
        var state = State();
        state.NextEntitySequence = ulong.MaxValue;
        state.EntityIdentities.Add(new() { EntityId = "entity.spawn.fffffffffffffffe", DefinitionId = "definition.beta" });
        var definitions = Definitions();
        Assert.True(FoundationValidator.ValidateGameState(state, definitions).IsValid);
        state.EntityIdentities.Reverse();
        definitions.DefinitionIds.Reverse();
        Assert.True(FoundationValidator.ValidateGameState(state, definitions).IsValid);
        var dto = JsonSerializer.Deserialize<GameStateDto>(JsonSerializer.Serialize(state))!;
        Assert.True(FoundationValidator.ValidateGameState(dto, definitions).IsValid);
        Assert.Equal(ulong.MaxValue, dto.NextEntitySequence);
    }
}
