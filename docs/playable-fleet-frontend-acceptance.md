# Playable fleet frontend — receiving candidate

20 October 2026. **Source candidate; Unity/native acceptance remains open.** The owner authorized this bounded existing-class implementation in the coordinator chat after the historical documentation-only assignment. The controlling local brief is [the playable frontend milestone](E:/HobbyDS9/docs/playable-frontend-milestone-2026-10-20.md). Broad roadmap autopilot remains paused. No main merge or latest-validated package promotion is authorized.

## Source and authority

The isolated `codex/playable-fleet-frontend` branch starts at reviewed `e3e76216c5d1033ae51614e364ccb1717506bb99`. Science allocation controls are `c0cf1dd`; map route reviews and the owned-world ledger are `7220d9a`. Backend `86fde5f537aa9b7ce2029d65d40f5ebed238372e` includes the independently reviewed operation-capacity and human-elimination corrections from `e75d788`. Read the [backend contract](playable-fleet-backend-contract.md) and [backend acceptance](playable-fleet-backend-acceptance.md).

The visual reference is UI revision `74193131bc6b22eda3fcc4e7da35876f8e38f227`, retained in the separate map-selection-prototype branch and local `E:/HobbyDS9/artifacts/art/owner-interface-revision-20261020-v2-1` bundle. Its old runtime tree was not merged. Original battle art `e104143e66c4f3cb7eca042614175b07ede27b47` was merged; see the [handoff](art/playable-battle-art-handoff-2026-10-20.md) and [exact pair mapping](../art/ships/playable-class-visuals-v1.json). Direct fetching of the owner's YouTube reference was throttled; this worker does not claim to have watched it.

## Implemented receiving source

- Separate Fleet campaign Start/Continue and format-6 save directory/slots; older demo/frontier profiles keep their own entry points and stores. Exact-byte five-file loader agrees with the backend package hash `87e57e759ad9ddbe0eebcecef86fb04a9c55c025ea28031205ac323c2c9c41c7`.
- Complete-root JSON/host/storage integration, validated before replacement. No nested-root fallback, new migration or private save inspection for rendering. Failed load/write retains the current session, local drafts and prior valid file. Saving preserves unsent research and organization drafts locally; successful load replaces them.
- Whole-percent LCARS Science allocation bars, exact locked backend weights, displayed totals of 100%, keyboard/pointer operation and complete allocation review.
- Sparse schematic map with separate system/star, fleet-ring and contact selection; known-route review and fresh-contact Follow using the exact reviewed route offer. Zoom/pan are presentation only. Stale and replacement-session offers reject.
- Empire ledger with owned facts, current FIFO construction, column sorting/filtering, system grouping and filtered totals; separate World detail and Production pages remain reachable. Foreign world data, ETA and unsupported output/deposit/power projections are explicitly unavailable.
- Named fleet roster with selected-fleet expansion, owned equipment/stock inspection, sorting by name/system/known missing hull/ship count, and reviewed create/transfer/rename. Backend validates ownership, co-location, stationary state and cleared orders. Organization drafts persist across navigation and saves, and block accidental turn advance.
- Existing per-round maneuver/focus controls use actual joint commitments. A presentation queue consumes every actually resolved round, including an AI-only remainder after human elimination. Acknowledgment/skip never resolves, finalizes, draws RNG or changes stocks. New human rounds and explicit aftermath remain gated until the current resolved playback is reviewed or skipped.
- A manual-render 3D stage uses only the displayed boundary's own/visible ships and exact identity pairs. No global later-reveal identity cache, private root, hidden mesh, guessed source or display-name class binding. Federation reuses its exact ArtDemo prefab; the original Cardassian OBJ LODs/materials/manifest anchors are receiving assets. Missing models have explicit generic markers.
- Schematic maneuver choreography uses actual before/after relations. Typed events drive weapon/ordnance, defensive, shield/hull and destruction effects. Hidden incoming ordnance originates from an edge cue, with no invented launcher. Camera, speed, pause and skip affect presentation only. Scene objects, materials, meshes and render textures are owned by the view and disposed on detachment.
- Reports show backend observed shots/hits/misses, received shield/hull damage, destruction and exact own retained stocks. Finalized history comes from the format-6 session and survives save/reload. No campaign victory is invented from the three-round limit.

This does not add classes, custom blueprint publication/refit, combat physics, resupply, hidden intelligence, strategic retreat, a 3D galaxy or gameplay power ratings.

## Executed portable checks

Evidence is in the isolated checkout's `artifacts/playable-frontend/` directory.

| Check | Recorded result |
| --- | --- |
| Locked Release solution build | Passed; zero warnings/errors |
| Full simulation suite | 1,862 passed; zero failed/skipped |
| Actual Unity adapter source | 221 passed; zero failed/skipped |
| UX / client / shell / storage / travel suites | 115 / 51 / 23 / 47 / 89 passed; zero failed/skipped |
| Total separate portable client cases | 546 passed |
| New receiving binding cases | 10 passed, included in adapter total |
| PrepareUnity / review/demo / new playable resources | Executed without launching Unity |
| Static integration and review binding | Passed after preparation |
| Eight pinned Unity-reference compiler outputs | Passed; new Review runtime/editor source has zero warnings; existing unrelated scaffold warnings remain recorded |

New binding tests execute actual UI-model paid construction, fleet assembly, lawful Follow, each human round, presentation acknowledgments, once-only aftermath and history/reload. They cover disjoint codecs/slots, failed atomic file writes, forged names, stale observations/session replacement, retained unsent drafts, eliminated-human three-round playback and per-boundary visual identity disclosure. They are not native Unity tests.

## Remaining acceptance gates

1. Independent combined-source review and exact source CI; backend-only approval/CI does not approve this receiving UI.
2. Fresh serialized Unity/native resource lease, receiving import, actual Editor tests and strict Windows x64 IL2CPP build from the final reviewed source.
3. Verify real Cardassian/Federation axes, normals, materials, anchors, LOD transitions, render support and disposal/performance. CPU exports and reference compilation are insufficient.
4. Actual native Start → paid construction → fleet assembly → right-click Follow → per-round controls/3D playback → statistics → explicit aftermath → save/quit/restart.
5. 1280×720, 1920×1080 and 3440×1440 layout, scroll/focus/selection, whole-percent locks, no clipped essential controls and persistent navigation.
6. Surviving human round controls, early defeat, all AI-only remainder rounds, pause/speed/skip, re-entry, load replacement, stale confirmations and no duplicate aftermath.
7. Carry the prior R8 native draft/load/keyboard/resize negatives into this new package; verify audio and preserve original owner saves and frozen R7/R8 builds.

No native result, audio confirmation, frame-time budget, visual approval or validated-package promotion is claimed by this source checkpoint. The separate builder is `PlayableFleetBuild.BuildWindowsPlayableFleet`; its output is `artifacts/playable-frontend/Windows/HobbyDS9FleetCampaign.exe`.

## Source review corrections after c49037c

The independent source review identified two receiving issues: the four new map marker families bypassed the existing native KeyDown fallback and action guard; the initial overview camera sizing could clip the outer members of a large formation. These are source findings, not failures reproduced in a new native player.

The corrective delta reuses the existing Entry button guard and held-key binding for system stars, annular fleet rings, contacts and fleets in transit. Ring hit testing stays separate from its center star. Right-click/Shift+F10 review additionally rejects detached, disabled and stale controls; keyboard context review requires focus. Camera framing uses complete visual model/LOD bounds, both maneuver endpoints and rotation envelopes, viewport aspect and suitable clipping planes. It imposes no gameplay fleet cap.

Three new dedicated `PlayableFleetEngineTests` cover actual map keyboard/modal/release/replacement behavior, the paid-fleet round UI with three-resolution captures and presentation-only skip, and 12/32-ship imported-model framing in both cameras at wide/tall aspect ratios with stage disposal. The eight reference-compiler outputs pass with these tests included. **The new tests are authored and reference-compiled only until actual engine results are recorded.** Static binding checks now assert that all four custom marker families use the shared binder. The original c49037c portable evidence remains separately preserved; these corrections do not relabel it as later execution.
