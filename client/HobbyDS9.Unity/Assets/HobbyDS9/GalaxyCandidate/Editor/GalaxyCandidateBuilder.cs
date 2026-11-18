using System;
using System.IO;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace HobbyDS9.GalaxyCandidate.Editor
{
    // No InitializeOnLoad, asset postprocessor or automatic scene/catalog hook.
    // Requires a coordinator graphics/import lease before explicit invocation.
    public static class GalaxyCandidateBuilder
    {
        public const string Output = "Assets/HobbyDS9/GalaxyCandidate/Generated";
        [Serializable] private sealed class Entry { public string file = ""; public long bytes = 0; public string sha256 = ""; }
        [Serializable] private sealed class Manifest { public string assetId = ""; public Entry[] files = Array.Empty<Entry>(); }
        [Serializable] private sealed class Receipt { public string status; public int[] triangles; public int[] repairedTangents; }

        public static void Build()
        {
            if (Environment.GetEnvironmentVariable("HOBBYDS9_GALAXY_IMPORT") != "1")
                throw new InvalidOperationException("Explicit candidate import opt-in required after obtaining the coordinator lease.");
            string repo = Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath, "../../.."));
            string source = Path.GetFullPath(Path.Combine(repo, "assets/art/galaxy-class-meshy-v2-runtime"));
            var manifest = JsonUtility.FromJson<Manifest>(File.ReadAllText(Path.Combine(source, "candidate-manifest.json")));
            if (manifest.assetId != "fed.galaxy-meshy-v2.standin.v1" || manifest.files.Length != 12)
                throw new InvalidOperationException("Unexpected candidate manifest.");
            foreach (var entry in manifest.files)
            {
                var path = Path.GetFullPath(Path.Combine(source, entry.file));
                if (!path.StartsWith(source + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Candidate path escapes source.");
                var data = File.ReadAllBytes(path);
                using (var hash = SHA256.Create())
                    if (data.LongLength != entry.bytes || BitConverter.ToString(hash.ComputeHash(data)).Replace("-", "").ToLowerInvariant() != entry.sha256)
                        throw new InvalidOperationException("Candidate hash mismatch: " + entry.file);
            }
            // New output only. Never delete/overwrite a prior inspected candidate.
            if (Directory.Exists(DiskPath(Output)) || File.Exists(DiskPath(Output + ".meta")))
                throw new InvalidOperationException("Candidate output already exists; preserve it and review before any replacement.");
            Directory.CreateDirectory(DiskPath(Output + "/Models"));
            Directory.CreateDirectory(DiskPath(Output + "/Textures"));
            AssetDatabase.Refresh();
            foreach (var file in new[] { "BaseColor.jpg", "Normal.jpg", "Emission.jpg", "MetallicSmoothness.png" })
            {
                string destination = Output + "/Textures/" + file;
                File.Copy(Path.Combine(source, "Textures", file), DiskPath(destination));
                AssetDatabase.ImportAsset(destination, ImportAssetOptions.ForceSynchronousImport);
                var importer = (TextureImporter)AssetImporter.GetAtPath(destination);
                bool normal = file == "Normal.jpg";
                importer.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
                importer.sRGBTexture = file == "BaseColor.jpg" || file == "Emission.jpg";
                importer.flipGreenChannel = false;
                importer.alphaSource = file == "MetallicSmoothness.png" ? TextureImporterAlphaSource.FromInput : TextureImporterAlphaSource.None;
                importer.alphaIsTransparency = false;
                importer.maxTextureSize = file == "BaseColor.jpg" || normal ? 4096 : 2048;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.mipmapEnabled = true;
                importer.wrapMode = TextureWrapMode.Repeat;
                importer.filterMode = FilterMode.Trilinear;
                importer.isReadable = false;
                importer.SaveAndReimport();
            }
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null || !shader.isSupported) throw new InvalidOperationException("URP Lit unavailable.");
            var material = new Material(shader) { name = "Galaxy v2 preserved PBR" };
            material.SetTexture("_BaseMap", LoadTexture("BaseColor.jpg"));
            material.SetTexture("_BumpMap", LoadTexture("Normal.jpg"));
            material.SetTexture("_MetallicGlossMap", LoadTexture("MetallicSmoothness.png"));
            material.SetTexture("_EmissionMap", LoadTexture("Emission.jpg"));
            material.SetColor("_BaseColor", Color.white);
            material.SetColor("_EmissionColor", Color.white);
            material.SetFloat("_BumpScale", 1);
            material.SetFloat("_Metallic", 1);
            material.SetFloat("_Smoothness", 1);
            material.SetFloat("_SmoothnessTextureChannel", 0);
            material.SetFloat("_Cull", (float)CullMode.Off);
            material.EnableKeyword("_NORMALMAP");
            material.EnableKeyword("_METALLICSPECGLOSSMAP");
            material.EnableKeyword("_EMISSION");
            material.doubleSidedGI = true;
            AssetDatabase.CreateAsset(material, Output + "/GalaxyPBR.mat");

            var root = new GameObject("Galaxy Meshy v2 candidate - unbound");
            var receipt = new Receipt { status = "Import data checks only; graphics/appearance acceptance pending", triangles = new[] { 60000, 20000, 7000 }, repairedTangents = new int[3] };
            var lods = new LOD[3];
            try
            {
                for (int level = 0; level < 3; ++level)
                {
                    string name = "GalaxyMeshyV2_LOD" + level;
                    string destination = Output + "/Models/" + name + ".fbx";
                    File.Copy(Path.Combine(source, "Models", name + ".fbx"), DiskPath(destination));
                    AssetDatabase.ImportAsset(destination, ImportAssetOptions.ForceSynchronousImport);
                    var importer = (ModelImporter)AssetImporter.GetAtPath(destination);
                    importer.importAnimation = false;
                    importer.importCameras = false;
                    importer.importLights = false;
                    importer.addCollider = false;
                    importer.materialImportMode = ModelImporterMaterialImportMode.None;
                    importer.globalScale = 1;
                    importer.useFileScale = true;
                    importer.importNormals = ModelImporterNormals.Import;
                    importer.importTangents = ModelImporterTangents.CalculateMikk;
                    importer.meshCompression = ModelImporterMeshCompression.Off;
                    importer.isReadable = true;
                    importer.SaveAndReimport();
                    var instance = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(destination), root.transform, false);
                    var filters = instance.GetComponentsInChildren<MeshFilter>(true);
                    if (filters.Length != 1 || filters[0].sharedMesh.subMeshCount != 1)
                        throw new InvalidOperationException("Candidate must import as one mesh/submesh per LOD.");
                    var mesh = UnityEngine.Object.Instantiate(filters[0].sharedMesh);
                    try
                    {
                        mesh.name = name + " validated frames";
                        if (mesh.triangles.Length / 3 != receipt.triangles[level]) throw new InvalidOperationException("Triangle count changed on import.");
                        receipt.repairedTangents[level] = GalaxyTangentFrames.RepairAndValidate(mesh);
                        AssetDatabase.CreateAsset(mesh, Output + "/Models/" + name + "-validated.asset");
                    }
                    catch { if (!AssetDatabase.Contains(mesh)) UnityEngine.Object.DestroyImmediate(mesh); throw; }
                    filters[0].sharedMesh = mesh;
                    var renderers = instance.GetComponentsInChildren<MeshRenderer>(true);
                    if (renderers.Length != 1) throw new InvalidOperationException("Unexpected candidate renderer hierarchy.");
                    renderers[0].sharedMaterial = material;
                    // Fail visibly rather than silently rotating/rescaling an unexpected import.
                    var size = renderers[0].bounds.size;
                    if (Mathf.Abs(size.z - 620) > 6.2f || Mathf.Abs(size.x - 452) > 5 || Mathf.Abs(size.y - 132) > 3)
                        throw new InvalidOperationException("Receiving candidate axes/metre bounds differ from CPU contract.");
                    lods[level] = new LOD(new[] { .50f, .20f, .02f }[level], renderers);
                }
                var group = root.AddComponent<LODGroup>();
                group.fadeMode = LODFadeMode.None;
                group.SetLODs(lods);
                group.RecalculateBounds();
                PrefabUtility.SaveAsPrefabAsset(root, Output + "/GalaxyCandidate.prefab");
                AssetDatabase.SaveAssets();
                string reports = Path.Combine(repo, "artifacts/galaxy-v2-unity");
                Directory.CreateDirectory(reports);
                File.WriteAllText(Path.Combine(reports, "import-data.json"), JsonUtility.ToJson(receipt, true));
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        private static string DiskPath(string assetPath) => Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath, "..", assetPath));
        private static Texture2D LoadTexture(string name) => AssetDatabase.LoadAssetAtPath<Texture2D>(Output + "/Textures/" + name);
    }
}
