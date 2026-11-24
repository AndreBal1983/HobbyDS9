using System;
using System.Linq;
using HobbyDS9.Application;
using HobbyDS9.Contracts;
using Xunit;

namespace HobbyDS9.RepositoryTestFixtures;

internal static class CycleRepositoryTestProgress
{
    public static void MineAndRefine(ICampaignColonisationCycleSession session, string actor)
    {
        var refinement = (ICampaignEngineeringRefinementSession)session;
        var belts = (ICampaignBeltBodiesSession)session;
        var stamp = session.CaptureStatus();
        Assert.True(refinement.SubmitEngineeringRefinement(new()
        {
            ActorId = actor, CommandId = "command.repository.refinement",
            ExpectedTurn = stamp.Turn, ExpectedRevision = stamp.CampaignRevision,
            Action = EngineeringRefinementAction.Start, ParentVersionId = "component.reactor.v1",
            Goal = EngineeringRefinementGoal.ReduceCapacity
        }).Accepted);

        stamp = session.CaptureStatus();
        Assert.True(session.SubmitStrategicCommand(new()
        {
            ActorCivilizationId = actor, CommandId = $"command.repository.allocate.{stamp.Turn}.{stamp.CampaignRevision}",
            ExpectedTurn = stamp.Turn, ExpectedRevision = stamp.CampaignRevision,
            Kind = CommandKind.SetResearchAllocation, SetResearchAllocation = new() { ScienceAllocationUnits = 0 }
        }).Accepted);

        var body = belts.CaptureBeltBodies(actor)!.Bodies.Single(b => b.FeatureId == CampaignAuthoredSolIdentity.FeatureId);
        stamp = session.CaptureStatus();
        Assert.True(belts.SubmitBeltCommand(new()
        {
            ActorId = actor, CommandId = "command.repository.survey",
            ExpectedTurn = stamp.Turn, ExpectedRevision = stamp.CampaignRevision,
            Action = BeltBodyAction.StartSurvey, BodyId = body.BodyId, ShipId = body.SurveyShipIds.First()
        }).Accepted);

        Advance(session);
        Advance(session);
        stamp = session.CaptureStatus();
        Assert.True(belts.SubmitBeltCommand(new()
        {
            ActorId = actor, CommandId = "command.repository.mine",
            ExpectedTurn = stamp.Turn, ExpectedRevision = stamp.CampaignRevision,
            Action = BeltBodyAction.EnqueueMine, BodyId = body.BodyId, ColonyId = "colony.sol"
        }).Accepted);

        for (var i = 0; i < 64; ++i)
        {
            var root = Assert.IsType<CampaignBeltBodiesSaveDto>(session.ExportCycleSave());
            if (Assert.Single(root.BeltBodies!.Mines!).Stage != BeltMineStage.Queued)
                break;
            Advance(session);
        }
        Advance(session);
    }

    public static string AssertMineAndRefinement(CampaignBeltBodiesSaveDto root)
    {
        Assert.True(Assert.Single(root.BeltBodies!.Mines!).TotalExtracted > 0);
        Assert.NotEmpty(root.BeltBodies.LastShipments!);
        var project = Assert.Single(root.Refinement!.Projects!);
        Assert.Equal(EngineeringRefinementStage.Pending, project.Stage);
        Assert.NotEmpty(root.Refinement.Operations!);
        Assert.NotEmpty(root.BeltBodies.Operations!);
        Assert.NotEmpty(root.BaseCampaign!.Journal!);
        return project.ProjectId;
    }

    private static void Advance(ICampaignColonisationCycleSession session)
    {
        var stamp = session.CaptureStatus();
        Assert.True(session.AdvanceTurn(stamp.Turn, stamp.CampaignRevision).Accepted);
    }
}
