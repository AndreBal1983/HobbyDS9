using System.Text.Json;
using System.Security.Cryptography;
using System.Text;
using HobbyDS9.Application;
using HobbyDS9.Contracts;
using Xunit;
using HobbyDS9.Simulation;
namespace HobbyDS9.Simulation.Tests;
public sealed class SingleplayerDemoTests
{
    private static T Clone<T>(T x)=>JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(x))!;
    private static CampaignTacticalReviewSession Backend(ICampaignSingleplayerDemoSession app)=>(CampaignTacticalReviewSession)app.GetType()
        .GetField("session",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)!.GetValue(app)!;
    private static void Advance(ICampaignSingleplayerDemoSession app){var s=app.CaptureStatus();Assert.True(app.AdvanceTurn(s.Turn,s.CampaignRevision).Accepted);}
    private static void Route(ICampaignSingleplayerDemoSession app)
    {
        var s=app.CaptureStatus();Assert.True(app.SubmitTravelOrder(new SubmitCampaignTravelOrderDto {ActorId="civ.federation",
            CommandId="command.demo.test-route",ExpectedTurn=s.Turn,ExpectedRevision=s.CampaignRevision,
            Order=new CampaignTravelOrderDto {FleetId="fleet.federation-scout",Intent=CampaignTravelIntent.Leave,RouteNodeIds=new(){"nav.bajor"}}}).Accepted);
    }
    private static ICampaignSingleplayerDemoSession Pending()
    {
        var app=CampaignSingleplayerDemoApplication.StartPackage(Package());Route(app);
        for(int i=0;i<6&&app.CaptureStatus().Phase==SessionPhase.Planning;i++)Advance(app);
        Assert.Equal(SessionPhase.AwaitingCombat,app.CaptureStatus().Phase);return app;
    }
    [Fact] public void InitialAiTurnStagesSuccessfully()
    {
        var app=CampaignSingleplayerDemoApplication.StartPackage(Package());var s=app.CaptureStatus();var r=app.AdvanceTurn(s.Turn,s.CampaignRevision);
        var backend=app.GetType().GetField("session",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)!.GetValue(app)!;
        var failure=backend.GetType().GetProperty("LastPhysicalFailure",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)!.GetValue(backend);
        Assert.True(r.Accepted,failure?.ToString());
    }
    internal static CampaignSingleplayerDemoPackageDto Package()=>SingleplayerDemoPackageReader.Read(Path.Combine(AppContext.BaseDirectory,"scenarios/singleplayer-demo-v1"));
    [Fact] public void ActualSingleHumanCampaignBuildsFightsAndReloadsDeterministically()
    {
        var p=Package();var first=SingleplayerDemoWalkthrough.Run(p);var json=JsonSerializer.Serialize(first);
        Assert.Equal("bb76fb9f03299f92ca009b46bc931ff5bd75a27018c02d31c6f4068b21ed12a4",p.PackageHash);
        Assert.Equal("4997ed0183765fa77fd7585182e28bbc4be452bcc428a1968be6337eed249204",Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json))).ToLowerInvariant());
        Assert.Equal(json,JsonSerializer.Serialize(SingleplayerDemoWalkthrough.Run(p,true)));
        Assert.Equal(json,JsonSerializer.Serialize(SingleplayerDemoWalkthrough.Run(p,false,true)));
        Assert.Equal(json,JsonSerializer.Serialize(SingleplayerDemoWalkthrough.Run(p)));
        var reordered=Clone(p);reordered.Integrated!.Physical!.CorePackage!.Campaign!.InitialState.Civilizations!.Reverse();
        reordered.Integrated.Physical.CorePackage.Campaign.InitialState.Fleets!.Reverse();
        reordered.Integrated.Physical.CorePackage.Campaign.Definitions.ShipDesigns!.Reverse();
        reordered.Integrated.Physical.InitialContacts!.Reverse();
        Assert.Equal(json,JsonSerializer.Serialize(SingleplayerDemoWalkthrough.Run(reordered)));
    }
    [Fact] public void OpponentIsNotPlayerControlledAndNestedExportFailsClosed()
    {
        var p=Package();var app=CampaignSingleplayerDemoApplication.StartPackage(p);
        Assert.Throws<ArgumentException>(()=>app.CaptureStrategy(p.OpponentActorId!));
        Assert.Throws<ArgumentException>(()=>app.GetEncounters(p.OpponentActorId!));
        Assert.Throws<ArgumentException>(()=>app.ExportIntegratedSave());Assert.Throws<InvalidOperationException>(()=>app.ExportSave());
        Assert.Throws<ArgumentException>(()=>Backend(app).ExportSave());Assert.Throws<ArgumentException>(()=>Backend(app).ExportTravelSave());
        Assert.False(CampaignIntegratedPlaytestApplication.StartPackage(IntegratedPlaytestTests.Package()) is ICampaignSingleplayerDemoSession);
        Assert.Equal(CampaignDemoOutcome.Active,app.CaptureDemo().Outcome);
    }
    [Theory]
    [InlineData(0)][InlineData(1)][InlineData(2)][InlineData(3)][InlineData(4)][InlineData(5)]
    public void FailedWholeTurnDoesNotSpendAiResourcesOrPublishRoutes(int phase)
    {
        var app=CampaignSingleplayerDemoApplication.StartPackage(Package());var before=JsonSerializer.Serialize(app.ExportDemoSave());
        var backend=Backend(app);backend.GetType().GetProperty("PhysicalPhaseProbe",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)!
            .SetValue(backend,new Func<StrategicTurnPhase,bool>(p=>(int)p!=phase));
        var s=app.CaptureStatus();Assert.False(app.AdvanceTurn(s.Turn,s.CampaignRevision).Accepted);Assert.Equal(before,JsonSerializer.Serialize(app.ExportDemoSave()));
        backend.GetType().GetProperty("PhysicalPhaseProbe",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)!.SetValue(backend,null);
        Advance(app);Assert.NotEqual(before,JsonSerializer.Serialize(app.ExportDemoSave()));
    }
    [Fact] public void AiBuysThroughRealFeesAndNeverChangesHumanOrders()
    {
        var app=CampaignSingleplayerDemoApplication.StartPackage(Package());var human=JsonSerializer.Serialize(app.GetOwnTravelOrders("civ.federation"));
        Advance(app);var save=app.ExportDemoSave();var game=save.Integrated!.Physical!.Core.Campaign.GameState;
        Assert.Equal(human,JsonSerializer.Serialize(app.GetOwnTravelOrders("civ.federation")));
        Assert.Single(game.Colonies!.Single(c=>c.OwnerCivilizationId=="civ.cardassian").ProductionQueue!);
        Assert.True(game.Civilizations!.Single(c=>c.CivilizationId=="civ.cardassian").Credits<300);
        Assert.Equal(1,save.OpponentPlan!.LastPlannedTurn);
        var s=app.CaptureStatus();var before=JsonSerializer.Serialize(save);Assert.False(app.AdvanceTurn(1,s.CampaignRevision).Accepted);
        Assert.Equal(before,JsonSerializer.Serialize(app.ExportDemoSave()));
    }
    [Theory][InlineData("policy")][InlineData("goal")][InlineData("actor")][InlineData("boundary")][InlineData("format")][InlineData("hash")]
    public void DemoLoaderRejectsForgedOrOtherProfilePlan(string field)
    {
        var p=Package();var save=CampaignSingleplayerDemoApplication.StartPackage(p).ExportDemoSave();
        switch(field){case "policy":save.OpponentPlan!.PolicyId="invented";break;case "goal":save.OpponentPlan!.GoalNodeId="nav.sol";break;
            case "actor":save.OpponentPlan!.ActorId="civ.federation";break;case "boundary":save.OpponentPlan!.LastPlannedTurn=99;break;
            case "format":save.FormatVersion=3;break;default:save.PackageHash=new string('a',64);break;}
        Assert.Throws<ArgumentException>(()=>CampaignSingleplayerDemoApplication.LoadSave(p,save));
    }
    [Fact] public void PendingAiCommitmentSurvivesReloadAndDoesNotResolveHumanRound()
    {
        var app=Pending();var save=app.ExportDemoSave();var b=Assert.Single(save.Integrated!.Physical!.Core.Battles);
        Assert.Single(b.Commitments);Assert.Equal("civ.cardassian",b.Commitments.Single().ActorId);Assert.Equal(1,b.State.NextRound);
        var loaded=CampaignSingleplayerDemoApplication.LoadSave(Package(),Clone(save));Assert.Equal(JsonSerializer.Serialize(save),JsonSerializer.Serialize(loaded.ExportDemoSave()));
        var o=loaded.Observe(b.State.EncounterId,"civ.federation");Assert.False(o.OwnCommitmentAccepted);Assert.Empty(o.Reports);
        Assert.Equal(CampaignTacticalError.ActorNotControlled,loaded.CommitRound(new CommitCampaignTacticalRoundDto {ActorId="civ.cardassian"}).Error);
        Assert.Equal(CampaignTacticalError.IncompleteBatch,loaded.FinalizeBatch(o.Turn,loaded.CaptureStatus().CampaignRevision).Error);
    }
    [Theory][InlineData("maneuver")][InlineData("focus")][InlineData("id")][InlineData("missing")]
    public void ForgedAiChoicesAreRejectedByRoundReplay(string field)
    {
        var app=Pending();var save=app.ExportDemoSave();var b=save.Integrated!.Physical!.Core.Battles.Single();
        switch(field){case "maneuver":b.Commitments.Single().Orders[0].Maneuver=CampaignFleetManeuver.Flank;break;
            case "focus":b.Commitments.Single().Orders[0].FocusFleetId=null;break;case "id":b.Commitments.Single().CommandId="ai-command.forged";break;default:b.Commitments.Clear();break;}
        Assert.Throws<ArgumentException>(()=>CampaignSingleplayerDemoApplication.LoadSave(Package(),save));
    }
    [Fact] public void WaitingWithoutEngagingTerminatesAtDeadlineWithoutContactExpirySoftlock()
    {
        var app=CampaignSingleplayerDemoApplication.StartPackage(Package());for(int i=0;i<12;i++)Advance(app);
        Assert.Equal(CampaignDemoOutcome.Defeat,app.CaptureDemo().Outcome);
        var before=JsonSerializer.Serialize(app.ExportDemoSave());var s=app.CaptureStatus();Assert.False(app.AdvanceTurn(s.Turn,s.CampaignRevision).Accepted);
        Assert.Equal(CampaignTacticalError.WrongPhase,app.SubmitStrategicCommand(new CommandEnvelope {ActorCivilizationId="civ.federation",
            CommandId="command.demo.after-end",Kind=CommandKind.EnqueueProduction,ExpectedTurn=s.Turn,ExpectedRevision=s.CampaignRevision,
            EnqueueProduction=new EnqueueProductionPayload {ColonyId="colony.sol",DefinitionId="ship-design.source-cruiser"}}).Error);
        Assert.Equal(CampaignTacticalError.WrongPhase,app.SetApproachCloak("civ.federation","command.demo.cloak-end","fleet.federation-scout",true,s.Turn,s.CampaignRevision).Error);
        Assert.Equal(CampaignTacticalError.WrongPhase,app.SubmitTravelOrder(new SubmitCampaignTravelOrderDto {ActorId="civ.federation",
            CommandId="command.demo.travel-end",ExpectedTurn=s.Turn,ExpectedRevision=s.CampaignRevision,Order=new CampaignTravelOrderDto {
                FleetId="fleet.federation-scout",Intent=CampaignTravelIntent.Stay,RouteNodeIds=new()}}).Error);
        Assert.Equal(before,JsonSerializer.Serialize(app.ExportDemoSave()));
    }
    [Theory][InlineData(0,1,CampaignDemoOutcome.Defeat)][InlineData(0,0,CampaignDemoOutcome.Defeat)]
    [InlineData(20,10,CampaignDemoOutcome.Victory)][InlineData(10,20,CampaignDemoOutcome.Defeat)][InlineData(10,10,CampaignDemoOutcome.Stalemate)]
    public void ExplicitDeadlineComparisonDefinesAllOutcomes(long human,long opponent,CampaignDemoOutcome expected)
        =>Assert.Equal(expected,CampaignTacticalReviewSession.DemoDeadlineOutcome(human,opponent));
    [Fact] public void PureTacticalPolicyAcceptsOnlyConditionViewsAndIsOrderIndependent()
    {
        var own=new[]{new CampaignTacticalCondition("ship.own","fleet.own","Cruiser",220,220,new long[]{18,25,12,0})};
        var seen=new[]{new CampaignTacticalCondition("ship.beta","fleet.beta","Other",100,200,new long[]{0,0,0,0}),
            new CampaignTacticalCondition("ship.alpha","fleet.alpha","Other",100,200,new long[]{0,0,0,0})};
        var orders=DemoAiPolicy.Orders(own,seen);Assert.Equal("fleet.alpha",orders.Single().FocusFleetId);Assert.Equal(CampaignFleetManeuver.Charge,orders.Single().Maneuver);
        Assert.Equal(JsonSerializer.Serialize(orders),JsonSerializer.Serialize(DemoAiPolicy.Orders(own,seen.Reverse().ToArray())));
        Assert.Null(DemoAiPolicy.Orders(own,Array.Empty<CampaignTacticalCondition>()).Single().FocusFleetId);
        Assert.Equal(CampaignFleetManeuver.Screen,DemoAiPolicy.Orders(own,Array.Empty<CampaignTacticalCondition>()).Single().Maneuver);
    }
    [Fact] public void InsufficientLocalDilithiumDoesNotGiveAiFreeShip()
    {
        var p=Package();foreach(var colony in p.Integrated!.Physical!.CorePackage!.Campaign!.InitialState.Colonies!.Where(c=>c.OwnerCivilizationId==p.OpponentActorId))
            foreach(var stock in colony.Stockpiles!.Where(s=>s.ResourceDefinitionId=="resource.dilithium"))stock.Amount=0;
        var app=CampaignSingleplayerDemoApplication.StartPackage(p);Advance(app);
        Assert.Empty(app.ExportDemoSave().Integrated!.Physical!.Core.Campaign.GameState.Colonies!.Where(c=>c.OwnerCivilizationId==p.OpponentActorId).SelectMany(c=>c.ProductionQueue!));
        Assert.Empty(app.ExportDemoSave().Integrated!.BuiltShips!);
    }
    [Fact] public void LateFailureRollsBackCompletedAiAndHumanShips()
    {
        var app=CampaignSingleplayerDemoApplication.StartPackage(Package());var s=app.CaptureStatus();Assert.True(app.SubmitStrategicCommand(new CommandEnvelope {
            ActorCivilizationId="civ.federation",CommandId="command.demo.rollback-build",Kind=CommandKind.EnqueueProduction,ExpectedTurn=s.Turn,ExpectedRevision=s.CampaignRevision,
            EnqueueProduction=new EnqueueProductionPayload {ColonyId="colony.sol",DefinitionId="ship-design.source-cruiser"}}).Accepted);
        Advance(app);var before=JsonSerializer.Serialize(app.ExportDemoSave());var backend=Backend(app);
        backend.GetType().GetProperty("PhysicalPhaseProbe",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)!.SetValue(backend,new Func<StrategicTurnPhase,bool>(p=>p!=StrategicTurnPhase.EncounterCreation));
        s=app.CaptureStatus();Assert.False(app.AdvanceTurn(s.Turn,s.CampaignRevision).Accepted);Assert.Equal(before,JsonSerializer.Serialize(app.ExportDemoSave()));
        backend.GetType().GetProperty("PhysicalPhaseProbe",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)!.SetValue(backend,null);
        Advance(app);Assert.Contains(app.ExportDemoSave().Integrated!.BuiltShips!,b=>b.OwnerCivilizationId=="civ.federation");
    }
    [Fact] public void HumanCommitReplayCannotResolveAnotherRoundOrAutoFinalize()
    {
        var app=Pending();var id=app.GetEncounters("civ.federation").Single();var o=app.Observe(id,"civ.federation");
        var command=new CommitCampaignTacticalRoundDto {ActorId="civ.federation",CommandId="command.demo.once",EncounterId=id,ExpectedTurn=o.Turn,
            ExpectedRound=o.Round,ExpectedBoundaryRevision=o.BoundaryRevision,Replacements=new()};
        Assert.True(app.CommitRound(command).Accepted);Assert.Equal(2,app.Observe(id,"civ.federation").Round);
        var before=JsonSerializer.Serialize(app.ExportDemoSave());Assert.Equal(CampaignTacticalError.DuplicateCommand,app.CommitRound(command).Error);
        Assert.Equal(before,JsonSerializer.Serialize(app.ExportDemoSave()));Assert.Equal(SessionPhase.AwaitingCombat,app.CaptureStatus().Phase);
    }
    [Theory][InlineData(1UL)][InlineData(2UL)][InlineData(3UL)][InlineData(4UL)]
    public void SeededBoundedCampaignsDoNotStallOrLoseReloadDeterminism(ulong seed)
    {
        var p=Package();p.Integrated!.Physical!.CorePackage!.Campaign!.InitialState.RngState.State=seed;
        var first=SingleplayerDemoWalkthrough.Run(p);Assert.Equal(JsonSerializer.Serialize(first),JsonSerializer.Serialize(SingleplayerDemoWalkthrough.Run(p,true)));
        Assert.InRange(first.Integrated!.Physical!.Core.Campaign.GameState.TurnNumber,2,13);
        Assert.All(first.Integrated.Physical.Core.Campaign.GameState.Civilizations!,c=>Assert.True(c.Credits>=0));
    }
}
