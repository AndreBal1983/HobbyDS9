using HobbyDS9.Contracts;
namespace HobbyDS9.Application
{
    public interface ICampaignEngineeringRefinementSession
    {
        EngineeringRefinementSnapshot? CaptureEngineeringRefinement(string actor);
        EngineeringRefinementPreview PreviewEngineeringRefinement(EngineeringRefinementCommandDto command);
        EngineeringRefinementResult SubmitEngineeringRefinement(EngineeringRefinementCommandDto command);
    }
    public partial class CampaignIntegratedPlaytestApplication : ICampaignEngineeringRefinementSession
    {
        public EngineeringRefinementSnapshot? CaptureEngineeringRefinement(string actor)=>session.CaptureEngineeringRefinement(actor);
        public EngineeringRefinementPreview PreviewEngineeringRefinement(EngineeringRefinementCommandDto command)=>session.PreviewEngineeringRefinement(command);
        public EngineeringRefinementResult SubmitEngineeringRefinement(EngineeringRefinementCommandDto command)=>session.SubmitEngineeringRefinement(command);
    }
}
