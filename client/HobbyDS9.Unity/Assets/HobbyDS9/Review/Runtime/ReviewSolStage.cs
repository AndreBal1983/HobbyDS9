#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using HobbyDS9.Contracts;
using UnityEngine;
using UnityEngine.Rendering;

namespace HobbyDS9.UnityReview
{
    // All geometry except the schematic central star comes from caller-disclosed bodies.
    internal sealed class ReviewSolStage : IDisposable
    {
        private const int Layer=31, MaxGeneratedSurfaces=64;
        private readonly GameObject root=null!;
        private readonly Transform orbitalRoot=null!;
        private readonly Camera camera=null!;
        private readonly Material material=null!;
        private readonly Mesh sphere=null!,track=null!;
        private readonly List<Mesh> importedCopies=new List<Mesh>();
        private readonly List<string> assetFallbacks=new List<string>();
        private readonly List<Texture2D> ownedTextures=new List<Texture2D>();
        private readonly Dictionary<string,(Texture2D surface,Texture2D clouds)> surfaces=new Dictionary<string,(Texture2D,Texture2D)>(StringComparer.Ordinal);
        private readonly Dictionary<string,Transform> planetTransforms=new Dictionary<string,Transform>(StringComparer.Ordinal);
        private readonly Dictionary<string,float> angles=new Dictionary<string,float>(StringComparer.Ordinal);
        private readonly Dictionary<string,MeshRenderer> planetOrbits=new Dictionary<string,MeshRenderer>(StringComparer.Ordinal);
        private readonly MaterialPropertyBlock properties=new MaterialPropertyBlock();
        private static readonly Color OrbitColor=new Color(.15f,.22f,.33f);
        private readonly float extent;
        private bool disposed;
        internal int ImportedMeshCount=>importedCopies.Count;
        internal IReadOnlyList<string> AssetFallbacks=>assetFallbacks.AsReadOnly();
        internal RenderTexture Texture {get;}=null!;

        internal ReviewSolStage(Shader shader,IReadOnlyList<ColonisationPlanetSnapshot> bodies)
        {
            try
            {
            root=new GameObject("Disclosed system presentation"){hideFlags=HideFlags.HideAndDontSave,layer=Layer};
            root.SetActive(false);root.transform.position=new Vector3(0,-10000,0);
            orbitalRoot=Child("Presentation rotation").transform;
            camera=Child("System preview camera").AddComponent<Camera>();
            camera.enabled=false;camera.orthographic=true;camera.nearClipPlane=.1f;
            extent=2.4f+bodies.Count*1.2f;
            camera.farClipPlane=Mathf.Max(55,extent*5);
            camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.019f,.027f,.049f,1);
            camera.cullingMask=1<<Layer;camera.allowHDR=false;camera.allowMSAA=false;
            Texture=new RenderTexture(960,400,24,RenderTextureFormat.ARGB32)
                {name="Disclosed system overview",hideFlags=HideFlags.HideAndDontSave,antiAliasing=1};
            Texture.Create();camera.targetTexture=Texture;
            material=new Material(shader){hideFlags=HideFlags.HideAndDontSave};sphere=Sphere();track=Annulus(.996f,1,"Schematic orbit");
            Part("Schematic central star",sphere,Vector3.zero,.75f,new Color(1,.69f,.29f),false);
            Mesh? delivered=null;
            for(int i=0;i<bodies.Count;i++)
            {
                var body=bodies[i];
                var profile=PlanetVisualProfile.From(body);
                // Layout uses disclosed order/count only, never a hidden catalogue or body class.
                var orbit=2.4f+i*1.2f;var angle=-22+i*137.508f;
                var theta=angle*Mathf.Deg2Rad;
                var mesh=sphere;
                if(profile.Kind!=PlanetSurfaceKind.Unknown)
                    mesh=delivered??=DeliveredMesh("Earth_LOD1",false,sphere);
                var planet=Part(body.PlanetId,mesh,new Vector3(Mathf.Cos(theta)*orbit,0,Mathf.Sin(theta)*orbit),.46f,Color.white,true);
                planet.transform.SetParent(orbitalRoot,false);
                var maps=Surface(profile);
                planet.GetPropertyBlock(properties);properties.SetTexture("_Surface",maps.surface);properties.SetTexture("_Clouds",maps.clouds);
                properties.SetFloat("_Unknown",profile.Kind==PlanetSurfaceKind.Unknown?1:0);
                properties.SetFloat("_Atmosphere",profile.AtmosphereStrength);planet.SetPropertyBlock(properties);
                planetTransforms.Add(body.PlanetId,planet.transform);angles.Add(body.PlanetId,angle);
                var trackRenderer=Part("Schematic orbit",track,new Vector3(0,-.045f,0),orbit,OrbitColor,false);
                trackRenderer.transform.SetParent(orbitalRoot,false);planetOrbits.Add(body.PlanetId,trackRenderer);
            }
            }
            catch {Dispose();throw;}
        }
        internal float ForegroundYaw(string id)=>angles.TryGetValue(id,out var angle)?angle+90:0;
        private (Texture2D surface,Texture2D clouds) Surface(PlanetVisualProfile profile)
        {
            var key=PlanetVisualProfile.VisualVersion+"/"+profile.Kind+"/"+profile.Seed+"/"+profile.HasAtmosphere+"/"+profile.AuthoredMap;
            if(surfaces.TryGetValue(key,out var cached))return cached;
            // Reuse one neutral surface after the texture budget; all bodies still have controls/geometry.
            if(surfaces.Count>=MaxGeneratedSurfaces)
                return surfaces["neutral"];
            if(!surfaces.ContainsKey("neutral"))
            {
                var plain=new Texture2D(1,1){name="Neutral unresolved surface",hideFlags=HideFlags.HideAndDontSave};
                ownedTextures.Add(plain);plain.SetPixel(0,0,new Color(.18f,.24f,.31f));plain.Apply();
                surfaces.Add("neutral",(plain,Texture2D.blackTexture));
            }
            if(profile.Kind==PlanetSurfaceKind.Unknown)return surfaces["neutral"];
            Texture2D? surface=profile.AuthoredMap==null?null:Resources.Load<Texture2D>("SystemBodyArt/"+profile.AuthoredMap);
            if(surface==null)surface=Generate(profile,false,256,128);
            var clouds=profile.HasAtmosphere?Generate(profile,true,128,64):Texture2D.blackTexture;
            var result=(surface!,clouds);surfaces.Add(key,result);return result;
        }
        private Texture2D Generate(PlanetVisualProfile profile,bool clouds,int width,int height)
        {
            var texture=new Texture2D(width,height,TextureFormat.RGBA32,true)
                {name=clouds?"Disclosed clouds":"Disclosed surface",hideFlags=HideFlags.HideAndDontSave,wrapModeU=TextureWrapMode.Repeat,wrapModeV=TextureWrapMode.Clamp};
            ownedTextures.Add(texture);
            var pixels=new Color[width*height];
            for(int y=0;y<height;y++)for(int x=0;x<width;x++)
            {
                double longitude=(double)x/width*Math.PI*2,latitude=((double)y/(height-1)-.5)*Math.PI;
                double a=Math.Cos(latitude)*Math.Cos(longitude),b=Math.Sin(latitude),c=Math.Cos(latitude)*Math.Sin(longitude);
                if(clouds){var value=PlanetSurfaceGenerator.Clouds(profile,a,b,c);pixels[y*width+x]=new Color(value,value,value,1);}
                else {var sample=PlanetSurfaceGenerator.Sample(profile,a,b,c);pixels[y*width+x]=new Color(sample.R,sample.G,sample.B,sample.Water);}
            }
            texture.SetPixels(pixels);texture.Apply(true,true);return texture;
        }
        private void Fallback(string reason){assetFallbacks.Add(reason);Debug.LogWarning("System asset fallback: "+reason);}
        // Bake the IMPORTED prefab transform, never a guessed axis/100x correction.
        // Copies are stage-owned; source FBX meshes/textures remain borrowed Resources.
        private Mesh DeliveredMesh(string name,bool ring,Mesh fallback)
        {
            var prefab=Resources.Load<GameObject>("SystemBodyArt/SolMeshes/"+name);
            if(prefab==null){Fallback(name+": model missing");return fallback;}
            var filters=prefab.GetComponentsInChildren<MeshFilter>(true);
            if(filters.Length!=1||filters[0].sharedMesh==null){Fallback(name+": expected one mesh");return fallback;}
            var source=filters[0].sharedMesh;
            if(!source.isReadable){Fallback(name+": readable import required for transform validation");return fallback;}
            var vertices=source.vertices;var normals=source.normals;var uv=source.uv;
            if(vertices.Length==0||normals.Length!=vertices.Length||uv.Length!=vertices.Length||source.subMeshCount!=1)
            {Fallback(name+": normals/UV/submesh contract mismatch");return fallback;}
            var matrix=Matrix4x4.identity;
            for(var t=filters[0].transform;t!=null;t=t.parent)
                matrix=Matrix4x4.TRS(t.localPosition,t.localRotation,t.localScale)*matrix;
            if(float.IsNaN(matrix.determinant)||Mathf.Abs(matrix.determinant)<1e-8f)
            {Fallback(name+": invalid model transform");return fallback;}
            var normalMatrix=matrix.inverse.transpose;
            for(var i=0;i<vertices.Length;i++)
            {
                var v=matrix.MultiplyPoint3x4(vertices[i]);
                var radius=ring?new Vector2(v.x,v.z).magnitude:v.magnitude;
                var valid=ring?(Mathf.Abs(v.y)<.02f&&(Mathf.Abs(radius-1.22f)<.02f||Mathf.Abs(radius-2.27f)<.02f)):Mathf.Abs(radius-1)<.02f;
                if(!valid||float.IsNaN(uv[i].x)||float.IsNaN(uv[i].y)||float.IsInfinity(uv[i].x)||float.IsInfinity(uv[i].y))
                {Fallback(name+": centered radius/axis/UV contract mismatch");return fallback;}
                vertices[i]=v;normals[i]=normalMatrix.MultiplyVector(normals[i]).normalized;
            }
            var triangles=source.triangles;
            if(matrix.determinant<0)for(var i=0;i<triangles.Length;i+=3){var swap=triangles[i];triangles[i]=triangles[i+1];triangles[i+1]=swap;}
            var copy=new Mesh{name="Sol validated "+name,hideFlags=HideFlags.HideAndDontSave};
            importedCopies.Add(copy);
            copy.vertices=vertices;copy.normals=normals;copy.uv=uv;copy.triangles=triangles;copy.RecalculateBounds();
            return copy;
        }

        private GameObject Child(string name)
        {
            var part=new GameObject(name){hideFlags=HideFlags.HideAndDontSave,layer=Layer};
            part.transform.SetParent(root.transform,false);return part;
        }
        private MeshRenderer Part(string name,Mesh mesh,Vector3 position,float scale,Color tint,bool lit)
        {
            var part=Child(name);part.transform.localPosition=position;part.transform.localScale=Vector3.one*scale;
            part.AddComponent<MeshFilter>().sharedMesh=mesh;
            var renderer=part.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;
            renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
            properties.Clear();properties.SetColor("_Tint",tint);properties.SetFloat("_Lit",lit?1:0);
            properties.SetTexture("_Surface",Texture2D.whiteTexture);properties.SetTexture("_Clouds",Texture2D.blackTexture);
            properties.SetVector("_StarCenter",root.transform.position);renderer.SetPropertyBlock(properties);return renderer;
        }
        internal bool Render(float aspect,float yaw,float zoom,string selectedPlanet)
        {
            if(disposed||!Texture.IsCreated())return false;
            var pixels=Mathf.Clamp(Mathf.RoundToInt(400*aspect),160,1536);
            if(Texture.width!=pixels){Texture.Release();Texture.width=pixels;Texture.Create();}
            camera.aspect=aspect;camera.orthographicSize=Mathf.Max(extent*.92f,extent/aspect)/zoom;
            var tilt=Quaternion.Euler(54,0,0);
            orbitalRoot.localRotation=Quaternion.Euler(0,yaw,0);
            var centre=root.transform.position;
            if(planetTransforms.TryGetValue(selectedPlanet,out var selected))centre=Vector3.Lerp(centre,selected.position,Mathf.Clamp01((zoom-.9f)*.8f));
            camera.transform.SetPositionAndRotation(centre+tilt*new Vector3(0,0,-extent*2.5f),tilt);
            foreach(var orbit in planetOrbits)
            {
                orbit.Value.GetPropertyBlock(properties);properties.SetColor("_Tint",orbit.Key==selectedPlanet?new Color(.95f,.68f,.39f):OrbitColor);orbit.Value.SetPropertyBlock(properties);
            }
            root.SetActive(true);
            try
            {
                var request=new RenderPipeline.StandardRequest{destination=Texture};
                if(GraphicsSettings.currentRenderPipeline==null)camera.Render();
                else if(RenderPipeline.SupportsRenderRequest(camera,request))RenderPipeline.SubmitRenderRequest(camera,request);
                else return false;
                return true;
            }
            finally{root.SetActive(false);}
        }
        internal bool TryGetPosition(string id,out Vector2 position)
        {
            position=default;if(disposed||!planetTransforms.TryGetValue(id,out var planet))return false;
            var p=camera.WorldToViewportPoint(planet.position);position=new Vector2(p.x,1-p.y);
            return p.z>0&&p.x>=0&&p.x<=1&&p.y>=0&&p.y<=1;
        }
        internal bool TryGetPortraitRect(string id,out Rect pixels)
        {
            pixels=default;if(disposed||!planetTransforms.TryGetValue(id,out var planet))return false;
            var centre=camera.WorldToViewportPoint(planet.position);
            var dx=.65f/(camera.orthographicSize*2*camera.aspect);var dy=.65f/(camera.orthographicSize*2);
            pixels=new Rect((centre.x-dx)*Texture.width,(centre.y-dy)*Texture.height,dx*2*Texture.width,dy*2*Texture.height);
            return centre.z>0;
        }
        private static Mesh Sphere()
        {
            const int rows=24, columns=48;
            var points=new List<Vector3>();
            var uvs=new List<Vector2>();
            var triangles=new List<int>();
            for(int row=0;row<=rows;row++)
            {
                var v=(float)row/rows;
                var phi=v*Mathf.PI;
                for(int col=0;col<=columns;col++)
                {
                    var u=(float)col/columns;
                    var theta=u*2*Mathf.PI;
                    points.Add(new Vector3(Mathf.Sin(phi)*Mathf.Cos(theta),Mathf.Cos(phi),Mathf.Sin(phi)*Mathf.Sin(theta)));
                    uvs.Add(new Vector2(u,1-v));
                    if(row==rows||col==columns)continue;
                    var a=row*(columns+1)+col;
                    var b=a+columns+1;
                    triangles.AddRange(new[]{a,a+1,b,a+1,b+1,b});
                }
            }
            var mesh=new Mesh { name="Sol sphere", hideFlags=HideFlags.HideAndDontSave };
            try
            {
            mesh.SetVertices(points);
            mesh.SetNormals(points); // Unit-radius positions are the outward normals.
            mesh.SetUVs(0,uvs);
            mesh.SetTriangles(triangles,0);
            return mesh;
            }
            catch {Destroy(mesh);throw;}
        }

        private static Mesh Annulus(float inner,float outer,string name)
        {
            const int segments=128;
            var points=new List<Vector3>();
            var uvs=new List<Vector2>();
            var triangles=new List<int>();
            for(int i=0;i<=segments;i++)
            {
                var u=(float)i/segments;
                var theta=u*2*Mathf.PI;
                var ray=new Vector3(Mathf.Cos(theta),0,Mathf.Sin(theta));
                points.Add(ray*inner);points.Add(ray*outer);
                uvs.Add(new Vector2(u,0));uvs.Add(new Vector2(u,1));
                if(i==segments)continue;
                var a=2*i;
                triangles.AddRange(new[]{a,a+2,a+1,a+1,a+2,a+3});
            }
            var mesh=new Mesh { name=name, hideFlags=HideFlags.HideAndDontSave };
            try {mesh.SetVertices(points);mesh.SetUVs(0,uvs);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();return mesh;}
            catch {Destroy(mesh);throw;}
        }

        private static void Destroy(UnityEngine.Object? value)
        {if(value==null)return;if(UnityEngine.Application.isPlaying)UnityEngine.Object.Destroy(value);else UnityEngine.Object.DestroyImmediate(value);}
        public void Dispose()
        {
            if(disposed)return;disposed=true;if(root!=null)root.SetActive(false);if(camera!=null)camera.targetTexture=null;if(Texture!=null)Texture.Release();
            Destroy(root);Destroy(Texture);Destroy(material);Destroy(sphere);Destroy(track);
            foreach(var mesh in importedCopies)Destroy(mesh);
            foreach(var texture in ownedTextures)Destroy(texture);
            importedCopies.Clear();ownedTextures.Clear();surfaces.Clear();
        }
    }
}
