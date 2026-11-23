#nullable enable
using System;
using HobbyDS9.Contracts;
using HobbyDS9.TacticalReviewClient;
namespace HobbyDS9.UnityReview
{
    public sealed partial class ReviewUiModel
    {
        private readonly CampaignColonisationRosterPackageDto? colonisationRosterPackage;
        public bool ColonisationRosterCampaign=>colonisationRosterPackage!=null;
        public ReviewUiModel(CampaignColonisationRosterPackageDto package,NaturalGalaxySettingsDto settings,ReviewNewtonsoftCodec codec,ReviewColonisationSlotStore slots)
        {
            RequireColonisationStore(slots,true);this.codec=codec;colonisationRosterPackage=codec.CopyRosterPackage(package);
            colonisationPackage=colonisationRosterPackage.BaseV1!;colonisationSlots=slots;
            generatedCampaignTemplate=codec.CopyPackage(colonisationPackage.Activities!.BaseTemplate!);StaticAstronomyCampaign=true;
            host=PhysicalReviewHost.StartColonisationRoster(colonisationRosterPackage,settings,codec,slots);
            Current=Controller(host,GeneratedHuman(generatedCampaignTemplate));host.CaptureGeneratedPresentation(Current.Frame);
        }
        private ReviewUiModel(CampaignColonisationRosterPackageDto package,ReviewNewtonsoftCodec codec,ReviewColonisationSlotStore slots,PhysicalReviewHost loaded,string actor,ReviewLoadDiagnostic? diagnostic=null)
        {
            RequireColonisationStore(slots,true);this.codec=codec;colonisationRosterPackage=codec.CopyRosterPackage(package);colonisationPackage=colonisationRosterPackage.BaseV1!;colonisationSlots=slots;
            generatedCampaignTemplate=codec.CopyPackage(colonisationPackage.Activities!.BaseTemplate!);StaticAstronomyCampaign=true;host=loaded;
            diagnostic?.Record(ReviewLoadPhase.ControllerCreate,ReviewLoadEvent.Begin);
            Current=Controller(host,actor);
            diagnostic?.Record(ReviewLoadPhase.ControllerCreate,ReviewLoadEvent.NormalReturn);
        }
        private static void RequireColonisationStore(ReviewColonisationSlotStore slots,bool roster)
        {if(slots==null||slots.RosterV2!=roster)throw new ArgumentException("Distinct colonisation slot namespace required.");}
        public static ReviewUiModel ContinueColonisationRoster(CampaignColonisationRosterPackageDto package,ReviewNewtonsoftCodec codec,ReviewColonisationSlotStore slots,string slot,ReviewLoadDiagnostic? diagnostic=null)
        {
            RequireColonisationStore(slots,true);var actor=GeneratedHuman(package.BaseV1!.Activities!.BaseTemplate!);
            var opened=PhysicalReviewHost.OpenColonisationRosterFromSlot(package,codec,slots,slot,actor,diagnostic:diagnostic);
            if(!opened.Succeeded)throw new InvalidOperationException("Colonisation V2 checkpoint unavailable.");
            return new ReviewUiModel(package,codec,slots,opened.Host!,actor,diagnostic);
        }
        private PhysicalHostOpenResult OpenColonisationModelSource(string slot,string actor,string? encounter=null)
            =>OpenColonisationModelSource(colonisationPackage!,colonisationRosterPackage,codec,colonisationSlots!,slot,actor,encounter);
        private static PhysicalHostOpenResult OpenColonisationModelSource(CampaignColonisationPackageDto package,CampaignColonisationRosterPackageDto? roster,
            ReviewNewtonsoftCodec workerCodec,ReviewColonisationSlotStore store,string slot,string actor,string? encounter)
            =>roster==null?PhysicalReviewHost.OpenColonisationFromSlot(package,workerCodec,store,slot,actor,encounter)
                :PhysicalReviewHost.OpenColonisationRosterFromSlot(roster,workerCodec,store,slot,actor,encounter);
    }
}
