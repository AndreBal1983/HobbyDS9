#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        private const string NetworkMaterialFixtureSha="18b05f6fbed37d1b40b39b868fafef7c164ee93dd414e123d1bfbcbcbac3f9e8";
        private const string NetworkMaterialQueue="queue-item.spawn.0000000000000032";
        private const string NetworkMaterialImage="network-material-completed-1920.png";
        private sealed class NetworkMaterialCase
        {
            internal readonly RepeatReceivingState Trace=new RepeatReceivingState();
            internal ColonyCase Case=>Trace.Case;
            internal string Phase="setup";
            internal int Flushed;
            internal bool OverrideAssigned;
            internal void Flush()
            {
                while(Flushed<Case.Shared.Timings.Count)
                {
                    File.AppendAllText(Path.Combine(CampaignOutput(),"network-material-timings.jsonl"),
                        JsonConvert.SerializeObject(Case.Shared.Timings[Flushed])+Environment.NewLine);
                    Flushed++;
                }
            }
            internal void Mark(string phase)
            {
                Phase=phase;
                File.AppendAllText(Path.Combine(CampaignOutput(),"network-material-phases.jsonl"),
                    JsonConvert.SerializeObject(new{phase,productiveMs=Trace.Productive.Elapsed.TotalMilliseconds})+Environment.NewLine);
                Flush();
            }
        }

        // Saved native branch only: no state is changed after normal typed Continue ingress.
        //720 is600 productive +90 cooperative drain + scene-exit margin, not extra gameplay time.
        [UnityTest,Timeout(720000)]
        public IEnumerator ColonisationNetworkMaterialSavedQueueCompletesAndWholeTypedContinue()
        {
            var t=new NetworkMaterialCase();var s=t.Case;var stack=new Stack<IEnumerator>();
            stack.Push(NetworkMaterialBody(t));Exception? failure=null;
            while(stack.Count>0)
            {
                bool next=false;object? value=null;var current=stack.Peek();
                try
                {
                    if(t.Trace.Productive.Elapsed.TotalSeconds>=600)throw new TimeoutException("Network material productive deadline before "+t.Phase);
                    next=current.MoveNext();if(next)value=current.Current;
                    if(t.Trace.Productive.Elapsed.TotalSeconds>=600)throw new TimeoutException("Network material productive deadline after "+t.Phase);
                    if(t.Flushed<s.Shared.Timings.Count)t.Flush();
                }
                catch(Exception error){failure=error;}
                if(failure!=null)break;
                if(!next){stack.Pop();try{(current as IDisposable)?.Dispose();}catch(Exception error){failure=error;break;}continue;}
                if(value is IEnumerator nested){stack.Push(nested);continue;}
                yield return value;
            }
            t.Trace.Productive.Stop();
            void Cleanup(string phase,Action action)
            {try{action();}catch(Exception error){failure=failure??error;try{CampaignDiagnosticError(phase,error);}catch{}}}
            Cleanup("network-material-release-all",()=>{var error=s.ReleaseAll();failure=failure??error;});
            Cleanup("network-material-shared-release",()=>s.Shared.Release.Set());
            if(s.Shared.Entry!=null)Cleanup("network-material-disable",()=>s.Shared.Entry.enabled=false);
            while(stack.Count>0)try{(stack.Pop() as IDisposable)?.Dispose();}catch(Exception error){failure=failure??error;}
            bool settled=true;
            bool Pending()
            {
                if(s.Pending||s.Shared.Holder?.IsCompleted==false)return true;
                bool pending=true;Cleanup("network-material-drain-status",()=>pending=s.Shared.Entry!=null&&ColonyPending(s.Shared.Entry));return pending;
            }
            var drain=System.Diagnostics.Stopwatch.StartNew();
            while(Pending())
            {
                if(drain.Elapsed.TotalSeconds>=90)
                {
                    settled=false;directory=null;
                    failure=failure??new TimeoutException("Network material worker unsettled; temporary namespace/override retained until scene exit.");break;
                }
                yield return null;
            }
            Cleanup("network-material-final-timings",t.Flush);
            bool overrideRestored=false;
            if(settled&&s.Shared.Entry!=null&&s.PriorDirectoryCaptured)
                Cleanup("network-material-restore-override",()=>{typeof(ReviewEntry).GetField("generatedCampaignTestDirectory",HullPrivate)!.SetValue(s.Shared.Entry,s.Shared.PriorDirectory);overrideRestored=true;});
            Cleanup("network-material-cleanup-receipt",()=>File.WriteAllText(Path.Combine(CampaignOutput(),"network-material-cleanup.json"),JsonConvert.SerializeObject(new{
                settled,allReleasesSet=s.Holds.All(h=>h.Release.IsSet),priorOverrideCaptured=s.PriorDirectoryCaptured,
                overrideAssigned=t.OverrideAssigned,overrideRestored,directoryRetained=!settled,entryDisabled=s.Shared.Entry==null||!s.Shared.Entry.enabled,
                cleanupMs=drain.Elapsed.TotalMilliseconds,productiveMs=t.Trace.Productive.Elapsed.TotalMilliseconds,
                productiveLimitSeconds=600,drainLimitSeconds=90,outerFrameworkSeconds=720,failed=failure!=null,lastPhase=t.Phase})));
            if(settled)foreach(var host in s.Hosts)Cleanup("network-material-remove-probe",()=>ColonyProbe(host,null));
            if(!s.Pending)foreach(var h in s.Holds){Cleanup("network-material-dispose-entered",()=>h.Entered.Dispose());Cleanup("network-material-dispose-release",()=>h.Release.Dispose());}
            if(s.Shared.Holder==null||s.Shared.Holder.IsCompleted)
            {Cleanup("network-material-dispose-shared-entered",()=>s.Shared.Entered.Dispose());Cleanup("network-material-dispose-shared-release",()=>s.Shared.Release.Dispose());}
            yield return new ExitPlayMode();
            if(failure!=null)System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
        }

        private static long NetworkMaterialStock(ColonisationWorldSnapshot world,string resource)
            =>world.Stocks.Where(r=>r.ResourceDefinitionId==resource).Sum(r=>r.Amount);
        private static long NetworkMaterialConsumed(ColonisationQueueSnapshot queue)
        {
            Assert.That(queue.RequiredIndustry,Is.EqualTo(60));Assert.That(queue.Required.Count,Is.EqualTo(1));
            Assert.That(queue.Required[0].ResourceDefinitionId,Is.EqualTo("resource.dilithium"));Assert.That(queue.Required[0].Amount,Is.EqualTo(6));
            Assert.That(queue.Consumed.Count,Is.EqualTo(1));Assert.That(queue.Consumed[0].ResourceDefinitionId,Is.EqualTo("resource.dilithium"));
            var consumed=queue.Consumed[0].Amount;Assert.That(consumed,Is.EqualTo((6*queue.CompletedIndustry+59)/60));return consumed;
        }
        private static void NetworkMaterialCompletion(CampaignColonisationRepeatSaveDto root,string actor,string world,long initialKits)
        {
            var state=root.BaseCampaign!;
            Assert.That(state.Production!.Any(q=>q.QueueItemId==NetworkMaterialQueue),Is.False);
            var history=state.ProductionHistory!.Where(h=>h.Order!.QueueItemId==NetworkMaterialQueue).ToArray();Assert.That(history.Length,Is.EqualTo(1));
            var receipt=history[0];var order=receipt.Order!;
            Assert.That(receipt.Completed,Is.True);Assert.That(order.OwnerId,Is.EqualTo(actor));Assert.That(order.ColonyId,Is.EqualTo(world));
            Assert.That(order.Kind,Is.EqualTo(ColonisationProductionKind.SettlementKit));Assert.That(order.DefinitionId,Is.EqualTo("resource.settlement-kit"));
            Assert.That(order.RequiredIndustry,Is.EqualTo(60));Assert.That(order.CompletedIndustry,Is.EqualTo(60));
            Assert.That(order.RequiredResources!.Count,Is.EqualTo(1));Assert.That(order.RequiredResources[0].ResourceDefinitionId,Is.EqualTo("resource.dilithium"));Assert.That(order.RequiredResources[0].Amount,Is.EqualTo(6));
            Assert.That(JsonConvert.SerializeObject(order.ConsumedResources),Is.EqualTo(JsonConvert.SerializeObject(order.RequiredResources)));
            var own=ColonyGame(state).Colonies!.Single(c=>c.ColonyId==world&&c.OwnerCivilizationId==actor);
            Assert.That(own.Stockpiles!.Where(r=>r.ResourceDefinitionId=="resource.settlement-kit").Sum(r=>r.Amount),Is.EqualTo(initialKits+1));
        }
        private IEnumerator NetworkMaterialBody(NetworkMaterialCase t)
        {
            var s=t.Case;yield return new EnterPlayMode();yield return null;yield return null;
            var bytes=V3Fixture("HOBBYDS9_COLONISATION_REPEAT_NETWORK_PARTIAL",NetworkMaterialFixtureSha);
            Assert.That(bytes.Length,Is.EqualTo(2636128),"exact immutable native fixture size BEFORE decoding");
            var codec=new ReviewNewtonsoftCodec();var fixture=codec.DecodeRepeatRoot(bytes);
            Assert.That(codec.EncodeRepeatRoot(fixture),Is.EqualTo(bytes));
            s.Shared.Entry=UnityEngine.Object.FindAnyObjectByType<ReviewEntry>();Assert.That(s.Shared.Entry,Is.Not.Null);var entry=s.Shared.Entry!;
            var field=typeof(ReviewEntry).GetField("generatedCampaignTestDirectory",HullPrivate)!;
            s.Shared.PriorDirectory=(string?)field.GetValue(entry);s.PriorDirectoryCaptured=true;
            directory=Path.Combine(Path.GetTempPath(),"HobbyDS9-Galaxy-Engine-Network-Materials-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(directory);
            var storePath=Path.Combine(directory,"campaign");field.SetValue(entry,storePath);t.OverrideAssigned=true;
            Assert.That(field.GetValue(entry),Is.EqualTo(storePath));
            var resolved=(string)typeof(ReviewEntry).GetMethod("GeneratedCampaignDirectory",HullPrivate)!.Invoke(entry,new object[]{false,false,false,false,false,false,false,false,true})!;
            Assert.That(Path.GetFullPath(resolved),Is.EqualTo(Path.GetFullPath(storePath)),"actual Repeat override-resolved path equals fixture store");
            var store=new ReviewColonisationRepeatSlotStore(resolved,codec);store.Write("generated-2",fixture,false);
            Assert.That(File.ReadAllBytes(Path.Combine(resolved,"unity-campaign-colonisation-repeat-v1-generated-2.json")),Is.EqualTo(bytes));
            var doc=entry.GetComponent<UIDocument>();
            using(var surface=new GalaxyInputSurface(doc))
            {
                var priorModel=ColonyModel(s);t.Mark("fixture-continue-begin");
                doc.rootVisualElement.Q<Foldout>("colonisation-repeat-campaign-menu").value=true;yield return null;
                var initial=doc.rootVisualElement.Q<Button>("colonisation-repeat-campaign-continue-generated-2");yield return ColonyReach(doc,initial,target:"saved Repeat material queue Continue2");
                yield return RepeatOperation(t.Trace,doc,"network-material-fixture-continue","Loading",()=>{Click(initial);Confirm(doc);});
                var model=ColonyModel(s);Assert.That(model,Is.Not.SameAs(priorModel));Assert.That(model.Current,Is.Not.SameAs(priorModel.Current));Assert.That(StreamHost(model),Is.Not.SameAs(StreamHost(priorModel)));
                Assert.That(model.ColonisationRepeatCampaign,Is.True);Assert.That(model.Current.Frame.Status.Turn,Is.EqualTo(31));Assert.That(RepeatBytes(model),Is.EqualTo(bytes),"normal typed full Replay ingress equals entire native fixture");
                var actor=model.Current.Frame.Actor;Assert.That(actor,Is.EqualTo("civ.federation"));
                // Selection uses public own production/world snapshots, never private plans or root target IDs.
                var queue=model.Colonisation().Production.Single(q=>q.Id==NetworkMaterialQueue);var world=model.Colonisation().Worlds.Single(w=>w.Id==queue.ColonyId);
                Assert.That(world.Stage,Is.EqualTo(ColonyEstablishmentStage.Established));Assert.That(queue.Kind,Is.EqualTo(ColonisationProductionKind.SettlementKit));Assert.That(queue.DefinitionId,Is.EqualTo("resource.settlement-kit"));
                Assert.That(queue.CompletedIndustry,Is.EqualTo(12));var consumed=NetworkMaterialConsumed(queue);Assert.That(consumed,Is.EqualTo(2));
                Assert.That(NetworkMaterialStock(world,"resource.dilithium"),Is.EqualTo(0));var initialKits=NetworkMaterialStock(world,"resource.settlement-kit");Assert.That(initialKits,Is.EqualTo(0));
                s.Home=world.Id;var controller=model.Current;var host=StreamHost(model);s.Hosts.Add(host);long work=queue.CompletedIndustry;bool intermediate=false,paidQuantum=false;
                t.Mark("fixture-continue-exact-complete");yield return ColonyWorld(s,doc,s.Home);
                while(ColonyModel(s).Colonisation().Production.Any(q=>q.Id==NetworkMaterialQueue))
                {
                    Assert.That(t.Trace.UiAdvances,Is.LessThan(20),"global ordinary UI Advance cap");
                    yield return RepeatAdvance(t.Trace,doc);model=ColonyModel(s);yield return ColonyWorld(s,doc,s.Home);
                    var current=model.Colonisation().Production.SingleOrDefault(q=>q.Id==NetworkMaterialQueue);
                    if(current!=null)
                    {
                        var actualWorld=model.Colonisation().Worlds.Single(w=>w.Id==s.Home);
                        Assert.That(current.CompletedIndustry,Is.InRange(work,Math.Min(60,work+Math.Max(0,actualWorld.Industry))));var nextConsumed=NetworkMaterialConsumed(current);
                        intermediate|=current.CompletedIndustry>12&&current.CompletedIndustry<60&&nextConsumed>=2&&nextConsumed<6;
                        paidQuantum|=current.CompletedIndustry>work&&nextConsumed==consumed;
                        work=current.CompletedIndustry;consumed=nextConsumed;
                    }
                    var audit=RepeatAudit(model); // Retained proof only; never an order/target input.
                    File.AppendAllText(Path.Combine(CampaignOutput(),"network-material-private-week-audit.jsonl"),JsonConvert.SerializeObject(new{
                        turn=model.Current.Frame.Status.Turn,queue=current,deliveries=ColonyGame(audit.BaseCampaign!).LastDeliveries!.Where(d=>d.DestinationPlanetId==world.PlanetId),
                        qualification="Own recipient audit after ordinary UI week; no every-week exclusive route claim or private AI action selection."})+Environment.NewLine);
                    t.Mark("completed-ui-week-"+model.Current.Frame.Status.Turn);
                }
                Assert.That(intermediate,Is.True);Assert.That(paidQuantum,Is.True,"a later work boundary did not charge an already consumed material quantum again");
                model=ColonyModel(s);var completed=RepeatAudit(model);NetworkMaterialCompletion(completed,actor,s.Home,initialKits);
                Assert.That(NetworkMaterialStock(model.Colonisation().Worlds.Single(w=>w.Id==s.Home),"resource.settlement-kit"),Is.EqualTo(initialKits+1));
                var expected=codec.EncodeRepeatRoot(completed);File.WriteAllBytes(Path.Combine(CampaignOutput(),"network-material-completed-private-audit.json"),expected);t.Mark("same-queue-completed");
                var saved=Path.Combine(resolved,"unity-campaign-colonisation-repeat-v1-generated-1.json");Assert.That(File.Exists(saved),Is.False);
                CampaignPage(doc,ReviewWorkspacePage.Saves);yield return null;Click(doc.rootVisualElement.Q<Button>("save-slot-generated-1"));
                yield return RepeatOperation(t.Trace,doc,"network-material-whole13-save","Saving",()=>Click(doc.rootVisualElement.Q<Button>("player-save")));
                Assert.That(File.ReadAllBytes(saved),Is.EqualTo(expected));Assert.That(RepeatBytes(ColonyModel(s)),Is.EqualTo(expected));t.Mark("normal-save-exact-complete");
                var beforeContinue=ColonyModel(s);Click(doc.rootVisualElement.Q<Button>("demo-menu"));yield return null;yield return null;
                var load=doc.rootVisualElement.Q<Button>("colonisation-repeat-campaign-continue-generated-1");yield return ColonyReach(doc,load,target:"explicit completed Repeat Continue1");
                yield return RepeatOperation(t.Trace,doc,"network-material-whole13-continue","Loading",()=>{Click(load);Confirm(doc);});
                model=ColonyModel(s);Assert.That(model,Is.Not.SameAs(beforeContinue));Assert.That(model.Current,Is.Not.SameAs(controller));Assert.That(StreamHost(model),Is.Not.SameAs(host));Assert.That(model.Current.Frame.Actor,Is.EqualTo(actor));Assert.That(model.ColonisationRepeatCampaign,Is.True);
                var continued=RepeatAudit(model);Assert.That(RepeatBytes(model),Is.EqualTo(expected),"entire committed13 BEFORE any fresh post-Continue week");NetworkMaterialCompletion(continued,actor,s.Home,initialKits);
                Assert.That(JsonConvert.SerializeObject(continued.AiPlans),Is.EqualTo(JsonConvert.SerializeObject(completed.AiPlans)));Assert.That(JsonConvert.SerializeObject(continued.RepeatGoals),Is.EqualTo(JsonConvert.SerializeObject(completed.RepeatGoals)),"both goal collections and SupplyCampId preserved");
                File.WriteAllBytes(Path.Combine(CampaignOutput(),"network-material-continued-private-audit.json"),RepeatBytes(model));t.Mark("continue-exact-before-fresh-week");
                yield return ColonyWorld(s,doc,s.Home);yield return RepeatCapture(t.Trace,doc,new Vector2Int(1920,1080),NetworkMaterialImage);
                Assert.That(RepeatBytes(ColonyModel(s)),Is.EqualTo(expected),"one fresh own-view capture changes no root state");
                File.WriteAllText(Path.Combine(CampaignOutput(),"network-material-summary.json"),JsonConvert.SerializeObject(new{
                    fixtureSha256=NetworkMaterialFixtureSha,fixtureBytes=bytes.Length,fixtureWholeEqualAfterFullReplay=true,explicitRepeatContinue=true,
                    sameQueue=NetworkMaterialQueue,sameQueueCompleted60Consumed6=true,localKitDelta1=true,intermediate,paidQuantum,
                    uiAdvances=t.Trace.UiAdvances,maximumUiAdvances=20,whole13SaveContinueEqualBeforeFreshWeek=true,newModel=true,newController=true,newHost=true,sameHumanActor=true,
                    bothGoalCollectionsAndSupplyCampRetained=true,oneFresh1920CaptureReadOnly=true,capture=NetworkMaterialImage,
                    qualification="Actual saved native branch continued through ordinary human UI weeks. No fresh colony voyage/manual material cargo/native/performance acceptance."}));
                t.Mark("all-network-material-gates-complete");
            }
        }
    }
}
