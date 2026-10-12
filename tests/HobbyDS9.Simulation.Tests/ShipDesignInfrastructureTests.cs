using System.Reflection;
using System.Text.Json;
using HobbyDS9.Application;
using HobbyDS9.Contracts;
using HobbyDS9.Contracts.ShipDesign;
using Xunit;
namespace HobbyDS9.Simulation.Tests;

public sealed class ShipDesignInfrastructureTests
{
    public sealed class Fixture
    {
        public ShipDesignCatalogueDto Catalogue { get; set; } = null!;
        public ShipDesignAccessDto Access { get; set; } = null!;
        public List<ShipClassDraftDto> Drafts { get; set; } = null!;
    }
    private static Fixture Load() => JsonSerializer.Deserialize<Fixture>(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "scenarios/ship-design-fixtures/fixture.json")))!;
    private static ShipDesigner Designer(Fixture f)
    {
        var created = ShipDesignApplication.Create(f.Catalogue);
        Assert.Empty(created.Issues);
        return Assert.IsType<ShipDesigner>(created.Designer);
    }
    private static ComponentVersionDto Component(Fixture f, string name) => f.Catalogue.Components!.Single(c => c.VersionId == "component." + name + ".v1");
    private static ShipClassDraftDto Draft(Fixture f, string name = "escort") => f.Drafts.Single(d => d.ClassId == "class." + name);
    private static ShipOperatingProfileDto Profile(ShipClassDraftDto d, OperatingProfileKind kind = OperatingProfileKind.Combat) => d.Profiles!.Single(p => p.Kind == kind);
    private static ProfilePowerAllocationDto Power(ShipClassDraftDto d, string name, OperatingProfileKind kind = OperatingProfileKind.Combat) => Profile(d, kind).Allocations!.Single(a => a.InstallationId == "fit." + name);
    private static string Json<T>(T data) => JsonSerializer.Serialize(data);
    private static void Has(ShipDesignPreview preview, DesignIssueCode code)
    {
        Assert.False(preview.IsValid);
        Assert.Null(preview.Compiled);
        Assert.Contains(preview.Issues, i => i.Code == code && i.Severity == DesignIssueSeverity.Error);
    }
    private static void Remove(ShipClassDraftDto d, string name)
    {
        d.Installations!.RemoveAll(i => i.InstallationId == "fit." + name);
        foreach (var p in d.Profiles!)
            p.Allocations!.RemoveAll(a => a.InstallationId == "fit." + name);
    }
    [Fact]
    public void AuthoredSurveyorAndEscortShareHullAndCompileActualPoweredOutputsAndCosts()
    {
        var f = Load();
        var designer = Designer(f);
        var before = Json(f);
        var survey = designer.Preview(Draft(f, "surveyor"), f.Access);
        var escort = designer.Preview(Draft(f), f.Access);
        Assert.True(survey.IsValid, Json(survey.Issues));
        Assert.True(escort.IsValid, Json(escort.Issues));
        Assert.Equal(100, survey.CapacityUsed);
        Assert.Equal(100, escort.CapacityUsed);
        Assert.Equal(30, escort.Compiled!.MaxHull);
        Assert.Equal(4, escort.Compiled.MovementBudget);
        Assert.Equal((20L, 12L, 15L, 18L, 7500, 1000), (escort.Compiled.Combat.ShieldCapacity, escort.Compiled.Combat.LongRangeAttack, escort.Compiled.Combat.MediumRangeAttack, escort.Compiled.Combat.CloseRangeAttack, escort.Compiled.Combat.AccuracyBps, escort.Compiled.Combat.EvasionBps));
        var combat = escort.Profiles.Single(p => p.Kind == OperatingProfileKind.Combat);
        Assert.Equal((60L, 66L, 66L, 60L), (combat.AvailablePower, combat.InstalledPeakPower, combat.RequestedPower, combat.AllocatedPower));
        Assert.Equal(12, combat.Installations.Single(i => i.InstallationId == "fit.energy-bank").AllocatedPower);
        Assert.Equal(52, survey.Profiles.Single(p => p.Kind == OperatingProfileKind.Survey).AllocatedPower);
        Assert.Equal(100, survey.Profiles.Single(p => p.Kind == OperatingProfileKind.Survey).Output(ShipOutputKind.Science));
        Assert.Equal((34L, 56L, 12L), (escort.Cost!.Credits, escort.Cost.Industry, escort.Cost.Maintenance));
        Assert.Equal((30L, 50L, 10L), (survey.Cost!.Credits, survey.Cost.Industry, survey.Cost.Maintenance));
        Assert.Equal(1, Assert.Single(escort.Cost.Resources).Amount);
        Assert.Equal(new[] { 2L, 1L }, escort.Mounts.OrderBy(m => m.Kind).Select(m => m.Used));
        Assert.All(survey.Mounts, m => Assert.Equal(0, m.Used));
        Assert.All(escort.Envelopes, e => Assert.Equal(1, e.PackagesUsed));
        Assert.Equal(before, Json(f));
    }
    [Fact]
    public void UnarmedSurveyorIsLegalAndWarningsDoNotBlockPublication()
    {
        var f = Load();
        var designer = Designer(f);
        var result = designer.Publish(Draft(f, "surveyor"), f.Access, "blueprint.surveyor.v1", 1);
        Assert.True(result.Published);
        Assert.Contains(result.Preview.Issues, i => i.Code == DesignIssueCode.Unarmed && i.Severity == DesignIssueSeverity.Warning);
        Assert.Contains(result.Preview.Issues, i => i.Code == DesignIssueCode.IntegrationGap);
        Assert.Equal(0, result.Blueprint!.Performance.Compiled!.Combat.LongRangeAttack);
        Draft(f, "surveyor").RoleId = "role.escort";
        Assert.True(designer.Publish(Draft(f, "surveyor"), f.Access, "blueprint.weak-escort.v1", 1).Published);
        Assert.Contains(designer.Preview(Draft(f, "surveyor"), f.Access).Issues, i => i.Code == DesignIssueCode.RoleUnderperformance);
    }
    [Theory]
    [InlineData(ComponentKind.Reactor, "reactor")]
    [InlineData(ComponentKind.LifeSupport, "life-support")]
    [InlineData(ComponentKind.Command, "command")]
    [InlineData(ComponentKind.Propulsion, "propulsion")]
    public void MissingRequiredSystemIsHardInvalidity(ComponentKind kind, string name)
    {
        var f = Load();
        Remove(Draft(f), name);
        var p = Designer(f).Preview(Draft(f), f.Access);
        Has(p, DesignIssueCode.MissingRequiredSystem);
        Assert.Contains(p.Issues, i => i.Message.Contains(kind.ToString()));
    }
    [Theory]
    [InlineData("capacity", DesignIssueCode.CapacityExceeded)]
    [InlineData("mount-count", DesignIssueCode.MountExceeded)]
    [InlineData("mount-size", DesignIssueCode.MountExceeded)]
    [InlineData("mount-type", DesignIssueCode.MountExceeded)]
    [InlineData("coverage", DesignIssueCode.MountExceeded)]
    [InlineData("reactor-size", DesignIssueCode.EnvelopeExceeded)]
    [InlineData("propulsion-size", DesignIssueCode.EnvelopeExceeded)]
    [InlineData("reactor-count", DesignIssueCode.EnvelopeExceeded)]
    [InlineData("propulsion-count", DesignIssueCode.EnvelopeExceeded)]
    [InlineData("hull-tag", DesignIssueCode.IncompatibleHull)]
    public void SharedCapacityAndMountEnvelopeLimitsAreHardErrors(string error, DesignIssueCode code)
    {
        var f = Load();
        var h = f.Catalogue.Hulls!.Single();
        switch (error)
        {
            case "capacity":
                h.Capacity = 99;
                break;
            case "mount-count":
                h.Mounts![0].Count = 1;
                break;
            case "mount-size":
                Component(f, "energy-bank").Mounts![0].SizeRating = 3;
                break;
            case "mount-type":
                Component(f, "energy-bank").Mounts![0].Kind = MountKind.TorpedoLauncher;
                break;
            case "coverage":
                Component(f, "energy-bank").Mounts![0].CoverageTag = "coverage.aft";
                break;
            case "reactor-size":
                h.ReactorSizeLimit = 1;
                break;
            case "propulsion-size":
                h.PropulsionSizeLimit = 1;
                break;
            case "reactor-count":
                Draft(f).Installations!.Single(i => i.InstallationId == "fit.reactor").Quantity = 2;
                break;
            case "propulsion-count":
                Draft(f).Installations!.Single(i => i.InstallationId == "fit.propulsion").Quantity = 2;
                break;
            case "hull-tag":
                h.Tags!.Clear();
                break;
        }
        Has(Designer(f).Preview(Draft(f), f.Access), code);
    }
    [Theory]
    [InlineData("crew")]
    [InlineData("embarked")]
    [InlineData("computer")]
    [InlineData("load")]
    [InlineData("numeric")]
    [InlineData("tag")]
    [InlineData("unpowered-tag")]
    public void PoweredDependenciesCannotBeBypassed(string error)
    {
        var f = Load();
        switch (error)
        {
            case "crew":
                foreach (var m in Component(f, "life-support").Modes!)
                    m.Outputs!.Single().Amount = 1;
                break;
            case "embarked":
                Draft(f).EmbarkedCrew = 1;
                break;
            case "computer":
                Component(f, "launcher").ComputerDemand = 100;
                break;
            case "load":
                f.Catalogue.Hulls!.Single().DesignLoad = 201;
                break;
            case "numeric":
                Component(f, "energy-bank").Dependencies!.Single().Amount = 100;
                break;
            case "tag":
                Component(f, "launcher").RequiredComponentTags![0] = "system-tag.absent";
                break;
            case "unpowered-tag":
                var a = Power(Draft(f), "escort-sensors");
                a.AllocatedPower = 0;
                break;
        }
        var preview = Designer(f).Preview(Draft(f), f.Access);
        Has(preview, DesignIssueCode.DependencyUnsatisfied);
        if (error != "embarked")
            Assert.Empty(preview.Profiles.Single(p => p.Kind == OperatingProfileKind.Combat).Outputs);
    }
    [Theory]
    [InlineData("essential", DesignIssueCode.EssentialPowerInsufficient)]
    [InlineData("supply", DesignIssueCode.AvailablePowerExceeded)]
    [InlineData("distribution", DesignIssueCode.AvailablePowerExceeded)]
    [InlineData("unsupported-partial", DesignIssueCode.InvalidPowerAllocation)]
    [InlineData("below-minimum", DesignIssueCode.InvalidPowerAllocation)]
    [InlineData("above-request", DesignIssueCode.InvalidPowerAllocation)]
    [InlineData("above-full", DesignIssueCode.InvalidPowerAllocation)]
    public void InfeasiblePowerNeverReportsMaximumPerformance(string error, DesignIssueCode code)
    {
        var f = Load();
        var d = Draft(f);
        switch (error)
        {
            case "essential":
                Power(d, "command").AllocatedPower = 0;
                break;
            case "supply":
                Component(f, "reactor").PowerSupply = 59;
                break;
            case "distribution":
                f.Catalogue.Hulls!.Single().PowerDistributionLimit = 59;
                break;
            case "unsupported-partial":
                foreach (var m in Component(f, "energy-bank").Modes!)
                {
                    m.Scaling = PowerScaling.Fixed;
                    m.MinimumPower = m.FullPower;
                }
                break;
            case "below-minimum":
                Power(d, "energy-bank").AllocatedPower = 1;
                break;
            case "above-request":
                Power(d, "launcher").RequestedPower = 5;
                break;
            case "above-full":
                Power(d, "launcher").RequestedPower = 9;
                break;
        }
        var p = Designer(f).Preview(d, f.Access);
        Has(p, code);
        var battle = p.Profiles.Single(p => p.Kind == OperatingProfileKind.Combat);
        Assert.Empty(battle.Outputs);
        Assert.All(battle.Installations, i => Assert.Empty(i.Outputs));
    }
    [Fact]
    public void SupportedPartialAndDisabledOptionalSystemsHaveOnlyTheirActualOutputs()
    {
        var f = Load();
        var d = Draft(f);
        Power(d, "escort-shield").AllocatedPower = 7;
        Power(d, "launcher").AllocatedPower = 0;
        var p = Designer(f).Preview(d, f.Access);
        Assert.True(p.IsValid);
        Assert.Equal(10, p.Compiled!.Combat.ShieldCapacity);
        Assert.Equal(6, p.Compiled.Combat.LongRangeAttack);
        Assert.Equal(12, p.Compiled.Combat.MediumRangeAttack);
        Assert.Equal(18, p.Compiled.Combat.CloseRangeAttack);
        Assert.DoesNotContain(p.Profiles.Single(p => p.Kind == OperatingProfileKind.Combat).Installations.Single(i => i.InstallationId == "fit.launcher").Outputs, o => o.Amount > 0);
        Power(d, "propulsion", OperatingProfileKind.Cruise).AllocatedPower = 15;
        p = Designer(f).Preview(d, f.Access);
        Assert.True(p.IsValid);
        Assert.Equal(2, p.Compiled!.MovementBudget);
        Assert.Equal(4, Designer(f).Catalogue.Components.Single(c => c.Kind == ComponentKind.Propulsion).Modes.Single(m => m.Profile == OperatingProfileKind.Cruise).Outputs.Single(o => o.Kind == ShipOutputKind.MovementBudget).Amount);
    }
    [Fact]
    public void MultipleIntensivePackagesDoNotMultiplyAccuracyOrMovement()
    {
        var f = Load();
        var d = Draft(f, "surveyor");
        f.Catalogue.Hulls!.Single().Capacity = 200;
        f.Catalogue.Hulls!.Single().MaxPropulsionPackages = 2;
        Component(f, "reactor").PowerSupply = 200;
        f.Catalogue.Hulls!.Single().PowerDistributionLimit = 200;
        d.Installations!.Single(i => i.InstallationId == "fit.propulsion").Quantity = 2;
        foreach (var p in d.Profiles!)
        {
            var a = p.Allocations!.Single(a => a.InstallationId == "fit.propulsion");
            a.RequestedPower *= 2;
            a.AllocatedPower *= 2;
        }
        var result = Designer(f).Preview(d, f.Access);
        Assert.True(result.IsValid);
        Assert.Equal(4, result.Compiled!.MovementBudget);
        Assert.Equal(1000, result.Compiled.Combat.EvasionBps);
        Assert.Equal(7500, result.Compiled.Combat.AccuracyBps);
    }
    [Theory]
    [InlineData("capacity")]
    [InlineData("cost")]
    [InlineData("power")]
    [InlineData("mounts")]
    public void OverflowIsStructuredAndNeverMutatesInputsOrPublishes(string what)
    {
        var f = Load();
        var c = Component(f, "energy-bank");
        if (what == "capacity")
            c.Capacity = long.MaxValue;
        else if (what == "cost")
            c.Cost!.Credits = long.MaxValue;
        else if (what == "power")
            c.Modes!.Single(m => m.Profile == OperatingProfileKind.Combat).FullPower = long.MaxValue;
        else
            c.Mounts!.Single().Count = long.MaxValue;
        var designer = Designer(f);
        var before = Json(f);
        var r = designer.Publish(Draft(f), f.Access, "blueprint.overflow", 1);
        Has(r.Preview, DesignIssueCode.ArithmeticOverflow);
        Assert.False(r.Published);
        Assert.Equal(before, Json(f));
    }
    [Fact]
    public void ImmutablePublishedBlueprintPinsVersionsFitAllocationsCostAndPerformance()
    {
        var f = Load();
        var designer = Designer(f);
        var published = designer.Publish(Draft(f), f.Access, "blueprint.escort.v1", 1).Blueprint!;
        Assert.NotNull(published);
        var frozen = Json(published);
        var catalogue = Json(designer.Catalogue);
        Component(f, "energy-bank").Modes!.Single(m => m.Profile == OperatingProfileKind.Combat).Outputs![0].Amount = 999;
        f.Catalogue.Hulls!.Single().Capacity = 999;
        Draft(f).DisplayName = "Caller edit";
        Power(Draft(f), "launcher").AllocatedPower = 0;
        f.Access.AvailableVersionIds!.Clear();
        Assert.Equal(frozen, Json(published));
        Assert.Equal(catalogue, Json(designer.Catalogue));
        Assert.Throws<NotSupportedException>(() => ((IList<ComponentVersionSnapshot>)published.Components).Clear());
        Assert.Throws<NotSupportedException>(() => ((IList<ComponentOperatingModeSnapshot>)published.Components[0].Modes).Clear());
        Assert.Throws<NotSupportedException>(() => ((IList<ProfilePerformance>)published.Performance.Profiles).Clear());
        var valid = Load();
        var improvedHull = JsonSerializer.Deserialize<HullVersionDto>(Json(valid.Catalogue.Hulls!.Single()))!;
        improvedHull.VersionId = "hull.utility.v2";
        improvedHull.VersionNumber = 2;
        improvedHull.ParentVersionId = "hull.utility.v1";
        improvedHull.Capacity = 104;
        valid.Catalogue.Hulls!.Add(improvedHull);
        var newer = Designer(valid);
        Assert.Equal(104, newer.Catalogue.Hulls.Single(h => h.VersionId == "hull.utility.v2").Capacity);
        Assert.Equal(100, published.Hull.Capacity);
    }
    [Fact]
    public void RepeatAndReorderedCatalogueFitProfilesAllocationsOutputsAndResourcesCompileIdentically()
    {
        var f = Load();
        var left = Designer(f).Publish(Draft(f), f.Access, "blueprint.escort.v1", 1);
        f.Catalogue.HullFamilies!.Reverse();
        f.Catalogue.Hulls!.Reverse();
        f.Catalogue.Components!.Reverse();
        f.Catalogue.Roles!.Reverse();
        f.Access.AvailableVersionIds!.Reverse();
        foreach (var h in f.Catalogue.Hulls)
        {
            h.Tags!.Reverse();
            h.Mounts!.Reverse();
            h.Cost!.Resources!.Reverse();
        }
        foreach (var c in f.Catalogue.Components)
        {
            c.Modes!.Reverse();
            c.Mounts!.Reverse();
            c.Tags!.Reverse();
            c.Dependencies!.Reverse();
            foreach (var m in c.Modes)
                m.Outputs!.Reverse();
            c.Cost!.Resources!.Reverse();
        }
        foreach (var r in f.Catalogue.Roles)
            r.Recommendations!.Reverse();
        var draft = Draft(f);
        draft.Installations!.Reverse();
        draft.Profiles!.Reverse();
        foreach (var p in draft.Profiles)
            p.Allocations!.Reverse();
        var designer = Designer(f);
        for (int i = 0; i < 10; i++)
            Assert.Equal(Json(left), Json(designer.Publish(draft, f.Access, "blueprint.escort.v1", 1)));
    }
    [Theory]
    [InlineData("hull")]
    [InlineData("component")]
    public void UnavailableVersionsBlockPublication(string kind)
    {
        var f = Load();
        f.Access.AvailableVersionIds!.Remove(kind == "hull" ? "hull.utility.v1" : "component.reactor.v1");
        var result = Designer(f).Publish(Draft(f), f.Access, "blueprint.escort", 1);
        Has(result.Preview, DesignIssueCode.VersionUnavailable);
        Assert.False(result.Published);
    }
    [Theory]
    [InlineData("id")]
    [InlineData("revision")]
    public void PublicationIdentityErrorsAreStructured(string error)
    {
        var f = Load();
        var result = Designer(f).Publish(Draft(f), f.Access, error == "id" ? "BAD" : "blueprint.escort", error == "revision" ? 0 : 1);
        Assert.False(result.Published);
        Assert.Null(result.Preview.Compiled);
        Assert.Contains(result.Preview.Issues, i => i.Severity == DesignIssueSeverity.Error);
    }
    [Fact]
    public void ApplicationApiExposesNoSimulationTypesOrMutableResultProperties()
    {
        foreach (var type in new[] { typeof(ShipDesignApplication), typeof(ShipDesigner), typeof(ShipDesignerCreationResult) })
        foreach (var m in type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
            Assert.All(m.GetParameters().Select(p => p.ParameterType).Append(m.ReturnType), t => Assert.DoesNotContain("HobbyDS9.Simulation", t.FullName ?? ""));
        foreach (var t in typeof(ShipDesignPreview).Assembly.GetTypes().Where(t => t.Namespace == "HobbyDS9.Contracts.ShipDesign" && !t.Name.EndsWith("Dto") && !t.IsEnum))
            Assert.All(t.GetProperties(), p => Assert.Null(p.SetMethod));
    }
    [Fact]
    public void PreviewPublishDoNotTouchExistingCampaignStateRevisionRngOrEntityAllocator()
    {
        var scenario = VerticalSliceFixture.Scenario(false, weighted: true);
        var session = CampaignApplication.StartScenario(scenario);
        var before = Json(session.ExportSave());
        var f = Load();
        var designer = Designer(f);
        foreach (var draft in f.Drafts)
        {
            Assert.True(designer.Preview(draft, f.Access).IsValid);
            Assert.True(designer.Publish(draft, f.Access, "blueprint." + draft.RoleId + ".v1", 1).Published);
        }
        Assert.Equal(before, Json(session.ExportSave()));
    }
    [Theory]
    [InlineData("null")]
    [InlineData("families-null")]
    [InlineData("null-entry")]
    [InlineData("duplicate-version")]
    [InlineData("duplicate-family-revision")]
    [InlineData("missing-family")]
    [InlineData("lineage")]
    [InlineData("component-lineage")]
    [InlineData("modes-null")]
    [InlineData("duplicate-mode")]
    [InlineData("unknown-mode")]
    [InlineData("unknown-scaling")]
    [InlineData("unknown-kind")]
    [InlineData("unknown-output")]
    [InlineData("invalid-bps")]
    [InlineData("negative-cost")]
    [InlineData("duplicate-resource")]
    [InlineData("zero-power-output")]
    [InlineData("free-reactor-output")]
    [InlineData("wrong-movement-provider")]
    [InlineData("missing-essential-output")]
    [InlineData("missing-essential-flag")]
    [InlineData("duplicate-mount")]
    [InlineData("missing-compatible-family")]
    public void InvalidCatalogueReturnsStructuredIssuesAndDoesNotMutateInput(string error)
    {
        var f = Load();
        var c = Component(f, "energy-bank");
        var h = f.Catalogue.Hulls!.Single();
        switch (error)
        {
            case "families-null":
                f.Catalogue.HullFamilies = null;
                break;
            case "null-entry":
                f.Catalogue.Components!.Add(null!);
                break;
            case "duplicate-version":
                c.VersionId = h.VersionId;
                break;
            case "duplicate-family-revision":
                var other = Component(f, "launcher");
                other.FamilyId = c.FamilyId;
                other.VersionNumber = c.VersionNumber;
                break;
            case "missing-family":
                h.FamilyId = "hull-family.absent";
                break;
            case "lineage":
                h.ParentVersionId = h.VersionId;
                break;
            case "component-lineage":
                c.ParentVersionId = c.VersionId;
                break;
            case "modes-null":
                c.Modes = null;
                break;
            case "duplicate-mode":
                c.Modes![1].Profile = c.Modes[0].Profile;
                break;
            case "unknown-mode":
                c.Modes![0].Profile = (OperatingProfileKind)99;
                break;
            case "unknown-scaling":
                c.Modes![0].Scaling = (PowerScaling)99;
                break;
            case "unknown-kind":
                c.Kind = (ComponentKind)99;
                break;
            case "unknown-output":
                c.Modes![1].Outputs![0].Kind = (ShipOutputKind)99;
                break;
            case "invalid-bps":
                Component(f, "command").Modes![1].Outputs!.Single(o => o.Kind == ShipOutputKind.AccuracyBps).Amount = 10001;
                break;
            case "negative-cost":
                c.Cost!.Credits = -1;
                break;
            case "duplicate-resource":
                Component(f, "reactor").Cost!.Resources!.Add(new()
                {
                    ResourceDefinitionId = "resource.dilithium",
                    Amount = 1
                });
                break;
            case "zero-power-output":
                c.Modes![0].Outputs!.Add(new()
                {
                    Kind = ShipOutputKind.LongRangeAttack,
                    Amount = 10
                });
                break;
            case "free-reactor-output":
                Component(f, "reactor").Modes![0].Outputs!.Add(new()
                {
                    Kind = ShipOutputKind.ShieldCapacity,
                    Amount = 99
                });
                break;
            case "wrong-movement-provider":
                c.Modes![1].Outputs!.Add(new()
                {
                    Kind = ShipOutputKind.MovementBudget,
                    Amount = 99
                });
                break;
            case "missing-essential-output":
                Component(f, "command").Modes![0].Outputs!.RemoveAll(o => o.Kind == ShipOutputKind.ComputerSupport);
                break;
            case "missing-essential-flag":
                Component(f, "propulsion").Modes![0].Essential = false;
                break;
            case "duplicate-mount":
                h.Mounts!.Add(h.Mounts[0]);
                break;
            case "missing-compatible-family":
                c.CompatibleHullFamilyIds!.Clear();
                break;
        }
        var input = error == "null" ? null : f.Catalogue;
        var before = Json(input);
        var result = ShipDesignApplication.Create(input);
        Assert.Null(result.Designer);
        Assert.NotEmpty(result.Issues);
        Assert.All(result.Issues, i => Assert.Equal(DesignIssueSeverity.Error, i.Severity));
        Assert.Equal(before, Json(input));
    }
    [Theory]
    [InlineData("null")]
    [InlineData("class-id")]
    [InlineData("name")]
    [InlineData("hull")]
    [InlineData("role")]
    [InlineData("quantity")]
    [InlineData("installation-null")]
    [InlineData("duplicate-installation")]
    [InlineData("unknown-component")]
    [InlineData("missing-profile")]
    [InlineData("duplicate-profile")]
    [InlineData("unknown-profile")]
    [InlineData("allocation-null")]
    [InlineData("missing-allocation")]
    [InlineData("duplicate-allocation")]
    [InlineData("negative-power")]
    [InlineData("access-null")]
    [InlineData("access-ids-null")]
    [InlineData("duplicate-access")]
    [InlineData("unknown-access")]
    public void MalformedDraftAndAccessAreOrdinaryNonMutatingRejections(string error)
    {
        var f = Load();
        var d = Draft(f);
        var a = Power(d, "launcher");
        switch (error)
        {
            case "class-id":
                d.ClassId = "Class.BAD";
                break;
            case "name":
                d.DisplayName = " ";
                break;
            case "hull":
                d.HullVersionId = "hull.absent";
                break;
            case "role":
                d.RoleId = "role.absent";
                break;
            case "quantity":
                d.Installations![0].Quantity = 0;
                break;
            case "installation-null":
                d.Installations = null;
                break;
            case "duplicate-installation":
                d.Installations!.Add(d.Installations[0]);
                break;
            case "unknown-component":
                d.Installations![0].ComponentVersionId = "component.absent";
                break;
            case "missing-profile":
                d.Profiles!.RemoveAt(0);
                break;
            case "duplicate-profile":
                d.Profiles!.Add(d.Profiles[0]);
                break;
            case "unknown-profile":
                d.Profiles![0].Kind = (OperatingProfileKind)99;
                break;
            case "allocation-null":
                Profile(d).Allocations = null;
                break;
            case "missing-allocation":
                Profile(d).Allocations!.Remove(a);
                break;
            case "duplicate-allocation":
                Profile(d).Allocations!.Add(a);
                break;
            case "negative-power":
                a.AllocatedPower = -1;
                break;
            case "access-ids-null":
                f.Access.AvailableVersionIds = null;
                break;
            case "duplicate-access":
                f.Access.AvailableVersionIds!.Add(f.Access.AvailableVersionIds[0]);
                break;
            case "unknown-access":
                f.Access.AvailableVersionIds!.Add("component.absent");
                break;
        }
        var designer = Designer(f);
        var before = Json(f);
        var result = designer.Publish(error == "null" ? null : d, error == "access-null" ? null : f.Access, "blueprint.test", 1);
        Assert.False(result.Published);
        Assert.False(result.Preview.IsValid);
        Assert.NotEmpty(result.Preview.Issues);
        Assert.Equal(before, Json(f));
    }
    [Fact]
    public void SupportedVersionLineageIsFrozenAndOldDeployableVersionsRemainSelectable()
    {
        var f = Load();
        var previous = Json(f.Catalogue.Hulls!.Single());
        var next = JsonSerializer.Deserialize<HullVersionDto>(previous)!;
        next.VersionId = "hull.utility.v2";
        next.VersionNumber = 2;
        next.ParentVersionId = "hull.utility.v1";
        next.Capacity = 104;
        f.Catalogue.Hulls!.Add(next);
        f.Access.AvailableVersionIds!.Add(next.VersionId);
        var designer = Designer(f);
        Assert.Equal(100, designer.Publish(Draft(f), f.Access, "blueprint.old", 1).Blueprint!.Hull.Capacity);
        Draft(f).HullVersionId = next.VersionId;
        Assert.Equal(104, designer.Publish(Draft(f), f.Access, "blueprint.new", 2).Blueprint!.Hull.Capacity);
        Assert.Equal(previous, Json(f.Catalogue.Hulls![0]));
    }
    [Fact]
    public void PreviewOnlyOperationalRangeWarnsWithoutRedefiningMovementOrSupply()
    {
        var f = Load();
        var support = Component(f, "mission-support").Modes!.Single(m => m.Profile == OperatingProfileKind.Survey);
        support.FullPower = 1;
        support.MinimumPower = 1;
        support.Outputs!.Add(new()
        {
            Kind = ShipOutputKind.OperationalRange,
            Amount = 20
        });
        var a = Power(Draft(f, "surveyor"), "mission-support", OperatingProfileKind.Survey);
        a.RequestedPower = 1;
        a.AllocatedPower = 1;
        var preview = Designer(f).Preview(Draft(f, "surveyor"), f.Access);
        Assert.True(preview.IsValid);
        Assert.Equal(20, preview.Profiles.Single(p => p.Kind == OperatingProfileKind.Survey).Output(ShipOutputKind.OperationalRange));
        Assert.Equal(4, preview.Compiled!.MovementBudget);
        Assert.Contains(preview.Issues, i => i.Code == DesignIssueCode.IntegrationGap && i.Message.Contains("OperationalRange"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    [InlineData(null)]
    public void StandaloneCatalogueFormatIsIndependentAndExplicit(int? version)
    {
        var f = Load();
        f.Catalogue.FormatVersion = version;
        Assert.Null(ShipDesignApplication.Create(f.Catalogue).Designer);
        Assert.Equal(13, FoundationVersions.SaveFormatVersion);
        Assert.Equal(9, FoundationVersions.ContentFormatVersion);
    }
    [Fact]
    public void SupportedPowerGridHasNoFreePerformanceAndRepeatsInStableOrder()
    {
        var f = Load();
        var designer = Designer(f);
        var d = Draft(f);
        foreach (var shield in new[] { 0, 7, 14 })
        foreach (var bank in new[] { 0, 8, 12, 16 })
        foreach (var launcher in new[] { 0, 2, 6, 8 })
        {
            Power(d, "escort-shield").AllocatedPower = shield;
            Power(d, "energy-bank").AllocatedPower = bank;
            Power(d, "launcher").AllocatedPower = launcher;
            var preview = designer.Preview(d, f.Access);
            Assert.Equal(Json(preview), Json(designer.Preview(d, f.Access)));
            if (28 + shield + bank + launcher > 60)
            {
                Has(preview, DesignIssueCode.AvailablePowerExceeded);
                Assert.Empty(preview.Profiles.Single(p => p.Kind == OperatingProfileKind.Combat).Outputs);
                continue;
            }
            Assert.True(preview.IsValid, Json(preview.Issues));
            var actual = preview.Compiled!.Combat;
            Assert.Equal(shield * 20 / 14, actual.ShieldCapacity);
            Assert.Equal(bank / 2 + launcher, actual.LongRangeAttack);
            Assert.Equal(bank + launcher / 2, actual.MediumRangeAttack);
            Assert.Equal(bank * 3 / 2, actual.CloseRangeAttack);
            Assert.Equal(7500, actual.AccuracyBps);
        }
    }
    [Fact]
    public void MountBestFitReservesConstrainedGroupsBeforeWildcardAndLargerMountsAcceptSmallerPackages()
    {
        var f = Load();
        var hull = f.Catalogue.Hulls!.Single();
        hull.Mounts![0].Count = 1;
        hull.Mounts.Add(new()
        {
            Id = "mount.energy.aft",
            Kind = MountKind.EnergyBank,
            SizeRating = 1,
            Count = 1,
            CoverageTag = "coverage.aft"
        });
        var variant = JsonSerializer.Deserialize<ComponentVersionDto>(Json(Component(f, "energy-bank")))!;
        variant.VersionId = "component.energy-bank.v2";
        variant.VersionNumber = 2;
        variant.ParentVersionId = "component.energy-bank.v1";
        variant.Mounts!.Single().CoverageTag = "";
        f.Catalogue.Components!.Add(variant);
        f.Access.AvailableVersionIds!.Add(variant.VersionId);
        var d = Draft(f);
        d.Installations!.Single(i => i.InstallationId == "fit.energy-bank").Quantity = 1;
        d.Installations!.Add(new()
        {
            InstallationId = "fit.a-wildcard",
            ComponentVersionId = variant.VersionId,
            Quantity = 1
        });
        foreach (var p in d.Profiles!)
        {
            var a = p.Allocations!.Single(a => a.InstallationId == "fit.energy-bank");
            a.RequestedPower /= 2;
            a.AllocatedPower /= 2;
            p.Allocations!.Add(new()
            {
                InstallationId = "fit.a-wildcard",
                RequestedPower = a.RequestedPower,
                AllocatedPower = a.AllocatedPower
            });
        }
        var preview = Designer(f).Preview(d, f.Access);
        Assert.True(preview.IsValid, Json(preview.Issues));
        Assert.All(preview.Mounts, m => Assert.Equal(1, m.Used));
        Assert.Equal(100, preview.CapacityUsed);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void PhysicalWeaponPackagesConsumeSpaceAndDeclareMounts(bool zeroCapacity)
    {
        var fixture = Load();
        var weapon = Component(fixture, "energy-bank");
        if (zeroCapacity) weapon.Capacity = 0;
        else weapon.Mounts!.Clear();
        var before = Json(fixture);
        var result = ShipDesignApplication.Create(fixture.Catalogue);
        Assert.Null(result.Designer);
        Assert.Contains(result.Issues, issue => issue.Code == (zeroCapacity ? DesignIssueCode.InvalidValue : DesignIssueCode.MissingField));
        Assert.Equal(before, Json(fixture));
    }
    [Fact]
    public void NamedResourceCostOverflowIsCheckedBeforePublication()
    {
        var fixture = Load();
        Component(fixture, "energy-bank").Cost!.Resources!.Add(new ResourceAmountDto { ResourceDefinitionId = "resource.dilithium", Amount = long.MaxValue });
        var designer = Designer(fixture);
        var before = Json(fixture);
        var result = designer.Publish(Draft(fixture), fixture.Access, "blueprint.resource-overflow", 1);
        Has(result.Preview, DesignIssueCode.ArithmeticOverflow);
        Assert.False(result.Published);
        Assert.Equal(before, Json(fixture));
    }

}
