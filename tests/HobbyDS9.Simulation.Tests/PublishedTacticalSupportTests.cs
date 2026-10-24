using System.Globalization;
using System.Text.Json;
using HobbyDS9.Application;
using HobbyDS9.Contracts;
using HobbyDS9.Contracts.ShipDesign;
using Xunit;
namespace HobbyDS9.Simulation.Tests;
public sealed class PublishedTacticalSupportTests
{
    // Explicitly test-only source catalogue and support semantics; never live campaign content.
    public sealed class Fixture {public ShipDesignCatalogueDto Catalogue {get;set;}=null!;public ShipDesignAccessDto Access {get;set;}=null!;}
    internal static Fixture Load()=>JsonSerializer.Deserialize<Fixture>(File.ReadAllText(Path.Combine(AppContext.BaseDirectory,"scenarios/ship-design-fixtures/fixture.json")))!;
    static ShipDesigner Designer(Fixture f)=>ShipDesignApplication.Create(f.Catalogue).Designer!;
    internal static ShipClassDraftDto Draft(Fixture f,bool beam=false,string? extra=null)
    {
        var ids=new List<string>{"component.reactor.v1","component.life-support.v1","component.command.v1","component.propulsion.v1"};if(beam){ids.Add("component.energy-bank.v1");ids.Add("component.escort-sensors.v1");}if(extra!=null)ids.Add(extra);
        return new(){ClassId="class.test.support",DisplayName="Test-only supported utility",HullVersionId="hull.utility.v1",RoleId="role.escort",EmbarkedCrew=30,
            Installations=ids.Select(id=>new ShipInstallationDto {InstallationId="fit."+id,ComponentVersionId=id,Quantity=1}).ToList(),
            Profiles=Enum.GetValues<OperatingProfileKind>().Select(p=>new ShipOperatingProfileDto {Kind=p,Allocations=ids.Select(id=>{
                var full=f.Catalogue.Components!.Single(c=>c.VersionId==id).Modes!.Single(m=>m.Profile==p).FullPower;
                return new ProfilePowerAllocationDto {InstallationId="fit."+id,RequestedPower=full,AllocatedPower=full};}).ToList()}).ToList()};
    }
    internal static TacticalSupportCatalogueDto Support(Fixture f)
    {
        var hull=f.Catalogue.Hulls!.Single(h=>h.VersionId=="hull.utility.v1");var mount=hull.Mounts!.Single(m=>m.Kind==MountKind.EnergyBank);
        var beam=f.Catalogue.Components!.Single(c=>c.VersionId=="component.energy-bank.v1");
        return new(){FormatVersion=2,SchemaId=PublishedTacticalSupportIdentity.Schema,CatalogueId="support.test.utility",Revision=1,SourceCompilerId=ShipDesignVersions.Compiler,
            BalanceStatus="provisional-test-only",CloakPolicyId=PublishedTacticalSupportIdentity.NoCloak,
            Hulls=new(){new(){HullVersionId=hull.VersionId,Armor=0,TacticalRole=TacticalShipRole.Line,SupportedRoleIds=new(){"role.escort","role.surveyor"},
                Capabilities=new(){Speed=30,Agility=20,Sensors=20,Command=20,Cohesion=30},Mounts=new(){new(){SourceMountId=mount.Id,SourceCoverageTag=mount.CoverageTag,Coverage=TacticalArc.Fore,Slots=(int)mount.Count}}}},
            Components=new(){new(){ComponentVersionId="component.reactor.v1",Kind=TacticalComponentSupportKind.Reactor},new(){ComponentVersionId="component.life-support.v1",Kind=TacticalComponentSupportKind.LifeSupport},
                new(){ComponentVersionId="component.command.v1",Kind=TacticalComponentSupportKind.Command},new(){ComponentVersionId="component.propulsion.v1",Kind=TacticalComponentSupportKind.Propulsion},
                new(){ComponentVersionId="component.escort-sensors.v1",Kind=TacticalComponentSupportKind.TargetingSensors,TacticalSensorRating=40},
                new(){ComponentVersionId=beam.VersionId,Kind=TacticalComponentSupportKind.Beam,Beam=new(){SourceMountId=mount.Id,SlotsRequired=(int)beam.Mounts!.Single().Count,Coverage=TacticalArc.Fore,Damage=7,
                    AccuracyBps=6500,TrackingBps=4200,OpportunitiesPerRound=2,CadenceRounds=2,InitialNextFireRound=1}}}};
    }
    static PublishedShipBlueprint Publish(Fixture f,ShipClassDraftDto d)
    {var r=Designer(f).Publish(d,f.Access,"blueprint.test.support",1);Assert.True(r.Published,JsonSerializer.Serialize(r.Preview.Issues));return r.Blueprint!;}
    static PublishedTacticalSupportResult Evaluate(Fixture f,PublishedShipBlueprint p,TacticalSupportCatalogueDto s)=>PublishedTacticalSupportApplication.Evaluate(Designer(f).Catalogue,p,f.Access,s);
    static void Rejected(PublishedTacticalSupportResult r,TacticalSupportIssueCode? code=null)
    {Assert.False(r.IsSupportedByCompiler);Assert.Null(r.Fit);Assert.NotEmpty(r.Issues);if(code!=null)Assert.Contains(r.Issues,x=>x.Code==code);}
    static ShipDesignPreview PreviewCopy(ShipDesignPreview p,ShipDesignCostSnapshot? cost,IEnumerable<ProfilePerformance>? profiles=null,IEnumerable<DesignIssue>? issues=null)=>
        new(issues??p.Issues,p.CapacityUsed,p.CapacityLimit,p.RequiredCrew,p.ReactorPower,p.DistributionLimit,p.Mounts,cost,profiles??p.Profiles,p.Compiled,p.Envelopes);
    static PublishedShipBlueprint Copy(PublishedShipBlueprint p,ShipDesignPreview? performance=null,IEnumerable<ComponentVersionSnapshot>? components=null,HullVersionSnapshot? hull=null)=>
        new(p.BlueprintId,p.Revision,p.Fit,hull??p.Hull,components??p.Components,performance??p.Performance);
    [Fact] public void UnarmedDerivationKeepsExactOrdinaryCostMovementAndExplicitNoCloakZeroStockPolicy()
    {
        var f=Load();var p=Publish(f,Draft(f));var s=Support(f);var before=JsonSerializer.Serialize(s);var r=Evaluate(f,p,s);Assert.True(r.IsSupportedByCompiler,JsonSerializer.Serialize(r.Issues));var fit=r.Fit!;
        Assert.Equal(p.Performance.Compiled!.MaxHull,fit.MaxHull);Assert.Equal(p.Performance.Compiled.MovementBudget,fit.MovementBudget);Assert.Equal(p.Performance.Cost!.Credits,fit.Cost.Credits);
        Assert.Equal(p.Performance.Cost.Industry,fit.Cost.Industry);Assert.Equal(p.BlueprintId,fit.BlueprintId);Assert.Equal(p.Revision,fit.BlueprintRevision);
        Assert.False(fit.CloakCapable);Assert.Equal(0,fit.CloakConcurrentPowerRequired);Assert.Equal(PublishedTacticalSupportIdentity.NoCloak,fit.CloakPolicyId);
        Assert.Empty(fit.Weapons);Assert.Empty(fit.Mounts);Assert.Equal(Enum.GetValues<TacticalSector>(),fit.ZeroShieldSectors);Assert.Equal(0,fit.ShieldRechargePerRound);
        Assert.Equal(0,fit.EssentialReservations.Single(x=>x.System==TacticalEssentialSystem.Targeting).AllocatedPower);Assert.Equal(before,JsonSerializer.Serialize(s));
    }
    [Fact] public void BeamUsesAuthoredSemanticsAndPreservesEntireRequiredCommandAllocation()
    {
        var f=Load();var p=Publish(f,Draft(f,true));var r=Evaluate(f,p,Support(f));Assert.True(r.IsSupportedByCompiler,JsonSerializer.Serialize(r.Issues));var fit=r.Fit!;var w=Assert.Single(fit.Weapons);
        Assert.Equal((7L,6500,4200,2,2,1),(w.Damage,w.AccuracyBps,w.TrackingBps,w.OpportunitiesPerRound,w.CadenceRounds,w.InitialNextFireRound));
        Assert.Equal(TacticalWeaponClass.Beam,w.Class);Assert.Equal("",w.AmmunitionDefinitionId);Assert.Equal(0,w.MagazineCapacity);Assert.Equal(0,w.InitialAmmunition);Assert.True(w.Powered);
        var combat=p.Performance.Profiles.Single(x=>x.Kind==OperatingProfileKind.Combat);
        Assert.Equal(combat.AllocatedPower,fit.EssentialReservations.Sum(x=>x.AllocatedPower)+w.AllocatedPower);
        Assert.Equal(6,fit.EssentialReservations.Single(x=>x.System==TacticalEssentialSystem.Targeting).AllocatedPower);Assert.Equal(40,fit.Capabilities.Sensors);
        var command=fit.EssentialReservations.Single(x=>x.System==TacticalEssentialSystem.Command);
        var originalCommand=p.Components.Single(c=>c.Kind==ComponentKind.Command).Modes.Single(m=>m.Profile==OperatingProfileKind.Combat);
        Assert.Equal(4,command.AllocatedPower);Assert.Equal(originalCommand.FullPower,command.RequiredPower);Assert.Equal(originalCommand.MinimumPower,command.RequiredPower);
    }
    [Theory][InlineData("missing")][InlineData("shield")][InlineData("sensor")]
    public void NoInstalledComponentMayBeSilentlyIgnoredOrReclassified(string kind)
    {
        var f=Load();var s=Support(f);var d=Draft(f,false,kind=="shield"?"component.small-shield.v1":kind=="sensor"?"component.survey-sensors.v1":null);
        if(kind=="missing")s.Components!.RemoveAll(x=>x.Kind==TacticalComponentSupportKind.Propulsion);
        Rejected(Evaluate(f,Publish(f,d),s),TacticalSupportIssueCode.MissingMapping);
    }
    [Fact] public void UnavailableSourceVersionsAndWrongSourceKindsFailClosed()
    {
        var f=Load();var p=Publish(f,Draft(f));var s=Support(f);f.Access.AvailableVersionIds!.Remove("component.reactor.v1");Rejected(Evaluate(f,p,s),TacticalSupportIssueCode.OrdinaryDesignRejected);
        f=Load();s=Support(f);s.Components![0].Kind=TacticalComponentSupportKind.Beam;Rejected(Evaluate(f,Publish(f,Draft(f)),s),TacticalSupportIssueCode.InvalidMetadata);
    }
    [Fact] public void OrdinaryPhysicalMountSuccessDoesNotBypassStricterTacticalAggregateSlotLimit()
    {
        var f=Load();var d=Draft(f,true);d.Installations!.Add(new(){InstallationId="fit.second-beam",ComponentVersionId="component.energy-bank.v1",Quantity=1});
        foreach(var profile in d.Profiles!) {var original=profile.Allocations!.Single(a=>a.InstallationId.Contains("energy-bank"));profile.Allocations!.Add(new(){InstallationId="fit.second-beam",RequestedPower=original.RequestedPower,AllocatedPower=original.AllocatedPower});}
        var p=Publish(f,d);var s=Support(f);Assert.True(Evaluate(f,p,s).IsSupportedByCompiler);
        s.Hulls![0].Mounts![0].Slots=1;Rejected(Evaluate(f,p,s),TacticalSupportIssueCode.TacticalValidation);
    }
    [Fact] public void QuantityAndSourceAccessFailuresStayDetachedAndRetainOrdinaryDiagnostics()
    {
        var f=Load();var d=Draft(f);d.Installations!.Single(i=>i.ComponentVersionId=="component.command.v1").Quantity=2;
        foreach(var profile in d.Profiles!){var a=profile.Allocations!.Single(a=>a.InstallationId.Contains("command"));a.RequestedPower*=2;a.AllocatedPower*=2;}
        Rejected(Evaluate(f,Publish(f,d),Support(f)),TacticalSupportIssueCode.UnsupportedQuantity);
        f=Load();var p=Publish(f,Draft(f));f.Access.AvailableVersionIds!.Remove("component.command.v1");var r=Evaluate(f,p,Support(f));
        Assert.Contains(r.OrdinaryIssues,i=>i.Code==DesignIssueCode.VersionUnavailable);Assert.Null(r.Fit);
    }
    [Fact] public void LegalPartialV1BeamPowerIsNotAcceptedAsFullTacticalPower()
    {
        var f=Load();var d=Draft(f,true);var a=d.Profiles!.Single(x=>x.Kind==OperatingProfileKind.Combat).Allocations!.Single(x=>x.InstallationId.Contains("energy-bank"));a.AllocatedPower=4;
        var p=Publish(f,d);Rejected(Evaluate(f,p,Support(f)),TacticalSupportIssueCode.CombatPower);
    }
    [Theory][InlineData("slots")][InlineData("coverage")][InlineData("requirement")][InlineData("unknown")]
    public void ExplicitBeamMountMustFitTheActualPhysicalSourceAndTacticalSlotLimits(string mutation)
    {
        var f=Load();var s=Support(f);var mount=s.Hulls![0].Mounts![0];var b=s.Components!.Single(x=>x.Beam!=null).Beam!;
        if(mutation=="slots")mount.Slots=3;else if(mutation=="coverage")b.Coverage=TacticalArc.Aft;else if(mutation=="requirement")b.SlotsRequired=2;else b.SourceMountId="mount.invented";
        Rejected(Evaluate(f,Publish(f,Draft(f,true)),s),TacticalSupportIssueCode.MountBinding);
    }
    [Fact] public void ArmedTargetingRequiresAnExplicitInstalledSensorMapping()
    {var f=Load();var s=Support(f);s.Components!.RemoveAll(c=>c.Kind==TacticalComponentSupportKind.TargetingSensors);Rejected(Evaluate(f,Publish(f,Draft(f,true)),s),TacticalSupportIssueCode.MissingMapping);}
    [Theory][InlineData("schema")][InlineData("compiler")][InlineData("cloak")][InlineData("missing-capability")][InlineData("duplicate")][InlineData("role")]
    public void MissingOrAmbiguousSupportSemanticsAreNeverDefaulted(string mutation)
    {
        var f=Load();var s=Support(f);if(mutation=="schema")s.SchemaId="unsupported";else if(mutation=="compiler")s.SourceCompilerId="compiler.other";
        else if(mutation=="cloak")s.CloakPolicyId=null;else if(mutation=="missing-capability")s.Hulls![0].Capabilities!.Sensors=null;
        else if(mutation=="duplicate")s.Components!.Add(s.Components[0]);else s.Hulls![0].SupportedRoleIds=new(){"role.surveyor"};
        Rejected(Evaluate(f,Publish(f,Draft(f,true)),s));
    }
    [Fact] public void PublicPublicationConstructorDoesNotAuthorizeForgedPerformanceOrDifferentFrozenSources()
    {
        var f=Load();var p=Publish(f,Draft(f));var perf=p.Performance;var forged=new ShipDesignPreview(perf.Issues,perf.CapacityUsed,perf.CapacityLimit,perf.RequiredCrew,perf.ReactorPower,perf.DistributionLimit,perf.Mounts,perf.Cost,perf.Profiles,
            new CompiledShipPerformance(999,perf.Compiled!.MovementBudget,new(){ShieldCapacity=perf.Compiled.Combat.ShieldCapacity,LongRangeAttack=perf.Compiled.Combat.LongRangeAttack,MediumRangeAttack=perf.Compiled.Combat.MediumRangeAttack,CloseRangeAttack=perf.Compiled.Combat.CloseRangeAttack,AccuracyBps=perf.Compiled.Combat.AccuracyBps,EvasionBps=perf.Compiled.Combat.EvasionBps}),perf.Envelopes);
        Rejected(Evaluate(f,new PublishedShipBlueprint(p.BlueprintId,p.Revision,p.Fit,p.Hull,p.Components,forged),Support(f)),TacticalSupportIssueCode.IdentityMismatch);
        f.Catalogue.Components!.Single(x=>x.VersionId=="component.command.v1").DisplayName="Changed exact version";
        Rejected(Evaluate(f,p,Support(f)),TacticalSupportIssueCode.IdentityMismatch);
    }
    [Fact] public void ExplicitSourceEncodingRejectsForgedCostComponentPowerAndSourceVersionIdentity()
    {
        var f=Load();var p=Publish(f,Draft(f));var cost=p.Performance.Cost!;
        var changedCost=new ShipDesignCostSnapshot(new(){Credits=cost.Credits+1,Industry=cost.Industry,Maintenance=cost.Maintenance,
            Resources=cost.Resources.Select(r=>new ResourceAmountDto {ResourceDefinitionId=r.ResourceDefinitionId,Amount=r.Amount}).ToList()});
        Rejected(Evaluate(f,Copy(p,PreviewCopy(p.Performance,changedCost)),Support(f)),TacticalSupportIssueCode.IdentityMismatch);
        var power=JsonSerializer.Deserialize<ComponentVersionDto>(JsonSerializer.Serialize(f.Catalogue.Components!.Single(c=>c.Kind==ComponentKind.Reactor)))!;power.PowerSupply++;
        Rejected(Evaluate(f,Copy(p,components:p.Components.Where(c=>c.Kind!=ComponentKind.Reactor).Concat(new[]{new ComponentVersionSnapshot(power)})),Support(f)),TacticalSupportIssueCode.IdentityMismatch);
        var hull=JsonSerializer.Deserialize<HullVersionDto>(JsonSerializer.Serialize(f.Catalogue.Hulls!.Single()))!;hull.VersionId="hull.test.substituted";
        Rejected(Evaluate(f,Copy(p,hull:new HullVersionSnapshot(hull)),Support(f)),TacticalSupportIssueCode.IdentityMismatch);
        var profiles=p.Performance.Profiles.Select(q=>new ProfilePerformance(q.Kind,q.AvailablePower,q.InstalledPeakPower,q.RequestedPower,q.AllocatedPower,
            q.Installations.Select(i=>new InstallationPowerPreview(i.InstallationId,i.FullPower,i.MinimumPower,i.RequestedPower,i.AllocatedPower+(q.Kind==OperatingProfileKind.Combat?1:0),i.Essential,i.Outputs)),q.Outputs));
        Rejected(Evaluate(f,Copy(p,PreviewCopy(p.Performance,p.Performance.Cost,profiles)),Support(f)),TacticalSupportIssueCode.IdentityMismatch);
    }
    [Theory][InlineData("fit")][InlineData("hull")][InlineData("cost")][InlineData("profiles")]
    public void MalformedNullablePublicPublicationSnapshotsRejectWithoutDereferencingNull(string missing)
    {
        var f=Load();var p=Publish(f,Draft(f));var malformed=missing=="fit"?new PublishedShipBlueprint(p.BlueprintId,p.Revision,null!,p.Hull,p.Components,p.Performance):
            missing=="hull"?new PublishedShipBlueprint(p.BlueprintId,p.Revision,p.Fit,null!,p.Components,p.Performance):
            missing=="cost"?Copy(p,PreviewCopy(p.Performance,null)):Copy(p,PreviewCopy(p.Performance,p.Performance.Cost,Array.Empty<ProfilePerformance>()));
        Rejected(Evaluate(f,malformed,Support(f)));
    }
    [Theory][InlineData(false)][InlineData(true)]
    public void EntireEvaluationSharesEncodedWorkAndUtf8ByteBudgets(bool multibyte)
    {
        var f=Load();f.Catalogue.Components!.Single(c=>c.Kind==ComponentKind.Command).DisplayName=new string(multibyte?'界':'x',multibyte?24000:50000);
        // Ordinary design validation accepts these names; fingerprint admission is separately bounded.
        var p=Publish(f,Draft(f));Rejected(Evaluate(f,p,Support(f)),TacticalSupportIssueCode.InvalidPublication);
    }
    [Fact] public void MutationCannotChangeReturnedFitAndChangedMetadataChangesItsSemanticIdentity()
    {
        var f=Load();var s=Support(f);var p=Publish(f,Draft(f,true));var fit=Evaluate(f,p,s).Fit!;
        s.Hulls![0].Capabilities!.Speed=99;s.Components!.Single(x=>x.Beam!=null).Beam!.Damage=123;
        Assert.Equal(30,fit.Capabilities.Speed);Assert.Equal(7,fit.Weapons.Single().Damage);
        var changed=Evaluate(f,p,s).Fit!;Assert.NotEqual(fit.SupportHash,changed.SupportHash);Assert.Equal(fit.SourceHash,changed.SourceHash);Assert.NotEqual(fit.FitId,changed.FitId);
        Assert.All(typeof(SupportedPublishedTacticalFit).GetProperties(),x=>Assert.False(x.CanWrite));
    }
    [Fact] public void SemanticIdentityIsDeterministicAcrossCatalogueOrderingAndCultures()
    {
        var f=Load();var s=Support(f);var p=Publish(f,Draft(f,true));var first=Evaluate(f,p,s).Fit!;var original=CultureInfo.CurrentCulture;
        try{foreach(var culture in new[]{"en-US","nl-NL","tr-TR"}){CultureInfo.CurrentCulture=CultureInfo.GetCultureInfo(culture);s.Components!.Reverse();s.Hulls![0].SupportedRoleIds!.Reverse();f.Catalogue.Components!.Reverse();
            var next=Evaluate(f,p,s);Assert.True(next.IsSupportedByCompiler);Assert.Equal(first.SupportHash,next.Fit!.SupportHash);Assert.Equal(first.SourceHash,next.Fit.SourceHash);Assert.Equal(first.FitId,next.Fit.FitId);}}
        finally{CultureInfo.CurrentCulture=original;}
    }
}
