using HobbyDS9.Contracts;
using Xunit;
using static HobbyDS9.Simulation.Tests.ColonisationCycleTests;

namespace HobbyDS9.Simulation.Tests;

public sealed class ColonisationCycleReplayExportTests
{
    [Theory]
    [InlineData("queue")]
    [InlineData("loaded")]
    [InlineData("founding")]
    [InlineData("next-real")]
    public void CompleteReplayAndRepeatedOwnViewsRetainTheExactWholeRoot(string checkpoint)
    {
        var root = Root(checkpoint);
        var expected = Wire(root);
        var loaded = CampaignTacticalReviewSession.LoadCycleSave(Package(), root);
        for (var n = 0; n < 2; n++)
        {
            loaded.CaptureColonisation("civ.federation");
            loaded.CaptureActivities("civ.federation");
            loaded.CaptureStatus();
            Assert.Equal(expected, Wire(root));
            Assert.Equal(expected, Wire(loaded.ExportCycleSave()));
        }
    }

    [Fact]
    public void BaseAndOuterOnlyTamperingFailAtTheirCompleteReplayComparisons()
    {
        var baseOnly = Root("next-real");
        var ship = baseOnly.BaseCampaign!.Campaign!.Integrated!.Physical!.Core.Campaign.GameState.Ships!
            .First(s => s.OwnerCivilizationId == "civ.federation" && s.CurrentHull > 1);
        ship.CurrentHull--;
        var baseBefore = Wire(baseOnly);
        var baseError = Assert.Throws<ArgumentException>(() => CampaignTacticalReviewSession.LoadCycleSave(Package(), baseOnly));
        Assert.Equal("Colonisation checkpoint differs from complete ordered operation replay.", baseError.Message);
        Assert.Equal(baseBefore, Wire(baseOnly));

        var outerOnly = Root("next-real");
        outerOnly.AiPlans![0].HoldReason = "Different retained hold reason";
        var outerBefore = Wire(outerOnly);
        var outerError = Assert.Throws<ArgumentException>(() => CampaignTacticalReviewSession.LoadCycleSave(Package(), outerOnly));
        Assert.Equal("Cycle differs from complete ordered replay.", outerError.Message);
        Assert.Equal(outerBefore, Wire(outerOnly));
    }

    [Fact]
    public void LoadedInputAndReturnedExportsDoNotRetainNestedBaseOrOuterAliases()
    {
        var root = Root("next-real");
        var expected = Wire(root);
        var loaded = CampaignTacticalReviewSession.LoadCycleSave(Package(), root);
        MutateNestedTrees(root);
        Assert.NotEqual(expected, Wire(root));
        Assert.Equal(expected, Wire(loaded.ExportCycleSave()));

        var exported = loaded.ExportCycleSave();
        MutateNestedTrees(exported);
        Assert.NotEqual(expected, Wire(exported));
        loaded.CaptureColonisation("civ.federation");
        loaded.CaptureActivities("civ.federation");
        Assert.Equal(expected, Wire(loaded.ExportCycleSave()));
    }

    private static void MutateNestedTrees(CampaignColonisationCycleSaveDto root)
    {
        root.BaseCampaign!.Map!.Settings!.Map!.Seed = "detached-input-mutation";
        root.BaseCampaign.Campaign!.Integrated!.Physical!.Core.Campaign.GameState.Ships![0].CurrentHull = 0;
        root.BaseCampaign.Journal!.Clear();
        root.AiPlans![0].HoldReason = "detached-plan-mutation";
        root.CycleActors![0].NextOrdinal++;
        root.CycleActors[0].Receipts!.Clear();
    }
}
