# Generated galaxy M1: backend acceptance

Historical format1 v1/v2 record. The later owner-authorized environment-first implementation and its current gates are recorded in [environment-first backend acceptance](environment-galaxy-backend-acceptance.md). The causal gap and design-alignment hold below describe the earlier scaffold; that hold was superseded by the owner's explicit environment-first M1 authorization.

20 October 2026. Scope: portable generated-map review only. Current owner-requested layout correction: `2fad182e40868b4e3f3e6e99651e4618c3b277b0`. Original implementation: `771a3deadbf600078ee57eff9cd29d4660138c8c`, based on protected playable commit `cc8578c17e919dacaa2975b5cc757bee1bc8befc`. This follow-up acceptance record changes documentation only.

[Draft PR72](https://github.com/AndreBal1983/HobbyDS9/pull/72) contains the [API/geometry contract](generated-galaxy-backend-contract.md) and [actual map/route samples](samples/generated-galaxy-v1/README.md).

## Current v2 scattered-layout correction

The owner found the original region distribution too square/row-like. V2 samples sites across the full map without grid slots, retaining minimum spacing and bounded work. Exact polygons, adjacency, costs, counts, empty regions and campaign isolation remain portable authority. Placement tuning is still **provisional**. The budget is pooled `requiredSiteCount * 256` draws per complete attempt, at most8attempts, not a per-site cutoff.

Generate now emits `irregular-regions-v2` / `provisional-scattered-sites-30-40-50-v2`. Open retains the exact original v1 pair and layout; mixed/unknown pairs fail without replacement. Format1/profile/DTO shapes stay unchanged. Original v1 sample bytes/hashes and all campaign identities remain intact. [Four v2 map/route samples](samples/generated-galaxy-v2/README.md) support same-seed comparisons42/40,43/40,12345/30 and20261020/50 with identical Art framing.

Exact correction validation: Release zero warnings/errors; **1,910 .NET tests passed**, including48generator cases; **546 portable client tests passed**; PrepareUnity and static integration passed. No failures/skips. The96representative maps now additionally verify center margins/minimum pair spacing and loss of one-site-per-grid-cell placement. New tests retain original v1 hashes/full reopen equality, pin four separate v2 vectors, compare sample area variation and reject mismatched algorithm/tuning pairs. All eight actual old/new comparison exports reopened with expected hashes. Old playable canonical/reverse/repeated/save-reload hashes below are unchanged.

Independent review approved exact `2fad182` with no consequential blocker. Its delta addendum is in the same local review report cited below. Retained correction receipts: `E:/HobbyDS9/artifacts/generated-galaxy-backend-2fad182-20261020/` (full TRX, build/client/static/determinism logs, eight-sample reopen log and manifest). Manifest SHA256: `ca00d25ce8cfa8afa5318cd4e24dbe8724398ff0db6eaacb140cadc0e97b92c2`. Independent retained-bundle verification is pending at this documentation checkpoint; source approval is separate.

[Correction CI run39476734179](https://github.com/AndreBal1983/HobbyDS9/actions/runs/39476734179) was queued at last query. Final documentation-head CI is tracked in PR72. Neither source review nor these local measurements establishes owner visual acceptance, combined frontend compatibility, Unity/AOT or native acceptance. Those gates remain separate; no M2 work.

**Known causal design gap:** v2 remains a geometry-first scaffold: sites, regions and adjacency are generated before systems are assigned. Stellar features, black holes and nebulae do not influence placement or topology. It does not yet implement the agreed feature-driven relationship between sparse space, dense/disturbed space and crossing density in [time/movement design](design/time-movement-mobilisation.md). Source approval covers the bounded implemented scaffold, not owner M1 design-complete acceptance. Final build/delivery clearance is held for design alignment. An environment-first follow-up needs an agreed feature/placement contract and versioned persisted feature inputs; no influence formulas, hazard effects or new features were implemented in response to the architecture question.

## Original v1 backend checks

| Check | Result on implementation commit |
| --- | --- |
| Full Release build | Zero warnings/errors |
| Full .NET suite | 1,901 passed; zero failures/skips |
| Generated-map coverage | 39 cases, including three many-seed cases covering 96 maps |
| Separate portable client suites | 546 passed: adapter221, UX115, travel89, shell23, client51, storage47 |
| PrepareUnity Prepare | Passed; no Editor execution |
| Unity static integration | Passed |
| Sample persistence | All three tracked map exports reopen with exact canonical identities/layout |
| Existing playable determinism | Canonical/reverse/repeated/save-reload equality; original hashes unchanged |

Generation checks cover all three supported sizes, genuine empty regions, connected unique adjacency, system associations, positive convex cell areas, complete shared boundaries, independent pairwise separating-axis non-overlap, separated candidate homes, stable routes, both unsigned64 seed endpoints, repeated/reordered inputs, detached export isolation and failed-open isolation. Reference host JSON tests reject missing, duplicate, unknown, malformed and oversized input. Three new generator hash vectors are explicit version1 baselines; no existing campaign golden was changed.

The existing playable package hash remains `87e57e759ad9ddbe0eebcecef86fb04a9c55c025ea28031205ac323c2c9c41c7`; its walkthrough save hash remains `f63317e1aa708b6e6c5bcb278c0d9aecfa17ca264f94290013a1b4f6a9d9cec5`. Save14, wrappers1–6, Content10/11, science-engineering-v1 and ship-design-compiler-v1 remain unchanged.

## Independent review and retained evidence

Independent source review approved exact implementation `771a3de` for this bounded portable scope, finding no consequential blocker. The reviewer inspected the retained test receipts without rerunning suites and verified all14 evidence-file SHA256 values, with zero mismatches.

Local retained paths:

- `E:/HobbyDS9/artifacts/generated-galaxy-m1-backend-review-20261020.md`: source assessment and evidence-verification addendum.
- `E:/HobbyDS9/artifacts/generated-galaxy-backend-771a3de-20261020/`: full TRX, build/client/static/determinism logs, source-pinned manifest and optional seed43/40-system sample with matched route.

The immutable manifest SHA256 is `240ff6c5bc74e577b1e2fc1d61d3c661c57dda84d2c71a48bbdb12ff26b1abc0`. Its review/CI status fields describe the delivery-time snapshot, not later review approval. Do not rewrite preserved evidence to update status.

[Implementation CI run39471656570](https://github.com/AndreBal1983/HobbyDS9/actions/runs/39471656570) was still pending at the last recorded check. Final documentation-head CI must be tracked separately in PR72; this record does not claim cross-platform completion.

## Limits and next checkpoint

Generation/document format1 is separate from campaign saves. It persists settings, supported generator/tuning identities and full generated data; unknown versions fail without migration. Coordinates are presentation units, not travel costs. Twice as many regions as systems, unit-cost adjacency, extents and candidate separation are **provisional review tuning**, not approved campaign balance.

Frontend integration must still verify its own strict codec, atomic export/reopen, unsaved-map replacement intent, selection invalidation, actual route/inspection parity, engine/AOT compatibility and readable controls at the three required resolutions. Backend tests and source approval do not establish Unity/native or visual acceptance. No Editor, native player, desktop input or GPU work was performed for backend acceptance.

Existing packages, saves and latest-validated pointer remain protected. No main merge or default activation. Deliver the combined M1 review build through the coordinator, then stop for owner review. Sensors/reacquisition, exploration progression, responsive AI and campaign activation remain later milestones.
