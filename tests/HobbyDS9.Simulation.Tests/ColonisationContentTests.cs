using System.Text.Json;
using HobbyDS9.Application;
using HobbyDS9.Contracts.ShipDesign;
using Xunit;

namespace HobbyDS9.Simulation.Tests;

public sealed class ColonisationContentTests
{
    internal static T Read<T>(string name)=>JsonSerializer.Deserialize<T>(File.ReadAllText(Path.Combine(AppContext.BaseDirectory,"scenarios/colonisation-v1/"+name)))!;
    [Fact] public void FourActualReadyFitsAreOrdinaryLegalInAllThreeProfiles()
    {
        var c=Read<ShipDesignCatalogueDto>("catalogue.json");var fits=Read<ShipClassDraftDto[]>("ready-classes.json");
        var designer=ShipDesignApplication.Create(c).Designer!;Assert.NotNull(designer);Assert.Equal(4,fits.Length);
        var receipts=new List<object>();
        foreach(var draft in fits)
        {
            var p=designer.Publish(draft,ActivityReadyClassTests.Access(c),"blueprint.check."+draft.ClassId.Substring(6),1);
            Assert.True(p.Published,JsonSerializer.Serialize(new{draft.ClassId,p.Preview.Issues}));var b=p.Blueprint!;
            Assert.InRange(b.Performance.CapacityUsed!.Value,1,b.Hull.Capacity);
            Assert.Equal(new[]{OperatingProfileKind.Cruise,OperatingProfileKind.Combat,OperatingProfileKind.Survey},b.Performance.Profiles.Select(x=>x.Kind).OrderBy(x=>x));
            Assert.All(b.Performance.Profiles,x=>Assert.True(x.AllocatedPower<=x.AvailablePower));
            receipts.Add(new{draft.ClassId,draft.DisplayName,draft.HullVersionId,b.Hull.Capacity,b.Performance.CapacityUsed,b.Performance.RequiredCrew,b.Performance.Cost,b.Performance.Profiles});
        }
        var transport=fits.Single(f=>f.ClassId=="class.galaxy-colony-transport.v1");Assert.DoesNotContain(transport.Installations!,i=>i.ComponentVersionId.Contains("energy-bank")||i.ComponentVersionId.Contains("activity-lab")||i.ComponentVersionId.Contains("contact-suite"));
        Assert.Single(transport.Installations!,i=>i.ComponentVersionId=="component.settler-module.v1");
        var path=Environment.GetEnvironmentVariable("HOBBYDS9_COLONISATION_FIT_RECEIPT");if(path!=null){using var file=new FileStream(path,FileMode.CreateNew);JsonSerializer.Serialize(file,receipts,new JsonSerializerOptions{WriteIndented=true});}
    }
    static (ShipDesignCatalogueDto c,ShipDesigner d,ShipClassDraftDto draft,PublishedShipBlueprint p,ColonisationSupportCatalogueDto support) Transport()
    {
        var c=Read<ShipDesignCatalogueDto>("catalogue.json");var d=ShipDesignApplication.Create(c).Designer!;
        var draft=Read<ShipClassDraftDto[]>("ready-classes.json").Single(x=>x.ClassId=="class.galaxy-colony-transport.v1");
        var p=d.Publish(draft,ActivityReadyClassTests.Access(c),"blueprint.colony-transport",1);Assert.True(p.Published);
        return(c,d,draft,p.Blueprint!,Read<ColonisationSupportCatalogueDto>("support.json"));
    }
    [Fact] public void V4DerivesAllReadyFitsWithTruthfulIdentityAndAlwaysReservedPassengerDrain()
    {
        var x=Transport();var c=x.c;var d=x.d;var support=x.support;var receipt=new List<object>();
        foreach(var draft in Read<ShipClassDraftDto[]>("ready-classes.json"))
        {
            var publication=d.Publish(draft,ActivityReadyClassTests.Access(c),"blueprint.v4."+draft.ClassId.Substring(6),1).Blueprint!;
            var result=PublishedTacticalSupportCompiler.EvaluateColonisation(d.Catalogue,publication,ActivityReadyClassTests.Access(c),support);
            Assert.True(result.Supported,JsonSerializer.Serialize(result.Issues));var ship=result.Ship!;
            Assert.Equal(ColonisationSupportIdentity.Compiler,ship.CompilerId);Assert.Equal(ship.CompilerId,ship.Tactical.CompilerId);
            Assert.Equal(support.CatalogueId,ship.Tactical.SupportCatalogueId);Assert.Equal(support.CatalogueId,ship.Tactical.ProvenanceId);
            var isTransport=draft.ClassId=="class.galaxy-colony-transport.v1";
            Assert.Equal(isTransport?100:0,ship.Passengers);Assert.Equal(isTransport?8:0,ship.PassengerSupportPower);
            if(isTransport)
            {
                Assert.Empty(ship.Tactical.Weapons);Assert.Equal(0,ship.StudyWork);Assert.Equal(0,ship.ContactCapacity);
                Assert.Equal(20,ship.Food);Assert.Equal(1,ship.Equipment);Assert.Equal(10,ship.Materials);
                var life=ship.Tactical.EssentialReservations.Single(e=>e.System==HobbyDS9.Contracts.TacticalEssentialSystem.LifeSupport);
                Assert.Equal(14,life.RequiredPower);Assert.Equal(life.RequiredPower,life.AllocatedPower);
            }
            Assert.False(ship.Tactical.CloakCapable);Assert.Equal(0,ship.Tactical.ShieldRechargePerRound);
            receipt.Add(new{draft.ClassId,ship.CompilerId,ship.Tactical.FitId,ship.Tactical.SupportHash,ship.Passengers,ship.Food,ship.Equipment,ship.Materials,ship.PassengerSupportPower,ship.StudyWork,ship.ContactCapacity,essentials=ship.Tactical.EssentialReservations});
        }
        var path=Environment.GetEnvironmentVariable("HOBBYDS9_COLONISATION_SUPPORT_RECEIPT");if(path!=null){using var file=new FileStream(path,FileMode.CreateNew);JsonSerializer.Serialize(file,receipt,new JsonSerializerOptions{WriteIndented=true});}
    }
    [Fact] public void V3RejectsTransportAndCompleteV4MetadataBindsIdentityWithoutMutatingOldResults()
    {
        var x=Transport();var access=ActivityReadyClassTests.Access(x.c);
        Assert.False(PublishedTacticalSupportCompiler.EvaluateActivities(x.d.Catalogue,x.p,access,x.support.Activity!).Supported);
        var before=PublishedTacticalSupportCompiler.EvaluateColonisation(x.d.Catalogue,x.p,access,x.support).Ship!;
        x.support.SettlerModules![0].Food=19;
        var after=PublishedTacticalSupportCompiler.EvaluateColonisation(x.d.Catalogue,x.p,access,x.support).Ship!;
        Assert.NotEqual(before.Tactical.FitId,after.Tactical.FitId);Assert.NotEqual(before.Tactical.SupportHash,after.Tactical.SupportHash);Assert.Equal(20,before.Food);
        var copy=before.CopySupport();copy.SettlerModules![0].Passengers=1;copy.Activity!.Modules!.Clear();Assert.Equal(100,before.CopySupport().SettlerModules![0].Passengers);Assert.NotEmpty(before.CopySupport().Activity!.Modules!);
        access.AvailableVersionIds!.Remove("component.settler-module.v1");Assert.False(PublishedTacticalSupportCompiler.EvaluateColonisation(x.d.Catalogue,x.p,access,x.support).Supported);
    }
    [Fact] public void V4RejectsForgedCostsPowerMissingMappingsAndMalformedSnapshots()
    {
        var x=Transport();var access=ActivityReadyClassTests.Access(x.c);
        foreach(var mutate in new Action<ColonisationSupportCatalogueDto>[] {s=>s.SettlerModules=null,s=>s.SettlerModules![0]=null!,s=>s.SettlerModules![0].RequiredPower=7,s=>s.SettlerModules![0].ComponentVersionId="component.unknown.v1",s=>s.Activity!.Modules=null,s=>s.Activity!.Tactical!.Components=null,s=>s.CompilerId=ActivityTacticalSupportIdentity.Compiler})
        {var metadata=Read<ColonisationSupportCatalogueDto>("support.json");mutate(metadata);Assert.False(PublishedTacticalSupportCompiler.EvaluateColonisation(x.d.Catalogue,x.p,access,metadata).Supported);}
        var cost=x.p.Performance.Cost!;var forged=new ShipDesignCostSnapshot(new ShipDesignCostDto{Credits=cost.Credits,Industry=cost.Industry+1,Maintenance=cost.Maintenance,Resources=new()});
        var v=x.p.Performance;var perf=new ShipDesignPreview(v.Issues,v.CapacityUsed,v.CapacityLimit,v.RequiredCrew,v.ReactorPower,v.DistributionLimit,v.Mounts,forged,v.Profiles,v.Compiled,v.Envelopes);
        Assert.False(PublishedTacticalSupportCompiler.EvaluateColonisation(x.d.Catalogue,new PublishedShipBlueprint(x.p.BlueprintId,1,x.p.Fit,x.p.Hull,x.p.Components,perf),access,x.support).Supported);
        Assert.False(PublishedTacticalSupportCompiler.EvaluateColonisation(x.d.Catalogue,new PublishedShipBlueprint(x.p.BlueprintId,1,null!,x.p.Hull,x.p.Components,v),access,x.support).Supported);
        x.draft.Profiles!.Single(p=>p.Kind==OperatingProfileKind.Combat).Allocations!.Single(a=>a.InstallationId.EndsWith(".settlers")).AllocatedPower=0;
        Assert.False(x.d.Publish(x.draft,access,"blueprint.no-passenger-power",1).Published);
    }

}
