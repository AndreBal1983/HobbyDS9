using System.Collections.Generic;
using HobbyDS9.Contracts;
using HobbyDS9.Simulation;
namespace HobbyDS9.Application
{
    public interface ICampaignColonisationExpansionSession:ICampaignColonisationOperations
    {
        CampaignColonisationExpansionSaveDto ExportColonisationExpansionSave();
        ICampaignColonisationExpansionSession ForkInteraction(int turn,long revision);
    }
    public sealed class CampaignColonisationExpansionApplication:CampaignIntegratedPlaytestApplication,ICampaignColonisationExpansionSession
    {
        private CampaignColonisationExpansionApplication(CampaignTacticalReviewSession backend):base(backend){}
        public static CampaignColonisationExpansionPackageDto CreatePackage(CampaignColonisationRosterPackageDto roster)=>CampaignTacticalReviewSession.CreateColonisationExpansionPackage(roster);
        public static ICampaignColonisationExpansionSession StartGame(CampaignColonisationExpansionPackageDto package,NaturalGalaxySettingsDto settings)
            =>new CampaignColonisationExpansionApplication(CampaignTacticalReviewSession.StartColonisationExpansion(package,settings));
        public static ICampaignColonisationExpansionSession LoadSave(CampaignColonisationExpansionPackageDto package,CampaignColonisationExpansionSaveDto root)
            =>new CampaignColonisationExpansionApplication(CampaignTacticalReviewSession.LoadColonisationExpansion(package,root));
        public CampaignGeneratedMapDto CaptureGeneratedMap(string actor)=>session.CaptureGeneratedMap(actor);
        public CampaignStaticAstronomySnapshot CaptureKnownAstronomy(string actor)=>session.CaptureKnownAstronomy(actor);
        public CampaignColonisationExpansionSaveDto ExportColonisationExpansionSave()=>session.ExportColonisationExpansionSave();
        public ColonisationCampaignSnapshot CaptureColonisation(string actor)=>session.CaptureColonisation(actor);
        public IReadOnlyList<ColonisationPlanetObservationDto> CaptureColonisationPlanets(string actor)=>session.CaptureColonisationPlanets(actor);
        public CampaignActivityObservationDto CaptureActivities(string actor)=>session.CaptureActivities(actor);
        public FrontierCommissionSnapshot CaptureFrontierCommission(string actor)=>session.CaptureFrontierCommission(actor);
        public ColonisationEligibilitySnapshot EvaluateColonisation(ColonisationCommandDto command)=>session.EvaluateColonisation(command);
        public CampaignTacticalMutationResult SubmitColonisation(ColonisationCommandDto? command)=>session.SubmitColonisation(command);
        public CampaignActivityEligibilityDto EvaluateActivity(CampaignActivityCommandDto command)=>session.EvaluateActivity(command);
        public CampaignTacticalMutationResult SubmitActivity(CampaignActivityCommandDto? command)=>session.SubmitActivity(command);
        public ICampaignColonisationExpansionSession ForkInteraction(int turn,long revision)=>new CampaignColonisationExpansionApplication(session.ForkColonisationInteraction(turn,revision));
        public ICampaignColonisationOperations ForkOperations(int turn,long revision)=>ForkInteraction(turn,revision);
        public int RemainingPlayableOperations=>session.RemainingPlayableOperations;
        public CampaignPlayableFleetSaveDto ExportPlayableSave()=>session.ExportPlayableSave();
        public IReadOnlyList<CampaignNamedFleet> CaptureFleetOrganization(string actor)=>session.CaptureFleetOrganization(actor);
        public CampaignFleetOrganizationResult SubmitFleetOrganization(SubmitCampaignFleetOrganizationDto? command)=>session.SubmitFleetOrganization(command);
        public CampaignBattlePlayback CaptureBattlePlayback(string encounter,string actor)=>session.CaptureBattlePlayback(encounter,actor);
        public IReadOnlyList<CampaignBattlePlayback> GetBattleHistory(string actor)=>session.GetBattleHistory(actor);
    }
}
