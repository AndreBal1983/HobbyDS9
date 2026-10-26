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
        private static readonly string[] FoodImages={
            "food-01-continued-1280.png","food-02-continued-1920.png","food-03-continued-3440.png"};
        private sealed class FoodReceivingState
        {
            internal readonly ColonyCase Case=new ColonyCase(); // Reuse tracking/helpers only; no old-profile persistence helper is called.
            internal readonly System.Diagnostics.Stopwatch Productive=System.Diagnostics.Stopwatch.StartNew();
            internal int Flushed,UiAdvances;
            internal string Phase="setup";
            internal void Flush()
            {
                // Inspecting the count is cheap; disk writes happen ONLY for new timing rows.
                while(Flushed<Case.Shared.Timings.Count)
                {
                    File.AppendAllText(Path.Combine(CampaignOutput(),"food-timings.jsonl"),
                        JsonConvert.SerializeObject(Case.Shared.Timings[Flushed])+Environment.NewLine);
                    Flushed++;
                }
            }
            internal void Mark(string phase)
            {
                Phase=phase;
                File.AppendAllText(Path.Combine(CampaignOutput(),"food-phases.jsonl"),
                    JsonConvert.SerializeObject(new{phase,productiveMs=Productive.Elapsed.TotalMilliseconds})+Environment.NewLine);
                Flush();
            }
        }

        // One explicit-menu expansion case; no original receiving test is changed or relabelled.
        //720 is ONLY600 productive +90 drain + scene-exit margin, never extra gameplay time.
        [UnityTest,Timeout(720000)]
        public IEnumerator ColonisationExpansionFoodInvestmentUsesRealQueueAndTypedContinue()
        {
            var t=new FoodReceivingState();var s=t.Case;var stack=new Stack<IEnumerator>();
            stack.Push(FoodReceivingBody(t));Exception? failure=null;
            while(stack.Count>0)
            {
                bool next=false;object? value=null;var current=stack.Peek();
                try
                {
                    if(t.Productive.Elapsed.TotalSeconds>=600)throw new TimeoutException("Food productive deadline before "+t.Phase);
                    next=current.MoveNext();if(next)value=current.Current;
                    if(t.Productive.Elapsed.TotalSeconds>=600)throw new TimeoutException("Food productive deadline after "+t.Phase);
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
            Cleanup("food-release-all",()=>{var error=s.ReleaseAll();failure=failure??error;});
            Cleanup("food-shared-release",()=>s.Shared.Release.Set());
            if(s.Shared.Entry!=null)Cleanup("food-disable",()=>s.Shared.Entry.enabled=false);
            while(stack.Count>0)try{(stack.Pop() as IDisposable)?.Dispose();}catch(Exception error){failure=failure??error;}
            bool settled=true;
            bool Pending()
            {
                if(s.Pending||s.Shared.Holder?.IsCompleted==false)return true;
                bool pending=true;Cleanup("food-drain-status",()=>pending=s.Shared.Entry!=null&&ColonyPending(s.Shared.Entry));return pending;
            }
            var cleanupClock=System.Diagnostics.Stopwatch.StartNew();
            while(Pending())
            {
                if(cleanupClock.Elapsed.TotalSeconds>=90)
                {
                    settled=false;directory=null;
                    failure=failure??new TimeoutException("Food worker unsettled; temporary namespace/override retained until scene exit.");break;
                }
                yield return null;
            }
            Cleanup("food-final-timings",t.Flush);
            bool overrideRestored=false;
            if(settled&&s.Shared.Entry!=null&&s.PriorDirectoryCaptured)
                Cleanup("food-restore-override",()=>{typeof(ReviewEntry).GetField("generatedCampaignTestDirectory",HullPrivate)!.SetValue(s.Shared.Entry,s.Shared.PriorDirectory);overrideRestored=true;});
            Cleanup("food-cleanup-receipt",()=>File.WriteAllText(Path.Combine(CampaignOutput(),"food-cleanup.json"),JsonConvert.SerializeObject(new{
                settled,allReleasesSet=s.Holds.All(h=>h.Release.IsSet),priorOverrideCaptured=s.PriorDirectoryCaptured,
                overrideRestored,directoryRetained=!settled,entryDisabled=s.Shared.Entry==null||!s.Shared.Entry.enabled,
                cleanupMs=cleanupClock.Elapsed.TotalMilliseconds,productiveMs=t.Productive.Elapsed.TotalMilliseconds,
                productiveLimitSeconds=600,drainLimitSeconds=90,outerFrameworkSeconds=720,failed=failure!=null,lastPhase=t.Phase})));
            if(settled)foreach(var host in s.Hosts)Cleanup("food-remove-probe",()=>ColonyProbe(host,null));
            if(!s.Pending)foreach(var h in s.Holds){Cleanup("food-dispose-entered",()=>h.Entered.Dispose());Cleanup("food-dispose-release",()=>h.Release.Dispose());}
            if(s.Shared.Holder==null||s.Shared.Holder.IsCompleted)
            {Cleanup("food-dispose-shared-entered",()=>s.Shared.Entered.Dispose());Cleanup("food-dispose-shared-release",()=>s.Shared.Release.Dispose());}
            yield return new ExitPlayMode();
            if(failure!=null)System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
        }


        private static void FoodUiFacts(UIDocument doc,ColonisationWorldSnapshot world)
        {
            var balance=doc.rootVisualElement.Q<Label>("colonisation-food-balance");Assert.That(balance,Is.Not.Null);
            Assert.That(balance.text,Does.Contain("Residents need "+world.FoodDemand+" food/week"));
            Assert.That(balance.text,Does.Contain("local balance "+(world.LocalFoodBalance>=0?"+":"")+world.LocalFoodBalance+" before imports/exports"));
            Assert.That(doc.rootVisualElement.Q<Label>("colonisation-food-shortage").text,Does.Contain("Consecutive food-shortage weeks: "+world.ShortageWeeks));
        }
        private static IEnumerator FoodRead(UIDocument doc,Label label)
        {
            Assert.That(label,Is.Not.Null);Assert.That(label.text,Is.Not.Empty);
            var parents=new List<ScrollView>();for(var at=label.parent;at!=null;at=at.parent)if(at is ScrollView view)parents.Add(view);
            foreach(var view in parents){view.ScrollTo(label);yield return null;yield return null;PublishedReceivingContained(view.contentViewport.worldBound,label,label.name);}
            Assert.That(label.panel,Is.SameAs(doc.rootVisualElement.panel));Assert.That(label.resolvedStyle.fontSize,Is.GreaterThanOrEqualTo(16));
            Assert.That(label.worldBound.yMax,Is.LessThanOrEqualTo(doc.rootVisualElement.Q("player-footer").worldBound.yMin));
        }
        private static IEnumerator FoodCapture(FoodReceivingState t,UIDocument doc,Vector2Int size,string filename,string hydroId)
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
                yield return FoodRead(doc,panel.Q<Label>("colonisation-food-balance"));
                yield return FoodRead(doc,effects);
                yield return ColonyReach(doc,panel.Q<Button>("colonisation-build-kit"),target:"own settlement project remains reachable");
            }
            finally
            {doc.panelSettings.targetTexture=oldTarget;doc.rootVisualElement.style.width=oldWidth;doc.rootVisualElement.style.height=oldHeight;RenderTexture.active=active;if(pixels!=null)UnityEngine.Object.DestroyImmediate(pixels);target.Release();UnityEngine.Object.DestroyImmediate(target);}
            yield return null;yield return null;
            Assert.That(ColonySelection(doc.rootVisualElement.Q<ReviewColonisationPanel>()),Is.EqualTo(selection),"resize preserves selected own world");
        }
        private IEnumerator FoodReceivingBody(FoodReceivingState t)
        {
            var s=t.Case;yield return new EnterPlayMode();yield return null;yield return null;
            var entry=UnityEngine.Object.FindAnyObjectByType<ReviewEntry>();Assert.That(entry,Is.Not.Null);s.Shared.Entry=entry;
            var field=typeof(ReviewEntry).GetField("generatedCampaignTestDirectory",HullPrivate)!;
            s.Shared.PriorDirectory=(string?)field.GetValue(entry);s.PriorDirectoryCaptured=true;
            directory=Path.Combine(Path.GetTempPath(),"HobbyDS9-Galaxy-Engine-Food-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(directory);
            var storePath=Path.Combine(directory,"campaign");field.SetValue(entry,storePath);var doc=entry.GetComponent<UIDocument>();
            using(var surface=new GalaxyInputSurface(doc))
            {
                t.Mark("explicit-new-begin");doc.rootVisualElement.Q<TextField>("generated-campaign-seed").value="42";
                doc.rootVisualElement.Q<DropdownField>("generated-campaign-size").value="250";doc.rootVisualElement.Q<DropdownField>("generated-campaign-quiet").value="2.00\u00d7";
                var menu=doc.rootVisualElement.Q<Foldout>("colonisation-expansion-campaign-menu");Assert.That(menu,Is.Not.Null);Assert.That(menu.text,Is.EqualTo("Colonisation & Expansion"));menu.value=true;
                var prior=ColonyModel(s);var start=doc.rootVisualElement.Q<Button>("colonisation-expansion-campaign-start");yield return ColonyReach(doc,start,target:"explicit new Expansion campaign");
                ColonyActivate(s,()=>{Click(start);if(doc.rootVisualElement.Q("review-one-use-confirmation")!=null)Confirm(doc);});yield return ColonyIdle(s,"food-new250");
                Assert.That(ColonyModel(s),Is.Not.SameAs(prior));Assert.That(ColonyModel(s).ColonisationExpansionCampaign,Is.True);
                var controller=ColonyModel(s).Current;var actor=controller.Frame.Actor;var initial=ColonyModel(s).Colonisation();
                // Actual owned snapshots select worlds and construction controls; audit roots never select commands.
                var yard=initial.Worlds.Single(w=>w.Stage==ColonyEstablishmentStage.Established&&w.Population==250&&w.FoodOutput==0&&w.FoodDemand==3);
                var home=initial.Worlds.Single(w=>w.Stage==ColonyEstablishmentStage.Established&&w.Population==1000&&w.FoodOutput==14&&w.FoodDemand==10);
                Assert.That(yard.LocalFoodBalance,Is.EqualTo(-3));Assert.That(home.LocalFoodBalance,Is.EqualTo(4));Assert.That(yard.Food,Is.EqualTo(4));
                yield return ColonyWorld(s,doc,home.Id);FoodUiFacts(doc,home);yield return ColonyWorld(s,doc,yard.Id);FoodUiFacts(doc,yard);s.Home=yard.Id;
                var activities=ColonyModel(s).ColonisationActivities();var hydro=activities.BuildOptions.Single(b=>b.Kind==ActivityProductionKind.Infrastructure&&b.DisplayName=="Hydroponic extension");
                var benefit=initial.BuildingBenefits.Single(b=>b.DefinitionId==hydro.DefinitionId);Assert.That(benefit.FoodOutput,Is.EqualTo(6));Assert.That(benefit.PowerDemand,Is.EqualTo(1));
                Assert.That(hydro.Cost.Industry,Is.EqualTo(40));Assert.That(hydro.Cost.Resources!.Single().ResourceDefinitionId,Is.EqualTo("resource.dilithium"));Assert.That(hydro.Cost.Resources.Single().Amount,Is.EqualTo(1));
                var effects=doc.rootVisualElement.Q<Label>("colonisation-building-effects-"+hydro.DefinitionId);Assert.That(effects.text,Does.Contain("Food +6"));Assert.That(effects.text,Does.Contain("power demand +1"));
                t.Mark("initial-visible-food-and-benefits");var credits=activities.Credits;
                var add=doc.rootVisualElement.Query<Button>().ToList().Single(b=>b.text=="+ Add "+hydro.DisplayName);yield return ColonyReach(doc,add,target:"normal enabled Hydroponic Add");
                yield return ColonyClick(s,doc,add.name);Assert.That(ColonyModel(s).ColonisationActivities().Credits,Is.EqualTo(credits),"streamed construction has no upfront normal-credit debit");
                var order=ColonyModel(s).Colonisation().Production.Single(q=>q.ColonyId==yard.Id&&q.DefinitionId==hydro.DefinitionId);
                Assert.That(order.RequiredIndustry,Is.EqualTo(40));Assert.That(order.CompletedIndustry,Is.Zero);Assert.That(order.Required.Single().Amount,Is.EqualTo(1));
                bool consumed=false,completed=false,nextEconomyBenefit=false;int completionTurn=-1;
                t.Mark("hydro-real-queue");
                for(int week=0;week<8&&!nextEconomyBenefit;week++)
                {
                    yield return ColonyAdvance(s,doc);t.UiAdvances++;
                    Assert.That(ColonyModel(s).Current,Is.SameAs(controller));Assert.That(ColonyModel(s).Current.Frame.Actor,Is.EqualTo(actor));
                    var own=ColonyModel(s).Colonisation();var current=own.Worlds.Single(w=>w.Id==yard.Id);
                    var activeOrder=own.Production.SingleOrDefault(q=>q.Id==order.Id);consumed|=activeOrder!=null&&activeOrder.CompletedIndustry>0&&activeOrder.Consumed.Any(r=>r.Amount>0);
                    var audit=ExpansionAudit(ColonyModel(s));var receipt=audit.BaseCampaign!.ProductionHistory!.SingleOrDefault(h=>h.Completed&&h.Order!.QueueItemId==order.Id);
                    if(receipt!=null)
                    {
                        Assert.That(receipt.Order!.CompletedIndustry,Is.EqualTo(40));Assert.That(receipt.Order.ConsumedResources!.Single().Amount,Is.EqualTo(1));
                        if(!completed)completionTurn=ColonyModel(s).Current.Frame.Status.Turn;completed=true;
                        nextEconomyBenefit=ColonyModel(s).Current.Frame.Status.Turn>completionTurn&&current.FoodOutput==yard.FoodOutput+benefit.FoodOutput;
                    }
                    File.AppendAllText(Path.Combine(CampaignOutput(),"food-week-observations.jsonl"),JsonConvert.SerializeObject(new{turn=own.Turn,current.Population,current.Food,current.FoodOutput,current.FoodDemand,current.LocalFoodBalance,current.ShortageWeeks,
                        queue=activeOrder==null?null:new{activeOrder.CompletedIndustry,activeOrder.RequiredIndustry,activeOrder.Consumed},completed,nextEconomyBenefit})+Environment.NewLine);
                    t.Mark("ordinary-ui-week-"+own.Turn);
                }
                Assert.That(consumed,Is.True,"positive Industry and actual material consumption observed before completion");Assert.That(completed,Is.True);Assert.That(nextEconomyBenefit,Is.True,"completion followed by actual next Economy within bounded advances, not a promised ETA");
                var final=ColonyModel(s).Colonisation().Worlds.Single(w=>w.Id==yard.Id);Assert.That(final.FoodOutput,Is.EqualTo(6));Assert.That(final.LocalFoodBalance,Is.EqualTo(final.FoodOutput-final.FoodDemand));
                yield return ColonyWorld(s,doc,yard.Id);FoodUiFacts(doc,final);
                var expected=ExpansionBytes(ColonyModel(s));t.Mark("save-begin");CampaignPage(doc,ReviewWorkspacePage.Saves);yield return null;Click(doc.rootVisualElement.Q<Button>("save-slot-generated-1"));
                ColonyActivate(s,()=>{Click(doc.rootVisualElement.Q<Button>("player-save"));if(doc.rootVisualElement.Q("review-one-use-confirmation")!=null)Confirm(doc);});yield return ColonyIdle(s,"food-whole12-save");
                Assert.That(File.ReadAllBytes(Path.Combine(storePath,"unity-campaign-colonisation-expansion-v1-generated-1.json")),Is.EqualTo(expected));
                File.WriteAllBytes(Path.Combine(CampaignOutput(),"food-completed-saved-private-audit.json"),expected);t.Mark("save-complete");
                prior=ColonyModel(s);Click(doc.rootVisualElement.Q<Button>("demo-menu"));yield return null;yield return null;
                var load=doc.rootVisualElement.Q<Button>("colonisation-expansion-campaign-continue-generated-1");yield return ColonyReach(doc,load,target:"explicit Expansion Continue");t.Mark("continue-begin");
                ColonyActivate(s,()=>{Click(load);Confirm(doc);});yield return ColonyIdle(s,"food-whole12-continue");
                Assert.That(ColonyModel(s),Is.Not.SameAs(prior));Assert.That(ColonyModel(s).Current,Is.Not.SameAs(controller));Assert.That(ColonyModel(s).Current.Frame.Actor,Is.EqualTo(actor));
                Assert.That(ColonyModel(s).ColonisationExpansionCampaign,Is.True);Assert.That(ExpansionBytes(ColonyModel(s)),Is.EqualTo(expected));t.Mark("continue-complete");
                yield return ColonyWorld(s,doc,yard.Id);FoodUiFacts(doc,ColonyModel(s).Colonisation().Worlds.Single(w=>w.Id==yard.Id));
                foreach(var size in new[]{new Vector2Int(1280,720),new Vector2Int(1920,1080),new Vector2Int(3440,1440)})
                {yield return FoodCapture(t,doc,size,FoodImages[size.x==1280?0:size.x==1920?1:2],hydro.DefinitionId);t.Mark("capture-"+size.x);}
                Assert.That(ExpansionBytes(ColonyModel(s)),Is.EqualTo(expected),"pure UI/read/resize/guide never changes whole root");
                Assert.That(Directory.GetFiles(CampaignOutput(),"food-0*-continued-*.png").Select(Path.GetFileName),Is.EquivalentTo(FoodImages));
                File.WriteAllText(Path.Combine(CampaignOutput(),"food-summary.json"),JsonConvert.SerializeObject(new{explicitMenu=true,systems=250,seed="42",initialFacts=true,publicBaseBenefits=true,realQueue=true,noCreditDebit=true,
                    consumed,completed,nextEconomyBenefit,wholeTypedContinue=true,threeLayouts=true,uiAdvances=t.UiAdvances,maximumUiAdvances=8,completionTurn,final.FoodOutput,final.FoodDemand,final.LocalFoodBalance,final.ShortageWeeks,captures=FoodImages,
                    qualification="Actual source/commands from own public views; audit only for exact full-root/own completion proof. No native/CPU performance or guaranteed ETA claim."}));
                t.Mark("all-food-gates-complete");
            }
        }
    }
}
