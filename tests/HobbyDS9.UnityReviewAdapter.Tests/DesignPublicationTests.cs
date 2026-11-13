using HobbyDS9.Application;
using HobbyDS9.Contracts;
using HobbyDS9.Contracts.ShipDesign;
using HobbyDS9.UnityReview;
using Xunit;
namespace HobbyDS9.UnityReviewAdapter.Tests;
public sealed class DesignPublicationTests : IDisposable
{
    readonly string directory=Path.Combine(Path.GetTempPath(),"hobby-design-publish-"+Guid.NewGuid().ToString("N"));
    readonly ReviewNewtonsoftCodec codec=new();
    static CampaignPlayableFleetPackageDto Template()=>PlayableFleetPackageReader.Read(Path.Combine(AppContext.BaseDirectory,"scenarios/playable-fleet-v1"));
    static readonly Lazy<CampaignGeneratedSaveDto> Initial=new(()=>CampaignStaticAstronomyApplication.StartGame(Template(),new NaturalGalaxySettingsDto { Map=new(){Seed="42",SystemCount=250,EmptyRegionCount=250,Width=2500000,Height=2000000,QuietEmptyAreaTargetPermille=2000,HomeCivilizationDefinitionIds=new(){"civilization.federation","civilization.cardassian"}}}).ExportGeneratedSave());
    static ShipClassDraftDto Fit(ReviewDesignPublication p){var d=ReviewDesignPublication.Starter(p.Library)!;d.ClassId=p.ClassId;return d;}
    ReviewUiModel Model()
    {
        var store=new ReviewStaticAstronomyCampaignSlotStore(directory,codec);store.Write("initial",Initial.Value,false);
        return ReviewUiModel.ContinueStaticAstronomyCampaign(Template(),codec,store,"initial");
    }
    [Fact] public void ActualGeneratedPublicationTwoRevisionsFrozenHistoryAndFullRootResume()
    {
        var m=Model();var p=new ReviewDesignPublication(m);var fit=Fit(p);
        Assert.NotNull(fit);Assert.True(ReviewDesignPublication.Valid(p.Library,fit));Assert.True(p.CanPublish(fit));
        m.LocalDesignDirty=true;var research=m.ResearchInputs()!;research.ScienceUnits=300;var credits=m.Current.Frame.Strategy!.Civilization.Credits;
        Assert.True(p.Publish(fit));Assert.Equal(credits,m.Current.Frame.Strategy.Civilization.Credits);Assert.False(p.CanPublish(fit));
        Assert.True(m.ResearchDraft!.Dirty);Assert.Equal(300,m.ResearchDraft.ScienceUnits);
        var one=Assert.Single(p.Library.Published);Assert.Equal(1,one.Blueprint.Revision);Assert.False(p.Library.CanConstructPublishedBlueprints);
        fit.DisplayName="Courier II";Assert.True(p.Publish(fit));Assert.Equal(2,p.Library.Published.Count);
        Assert.Equal("Utility class",one.Blueprint.Fit.DisplayName);Assert.Equal(3,p.NextRevision);
        Assert.Throws<InvalidOperationException>(()=>p.Edit(one));var copy=p.Edit(p.Library.Published.Single(r=>r.Blueprint.Revision==1));copy.DisplayName="Courier III";Assert.Equal("Utility class",one.Blueprint.Fit.DisplayName);
        Assert.True(p.Publish(copy));Assert.Equal(4,p.NextRevision);
        Assert.True(m.Save("published"));var before=File.ReadAllBytes(Path.Combine(directory,"unity-generated-static-astronomy-v2-published.json"));
        Assert.True(m.Load("published",true));Assert.False(p.Current);Assert.False(p.CanPublish(copy));
        Assert.Equal(3,m.DesignLibrary().Published.Count);Assert.True(m.Save("resaved"));
        Assert.Equal(before,File.ReadAllBytes(Path.Combine(directory,"unity-generated-static-astronomy-v2-resaved.json")));
    }
    [Fact] public void InvalidPowerAndStaleFrameDoNotPublishOrConsumeRevisionAndRefreshPreservesInput()
    {
        var m=Model();var p=new ReviewDesignPublication(m);var fit=Fit(p);var frame=m.Current.Frame;
        fit.Profiles![0].Allocations!.Single(a=>a.InstallationId.Contains("propulsion")).AllocatedPower=0;
        Assert.False(p.CanPublish(fit));Assert.False(p.Publish(fit));Assert.Empty(m.DesignLibrary().Published);Assert.Same(frame,m.Current.Frame);Assert.Equal(1,p.NextRevision);
        fit=Fit(p);var old=m.Current.Frame;
        Assert.True(m.AllocateResearch(300,ReviewUiModel.NewCommandId()));Assert.False(p.CanPublish(fit));
        Assert.False(m.PublishDesign(old,p.BlueprintId,1,fit));Assert.Empty(m.DesignLibrary().Published);
        Assert.True(p.Refresh());Assert.True(p.CanPublish(fit));Assert.Equal("Utility class",fit.DisplayName);Assert.True(p.Publish(fit));
    }
    [Fact] public void AvailabilityIsRealAndUnsupportedConstructionStaysRejected()
    {
        var m=Model();var p=new ReviewDesignPublication(m);var fit=Fit(p);
        var actual=p.Library.Catalogue!;
        Assert.False(ShipDesignApplication.FromCampaignCatalogue(actual.Catalogue).Preview(fit,new ShipDesignAccessDto {AvailableVersionIds=actual.AvailableVersionIds.Where(x=>x!="component.reactor.v1").ToList()}).IsValid);
        Assert.True(p.Publish(fit));var frame=m.Current.Frame;
        var c=m.StrategicEnvelope(CommandKind.EnqueueBlueprintConstruction,ReviewUiModel.NewCommandId());
        c.EnqueueBlueprintConstruction=new(){ColonyId=m.Current.Frame.Strategy!.Colonies.First().ColonyId.Value,BlueprintId=p.BlueprintId,Revision=1};
        Assert.False(m.Strategic(c));Assert.Same(frame,m.Current.Frame);Assert.Single(m.DesignLibrary().Published);
    }
    [Fact] public void BusyLeaseAndForeignHistoryCannotPublishOrReplaceLocalIdentity()
    {
        var m=Model();var p=new ReviewDesignPublication(m);var fit=Fit(p);var id=p.BlueprintId;
        var work=m.BeginStaticCheckpointOperation("held",false)!;Assert.NotNull(work);
        Assert.False(p.Publish(fit));Assert.Equal(id,p.BlueprintId);Assert.Equal(1,p.NextRevision);
        m.AbandonStaticCheckpointOperation(work);Assert.True(p.Publish(fit));
        var record=p.Library.Published.Single();var other=new ReviewDesignPublication(m);
        other.NewIdentity();var detached=new ReviewDesignPublication(m);Assert.True(detached.Publish(Fit(detached)));
        // Same actor's record from an earlier observation is not a current-library handle.
        Assert.Throws<InvalidOperationException>(()=>detached.Edit(record));
        Assert.False(other.Current);Assert.False(other.Publish(fit));Assert.Equal(1,other.NextRevision);
    }
    [Fact] public void NewClassHasDistinctLineageAndEditingOldClassContinuesOnlyItsRevision()
    {
        var m=Model();var p=new ReviewDesignPublication(m);var firstId=p.BlueprintId;var firstFit=Fit(p);Assert.True(p.Publish(firstFit));
        p.NewIdentity();var secondId=p.BlueprintId;var secondFit=Fit(p);Assert.NotEqual(firstId,secondId);Assert.NotEqual(firstFit.ClassId,secondFit.ClassId);
        Assert.True(p.Publish(secondFit));Assert.Equal(2,p.Library.Published.Count);Assert.All(p.Library.Published,r=>Assert.Equal(1,r.Blueprint.Revision));
        var old=p.Library.Published.Single(r=>r.Blueprint.BlueprintId==firstId);var copy=p.Edit(old);copy.DisplayName="Original revised";
        Assert.Equal(firstId,p.BlueprintId);Assert.Equal(firstFit.ClassId,p.ClassId);Assert.True(p.Publish(copy));
        Assert.Single(p.Library.Published,r=>r.Blueprint.BlueprintId==secondId);Assert.Equal(2,p.Library.Published.Count(r=>r.Blueprint.BlueprintId==firstId));
        Assert.True(m.Save("classes"));Assert.True(m.Load("classes",true));Assert.Equal(2,m.DesignLibrary().Published.Select(r=>r.Blueprint.Fit.ClassId).Distinct().Count());
    }
    [Fact] public void PostPublicationRefreshKeepsSuccessButExplicitRefreshAnnouncesAndStaleRefreshStillRejects()
    {
        var m=Model();var p=new ReviewDesignPublication(m);var fit=Fit(p);Assert.True(p.Publish(fit));
        Assert.Equal("Published revision 1.",p.Notice);var frame=m.Current.Frame;var library=p.Library;
        Assert.True(p.Refresh(false));Assert.NotSame(frame,m.Current.Frame);Assert.NotSame(library,p.Library);
        Assert.True(p.Current);Assert.Equal(2,p.NextRevision);Assert.Single(p.Library.Published);Assert.False(p.CanPublish(fit));
        Assert.Equal("Published revision 1.",p.Notice);
        Assert.True(p.Refresh());Assert.Equal("Current campaign catalogue refreshed; local fit retained.",p.Notice);
        Assert.True(m.Save("notice"));Assert.True(m.Load("notice",true));Assert.False(p.Refresh(false));
        Assert.Equal("Campaign replaced. Open the current campaign designer.",p.Notice);Assert.False(p.CanPublish(fit));
    }
    public void Dispose(){if(Directory.Exists(directory))Directory.Delete(directory,true);}
}



