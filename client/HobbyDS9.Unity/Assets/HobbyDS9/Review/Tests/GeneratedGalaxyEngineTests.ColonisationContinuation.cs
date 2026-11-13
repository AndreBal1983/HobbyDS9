#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using HobbyDS9.Contracts;
using HobbyDS9.TacticalReviewClient;
using Newtonsoft.Json;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace HobbyDS9.UnityReview.Tests
{
    public sealed partial class GeneratedGalaxyEngineTests
    {
        private const string ColonyTailFoundingSha="54454c1921dc7f2b86221132dd8f05e3cf442f245d6145f2aa388bff0ead6dc0";
        private const string ColonyTailMainSha="4954304ce387e60c0eaee1b09c23ee4ea51d1189ca2a7851c940ae21a37eac83";
        private static readonly string[] ColonyTailImages={
            "colony-tail-01-evacuated-1920.png","colony-tail-02-empty-outpost-1280.png",
            "colony-tail-03-recommissioned-1920.png","colony-tail-04-main-established-1920.png",
            "colony-tail-05-historical-format10-1920.png"};
        private sealed class ColonyTailState
        {
            internal readonly ColonyCase Case=new ColonyCase{RosterV2=true};
            internal readonly System.Diagnostics.Stopwatch Productive=System.Diagnostics.Stopwatch.StartNew();
            internal int Flushed;
            internal string Phase="setup";
            internal void Flush()
            {
                // Inspecting the count is cheap; disk writes happen ONLY for new timing rows.
                while(Flushed<Case.Shared.Timings.Count)
                {
                    File.AppendAllText(Path.Combine(CampaignOutput(),"colony-tail-timings.jsonl"),
                        JsonConvert.SerializeObject(Case.Shared.Timings[Flushed])+Environment.NewLine);
                    Flushed++;
                }
            }
            internal void Mark(string phase)
            {
                Phase=phase;
                File.AppendAllText(Path.Combine(CampaignOutput(),"colony-tail-phases.jsonl"),
                    JsonConvert.SerializeObject(new{phase,productiveMs=Productive.Elapsed.TotalMilliseconds})+Environment.NewLine);
                Flush();
            }
        }

        // The original full-case600-second test remains unchanged. This case starts from saved branches.
        //720 is ONLY600 productive +90 drain + scene-exit margin, never extra gameplay time.
        [UnityTest,Timeout(720000)]
        public IEnumerator ColonisationRosterV2SavedBranchesFinishEvacuationAndRecommission()
        {
            var t=new ColonyTailState();var s=t.Case;var stack=new Stack<IEnumerator>();
            stack.Push(ColonyTailBody(t));Exception? failure=null;
            while(stack.Count>0)
            {
                bool next=false;object? value=null;var current=stack.Peek();
                try
                {
                    if(t.Productive.Elapsed.TotalSeconds>=600)throw new TimeoutException("Saved-tail productive deadline before "+t.Phase);
                    next=current.MoveNext();if(next)value=current.Current;
                    if(t.Productive.Elapsed.TotalSeconds>=600)throw new TimeoutException("Saved-tail productive deadline after "+t.Phase);
                    if(t.Flushed<s.Shared.Timings.Count)t.Flush();
                }
                catch(Exception error){failure=error;}
                if(failure!=null)break;
                if(!next){stack.Pop();try{(current as IDisposable)?.Dispose();}catch(Exception error){failure=error;break;}continue;}
                if(value is IEnumerator nested){stack.Push(nested);continue;}
                yield return value;
            }
            t.Productive.Stop();
            void Cleanup(string phase,Action action)
            {try{action();}catch(Exception error){failure=failure??error;try{CampaignDiagnosticError(phase,error);}catch{/* Never skip release/drain/exit for a receipt failure. */}}}
            Cleanup("tail-release-all",()=>{var error=s.ReleaseAll();failure=failure??error;});
            Cleanup("tail-shared-release",()=>s.Shared.Release.Set());
            if(s.Shared.Entry!=null)Cleanup("tail-disable",()=>s.Shared.Entry.enabled=false);
            while(stack.Count>0)try{(stack.Pop() as IDisposable)?.Dispose();}catch(Exception error){failure=failure??error;}
            bool settled=true;
            bool Pending()
            {
                if(s.Pending||s.Shared.Holder?.IsCompleted==false)return true;
                bool pending=true;Cleanup("tail-drain-status",()=>pending=s.Shared.Entry!=null&&ColonyPending(s.Shared.Entry));return pending;
            }
            var cleanupClock=System.Diagnostics.Stopwatch.StartNew();
            while(Pending())
            {
                if(cleanupClock.Elapsed.TotalSeconds>=90)
                {
                    settled=false;directory=null;
                    failure=failure??new TimeoutException("Saved-tail worker unsettled; temporary namespace/override retained until scene exit.");break;
                }
                yield return null;
            }
            Cleanup("tail-final-timings",t.Flush);
            bool overrideRestored=false;
            if(settled&&s.Shared.Entry!=null&&s.PriorDirectoryCaptured)
                Cleanup("tail-restore-override",()=>{typeof(ReviewEntry).GetField("generatedCampaignTestDirectory",HullPrivate)!.SetValue(s.Shared.Entry,s.Shared.PriorDirectory);overrideRestored=true;});
            Cleanup("tail-cleanup-receipt",()=>File.WriteAllText(Path.Combine(CampaignOutput(),"colony-tail-cleanup.json"),JsonConvert.SerializeObject(new{
                settled,allReleasesSet=s.Holds.All(h=>h.Release.IsSet),priorOverrideCaptured=s.PriorDirectoryCaptured,
                overrideRestored,directoryRetained=!settled,entryDisabled=s.Shared.Entry==null||!s.Shared.Entry.enabled,
                cleanupMs=cleanupClock.Elapsed.TotalMilliseconds,productiveMs=t.Productive.Elapsed.TotalMilliseconds,
                productiveLimitSeconds=600,drainLimitSeconds=90,outerFrameworkSeconds=720,failed=failure!=null,lastPhase=t.Phase})));
            if(settled)foreach(var host in s.Hosts)Cleanup("tail-remove-probe",()=>ColonyProbe(host,null));
            if(!s.Pending)foreach(var h in s.Holds){Cleanup("tail-dispose-entered",()=>h.Entered.Dispose());Cleanup("tail-dispose-release",()=>h.Release.Dispose());}
            if(s.Shared.Holder==null||s.Shared.Holder.IsCompleted)
            {Cleanup("tail-dispose-shared-entered",()=>s.Shared.Entered.Dispose());Cleanup("tail-dispose-shared-release",()=>s.Shared.Release.Dispose());}
            yield return new ExitPlayMode();
            if(failure!=null)System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
        }

        private static long TailKits(ColonisationWorldSnapshot world)=>world.Stocks.Where(x=>x.ResourceDefinitionId=="resource.settlement-kit").Sum(x=>x.Amount);
        private static void TailSelectPublicFounding(ColonyCase s)
        {
            var model=ColonyModel(s);var own=model.Colonisation();
            var target=own.Worlds.Single(x=>x.Stage==ColonyEstablishmentStage.Founding);
            s.TargetWorld=target.Id;s.TargetPlanet=target.PlanetId;
            s.TargetNode=model.GeneratedMap().Regions.Single(x=>x.SystemId==target.SystemId).NodeId;
            var home=own.Worlds.Where(x=>x.Stage==ColonyEstablishmentStage.Established&&x.CampId==null&&x.Population>=600&&x.Cohorts.Any(c=>c.Population>=400))
                .OrderByDescending(x=>x.Population).ThenBy(x=>x.Id,StringComparer.Ordinal).First();
            s.Home=home.Id;s.HomeSystem=home.SystemId;s.HomeNode=model.GeneratedMap().Regions.Single(x=>x.SystemId==home.SystemId).NodeId;
            var present=own.Transports.Where(x=>x.Living&&x.Stationary&&x.NodeId==s.TargetNode&&x.BlueprintRevision==1&&x.PassengerCapacity==100).ToArray();
            s.Transport=present.Single(x=>x.ManifestId==null).ShipId;
            s.SecondTransport=present.Single(x=>x.Kind==SettlerManifestKind.Settlement&&x.Population>0&&x.Cargo.Any(c=>c.ResourceDefinitionId=="resource.settlement-kit"&&c.Amount==1)).ShipId;
            Assert.That(target.Population,Is.InRange(1L,100L));Assert.That(target.CampId,Is.Not.Null);
        }
        private static IEnumerator TailUnloadAtHome(ColonyTailState t,UIDocument doc,string ship)
        {
            var s=t.Case;yield return ColonyMove(s,doc,ship,s.HomeNode);yield return ColonyTransport(s,doc,s.Home,ship);
            var before=ColonyModel(s).Colonisation().Worlds.Single(x=>x.Id==s.Home);
            var cargo=ColonyModel(s).Colonisation().Transports.Single(x=>x.ShipId==ship);
            Assert.That(cargo.Living,Is.True);Assert.That(cargo.Population,Is.GreaterThan(0));Assert.That(cargo.ManifestId,Is.Not.Null);
            var manifest=cargo.ManifestId;var kits=cargo.Cargo.Where(x=>x.ResourceDefinitionId=="resource.settlement-kit").Sum(x=>x.Amount);
            yield return ColonyOrder(s,doc,"colonisation-unload");
            var after=ColonyModel(s).Colonisation().Worlds.Single(x=>x.Id==s.Home);
            Assert.That(after.Population,Is.EqualTo(before.Population+cargo.Population));Assert.That(after.Food,Is.EqualTo(before.Food+cargo.Food));
            foreach(var material in cargo.Cargo)
                Assert.That(after.Stocks.Where(x=>x.ResourceDefinitionId==material.ResourceDefinitionId).Sum(x=>x.Amount),
                    Is.EqualTo(before.Stocks.Where(x=>x.ResourceDefinitionId==material.ResourceDefinitionId).Sum(x=>x.Amount)+material.Amount));
            Assert.That(ColonyModel(s).Colonisation().Transports.Single(x=>x.ShipId==ship).ManifestId,Is.Null);
            var report=ColonyModel(s).Colonisation().Reports.Single(x=>x.ManifestId==manifest);Assert.That(report.Stage,Is.EqualTo(SettlerManifestStage.Unloaded));
            // Historical root is audit only, never a source for any command selection.
            var retained=ColonyAudit(ColonyModel(s)).Colonisation!.ManifestRecords!.Single(x=>x.ManifestId==manifest);
            Assert.That(retained.FoodConsumed,Is.GreaterThanOrEqualTo(0));Assert.That(retained.PassengerDeaths,Is.GreaterThanOrEqualTo(0));
            s.Shared.Timings.Add(new{phase="tail-actual-home-unload",ship,manifest,cargo.Population,cargo.Food,recoveredKits=kits,
                retained.FoodConsumed,retained.PassengerDeaths,returnKind=cargo.Kind});
        }
        private IEnumerator ColonyTailBody(ColonyTailState t)
        {
            var s=t.Case;
            yield return new EnterPlayMode();yield return null;yield return null;
            s.Shared.Entry=UnityEngine.Object.FindAnyObjectByType<ReviewEntry>();Assert.That(s.Shared.Entry,Is.Not.Null);
            var entry=s.Shared.Entry!;var field=typeof(ReviewEntry).GetField("generatedCampaignTestDirectory",HullPrivate)!;
            s.Shared.PriorDirectory=(string?)field.GetValue(entry);s.PriorDirectoryCaptured=true;
            // Only immutable pre-ingress fixtures enter the temp store; no body state is injected later.
            var founding=V3Fixture("HOBBYDS9_COLONISATION_V2_FOUNDING",ColonyTailFoundingSha);
            var main=V3Fixture("HOBBYDS9_COLONISATION_V2_ESTABLISHED",ColonyTailMainSha);
            var historical=V3Fixture("HOBBYDS9_ACTIVITY_HULL_COMBAT",ActivityHullFixtureSha);
            directory=Path.Combine(Path.GetTempPath(),"HobbyDS9-Galaxy-Engine-Colony-Tail-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(directory);
            var storePath=Path.Combine(directory,"campaign");field.SetValue(entry,storePath);
            var codec=new ReviewNewtonsoftCodec();var store=ReviewColonisationSlotStore.CreateRosterV2(storePath,codec);
            var foundingRoot=codec.DecodeColonisationRoot(founding);var mainRoot=codec.DecodeColonisationRoot(main);
            Assert.That(codec.EncodeColonisationRoot(foundingRoot),Is.EqualTo(founding));Assert.That(codec.EncodeColonisationRoot(mainRoot),Is.EqualTo(main));
            store.Write("generated-1",mainRoot,false);store.Write("generated-2",foundingRoot,false);
            Assert.That(File.ReadAllBytes(Path.Combine(storePath,s.SlotPrefix+"generated-1.json")),Is.EqualTo(main));
            Assert.That(File.ReadAllBytes(Path.Combine(storePath,s.SlotPrefix+"generated-2.json")),Is.EqualTo(founding));
            var doc=entry.GetComponent<UIDocument>();
            using(var surface=new GalaxyInputSurface(doc))
            {
                t.Mark("founding-continue-begin");
                // Already on the landing menu: do not activate its hidden in-campaign Menu button.
                var priorModel=ColonyModel(s);doc.rootVisualElement.Q<Foldout>(s.MenuPrefix+"-menu").value=true;yield return null;
                var initial=doc.rootVisualElement.Q<Button>(s.MenuPrefix+"-continue-generated-2");yield return ColonyReach(doc,initial,target:"saved founding V2 Continue");
                ColonyActivate(s,()=>{Click(initial);Confirm(doc);});yield return ColonyIdle(s,"tail-founding-continue");
                Assert.That(ColonyModel(s),Is.Not.SameAs(priorModel));Assert.That(ColonyModel(s).ColonisationRosterCampaign,Is.True);
                Assert.That(ColonyBytes(ColonyModel(s)),Is.EqualTo(founding));t.Mark("founding-continue-complete");
                TailSelectPublicFounding(s);
                var before=ColonyModel(s).Colonisation().Worlds.Single(x=>x.Id==s.TargetWorld);var firstCamp=before.CampId;
                var firstHull=ColonyModel(s).Colonisation().Transports.Single(x=>x.ShipId==s.Transport);
                var origin=JsonConvert.SerializeObject(ColonyAudit(ColonyModel(s)).Colonisation!.ColonyOrigins!.Single(x=>x.ColonyId==s.TargetWorld));
                var campCount=ColonyAudit(ColonyModel(s)).Colonisation!.CampSources!.Count(x=>x.ColonyId==s.TargetWorld);
                t.Mark("evacuation-begin");yield return ColonyTransport(s,doc,s.TargetWorld);yield return ColonyOrder(s,doc,"colonisation-evacuate");
                var evacuated=ColonyModel(s).Colonisation().Transports.Single(x=>x.ShipId==s.Transport);
                var empty=ColonyModel(s).Colonisation().Worlds.Single(x=>x.Id==s.TargetWorld);
                Assert.That(evacuated.Kind,Is.EqualTo(SettlerManifestKind.ReturnOnly));Assert.That(evacuated.Population,Is.EqualTo(before.Population));
                Assert.That(empty.Stage,Is.EqualTo(ColonyEstablishmentStage.EmptyOutpost));Assert.That(empty.Cohorts,Is.Empty);Assert.That(empty.CampId,Is.EqualTo(firstCamp));
                Assert.That(empty.Food+evacuated.Food,Is.EqualTo(before.Food));
                yield return ColonyCapture(s,doc,new Vector2Int(1920,1080),ColonyTailImages[0]);
                yield return ColonyIncomingRejected(s,doc,s.Transport);yield return ColonyIncomingRejected(s,doc,s.SecondTransport);t.Mark("evacuation-complete");
                t.Mark("return-unload-begin");yield return TailUnloadAtHome(t,doc,s.Transport);yield return TailUnloadAtHome(t,doc,s.SecondTransport);t.Mark("return-unload-complete");
                yield return ColonyNoCohort(s,doc);
                var noCohort=doc.rootVisualElement.Q<Button>("colonisation-embark");doc.rootVisualElement.Q<ScrollView>("colonisation-scroll").ScrollTo(noCohort);yield return null;yield return null;
                yield return ColonyCapture(s,doc,new Vector2Int(1280,720),ColonyTailImages[1]);
                t.Mark("fresh-kit-begin");yield return ColonyWorld(s,doc,s.Home);
                var kits=TailKits(ColonyModel(s).Colonisation().Worlds.Single(x=>x.Id==s.Home));var credits=ColonyModel(s).ColonisationActivities().Credits;
                yield return ColonyClick(s,doc,"colonisation-build-kit");Assert.That(ColonyModel(s).ColonisationActivities().Credits,Is.EqualTo(credits));
                var kit=ColonyModel(s).Colonisation().Production.Single(x=>x.ColonyId==s.Home&&x.Kind==ColonisationProductionKind.SettlementKit).Id;
                yield return ColonyManufacture(s,doc,2);Assert.That(TailKits(ColonyModel(s).Colonisation().Worlds.Single(x=>x.Id==s.Home)),Is.EqualTo(kits+1));
                Assert.That(ColonyAudit(ColonyModel(s)).ProductionHistory!.Count(x=>x.Completed&&x.Order!.QueueItemId==kit),Is.EqualTo(1));t.Mark("fresh-kit-complete");
                t.Mark("recommission-begin");yield return ColonyLoadOne(s,doc,s.Transport);yield return ColonyMove(s,doc,s.Transport,s.TargetNode);
                yield return ColonyIncomingRejected(s,doc,s.Transport);yield return ColonyDeploy(s,doc);yield return ColonyEstablished(s,doc);
                var recommissioned=ColonyModel(s).Colonisation().Worlds.Single(x=>x.Id==s.TargetWorld);
                Assert.That(recommissioned.PlanetId,Is.EqualTo(s.TargetPlanet));Assert.That(recommissioned.CampId,Is.Not.EqualTo(firstCamp));
                Assert.That(recommissioned.EstablishmentWork,Is.GreaterThanOrEqualTo(16));Assert.That(recommissioned.FedWeeks,Is.GreaterThanOrEqualTo(4));
                var reused=ColonyModel(s).Colonisation().Transports.Single(x=>x.ShipId==s.Transport);
                Assert.That(reused.BlueprintId,Is.EqualTo(firstHull.BlueprintId));Assert.That(reused.BlueprintRevision,Is.EqualTo(firstHull.BlueprintRevision));Assert.That(reused.HullVersionId,Is.EqualTo(firstHull.HullVersionId));
                var audit=ColonyAudit(ColonyModel(s)).Colonisation!;
                Assert.That(JsonConvert.SerializeObject(audit.ColonyOrigins!.Single(x=>x.ColonyId==s.TargetWorld)),Is.EqualTo(origin));
                Assert.That(audit.CampSources!.Count(x=>x.ColonyId==s.TargetWorld),Is.EqualTo(campCount+1));
                yield return ColonySaveContinue(s,doc,"tail-recommissioned",3);yield return ColonyWorld(s,doc,s.TargetWorld);
                yield return ColonyCapture(s,doc,new Vector2Int(1920,1080),ColonyTailImages[2]);yield return ColonyHistoryComponent(s,doc);t.Mark("recommission-complete");
                t.Mark("main-restore-begin");yield return ColonyContinue(s,doc,"tail-main-established",main,1);
                var mainWorld=ColonyModel(s).Colonisation().Worlds.Single(x=>x.Id==s.TargetWorld);
                Assert.That(mainWorld.Stage,Is.EqualTo(ColonyEstablishmentStage.Established));Assert.That(mainWorld.Population,Is.GreaterThan(0));Assert.That(mainWorld.Industry,Is.GreaterThan(0));
                Assert.That(ColonyAudit(ColonyModel(s)).ProductionHistory!.Any(x=>x.Completed&&x.Order!.ColonyId==mainWorld.Id&&x.Order.DefinitionId=="infrastructure.frontier-mine.v1"),Is.True);
                var mainMission=JsonConvert.SerializeObject(ColonyModel(s).FrontierCommission());
                yield return ColonyWorld(s,doc,mainWorld.Id);yield return ColonyCapture(s,doc,new Vector2Int(1920,1080),ColonyTailImages[3]);
                Assert.That(JsonConvert.SerializeObject(ColonyModel(s).FrontierCommission()),Is.EqualTo(mainMission));t.Mark("main-restore-complete");
                t.Mark("historical-continue-begin");var old=codec.DecodeStreamedActivitiesRoot(historical);var canonicalOld=codec.EncodeStreamedActivitiesRoot(old);
                new ReviewStreamedActivitiesSlotStore(storePath,codec).Write("generated-3",old,false);
                Click(doc.rootVisualElement.Q<Button>("demo-menu"));yield return null;doc.rootVisualElement.Q<Foldout>("streamed-activities-campaign-menu").value=true;yield return null;
                ColonyActivate(s,()=>{Click(doc.rootVisualElement.Q<Button>("streamed-activities-campaign-continue-generated-3"));Confirm(doc);});
                yield return ColonyIdle(s,"tail-historical-format10");var oldModel=ColonyModel(s);
                Assert.That(oldModel.ColonisationCampaign,Is.False);Assert.That(oldModel.ColonisationRosterCampaign,Is.False);Assert.That(StreamBytes(oldModel),Is.EqualTo(canonicalOld));
                CampaignPage(doc,ReviewWorkspacePage.Map);yield return null;yield return null;
                // Reuse the Format10 capture helper, retaining its original pixels and layout checks.
                yield return StreamCapture(doc,new Vector2Int(1920,1080),"tail-historical-format10");
                File.Move(Path.Combine(CampaignOutput(),"streamed-tail-historical-format10-1920x1080.png"),Path.Combine(CampaignOutput(),ColonyTailImages[4]));
                Assert.That(StreamBytes(ColonyModel(s)),Is.EqualTo(canonicalOld));t.Mark("historical-continue-complete");
                Assert.That(Directory.GetFiles(CampaignOutput(),"colony-tail-*.png").Select(Path.GetFileName),Is.EquivalentTo(ColonyTailImages));
                File.WriteAllText(Path.Combine(CampaignOutput(),"colony-tail-results.json"),JsonConvert.SerializeObject(new{
                    passed=true,savedBranchContinuationOnly=true,freshFullJourney=false,originalFullCasePassed=false,
                    foundingSha256=ColonyTailFoundingSha,mainSha256=ColonyTailMainSha,historicalSha256=ActivityHullFixtureSha,
                    actualEvacuation=true,returnOnlyAndSettlementUnloaded=true,emptyOutpostNoPassengerBypass=true,
                    actualNewKitAndSameR1Transport=true,recommissionFedWeeks=recommissioned.FedWeeks,
                    sameColonyOrigin=true,recommissionSaveContinueWholeRoot=true,mainWholeRootRestored=true,
                    historicalCanonicalWholeRoot=true,historyNoLiveTransportComponentOnly=true,nativeInput=false,captures=ColonyTailImages}));
                t.Mark("all-tail-gates-complete");
            }
        }
    }
}
