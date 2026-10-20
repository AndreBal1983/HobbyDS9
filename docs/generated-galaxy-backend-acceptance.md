# Generated galaxy M1: backend acceptance

20 October 2026. Scope: portable generated-map review only. Implementation commit `771a3deadbf600078ee57eff9cd29d4660138c8c`, based on protected playable commit `cc8578c17e919dacaa2975b5cc757bee1bc8befc`. This acceptance record is a separate documentation change; generator source, tuning, samples and tests are unchanged.

[Draft PR72](https://github.com/AndreBal1983/HobbyDS9/pull/72) contains the [API/geometry contract](generated-galaxy-backend-contract.md) and [actual map/route samples](samples/generated-galaxy-v1/README.md).

## Completed backend checks

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
