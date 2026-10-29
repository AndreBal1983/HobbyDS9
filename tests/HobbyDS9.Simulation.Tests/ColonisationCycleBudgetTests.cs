using System.Reflection;
using System.Text.Json;
using HobbyDS9.Contracts;
using Xunit;
using static HobbyDS9.Simulation.Tests.ColonisationCycleTests;
namespace HobbyDS9.Simulation.Tests;
// Typed maximal graphs intentionally are not semantic histories or gameplay fixtures.
public sealed class ColonisationCycleBudgetTests
{
    readonly Xunit.Abstractions.ITestOutputHelper output;public ColonisationCycleBudgetTests(Xunit.Abstractions.ITestOutputHelper output){this.output=output;}
    static ColonisationPolicyDto Policy()=>Copy(Package().BaseGrowth!.BaseRosterV2!.BaseV1!.Settlement!);
    static ColonisationAiPlanDto Full(string goal,string actor,bool ledger)
    {
        var p=new ColonisationAiPlanDto{GoalId=goal};foreach(var prop in typeof(ColonisationAiPlanDto).GetProperties().Where(p=>p.PropertyType==typeof(string)&&p.Name!=nameof(ColonisationAiPlanDto.GoalId)))prop.SetValue(p,new string('\u0001',512));
        p.ActorId=actor;p.HoldReason=new string('\u0001',128);p.OwnAttemptedTargets=Enumerable.Range(0,8).Select(_=>new string('\u0001',512)).ToList();p.LatestActions=ledger?Enumerable.Range(0,8).Select(_=>new ColonisationAiActionReceiptDto{CommandId=new string('\u0001',128),Action=ColonisationAiAction.Travel,Error=CampaignTacticalError.None,BeforeRevision=long.MaxValue,AfterRevision=long.MaxValue}).ToList():new();p.LastPlannedTurn=int.MaxValue;p.BlueprintRevision=int.MaxValue;p.DisputedBoundaries=int.MaxValue;return p;
    }
    static ColonisationCycleReceiptDto Receipt()=>new(){Ordinal=int.MaxValue,StartedTurn=int.MaxValue,TerminalTurn=int.MaxValue,ClosedTurn=int.MaxValue,Outcome=ColonisationCycleOutcome.Established,SettlementManifestId=new string('\u0001',512),TargetPlanetId=new string('\u0001',512),CampId=new string('\u0001',512)};
    static (long Visits,long Bytes) Charge(object value)
    {using var doc=JsonDocument.Parse(Wire(value));long Visits(JsonElement e)=>1+(e.ValueKind==JsonValueKind.Object?e.EnumerateObject().Sum(p=>1+Visits(p.Value)):e.ValueKind==JsonValueKind.Array?e.EnumerateArray().Sum(Visits):0);return(Visits(doc.RootElement),JsonSerializer.SerializeToUtf8Bytes(value).LongLength);}
    static CampaignColonisationCycleSaveDto Empty()=>CampaignTacticalReviewSession.StartCycleGame(Package(),PublishedConstructionProfileTests.Settings(30)).ExportCycleSave();
    [Fact]public void MaximalActorAndReceiptGraphFitsProposedFixedChargesIncludingKeysEscapesAndIntegers()
    {
        var actor=new string('\u0001',512);var first=Full("establish-first-frontier-colony-v1",actor,true);var cycle=new ColonisationCycleActorDto{ActorId=actor,NextOrdinal=int.MaxValue,SupplyCampId=new string('\u0001',512),Active=new(){Ordinal=int.MaxValue,StartedTurn=int.MaxValue,SettlementManifestId=new string('\u0001',512),CommittedTargetPlanetId=new string('\u0001',512),Assignment=Full(CampaignColonisationCycleIdentity.Goal,actor,false)}};
        var a=Charge(new{first,cycle});var t=Charge(Receipt());output.WriteLine($"max actor={a.Visits}/{a.Bytes}; receipt={t.Visits}/{t.Bytes}");Assert.True(a.Visits<=ColonisationRootBudget.CycleActorVisits);Assert.True(a.Bytes<=ColonisationRootBudget.CycleActorBytes);Assert.True(t.Visits<=ColonisationRootBudget.CycleReceiptVisits);Assert.True(t.Bytes<=ColonisationRootBudget.CycleReceiptBytes);
        var root=Empty();root.PackageHash=new string('\u0001',512);root.AiPlans=Enumerable.Range(0,4).Select(i=>Full(first.GoalId,i+new string('\u0001',511),true)).ToList();root.CycleActors=root.AiPlans.Select(p=>{var x=Copy(cycle);x.ActorId=p.ActorId;x.Active!.Assignment!.ActorId=p.ActorId;x.Receipts!.Add(Receipt());return x;}).ToList();ColonisationRootBudget.ValidateCycle(root,Policy());
        var shape=Charge(new{root.FormatVersion,root.ProfileId,root.PackageHash,root.AiPlans,root.CycleActors});Assert.True(shape.Bytes<=ColonisationRootBudget.CycleOuterBytes+4*ColonisationRootBudget.CycleActorBytes+8*ColonisationRootBudget.CycleReceiptBytes);Assert.True(shape.Visits<=ColonisationRootBudget.CycleOuterVisits+4*ColonisationRootBudget.CycleActorVisits+8*ColonisationRootBudget.CycleReceiptVisits);
    }
    [Fact]public void EmptyAndFullActiveStorageHaveSameAllowanceAndReceiptReservationSubstitutionAtExactEdges()
    {
        var empty=Empty();empty.AiPlans=Enumerable.Range(0,4).Select(i=>new ColonisationAiPlanDto{ActorId="actor"+i}).ToList();empty.CycleActors=empty.AiPlans.Select(p=>new ColonisationCycleActorDto{ActorId=p.ActorId,Active=new(){Assignment=new(){ActorId=p.ActorId,GoalId=CampaignColonisationCycleIdentity.Goal}}}).ToList();
        var full=Copy(empty);foreach(var p in full.AiPlans!) {var f=Full(p.GoalId,p.ActorId,true);full.CycleActors!.Single(a=>a.ActorId==p.ActorId).Active!.Assignment=Full(CampaignColonisationCycleIdentity.Goal,p.ActorId,false);p.OwnAttemptedTargets=f.OwnAttemptedTargets;p.LatestActions=f.LatestActions;}
        var closed=Copy(full);foreach(var a in closed.CycleActors!){a.Active=null;a.Receipts!.Add(Receipt());}
        long Edge(CampaignColonisationCycleSaveDto root,bool work){long lo=1,hi=work?2000000:67108864;while(lo<hi){var mid=lo+(hi-lo)/2;var p=Policy();if(work)p.MaximumValidationVisits=mid;else p.MaximumCanonicalRootBytes=mid;try{ColonisationRootBudget.ValidateCycle(root,p);hi=mid;}catch(ArgumentException){lo=mid+1;}}return lo;}
        foreach(var work in new[]{true,false}){var edge=Edge(empty,work);Assert.Equal(edge,Edge(full,work));Assert.Equal(edge,Edge(closed,work));var p=Policy();if(work)p.MaximumValidationVisits=edge;else p.MaximumCanonicalRootBytes=edge;ColonisationRootBudget.ValidateCycle(closed,p);if(work)p.MaximumValidationVisits--;else p.MaximumCanonicalRootBytes--;Assert.Throws<ColonisationRootCapacityException>(()=>ColonisationRootBudget.ValidateCycle(closed,p));if(!work)Assert.True(JsonSerializer.SerializeToUtf8Bytes(full).LongLength<edge);}
    }
    [Fact]public void NullOversizeUnknownAndFiveActorCarriersFailBeforeBaseCopy()
    {
        var root=Empty();foreach(var poison in new Action<CampaignColonisationCycleSaveDto>[] {x=>x.CycleActors![0].Receipts=null,x=>x.CycleActors![0].SupplyCampId=new string('x',513),x=>x.AiPlans=Enumerable.Repeat(x.AiPlans![0],5).ToList(),x=>x.CycleActors![0].Receipts!.Add(new(){Outcome=(ColonisationCycleOutcome)99}),x=>x.CycleActors![0].Active=new(){CommittedTargetPlanetId=new string('x',513),Assignment=new(){GoalId=CampaignColonisationCycleIdentity.Goal}}}){var bad=Copy(root);poison(bad);Assert.Throws<ArgumentException>(()=>ColonisationRootBudget.ValidateCycle(bad,Policy()));}
    }
}
