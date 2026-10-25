using HobbyDS9.Contracts;

namespace HobbyDS9.TacticalReviewClient
{
    public interface IColonisationReviewHostCodec:IPlayableFleetReviewHostCodec,IKnownGeneratedMapCodec
    {
        CampaignColonisationPackageDto CopyPackage(CampaignColonisationPackageDto package);
        CampaignColonisationSaveDto CopySave(CampaignColonisationSaveDto root);
    }
    public interface IColonisationCheckpointStore
    {
        void Write(string slot,CampaignColonisationSaveDto root,bool overwrite);
        CampaignColonisationSaveDto Read(string slot);
    }
    public interface IBackgroundColonisationCheckpointCodec:IColonisationReviewHostCodec { }
    public interface IBackgroundColonisationCheckpointStore:IColonisationCheckpointStore { }
}
