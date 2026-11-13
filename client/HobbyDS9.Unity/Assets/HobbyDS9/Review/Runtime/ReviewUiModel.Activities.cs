#nullable enable
using System;
using System.Linq;
using System.Runtime.CompilerServices;
using HobbyDS9.Contracts;
using HobbyDS9.TacticalReviewClient;

namespace HobbyDS9.UnityReview
{
    public sealed partial class ReviewUiModel
    {
        private readonly CampaignActivitiesPackageDto? activitiesPackage;
        private readonly ReviewActivitiesSlotStore? activitiesSlots;
        public bool ActivitiesCampaign=>activitiesPackage!=null||StreamedActivitiesCampaign||ColonisationCampaign;
        public bool DepartureInterruptsExpedition(KnownRouteOffer offer)
        {
            if(!ActivitiesCampaign||Busy||Closed||!offer.Route.Success||offer.Route.Steps.Count==0||offer.Actor!=Current.Frame.Actor
                ||offer.Turn!=Current.Frame.Status.Turn||offer.CampaignRevision!=Current.Frame.Status.CampaignRevision)return false;
            return DepartureInterruptsPlanetSurvey(offer)||Activities().Findings.Any(f=>f.ActorId==offer.Actor&&f.FleetId==offer.Fleet
                &&(f.Stage==ActivityProgressStage.Active||f.Stage==ActivityProgressStage.AwaitingClaim));
        }
        public bool DepartureInterruptsPlanetSurvey(KnownRouteOffer offer)
        {
            if(!ColonisationCampaign||Busy||Closed||!offer.Route.Success||offer.Route.Steps.Count==0||offer.Actor!=Current.Frame.Actor
                ||offer.Turn!=Current.Frame.Status.Turn||offer.CampaignRevision!=Current.Frame.Status.CampaignRevision)return false;
            var ownShips=ColonisationActivities().Ships.Where(s=>s.FleetId==offer.Fleet).Select(s=>s.ShipId).ToHashSet(StringComparer.Ordinal);
            return Colonisation().Surveys.Any(s=>s.Stage==PlanetSurveyStage.Active&&ownShips.Contains(s.ShipId));
        }
        public ReviewUiModel(CampaignActivitiesPackageDto package,NaturalGalaxySettingsDto settings,
            ReviewNewtonsoftCodec codec,ReviewActivitiesSlotStore slots)
        {
            this.codec=codec;activitiesPackage=codec.CopyPackage(package);activitiesSlots=slots;
            generatedCampaignTemplate=codec.CopyPackage(package.BaseTemplate!);
            // Known astronomy rendering capability, not outer-root admission.
            StaticAstronomyCampaign=true;
            host=PhysicalReviewHost.StartActivities(activitiesPackage,settings,codec,slots);
            Current=Controller(host,GeneratedHuman(generatedCampaignTemplate));host.CaptureGeneratedPresentation(Current.Frame);
        }
        private ReviewUiModel(CampaignActivitiesPackageDto package,ReviewNewtonsoftCodec codec,
            ReviewActivitiesSlotStore slots,PhysicalReviewHost loaded,string actor)
        {this.codec=codec;activitiesPackage=codec.CopyPackage(package);activitiesSlots=slots;
         generatedCampaignTemplate=codec.CopyPackage(package.BaseTemplate!);StaticAstronomyCampaign=true;host=loaded;Current=Controller(host,actor);}
        public static ReviewUiModel ContinueActivities(CampaignActivitiesPackageDto package,ReviewNewtonsoftCodec codec,
            ReviewActivitiesSlotStore slots,string slot)
        {var actor=GeneratedHuman(package.BaseTemplate!);var opened=PhysicalReviewHost.OpenActivitiesFromSlot(package,codec,slots,slot,actor);
         if(!opened.Succeeded)throw new InvalidOperationException("Activity campaign checkpoint unavailable.");
         return new ReviewUiModel(package,codec,slots,opened.Host!,actor);}
        public CampaignActivityObservationDto Activities()
        {if(!ActivitiesCampaign||Busy||Closed)throw new InvalidOperationException("Activity observation unavailable.");return ColonisationCampaign?ColonisationActivities():StreamedActivitiesCampaign?StreamedActivities().Activities:host.CaptureActivities(Current.Frame);}
        public CampaignActivityEligibilityDto ActivityEligibility(CampaignActivityCommandDto command)
        {if(!ActivitiesCampaign||Busy||Closed||Current.HasPendingWork)throw new InvalidOperationException("Activity eligibility unavailable.");return ColonisationCampaign?host.EvaluateColonisationActivity(Current.Frame,command):StreamedActivitiesCampaign?host.EvaluateStreamedActivity(Current.Frame,command):host.EvaluateActivity(Current.Frame,command);}
        public bool SubmitActivity(CampaignActivityCommandDto command)=>Run(()=>{
            if(!ActivitiesCampaign||Current.HasPendingWork||Current.Dirty||Current.RefreshRequired)return Unavailable();
            CampaignTacticalMutationResult result;
            if(ColonisationCampaign)host.SubmitColonisationActivity(Current.Frame,command,out result);else if(StreamedActivitiesCampaign)host.SubmitStreamedActivity(Current.Frame,command,out result);else host.SubmitActivity(Current.Frame,command,out result);
            Message=result.Accepted?"Activity order accepted.":"Activity order unavailable: "+result.Error;
            if(!result.Accepted)return false;
            var refreshed=Current.Refresh();if(refreshed&&ResearchDraft!=null)ResearchDraft=ResearchDraft.PreserveUnsubmittedEdits(Current.Frame);return refreshed;
        });
        private ReviewCheckpointOperation? BeginActivitiesCheckpointOperation(string slot,bool loading,bool overwrite,bool discard)
        {
            if(Busy||Closed||Current.HasPendingWork)return null;
            if(loading&&HasLocalEdits&&!discard){Message="Explicit discard required before load.";return null;}
            var source=Current;var sourceHost=host;var frame=source.Frame;Busy=true;
            try
            {
                var save=loading?null:sourceHost.PrepareActivitiesCheckpointSave(frame,slot,overwrite);
                var package=activitiesPackage!;var workerCodec=codec;var workerStore=activitiesSlots!;
                ReviewCheckpointOperation? operation=null;
                operation=new ReviewCheckpointOperation(source,slot,loading,cancelled=>{
                    ReviewCheckpointCompletion result;
                    try
                    {
                        if(cancelled())result=new ReviewCheckpointCompletion(ReviewCheckpointError.Busy);
                        else if(!loading){var done=save!.Execute(cancelled);result=new ReviewCheckpointCompletion(done.Error,activitiesSaveWork:save);}
                        else{var opened=PhysicalReviewHost.OpenActivitiesFromSlot(package,workerCodec,workerStore,slot,frame.Actor,frame.Battle?.EncounterId);
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
