#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HobbyDS9.Contracts;
using NUnit.Framework;
using UnityEngine;

namespace HobbyDS9.UnityReview.Tests
{
    // Dedicated EditMode graphics fixture, not PlayMode or native player acceptance.
    public sealed class SolAssetBindingTests
    {
        private const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        [SetUp] public void RequireDedicatedRun()
        {
            if(!Environment.GetCommandLineArgs().Contains("--hobbyds9-sol-asset-tests"))
                Assert.Ignore("Root must admit a dedicated graphics-capable Sol receiving run.");
        }
        private static ColonisationPlanetSnapshot Planet(string id,bool surveyed=true,string kind="Temperate")
            =>new ColonisationPlanetSnapshot(new ColonisationPlanetObservationDto{
                PlanetId=id,BodyId="body."+id,SystemId="system.fixture",DisplayName=id,Surveyed=surveyed,
                Environment=new PlanetDefinitionDto{PlanetClass=kind,Atmosphere=kind=="Vacuum"?"atmosphere.vacuum":"atmosphere.oxygen-nitrogen",Pressure=10000}});
        private static object Field(object stage,string name)=>stage.GetType().GetField(name,Private)!.GetValue(stage)!;
        private static object Stage(params ColonisationPlanetSnapshot[] bodies)
        {
            var shader=Resources.Load<Shader>("SystemBodyArt/ProceduralSurface");
            Assert.That(shader,Is.Not.Null);Assert.That(shader.isSupported,Is.True);
            var type=typeof(ReviewPlayerView).Assembly.GetType("HobbyDS9.UnityReview.ReviewSolStage",true)!;
            return Activator.CreateInstance(type,Private,null,new object[]{shader,bodies},null)!;
        }
        [Test] public void DeliveredMeshAndRingAssetsRetainTheirImportContract()
        {
            // Asset presence is distinct from permission to put a body in a campaign scene.
            foreach(var name in new[]{"Sun","Mercury","Venus","Earth","Mars","Jupiter","Saturn","Uranus","Neptune","Saturn_Rings"})
            {
                var asset=Resources.Load<GameObject>("SystemBodyArt/SolMeshes/"+name+"_LOD1");
                Assert.That(asset,Is.Not.Null,name);
                var filters=asset.GetComponentsInChildren<MeshFilter>(true);
                Assert.That(filters.Length,Is.EqualTo(1),name);var mesh=filters[0].sharedMesh;
                Assert.That(mesh,Is.Not.Null,name);Assert.That(mesh.isReadable,Is.True,name);
                Assert.That(mesh.vertexCount,Is.GreaterThan(0),name);
                Assert.That(mesh.normals.Length,Is.EqualTo(mesh.vertexCount),name);
                Assert.That(mesh.uv.Length,Is.EqualTo(mesh.vertexCount),name);
                Assert.That(mesh.subMeshCount,Is.EqualTo(1),name);
            }
            var ring=Resources.Load<Shader>("SystemBodyArt/SolMeshes/SolRing");
            Assert.That(ring,Is.Not.Null);Assert.That(ring.isSupported,Is.True);
            Assert.That(Resources.Load<Texture2D>("SystemBodyArt/SolMeshes/saturn_rings"),Is.Not.Null);
        }
        [Test] public void DisclosedBodiesReuseOneValidatedMeshAndDisposeOnlyOwnedResources()
        {
            var borrowedPrefab=Resources.Load<GameObject>("SystemBodyArt/SolMeshes/Earth_LOD1");
            Assert.That(borrowedPrefab,Is.Not.Null);
            var borrowedMesh=borrowedPrefab.GetComponentInChildren<MeshFilter>().sharedMesh;
            var borrowedMaps=new[]{Resources.Load<Texture2D>("SystemBodyArt/earth_daymap"),Resources.Load<Texture2D>("SystemBodyArt/mars")};
            foreach(var map in borrowedMaps)Assert.That(map,Is.Not.Null);
            var unrelated=new GameObject("Unrelated disposal sentinel");
            object? stage=null;var owned=new List<UnityEngine.Object>();
            try
            {
                stage=Stage(Planet("planet.sol"),Planet("planet.mars",kind:"Vacuum"));
                var transforms=(Dictionary<string,Transform>)Field(stage,"planetTransforms");
                Assert.That(transforms.Keys,Is.EquivalentTo(new[]{"planet.sol","planet.mars"}),"No decorative undisclosed planet may be instantiated.");
                Assert.That((int)stage.GetType().GetProperty("ImportedMeshCount",Private)!.GetValue(stage)!,Is.EqualTo(1),"Unit sphere mesh is shared, not one imported copy per body.");
                var fallbacks=(IEnumerable)stage.GetType().GetProperty("AssetFallbacks",Private)!.GetValue(stage)!;
                Assert.That(fallbacks.Cast<object>(),Is.Empty,"A procedural fallback must not pass as the delivered mesh.");
                var first=transforms["planet.sol"].GetComponent<MeshFilter>().sharedMesh;
                Assert.That(transforms["planet.mars"].GetComponent<MeshFilter>().sharedMesh,Is.SameAs(first));
                Assert.That(first,Is.Not.SameAs(borrowedMesh));
                Assert.That(first.vertices.All(p=>Mathf.Abs(p.magnitude-1)<.02f),Is.True,"Validate actual baked mesh positions.");
                var root=(GameObject)Field(stage,"root");
                Assert.That(root.GetComponentsInChildren<MeshRenderer>(true).Length,Is.EqualTo(5),"One star, two disclosed bodies and their two orbits.");
                Assert.That(root.GetComponentsInChildren<Camera>(true).Length,Is.EqualTo(1));
                owned.Add(root);owned.Add((Material)Field(stage,"material"));owned.Add((Mesh)Field(stage,"sphere"));owned.Add((Mesh)Field(stage,"track"));
                owned.Add((RenderTexture)stage.GetType().GetProperty("Texture",Private)!.GetValue(stage)!);
                owned.AddRange(((IEnumerable)Field(stage,"importedCopies")).Cast<UnityEngine.Object>());
                owned.AddRange(((IEnumerable)Field(stage,"ownedTextures")).Cast<UnityEngine.Object>());
            }
            finally
            {
                if(stage!=null){((IDisposable)stage).Dispose();((IDisposable)stage).Dispose();}
                try
                {
                    Assert.That(owned.All(value=>value==null),Is.True,"All captured stage-owned Unity resources must be destroyed in EditMode.");
                    Assert.That(unrelated!=null,Is.True);
                    Assert.That(borrowedPrefab!=null&&borrowedMesh!=null,Is.True);
                    Assert.That(borrowedMaps.All(value=>value!=null),Is.True,"Borrowed textures must survive.");
                }
                finally {if(unrelated!=null)UnityEngine.Object.DestroyImmediate(unrelated);}
            }
        }
        [Test] public void MalformedUnsurveyedEnvironmentsBindIdenticalNeutralRenderInputs()
        {
            var a=Planet("planet.sol",false,"Oceanic");var b=Planet("planet.secret",false,"Vacuum");
            var stage=Stage(a,b);
            try
            {
                var transforms=(Dictionary<string,Transform>)Field(stage,"planetTransforms");
                var first=new MaterialPropertyBlock();var second=new MaterialPropertyBlock();
                transforms[a.PlanetId].GetComponent<MeshRenderer>().GetPropertyBlock(first);
                transforms[b.PlanetId].GetComponent<MeshRenderer>().GetPropertyBlock(second);
                Assert.That(first.GetFloat("_Unknown"),Is.EqualTo(1));Assert.That(second.GetFloat("_Unknown"),Is.EqualTo(1));
                Assert.That(first.GetFloat("_Atmosphere"),Is.Zero);Assert.That(second.GetFloat("_Atmosphere"),Is.Zero);
                Assert.That(first.GetTexture("_Surface"),Is.SameAs(second.GetTexture("_Surface")));
                Assert.That(first.GetTexture("_Clouds"),Is.SameAs(second.GetTexture("_Clouds")));
                Assert.That(first.GetTexture("_Surface"),Is.Not.SameAs(Resources.Load<Texture2D>("SystemBodyArt/earth_daymap")));
                Assert.That(transforms[a.PlanetId].GetComponent<MeshFilter>().sharedMesh,Is.SameAs(transforms[b.PlanetId].GetComponent<MeshFilter>().sharedMesh));
                Assert.That((int)stage.GetType().GetProperty("ImportedMeshCount",Private)!.GetValue(stage)!,Is.Zero);
            }
            finally {((IDisposable)stage).Dispose();}
        }
        private static object[] StageResourceIdentities()
        {
            // Only distinctive stage-owned resource names; no assets/files/owner stores.
            var roots=Resources.FindObjectsOfTypeAll<GameObject>().Where(x=>x.name=="Disclosed system presentation").Cast<UnityEngine.Object>();
            var targets=Resources.FindObjectsOfTypeAll<RenderTexture>().Where(x=>x.name=="Disclosed system overview").Cast<UnityEngine.Object>();
            var textures=Resources.FindObjectsOfTypeAll<Texture2D>().Where(x=>x.name=="Neutral unresolved surface"||x.name=="Disclosed surface"||x.name=="Disclosed clouds").Cast<UnityEngine.Object>();
            var meshes=Resources.FindObjectsOfTypeAll<Mesh>().Where(x=>x.name=="Sol sphere"||x.name=="Schematic orbit"||x.name.StartsWith("Sol validated ",StringComparison.Ordinal)).Cast<UnityEngine.Object>();
            return roots.Concat(targets).Concat(textures).Concat(meshes).Cast<object>().ToArray();
        }
        private static void AssertSameStageResources(object[] before, string message)
        {
            var after=StageResourceIdentities();
            Assert.That(after.Length,Is.EqualTo(before.Length),message);
            foreach (var resource in before)
                Assert.That(after.Any(other=>ReferenceEquals(resource,other)),Is.True,message);
        }
        [Test] public void ConstructorFailureBeforeTextureAllocationStillReleasesRootAndCamera()
        {
            var before=StageResourceIdentities();
            var failure=Assert.Throws<TargetInvocationException>(()=>Stage((ColonisationPlanetSnapshot[])null!));
            Assert.That(failure!.InnerException,Is.TypeOf<NullReferenceException>().Or.TypeOf<ArgumentNullException>());
            AssertSameStageResources(before,"Failure before the render target exists must still clean the already allocated root/camera.");
        }
        [Test] public void ConstructorFailureAfterSurfaceCreationReleasesPartialStageResources()
        {
            // Failure is inside the dedicated test-owned constructor, after one generated
            // body. This never injects a fault into an application/campaign or borrowed asset.
            var before=StageResourceIdentities();
            var failure=Assert.Throws<TargetInvocationException>(()=>Stage(Planet("planet.generated"),null!));
            Assert.That(failure!.InnerException,Is.TypeOf<ArgumentNullException>());
            AssertSameStageResources(before,"Failed construction must release roots, render targets, generated textures and mesh copies.");
        }
        [Test] public void EmptyDisclosedSetHasOnlyTheSchematicStar()
        {
            var stage=Stage();
            try
            {
                Assert.That(((Dictionary<string,Transform>)Field(stage,"planetTransforms")),Is.Empty);
                Assert.That(((GameObject)Field(stage,"root")).GetComponentsInChildren<MeshRenderer>(true).Length,Is.EqualTo(1));
            }
            finally {((IDisposable)stage).Dispose();}
        }
    }
}
