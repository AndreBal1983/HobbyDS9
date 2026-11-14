# GUI-POLISH-02 receiving

14 November 2026. **Presentation source candidate; Unity graphics/native and owner visual acceptance pending.** Base is `23acf300df317c39eef8af30f8eeb332ba8d41d9`: QA's tested HUD PR111/f5cdac0 plus PR113's map viewport receiving. Optional original hull proposals are not substituted into this branch. Existing saves, content/rules versions, sealed packages and main stay unchanged.

## Routine actions

Streamed/colonisation Preview, Submission and Planning advance keep the mounted page and show `routine-command-pending`. Saving also stays on the mounted page; campaign generation, profile/archive loading and checkpoint loading retain their existing major-load presentation. A command check has no fake delay, percentage or optimistic acceptance. Conflicting action/navigation controls are unavailable until the one-use worker drains; scrollbars remain usable. Existing model/host leases, copied commands, source/controller/frame stamps, cancellation and transactional publication remain authoritative.

Preview/rejection completion updates local review/status and footer without disposing the page. Accepted frame replacement rebuilds from authoritative snapshots once; the existing selection/draft/camera capture remains, and the latest scroll offsets plus named focus restore after layout with epoch/model/frame guards. Closing an unchanged confirmation also retains its underlying view. Disable/late completion cannot publish into another owner. Re-enable during a draining cancellation exposes a local pending shell without querying the locked source; acceptance must check this separately from an uninterrupted mounted command.

`--hobbyds9-command-timing` writes `HOBBYDS9_COMMAND_TIMING` rows for routine streamed/colonisation operations: owning-thread admission/local cue, worker await, publication and view reconciliation. Worker time includes dispatch/continuation scheduling; view time is synchronous reconciliation, not GPU completion or next-frame latency. No timing enters gameplay state or causes a retry.

Portable .NET measurements on this machine, seed42 continuous-expansion package, one fresh preview/enqueue pair per size (no performance threshold):

| Systems / empty regions | Operation | Admission ms | Worker ms | Publication ms |
| --- | --- | ---: | ---: | ---: |
| 30 / 30, 750000×600000 | Preview | 0.072 | 70.10 | 0.015 |
| 30 / 30 | Enqueue | 0.076 | 89.39 | 0.022 |
| 250 / 250, 2000000×2000000 | Preview | 1.619 | 199.49 | 1.967 |
| 250 / 250 | Enqueue | 0.326 | 212.09 | 1.147 |

Existing host phase probes put fork time near44ms/29ms at250, followed by roughly152ms for eligibility or183ms for mutation plus full frame/presentation/Commission/planet/activity capture. The probe does not isolate mutation from capture. These are representative CPU observations, not the owner's save, Unity/IL2CPP timings, percentiles or evidence of a backend optimization. A future backend investigation should separate `ForkOperations`, eligibility/command validation and accepted-state captures in `PhysicalReviewHost.ColonisationInteraction`; there is no serialization/replay in this command fork path. The GUI removes the unnecessary whole-screen replacement and preview rebuild; portable rules/capture semantics are unchanged.

The measurement fixture also verifies read-only preview, duplicate admission refusal, accepted publication and repeated-completion refusal. Existing transaction tests cover canonical root equality, rejected construction, stale frames, cancellation/drain, typed save/continue and late failure. The first250 fixture used an invalid small map extent and failed density admission before executing a command; after using supported extents its two-case rerun passed. Keep that setup failure separate from implementation results.

## Survey review

Departure review leads with **Interrupt survey?**, actor-owned active survey targets and actual completed-week counts, followed by concise consequences. Work loss at the weekly boundary remains explicit. **Keep survey** sends no travel order; **Interrupt survey & depart** runs the existing stamped travel command. Travel cost/route/timing remains in a collapsed, keyboard-reachable disclosure. Permanent empire survey progress is agreed design only and is not promised or implemented here. No hidden target, total-duration estimate or extra survey reward is inferred.

## System view

Circular XZ-plane tracks and bodies use one camera at24° elevation, producing an approximately0.407 projected minor/major ratio. Camera fit uses that projected envelope; spheres are not flattened and orbital mechanics are unchanged. Markers use that same camera's world projection. The star position still owns overview day/night lighting. Lower fill, stronger direct daylight, reduced cloud opacity and a day-side atmospheric surface limb replace the broad16%fill/unlit rim. Selected portraits use stable, more lateral lighting independently of overview pose. The received Art recipe uses overview fill0.04/portrait0.025, direct1.15, cloud opacity0.55 and a fifth-power day-side limb. Exact disclosed authored Mars suppresses Earth-like decorative clouds/blue limb while retaining snapshot atmosphere/pressure facts. Generated cloud masks are linear; color maps retain their existing color interpretation and attribution. No additional bodies or exterior atmosphere shell are introduced; unknowns retain neutral disclosure guards. Render targets, reduced-motion behavior and dirty rendering remain bounded as before.

The recovered System comparison and the owner's four screenshots were inspected. The newest standalone director render remains unrecovered; this candidate cannot claim parity. Shader compilation, actual day/night contrast, full globe framing, orbit/label picking, resize/zoom/re-entry/load, reduced motion, idle redraw/resource disposal and GPU/frame timing require QA-owned Unity/native evidence at1280/1920/3440 render targets and available native sizes. The white-globe graphics fixture distinguishes real directional contrast from texture coloration; the plane fixture checks projected aspect and marker/body alignment.

14 November post-reboot receiving correction: Art PR115 final `a0e528cf7c046342a87703be7b147d4606ae493b` specifies portrait key **(-6.3,2.45,-2.8)**. This supersedes PR114's exploratory **(-3.6,1.4,-1.2)** without changing the received shader, globe/camera framing, overview star lighting or other GUI behavior. The correction applies Art's exact `PortraitKeyFinal.patch` on a separate continuation; PR114's delivered pin remains immutable. Art's final Earth/Mars no-cloud formula probes were inspected and are supporting source evidence only. No interrupted or post-reboot Unity/native test is counted as passed; QA must record the actual source/key with its after-images.

## Acceptance boundary

The owner's Defiant crop matches the detailed retained legacy Meshy model, not the optional original Escort. GUI independently matched all seven mesh/map input hashes in Art's audit, including the29,998-triangle Defiant and its three JPEGs. Current source binds white tint and sRGB Image_0, linear Image_1 roughnessG/metallicB and tangent-normal Image_2; source emission is absent and PreviewStudio defaults it to black. That retained albedo is gray/red. This establishes source identity, not approval of its colors or a native material-binding pass. The approved replacement's actual palette/material payload remains unreceived. No guessed recolor/emission or silent model substitution is included; the Defiant color complaint remains open against that missing target and the next exact-source native binding check.

Source/reference compilation and portable transaction tests do not pass native visual quality. Focused receiving must exercise accepted/rejected production and rapid duplicate input, retained mounted instance/selection/scroll during work, latest scroll after accepted refresh, unchanged preview view, local saving and major loading, modal cancellation/confirmation and keyboard disclosure. Preserve original failures, per-pin logs and actual before/after captures. The inherited window-X shutdown defect remains open; no speculative teardown change is included. Defiant material receiving is tracked separately against the exact legacy asset shown by the owner.
