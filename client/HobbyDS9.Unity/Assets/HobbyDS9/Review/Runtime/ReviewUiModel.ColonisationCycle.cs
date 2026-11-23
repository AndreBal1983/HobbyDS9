#nullable enable
using System;
using HobbyDS9.Contracts;
using HobbyDS9.TacticalReviewClient;
namespace HobbyDS9.UnityReview
{
    public sealed partial class ReviewUiModel
    {
        private readonly ReviewColonisationCycleSlotStore? colonisationCycleSlots;
        private readonly CampaignColonisationCyclePackageDto? colonisationCyclePackage;
        public bool ColonisationCycleCampaign=>colonisationCyclePackage!=null;
        public ReviewUiModel(CampaignColonisationCyclePackageDto package,NaturalGalaxySettingsDto settings,ReviewNewtonsoftCodec codec,ReviewColonisationCycleSlotStore slots)
        {
            if(slots==null)throw new ArgumentNullException(nameof(slots));this.codec=codec;colonisationCyclePackage=codec.CopyCyclePackage(package);
            colonisationPackage=colonisationCyclePackage.BaseGrowth!.BaseRosterV2!.BaseV1!;colonisationCycleSlots=slots;
            generatedCampaignTemplate=codec.CopyPackage(colonisationPackage.Activities!.BaseTemplate!);StaticAstronomyCampaign=true;
            host=PhysicalReviewHost.StartColonisationCycle(colonisationCyclePackage,settings,codec,slots);
            Current=Controller(host,GeneratedHuman(generatedCampaignTemplate));host.CaptureGeneratedPresentation(Current.Frame);
        }
        private ReviewUiModel(CampaignColonisationCyclePackageDto package,ReviewNewtonsoftCodec codec,ReviewColonisationCycleSlotStore slots,PhysicalReviewHost loaded,string actor,ReviewLoadDiagnostic? diagnostic=null)
        {
            if(slots==null)throw new ArgumentNullException(nameof(slots));this.codec=codec;colonisationCyclePackage=codec.CopyCyclePackage(package);colonisationPackage=colonisationCyclePackage.BaseGrowth!.BaseRosterV2!.BaseV1!;colonisationCycleSlots=slots;
            generatedCampaignTemplate=codec.CopyPackage(colonisationPackage.Activities!.BaseTemplate!);StaticAstronomyCampaign=true;host=loaded;
            diagnostic?.Record(ReviewLoadPhase.ControllerCreate,ReviewLoadEvent.Begin);
            Current=Controller(host,actor);
            diagnostic?.Record(ReviewLoadPhase.ControllerCreate,ReviewLoadEvent.NormalReturn);
        }
        public static ReviewUiModel ContinueColonisationCycle(CampaignColonisationCyclePackageDto package,ReviewNewtonsoftCodec codec,ReviewColonisationCycleSlotStore slots,string slot,ReviewLoadDiagnostic? diagnostic=null)
        {
            if(slots==null)throw new ArgumentNullException(nameof(slots));var actor=GeneratedHuman(package.BaseGrowth!.BaseRosterV2!.BaseV1!.Activities!.BaseTemplate!);
            var opened=PhysicalReviewHost.OpenColonisationCycleFromSlot(package,codec,slots,slot,actor);
            if(!opened.Succeeded)throw new InvalidOperationException("Expansion checkpoint unavailable.");
            return new ReviewUiModel(package,codec,slots,opened.Host!,actor,diagnostic);
        }
        private ReviewCheckpointOperation? BeginColonisationCycleCheckpointOperation(string slot,bool loading,bool overwrite,bool discard)
        {
            if(Busy||Closed||Current.HasPendingWork)return null;
            if(loading&&HasLocalEdits&&!discard){Message="Explicit discard required before load.";return null;}
            var source=Current;var sourceHost=host;var frame=source.Frame;Busy=true;
            try
            {
                var save=loading?null:sourceHost.PrepareColonisationCycleCheckpointSave(frame,slot,overwrite);
                var package=colonisationCyclePackage!;var workerCodec=codec;var workerStore=colonisationCycleSlots!;
                ReviewCheckpointOperation? operation=null;
                operation=new ReviewCheckpointOperation(source,slot,loading,cancelled=>{
                    ReviewCheckpointCompletion result;
                    try
                    {
                        if(cancelled())result=new ReviewCheckpointCompletion(ReviewCheckpointError.Busy);
                        else if(!loading){var done=save!.Execute(cancelled);result=new ReviewCheckpointCompletion(done.Error,colonisationCycleSaveWork:save);}
                        else{var opened=PhysicalReviewHost.OpenColonisationCycleFromSlot(package,workerCodec,workerStore,slot,frame.Actor,frame.Battle?.EncounterId);
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
