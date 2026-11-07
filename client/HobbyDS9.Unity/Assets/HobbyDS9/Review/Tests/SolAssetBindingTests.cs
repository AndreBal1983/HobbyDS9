#nullable enable
using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace HobbyDS9.UnityReview.Tests
{
    // Explicit graphics-capable EditMode receiving, no PlayMode bootstrap.
    public sealed class SolAssetBindingTests
    {
        [SetUp] public void RequireDedicatedRun()
        {
            if(!Environment.GetCommandLineArgs().Contains("--hobbyds9-sol-asset-tests"))
                Assert.Ignore("Root must admit a dedicated graphics-capable Sol receiving run.");
        }
        [Test] public void RealImportedMeshesBindAndDisposeWithoutDestroyingBorrowedAssets()
        {
            var names=new[]{"Sun","Mercury","Venus","Earth","Mars","Jupiter","Saturn","Uranus","Neptune","Saturn_Rings"};
            var borrowed=names.Select(name=>Resources.Load<GameObject>("SystemBodyArt/SolMeshes/"+name+"_LOD1")).ToArray();
            foreach(var asset in borrowed)
            {
                Assert.That(asset,Is.Not.Null);var filters=asset.GetComponentsInChildren<MeshFilter>(true);
                Assert.That(filters.Length,Is.EqualTo(1));Assert.That(filters[0].sharedMesh.isReadable,Is.True);
            }
            var shader=Resources.Load<Shader>("SystemBodyArt/SolSurface");Assert.That(shader,Is.Not.Null);Assert.That(shader.isSupported,Is.True);
            var ring=Resources.Load<Shader>("SystemBodyArt/SolMeshes/SolRing");Assert.That(ring,Is.Not.Null);Assert.That(ring.isSupported,Is.True);
            Assert.That(Resources.Load<Texture2D>("SystemBodyArt/SolMeshes/saturn_rings"),Is.Not.Null);
            var type=typeof(ReviewPlayerView).Assembly.GetType("HobbyDS9.UnityReview.ReviewSolStage",true)!;
            var flags=BindingFlags.Instance|BindingFlags.NonPublic;
            IDisposable? stage=null;
            try
            {
                stage=(IDisposable)Activator.CreateInstance(type,flags,null,new object[]{shader},null)!;
                Assert.That(type.GetProperty("ImportedMeshCount",flags)!.GetValue(stage),Is.EqualTo(10),"Fallback must not pass as delivered mesh use");
                var fallbacks=(System.Collections.IEnumerable)type.GetProperty("AssetFallbacks",flags)!.GetValue(stage)!;
                Assert.That(fallbacks.Cast<object>(),Is.Empty);
            }
            finally {stage?.Dispose();stage?.Dispose();}
            foreach(var asset in borrowed)Assert.That(asset.GetComponentInChildren<MeshFilter>().sharedMesh,Is.Not.Null,"Stage must not destroy imported resources");
        }
    }
}
