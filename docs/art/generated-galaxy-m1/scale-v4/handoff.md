# M1 scaled geography / art delivery

23 October 2026. **Delivered:** actual250-system review followed by actual1000-system stress presentation, exact shared-edge portal routes, semantic labels, indexed picking, culling and source-backed quiet-space inspection. **Proposed:** visual thresholds and styling. **Open:** separate Unity/native performance/input acceptance and owner review. No campaign behavior or M2 work.

## Review and source

Start with the new [scale review](E:/HobbyDS9/artifacts/art/generated-galaxy-m1-20261023-scale-v4-1/review.html), then its250gallery before1000stress views. Existing `e47fcc9` environment gallery and all earlier immutable bundles remain preserved. The ZIP, payload manifest and delivery receipt are beside the entry.

Actual data are pinned to **`aa434036462c08eceb323aa581e539d307ca65e0`**. The250payloads are unchanged from `17725f727ddcfc2c03f6a67601f00cfe8f232aa8`. [Sample index](../../../../art/generated-galaxy-m1/scale-v4/sample-index.json) pins six raw map/report blobs copied directly from committed Git bytes. [Per-file provenance](../../../../art/generated-galaxy-m1/scale-v4/source-provenance.json) records that the seed42/250report retains its exact17725f7bytes: aa434 later changed timing/failure-probe metadata, while Settings, Tuning, Route, ContentHash and all Diagnostics remain identical. The three actual map documents and other reports match aa434 exactly. The renderer input index and54captures remain unchanged. Reports contain the actual API routes and measured quiet/disturbed/component identities. No generator or pathfinder is implemented by Art.

| Actual sample | Canonical hash | Quiet-empty/system mean area | Quiet / disturbed empty | Quiet sets / largest |
| --- | --- | ---: | ---: | ---: |
|42 /250|`006ab67d08f1cd3c3d9e73da2fa335fb5e4ba1cee1acd824d63dbd44135c1937`|2.1171|132 /118|19 /41|
|43 /250|`56fdc0846cbcc613fc8bc384deac8fe01d6b23bf4b52edeedd48ea529ab155ab`|2.1238|121 /129|21 /23|
|42 /1000|`b7a10c946954cfdf51a28e90bccda98ee8a1d4dcbd6e35c23c811fd94878a834`|2.3204|538 /462|50 /273|

These are soft, seed-dependent generation outcomes, not an exact area rule for each cell. Quiet classifies sampled site pressure and excludes black-hole anchors and sufficiently strong enabled black-hole/nebula contributions. It is not whole-cell environmental coverage, actor knowledge or a guaranteed galaxy-spanning corridor. [Independent diagnostic parity](../../../../art/generated-galaxy-m1/scale-v4/diagnostic-parity.json) matches all three reports' classifications, component membership, means and tiled area.

## Presentation handoff

Use [presentation contract](presentation-contract.md), [tokens](../../../../art/generated-galaxy-m1/scale-v4/tokens.json), [browser renderer](../../../../tools/art/scaled-galaxy-m1/map.js) and [exact-edge helper](../../../../tools/art/scaled-galaxy-m1/scale-geometry.js). Source remains in Art's isolated scale-v4 checkout; no runtime C#/USS or portable files changed.

- One connected5:4extent, uniform Fit and honest letterboxing. Original crooked corners, small positive shared edges, stars, boundaries and costs remain unchanged. Culling affects off-screen presentation only.
- Semantic zoom uses the typical projected visible-region size. System-name caps start at12/36/80 with viewport and collision limits. Close stars remain actual points; zoom separates their names and targets. Normal boundaries fade at overview instead of being regularized or removed from the graph.
- Indexed nearest-star picking uses the actual received pointer coordinate, independent of marker paint order. Stars keep priority over candidate/feature strokes. Candidate/Environment actions provide alternate access; local zoom resolves overview ambiguity.
- Draw each accepted hop as centre→exact shared-edge midpoint→next centre. Doubled midpoint coordinates retain halves. No epsilon, snapping, minimum-edge filter, corner-only links, new graph steps/costs or unconstrained smoothing. Portals are drawing vertices. The measured API routes remain22 and42crossings/cost respectively.
- The250quiet-set highlight matches the largest report component's exact41member regions. The inspector discloses fragmentation. The1000component data are independently checked but its highlight is not a separate screenshot acceptance claim.
- Selected influence shows only the selected feature's real clipped ellipse, avoiding an unlabeled1000-star outline web. Full feature/region data remain in the exported document. Features do not imply sensors, hazards or ownership.
- Browser pan does not select label text. Candidate subtitles stack below relocated system labels. SVG updates use fragments. A stale drag-suppression flag found during testing was corrected so the next deliberate pointer-down can select normally.

## Executed evidence

The final source produced **36captures at250** and **18at1000**, at1280×720,1920×1080 and3440×1440. After the owner's reboot, all54capture hashes, both index pins and four renderer source hashes matched. They were reused; no screenshots were repeated for recovery. Earlier failed/intermediate runs remain outside the final gallery and partial verification files are retained.

Recovered focused checks pass at all three sizes:

- 250:144dense overview pointer checks,750all-centre nearest-model checks, exact500polygons/vertices/stars, complete format3 export equality, keyboard pan, missing-fixture retention and gallery loading.
- 1000:72overview pointer checks against an independent nearest calculation from **received** mouse coordinates,3000all-centre model checks and the same polygon/export/control checks. Four requested subpixel targets legitimately resolved to a different nearer star after browser coordinate quantization: three at1280 and one at1920. Both affected views then passed keyboard→Centre→zoom→actual-pointer selection of the originally requested star. No retargeting occurred in the tested3440cases. This is not a claim that every star has a unique overview pixel.
- Screen-marker error stays below0.00012px in these browser checks; the existing native tolerance is unchanged. Browser alignment does not establish native Unity alignment.
- Independent integer geometry proof checks every1437/5880graph edge has exact shared endpoints, then validates every rendered route vertex against the raw polygons. Each display proves22/42portals, retaining18/35half-coordinate midpoints and44/84convex intra-cell segments. Across the six views:192portals and384segments, with API node sequence and cost unchanged.

The initial1000test incorrectly expected a requested fractional mouse coordinate to reach the browser unchanged. Its failed receipt and exact trace remain preserved: requested `(583.86581025,341.6374715)` arrived as `(583,341)`, selecting the nearer actual star. The corrected check computes the expected answer independently from that received coordinate and separately verifies zoom disambiguation; it does not relax geometry/alignment thresholds or change the renderer.

The portal comparison uses one actual250hop (`nav.scaled.0013`→`nav.scaled.0277`). Its left panel **reproduces the previous chord drawing rule**, not an old executable screenshot; exact rational line intersection proves that chord crosses the shared-edge line outside its finite segment. The right panel uses the real midpoint, with unchanged cells and graph cost1.

Representative final overview, quiet-space, portal-close and local views plus the comparison diagram were visually inspected. Synchronous Fit-handler observations were4.3ms at250 and16.8–20ms at1000 in this CPU/headless check; these exclude paint/input and are not frame-rate or Unity-performance acceptance. Screenshot automation was slow before the reboot; no production performance guarantee follows.

## Reproduction and stopping point

[render.cjs](../../../../tools/art/scaled-galaxy-m1/render.cjs) takes index, fresh output directory and250or1000. [verify.cjs](../../../../tools/art/scaled-galaxy-m1/verify.cjs) takes the same plus a fresh receipt directory and saves per-viewport checkpoints. [verify-portals.py](../../../../tools/art/scaled-galaxy-m1/verify-portals.py) checks those receipts with exact integer arithmetic. [verify-diagnostics.py](../../../../tools/art/scaled-galaxy-m1/verify-diagnostics.py) checks report facts; [compare-portals.py](../../../../tools/art/scaled-galaxy-m1/compare-portals.py) creates the source-backed diagnostic with Pillow. Chrome runs headless with GPU disabled. No technical installations, paid tools, Unity, Blender or desktop input were used.

Source/payload hashes and limits are in the delivery receipts. Root owns independent backend/CI review and the separate native game package. Existing source/saves/latest pointers remain intact. Stop for M1 owner review; no new default activation or subsequent milestone work.
