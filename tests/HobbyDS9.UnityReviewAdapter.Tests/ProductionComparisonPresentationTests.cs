using HobbyDS9.Contracts;
using HobbyDS9.UnityReview;
using Xunit;
namespace HobbyDS9.UnityReviewAdapter.Tests;
public sealed class ProductionComparisonPresentationTests
{
    [Fact]public void MissingComparisonAndMissingWeaponDataNeverBecomeZeroOrUnarmed()
    {
        Assert.Equal("Exploration unavailable · Combat unavailable",ProductionComparisonPresentation.Summary(null));
        var partial=new ActivityBuildComparisonDto{StudyWork=0,ShieldCapacity=0};var text=ProductionComparisonPresentation.Summary(partial);
        Assert.Contains("Study work 0",text);Assert.Contains("Shields 0",text);Assert.Contains("Hull unavailable",text);Assert.Contains("Weapons unavailable",text);Assert.DoesNotContain("Unarmed",text);
        Assert.Contains("Weapons · unavailable",ProductionComparisonPresentation.Details(partial));
    }
    [Fact]public void EmptySupportedWeaponListIsUnarmedAndSeparateCapabilityValuesStayExact()
    {
        var comparison=new ActivityBuildComparisonDto{StudyWork=0,ContactCapacity=0,MaxHull=30,Armor=0,ShieldCapacity=0,Speed=30,MovementBudget=6,Weapons=new()};
        var text=ProductionComparisonPresentation.Summary(comparison);
        Assert.Contains("Study work 0 · Contact 0",text);Assert.Contains("Hull 30 · Armor 0 · Shields 0",text);Assert.Contains("Tactical speed 30 · Movement budget 6",text);Assert.Contains("Unarmed",text);
        Assert.Contains("Weapons · Unarmed",ProductionComparisonPresentation.Details(comparison));Assert.DoesNotContain("supply",text,StringComparison.OrdinalIgnoreCase);
    }
    [Fact]public void ActualWeaponCadenceAccuracyTrackingAndAllocationStayDistinct()
    {
        var comparison=new ActivityBuildComparisonDto{StudyWork=3,ContactCapacity=1,Weapons=new(){new(){Class=TacticalWeaponClass.Beam,Coverage=TacticalArc.Fore|TacticalArc.Port,Damage=7,OpportunitiesPerRound=2,CadenceRounds=2,AccuracyBps=6550,TrackingBps=4200,AllocatedPower=3,RequiredPower=4,Powered=false}}};
        var text=ProductionComparisonPresentation.Summary(comparison);var details=ProductionComparisonPresentation.Details(comparison);
        Assert.Contains("1 weapon package · 0 powered",text);Assert.Contains("Weapon 1 · Beam · Fore, Port · Damage 7",details);Assert.Contains("Opportunities / round · 2 · Cadence · 2 rounds",details);
        Assert.Contains("Accuracy · 65.5% · Tracking · 42%",details);Assert.Contains("Power allocated / required · 3 / 4 · Unpowered",details);Assert.DoesNotContain(details,row=>row.Contains("hit chance",StringComparison.OrdinalIgnoreCase));
    }
}
