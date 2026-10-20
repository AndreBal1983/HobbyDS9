# M1 irregularity correction / geometry comparison

20 October 2026. Owner requested more pronounced irregularity after the first maps still appeared square in their distribution. This bounded correction compares **actual** Primary v1/v2 outputs at identical seeds and framing. It preserves the original [d5c0637 art bundle](E:/HobbyDS9/artifacts/art/generated-galaxy-m1-20261020-v1/review.html), earlier source, packages and saves.

**Latest owner clarification:** geography is intended to be influenced by stellar mass, black holes and nebulae. **Neither v1 nor v2 implements that causality.** V2 is still geometry-first scattered placement followed by Voronoi regions and graph creation. The evidence here demonstrates a geometric improvement only; it is **not final M1 design acceptance** or proof of an environment-first generator. A new environment-first contract is proposal-only pending owner direction. No invented masses, phenomena, causal overlays, sensor behavior or M2 visuals were added.

## Exact inputs and comparison

- Before: retained Primary generator sample source771a3deadbf600078ee57eff9cd29d4660138c8c, `irregular-regions-v1` / `provisional-review-30-40-50-v1`.
- After: actual correction source**2fad182e40868b4e3f3e6e99651e4618c3b277b0**, `irregular-regions-v2` / `provisional-scattered-sites-30-40-50-v2`.
- [V1 input byte/hash index](../../../art/generated-galaxy-m1/sample-index.json) and [separate V2 index](../../../art/generated-galaxy-m1/scattered-v2/sample-index.json) pin the original full documents and matched route fixtures. Original line endings are preserved by scoped attributes. All four pairs use unchanged extent/count settings.
- New versioned [comparison review](E:/HobbyDS9/artifacts/art/generated-galaxy-m1-20261020-irregularity-v2/comparison.html), [V2 state gallery](E:/HobbyDS9/artifacts/art/generated-galaxy-m1-20261020-irregularity-v2/review.html) and [interactive sample viewer](E:/HobbyDS9/artifacts/art/generated-galaxy-m1-20261020-irregularity-v2/index.html). The prior stable bundle remains untouched.

Both before and after screenshots were rendered with **the same source hashes, viewport, Fit transform rule, palette and label rules**. No points were jittered, removed or rescaled independently by the frontend. Identical top-down extents remain rectangular; internal site distribution changed. The original stable bundle remains the historical baseline; the comparison also retains newly rendered v1 reference frames using the same picking fix as v2.

## Visual assessment

The four same-seed pairs show reduced horizontal/vertical row impression and noticeably greater region-size/shape variation. Larger sparse patches and closer groupings replace the one-site-per-grid-cell rhythm. The irregular boundaries remain readable; overview deliberately suppresses most names, while mid/close reveal collision-checked names without shifting actual stars. The outer rectangular footprint and convex Voronoi cell vocabulary remain; this is not a fractal/concave-region or physical-feature model.

| Seed / systems | Cell-area P90/P10, before → after | Area coefficient of variation, before → after |
| --- | --- | --- |
|42 /40|1.47 →2.76|0.149 →0.379|
|43 /40|1.55 →2.59|0.156 →0.423|
|12345 /30|1.46 →2.29|0.166 →0.321|
|20261020 /50|1.52 →2.80|0.161 →0.366|

These are **descriptive appearance diagnostics**, not balance rules or acceptance thresholds. P90/P10 compares the90th and10th percentile cell areas; CV is area standard deviation divided by mean. `layout-diagnostics.json` also records nearest-site spacing, a descriptive near-horizontal/vertical nearest-neighbour share, bounding-box elongation and empty-region area. Source [compare-irregularity.py](../../../tools/art/generated-galaxy-m1/compare-irregularity.py) reads real polygons/sites and never generates or edits them.

The number of empty regions is unchanged at exactly half. Their area is not fixed at half: default42/40 changes from50.0% to45.1% of the rectangular extent. It would be misleading to call v2 "more empty" by quantity; the observed improvement is **less uniform distribution**, with more varied areas and spaces. There are still exactly30/40/50 actual systems; no decorative point is counted as a system.

## Data identities and persistence boundary

| V2 seed / systems | Canonical content hash |
| --- | --- |
|42 /40|`b7f6c3041a330eac4dee6cc2e2e1ad3cfb0e6b77d3c81351835d6a1e6cf0938e`|
|43 /40|`6f2ae2943bb9dd9e69425dbe5956cbca93958e8e3837d8958414c12aa17b3a9c`|
|12345 /30|`e86eed4273cfd59419844a6bfb83ccbc86d59b5d825a5306b74bb924c072e781`|
|20261020 /50|`25a1cc65053574d71648097c6a5d97e4832e5d9112ebb33e51aae71a20b84a25`|

Primary reports bounded global scattered placement with40,000-unit minimum site spacing and15,000-unit margin, preserving extents, counts, exact clipping and explicit unit graph costs. Those values remain provisional generator tuning. The units are presentation coordinates, not physical distance or travel cost. This art handoff does not reimplement or approve those generator rules.

Format1/profile/API shapes remain unchanged. Current Generate emits v2; Primary retains the exact v1 algorithm for Open and exposes `GeneratedGalaxyIdentity.Supports(generator,tuning)` for the two exact supported pairs. Unknown/mixed pairs reject. **Old exports must not be regenerated or remapped into v2**, and map-scoped IDs/selection must not transfer merely because a string ID repeats. These are Primary's persistence contracts; this art pass does not claim to execute portable Open or native receiving tests.

## Legibility correction and verification

The denser actual v2 layouts exposed one practical picking problem: a candidate bracket stroke could cover a neighbouring star centre at1280×720. The art viewer now places star targets above candidate bracket strokes and offers **Candidate details** from a candidate star's inspector. Exposed bracket corners and keyboard selection remain available. **No geometry, spacing, marker position or node association changed.** Secondary was informed to preserve equivalent native semantic priority.

Executed evidence:

- 39 fresh v2 captures across13states at1280×720,1920×1080 and3440×1440; three resolution-specific interaction groups pass with zero page errors. Matching39v1 reference captures use identical renderer hashes for the A/B comparison. This rerender was needed for the picking correction and exact matched framing; no productive backend suite was duplicated.
- Default V2 rendered-data parity: all 80 polygons / 449 vertices, 40 system centres and two candidate markers match the supplied document under the declared transform. Exported JSON remains identical to its input. Source/image hashes, keyboard focus and gallery loading checks pass.
- All 160 system centres and all eight candidate brackets across the four v2 samples were picked successfully at1280×720. This includes the previously obscured neighbour; no forced click bypass was used. Reports: `review-checks.json`, `parity-checks.json`, `scattered-picking-checks.json`.
- All four same-seed comparison images and default narrow overview/mid/close were visually inspected. Irregularity and natural space variation are improved, while owner acceptance of the intended causal geography remains outstanding.

No Unity/C#/USS, backend source, active package, save, latest pointer, owner desktop, Blender or GPU operation changed here. Native/build and consequential generator/version review belong to their assigned workers. Art stops after this separately versioned geometry-evidence delivery; no phenomena proposal is portrayed as implemented and no later-stage work begins.
