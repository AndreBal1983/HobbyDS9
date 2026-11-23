using System.Collections.Generic;

namespace HobbyDS9.Contracts
{
    // Explicit peer carriers keep older serialized contracts and source editions closed.
    public sealed class CampaignEngineeringRefinementPackageDto:CampaignColonisationCyclePackageDto
    {
        public SystemRichnessPolicyDto? Generation {get;set;}=new SystemRichnessPolicyDto{GenerationVersion=CampaignAuthoredSolIdentity.Generation};
        public string RefinementPolicyId {get;set;}=EngineeringRefinementIdentity.Policy;
        public CampaignEngineeringRefinementPackageDto(){FormatVersion=4;ProfileId=EngineeringRefinementIdentity.PackageProfile;}
    }
    public sealed class CampaignEngineeringRefinementSaveDto:CampaignColonisationCycleSaveDto
    {
        public string GenerationVersion {get;set;}=CampaignAuthoredSolIdentity.Generation;
        public List<SystemAsteroidBeltDto>? SystemBelts {get;set;}=new List<SystemAsteroidBeltDto>();
        public EngineeringRefinementStateDto? Refinement {get;set;}=new EngineeringRefinementStateDto();
        public CampaignEngineeringRefinementSaveDto(){FormatVersion=EngineeringRefinementIdentity.Format;ProfileId=EngineeringRefinementIdentity.Profile;}
    }
}
