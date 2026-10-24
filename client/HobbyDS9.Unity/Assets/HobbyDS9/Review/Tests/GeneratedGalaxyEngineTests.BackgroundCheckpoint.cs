#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using HobbyDS9.Contracts;
using Newtonsoft.Json;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace HobbyDS9.UnityReview.Tests
{
    public sealed partial class GeneratedGalaxyEngineTests
    {
        private static object? CheckpointField(ReviewEntry entry,string name)=>typeof(ReviewEntry).GetField(name,BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(entry);
        // Instrument the real Unity synchronization-context continuation without changing its scheduling.
        private sealed class CheckpointContext : SynchronizationContext
        {
            private readonly SynchronizationContext target;
            internal int Finished,Thread;
            internal bool Inline;
            internal CheckpointContext(SynchronizationContext target){this.target=target;}
            public override void Post(SendOrPostCallback callback,object? state)=>target.Post(s=>{
                try{callback(s);}finally{Thread=System.Threading.Thread.CurrentThread.ManagedThreadId;Interlocked.Increment(ref Finished);}
            },state);
            public override void Send(SendOrPostCallback callback,object? state)=>target.Send(callback,state);
            public override SynchronizationContext CreateCopy()=>this;
        }
        private sealed class CheckpointCaseState
        {
            internal ReviewEntry? Entry;
            internal string? PriorDirectory;
            internal readonly List<CheckpointContext> Contexts=new List<CheckpointContext>();
            internal readonly List<ManualResetEventSlim> Releases=new List<ManualResetEventSlim>();
            internal readonly List<Task> Holders=new List<Task>();
            internal readonly List<object> Outcomes=new List<object>();
            internal void Release(){foreach(var release in Releases)release.Set();}
        }
        private static CheckpointContext CheckpointActivate(CheckpointCaseState state,Action activate)
        {
            var original=SynchronizationContext.Current;Assert.That(original,Is.Not.Null,"actual Unity synchronization context");
            var tracked=new CheckpointContext(original!);SynchronizationContext.SetSynchronizationContext(tracked);
            try{activate();}finally{SynchronizationContext.SetSynchronizationContext(original);}
            if(state.Entry!=null&&(CheckpointField(state.Entry,"checkpointUiOperation")!=null||(bool)CheckpointField(state.Entry,"generatedCampaignBusy")!))state.Contexts.Add(tracked);
            else{tracked.Inline=true;tracked.Thread=System.Threading.Thread.CurrentThread.ManagedThreadId;tracked.Finished=1;}
            return tracked;
        }
        private static void CheckpointCapture(UIDocument doc,string name)
        {
            var target=doc.panelSettings.targetTexture;Assert.That(target,Is.Not.Null);
            var previous=RenderTexture.active;Texture2D? image=null;
            try
            {
                RenderTexture.active=target;image=new Texture2D(target.width,target.height,TextureFormat.RGBA32,false);
                image.ReadPixels(new Rect(0,0,target.width,target.height),0,0);image.Apply();
                File.WriteAllBytes(Path.Combine(CampaignOutput(),"checkpoint-"+name+".png"),image.EncodeToPNG());
            }
            finally{RenderTexture.active=previous;if(image!=null)UnityEngine.Object.DestroyImmediate(image);}
        }
        private static void CheckpointBusy(UIDocument doc,string title)
        {
            Assert.That(doc.rootVisualElement.Q("campaign-operation-busy"),Is.Not.Null);
            Assert.That(doc.rootVisualElement.Q<Label>("campaign-operation-title").text,Does.Contain(title));
            Assert.That(doc.rootVisualElement.Q("campaign-operation-activity").worldBound.height,Is.GreaterThan(0));
        }
        [UnityTest,Timeout(600000)]
        public IEnumerator BackgroundCheckpointOperationsKeepActualUiResponsiveAndAtomic()
        {
            var state=new CheckpointCaseState();var body=BackgroundCheckpointBody(state);Exception? failure=null;
            while(true)
            {
                bool more;object? current=null;
                try{more=body.MoveNext();if(more)current=body.Current;}
                catch(Exception error){failure=error;break;}
                if(!more)break;yield return current;
            }
            // This wrapper observes every assertion in the body directly; cleanup does not depend
            // on Unity unwinding a parent iterator after a nested coroutine assertion.
            state.Release();if(state.Entry!=null)state.Entry.enabled=false;
            var cleanup=System.Diagnostics.Stopwatch.StartNew();
            while(state.Holders.Any(t=>!t.IsCompleted)||state.Contexts.Any(c=>Volatile.Read(ref c.Finished)==0))
            {
                if(cleanup.Elapsed.TotalSeconds>=90)
                {
                    File.WriteAllText(Path.Combine(CampaignOutput(),"checkpoint-cleanup-unsettled.txt"),directory??"unknown");
                    directory=null;Assert.Fail("Worker cleanup did not settle; temporary namespace retained rather than deleting active IO.");
                }
                yield return null;
            }
            foreach(var task in state.Holders)if(task.IsFaulted&&failure==null)failure=task.Exception;
            if(state.Entry!=null)typeof(ReviewEntry).GetField("generatedCampaignTestDirectory",BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(state.Entry,state.PriorDirectory);
            (body as IDisposable)?.Dispose();foreach(var release in state.Releases)release.Dispose();
            File.WriteAllText(Path.Combine(CampaignOutput(),"checkpoint-results.json"),JsonConvert.SerializeObject(new{
                outcomes=state.Outcomes,passed=failure==null,continuations=state.Contexts.Select(c=>new{completed=c.Finished,thread=c.Thread,c.Inline}),
                claim="Actual UI NavigationSubmit activation/Unity continuation and scheduler proof; no native pointer/FPS/GPU claim",failure=failure?.ToString()}));
            if(failure!=null)ExceptionDispatchInfo.Capture(failure).Throw();
            yield return new ExitPlayMode();
        }
        private IEnumerator BackgroundCheckpointBody(CheckpointCaseState state)
        {
            yield return new EnterPlayMode();yield return null;yield return null;
            var entry=UnityEngine.Object.FindAnyObjectByType<ReviewEntry>();state.Entry=entry;var doc=entry.GetComponent<UIDocument>();
            int mainThread=Thread.CurrentThread.ManagedThreadId;
            directory=Path.Combine(Path.GetTempPath(),"HobbyDS9-Galaxy-Engine-Checkpoint-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(directory);
            var storePath=Path.Combine(directory,"campaign");var directoryField=typeof(ReviewEntry).GetField("generatedCampaignTestDirectory",BindingFlags.Instance|BindingFlags.NonPublic)!;
            state.PriorDirectory=(string?)directoryField.GetValue(entry);directoryField.SetValue(entry,storePath);
            var codec=new ReviewNewtonsoftCodec();
            using(var surface=new GalaxyInputSurface(doc))
            {
                yield return null;yield return null;
                // Actual mainlanding New 250 also receives the same busy panel. No forced delay.
                doc.rootVisualElement.Q<TextField>("generated-campaign-seed").value="42";doc.rootVisualElement.Q<DropdownField>("generated-campaign-size").value="250";
                var generation=CheckpointActivate(state,()=>Click(doc.rootVisualElement.Q<Button>("generated-campaign-start")));
                Assert.That(doc.rootVisualElement.Q<Label>("campaign-operation-title").text,Does.Contain("Generating galaxy"));
                int expectedCaptures=5;yield return null;yield return null;
                if((bool)CheckpointField(entry,"generatedCampaignBusy")!)
                {CheckpointBusy(doc,"Generating");CheckpointCapture(doc,"generating-galaxy");expectedCaptures++;}
                var timer=System.Diagnostics.Stopwatch.StartNew();
                while((bool)CheckpointField(entry,"generatedCampaignBusy")!){Assert.That(timer.Elapsed.TotalSeconds,Is.LessThan(90));yield return null;}
                yield return null;yield return null;Assert.That(generation.Thread,Is.EqualTo(mainThread));
                Assert.That(doc.rootVisualElement.Q("campaign-operation-busy"),Is.Null);
                var initial=codec.EncodeStaticAstronomyCampaignRoot(CampaignPrivateAudit(CampaignModel(entry)));
                Action<UnityReviewWriteStage>? writeProbe=null;
                var store=new ReviewStaticAstronomyCampaignSlotStore(storePath,codec,s=>writeProbe?.Invoke(s));store.Write("generated-1",codec.DecodeStaticAstronomyCampaignRoot(initial),false);
                var template=(CampaignPlayableFleetPackageDto)typeof(ReviewEntry).GetMethod("LoadPlayableFleetTemplate",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(entry,null)!;
                var model=ReviewUiModel.ContinueStaticAstronomyCampaign(template,codec,store,"generated-1");
                void Publish(ReviewUiModel next)=>typeof(ReviewEntry).GetMethod("PublishPlayerProfile",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(entry,new object[]{next,"generated-1"});
                Publish(model);yield return null;yield return null;
                CampaignPage(doc,ReviewWorkspacePage.Research);var split=doc.rootVisualElement.Q<DropdownField>("integrated-research-split");split.index=(split.index+1)%split.choices.Count;
                var draft=model.ResearchInputs()!;int desired=draft.ScienceUnits;Assert.That(draft.Dirty,Is.True);
                CampaignPage(doc,ReviewWorkspacePage.Saves);Click(doc.rootVisualElement.Q<Button>("save-slot-generated-2"));
                var before=model.Current.Frame;var beforeController=model.Current;double zoom=model.GeneratedViewport.Zoom,worldX=model.GeneratedViewport.WorldX(0),worldY=model.GeneratedViewport.WorldY(0);
                var entered=new ManualResetEventSlim();var release=new ManualResetEventSlim();state.Releases.Add(release);int writerThread=0,writes=0;
                writeProbe=s=>{if(s==UnityReviewWriteStage.AfterPartialWrite){writerThread=Thread.CurrentThread.ManagedThreadId;Interlocked.Increment(ref writes);entered.Set();if(!release.Wait(TimeSpan.FromSeconds(90)))throw new TimeoutException();}};
                var save=CheckpointActivate(state,()=>Click(doc.rootVisualElement.Q<Button>("player-save")));timer.Restart();
                while(!entered.IsSet){Assert.That(timer.Elapsed.TotalSeconds,Is.LessThan(90));yield return null;}
                CheckpointBusy(doc,"Saving");Assert.That(writerThread,Is.Not.EqualTo(mainThread));var active=CheckpointField(entry,"checkpointUiOperation");
                int ticks=0,frames=0;var heartbeat=doc.rootVisualElement.schedule.Execute(()=>ticks++).Every(10);var positions=new HashSet<float>();timer.Restart();
                while(frames<8||timer.Elapsed.TotalSeconds<.3)
                {positions.Add(doc.rootVisualElement.Q("campaign-operation-indicator").style.left.value.value);frames++;yield return null;}
                Assert.That(ticks,Is.GreaterThanOrEqualTo(3));Assert.That(positions.Count,Is.GreaterThan(1));
                // Real attached navigation/menu actions must be refused while the work owns the UI.
                Click(doc.rootVisualElement.Q<Button>("demo-menu"));Click(doc.rootVisualElement.Q<Button>("review-nav-Map"));
                Assert.That(CheckpointField(entry,"checkpointUiOperation"),Is.SameAs(active));Assert.That(model.BeginStaticCheckpointOperation("other",false),Is.Null);Assert.That(model.Advance(),Is.False);
                doc.rootVisualElement.style.width=1200;yield return null;yield return null;CheckpointBusy(doc,"Saving");Assert.That(model.Current.Frame,Is.SameAs(before));
                CheckpointCapture(doc,"saving-responsive-resized");state.Outcomes.Add(new{phase="blocked-save",ticks,frames,indicatorPositions=positions.Count,writerThread,mainThread});
                release.Set();timer.Restart();while(model.Busy){Assert.That(timer.Elapsed.TotalSeconds,Is.LessThan(90));yield return null;}
                yield return null;Assert.That(save.Finished,Is.GreaterThan(0));Assert.That(save.Thread,Is.EqualTo(mainThread));Assert.That(writes,Is.EqualTo(1));heartbeat.Pause();writeProbe=null;
                doc.rootVisualElement.style.width=1280;yield return null;yield return null;
                Assert.That(model.Message,Is.EqualTo("Checkpoint result: None"));Assert.That(model.Current.Frame,Is.SameAs(before));Assert.That(model.ResearchInputs(),Is.SameAs(draft));Assert.That(draft.ScienceUnits,Is.EqualTo(desired));
                var file2=Path.Combine(storePath,"unity-generated-static-astronomy-v2-generated-2.json");Assert.That(File.ReadAllBytes(file2),Is.EqualTo(initial));
                // Occupied Cancel never starts worker work or changes the root/draft.
                Click(doc.rootVisualElement.Q<Button>("player-save"));Assert.That(doc.rootVisualElement.Q("review-one-use-confirmation"),Is.Not.Null);Confirm(doc,false);
                Assert.That(CheckpointField(entry,"checkpointUiOperation"),Is.Null);Assert.That(File.ReadAllBytes(file2),Is.EqualTo(initial));Assert.That(model.ResearchInputs(),Is.SameAs(draft));
                // A shape-admitted but incompatible root fails after full validation, preserving camera/drafts/frame.
                var invalid=codec.DecodeStaticAstronomyCampaignRoot(initial);invalid.PackageHash=new string('a',64);store.Write("generated-3",invalid,false);
                Click(doc.rootVisualElement.Q<Button>("save-slot-generated-3"));Click(doc.rootVisualElement.Q<Button>("player-load"));
                var failed=CheckpointActivate(state,()=>Confirm(doc));timer.Restart();while(model.Busy){Assert.That(timer.Elapsed.TotalSeconds,Is.LessThan(90));yield return null;}
                yield return null;Assert.That(failed.Thread,Is.EqualTo(mainThread));Assert.That(model.Current,Is.SameAs(beforeController));Assert.That(model.Current.Frame,Is.SameAs(before));
                Assert.That(model.ResearchInputs(),Is.SameAs(draft));Assert.That(draft.ScienceUnits,Is.EqualTo(desired));Assert.That(model.GeneratedViewport.Zoom,Is.EqualTo(zoom));Assert.That(model.GeneratedViewport.WorldX(0),Is.EqualTo(worldX));Assert.That(model.GeneratedViewport.WorldY(0),Is.EqualTo(worldY));
                Assert.That(doc.rootVisualElement.Q("campaign-operation-busy"),Is.Null);CheckpointCapture(doc,"failed-load-kept-draft");
                // Race an empty admission with a distinct valid file after the initial existence check.
                File.Delete(file2);var raced=System.Text.Encoding.UTF8.GetBytes(" \n"+System.Text.Encoding.UTF8.GetString(initial));
                writeProbe=s=>{if(s==UnityReviewWriteStage.BeforeWrite)File.WriteAllBytes(file2,raced);};
                Click(doc.rootVisualElement.Q<Button>("save-slot-generated-2"));var race=CheckpointActivate(state,()=>Click(doc.rootVisualElement.Q<Button>("player-save")));timer.Restart();
                while(model.Busy){Assert.That(timer.Elapsed.TotalSeconds,Is.LessThan(90));yield return null;}yield return null;
                Assert.That(doc.rootVisualElement.Q("review-one-use-confirmation"),Is.Not.Null);Confirm(doc,false);writeProbe=null;
                Assert.That(File.ReadAllBytes(file2),Is.EqualTo(raced));Assert.That(model.Current.Frame,Is.SameAs(before));Assert.That(model.ResearchInputs(),Is.SameAs(draft));
                // Block actual Load on its real store gate, held ONLY by a separate worker task.
                var loadEntered=new ManualResetEventSlim();var loadRelease=new ManualResetEventSlim();state.Releases.Add(loadRelease);
                var storeGate=typeof(ReviewStaticAstronomyCampaignSlotStore).GetField("gate",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(store)!;
                var holder=Task.Run(()=>{lock(storeGate){loadEntered.Set();if(!loadRelease.Wait(TimeSpan.FromSeconds(90)))throw new TimeoutException();}});state.Holders.Add(holder);timer.Restart();
                while(!loadEntered.IsSet){Assert.That(timer.Elapsed.TotalSeconds,Is.LessThan(90));yield return null;}
                Click(doc.rootVisualElement.Q<Button>("player-load"));var load=CheckpointActivate(state,()=>Confirm(doc));yield return null;CheckpointBusy(doc,"Loading");
                ticks=0;frames=0;positions.Clear();heartbeat=doc.rootVisualElement.schedule.Execute(()=>ticks++).Every(10);timer.Restart();
                while(frames<8||timer.Elapsed.TotalSeconds<.3){positions.Add(doc.rootVisualElement.Q("campaign-operation-indicator").style.left.value.value);frames++;yield return null;}
                Assert.That(ticks,Is.GreaterThanOrEqualTo(3));Assert.That(positions.Count,Is.GreaterThan(1));Assert.That(model.Current.Frame,Is.SameAs(before));Assert.That(draft.ScienceUnits,Is.EqualTo(desired));
                CheckpointCapture(doc,"loading-responsive");state.Outcomes.Add(new{phase="blocked-load",ticks,frames,indicatorPositions=positions.Count});heartbeat.Pause();loadRelease.Set();timer.Restart();
                while(model.Busy){Assert.That(timer.Elapsed.TotalSeconds,Is.LessThan(90));yield return null;}yield return null;
                Assert.That(load.Thread,Is.EqualTo(mainThread));Assert.That(model.Current,Is.Not.SameAs(beforeController));Assert.That(model.HasLocalEdits,Is.False);
                Assert.That(codec.EncodeStaticAstronomyCampaignRoot(CampaignPrivateAudit(model)),Is.EqualTo(initial));
                Click(doc.rootVisualElement.Q<Button>("player-save"));var resave=CheckpointActivate(state,()=>Confirm(doc));timer.Restart();while(model.Busy){Assert.That(timer.Elapsed.TotalSeconds,Is.LessThan(90));yield return null;}
                yield return null;Assert.That(resave.Thread,Is.EqualTo(mainThread));Assert.That(File.ReadAllBytes(file2),Is.EqualTo(initial));CheckpointCapture(doc,"reloaded-byte-exact");
                // Disable while an old atomic write is already underway; a new independently
                // owned model/store Load stays busy until its own worker is released.
                entered.Reset();release.Reset();writeProbe=s=>{if(s==UnityReviewWriteStage.AfterPartialWrite){entered.Set();if(!release.Wait(TimeSpan.FromSeconds(90)))throw new TimeoutException();}};
                Click(doc.rootVisualElement.Q<Button>("player-save"));var old=CheckpointActivate(state,()=>Confirm(doc));timer.Restart();while(!entered.IsSet){Assert.That(timer.Elapsed.TotalSeconds,Is.LessThan(90));yield return null;}
                entry.enabled=false;Assert.That(model.Busy,Is.False);entry.enabled=true;yield return null;yield return null;
                var nextStorePath=Path.Combine(directory,"new-owner");var nextStore=new ReviewStaticAstronomyCampaignSlotStore(nextStorePath,codec);nextStore.Write("generated-1",codec.DecodeStaticAstronomyCampaignRoot(initial),false);
                var next=ReviewUiModel.ContinueStaticAstronomyCampaign(template,codec,nextStore,"generated-1");Publish(next);yield return null;yield return null;CampaignPage(doc,ReviewWorkspacePage.Saves);
                var nextEntered=new ManualResetEventSlim();var nextRelease=new ManualResetEventSlim();state.Releases.Add(nextRelease);var nextGate=typeof(ReviewStaticAstronomyCampaignSlotStore).GetField("gate",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(nextStore)!;
                var nextHolder=Task.Run(()=>{lock(nextGate){nextEntered.Set();if(!nextRelease.Wait(TimeSpan.FromSeconds(90)))throw new TimeoutException();}});state.Holders.Add(nextHolder);timer.Restart();while(!nextEntered.IsSet){Assert.That(timer.Elapsed.TotalSeconds,Is.LessThan(90));yield return null;}
                Click(doc.rootVisualElement.Q<Button>("player-load"));var newer=CheckpointActivate(state,()=>Confirm(doc));var newerOperation=CheckpointField(entry,"checkpointUiOperation");var newerToken=CheckpointField(entry,"campaignBusyToken");
                release.Set();timer.Restart();while(old.Finished==0){Assert.That(timer.Elapsed.TotalSeconds,Is.LessThan(90));yield return null;}
                Assert.That(next.Busy,Is.True);Assert.That(CheckpointField(entry,"checkpointUiOperation"),Is.SameAs(newerOperation));Assert.That(CheckpointField(entry,"checkpointOwner"),Is.SameAs(next));Assert.That(CheckpointField(entry,"campaignBusyToken"),Is.SameAs(newerToken));
                yield return null;CheckpointBusy(doc,"Loading");CheckpointCapture(doc,"stale-finally-kept-new-busy");nextRelease.Set();timer.Restart();while(next.Busy){Assert.That(timer.Elapsed.TotalSeconds,Is.LessThan(90));yield return null;}
                yield return null;Assert.That(newer.Thread,Is.EqualTo(mainThread));Assert.That(doc.rootVisualElement.Q("campaign-operation-busy"),Is.Null);
                Assert.That(codec.EncodeStaticAstronomyCampaignRoot(CampaignPrivateAudit(next)),Is.EqualTo(initial));Assert.That(Directory.GetFiles(CampaignOutput(),"checkpoint-*.png").Length,Is.EqualTo(expectedCaptures));
                state.Outcomes.Add(new{phase="complete",dirtyDraftSavePreserved=true,failedLoadAtomic=true,occupiedCancel=true,racedFilePreserved=true,reloadBytesExact=true,staleFinallyRetainedNewOwner=true,mainThread});
            }
        }
    }
}
