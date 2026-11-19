using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using HobbyDS9.Application;
using HobbyDS9.Contracts;

namespace HobbyDS9.TacticalReviewClient
{
    public enum EngineeringRefinementInteractionKind { Preview, Submission }
    public sealed class EngineeringRefinementInteractionCompletion
    {
        internal readonly EngineeringRefinementInteractionWork Work;
        internal readonly ICampaignColonisationOperations? Candidate;
        internal ReviewFrame? Frame;
        internal GeneratedCampaignPresentation? Presentation;
        internal FrontierCommissionSnapshot? Commission;
        internal ColonisationCampaignSnapshot? Observation;
        internal IReadOnlyList<ColonisationPlanetSnapshot>? Planets;
        internal bool Published;
        public EngineeringRefinementPreview? Preview {get;internal set;}
        public EngineeringRefinementResult? Result {get;internal set;}
        public EngineeringRefinementSnapshot? PreparedRefinement {get;internal set;}
        public ColonisationCampaignSnapshot? PreparedObservation=>Observation;
        public IReadOnlyList<ColonisationPlanetSnapshot>? PreparedPlanets=>Planets;
        public bool Failed {get;internal set;}
        public bool Committed=>Published;
        public ReviewFrame? PreparedFrame=>Frame;
        internal EngineeringRefinementInteractionCompletion(EngineeringRefinementInteractionWork work,ICampaignColonisationOperations? candidate=null){Work=work;Candidate=candidate;}
    }
    public sealed class EngineeringRefinementInteractionWork
    {
        internal readonly PhysicalReviewHost Host;
        internal readonly ICampaignColonisationOperations Source;
        private readonly EngineeringRefinementCommandDto command;
        private int executed,cancelled,finished;
        public ReviewFrame SourceFrame {get;}
        public EngineeringRefinementInteractionKind Kind {get;}
        public EngineeringRefinementCommandDto Command=>Copy(command);
        public bool Cancelled=>Volatile.Read(ref cancelled)!=0;
        public bool Settled=>Volatile.Read(ref executed)==0||Volatile.Read(ref finished)!=0;
        internal EngineeringRefinementInteractionWork(PhysicalReviewHost host,ICampaignColonisationOperations source,ReviewFrame frame,EngineeringRefinementInteractionKind kind,EngineeringRefinementCommandDto command)
        {Host=host;Source=source;SourceFrame=frame;Kind=kind;this.command=Copy(command);}
        private static EngineeringRefinementCommandDto Copy(EngineeringRefinementCommandDto c)=>new EngineeringRefinementCommandDto
        {ActorId=c.ActorId,CommandId=c.CommandId,ExpectedTurn=c.ExpectedTurn,ExpectedRevision=c.ExpectedRevision,Action=c.Action,ParentVersionId=c.ParentVersionId,Goal=c.Goal,ProjectId=c.ProjectId,ContextBlueprintId=c.ContextBlueprintId,ContextBlueprintRevision=c.ContextBlueprintRevision};
        internal void Cancel()=>Interlocked.Exchange(ref cancelled,1);
        public EngineeringRefinementInteractionCompletion Execute()
        {
            if(Interlocked.Exchange(ref executed,1)!=0)throw new InvalidOperationException("Interaction already executed.");
            try{if(Cancelled)return new EngineeringRefinementInteractionCompletion(this){Failed=true};return Host.ExecuteEngineeringRefinementInteraction(this);}
            catch(Exception){return new EngineeringRefinementInteractionCompletion(this){Failed=true};}
            finally{Volatile.Write(ref finished,1);Host.EngineeringRefinementInteractionSettled(this);}
        }
    }
    public sealed partial class PhysicalReviewHost
    {
        private EngineeringRefinementInteractionWork? engineeringRefinementInteraction;
        public bool SupportsEngineeringRefinement=>colonisationCyclePackage is CampaignEngineeringRefinementPackageDto;
        internal Action<string>? EngineeringRefinementInteractionProbe {get;set;}
        public EngineeringRefinementSnapshot? CaptureEngineeringRefinement(ReviewFrame frame)
        {lock(gate){RequireCurrent(frame.HostId,frame.Generation);var status=session.CaptureStatus();
            if(status.Turn!=frame.Status.Turn||status.CampaignRevision!=frame.Status.CampaignRevision)throw new InvalidOperationException("Engineering observation changed.");
            return (session as ICampaignEngineeringRefinementSession)?.CaptureEngineeringRefinement(frame.Actor);}}
        public EngineeringRefinementInteractionWork PrepareEngineeringRefinementInteraction(ReviewFrame frame,EngineeringRefinementInteractionKind kind,EngineeringRefinementCommandDto command)
        {
            lock(gate)
            {
                RequireNoStreamedInteraction();RequireCurrent(frame.HostId,frame.Generation);var status=session.CaptureStatus();
                if(colonisationPackage==null||(colonisationCyclePackage!=null?colonisationCycleCodec is not IBackgroundColonisationCycleCheckpointCodec:colonisationRepeatPackage!=null?colonisationRepeatCodec is not IBackgroundColonisationRepeatCheckpointCodec:colonisationGrowthPackage!=null?colonisationGrowthCodec is not IBackgroundColonisationGrowthCheckpointCodec:colonisationExpansionPackage!=null?colonisationExpansionCodec is not IBackgroundColonisationExpansionCheckpointCodec:colonisationCodec is not IBackgroundColonisationCheckpointCodec)
                    ||session is not ICampaignColonisationOperations source||session is not ICampaignEngineeringRefinementSession
                    ||!Enum.IsDefined(typeof(EngineeringRefinementInteractionKind),kind)||command==null||command.ActorId!=frame.Actor
                    ||command.ExpectedTurn!=frame.Status.Turn||command.ExpectedRevision!=frame.Status.CampaignRevision
                    ||status.Turn!=frame.Status.Turn||status.CampaignRevision!=frame.Status.CampaignRevision)
                    throw new InvalidOperationException("Invalid Engineering interaction observation.");
                return engineeringRefinementInteraction=new EngineeringRefinementInteractionWork(this,source,frame,kind,command);
            }
        }
        internal EngineeringRefinementInteractionCompletion ExecuteEngineeringRefinementInteraction(EngineeringRefinementInteractionWork work)
        {
            EngineeringRefinementInteractionProbe?.Invoke("before-fork");
            var candidate=work.Source.ForkOperations(work.SourceFrame.Status.Turn,work.SourceFrame.Status.CampaignRevision);
            var completion=new EngineeringRefinementInteractionCompletion(work,candidate);
            if(work.Cancelled||candidate is not ICampaignEngineeringRefinementSession refinement){completion.Failed=true;return completion;}
            EngineeringRefinementInteractionProbe?.Invoke("before-action");
            if(work.Kind==EngineeringRefinementInteractionKind.Preview)completion.Preview=refinement.PreviewEngineeringRefinement(work.Command);
            else completion.Result=refinement.SubmitEngineeringRefinement(work.Command);
            completion.PreparedRefinement=refinement.CaptureEngineeringRefinement(work.SourceFrame.Actor);
            if(completion.Result?.Accepted==true)
            {
                var frame=Capture(candidate,work.SourceFrame.Actor,work.SourceFrame.Battle?.EncounterId,work.SourceFrame.Generation);
                completion.Frame=frame;completion.Presentation=CaptureColonisationCandidate(candidate,frame);completion.Commission=candidate.CaptureFrontierCommission(frame.Actor);
                completion.Observation=candidate.CaptureColonisation(frame.Actor);completion.Planets=candidate.CaptureColonisationPlanets(frame.Actor).Select(p=>new ColonisationPlanetSnapshot(p)).ToList().AsReadOnly();
            }
            EngineeringRefinementInteractionProbe?.Invoke("after-capture");return completion;
        }
        public bool CompleteEngineeringRefinementInteraction(EngineeringRefinementInteractionWork work,EngineeringRefinementInteractionCompletion completion,EngineeringRefinementControllerUpdate? update=null)
        {
            lock(gate)
            {
                if(!ReferenceEquals(engineeringRefinementInteraction,work)||!ReferenceEquals(completion.Work,work)||work.Cancelled||completion.Failed
                    ||!ReferenceEquals(session,work.Source)||work.SourceFrame.HostId!=id||work.SourceFrame.Generation!=generation)return false;
                var status=session.CaptureStatus();if(status.Turn!=work.SourceFrame.Status.Turn||status.CampaignRevision!=work.SourceFrame.Status.CampaignRevision)return false;
                if(completion.Result?.Accepted==true)
                {
                    if(completion.Candidate==null||completion.Frame==null||completion.Presentation==null||update==null||!update.Current(work,completion))return false;
                    EngineeringRefinementInteractionProbe?.Invoke("before-publication");
                    if(!ReferenceEquals(engineeringRefinementInteraction,work)||work.Cancelled||!ReferenceEquals(session,work.Source)||!update.Current(work,completion))return false;
                    session=completion.Candidate;colonisationCommittedPresentation=completion.Presentation;colonisationPresentationSource=completion.Candidate;
                    colonisationCommittedCommission=completion.Commission;colonisationCommissionFrame=completion.Frame;update.Apply();completion.Published=true;
                }
                engineeringRefinementInteraction=null;return true;
            }
        }
        public void AbandonEngineeringRefinementInteraction(EngineeringRefinementInteractionWork work)
        {lock(gate){if(!ReferenceEquals(work.Host,this))return;work.Cancel();if(work.Settled&&ReferenceEquals(engineeringRefinementInteraction,work))engineeringRefinementInteraction=null;}}
        internal void EngineeringRefinementInteractionSettled(EngineeringRefinementInteractionWork work)
        {lock(gate){if(work.Cancelled&&ReferenceEquals(engineeringRefinementInteraction,work))engineeringRefinementInteraction=null;}}
    }
    public sealed class EngineeringRefinementControllerUpdate
    {
        private readonly ReviewController controller;
        private readonly EngineeringRefinementInteractionCompletion completion;
        internal EngineeringRefinementControllerUpdate(ReviewController controller,EngineeringRefinementInteractionCompletion completion){this.controller=controller;this.completion=completion;}
        internal bool Current(EngineeringRefinementInteractionWork work,EngineeringRefinementInteractionCompletion value)=>ReferenceEquals(completion,value)&&controller.CanReceiveEngineeringRefinementInteraction(work,value);
        internal void Apply()=>controller.ApplyColonisationInteraction(completion.PreparedFrame!,ColonisationInteractionKind.Submission);
    }
    public sealed partial class ReviewController
    {
        internal bool CanReceiveEngineeringRefinementInteraction(EngineeringRefinementInteractionWork work,EngineeringRefinementInteractionCompletion completion)
            =>ReferenceEquals(host,work.Host)&&ReferenceEquals(Frame,work.SourceFrame)&&!RefreshRequired&&!Dirty&&pending==null&&completion.PreparedFrame!=null
                &&completion.PreparedFrame.HostId==Frame.HostId&&completion.PreparedFrame.Generation==Frame.Generation&&completion.PreparedFrame.Actor==Frame.Actor
                &&completion.PreparedFrame.Status.Turn==Frame.Status.Turn&&completion.PreparedFrame.Status.CampaignRevision>Frame.Status.CampaignRevision;
        public EngineeringRefinementControllerUpdate PrepareEngineeringRefinementInteractionReceive(EngineeringRefinementInteractionCompletion completion)
        {if(!CanReceiveEngineeringRefinementInteraction(completion.Work,completion))throw new InvalidOperationException("Obsolete Engineering controller.");return new EngineeringRefinementControllerUpdate(this,completion);}
    }
}
