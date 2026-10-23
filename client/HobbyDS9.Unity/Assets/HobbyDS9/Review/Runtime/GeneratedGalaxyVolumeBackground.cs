#nullable enable
using System;
using System.Diagnostics;
using System.Linq;
using HobbyDS9.Contracts;
using UnityEngine;

namespace HobbyDS9.UnityReview
{
    // One manually rendered UI background, not a camera/input surface. The map's
    // navigation plane remains its existing viewport and Painter2D projection.
    public sealed class GeneratedGalaxyVolumeBackground : IDisposable
    {
        private readonly GeneratedGalaxyMapData map;
        private readonly Material material=null!;
        private readonly Texture3D field=null!;
        private RenderTexture? texture;
        private bool disposed;
        private readonly Vector4[] clouds=new Vector4[GeneratedGalaxyVisualField.MaximumClouds];
        private readonly Vector4[] parameters=new Vector4[GeneratedGalaxyVisualField.MaximumClouds];
        private double lastX=double.NaN,lastY,lastScale;private int lastWidth,lastHeight;private bool lastFeatures,lastDepth;
        public RenderTexture? Texture=>texture;
        public int RenderCount { get; private set; }
        public int VisibleCloudCount { get; private set; }
        public int OmittedCloudCount { get; private set; }
        public double LastSubmissionMilliseconds { get; private set; }
        public static int LiveInstances { get; private set; }
        public GeneratedGalaxyVolumeBackground(GeneratedGalaxyMapData map)
        {
            this.map=map;
            try
            {
            var shader=Resources.Load<Shader>("GalaxyPresentation/Volume");
            if(shader==null||!shader.isSupported)throw new NotSupportedException("Galaxy volume shader unavailable; flat map retained.");
            if(!SystemInfo.supports3DTextures)throw new NotSupportedException("Galaxy density textures unsupported; flat map retained.");
            if(!SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGBHalf))throw new NotSupportedException("Galaxy high-precision target unsupported; flat map retained.");
            material=new Material(shader){hideFlags=HideFlags.HideAndDontSave};
            const int n=GeneratedGalaxyVisualField.Resolution;
            field=new Texture3D(n,n,n,TextureFormat.RGBA32,true){name="Galaxy coherent field v1",hideFlags=HideFlags.HideAndDontSave,
                wrapMode=TextureWrapMode.Clamp,filterMode=FilterMode.Trilinear};
            var pixels=new Color32[n*n*n];uint seed=GeneratedGalaxyVisualField.Seed("shared","cloud-density");
            for(int z=0;z<n;z++)for(int y=0;y<n;y++)for(int x=0;x<n;x++)
            {
                double a=2.0*x/(n-1)-1,b=2.0*y/(n-1)-1,c=2.0*z/(n-1)-1;
                byte density=(byte)Math.Round(255*GeneratedGalaxyVisualField.Density(a,b,c,seed));
                byte noise=(byte)Math.Round(255*GeneratedGalaxyVisualField.Noise(a*4+9,b*4+3,c*4+7,seed));
                pixels[x+n*(y+n*z)]=new Color32(density,noise,0,255);
            }
            field.SetPixels32(pixels);field.Apply(true,true);material.SetTexture("_Field",field);LiveInstances++;
            }
            catch{if(field!=null)Destroy(field);if(material!=null)Destroy(material);throw;}
        }
        public bool Render(GeneratedGalaxyViewport viewport,double width,double height,bool features,bool depth)
        {
            if(disposed)throw new ObjectDisposedException(nameof(GeneratedGalaxyVolumeBackground));
            // A graphics-context loss can invalidate a previously created target.
            // Detect this before the unchanged-view cache return as well as publish.
            if(texture!=null&&!texture.IsCreated())throw new InvalidOperationException("Galaxy background target lost. Flat map retained.");
            var size=GeneratedGalaxyVisualField.TargetSize(width,height);
            double x=viewport.WorldX(0)/map.Width,y=viewport.WorldY(0)/map.Height;
            if(lastX==x&&lastY==y&&lastScale==viewport.Scale&&lastWidth==size.width&&lastHeight==size.height&&lastFeatures==features&&lastDepth==depth)return false;
            var watch=Stopwatch.StartNew();
            if(texture==null||texture.width!=size.width||texture.height!=size.height)
            {
                ReleaseTexture();texture=new RenderTexture(size.width,size.height,0,RenderTextureFormat.ARGBHalf,RenderTextureReadWrite.Linear)
                    {name="Galaxy half-resolution background",hideFlags=HideFlags.HideAndDontSave,filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp};
                if(!texture.Create()||!texture.IsCreated())throw new InvalidOperationException("Galaxy background target allocation failed ("+size.width+"x"+size.height+"). Flat map retained.");
            }
            material.SetVector("_View",new Vector4((float)x,(float)y,(float)(width/viewport.Scale/map.Width),(float)(height/viewport.Scale/map.Height)));
            material.SetFloat("_Depth",depth?1:0);material.SetFloat("_GalaxySeed",GeneratedGalaxyVisualField.Seed(map.Settings.Seed,"macro")%1000/1000f);
            double spanX=width/viewport.Scale/map.Width,spanY=height/viewport.Scale/map.Height,cx=x+spanX/2,cy=y+spanY/2;
            // Positive depth is closer than the reference plane. Conservative padded
            // bounds include maximum perspective expansion, so culling cannot clip a wisp.
            var visible=features?map.Features.Where(f=>f.Kind==GalaxyEnvironmentFeatureKind.Nebula).Where(f=>
                (f.Position.X+Math.Max(f.RadiusX,f.RadiusY)*3.7)/map.Width>=x-spanX*.05&&(f.Position.X-Math.Max(f.RadiusX,f.RadiusY)*3.7)/map.Width<=x+spanX*1.05&&
                (f.Position.Y+Math.Max(f.RadiusX,f.RadiusY)*3.7)/map.Height>=y-spanY*.05&&(f.Position.Y-Math.Max(f.RadiusX,f.RadiusY)*3.7)/map.Height<=y+spanY*1.05)
                .OrderBy(f=>Math.Pow((double)f.Position.X/map.Width-cx,2)+Math.Pow((double)f.Position.Y/map.Height-cy,2))
                .ThenBy(f=>f.FeatureId,StringComparer.Ordinal).ToArray():Array.Empty<EnvironmentGalaxyFeatureDto>();
            VisibleCloudCount=Math.Min(visible.Length,clouds.Length);OmittedCloudCount=Math.Max(0,visible.Length-clouds.Length);
            for(int i=0;i<VisibleCloudCount;i++)
            {
                var f=visible[i];uint seed=GeneratedGalaxyVisualField.Seed(map.Settings.Seed,f.FeatureId);
                // Visual padding is independent of the generation-influence ellipse.
                clouds[i]=new Vector4((float)f.Position.X/map.Width,(float)f.Position.Y/map.Height,(float)f.RadiusX/map.Width*2.4f,(float)f.RadiusY/map.Height*2.4f);
                parameters[i]=new Vector4((seed%6283)/1000f,((seed>>12)%3),((seed>>17)%1000)/1000f,0);
            }
            material.SetInt("_CloudCount",VisibleCloudCount);material.SetVectorArray("_Clouds",clouds);material.SetVectorArray("_CloudParameters",parameters);
            var prior=RenderTexture.active;
            try{Graphics.Blit(null,texture,material);}finally{RenderTexture.active=prior;}
            if(!texture.IsCreated())throw new InvalidOperationException("Galaxy background target lost during rendering. Flat map retained.");
            lastX=x;lastY=y;lastScale=viewport.Scale;lastWidth=size.width;lastHeight=size.height;lastFeatures=features;lastDepth=depth;RenderCount++;
            LastSubmissionMilliseconds=watch.Elapsed.TotalMilliseconds;return true; // CPU submission, NOT GPU frame time.
        }
        private static void Destroy(UnityEngine.Object value){if(UnityEngine.Application.isPlaying)UnityEngine.Object.Destroy(value);else UnityEngine.Object.DestroyImmediate(value);}
        private void ReleaseTexture(){if(texture==null)return;texture.Release();Destroy(texture);texture=null;}
        public void Dispose(){if(disposed)return;disposed=true;ReleaseTexture();Destroy(field);Destroy(material);LiveInstances--;}
    }
}
