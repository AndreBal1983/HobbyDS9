using System;
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace HobbyDS9.ArtDemo.Tests
{
    public sealed class ShipPreviewStageTests
    {
        private static ShipPreviewSelection Cruiser(ShipPreviewDisclosure disclosure = ShipPreviewDisclosure.Owned) =>
            new ShipPreviewSelection("civilization.federation", "ship-design.source-cruiser", disclosure);

        [TestCase("civilization.federation", "ship-design.source-cruiser", ShipPreviewDisclosure.Owned, ShipVisualCatalog.ExplorationCruiser)]
        [TestCase("civilization.federation", "ship-design.source-cruiser", ShipPreviewDisclosure.LawfullyVisible, ShipVisualCatalog.ExplorationCruiser)]
        [TestCase("civilization.federation", "ship-design.source-cruiser", ShipPreviewDisclosure.None, null)]
        [TestCase("civilization.cardassian", "ship-design.source-cruiser", ShipPreviewDisclosure.LawfullyVisible, null)]
        [TestCase("civilization.cardassian", "ship-design.travel-target", ShipPreviewDisclosure.LawfullyVisible, null)]
        [TestCase("civilization.federation", "ship-design.scout", ShipPreviewDisclosure.Owned, null)]
        [TestCase("civ.federation", "ship-design.source-cruiser", ShipPreviewDisclosure.Owned, null)]
        [TestCase("civilization.federation", "ship-design.source-cruiser-extra", ShipPreviewDisclosure.Owned, null)]
        [TestCase(null, null, ShipPreviewDisclosure.Owned, null)]
        public void MappingIsExactAndDisclosureBound(string civilization, string design, ShipPreviewDisclosure disclosure, string expected)
        {
            Assert.That(ShipVisualCatalog.Resolve(new ShipPreviewSelection(civilization, design, disclosure))?.VisualKey, Is.EqualTo(expected));
        }

        [TestCase(ShipVisualCatalog.ExplorationCruiser, 620f, 7700, 3542, 2004)]
        [TestCase(ShipVisualCatalog.PatrolFrigate, 280f, 6612, 3072, 1564)]
        [TestCase(ShipVisualCatalog.CompactEscort, 170f, 3892, 1852, 1260)]
        public void BuildIncludedPrefabsHaveVerifiedGeometryAxesMaterialsAndLods(string key, float length, int first, int second, int third)
        {
            var definition = ShipVisualCatalog.Find(key);
            var prefab = definition.LoadPrefab();
            Assert.That(prefab, Is.Not.Null, definition.ResourcePath);
            var instance = UnityEngine.Object.Instantiate(prefab);
            try
            {
                Assert.That(instance.transform.localScale, Is.EqualTo(Vector3.one));
                Assert.That(instance.GetComponentsInChildren<Collider>(true), Is.Empty);
                Assert.That(instance.GetComponentsInChildren<AudioListener>(true), Is.Empty);
                var lods = instance.GetComponent<LODGroup>().GetLODs();
                Assert.That(lods, Has.Length.EqualTo(3));
                int[] expected = { first, second, third };
                for (int l = 0; l < 3; ++l)
                {
                    Assert.That(lods[l].renderers, Has.Length.EqualTo(4));
                    var triangles = 0;
                    var bounds = lods[l].renderers[0].bounds;
                    foreach (var renderer in lods[l].renderers)
                    {
                        bounds.Encapsulate(renderer.bounds);
                        var mesh = renderer.GetComponent<MeshFilter>().sharedMesh;
                        triangles += mesh.triangles.Length / 3;
                        Assert.That(mesh.normals, Has.Length.EqualTo(mesh.vertexCount));
                        Assert.That(mesh.uv, Has.Length.EqualTo(mesh.vertexCount));
                        foreach (var material in renderer.sharedMaterials)
                        {
                            Assert.That(material, Is.Not.Null);
                            Assert.That(material.shader.name, Is.EqualTo("Universal Render Pipeline/Lit"));
                            Assert.That(material.shader.isSupported, Is.True);
                            if (material.GetColor("_EmissionColor").maxColorComponent > 0)
                                Assert.That(material.IsKeywordEnabled("_EMISSION"), Is.True, material.name + " world emission must survive URP validation.");
                        }
                    }
                    Assert.That(triangles, Is.EqualTo(expected[l]));
                    Assert.That(bounds.size.z, Is.EqualTo(length).Within(length * .015f));
                }
                var bows = instance.GetComponentsInChildren<Transform>(true).Where(t => t.name == "Axis_Bow").ToArray();
                var ups = instance.GetComponentsInChildren<Transform>(true).Where(t => t.name == "Axis_Up").ToArray();
                Assert.That(bows, Has.Length.EqualTo(3));
                Assert.That(ups, Has.Length.EqualTo(3));
                foreach (var t in bows)
                {
                    var p = instance.transform.InverseTransformPoint(t.position);
                    Assert.That(p.z, Is.GreaterThan(0)); Assert.That(Mathf.Abs(p.x) + Mathf.Abs(p.y), Is.LessThan(.01f));
                }
                foreach (var t in ups)
                {
                    var p = instance.transform.InverseTransformPoint(t.position);
                    Assert.That(p.y, Is.GreaterThan(0)); Assert.That(Mathf.Abs(p.x) + Mathf.Abs(p.z), Is.LessThan(.01f));
                }
            }
            finally { UnityEngine.Object.DestroyImmediate(instance); }
        }

        [Test]
        public void CruiserAndGenericFallbackRenderRealPixelsWithoutMutatingExternalState()
        {
            var active = SceneManager.GetActiveScene();
            var ambient = RenderSettings.ambientLight;
            var sun = RenderSettings.sun;
            var activeTexture = RenderTexture.active;
            var listeners = UnityEngine.Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Length;
            using (var stage = new ShipPreviewStage())
            {
                stage.Show(Cruiser());
                Assert.That(stage.Availability, Is.EqualTo(ShipPreviewAvailability.Model));
                Assert.That(stage.VisualKey, Is.EqualTo(ShipVisualCatalog.ExplorationCruiser));
                AssertRendered(stage.Texture, "cruiser");
                var baseline = Read(stage.Texture);
                var unrelatedLight = new GameObject("Unrelated main-scene directional light");
                var unrelatedObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
                try
                {
                    var light = unrelatedLight.AddComponent<Light>();
                    light.type = LightType.Directional;
                    light.color = Color.magenta;
                    light.intensity = 100;
                    light.cullingMask = -1;
                    unrelatedObject.transform.position = new Vector3(0, -10000, 0);
                    unrelatedObject.transform.localScale = Vector3.one * 20;
                    stage.Render();
                    var contaminated = Read(stage.Texture);
                    try { Assert.That(contaminated.GetPixels32(), Is.EqualTo(baseline.GetPixels32()), "External all-layer light/default-layer object must not contaminate the preview."); }
                    finally { UnityEngine.Object.DestroyImmediate(contaminated); }
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(baseline);
                    UnityEngine.Object.DestroyImmediate(unrelatedLight);
                    UnityEngine.Object.DestroyImmediate(unrelatedObject);
                }
                stage.Show(new ShipPreviewSelection("civilization.cardassian", "ship-design.travel-target", ShipPreviewDisclosure.LawfullyVisible));
                Assert.That(stage.Availability, Is.EqualTo(ShipPreviewAvailability.GenericFallback));
                Assert.That(stage.VisualKey, Is.Null);
                Assert.That(stage.Label, Does.Contain("generic marker"));
                AssertRendered(stage.Texture, "fallback");
                stage.Show(Cruiser(ShipPreviewDisclosure.None));
                Assert.That(stage.Availability, Is.EqualTo(ShipPreviewAvailability.Empty));
                Assert.That(stage.Label, Is.EqualTo("No visible ship selected"));
                var image = Read(stage.Texture);
                try { Assert.That(image.GetPixels32().Distinct().Count(), Is.EqualTo(1), "Clear must remove the previous visible ship."); }
                finally { UnityEngine.Object.DestroyImmediate(image); }
            }
            Assert.That(SceneManager.GetActiveScene(), Is.EqualTo(active));
            Assert.That(RenderSettings.ambientLight, Is.EqualTo(ambient));
            Assert.That(RenderSettings.sun, Is.EqualTo(sun));
            Assert.That(RenderTexture.active, Is.EqualTo(activeTexture));
            Assert.That(UnityEngine.Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Length, Is.EqualTo(listeners));
        }

        [UnityTest]
        public IEnumerator ReplacementAndMultipleInstancesReleaseTheirOwnNativeResources()
        {
            yield return null; // Drain prior asynchronously unloaded preview scenes.
            int scenes = SceneManager.sceneCount;
            int cameras = UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length;
            int lights = UnityEngine.Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length;
            int materials = PreviewMaterialCount();
            int meshes = Resources.FindObjectsOfTypeAll<Mesh>().Count(m => m.name == "ArtDemo generic marker");
            int textures = Resources.FindObjectsOfTypeAll<RenderTexture>().Count(t => t.name == "ArtDemo ship preview");
            var first = new ShipPreviewStage(256, 160);
            var second = new ShipPreviewStage(320, 200);
            var texture = first.Texture;
            try
            {
                Assert.That(first.Texture, Is.Not.SameAs(second.Texture));
                for (int i = 0; i < 4; ++i)
                {
                    first.Show(Cruiser());
                    first.Show(new ShipPreviewSelection("civilization.cardassian", "ship-design.travel-target", ShipPreviewDisclosure.LawfullyVisible));
                }
                second.Show(Cruiser());
                var cruiserBefore = Read(second.Texture);
                var genericBefore = Read(first.Texture);
                try
                {
                    // Different simultaneous content, not merely object counts.
                    Assert.That(cruiserBefore.GetPixels32().Distinct().Count(), Is.GreaterThan(2));
                    Assert.That(genericBefore.GetPixels32().Distinct().Count(), Is.GreaterThan(2));
                    first.Render();
                    second.Render();
                    var cruiserAfter = Read(second.Texture);
                    var genericAfter = Read(first.Texture);
                    try
                    {
                        Assert.That(cruiserAfter.GetPixels32(), Is.EqualTo(cruiserBefore.GetPixels32()), "Other stage must not contaminate cruiser render.");
                        Assert.That(genericAfter.GetPixels32(), Is.EqualTo(genericBefore.GetPixels32()), "Other stage must not contaminate fallback render.");
                    }
                    finally { UnityEngine.Object.DestroyImmediate(cruiserAfter); UnityEngine.Object.DestroyImmediate(genericAfter); }
                }
                finally { UnityEngine.Object.DestroyImmediate(cruiserBefore); UnityEngine.Object.DestroyImmediate(genericBefore); }
                first.Dispose(); first.Dispose();
                Assert.That(texture == null, Is.True);
                Assert.That(first.Texture, Is.Null);
                Assert.Throws<ObjectDisposedException>(() => first.Render());
                second.Render();
                Assert.That(second.Availability, Is.EqualTo(ShipPreviewAvailability.Model));
            }
            finally { first.Dispose(); second.Dispose(); }
            yield return null;
            Assert.That(SceneManager.sceneCount, Is.EqualTo(scenes));
            Assert.That(UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length, Is.EqualTo(cameras));
            Assert.That(UnityEngine.Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length, Is.EqualTo(lights));
            Assert.That(PreviewMaterialCount(), Is.EqualTo(materials));
            Assert.That(Resources.FindObjectsOfTypeAll<Mesh>().Count(m => m.name == "ArtDemo generic marker"), Is.EqualTo(meshes));
            Assert.That(Resources.FindObjectsOfTypeAll<RenderTexture>().Count(t => t.name == "ArtDemo ship preview"), Is.EqualTo(textures));
        }

        [Test]
        public void TextureAllocationIsBoundedAndUnknownKeysDoNotLoadAnAsset()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new ShipPreviewStage(0, 320));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ShipPreviewStage(4096, 320));
            Assert.That(ShipVisualCatalog.Find("alien.cruiser"), Is.Null);
            Assert.That(ShipVisualCatalog.Resolve(null), Is.Null);
        }

        private static Texture2D Read(RenderTexture texture)
        {
            var before = RenderTexture.active;
            var image = new Texture2D(texture.width, texture.height, TextureFormat.RGBA32, false);
            try { RenderTexture.active = texture; image.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0); image.Apply(); }
            finally { RenderTexture.active = before; }
            return image;
        }

        private static int PreviewMaterialCount() => Resources.FindObjectsOfTypeAll<Material>()
            .Count(m => m.name == "ArtDemo generic marker" || m.name.EndsWith(" preview", StringComparison.Ordinal));

        private static void AssertRendered(RenderTexture texture, string name)
        {
            var image = Read(texture);
            try
            {
                var pixels = image.GetPixels32();
                var background = pixels[0];
                int foreground = pixels.Count(p => Math.Abs(p.r - background.r) + Math.Abs(p.g - background.g) + Math.Abs(p.b - background.b) > 35);
                Assert.That(foreground, Is.GreaterThan(pixels.Length * .015), "A nonblank rendered model is required.");
                Assert.That(foreground, Is.LessThan(pixels.Length * .8), "Model must fit inside the preview.");
                // Detect the error shader rather than accepting any coloured blob.
                Assert.That(pixels.Count(p => p.r > 180 && p.b > 180 && p.g < 70), Is.LessThan(pixels.Length * .005));
                var path = Environment.GetEnvironmentVariable("HOBBYDS9_ART_EVIDENCE") ?? Path.GetFullPath(Path.Combine(Application.dataPath, "../../../artifacts/art-demo-runtime"));
                Directory.CreateDirectory(path);
                File.WriteAllBytes(Path.Combine(path, name + ".png"), image.EncodeToPNG());
            }
            finally { UnityEngine.Object.DestroyImmediate(image); }
        }
    }
}
