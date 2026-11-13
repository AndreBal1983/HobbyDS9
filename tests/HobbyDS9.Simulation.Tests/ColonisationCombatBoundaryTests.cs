using HobbyDS9.Contracts;
using Xunit;
using static HobbyDS9.Simulation.Tests.ColonisationTransportTests;

namespace HobbyDS9.Simulation.Tests;

public sealed class ColonisationCombatBoundaryTests
{
    [Fact] public void ActualAiTravelCombatRoundsAndFinalAftermathKeepTheExactSettlementEraAndRollback()
    {
        var package=ColonisationInventoryTests.Package();var s=CampaignTacticalReviewSession.StartColonisation(package,PublishedConstructionProfileTests.Settings());
        for(int i=0;i<80&&s.CaptureStatus().Phase==SessionPhase.Planning;i++)Advance(s);
        Assert.Equal(SessionPhase.AwaitingCombat,s.CaptureStatus().Phase);var waiting=s.ExportColonisationSave();var turn=s.CaptureStatus().Turn;
        Assert.Equal(turn-1,waiting.Colonisation!.LastSettlementTurn);Assert.NotEmpty(s.KnownContacts(Human));
        Assert.Contains(waiting.PublishedBuiltShips!,r=>r.OwnerCivilizationId!="civ.federation");
        Assert.Equal(Json(waiting),Json(CampaignTacticalReviewSession.LoadColonisation(package,waiting).ExportColonisationSave()));
        // Defensive exact recording-cap fixture; it is not a different supported
        // public policy. Every real round and aftermath must conserve this debt.
        var context=typeof(CampaignTacticalReviewSession).GetField("colonisation",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance)!.GetValue(s)!;
        var policy=(ColonisationPolicyDto)context.GetType().GetField("Policy",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance)!.GetValue(context)!;
        var controllers=(Dictionary<string,ControllerKind>)typeof(CampaignTacticalReviewSession).GetField("controllers",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance)!.GetValue(s)!;
        policy.JournalBudget=checked(waiting.Journal!.Count+waiting.Production!.Count+(int)PlayableAdvanceCapacity.RequiredSlots(waiting.Campaign!.Integrated!.Physical!,controllers)-1);
        foreach(var id in s.Encounters(Human))
        {
            while(!s.Observe(id,Human).Complete)
            {
                var view=s.Observe(id,Human);Assert.True(s.CommitRound(new(){ActorId=Human,EncounterId=id,CommandId="command.colony.round."+view.Round,
                    ExpectedTurn=view.Turn,ExpectedRound=view.Round,ExpectedBoundaryRevision=view.BoundaryRevision,Replacements=new()}).Accepted,s.LastPhysicalFailure);
                Assert.Equal(turn-1,s.ExportColonisationSave().Colonisation!.LastSettlementTurn);
            }
        }
        var completed=s.ExportColonisationSave();var before=Json(completed);var stamp=s.CaptureStatus();
        s.FinalizationProbe=_=>throw new InvalidOperationException("Deliberate colonisation aftermath fault.");
        Assert.False(s.FinalizeBatch(stamp.Turn,stamp.CampaignRevision).Accepted);Assert.Equal(before,Json(s.ExportColonisationSave()));s.FinalizationProbe=null;
        s.PublishedTransactionProbe=stage=>{if(stage=="after-cache-copy")throw new InvalidOperationException("Deliberate late settlement publication fault.");};
        Assert.False(s.FinalizeBatch(stamp.Turn,stamp.CampaignRevision).Accepted);Assert.Equal(before,Json(s.ExportColonisationSave()));s.PublishedTransactionProbe=null;
        Assert.True(s.FinalizeBatch(stamp.Turn,stamp.CampaignRevision).Accepted,s.LastPhysicalFailure);var final=s.ExportColonisationSave();
        Assert.Equal(turn,final.Colonisation!.LastSettlementTurn);Assert.Equal(turn+1,s.CaptureStatus().Turn);Assert.Equal(turn,final.LastSettlementProof!.Turn);
        Assert.Equal(Json(final),Json(CampaignTacticalReviewSession.LoadColonisation(package,final).ExportColonisationSave()));
    }
}
