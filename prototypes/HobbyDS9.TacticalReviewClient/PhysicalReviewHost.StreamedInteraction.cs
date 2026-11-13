using System;
using System.Threading;
using HobbyDS9.Application;
using HobbyDS9.Contracts;

namespace HobbyDS9.TacticalReviewClient
{
    public enum StreamedInteractionKind { Preview, Submission, PlanningAdvance }
    public sealed class StreamedInteractionCompletion
    {
        internal readonly StreamedInteractionWork Work;
        internal readonly ICampaignStreamedActivitiesSession? Candidate;
        internal ReviewFrame? Frame;
        internal GeneratedCampaignPresentation? Presentation;
        internal FrontierCommissionSnapshot? Commission;
        internal bool Published;
        public CampaignActivityEligibilityDto? Eligibility {get;internal set;}
        public CampaignTacticalMutationResult? Result {get;internal set;}
        public bool Failed {get;internal set;}
        public bool Committed=>Published;
        public ReviewFrame? PreparedFrame=>Frame;
        internal StreamedInteractionCompletion(StreamedInteractionWork work,ICampaignStreamedActivitiesSession? candidate=null){Work=work;Candidate=candidate;}
    }
    public sealed class StreamedInteractionWork
    {
        internal readonly PhysicalReviewHost Host;
        internal readonly ICampaignStreamedActivitiesSession Source;
        public ReviewFrame SourceFrame {get;}
        public StreamedInteractionKind Kind {get;}
        private readonly CampaignActivityCommandDto? command;
        private int executed,cancelled,finished;
        internal StreamedInteractionWork(PhysicalReviewHost host,ICampaignStreamedActivitiesSession source,ReviewFrame frame,StreamedInteractionKind kind,CampaignActivityCommandDto? command)
        {Host=host;Source=source;SourceFrame=frame;Kind=kind;this.command=command==null?null:Copy(command);}
        public CampaignActivityCommandDto? Command=>command==null?null:Copy(command);
        internal static CampaignActivityCommandDto Copy(CampaignActivityCommandDto c)=>new CampaignActivityCommandDto{ActorId=c.ActorId,ExpectedTurn=c.ExpectedTurn,ExpectedRevision=c.ExpectedRevision,CommandId=c.CommandId,Kind=c.Kind,TargetId=c.TargetId,DefinitionId=c.DefinitionId,BlueprintRevision=c.BlueprintRevision,ShipId=c.ShipId};
        public bool Cancelled=>Volatile.Read(ref cancelled)!=0;
        public bool Settled=>Volatile.Read(ref executed)==0||Volatile.Read(ref finished)!=0;
        internal void Cancel()=>Interlocked.Exchange(ref cancelled,1);
        public StreamedInteractionCompletion Execute()
        {
            if(Interlocked.Exchange(ref executed,1)!=0)throw new InvalidOperationException("Interaction already executed.");
            try{if(Cancelled)return new StreamedInteractionCompletion(this){Failed=true};return Host.ExecuteStreamedInteraction(this);}
            catch(Exception){return new StreamedInteractionCompletion(this){Failed=true};}
            finally{Volatile.Write(ref finished,1);Host.StreamedInteractionSettled(this);}
        }
    }
    public sealed partial class PhysicalReviewHost
    {
        private StreamedInteractionWork? streamedInteraction;
        private GeneratedCampaignPresentation? streamedCommittedPresentation;
        private ICampaignStreamedActivitiesSession? streamedPresentationSource;
        private FrontierCommissionSnapshot? streamedCommittedCommission;
        private ReviewFrame? streamedCommissionFrame;
        internal Action<string>? StreamedInteractionProbe {get;set;}
        private void RequireNoStreamedInteraction()
        {if(streamedInteraction!=null||colonisationInteraction!=null)throw new InvalidOperationException("Wait for the current streamed interaction.");}
        public StreamedInteractionWork PrepareStreamedInteraction(ReviewFrame frame,StreamedInteractionKind kind,CampaignActivityCommandDto? command=null)
        {
            lock(gate)
            {
                RequireNoStreamedInteraction();RequireCurrent(frame.HostId,frame.Generation);
                var status=session.CaptureStatus();
                if(streamedActivitiesPackage==null||streamedActivitiesCodec is not IBackgroundStreamedActivitiesCheckpointCodec||!Enum.IsDefined(typeof(StreamedInteractionKind),kind)
                    ||status.Turn!=frame.Status.Turn||status.CampaignRevision!=frame.Status.CampaignRevision
                    ||kind!=StreamedInteractionKind.PlanningAdvance&&(command==null||command.ActorId!=frame.Actor||command.ExpectedTurn!=frame.Status.Turn||command.ExpectedRevision!=frame.Status.CampaignRevision))
                    throw new InvalidOperationException("Invalid streamed interaction observation.");
                return streamedInteraction=new StreamedInteractionWork(this,(ICampaignStreamedActivitiesSession)session,frame,kind,command);
            }
        }
        internal StreamedInteractionCompletion ExecuteStreamedInteraction(StreamedInteractionWork work)
        {
            // No host gate, source mutation, Unity objects, serialization or replay.
            StreamedInteractionProbe?.Invoke("before-fork");
            var candidate=work.Source.ForkInteraction(work.SourceFrame.Status.Turn,work.SourceFrame.Status.CampaignRevision);
            var completion=new StreamedInteractionCompletion(work,candidate);
            if(work.Cancelled){completion.Failed=true;return completion;}
            StreamedInteractionProbe?.Invoke("before-action");
            if(work.Kind==StreamedInteractionKind.Preview)completion.Eligibility=candidate.EvaluateActivity(work.Command!);
            else completion.Result=work.Kind==StreamedInteractionKind.Submission?candidate.SubmitActivity(work.Command!):candidate.AdvanceTurn(work.SourceFrame.Status.Turn,work.SourceFrame.Status.CampaignRevision);
            if(work.Kind!=StreamedInteractionKind.Preview&&completion.Result!.Accepted)
            {
                var frame=Capture(candidate,work.SourceFrame.Actor,work.SourceFrame.Battle?.EncounterId,work.SourceFrame.Generation);
                if(work.Kind==StreamedInteractionKind.PlanningAdvance&&frame.Encounters.Count==1)frame=Capture(candidate,frame.Actor,frame.Encounters[0],frame.Generation);
                completion.Frame=frame;completion.Presentation=CaptureStreamedActivitiesCandidate(candidate,frame);completion.Commission=candidate.CaptureFrontierCommission(frame.Actor);
            }
            StreamedInteractionProbe?.Invoke("after-capture");return completion;
        }
        public bool CompleteStreamedInteraction(StreamedInteractionWork work,StreamedInteractionCompletion completion,StreamedControllerUpdate? update=null)
        {
            lock(gate)
            {
                if(!ReferenceEquals(streamedInteraction,work)||!ReferenceEquals(completion.Work,work)||work.Cancelled||completion.Failed
                    ||!ReferenceEquals(session,work.Source)||work.SourceFrame.HostId!=id||work.SourceFrame.Generation!=generation)return false;
                var status=session.CaptureStatus();
                if(status.Turn!=work.SourceFrame.Status.Turn||status.CampaignRevision!=work.SourceFrame.Status.CampaignRevision)return false;
                if(work.Kind!=StreamedInteractionKind.Preview&&completion.Result?.Accepted==true)
                {
                    if(completion.Candidate==null||completion.Frame==null||completion.Presentation==null||update==null||!update.Current(work,completion))return false;
                    // All throwing work precedes the live publication assignments.
                    StreamedInteractionProbe?.Invoke("before-publication");
                    if(!ReferenceEquals(streamedInteraction,work)||work.Cancelled||!ReferenceEquals(session,work.Source)||!update.Current(work,completion))return false;
                    session=completion.Candidate;streamedCommittedPresentation=completion.Presentation;streamedPresentationSource=completion.Candidate;streamedCommittedCommission=completion.Commission;streamedCommissionFrame=completion.Frame;update.Apply();completion.Published=true;
                }
                streamedInteraction=null;return true;
            }
        }
        public void AbandonStreamedInteraction(StreamedInteractionWork work)
        {lock(gate){if(!ReferenceEquals(work.Host,this))return;work.Cancel();if(work.Settled&&ReferenceEquals(streamedInteraction,work))streamedInteraction=null;}}
        internal void StreamedInteractionSettled(StreamedInteractionWork work)
        {lock(gate){if(work.Cancelled&&ReferenceEquals(streamedInteraction,work))streamedInteraction=null;}}
    }
    // Prepared on the owning thread. Application is plain assignment only; no
    // host read, callback, allocation or validation can fail after publication.
    public sealed class StreamedControllerUpdate
    {
        private readonly ReviewController controller;
        private readonly StreamedInteractionCompletion completion;
        internal StreamedControllerUpdate(ReviewController controller,StreamedInteractionCompletion completion){this.controller=controller;this.completion=completion;}
        internal bool Current(StreamedInteractionWork work,StreamedInteractionCompletion value)=>ReferenceEquals(completion,value)&&controller.CanReceiveStreamedInteraction(work,value);
        internal void Apply()=>controller.ApplyStreamedInteraction(completion.Frame!,completion.Work.Kind);
    }
    public sealed partial class ReviewController
    {
        internal bool CanReceiveStreamedInteraction(StreamedInteractionWork work,StreamedInteractionCompletion completion)
            =>ReferenceEquals(host,work.Host)&&ReferenceEquals(Frame,work.SourceFrame)&&!RefreshRequired&&!Dirty&&pending==null&&completion.Frame!=null
                &&completion.Frame.HostId==Frame.HostId&&completion.Frame.Generation==Frame.Generation&&completion.Frame.Actor==Frame.Actor
                &&completion.Frame.Status.Turn>=Frame.Status.Turn&&completion.Frame.Status.CampaignRevision>=Frame.Status.CampaignRevision;
        public StreamedControllerUpdate PrepareStreamedInteractionReceive(StreamedInteractionCompletion completion)
        {
            if(!CanReceiveStreamedInteraction(completion.Work,completion))throw new InvalidOperationException("Obsolete streamed controller.");
            return new StreamedControllerUpdate(this,completion);
        }
        internal void ApplyStreamedInteraction(ReviewFrame frame,StreamedInteractionKind kind)
        {Frame=frame;Mode=ModeFor(frame);RefreshRequired=false;LastBackendError=null;if(kind==StreamedInteractionKind.PlanningAdvance&&frame.Encounters.Count==1)SelectedShip=null;}
    }
}
