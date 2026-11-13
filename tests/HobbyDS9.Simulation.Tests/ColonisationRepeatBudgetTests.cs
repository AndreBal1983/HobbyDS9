using System.Reflection;
using System.Text.Json;
using HobbyDS9.Contracts;
using Xunit;
namespace HobbyDS9.Simulation.Tests;
// Maximal typed shape accounting, not a claim that invented actor IDs form a semantic replay root.
public sealed class ColonisationRepeatBudgetTests
{
    readonly Xunit.Abstractions.ITestOutputHelper output;
    public ColonisationRepeatBudgetTests(Xunit.Abstractions.ITestOutputHelper output){this.output=output;}
    static T Copy<T>(T x)=>JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(x))!;
    static ColonisationPolicyDto Policy()=>Copy(ColonisationRepeatTests.Package().BaseGrowth!.BaseRosterV2!.BaseV1!.Settlement!);
    static ColonisationAiPlanDto Full(ColonisationAiPlanDto p,bool ledger,string actor)
    {
        foreach(var prop in typeof(ColonisationAiPlanDto).GetProperties().Where(p=>p.PropertyType==typeof(string)&&p.Name!=nameof(ColonisationAiPlanDto.GoalId)))prop.SetValue(p,new string('\u0001',512));
        p.ActorId=actor;p.HoldReason=new string('\u0001',128);p.OwnAttemptedTargets=Enumerable.Range(0,8).Select(_=>new string('\u0001',512)).ToList();
        p.LatestActions=ledger?Enumerable.Range(0,8).Select(_=>new ColonisationAiActionReceiptDto{CommandId=new string('\u0001',128),Action=ColonisationAiAction.Travel,Error=CampaignTacticalError.None,BeforeRevision=long.MaxValue,AfterRevision=long.MaxValue}).ToList():new();
        p.LastPlannedTurn=int.MaxValue;p.BlueprintRevision=int.MaxValue;p.DisputedBoundaries=int.MaxValue;return p;
    }
    static (long Visits,long Bytes) Charge(object value)
    {
        using var doc=JsonDocument.Parse(JsonSerializer.Serialize(value));long Visits(JsonElement e)=>1+(e.ValueKind==JsonValueKind.Object?e.EnumerateObject().Sum(p=>1+Visits(p.Value)):e.ValueKind==JsonValueKind.Array?e.EnumerateArray().Sum(Visits):0);
        return(Visits(doc.RootElement),JsonSerializer.SerializeToUtf8Bytes(value).LongLength);
    }
    [Fact]public void TypedWorstCaseWithBothPlansEightReceiptsTargetsAndSupplyCommitmentFitsFixedReserve()
    {
        var root=CampaignTacticalReviewSession.StartRepeatGame(ColonisationRepeatTests.Package(),PublishedConstructionProfileTests.Settings(30)).ExportRepeatSave();var first=Full(root.AiPlans![0],true,new string('\u0001',512));var goal=root.RepeatGoals![0];goal.ActorId=first.ActorId;goal.Assignment=Full(goal.Assignment!,false,first.ActorId);goal.SettlementManifestId=new string('\u0001',512);goal.SupplyCampId=new string('\u0001',512);goal.StartedTurn=int.MaxValue;goal.ClosedTurn=int.MaxValue;
        var charge=Charge(new{first,goal});output.WriteLine($"maximal typed actor shape: {charge.Visits} visits including keys; {charge.Bytes} JSON UTF8 bytes; fixed reservation512/262144");Assert.True(charge.Visits<=ColonisationRootBudget.RepeatActorVisits,$"{charge.Visits}");Assert.True(charge.Bytes<=ColonisationRootBudget.RepeatActorBytes,$"{charge.Bytes}");
        // Include maximum integral widths and all keys; escaped strings have six JSON bytes per UTF16 unit.
        root.AiPlans=Enumerable.Range(0,4).Select(i=>{var p=Copy(first);p.ActorId=i+new string('\u0001',511);return p;}).ToList();
        root.RepeatGoals=root.AiPlans.Select(p=>{var g=Copy(goal);g.ActorId=p.ActorId;g.Assignment!.ActorId=p.ActorId;return g;}).ToList();ColonisationRootBudget.ValidateRepeat(root,Policy());
        root.PackageHash=new string('\u0001',512);var extra=JsonSerializer.SerializeToUtf8Bytes(new{root.FormatVersion,root.ProfileId,root.PackageHash,root.AiPlans,root.RepeatGoals}).LongLength;
        Assert.True(extra<=ColonisationRootBudget.RepeatOuterBytes+4*ColonisationRootBudget.RepeatActorBytes);
    }
    [Fact]public void EmptyAndMaximalFourActorPlansHaveIdenticalAllowanceAtExactVisitAndByteEdges()
    {
        var empty=CampaignTacticalReviewSession.StartRepeatGame(ColonisationRepeatTests.Package(),PublishedConstructionProfileTests.Settings(30)).ExportRepeatSave();
        empty.AiPlans=Enumerable.Range(0,4).Select(i=>{var p=Copy(empty.AiPlans![0]);p.ActorId="actor"+i;return p;}).ToList();
        empty.RepeatGoals=empty.AiPlans.Select(p=>new ColonisationRepeatGoalDto{ActorId=p.ActorId,Assignment=new(){ActorId=p.ActorId,GoalId=CampaignColonisationRepeatIdentity.Goal}}).ToList();
        var full=Copy(empty);foreach(var p in full.AiPlans!)Full(p,true,p.ActorId);foreach(var g in full.RepeatGoals!){Full(g.Assignment!,false,g.ActorId);g.SettlementManifestId=new string('x',512);g.SupplyCampId=new string('x',512);}
        foreach(var work in new[]{true,false}){
            long Edge(CampaignColonisationRepeatSaveDto root){long low=1,high=work?2000000:67108864;while(low<high){var mid=low+(high-low)/2;var p=Policy();if(work)p.MaximumValidationVisits=mid;else p.MaximumCanonicalRootBytes=mid;try{ColonisationRootBudget.ValidateRepeat(root,p);high=mid;}catch(ArgumentException){low=mid+1;}}return low;}
            var edge=Edge(empty);Assert.Equal(edge,Edge(full));var policy=Policy();if(work)policy.MaximumValidationVisits=edge;else policy.MaximumCanonicalRootBytes=edge;ColonisationRootBudget.ValidateRepeat(full,policy);
            if(work)policy.MaximumValidationVisits--;else policy.MaximumCanonicalRootBytes--;Assert.Throws<ArgumentException>(()=>ColonisationRootBudget.ValidateRepeat(full,policy));
            if(!work)Assert.True(JsonSerializer.SerializeToUtf8Bytes(full).LongLength<=edge);
        }
    }
    [Fact]public void FifthActorUnknownStageOversizedSupplyAndLedgerFailClosedBeforeBaseTraversal()
    {
        var root=CampaignTacticalReviewSession.StartRepeatGame(ColonisationRepeatTests.Package(),PublishedConstructionProfileTests.Settings(30)).ExportRepeatSave();
        foreach(var poison in new Action<CampaignColonisationRepeatSaveDto>[] {r=>r.RepeatGoals![0].SupplyCampId=new string('x',513),r=>r.RepeatGoals![0].Assignment!.LatestActions!.Add(new()),r=>r.RepeatGoals![0].State=(ColonisationRepeatState)99,r=>r.AiPlans=Enumerable.Repeat(r.AiPlans![0],5).ToList()}){var bad=Copy(root);poison(bad);Assert.Throws<ArgumentException>(()=>ColonisationRootBudget.ValidateRepeat(bad,Policy()));}
    }
}
