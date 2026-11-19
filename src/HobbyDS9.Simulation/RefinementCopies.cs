using System.Linq;
using HobbyDS9.Contracts;
namespace HobbyDS9.Simulation
{
    internal static class RefinementCopies
    {
        internal static EngineeringRefinementCommandDto Copy(EngineeringRefinementCommandDto c)=>new EngineeringRefinementCommandDto{ActorId=c.ActorId,CommandId=c.CommandId,ExpectedTurn=c.ExpectedTurn,ExpectedRevision=c.ExpectedRevision,Action=c.Action,ParentVersionId=c.ParentVersionId,Goal=c.Goal,ProjectId=c.ProjectId,ContextBlueprintId=c.ContextBlueprintId,ContextBlueprintRevision=c.ContextBlueprintRevision};
        internal static EngineeringRefinementVersionDto Copy(EngineeringRefinementVersionDto v)=>new EngineeringRefinementVersionDto{VersionId=v.VersionId,OwnerId=v.OwnerId,RootVersionId=v.RootVersionId,ParentVersionId=v.ParentVersionId,MaturityMilli=v.MaturityMilli,Stage=v.Stage,Component=v.Component==null?null:BlueprintCopies.Copy(v.Component)};
        internal static EngineeringRefinementProjectDto Copy(EngineeringRefinementProjectDto p)=>new EngineeringRefinementProjectDto{ProjectId=p.ProjectId,ActorId=p.ActorId,Ordinal=p.Ordinal,Goal=p.Goal,ParentVersionId=p.ParentVersionId,RootVersionId=p.RootVersionId,ParentMaturityMilli=p.ParentMaturityMilli,StartedTurn=p.StartedTurn,ResolvedTurn=p.ResolvedTurn,EffectiveWork=p.EffectiveWork,Stage=p.Stage,Context=p.Context==null?null:BlueprintCopies.Copy(p.Context),InstallationId=p.InstallationId,Before=p.Before==null?null:BlueprintCopies.Copy(p.Before),GenerationRoot=p.GenerationRoot==null?null:BlueprintCopies.Copy(p.GenerationRoot),Outcome=p.Outcome==null?null:Copy(p.Outcome),Downside=p.Downside,DrawIdentity=p.DrawIdentity,DecisionTurn=p.DecisionTurn};
        internal static EngineeringRefinementStateDto Copy(EngineeringRefinementStateDto s)=>new EngineeringRefinementStateDto{PolicyId=s.PolicyId,Projects=s.Projects?.Select(Copy).ToList(),Operations=s.Operations?.Select(o=>new EngineeringRefinementOperationDto{BaseJournalOffset=o.BaseJournalOffset,Command=o.Command==null?null:Copy(o.Command)}).ToList()};
    }
}
