# Procedural nebulae with map parallax / concept approach

23 October 2026. **Owner requested:** an original nebula concept inspired by four supplied astronomy images, visible depth while panning the galaxy map, and a concrete procedural Unity3D plan. **Proposed:** the rendering approach below. **Not implemented:** production Unity rendering or a gameplay change. Existing map build9d0, checkpoint913, ship pack and prior99-file art gallery remain unchanged.

Use dark, sculpted dust as the structure, illuminated thin rims and translucent blue/teal emission as the surrounding volume. Warm gold and restrained pink/magenta are alternative ionization palettes, not ownership or danger colours. Keep broad readable voids and asymmetry; avoid a uniformly bright oval or repeating cloud stamps. The four original attachments were inspected directly; their pixels will not be used as textures.

Recommend a shared seeded3D density representation, bounded to each supplied nebula footprint. A low-resolution3D field defines the main body, domain-warped filaments and dust channels; palette and embedded light parameters provide variety. Overview uses a cheap depth-preserving approximation; close inspection can use bounded raymarching. A flat plane alone cannot supply internal depth.

An orthographic camera translated sideways produces no depth-dependent parallax. Use a locked-orientation perspective presentation camera, with stars/routes/picking on the reference map plane and nebula density at several decorative depths. Keep map-plane pan/zoom projection exact and constrain depth/tilt. An orthographic fallback may deliberately offset depth layers, but must be labelled an artistic approximation, not physical parallax.

The first proof will run on CPU only: original procedural density slices, a four-treatment contact sheet, actual Natural v5 map-scale context and a browser Canvas2D demo with perspective-projected depth slices. This demonstrates depth-dependent displacement and changing overlap, not a shipping Unity shader or arbitrary-orbit volume. Maximum initial batch: four variants,32 slices per variant at512×320, bounded below512MiB working memory; at most six headless browser captures with GPU disabled. No external downloads, paid generation, native UI, Unity launch/import/build or GPU rendering.

Anchor the proof to actual seed42/250 Natural v5 data (`4ea9b0d4581edb60460413e9fee0203257bd47e5b6cb43fc044ee5b2cda01976`), never shift pickable stars, region polygons or route vertices to fake depth. All decorative randomness uses a separate art-version/feature-ID seed. Volume depth, colour and dust do not determine traversal, sensors, visibility, ownership, damage or new map topology.

The completed study will add concrete camera mapping, LOD/overdraw/memory budgets, readability checks, renderer alternatives, risks and a staged Unity acceptance plan. Performance figures will be proposed budgets until measured in the real Unity scene.

## Recommended Unity implementation

Keep the existing Unity6000.6.5f1 / URP17.6.0 project. Add a presentation-only nebula renderer, not a pipeline migration. Unity's [pipeline comparison](https://docs.unity.com/en-us/engine/6000.6/manual/render-pipelines/choose-a-render-pipeline/feature-comparison) lists local volumetrics as an HDRP feature rather than a built-in URP feature. The proposal therefore needs its own bounded volume shader/pass; adding a fog setting alone is insufficient. Use the supported [URP Render Graph custom-pass workflow](https://docs.unity.com/en-us/engine/6000.6/manual/render-pipelines/universal-render-pipeline/customizing-urp/render-graph) for transient targets and compositing. These are design recommendations, not code compiled against17.6.0 in this study.

| Representation | Useful role | Depth truth and trade-off |
| --- | --- | --- |
| One flat texture | Very distant thumbnail/fallback | No internal pan parallax; insufficient as the requested main treatment. |
| Coherent depth slices | Regional map, first implementation proof | Real separated sample planes from one3D field; changing overlap and perspective displacement. Limited viewing angle, slice/alpha artifacts and overdraw remain. |
| Local3D density raymarch | Nearby large nebulae | Samples a continuous3D field along each pixel ray; supports changing viewpoint without cards separating. More texture fetches/fill cost; requires bounds, step limits and lighting approximations. |
| Hybrid, recommended | All map scales | The same seeded morphology feeds raymarch, slices and far impostors, so LOD changes preserve identity. Hysteresis and crossfade are required to avoid popping. |

Start with a fixed-orientation depth-slice proof inside an isolated Unity review scene. Once picking/readability/parallax are accepted, replace only the near representation with a local proxy-volume raymarch. Do not build a full-galaxy fluid simulation, simulate dust every frame, or use hundreds of independent particle clouds. Static morphology plus camera movement already delivers the requested depth.

### Seeded morphology and lighting

Derive a separate art seed from UTF-8 `nebula-art-v1 | galaxy seed | stable FeatureId`; specify a stable hash/PRNG in the implementation. Do not reseed from the whole MapContentHash: a quiet-area/topology change can change that hash while preserving the nebula features, and should not reshuffle their art. Use resolved feature geometry and visual parameters in cache validation; retain map hash only as provenance/binding metadata. Never use platform-dependent string hashing or consume campaign/generator RNG. Resolve cached shape parameters once: asymmetric ellipsoidal lobes, cavity offset, filament direction/width, ridge erosion, dust extinction, palette and one or two decorative embedded lights. Position/radii come from the existing supplied feature. Authoring parameters and renderer version belong in presentation metadata/cache, not map ContentHash or game saves.

Build a coarse signed envelope, domain-warped3D fBm and ridged dust field, then preserve empty channels by thresholding rather than filling every voxel. Use two channels for emission density and dust/extinction; derive colour from a small palette and stable light coordinates. Warp amplitude stays below the envelope margin. Generate a64³ or128³ field once off the main thread or through a separately accepted compute path. Quantize/cache it with a versioned key; do not rebuild during pan. Texture upload is a bounded main-thread step. Unity documents [Texture3D.Apply](https://docs.unity.com/en-us/engine/6000.0/script-reference/unityengine/texture3d/apply) as a full upload and allows releasing the CPU copy afterward; avoid repeated per-frame uploads.

For the near raymarch, intersect the view ray with the bounded nebula envelope, sample front-to-back and use `alpha = 1 - exp(-sigma * density * stepLength)`. Accumulate premultiplied colour/transmittance and terminate when transmittance is negligible. Distinguish emission glow from cold dust extinction: additive-only blending cannot make the dark lanes in the references. A precomputed directional-light term or a few bounded density taps can suggest illuminated rims; full multiple scattering/shadow raymarching is outside the first proof. Keep light positions fixed to the volume so surfaces do not shimmer when panning. Avoid pumping exposure, rapid noise animation or a glittering particle effect.

Use muted blue/teal with thin gold rims as the default proposal, with blue/rose and violet cavities as morphology/palette variations. These colours have no faction, ownership, danger or sensor meaning. Bloom, if later enabled, affects only the cloud pass and is restrained enough to retain dark channels. The volume should remain legible with bloom disabled.

### Camera, anchors and depth

Let authoritative map coordinates be `(X,Y)` and convert them with one documented presentation scale `k` into the reference plane `Z=0`. Keep the camera at `(Cx,Cy,D)`, looking along `-Z` with no initial orbit. A fixed focal length projects a cloud sample at decorative depth `z` by `screenX = centreX + focal * (kX-Cx)/(D-z)`; the map plane uses the same expression with `z=0`. Pan translates the camera, so near dust moves faster and distant glow moves slower. Picking intersects the pointer ray with `Z=0`, then uses the existing map hit policy. Region edges, star markers, candidates, route vertices and hit targets all use that exact plane projection.

Store thickness in volume-local art units, proportional to the supplied ellipse, not in gameplay distance/time. Keep it fixed during pan/zoom. Choose the closest allowed camera distance first and cap half-depth to roughly12% of that distance for the initial production proof; clamp zoom before reaching/entering a volume. The CPU demo exaggerates half-depth to18% of camera distance to make the comparison clear. It has pan and a flat-depth control, not arbitrary camera orbit or production zoom. Across ±120px of map-plane pan the near/far samples have different displacements; pickable stars still move by exactly240px.

If retaining an orthographic strategic camera is necessary, use a separately disclosed decorative depth transform or a secondary perspective environment camera. Do not claim that translating an orthographic camera alone creates parallax. A secondary camera must share the reference-plane mapping exactly; otherwise stars appear to slide across their own cloud anchors. A single perspective camera with layered rendering is simpler to validate initially. Orbit/tilt remains deferred until the UI can show/reference the same plane without ambiguous picking.

The visible footprint outline remains the authoritative centre/radii projected on `Z=0`. Shape is an artistic subset of that envelope, not a new gameplay boundary. The demo uses an explicit reference-plane mask for bounded cloud compositing; that is an approximation. The Unity volume should use a smooth3D envelope and a projection safety fade, with no hard oval edge or unrelated flat backdrop. Decorative dust never obscures required navigation information.

### Layering and readability

Composite space and nebula colour first, then region strokes, selected boundaries, routes, stars/selection rings, labels and console UI. The readable navigation layer is deliberately not physically extinguished by foreground dust. This is a command map: a cloud can have depth while its navigational overlay remains clear. Use a dark under-stroke on amber routes, light star cores with a dark separator, sparse labels and a safe contrast backing where needed. Semantic zoom controls labels; no extra decorative stars in the map should look pickable. The treatment previews' tiny stars are illustrative only and are absent from the actual-map demo.

A future actor-filtered view creates volumes only from features lawful for that view, before rendering/cache upload. Do not build hidden nebula volumes and merely fade their labels: their glow/parallax can reveal private geography. The present demonstration is explicitly full generator output, not evidence of fog-of-war integration. Feature visibility, sensor effects, hazards and movement costs remain Application/simulation decisions.

### Proposed budgets and LOD

These are starting limits to profile, not measured Unity performance. Target a representative1080p map at60fps and reserve at most1.5ms steady GPU time /2.5ms heavy-view time for nebulae on an owner-selected target GPU. Confirm the hardware before treating those numbers as acceptance. CPU pan should allocate no memory or regenerate noise. Degrade samples, target resolution and decorative volume count before sacrificing route/picking clarity.

| Projected footprint diameter | Initial representation | Proposed bound |
| --- | --- | --- |
| Below12px | Stable feature glyph / tiny static fallback | Internal parallax is subpixel; never remove the actual feature marker. |
| 12–64px | Three-depth impostor | Shared morphology, no claim of an orbitable volume. |
| 64–240px | 8–12 coherent depth planes | Low-resolution cloud target, shared global depth order. |
| Above240px | Local density raymarch |24–48 samples; at most4 nearby high-detail volumes, early exit. |

Use roughly15% hysteresis around LOD thresholds and a brief spatially stable blend between representations. Keep art seed/light/depth unchanged across transitions. Prefer no temporal accumulation in the first proof; it avoids ghost trails during pan. If history is added later, reset on map/feature/camera discontinuities and exclude UI/markers from the history.

Budget cloud shading at half resolution, with a starting cap of960×540 at1080p and1280×720 for larger windows. Cull by projected bounds and use per-volume screen rectangles or tile lists rather than marching the whole screen for every feature. Target total shaded cloud coverage below35% in ordinary views, with an explicit dense-overlap fallback. Regional planes from different volumes need a common back-to-front depth order; sorting whole cloud centres is insufficient when they overlap. A shared volume pass with a small per-tile list is the near-stage recommendation. Do not draw32full-screen transparent planes for every nebula.

At128³, an uncompressed RG8 density/dust field is4MiB, approximately4.57MiB with a full3D mip chain. Eight resident128³ fields plus eight64³ fields use roughly41.1MiB. One2048²RGBA8 impostor atlas with mipmaps adds about21.3MiB: an initial texture-pool target is64MiB. Two960×540RGBA16F colour targets add about7.9MiB; allow up to96MiB total nebula GPU residency including larger-window targets and overhead. These are arithmetic estimates, not allocation captures. Verify RG8/3D-format/filter support and actual residency on target hardware; use supported fallbacks rather than assuming3D compression. Release CPU texture copies after accepted upload where regeneration is unnecessary. Cap near-volume residency and stream/cache by lawful view, with stable visual eviction order.

### Staged acceptance before production

1. Approve one morphology/palette and the desired parallax strength from this original study. Do not treat the photographs or these colours as final game assets by implication.
2. In an isolated URP review scene, project one actual feature and unchanged stars/regions/route. Capture centre and ±pan at1280×720,1920×1080 and3440×1440. Verify pointer rays/picks before and after pan/zoom and exact overlay coincidence, with clouds on/off.
3. Add8–12slices from the same seeded field, test edge fade and sorted overlap, and compare against a flat-depth control. Keep reduced-motion/static options without changing gameplay data.
4. Add the near local-raymarch path only after the slice proof is accepted. Measure GPU pass timings, frame time, overdraw, render-target size, allocations and resident memory on selected hardware. Test dense overlapping fields, map swaps, disposal, LOD transitions and no-bloom operation. Optimise against those captures, not the CPU browser's generation time.
5. Only then bind the renderer behind the actual lawful-feature snapshot boundary. Preserve content hashes, saves, route cost/vertices and all normal commands; add no physics colliders or picking surfaces to decorative volumes.

No Unity implementation, import, build, runtime frame-time claim or production acceptance is delivered by this concept assignment.

## Actual delivered proof and limitations

The original four-treatment contact sheet and interactive demo are in `E:/HobbyDS9/artifacts/art/nebula-volume-v1-20261023-2/`. Open review.html for the gallery or index.html for the interactive study. Choose a treatment and use **Play parallax** or drag/Move viewpoint; **Depth** provides a flat control and **Depth guides** exposes near/map/far sample positions. Final map composites are map-scale-final.png and depth-final.png. Source recipes are [generate.py](../../tools/art/nebula-volume-v1/generate.py), [demo.js](../../tools/art/nebula-volume-v1/demo.js) and [review-final.cjs](../../tools/art/nebula-volume-v1/review-final.cjs).

The final field batch made4×32RGBA slices at512×320 in39.7735seconds on CPU, with measured process peak working set86,265,856bytes (82.3MiB). The Win32 memory query explicitly succeeded. Earlier batch1 remains preserved separately: its internal memory value0 was invalid because of a handle binding error; an external partial peak87,490,560bytes is not a final measurement. Batch2 refined overly soft initial filaments and corrected that instrumentation. These figures describe offline Python generation, not Unity frame time or GPU memory.

The treatment sheet's four fixed experimental seeds (1021/2033/3079/4051) let the owner compare morphology. This study does not yet implement the proposed feature-seed/cache contract. Its NumPy floating-noise recipe is an authoring proof, not a cross-platform bit-identical simulation algorithm. A production implementation must choose a stable art hash/PRNG and retention policy separately.

Map context uses actual feature `feature.nebula.natural.0010`, centre(2059439,1093827), radii(185023,144014) from seed42/250 Natural v5. Stars/regions and the actual portable API route are unchanged. The demonstration route is explicitly the exact shared-edge-midpoint presentation baseline derived from that API plan; it is not the newer certified frontend drawing or a gameplay change. The colour/depth layers are original generated pixels, not a reference photograph or downloaded texture.

Initial six browser captures/proof and source are preserved under browser-run1. Their hard footprint clip was rejected visually. The approved correction reused all density slices, softened the mask inward and produced only two new captures. Final-source tests show44.6465px near/far separation at120px pan, exact240px map-plane star displacement across ±120px pan, zero depth differential in the flat control, correct raw-star picking, no compact overflow and unchanged raw map/route inputs. Pixel checks also verify zero cloud alpha beyond the reference ellipse and visible sampled star cores. Label placement now skips collisions with neighbouring stars. A same-origin loopback server was used only during pixel verification because file-URL images taint canvas reads; it was closed afterward. The interactive file itself needs no server for ordinary use.

This is a32-plane perspective approximation of one coherent3D field. It proves internal relative movement and overlap during pan; it does not prove arbitrary orbit, near-volume traversal, physically correct multiple scattering, overlap sorting across many nebulae, full-map performance, final material quality or production camera integration. The inward reference-plane mask is a readable bounded mockup compromise, not a general3D clipping solution. Final opacity, map-scale palette, default parallax strength and morphology require owner review; memory/GPU/LOD targets above remain proposed.
