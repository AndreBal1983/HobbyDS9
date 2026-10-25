using System.Text.Json;
using HobbyDS9.Contracts;
using Xunit;

namespace HobbyDS9.Simulation.Tests;

public sealed class PlanetaryOwnershipContextTests
{
    // Binding-only fixture; not a started colonisation campaign or authorisation shortcut.
    static (ScenarioDto scenario,List<PlanetControlDto> controls) Mixed()
    {
        var s=VerticalSliceFixture.Scenario();var game=s.InitialState;
        foreach(var system in game.StarSystems!)system.OwnerCivilizationId=null;
        game.Colonies!.RemoveAll(c=>c.OwnerCivilizationId==null);
        // Use an actual existing actor ID rather than imposing a fixture naming convention.
        var foreign=game.Civilizations!.First(c=>c.CivilizationId!=game.Colonies.Single(c=>c.ColonyId=="colony.sol").OwnerCivilizationId).CivilizationId;
        game.Colonies.Single(c=>c.ColonyId=="colony.mars").OwnerCivilizationId=foreign;
        return(s,game.Planets!.Select(p=>new PlanetControlDto{PlanetId=p.PlanetId,Kind=game.Colonies.Any(c=>c.PlanetId==p.PlanetId)?PlanetControllerKind.Empire:PlanetControllerKind.Unclaimed,ActorId=game.Colonies.SingleOrDefault(c=>c.PlanetId==p.PlanetId)?.OwnerCivilizationId}).ToList());
    }
    [Fact] public void MixedSystemHasDistinctPlanetOwnersButOldPublicValidationStillRejectsIt()
    {
        var x=Mixed();var game=x.scenario.InitialState;var before=JsonSerializer.Serialize(game);
        var owned=PlanetaryOwnershipContext.CreateForColonisation(x.scenario.Definitions,game,x.controls,Array.Empty<ActivityMinorStateDto>());
        owned.RequireOwner(x.scenario.Definitions,game);var earth=game.Colonies!.Single(c=>c.ColonyId=="colony.sol");var mars=game.Colonies!.Single(c=>c.ColonyId=="colony.mars");
        Assert.Equal(earth.SystemId,mars.SystemId);Assert.NotEqual(earth.OwnerCivilizationId,mars.OwnerCivilizationId);
        Assert.True(owned.EmpireControls(earth.PlanetId,earth.OwnerCivilizationId!));Assert.False(owned.EmpireControls(mars.PlanetId,earth.OwnerCivilizationId!));
        Assert.False(FoundationValidator.ValidateGameState(game,x.scenario.Definitions).IsValid);Assert.Equal(before,JsonSerializer.Serialize(game));
    }
    [Fact] public void ReferenceEraAndDefinitionBindingRejectsOtherCopiesEvenWithEqualContent()
    {
        var x=Mixed();var original=x.scenario.InitialState;var context=PlanetaryOwnershipContext.CreateForColonisation(x.scenario.Definitions,original,x.controls,Array.Empty<ActivityMinorStateDto>());
        var next=SimulationSession.CopyState(original);var copied=context.CopyForState(next);
        Assert.Throws<ArgumentException>(()=>context.RequireOwner(x.scenario.Definitions,next));copied.RequireOwner(x.scenario.Definitions,next);
        var equalDefinitions=JsonSerializer.Deserialize<DefinitionCatalogDto>(JsonSerializer.Serialize(x.scenario.Definitions))!;
        Assert.Throws<ArgumentException>(()=>copied.RequireOwner(equalDefinitions,next));
        x.controls[0].ActorId="civilization.forged";var exposed=context.CopyControls();exposed[0].Kind=PlanetControllerKind.Unclaimed;exposed[0].ActorId=null;
        Assert.Equal(context.CopyControls().Select(c=>(c.PlanetId,c.Kind,c.ActorId)),copied.CopyControls().Select(c=>(c.PlanetId,c.Kind,c.ActorId)));
        next.Colonies![0].OwnerCivilizationId="civilization.forged";Assert.Throws<ArgumentException>(()=>context.CopyForState(next));context.ValidateOwnership();
    }
    [Theory]
    [InlineData("missing")][InlineData("duplicate")][InlineData("unknown")][InlineData("system-owner")][InlineData("colony-owner")][InlineData("unclaimed-owner")][InlineData("minor-as-empire")][InlineData("ghost-empire")]
    public void InvalidPlanetControlCannotCreateContext(string fault)
    {
        var x=Mixed();var state=x.scenario.InitialState;
        switch(fault)
        {
            case "missing":x.controls.RemoveAt(0);break;
            case "duplicate":x.controls.Add(x.controls[0]);break;
            case "unknown":x.controls[0].PlanetId="planet.unknown";break;
            case "system-owner":state.StarSystems![0].OwnerCivilizationId=state.Civilizations![0].CivilizationId;break;
            case "colony-owner":state.Colonies![0].OwnerCivilizationId="civilization.forged";break;
            case "unclaimed-owner":x.controls[0].Kind=PlanetControllerKind.Unclaimed;x.controls[0].ActorId="civilization.forged";break;
            case "ghost-empire":var row=x.controls.First(c=>c.Kind==PlanetControllerKind.Unclaimed);row.Kind=PlanetControllerKind.Empire;row.ActorId=state.Civilizations![0].CivilizationId;break;
            case "minor-as-empire":x.controls[0].Kind=PlanetControllerKind.IndependentGovernment;x.controls[0].ActorId=state.Civilizations![0].CivilizationId;break;
        }
        Assert.Throws<ArgumentException>(()=>PlanetaryOwnershipContext.CreateForColonisation(x.scenario.Definitions,state,x.controls,Array.Empty<ActivityMinorStateDto>()));
    }
}
