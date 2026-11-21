using System.Collections.Generic;
using HobbyDS9.Contracts;
using HobbyDS9.Simulation;

namespace HobbyDS9.Application
{
    public interface ICampaignGeneratedSession : ICampaignPlayableFleetSession
    {
        CampaignGeneratedMapDto CaptureGeneratedMap(string actor);
        CampaignGeneratedSaveDto ExportGeneratedSave();
    }
    public sealed class CampaignGeneratedApplication : CampaignIntegratedPlaytestApplication, ICampaignGeneratedSession
    {
        private CampaignGeneratedApplication(CampaignTacticalReviewSession backend) : base(backend) { }
        public static ICampaignGeneratedSession StartGame(CampaignPlayableFleetPackageDto template, NaturalGalaxySettingsDto settings)
            => new CampaignGeneratedApplication(CampaignTacticalReviewSession.StartGenerated(template, settings));
        public static ICampaignGeneratedSession LoadSave(CampaignPlayableFleetPackageDto template, CampaignGeneratedSaveDto save)
            => new CampaignGeneratedApplication(CampaignTacticalReviewSession.LoadGenerated(template, save));
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
