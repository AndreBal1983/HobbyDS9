using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using HobbyDS9.TacticalUxPrototype.Tests.Prototype;
using Xunit;

namespace HobbyDS9.TacticalUxPrototype.Tests
{
    public sealed class PackagePresentationTests
    {
        private sealed class Port : IPrototypeActions
        {
            public int Requests { get; private set; }
            public void RecordRound(RoundIntent intent) { Requests++; }
            public void RecordCommittedSave(OperationIntent intent) { Requests++; }
            public void RecordCompletion(OperationIntent intent) { Requests++; }
        }
        private static (TacticalPresenter Ui, Port Port) Make(IEnumerable<OwnPackage>? packages)
        {
            var own = new OwnShip("own", "A", "authored fixture", 90, 100,
                new long[] { 3, 13, 40, 5 }, new long[] { 40, 40, 40, 40 }, packages, false, true);
            var b = new Boundary("battle", "human", 1, 8, BoundaryStatus.Ready,
                new[] { new FleetOrder("A", Maneuver.Hold, null) }, new[] { own },
                new[] { new VisibleShip("enemy", "X", "visible", 92, new long[] { 0, 5, 5, 0 }) });
            var port = new Port(); return (new TacticalPresenter(port, b), port);
        }
        private static string Text(TacticalPresenter ui) => string.Join("\n", ui.RenderText());

        [Fact] public void MissingPackageProjectionRemainsAbsentWithoutAggregateConversion()
        {
            var (ui, port) = Make(null);
            Assert.Null(Assert.Single(ui.Observation.OwnShips).Packages);
            Assert.Contains("DEPENDENCY: own per-package stock/readiness projection absent", Text(ui));
            Assert.DoesNotContain("Stock 0", Text(ui)); Assert.DoesNotContain("aggregate ammo", Text(ui));
            Assert.Equal(0, port.Requests);
        }
        [Fact] public void ExplicitEmptyPackageListIsNotAnUnknownProjection()
        {
            var (ui, port) = Make(Array.Empty<OwnPackage>());
            Assert.Contains("Supplied empty package list", Text(ui));
            Assert.DoesNotContain("per-package stock/readiness projection absent", Text(ui));
            Assert.Equal(0, port.Requests);
        }
        [Fact] public void SeparateMagazinesAndReadinessAreDisplayedWithoutPoolingOrPowerMath()
        {
            var packages = new[] {
                new OwnPackage("fore.launcher", "Torpedo", true, 3, 12, "Supplied: cooling until boundary 3"),
                new OwnPackage("aft.launcher", "Torpedo", true, 1, 4, "Supplied: unpowered") };
            var (ui, port) = Make(packages); var before = JsonSerializer.Serialize(ui.Observation);
            var text = Text(ui);
            Assert.Contains("fore.launcher Torpedo: Stock 3/12; Supplied: cooling until boundary 3", text);
            Assert.Contains("aft.launcher Torpedo: Stock 1/4; Supplied: unpowered", text);
            Assert.DoesNotContain("Stock 4/16", text);
            ui.ReplayReport(); ui.Resize(3440, 1440); Text(ui);
            Assert.Equal(before, JsonSerializer.Serialize(ui.Observation)); Assert.Equal(0, port.Requests);
        }
        [Theory] [InlineData(null, 12L)] [InlineData(7L, null)] [InlineData(null, null)]
        public void AbsentStockOrCapacityIsNeverDefaulted(long? ammo, long? capacity)
        {
            var (ui, port) = Make(new[] { new OwnPackage("launcher", "Torpedo", true, ammo, capacity, null) });
            var text = Text(ui);
            if (!ammo.HasValue) Assert.Contains("DEPENDENCY: ammunition absent", text);
            if (!capacity.HasValue) Assert.Contains("DEPENDENCY: capacity absent", text);
            Assert.Contains("DEPENDENCY: readiness reason absent", text);
            Assert.Equal(0, port.Requests);
        }
        [Fact] public void ZeroAndHugeStocksStayExactWithoutPercentOrTotalCalculation()
        {
            var (ui, port) = Make(new[] {
                new OwnPackage("zero", "Torpedo", true, 0, 0, "Supplied: unavailable"),
                new OwnPackage("huge", "Torpedo", true, long.MaxValue, long.MaxValue, "Supplied: powered") });
            Assert.Contains("zero Torpedo: Stock 0/0", Text(ui));
            Assert.Contains("Stock 9223372036854775807/9223372036854775807", Text(ui));
            Assert.DoesNotContain("%", Text(ui)); Assert.Equal(0, port.Requests);
        }
        [Fact] public void EnergyAmmunitionKindIsASuppliedFactRatherThanNameInference()
        {
            var (ui, port) = Make(new[] {
                new OwnPackage("energy", "Beam", false, null, null, "Supplied: ready"),
                new OwnPackage("unknown", "Torpedo", null, null, null, null) });
            Assert.Contains("energy Beam: No physical ammunition (supplied); Supplied: ready", Text(ui));
            Assert.Contains("DEPENDENCY: stock kind absent", Text(ui));
            Assert.Contains("DEPENDENCY: readiness reason absent", Text(ui));
            Assert.Equal(0, port.Requests);
        }
        [Fact] public void ReadinessIsOnlySuppliedTextNotDerivedFromRoundOrStock()
        {
            var (ui, port) = Make(new[] { new OwnPackage("fixture", "Torpedo", true, 0, 7, "Supplied: ready") });
            Assert.Contains("Stock 0/7; Supplied: ready", Text(ui)); // Deliberately inconsistent fixture: client must not invent a correction.
            Assert.DoesNotContain("out of ammo", Text(ui)); Assert.Equal(0, port.Requests);
        }
        [Fact] public void PackageFactsAreDetachedAndEnemyReservesStillCannotAppear()
        {
            var list = new List<OwnPackage> { new OwnPackage("own.package", "Torpedo", true, 2, 7, "Supplied: ready") };
            var (ui, port) = Make(list); list.Clear();
            var packages = Assert.Single(ui.Observation.OwnShips).Packages!;
            Assert.Single(packages);
            Assert.Throws<NotSupportedException>(() => ((IList<OwnPackage>)packages).Clear());
            var visible = JsonSerializer.Serialize(ui.Observation.VisibleShips);
            foreach (var field in new[] { "Packages", "Ammunition", "Readiness", "Orders", "MagazineCapacity" })
                Assert.DoesNotContain(field, visible);
            Assert.Equal(0, port.Requests);
        }
        [Fact] public void PackageFactsCannotEnterRoundOrderPayload()
        {
            var (ui, port) = Make(new[] { new OwnPackage("own.package", "Torpedo", true, 2, 7, "Supplied: ready") });
            var request = new RoundIntent(1, "test", ui.Observation, ui.Observation.StandingOrders);
            var json = JsonSerializer.Serialize(request);
            foreach (var field in new[] { "Packages", "Ammunition", "Readiness", "MagazineCapacity" }) Assert.DoesNotContain(field, json);
            Assert.Equal(0, port.Requests);
        }
        [Fact] public void SameFrozenBoundaryCannotSilentlyChangePackageReadinessOrCompleteness()
        {
            var (ui, port) = Make(null); var original = ui.Observation;
            var supplied = Make(new[] { new OwnPackage("new", "Torpedo", true, 5, 7, "Supplied: ready") }).Ui.Observation;
            Assert.False(ui.Refresh(supplied, 1)); Assert.Same(original, ui.Observation); Assert.Equal(0, port.Requests);
        }
    }
}
