using System.Collections.Generic;
using HobbyDS9.Contracts;
using HobbyDS9.Simulation;
namespace HobbyDS9.Application
{
    public interface ICampaignColonisationRepeatSession:ICampaignColonisationOperations
    {
        CampaignColonisationRepeatSaveDto ExportRepeatSave();
        ICampaignColonisationRepeatSession ForkInteraction(int turn,long revision);
    }
    public sealed class CampaignColonisationRepeatApplication:CampaignIntegratedPlaytestApplication,ICampaignColonisationRepeatSession
    {
        private CampaignColonisationRepeatApplication(CampaignTacticalReviewSession backend):base(backend){}
        public static CampaignColonisationRepeatPackageDto CreateRepeatPackage(CampaignColonisationExpansionPackageDto original)=>CampaignTacticalReviewSession.CreateRepeatPackage(original);
        public static ICampaignColonisationRepeatSession StartRepeatGame(CampaignColonisationRepeatPackageDto package,NaturalGalaxySettingsDto settings)
            =>new CampaignColonisationRepeatApplication(CampaignTacticalReviewSession.StartRepeatGame(package,settings));
        public static ICampaignColonisationRepeatSession LoadRepeatSave(CampaignColonisationRepeatPackageDto package,CampaignColonisationRepeatSaveDto root)
            =>new CampaignColonisationRepeatApplication(CampaignTacticalReviewSession.LoadRepeatSave(package,root));
        public CampaignGeneratedMapDto CaptureGeneratedMap(string actor)=>session.CaptureGeneratedMap(actor);
        public CampaignStaticAstronomySnapshot CaptureKnownAstronomy(string actor)=>session.CaptureKnownAstronomy(actor);
        public CampaignColonisationRepeatSaveDto ExportRepeatSave()=>session.ExportRepeatSave();
        public ColonisationCampaignSnapshot CaptureColonisation(string actor)=>session.CaptureColonisation(actor);
        public IReadOnlyList<ColonisationPlanetObservationDto> CaptureColonisationPlanets(string actor)=>session.CaptureColonisationPlanets(actor);
        public CampaignActivityObservationDto CaptureActivities(string actor)=>session.CaptureActivities(actor);
        public FrontierCommissionSnapshot CaptureFrontierCommission(string actor)=>session.CaptureFrontierCommission(actor);
        public ColonisationEligibilitySnapshot EvaluateColonisation(ColonisationCommandDto command)=>session.EvaluateColonisation(command);
        public CampaignTacticalMutationResult SubmitColonisation(ColonisationCommandDto? command)=>session.SubmitColonisation(command);
        public CampaignActivityEligibilityDto EvaluateActivity(CampaignActivityCommandDto command)=>session.EvaluateActivity(command);
        public CampaignTacticalMutationResult SubmitActivity(CampaignActivityCommandDto? command)=>session.SubmitActivity(command);
        public ICampaignColonisationRepeatSession ForkInteraction(int turn,long revision)=>new CampaignColonisationRepeatApplication(session.ForkColonisationInteraction(turn,revision));
        public ICampaignColonisationOperations ForkOperations(int turn,long revision)=>ForkInteraction(turn,revision);
        public int RemainingPlayableOperations=>session.RemainingPlayableOperations;
        public CampaignPlayableFleetSaveDto ExportPlayableSave()=>session.ExportPlayableSave();
        public IReadOnlyList<CampaignNamedFleet> CaptureFleetOrganization(string actor)=>session.CaptureFleetOrganization(actor);
        public CampaignFleetOrganizationResult SubmitFleetOrganization(SubmitCampaignFleetOrganizationDto? command)=>session.SubmitFleetOrganization(command);
        public CampaignBattlePlayback CaptureBattlePlayback(string encounter,string actor)=>session.CaptureBattlePlayback(encounter,actor);
        public IReadOnlyList<CampaignBattlePlayback> GetBattleHistory(string actor)=>session.GetBattleHistory(actor);
    }
}
