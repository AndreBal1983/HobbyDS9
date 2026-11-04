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
        private static readonly string[] CycleImages={
            "cycle-01-continued-1280.png","cycle-02-continued-1920.png","cycle-03-continued-3440.png"};
        private sealed class CycleReceivingState
        {
            internal readonly ColonyCase Case=new ColonyCase(); // Reuse tracking/helpers only; no old-profile persistence helper is called.
            internal readonly System.Diagnostics.Stopwatch Productive=System.Diagnostics.Stopwatch.StartNew();
            internal bool SawQueued,SawActions,SawLoaded,SawFounding,SawEstablished,SawFirstClosed,SawTwoEstablished,SawThirdCommitted;
            internal int Flushed,UiAdvances;
            internal string Phase="setup";
            internal bool OverrideAssigned;
            internal readonly List<ReviewUiModel> Models=new List<ReviewUiModel>();
            internal readonly List<CycleActivationContext> Activations=new List<CycleActivationContext>();
            internal void Flush()
            {
                // Inspecting the count is cheap; disk writes happen ONLY for new timing rows.
                while(Flushed<Case.Shared.Timings.Count)
                {
                    File.AppendAllText(Path.Combine(CampaignOutput(),"cycle-timings.jsonl"),
                        JsonConvert.SerializeObject(Case.Shared.Timings[Flushed])+Environment.NewLine);
                    Flushed++;
                }
            }
            internal void Mark(string phase)
            {
                Phase=phase;
                File.AppendAllText(Path.Combine(CampaignOutput(),"cycle-phases.jsonl"),
                    JsonConvert.SerializeObject(new{phase,productiveMs=Productive.Elapsed.TotalMilliseconds})+Environment.NewLine);
                Flush();
            }
        }

        // One explicit-menu cycle case; no original receiving test is changed or relabelled.
        //720 is ONLY600 productive +90 drain + scene-exit margin, never extra gameplay time.
        [UnityTest,Timeout(720000)]
        public IEnumerator ColonisationCycleUsesExplicitMenuAndWholeTypedContinue()
        {
            var t=new CycleReceivingState();var s=t.Case;var stack=new Stack<IEnumerator>();
            stack.Push(CycleReceivingBody(t));Exception? failure=null;
            while(stack.Count>0)
            {
                bool next=false;object? value=null;var current=stack.Peek();
                try
                {
                    if(t.Productive.Elapsed.TotalSeconds>=600)throw new TimeoutException("Cycle productive deadline before "+t.Phase);
                    next=current.MoveNext();if(next)value=current.Current;CycleDiscover(t);
                    if(t.Productive.Elapsed.TotalSeconds>=600)throw new TimeoutException("Cycle productive deadline after "+t.Phase);
                    if(t.Flushed<s.Shared.Timings.Count)t.Flush();
                }
                catch(Exception error)
                {
                    failure=error; // Diagnostics must never replace the original failure or prevent cleanup.
                    try{CycleDiscover(t);}catch(Exception ownershipError){try{CampaignDiagnosticError("cycle-failure-ownership",ownershipError);}catch{}}
                    try{CycleFailureGeometry(t,current.GetType().FullName??"iterator");}
                    catch(Exception diagnosticError){try{CampaignDiagnosticError("cycle-failure-geometry",diagnosticError);}catch{}}
                }
                if(failure!=null)break;
                if(!next){stack.Pop();try{(current as IDisposable)?.Dispose();}catch(Exception error){failure=error;break;}continue;}
                if(value is IEnumerator nested){stack.Push(nested);continue;}
                yield return value;
            }
            t.Productive.Stop();
            void Cleanup(string phase,Action action)
            {try{action();}catch(Exception error){failure=failure??error;try{CampaignDiagnosticError(phase,error);}catch{/* Never skip release/drain/exit for a receipt failure. */}}}
            Cleanup("cycle-release-all",()=>{var error=s.ReleaseAll();failure=failure??error;});
            Cleanup("cycle-shared-release",()=>s.Shared.Release.Set());
            if(s.Shared.Entry!=null)Cleanup("cycle-disable",()=>s.Shared.Entry.enabled=false);
            while(stack.Count>0)try{(stack.Pop() as IDisposable)?.Dispose();}catch(Exception error){failure=failure??error;}
            bool settled=true;
            bool Pending()
            {
                bool pending=true;Cleanup("cycle-drain-status",()=>pending=CyclePending(t));return pending;
            }
            var cleanupClock=System.Diagnostics.Stopwatch.StartNew();
            while(Pending())
            {
                if(cleanupClock.Elapsed.TotalSeconds>=90)
                {
                    settled=false;directory=null;
                    failure=failure??new TimeoutException("Cycle worker unsettled; temporary namespace/override retained until scene exit.");break;
                }
                yield return null;
            }
            Cleanup("cycle-final-timings",t.Flush);
            bool overrideRestored=false;
            if(settled&&s.Shared.Entry!=null&&s.PriorDirectoryCaptured&&t.OverrideAssigned)
                Cleanup("cycle-restore-override",()=>{typeof(ReviewEntry).GetField("generatedCampaignTestDirectory",HullPrivate)!.SetValue(s.Shared.Entry,s.Shared.PriorDirectory);overrideRestored=true;});
            bool directoryRetained=!settled||t.OverrideAssigned&&!overrideRestored;
            if(directoryRetained)directory=null; // Never let shared teardown delete a namespace still selected by a failed restore.
            Cleanup("cycle-cleanup-receipt",()=>File.WriteAllText(Path.Combine(CampaignOutput(),"cycle-cleanup.json"),JsonConvert.SerializeObject(new{
                settled,allReleasesSet=s.Holds.All(h=>h.Release.IsSet),priorOverrideCaptured=s.PriorDirectoryCaptured,overrideAssigned=t.OverrideAssigned,trackedModels=t.Models.Count,trackedHosts=s.Hosts.Count,trackedContexts=s.Contexts.Count,
                overrideRestored,directoryRetained,entryDisabled=s.Shared.Entry==null||!s.Shared.Entry.enabled,
                cleanupMs=cleanupClock.Elapsed.TotalMilliseconds,productiveMs=t.Productive.Elapsed.TotalMilliseconds,
                productiveLimitSeconds=600,drainLimitSeconds=90,outerFrameworkSeconds=720,failed=failure!=null,lastPhase=t.Phase})));
            if(settled)foreach(var host in s.Hosts)Cleanup("cycle-remove-probe",()=>ColonyProbe(host,null));
            if(!s.Pending)foreach(var h in s.Holds){Cleanup("cycle-dispose-entered",()=>h.Entered.Dispose());Cleanup("cycle-dispose-release",()=>h.Release.Dispose());}
            if(s.Shared.Holder==null||s.Shared.Holder.IsCompleted)
            {Cleanup("cycle-dispose-shared-entered",()=>s.Shared.Entered.Dispose());Cleanup("cycle-dispose-shared-release",()=>s.Shared.Release.Dispose());}
            yield return new ExitPlayMode();
            if(failure!=null)System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
        }

        private static void CycleFailureGeometry(CycleReceivingState t,string iterator)
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
            File.WriteAllText(Path.Combine(CampaignOutput(),"cycle-failure-geometry.json"),JsonConvert.SerializeObject(new{
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
                File.WriteAllBytes(Path.Combine(CampaignOutput(),"cycle-failure-view.png"),pixels.EncodeToPNG());
            }
            finally{RenderTexture.active=prior;if(pixels!=null)UnityEngine.Object.DestroyImmediate(pixels);}
        }

        // Audit IDs never select an action or human destination.
        private static CampaignColonisationCycleSaveDto CycleAudit(ReviewUiModel model)
            =>((HobbyDS9.Application.ICampaignColonisationCycleSession)typeof(PhysicalReviewHost).GetField("session",HullPrivate)!.GetValue(StreamHost(model))!).ExportCycleSave();
        private static byte[] CycleBytes(ReviewUiModel model)=>new ReviewNewtonsoftCodec().EncodeCycleRoot(CycleAudit(model));
        private static void CycleCensus(CycleReceivingState t,CampaignColonisationCycleSaveDto root)
        {
            var first=AssertSingleCycle(root.AiPlans!);var actor=AssertSingleCycle(root.CycleActors!);var active=actor.Active;var plan=active?.Assignment;var state=root.BaseCampaign!;
            Assert.That(actor.ActorId,Is.EqualTo(first.ActorId));Assert.That(actor.ActorId,Is.Not.EqualTo(ColonyModel(t.Case).Current.Frame.Actor));
            var manifest=state.Colonisation!.ManifestRecords!.SingleOrDefault(m=>m.ManifestId==active?.SettlementManifestId);
            var queues=state.Production!.Where(q=>q.OwnerId==actor.ActorId&&plan!=null&&(q.QueueItemId==plan.KitQueueId||q.QueueItemId==plan.TransportQueueId)).ToArray();
            var camps=state.Colonisation.CampSources!.Where(c=>c.OwnerId==actor.ActorId).ToArray();
            var camp=camps.SingleOrDefault(c=>c.ManifestId==active?.SettlementManifestId);
            t.SawFirstClosed|=first.Stage==ColonisationAiStage.GrowthClosed;
            t.SawQueued|=queues.Length>0;t.SawActions|=first.LatestActions!.Any(a=>a.Accepted);
            t.SawLoaded|=manifest?.Stage==SettlerManifestStage.Loaded&&manifest.Passengers!.Population==100;
            t.SawFounding|=camp?.Stage==ColonyEstablishmentStage.Founding;
            var established=actor.Receipts!.Where(x=>x.Outcome==ColonisationCycleOutcome.Established).ToArray();
            t.SawEstablished|=established.Length>0;t.SawTwoEstablished|=established.Length>=2;
            t.SawThirdCommitted|=established.Length>=2&&active?.Ordinal==3&&active.SettlementManifestId!=null&&active.CommittedTargetPlanetId!=null&&manifest?.Kind==SettlerManifestKind.Settlement;
            Assert.That(first.LatestActions!.Count,Is.LessThanOrEqualTo(8));Assert.That(first.LatestActions.Count(a=>a.Accepted),Is.LessThanOrEqualTo(4));
            if(plan!=null)Assert.That(plan.LatestActions,Is.Empty);
            File.AppendAllText(Path.Combine(CampaignOutput(),"cycle-private-week-audit.jsonl"),JsonConvert.SerializeObject(new{
                turn=ColonyGame(state).TurnNumber,first.Stage,actor.NextOrdinal,actor.SupplyCampId,active,actor.Receipts,queues,manifest,camps,
                ownSources=ColonyGame(state).Colonies!.Where(w=>w.OwnerCivilizationId==actor.ActorId).ToArray(),
                origins=state.Colonisation.ManifestOrigins!.Where(o=>state.Colonisation.ManifestRecords!.Any(m=>m.ManifestId==o.ManifestId&&m.OwnerId==actor.ActorId)).ToArray(),
                resolutions=state.Colonisation.ManifestResolutions!.Where(o=>state.Colonisation.ManifestRecords!.Any(m=>m.ManifestId==o.ManifestId&&m.OwnerId==actor.ActorId)).ToArray(),
                qualification="PRIVATE AUDIT ONLY. No AI fact selects human controls/targets/routes. Concurrent queue is not consumed-kit provenance; source stock deltas include Economy/migration/freight."})+Environment.NewLine);
        }
        private static T AssertSingleCycle<T>(IReadOnlyList<T> rows)
        {Assert.That(rows.Count,Is.EqualTo(1),"actual two-empire case has exactly one AI actor");return rows[0];}
        private sealed class CycleActivationContext:SynchronizationContext
        {
            private readonly CheckpointContext inner;private int pending;
            internal CycleActivationContext(CheckpointContext inner){this.inner=inner;}
            internal bool Pending=>Volatile.Read(ref pending)!=0;
            public override void Post(SendOrPostCallback callback,object? state)
            {
                Interlocked.Increment(ref pending);int released=0;
                void Release(){if(Interlocked.Exchange(ref released,1)==0)Interlocked.Decrement(ref pending);}
                try{inner.Post(value=>{try{callback(value);}finally{Release();}},state);}
                catch{Release();throw;}
            }
            public override void Send(SendOrPostCallback callback,object? state)=>inner.Send(callback,state);
            public override SynchronizationContext CreateCopy()=>this;
        }
        private static void CycleDiscover(CycleReceivingState t)
        {
            var entry=t.Case.Shared.Entry;if(entry==null)return;var model=CampaignModel(entry);
            if(!t.Models.Contains(model))t.Models.Add(model);
            var host=StreamHost(model);if(!t.Case.Hosts.Contains(host))t.Case.Hosts.Add(host);
        }
        private static bool CyclePending(CycleReceivingState t)
        {
            CycleDiscover(t);var s=t.Case;
            return s.Pending||s.Shared.Holder?.IsCompleted==false||t.Activations.Any(a=>a.Pending)
                ||t.Models.Any(m=>m.Busy||m.Current.HasPendingWork)||s.Shared.Entry!=null&&ColonyPending(s.Shared.Entry);
        }
        private static void CycleActivate(CycleReceivingState t,Action activate)
        {
            var s=t.Case;CycleDiscover(t);
            object?[] Operations()=>new[]{CheckpointField(s.Shared.Entry!,"colonisationUiWork"),CheckpointField(s.Shared.Entry!,"streamedUiWork"),CheckpointField(s.Shared.Entry!,"checkpointUiOperation"),CheckpointField(s.Shared.Entry!,"generatedCampaignOperationToken")};
            var before=Operations();var prior=SynchronizationContext.Current;Assert.That(prior,Is.Not.Null);
            var context=new CheckpointContext(prior!);var owner=new CycleActivationContext(context);
            s.Contexts.Add(context);t.Activations.Add(owner); // Retain BEFORE the action can start work or throw.
            SynchronizationContext.SetSynchronizationContext(owner);bool returned=false;
            try{activate();returned=true;}
            finally
            {
                SynchronizationContext.SetSynchronizationContext(prior);
                try
                {
                    CycleDiscover(t);var after=Operations();
                    if(returned&&!owner.Pending&&!ColonyPending(s.Shared.Entry!)&&!after.Where((operation,index)=>operation!=null&&!ReferenceEquals(operation,before[index])).Any())
                    {context.Inline=true;Volatile.Write(ref context.Finished,1);}
                    // On a throwing/uncertain activation keep ownership pending until a real continuation settles it; cleanup fails closed otherwise.
                }
                catch(Exception error){if(returned)throw;try{CampaignDiagnosticError("cycle-activation-discovery",error);}catch{}}
            }
        }
        private static IEnumerator CycleWorld(CycleReceivingState t,UIDocument doc,string world)
        {
            CycleActivate(t,()=>CampaignPage(doc,ReviewWorkspacePage.Production));yield return null;yield return null;
            var tab=doc.rootVisualElement.Q<Button>("colonisation-tab-Worlds");yield return ColonyReach(doc,tab,target:"current own Worlds tab");
            CycleActivate(t,()=>Click(tab));yield return ColonyIdle(t.Case,"cycle-own-worlds-tab");
            var worlds=ColonyModel(t.Case).Colonisation().Worlds;var field=doc.rootVisualElement.Q<DropdownField>("colonisation-world-selector");
            Assert.That(field,Is.Not.Null);int index=worlds.ToList().FindIndex(w=>w.Id==world);Assert.That(index,Is.GreaterThanOrEqualTo(0),"current human-owned world selection");
            yield return ColonyReach(doc,field);CycleActivate(t,()=>field.index=index);yield return null;yield return null;
        }
        private static IEnumerator CycleOperation(CycleReceivingState t,UIDocument doc,string phase,string busyTitle,Action activate)
        {
            var s=t.Case;var clock=System.Diagnostics.Stopwatch.StartNew();var busyClock=new System.Diagnostics.Stopwatch();int busyFrames=0,ticks=0;
            bool sawBusy=false;var positions=new HashSet<float>();var pulse=doc.rootVisualElement.schedule.Execute(()=>ticks++).Every(10);
            try
            {
                CycleActivate(t,activate);
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
                File.AppendAllText(Path.Combine(CampaignOutput(),"cycle-operation-observations.jsonl"),JsonConvert.SerializeObject(new{phase,wallMs=clock.Elapsed.TotalMilliseconds,busyMs=busyClock.Elapsed.TotalMilliseconds,busyFrames,uiTicks=ticks,indicatorPositions=positions.Count,sawBusy,noLatencyThreshold=true})+Environment.NewLine);
            }
            finally{pulse.Pause();}
        }
        private static IEnumerator CycleAdvance(CycleReceivingState t,UIDocument doc)
        {
            var s=t.Case;Assert.That(++t.UiAdvances,Is.LessThanOrEqualTo(128),"global ordinary UI Advance cap including the fresh post-Continue week");
            var model=ColonyModel(s);Assert.That(model.Current.Frame.Status.Phase,Is.EqualTo(SessionPhase.Planning),"unsupported tactical/event phase is a finding, never forged or switched");
            Assert.That(doc.rootVisualElement.Q("review-one-use-confirmation"),Is.Null,"no unattended modal before End week");
            CycleActivate(t,()=>CampaignPage(doc,ReviewWorkspacePage.Map));yield return V3Settled(doc);var turn=model.Current.Frame.Status.Turn;
            var button=doc.rootVisualElement.Q<Button>("player-primary-action");yield return ColonyToolbarReach(doc,button);
            CycleActivate(t,()=>{Click(button);Confirm(doc);});yield return ColonyIdle(s,"cycle-ui-week");
            Assert.That(ColonyModel(s).Current.Frame.Status.Phase,Is.EqualTo(SessionPhase.Planning),"unexpected tactical/modal flow stops the bounded receiving case");
            Assert.That(doc.rootVisualElement.Q("review-one-use-confirmation"),Is.Null);Assert.That(ColonyModel(s).Current.Frame.Status.Turn,Is.EqualTo(turn+1));
        }
        private static IEnumerator CycleCapture(CycleReceivingState t,UIDocument doc,Vector2Int size,string filename)
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
        private IEnumerator CycleReceivingBody(CycleReceivingState t)
        {
            var s=t.Case;yield return new EnterPlayMode();yield return null;yield return null;
            s.Shared.Entry=UnityEngine.Object.FindAnyObjectByType<ReviewEntry>();Assert.That(s.Shared.Entry,Is.Not.Null);var entry=s.Shared.Entry!;
            var field=typeof(ReviewEntry).GetField("generatedCampaignTestDirectory",HullPrivate)!;
            s.Shared.PriorDirectory=(string?)field.GetValue(entry);s.PriorDirectoryCaptured=true;
            directory=Path.Combine(Path.GetTempPath(),"HobbyDS9-Galaxy-Engine-Cycle-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(directory);
            var storePath=Path.Combine(directory,"campaign");field.SetValue(entry,storePath);t.OverrideAssigned=true;
            Assert.That(field.GetValue(entry),Is.EqualTo(storePath));
            var resolved=(string)typeof(ReviewEntry).GetMethod("ColonisationCycleCampaignDirectory",HullPrivate)!.Invoke(entry,null)!;
            Assert.That(Path.GetFullPath(resolved),Is.EqualTo(Path.GetFullPath(storePath)),"actual Cycle-only override resolution equals temporary store");
            CycleDiscover(t);var doc=entry.GetComponent<UIDocument>();
            using(var surface=new GalaxyInputSurface(doc))
            {
                t.Mark("explicit-new-begin");CycleActivate(t,()=>{doc.rootVisualElement.Q<TextField>("generated-campaign-seed").value="42";
                doc.rootVisualElement.Q<DropdownField>("generated-campaign-size").value="250";doc.rootVisualElement.Q<DropdownField>("generated-campaign-quiet").value="2.00\u00d7";});
                var menu=doc.rootVisualElement.Q<Foldout>("colonisation-cycle-campaign-menu");Assert.That(menu,Is.Not.Null);Assert.That(menu.text,Is.EqualTo("Campaign"));Assert.That(menu.value,Is.True);
                var oldModel=ColonyModel(s);var start=doc.rootVisualElement.Q<Button>("colonisation-cycle-campaign-start");yield return ColonyReach(doc,start,target:"explicit New Cycle campaign");
                yield return CycleOperation(t,doc,"cycle-new250","Generating",()=>{Click(start);if(doc.rootVisualElement.Q("review-one-use-confirmation")!=null)Confirm(doc);});
                var model=ColonyModel(s);Assert.That(model,Is.Not.SameAs(oldModel));Assert.That(model.ColonisationCycleCampaign,Is.True);Assert.That(model.ColonisationGrowthCampaign,Is.False);Assert.That(model.SupportsKitFreePassengers,Is.True);
                var controller=model.Current;var actor=controller.Frame.Actor;var host=StreamHost(model);var initial=CycleAudit(model);
                Assert.That(initial.FormatVersion,Is.EqualTo(14));Assert.That(initial.ProfileId,Is.EqualTo(CampaignColonisationCycleIdentity.Profile));Assert.That(initial.PackageHash.Length,Is.EqualTo(64));
                Assert.That(initial.BaseCampaign!.FrozenPlanets!.PolicyId,Is.EqualTo(CampaignColonisationExpansionIdentity.InventoryPolicy));
                Assert.That(initial.BaseCampaign.Map!.Settings!.Map!.Seed,Is.EqualTo("42"));Assert.That(initial.BaseCampaign.Map.Settings.Map.SystemCount,Is.EqualTo(250));
                Assert.That(initial.AiPlans!.All(p=>p.ActorId!=actor),Is.True);Assert.That(initial.CycleActors!.All(g=>g.ActorId!=actor),Is.True);
                var initialActor=AssertSingleCycle(initial.CycleActors!);Assert.That(initialActor.Receipts,Is.Empty);Assert.That(initialActor.Active,Is.Null);Assert.That(initialActor.NextOrdinal,Is.EqualTo(1));
                CycleCensus(t,initial);t.Mark("explicit-new-complete");
                // Only current human own views choose this page. No AI IDs or positions select a command.
                var own=model.Colonisation();var home=own.Worlds.Where(w=>w.Stage==ColonyEstablishmentStage.Established&&w.Population>0).OrderByDescending(w=>w.Industry).ThenBy(w=>w.Id,StringComparer.Ordinal).First();
                s.Home=home.Id;t.Mark("own-world-navigation-begin");yield return CycleWorld(t,doc,s.Home);t.Mark("own-world-navigation-complete");
                t.Mark("own-kit-reach-begin");yield return ColonyReach(doc,doc.rootVisualElement.Q<Button>("colonisation-build-kit"),target:"human own production remains usable");t.Mark("own-kit-reach-complete");
                CampaignColonisationCycleSaveDto audit=initial;
                while(!t.SawThirdCommitted&&t.UiAdvances<127)
                {
                    yield return CycleAdvance(t,doc);Assert.That(ColonyModel(s).Current,Is.SameAs(controller));Assert.That(ColonyModel(s).Current.Frame.Actor,Is.EqualTo(actor));
                    audit=CycleAudit(ColonyModel(s));CycleCensus(t,audit);t.Mark("completed-ui-week-"+ColonyModel(s).Current.Frame.Status.Turn);
                }
                Assert.That(t.SawFirstClosed&&t.SawQueued&&t.SawActions&&t.SawLoaded&&t.SawFounding&&t.SawTwoEstablished&&t.SawThirdCommitted,Is.True,"ordinary bootstrap, two real Cycle establishments and third commit within global128/600 seconds");
                var first=AssertSingleCycle(audit.AiPlans!);var cycle=AssertSingleCycle(audit.CycleActors!);var terminal=cycle.Receipts!.Where(x=>x.Outcome==ColonisationCycleOutcome.Established).ToArray();
                Assert.That(first.Stage,Is.EqualTo(ColonisationAiStage.GrowthClosed));Assert.That(terminal.Length,Is.EqualTo(2));
                Assert.That(terminal.Select(x=>x.SettlementManifestId).Distinct().Count(),Is.EqualTo(2));Assert.That(terminal.Select(x=>x.TargetPlanetId).Distinct().Count(),Is.EqualTo(2));
                foreach(var receipt in terminal)
                {
                    var camp=audit.BaseCampaign!.Colonisation!.CampSources!.Single(c=>c.CampId==receipt.CampId&&c.OwnerId==cycle.ActorId&&c.ManifestId==receipt.SettlementManifestId);
                    Assert.That(audit.BaseCampaign.Colonisation.CampTerminals!.Any(c=>c.CampId==camp.CampId&&c.Kind==SettlementTerminalKind.Established&&c.Turn==receipt.TerminalTurn),Is.True);
                    Assert.That(audit.BaseCampaign.Colonisation.ColonyOrigins!.Any(c=>c.ColonyId==camp.ColonyId&&c.ManifestId==receipt.SettlementManifestId&&c.PlanetId==receipt.TargetPlanetId),Is.True);
                    Assert.That(camp.FedWeeks,Is.GreaterThanOrEqualTo(4));Assert.That(camp.CompletedIndustry,Is.EqualTo(16));Assert.That(camp.Deaths,Is.Zero);
                    var manifest=audit.BaseCampaign.Colonisation.ManifestRecords!.Single(m=>m.ManifestId==receipt.SettlementManifestId);
                    Assert.That(manifest.OwnerId,Is.EqualTo(cycle.ActorId));Assert.That(manifest.Kind,Is.EqualTo(SettlerManifestKind.Settlement));Assert.That(manifest.PopulationTransferred,Is.EqualTo(100));Assert.That(manifest.EquipmentConsumed,Is.EqualTo(1));
                    var origin=audit.BaseCampaign.Colonisation.ManifestOrigins!.Single(o=>o.ManifestId==manifest.ManifestId);Assert.That(origin.EmbarkedPopulation,Is.EqualTo(100));Assert.That(origin.LoadedFood,Is.EqualTo(20));
                    Assert.That(ColonyModel(s).Colonisation().Worlds.Any(w=>w.Id==camp.ColonyId),Is.False,"AI colony never selected/revealed for capture");
                }
                var third=cycle.Active!;Assert.That(third.Ordinal,Is.EqualTo(3));Assert.That(third.SettlementManifestId,Is.Not.Null);Assert.That(third.CommittedTargetPlanetId,Is.Not.Null);
                var committed=audit.BaseCampaign!.Colonisation!.ManifestRecords!.Single(m=>m.ManifestId==third.SettlementManifestId&&m.OwnerId==cycle.ActorId&&m.Kind==SettlerManifestKind.Settlement);
                var thirdOrigin=audit.BaseCampaign.Colonisation.ManifestOrigins!.Single(o=>o.ManifestId==committed.ManifestId);Assert.That(thirdOrigin.EmbarkedPopulation,Is.EqualTo(100));Assert.That(thirdOrigin.LoadedFood,Is.EqualTo(20));
                var expected=new ReviewNewtonsoftCodec().EncodeCycleRoot(audit);var mission=JsonConvert.SerializeObject(ColonyModel(s).FrontierCommission());
                var saved=Path.Combine(storePath,"unity-campaign-colonisation-cycle-v1-generated-1.json");Assert.That(File.Exists(saved),Is.False,"unused temporary slot only");
                File.WriteAllBytes(Path.Combine(CampaignOutput(),"cycle-committed-pre-save-private-audit.json"),expected);
                t.Mark("save-begin");CycleActivate(t,()=>CampaignPage(doc,ReviewWorkspacePage.Saves));yield return null;CycleActivate(t,()=>Click(doc.rootVisualElement.Q<Button>("save-slot-generated-1")));
                yield return CycleOperation(t,doc,"cycle-whole14-save","Saving",()=>Click(doc.rootVisualElement.Q<Button>("player-save")));
                Assert.That(File.ReadAllBytes(saved),Is.EqualTo(expected));Assert.That(CycleBytes(ColonyModel(s)),Is.EqualTo(expected));t.Mark("save-complete");
                var prior=ColonyModel(s);CycleActivate(t,()=>Click(doc.rootVisualElement.Q<Button>("demo-menu")));yield return null;yield return null;
                var load=doc.rootVisualElement.Q<Button>("colonisation-cycle-campaign-continue-generated-1");yield return ColonyReach(doc,load,target:"explicit Cycle Continue1");t.Mark("continue-begin");
                yield return CycleOperation(t,doc,"cycle-whole14-continue","Loading",()=>{Click(load);Confirm(doc);});
                model=ColonyModel(s);Assert.That(model,Is.Not.SameAs(prior));Assert.That(model.Current,Is.Not.SameAs(controller));Assert.That(StreamHost(model),Is.Not.SameAs(host));Assert.That(model.Current.Frame.Actor,Is.EqualTo(actor));
                Assert.That(model.ColonisationCycleCampaign,Is.True);Assert.That(model.ColonisationGrowthCampaign,Is.False);
                var continued=CycleAudit(model);Assert.That(CycleBytes(model),Is.EqualTo(expected),"whole committed14 equality BEFORE any post-Continue week");
                Assert.That(JsonConvert.SerializeObject(continued.AiPlans),Is.EqualTo(JsonConvert.SerializeObject(audit.AiPlans)));Assert.That(JsonConvert.SerializeObject(continued.CycleActors),Is.EqualTo(JsonConvert.SerializeObject(audit.CycleActors)));
                Assert.That(continued.CycleActors![0].SupplyCampId,Is.EqualTo(cycle.SupplyCampId));Assert.That(JsonConvert.SerializeObject(model.FrontierCommission()),Is.EqualTo(mission));
                File.WriteAllBytes(Path.Combine(CampaignOutput(),"cycle-continued-before-fresh-week-private-audit.json"),CycleBytes(model));t.Mark("continue-complete-exact-before-fresh-week");
                yield return CycleAdvance(t,doc);CycleCensus(t,CycleAudit(ColonyModel(s)));t.Mark("fresh-post-continue-week-complete");
                yield return CycleWorld(t,doc,s.Home);var unchanged=CycleBytes(ColonyModel(s));
                foreach(var size in new[]{new Vector2Int(1280,720),new Vector2Int(1920,1080),new Vector2Int(3440,1440)})
                {yield return CycleCapture(t,doc,size,CycleImages[size.x==1280?0:size.x==1920?1:2]);t.Mark("capture-"+size.x);}
                Assert.That(CycleBytes(ColonyModel(s)),Is.EqualTo(unchanged),"own-view resize/capture is read-only");
                Assert.That(Directory.GetFiles(CampaignOutput(),"cycle-0*-continued-*.png").Select(Path.GetFileName),Is.EquivalentTo(CycleImages));
                File.WriteAllBytes(Path.Combine(CampaignOutput(),"cycle-final-private-audit.json"),unchanged);
                File.WriteAllText(Path.Combine(CampaignOutput(),"cycle-summary.json"),JsonConvert.SerializeObject(new{explicitMenu=true,systems=250,seed="42",quiet="2.00x",uiAdvances=t.UiAdvances,maximumUiAdvances=128,finalTurn=ColonyModel(s).Current.Frame.Status.Turn,
                    t.SawFirstClosed,t.SawQueued,t.SawActions,t.SawLoaded,t.SawFounding,t.SawEstablished,t.SawTwoEstablished,t.SawThirdCommitted,whole14SaveContinueEqualBeforeFreshWeek=true,newController=true,newHost=true,sameHumanActor=true,firstPlansCycleHistoryActiveAndSupplyCampPreserved=true,freshPostContinueWeek=true,captures=CycleImages,
                    qualification="Actual AI records are private audit ONLY, never human action selection. Pictures show own human views. No fresh human voyage/native/performance or original15-image acceptance claim."}));
                t.Mark("all-cycle-gates-complete");
            }
        }
    }
}
