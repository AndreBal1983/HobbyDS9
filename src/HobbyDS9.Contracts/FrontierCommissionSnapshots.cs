using System;
using System.Collections.Generic;
using System.Linq;
namespace HobbyDS9.Contracts
{
    public static class FrontierCommissionIdentity
    {public const string Mission="mission.frontier-commission.v1",Policy="frontier-commission-retained-evidence-v1";public const int Revision=1;}
    public enum FrontierCommissionAvailability { Available=1, UnsupportedContent=2 }
    public enum FrontierCommissionProgress { NotStarted=1, Underway=2, Completed=3 }
    public sealed class FrontierCommissionEvidence
    {
        public string ReceiptId {get;}public string RelatedId {get;}public string Label {get;}public int Turn {get;}public string Report {get;}
        public long LearnedInternalUnits {get;}public long UnlearnableInternalUnits {get;}
        public FrontierCommissionEvidence(string receiptId,string relatedId,string label,int turn,string report,long learned=0,long unlearnable=0)
        {ReceiptId=receiptId;RelatedId=relatedId;Label=label;Turn=turn;Report=report;LearnedInternalUnits=learned;UnlearnableInternalUnits=unlearnable;}
    }
    public sealed class FrontierCommissionObjective
    {
        public string Id {get;}public string Caption {get;}public string Guidance {get;}public FrontierCommissionProgress Progress {get;}public FrontierCommissionEvidence? Evidence {get;}
        public FrontierCommissionObjective(string id,string caption,string guidance,bool underway,FrontierCommissionEvidence? evidence)
        {Id=id;Caption=caption;Guidance=guidance;Evidence=evidence;Progress=evidence!=null?FrontierCommissionProgress.Completed:underway?FrontierCommissionProgress.Underway:FrontierCommissionProgress.NotStarted;}
    }
    public sealed class FrontierCommissionSnapshot
    {
        public string MissionId=>FrontierCommissionIdentity.Mission;public int MissionRevision=>FrontierCommissionIdentity.Revision;public string EvaluationPolicy=>FrontierCommissionIdentity.Policy;
        public FrontierCommissionAvailability Availability {get;}public string Actor {get;}public int Turn {get;}public long Revision {get;}public string SourcePackageHash {get;}
        public IReadOnlyList<FrontierCommissionObjective> Objectives {get;}public int CompletedObjectives=>Objectives.Count(o=>o.Progress==FrontierCommissionProgress.Completed);
        public bool Completed=>Availability==FrontierCommissionAvailability.Available&&Objectives.Count==4&&CompletedObjectives==4;
        public int? CompletedTurn=>Completed?Objectives.Max(o=>o.Evidence!.Turn):(int?)null;
        public FrontierCommissionSnapshot(FrontierCommissionAvailability availability,string actor,int turn,long revision,string sourcePackageHash,IEnumerable<FrontierCommissionObjective> objectives)
        {Availability=availability;Actor=actor;Turn=turn;Revision=revision;SourcePackageHash=sourcePackageHash;Objectives=(availability==FrontierCommissionAvailability.Available?objectives.ToArray():Array.Empty<FrontierCommissionObjective>()).ToList().AsReadOnly();}
    }
}
