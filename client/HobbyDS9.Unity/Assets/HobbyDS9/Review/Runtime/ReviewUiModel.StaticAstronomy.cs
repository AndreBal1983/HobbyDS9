#nullable enable
using System;
using HobbyDS9.Contracts;
using HobbyDS9.TacticalReviewClient;

namespace HobbyDS9.UnityReview
{
    public sealed partial class ReviewUiModel
    {
        public bool StaticAstronomyCampaign { get; }
        private readonly ReviewStaticAstronomyCampaignSlotStore? staticAstronomySlots;
        public ReviewUiModel(CampaignPlayableFleetPackageDto template,NaturalGalaxySettingsDto settings,
            ReviewNewtonsoftCodec codec,ReviewStaticAstronomyCampaignSlotStore slots)
        {
            this.codec=codec;generatedCampaignTemplate=codec.CopyPackage(template);staticAstronomySlots=slots;
            host=PhysicalReviewHost.StartStaticAstronomyCampaign(generatedCampaignTemplate,settings,codec,slots);
            Current=Controller(host,GeneratedHuman(generatedCampaignTemplate));StaticAstronomyCampaign=true;
            host.CaptureGeneratedPresentation(Current.Frame);
        }
        public static ReviewUiModel ContinueStaticAstronomyCampaign(CampaignPlayableFleetPackageDto template,
            ReviewNewtonsoftCodec codec,ReviewStaticAstronomyCampaignSlotStore slots,string slot)
        {
            var actor=GeneratedHuman(template);
            var opened=PhysicalReviewHost.OpenStaticAstronomyCampaignFromSlot(template,codec,slots,slot,actor);
            if(!opened.Succeeded)throw new InvalidOperationException("Static astronomy campaign could not be loaded.");
            return new ReviewUiModel(template,codec,slots,opened.Host!,actor);
        }
        private ReviewUiModel(CampaignPlayableFleetPackageDto template,ReviewNewtonsoftCodec codec,ReviewStaticAstronomyCampaignSlotStore slots,
            PhysicalReviewHost loaded,string actor)
        {
            this.codec=codec;generatedCampaignTemplate=codec.CopyPackage(template);staticAstronomySlots=slots;
            host=loaded;Current=Controller(host,actor);StaticAstronomyCampaign=true;
        }
        public GeneratedCampaignPresentation GeneratedPresentation()
        {
            if(!StaticAstronomyCampaign)throw new InvalidOperationException("No static astronomy campaign is active.");
            return host.CaptureGeneratedPresentation(Current.Frame);
        }
        public bool IsCurrentGeneratedPresentation(GeneratedCampaignPresentation presentation)
        {
            try{host.ValidateGeneratedPresentation(Current.Frame,presentation);return true;}
            catch(InvalidOperationException){return false;}
        }
    }
}
