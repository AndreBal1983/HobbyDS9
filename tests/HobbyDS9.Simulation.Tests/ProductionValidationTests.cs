using System.Text.Json;
using HobbyDS9.Contracts;
using HobbyDS9.Simulation;
using Xunit;
using static HobbyDS9.Simulation.Tests.VerticalSliceFixture;

namespace HobbyDS9.Simulation.Tests;

public sealed class ProductionValidationTests
{
    [Theory]
    [InlineData("missing-designs")]
    [InlineData("null-design")]
    [InlineData("duplicate-design")]
    [InlineData("invalid-design-id")]
    [InlineData("missing-label")]
    [InlineData("industry")]
    [InlineData("hull")]
    [InlineData("resource")]
    [InlineData("credits")]
    [InlineData("missing-prerequisites")]
    [InlineData("duplicate-prerequisites")]
    [InlineData("non-research-prerequisite")]
    [InlineData("missing-queue")]
    [InlineData("null-item")]
    [InlineData("duplicate-item")]
    [InlineData("duplicate-global-item")]
    [InlineData("missing-item-header")]
    [InlineData("design-cost-mismatch")]
    [InlineData("paid-cost-mismatch")]
    [InlineData("negative-progress")]
    [InlineData("complete-active-item")]
    [InlineData("non-head-progress")]
    [InlineData("unknown-design")]
    [InlineData("missing-ships")]
    [InlineData("missing-fleets")]
    [InlineData("empty-fleet")]
    [InlineData("duplicate-membership")]
    [InlineData("missing-membership")]
    [InlineData("missing-ship")]
    [InlineData("wrong-owner")]
    [InlineData("wrong-backlink")]
    [InlineData("hull-overflow")]
    [InlineData("zero-hull")]
    [InlineData("unknown-location")]
    [InlineData("bad-route")]
    [InlineData("missing-route")]
    [InlineData("missing-members")]
    [InlineData("old-save")]
    [InlineData("old-content")]
    public void InvalidContentOrPersistedProgressAndMembershipRejectWithoutMutation(string error)
    {
        var scenario = Scenario(); PopulationFixture.SetDilithium(scenario.InitialState.Colonies![0], 100);
        var s = Session(scenario); Assert.True(Enqueue(s).Accepted); Advance(s); Advance(s); Advance(s);
        Assert.True(Enqueue(s, "cmd.second").Accepted); Assert.True(Enqueue(s, "cmd.third").Accepted);
        var save = s.ExportSave(); var colony = save.GameState.Colonies!.Single(c => c.ColonyId == "colony.sol"); var item = colony.ProductionQueue![0];
        var ship = save.GameState.Ships!.Single(x => x.ShipId == "ship.federation-scout"); var fleet = save.GameState.Fleets!.Single(x => x.FleetId == ship.FleetId); var design = scenario.Definitions.ShipDesigns![0];
        switch (error)
        {
            case "missing-designs": scenario.Definitions.ShipDesigns = null; break;
            case "null-design": scenario.Definitions.ShipDesigns!.Add(null!); break;
            case "duplicate-design": scenario.Definitions.ShipDesigns!.Add(design); break;
            case "invalid-design-id": design.DefinitionId = "Bad"; break;
            case "missing-label": design.DisplayName = " "; break;
            case "industry": design.IndustryCost = 0; break;
            case "hull": design.MaxHull = 0; break;
            case "resource": design.ResourceCosts![0].Amount = -1; break;
            case "credits": design.CreditCost = -1; break;
            case "missing-prerequisites": design.RequiredResearchDefinitionIds = null; break;
            case "duplicate-prerequisites": design.RequiredResearchDefinitionIds = new() { "research.advanced-engineering", "research.advanced-engineering" }; break;
            case "non-research-prerequisite": design.RequiredResearchDefinitionIds!.Add("economy.baseline"); break;
            case "missing-queue": colony.ProductionQueue = null; break;
            case "null-item": colony.ProductionQueue!.Add(null!); break;
            case "duplicate-item": colony.ProductionQueue!.Add(item); break;
            case "duplicate-global-item": save.GameState.Colonies![0].ProductionQueue!.Add(item); break;
            case "missing-item-header": save.GameState.EntityIdentities.RemoveAll(e => e.EntityId == item.QueueItemId); break;
            case "design-cost-mismatch": item.RequiredIndustry++; break;
            case "paid-cost-mismatch": item.PaidCredits--; break;
            case "negative-progress": item.CompletedIndustry = -1; break;
            case "complete-active-item": item.CompletedIndustry = item.RequiredIndustry; break;
            case "non-head-progress": colony.ProductionQueue![1].CompletedIndustry = 1; break;
            case "unknown-design": item.ShipDesignDefinitionId = "ship-design.unknown"; break;
            case "missing-ships": save.GameState.Ships = null; break;
            case "missing-fleets": save.GameState.Fleets = null; break;
            case "empty-fleet": fleet.ShipIds!.Clear(); break;
            case "duplicate-membership": fleet.ShipIds!.Add(ship.ShipId); break;
            case "missing-membership": ship.FleetId = "fleet.absent"; break;
            case "missing-ship": fleet.ShipIds![0] = "ship.absent"; break;
            case "wrong-owner": ship.OwnerCivilizationId = "civ.cardassian"; break;
            case "wrong-backlink": ship.FleetId = "fleet.absent"; break;
            case "hull-overflow": ship.CurrentHull = 11; break;
            case "zero-hull": ship.CurrentHull = 0; break;
            case "unknown-location": fleet.LocationNodeId = "system.absent"; break;
            case "bad-route": fleet.RouteNodeIds!.Add("system.gamma-exit"); break;
            case "missing-route": fleet.RouteNodeIds = null; break;
            case "missing-members": fleet.ShipIds = null; break;
            case "old-save": save.SaveFormatVersion = 6; break;
            case "old-content": scenario.Definitions.ContentFormatVersion = 3; break;
        }
        var before = JsonSerializer.Serialize(new { save, scenario.Definitions });
        Assert.False(FoundationValidator.ValidateSave(save, scenario.Definitions, Compatibility(scenario)).IsValid);
        Assert.Throws<ArgumentException>(() => new SimulationSession(save, scenario.Definitions, Compatibility(scenario)));
        Assert.Equal(before, JsonSerializer.Serialize(new { save, scenario.Definitions }));
    }

    [Fact]
    public void RefundCapacityOverflowAndFailedAllocationAreAtomicAndOtherItemsUnaffected()
    {
        var scenario = Scenario(); PopulationFixture.SetDilithium(scenario.InitialState.Colonies![0], 100);
        var s = Session(scenario); Assert.True(Enqueue(s).Accepted); Assert.True(Enqueue(s, "cmd.second").Accepted);
        var save = s.ExportSave(); save.GameState.Civilizations!.Single(c => c.CivilizationId == "civ.federation").Credits = long.MaxValue;
        s = new(save, scenario.Definitions, Compatibility(scenario)); var before = Json(s);
        Assert.Throws<OverflowException>(() => Cancel(s, Colony(s).ProductionQueue![0].QueueItemId)); Assert.Equal(before, Json(s));
        save.GameState.Civilizations!.Single(c => c.CivilizationId == "civ.federation").Credits = 20;
        s = new(save, scenario.Definitions, Compatibility(scenario)); var second = Colony(s).ProductionQueue![1];
        Assert.True(Cancel(s, Colony(s).ProductionQueue![0].QueueItemId).Accepted);
        Assert.Equal(JsonSerializer.Serialize(second), JsonSerializer.Serialize(Assert.Single(Colony(s).ProductionQueue!)));
        save = s.ExportSave(); save.GameState.NextEntitySequence = ulong.MaxValue; s = new(save, scenario.Definitions, Compatibility(scenario)); before = Json(s);
        Assert.Throws<OverflowException>(() => Enqueue(s, "cmd.overflow")); Assert.Equal(before, Json(s));
    }
}
