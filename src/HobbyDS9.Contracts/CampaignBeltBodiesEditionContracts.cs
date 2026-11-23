using System.Collections.Generic;
namespace HobbyDS9.Contracts
{
    public static class BeltBodiesEditionIdentity
    {
        public const int Format=18,PackageFormat=5;
        public const string Profile="generated-belt-bodies-v1",PackageProfile="generated-belt-bodies-package-v1";
        public const string Policy="belt-bodies-finite-local-extraction-v1",SaveDirectorySuffix="-belt-bodies-v1";
        public const int MaximumBodies=1000,MaximumObservations=4000,MaximumSurveys=4096,MaximumMines=2048,MaximumCommands=4096,MaximumLastShipments=1000;
    }
    // Peer carriers preserve the complete Format17 contract and the earlier DTO shapes.
    public sealed class CampaignBeltBodiesPackageDto:CampaignColonisationCyclePackageDto
    {
        public SystemRichnessPolicyDto? Generation {get;set;}=new SystemRichnessPolicyDto{GenerationVersion=CampaignAuthoredSolIdentity.Generation};
        public string RefinementPolicyId {get;set;}=EngineeringRefinementIdentity.Policy;
        public string BeltBodiesPolicyId {get;set;}=BeltBodiesEditionIdentity.Policy;
        public CampaignBeltBodiesPackageDto(){FormatVersion=BeltBodiesEditionIdentity.PackageFormat;ProfileId=BeltBodiesEditionIdentity.PackageProfile;}
    }
    public sealed class CampaignBeltBodiesSaveDto:CampaignColonisationCycleSaveDto
    {
        public string GenerationVersion {get;set;}=CampaignAuthoredSolIdentity.Generation;
        public List<SystemAsteroidBeltDto>? SystemBelts {get;set;}=new List<SystemAsteroidBeltDto>();
        public EngineeringRefinementStateDto? Refinement {get;set;}=new EngineeringRefinementStateDto();
        public BeltBodyStateDto? BeltBodies {get;set;}=new BeltBodyStateDto();
        public CampaignBeltBodiesSaveDto(){FormatVersion=BeltBodiesEditionIdentity.Format;ProfileId=BeltBodiesEditionIdentity.Profile;}
    }
}
