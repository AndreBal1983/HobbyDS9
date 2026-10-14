using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace HobbyDS9.ArtDemo
{
    // Caller owns this instance and must Dispose when its panel is removed.
    // Manual render only: no Update, animation, listener, simulation or input.
    public sealed class ShipPreviewStage : IDisposable
    {
        private const int PreviewLayer = 31;
        private static readonly HashSet<int> Slots = new HashSet<int>();
        private readonly int slot;
        private readonly GameObject root;
        private readonly Camera camera;
        private GameObject model;
        private Mesh fallbackMesh;
        private Material fallbackMaterial;
        private readonly List<Material> previewMaterials = new List<Material>();
        private static readonly Color Background = new Color(.018f, .028f, .045f, 1);
        private bool disposed;
        public RenderTexture Texture { get; private set; }
        public string Label { get; private set; } = "No visible ship selected";
        public ShipPreviewAvailability Availability { get; private set; }
        public string VisualKey { get; private set; }

        public ShipPreviewStage(int width = 512, int height = 320)
        {
            if (width < 32 || width > 2048 || height < 32 || height > 2048) throw new ArgumentOutOfRangeException(nameof(width), "Preview dimensions must be 32–2048 pixels.");
            while (Slots.Contains(slot)) ++slot;
            Slots.Add(slot);
            root = new GameObject("ArtDemo ship preview stage") { hideFlags = HideFlags.DontSave };
            // Ordinary runtime scenes are NOT Camera.scene isolation. Use a
            // reserved layer, separate bounded volumes and synchronous activation.
            // All stage roots remain inactive outside manual Render().
            root.layer = PreviewLayer;
            root.transform.position = new Vector3(0, -10000, slot * 128);
            camera = Child("Preview camera").AddComponent<Camera>();
            camera.enabled = false;
            camera.cullingMask = 1 << PreviewLayer;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Background;
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
            root.SetActive(false);
            ClearTexture();
        }

        private GameObject Child(string name)
        {
            var child = new GameObject(name) { hideFlags = HideFlags.DontSave };
            child.layer = PreviewLayer;
            child.transform.SetParent(root.transform, false);
            return child;
        }

        public void Show(ShipPreviewSelection selection)
        {
            EnsureAlive();
            Clear();
            if (selection == null || (selection.Disclosure != ShipPreviewDisclosure.Owned && selection.Disclosure != ShipPreviewDisclosure.LawfullyVisible)) return;
            var studio = Resources.Load<Shader>("ArtDemo/PreviewStudio");
            if (studio == null || !studio.isSupported)
            {
                Label = "Ship preview unavailable in this renderer";
                Availability = ShipPreviewAvailability.RenderingUnavailable;
                return;
            }
            var definition = ShipVisualCatalog.Resolve(selection);
            var prefab = definition?.LoadPrefab();
            if (prefab != null)
            {
                model = UnityEngine.Object.Instantiate(prefab, root.transform, false);
                foreach (var part in model.GetComponentsInChildren<Transform>(true)) part.gameObject.layer = PreviewLayer;
                ApplyStudioMaterials();
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
            // Read mesh-local bounds, which remain valid for inactive instances.
            var filters = model.GetComponentsInChildren<MeshFilter>(true);
            var bounds = new Bounds(filters[0].transform.TransformPoint(filters[0].sharedMesh.bounds.center), Vector3.zero);
            foreach (var filter in filters)
            {
                var b = filter.sharedMesh.bounds;
                for (int x = -1; x <= 1; x += 2)
                for (int y = -1; y <= 1; y += 2)
                for (int z = -1; z <= 1; z += 2)
                    bounds.Encapsulate(filter.transform.TransformPoint(b.center + Vector3.Scale(b.extents, new Vector3(x, y, z))));
            }
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
            foreach (var material in previewMaterials) Destroy(material);
            previewMaterials.Clear();
            VisualKey = null;
            Label = "No visible ship selected";
            Availability = ShipPreviewAvailability.Empty;
            ClearTexture();
        }

        public void Render()
        {
            EnsureAlive();
            if (Availability == ShipPreviewAvailability.Empty) { ClearTexture(); return; }
            root.SetActive(true);
            try
            {
                var request = new RenderPipeline.StandardRequest { destination = Texture };
                if (GraphicsSettings.currentRenderPipeline == null) camera.Render();
                else if (RenderPipeline.SupportsRenderRequest(camera, request)) RenderPipeline.SubmitRenderRequest(camera, request);
                else { ClearTexture(); Label = "Ship preview unavailable in this renderer"; Availability = ShipPreviewAvailability.RenderingUnavailable; }
            }
            finally { root.SetActive(false); }
        }

        private void CreateFallback()
        {
            model = Child("Generic unavailable model marker");
            // Deliberately abstract octahedron; no faction, hull or equipment claim.
            fallbackMesh = new Mesh { name = "ArtDemo generic marker", hideFlags = HideFlags.DontSave };
            fallbackMesh.vertices = new[] { new Vector3(0, 2, 0), new Vector3(0, -2, 0), new Vector3(2, 0, 0), new Vector3(0, 0, 2), new Vector3(-2, 0, 0), new Vector3(0, 0, -2) };
            fallbackMesh.triangles = new[] { 0,3,2, 0,4,3, 0,5,4, 0,2,5, 1,2,3, 1,3,4, 1,4,5, 1,5,2 };
            fallbackMesh.RecalculateNormals();
            var shader = Resources.Load<Shader>("ArtDemo/PreviewStudio");
            fallbackMaterial = new Material(shader) { name = "ArtDemo generic marker", hideFlags = HideFlags.DontSave };
            fallbackMaterial.SetColor("_BaseColor", new Color(.52f, .43f, .25f));
            model.AddComponent<MeshFilter>().sharedMesh = fallbackMesh;
            model.AddComponent<MeshRenderer>().sharedMaterial = fallbackMaterial;
        }

        private void ApplyStudioMaterials()
        {
            // Fixed normal-based studio shading ignores unrelated scene lighting,
            // ambient probes, fog and volumes. Never alter shared URP materials.
            var shader = Resources.Load<Shader>("ArtDemo/PreviewStudio");
            var cache = new Dictionary<Material, Material>();
            foreach (var renderer in model.GetComponentsInChildren<Renderer>(true))
            {
                var materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; ++i)
                {
                    var source = materials[i];
                    if (!cache.TryGetValue(source, out var target))
                    {
                        target = new Material(shader) { name = source.name + " preview", hideFlags = HideFlags.DontSave };
                        target.SetColor("_BaseColor", source.GetColor("_BaseColor"));
                        target.SetColor("_EmissionColor", source.GetColor("_EmissionColor"));
                        target.SetFloat("_Metallic", source.GetFloat("_Metallic"));
                        target.SetFloat("_Smoothness", source.GetFloat("_Smoothness"));
                        cache.Add(source, target);
                        previewMaterials.Add(target);
                    }
                    materials[i] = target;
                }
                renderer.sharedMaterials = materials;
            }
        }

        private void ClearTexture()
        {
            var previous = RenderTexture.active;
            try { RenderTexture.active = Texture; GL.Clear(true, true, Background); }
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
            if (camera != null) camera.targetTexture = null;
            if (root != null) root.SetActive(false);
            Destroy(root);
            Texture.Release();
            Destroy(Texture);
            Texture = null;
            Slots.Remove(slot);
        }
    }
}
