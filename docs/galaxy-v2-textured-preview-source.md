# Galaxy v2 textured preview source checkpoint

19 October 2026. **Source prepared and C# API compilation passed; no Unity import, shader compilation, NUnit execution or rendering performed.** Secondary owns the current exclusive Unity/GPU/build lease. This checkpoint does not activate the candidate or replace the existing Federation pack.

## Owned seam

New isolated package: `client/HobbyDS9.Unity/Assets/HobbyDS9/GalaxyCandidate`. Runtime, Editor and Editor-test assemblies have explicit references and `autoReferenced:false`. All new files/folders have distinct Unity metadata. No existing ArtDemo source, shader, manifest, catalog, prefab, call site or binding changes. Candidate shader is outside Resources with no current runtime consumer.

- `GalaxyPreviewMaterial.Create(source, shader)` makes a caller-owned preview material, borrowing four textures and copying independent UV transforms and colour/normal/smoothness factors. It requires complete textures and the specific supported shader. Source materials/textures remain untouched.
- `TexturedStudio.shader` samples base, normal, metallic R/smoothness A and emission. Importer colour spaces handle sRGB decoding. Unity normal unpacking, tangent handedness and negative scale are respected. Back-face normals flip for double-sided shading. Fixed studio lighting ignores scene lights/probes/fog/volumes/shadows. This is a textured studio approximation, not world URP PBR lighting equivalence.
- `GalaxyTangentFrames.RepairAndValidate` operates on a cloned readable mesh, retaining valid MikkTSpace frames. At most16 invalid frames use the first valid incident UV triangle. Nonfinite geometry, invalid normals, missing streams, degenerate UV bases or broad damage reject. Failure leaves the mesh untouched; success changes only tangents. No arbitrary-axis fallback disguises missing UV data.
- `GalaxyCandidateBuilder.Build` is explicit Editor-only preparation without automatic callbacks. `HOBBYDS9_GALAXY_IMPORT=1` prevents accidental invocation; it does **not** replace the coordinator lease. The builder verifies12pinned payloads, sets explicit import options, creates a URP material, clones meshes for tangent validation, checks counts/metre bounds and creates a three-LOD prefab under `GalaxyCandidate/Generated`, outside Resources. Existing output rejects rather than overwrites. Preserve partial output/logs after failure; no ready-prefab claim follows failure. No current code loads the prefab.

Initial texture acceptance uses uncompressed source-resolution maps with mipmaps/trilinear sampling: base/emission sRGB, normal/MR linear, normal green unflipped and preserved smoothness alpha. MR red is not inferred occlusion. Texture memory/performance approval remains pending. LOD thresholds .50/.20/.02 are provisional presentation values, not simulation rules.

## CPU evidence

`tools/art/compile_galaxy_candidate.py` compiles the three actual C# source sets against installed Unity6000.6.5f1 modular references, .NET Standard2.1/C#9, warnings as errors. **All three pass.** Initial compilation mixed monolithic/modular Editor references; the final reference list corrects that and excludes UnityEditor from runtime. It does not execute Unity native APIs/tests or establish asmdef/import/shader/AOT acceptance.

Local receipt: `artifacts/galaxy-v2-source-compile/evidence.json`, with source/reference/assembly hashes and logs under Runtime, Editor and Tests-Editor. Tests reference the preserved original compiled ArtDemo assembly. Authored NUnit cases cover UV/mirrored handedness, bounded transactional failures, unchanged catalog/disclosure, map/UV/factor copying and borrowed-resource ownership. **Tests are authored/compiled, not executed.** Independent read-only review found no actionable helper/shader/validator/builder defects.

```text
python tools/art/compile_galaxy_candidate.py --managed "<Unity6000.6.5f1>/Editor/Data/Managed" --nunit "<existing package>/nunit.framework.dll" --art-demo "<preserved compiled ArtDemo>/HobbyDS9.ArtDemo.dll"
```

## Receiving gates

1. Obtain coordinator import/GPU lease; preserve current Blender scene/demo. Explicitly invoke builder only in isolated art checkout. Inspect metadata, dimensions, +Z bow/+Y up, UVs, normal handedness and actual repaired counts. Bounds alone do not prove bow direction.
2. Execute candidate NUnit and existing ArtDemo graphics tests. Inspect all textured LODs and world URP material for UV distortion, normal seams/back faces, emission/channels and error shaders. Record real captures/exact source pins. Compiler success is not this evidence.
3. Agree receiving stage adapter contract before changing existing stage. Preserve disclosure-before-instantiation, reserved layer31, separate stage volumes, inactive roots outside synchronous Render/finally and own-resource cleanup. Retain original colour-only path for old hulls. Add real texture/normal/smoothness pixel probes, external-light/object invariance, multi-instance/replacement/clear/disposal tests.
4. Keep candidate unbound until appearance validation and explicit catalog decision. Native texture memory/LOD/appearance/cleanup acceptance precedes release adoption. Root owns merge/activation.

Related: [CPU contract](galaxy-v2-runtime-contract.md), [worker checkpoint](galaxy-v2-runtime-checkpoint.md), [existing preview acceptance](demo-art-runtime-acceptance.md).
