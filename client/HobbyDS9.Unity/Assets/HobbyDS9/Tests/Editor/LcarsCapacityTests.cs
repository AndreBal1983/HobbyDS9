#nullable enable
using System.Linq;
using NUnit.Framework;
using UnityEngine.UIElements;
namespace HobbyDS9.UnityClient.Tests
{
    public sealed class LcarsCapacityTests
    {
        [Test]public void SixtyBudgetTenUsedHasExactUnitPositions()
        {
            var meter=new LcarsCapacityMeter("Allocated power",10,60);
            Assert.That(meter.BlockCount,Is.EqualTo(60));Assert.That(meter.UnitsPerBlock,Is.EqualTo(1));
            Assert.That(meter.Query(className:"capacity-filled").ToList().Count,Is.EqualTo(10));
            Assert.That(meter.Query(className:"capacity-hollow").ToList().Count,Is.EqualTo(50));
            StringAssert.Contains("10 / 60",meter.Q<Label>("capacity-exact").text);
        }
        [Test]public void CompactnessPreservesTwelvePositionsAndThreeFreed()
        {
            var meter=new LcarsCapacityMeter("Module hull space",9,12,12,false);
            Assert.That(meter.BlockCount,Is.EqualTo(12));Assert.That(meter.Query(className:"capacity-filled").ToList().Count,Is.EqualTo(9));
            Assert.That(meter.Query(className:"capacity-hollow").ToList().Count,Is.EqualTo(3));
            StringAssert.Contains("12 → 9",meter.tooltip);StringAssert.Contains("3 less",meter.tooltip);StringAssert.Contains("freed positions",meter.tooltip);
        }
        [Test]public void OverflowAndLargeValuesStayExactAndBounded()
        {
            var overflow=new LcarsCapacityMeter("Requested",65,60);Assert.That(overflow.BlockCount,Is.EqualTo(65));StringAssert.Contains("5 over budget",overflow.tooltip);
            var large=new LcarsCapacityMeter("Allocated",125,1001);Assert.That(large.BlockCount,Is.LessThanOrEqualTo(120));Assert.That(large.UnitsPerBlock,Is.EqualTo(9));StringAssert.Contains("125 / 1001",large.tooltip);
            var extreme=new LcarsCapacityMeter("Limit",long.MaxValue,long.MaxValue);Assert.That(extreme.BlockCount,Is.LessThanOrEqualTo(120));
            var invalid=new LcarsCapacityMeter("Invalid",long.MinValue,60,long.MaxValue,false);Assert.That(invalid.BlockCount,Is.EqualTo(0));
        }
    }
}
