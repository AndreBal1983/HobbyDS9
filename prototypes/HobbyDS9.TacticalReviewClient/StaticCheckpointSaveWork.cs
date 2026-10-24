using System;
using System.Threading;
using HobbyDS9.Application;
using HobbyDS9.Contracts;

namespace HobbyDS9.TacticalReviewClient
{
    // Trusted, one-use private checkpoint work. Neither the root nor template is a player view.
    public sealed class StaticCheckpointSaveWork
    {
        private readonly Func<Func<bool>,ReviewCheckpointResult> execute;
        private int started;
        private ReviewCheckpointResult? result;
        public ReviewFrame Source { get; }
        internal StaticCheckpointSaveWork(ReviewFrame source,Func<Func<bool>,ReviewCheckpointResult> execute)
        {Source=source;this.execute=execute;}
        public ReviewCheckpointResult Execute(Func<bool> cancelled)
        {
            if(cancelled==null)throw new ArgumentNullException(nameof(cancelled));
            if(Interlocked.Exchange(ref started,1)!=0)throw new InvalidOperationException("Checkpoint work is one-use.");
            var completed=execute(cancelled);Volatile.Write(ref result,completed);return completed;
        }
        internal ReviewCheckpointResult? Result=>Volatile.Read(ref result);
    }

    public sealed partial class PhysicalReviewHost
    {
        public bool IsCurrentStaticCheckpointSource(ReviewFrame observed)
        {
            lock(gate)
            {
                if(!staticAstronomy||observed.HostId!=id||observed.Generation!=generation)return false;
                var status=session.CaptureStatus();
                return observed.Status.Turn==status.Turn&&observed.Status.CampaignRevision==status.CampaignRevision;
            }
        }
        public StaticCheckpointSaveWork PrepareStaticCheckpointSave(ReviewFrame observed,string slot,bool overwrite)
        {
            lock(gate)
            {
                if(!IsCurrentStaticCheckpointSource(observed))throw new InvalidOperationException("Checkpoint observation changed.");
                // The portable export is detached. Only this capture holds the live host gate;
                // JSON copies, authoritative replay and file IO below never hold it.
                var root=((ICampaignGeneratedSession)session).ExportGeneratedSave();
                var metadata=new ReviewSlotCheckpoint(slot,session.CaptureStatus());
                var template=generatedTemplate!;var workerCodec=(IStaticAstronomyReviewHostCodec)generatedCodec!;
                var workerStore=generatedStore;
                return new StaticCheckpointSaveWork(observed,cancelled=>{
                    if(cancelled()||!IsCurrentStaticCheckpointSource(observed))return new ReviewCheckpointResult(ReviewCheckpointError.Busy);
                    if(workerStore==null)return new ReviewCheckpointResult(ReviewCheckpointError.NoStore);
                    CampaignGeneratedSaveDto save;
                    try
                    {
                        save=workerCodec.CopyStaticAstronomySave(root);
                        if(workerCodec is IStaticAstronomyOwnedExportValidator validator)validator.ValidateOwnedStaticAstronomyExport(template,save);
                        else CampaignStaticAstronomyApplication.LoadSave(template,workerCodec.CopyStaticAstronomySave(save));
                    }
                    catch(Exception){return new ReviewCheckpointResult(ReviewCheckpointError.SerializationFailed);}
                    // A save is the accepted point-in-time snapshot, not the local draft. Once
                    // atomic writing starts it may commit despite later UI teardown/cancellation.
                    if(cancelled()||!IsCurrentStaticCheckpointSource(observed))return new ReviewCheckpointResult(ReviewCheckpointError.Busy);
                    try{workerStore.Write(slot,save,overwrite);}
                    catch(ReviewCheckpointStoreException e){return new ReviewCheckpointResult(e.Error);}
                    catch(Exception){return new ReviewCheckpointResult(ReviewCheckpointError.WriteFailed);}
                    return new ReviewCheckpointResult(ReviewCheckpointError.None,metadata);
                });
            }
        }
    }

    public sealed partial class ReviewController
    {
        public bool ReceiveStaticCheckpointSave(StaticCheckpointSaveWork work)
        {
            if(work==null||!ReferenceEquals(Frame,work.Source)||pending!=null||work.Result==null)return false;
            LastCheckpointError=work.Result.Error;return true;
        }
    }
}
