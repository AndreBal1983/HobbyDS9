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
        private static readonly string[] HumanImages={
            "human-01-established-1280.png","human-02-established-1920.png","human-03-established-3440.png"};
        private sealed class HumanReceivingState
        {
            internal readonly ColonyCase Case=new ColonyCase(); // Reuse tracking/helpers only; no old-profile persistence helper is called.
            internal readonly System.Diagnostics.Stopwatch Productive=System.Diagnostics.Stopwatch.StartNew();
            internal int Flushed,UiAdvances,LoadedAdvances,SurveyBodies,FrontierMoves;
            internal string Actor="",TransportBlueprint="",TransportManifest="",SupplyManifest="";
            internal ReviewController? Controller;
            internal string Phase="setup";
            internal void Flush()
            {
                // Inspecting the count is cheap; disk writes happen ONLY for new timing rows.
                while(Flushed<Case.Shared.Timings.Count)
                {
                    File.AppendAllText(Path.Combine(CampaignOutput(),"human-timings.jsonl"),
                        JsonConvert.SerializeObject(Case.Shared.Timings[Flushed])+Environment.NewLine);
                    Flushed++;
                }
            }
            internal void Mark(string phase)
            {
                Phase=phase;
                File.AppendAllText(Path.Combine(CampaignOutput(),"human-phases.jsonl"),
                    JsonConvert.SerializeObject(new{phase,productiveMs=Productive.Elapsed.TotalMilliseconds})+Environment.NewLine);
                Flush();
            }
        }

        // One explicit-menu expansion case; no original receiving test is changed or relabelled.
        //720 is ONLY600 productive +90 drain + scene-exit margin, never extra gameplay time.
        [UnityTest,Timeout(720000)]
        public IEnumerator ColonisationExpansionHumanExpeditionFromFreshCampaign()
        {
            var t=new HumanReceivingState();var s=t.Case;var stack=new Stack<IEnumerator>();
            stack.Push(HumanReceivingBody(t));Exception? failure=null;
            while(stack.Count>0)
            {
                bool next=false;object? value=null;var current=stack.Peek();
                try
                {
                    if(t.Productive.Elapsed.TotalSeconds>=600)throw new TimeoutException("Human expedition productive deadline before "+t.Phase);
                    next=current.MoveNext();if(next)value=current.Current;
                    if(t.Productive.Elapsed.TotalSeconds>=600)throw new TimeoutException("Human expedition productive deadline after "+t.Phase);
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
            Cleanup("human-release-all",()=>{var error=s.ReleaseAll();failure=failure??error;});
            Cleanup("human-shared-release",()=>s.Shared.Release.Set());
            if(s.Shared.Entry!=null)Cleanup("human-disable",()=>s.Shared.Entry.enabled=false);
            while(stack.Count>0)try{(stack.Pop() as IDisposable)?.Dispose();}catch(Exception error){failure=failure??error;}
            bool settled=true;
            bool Pending()
            {
                if(s.Pending||s.Shared.Holder?.IsCompleted==false)return true;
                bool pending=true;Cleanup("human-drain-status",()=>pending=s.Shared.Entry!=null&&ColonyPending(s.Shared.Entry));return pending;
            }
            var cleanupClock=System.Diagnostics.Stopwatch.StartNew();
            while(Pending())
            {
                if(cleanupClock.Elapsed.TotalSeconds>=90)
                {
                    settled=false;directory=null;
                    failure=failure??new TimeoutException("Human expedition worker unsettled; temporary namespace/override retained until scene exit.");break;
                }
                yield return null;
            }
            Cleanup("human-final-timings",t.Flush);
            bool overrideRestored=false;
            if(settled&&s.Shared.Entry!=null&&s.PriorDirectoryCaptured)
                Cleanup("human-restore-override",()=>{typeof(ReviewEntry).GetField("generatedCampaignTestDirectory",HullPrivate)!.SetValue(s.Shared.Entry,s.Shared.PriorDirectory);overrideRestored=true;});
            Cleanup("human-cleanup-receipt",()=>File.WriteAllText(Path.Combine(CampaignOutput(),"human-cleanup.json"),JsonConvert.SerializeObject(new{
                settled,allReleasesSet=s.Holds.All(h=>h.Release.IsSet),priorOverrideCaptured=s.PriorDirectoryCaptured,
                overrideRestored,directoryRetained=!settled,entryDisabled=s.Shared.Entry==null||!s.Shared.Entry.enabled,
                cleanupMs=cleanupClock.Elapsed.TotalMilliseconds,productiveMs=t.Productive.Elapsed.TotalMilliseconds,
                productiveLimitSeconds=600,drainLimitSeconds=90,outerFrameworkSeconds=720,failed=failure!=null,lastPhase=t.Phase})));
            if(settled)foreach(var host in s.Hosts)Cleanup("human-remove-probe",()=>ColonyProbe(host,null));
            if(!s.Pending)foreach(var h in s.Holds){Cleanup("human-dispose-entered",()=>h.Entered.Dispose());Cleanup("human-dispose-release",()=>h.Release.Dispose());}
            if(s.Shared.Holder==null||s.Shared.Holder.IsCompleted)
            {Cleanup("human-dispose-shared-entered",()=>s.Shared.Entered.Dispose());Cleanup("human-dispose-shared-release",()=>s.Shared.Release.Dispose());}
            yield return new ExitPlayMode();
            if(failure!=null)System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
        }


        private static IEnumerator HumanRead(UIDocument doc,Label label)
        {
            Assert.That(label,Is.Not.Null);Assert.That(label.text,Is.Not.Empty);
            var parents=new List<ScrollView>();for(var at=label.parent;at!=null;at=at.parent)if(at is ScrollView view)parents.Add(view);
            foreach(var view in parents){view.ScrollTo(label);yield return null;yield return null;PublishedReceivingContained(view.contentViewport.worldBound,label,label.name);}
            Assert.That(label.panel,Is.SameAs(doc.rootVisualElement.panel));Assert.That(label.resolvedStyle.fontSize,Is.GreaterThanOrEqualTo(16));
            Assert.That(label.worldBound.yMax,Is.LessThanOrEqualTo(doc.rootVisualElement.Q("player-footer").worldBound.yMin));
        }
        private static IEnumerator HumanCapture(HumanReceivingState t,UIDocument doc,Vector2Int size,string filename,string hydroId)
        {
            var s=t.Case;var selection=ColonySelection(doc.rootVisualElement.Q<ReviewColonisationPanel>());
            var oldTarget=doc.panelSettings.targetTexture;var oldWidth=doc.rootVisualElement.style.width;var oldHeight=doc.rootVisualElement.style.height;
            var active=RenderTexture.active;var target=new RenderTexture(size.x,size.y,24,RenderTextureFormat.ARGB32);Texture2D? pixels=null;
            try
            {
                Assert.That(target.Create(),Is.True);doc.panelSettings.targetTexture=target;doc.rootVisualElement.style.width=size.x;doc.rootVisualElement.style.height=size.y;
                yield return null;yield return null;yield return null;
                var panel=doc.rootVisualElement.Q<ReviewColonisationPanel>();Assert.That(panel,Is.Not.Null);
                var guide=panel.Q<Foldout>("colonisation-preparation-guide");Assert.That(guide,Is.Not.Null);guide.value=size.x==1280;yield return null;yield return null;
                var outer=panel.Q<ScrollView>("colonisation-scroll");var catalogue=panel.Q<ScrollView>("colonisation-catalogue");
                var effects=panel.Q<Label>("colonisation-building-effects-"+hydroId);Assert.That(effects,Is.Not.Null);
                catalogue.ScrollTo(effects);outer.ScrollTo(size.x==1280?(VisualElement)guide:panel.Q("colonisation-food-balance"));yield return null;yield return null;
                RenderTexture.active=target;pixels=new Texture2D(size.x,size.y,TextureFormat.RGBA32,false);pixels.ReadPixels(new Rect(0,0,size.x,size.y),0,0);pixels.Apply();
                File.WriteAllBytes(Path.Combine(CampaignOutput(),filename),pixels.EncodeToPNG()); // Actual intended image precedes geometry assertions.
                File.WriteAllText(Path.Combine(CampaignOutput(),filename.Replace(".png",".layout.json")),JsonConvert.SerializeObject(new{
                    root=doc.rootVisualElement.worldBound.ToString(),panel=panel.worldBound.ToString(),viewport=outer.contentViewport.worldBound.ToString(),
                    guide=guide.worldBound.ToString(),guideExpanded=guide.value,effects=effects.worldBound.ToString(),selection=ColonySelection(panel)}));
                Assert.That(guide.value,Is.EqualTo(size.x==1280));Assert.That(doc.rootVisualElement.Query<ReviewColonisationPanel>().ToList().Count,Is.EqualTo(1));
                Assert.That(outer.contentViewport.worldBound.height,Is.GreaterThanOrEqualTo(100));
                Assert.That(panel.worldBound.yMax,Is.LessThanOrEqualTo(doc.rootVisualElement.Q("player-footer").worldBound.yMin));
                foreach(var tab in new[]{"Worlds","Planets","Transports","Science"})
                {var button=panel.Q<Button>("colonisation-tab-"+tab);Assert.That(button.worldBound.height,Is.GreaterThanOrEqualTo(44));Assert.That(button.resolvedStyle.fontSize,Is.GreaterThanOrEqualTo(16));}
                // The expanded guide, facts, building effects and action need not fit simultaneously. Exercise normal scrolling/reach.
                yield return ColonyReach(doc,guide.Q<Toggle>(),target:"preparation guide foldout header");
                yield return HumanRead(doc,panel.Q<Label>("colonisation-food-balance"));
                yield return HumanRead(doc,effects);
                yield return ColonyReach(doc,panel.Q<Button>("colonisation-build-kit"),target:"own settlement project remains reachable");
            }
            finally
            {doc.panelSettings.targetTexture=oldTarget;doc.rootVisualElement.style.width=oldWidth;doc.rootVisualElement.style.height=oldHeight;RenderTexture.active=active;if(pixels!=null)UnityEngine.Object.DestroyImmediate(pixels);target.Release();UnityEngine.Object.DestroyImmediate(target);}
            yield return null;yield return null;
            Assert.That(ColonySelection(doc.rootVisualElement.Q<ReviewColonisationPanel>()),Is.EqualTo(selection),"resize preserves selected own world");
        }

        private static IEnumerator HumanAdvance(HumanReceivingState t,UIDocument doc,string phase)
        {
            Assert.That(t.UiAdvances,Is.LessThan(64),"one global normal-Advance bound across every stage");
            var aboard=ColonyModel(t.Case).Colonisation().Transports.SingleOrDefault(x=>x.ShipId==t.Case.Transport);
            if(aboard?.Kind==SettlerManifestKind.Settlement&&aboard.ManifestId!=null)
            {Assert.That(t.LoadedAdvances,Is.LessThan(12),"total loaded weeks includes movement, cover and landing, not only route steps");t.LoadedAdvances++;}
            t.UiAdvances++;yield return ColonyAdvance(t.Case,doc);
            var model=ColonyModel(t.Case);Assert.That(model.Current,Is.SameAs(t.Controller));Assert.That(model.Current.Frame.Actor,Is.EqualTo(t.Actor));
            var own=model.Colonisation();
            File.AppendAllText(Path.Combine(CampaignOutput(),"human-week-observations.jsonl"),JsonConvert.SerializeObject(new{phase,week=t.UiAdvances,own.Turn,
                worlds=own.Worlds.Select(w=>new{w.Id,w.Population,w.Food,w.FoodOutput,w.FoodDemand,w.ShortageWeeks,w.Stage,w.FedWeeks,w.EstablishmentWork}),
                transports=own.Transports.Select(x=>new{x.ShipId,x.ManifestId,x.Population,x.Food,x.Deaths,x.Living,x.Stationary,x.NodeId})})+Environment.NewLine);
            t.Mark(phase+"-week-"+own.Turn);
        }
        private static IEnumerator HumanReview(HumanReceivingState t,UIDocument doc,string id)
        {
            var before=ExpansionBytes(ColonyModel(t.Case));yield return ColonyClick(t.Case,doc,id);
            Assert.That(ExpansionBytes(ColonyModel(t.Case)),Is.EqualTo(before),"typed12 preview is pure");
            Assert.That(doc.rootVisualElement.Q<Button>("colonisation-order-confirm"),Is.Not.Null,"actual current worker-reviewed availability is authority: "+id);
        }
        private static IEnumerator HumanOrder(HumanReceivingState t,UIDocument doc,string id)
        {yield return HumanReview(t,doc,id);yield return ColonyClick(t.Case,doc,"colonisation-order-confirm");}
        private static IEnumerator HumanMove(HumanReceivingState t,UIDocument doc,string ship,string destination,bool loaded=false)
        {
            var s=t.Case;var own=ColonyModel(s).ColonisationActivities().Ships.Single(x=>x.ShipId==ship);
            if(own.Stationary&&own.NodeId==destination)yield break;
            var route=ColonyModel(s).Current.FindKnownRoute(own.FleetId,destination);Assert.That(route?.Route.Success,Is.True,"actual actor-known route");
            s.Shared.Timings.Add(new{phase="human-route-offer",own.FleetId,from=own.NodeId,destination,route!.Route.TotalCost,qualification="Public effective cost; not a fabricated hop-based ETA."});
            yield return StreamRoute(doc,s.Shared.Entry!,own.FleetId,destination);
            int advances=0;
            while(!ColonyModel(s).ColonisationActivities().Ships.Single(x=>x.ShipId==ship).Stationary)
            {
                Assert.That(advances++,Is.LessThan(loaded?12:16),"subordinate movement bound");yield return HumanAdvance(t,doc,loaded?"loaded-voyage":"ordinary-movement");
                if(loaded){var cargo=ColonyModel(s).Colonisation().Transports.Single(x=>x.ShipId==ship);Assert.That(cargo.Living,Is.True);Assert.That(cargo.Population,Is.EqualTo(100));Assert.That(cargo.Deaths,Is.Zero);Assert.That(cargo.Food,Is.GreaterThan(4),"retain finite food for establishment; no silent starving arrival");}
            }
            Assert.That(ColonyModel(s).ColonisationActivities().Ships.Single(x=>x.ShipId==ship).NodeId,Is.EqualTo(destination));
        }
        private static IEnumerator HumanJoin(HumanReceivingState t,UIDocument doc)
        {
            var s=t.Case;var wanted=new[]{s.Explorer,s.Escort,s.Transport};
            foreach(var id in wanted)yield return HumanMove(t,doc,id,s.HomeNode);
            string Fleet(string id)=>ColonyModel(s).ColonisationActivities().Ships.Single(x=>x.ShipId==id).FleetId;
            var target=Fleet(s.Explorer);var transfers=wanted.Where(x=>Fleet(x)!=target).ToArray();
            if(transfers.Length>0)
            {
                foreach(var fleet in wanted.Select(Fleet).Distinct(StringComparer.Ordinal).ToArray())
                {
                    CampaignPage(doc,ReviewWorkspacePage.Map);yield return V3Settled(doc);Click(doc.rootVisualElement.Q<Button>("map-fleet-"+fleet));yield return V3Settled(doc);
                    CampaignPage(doc,ReviewWorkspacePage.FleetOrders);yield return null;yield return null;
                    var remain=doc.rootVisualElement.Q<Button>("fleet-clear-route");yield return ColonyReach(doc,remain,target:"own stationary fleet Remain before lawful organization");
                    ColonyActivate(s,()=>{Click(remain);Confirm(doc);});yield return ColonyIdle(s,"human-colocated-remain");
                }
                foreach(var ship in transfers)
                {
                    if(doc.rootVisualElement.Q<Toggle>("fleet-member-"+ship)==null){Click(doc.rootVisualElement.Q<Button>("fleet-roster-"+Fleet(ship)));yield return null;yield return null;}
                    var member=doc.rootVisualElement.Q<Toggle>("fleet-member-"+ship);yield return ColonyReach(doc,member,target:"actual own colocated member");member.value=true;yield return null;yield return null;
                }
                var join=doc.rootVisualElement.Q<Button>("fleet-transfer-"+target);yield return ColonyReach(doc,join,target:"actual normal own fleet transfer");
                ColonyActivate(s,()=>{Click(join);Confirm(doc);});yield return ColonyIdle(s,"human-colocated-transfer");
            }
            Assert.That(wanted.All(x=>Fleet(x)==target),Is.True,"actual autojoin or ordinary accepted organization, never silent merge");
        }
        private static IEnumerator HumanCover(HumanReceivingState t,UIDocument doc,string phase)
        {
            var s=t.Case;var fleet=ColonyModel(s).ColonisationActivities().Ships.Single(x=>x.ShipId==s.Explorer).FleetId;
            Assert.That(ColonyModel(s).ColonisationActivities().Ships.Single(x=>x.ShipId==s.Escort).FleetId,Is.EqualTo(fleet));
            CampaignPage(doc,ReviewWorkspacePage.Map);yield return V3Settled(doc);Click(doc.rootVisualElement.Q<Button>("map-fleet-"+fleet));yield return V3Settled(doc);
            CampaignPage(doc,ReviewWorkspacePage.FleetOrders);yield return null;yield return null;
            var engage=doc.rootVisualElement.Q<Button>("player-stay-engage");yield return ColonyReach(doc,engage,target:"ordinary own standing StayEngage; no enemy identity");
            ColonyActivate(s,()=>{Click(engage);Confirm(doc);});yield return ColonyIdle(s,phase+"-standing-order");
            int prior=ColonyModel(s).History().Count;yield return HumanAdvance(t,doc,phase);
            s.Shared.Timings.Add(new{phase,actualEncounterDelta=ColonyModel(s).History().Count-prior,qualification="Zero encounters is not a battle-win claim."});
        }
        private static string HumanFrontier(HumanReceivingState t)
        {
            var s=t.Case;var model=ColonyModel(s);var own=model.ColonisationActivities().Ships.Single(x=>x.ShipId==s.Explorer);var map=model.GeneratedMap();
            var excluded=ColonyObservedRisk(s,"human-known-frontier");var distance=new Dictionary<string,int>(StringComparer.Ordinal){{own.NodeId,0}};var q=new Queue<string>();q.Enqueue(own.NodeId);
            while(q.Count>0){var node=q.Dequeue();foreach(var edge in map.Connections.Where(e=>e.EndpointA==node||e.EndpointB==node)){var next=edge.EndpointA==node?edge.EndpointB:edge.EndpointA;if(distance.ContainsKey(next))continue;distance.Add(next,distance[node]+1);q.Enqueue(next);}}
            var offers=map.Regions.Where(x=>!s.SurveyVisited.Contains(x.NodeId)&&!excluded.Contains(x.NodeId)&&distance.ContainsKey(x.NodeId)).OrderBy(x=>distance[x.NodeId]).ThenBy(x=>x.NodeId,StringComparer.Ordinal).Take(8)
                .Select(x=>model.Current.FindKnownRoute(own.FleetId,x.NodeId)).Where(x=>x?.Route.Success==true).OrderBy(x=>x!.Route.TotalCost).ThenBy(x=>x!.Destination,StringComparer.Ordinal).ToArray();
            Assert.That(offers,Is.Not.Empty,"bounded actor-known reachable frontier; no private/fixed-node fallback");return offers[0]!.Destination;
        }
        private static IEnumerator HumanSurvey(HumanReceivingState t,UIDocument doc)
        {
            var s=t.Case;var model=ColonyModel(s);var home=model.Colonisation().Worlds.Single(x=>x.Id==s.Home);
            var homeEnvironment=StreamHost(model).CaptureColonisationPlanets(model.Current.Frame).Single(x=>x.PlanetId==home.PlanetId).Environment!;
            while(s.TargetPlanet=="")
            {
                var own=ColonyModel(s).ColonisationActivities().Ships.Single(x=>x.ShipId==s.Explorer);s.SurveyVisited.Add(own.NodeId);
                var candidates=ColonyModel(s).ColonisationPlanets().Where(x=>x.SystemId!=s.HomeSystem&&!x.Surveyed&&ColonyModel(s).GeneratedMap().Regions.Any(r=>r.NodeId==own.NodeId&&r.SystemId==x.SystemId)).OrderBy(x=>x.PlanetId,StringComparer.Ordinal).ToArray();
                foreach(var planet in candidates)
                {
                    Assert.That(t.SurveyBodies++,Is.LessThan(12),"subordinate actual surveyed-body bound");
                    yield return ColonyPage(s,doc,"Planets");yield return ColonyPick(doc,"colonisation-planet-selector",ColonyModel(s).ColonisationPlanets(),x=>x.PlanetId,planet.PlanetId);
                    yield return ColonyClick(s,doc,"colonisation-survey-start-"+s.Explorer);
                    for(int week=0;week<4&&!ColonyModel(s).ColonisationPlanets().Single(x=>x.PlanetId==planet.PlanetId).Surveyed;week++)yield return HumanAdvance(t,doc,"planetary-survey");
                    var observed=StreamHost(ColonyModel(s)).CaptureColonisationPlanets(ColonyModel(s).Current.Frame).Single(x=>x.PlanetId==planet.PlanetId);Assert.That(observed.Surveyed,Is.True);var e=observed.Environment!;
                    // Conservative public home comparison ONLY selects a candidate; fresh deployment review remains authority.
                    if(!ColonyObservedRisk(s,"human-post-survey",own.NodeId).Contains(own.NodeId)&&observed.KnownControl?.Kind==PlanetControllerKind.Unclaimed
                        &&e.Gravity==homeEnvironment.Gravity&&e.Pressure==homeEnvironment.Pressure&&e.Atmosphere==homeEnvironment.Atmosphere&&e.Radiation<=homeEnvironment.Radiation
                        &&e.Habitats!.Any(h=>h.NaturalCapacity>=100&&homeEnvironment.Habitats!.Any(k=>k.Kind==h.Kind&&k.ShareBps>0)))
                    {s.TargetPlanet=planet.PlanetId;s.TargetNode=own.NodeId;break;}
                }
                if(s.TargetPlanet!="")break;
                Assert.That(t.FrontierMoves++,Is.LessThan(20),"subordinate known-frontier move bound");yield return HumanMove(t,doc,s.Explorer,HumanFrontier(t));
            }
            yield return HumanCover(t,doc,"surveyed-target-cover");yield return HumanMove(t,doc,s.Explorer,s.HomeNode);
            Assert.That(ColonyObservedRisk(s,"human-home-before-load",s.TargetNode).Contains(s.TargetNode),Is.False,"new observed risk fails honestly before people embark");
        }
        private IEnumerator HumanReceivingBody(HumanReceivingState t)
        {
            var s=t.Case;yield return new EnterPlayMode();yield return null;yield return null;
            var entry=UnityEngine.Object.FindAnyObjectByType<ReviewEntry>();Assert.That(entry,Is.Not.Null);s.Shared.Entry=entry;
            var field=typeof(ReviewEntry).GetField("generatedCampaignTestDirectory",HullPrivate)!;s.Shared.PriorDirectory=(string?)field.GetValue(entry);s.PriorDirectoryCaptured=true;
            directory=Path.Combine(Path.GetTempPath(),"HobbyDS9-Galaxy-Engine-Human-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(directory);var storePath=Path.Combine(directory,"campaign");field.SetValue(entry,storePath);
            var doc=entry.GetComponent<UIDocument>();using(var surface=new GalaxyInputSurface(doc))
            {
                t.Mark("new-expansion-begin");doc.rootVisualElement.Q<TextField>("generated-campaign-seed").value="42";doc.rootVisualElement.Q<DropdownField>("generated-campaign-size").value="250";
                doc.rootVisualElement.Q<DropdownField>("generated-campaign-quiet").value="2.00\u00d7";doc.rootVisualElement.Q<Foldout>("colonisation-expansion-campaign-menu").value=true;
                var start=doc.rootVisualElement.Q<Button>("colonisation-expansion-campaign-start");yield return ColonyReach(doc,start,target:"explicit New Expansion");ColonyActivate(s,()=>{Click(start);if(doc.rootVisualElement.Q("review-one-use-confirmation")!=null)Confirm(doc);});yield return ColonyIdle(s,"human-new250");
                var model=ColonyModel(s);Assert.That(model.ColonisationExpansionCampaign,Is.True);t.Controller=model.Current;t.Actor=model.Current.Frame.Actor;
                var initialAudit=ExpansionAudit(model);Assert.That(initialAudit.FormatVersion,Is.EqualTo(12));Assert.That(initialAudit.BaseCampaign!.Map!.Settings!.Map!.Seed,Is.EqualTo("42"));Assert.That(initialAudit.BaseCampaign.Map.Settings.Map.SystemCount,Is.EqualTo(250));
                var own=model.Colonisation();var home=own.Worlds.Where(w=>w.Stage==ColonyEstablishmentStage.Established&&w.Population>=600&&w.Food>=20&&w.Cohorts.Any(c=>c.Population>=400)).OrderByDescending(w=>w.Population).ThenBy(w=>w.Id,StringComparer.Ordinal).First();
                var yard=own.Worlds.Where(w=>w.Stage==ColonyEstablishmentStage.Established&&w.Population>0&&w.SystemId==home.SystemId&&w.Shipbuilding).OrderByDescending(w=>w.Industry).ThenBy(w=>w.Id,StringComparer.Ordinal).First();
                s.Home=home.Id;s.HomeSystem=home.SystemId;s.HomeNode=model.GeneratedMap().Regions.Single(r=>r.SystemId==home.SystemId).NodeId;s.Yard=yard.Id;
                s.Explorer=model.ColonisationActivities().Ships.Where(x=>x.StudyWork>0).OrderBy(x=>x.ShipId,StringComparer.Ordinal).First().ShipId;
                var options=model.ColonisationActivities().BuildOptions;var transport=options.Single(x=>x.Kind==ActivityProductionKind.PublishedShip&&x.Supported&&x.DisplayName=="Galaxy Colony Transport"&&x.BlueprintRevision==1);t.TransportBlueprint=transport.DefinitionId;
                var escort=options.Single(x=>x.Supported&&x.HullVersionId=="hull.defiant-combat.v1"&&x.BlueprintRevision==1);
                var hydro=options.Where(x=>x.Kind==ActivityProductionKind.Infrastructure).Join(own.BuildingBenefits,b=>b.DefinitionId,b=>b.DefinitionId,(o,b)=>new{o,b}).Where(x=>x.b.FoodOutput>0).OrderBy(x=>x.o.Cost.Industry).ThenBy(x=>x.o.DefinitionId,StringComparer.Ordinal).First().o;
                var requiredFoodInvestment=yard.LocalFoodBalance<0;var initialCredits=model.ColonisationActivities().Credits;
                yield return ColonyWorld(s,doc,yard.Id);if(yard.LocalFoodBalance<0)yield return ColonyClick(s,doc,doc.rootVisualElement.Query<Button>().ToList().Single(b=>b.text=="+ Add "+hydro.DisplayName).name);
                yield return ColonyClick(s,doc,doc.rootVisualElement.Query<Button>().ToList().Single(b=>b.text=="+ Add "+transport.DisplayName).name);
                yield return ColonyWorld(s,doc,home.Id);yield return ColonyClick(s,doc,"colonisation-build-kit");yield return ColonyClick(s,doc,doc.rootVisualElement.Query<Button>().ToList().Single(b=>b.text=="+ Add "+escort.DisplayName).name);
                Assert.That(ColonyModel(s).ColonisationActivities().Credits,Is.EqualTo(initialCredits),"no upfront normal credits for any of the real streamed orders");
                var capturedOrders=ColonyModel(s).Colonisation().Production.ToArray();var orders=capturedOrders.Select(x=>x.Id).ToArray();Assert.That(orders.Length,Is.EqualTo(yard.LocalFoodBalance<0?4:3));bool consumed=false;
                for(int week=0;week<32&&ColonyModel(s).Colonisation().Production.Any(q=>orders.Contains(q.Id));week++)
                {yield return HumanAdvance(t,doc,"real-manufacture");consumed|=ColonyModel(s).Colonisation().Production.Any(q=>orders.Contains(q.Id)&&q.CompletedIndustry>0&&q.Consumed.Any(x=>x.Amount>0));}
                Assert.That(ColonyModel(s).Colonisation().Production.Any(q=>orders.Contains(q.Id)),Is.False,"all captured real orders complete within subordinate manufacture bound");Assert.That(consumed,Is.True);
                var manufactured=ExpansionAudit(ColonyModel(s));foreach(var captured in capturedOrders)
                {
                    var completion=manufactured.BaseCampaign!.ProductionHistory!.Single(r=>r.Completed&&r.Order!.QueueItemId==captured.Id).Order!;
                    Assert.That(completion.CompletedIndustry,Is.EqualTo(captured.RequiredIndustry));Assert.That(completion.RequiredIndustry,Is.EqualTo(captured.RequiredIndustry));
                    Assert.That(completion.ConsumedResources!.Select(x=>new{x.ResourceDefinitionId,x.Amount}),Is.EqualTo(captured.Required.Select(x=>new{x.ResourceDefinitionId,x.Amount})),"actual complete material vector, no granted work");
                }
                var foodInvestmentDone=!requiredFoodInvestment||ColonyModel(s).Colonisation().Worlds.Single(w=>w.Id==yard.Id).FoodOutput==yard.FoodOutput+own.BuildingBenefits.Single(b=>b.DefinitionId==hydro.DefinitionId).FoodOutput;
                Assert.That(foodInvestmentDone,Is.True,"conditional real Hydro order installed its authored food benefit");
                s.Transport=ColonyModel(s).Colonisation().Transports.Single(x=>x.BlueprintId==transport.DefinitionId&&x.BlueprintRevision==1).ShipId;s.Escort=ColonyModel(s).Colonisation().OwnShipSources.Single(x=>x.BlueprintId==escort.DefinitionId&&x.Revision==1).ShipId;
                yield return HumanJoin(t,doc);t.Mark("real-ships-kit-and-yard-food-complete");yield return HumanSurvey(t,doc);
                // Revalidate current own physical source and complete cargo context immediately before actual loading.
                own=ColonyModel(s).Colonisation();home=own.Worlds.Single(w=>w.Id==s.Home);var carrier=own.Transports.Single(x=>x.ShipId==s.Transport);var cohort=home.Cohorts.Where(c=>c.Population>=400).OrderByDescending(c=>c.Population).ThenBy(c=>c.Id,StringComparer.Ordinal).First();
                Assert.That(home.Stage,Is.EqualTo(ColonyEstablishmentStage.Established));Assert.That(home.Population-100,Is.GreaterThanOrEqualTo(500));Assert.That(home.Food,Is.GreaterThanOrEqualTo(20));Assert.That(TailKits(home),Is.GreaterThanOrEqualTo(1));
                Assert.That(carrier.Living&&carrier.Stationary,Is.True);Assert.That(carrier.NodeId,Is.EqualTo(s.HomeNode));Assert.That(carrier.ManifestId,Is.Null);
                yield return ColonyTransport(s,doc,s.Home,s.Transport);yield return ColonyPick(doc,"colonisation-cohort",home.Cohorts,x=>x.Id,cohort.Id);doc.rootVisualElement.Q<IntegerField>("colonisation-food").value=20;
                foreach(var input in doc.rootVisualElement.Query<IntegerField>().ToList().Where(x=>x.name.StartsWith("colonisation-material-",StringComparison.Ordinal)))input.value=0;
                yield return HumanOrder(t,doc,"colonisation-embark");own=ColonyModel(s).Colonisation();carrier=own.Transports.Single(x=>x.ShipId==s.Transport);t.TransportManifest=carrier.ManifestId!;
                Assert.That(own.Worlds.Single(x=>x.Id==s.Home).Population,Is.EqualTo(home.Population-100));Assert.That(own.Worlds.Single(x=>x.Id==s.Home).Food,Is.EqualTo(home.Food-20));Assert.That(TailKits(own.Worlds.Single(x=>x.Id==s.Home)),Is.EqualTo(TailKits(home)-1));
                Assert.That(carrier.Population,Is.EqualTo(100));Assert.That(carrier.Food,Is.EqualTo(20));Assert.That(carrier.BlueprintRevision,Is.EqualTo(1));Assert.That(carrier.Cargo.Single(x=>x.ResourceDefinitionId=="resource.settlement-kit").Amount,Is.EqualTo(1));
                t.Mark("conserved-human100-loaded");yield return HumanMove(t,doc,s.Transport,s.TargetNode,true);yield return HumanCover(t,doc,"loaded-approach-cover");
                yield return ColonyTransport(s,doc,s.Home,s.Transport);yield return ColonyPick(doc,"colonisation-deploy-planet",ColonyModel(s).ColonisationPlanets().Where(p=>p.Surveyed).ToArray(),p=>p.PlanetId,s.TargetPlanet);
                yield return HumanOrder(t,doc,"colonisation-deploy");var intent=ColonyModel(s).Colonisation().Intents.Single(x=>x.ManifestId==t.TransportManifest&&x.PlanetId==s.TargetPlanet);
                yield return HumanAdvance(t,doc,"actual-landing");var outcome=ColonyModel(s).Colonisation().LatestDeployments.Single(x=>x.IntentId==intent.Id&&x.ManifestId==t.TransportManifest);
                File.WriteAllText(Path.Combine(CampaignOutput(),"human-own-landing-result.json"),JsonConvert.SerializeObject(outcome));Assert.That(outcome.Outcome,Is.EqualTo(DeploymentBoundaryOutcome.Deployed),"surprises fail honestly without state injection/recovery loops");
                var camp=ColonyModel(s).Colonisation().Worlds.Single(x=>x.PlanetId==s.TargetPlanet);s.TargetWorld=camp.Id;Assert.That(camp.Population,Is.EqualTo(100));Assert.That(camp.Stage,Is.EqualTo(ColonyEstablishmentStage.Founding));
                for(int week=0;week<8&&ColonyModel(s).Colonisation().Worlds.Single(x=>x.Id==s.TargetWorld).Stage!=ColonyEstablishmentStage.Established;week++)yield return HumanAdvance(t,doc,"fed-establishment");
                camp=ColonyModel(s).Colonisation().Worlds.Single(x=>x.Id==s.TargetWorld);Assert.That(camp.Stage,Is.EqualTo(ColonyEstablishmentStage.Established));Assert.That(camp.Population,Is.EqualTo(100));Assert.That(camp.Deaths,Is.Zero);Assert.That(camp.FedWeeks,Is.GreaterThanOrEqualTo(4));Assert.That(camp.EstablishmentWork,Is.EqualTo(16));
                carrier=ColonyModel(s).Colonisation().Transports.Single(x=>x.ShipId==s.Transport);Assert.That(carrier.Living,Is.True);Assert.That(carrier.ManifestId,Is.Null);Assert.That(carrier.BlueprintRevision,Is.EqualTo(1));t.Mark("human-first-colony-established");
                yield return HumanMove(t,doc,s.Transport,s.HomeNode);yield return ColonyTransport(s,doc,s.Home,s.Transport);home=ColonyModel(s).Colonisation().Worlds.Single(x=>x.Id==s.Home);Assert.That(home.Food,Is.GreaterThanOrEqualTo(5));
                doc.rootVisualElement.Q<IntegerField>("colonisation-food").value=5;foreach(var input in doc.rootVisualElement.Query<IntegerField>().ToList().Where(x=>x.name.StartsWith("colonisation-material-",StringComparison.Ordinal)))input.value=0;
                yield return HumanOrder(t,doc,"colonisation-load-supplies");carrier=ColonyModel(s).Colonisation().Transports.Single(x=>x.ShipId==s.Transport);t.SupplyManifest=carrier.ManifestId!;
                Assert.That(carrier.Kind,Is.EqualTo(SettlerManifestKind.SupplyOnly));Assert.That(carrier.Population,Is.Zero);Assert.That(carrier.Food,Is.EqualTo(5));Assert.That(t.SupplyManifest,Is.Not.EqualTo(t.TransportManifest));
                Assert.That(ColonyModel(s).Colonisation().Worlds.Single(x=>x.Id==s.Home).Population,Is.EqualTo(home.Population));Assert.That(ColonyModel(s).Colonisation().Worlds.Single(x=>x.Id==s.Home).Food,Is.EqualTo(home.Food-5));
                yield return HumanMove(t,doc,s.Transport,s.TargetNode);yield return ColonyTransport(s,doc,s.TargetWorld,s.Transport);doc.rootVisualElement.Q<IntegerField>("colonisation-food").value=5;yield return HumanOrder(t,doc,"colonisation-deliver");
                var delivered=ExpansionAudit(ColonyModel(s));var receipt=delivered.BaseCampaign!.Colonisation!.ManifestResolutions!.Single(x=>x.ManifestId==t.SupplyManifest);
                Assert.That(receipt.Stage,Is.EqualTo(SettlerManifestStage.Delivered));Assert.That(receipt.DestinationPlanetId,Is.EqualTo(s.TargetPlanet));Assert.That(receipt.FoodTransferred,Is.EqualTo(5));Assert.That(receipt.PopulationTransferred,Is.Zero);
                var reused=delivered.BaseCampaign.Colonisation.ManifestRecords!.Single(x=>x.ManifestId==t.SupplyManifest&&x.OwnerId==t.Actor&&x.ShipId==s.Transport);Assert.That(reused.FoodDelivered,Is.EqualTo(5));
                var source=delivered.BaseCampaign.Colonisation.ManifestOrigins!.Single(x=>x.ManifestId==t.SupplyManifest);Assert.That(source.SourceColonyId,Is.EqualTo(s.Home));Assert.That(source.Kind,Is.EqualTo(ManifestOriginKind.Supply));
                carrier=ColonyModel(s).Colonisation().Transports.Single(x=>x.ShipId==s.Transport);Assert.That(carrier.ManifestId,Is.Null);Assert.That(carrier.BlueprintRevision,Is.EqualTo(1));Assert.That(carrier.BlueprintId,Is.EqualTo(t.TransportBlueprint));t.Mark("same-r1-carrier-supply-delivered");
                var expected=ExpansionBytes(ColonyModel(s));CampaignPage(doc,ReviewWorkspacePage.Saves);yield return null;Click(doc.rootVisualElement.Q<Button>("save-slot-generated-1"));t.Mark("final-save-begin");
                ColonyActivate(s,()=>{Click(doc.rootVisualElement.Q<Button>("player-save"));if(doc.rootVisualElement.Q("review-one-use-confirmation")!=null)Confirm(doc);});yield return ColonyIdle(s,"human-whole12-save");
                Assert.That(File.ReadAllBytes(Path.Combine(storePath,"unity-campaign-colonisation-expansion-v1-generated-1.json")),Is.EqualTo(expected));File.WriteAllBytes(Path.Combine(CampaignOutput(),"human-established-saved-private-audit.json"),expected);
                var prior=ColonyModel(s);Click(doc.rootVisualElement.Q<Button>("demo-menu"));yield return null;yield return null;var load=doc.rootVisualElement.Q<Button>("colonisation-expansion-campaign-continue-generated-1");yield return ColonyReach(doc,load,target:"explicit Expansion Continue");
                t.Mark("final-continue-begin");ColonyActivate(s,()=>{Click(load);Confirm(doc);});yield return ColonyIdle(s,"human-whole12-continue");
                Assert.That(ColonyModel(s),Is.Not.SameAs(prior));Assert.That(ColonyModel(s).Current,Is.Not.SameAs(t.Controller));Assert.That(ColonyModel(s).Current.Frame.Actor,Is.EqualTo(t.Actor));Assert.That(ColonyModel(s).ColonisationExpansionCampaign,Is.True);Assert.That(ExpansionBytes(ColonyModel(s)),Is.EqualTo(expected));
                yield return ColonyWorld(s,doc,s.TargetWorld);
                foreach(var size in new[]{new Vector2Int(1280,720),new Vector2Int(1920,1080),new Vector2Int(3440,1440)}){yield return HumanCapture(t,doc,size,HumanImages[size.x==1280?0:size.x==1920?1:2],hydro.DefinitionId);t.Mark("capture-"+size.x);}
                Assert.That(ExpansionBytes(ColonyModel(s)),Is.EqualTo(expected));Assert.That(Directory.GetFiles(CampaignOutput(),"human-0*-established-*.png").Select(Path.GetFileName),Is.EquivalentTo(HumanImages));
                File.WriteAllText(Path.Combine(CampaignOutput(),"human-summary.json"),JsonConvert.SerializeObject(new{explicitFreshExpansion=true,systems=250,seed="42",realHydroWhenNeeded=foodInvestmentDone,requiredFoodInvestment,realTransportKitEscort=true,conserved100=true,fedEstablishment=true,sameR1SupplyReuse=true,wholeTyped12Continue=true,threeLayouts=true,
                    t.UiAdvances,maximumUiAdvances=64,t.LoadedAdvances,maximumLoadedAdvances=12,t.SurveyBodies,maximumBodies=12,t.FrontierMoves,maximumFrontierMoves=20,manufactureMaximum=32,loadedVoyageMaximum=12,manifest=t.TransportManifest,supplyManifest=t.SupplyManifest,captures=HumanImages,
                    qualification="One continuous fresh human UI journey. Audit roots only prove whole bytes/own receipts, never choose actions. No original full15 reclassification, no native/ETA/battle-win claim."}));t.Mark("all-human-gates-complete");
            }
        }
    }
}
