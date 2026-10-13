namespace HobbyDS9.TacticalReviewClient
{
    // Common SAFE presenter seam. Private save carriers remain in their separate typed hosts.
    internal interface ITacticalClientHost
    {
        ReviewFrame Read(string actor,string? encounter = null);
        ReviewReceipt Execute(ReviewWork work);
        bool IsCurrent(ReviewWork work);
        ReviewSaveHandle SaveCommitted();
        ReviewFrame LoadCommitted(ReviewSaveHandle handle,string actor,string? encounter);
        ReviewCheckpointResult SaveCommittedToSlot(string slot,bool overwrite = false);
        ReviewCheckpointResult LoadCommittedFromSlot(string slot,string actor,string? encounter = null);
        ClientHostReopenResult Reopen(string slot,string actor,string? encounter);
    }
    internal sealed class ClientHostReopenResult
    {
        internal ReviewCheckpointError Error { get; }
        internal bool Succeeded => Error == ReviewCheckpointError.None;
        internal ITacticalClientHost? Host { get; }
        internal ReviewFrame? Frame { get; }
        internal ClientHostReopenResult(ReviewCheckpointError error,ITacticalClientHost? host = null,ReviewFrame? frame = null)
        { Error=error; Host=host; Frame=frame; }
    }
    public sealed partial class ReviewHost : ITacticalClientHost
    {
        bool ITacticalClientHost.IsCurrent(ReviewWork work) => IsCurrent(work);
        ClientHostReopenResult ITacticalClientHost.Reopen(string slot,string actor,string? encounter)
        {
            var opened=ReopenFromSlot(slot,actor,encounter);
            return new ClientHostReopenResult(opened.Error,opened.Host,opened.Frame);
        }
    }
}
