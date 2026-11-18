# Owner Galaxy Meshy v2 candidate runtime derivative

19 October 2026. **CPU preparation in progress; graphics acceptance pending.** Base `c2621f82c52b38dcfdf85bbcddf8e5d43dde459b`, branch `codex/galaxy-v2-runtime`, isolated checkout `C:/Users/ambal/.codex/worktrees/galaxy-v2-runtime/HobbyDS9`. This is the owner-selected reference-based Meshy v2 asset from `22a5ad955e3e45a01981b4bc7fedcdb256718b68`, preserved unchanged under [source/provenance](../assets/art/galaxy-class-meshy-v2/README.md). No new generation, credits, provider download or old third-party Galaxy geometry.

## Proposed asset seam, no activation

- Candidate key `fed.galaxy-meshy-v2.standin.v1`; separate candidate resource folder `GalaxyStandIn`. No gameplay identity/mapping or equipment inferred. Existing `ArtDemo` release catalog, the exact Federation/source-cruiser mapping, disclosure guard, generic fallback and original three-hull pack remain pinned.
- Art owns new derivative recipe, manifest, candidate model/texture/material/prefab paths and optional texture-aware preview support. Secondary owns existing UI call sites; coordinator owns merge/activation and Unity/GPU/native leases.
- Initial CPU targets: **60,000 / 20,000 / 7,000 triangles**, one mesh/material per LOD. These are provisional art budgets; measured counts and reduction/quality diagnostics must be recorded. No fleet-performance approval implied.
- Nominal art length **620 m**, consistent with the old cruiser stand-in's preview convention, is a presentation choice only. Source normalized units are not physical dimensions or gameplay stats. Raw glTF bow is -X, up +Y; candidate export must be +Z bow/+Y up, centered bounding-box pivot and identity scale.

## Textures and preview dependency

Source GLB: 1,067,410 indexed triangles,580,755 indexed attribute vertices,one double-sided material. Four embedded JPEG maps: base colour4096², normal4096², metallic/roughness2048² and emission2048². Source factors are base white, metallic1,roughness1,emission white. There is no authored occlusion map: do not infer one from the unused MR red channel.

Keep original embedded images byte-identical in the derivative provenance. Blender import/export preserves UV charts; simplification may interpolate UV coordinates, so chart/appearance fidelity requires graphics review. For URP, use base/emission as sRGB, normal/non-colour data correctly imported, and derive metallic/smoothness as **R=source MR.B**, **A=255−source MR.G** with linear sampling. Double-sided source remains double-sided until inspected. Preserve original maps; any importer downsampling is a documented reversible art budget, not source replacement.

**Existing `ArtDemo/PreviewStudio` does not sample maps.** Merely loading this mesh through its current colour-only material conversion loses the supplied texture detail. Keep activation disabled until a separately reviewed texture-aware path carries base, normal, metallic/smoothness and emission maps into preview materials, including UV/tangent and sRGB conventions. Do not claim textured UI integration from a provider thumbnail or CPU conversion.

## Verification gates

CPU: source/chunk SHA-256, actual GLB inventory, source orientation slices/projections, post-reduction triangle counts/UV/normal/material/tangent/bounds integrity, texture extraction/repack hashes and channels, independent exported-axis/scale checks. Preserve scripts and exact measured manifest.

Later coordinator-authorized graphics slot: pinned Unity import, explicit texture importer/URP material/prefab/LOD metadata, mapped/fallback/disclosure lifecycle and real textured engine captures. Current release stays pinned until those pass. No desktop takeover, active Blender scene modification, GPU render or Unity import/build is authorized during CPU preparation.

See [worker checkpoint](galaxy-v2-runtime-checkpoint.md) for actual completed evidence and next actions. Proposed design choices above are not new agreed simulation rules or a claim of exact canon equipment.
