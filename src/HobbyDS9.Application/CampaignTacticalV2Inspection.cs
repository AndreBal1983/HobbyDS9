using HobbyDS9.Contracts;

namespace HobbyDS9.Application
{
    // Optional additive capability: existing client/session interfaces do not change.
    public interface ICampaignTacticalV2Inspection
    {
        CampaignTacticalV2EligibilitySnapshot InspectCampaignTacticalV2(string? actorCivilizationId);
    }

    public sealed partial class CampaignApplication : ICampaignTacticalV2Inspection
    {
        public CampaignTacticalV2EligibilitySnapshot InspectCampaignTacticalV2(string? actorCivilizationId)
            => session.InspectCampaignTacticalV2(actorCivilizationId);
    }
}
