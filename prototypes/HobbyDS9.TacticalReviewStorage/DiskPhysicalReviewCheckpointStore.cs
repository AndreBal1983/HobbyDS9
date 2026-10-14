using HobbyDS9.Contracts;
using HobbyDS9.TacticalReviewClient;

namespace HobbyDS9.TacticalReviewStorage;

// Format 2 uses a disjoint filename namespace, sharing only the byte/file protocol.
public sealed class DiskPhysicalReviewCheckpointStore : IPhysicalReviewCheckpointStore
{
    private readonly AtomicReviewFileStore files;
    private readonly IPhysicalReviewFileCodec codec;
    public DiskPhysicalReviewCheckpointStore(string reviewDirectory,IPhysicalReviewFileCodec codec,Action<ReviewWriteStage>? writeProbe=null)
    { this.codec=codec; files=new AtomicReviewFileStore(reviewDirectory,"physical-review-checkpoint-",writeProbe); }
    public void Write(string slot,CampaignTacticalTravelSaveDto privateRoot,bool overwrite)
        => files.Write(slot,()=>codec.EncodePrivateSave(privateRoot),overwrite);
    public CampaignTacticalTravelSaveDto Read(string slot)
    {
        var bytes=files.Read(slot);
        try { return codec.DecodePrivateSave(bytes); }
        catch(Exception) { throw new ReviewCheckpointStoreException(ReviewCheckpointError.InvalidCheckpoint); }
    }
}
