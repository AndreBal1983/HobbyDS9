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
        private static string[] FeatherFields(GeneratedGalaxyV3Background renderer)
        {
            var fields=(Array)typeof(GeneratedGalaxyV3Background).GetField("displayed",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(renderer)!;
            return fields.Cast<object>().Select(f=>((CampaignNebulaVisualProxy)f.GetType().GetField("Proxy",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(f)!).FeatureId).ToArray();
        }
        private static void FeatherMask(UIDocument doc,string name)
        {
            var renderer=V3Renderer(doc);var mask=(Texture2D)typeof(GeneratedGalaxyV3Background).GetField("mask",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(renderer)!;
            var pixels=mask.GetRawTextureData<byte>().ToArray();int partial=pixels.Count(b=>b>0&&b<255),opaque=pixels.Count(b=>b==255);
            File.WriteAllText(Path.Combine(CampaignOutput(),"feather-mask-"+name+".json"),JsonConvert.SerializeObject(new{name,mask.width,mask.height,partial,opaque,levels=pixels.Where(b=>b>0&&b<255).Distinct().OrderBy(b=>b).ToArray(),renderer.LocalStatus,renderer.VisibleFieldCount,renderer.ParticleCount,renderer.LocalFetchUpperBound,renderer.LocalSize,fields=FeatherFields(renderer),claim="Actual uploaded renderer-owned R8 resource; not a GPU sampler readback"}));
            Assert.That(renderer.LocalStatus,Is.EqualTo("charted-union"));Assert.That(partial,Is.GreaterThan(10));Assert.That(opaque,Is.GreaterThan(10));
            Assert.That(pixels.Where(b=>b>0&&b<255).Distinct().Count(),Is.GreaterThanOrEqualTo(4));
            Assert.That(renderer.VisibleFieldCount,Is.InRange(1,8));Assert.That(renderer.ParticleCount,Is.LessThanOrEqualTo(256));Assert.That(renderer.LocalFetchUpperBound,Is.LessThanOrEqualTo(40000000));
        }
        private static (double x,double y,string node) FeatherKnownEdge(GeneratedCampaignPresentation pair)
        {
            var regions=pair.KnownMap.Regions;var proxy=pair.Astronomy.Nebulae.First();
            // Only disclosed charted polygons enter this framing choice. Opposite
            // union-side probes discard internal edges, including partial seams.
            var candidates=regions.SelectMany(region=>region.Boundary.Select((a,i)=>new{region.NodeId,a,b=region.Boundary[(i+1)%region.Boundary.Count]}))
                .Where(e=>e.a.X!=e.b.X&&e.a.Y!=e.b.Y).Select(e=>{
                    double x=(e.a.X+(double)e.b.X)/2,y=(e.a.Y+(double)e.b.Y)/2,dx=e.b.X-(double)e.a.X,dy=e.b.Y-(double)e.a.Y,length=Math.Sqrt(dx*dx+dy*dy);
                    double nx=-dy/length*.01,ny=dx/length*.01;
                    bool left=regions.Any(r=>V3Inside(r,x+nx,y+ny)),right=regions.Any(r=>V3Inside(r,x-nx,y-ny));
                    double score=Math.Pow((x-proxy.CentreX)/proxy.VisualReferenceRadiusX,2)+Math.Pow((y-proxy.CentreY)/proxy.VisualReferenceRadiusY,2);
                    return new{x,y,e.NodeId,edge=left!=right,score};})
                .Where(e=>e.edge).OrderBy(e=>e.score).ThenBy(e=>e.NodeId,StringComparer.Ordinal).ToArray();
            Assert.That(candidates,Is.Not.Empty,"actual known map must offer an oblique union edge; no synthetic substitute");
            var selected=candidates.First();Assert.That(selected.score,Is.LessThan(9),"selected edge must be near the lawful visual proxy, not an empty remote boundary");
            return(selected.x,selected.y,selected.NodeId);
        }
        private static IEnumerator FeatherSized(ReviewEntry entry,UIDocument doc,Vector2Int size,string name,bool matchedPair,bool depth)
        {
            var prior=doc.panelSettings.targetTexture;var priorActive=RenderTexture.active;var width=doc.rootVisualElement.style.width;var height=doc.rootVisualElement.style.height;
            var target=new RenderTexture(size.x,size.y,24,RenderTextureFormat.ARGB32);bool succeeded=false;
            try
            {
                Assert.That(target.Create(),Is.True);doc.panelSettings.targetTexture=target;doc.rootVisualElement.style.width=size.x;doc.rootVisualElement.style.height=size.y;
                yield return null;yield return null;yield return null;
                var model=CampaignModel(entry);var map=doc.rootVisualElement.Q("playable-known-map");Assert.That(map?.panel,Is.Not.Null);
                Click(doc.rootVisualElement.Q<Button>("map-fit"));yield return null;yield return null;
                var proxy=model.GeneratedPresentation().Astronomy.Nebulae.First();var view=model.GeneratedViewport;
                if(!matchedPair)
                {
                    var edge=FeatherKnownEdge(model.GeneratedPresentation());
                    double rx=proxy.VisualReferenceRadiusX*.85,ry=proxy.VisualReferenceRadiusY*.85;
                    view.FitWorldBounds(edge.x-rx,edge.y-ry,edge.x+rx,edge.y+ry);
                    File.WriteAllText(Path.Combine(CampaignOutput(),"feather-edge-"+name+".json"),JsonConvert.SerializeObject(new{edge.x,edge.y,edge.node,proxy.FeatureId,depth,label="Actual known-polygon edge selected near lawful actor-known proxy; closeup appearance only"}));
                }
                else view.Pan(map!.contentRect.width/2-view.X(proxy.CentreX),map.contentRect.height/2-view.Y(proxy.CentreY));
                view.Pan(.375,-.625);using(var geometry=GeometryChangedEvent.GetPooled(map!.layout,map.layout)){geometry.target=map;map.SendEvent(geometry);}
                var depthButton=doc.rootVisualElement.Q<Button>("campaign-visual-depth");if(depthButton.text!=(depth?"Depth: on":"Depth: off")){Click(depthButton);yield return null;}
                Assert.That(doc.rootVisualElement.Q<Button>("campaign-visual-depth").text,Is.EqualTo(depth?"Depth: on":"Depth: off"));
                yield return V3Settled(doc);V3Geometry(entry,doc,name);FeatherMask(doc,name);
                var renderer=V3Renderer(doc);var fields=FeatherFields(renderer);int renders=renderer.RenderCount;
                yield return null;yield return null;yield return null;Assert.That(renderer.RenderCount,Is.EqualTo(renders),"settled actual UI has no idle rendering");CollectionAssert.AreEqual(fields,FeatherFields(renderer));
                V3Save(target,"feather-"+name+"-on");
                if(matchedPair)
                {
                    var on=V3Read(target);Click(doc.rootVisualElement.Q<Button>("campaign-visual-effects"));yield return V3Settled(doc);
                    Assert.That(V3Renderer(doc).VisibleFieldCount,Is.Zero);Assert.That(V3Renderer(doc).ParticleCount,Is.Zero);Assert.That(V3Renderer(doc).NeedsTransition,Is.False);
                    V3Save(target,"feather-"+name+"-macro-only");V3Exterior(entry,doc,target,on,V3Read(target),"feather-"+name);
                    Click(doc.rootVisualElement.Q<Button>("campaign-visual-effects"));yield return V3Settled(doc);CollectionAssert.AreEqual(fields,FeatherFields(V3Renderer(doc)));
                }
                succeeded=true;
            }
            finally
            {
                if(!succeeded)V3Audit(entry,"feather-failed-"+name);
                doc.panelSettings.targetTexture=prior;doc.rootVisualElement.style.width=width;doc.rootVisualElement.style.height=height;RenderTexture.active=priorActive;
                target.Release();UnityEngine.Object.Destroy(target);
            }
            yield return null;yield return null;yield return V3Settled(doc);
        }
        [UnityTest,Timeout(600000)] public IEnumerator NebulaFeatherPreservesActualChartedExteriorAcrossThreeSizes()
        {
            var state=new PublishedReceivingState();var stack=new Stack<IEnumerator>();stack.Push(NebulaFeatherReceivingBody(state));Exception? failure=null;
            while(stack.Count>0)
            {
                bool next=false;object? value=null;var current=stack.Peek();
                try{next=current.MoveNext();if(next)value=current.Current;}catch(Exception error){failure=error;}
                if(failure!=null)break;
                if(!next){stack.Pop();try{(current as IDisposable)?.Dispose();}catch(Exception error){failure=error;break;}continue;}
                if(value is IEnumerator nested){stack.Push(nested);continue;}yield return value;
            }
            state.Release.Set();
            while(stack.Count>0)
            {try{(stack.Pop() as IDisposable)?.Dispose();}catch(Exception error){failure=failure??error;CampaignDiagnosticError("feather-iterator-cleanup",error);}}
            var cleanup=System.Diagnostics.Stopwatch.StartNew();
            while(state.Entry!=null&&((bool)CheckpointField(state.Entry,"generatedCampaignBusy")!||CampaignModel(state.Entry).Busy||CheckpointField(state.Entry,"checkpointUiOperation")!=null||state.Holder?.IsCompleted==false))
            {
                if(cleanup.Elapsed.TotalSeconds>=90)
                {
                    try{File.WriteAllText(Path.Combine(CampaignOutput(),"feather-retained-temporary-namespace.txt"),directory??"not assigned");}catch(Exception error){CampaignDiagnosticError("feather-retained-namespace",error);}
                    directory=null;failure=failure??new TimeoutException("Unsettled nebula receiving operation; temporary namespace retained.");break;
                }
                yield return null;
            }
            try{if(state.Entry!=null)V3Audit(state.Entry,failure==null?"feather-final":"feather-failed");}catch(Exception error){CampaignDiagnosticError("feather-final-audit",error);}
            if(state.Entry!=null)
            {
                try{state.Entry.enabled=false;}catch(Exception error){failure=failure??error;}
                try{typeof(ReviewEntry).GetField("generatedCampaignTestDirectory",BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(state.Entry,state.PriorDirectory);}catch(Exception error){failure=failure??error;}
            }
            if(state.Holder==null||state.Holder.IsCompleted){state.Entered.Dispose();state.Release.Dispose();}
            yield return new ExitPlayMode();
            if(failure!=null)System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
        }
        private IEnumerator NebulaFeatherReceivingBody(PublishedReceivingState state)
        {
            yield return new EnterPlayMode();yield return null;yield return null;
            var entry=UnityEngine.Object.FindAnyObjectByType<ReviewEntry>();state.Entry=entry;var doc=entry.GetComponent<UIDocument>();
            var overrideField=typeof(ReviewEntry).GetField("generatedCampaignTestDirectory",BindingFlags.Instance|BindingFlags.NonPublic)!;state.PriorDirectory=(string?)overrideField.GetValue(entry);
            directory=Path.Combine(Path.GetTempPath(),"HobbyDS9-Galaxy-Engine-NebulaFeather-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(directory);
            var path=Path.Combine(directory,"campaign");overrideField.SetValue(entry,path);
            var codec=new ReviewNewtonsoftCodec();var store=new ReviewStaticAstronomyCampaignSlotStore(path,codec);
            store.Write("generated-2",codec.DecodeStaticAstronomyCampaignRoot(V3Fixture("HOBBYDS9_V3_EXPLORE","d7b332383b726607209067a5c89085a4979814b12c35a5fd717a41ebd3ecdddc")),false);
            int baseline=GeneratedGalaxyV3Background.LiveInstances;int priorImages=Directory.GetFiles(CampaignOutput(),"v3-feather-*.png").Length;
            using(var surface=new GalaxyInputSurface(doc))
            {
                yield return null;yield return null;
                doc.rootVisualElement.Q<Foldout>("earlier-generated-campaign-menu").value=true;yield return null;
                var load=doc.rootVisualElement.Q<Button>("generated-campaign-continue-generated-2");Assert.That(load.visible,Is.True);Click(load);Confirm(doc);yield return WaitCampaign(entry,"nebula-feather-explore32");
                var model=CampaignModel(entry);var pair=model.GeneratedPresentation();Assert.That(pair.Frame.Status.Turn,Is.EqualTo(32));Assert.That(pair.Astronomy.Nebulae.Count,Is.EqualTo(3));
                var before=codec.EncodeStaticAstronomyCampaignRoot(CampaignPrivateAudit(model));CampaignPage(doc,ReviewWorkspacePage.Map);yield return V3Settled(doc);
                foreach(var size in V3Sizes)yield return FeatherSized(entry,doc,size,"regional-"+size.x,true,true);
                yield return FeatherSized(entry,doc,new Vector2Int(1920,1080),"oblique-closeup-1920-depth-on",false,true);
                yield return FeatherSized(entry,doc,new Vector2Int(3440,1440),"oblique-closeup-3440-depth-off",false,false);
                Assert.That(codec.EncodeStaticAstronomyCampaignRoot(CampaignPrivateAudit(CampaignModel(entry))),Is.EqualTo(before),"visual-only framing/toggles preserve whole authoritative campaign");
                var map=doc.rootVisualElement.Q("playable-known-map");var parent=map.parent;int index=parent.IndexOf(map);var old=V3Renderer(doc);map.RemoveFromHierarchy();yield return null;
                Assert.That(GeneratedGalaxyV3Background.LiveInstances,Is.EqualTo(baseline));Assert.That(map.Q<Image>("campaign-v3-content-background").userData,Is.Null);
                Assert.Throws<InvalidOperationException>(()=>old.Render(model.GeneratedViewport,400,300,true,true));parent.Insert(index,map);yield return null;yield return V3Settled(doc);Assert.That(GeneratedGalaxyV3Background.LiveInstances,Is.EqualTo(baseline+1));
                bool current=true;using(var probe=new GeneratedGalaxyV3Background(pair,()=>current))
                {
                    Assert.That(probe.Render(model.GeneratedViewport,320,180,true,true),Is.True);int count=probe.RenderCount;var texture=probe.Texture;current=false;
                    Assert.Throws<InvalidOperationException>(()=>probe.Render(model.GeneratedViewport,320,180,true,true));Assert.That(probe.RenderCount,Is.EqualTo(count));Assert.That(probe.Texture,Is.SameAs(texture));
                }
                Assert.That(Directory.GetFiles(CampaignOutput(),"v3-feather-*.png").Length-priorImages,Is.EqualTo(8),"six exact matched regional frames plus two appearance-only depth closeups");
                File.WriteAllText(Path.Combine(CampaignOutput(),"nebula-feather-summary.json"),JsonConvert.SerializeObject(new{fixture="Official actor-known Explore32, not native campaign play",frames=8,regionalMatchedPairs=3,appearanceOnlyCloseups=2,exactWholeCampaignUnchanged=true,exteriorClaim="Actual UI-composed8-bit Color32 equality, not unquantized radiance",syntheticImages=0}));
                Click(doc.rootVisualElement.Q<Button>("demo-menu"));yield return null;Assert.That(GeneratedGalaxyV3Background.LiveInstances,Is.EqualTo(baseline));
            }
        }
    }
}

