using System.Text.Json;
using HobbyDS9.Application;
using HobbyDS9.Contracts;
using HobbyDS9.Simulation;
using Xunit;

namespace HobbyDS9.Simulation.Tests;

public sealed class IntegratedPlaytestTests
{
    private const string A="civ.federation",B="civ.cardassian",FA="fleet.federation-scout",FB="fleet.cardassian-scout",Design="ship-design.source-cruiser";
    private static string Json<T>(T value)=>JsonSerializer.Serialize(value);
    private static T Clone<T>(T value)=>JsonSerializer.Deserialize<T>(Json(value))!;
    internal static CampaignIntegratedPlaytestPackageDto Package()=>IntegratedPlaytestPackageReader.Read(Path.Combine(AppContext.BaseDirectory,"scenarios","integrated-playtest-v1"));
    private static ICampaignIntegratedPlaytestSession Reload(CampaignIntegratedPlaytestPackageDto p,ICampaignIntegratedPlaytestSession app)
        =>CampaignIntegratedPlaytestApplication.LoadSave(p,Clone(app.ExportIntegratedSave()));
    private static CommandEnvelope Command(ICampaignIntegratedPlaytestSession app,CommandKind kind,string actor=A)
    {
        var stamp=app.CaptureStatus();return new CommandEnvelope {Kind=kind,ActorCivilizationId=actor,
            CommandId="command.integrated."+stamp.Turn+"."+stamp.CampaignRevision,ExpectedTurn=stamp.Turn,ExpectedRevision=stamp.CampaignRevision};
    }
    private static void Advance(ICampaignIntegratedPlaytestSession app)
    {var s=app.CaptureStatus();Assert.True(app.AdvanceTurn(s.Turn,s.CampaignRevision).Accepted);}
    private static CommandEnvelope Enqueue(ICampaignIntegratedPlaytestSession app,string colony="colony.sol")
    {var command=Command(app,CommandKind.EnqueueProduction);command.EnqueueProduction=new EnqueueProductionPayload {ColonyId=colony,DefinitionId=Design};return command;}
    private static void Build(ICampaignIntegratedPlaytestSession app)
    {
        Assert.True(app.SubmitStrategicCommand(Enqueue(app)).Accepted);
        for(int i=0;i<100&&app.ExportIntegratedSave().BuiltShips!.Count==0;i++)Advance(app);
        Assert.Single(app.ExportIntegratedSave().BuiltShips!);
    }
    private static void Order(ICampaignIntegratedPlaytestSession app,string actor,string fleet,CampaignTravelIntent intent,params string[] route)
    {
        var s=app.CaptureStatus();Assert.True(app.SubmitTravelOrder(new SubmitCampaignTravelOrderDto {ActorId=actor,
            CommandId="command.integrated.travel."+s.CampaignRevision,ExpectedTurn=s.Turn,ExpectedRevision=s.CampaignRevision,
            Order=new CampaignTravelOrderDto {FleetId=fleet,Intent=intent,RouteNodeIds=route.ToList()}}).Accepted);
    }

    private static CampaignTacticalReviewSession Backend(ICampaignIntegratedPlaytestSession app)=>(CampaignTacticalReviewSession)app.GetType()
        .GetField("session",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)!.GetValue(app)!;
    private static void Contact(ICampaignIntegratedPlaytestSession app,string fleet)
    {
        Order(app,A,fleet,CampaignTravelIntent.Leave,"nav.bajor");
        var target=app.GetOwnStrategicFleets(B).Single(f=>f.FleetId.Value==FB);
        var route=app.FindKnownRoute(B,target.LocationNodeId.Value,"nav.bajor");Assert.True(route.Success);
        Order(app,B,FB,CampaignTravelIntent.Leave,route.Steps.ToArray());
        for(int i=0;i<8&&app.CaptureStatus().Phase==SessionPhase.Planning&&app.GetOwnStrategicFleets(B).Single(f=>f.FleetId.Value==FB).LocationNodeId.Value!="nav.bajor";i++)Advance(app);
        if(app.CaptureStatus().Phase==SessionPhase.Planning)
        {Order(app,A,fleet,CampaignTravelIntent.StayEngage);Advance(app);}
        Assert.Equal(SessionPhase.AwaitingCombat,app.CaptureStatus().Phase);
        Assert.NotEmpty(app.GetEncounters(A));
    }
    private static CommitCampaignTacticalRoundDto Round(ICampaignIntegratedPlaytestSession app,string id,string actor)
    {
        var v=app.Observe(id,actor);return new CommitCampaignTacticalRoundDto {ActorId=actor,EncounterId=id,
            CommandId="command.integrated.round."+id+"."+actor+"."+v.Round,ExpectedTurn=v.Turn,ExpectedRound=v.Round,
            ExpectedBoundaryRevision=v.BoundaryRevision,Replacements=new List<CampaignTacticalFleetOrderDto>()};
    }
    private static ICampaignIntegratedPlaytestSession Complete(CampaignIntegratedPlaytestPackageDto p,ICampaignIntegratedPlaytestSession app,bool reload=false,bool reverse=false)
    {
        foreach(var id in app.GetEncounters(A))while(!app.Observe(id,A).Complete)
        {
            var commands=new[]{A,B}.Select(a=>Round(app,id,a)).ToArray();if(reverse)Array.Reverse(commands);
            Assert.True(app.CommitRound(commands[0]).Accepted);if(reload)app=Reload(p,app);
            Assert.True(app.CommitRound(commands[1]).Accepted);if(reload)app=Reload(p,app);
        }
        var s=app.CaptureStatus();Assert.True(app.FinalizeBatch(s.Turn,s.CampaignRevision).Accepted);
        Assert.False(app.FinalizeBatch(s.Turn,s.CampaignRevision).Accepted);
        return app;
    }

    [Fact]
    public void OneSessionChangesRealResearchPaysProductionAndProvisionsImmutableShipOnce()
    {
        var p=Package();var app=CampaignIntegratedPlaytestApplication.StartPackage(p);
        var own=app.CaptureStrategy(A);Assert.Equal(70m,own.Research.SciencePercent);Assert.Equal(6,own.Research.Science.Count);
        var command=Command(app,CommandKind.SetResearchAllocation);command.SetResearchAllocation=new SetResearchAllocationPayload {ScienceAllocationUnits=360};
        Assert.True(app.SubmitStrategicCommand(command).Accepted);
        var cash=app.CaptureStrategy(A).Civilization.Credits;
        var design=app.CaptureStrategy(A).Construction.Single(o=>o.ColonyId.Value=="colony.sol").Design;
        Assert.True(app.SubmitStrategicCommand(Enqueue(app)).Accepted);
        Assert.Equal(cash-design.CreditCost,app.CaptureStrategy(A).Civilization.Credits);
        var queued=app.ExportIntegratedSave().Physical!.Core.Campaign.GameState.Colonies!.Single(c=>c.ColonyId=="colony.sol").ProductionQueue!.Single();
        Assert.Equal(design.IndustryCost,queued.RequiredIndustry);Assert.Equal(0,queued.CompletedIndustry);
        Advance(app);
        Assert.All(app.CaptureStrategy(A).Research.Science,c=>Assert.True(c.Progress>0));
        Assert.Equal(60m,app.CaptureStrategy(A).Research.SciencePercent);
        var after=app.ExportIntegratedSave().Physical!.Core.Campaign.GameState.Colonies!.Single(c=>c.ColonyId=="colony.sol").ProductionQueue!.Single();
        Assert.True(after.CompletedIndustry>0&&after.CompletedIndustry<after.RequiredIndustry);
        app=Reload(p,app);
        for(int i=0;i<100&&app.ExportIntegratedSave().BuiltShips!.Count==0;i++)Advance(app);
        var save=app.ExportIntegratedSave();var built=Assert.Single(save.BuiltShips!);
        var source=p.Physical!.CorePackage!.Campaign!.Definitions.AuthoredTacticalEquipment!.InitialShipProvisions!.Single(s=>s.ShipId==built.SourceProvisionShipId);
        var pool=save.Physical!.Core.Pools.Single(s=>s.ShipId==built.ShipId);
        Assert.Equal(Json(source.InitialShields),Json(pool.Shields));Assert.Equal(Json(source.InitialAmmunition),Json(pool.Ammunition));
        var actual=save.Physical.Core.Campaign.GameState.Ships!.Single(s=>s.ShipId==built.ShipId);
        Assert.Equal(Design,actual.ShipDesignDefinitionId);Assert.Null(actual.Blueprint);Assert.Equal(220,actual.CurrentHull);
        Assert.Equal(FA,actual.FleetId);Assert.Equal(queued.QueueItemId,built.QueueItemId);
        Assert.Empty(save.Physical.Core.Campaign.GameState.Colonies!.Single(c=>c.ColonyId=="colony.sol").ProductionQueue!);
        app=Reload(p,app);Advance(app);Assert.Single(app.ExportIntegratedSave().BuiltShips!);
        Assert.Equal(Json(pool),Json(app.ExportIntegratedSave().Physical!.Core.Pools.Single(s=>s.ShipId==built.ShipId)));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ProducedShipMovesIntoRealContactFightsAndReloadsSameCampaign(bool spawnFleet)
    {
        var p=Package();var app=CampaignIntegratedPlaytestApplication.StartPackage(p);
        if(spawnFleet){Order(app,A,FA,CampaignTravelIntent.Leave,"nav.bajor");Advance(app);}
        Build(app);var built=app.ExportIntegratedSave().BuiltShips!.Single();var fleet=built.FleetIdAtConstruction!;
        Assert.Equal(!spawnFleet,fleet==FA);
        Assert.Contains(app.GetOwnTravelOrders(A),o=>o.FleetId==fleet&&o.Intent==CampaignTravelIntent.Stay);
        app=Reload(p,app);Contact(app,fleet);
        var id=app.GetEncounters(A).Single();Assert.Contains(app.Observe(id,A).OwnShips,s=>s.Condition.ShipId==built.ShipId);
        Assert.All(app.CaptureStrategy(A).Construction,o=>Assert.Equal(CommandErrorCode.CombatPending,o.UnavailableReason));
        Assert.False(app.SubmitStrategicCommand(Enqueue(app)).Accepted);
        app=Complete(p,app,true);var final=app.ExportIntegratedSave();Assert.Equal(SessionPhase.Planning,app.CaptureStatus().Phase);
        Assert.Single(final.BuiltShips!);Assert.Empty(app.GetEncounters(A));
        var survivors=final.Physical!.Core.Pools.Where(s=>s.ShipId==built.ShipId).ToList();
        Assert.Single(survivors);
        Assert.All(survivors,s=>Assert.True(s.Ammunition.Single().Stock<7));
        app=Reload(p,app);Assert.Equal(Json(final),Json(app.ExportIntegratedSave()));
        Advance(app);Assert.Single(app.ExportIntegratedSave().BuiltShips!);Assert.Empty(app.GetEncounters(A));
        Assert.All(survivors,s=>Assert.Equal(Json(s),Json(app.ExportIntegratedSave().Physical!.Core.Pools.Single(n=>n.ShipId==s.ShipId))));
    }

    [Fact]
    public void ActualEngineeringRequiresEarnedScienceThenAdvancesAndPersistsWithoutFreeUnlock()
    {
        var p=Package();var app=CampaignIntegratedPlaytestApplication.StartPackage(p);
        var initial=app.CaptureStrategy(A).Research.Engineering.Single();Assert.False(initial.CanTarget);
        var target=Command(app,CommandKind.SetEngineeringTarget);target.SetEngineeringTarget=new SetEngineeringTargetPayload {TargetDefinitionId=initial.Definition.DefinitionId.Value};
        var before=Json(app.ExportIntegratedSave());Assert.False(app.SubmitStrategicCommand(target).Accepted);Assert.Equal(before,Json(app.ExportIntegratedSave()));
        var science=Command(app,CommandKind.SetResearchAllocation);science.SetResearchAllocation=new SetResearchAllocationPayload {ScienceAllocationUnits=600};
        Assert.True(app.SubmitStrategicCommand(science).Accepted);
        for(int i=0;i<300&&!app.CaptureStrategy(A).Research.Engineering.Single().CanTarget;i++)Advance(app);
        Assert.True(app.CaptureStrategy(A).Research.Engineering.Single().CanTarget);
        target=Command(app,CommandKind.SetEngineeringTarget);target.SetEngineeringTarget=new SetEngineeringTargetPayload {TargetDefinitionId=initial.Definition.DefinitionId.Value};
        Assert.True(app.SubmitStrategicCommand(target).Accepted);
        var engineering=Command(app,CommandKind.SetResearchAllocation);engineering.SetResearchAllocation=new SetResearchAllocationPayload {ScienceAllocationUnits=0};
        Assert.True(app.SubmitStrategicCommand(engineering).Accepted);Advance(app);
        Assert.True(app.CaptureStrategy(A).Research.Engineering.Single().Progress>0);
        app=Reload(p,app);Assert.True(app.CaptureStrategy(A).Research.Engineering.Single().Progress>0);
        for(int i=0;i<100&&app.CaptureStrategy(A).Research.Engineering.Single().Band!=EngineeringMaturityBand.Mature;i++)Advance(app);
        Assert.Equal(EngineeringMaturityBand.Mature,app.CaptureStrategy(A).Research.Engineering.Single().Band);
        Assert.DoesNotContain(app.CaptureStrategy(A).Construction,o=>o.Design.DefinitionId.Value=="ship-design.advanced-scout");
    }

    [Theory]
    [InlineData("stale-turn")][InlineData("stale-revision")][InlineData("foreign")][InlineData("malformed")]
    [InlineData("unsupported-static")][InlineData("blueprint")][InlineData("credits")]
    public void InvalidProductionNeverMutatesWholeRoot(string kind)
    {
        var app=CampaignIntegratedPlaytestApplication.StartPackage(Package());var command=Enqueue(app);
        switch(kind)
        {
            case "stale-turn":command.ExpectedTurn--;break;
            case "stale-revision":command.ExpectedRevision--;break;
            case "foreign":command.EnqueueProduction!.ColonyId="colony.cardassia";break;
            case "malformed":command.SetResearchAllocation=new SetResearchAllocationPayload {ScienceAllocationUnits=300};break;
            case "unsupported-static":command.EnqueueProduction!.DefinitionId="ship-design.scout";break;
            case "blueprint":command.Kind=CommandKind.EnqueueBlueprintConstruction;command.EnqueueProduction=null;command.EnqueueBlueprintConstruction=new EnqueueBlueprintConstructionPayload {ColonyId="colony.sol",BlueprintId="blueprint.arbitrary",Revision=1};break;
            case "credits":Assert.True(app.SubmitStrategicCommand(command).Accepted);command=Enqueue(app);break;
        }
        var before=Json(app.ExportIntegratedSave());Assert.False(app.SubmitStrategicCommand(command).Accepted);Assert.Equal(before,Json(app.ExportIntegratedSave()));
    }

    [Fact]
    public void DuplicateAndPaidCancellationDoNotCreateResourcesOrSources()
    {
        var app=CampaignIntegratedPlaytestApplication.StartPackage(Package());var cash=app.CaptureStrategy(A).Civilization.Credits;var enqueue=Enqueue(app);
        Assert.True(app.SubmitStrategicCommand(enqueue).Accepted);var before=Json(app.ExportIntegratedSave());
        Assert.Equal(CampaignTacticalError.DuplicateCommand,app.SubmitStrategicCommand(enqueue).Error);Assert.Equal(before,Json(app.ExportIntegratedSave()));
        var queue=app.CaptureStrategy(A).Colonies.Single(c=>c.ColonyId.Value=="colony.sol").State.ProductionQueue.Single();
        var cancel=Command(app,CommandKind.CancelProduction);cancel.CancelProduction=new CancelProductionPayload {ColonyId="colony.sol",QueueItemId=queue.QueueItemId.Value};
        Assert.True(app.SubmitStrategicCommand(cancel).Accepted);Assert.Equal(cash,app.CaptureStrategy(A).Civilization.Credits);
        Assert.Empty(app.ExportIntegratedSave().BuiltShips!);Assert.False(app.SubmitStrategicCommand(cancel).Accepted);
    }

    [Fact]
    public void CapturesAreDetachedActorScopedAndHideEnemyEconomyResearchAndQueue()
    {
        var p=Package();p.Physical!.CorePackage!.Campaign!.InitialState.Civilizations!.Single(c=>c.CivilizationId==B).Credits=987654;
        var app=CampaignIntegratedPlaytestApplication.StartPackage(p);var own=app.CaptureStrategy(A);var before=Json(own);
        Assert.All(own.Colonies,c=>Assert.Equal(A,c.OwnerCivilizationId!.Value.Value));
        Assert.All(own.Ships,s=>Assert.Equal(A,s.OwnerCivilizationId.Value));Assert.All(own.Fleets,f=>Assert.Equal(A,f.OwnerCivilizationId.Value));
        Assert.DoesNotContain("987654",before);Assert.DoesNotContain("colony.cardassia",before);Assert.DoesNotContain("fleet.cardassian-scout",before);
        Assert.Throws<ArgumentException>(()=>app.CaptureStrategy("civ.unknown"));
        Assert.True(app.SubmitStrategicCommand(Enqueue(app)).Accepted);Advance(app);Assert.Equal(before,Json(own));
        Assert.Throws<InvalidOperationException>(()=>app.ExportSave());
    }

    [Theory]
    [InlineData(0)][InlineData(1)][InlineData(2)][InlineData(4)]
    public void UnsupportedSaveVersionsRejectWithoutMigrating(int format)
    {var p=Package();var app=CampaignIntegratedPlaytestApplication.StartPackage(p);var save=app.ExportIntegratedSave();save.FormatVersion=format;Assert.Throws<ArgumentException>(()=>CampaignIntegratedPlaytestApplication.LoadSave(p,save));}

    [Theory]
    [InlineData("missing-registry")][InlineData("substitute-source")][InlineData("missing-pool")][InlineData("refill")][InlineData("foreign-owner")]
    public void ForgedProducedProvenanceAndStocksReject(string kind)
    {
        var p=Package();var app=CampaignIntegratedPlaytestApplication.StartPackage(p);Build(app);var save=app.ExportIntegratedSave();var born=save.BuiltShips!.Single();
        switch(kind)
        {
            case "missing-registry":save.BuiltShips!.Clear();break;
            case "substitute-source":born.SourceProvisionShipId="ship.cardassian-scout";break;
            case "missing-pool":save.Physical!.Core.Pools.RemoveAll(s=>s.ShipId==born.ShipId);break;
            case "refill":save.Physical!.Core.Pools.Single(s=>s.ShipId==born.ShipId).Ammunition.Single().Stock=12;break;
            case "foreign-owner":born.OwnerCivilizationId=B;break;
        }
        Assert.Throws<ArgumentException>(()=>CampaignIntegratedPlaytestApplication.LoadSave(p,save));
    }

    [Theory]
    [InlineData((int)StrategicTurnPhase.AiPlanning)][InlineData((int)StrategicTurnPhase.Economy)][InlineData((int)StrategicTurnPhase.Research)]
    [InlineData((int)StrategicTurnPhase.Production)][InlineData((int)StrategicTurnPhase.Movement)][InlineData((int)StrategicTurnPhase.EncounterCreation)]
    public void FailedAdvanceIncludingAfterProvisioningRollsBackWholeRoot(int failure)
    {
        var p=Package();var app=CampaignIntegratedPlaytestApplication.StartPackage(p);Assert.True(app.SubmitStrategicCommand(Enqueue(app)).Accepted);
        for(int i=0;i<100;i++)
        {
            var trial=Reload(p,app);Advance(trial);if(trial.ExportIntegratedSave().BuiltShips!.Count>0)break;Advance(app);
        }
        Assert.Empty(app.ExportIntegratedSave().BuiltShips!);var before=Json(app.ExportIntegratedSave());
        Backend(app).PhysicalPhaseProbe=phase=>(int)phase!=failure;var stamp=app.CaptureStatus();Assert.False(app.AdvanceTurn(stamp.Turn,stamp.CampaignRevision).Accepted);
        Assert.Equal(before,Json(app.ExportIntegratedSave()));Backend(app).PhysicalPhaseProbe=null;Advance(app);Assert.Single(app.ExportIntegratedSave().BuiltShips!);
    }

    [Fact]
    public void FailedFinalizationKeepsStagedBattleAndConstructionProvenanceThenCommitsOnce()
    {
        var p=Package();var app=CampaignIntegratedPlaytestApplication.StartPackage(p);Build(app);Contact(app,FA);
        foreach(var id in app.GetEncounters(A))while(!app.Observe(id,A).Complete)
        {var a=Round(app,id,A);var b=Round(app,id,B);Assert.True(app.CommitRound(a).Accepted);Assert.True(app.CommitRound(b).Accepted);}
        var before=Json(app.ExportIntegratedSave());Backend(app).FinalizationProbe=_=>throw new InvalidOperationException("late aftermath failure");
        var stamp=app.CaptureStatus();Assert.False(app.FinalizeBatch(stamp.Turn,stamp.CampaignRevision).Accepted);Assert.Equal(before,Json(app.ExportIntegratedSave()));
        Backend(app).FinalizationProbe=null;Assert.True(app.FinalizeBatch(stamp.Turn,stamp.CampaignRevision).Accepted);
        Assert.Single(app.ExportIntegratedSave().BuiltShips!);Assert.False(app.FinalizeBatch(stamp.Turn,stamp.CampaignRevision).Accepted);
    }

    [Fact]
    public void CanonicalReversedRepeatedReorderedAndWaitingReloadRunsAgree()
    {
        string Run(int mode)
        {
            var p=Package();if(mode==3)
            {var g=p.Physical!.CorePackage!.Campaign!.InitialState;g.Colonies!.Reverse();g.Civilizations!.Reverse();g.Fleets!.Reverse();p.Physical.InitialContacts!.Reverse();p.Physical.InitialOrders!.Reverse();}
            var app=CampaignIntegratedPlaytestApplication.StartPackage(p);Build(app);Contact(app,FA);
            app=Complete(p,app,mode==4,mode==1);Advance(app);return Json(app.ExportIntegratedSave());
        }
        var canonical=Run(0);for(int mode=1;mode<=4;mode++)Assert.Equal(canonical,Run(mode));
    }

    [Theory]
    [InlineData("weights")][InlineData("stale")][InlineData("unknown-actor")][InlineData("duplicate")]
    public void ScienceCommandsUseSharedValidationAndRejectedChangesAreAtomic(string kind)
    {
        var app=CampaignIntegratedPlaytestApplication.StartPackage(Package());var command=Command(app,CommandKind.SetScienceAllocation);
        command.SetScienceAllocation=new SetScienceAllocationPayload {Allocations=Enum.GetValues<ScienceDiscipline>().Select((d,i)=>new ScienceAllocationDto {Discipline=d,Weight=i==0?150:i==1?50:100}).ToList()};
        if(kind=="weights")command.SetScienceAllocation.Allocations![0].Weight++;
        if(kind=="stale")command.ExpectedTurn++;
        if(kind=="unknown-actor")command.ActorCivilizationId="civ.unknown";
        if(kind=="duplicate")Assert.True(app.SubmitStrategicCommand(command).Accepted);
        var before=Json(app.ExportIntegratedSave());Assert.False(app.SubmitStrategicCommand(command).Accepted);Assert.Equal(before,Json(app.ExportIntegratedSave()));
    }

    [Fact]
    public void SixWayAllocationAppliesPersistsAndNeverMutatesDetachedView()
    {
        var p=Package();var app=CampaignIntegratedPlaytestApplication.StartPackage(p);var initial=app.CaptureStrategy(A);
        var command=Command(app,CommandKind.SetScienceAllocation);
        command.SetScienceAllocation=new SetScienceAllocationPayload {Allocations=Enum.GetValues<ScienceDiscipline>().Select((d,i)=>new ScienceAllocationDto {Discipline=d,Weight=i==0?150:i==1?50:100}).ToList()};
        Assert.True(app.SubmitStrategicCommand(command).Accepted);var state=app.CaptureStrategy(A).Civilization.Research!;
        Assert.Equal(600,state.ScienceAllocations.Sum(a=>a.Weight));Assert.Equal(100,initial.Civilization.Research!.ScienceAllocations.First().Weight);
        app=Reload(p,app);Assert.Equal(Json(state.ScienceAllocations),Json(app.CaptureStrategy(A).Civilization.Research!.ScienceAllocations));Advance(app);
        Assert.All(app.CaptureStrategy(A).Research.Science,s=>Assert.True(s.Progress>0));
    }

    [Fact]
    public void ProducedBattleRetainsPrivateWaitingCommandsAndRejectsStaleDuplicateForeignRound()
    {
        var p=Package();var app=CampaignIntegratedPlaytestApplication.StartPackage(p);Build(app);Contact(app,FA);
        var id=app.GetEncounters(A).Single();var a=Round(app,id,A);Assert.True(app.CommitRound(a).Accepted);app=Reload(p,app);
        Assert.True(app.Observe(id,A).OwnCommitmentAccepted);Assert.False(app.Observe(id,B).OwnCommitmentAccepted);
        var before=Json(app.ExportIntegratedSave());Assert.False(app.CommitRound(a).Accepted);Assert.Equal(before,Json(app.ExportIntegratedSave()));
        var b=Round(app,id,B);b.ExpectedRound++;Assert.False(app.CommitRound(b).Accepted);Assert.Equal(before,Json(app.ExportIntegratedSave()));
        b=Round(app,id,B);b.Replacements!.Add(new CampaignTacticalFleetOrderDto {FleetId=FA,Maneuver=CampaignFleetManeuver.Hold});
        Assert.False(app.CommitRound(b).Accepted);Assert.Equal(before,Json(app.ExportIntegratedSave()));
    }

    [Theory]
    [InlineData("format")][InlineData("default-activation")][InlineData("source")][InlineData("blueprint-static")][InlineData("null-registry")]
    public void MalformedPackagesAndNullSaveRegistryFailClosed(string kind)
    {
        var p=Package();if(kind=="null-registry")
        {var app=CampaignIntegratedPlaytestApplication.StartPackage(p);var save=app.ExportIntegratedSave();save.BuiltShips!.Add(null!);Assert.Throws<ArgumentException>(()=>CampaignIntegratedPlaytestApplication.LoadSave(p,save));return;}
        if(kind=="format")p.FormatVersion=2;
        if(kind=="default-activation")p.DefaultActivationAllowed=true;
        if(kind=="source")p.ConstructionTemplates!.Single().SourceProvisionShipId="ship.unknown";
        if(kind=="blueprint-static")p.ConstructionTemplates!.Single().ShipDesignDefinitionId="ship-design.scout";
        Assert.Throws<ArgumentException>(()=>CampaignIntegratedPlaytestApplication.StartPackage(p));
    }
}
