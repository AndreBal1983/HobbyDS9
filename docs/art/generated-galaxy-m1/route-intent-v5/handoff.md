# Route intent and natural distribution / candidate evidence

23 October 2026. **Delivered for review:** actual API fixture viewer, 36 route captures at three viewport sizes, independent raw/API/rendered-SVG checks, and six matched old/new natural-distribution captures. **Proposed:** presentation defaults. **Open:** native frontend acceptance, final visual route acceptance and separately proved corridor straightening. No campaign, gameplay, native executable, save, Unity or GPU operation was changed.

## Route result and remaining visual issue

The retained format3 seed42/250 map is `006ab67d08f1cd3c3d9e73da2fa335fb5e4ba1cee1acd824d63dbd44135c1937`. Route implementation is `c936130740c66c90efd235474de8f6152d754657`; examples are exact blobs from `f8d72f8eed9f72c0578071264bf608ca8f891a19`. Comparison.Query uses nav.scaled.0015 to SystemLocation nav.scaled.0189 / system.scaled.0189 with the full explicit filter and policy minimum-crossings-centre-span-v1. Old and new routes both cost22; centre-span proxies change from4,123,357 to3,139,347. That proxy is neither visible-line length nor travel time.

**Visual acceptance is not passed.** In `routes/standard-new-policy-transit.png`, the path bends down toward the bottom boundary before turning right and back toward the destination. A further central jog is visible. Removing automatic centre returns helps, but midpoint doglegs remain conspicuous. Do not label this the straightest or final visually logical route. Root is separately scoping proved ordered-corridor straightening; this art increment adds no solver, string-pulling or rounding.

The route gallery separates two comparisons: legacy/new route choice with transit drawing held fixed, and centre-return/transit drawing with new corridor held fixed. The same map and camera are used. Every successful leg preserves explicit anchors; A→B→A retains B and two separate lines, and A→B→B→C keeps the zero-length leg. A blocked middle-leg fixture keeps user rows but clears all lines and partial totals. Normal right-click replaces; Shift appends; remove-last and clear are exercised against supplied API cases or explicitly labelled returned successful-leg prefixes. Arbitrary drafts have no supplied result and are not searched locally.

Run2 reused36 completed captures,21 DOM observations and three viewport checks, with no browser errors. The corrected narrow inspector scrolls only its target list, leaving total/actions and start visible. Independent verification checked33 legs,432 portals,396 contained segments and561 actual rendered SVG points against exact raw polygon boundaries, API snapshots and explicit anchors. Tolerance is only for floating projection roundtrip; raw edges/midpoints use exact doubled integers. These are browser/source checks, not native acceptance.

## Actual natural distribution comparison

The natural runtime is `5d83888791a0c4ca6728627ed6cad01567313ff8`. Seed42/43 actual250 samples use that pin; actual1000 seed42 samples use `930b1adb34756be9bf3b605da56d6b0827902708`, with runtime unchanged. Old raw documents retain the scale-v4 provenance, including the earlier seed42/250 report at17725f7 rather than incorrectly attributing its exact bytes to aa434. The exact original browser input is frozen as natural/capture-index.json; natural/comparison-index.json clarifies per-file document/report pins afterward. Measurements and rendered inputs did not change, so the green six captures were reused.

| Seed / size | Version | Quiet / disturbed | Quiet/system mean area | Quiet components / largest | Crossings median / P90 / max | Actual NN isolated ≥120k |
| --- | --- | --- | --- | --- | --- | --- |
| 42 /250 | Old |132 /118 |2.1171× |19 /41 |9 /15 /22 |0 |
| 42 /250 | New |133 /117 |2.3024× |17 /38 |9 /15 /23 |25 |
| 43 /250 | Old |121 /129 |2.1238× |21 /23 |9 /15 /22 |1 |
| 43 /250 | New |115 /135 |2.7254× |17 /27 |10 /15 /23 |25 |
| 42 /1000 | Old |538 /462 |2.3204× |50 /273 |17 /27 /42 |1 |
| 42 /1000 | New |489 /511 |2.7101× |53 /94 |17 /27 /42 |100 |

New maps show varied loose associations and solitary stars; repeated three-star patterning is visibly reduced. Dense overlapping associations remain in places, which is a natural candidate characteristic rather than proof that every provenance group is visually separable. All new maps have provenance arities2–9. Group membership covers each supplied star once; these are not measured nearest-neighbour connected components. Independent brute-force nearest-neighbour records cover3000 stars across all six maps and match every new backend row. Independent exact shoelace areas match quiet, disturbed and system means. Full means, candidate routes/costs, per-file hashes and identities are retained in comparison-index.json and paired raw reports.

All six views use the same drawing grammar and linked camera for both versions. Overview displays every supplied star, and central3× detail uses the same world crop. Actual DOM coordinates are checked against raw source positions and the recorded camera; no stars or regions are moved, jittered or fabricated. Extents/counts/seeds match while geography, identities, candidate starts and route endpoints legitimately differ. Cross-version route cost is not a controlled same-corridor comparison. Quiet area is site pressure, not whole-cell environmental coverage; the new soft2× target overshoot is explicit. Neither version establishes a galaxy-spanning quiet corridor.

## Local review and reproduction

Stable delivery is `E:/HobbyDS9/artifacts/art/galaxy-route-natural-v5-20261023-1/review.html`; original run evidence remains in this managed worktree under artifacts/art/route-intent-v5-run2 and natural-v5-comparison-run1. Safe913 and prior scale-v4 galleries/packages are preserved.

Sources are [route viewer](../../../../tools/art/route-intent-v5/view.js), [route verifier](../../../../tools/art/route-intent-v5/verify.py), [natural source/measurement preparer](../../../../tools/art/route-intent-v5/prepare-natural.py) and [matched browser renderer](../../../../tools/art/route-intent-v5/render-natural.cjs). The natural preparer reads exact committed blobs and never invokes generation or changes coordinates. Use separate new output folders for any justified future capture. No old54-capture batch or native test was rerun.

Before treating this as final UX, resolve the explicit visual dogleg finding and separately accept the real frontend's input/start/persistence behavior. The read-only fixture artifact cannot establish those runtime contracts.
