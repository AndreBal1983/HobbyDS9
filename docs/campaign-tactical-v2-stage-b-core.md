# Campaign tactical v2 — gated authoritative lifecycle core

13 October 2026. **Stage B core implemented and tested; full Stage B and production activation remain gated.** This is actual Scenario/Simulation/Application-owned state, not the [Stage A toy](campaign-tactical-v2-lifecycle-backend.md). The physical travel/contact/intent path is not yet integrated. Recommended numerical policies remain **proposed**, not newly accepted gameplay.

## Checkpoint and scope

Fresh main remains `53d2e64973d9081a4d0090e771f5e777fc55cf40`. Draft equipment PR #19 remains at `be02572fc3fc2cfdb90cdd9ad763bbdf3d79716b`; draft Stage A PR #20 remains at `95a43685806f8b4fd724e6d645f854ac78f98571`. Neither was merged, reset or rewritten. This increment uses the existing isolated lifecycle worktree on a new `codex/campaign-tactical-v2-lifecycle-stage-b1` branch, preserving both parents.

The actual Stage A baseline passed locked restore/zero-warning Release and **1,555 tests**. Reviewed equipment code/validation/tests, design index, ship/AI/time/tactical chapters, architecture, blueprint/save contracts, latest UX reconciliation, Stage A alternatives, and subsequent product-manager handoff:

- Pinned documentation decision commit `bc68de172318d19ee256c81351728d30d6445978`, read locally without importing it. This preserves main's shield-first, armor and bounded three-round/recharge decisions.
- Read the separate presenter preparation and B1–B11 dependency plan at its delivered checkpoint; no presenter, Unity or other-worktree files were edited/imported.
- Read the product-manager candidate review, including the mobility discontinuity and observed hull-ratio caveats.

The milestone's agreed fleet/target/cloak/resource/stop behavior remains the intended contract. This increment does not claim accepted contact eligibility from co-location, accepted mobility tuning, a complete live Unity slice, or completed physical pursuit.

## Implemented authority

[CampaignTacticalReviewSession](../src/HobbyDS9.Simulation/CampaignTacticalReviewSession.cs) owns copies of the scenario definitions, controller configuration, current campaign, current pools, exact source bindings, cloak settings and private battle checkpoints under one lock. [Application](../src/HobbyDS9.Application/ICampaignTacticalReviewSession.cs) exposes a separate explicit review facade. Default `ICampaignSession` and source eligibility stay unchanged/disabled.

- Each actual initial ship requires its own validated authored provision and exact fit/revision/provenance. Current shields/ammunition initialize **once**, then save/load current quantities. Missing pools reject; destroyed ships/pools/fleets are removed, never recreated. V1 blueprints/static names cannot acquire equipment through this path.
- Frozen campaign requests supply real fleet/ship membership, owners, locations and encounter-local seeds. Each fleet remains one formation; same-owner fleets remain separate. Immutable installed hardware is compiled from validated authored fits, never client combatants or toy DTOs.
- Orders carry actor, command/encounter identity, expected turn/round and encounter-boundary revision. Replacements name owned active fleets and only one of five maneuvers plus visible hostile fleet focus. Omitted fleets retain validated intent. No per-ship target, protected allied fleet, result, RNG, stock or geometry payload exists.
- Private accepted human commitments persist across reload. Own observation includes the accepted command ID and exact committed orders, separately from standing orders. Opponent readiness/commands are absent. Encounter-boundary revisions advance once per resolved round; global campaign revision advances for each accepted mutation. Independent encounters therefore do not borrow global revision numbers as their round tokens.
- Baseline AI commits Hold/stable visible focus from the same frozen lawful state, before human replacements are applied. AI commitments are private; human API rejects AI actors. All-AI rounds can iterate without human orders; batch finalization remains an explicit host operation.
- Exactly one interactive round resolves when all active sides have committed. Later human decisions remain pending. Three rounds maximum, Defeat only; initial/loaded/retained/replacement orders reject cross-fleet protection and exit maneuvers.
- Save validation replays at most three **private recorded joint rounds** from actual frozen campaign requests and pre-batch pools, comparing state, stocks, RNG, geometry and filtered reports. In-bounds forged round damage/RNG/results cannot replace replayed authority. Replay uses isolated encounter streams and never changes the live campaign RNG.
- Completed results stay private/staged until the whole batch finalizes. All hull/destruction/sectors/ammunition commit canonically once; a later injected failure preserves every staged report and all campaign pools. Finalization clears the batch, opens next Planning and advances the existing turn once.
- Participating routes are cleared at legacy encounter allocation and cannot resume after finalization. This is **not** proof of stopping at an earlier physical contact; the missing travel gate remains explicit.

The shared resolver has a **private** review-profile context. Its public standalone path, DTO identity, armor/recharge/volley semantics and golden reference remain unchanged. Review context adds per-ship mobility, hidden offensive-target exclusion, ordinary reactive defense from hidden launchers, fleet-local defense and fallback explanation. It cannot be supplied through player commands.

## Proposed profile rules and lawful observations

All new choices are versioned review candidates, not approved universal rules:

- Personal tactical mobility is floor((speed + agility)/2). Shared range contests use the lower median of surviving individual mobility, multiplied by the frozen speed/agility weights. No extra command/cohesion bonus is enabled. A faster individual Flank attacker obtains a local lateral target facing even beside a slower fleet median. The canonical pair convention chooses Port/Starboard; no XYZ/camera geometry.
- A Screen member can spend its actual powered beam opportunities protecting a same-fleet ship when its personal mobility meets the launching ship's mobility. Self-defense retains its baseline eligibility. Every attempt, including failure, consumes a real offensive opportunity and cadence; no opportunities arise from an Escort label. This sharp eligibility threshold and launcher-mobility relationship remain consequential review questions.
- Authored cloak capability and positive concurrent reservation are explicit for every fit. Effective power, essentials, ordinary equipment and cloak must fit together; no free powered attack. Example: usable 80, essentials 26, equipment 36, cloak 6; remaining power 12. Reservation releases at reveal without reallocating weapon/shield output.
- Every encounter begins Distant/Fore. Hidden ships can fire ordinary round-one opportunities at visible targets; no extra volley. Mixed members remain individually visible/hidden. Incoming torpedoes can trigger lawful same-fleet interception without revealing hidden launcher IDs. All surviving hidden ships reveal after round one; mutual full cloak makes round one maneuver-only without nonexistent offensive ammo/RNG use.
- Visible condition deliberately includes **exact current/max hull**, type and four current shield stocks. This is a **proposed display precision** under the agreed visible-hull-condition rule. Automatic lowest-hull-ratio ranking uses those same visible quantities with stable ties, not an undisclosed maximum from private fixtures. Hidden ships are excluded before ranking. Baseline AI uses only visible fleet choices; it does not read reserves or fresh opposing orders.
- Own inspection includes exact current per-package stocks/cadence, package/version/mount/slots, coverage, damage/tracking/accuracy, power, armor, capabilities, fixed sector maxima, essentials and cloak reservation. Enemy projections contain no fit, armor, magazine, power or new order fields.
- Pair relations include only visible enemy fleets. Actor-local report sequence numbers are dense/stable and do not expose gaps in a private global event stream. Saved/observed reports filter enemy order/package/chance/reserve details and hidden round-one identity; incoming-ordnance reports can omit the launcher. Sector/range facts are filtered alongside IDs.

The Application surface has no global `SessionSnapshot` query: `CaptureStatus` supplies public phase/global stamp/batch readiness without enemy rosters or RNG; `GetOwnStrategicFleets(actor)` is owner-scoped. Full export is a **host save carrier**, never a renderer/tooltip/player diagnostic view.

## API and version matrix

Callable [review interface](../src/HobbyDS9.Application/ICampaignTacticalReviewSession.cs):

| Operation | Actual result / authority |
| --- | --- |
| StartReviewPackage / LoadReviewSave | New validated owned session; failed creation/load cannot replace a running session |
| CaptureStatus | Global turn/revision/phase, actual all-batch readiness, explicit legacy travel/admission labels, activation false |
| GetOwnStrategicFleets / GetFleetCloakSettings / InspectOwnShips | Immutable owner facts, current pools and persisted approach setting/capability |
| GetEncounters / Observe | Owned encounter IDs; actor-scoped round/boundary, own standing/accepted orders, legal choices, visible condition/relations and ordered reports |
| SetApproachCloak | Stamped owned Planning setting; no mid-battle re-cloak or stock change |
| SubmitStrategicCommand | Normal shared validation/staging; v2 construction/provisioning is unsupported, no inferred legacy equipment |
| AdvanceTurn | Existing strategic phases and legacy allocation in this gated increment; mixed/unbound produced ships reject without committing the turn |
| CommitRound | Typed ordinary errors; private acceptance or exactly one authoritative round; no forged result submission |
| FinalizeBatch | Global stamped canonical atomic consequences, once |
| ExportSave | Detached **complete private host save**, including opponents' private state for valid resume; not player observations |

| Contract | Identity / compatibility |
| --- | --- |
| Legacy saves/content/compiler/rules | Save **14**, Content **10/11**, `ship-design-compiler-v1`, `science-engineering-v1`, unchanged |
| Review composite content | New **package format 1**, `campaign-tactical-v2-lifecycle-review-1`, status `proposed`, `DefaultActivationAllowed=false` required |
| Review complete saves | New separately typed **review save format 1**; contains existing Save-14 campaign plus exact pools/settings/private checkpoints/history/ledger |
| Review resolver | `combat-resolver.campaign-tactical-review` / **1**; standalone-compatible private state carrier plus frozen review context |
| Standalone | Format 1, `combat-resolver.tactical` / 2, `tactical-combat-v2-provisional-1`, unchanged |
| Default campaign | Existing phased resolver / 1, unchanged; cannot automatically load a review wrapper or activate v2 |

This is a deliberate **new composite namespace**, not a silent Save-14 migration or a claim that Save 14 alone contains current v2 pools. Plain old saves are not automatically wrapped/provisioned. Future accepted routing/unified schemas must receive deliberate identities based on their actual changes; no next save/content integer is reserved.

## Actual CLI walkthrough and vectors

[Authored review package](../scenarios/tactical-v2-campaign-review/README.md) uses complete initial authored sources in a controlled, initially co-located two-owner experiment. Legacy unbound scouts are absent; actual static movement/design and equipment values remain source facts. All starting controllers are human for scripted joint-commit proof.

```powershell
dotnet restore HobbyDS9.slnx --locked-mode
dotnet build HobbyDS9.slnx -c Release --no-restore
dotnet run --project src/HobbyDS9.Headless -c Release --no-build --no-restore -- --review-campaign-tactical-v2
python scripts/check-campaign-tactical-review.py
```

The host hashes exact LF profile/scenario bytes and composes package identity as SHA256 of ASCII `campaign-tactical-review-package-v1\n{profile-sha256}\n{scenario-sha256}\n`. Observed package hash: `5406e3f89c513c8355ceee830421854508c34f24eac32aa1bfa5e6262fd762c5`.

It starts real Application state, enables owned approach cloak, advances to an allocated checkpoint, commits Round 1, serializes/reloads waiting commitments and boundaries, changes Round 2 to Flank and Round 3 to Screen, finalizes, then advances another strategic turn to a **second actual encounter**. No toy checkpoint/result is imported. The scripted first cruiser starts 7 ammunition, retains 3 after battle one, starts battle two at **3**, and ends at **0**. Shields start battle two exactly at the first final 26/25/12/0; only normal in-battle transitions increase them afterward. No between-battle refill/reset occurs.

Canonical, reversed commitment arrival, repeated and serialized/reloaded two-battle campaigns all produce:

`29110377df8d8ffa3e2f4d2cfdf40392671fb8b87d82d1955ec29def827cf199`.

The CLI displays own pools and public diagnostics, not enemy reserve/order payloads. These are independent review-campaign vectors, not replacement legacy/standalone goldens.

## Acceptance and remaining gates

Local final locked restore/Release: **zero warnings/errors**. Full suite: **1,621 passed, zero failed/skipped**, retaining all 1,555 starting cases plus **66 real-session cases**. New evidence covers actual source/resource ownership, no repeated provisioning, private receipts and AI, simultaneous stamps, ordinary rejection/replay/foreign/hidden/exit cases, in-bounds forged round state/replay validation, malformed provenance/hardware/resources, cloak cases, same-fleet versus separate-fleet defense, individual flank access with a slower shared median, lawful focus fallback, saved/current stocks, exact-once and later-batch rollback, source/state reordering, cultures and a **1,000-ship actual authored campaign round/reload**.

Portable PrepareUnity/static checks, authored equipment/legacy inspection, Stage A alternatives and standalone playground/R1/R2 resumes passed. Legacy canonical/reverse/repeated 25-turn save remains `fb2b75f18c8c5423f3a0850f38f6e53ee8249d3d08385978b5b2532041cc6089`, RNG `d8919406d6e01cf1`; standalone v2 reference remains `ccae7836c085936ed962b9ba001764ae4252f135fdf62fa1fbe2c0ea0bcef07c`. No old baseline was updated.

Exact delivered head/three-platform CI and running-check status are recorded in the draft PR and ignored delivery record. No merge, force push, default activation, Unity source/Editor/native/desktop or other-chat message is part of this delivery.

**Full Stage B still requires** physical shared time, explicit lawful hostility/engagement and fresh leave/stay/follow intent, weighted-edge timeline integration, simultaneous arrival/departure/start policies and later-arrival encounter batching. Current legacy co-location admission cannot demonstrate those rules and can prevent an agreed departure from a shared sector. Route clearing after legacy allocation is insufficient for mid-turn contact acceptance. This mismatch is intentionally visible in `CaptureStatus`, not silently labelled implemented.

Equipment acceptance and consequential policy review remain required before activation. Review the individual/shared mobility and screening thresholds, concurrent cloak reservation/release, visible condition precision/ranking, facing convention, stamps/private-history/report retention and the proposed physical timing/admission rules. No response means no acceptance.

This is a coherent **gated Stage B core checkpoint**, not complete campaign lifecycle or Unity acceptance. It is preserved before the overnight coordination cutoff of 06:30 UTC on 14 October 2026; further authorized increments can build on it without resetting prior work.
