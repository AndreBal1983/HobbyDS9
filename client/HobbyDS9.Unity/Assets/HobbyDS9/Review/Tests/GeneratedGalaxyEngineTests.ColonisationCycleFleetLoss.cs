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
        private const string CycleFleetLossFixtureSha="5c43035a16ddddac2236a4363c583b0d49e06e9f263eea069bc529b605883105";

        [UnityTest,Timeout(720000)]
        public IEnumerator ColonisationCycleSavedFleetLossPlanetOrderAndWholeTypedContinue()
        {
            var t=new CycleReceivingState();var s=t.Case;var stack=new Stack<IEnumerator>();
            stack.Push(CycleFleetLossBody(t));Exception? failure=null;
            while(stack.Count>0)
            {
                bool next=false;object? value=null;var current=stack.Peek();
                try
                {
                    if(t.Productive.Elapsed.TotalSeconds>=600)throw new TimeoutException("Cycle fleet-loss deadline before "+t.Phase);
                    next=current.MoveNext();if(next)value=current.Current;CycleDiscover(t);
                    if(t.Productive.Elapsed.TotalSeconds>=600)throw new TimeoutException("Cycle fleet-loss deadline after "+t.Phase);
                    if(t.Flushed<s.Shared.Timings.Count)t.Flush();
                }
                catch(Exception error)
                {
                    failure=error;
                    try{CycleDiscover(t);}catch(Exception ownershipError){try{CampaignDiagnosticError("cycle-fleet-loss-ownership",ownershipError);}catch{}}
                }
                if(failure!=null)break;
                if(!next){stack.Pop();try{(current as IDisposable)?.Dispose();}catch(Exception error){failure=error;break;}continue;}
                if(value is IEnumerator nested){stack.Push(nested);continue;}
                yield return value;
            }
            t.Productive.Stop();
            void Cleanup(string phase,Action action)
            {try{action();}catch(Exception error){failure=failure??error;try{CampaignDiagnosticError(phase,error);}catch{}}}
            Cleanup("cycle-fleet-loss-release-all",()=>{var error=s.ReleaseAll();failure=failure??error;});
            Cleanup("cycle-fleet-loss-shared-release",()=>s.Shared.Release.Set());
            if(s.Shared.Entry!=null)Cleanup("cycle-fleet-loss-disable",()=>s.Shared.Entry.enabled=false);
            while(stack.Count>0)try{(stack.Pop() as IDisposable)?.Dispose();}catch(Exception error){failure=failure??error;}
            bool settled=true;var drain=System.Diagnostics.Stopwatch.StartNew();
            bool Pending(){bool pending=true;Cleanup("cycle-fleet-loss-drain",()=>pending=CyclePending(t));return pending;}
            while(Pending())
            {
                if(drain.Elapsed.TotalSeconds>=90)
                {settled=false;directory=null;failure=failure??new TimeoutException("Cycle fleet-loss owners unsettled; preserve temporary override/namespace.");break;}
                yield return null;
            }
            Cleanup("cycle-fleet-loss-timings",t.Flush);bool overrideRestored=false;
            if(settled&&s.Shared.Entry!=null&&s.PriorDirectoryCaptured&&t.OverrideAssigned)
                Cleanup("cycle-fleet-loss-restore-override",()=>{typeof(ReviewEntry).GetField("generatedCampaignTestDirectory",HullPrivate)!.SetValue(s.Shared.Entry,s.Shared.PriorDirectory);overrideRestored=true;});
            bool directoryRetained=!settled||t.OverrideAssigned&&!overrideRestored;
            if(directoryRetained)directory=null;
            Cleanup("cycle-fleet-loss-cleanup-receipt",()=>File.WriteAllText(Path.Combine(CampaignOutput(),"cycle-fleet-loss-cleanup.json"),JsonConvert.SerializeObject(new{
                settled,overrideRestored,directoryRetained,priorOverrideCaptured=s.PriorDirectoryCaptured,overrideAssigned=t.OverrideAssigned,
                trackedModels=t.Models.Count,trackedHosts=s.Hosts.Count,trackedContexts=s.Contexts.Count,
                allReleasesSet=s.Holds.All(h=>h.Release.IsSet),entryDisabled=s.Shared.Entry==null||!s.Shared.Entry.enabled,
                productiveMs=t.Productive.Elapsed.TotalMilliseconds,cleanupMs=drain.Elapsed.TotalMilliseconds,
                productiveLimitSeconds=600,drainLimitSeconds=90,outerFrameworkSeconds=720,failed=failure!=null,lastPhase=t.Phase})));
            if(settled)foreach(var host in s.Hosts)Cleanup("cycle-fleet-loss-remove-probe",()=>ColonyProbe(host,null));
            if(!s.Pending)foreach(var h in s.Holds){Cleanup("cycle-fleet-loss-dispose-entered",()=>h.Entered.Dispose());Cleanup("cycle-fleet-loss-dispose-release",()=>h.Release.Dispose());}
            if(s.Shared.Holder==null||s.Shared.Holder.IsCompleted)
            {Cleanup("cycle-fleet-loss-dispose-shared-entered",()=>s.Shared.Entered.Dispose());Cleanup("cycle-fleet-loss-dispose-shared-release",()=>s.Shared.Release.Dispose());}
            yield return new ExitPlayMode();
            if(failure!=null)System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
        }

        private static IEnumerator CycleFleetLossEncounter(CycleReceivingState t,UIDocument doc,string id)
        {
            var model=ColonyModel(t.Case);var index=model.Current.Frame.Encounters.ToList().IndexOf(id);
            Assert.That(index,Is.GreaterThanOrEqualTo(0));
            var rail=doc.rootVisualElement.Q<ScrollView>("central-fleet-rail");Assert.That(rail,Is.Not.Null);
            if(rail.resolvedStyle.display==DisplayStyle.None)
            {
                var details=doc.rootVisualElement.Q<Button>("combat-toggle-fleet-details");
                Assert.That(details,Is.Not.Null);Assert.That(details.text,Is.EqualTo("Fleet details"));
                var beforeDetails=CycleBytes(model);yield return CycleCombatClick(t,doc,details);
                Assert.That(CycleBytes(model),Is.EqualTo(beforeDetails),"public fleet-details toggle is cosmetic");
            }
            rail=doc.rootVisualElement.Q<ScrollView>("central-fleet-rail");Assert.That(rail,Is.Not.Null);
            Assert.That(rail.resolvedStyle.display,Is.Not.EqualTo(DisplayStyle.None));
            var picker=doc.rootVisualElement.Q<DropdownField>("owned-encounter-picker");
            yield return ColonyReach(doc,picker);Assert.That(index,Is.LessThan(picker.choices.Count));
            CycleActivate(t,()=>picker.index=index);yield return null;
            yield return CycleCombatClick(t,doc,CampaignButton(doc,"Open selected encounter"),true);
            Assert.That(model.Current.Frame.Battle!.EncounterId,Is.EqualTo(id));
        }

        private IEnumerator CycleFleetLossBody(CycleReceivingState t)
        {
            var s=t.Case;yield return new EnterPlayMode();yield return null;yield return null;
            var bytes=V3Fixture("HOBBYDS9_COLONISATION_CYCLE_FLEET_LOSS_WAITING",CycleFleetLossFixtureSha);
            Assert.That(bytes.Length,Is.EqualTo(1353722),"exact genuine unreinforced waiting fixture BEFORE decode");
            var codec=new ReviewNewtonsoftCodec();var fixture=codec.DecodeCycleRoot(bytes);
            Assert.That(codec.EncodeCycleRoot(fixture),Is.EqualTo(bytes));
            s.Shared.Entry=UnityEngine.Object.FindAnyObjectByType<ReviewEntry>();Assert.That(s.Shared.Entry,Is.Not.Null);var entry=s.Shared.Entry!;
            CycleDiscover(t);var field=typeof(ReviewEntry).GetField("generatedCampaignTestDirectory",HullPrivate)!;
            s.Shared.PriorDirectory=(string?)field.GetValue(entry);s.PriorDirectoryCaptured=true;
            directory=Path.Combine(Path.GetTempPath(),"HobbyDS9-Galaxy-Engine-Cycle-Fleet-Loss-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(directory);
            var storePath=Path.Combine(directory,"campaign");field.SetValue(entry,storePath);t.OverrideAssigned=true;
            var resolved=(string)typeof(ReviewEntry).GetMethod("ColonisationCycleCampaignDirectory",HullPrivate)!.Invoke(entry,null)!;
            Assert.That(Path.GetFullPath(resolved),Is.EqualTo(Path.GetFullPath(storePath)));
            var store=new ReviewColonisationCycleSlotStore(resolved,codec);store.Write("generated-2",fixture,false);
            Assert.That(File.ReadAllBytes(Path.Combine(resolved,"unity-campaign-colonisation-cycle-v1-generated-2.json")),Is.EqualTo(bytes));
            var doc=entry.GetComponent<UIDocument>();
            using(var surface=new GalaxyInputSurface(doc))
            {
                var prior=ColonyModel(s);var oldController=prior.Current;var oldHost=StreamHost(prior);
                doc.rootVisualElement.Q<Foldout>("colonisation-cycle-campaign-menu").value=true;yield return null;
                var load=doc.rootVisualElement.Q<Button>("colonisation-cycle-campaign-continue-generated-2");yield return ColonyReach(doc,load);
                yield return CycleOperation(t,doc,"cycle-fleet-loss-ingress","Loading",()=>{Click(load);Confirm(doc);});
                var model=ColonyModel(s);var actor=model.Current.Frame.Actor;
                Assert.That(model,Is.Not.SameAs(prior));Assert.That(model.Current,Is.Not.SameAs(oldController));Assert.That(StreamHost(model),Is.Not.SameAs(oldHost));
                Assert.That(actor,Is.EqualTo("civ.federation"));Assert.That(model.ColonisationCycleCampaign,Is.True);
                Assert.That(model.Current.Frame.Status.Turn,Is.EqualTo(69));Assert.That(model.Current.Frame.Status.Phase,Is.EqualTo(SessionPhase.AwaitingCombat));
                Assert.That(CycleBytes(model),Is.EqualTo(bytes),"normal whole14 replay ingress");
                File.WriteAllBytes(Path.Combine(CampaignOutput(),"cycle-fleet-loss-waiting-private-audit.json"),CycleBytes(model));
                // Full roots authenticate outcomes only; all action choices come from current own public observations.
                var physical=CycleAudit(model).BaseCampaign!.Campaign!.Integrated!.Physical!;
                var encounters=model.Current.Frame.Encounters.OrderBy(x=>x,StringComparer.Ordinal).ToArray();Assert.That(encounters,Is.Not.Empty);
                var before=new Dictionary<string,string>(StringComparer.Ordinal);var ownedFleets=new HashSet<string>(StringComparer.Ordinal);
                CycleActivate(t,()=>CampaignPage(doc,ReviewWorkspacePage.Combat));yield return null;yield return null;
                // Complete lawful own baseline across ALL encounters before any round, never absent=>damage.
                foreach(var id in encounters)
                {
                    yield return CycleFleetLossEncounter(t,doc,id);
                    var battle=model.Current.Frame.Battle!;Assert.That(battle.ActorId,Is.EqualTo(actor));
                    var request=physical.Requests.Single(r=>r.EncounterId==id);
                    Assert.That(request.Participants.Any(p=>p.CivilizationId==actor),Is.True);
                    Assert.That(request.Participants.Any(p=>physical.Core.Campaign.GameState.Civilizations!.Single(c=>c.CivilizationId==p.CivilizationId).ControllerKind==ControllerKind.AI),Is.True);
                    Assert.That(physical.Core.Battles.Any(b=>b.State.EncounterId==id),Is.True);
                    foreach(var ship in battle.OwnShips)
                    {
                        var combatStocks=CycleCombatStocks(ship);if(before.TryGetValue(ship.Condition.ShipId,out var known))Assert.That(combatStocks,Is.EqualTo(known));
                        else before.Add(ship.Condition.ShipId,combatStocks);
                        ownedFleets.Add(ship.Condition.FleetId);
                    }
                }
                Assert.That(before,Is.Not.Empty);Assert.That(ownedFleets,Is.Not.Empty);
                var staged=new Dictionary<string,long>(StringComparer.Ordinal);int rounds=0;
                foreach(var id in encounters)
                {
                    yield return CycleFleetLossEncounter(t,doc,id);
                    int inEncounter=0;
                    while(!model.Current.Frame.Battle!.Complete)
                    {
                        Assert.That(++inEncounter,Is.LessThanOrEqualTo(3));rounds++;var battle=model.Current.Frame.Battle;
                        Assert.That(model.Current.Frame.Actor,Is.EqualTo(actor));Assert.That(battle.EncounterId,Is.EqualTo(id));
                        var orders=battle.StandingOrders.Where(o=>o.CanOrder).ToArray();Assert.That(orders,Has.Length.EqualTo(1),"actual saved case has one own formation");
                        var maneuver=doc.rootVisualElement.Q<DropdownField>("combat-maneuver-picker");yield return ColonyReach(doc,maneuver);
                        int hold=battle.LegalManeuvers.ToList().IndexOf(CampaignFleetManeuver.Hold);
                        Assert.That(hold,Is.GreaterThanOrEqualTo(0));Assert.That(hold,Is.LessThan(maneuver.choices.Count));
                        Assert.That(maneuver.choices[hold],Is.EqualTo(ReviewDemoCombatText.Maneuver(CampaignFleetManeuver.Hold.ToString())));
                        CycleActivate(t,()=>maneuver.index=hold);yield return null;
                        yield return CycleCombatClick(t,doc,CampaignButton(doc,"Apply to local draft"));
                        Assert.That(model.Current.Draft.Single(d=>d.Fleet==orders[0].FleetId).Maneuver,Is.EqualTo(CampaignFleetManeuver.Hold));
                        Assert.That(entry.Presentation!.CanCommitRound,Is.True,"real AI commitment without actor switch");
                        yield return CycleCombatClick(t,doc,CampaignButton(doc,"Review / commit round "+battle.Round),true);
                        Assert.That(entry.Presentation.PendingPlayback,Is.Not.Null);var accepted=CycleBytes(model);var boundary=model.Current.Frame;
                        yield return CycleCombatClick(t,doc,doc.rootVisualElement.Q<Button>("combat-skip-pending-playback"));
                        Assert.That(model.Current.Frame,Is.SameAs(boundary));Assert.That(CycleBytes(model),Is.EqualTo(accepted));
                        if(!model.Current.Frame.Battle!.Complete)yield return CycleCombatClick(t,doc,CampaignButton(doc,"Plan next round"));
                    }
                    foreach(var ship in model.Current.Frame.Battle!.OwnShips)
                    {
                        Assert.That(before.ContainsKey(ship.Condition.ShipId),Is.True,"known staged own participant");
                        if(staged.TryGetValue(ship.Condition.ShipId,out var hull))Assert.That(ship.Condition.Hull,Is.EqualTo(hull));
                        else staged.Add(ship.Condition.ShipId,ship.Condition.Hull);
                    }
                }
                Assert.That(staged,Is.Not.Empty);Assert.That(staged.Keys.OrderBy(x=>x,StringComparer.Ordinal),Is.EqualTo(before.Keys.OrderBy(x=>x,StringComparer.Ordinal)));
                Assert.That(entry.Presentation!.CanFinalize,Is.True);yield return CycleCombatClick(t,doc,CampaignButton(doc,"Confirm campaign aftermath"),true);
                var aftermath=CycleBytes(model);File.WriteAllBytes(Path.Combine(CampaignOutput(),"cycle-fleet-loss-aftermath-private-audit.json"),aftermath);
                // Preserve real after-finalize root BEFORE asserting the historical loss recurs.
                Assert.That(staged.Values.All(h=>h==0),Is.True,"survival is honest narrower failed evidence, never force loss");
                Assert.That(model.Current.Frame.Status.Turn,Is.EqualTo(70));Assert.That(model.Current.Frame.Status.Phase,Is.EqualTo(SessionPhase.Planning));
                Assert.That(model.Current.Frame.OwnShips,Is.Empty);Assert.That(model.Current.Frame.OwnStrategicFleets,Is.Empty);
                var after=CycleAudit(model);var core=after.BaseCampaign!.Campaign!.Integrated!.Physical!;
                Assert.That(core.Core.Campaign.GameState.Ships!.Any(x=>x.OwnerCivilizationId==actor),Is.False);
                Assert.That(core.Core.Campaign.GameState.Fleets!.Any(x=>x.OwnerCivilizationId==actor),Is.False);
                Assert.That(core.Core.Pools.Any(p=>before.ContainsKey(p.ShipId)),Is.False);Assert.That(core.Orders.Any(o=>ownedFleets.Contains(o.FleetId)),Is.False);
                Assert.That(core.Requests,Is.Empty);Assert.That(core.Core.Battles,Is.Empty);Assert.That(core.Phase,Is.EqualTo(SessionPhase.Planning));
                Assert.That(after.BaseCampaign.Colonisation!.LastSettlementTurn,Is.EqualTo(69));Assert.That(after.BaseCampaign.LastSettlementProof!.Turn,Is.EqualTo(69));
                Assert.That(entry.Presentation.Stage,Is.EqualTo(ReviewCombatStage.NoOwnedFleet));Assert.That(entry.Presentation.CampaignComplete,Is.False);
                Assert.That(entry.Presentation.FinalizeAftermath(true),Is.False,"flow/controller guard only, not dispatched simulation rejection");
                Assert.That(entry.Presentation.DraftNextOrder(ownedFleets.OrderBy(x=>x,StringComparer.Ordinal).First(),CampaignTravelIntent.Stay),Is.False,"no-fleet flow guard");
                Assert.That(CycleBytes(model),Is.EqualTo(aftermath));
                string? worldId=null;ActivityBuildOptionDto? chosen=null;
                foreach(var world in model.Colonisation().Worlds.OrderBy(w=>w.Id,StringComparer.Ordinal))
                {
                    if(world.ConstructionUnavailableReason(false)!=null)continue;
                    foreach(var option in model.ColonisationActivities().BuildOptions.Where(o=>o.Kind==ActivityProductionKind.Infrastructure&&o.Supported).OrderBy(o=>o.DefinitionId,StringComparer.Ordinal))
                    {
                        if(model.ColonisationActivities().Structures.Any(q=>q.ColonyId==world.Id&&q.DefinitionId==option.DefinitionId)
                            ||model.Colonisation().Production.Any(q=>q.ColonyId==world.Id&&q.DefinitionId==option.DefinitionId))continue;
                        var stamp=model.Current.Frame.Status;
                        var eligible=model.ActivityEligibility(new CampaignActivityCommandDto{ActorId=actor,ExpectedTurn=stamp.Turn,ExpectedRevision=stamp.CampaignRevision,
                            CommandId="command.fleet-loss.eligibility."+Guid.NewGuid().ToString("N"),Kind=CampaignActivityCommandKind.EnqueueStructure,TargetId=world.Id,DefinitionId=option.DefinitionId,BlueprintRevision=option.BlueprintRevision});
                        if(!eligible.Accepted)continue;worldId=world.Id;chosen=option;break;
                    }
                    if(chosen!=null)break;
                }
                Assert.That(chosen,Is.Not.Null,"no legal post-loss infrastructure is narrower FAILED evidence; no grant/fallback");
                yield return CycleWorld(t,doc,worldId!);
                var observation=model.ColonisationActivities();var visible=model.Colonisation();
                var ordered=observation.BuildOptions.Where(o=>o.Kind==ActivityProductionKind.PublishedShip).Concat(observation.BuildOptions.Where(o=>o.Kind==ActivityProductionKind.Infrastructure)).ToArray();
                int index=Array.FindIndex(ordered,o=>o.DefinitionId==chosen!.DefinitionId&&o.Kind==chosen.Kind&&o.BlueprintRevision==chosen.BlueprintRevision);
                Assert.That(index,Is.GreaterThanOrEqualTo(0));Assert.That(doc.rootVisualElement.Q<DropdownField>("colonisation-world-selector").index,Is.EqualTo(visible.Worlds.ToList().FindIndex(w=>w.Id==worldId)));
                var add=doc.rootVisualElement.Q<Button>("colonisation-build-"+index);Assert.That(add,Is.Not.Null);
                Assert.That(add.text,Is.EqualTo("+ Add "+chosen!.DisplayName));Assert.That(add.enabledInHierarchy,Is.True);
                var frame=model.Current.Frame;var eligibility=model.ActivityEligibility(new CampaignActivityCommandDto{ActorId=actor,ExpectedTurn=frame.Status.Turn,ExpectedRevision=frame.Status.CampaignRevision,
                    CommandId="command.fleet-loss.final-eligibility."+Guid.NewGuid().ToString("N"),Kind=CampaignActivityCommandKind.EnqueueStructure,TargetId=worldId!,DefinitionId=chosen.DefinitionId,BlueprintRevision=chosen.BlueprintRevision});
                Assert.That(eligibility.Accepted,Is.True);Assert.That(eligibility.Turn,Is.EqualTo(frame.Status.Turn));Assert.That(eligibility.Revision,Is.EqualTo(frame.Status.CampaignRevision));
                var priorIds=visible.Production.Select(q=>q.Id).ToHashSet(StringComparer.Ordinal);var credits=frame.Strategy!.Civilization.Credits;
                var stocks=JsonConvert.SerializeObject(visible.Worlds.Single(w=>w.Id==worldId).Stocks);
                yield return CycleCombatClick(t,doc,add); // Actual current Cycle callback submits '+' immediately ONCE; no direct SubmitActivity.
                var queued=model.Colonisation().Production.Where(q=>!priorIds.Contains(q.Id)).ToArray();Assert.That(queued,Has.Length.EqualTo(1));
                Assert.That(model.Colonisation().Production.Count,Is.EqualTo(priorIds.Count+1));var item=queued[0];
                Assert.That(item.ColonyId,Is.EqualTo(worldId));Assert.That(item.DefinitionId,Is.EqualTo(chosen.DefinitionId));Assert.That(item.CompletedIndustry,Is.Zero);
                Assert.That(item.Consumed.All(r=>r.Amount==0),Is.True);Assert.That(model.Current.Frame.Strategy!.Civilization.Credits,Is.EqualTo(credits));
                Assert.That(JsonConvert.SerializeObject(model.Colonisation().Worlds.Single(w=>w.Id==worldId).Stocks),Is.EqualTo(stocks));
                Assert.That(model.Current.Frame.Status.CampaignRevision,Is.GreaterThan(frame.Status.CampaignRevision));Assert.That(model.Current.Frame.Actor,Is.EqualTo(actor));Assert.That(model.Current.Frame.Status.Turn,Is.EqualTo(70));
                var committed=CycleBytes(model);Assert.That(committed.SequenceEqual(aftermath),Is.False);
                File.WriteAllBytes(Path.Combine(CampaignOutput(),"cycle-fleet-loss-planet-order-private-audit.json"),committed);
                yield return CycleCapture(t,doc,new Vector2Int(1920,1080),"cycle-fleet-loss-own-planet-1920.png");Assert.That(CycleBytes(model),Is.EqualTo(committed));
                CycleActivate(t,()=>CampaignPage(doc,ReviewWorkspacePage.Saves));yield return null;yield return null;
                CycleActivate(t,()=>Click(doc.rootVisualElement.Q<Button>("save-slot-generated-1")));
                yield return CycleOperation(t,doc,"cycle-fleet-loss-save","Saving",()=>Click(doc.rootVisualElement.Q<Button>("player-save")));
                Assert.That(File.ReadAllBytes(Path.Combine(resolved,"unity-campaign-colonisation-cycle-v1-generated-1.json")),Is.EqualTo(committed));
                prior=model;oldController=model.Current;oldHost=StreamHost(model);
                CycleActivate(t,()=>Click(doc.rootVisualElement.Q<Button>("demo-menu")));yield return null;yield return null;
                var continued=doc.rootVisualElement.Q<Button>("colonisation-cycle-campaign-continue-generated-1");yield return ColonyReach(doc,continued);
                yield return CycleOperation(t,doc,"cycle-fleet-loss-final-continue","Loading",()=>{Click(continued);Confirm(doc);});
                model=ColonyModel(s);Assert.That(model,Is.Not.SameAs(prior));Assert.That(model.Current,Is.Not.SameAs(oldController));Assert.That(StreamHost(model),Is.Not.SameAs(oldHost));
                Assert.That(CycleBytes(model),Is.EqualTo(committed),"WHOLE14 normal Continue BEFORE any fresh command/week");
                Assert.That(model.Current.Frame.Actor,Is.EqualTo(actor));Assert.That(model.Current.Frame.Status.Turn,Is.EqualTo(70));
                Assert.That(model.Current.Frame.OwnShips,Is.Empty);Assert.That(model.Current.Frame.OwnStrategicFleets,Is.Empty);
                Assert.That(model.Colonisation().Production.Single(q=>q.Id==item.Id).DefinitionId,Is.EqualTo(chosen.DefinitionId));
                Assert.That(entry.Presentation!.CampaignComplete,Is.False);File.WriteAllBytes(Path.Combine(CampaignOutput(),"cycle-fleet-loss-continued-private-audit.json"),CycleBytes(model));
                Assert.That(V3Fixture("HOBBYDS9_COLONISATION_CYCLE_FLEET_LOSS_WAITING",CycleFleetLossFixtureSha),Is.EqualTo(bytes));
                t.Mark("cycle-fleet-loss-complete");File.WriteAllText(Path.Combine(CampaignOutput(),"cycle-fleet-loss-summary.json"),JsonConvert.SerializeObject(new{
                    actor,fixtureSha=CycleFleetLossFixtureSha,fixtureBytes=bytes.Length,waitingTurn=69,aftermathTurn=70,ordinaryAdvances=0,rounds,knownOwnedParticipants=before.Count,stagedShips=staged.Count,
                    world=worldId,queue=item.Id,definition=chosen.DefinitionId,whole14IngressEqual=true,whole14SaveContinueEqual=true,newModel=true,newController=true,newHost=true,
                    qualification="Genuine unreinforced saved30 case; actual total-loss and public legal infrastructure asserted, never assumed. Failed historical second execution stays failed. Flow-guard rejection only; no native/performance/full-goal claim."}));
            }
        }
    }
}
