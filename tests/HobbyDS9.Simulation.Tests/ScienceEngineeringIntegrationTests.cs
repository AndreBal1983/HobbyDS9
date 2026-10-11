using System.Text.Json;
using HobbyDS9.Application;
using HobbyDS9.Contracts;
using HobbyDS9.Simulation;
using Xunit;
namespace HobbyDS9.Simulation.Tests;
public sealed class ScienceEngineeringIntegrationTests
{
    private static ScenarioDto Scenario()
    { var s=VerticalSliceFixture.Scenario(humans:true,weighted:true);return s; }
    private static CivilizationStateDto Human(ScenarioDto s)=>s.InitialState.Civilizations!.Single(c=>c.CivilizationId=="civ.federation");
    private static CommandEnvelope Command(ICampaignSession s,CommandKind kind,string id="cmd.research")
    { var c=s.CaptureSnapshot().Campaign;return new(){CommandId=id,ActorCivilizationId="civ.federation",ExpectedTurn=c.TurnNumber,ExpectedRevision=c.StateRevision,Kind=kind}; }
    private static string Json(ICampaignSession s)=>JsonSerializer.Serialize(s.ExportSave());
    private static void Advance(ICampaignSession s){var c=s.CaptureSnapshot().Campaign;Assert.Equal(TurnStatus.Completed,s.AdvanceTurn(c.TurnNumber,c.StateRevision).Status);}
    [Theory]
    [InlineData(0)][InlineData(1)][InlineData(3333)][InlineData(10000)]
    public void SplitConservesOutputAndUsesFloorPerCivilization(int allocation)
    {
        var scenario=Scenario();Human(scenario).ScienceAllocationBps=allocation;Human(scenario).ScienceFocus=null;
        var s=CampaignApplication.StartScenario(scenario);Advance(s);var state=s.ExportSave().GameState.Civilizations!.Single(c=>c.CivilizationId=="civ.federation");
        var stats=state.Statistics!;Assert.True(stats.TotalResearchGenerated>0);Assert.Equal(stats.TotalResearchGenerated*allocation/10000,stats.TotalScienceAllocated);
        Assert.Equal(stats.TotalResearchGenerated,stats.TotalScienceAllocated+stats.TotalEngineeringAllocated);Assert.Equal(stats.TotalScienceAllocated,state.ScienceReserve);Assert.Equal(stats.TotalEngineeringAllocated,state.EngineeringReserve);
    }
    [Theory]
    [InlineData(-1,CommandErrorCode.InvalidResearchAllocation)][InlineData(10001,CommandErrorCode.InvalidResearchAllocation)][InlineData(7000,CommandErrorCode.ResearchAllocationUnchanged)]
    public void AllocationRejectionsAreByteIdenticalAndDoNotReserveIds(int value,CommandErrorCode error)
    {
        var s=CampaignApplication.StartScenario(Scenario());var c=Command(s,CommandKind.SetResearchAllocation);c.SetResearchAllocation=new(){ScienceAllocationBps=value};var before=Json(s);
        Assert.Equal(error,s.SubmitCommand(c).ErrorCode);Assert.Equal(before,Json(s));c.SetResearchAllocation.ScienceAllocationBps=5000;Assert.True(s.SubmitCommand(c).Accepted);Assert.Equal(1,s.CaptureSnapshot().Campaign.StateRevision);
    }
    [Theory]
    [InlineData(0,CommandErrorCode.InvalidScienceDiscipline)][InlineData(7,CommandErrorCode.InvalidScienceDiscipline)][InlineData(3,CommandErrorCode.ScienceFocusUnchanged)]
    public void FocusRejectionsPreserveEverything(int value,CommandErrorCode code)
    {
        var s=CampaignApplication.StartScenario(Scenario());var c=Command(s,CommandKind.SetScienceFocus);c.SetScienceFocus=new(){Discipline=(ScienceDiscipline)value};var before=Json(s);
        Assert.Equal(code,s.SubmitCommand(c).ErrorCode);Assert.Equal(before,Json(s));
    }
    [Fact]
    public void MaxFocusIsRejectedAndScienceBanksWithoutRedirectingAllocation()
    {
        var scenario=Scenario();var h=Human(scenario);h.Disciplines!.Single(d=>d.Discipline==ScienceDiscipline.Construction).Level=10;
        var s=CampaignApplication.StartScenario(scenario);var c=Command(s,CommandKind.SetScienceFocus);c.SetScienceFocus=new(){Discipline=ScienceDiscipline.Construction};var before=Json(s);
        Assert.Equal(CommandErrorCode.ScienceFocusMaxed,s.SubmitCommand(c).ErrorCode);Assert.Equal(before,Json(s));Advance(s);
        var r=s.CaptureSnapshot().Campaign.Civilizations.Single(c=>c.CivilizationId.Value=="civ.federation").Research!;
        Assert.True(r.ScienceReserve>0);Assert.Equal(0,r.Statistics.TotalScienceSpent);Assert.True(r.EngineeringReserve>0);
    }
    [Theory]
    [InlineData(14,23,EngineeringMaturityBand.Development)][InlineData(15,25,EngineeringMaturityBand.Prototype)][InlineData(29,48,EngineeringMaturityBand.Prototype)]
    [InlineData(30,50,EngineeringMaturityBand.Operational)][InlineData(59,98,EngineeringMaturityBand.Operational)][InlineData(60,100,EngineeringMaturityBand.Mature)]
    public void MaturityFloorAndBandBoundaries(long progress,int maturity,EngineeringMaturityBand band)
    { Assert.Equal(maturity,EngineeringRules.Maturity(progress,60));Assert.Equal(band,EngineeringRules.Band(maturity)); }
    [Fact]
    public void EngineeringProgressPersistsAcrossRetargetAndOnlyMatureUnlocksDesign()
    {
        var scenario=Scenario();var h=Human(scenario);foreach(var d in h.Disciplines!)d.Level=2;h.EngineeringReserve=30;
        var s=CampaignApplication.StartScenario(scenario);var target=Command(s,CommandKind.SetEngineeringTarget);target.SetEngineeringTarget=new(){TargetDefinitionId="engineering.advanced-starship-engineering"};Assert.True(s.SubmitCommand(target).Accepted);Advance(s);
        var snapshot=s.CaptureSnapshot();var view=ResearchOverviewSnapshot.For(snapshot,new EntityId("civ.federation"));var app=Assert.Single(view.Engineering);
        Assert.Equal(EngineeringMaturityBand.Operational,app.Band);Assert.Empty(snapshot.Campaign.Civilizations.Single(c=>c.CivilizationId.Value=="civ.federation").MatureEngineeringApplicationDefinitionIds);
        Assert.Equal(CommandErrorCode.DesignEngineeringPrerequisiteMissing,StrategicOptions.Production(snapshot,new EntityId("civ.federation"),new EntityId("colony.sol")).Single(d=>d.Design.DefinitionId.Value=="ship-design.advanced-scout").UnavailableReason);
        target=Command(s,CommandKind.SetEngineeringTarget,"cmd.clear");target.SetEngineeringTarget=new(){TargetDefinitionId=null};Assert.True(s.SubmitCommand(target).Accepted);
        var save=s.ExportSave();s=CampaignApplication.LoadSave(scenario,JsonSerializer.Deserialize<SaveEnvelopeDto>(JsonSerializer.Serialize(save))!);Assert.Equal(app.Progress,Assert.Single(ResearchOverviewSnapshot.For(s.CaptureSnapshot(),new EntityId("civ.federation")).Engineering).Progress);
        target=Command(s,CommandKind.SetEngineeringTarget,"cmd.resume");target.SetEngineeringTarget=new(){TargetDefinitionId="engineering.advanced-starship-engineering"};Assert.True(s.SubmitCommand(target).Accepted);
        for(int i=0;i<20;i++)Advance(s);
        view=ResearchOverviewSnapshot.For(s.CaptureSnapshot(),new EntityId("civ.federation"));app=Assert.Single(view.Engineering);Assert.Equal(60,app.Progress);Assert.Equal(100,app.MaturityPercent);Assert.Null(view.Target);Assert.True(view.EngineeringReserve>0);
        Assert.Single(s.CaptureSnapshot().Campaign.Civilizations.Single(c=>c.CivilizationId.Value=="civ.federation").MatureEngineeringApplicationDefinitionIds);
        Assert.True(StrategicOptions.Production(s.CaptureSnapshot(),new EntityId("civ.federation"),new EntityId("colony.sol")).Single(d=>d.Design.DefinitionId.Value=="ship-design.advanced-scout").CanEnqueue);
    }
    [Fact]
    public void RequirementUsesStructuralOrdinalAndHigherLevelsSatisfyPreviousIntervals()
    {
        var state=ScienceCoreTests.Initial(99);var requirement=new EngineeringScienceRequirementDto{Discipline=ScienceDiscipline.Propulsion,IntervalStartLevel=1,RequiredBreakthroughOrdinal=2};var d=state.Disciplines!.Single(d=>d.Discipline==requirement.Discipline);
        var threshold=ScienceRules.Schedule(99,requirement.Discipline,1,10000).Single(p=>p.Ordinal==2).Threshold;
        d.ProgressTowardNextLevel=threshold-1;Assert.False(EngineeringRules.ScienceSatisfied(state,requirement,10000));d.ProgressTowardNextLevel=threshold;Assert.True(EngineeringRules.ScienceSatisfied(state,requirement,10000));d.Level=2;d.ProgressTowardNextLevel=0;Assert.True(EngineeringRules.ScienceSatisfied(state,requirement,10000));
    }
    [Fact]
    public void NestedSnapshotsReceiptsAndContentAreDetachedReadOnlyAndRenderingIsPure()
    {
        var scenario=Scenario();var s=CampaignApplication.StartScenario(scenario);var old=s.CaptureSnapshot();Advance(s);var snap=s.CaptureSnapshot();var before=Json(s);
        var view=ResearchOverviewSnapshot.For(snap,new EntityId("civ.federation"));Assert.Equal(6,view.Science.Count);Assert.Single(view.Engineering);Assert.NotEmpty(view.Receipts);Assert.Equal(before,Json(s));
        Assert.Throws<NotSupportedException>(()=>((IList<ScienceCardSnapshot>)view.Science).Clear());Assert.Throws<NotSupportedException>(()=>((IList<ScienceBreakpointSnapshot>)view.Science[0].Breakpoints).Clear());Assert.Throws<NotSupportedException>(()=>((IList<ResearchReceiptSnapshot>)view.Receipts).Clear());
        Human(scenario).Disciplines!.Clear();scenario.Definitions.EngineeringApplications![0].ScienceRequirements!.Clear();Assert.Equal(before,Json(s));Assert.All(old.Campaign.Civilizations.Select(c=>c.Research!),r=>Assert.Equal(0,r.Statistics.TotalResearchGenerated));
    }
    [Theory]
    [InlineData("seed")][InlineData("allocation")][InlineData("progress")][InlineData("statistics")][InlineData("receipts")]
    public void FailedResearchPhaseRollsBackAllResearchState(string field)
    {
        var scenario=Scenario();var session=new SimulationSession(VerticalSliceFixture.Session(scenario).ExportSave(),scenario.Definitions,VerticalSliceFixture.Compatibility(scenario),new Dictionary<CommandKind,ICommandHandler>(),(phase,staging)=>
        {
            if(phase!=StrategicTurnPhase.Research)return true;var h=staging.Civilizations!.Single(c=>c.CivilizationId=="civ.federation");
            if(field=="seed")h.ResearchDiscoverySeed=100;if(field=="allocation")h.ScienceAllocationBps=0;if(field=="progress")h.Disciplines![0].ProgressTowardNextLevel=0;if(field=="statistics")h.Statistics!.TotalResearchGenerated=999;if(field=="receipts")staging.LastResearchReceipts!.Clear();return false;
        });var before=JsonSerializer.Serialize(session.ExportSave());var c=session.CaptureSnapshot();Assert.Equal(TurnStatus.Rejected,session.AdvanceTurn(c.TurnNumber,c.StateRevision).Status);Assert.Equal(before,JsonSerializer.Serialize(session.ExportSave()));
    }
    [Fact]
    public void OldSaveAndContentAreRejectedWithoutReplacingLiveCampaign()
    {
        var scenario=Scenario();var session=CampaignApplication.StartScenario(scenario);var before=Json(session);var save=session.ExportSave();save.SaveFormatVersion=12;
        Assert.Throws<ArgumentException>(()=>CampaignApplication.LoadSave(scenario,save));Assert.Equal(before,Json(session));scenario.Definitions.ContentFormatVersion=8;Assert.Throws<ArgumentException>(()=>CampaignApplication.StartScenario(scenario));Assert.Equal(before,Json(session));
    }
    [Theory]
    [InlineData("missing-seed")][InlineData("missing-disciplines")][InlineData("duplicate-disciplines")][InlineData("missing-statistics")]
    [InlineData("negative-science")][InlineData("invalid-pace")][InlineData("progress-at-threshold-cost")][InlineData("max-progress")]
    [InlineData("unknown-application")][InlineData("immature-unlock")][InlineData("missing-mature-unlock")][InlineData("unknown-receipt")][InlineData("overflow-cost")]
    public void MalformedResearchSaveIsRejectedWithoutRepair(string error)
    {
        var scenario=Scenario();var h=Human(scenario);
        switch(error)
        {
            case "missing-seed":h.ResearchDiscoverySeed=null;break;
            case "missing-disciplines":h.Disciplines=null;break;
            case "duplicate-disciplines":h.Disciplines![0].Discipline=h.Disciplines[1].Discipline;break;
            case "missing-statistics":h.Statistics=null;break;
            case "negative-science":h.ScienceReserve=-1;break;
            case "invalid-pace":scenario.InitialState.ResearchPaceFactor=10001;break;
            case "progress-at-threshold-cost":h.Disciplines![0].ProgressTowardNextLevel=100;break;
            case "max-progress":h.Disciplines![0].Level=10;h.Disciplines[0].ProgressTowardNextLevel=1;break;
            case "unknown-application":h.EngineeringProgress!.Add(new(){DefinitionId="engineering.missing",Progress=1});break;
            case "immature-unlock":h.MatureEngineeringApplicationDefinitionIds!.Add("engineering.advanced-starship-engineering");break;
            case "missing-mature-unlock":h.EngineeringProgress!.Add(new(){DefinitionId="engineering.advanced-starship-engineering",Progress=60});break;
            case "unknown-receipt":scenario.InitialState.LastResearchReceipts!.Add(new(){CivilizationId="civ.federation",Category="invented"});break;
            case "overflow-cost":scenario.InitialState.ResearchPaceFactor=30000;scenario.Definitions.EngineeringApplications![0].BaseEngineeringCost=long.MaxValue;break;
        }
        var before=JsonSerializer.Serialize(scenario);Assert.NotEmpty(CampaignApplication.ValidateScenario(scenario));Assert.Throws<ArgumentException>(()=>CampaignApplication.StartScenario(scenario));Assert.Equal(before,JsonSerializer.Serialize(scenario));
    }
    [Fact]
    public void AiRetainsAuthoredAllocationAndChoosesLowestScienceWithoutSharedRngOrHumanAutomation()
    {
        var scenario=VerticalSliceFixture.Scenario(false,weighted:true);var cardassian=scenario.InitialState.Civilizations!.Single(c=>c.CivilizationId=="civ.cardassian");
        cardassian.ScienceFocus=null;cardassian.Disciplines!.Single(d=>d.Discipline==ScienceDiscipline.Construction).Level=10;
        var s=CampaignApplication.StartScenario(scenario);var c=s.CaptureSnapshot().Campaign;var result=s.AdvanceTurn(c.TurnNumber,c.StateRevision);Assert.Equal(TurnStatus.Completed,result.Status);
        var state=s.ExportSave().GameState;var ai=state.Civilizations!.Single(c=>c.CivilizationId=="civ.cardassian");Assert.Equal(ScienceDiscipline.Biotech,ai.ScienceFocus);Assert.Equal(7000,ai.ScienceAllocationBps);
        Assert.Contains(result.CommandResults,r=>r.CommandId=="ai-command.t0000000001.a00000000.k7.n00000000"&&r.Accepted);
        var human=state.Civilizations!.Single(c=>c.CivilizationId=="civ.federation");Assert.Equal(ScienceDiscipline.Construction,human.ScienceFocus);Assert.Null(human.EngineeringTargetDefinitionId);Assert.Equal(c.RngState.State,s.CaptureSnapshot().Campaign.RngState.State);
    }
    [Theory]
    [InlineData(CommandKind.SetResearchAllocation)][InlineData(CommandKind.SetScienceFocus)][InlineData(CommandKind.SetEngineeringTarget)]
    public void NewCommandsUseStaleDuplicateAndControllerGuards(CommandKind kind)
    {
        var scenario=VerticalSliceFixture.Scenario(false,weighted:true);foreach(var d in Human(scenario).Disciplines!)d.Level=2;
        var s=CampaignApplication.StartScenario(scenario);var command=Command(s,kind);
        if(kind==CommandKind.SetResearchAllocation)command.SetResearchAllocation=new(){ScienceAllocationBps=5000};
        if(kind==CommandKind.SetScienceFocus)command.SetScienceFocus=new(){Discipline=ScienceDiscipline.Propulsion};
        if(kind==CommandKind.SetEngineeringTarget)command.SetEngineeringTarget=new(){TargetDefinitionId="engineering.advanced-starship-engineering"};
        Assert.True(s.SubmitCommand(command).Accepted);var before=Json(s);Assert.Equal(CommandErrorCode.DuplicateCommand,s.SubmitCommand(command).ErrorCode);
        command.CommandId="cmd.stale";Assert.Equal(CommandErrorCode.StaleRevision,s.SubmitCommand(command).ErrorCode);Assert.Equal(before,Json(s));
        command.ExpectedRevision=s.CaptureSnapshot().Campaign.StateRevision;command.ExpectedTurn=0;Assert.Equal(CommandErrorCode.StaleTurn,s.SubmitCommand(command).ErrorCode);Assert.Equal(before,Json(s));command.ExpectedTurn=s.CaptureSnapshot().Campaign.TurnNumber;command.ActorCivilizationId="civ.cardassian";Assert.Equal(CommandErrorCode.ActorNotControlled,s.SubmitCommand(command).ErrorCode);Assert.Equal(before,Json(s));
    }

    [Theory]
    [InlineData(0,EngineeringMaturityBand.Development)][InlineData(24,EngineeringMaturityBand.Development)]
    [InlineData(25,EngineeringMaturityBand.Prototype)][InlineData(49,EngineeringMaturityBand.Prototype)]
    [InlineData(50,EngineeringMaturityBand.Operational)][InlineData(99,EngineeringMaturityBand.Operational)][InlineData(100,EngineeringMaturityBand.Mature)]
    public void ExactPercentBandsArePinned(int percent,EngineeringMaturityBand band)
    {Assert.Equal(percent,EngineeringRules.Maturity(percent,100));Assert.Equal(band,EngineeringRules.Band(percent));}
    [Theory]
    [InlineData("seed")][InlineData("allocation")][InlineData("focus")][InlineData("progress")][InlineData("statistics")][InlineData("receipts")]
    public void OtherPhasesCannotMutateResearch(string field)
    {
        var scenario=Scenario();var s=new SimulationSession(VerticalSliceFixture.Session(scenario).ExportSave(),scenario.Definitions,VerticalSliceFixture.Compatibility(scenario),new Dictionary<CommandKind,ICommandHandler>(),(phase,state)=>
        {
            if(phase!=StrategicTurnPhase.Economy)return true;var h=state.Civilizations!.Single(c=>c.CivilizationId=="civ.federation");
            if(field=="seed")h.ResearchDiscoverySeed=42;if(field=="allocation")h.ScienceAllocationBps=5000;if(field=="focus")h.ScienceFocus=ScienceDiscipline.Propulsion;
            if(field=="progress")h.Disciplines![0].ProgressTowardNextLevel=1;if(field=="statistics")h.Statistics!.TotalBreakthroughs=1;
            if(field=="receipts")state.LastResearchReceipts!.Add(new(){CivilizationId="civ.federation",Category="allocation",Amount=1});return true;
        });var before=JsonSerializer.Serialize(s.ExportSave());var stamp=s.CaptureSnapshot();Assert.Throws<InvalidOperationException>(()=>s.AdvanceTurn(stamp.TurnNumber,stamp.StateRevision));Assert.Equal(before,JsonSerializer.Serialize(s.ExportSave()));
    }
    [Theory]
    [InlineData("science-reserve")][InlineData("engineering-reserve")][InlineData("generated")]
    public void ResearchOverflowRollsBackEarlierEconomyAndAllSessionFacts(string field)
    {
        var scenario=Scenario();var h=Human(scenario);if(field=="science-reserve")h.ScienceReserve=long.MaxValue;if(field=="engineering-reserve")h.EngineeringReserve=long.MaxValue;
        if(field=="generated"){h.Statistics!.TotalResearchGenerated=long.MaxValue;h.Statistics.TotalScienceAllocated=long.MaxValue;}
        var s=CampaignApplication.StartScenario(scenario);var before=Json(s);var stamp=s.CaptureSnapshot().Campaign;Assert.Throws<OverflowException>(()=>s.AdvanceTurn(stamp.TurnNumber,stamp.StateRevision));Assert.Equal(before,Json(s));
    }

}
