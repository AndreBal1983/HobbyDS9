using System.Collections.Generic;
using HobbyDS9.Contracts;
using HobbyDS9.Contracts.ShipDesign;
using HobbyDS9.Simulation;

namespace HobbyDS9.Application
{
    public interface ICampaignPublishedConstructionSession : ICampaignPlayableFleetSession
    {
        CampaignGeneratedMapDto CaptureGeneratedMap(string actor);
        CampaignStaticAstronomySnapshot CaptureKnownAstronomy(string actor);
        CampaignPublishedConstructionSaveDto ExportPublishedConstructionSave();
        CampaignPublishedConstructionEligibility CaptureConstructionEligibility(string actor,string blueprint,int revision,string colony);
    }
    // Explicit portable entry only. No host/codec/store/Unity/default registration.
    public sealed class CampaignPublishedConstructionApplication : CampaignIntegratedPlaytestApplication,ICampaignPublishedConstructionSession
    {
        private CampaignPublishedConstructionApplication(CampaignTacticalReviewSession backend):base(backend) { }
        public static CampaignPublishedConstructionPackageDto CreatePackage(CampaignPlayableFleetPackageDto template,TacticalSupportCatalogueDto support,int recordBudget)
            =>CampaignTacticalReviewSession.CreatePublishedConstructionPackage(template,support,recordBudget);
        public static ICampaignPublishedConstructionSession StartGame(CampaignPublishedConstructionPackageDto package,NaturalGalaxySettingsDto settings)
            =>new CampaignPublishedConstructionApplication(CampaignTacticalReviewSession.StartPublishedConstruction(package,settings));
        public static ICampaignPublishedConstructionSession LoadSave(CampaignPublishedConstructionPackageDto package,CampaignPublishedConstructionSaveDto save)
            =>new CampaignPublishedConstructionApplication(CampaignTacticalReviewSession.LoadPublishedConstruction(package,save));
        public CampaignGeneratedMapDto CaptureGeneratedMap(string actor)=>session.CaptureGeneratedMap(actor);
        public CampaignStaticAstronomySnapshot CaptureKnownAstronomy(string actor)=>session.CaptureKnownAstronomy(actor);
        public CampaignPublishedConstructionSaveDto ExportPublishedConstructionSave()=>session.ExportPublishedConstructionSave();
        public CampaignPublishedConstructionEligibility CaptureConstructionEligibility(string actor,string blueprint,int revision,string colony)
            =>session.CapturePublishedConstructionEligibility(actor,blueprint,revision,colony);
        public int RemainingPlayableOperations=>session.RemainingPlayableOperations;
        public CampaignPlayableFleetSaveDto ExportPlayableSave()=>session.ExportPlayableSave();
        public IReadOnlyList<CampaignNamedFleet> CaptureFleetOrganization(string actor)=>session.CaptureFleetOrganization(actor);
        public CampaignFleetOrganizationResult SubmitFleetOrganization(SubmitCampaignFleetOrganizationDto? command)=>session.SubmitFleetOrganization(command);
        public CampaignBattlePlayback CaptureBattlePlayback(string encounter,string actor)=>session.CaptureBattlePlayback(encounter,actor);
        public IReadOnlyList<CampaignBattlePlayback> GetBattleHistory(string actor)=>session.GetBattleHistory(actor);
    }
}
