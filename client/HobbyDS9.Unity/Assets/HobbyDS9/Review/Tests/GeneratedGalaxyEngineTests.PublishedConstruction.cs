#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using HobbyDS9.Application;
using HobbyDS9.Contracts;
using HobbyDS9.Contracts.ShipDesign;
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
        private sealed class PublishedReceivingState
        {
            internal ReviewEntry? Entry;
            internal string? PriorDirectory;
            internal readonly ManualResetEventSlim Entered=new ManualResetEventSlim(),Release=new ManualResetEventSlim();
            internal Task? Holder;
            internal readonly List<object> Timings=new List<object>();
        }
        private static CampaignPublishedConstructionSaveDto PublishedReceivingAudit(ReviewUiModel model)
        {
            // Private identity/payment/provenance audit ONLY, never used to select a player action.
            var host=(PhysicalReviewHost)typeof(ReviewUiModel).GetField("host",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(model)!;
            return ((ICampaignPublishedConstructionSession)typeof(PhysicalReviewHost).GetField("session",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(host)!).ExportPublishedConstructionSave();
        }
        private static byte[] PublishedReceivingBytes(ReviewUiModel model)=>new ReviewNewtonsoftCodec().EncodePublishedConstructionRoot(PublishedReceivingAudit(model));
        private static IEnumerator PublishedReceivingIdle(PublishedReceivingState state,string phase)
        {
            var clock=System.Diagnostics.Stopwatch.StartNew();int frames=0;
            while((bool)CheckpointField(state.Entry!,"generatedCampaignBusy")!||CampaignModel(state.Entry!).Busy||CheckpointField(state.Entry!,"checkpointUiOperation")!=null)
            {Assert.That(clock.Elapsed.TotalSeconds,Is.LessThan(90),phase);frames++;yield return null;}
            state.Timings.Add(new{phase,observedWallMs=clock.Elapsed.TotalMilliseconds,yieldedFrames=frames,notFps=true});yield return null;yield return null;
        }
        private static IEnumerator PublishedReceivingInstall(ReviewEntry entry,UIDocument doc,string system,string component)
        {
            Click(doc.rootVisualElement.Q<Button>("designer-system-"+system));yield return null;
            Click(doc.rootVisualElement.Q<Button>("select-component-"+component));yield return null;
            Click(doc.rootVisualElement.Q<Button>("designer-add"));yield return null;
            Click(doc.rootVisualElement.Q<Button>("designer-fit-package"));yield return null;
            Assert.That(DesignerDto(entry).Installations!.Any(i=>i.ComponentVersionId==component),Is.True);
        }
        private static IEnumerator PublishedReceivingPower(ReviewEntry entry,UIDocument doc,params string[] components)
        {
            var catalogue=CampaignModel(entry).DesignLibrary().Catalogue!.Catalogue;
            Click(doc.rootVisualElement.Q<Button>("designer-system-profiles"));yield return null;
            foreach(var profile in Enum.GetValues(typeof(OperatingProfileKind)).Cast<OperatingProfileKind>())
            {
                Click(doc.rootVisualElement.Q<Button>("designer-profile-"+profile.ToString().ToLowerInvariant()));yield return null;
                foreach(var id in components)
                {
                    var full=catalogue.Components.Single(c=>c.VersionId==id).Modes.Single(m=>m.Profile==profile).FullPower;
                    doc.rootVisualElement.Q<LongField>("power-requested-"+id).value=full;yield return null;
                    doc.rootVisualElement.Q<LongField>("power-allocated-"+id).value=full;yield return null;
                }
            }
            Click(doc.rootVisualElement.Q<Button>("designer-system-overview"));yield return null;
            Assert.That(doc.rootVisualElement.Q<Button>("integrated-design-publish").enabledInHierarchy,Is.True,"actual preview accepts the explicit authored power allocations");
        }
        private static void PublishedReceivingSelect(UIDocument doc,string displayName,string colony)
        {
            doc.rootVisualElement.Q<Foldout>("integrated-design-construction").value=true;
            var revisions=doc.rootVisualElement.Q<DropdownField>("published-construction-revision");
            revisions.value=revisions.choices.Single(c=>c.StartsWith(displayName+" ·",StringComparison.Ordinal));
            doc.rootVisualElement.Q<DropdownField>("published-construction-colony").value=colony;
        }
        private static void PublishedReceivingContained(Rect container,VisualElement item,string message)
        {Assert.That(item.worldBound.width,Is.GreaterThan(20),message);Assert.That(item.worldBound.height,Is.GreaterThanOrEqualTo(20),message);
         Assert.That(container.Contains(item.worldBound.min+Vector2.one)&&container.Contains(item.worldBound.max-Vector2.one),Is.True,message+" "+item.worldBound+" inside "+container);}
        private static IEnumerator PublishedReceivingCapture(UIDocument doc,Vector2Int size,string label,bool designer)
        {
            var previous=doc.panelSettings.targetTexture;var active=RenderTexture.active;var width=doc.rootVisualElement.style.width;var height=doc.rootVisualElement.style.height;
            var target=new RenderTexture(size.x,size.y,24,RenderTextureFormat.ARGB32);Texture2D? image=null;
            try
            {
                Assert.That(target.Create(),Is.True);doc.panelSettings.targetTexture=target;doc.rootVisualElement.style.width=size.x;doc.rootVisualElement.style.height=size.y;
                yield return null;yield return null;yield return null;
                if(designer){doc.rootVisualElement.Q<Foldout>("integrated-design-construction").value=true;doc.rootVisualElement.Q<Foldout>("integrated-design-history").value=true;yield return null;yield return null;}
                RenderTexture.active=target;image=new Texture2D(size.x,size.y,TextureFormat.RGBA32,false);image.ReadPixels(new Rect(0,0,size.x,size.y),0,0);image.Apply();
                // Capture the intended state before assertions; failed layout evidence is retained within the eight-frame budget.
                File.WriteAllBytes(Path.Combine(CampaignOutput(),"construction-"+label+"-"+size.x+"x"+size.y+".png"),image.EncodeToPNG());
                var root=doc.rootVisualElement.worldBound;var footer=doc.rootVisualElement.Q("player-footer").worldBound;
                if(designer)
                {
                    var inspector=doc.rootVisualElement.Q(className:"sd-inspector-footer").worldBound;
                    var queue=doc.rootVisualElement.Q<Button>("published-construction-queue");var preview=doc.rootVisualElement.Q<Image>("designer-hull-reference-image");
                    File.WriteAllText(Path.Combine(CampaignOutput(),"construction-layout-"+label+"-"+size.x+".json"),JsonConvert.SerializeObject(new{root=root.ToString(),inspector=inspector.ToString(),queue=queue.worldBound.ToString(),image=preview.worldBound.ToString(),footer=footer.ToString()}));
                    foreach(var id in new[]{"published-construction-revision","published-construction-colony","published-construction-queue"})
                    {var control=doc.rootVisualElement.Q(id);PublishedReceivingContained(root,control,id);PublishedReceivingContained(inspector,control,id);Assert.That(control.worldBound.yMax,Is.LessThanOrEqualTo(footer.yMin),id+" cannot overlap navigation/footer");}
                    Assert.That(queue.worldBound.height,Is.GreaterThanOrEqualTo(36));Assert.That(queue.resolvedStyle.fontSize,Is.GreaterThanOrEqualTo(16));
                    foreach(var id in new[]{"published-construction-revision","published-construction-colony"})
                    {var field=doc.rootVisualElement.Q<DropdownField>(id);Assert.That(field.worldBound.width,Is.GreaterThanOrEqualTo(120));Assert.That(field.worldBound.height,Is.GreaterThanOrEqualTo(24));}
                    Assert.That(preview.worldBound.height,Is.GreaterThanOrEqualTo(150),"expanded build and history controls retain the accepted useful model viewport");
                    var history=doc.rootVisualElement.Q<ScrollView>("integrated-design-history-scroll");
                    foreach(var revision in history.Query<Button>().ToList())
                    {history.ScrollTo(revision);yield return null;yield return null;PublishedReceivingContained(history.contentViewport.worldBound,revision,"history revision remains scroll-reachable");}
                }
                else PublishedReceivingContained(root,doc.rootVisualElement.Q<Button>("map-fit"),"official V2 map controls");
            }
            finally{doc.panelSettings.targetTexture=previous;doc.rootVisualElement.style.width=width;doc.rootVisualElement.style.height=height;RenderTexture.active=active;
                if(image!=null)UnityEngine.Object.DestroyImmediate(image);target.Release();UnityEngine.Object.DestroyImmediate(target);}
            yield return null;yield return null;yield return null;
        }
        [UnityTest,Timeout(600000)]
        public IEnumerator PublishedConstructionUsesActualDesignerPaymentAndTypedContinue()
        {
            var state=new PublishedReceivingState();var stack=new Stack<IEnumerator>();stack.Push(PublishedReceivingBody(state));Exception? failure=null;
            while(stack.Count>0)
            {
                bool next=false;object? value=null;var current=stack.Peek();
                try{next=current.MoveNext();if(next)value=current.Current;}catch(Exception error){failure=error;}
                if(failure!=null)break;if(!next){stack.Pop();(current as IDisposable)?.Dispose();continue;}
                if(value is IEnumerator nested){stack.Push(nested);continue;}yield return value;
            }
            state.Release.Set();while(stack.Count>0)(stack.Pop() as IDisposable)?.Dispose();
            var cleanup=System.Diagnostics.Stopwatch.StartNew();
            while(state.Entry!=null&&((bool)CheckpointField(state.Entry,"generatedCampaignBusy")!||CampaignModel(state.Entry).Busy||CheckpointField(state.Entry,"checkpointUiOperation")!=null||state.Holder?.IsCompleted==false))
            {if(cleanup.Elapsed.TotalSeconds>=90){directory=null;failure=failure??new TimeoutException("Unsettled construction worker; temporary namespace retained.");break;}yield return null;}
            try
            {
                File.WriteAllText(Path.Combine(CampaignOutput(),"construction-latency.json"),JsonConvert.SerializeObject(state.Timings));
                if(state.Entry!=null&&CampaignModel(state.Entry).PublishedConstructionCampaign&&!CampaignModel(state.Entry).Busy)
                    File.WriteAllBytes(Path.Combine(CampaignOutput(),failure==null?"construction-final-private-audit.json":"construction-failed-private-audit.json"),PublishedReceivingBytes(CampaignModel(state.Entry)));
            }catch(Exception error){CampaignDiagnosticError("construction-final-audit",error);}
            if(state.Entry!=null){state.Entry.enabled=false;typeof(ReviewEntry).GetField("generatedCampaignTestDirectory",BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(state.Entry,state.PriorDirectory);}
            if(state.Holder==null||state.Holder.IsCompleted){state.Entered.Dispose();state.Release.Dispose();}yield return new ExitPlayMode();
            if(failure!=null)System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
        }
        private IEnumerator PublishedReceivingBody(PublishedReceivingState state)
        {
            yield return new EnterPlayMode();yield return null;yield return null;
            var entry=UnityEngine.Object.FindAnyObjectByType<ReviewEntry>();state.Entry=entry;var doc=entry.GetComponent<UIDocument>();
            directory=Path.Combine(Path.GetTempPath(),"HobbyDS9-Galaxy-Engine-Published-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(directory);
            var testField=typeof(ReviewEntry).GetField("generatedCampaignTestDirectory",BindingFlags.Instance|BindingFlags.NonPublic)!;state.PriorDirectory=(string?)testField.GetValue(entry);testField.SetValue(entry,Path.Combine(directory,"campaign"));
            var codec=new ReviewNewtonsoftCodec();
            using(var surface=new GalaxyInputSurface(doc))
            {
                yield return null;yield return null;
                doc.rootVisualElement.Q<TextField>("generated-campaign-seed").value="42";doc.rootVisualElement.Q<DropdownField>("generated-campaign-size").value="250";
                doc.rootVisualElement.Q<DropdownField>("generated-campaign-quiet").value="2.00×";
                doc.rootVisualElement.Q<Foldout>("published-construction-menu").value=true;Click(doc.rootVisualElement.Q<Button>("published-construction-start"));Confirm(doc);
                Assert.That(doc.rootVisualElement.Q<Label>("campaign-operation-title").text,Does.Contain("Generating"));yield return PublishedReceivingIdle(state,"actual-new250");
                var model=CampaignModel(entry);Assert.That(model.PublishedConstructionCampaign,Is.True);
                var colony=model.Current.Frame.Strategy!.Construction.Where(o=>o.CanEnqueue).OrderBy(o=>o.ColonyId.Value,StringComparer.Ordinal).First().ColonyId.Value;
                CampaignPage(doc,ReviewWorkspacePage.Designer);yield return null;yield return null;
                yield return PublishedReceivingInstall(entry,doc,"sensors","component.escort-sensors.v1");yield return PublishedReceivingInstall(entry,doc,"weapons","component.energy-bank.v1");
                yield return PublishedReceivingPower(entry,doc,"component.escort-sensors.v1","component.energy-bank.v1");
                doc.rootVisualElement.Q<TextField>("designer-name").value="Checkpoint Escort";yield return null;Click(doc.rootVisualElement.Q<Button>("integrated-design-publish"));yield return null;
                var revision1=model.DesignLibrary().Published.Single();var blueprint=revision1.Blueprint.BlueprintId;var frozen=JsonConvert.SerializeObject(revision1.Blueprint);
                PublishedReceivingSelect(doc,"Checkpoint Escort",colony);yield return null;Click(doc.rootVisualElement.Q<Button>("published-construction-queue"));
                var abandonedConfirm=doc.rootVisualElement.Q("review-one-use-confirmation").Query<Button>().ToList().Single(b=>b.text=="Confirm");Confirm(doc,false);
                var priorRevision=model.Current.Frame.Status.CampaignRevision;
                CampaignPage(doc,ReviewWorkspacePage.Research);var split=doc.rootVisualElement.Q<SliderInt>("integrated-research-split");split.value=(split.value+1)%21;yield return null;
                Click(doc.rootVisualElement.Q<Button>("player-primary-action"));Confirm(doc);yield return null;
                Assert.That(model.Current.Frame.Status.CampaignRevision,Is.GreaterThan(priorRevision),"real R&D command changes the observation stamp");
                split=doc.rootVisualElement.Q<SliderInt>("integrated-research-split");split.value=(split.value+1)%21;yield return null;
                var draft=model.ResearchInputs();Assert.That(draft!.Dirty,Is.True);var beforeStale=PublishedReceivingBytes(model);var cash=model.Current.Frame.Strategy!.Civilization.Credits;
                Click(abandonedConfirm);yield return null;Assert.That(PublishedReceivingBytes(model),Is.EqualTo(beforeStale));Assert.That(model.Current.Frame.Strategy!.Civilization.Credits,Is.EqualTo(cash));Assert.That(model.ResearchInputs(),Is.SameAs(draft));Assert.That(draft.Dirty,Is.True);
                Click(CampaignButton(doc,"Discard changes"));Confirm(doc);yield return null;
                CampaignPage(doc,ReviewWorkspacePage.Designer);Click(doc.rootVisualElement.Q<Button>("integrated-design-refresh"));yield return null;
                PublishedReceivingSelect(doc,"Checkpoint Escort",colony);yield return null;var cost=revision1.Blueprint.Performance.Cost!;
                var queueButton=doc.rootVisualElement.Q<Button>("published-construction-queue");
                var eligibilityText=doc.rootVisualElement.Q<Label>("published-construction-eligibility").text;
                cash=model.Current.Frame.Strategy!.Civilization.Credits;
                File.WriteAllText(Path.Combine(CampaignOutput(),"construction-before-payment.json"),JsonConvert.SerializeObject(new{
                    enabled=queueButton.enabledInHierarchy,eligibilityText,selectedRevision=doc.rootVisualElement.Q<DropdownField>("published-construction-revision").value,
                    colony,cash,cost,model.Message,model.Current.Dirty,model.Current.RefreshRequired,model.Current.Frame.Status.Turn,model.Current.Frame.Status.CampaignRevision}));
                Assert.That(queueButton.enabledInHierarchy,Is.True,eligibilityText);Assert.That(eligibilityText,Does.Contain("eligible"));
                Click(queueButton);Confirm(doc);yield return null;
                File.WriteAllText(Path.Combine(CampaignOutput(),"construction-after-payment.json"),JsonConvert.SerializeObject(new{
                    cash=model.Current.Frame.Strategy!.Civilization.Credits,model.Message,model.Current.Dirty,model.Current.RefreshRequired,
                    model.Current.Frame.Status.Turn,model.Current.Frame.Status.CampaignRevision}));
                Assert.That(model.Current.Frame.Strategy!.Civilization.Credits,Is.EqualTo(cash-cost.Credits),model.Message);
                Click(doc.rootVisualElement.Q<Button>("integrated-design-refresh"));yield return null;doc.rootVisualElement.Q<TextField>("designer-name").value="Checkpoint Escort II";yield return null;Click(doc.rootVisualElement.Q<Button>("integrated-design-publish"));yield return null;
                Assert.That(JsonConvert.SerializeObject(model.DesignLibrary().Published.Single(r=>r.Blueprint.BlueprintId==blueprint&&r.Blueprint.Revision==1).Blueprint),Is.EqualTo(frozen));
                PublishedReceivingSelect(doc,"Checkpoint Escort II",colony);
                foreach(var size in new[]{new Vector2Int(1280,720),new Vector2Int(1920,1080),new Vector2Int(3440,1440)})yield return PublishedReceivingCapture(doc,size,"queued-r1-r2",true);
                var pending=PublishedReceivingAudit(model);Assert.That(pending.Campaign!.Integrated.Physical!.Core.Campaign.GameState.Colonies!.Single(c=>c.ColonyId==colony).ProductionQueue!.Single().Blueprint!.Revision,Is.EqualTo(1));
                var originalOwnShips=new HashSet<string>(model.Current.Frame.OwnShips.Select(s=>s.Condition.ShipId),StringComparer.Ordinal);
                CampaignPage(doc,ReviewWorkspacePage.Map);
                for(int turn=0;turn<100&&model.Current.Frame.OwnShips.All(s=>originalOwnShips.Contains(s.Condition.ShipId));turn++)
                {Assert.That(model.Current.Frame.Status.Phase,Is.EqualTo(SessionPhase.Planning));Click(doc.rootVisualElement.Q<Button>("player-primary-action"));Confirm(doc);yield return null;yield return null;}
                var completed=PublishedReceivingAudit(model);var built=completed.PublishedBuiltShips!.Single();Assert.That(built.BlueprintRevision,Is.EqualTo(1));
                Assert.That(model.Current.Frame.OwnShips.Any(s=>s.Condition.ShipId==built.ShipId),Is.True);
                CampaignPage(doc,ReviewWorkspacePage.Designer);Click(doc.rootVisualElement.Q<Button>("integrated-design-refresh"));yield return null;
                Click(doc.rootVisualElement.Q<Button>("integrated-design-new"));yield return null;yield return PublishedReceivingInstall(entry,doc,"sensors","component.survey-sensors.v1");
                yield return PublishedReceivingPower(entry,doc,"component.survey-sensors.v1");doc.rootVisualElement.Q<TextField>("designer-name").value="Unsupported Survey";yield return null;
                var untouchedCash=model.Current.Frame.Strategy!.Civilization.Credits;Click(doc.rootVisualElement.Q<Button>("integrated-design-publish"));yield return null;PublishedReceivingSelect(doc,"Unsupported Survey",colony);yield return null;
                Assert.That(doc.rootVisualElement.Q<Button>("published-construction-queue").enabledInHierarchy,Is.False);Assert.That(doc.rootVisualElement.Q<Label>("published-construction-eligibility").text,Does.Contain("unsupported"));Assert.That(model.Current.Frame.Strategy!.Civilization.Credits,Is.EqualTo(untouchedCash));
                Assert.That(model.Current.Frame.Strategy.Colonies.All(c=>c.State.ProductionQueue.Count==0),Is.True,"unsupported publication creates no paid queue");
                doc.rootVisualElement.Q<TextField>("designer-name").value="Unsent local edit";yield return null;var dirty=JsonConvert.SerializeObject(DesignerDto(entry));
                var expected=PublishedReceivingBytes(model);CampaignPage(doc,ReviewWorkspacePage.Saves);Click(doc.rootVisualElement.Q<Button>("save-slot-generated-1"));
                var clock=System.Diagnostics.Stopwatch.StartNew();Click(doc.rootVisualElement.Q<Button>("player-save"));state.Timings.Add(new{phase="save-click-and-detached-capture",synchronousMs=clock.Elapsed.TotalMilliseconds});yield return PublishedReceivingIdle(state,"save-validation-and-write");
                var path=Path.Combine(directory!,"campaign","unity-published-construction-v1-generated-1.json");Assert.That(File.ReadAllBytes(path),Is.EqualTo(expected));Assert.That(model.LocalDesignDirty,Is.True);Assert.That(JsonConvert.SerializeObject(DesignerDto(entry)),Is.EqualTo(dirty));
                File.WriteAllBytes(Path.Combine(CampaignOutput(),"construction-saved-private-audit.json"),expected);
                var store=typeof(ReviewUiModel).GetField("publishedConstructionSlots",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(model)!;
                var storeGate=store.GetType().GetField("gate",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(store)!;
                state.Holder=Task.Run(()=>{lock(storeGate){state.Entered.Set();if(!state.Release.Wait(TimeSpan.FromSeconds(90)))throw new TimeoutException();}});
                clock.Restart();while(!state.Entered.IsSet){Assert.That(clock.Elapsed.TotalSeconds,Is.LessThan(90));yield return null;}
                Click(doc.rootVisualElement.Q<Button>("player-load"));Confirm(doc);yield return null;CheckpointBusy(doc,"Loading");
                int ticks=0;var heartbeat=doc.rootVisualElement.schedule.Execute(()=>ticks++).Every(10);var positions=new HashSet<float>();clock.Restart();
                while(clock.Elapsed.TotalSeconds<.3){positions.Add(doc.rootVisualElement.Q("campaign-operation-indicator").style.left.value.value);yield return null;}
                Assert.That(ticks,Is.GreaterThanOrEqualTo(3));Assert.That(positions.Count,Is.GreaterThan(1));CheckpointCapture(doc,"construction-loading-responsive");heartbeat.Pause();state.Release.Set();yield return PublishedReceivingIdle(state,"held-ui-load");
                Assert.That(PublishedReceivingBytes(model),Is.EqualTo(expected));Assert.That(model.HasLocalEdits,Is.False);
                Click(doc.rootVisualElement.Q<Button>("demo-menu"));yield return null;doc.rootVisualElement.Q<Foldout>("published-construction-menu").value=true;
                Click(doc.rootVisualElement.Q<Button>("published-construction-continue-generated-1"));Confirm(doc);yield return PublishedReceivingIdle(state,"menu-typed-continue");
                model=CampaignModel(entry);Assert.That(PublishedReceivingBytes(model),Is.EqualTo(expected));
                CampaignPage(doc,ReviewWorkspacePage.Designer);yield return null;doc.rootVisualElement.Q<Foldout>("integrated-design-history").value=true;
                Click(doc.rootVisualElement.Q<Button>("integrated-design-edit-"+blueprint+"-2"));yield return null;PublishedReceivingSelect(doc,"Checkpoint Escort II",colony);
                foreach(var size in new[]{new Vector2Int(1280,720),new Vector2Int(1920,1080),new Vector2Int(3440,1440)})yield return PublishedReceivingCapture(doc,size,"continued",true);
                CampaignPage(doc,ReviewWorkspacePage.Map);yield return null;yield return null;
                var fleet=model.Current.Frame.OwnStrategicFleets.First(f=>f.TransitTotalCost==0);Click(doc.rootVisualElement.Q<Button>("map-fleet-"+fleet.FleetId.Value));yield return null;yield return null;Click(doc.rootVisualElement.Q<Button>("map-fit"));yield return null;yield return null;
                var destination=model.GeneratedMap().Regions.Select(r=>(r.NodeId,Offer:model.Current.FindKnownRoute(fleet.FleetId.Value,r.NodeId))).Where(p=>p.Offer?.Route.Success==true&&p.Offer.Route.TotalCost>0).OrderBy(p=>p.Offer!.Route.TotalCost).ThenBy(p=>p.NodeId,StringComparer.Ordinal).First().NodeId;
                yield return CampaignPointerRight(doc,entry,doc.rootVisualElement.Q<Button>("map-star-"+destination));if(doc.rootVisualElement.Q("review-one-use-confirmation")!=null)Confirm(doc);yield return null;
                var turnBefore=model.Current.Frame.Status.Turn;Click(doc.rootVisualElement.Q<Button>("player-primary-action"));Confirm(doc);yield return null;Assert.That(model.Current.Frame.Status.Turn,Is.GreaterThan(turnBefore));
                File.WriteAllBytes(Path.Combine(CampaignOutput(),"construction-after-order-private-audit.json"),PublishedReceivingBytes(model));
                // Separate official V2 fixture, never an owner file or a conversion of the new profile.
                var template=(CampaignPlayableFleetPackageDto)typeof(ReviewEntry).GetMethod("LoadPlayableFleetTemplate",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(entry,null)!;
                var legacy=CampaignStaticAstronomyApplication.StartGame(template,new NaturalGalaxySettingsDto{Map=new ScaledGalaxySettingsDto{Seed="42",SystemCount=250,EmptyRegionCount=250,Width=2500000,Height=2000000,QuietEmptyAreaTargetPermille=2000,HomeCivilizationDefinitionIds=new List<string>{"civilization.federation","civilization.cardassian"}}}).ExportGeneratedSave();
                var legacyBytes=codec.EncodeStaticAstronomyCampaignRoot(legacy);new ReviewStaticAstronomyCampaignSlotStore(Path.Combine(directory!,"campaign"),codec).Write("generated-3",legacy,false);
                File.WriteAllBytes(Path.Combine(CampaignOutput(),"construction-official-v2-fixture.json"),legacyBytes);
                Click(doc.rootVisualElement.Q<Button>("demo-menu"));yield return null;Click(doc.rootVisualElement.Q<Button>("generated-campaign-continue-generated-3"));Confirm(doc);yield return PublishedReceivingIdle(state,"separate-official-v2-continue");
                Assert.That(CampaignModel(entry).PublishedConstructionCampaign,Is.False);Assert.That(codec.EncodeStaticAstronomyCampaignRoot(CampaignPrivateAudit(CampaignModel(entry))),Is.EqualTo(legacyBytes));
                yield return PublishedReceivingCapture(doc,new Vector2Int(1280,720),"official-v2-fixture",false);
                Assert.That(Directory.GetFiles(CampaignOutput(),"construction-*.png").Length+Directory.GetFiles(CampaignOutput(),"checkpoint-construction-*.png").Length,Is.EqualTo(8));
                File.WriteAllText(Path.Combine(CampaignOutput(),"construction-results.json"),JsonConvert.SerializeObject(new{passed=true,captures=8,fresh250=true,paidR1=true,r2Preserved=true,completedR1=true,unsupportedPublishedNotQueued=true,cancelledStaleConfirmNoDebit=true,dirtyDraftRetained=true,busyUiPumps=true,typedContinueExact=true,freshKnownOrder=true,separateOfficialV2Fixture=true,nativeInput=false}));
            }
        }
    }
}
