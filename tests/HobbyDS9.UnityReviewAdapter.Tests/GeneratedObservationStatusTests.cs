using HobbyDS9.Contracts;
using HobbyDS9.UnityReview;
using Xunit;

namespace HobbyDS9.UnityReviewAdapter.Tests;
public sealed class GeneratedObservationStatusTests
{
    static CampaignGeneratedMapDto Map()=>new(){ActorId="civ.human",Regions=new(){new(){NodeId="nav.observed"},new(){NodeId="nav.remembered"},new(){NodeId="nav.charted"}},ObservedNodeIds=new(){"nav.observed","nav.unknown"}};
    static CampaignKnownContact Contact(string node,bool fresh,int turn)=>new(new(){FleetId="fleet.enemy",LastSeenNodeId=node,LastSeenTurn=turn,TrackingNodeIds=new(){node}},fresh);
    [Fact] public void CoverageAndMemoryHaveDifferentMeaningAndHiddenInputsStayHidden()
    {
        var map=Map();var contacts=new[]{Contact("nav.observed",false,2),Contact("nav.remembered",false,3),Contact("nav.unknown",true,7)};
        var status=new GeneratedObservationStatus(map,"civ.human",contacts);
        Assert.Equal("Current sensors · no contacts detected",status.Describe("nav.observed"));
        Assert.Equal("Last seen turn 3 · current presence unknown",status.Describe("nav.remembered"));
        Assert.Equal("Charted · no current sensor coverage",status.Describe("nav.charted"));
        Assert.Null(status.Describe("nav.unknown"));
        Assert.Equal(3,map.Regions.Count);Assert.Equal(2,map.ObservedNodeIds.Count);Assert.Equal(3,contacts.Length);
    }
    [Fact] public void FreshContactOutsideCurrentCoverageCannotBePresentedAsCurrent()
    {
        var status=new GeneratedObservationStatus(Map(),"civ.human",new[]{Contact("nav.remembered",true,8),Contact("nav.observed",true,8)});
        Assert.Equal("Last seen turn 8 · current presence unknown",status.Describe("nav.remembered"));
        Assert.Equal("Current sensors · 1 hostile contact",status.Describe("nav.observed"));
    }
    [Fact] public void ProjectionIsDetachedAndRejectsAnotherActorsMap()
    {
        var map=Map();var status=new GeneratedObservationStatus(map,"civ.human",Array.Empty<CampaignKnownContact>());
        map.ObservedNodeIds.Clear();map.Regions.Clear();
        Assert.Equal("Current sensors · no contacts detected",status.Describe("nav.observed"));
        Assert.Throws<ArgumentException>(()=>new GeneratedObservationStatus(Map(),"civ.other",Array.Empty<CampaignKnownContact>()));
    }
}
