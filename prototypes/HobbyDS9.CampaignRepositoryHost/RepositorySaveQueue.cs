using System;
using System.Threading;
using System.Text;
using HobbyDS9.Contracts;
using HobbyDS9.Persistence;
using HobbyDS9.TacticalReviewClient;

namespace HobbyDS9.CampaignRepositoryHost
{
    public enum AutosaveBoundary { CompletedTurn, AwaitingCombat, BufferedCombatResult, CombatFinalized }
    public enum SaveRequestStatus { Queued, Coalesced, Unchanged, Busy, Stale, NeedsConfirmation, AutomaticPaused, Unavailable }
    public enum SaveOutcome { Committed, NotCommitted, Unknown, Cancelled }
    public enum SaveWorkKind { Checkpoint, Maintenance, Resolution, RefreshHead }
    public sealed class SaveQueueSnapshot
    {
        public int InFlightImages {get;}
        public bool WorkerActive {get;}
        public bool PendingAutosave {get;}
        public Guid? PendingRequestId {get;}
        public bool OutcomeUnknown {get;}
        public bool HeadConfirmed {get;}
        public bool ManualNeedsConfirmation {get;}
        public string? OperationId {get;}
        public bool HasUnsavedProgress {get;}
        public bool AutomaticPaused {get;}
        public bool CanQuitAfterManualSave {get;}
        public CampaignRepositoryStamp? Current {get;}
        public CampaignRepositoryStamp? LastDurable {get;}
        public string? LastDurableCheckpointId {get;}
        public RepositoryError? LastError {get;}
        public SaveOutcome? LastManualOutcome {get;}
        public RepositoryUsage? Usage {get;}
        public BranchInfo? ConfirmedBranch {get;}
        internal SaveQueueSnapshot(int images,bool active,bool auto,Guid? request,bool unknown,bool headConfirmed,bool needsConfirmation,string? operationId,bool dirty,bool paused,bool quit,
            CampaignRepositoryStamp? current,CampaignRepositoryStamp? durable,string? checkpoint,RepositoryError? error,SaveOutcome? manual,RepositoryUsage? usage,BranchInfo? branch)
        {InFlightImages=images;WorkerActive=active;PendingAutosave=auto;PendingRequestId=request;OutcomeUnknown=unknown;HeadConfirmed=headConfirmed;ManualNeedsConfirmation=needsConfirmation;OperationId=operationId;HasUnsavedProgress=dirty;
            AutomaticPaused=paused;CanQuitAfterManualSave=quit;Current=current;LastDurable=durable;LastDurableCheckpointId=checkpoint;LastError=error;LastManualOutcome=manual;Usage=usage;ConfirmedBranch=branch;}
    }
    public sealed class SaveQueueAdmission
    {
        public SaveRequestStatus Status {get;}
        public RepositorySaveWork? Work {get;}
        public Guid? PendingRequestId {get;}
        public ReferenceUpdate? ManualReference {get;}
        internal SaveQueueAdmission(SaveRequestStatus status,RepositorySaveWork? work=null,Guid? pending=null,ReferenceUpdate? reference=null)
        {Status=status;Work=work;PendingRequestId=pending;ManualReference=reference;}
    }
    public sealed class RepositorySaveWork
    {
        private Func<Func<bool>,SaveWorkResult>? execute;
        private SaveWorkResult? result;
        private int cancelled;
        public Guid Token {get;}=Guid.NewGuid();
        public SaveWorkKind Kind {get;}
        public CampaignRepositoryStamp Source {get;}
        public bool Manual {get;}
        public Guid? PendingRequestId {get;}
        public string OperationId {get;}
        internal RepositorySaveWork(SaveWorkKind kind,CampaignRepositoryStamp source,bool manual,Guid? pending,string operationId,Func<Func<bool>,SaveWorkResult> execute)
        {Kind=kind;Source=source;Manual=manual;PendingRequestId=pending;OperationId=operationId;this.execute=execute;}
        public void Cancel()=>Interlocked.Exchange(ref cancelled,1);
        public SaveWorkResult Execute()
        {
            var run=Interlocked.Exchange(ref execute,null)??throw new InvalidOperationException("Save worker is one-use.");
            var done=run(()=>Volatile.Read(ref cancelled)!=0);Volatile.Write(ref result,done);return done;
        }
        internal SaveWorkResult? Result=>Volatile.Read(ref result);
    }
    public sealed class SaveWorkResult
    {
        public SaveOutcome Outcome {get;}
        public DurableReceipt? Receipt {get;}
        public RepositoryError? Error {get;}
        public RepositoryUsage? Usage {get;}
        internal BranchInfo? Head {get;}
        internal SaveWorkResult(SaveOutcome outcome,DurableReceipt? receipt=null,RepositoryError? error=null,BranchInfo? head=null,RepositoryUsage? usage=null)
        {Outcome=outcome;Receipt=receipt;Error=error;Head=head;Usage=usage;}
    }

    // One capture/image permit, one latest autosave stamp, one cancellable manual
    // admission request. All Execute calls belong on a worker, never the UI thread.
    public sealed class RepositorySaveQueue
    {
        private readonly object gate=new object();
        private readonly ICampaignRepositoryCodec codec;
        private readonly Action<int> delay;
        private long softBudget;
        private string boundEpoch;
        private PhysicalReviewHost host;
        private ICampaignRepository repository;
        private BranchInfo branch;
        private Intent? automatic,manual;
        private Attempt? attempt;
        private RepositorySaveWork? worker;
        private RepositoryPayloadWork? payloadWorker;
        private CampaignRepositoryStamp? current,lastDurable,lastManualSource;
        private string? lastCheckpoint;
        private RepositoryError? lastError;
        private SaveOutcome? lastManualOutcome;
        private RepositoryUsage? usage;
        private bool maintenanceDue,headConfirmed=true,contextStale,manualNeedsConfirmation;
        private sealed class Intent
        {
            internal CampaignRepositoryStamp Source=null!;
            internal ReferenceUpdate? Reference;
            internal Guid? Pending;
            internal bool Milestone;
        }
        private sealed class Attempt
        {
            internal ICampaignRepository Repository=null!;
            internal BranchInfo Branch=null!;
            internal CampaignRepositoryStamp Source=null!;
            internal bool Manual,Maintenance;
            internal Guid? Pending;
            internal string Operation="";
            internal ReferenceUpdate? Reference;
            internal CampaignRepositoryCaptureWork? Capture;
            internal CheckpointCommit? Request;
            internal bool Unknown;
        }
        public RepositorySaveQueue(PhysicalReviewHost host,ReviewFrame frame,ICampaignRepository repository,BranchInfo confirmedBranch,ICampaignRepositoryCodec codec,
            long softAutomaticBytes=512L*1024*1024,Action<int>? workerBackoff=null)
        {
            this.host=host??throw new ArgumentNullException(nameof(host));this.repository=repository??throw new ArgumentNullException(nameof(repository));
            branch=confirmedBranch??throw new ArgumentNullException(nameof(confirmedBranch));this.codec=codec??throw new ArgumentNullException(nameof(codec));
            if(softAutomaticBytes<1)throw new ArgumentOutOfRangeException(nameof(softAutomaticBytes));
            softBudget=softAutomaticBytes;delay=workerBackoff??Thread.Sleep;maintenanceDue=branch.Head!=null;
            if(host.CaptureRepositoryBoundary(frame,out var initial)!=CampaignRepositoryError.None)throw new ArgumentException("Current host frame required.",nameof(frame));
            current=initial;boundEpoch=initial!.EpochIdentity;
        }
        public SaveQueueSnapshot Snapshot
        {
            get{lock(gate){var dirty=lastDurable==null||!host.IsRepositoryStateCurrent(lastDurable);
                return new SaveQueueSnapshot(payloadWorker!=null||attempt!=null&&!attempt.Maintenance?1:0,worker!=null||payloadWorker!=null,automatic!=null,manual?.Pending,attempt?.Unknown==true,headConfirmed&&!contextStale,manualNeedsConfirmation,attempt?.Operation,
                    dirty,usage?.AutomaticBudgetExceeded==true,worker==null&&payloadWorker==null&&attempt==null&&manual==null&&!manualNeedsConfirmation&&lastManualOutcome==SaveOutcome.Committed&&lastManualSource!=null&&host.IsRepositoryStateCurrent(lastManualSource),
                    current,lastDurable,lastCheckpoint,lastError,lastManualOutcome,usage,headConfirmed&&!contextStale?branch:null);}}
        }
        public bool CanReplaceSession {get{lock(gate)return worker==null&&payloadWorker==null&&attempt==null;}}
        public CampaignRepositoryError ObserveFrame(ReviewFrame frame)
        {lock(gate){var e=host.CaptureRepositoryBoundary(frame,out var stamp);if(e!=CampaignRepositoryError.None)return e;
            if(stamp!.EpochIdentity!=boundEpoch){contextStale=true;return CampaignRepositoryError.Stale;}current=stamp;return e;}}
        public SaveRequestStatus OfferAutosave(ReviewFrame frame,AutosaveBoundary reason)
        {
            lock(gate)
            {
                if(!Enum.IsDefined(typeof(AutosaveBoundary),reason))throw new ArgumentOutOfRangeException(nameof(reason));
                if(contextStale||ObserveFrame(frame)!=CampaignRepositoryError.None)return SaveRequestStatus.Stale;
                var planning=current!.Phase==SessionPhase.Planning;
                if((reason==AutosaveBoundary.CompletedTurn||reason==AutosaveBoundary.CombatFinalized)!=planning)return SaveRequestStatus.Unavailable;
                if(current.SameState(lastDurable)||attempt!=null&&!attempt.Manual&&current.SameState(attempt.Source))return SaveRequestStatus.Unchanged;
                var coalesced=automatic!=null;
                var milestone=automatic?.Milestone==true||reason==AutosaveBoundary.CompletedTurn&&current.Turn>1&&(current.Turn-1)%10==0;
                automatic=new Intent{Source=current,Milestone=milestone};
                return usage?.AutomaticBudgetExceeded==true?SaveRequestStatus.AutomaticPaused:coalesced?SaveRequestStatus.Coalesced:SaveRequestStatus.Queued;
            }
        }
        public SaveRequestStatus RequestManual(ReviewFrame frame,ReferenceUpdate reference,out Guid pendingRequestId)
        {
            pendingRequestId=Guid.Empty;if(reference==null)throw new ArgumentNullException(nameof(reference));
            if(reference.Kind!=SaveReferenceKind.Manual||string.IsNullOrEmpty(reference.Id)||reference.Id.Length>96||string.IsNullOrEmpty(reference.Name)||reference.Name.Length>120
                ||reference.Notes==null||reference.Notes.Length>1000||reference.ExpectedGeneration<0||reference.Id.IndexOf('\0')>=0||reference.Name.IndexOf('\0')>=0||reference.Notes.IndexOf('\0')>=0)
                throw new ArgumentException("Bounded manual reference required.",nameof(reference));
            var utf8=new UTF8Encoding(false,true);utf8.GetByteCount(reference.Id);utf8.GetByteCount(reference.Name);utf8.GetByteCount(reference.Notes);
            lock(gate)
            {
                if(manual!=null||attempt?.Manual==true)return SaveRequestStatus.Busy;
                if(contextStale||ObserveFrame(frame)!=CampaignRepositoryError.None)return SaveRequestStatus.Stale;
                pendingRequestId=Guid.NewGuid();manual=new Intent{Source=current!,Reference=reference,Pending=pendingRequestId};
                manualNeedsConfirmation=false;lastManualOutcome=null;lastManualSource=null;return SaveRequestStatus.Queued;
            }
        }
        public bool CancelWaitingRequest(Guid pendingRequestId)
        {lock(gate){if(manual?.Pending!=pendingRequestId)return false;manual=null;lastManualOutcome=SaveOutcome.Cancelled;return true;}}

        public SaveQueueAdmission PrepareNext(ReviewFrame frame)
        {
            lock(gate)
            {
                if(worker!=null||payloadWorker!=null)return new SaveQueueAdmission(SaveRequestStatus.Busy);
                if(attempt?.Unknown==true)return StartWork(SaveWorkKind.Resolution,attempt);
                if(contextStale||ObserveFrame(frame)!=CampaignRepositoryError.None)return new SaveQueueAdmission(SaveRequestStatus.Stale);
                if(!headConfirmed)return StartWork(SaveWorkKind.RefreshHead,new Attempt{Repository=repository,Branch=branch,Source=current!,Maintenance=true});
                if(maintenanceDue)return StartWork(SaveWorkKind.Maintenance,new Attempt{Repository=repository,Branch=branch,Source=current!,Maintenance=true,Operation="retention-"+Guid.NewGuid().ToString("N")});
                var selected=manual??automatic;
                if(selected==null)return new SaveQueueAdmission(manualNeedsConfirmation?SaveRequestStatus.NeedsConfirmation:SaveRequestStatus.Unchanged);
                if(!selected.Source.SameState(current))
                {
                    if(manual!=null){manual=null;manualNeedsConfirmation=true;return new SaveQueueAdmission(SaveRequestStatus.NeedsConfirmation,pending:selected.Pending,reference:selected.Reference);}
                    automatic=null;return new SaveQueueAdmission(SaveRequestStatus.Stale);
                }
                if(manual==null&&usage?.AutomaticBudgetExceeded==true)return new SaveQueueAdmission(SaveRequestStatus.AutomaticPaused);
                var captured=host.PrepareRepositoryCheckpoint(frame,codec);
                if(captured.Error!=CampaignRepositoryError.None)return new SaveQueueAdmission(captured.Error==CampaignRepositoryError.Busy?SaveRequestStatus.Busy:SaveRequestStatus.Stale);
                var isManual=manual!=null;
                var reference=selected.Reference??new ReferenceUpdate((selected.Milestone?"milestone-":"auto-")+Guid.NewGuid().ToString("N"),0,
                    selected.Milestone?SaveReferenceKind.Milestone:SaveReferenceKind.Auto,"Recovery turn "+current!.Turn,"Latest complete boundary; intermediate requests may coalesce.");
                if(isManual)manual=null;else automatic=null;
                return StartWork(SaveWorkKind.Checkpoint,new Attempt{Repository=repository,Branch=branch,Source=captured.Work!.Source,Capture=captured.Work,
                    Manual=isManual,Pending=selected.Pending,Reference=reference,Operation="save-"+Guid.NewGuid().ToString("N")});
            }
        }
        private SaveQueueAdmission StartWork(SaveWorkKind kind,Attempt active)
        {
            attempt=active;
            worker=new RepositorySaveWork(kind,active.Source,active.Manual,active.Pending,active.Operation,cancelled=>Execute(kind,active,cancelled));
            return new SaveQueueAdmission(SaveRequestStatus.Queued,worker);
        }
        private T RetryBusy<T>(Func<T> operation)
        {
            for(var retry=0;;retry++)try{return operation();}
            catch(RepositoryException e) when(e.Error==RepositoryError.Busy&&retry<3){delay(125<<retry);}
        }
        private BranchInfo ReadHead(Attempt a)
        {try{return RetryBusy(()=>a.Repository.GetBranch(a.Branch.Id));}catch(RepositoryException e) when(e.Error==RepositoryError.NotFound){return new BranchInfo(a.Branch.Id,null,0);}}
        private SaveWorkResult KnownFailure(Attempt a,RepositoryError? error,SaveOutcome outcome=SaveOutcome.NotCommitted)
        {
            try{return new SaveWorkResult(outcome,error:error,head:ReadHead(a));}
            catch(Exception){return new SaveWorkResult(outcome,error:error??RepositoryError.Unavailable);}
        }
        private SaveWorkResult MaintenanceCommitted(Attempt a,DurableReceipt receipt)
        {
            BranchInfo? head=null;
            try{head=ReadHead(a);return new SaveWorkResult(SaveOutcome.Committed,receipt,head:head,usage:RetryBusy(()=>a.Repository.GetStorageUsage(head,softBudget)));}
            catch(RepositoryException e){return new SaveWorkResult(SaveOutcome.Committed,receipt,e.Error,head);}
            catch(Exception){return new SaveWorkResult(SaveOutcome.Committed,receipt,RepositoryError.Unavailable,head);}
        }
        private SaveWorkResult Resolve(Attempt a)
        {
            try
            {
                var receipt=RetryBusy(()=>a.Repository.FindOperation(a.Operation));
                if(receipt==null)return KnownFailure(a,null);
                // Reissuing the immutable request checks its exact fingerprint and
                // returns the existing receipt before CAS, never creates a new ID.
                receipt=a.Maintenance?RetryBusy(()=>a.Repository.ApplyRetention(a.Operation,a.Branch,10,3,softBudget))
                    :RetryBusy(()=>a.Repository.CommitCheckpoint(a.Request!));
                if(a.Maintenance)return MaintenanceCommitted(a,receipt);
                // An idempotent receipt proves this operation committed, but a
                // later writer may already have advanced the branch.
                try{return new SaveWorkResult(SaveOutcome.Committed,receipt,head:ReadHead(a));}
                catch(RepositoryException e){return new SaveWorkResult(SaveOutcome.Committed,receipt,e.Error);}
                catch(Exception){return new SaveWorkResult(SaveOutcome.Committed,receipt,RepositoryError.Unavailable);}
            }
            catch(Exception){return new SaveWorkResult(SaveOutcome.Unknown,error:RepositoryError.OutcomeUnknown);}
        }
        private SaveWorkResult Execute(SaveWorkKind kind,Attempt a,Func<bool> cancelled)
        {
            if(kind==SaveWorkKind.Resolution)return Resolve(a);
            if(kind==SaveWorkKind.RefreshHead)return KnownFailure(a,null);
            try
            {
                if(cancelled())return KnownFailure(a,null,SaveOutcome.Cancelled);
                if(a.Maintenance)
                {
                    var receipt=RetryBusy(()=>a.Repository.ApplyRetention(a.Operation,a.Branch,10,3,softBudget));
                    return MaintenanceCommitted(a,receipt);
                }
                var capture=a.Capture!.Execute(cancelled);a.Capture=null;
                if(!capture.Succeeded)return KnownFailure(a,capture.Error==CampaignRepositoryError.Cancelled?null:RepositoryError.ValidationFailed,
                    capture.Error==CampaignRepositoryError.Cancelled?SaveOutcome.Cancelled:SaveOutcome.NotCommitted);
                a.Request=new CheckpointCommit(a.Operation,a.Branch.Id,a.Branch.Generation,a.Branch.Head,CycleCheckpointValidator.Map(capture.Image!),a.Reference,a.Source.EpochIdentity);
                if(cancelled())return KnownFailure(a,null,SaveOutcome.Cancelled);
                var done=RetryBusy(()=>a.Repository.CommitCheckpoint(a.Request));
                return new SaveWorkResult(SaveOutcome.Committed,done,head:new BranchInfo(a.Branch.Id,done.CheckpointId,done.BranchGeneration));
            }
            catch(RepositoryException e)
            {
                if(e.Error==RepositoryError.OutcomeUnknown)return Resolve(a);
                return KnownFailure(a,e.Error);
            }
            catch(Exception)
            {return a.Request!=null||a.Maintenance?Resolve(a):KnownFailure(a,RepositoryError.ValidationFailed);}
        }
        public bool Complete(RepositorySaveWork completed)
        {
            if(completed==null)throw new ArgumentNullException(nameof(completed));
            lock(gate)
            {
                if(!ReferenceEquals(worker,completed)||completed.Result==null||attempt==null)return false;
                var result=completed.Result;var active=attempt;worker=null;
                if(result.Outcome==SaveOutcome.Unknown){active.Unknown=true;lastError=RepositoryError.OutcomeUnknown;
                    if(active.Manual){lastManualOutcome=SaveOutcome.Unknown;lastManualSource=active.Source;}return true;}
                active.Unknown=false;attempt=null;active.Capture=null;active.Request=null;
                var epochCurrent=host.IsRepositorySessionCurrent(active.Source);contextStale|=!epochCurrent;
                if(epochCurrent&&result.Head!=null){branch=result.Head;headConfirmed=true;}else headConfirmed=false;
                lastError=result.Error;
                if(completed.Kind==SaveWorkKind.RefreshHead)return true;
                if(active.Maintenance){maintenanceDue=false;if(epochCurrent&&result.Usage!=null)usage=result.Usage;return true;}
                if(active.Manual){lastManualOutcome=result.Outcome;lastManualSource=active.Source;}
                if(result.Outcome==SaveOutcome.Committed)
                {
                    if(epochCurrent){lastDurable=active.Source;lastCheckpoint=result.Receipt!.CheckpointId;maintenanceDue=true;
                        if(automatic?.Source.SameState(lastDurable)==true)automatic=null;}
                }
                return true;
            }
        }
        // The UI waits/drains work before load/new/rebind. Cancellation cannot
        // erase an already committed receipt or abandon an unknown operation.
        public bool Rebind(PhysicalReviewHost newHost,ReviewFrame frame,ICampaignRepository newRepository,BranchInfo confirmedBranch)
        {
            if(newHost==null||newRepository==null||confirmedBranch==null)throw new ArgumentNullException();
            lock(gate)
            {
                if(worker!=null||payloadWorker!=null||attempt!=null)return false;
                if(newHost.CaptureRepositoryBoundary(frame,out var source)!=CampaignRepositoryError.None)return false;
                host=newHost;repository=newRepository;branch=confirmedBranch;automatic=null;manual=null;current=source;boundEpoch=source!.EpochIdentity;lastDurable=null;lastCheckpoint=null;
                lastManualSource=null;lastManualOutcome=null;manualNeedsConfirmation=false;lastError=null;usage=null;
                contextStale=false;headConfirmed=true;maintenanceDue=branch.Head!=null;
                return true;
            }
        }
        public bool ConfirmRestoredCheckpoint(CampaignRepositoryRestoreResult restored,CheckpointImage stored,string checkpointId)
        {
            if(restored==null||stored==null)throw new ArgumentNullException();
            lock(gate)
            {
                if(worker!=null||payloadWorker!=null||attempt!=null||restored.Image==null||restored.PublishedStamp==null||branch.Head!=checkpointId
                    ||restored.PublishedStamp.EpochIdentity!=boundEpoch||!host.IsRepositoryStateCurrent(restored.PublishedStamp)||!CycleCheckpointValidator.MetadataMatches(stored,restored.Image))return false;
                current=lastDurable=restored.PublishedStamp;lastCheckpoint=checkpointId;contextStale=false;return true;
            }
        }
        public bool UpdateSoftAutomaticBudget(long bytes)
        {
            if(bytes<1)throw new ArgumentOutOfRangeException(nameof(bytes));
            lock(gate){if(worker!=null||payloadWorker!=null||attempt!=null)return false;softBudget=bytes;usage=null;maintenanceDue=branch.Head!=null;return true;}
        }
        public RepositoryPayloadAdmission PreparePayloadRead(ReviewFrame frame,string checkpointId)
        {
            if(string.IsNullOrEmpty(checkpointId)||checkpointId.Length>96)throw new ArgumentException("Checkpoint identity required.",nameof(checkpointId));
            lock(gate)
            {
                if(worker!=null||payloadWorker!=null||attempt!=null)return new RepositoryPayloadAdmission(SaveRequestStatus.Busy);
                if(contextStale||ObserveFrame(frame)!=CampaignRepositoryError.None)return new RepositoryPayloadAdmission(SaveRequestStatus.Stale);
                var source=repository;RepositoryError? storageError=null;
                var prepared=host.PrepareRepositoryRestore(frame,()=>{
                    try
                    {
                        var stored=RetryBusy(()=>source.ReadCheckpoint(checkpointId));
                        return new CampaignRepositoryStoredPayload(stored.ReadPayload(),stored.EditionIdentity,stored.Turn,stored.Revision,stored.Phase,stored.PayloadHash);
                    }
                    catch(RepositoryException e){storageError=e.Error;throw;}
                },codec);
                if(prepared.Error!=CampaignRepositoryError.None)return new RepositoryPayloadAdmission(prepared.Error==CampaignRepositoryError.Busy?SaveRequestStatus.Busy:SaveRequestStatus.Stale);
                payloadWorker=new RepositoryPayloadWork(checkpointId,prepared.Work!,()=>storageError);
                return new RepositoryPayloadAdmission(SaveRequestStatus.Queued,payloadWorker);
            }
        }
        public bool CompletePayloadValidation(RepositoryPayloadWork completed)
        {
            if(completed==null)throw new ArgumentNullException(nameof(completed));
            lock(gate)
            {
                if(!ReferenceEquals(payloadWorker,completed)||completed.Result==null)return false;
                var sameEpoch=host.IsRepositorySessionCurrent(completed.Source);
                completed.Result.Release();payloadWorker=null;contextStale|=!sameEpoch;
                return sameEpoch&&!completed.CancellationRequested;
            }
        }
        public CampaignRepositoryError CompletePayloadRestore(RepositoryPayloadWork completed,out ReviewFrame? frame)
        {
            frame=null;if(completed==null)throw new ArgumentNullException(nameof(completed));
            lock(gate)
            {
                if(!ReferenceEquals(payloadWorker,completed)||completed.Result?.Restore==null)return CampaignRepositoryError.Stale;
                lock(completed.AdoptionGate)
                {
                    var result=completed.Result;
                    if(completed.CancellationRequested){result.Release();payloadWorker=null;return CampaignRepositoryError.Cancelled;}
                    var error=host.CompleteRepositoryRestore(completed.Restore,result.Restore,out frame);
                    // Busy retains ownership until the outer interaction drains;
                    // cancellation is checked again on the subsequent adoption.
                    if(error==CampaignRepositoryError.Busy)return error;
                    if(error==CampaignRepositoryError.None)
                    {
                        result.Loaded=new RepositoryLoadedCheckpoint(completed.CheckpointId,result.Restore.PublishedStamp!,result.Summary!);
                        contextStale=true;
                    }
                    result.Release();payloadWorker=null;return error;
                }
            }
        }
        public bool ConfirmRestoredCheckpoint(RepositoryLoadedCheckpoint loaded)
        {
            if(loaded==null)throw new ArgumentNullException(nameof(loaded));
            lock(gate)
            {
                if(worker!=null||payloadWorker!=null||attempt!=null||loaded.CheckpointId!=branch.Head||loaded.Source.EpochIdentity!=boundEpoch||!host.IsRepositoryStateCurrent(loaded.Source))return false;
                current=lastDurable=loaded.Source;lastCheckpoint=loaded.CheckpointId;contextStale=false;return true;
            }
        }
    }
}
