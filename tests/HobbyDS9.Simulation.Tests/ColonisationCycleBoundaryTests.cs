using System.Reflection;
using System.Text.Json;
using HobbyDS9.Contracts;
using Xunit;
using static HobbyDS9.Simulation.Tests.ColonisationTransportTests;
using static HobbyDS9.Simulation.Tests.ColonisationCycleTests;
namespace HobbyDS9.Simulation.Tests;
// Only cached public checkpoints are replay roots. All mutations below are labelled defensive fixtures.
public sealed class ColonisationCycleBoundaryTests
{
    const BindingFlags Private=BindingFlags.NonPublic|BindingFlags.Instance;
    internal static T Field<T>(object o,string name)=>(T)o.GetType().GetField(name,Private)!.GetValue(o)!;
    internal static object Expansion(CampaignTacticalReviewSession s)=>Field<object>(s,"expansion");
    internal static object Context(CampaignTacticalReviewSession s)=>Field<object>(s,"colonisation");
    internal static GameStateDto Live(CampaignTacticalReviewSession s)=>Field<CampaignTacticalReviewSaveDto>(s,"state").Campaign.GameState;
    internal static CampaignColonisationStateDto Facts(CampaignTacticalReviewSession s)=>Field<CampaignColonisationStateDto>(Context(s),"State");
    internal static ColonisationCycleActorDto Owner(CampaignTacticalReviewSession s)=>Assert.Single(Field<List<ColonisationCycleActorDto>>(Expansion(s),"CycleActors"));
    internal static ColonisationAiPlanDto First(CampaignTacticalReviewSession s)=>Assert.Single(Field<List<ColonisationAiPlanDto>>(Expansion(s),"Plans"));
    internal static object? Call(CampaignTacticalReviewSession s,string name,params object?[] args)=>typeof(CampaignTacticalReviewSession).GetMethods(Private).Single(m=>m.Name==name&&m.GetParameters().Length==args.Length).Invoke(s,args);
    internal static object Search()=>Activator.CreateInstance(typeof(CampaignTacticalReviewSession).GetNestedType("ExpansionSearch",BindingFlags.NonPublic)!,true)!;
    [Fact]public void ActualReturnKeepsIntendedTargetWhenMutableAssignmentIsCleared()
    {
        var s=Fresh("loaded");var a=Owner(s);var id=a.Active!.SettlementManifestId;var intended=a.Active.CommittedTargetPlanetId;var firstCamp=First(s).CampId;
        a.Active.Assignment!.Stage=ColonisationAiStage.Recovering;a.Active.Assignment.TargetPlanetId=null;a.Active.Assignment.TargetNodeId=null;
        for(var n=0;n<8&&Owner(s).Active!=null;n++)Advance(s);
        var receipt=Assert.Single(Owner(s).Receipts!);Assert.Equal(ColonisationCycleOutcome.Returned,receipt.Outcome);Assert.Equal(id,receipt.SettlementManifestId);Assert.Equal(intended,receipt.TargetPlanetId);
        var resolution=Assert.Single(Facts(s).ManifestResolutions!,m=>m.ManifestId==id);Assert.Equal(SettlerManifestStage.Unloaded,resolution.Stage);Assert.NotEqual(intended,resolution.DestinationPlanetId);
        Assert.Equal(100,resolution.PopulationTransferred);Assert.Contains(resolution.CargoTransferred!,r=>r.ResourceDefinitionId=="resource.settlement-kit"&&r.Amount==1);Assert.Equal(firstCamp,First(s).CampId);
    }
    [Fact]public void ActualCarrierLossKeepsLatchAndRecordsOneTerminalWithoutFreeRetry()
    {
        // Optional PRIVATE immutable input avoids the process-local public journey only for this Fact.
        const string variable="HOBBYDS9_CYCLE_CARRIER_LOSS_FIXTURE";
        bool supplied=Environment.GetEnvironmentVariables().Contains(variable);
        string? path=supplied?Environment.GetEnvironmentVariable(variable):null;
        byte[]? input=null;CampaignColonisationCycleSaveDto? decoded=null;CampaignColonisationCyclePackageDto? package=null;
        string? packageBefore=null,inputBefore=null;
        byte[] ReadInput()
        {
            Assert.True(!string.IsNullOrWhiteSpace(path)&&Path.IsPathFullyQualified(path),"Explicit fixture path required");
            var full=Path.GetFullPath(path!);var boundary=Path.GetFullPath("E:/HobbyDS9/artifacts").TrimEnd(Path.DirectorySeparatorChar,Path.AltDirectorySeparatorChar);
            Assert.True(full.StartsWith(boundary+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase),"Artifact confinement required");
            FileSystemInfo? item=new FileInfo(full);bool reached=false;
            while(item!=null){Assert.True(item.Exists&&(item.Attributes&FileAttributes.ReparsePoint)==0,"Plain existing fixture ancestry required");
                if(string.Equals(item.FullName,boundary,StringComparison.OrdinalIgnoreCase)){reached=true;break;}
                item=item is FileInfo file?file.Directory:((DirectoryInfo)item).Parent;}
            Assert.True(reached,"Artifact boundary ancestry required");
            using var stream=new FileStream(full,FileMode.Open,FileAccess.Read,FileShare.Read);
            Assert.True(stream.Length==2780110,"Exact bounded whole fixture length required");
            var bytes=new byte[2780110];int offset=0;
            while(offset<bytes.Length){int read=stream.Read(bytes,offset,bytes.Length-offset);Assert.True(read>0,"Complete fixture read required");offset+=read;}
            Assert.True(stream.ReadByte()==-1,"Fixture must have no trailing bytes");
            Assert.True(Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(bytes)).Equals("800091fe1d9bbc5959d2216d7447cc6446b746901463e8ab27b57d531b4b4937",StringComparison.OrdinalIgnoreCase),"Exact immutable whole fixture hash required");
            return bytes;
        }
        CampaignTacticalReviewSession s;
        if(supplied)
        {
            input=ReadInput();decoded=JsonSerializer.Deserialize<CampaignColonisationCycleSaveDto>(input);
            Assert.True(decoded!=null&&decoded.FormatVersion==14&&decoded.ProfileId=="generated-colonisation-cycle-v1"&&decoded.PackageHash=="34aba41a024a8c3e37c3bd24922ad2b0b47fcab3dffb5cd9d049d99e300dae75","Closed whole Cycle fixture required");
            package=Package();packageBefore=Wire(package);inputBefore=Wire(decoded);
            Assert.True(package.PackageHash==decoded!.PackageHash,"Actual current package identity required");
            s=CampaignTacticalReviewSession.LoadCycleSave(package,decoded);
            Assert.True(Wire(s.ExportCycleSave())==inputBefore,"Normal complete replay must preserve entire input");
            Assert.True(Wire(package)==packageBefore&&Wire(decoded)==inputBefore&&ReadInput().SequenceEqual(input),"Package, input DTO and whole file unchanged after load");
        }
        else s=Fresh("loaded"); // Existing normal-suite path remains unchanged; explicit input never reaches Journey.
        var a=Owner(s);Assert.True(a.Active!=null,"Active settlement required");
        var id=a.Active!.SettlementManifestId;var target=a.Active.CommittedTargetPlanetId;int ordinal=a.Active.Ordinal,next=a.NextOrdinal;
        var receiptsBefore=a.Receipts!.Select(Wire).ToArray();var bootstrapBefore=Wire(First(s));
        var m=Facts(s).ManifestRecords!.Single(m=>m.ManifestId==id);var g=Live(s);var facts=Facts(s);
        Assert.True(m.Stage==SettlerManifestStage.Loaded&&m.Kind==SettlerManifestKind.Settlement&&m.Passengers!=null&&m.Passengers.Population>0,"Actual loaded Settlement passengers required");
        Assert.True(m.Cargo!.SingleOrDefault(r=>r.ResourceDefinitionId=="resource.settlement-kit")?.Amount==1,"One physical settlement kit required");
        var ship=g.Ships!.Single(x=>x.ShipId==m.ShipId&&x.OwnerCivilizationId==m.OwnerId);Assert.True(ship.CurrentHull>0,"Live exact carrier required");
        if(supplied)Assert.True(g.TurnNumber==68&&g.Phase==SessionPhase.Planning&&receiptsBefore.Length==2&&ordinal==3&&next==4
            &&facts.ManifestRecords!.Count(x=>x.Stage==SettlerManifestStage.Loaded)==1&&m.Passengers!.Population==100&&m.Food==19,"Exact admitted mature fixture stage/census required");
        long people=m.Passengers!.Population,food=m.Food,deaths=g.PopulationStatistics!.Deaths,oldDeaths=m.PassengerDeaths,oldFoodLost=m.FoodLost;
        string cohort=m.Passengers.CohortId;var cargo=m.Cargo!.ToDictionary(r=>r.ResourceDefinitionId,r=>r.Amount,StringComparer.Ordinal);
        var oldLost=m.MaterialsLost!.ToDictionary(r=>r.ResourceDefinitionId,r=>r.Amount,StringComparer.Ordinal);
        var transferred=Wire(new{m.PopulationTransferred,m.FoodDelivered,m.FoodConsumed,m.EquipmentConsumed,m.MaterialsDelivered});
        Assert.True(!facts.ManifestResolutions!.Any(x=>x.ManifestId==id),"No prior terminal for active manifest");
        // Deliberately defensive hull0 accounting, NOT actual combat removal or a replayable modified root.
        ship.CurrentHull=0;Call(s,"TickCarriedSettlers",g,facts);Call(s,"ReconcileCycle",g);
        Assert.True(m.Stage==SettlerManifestStage.Destroyed&&m.Passengers==null&&m.Food==0&&m.Cargo!.All(r=>r.Amount==0),"All remaining finite cargo must be lost");
        Assert.True(m.PassengerDeaths==oldDeaths+people&&g.PopulationStatistics.Deaths==deaths+people&&m.FoodLost==oldFoodLost+food,"Exact remaining population/food loss required");
        if(!supplied)Assert.Equal(100,m.PassengerDeaths); // Preserve the original default-fixture deaths assertion.
        foreach(var resource in oldLost.Keys.Union(cargo.Keys,StringComparer.Ordinal))
            Assert.True(PopulationRules.Stock(m.MaterialsLost!,resource)==oldLost.GetValueOrDefault(resource)+cargo.GetValueOrDefault(resource),"Exact per-resource remaining cargo loss required");
        Assert.True(m.MaterialsLost!.All(r=>oldLost.ContainsKey(r.ResourceDefinitionId)||cargo.ContainsKey(r.ResourceDefinitionId)),"No invented lost resource");
        Assert.True(Wire(new{m.PopulationTransferred,m.FoodDelivered,m.FoodConsumed,m.EquipmentConsumed,m.MaterialsDelivered})==transferred,"Loss must not transfer or consume cargo");
        Assert.True(!g.EntityIdentities.Any(x=>x.EntityId==cohort||x.EntityId==id),"Passenger and manifest identities retired");
        var resolutions=facts.ManifestResolutions!.Where(x=>x.ManifestId==id).ToList();Assert.True(resolutions.Count==1,"Exactly one manifest terminal required");var resolution=resolutions[0];
        Assert.True(resolution.Stage==SettlerManifestStage.Destroyed&&resolution.Turn==g.TurnNumber&&resolution.DestinationPlanetId==null
            &&resolution.PopulationLost==m.PassengerDeaths&&resolution.FoodLost==m.FoodLost&&resolution.PopulationTransferred==m.PopulationTransferred&&resolution.FoodTransferred==m.FoodDelivered
            &&Wire(resolution.CargoLost)==Wire(m.MaterialsLost)&&Wire(resolution.CargoTransferred)==Wire(m.MaterialsDelivered),"Exact manifest terminal totals required");
        Assert.True(a.Receipts!.Count==receiptsBefore.Length+1&&a.Receipts!.Take(receiptsBefore.Length).Select(Wire).SequenceEqual(receiptsBefore),"Historical receipt prefix retained");
        var receipt=a.Receipts!.Single(x=>x.Ordinal==ordinal);
        Assert.True(receipt.Outcome==ColonisationCycleOutcome.Lost&&receipt.TargetPlanetId==target&&receipt.SettlementManifestId==id&&receipt.TerminalTurn==resolution.Turn,"Exact Lost latch and terminal required");
        Assert.True(a.Active==null&&a.NextOrdinal==next&&Wire(First(s))==bootstrapBefore,"Loss closes only the committed mission");
        var after=Wire(new{game=g,facts,owner=a,bootstrap=First(s)});Call(s,"ReconcileCycle",g);
        Assert.True(Wire(new{game=g,facts,owner=Owner(s),bootstrap=First(s)})==after,"Repeat reconciliation must not duplicate loss or terminal");
        var kitsBefore=Wire(Field<List<ColonisationProductionItemDto>>(Context(s),"Production").Where(q=>q.Kind==ColonisationProductionKind.SettlementKit));
        var settlementsBefore=Wire(facts.ManifestRecords!.Where(x=>x.Kind==SettlerManifestKind.Settlement));
        var planned=(bool)Call(s,"PlanCycleAction",a.ActorId,Search())!;
        if(!supplied)Assert.False(planned); // Original fixture expectation; logistics may legitimately run with explicit input.
        Assert.True(Owner(s).Active==null&&Owner(s).NextOrdinal==next&&Wire(Facts(s).ManifestRecords!.Where(x=>x.Kind==SettlerManifestKind.Settlement))==settlementsBefore
            &&Wire(Field<List<ColonisationProductionItemDto>>(Context(s),"Production").Where(q=>q.Kind==ColonisationProductionKind.SettlementKit))==kitsBefore,"No same-week new settlement mission or procurement");
        if(supplied)Assert.True(Wire(package)==packageBefore&&Wire(decoded)==inputBefore&&ReadInput().SequenceEqual(input!),"Original package/input DTO/file remain unchanged through defensive case");
    }
    [Fact]public void EstablishedReceiptRemainsHistoricalSuccessAfterLaterEmptyOutpost()
    {
        var s=Fresh("closed1");var receipt=Assert.Single(Owner(s).Receipts!);var before=Wire(receipt);var camp=Facts(s).CampSources!.Single(c=>c.CampId==receipt.CampId);var g=Live(s);
        Assert.Contains(Facts(s).CampTerminals!,t=>t.CampId==camp.CampId&&t.Kind==SettlementTerminalKind.Established&&t.Turn==receipt.TerminalTurn);
        // Metadata/provenance fixture: later empty state is not forged as a public conquest/save history.
        camp.Stage=ColonyEstablishmentStage.EmptyOutpost;Facts(s).CampTerminals!.Add(new(){CampId=camp.CampId,Kind=SettlementTerminalKind.Extinct,Turn=g.TurnNumber});
        Call(s,"ReconcileCycle",g);Call(s,"ValidateCycleState",g);Assert.Equal(before,Wire(Assert.Single(Owner(s).Receipts!)));Assert.False((bool)Call(s,"RepeatNeedsSupply",g,camp)!);
    }
    [Fact]public void DeficitButNotUrgentRecipientGetsRealStreamedFoodQueueOnlyOnce()
    {
        var s=Fresh("closed1");var g=Live(s);var a=Owner(s);var camp=Facts(s).CampSources!.Single(c=>c.CampId==First(s).CampId);var world=g.Colonies!.Single(w=>w.ColonyId==camp.ColonyId);
        var donor=g.Colonies!.Where(w=>w.OwnerCivilizationId==a.ActorId&&w.ColonyId!=world.ColonyId).SelectMany(w=>w.Cohorts!).OrderByDescending(c=>c.Population).First();
        // Conserve actual own population while creating a one-food deficit with existing high reserves.
        var transfer=201-PopulationRules.Population(world);Assert.True(transfer>0&&donor.Population>transfer);donor.Population-=transfer;world.Cohorts![0].Population+=transfer;
        var output=(WorldEconomySnapshot)Call(s,"ProjectOwnedWorld",world,g)!;Assert.Equal(1,output.FoodDemand-output.FoodOutput);Assert.False((bool)Call(s,"RepeatNeedsSupply",g,camp)!);
        Assert.Null(a.Active);var credits=g.Civilizations!.Single(c=>c.CivilizationId==a.ActorId).Credits;var stocks=Wire(world.Stockpiles!);var queues=Field<List<ColonisationProductionItemDto>>(Context(s),"Production").Select(q=>q.QueueItemId).ToHashSet();
        Assert.True((bool)Call(s,"PlanCycleRecipientFoodReadiness",a.ActorId)!);a=Owner(s);g=Live(s);world=g.Colonies!.Single(w=>w.ColonyId==world.ColonyId);
        var q=Assert.Single(Field<List<ColonisationProductionItemDto>>(Context(s),"Production"),q=>!queues.Contains(q.QueueItemId));Assert.Equal(ColonisationProductionKind.Infrastructure,q.Kind);Assert.Equal(world.ColonyId,q.ColonyId);Assert.Equal(0,q.CompletedIndustry);Assert.All(q.ConsumedResources!,r=>Assert.Equal(0,r.Amount));
        Assert.Equal(credits,g.Civilizations!.Single(c=>c.CivilizationId==a.ActorId).Credits);Assert.Equal(stocks,Wire(world.Stockpiles!));Assert.Equal(output.FoodOutput,((WorldEconomySnapshot)Call(s,"ProjectOwnedWorld",world,g)!).FoodOutput);Assert.Null(a.Active);
        Call(s,"PlanCycleRecipientFoodReadiness",a.ActorId);Assert.Single(Field<List<ColonisationProductionItemDto>>(Context(s),"Production"),x=>x.ColonyId==q.ColonyId&&x.DefinitionId==q.DefinitionId);
        for(int n=0;n<32&&Field<List<ColonisationProductionItemDto>>(Context(s),"Production").Any(x=>x.QueueItemId==q.QueueItemId);n++)Advance(s);
        world=Live(s).Colonies!.Single(w=>w.ColonyId==q.ColonyId);Assert.True(((WorldEconomySnapshot)Call(s,"ProjectOwnedWorld",world,Live(s))!).FoodOutput>output.FoodOutput);
        Assert.Contains(Field<List<ColonisationProductionReceiptDto>>(Context(s),"History"),x=>x.Order!.QueueItemId==q.QueueItemId&&x.Completed);
    }
    [Fact]public void LoadedCycleAllLateFaultsPreserveEntireRootAndActorCommitments()
    {
        var s=Fresh("loaded");var before=Wire(s.ExportCycleSave());foreach(var point in new[]{"after-command","before-copy","after-transcript-copy","after-cache-copy"})
        {bool seen=false;s.PublishedTransactionProbe=p=>{if(p==point){seen=true;throw new InvalidOperationException("Cycle fault");}};var f=s.CaptureStatus();Assert.False(s.AdvanceTurn(f.Turn,f.CampaignRevision).Accepted);Assert.True(seen);Assert.Equal(before,Wire(s.ExportCycleSave()));}
        s.PublishedTransactionProbe=null;
        foreach(var phase in Enum.GetValues<StrategicTurnPhase>()){bool seen=false;s.PhysicalPhaseProbe=p=>{if(p==phase){seen=true;return false;}return true;};var f=s.CaptureStatus();Assert.False(s.AdvanceTurn(f.Turn,f.CampaignRevision).Accepted);Assert.True(seen);Assert.Equal(before,Wire(s.ExportCycleSave()));}s.PhysicalPhaseProbe=null;
    }
    [Fact]public void HiddenForeignFactsAndActorOrderingDoNotChangeCycleDecisions()
    {
        var a=CampaignTacticalReviewSession.StartCycleGame(Package(),PublishedConstructionProfileTests.Settings(30));var b=CampaignTacticalReviewSession.StartCycleGame(Package(),PublishedConstructionProfileTests.Settings(30));
        var g=Live(b);g.Civilizations!.Reverse();g.Colonies!.Reverse();g.Ships!.Reverse();g.Fleets!.Reverse();g.Civilizations.Single(c=>c.ControllerKind==ControllerKind.Human).Credits+=123;
        Call(a,"PlanExpansion",Call(a,"TravelCopy"));Call(b,"PlanExpansion",Call(b,"TravelCopy"));Assert.Equal(Wire(First(a)),Wire(First(b)));Assert.Equal(Wire(Owner(a)),Wire(Owner(b)));
    }
    [Fact]public void PlausibleReceiptLatchOrdinalAndForeignSourceTamperingFailFullReplay()
    {
        var root=Root("next-real");foreach(var change in new Action<CampaignColonisationCycleSaveDto>[] {
            x=>x.CycleActors![0].Active!.CommittedTargetPlanetId=x.CycleActors[0].Receipts![0].TargetPlanetId,
            x=>x.CycleActors![0].Receipts![0].TargetPlanetId=x.CycleActors[0].Active!.CommittedTargetPlanetId!,
            x=>x.CycleActors![0].Receipts![0].TerminalTurn++,x=>x.CycleActors![0].NextOrdinal++,
            x=>x.CycleActors![0].SupplyCampId="foreign",x=>x.CycleActors![0].Active!.Assignment!.HomeColonyId="colony.sol"})
        {var bad=Copy(root);change(bad);Assert.Throws<ArgumentException>(()=>CampaignTacticalReviewSession.LoadCycleSave(Package(),bad));}
    }
    [Fact]public void BootstrapReturnThenRealRetryRetainsBothActualManifestProvenances()
    {
        var s=Fresh("bootstrap-loaded");var first=First(s);var id=first.ManifestId;Assert.Null(first.CampId);first.Stage=ColonisationAiStage.Recovering;first.TargetPlanetId=null;first.TargetNodeId=null;
        for(int n=0;n<12&&!Facts(s).ManifestRecords!.Any(m=>m.OwnerId==first.ActorId&&m.Kind==SettlerManifestKind.Settlement&&m.ManifestId!=id);n++)Advance(s);
        Assert.Contains(Facts(s).ManifestResolutions!,r=>r.ManifestId==id&&r.Stage==SettlerManifestStage.Unloaded);
        Assert.Contains(Facts(s).ManifestRecords!,m=>m.OwnerId==first.ActorId&&m.Kind==SettlerManifestKind.Settlement&&m.ManifestId!=id);
        Call(s,"ValidateCycleState",Live(s));
        for(int n=0;n<96&&Owner(s).Active==null&&Owner(s).Receipts!.Count==0;n++)Advance(s);
        Assert.True(Owner(s).Active!=null||Owner(s).Receipts!.Count>0);Call(s,"ValidateCycleState",Live(s));
        Assert.Contains(Facts(s).ManifestResolutions!,r=>r.ManifestId==id&&r.Stage==SettlerManifestStage.Unloaded);
    }
    [Fact]public void NewCycleCapacityTypeIsOnlyForVisitByteExhaustionAndMalformedRootStaysGeneric()
    {
        var root=Root("loaded");var policy=Copy(Package().BaseGrowth!.BaseRosterV2!.BaseV1!.Settlement!);policy.MaximumValidationVisits=1;
        Assert.Throws<ColonisationRootCapacityException>(()=>ColonisationRootBudget.ValidateCycle(root,policy));
        var malformed=Copy(root);malformed.CycleActors![0].Active!.Assignment!.OwnAttemptedTargets=null;
        Assert.Throws<ArgumentException>(()=>ColonisationRootBudget.ValidateCycle(malformed,policy));
        var legacy=CampaignTacticalReviewSession.StartRepeatGame(ColonisationRepeatTests.Package(),PublishedConstructionProfileTests.Settings(30)).ExportRepeatSave();
        Assert.Throws<ArgumentException>(()=>ColonisationRootBudget.ValidateRepeat(legacy,policy));
    }

    [Fact]public void BootstrapPostImmigrationFoodGapIsMaintainedBeforeGrowthWaitingWithoutPhantomCycle()
    {
        var s=Fresh("before-immigration");var first=First(s);var a=Owner(s);var g=Live(s);var camp=Facts(s).CampSources!.Single(c=>c.CampId==first.CampId);var world=g.Colonies!.Single(w=>w.ColonyId==camp.ColonyId);
        var donor=g.Colonies!.Single(w=>w.ColonyId==first.HomeColonyId).Cohorts!.OrderByDescending(c=>c.Population).First();
        var transfer=101-PopulationRules.Population(world);Assert.True(transfer>0);donor.Population-=transfer;world.Cohorts![0].Population+=transfer;
        var current=(WorldEconomySnapshot)Call(s,"ProjectOwnedWorld",world,g)!;Assert.Equal(current.FoodDemand,current.FoodOutput);Assert.False((bool)Call(s,"RepeatNeedsSupply",g,camp)!);
        var people=Copy(donor);people.CohortId="fixture.preview-people";people.Population=100;var forecast=Copy(world);forecast.Cohorts!.Add(people);var future=(WorldEconomySnapshot)Call(s,"ProjectOwnedWorld",forecast,g)!;
        Assert.Equal(0,future.Crowding);Assert.True(future.FoodDemand>future.FoodOutput);Assert.DoesNotContain(Facts(s).ManifestRecords!,m=>m.OwnerId==a.ActorId&&m.Kind==SettlerManifestKind.ImmigrationOnly);
        Assert.True((bool)Call(s,"PlanCycleAction",a.ActorId,Search())!);Assert.Null(Owner(s).Active);Assert.Empty(Owner(s).Receipts!);
        Assert.Contains(Field<List<ColonisationProductionItemDto>>(Context(s),"Production"),q=>q.ColonyId==world.ColonyId&&q.Kind==ColonisationProductionKind.Infrastructure);
        Assert.NotEqual(ColonisationAiStage.GrowthClosed,First(s).Stage);
    }
    [Fact]public void EarliestRetainedCampGetsRealNewSupplyAfterMultipleCycleClosures()
    {
        var s=Fresh("closed2");var g=Live(s);var a=Owner(s);var old=Wire(a.Receipts!);var camp=Facts(s).CampSources!.Single(c=>c.CampId==First(s).CampId);var world=g.Colonies!.Single(w=>w.ColonyId==camp.ColonyId);
        var donor=g.Colonies!.Where(w=>w.OwnerCivilizationId==a.ActorId&&w.ColonyId!=world.ColonyId).SelectMany(w=>w.Cohorts!).OrderByDescending(c=>c.Population).First();var transfer=201-PopulationRules.Population(world);Assert.True(transfer>0);donor.Population-=transfer;world.Cohorts![0].Population+=transfer;
        Facts(s).FoodConsumed+=world.Food;world.Food=0;Assert.True((bool)Call(s,"RepeatNeedsSupply",g,camp)!);
        var previous=Facts(s).ManifestRecords!.Select(m=>m.ManifestId).ToHashSet();SettlerManifestDto? supply=null;
        for(int n=0;n<16;n++)
        {
            supply=Facts(s).ManifestRecords!.FirstOrDefault(m=>m.OwnerId==a.ActorId&&m.Kind==SettlerManifestKind.SupplyOnly&&!previous.Contains(m.ManifestId));
            if(supply?.Stage==SettlerManifestStage.Delivered)break;
            if(supply?.Stage==SettlerManifestStage.Loaded)Assert.Equal(camp.CampId,Owner(s).SupplyCampId);
            Advance(s);
        }
        Assert.NotNull(supply);Assert.Equal(SettlerManifestStage.Delivered,supply!.Stage);Assert.True(supply.FoodDelivered>0);
        Assert.Contains(Facts(s).ManifestResolutions!,x=>x.ManifestId==supply.ManifestId&&x.FoodTransferred>0&&x.DestinationPlanetId==world.PlanetId);Assert.Equal(old,Wire(Owner(s).Receipts!));
    }
    [Fact]public void LoadedCycleKeepsExactCarrierWithAnotherEligibleTransportAndBlockedReturnKeepsCargo()
    {
        var s=Fresh("loaded");var a=Owner(s);var m=Facts(s).ManifestRecords!.Single(m=>m.ManifestId==a.Active!.SettlementManifestId);var g=Live(s);var ship=g.Ships!.Single(x=>x.ShipId==m.ShipId);
        var other=Copy(ship);other.ShipId="aaa.fixture-other-transport";g.Ships!.Add(other);var records=Field<List<CampaignPublishedBuiltShipDto>>(Field<object>(s,"published"),"Records");var provenance=Copy(records.Single(x=>x.ShipId==ship.ShipId));provenance.ShipId=other.ShipId;records.Add(provenance);
        var capability=Call(s,"TransportCapability",g,other)!;Assert.True(Convert.ToInt64(capability.GetType().GetProperty("Passengers")!.GetValue(capability))>=100);
        var fleet=g.Fleets!.Single(f=>f.FleetId==ship.FleetId);fleet.TransitTotalCost=10;fleet.TransitProgress=1;a.Active!.Assignment!.TransportShipId=other.ShipId;
        Assert.False((bool)Call(s,"PlanCycleAction",a.ActorId,Search())!);Assert.Equal(m.ShipId,Owner(s).Active!.Assignment!.TransportShipId);
        fleet.TransitTotalCost=0;fleet.TransitProgress=0;fleet.RouteNodeIds!.Clear();fleet.LocationNodeId=a.Active.Assignment.TargetNodeId!;g.NavigationKnowledge!.Single(k=>k.CivilizationId==a.ActorId).Connections!.Clear();var before=Wire(m);
        Assert.False((bool)Call(s,"RepeatReturn",a.ActorId,ship,m,Search())!);Assert.Equal(before,Wire(m));Assert.Equal(m.ManifestId,Owner(s).Active!.SettlementManifestId);
    }
    [Fact]public void CycleDeployCannotChangeLatchedIntentionAndForeignSourceCannotProvideFood()
    {
        var s=Fresh("loaded");var a=Owner(s);var active=a.Active!;First(s).LastPlannedTurn=Live(s).TurnNumber;First(s).LatestActions!.Clear();
        var slot=Enum.Parse(typeof(CampaignTacticalReviewSession).GetNestedType("MissionSlot",BindingFlags.NonPublic)!,"Cycle");
        Assert.False((bool)Call(s,"AssignedColonisation",slot,a.ActorId,ColonisationAiAction.Deploy,new ColonisationCommandDto{Action=ColonisationAction.RequestDeployment,Deploy=new(){ManifestId=active.SettlementManifestId!,PlanetId="planet.sol"}})!);
        // Only the failed bounded receipt/hold may change, not physical state or immutable commitment.
        Assert.Equal(Wire(Copy(ColonisationCycleTests.Root("loaded")).BaseCampaign),Wire(s.ExportCycleSave().BaseCampaign));Assert.Equal(active.CommittedTargetPlanetId,Owner(s).Active!.CommittedTargetPlanetId);
        var g=Live(s);var camp=Facts(s).CampSources!.Single(c=>c.CampId==First(s).CampId);var receiver=g.Colonies!.Single(w=>w.ColonyId==camp.ColonyId);
        foreach(var world in g.Colonies!.Where(w=>w.OwnerCivilizationId==a.ActorId&&w.ColonyId!=receiver.ColonyId)){Facts(s).FoodConsumed+=world.Food;world.Food=0;}
        var original=Wire(new{g,facts=Facts(s)});Assert.False((bool)Call(s,"PlanRepeatSupply",a.ActorId,camp,null,Search())!);Assert.Equal(original,Wire(new{g,facts=Facts(s)}));
    }

    [Fact]public void MalformedCurrentCycleInputIsNotSwallowedAsOrdinaryHeadroomHold()
    {
        var s=Fresh("closed1");Owner(s).Receipts![0].ClosedTurn--;First(s).OwnAttemptedTargets=null;var before=Wire(new{game=Live(s),owner=Owner(s),first=First(s)});
        var error=Assert.Throws<TargetInvocationException>(()=>Call(s,"PlanCycleAction",Owner(s).ActorId,Search()));Assert.IsType<ArgumentException>(error.InnerException);Assert.IsNotType<ColonisationRootCapacityException>(error.InnerException);
        Assert.Equal(before,Wire(new{game=Live(s),owner=Owner(s),first=First(s)}));
    }

}
