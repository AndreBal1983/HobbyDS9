using HobbyDS9.Contracts;

namespace HobbyDS9.Application
{
    // Implemented only by the explicit new-profile factory/loader, not historical v1 sessions.
    public interface ICampaignStaticAstronomySession : ICampaignGeneratedSession
    {
        CampaignStaticAstronomySnapshot CaptureKnownAstronomy(string actor);
    }
}
