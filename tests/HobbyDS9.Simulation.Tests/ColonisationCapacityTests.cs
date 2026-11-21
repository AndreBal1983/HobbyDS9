using System.Reflection;
using HobbyDS9.Contracts;
using Xunit;
using static HobbyDS9.Simulation.Tests.ColonisationTransportTests;

namespace HobbyDS9.Simulation.Tests;

public sealed class ColonisationCapacityTests
{
    const BindingFlags Private=BindingFlags.NonPublic|BindingFlags.Instance;
    static GameStateDto Live(CampaignTacticalReviewSession s)=>((CampaignTacticalReviewSaveDto)typeof(CampaignTacticalReviewSession).GetField("state",Private)!.GetValue(s)!).Campaign.GameState;
    static ColonisationPolicyDto Policy(CampaignTacticalReviewSession s)
    {var context=typeof(CampaignTacticalReviewSession).GetField("colonisation",Private)!.GetValue(s)!;return (ColonisationPolicyDto)context.GetType().GetField("Policy",Private)!.GetValue(context)!;}
    [Fact]public void DefensiveStagedAdvanceUsesItsActualRevisionAndAiCannotSpendLastIncrement()
    {
        var s=CampaignTacticalReviewSession.StartColonisation(ColonisationInventoryTests.Package(),PublishedConstructionProfileTests.Settings());
        // Explicit fault-injected counter boundary, not a claimed reachable replay root.
        Live(s).StateRevision=long.MaxValue-1;Advance(s);Assert.Equal(long.MaxValue,s.CaptureStatus().CampaignRevision);
        var before=Json(s.ExportColonisationSave());var f=s.CaptureStatus();Assert.False(s.AdvanceTurn(f.Turn,f.CampaignRevision).Accepted);Assert.Equal(before,Json(s.ExportColonisationSave()));
    }
    [Fact]public void DefensiveLastRevisionIsReservedForActualQueuedCancellation()
    {
        var s=CampaignTacticalReviewSession.StartColonisation(ColonisationInventoryTests.Package(),PublishedConstructionProfileTests.Settings());
        var kit=Command(s,ColonisationAction.EnqueueKit);kit.EnqueueKit=new(){ColonyId="colony.sol"};Assert.True(s.SubmitColonisation(kit).Accepted);
        Live(s).StateRevision=long.MaxValue-1;var before=s.ExportColonisationSave();var f=s.CaptureStatus();
        Assert.False(s.SubmitStrategicCommand(new(){ActorCivilizationId=Human,CommandId="command.colony.last.optional",ExpectedTurn=f.Turn,ExpectedRevision=f.CampaignRevision,
            Kind=CommandKind.SetResearchAllocation,SetResearchAllocation=new(){ScienceAllocationUnits=300}}).Accepted);Assert.Equal(Json(before),Json(s.ExportColonisationSave()));
        Assert.True(s.SubmitActivity(new(){ActorId=Human,CommandId="command.colony.last.cancel",ExpectedTurn=f.Turn,ExpectedRevision=f.CampaignRevision,
            Kind=CampaignActivityCommandKind.CancelProduction,TargetId=before.Production!.Single().QueueItemId}).Accepted,s.LastPhysicalFailure);
        Assert.Equal(long.MaxValue,s.CaptureStatus().CampaignRevision);Assert.Empty(s.ExportColonisationSave().Production!);
    }
    [Theory][InlineData(true)][InlineData(false)]
    public void DefensiveExactRetainedLimitReservesCompletionAndRejectsExcessQueue(bool ship)
    {
        var s=CampaignTacticalReviewSession.StartColonisation(ColonisationInventoryTests.Package(),PublishedConstructionProfileTests.Settings());
        // Only the test lowers the private immutable policy; no public package can
        // claim this alternate policy or load these defensive arithmetic fixtures.
        Policy(s).RecordBudget=2;var f=s.CaptureStatus();
        Assert.True(s.SubmitActivity(new(){ActorId=Human,CommandId="command.colony.exact.record",ExpectedTurn=f.Turn,ExpectedRevision=f.CampaignRevision,
            Kind=ship?CampaignActivityCommandKind.EnqueueShip:CampaignActivityCommandKind.EnqueueStructure,TargetId="colony.sol",
            DefinitionId=ship?"blueprint.ready.defiant-combat":"infrastructure.fabrication-annex.v1",BlueprintRevision=ship?1:0}).Accepted,s.LastPhysicalFailure);
        var before=Json(s.ExportColonisationSave());var excess=Command(s,ColonisationAction.EnqueueKit);excess.EnqueueKit=new(){ColonyId="colony.sol"};Assert.False(s.SubmitColonisation(excess).Accepted);Assert.Equal(before,Json(s.ExportColonisationSave()));
        for(int i=0;i<48&&s.ExportColonisationSave().Production!.Any(q=>q.OwnerId==Human);i++)Advance(s);
        var completed=s.ExportColonisationSave();Assert.DoesNotContain(completed.Production!,q=>q.OwnerId==Human);Assert.Single(completed.ProductionHistory!);
        if(ship)Assert.Single(completed.PublishedBuiltShips!);else Assert.Single(completed.Activities!.Structures!);
    }
    [Fact]public void DefensiveAllocationEdgeRejectsBeforeAnyQueueIdentityOrPayment()
    {
        var s=CampaignTacticalReviewSession.StartColonisation(ColonisationInventoryTests.Package(),PublishedConstructionProfileTests.Settings());
        Live(s).NextEntitySequence=ulong.MaxValue-1;var before=Json(s.ExportColonisationSave());var kit=Command(s,ColonisationAction.EnqueueKit);kit.EnqueueKit=new(){ColonyId="colony.sol"};
        Assert.False(s.SubmitColonisation(kit).Accepted);Assert.Equal(before,Json(s.ExportColonisationSave()));
    }
}
