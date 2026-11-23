using System;
using System.Threading;
using HobbyDS9.Application;
using HobbyDS9.Contracts;

namespace HobbyDS9.TacticalReviewClient
{
    public sealed class ColonisationCycleCheckpointSaveWork
    {
        private readonly Func<Func<bool>,ReviewCheckpointResult> execute;
        private int started;
        private ReviewCheckpointResult? result;
        public ReviewFrame Source { get; }
        internal ColonisationCycleCheckpointSaveWork(ReviewFrame source,Func<Func<bool>,ReviewCheckpointResult> execute){Source=source;this.execute=execute;}
        public ReviewCheckpointResult Execute(Func<bool> cancelled)
        {if(cancelled==null)throw new ArgumentNullException(nameof(cancelled));if(Interlocked.Exchange(ref started,1)!=0)throw new InvalidOperationException("Checkpoint work is one-use.");
         var done=execute(cancelled);Volatile.Write(ref result,done);return done;}
        internal ReviewCheckpointResult? Result=>Volatile.Read(ref result);
    }
    public sealed partial class PhysicalReviewHost
    {
        public bool IsCurrentColonisationCycleCheckpointSource(ReviewFrame observed)
        {lock(gate){if(streamedInteraction!=null||colonisationInteraction!=null||colonisationCyclePackage==null||observed.HostId!=id||observed.Generation!=generation)return false;
            var status=session.CaptureStatus();return status.Turn==observed.Status.Turn&&status.CampaignRevision==observed.Status.CampaignRevision;}}
        public ColonisationCycleCheckpointSaveWork PrepareColonisationCycleCheckpointSave(ReviewFrame observed,string slot,bool overwrite)
        {
            lock(gate)
            {
                if(!IsCurrentColonisationCycleCheckpointSource(observed)||colonisationCycleCodec is not IBackgroundColonisationCycleCheckpointCodec
                    ||colonisationCycleStore is not IBackgroundColonisationCycleCheckpointStore)throw new InvalidOperationException("Background colonisation checkpoint unavailable.");
                var root=((ICampaignColonisationCycleSession)session).ExportCycleSave();
                var metadata=new ReviewSlotCheckpoint(slot,session.CaptureStatus());var workerCodec=colonisationCycleCodec;var workerStore=colonisationCycleStore;
                return new ColonisationCycleCheckpointSaveWork(observed,cancelled=>{
                    if(cancelled()||!IsCurrentColonisationCycleCheckpointSource(observed))return new ReviewCheckpointResult(ReviewCheckpointError.Busy);
                    CampaignColonisationCycleSaveDto copy;
                    try{copy=workerCodec.CopyCycleSave(root);LoadColonisationCycleSource(copy);}
                    catch(Exception){return new ReviewCheckpointResult(ReviewCheckpointError.SerializationFailed);}
                    if(cancelled()||!IsCurrentColonisationCycleCheckpointSource(observed))return new ReviewCheckpointResult(ReviewCheckpointError.Busy);
                    try{workerStore.Write(slot,copy,overwrite);return new ReviewCheckpointResult(ReviewCheckpointError.None,metadata);}
                    catch(ReviewCheckpointStoreException e){return new ReviewCheckpointResult(e.Error);}
                    catch(Exception){return new ReviewCheckpointResult(ReviewCheckpointError.WriteFailed);}
                });
            }
        }
    }
    public sealed partial class ReviewController
    {
        public bool ReceiveColonisationCycleCheckpointSave(ColonisationCycleCheckpointSaveWork work)
        {if(work==null||!ReferenceEquals(Frame,work.Source)||pending!=null||work.Result==null)return false;LastCheckpointError=work.Result.Error;return true;}
    }
}
