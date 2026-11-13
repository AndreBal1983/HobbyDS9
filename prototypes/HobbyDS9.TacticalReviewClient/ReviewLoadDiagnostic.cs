using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace HobbyDS9.TacticalReviewClient
{
    public enum ReviewLoadPhase
    {
        TemplateLoad, Resources, ActivityCompose, ColonisationCompose, RosterCompose,
        StoreCreate, ModelContinue, WorkerReturned, HideBusy, Publish, HostOpen,
        StoreRead, RosterCopy, SaveCopy, FullReplay, ReadFrame, Presentation,
        ControllerCreate, DecodeParse, DecodeShape, DecodeNatural, DecodeTyped, DecodeCarrier
    }
    public enum ReviewLoadEvent { Begin, NormalReturn, Error }
    public enum ReviewLoadFailureType
    {
        None, ArgumentNull, Argument, InvalidOperation, InvalidData, IO,
        UnauthorizedAccess, MissingMethod, TypeLoad, NotSupported, Overflow, OutOfMemory, Other
    }
    public readonly struct ReviewLoadDiagnosticRecord
    {
        public ReviewLoadPhase Phase { get; }
        public ReviewLoadEvent Event { get; }
        public ReviewLoadFailureType Failure { get; }
        public ReviewCheckpointError Error { get; }
        public long ElapsedTicks { get; }
        internal ReviewLoadDiagnosticRecord(ReviewLoadPhase phase,ReviewLoadEvent kind,
            ReviewLoadFailureType failure,ReviewCheckpointError error,long elapsed)
        {Phase=phase;Event=kind;Failure=failure;Error=error;ElapsedTicks=elapsed;}
    }
    // Operation-local observation only. Null is the disabled path; no gameplay authority.
    public sealed class ReviewLoadDiagnostic
    {
        public const int MaximumRecords=96;
        private readonly Action<ReviewLoadDiagnosticRecord> sink;
        private readonly long started;
        private int count;
        private int closed;
        private ReviewLoadDiagnostic(Action<ReviewLoadDiagnosticRecord> sink)
        {this.sink=sink;started=Stopwatch.GetTimestamp();}
        public static ReviewLoadDiagnostic? Create(Action<ReviewLoadDiagnosticRecord>? sink)
        {
            if(sink==null)return null;
            try {return new ReviewLoadDiagnostic(sink);}catch(Exception){return null;}
        }
        public void Record(ReviewLoadPhase phase,ReviewLoadEvent kind,Exception? exception=null,
            ReviewCheckpointError error=ReviewCheckpointError.None)
        {
            // Only diagnostic formatting/clock/callback work is protected by this catch.
            try
            {
                if(Volatile.Read(ref closed)!=0||Volatile.Read(ref count)>=MaximumRecords)return;
                if((uint)phase>(uint)ReviewLoadPhase.DecodeCarrier||(uint)kind>(uint)ReviewLoadEvent.Error||
                    (uint)error>(uint)ReviewCheckpointError.DirtyDraft)return;
                if(Interlocked.Increment(ref count)>MaximumRecords)return;
                sink(new ReviewLoadDiagnosticRecord(phase,kind,Classify(exception),error,Stopwatch.GetTimestamp()-started));
            }
            catch(Exception) { }
        }
        public void Close() {Volatile.Write(ref closed,1);}
        public static ReviewLoadFailureType Classify(Exception? error)
        {
            if(error==null)return ReviewLoadFailureType.None;
            if(error is ArgumentNullException)return ReviewLoadFailureType.ArgumentNull;
            if(error is ArgumentException)return ReviewLoadFailureType.Argument;
            if(error is InvalidOperationException)return ReviewLoadFailureType.InvalidOperation;
            if(error is InvalidDataException)return ReviewLoadFailureType.InvalidData;
            if(error is IOException)return ReviewLoadFailureType.IO;
            if(error is UnauthorizedAccessException)return ReviewLoadFailureType.UnauthorizedAccess;
            if(error is MissingMethodException)return ReviewLoadFailureType.MissingMethod;
            if(error is TypeLoadException)return ReviewLoadFailureType.TypeLoad;
            if(error is NotSupportedException)return ReviewLoadFailureType.NotSupported;
            if(error is OverflowException)return ReviewLoadFailureType.Overflow;
            if(error is OutOfMemoryException)return ReviewLoadFailureType.OutOfMemory;
            return ReviewLoadFailureType.Other;
        }
    }
}
