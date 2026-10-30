using System.Reflection;
using System.Text.Json;
using HobbyDS9.Application;
using HobbyDS9.Contracts;
using HobbyDS9.Contracts.ShipDesign;
using Xunit;

namespace HobbyDS9.Simulation.Tests;

public sealed class VisibleHullFamilyProjectionTests
{
    const string Human="civ.federation", Observer="civ.cardassian", Blueprint="blueprint.ready.defiant-combat";
    static string Wire(object value)=>JsonSerializer.Serialize(value);
    static T Copy<T>(T value) where T:notnull=>JsonSerializer.Deserialize<T>(Wire(value))!;
    // A projection-only encounter is never installed in a session or submitted as a result.
    // Real sources below come from public startup/public streamed builds, not copied private caches.
    static CampaignPlaybackBoundary Project(CampaignTacticalReviewSession session,IEnumerable<string> ships,string actor,IReadOnlyCollection<string>? hidden=null,bool destroyed=false)
    {
        var game=session.ExportStreamedActivitiesSave().Campaign!.Integrated!.Physical!.Core.Campaign.GameState;
        var battle=new TacticalEncounterV2Dto();
        foreach(var group in game.Ships!.Where(s=>ships.Contains(s.ShipId)).GroupBy(s=>s.OwnerCivilizationId))
            battle.Formations.Add(new TacticalFormationV2Dto{FormationId="projection."+group.Key,SideId=group.Key,
                Ships=group.Select(s=>new TacticalShipV2Dto{ShipId=s.ShipId,Hull=destroyed?0:s.CurrentHull,MaxHull=s.CurrentHull}).ToList()});
        var method=typeof(CampaignTacticalReviewSession).GetMethod("PlaybackBoundary",BindingFlags.Instance|BindingFlags.NonPublic)!;
        return (CampaignPlaybackBoundary)method.Invoke(session,new object[]{battle,hidden??Array.Empty<string>(),actor})!;
    }
    static CampaignTacticalReviewSession Start(CampaignStreamedActivitiesPackageDto? package=null)=>
        CampaignTacticalReviewSession.StartStreamedActivities(package??FrontierCommissionTests.Package(),PublishedConstructionProfileTests.Settings());
    [Fact] public void OldIdentityConstructorAbisDefaultFamilyToNull()
    {
        var type=typeof(CampaignPlaybackShipIdentity);
        Assert.NotNull(type.GetConstructor(new[]{typeof(string),typeof(string),typeof(string)}));
        Assert.NotNull(type.GetConstructor(new[]{typeof(string),typeof(string),typeof(string),typeof(string)}));
        Assert.NotNull(type.GetConstructor(Enumerable.Repeat(typeof(string),5).ToArray()));
        Assert.Null(new CampaignPlaybackShipIdentity("ship.one","civilization.one","blueprint.one").VisibleHullFamilyId);
        var old=new CampaignPlaybackShipIdentity("ship.one","civilization.one","blueprint.one","hull.exact.v1");
        Assert.Null(old.VisibleHullFamilyId);Assert.Equal("hull.exact.v1",old.OwnHullVersionId);
        Assert.False(type.GetProperty(nameof(CampaignPlaybackShipIdentity.VisibleHullFamilyId))!.CanWrite);
    }
    [Fact] public void HiddenBeforeRevealHasNoFamilyAndOldBoundariesNeverBackfillAcrossActorOrSession()
    {
        var s=Start();var root=s.ExportStreamedActivitiesSave();var before=Wire(root);
        var own=root.Campaign!.Integrated!.Physical!.Core.Campaign.GameState.Ships!.Single(x=>x.OwnerCivilizationId==Human).ShipId;
        var hidden=Project(s,new[]{own},Observer,new[]{own});Assert.Empty(hidden.VisibleEnemies);Assert.Empty(hidden.ShipIdentities);var hiddenBytes=Wire(hidden);
        var revealed=Project(s,new[]{own},Observer);var identity=Assert.Single(revealed.ShipIdentities);
        Assert.Equal("hull-family.constitution-explorer",identity.VisibleHullFamilyId);Assert.Null(identity.OwnHullVersionId);var revealBytes=Wire(revealed);
        var self=Assert.Single(Project(s,new[]{own},Human).ShipIdentities);Assert.Equal("hull.constitution-explorer.v1",self.OwnHullVersionId);
        Assert.Equal(identity.VisibleHullFamilyId,self.VisibleHullFamilyId);
        Assert.Equal(hiddenBytes,Wire(hidden));Assert.Equal(revealBytes,Wire(revealed));
        var fresh=Start();Assert.Empty(Project(fresh,new[]{own},Observer,new[]{own}).ShipIdentities);
        Assert.Equal(hiddenBytes,Wire(hidden));Assert.Equal(before,Wire(s.ExportStreamedActivitiesSave()));
    }
    [Fact] public void FullyHiddenUnknownSourceIsNotPrivatelyResolvedOrCounted()
    {
        var s=Start();var before=Wire(s.ExportStreamedActivitiesSave());
        var battle=new TacticalEncounterV2Dto{Formations=new(){new(){FormationId="projection.hidden",SideId=Human,Ships=new(){new(){ShipId="ship.never-in-equipment",Hull=1,MaxHull=1}}}}};
        var method=typeof(CampaignTacticalReviewSession).GetMethod("PlaybackBoundary",BindingFlags.Instance|BindingFlags.NonPublic)!;
        var hidden=(CampaignPlaybackBoundary)method.Invoke(s,new object[]{battle,new[]{"ship.never-in-equipment"},Observer})!;
        Assert.Empty(hidden.OwnShips);Assert.Empty(hidden.VisibleEnemies);Assert.Empty(hidden.ShipIdentities);Assert.Equal(before,Wire(s.ExportStreamedActivitiesSave()));
    }
    sealed class BuiltFixture
    {
        public CampaignStreamedActivitiesPackageDto Package=null!;
        public CampaignStreamedActivitiesSaveDto Root=null!;
        public string[] Ships=null!;
    }
    // ONE real R1 public streamed build, <=24 total weeks; later revisions are publications only.
    // OPEN: a newly BUILT engineering-v2/R2 ship projection is not proved by this fixture.
    // The extra same-family engineered hull/support is explicit test content, never live content.
    static readonly Lazy<BuiltFixture> Built=new(()=>{
        var content=CampaignActivitiesTests.Package();var hull=Copy(content.Catalogue!.Hulls!.Single(h=>h.VersionId=="hull.defiant-combat.v1"));
        hull.VersionId="hull.defiant-combat.v2";hull.VersionNumber=2;hull.ParentVersionId="hull.defiant-combat.v1";content.Catalogue!.Hulls!.Add(hull);
        var support=Copy(content.Support!.Tactical!.Hulls!.Single(h=>h.HullVersionId=="hull.defiant-combat.v1"));support.HullVersionId=hull.VersionId;content.Support!.Tactical!.Hulls!.Add(support);
        content=CampaignTacticalReviewSession.CreateActivitiesPackage(content.BaseTemplate!,content.Catalogue,content.ReadyClasses!,content.Support,content.Content!,content.RecordBudget);
        var package=CampaignTacticalReviewSession.CreateStreamedActivitiesPackage(content);var s=Start(package);int advances=0;
        string Build(int revision)
        {
            var stamp=s.CaptureStatus();Assert.True(s.SubmitActivity(new(){ActorId=Human,CommandId="command.family.build."+revision,ExpectedTurn=stamp.Turn,ExpectedRevision=stamp.CampaignRevision,
                Kind=CampaignActivityCommandKind.EnqueueShip,TargetId="colony.sol",DefinitionId=Blueprint,BlueprintRevision=revision}).Accepted,s.LastPhysicalFailure);
            for(;advances<24&&!s.ExportStreamedActivitiesSave().PublishedBuiltShips!.Any(x=>x.OwnerCivilizationId==Human&&x.BlueprintId==Blueprint&&x.BlueprintRevision==revision);advances++)
            {Assert.Equal(SessionPhase.Planning,s.CaptureStatus().Phase);FrontierCommissionTests.Advance(s);}
            return Assert.Single(s.ExportStreamedActivitiesSave().PublishedBuiltShips!,x=>x.OwnerCivilizationId==Human&&x.BlueprintId==Blueprint&&x.BlueprintRevision==revision).ShipId!;
        }
        void Publish(int revision,ShipClassDraftDto draft)
        {
            draft.ClassId="class.defiant-combat.v1";var stamp=s.CaptureStatus();var result=s.SubmitStrategicCommand(new(){ActorCivilizationId=Human,CommandId="command.family.publish."+revision,
                ExpectedTurn=stamp.Turn,ExpectedRevision=stamp.CampaignRevision,Kind=CommandKind.PublishShipBlueprint,PublishShipBlueprint=new(){BlueprintId=Blueprint,Revision=revision,Draft=draft}});
            Assert.True(result.Accepted,result.Error+" "+s.LastPhysicalFailure);
        }
        var first=Build(1);var built=Assert.Single(s.ExportStreamedActivitiesSave().PublishedBuiltShips!,x=>x.OwnerCivilizationId==Human);
        var starter=s.ExportStreamedActivitiesSave().Campaign!.Integrated!.Physical!.Core.Campaign.GameState.Ships!.Single(x=>x.OwnerCivilizationId==Human&&x.ShipId!=first).ShipId;
        var initialBoundary=Project(s,new[]{first},Observer);
        var retained=new List<(CampaignPlaybackBoundary Boundary,string Bytes)>{(initialBoundary,Wire(initialBoundary))};
        void CheckR1AfterPublication(int revision,string expectedHull)
        {
            var complete=s.ExportStreamedActivitiesSave();var before=Wire(complete);
            var app=CampaignStreamedActivitiesApplication.LoadSave(package,complete);
            Assert.Equal(before,Wire(app.ExportStreamedActivitiesSave()));
            var library=app.CaptureDesignLibrary(Human);
            var published=Assert.Single(library.Published,x=>x.Blueprint.BlueprintId==Blueprint&&x.Blueprint.Revision==revision);
            Assert.Equal(expectedHull,published.Blueprint.Fit.HullVersionId);
            Assert.Equal(revision==2?"hull-family.defiant-combat":"hull-family.constitution-explorer",
                content.Catalogue!.Hulls!.Single(x=>x.VersionId==published.Blueprint.Fit.HullVersionId).FamilyId);
            Assert.Equal("hull.defiant-combat.v1",Assert.Single(library.Published,x=>x.Blueprint.BlueprintId==Blueprint&&x.Blueprint.Revision==1).Blueprint.Fit.HullVersionId);
            var record=Assert.Single(s.ExportStreamedActivitiesSave().PublishedBuiltShips!,x=>x.OwnerCivilizationId==Human);
            Assert.Equal(1,record.BlueprintRevision);Assert.Equal(built.FitId,record.FitId);Assert.Equal(built.FitRevision,record.FitRevision);
            var foreign=Project(s,new[]{first},Observer);var identity=Assert.Single(foreign.ShipIdentities);
            Assert.Equal("hull-family.defiant-combat",identity.VisibleHullFamilyId);Assert.Null(identity.OwnHullVersionId);
            var own=Assert.Single(Project(s,new[]{first},Human).ShipIdentities);Assert.Equal("hull.defiant-combat.v1",own.OwnHullVersionId);
            Assert.Equal(identity.VisibleHullFamilyId,own.VisibleHullFamilyId);
            foreach(var old in retained)Assert.Equal(old.Bytes,Wire(old.Boundary));
            retained.Add((foreign,Wire(foreign)));Assert.Equal(before,Wire(s.ExportStreamedActivitiesSave()));
        }
        var same=Copy(content.ReadyClasses!.Single(d=>d.HullVersionId=="hull.defiant-combat.v1"));same.HullVersionId=hull.VersionId;Publish(2,same);
        CheckR1AfterPublication(2,"hull.defiant-combat.v2");
        var different=Copy(content.ReadyClasses!.Single(d=>d.HullVersionId=="hull.constitution-explorer.v1"));Publish(3,different);
        CheckR1AfterPublication(3,"hull.constitution-explorer.v1");
        return new BuiltFixture{Package=package,Root=s.ExportStreamedActivitiesSave(),Ships=new[]{first,starter}};
    });
    [Fact] public void FrozenPublishedR1SurvivesSameFamilyEngineeringRevisionAndLaterDifferentFamilyPublication()
    {
        var f=Built.Value;var s=CampaignTacticalReviewSession.LoadStreamedActivities(f.Package,Copy(f.Root));var before=Wire(s.ExportStreamedActivitiesSave());
        var foreign=Project(s,f.Ships,Observer);var own=Project(s,f.Ships,Human);
        string Family(CampaignPlaybackBoundary b,string ship)=>Assert.Single(b.ShipIdentities,x=>x.ShipId==ship).VisibleHullFamilyId!;
        Assert.Equal("hull-family.defiant-combat",Family(foreign,f.Ships[0]));
        Assert.Equal("hull-family.constitution-explorer",Family(foreign,f.Ships[1]));Assert.All(foreign.ShipIdentities,x=>Assert.Null(x.OwnHullVersionId));
        Assert.Equal(new[]{"hull.defiant-combat.v1","hull.constitution-explorer.v1"},f.Ships.Select(id=>Assert.Single(own.ShipIdentities,x=>x.ShipId==id).OwnHullVersionId));
        // Repeated capture/export includes the complete RNG, accepted commands and journal, not a handpicked subset.
        for(int i=0;i<3;i++){Assert.Equal(Wire(foreign),Wire(Project(s,f.Ships,Observer)));Assert.Equal(before,Wire(s.ExportStreamedActivitiesSave()));}
        var historical=Project(s,f.Ships,Observer,destroyed:true);Assert.All(historical.VisibleEnemies,x=>Assert.Equal(0,x.Hull));
        Assert.Equal(foreign.ShipIdentities.Select(x=>x.VisibleHullFamilyId),historical.ShipIdentities.Select(x=>x.VisibleHullFamilyId));
        Assert.Equal(before,Wire(s.ExportStreamedActivitiesSave()));
    }

    [Fact] public void CycleBuiltR2RetainsExactFamilyAfterR3AndWholeReplay()
    {
        // Blueprint R2 uses an existing v1 hull; engineered-v2 content remains OPEN.
        // ONE new R2 ship, no old Built/Journey lazy fixture and <=24 TOTAL ordinary weeks.
        var package=ColonisationCycleTests.Package();var packageBefore=Wire(package);
        var app=CampaignColonisationCycleApplication.StartCycleGame(package,PublishedConstructionProfileTests.Settings(30));
        var library=app.CaptureDesignLibrary(Human);
        var r1=Assert.Single(library.Published,p=>p.Blueprint.BlueprintId==Blueprint&&p.Blueprint.Revision==1).Blueprint;
        var source=Assert.Single(library.Published,p=>p.Blueprint.Fit.HullVersionId=="hull.constitution-explorer.v1"&&p.Blueprint.Revision==1).Blueprint;
        var r1Bytes=Wire(r1);var expectedR1Family=r1.Hull.FamilyId;var expectedR2Family=source.Hull.FamilyId;
        Assert.Equal("hull-family.defiant-combat",expectedR1Family);
        Assert.Equal("hull-family.constitution-explorer",expectedR2Family);Assert.NotEqual(expectedR1Family,expectedR2Family);
        Assert.Equal(1,source.Hull.VersionNumber);
        ShipClassDraftDto Draft(ShipClassDraftSnapshot fit)=>JsonSerializer.Deserialize<ShipClassDraftDto>(Wire(fit))!;
        void Publish(int revision,ShipClassDraftDto draft)
        {
            draft.ClassId=r1.Fit.ClassId;draft.DisplayName="Actual built R2 probe revision "+revision;
            var status=app.CaptureStatus();
            var result=app.SubmitStrategicCommand(new(){ActorCivilizationId=Human,CommandId="command.family.cycle.publish."+revision,
                ExpectedTurn=status.Turn,ExpectedRevision=status.CampaignRevision,Kind=CommandKind.PublishShipBlueprint,
                PublishShipBlueprint=new(){BlueprintId=Blueprint,Revision=revision,Draft=draft}});
            Assert.True(result.Accepted,result.Error.ToString());
        }
        var expectedR2Fit=Draft(source.Fit);Publish(2,expectedR2Fit);
        var r2=Assert.Single(app.CaptureDesignLibrary(Human).Published,p=>p.Blueprint.BlueprintId==Blueprint&&p.Blueprint.Revision==2).Blueprint;
        Assert.Equal(source.Fit.HullVersionId,r2.Fit.HullVersionId);Assert.Equal(expectedR2Family,r2.Hull.FamilyId);
        Assert.Equal(Wire(expectedR2Fit),Wire(Draft(r2.Fit)));var r2Bytes=Wire(r2);
        var retainedR2=Assert.Single(app.ExportCycleSave().BaseCampaign!.Campaign!.Integrated!.Physical!.Core.Campaign.GameState.Blueprints!,
            p=>p.BlueprintId==Blueprint&&p.Revision==2);
        var frozenR2Catalogue=retainedR2.FrozenCatalogue!;
        var frozenAccess=new ShipDesignAccessDto{AvailableVersionIds=frozenR2Catalogue.Hulls!.Select(h=>h.VersionId)
            .Concat(frozenR2Catalogue.Components!.Select(c=>c.VersionId)).OrderBy(id=>id,StringComparer.Ordinal).ToList()};
        var expectedPublication=new ShipDesignCompiler(new ShipDesignCatalogueSnapshot(frozenR2Catalogue))
            .Publish(retainedR2.Fit!,frozenAccess,Blueprint,2);
        Assert.True(expectedPublication.Published);
        var expectedSupport=PublishedTacticalSupportCompiler.EvaluateColonisation(new ShipDesignCatalogueSnapshot(frozenR2Catalogue),
            expectedPublication.Blueprint!,frozenAccess,package.BaseGrowth!.BaseRosterV2!.BaseV1!.Support!);
        Assert.True(expectedSupport.Supported);var expectedTactical=expectedSupport.Ship!.Tactical;
        var option=Assert.Single(app.CaptureActivities(Human).BuildOptions,o=>o.DefinitionId==Blueprint&&o.BlueprintRevision==2);
        Assert.True(option.Supported,option.Reason);Assert.NotNull(option.Cost);var cost=option.Cost!;
        Assert.True(cost.Industry>0);Assert.NotNull(cost.Resources);Assert.Contains(cost.Resources!,r=>r.Amount>0);
        CampaignActivityCommandDto Enqueue(string world)
        {
            var status=app.CaptureStatus();return new(){ActorId=Human,CommandId="command.family.cycle.enqueue.r2",
                ExpectedTurn=status.Turn,ExpectedRevision=status.CampaignRevision,Kind=CampaignActivityCommandKind.EnqueueShip,
                TargetId=world,DefinitionId=Blueprint,BlueprintRevision=2};
        }
        var eligible=app.CaptureColonisation(Human).Worlds.Where(w=>w.Stage==ColonyEstablishmentStage.Established&&w.Population>0&&w.Shipbuilding)
            .Where(w=>app.EvaluateActivity(Enqueue(w.Id)).Accepted).OrderByDescending(w=>w.Industry).ThenBy(w=>w.Id,StringComparer.Ordinal).ToList();
        Assert.NotEmpty(eligible);var yard=eligible[0];var credit=app.CaptureActivities(Human).Credits;
        var stocks=Wire(yard.Stocks);var queues=app.CaptureColonisation(Human).Production.Select(q=>q.Id).ToHashSet(StringComparer.Ordinal);
        var priorShips=app.CaptureColonisation(Human).OwnShipSources.Select(s=>s.ShipId).ToHashSet(StringComparer.Ordinal);
        Assert.True(app.EvaluateActivity(Enqueue(yard.Id)).Accepted);
        var submitted=app.SubmitActivity(Enqueue(yard.Id));Assert.True(submitted.Accepted,submitted.Error.ToString());
        var queued=Assert.Single(app.CaptureColonisation(Human).Production,q=>!queues.Contains(q.Id));
        Assert.Equal(yard.Id,queued.ColonyId);Assert.Equal(Blueprint,queued.DefinitionId);Assert.Equal(2,queued.BlueprintRevision);
        Assert.Equal(cost.Industry,queued.RequiredIndustry);Assert.Equal(0,queued.CompletedIndustry);
        Assert.All(queued.Consumed,r=>Assert.Equal(0,r.Amount));Assert.Equal(credit,app.CaptureActivities(Human).Credits);
        Assert.Equal(stocks,Wire(app.CaptureColonisation(Human).Worlds.Single(w=>w.Id==yard.Id).Stocks));
        Assert.Equal(cost.Resources!.OrderBy(r=>r.ResourceDefinitionId,StringComparer.Ordinal).Select(r=>(r.ResourceDefinitionId,r.Amount)),
            queued.Required.OrderBy(r=>r.ResourceDefinitionId,StringComparer.Ordinal).Select(r=>(r.ResourceDefinitionId,r.Amount)));
        // Publish different-family R3 BEFORE ANY Advance: completion must still use retained R2.
        var expectedR3Fit=Draft(r1.Fit);Publish(3,expectedR3Fit);
        var afterPublish=app.CaptureDesignLibrary(Human);
        var r3=Assert.Single(afterPublish.Published,p=>p.Blueprint.BlueprintId==Blueprint&&p.Blueprint.Revision==3).Blueprint;
        Assert.Equal(r1.Fit.HullVersionId,r3.Fit.HullVersionId);Assert.Equal(Wire(expectedR3Fit),Wire(Draft(r3.Fit)));
        Assert.Equal(expectedR1Family,r3.Hull.FamilyId);Assert.NotEqual(expectedR2Family,r3.Hull.FamilyId);
        Assert.Equal(r1Bytes,Wire(Assert.Single(afterPublish.Published,p=>p.Blueprint.BlueprintId==Blueprint&&p.Blueprint.Revision==1).Blueprint));
        Assert.Equal(r2Bytes,Wire(Assert.Single(afterPublish.Published,p=>p.Blueprint.BlueprintId==Blueprint&&p.Blueprint.Revision==2).Blueprint));
        Assert.Equal(2,Assert.Single(app.CaptureColonisation(Human).Production,q=>q.Id==queued.Id).BlueprintRevision);
        int advances=0;bool sawWork=false,sawConsumption=false;
        while(!app.CaptureColonisation(Human).OwnShipSources.Any(s=>!priorShips.Contains(s.ShipId)&&s.BlueprintId==Blueprint&&s.Revision==2)&&advances<24)
        {
            var status=app.CaptureStatus();Assert.Equal(SessionPhase.Planning,status.Phase);
            var result=app.AdvanceTurn(status.Turn,status.CampaignRevision);Assert.True(result.Accepted,result.Error.ToString());advances++;
            var progress=app.CaptureColonisation(Human).Production.SingleOrDefault(q=>q.Id==queued.Id);
            if(progress!=null){Assert.Equal(2,progress.BlueprintRevision);sawWork|=progress.CompletedIndustry>0&&progress.CompletedIndustry<progress.RequiredIndustry;sawConsumption|=progress.Consumed.Any(r=>r.Amount>0);}
        }
        Assert.True(sawWork,"No real intermediate work observed within unchanged cap.");
        Assert.True(sawConsumption,"No real local material consumption observed.");
        var born=Assert.Single(app.CaptureColonisation(Human).OwnShipSources,s=>!priorShips.Contains(s.ShipId)&&s.BlueprintId==Blueprint&&s.Revision==2);
        Assert.DoesNotContain(app.CaptureColonisation(Human).Production,q=>q.Id==queued.Id);
        var root=app.ExportCycleSave();var basis=root.BaseCampaign!;
        var receipt=Assert.Single(basis.ProductionHistory!,r=>r.Order!.QueueItemId==queued.Id);
        Assert.True(receipt.Completed);Assert.Equal(born.ShipId,receipt.ShipId);Assert.NotNull(receipt.Order);var order=receipt.Order!;
        Assert.Equal(Human,order.OwnerId);Assert.Equal(yard.Id,order.ColonyId);Assert.Equal(2,order.Blueprint!.Revision);
        Assert.Equal(queued.RequiredIndustry,order.CompletedIndustry);Assert.Equal(order.RequiredIndustry,order.CompletedIndustry);
        Assert.Equal(order.RequiredResources!.OrderBy(r=>r.ResourceDefinitionId,StringComparer.Ordinal).Select(r=>(r.ResourceDefinitionId,r.Amount)),
            order.ConsumedResources!.OrderBy(r=>r.ResourceDefinitionId,StringComparer.Ordinal).Select(r=>(r.ResourceDefinitionId,r.Amount)));
        var record=Assert.Single(basis.PublishedBuiltShips!,r=>r.QueueItemId==queued.Id);
        Assert.Equal(born.ShipId,record.ShipId);Assert.Equal(Human,record.OwnerCivilizationId);Assert.Equal(Blueprint,record.BlueprintId);
        Assert.Equal(2,record.BlueprintRevision);Assert.Equal(2,record.FitRevision);Assert.Equal(receipt.ResolvedTurn,record.TurnBuilt);
        Assert.Equal(expectedTactical.BlueprintId,record.BlueprintId);Assert.Equal(expectedTactical.BlueprintRevision,record.BlueprintRevision);
        Assert.Equal(expectedTactical.SourceHash,record.PublicationHash);Assert.Equal(expectedTactical.SupportCatalogueId,record.SupportCatalogueId);
        Assert.Equal(expectedTactical.SupportCatalogueRevision,record.SupportCatalogueRevision);Assert.Equal(expectedTactical.SupportHash,record.SupportHash);
        Assert.Equal(expectedTactical.CompilerId,record.SupportCompilerId);Assert.Equal(expectedTactical.FitId,record.FitId);
        Assert.Equal(expectedTactical.FitRevision,record.FitRevision);
        var game=basis.Campaign!.Integrated!.Physical!.Core.Campaign.GameState;
        var actual=Assert.Single(game.Ships!,s=>s.ShipId==born.ShipId);Assert.Equal(Human,actual.OwnerCivilizationId);
        Assert.Equal(Blueprint,actual.Blueprint!.BlueprintId);Assert.Equal(2,actual.Blueprint.Revision);
        var frozen=Assert.Single(game.Blueprints!,p=>p.BlueprintId==Blueprint&&p.Revision==2);
        Assert.Equal(source.Fit.HullVersionId,frozen.Fit!.HullVersionId);Assert.Equal(Wire(expectedR2Fit),Wire(frozen.Fit));
        Assert.Equal(frozen.CompilerVersion,record.SourceCompilerId);Assert.Equal(frozen.RulesVersion,record.SourceRulesVersion);Assert.Equal(frozen.SourceContentHash,record.SourceContentHash);
        var full=Wire(root);var loadInput=Copy(root);var loadInputBefore=Wire(loadInput);
        var loaded=CampaignTacticalReviewSession.LoadCycleSave(package,loadInput);
        Assert.Equal(loadInputBefore,Wire(loadInput));Assert.Equal(packageBefore,Wire(package));
        Assert.Equal(14,root.FormatVersion);Assert.Equal(full,Wire(loaded.ExportCycleSave()));
        // Case-local PROJECTION ONLY from the real built R2; never installed, submitted or used for commands.
        CampaignPlaybackBoundary Boundary(string actor,bool hidden=false)
        {
            var battle=new TacticalEncounterV2Dto{Formations=new(){new(){FormationId="projection.actual-built-r2",SideId=Human,
                Ships=new(){new(){ShipId=born.ShipId,Hull=actual.CurrentHull,MaxHull=actual.CurrentHull}}}}};
            var method=typeof(CampaignTacticalReviewSession).GetMethod("PlaybackBoundary",BindingFlags.Instance|BindingFlags.NonPublic)!;
            return (CampaignPlaybackBoundary)method.Invoke(loaded,new object[]{battle,hidden?new[]{born.ShipId}:Array.Empty<string>(),actor})!;
        }
        var hidden=Boundary(Observer,true);Assert.Empty(hidden.VisibleEnemies);Assert.Empty(hidden.ShipIdentities);var hiddenBytes=Wire(hidden);
        var foreign=Boundary(Observer);var identity=Assert.Single(foreign.ShipIdentities);
        Assert.Equal(expectedR2Family,identity.VisibleHullFamilyId);Assert.Null(identity.OwnHullVersionId);
        var own=Assert.Single(Boundary(Human).ShipIdentities);Assert.Equal(source.Fit.HullVersionId,own.OwnHullVersionId);
        Assert.Equal(expectedR2Family,own.VisibleHullFamilyId);var visibleBytes=Wire(foreign);
        for(int i=0;i<3;i++){Assert.Equal(visibleBytes,Wire(Boundary(Observer)));Assert.Equal(hiddenBytes,Wire(hidden));Assert.Equal(full,Wire(loaded.ExportCycleSave()));}
        Assert.Equal(full,Wire(app.ExportCycleSave()));Assert.Equal(loadInputBefore,Wire(loadInput));Assert.Equal(packageBefore,Wire(package));
        // Mutate only caller-owned DTOs AFTER successful load; never resubmit either changed tree.
        void MutateDetached(CampaignColonisationCycleSaveDto detached)
        {
            detached.BaseCampaign!.Map!.Settings!.Map!.Seed="caller-only-r2-mutation";
            Assert.Single(detached.BaseCampaign.Campaign!.Integrated!.Physical!.Core.Campaign.GameState.Ships!,s=>s.ShipId==born.ShipId).CurrentHull=0;
            detached.BaseCampaign.Journal!.Clear();detached.AiPlans![0].HoldReason="caller-only-r2-mutation";
        }
        MutateDetached(loadInput);Assert.NotEqual(loadInputBefore,Wire(loadInput));
        Assert.Equal(full,Wire(loaded.ExportCycleSave()));Assert.Equal(visibleBytes,Wire(Boundary(Observer)));Assert.Equal(hiddenBytes,Wire(hidden));
        var exported=loaded.ExportCycleSave();MutateDetached(exported);Assert.NotEqual(full,Wire(exported));
        Assert.Equal(full,Wire(loaded.ExportCycleSave()));Assert.Equal(full,Wire(app.ExportCycleSave()));
        Assert.Equal(visibleBytes,Wire(Boundary(Observer)));Assert.Equal(hiddenBytes,Wire(hidden));Assert.Equal(packageBefore,Wire(package));
    }
}
