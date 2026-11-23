#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HobbyDS9.Contracts;
using NUnit.Framework;
using UnityEngine;
namespace HobbyDS9.UnityReview.Tests
{
    public sealed class SystemBeltRenderTests
    {
        [Test]public void ActualImportedResourcesBuildTwoObjectsAndDisposeOwnedMeshesAcrossDetailSwitches()
        {
            var root=new GameObject("Belt receiving test");var failures=new List<string>();
            var sky=new CampaignStaticAstronomySnapshot("human","map",3,7,Array.Empty<CampaignNebulaVisualProxy>(),new[]{new SystemAsteroidBeltDto{FeatureId="belt.a",SystemId="system.a",InnerRadiusMilliAu=2200,OuterRadiusMilliAu=2700,VisualSeed=new string('a',64)}});
            var slots=BeltVisualLayout.ForSystem(sky,"human",3,7,"system.a");
            var type=typeof(ReviewEntry).Assembly.GetType("HobbyDS9.UnityReview.ReviewSystemBelts",true)!;
            using(var belts=(IDisposable)Activator.CreateInstance(type,BindingFlags.Instance|BindingFlags.NonPublic,null,new object[]{root.transform,31,slots,Vector3.zero,(Action<string>)failures.Add},null)!)
            {
                void Detail(float halfHeight)=>type.GetMethod("SetDetail",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(belts,new object[]{halfHeight,400});
                Assert.That(failures,Is.Empty);Assert.That(root.transform.childCount,Is.EqualTo(2));
                var rocks=root.GetComponentsInChildren<MeshFilter>().Single(f=>f.name.Contains("rocks"));
                Detail(20);Assert.That(rocks.sharedMesh.triangles.Length/3,Is.EqualTo(25600));
                Detail(100);Assert.That(rocks.sharedMesh.triangles.Length/3,Is.EqualTo(2400));
                Detail(1000);Assert.That(rocks.GetComponent<MeshRenderer>().enabled,Is.False);
                Detail(20);Assert.That(rocks.sharedMesh.triangles.Length/3,Is.EqualTo(25600));
                Assert.That(root.GetComponentsInChildren<Collider>().Length,Is.EqualTo(0));
            }
            UnityEngine.Object.DestroyImmediate(root);
        }
    }
}
