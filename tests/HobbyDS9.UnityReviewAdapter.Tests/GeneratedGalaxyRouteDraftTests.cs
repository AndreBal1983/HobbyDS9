using HobbyDS9.UnityReview;
using Xunit;

namespace HobbyDS9.UnityReviewAdapter.Tests;

public sealed class GeneratedGalaxyRouteDraftTests
{
    private static GeneratedGalaxyDraftAnchor Anchor(string id,GeneratedGalaxyDraftAnchorKind kind=GeneratedGalaxyDraftAnchorKind.RegionTransit)=>new(id,kind);
    [Fact] public void ShiftAppendsFormerDestinationAndOrdinaryTargetReplacesEntireQueue()
    {
        var draft=new GeneratedGalaxyRouteDraft("map-one",3);draft.SetStart(Anchor("start"));
        Assert.True(draft.SetTarget(Anchor("first",GeneratedGalaxyDraftAnchorKind.StarLocation),false));
        var detached=draft.Targets;
        Assert.True(draft.SetTarget(Anchor("second"),true));
        Assert.Equal(new[]{"first","second"},draft.Targets.Select(t=>t.NodeId));
        Assert.Equal(GeneratedGalaxyDraftAnchorKind.StarLocation,draft.Targets[0].Kind);
        Assert.Single(detached);
        Assert.True(draft.SetTarget(Anchor("replacement"),false));
        Assert.Equal("replacement",Assert.Single(draft.Targets).NodeId);
        Assert.Equal("start",draft.Start!.NodeId);
    }
    [Fact] public void FullQueueRejectsAppendAtomicallyButStillAllowsReplacement()
    {
        var draft=new GeneratedGalaxyRouteDraft("map",1);draft.SetStart(Anchor("a"));
        Assert.True(draft.SetTarget(Anchor("b"),true));
        Assert.False(draft.SetTarget(Anchor("c"),true));Assert.Equal("b",Assert.Single(draft.Targets).NodeId);
        Assert.True(draft.SetTarget(Anchor("c"),false));Assert.Equal("c",Assert.Single(draft.Targets).NodeId);
    }
    [Fact] public void UndoAndClearRetainExplicitStartAndMapIdentityChangesClearAll()
    {
        var draft=new GeneratedGalaxyRouteDraft("hash-a",3);draft.SetStart(Anchor("same-id"));
        draft.SetTarget(Anchor("b"),false);draft.SetTarget(Anchor("c"),true);
        Assert.True(draft.RemoveLast());Assert.Equal("b",Assert.Single(draft.Targets).NodeId);
        draft.Clear();Assert.Empty(draft.Targets);Assert.False(draft.RemoveLast());Assert.NotNull(draft.Start);
        draft.SetTarget(Anchor("b"),false);draft.ChangeMap("hash-a");Assert.Single(draft.Targets);
        draft.ChangeMap("hash-b");Assert.Null(draft.Start);Assert.Empty(draft.Targets);
    }
    [Fact] public void ExplicitRebaseKeepsTargetsAndNoStartCannotAcceptTarget()
    {
        var draft=new GeneratedGalaxyRouteDraft("map",2);Assert.False(draft.SetTarget(Anchor("b"),false));
        draft.SetStart(Anchor("a"));draft.SetTarget(Anchor("b"),false);draft.SetStart(Anchor("new-start"));
        Assert.Equal("new-start",draft.Start!.NodeId);Assert.Equal("b",Assert.Single(draft.Targets).NodeId);
    }
}
