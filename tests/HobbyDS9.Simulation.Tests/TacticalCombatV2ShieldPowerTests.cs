using HobbyDS9.Simulation;
using Xunit;
using static HobbyDS9.Simulation.Tests.TacticalCombatV2Fixture;

namespace HobbyDS9.Simulation.Tests;

public sealed class TacticalCombatV2ShieldPowerTests
{
    [Fact]
    public void ZeroRequiredAndAllocatedPowerCannotGrantPositiveRecharge()
    {
        var state = Encounter(); Ship(state).ShieldPowerRequired = Ship(state).ShieldPowerAllocated = 0;
        Ship(state).ShieldRechargePerRound = 1; var before = Json(state);
        Assert.Throws<ArgumentException>(() => Round(state)); Assert.Equal(before, Json(state));
    }
    [Fact]
    public void ZeroPowerPassiveShieldsWithoutRechargeRemainValid()
    {
        var state = Encounter(); Ship(state).ShieldPowerRequired = Ship(state).ShieldPowerAllocated = 0;
        TacticalCombatV2State.Validate(state); var result = Round(state);
        Assert.Equal(40, Ship(result.NextState).Shields[0].Current);
    }
}
