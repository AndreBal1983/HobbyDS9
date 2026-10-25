using System.Text.Json;
using HobbyDS9.Application;
using HobbyDS9.Contracts;
using HobbyDS9.Simulation;
using Xunit;

namespace HobbyDS9.Simulation.Tests;

public sealed class PublishedConstructionProfileTests
{
    const string Human="civ.federation",Id="blueprint.optin.escort";
    static string Json<T>(T value)=>JsonSerializer.Serialize(value);
    static T Clone<T>(T value)=>JsonSerializer.Deserialize<T>(Json(value))!;
    internal static CampaignPublishedConstructionPackageDto Package()
    {var p=PlayableFleetTests.Package();p.ReplayOperationLimit=65536;return CampaignPublishedConstructionApplication.CreatePackage(p,PublishedTacticalSupportTests.Support(PublishedTacticalSupportTests.Load()),512);}
    internal static NaturalGalaxySettingsDto Settings(int size=30)=>new(){Map=new(){Seed="42",SystemCount=size,EmptyRegionCount=size,Width=size==30?750000:2500000,Height=size==30?600000:2000000,
        HomeCivilizationDefinitionIds=new(){"civilization.federation","civilization.cardassian"}}};
    static void Publish(ICampaignPublishedConstructionSession app,int revision)
    {
        var s=app.CaptureStatus();var draft=PublishedTacticalSupportTests.Draft(PublishedTacticalSupportTests.Load(),true);draft.DisplayName="Escort R"+revision;
        var r=app.SubmitStrategicCommand(new(){ActorCivilizationId=Human,CommandId="command.optin.publish."+revision,ExpectedTurn=s.Turn,ExpectedRevision=s.CampaignRevision,
            Kind=CommandKind.PublishShipBlueprint,PublishShipBlueprint=new(){BlueprintId=Id,Revision=revision,Draft=draft}});Assert.True(r.Accepted,r.Error.ToString());
    }
    static void Queue(ICampaignPublishedConstructionSession app)
    {
        var s=app.CaptureStatus();var r=app.SubmitStrategicCommand(new(){ActorCivilizationId=Human,CommandId="command.optin.queue."+s.CampaignRevision,
            ExpectedTurn=s.Turn,ExpectedRevision=s.CampaignRevision,Kind=CommandKind.EnqueueBlueprintConstruction,
            EnqueueBlueprintConstruction=new(){ColonyId="colony.sol",BlueprintId=Id,Revision=1}});Assert.True(r.Accepted,r.Error.ToString());
    }
    static CampaignTacticalReviewSession Backend(ICampaignPublishedConstructionSession app)=>(CampaignTacticalReviewSession)typeof(CampaignIntegratedPlaytestApplication)
        .GetField("session",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)!.GetValue(app)!;
    static void Order(ICampaignPublishedConstructionSession app,string fleet,CampaignTravelIntent intent,IReadOnlyList<string> route)
    {var s=app.CaptureStatus();var r=app.SubmitTravelOrder(new(){ActorId=Human,CommandId="command.optin.travel."+s.CampaignRevision,ExpectedTurn=s.Turn,
        ExpectedRevision=s.CampaignRevision,Order=new(){FleetId=fleet,Intent=intent,RouteNodeIds=route.ToList()}});Assert.True(r.Accepted,r.Error.ToString());}
    static void Advance(ICampaignPublishedConstructionSession app)
    {var s=app.CaptureStatus();var r=app.AdvanceTurn(s.Turn,s.CampaignRevision);Assert.True(r.Accepted,r.Error+" "+Backend(app).LastPhysicalFailure);}
    [Fact] public void ExplicitStartPublicationPaidQueueAndFrozenR1CompletionRoundTrip()
    {
        var p=Package();var app=CampaignPublishedConstructionApplication.StartGame(p,Settings());var initial=app.ExportPublishedConstructionSave();
        Assert.Equal(8,initial.FormatVersion);Assert.Equal(CampaignPublishedConstructionIdentity.EmbeddedCoreProfile,initial.Campaign!.Integrated!.Physical!.Core.ProfileId);
        Publish(app,1);var money=app.CaptureStrategy(Human).Civilization.Credits;Queue(app);var pending=app.ExportPublishedConstructionSave();
        Assert.True(app.CaptureStrategy(Human).Civilization.Credits<money);Assert.Single(pending.Campaign!.Integrated!.Physical!.Core.Campaign.GameState.Colonies!.Single(c=>c.ColonyId=="colony.sol").ProductionQueue!);
        app=CampaignPublishedConstructionApplication.LoadSave(p,Clone(pending));Assert.Equal(Json(pending),Json(app.ExportPublishedConstructionSave()));Publish(app,2);
        for(int i=0;i<100&&app.ExportPublishedConstructionSave().PublishedBuiltShips!.Count==0;i++)
        {var s=app.CaptureStatus();var r=app.AdvanceTurn(s.Turn,s.CampaignRevision);Assert.True(r.Accepted,r.Error.ToString());}
        var built=app.ExportPublishedConstructionSave();var ship=Assert.Single(built.PublishedBuiltShips!);Assert.Equal(1,ship.BlueprintRevision);
        Assert.False(app.InspectOwnShips(Human).Single(s=>s.Condition.ShipId==ship.ShipId).CloakCapable);
        app=CampaignPublishedConstructionApplication.LoadSave(p,Clone(built));Assert.Equal(Json(built),Json(app.ExportPublishedConstructionSave()));
        Assert.Throws<InvalidOperationException>(()=>app.ExportPlayableSave());Assert.Throws<ArgumentException>(()=>app.ExportIntegratedSave());
    }
    [Fact] public void NewDomainsCannotBeLoadedByV2AndCallerMutationsDoNotChangeSession()
    {
        var p=Package();var app=CampaignPublishedConstructionApplication.StartGame(p,Settings());var root=app.ExportPublishedConstructionSave();
        var v2=CampaignStaticAstronomyApplication.StartGame(p.BaseTemplate!,Settings()).ExportGeneratedSave();Assert.NotEqual(v2.PackageHash,root.PackageHash);
        v2.Campaign=root.Campaign!;Assert.Throws<ArgumentException>(()=>CampaignStaticAstronomyApplication.LoadSave(p.BaseTemplate!,v2));
        var bytes=Json(root);p.Support!.Components!.Clear();p.BaseTemplate!.Integrated!.Physical!.CorePackage!.Campaign!.InitialState.Colonies!.Clear();
        root.Map!.Layout.Regions.Clear();Assert.Equal(bytes,Json(app.ExportPublishedConstructionSave()));
    }
    [Fact] public void JournalLimitBindsWrapperAndGeneratedIdentityEvenBeforeAnyOperation()
    {
        var first=Package();var template=Clone(first.BaseTemplate!);template.ReplayOperationLimit=65535;
        var second=CampaignPublishedConstructionApplication.CreatePackage(template,first.Support!,first.RetainedRecordBudget!.Value);
        Assert.Equal(first.BaseTemplate!.PackageHash,second.BaseTemplate!.PackageHash);
        Assert.Equal(Json(first.Support),Json(second.Support));Assert.Equal(first.RetainedRecordBudget,second.RetainedRecordBudget);
        Assert.NotEqual(first.PackageHash,second.PackageHash);
        var a=CampaignPublishedConstructionApplication.StartGame(first,Settings()).ExportPublishedConstructionSave();
        var b=CampaignPublishedConstructionApplication.StartGame(second,Settings()).ExportPublishedConstructionSave();
        Assert.Equal(a.Map!.ContentHash,b.Map!.ContentHash);Assert.NotEqual(a.PackageHash,b.PackageHash);
        Assert.Empty(a.Campaign!.Transcript);Assert.Empty(b.Campaign!.Transcript);
        Assert.Throws<ArgumentException>(()=>CampaignPublishedConstructionApplication.LoadSave(second,Clone(a)));
        Assert.Throws<ArgumentException>(()=>CampaignPublishedConstructionApplication.LoadSave(first,Clone(b)));
    }
    [Theory][InlineData("after-command")][InlineData("before-copy")][InlineData("after-transcript-copy")][InlineData("after-cache-copy")]
    public void LatePublicationCopyTranscriptAndCacheFailuresPublishNothing(string stage)
    {
        var p=Package();var app=CampaignPublishedConstructionApplication.StartGame(p,Settings());var before=Json(app.ExportPublishedConstructionSave());
        Backend(app).PublishedTransactionProbe=s=>{if(s==stage)throw new InvalidOperationException("test-only late fault");};
        var status=app.CaptureStatus();var result=app.SubmitStrategicCommand(new(){ActorCivilizationId=Human,CommandId="command.optin.fault",
            ExpectedTurn=status.Turn,ExpectedRevision=status.CampaignRevision,Kind=CommandKind.PublishShipBlueprint,
            PublishShipBlueprint=new(){BlueprintId=Id,Revision=1,Draft=PublishedTacticalSupportTests.Draft(PublishedTacticalSupportTests.Load(),true)}});
        Assert.False(result.Accepted);Assert.Equal(before,Json(app.ExportPublishedConstructionSave()));
        Backend(app).PublishedTransactionProbe=null;Publish(app,1);Assert.Single(app.CaptureDesignLibrary(Human).Published);
    }
    [Fact] public void PaidProductionProvisionFailureRetainsMoneyQueueIdsRngAndCanRetryExactlyOnce()
    {
        var p=Package();var app=CampaignPublishedConstructionApplication.StartGame(p,Settings());Publish(app,1);Queue(app);
        Backend(app).PublishedTransactionProbe=s=>{if(s=="after-provision")throw new InvalidOperationException("test-only provision fault");};
        bool failed=false;
        for(int i=0;i<100;i++)
        {
            var before=Json(app.ExportPublishedConstructionSave());var stamp=app.CaptureStatus();var r=app.AdvanceTurn(stamp.Turn,stamp.CampaignRevision);
            if(!r.Accepted){Assert.Equal(before,Json(app.ExportPublishedConstructionSave()));failed=true;break;}
        }
        Assert.True(failed);Backend(app).PublishedTransactionProbe=null;Advance(app);
        Assert.Single(app.ExportPublishedConstructionSave().PublishedBuiltShips!);
    }
    [Fact] public void TightRecordBudgetSkipsAiBeforePaymentAndRejectsLaterHumanLiabilityAtomically()
    {
        var p=Package();p=CampaignPublishedConstructionApplication.CreatePackage(p.BaseTemplate!,p.Support!,1);
        var app=CampaignPublishedConstructionApplication.StartGame(p,Settings());Publish(app,1);Queue(app);
        for(int i=0;i<100&&app.ExportPublishedConstructionSave().PublishedBuiltShips!.Count==0;i++)Advance(app);
        var root=app.ExportPublishedConstructionSave();Assert.Single(root.PublishedBuiltShips!);Assert.Empty(root.Campaign!.Integrated!.BuiltShips!);
        var before=Json(root);var s=app.CaptureStatus();var r=app.SubmitStrategicCommand(new(){ActorCivilizationId=Human,CommandId="command.optin.no-capacity",
            ExpectedTurn=s.Turn,ExpectedRevision=s.CampaignRevision,Kind=CommandKind.EnqueueBlueprintConstruction,
            EnqueueBlueprintConstruction=new(){ColonyId="colony.sol",BlueprintId=Id,Revision=1}});
        Assert.Equal(CampaignTacticalError.Overflow,r.Error);Assert.Equal(before,Json(app.ExportPublishedConstructionSave()));
    }
    [Fact] public void ExactJournalBoundaryChecksPostPaymentQueueAndRejectsWithoutPublishingAnyState()
    {
        var source=Package();source.BaseTemplate!.ReplayOperationLimit=2;
        var p=CampaignPublishedConstructionApplication.CreatePackage(source.BaseTemplate,source.Support!,512);
        var app=CampaignPublishedConstructionApplication.StartGame(p,Settings());Publish(app,1);
        var before=Json(app.ExportPublishedConstructionSave());var stamp=app.CaptureStatus();
        var facts=(ConstructionJournalFacts)typeof(CampaignTacticalReviewSession).GetMethod("PublishedCapacity",
            System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)!.Invoke(Backend(app),
                new object[]{ConstructionJournalOperation.AlreadyStagedBoundary,1L})!;
        Assert.True(facts.Certified);Assert.Equal(0,facts.Pending);Assert.Equal(0,facts.ReservedSlots);
        Assert.Equal(stamp.CampaignRevision,facts.Revision);
        var result=app.SubmitStrategicCommand(new(){ActorCivilizationId=Human,CommandId="command.optin.exact-journal",
            ExpectedTurn=stamp.Turn,ExpectedRevision=stamp.CampaignRevision,Kind=CommandKind.EnqueueBlueprintConstruction,
            EnqueueBlueprintConstruction=new(){ColonyId="colony.sol",BlueprintId=Id,Revision=1}});
        Assert.Equal(CampaignTacticalError.Overflow,result.Error);Assert.Equal(before,Json(app.ExportPublishedConstructionSave()));
        Assert.Equal(1,app.RemainingPlayableOperations);
        // A second immutable revision has no paid queue debt and still fits exactly.
        Publish(app,2);Assert.Equal(0,app.RemainingPlayableOperations);
        var root=app.ExportPublishedConstructionSave();Assert.Equal(2,app.CaptureDesignLibrary(Human).Published.Count);
        Assert.Equal(Json(root),Json(CampaignPublishedConstructionApplication.LoadSave(p,Clone(root)).ExportPublishedConstructionSave()));
    }
    [Fact] public void MixedStaticAndPublishedSourcesReplayAndRejectForgedProvenanceAndPackageMetadata()
    {
        var p=Package();var app=CampaignPublishedConstructionApplication.StartGame(p,Settings());Publish(app,1);Queue(app);
        var stamp=app.CaptureStatus();Assert.True(app.SubmitStrategicCommand(new(){ActorCivilizationId=Human,CommandId="command.optin.static",
            ExpectedTurn=stamp.Turn,ExpectedRevision=stamp.CampaignRevision,Kind=CommandKind.EnqueueProduction,
            EnqueueProduction=new(){ColonyId="colony.sol",DefinitionId="ship-design.source-cruiser"}}).Accepted);
        for(int i=0;i<100;i++)
        {
            var current=app.ExportPublishedConstructionSave();
            if(current.PublishedBuiltShips!.Count==1&&current.Campaign!.Integrated!.BuiltShips!.Any(s=>s.OwnerCivilizationId==Human))break;
            Advance(app);
        }
        var root=app.ExportPublishedConstructionSave();var pub=Assert.Single(root.PublishedBuiltShips!);
        Assert.Contains(root.Campaign!.Integrated!.BuiltShips!,s=>s.OwnerCivilizationId==Human);
        var pool=root.Campaign.Integrated.Physical!.Core.Pools.Single(s=>s.ShipId==pub.ShipId);
        Assert.Empty(pool.Ammunition);Assert.All(pool.Shields,s=>Assert.Equal(0,s.Current));
        var bytes=Json(root);Assert.Equal(bytes,Json(CampaignPublishedConstructionApplication.LoadSave(p,Clone(root)).ExportPublishedConstructionSave()));
        var mutations=new Action<CampaignPublishedConstructionSaveDto>[] {
            r=>r.FormatVersion=7,r=>r.PolicyId="generated-static-centres-v1",r=>r.SupportIdentity!.SupportHash="forged",
            r=>r.PublishedBuiltShips![0].QueueItemId="queue.forged",r=>r.PublishedBuiltShips![0].BlueprintRevision=2,
            r=>r.PublishedBuiltShips![0].OwnerCivilizationId="civ.cardassian",r=>r.PublishedBuiltShips![0].PublicationHash="forged",
            r=>r.PublishedBuiltShips!.Add(Clone(r.PublishedBuiltShips[0])),r=>r.PublishedBuiltShips!.Clear(),r=>r.PublishedBuiltShips![0]=null!,
            r=>r.Campaign!.Integrated!.Physical!.Core.ProfileId="generated-campaign-owned-core-v1"};
        foreach(var mutate in mutations){var forged=Clone(root);mutate(forged);Assert.Throws<ArgumentException>(()=>CampaignPublishedConstructionApplication.LoadSave(p,forged));}
        var altered=Clone(p);altered.Support!.Components!.Clear();
        Assert.Throws<ArgumentException>(()=>CampaignPublishedConstructionApplication.LoadSave(altered,Clone(root)));
        pool.Ammunition.Clear();pub.PublicationHash="caller mutation";
        Assert.Equal(bytes,Json(app.ExportPublishedConstructionSave()));
    }
    [Fact] public void StaleWrongActorAndUnsupportedDraftNeverPayOrPublish()
    {
        var p=Package();var app=CampaignPublishedConstructionApplication.StartGame(p,Settings());Publish(app,1);
        var before=Json(app.ExportPublishedConstructionSave());var s=app.CaptureStatus();
        foreach(var actor in new[]{Human,"civ.cardassian"})
        {
            var result=app.SubmitStrategicCommand(new(){ActorCivilizationId=actor,CommandId="command.optin.denied."+actor,
                ExpectedTurn=s.Turn,ExpectedRevision=actor==Human?s.CampaignRevision-1:s.CampaignRevision,Kind=CommandKind.EnqueueBlueprintConstruction,
                EnqueueBlueprintConstruction=new(){ColonyId="colony.sol",BlueprintId=Id,Revision=1}});
            Assert.False(result.Accepted);Assert.Equal(before,Json(app.ExportPublishedConstructionSave()));
        }
        var bad=Clone(app.ExportPublishedConstructionSave());bad.Discoveries!.Add(null!);
        Assert.Throws<ArgumentException>(()=>CampaignPublishedConstructionApplication.LoadSave(p,bad));
    }
    [Fact] public void Actual250BuiltBeamShipTravelsToLawfulHomeGuardCombatAndReplaysThroughAftermath()
    {
        var p=Package();var app=CampaignPublishedConstructionApplication.StartGame(p,Settings(250));Publish(app,1);Queue(app);Publish(app,2);
        for(int i=0;i<100&&app.ExportPublishedConstructionSave().PublishedBuiltShips!.Count==0;i++)Advance(app);
        var built=Assert.Single(app.ExportPublishedConstructionSave().PublishedBuiltShips!);
        var ownSystem=app.CaptureStrategy(Human).Colonies.Single(c=>c.ColonyId.Value=="colony.sol").SystemId;
        string home=app.CaptureGeneratedMap(Human).Regions.Single(r=>r.SystemId==ownSystem.Value).NodeId;
        var visited=new HashSet<string>(StringComparer.Ordinal);
        for(int turn=0;turn<8&&app.CaptureStatus().Phase==SessionPhase.Planning;turn++)
        {
            foreach(var fleet in app.GetOwnStrategicFleets(Human))
            {
                visited.Add(fleet.LocationNodeId.Value);if(fleet.TransitTotalCost>0||fleet.RouteNodeIds.Count>0)continue;
                var map=app.CaptureGeneratedMap(Human);var candidates=map.Regions.Where(r=>!visited.Contains(r.NodeId))
                    .Select(r=>app.FindKnownRoute(Human,fleet.LocationNodeId.Value,r.NodeId)).Where(r=>r.Success&&r.TotalCost>0)
                    .OrderBy(r=>r.TotalCost).ThenBy(r=>r.Steps.Last(),StringComparer.Ordinal).FirstOrDefault();
                if(candidates!=null)Order(app,fleet.FleetId.Value,CampaignTravelIntent.Leave,candidates.Steps);
            }
            Advance(app);
        }
        var guarded=new HashSet<string>(StringComparer.Ordinal);var returning=new HashSet<string>(StringComparer.Ordinal);
        for(int turn=0;turn<128&&app.CaptureStatus().Phase==SessionPhase.Planning;turn++)
        {
            foreach(var fleet in app.GetOwnStrategicFleets(Human))
            {
                if(fleet.TransitTotalCost>0||fleet.RouteNodeIds.Count>0)continue;
                if(fleet.LocationNodeId.Value==home){if(guarded.Add(fleet.FleetId.Value))Order(app,fleet.FleetId.Value,CampaignTravelIntent.StayEngage,Array.Empty<string>());}
                else if(returning.Add(fleet.FleetId.Value))
                {var route=app.FindKnownRoute(Human,fleet.LocationNodeId.Value,home);Assert.True(route.Success);Order(app,fleet.FleetId.Value,CampaignTravelIntent.Leave,route.Steps);}
            }
            Advance(app);
        }
        Assert.Equal(SessionPhase.AwaitingCombat,app.CaptureStatus().Phase);
        var pending=app.ExportPublishedConstructionSave();
        Assert.Contains(pending.Campaign!.Integrated!.Physical!.Core.Battles.SelectMany(b=>b.State.Formations).Where(f=>f.SideId==Human).SelectMany(f=>f.Ships),s=>s.ShipId==built.ShipId);
        app=CampaignPublishedConstructionApplication.LoadSave(p,Clone(pending));Assert.Equal(Json(pending),Json(app.ExportPublishedConstructionSave()));
        foreach(var id in app.GetEncounters(Human))
        {
            while(!app.Observe(id,Human).Complete)
            {
                var o=app.Observe(id,Human);var r=app.CommitRound(new(){ActorId=Human,EncounterId=id,CommandId="command.optin.round."+app.CaptureStatus().CampaignRevision,
                    ExpectedTurn=o.Turn,ExpectedRound=o.Round,ExpectedBoundaryRevision=o.BoundaryRevision,Replacements=new()});Assert.True(r.Accepted,r.Error.ToString());
                var round=app.ExportPublishedConstructionSave();app=CampaignPublishedConstructionApplication.LoadSave(p,Clone(round));Assert.Equal(Json(round),Json(app.ExportPublishedConstructionSave()));
            }
            var playback=app.CaptureBattlePlayback(id,Human);Assert.Contains(playback.Initial.ShipIdentities,s=>s.ShipId==built.ShipId&&s.ShipDesignDefinitionId==Id);
        }
        var status=app.CaptureStatus();var finalized=app.FinalizeBatch(status.Turn,status.CampaignRevision);Assert.True(finalized.Accepted,finalized.Error.ToString());
        var aftermath=app.ExportPublishedConstructionSave();Assert.Single(aftermath.PublishedBuiltShips!);Assert.NotEmpty(app.GetBattleHistory(Human));
        app=CampaignPublishedConstructionApplication.LoadSave(p,Clone(aftermath));Assert.Equal(Json(aftermath),Json(app.ExportPublishedConstructionSave()));
        var own=app.GetOwnStrategicFleets(Human).First();var known=app.CaptureGeneratedMap(Human).Regions.Where(r=>r.NodeId!=own.LocationNodeId.Value)
            .Select(r=>app.FindKnownRoute(Human,own.LocationNodeId.Value,r.NodeId)).Where(r=>r.Success).OrderBy(r=>r.TotalCost).First();
        Order(app,own.FleetId.Value,CampaignTravelIntent.Leave,known.Steps);Advance(app);
    }
}
