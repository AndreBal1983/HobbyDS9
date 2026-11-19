using System.Text.Json;
using HobbyDS9.Application;
using HobbyDS9.Contracts;
using HobbyDS9.Contracts.ShipDesign;
using Xunit;

namespace HobbyDS9.Simulation.Tests;

public sealed class ReactorRefinementSupportTests
{
    const string Refined="component.refinement.support-reactor.v2";
    const string Blueprint="blueprint.refinement.support-reactor";
    static string Json(object x)=>JsonSerializer.Serialize(x);
    static T Copy<T>(T x)=>JsonSerializer.Deserialize<T>(Json(x!))!;
    static ShipDesignCatalogueDto Catalogue()=>ColonisationContentTests.Read<ShipDesignCatalogueDto>("catalogue.json");
    static ColonisationSupportCatalogueDto V4()=>ColonisationContentTests.Read<ColonisationSupportCatalogueDto>("support.json");
    static ShipClassDraftDto Draft(string reactor="component.reactor.v1")
    {
        var d=ColonisationContentTests.Read<ShipClassDraftDto[]>("ready-classes.json").Single(x=>x.ClassId=="class.constitution-explorer.v1");
        d.Installations!.Single(x=>x.ComponentVersionId=="component.reactor.v1").ComponentVersionId=reactor;
        return d;
    }
    static ComponentVersionDto Add(ShipDesignCatalogueDto c,string id=Refined,string parent="component.reactor.v1")
    {
        var child=Copy(c.Components!.Single(x=>x.VersionId==parent));child.ParentVersionId=parent;child.VersionId=id;child.VersionNumber++;
        child.DisplayName="Refined reactor";child.Capacity-=2;child.PowerSupply+=9;child.Cost!.Industry--;
        c.Components!.Add(child);return child;
    }
    static (ShipDesigner Designer,PublishedShipBlueprint Publication) Publish(ShipDesignCatalogueDto c,ShipClassDraftDto d)
    {
        var designer=ShipDesignApplication.Create(c).Designer;Assert.NotNull(designer);
        var result=designer.Publish(d,ActivityReadyClassTests.Access(c),Blueprint,1);
        Assert.True(result.Published,Json(result.Preview.Issues));return(designer,result.Blueprint!);
    }
    static ColonisationSupportResult Evaluate(ShipDesignCatalogueDto c,ShipClassDraftDto d,ColonisationSupportCatalogueDto support)
    {var p=Publish(c,d);return PublishedTacticalSupportCompiler.EvaluateColonisation(p.Designer.Catalogue,p.Publication,ActivityReadyClassTests.Access(c),support);}

    [Fact] public void OptInClonesOnlyExplicitEditionAndLeavesV4PublicationsUnchanged()
    {
        var c=Catalogue();var old=V4();var before=Json(old);var source=Json(c);
        var first=Evaluate(c,Draft(),old);Assert.True(first.Supported,Json(first.Issues));
        var v5=PublishedTacticalSupportCompiler.CreateRefinementSupport(old);
        Assert.Equal(5,v5.FormatVersion);Assert.Equal(ColonisationRefinementSupportIdentity.Schema,v5.SchemaId);
        Assert.Equal(ColonisationRefinementSupportIdentity.Compiler,v5.CompilerId);
        Assert.Equal(Json(old.Activity!),Json(v5.Activity!));Assert.Equal(Json(old.SettlerModules!),Json(v5.SettlerModules!));
        v5.Activity!.Tactical!.Components!.Clear();
        Assert.Equal(before,Json(old));Assert.Equal(source,Json(c));
        var second=Evaluate(c,Draft(),old).Ship!;
        Assert.Equal(ColonisationSupportIdentity.Compiler,second.CompilerId);
        Assert.Equal(first.Ship!.Tactical.SupportHash,second.Tactical.SupportHash);Assert.Equal(first.Ship.Tactical.FitId,second.Tactical.FitId);
        Assert.Throws<ArgumentException>(()=>PublishedTacticalSupportCompiler.CreateRefinementSupport(v5));
    }

    [Theory][InlineData(false)][InlineData(true)]
    public void RealRefinementUsesCompiledSpaceCostAndPowerOnlyInExplicitV5(bool powerOnly)
    {
        var c=Catalogue();var root=c.Components!.Single(x=>x.VersionId=="component.reactor.v1");var child=Add(c);
        if(powerOnly){child.Capacity=root.Capacity;child.Cost!.Industry=root.Cost!.Industry;}
        else child.PowerSupply=root.PowerSupply;
        var support=V4();var metadata=PublishedTacticalSupportCompiler.CreateRefinementSupport(support);var untouched=Json(metadata);var frozen=Json(c);
        var original=Publish(c,Draft());var p=Publish(c,Draft(Refined));
        Assert.False(PublishedTacticalSupportCompiler.EvaluateColonisation(p.Designer.Catalogue,p.Publication,ActivityReadyClassTests.Access(c),support).Supported);
        var actual=PublishedTacticalSupportCompiler.EvaluateColonisation(p.Designer.Catalogue,p.Publication,ActivityReadyClassTests.Access(c),metadata);
        Assert.True(actual.Supported,Json(actual.Issues));var fit=actual.Ship!.Tactical;
        Assert.Equal(child.PowerSupply,fit.ReactorOutput);Assert.Equal(child.PowerSupply,p.Publication.Performance.ReactorPower);
        Assert.Equal(p.Publication.Performance.Cost!.Industry,fit.Cost.Industry);
        Assert.Equal(powerOnly?original.Publication.Performance.CapacityUsed:original.Publication.Performance.CapacityUsed-2,p.Publication.Performance.CapacityUsed);
        Assert.Equal(ColonisationRefinementSupportIdentity.Compiler,fit.CompilerId);Assert.Equal(fit.CompilerId,actual.Ship.CompilerId);
        Assert.Equal(untouched,Json(metadata));Assert.Equal(untouched,Json(actual.Ship.CopySupport()));Assert.Equal(frozen,Json(c));
        Assert.Equal(root.PowerSupply,original.Publication.Performance.ReactorPower);
    }

    [Fact] public void FrozenMultiHopAncestryIsRequiredAndUnrelatedVersionsDoNotChangeSupportHash()
    {
        var c=Catalogue();Add(c);var descendant=Add(c,"component.refinement.support-reactor.v3",Refined);
        var support=PublishedTacticalSupportCompiler.CreateRefinementSupport(V4());var draft=Draft(descendant.VersionId);
        var first=Evaluate(c,draft,support);Assert.True(first.Supported,Json(first.Issues));
        var frozen=Copy(c);Add(c,"component.refinement.unrelated-reactor.v4",descendant.VersionId);
        var next=Evaluate(c,draft,support);Assert.True(next.Supported,Json(next.Issues));
        Assert.Equal(first.Ship!.Tactical.SupportHash,next.Ship!.Tactical.SupportHash);
        Assert.Equal(first.Ship.Tactical.FitId,next.Ship.Tactical.FitId);
        Assert.Equal(first.Ship.Tactical.SupportHash,Evaluate(frozen,draft,support).Ship!.Tactical.SupportHash);
        c.Components!.Single(x=>x.VersionId==Refined).Cost!.Credits++;
        Assert.False(Evaluate(c,draft,support).Supported);
    }

    [Fact] public void LegalUnapprovedChangesOrUnrootedAndAssertedMappingsRemainUnsupported()
    {
        var support=PublishedTacticalSupportCompiler.CreateRefinementSupport(V4());
        var forbidden=Catalogue();Add(forbidden).Cost!.Credits++;
        Assert.False(Evaluate(forbidden,Draft(Refined),support).Supported);
        var unprefixed=Catalogue();Add(unprefixed,"component.unapproved-reactor.v2");
        Assert.False(Evaluate(unprefixed,Draft("component.unapproved-reactor.v2"),support).Supported);
        var unrooted=Catalogue();var child=Add(unrooted);child.ParentVersionId=null;
        Assert.False(Evaluate(unrooted,Draft(Refined),support).Supported);
        var asserted=Copy(support);asserted.Activity!.Tactical!.Components!.Add(new(){ComponentVersionId=Refined,Kind=TacticalComponentSupportKind.Reactor});
        var valid=Catalogue();Add(valid);Assert.False(Evaluate(valid,Draft(Refined),asserted).Supported);
        var fakeLab=Catalogue();var lab=Copy(fakeLab.Components!.Single(x=>x.VersionId=="component.activity-lab.standard.v1"));
        lab.ParentVersionId=lab.VersionId;lab.VersionId="component.refinement.fake-lab.v2";lab.VersionNumber++;fakeLab.Components!.Add(lab);
        var d=Draft();d.Installations!.Single(x=>x.ComponentVersionId==lab.ParentVersionId).ComponentVersionId=lab.VersionId;
        Assert.False(Evaluate(fakeLab,d,support).Supported);
    }

    [Fact] public void ActualConstructionAndRetainedReloadUseNewPowerAndFrozenPublication()
    {
        var old=ColonisationInventoryTests.Package();var activities=old.Activities!;var child=Add(activities.Catalogue!);Add(old.Catalogue!);
        var ap=CampaignTacticalReviewSession.CreateActivitiesPackage(activities.BaseTemplate!,activities.Catalogue!,activities.ReadyClasses!,activities.Support!,activities.Content!,activities.RecordBudget);
        var package=CampaignTacticalReviewSession.CreateColonisationPackage(ap,old.Catalogue!,old.ReadyClasses!,PublishedTacticalSupportCompiler.CreateRefinementSupport(old.Support!),old.Settlement!);
        var s=CampaignTacticalReviewSession.StartColonisation(package,PublishedConstructionProfileTests.Settings(30));var stamp=s.CaptureStatus();
        var readyBefore=Json(ColonisationTransportTests.Game(s.ExportColonisationSave()).Blueprints!.Single(b=>b.BlueprintId=="blueprint.ready.constitution-explorer"));
        var publish=s.SubmitStrategicCommand(new(){ActorCivilizationId=ColonisationTransportTests.Human,CommandId="command.refinement.publish",ExpectedTurn=stamp.Turn,ExpectedRevision=stamp.CampaignRevision,
            Kind=CommandKind.PublishShipBlueprint,PublishShipBlueprint=new(){BlueprintId=Blueprint,Revision=1,Draft=Draft(Refined)}});
        Assert.True(publish.Accepted,publish.Error+" "+s.LastPhysicalFailure);stamp=s.CaptureStatus();
        var queued=s.SubmitActivity(new(){ActorId=ColonisationTransportTests.Human,CommandId="command.refinement.build",ExpectedTurn=stamp.Turn,ExpectedRevision=stamp.CampaignRevision,
            Kind=CampaignActivityCommandKind.EnqueueShip,TargetId="colony.sol",DefinitionId=Blueprint,BlueprintRevision=1});
        Assert.True(queued.Accepted,queued.Error+" "+s.LastPhysicalFailure);
        for(int i=0;i<32&&!s.ExportColonisationSave().PublishedBuiltShips!.Any(b=>b.BlueprintId==Blueprint);i++)ColonisationTransportTests.Advance(s);
        var save=s.ExportColonisationSave();var built=Assert.Single(save.PublishedBuiltShips!,b=>b.BlueprintId==Blueprint);
        Assert.Equal(ColonisationRefinementSupportIdentity.Compiler,built.SupportCompilerId);
        var publication=ColonisationTransportTests.Game(save).Blueprints!.Single(b=>b.BlueprintId==Blueprint);
        var actual=PublishedTacticalSupportCompiler.EvaluateColonisation(new ShipDesignCatalogueSnapshot(publication.FrozenCatalogue!),CampaignBlueprints.Restore(publication),ActivityReadyClassTests.Access(publication.FrozenCatalogue!),package.Support!);
        Assert.True(actual.Supported,Json(actual.Issues));Assert.Equal(child.PowerSupply,actual.Ship!.Tactical.ReactorOutput);
        Assert.Equal(actual.Ship.Tactical.SupportHash,built.SupportHash);Assert.Equal(actual.Ship.Tactical.FitId,built.FitId);
        Assert.Equal(readyBefore,Json(ColonisationTransportTests.Game(save).Blueprints!.Single(b=>b.BlueprintId=="blueprint.ready.constitution-explorer")));
        Assert.Equal(Json(save),Json(CampaignTacticalReviewSession.LoadColonisation(package,save).ExportColonisationSave()));
    }
}
