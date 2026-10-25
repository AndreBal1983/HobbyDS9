using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace HobbyDS9.ArtDemo.Tests
{
    public sealed class ActivityHullPresentationTests
    {
        [TestCase("hull.constitution-explorer.v1","activity.constitution-explorer.v1")]
        [TestCase("hull.galaxy-cruiser.v1","activity.galaxy-cruiser.v1")]
        [TestCase("hull.defiant-combat.v1","activity.defiant-combat.v1")]
        [TestCase("hull.utility.v1",null)]
        [TestCase("Hull.defiant-combat.v1",null)]
        [TestCase("hull.defiant-combat.v1-extra",null)]
        [TestCase("Defiant",null)]
        [TestCase(null,null)]
        public void OnlyExactDisclosedHullIdentitiesHaveActivityBindings(string hull,string expected)
        {
            Assert.That(ActivityHullCatalog.Find(hull)?.VisualKey,Is.EqualTo(expected));
            Assert.That(ShipPreviewStage.DesignerReferenceKey(hull),Is.EqualTo(hull=="hull.utility.v1"?ShipPreviewStage.DesignerGalaxyKey:null));
        }

        [TestCase("Constitution/Models/ConstitutionExplorer_LOD0",6592,8)]
        [TestCase("Constitution/Models/ConstitutionExplorer_LOD1",2500,8)]
        [TestCase("Defiant/Models/Defiant_LOD0",29998,1)]
        [TestCase("Defiant/Models/Defiant_LOD1",7998,1)]
        public void SelectedRuntimeMeshesRetainDistinctGeometryUvNormalsAndSlots(string resource,int triangles,int slots)
        {
            var prefab=Resources.Load<GameObject>("ArtDemo/ActivityHulls/"+resource);
            Assert.That(prefab,Is.Not.Null,resource);
            var filters=prefab.GetComponentsInChildren<MeshFilter>(true);
            Assert.That(filters,Has.Length.EqualTo(1));
            var mesh=filters[0].sharedMesh;
            Assert.That(mesh.triangles.Length/3,Is.EqualTo(triangles));
            Assert.That(mesh.subMeshCount,Is.EqualTo(slots));
            Assert.That(mesh.normals,Has.Length.EqualTo(mesh.vertexCount));
            Assert.That(mesh.uv,Has.Length.EqualTo(mesh.vertexCount));
            Assert.That(mesh.tangents,Has.Length.EqualTo(mesh.vertexCount));
            Assert.That(prefab.GetComponentsInChildren<Collider>(true),Is.Empty);
            Assert.That(prefab.GetComponentsInChildren<Camera>(true),Is.Empty);
        }

        [Test]
        public void FixedActivityHullSwitchingPreservesTextureAndDoesNotBecomeGalleryEquipment()
        {
            using(var stage=new ShipPreviewStage(768,480))
            {
                var texture=stage.Texture;
                foreach(var hull in new[]{"hull.constitution-explorer.v1","hull.galaxy-cruiser.v1","hull.defiant-combat.v1"})
                {
                    stage.ShowActivityHull(hull);
                    Assert.That(stage.IsActivityHull,Is.True);
                    Assert.That(stage.Availability,Is.EqualTo(ShipPreviewAvailability.Model),stage.Label);
                    Assert.That(stage.VisualKey,Is.EqualTo(ActivityHullCatalog.Find(hull).VisualKey));
                    Assert.That(stage.Label,Does.Contain("fixed hull presentation"));
                    var before=stage.CaptureArtDiagnostics();
                    Assert.That(before.AssembledBoundsSize.Max(),Is.InRange(5f,20f));
                    Assert.That(before.VisibleMinDepth,Is.GreaterThan(before.CameraNear));
                    Assert.That(before.VisibleMaxDepth,Is.LessThan(before.CameraFar));
                    stage.SetStudyVariant(DesignerStudyVariant.SovereignNacelles);
                    stage.SetReferenceView(DesignerReferenceView.CoreCutaway);
                    Assert.That(stage.CaptureArtDiagnostics(),Is.SameAs(before));
                    stage.OrbitReference(75,15);stage.ZoomReference(1.1f);
                    Assert.That(stage.CaptureArtDiagnostics(),Is.Not.SameAs(before));
                    Assert.That(stage.Texture,Is.SameAs(texture));
                    var orbit=stage.CaptureArtDiagnostics();stage.ShowActivityHull(hull);
                    Assert.That(stage.CaptureArtDiagnostics(),Is.SameAs(orbit),"same identity must not rebuild");
                }
                stage.ShowActivityHull("hull.utility.v1");
                Assert.That(stage.IsActivityHull,Is.False);
                Assert.That(stage.Availability,Is.EqualTo(ShipPreviewAvailability.Empty));
                Assert.That(stage.VisualKey,Is.Null);
                stage.ShowDesignerReference("hull.utility.v1",null);
                Assert.That(stage.IsActivityHull,Is.False);
                Assert.That(stage.VisualKey,Is.EqualTo(ShipPreviewStage.DesignerGalaxyKey));
            }
        }
    }
}
