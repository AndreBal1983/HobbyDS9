#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        private const string CycleCombatFixtureSha="964641d0e1010d66ed87287d8a025d2716167c03f224e69c47a6a85b2c2d5890";

        [UnityTest,Timeout(720000)]
        public IEnumerator ColonisationCycleSavedCombatAftermathAndWholeTypedContinue()
        {
            var t=new CycleReceivingState();var s=t.Case;var stack=new Stack<IEnumerator>();
            stack.Push(CycleCombatBody(t));Exception? failure=null;
            while(stack.Count>0)
            {
                bool next=false;object? value=null;var current=stack.Peek();
                try
                {
                    if(t.Productive.Elapsed.TotalSeconds>=600)throw new TimeoutException("Cycle combat deadline before "+t.Phase);
                    next=current.MoveNext();if(next)value=current.Current;CycleDiscover(t);
                    if(t.Productive.Elapsed.TotalSeconds>=600)throw new TimeoutException("Cycle combat deadline after "+t.Phase);
                    if(t.Flushed<s.Shared.Timings.Count)t.Flush();
                }
                catch(Exception error)
                {
                    failure=error;
                    try{CycleDiscover(t);}catch(Exception ownershipError){try{CampaignDiagnosticError("cycle-combat-ownership",ownershipError);}catch{}}
                }
                if(failure!=null)break;
                if(!next){stack.Pop();try{(current as IDisposable)?.Dispose();}catch(Exception error){failure=error;break;}continue;}
                if(value is IEnumerator nested){stack.Push(nested);continue;}
                yield return value;
            }
            t.Productive.Stop();
            void Cleanup(string phase,Action action)
            {try{action();}catch(Exception error){failure=failure??error;try{CampaignDiagnosticError(phase,error);}catch{}}}
            Cleanup("cycle-combat-release-all",()=>{var error=s.ReleaseAll();failure=failure??error;});
            Cleanup("cycle-combat-shared-release",()=>s.Shared.Release.Set());
            if(s.Shared.Entry!=null)Cleanup("cycle-combat-disable",()=>s.Shared.Entry.enabled=false);
            while(stack.Count>0)try{(stack.Pop() as IDisposable)?.Dispose();}catch(Exception error){failure=failure??error;}
            bool settled=true;var drain=System.Diagnostics.Stopwatch.StartNew();
            bool Pending(){bool pending=true;Cleanup("cycle-combat-drain",()=>pending=CyclePending(t));return pending;}
            while(Pending())
            {
                if(drain.Elapsed.TotalSeconds>=90)
                {settled=false;directory=null;failure=failure??new TimeoutException("Cycle combat owners unsettled; preserve temporary override/namespace.");break;}
                yield return null;
            }
            Cleanup("cycle-combat-timings",t.Flush);bool overrideRestored=false;
            if(settled&&s.Shared.Entry!=null&&s.PriorDirectoryCaptured&&t.OverrideAssigned)
                Cleanup("cycle-combat-restore-override",()=>{typeof(ReviewEntry).GetField("generatedCampaignTestDirectory",HullPrivate)!.SetValue(s.Shared.Entry,s.Shared.PriorDirectory);overrideRestored=true;});
            bool directoryRetained=!settled||t.OverrideAssigned&&!overrideRestored;
            if(directoryRetained)directory=null;
            Cleanup("cycle-combat-cleanup-receipt",()=>File.WriteAllText(Path.Combine(CampaignOutput(),"cycle-combat-cleanup.json"),JsonConvert.SerializeObject(new{
                settled,overrideRestored,directoryRetained,priorOverrideCaptured=s.PriorDirectoryCaptured,overrideAssigned=t.OverrideAssigned,
                trackedModels=t.Models.Count,trackedHosts=s.Hosts.Count,trackedContexts=s.Contexts.Count,
                allReleasesSet=s.Holds.All(h=>h.Release.IsSet),entryDisabled=s.Shared.Entry==null||!s.Shared.Entry.enabled,
                productiveMs=t.Productive.Elapsed.TotalMilliseconds,cleanupMs=drain.Elapsed.TotalMilliseconds,
                productiveLimitSeconds=600,drainLimitSeconds=90,outerFrameworkSeconds=720,failed=failure!=null,lastPhase=t.Phase})));
            if(settled)foreach(var host in s.Hosts)Cleanup("cycle-combat-remove-probe",()=>ColonyProbe(host,null));
            if(!s.Pending)foreach(var h in s.Holds){Cleanup("cycle-combat-dispose-entered",()=>h.Entered.Dispose());Cleanup("cycle-combat-dispose-release",()=>h.Release.Dispose());}
            if(s.Shared.Holder==null||s.Shared.Holder.IsCompleted)
            {Cleanup("cycle-combat-dispose-shared-entered",()=>s.Shared.Entered.Dispose());Cleanup("cycle-combat-dispose-shared-release",()=>s.Shared.Release.Dispose());}
            yield return new ExitPlayMode();
            if(failure!=null)System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
        }

        private static string CycleCombatStocks(CampaignTacticalOwnShip ship)=>JsonConvert.SerializeObject(new{
            ship.Condition.Hull,Shields=ship.Condition.Shields.ToArray(),Weapons=ship.Weapons.OrderBy(w=>w.WeaponId,StringComparer.Ordinal).Select(w=>new{w.WeaponId,w.Ammunition}).ToArray()});
        private static IEnumerator CycleCombatClick(CycleReceivingState t,UIDocument doc,Button button,bool confirm=false)
        {
            Assert.That(button,Is.Not.Null);var parents=new List<ScrollView>();
            for(var at=button.parent;at!=null;at=at.parent)if(at is ScrollView scroll)parents.Add(scroll);
            foreach(var scroll in parents){scroll.ScrollTo(button);yield return null;yield return null;}
            Assert.That(button.panel,Is.SameAs(doc.rootVisualElement.panel));Assert.That(button.enabledInHierarchy,Is.True,button.text);
            foreach(var scroll in parents)PublishedReceivingContained(scroll.contentViewport.worldBound,button,button.text);
            var size=new Vector2Int(Mathf.RoundToInt(doc.rootVisualElement.worldBound.width),Mathf.RoundToInt(doc.rootVisualElement.worldBound.height));
            BattleVisible(button,size); // Combat actions may live in the footer; no content-above-footer assertion.
            CycleActivate(t,()=>{Click(button);if(confirm)Confirm(doc);});yield return ColonyIdle(t.Case,"cycle-combat-action-"+button.text);
        }

        private IEnumerator CycleCombatBody(CycleReceivingState t)
        {
            var s=t.Case;yield return new EnterPlayMode();yield return null;yield return null;
            var bytes=V3Fixture("HOBBYDS9_COLONISATION_CYCLE_COMBAT_WAITING",CycleCombatFixtureSha);
            Assert.That(bytes.Length,Is.EqualTo(1358829),"exact complete waiting fixture BEFORE decode");
            var codec=new ReviewNewtonsoftCodec();var fixture=codec.DecodeCycleRoot(bytes);Assert.That(codec.EncodeCycleRoot(fixture),Is.EqualTo(bytes));
            s.Shared.Entry=UnityEngine.Object.FindAnyObjectByType<ReviewEntry>();Assert.That(s.Shared.Entry,Is.Not.Null);var entry=s.Shared.Entry!;
            CycleDiscover(t);var field=typeof(ReviewEntry).GetField("generatedCampaignTestDirectory",HullPrivate)!;
            s.Shared.PriorDirectory=(string?)field.GetValue(entry);s.PriorDirectoryCaptured=true;
            directory=Path.Combine(Path.GetTempPath(),"HobbyDS9-Galaxy-Engine-Cycle-Combat-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(directory);
            var storePath=Path.Combine(directory,"campaign");field.SetValue(entry,storePath);t.OverrideAssigned=true;
            var resolved=(string)typeof(ReviewEntry).GetMethod("ColonisationCycleCampaignDirectory",HullPrivate)!.Invoke(entry,null)!;
            Assert.That(Path.GetFullPath(resolved),Is.EqualTo(Path.GetFullPath(storePath)));
            var store=new ReviewColonisationCycleSlotStore(resolved,codec);store.Write("generated-2",fixture,false);
            Assert.That(File.ReadAllBytes(Path.Combine(resolved,"unity-campaign-colonisation-cycle-v1-generated-2.json")),Is.EqualTo(bytes));
            var doc=entry.GetComponent<UIDocument>();
            using(var surface=new GalaxyInputSurface(doc))
            {
                var prior=ColonyModel(s);var oldController=prior.Current;var oldHost=StreamHost(prior);
                t.Mark("cycle-combat-fixture-continue-begin");
                doc.rootVisualElement.Q<Foldout>("colonisation-cycle-campaign-menu").value=true;yield return null;
                var load=doc.rootVisualElement.Q<Button>("colonisation-cycle-campaign-continue-generated-2");yield return ColonyReach(doc,load,target:"saved Cycle combat Continue2");
                yield return CycleOperation(t,doc,"cycle-combat-fixture-continue","Loading",()=>{Click(load);Confirm(doc);});
                var model=ColonyModel(s);var actor=model.Current.Frame.Actor;
                Assert.That(model,Is.Not.SameAs(prior));Assert.That(model.Current,Is.Not.SameAs(oldController));Assert.That(StreamHost(model),Is.Not.SameAs(oldHost));
                Assert.That(model.ColonisationCycleCampaign,Is.True);Assert.That(actor,Is.EqualTo("civ.federation"));
                Assert.That(model.Current.Frame.Status.Turn,Is.EqualTo(69));Assert.That(model.Current.Frame.Status.Phase,Is.EqualTo(SessionPhase.AwaitingCombat));
                Assert.That(CycleBytes(model),Is.EqualTo(bytes),"normal typed full replay ingress, not inner-state injection");
                File.WriteAllBytes(Path.Combine(CampaignOutput(),"cycle-combat-waiting-private-audit.json"),CycleBytes(model));
                // Complete roots are assertions ONLY, never choices for encounters, fleet orders or targets.
                var physical=CycleAudit(model).BaseCampaign!.Campaign!.Integrated!.Physical!;
                Assert.That(physical.Requests.Count,Is.EqualTo(1));Assert.That(model.Current.Frame.Encounters.Count,Is.EqualTo(1));
                foreach(var id in model.Current.Frame.Encounters)
                {
                    var request=physical.Requests.Single(r=>r.EncounterId==id);
                    Assert.That(request.Participants.Any(p=>p.CivilizationId==actor),Is.True);
                    Assert.That(request.Participants.Any(p=>physical.Core.Campaign.GameState.Civilizations!.Single(c=>c.CivilizationId==p.CivilizationId).ControllerKind==ControllerKind.AI),Is.True);
                    Assert.That(physical.Core.Battles.Any(b=>b.State.EncounterId==id&&b.State.Formations.Any(f=>f.SideId==actor)),Is.True);
                }
                CycleActivate(t,()=>CampaignPage(doc,ReviewWorkspacePage.Combat));yield return null;yield return null;
                Assert.That(model.Current.Frame.Battle,Is.Not.Null);var encounter=model.Current.Frame.Battle!.EncounterId;
                Assert.That(model.Current.Frame.Encounters,Does.Contain(encounter));
                var before=model.Current.Frame.Battle.OwnShips.ToDictionary(ship=>ship.Condition.ShipId,CycleCombatStocks,StringComparer.Ordinal);
                Assert.That(before,Is.Not.Empty);int rounds=0;bool captured=false;
                while(!model.Current.Frame.Battle!.Complete)
                {
                    Assert.That(++rounds,Is.LessThanOrEqualTo(3));Assert.That(model.Current.Frame.Actor,Is.EqualTo(actor));
                    Assert.That(model.Current.Frame.Battle.EncounterId,Is.EqualTo(encounter));
                    var order=model.Current.Frame.Battle.StandingOrders.Where(o=>o.CanOrder).ToArray();
                    Assert.That(order.Length,Is.EqualTo(1),"exact saved case has one actual human formation");
                    var maneuver=doc.rootVisualElement.Q<DropdownField>("combat-maneuver-picker");yield return ColonyReach(doc,maneuver);
                    int hold=maneuver.choices.IndexOf(CampaignFleetManeuver.Hold.ToString());Assert.That(hold,Is.GreaterThanOrEqualTo(0));
                    CycleActivate(t,()=>maneuver.index=hold);yield return null;
                    yield return CycleCombatClick(t,doc,CampaignButton(doc,"Apply to local draft"));
                    Assert.That(entry.Presentation!.CanCommitRound,Is.True,"actual AI commitment needs no actor switch");
                    var round=model.Current.Frame.Battle.Round;t.Mark("cycle-combat-round-"+round);
                    yield return CycleCombatClick(t,doc,CampaignButton(doc,"Review / commit round "+round),true);
                    Assert.That(model.Current.Frame.Actor,Is.EqualTo(actor));Assert.That(entry.Presentation.PendingPlayback,Is.Not.Null);
                    var accepted=CycleBytes(model);var boundary=model.Current.Frame;
                    if(!captured)
                    {
                        yield return CycleCombatClick(t,doc,doc.rootVisualElement.Q<Button>("combat-tab-battle"));
                        yield return BattleSized(entry,doc,new Vector2Int(1920,1080),"cycle-combat-playback",true);
                        Assert.That(CycleBytes(model),Is.EqualTo(accepted),"actual scene capture is non-authoritative");captured=true;
                    }
                    yield return CycleCombatClick(t,doc,doc.rootVisualElement.Q<Button>("combat-skip-pending-playback"));
                    Assert.That(model.Current.Frame,Is.SameAs(boundary));Assert.That(CycleBytes(model),Is.EqualTo(accepted),"playback acknowledgment changes no campaign state");
                    if(!model.Current.Frame.Battle!.Complete)yield return CycleCombatClick(t,doc,CampaignButton(doc,"Plan next round"));
                }
                Assert.That(captured,Is.True);Assert.That(entry.Presentation!.CanFinalize,Is.True);
                var staged=model.Current.Frame.Battle!.OwnShips.ToDictionary(ship=>ship.Condition.ShipId,ship=>(Hull:ship.Condition.Hull,Stocks:CycleCombatStocks(ship)),StringComparer.Ordinal);
                Assert.That(staged,Is.Not.Empty);Assert.That(staged.Keys.All(before.ContainsKey),Is.True,"known own baseline for every staged ship");
                Assert.That(staged.Any(p=>before[p.Key]!=p.Value.Stocks),Is.True,"harmless encounter is not a consequence proof");
                t.Mark("cycle-combat-finalize");yield return CycleCombatClick(t,doc,CampaignButton(doc,"Confirm campaign aftermath"),true);
                Assert.That(model.Current.Frame.Status.Phase,Is.EqualTo(SessionPhase.Planning));Assert.That(model.Current.Frame.Status.Turn,Is.EqualTo(70));
                var stocks=model.Current.Frame.OwnShips.ToDictionary(ship=>ship.Condition.ShipId,CycleCombatStocks,StringComparer.Ordinal);
                foreach(var p in staged)if(p.Value.Hull==0)Assert.That(stocks.ContainsKey(p.Key),Is.False);else Assert.That(stocks[p.Key],Is.EqualTo(p.Value.Stocks));
                var aftermath=CycleBytes(model);File.WriteAllBytes(Path.Combine(CampaignOutput(),"cycle-combat-aftermath-private-audit.json"),aftermath);
                var after=CycleAudit(model);var actual=after.BaseCampaign!.Campaign!.Integrated!.Physical!;
                Assert.That(actual.Requests,Is.Empty);Assert.That(actual.Core.Battles,Is.Empty);Assert.That(actual.Phase,Is.EqualTo(SessionPhase.Planning));
                Assert.That(after.BaseCampaign.Colonisation!.LastSettlementTurn,Is.EqualTo(69));Assert.That(after.BaseCampaign.LastSettlementProof!.Turn,Is.EqualTo(69));
                Assert.That(entry.Presentation.FinalizeAftermath(true),Is.False,"PUBLIC FLOW/CONTROLLER guard rejection only; not a dispatched simulation/replay rejection");
                Assert.That(CycleBytes(model),Is.EqualTo(aftermath));
                var survivor=model.Current.Frame.OwnStrategicFleets.OrderBy(f=>f.FleetId.Value,StringComparer.Ordinal).FirstOrDefault();
                Assert.That(survivor,Is.Not.Null,"no-survivor remains failed narrower evidence");
                CycleActivate(t,()=>CampaignPage(doc,ReviewWorkspacePage.FleetOrders));yield return null;yield return null;
                var fleetButton=doc.rootVisualElement.Q<Button>("fleet-roster-"+survivor!.FleetId.Value);
                if(!fleetButton.ClassListContains("selected-nav"))yield return CycleCombatClick(t,doc,fleetButton);
                yield return CycleCombatClick(t,doc,doc.rootVisualElement.Q<Button>("fleet-clear-route"),true);
                Assert.That(model.Current.Frame.Travel!.OwnOrders.Single(o=>o.FleetId==survivor.FleetId.Value).Intent,Is.EqualTo(CampaignTravelIntent.Stay));
                Assert.That(model.Current.Frame.Status.Turn,Is.EqualTo(70));Assert.That(model.Current.Frame.Actor,Is.EqualTo(actor));
                Assert.That(JsonConvert.SerializeObject(model.Current.Frame.OwnShips.ToDictionary(ship=>ship.Condition.ShipId,CycleCombatStocks,StringComparer.Ordinal)),Is.EqualTo(JsonConvert.SerializeObject(stocks)));
                var committed=CycleBytes(model);var committedRoot=CycleAudit(model);
                Assert.That(committed.SequenceEqual(aftermath),Is.False,"fresh accepted Stay has a real command receipt");
                File.WriteAllBytes(Path.Combine(CampaignOutput(),"cycle-combat-after-fresh-order-private-audit.json"),committed);
                CycleActivate(t,()=>CampaignPage(doc,ReviewWorkspacePage.Saves));yield return null;yield return null;
                CycleActivate(t,()=>Click(doc.rootVisualElement.Q<Button>("save-slot-generated-1")));t.Mark("cycle-combat-save");
                yield return CycleOperation(t,doc,"cycle-combat-save","Saving",()=>Click(doc.rootVisualElement.Q<Button>("player-save")));
                Assert.That(File.ReadAllBytes(Path.Combine(resolved,"unity-campaign-colonisation-cycle-v1-generated-1.json")),Is.EqualTo(committed));
                prior=model;oldController=model.Current;oldHost=StreamHost(model);
                CycleActivate(t,()=>Click(doc.rootVisualElement.Q<Button>("demo-menu")));yield return null;yield return null;
                var continued=doc.rootVisualElement.Q<Button>("colonisation-cycle-campaign-continue-generated-1");yield return ColonyReach(doc,continued);
                t.Mark("cycle-combat-final-continue");yield return CycleOperation(t,doc,"cycle-combat-final-continue","Loading",()=>{Click(continued);Confirm(doc);});
                model=ColonyModel(s);Assert.That(model,Is.Not.SameAs(prior));Assert.That(model.Current,Is.Not.SameAs(oldController));Assert.That(StreamHost(model),Is.Not.SameAs(oldHost));
                Assert.That(model.Current.Frame.Actor,Is.EqualTo(actor));Assert.That(model.Current.Frame.Status.Turn,Is.EqualTo(70));Assert.That(CycleBytes(model),Is.EqualTo(committed),"WHOLE14 normal Continue BEFORE any fresh week");
                Assert.That(JsonConvert.SerializeObject(CycleAudit(model).AiPlans),Is.EqualTo(JsonConvert.SerializeObject(committedRoot.AiPlans)));
                Assert.That(JsonConvert.SerializeObject(CycleAudit(model).CycleActors),Is.EqualTo(JsonConvert.SerializeObject(committedRoot.CycleActors)));
                Assert.That(JsonConvert.SerializeObject(model.Current.Frame.OwnShips.ToDictionary(ship=>ship.Condition.ShipId,CycleCombatStocks,StringComparer.Ordinal)),Is.EqualTo(JsonConvert.SerializeObject(stocks)));
                Assert.That(model.Current.Frame.Travel!.OwnOrders.Single(o=>o.FleetId==survivor.FleetId.Value).Intent,Is.EqualTo(CampaignTravelIntent.Stay));
                File.WriteAllBytes(Path.Combine(CampaignOutput(),"cycle-combat-continued-private-audit.json"),CycleBytes(model));
                Assert.That(V3Fixture("HOBBYDS9_COLONISATION_CYCLE_COMBAT_WAITING",CycleCombatFixtureSha),Is.EqualTo(bytes));
                t.Mark("cycle-combat-complete");File.WriteAllText(Path.Combine(CampaignOutput(),"cycle-combat-summary.json"),JsonConvert.SerializeObject(new{
                    actor,fixtureSha=CycleCombatFixtureSha,fixtureBytes=bytes.Length,waitingTurn=69,aftermathTurn=70,ordinaryAdvances=0,rounds,stagedShips=staged.Count,
                    whole14IngressEqual=true,whole14SaveContinueEqual=true,newModel=true,newController=true,newHost=true,
                    repeatedFinalizeProof="flow/controller rejection only",image="battle-v2-cycle-combat-playback-1920x1080.png",
                    qualification="Saved supported30seed42 branch, not native250 or a fresh69-week journey. Actual resource values are audited; historical CPU damage is not assumed. No performance/universal-audio/full-goal claim."}));
            }
        }
    }
}
