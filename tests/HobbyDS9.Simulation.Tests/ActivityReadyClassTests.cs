using System.Text.Json;
using HobbyDS9.Application;
using HobbyDS9.Contracts;
using HobbyDS9.Contracts.ShipDesign;
using Xunit;

namespace HobbyDS9.Simulation.Tests;

public sealed class ActivityReadyClassTests
{
    internal static T Read<T>(string name)=>JsonSerializer.Deserialize<T>(File.ReadAllText(Path.Combine(AppContext.BaseDirectory,"scenarios/activities-v1/"+name)))!;
    internal static ShipDesignCatalogueDto Catalogue()=>Read<ShipDesignCatalogueDto>("catalogue.json");
    internal static ShipClassDraftDto[] Ready()=>Read<ShipClassDraftDto[]>("ready-classes.json");
    internal static ActivityTacticalSupportCatalogueDto Support()=>Read<ActivityTacticalSupportCatalogueDto>("support.json");
    internal static ShipDesignAccessDto Access(ShipDesignCatalogueDto c)=>new(){AvailableVersionIds=c.Hulls!.Select(h=>h.VersionId).Concat(c.Components!.Select(m=>m.VersionId)).ToList()};
    static ShipDesigner Designer(ShipDesignCatalogueDto c)
    {var result=ShipDesignApplication.Create(c);Assert.NotNull(result.Designer);return result.Designer!;}
    internal static PublishedShipBlueprint Publish(ShipDesigner designer,ShipDesignCatalogueDto c,ShipClassDraftDto draft)
    {var result=designer.Publish(draft,Access(c),"blueprint.ready."+draft.ClassId.Substring(6,draft.ClassId.Length-9),1);
     Assert.True(result.Published,JsonSerializer.Serialize(result.Preview.Issues));return result.Blueprint!;}
    static ActivitySupportResult Evaluate(ShipDesigner d,ShipDesignCatalogueDto c,PublishedShipBlueprint p,ActivityTacticalSupportCatalogueDto s)
        =>PublishedActivitySupportApplication.Evaluate(d.Catalogue,p,Access(c),s);
    [Fact] public void ThreeRealReadyClassesCompileLegallyAndHaveGenuineActivityCombatAndCostTradeoffs()
    {
        var catalogue=Catalogue();var designer=Designer(catalogue);var ready=Ready();var support=Support();var receipt=new List<object>();var ships=new List<ActivitySupportedShip>();
        foreach(var draft in ready)
        {var publication=Publish(designer,catalogue,draft);var result=Evaluate(designer,catalogue,publication,support);Assert.True(result.Supported,JsonSerializer.Serialize(result.Issues));
         var ship=result.Ship!;ships.Add(ship);Assert.InRange(publication.Performance.CapacityUsed!.Value,1,publication.Hull.Capacity);
         Assert.True(publication.Performance.Profiles.All(p=>p.AllocatedPower<=publication.Performance.ReactorPower));
         Assert.Equal(Enum.GetValues<TacticalSector>().Length,ship.Tactical.ZeroShieldSectors.Count);Assert.False(ship.Tactical.CloakCapable);
         Assert.Equal(ActivityTacticalSupportIdentity.Compiler,ship.Tactical.CompilerId);Assert.Equal(support.CatalogueId,ship.Tactical.SupportCatalogueId);
         Assert.Equal(support.Revision,ship.Tactical.SupportCatalogueRevision);Assert.Equal(support.CatalogueId,ship.Tactical.ProvenanceId);
         receipt.Add(new{draft.ClassId,draft.DisplayName,draft.HullVersionId,publication.Hull.Capacity,publication.Performance.CapacityUsed,publication.Hull.MaxHull,
             publication.Performance.Cost,ship.StudyWork,ship.ContactCapacity,ship.Tactical.MovementBudget,ship.Tactical.FitId,ship.SupportHash,
             weapons=ship.Tactical.Weapons.Count,profiles=publication.Performance.Profiles});}
        Assert.Equal(3,ready.Select(d=>d.HullVersionId).Distinct().Count());Assert.Equal(new[]{3,4,1},ships.Select(s=>s.StudyWork));Assert.Equal(new[]{1,3,0},ships.Select(s=>s.ContactCapacity));
        Assert.True(ships[1].Tactical.Cost.Credits>ships[0].Tactical.Cost.Credits);Assert.Equal(new[]{1,2,3},ships.Select(s=>s.Tactical.Weapons.Count));
        Assert.Equal(3,ships.Select(s=>s.Tactical.MaxHull).Distinct().Count());Assert.Equal(3,ships.Select(s=>s.Tactical.FitId).Distinct().Count());
        var path=Environment.GetEnvironmentVariable("HOBBYDS9_ACTIVITY_FIT_RECEIPT");if(path!=null){using var file=new FileStream(path,FileMode.CreateNew);JsonSerializer.Serialize(file,receipt,new JsonSerializerOptions{WriteIndented=true});}
    }
    [Fact] public void V2StillRejectsActivityModulesAndV3DoesNotGrantCombatPowerOrOutputs()
    {
        var c=Catalogue();var d=Designer(c);var draft=Ready()[0];var p=Publish(d,c,draft);var support=Support();
        var old=PublishedTacticalSupportApplication.Evaluate(d.Catalogue,p,Access(c),support.Tactical!);Assert.False(old.IsSupportedByCompiler);
        var module=draft.Installations!.First(i=>i.ComponentVersionId.Contains("activity-lab"));
        draft.Profiles!.Single(x=>x.Kind==OperatingProfileKind.Survey).Allocations!.Single(a=>a.InstallationId==module.InstallationId).AllocatedPower--;
        var preview=d.Publish(draft,Access(c),"blueprint.bad.power",1);Assert.False(preview.Published);
        var good=Ready()[0];var source=c.Components!.Single(m=>m.VersionId==module.ComponentVersionId);var combat=source.Modes!.Single(m=>m.Profile==OperatingProfileKind.Combat);
        combat.FullPower=1;combat.MinimumPower=1;
        var changedDesigner=Designer(c);var changed=Publish(changedDesigner,c,good);Assert.False(Evaluate(changedDesigner,c,changed,support).Supported);
    }
    [Fact] public void WholeV3MetadataAndCompilerBindIdentityAndCannotForgePublicationOrUnlocks()
    {
        var c=Catalogue();var d=Designer(c);var p=Publish(d,c,Ready()[0]);var support=Support();var baseline=Evaluate(d,c,p,support).Ship!;
        support.Modules!.Reverse();support.Tactical!.Hulls!.Reverse();Assert.Equal(baseline.Tactical.FitId,Evaluate(d,c,p,support).Ship!.Tactical.FitId);
        support.Modules.Single(m=>m.ComponentVersionId=="component.activity-lab.standard.v1").StudyWork++;
        var changed=Evaluate(d,c,p,support);Assert.True(changed.Supported);Assert.NotEqual(baseline.SupportHash,changed.Ship!.SupportHash);Assert.NotEqual(baseline.Tactical.FitId,changed.Ship.Tactical.FitId);
        var access=Access(c);access.AvailableVersionIds!.Remove("component.activity-lab.standard.v1");Assert.False(PublishedActivitySupportApplication.Evaluate(d.Catalogue,p,access,Support()).Supported);
        var cost=p.Performance.Cost!;var forged=new ShipDesignCostSnapshot(new ShipDesignCostDto{Credits=cost.Credits+1,Industry=cost.Industry,Maintenance=cost.Maintenance,Resources=new()});
        var perf=p.Performance;var fake=new ShipDesignPreview(perf.Issues,perf.CapacityUsed,perf.CapacityLimit,perf.RequiredCrew,perf.ReactorPower,perf.DistributionLimit,perf.Mounts,forged,perf.Profiles,perf.Compiled,perf.Envelopes);
        Assert.False(Evaluate(d,c,new PublishedShipBlueprint(p.BlueprintId,1,p.Fit,p.Hull,p.Components,fake),Support()).Supported);
        support.CompilerId="published-tactical-support-compiler-v2";Assert.False(Evaluate(d,c,p,support).Supported);
        Assert.Equal(3,baseline.StudyWork);Assert.Equal(1,baseline.ContactCapacity); // Later caller mutation changes no returned capability.
        var copy=baseline.CopySupport();copy.Modules!.Clear();copy.Tactical!.Hulls!.Clear();Assert.NotEmpty(baseline.CopySupport().Modules!);Assert.Equal(3,baseline.CopySupport().Tactical!.Hulls!.Count);
    }
    [Fact] public void DisabledLabAndLowReactorNeverReceiveScientificWorkForFree()
    {
        var c=Catalogue();var d=Designer(c);var draft=Ready()[0];var lab=draft.Installations!.Single(i=>i.ComponentVersionId.Contains("activity-lab"));
        var allocation=draft.Profiles!.Single(p=>p.Kind==OperatingProfileKind.Survey).Allocations!.Single(a=>a.InstallationId==lab.InstallationId);
        allocation.RequestedPower=0;allocation.AllocatedPower=0;
        var p=Publish(d,c,draft);Assert.False(Evaluate(d,c,p,Support()).Supported); // Ordinary optional disable is legal, supported study is not.
        c.Components!.Single(m=>m.VersionId=="component.reactor.v1").PowerSupply=20;
        var weak=Designer(c).Publish(Ready()[0],Access(c),"blueprint.underpowered",1);Assert.False(weak.Published);
        Assert.Contains(weak.Preview.Issues,i=>i.Code==DesignIssueCode.AvailablePowerExceeded);
    }
    [Fact] public void MalformedPublicInputsAndMissingMappingsFailClosed()
    {
        var c=Catalogue();var d=Designer(c);var p=Publish(d,c,Ready()[0]);
        foreach(var mutate in new Action<ActivityTacticalSupportCatalogueDto>[] {
            s=>s.Modules=null,s=>s.Modules!.Add(null!),s=>s.Modules![0].ComponentVersionId=null!,
            s=>s.Tactical!.Hulls=null,s=>s.Tactical!.Components!.Add(null!),s=>s.Tactical!.Hulls![0].Capabilities=null,
            s=>s.Modules!.RemoveAll(m=>m.ComponentVersionId=="component.activity-lab.standard.v1")})
        {var s=Support();mutate(s);Assert.False(Evaluate(d,c,p,s).Supported);}
        var missing=new PublishedShipBlueprint(p.BlueprintId,1,null!,p.Hull,p.Components,p.Performance);
        Assert.False(Evaluate(d,c,missing,Support()).Supported);
    }
}
