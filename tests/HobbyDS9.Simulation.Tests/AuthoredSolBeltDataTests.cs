using System.Text.Json;
using HobbyDS9.Contracts;
using HobbyDS9.Simulation;
using Xunit;

namespace HobbyDS9.Simulation.Tests;

public sealed class AuthoredSolBeltDataTests
{
    static readonly string[] Suffixes={"mercury","venus","sol","mars","jupiter","saturn","uranus","neptune"};
    static FrozenColonisationPlanetsDto Inventory(string system="system.generated.0042")
    {
        var inventory=new FrozenColonisationPlanetsDto();
        foreach(var suffix in Suffixes)
        {
            var id="planet."+suffix;var definition="planet-definition."+suffix;
            inventory.InitialPlanets!.Add(new(){PlanetId=id,DefinitionId=definition,SystemId=system,RemainingDeposits=new()});
            inventory.Definitions!.Add(new(){DefinitionId=definition,SystemDefinitionId="star-system."+system,DisplayName=suffix,Deposits=new()});
            inventory.Bodies!.Add(new(){PlanetId=id,BodyId="body.physical."+id,SystemId=system});
        }
        return inventory;
    }
    static string Json<T>(T value)=>JsonSerializer.Serialize(value);

    [Fact]
    public void AuthoredBandUsesMappedSolIdentityAndLeavesEightPlanetsAndAllInputFactsUntouched()
    {
        var inventory=Inventory();var before=Json(inventory);
        var belt=Assert.IsType<SystemAsteroidBeltDto>(AuthoredSolBeltData.Create(inventory,"42"));
        Assert.Equal("belt.authored.sol.asteroid.v1",belt.FeatureId);
        Assert.Equal("system.generated.0042",belt.SystemId);
        Assert.Equal(2200,belt.InnerRadiusMilliAu);Assert.Equal(3200,belt.OuterRadiusMilliAu);
        Assert.Equal(1000,belt.OuterRadiusMilliAu-belt.InnerRadiusMilliAu);
        Assert.Matches("^[0-9a-f]{64}$",belt.VisualSeed);
        Assert.Equal(8,inventory.InitialPlanets!.Count);Assert.Equal(before,Json(inventory));
        Assert.All(inventory.InitialPlanets,p=>Assert.Empty(p.RemainingDeposits!));
    }

    [Fact]
    public void VersionedVisualSeedIsStableAcrossOrderingAndDetachedFromReturnedMutations()
    {
        var inventory=Inventory();var original=Assert.IsType<SystemAsteroidBeltDto>(AuthoredSolBeltData.Create(inventory,"42"));
        var first=Json(original);inventory.InitialPlanets!.Reverse();inventory.Definitions!.Reverse();inventory.Bodies!.Reverse();
        Assert.Equal(first,Json(AuthoredSolBeltData.Create(inventory,"42")));
        var otherSeed=Assert.IsType<SystemAsteroidBeltDto>(AuthoredSolBeltData.Create(inventory,"43"));
        Assert.Equal(original.FeatureId,otherSeed.FeatureId);Assert.Equal(original.InnerRadiusMilliAu,otherSeed.InnerRadiusMilliAu);
        Assert.NotEqual(original.VisualSeed,otherSeed.VisualSeed);
        var otherSystem=Assert.IsType<SystemAsteroidBeltDto>(AuthoredSolBeltData.Create(Inventory("system.generated.0007"),"42"));
        Assert.Equal("system.generated.0007",otherSystem.SystemId);Assert.NotEqual(original.VisualSeed,otherSystem.VisualSeed);
        original.VisualSeed="modified";Assert.Equal(first,Json(AuthoredSolBeltData.Create(inventory,"42")));
    }

    [Theory]
    [InlineData("missing-earth")][InlineData("missing-mars")][InlineData("missing-jupiter")]
    [InlineData("duplicate-earth")][InlineData("wrong-definition")][InlineData("wrong-system")]
    [InlineData("wrong-binding")][InlineData("wrong-definition-system")][InlineData("missing-bindings")][InlineData("duplicate-body")]
    public void MissingDuplicateOrInconsistentAuthoredAssociationsDoNotManufactureABelt(string mutation)
    {
        var inventory=Inventory();
        switch(mutation)
        {
            case "missing-earth":inventory.InitialPlanets!.RemoveAll(p=>p.PlanetId=="planet.sol");break;
            case "missing-mars":inventory.InitialPlanets!.RemoveAll(p=>p.PlanetId=="planet.mars");break;
            case "missing-jupiter":inventory.InitialPlanets!.RemoveAll(p=>p.PlanetId=="planet.jupiter");break;
            case "duplicate-earth":inventory.InitialPlanets!.Add(inventory.InitialPlanets.Single(p=>p.PlanetId=="planet.sol"));break;
            case "wrong-definition":inventory.InitialPlanets!.Single(p=>p.PlanetId=="planet.sol").DefinitionId="planet-definition.other";break;
            case "wrong-system":inventory.InitialPlanets!.Single(p=>p.PlanetId=="planet.jupiter").SystemId="system.other";break;
            case "wrong-binding":inventory.Bodies!.Single(b=>b.PlanetId=="planet.jupiter").SystemId="system.other";break;
            case "wrong-definition-system":inventory.Definitions!.Single(d=>d.DefinitionId=="planet-definition.jupiter").SystemDefinitionId="star-system.other";break;
            case "missing-bindings":inventory.Bodies=null;break;
            case "duplicate-body":inventory.Bodies!.Single(b=>b.PlanetId=="planet.jupiter").BodyId=inventory.Bodies!.Single(b=>b.PlanetId=="planet.sol").BodyId;break;
        }
        var before=Json(inventory);Assert.Null(AuthoredSolBeltData.Create(inventory,"42"));Assert.Equal(before,Json(inventory));
    }

    [Fact]
    public void DatasetPinsOfficialSourceApproximationAndUnitsWithoutPretendingToDefineExactEdges()
    {
        DirectoryInfo? directory=new(AppContext.BaseDirectory);
        string? path=null;
        while(directory!=null)
        {
            var candidate=Path.Combine(directory.FullName,"scenarios","authored-sol-belt-v1","belt.json");
            if(File.Exists(candidate)){path=candidate;break;}directory=directory.Parent;
        }
        Assert.NotNull(path);
        using var document=JsonDocument.Parse(File.ReadAllBytes(path));var root=document.RootElement;
        Assert.Equal(AuthoredSolBeltData.Version,root.GetProperty("Version").GetString());
        Assert.Equal(AuthoredSolBeltData.SourceUrl,root.GetProperty("SourceUrl").GetString());
        Assert.Equal(AuthoredSolBeltData.SourceQuestionDate,root.GetProperty("SourceQuestionDate").GetString());
        Assert.Equal("milli-AU",root.GetProperty("StoredUnit").GetString());
        Assert.Equal(AuthoredSolBeltData.InnerRadiusMilliAu,root.GetProperty("InnerRadiusMilliAu").GetInt32());
        Assert.Equal(AuthoredSolBeltData.OuterRadiusMilliAu,root.GetProperty("OuterRadiusMilliAu").GetInt32());
        Assert.True(root.GetProperty("ApproximateBand").GetBoolean());
    }
}
