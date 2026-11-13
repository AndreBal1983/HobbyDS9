using System.Text.Json;
using HobbyDS9.Contracts;
using Xunit;

namespace HobbyDS9.Simulation.Tests;

public sealed class StreamedLogisticsTests
{
    [Fact] public void RealRouteBlockadePausesNextMaterialQuantumThenPhysicalDeliveryResumesConservatively()
    {
        var scenario=VerticalSliceFixture.Scenario();var game=scenario.InitialState;
        var origin=game.Colonies!.Single(c=>c.ColonyId=="colony.sol");var yard=game.Colonies!.Single(c=>c.ColonyId=="colony.mars");
        foreach(var c in game.Colonies!){c.DesiredStockpiles!.Clear();c.Food=1000;c.Stability=10000;}
        PopulationFixture.SetDilithium(origin,10);PopulationFixture.SetDilithium(yard,0);
        var hostile=game.Fleets!.Single(f=>f.OwnerCivilizationId=="civ.cardassian");hostile.LocationNodeId="system.sol";
        var desired=JsonSerializer.Serialize(game.Colonies.Select(c=>c.DesiredStockpiles));
        IReadOnlyList<ResourceAmountDto> costs=new List<ResourceAmountDto>{new(){ResourceDefinitionId="resource.dilithium",Amount=4}};
        IReadOnlyDictionary<string,IReadOnlyList<ResourceAmountDto>> demand=new Dictionary<string,IReadOnlyList<ResourceAmountDto>>{
            [yard.ColonyId]=new List<ResourceAmountDto>{new(){ResourceDefinitionId="resource.dilithium",Amount=1}}};
        long completed=0,consumed=0;
        void Work()
        {
            var step=StreamedMaterialWork.Plan(costs,yard.Stockpiles!,80,completed,8);
            foreach(var r in step.ConsumedDelta){PopulationRules.AddStock(yard.Stockpiles!,r.ResourceDefinitionId,-r.Amount);consumed+=r.Amount;}
            completed=step.CompletedWork;
        }
        PopulationLogistics.Execute(game,scenario.Definitions,demand);Work();Assert.Equal(0,completed);
        Assert.Contains(game.LastDeliveries!,r=>r.DestinationPlanetId==yard.PlanetId&&r.ResourceDefinitionId=="resource.dilithium"&&r.RouteIntegrity==0&&r.Delivered==0);
        hostile.LocationNodeId="system.cardassia";
        PopulationLogistics.Execute(game,scenario.Definitions,demand);Assert.Equal(1,PopulationFixture.Dilithium(yard));Work();Assert.Equal(8,completed);Assert.Equal(1,consumed);
        hostile.LocationNodeId="system.sol";
        for(int i=0;i<3;i++){PopulationLogistics.Execute(game,scenario.Definitions,demand);Work();}
        Assert.Equal(20,completed);Assert.Equal(1,consumed);Assert.Equal(0,PopulationFixture.Dilithium(yard));
        hostile.LocationNodeId="system.cardassia";
        for(int i=0;i<20&&completed<80;i++){PopulationLogistics.Execute(game,scenario.Definitions,demand);Work();}
        Assert.Equal(80,completed);Assert.Equal(4,consumed);
        Assert.Equal(10,PopulationFixture.Dilithium(origin)+PopulationFixture.Dilithium(yard)+consumed);
        Assert.Equal(desired,JsonSerializer.Serialize(game.Colonies.Select(c=>c.DesiredStockpiles)));
    }
    [Fact] public void SameTrancheDemandProtectsDonorOwnWorkAndNeverImportsForeignOrUnknownSupply()
    {
        var scenario=VerticalSliceFixture.Scenario();var game=scenario.InitialState;var earth=game.Colonies!.Single(c=>c.ColonyId=="colony.sol");var mars=game.Colonies!.Single(c=>c.ColonyId=="colony.mars");
        foreach(var c in game.Colonies!){c.DesiredStockpiles!.Clear();c.Food=1000;c.Stability=10000;}
        game.Fleets!.Clear();PopulationFixture.SetDilithium(earth,3);PopulationFixture.SetDilithium(mars,0);
        var demand=new Dictionary<string,IReadOnlyList<ResourceAmountDto>>{
            [earth.ColonyId]=new List<ResourceAmountDto>{new(){ResourceDefinitionId="resource.dilithium",Amount=2}},
            [mars.ColonyId]=new List<ResourceAmountDto>{new(){ResourceDefinitionId="resource.dilithium",Amount=4}}};
        PopulationLogistics.Execute(game,scenario.Definitions,demand);Assert.Equal(2,PopulationFixture.Dilithium(earth));Assert.Equal(1,PopulationFixture.Dilithium(mars));
        var foreign=game.Colonies.Single(c=>c.OwnerCivilizationId=="civ.cardassian");PopulationFixture.SetDilithium(foreign,100);
        PopulationLogistics.Execute(game,scenario.Definitions,demand);Assert.Equal(100,PopulationFixture.Dilithium(foreign));Assert.Equal(1,PopulationFixture.Dilithium(mars));
    }
}
