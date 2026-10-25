#nullable enable
using System;
using System.Linq;
using HobbyDS9.Contracts;
using HobbyDS9.TacticalReviewClient;

namespace HobbyDS9.UnityReview
{
    public sealed partial class ReviewUiModel
    {
        private readonly CampaignColonisationPackageDto? colonisationPackage;
        private readonly ReviewColonisationSlotStore? colonisationSlots;
        public bool ColonisationCampaign=>colonisationPackage!=null;
        public ReviewUiModel(CampaignColonisationPackageDto package,NaturalGalaxySettingsDto settings,
            ReviewNewtonsoftCodec codec,ReviewColonisationSlotStore slots)
        {
            this.codec=codec;colonisationPackage=codec.CopyPackage(package);colonisationSlots=slots;
            generatedCampaignTemplate=codec.CopyPackage(package.Activities!.BaseTemplate!);
            // Known astronomy rendering capability, not outer-root admission.
            StaticAstronomyCampaign=true;
            host=PhysicalReviewHost.StartColonisation(colonisationPackage,settings,codec,slots);
            Current=Controller(host,GeneratedHuman(generatedCampaignTemplate));host.CaptureGeneratedPresentation(Current.Frame);
        }
        private ReviewUiModel(CampaignColonisationPackageDto package,ReviewNewtonsoftCodec codec,
            ReviewColonisationSlotStore slots,PhysicalReviewHost loaded,string actor)
        {this.codec=codec;colonisationPackage=codec.CopyPackage(package);colonisationSlots=slots;
         generatedCampaignTemplate=codec.CopyPackage(package.Activities!.BaseTemplate!);StaticAstronomyCampaign=true;host=loaded;Current=Controller(host,actor);}
        public static ReviewUiModel ContinueColonisation(CampaignColonisationPackageDto package,ReviewNewtonsoftCodec codec,
            ReviewColonisationSlotStore slots,string slot)
        {var actor=GeneratedHuman(package.Activities!.BaseTemplate!);var opened=PhysicalReviewHost.OpenColonisationFromSlot(package,codec,slots,slot,actor);
         if(!opened.Succeeded)throw new InvalidOperationException("Colonisation campaign checkpoint unavailable.");
         return new ReviewUiModel(package,codec,slots,opened.Host!,actor);}
        private ReviewFrame? colonisationObservationFrame;
        private ColonisationCampaignSnapshot? colonisationObservation;
        private System.Collections.Generic.IReadOnlyList<ColonisationPlanetSnapshot>? colonisationPlanets;
        private void PrepareColonisationObservation()
        {
            if(!ColonisationCampaign||Busy||Closed)throw new InvalidOperationException("Colonisation observation unavailable.");
            if(ReferenceEquals(Current.Frame,colonisationObservationFrame))return;
            var frame=Current.Frame;var observation=host.CaptureColonisation(frame);
            var planets=host.CaptureColonisationPlanets(frame).Select(x=>new ColonisationPlanetSnapshot(x)).ToList().AsReadOnly();
            colonisationObservation=observation;colonisationPlanets=planets;colonisationObservationFrame=frame;
        }
        public ColonisationCampaignSnapshot Colonisation(){PrepareColonisationObservation();return colonisationObservation!;}
        public System.Collections.Generic.IReadOnlyList<ColonisationPlanetSnapshot> ColonisationPlanets(){PrepareColonisationObservation();return colonisationPlanets!;}
        public CampaignActivityObservationDto ColonisationActivities()
        {PrepareColonisationObservation();return host.CaptureColonisationActivities(Current.Frame);}
        private ReviewCheckpointOperation? BeginColonisationCheckpointOperation(string slot,bool loading,bool overwrite,bool discard)
        {
            if(Busy||Closed||Current.HasPendingWork)return null;
            if(loading&&HasLocalEdits&&!discard){Message="Explicit discard required before load.";return null;}
            var source=Current;var sourceHost=host;var frame=source.Frame;Busy=true;
            try
            {
                var save=loading?null:sourceHost.PrepareColonisationCheckpointSave(frame,slot,overwrite);
                var package=colonisationPackage!;var workerCodec=codec;var workerStore=colonisationSlots!;
                ReviewCheckpointOperation? operation=null;
                operation=new ReviewCheckpointOperation(source,slot,loading,cancelled=>{
                    ReviewCheckpointCompletion result;
                    try
                    {
                        if(cancelled())result=new ReviewCheckpointCompletion(ReviewCheckpointError.Busy);
                        else if(!loading){var done=save!.Execute(cancelled);result=new ReviewCheckpointCompletion(done.Error,colonisationSaveWork:save);}
                        else{var opened=PhysicalReviewHost.OpenColonisationFromSlot(package,workerCodec,workerStore,slot,frame.Actor,frame.Battle?.EncounterId);
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
