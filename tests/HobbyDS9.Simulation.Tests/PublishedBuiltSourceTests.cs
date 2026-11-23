using System.Text.Json;
using HobbyDS9.Contracts;
using HobbyDS9.Contracts.ShipDesign;
using HobbyDS9.Simulation;
using Xunit;
namespace HobbyDS9.Simulation.Tests;

public sealed class PublishedBuiltSourceTests
{
    static T Clone<T>(T x)=>JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(x))!;
    // Test-only authored support; no new live content or integrated admission.
    internal static (CampaignPublishedBuiltShipDto Record,GameStateDto Before,GameStateDto After,TacticalSupportCatalogueDto Support) Completion()
    {
        var f=PublishedTacticalSupportTests.Load();var support=PublishedTacticalSupportTests.Support(f);var package=IntegratedPlaytestTests.Package();var scenario=package.Physical!.CorePackage!.Campaign!;
        var game=Clone(scenario.InitialState);var draft=PublishedTacticalSupportTests.Draft(f,true);
        var publication=new BlueprintRevisionDto{OwnerCivilizationId="civ.federation",BlueprintId="blueprint.test.built",Revision=1,CompilerVersion=ShipDesignVersions.Compiler,
            RulesVersion=scenario.RulesVersion,SourceContentHash=scenario.ContentHash,FrozenCatalogue=f.Catalogue,Fit=draft};
        var restored=CampaignBlueprints.Restore(publication);publication.CompiledDesign=CampaignBlueprints.Design(restored);var cost=restored.Performance.Cost!;
        publication.Cost=new(){Credits=cost.Credits,Industry=cost.Industry,Maintenance=cost.Maintenance,Resources=cost.Resources.Select(x=>new ResourceAmountDto{ResourceDefinitionId=x.ResourceDefinitionId,Amount=x.Amount}).ToList()};
        game.Blueprints!.Add(publication);var command=new CommandEnvelope{ActorCivilizationId="civ.federation",EnqueueBlueprintConstruction=new(){ColonyId="colony.sol",BlueprintId=publication.BlueprintId,Revision=1}};
        var money=game.Civilizations!.Single(c=>c.CivilizationId=="civ.federation").Credits;
        Assert.Equal(CommandErrorCode.None,new EnqueueBlueprintConstructionHandler(scenario.Definitions).Execute(game,command));
        Assert.Equal(money-cost.Credits,game.Civilizations!.Single(c=>c.CivilizationId=="civ.federation").Credits);
        var queue=Clone(game.Colonies!.Single(c=>c.ColonyId=="colony.sol").ProductionQueue!.Single());
        GameStateDto before=game;ShipStateDto? ship=null;
        for(int i=0;i<100 && ship==null;i++){before=Clone(game);ProductionPhase.Execute(game,scenario.Definitions);ship=game.Ships!.SingleOrDefault(s=>s.Blueprint?.BlueprintId==publication.BlueprintId);if(ship==null)game.TurnNumber++;}
        Assert.NotNull(ship);var derived=PublishedTacticalSupportCompiler.Evaluate(new(publication.FrozenCatalogue!),restored,CampaignBlueprints.FrozenAccess(publication.FrozenCatalogue!),support);Assert.True(derived.IsSupportedByCompiler);var fit=derived.Fit!;
        var r=new CampaignPublishedBuiltShipDto{ShipId=ship!.ShipId,BlueprintId=publication.BlueprintId,BlueprintRevision=1,OwnerCivilizationId=publication.OwnerCivilizationId,FleetIdAtConstruction=ship.FleetId,
            QueueItemId=queue.QueueItemId,TurnBuilt=before.TurnNumber,SourceCompilerId=publication.CompilerVersion,SourceRulesVersion=publication.RulesVersion,SourceContentHash=publication.SourceContentHash,
            PublicationHash=fit.SourceHash,SupportCatalogueId=fit.SupportCatalogueId,SupportCatalogueRevision=fit.SupportCatalogueRevision,SupportHash=fit.SupportHash,SupportCompilerId=fit.CompilerId,FitId=fit.FitId,FitRevision=fit.FitRevision};
        return(r,before,game,support);
    }
    [Fact] public void ActualPaidQueueProductionAssociationResolvesFrozenBeamSourceAndCopiesEveryOutput()
    {
        var f=Completion();var source=PublishedBuiltShipValidation.ValidateCompletion(f.Record,f.Before,f.After,f.Support,1);
        var equipment=source.Equipment;Assert.Equal(CampaignEquipmentSourceKind.PublishedBuilt,equipment.Kind);Assert.Equal(f.Record.BlueprintId,equipment.PlaybackDesignId);
        Assert.Equal(f.After.Blueprints!.Single().Fit!.DisplayName,equipment.DisplayName);Assert.False(equipment.CloakCapable);Assert.Equal(0,equipment.CloakPower);
        Assert.Throws<InvalidOperationException>(()=>equipment.AuthoredProvision);Assert.Single(equipment.Fit.Weapons!);Assert.All(equipment.Fit.Weapons!,w=>Assert.Equal(TacticalWeaponClass.Beam,w.Class));
        var pool=equipment.InitialPool();Assert.Empty(pool.Ammunition);Assert.All(pool.Shields,x=>Assert.Equal(0,x.Current));pool.Shields[0].Current=999;Assert.All(equipment.InitialPool().Shields,x=>Assert.Equal(0,x.Current));
        var hash=source.Provenance.PublicationHash;source.Provenance.PublicationHash="forged";f.Record.PublicationHash="forged";f.Support.Components!.Clear();f.After.Blueprints![0].Fit!.DisplayName="changed";
        Assert.Equal(hash,source.Provenance.PublicationHash);Assert.NotEqual("changed",equipment.DisplayName);Assert.Single(equipment.Fit.Weapons!);
    }
    [Fact] public void RetainedDestroyedSourcesNeedNoCurrentAccessAndStillResolveOriginalRevision()
    {
        var f=Completion();f.After.Ships!.RemoveAll(s=>s.ShipId==f.Record.ShipId);foreach(var fleet in f.After.Fleets!)fleet.ShipIds!.Remove(f.Record.ShipId!);
        // Current research access is deliberately not an argument. The recorded catalogue is archival source, not permission.
        foreach(var owner in f.After.Civilizations!)owner.MatureEngineeringApplicationDefinitionIds!.Clear();
        var result=PublishedBuiltShipValidation.ValidateRetained(new[]{f.Record},f.After,f.Support,1);Assert.Single(result);Assert.Equal(f.Record.BlueprintRevision,result[0].Provenance.BlueprintRevision);
    }
    [Fact] public void DetachedDerivationIsStableUnderSourceSupportAndStateReordering()
    {
        var f=Completion();var original=PublishedBuiltShipValidation.ValidateRetained(new[]{f.Record},f.After,f.Support,1)[0];
        f.After.Ships!.Reverse();f.After.Civilizations!.Reverse();var publication=f.After.Blueprints![0];publication.FrozenCatalogue!.Hulls!.Reverse();publication.FrozenCatalogue.Components!.Reverse();
        publication.Cost!.Resources!.Reverse();publication.CompiledDesign!.ResourceCosts!.Reverse();f.Support.Hulls!.Reverse();f.Support.Components!.Reverse();
        var reordered=PublishedBuiltShipValidation.ValidateRetained(new[]{f.Record},f.After,f.Support,1)[0];
        Assert.Equal(JsonSerializer.Serialize(original.Provenance),JsonSerializer.Serialize(reordered.Provenance));Assert.Equal(JsonSerializer.Serialize(original.Equipment.Fit),JsonSerializer.Serialize(reordered.Equipment.Fit));
    }
    [Theory][InlineData("paid")][InlineData("industry")][InlineData("resources")][InlineData("queue")][InlineData("still-queued")][InlineData("preexisting-ship")][InlineData("fleet")][InlineData("turn")]
    public void CompletionRequiresTheActualQueueReceiptAndNewShipAssociation(string kind)
    {
        var f=Completion();var queue=f.Before.Colonies!.Single(c=>c.ColonyId=="colony.sol").ProductionQueue![0];
        if(kind=="paid")queue.PaidCredits++;
        if(kind=="industry")queue.RequiredIndustry++;
        if(kind=="resources")queue.PaidResources!.Clear();
        if(kind=="queue")f.Record.QueueItemId="queue-item.0000000000000001";
        if(kind=="still-queued")f.After.Colonies!.Single(c=>c.ColonyId=="colony.sol").ProductionQueue!.Add(Clone(queue));
        if(kind=="preexisting-ship")f.Before.Ships!.Add(Clone(f.After.Ships!.Single(s=>s.ShipId==f.Record.ShipId)));
        if(kind=="fleet")f.Record.FleetIdAtConstruction="fleet.wrong";
        if(kind=="turn")f.Record.TurnBuilt++;
        Assert.Throws<ArgumentException>(()=>PublishedBuiltShipValidation.ValidateCompletion(f.Record,f.Before,f.After,f.Support,1));
    }
    [Theory][InlineData("owner")][InlineData("support")][InlineData("source")][InlineData("fit")][InlineData("revision")][InlineData("cost")][InlineData("power")][InlineData("identity")][InlineData("cloak")][InlineData("duplicate")][InlineData("unknown")]
    public void RetainedForgeryUnknownAndUnsupportedSourcesFailClosed(string kind)
    {
        var f=Completion();var records=new List<CampaignPublishedBuiltShipDto>{f.Record};
        if(kind=="owner")f.Record.OwnerCivilizationId="civ.cardassian";
        if(kind=="support")f.Record.SupportHash=new string('0',64);
        if(kind=="source")f.Record.SourceContentHash=new string('0',64);
        if(kind=="fit")f.Record.FitRevision++;
        if(kind=="revision")f.Record.BlueprintRevision++;
        if(kind=="cost")f.After.Blueprints![0].Cost!.Credits++;
        if(kind=="power")f.After.Blueprints![0].FrozenCatalogue!.Components![0].PowerSupply++;
        if(kind=="identity")f.Record.ShipId="ship.invalid";
        if(kind=="cloak")f.Support.CloakPolicyId="cloak.invented";
        if(kind=="duplicate")records.Add(Clone(f.Record));
        if(kind=="unknown")f.After.Blueprints!.Clear();
        Assert.Throws<ArgumentException>(()=>PublishedBuiltShipValidation.ValidateRetained(records,f.After,f.Support,1));
    }
}
