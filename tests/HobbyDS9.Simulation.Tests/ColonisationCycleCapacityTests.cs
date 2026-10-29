using HobbyDS9.Contracts;
using Xunit;
using static HobbyDS9.Simulation.Tests.ColonisationTransportTests;
using static HobbyDS9.Simulation.Tests.ColonisationCycleTests;
using static HobbyDS9.Simulation.Tests.ColonisationCycleBoundaryTests;
namespace HobbyDS9.Simulation.Tests;
// Exact counter manipulations are private capacity fixtures, not replay-admitted histories.
public sealed class ColonisationCycleCapacityTests
{
    static ColonisationPolicyDto Policy(CampaignTacticalReviewSession s)=>Field<ColonisationPolicyDto>(Context(s),"Policy");
    static bool Capacity(CampaignTacticalReviewSession s)=>(bool)Call(s,"ColonisationCapacity",0,null,0)!;
    static int Edge(CampaignTacticalReviewSession s){var p=Policy(s);var old=p.RecordBudget;int lo=0,hi=old;while(lo<hi){var mid=lo+(hi-lo)/2;p.RecordBudget=mid;if(Capacity(s))hi=mid;else lo=mid+1;}p.RecordBudget=old;return lo;}
    [Fact]public void AllActorActualPlusReservedMetadataIsChargedAndClosureDoesNotGrowPeak()
    {
        var s=Fresh("loaded");var edge=Edge(s);var a=Owner(s);var mission=a.Active!;var receipt=new ColonisationCycleReceiptDto{Ordinal=mission.Ordinal,SettlementManifestId=mission.SettlementManifestId!,TargetPlanetId=mission.CommittedTargetPlanetId!};
        a.Active=null;a.Receipts!.Add(receipt);Assert.Equal(edge,Edge(s));
        // Every actor is counted, not only whichever actor is currently planning.
        var all=Field<List<ColonisationCycleActorDto>>(Expansion(s),"CycleActors");var extra=Copy(a);extra.ActorId="defensive.other-actor";all.Add(extra);Assert.Equal(edge+1,Edge(s));all.Remove(extra);
        Policy(s).RecordBudget=edge-1;Assert.False(Capacity(s));Policy(s).RecordBudget=edge;Assert.True(Capacity(s));
    }
    [Fact]public void HumanMutationCannotSpendReceiptHeadroomAndFailureIsAtomic()
    {
        var s=Fresh("loaded");var edge=Edge(s);Policy(s).RecordBudget=edge;var before=Wire(s.ExportCycleSave());var command=Command(s,ColonisationAction.EnqueueKit);command.EnqueueKit=new(){ColonyId="colony.mars"};Assert.False(s.SubmitColonisation(command).Accepted);Assert.Equal(before,Wire(s.ExportCycleSave()));
        Policy(s).RecordBudget=8192;command=Command(s,ColonisationAction.EnqueueKit);command.EnqueueKit=new(){ColonyId="colony.mars"};Assert.True(s.SubmitColonisation(command).Accepted,s.LastPhysicalFailure);
    }
    [Fact]public void NoReceiptCapacityHoldsNewCycleBeforeProcurement()
    {
        var s=Fresh("closed1");var g=Live(s);Owner(s).Receipts![0].ClosedTurn--; // isolated metadata/counter fixture; movement clock remains actual, no altered replay claim.
        Policy(s).RecordBudget=Edge(s);var before=Wire(new{g,production=Field<List<ColonisationProductionItemDto>>(Context(s),"Production"),next=Owner(s).NextOrdinal});
        Assert.False((bool)Call(s,"PlanCycleAction",Owner(s).ActorId,Search())!);Assert.Null(Owner(s).Active);Assert.Equal(before,Wire(new{g=Live(s),production=Field<List<ColonisationProductionItemDto>>(Context(s),"Production"),next=Owner(s).NextOrdinal}));
    }
}
