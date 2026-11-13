# System production 01 — Earth/Mars receiving

13 November 2026. **Implemented source candidate; actual Unity import, URP appearance, native input and owner visual acceptance remain open.** This is a separate integration after GUI refresh source `6724c1773f792dc5e3b711e71b84537c9db5bb8e`; it does not change the refresh branch or QA's in-progress build.

## Result and source

The System overview and selected/opened Planet portrait now use the original production sphere for the exact surveyed Earth (`planet.sol`) and Mars (`planet.mars`) identities. Both use LOD1, 2,208 triangles, centred unit radius. Current presentation radius, orbit geometry, camera/portrait framing, picking, snapshot disclosure and commands are unchanged. Other resolved worlds keep the previous shared sphere, and unsurveyed worlds keep the neutral procedural sphere. Planet names and class similarities do not admit a new binding.

Art source: [PR105](https://github.com/AndreBal1983/HobbyDS9/pull/105), exact `560d6c13697ba708c5aae4c450c6e5dafa72c837`. Its [manifest](../art/asset-production-01/system-v1/manifest.json), [proposed receiving inventory](../art/asset-production-01/system-v1/receiving.json), editable sources and [contact sheet](../art/asset-production-01/system-v1/contact-sheet.png) are retained outside Unity Assets. The source commit is included as a separate cherry-pick. The contact sheet was inspected; it is Blender evidence, not an image of this game candidate.

The bounded runtime slice is under `client/HobbyDS9.Unity/Assets/HobbyDS9/Review/Resources/SystemBodyArt/Production01/`. Its [actual receiving inventory](../client/HobbyDS9.Unity/Assets/HobbyDS9/Review/Resources/SystemBodyArt/Production01/receiving-inventory.json) pins the source manifest and two exact FBX payloads, binding IDs, triangle counts, unique metadata GUIDs and borrowed map hashes. Earth/Mars continue to borrow the existing unchanged Solar System Scope / INOVE colour maps and attribution.

## Material and resource ownership

The receiver suppresses imported studio materials and external FBX image searches. `ReviewSolStage` constructs its existing shared `HobbyDS9/Review/ProceduralSystemSurface` URP material and copies the current surface/cloud/unknown/atmosphere property block into the portrait. No Blender shader or camera is ported. The Earth shell is **not received or displayed**; the existing procedural rim remains. Adding a transparent Fresnel shell requires another scoped change and native acceptance. Other Sol bodies, rings, orbit/accent exports and alternate GLBs remain reference/source only.

The two new FBX imports retain authored UVs/normals and readable meshes so the existing full hierarchy-transform/radius validation can run. The stage caches each selected resource's transformed mesh once, owns and disposes those copies, and borrows the Resources prefabs/maps. Missing or invalid models still record a diagnostic and use the procedural fallback. Portraits share their selected globe's validated mesh; they do not allocate another imported copy. No new render target, per-frame redraw, layer, controller or simulation object is introduced.

## Reproduce and check

From the repository root, using installed Python:

```text
python -B tools/art/receive_system_production_01.py
python -B tools/art/receive_system_production_01.py --check
python -B tools/art/verify_asset_production_01.py art/asset-production-01/system-v1
```

The receiver uses stdlib only, verifies the selected source hashes/counts and borrowed map hashes, checks GUID collisions, writes deterministic new files, and refuses modified receiving files (allowing LF/CRLF checkout conversion for text only). Its source-manifest hash uses UTF-8/LF Git text content; FBX and texture hashes always identify raw bytes. `--check` never writes. It copies neither `.blend` nor GLB into Unity and does not require Blender or Unity to prepare the runtime payload. The independent source verifier checks all31 GLBs and manifest payload/source-map hashes; those checks do not validate Unity's importer or shader pixels.

The graphics-gated `SolAssetBindingTests` keeps the existing asset, disclosure, portrait and lifecycle checks, and updates its Earth/Mars case to require two separate 2,208-triangle production meshes with no fallback. Borrowed assets must survive disposal. The added exact-identity/survey case rejects unsurveyed Earth/Mars, lookalike `planet.earth`, unbound `planet.jupiter` and wrong-case identity. Actual execution belongs to QA's dedicated `--hobbyds9-sol-asset-tests` run; source compilation is separate evidence.

## QA receiving

Receive the final integration SHA in QA's isolated worktree, after the current refresh build is finished. Run the existing Sol graphics/input fixture and compare System selection, opened Planet portrait and exit/re-entry for Earth, Mars, an unbound resolved world and an unsurveyed body. Verify actual imported unit bounds, UV north/seam, outward normals/winding, no missing/pink materials, shader lighting, identical control/marker positions, double-click/focus ownership and clean resource teardown. A fallback must fail the delivered-mesh gate.

Capture at the agreed small/full-HD/ultrawide targets where available, recording game target, window pixels and DPI separately. Compare current material appearance with the source's direct-light references without assuming Cycles/AgX matches URP. No native run, new player build, GPU memory/frame-time measurement, artistic acceptance, hull replacement, hidden-body disclosure, rule, save or schema change is established by this receiving document.
