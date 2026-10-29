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
}
