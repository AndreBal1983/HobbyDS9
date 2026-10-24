#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using HobbyDS9.Contracts;
using HobbyDS9.TacticalReviewClient;
using Newtonsoft.Json;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace HobbyDS9.UnityReview.Tests
{
    public sealed partial class GeneratedGalaxyEngineTests
    {
        private static readonly Vector2Int[] V3Sizes={new Vector2Int(1280,720),new Vector2Int(1920,1080),new Vector2Int(3440,1440)};
        private static GeneratedGalaxyV3Background V3Renderer(UIDocument doc)
        {
            var image=doc.rootVisualElement.Q<Image>("campaign-v3-content-background");
            Assert.That(image,Is.Not.Null);Assert.That(image.pickingMode,Is.EqualTo(PickingMode.Ignore));
            Assert.That(image.userData,Is.TypeOf<GeneratedGalaxyV3Background>(),doc.rootVisualElement.Q<Label>("campaign-visual-status")?.tooltip);
            return (GeneratedGalaxyV3Background)image.userData;
        }
        private static IEnumerator V3Settled(UIDocument doc)
        {
            var timer=System.Diagnostics.Stopwatch.StartNew();yield return null;yield return null;
            while(V3Renderer(doc).NeedsTransition){Assert.That(timer.Elapsed.TotalSeconds,Is.LessThan(3),"bounded residency fade");yield return null;}
            yield return null;yield return null;
        }
        private static void V3Audit(ReviewEntry entry,string phase)
        {
            try
            {
                var model=CampaignModel(entry);if(!model.StaticAstronomyCampaign||model.Busy||model.Current.HasPendingWork)return;
                var pair=model.GeneratedPresentation();File.WriteAllText(Path.Combine(CampaignOutput(),"v3-actor-"+phase+".json"),JsonConvert.SerializeObject(new{pair.Actor,pair.Generation,Map=pair.KnownMap,pair.Astronomy}));
                File.WriteAllBytes(Path.Combine(CampaignOutput(),"v3-private-audit-"+phase+".json"),new ReviewNewtonsoftCodec().EncodeStaticAstronomyCampaignRoot(CampaignPrivateAudit(model)));
            }
            catch(Exception error){CampaignDiagnosticError("v3-audit-"+phase,error);}
        }
        private static byte[] V3Fixture(string variable,string hash)
        {
            var path=Environment.GetEnvironmentVariable(variable);Assert.That(path,Is.Not.Null.And.Not.Empty);
            var bytes=File.ReadAllBytes(path!);Assert.That(bytes.Length,Is.InRange(1,ReviewStaticAstronomyCampaignSlotStore.MaximumBytes));
            using(var sha=SHA256.Create())Assert.That(BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-","").ToLowerInvariant(),Is.EqualTo(hash));
            return bytes;
        }
        private static Color32[] V3Read(RenderTexture target)
        {
            var prior=RenderTexture.active;var image=new Texture2D(target.width,target.height,TextureFormat.RGBA32,false,true);
            try{RenderTexture.active=target;image.ReadPixels(new Rect(0,0,target.width,target.height),0,0);image.Apply();return image.GetPixels32();}
            finally{RenderTexture.active=prior;UnityEngine.Object.Destroy(image);}
        }
        private static void V3Save(RenderTexture target,string name,bool opaqueDiagnostic=false)
        {
            var image=new Texture2D(target.width,target.height,TextureFormat.RGBA32,false,true);
            try{var pixels=V3Read(target);if(opaqueDiagnostic)for(int i=0;i<pixels.Length;i++)pixels[i].a=255;image.SetPixels32(pixels);image.Apply();File.WriteAllBytes(Path.Combine(CampaignOutput(),"v3-"+name+".png"),image.EncodeToPNG());}
            finally{UnityEngine.Object.Destroy(image);}
        }
        private static void V3Geometry(ReviewEntry entry,UIDocument doc,string name)
        {
            var map=doc.rootVisualElement.Q("playable-known-map");var image=map.Q<Image>("campaign-v3-content-background");var lines=map.Q("playable-known-lines");
            var vp=CampaignModel(entry).GeneratedViewport;var known=CampaignModel(entry).GeneratedPresentation().KnownMap;
            foreach(var point in new[]{Vector2.zero,new Vector2(map.contentRect.width,0),new Vector2(0,map.contentRect.height),map.contentRect.size})
            {
                var expected=map.LocalToWorld(map.contentRect.position+point);var quad=image.LocalToWorld(point);var paint=lines.LocalToWorld(point);
                object XY(Vector2 p)=>new{x=p.x,y=p.y};object Box(Rect r)=>new{x=r.x,y=r.y,width=r.width,height=r.height};
                File.AppendAllText(Path.Combine(CampaignOutput(),"v3-planes.jsonl"),JsonConvert.SerializeObject(new{name,point=XY(point),expected=XY(expected),quad=XY(quad),paint=XY(paint),content=Box(map.contentRect),world=Box(map.worldBound)})+Environment.NewLine);
                Assert.That(Vector2.Distance(expected,quad),Is.LessThan(.01));Assert.That(Vector2.Distance(expected,paint),Is.LessThan(.01));
            }
            foreach(var star in known.Features.Where(f=>f.Kind==GalaxyEnvironmentFeatureKind.Star))
            {
                var point=new Vector2((float)vp.X(star.Position.X),(float)vp.Y(star.Position.Y));
                var expected=map.LocalToWorld(map.contentRect.position+point);var actual=map.Q<Button>("map-star-"+star.NodeId).worldBound.center;
                Assert.That(Vector2.Distance(expected,actual),Is.LessThan(2),star.FeatureId);
            }
        }
        private static bool V3Inside(GeneratedGalaxyRegionDto region,double x,double y)
        {
            // Independent closed convex-half-plane oracle; no production mask call.
            for(int i=0;i<region.Boundary.Count;i++)
            {var a=region.Boundary[i];var b=region.Boundary[(i+1)%region.Boundary.Count];if((b.X-(double)a.X)*(y-a.Y)-(b.Y-(double)a.Y)*(x-a.X)<0)return false;}
            return true;
        }
        private static void V3Exterior(ReviewEntry entry,UIDocument doc,RenderTexture target,Color32[] enabled,Color32[] macroOnly,string name)
        {
            var map=doc.rootVisualElement.Q("playable-known-map");var vp=CampaignModel(entry).GeneratedViewport;
            var regions=CampaignModel(entry).GeneratedPresentation().KnownMap.Regions;
            var boxes=regions.Select(r=>new{Region=r,Left=r.Boundary.Min(p=>p.X),Right=r.Boundary.Max(p=>p.X),Top=r.Boundary.Min(p=>p.Y),Bottom=r.Boundary.Max(p=>p.Y)}).ToArray();
            Rect content=new Rect(map.LocalToWorld(map.contentRect.position),map.contentRect.size);int changed=0,exteriorChanged=0,samples=0;
            for(int y=Math.Max(0,(int)Math.Ceiling(content.yMin));y<Math.Min(target.height,(int)Math.Floor(content.yMax));y++)
            for(int x=Math.Max(0,(int)Math.Ceiling(content.xMin));x<Math.Min(target.width,(int)Math.Floor(content.xMax));x++)
            {
                int index=x+(target.height-1-y)*target.width;samples++;if(enabled[index].Equals(macroOnly[index]))continue;changed++;
                var local=map.WorldToLocal(new Vector2(x+.5f,y+.5f))-map.contentRect.position;double wx=vp.WorldX(local.x),wy=vp.WorldY(local.y);
                if(!boxes.Any(b=>wx>=b.Left&&wx<=b.Right&&wy>=b.Top&&wy<=b.Bottom&&V3Inside(b.Region,wx,wy)))exteriorChanged++;
            }
            File.WriteAllText(Path.Combine(CampaignOutput(),"v3-exterior-"+name+".json"),JsonConvert.SerializeObject(new{name,samples,changed,exteriorChanged,claim="Actual composed 8-bit Color32 UI equality outside charted union, including local shadows; no tolerance; not unquantized-radiance proof"}));
            Assert.That(samples,Is.GreaterThan(1000));Assert.That(changed,Is.GreaterThan(0),"actual local contribution must be visible; blank comparisons cannot pass");Assert.That(exteriorChanged,Is.Zero,"final UI local contribution outside charted union");
        }
        private static IEnumerator V3Sized(ReviewEntry entry,UIDocument doc,Vector2Int size,string name,bool pair,bool regional,bool closeup=false)
        {
            var prior=doc.panelSettings.targetTexture;var w=doc.rootVisualElement.style.width;var h=doc.rootVisualElement.style.height;
            var target=new RenderTexture(size.x,size.y,24,RenderTextureFormat.ARGB32);bool success=false;
            try
            {
                Assert.That(target.Create(),Is.True);doc.panelSettings.targetTexture=target;doc.rootVisualElement.style.width=size.x;doc.rootVisualElement.style.height=size.y;
                yield return null;yield return null;yield return null;
                var map=doc.rootVisualElement.Q("playable-known-map");
                if(map!=null)
                {
                    Click(doc.rootVisualElement.Q<Button>(regional?"map-fit":"map-fit-galaxy"));yield return null;
                    if(regional)
                    {
                        // Lawful known proxy centre, not a full-root feature; fractional pan exercises filter support.
                        var proxy=CampaignModel(entry).GeneratedPresentation().Astronomy.Nebulae.First();var vp=CampaignModel(entry).GeneratedViewport;
                        if(closeup)vp.FitWorldBounds(proxy.CentreX-proxy.VisualReferenceRadiusX,proxy.CentreY-proxy.VisualReferenceRadiusY,proxy.CentreX+proxy.VisualReferenceRadiusX,proxy.CentreY+proxy.VisualReferenceRadiusY);
                        vp.Pan(map.contentRect.width/2-vp.X(proxy.CentreX)+.375,map.contentRect.height/2-vp.Y(proxy.CentreY)-.625);
                        using(var e=GeometryChangedEvent.GetPooled(map.layout,map.layout)){e.target=map;map.SendEvent(e);}yield return null;
                    }
                    yield return V3Settled(doc);V3Geometry(entry,doc,name);
                }
                else
                {
                    Assert.That(doc.rootVisualElement.Query<Label>().ToList().Any(l=>l.text=="HobbyDS9 — Start / Continue"),Is.True);
                    var seed=doc.rootVisualElement.Q<TextField>("generated-campaign-seed");Assert.That(seed.value,Is.EqualTo("42"));Assert.That(seed.worldBound.width,Is.GreaterThan(40));
                    Assert.That(doc.rootVisualElement.Q<Foldout>("other-campaign-profiles").value,Is.False);
                    foreach(var id in new[]{"generated-campaign-start","generated-campaign-seed","generated-campaign-continue-generated-1"})
                    {var box=doc.rootVisualElement.Q(id).worldBound;Assert.That(box.xMin,Is.GreaterThanOrEqualTo(0));Assert.That(box.xMax,Is.LessThanOrEqualTo(size.x));Assert.That(box.yMin,Is.GreaterThanOrEqualTo(0));Assert.That(box.yMax,Is.LessThanOrEqualTo(size.y));}
                }
                V3Save(target,name+"-"+size.x);var on=pair?V3Read(target):null;
                if(pair)
                {
                    var renderer=V3Renderer(doc);int renders=renderer.RenderCount;yield return null;yield return null;yield return null;Assert.That(renderer.RenderCount,Is.EqualTo(renders),"actual UI scheduler submits nothing while settled");
                    Click(doc.rootVisualElement.Q<Button>("campaign-visual-effects"));yield return V3Settled(doc);
                    Assert.That(renderer.VisibleFieldCount,Is.Zero);Assert.That(renderer.ParticleCount,Is.Zero);Assert.That(renderer.NeedsTransition,Is.False);
                    V3Save(target,name+"-macro-only-"+size.x);V3Exterior(entry,doc,target,on!,V3Read(target),name+"-"+size.x);
                    Click(doc.rootVisualElement.Q<Button>("campaign-visual-effects"));yield return V3Settled(doc);
                }
                success=true;
            }
            finally
            {
                if(!success)V3Audit(entry,"capture-"+name+"-"+size.x);
                doc.panelSettings.targetTexture=prior;doc.rootVisualElement.style.width=w;doc.rootVisualElement.style.height=h;target.Release();UnityEngine.Object.Destroy(target);
            }
            yield return null;yield return null;yield return null;
        }
        private static byte[] V3PngRows(byte[] png)
        {
            int Big(int i)=>(png[i]<<24)|(png[i+1]<<16)|(png[i+2]<<8)|png[i+3];
            Assert.That(Big(16),Is.EqualTo(2048));Assert.That(Big(20),Is.EqualTo(2048));Assert.That(png[24],Is.EqualTo(8));Assert.That(png[25],Is.EqualTo(6));
            using(var compressed=new MemoryStream())
            {
                for(int at=8;at<png.Length;){int length=Big(at);if(png[at+4]==73&&png[at+5]==68&&png[at+6]==65&&png[at+7]==84)compressed.Write(png,at+8,length);at+=length+12;}
                var zlib=compressed.ToArray();using(var raw=new MemoryStream(zlib,2,zlib.Length-6))using(var inflate=new DeflateStream(raw,CompressionMode.Decompress))
                {
                    var rows=new byte[2048*2048*4];var line=new byte[8192];
                    for(int y=0;y<2048;y++)
                    {
                        int filter=inflate.ReadByte();int count=0;while(count<line.Length){int n=inflate.Read(line,count,line.Length-count);Assert.That(n,Is.GreaterThan(0));count+=n;}
                        for(int x=0;x<8192;x++)
                        {int a=x<4?0:rows[y*8192+x-4],b=y==0?0:rows[(y-1)*8192+x],c=y==0||x<4?0:rows[(y-1)*8192+x-4];int value=0;
                            if(filter==1)value=a;else if(filter==2)value=b;else if(filter==3)value=(a+b)/2;else if(filter==4){int p=a+b-c,pa=Math.Abs(p-a),pb=Math.Abs(p-b),pc=Math.Abs(p-c);value=pa<=pb&&pa<=pc?a:pb<=pc?b:c;}else Assert.That(filter,Is.Zero);
                            rows[y*8192+x]=unchecked((byte)(line[x]+value));}
                    }
                    return rows;
                }
            }
        }
        private static void V3ImportProbe()
        {
            const string path="Assets/HobbyDS9/Review/Resources/GalaxyPresentationV3/galaxy.png";
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);Assert.That(importer.sRGBTexture,Is.False);Assert.That(importer.alphaIsTransparency,Is.False);
            Assert.That(importer.mipmapEnabled,Is.True);Assert.That(importer.isReadable,Is.False);Assert.That(importer.textureCompression,Is.EqualTo(TextureImporterCompression.Uncompressed));
            var atlas=Resources.Load<Texture2D>("GalaxyPresentationV3/galaxy");Assert.That(GraphicsFormatUtility.IsSRGBFormat(atlas.graphicsFormat),Is.False);Assert.That(atlas.mipmapCount,Is.EqualTo(12));
            Assert.That(atlas.format,Is.EqualTo(TextureFormat.RGBA32));Assert.That(atlas.wrapMode,Is.EqualTo(TextureWrapMode.Clamp));Assert.That(atlas.filterMode,Is.EqualTo(FilterMode.Trilinear));
            var bytes=File.ReadAllBytes(Path.Combine(UnityEngine.Application.dataPath,"HobbyDS9/Review/Resources/GalaxyPresentationV3/galaxy.png"));var rows=V3PngRows(bytes);
            var rt=new RenderTexture(1,1,0,RenderTextureFormat.ARGBHalf,RenderTextureReadWrite.Linear);var mat=new Material(Resources.Load<Shader>("GalaxyPresentationV3/Presentation"));
            var prior=RenderTexture.active;var probe=new Texture2D(1,1,TextureFormat.RGBAFloat,false,true);int nonzero=0;
            try
            {
                Assert.That(rt.Create(),Is.True);mat.SetTexture("_Atlas",atlas);
                foreach(var p in new[]{new Vector2Int(1024,1024),new Vector2Int(700,900),new Vector2Int(1200,1150),new Vector2Int(900,700),new Vector2Int(1150,1200)})
                {
                    mat.SetVector("_ProbeUv",new Vector4((p.x+.5f)/2048,1-(p.y+.5f)/2048,0,0));Graphics.Blit(null,rt,mat,3);RenderTexture.active=rt;
                    probe.ReadPixels(new Rect(0,0,1,1),0,0);probe.Apply();var actual=probe.GetPixel(0,0);int i=(p.x+p.y*2048)*4;
                    for(int c=0;c<4;c++){Assert.That(actual[c],Is.EqualTo(rows[i+c]/255f).Within(1f/2048),"linear channel/Y probe "+p+" channel"+c);if(rows[i+c]>0)nonzero++;}
                }
                Assert.That(nonzero,Is.GreaterThan(4));
            }
            finally{RenderTexture.active=prior;rt.Release();UnityEngine.Object.Destroy(rt);UnityEngine.Object.Destroy(mat);UnityEngine.Object.Destroy(probe);}
        }
        private static void V3SyntheticEight(GeneratedCampaignPresentation actual,ReviewNewtonsoftCodec codec)
        {
            // Explicit synthetic workload, not an actor observation or a player input.
            // Every centre is the SAME already-charted lawful proxy centre.
            var at=actual.Astronomy.Nebulae.First();var map=actual.KnownMap;
            var proxies=Enumerable.Range(0,8).Select(i=>new CampaignNebulaVisualProxy("synthetic-known-"+i,at.CentreX,at.CentreY,at.VisualReferenceRadiusX,at.VisualReferenceRadiusY));
            var astronomy=new CampaignStaticAstronomySnapshot(actual.Actor,actual.MapContentHash,actual.Frame.Status.Turn,actual.Frame.Status.CampaignRevision,proxies);
            var constructor=typeof(GeneratedCampaignPresentation).GetConstructors(BindingFlags.Instance|BindingFlags.NonPublic).Single();
            var synthetic=(GeneratedCampaignPresentation)constructor.Invoke(new object[]{actual.Frame,map,astronomy,codec});
            var view=new GeneratedGalaxyViewport();view.SetMap(map.Width,map.Height);view.Resize(1920,1080);view.FitWorldBounds(at.CentreX-at.VisualReferenceRadiusX*3,at.CentreY-at.VisualReferenceRadiusY*3,at.CentreX+at.VisualReferenceRadiusX*3,at.CentreY+at.VisualReferenceRadiusY*3);
            bool current=true;
            using(var renderer=new GeneratedGalaxyV3Background(synthetic,()=>current))
            {
                Assert.That(renderer.Render(view,1920,1080,true,true),Is.True);Assert.That(renderer.VisibleFieldCount,Is.EqualTo(8));Assert.That(renderer.LocalFetchUpperBound,Is.LessThanOrEqualTo(40000000));
                Assert.That(renderer.Texture!.width,Is.EqualTo(960));Assert.That(renderer.Texture.height,Is.EqualTo(540));Assert.That(renderer.ParticleCount,Is.LessThanOrEqualTo(256));
                var uploaded=typeof(GeneratedGalaxyV3Background).GetField("assets",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(renderer)!;
                // Actual uploaded resource properties; not a GPU sampler-binding query.
                var volume=(Texture3D)uploaded.GetType().GetField("Volume",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(uploaded)!;
                Assert.That(volume.mipmapCount,Is.EqualTo(8));Assert.That(GraphicsFormatUtility.IsSRGBFormat(volume.graphicsFormat),Is.False);
                var mesh=(Mesh)typeof(GeneratedGalaxyV3Background).GetField("mesh",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(renderer)!;var vertices=mesh.vertices;
                for(int i=0;i<renderer.ParticleCount;i++)
                {Assert.That((vertices[i*4+1].x-vertices[i*4].x)*renderer.LocalSize.x/2,Is.LessThanOrEqualTo(32.001),"full quad width, final float projection");Assert.That((vertices[i*4].y-vertices[i*4+3].y)*renderer.LocalSize.y/2,Is.LessThanOrEqualTo(32.001),"full quad height");}
                V3Save(renderer.Texture,"SYNTHETIC-eight-known-fields-not-campaign");
                File.WriteAllText(Path.Combine(CampaignOutput(),"v3-synthetic-workload.json"),JsonConvert.SerializeObject(new{label="Synthetic eight-field overlap; no campaign observation claim",renderer.VisibleFieldCount,renderer.ParticleCount,renderer.LocalFetchUpperBound,renderer.LocalSize,renderer.LocalStatus,MacroWidth=renderer.Texture.width,MacroHeight=renderer.Texture.height,ParticleFullQuadPixelCap=32,ParticleFragmentUpperBound=renderer.ParticleCount*32*32}));
                int renders=renderer.RenderCount;Assert.That(renderer.Render(view,1920,1080,true,true),Is.False);Assert.That(renderer.RenderCount,Is.EqualTo(renders));
                current=false;var texture=renderer.Texture;Assert.Throws<InvalidOperationException>(()=>renderer.Render(view,1920,1080,true,true));Assert.That(renderer.RenderCount,Is.EqualTo(renders));Assert.That(renderer.Texture,Is.SameAs(texture),"stale callback refuses before submission/publication");
            }
        }
        private static void V3ParticlePlacementProbe()
        {
            // Isolated actual shader pass2, not campaign data or a UI stand-in.
            // White mask prevents the final gate from hiding an incorrectly placed splat.
            const int width=64,height=48;const float cx=17.28f,cy=14.88f,radius=4;
            var target=new RenderTexture(width,height,0,RenderTextureFormat.ARGBHalf,RenderTextureReadWrite.Linear);
            var mask=new Texture2D(1,1,TextureFormat.R8,false,true){filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Clamp};
            var material=new Material(Resources.Load<Shader>("GalaxyPresentationV3/Presentation"));var mesh=new Mesh();var prior=RenderTexture.active;
            try
            {
                mask.LoadRawTextureData(new byte[]{255});mask.Apply();material.SetTexture("_Mask",mask);Assert.That(target.Create(),Is.True);
                var vertices=new Vector3[4];var uv=new Vector2[4];var masks=new Vector2[4];var colours=new Color[4];
                for(int k=0;k<4;k++){float x=cx+(k==0||k==3?-radius:radius),y=cy+(k<2?-radius:radius);vertices[k]=new Vector3(2*x/width-1,1-2*y/height,0);uv[k]=new Vector2(k==0||k==3?0:1,k<2?0:1);masks[k]=new Vector2(x/width,y/height);colours[k]=new Color(1,1,1,0);}
                mesh.vertices=vertices;mesh.uv=uv;mesh.uv2=masks;mesh.colors=colours;mesh.triangles=new[]{0,1,2,0,2,3};
                Graphics.SetRenderTarget(target);GL.Clear(false,true,Color.clear);Assert.That(material.SetPass(2),Is.True);Graphics.DrawMeshNow(mesh,Matrix4x4.identity);
                var pixels=V3Read(target);double weight=0,xsum=0,ysum=0;
                for(int y=0;y<height;y++)for(int x=0;x<width;x++){double value=pixels[x+(height-1-y)*width].r;weight+=value;xsum+=(x+.5)*value;ysum+=(y+.5)*value;}
                Assert.That(weight,Is.GreaterThan(0));double actualX=xsum/weight,actualY=ysum/weight;
                double error=Math.Sqrt(Math.Pow(actualX-cx,2)+Math.Pow(actualY-cy,2)),reflected=Math.Sqrt(Math.Pow(actualX-cx,2)+Math.Pow(actualY-(height-cy),2));
                File.WriteAllText(Path.Combine(CampaignOutput(),"v3-particle-placement.json"),JsonConvert.SerializeObject(new{label="Single asymmetric component probe; SAME shader pass2, white mask, no final gate",width,height,expectedX=cx,expectedY=cy,actualX,actualY,error,reflected,centroidTolerancePixels=.35,SystemInfo.graphicsDeviceType,SystemInfo.graphicsUVStartsAtTop}));
                V3Save(target,"COMPONENT-raw-linear-data-asymmetric-particle-not-campaign",true);
                Assert.That(error,Is.LessThan(.35),"bounded subpixel centroid only; strict exterior tolerance stays ZERO");Assert.That(reflected,Is.GreaterThan(10),"reject vertically reflected particle placement");
            }
            finally{RenderTexture.active=prior;target.Release();UnityEngine.Object.Destroy(target);UnityEngine.Object.Destroy(mask);UnityEngine.Object.Destroy(material);UnityEngine.Object.Destroy(mesh);}
        }
        [UnityTest] public IEnumerator V3GalaxyAndChartedNebulaePreserveActualCampaignControlsAndComposition()
        {
            yield return new EnterPlayMode();yield return null;yield return null;
            var entry=UnityEngine.Object.FindAnyObjectByType<ReviewEntry>();var doc=entry.GetComponent<UIDocument>();
            directory=Path.Combine(Path.GetTempPath(),"HobbyDS9-Galaxy-Engine-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(directory);
            typeof(ReviewEntry).GetField("generatedCampaignTestDirectory",BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(entry,Path.Combine(directory,"campaign"));
            var codec=new ReviewNewtonsoftCodec();var storePath=Path.Combine(directory,"campaign");var v2=new ReviewStaticAstronomyCampaignSlotStore(storePath,codec);var v1=new ReviewGeneratedCampaignSlotStore(storePath,codec);
            var a=V3Fixture("HOBBYDS9_V3_EXPLORE","d7b332383b726607209067a5c89085a4979814b12c35a5fd717a41ebd3ecdddc");
            var b=V3Fixture("HOBBYDS9_V3_COMBAT","e3be33bf1fae4bcf9de27a73aef4b628c8419e0c5194f806cf39684ab32e46bc");
            var old=V3Fixture("HOBBYDS9_V3_LEGACY","1a4a3ea7395a360f53a4a2bf0f0d949722f532681f6122f824704ff97cb23c92");
            v2.Write("generated-2",codec.DecodeStaticAstronomyCampaignRoot(a),false);v1.Write("generated-3",codec.DecodeGeneratedCampaignRoot(old),false);
            var legacyBytes=codec.EncodeGeneratedCampaignRoot(v1.Read("generated-3"));
            Directory.CreateDirectory(storePath);var wrong=Path.Combine(storePath,"unity-generated-static-astronomy-v2-generated-3.json");using(var file=new FileStream(wrong,FileMode.CreateNew))file.Write(old,0,old.Length);
            var initial=CampaignModel(entry);int baseline=GeneratedGalaxyV3Background.LiveInstances;
            try
            {
                using(var surface=new GalaxyInputSurface(doc))
                {
                    foreach(var size in V3Sizes)yield return V3Sized(entry,doc,size,"menu",false,false); //3
                    var fold=doc.rootVisualElement.Q<Foldout>("other-campaign-profiles");var start=doc.rootVisualElement.Q<Button>("generated-campaign-start");start.Focus();yield return null;
                    for(int i=0;i<10;i++){Key(doc.rootVisualElement.panel.focusController.focusedElement as VisualElement??start,KeyCode.Tab);Key(doc.rootVisualElement.panel.visualTree,KeyCode.Tab,true);yield return null;var focused=doc.rootVisualElement.panel.focusController.focusedElement as VisualElement;Assert.That(focused==null||!fold.Contains(focused)||focused==fold.Q<Toggle>(),Is.True,"collapsed older controls cannot receive Tab focus");}
                    var foldToggle=fold.Q<Toggle>();foldToggle.Focus();yield return null;
                    Assert.That(doc.rootVisualElement.panel.focusController.focusedElement,Is.SameAs(foldToggle));
                    // BaseBoolField consumes navigation-submit. Pooled KeyDown does
                    // not reproduce the input module's Space-to-submit translation.
                    // This proves focused navigation activation, NOT native Space.
                    using(var submit=NavigationSubmitEvent.GetPooled()){submit.target=foldToggle;foldToggle.SendEvent(submit);}yield return null;
                    Assert.That(fold.value,Is.True,"focused navigation submit expands older profiles");
                    using(var submit=NavigationSubmitEvent.GetPooled()){submit.target=foldToggle;foldToggle.SendEvent(submit);}yield return null;Assert.That(fold.value,Is.False);
                    Click(doc.rootVisualElement.Q<Button>("generated-campaign-continue-generated-1"));Confirm(doc);yield return WaitCampaign(entry,"v3-empty-v2");Assert.That(CampaignModel(entry),Is.SameAs(initial));
                    Click(doc.rootVisualElement.Q<Button>("generated-campaign-continue-generated-3"));Confirm(doc);yield return WaitCampaign(entry,"v3-wrong-profile");Assert.That(CampaignModel(entry),Is.SameAs(initial));Assert.That(File.ReadAllBytes(wrong),Is.EqualTo(old));
                    Click(doc.rootVisualElement.Q<Button>("generated-campaign-start"));if(doc.rootVisualElement.Q("review-one-use-confirmation")!=null)Confirm(doc);yield return WaitCampaign(entry,"v3-new250");
                    var fresh=CampaignModel(entry);Assert.That(fresh.StaticAstronomyCampaign,Is.True);var freshPair=fresh.GeneratedPresentation();
                    CampaignPage(doc,ReviewWorkspacePage.Saves);Click(doc.rootVisualElement.Q<Button>("player-save"));var saved=codec.EncodeStaticAstronomyCampaignRoot(v2.Read("generated-1"));
                    Click(doc.rootVisualElement.Q<Button>("demo-menu"));Click(doc.rootVisualElement.Q<Button>("generated-campaign-continue-generated-1"));Confirm(doc);yield return WaitCampaign(entry,"v3-continue-fresh");
                    var continued=CampaignModel(entry);Assert.That(codec.EncodeStaticAstronomyCampaignRoot(CampaignPrivateAudit(continued)),Is.EqualTo(saved));Assert.That(continued.IsCurrentGeneratedPresentation(freshPair),Is.False);
                    Click(doc.rootVisualElement.Q<Button>("demo-menu"));doc.rootVisualElement.Q<Foldout>("other-campaign-profiles").value=true;
                    Click(doc.rootVisualElement.Q<Button>("generated-campaign-v1-continue-generated-3"));Confirm(doc);yield return WaitCampaign(entry,"v3-explicit-v1");Assert.That(CampaignModel(entry).StaticAstronomyCampaign,Is.False);
                    CampaignPage(doc,ReviewWorkspacePage.Saves);Click(doc.rootVisualElement.Q<Button>("player-save"));if(doc.rootVisualElement.Q("review-one-use-confirmation")!=null)Confirm(doc);
                    Assert.That(codec.EncodeGeneratedCampaignRoot(v1.Read("generated-3")),Is.EqualTo(legacyBytes));
                    Click(doc.rootVisualElement.Q<Button>("demo-menu"));Click(doc.rootVisualElement.Q<Button>("generated-campaign-continue-generated-2"));Confirm(doc);yield return WaitCampaign(entry,"v3-labelled-explore32");
                    var model=CampaignModel(entry);var pair=model.GeneratedPresentation();Assert.That(pair.Frame.Status.Turn,Is.EqualTo(32));Assert.That(pair.Astronomy.Nebulae.Count,Is.EqualTo(3));V3ImportProbe();V3ParticlePlacementProbe(); //1 component frame
                    V3SyntheticEight(pair,codec); //1 labelled workload frame
                    foreach(var size in V3Sizes)yield return V3Sized(entry,doc,size,"explore32-overview",false,false); //3
                    foreach(var size in V3Sizes)yield return V3Sized(entry,doc,size,"explore32-regional",true,true); //6 on/off
                    yield return V3Sized(entry,doc,new Vector2Int(1920,1080),"explore32-lawful-proxy-closeup",false,true,true); //1 tighter lawful-proxy view
                    yield return V3Settled(doc);var renderer=V3Renderer(doc);Assert.That(renderer.Texture!.format,Is.EqualTo(RenderTextureFormat.ARGBHalf));
                    var known=pair.KnownMap;var fleet=model.Current.Frame.OwnStrategicFleets.First();Click(doc.rootVisualElement.Q<Button>("map-fleet-"+fleet.FleetId.Value));
                    var target=known.Regions.Select(r=>(r.NodeId,Offer:model.Current.FindKnownRoute(fleet.FleetId.Value,r.NodeId))).Where(p=>p.Offer?.Route.Success==true&&p.Offer.Route.TotalCost>0).OrderBy(p=>p.Offer!.Route.TotalCost).ThenBy(p=>p.NodeId,StringComparer.Ordinal).First().NodeId;
                    var beforeCancel=codec.EncodeStaticAstronomyCampaignRoot(CampaignPrivateAudit(model));
                    Click(doc.rootVisualElement.Q<Button>("map-fit"));yield return null;yield return CampaignPointerRight(doc,entry,doc.rootVisualElement.Q<Button>("map-star-"+target),0);yield return CampaignPointerRight(doc,entry,doc.rootVisualElement.Q<Button>("map-star-"+target));Confirm(doc,false);
                    Assert.That(codec.EncodeStaticAstronomyCampaignRoot(CampaignPrivateAudit(model)),Is.EqualTo(beforeCancel),"actual route review Cancel preserves canonical campaign");
                    yield return CampaignZoomPan(doc,entry);yield return V3Settled(doc);V3Save(doc.panelSettings.targetTexture,"motion-pan");
                    Click(doc.rootVisualElement.Q<Button>("campaign-visual-depth"));yield return V3Settled(doc);V3Save(doc.panelSettings.targetTexture,"motion-depth-off");
                    Click(doc.rootVisualElement.Q<Button>("campaign-visual-depth"));yield return V3Settled(doc);V3Save(doc.panelSettings.targetTexture,"motion-depth-on"); //3
                    var map=doc.rootVisualElement.Q("playable-known-map");var parent=map.parent;int index=parent.IndexOf(map);map.RemoveFromHierarchy();yield return null;Assert.That(GeneratedGalaxyV3Background.LiveInstances,Is.EqualTo(baseline));Assert.That(map.Q<Image>("campaign-v3-content-background").userData,Is.Null);
                    parent.Insert(index,map);yield return null;yield return V3Settled(doc);Assert.That(GeneratedGalaxyV3Background.LiveInstances,Is.EqualTo(baseline+1));
                    var lost=V3Renderer(doc);lost.Texture!.Release();Assert.Throws<InvalidOperationException>(()=>lost.Render(model.GeneratedViewport,map.contentRect.width,map.contentRect.height,true,true));
                    // Next actual refresh must latch flat fallback, never publish a lost texture.
                    Click(doc.rootVisualElement.Q<Button>("campaign-visual-depth"));yield return null;Assert.That(map.Q<Image>("campaign-v3-content-background").image,Is.Null);Assert.That(GeneratedGalaxyV3Background.LiveInstances,Is.EqualTo(baseline));
                    v2.Write("generated-3",codec.DecodeStaticAstronomyCampaignRoot(b),true);Click(doc.rootVisualElement.Q<Button>("demo-menu"));Click(doc.rootVisualElement.Q<Button>("generated-campaign-continue-generated-3"));Confirm(doc);yield return WaitCampaign(entry,"v3-labelled-combat-zero");Assert.That(CampaignModel(entry).IsCurrentGeneratedPresentation(pair),Is.False);
                    CampaignPage(doc,ReviewWorkspacePage.Map);yield return V3Settled(doc);Assert.That(CampaignModel(entry).GeneratedPresentation().Astronomy.Nebulae.Count,Is.Zero);Assert.That(V3Renderer(doc).VisibleFieldCount,Is.Zero);V3Save(doc.panelSettings.targetTexture,"combat-zero-proxy"); //1
                    Assert.That(Directory.GetFiles(CampaignOutput(),"v3-*.png").Length,Is.EqualTo(19),"explicit19 normal frames, within20 maximum");
                    File.WriteAllText(Path.Combine(CampaignOutput(),"v3-summary.json"),JsonConvert.SerializeObject(new{source="receiving candidate",frames=Directory.GetFiles(CampaignOutput(),"v3-*.png").Length,claim="Labelled separate journeys; no native/FPS/GPU-time claim",ManagedBytes=GC.GetTotalMemory(false)}));
                    Click(doc.rootVisualElement.Q<Button>("demo-menu"));yield return null;Assert.That(GeneratedGalaxyV3Background.LiveInstances,Is.EqualTo(baseline));
                }
            }
            finally{V3Audit(entry,"finally");}
            yield return new ExitPlayMode();
        }
    }
}
