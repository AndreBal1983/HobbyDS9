using System.Collections.Generic;
using HobbyDS9.Contracts.ShipDesign;
namespace HobbyDS9.Contracts
{
    public static class EngineeringRefinementIdentity
    {
        public const int Format=17;
        public const string Profile="generated-engineering-refinement-v1",PackageProfile="generated-engineering-refinement-package-v1";
        public const string Policy="targeted-reactor-refinement-v1";
        public const string SaveDirectorySuffix="-engineering-refinement-v1";
        public const int MaximumProjects=64,MaximumCommands=1024,MaturityScale=1000;
        // Internal Engineering units (600 = one displayed Engineering unit), before pace.
        public const long StandardWork=24000;
    }
    public enum EngineeringRefinementGoal { ReduceCapacity=1,IncreasePowerSupply=2,ReduceIndustry=3 }
    public enum EngineeringRefinementAction { Start=1,Accept=2,Continue=3,Discard=4,Pause=5,Resume=6 }
    public enum EngineeringRefinementStage { Working=1,Pending=2,Released=3,RetainedInternal=4,Discarded=5 }
    public enum EngineeringRefinementError { None=0,Unavailable=1,ActorNotControlled=2,StaleTurn=3,StaleRevision=4,Duplicate=5,WrongPhase=6,UnknownParent=7,UnsupportedGoal=8,NoRepresentativeFit=9,GenerationLocked=10,ActiveProject=11,PendingOutcome=12,Exhausted=13,UnknownOutcome=14,InvalidDecision=15,Capacity=16,InvalidCommand=17 }
    public sealed class EngineeringRefinementCommandDto
    {
        public string ActorId {get;set;}="";public string CommandId {get;set;}="";
        public int ExpectedTurn {get;set;}public long ExpectedRevision {get;set;}
        public EngineeringRefinementAction Action {get;set;}
        public string? ParentVersionId {get;set;}public EngineeringRefinementGoal Goal {get;set;}
        public string? ProjectId {get;set;}
        public string? ContextBlueprintId {get;set;}public int ContextBlueprintRevision {get;set;}
    }
    public sealed class EngineeringRefinementVersionDto
    {
        public string VersionId {get;set;}="";public string OwnerId {get;set;}="";public string RootVersionId {get;set;}="";
        public string ParentVersionId {get;set;}="";public int MaturityMilli {get;set;}
        public ComponentVersionDto? Component {get;set;}
        public EngineeringRefinementStage Stage {get;set;}
    }
    public sealed class EngineeringRefinementProjectDto
    {
        public string ProjectId {get;set;}="";public string ActorId {get;set;}="";public int Ordinal {get;set;}
        public EngineeringRefinementGoal Goal {get;set;}public string ParentVersionId {get;set;}="";public string RootVersionId {get;set;}="";
        public int ParentMaturityMilli {get;set;}public int StartedTurn {get;set;}public int? ResolvedTurn {get;set;}
        public long EffectiveWork {get;set;}public EngineeringRefinementStage Stage {get;set;}
        public BlueprintRevisionDto? Context {get;set;}public string InstallationId {get;set;}="";
        public ComponentVersionDto? Before {get;set;}public ComponentVersionDto? GenerationRoot {get;set;}
        public EngineeringRefinementVersionDto? Outcome {get;set;}
        public EngineeringRefinementGoal? Downside {get;set;}public string? DrawIdentity {get;set;}
        public int? DecisionTurn {get;set;}
    }
    public sealed class EngineeringRefinementOperationDto
    {
        public int BaseJournalOffset {get;set;}public EngineeringRefinementCommandDto? Command {get;set;}
    }
    public sealed class EngineeringRefinementStateDto
    {
        public string PolicyId {get;set;}=EngineeringRefinementIdentity.Policy;
        public List<EngineeringRefinementProjectDto>? Projects {get;set;}=new List<EngineeringRefinementProjectDto>();
        public List<EngineeringRefinementOperationDto>? Operations {get;set;}=new List<EngineeringRefinementOperationDto>();
    }
    public sealed class EngineeringRefinementResult
    {
        public EngineeringRefinementError Error {get;}public bool Accepted=>Error==EngineeringRefinementError.None;
        public int Turn {get;}public long Revision {get;}public string? ProjectId {get;}
        public EngineeringRefinementResult(EngineeringRefinementError error,int turn,long revision,string? project=null){Error=error;Turn=turn;Revision=revision;ProjectId=project;}
    }
    public sealed class EngineeringRefinementVectorSnapshot
    {
        public string VersionId {get;}public long Capacity {get;}public long PowerSupply {get;}public long Industry {get;}public int? MaturityMilli {get;}
        public ComponentVersionSnapshot Component {get;}
        public EngineeringRefinementVectorSnapshot(ComponentVersionDto c,int? maturity){VersionId=c.VersionId;Capacity=c.Capacity;PowerSupply=c.PowerSupply;Industry=c.Cost!.Industry;MaturityMilli=maturity;Component=new ComponentVersionSnapshot(c);}
    }
    public sealed class EngineeringRefinementPreview
    {
        public EngineeringRefinementError Error {get;}public bool CanStart=>Error==EngineeringRefinementError.None;
        public EngineeringRefinementVectorSnapshot? Parent {get;}public long Work {get;}
        public long? TargetMinimum {get;}public long? TargetMaximum {get;}public string? ContextBlueprintId {get;}public int ContextBlueprintRevision {get;}
        public EngineeringRefinementPreview(EngineeringRefinementError error,EngineeringRefinementVectorSnapshot? parent=null,long work=0,long? minimum=null,long? maximum=null,string? context=null,int revision=0)
        {Error=error;Parent=parent;Work=work;TargetMinimum=minimum;TargetMaximum=maximum;ContextBlueprintId=context;ContextBlueprintRevision=revision;}
    }
    public sealed class EngineeringRefinementProjectSnapshot
    {
        public string ProjectId {get;}public EngineeringRefinementGoal Goal {get;}public EngineeringRefinementStage Stage {get;}
        public EngineeringRefinementVectorSnapshot Before {get;}public EngineeringRefinementVectorSnapshot? Outcome {get;}
        public EngineeringRefinementGoal? Downside {get;}public long Work {get;}public long Progress {get;}
        public bool CanAccept {get;}public bool CanContinue {get;}public bool CanDiscard {get;}
        public bool IsActive {get;}public bool CanPause {get;}public bool CanResume {get;}
        public EngineeringRefinementProjectSnapshot(EngineeringRefinementProjectDto p,long progress,bool active=false,bool releaseEligible=true,bool resumeAvailable=true,bool canInteract=true)
        {IsActive=active;CanPause=canInteract&&p.Stage==EngineeringRefinementStage.Working&&active;CanResume=canInteract&&p.Stage==EngineeringRefinementStage.Working&&!active&&resumeAvailable;ProjectId=p.ProjectId;Goal=p.Goal;Stage=p.Stage;Before=new EngineeringRefinementVectorSnapshot(p.Before!,p.ParentMaturityMilli);Outcome=p.Outcome==null?null:new EngineeringRefinementVectorSnapshot(p.Outcome.Component!,p.Outcome.MaturityMilli);
         Downside=p.Downside;Work=p.EffectiveWork;Progress=progress;CanAccept=canInteract&&releaseEligible&&(p.Stage==EngineeringRefinementStage.Pending||p.Stage==EngineeringRefinementStage.RetainedInternal);CanContinue=canInteract&&p.Stage==EngineeringRefinementStage.Pending;CanDiscard=canInteract&&p.Stage==EngineeringRefinementStage.Pending;}
    }
    public sealed class EngineeringRefinementParentSnapshot
    {
        public EngineeringRefinementVectorSnapshot Vector {get;}public string DisplayName {get;}public bool Released {get;}
        public EngineeringRefinementParentSnapshot(ComponentVersionDto c,int? maturity,bool released){Vector=new EngineeringRefinementVectorSnapshot(c,maturity);DisplayName=c.DisplayName;Released=released;}
    }
    public sealed class EngineeringRefinementSnapshot
    {
        public string ActorId {get;}public int Turn {get;}public long Revision {get;}public string PolicyId=>EngineeringRefinementIdentity.Policy;
        public IReadOnlyList<EngineeringRefinementParentSnapshot> Parents {get;}public IReadOnlyList<EngineeringRefinementProjectSnapshot> Projects {get;}
        public EngineeringRefinementSnapshot(string actor,int turn,long revision,IEnumerable<EngineeringRefinementParentSnapshot> parents,IEnumerable<EngineeringRefinementProjectSnapshot> projects)
        {ActorId=actor;Turn=turn;Revision=revision;Parents=new List<EngineeringRefinementParentSnapshot>(parents).AsReadOnly();Projects=new List<EngineeringRefinementProjectSnapshot>(projects).AsReadOnly();}
    }
}
