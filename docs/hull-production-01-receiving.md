# Original hull candidate receiving 01

13 November 2026. **Source receiving candidate; Unity import/material/native/owner artistic acceptance OPEN.** These are original proposals, not the approved external replacement hulls. Further gameplay remains gated by main-HUD reference fidelity.

## Scope and exact bindings

Art source [PR105](https://github.com/AndreBal1983/HobbyDS9/pull/105) final guide pin `70d55db18f2fb3be4e0196094736c799a88aa3f9`, binary pin `bfaa434babba12839be38b9a1cb44c4f973db427`. The final [manifest](../art/asset-production-01/hulls-v1/manifest.json), [material recipes](../art/asset-production-01/hulls-v1/material-recipes.json), editable sources, six inspection renders and [contact sheet](../art/asset-production-01/hulls-v1/contact-sheet.png) remain outside Unity Assets. The contact sheet was inspected; it is Cycles/AgX source evidence, not this game's native appearance.

| Existing disclosed hull ID | Received original candidate | Runtime triangles / slots / nominal art length |
|---|---|---|
| `hull.galaxy-cruiser.v1` | Complete merged Cruiser_LOD0 exterior |9,884 /8 /620 m |
| `hull.defiant-combat.v1` | Complete merged Escort_LOD0 exterior |7,560 /8 /170 m |

Only these two FBXs and eight shared factor-only materials enter `ArtDemo/Resources/ArtDemo/Production01/Hulls/`. Lower LODs, GLBs, editable blends and studio renders remain source evidence; there is no claim of runtime automatic LOD switching. Constitution retains its existing original binding. Galaxy designer studies, old source meshes, metadata and attribution remain unchanged. The approved external Galaxy/Defiant replacements remain unreceived.

The exact activity catalogue keys remain stable. Its two captions now say **original candidate exterior**. Construction and Activities show those captions through the existing stage. Fleet inspection may use the exact current-frame owned activity ship's `HullVersionId`, matched by ship and fleet ID after actor/turn/revision checks; it does not infer a hull from class text, role, civilization or dimensions. Unknown exact hulls stay schematic. The original three-argument ReviewShipArtPanel constructor remains as an overload for existing callers; selection without an exact hull retains the original lawful civilization/design resolver. A utility hull does not acquire Galaxy geometry.

Actor-filtered Battle keeps its current owned exact-hull and permitted enemy-family representative bindings. If an original candidate is displayed, the panel explicitly discloses **Original candidate hull art; artistic approval pending** without exposing an enemy's engineered version. No authoritative pose, mount, hit, weapon, stock or battle result is inferred from the new mesh. The separate Combat fixture's static owned inspector does not become fabricated tactical playback.

## Import/material/lifetime contract

`tools/art/receive_hull_production_01.py` writes/checks deterministic new files from the pinned source records and factor recipes. It records exact source payload hashes, canonical UTF-8/LF manifest/recipe hashes, slot order, nominal lengths and unique GUIDs in the [runtime inventory](../client/HobbyDS9.Unity/Assets/HobbyDS9/ArtDemo/Resources/ArtDemo/Production01/Hulls/receiving-inventory.json). Import metadata remaps each named source slot explicitly; runtime also requires the imported slot to reference that exact Resources material before displaying the candidate. Missing/wrong mappings produce honest unavailable/schematic fallback.

Materials use the current URP `HobbyDS9/ArtDemo/PreviewStudio` shader, with source linear colour, metallic, smoothness and emission colour multiplied by strength. No unique hull texture, normal map, new shader, studio environment, bloom, camera, animation, rig, collider or listener is introduced. The shader is opaque/back-face culled with bounded studio lighting, not a physical Cycles material match; glTF double-sided flags do not change its culling. Normals/winding and appearance need actual native receiving.

FBX import preserves complete root TRS/metre units and authored normals/UV0, generates tangents, leaves mesh compression off, and keeps Read/Write for explicit geometry acceptance. Runtime retains the existing socket and 10/nominal-length preview or4/length Battle normalization. One merged model contains the complete exterior; no core/cutaway is added. A stage owns its private material clones/model and disposes them; Resources materials and meshes remain borrowed. Full-size512x320 inspectors remain, with no thumbnail substitution or per-row render stage.

The earlier Earth/Mars receiver now reads an exact retained560d6c1 source-manifest snapshot under `tools/art/contracts/`. Art's final batch extended the System manifest without changing the selected FBX/map bytes; this preserves the existing first-slice inventory and reproduction checks instead of rewriting it when adding the hull packet. The received Earth/Mars asset bytes, GUIDs, maps and captions remain unchanged. The Earth shell is still not enabled.

## Validation and native gates

Run `python -B tools/art/receive_hull_production_01.py --check` and the earlier System receiver --check. The independent source verifier covers all six hull GLBs/payloads; no Blender/Unity launch is required. Art's relocated FBX results remain separate source evidence. New Editor assertions require exact received triangle counts, eight named remaps, centred Unity metre bounds, complete merged exterior and no cutaway; existing private-material lifetime/orbit/framing/hidden-ID rejection cases remain. These are source-compiled tests until QA executes them.

QA must receive the exact final integration SHA after existing queued work, run `HobbyDS9.ArtDemo.Tests.ActivityHullPresentationTests` with actual graphics and current source dependencies, then exercise Construction, Activities, selected Fleet and actor-safe Battle. Check source/import axis conversion (+Z bow,+Y dorsal), full silhouettes/normal winding/UVs, slot/colour/emission mapping, no pink or fallback, original-candidate captions, exact unsupported hull fallback, switching/same-ID reuse/detach/resource teardown and shared-source immutability. Capture the full model at agreed sizes; inspect actual draw calls, memory and frame cost rather than inferring performance from smaller triangle counts. Recheck Save/current-frame/draft/profile boundaries alongside the main-HUD candidate. No new gameplay, save/schema, artistic approval or native performance acceptance is established by this document.
