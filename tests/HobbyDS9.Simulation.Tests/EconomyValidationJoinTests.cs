using System.Text.Json;
using HobbyDS9.Contracts;
using Xunit;

namespace HobbyDS9.Simulation.Tests;

public sealed class EconomyValidationJoinTests
{
    // Captured BEFORE the production edit from clean 5e4a6f0, Simulation DLL C60724EB030E9C646D8A4633F13D28FD509ED40AA0942EEA1993EFDB4421B30B.
    // Ordered public reports, not a second implementation of the join algorithm. External oracle provenance is in the source packet.
    public static IEnumerable<object[]> OriginalOrderedReports()
    {
        yield return new object[] { "valid", "" };
        yield return new object[] { "reordered", "" };
        yield return new object[] { "definition-systems-null", "MissingField $.Definitions.StarSystems\nInvalidEconomicValue $.Definitions.planet-definition.sol\nInvalidEconomicValue $.Definitions.planet-definition.cardassia\nInvalidEconomicValue $.Definitions.planet-definition.founders-homeworld\nInvalidEconomicValue $.Definitions.planet-definition.bajor\nInvalidEconomicValue $.Definitions.planet-definition.mars\nMissingDefinition $.GameState.StarSystems[0].DefinitionId\nMissingDefinition $.GameState.StarSystems[1].DefinitionId\nMissingDefinition $.GameState.StarSystems[2].DefinitionId\nMissingDefinition $.GameState.StarSystems[3].DefinitionId\nMissingDefinition $.GameState.StarSystems[4].DefinitionId\nMissingDefinition $.GameState.StarSystems[5].DefinitionId" };
        yield return new object[] { "definition-system-null-row", "MissingField $.Definitions.StarSystems[6]" };
        yield return new object[] { "definition-system-null-key", "InvalidId $.Definitions.StarSystems[0].DefinitionId\nInvalidEconomicValue $.Definitions.planet-definition.sol\nInvalidEconomicValue $.Definitions.planet-definition.mars\nMissingDefinition $.GameState.StarSystems[0].DefinitionId" };
        yield return new object[] { "definition-system-duplicate", "DuplicateId $.Definitions.StarSystems[6].DefinitionId" };
        yield return new object[] { "definition-system-case", "InvalidId $.Definitions.StarSystems[0].DefinitionId\nInvalidEconomicValue $.Definitions.planet-definition.sol\nInvalidEconomicValue $.Definitions.planet-definition.mars\nMissingDefinition $.GameState.StarSystems[0].DefinitionId" };
        yield return new object[] { "planet-definitions-null", "MissingField $.Definitions.PopulationDefinitions\nInvalidEconomicValue $.GameState.planet.sol\nInvalidEconomicValue $.GameState.planet.cardassia\nInvalidEconomicValue $.GameState.planet.founders-homeworld\nInvalidEconomicValue $.GameState.planet.bajor\nInvalidEconomicValue $.GameState.planet.mars" };
        yield return new object[] { "planet-definition-null-row", "InvalidEconomicValue $.Definitions.Planets" };
        yield return new object[] { "planet-definition-null-key", "InvalidEconomicValue $.Definitions.\nInvalidEconomicValue $.GameState.planet.sol" };
        yield return new object[] { "planet-reference-null-key", "IdentityMismatch $.GameState.planet.sol\nInvalidEconomicValue $.GameState.planet.sol" };
        yield return new object[] { "planet-null-key-match", "InvalidEconomicValue $.Definitions.\nIdentityMismatch $.GameState.planet.sol" };
        yield return new object[] { "planet-null-key-first-wrong", "InvalidEconomicValue $.Definitions.\nInvalidEconomicValue $.Definitions.\nInvalidEconomicValue $.Definitions.\nIdentityMismatch $.GameState.planet.sol\nInvalidEconomicValue $.GameState.planet.sol" };
        yield return new object[] { "planet-duplicate-first-correct", "InvalidEconomicValue $.Definitions.planet-definition.sol\nInvalidEconomicValue $.Definitions.planet-definition.sol" };
        yield return new object[] { "planet-duplicate-first-wrong", "InvalidEconomicValue $.Definitions.planet-definition.sol\nInvalidEconomicValue $.Definitions.planet-definition.sol\nInvalidEconomicValue $.GameState.planet.sol" };
        yield return new object[] { "planet-duplicate-reversed", "InvalidEconomicValue $.Definitions.planet-definition.sol\nInvalidEconomicValue $.Definitions.planet-definition.sol" };
        yield return new object[] { "planet-definition-case", "InvalidEconomicValue $.Definitions.PLANET-DEFINITION.SOL\nInvalidEconomicValue $.GameState.planet.sol" };
        yield return new object[] { "definition-association-missing", "InvalidEconomicValue $.Definitions.planet-definition.sol\nInvalidEconomicValue $.GameState.planet.sol" };
        yield return new object[] { "definition-association-null-match", "InvalidId $.Definitions.StarSystems[0].DefinitionId\nInvalidEconomicValue $.Definitions.planet-definition.mars\nIdentityMismatch $.GameState.StarSystems[0]\nInvalidId $.GameState.StarSystems[0].DefinitionId\nInvalidEconomicValue $.GameState.planet.mars" };
        yield return new object[] { "definition-association-null-miss", "InvalidEconomicValue $.Definitions.planet-definition.sol\nInvalidEconomicValue $.GameState.planet.sol" };
        yield return new object[] { "state-planets-null", "MissingField $.GameState.PopulationState" };
        yield return new object[] { "state-planet-null-row", "InvalidEconomicValue $.GameState.Planets" };
        yield return new object[] { "state-definition-missing", "IdentityMismatch $.GameState.planet.sol\nInvalidEconomicValue $.GameState.planet.sol" };
        yield return new object[] { "state-definition-case", "IdentityMismatch $.GameState.planet.sol\nInvalidEconomicValue $.GameState.planet.sol" };
        yield return new object[] { "state-systems-null", "MissingField $.GameState.StarSystems\nInvalidFleetRoute $.GameState.Navigation\nInvalidEconomicValue $.GameState.planet.sol\nInvalidEconomicValue $.GameState.planet.cardassia\nInvalidEconomicValue $.GameState.planet.founders-homeworld\nInvalidEconomicValue $.GameState.planet.bajor\nInvalidEconomicValue $.GameState.planet.mars\nInvalidEconomicValue $.GameState.colony.sol.Ownership\nInvalidEconomicValue $.GameState.colony.cardassia.Ownership\nInvalidEconomicValue $.GameState.colony.founders-homeworld.Ownership\nInvalidEconomicValue $.GameState.colony.bajor.Ownership\nInvalidEconomicValue $.GameState.colony.mars.Ownership\nMissingField $.GameState.StarSystems\nMissingEndpoint $.Definitions.GalaxyEdges[0]\nMissingEndpoint $.Definitions.GalaxyEdges[1]\nMissingEndpoint $.Definitions.GalaxyEdges[2]\nMissingEndpoint $.Definitions.GalaxyEdges[3]\nMissingEndpoint $.Definitions.GalaxyEdges[4]" };
        yield return new object[] { "state-system-null-row", "MissingField $.GameState.StarSystems[6]\nInvalidId $.GameState.StarSystems" };
        yield return new object[] { "state-system-duplicate", "DuplicateId $.GameState.StarSystems[6]\nDuplicateId $.GameState.StarSystems" };
        yield return new object[] { "state-system-id-case", "InvalidEconomicValue $.GameState.planet.sol\nInvalidEconomicValue $.GameState.colony.sol.Planet" };
        yield return new object[] { "state-system-definition-case", "IdentityMismatch $.GameState.StarSystems[0]\nInvalidId $.GameState.StarSystems[0].DefinitionId\nInvalidEconomicValue $.GameState.planet.sol\nInvalidEconomicValue $.GameState.planet.mars" };
        yield return new object[] { "state-system-missing", "InvalidEconomicValue $.GameState.planet.sol\nInvalidEconomicValue $.GameState.colony.sol.Planet" };
        yield return new object[] { "state-system-wrong-association", "IdentityMismatch $.GameState.StarSystems[0]\nInvalidEconomicValue $.GameState.planet.sol\nInvalidEconomicValue $.GameState.planet.mars" };
        yield return new object[] { "state-system-null-id-match", "InvalidId $.GameState.StarSystems[0]\nInvalidFleetRoute $.GameState.Navigation\nInvalidEconomicValue $.GameState.planet.mars\nInvalidEconomicValue $.GameState.colony.sol.Planet\nInvalidEconomicValue $.GameState.colony.sol.Ownership\nInvalidEconomicValue $.GameState.colony.mars.Ownership\nInvalidId $.GameState.StarSystems\nMissingEndpoint $.Definitions.GalaxyEdges[0]" };
        yield return new object[] { "state-system-null-id-miss", "InvalidEconomicValue $.GameState.planet.sol\nInvalidEconomicValue $.GameState.colony.sol.Planet" };
        yield return new object[] { "state-system-null-definition-match", "InvalidEconomicValue $.Definitions.planet-definition.sol\nIdentityMismatch $.GameState.StarSystems[0]\nInvalidId $.GameState.StarSystems[0].DefinitionId\nInvalidEconomicValue $.GameState.planet.mars" };
        yield return new object[] { "state-system-null-definition-miss", "IdentityMismatch $.GameState.StarSystems[0]\nInvalidId $.GameState.StarSystems[0].DefinitionId\nInvalidEconomicValue $.GameState.planet.sol\nInvalidEconomicValue $.GameState.planet.mars" };
        yield return new object[] { "pair-delimiter-collision", "InvalidEconomicValue $.Definitions.planet-definition.sol\nInvalidId $.GameState.StarSystems[0]\nMissingDefinition $.GameState.StarSystems[0].DefinitionId\nInvalidFleetRoute $.GameState.Navigation\nInvalidEconomicValue $.GameState.planet.sol\nInvalidEconomicValue $.GameState.planet.mars\nInvalidEconomicValue $.GameState.colony.sol.Planet\nInvalidEconomicValue $.GameState.colony.sol.Ownership\nInvalidEconomicValue $.GameState.colony.mars.Ownership\nInvalidId $.GameState.StarSystems\nMissingEndpoint $.Definitions.GalaxyEdges[0]" };
        yield return new object[] { "multiple-errors", "MissingField $.Definitions.StarSystems[6]\nInvalidEconomicValue $.Definitions.planet-definition.sol\nInvalidEconomicValue $.Definitions.planet-definition.sol\nInvalidEconomicValue $.Definitions.Planets\nMissingField $.GameState.StarSystems[6]\nInvalidEconomicValue $.GameState.planet.sol\nInvalidEconomicValue $.GameState.Planets\nInvalidEconomicValue $.GameState.colony.sol.Planet\nInvalidId $.GameState.StarSystems" };
        yield return new object[] { "population-state-missing", "MissingField $.GameState.PopulationState" };
    }
    static string Report(ScenarioDto s) => string.Join("\n", FoundationValidator.ValidateGameState(s.InitialState,s.Definitions).Issues.Select(i=>i.Code+" "+i.Path));
    [Theory]
    [MemberData(nameof(OriginalOrderedReports))]
    public void PublicOrderedReportsMatchPreChangeOracleWithoutInputMutation(string name,string expected)
    {
        var s=EconomyValidationJoinCases.Scenario(); EconomyValidationJoinCases.Mutate(s,name);
        var before=JsonSerializer.Serialize(s);
        Assert.Equal(expected,Report(s));
        Assert.Equal(before,JsonSerializer.Serialize(s));
    }
    [Fact]
    public void FreshInvocationReadsChangedDefinitionMembershipFirstRowAndStateAssociation()
    {
        var s=EconomyValidationJoinCases.Scenario();var d=s.Definitions;var g=s.InitialState;
        Assert.Equal("",Report(s));
        var p=d.Planets![0];var planet=g.Planets!.First(x=>x.DefinitionId==p.DefinitionId);
        var system=g.StarSystems!.First(x=>x.SystemId==planet.SystemId);
        var systemDefinition=d.StarSystems!.First(x=>x.DefinitionId==p.SystemDefinitionId);
        var definitionId=systemDefinition.DefinitionId;
        systemDefinition.DefinitionId="system.absent"; Assert.NotEqual("",Report(s));
        systemDefinition.DefinitionId=definitionId; Assert.Equal("",Report(s));
        var wrong=EconomyValidationJoinCases.Clone(p);wrong.SystemDefinitionId="system.absent";
        d.Planets.Insert(0,wrong);
        Assert.Contains("InvalidEconomicValue $.GameState."+planet.PlanetId,Report(s));
        d.Planets.RemoveAt(0);Assert.Equal("",Report(s));
        var association=system.DefinitionId;system.DefinitionId="system.absent";
        Assert.Contains("InvalidEconomicValue $.GameState."+planet.PlanetId,Report(s));
        system.DefinitionId=association;Assert.Equal("",Report(s));
    }
}
