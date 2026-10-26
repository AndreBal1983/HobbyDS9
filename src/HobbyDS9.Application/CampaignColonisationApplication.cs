using System.Collections.Generic;
using HobbyDS9.Contracts;
using HobbyDS9.Simulation;

namespace HobbyDS9.Application
{
    public interface ICampaignColonisationSession:ICampaignPlayableFleetSession
    {
        CampaignGeneratedMapDto CaptureGeneratedMap(string actor);
        CampaignStaticAstronomySnapshot CaptureKnownAstronomy(string actor);
        CampaignColonisationSaveDto ExportColonisationSave();
        ColonisationCampaignSnapshot CaptureColonisation(string actor);
        IReadOnlyList<ColonisationPlanetObservationDto> CaptureColonisationPlanets(string actor);
        CampaignActivityObservationDto CaptureActivities(string actor);
        FrontierCommissionSnapshot CaptureFrontierCommission(string actor);
        ColonisationEligibilitySnapshot EvaluateColonisation(ColonisationCommandDto command);
        CampaignTacticalMutationResult SubmitColonisation(ColonisationCommandDto? command);
        CampaignActivityEligibilityDto EvaluateActivity(CampaignActivityCommandDto command);
        CampaignTacticalMutationResult SubmitActivity(CampaignActivityCommandDto? command);
        ICampaignColonisationSession ForkInteraction(int turn,long revision);
    }
    public sealed class CampaignColonisationApplication:CampaignIntegratedPlaytestApplication,ICampaignColonisationSession
    {
        private CampaignColonisationApplication(CampaignTacticalReviewSession backend):base(backend){}
        public static CampaignColonisationPackageDto CreatePackage(CampaignActivitiesPackageDto activities,HobbyDS9.Contracts.ShipDesign.ShipDesignCatalogueDto catalogue,
            IReadOnlyList<HobbyDS9.Contracts.ShipDesign.ShipClassDraftDto> ready,HobbyDS9.Contracts.ShipDesign.ColonisationSupportCatalogueDto support,ColonisationPolicyDto policy)
            =>CampaignTacticalReviewSession.CreateColonisationPackage(activities,catalogue,ready,support,policy);
        public static ICampaignColonisationSession StartGame(CampaignColonisationPackageDto package,NaturalGalaxySettingsDto settings)
            =>new CampaignColonisationApplication(CampaignTacticalReviewSession.StartColonisation(package,settings));
        public static ICampaignColonisationSession LoadSave(CampaignColonisationPackageDto package,CampaignColonisationSaveDto root)
            =>new CampaignColonisationApplication(CampaignTacticalReviewSession.LoadColonisation(package,root));
        public static CampaignColonisationRosterPackageDto CreateRosterPackage(CampaignColonisationPackageDto baseV1)
            =>CampaignTacticalReviewSession.CreateColonisationRosterPackage(baseV1);
        public static ICampaignColonisationSession StartRosterGame(CampaignColonisationRosterPackageDto package,NaturalGalaxySettingsDto settings)
            =>new CampaignColonisationApplication(CampaignTacticalReviewSession.StartColonisationRoster(package,settings));
        public static ICampaignColonisationSession LoadRosterSave(CampaignColonisationRosterPackageDto package,CampaignColonisationSaveDto root)
            =>new CampaignColonisationApplication(CampaignTacticalReviewSession.LoadColonisationRoster(package,root));
        public CampaignGeneratedMapDto CaptureGeneratedMap(string actor)=>session.CaptureGeneratedMap(actor);
        public CampaignStaticAstronomySnapshot CaptureKnownAstronomy(string actor)=>session.CaptureKnownAstronomy(actor);
        public CampaignColonisationSaveDto ExportColonisationSave()=>session.ExportColonisationSave();
        public ColonisationCampaignSnapshot CaptureColonisation(string actor)=>session.CaptureColonisation(actor);
        public IReadOnlyList<ColonisationPlanetObservationDto> CaptureColonisationPlanets(string actor)=>session.CaptureColonisationPlanets(actor);
        public CampaignActivityObservationDto CaptureActivities(string actor)=>session.CaptureActivities(actor);
        public FrontierCommissionSnapshot CaptureFrontierCommission(string actor)=>session.CaptureFrontierCommission(actor);
        public ColonisationEligibilitySnapshot EvaluateColonisation(ColonisationCommandDto command)=>session.EvaluateColonisation(command);
        public CampaignTacticalMutationResult SubmitColonisation(ColonisationCommandDto? command)=>session.SubmitColonisation(command);
        public CampaignActivityEligibilityDto EvaluateActivity(CampaignActivityCommandDto command)=>session.EvaluateActivity(command);
        public CampaignTacticalMutationResult SubmitActivity(CampaignActivityCommandDto? command)=>session.SubmitActivity(command);
        public ICampaignColonisationSession ForkInteraction(int turn,long revision)=>new CampaignColonisationApplication(session.ForkColonisationInteraction(turn,revision));
        public int RemainingPlayableOperations=>session.RemainingPlayableOperations;
        public CampaignPlayableFleetSaveDto ExportPlayableSave()=>session.ExportPlayableSave();
        public IReadOnlyList<CampaignNamedFleet> CaptureFleetOrganization(string actor)=>session.CaptureFleetOrganization(actor);
        public CampaignFleetOrganizationResult SubmitFleetOrganization(SubmitCampaignFleetOrganizationDto? command)=>session.SubmitFleetOrganization(command);
        public CampaignBattlePlayback CaptureBattlePlayback(string encounter,string actor)=>session.CaptureBattlePlayback(encounter,actor);
        public IReadOnlyList<CampaignBattlePlayback> GetBattleHistory(string actor)=>session.GetBattleHistory(actor);
    }
}
