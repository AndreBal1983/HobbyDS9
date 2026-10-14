# HobbyDS9 overnight handover — 14 October 2026

Prepared during the final verification window before 08:30 Europe/Amsterdam. **A runnable opt-in Unity review now exercises the real campaign-v2 lifecycle. It remains a review harness with proposed policies, not an activated default campaign or finished tactical game interface.** No PRs were merged. Main and ordinary user saves were preserved.

## Delivered result

The reviewed stack adds explicit authored equipment, authoritative session-owned combat, physical travel/contact, actor-safe observations and commitments, full physical-root durable checkpoints, truthful own-resource explanations, a headless client, and an opt-in Unity binding. Later deliveries supersede earlier inspection/prototype limitations only for their explicit gated path. See [chronological evidence](overnight-review-notes.md) for each PR, exact commit, source review and assignment.

Final native source: [draft PR #33](https://github.com/AndreBal1983/HobbyDS9/pull/33), `bae723f182dbff07bcfbf83a47716c7143eed6b8`, descending from [#32 modal fix](https://github.com/AndreBal1983/HobbyDS9/pull/32), `280930db107204f23a4fe0821710bbbe75c1fc10`, and the combined backend/client/import stack. #32 makes retained confirmation callbacks one-use and invalidates canceled/stale actions; its source-level regression reproduces the old defect, without claiming actual Unity queued delivery. #33 fixes the control contrast PM observed in the actual native build.

| Evidence | Result and limit |
| --- | --- |
| Portable backend/client | 1,685 backend cases at the final gameplay pin; separate physical-client/storage/presenter/adapter suites recorded in owning PRs. No unchanged backend rerun for styling. |
| Exact-head CI #32 and #33 | Each head: 14 successful workflows, 42 successful Linux/Windows/macOS jobs; open drafts, unmerged. |
| Actual Unity 6000.6.5f1 | 121/121 EditMode tests passed, no failures/skips, on both native candidate pins. |
| Actual Windows x64 IL2CPP | Build succeeded on both pins. Final build reports success and exit 0. |
| PM native campaign walkthrough | Physical Follow/Leave contact at Bajor time 3/4; independent private human commitments; save during waiting; new-process resume; changed Flank/Screen orders; three-round completion and once-only aftermath; fresh StayEngage contact at time 0/1/Distant; second battle to depleted torpedoes. |
| Native persistence | New final checkpoint reopened in a separate process: Turn 3 Planning, hulls 207/173, torpedoes 0/0; retained shields/damage. |
| Native failure path | Faster checkpoint in slower session rejected as InvalidCheckpoint; initial Turn 1/hull 220/shields/command ID retained, original save hash unchanged. |
| Layout/controls | Readable fixed contrast at 1280×720, 1920×1080 and 3440×1440. Narrow panes stack and scroll; own ship inspection worked. Full battles were exercised at 1920, not every resolution. |
| Default bootstrap | Same executable without review flags opens existing strategic LCARS Turn 1 Planning. No default campaign commands or saves issued. Startup smoke only. |

## Reproduce locally

Open [Launch Physical Review — Faster](C:/Users/ambal/.codex/worktrees/unity-review-editor-acceptance/HobbyDS9/artifacts/unity-review/Windows/Launch-PhysicalReview-Faster.cmd). The [slower launcher](C:/Users/ambal/.codex/worktrees/unity-review-editor-acceptance/HobbyDS9/artifacts/unity-review/Windows/Launch-PhysicalReview-Slower.cmd) and [player README](C:/Users/ambal/.codex/worktrees/unity-review-editor-acceptance/HobbyDS9/artifacts/unity-review/Windows/README-PhysicalReview.md) are alongside it. Keep the executable, DLLs and data directory together. Launchers were reviewed as simple relative-path wrappers; they were not executed as part of packaging. Launching the executable alone follows the existing default bootstrap. Faster review arguments are:

```text
--hobbyds9-physical-review --hobbyds9-review-profile faster --hobbyds9-review-actor civ.federation
```

The actor switch is deliberately labelled trusted hotseat review. It is not a secure multiplayer login or polished player flow. Review-only slots created during this acceptance: `pm-live-20261014`, `pm-waiting-20261014`, `pm-final-20261014`. The final slot belongs to faster; slower rejects it. Loading requires confirmation. Do not overwrite a slot needed for reproduction.

Final save: physical format 2 / nested campaign save 14, 74,651 bytes, SHA-256 `64dd7a146b7e999f8caef6c32fa5f4ffb402223f85ff3ae4e1dfe13ced3e4119`. Own and opposing stocks were independently checked from this newly created trusted-host save; private reserves were not exposed through the opposing actor view.

Primary's [native acceptance report](C:/Users/ambal/.codex/worktrees/unity-review-editor-acceptance/HobbyDS9/artifacts/unity-review-acceptance/acceptance-bae723f.md) contains XML/log/build receipt links, hashes and CI evidence. Its checkout retains automatic Editor-generated settings/resource differences, captured in a patch; source pin alone is not a claim that the imported checkout is clean. Earlier build/logs are preserved. PM closed all test player processes after checks.

## Remaining gates

Injected Enter/Shift-Tab modal navigation and Escape popup dismissal did not work; mouse, text entry and OS close worked. Source inspection did not establish the navigation cause. Space is not configured as Submit. Physical-keyboard comparison is still needed before keyboard acceptance or a justified code fix.

Runtime logs contain missing ICU data/minimal text segmentation and unsupported/stripped URP depth-of-field/Panini post-processing passes. English UI worked. International text/emoji, full graphics/post-processing and physical audio remain unaccepted. This is not a clean-log or zero-warning delivery.

Secondary's [read-only runtime follow-up](C:/Users/ambal/.codex/worktrees/unity-review-runtime-followup/HobbyDS9/docs/unity-review-runtime-followup.md), local commit `87e652967b1539c4f322306acaabd70b933800e4`, traces the panel dependency to a runtime-created pattern also used in default v1 and the shader notices to inherited URP assets. It preserves the distinction between committed settings and local import changes, and does not establish the exact stripping/unsupported-compilation cause. No speculative runtime patch was made.

The [activation decision packet](C:/Users/ambal/.codex/worktrees/campaign-tactical-v2-lifecycle/HobbyDS9/docs/campaign-tactical-v2-activation-decisions.md) retains four consequential proposed choices: travel-boundary timing, individual-mobility/formation formulas, concurrent cloak power, and enemy-condition precision. Mixed-hostility participation and provisioning newly constructed v2 ships are separate scope/engineering gates. The packet's older statements that native checks were unperformed describe its earlier checkpoint; this handover adds the bounded native evidence above. No choice became approved merely because tests passed.

## Recommended next work after owner review

First play the controlled review and discuss one policy at a time using the packet's worked outcomes. Keep fixed authored inventory and supported owner relationships for that trial. Preserve the agreed simple fleet commands, finite stocks, fleet-local screening, cloak reveal, information boundaries and physical pursuit.

Then assign Primary a bounded portable increment only for an explicitly selected policy deviation or newly agreed provisioning/side scope. Assign Secondary the native keyboard/rendering acceptance follow-up and a player-facing presentation increment using the existing real APIs. Keep backend and client ownership separate; do not implement mechanics in Unity. No additional default activation or merge is implied. A reviewed integration/merge plan can follow explicit authorization.

The computer reset interrupted monitoring: the durable log has a gap from 02:41 to 07:13 local. Final native work followed the owner's explicit 07:32 desktop authorization. This report does not claim continuous overnight activity.

Both Primary and Secondary reached final safe checkpoints and are idle, with no queued runtime work. Primary's artifact receipt inventories 698 files; receipt SHA-256 `57969fef65a48ae40a062bf4ff8bb7b810b222f28006ae6c33cde0004e3d54a6`. The deadline remains 08:30; no further implementation or design decisions are implied by remaining trial time.

Closeout after08:30: both final delivered checkpoints unchanged; monitor paused, one-shot morning automation removed, no development continued pastcutoff. Owner may run the manual test; further coordination awaits new instructions.
