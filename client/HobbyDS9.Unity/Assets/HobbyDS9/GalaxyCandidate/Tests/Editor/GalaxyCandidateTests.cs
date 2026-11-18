using System;
using System.Linq;
using HobbyDS9.ArtDemo;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace HobbyDS9.GalaxyCandidate.Tests
{
    public sealed class GalaxyCandidateTests
    {
        private static Mesh Triangle(bool mirrored = false)
        {
            var mesh = new Mesh();
            mesh.vertices = new[] { Vector3.zero, Vector3.right, Vector3.up };
            mesh.normals = new[] { Vector3.forward, Vector3.forward, Vector3.forward };
            mesh.uv = new[] { Vector2.zero, mirrored ? Vector2.left : Vector2.right, Vector2.up };
            mesh.triangles = new[] { 0, 1, 2 };
            mesh.tangents = new Vector4[3];
            return mesh;
        }

        [TestCase(false, 1f, 1f)]
        [TestCase(true, -1f, -1f)]
        public void UvFallbackPreservesGeometryAndMirroredHandedness(bool mirrored, float direction, float handedness)
        {
            var mesh = Triangle(mirrored);
            try
            {
                var positions = mesh.vertices; var normals = mesh.normals; var uv = mesh.uv; var indices = mesh.triangles;
                Assert.That(GalaxyTangentFrames.RepairAndValidate(mesh), Is.EqualTo(3));
                Assert.That(mesh.tangents.All(t => Mathf.Abs(t.x - direction) < .001f && Mathf.Abs(t.w - handedness) < .001f), Is.True);
                Assert.That(mesh.vertices, Is.EqualTo(positions));
                Assert.That(mesh.normals, Is.EqualTo(normals));
                Assert.That(mesh.uv, Is.EqualTo(uv));
                Assert.That(mesh.triangles, Is.EqualTo(indices));
                Assert.That(GalaxyTangentFrames.RepairAndValidate(mesh, 0), Is.Zero, "Valid imported frames remain unchanged.");
            }
            finally { UnityEngine.Object.DestroyImmediate(mesh); }
        }

        [Test]
        public void InvalidBasisAndBudgetFailuresLeaveMeshUntouched()
        {
            var mesh = Triangle();
            try
            {
                var before = mesh.tangents;
                Assert.Throws<InvalidOperationException>(() => GalaxyTangentFrames.RepairAndValidate(mesh, 2));
                Assert.That(mesh.tangents, Is.EqualTo(before));
                mesh.uv = new Vector2[3];
                Assert.Throws<InvalidOperationException>(() => GalaxyTangentFrames.RepairAndValidate(mesh));
                Assert.That(mesh.tangents, Is.EqualTo(before));
                mesh.normals = new[] { new Vector3(float.NaN, 0, 1), Vector3.forward, Vector3.forward };
                Assert.Throws<InvalidOperationException>(() => GalaxyTangentFrames.RepairAndValidate(mesh));
            }
            finally { UnityEngine.Object.DestroyImmediate(mesh); }
        }

        [Test]
        public void CandidateRemainsUnboundAndOriginalCatalogIntact()
        {
            Assert.That(ShipVisualCatalog.Find("fed.galaxy-meshy-v2.standin.v1"), Is.Null);
            Assert.That(ShipVisualCatalog.Resolve(new ShipPreviewSelection("civilization.federation", "ship-design.source-cruiser", ShipPreviewDisclosure.Owned)).VisualKey,
                Is.EqualTo(ShipVisualCatalog.ExplorationCruiser));
            Assert.That(ShipVisualCatalog.Resolve(new ShipPreviewSelection("civilization.federation", "ship-design.source-cruiser", ShipPreviewDisclosure.None)), Is.Null);
        }

        [Test]
        public void AdapterBorrowsAllMapsAndUvTransformsWithoutChangingSource()
        {
            var shader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/HobbyDS9/GalaxyCandidate/Shaders/TexturedStudio.shader");
            var source = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            var texture = new Texture2D(2, 2);
            Material preview = null;
            try
            {
                foreach (var property in new[] { "_BaseMap", "_BumpMap", "_MetallicGlossMap", "_EmissionMap" })
                {
                    source.SetTexture(property, texture);
                    source.SetTextureScale(property, new Vector2(2, 3));
                    source.SetTextureOffset(property, new Vector2(.2f, .3f));
                }
                source.SetColor("_BaseColor", Color.cyan);
                source.SetColor("_EmissionColor", Color.blue);
                source.SetFloat("_BumpScale", .75f);
                source.SetFloat("_Smoothness", .8f);
                preview = GalaxyPreviewMaterial.Create(source, shader);
                foreach (var property in new[] { "_BaseMap", "_BumpMap", "_MetallicGlossMap", "_EmissionMap" })
                {
                    Assert.That(preview.GetTexture(property), Is.SameAs(texture));
                    Assert.That(preview.GetTextureScale(property), Is.EqualTo(new Vector2(2, 3)));
                    Assert.That(preview.GetTextureOffset(property), Is.EqualTo(new Vector2(.2f, .3f)));
                    Assert.That(source.GetTexture(property), Is.SameAs(texture));
                }
                Assert.That(preview.GetColor("_BaseColor"), Is.EqualTo(Color.cyan));
                Assert.That(preview.GetColor("_EmissionColor"), Is.EqualTo(Color.blue));
                Assert.That(preview.GetFloat("_BumpScale"), Is.EqualTo(.75f));
                Assert.That(preview.GetFloat("_Smoothness"), Is.EqualTo(.8f));
                Assert.That(source.shader.name, Is.EqualTo("Universal Render Pipeline/Lit"));
                source.SetTexture("_BumpMap", null);
                Assert.Throws<ArgumentException>(() => GalaxyPreviewMaterial.Create(source, shader));
            }
            finally
            {
                if (preview != null) UnityEngine.Object.DestroyImmediate(preview);
                UnityEngine.Object.DestroyImmediate(source);
                Assert.That(texture != null, Is.True, "Preview disposal does not own borrowed textures.");
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }
    }
}
