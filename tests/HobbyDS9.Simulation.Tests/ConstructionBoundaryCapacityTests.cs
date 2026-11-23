using System.Text.Json;
using HobbyDS9.Contracts;
using HobbyDS9.Simulation;
using Xunit;

namespace HobbyDS9.Simulation.Tests;

public sealed class ConstructionBoundaryCapacityTests
{
    static T Clone<T>(T value)=>JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(value))!;
    sealed class Fixture
    {
        internal GameStateDto Before=null!,After=null!;
        internal DefinitionCatalogDto Definitions=null!;
        internal CampaignTacticalTravelSaveDto Next=null!;
        internal List<CampaignIntegratedBuiltShipDto> StaticBefore=new(),StaticAfter=new();
        internal List<CampaignPublishedBuiltShipDto> PublishedBefore=new(),PublishedAfter=new();
        internal Dictionary<string,ControllerKind> Controllers=new(){{"human",ControllerKind.Human},{"ai",ControllerKind.AI}};
        internal ConstructionBoundaryFacts Evaluate(long records=16,long used=0,long limit=32,int? turn=null,long? revision=null)
            =>ConstructionBoundaryCapacity.Evaluate(Before,After,Definitions,Next,Controllers,StaticBefore,StaticAfter,PublishedBefore,PublishedAfter,
                records,used,limit,turn??Before.TurnNumber,revision??Before.StateRevision);
        internal void Stage()
        {
            After=Clone(Before);ProductionPhase.Execute(After,Definitions);
            var final=Clone(After);final.TurnNumber++;final.StateRevision++;
            // Component fixture: this binds the real Production output to a synthetic
            // movement/final boundary. It does not pretend to activate a new profile.
            Next=new(){Phase=SessionPhase.Planning,Core=new(){Campaign=new(){GameState=final},Battles=new()},
                MovementProof=new(){Start=Clone(After),Orders=new(),Contacts=new()}};
        }
    }
    static Fixture Ready(bool ai=false)
    {
        var paid=PublishedBuiltSourceTests.Completion();var f=new Fixture{Before=paid.Before,Definitions=IntegratedPlaytestTests.Package().Physical!.CorePackage!.Campaign!.Definitions};
        f.PublishedAfter.Add(paid.Record);
        if(ai)
        {
            var cost=f.Definitions.ShipDesigns!.Single(d=>d.DefinitionId=="ship-design.source-cruiser");
            f.Before.Civilizations!.Single(c=>c.CivilizationId=="civ.cardassian").Credits+=cost.CreditCost;
            foreach(var r in cost.ResourceCosts!)PopulationRules.AddStock(f.Before.Colonies!.Single(c=>c.ColonyId=="colony.cardassia").Stockpiles!,r.ResourceDefinitionId,r.Amount);
            Assert.Equal(CommandErrorCode.None,new EnqueueProductionHandler(f.Definitions).Execute(f.Before,new(){ActorCivilizationId="civ.cardassian",
                EnqueueProduction=new(){ColonyId="colony.cardassia",DefinitionId="ship-design.source-cruiser"}}));
            var q=f.Before.Colonies!.Single(c=>c.ColonyId=="colony.cardassia").ProductionQueue!.Single();
            // Explicit already-worked paid fixture, not a completion forecast.
            q.CompletedIndustry=q.RequiredIndustry-1;
        }
        f.Stage();
        var built=f.After.Ships!.Single(s=>s.Blueprint?.BlueprintId==paid.Record.BlueprintId);
        f.PublishedAfter[0].ShipId=built.ShipId;f.PublishedAfter[0].FleetIdAtConstruction=built.FleetId;
        if(ai)
        {
            var ship=f.After.Ships!.Single(s=>s.OwnerCivilizationId=="civ.cardassian"&&!f.Before.Ships!.Any(b=>b.ShipId==s.ShipId));
            f.StaticAfter.Add(new(){ShipId=ship.ShipId,OwnerCivilizationId=ship.OwnerCivilizationId,FleetIdAtConstruction=ship.FleetId,
                TurnBuilt=f.Before.TurnNumber,ShipDesignDefinitionId=ship.ShipDesignDefinitionId,
                QueueItemId=f.Before.Colonies!.Single(c=>c.ColonyId=="colony.cardassia").ProductionQueue![0].QueueItemId});
        }
        return f;
    }
    [Fact] public void ActualMixedPaidProductionCertifiesOneBoundaryForBothOwnersWithoutMutatingInputs()
    {
        var f=Ready(true);var a=JsonSerializer.Serialize(f.Before);var b=JsonSerializer.Serialize(f.After);
        var facts=f.Evaluate(records:2,limit:1);
        Assert.True(facts.CertifiedNextBoundary);Assert.Equal(2,facts.PendingReservations);Assert.Equal(0,facts.RetainedRecords);
        Assert.Equal(1,facts.NextBatchSlots);Assert.Equal(0,facts.CancellationSlots);
        Assert.False(f.Evaluate(records:1).RecordCapacity);
        Assert.Equal(a,JsonSerializer.Serialize(f.Before));Assert.Equal(b,JsonSerializer.Serialize(f.After));
        f.PublishedAfter[0].QueueItemId="changed";f.After.Colonies!.Clear();Assert.True(facts.CertifiedNextBoundary);
    }
    [Fact] public void RemainingPaidQueueIsUncertifiableEvenWhenCurrentHeadCompletesAndCancellationFits()
    {
        var f=Ready();var colony=f.Before.Colonies!.Single(c=>c.ColonyId=="colony.sol");var q=Clone(colony.ProductionQueue![0]);
        q.QueueItemId="queue-item.test.tail";colony.ProductionQueue.Add(q);f.Stage();
        var facts=f.Evaluate();Assert.True(facts.JournalCapacity);Assert.False(facts.AllPaidQueuesCompleteNextAdvance);
        Assert.False(facts.CertifiedNextBoundary);Assert.Equal(1,facts.CancellationSlots);Assert.Equal(2,facts.PendingReservations);
        Assert.Equal(ConstructionBoundaryCertificate.Uncertifiable,facts.Certificate);
        Assert.True(f.Evaluate(limit:1).NextBatchFitsJournal);
        Assert.False(f.Evaluate(limit:1).CancellationReserveFitsAfterBatch); // advance fits; cancellation reserve does not.
    }
    [Fact] public void IncompleteActualProductionNeverProjectsAFutureIndustryCompletion()
    {
        var f=Ready();var q=f.Before.Colonies!.Single(c=>c.ColonyId=="colony.sol").ProductionQueue![0];
        q.CompletedIndustry=0;q.RequiredIndustry=long.MaxValue;f.PublishedAfter.Clear();f.Stage();
        var facts=f.Evaluate(limit:long.MaxValue);Assert.False(facts.AllPaidQueuesCompleteNextAdvance);
        Assert.Equal(ConstructionBoundaryCertificate.Uncertifiable,facts.Certificate);Assert.Equal(1,facts.CancellationSlots);
    }
    [Theory][InlineData(0)][InlineData(4)]
    public void InvalidRemainingRoundBoundaryRejects(int round)
    {
        var f=Ready();f.Next.Phase=SessionPhase.AwaitingCombat;f.Next.Core.Campaign.GameState.TurnNumber=f.Before.TurnNumber;
        f.Next.Core.Battles.Add(new(){State=new(){NextRound=round,Formations=new(){new(){SideId="human",Status=TacticalFormationStatus.Active}}}});
        Assert.Throws<ArgumentException>(()=>f.Evaluate());
    }
    [Fact] public void ActualCancellationFreesOnlyPendingReservationAndDestroyedRetainedRecordsStillConsumeCapacity()
    {
        var f=Ready();var colony=f.Before.Colonies!.Single(c=>c.ColonyId=="colony.sol");
        Assert.Equal(CommandErrorCode.None,new CancelProductionHandler().Execute(f.Before,new(){ActorCivilizationId=colony.OwnerCivilizationId!,
            CancelProduction=new(){ColonyId=colony.ColonyId,QueueItemId=colony.ProductionQueue![0].QueueItemId}}));
        f.PublishedAfter.Clear();f.Stage();Assert.Equal(0,f.Evaluate(records:0).PendingReservations);
        f.PublishedBefore.Add(new(){ShipId="ship.retired",QueueItemId="queue.retired"});f.PublishedAfter.Add(Clone(f.PublishedBefore[0]));
        Assert.False(f.Evaluate(records:0).RecordCapacity);Assert.True(f.Evaluate(records:1).CertifiedNextBoundary);
    }
    [Fact] public void CombatDestructionDoesNotEraseProductionBornAssociation()
    {
        var f=Ready();f.Next.Core.Campaign.GameState.Ships!.RemoveAll(s=>s.ShipId==f.PublishedAfter[0].ShipId);
        Assert.True(f.Evaluate().CertifiedNextBoundary);
        f.PublishedAfter.Clear();Assert.Throws<ArgumentException>(()=>f.Evaluate());
    }
    [Theory][InlineData("turn")][InlineData("revision")][InlineData("movement-proof")][InlineData("production")][InlineData("receipt")][InlineData("retained")][InlineData("duplicate")]
    public void StaleForgedOrIncompleteAssociationsRejectWithoutPublishing(string kind)
    {
        var f=Ready();var before=JsonSerializer.Serialize(f.Before);
        if(kind=="turn"){Assert.Throws<ArgumentException>(()=>f.Evaluate(turn:f.Before.TurnNumber+1));return;}
        if(kind=="revision"){Assert.Throws<ArgumentException>(()=>f.Evaluate(revision:f.Before.StateRevision+1));return;}
        if(kind=="movement-proof")f.Next.MovementProof!.Start.StateRevision++;
        if(kind=="production")f.After.NextEntitySequence++;
        if(kind=="receipt")f.PublishedAfter[0].QueueItemId="wrong";
        if(kind=="retained")f.StaticBefore.Add(new(){ShipId="lost",QueueItemId="lost"});
        if(kind=="duplicate")f.StaticAfter.Add(new(){ShipId=f.PublishedAfter[0].ShipId,QueueItemId="duplicate"});
        if(kind=="movement-proof"||kind=="production")Assert.Throws<InvalidOperationException>(()=>f.Evaluate());
        else Assert.Throws<ArgumentException>(()=>f.Evaluate());
        Assert.Equal(before,JsonSerializer.Serialize(f.Before));
    }
    [Theory][InlineData(1,5)][InlineData(2,4)][InlineData(3,3)]
    public void ActualHumanRoundBatchCostAndJournalBoundaryAreExact(int round,int slots)
    {
        var f=Ready();f.Next.Phase=SessionPhase.AwaitingCombat;f.Next.Core.Campaign.GameState.TurnNumber=f.Before.TurnNumber;
        f.Next.Core.Battles.Add(new(){State=new(){NextRound=round,Formations=new(){new(){SideId="human",Status=TacticalFormationStatus.Active}}}});
        Assert.Equal(slots,PlayableAdvanceCapacity.RequiredSlots(f.Next,f.Controllers));
        Assert.True(f.Evaluate(limit:slots).CertifiedNextBoundary);Assert.False(f.Evaluate(limit:slots-1).JournalCapacity);
        f.Next.Core.Battles[0].State.Formations[0].SideId="ai";Assert.Throws<ArgumentException>(()=>f.Evaluate());
    }
    [Theory][InlineData(-1,0,1)][InlineData(1,-1,1)][InlineData(1,2,1)]
    public void InvalidBudgetsReject(long records,long used,long limit)
    {var f=Ready();Assert.Throws<ArgumentException>(()=>f.Evaluate(records,used,limit));}
    [Fact] public void LongMaxJournalArithmeticHasNoAdditionOverflowAndLateProductionOverflowIsDetached()
    {
        var f=Ready();Assert.True(f.Evaluate(16,long.MaxValue-1,long.MaxValue).CertifiedNextBoundary);
        Assert.False(f.Evaluate(16,long.MaxValue,long.MaxValue).JournalCapacity);
        f.Before.PopulationStatistics!.ProductionCompleted=long.MaxValue;var original=JsonSerializer.Serialize(f.Before);
        Assert.Throws<OverflowException>(()=>f.Evaluate());Assert.Equal(original,JsonSerializer.Serialize(f.Before));
    }
}
