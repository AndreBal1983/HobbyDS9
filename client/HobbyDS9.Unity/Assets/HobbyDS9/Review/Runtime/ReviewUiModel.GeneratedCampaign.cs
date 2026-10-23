#nullable enable
using System;
using System.Linq;
using HobbyDS9.Contracts;
using HobbyDS9.TacticalReviewClient;

namespace HobbyDS9.UnityReview
{
    public sealed partial class ReviewUiModel
    {
        private readonly CampaignPlayableFleetPackageDto? generatedCampaignTemplate;
        private readonly ReviewGeneratedCampaignSlotStore? generatedCampaignSlots;
        public bool GeneratedCampaign=>generatedCampaignTemplate!=null;
        public ReviewUiModel(CampaignPlayableFleetPackageDto template,NaturalGalaxySettingsDto settings,
            ReviewNewtonsoftCodec codec,ReviewGeneratedCampaignSlotStore slots)
        {
            this.codec=codec;generatedCampaignTemplate=codec.CopyPackage(template);generatedCampaignSlots=slots;
            host=PhysicalReviewHost.StartGeneratedCampaign(generatedCampaignTemplate,settings,codec,slots);
            Current=Controller(host,GeneratedHuman(generatedCampaignTemplate));
        }
        private ReviewUiModel(CampaignPlayableFleetPackageDto template,ReviewNewtonsoftCodec codec,
            ReviewGeneratedCampaignSlotStore slots,PhysicalReviewHost loaded,string actor)
        {
            this.codec=codec;generatedCampaignTemplate=codec.CopyPackage(template);generatedCampaignSlots=slots;
            host=loaded;Current=Controller(host,actor);
        }
        private static string GeneratedHuman(CampaignPlayableFleetPackageDto template)
        {
            var humans=template.Integrated!.Physical!.CorePackage!.Campaign!.InitialState.Civilizations!
                .Where(c=>c.ControllerKind==ControllerKind.Human).ToList();
            if(humans.Count!=1)throw new ArgumentException("Generated campaign requires exactly one human controller.");
            return humans[0].CivilizationId;
        }
        public static ReviewUiModel ContinueGeneratedCampaign(CampaignPlayableFleetPackageDto template,
            ReviewNewtonsoftCodec codec,ReviewGeneratedCampaignSlotStore slots,string slot)
        {
            var actor=GeneratedHuman(template);
            var opened=PhysicalReviewHost.OpenGeneratedCampaignFromSlot(template,codec,slots,slot,actor);
            if(!opened.Succeeded)throw new InvalidOperationException("Generated campaign could not be loaded.");
            // Only a fully validated host reaches the published model. No generation occurs here.
            return new ReviewUiModel(template,codec,slots,opened.Host!,actor);
        }
        public CampaignGeneratedMapDto GeneratedMap()
        {
            if(!GeneratedCampaign)throw new InvalidOperationException("No generated campaign is active.");
            return host.CaptureGeneratedMap(Current.Frame);
        }
    }
}
