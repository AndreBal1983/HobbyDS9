using System.Text.Json;
using HobbyDS9.Application;
using HobbyDS9.Contracts;
using HobbyDS9.Simulation;
using Xunit;

namespace HobbyDS9.Simulation.Tests;

public sealed class ConstructionJournalReserveTests
{
    static T Clone<T>(T value)=>JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(value))!;
    static (CampaignPlayableFleetPackageDto Package,ICampaignPlayableFleetSession App) Start(bool buy=true)
    {
        var p=PlayableFleetTests.Package();p.ReplayOperationLimit=65536;
        var app=CampaignPlayableFleetApplication.StartPackage(p);
        if(buy)
        {
            var s=app.CaptureStatus();Assert.True(app.SubmitStrategicCommand(new(){ActorCivilizationId="civ.federation",CommandId="command.reserve.buy",
                Kind=CommandKind.EnqueueProduction,ExpectedTurn=s.Turn,ExpectedRevision=s.CampaignRevision,
                EnqueueProduction=new(){ColonyId="colony.sol",DefinitionId="ship-design.source-cruiser"}}).Accepted);
        }
        return(p,app);
    }
    internal static ConstructionJournalFacts Evaluate(CampaignPlayableFleetPackageDto p,CampaignPlayableFleetSaveDto save,
        ConstructionJournalOperation kind=ConstructionJournalOperation.AlreadyStagedBoundary,long? used=null,long records=4096,long work=10000000)
        =>ConstructionJournalReserve.Evaluate(p,p.PackageHash!,save.Integrated!.Physical!,save.Integrated.BuiltShips!,
            Array.Empty<CampaignPublishedBuiltShipDto>(),used??save.Transcript!.Count,kind,records,work);
    [Fact] public void RealPolicyEnumerationMatchesIndependentAllRegimeProjectionAndLeavesInputsDetached()
    {
        var f=Start();var save=f.App.ExportPlayableSave();var before=JsonSerializer.Serialize(save);var package=JsonSerializer.Serialize(f.Package);
        var result=Evaluate(f.Package,save);Assert.True(result.Certified,result.UncertifiableReason);
        var world=save.Integrated!.Physical!.Core.Campaign.GameState.Colonies!.Single(c=>c.ColonyId=="colony.sol");
        var definitions=f.Package.Integrated!.Physical!.CorePackage!.Campaign!.Definitions;
        var values=new List<long>();
        foreach(var p in definitions.GovernancePolicies!)foreach(ResistanceState r in Enum.GetValues<ResistanceState>())foreach(int d in new[]{0,1,2})
        {var c=Clone(world);c.Politics!.GovernancePolicyDefinitionId=p.DefinitionId;c.Politics.Resistance=r;c.Politics.DisruptionRemainingTurns=d;
         values.Add(WorldEconomy.Project(c,save.Integrated.Physical.Core.Campaign.GameState,definitions).Industry);}
        Assert.Equal(values.Min(),Assert.Single(result.Floors).Industry);Assert.Equal(2,result.Floors[0].Industry);
        Assert.Equal(before,JsonSerializer.Serialize(save));Assert.Equal(package,JsonSerializer.Serialize(f.Package));
        save.Integrated.Physical.Core.Campaign.GameState.Colonies!.Clear();definitions.GovernancePolicies!.Clear();
        Assert.Equal(2,result.Floors[0].Industry);Assert.True(result.Certified);
        Assert.Throws<NotSupportedException>(()=>((IList<ConstructionIndustryFloor>)result.Floors).Clear());
    }
    [Theory][InlineData("policy")][InlineData("power")][InlineData("rounding")]
    public void ExhaustiveFloorFailsClosedForZeroPolicyPowerAndIntegerRounding(string kind)
    {
        var f=Start();var definitions=f.Package.Integrated!.Physical!.CorePackage!.Campaign!.Definitions;
        if(kind=="policy")definitions.GovernancePolicies![0].IndustryFactor=0;
        if(kind=="power")foreach(var i in definitions.Infrastructure!)i.PowerSupply=0;
        if(kind=="rounding")foreach(var p in definitions.GovernancePolicies!)p.IndustryFactor=1;
        var result=Evaluate(f.Package,f.App.ExportPlayableSave());Assert.False(result.Certified);Assert.Contains("floor",result.UncertifiableReason!);
    }
    [Fact] public void AllOwnerFifoWorkRoundsEachItemSeparatelyWithoutCarryingIndustry()
    {
        var f=Start();var save=f.App.ExportPlayableSave();var game=save.Integrated!.Physical!.Core.Campaign.GameState;
        var sol=game.Colonies!.Single(c=>c.ColonyId=="colony.sol");var q=sol.ProductionQueue![0];q.RequiredIndustry=3;q.CompletedIndustry=0;
        var tail=Clone(q);tail.QueueItemId="queue.test.tail";tail.RequiredIndustry=3;sol.ProductionQueue.Add(tail);
        var ai=game.Colonies!.Single(c=>c.ColonyId=="colony.cardassia");var other=Clone(q);other.QueueItemId="queue.test.ai";other.RequiredIndustry=9;ai.ProductionQueue!.Add(other);
        var r=Evaluate(f.Package,save);Assert.True(r.Certified,r.UncertifiableReason);Assert.Equal(3,r.Pending);
        Assert.Equal(5,r.Horizon); // AI ceil(9/2)=5; human ceil(3/2)+ceil(3/2)=4.
        ai.ProductionQueue.Clear();r=Evaluate(f.Package,save);Assert.Equal(4,r.Horizon);Assert.Equal(2,r.Pending);
        Assert.False(Evaluate(f.Package,save,records:1).RecordCapacity);
    }
    [Theory][InlineData(1,1)][InlineData(2,0)]
    public void CandidateOperationHasExactDeltaAndImmutableStampBinding(int operationValue,int delta)
    {
        var operation=(ConstructionJournalOperation)operationValue;
        var f=Start();var save=f.App.ExportPlayableSave();var baseResult=Evaluate(f.Package,save);long used=f.Package.ReplayOperationLimit!.Value-baseResult.ReservedSlots;
        var result=Evaluate(f.Package,save,operation,used);Assert.Equal(delta,result.ProposedDelta);Assert.Equal(operation,result.Operation);
        Assert.Equal(delta==0,result.JournalCapacity);Assert.Equal(save.Integrated!.Physical!.Core.Campaign.GameState.StateRevision,result.Revision);
        Assert.Equal(save.Integrated.Physical.Phase,result.Phase);Assert.Equal(f.Package.PackageHash,result.PackageHash);
        Assert.True(Evaluate(f.Package,save,operation,used-delta).Certified);
    }
    [Fact] public void SplitFleetCannotInflateShipBasedCombatBoundAndActualProductionReducesHorizon()
    {
        var f=Start();var save=f.App.ExportPlayableSave();var before=Evaluate(f.Package,save);var next=Clone(save);
        var game=next.Integrated!.Physical!.Core.Campaign.GameState;
        EconomyPhase.Execute(game,f.Package.Integrated!.Physical!.CorePackage!.Campaign!.Definitions);
        ResearchPhase.Execute(game,f.Package.Integrated!.Physical!.CorePackage!.Campaign!.Definitions);
        ProductionPhase.Execute(game,f.Package.Integrated!.Physical!.CorePackage!.Campaign!.Definitions);
        game.StateRevision++;game.TurnNumber++;next.Transcript!.Add(new(){Common=new(){Kind=FrontierOperationKind.Advance}});
        // Production has not completed this long queue: no new retained record yet.
        Assert.Equal(save.Integrated!.BuiltShips!.Count,next.Integrated.BuiltShips!.Count);
        var after=Evaluate(f.Package,next);Assert.True(after.Certified,after.UncertifiableReason);
        Assert.True(after.Horizon<before.Horizon);Assert.True(after.Used+after.ReservedSlots<=before.Used+before.ReservedSlots);
        var originalBound=after.EncounterBound;
        // Any legal split changes fleet grouping only; the calculator deliberately
        // derives its bound from ship+pending cardinality, not grouping count.
        game.Fleets!.Reverse();Assert.Equal(originalBound,Evaluate(f.Package,next).EncounterBound);
    }
    [Theory][InlineData("automation")][InlineData("infrastructure")][InlineData("owner")][InlineData("identity")][InlineData("work")][InlineData("range")][InlineData("turn")][InlineData("revision")][InlineData("allocation")][InlineData("food")]
    public void UnsupportedChangedOrExhaustedCandidatesAreUncertifiable(string kind)
    {
        var f=Start();var save=f.App.ExportPlayableSave();var game=save.Integrated!.Physical!.Core.Campaign.GameState;var colony=game.Colonies!.Single(c=>c.ColonyId=="colony.sol");
        if(kind=="automation")colony.Automation++;
        if(kind=="infrastructure")colony.Infrastructure![0].Count++;
        if(kind=="owner")colony.OwnerCivilizationId="civ.cardassian";
        if(kind=="identity")save.Integrated.Physical.PackageHash=new string('f',64);
        if(kind=="range")colony.ProductionQueue![0].CompletedIndustry=colony.ProductionQueue[0].RequiredIndustry;
        if(kind=="turn")game.TurnNumber=int.MaxValue;
        if(kind=="revision")game.StateRevision=long.MaxValue;
        if(kind=="allocation")game.NextEntitySequence=ulong.MaxValue;
        if(kind=="food")colony.Food=long.MaxValue;
        var before=JsonSerializer.Serialize(save);var result=Evaluate(f.Package,save,work:kind=="work"?1:10000000);
        Assert.False(result.Certified);Assert.NotNull(result.UncertifiableReason);Assert.Equal(before,JsonSerializer.Serialize(save));
    }
    [Fact] public void TechnicalBudgetAndUnknownOperationDoNotActivateALargerDomain()
    {
        var f=Start();var save=f.App.ExportPlayableSave();f.Package.ReplayOperationLimit=65537;
        Assert.False(Evaluate(f.Package,save).Certified);f.Package.ReplayOperationLimit=65536;
        Assert.False(Evaluate(f.Package,save,(ConstructionJournalOperation)999).Certified);
    }
    [Theory][InlineData(0)][InlineData(10000001)]
    public void UnsupportedHelperWorkDomainReturnsNoCertificate(long budget)
    {var f=Start();Assert.False(Evaluate(f.Package,f.App.ExportPlayableSave(),work:budget).Certified);}
    [Fact] public void ActualPaidFrozenR1WorkDoesNotBecomeLaterPublishedR2Work()
    {
        var f=Start(false);var paid=PublishedBuiltSourceTests.Completion();var save=f.App.ExportPlayableSave();
        save.Integrated!.Physical!.Core.Campaign.GameState=Clone(paid.Before);
        var scenario=f.Package.Integrated!.Physical!.CorePackage!.Campaign!;
        foreach(var actor in save.Integrated.Physical.Core.Campaign.GameState.Civilizations!)
            if(actor.CivilizationId!="civ.federation")actor.ControllerKind=ControllerKind.AI;
        scenario.InitialState=Clone(save.Integrated.Physical.Core.Campaign.GameState); // Explicit single-human component fixture, no new session admission.
        var before=Evaluate(f.Package,save);Assert.True(before.Certified,before.UncertifiableReason);
        var game=save.Integrated.Physical.Core.Campaign.GameState;var r1=game.Blueprints!.Single();var draft=Clone(r1.Fit!);draft.DisplayName="Later R2";
        Assert.Equal(CommandErrorCode.None,new PublishShipBlueprintHandler(scenario.Definitions,new SaveCompatibility(scenario.ScenarioId,scenario.RulesVersion,scenario.ContentHash))
            .Execute(game,new(){ActorCivilizationId=r1.OwnerCivilizationId!,PublishShipBlueprint=new(){BlueprintId=r1.BlueprintId!,Revision=2,Draft=draft}}));
        var after=Evaluate(f.Package,save);Assert.True(after.Certified,after.UncertifiableReason);
        Assert.Equal(before.Horizon,after.Horizon);Assert.Equal(before.ReservedSlots,after.ReservedSlots);
        Assert.Equal(1,game.Colonies!.Single(c=>c.ColonyId=="colony.sol").ProductionQueue![0].Blueprint!.Revision);
        Assert.Equal(2,game.Blueprints!.Count);
    }
    [Fact] public void ActualInternalAiProcurementAddsLiabilityAndMustRecomputeEvenWithZeroPlayerDelta()
    {
        var f=Start();var save=f.App.ExportPlayableSave();var core=save.Integrated!.Physical!.Core;
        var scenario=f.Package.Integrated!.Physical!.CorePackage!.Campaign!;var game=core.Campaign.GameState;
        var design=scenario.Definitions.ShipDesigns!.Single(d=>d.DefinitionId=="ship-design.source-cruiser");
        // Explicit funded detached AI fixture. Run the actual internal planner/shared
        // command handlers, not a caller boolean claiming procurement happened.
        game.Civilizations!.Single(c=>c.CivilizationId=="civ.cardassian").Credits+=design.CreditCost;
        foreach(var r in design.ResourceCosts!)PopulationRules.AddStock(game.Colonies!.Single(c=>c.ColonyId=="colony.cardassia").Stockpiles!,r.ResourceDefinitionId,r.Amount);
        var before=Evaluate(f.Package,save);
        var planner=new SimulationSession(core.Campaign,scenario.Definitions,new SaveCompatibility(scenario.ScenarioId,scenario.RulesVersion,scenario.ContentHash));
        core.Campaign.GameState=planner.RunDemoPlanningAi("civ.cardassian",f.Package.Integrated.ConstructionTemplates!.Select(t=>t.ShipDesignDefinitionId!).ToList());
        var after=Evaluate(f.Package,save,ConstructionJournalOperation.InternalAiProcurement);
        Assert.True(after.Certified,after.UncertifiableReason);Assert.Equal(before.Pending+1,after.Pending);Assert.Equal(0,after.ProposedDelta);
        Assert.True(after.ReservedSlots>before.ReservedSlots);
        Assert.False(Evaluate(f.Package,save,ConstructionJournalOperation.InternalAiProcurement,65536-before.ReservedSlots).Certified);
    }
}

public sealed partial class PlayableFleetTests
{
    [Fact] public void ActualFleetSplitPreservesShipBasedEncounterBound()
    {
        var app=Built();var p=Package();var before=ConstructionJournalReserveTests.Evaluate(p,app.ExportPlayableSave());
        var fleetCount=app.GetOwnStrategicFleets(Human).Count;
        var ship=app.ExportPlayableSave().Integrated!.Physical!.Core.Campaign.GameState.Ships!.First(s=>s.OwnerCivilizationId==Human).ShipId;
        Assert.True(app.SubmitFleetOrganization(Organization(app,new[]{ship})).Accepted);
        var after=ConstructionJournalReserveTests.Evaluate(p,app.ExportPlayableSave());
        Assert.True(app.GetOwnStrategicFleets(Human).Count>fleetCount);Assert.Equal(before.PotentialShips,after.PotentialShips);
        Assert.Equal(before.EncounterBound,after.EncounterBound);Assert.Equal(before.BatchSlots,after.BatchSlots);
    }
    [Fact] public void ActualAdvanceWithExistingPaidQueueConservesJournalReserve()
    {
        var p=Package();p.ReplayOperationLimit=65536;
        // Test-authored affordability boundary: no optional AI procurement can enter
        // this first advance without the future recertification path (not live yet).
        var initial=p.Integrated!.Physical!.CorePackage!.Campaign!.InitialState;
        foreach(var actor in initial.Civilizations!.Where(c=>c.ControllerKind==ControllerKind.AI))actor.Credits=0;
        foreach(var colony in initial.Colonies!.Where(c=>c.OwnerCivilizationId!=Human))foreach(var stock in colony.Stockpiles!)stock.Amount=0;
        var app=CampaignPlayableFleetApplication.StartPackage(p);Buy(app);
        var before=ConstructionJournalReserveTests.Evaluate(p,app.ExportPlayableSave());Assert.True(before.Certified,before.UncertifiableReason);
        Advance(app);
        var after=ConstructionJournalReserveTests.Evaluate(p,app.ExportPlayableSave());Assert.True(after.Certified,after.UncertifiableReason);
        Assert.True(after.Horizon<before.Horizon);Assert.True(after.Used+after.ReservedSlots<=before.Used+before.ReservedSlots);
    }
    [Theory][InlineData("research")][InlineData("publication")][InlineData("travel")][InlineData("organisation")]
    public void ActualOptionalOperationsSpendOneEntryAndKeepIndustryLiability(string kind)
    {
        var p=Package();p.ReplayOperationLimit=65536;var app=CampaignPlayableFleetApplication.StartPackage(p);Buy(app);
        var before=ConstructionJournalReserveTests.Evaluate(p,app.ExportPlayableSave());var s=app.CaptureStatus();
        if(kind=="research")Assert.True(app.SubmitStrategicCommand(new(){ActorCivilizationId=Human,CommandId="command.reserve.science",
            ExpectedTurn=s.Turn,ExpectedRevision=s.CampaignRevision,Kind=CommandKind.SetResearchAllocation,
            SetResearchAllocation=new(){ScienceAllocationUnits=300}}).Accepted);
        if(kind=="publication")Assert.True(app.SubmitStrategicCommand(new(){ActorCivilizationId=Human,CommandId="command.reserve.publish",
            ExpectedTurn=s.Turn,ExpectedRevision=s.CampaignRevision,Kind=CommandKind.PublishShipBlueprint,
            PublishShipBlueprint=new(){BlueprintId="blueprint.reserve",Revision=1,Draft=PublishedTacticalSupportTests.Draft(PublishedTacticalSupportTests.Load(),true)}}).Accepted);
        if(kind=="travel")Travel(app,CampaignTravelIntent.Leave);
        if(kind=="organisation")Assert.True(app.SubmitFleetOrganization(Organization(app,Array.Empty<string>(),CampaignFleetOrganizationKind.Rename,
            app.GetOwnStrategicFleets(Human).First().FleetId.Value,"Reserve test")).Accepted);
        var save=app.ExportPlayableSave();var after=ConstructionJournalReserveTests.Evaluate(p,save);
        Assert.True(after.Certified,after.UncertifiableReason);Assert.Equal(before.Used+1,after.Used);Assert.Equal(before.ReservedSlots,after.ReservedSlots);
        long exact=65536-after.ReservedSlots;
        Assert.False(ConstructionJournalReserveTests.Evaluate(p,save,ConstructionJournalOperation.PlayerOptional,exact).Certified);
        Assert.True(ConstructionJournalReserveTests.Evaluate(p,save,ConstructionJournalOperation.PlayerOptional,exact-1).Certified);
    }
    [Fact] public void ActualMandatoryRoundsAndAftermathConserveUnpublishedReserve()
    {
        var p=Package();p.ReplayOperationLimit=65536;var pending=BeforeCapacityEncounter(p).Pending;
        var app=CampaignPlayableFleetApplication.LoadSave(p,pending);
        var facts=ConstructionJournalReserveTests.Evaluate(p,app.ExportPlayableSave());Assert.True(facts.Certified,facts.UncertifiableReason);
        Assert.True(facts.CurrentCombatDebt>0);
        foreach(var id in app.GetEncounters(Human))while(!app.Observe(id,Human).Complete)
        {
            var before=ConstructionJournalReserveTests.Evaluate(p,app.ExportPlayableSave());var v=app.Observe(id,Human);
            Assert.True(app.CommitRound(new(){ActorId=Human,EncounterId=id,CommandId="command.reserve.round."+id+"."+v.Round,
                ExpectedTurn=v.Turn,ExpectedRound=v.Round,ExpectedBoundaryRevision=v.BoundaryRevision,Replacements=new()}).Accepted);
            var after=ConstructionJournalReserveTests.Evaluate(p,app.ExportPlayableSave());Assert.True(after.Certified,after.UncertifiableReason);
            Assert.True(after.Used+after.ReservedSlots<=before.Used+before.ReservedSlots);
        }
        var beforeFinal=ConstructionJournalReserveTests.Evaluate(p,app.ExportPlayableSave());var s=app.CaptureStatus();
        Assert.True(app.FinalizeBatch(s.Turn,s.CampaignRevision).Accepted);
        var final=ConstructionJournalReserveTests.Evaluate(p,app.ExportPlayableSave());Assert.True(final.Certified,final.UncertifiableReason);
        Assert.Equal(0,final.CurrentCombatDebt);Assert.True(final.Used+final.ReservedSlots<=beforeFinal.Used+beforeFinal.ReservedSlots);
    }
}
