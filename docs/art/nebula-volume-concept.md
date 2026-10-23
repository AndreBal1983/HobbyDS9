# Procedural nebulae with map parallax / concept approach

23 October 2026. **Owner requested:** an original nebula concept inspired by four supplied astronomy images, visible depth while panning the galaxy map, and a concrete procedural Unity3D plan. **Proposed:** the rendering approach below. **Not implemented:** production Unity rendering or a gameplay change. Existing map build9d0, checkpoint913, ship pack and prior99-file art gallery remain unchanged.

Use dark, sculpted dust as the structure, illuminated thin rims and translucent blue/teal emission as the surrounding volume. Warm gold and restrained pink/magenta are alternative ionization palettes, not ownership or danger colours. Keep broad readable voids and asymmetry; avoid a uniformly bright oval or repeating cloud stamps. The four original attachments were inspected directly; their pixels will not be used as textures.

Recommend a shared seeded3D density representation, bounded to each supplied nebula footprint. A low-resolution3D field defines the main body, domain-warped filaments and dust channels; palette and embedded light parameters provide variety. Overview uses a cheap depth-preserving approximation; close inspection can use bounded raymarching. A flat plane alone cannot supply internal depth.

An orthographic camera translated sideways produces no depth-dependent parallax. Use a locked-orientation perspective presentation camera, with stars/routes/picking on the reference map plane and nebula density at several decorative depths. Keep map-plane pan/zoom projection exact and constrain depth/tilt. An orthographic fallback may deliberately offset depth layers, but must be labelled an artistic approximation, not physical parallax.

The first proof will run on CPU only: original procedural density slices, a four-treatment contact sheet, actual Natural v5 map-scale context and a browser Canvas2D demo with perspective-projected depth slices. This demonstrates depth-dependent displacement and changing overlap, not a shipping Unity shader or arbitrary-orbit volume. Maximum initial batch: four variants,32 slices per variant at512×320, bounded below512MiB working memory; at most six headless browser captures with GPU disabled. No external downloads, paid generation, native UI, Unity launch/import/build or GPU rendering.

Anchor the proof to actual seed42/250 Natural v5 data (`4ea9b0d4581edb60460413e9fee0203257bd47e5b6cb43fc044ee5b2cda01976`), never shift pickable stars, region polygons or route vertices to fake depth. All decorative randomness uses a separate art-version/feature-ID seed. Volume depth, colour and dust do not determine traversal, sensors, visibility, ownership, damage or new map topology.

The completed study will add concrete camera mapping, LOD/overdraw/memory budgets, readability checks, renderer alternatives, risks and a staged Unity acceptance plan. Performance figures will be proposed budgets until measured in the real Unity scene.
