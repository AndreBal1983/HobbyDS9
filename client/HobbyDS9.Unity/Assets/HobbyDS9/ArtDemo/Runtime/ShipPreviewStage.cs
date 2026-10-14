using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace HobbyDS9.ArtDemo
{
    // Caller owns this instance and must Dispose when its panel is removed.
    // Manual render only: no Update, animation, listener, simulation or input.
    public sealed class ShipPreviewStage : IDisposable
    {
        private readonly Scene scene;
        private readonly GameObject root;
        private readonly Camera camera;
        private GameObject model;
        private Mesh fallbackMesh;
        private Material fallbackMaterial;
        private bool disposed;
        public RenderTexture Texture { get; private set; }
        public string Label { get; private set; } = "No visible ship selected";
        public ShipPreviewAvailability Availability { get; private set; }
        public string VisualKey { get; private set; }

        public ShipPreviewStage(int width = 512, int height = 320)
        {
            if (width < 32 || width > 2048 || height < 32 || height > 2048) throw new ArgumentOutOfRangeException(nameof(width), "Preview dimensions must be 32–2048 pixels.");
            scene = SceneManager.CreateScene("ArtDemoShipPreview-" + Guid.NewGuid().ToString("N"));
            root = new GameObject("ArtDemo ship preview stage") { hideFlags = HideFlags.DontSave };
            SceneManager.MoveGameObjectToScene(root, scene);
            // Outside ordinary world-camera bounds; private camera also filters
            // by scene. Never change active scene, global lights or RenderSettings.
            root.transform.position = new Vector3(0, -10000, 0);
            camera = Child("Preview camera").AddComponent<Camera>();
            camera.enabled = false;
            camera.scene = scene;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.018f, .028f, .045f, 1);
            camera.orthographic = true;
            camera.orthographicSize = 4.5f;
            camera.nearClipPlane = .1f;
            camera.farClipPlane = 80;
            camera.allowHDR = false;
            camera.allowMSAA = false;
            var additional = camera.GetUniversalAdditionalCameraData();
            additional.renderPostProcessing = false;
            additional.renderShadows = false;
            additional.allowXRRendering = false;
            Texture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32)
            { name = "ArtDemo ship preview", hideFlags = HideFlags.DontSave, antiAliasing = 1 };
            Texture.Create();
            camera.targetTexture = Texture;
            // Point lights stay spatially bounded, unlike a global directional.
            LightAt("Key", new Vector3(6, 8, 7), new Color(.76f, .85f, 1), 7);
            LightAt("Fill", new Vector3(-7, 3, 1), new Color(.43f, .62f, 1), 4);
            LightAt("Rim", new Vector3(1, 4, -8), new Color(1, .76f, .47f), 6);
            ClearTexture();
        }

        private GameObject Child(string name)
        {
            var child = new GameObject(name) { hideFlags = HideFlags.DontSave };
            child.transform.SetParent(root.transform, false);
            return child;
        }

        private void LightAt(string name, Vector3 position, Color colour, float intensity)
        {
            var light = Child(name).AddComponent<Light>();
            light.transform.localPosition = position;
            light.type = LightType.Point;
            light.color = colour;
            light.intensity = intensity;
            light.range = 25;
            light.shadows = LightShadows.None;
        }

        public void Show(ShipPreviewSelection selection)
        {
            EnsureAlive();
            Clear();
            if (selection == null || (selection.Disclosure != ShipPreviewDisclosure.Owned && selection.Disclosure != ShipPreviewDisclosure.LawfullyVisible)) return;
            var definition = ShipVisualCatalog.Resolve(selection);
            var prefab = definition?.LoadPrefab();
            if (prefab != null)
            {
                model = UnityEngine.Object.Instantiate(prefab, root.transform, false);
                model.transform.localScale = Vector3.one * (10 / definition.NominalArtLengthMetres);
                VisualKey = definition.VisualKey;
                Label = definition.Label;
                Availability = ShipPreviewAvailability.Model;
            }
            else
            {
                CreateFallback();
                Label = "Ship model unavailable · generic marker";
                Availability = ShipPreviewAvailability.GenericFallback;
            }
            var renderers = model.GetComponentsInChildren<Renderer>(true);
            var bounds = renderers[0].bounds;
            foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
            var centre = bounds.center;
            camera.transform.position = centre + new Vector3(10, 8, 13);
            camera.transform.LookAt(centre, Vector3.up);
            // Bounding sphere fits both axes even for narrow textures.
            camera.aspect = (float)Texture.width / Texture.height;
            camera.orthographicSize = Mathf.Max(.5f, bounds.extents.magnitude * 1.15f / Mathf.Min(1, camera.aspect));
            Render();
        }

        public void Clear()
        {
            EnsureAlive();
            // Deferred destruction in play mode must not leave old visible mesh
            // during a same-frame replacement render.
            if (model != null) { model.SetActive(false); Destroy(model); model = null; }
            Destroy(fallbackMesh); fallbackMesh = null;
            Destroy(fallbackMaterial); fallbackMaterial = null;
            VisualKey = null;
            Label = "No visible ship selected";
            Availability = ShipPreviewAvailability.Empty;
            ClearTexture();
        }

        public void Render()
        {
            EnsureAlive();
            if (Availability == ShipPreviewAvailability.Empty) { ClearTexture(); return; }
            var request = new RenderPipeline.StandardRequest { destination = Texture };
            if (GraphicsSettings.currentRenderPipeline == null) camera.Render();
            else if (RenderPipeline.SupportsRenderRequest(camera, request)) RenderPipeline.SubmitRenderRequest(camera, request);
            else { ClearTexture(); Label = "Ship preview unavailable in this renderer"; Availability = ShipPreviewAvailability.RenderingUnavailable; }
        }

        private void CreateFallback()
        {
            model = Child("Generic unavailable model marker");
            // Deliberately abstract octahedron; no faction, hull or equipment claim.
            fallbackMesh = new Mesh { name = "ArtDemo generic marker", hideFlags = HideFlags.DontSave };
            fallbackMesh.vertices = new[] { new Vector3(0, 2, 0), new Vector3(0, -2, 0), new Vector3(2, 0, 0), new Vector3(0, 0, 2), new Vector3(-2, 0, 0), new Vector3(0, 0, -2) };
            fallbackMesh.triangles = new[] { 0,3,2, 0,4,3, 0,5,4, 0,2,5, 1,2,3, 1,3,4, 1,4,5, 1,5,2 };
            fallbackMesh.RecalculateNormals();
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            fallbackMaterial = new Material(shader) { name = "ArtDemo generic marker", hideFlags = HideFlags.DontSave };
            fallbackMaterial.color = new Color(.52f, .43f, .25f);
            model.AddComponent<MeshFilter>().sharedMesh = fallbackMesh;
            model.AddComponent<MeshRenderer>().sharedMaterial = fallbackMaterial;
        }

        private void ClearTexture()
        {
            var previous = RenderTexture.active;
            try { RenderTexture.active = Texture; GL.Clear(true, true, camera.backgroundColor); }
            finally { RenderTexture.active = previous; }
        }

        private void EnsureAlive() { if (disposed) throw new ObjectDisposedException(nameof(ShipPreviewStage)); }
        private static void Destroy(UnityEngine.Object value)
        {
            if (value == null) return;
            if (Application.isPlaying) UnityEngine.Object.Destroy(value); else UnityEngine.Object.DestroyImmediate(value);
        }

        public void Dispose()
        {
            if (disposed) return;
            Clear();
            disposed = true;
            camera.targetTexture = null;
            root.SetActive(false);
            Destroy(root);
            Texture.Release();
            Destroy(Texture);
            Texture = null;
            if (scene.IsValid() && scene.isLoaded) SceneManager.UnloadSceneAsync(scene);
        }
    }
}
