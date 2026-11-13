using System.Collections.Generic;
using HobbyDS9.Contracts;
using HobbyDS9.Simulation;

namespace HobbyDS9.Application
{
    public interface ICampaignPlayableFleetSession : ICampaignIntegratedPlaytestSession
    {
        int RemainingPlayableOperations { get; }
        CampaignPlayableFleetSaveDto ExportPlayableSave();
        IReadOnlyList<CampaignNamedFleet> CaptureFleetOrganization(string actor);
        CampaignFleetOrganizationResult SubmitFleetOrganization(SubmitCampaignFleetOrganizationDto? command);
        CampaignBattlePlayback CaptureBattlePlayback(string encounter, string actor);
        IReadOnlyList<CampaignBattlePlayback> GetBattleHistory(string actor);
    }
    public sealed class CampaignPlayableFleetApplication : CampaignIntegratedPlaytestApplication, ICampaignPlayableFleetSession
    {
        private CampaignPlayableFleetApplication(CampaignTacticalReviewSession session) : base(session) { }
        public static ICampaignPlayableFleetSession StartPackage(CampaignPlayableFleetPackageDto package)
            => new CampaignPlayableFleetApplication(CampaignTacticalReviewSession.StartPlayable(package));
        public static ICampaignPlayableFleetSession LoadSave(CampaignPlayableFleetPackageDto package, CampaignPlayableFleetSaveDto save)
            => new CampaignPlayableFleetApplication(CampaignTacticalReviewSession.LoadPlayable(package, save));
        public int RemainingPlayableOperations => session.RemainingPlayableOperations;
        public CampaignPlayableFleetSaveDto ExportPlayableSave() => session.ExportPlayableSave();
        public IReadOnlyList<CampaignNamedFleet> CaptureFleetOrganization(string actor) => session.CaptureFleetOrganization(actor);
        public CampaignFleetOrganizationResult SubmitFleetOrganization(SubmitCampaignFleetOrganizationDto? command) => session.SubmitFleetOrganization(command);
        public CampaignBattlePlayback CaptureBattlePlayback(string encounter, string actor) => session.CaptureBattlePlayback(encounter, actor);
        public IReadOnlyList<CampaignBattlePlayback> GetBattleHistory(string actor) => session.GetBattleHistory(actor);
    }
}
