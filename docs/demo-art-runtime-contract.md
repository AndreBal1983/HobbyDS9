# Demo ship art runtime contract

14 October 2026. **Implemented source; native acceptance pending.** Reversible provisional demo presentation, not final canon artwork or new gameplay rules. Art owns `client/HobbyDS9.Unity/Assets/HobbyDS9/ArtDemo`, `tools/art` fleet recipes/verification, and this contract. Frontend owns its Review entry/view/styles and call sites; backend owns simulation and lawful projections. No frontend or backend source is changed by this package.

## Consumer seam

Add `HobbyDS9.ArtDemo` to the consuming Unity assembly definition's references. Namespace `HobbyDS9.ArtDemo` has no Application/Contracts dependencies or campaign/save access. It references the project's existing URP/Core runtime packages.

```csharp
var stage = new ShipPreviewStage(512, 320);
// Only from an already-filtered selected OWN or LAWFULLY VISIBLE projection:
stage.Show(new ShipPreviewSelection(civilizationDefinitionId,
    shipDesignDefinitionId, ShipPreviewDisclosure.Owned));
image.image = stage.Texture; // UI Toolkit Image; live UI owns size and label.
label.text = stage.Label;
// On loss of visibility/no selection: Clear() or Show(null)/Disclosure.None.
// On panel detach/replacement: clear the Image reference, then Dispose().
stage.Dispose();
```

`Show` replaces any previous instance, frames the geometry and renders immediately. `Render()` is manual; no per-frame redraw, Update/input/animation/physics/audio service is created. `Clear()` clears the image immediately and removes the old instance. `Texture` is a fixed-size 32–2048-pixel-per-axis ARGB32/depth24 render texture, default 512×320, about 1.1 MiB colour plus depth before driver overhead. Resize by disposing/recreating; keep one stage per selected-ship panel rather than per roster row. `Dispose()` is idempotent and releases native texture/mesh/material/objects. In play mode destruction is deferred by Unity; the root and replaced model are disabled immediately. Calls after disposal throw `ObjectDisposedException`.

`Label` is a plain string. `Availability` is `Empty`, `Model`, `GenericFallback` or `RenderingUnavailable`; `VisualKey` is null except for a successfully loaded mapped model. Display the live label alongside the image, including the generic fallback label. The caller owns authoritative identity names, text/accessibility/focus and visibility. It must not infer a civilization definition from an actor ID: e.g. `civ.federation` is not `civilization.federation`. Own strategy uses the supplied `CivilizationSnapshot.DefinitionId` and ship design ID. Observed enemies lacking a legally revealed identity stay generic. Do not query a trusted save/roster to fill this seam.

## Exact provisional mapping

| Civilization definition | Ship design definition | Visual key | Resource path |
| --- | --- | --- | --- |
| `civilization.federation` | `ship-design.source-cruiser` | `fed.exploration-cruiser.v1` | `ArtDemo/Federation/Prefabs/exploration-cruiser` |

Only `Owned` or `LawfullyVisible` disclosure permits rendering. `None`, invalid enum, or null selection clears the stage with **No visible ship selected** and no identity label. Unknown/missing mapping or prefab gives an abstract octahedral marker labelled **Ship model unavailable · generic marker**. Cardassian ships never receive Federation geometry. A renderer without supported render requests reports **Ship preview unavailable in this renderer**. No substring, role, statistics, class-name or generic utility-hull inference.

`ShipVisualCatalog.Find` also exposes art-library entries `fed.patrol-frigate.v1` → `ArtDemo/Federation/Prefabs/patrol-frigate` and `fed.compact-escort.v1` → `ArtDemo/Federation/Prefabs/compact-escort`. These are included for subsequent explicit mapping, not extra gameplay ships. Do not map them to existing ships without recording an exact reversible binding. The library contains no exact canon replicas or inferred equipment/refits.

## Assets and isolation

The three original procedural hulls and all dependencies are tracked under `ArtDemo/Resources/ArtDemo/Federation`: nine FBX files/metas, three prefabs/metas and nine shared URP Lit materials/metas. Resources dependencies make them build-included without ignored artifact preparation or an external installation. No downloaded noncommercial Galaxy prototype geometry is used. `source-manifest.json` preserves the original pack's provenance, source hashes, art choices and per-LOD counts. `runtime-manifest.json` pins Resources files: raw SHA-256 for FBX payloads; UTF-8 LF/trailing-line-whitespace-normalized SHA-256 for Unity text, permitting serialization whitespace only. Committed asset text uses clean LF. The source authoring recipe is `tools/art/build_federation_fleet.py`; editable Blender originals/previews remain preserved at `E:/HobbyDS9/artifacts/art/federation-fleet-v1`. Regenerate in a fresh Blender background process, optionally passing an output directory after `--`; do not reset the user's interactive Blender scene.

| Original hull | Art length | LOD0 / LOD1 / LOD2 triangles |
| --- | ---: | --- |
| Exploration cruiser | 620 m | 7700 / 3542 / 2004 |
| Patrol frigate | 280 m | 6612 / 3072 / 1564 |
| Compact escort | 170 m | 3892 / 1852 / 1260 |

Metre bounds, +Z bow/+Y up, identity root scale, module/effect anchors, normals/basic planar UVs and proposed LOD transitions .18/.07/.015 are retained. Art lengths are never authoritative stats. Material colours/emission need no external textures; unique atlases and detailed surface polish remain open. Multiple material slots mean draw-call/fleet budgets still need profiling. Model read/write is disabled for runtime memory savings; the Editor fixture reads imported geometry during acceptance.

The provider frames a normalized ten-unit model using an inactive/manual camera. Each stage has a reserved-layer-31 volume at Y=-10000, separated from other live stages by 128 units; freed slots are reused. **Layer 31 is reserved for these offscreen previews.** Each owned root stays inactive except inside a synchronous manual render try/finally. No persistent stage geometry or lights enter world-camera frames; no new scene or actual Light objects are created. Do not place game geometry on this layer/in these volumes or render a world camera there.

The stage uses cloned preview-only materials with `ArtDemo/PreviewStudio`, a fixed normal-based studio shader preserving hull colours/metallic/smoothness/emission. It ignores external lights, ambient probes, fog and volumes. Shared world-prefab URP Lit materials are unchanged. This bounded studio treatment is proposed preview presentation, not a physically based world-lighting replacement. There is no postprocessing, shadow, animation, collider, audio listener, simulation position or equipment inference. Active scene, external cameras/lights, RenderSettings and volume profiles stay unchanged. Dispose on panel loss to avoid retaining unused stages.

Do not use `Camera.scene` as runtime isolation: [Unity documents support only for Editor preview scenes](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Camera-scene.html). The provider deliberately does not depend on that property.

## Validation and reproducibility

CPU-only integrity check: `python tools/art/verify_demo_art_assets.py`. Prepare ordinary portable Unity dependencies with the existing `scripts/PrepareUnity.proj` and `scripts/PrepareUnityIntegrated.proj` Prepare targets. Neither the art package nor build needs the ignored art source directory.

After obtaining the coordinator's exclusive Unity/import/render lease, use pinned Unity **6000.6.5f1**, graphics enabled, EditMode filter `HobbyDS9.ArtDemo.Tests`. The fixture checks exact/unsupported/hidden mapping, all three tracked prefab/LOD mesh counts/axes/metre bounds/UV/normals/materials, real cruiser/fallback pixels (not pink error shader), clear/replacement, simultaneous visibly distinct instances, external bright all-layer light/default-layer object pixel invariance, external state preservation and native allocation/disposal. Actual engine screenshots are written to `artifacts/art-demo-runtime/cruiser.png` and `fallback.png`, or the directory in `HOBBYDS9_ART_EVIDENCE`. These are graphics-enabled Editor tests/captures, not a standalone native player usability run.

Source prepared at base `8ae866556e21cef47a9e5b0efaea49e01d30f3f6` in branch `codex/demo-art-runtime`, isolated checkout `C:/Users/ambal/.codex/worktrees/demo-art-runtime/HobbyDS9`. No competing import/render work is authorized until lease release. Current native pass/fail, evidence and delivery pin belong in [the art checkpoint](demo-art-runtime-checkpoint.md); do not treat historical isolated-pack import checks as tests of this provider or a native player usability result.
