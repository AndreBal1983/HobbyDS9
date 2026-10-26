#nullable enable
using System;
using HobbyDS9.Contracts;
using HobbyDS9.TacticalReviewClient;
namespace HobbyDS9.UnityReview
{
    public sealed partial class ReviewUiModel
    {
        private readonly ReviewColonisationGrowthSlotStore? colonisationGrowthSlots;
        private readonly CampaignColonisationExpansionPackageDto? colonisationGrowthPackage;
        public bool SupportsKitFreePassengers=>ColonisationGrowthCampaign;
        public bool ColonisationGrowthCampaign=>colonisationGrowthPackage!=null;
        public ReviewUiModel(CampaignColonisationExpansionPackageDto package,NaturalGalaxySettingsDto settings,ReviewNewtonsoftCodec codec,ReviewColonisationGrowthSlotStore slots)
        {
            if(slots==null)throw new ArgumentNullException(nameof(slots));this.codec=codec;colonisationGrowthPackage=codec.CopyGrowthPackage(package);
            colonisationPackage=colonisationGrowthPackage.BaseRosterV2!.BaseV1!;colonisationGrowthSlots=slots;
            generatedCampaignTemplate=codec.CopyPackage(colonisationPackage.Activities!.BaseTemplate!);StaticAstronomyCampaign=true;
            host=PhysicalReviewHost.StartColonisationGrowth(colonisationGrowthPackage,settings,codec,slots);
            Current=Controller(host,GeneratedHuman(generatedCampaignTemplate));host.CaptureGeneratedPresentation(Current.Frame);
        }
        private ReviewUiModel(CampaignColonisationExpansionPackageDto package,ReviewNewtonsoftCodec codec,ReviewColonisationGrowthSlotStore slots,PhysicalReviewHost loaded,string actor,ReviewLoadDiagnostic? diagnostic=null)
        {
            if(slots==null)throw new ArgumentNullException(nameof(slots));this.codec=codec;colonisationGrowthPackage=codec.CopyGrowthPackage(package);colonisationPackage=colonisationGrowthPackage.BaseRosterV2!.BaseV1!;colonisationGrowthSlots=slots;
            generatedCampaignTemplate=codec.CopyPackage(colonisationPackage.Activities!.BaseTemplate!);StaticAstronomyCampaign=true;host=loaded;
            diagnostic?.Record(ReviewLoadPhase.ControllerCreate,ReviewLoadEvent.Begin);
            Current=Controller(host,actor);
            diagnostic?.Record(ReviewLoadPhase.ControllerCreate,ReviewLoadEvent.NormalReturn);
        }
        public static ReviewUiModel ContinueColonisationGrowth(CampaignColonisationExpansionPackageDto package,ReviewNewtonsoftCodec codec,ReviewColonisationGrowthSlotStore slots,string slot,ReviewLoadDiagnostic? diagnostic=null)
        {
            if(slots==null)throw new ArgumentNullException(nameof(slots));var actor=GeneratedHuman(package.BaseRosterV2!.BaseV1!.Activities!.BaseTemplate!);
            var opened=PhysicalReviewHost.OpenColonisationGrowthFromSlot(package,codec,slots,slot,actor);
            if(!opened.Succeeded)throw new InvalidOperationException("Expansion checkpoint unavailable.");
            return new ReviewUiModel(package,codec,slots,opened.Host!,actor,diagnostic);
        }
        private ReviewCheckpointOperation? BeginColonisationGrowthCheckpointOperation(string slot,bool loading,bool overwrite,bool discard)
        {
            if(Busy||Closed||Current.HasPendingWork)return null;
            if(loading&&HasLocalEdits&&!discard){Message="Explicit discard required before load.";return null;}
            var source=Current;var sourceHost=host;var frame=source.Frame;Busy=true;
            try
            {
                var save=loading?null:sourceHost.PrepareColonisationGrowthCheckpointSave(frame,slot,overwrite);
                var package=colonisationGrowthPackage!;var workerCodec=codec;var workerStore=colonisationGrowthSlots!;
                ReviewCheckpointOperation? operation=null;
                operation=new ReviewCheckpointOperation(source,slot,loading,cancelled=>{
                    ReviewCheckpointCompletion result;
                    try
                    {
                        if(cancelled())result=new ReviewCheckpointCompletion(ReviewCheckpointError.Busy);
                        else if(!loading){var done=save!.Execute(cancelled);result=new ReviewCheckpointCompletion(done.Error,colonisationGrowthSaveWork:save);}
                        else{var opened=PhysicalReviewHost.OpenColonisationGrowthFromSlot(package,workerCodec,workerStore,slot,frame.Actor,frame.Battle?.EncounterId);
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
