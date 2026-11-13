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
        public void BareModelsOwnIndependentMaterialsAndNoCameraOrRenderTexture()
        {
            var parent=new GameObject("Owned bare model test");ActivityHullModel first=null,second=null;
            try
            {
                int textures=Resources.FindObjectsOfTypeAll<RenderTexture>().Length;
                first=ActivityHullModel.Create(parent.transform,"hull.defiant-combat.v1");second=ActivityHullModel.Create(parent.transform,"hull.defiant-combat.v1");
                Assert.That(first,Is.Not.Null);Assert.That(second,Is.Not.Null);
                Assert.That(first.Root.GetComponentsInChildren<Camera>(true),Is.Empty);Assert.That(Resources.FindObjectsOfTypeAll<RenderTexture>().Length,Is.EqualTo(textures));
                var imported=Resources.Load<GameObject>(ActivityHullCatalog.Find("hull.defiant-combat.v1").ResourcePath).GetComponentInChildren<MeshRenderer>(true).sharedMaterial;
                var importedJson=UnityEditor.EditorJsonUtility.ToJson(imported);
                var a=first.Root.GetComponentInChildren<MeshRenderer>(true).sharedMaterial;var b=second.Root.GetComponentInChildren<MeshRenderer>(true).sharedMaterial;
                Assert.That(a,Is.Not.SameAs(imported));Assert.That(b,Is.Not.SameAs(imported));Assert.That(a,Is.Not.SameAs(b));var original=b.GetColor("_BaseColor");a.SetColor("_BaseColor",Color.magenta);Assert.That(b.GetColor("_BaseColor"),Is.EqualTo(original));
                var root=first.Root;first.Dispose();first.Dispose();
                if(Application.isPlaying)Assert.That(root.activeSelf,Is.False);else{Assert.That(root==null,Is.True);Assert.That(a==null,Is.True);}
                Assert.That(second.Root!=null,Is.True);Assert.That(b!=null,Is.True);Assert.That(UnityEditor.EditorJsonUtility.ToJson(imported),Is.EqualTo(importedJson));
                Assert.That(ActivityHullModel.Create(parent.transform,"Defiant"),Is.Null);Assert.That(ActivityHullModel.Create(parent.transform,null),Is.Null);
            }
            finally{first?.Dispose();second?.Dispose();UnityEngine.Object.DestroyImmediate(parent);}
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
                    if(hull!="hull.constitution-explorer.v1")Assert.That(stage.Label,Does.Contain("original candidate exterior"));
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
        [Test]
        public void FixedGalaxyExteriorDoesNotInstantiateTheDesignerCoreStudy()
        {
            var parent=new GameObject("Owned exterior-only test");ActivityHullModel model=null!;
            try
            {
                model=ActivityHullModel.Create(parent.transform,"hull.galaxy-cruiser.v1");
                Assert.That(model,Is.Not.Null);
                Assert.That(model.Root.GetComponentsInChildren<Transform>(true).Any(t=>t.name=="CoreCutaway"),Is.False);
                Assert.That(model.Definition.IsOriginalCandidate,Is.True);
                Assert.That(model.Root.GetComponentsInChildren<MeshRenderer>(true).Length,Is.EqualTo(1),"The new complete exterior is one merged source mesh.");
                Assert.That(model.Root.GetComponentInChildren<MeshFilter>(true).sharedMesh.triangles.Length/3,Is.EqualTo(9884),"Require the complete pinned exterior, not a partial mesh.");
            }
            finally{model?.Dispose();UnityEngine.Object.DestroyImmediate(parent);}
        }
        [TestCase("hull.galaxy-cruiser.v1",9884,620f)]
        [TestCase("hull.defiant-combat.v1",7560,170f)]
        public void OriginalCandidatesRetainExactMaterialRolesAndMetreBounds(string hull,int triangles,float length)
        {
            var definition=ActivityHullCatalog.Find(hull);
            Assert.That(definition.IsOriginalCandidate,Is.True);Assert.That(definition.Label,Does.Contain("original candidate"));
            var prefab=Resources.Load<GameObject>(definition.ResourcePath);Assert.That(prefab,Is.Not.Null);
            var filter=prefab.GetComponentInChildren<MeshFilter>(true);var mesh=filter.sharedMesh;
            Assert.That(mesh.isReadable,Is.True);Assert.That(mesh.triangles.Length/3,Is.EqualTo(triangles));
            Assert.That(mesh.subMeshCount,Is.EqualTo(8));Assert.That(mesh.uv.Length,Is.EqualTo(mesh.vertexCount));
            Assert.That(mesh.normals.Length,Is.EqualTo(mesh.vertexCount));
            var bounds=new Bounds(filter.transform.TransformPoint(mesh.vertices[0]),Vector3.zero);
            foreach(var point in mesh.vertices)bounds.Encapsulate(filter.transform.TransformPoint(point));
            Assert.That(bounds.size.z,Is.EqualTo(length).Within(.02f));Assert.That(bounds.center.magnitude,Is.LessThan(.02f));
            var roles=hull=="hull.galaxy-cruiser.v1"
                ?new[]{"Hull_Pearl","Hull_Slate","Recess_Graphite","Sensor_Bronze","Engine_Blue","Collector_Red","Trim_Titanium","Windows_Warm"}
                :new[]{"Hull_Pearl","Hull_Slate","Trim_Titanium","Recess_Graphite","Collector_Red","Engine_Blue","Windows_Warm","Sensor_Bronze"};
            var materials=prefab.GetComponentInChildren<MeshRenderer>(true).sharedMaterials;
            Assert.That(materials.Length,Is.EqualTo(roles.Length));
            for(int i=0;i<roles.Length;i++)
            {
                Assert.That(materials[i],Is.SameAs(Resources.Load<Material>("ArtDemo/Production01/Hulls/Materials/"+roles[i])));
                Assert.That(materials[i].shader,Is.SameAs(Resources.Load<Shader>("ArtDemo/PreviewStudio")));
            }
            Assert.That(prefab.GetComponentsInChildren<Collider>(true),Is.Empty);Assert.That(prefab.GetComponentsInChildren<Camera>(true),Is.Empty);
        }
    }
}
