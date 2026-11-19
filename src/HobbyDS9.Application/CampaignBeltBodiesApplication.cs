using HobbyDS9.Contracts;
namespace HobbyDS9.Application
{
    public interface ICampaignBeltBodiesSession
    {
        BeltBodiesSnapshot? CaptureBeltBodies(string actor);
        BeltBodyCommandPreview PreviewBeltCommand(BeltBodyCommandDto command);
        BeltBodyCommandResult SubmitBeltCommand(BeltBodyCommandDto command);
    }
    public partial class CampaignIntegratedPlaytestApplication:ICampaignBeltBodiesSession
    {
        public BeltBodiesSnapshot? CaptureBeltBodies(string actor)=>session.CaptureBeltBodies(actor);
        public BeltBodyCommandPreview PreviewBeltCommand(BeltBodyCommandDto command)=>session.PreviewBeltCommand(command);
        public BeltBodyCommandResult SubmitBeltCommand(BeltBodyCommandDto command)=>session.SubmitBeltCommand(command);
    }
}
