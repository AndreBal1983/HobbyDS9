using System.Security.Cryptography;
using System.Text.Json;
using HobbyDS9.Application;
using HobbyDS9.Contracts;
using HobbyDS9.Contracts.ShipDesign;
using HobbyDS9.Simulation;
using Xunit;
namespace HobbyDS9.Simulation.Tests;

public sealed class EquipmentSourceTests
{
    static T Clone<T>(T x)=>JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(x))!;
    static string Hash(object x)=>Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(x))).ToLowerInvariant();
    static CampaignIntegratedPlaytestPackageDto Package()=>IntegratedPlaytestTests.Package();
    static CampaignEquipmentSource Resolve(CampaignIntegratedPlaytestPackageDto p,string ship,IEnumerable<CampaignIntegratedBuiltShipDto>? records=null)
        =>CampaignEquipmentSourceResolver.ResolveAuthored(p.Physical!.CorePackage!.Campaign!.Definitions,p.Physical.CorePackage.CloakSources!,p.ConstructionTemplates,records??Array.Empty<CampaignIntegratedBuiltShipDto>(),ship);
    [Fact] public void AuthoredSourcePreservesEveryFitStockCloakAndPresentationFactAndCopiesOut()
    {
        var p=Package();var definitions=p.Physical!.CorePackage!.Campaign!.Definitions;
        var expected=AuthoredTacticalEquipmentCopies.Copy(definitions.AuthoredTacticalEquipment!);
        foreach(var provision in expected.InitialShipProvisions!)
        {
            var source=Resolve(p,provision.ShipId!);var fit=expected.Fits!.Single(f=>f.DefinitionId==provision.FitDefinitionId);
            Assert.Equal(JsonSerializer.Serialize(fit),JsonSerializer.Serialize(source.Fit));Assert.Equal(JsonSerializer.Serialize(provision),JsonSerializer.Serialize(source.AuthoredProvision));
            var cloak=p.Physical.CorePackage.CloakSources!.Single(c=>c.FitId==fit.DefinitionId);Assert.Equal(cloak.Capable,source.CloakCapable);Assert.Equal(cloak.ConcurrentPowerRequired,source.CloakPower);
            Assert.Equal(definitions.ShipDesigns!.Single(d=>d.DefinitionId==provision.ShipDesignDefinitionId).DisplayName,source.DisplayName);Assert.Equal(provision.ShipDesignDefinitionId,source.PlaybackDesignId);
            var pool=source.InitialPool();pool.Shields[0].Current=987;source.Fit.Weapons!.Clear();source.AuthoredProvision.InitialAmmunition!.Clear();
            Assert.Equal(JsonSerializer.Serialize(fit),JsonSerializer.Serialize(source.Fit));Assert.NotEqual(987,source.InitialPool().Shields[0].Current);
        }
    }
    [Theory][InlineData("unknown")][InlineData("duplicate-initial")][InlineData("initial-and-built")][InlineData("missing-cloak")][InlineData("duplicate-cloak")][InlineData("wrong-cloak-revision")][InlineData("duplicate-fit")]
    public void UnknownAmbiguousAndMissingAuthoredCloakAlwaysReject(string mutation)
    {
        var p=Package();var core=p.Physical!.CorePackage!;var eq=core.Campaign!.Definitions.AuthoredTacticalEquipment!;var first=eq.InitialShipProvisions![0];var ship=first.ShipId!;var records=new List<CampaignIntegratedBuiltShipDto>();
        if(mutation=="unknown")ship="ship.unknown";
        if(mutation=="duplicate-initial")eq.InitialShipProvisions.Add(first);
        if(mutation=="initial-and-built")records.Add(new(){ShipId=ship});
        if(mutation=="missing-cloak")core.CloakSources!.RemoveAll(c=>c.FitId==first.FitDefinitionId);
        if(mutation=="duplicate-cloak")core.CloakSources!.Add(Clone(core.CloakSources.Single(c=>c.FitId==first.FitDefinitionId)));
        if(mutation=="wrong-cloak-revision")core.CloakSources!.Single(c=>c.FitId==first.FitDefinitionId).FitRevision++;
        if(mutation=="duplicate-fit")eq.Fits!.Add(Clone(eq.Fits.Single(f=>f.DefinitionId==first.FitDefinitionId)));
        Assert.Throws<ArgumentException>(()=>Resolve(p,ship,records));
    }
    [Fact] public void StaticBuiltSourceKeepsTemplateIdentityButOwnsItsStocksAndRejectsAmbiguousLedger()
    {
        var p=Package();var template=p.ConstructionTemplates!.Single();var original=p.Physical!.CorePackage!.Campaign!.Definitions.AuthoredTacticalEquipment!.InitialShipProvisions!.Single(x=>x.ShipId==template.SourceProvisionShipId);
        var record=new CampaignIntegratedBuiltShipDto{ShipId="ship.0000000000000002",OwnerCivilizationId="civ.federation",ShipDesignDefinitionId=template.ShipDesignDefinitionId,SourceProvisionShipId=template.SourceProvisionShipId};
        var source=Resolve(p,record.ShipId!,new[]{record});Assert.Equal(CampaignEquipmentSourceKind.StaticBuilt,source.Kind);Assert.Equal(original.ShipDesignDefinitionId,source.PlaybackDesignId);
        original.InitialShields![0].Current=123;record.OwnerCivilizationId="civ.forged";Assert.Equal("civ.federation",source.Owner);Assert.NotEqual(123,source.InitialPool().Shields[0].Current);
        Assert.Throws<ArgumentException>(()=>Resolve(p,record.ShipId!,new[]{record,record}));
    }
    [Fact] public void ExistingIntegratedInitialBuiltAndObservedBytesMatchPreChangeAssemblyGoldens()
    {
        var p=Package();var app=CampaignIntegratedPlaytestApplication.StartPackage(p);
        Assert.Equal("2840978a73b1ebb1e0c75528c0299cb0a0e14934c66639acd7bf34989534bb29",Hash(app.ExportIntegratedSave()));
        var s=app.CaptureStatus();Assert.True(app.SubmitStrategicCommand(new(){Kind=CommandKind.EnqueueProduction,ActorCivilizationId="civ.federation",CommandId="command.source.golden",ExpectedTurn=s.Turn,ExpectedRevision=s.CampaignRevision,EnqueueProduction=new(){ColonyId="colony.sol",DefinitionId="ship-design.source-cruiser"}}).Accepted);
        for(var i=0;i<100 && app.ExportIntegratedSave().BuiltShips!.Count==0;i++){s=app.CaptureStatus();Assert.True(app.AdvanceTurn(s.Turn,s.CampaignRevision).Accepted);}
        Assert.Equal("a15d8d043998ef47fed720a3033fdd5902bfc3a15ab8e15c34032eb70e38f5b6",Hash(app.ExportIntegratedSave()));Assert.Equal("4ddab9152c1581eca93c45806338138609345bf183899468a315b2d38507af89",Hash(app.InspectOwnShips("civ.federation")));
        Assert.Equal(Hash(app.ExportIntegratedSave()),Hash(CampaignIntegratedPlaytestApplication.LoadSave(p,Clone(app.ExportIntegratedSave())).ExportIntegratedSave()));
    }
    [Theory][InlineData(false,"4d17eb3f6bfdc187c1f86528e4483eb9d8a2e9f89e7527b3f73f2aec64cb211f")][InlineData(true,"bc8a64ec12092a37d71da1fb8c7c94e9fbb35aaf3dd725bccc06fa6882784fd6")]
    public void GeneratedProfilesRetainExactBytesAndBlueprintConstructionRejection(bool v2,string expected)
    {
        var template=PlayableFleetTests.Package();var settings=new NaturalGalaxySettingsDto{Map=new(){Seed="42",SystemCount=30,EmptyRegionCount=30,Width=750000,Height=600000,HomeCivilizationDefinitionIds=new(){"civilization.federation","civilization.cardassian"}}};
        ICampaignGeneratedSession app=v2?CampaignStaticAstronomyApplication.StartGame(template,settings):CampaignGeneratedApplication.StartGame(template,settings);var root=app.ExportGeneratedSave();Assert.Equal(expected,Hash(root));
        var s=app.CaptureStatus();var result=app.SubmitStrategicCommand(new(){Kind=CommandKind.EnqueueBlueprintConstruction,ActorCivilizationId="civ.federation",CommandId="command.source.no-admission",ExpectedTurn=s.Turn,ExpectedRevision=s.CampaignRevision,EnqueueBlueprintConstruction=new(){ColonyId="colony.sol",BlueprintId="blueprint.test.support",Revision=1}});
        Assert.Equal(CampaignTacticalError.UnsupportedSource,result.Error);Assert.Equal(expected,Hash(app.ExportGeneratedSave()));
        var reloaded=v2?(ICampaignGeneratedSession)CampaignStaticAstronomyApplication.LoadSave(template,Clone(root)):CampaignGeneratedApplication.LoadSave(template,Clone(root));Assert.Equal(expected,Hash(reloaded.ExportGeneratedSave()));
    }
    [Fact] public void EvenActuallyPublishedSupportedBlueprintDoesNotGainIntegratedConstructionAdmission()
    {
        var app=CampaignIntegratedPlaytestApplication.StartPackage(Package());var f=PublishedTacticalSupportTests.Load();var draft=PublishedTacticalSupportTests.Draft(f,true);var s=app.CaptureStatus();
        Assert.True(app.SubmitStrategicCommand(new(){Kind=CommandKind.PublishShipBlueprint,ActorCivilizationId="civ.federation",CommandId="command.source.publish",ExpectedTurn=s.Turn,ExpectedRevision=s.CampaignRevision,
            PublishShipBlueprint=new(){BlueprintId="blueprint.test.support",Revision=1,Draft=draft}}).Accepted);
        var library=app.CaptureDesignLibrary("civ.federation");var publication=library.Published.Single().Blueprint;
        Assert.True(PublishedTacticalSupportCompiler.Evaluate(library.Catalogue!.Catalogue,publication,new(){AvailableVersionIds=library.Catalogue.AvailableVersionIds.ToList()},PublishedTacticalSupportTests.Support(f)).IsSupportedByCompiler);
        var before=Hash(app.ExportIntegratedSave());s=app.CaptureStatus();var result=app.SubmitStrategicCommand(new(){Kind=CommandKind.EnqueueBlueprintConstruction,ActorCivilizationId="civ.federation",CommandId="command.source.still-unsupported",ExpectedTurn=s.Turn,ExpectedRevision=s.CampaignRevision,
            EnqueueBlueprintConstruction=new(){ColonyId="colony.sol",BlueprintId=publication.BlueprintId,Revision=1}});
        Assert.Equal(CampaignTacticalError.UnsupportedSource,result.Error);Assert.Equal(before,Hash(app.ExportIntegratedSave()));
    }
}
