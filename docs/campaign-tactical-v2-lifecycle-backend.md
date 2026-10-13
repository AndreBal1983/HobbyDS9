# Campaign tactical v2 lifecycle — executable contract review

13 October 2026. **Stage A review candidate; Stage B not implemented; campaign activation disabled.** All formulas, numerical fixtures and future API names below are proposals. The executable review assembly has no campaign-state import/conversion or production Simulation/Application dependency. It must not be used as a combat resolver.

## Checkpoint and review inputs

Fresh fetch confirmed accepted main `53d2e64973d9081a4d0090e771f5e777fc55cf40`. Equipment [PR #19](https://github.com/AndreBal1983/HobbyDS9/pull/19) remains **draft, open and unmerged**, head `be02572fc3fc2cfdb90cdd9ad763bbdf3d79716b`. Its [exact-head three-platform PR CI](https://github.com/AndreBal1983/HobbyDS9/actions/runs/38427862194) has completed successfully. Green CI is not owner acceptance or merge authorization. This isolated `codex/campaign-tactical-v2-lifecycle` branch starts at that equipment checkpoint, preserving both existing branches/worktrees. Delivery is stacked on the equipment branch, not a claim of accepted-main integration.

The actual starting locked restore/Release passed with zero warnings/errors and **1,507 tests**. Reviewed [design index](design/README.md), ship/AI/time/tactical chapters, [architecture](architecture.md), [blueprint contracts](blueprint-construction-v1.md), [equipment report](tactical-v2-authored-equipment-foundation.md), the save validators and actual movement/resolver/session boundaries.

The owner's lifecycle assignment/milestone and amended tactical chapter are local planning inputs, not files distributed by this branch. Read-input SHA256:

- Backend assignment: `b1422ec82e3e9c79bc4c9076fcc90886ed909bfefc793a6bbd4cb51603f1ae5e`.
- Lifecycle milestone: `a61e29d88c7c2d77752803adf596ecfc323eefb0c7e86bfb4c678d0966358033`.
- Amended tactical chapter: `1dee3914fdd69ebcba9b5be0b03684d9e04210c2f4a912941636dc6ebd24f4e8`.

Latest separate UX reconciliation inspected at local commit `455bf10` on `codex/campaign-tactical-v2-ux-spec`; its D1–D8 register preserves agreed behavior versus remaining policies. It was read, not edited, merged, pushed or dispatched here. The older tracked tactical chapter still contains superseded proposals. The dated owner decisions summarized next govern this work; documentation existence does not establish runtime support.

**Agreed behavior retained:** one strategic fleet/one tactical command unit, independent same-route fleets, meaningful individual mobility, fleet-local finite screening, fleet focus plus automatic visible-hostile fallback without rewriting intent, Distant openings, ordinary round-one cloak fire/protection, incoming-ordnance defense, end-round-one reveal, mutual-cloak maneuver-only round, visible enemy types/hull/shields but hidden reserves/new orders, retained survivor pools, movement stop, fresh following-turn orders and physical pursuit. No within-battle exit maneuvers, extra round, automatic stock recovery, inferred v1 equipment, refit or Unity work.

## Exact integration gaps

| Existing boundary | Missing lifecycle authority |
| --- | --- |
| Standalone formation ratings and shared pair geometry | Individual capability contribution under one fleet order; maneuver/target access that does not apply a universal minimum speed |
| Standalone designated allied-formation protection | Fleet-local acquisition/interception and order validation, including retained/loaded orders |
| Source fits and initial provisions | Explicit cloak capability/power, initialized-once current four-sector/ammunition pools with exact fit provenance |
| Omniscient standalone events | Lawful start-round projections and audience-filtered saved reports; hidden launcher identities and reserves excluded |
| Pure fixture `ResolveRound` | Session-owned encounter/RNG/round, actor/controller validation, simultaneous commitments and replay protection |
| Shared movement-cost stepping and post-Movement encounter allocation | Equal physical week fractions per fleet, exact intermediate contact, intentional lawful hostility, stopped routes and following-turn decisions |
| V1 request/result buffering | V2 private staged rounds, all-encounter atomic aftermath and independent pinned identities |

The existing Movement phase does freeze multi-owner stationary locations during shared integer cost steps; it is not simply an unmodified end-position loop. However different fleet budgets are consumed at the same cost-step rate and eligibility uses multi-owner presence. Neither that freeze nor the later encounter detector proves the required simultaneous physical timeline and lawful engagement contract.

## Reproduction and executable boundaries

From the repository root:

```powershell
dotnet restore HobbyDS9.slnx --locked-mode
dotnet build HobbyDS9.slnx -c Release --no-restore
dotnet run --project src/HobbyDS9.Headless -c Release --no-build --no-restore -- --review-lifecycle-prototypes
python scripts/check-campaign-lifecycle-prototypes.py
```

[Review arithmetic/types](../prototypes/HobbyDS9.CampaignTacticalV2.Prototypes/FleetCandidates.cs), [travel alternatives](../prototypes/HobbyDS9.CampaignTacticalV2.Prototypes/TravelCandidates.cs), [transaction prototype](../prototypes/HobbyDS9.CampaignTacticalV2.Prototypes/PrototypeBatch.cs) and [CLI walkthrough](../src/HobbyDS9.Headless/LifecyclePrototypeDiagnostics.cs) are executable but **not campaign authority**. The portable prototype compiles netstandard2.1/C#9 and references no HobbyDS9 production assembly. Only tests and the diagnostic host reference it; Contracts, Simulation, Application and portable Unity assembly copies do not.

The CLI flag accepts no scenario, result, resource edit or checkpoint input. Existing scenario/Application CLI and equipment/legacy inspection commands remain unchanged. Prototype JSON serialization/reload happens on separately typed in-memory checkpoints, never `SaveEnvelopeDto`. Tests assert the dependency separation.

## Mobility and composition alternatives — proposed

Fixture: capital speed/agility/command/cohesion **30/20/70/60**, cruiser **70/60/50/55**, escort **90/80/30/40**. Cruiser capability values match the provisional equipment example; capital/escort are explicitly additional review fixtures, not published live fits. All are in one fleet. Defensive equipment offers respectively **1/2/2** finite opportunities. The supplied incoming threat's review mobility is **70**.

**A: individual threshold.** Personal mobility is floor((speed + agility)/2). Each ship compares its own result against threat mobility. Meeting the threshold makes its existing defensive opportunities eligible; it does not create additional opportunities. A positive margin supplies a candidate local lateral target aspect for a Flank order, while fleet-pair opening range remains Distant.

**B: positive coordination threshold.** Same personal mobility, plus floor(max(min(command, cohesion)) / explicitly supplied divisor), here **10**. This gives **6** positive support without lowering anyone's baseline or imposing a size cap. It is a proposal for a basic coordination benefit, not the future commander hierarchy.

| Ship | Personal mobility | A screening / Flank target aspect | B screening / Flank target aspect |
| --- | --- | --- | --- |
| Capital | 25 | 0 / Fore | 0 / Fore |
| Cruiser | 65 | 0 / Fore | 2 / Lateral |
| Escort | 85 | 2 / Lateral | 2 / Lateral |

Fast escorts retain local usefulness beside the capital under both alternatives. Homogeneous fleets remain valid; adding a 10,000th capable ship still contributes its two baseline opportunities. Labels do not enter the arithmetic. Failed interception consumes an opportunity that consequently cannot be used offensively; saturation and destruction remove contributions.

The threshold discontinuity and maximum-leader support are deliberate reviewable tradeoffs, not approved balance. The local aspect candidate is **not wired into production or the toy firing model**. Full per-ship arc/geometry integration, sensor use, maneuver comparison, command-loss effects and all weapon/recharge rules remain Stage B work after review. No single averaged/minimum fleet rating is proposed as a replacement for individual actions.

Automatic target ranking alternatives are also executable: **lowest stable ID** versus **lowest observed hull ratio**, with ordinal ties. A visible 80/100 ship with lower ID wins the former; a visible 90/200 ship wins the latter. Hidden/dead ships cannot enter that ranking. The toy round uses lowest hull ratio, preserves unavailable standing fleet focus and emits a lawful fallback explanation. A new focus selection requires a visible hostile fleet; retaining an unavailable prior preference does not permit firing at hidden members.

## Shared travel/contact alternatives — proposed

Each explicit known route leg has a positive authored cost. Travel duration is **remaining edge cost / fleet's ordinary strategic movement budget**, in exact normalized BigInteger rational week fractions. Starting weighted-edge progress is retained; no intermediate node or combat sector is fabricated. A fleet in an edge interior occupies no sector. Arrival at week end does not begin the next edge. Routes are semantic sequences, not sorted sets.

The review model takes explicit lawful hostile contact-node knowledge and independent routes. It does not infer a follower's route from the opponent's hidden destination. These are controlled fixture inputs, not a new fog-of-war/navigation validator. Production must derive routes/costs/knowledge from owned state, dynamic traversal effects and actual navigation associations.

**Boundary A: Departures First.** Occupancy uses half-open visit intervals; instantaneous through-sector visits have no contact window. Both passing through B at 1/2 can continue. This may undesirably allow simultaneous hostile passage through a defended region.

**Boundary B: Arrivals Before Departures — recommended for review.** Batch all arrivals, evaluate lawful same-sector contacts, then departures at the same exact time. Both fleets reaching B at 1/2 engage and stop there; remaining routes are cleared. Ordinal IDs only canonicalize output, never choose a first mover.

Separately choose **shared-start departures before renewed contact — recommended** versus **contact before departure**. Under the former, a departing fleet may leave an already shared sector at time zero; stationary hostile fleets choosing combat still engage. Under the latter, a staying engager can stop a departing opponent immediately. This materially changes whether post-battle departure is possible and needs review.

| Worked case | Executed outcome |
| --- | --- |
| Two through-routes arrive B at 1/2 | Boundary A: no meeting; B: one meeting, both stop at B |
| Known-contact pursuer speed 3, departing target speed 2 | Pursuer reaches declared B at 1/3; target arrives B at 1/2; Boundary B contact at 1/2 |
| Same pursuit with speed 1 | Pursuer arrives B at 1; target already left; no encounter or catch roll |
| Shared starting B, one leaves | Departure-first start: no forced time-zero encounter; contact-first start: encounter |
| Both stationary at known hostile B and engage | New turn's contact at 0; no fourth round/same-turn repeat mechanism |
| Both leave without engagement intent | Neither forces combat, even under contact-first start |
| Opposite directions along B–C | Interior path crossing is not a sector meeting |
| Weighted B–C cost 5, initial progress 1, speed 2 | Remains in edge, progress 3/5, no false occupancy at B |
| Contact participants plus unrelated fleet | Only connected lawful participants stop; unrelated route reaches E |

Disconnected lawful contact components remain separate. One fleet cannot enter a second encounter in the same prototype turn; stopped movement is not resumed. Routing later arrivals into an already allocated encounter and larger connected contact batches require an explicit reviewed production policy. The prototype reports exact endpoint membership/stops but does not run economy, allocate campaign IDs/seeds, map nodes to systems or increment a calendar.

## Cloak, observation and round transactions — proposals/proof

The toy source explicitly supplies capability, approach-derived initial hidden state and cloak power cost. Example usable power **80**, ordinary full allocation **62**, cloak reservation **6** is feasible concurrently, leaving **12** unused. With usable power **62** it rejects; turning a 6-power cloak into free power is not an option. A separately authored zero-cost capability is representable but would be a distinct reviewed source rule. Recommend explicit concurrent full reservations, release cloak allocation after round one, no refill or re-cloak. General anti-cloak balance remains excluded.

Toy rounds use two torpedo opportunities, finite seven-round stock, yield six, fore shields ten, and explicit deterministic fixture rolls **1000/9000** against interception chance **5000 bps**. These are review inputs, **not campaign RNG or approved combat balance**. Incoming-ordnance defense uses only the target fleet and spends offensive opportunities on both success and failure:

- Fully cloaked A versus visible B: A spends two ordinary torpedoes; B cannot offensively target A, but intercepts one and fails one. B retains seven torpedoes; A retains five. Both surviving A/B ships are visible at the boundary.
- Both fully cloaked: no legal offensive target, no launch or fixture-roll consumption, both retain seven; round one counts and reveal precedes round two.
- Mixed A: only its visible member is targetable; reveal exposes surviving hidden members at the boundary.
- Same-fleet escort: two defensive attempts. The same equipped escort in another friendly fleet: zero contribution to the transport's defense.

The toy omits energy fire, mounts/arcs/cadence, armor, recharge and geometric firing consequences. It orders opportunity spending canonically rather than claiming to reproduce the standalone simultaneous volley. This is an access/privacy/transaction proof, **not complete tactical resolution**; production must preserve/review the full agreed simultaneous semantics and compatible standalone rules separately.

Visible enemy projections contain only ship/fleet identity, type, hull and four current shield stocks. Own projections are detached copies including exact stocks. Hidden ships are absent; reports to the attacked actor describe incoming ordnance without naming its hidden launcher. Reports do not disclose enemy ammunition, fit internals or fresh orders, including after reveal. Focus-fallback reports retain standing intent.

`PrototypeRoundCommand` carries encounter, actor, command ID, round, boundary revision and full owned active-fleet orders (maneuver/fleet focus only). Initial, loaded, retained and replacement values are checked; no protection/ship-target/result/resource/seed payload exists. Baseline AI derives Hold and stable visible focus from the same observation as a human, before reading any commitment.

The first accepted commitment is private and can be serialized/reloaded. Its acceptance does not advance the prototype boundary stamp or reveal opponent commitment state; the second actor can submit from the same start-round observation. Joint completion resolves **one** round, then advances the boundary. Duplicate/foreign/stale/hidden/exit/malformed orders reject before stocks, reports or fixture cursor change. Unresolved later human rounds stay unresolved.

**Proposed production stamp distinction:** global campaign revision for save/transaction concurrency, separate immutable encounter-round boundary token for simultaneous commands, plus actor-specific acceptance/replay state. Global revision should increment for persisted commitments; the prototype's single boundary revision is not a drop-in replacement for the existing campaign stamp. Keep both values in the new checkpoint and avoid exposing opposing private acceptance details.

## Typed production API and version proposals

These names are proposed contracts, not callable Application APIs:

| Proposed operation/type | Authority and fields |
| --- | --- |
| `SetFleetApproachCloak` strategic command | Own fleet, boolean setting, existing Planning actor/command/stamps; source capability/power remains content-owned |
| `CampaignTacticalObservation` | Actor-scoped encounter/round/boundary token, own units/resources, visible contacts/condition, lawful focus choices and filtered reports |
| `CommitCampaignTacticalRound` | Encounter/actor/command ID/round/boundary token; owned fleet maneuver/focus replacements, explicit retain intent; no opponent DTOs or results |
| `CampaignTacticalCommitResult` | Stable rejection or own accepted/waiting/resolved boundary; no private opponent commitment payload |
| `CurrentTacticalShipPools` | Stable ship, exact fit/provisioning provenance/revision, current hull/four sectors/per-package ammunition; initialized once and retained across battles |
| `CampaignTacticalCheckpoint` | Frozen membership/sides/source/profile, local RNG, next round, standing orders, private commitments/replay IDs, visibility and filtered current-batch reports |
| `FinalizeCampaignTacticalBatch` | Existing campaign turn/revision; session-owned completed encounters only; staged atomic consequences, no accepted result DTO supplied by host |
| `StrategicContactOrder` | Leave/stay-engage/follow intent tied to owned fleet and lawful route/contact evidence; reviewed shared timeline, no hidden route or separate success roll |

Recommend persisted waiting-for-another-human boundaries and complete current-batch reports, no permanent battle archive. A local single-human host can gather baseline AI commitments in the same transaction; multiplayer waiting must not authorize host-forged opponent orders. The prototype demonstrates private waiting/reload and same lawful AI observation, not authenticated networking.

| Identity | This candidate | Live-path proposal |
| --- | --- | --- |
| Campaign save | **14**, unchanged | Assign a new explicit supported schema when mutable pools/batches/commitments are implemented; no migration guessed |
| Content | Legacy **10**, equipment **11**, unchanged | New pinned cloak/live profile schema with deliberate compatibility validation |
| Campaign rules/compiler | `science-engineering-v1`, `ship-design-compiler-v1`, unchanged | Explicit opt-in live rules/profile; frozen v1 remains v1 |
| Campaign resolver | `combat-resolver.phased` / 1, unchanged | Distinct reviewed campaign-v2 resolver/profile identity |
| Standalone v2 | Format 1, `combat-resolver.tactical` / 2, `tactical-combat-v2-provisional-1`, unchanged | Preserve unchanged pathway/reference |
| Review JSON | `campaign-lifecycle-review-prototype-1` only | Never valid as a campaign save/content/result |

No next save/content integer or production resolver identity is reserved merely by this document. Production allocation must reject unsupported mixed-source encounters explicitly; neither static names nor frozen v1 aggregate stats synthesize v2 equipment.

## Persistence, atomicity and independent vectors

The CLI runs three committed toy rounds canonically, with reversed commitment arrival, repeated/reordered source and serialized reload after the first commitment and every resolved boundary. All produce checkpoint SHA256:

`cd0e04152dbf067e36037deafc42c911776cfdce24225580a60daea533695eb5`.

It then creates an explicitly **second toy encounter** from retained survivors: A hull **100**, shields **10/5/5/0**, ammunition **1**; B hull **92**, shields **0/5/5/0**, ammunition **7**. Reapplying capable approach cloak changes no pools; the next ordinary launch leaves A ammunition **0**. This is a serialized resource/provenance example, not two actual campaign-owned battles.

The private batch prototype resolves two encounters, injects a failure at the later canonical encounter, proves every campaign-like pool/staged result unchanged, then finalizes once. It has no result-submission method. Toy aftermath preserves exact fit revisions and removes destroyed ships; no clock, rewards or recovery are implemented.

The entire repeatable review transcript has SHA256 `ef8c1ebe98b5a8de7aa69d025b1876ab73e67c3c8b8f8790d02a7307a4176aa9`. These are **new independent prototype vectors**, not replacement campaign or standalone goldens.

## Acceptance and remaining gates

Local final locked restore and Release: **zero warnings/errors**. Full suite: **1,555 passed, zero failed/skipped**, retaining all 1,507 baseline cases plus **48 prototype cases**. Coverage includes exact overflow-safe time, both contact policies, weighted-edge progress/end-week behavior, pursuit/unknown-contact isolation, separate fleets, individual mobility/large fleets, cloak cases/reactive costs/privacy, retained focus fallback, private AI observations, transaction rejection, loaded commitment/resource validation, detached exports, culture/order/reload equality, depleted subsequent fixtures and later-batch rollback/exactly-once finalization.

Large-fleet checks exercise **10,000** individual capability contributions and a **1,000-ship** round with one command unit per fleet. They are structural stress evidence, not a measured playable combat performance claim.

Portable PrepareUnity/static checks passed, copying only existing production assemblies. Actual legacy Application/scenario CLI, source equipment valid/invalid/repeat/isolation, legacy inspection and standalone playground/R1/R2 resume smokes passed. Legacy canonical/reverse/repeated 25-turn save hash remains `fb2b75f18c8c5423f3a0850f38f6e53ee8249d3d08385978b5b2532041cc6089`, RNG `d8919406d6e01cf1`. The complete suite retains unchanged standalone v2 golden `ccae7836c085936ed962b9ba001764ae4252f135fdf62fa1fbe2c0ea0bcef07c`.

Exact final head/three-platform CI links are recorded in the draft PR and ignored local delivery record, avoiding a self-referential source SHA. No Unity Editor/native/desktop or merge was run/authorized.

**Required review before Stage B activation:** accept the equipment checkpoint; choose individual versus coordination mobility and downstream geometry/opportunity integration; contact boundary/shared-start policies and later-arrival batching; lawful follow/contact knowledge; source cloak concurrent-power semantics; automatic visible target ranking; joint commitment/save/report/stamp policy and facing initialization. Review may change the candidate formulas; silence is not agreement.

Stage B still needs genuine campaign-owned allocation/current resources, versioned opt-in routing, actual source/profile provenance, one-round production resolution with full compatible weapon rules, lawful stamped human/AI commits and persisted boundaries, shared authoritative travel and atomic multi-encounter aftermath. Its decisive proof is campaign Round 1 → save/load → changed Round 2/3 → finalize → following strategic turn/new encounter with depleted stocks, plus old/new determinism and exact-head CI. **None of the prototype fixtures or DTOs can substitute for that proof.**
