using System.Text.Json;
using HobbyDS9.Contracts;

namespace HobbyDS9.Simulation.Tests;

// Test inputs only; also compiled into the external pre-change ordered-report oracle.
internal static class EconomyValidationJoinCases
{
    internal static readonly string[] Names = {
        "valid", "reordered", "definition-systems-null", "definition-system-null-row", "definition-system-null-key",
        "definition-system-duplicate", "definition-system-case", "planet-definitions-null", "planet-definition-null-row",
        "planet-definition-null-key", "planet-reference-null-key", "planet-null-key-match", "planet-null-key-first-wrong",
        "planet-duplicate-first-correct", "planet-duplicate-first-wrong", "planet-duplicate-reversed", "planet-definition-case",
        "definition-association-missing", "definition-association-null-match", "definition-association-null-miss",
        "state-planets-null", "state-planet-null-row", "state-definition-missing", "state-definition-case",
        "state-systems-null", "state-system-null-row", "state-system-duplicate", "state-system-id-case",
        "state-system-definition-case", "state-system-missing", "state-system-wrong-association", "state-system-null-id-match",
        "state-system-null-id-miss", "state-system-null-definition-match", "state-system-null-definition-miss",
        "pair-delimiter-collision", "multiple-errors", "population-state-missing" };

    internal static ScenarioDto Scenario() => JsonSerializer.Deserialize<ScenarioDto>(File.ReadAllBytes(
        Path.Combine(AppContext.BaseDirectory, "scenarios", "ds9-slice", "scenario.json")))!;
    internal static T Clone<T>(T row) => JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(row))!;
    internal static void Mutate(ScenarioDto s, string name)
    {
        var d=s.Definitions; var g=s.InitialState;
        var p=d.Planets![0]; var planet=g.Planets!.First(x=>x.DefinitionId==p.DefinitionId);
        var system=g.StarSystems!.First(x=>x.SystemId==planet.SystemId);
        var systemDefinition=d.StarSystems!.First(x=>x.DefinitionId==p.SystemDefinitionId);
        void Duplicate(bool firstWrong)
        {
            var wrong=Clone(p); wrong.SystemDefinitionId="system.absent";
            if(firstWrong)d.Planets!.Insert(0,wrong);else d.Planets!.Add(wrong);
        }
        switch(name)
        {
            case "valid":break;
            case "reordered":d.Planets!.Reverse();d.StarSystems!.Reverse();g.Planets!.Reverse();g.StarSystems!.Reverse();break;
            case "definition-systems-null":d.StarSystems=null;break;
            case "definition-system-null-row":d.StarSystems!.Add(null!);break;
            case "definition-system-null-key":systemDefinition.DefinitionId=null!;break;
            case "definition-system-duplicate":d.StarSystems!.Add(Clone(systemDefinition));break;
            case "definition-system-case":systemDefinition.DefinitionId=systemDefinition.DefinitionId.ToUpperInvariant();break;
            case "planet-definitions-null":d.Planets=null;break;
            case "planet-definition-null-row":d.Planets!.Add(null!);break;
            case "planet-definition-null-key":p.DefinitionId=null!;break;
            case "planet-reference-null-key":planet.DefinitionId=null!;break;
            case "planet-null-key-match":p.DefinitionId=null!;planet.DefinitionId=null!;break;
            case "planet-null-key-first-wrong":p.DefinitionId=null!;planet.DefinitionId=null!;Duplicate(true);break;
            case "planet-duplicate-first-correct":Duplicate(false);break;
            case "planet-duplicate-first-wrong":Duplicate(true);break;
            case "planet-duplicate-reversed":Duplicate(true);d.Planets!.Reverse();break;
            case "planet-definition-case":p.DefinitionId=p.DefinitionId.ToUpperInvariant();break;
            case "definition-association-missing":p.SystemDefinitionId="system.absent";break;
            case "definition-association-null-match":p.SystemDefinitionId=null!;systemDefinition.DefinitionId=null!;system.DefinitionId=null!;break;
            case "definition-association-null-miss":p.SystemDefinitionId=null!;break;
            case "state-planets-null":g.Planets=null;break;
            case "state-planet-null-row":g.Planets!.Add(null!);break;
            case "state-definition-missing":planet.DefinitionId="planet-definition.absent";break;
            case "state-definition-case":planet.DefinitionId=planet.DefinitionId.ToUpperInvariant();break;
            case "state-systems-null":g.StarSystems=null;break;
            case "state-system-null-row":g.StarSystems!.Add(null!);break;
            case "state-system-duplicate":g.StarSystems!.Add(Clone(system));break;
            case "state-system-id-case":planet.SystemId=planet.SystemId.ToUpperInvariant();break;
            case "state-system-definition-case":system.DefinitionId=system.DefinitionId.ToUpperInvariant();break;
            case "state-system-missing":planet.SystemId="system.absent";break;
            case "state-system-wrong-association":system.DefinitionId=d.StarSystems!.First(x=>x.DefinitionId!=p.SystemDefinitionId).DefinitionId;break;
            case "state-system-null-id-match":system.SystemId=null!;planet.SystemId=null!;break;
            case "state-system-null-id-miss":planet.SystemId=null!;break;
            case "state-system-null-definition-match":system.DefinitionId=null!;p.SystemDefinitionId=null!;break;
            case "state-system-null-definition-miss":system.DefinitionId=null!;break;
            case "pair-delimiter-collision":system.SystemId="system.a|b";system.DefinitionId="c";planet.SystemId="system.a";p.SystemDefinitionId="b|c";break;
            case "multiple-errors":Duplicate(true);d.Planets!.Add(null!);d.StarSystems!.Add(null!);g.Planets!.Add(null!);g.StarSystems!.Add(null!);planet.SystemId="system.absent";break;
            case "population-state-missing":g.PopulationStatistics=null;break;
            default:throw new ArgumentOutOfRangeException(nameof(name),name,"Unknown input mutation");
        }
    }
}
