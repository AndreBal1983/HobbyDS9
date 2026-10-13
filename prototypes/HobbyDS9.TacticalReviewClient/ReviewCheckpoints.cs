using System;
using System.IO;
using HobbyDS9.Application;
using HobbyDS9.Contracts;

namespace HobbyDS9.TacticalReviewClient
{
    // This is a TRUSTED HOST seam. Private payloads do not belong in a player view.
    public interface IReviewCheckpointStore
    {
        void Write(string slot, CampaignTacticalReviewSaveDto privateSave, bool overwrite);
        CampaignTacticalReviewSaveDto Read(string slot);
    }
    public enum ReviewCheckpointError
    {
        None, NoStore, InvalidDirectory, InvalidSlot, SlotOccupied, NotFound,
        SerializationFailed, WriteFailed, ReadFailed, InvalidCheckpoint, Busy, DirtyDraft
    }
    public sealed class ReviewCheckpointStoreException : IOException
    {
        public ReviewCheckpointError Error { get; }
        public ReviewCheckpointStoreException(ReviewCheckpointError error)
            : base("Review checkpoint storage operation failed.")
        {
            if(error == ReviewCheckpointError.None || !Enum.IsDefined(typeof(ReviewCheckpointError),error))
                throw new ArgumentOutOfRangeException(nameof(error));
            Error=error;
        }
    }
    public sealed class ReviewSlotCheckpoint
    {
        public string Slot { get; }
        public int Turn { get; }
        public long CampaignRevision { get; }
        internal ReviewSlotCheckpoint(string slot, CampaignTacticalReviewStatus status)
        { Slot=slot; Turn=status.Turn; CampaignRevision=status.CampaignRevision; }
        public override string ToString() => "Private committed review slot checkpoint";
    }
    public sealed class ReviewCheckpointResult
    {
        public ReviewCheckpointError Error { get; }
        public bool Succeeded => Error == ReviewCheckpointError.None;
        public ReviewSlotCheckpoint? Checkpoint { get; }
        public ReviewFrame? Frame { get; }
        internal ReviewCheckpointResult(ReviewCheckpointError error, ReviewSlotCheckpoint? checkpoint = null, ReviewFrame? frame = null)
        { Error=error; Checkpoint=checkpoint; Frame=frame; }
    }
    public sealed class ReviewHostOpenResult
    {
        public ReviewCheckpointError Error { get; }
        public bool Succeeded => Error == ReviewCheckpointError.None;
        public ReviewHost? Host { get; }
        public ReviewFrame? Frame { get; }
        internal ReviewHostOpenResult(ReviewCheckpointError error, ReviewHost? host = null, ReviewFrame? frame = null)
        { Error=error; Host=host; Frame=frame; }
    }
    public sealed partial class ReviewHost
    {
        public ReviewCheckpointResult SaveCommittedToSlot(string slot, bool overwrite = false)
        {
            lock(gate)
            {
                if(checkpointStore == null) return new ReviewCheckpointResult(ReviewCheckpointError.NoStore);
                CampaignTacticalReviewSaveDto save;
                try { save=codec.CopySave(session.ExportSave()); }
                catch(Exception) { return new ReviewCheckpointResult(ReviewCheckpointError.SerializationFailed); }
                var metadata=new ReviewSlotCheckpoint(slot,session.CaptureStatus());
                try { checkpointStore.Write(slot,save,overwrite); }
                catch(ReviewCheckpointStoreException e) { return new ReviewCheckpointResult(e.Error); }
                catch(Exception) { return new ReviewCheckpointResult(ReviewCheckpointError.WriteFailed); }
                return new ReviewCheckpointResult(ReviewCheckpointError.None,metadata);
            }
        }
        public ReviewCheckpointResult LoadCommittedFromSlot(string slot, string actor, string? encounter = null)
        {
            lock(gate)
            {
                if(checkpointStore == null) return new ReviewCheckpointResult(ReviewCheckpointError.NoStore);
                CampaignTacticalReviewSaveDto save;
                try { save=checkpointStore.Read(slot); }
                catch(ReviewCheckpointStoreException e) { return new ReviewCheckpointResult(e.Error); }
                catch(Exception) { return new ReviewCheckpointResult(ReviewCheckpointError.ReadFailed); }
                try
                {
                    var candidate=CampaignTacticalReviewApplication.LoadReviewSave(package,codec.CopySave(save));
                    var epoch=checked(generation+1);
                    var frame=Capture(candidate,actor,encounter,epoch);
                    var metadata=new ReviewSlotCheckpoint(slot,frame.Status);
                    // This is the sole live replacement point; all I/O and validation finished.
                    session=candidate; generation=epoch;
                    return new ReviewCheckpointResult(ReviewCheckpointError.None,metadata,frame);
                }
                catch(Exception) { return new ReviewCheckpointResult(ReviewCheckpointError.InvalidCheckpoint); }
            }
        }
        public static ReviewHostOpenResult OpenFromSlot(CampaignTacticalReviewPackageDto package, IReviewHostCodec codec,
            IReviewCheckpointStore store, string slot, string actor, string? encounter = null)
        {
            try
            {
                var candidate=new ReviewHost(package,codec,store);
                var loaded=candidate.LoadCommittedFromSlot(slot,actor,encounter);
                return loaded.Succeeded ? new ReviewHostOpenResult(ReviewCheckpointError.None,candidate,loaded.Frame)
                    : new ReviewHostOpenResult(loaded.Error);
            }
            catch(Exception) { return new ReviewHostOpenResult(ReviewCheckpointError.InvalidCheckpoint); }
        }
        internal ReviewHostOpenResult ReopenFromSlot(string slot, string actor, string? encounter)
        {
            lock(gate)
            {
                if(checkpointStore == null) return new ReviewHostOpenResult(ReviewCheckpointError.NoStore);
                return OpenFromSlot(package,codec,checkpointStore,slot,actor,encounter);
            }
        }
    }
}
