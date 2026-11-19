using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using HobbyDS9.Contracts;
using HobbyDS9.Contracts.ShipDesign;
using static HobbyDS9.Simulation.CampaignTacticalReviewData;
namespace HobbyDS9.Simulation
{
    public sealed partial class CampaignTacticalReviewSession
    {
        private EngineeringRefinementStateDto? refinement;
        private static DefinitionCatalogDto CopyRefinementDefinitions(DefinitionCatalogDto source)
        {
            var copy=SimulationSession.CopyDefinitions(source);
            // Only equipment, Engineering definitions and access gates are mutable in
            // this edition. The private topology identity is stable across forks.
            copy.NavigationRegions=source.NavigationRegions;copy.NavigationConnections=source.NavigationConnections;copy.Phenomena=source.Phenomena;
            return copy;
        }
        private const string RefinementWorkPrefix="engineering.refinement.work.",RefinementReleasePrefix="engineering.refinement.release.";
        private static readonly string[] RefinementRoots={"component.reactor.v1","component.defiant-reactor.v1"};
        private EngineeringRefinementResult RefinementResult(EngineeringRefinementError error,string? id=null)
            =>new EngineeringRefinementResult(error,state.Campaign.GameState.TurnNumber,state.Campaign.GameState.StateRevision,id);
        private EngineeringRefinementError RefinementHeader(EngineeringRefinementCommandDto? c)
        {
            if(refinement==null||colonisation==null)return EngineeringRefinementError.Unavailable;
            if(c==null||!Enum.IsDefined(typeof(EngineeringRefinementAction),c.Action)||!EntityId.TryParse(c.CommandId,out _)||c.CommandId.StartsWith("ai-command.",StringComparison.Ordinal))return EngineeringRefinementError.InvalidCommand;
            if(Actor(c.ActorId)!=CampaignTacticalError.None)return EngineeringRefinementError.ActorNotControlled;
            var game=state.Campaign.GameState;
            if(CurrentPhase!=SessionPhase.Planning)return EngineeringRefinementError.WrongPhase;
            if(c.ExpectedTurn!=game.TurnNumber)return EngineeringRefinementError.StaleTurn;
            if(c.ExpectedRevision!=game.StateRevision)return EngineeringRefinementError.StaleRevision;
            if(game.AcceptedCommandIds!.Contains(c.CommandId)||state.AcceptedRoundCommands.Contains(c.CommandId,StringComparer.Ordinal))return EngineeringRefinementError.Duplicate;
            if(game.StateRevision==long.MaxValue||refinement.Operations!.Count>=EngineeringRefinementIdentity.MaximumCommands)return EngineeringRefinementError.Capacity;
            return EngineeringRefinementError.None;
        }
        private EngineeringRefinementProjectDto? OwnedProject(string actor,string? id)=>refinement?.Projects!.SingleOrDefault(p=>p.ActorId==actor&&p.ProjectId==id);
        private EngineeringRefinementVersionDto? RetainedVersion(string actor,string? id)=>refinement?.Projects!.Where(p=>p.ActorId==actor&&p.Outcome!=null&&(p.Stage==EngineeringRefinementStage.Released||p.Stage==EngineeringRefinementStage.RetainedInternal)).Select(p=>p.Outcome!).SingleOrDefault(v=>v.VersionId==id);
        private EngineeringRefinementError PrepareRefinement(EngineeringRefinementCommandDto c,out EngineeringRefinementProjectDto? proposed)
        {
            proposed=null;var header=RefinementHeader(c);if(header!=EngineeringRefinementError.None)return header;
            if(c.Action!=EngineeringRefinementAction.Start&&c.Action!=EngineeringRefinementAction.Continue)return EngineeringRefinementError.InvalidDecision;
            if(!Enum.IsDefined(typeof(EngineeringRefinementGoal),c.Goal))return EngineeringRefinementError.UnsupportedGoal;
            var game=state.Campaign.GameState;var owner=game.Civilizations!.Single(a=>a.CivilizationId==c.ActorId);
            EngineeringRefinementProjectDto? origin=null;EngineeringRefinementVersionDto? version=null;string? parent=c.ParentVersionId;
            if(c.Action==EngineeringRefinementAction.Continue)
            {
                origin=OwnedProject(c.ActorId,c.ProjectId);if(origin?.Stage!=EngineeringRefinementStage.Pending||origin.Outcome==null)return EngineeringRefinementError.UnknownOutcome;
                version=origin.Outcome;parent=version.VersionId;
                if(c.ParentVersionId!=null&&c.ParentVersionId!=parent)return EngineeringRefinementError.InvalidCommand;
            }
            else version=RetainedVersion(c.ActorId,parent);
            if(refinement!.Projects!.Count>=EngineeringRefinementIdentity.MaximumProjects)return EngineeringRefinementError.Capacity;
            if(owner.EngineeringTargetDefinitionId!=null)return EngineeringRefinementError.ActiveProject;
            if(refinement.Projects.Any(p=>p.ActorId==c.ActorId&&p.Stage==EngineeringRefinementStage.Pending&&p!=origin))return EngineeringRefinementError.PendingOutcome;
            var rootId=version?.RootVersionId??parent;
            if(rootId==null||!RefinementRoots.Contains(rootId,StringComparer.Ordinal))return EngineeringRefinementError.UnknownParent;
            if(version==null&&!RefinementRoots.Contains(parent,StringComparer.Ordinal))return EngineeringRefinementError.UnknownParent;
            var root=definitions.ShipDesignCatalogue!.Components!.SingleOrDefault(x=>x.VersionId==rootId);
            if(root==null||!CampaignBlueprints.Access(definitions,owner).AvailableVersionIds!.Contains(rootId,StringComparer.Ordinal))return EngineeringRefinementError.GenerationLocked;
            var before=version?.Component??root;var maturity=version?.MaturityMilli??0;
            var context=origin?.Context;
            // A retained result keeps the originally frozen service context by default.
            if(context==null&&version!=null)context=refinement.Projects.Single(p=>p.Outcome?.VersionId==version.VersionId).Context;
            if(c.ContextBlueprintId!=null)
            {
                context=game.Blueprints!.SingleOrDefault(b=>b.OwnerCivilizationId==c.ActorId&&b.BlueprintId==c.ContextBlueprintId&&b.Revision==c.ContextBlueprintRevision);
                if(context==null)return EngineeringRefinementError.NoRepresentativeFit;
            }
            if(context==null)context=game.Blueprints!.Where(b=>b.OwnerCivilizationId==c.ActorId&&b.Fit!.Installations!.Any(i=>i.ComponentVersionId==rootId)).OrderBy(b=>b.BlueprintId,StringComparer.Ordinal).ThenBy(b=>b.Revision).FirstOrDefault();
            if(context==null)return EngineeringRefinementError.NoRepresentativeFit;
            var lineage=new HashSet<string>(StringComparer.Ordinal){rootId,parent!};
            if(version!=null)foreach(var ancestor in RefinementAncestry(version))lineage.Add(ancestor.VersionId);
            var inherited=origin??(version==null?null:refinement.Projects.Single(p=>p.Outcome?.VersionId==version.VersionId));
            var installation=context.Fit!.Installations!.Where(i=>lineage.Contains(i.ComponentVersionId))
                .OrderBy(i=>c.ContextBlueprintId==null&&i.InstallationId==inherited?.InstallationId?0:1)
                .ThenBy(i=>i.InstallationId,StringComparer.Ordinal).FirstOrDefault();
            if(installation==null)return EngineeringRefinementError.NoRepresentativeFit;
            var ordinal=refinement.Projects.Count+1;var project=new EngineeringRefinementProjectDto{
                ProjectId=RefinementWorkPrefix+c.ActorId+"."+ordinal.ToString("D4",CultureInfo.InvariantCulture),ActorId=c.ActorId,Ordinal=ordinal,Goal=c.Goal,
                ParentVersionId=parent!,RootVersionId=rootId,ParentMaturityMilli=maturity,StartedTurn=game.TurnNumber,EffectiveWork=ScienceRules.AdjustCost(EngineeringRefinementIdentity.StandardWork,game.ResearchPaceFactor!.Value),
                Stage=EngineeringRefinementStage.Working,Context=BlueprintCopies.Copy(context),InstallationId=installation.InstallationId,Before=BlueprintCopies.Copy(before!),GenerationRoot=BlueprintCopies.Copy(root)};
            if(!EngineeringRefinementMath.Enumerate(project).Feasible)return EngineeringRefinementError.Exhausted;
            proposed=project;return EngineeringRefinementError.None;
        }
        public EngineeringRefinementPreview PreviewEngineeringRefinement(EngineeringRefinementCommandDto command)
        {
            lock(gate)
            {
                var error=PrepareRefinement(command,out var p);if(p==null)return new EngineeringRefinementPreview(error);
                var options=EngineeringRefinementMath.Enumerate(p);var values=options.Clean.Concat(options.Mixed).Select(x=>EngineeringRefinementMath.Value(x.Component,p.Goal)).ToList();
                return new EngineeringRefinementPreview(error,new EngineeringRefinementVectorSnapshot(p.Before!,p.ParentMaturityMilli),p.EffectiveWork,values.Min(),values.Max(),p.Context!.BlueprintId,p.Context.Revision);
            }
        }
        private void AddManagedDefinition(string id,EngineeringProjectKind kind,string label)
        {
            if(definitions.EngineeringApplications!.Any(a=>a.DefinitionId==id))return;
            definitions.EngineeringApplications!.Add(new EngineeringApplicationDefinitionDto{DefinitionId=id,DisplayName=label,Kind=kind,
                BaseEngineeringCost=kind==EngineeringProjectKind.TargetedRefinement?EngineeringRefinementIdentity.StandardWork:1,
                EffectSummary=kind==EngineeringProjectKind.TargetedRefinement?"Paid targeted reactor experiment; outcome requires review.":"Explicit release authorization; not an Engineering work project.",ScienceRequirements=new List<EngineeringScienceRequirementDto>(),PrerequisiteDefinitionIds=new List<string>()});
            definitions.DefinitionIds.Add(id);definitions.DefinitionIds.Sort(StringComparer.Ordinal);
            definitions.EngineeringApplications.Sort((a,b)=>StringComparer.Ordinal.Compare(a.DefinitionId,b.DefinitionId));
        }
        private void StartRefinement(EngineeringRefinementProjectDto p)
        {
            AddManagedDefinition(p.ProjectId,EngineeringProjectKind.TargetedRefinement,"Reactor development · "+p.Goal);
            var owner=state.Campaign.GameState.Civilizations!.Single(a=>a.CivilizationId==p.ActorId);
            owner.EngineeringProgress!.Add(new EngineeringProgressDto{DefinitionId=p.ProjectId});owner.EngineeringProgress.Sort((a,b)=>StringComparer.Ordinal.Compare(a.DefinitionId,b.DefinitionId));
            owner.EngineeringTargetDefinitionId=p.ProjectId;refinement!.Projects!.Add(p);
        }
        private void ResolveCompletedRefinements(GameStateDto game)
        {
            if(refinement==null)return;
            foreach(var p in refinement.Projects!.Where(p=>p.Stage==EngineeringRefinementStage.Working).OrderBy(p=>p.ProjectId,StringComparer.Ordinal))
            {
                var owner=game.Civilizations!.Single(a=>a.CivilizationId==p.ActorId);
                if(!owner.MatureEngineeringApplicationDefinitionIds!.Contains(p.ProjectId,StringComparer.Ordinal))continue;
                var choice=EngineeringRefinementMath.Resolve(p,generatedMap!.Settings.Map.Seed,out var draw);var component=BlueprintCopies.Copy(choice.Component);
                component.ParentVersionId=p.ParentVersionId;component.VersionId="component.refinement."+p.ActorId+"."+p.Ordinal.ToString("D4",CultureInfo.InvariantCulture);
                component.VersionNumber=checked(definitions.ShipDesignCatalogue!.Components!.Where(c=>c.FamilyId==component.FamilyId).Select(c=>c.VersionNumber)
                    .Concat(refinement.Projects.Where(x=>x.Outcome?.Component?.FamilyId==component.FamilyId).Select(x=>x.Outcome!.Component!.VersionNumber)).Max()+1);
                component.DisplayName=p.Before!.DisplayName+" · development "+p.Ordinal.ToString(CultureInfo.InvariantCulture);
                p.Outcome=new EngineeringRefinementVersionDto{VersionId=component.VersionId,OwnerId=p.ActorId,RootVersionId=p.RootVersionId,ParentVersionId=p.ParentVersionId,
                    MaturityMilli=checked(p.ParentMaturityMilli+EngineeringRefinementMath.MaturityGain(p.ParentMaturityMilli)),Component=component,Stage=EngineeringRefinementStage.Pending};
                p.Downside=choice.Downside;p.DrawIdentity=draw;p.ResolvedTurn=game.TurnNumber;p.Stage=EngineeringRefinementStage.Pending;
            }
        }
        private IEnumerable<EngineeringRefinementVersionDto> RefinementAncestry(EngineeringRefinementVersionDto v)
        {
            var rows=new List<EngineeringRefinementVersionDto>();var current=v;
            for(int i=0;i<EngineeringRefinementIdentity.MaximumProjects;i++)
            {
                rows.Add(current);if(RefinementRoots.Contains(current.ParentVersionId,StringComparer.Ordinal)){rows.Reverse();return rows;}
                current=refinement!.Projects!.Select(p=>p.Outcome).Single(x=>x?.VersionId==current.ParentVersionId)!;
            }
            throw new InvalidOperationException("Bounded refinement ancestry exceeded.");
        }
        private bool CanReleaseRefinement(EngineeringRefinementProjectDto p)
        {
            if(p.Outcome==null||colonisation?.Support.CompilerId!=ColonisationRefinementSupportIdentity.Compiler)return false;
            var catalog=BlueprintCopies.Copy(p.Context!.FrozenCatalogue!);
            foreach(var v in RefinementAncestry(p.Outcome))if(!catalog.Components!.Any(x=>x.VersionId==v.VersionId))catalog.Components!.Add(BlueprintCopies.Copy(v.Component!));
            var draft=BlueprintCopies.Copy(p.Context.Fit!);draft.Installations!.Single(i=>i.InstallationId==p.InstallationId).ComponentVersionId=p.Outcome.VersionId;
            var access=CampaignBlueprints.FrozenAccess(catalog);var publication=CampaignBlueprints.Compiler(catalog).Publish(draft,access,p.Context.BlueprintId,p.Context.Revision);
            return publication.Published&&PublishedTacticalSupportCompiler.EvaluateColonisation(new ShipDesignCatalogueSnapshot(catalog),publication.Blueprint!,access,colonisation.Support).Supported;
        }
        private void ReleaseRefinement(EngineeringRefinementProjectDto p)
        {
            var owner=state.Campaign.GameState.Civilizations!.Single(a=>a.CivilizationId==p.ActorId);
            foreach(var v in RefinementAncestry(p.Outcome!))
            {
                var release=RefinementReleasePrefix+v.OwnerId+"."+v.VersionId.Substring(v.VersionId.LastIndexOf('.')+1);
                if(!definitions.ShipDesignCatalogue!.Components!.Any(c=>c.VersionId==v.VersionId))
                {
                    AddManagedDefinition(release,EngineeringProjectKind.ManagedRelease,"Reactor release authorization");
                    definitions.ShipDesignCatalogue!.Components!.Add(BlueprintCopies.Copy(v.Component!));definitions.ShipDesignCatalogue!.Components!.Sort((a,b)=>StringComparer.Ordinal.Compare(a.VersionId,b.VersionId));
                    definitions.DefinitionIds.Add(v.VersionId);definitions.DefinitionIds.Sort(StringComparer.Ordinal);
                    definitions.ShipDesignVersionAccess!.Add(new ShipDesignVersionAccessDto{VersionId=v.VersionId,RequiredEngineeringApplicationDefinitionIds=new List<string>{release}});
                    definitions.ShipDesignVersionAccess.Sort((a,b)=>StringComparer.Ordinal.Compare(a.VersionId,b.VersionId));
                }
                if(v.VersionId!=p.Outcome!.VersionId)continue;
                // A release marker is an authorization, not another paid experiment or a refund.
                owner.EngineeringProgress!.Add(new EngineeringProgressDto{DefinitionId=release,Progress=ScienceRules.AdjustCost(1,state.Campaign.GameState.ResearchPaceFactor!.Value)});
                owner.EngineeringProgress.Sort((a,b)=>StringComparer.Ordinal.Compare(a.DefinitionId,b.DefinitionId));owner.MatureEngineeringApplicationDefinitionIds!.Add(release);owner.MatureEngineeringApplicationDefinitionIds.Sort(StringComparer.Ordinal);
            }
            p.Stage=EngineeringRefinementStage.Released;p.Outcome!.Stage=p.Stage;p.DecisionTurn=state.Campaign.GameState.TurnNumber;
        }
        public EngineeringRefinementResult SubmitEngineeringRefinement(EngineeringRefinementCommandDto command)
        {
            if(published!=null&&!publishedExecuting)
            {
                EngineeringRefinementResult? value=null;
                var result=PublishedMutation(c=>{value=c.SubmitEngineeringRefinement(command);return c.Result(value.Accepted?CampaignTacticalError.None:CampaignTacticalError.InvalidOrder);});
                return result.Accepted?value!:value!=null&&!value.Accepted?value:RefinementResult(result.Error==CampaignTacticalError.Overflow?EngineeringRefinementError.Capacity:EngineeringRefinementError.InvalidCommand);
            }
            lock(gate)
            {
                var error=RefinementHeader(command);if(error!=EngineeringRefinementError.None)return RefinementResult(error);
                var c=RefinementCopies.Copy(command);var game=state.Campaign.GameState;string? projectId=c.ProjectId;
                if(c.Action==EngineeringRefinementAction.Start||c.Action==EngineeringRefinementAction.Continue)
                {
                    error=PrepareRefinement(c,out var p);if(error!=EngineeringRefinementError.None)return RefinementResult(error);
                    if(c.Action==EngineeringRefinementAction.Continue){var original=OwnedProject(c.ActorId,c.ProjectId)!;original.Stage=EngineeringRefinementStage.RetainedInternal;original.Outcome!.Stage=original.Stage;original.DecisionTurn=game.TurnNumber;}
                    StartRefinement(p!);projectId=p!.ProjectId;
                }
                else
                {
                    var p=OwnedProject(c.ActorId,c.ProjectId);if(p==null)return RefinementResult(EngineeringRefinementError.UnknownOutcome);
                    var owner=game.Civilizations!.Single(a=>a.CivilizationId==c.ActorId);
                    if(c.Action==EngineeringRefinementAction.Accept)
                    {if((p.Stage!=EngineeringRefinementStage.Pending&&p.Stage!=EngineeringRefinementStage.RetainedInternal)||!CanReleaseRefinement(p))return RefinementResult(EngineeringRefinementError.InvalidDecision);ReleaseRefinement(p);}
                    else if(c.Action==EngineeringRefinementAction.Discard)
                    {if(p.Stage!=EngineeringRefinementStage.Pending)return RefinementResult(EngineeringRefinementError.InvalidDecision);p.Stage=EngineeringRefinementStage.Discarded;p.Outcome!.Stage=p.Stage;p.DecisionTurn=game.TurnNumber;}
                    else if(c.Action==EngineeringRefinementAction.Pause)
                    {if(p.Stage!=EngineeringRefinementStage.Working||owner.EngineeringTargetDefinitionId!=p.ProjectId)return RefinementResult(EngineeringRefinementError.InvalidDecision);owner.EngineeringTargetDefinitionId=null;}
                    else if(c.Action==EngineeringRefinementAction.Resume)
                    {if(p.Stage!=EngineeringRefinementStage.Working||owner.EngineeringTargetDefinitionId!=null)return RefinementResult(EngineeringRefinementError.ActiveProject);owner.EngineeringTargetDefinitionId=p.ProjectId;}
                    else return RefinementResult(EngineeringRefinementError.InvalidDecision);
                }
                game.AcceptedCommandIds!.Add(c.CommandId);game.StateRevision++;
                refinement!.Operations!.Add(new EngineeringRefinementOperationDto{BaseJournalOffset=colonisation!.Journal.Count,Command=c});
                ValidateSave(state);return RefinementResult(EngineeringRefinementError.None,projectId);
            }
        }
        public EngineeringRefinementSnapshot? CaptureEngineeringRefinement(string actor)
        {
            lock(gate)
            {
                if(refinement==null)return null;
                Require(Actor(actor)==CampaignTacticalError.None,"Controlled actor required.");var game=state.Campaign.GameState;var owner=game.Civilizations!.Single(a=>a.CivilizationId==actor);
                var access=CampaignBlueprints.Access(definitions,owner).AvailableVersionIds!;
                var parents=definitions.ShipDesignCatalogue!.Components!.Where(c=>RefinementRoots.Contains(c.VersionId,StringComparer.Ordinal)&&access.Contains(c.VersionId,StringComparer.Ordinal))
                    .OrderBy(c=>c.VersionId,StringComparer.Ordinal).Select(c=>new EngineeringRefinementParentSnapshot(c,null,true)).ToList();
                parents.AddRange(refinement.Projects!.Where(p=>p.ActorId==actor&&(p.Stage==EngineeringRefinementStage.Released||p.Stage==EngineeringRefinementStage.RetainedInternal))
                    .Select(p=>new EngineeringRefinementParentSnapshot(p.Outcome!.Component!,p.Outcome.MaturityMilli,p.Stage==EngineeringRefinementStage.Released)));
                var canInteract=CurrentPhase==SessionPhase.Planning&&game.StateRevision<long.MaxValue&&refinement.Operations!.Count<EngineeringRefinementIdentity.MaximumCommands;
                var projects=refinement.Projects.Where(p=>p.ActorId==actor).Select(p=>new EngineeringRefinementProjectSnapshot(p,owner.EngineeringProgress!.Single(x=>x.DefinitionId==p.ProjectId).Progress,owner.EngineeringTargetDefinitionId==p.ProjectId,(p.Stage==EngineeringRefinementStage.Pending||p.Stage==EngineeringRefinementStage.RetainedInternal)&&CanReleaseRefinement(p),owner.EngineeringTargetDefinitionId==null,canInteract)).ToList();
                return new EngineeringRefinementSnapshot(actor,game.TurnNumber,game.StateRevision,parents,projects);
            }
        }
    }
}
