using HobbyDS9.Contracts;
using HobbyDS9.TacticalReviewClient;

namespace HobbyDS9.TacticalReviewStorage;

// Explicit B1 namespace and typed codec; never a nested physical-carrier adapter.
public sealed class DiskReviewCheckpointStore : IReviewCheckpointStore
{
    private readonly AtomicReviewFileStore files;
    private readonly IReviewFileCodec codec;
    public DiskReviewCheckpointStore(string reviewDirectory,IReviewFileCodec codec,Action<ReviewWriteStage>? writeProbe=null)
    { this.codec=codec; files=new AtomicReviewFileStore(reviewDirectory,"review-checkpoint-",writeProbe); }
    public void Write(string slot,CampaignTacticalReviewSaveDto privateSave,bool overwrite)
        => files.Write(slot,()=>codec.EncodePrivateSave(privateSave),overwrite);
    public CampaignTacticalReviewSaveDto Read(string slot)
    {
        var bytes=files.Read(slot);
        try { return codec.DecodePrivateSave(bytes); }
        catch(Exception) { throw new ReviewCheckpointStoreException(ReviewCheckpointError.InvalidCheckpoint); }
    }
}
