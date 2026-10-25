#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
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
        private const string ActivityHullFixtureSha="d583e45117fd7bc9076a8605b7ef510cebe1e936b19a7938f6186a1ab430f0cc";
        private static readonly BindingFlags HullPrivate=BindingFlags.Instance|BindingFlags.NonPublic;
        private static readonly Dictionary<string,string> HullExpected=new Dictionary<string,string>(StringComparer.Ordinal)
        {{"hull.constitution-explorer.v1","activity.constitution-explorer.v1"},{"hull.defiant-combat.v1","activity.defiant-combat.v1"}};
        private static object HullProperty(object value,string name)=>value.GetType().GetProperty(name)!.GetValue(value)!;
        private static IDictionary HullInstances(ReviewBattleStage stage)=>(IDictionary)typeof(ReviewBattleStage).GetField("hullModels",HullPrivate)!.GetValue(stage)!;
        private static string HullSourceMaterial(Material value)=>JsonUtility.ToJson(value);
        private static Dictionary<Material,string> HullImportedMaterials()
        {
            var result=new Dictionary<Material,string>();
            foreach(var path in new[]{"ArtDemo/ActivityHulls/Constitution/Models/ConstitutionExplorer_LOD0","ArtDemo/ActivityHulls/Defiant/Models/Defiant_LOD0"})
            {
                var prefab=Resources.Load<GameObject>(path);Assert.That(prefab,Is.Not.Null,path);
                foreach(var material in prefab.GetComponentsInChildren<Renderer>(true).SelectMany(r=>r.sharedMaterials).Distinct())result[material]=HullSourceMaterial(material);
            }
            return result;
        }
        private static void HullSourcesUnchanged(Dictionary<Material,string> sources)
        {foreach(var item in sources)Assert.That(HullSourceMaterial(item.Key),Is.EqualTo(item.Value),"imported shared material remains unchanged");}
        private static object HullStageAudit(ReviewBattleStage stage,string label)
        {
            var boundary=(CampaignPlaybackBoundary)typeof(ReviewBattleStage).GetField("boundary",HullPrivate)!.GetValue(stage)!;
            var models=HullInstances(stage);var ships=(Dictionary<string,GameObject>)typeof(ReviewBattleStage).GetField("ships",HullPrivate)!.GetValue(stage)!;
            var wanted=boundary.OwnShips.Where(s=>s.Hull>0).Select(s=>s.ShipId).ToArray();
            Assert.That(models.Keys.Cast<string>(),Is.EquivalentTo(wanted),"exact currently presented living OWN boundary hulls, not authoritative post-round/future rows");
            var facts=new List<object>();
            foreach(var id in wanted)
            {
                var identity=boundary.ShipIdentities.Single(i=>i.ShipId==id);Assert.That(identity.OwnHullVersionId,Is.Not.Null);
                Assert.That(HullExpected.ContainsKey(identity.OwnHullVersionId!),Is.True,"fixture contains only disclosed starter Constitution and produced Defiant");
                var instance=models[id]!;var definition=HullProperty(instance,"Definition");var root=(GameObject)HullProperty(instance,"Root");
                Assert.That((string)HullProperty(definition,"HullVersionId"),Is.EqualTo(identity.OwnHullVersionId));
                Assert.That((string)HullProperty(definition,"VisualKey"),Is.EqualTo(HullExpected[identity.OwnHullVersionId!]));
                Assert.That((string)HullProperty(instance,"MissingArt"),Is.Empty);Assert.That(ships[id].transform.GetChild(0).gameObject,Is.SameAs(root));
                Assert.That(root.GetComponentsInChildren<Camera>(true),Is.Empty,"bare model contains no per-ship camera");
                var renderers=root.GetComponentsInChildren<MeshRenderer>(true);Assert.That(renderers,Is.Not.Empty);
                foreach(var renderer in renderers)foreach(var material in renderer.sharedMaterials)
                {
                    Assert.That(material.shader,Is.SameAs(Resources.Load<Shader>("ArtDemo/PreviewStudio")));
                    if(identity.OwnHullVersionId=="hull.defiant-combat.v1")
                    {
                        const string path="ArtDemo/ActivityHulls/Defiant/Textures/Defiant_Image_";
                        Assert.That(material.GetTexture("_BaseMap"),Is.SameAs(Resources.Load<Texture2D>(path+"0")));
                        Assert.That(material.GetTexture("_BumpMap"),Is.SameAs(Resources.Load<Texture2D>(path+"2")));
                        Assert.That(material.GetTexture("_MetallicRoughnessMap"),Is.SameAs(Resources.Load<Texture2D>(path+"1")));
                        Assert.That(material.GetFloat("_UseNormalMap"),Is.EqualTo(1));Assert.That(material.GetFloat("_UseMetallicRoughnessMap"),Is.EqualTo(1));
                    }
                }
                facts.Add(new{id,identity.OwnHullVersionId,visualKey=HullProperty(definition,"VisualKey"),renderers=renderers.Length,materials=renderers.Sum(r=>r.sharedMaterials.Length)});
            }
            foreach(var enemy in boundary.VisibleEnemies)
            {
                Assert.That(boundary.ShipIdentities.Single(i=>i.ShipId==enemy.ShipId).OwnHullVersionId,Is.Null);Assert.That(models.Contains(enemy.ShipId),Is.False);
                if(enemy.Hull>0)
                {
                    Assert.That(ships.ContainsKey(enemy.ShipId),Is.True);var identity=boundary.ShipIdentities.Single(i=>i.ShipId==enemy.ShipId);
                    if(identity.CivilizationDefinitionId=="civilization.cardassian"&&identity.ShipDesignDefinitionId=="ship-design.travel-target")
                        Assert.That(ships[enemy.ShipId].GetComponentsInChildren<MeshFilter>(true).Any(f=>f.sharedMesh!=null&&f.sharedMesh.name!="Presentation marker"),Is.True,"legacy visible enemy model retained");
                }
            }
            Assert.That(ships.Keys,Is.EquivalentTo(boundary.OwnShips.Concat(boundary.VisibleEnemies).Where(s=>s.Hull>0).Select(s=>s.ShipId)));
            Assert.That(boundary.VisibleEnemies.Any(e=>e.Hull>0&&boundary.ShipIdentities.Any(i=>i.ShipId==e.ShipId&&i.CivilizationDefinitionId=="civilization.cardassian"&&i.ShipDesignDefinitionId=="ship-design.travel-target")),Is.True,"actual fixture retains the disclosed legacy enemy");
            File.AppendAllText(Path.Combine(CampaignOutput(),"activity-battle-hulls.jsonl"),JsonConvert.SerializeObject(new{label,facts,stage.GenericMarkers,foreignHullDisclosure=false})+Environment.NewLine);
            return facts;
        }
        private sealed class HullDisposedProof
        {
            private readonly ReviewBattleStage stage;
            private readonly GameObject[] roots;
            private readonly Material[] materials;
            internal HullDisposedProof(ReviewBattleStage stage)
            {
                this.stage=stage;var values=HullInstances(stage).Values.Cast<object>().ToArray();roots=values.Select(v=>(GameObject)HullProperty(v,"Root")).ToArray();
                materials=values.SelectMany(v=>((IEnumerable)v.GetType().GetField("materialsOwned",HullPrivate)!.GetValue(v)!).Cast<Material>()).Distinct().ToArray();
            }
            internal void AssertDisposed()
            {Assert.That(HullInstances(stage).Count,Is.Zero);Assert.That(roots.All(r=>r==null),Is.True);Assert.That(materials.All(m=>m==null),Is.True);Assert.That(stage.Texture==null||!stage.Texture.IsCreated(),Is.True);}
        }
        private static IEnumerator HullSized(ReviewEntry entry,UIDocument doc,Vector2Int size,string label,bool scene,bool overview=false,bool paused=false)
        {
            var old=doc.panelSettings.targetTexture;var width=doc.rootVisualElement.style.width;var height=doc.rootVisualElement.style.height;var active=RenderTexture.active;
            var target=new RenderTexture(size.x,size.y,24,RenderTextureFormat.ARGB32);Texture2D? pixels=null;bool passed=false;
            try
            {
                Assert.That(target.Create(),Is.True);doc.panelSettings.targetTexture=target;doc.rootVisualElement.style.width=size.x;doc.rootVisualElement.style.height=size.y;yield return null;yield return null;yield return null;
                if(scene)
                {
                    var camera=(Camera)typeof(ReviewBattleStage).GetField("camera",HullPrivate)!.GetValue(BattleStage(doc))!;
                    if(camera.orthographic!=overview)Click(CampaignButton(doc,"Overview / cinematic"));
                    if(BattleStage(doc).Paused!=paused)Click(doc.rootVisualElement.Q<Button>("battle-playback-pause"));
                    yield return null;yield return null;
                }
                RenderTexture.active=target;pixels=new Texture2D(size.x,size.y,TextureFormat.RGBA32,false);pixels.ReadPixels(new Rect(0,0,size.x,size.y),0,0);pixels.Apply();
                File.WriteAllBytes(Path.Combine(CampaignOutput(),"battle-v2-"+label+"-"+size.x+"x"+size.y+".png"),pixels.EncodeToPNG());
                BattleLayoutAudit(doc,size,label);
                var rail=doc.rootVisualElement.Q("central-combat-surface").Q(className:"review-order-dock");BattleVisible(rail,size);
                foreach(var id in new[]{"combat-tab-battle","combat-tab-stocks","combat-tab-report","combat-tab-inspection","combat-toggle-fleet-details"}){var item=doc.rootVisualElement.Q(id);if(item!=null)BattleVisible(item,size);}
                foreach(var action in doc.rootVisualElement.Query<Button>().ToList().Where(b=>b.text.StartsWith("Review / commit round ",StringComparison.Ordinal)||b.name=="combat-skip-pending-playback"))BattleVisible(action,size);
                if(scene)
                {
                    var stage=BattleStage(doc);var camera=(Camera)typeof(ReviewBattleStage).GetField("camera",HullPrivate)!.GetValue(stage)!;
                    Assert.That(camera.orthographic,Is.EqualTo(overview));Assert.That(stage.Paused,Is.EqualTo(paused));HullStageAudit(stage,label+"-"+size.x);BattleCameraContainment(stage,label+"-"+size.x);
                    var image=doc.rootVisualElement.Q<Image>("battle-3d-image");BattleVisible(image,size);Assert.That(image.image,Is.TypeOf<RenderTexture>());Assert.That(image.image.width,Is.EqualTo(1280));Assert.That(image.image.height,Is.EqualTo(600));
                    Assert.That(image.scaleMode,Is.EqualTo(ScaleMode.ScaleToFit));
                    float scale=Math.Min(image.contentRect.width/1280,image.contentRect.height/600);
                    File.AppendAllText(Path.Combine(CampaignOutput(),"activity-hull-layout.jsonl"),JsonConvert.SerializeObject(new{label,size.x,size.y,widget=image.worldBound.ToString(),paintedWidth=1280*scale,paintedHeight=600*scale,camera.orthographic,stage.Paused})+Environment.NewLine);
                    if(size.x==1280)Assert.That(600*scale,Is.GreaterThanOrEqualTo(220),"actual painted scene height");
                    Assert.That(image.worldBound.Overlaps(rail.worldBound),Is.False);
                    foreach(var control in doc.rootVisualElement.Q("resolved-battle-presentation").Query<Button>().ToList())BattleVisible(control,size);
                }
                passed=true;
            }
            finally
            {
                try{File.AppendAllText(Path.Combine(CampaignOutput(),"activity-hull-capture-outcomes.jsonl"),JsonConvert.SerializeObject(new{label,size.x,size.y,assertionsPassed=passed,imageWritten=pixels!=null,claim="same intended state; failed PNG is not a pass"})+Environment.NewLine);}catch(Exception error){CampaignDiagnosticError("activity-hull-capture-outcome",error);}
                doc.panelSettings.targetTexture=old;doc.rootVisualElement.style.width=width;doc.rootVisualElement.style.height=height;RenderTexture.active=active;target.Release();UnityEngine.Object.Destroy(target);if(pixels!=null)UnityEngine.Object.Destroy(pixels);
            }
            yield return null;yield return null;yield return null;
        }
        private static void HullHistory(ReviewUiModel model,string starter,string produced,string encounter)
        {
            var root=StreamBytes(model);var history=model.History().Single(b=>b.EncounterId==encounter);Assert.That(history.Complete,Is.True);var seen=new HashSet<string>();
            foreach(var boundary in new[]{history.Initial}.Concat(history.Rounds.SelectMany(r=>new[]{r.Before,r.After})))foreach(var identity in boundary.ShipIdentities)
            {
                if(boundary.OwnShips.Any(s=>s.ShipId==identity.ShipId))
                {if(identity.ShipId==starter){Assert.That(identity.OwnHullVersionId,Is.EqualTo("hull.constitution-explorer.v1"));seen.Add(starter);}if(identity.ShipId==produced){Assert.That(identity.OwnHullVersionId,Is.EqualTo("hull.defiant-combat.v1"));seen.Add(produced);}}
                else Assert.That(identity.OwnHullVersionId,Is.Null);
            }
            Assert.That(seen,Does.Contain(starter));Assert.That(seen,Does.Contain(produced));Assert.That(StreamBytes(model),Is.EqualTo(root));
        }
        private static IEnumerator HullBareFactoryProbe(string disclosedHull)
        {
            var type=Type.GetType("HobbyDS9.ArtDemo.ActivityHullModel, HobbyDS9.ArtDemo",true)!;var parent=new GameObject("Isolated bare hull lifetime probe");parent.SetActive(false);
            IDisposable? first=null,second=null;GameObject? firstRoot=null,secondRoot=null;Material? a=null,b=null;
            try
            {
                int cameras=UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsInactive.Include).Length,textures=Resources.FindObjectsOfTypeAll<RenderTexture>().Length;
                var create=type.GetMethod("Create",new[]{typeof(Transform),typeof(string)})!;
                first=(IDisposable?)create.Invoke(null,new object[]{parent.transform,disclosedHull});second=(IDisposable?)create.Invoke(null,new object[]{parent.transform,disclosedHull});Assert.That(first,Is.Not.Null);Assert.That(second,Is.Not.Null);
                firstRoot=(GameObject)HullProperty(first!,"Root");secondRoot=(GameObject)HullProperty(second!,"Root");
                a=firstRoot.GetComponentInChildren<MeshRenderer>(true).sharedMaterial;b=secondRoot.GetComponentInChildren<MeshRenderer>(true).sharedMaterial;Assert.That(a,Is.Not.SameAs(b));
                var before=b.GetColor("_BaseColor");a.SetColor("_BaseColor",Color.magenta);Assert.That(b.GetColor("_BaseColor"),Is.EqualTo(before));
                Assert.That(UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsInactive.Include).Length,Is.EqualTo(cameras));Assert.That(Resources.FindObjectsOfTypeAll<RenderTexture>().Length,Is.EqualTo(textures));
                Assert.That(create.Invoke(null,new object[]{parent.transform,"unknown.hull"}),Is.Null);
                first!.Dispose();first.Dispose();Assert.That(firstRoot.activeSelf,Is.False);Assert.That(secondRoot!=null&&b!=null,Is.True);
            }
            finally{first?.Dispose();second?.Dispose();UnityEngine.Object.Destroy(parent);}
            yield return null;yield return null;Assert.That(firstRoot==null&&secondRoot==null&&a==null&&b==null,Is.True,"deferred owned roots and independent materials destroyed");
        }
        private static IEnumerator HullGalaxyComponent(ReviewUiModel model)
        {
            var option=model.Activities().BuildOptions.Single(o=>o.HullVersionId=="hull.galaxy-cruiser.v1"&&o.BlueprintRevision==1);Assert.That(option.Supported,Is.True);
            var type=Type.GetType("HobbyDS9.ArtDemo.ShipPreviewStage, HobbyDS9.ArtDemo",true)!;var active=RenderTexture.active;Texture2D? image=null;var stage=(IDisposable)Activator.CreateInstance(type,new object[]{768,480})!;
            try
            {
                type.GetMethod("ShowActivityHull")!.Invoke(stage,new object[]{option.HullVersionId});type.GetMethod("Render")!.Invoke(stage,null);
                Assert.That(HullProperty(stage,"VisualKey"),Is.EqualTo("activity.galaxy-cruiser.v1"));Assert.That(HullProperty(stage,"Availability").ToString(),Is.EqualTo("Model"));
                var texture=(RenderTexture)HullProperty(stage,"Texture");RenderTexture.active=texture;image=new Texture2D(768,480,TextureFormat.RGBA32,false);image.ReadPixels(new Rect(0,0,768,480),0,0);image.Apply();
                File.WriteAllBytes(Path.Combine(CampaignOutput(),"battle-v2-activity-galaxy-component-768x480.png"),image.EncodeToPNG());Assert.That(image.GetPixels32().Distinct().Take(33).Count(),Is.GreaterThan(32));
                type.GetMethod("OrbitReference")!.Invoke(stage,new object[]{75f,15f});type.GetMethod("ZoomReference")!.Invoke(stage,new object[]{1.1f});Assert.That(HullProperty(stage,"Texture"),Is.SameAs(texture));
                File.WriteAllText(Path.Combine(CampaignOutput(),"activity-galaxy-component.json"),JsonConvert.SerializeObject(new{componentOnly=true,notThreeBuiltShips=true,option.HullVersionId,option.DefinitionId,option.BlueprintRevision,diagnostics=type.GetMethod("CaptureArtDiagnostics")!.Invoke(stage,null)}));
            }
            finally{RenderTexture.active=active;if(image!=null)UnityEngine.Object.DestroyImmediate(image);stage.Dispose();}
            yield return null;yield return null;
        }
        [UnityTest,Timeout(600000)]
        public IEnumerator ActivityBattleHullsUseFrozenOwnIdentityAndActualControls()
        {
            var state=new PublishedReceivingState();var stack=new Stack<IEnumerator>();stack.Push(ActivityBattleHullsBody(state));Exception? failure=null;
            while(stack.Count>0)
            {bool next=false;object? value=null;var current=stack.Peek();try{next=current.MoveNext();if(next)value=current.Current;}catch(Exception e){failure=e;}if(failure!=null)break;if(!next){stack.Pop();(current as IDisposable)?.Dispose();continue;}if(value is IEnumerator nested){stack.Push(nested);continue;}yield return value;}
            state.Release.Set();while(stack.Count>0)try{(stack.Pop() as IDisposable)?.Dispose();}catch(Exception e){failure=failure??e;}
            var cleanup=System.Diagnostics.Stopwatch.StartNew();
            while(state.Holder?.IsCompleted==false||state.Entry!=null&&((bool)CheckpointField(state.Entry,"generatedCampaignBusy")!||CampaignModel(state.Entry).Busy||CheckpointField(state.Entry,"checkpointUiOperation")!=null||CheckpointField(state.Entry,"streamedUiWork")!=null))
            {if(cleanup.Elapsed.TotalSeconds>=90){directory=null;failure=failure??new TimeoutException("Unsettled battle hull worker; test namespace retained.");break;}yield return null;}
            try{File.WriteAllText(Path.Combine(CampaignOutput(),"activity-hull-latency.json"),JsonConvert.SerializeObject(state.Timings));if(state.Entry!=null&&CampaignModel(state.Entry).StreamedActivitiesCampaign&&!CampaignModel(state.Entry).Busy)File.WriteAllBytes(Path.Combine(CampaignOutput(),failure==null?"activity-hull-final-private-audit.json":"activity-hull-failed-private-audit.json"),StreamBytes(CampaignModel(state.Entry)));}catch(Exception e){CampaignDiagnosticError("activity-hull-final-audit",e);}
            if(state.Entry!=null){state.Entry.enabled=false;typeof(ReviewEntry).GetField("generatedCampaignTestDirectory",HullPrivate)!.SetValue(state.Entry,state.PriorDirectory);}
            if(state.Holder==null||state.Holder.IsCompleted){state.Entered.Dispose();state.Release.Dispose();}yield return new ExitPlayMode();if(failure!=null)System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
        }
        private IEnumerator ActivityBattleHullsBody(PublishedReceivingState state)
        {
            yield return new EnterPlayMode();yield return null;yield return null;var entry=UnityEngine.Object.FindAnyObjectByType<ReviewEntry>();state.Entry=entry;var doc=entry.GetComponent<UIDocument>();
            directory=Path.Combine(Path.GetTempPath(),"HobbyDS9-Galaxy-Engine-Activity-Hulls-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(directory);
            var field=typeof(ReviewEntry).GetField("generatedCampaignTestDirectory",HullPrivate)!;state.PriorDirectory=(string?)field.GetValue(entry);var storePath=Path.Combine(directory,"campaign");field.SetValue(entry,storePath);
            var fixture=V3Fixture("HOBBYDS9_ACTIVITY_HULL_COMBAT",ActivityHullFixtureSha);var codec=new ReviewNewtonsoftCodec();var expected=codec.EncodeStreamedActivitiesRoot(codec.DecodeStreamedActivitiesRoot(fixture));
            var store=new ReviewStreamedActivitiesSlotStore(storePath,codec);store.Write("generated-3",codec.DecodeStreamedActivitiesRoot(fixture),false);
            using(var surface=new GalaxyInputSurface(doc))
            {
                Click(doc.rootVisualElement.Q<Button>("streamed-activities-campaign-continue-generated-3"));Confirm(doc);yield return PublishedReceivingIdle(state,"activity-hulls-normal-continue");
                var model=CampaignModel(entry);Assert.That(model.StreamedActivitiesCampaign,Is.True);Assert.That(model.Current.Frame.Status.Phase,Is.EqualTo(SessionPhase.AwaitingCombat));Assert.That(StreamBytes(model),Is.EqualTo(expected));
                var own=model.Activities().Ships;var starter=own.Single(s=>s.HullVersionId=="hull.constitution-explorer.v1").ShipId;var produced=own.Single(s=>s.HullVersionId=="hull.defiant-combat.v1").ShipId;
                var provenance=StreamAudit(model);var born=provenance.PublishedBuiltShips!.Single(r=>r.ShipId==produced&&r.OwnerCivilizationId==model.Activities().ActorId);
                Assert.That(born.BlueprintRevision,Is.EqualTo(1));Assert.That(provenance.PublishedBuiltShips.Any(r=>r.ShipId==starter),Is.False,"starter is not a paid/constructed ship");
                Assert.That(provenance.ProductionHistory!.Single(r=>r.Completed&&r.ShipId==produced).Order!.QueueItemId,Is.EqualTo(born.QueueItemId));
                Assert.That(model.Current.Frame.Encounters.Count,Is.EqualTo(1));var encounter=model.Current.Frame.Encounters.Single();
                CampaignPage(doc,ReviewWorkspacePage.Combat);yield return null;yield return null;var sources=HullImportedMaterials();var disposal=new HullDisposedProof(BattleStage(doc));
                yield return StreamSaveContinue(state,doc,"activity-hull-pending");disposal.AssertDisposed();model=CampaignModel(entry);Assert.That(StreamBytes(model),Is.EqualTo(expected));CampaignPage(doc,ReviewWorkspacePage.Combat);yield return null;yield return null;
                var pure=StreamBytes(model);for(int i=0;i<V3Sizes.Length;i++)yield return HullSized(entry,doc,V3Sizes[i],"activity-orders",true);
                Assert.That(StreamBytes(model),Is.EqualTo(pure));yield return HullBareFactoryProbe("hull.defiant-combat.v1");yield return HullGalaxyComponent(model);HullSourcesUnchanged(sources);Assert.That(StreamBytes(model),Is.EqualTo(pure),"component/factory operations cannot mutate campaign");
                int rounds=0;while(!model.Current.Frame.Battle!.Complete)
                {
                    Assert.That(++rounds,Is.LessThanOrEqualTo(3));int round=model.Current.Frame.Battle.Round;
                    Click(CampaignButton(doc,"Review / commit round "+round));Confirm(doc);yield return StreamUiIdle(entry,"activity-hull-commit-round-"+round);
                    var accepted=StreamBytes(model);var acceptedFrame=model.Current.Frame;
                    if(rounds==1)
                    {
                        for(int i=0;i<V3Sizes.Length;i++)yield return HullSized(entry,doc,V3Sizes[i],"activity-round1",true,i==1,true);
                        Assert.That(StreamBytes(model),Is.EqualTo(accepted));if(!BattleStage(doc).Paused)Click(doc.rootVisualElement.Q<Button>("battle-playback-pause"));Click(doc.rootVisualElement.Q<Button>("battle-playback-pause"));Assert.That(BattleStage(doc).Paused,Is.False);
                    }
                    Click(doc.rootVisualElement.Q<Button>("combat-skip-pending-playback"));yield return null;yield return null;
                    Assert.That(model.Current.Frame,Is.SameAs(acceptedFrame));Assert.That(StreamBytes(model),Is.EqualTo(accepted),"camera/pause/skip does not change accepted round");
                    if(!model.Current.Frame.Battle.Complete)Click(CampaignButton(doc,"Plan next round"));
                }
                Click(CampaignButton(doc,"Confirm campaign aftermath"));Confirm(doc);yield return StreamUiIdle(entry,"activity-hull-aftermath");
                Assert.That(model.Current.Frame.Status.Phase,Is.EqualTo(SessionPhase.Planning));HullHistory(model,starter,produced,encounter);HullSourcesUnchanged(sources);
                Click(doc.rootVisualElement.Q<Button>("combat-tab-report"));yield return null;yield return null;
                Assert.That(doc.rootVisualElement.Query<Label>().ToList().Any(l=>l.text.StartsWith("Battle statistics",StringComparison.Ordinal)&&l.text.Contains("turn 39")),Is.True,"actual retained encounter report");
                yield return HullSized(entry,doc,new Vector2Int(3440,1440),"activity-history",false);
                var aftermath=StreamBytes(model);var report=JsonConvert.SerializeObject(model.History());yield return StreamSaveContinue(state,doc,"activity-hull-aftermath");model=CampaignModel(entry);
                Assert.That(StreamBytes(model),Is.EqualTo(aftermath));Assert.That(JsonConvert.SerializeObject(model.History()),Is.EqualTo(report));HullHistory(model,starter,produced,encounter);
                CampaignPage(doc,ReviewWorkspacePage.Combat);Click(doc.rootVisualElement.Q<Button>("combat-tab-report"));yield return null;yield return null;yield return HullSized(entry,doc,new Vector2Int(1280,720),"activity-continued-history",false);
                Assert.That(StreamBytes(model),Is.EqualTo(aftermath));HullSourcesUnchanged(sources);
                Assert.That(Directory.GetFiles(CampaignOutput(),"battle-v2-activity-*.png").Length,Is.EqualTo(9));
                File.WriteAllText(Path.Combine(CampaignOutput(),"activity-hull-results.json"),JsonConvert.SerializeObject(new{passed=true,fixtureSha256=ActivityHullFixtureSha,starter,produced,encounter,rounds,captures=9,galaxyComponentOnly=true,threeBuiltShips=false,normalTypedContinue=true,pendingAndAftermathRootExact=true,nativeInput=false,shutdownCauseClaim=false}));
            }
        }
    }
}
