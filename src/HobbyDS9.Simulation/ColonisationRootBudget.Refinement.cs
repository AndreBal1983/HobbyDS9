using System;
using HobbyDS9.Contracts;

namespace HobbyDS9.Simulation
{
    internal static partial class ColonisationRootBudget
    {
        private static (long Visits,long Bytes) RefinementCost(CampaignEngineeringRefinementSaveDto root,ColonisationPolicyDto policy)
        {
            var state=root.Refinement;
            if(state==null||state.PolicyId!=EngineeringRefinementIdentity.Policy||state.Projects==null||state.Operations==null
                ||state.Projects.Count>EngineeringRefinementIdentity.MaximumProjects||state.Operations.Count>EngineeringRefinementIdentity.MaximumCommands
                ||root.BaseCampaign?.Journal==null)Fail("Bounded complete refinement state required.");
            var previous=0;
            foreach(var op in state!.Operations!)
            {
                if(op==null||op.Command==null||op.BaseJournalOffset<previous||op.BaseJournalOffset>root.BaseCampaign!.Journal!.Count)
                    Fail("Ordered bounded refinement operations required.");
                previous=op.BaseJournalOffset;
            }
            return new Counter(policy.MaximumValidationVisits,policy.MaximumCanonicalRootBytes,ColonisationSourceEdition.Growth,true).Refinement(state);
        }
        private sealed partial class Counter
        {
            internal (long Visits,long Bytes) Refinement(EngineeringRefinementStateDto state)
            {
                var initialVisits=remainingVisits;var initialBytes=remainingBytes;
                Count(96);Text(state.PolicyId);Count(2);
                foreach(var p in state.Projects!)
                {
                    Need(p);Count(512);Text(p.ProjectId);Text(p.ActorId);V(p.Ordinal);V((int)p.Goal);Text(p.ParentVersionId);Text(p.RootVersionId);
                    V(p.ParentMaturityMilli);V(p.StartedTurn);V(p.ResolvedTurn);V(p.EffectiveWork);V((int)p.Stage);Text(p.InstallationId);
                    Text(p.DrawIdentity);V(p.DecisionTurn);V(p.Downside.HasValue?(int)p.Downside.Value:0);
                    // The existing fixed visitor accounts for every nested frozen fit,
                    // catalogue, compiled design and component before replay or copying.
                    if(p.Context==null||p.Context.FrozenCatalogue==null||p.Context.Fit==null
                        ||p.Context.FrozenCatalogue.Hulls==null||p.Context.FrozenCatalogue.Hulls.Count>64
                        ||p.Context.FrozenCatalogue.Components==null||p.Context.FrozenCatalogue.Components.Count>512
                        ||p.Context.Fit.Installations==null||p.Context.Fit.Installations.Count>128
                        ||p.Context.Fit.Profiles==null||p.Context.Fit.Profiles.Count>3)Fail("Bounded frozen refinement context required.");
                    foreach(var profile in p.Context.Fit.Profiles)
                        if(profile==null||profile.Allocations==null||profile.Allocations.Count>128)Fail("Bounded refinement allocations required.");
                    V(p.Context);V(p.Before);V(p.GenerationRoot);
                    if(p.Outcome==null)Null();else
                    {
                        var outcome=p.Outcome;Count(192);Text(outcome.VersionId);Text(outcome.OwnerId);Text(outcome.RootVersionId);Text(outcome.ParentVersionId);
                        V(outcome.MaturityMilli);V((int)outcome.Stage);V(outcome.Component);
                    }
                }
                Count(2);
                foreach(var op in state.Operations!)
                {
                    Count(64);V(op.BaseJournalOffset);var c=op.Command!;Count(256);Text(c.ActorId);Text(c.CommandId);V(c.ExpectedTurn);V(c.ExpectedRevision);
                    V((int)c.Action);Text(c.ParentVersionId);V((int)c.Goal);Text(c.ProjectId);Text(c.ContextBlueprintId);V(c.ContextBlueprintRevision);
                }
                return(initialVisits-remainingVisits,initialBytes-remainingBytes);
            }
            private void Text(string? value){if(value!=null&&value.Length>512)Fail("Refinement identity string bound.");V(value);}
        }
    }
}
