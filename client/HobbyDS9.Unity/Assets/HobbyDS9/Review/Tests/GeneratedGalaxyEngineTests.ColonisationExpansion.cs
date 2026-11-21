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
        private static readonly string[] ExpansionImages={
            "expansion-01-continued-1280.png","expansion-02-continued-1920.png","expansion-03-continued-3440.png"};
        private sealed class ExpansionReceivingState
        {
            internal readonly ColonyCase Case=new ColonyCase(); // Reuse tracking/helpers only; no old-profile persistence helper is called.
            internal readonly System.Diagnostics.Stopwatch Productive=System.Diagnostics.Stopwatch.StartNew();
            internal bool SawQueued,SawActions,SawLoaded,SawFounding,SawEstablished;
            internal int Flushed,UiAdvances;
            internal string Phase="setup";
            internal void Flush()
            {
                // Inspecting the count is cheap; disk writes happen ONLY for new timing rows.
                while(Flushed<Case.Shared.Timings.Count)
                {
                    File.AppendAllText(Path.Combine(CampaignOutput(),"expansion-timings.jsonl"),
                        JsonConvert.SerializeObject(Case.Shared.Timings[Flushed])+Environment.NewLine);
                    Flushed++;
                }
            }
            internal void Mark(string phase)
            {
                Phase=phase;
                File.AppendAllText(Path.Combine(CampaignOutput(),"expansion-phases.jsonl"),
                    JsonConvert.SerializeObject(new{phase,productiveMs=Productive.Elapsed.TotalMilliseconds})+Environment.NewLine);
                Flush();
            }
        }

        // One explicit-menu expansion case; no original receiving test is changed or relabelled.
        //720 is ONLY600 productive +90 drain + scene-exit margin, never extra gameplay time.
        [UnityTest,Timeout(720000)]
        public IEnumerator ColonisationExpansionUsesExplicitMenuAndWholeTypedContinue()
        {
            var t=new ExpansionReceivingState();var s=t.Case;var stack=new Stack<IEnumerator>();
            stack.Push(ExpansionReceivingBody(t));Exception? failure=null;
            while(stack.Count>0)
            {
                bool next=false;object? value=null;var current=stack.Peek();
                try
                {
                    if(t.Productive.Elapsed.TotalSeconds>=600)throw new TimeoutException("Expansion productive deadline before "+t.Phase);
                    next=current.MoveNext();if(next)value=current.Current;
                    if(t.Productive.Elapsed.TotalSeconds>=600)throw new TimeoutException("Expansion productive deadline after "+t.Phase);
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
            Cleanup("expansion-release-all",()=>{var error=s.ReleaseAll();failure=failure??error;});
            Cleanup("expansion-shared-release",()=>s.Shared.Release.Set());
            if(s.Shared.Entry!=null)Cleanup("expansion-disable",()=>s.Shared.Entry.enabled=false);
            while(stack.Count>0)try{(stack.Pop() as IDisposable)?.Dispose();}catch(Exception error){failure=failure??error;}
            bool settled=true;
            bool Pending()
            {
                if(s.Pending||s.Shared.Holder?.IsCompleted==false)return true;
                bool pending=true;Cleanup("expansion-drain-status",()=>pending=s.Shared.Entry!=null&&ColonyPending(s.Shared.Entry));return pending;
            }
            var cleanupClock=System.Diagnostics.Stopwatch.StartNew();
            while(Pending())
            {
                if(cleanupClock.Elapsed.TotalSeconds>=90)
                {
                    settled=false;directory=null;
                    failure=failure??new TimeoutException("Expansion worker unsettled; temporary namespace/override retained until scene exit.");break;
                }
                yield return null;
            }
            Cleanup("expansion-final-timings",t.Flush);
            bool overrideRestored=false;
            if(settled&&s.Shared.Entry!=null&&s.PriorDirectoryCaptured)
                Cleanup("expansion-restore-override",()=>{typeof(ReviewEntry).GetField("generatedCampaignTestDirectory",HullPrivate)!.SetValue(s.Shared.Entry,s.Shared.PriorDirectory);overrideRestored=true;});
            Cleanup("expansion-cleanup-receipt",()=>File.WriteAllText(Path.Combine(CampaignOutput(),"expansion-cleanup.json"),JsonConvert.SerializeObject(new{
                settled,allReleasesSet=s.Holds.All(h=>h.Release.IsSet),priorOverrideCaptured=s.PriorDirectoryCaptured,
                overrideRestored,directoryRetained=!settled,entryDisabled=s.Shared.Entry==null||!s.Shared.Entry.enabled,
                cleanupMs=cleanupClock.Elapsed.TotalMilliseconds,productiveMs=t.Productive.Elapsed.TotalMilliseconds,
                productiveLimitSeconds=600,drainLimitSeconds=90,outerFrameworkSeconds=720,failed=failure!=null,lastPhase=t.Phase})));
            if(settled)foreach(var host in s.Hosts)Cleanup("expansion-remove-probe",()=>ColonyProbe(host,null));
            if(!s.Pending)foreach(var h in s.Holds){Cleanup("expansion-dispose-entered",()=>h.Entered.Dispose());Cleanup("expansion-dispose-release",()=>h.Release.Dispose());}
            if(s.Shared.Holder==null||s.Shared.Holder.IsCompleted)
            {Cleanup("expansion-dispose-shared-entered",()=>s.Shared.Entered.Dispose());Cleanup("expansion-dispose-shared-release",()=>s.Shared.Release.Dispose());}
            yield return new ExitPlayMode();
            if(failure!=null)System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
        }

        private static CampaignColonisationExpansionSaveDto ExpansionAudit(ReviewUiModel model)
            =>((HobbyDS9.Application.ICampaignColonisationExpansionSession)typeof(PhysicalReviewHost).GetField("session",HullPrivate)!.GetValue(StreamHost(model))!).ExportColonisationExpansionSave();
        private static byte[] ExpansionBytes(ReviewUiModel model)=>new ReviewNewtonsoftCodec().EncodeColonisationExpansionRoot(ExpansionAudit(model));
        private static void ExpansionCensus(ExpansionReceivingState t,CampaignColonisationExpansionSaveDto root)
        {
            // Private immutable audit ONLY. None of these IDs/facts select a human action or destination.
            var plans=root.AiPlans!;var actors=plans.Select(p=>p.ActorId).ToHashSet(StringComparer.Ordinal);var state=root.BaseCampaign!;
            var cargo=state.Colonisation!.ManifestRecords!.Where(m=>actors.Contains(m.OwnerId)).ToArray();
            bool queued=state.Production!.Any(q=>actors.Contains(q.OwnerId));
            bool loaded=cargo.Any(m=>m.Kind==SettlerManifestKind.Settlement&&m.Stage==SettlerManifestStage.Loaded&&m.Passengers!.Population==100);
            bool founding=state.Colonisation.CampSources!.Any(c=>c.Stage==ColonyEstablishmentStage.Founding&&plans.Any(p=>p.CampId==c.CampId));
            bool established=plans.Any(p=>p.EstablishedColonyId!=null);
            t.SawQueued|=queued;t.SawLoaded|=loaded;t.SawFounding|=founding;t.SawEstablished|=established;
            t.SawActions|=plans.Any(p=>p.LatestActions!.Any(a=>a.Accepted));
            File.AppendAllText(Path.Combine(CampaignOutput(),"expansion-private-week-audit.jsonl"),JsonConvert.SerializeObject(new{
                turn=ColonyGame(state).TurnNumber,queued,loaded,founding,established,
                plans=plans.Select(p=>new{p.ActorId,p.Stage,p.LastPlannedTurn,p.TransportShipId,p.ManifestId,p.CampId,p.EstablishedColonyId,accepted=p.LatestActions!.Count(a=>a.Accepted)}),
                qualification="Audit evidence only; never human command selection or player-visible knowledge."})+Environment.NewLine);
        }
        private static IEnumerator ExpansionBusy(ExpansionReceivingState t,UIDocument doc,ColonyHold hold,byte[] before,ReviewFrame frame)
        {
            var s=t.Case;InteractionPending(doc,"Applying");int ticks=0;var positions=new HashSet<float>();
            var pulse=doc.rootVisualElement.schedule.Execute(()=>ticks++).Every(10);var clock=System.Diagnostics.Stopwatch.StartNew();
            try
            {
                while(clock.Elapsed.TotalSeconds<.3){if(doc.rootVisualElement.Q("campaign-operation-indicator") is VisualElement indicator)positions.Add(indicator.style.left.value.value);yield return null;}
                Assert.That(ticks,Is.GreaterThanOrEqualTo(3));if(positions.Count>0)Assert.That(positions.Count,Is.GreaterThan(1));
                Assert.That(ColonyModel(s).Current.Frame,Is.SameAs(frame));Assert.That(ExpansionBytes(ColonyModel(s)),Is.EqualTo(before));
                var page=s.Shared.Entry!.Workspace!.Page;Click(doc.rootVisualElement.Q<Button>("review-nav-Map"));Assert.That(s.Shared.Entry.Workspace.Page,Is.EqualTo(page));
                Assert.That(ColonyModel(s).BeginColonisationInteraction(ColonisationInteractionKind.PlanningAdvance),Is.Null);
            }
            finally{pulse.Pause();}
        }
        private static IEnumerator ExpansionCapture(ExpansionReceivingState t,UIDocument doc,Vector2Int size,string filename)
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
        private IEnumerator ExpansionReceivingBody(ExpansionReceivingState t)
        {
            var s=t.Case;yield return new EnterPlayMode();yield return null;yield return null;
            s.Shared.Entry=UnityEngine.Object.FindAnyObjectByType<ReviewEntry>();Assert.That(s.Shared.Entry,Is.Not.Null);var entry=s.Shared.Entry!;
            var field=typeof(ReviewEntry).GetField("generatedCampaignTestDirectory",HullPrivate)!;
            s.Shared.PriorDirectory=(string?)field.GetValue(entry);s.PriorDirectoryCaptured=true;
            directory=Path.Combine(Path.GetTempPath(),"HobbyDS9-Galaxy-Engine-Expansion-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(directory);
            var storePath=Path.Combine(directory,"campaign");field.SetValue(entry,storePath);var doc=entry.GetComponent<UIDocument>();
            using(var surface=new GalaxyInputSurface(doc))
            {
                t.Mark("explicit-new-begin");doc.rootVisualElement.Q<TextField>("generated-campaign-seed").value="42";
                doc.rootVisualElement.Q<DropdownField>("generated-campaign-size").value="250";doc.rootVisualElement.Q<DropdownField>("generated-campaign-quiet").value="2.00\u00d7";
                var menu=doc.rootVisualElement.Q<Foldout>("colonisation-expansion-campaign-menu");Assert.That(menu,Is.Not.Null);Assert.That(menu.text,Is.EqualTo("Colonisation & Expansion"));Assert.That(menu.value,Is.True);
                var oldModel=ColonyModel(s);var start=doc.rootVisualElement.Q<Button>("colonisation-expansion-campaign-start");yield return ColonyReach(doc,start,target:"explicit new expansion campaign");
                ColonyActivate(s,()=>{Click(start);if(doc.rootVisualElement.Q("review-one-use-confirmation")!=null)Confirm(doc);});yield return ColonyIdle(s,"expansion-new250");
                Assert.That(ColonyModel(s),Is.Not.SameAs(oldModel));Assert.That(ColonyModel(s).ColonisationExpansionCampaign,Is.True);Assert.That(ColonyModel(s).ColonisationRosterCampaign,Is.False);
                var controller=ColonyModel(s).Current;var actor=controller.Frame.Actor;var first=ExpansionAudit(ColonyModel(s));
                Assert.That(first.FormatVersion,Is.EqualTo(12));Assert.That(first.PackageHash,Is.EqualTo("4acb4451ae0ae4eb0b7d748e169ad999b0e0ff6aefea251d12fc272e3efa7ca9"));
                Assert.That(first.BaseCampaign!.FrozenPlanets!.PolicyId,Is.EqualTo(CampaignColonisationExpansionIdentity.InventoryPolicy));
                Assert.That(first.BaseCampaign!.Map!.Settings!.Map!.Seed,Is.EqualTo("42"));Assert.That(first.BaseCampaign.Map.Settings.Map.SystemCount,Is.EqualTo(250));
                Assert.That(first.AiPlans,Is.Not.Empty);Assert.That(first.AiPlans!.All(p=>p.ActorId!=actor),Is.True);
                ExpansionCensus(t,first);t.Mark("explicit-new-complete");
                // Source world comes ONLY from human own observations, never AI state or audit roots.
                var home=ColonyModel(s).Colonisation().Worlds.Where(w=>w.Stage==ColonyEstablishmentStage.Established&&w.Population>0).OrderByDescending(w=>w.Industry).ThenBy(w=>w.Id,StringComparer.Ordinal).First();
                s.Home=home.Id;yield return ColonyWorld(s,doc,s.Home);
                var kit=doc.rootVisualElement.Q<Button>("colonisation-build-kit");yield return ColonyReach(doc,kit,target:"human kit remains available in expansion profile");
                var before=ExpansionBytes(ColonyModel(s));var frame=ColonyModel(s).Current.Frame;var credits=ColonyModel(s).ColonisationActivities().Credits;
                t.Mark("human-kit-held-begin");var held=ColonyStartHold(s,()=>Click(kit));yield return ColonyEntered(held);yield return ExpansionBusy(t,doc,held,before,frame);
                held.Release.Set();yield return ColonyIdle(s,"expansion-human-kit");ColonyProbe(StreamHost(ColonyModel(s)),null);
                Assert.That(ColonyModel(s).Current,Is.SameAs(controller));Assert.That(ColonyModel(s).Current.Frame.Actor,Is.EqualTo(actor));
                Assert.That(ColonyModel(s).ColonisationActivities().Credits,Is.EqualTo(credits),"ordinary streaming has no upfront normal-credit debit");
                var humanOrder=ColonyModel(s).Colonisation().Production.Single(q=>q.ColonyId==s.Home&&q.Kind==ColonisationProductionKind.SettlementKit).Id;
                t.Mark("human-kit-held-complete");
                CampaignColonisationExpansionSaveDto audit=first;
                for(int week=0;week<160&&!t.SawEstablished;week++)
                {
                    yield return ColonyAdvance(s,doc);t.UiAdvances++;Assert.That(ColonyModel(s).Current,Is.SameAs(controller));Assert.That(ColonyModel(s).Current.Frame.Actor,Is.EqualTo(actor));
                    audit=ExpansionAudit(ColonyModel(s));ExpansionCensus(t,audit);t.Mark("completed-ui-week-"+ColonyModel(s).Current.Frame.Status.Turn);
                }
                Assert.That(t.SawQueued&&t.SawActions&&t.SawLoaded&&t.SawFounding&&t.SawEstablished,Is.True,"actual AI queue/load/founding/first established colony within productive bound");
                Assert.That(audit.BaseCampaign!.PublishedBuiltShips!.Any(b=>audit.AiPlans!.Any(p=>p.ActorId==b.OwnerCivilizationId)&&b.BlueprintId!.Contains("galaxy-colony-transport")&&b.BlueprintRevision==1),Is.True);
                var ai= audit.AiPlans!.First(p=>p.EstablishedColonyId!=null);var camp=audit.BaseCampaign.Colonisation!.CampSources!.Single(c=>c.CampId==ai.CampId);
                Assert.That(camp.FedWeeks,Is.GreaterThanOrEqualTo(4));Assert.That(camp.CompletedIndustry,Is.EqualTo(16));Assert.That(camp.Deaths,Is.Zero);
                Assert.That(ColonyGame(audit.BaseCampaign).Colonies!.Single(c=>c.ColonyId==ai.EstablishedColonyId).Cohorts!.Sum(c=>c.Population),Is.EqualTo(100));
                var manifest=audit.BaseCampaign.Colonisation.ManifestRecords!.Single(m=>m.ManifestId==camp.ManifestId);Assert.That(manifest.PopulationTransferred,Is.EqualTo(100));Assert.That(manifest.EquipmentConsumed,Is.EqualTo(1));
                Assert.That(audit.BaseCampaign.ProductionHistory!.Count(h=>h.Completed&&h.Order!.QueueItemId==humanOrder),Is.EqualTo(1),"human ordinary kit also completes");
                // The AI body may remain unknown to the human. Never reveal/select it for a screenshot or command.
                Assert.That(ColonyModel(s).Colonisation().Worlds.Any(w=>w.Id==ai.EstablishedColonyId),Is.False);
                var expected=new ReviewNewtonsoftCodec().EncodeColonisationExpansionRoot(audit);var mission=JsonConvert.SerializeObject(ColonyModel(s).FrontierCommission());
                t.Mark("save-begin");CampaignPage(doc,ReviewWorkspacePage.Saves);yield return null;Click(doc.rootVisualElement.Q<Button>("save-slot-generated-1"));
                ColonyActivate(s,()=>{Click(doc.rootVisualElement.Q<Button>("player-save"));if(doc.rootVisualElement.Q("review-one-use-confirmation")!=null)Confirm(doc);});yield return ColonyIdle(s,"expansion-full12-save");
                var saved=Path.Combine(storePath,"unity-campaign-colonisation-expansion-v1-generated-1.json");Assert.That(File.ReadAllBytes(saved),Is.EqualTo(expected));
                File.WriteAllBytes(Path.Combine(CampaignOutput(),"expansion-established-saved-private-audit.json"),expected);t.Mark("save-complete");
                var prior=ColonyModel(s);Click(doc.rootVisualElement.Q<Button>("demo-menu"));yield return null;yield return null;
                var load=doc.rootVisualElement.Q<Button>("colonisation-expansion-campaign-continue-generated-1");yield return ColonyReach(doc,load,target:"explicit expansion Continue");t.Mark("continue-begin");
                ColonyActivate(s,()=>{Click(load);Confirm(doc);});yield return ColonyIdle(s,"expansion-full12-continue");
                Assert.That(ColonyModel(s),Is.Not.SameAs(prior));Assert.That(ColonyModel(s).Current,Is.Not.SameAs(controller));Assert.That(ColonyModel(s).Current.Frame.Actor,Is.EqualTo(actor));
                Assert.That(ColonyModel(s).ColonisationExpansionCampaign,Is.True);Assert.That(ExpansionBytes(ColonyModel(s)),Is.EqualTo(expected));
                Assert.That(JsonConvert.SerializeObject(ColonyModel(s).FrontierCommission()),Is.EqualTo(mission));
                Assert.That(doc.rootVisualElement.Q<Button>("colonisation-order-confirm"),Is.Null);t.Mark("continue-complete");
                yield return ColonyWorld(s,doc,s.Home);var unchanged=ExpansionBytes(ColonyModel(s));
                foreach(var size in new[]{new Vector2Int(1280,720),new Vector2Int(1920,1080),new Vector2Int(3440,1440)})
                {yield return ExpansionCapture(t,doc,size,ExpansionImages[size.x==1280?0:size.x==1920?1:2]);t.Mark("capture-"+size.x);}
                Assert.That(ExpansionBytes(ColonyModel(s)),Is.EqualTo(unchanged),"read-only resize/layout/capture never changes full outer campaign");
                Assert.That(Directory.GetFiles(CampaignOutput(),"expansion-0*-continued-*.png").Select(Path.GetFileName),Is.EquivalentTo(ExpansionImages));
                File.WriteAllText(Path.Combine(CampaignOutput(),"expansion-summary.json"),JsonConvert.SerializeObject(new{explicitMenu=true,systems=250,seed="42",uiAdvances=t.UiAdvances,finalTurn=ColonyModel(s).Current.Frame.Status.Turn,
                    t.SawQueued,t.SawActions,t.SawLoaded,t.SawFounding,t.SawEstablished,humanKitCompleted=true,whole12SaveContinueEqual=true,captures=ExpansionImages,
                    qualification="Actual AI facts from audit only; human commands and pictures use own lawful observations. Original failed15-image test remains unchanged."}));
                t.Mark("all-expansion-gates-complete");
            }
        }
    }
}
