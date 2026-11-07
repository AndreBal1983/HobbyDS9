#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace HobbyDS9.UnityReview
{
    // A schematic presentation scene. Only the caller's disclosed snapshots can become controls.
    internal sealed class ReviewSolStage : IDisposable
    {
        private sealed class Body
        {
            internal readonly string Map;
            internal readonly float Orbit, Angle, Radius;
            internal readonly string? PlanetId;
            internal Body(string map, float orbit, float angle, float radius, string? planetId = null)
            { Map=map; Orbit=orbit; Angle=angle; Radius=radius; PlanetId=planetId; }
        }

        private static readonly Body[] Bodies =
        {
            new Body("sun",0,0,.99f),
            new Body("mercury",1.72f,-118,.22f),
            new Body("venus_atmosphere",2.52f,109,.33f),
            new Body("earth_daymap",3.47f,-22,.46f,"planet.sol"),
            new Body("mars",4.52f,161,.36f,"planet.mars"),
            new Body("jupiter",5.65f,65,.82f),
            new Body("saturn",6.8f,-136,.67f),
            new Body("uranus",7.97f,-77,.53f),
            new Body("neptune",9.04f,24,.49f),
        };
        private const int Layer = 31;
        private readonly GameObject root;
        private readonly Camera camera;
        private readonly Material material;
        private readonly Mesh sphere, track, rings;
        private readonly List<Mesh> importedCopies = new List<Mesh>();
        private readonly List<string> assetFallbacks = new List<string>();
        private readonly Material? ringMaterial;
        internal int ImportedMeshCount => importedCopies.Count;
        internal IReadOnlyList<string> AssetFallbacks => assetFallbacks.AsReadOnly();
        private readonly Dictionary<string,Transform> planetTransforms = new Dictionary<string,Transform>(StringComparer.Ordinal);
        private readonly Dictionary<string,MeshRenderer> planetOrbits = new Dictionary<string,MeshRenderer>(StringComparer.Ordinal);
        private readonly MaterialPropertyBlock properties = new MaterialPropertyBlock();
        private static readonly Color OrbitColor = new Color(.15f,.22f,.33f);
        private bool disposed;
        internal RenderTexture Texture { get; }

        internal ReviewSolStage(Shader shader)
        {
            root=new GameObject("Sol presentation stage") { hideFlags=HideFlags.HideAndDontSave, layer=Layer };
            root.SetActive(false);
            root.transform.position=new Vector3(0,-10000,0);
            camera=Child("Sol preview camera").AddComponent<Camera>();
            camera.enabled=false;
            camera.orthographic=true;
            camera.nearClipPlane=.1f;
            camera.farClipPlane=55f;
            camera.clearFlags=CameraClearFlags.SolidColor;
            camera.backgroundColor=new Color(.019f,.027f,.049f,1);
            camera.cullingMask=1<<Layer;
            camera.allowHDR=false;
            camera.allowMSAA=false;
            Texture=new RenderTexture(960,400,24,RenderTextureFormat.ARGB32)
                { name="Sol overview", hideFlags=HideFlags.HideAndDontSave, antiAliasing=1 };
            Texture.Create();
            camera.targetTexture=Texture;
            material=new Material(shader) { hideFlags=HideFlags.HideAndDontSave };
            sphere=Sphere();
            track=Annulus(.996f,1, "Sol orbit");
            rings=Annulus(.83f,1.38f, "Saturn rings");
            var ringShader=Resources.Load<Shader>("SystemBodyArt/SolMeshes/SolRing");
            var ringMap=Resources.Load<Texture2D>("SystemBodyArt/SolMeshes/saturn_rings");
            if(ringShader!=null&&ringShader.isSupported&&ringMap!=null)
            {
                ringMaterial=new Material(ringShader) { hideFlags=HideFlags.HideAndDontSave };
                ringMaterial.SetTexture("_Surface",ringMap);
            }
            else Fallback("Saturn ring material or alpha texture unavailable; using legacy rings.");
            foreach(var body in Bodies)
            {
                var theta=body.Angle*Mathf.Deg2Rad;
                var position=new Vector3(Mathf.Cos(theta)*body.Orbit,0,Mathf.Sin(theta)*body.Orbit);
                var texture=Resources.Load<Texture2D>("SystemBodyArt/"+body.Map);
                var mesh=DeliveredMesh(BodyMesh(body.Map),false,sphere);
                var planet=Part(body.Map,mesh,position,body.Radius,Color.white,texture,body.Orbit>0);
                planet.shadowCastingMode=ShadowCastingMode.Off;planet.receiveShadows=false;
                if(body.PlanetId!=null)planetTransforms.Add(body.PlanetId,planet.transform);
                if(body.Orbit==0)continue;
                var orbit=Part("Schematic orbit",track,new Vector3(0,-.045f,0),body.Orbit,OrbitColor);
                if(body.PlanetId!=null)planetOrbits.Add(body.PlanetId,orbit);
                if(body.Map=="saturn")
                {
                    var ringMesh=ringMaterial==null?rings:DeliveredMesh("Saturn_Rings_LOD1",true,rings);
                    var delivered=ringMaterial!=null&&!ReferenceEquals(ringMesh,rings);
                    var ring=Part("Saturn rings",ringMesh,position,delivered?body.Radius:1,delivered?Color.white:new Color(.72f,.57f,.42f),striped:!delivered);
                    if(delivered)ring.sharedMaterial=ringMaterial;
                    ring.shadowCastingMode=ShadowCastingMode.Off;ring.receiveShadows=false;
                    ring.transform.localRotation=delivered?Quaternion.Euler(26.7f,0,0):Quaternion.Euler(11,0,22);
                }
            }
        }

        private static string BodyMesh(string map)=>map switch
        {
            "sun"=>"Sun_LOD1", "mercury"=>"Mercury_LOD1", "venus_atmosphere"=>"Venus_LOD1",
            "earth_daymap"=>"Earth_LOD1", "mars"=>"Mars_LOD1", "jupiter"=>"Jupiter_LOD1",
            "saturn"=>"Saturn_LOD1", "uranus"=>"Uranus_LOD1", "neptune"=>"Neptune_LOD1",
            _=>""
        };

        private void Fallback(string reason)
        { assetFallbacks.Add(reason);Debug.LogWarning("Sol asset fallback: "+reason); }

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
            copy.vertices=vertices;copy.normals=normals;copy.uv=uv;copy.triangles=triangles;copy.RecalculateBounds();
            importedCopies.Add(copy);return copy;
        }

        private GameObject Child(string name)
        {
            var part=new GameObject(name) { hideFlags=HideFlags.HideAndDontSave, layer=Layer };
            part.transform.SetParent(root.transform,false);
            return part;
        }

        private MeshRenderer Part(string name, Mesh mesh, Vector3 position, float scale, Color color,
            Texture2D? map=null, bool lit=false, bool striped=false)
        {
            var part=Child(name);
            part.transform.localPosition=position;
            part.transform.localScale=Vector3.one*scale;
            part.AddComponent<MeshFilter>().sharedMesh=mesh;
            var renderer=part.AddComponent<MeshRenderer>();
            renderer.sharedMaterial=material;
            properties.Clear();
            properties.SetColor("_Tint",color);
            properties.SetFloat("_Lit",lit?1:0);
            properties.SetFloat("_Striped",striped?1:0);
            properties.SetVector("_StarCenter",root.transform.position);
            if(map!=null)properties.SetTexture("_Surface",map);
            renderer.SetPropertyBlock(properties);
            return renderer;
        }

        internal bool Render(float aspect, float yaw, float zoom, string selectedPlanet)
        {
            if(disposed||!Texture.IsCreated())return false;
            camera.aspect=aspect;
            camera.orthographicSize=Mathf.Max(6.9f,10.4f/aspect)/zoom;
            var tilt=Quaternion.Euler(54,yaw,0);
            camera.transform.SetPositionAndRotation(root.transform.position+tilt*new Vector3(0,0,-24),tilt);
            foreach(var orbit in planetOrbits)
            {
                orbit.Value.GetPropertyBlock(properties);
                properties.SetColor("_Tint",orbit.Key==selectedPlanet?new Color(.95f,.68f,.39f):OrbitColor);
                orbit.Value.SetPropertyBlock(properties);
            }
            root.SetActive(true);
            try
            {
                var request=new RenderPipeline.StandardRequest { destination=Texture };
                if(GraphicsSettings.currentRenderPipeline==null)camera.Render();
                else if(RenderPipeline.SupportsRenderRequest(camera,request))RenderPipeline.SubmitRenderRequest(camera,request);
                else return false;
                return true;
            }
            finally { root.SetActive(false); }
        }

        internal bool TryGetPosition(string planetId, out Vector2 position)
        {
            position=default;
            if(disposed||!planetTransforms.TryGetValue(planetId,out var planet))return false;
            var p=camera.WorldToViewportPoint(planet.position);
            position=new Vector2(p.x,1-p.y);
            return p.z>0&&p.x>=0&&p.x<=1&&p.y>=0&&p.y<=1;
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
            mesh.SetVertices(points);
            mesh.SetNormals(points); // Unit-radius positions are the outward normals.
            mesh.SetUVs(0,uvs);
            mesh.SetTriangles(triangles,0);
            return mesh;
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
            mesh.SetVertices(points);mesh.SetUVs(0,uvs);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();
            return mesh;
        }

        private static void Destroy(UnityEngine.Object value)
        {
            if(UnityEngine.Application.isPlaying)UnityEngine.Object.Destroy(value);
            else UnityEngine.Object.DestroyImmediate(value);
        }

        public void Dispose()
        {
            if(disposed)return;
            disposed=true;
            root.SetActive(false);
            camera.targetTexture=null;
            Texture.Release();
            Destroy(root);Destroy(Texture);Destroy(material);Destroy(sphere);Destroy(track);Destroy(rings);
            if(ringMaterial!=null)Destroy(ringMaterial);
            foreach(var copy in importedCopies)Destroy(copy);
            importedCopies.Clear();
        }
    }
}
