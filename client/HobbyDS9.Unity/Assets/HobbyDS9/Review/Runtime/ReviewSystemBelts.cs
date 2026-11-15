#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
namespace HobbyDS9.UnityReview
{
    // Two stage objects, cached detail meshes, no physics/picking/gameplay or frame RNG.
    internal sealed class ReviewSystemBelts:IDisposable
    {
        private const string Path="SystemBodyArt/AsteroidBeltV1/";
        private readonly List<Mesh> meshes=new List<Mesh>();
        private readonly List<Material> materials=new List<Material>();
        private readonly List<GameObject> objects=new List<GameObject>();
        private MeshRenderer? rocks;private MeshFilter? rockFilter;private Mesh? high,low;
        private float outer;private int detail=-1;private bool disposed;
        private sealed class Geometry
        {
            internal Vector3[] Vertices=Array.Empty<Vector3>(),Normals=Array.Empty<Vector3>();internal Color[] Colors=Array.Empty<Color>();internal Vector2[] UV=Array.Empty<Vector2>();internal int[] Triangles=Array.Empty<int>();
        }
        internal ReviewSystemBelts(Transform parent,int layer,IReadOnlyList<BeltVisualSlot> slots,Vector3 star,Action<string> fallback)
        {
            try
            {
                if(slots.Count<1||slots.Count>BeltVisualLayout.MaximumFeatures)throw new ArgumentException("Belt feature budget");
                outer=(float)slots.Max(s=>s.Envelope);
                var rockShader=Resources.Load<Shader>(Path+"BeltRock");var dustShader=Resources.Load<Shader>(Path+"BeltSoftAnnulus");
                if(rockShader==null||dustShader==null||!rockShader.isSupported||!dustShader.isSupported)throw new InvalidOperationException("Belt shaders unavailable");
                var prototypes=new[]{0,1}.Select(lod=>Enumerable.Range(0,4).Select(i=>Read("Belt_Rock_"+i+"_LOD"+lod,false,lod==0?80:20)).ToArray()).ToArray();
                var annulus=Read("Belt_SoftAnnulus",true,384);
                high=BuildRocks(slots,prototypes[0],false);low=BuildRocks(slots,prototypes[1],true);
                var dust=BuildDust(slots,annulus);
                var opaque=new Material(rockShader){name="Disclosed belt rocks",hideFlags=HideFlags.HideAndDontSave};materials.Add(opaque);opaque.SetVector("_StarCenter",star);
                var soft=new Material(dustShader){name="Disclosed belt haze",hideFlags=HideFlags.HideAndDontSave};materials.Add(soft);soft.SetFloat("_Opacity",.065f);
                rocks=Object("Disclosed asteroid belt rocks",high,opaque,parent,layer);rockFilter=rocks.GetComponent<MeshFilter>();
                Object("Disclosed asteroid belt haze",dust,soft,parent,layer);
            }
            catch(Exception e){Dispose();fallback("Asteroid belt: "+e.Message+". Disclosed feature remains in the text list.");}
        }
        private MeshRenderer Object(string name,Mesh mesh,Material material,Transform parent,int layer)
        {
            var value=new GameObject(name){hideFlags=HideFlags.HideAndDontSave,layer=layer};objects.Add(value);value.transform.SetParent(parent,false);
            value.AddComponent<MeshFilter>().sharedMesh=mesh;var renderer=value.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;return renderer;
        }
        private static bool Finite(float v)=>!float.IsNaN(v)&&!float.IsInfinity(v);
        private static Geometry Read(string name,bool annulus,int expectedTriangles)
        {
            var prefab=Resources.Load<GameObject>(Path+name);if(prefab==null)throw new InvalidOperationException(name+" missing");
            var filters=prefab.GetComponentsInChildren<MeshFilter>(true);
            if(filters.Length!=1||filters[0].sharedMesh==null||!filters[0].sharedMesh.isReadable)throw new InvalidOperationException(name+" requires one readable imported mesh");
            var source=filters[0].sharedMesh;var g=new Geometry{Vertices=source.vertices,Normals=source.normals,Colors=source.colors,UV=source.uv,Triangles=source.triangles};
            if(source.subMeshCount!=1||g.Triangles.Length!=expectedTriangles*3||g.Vertices.Length==0||g.Normals.Length!=g.Vertices.Length||(!annulus&&g.Colors.Length!=g.Vertices.Length)||(annulus&&g.UV.Length!=g.Vertices.Length))throw new InvalidOperationException(name+" geometry/color/normal/UV contract");
            var matrix=Matrix4x4.identity;for(var t=filters[0].transform;t!=null;t=t.parent)matrix=Matrix4x4.TRS(t.localPosition,t.localRotation,t.localScale)*matrix;
            if(!Finite(matrix.determinant)||Mathf.Abs(matrix.determinant)<1e-8f)throw new InvalidOperationException(name+" degenerate imported transform");
            var normal=matrix.inverse.transpose;
            for(var i=0;i<g.Vertices.Length;i++)
            {
                var v=matrix.MultiplyPoint3x4(g.Vertices[i]);var n=normal.MultiplyVector(g.Normals[i]).normalized;
                if(!Finite(v.x)||!Finite(v.y)||!Finite(v.z)||!Finite(n.x)||!Finite(n.y)||!Finite(n.z)||n.sqrMagnitude<.9f)throw new InvalidOperationException(name+" non-finite geometry");
                var radius=new Vector2(v.x,v.z).magnitude;
                if(annulus?(Mathf.Abs(v.y)>.02f||Mathf.Min(Mathf.Abs(radius-1),Mathf.Abs(radius-1.24f))>.02f):v.magnitude>1.02f)throw new InvalidOperationException(name+" centered axis/radius contract");
                g.Vertices[i]=v;g.Normals[i]=n;
            }
            if(matrix.determinant<0)for(var i=0;i<g.Triangles.Length;i+=3){var t=g.Triangles[i];g.Triangles[i]=g.Triangles[i+1];g.Triangles[i+1]=t;}
            return g;
        }
        // Imported Unity basis -> Blender XYZ -> intrinsic XYZ rotation -> Unity.
        // B maps (x,y,z) to (x,z,y); B Rz Ry Rx B^-1 has positive determinant.
        private static Vector3 Rotate(Vector3 unity,BeltRockPlacement p,bool normal)
        {
            double x=unity.x,y=unity.z,z=unity.y;
            if(normal){x/=p.Sx;y/=p.Sy;z/=p.Sz;}else{x*=p.Sx;y*=p.Sy;z*=p.Sz;}
            var cy=Math.Cos(p.Rx)*y-Math.Sin(p.Rx)*z;z=Math.Sin(p.Rx)*y+Math.Cos(p.Rx)*z;y=cy;
            var cx=Math.Cos(p.Ry)*x+Math.Sin(p.Ry)*z;z=-Math.Sin(p.Ry)*x+Math.Cos(p.Ry)*z;x=cx;
            cx=Math.Cos(p.Rz)*x-Math.Sin(p.Rz)*y;y=Math.Sin(p.Rz)*x+Math.Cos(p.Rz)*y;x=cx;
            var result=new Vector3((float)x,(float)z,(float)y);return normal?result.normalized:result;
        }
        private Mesh BuildRocks(IReadOnlyList<BeltVisualSlot> slots,Geometry[] prototypes,bool lower)
        {
            var vertices=new List<Vector3>();var normals=new List<Vector3>();var colors=new List<Color>();var triangles=new List<int>();
            for(var index=0;index<slots.Count;index++)
            {
                var slot=slots[index];var mid=(slot.Inner+slot.Outer)/2;
                foreach(var point in BeltVisualLayout.Rocks(BeltVisualLayout.Seed(slot.Feature.SystemId,slot.Feature.FeatureId,slot.Feature.VisualSeed),BeltVisualLayout.RockBudget(index,slots.Count,lower)))
                {
                    var g=prototypes[point.Prototype];var start=vertices.Count;var radius=(float)Math.Min(mid*point.Radius,.15*(slot.Outer-slot.Inner));
                    var radial=slot.Inner+(slot.Outer-slot.Inner)*point.Radial;var angle=2*Math.PI*point.Turns;
                    var centre=new Vector3((float)(Math.Cos(angle)*radial),(float)(point.Height*mid),(float)(Math.Sin(angle)*radial));
                    for(var i=0;i<g.Vertices.Length;i++){vertices.Add(centre+Rotate(g.Vertices[i],point,false)*radius);normals.Add(Rotate(g.Normals[i],point,true));colors.Add(g.Colors[i]*(float)point.Tone);}
                    triangles.AddRange(g.Triangles.Select(t=>t+start));
                }
            }
            return Mesh("Disclosed belt "+(lower?"LOD1":"LOD0"),vertices,normals,triangles,colors,null);
        }
        private Mesh BuildDust(IReadOnlyList<BeltVisualSlot> slots,Geometry g)
        {
            var vertices=new List<Vector3>();var normals=new List<Vector3>();var uv=new List<Vector2>();var triangles=new List<int>();
            foreach(var slot in slots)
            {
                var start=vertices.Count;
                for(var i=0;i<g.Vertices.Length;i++){var v=g.Vertices[i];var radial=new Vector2(v.x,v.z).magnitude;var target=radial<1.12f?slot.Inner:slot.Outer;vertices.Add(new Vector3(v.x*(float)(target/radial),-.015f,v.z*(float)(target/radial)));normals.Add(Vector3.up);uv.Add(g.UV[i]);}
                triangles.AddRange(g.Triangles.Select(t=>start+t));
            }
            return Mesh("Disclosed belt haze",vertices,normals,triangles,null,uv);
        }
        private Mesh Mesh(string name,List<Vector3> vertices,List<Vector3> normals,List<int> triangles,List<Color>? colors,List<Vector2>? uv)
        {
            var mesh=new Mesh{name=name,hideFlags=HideFlags.HideAndDontSave,indexFormat=IndexFormat.UInt32};meshes.Add(mesh);mesh.SetVertices(vertices);mesh.SetNormals(normals);if(colors!=null)mesh.SetColors(colors);if(uv!=null)mesh.SetUVs(0,uv);mesh.SetTriangles(triangles,0);mesh.RecalculateBounds();return mesh;
        }
        internal void SetDetail(float halfHeight,int pixelHeight)
        {
            if(disposed||rocks==null||rockFilter==null)return;var next=BeltVisualLayout.Detail(outer/Mathf.Max(.01f,halfHeight)*pixelHeight,detail);if(next==detail)return;detail=next;
            rocks.enabled=detail<2;rockFilter.sharedMesh=detail==0?high:low;
        }
        private static void Destroy(UnityEngine.Object value){if(UnityEngine.Application.isPlaying)UnityEngine.Object.Destroy(value);else UnityEngine.Object.DestroyImmediate(value);}
        public void Dispose(){if(disposed)return;disposed=true;foreach(var value in objects)Destroy(value);foreach(var value in meshes)Destroy(value);foreach(var value in materials)Destroy(value);objects.Clear();meshes.Clear();materials.Clear();}
    }
}
