#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
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
        // Receiver-independent lifetime facts: never discover fields through a destroyed Entry.
        private sealed class CycleLifecycleContext:SynchronizationContext
        {
            private readonly SynchronizationContext target;
            internal int Outstanding,Pending,Finished;
            internal Exception? Fault;
            internal readonly List<int> Threads=new List<int>();
            internal CycleLifecycleContext(SynchronizationContext target){this.target=target;}
            public override void OperationStarted(){Interlocked.Increment(ref Outstanding);target.OperationStarted();}
            public override void OperationCompleted(){Interlocked.Decrement(ref Outstanding);target.OperationCompleted();}
            public override void Post(SendOrPostCallback callback,object? state)
            {
                Interlocked.Increment(ref Pending);
                try{target.Post(value=>{
                    try{lock(Threads)Threads.Add(Thread.CurrentThread.ManagedThreadId);callback(value);}
                    catch(Exception error){Interlocked.CompareExchange(ref Fault,error,null);}
                    finally{Interlocked.Increment(ref Finished);Interlocked.Decrement(ref Pending);}
                },state);}
                catch(Exception error){Interlocked.CompareExchange(ref Fault,error,null);Interlocked.Decrement(ref Pending);throw;}
            }
            public override void Send(SendOrPostCallback callback,object? state)=>target.Send(callback,state);
            public override SynchronizationContext CreateCopy()=>this;
            internal bool Settled=>Volatile.Read(ref Outstanding)==0&&Volatile.Read(ref Pending)==0;
        }
        private sealed class CycleLifecycleState
        {
            internal GameObject? OwnedObject;
            internal ReviewEntry? OwnedEntry;
            internal ReviewUiModel? Model;
            internal PhysicalReviewHost? Host;
            internal GalaxyInputSurface? Surface;
            internal ReviewController? Controller;
            internal ReviewFrame? Frame;
            internal ReviewCheckpointOperation? Operation;
            internal readonly List<CycleLifecycleContext> Contexts=new List<CycleLifecycleContext>();
            internal readonly ManualResetEventSlim Entered=new ManualResetEventSlim(),Release=new ManualResetEventSlim();
            internal int Armed,HoldCount,WorkerThread,TimedOut,MainThreadViolation,BarrierExited;
            internal int MainThread;
            internal bool PriorCaptured,OverrideAssigned,DestroyedBeforeRelease;
            internal string? PriorOverride,Namespace;
            internal readonly System.Diagnostics.Stopwatch Productive=System.Diagnostics.Stopwatch.StartNew();
            internal bool Settled=>Contexts.All(c=>c.Settled)&&(Volatile.Read(ref HoldCount)==0||Volatile.Read(ref BarrierExited)!=0);
            internal void Observe(ReviewLoadDiagnosticRecord row)
            {
                if(row.Phase!=ReviewLoadPhase.DecodeParse||row.Event!=ReviewLoadEvent.Begin||Interlocked.CompareExchange(ref Armed,0,1)!=1)return;
                Interlocked.Increment(ref HoldCount);WorkerThread=Thread.CurrentThread.ManagedThreadId;Entered.Set();
                try
                {
                    if(WorkerThread==MainThread){Volatile.Write(ref MainThreadViolation,1);return;}
                    if(!Release.Wait(TimeSpan.FromSeconds(90)))Volatile.Write(ref TimedOut,1);
                }
                finally{Volatile.Write(ref BarrierExited,1);}
            }
        }
        private static void CycleLifecycleActivate(CycleLifecycleState s,Action activate)
        {
            var prior=SynchronizationContext.Current;Assert.That(prior,Is.Not.Null);
            var captured=new CycleLifecycleContext(prior!);s.Contexts.Add(captured); // BEFORE any throwing action.
            SynchronizationContext.SetSynchronizationContext(captured);
            try{activate();}
            finally
            {
                SynchronizationContext.SetSynchronizationContext(prior);
                // The only post-action discovery is while THIS owned Unity receiver is alive.
                if(s.OwnedEntry!=null)s.Operation=(ReviewCheckpointOperation?)CheckpointField(s.OwnedEntry,"checkpointUiOperation");
            }
        }
        [UnityTest,Timeout(720000)]
        public IEnumerator ColonisationCyclePendingLoadDestroyedReceiverCannotPublish()
        {
            var s=new CycleLifecycleState();var stack=new Stack<IEnumerator>();stack.Push(CycleLifecycleBody(s));Exception? failure=null;
            while(stack.Count>0)
            {
                var current=stack.Peek();bool more=false;object? value=null;
                try
                {
                    if(s.Productive.Elapsed.TotalSeconds>=600)throw new TimeoutException("Cycle lifecycle productive deadline before MoveNext.");
                    more=current.MoveNext();if(more)value=current.Current;
                    if(s.Productive.Elapsed.TotalSeconds>=600)throw new TimeoutException("Cycle lifecycle productive deadline after MoveNext.");
                }
                catch(Exception error){failure=error;break;}
                if(!more){stack.Pop();try{(current as IDisposable)?.Dispose();}catch(Exception error){failure=error;break;}continue;}
                if(value is IEnumerator nested){stack.Push(nested);continue;}yield return value;
            }
            s.Productive.Stop();s.Release.Set(); // All failure paths release before cleanup or disposal.
            void Cleanup(Action action){try{action();}catch(Exception error){failure=failure??error;}}
            if(s.Operation!=null&&s.Model!=null)Cleanup(()=>s.Model.AbandonStaticCheckpointOperation(s.Operation));
            if(s.Surface!=null)Cleanup(()=>{s.Surface.Dispose();s.Surface=null;});
            if(s.OwnedEntry!=null)Cleanup(()=>s.OwnedEntry.enabled=false);
            while(stack.Count>0)Cleanup(()=>{(stack.Pop() as IDisposable)?.Dispose();});
            var drain=System.Diagnostics.Stopwatch.StartNew();bool settled=true;
            while(!s.Settled)
            {
                if(drain.Elapsed.TotalSeconds>=90){settled=false;failure=failure??new TimeoutException("Lifecycle owners unsettled; namespace retained.");break;}
                yield return null;
            }
            // Leave another two UI frames for separately posted async-void exception callbacks.
            if(settled)
            {
                yield return null;yield return null;settled=s.Settled;
                if(!settled)failure=failure??new TimeoutException("Callbacks became pending during grace; preserve namespace and fail within original cleanup deadline.");
            }
            foreach(var context in s.Contexts)if(context.Fault!=null)failure=failure??context.Fault;
            bool restored=false,destroyed=s.OwnedEntry==null;
            if(settled&&s.OwnedEntry!=null&&s.PriorCaptured&&s.OverrideAssigned)
                Cleanup(()=>{typeof(ReviewEntry).GetField("generatedCampaignTestDirectory",HullPrivate)!.SetValue(s.OwnedEntry,s.PriorOverride);restored=true;});
            if(settled&&s.OwnedObject!=null){Cleanup(()=>UnityEngine.Object.Destroy(s.OwnedObject));yield return null;}
            bool retained=!settled||s.OverrideAssigned&&!destroyed&&!restored;
            if(retained)directory=null; // Inherited TearDown must never delete unsettled IO.
            Cleanup(()=>File.WriteAllText(Path.Combine(CampaignOutput(),"cycle-checkpoint-lifecycle-cleanup.json"),JsonConvert.SerializeObject(new{
                passed=failure==null,settled,retained,restored,destroyed,priorCaptured=s.PriorCaptured,overrideAssigned=s.OverrideAssigned,
                destroyedBeforeRelease=s.DestroyedBeforeRelease,holds=s.HoldCount,s.WorkerThread,s.MainThread,s.TimedOut,s.MainThreadViolation,
                productiveMs=s.Productive.Elapsed.TotalMilliseconds,drainMs=drain.Elapsed.TotalMilliseconds,
                contexts=s.Contexts.Select(c=>new{c.Outstanding,c.Pending,c.Finished,threads=c.Threads.ToArray(),fault=c.Fault?.GetType().Name}),
                scope="In-process receiver teardown only; not OS-exit liveness or native shutdown diagnosis"})));
            if(settled){s.Entered.Dispose();s.Release.Dispose();}
            yield return new ExitPlayMode();
            if(failure!=null)System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
        }
        private IEnumerator CycleLifecycleBody(CycleLifecycleState s)
        {
            yield return new EnterPlayMode();yield return null;yield return null;s.MainThread=Thread.CurrentThread.ManagedThreadId;
            // Create and retain this exact fixture object before AddComponent/OnEnable can throw.
            s.OwnedObject=new GameObject("Cycle pending-load owned receiver "+Guid.NewGuid().ToString("N"));s.OwnedObject.SetActive(false);
            s.OwnedEntry=s.OwnedObject.AddComponent<ReviewEntry>();var entry=s.OwnedEntry!;
            Assert.That(entry.gameObject,Is.SameAs(s.OwnedObject));
            Assert.That(SingleplayerDemoLaunchOptions.TryParse(new[]{"--hobbyds9-singleplayer-demo"},out var options),Is.True);entry.ConfigureDemo(options!);
            var directoryField=typeof(ReviewEntry).GetField("generatedCampaignTestDirectory",HullPrivate)!;
            s.PriorOverride=(string?)directoryField.GetValue(entry);s.PriorCaptured=true;
            directory=Path.Combine(Path.GetTempPath(),"HobbyDS9-Galaxy-Engine-Cycle-Lifecycle-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(directory);
            s.Namespace=Path.Combine(directory,"campaign");directoryField.SetValue(entry,s.Namespace);s.OverrideAssigned=true;
            var diagnostic=ReviewLoadDiagnostic.Create(s.Observe)!;var codec=new ReviewNewtonsoftCodec(diagnostic);
            var template=(CampaignPlayableFleetPackageDto)typeof(ReviewEntry).GetMethod("LoadPlayableFleetTemplate",HullPrivate)!.Invoke(entry,null)!;
            byte[] Resource(string folder,string name)=>Resources.Load<TextAsset>(folder+"/"+name).bytes;
            var activities=ActivitiesPlaytestOptions.CreatePackage(template,new[]{"catalogue","ready-classes","support","activities"}.Select(n=>Resource("ActivitiesV1",n)).ToArray(),codec);
            var colony=ColonisationPlaytestOptions.CreatePackage(activities,new[]{"catalogue","ready-classes","support","settlement"}.Select(n=>Resource("ColonisationV1",n)).ToArray(),codec);
            var roster=ColonisationPlaytestOptions.CreateRosterPackage(colony,Resource("ColonisationRosterV2","roster"),codec);
            var growth=ColonisationGrowthPlaytestOptions.CreatePackage(roster,Resource("ColonisationExpansionV1","planner"),codec);
            var package=ColonisationCyclePlaytestOptions.CreatePackage(growth,Resource("ColonisationCycleV1","cycle-policy"),codec);
            var packageBytes=JsonConvert.SerializeObject(package);var store=new ReviewColonisationCycleSlotStore(s.Namespace!,codec);
            s.Model=new ReviewUiModel(package,new(){Map=new(){Seed="42",SystemCount=30,EmptyRegionCount=30,Width=750000,Height=600000,
                HomeCivilizationDefinitionIds=new List<string>{"civilization.federation","civilization.cardassian"}}},codec,store);
            var model=s.Model!;s.Host=StreamHost(model);s.Controller=model.Current;s.Frame=model.Current.Frame;
            // Fixture wiring only: no UI tree/Build on the inactive receiver. Normal OnEnable builds once active.
            Assert.That(s.OwnedObject.activeSelf,Is.False);
            void Stage(string field,object? value)=>typeof(ReviewEntry).GetField(field,HullPrivate)!.SetValue(entry,value);
            var flow=new ReviewCombatFlow(model);Stage("model",model);Stage("flow",flow);Stage("workspace",new ReviewWorkspace(flow));
            Stage("slotName","generated-1");Stage("demoLanding",false);Stage("demoStarted",true);
            s.OwnedObject.SetActive(true);
            yield return null;yield return null;
            Assert.That(CampaignModel(entry),Is.SameAs(model));Assert.That(StreamHost(model),Is.SameAs(s.Host));
            Assert.That((string)typeof(ReviewEntry).GetMethod("ColonisationCycleCampaignDirectory",HullPrivate)!.Invoke(entry,null)!,Is.EqualTo(s.Namespace));
            var doc=entry.GetComponent<UIDocument>();
            s.Surface=new GalaxyInputSurface(doc);
            {
                CampaignPage(doc,ReviewWorkspacePage.Saves);yield return null;yield return null;
                CycleLifecycleActivate(s,()=>Click(doc.rootVisualElement.Q<Button>("player-save")));
                Assert.That(s.Operation,Is.Not.Null);
                while(!s.Contexts.Last().Settled||model.Busy)yield return null;
                Assert.That(s.Contexts.Last().Finished,Is.GreaterThan(0));Assert.That(model.Message,Is.EqualTo("Checkpoint result: None"));
                var bytes=CycleBytes(model);Assert.That(CycleAudit(model).FormatVersion,Is.EqualTo(14));var input=Path.Combine(s.Namespace!,"unity-campaign-colonisation-cycle-v1-generated-1.json");
                Assert.That(File.ReadAllBytes(input),Is.EqualTo(bytes));store.Write("generated-3",CycleAudit(model),false);
                var sentinel=Path.Combine(s.Namespace!,"unity-campaign-colonisation-cycle-v1-generated-3.json");var sentinelBytes=File.ReadAllBytes(sentinel);
                var draft=model.ResearchInputs();Assert.That(draft,Is.Not.Null);model.LocalDesignDirty=true;model.OrganizationName="retained lifecycle draft";
                var controller=model.Current;var frame=model.Current.Frame;// Capture actual field without assuming its integral type.
                var epochBefore=Convert.ToInt64(CheckpointField(entry,"viewEpoch"));
                Click(doc.rootVisualElement.Q<Button>("player-load"));Volatile.Write(ref s.Armed,1);
                CycleLifecycleActivate(s,()=>Confirm(doc));var operation=s.Operation;Assert.That(operation,Is.Not.Null);
                while(!s.Entered.IsSet)yield return null;
                Assert.That(Volatile.Read(ref s.HoldCount),Is.EqualTo(1));Assert.That(s.WorkerThread,Is.Not.EqualTo(s.MainThread));
                Assert.That(s.MainThreadViolation,Is.Zero);Assert.That(s.TimedOut,Is.Zero);Assert.That(model.Busy,Is.True);
                Assert.That(model.Current,Is.SameAs(controller));Assert.That(model.Current.Frame,Is.SameAs(frame));Assert.That(CycleBytes(model),Is.EqualTo(bytes));
                Assert.That(model.Close(true),Is.False);Assert.That(model.Closed,Is.False);
                s.Surface.Dispose();s.Surface=null; // Restore only the live owned document, before its destruction.
                entry.enabled=false;var epoch=Convert.ToInt64(CheckpointField(entry,"viewEpoch"));Assert.That(epoch,Is.GreaterThan(epochBefore));
                Assert.That(CheckpointField(entry,"checkpointUiOperation"),Is.Null);Assert.That(model.Busy,Is.False);
                UnityEngine.Object.Destroy(s.OwnedObject);while(s.OwnedEntry!=null)yield return null;
                s.DestroyedBeforeRelease=true;Assert.That(s.Release.IsSet,Is.False);s.Release.Set();
                // Never query Entry fields, scene discovery or replacement objects from this point onward.
                while(!s.Settled)yield return null;yield return null;yield return null;
                Assert.That(s.Contexts.All(c=>c.Fault==null),Is.True,"unexpected async callback fault captured");
                Assert.That(s.Contexts.Count,Is.EqualTo(2));Assert.That(s.Contexts.All(c=>c.Finished>0),Is.True);Assert.That(s.Contexts.SelectMany(c=>c.Threads).All(t=>t==s.MainThread),Is.True);
                Assert.That(s.TimedOut,Is.Zero);Assert.That(s.MainThreadViolation,Is.Zero);Assert.That(s.HoldCount,Is.EqualTo(1));Assert.That(s.BarrierExited,Is.EqualTo(1));
                Assert.That(model.Current,Is.SameAs(controller));Assert.That(model.Current.Frame,Is.SameAs(frame));Assert.That(StreamHost(model),Is.SameAs(s.Host));
                Assert.That(model.ResearchInputs(),Is.SameAs(draft));Assert.That(model.LocalDesignDirty,Is.True);Assert.That(model.OrganizationName,Is.EqualTo("retained lifecycle draft"));
                Assert.That(CycleBytes(model),Is.EqualTo(bytes));Assert.That(File.ReadAllBytes(input),Is.EqualTo(bytes));Assert.That(File.ReadAllBytes(sentinel),Is.EqualTo(sentinelBytes));
                Assert.That(Directory.GetFiles(s.Namespace!,"*.tmp"),Is.Empty);Assert.That(JsonConvert.SerializeObject(package),Is.EqualTo(packageBytes));
                var fresh=ReviewUiModel.ContinueColonisationCycle(package,codec,store,"generated-1");
                Assert.That(fresh.Current,Is.Not.SameAs(controller));Assert.That(StreamHost(fresh),Is.Not.SameAs(s.Host));
                Assert.That(fresh.Current.Frame.Actor,Is.EqualTo(frame.Actor));Assert.That(CycleBytes(fresh),Is.EqualTo(bytes));
                Assert.That(File.ReadAllBytes(input),Is.EqualTo(bytes));Assert.That(File.ReadAllBytes(sentinel),Is.EqualTo(sentinelBytes));
                Assert.That(JsonConvert.SerializeObject(package),Is.EqualTo(packageBytes));
                File.WriteAllText(Path.Combine(CampaignOutput(),"cycle-checkpoint-lifecycle-results.json"),JsonConvert.SerializeObject(new{
                    passed=true,smallOfficial30Fixture=true,normalSaveAndLoad=true,zeroAdvance=true,destroyedReceiverBeforeRelease=true,
                    whole14InputAndSentinelExact=true,newHostControllerContinue=true,callerDraftsRetained=true,holds=s.HoldCount,
                    mainThread=s.MainThread,workerThread=s.WorkerThread,lateContinuations=s.Contexts.Sum(c=>c.Finished),
                    claim="In-process pending Cycle load receiver teardown, not native process shutdown liveness"}));
            }
        }
    }
}
