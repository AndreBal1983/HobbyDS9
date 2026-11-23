# Generated galaxy M1 — Unity review entry

20 October 2026. Implementation candidate for owner review. Generated geography only; sensors, exploration progression and responsive AI are later milestones. Do not promote this candidate to a default campaign or advance automatically to M2.

## Source and authority

The isolated `codex/generated-galaxy-m1-frontend` branch starts at protected fleet source `cc8578c17e919dacaa2975b5cc757bee1bc8befc`. It incorporates portable contract `0fee81e`, generator implementation `771a3de`, and Art tokens `a1a2fa2` with their histories. See the [portable contract](generated-galaxy-backend-contract.md), [real sample documents](samples/generated-galaxy-v1/README.md) and [presentation handoff](art/generated-galaxy-m1/README.md).

The menu opens a separate full-map review without replacing the active fleet campaign or its unsent edits. Generate uses `GeneratedGalaxyApplication`, the existing admitted civilization catalogue and the requested seed/system count. Candidate starts use the existing Federation and Cardassian definition identities; they are unoccupied candidates, not borders, colonies or contacts. Bodies/resources are not generated.

Unity draws the supplied centres and polygon vertices with a uniform presentation transform. It never generates substitute regions, derives movement cost from pixels or runs a second pathfinder. Route previews call `FindRoute` and include the origin plus the returned steps. Same-node queries are valid zero-cost previews. No fleet command is issued.

## Player path

1. Choose **Generated galaxy review** from Start / Continue.
2. Enter an unsigned whole-number seed, select **30**, **40** (default) or **50** systems, then **Generate**.
3. Click a star, region or candidate bracket. Inspect actual identities, empty regions and graph neighbours. The region picker provides keyboard access to every region.
4. Drag empty space or use arrow controls to pan; wheel or +/− to zoom; **Fit** restores the whole map. Labels remain screen-sized and become more detailed as zoom increases. Ordinary graph links start hidden and have a separate toggle; region outlines and selected routes remain visible.
5. Choose **Use as route start**, then right-click another region/star, or select it and choose **Preview route here**. Inspect graph cost and route steps; this is not an ETA or movement order.
6. Choose map slot 1–3 and **Export**. **Open** validates and reopens that separate document. Back to menu retains the review locally and leaves the fleet campaign intact.

Generating/opening over unexported work requires confirmation. Failed generation, export or open retains the current map. Successful replacement resets selections and routes because node IDs are scoped to a particular map identity.

## Persistence boundary

The Unity host uses a separate `HobbyDS9GeneratedGalaxyReviewOnly-v1` directory with `generated-map-1/2/3.json` slots. No campaign save namespace, package or latest-validated pointer is changed. Strict JSON decoding enforces a 2 MiB limit, depth 16, required fields, bounded arrays, exact scalar types, and rejects duplicate/unknown properties. Portable `Open` verifies the full versioned result before replacement. Atomic writes preserve a previous export on failure.

Generated data remains an isolated future campaign input. Export does not publish a campaign, create fleets or activate AI. Visibility is intentionally the full generator output, not actor-filtered knowledge.

## Acceptance status

Initial receiving checks: **241 adapter tests passed**, including 20 new generated-map host/viewport/codec/binding cases. The three actual samples match Primary's content hashes and route data through the real Unity-compatible codec. Tests cover failed/corrupt/missing file replacement, overwrite protection, map-size preservation, seed precision, same-node routing and viewport bounds.

Unity reference compilation, actual Editor control/render acceptance, native input and strict Windows IL2CPP delivery must be reported separately when run. A source-level test or textual layout is not rendered acceptance. The dedicated `GeneratedGalaxyEngineTests` exercise displayed controls, three seed/size pairs, exact counts/selection/routes, candidate and empty-region inspection, export/reopen, failure retention and original campaign identity at 1280×720, 1920×1080 and 3440×1440.

Build target `GeneratedGalaxyBuild.BuildWindowsGeneratedGalaxy` writes a new isolated output under `artifacts/generated-galaxy/Windows`; it does not overwrite the protected fleet package. Final source review, exact CI, resource lease, input provenance and a new versioned deliverable remain required.

The first actual receiving run at `a0f7e84` passed one control/render test and captured 39 images. It exposed pale input text on light default fields during visual review, so it is **not visually accepted**. Its import log also records absent classic resources because only review/profile preparation had run. The follow-up uses `scripts/PrepareUnityGeneratedGalaxy.proj`: base preparation, then Demo (including Integrated/Review dependencies), Frontier and PlayableFleet, before snapshotting inputs and opening Unity. This preserves exact source-package bytes and includes the classic resources.

The separate correction styles inputs/scroll controls, makes graph links optional/default off, and extends actual engine checks for polygon picking, drag, wheel, Fit, raw keyboard/modal exclusion, stale controls, current campaign draft retention, displayed marker alignment and painted route segments. Its results must be recorded when executed; authored assertions alone are not evidence.

The previous fleet candidate's unfinished native aftermath/save-restart/audio checks remain unfinished; generated-map work does not imply their completion.

## Marker-origin correction candidate

The frozen `471922d` receiving result remains one pass and one failure: marker distance 2.0712347 pixels against the unchanged less-than-two-pixel requirement. Its evidence is retained. The correction fits the viewport to the content rectangle, adds its local origin for painter/pointer positions, and subtracts the actual parent border when placing absolute children. Wheel anchors use the inverse conversion. This addresses the border-box versus child-containing-box inconsistency; actual pixel-rounding residuals still require the new engine receipt.

Stars are placed above candidate corner hit targets. Starting candidates remain accessible through the selected star/region inspector as well as their corners. The engine fixture records per-axis marker errors, resolved geometry, borders and padding at all three sizes, saves screenshots before assertions, and exercises actual panel picking with an overlapping candidate target. The threshold is unchanged. Eight pinned-reference compiler outputs pass; runtime/editor review outputs have no warnings, while retained unrelated default/test warnings remain. Engine, native and package acceptance are pending for this correction.

Actual correction receiving at `6474690913b25204d76664013810036663583763`: two engine cases passed, zero failed/skipped; 42 screenshots and 360 marker measurements across three sizes and seeds. Maximum distance was 0.7047837 pixels, with observed one-pixel borders and content-box offsets. The unchanged less-than-two-pixel threshold passed; this is consistent with removing the border offset and retaining ordinary layout rounding. All 553 prepared input hashes were restored, four automatic settings changes archived, no extras, process absent and engine log exit zero. Frozen external evidence: `E:/HobbyDS9/artifacts/generated-galaxy-receiving-6474690-20261020`, 615 payload hashes, manifest SHA256 `6f84f3e69b5f3ef4bb43ac9d80a2b300feeb0f3d9f6a1efc9839e5b993b5fce0`. The new untracked `.vsconfig` is preserved. Native input and package acceptance remain unclaimed.

## Environment-first integration candidate

The [format-2 contract](environment-galaxy-m1-contract.md) supplies actual stars, black holes, nebula influence ellipses and sampled region density provenance. The host dispatches by outer format version, validates the complete respective document through its portable Open API, and retains that authority for export/routes. A separate detached presentation projection shares map geometry access; it is never serialized as a legacy document or validated through legacy Open. Format-1 v1/v2 remain separate, unchanged maps.

Stars render at persisted feature positions. Features shows black-hole/nebula presentation, while system stars remain inspectable; Influence areas shows scaled generation ellipses, and Density causes displays persisted base/contribution/capped target values. These display controls do not change generation switches or content hashes. Feature inspection is available through markers, system/contribution buttons and the feature picker, preserving access under overlaps. Density explains sampled placement pressure, not unique polygon causation or an inverse-area formula. No hazard, sensor or campaign effects are introduced.

The candidate integrates authoritative implementation `e37e2e2` and its actual samples. All 254 adapter tests passed with zero skips, including three new default-format hashes, full feature-layout roundtrip, seven committed v1/v2 exports and failed format/feature/contribution/malformed-load isolation. The first focused run retained a real failure (30 passed, one failed): `InvalidDataException` escaped the workspace handler. Explicit Open/Export handling now preserves state and reports failure; the failed TRX remains in evidence. One overlapping CPU compilation attempt conflicted on shared outputs and ran no tests; its log is retained, and the complete passing suite ran serially afterward.

Eight pinned-reference compiler outputs pass (retained unrelated warnings documented in their receipts), followed by a successful focused ReviewTests compilation after adding actual exterior-pixel clipping checks. Influence ellipse tessellation is clipped against the exported map rectangle, not only the viewport; raw geometry remains unchanged. Inspector selections reset scrolling to the heading. Three-size engine rendering, independent review and exact CI remain gates before a package. Marker-only evidence above does not stand in for environment acceptance.
