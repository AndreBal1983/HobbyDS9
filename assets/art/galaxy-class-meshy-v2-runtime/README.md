# Galaxy Meshy v2 — CPU-prepared runtime candidate

19 October 2026. **Not activated; Unity/texture-aware preview acceptance pending.**

Optimized derivative of the owner's selected [reference-based v2 source](../galaxy-class-meshy-v2/README.md), preserving that source unchanged. Candidate ID `fed.galaxy-meshy-v2.standin.v1`. The current release ship art/mapping is not replaced by these files.

| LOD | Triangles | Geometric vertices | FBX bytes | Reduction from source |
| --- | ---: | ---: | ---: | ---: |
| 0 | 60,000 | 29,840 | 2,292,844 | 94.38% |
| 1 | 20,000 | 9,840 | 808,796 | 98.13% |
| 2 | 7,000 | 3,340 | 315,596 | 99.34% |

One mesh/material per LOD. Original four PBR JPEGs are extracted byte-identically: 4096² base/normal and2048² MR/emission. `MetallicSmoothness.png` losslessly repacks MR.B into R and255−MR.G into A for linear URP sampling; MR red is not an invented occlusion map. Original MR JPEG is provenance/source data, not an extra runtime texture.

`candidate-manifest.json` pins every payload; `derivative-manifest.json` records source SHA, Blender version, transformation, exact counts, texture hashes and export hashes. `Validation/` contains scoped CPU data reports. Source triangles1,067,410 become60k/20k/7k; source UV charts remain, with interpolation from collapse simplification. No re-unwrap, new texture generation, equipment/hardpoint or simulation inference.

Nominal **620 m is art scale only**. Raw glTF -X bow/+Y up is transformed to Blender -Y/+Z and diagnostic glTF +Z/+Y. LOD0 measures approximately451.75 m wide×131.75 m high×619.83 m long in engine coordinates; lower LODs retain the same scale/pivot and shrink under0.4% at the extremities. Fresh CPU FBX re-import checks pass geometry counts/UV/finite normals/authoring axes/bounds. Actual Unity FBX axes/material/LOD import remains a gate.

## Tangent and graphics gate

Strict independent validation caught3zero tangent vertices on2small triangles in the diagnostic LOD0GLB. A recorded fallback uses the existing incident triangle's UV gradient, orthogonalizes against its normal and derives handedness. It changes only those tangent entries, not geometry/UV/normal/image data. See `Validation/tangent-repair.json`. Diagnostic GLB normal/tangent/UV/axes/scale and all4texture-pixel checks now pass.

Fresh Blender FBX recalculation still reports3/1/1zero tangent loops across the LODs. **This is not silently accepted as native-ready.** The receiving Unity importer must compute MikkTSpace frames, assert finite/unit/orthogonal results, and apply the same bounded UV-gradient fallback if needed. Then inspect real textured geometry at relevant camera sizes. Current `ArtDemo/PreviewStudio` does not sample texture maps; a texture-aware adapter is required before integration. No Unity/PBR render/native-player claim is made by CPU validation or the provider preview.

## Reproduce / verify

From the repository root, restore the original source using its standard-library script. Then run a new background Blender process, never the user's interactive scene:

```text
python assets/art/galaxy-class-meshy-v2/restore_model.py
Blender --background --factory-startup --threads 2 --python tools/art/prepare_galaxy_v2.py
python tools/art/repair_galaxy_v2_diagnostic_tangents.py
python tools/art/validate_galaxy_v2_cpu.py
Blender --background --factory-startup --threads 2 --python tools/art/validate_galaxy_v2_fbx.py
python tools/art/package_galaxy_v2_candidate.py
python tools/art/verify_galaxy_v2_candidate.py
```

Blender5.2.2LTS prepared the candidate in approximately31seconds with no render/GPU/Unity invocation. Python data validation/repack uses NumPy/Pillow; payload hash verification uses only the standard library. Intermediate GLB, optimized editable packed-texture `.blend`, logs and detailed work files remain in `artifacts/galaxy-v2-prepared`/`artifacts/galaxy-v2-cpu`, outside the source package and current release. FBX and source map bytes are pinned per delivery; FBX serialization timestamps need not be byte-identical across regeneration.

See [candidate contract](../../../docs/galaxy-v2-runtime-contract.md) and [checkpoint](../../../docs/galaxy-v2-runtime-checkpoint.md). Coordinator owns later activation/merge/import/render leases. No new provider calls or credits were used.
