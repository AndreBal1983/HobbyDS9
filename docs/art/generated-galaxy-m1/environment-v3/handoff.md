# Environment-first M1 / art delivery

Visual work 20 October; delivery frozen 23 October 2026. **Delivered:** actual-export browser art review, reusable presentation tokens, three-size captures and controlled feature comparisons. **Proposed:** palette, footprint contrast and inspector styling. **Open:** native Unity/render/input acceptance and owner review. This is M1 map presentation only.

## Review entry and source

New stable [controlled comparison](E:/HobbyDS9/artifacts/art/generated-galaxy-m1-20261023-environment-v3/comparison.html), [63-capture gallery](E:/HobbyDS9/artifacts/art/generated-galaxy-m1-20261023-environment-v3/review.html) and [interactive actual-sample viewer](E:/HobbyDS9/artifacts/art/generated-galaxy-m1-20261023-environment-v3/index.html). The ZIP, payload manifest and delivery receipt sit beside them. Earlier v1/v2 deliveries, source viewers, packages and saves remain preserved.

Inputs are exact committed Primary blobs from **`e37e2e2e376c24b43d279089baede49b94d30f6e`**, following the exact format-2 contract at `f6316e18b90b32e68dd81354360dfeb7d4390e3b`. [Sample index](../../../../art/generated-galaxy-m1/environment-v3/sample-index.json) pins sixteen raw map/route files plus the separate Primary causal report. No generator, polygon reconstruction or route search runs in this viewer.

Normal samples are seed42/40,43/40,12345/30 and20261020/50. Four additional seed42/40 controls disable stellar, black-hole, nebula or all influence. Default canonical content hash is `404d1e15057f42531a6f1da078efd78663d902dcff22e5484e6e3ba2cc8d68fe`. The complete raw format-2 document is retained for export; the browser uses an explicit read-only Layout adapter for rendering.

## Presentation and implementation handoff

Use [tokens](../../../../art/generated-galaxy-m1/environment-v3/tokens.json) and the [presentation contract](presentation-contract.md). The new [renderer source](../../../../tools/art/generated-galaxy-environment-m1/map.js) leaves the prior geometry-only viewer untouched.

- Render stars at persisted Features.Position, retaining their real SystemId/NodeId association. Black holes use dark cores and broken lavender rings. Nebulae use faint teal influence ellipses and diamond centre targets. No decorative feature or star is added.
- The axis-aligned ellipses use exact RadiusX/RadiusY and clip to the exported rectangular extent. They represent normalized **generation influence**, not physical radii, mass, hazards, sensor ranges or territory.
- Features, Influence areas and Density causes are display controls. Loading a separately labeled control export is the only comparison action that changes which document is shown. Visibility changes do not alter the loaded document or its hash.
- Regions retain neutral one-pixel boundaries at0.32 opacity. All geometry, markers, pointer conversion and route endpoints share one SVG content-box origin; the panel border is outside that coordinate space. Uniform Fit measures the real toolbar height and preserves letterboxing.
- Star centres take picking priority over candidate/feature strokes. Candidate details remain reachable from the system inspector. The Environment index and contributing-feature buttons provide independent feature access when centres overlap; distinct direct pointer targets cannot be guaranteed for coincident nebula centres.
- Each region's compact density summary shows its persisted BaseDensity, capped TargetDensity and actual sorted feature contributions. Contributions live in expandable detail; the summary precedes long action lists. Selection and Centre actions return the inspector to its heading. Density is a site sample explaining placement pressure, not unique attribution of polygon shape or an inverse-area rule.
- A black-hole node remains system-free navigation space. A nebula has no single route node. Candidate homes remain unoccupied candidates. Routes use the exact exported origin/steps/cost; arbitrary-endpoint search and Open remain native actions.

## Controlled causal evidence

All four comparison pairs assert identical complete Features, Tuning and map inputs, with only the stated persisted influence flags changed. Same renderer, viewport and Fit are used. The fixed region budget redistributes sites across the map, including distant cells; individual cells need not shrink monotonically.

Primary's hash-pinned causal report measures **additional sites inside a family's half-weight influence cores**, excluding mandatory star/black-hole anchors. The seed42 counts are:

| Influence family | Disabled | Enabled |
| --- | ---: | ---: |
| Stars |21|23|
| Black holes |2|6|
| Nebulae |5|8|

Primary's17-seed totals are335→415,49→106 and108→179 respectively. These are Primary's reported portable observations, not newly executed portable tests by Art. Browser comparison diagnostics separately report full-support-union counts and cell-area observations; those use a different measurement and are not acceptance thresholds. In particular, the full stellar-support count is32 in both default variants, while its half-weight-core count changes21→23. Do not conflate the two or promise an area reduction for every polygon.

## Executed art checks and limits

-63 captures:21 states at1280×720,1920×1080 and3440×1440, including feature inspections, empty black-hole region, visibility, influence areas, candidate/route, mid/close zoom, all sizes/seeds and four controls. No browser page errors.
- Default render matches80 actual polygons /451 supplied vertices /40 stars /45 total features. Polygon vertices and ellipse centres/radii are compared directly with persisted data. Overview screen-marker error is below0.00011px across these browser sizes; native Unity alignment is a separate gate and its original tolerance must not be relaxed.
- Independent verifier visits all four normal maps at all three sizes:480 actual star-centre clicks and60 independent black-hole/nebula inspections. It also matches all footprint radii/clipping, every clicked star's displayed density provenance, returned route points, keyboard focus after redraw, exact complete format-2 export equality, unavailable-sample retention, screenshot/source hashes and gallery image loading.
- All four comparison panels plus representative narrow overview/region/close and standard/wide feature views were visually inspected. Intermediate runs remain preserved; the final bundle uses run3 after inspector, Fit and extent-clipping corrections.

CPU/headless Chrome with GPU disabled only. No Unity/Blender/desktop input, active-game integration, portable-rule changes, sensor facts, ownership, damage, AI, exploration progression or M2 work. The frontend implementer owns native acceptance and the separate game review package; this artifact does not replace it.

## Reproduction

[render.cjs](../../../../tools/art/generated-galaxy-environment-m1/render.cjs) takes the sample-index path and a fresh output path. [verify.cjs](../../../../tools/art/generated-galaxy-environment-m1/verify.cjs) takes the same index and rendered output. [compare.py](../../../../tools/art/generated-galaxy-environment-m1/compare.py) takes index, rendered output and a fresh comparison directory. [package.py](../../../../tools/art/generated-galaxy-environment-m1/package.py) takes rendered output, comparison directory and a new delivery target. Node requires Playwright and Chrome; Python requires Pillow. Existing output directories reject rather than overwrite previous evidence.

Stop at M1 owner review. Production-scale performance, physical mass modeling and subsequent milestones remain separate work.
