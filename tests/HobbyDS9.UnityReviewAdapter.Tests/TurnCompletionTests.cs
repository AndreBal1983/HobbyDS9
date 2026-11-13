using HobbyDS9.Contracts;
using HobbyDS9.UnityReview;
using Xunit;

namespace HobbyDS9.UnityReviewAdapter.Tests;

public sealed class TurnCompletionTests
{
    static ReviewTurnBoundary Frame(int turn=1,long revision=1,long generation=1,string actor="human",SessionPhase phase=SessionPhase.Planning)
        =>new(actor,generation,turn,revision,phase);
    static ReviewCompletionNotice Notice(string id="a")=>new(id,"Completed","Recorded fact");
    static ResearchReceiptSnapshot Receipt(string category,string actor="human",string? application=null,ScienceDiscipline? discipline=null,int ordinal=0,long amount=0)
        =>new(new ResearchReceiptDto{Category=category,CivilizationId=actor,ApplicationDefinitionId=application,Discipline=discipline,IntervalStartLevel=1,BreakthroughOrdinal=ordinal,Amount=amount});
    static readonly Dictionary<string,ReviewEngineeringNoticeDefinition> Definitions=new()
    { ["engineering.advanced-starship-engineering"]=new("Advanced Starship Engineering","Unlocks Advanced Scout production."),
      ["engineering.compact-reactor"]=new("Compact Reactor","A new compact reactor version.") };
    [Fact] public void OnePublishedBoundaryQueuesOnceAndEachAcknowledgmentIsOneUse()
    {
        var queue=new ReviewTurnCompletionQueue();var owner=new object();var before=Frame();var after=Frame(2,2);
        queue.Synchronize(owner,before);var a=Notice();var b=Notice("b");
        Assert.True(queue.Capture(owner,before,after,new[]{a,b,a}));Assert.Equal(2,queue.Count);
        Assert.Same(a,queue.Peek(owner,after));Assert.False(queue.Capture(owner,before,after,new[]{a,b}));
        Assert.True(queue.Acknowledge(owner,after,a));Assert.False(queue.Acknowledge(owner,after,a));
        Assert.Same(b,queue.Peek(owner,after));Assert.True(queue.Acknowledge(owner,after,b));Assert.Null(queue.Peek(owner,after));
    }
    [Fact] public void PendingCombatWaitsForActualPlanningTransition()
    {
        var queue=new ReviewTurnCompletionQueue();var owner=new object();queue.Synchronize(owner,Frame());
        var pending=Frame(1,2,phase:SessionPhase.AwaitingCombat);var next=Frame(2,3);
        Assert.False(queue.Capture(owner,Frame(),pending,new[]{Notice()}));Assert.Null(queue.Peek(owner,pending));
        Assert.True(queue.Capture(owner,pending,next,new[]{Notice()}));Assert.NotNull(queue.Peek(owner,next));
    }
    [Theory][InlineData(1,2)][InlineData(2,1)][InlineData(3,3)]
    public void NoRejectedSameTurnRevisionOrSkippedBoundaryIsAnnounced(int turn,long revision)
    {
        var queue=new ReviewTurnCompletionQueue();var owner=new object();queue.Synchronize(owner,Frame());
        Assert.False(queue.Capture(owner,Frame(),Frame(turn,revision),new[]{Notice()}));
    }
    [Fact] public void ReplacementAndLoadBaselineDiscardTransientNoticesWithoutReplayingReceipts()
    {
        var queue=new ReviewTurnCompletionQueue();var owner=new object();var after=Frame(2,2);
        queue.Synchronize(owner,Frame());var old=Notice();queue.Capture(owner,Frame(),after,new[]{old});
        var loaded=Frame(2,2,2);queue.Synchronize(owner,loaded);Assert.Null(queue.Peek(owner,loaded));
        Assert.False(queue.Acknowledge(owner,after,old));
        var fresh=Notice("fresh");var next=Frame(3,3,2);Assert.True(queue.Capture(owner,loaded,next,new[]{fresh}));
        Assert.False(queue.Capture(owner,Frame(),after,new[]{old}));Assert.Same(fresh,queue.Peek(owner,next));
        var replacement=new object();queue.Synchronize(replacement,next);Assert.Null(queue.Peek(replacement,next));
    }
    [Fact] public void NoCompletionTurnRemainsSilentAndCannotBeRecollectedOnRerender()
    {
        var queue=new ReviewTurnCompletionQueue();var owner=new object();var after=Frame(2,2);queue.Synchronize(owner,Frame());
        Assert.True(queue.Capture(owner,Frame(),after,Array.Empty<ReviewCompletionNotice>()));Assert.Null(queue.Peek(owner,after));
        queue.Synchronize(owner,after);Assert.False(queue.Capture(owner,Frame(),after,new[]{Notice()}));
    }
    [Fact] public void ResearchUsesActualCompletionCategoriesActorAndStableStructuralIdentity()
    {
        var receipts=new[]{Receipt("engineering-spent",amount:30),Receipt("engineering-band",application:"engineering.compact-reactor",amount:3),
            Receipt("engineering-mature",application:"engineering.compact-reactor",amount:24000),
            Receipt("science-level",discipline:ScienceDiscipline.Energy,amount:2),
            Receipt("science-breakthrough",discipline:ScienceDiscipline.Energy,ordinal:1,amount:100),
            Receipt("science-breakthrough",actor:"opponent",discipline:ScienceDiscipline.Weapons,ordinal:1)};
        var facts=ReviewCompletionFacts.Research(receipts,"human",5,Definitions,true);
        Assert.Equal(3,facts.Count);Assert.Contains("discovery 1 reached",facts[0].Detail);Assert.Contains("level 2",facts[1].Detail);
        Assert.Equal("Compact Reactor completed",facts[2].Title);Assert.DoesNotContain(facts,n=>n.Detail.Contains("Weapons"));
        Assert.Equal(facts.Select(n=>n.Identity),ReviewCompletionFacts.Research(receipts.Reverse().Concat(receipts),"human",5,Definitions,true).Select(n=>n.Identity));
    }
    [Fact] public void ScoutCompletionNeverPromisesConstructionUnavailableInThisCampaign()
    {
        var facts=ReviewCompletionFacts.Research(new[]{Receipt("engineering-mature",application:"engineering.advanced-starship-engineering",amount:18000)},"human",2,Definitions,true);
        Assert.Single(facts);Assert.Equal("Advanced Starship Engineering completed",facts[0].Title);
        Assert.Contains("not offered",facts[0].Detail);Assert.DoesNotContain("Unlocks Advanced Scout",facts[0].Detail);
    }
    [Fact] public void ExistingStructureReceiptsFilterOwnerWeekAndQueueIdentity()
    {
        var own=new ActivityStructureReceiptDto{QueueItemId="q.2",OwnerId="human",ColonyId="c",DefinitionId="lab",CompletedTurn=4};
        var rows=new[]{own,own,new(){QueueItemId="enemy",OwnerId="opponent",ColonyId="c",DefinitionId="lab",CompletedTurn=4},
            new(){QueueItemId="old",OwnerId="human",ColonyId="c",DefinitionId="lab",CompletedTurn=3}};
        var result=ReviewCompletionFacts.Structures(rows,"human",4,new Dictionary<string,string>{{"lab","Laboratory"}},new Dictionary<string,string>{{"c","Earth"}});
        Assert.Single(result);Assert.Equal("Laboratory completed",result[0].Title);Assert.Contains("Earth",result[0].Detail);Assert.Equal("c",result[0].ColonyId);
    }
}
