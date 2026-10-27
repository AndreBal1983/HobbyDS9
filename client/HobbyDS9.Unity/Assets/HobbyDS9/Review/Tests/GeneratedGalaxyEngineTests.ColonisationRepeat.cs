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
        private static readonly string[] RepeatImages={
            "repeat-01-continued-1280.png","repeat-02-continued-1920.png","repeat-03-continued-3440.png"};
        private sealed class RepeatReceivingState
        {
            internal readonly ColonyCase Case=new ColonyCase(); // Reuse tracking/helpers only; no old-profile persistence helper is called.
            internal readonly System.Diagnostics.Stopwatch Productive=System.Diagnostics.Stopwatch.StartNew();
            internal bool SawQueued,SawActions,SawLoaded,SawFounding,SawEstablished,SawFirstClosed;
            internal int Flushed,UiAdvances;
            internal string Phase="setup";
            internal void Flush()
            {
                // Inspecting the count is cheap; disk writes happen ONLY for new timing rows.
                while(Flushed<Case.Shared.Timings.Count)
                {
                    File.AppendAllText(Path.Combine(CampaignOutput(),"repeat-timings.jsonl"),
                        JsonConvert.SerializeObject(Case.Shared.Timings[Flushed])+Environment.NewLine);
                    Flushed++;
                }
            }
            internal void Mark(string phase)
            {
                Phase=phase;
                File.AppendAllText(Path.Combine(CampaignOutput(),"repeat-phases.jsonl"),
                    JsonConvert.SerializeObject(new{phase,productiveMs=Productive.Elapsed.TotalMilliseconds})+Environment.NewLine);
                Flush();
            }
        }

        // One explicit-menu repeat case; no original receiving test is changed or relabelled.
        //720 is ONLY600 productive +90 drain + scene-exit margin, never extra gameplay time.
        [UnityTest,Timeout(720000)]
        public IEnumerator ColonisationRepeatUsesExplicitMenuAndWholeTypedContinue()
        {
            var t=new RepeatReceivingState();var s=t.Case;var stack=new Stack<IEnumerator>();
            stack.Push(RepeatReceivingBody(t));Exception? failure=null;
            while(stack.Count>0)
            {
                bool next=false;object? value=null;var current=stack.Peek();
                try
                {
                    if(t.Productive.Elapsed.TotalSeconds>=600)throw new TimeoutException("Repeat productive deadline before "+t.Phase);
                    next=current.MoveNext();if(next)value=current.Current;
                    if(t.Productive.Elapsed.TotalSeconds>=600)throw new TimeoutException("Repeat productive deadline after "+t.Phase);
                    if(t.Flushed<s.Shared.Timings.Count)t.Flush();
                }
                catch(Exception error)
                {
                    failure=error; // Diagnostics must never replace the original failure or prevent cleanup.
                    try{RepeatFailureGeometry(t,current.GetType().FullName??"iterator");}
                    catch(Exception diagnosticError){try{CampaignDiagnosticError("repeat-failure-geometry",diagnosticError);}catch{}}
                }
                if(failure!=null)break;
                if(!next){stack.Pop();try{(current as IDisposable)?.Dispose();}catch(Exception error){failure=error;break;}continue;}
                if(value is IEnumerator nested){stack.Push(nested);continue;}
                yield return value;
            }
            t.Productive.Stop();
            void Cleanup(string phase,Action action)
            {try{action();}catch(Exception error){failure=failure??error;try{CampaignDiagnosticError(phase,error);}catch{/* Never skip release/drain/exit for a receipt failure. */}}}
            Cleanup("repeat-release-all",()=>{var error=s.ReleaseAll();failure=failure??error;});
            Cleanup("repeat-shared-release",()=>s.Shared.Release.Set());
            if(s.Shared.Entry!=null)Cleanup("repeat-disable",()=>s.Shared.Entry.enabled=false);
            while(stack.Count>0)try{(stack.Pop() as IDisposable)?.Dispose();}catch(Exception error){failure=failure??error;}
            bool settled=true;
            bool Pending()
            {
                if(s.Pending||s.Shared.Holder?.IsCompleted==false)return true;
                bool pending=true;Cleanup("repeat-drain-status",()=>pending=s.Shared.Entry!=null&&ColonyPending(s.Shared.Entry));return pending;
            }
            var cleanupClock=System.Diagnostics.Stopwatch.StartNew();
            while(Pending())
            {
                if(cleanupClock.Elapsed.TotalSeconds>=90)
                {
                    settled=false;directory=null;
                    failure=failure??new TimeoutException("Repeat worker unsettled; temporary namespace/override retained until scene exit.");break;
                }
                yield return null;
            }
            Cleanup("repeat-final-timings",t.Flush);
            bool overrideRestored=false;
            if(settled&&s.Shared.Entry!=null&&s.PriorDirectoryCaptured)
                Cleanup("repeat-restore-override",()=>{typeof(ReviewEntry).GetField("generatedCampaignTestDirectory",HullPrivate)!.SetValue(s.Shared.Entry,s.Shared.PriorDirectory);overrideRestored=true;});
            Cleanup("repeat-cleanup-receipt",()=>File.WriteAllText(Path.Combine(CampaignOutput(),"repeat-cleanup.json"),JsonConvert.SerializeObject(new{
                settled,allReleasesSet=s.Holds.All(h=>h.Release.IsSet),priorOverrideCaptured=s.PriorDirectoryCaptured,
                overrideRestored,directoryRetained=!settled,entryDisabled=s.Shared.Entry==null||!s.Shared.Entry.enabled,
                cleanupMs=cleanupClock.Elapsed.TotalMilliseconds,productiveMs=t.Productive.Elapsed.TotalMilliseconds,
                productiveLimitSeconds=600,drainLimitSeconds=90,outerFrameworkSeconds=720,failed=failure!=null,lastPhase=t.Phase})));
            if(settled)foreach(var host in s.Hosts)Cleanup("repeat-remove-probe",()=>ColonyProbe(host,null));
            if(!s.Pending)foreach(var h in s.Holds){Cleanup("repeat-dispose-entered",()=>h.Entered.Dispose());Cleanup("repeat-dispose-release",()=>h.Release.Dispose());}
            if(s.Shared.Holder==null||s.Shared.Holder.IsCompleted)
            {Cleanup("repeat-dispose-shared-entered",()=>s.Shared.Entered.Dispose());Cleanup("repeat-dispose-shared-release",()=>s.Shared.Release.Dispose());}
            yield return new ExitPlayMode();
            if(failure!=null)System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
        }

        private static void RepeatFailureGeometry(RepeatReceivingState t,string iterator)
        {
            var entry=t.Case.Shared.Entry;if(entry==null)return;var doc=entry.GetComponent<UIDocument>();var root=doc.rootVisualElement;
            object Box(VisualElement? item)=>item==null?(object)new{present=false}:new{
                present=true,item.name,type=item.GetType().Name,classes=item.GetClasses().ToArray(),layout=item.layout.ToString(),world=item.worldBound.ToString(),
                height=item.resolvedStyle.height,minHeight=item.resolvedStyle.minHeight.ToString(),maxHeight=item.resolvedStyle.maxHeight.ToString(),
                flexGrow=item.resolvedStyle.flexGrow,flexShrink=item.resolvedStyle.flexShrink,flexBasis=item.resolvedStyle.flexBasis.ToString(),overflow=item.style.overflow.value.ToString()};
            object Scroll(ScrollView item)=>new{viewport=Box(item.contentViewport),content=Box(item.contentContainer),offset=item.scrollOffset.ToString(),
                vertical=new{low=item.verticalScroller.lowValue,high=item.verticalScroller.highValue,value=item.verticalScroller.value},
                horizontal=new{low=item.horizontalScroller.lowValue,high=item.horizontalScroller.highValue,value=item.horizontalScroller.value}};
            object OwnControl(string id)
            {
                var item=root.Q(id);var ancestors=new List<object>();
                for(var at=item?.parent;at!=null;at=at.parent)ancestors.Add(new{box=Box(at),scroll=at is ScrollView view?Scroll(view):null});
                return new{id,control=Box(item),ancestors};
            }
            File.WriteAllText(Path.Combine(CampaignOutput(),"repeat-failure-geometry.json"),JsonConvert.SerializeObject(new{
                phase=t.Phase,iterator,productiveMs=t.Productive.Elapsed.TotalMilliseconds,root=Box(root),footer=Box(root.Q("player-footer")),
                content=Box(root.Q("review-central-content")),body=Box(root.Query<VisualElement>().ToList().FirstOrDefault(e=>e.ClassListContains("console-body"))),panel=Box(root.Q<ReviewColonisationPanel>()),
                controls=new[]{"colonisation-tab-Worlds","colonisation-tab-Planets","colonisation-tab-Transports","colonisation-tab-Science","colonisation-world-selector","colonisation-build-kit"}.Select(OwnControl).ToArray(),
                qualification="Read-only own UI geometry before disable/disposal. No target is inferred from the flattened iterator or private AI audit."}));
            var prior=RenderTexture.active;Texture2D? pixels=null;
            try
            {
                var target=doc.panelSettings.targetTexture;if(target==null)throw new InvalidOperationException("Failure render target unavailable.");
                RenderTexture.active=target;pixels=new Texture2D(target.width,target.height,TextureFormat.RGBA32,false);
                pixels.ReadPixels(new Rect(0,0,target.width,target.height),0,0);pixels.Apply();
                File.WriteAllBytes(Path.Combine(CampaignOutput(),"repeat-failure-view.png"),pixels.EncodeToPNG());
            }
            finally{RenderTexture.active=prior;if(pixels!=null)UnityEngine.Object.DestroyImmediate(pixels);}
        }

        // Audit IDs never select an action or human destination.
        private static CampaignColonisationRepeatSaveDto RepeatAudit(ReviewUiModel model)
            =>((HobbyDS9.Application.ICampaignColonisationRepeatSession)typeof(PhysicalReviewHost).GetField("session",HullPrivate)!.GetValue(StreamHost(model))!).ExportRepeatSave();
        private static byte[] RepeatBytes(ReviewUiModel model)=>new ReviewNewtonsoftCodec().EncodeRepeatRoot(RepeatAudit(model));
        private static void RepeatCensus(RepeatReceivingState t,CampaignColonisationRepeatSaveDto root)
        {
            var first=AssertSingleRepeat(root.AiPlans!);var goal=AssertSingleRepeat(root.RepeatGoals!);var second=goal.Assignment!;var state=root.BaseCampaign!;
            var manifest=state.Colonisation!.ManifestRecords!.SingleOrDefault(m=>m.ManifestId==goal.SettlementManifestId);
            var queue=state.Production!.Where(q=>q.OwnerId==goal.ActorId&&(q.QueueItemId==second.KitQueueId||q.QueueItemId==second.TransportQueueId)).ToArray();
            var camp=state.Colonisation.CampSources!.SingleOrDefault(c=>c.CampId==second.CampId);
            t.SawFirstClosed|=first.Stage==ColonisationAiStage.GrowthClosed;
            t.SawQueued|=queue.Length>0;t.SawActions|=first.LatestActions!.Any(a=>a.Accepted);
            t.SawLoaded|=manifest?.Stage==SettlerManifestStage.Loaded&&manifest.Passengers!.Population==100;
            t.SawFounding|=camp?.Stage==ColonyEstablishmentStage.Founding;
            t.SawEstablished|=goal.State==ColonisationRepeatState.Established&&camp?.Stage==ColonyEstablishmentStage.Established;
            Assert.That(second.LatestActions,Is.Empty);Assert.That(first.LatestActions!.Count,Is.LessThanOrEqualTo(8));Assert.That(first.LatestActions!.Count(a=>a.Accepted),Is.LessThanOrEqualTo(4));
            File.AppendAllText(Path.Combine(CampaignOutput(),"repeat-private-week-audit.jsonl"),JsonConvert.SerializeObject(new{
                turn=ColonyGame(state).TurnNumber,first.Stage,goal.State,secondStage=second.Stage,goal.SettlementManifestId,goal.SupplyCampId,
                first.CampId,secondCamp=second.CampId,first.EstablishedColonyId,secondColony=second.EstablishedColonyId,
                queue=queue.Select(q=>new{q.QueueItemId,q.OwnerId,q.Kind,q.CompletedIndustry,q.RequiredIndustry}),manifest,
                qualification="Private audit only. No IDs/facts from this row select human controls, targets or routes; concurrent queued kit is not assumed to supply an already-loaded manifest."})+Environment.NewLine);
        }
        private static T AssertSingleRepeat<T>(IReadOnlyList<T> rows)
        {Assert.That(rows.Count,Is.EqualTo(1),"actual two-empire case has exactly one AI actor");return rows[0];}
        private static IEnumerator RepeatOperation(RepeatReceivingState t,UIDocument doc,string phase,string busyTitle,Action activate)
        {
            var s=t.Case;var clock=System.Diagnostics.Stopwatch.StartNew();var busyClock=new System.Diagnostics.Stopwatch();int busyFrames=0,ticks=0;
            bool sawBusy=false;var positions=new HashSet<float>();var pulse=doc.rootVisualElement.schedule.Execute(()=>ticks++).Every(10);
            try
            {
                ColonyActivate(s,activate);
                while(ColonyPending(s.Shared.Entry!))
                {
                    if(!busyClock.IsRunning)busyClock.Start();sawBusy=true;busyFrames++;
                    yield return null;
                    // Worker completion may remove the overlay between frames; inspect only its real lifetime.
                    if(ColonyPending(s.Shared.Entry!))
                    {
                        CheckpointBusy(doc,busyTitle);
                        var indicator=doc.rootVisualElement.Q("campaign-operation-indicator");Assert.That(indicator,Is.Not.Null);
                        positions.Add(indicator.style.left.value.value);
                    }
                }
                busyClock.Stop();yield return null;yield return null;
                Assert.That(sawBusy,Is.True,"normal asynchronous operation presented busy ownership");
                if(busyClock.Elapsed.TotalMilliseconds>=250){Assert.That(ticks,Is.GreaterThanOrEqualTo(3));Assert.That(positions.Count,Is.GreaterThan(1),"indeterminate indicator changed while real work was busy");}
                s.Shared.Timings.Add(new{phase,observedWallMs=clock.Elapsed.TotalMilliseconds,busyMs=busyClock.Elapsed.TotalMilliseconds,busyFrames,uiTicks=ticks,indicatorPositions=positions.Count,notFps=true,noLatencyThreshold=true});
                File.AppendAllText(Path.Combine(CampaignOutput(),"repeat-operation-observations.jsonl"),JsonConvert.SerializeObject(new{phase,wallMs=clock.Elapsed.TotalMilliseconds,busyMs=busyClock.Elapsed.TotalMilliseconds,busyFrames,uiTicks=ticks,indicatorPositions=positions.Count,sawBusy,noLatencyThreshold=true})+Environment.NewLine);
            }
            finally{pulse.Pause();}
        }
        private static IEnumerator RepeatFooterReach(UIDocument doc,Button button)
        {
            var root=doc.rootVisualElement;var footer=root.Q("player-footer");
            Assert.That(button,Is.Not.Null);Assert.That(footer,Is.Not.Null);Assert.That(root.panel,Is.Not.Null);
            Assert.That(root.Q<Button>("player-primary-action"),Is.SameAs(button),"current footer action");
            Assert.That(button.panel,Is.SameAs(root.panel));Assert.That(footer.panel,Is.SameAs(root.panel));
            Assert.That(button.enabledInHierarchy,Is.True);Assert.That(footer.Contains(button),Is.True,"End week belongs to the current footer");
            var hit=root.panel.Pick(button.worldBound.center);
            Assert.That(hit==button||hit!=null&&button.Contains(hit),Is.True,"actual footer action pick");
            void Contained(Rect bounds,string owner)
            {
                Assert.That(button.worldBound.xMin,Is.GreaterThanOrEqualTo(bounds.xMin),owner+" left");
                Assert.That(button.worldBound.yMin,Is.GreaterThanOrEqualTo(bounds.yMin),owner+" top");
                Assert.That(button.worldBound.xMax,Is.LessThanOrEqualTo(bounds.xMax),owner+" right");
                Assert.That(button.worldBound.yMax,Is.LessThanOrEqualTo(bounds.yMax),owner+" bottom");
            }
            Contained(footer.worldBound,"footer");Contained(root.worldBound,"root");
            Assert.That(button.worldBound.height,Is.GreaterThanOrEqualTo(36));Assert.That(button.resolvedStyle.fontSize,Is.GreaterThanOrEqualTo(16));
            yield break; // The footer is not scrolled; no additional frame or wait is introduced.
        }
        private static IEnumerator RepeatAdvance(RepeatReceivingState t,UIDocument doc)
        {
            var s=t.Case;Assert.That(++t.UiAdvances,Is.LessThanOrEqualTo(64),"global ordinary UI Advance cap including the fresh post-Continue week");
            var model=ColonyModel(s);Assert.That(model.Current.Frame.Status.Phase,Is.EqualTo(SessionPhase.Planning),"unsupported tactical/event phase is a finding, never forged or switched");
            Assert.That(doc.rootVisualElement.Q("review-one-use-confirmation"),Is.Null,"no unattended modal before End week");
            CampaignPage(doc,ReviewWorkspacePage.Map);yield return V3Settled(doc);var turn=model.Current.Frame.Status.Turn;
            var button=doc.rootVisualElement.Q<Button>("player-primary-action");yield return RepeatFooterReach(doc,button);
            ColonyActivate(s,()=>{Click(button);Confirm(doc);});yield return ColonyIdle(s,"repeat-ui-week");
            Assert.That(ColonyModel(s).Current.Frame.Status.Phase,Is.EqualTo(SessionPhase.Planning),"unexpected tactical/modal flow stops the bounded receiving case");
            Assert.That(doc.rootVisualElement.Q("review-one-use-confirmation"),Is.Null);Assert.That(ColonyModel(s).Current.Frame.Status.Turn,Is.EqualTo(turn+1));
        }
        private static IEnumerator RepeatCapture(RepeatReceivingState t,UIDocument doc,Vector2Int size,string filename)
        {
            var s=t.Case;var selection=ColonySelection(doc.rootVisualElement.Q<ReviewColonisationPanel>());
            var oldTarget=doc.panelSettings.targetTexture;var oldWidth=doc.rootVisualElement.style.width;var oldHeight=doc.rootVisualElement.style.height;
            var active=RenderTexture.active;var target=new RenderTexture(size.x,size.y,24,RenderTextureFormat.ARGB32);Texture2D? pixels=null;
            try
            {
                Assert.That(target.Create(),Is.True);doc.panelSettings.targetTexture=target;doc.rootVisualElement.style.width=size.x;doc.rootVisualElement.style.height=size.y;
                yield return null;yield return null;yield return null;
                var panel=doc.rootVisualElement.Q<ReviewColonisationPanel>();var scroll=panel?.Q<ScrollView>("colonisation-scroll");
                var facts=panel?.Q("colonisation-world-facts");if(scroll!=null&&facts!=null){scroll.ScrollTo(facts);yield return null;yield return null;}
                RenderTexture.active=target;pixels=new Texture2D(size.x,size.y,TextureFormat.RGBA32,false);pixels.ReadPixels(new Rect(0,0,size.x,size.y),0,0);pixels.Apply();
                File.WriteAllBytes(Path.Combine(CampaignOutput(),filename),pixels.EncodeToPNG()); // Keep the actual intended image even on later geometry failure.
                File.WriteAllText(Path.Combine(CampaignOutput(),filename.Replace(".png",".layout.json")),JsonConvert.SerializeObject(new{
                    root=doc.rootVisualElement.worldBound.ToString(),panel=panel?.worldBound.ToString(),viewport=scroll?.contentViewport.worldBound.ToString(),footer=doc.rootVisualElement.Q("player-footer")?.worldBound.ToString(),selection=ColonySelection(panel)}));
                Assert.That(panel,Is.Not.Null);Assert.That(doc.rootVisualElement.Query<ReviewColonisationPanel>().ToList().Count,Is.EqualTo(1));
                Assert.That(scroll!.contentViewport.worldBound.height,Is.GreaterThanOrEqualTo(100));
                Assert.That(panel!.worldBound.yMax,Is.LessThanOrEqualTo(doc.rootVisualElement.Q("player-footer").worldBound.yMin));
                foreach(var id in new[]{"Worlds","Planets","Transports","Science"})
                {var button=panel.Q<Button>("colonisation-tab-"+id);Assert.That(button,Is.Not.Null);Assert.That(button.worldBound.height,Is.GreaterThanOrEqualTo(44));Assert.That(button.resolvedStyle.fontSize,Is.GreaterThanOrEqualTo(16));}
                foreach(var id in new[]{"colonisation-section-PublishedShip","colonisation-section-Infrastructure","colonisation-section-projects","colonisation-section-system"})
                {var section=panel.Q(id);Assert.That(section,Is.Not.Null);panel.Q<ScrollView>("colonisation-catalogue").ScrollTo(section);scroll.ScrollTo(section);yield return null;yield return null;PublishedReceivingContained(panel.Q<ScrollView>("colonisation-catalogue").contentViewport.worldBound,section,id);}
                Assert.That(panel.Q<ScrollView>("colonisation-queue").contentViewport.worldBound.height,Is.GreaterThanOrEqualTo(100));
            }
            finally
            {doc.panelSettings.targetTexture=oldTarget;doc.rootVisualElement.style.width=oldWidth;doc.rootVisualElement.style.height=oldHeight;RenderTexture.active=active;if(pixels!=null)UnityEngine.Object.DestroyImmediate(pixels);target.Release();UnityEngine.Object.DestroyImmediate(target);}
            yield return null;yield return null;
            Assert.That(ColonySelection(doc.rootVisualElement.Q<ReviewColonisationPanel>()),Is.EqualTo(selection),"same controller retains own selector state at all three sizes");
        }
        private IEnumerator RepeatReceivingBody(RepeatReceivingState t)
        {
            var s=t.Case;yield return new EnterPlayMode();yield return null;yield return null;
            s.Shared.Entry=UnityEngine.Object.FindAnyObjectByType<ReviewEntry>();Assert.That(s.Shared.Entry,Is.Not.Null);var entry=s.Shared.Entry!;
            var field=typeof(ReviewEntry).GetField("generatedCampaignTestDirectory",HullPrivate)!;
            s.Shared.PriorDirectory=(string?)field.GetValue(entry);s.PriorDirectoryCaptured=true;
            directory=Path.Combine(Path.GetTempPath(),"HobbyDS9-Galaxy-Engine-Repeat-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(directory);
            var storePath=Path.Combine(directory,"campaign");field.SetValue(entry,storePath);var doc=entry.GetComponent<UIDocument>();
            using(var surface=new GalaxyInputSurface(doc))
            {
                t.Mark("explicit-new-begin");doc.rootVisualElement.Q<TextField>("generated-campaign-seed").value="42";
                doc.rootVisualElement.Q<DropdownField>("generated-campaign-size").value="250";doc.rootVisualElement.Q<DropdownField>("generated-campaign-quiet").value="2.00\u00d7";
                var menu=doc.rootVisualElement.Q<Foldout>("colonisation-repeat-campaign-menu");Assert.That(menu,Is.Not.Null);Assert.That(menu.text,Is.EqualTo("Colonisation & Repeat Expansion"));Assert.That(menu.value,Is.True);
                var oldModel=ColonyModel(s);var start=doc.rootVisualElement.Q<Button>("colonisation-repeat-campaign-start");yield return ColonyReach(doc,start,target:"explicit New Repeat campaign");
                yield return RepeatOperation(t,doc,"repeat-new250","Generating",()=>{Click(start);if(doc.rootVisualElement.Q("review-one-use-confirmation")!=null)Confirm(doc);});
                var model=ColonyModel(s);Assert.That(model,Is.Not.SameAs(oldModel));Assert.That(model.ColonisationRepeatCampaign,Is.True);Assert.That(model.ColonisationGrowthCampaign,Is.False);Assert.That(model.SupportsKitFreePassengers,Is.True);
                var controller=model.Current;var actor=controller.Frame.Actor;var host=StreamHost(model);var initial=RepeatAudit(model);
                Assert.That(initial.FormatVersion,Is.EqualTo(13));Assert.That(initial.ProfileId,Is.EqualTo(CampaignColonisationRepeatIdentity.Profile));Assert.That(initial.PackageHash.Length,Is.EqualTo(64));
                Assert.That(initial.BaseCampaign!.FrozenPlanets!.PolicyId,Is.EqualTo(CampaignColonisationExpansionIdentity.InventoryPolicy));
                Assert.That(initial.BaseCampaign.Map!.Settings!.Map!.Seed,Is.EqualTo("42"));Assert.That(initial.BaseCampaign.Map.Settings.Map.SystemCount,Is.EqualTo(250));
                Assert.That(initial.AiPlans!.All(p=>p.ActorId!=actor),Is.True);Assert.That(initial.RepeatGoals!.All(g=>g.ActorId!=actor),Is.True);
                RepeatCensus(t,initial);t.Mark("explicit-new-complete");
                // Only current human own views choose this page. No AI IDs or positions select a command.
                var own=model.Colonisation();var home=own.Worlds.Where(w=>w.Stage==ColonyEstablishmentStage.Established&&w.Population>0).OrderByDescending(w=>w.Industry).ThenBy(w=>w.Id,StringComparer.Ordinal).First();
                s.Home=home.Id;t.Mark("own-world-navigation-begin");yield return ColonyWorld(s,doc,s.Home);t.Mark("own-world-navigation-complete");
                t.Mark("own-kit-reach-begin");yield return ColonyReach(doc,doc.rootVisualElement.Q<Button>("colonisation-build-kit"),target:"human own production remains usable");t.Mark("own-kit-reach-complete");
                CampaignColonisationRepeatSaveDto audit=initial;
                while(!t.SawEstablished&&t.UiAdvances<64)
                {
                    yield return RepeatAdvance(t,doc);Assert.That(ColonyModel(s).Current,Is.SameAs(controller));Assert.That(ColonyModel(s).Current.Frame.Actor,Is.EqualTo(actor));
                    audit=RepeatAudit(ColonyModel(s));RepeatCensus(t,audit);t.Mark("completed-ui-week-"+ColonyModel(s).Current.Frame.Status.Turn);
                }
                Assert.That(t.SawFirstClosed&&t.SawQueued&&t.SawActions&&t.SawLoaded&&t.SawFounding&&t.SawEstablished,Is.True,"normal real first Growth closure and distinct second colony within64 UI weeks/600 productive seconds");
                var first=AssertSingleRepeat(audit.AiPlans!);var goal=AssertSingleRepeat(audit.RepeatGoals!);var second=goal.Assignment!;
                Assert.That(first.Stage,Is.EqualTo(ColonisationAiStage.GrowthClosed));Assert.That(goal.State,Is.EqualTo(ColonisationRepeatState.Established));
                Assert.That(second.EstablishedColonyId,Is.Not.Null.And.Not.EqualTo(first.EstablishedColonyId));
                var camp=audit.BaseCampaign!.Colonisation!.CampSources!.Single(c=>c.CampId==second.CampId);Assert.That(camp.ManifestId,Is.EqualTo(goal.SettlementManifestId));
                Assert.That(camp.FedWeeks,Is.GreaterThanOrEqualTo(4));Assert.That(camp.CompletedIndustry,Is.EqualTo(16));Assert.That(camp.Deaths,Is.Zero);
                var manifest=audit.BaseCampaign.Colonisation.ManifestRecords!.Single(m=>m.ManifestId==goal.SettlementManifestId);
                Assert.That(manifest.Kind,Is.EqualTo(SettlerManifestKind.Settlement));Assert.That(manifest.PopulationTransferred,Is.EqualTo(100));Assert.That(manifest.EquipmentConsumed,Is.EqualTo(1));
                Assert.That(audit.BaseCampaign.Colonisation.ManifestRecords.Count(m=>m.OwnerId==goal.ActorId&&m.Kind==SettlerManifestKind.Settlement),Is.EqualTo(2));
                Assert.That(ColonyModel(s).Colonisation().Worlds.Any(w=>w.Id==second.EstablishedColonyId),Is.False,"private AI target is never selected/revealed for capture");
                var expected=new ReviewNewtonsoftCodec().EncodeRepeatRoot(audit);var mission=JsonConvert.SerializeObject(ColonyModel(s).FrontierCommission());
                var saved=Path.Combine(storePath,"unity-campaign-colonisation-repeat-v1-generated-1.json");Assert.That(File.Exists(saved),Is.False,"unused temporary slot only");
                File.WriteAllBytes(Path.Combine(CampaignOutput(),"repeat-established-pre-save-private-audit.json"),expected);
                t.Mark("save-begin");CampaignPage(doc,ReviewWorkspacePage.Saves);yield return null;Click(doc.rootVisualElement.Q<Button>("save-slot-generated-1"));
                yield return RepeatOperation(t,doc,"repeat-whole13-save","Saving",()=>Click(doc.rootVisualElement.Q<Button>("player-save")));
                Assert.That(File.ReadAllBytes(saved),Is.EqualTo(expected));Assert.That(RepeatBytes(ColonyModel(s)),Is.EqualTo(expected));t.Mark("save-complete");
                var prior=ColonyModel(s);Click(doc.rootVisualElement.Q<Button>("demo-menu"));yield return null;yield return null;
                var load=doc.rootVisualElement.Q<Button>("colonisation-repeat-campaign-continue-generated-1");yield return ColonyReach(doc,load,target:"explicit Repeat Continue1");t.Mark("continue-begin");
                yield return RepeatOperation(t,doc,"repeat-whole13-continue","Loading",()=>{Click(load);Confirm(doc);});
                model=ColonyModel(s);Assert.That(model,Is.Not.SameAs(prior));Assert.That(model.Current,Is.Not.SameAs(controller));Assert.That(StreamHost(model),Is.Not.SameAs(host));Assert.That(model.Current.Frame.Actor,Is.EqualTo(actor));
                Assert.That(model.ColonisationRepeatCampaign,Is.True);Assert.That(model.ColonisationGrowthCampaign,Is.False);
                var continued=RepeatAudit(model);Assert.That(RepeatBytes(model),Is.EqualTo(expected),"whole committed13 equality BEFORE any post-Continue week");
                Assert.That(JsonConvert.SerializeObject(continued.AiPlans),Is.EqualTo(JsonConvert.SerializeObject(audit.AiPlans)));Assert.That(JsonConvert.SerializeObject(continued.RepeatGoals),Is.EqualTo(JsonConvert.SerializeObject(audit.RepeatGoals)));
                Assert.That(continued.RepeatGoals![0].SupplyCampId,Is.EqualTo(goal.SupplyCampId));Assert.That(JsonConvert.SerializeObject(model.FrontierCommission()),Is.EqualTo(mission));
                File.WriteAllBytes(Path.Combine(CampaignOutput(),"repeat-continued-before-fresh-week-private-audit.json"),RepeatBytes(model));t.Mark("continue-complete-exact-before-fresh-week");
                yield return RepeatAdvance(t,doc);RepeatCensus(t,RepeatAudit(ColonyModel(s)));t.Mark("fresh-post-continue-week-complete");
                yield return ColonyWorld(s,doc,s.Home);var unchanged=RepeatBytes(ColonyModel(s));
                foreach(var size in new[]{new Vector2Int(1280,720),new Vector2Int(1920,1080),new Vector2Int(3440,1440)})
                {yield return RepeatCapture(t,doc,size,RepeatImages[size.x==1280?0:size.x==1920?1:2]);t.Mark("capture-"+size.x);}
                Assert.That(RepeatBytes(ColonyModel(s)),Is.EqualTo(unchanged),"own-view resize/capture is read-only");
                Assert.That(Directory.GetFiles(CampaignOutput(),"repeat-0*-continued-*.png").Select(Path.GetFileName),Is.EquivalentTo(RepeatImages));
                File.WriteAllBytes(Path.Combine(CampaignOutput(),"repeat-final-private-audit.json"),unchanged);
                File.WriteAllText(Path.Combine(CampaignOutput(),"repeat-summary.json"),JsonConvert.SerializeObject(new{explicitMenu=true,systems=250,seed="42",quiet="2.00x",uiAdvances=t.UiAdvances,maximumUiAdvances=64,finalTurn=ColonyModel(s).Current.Frame.Status.Turn,
                    t.SawFirstClosed,t.SawQueued,t.SawActions,t.SawLoaded,t.SawFounding,t.SawEstablished,whole13SaveContinueEqualBeforeFreshWeek=true,newController=true,newHost=true,sameHumanActor=true,bothGoalsAndSupplyCampPreserved=true,freshPostContinueWeek=true,captures=RepeatImages,
                    qualification="Actual AI records are private audit ONLY, never human action selection. Pictures show own human views. No fresh human voyage/native/performance or original15-image acceptance claim."}));
                t.Mark("all-repeat-gates-complete");
            }
        }
    }
}
