# LCARS Ship Designer Sliders v1

Issue: [#7](https://github.com/AndreBal1983/HobbyDS9/issues/7). Based on fresh main `5beba2d652d283a2efb2f3ef44ddd5df9e38815d`, after accepted and merged PR #6. This milestone changes Unity presentation/local editing only; tactical backend work from Issue #8 is not required.

## Control inventory and binding

Operating Profiles uses one reusable `LcarsPowerSlider` for **Requested** and **Allocated** power for each installed component in Cruise, Combat and Survey. Twelve segments fill against the actual stack's full demand, with blue/lavender/amber low/middle/high position bands. These are presentation thresholds, not gameplay eligibility or critical-budget thresholds. High numeric position is not automatically an error. Backend error borders/messages are separate; zero, no demand, outside track range and pending drag validation have text labels independent of color.

Bounds are 0 through `InstallationPowerPreview.FullPower`; minimum powered demand is displayed verbatim from `MinimumPower`. The existing Application preview adapter supplies these detached facts alongside its other readouts, without another compiler call. Integer power DTOs supply one-unit increments. The track is a convenient edit domain, **not a claim that every intermediate value is legal**: Fixed systems, minimum demand, essentials, requested-versus-allocated consistency and profile budgets continue to be validated by the backend. Unity does not reproduce these rules or automatically shed/reallocate power.

Exact delayed Int64 entry remains beside every slider. Out-of-range, negative and over-budget values are retained in the draft and validated, not silently clamped. Only the track's visible thumb is bounded; the real value remains readable. Zero-demand tracks are disabled while precise input remains available. Missing backend range or ranges beyond SliderInt's domain retain the original precise fields and explicitly report the range gap. Backend-unavailable previews remain unevaluated.

Crew has no authoritative upper range and keeps exact entry. Component quantities keep Add/Remove/±1: capacity alone does not establish a safe quantity range because mounts, envelopes, dependencies and quantities interact. Class names, identifiers, hull/template/role pickers, immutable catalogue facts, derived costs/outputs and screens outside the designer are unchanged. No percentage setting currently exists in the local designer; unrelated campaign Research controls are outside scope.

## Commit and continuity

Pointer click/drag uses UI Toolkit's integer slider and capture handling. During dragging only fill/readout/exact echo update; a pending notice explains that results still describe the committed draft. Release or capture loss submits a single changed local value and uses the existing `SetPower`/Application preview path. Keyboard navigation adjusts one unit once; Home/End reach track bounds. Exact entry commits on Return/focus loss. No change event recursively writes the other field, and unchanged values do not trigger compilation. Tab/profile switching, rename, templates, components, Reset and navigation/discard guards retain the same local draft APIs. Existing scroll offsets survive refresh.

Overview PNG, VSync, campaign save/content/rules versions, portable compiler, AI and combat resolver are unchanged. Publish remains disabled. [Unity Ship Designer — Campaign Blueprint Integration](unity-ship-designer-campaign-blueprint-integration.md) remains a separate proposed milestone.

## Acceptance evidence

Final-head test counts, CI, native interactions and reviewed resolution findings are recorded below after execution and in the draft PR. Unity Game view evidence is produced by the capture helper under ignored `artifacts/unity/ship-designer-sliders/`: Overview for both templates, Weapons, Power and Combat profiles at normal/zero/full/invalid values, each at 1280×720, 1920×1080 and 3440×1440. This helper refuses to discard a user's pre-existing modified draft; it resets only its own generated fixture edits.

Desktop control is confined to this milestone's Unity/native player for necessary acceptance. Agent 1's worktree/session is left undisturbed. The new PR remains **draft** and is **not auto-merged**.
