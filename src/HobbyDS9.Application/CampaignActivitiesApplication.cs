using System.Collections.Generic;
using HobbyDS9.Contracts;
using HobbyDS9.Contracts.ShipDesign;
using HobbyDS9.Simulation;

namespace HobbyDS9.Application
{
    public interface ICampaignActivitiesSession:ICampaignPlayableFleetSession
    {
        CampaignGeneratedMapDto CaptureGeneratedMap(string actor);
        CampaignStaticAstronomySnapshot CaptureKnownAstronomy(string actor);
        CampaignActivitiesSaveDto ExportActivitiesSave();
        CampaignActivityObservationDto CaptureActivities(string actor);
        CampaignActivityEligibilityDto EvaluateActivity(CampaignActivityCommandDto command);
        CampaignTacticalMutationResult SubmitActivity(CampaignActivityCommandDto? command);
    }
    public sealed class CampaignActivitiesApplication:CampaignIntegratedPlaytestApplication,ICampaignActivitiesSession
    {
        private CampaignActivitiesApplication(CampaignTacticalReviewSession backend):base(backend){}
        public static CampaignActivitiesPackageDto CreatePackage(CampaignPlayableFleetPackageDto template,ShipDesignCatalogueDto catalogue,
            IReadOnlyList<ShipClassDraftDto> ready,ActivityTacticalSupportCatalogueDto support,CampaignActivityContentDto content,int recordBudget)
            =>CampaignTacticalReviewSession.CreateActivitiesPackage(template,catalogue,ready,support,content,recordBudget);
        public static ICampaignActivitiesSession StartGame(CampaignActivitiesPackageDto package,NaturalGalaxySettingsDto settings)
            =>new CampaignActivitiesApplication(CampaignTacticalReviewSession.StartActivities(package,settings));
        public static ICampaignActivitiesSession LoadSave(CampaignActivitiesPackageDto package,CampaignActivitiesSaveDto save)
            =>new CampaignActivitiesApplication(CampaignTacticalReviewSession.LoadActivities(package,save));
        public CampaignGeneratedMapDto CaptureGeneratedMap(string actor)=>session.CaptureGeneratedMap(actor);
        public CampaignStaticAstronomySnapshot CaptureKnownAstronomy(string actor)=>session.CaptureKnownAstronomy(actor);
        public CampaignActivitiesSaveDto ExportActivitiesSave()=>session.ExportActivitiesSave();
        public CampaignActivityObservationDto CaptureActivities(string actor)=>session.CaptureActivities(actor);
        public CampaignActivityEligibilityDto EvaluateActivity(CampaignActivityCommandDto command)=>session.EvaluateActivity(command);
        public CampaignTacticalMutationResult SubmitActivity(CampaignActivityCommandDto? command)=>session.SubmitActivity(command);
        public int RemainingPlayableOperations=>session.RemainingPlayableOperations;
        public CampaignPlayableFleetSaveDto ExportPlayableSave()=>session.ExportPlayableSave();
        public IReadOnlyList<CampaignNamedFleet> CaptureFleetOrganization(string actor)=>session.CaptureFleetOrganization(actor);
        public CampaignFleetOrganizationResult SubmitFleetOrganization(SubmitCampaignFleetOrganizationDto? command)=>session.SubmitFleetOrganization(command);
        public CampaignBattlePlayback CaptureBattlePlayback(string encounter,string actor)=>session.CaptureBattlePlayback(encounter,actor);
        public IReadOnlyList<CampaignBattlePlayback> GetBattleHistory(string actor)=>session.GetBattleHistory(actor);
    }
}
