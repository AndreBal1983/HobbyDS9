#nullable enable
using System;
using System.Runtime.CompilerServices;
using HobbyDS9.Contracts;
using HobbyDS9.TacticalReviewClient;

namespace HobbyDS9.UnityReview
{
    public sealed partial class ReviewUiModel
    {
        private readonly CampaignStreamedActivitiesPackageDto? streamedActivitiesPackage;
        private readonly ReviewStreamedActivitiesSlotStore? streamedActivitiesSlots;
        public bool StreamedActivitiesCampaign=>streamedActivitiesPackage!=null;
        public ReviewUiModel(CampaignStreamedActivitiesPackageDto package,NaturalGalaxySettingsDto settings,
            ReviewNewtonsoftCodec codec,ReviewStreamedActivitiesSlotStore slots)
        {
            this.codec=codec;streamedActivitiesPackage=codec.CopyPackage(package);streamedActivitiesSlots=slots;
            generatedCampaignTemplate=codec.CopyPackage(package.ContentPackage!.BaseTemplate!);
            // Known astronomy rendering capability, not outer-root admission.
            StaticAstronomyCampaign=true;
            host=PhysicalReviewHost.StartStreamedActivities(streamedActivitiesPackage,settings,codec,slots);
            Current=Controller(host,GeneratedHuman(generatedCampaignTemplate));host.CaptureGeneratedPresentation(Current.Frame);
        }
        private ReviewUiModel(CampaignStreamedActivitiesPackageDto package,ReviewNewtonsoftCodec codec,
            ReviewStreamedActivitiesSlotStore slots,PhysicalReviewHost loaded,string actor)
        {this.codec=codec;streamedActivitiesPackage=codec.CopyPackage(package);streamedActivitiesSlots=slots;
         generatedCampaignTemplate=codec.CopyPackage(package.ContentPackage!.BaseTemplate!);StaticAstronomyCampaign=true;host=loaded;Current=Controller(host,actor);}
        public static ReviewUiModel ContinueStreamedActivities(CampaignStreamedActivitiesPackageDto package,ReviewNewtonsoftCodec codec,
            ReviewStreamedActivitiesSlotStore slots,string slot)
        {var actor=GeneratedHuman(package.ContentPackage!.BaseTemplate!);var opened=PhysicalReviewHost.OpenStreamedActivitiesFromSlot(package,codec,slots,slot,actor);
         if(!opened.Succeeded)throw new InvalidOperationException("Activity campaign checkpoint unavailable.");
         return new ReviewUiModel(package,codec,slots,opened.Host!,actor);}
        public CampaignStreamedActivityObservationDto StreamedActivities()
        {if(!StreamedActivitiesCampaign||Busy||Closed)throw new InvalidOperationException("Activity observation unavailable.");return host.CaptureStreamedActivities(Current.Frame);}
        public FrontierCommissionSnapshot FrontierCommission()
        {if((!StreamedActivitiesCampaign&&!ColonisationCampaign)||Busy||Closed)throw new InvalidOperationException("Commission observation unavailable.");return host.CaptureFrontierCommission(Current.Frame);}
        public CampaignActivityEligibilityDto StreamedActivityEligibility(CampaignActivityCommandDto command)
        {if(!StreamedActivitiesCampaign||Busy||Closed||Current.HasPendingWork)throw new InvalidOperationException("Activity eligibility unavailable.");return host.EvaluateStreamedActivity(Current.Frame,command);}
        public bool SubmitStreamedActivity(CampaignActivityCommandDto command)=>Run(()=>{
            if(!StreamedActivitiesCampaign||Current.HasPendingWork||Current.Dirty||Current.RefreshRequired)return Unavailable();
            host.SubmitStreamedActivity(Current.Frame,command,out var result);Message=result.Accepted?"Activity order accepted.":"Activity order unavailable: "+result.Error;
            if(!result.Accepted)return false;
            var refreshed=Current.Refresh();if(refreshed&&ResearchDraft!=null)ResearchDraft=ResearchDraft.PreserveUnsubmittedEdits(Current.Frame);return refreshed;
        });
        private ReviewCheckpointOperation? BeginStreamedActivitiesCheckpointOperation(string slot,bool loading,bool overwrite,bool discard)
        {
            if(Busy||Closed||Current.HasPendingWork)return null;
            if(loading&&HasLocalEdits&&!discard){Message="Explicit discard required before load.";return null;}
            var source=Current;var sourceHost=host;var frame=source.Frame;Busy=true;
            try
            {
                var save=loading?null:sourceHost.PrepareStreamedActivitiesCheckpointSave(frame,slot,overwrite);
                var package=streamedActivitiesPackage!;var workerCodec=codec;var workerStore=streamedActivitiesSlots!;
                ReviewCheckpointOperation? operation=null;
                operation=new ReviewCheckpointOperation(source,slot,loading,cancelled=>{
                    ReviewCheckpointCompletion result;
                    try
                    {
                        if(cancelled())result=new ReviewCheckpointCompletion(ReviewCheckpointError.Busy);
                        else if(!loading){var done=save!.Execute(cancelled);result=new ReviewCheckpointCompletion(done.Error,streamedActivitiesSaveWork:save);}
                        else{var opened=PhysicalReviewHost.OpenStreamedActivitiesFromSlot(package,workerCodec,workerStore,slot,frame.Actor,frame.Battle?.EncounterId);
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
