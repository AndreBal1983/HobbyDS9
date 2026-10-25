#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using HobbyDS9.Contracts;
using HobbyDS9.TacticalReviewClient;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace HobbyDS9.UnityReview
{
    // Dedicated V3 receiver. No full generated document, seed or private feature
    // collection is accepted. V1's background implementation is unchanged.
    public sealed class GeneratedGalaxyV3Background : IDisposable
    {
        private static void Destroy(UnityEngine.Object o){if(UnityEngine.Application.isPlaying)UnityEngine.Object.Destroy(o);else UnityEngine.Object.DestroyImmediate(o);}
        private sealed class Assets : IDisposable
        {
            internal Texture2D Atlas=null!;internal Texture3D Volume=null!;internal IReadOnlyList<GalaxyV3Particle> Particles=null!;
            internal int References;
            internal Assets()
            {
                try
                {
                    Atlas=Resources.Load<Texture2D>("GalaxyPresentationV3/galaxy");
                    if(Atlas==null||Atlas.width!=2048||Atlas.height!=2048||Atlas.mipmapCount!=12||Atlas.isReadable
                        ||Atlas.format!=TextureFormat.RGBA32||GraphicsFormatUtility.IsSRGBFormat(Atlas.graphicsFormat)
                        ||Atlas.filterMode!=FilterMode.Trilinear||Atlas.wrapMode!=TextureWrapMode.Clamp)
                        throw new InvalidOperationException("V3 atlas must be linear RGBA32, mipmapped, clamped and non-readable.");
                    var raw=Resources.Load<TextAsset>("GalaxyPresentationV3/nebula");var points=Resources.Load<TextAsset>("GalaxyPresentationV3/particles");
                    if(raw==null||points==null)throw new InvalidOperationException("V3 data assets are unavailable.");
                    try
                    {
                        Particles=GeneratedGalaxyV3AssetData.ReadParticles(points.bytes);
                        Volume=new Texture3D(128,96,64,TextureFormat.RGBA32,true){name="V3 linear guarded volume",hideFlags=HideFlags.HideAndDontSave,wrapMode=TextureWrapMode.Clamp,filterMode=FilterMode.Trilinear};
                        GeneratedGalaxyV3AssetData.UploadVolume(raw.bytes,(level,w,h,d,data)=>Volume.SetPixelData(data,level));Volume.Apply(false,true);
                        if(GraphicsFormatUtility.IsSRGBFormat(Volume.graphicsFormat)||Volume.mipmapCount!=8)throw new InvalidOperationException("V3 volume format mismatch.");
                    }
                    finally{Resources.UnloadAsset(raw);Resources.UnloadAsset(points);}
                }
                catch{Dispose();throw;}
            }
            public void Dispose(){if(Volume!=null)Destroy(Volume);if(Atlas!=null)Resources.UnloadAsset(Atlas);}
        }
        private sealed class Field
        {
            internal readonly CampaignNebulaVisualProxy Proxy;internal readonly float Cos,Sin;
            internal Field(CampaignNebulaVisualProxy proxy){Proxy=proxy;double angle=GeneratedGalaxyV3Math.Variant(proxy.FeatureId)/(double)uint.MaxValue*Math.PI*2;Cos=(float)Math.Cos(angle);Sin=(float)Math.Sin(angle);}
        }
        // Provisional visual tuning; support is never expanded by this inward attenuation.
        private const double InwardMaskFeatherUiPixels=8;
        private static Assets? shared;
        public static int LiveInstances { get; private set; }
        public static int SharedReferences=>shared?.References??0;
        private readonly Assets assets;private readonly Material material=null!;private readonly Mesh mesh=null!;
        private readonly Func<bool> isCurrent;private readonly int mapWidth,mapHeight;
        private readonly IReadOnlyList<GeneratedGalaxyRegionDto> regions;
        private readonly Field[] lawfulFields;
        private readonly Vector4[] fieldVectors=new Vector4[8],fieldParameters=new Vector4[8];
        private readonly Vector3[] vertices=new Vector3[1024];private readonly Vector2[] uv=new Vector2[1024],maskUv=new Vector2[1024];private readonly Color[] colours=new Color[1024];private readonly int[] indices=new int[1536];
        private Field[] displayed=Array.Empty<Field>();private readonly GalaxyV3Residency residency=new GalaxyV3Residency();
        private RenderTexture? final,local;private Texture2D? mask;
        private bool disposed;private double lastX=double.NaN,lastY,lastScale,lastWidth,lastHeight;
        private bool lastEffects,lastDepth;private string maskKey="";
        public RenderTexture? Texture=>final;
        public bool NeedsTransition=>residency.NeedsTransition;
        public int RenderCount { get; private set; }
        public int VisibleFieldCount=>displayed.Length;
        public int ParticleCount { get; private set; }
        public int LocalFetchUpperBound { get; private set; }
        public string LocalStatus { get; private set; }="not-rendered";
        public Vector2Int LocalSize=>local==null?Vector2Int.zero:new Vector2Int(local.width,local.height);
        public GeneratedGalaxyV3Background(GeneratedCampaignPresentation pair,Func<bool> isCurrent)
        {
            this.isCurrent=isCurrent;if(!isCurrent())throw new InvalidOperationException("Stale V3 observation.");
            if(!SystemInfo.supports3DTextures||!SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGBHalf)
                ||!SystemInfo.SupportsTextureFormat(TextureFormat.R8)||QualitySettings.activeColorSpace!=ColorSpace.Linear)
                throw new NotSupportedException("V3 requires linear colour, 3D textures and half-precision targets.");
            var shader=Resources.Load<Shader>("GalaxyPresentationV3/Presentation");if(shader==null||!shader.isSupported)throw new NotSupportedException("V3 shader is unavailable.");
            var known=pair.KnownMap;mapWidth=known.Width;mapHeight=known.Height;regions=known.Regions;
            if(mapWidth<1||mapHeight<1||pair.Astronomy.Nebulae.Count>1250)throw new InvalidOperationException("Invalid V3 known geometry.");
            lawfulFields=pair.Astronomy.Nebulae.Select(p=>{if(p.VisualReferenceRadiusX<=0||p.VisualReferenceRadiusY<=0)throw new InvalidOperationException("Invalid V3 proxy.");return new Field(p);}).ToArray();
            assets=shared??new Assets();shared=assets;assets.References++;
            try
            {
            material=new Material(shader){hideFlags=HideFlags.HideAndDontSave};mesh=new Mesh{name="V3 bounded soft particles",hideFlags=HideFlags.HideAndDontSave};mesh.MarkDynamic();
            for(int i=0;i<256;i++){int v=i*4,k=i*6;indices[k]=v;indices[k+1]=v+1;indices[k+2]=v+2;indices[k+3]=v;indices[k+4]=v+2;indices[k+5]=v+3;}
            material.SetTexture("_Atlas",assets.Atlas);material.SetTexture("_Volume",assets.Volume);LiveInstances++;
            }
            catch{if(material!=null)Destroy(material);if(mesh!=null)Destroy(mesh);if(--assets.References==0){assets.Dispose();shared=null;}throw;}
        }
        private (double x,double y) Project(Field field,double x,double y,double z,GeneratedGalaxyViewport view,double width,double height,bool depth)
        {
            double dx=field.Cos*x+field.Sin*y,dy=-field.Sin*x+field.Cos*y;
            double wx=(field.Proxy.CentreX-mapWidth*.5+dx*field.Proxy.VisualReferenceRadiusX)/mapWidth,
                wy=(field.Proxy.CentreY-mapHeight*.5+dy*field.Proxy.VisualReferenceRadiusY)/mapWidth;
            double cx=(view.WorldX(width/2)-mapWidth*.5)/mapWidth,cy=(view.WorldY(height/2)-mapHeight*.5)/mapWidth;
            var p=GeneratedGalaxyV3Math.Project(wx,wy,z*.8,cx,cy,depth);
            return(view.X(p.x*mapWidth+mapWidth*.5),view.Y(p.y*mapWidth+mapHeight*.5));
        }
        private Field[] Select(GeneratedGalaxyViewport view,double width,double height,bool depth)
        {
            var candidates=new List<(Field field,double score)>();
            foreach(var field in lawfulFields)
            {
                double left=double.PositiveInfinity,top=left,right=double.NegativeInfinity,bottom=right;
                foreach(double x in new[]{-2.5,2.5})foreach(double y in new[]{-1.65,1.65})foreach(double z in new[]{-1.0,1.0})
                {var p=Project(field,x,y,z,view,width,height,depth);left=Math.Min(left,p.x);right=Math.Max(right,p.x);top=Math.Min(top,p.y);bottom=Math.Max(bottom,p.y);}
                if(right<0||bottom<0||left>width||top>height)continue;
                double score=Math.Pow((left+right-width)/Math.Max(1,right-left),2)+Math.Pow((top+bottom-height)/Math.Max(1,bottom-top),2);
                if(displayed.Any(f=>f.Proxy.FeatureId==field.Proxy.FeatureId))score*=.85; // Bounded retention hysteresis.
                candidates.Add((field,score));
            }
            return candidates.OrderBy(p=>p.score).ThenBy(p=>p.field.Proxy.FeatureId,StringComparer.Ordinal).Take(8).Select(p=>p.field).OrderBy(f=>f.Proxy.FeatureId,StringComparer.Ordinal).ToArray();
        }
        private float UpdateFields(Field[] next)
        {
            residency.Update(next.Select(f=>f.Proxy.FeatureId),Time.realtimeSinceStartupAsDouble);
            displayed=residency.Visible.Select(id=>lawfulFields.First(f=>f.Proxy.FeatureId==id)).ToArray();
            return 1;
        }
        private static RenderTexture Target(int width,int height,string name)
        {
            var rt=new RenderTexture(width,height,0,RenderTextureFormat.ARGBHalf,RenderTextureReadWrite.Linear){name=name,hideFlags=HideFlags.HideAndDontSave,filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp};
            if(!rt.Create()||!rt.IsCreated()){rt.Release();Destroy(rt);throw new InvalidOperationException("V3 render target allocation failed.");}return rt;
        }
        private static void Release(ref RenderTexture? rt){if(rt!=null){rt.Release();Destroy(rt);rt=null;}}
        private static bool Finite(double value)=>!double.IsNaN(value)&&!double.IsInfinity(value);
        private static bool ShaderFinite(double value)=>Finite(value)&&Math.Abs(value)<=float.MaxValue;
        internal static bool HasRenderableGeometry(GeneratedGalaxyViewport view,double width,double height,int mapWidth,int mapHeight)
        {
            if(!ShaderFinite(width)||!ShaderFinite(height)||width<=0||height<=0||!ShaderFinite(view.Scale)||view.Scale<=0)return false;
            double x=view.WorldX(0),y=view.WorldY(0);
            return ShaderFinite(x)&&ShaderFinite(y)&&ShaderFinite(view.WorldX(width))&&ShaderFinite(view.WorldY(height))
                &&ShaderFinite(view.X(0))&&ShaderFinite(view.Y(0))&&ShaderFinite(view.X(mapWidth))&&ShaderFinite(view.Y(mapHeight))
                &&ShaderFinite((x-mapWidth*.5)/mapWidth)&&ShaderFinite((y-mapHeight*.5)/mapWidth)
                &&ShaderFinite(width/view.Scale/mapWidth)&&ShaderFinite(height/view.Scale/mapWidth);
        }
        public bool Render(GeneratedGalaxyViewport view,double width,double height,bool effects,bool depth)
        {
            if(disposed||!isCurrent())throw new InvalidOperationException("V3 observation is obsolete.");
            if(final!=null&&!final.IsCreated()||local!=null&&!local.IsCreated())throw new InvalidOperationException("V3 graphics context was lost.");
            // Pending UI layout is ordinary: preserve all cached/rendering state and retry later.
            if(!HasRenderableGeometry(view,width,height,mapWidth,mapHeight))return false;
            double x=view.WorldX(0),y=view.WorldY(0);
            if(lastX==x&&lastY==y&&lastScale==view.Scale&&lastWidth==width&&lastHeight==height&&lastEffects==effects&&lastDepth==depth&&!NeedsTransition)return false;
            var size=GeneratedGalaxyVisualField.TargetSize(width,height);
            float fade;
            if(!effects){displayed=Array.Empty<Field>();residency.Reset();fade=0;}
            else fade=UpdateFields(Select(view,width,height,depth));
            var localSize=GeneratedGalaxyV3AssetData.LocalTarget(size.width,size.height,displayed.Length);LocalFetchUpperBound=48*displayed.Length*localSize.width*localSize.height;
            if(final==null||final.width!=size.width||final.height!=size.height){Release(ref final);final=Target(size.width,size.height,"V3 final full-resolution macro");}
            if(local==null||local.width!=localSize.width||local.height!=localSize.height)
            {Release(ref local);local=Target(localSize.width,localSize.height,"V3 adaptive local accumulation");}
            // The border mask has stable final-target resolution, independent of
            // how many volumes share the ray budget. Changes at the field cap
            // must not change the mask's pixel grid.
            if(mask==null||mask.width!=final.width||mask.height!=final.height)
            {if(mask!=null)Destroy(mask);mask=new Texture2D(final.width,final.height,TextureFormat.R8,false,true){name="V3 conservative charted union",hideFlags=HideFlags.HideAndDontSave,filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp};maskKey="";}
            string key=x.ToString("R",System.Globalization.CultureInfo.InvariantCulture)+"/"+y.ToString("R",System.Globalization.CultureInfo.InvariantCulture)+"/"+view.Scale.ToString("R",System.Globalization.CultureInfo.InvariantCulture)+"/"+width+"/"+height+"/"+mask.width+"/"+mask.height;
            if(maskKey!=key)
            {
                var guard=GeneratedGalaxyV3AssetData.MaskGuard(mask.width,mask.height,final.width,final.height,width,height);
                // One extra mask texel covers its bilinear footprint. Feathering
                // remains inside the certified union, never into unknown space.
                var result=GeneratedGalaxyChartedMask.Build(regions,view,width,height,mask.width,mask.height,guard.x+1,guard.y+1,inwardFeatherUiPixels:InwardMaskFeatherUiPixels);
                mask!.LoadRawTextureData(result.Pixels);mask.Apply(false,false);LocalStatus=result.Succeeded?"charted-union":"local disabled: "+result.Reason;maskKey=key;
            }
            material.SetVector("_View",new Vector4((float)((x-mapWidth*.5)/mapWidth),(float)((y-mapHeight*.5)/mapWidth),(float)(width/view.Scale/mapWidth),(float)(height/view.Scale/mapWidth)));
            material.SetFloat("_Depth",depth?1:0);material.SetFloat("_Effects",effects?1:0);material.SetTexture("_Mask",mask);
            material.SetInt("_FieldCount",displayed.Length);material.SetVector("_LocalSize",new Vector4(local.width,local.height,0,0));
            for(int i=0;i<displayed.Length;i++)
            {
                var f=displayed[i];fieldVectors[i]=new Vector4((float)((f.Proxy.CentreX-mapWidth*.5)/mapWidth),(float)((f.Proxy.CentreY-mapHeight*.5)/mapWidth),(float)f.Proxy.VisualReferenceRadiusX/mapWidth,(float)f.Proxy.VisualReferenceRadiusY/mapWidth);
                double spanX=5*f.Proxy.VisualReferenceRadiusX*view.Scale*local.width/width,spanY=3.3*f.Proxy.VisualReferenceRadiusY*view.Scale*local.height/height;
                float mip=(float)Math.Max(Math.Log(64.0/48,2),Math.Max(0,Math.Log(Math.Max(128/Math.Max(1,spanX),96/Math.Max(1,spanY)),2)));
                fieldParameters[i]=new Vector4(f.Cos,f.Sin,mip,fade*residency.Weight(f.Proxy.FeatureId,Time.realtimeSinceStartupAsDouble));
            }
            material.SetVectorArray("_Fields",fieldVectors);material.SetVectorArray("_FieldParameters",fieldParameters);
            float macroMip=(float)Math.Max(0,Math.Log(2048/Math.Max(1,.47*2.3*Math.Cos(48*Math.PI/180)*mapWidth*view.Scale*Math.Min(final.width/width,final.height/height)*.85),2));material.SetFloat("_MacroMip",macroMip);
            var prior=RenderTexture.active;
            try
            {
                Graphics.Blit(null,local,material,0);DrawParticles(view,width,height,depth,fade);
                material.SetTexture("_Local",local);Graphics.Blit(null,final,material,1);
            }
            finally{RenderTexture.active=prior;}
            if(!isCurrent()||!final.IsCreated()||!local.IsCreated())throw new InvalidOperationException("V3 frame became unavailable before publication.");
            lastX=x;lastY=y;lastScale=view.Scale;lastWidth=width;lastHeight=height;lastEffects=effects;lastDepth=depth;RenderCount++;return true;
        }
        private void DrawParticles(GeneratedGalaxyViewport view,double width,double height,bool depth,float fade)
        {
            ParticleCount=0;if(displayed.Length==0||fade<=0)return;
            // Candidate index then stable feature order fairly shares one TOTAL cap.
            var ordered=displayed.OrderBy(f=>f.Proxy.FeatureId,StringComparer.Ordinal).ToArray();
            foreach(var point in assets.Particles)foreach(var field in ordered)
            {
                if(ParticleCount>=256)goto ready;
                var p=Project(field,point.X,point.Y,point.Z,view,width,height,depth);
                double q=depth?4/(4-point.Z*.8):1;
                double diameter=Math.Min(32,2*point.Radius*Math.Max(field.Proxy.VisualReferenceRadiusX,field.Proxy.VisualReferenceRadiusY)*view.Scale*q*Math.Max(local!.width/width,local.height/height));
                if(!Finite(p.x)||!Finite(p.y)||!Finite(diameter)||diameter<=0)continue;
                double gain=Math.Max(0,Math.Min(1,(diameter-.5)/1.5))*fade*residency.Weight(field.Proxy.FeatureId,Time.realtimeSinceStartupAsDouble);if(gain<=0)continue;
                double px=p.x*local!.width/width,py=p.y*local.height/height,r=diameter/2;if(px+r<0||py+r<0||px-r>local.width||py-r>local.height)continue;
                if(!ShaderFinite((px-r)/local.width)||!ShaderFinite((px+r)/local.width)||!ShaderFinite((py-r)/local.height)||!ShaderFinite((py+r)/local.height))continue;
                int at=ParticleCount++*4;
                for(int k=0;k<4;k++)
                {
                    double xx=px+(k==0||k==3?-r:r),yy=py+(k<2?-r:r);float nx=(float)(xx/local.width),ny=(float)(yy/local.height);
                    vertices[at+k]=new Vector3(nx*2-1,1-ny*2,0);uv[at+k]=new Vector2(k==0||k==3?0:1,k<2?0:1);maskUv[at+k]=new Vector2(nx,ny);
                    colours[at+k]=new Color(.55f,.8f,1,0)*(float)(point.Brightness*.08*gain);
                }
            }
            ready:
            if(ParticleCount==0)return;mesh.Clear();mesh.SetVertices(vertices,0,ParticleCount*4);mesh.SetUVs(0,uv,0,ParticleCount*4);mesh.SetUVs(1,maskUv,0,ParticleCount*4);mesh.SetColors(colours,0,ParticleCount*4);
            mesh.SetIndices(indices,0,ParticleCount*6,MeshTopology.Triangles,0,false);Graphics.SetRenderTarget(local);
            if(!material.SetPass(2))throw new InvalidOperationException("V3 particle pass unavailable.");Graphics.DrawMeshNow(mesh,Matrix4x4.identity);
        }
        public void Dispose()
        {
            if(disposed)return;disposed=true;Release(ref final);Release(ref local);if(mask!=null)Destroy(mask);Destroy(material);Destroy(mesh);
            if(--assets.References==0){assets.Dispose();shared=null;}LiveInstances--;
        }
    }
}
