# PB-20261113 art and audio receipt

13 November 2026. Phase: **asset audit complete; receiving visual/audio acceptance belongs to Unity and QA**. Art owns this receipt only. No asset generation, Unity runtime/backend edits, installation, import or engine process was performed.

Worktree: `C:/HobbyDS9/artifacts/worktrees/PB-20261113-art`; branch `codex/pb-20261113-art-audit`; starting SHA `e99c151f9d7f876300457ddac6b7c3c939330140` (remote main verified with ls-remote and fetched). Existing shared audio documentation and production brief remain untouched. Audit evidence copies are under this worktree's ignored `artifacts/pb-art-audit/`.

## Recommended visual dependency

Use the preserved [laptop snapshot b9401ce](https://github.com/AndreBal1983/HobbyDS9/tree/b9401ce175c02ed512e9ae0e675e456b19b2a371), including integrated GUI `450fe17a619c5e8bf8a9d9e05eb37e50c9360d34`, as the proposed recent visual dependency. It contains later received assets and source bindings absent from main and from the older art PRs. This is a recoverable source baseline, **not new native acceptance**. Architecture/Unity/QA select and validate the combined gameplay candidate separately.

The snapshot is verified against the actual remote branch `codex/laptop-migration-20261113`. Its top commit explicitly preserves migration work; integrated GUI commit says not native-tested. Do not pick main alone and call its old preview pack the latest graphics. Do not select a draft merely by PR number.

## Available art and import requirements

All paths in this table are repository-relative **at b9401ce**, unless another pin is named. Resources below have tracked bytes/metas and can be recovered without the unavailable original E: folders.

| Surface | Exact source / provenance | Receiving requirements and fallback |
| --- | --- | --- |
| Galaxy/nebula V3 | `client/HobbyDS9.Unity/Assets/HobbyDS9/Review/Resources/GalaxyPresentationV3/{galaxy.png,nebula.bytes,particles.json,Presentation.shader}`; contracts `docs/generated-visual-v3-receiver-contract.md` and `docs/generated-visual-v3-unity-receiving.md`. Original procedural asset authority is PR84 `aa9b0f092838033ef402629c96d1533c5468249f`, `tools/art/galaxy-nebula-v3/generate.py`. | Atlas RGB is linear emission and A is extinction, not transparency. Require non-sRGB RGBA32, independent mips, Clamp/Trilinear, non-readable, uncompressed and alphaIsTransparency false. Volume is x-fastest 128×96×64 RGBA8, guarded linear mips; 256 particle candidates total. Validate numeric channels/orientation and actual shader output. Actor-known proxies and explored polygon union constrain local detail. Unsupported/lost rendering leaves usable flat navigation with no local reveal. |
| Designer Galaxy | `ArtDemo/Resources/ArtDemo/Designer/Galaxy/` beneath `client/HobbyDS9.Unity/Assets/HobbyDS9/`; `asset-contract.json`, `delivery-manifest.json`, `receiving-inventory.json`, `ATTRIBUTION.txt`, `SOURCE-LICENSE.html`. See `docs/designer-galaxy-art-integration-2026-10-24.md`. | Ten received FBXs, four 2K albedo PNGs, twelve material roles. +Z bow/+Y up, metre source, explicit material remaps/socket-relative parts. Verify sRGB albedo, texture orientation, private preview material caching, orbit/zoom and replacement/disposal. Exact utility-hull reference is illustrative; later selected-hull routing must remain intact. Unknown/missing resources retain labelled schematic fallback. |
| Activity/fleet hulls | `ArtDemo/Resources/ArtDemo/ActivityHulls/`; `receiving-inventory.json`, `CREDITS.txt`, Constitution and Defiant `source-contract.json`; bindings in `ArtDemo/Runtime/ActivityHullPresentation.cs` and `ActivityHullModel.cs`. Galaxy Cruiser reuses the designer pack with Classic nacelles. | Four received models/three textures; recorded fixed Galaxy assembly 70,377 source triangles. Verify selected hull and displayed identity agree, package submesh order/materials, all visible fleet bounds, finite framing and disposal. These are visual assemblies, not new class admission or equipment stats. |
| Original Cardassian battle prototype | `art/ships/cardassian-cruiser-v1/`, exact source map `art/ships/playable-class-visuals-v1.json`, `tools/art/build_cardassian_cruiser.py`; original PR69 `e104143e66c4f3cb7eca042614175b07ede27b47`. Received path `Review/Resources/BattlePresentation/Cardassian/` under the Unity asset root. | Original broad-shoulder/long-spine geometry; LOD triangles 3,320/1,496/512, six materials and eight anchors. Verify +Z bow/+Y up, non-mirrored facing, URP materials and exact lawful mapping; no hidden silhouettes/count-bearing effects. Native artistic approval remains separate. |
| Sol and planetary views | `Review/Resources/SystemBodyArt/` under the Unity asset root: nine JPEG maps, `SolMeshes/` with ten FBXs including separate Saturn rings and ring PNG. `ATTRIBUTION.txt` exists at both levels. `Review/Runtime/PlanetVisualProfile.cs`, `ReviewSolStage.cs` supply procedural/loaded presentation. | Maps attributed by tracked records to Solar System Scope/INOVE, CC BY 4.0. Preserve attribution. JPEGs are original 2K maps imported at up to 1024, sRGB. Sol mesh loader requires readable meshes for transform validation; do not apply the old ship pack's read/write-off policy blindly. Verify normals/UV/centred radius/axes, day-night lighting and rings. Missing models log `System asset fallback` and use procedural geometry; missing maps must be disclosed. Unknown bodies remain unresolved. |
| Legacy Federation preview pack | `ArtDemo/Resources/ArtDemo/Federation/`, `source-manifest.json`, `runtime-manifest.json`; source recipe `tools/art/build_federation_fleet.py`. Historical [art acceptance](demo-art-runtime-acceptance.md). | Three original hulls, nine FBXs, nine materials and three prefabs. Only exact Federation/source-cruiser mapping is enabled in the old catalog; patrol/escort are library entries. Generic marker and explicit no-selection remain valid fallbacks. Historical 15/15 Editor result does not establish this combined build's acceptance. |

### Rights and source distinctions

- Designer Galaxy attribution names Chris Kuhn / Kuhn Industries, Big D / Enterprise-D, BlendSwap page 16226/archive 81407, CC BY-NC-SA 3.0. Preserve the actual attribution/license and derivative notices with any demo carrying it. This is the recorded noncommercial fan-art source, distinct from the older Meshy experiment.
- Activity Constitution geometry is recorded as original procedural art. Defiant is owner-provided Meshy source; the tracked credits explicitly say task ID/license not supplied and no public redistribution clearance claimed. Preserve its source contract and this limitation; do not promote the audit to a public-release clearance.
- The Galaxy Meshy PR49 candidate is a different, unbound pack: `assets/art/galaxy-class-meshy-v2-runtime/` at `061d3a6e83ce644fce4025ae2d49f5a3b1e12923`. Its 60k/20k/7k LODs and texture-aware helper do not supersede the later received designer pack. Import/tangent/shader/native gates remain open in that PR. Do not silently swap it into the build.

## Verified source bytes

Extracted selected snapshot Resources to the isolated evidence folder via Git archive. Recomputed all hashes in designer `receiving-inventory.json`: **65 checked, zero mismatches**; ActivityHulls: **38 checked, zero mismatches**. These verify stored files, not imported meshes or rendering.

| V3 tracked payload | Bytes | SHA-256 computed in this audit |
| --- | ---: | --- |
| `galaxy.png` | 2,951,817 | `5207e26272779e9039907cb559963f99541e1f358972decb6283355cdbf20c1f` |
| `nebula.bytes` | 3,145,728 | `6357f4f109b7efb01af38f638de91677d7670703acad9acd1635a63dfed885ab` |
| `particles.json` | 60,335 | `94312349f46b7c6fe0c519181929728669237c6b31407da21d2735c2d0a4e974` |

Parsed particle list: 256 entries. Read actual atlas metadata: sRGB off, non-readable, Trilinear, Clamp, alphaIsTransparency false, uncompressed platform settings. **Actual Unity importer output, graphics format, mips and numeric-channel probe remain unexecuted.**

V3 receiving contract proposes <35 MiB logical texture/buffer payload excluding engine/driver copies, at most eight lawful local fields, 256 visible particles TOTAL, and <=40 million local volume fetches per submission. These are admission bounds, not measured FPS/VRAM. Keep the actual receiving contract's smaller local target and normal capped macro target; no expansion to hide poor performance.

## Open PR audit and unavailable artifacts

Live GitHub metadata checked 13 November: art PRs49,60,69,74,77,78,80,82,83,84 plus receiving PRs71/81 are open drafts. Recorded exact heads: #49 `061d3a6e83ce644fce4025ae2d49f5a3b1e12923`; #60 `b3496b6b1da31634d9beb8a7357b2480f37a9a72`; #69 `e104143e66c4f3cb7eca042614175b07ede27b47`; #71 `cc8578c17e919dacaa2975b5cc757bee1bc8befc`; #81 `9d0ddbe74ee88093295c1cbea482047a84a851c1`; #82 `23621ae5ebb23e909161d5f0e0cc90aea5aaae46`; #83 `4eb82d96e305ad6d5eb3fc3d05decca36074d62b`; #84 `aa9b0f092838033ef402629c96d1533c5468249f`.

PR84's four CPU proofs, PR83's freeform gallery, PR60's 94-state browser concepts and PR69's CPU exports describe intent/evidence at their own pins. They are not whole-game Unity/native acceptance. The snapshot has later V3 receiving code/data; do not regress to the old 48-cubed soft field just because its earlier proof is easier to locate.

Checked original E: directories for V3 detailed galaxy, freeform v2, playable battle art and Federation fleet: **all unavailable on this machine**. Their raw galleries, editable blends and logs have not been recovered. Original Cardassian mesh previews remain tracked at PR69 (`three-quarter.png`, `top.png`), and old Federation Editor evidence remains tracked under `docs/demo-art-runtime-evidence/`; use those with their labels. No new contact sheet was made from third-party imagery or mislabelled CPU/Editor pictures as the receiving player.

The V3 generation recipe at PR84 hard-codes an old E: map-context file, temporary clipboard reference images and an old 5cb screenshot for calibration. A clean regeneration cannot run unchanged here. Tracked received bytes are available; preserve them instead of silently regenerating/replacing them.

## Audio target

Snapshot `Resources/MusicCatalog.asset` references **nine** MP3s: the prior five plus Bajor 1, Bajor 2, Cardassia 1 and Klingon 1 - no' wIqaw. All nine source files and six LCARS WAVs are tracked. Use snapshot `docs/audio-sources.md` for the additional owner-supplied file hashes, and retain existing metadata/catalog IDs. The shared five-track documentation is older and intentionally untouched by this assignment.

Existing generated ElevenLabs provenance and user-supplied LCARS source limitations remain in the ledger. `docs/art/music-generation-yue2.md` records the owner's retirement of local music generation and return to owner-operated Suno; do not install a local generator or initiate generation. Audition actual music/SFX on the combined player: independent master/music/SFX settings, mute, transition, save/load continuity and no duplicate sources/listeners. Catalog/reference tests are not hardware audibility evidence.

## Concrete receiving visual checklist

1. Record exact combined source SHA, launch/profile, Unity version and asset fingerprints. Keep 6000.6.5f1 project pin in source; QA's isolated 6000.6.9f1 experiment must archive every import/version delta. No engine experiment was run by Art.
2. At 1280×720, 1920×1080 and 3440×1440: inspect current Campaign/System/fleet-card GUI, selected context, keyboard focus, readable values, primary actions and scrolling. Keep browser/mockup references separate from actual player captures.
3. Designer: confirm selected hull routing, textured Galaxy where explicitly illustrative, distinct activity hulls, correct nacelle/core offsets, no coincident variants/pink materials, bounded orbit/zoom and cleanup. Show actual counts separately from one representative component.
4. System/Sol: check the delivered maps/meshes rather than an unexplained generic fallback; Earth/Mars identity, all Sol bodies, orientation, lighting, ring transparency and selection alignment. Capture fallback diagnostics if resources fail validation.
5. Galaxy: actual warm compact core, branching/dust morphology, restrained coloured local wisps/particles; independent depth/Nebulae controls. Verify unchanged star/route picking and content-plane alignment. At matched cameras local on-minus-off contributions must remain zero outside the explored union; hidden proxies never become silhouettes or decorative clues. Keep the recorded conservative inset honest.
6. Combat: distinct lawful Federation/Cardassian visuals, full fleet framing, shield-sector labels and ammunition from reports; no hidden launcher/model/count reveal. Pause/skip/speed only present the resolved result. Test replacement/detach/save-load without leaked meshes/materials/targets or replayed mutations.
7. Report measured imported memory/draws/frame behavior, actual shader/import failures and audio listening results. Missing GPU timing is unavailable evidence, not a performance pass. Preserve captures and old packages; final owner artistic/native acceptance is still required.

## Delivery boundary and next dependency

Executed: remote pin verification/fetch, live open-PR metadata and source contract reads, Git tracked-path recovery audit, four old-directory availability checks, snapshot asset extraction, 103 receiving hash checks, V3 hashes/particle count/metadata inspection. Historical Unity/CPU/browser pass counts cited by upstream records are **reported evidence**, not rerun here. No gameplay tests were needed for this documentation audit.

Next: Unity confirms the combined recent source candidate and preserves these asset/provenance paths; QA owns the exclusive 9f1 import, graphics, strict build and native/audio checks. The audit itself has no dependency on .NET/Visual Studio installation. It does not certify those prerequisites or a playable executable.
