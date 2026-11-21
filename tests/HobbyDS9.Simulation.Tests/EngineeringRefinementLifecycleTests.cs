using System.Text.Json;
using HobbyDS9.Contracts;
using HobbyDS9.Contracts.ShipDesign;
using Xunit;
using static HobbyDS9.Simulation.Tests.ColonisationTransportTests;
namespace HobbyDS9.Simulation.Tests;
public sealed class EngineeringRefinementLifecycleTests
{
    internal static CampaignEngineeringRefinementPackageDto Package()
    {
        var folder=Path.Combine(AppContext.BaseDirectory,"scenarios/refinement-lifecycle-template");Directory.CreateDirectory(folder);
        foreach(var name in new[]{"playable","package","physical","core"})File.Copy(Path.Combine(AppContext.BaseDirectory,"scenarios/playable-fleet-v1",name+".json"),Path.Combine(folder,name+".json"),true);
        File.Copy(Path.Combine(AppContext.BaseDirectory,"scenarios/sol-eight-planets-v1/scenario.json"),Path.Combine(folder,"scenario.json"),true);
        var template=PlayableFleetPackageReader.Read(folder);template.ReplayOperationLimit=65536;
        var activities=CampaignTacticalReviewSession.CreateActivitiesPackage(template,ActivityReadyClassTests.Catalogue(),ActivityReadyClassTests.Ready(),ActivityReadyClassTests.Support(),ActivityReadyClassTests.Read<CampaignActivityContentDto>("activities.json"),2048);
        var colony=CampaignTacticalReviewSession.CreateColonisationPackage(activities,ColonisationContentTests.Read<ShipDesignCatalogueDto>("catalogue.json"),ColonisationContentTests.Read<ShipClassDraftDto[]>("ready-classes.json"),ColonisationContentTests.Read<ColonisationSupportCatalogueDto>("support.json"),ColonisationContentTests.Read<ColonisationPolicyDto>("settlement.json"));
        return CampaignTacticalReviewSession.CreateEngineeringRefinementPackage(CampaignTacticalReviewSession.CreateGrowthPackage(CampaignTacticalReviewSession.CreateColonisationExpansionPackage(CampaignTacticalReviewSession.CreateColonisationRosterPackage(colony))));
    }
    internal static CampaignTacticalReviewSession Fresh()=>CampaignTacticalReviewSession.StartCycleGame(Package(),PublishedConstructionProfileTests.Settings(30));
    static CampaignEngineeringRefinementSaveDto Root(CampaignTacticalReviewSession s)=>Assert.IsType<CampaignEngineeringRefinementSaveDto>(s.ExportCycleSave());
    static EngineeringRefinementCommandDto Command(CampaignTacticalReviewSession s,EngineeringRefinementAction action=EngineeringRefinementAction.Start,string? project=null,EngineeringRefinementGoal goal=EngineeringRefinementGoal.ReduceCapacity,string? parent="component.reactor.v1")
    {var stamp=s.CaptureStatus();return new(){ActorId=Human,CommandId="command.refinement."+stamp.Turn+"."+stamp.CampaignRevision,ExpectedTurn=stamp.Turn,ExpectedRevision=stamp.CampaignRevision,Action=action,ProjectId=project,Goal=goal,ParentVersionId=action==EngineeringRefinementAction.Start?parent:null};}
    static EngineeringRefinementProjectSnapshot Complete(CampaignTacticalReviewSession s)
    {
        if(Game(Root(s).BaseCampaign!).Civilizations!.Single(a=>a.CivilizationId==Human).ScienceAllocationUnits!=0)
        {
            var stamp=s.CaptureStatus();var allocate=s.SubmitStrategicCommand(new(){ActorCivilizationId=Human,CommandId="command.refinement.allocation."+stamp.CampaignRevision,ExpectedTurn=stamp.Turn,ExpectedRevision=stamp.CampaignRevision,Kind=CommandKind.SetResearchAllocation,SetResearchAllocation=new(){ScienceAllocationUnits=0}});
            Assert.True(allocate.Accepted,allocate.Error+" "+s.LastPhysicalFailure);
        }
        var projectId=s.CaptureEngineeringRefinement(Human)!.Projects.Single(p=>p.IsActive).ProjectId;
        for(int i=0;i<32;i++){var p=s.CaptureEngineeringRefinement(Human)!.Projects.Single(p=>p.ProjectId==projectId);if(p.Stage!=EngineeringRefinementStage.Working)return p;Advance(s);}
        throw new InvalidOperationException("Paid refinement did not complete in32 turns.");
    }
    internal static EngineeringRefinementProjectDto MathProject(CampaignTacticalReviewSession s,EngineeringRefinementGoal goal=EngineeringRefinementGoal.ReduceCapacity,string root="component.reactor.v1")
    {
        var game=Game(Root(s).BaseCampaign!);var context=game.Blueprints!.First(x=>x.OwnerCivilizationId==Human&&x.Fit!.Installations!.Any(i=>i.ComponentVersionId==root));
        var component=context.FrozenCatalogue!.Components!.Single(x=>x.VersionId==root);
        return new(){ActorId=Human,ProjectId="engineering.refinement.work.test.1",ParentVersionId=root,RootVersionId=root,Goal=goal,Before=BlueprintCopies.Copy(component),GenerationRoot=BlueprintCopies.Copy(component),Context=BlueprintCopies.Copy(context),InstallationId=context.Fit!.Installations!.First(i=>i.ComponentVersionId==root).InstallationId};
    }
    [Fact] public void ActualPaidCompletionPersistsPendingWithoutChangingExistingHardwareThenDiscardIsTerminal()
    {
        var s=Fresh();var before=Root(s);var frozen=Json(Game(before.BaseCampaign!).Blueprints!);
        var missing=Command(s);missing.ContextBlueprintId="blueprint.missing";missing.ContextBlueprintRevision=1;
        Assert.Equal(EngineeringRefinementError.NoRepresentativeFit,s.PreviewEngineeringRefinement(missing).Error);Assert.False(s.SubmitEngineeringRefinement(missing).Accepted);Assert.Equal(Json(before),Json(Root(s)));
        var command=Command(s);var preview=s.PreviewEngineeringRefinement(command);Assert.True(preview.CanStart,preview.Error.ToString());Assert.Equal(12000,preview.Work);
        Assert.Equal(Json(before),Json(Root(s)));var result=s.SubmitEngineeringRefinement(command);Assert.True(result.Accepted,result.Error+" "+s.LastPhysicalFailure);
        Assert.False(s.SubmitEngineeringRefinement(command).Accepted);Assert.Single(s.CaptureEngineeringRefinement(Human)!.Projects);
        var pending=Complete(s);Assert.Equal(EngineeringRefinementStage.Pending,pending.Stage);Assert.True(pending.CanAccept);Assert.NotNull(pending.Outcome);Assert.True(pending.Outcome!.Capacity<pending.Before.Capacity);Assert.Equal(5000,pending.Outcome.MaturityMilli);
        var root=Root(s);var owner=Game(root.BaseCampaign!).Civilizations!.Single(x=>x.CivilizationId==Human);Assert.True(owner.Statistics!.TotalEngineeringSpent>=pending.Work);
        Assert.DoesNotContain(pending.Outcome.VersionId,s.CaptureIntegratedLibrary(Human).Catalogue!.AvailableVersionIds);Assert.Equal(frozen,Json(Game(root.BaseCampaign!).Blueprints!));
        var wire=Json(root);var loaded=CampaignTacticalReviewSession.LoadCycleSave(Package(),root);Assert.Equal(wire,Json(Root(loaded)));Assert.Equal(pending.Outcome.PowerSupply,loaded.CaptureEngineeringRefinement(Human)!.Projects.Single().Outcome!.PowerSupply);
        var discard=Command(loaded,EngineeringRefinementAction.Discard,pending.ProjectId);Assert.True(loaded.SubmitEngineeringRefinement(discard).Accepted);var discarded=Root(loaded);Assert.Equal(EngineeringRefinementStage.Discarded,discarded.Refinement!.Projects!.Single().Stage);
        Assert.NotNull(discarded.Refinement.Projects!.Single().DrawIdentity);Assert.NotNull(discarded.Refinement.Projects!.Single().Outcome);var terminal=Json(discarded);
        Assert.False(loaded.SubmitEngineeringRefinement(Command(loaded,EngineeringRefinementAction.Accept,pending.ProjectId)).Accepted);
        Assert.False(loaded.SubmitEngineeringRefinement(Command(loaded,EngineeringRefinementAction.Continue,pending.ProjectId)).Accepted);
        Assert.False(loaded.SubmitEngineeringRefinement(Command(loaded,parent:pending.Outcome.VersionId)).Accepted);Assert.Equal(terminal,Json(Root(loaded)));
        Assert.Equal(terminal,Json(Root(CampaignTacticalReviewSession.LoadCycleSave(Package(),discarded))));
    }
    [Fact] public void AcceptReleasesUsableImmutableComponentAndNewBlueprintWithoutUpgradingReadyPublication()
    {
        var s=Fresh();Assert.True(s.SubmitEngineeringRefinement(Command(s)).Accepted);var pending=Complete(s);var before=Json(Game(Root(s).BaseCampaign!).Blueprints!);
        var accept=s.SubmitEngineeringRefinement(Command(s,EngineeringRefinementAction.Accept,pending.ProjectId));Assert.True(accept.Accepted,accept.Error+" "+s.LastPhysicalFailure);
        var library=s.CaptureIntegratedLibrary(Human).Catalogue!;Assert.Contains(pending.Outcome!.VersionId,library.AvailableVersionIds);
        var game=Game(Root(s).BaseCampaign!);var ready=game.Blueprints!.First(b=>b.OwnerCivilizationId==Human&&b.Fit!.Installations!.Any(i=>i.ComponentVersionId=="component.reactor.v1"));
        var draft=BlueprintCopies.Copy(ready.Fit!);draft.Installations!.First(i=>i.ComponentVersionId=="component.reactor.v1").ComponentVersionId=pending.Outcome.VersionId;
        var stamp=s.CaptureStatus();var publish=s.SubmitStrategicCommand(new(){ActorCivilizationId=Human,CommandId="command.refinement.publish",ExpectedTurn=stamp.Turn,ExpectedRevision=stamp.CampaignRevision,Kind=CommandKind.PublishShipBlueprint,PublishShipBlueprint=new(){BlueprintId="blueprint.refinement.actual",Revision=1,Draft=draft}});
        Assert.True(publish.Accepted,publish.Error+" "+s.LastPhysicalFailure);Assert.Equal(before,Json(Game(Root(s).BaseCampaign!).Blueprints!.Where(b=>b.BlueprintId!="blueprint.refinement.actual").ToList()));
        var eligibility=s.CaptureActivities(Human).BuildOptions.Where(b=>b.DefinitionId=="blueprint.refinement.actual").ToList();Assert.NotEmpty(eligibility);
        // A child may use a newly published accepted-parent installation and then
        // continue from that inherited frozen context even though it installs an ancestor.
        var child=Enum.GetValues<EngineeringRefinementGoal>().Select(g=>{var c=Command(s,goal:g,parent:pending.Outcome.VersionId);c.ContextBlueprintId="blueprint.refinement.actual";c.ContextBlueprintRevision=1;return c;}).First(c=>s.PreviewEngineeringRefinement(c).CanStart);
        Assert.True(s.SubmitEngineeringRefinement(child).Accepted);var finishedChild=Complete(s);
        var continuation=Enum.GetValues<EngineeringRefinementGoal>().Select(g=>Command(s,EngineeringRefinementAction.Continue,finishedChild.ProjectId,g)).First(c=>s.PreviewEngineeringRefinement(c).CanStart);
        Assert.True(s.SubmitEngineeringRefinement(continuation).Accepted);
        var root=Root(s);Assert.Equal(Json(root),Json(Root(CampaignTacticalReviewSession.LoadCycleSave(Package(),root))));
    }
    [Fact] public void ContinueIsPaidAtomicRetainsInternalParentAndOlderBranchMaturityRemainsIndependent()
    {
        var s=Fresh();Assert.True(s.SubmitEngineeringRefinement(Command(s)).Accepted);var first=Complete(s);var pending=Json(Root(s));
        var invalid=Command(s,EngineeringRefinementAction.Continue,first.ProjectId,(EngineeringRefinementGoal)999);Assert.False(s.SubmitEngineeringRefinement(invalid).Accepted);Assert.Equal(pending,Json(Root(s)));
        var goals=Enum.GetValues<EngineeringRefinementGoal>();var next=goals.Select(g=>Command(s,EngineeringRefinementAction.Continue,first.ProjectId,g)).First(c=>s.PreviewEngineeringRefinement(c).CanStart);
        Assert.True(s.SubmitEngineeringRefinement(next).Accepted);var snap=s.CaptureEngineeringRefinement(Human)!;Assert.Equal(EngineeringRefinementStage.RetainedInternal,snap.Projects[0].Stage);Assert.Equal(first.Outcome!.MaturityMilli,snap.Projects[1].Before.MaturityMilli);Assert.Equal(first.Work,snap.Projects[1].Work);
        Assert.DoesNotContain(first.Outcome.VersionId,s.CaptureIntegratedLibrary(Human).Catalogue!.AvailableVersionIds);
        var paused=Command(s,EngineeringRefinementAction.Pause,snap.Projects[1].ProjectId);Assert.True(s.SubmitEngineeringRefinement(paused).Accepted);Assert.True(s.CaptureEngineeringRefinement(Human)!.Projects[1].CanResume);
        Assert.True(s.SubmitEngineeringRefinement(Command(s,parent:"component.reactor.v1")).Accepted);
        var other=s.CaptureEngineeringRefinement(Human)!.Projects.Last();Assert.False(s.CaptureEngineeringRefinement(Human)!.Projects[1].CanResume);
        Assert.False(s.SubmitEngineeringRefinement(Command(s,EngineeringRefinementAction.Resume,snap.Projects[1].ProjectId)).Accepted);
        Assert.True(s.SubmitEngineeringRefinement(Command(s,EngineeringRefinementAction.Pause,other.ProjectId)).Accepted);
        Assert.True(s.CaptureEngineeringRefinement(Human)!.Projects[1].CanResume);
        Assert.True(s.SubmitEngineeringRefinement(Command(s,EngineeringRefinementAction.Resume,snap.Projects[1].ProjectId)).Accepted);var second=Complete(s);Assert.True(second.Outcome!.MaturityMilli>first.Outcome.MaturityMilli);
        Assert.True(s.SubmitEngineeringRefinement(Command(s,EngineeringRefinementAction.Discard,second.ProjectId)).Accepted);
        Assert.True(s.SubmitEngineeringRefinement(Command(s,parent:"component.reactor.v1")).Accepted);Assert.Equal(0,s.CaptureEngineeringRefinement(Human)!.Projects.Last().Before.MaturityMilli);
        Assert.True(s.SubmitEngineeringRefinement(Command(s,EngineeringRefinementAction.Accept,first.ProjectId)).Accepted);Assert.Contains(first.Outcome.VersionId,s.CaptureIntegratedLibrary(Human).Catalogue!.AvailableVersionIds);
        var root=Root(s);Assert.Equal(Json(root),Json(Root(CampaignTacticalReviewSession.LoadCycleSave(Package(),root))));
    }
    [Fact] public void ContextualIntegerOutcomesKeepServiceRealDetractorsAndSeventyThirtyCategory()
    {
        var s=Fresh();var p=MathProject(s);var options=EngineeringRefinementMath.Enumerate(p);Assert.True(options.Feasible);
        Assert.All(options.Clean,c=>{Assert.True(c.Component.Capacity<p.Before!.Capacity);Assert.Null(c.Downside);Assert.Equal(p.Before.PowerSupply,c.Component.PowerSupply);Assert.Equal(p.Before.Cost!.Industry,c.Component.Cost!.Industry);});
        Assert.All(options.Mixed,c=>{Assert.True(c.Component.Capacity<p.Before!.Capacity);Assert.NotNull(c.Downside);Assert.True(c.Component.PowerSupply<p.Before.PowerSupply||c.Component.Cost!.Industry>p.Before.Cost!.Industry);});
        var clean=0;for(int i=0;i<200;i++){p.ProjectId="engineering.refinement.work.test."+i;var c=EngineeringRefinementMath.Resolve(p,"seed",out var draw);Assert.Equal(64,draw.Length);if(c.Downside==null)clean++;Assert.Equal(Json(c.Component),Json(EngineeringRefinementMath.Resolve(p,"seed",out _).Component));}Assert.InRange(clean,110,170);
        Assert.Equal((1,1),EngineeringRefinementMath.Factor(100000));Assert.Equal((25000,50000),EngineeringRefinementMath.Factor(125000));Assert.Equal(2500,EngineeringRefinementMath.MaturityGain(125000));Assert.Equal(0,EngineeringRefinementMath.MaturityGain(150000));
        p.ParentMaturityMilli=150000;Assert.False(EngineeringRefinementMath.Enumerate(p).Feasible);
    }
}
