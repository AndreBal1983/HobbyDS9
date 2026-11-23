using System.Collections.Generic;
using HobbyDS9.Contracts;
using HobbyDS9.Simulation;

namespace HobbyDS9.Application
{
    public sealed class CampaignStaticAstronomyApplication : CampaignIntegratedPlaytestApplication, ICampaignStaticAstronomySession
    {
        private CampaignStaticAstronomyApplication(CampaignTacticalReviewSession backend) : base(backend) { }
        public static ICampaignStaticAstronomySession StartGame(CampaignPlayableFleetPackageDto template, NaturalGalaxySettingsDto settings)
            => new CampaignStaticAstronomyApplication(CampaignTacticalReviewSession.StartStaticAstronomy(template, settings));
        public static ICampaignStaticAstronomySession LoadSave(CampaignPlayableFleetPackageDto template, CampaignGeneratedSaveDto save)
            => new CampaignStaticAstronomyApplication(CampaignTacticalReviewSession.LoadStaticAstronomy(template, save));
        public CampaignStaticAstronomySnapshot CaptureKnownAstronomy(string actor) => session.CaptureKnownAstronomy(actor);
        public CampaignGeneratedMapDto CaptureGeneratedMap(string actor) => session.CaptureGeneratedMap(actor);
        public CampaignGeneratedSaveDto ExportGeneratedSave() => session.ExportGeneratedSave();
        public int RemainingPlayableOperations => session.RemainingPlayableOperations;
        public CampaignPlayableFleetSaveDto ExportPlayableSave() => session.ExportPlayableSave();
        public IReadOnlyList<CampaignNamedFleet> CaptureFleetOrganization(string actor) => session.CaptureFleetOrganization(actor);
        public CampaignFleetOrganizationResult SubmitFleetOrganization(SubmitCampaignFleetOrganizationDto? command) => session.SubmitFleetOrganization(command);
        public CampaignBattlePlayback CaptureBattlePlayback(string encounter, string actor) => session.CaptureBattlePlayback(encounter, actor);
        public IReadOnlyList<CampaignBattlePlayback> GetBattleHistory(string actor) => session.GetBattleHistory(actor);
    }
}
