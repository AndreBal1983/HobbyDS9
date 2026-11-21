using System.Collections.Generic;
using HobbyDS9.Contracts;
using HobbyDS9.Simulation;

namespace HobbyDS9.Application
{
    public interface ICampaignStreamedActivitiesSession:ICampaignPlayableFleetSession
    {
        CampaignGeneratedMapDto CaptureGeneratedMap(string actor);
        CampaignStaticAstronomySnapshot CaptureKnownAstronomy(string actor);
        CampaignStreamedActivitiesSaveDto ExportStreamedActivitiesSave();
        CampaignStreamedActivityObservationDto CaptureStreamedActivities(string actor);
        FrontierCommissionSnapshot CaptureFrontierCommission(string actor);
        CampaignActivityEligibilityDto EvaluateActivity(CampaignActivityCommandDto command);
        CampaignTacticalMutationResult SubmitActivity(CampaignActivityCommandDto? command);
        ICampaignStreamedActivitiesSession ForkInteraction(int turn,long revision);
    }
    public sealed class CampaignStreamedActivitiesApplication:CampaignIntegratedPlaytestApplication,ICampaignStreamedActivitiesSession
    {
        private CampaignStreamedActivitiesApplication(CampaignTacticalReviewSession backend):base(backend){}
        public static CampaignStreamedActivitiesPackageDto CreatePackage(CampaignActivitiesPackageDto source)=>CampaignTacticalReviewSession.CreateStreamedActivitiesPackage(source);
        public static ICampaignStreamedActivitiesSession StartGame(CampaignStreamedActivitiesPackageDto package,NaturalGalaxySettingsDto settings)
            =>new CampaignStreamedActivitiesApplication(CampaignTacticalReviewSession.StartStreamedActivities(package,settings));
        public static ICampaignStreamedActivitiesSession LoadSave(CampaignStreamedActivitiesPackageDto package,CampaignStreamedActivitiesSaveDto root)
            =>new CampaignStreamedActivitiesApplication(CampaignTacticalReviewSession.LoadStreamedActivities(package,root));
        public CampaignGeneratedMapDto CaptureGeneratedMap(string actor)=>session.CaptureGeneratedMap(actor);
        public CampaignStaticAstronomySnapshot CaptureKnownAstronomy(string actor)=>session.CaptureKnownAstronomy(actor);
        public CampaignStreamedActivitiesSaveDto ExportStreamedActivitiesSave()=>session.ExportStreamedActivitiesSave();
        public CampaignStreamedActivityObservationDto CaptureStreamedActivities(string actor)=>session.CaptureStreamedActivities(actor);
        public FrontierCommissionSnapshot CaptureFrontierCommission(string actor)=>session.CaptureFrontierCommission(actor);
        public CampaignActivityEligibilityDto EvaluateActivity(CampaignActivityCommandDto command)=>session.EvaluateActivity(command);
        public CampaignTacticalMutationResult SubmitActivity(CampaignActivityCommandDto? command)=>session.SubmitActivity(command);
        public ICampaignStreamedActivitiesSession ForkInteraction(int turn,long revision)
            =>new CampaignStreamedActivitiesApplication(session.ForkStreamedInteraction(turn,revision));
        public int RemainingPlayableOperations=>session.RemainingPlayableOperations;
        public CampaignPlayableFleetSaveDto ExportPlayableSave()=>session.ExportPlayableSave();
        public IReadOnlyList<CampaignNamedFleet> CaptureFleetOrganization(string actor)=>session.CaptureFleetOrganization(actor);
        public CampaignFleetOrganizationResult SubmitFleetOrganization(SubmitCampaignFleetOrganizationDto? command)=>session.SubmitFleetOrganization(command);
        public CampaignBattlePlayback CaptureBattlePlayback(string encounter,string actor)=>session.CaptureBattlePlayback(encounter,actor);
        public IReadOnlyList<CampaignBattlePlayback> GetBattleHistory(string actor)=>session.GetBattleHistory(actor);
    }
}
