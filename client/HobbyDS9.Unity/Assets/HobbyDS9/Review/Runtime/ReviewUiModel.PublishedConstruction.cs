#nullable enable
using System;
using System.Runtime.CompilerServices;
using HobbyDS9.Contracts;
using HobbyDS9.TacticalReviewClient;

namespace HobbyDS9.UnityReview
{
    public sealed partial class ReviewUiModel
    {
        private readonly CampaignPublishedConstructionPackageDto? publishedConstructionPackage;
        private readonly ReviewPublishedConstructionSlotStore? publishedConstructionSlots;
        public bool PublishedConstructionCampaign=>publishedConstructionPackage!=null;
        // Multiple views/modal redraws may observe the same frame. Issuance is reference-keyed,
        // weakly held, and scoped to that exact controller/frame, never to the latest redraw.
        private ConditionalWeakTable<CampaignPublishedConstructionEligibility,object> constructionObservations=new();
        private ReviewController? constructionObserver;
        private ReviewFrame? constructionFrame;
        public ReviewUiModel(CampaignPublishedConstructionPackageDto package,NaturalGalaxySettingsDto settings,
            ReviewNewtonsoftCodec codec,ReviewPublishedConstructionSlotStore slots)
        {
            this.codec=codec;publishedConstructionPackage=codec.CopyPackage(package);publishedConstructionSlots=slots;
            generatedCampaignTemplate=codec.CopyPackage(package.BaseTemplate!);
            // Known astronomy rendering capability, not outer-root admission.
            StaticAstronomyCampaign=true;
            host=PhysicalReviewHost.StartPublishedConstruction(publishedConstructionPackage,settings,codec,slots);
            Current=Controller(host,GeneratedHuman(generatedCampaignTemplate));host.CaptureGeneratedPresentation(Current.Frame);
        }
        private ReviewUiModel(CampaignPublishedConstructionPackageDto package,ReviewNewtonsoftCodec codec,
            ReviewPublishedConstructionSlotStore slots,PhysicalReviewHost loaded,string actor)
        {this.codec=codec;publishedConstructionPackage=codec.CopyPackage(package);publishedConstructionSlots=slots;
         generatedCampaignTemplate=codec.CopyPackage(package.BaseTemplate!);StaticAstronomyCampaign=true;host=loaded;Current=Controller(host,actor);}
        public static ReviewUiModel ContinuePublishedConstruction(CampaignPublishedConstructionPackageDto package,ReviewNewtonsoftCodec codec,
            ReviewPublishedConstructionSlotStore slots,string slot)
        {var actor=GeneratedHuman(package.BaseTemplate!);var opened=PhysicalReviewHost.OpenPublishedConstructionFromSlot(package,codec,slots,slot,actor);
         if(!opened.Succeeded)throw new InvalidOperationException("Published-construction checkpoint unavailable.");
         return new ReviewUiModel(package,codec,slots,opened.Host!,actor);}
        public CampaignPublishedConstructionEligibility ConstructionEligibility(string blueprint,int revision,string colony)
        {if(!PublishedConstructionCampaign||Busy||Closed)throw new InvalidOperationException("Construction observation unavailable.");
         var result=host.CaptureConstructionEligibility(Current.Frame,blueprint,revision,colony);
         if(!ReferenceEquals(constructionObserver,Current)||!ReferenceEquals(constructionFrame,Current.Frame))
             constructionObservations=new ConditionalWeakTable<CampaignPublishedConstructionEligibility,object>();
         constructionObserver=Current;constructionFrame=Current.Frame;constructionObservations.Add(result,new object());return result;}
        public bool EnqueuePublished(CampaignPublishedConstructionEligibility observed,string command)
        {
            if(!PublishedConstructionCampaign||Busy||Closed||Current.HasPendingWork||Current.Dirty||Current.RefreshRequired
                ||observed==null||!constructionObservations.TryGetValue(observed,out _)||!ReferenceEquals(Current,constructionObserver)||!ReferenceEquals(Current.Frame,constructionFrame)
                ||observed.ActorId!=Current.Frame.Actor||observed.Turn!=Current.Frame.Status.Turn||observed.CampaignRevision!=Current.Frame.Status.CampaignRevision)
            {Message="Construction observation changed. Refresh before retrying; no payment made.";return false;}
            var c=StrategicEnvelope(CommandKind.EnqueueBlueprintConstruction,command);c.EnqueueBlueprintConstruction=new EnqueueBlueprintConstructionPayload{
                ColonyId=observed.ColonyId,BlueprintId=observed.BlueprintId,Revision=observed.BlueprintRevision};
            return Strategic(c); // Revalidates all authority; CanQueue is never trusted permission.
        }
        private ReviewCheckpointOperation? BeginPublishedCheckpointOperation(string slot,bool loading,bool overwrite,bool discard)
        {
            if(Busy||Closed||Current.HasPendingWork)return null;
            if(loading&&HasLocalEdits&&!discard){Message="Explicit discard required before load.";return null;}
            var source=Current;var sourceHost=host;var frame=source.Frame;Busy=true;
            try
            {
                var save=loading?null:sourceHost.PreparePublishedCheckpointSave(frame,slot,overwrite);
                var package=publishedConstructionPackage!;var workerCodec=codec;var workerStore=publishedConstructionSlots!;
                ReviewCheckpointOperation? operation=null;
                operation=new ReviewCheckpointOperation(source,slot,loading,cancelled=>{
                    ReviewCheckpointCompletion result;
                    try
                    {
                        if(cancelled())result=new ReviewCheckpointCompletion(ReviewCheckpointError.Busy);
                        else if(!loading){var done=save!.Execute(cancelled);result=new ReviewCheckpointCompletion(done.Error,publishedSaveWork:save);}
                        else{var opened=PhysicalReviewHost.OpenPublishedConstructionFromSlot(package,workerCodec,workerStore,slot,frame.Actor,frame.Battle?.EncounterId);
                            if(!opened.Succeeded)result=new ReviewCheckpointCompletion(opened.Error);
                            else{var next=Controller(opened.Host!,frame.Actor);opened.Host!.CaptureGeneratedPresentation(next.Frame);
                                result=new ReviewCheckpointCompletion(ReviewCheckpointError.None,opened.Host,next);}}
                    }
                    catch(Exception){result=new ReviewCheckpointCompletion(loading?ReviewCheckpointError.InvalidCheckpoint:ReviewCheckpointError.SerializationFailed);}
                    result.Operation=operation;return result;
                });
                checkpointOperation=operation;Message=loading?"Loading and validating campaign…":"Saving and validating campaign…";return operation;
            }
            catch(Exception){Busy=false;Message="Checkpoint result: SerializationFailed";return null;}
        }
    }
}
