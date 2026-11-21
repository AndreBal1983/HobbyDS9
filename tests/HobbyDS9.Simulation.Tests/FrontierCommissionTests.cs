using System.Text.Json;
using HobbyDS9.Application;
using HobbyDS9.Contracts;
using Xunit;
using Xunit.Abstractions;
namespace HobbyDS9.Simulation.Tests;
public sealed class FrontierCommissionTests
{
 const string Human="civ.federation";
 readonly ITestOutputHelper output;
 public FrontierCommissionTests(ITestOutputHelper output){this.output=output;}
 static string Json(object value)=>JsonSerializer.Serialize(value);
 internal static CampaignStreamedActivitiesPackageDto Package()=>CampaignTacticalReviewSession.CreateStreamedActivitiesPackage(CampaignActivitiesTests.Package());
 static CampaignActivityCommandDto Cmd(CampaignTacticalReviewSession s,CampaignActivityCommandKind kind,string target,string definition="",string ship="")
 {var f=s.CaptureStatus();return new(){ActorId=Human,CommandId="command.commission."+f.CampaignRevision,ExpectedTurn=f.Turn,ExpectedRevision=f.CampaignRevision,Kind=kind,TargetId=target,DefinitionId=definition,BlueprintRevision=1,ShipId=ship};}
 internal static void Advance(CampaignTacticalReviewSession s){var f=s.CaptureStatus();var result=s.AdvanceTurn(f.Turn,f.CampaignRevision);Assert.True(result.Accepted,result.Error+" "+s.LastPhysicalFailure);}
 static void Rehash(CampaignStreamedActivitiesPackageDto p){var c=p.ContentPackage!;p.ContentPackage=CampaignTacticalReviewSession.CreateActivitiesPackage(c.BaseTemplate!,c.Catalogue!,c.ReadyClasses!,c.Support!,c.Content!,c.RecordBudget);p.PackageHash=CampaignStreamedActivitiesData.PackageHash(p);}
 [Fact] public void OfficialFingerprintAndReadOnlyInitialReportHaveNoStartingShipCredit()
 {
  var p=Package();Assert.Equal(CampaignTacticalReviewSession.FrontierCommissionOfficialContent,CampaignActivitiesData.CommissionContentFingerprint(p.ContentPackage!.Content!));
  Assert.Equal("5e0dbe6fdece09f9e4ad58d1e8cea102e114373dcb9a270e6906034ab8d6d6c6",p.PackageHash);
  var s=CampaignTacticalReviewSession.StartStreamedActivities(p,PublishedConstructionProfileTests.Settings());var bytes=Json(s.ExportStreamedActivitiesSave());
  var report=s.CaptureFrontierCommission(Human);Assert.Equal(FrontierCommissionAvailability.Available,report.Availability);Assert.Equal(4,report.Objectives.Count);Assert.Equal(0,report.CompletedObjectives);Assert.False(report.Completed);Assert.Null(report.CompletedTurn);
  Assert.All(report.Objectives,o=>Assert.Null(o.Evidence));Assert.Throws<NotSupportedException>(()=>((IList<FrontierCommissionObjective>)report.Objectives).Clear());
  for(int i=0;i<5;i++)Assert.Equal(Json(report),Json(s.CaptureFrontierCommission(Human)));Assert.Equal(bytes,Json(s.ExportStreamedActivitiesSave()));
  Assert.Throws<ArgumentException>(()=>s.CaptureFrontierCommission("civ.cardassian"));Assert.Equal(bytes,Json(s.ExportStreamedActivitiesSave()));
 }
 [Theory] [InlineData(false)] [InlineData(true)]
 public void ContentWriterExtractionKeepsPrechangeOfficialReorderedAndCustomPackageGoldens(bool custom)
 {
  var p=Package();var c=p.ContentPackage!;c.Content!.Structures!.Reverse();c.Content.Sites!.Reverse();c.Content.Minors!.Reverse();c.Content.Species!.Reverse();foreach(var site in c.Content.Sites.Concat(c.Content.Minors.Select(m=>m.Encounter!)))site.Choices!.Reverse();
  if(custom)c.Content.Sites.Single(x=>x.DefinitionId=="activity.pulsar.v1").Choices!.Single(x=>x.ChoiceId=="choice.pulsar.baseline").Insight++;
  Rehash(p);
  Assert.Equal(custom?"ea11ff9597bd28a75db8d6b228942e3aff415d21ae1329f7bfcaae73e6d01494":"7d01255700f8fc91d7f11debb83e415d538cdc9b83c65af7e369b365060d9ae7",p.ContentPackage!.PackageHash);
  Assert.Equal(custom?"54d7a2579d6a5be567a4fa6d9cff1ca4f6bb61302839d3d40c4c617dddcacc05":"5e0dbe6fdece09f9e4ad58d1e8cea102e114373dcb9a270e6906034ab8d6d6c6",p.PackageHash);
 }
 [Theory] [InlineData("removed")] [InlineData("renamed")] [InlineData("work")] [InlineData("reward")] [InlineData("consent")] [InlineData("text")]
 public void SameContentIdentityDoesNotAuthorizeChangedSemantics(string change)
 {
  var p=Package();var c=p.ContentPackage!.Content!;
  if(change=="removed")c.Sites![0].Choices!.RemoveAt(0);
  if(change=="renamed")c.Minors![0].Encounter!.Choices![0].ChoiceId="choice.changed.valid";
  if(change=="work")c.Sites![0].Choices![1].Work++;
  if(change=="reward")c.Sites![0].Choices![0].Insight++;
  if(change=="consent")c.Minors![0].Encounter!.Choices![1].ConsentChanceBps--;
  if(change=="text")c.Sites![0].Title+=" changed";
  Rehash(p);var s=CampaignTacticalReviewSession.StartStreamedActivities(p,PublishedConstructionProfileTests.Settings());var before=Json(s.ExportStreamedActivitiesSave());
  var report=s.CaptureFrontierCommission(Human);Assert.Equal(FrontierCommissionAvailability.UnsupportedContent,report.Availability);Assert.Empty(report.Objectives);Assert.False(report.Completed);Assert.Null(report.CompletedTurn);Assert.Equal(before,Json(s.ExportStreamedActivitiesSave()));
  Assert.True(s.SubmitActivity(Cmd(s,CampaignActivityCommandKind.EnqueueStructure,"colony.sol","infrastructure.fabrication-annex.v1")).Accepted,s.LastPhysicalFailure);
 }
 [Fact] public void CancelPreviewAndPendingOrdersAreNotHistoricalAccomplishments()
 {
  var s=CampaignTacticalReviewSession.StartStreamedActivities(Package(),PublishedConstructionProfileTests.Settings());var command=Cmd(s,CampaignActivityCommandKind.EnqueueShip,"colony.sol","blueprint.ready.constitution-explorer");
  Assert.True(s.EvaluateActivity(command).Accepted);Assert.Equal(0,s.CaptureFrontierCommission(Human).CompletedObjectives);Assert.True(s.SubmitActivity(command).Accepted);
  Assert.Equal(FrontierCommissionProgress.Underway,s.CaptureFrontierCommission(Human).Objectives.Single(o=>o.Id=="ship").Progress);
  var id=s.CaptureStreamedActivities(Human).Production.Single().QueueItemId;Assert.True(s.SubmitActivity(Cmd(s,CampaignActivityCommandKind.CancelProduction,id)).Accepted);Assert.Equal(0,s.CaptureFrontierCommission(Human).CompletedObjectives);
 }
 static ActivityFindingDto Claim(CampaignTacticalReviewSession s,bool minor,bool distant=false,bool abort=false)
 {
  var site=CampaignActivitiesTests.Discover(s,minor);var ship=s.CaptureActivities(Human).Ships.First();
  if(ship.NodeId!=site.Site.NodeId){CampaignActivitiesTests.Travel(s,ship.FleetId,CampaignActivitiesTests.KnownPath(s.CaptureGeneratedMap(Human),ship.NodeId,site.Site.NodeId));for(int i=0;i<20&&s.CaptureActivities(Human).Ships.First().NodeId!=site.Site.NodeId;i++)Advance(s);}
  ship=s.CaptureActivities(Human).Ships.First();Assert.Equal(site.Site.NodeId,ship.NodeId);
  var choice=minor?site.Choices.Single(c=>c.ChoiceId.EndsWith(distant?".observe":".channel")):site.Choices.First(c=>c.RequiredStudy<=ship.StudyWork);
  Assert.True(s.SubmitActivity(Cmd(s,CampaignActivityCommandKind.StartStudy,site.Site.SiteId,choice.ChoiceId,ship.ShipId)).Accepted,s.LastPhysicalFailure);
  Assert.NotEqual(FrontierCommissionProgress.Completed,s.CaptureFrontierCommission(Human).Objectives.Single(o=>o.Id==(minor?"society":"science")).Progress);
  for(int i=0;i<3&&s.CaptureActivities(Human).Findings.Last().Stage==ActivityProgressStage.Active&&!abort;i++)Advance(s);
  Assert.True(s.SubmitActivity(Cmd(s,abort?CampaignActivityCommandKind.AbortStudy:CampaignActivityCommandKind.ClaimFinding,site.Site.SiteId,choice.ChoiceId)).Accepted,s.LastPhysicalFailure);
  return s.CaptureActivities(Human).Findings.Last();
 }
 [Theory] [InlineData("42",false)] [InlineData("43",false)] [InlineData("42",true)]
 public void Real250CommandsCompleteIndependentMissionAndFullReplayRestoresIdenticalReport(string seed,bool distant)
 {
  var p=Package();var settings=PublishedConstructionProfileTests.Settings(250);settings.Map.Seed=seed;
  var s=CampaignTacticalReviewSession.StartStreamedActivities(p,settings);
  Assert.True(s.SubmitActivity(Cmd(s,CampaignActivityCommandKind.EnqueueStructure,"colony.sol","infrastructure.research-laboratory.v1")).Accepted);
  Assert.True(s.SubmitActivity(Cmd(s,CampaignActivityCommandKind.EnqueueShip,"colony.sol","blueprint.ready.constitution-explorer")).Accepted);
  var draft=ActivityReadyClassTests.Ready().Single(d=>d.ClassId=="class.constitution-explorer.v1");draft.DisplayName="Commission R2";var f=s.CaptureStatus();
  Assert.True(s.SubmitStrategicCommand(new(){ActorCivilizationId=Human,CommandId="command.commission.r2",ExpectedTurn=f.Turn,ExpectedRevision=f.CampaignRevision,Kind=CommandKind.PublishShipBlueprint,PublishShipBlueprint=new(){BlueprintId="blueprint.ready.constitution-explorer",Revision=2,Draft=draft}}).Accepted);
  var pending=s.ExportStreamedActivitiesSave();var report=s.CaptureFrontierCommission(Human);s=CampaignTacticalReviewSession.LoadStreamedActivities(p,pending);Assert.Equal(Json(pending),Json(s.ExportStreamedActivitiesSave()));Assert.Equal(Json(report),Json(s.CaptureFrontierCommission(Human)));
  for(int i=0;i<40&&s.CaptureFrontierCommission(Human).CompletedObjectives<2;i++)Advance(s);
  Assert.Equal(2,s.CaptureFrontierCommission(Human).CompletedObjectives);Assert.Equal(1,s.ExportStreamedActivitiesSave().PublishedBuiltShips!.Single(x=>x.OwnerCivilizationId==Human).BlueprintRevision);
  var science=Claim(s,false);Assert.Equal(ActivityProgressStage.Completed,science.Stage);Assert.Equal(3,s.CaptureFrontierCommission(Human).CompletedObjectives);
  var society=Claim(s,true,distant);Assert.Equal(ActivityProgressStage.Completed,society.Stage);
  if(!distant){var known=s.CaptureActivities(Human).Sites.Single(x=>x.Site.SiteId==society.SiteId);Assert.Equal(seed=="42"?ActivityMinorRelation.Refused:ActivityMinorRelation.Accepted,known.Relation!.Value);output.WriteLine("Consent seed="+seed+" relation="+known.Relation);}
  var final=s.ExportStreamedActivitiesSave();var completed=s.CaptureFrontierCommission(Human);Assert.True(completed.Completed);Assert.Equal(4,completed.CompletedObjectives);Assert.Equal(completed.Objectives.Max(o=>o.Evidence!.Turn),completed.CompletedTurn);
  Assert.Equal(Json(final),Json(CampaignTacticalReviewSession.LoadStreamedActivities(p,final).ExportStreamedActivitiesSave()));Assert.Equal(Json(completed),Json(CampaignTacticalReviewSession.LoadStreamedActivities(p,final).CaptureFrontierCommission(Human)));
  var before=Json(s.ExportStreamedActivitiesSave());for(int i=0;i<3;i++)s.CaptureFrontierCommission(Human);Assert.Equal(before,Json(s.ExportStreamedActivitiesSave()));Advance(s);Assert.True(s.CaptureFrontierCommission(Human).Completed);
 }
 [Fact] public void ActualAbortedScienceIsNotACompletedInvestigation()
 {
  var s=CampaignTacticalReviewSession.StartStreamedActivities(Package(),PublishedConstructionProfileTests.Settings());var finding=Claim(s,false,abort:true);Assert.Equal(ActivityProgressStage.Aborted,finding.Stage);Assert.Equal(0,s.CaptureFrontierCommission(Human).CompletedObjectives);
 }
 [Fact] public void ActualMaxedFieldClaimStillRecordsCompletedStudyWithNoNewLearnedScience()
 {
  var p=Package();var game=p.ContentPackage!.BaseTemplate!.Integrated!.Physical!.CorePackage!.Campaign!.InitialState;
  var owner=game.Civilizations!.Single(c=>c.CivilizationId==Human);
  foreach(ScienceDiscipline field in Enum.GetValues<ScienceDiscipline>())ScienceRules.GrantActivityInsight(owner,game.ResearchPaceFactor!.Value,Human,field,100000,new());
  Assert.All(owner.Disciplines!,d=>Assert.Equal(10,d.Level));Rehash(p);
  var s=CampaignTacticalReviewSession.StartStreamedActivities(p,PublishedConstructionProfileTests.Settings());var finding=Claim(s,false);Assert.Equal(0,finding.InsightApplied);Assert.True(finding.InsightUnlearnable>0);
  var objective=s.CaptureFrontierCommission(Human).Objectives.Single(o=>o.Id=="science");Assert.Equal(FrontierCommissionProgress.Completed,objective.Progress);Assert.Equal(0,objective.Evidence!.LearnedInternalUnits);Assert.Equal(finding.InsightUnlearnable,objective.Evidence.UnlearnableInternalUnits);
  var root=s.ExportStreamedActivitiesSave();Assert.Equal(Json(s.CaptureFrontierCommission(Human)),Json(CampaignTacticalReviewSession.LoadStreamedActivities(p,root).CaptureFrontierCommission(Human)));
 } 
 [Fact] public void ActualAutomaticAiCompletedConstructionDoesNotCreditHumanMission()
 {
  var s=CampaignTacticalReviewSession.StartStreamedActivities(Package(),PublishedConstructionProfileTests.Settings(250));
  for(int i=0;i<30&&!s.ExportStreamedActivitiesSave().ProductionHistory!.Any(h=>h.Completed&&h.Order!.OwnerId!=Human);i++)
  {
   if(s.CaptureStatus().Phase==SessionPhase.AwaitingCombat)
   {
    foreach(var id in s.Encounters(Human))while(!s.Observe(id,Human).Complete){var o=s.Observe(id,Human);Assert.True(s.CommitRound(new(){ActorId=Human,CommandId="command.commission.foreign.round."+s.CaptureStatus().CampaignRevision,EncounterId=id,ExpectedTurn=o.Turn,ExpectedRound=o.Round,ExpectedBoundaryRevision=o.BoundaryRevision,Replacements=new()}).Accepted);}
    var f=s.CaptureStatus();Assert.True(s.FinalizeBatch(f.Turn,f.CampaignRevision).Accepted);
   }
   else Advance(s);
  }
  var root=s.ExportStreamedActivitiesSave();Assert.Contains(root.ProductionHistory!,h=>h.Completed&&h.Order!.OwnerId!=Human);Assert.Contains(root.PublishedBuiltShips!,r=>r.OwnerCivilizationId!=Human);
  Assert.DoesNotContain(root.ProductionHistory!,h=>h.Completed&&h.Order!.OwnerId==Human);var before=Json(root);var report=s.CaptureFrontierCommission(Human);Assert.Equal(0,report.CompletedObjectives);Assert.All(report.Objectives,o=>Assert.Null(o.Evidence));Assert.Equal(before,Json(s.ExportStreamedActivitiesSave()));
 }
 [Theory] [InlineData("constitution-explorer")] [InlineData("defiant-combat")]
 public void HistoricalLaunchSurvivesActualLawfulTacticalLossAndReplay(string role)
 {
  var p=Package();var app=CampaignStreamedActivitiesApplication.StartGame(p,PublishedConstructionProfileTests.Settings());
  var f=app.CaptureStatus();Assert.True(app.SubmitActivity(new(){ActorId=Human,CommandId="command.commission.loss-build",ExpectedTurn=f.Turn,ExpectedRevision=f.CampaignRevision,Kind=CampaignActivityCommandKind.EnqueueShip,TargetId="colony.sol",DefinitionId="blueprint.ready."+role,BlueprintRevision=1}).Accepted);
  var starter=app.CaptureStreamedActivities(Human).Activities.Ships.Single().ShipId;
  void Step(){var stamp=app.CaptureStatus();var result=app.AdvanceTurn(stamp.Turn,stamp.CampaignRevision);Assert.True(result.Accepted,result.Error.ToString());}
  for(int i=0;i<30&&!app.ExportStreamedActivitiesSave().PublishedBuiltShips!.Any(x=>x.OwnerCivilizationId==Human);i++)Step();
  var revised=ActivityReadyClassTests.Ready().Single(d=>d.ClassId=="class."+(role=="constitution-explorer"?"defiant-combat":"constitution-explorer")+".v1");
  revised.ClassId="class."+role+".v1";revised.DisplayName="Different hull after frozen R1 construction";
  f=app.CaptureStatus();Assert.True(app.SubmitStrategicCommand(new(){ActorCivilizationId=Human,CommandId="command.battle-hull.r2",ExpectedTurn=f.Turn,ExpectedRevision=f.CampaignRevision,Kind=CommandKind.PublishShipBlueprint,PublishShipBlueprint=new(){BlueprintId="blueprint.ready."+role,Revision=2,Draft=revised}}).Accepted);
  var library=app.CaptureDesignLibrary(Human);Assert.NotEqual(library.Published.Single(x=>x.Blueprint.BlueprintId=="blueprint.ready."+role&&x.Blueprint.Revision==1).Blueprint.Fit.HullVersionId,library.Published.Single(x=>x.Blueprint.BlueprintId=="blueprint.ready."+role&&x.Blueprint.Revision==2).Blueprint.Fit.HullVersionId);
  var born=app.ExportStreamedActivitiesSave().PublishedBuiltShips!.Single(x=>x.OwnerCivilizationId==Human);Assert.Equal(1,born.BlueprintRevision);var launch=app.CaptureFrontierCommission(Human).Objectives.Single(o=>o.Id=="ship");Assert.NotNull(launch.Evidence);
  var system=app.CaptureStrategy(Human).Colonies.Single(c=>c.ColonyId.Value=="colony.sol").SystemId;var home=app.CaptureGeneratedMap(Human).Regions.Single(r=>r.SystemId==system.Value).NodeId;
  void Order(string fleet,CampaignTravelIntent intent,IReadOnlyList<string> steps){var stamp=app.CaptureStatus();Assert.True(app.SubmitTravelOrder(new(){ActorId=Human,CommandId="command.commission.guard."+stamp.CampaignRevision,ExpectedTurn=stamp.Turn,ExpectedRevision=stamp.CampaignRevision,Order=new(){FleetId=fleet,Intent=intent,RouteNodeIds=steps.ToList()}}).Accepted);}
  var visited=new HashSet<string>();
  for(int i=0;i<8&&app.CaptureStatus().Phase==SessionPhase.Planning;i++)
  {
   foreach(var fleet in app.GetOwnStrategicFleets(Human))
   {visited.Add(fleet.LocationNodeId.Value);if(fleet.TransitTotalCost>0||fleet.RouteNodeIds.Count>0)continue;var path=app.CaptureGeneratedMap(Human).Regions.Where(r=>!visited.Contains(r.NodeId)).Select(r=>app.FindKnownRoute(Human,fleet.LocationNodeId.Value,r.NodeId)).Where(r=>r.Success&&r.TotalCost>0).OrderBy(r=>r.TotalCost).ThenBy(r=>r.Steps.Last(),StringComparer.Ordinal).FirstOrDefault();if(path!=null)Order(fleet.FleetId.Value,CampaignTravelIntent.Leave,path.Steps);}
   Step();
  }
  var guarded=new HashSet<string>();var returning=new HashSet<string>();bool fought=false;
  for(int i=0;i<128&&app.ExportStreamedActivitiesSave().Campaign!.Integrated!.Physical!.Core.Campaign.GameState.Ships!.Any(s=>s.ShipId==born.ShipId);i++)
  {
   if(app.CaptureStatus().Phase==SessionPhase.AwaitingCombat)
   {
    fought=true;foreach(var id in app.GetEncounters(Human))while(!app.Observe(id,Human).Complete){var o=app.Observe(id,Human);Assert.True(app.CommitRound(new(){ActorId=Human,CommandId="command.commission.round."+app.CaptureStatus().CampaignRevision,EncounterId=id,ExpectedTurn=o.Turn,ExpectedRound=o.Round,ExpectedBoundaryRevision=o.BoundaryRevision,Replacements=new()}).Accepted);}
    var status=app.CaptureStatus();Assert.True(app.FinalizeBatch(status.Turn,status.CampaignRevision).Accepted);continue;
   }
   foreach(var fleet in app.GetOwnStrategicFleets(Human))
   {if(fleet.TransitTotalCost>0||fleet.RouteNodeIds.Count>0)continue;if(fleet.LocationNodeId.Value==home){if(guarded.Add(fleet.FleetId.Value))Order(fleet.FleetId.Value,CampaignTravelIntent.StayEngage,Array.Empty<string>());}else if(returning.Add(fleet.FleetId.Value)){var route=app.FindKnownRoute(Human,fleet.LocationNodeId.Value,home);Assert.True(route.Success);Order(fleet.FleetId.Value,CampaignTravelIntent.Leave,route.Steps);}}
   Step();
  }
  var root=app.ExportStreamedActivitiesSave();Assert.True(fought);Assert.DoesNotContain(root.Campaign!.Integrated!.Physical!.Core.Campaign.GameState.Ships!,s=>s.ShipId==born.ShipId);Assert.Contains(root.PublishedBuiltShips!,x=>x.ShipId==born.ShipId);
  Assert.Equal(Json(launch),Json(app.CaptureFrontierCommission(Human).Objectives.Single(o=>o.Id=="ship")));
  void CheckHulls(ICampaignStreamedActivitiesSession current)
  {
   var before=Json(current.ExportStreamedActivitiesSave());var history=current.GetBattleHistory(Human);Assert.NotEmpty(history);
   var seen=new HashSet<string>();
   foreach(var battle in history)foreach(var boundary in new[]{battle.Initial}.Concat(battle.Rounds.SelectMany(r=>new[]{r.Before,r.After})))
   {
    foreach(var identity in boundary.ShipIdentities)
    {
     if(boundary.OwnShips.Any(o=>o.ShipId==identity.ShipId))
     {
      if(identity.ShipId==starter){Assert.Equal("hull.constitution-explorer.v1",identity.OwnHullVersionId);Assert.Equal("hull-family.constitution-explorer",identity.VisibleHullFamilyId);seen.Add(starter);}
      else if(identity.ShipId==born.ShipId){Assert.Equal("hull."+role+".v1",identity.OwnHullVersionId);Assert.Equal("hull-family."+role,identity.VisibleHullFamilyId);Assert.Equal(born.BlueprintId,identity.ShipDesignDefinitionId);seen.Add(born.ShipId!);}
     }
     else Assert.Null(identity.OwnHullVersionId);
    }
   }
   Assert.Contains(starter,seen);Assert.Contains(born.ShipId!,seen);Assert.Equal(before,Json(current.ExportStreamedActivitiesSave()));
  }
  CheckHulls(app);
  var restored=CampaignStreamedActivitiesApplication.LoadSave(p,root);Assert.Equal(Json(root),Json(restored.ExportStreamedActivitiesSave()));Assert.Equal(Json(app.CaptureFrontierCommission(Human)),Json(restored.CaptureFrontierCommission(Human)));CheckHulls(restored);
 }

}
