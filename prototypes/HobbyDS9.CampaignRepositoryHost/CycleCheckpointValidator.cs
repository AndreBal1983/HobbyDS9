using System;
using HobbyDS9.Persistence;
using HobbyDS9.TacticalReviewClient;

namespace HobbyDS9.CampaignRepositoryHost
{
    // Optional host composition only. This assembly never references SQLite,
    // Unity or native code, and the existing game/host DLLs do not depend on it.
    public sealed class CycleCheckpointValidator:ICheckpointValidator
    {
        private readonly CycleCampaignRepositoryAdapter adapter;
        public string EditionIdentity {get;}
        public CycleCheckpointValidator(CycleCampaignRepositoryAdapter adapter,string editionIdentity)
        {
            this.adapter=adapter??throw new ArgumentNullException(nameof(adapter));
            if(string.IsNullOrEmpty(editionIdentity)||editionIdentity.Length>CampaignRepositoryEdition.MaximumIdentityCharacters)
                throw new ArgumentException("Complete validated edition identity required.",nameof(editionIdentity));
            EditionIdentity=editionIdentity;
        }
        public CheckpointImage Validate(byte[] ownedPayload)
        {
            var result=adapter.Validate(ownedPayload,EditionIdentity);
            if(result.DefiniteCorruption)
                throw new InvalidCheckpointPayloadException("Decoded Cycle payload has impossible turn or revision counters for this edition.");
            if(!result.Succeeded)throw new RepositoryException(result.Error==CampaignRepositoryError.Incompatible?RepositoryError.IncompatibleEdition:RepositoryError.ValidationFailed);
            return Map(result.Image!);
        }
        public static CheckpointImage Map(CampaignRepositoryImage image)
        {
            if(image==null)throw new ArgumentNullException(nameof(image));
            return new CheckpointImage(image.ReadPayload(),image.Edition.Identity,image.Turn,image.Revision,image.Phase.ToString());
        }
        public static bool MetadataMatches(CheckpointImage stored,CampaignRepositoryImage validated)
            =>stored.EditionIdentity==validated.Edition.Identity&&stored.Turn==validated.Turn&&stored.Revision==validated.Revision
                &&stored.Phase==validated.Phase.ToString()&&stored.PayloadLength==validated.Length&&stored.PayloadHash==validated.PayloadHash;
    }
}
