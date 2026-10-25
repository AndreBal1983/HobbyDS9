#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using HobbyDS9.Application;
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
        // Roots below are private audit evidence only. All destinations and commands
        // are selected from the actual controlled actor's presented observations.
        private static CampaignStreamedActivitiesSaveDto StreamAudit(ReviewUiModel model)
        {
            var host=(PhysicalReviewHost)typeof(ReviewUiModel).GetField("host",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(model)!;
            return ((ICampaignStreamedActivitiesSession)typeof(PhysicalReviewHost).GetField("session",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(host)!).ExportStreamedActivitiesSave();
        }
        private static byte[] StreamBytes(ReviewUiModel model)=>new ReviewNewtonsoftCodec().EncodeStreamedActivitiesRoot(StreamAudit(model));
        private static string StreamYard(ReviewUiModel model)
        {
            var own=model.Activities();var ship=own.BuildOptions.First(o=>o.Kind==ActivityProductionKind.PublishedShip&&o.Supported);
            // The same actor-safe, non-mutating eligibility projection used by the panel;
            // old upfront-paid production availability is not authority for this profile.
            return own.Colonies.OrderBy(c=>c.ColonyId,StringComparer.Ordinal).First(c=>model.ActivityEligibility(new CampaignActivityCommandDto{
                ActorId=own.ActorId,ExpectedTurn=own.Turn,ExpectedRevision=own.Revision,CommandId="command.receiving.yard-probe",Kind=CampaignActivityCommandKind.EnqueueShip,
                TargetId=c.ColonyId,DefinitionId=ship.DefinitionId,BlueprintRevision=ship.BlueprintRevision}).Accepted).ColonyId;
        }
        private static IEnumerator StreamClick(UIDocument doc,string id,bool scroll=true)
        {
            if(id.StartsWith("activity-build-review-",StringComparison.Ordinal))
            {
                var model=CampaignModel(doc.GetComponent<ReviewEntry>());var yard=StreamYard(model);
                doc.rootVisualElement.Q<DropdownField>("activity-colony-selector").index=model.Activities().Colonies.FindIndex(c=>c.ColonyId==yard);yield return null;
            }
            var button=doc.rootVisualElement.Q<Button>(id);Assert.That(button,Is.Not.Null,id);
            var view=doc.rootVisualElement.Q<ScrollView>("activity-scroll");if(scroll&&view!=null&&view.Contains(button))view.ScrollTo(button);
            yield return null;yield return null;
            Assert.That(button.panel,Is.SameAs(doc.rootVisualElement.panel),id+" must be attached");
            Assert.That(button.enabledInHierarchy,Is.True,id);Click(button);yield return null;yield return null;
        }
        private static IEnumerator StreamOrder(UIDocument doc,string review)
        {yield return StreamClick(doc,review);yield return StreamClick(doc,"activity-order-confirm",false);}
        private static IEnumerator StreamPage(UIDocument doc,bool expeditions=false)
        {
            CampaignPage(doc,ReviewWorkspacePage.Production);yield return null;yield return null;
            var model=CampaignModel(doc.GetComponent<ReviewEntry>());var observation=model.Activities();
            var yard=StreamYard(model);
            doc.rootVisualElement.Q<DropdownField>("activity-colony-selector").index=observation.Colonies.FindIndex(c=>c.ColonyId==yard);yield return null;
            if(expeditions)yield return StreamClick(doc,"activity-tab-expeditions",false);
        }
        private static IEnumerator StreamAdvance(UIDocument doc,ReviewEntry entry)
        {
            CampaignPage(doc,ReviewWorkspacePage.Map);yield return V3Settled(doc);
            var before=CampaignModel(entry).Current.Frame.Status.Turn;
            Assert.That(CampaignModel(entry).Current.Frame.Status.Phase,Is.EqualTo(SessionPhase.Planning),"no combat fixture or forced encounter in this journey");
            Click(doc.rootVisualElement.Q<Button>("player-primary-action"));Confirm(doc);yield return null;yield return null;
            Assert.That(CampaignModel(entry).Current.Frame.Status.Turn,Is.GreaterThan(before),CampaignModel(entry).Message);
        }
        private static IEnumerator StreamRoute(UIDocument doc,ReviewEntry entry,string fleet,string node,bool? confirm=null)
        {
            CampaignPage(doc,ReviewWorkspacePage.Map);yield return V3Settled(doc);
            Click(doc.rootVisualElement.Q<Button>("map-fleet-"+fleet));yield return V3Settled(doc);
            Click(doc.rootVisualElement.Q<Button>("map-fit"));yield return V3Settled(doc);
            yield return CampaignPointerRight(doc,entry,doc.rootVisualElement.Q<Button>("map-star-"+node));
            if(doc.rootVisualElement.Q("review-one-use-confirmation")!=null)
            {
                if(confirm.HasValue){var wording=string.Join(" ",doc.rootVisualElement.Q("review-one-use-confirmation").Query<Label>().ToList().Select(l=>l.text));Assert.That(wording,Does.Contain("Leaving ends this expedition"));Assert.That(wording,Does.Contain("not refunded"));}
                Confirm(doc,confirm??true);
            }
            else Assert.That(confirm,Is.Not.EqualTo(false),"expected interruption warning is a real modal");
            yield return V3Settled(doc);
        }
        private static IEnumerator StreamCapture(UIDocument doc,Vector2Int size,string label,bool review=false,bool expedition=false,string? reviewId=null)
        {
            var previous=doc.panelSettings.targetTexture;var active=RenderTexture.active;var w=doc.rootVisualElement.style.width;var h=doc.rootVisualElement.style.height;
            var target=new RenderTexture(size.x,size.y,24,RenderTextureFormat.ARGB32);Texture2D? image=null;
            try
            {
                Assert.That(target.Create(),Is.True);doc.panelSettings.targetTexture=target;doc.rootVisualElement.style.width=size.x;doc.rootVisualElement.style.height=size.y;
                yield return null;yield return null;yield return null;
                // Resize can rebuild the surface and correctly dispose its old review.
                // Review again through the current observed option, never a retained control.
                if(review){Assert.That(reviewId,Is.Not.Null);yield return StreamClick(doc,reviewId!);}
                var scroll=doc.rootVisualElement.Q<ScrollView>("activity-scroll");
                if(scroll!=null){var item=doc.rootVisualElement.Q(expedition?"activity-known-sites":"activity-production-queue");if(item!=null)scroll.ScrollTo(item);yield return null;yield return null;}
                RenderTexture.active=target;image=new Texture2D(size.x,size.y,TextureFormat.RGBA32,false);image.ReadPixels(new Rect(0,0,size.x,size.y),0,0);image.Apply();
                File.WriteAllBytes(Path.Combine(CampaignOutput(),"streamed-"+label+"-"+size.x+"x"+size.y+".png"),image.EncodeToPNG());
                var root=doc.rootVisualElement.worldBound;var footer=doc.rootVisualElement.Q("player-footer").worldBound;
                File.WriteAllText(Path.Combine(CampaignOutput(),"streamed-layout-"+label+"-"+size.x+".json"),JsonConvert.SerializeObject(new{
                    root=root.ToString(),footer=footer.ToString(),panel=doc.rootVisualElement.Q("campaign-activities-panel")?.worldBound.ToString(),
                    scroll=scroll?.contentViewport.worldBound.ToString(),confirm=doc.rootVisualElement.Q("activity-order-confirm")?.worldBound.ToString()}));
                if(scroll!=null)
                {
                    Assert.That(doc.rootVisualElement.Query<ReviewActivitiesPanel>().ToList().Count,Is.EqualTo(1),"one current activities surface after redraw/resize");
                    Assert.That(scroll.contentViewport.worldBound.height,Is.GreaterThanOrEqualTo(100),"usable activity viewport");
                    foreach(var id in expedition?new[]{"activity-ship-selector","activity-site-selector"}:new[]{"activity-colony-selector","activity-build-review-0"})
                    {
                        var item=doc.rootVisualElement.Q(id);Assert.That(item,Is.Not.Null,id);scroll.ScrollTo(item);yield return null;yield return null;
                        PublishedReceivingContained(scroll.contentViewport.worldBound,item,id+" must be genuinely scroll reachable");
                        Assert.That(item.worldBound.yMax,Is.LessThanOrEqualTo(footer.yMin),id+" must not cover footer");
                    }
                    if(!expedition){var cancel=doc.rootVisualElement.Q<Button>("activity-queue-cancel-0");if(cancel!=null){scroll.ScrollTo(cancel);yield return null;yield return null;PublishedReceivingContained(scroll.contentViewport.worldBound,cancel,"queue cancellation remains genuinely scroll reachable");}}
                }
                if(review)
                {
                    var confirm=doc.rootVisualElement.Q<Button>("activity-order-confirm");PublishedReceivingContained(root,confirm,"actual order Confirm");
                    Assert.That(confirm.worldBound.yMax,Is.LessThanOrEqualTo(footer.yMin));Assert.That(confirm.worldBound.height,Is.GreaterThanOrEqualTo(36));Assert.That(confirm.resolvedStyle.fontSize,Is.GreaterThanOrEqualTo(16));
                    var picked=doc.rootVisualElement.panel.Pick(confirm.worldBound.center);Assert.That(picked==confirm||(picked!=null&&confirm.Contains(picked)),Is.True,"actual panel pick reaches Confirm, not an overlay");
                }
            }
            finally{doc.panelSettings.targetTexture=previous;doc.rootVisualElement.style.width=w;doc.rootVisualElement.style.height=h;RenderTexture.active=active;if(image!=null)UnityEngine.Object.DestroyImmediate(image);target.Release();UnityEngine.Object.DestroyImmediate(target);}
            yield return null;yield return null;yield return null;
            // Surface restoration is another resize; leave a current review for Back.
            if(review)yield return StreamClick(doc,reviewId!);
        }
        private static void StreamHullComponent(ActivityBuildOptionDto option)
        {
            // Component receiving proof using disclosed OWN ready build options;
            // this does not claim that all three classes were built in this campaign.
            var active=RenderTexture.active;Texture2D? image=null;
            var type=Type.GetType("HobbyDS9.ArtDemo.ShipPreviewStage, HobbyDS9.ArtDemo",true)!;
            var stage=(IDisposable)Activator.CreateInstance(type,new object[]{768,480})!;
            try
            {
                type.GetMethod("ShowActivityHull")!.Invoke(stage,new object[]{option.HullVersionId});type.GetMethod("Render")!.Invoke(stage,null);
                Assert.That((bool)type.GetProperty("IsActivityHull")!.GetValue(stage)!,Is.True);Assert.That(type.GetProperty("Availability")!.GetValue(stage)!.ToString(),Is.EqualTo("Model"));
                RenderTexture.active=(RenderTexture)type.GetProperty("Texture")!.GetValue(stage)!;image=new Texture2D(768,480,TextureFormat.RGBA32,false);image.ReadPixels(new Rect(0,0,768,480),0,0);image.Apply();Assert.That(image.GetPixels32().Distinct().Take(33).Count(),Is.GreaterThan(32),"actual hull pixels differ from a uniform clear");
                File.WriteAllBytes(Path.Combine(CampaignOutput(),"streamed-component-own-ready-"+option.HullVersionId+".png"),image.EncodeToPNG());
                File.WriteAllText(Path.Combine(CampaignOutput(),"streamed-component-own-ready-"+option.HullVersionId+".json"),JsonConvert.SerializeObject(new{componentOnly=true,option.HullVersionId,option.DefinitionId,option.BlueprintRevision,diagnostics=type.GetMethod("CaptureArtDiagnostics")!.Invoke(stage,null)}));
            }
            finally{RenderTexture.active=active;if(image!=null)UnityEngine.Object.DestroyImmediate(image);stage.Dispose();}
        }
        private IEnumerator StreamSaveContinue(PublishedReceivingState state,UIDocument doc,string label)
        {
            var savingModel=CampaignModel(state.Entry!);var dirty=savingModel.LocalDesignDirty;var draft=dirty?JsonConvert.SerializeObject(DesignerDto(state.Entry!)):null;var expected=StreamBytes(savingModel);CampaignPage(doc,ReviewWorkspacePage.Saves);yield return null;
            Click(doc.rootVisualElement.Q<Button>("save-slot-generated-1"));var clock=System.Diagnostics.Stopwatch.StartNew();Click(doc.rootVisualElement.Q<Button>("player-save"));
            if(doc.rootVisualElement.Q("review-one-use-confirmation")!=null)Confirm(doc);
            state.Timings.Add(new{phase=label+"-save-synchronous-capture",ms=clock.Elapsed.TotalMilliseconds});yield return PublishedReceivingIdle(state,label+"-save");
            var path=Path.Combine(directory!,"campaign","unity-campaign-streamed-activities-v1-generated-1.json");Assert.That(File.ReadAllBytes(path),Is.EqualTo(expected));if(dirty){Assert.That(savingModel.LocalDesignDirty,Is.True);Assert.That(JsonConvert.SerializeObject(DesignerDto(state.Entry!)),Is.EqualTo(draft),"Save retains the unsent class draft");}
            File.WriteAllBytes(Path.Combine(CampaignOutput(),"streamed-"+label+"-saved-private-audit.json"),expected);
            Click(doc.rootVisualElement.Q<Button>("demo-menu"));yield return null;yield return null;
            Click(doc.rootVisualElement.Q<Button>("streamed-activities-campaign-continue-generated-1"));Confirm(doc);yield return PublishedReceivingIdle(state,label+"-menu-continue");
            Assert.That(CampaignModel(state.Entry!).StreamedActivitiesCampaign,Is.True);Assert.That(StreamBytes(CampaignModel(state.Entry!)),Is.EqualTo(expected));
        }
        [UnityTest,Timeout(600000)]
        public IEnumerator StreamedActivitiesCampaignUsesActualWorldsExpeditionsAndTypedContinue()
        {
            var state=new PublishedReceivingState();var stack=new Stack<IEnumerator>();stack.Push(StreamReceivingBody(state));Exception? failure=null;
            while(stack.Count>0)
            {
                bool next=false;object? value=null;var current=stack.Peek();try{next=current.MoveNext();if(next)value=current.Current;}catch(Exception error){failure=error;}
                if(failure!=null)break;if(!next){stack.Pop();(current as IDisposable)?.Dispose();continue;}if(value is IEnumerator nested){stack.Push(nested);continue;}yield return value;
            }
            state.Release.Set();while(stack.Count>0)(stack.Pop() as IDisposable)?.Dispose();var cleanup=System.Diagnostics.Stopwatch.StartNew();
            while(state.Entry!=null&&((bool)CheckpointField(state.Entry,"generatedCampaignBusy")!||CampaignModel(state.Entry).Busy||CheckpointField(state.Entry,"checkpointUiOperation")!=null||state.Holder?.IsCompleted==false))
            {if(cleanup.Elapsed.TotalSeconds>=90){directory=null;failure=failure??new TimeoutException("Unsettled streamed worker; temporary namespace retained.");break;}yield return null;}
            try{File.WriteAllText(Path.Combine(CampaignOutput(),"streamed-latency.json"),JsonConvert.SerializeObject(state.Timings));if(state.Entry!=null&&CampaignModel(state.Entry).StreamedActivitiesCampaign&&!CampaignModel(state.Entry).Busy)File.WriteAllBytes(Path.Combine(CampaignOutput(),failure==null?"streamed-final-private-audit.json":"streamed-failed-private-audit.json"),StreamBytes(CampaignModel(state.Entry)));}catch(Exception error){CampaignDiagnosticError("streamed-final-audit",error);}
            if(state.Entry!=null){state.Entry.enabled=false;typeof(ReviewEntry).GetField("generatedCampaignTestDirectory",BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(state.Entry,state.PriorDirectory);}
            if(state.Holder==null||state.Holder.IsCompleted){state.Entered.Dispose();state.Release.Dispose();}yield return new ExitPlayMode();if(failure!=null)System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
        }
        private IEnumerator StreamReceivingBody(PublishedReceivingState state)
        {
            yield return new EnterPlayMode();yield return null;yield return null;var entry=UnityEngine.Object.FindAnyObjectByType<ReviewEntry>();state.Entry=entry;var doc=entry.GetComponent<UIDocument>();
            directory=Path.Combine(Path.GetTempPath(),"HobbyDS9-Galaxy-Engine-Streamed-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(directory);
            var field=typeof(ReviewEntry).GetField("generatedCampaignTestDirectory",BindingFlags.Instance|BindingFlags.NonPublic)!;state.PriorDirectory=(string?)field.GetValue(entry);field.SetValue(entry,Path.Combine(directory,"campaign"));
            using(var surface=new GalaxyInputSurface(doc))
            {
                yield return null;doc.rootVisualElement.Q<TextField>("generated-campaign-seed").value="42";doc.rootVisualElement.Q<DropdownField>("generated-campaign-size").value="250";doc.rootVisualElement.Q<DropdownField>("generated-campaign-quiet").value="2.00×";
                Assert.That(doc.rootVisualElement.Q<Foldout>("streamed-activities-campaign-menu").value,Is.True);Click(doc.rootVisualElement.Q<Button>("streamed-activities-campaign-start"));Confirm(doc);
                Assert.That(doc.rootVisualElement.Q<Label>("campaign-operation-title").text,Does.Contain("Generating"));yield return PublishedReceivingIdle(state,"streamed-new250");
                var model=CampaignModel(entry);Assert.That(model.StreamedActivitiesCampaign,Is.True);Assert.That(model.Activities().Sites,Is.Empty);
                var ready=model.Activities().BuildOptions.Where(o=>o.Kind==ActivityProductionKind.PublishedShip&&o.Supported).ToList();Assert.That(ready.Select(o=>o.HullVersionId).Distinct().Count(),Is.EqualTo(3));foreach(var option in ready)StreamHullComponent(option);
                var blueprint=ready.Single(o=>o.HullVersionId=="hull.constitution-explorer.v1").DefinitionId;
                yield return StreamPage(doc);var credits=model.Activities().Credits;var stock=JsonConvert.SerializeObject(model.StreamedActivities().Supply);
                int structure=model.Activities().BuildOptions.FindIndex(o=>o.Kind==ActivityProductionKind.Infrastructure&&o.DefinitionId=="infrastructure.fabrication-annex.v1");Assert.That(structure,Is.GreaterThanOrEqualTo(0));
                yield return StreamOrder(doc,"activity-build-review-"+structure);Assert.That(model.Activities().Credits,Is.EqualTo(credits));Assert.That(JsonConvert.SerializeObject(model.StreamedActivities().Supply),Is.EqualTo(stock));
                int ship=model.Activities().BuildOptions.FindIndex(o=>o.DefinitionId==blueprint&&o.BlueprintRevision==1);yield return StreamOrder(doc,"activity-build-review-"+ship);
                Assert.That(model.StreamedActivities().Production.Count,Is.EqualTo(2));Assert.That(model.Activities().Credits,Is.EqualTo(credits));Assert.That(model.StreamedActivities().Production.All(q=>q.CompletedIndustry==0&&q.ConsumedResources!.All(r=>r.Amount==0)),Is.True);
                CampaignPage(doc,ReviewWorkspacePage.Designer);yield return null;yield return null;doc.rootVisualElement.Q<Foldout>("integrated-design-history").value=true;
                yield return StreamClick(doc,"integrated-design-edit-"+blueprint+"-1",false);doc.rootVisualElement.Q<TextField>("designer-name").value="Explorer revised after queue";yield return null;yield return StreamClick(doc,"integrated-design-publish",false);
                Assert.That(model.DesignLibrary().Published.Any(r=>r.Blueprint.BlueprintId==blueprint&&r.Blueprint.Revision==2),Is.True);Assert.That(model.StreamedActivities().Production.Last().Blueprint!.Revision,Is.EqualTo(1));
                yield return StreamPage(doc);yield return StreamClick(doc,"activity-build-review-"+model.Activities().BuildOptions.FindIndex(o=>o.DefinitionId==blueprint&&o.BlueprintRevision==2));
                var redrawBytes=StreamBytes(model);var redrawFrame=model.Current.Frame;
                for(int redraw=0;redraw<3;redraw++)
                {
                    var previous=doc.rootVisualElement.Q<ReviewActivitiesPanel>();var oldConfirm=previous.Q<Button>("activity-order-confirm");
                    Assert.That(oldConfirm,Is.Not.Null,"capture a real pending control before same-frame redraw");
                    typeof(ReviewEntry).GetMethod("Draw",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(entry,null);yield return null;yield return null;
                    Assert.That(doc.rootVisualElement.Query<ReviewActivitiesPanel>().ToList().Count,Is.EqualTo(1));Assert.That(doc.rootVisualElement.Q<ReviewActivitiesPanel>(),Is.Not.SameAs(previous));Assert.That(previous.panel,Is.Null);
                    Assert.That((bool)typeof(ReviewActivitiesPanel).GetField("disposed",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(previous)!,Is.True,"the old captured callbacks are disposed, not merely hidden");
                    Click(oldConfirm);yield return null;Assert.That(model.Current.Frame,Is.SameAs(redrawFrame));Assert.That(StreamBytes(model),Is.EqualTo(redrawBytes),"retained disposed Confirm cannot enqueue, spend or redraw state");
                    yield return StreamClick(doc,"activity-build-review-"+model.Activities().BuildOptions.FindIndex(o=>o.DefinitionId==blueprint&&o.BlueprintRevision==2));
                }
                foreach(var size in new[]{new Vector2Int(1280,720),new Vector2Int(1920,1080),new Vector2Int(3440,1440)})yield return StreamCapture(doc,size,"pending-review",true,reviewId:"activity-build-review-"+model.Activities().BuildOptions.FindIndex(o=>o.DefinitionId==blueprint&&o.BlueprintRevision==2));
                yield return StreamClick(doc,"activity-order-back",false);CampaignPage(doc,ReviewWorkspacePage.Designer);yield return null;doc.rootVisualElement.Q<TextField>("designer-name").value="Unsent local class edit";yield return null;Assert.That(model.LocalDesignDirty,Is.True);yield return StreamSaveContinue(state,doc,"pending");model=CampaignModel(entry);
                bool progressed=false;for(int i=0;i<48&&model.StreamedActivities().Production.Count>0;i++)
                {yield return StreamAdvance(doc,entry);var orders=model.StreamedActivities().Production;progressed|=orders.Any(q=>q.CompletedIndustry>0&&q.ConsumedResources!.Any(r=>r.Amount>0));}
                var completed=StreamAudit(model);Assert.That(progressed,Is.True,"actual gradual work/material consumption observed");Assert.That(completed.Production,Is.Empty);Assert.That(completed.ProductionHistory!.Count(r=>r.Completed&&r.Order!.OwnerId==model.Activities().ActorId),Is.EqualTo(2));
                Assert.That(completed.PublishedBuiltShips!.Single(r=>r.OwnerCivilizationId==model.Activities().ActorId).BlueprintRevision,Is.EqualTo(1));Assert.That(model.Activities().Structures.Count,Is.EqualTo(1));
                yield return StreamSaveContinue(state,doc,"completed");model=CampaignModel(entry);yield return StreamPage(doc);yield return StreamClick(doc,"activity-build-review-"+model.Activities().BuildOptions.FindIndex(o=>o.DefinitionId==blueprint&&o.BlueprintRevision==2));
                foreach(var size in new[]{new Vector2Int(1280,720),new Vector2Int(1920,1080),new Vector2Int(3440,1440)})yield return StreamCapture(doc,size,"continued-review",true,reviewId:"activity-build-review-"+model.Activities().BuildOptions.FindIndex(o=>o.DefinitionId==blueprint&&o.BlueprintRevision==2));
                yield return StreamClick(doc,"activity-order-back",false);
                yield return StreamExpedition(doc,entry,false);yield return StreamExpedition(doc,entry,true);
                yield return StreamSaveContinue(state,doc,"findings");model=CampaignModel(entry);
                // Hold the actual slot store on a worker, never a semaphore on the UI thread.
                CampaignPage(doc,ReviewWorkspacePage.Saves);yield return null;Click(doc.rootVisualElement.Q<Button>("save-slot-generated-1"));
                var store=typeof(ReviewUiModel).GetField("streamedActivitiesSlots",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(model)!;var gate=store.GetType().GetField("gate",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(store)!;
                state.Holder=System.Threading.Tasks.Task.Run(()=>{lock(gate){state.Entered.Set();if(!state.Release.Wait(TimeSpan.FromSeconds(90)))throw new TimeoutException();}});
                var clock=System.Diagnostics.Stopwatch.StartNew();while(!state.Entered.IsSet){Assert.That(clock.Elapsed.TotalSeconds,Is.LessThan(90));yield return null;}
                var expected=StreamBytes(model);Click(doc.rootVisualElement.Q<Button>("player-load"));Confirm(doc);yield return null;CheckpointBusy(doc,"Loading");
                int ticks=0;var heartbeat=doc.rootVisualElement.schedule.Execute(()=>ticks++).Every(10);var positions=new HashSet<float>();clock.Restart();
                while(clock.Elapsed.TotalSeconds<.3){positions.Add(doc.rootVisualElement.Q("campaign-operation-indicator").style.left.value.value);yield return null;}
                Assert.That(ticks,Is.GreaterThanOrEqualTo(3));Assert.That(positions.Count,Is.GreaterThan(1));CheckpointCapture(doc,"streamed-loading-responsive");heartbeat.Pause();state.Release.Set();yield return PublishedReceivingIdle(state,"streamed-held-load");model=CampaignModel(entry);Assert.That(StreamBytes(model),Is.EqualTo(expected));
                // Separate, accepted actual-engine V2 Explore32 fixture through official typed store.
                var legacyPath=Environment.GetEnvironmentVariable("HOBBYDS9_V3_EXPLORE");Assert.That(File.Exists(legacyPath),Is.True);var legacyBytes=File.ReadAllBytes(legacyPath!);
                using(var sha=System.Security.Cryptography.SHA256.Create())Assert.That(BitConverter.ToString(sha.ComputeHash(legacyBytes)).Replace("-","").ToLowerInvariant(),Is.EqualTo("d7b332383b726607209067a5c89085a4979814b12c35a5fd717a41ebd3ecdddc"));
                var codec=new ReviewNewtonsoftCodec();var legacy=codec.DecodeStaticAstronomyCampaignRoot(legacyBytes);new ReviewStaticAstronomyCampaignSlotStore(Path.Combine(directory,"campaign"),codec).Write("generated-3",legacy,false);
                Click(doc.rootVisualElement.Q<Button>("demo-menu"));yield return null;doc.rootVisualElement.Q<Foldout>("earlier-generated-campaign-menu").value=true;yield return null;
                Click(doc.rootVisualElement.Q<Button>("generated-campaign-continue-generated-3"));Confirm(doc);yield return PublishedReceivingIdle(state,"separate-official-v2-explore");
                Assert.That(CampaignModel(entry).StreamedActivitiesCampaign,Is.False);Assert.That(codec.EncodeStaticAstronomyCampaignRoot(CampaignPrivateAudit(CampaignModel(entry))),Is.EqualTo(codec.EncodeStaticAstronomyCampaignRoot(legacy)));
                yield return StreamCapture(doc,new Vector2Int(1280,720),"separate-v2-explore");
                int captures=Directory.GetFiles(CampaignOutput(),"streamed-*.png").Length+Directory.GetFiles(CampaignOutput(),"checkpoint-streamed-*.png").Length;Assert.That(captures,Is.EqualTo(14));
                File.WriteAllText(Path.Combine(CampaignOutput(),"streamed-results.json"),JsonConvert.SerializeObject(new{passed=true,captures,normalConstructionCredits=false,gradualMaterials=true,mixedFifo=true,frozenR1=true,r2Published=true,scienceAndMinor=true,typedContinueExact=true,threeOwnReadyHullComponentProof=true,nativeInput=false}));
            }
        }
        private static IEnumerator StreamExpedition(UIDocument doc,ReviewEntry entry,bool minor,bool interruption=true)
        {
            var model=CampaignModel(entry);var visited=new HashSet<string>();ActivityKnownSiteDto? found=null;
            var explorer=model.Activities().Ships.First(s=>s.HullVersionId=="hull.constitution-explorer.v1");
            for(int i=0;i<48;i++)
            {
                explorer=model.Activities().Ships.Single(s=>s.ShipId==explorer.ShipId);visited.Add(explorer.NodeId);
                found=model.Activities().Sites.FirstOrDefault(s=>(s.Minor!=null)==minor&&s.Choices.Any(c=>c.RequiredStudy<=explorer.StudyWork&&c.RequiredContact<=explorer.ContactCapacity&&!model.Activities().Findings.Any(f=>f.SiteId==s.Site.SiteId&&f.FindingId==c.FindingId)));if(found!=null)break;
                var fleet=model.Current.Frame.OwnStrategicFleets.Single(f=>f.FleetId.Value==explorer.FleetId);
                if(explorer.Stationary&&fleet.RouteNodeIds.Count==0)
                {
                    var route=model.GeneratedMap().Regions.Where(r=>!visited.Contains(r.NodeId)).Select(r=>(Node:r.NodeId,Offer:model.Current.FindKnownRoute(explorer.FleetId,r.NodeId)))
                        .Where(x=>x.Offer?.Route.Success==true&&x.Offer.Route.TotalCost>0).OrderBy(x=>x.Offer!.Route.Steps.Count).ThenBy(x=>x.Node,StringComparer.Ordinal).First();
                    yield return StreamRoute(doc,entry,explorer.FleetId,route.Node);
                }
                yield return StreamAdvance(doc,entry);
            }
            Assert.That(found,Is.Not.Null,"normal actor-known exploration discovers "+(minor?"minor society":"science site"));
            explorer=model.Activities().Ships.Single(s=>s.ShipId==explorer.ShipId);
            if(explorer.NodeId!=found!.Site.NodeId)yield return StreamRoute(doc,entry,explorer.FleetId,found.Site.NodeId);
            for(int i=0;i<24&&(model.Activities().Ships.Single(s=>s.ShipId==explorer.ShipId).NodeId!=found.Site.NodeId||!model.Activities().Ships.Single(s=>s.ShipId==explorer.ShipId).Stationary);i++)yield return StreamAdvance(doc,entry);
            explorer=model.Activities().Ships.Single(s=>s.ShipId==explorer.ShipId);Assert.That(explorer.NodeId,Is.EqualTo(found.Site.NodeId));Assert.That(explorer.Stationary,Is.True);
            yield return StreamPage(doc,true);doc.rootVisualElement.Q<DropdownField>("activity-ship-selector").index=model.Activities().Ships.FindIndex(s=>s.ShipId==explorer.ShipId);
            doc.rootVisualElement.Q<DropdownField>("activity-site-selector").index=model.Activities().Sites.FindIndex(s=>s.Site.SiteId==found.Site.SiteId);yield return null;
            found=model.Activities().Sites.Single(s=>s.Site.SiteId==found.Site.SiteId);int choice=found.Choices.FindIndex(c=>minor?c.ChoiceId.EndsWith(".channel",StringComparison.Ordinal):c.RequiredStudy<=explorer.StudyWork&&c.Work>0);Assert.That(choice,Is.GreaterThanOrEqualTo(0));
            var cash=model.Activities().Credits;var cost=found.Choices[choice].Credits;yield return StreamOrder(doc,"activity-choice-review-"+choice);Assert.That(model.Activities().Credits,Is.EqualTo(cash-cost));
            if(!minor&&interruption)
            {
                var destination=model.GeneratedMap().Regions.Select(r=>(r.NodeId,Offer:model.Current.FindKnownRoute(explorer.FleetId,r.NodeId))).Where(x=>x.Offer?.Route.Success==true&&x.Offer.Route.TotalCost>0).OrderBy(x=>x.Offer!.Route.TotalCost).ThenBy(x=>x.NodeId,StringComparer.Ordinal).First().NodeId;
                var before=StreamBytes(model);yield return StreamRoute(doc,entry,explorer.FleetId,destination,false);Assert.That(StreamBytes(model),Is.EqualTo(before));
                yield return StreamRoute(doc,entry,explorer.FleetId,destination,true);Assert.That(model.Activities().Findings.Last().Stage,Is.EqualTo(ActivityProgressStage.Aborted));
                Assert.That(model.Activities().Findings.Count(f=>f.Stage==ActivityProgressStage.Aborted),Is.EqualTo(1));
                yield return StreamCapture(doc,new Vector2Int(1280,720),"confirmed-departure");
                yield return StreamExpedition(doc,entry,false,false);yield break;
            }
            for(int i=0;i<8&&model.Activities().Findings.Last().Stage==ActivityProgressStage.Active;i++)yield return StreamAdvance(doc,entry);
            Assert.That(model.Activities().Findings.Last().Stage,Is.EqualTo(ActivityProgressStage.AwaitingClaim));yield return StreamPage(doc,true);
            int finding=model.Activities().Findings.Count-1;yield return StreamOrder(doc,"activity-finding-claim-"+finding);Assert.That(model.Activities().Findings.Last().Stage,Is.EqualTo(ActivityProgressStage.Completed));
            var resolved=model.Activities().Findings.Last();if(minor)Assert.That(model.Activities().Sites.Single(s=>s.Site.SiteId==found.Site.SiteId).Relation,Is.EqualTo(ActivityMinorRelation.Accepted).Or.EqualTo(ActivityMinorRelation.Refused));else Assert.That(resolved.InsightApplied,Is.GreaterThan(0));
            yield return StreamCapture(doc,new Vector2Int(1280,720),minor?"minor-consent":"science-claim",false,true);

        }
    }
}
