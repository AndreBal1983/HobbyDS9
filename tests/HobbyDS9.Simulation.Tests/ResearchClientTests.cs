using System.Text.Json;
using HobbyDS9.Application;
using HobbyDS9.Contracts;
using HobbyDS9.Simulation;
using HobbyDS9.UnityClient;
using Xunit;
namespace HobbyDS9.Simulation.Tests;
public sealed class ResearchClientTests
{
    private static readonly EntityId Human=new("civ.federation");
    private static string Json(StrategicClient c)=>JsonSerializer.Serialize(c.ExportSave());
    private static StrategicClient Start(ScenarioDto scenario)=>new(CampaignApplication.StartScenario(scenario),save=>CampaignApplication.LoadSave(scenario,save));
    [Fact]
    public void ClientResearchActionsAreStampedAndViewsAreDetachedAcrossReload()
    {
        var scenario=VerticalSliceFixture.Scenario(false,weighted:true);var client=Start(scenario);var old=client.Snapshot;var view=ResearchOverviewSnapshot.For(old,Human);
        Assert.Equal(6,view.Science.Count);Assert.Single(view.Engineering);Assert.Equal(7000,view.ScienceAllocationBps);Assert.False(view.Engineering[0].CanTarget);
        Assert.True(client.AllocateResearch(old,Human,5000).Accepted);var before=Json(client);
        Assert.Equal(CommandErrorCode.StaleRevision,client.FocusScience(old,Human,ScienceDiscipline.Propulsion).ErrorCode);Assert.Equal(before,Json(client));
        Assert.True(client.FocusScience(client.Snapshot,Human,ScienceDiscipline.Propulsion).Accepted);var save=client.ExportSave();client.Load(JsonSerializer.Deserialize<SaveEnvelopeDto>(JsonSerializer.Serialize(save))!);
        var refreshed=ResearchOverviewSnapshot.For(client.Snapshot,Human);Assert.Equal(5000,refreshed.ScienceAllocationBps);Assert.Equal(ScienceDiscipline.Propulsion,refreshed.Focus);Assert.Equal(ScienceDiscipline.Construction,view.Focus);
        Assert.Equal(7000,view.ScienceAllocationBps);Assert.Equal(2,client.Snapshot.Campaign.StateRevision);
    }
    [Fact]
    public void ClientRejectionsProduceTypedErrorFeedbackAndNoMutation()
    {
        var client=Start(VerticalSliceFixture.Scenario(false,weighted:true));var before=Json(client);
        var result=client.Engineering(client.Snapshot,Human,new DefinitionId("engineering.advanced-starship-engineering"));Assert.Equal(CommandErrorCode.EngineeringScienceRequirementMissing,result.ErrorCode);Assert.Equal(LcarsSfx.Error,SfxActionFeedback.For(result));Assert.Equal(before,Json(client));
        result=client.AllocateResearch(client.Snapshot,Human,5000);Assert.True(result.Accepted);Assert.Equal(LcarsSfx.Success,SfxActionFeedback.For(result));
    }
    [Fact]
    public void KnownDiscoveryVectorAndCompleteCostTablePinTheAlgorithm()
    {
        Assert.Equal(new long[]{100,130,170,220,290,380,500,650,850},Enumerable.Range(1,9).Select(level=>ScienceRules.IntervalCost(level,10000)));
        var points=ScienceRules.Schedule(99,ScienceDiscipline.Construction,1,10000);
        Assert.Equal(new[]{2,4,1,5,3},points.Select(p=>p.Ordinal));Assert.Equal(new long[]{22,22,17,22,17},points.Select(p=>p.ChunkCost));Assert.Equal(new long[]{22,44,61,83,100},points.Select(p=>p.Threshold));
    }
    [Fact]
    public void AiChoosesAdjustedRemainingCostBeforeBaseCost()
    {
        var scenario=VerticalSliceFixture.Scenario(false,weighted:true);var ai=scenario.InitialState.Civilizations!.Single(c=>c.CivilizationId=="civ.cardassian");foreach(var d in ai.Disciplines!)d.Level=2;
        ai.EngineeringProgress!.Add(new(){DefinitionId="engineering.advanced-starship-engineering",Progress=50});
        scenario.Definitions.DefinitionIds.Add("engineering.less-expensive");scenario.Definitions.EngineeringApplications!.Add(new(){DefinitionId="engineering.less-expensive",DisplayName="Fixture",BaseEngineeringCost=12,ScienceRequirements=new(),PrerequisiteDefinitionIds=new()});
        var s=CampaignApplication.StartScenario(scenario);var c=s.CaptureSnapshot().Campaign;var result=s.AdvanceTurn(c.TurnNumber,c.StateRevision);Assert.Equal(TurnStatus.Completed,result.Status);
        var state=s.ExportSave().GameState.Civilizations!.Single(c=>c.CivilizationId=="civ.cardassian");Assert.Equal("engineering.advanced-starship-engineering",state.EngineeringTargetDefinitionId);Assert.Equal(52,Assert.Single(state.EngineeringProgress!).Progress);
        Assert.DoesNotContain(result.CommandResults,r=>!r.Accepted);
    }
    [Fact]
    public void ContentAndDisciplineOrderDoNotChangeResearchProjections()
    {
        var original=VerticalSliceFixture.Scenario(false,weighted:true);var reversed=JsonSerializer.Deserialize<ScenarioDto>(JsonSerializer.Serialize(original))!;
        foreach(var c in reversed.InitialState.Civilizations!)c.Disciplines!.Reverse();foreach(var app in reversed.Definitions.EngineeringApplications!)app.ScienceRequirements!.Reverse();
        var a=Start(original);var b=Start(reversed);Assert.Equal(JsonSerializer.Serialize(ResearchOverviewSnapshot.For(a.Snapshot,Human)),JsonSerializer.Serialize(ResearchOverviewSnapshot.For(b.Snapshot,Human)));
    }
    [Theory]
    [InlineData(CommandKind.SetResearchAllocation)][InlineData(CommandKind.SetScienceFocus)][InlineData(CommandKind.SetEngineeringTarget)]
    public void ResearchCommandsRejectDuringCombatWithoutChangingTheCheckpoint(CommandKind kind)
    {
        var scenario=VerticalSliceFixture.Scenario(humans:true,weighted:true);
        foreach(var f in scenario.InitialState.Fleets!)f.LocationNodeId="nav.bajor";
        var session=CampaignApplication.StartScenario(scenario);var snapshot=session.CaptureSnapshot().Campaign;Assert.Equal(TurnStatus.AwaitingCombat,session.AdvanceTurn(snapshot.TurnNumber,snapshot.StateRevision).Status);
        snapshot=session.CaptureSnapshot().Campaign;var command=new CommandEnvelope{CommandId="cmd.blocked",ActorCivilizationId=Human.Value,ExpectedTurn=snapshot.TurnNumber,ExpectedRevision=snapshot.StateRevision,Kind=kind};
        if(kind==CommandKind.SetResearchAllocation)command.SetResearchAllocation=new(){ScienceAllocationBps=5000};
        if(kind==CommandKind.SetScienceFocus)command.SetScienceFocus=new(){Discipline=ScienceDiscipline.Propulsion};
        if(kind==CommandKind.SetEngineeringTarget)command.SetEngineeringTarget=new(){TargetDefinitionId=null};
        var before=JsonSerializer.Serialize(session.ExportSave());Assert.Equal(CommandErrorCode.CombatPending,session.SubmitCommand(command).ErrorCode);Assert.Equal(before,JsonSerializer.Serialize(session.ExportSave()));
    }
    [Fact]
    public void ExactScienceCrossingReevaluatesApplicationEligibilityWithoutSpendingEngineering()
    {
        var scenario=VerticalSliceFixture.Scenario(humans:true,weighted:true);var h=scenario.InitialState.Civilizations!.Single(c=>c.CivilizationId==Human.Value);
        h.Disciplines!.Single(d=>d.Discipline==ScienceDiscipline.Construction).Level=2;h.ScienceFocus=ScienceDiscipline.Propulsion;h.ScienceAllocationBps=2500;
        long threshold=ScienceRules.Schedule(h.ResearchDiscoverySeed!.Value,ScienceDiscipline.Propulsion,1,10000).Single(p=>p.Ordinal==2).Threshold;
        h.Disciplines!.Single(d=>d.Discipline==ScienceDiscipline.Propulsion).ProgressTowardNextLevel=threshold-1;
        var client=Start(scenario);Assert.False(ResearchOverviewSnapshot.For(client.Snapshot,Human).Engineering.Single().CanTarget);
        Assert.Equal(TurnStatus.Completed,client.EndTurn(client.Snapshot).Status);var view=ResearchOverviewSnapshot.For(client.Snapshot,Human);Assert.True(view.Engineering.Single().CanTarget);Assert.Equal(0,view.Engineering.Single().Progress);
        Assert.True(client.Engineering(client.Snapshot,Human,new DefinitionId("engineering.advanced-starship-engineering")).Accepted);
    }

}
