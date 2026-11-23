using System.Reflection;
using System.Text;
using HobbyDS9.Application;
using HobbyDS9.Contracts;
using HobbyDS9.Contracts.ShipDesign;
using HobbyDS9.TacticalReviewClient;
using HobbyDS9.UnityReview;
using Xunit;

namespace HobbyDS9.UnityReviewAdapter.Tests;
public sealed class PublishedConstructionBindingTests : IDisposable
{
    const string Human="civ.federation";
    readonly string directory=Path.Combine(Path.GetTempPath(),"HobbyDS9-Published-Binding-"+Guid.NewGuid().ToString("N"));
    readonly ReviewNewtonsoftCodec codec=new();
    static NaturalGalaxySettingsDto Settings()=>new(){Map=new(){Seed="42",SystemCount=30,EmptyRegionCount=30,Width=750000,Height=600000,
        HomeCivilizationDefinitionIds=new(){"civilization.federation","civilization.cardassian"}}};
    CampaignPublishedConstructionPackageDto Package()
    {var template=PlayableFleetPackageReader.Read(Path.Combine(AppContext.BaseDirectory,"scenarios/playable-fleet-v1"));
     return PublishedConstructionPlaytestOptions.CreatePackage(template,File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,"scenarios/published-construction-v1/support.json")),codec);}
    ReviewPublishedConstructionSlotStore Store(Action<UnityReviewWriteStage>? probe=null)=>new(directory,codec,probe);
    string FileFor(string slot)=>Path.Combine(directory,"unity-published-construction-v1-"+slot+".json");
    static PhysicalReviewHost Host(ReviewController controller)=>(PhysicalReviewHost)typeof(ReviewController).GetField("host",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(controller)!;
    ReviewUiModel Model(Action<UnityReviewWriteStage>? probe=null)=>new(Package(),Settings(),codec,Store(probe));
    static ReviewDesignPublication Publish(ReviewUiModel model)
    {var p=new ReviewDesignPublication(model);var fit=ReviewDesignPublication.Starter(p.Library)!;fit.ClassId=p.ClassId;Assert.True(p.Publish(fit));return p;}
    [Fact] public void ProvisionalTableHasExactExternalAndUnityBytesAndCannotBeChangedBySaveMetadata()
    {
        var bytes=File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,"scenarios/published-construction-v1/support.json"));
        Assert.Equal(bytes,File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,"scenarios/published-construction-v1/unity-support.json")));
        var template=PlayableFleetPackageReader.Read(Path.Combine(AppContext.BaseDirectory,"scenarios/playable-fleet-v1"));
        var before=codec.CopyPackage(template);var package=PublishedConstructionPlaytestOptions.CreatePackage(template,bytes,codec);
        Assert.Equal(before.ReplayOperationLimit,template.ReplayOperationLimit);Assert.Equal(65536,package.BaseTemplate!.ReplayOperationLimit);
        var changed=Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(bytes).Replace("\"Damage\":7","\"Damage\":8"));Assert.NotEqual(bytes,changed);
        Assert.Throws<ArgumentException>(()=>PublishedConstructionPlaytestOptions.CreatePackage(template,changed,codec));
    }
    [Fact] public void ActualPublicationPaidRevisionSaveContinueAndInterfaceReopenRemainTypedAndByteExact()
    {
        var model=Model();var publication=Publish(model);var r1=publication.Library.Published.Single().Blueprint;
        var offer=model.ConstructionEligibility(r1.BlueprintId,1,"colony.sol");Assert.True(offer.CanQueue);var money=model.Current.Frame.Strategy!.Civilization.Credits;
        Assert.True(model.EnqueuePublished(offer,ReviewUiModel.NewCommandId()));Assert.Equal(money-offer.Cost.Credits,model.Current.Frame.Strategy!.Civilization.Credits);
        Assert.True(publication.Refresh(false));var fit=publication.Edit(publication.Library.Published.Single());fit.DisplayName="Revision two";Assert.True(publication.Publish(fit));
        Assert.True(model.Save("one"));var bytes=File.ReadAllBytes(FileFor("one"));var root=Store().Read("one");
        Assert.Equal(1,root.Campaign!.Integrated.Physical!.Core.Campaign.GameState.Colonies!.Single(c=>c.ColonyId=="colony.sol").ProductionQueue!.Single().Blueprint!.Revision);
        var old=model.Current;var pair=model.GeneratedPresentation();Assert.True(model.Load("one",true));Assert.NotSame(old,model.Current);Assert.False(model.IsCurrentGeneratedPresentation(pair));
        Assert.True(model.Save("two"));Assert.Equal(bytes,File.ReadAllBytes(FileFor("two")));
        var controller=model.Current;var oldHost=Host(controller);var handle=oldHost.SaveCommitted();
        Assert.True(controller.ReopenFromSlot("one").Succeeded);Assert.NotSame(oldHost,Host(controller));
        Assert.Throws<ArgumentException>(()=>Host(controller).LoadCommitted(handle,Human,null));
        Assert.True(controller.SaveCommittedToSlot("three").Succeeded);Assert.Equal(bytes,File.ReadAllBytes(FileFor("three")));
    }
    [Fact] public void SameFrameRedrawDoesNotInvalidateCapturedConfirmationButUnissuedCopiesAreRejected()
    {
        var model=Model();var p=Publish(model);
        var captured=model.ConstructionEligibility(p.BlueprintId,1,"colony.sol");
        var frame=model.Current.Frame;var money=frame.Strategy!.Civilization.Credits;
        var redraw=model.ConstructionEligibility(p.BlueprintId,1,"colony.sol");
        Assert.NotSame(captured,redraw);Assert.Same(frame,model.Current.Frame);
        var forged=new CampaignPublishedConstructionEligibility(captured.ActorId,captured.Turn,captured.CampaignRevision,
            captured.BlueprintId,captured.BlueprintRevision,captured.ColonyId,captured.Cost,captured.OrdinaryReason,captured.SupportIssues,captured.CommandResult);
        Assert.False(model.EnqueuePublished(forged,ReviewUiModel.NewCommandId()));
        Assert.Equal(money,model.Current.Frame.Strategy!.Civilization.Credits);
        Assert.True(model.EnqueuePublished(captured,ReviewUiModel.NewCommandId()),model.Message);
        Assert.Equal(money-captured.Cost.Credits,model.Current.Frame.Strategy!.Civilization.Credits);
        var after=model.Current.Frame;
        Assert.False(model.EnqueuePublished(redraw,ReviewUiModel.NewCommandId()));
        Assert.Same(after,model.Current.Frame);
    }
    [Fact] public void StrictCarrierRejectsWrongOrStrippedMarkersButReplayStillRejectsAdmittedForgeryAtomically()
    {
        var model=Model();Assert.True(model.Save("one"));var root=Store().Read("one");
        var bytes=codec.EncodePublishedConstructionRoot(root);var stripped=Encoding.UTF8.GetString(bytes).Replace("\"FormatVersion\":8,","");
        Assert.ThrowsAny<Exception>(()=>codec.DecodePublishedConstructionRoot(Encoding.UTF8.GetBytes(stripped)));
        root.Campaign!.Integrated.Physical!.Core.ProfileId=CampaignStaticAstronomyIdentity.EmbeddedCoreProfile;
        Assert.ThrowsAny<Exception>(()=>codec.EncodePublishedConstructionRoot(root));
        root=Store().Read("one");root.PackageHash=new string('a',64);Store().Write("forged",root,false); // Carrier-only admission is deliberate.
        var before=model.Current;var frame=before.Frame;var draft=model.ResearchInputs();model.LocalDesignDirty=true;model.OrganizationName="keep";
        var load=model.BeginStaticCheckpointOperation("forged",true,discard:true)!;Assert.False(model.CompleteStaticCheckpointOperation(load,load.Execute()));
        Assert.Same(before,model.Current);Assert.Same(frame,model.Current.Frame);Assert.Same(draft,model.ResearchInputs());Assert.True(model.LocalDesignDirty);Assert.Equal("keep",model.OrganizationName);
        Assert.False(model.Load("forged",true));Assert.Same(before,model.Current);
    }
    [Fact] public void EligibilityCannotBeReusedAfterSameStampHostReplacementOrAnotherCommand()
    {
        var model=Model();var p=Publish(model);var offer=model.ConstructionEligibility(p.BlueprintId,1,"colony.sol");Assert.True(model.Save("one"));
        Assert.True(model.Load("one",true));Assert.False(model.EnqueuePublished(offer,ReviewUiModel.NewCommandId()));
        var current=model.ConstructionEligibility(p.BlueprintId,1,"colony.sol");Assert.True(model.AllocateResearch(300,ReviewUiModel.NewCommandId()));
        var before=model.Current.Frame.Strategy!.Civilization.Credits;Assert.False(model.EnqueuePublished(current,ReviewUiModel.NewCommandId()));Assert.Equal(before,model.Current.Frame.Strategy.Civilization.Credits);
    }
    [Fact] public void ValidUnmappedPublicationRemainsPublishableButCannotBePaidQueued()
    {
        var model=Model();var p=new ReviewDesignPublication(model);var fit=ReviewDesignPublication.Starter(p.Library)!;fit.ClassId=p.ClassId;
        const string component="component.survey-sensors.v1",installation="fit.unmapped-survey";
        fit.Installations!.Add(new(){InstallationId=installation,ComponentVersionId=component,Quantity=1});
        var source=p.Library.Catalogue!.Catalogue.Components.Single(c=>c.VersionId==component);
        foreach(var profile in fit.Profiles!)
        {var full=source.Modes.Single(m=>m.Profile==profile.Kind).FullPower;profile.Allocations!.Add(new(){InstallationId=installation,RequestedPower=full,AllocatedPower=full});}
        Assert.True(p.Publish(fit));Assert.True(model.Save("before"));var before=File.ReadAllBytes(FileFor("before"));
        var offer=model.ConstructionEligibility(p.BlueprintId,1,"colony.sol");Assert.False(offer.Supported);Assert.False(offer.CanQueue);Assert.NotEmpty(offer.SupportIssues);
        Assert.False(model.EnqueuePublished(offer,ReviewUiModel.NewCommandId()));Assert.True(model.Save("after"));Assert.Equal(before,File.ReadAllBytes(FileFor("after")));
    }
    [Fact] public void CompletedFrozenRevisionReloadsThroughTypedPlayerHostWithoutFreeEquipment()
    {
        var model=Model();var p=Publish(model);var offer=model.ConstructionEligibility(p.BlueprintId,1,"colony.sol");Assert.True(model.EnqueuePublished(offer,ReviewUiModel.NewCommandId()));
        Assert.True(p.Refresh(false));var fit=p.Edit(p.Library.Published.Single());fit.DisplayName="Later class";Assert.True(p.Publish(fit));
        bool complete=false;
        for(int i=0;i<100;i++){Assert.True(model.Save("progress",true));if(Store().Read("progress").PublishedBuiltShips!.Count>0){complete=true;break;}Assert.True(model.Advance());}
        Assert.True(complete);var root=Store().Read("progress");var built=Assert.Single(root.PublishedBuiltShips!);Assert.Equal(1,built.BlueprintRevision);
        var ship=model.Current.Frame.OwnShips.Single(s=>s.Condition.ShipId==built.ShipId);Assert.False(ship.CloakCapable);
        Assert.True(model.Load("progress",true));Assert.True(model.Save("resumed"));Assert.Equal(File.ReadAllBytes(FileFor("progress")),File.ReadAllBytes(FileFor("resumed")));
    }
    [Fact] public async Task HeldWorkerKeepsKnownPresentationReadableAndOldCompletionCannotUnlockNewOperation()
    {
        using var entered=new ManualResetEventSlim();using var release=new ManualResetEventSlim();
        bool block=false;var model=Model(s=>{if(block&&s==UnityReviewWriteStage.AfterPartialWrite){entered.Set();if(!release.Wait(TimeSpan.FromSeconds(10)))throw new TimeoutException();}});
        Assert.True(model.Save("input"));block=true;var controller=model.Current;var pair=model.GeneratedPresentation();var draft=model.ResearchInputs();model.LocalDesignDirty=true;
        var op=model.BeginStaticCheckpointOperation("held",false)!;var worker=Task.Run(op.Execute);
        try{Assert.True(entered.Wait(TimeSpan.FromSeconds(5)));
            await Task.Run(()=>{Assert.True(model.IsCurrentGeneratedPresentation(pair));Host(controller).Read(Human);}).WaitAsync(TimeSpan.FromSeconds(5));
            Assert.True(model.TryCheckWriteAdmission("held",out var error));Assert.Equal(ReviewCheckpointError.Busy,error);Assert.Null(model.BeginStaticCheckpointOperation("another",false));
            model.AbandonStaticCheckpointOperation(op);var next=model.BeginStaticCheckpointOperation("input",true,discard:true)!;Assert.NotNull(next);
            release.Set();Assert.False(model.CompleteStaticCheckpointOperation(op,await worker));Assert.True(model.Busy);Assert.Same(controller,model.Current);Assert.Same(draft,model.ResearchInputs());
            Assert.True(model.CompleteStaticCheckpointOperation(next,next.Execute()));Assert.False(model.Busy);Assert.NotSame(controller,model.Current);Assert.False(model.LocalDesignDirty);
        }finally{release.Set();await worker;}
    }
    [Fact] public void FinalOverwriteRaceKeepsWinnerAndBackgroundSnapshotMatchesSynchronousBytes()
    {
        var model=Model();Assert.True(model.Save("input"));var root=Store().Read("input");var winner=codec.EncodePublishedConstructionRoot(root);
        var racing=Store(s=>{if(s==UnityReviewWriteStage.BeforeReplace)Store().Write("race",root,false);});
        var failure=Assert.Throws<ReviewCheckpointStoreException>(()=>racing.Write("race",root,false));Assert.Equal(ReviewCheckpointError.SlotOccupied,failure.Error);Assert.Equal(winner,File.ReadAllBytes(FileFor("race")));
        var op=model.BeginStaticCheckpointOperation("background",false)!;Assert.True(model.CompleteStaticCheckpointOperation(op,op.Execute()));
        Assert.Equal(winner,File.ReadAllBytes(FileFor("background")));Assert.Empty(Directory.GetFiles(directory,"*.tmp"));
    }
    public void Dispose()
    {var full=Path.GetFullPath(directory);if(!full.StartsWith(Path.GetFullPath(Path.GetTempPath()),StringComparison.OrdinalIgnoreCase)||!Path.GetFileName(full).StartsWith("HobbyDS9-Published-Binding-",StringComparison.Ordinal))throw new InvalidOperationException();
     if(Directory.Exists(full))Directory.Delete(full,true);}
}
