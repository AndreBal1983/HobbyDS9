using System;
using System.Threading;
using HobbyDS9.Application;
using HobbyDS9.Contracts;

namespace HobbyDS9.TacticalReviewClient
{
    public sealed class ColonisationExpansionCheckpointSaveWork
    {
        private readonly Func<Func<bool>,ReviewCheckpointResult> execute;
        private int started;
        private ReviewCheckpointResult? result;
        public ReviewFrame Source { get; }
        internal ColonisationExpansionCheckpointSaveWork(ReviewFrame source,Func<Func<bool>,ReviewCheckpointResult> execute){Source=source;this.execute=execute;}
        public ReviewCheckpointResult Execute(Func<bool> cancelled)
        {if(cancelled==null)throw new ArgumentNullException(nameof(cancelled));if(Interlocked.Exchange(ref started,1)!=0)throw new InvalidOperationException("Checkpoint work is one-use.");
         var done=execute(cancelled);Volatile.Write(ref result,done);return done;}
        internal ReviewCheckpointResult? Result=>Volatile.Read(ref result);
    }
    public sealed partial class PhysicalReviewHost
    {
        public bool IsCurrentColonisationExpansionCheckpointSource(ReviewFrame observed)
        {lock(gate){if(streamedInteraction!=null||colonisationInteraction!=null||colonisationExpansionPackage==null||observed.HostId!=id||observed.Generation!=generation)return false;
            var status=session.CaptureStatus();return status.Turn==observed.Status.Turn&&status.CampaignRevision==observed.Status.CampaignRevision;}}
        public ColonisationExpansionCheckpointSaveWork PrepareColonisationExpansionCheckpointSave(ReviewFrame observed,string slot,bool overwrite)
        {
            lock(gate)
            {
                if(!IsCurrentColonisationExpansionCheckpointSource(observed)||colonisationExpansionCodec is not IBackgroundColonisationExpansionCheckpointCodec
                    ||colonisationExpansionStore is not IBackgroundColonisationExpansionCheckpointStore)throw new InvalidOperationException("Background colonisation checkpoint unavailable.");
                var root=((ICampaignColonisationExpansionSession)session).ExportColonisationExpansionSave();
                var metadata=new ReviewSlotCheckpoint(slot,session.CaptureStatus());var workerCodec=colonisationExpansionCodec;var workerStore=colonisationExpansionStore;
                return new ColonisationExpansionCheckpointSaveWork(observed,cancelled=>{
                    if(cancelled()||!IsCurrentColonisationExpansionCheckpointSource(observed))return new ReviewCheckpointResult(ReviewCheckpointError.Busy);
                    CampaignColonisationExpansionSaveDto copy;
                    try{copy=workerCodec.CopyExpansionSave(root);LoadColonisationExpansionSource(copy);}
                    catch(Exception){return new ReviewCheckpointResult(ReviewCheckpointError.SerializationFailed);}
                    if(cancelled()||!IsCurrentColonisationExpansionCheckpointSource(observed))return new ReviewCheckpointResult(ReviewCheckpointError.Busy);
                    try{workerStore.Write(slot,copy,overwrite);return new ReviewCheckpointResult(ReviewCheckpointError.None,metadata);}
                    catch(ReviewCheckpointStoreException e){return new ReviewCheckpointResult(e.Error);}
                    catch(Exception){return new ReviewCheckpointResult(ReviewCheckpointError.WriteFailed);}
                });
            }
        }
    }
    public sealed partial class ReviewController
    {
        public bool ReceiveColonisationExpansionCheckpointSave(ColonisationExpansionCheckpointSaveWork work)
        {if(work==null||!ReferenceEquals(Frame,work.Source)||pending!=null||work.Result==null)return false;LastCheckpointError=work.Result.Error;return true;}
    }
}
