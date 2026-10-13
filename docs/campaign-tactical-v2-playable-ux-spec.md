# First playable campaign Tactical Combat v2 — reconciled UX specification

Reconciliation date: 13 October 2026. Status: **agreed player behavior; proposed interfaces and technical details; documentation only**. This supersedes the interface proposals in local revisions bb89826 and d4d12ae where they conflict with the dated lifecycle decisions. It does not implement or activate campaign v2.

Unity implementation waits for reviewed authoritative campaign lifecycle contracts and actual campaign-owned round → save/resume → finalization → successive-battle proof. Textual wireframes are proposals, not rendered or native acceptance. No production Unity/backend files, saves, runtime versions or gameplay are changed.

## Sources, checkpoints and precedence

This separate documentation branch retains local base 4e1a365fbae52591ddfc593200fae7b1f79a53db. The current accepted combined checkpoint is **53d2e64973d9081a4d0090e771f5e777fc55cf40**, verified by fetching origin/main during this reconciliation. Its tree matches the earlier reviewed combined candidate 22096fb56d81d203f194a6c0407d7de76721d93c. No branch was merged into this documentation branch.

Read together:

- Repository design context: [index](design/README.md), [ship design](design/ship-design.md), [strategic AI](design/strategic-ai.md), [time and movement](design/time-movement-mobilisation.md), [architecture](architecture.md), and [blueprint construction](blueprint-construction-v1.md). These relative links exist on this branch; their historical implementation statements do not establish new lifecycle support.
- **Latest agreed behavior:** the owner's amended [tactical chapter](E:/HobbyDS9/docs/design/tactical-combat.md) and [lifecycle milestone](E:/HobbyDS9/docs/campaign-tactical-v2-lifecycle-spec.md), both dated 13 October 2026. These primary-checkout inputs are modified/untracked and are not distributed by this branch. The relative [older tactical chapter](design/tactical-combat.md) remains historical context; the dated decisions consolidated below govern this revision. Earlier open statements about mutual cloak, next-encounter cloak initialization, command-unit identity and ordinary scan detail are superseded by the later explicit decisions.
- The [reconciliation assignment](E:/HobbyDS9/docs/prompts/campaign-tactical-v2-lifecycle-ux-reconciliation.md), [next-step plan](E:/HobbyDS9/docs/next-step-preparation.md), and earlier [product review](E:/HobbyDS9/docs/campaign-tactical-v2-ux-review.md) are local review inputs, not files included here. Use repository-relative links only after those target documents actually exist in the delivery branch.
- Accepted [combined report](https://github.com/AndreBal1983/HobbyDS9/blob/53d2e64973d9081a4d0090e771f5e777fc55cf40/docs/inspection-designer-combined-acceptance.md), read at that exact checkpoint. Its 1,358 .NET / 116 Unity tests, IL2CPP/native acceptance and unchanged determinism are recorded executions, not reruns here. Its original draft/unmerged wording is historical; the accepted merge checkpoint above supersedes it.
- Equipment delivery [report](https://github.com/AndreBal1983/HobbyDS9/blob/be02572fc3fc2cfdb90cdd9ad763bbdf3d79716b/docs/tactical-v2-authored-equipment-foundation.md) and [typed source definitions](https://github.com/AndreBal1983/HobbyDS9/blob/be02572fc3fc2cfdb90cdd9ad763bbdf3d79716b/src/HobbyDS9.Contracts/AuthoredTacticalEquipmentDtos.cs), pinned at **be02572fc3fc2cfdb90cdd9ad763bbdf3d79716b** (also the fetched equipment branch head). The report records 1,507 tests and Content 11 source support alongside legacy Content 10; Save 14 and the v1 path remain unchanged. The planning inputs describe this as PR #19 awaiting acceptance; this reconciliation does not merge or establish acceptance of it.
- Historical [standalone backend](https://github.com/AndreBal1983/HobbyDS9/blob/53d2e64973d9081a4d0090e771f5e777fc55cf40/docs/tactical-combat-v2-backend.md), [playground](https://github.com/AndreBal1983/HobbyDS9/blob/53d2e64973d9081a4d0090e771f5e777fc55cf40/docs/tactical-combat-v2-playground.md) and [inspection report](https://github.com/AndreBal1983/HobbyDS9/blob/53d2e64973d9081a4d0090e771f5e777fc55cf40/docs/campaign-tactical-combat-v2-integration.md). Their fixture targeting/protection and observation capabilities are not campaign permissions.

Read-input SHA256 identities: lifecycle milestone a61e29d88c7c2d77752803adf596ecfc323eefb0c7e86bfb4c678d0966358033; latest tactical chapter 1dee3914fdd69ebcba9b5be0b03684d9e04210c2f4a912941636dc6ebd24f4e8; assignment 69da4851cb4cd1eea498576046b9d61c29f55696cc15439be73569c99423451c. These identify local inputs, not committed source versions.

### Agreed behavior and remaining scope

**Agreed, 13 October 2026, source: latest tactical chapter and lifecycle milestone linked above:**

- One strategic fleet equals one tactical command unit. Same-route fleets remain separate. No automatic tactical wings, role-group splitting or merging.
- Player controls are fleet maneuver and enemy-fleet focus. Ships select suitable individual targets and contribute automatically from actual installed capabilities. Ship selection opens inspection, not firing orders.
- Screening protects ships within the same fleet only. Put escorts and transports together strategically. No allied-fleet protection picker or cross-fleet defensive contribution.
- Every new battle starts Distant between opposing fleet formations. Later rounds maneuver normally. Initial facings and mobility/aggregation formulas remain open; no universal tactical slowest-ship rule or label-based composition bonus.
- Focus is a preference among lawful firing targets. Automatic visible-hostile fallback preserves standing focus and is explained in reports; deterministic ranking remains open.
- Visible enemy ship types, hull condition and shield strength are shown. Enemy reserves and freshly selected orders stay hidden across planner, inspection, tooltips, reports and replay. Own equipment/resources remain fully inspectable.
- One strategic fleet cloak setting governs approach: capable ships cloak, incapable members stay visible. Cloaked ships have ordinary round-one fire against visible targets without offensive targeting of themselves. This is targeting protection, not an extra attack or round; ordinary resources/cadence/opportunities apply.
- Incoming torpedoes from hidden launchers can trigger ordinary automatic defensive reactions. No per-torpedo order, guaranteed interception or free capacity. All surviving cloaked ships reveal at round-one end regardless of firing and stay revealed through that encounter.
- Mutual full cloak produces maneuver-only round one, then reveal. It counts toward the three-round maximum; nonexistent offensive shots spend no ammunition or offensive firing opportunities.
- Survivors retain hull, four shield stocks and finite ammunition. No automatic between-battle recovery/refill. Meeting and engaging stops all participating fleets' remaining travel in the encounter sector; interrupted routes do not execute after battle. Other fleets are unaffected.
- Next-turn Planning accepts fresh strategic orders to leave, stay/re-engage or follow. Pursuit uses ordinary physical map movement and lawful same-sector/time engagement, with no separate catch roll or remote interception minigame. Mutual stay-and-combat produces a new encounter next turn with retained stocks, not a fourth round or same-turn loop.

**Preserved agreed initial scope:** authored opt-in v2 ships; basic destruction/three-round battles; no strategic retreat or objective exits during battle; no conquest/objective reward; no v1 blueprint conversion, refits, resupply economy, subsystem targeting or advanced tactical AI. Post-battle strategic leave/follow orders do not introduce within-battle exits.

**Proposed:** screen layouts, five-maneuver menu, exact display precision, reports/warnings, confirmation wording and saving committed state while retaining local unsent edits. Remaining formulas, cloak operational power/cost semantics, shared travel timing, version identities and persistence contracts are open. Broader anti-cloak counter-detection and strategic fog-of-war systems are outside this slice.

## Current capability versus required authority

| Area | Existing capability at inspected checkpoints | Missing campaign behavior / interface dependency |
| --- | --- | --- |
| Equipment sources | Equipment candidate validates complete authored fits, essential power reservations, fixed maxima and initial provisions; detached owned-source inspection | Initial provisions are content facts, not persisted current battle pools. No cloak capability/state support or live activation follows from source completeness |
| Round resolver | Standalone one-round resolution, standing orders, pair geometry, shields/ammunition, immutable events and three-round completion | Session-owned authorized rounds, fleet-local screening, cloak/visibility and agreed automatic targeting/mobility need accepted rules/contracts and independent vectors |
| Protection/target policy | Standalone supports designated allied-formation protection and ship-priority fields | Reject cross-fleet protection and exclude player priority controls on campaign path; do not silently change standalone goldens |
| Campaign checkpoint | V1 pending/buffered combat and atomic finalization exist | Saved current v2 pools/orders/visibility, filtered reports, joint commitments, stop-travel consequences and atomic v2 aftermath remain lifecycle work |
| Movement | Existing weighted strategic navigation exists | Physical intermediate same-sector/time contact, simultaneous travel boundaries and stop behavior require scoped authority, not a UI chase calculation |
| Unity | Accepted live v1 designer/publication/construction and existing v1 reports | This document supplies no v2 runtime interface, new command or native evidence |

No fictional API names or signatures are supplied. The actions below identify required authoritative behavior only. Do not submit combatant DTOs, opponent orders, geometry, stocks, seeds or results as player commands. Application presents detached lawful facts; Simulation validates ownership, phase, stamps, permitted orders, resources, replay and outcome.

Basic Defeat objectives alone do not prevent standalone Disengage extraction. Simulation must reject all excluded exit paths, including initial, loaded, retained and replacement orders. Menu hiding is insufficient. Fighting Withdrawal changes tactical range while staying in battle. Cross-fleet defensive access must be prevented in acquisition, interception and consequences, not merely omitted from the order payload.

Engagement must halt remaining participant travel at the actual contact point, before it can execute farther along the route. The checkpoint records that interruption; finalization preserves the halt and prevents the old route from becoming executable again. The legacy end-of-Movement-only detector does not prove this behavior. A shared authoritative travel/contact timeline is a lifecycle activation gate, not a post-battle UI cancellation or a client-side chase check.

## Proposed player actions and their authority

| Proposed UI action | Required authoritative behavior; no API promised |
| --- | --- |
| Enable fleet approach cloak in strategic Planning | Own controllable fleet, observed stamp and legal Planning phase; persist setting and derive per-ship eligibility from explicit equipment and accepted operational rules. Never infer capability from names |
| Select own fleet / inspect ship | Read permitted immutable facts; no mutation, RNG, target assignment or stock change |
| Draft fleet maneuver / enemy-fleet focus | Local edits bound to session, actor, encounter and round. Backend supplies lawful choices; no protection destination or player ship-priority field |
| Review and commit round N | Validate complete permitted fleet-order replacements, preserve other standing fields, jointly commit lawful opposing choices and resolve exactly one round. Reject before mutation/RNG; no automatic refresh/retry |
| Retain orders | Explicit commitment of the next round with current standing intent; do not reselect targets in Unity or pre-resolve later human choices |
| Save committed battle state | Export supported stable authoritative boundary; proposed host behavior retains local unsent edits but does not serialize or commit them |
| Load boundary | Validate fully before replacing session; restore committed resources/orders/round/setting once. Successful replacement drops old drafts; failed load retains session/draft |
| Complete battle turn | Once all encounters complete, atomically apply current survivor pools/destruction and travel stop, clear batch, complete existing strategic turn once and open next Planning |
| Leave / stay and seek combat / follow next turn | Fresh strategic orders through actual map access/knowledge/engagement rules; no same-turn continuation, teleport, catch-roll modal or hidden route disclosure |

The strategic cloak control is an agreed player setting with a proposed UI. It is not a mid-battle re-cloak button. Show current accepted setting separately from an unsent strategic change and per-ship current visibility. Zero eligible members cannot gain capability through enabling it; eligibility/reasons must come from authority.

## Annotated wireframe A — strategic approach and round planner

Proposed 1920 × 1080 layout. Illustrative names and values; brackets identify annotations.

```text
+--------------------------------------------------------------------------------+
| STRATEGIC FLEET A [1] Approach cloak [Enabled]  Capable members: own facts only  |
| Applies to next encounters. No recovery/refill. [Review strategic change]       |
+--------------------------------------------------------------------------------+
| OPT-IN V2 | Border sector | Round 1 / up to 3 | Battle turn incomplete          |
+-----------------------+--------------------------------+-----------------------+
| [2] YOUR FLEETS        | [3] SELECTED FLEET / RELATION   | [4] FLEET ORDERS       |
| > Fleet A             | Fleet A -> visible Fleet X     | Standing: Hold & Fire |
|   cruiser + transport | Opening range: Distant         | Maneuver [Screen v]   |
|   Fleet B             | Own exposure: reported facing  | Focus [Fleet X v]     |
|   separate escort     | Enemy types / hull / shields   | Unsent change         |
|                       | [Inspect visible ship]         |                       |
| VISIBLE HOSTILES      | No enemy reserves or new orders| Same-fleet screening  |
| Fleet X [observed]    | Abstract geometry, no steering | [Composition details] |
+-----------------------+--------------------------------+-----------------------+
| [5] Own ship inspection: hull | Fore | Port | Starboard | Aft | finite magazines |
| Orders persist. Focus is preference, not guaranteed fire or subsystem targeting.|
+--------------------------------------------------------------------------------+
| [Back to campaign] [Save / Load] [Discard edits]          [Review round 1 orders]|
+--------------------------------------------------------------------------------+
```

1. Strategic strip illustrates the separate Planning control. At an active checkpoint it is read-only accepted approach setting, not an executable mid-battle change. All cloaked survivors reveal at the round-one boundary without clearing the approach setting.
2. Fleet A's ships are one command unit. Fleet B remains separate even if it follows the same route. No automatic escort wing or transport formation is created.
3. Every opposing pair starts Distant; later pairwise range/exposure comes from the backend. Enemy inspection reveals permitted visible types, hull and shield strength, never magazines. Facing initialization/display precision remain proposals.
4. Only maneuver and enemy-fleet focus are player tactical controls. Screen is fleet-local. Composition details explain actual contributing equipment, defensive opportunities used and lost offense; they do not assign buffs by class names.
5. Exact own per-ship inspection remains available. Fleet condition summaries do not replace four directional pools. Lists and details scroll independently; footer stays reachable.

Proposed initial maneuvers: Maintain Distance / Hold & Fire, Charge, Flank, Screen, Fighting Withdrawal. Pattern aliases may accompany practical names as proposed presentation only. Pursuit's inclusion as a tactical menu item remains D5; post-battle strategic following is already agreed and is a different action. Breakthrough, Disengage, objectives/retreat destinations and individual firing/power controls are absent and authoritatively excluded.

A preferred-range field is not part of the main controls. The campaign must determine any needed range-policy inputs under reviewed rules; standalone defaults or omitted DTO fields cannot become accidental player policy. Review lists each owned active fleet's retained versus proposed maneuver/focus. Complete replacements follow the eventual lifecycle contract, not invented partial patches.

## Annotated wireframe B — fleet focus and no visible enemy

```text
+------------------------------------------------------------------------+
| [1] Fleet A — preferred enemy fleet for round 2                         |
| (*) No fleet preference                                                |
| ( ) Fleet X [visible] — legal preference; fire access may change        |
| ( ) Fleet Y [visible] — legal preference; not a guaranteed shot         |
| [2] Standing focus unavailable — retained; fallback is automatic       |
| Individual visible ships: [Inspect condition] — no firing selector     |
| [Cancel]                                         [Use in order draft]  |
+------------------------------------------------------------------------+
| [3] NO VISIBLE ENEMY                                                   |
| Possible attack. Proceed cautiously; standing orders are unchanged.    |
| No hidden names, ship counts, fleet counts or stock/position details.   |
| Choose your fleet maneuver; commit only through the normal review.     |
+------------------------------------------------------------------------+
```

1. The focus picker contains lawful visible hostile choices from authority. A legal preference can currently lack firing opportunity; future maneuver, range/arcs, screening, power, cadence and ammunition still constrain acquisition. Unity does not compute them.
2. Destroyed/unavailable focus is preserved. Automatic selection may use another visible hostile with valid targets; it does not overwrite standing focus. Report the lawful reason and observed alternate target. Where no valid target exists, no shot is manufactured. “No preference” is an explicit player clear, not an automatic fallback rewrite. Previously known identity is shown only to the extent permitted; unavailability must not confirm hidden survival/location.
3. Warning wording is proposed; caution is agreed. The authoritative encounter may contain hidden opponents, but the player projection must not reveal their identities/counts. Do not substitute Screen, add defensive opportunities or disclose hidden launchers when incoming ordnance appears. No strategic detection/tracking minigame is introduced.

## Annotated wireframe C — individual condition, composition and visibility

```text
+------------------------------------------------------------------------+
| YOUR FLEET A / CRUISER [1] Exact authored fit and current round boundary |
| Hull 220 / 220 | Armor 10 | Own power/reservations [Details]             |
|                       FORE 18 / 40                                     |
|          PORT 25 / 25   [ship outline]   STARBOARD 12 / 25               |
|                       AFT 0 / 20                                       |
| Torpedo launcher: 7 / 12 [2] | Beam/Cannon: no physical ammunition       |
| Cadence / power / coverage: backend-reported readiness [Details]        |
| Approach cloak: Enabled | Current state: [backend status]               |
| [3] Fleet Screen contribution: actual attempts/successes and limits     |
+------------------------------------------------------------------------+
| VISIBLE ENEMY SHIP [4] Type: observed | Hull condition: observed         |
| Shield strength / facing detail: permitted observation                  |
| Ammunition reserves: not disclosed | Upcoming orders: not disclosed     |
| [Close inspection]                                                     |
+------------------------------------------------------------------------+
```

1. Cruiser values are the equipment report's **provisional source facts**, not a live battle capture. PR #19 supplies initial Fore/Port/Starboard/Aft values and caps; it does not expose persisted current v2 combat stocks. A future live inspector must label actual current boundary data and never refill it from source provisions.
2. Own magazines are per installed package/munition. Energy weapons have no physical ammunition; no fictional infinite torpedo pool. Show out of ammo, unpowered, cadence and no firing opportunity as different backend-reported states.
3. A capable cruiser/capital ship may contribute screening, not only an Escort-labelled hull. Fast escorts keep useful individual tactical mobility; the exact model is open. Show actual contribution/finite opportunity costs and saturation. Fleet B cannot contribute defense to Fleet A merely by ownership, route or proximity.
4. Enemy types/hull/shield strength are agreed visible facts; exact precision is D4. Hide reserves, powered-fit internals not granted by observation, private orders and unrevealed ships. Own cloak eligibility here is illustrative future data: PR #19 has no authored cloak capability.

Four sectors remain ship-relative, not camera-relative. Display exact own current/max with text and bars; zero capacity is 0/0 without percentage division. No shield redistribution, manual firing, repair, refill, profile swapping or re-cloak control exists in battle. In-battle recharge follows accepted pinned rules only; it does not transfer intact sector points or create between-battle recovery.

## Annotated wireframe D — resolved round report and reveal

```text
+--------------------------------------------------------------------------------+
| ROUND 1 RESOLVED [1] | Battle ongoing | Round 2 awaits orders                     |
| Maneuver: authoritative pairwise range / exposure changes                       |
| Own Screen: attempts / successes / penetrating threats, no generic bonus        |
| [2] Incoming torpedo intercepted; launching ship unavailable in that observation|
| Own torpedoes: actual launches and retained stock | Enemy launches: observed only|
| [3] Round-one reveal: surviving formerly cloaked ships are now visible           |
| [4] Focus fallback: Fleet X unavailable; visible Fleet Y acquired where legal    |
| Standing focus: Fleet X (retained)                                               |
+--------------------------------------------------------------------------------+
| EVENTS [expand] | Ship / fleet filter [visible permitted entries only]           |
| Original sequence order, filtered authoritative rows, no hidden stock fields    |
+--------------------------------------------------------------------------------+
| [Back to campaign] [Save / Load] [Inspect stocks]                   [Plan round 2]|
+--------------------------------------------------------------------------------+
```

1. Summaries show observed maneuver/fire/actual shield/hull loss, armor and destruction; attempted yield, landed damage and actual hull loss differ. No local combat math or stock change drives the report. Own defensive attempts, including failures, consume real firing opportunities.
2. Incoming ordnance may be a defensive target without its launcher being an offensive target. Report an observed incoming threat without hidden launcher identity or remaining enemy reserves. Defensive reactions are automatic and constrained by actual equipment/coverage/power/cadence/opportunities.
3. Reveal occurs at the authoritative round-one end even if neither side fired. Use the boundary's newly permitted enemy type/hull/shield observations; do not retroactively reveal concealed launcher/private fields in earlier events. No UI timer or animation decides targetability.
4. Explain visible-target fallback while retaining focus. If no eligible shot occurred, say so without leaking hidden candidates. Filters/tooltips/replay/debug details presented to players must obey the same observation boundary. Do not dump a full standalone event trace.

Playback/skip/report acknowledgment is presentation only, never a second RNG draw, launch or recharge. Round limit means survivors remain, not a fabricated winner. No round 4. Enemy-reserve privacy holds after reveal and after completion. Fresh opposing commitments cannot be disclosed for counter-picking.

## Annotated wireframe E — committed save with local edits retained

```text
+------------------------------------------------------------------------+
| SAVE CAMPAIGN — round 1 resolved; round 2 ready                         |
| Saved data: committed boundary, orders and persistent stocks           |
| [1] Unsent changes stay in this running session but are not saved.      |
|     Successful load or restart loses them.                             |
| [Cancel]                                [Save committed battle state] |
+------------------------------------------------------------------------+
| LOAD CONFIRMATION: valid boundary restores round 2 ready                |
| [2] Successful replacement discards this session's unsent edits.        |
| [Cancel]                                                   [Load slot] |
+------------------------------------------------------------------------+
```

1. **Revised UX proposal, still pending interaction review:** saving committed state retains the local draft, does not commit it and does not require discard. Failed save retains prior valid file, live campaign and edits. Success leaves the draft labelled Unsent, not Saved order. Discard is a separate deliberate action. Slot overwrite confirmation remains separate.
2. Successful load/restart restores committed state only. Failed validation/load preserves old session/draft/reports. Do not match old drafts to replacement sessions by coincident IDs. Navigation/resize preserves local edits; changing session/actor or quitting warns appropriately. Save-and-quit warns of unsent-edit loss and stays open on failed save.

Accepted v1 save behavior and standalone boundary saves do not constitute accepted **campaign-v2** persistence. The required lifecycle must preserve current pools, standing focus/maneuver, approach setting, round/completion/visibility, exact fits/identities, encounter-local RNG, all pending/staged encounters and replay protection. Current-batch report retention, waiting-for-human boundaries and exact version identities remain open. Save 14/Content 11 source support does not reserve the live lifecycle versions.

Disable session replacement while a round transaction is in flight; reconcile an ambiguous response through authoritative refresh, never automatic retry. No future human rounds are pre-resolved. Saved round 1 resumes round 2 without replaying round 1 or granting recharge/reveal again.

## Annotated wireframe F — aftermath and fresh strategic orders

```text
+--------------------------------------------------------------------------------+
| ENCOUNTER COMPLETE — ROUND LIMIT 3 / 3 | Survivors remain in encounter sector     |
| [1] Actual retained hull / four shields / ammunition | AFTERMATH PENDING          |
| Participating travel stopped; previous routes will not resume                   |
| Other encounters: authoritative status [Next encounter]                         |
| [Save checkpoint] [Complete battle turn: enabled only when batch is ready]       |
+--------------------------------------------------------------------------------+
| AFTER ACCEPTED ATOMIC COMPLETION — NEXT PLANNING                                 |
| [2] Fleet A: same sector, retained stocks, no extra same-turn movement            |
| Previous interrupted route [reference only]                                     |
| Fresh strategic order: [Leave via lawful route] [Stay / seek combat] [Follow]    |
| [3] Follow uses map movement and lawful contact knowledge, not a catch roll      |
| Approach cloak setting: retained; capability/conditions checked next encounter   |
+--------------------------------------------------------------------------------+
```

1. Until finalization, label stocks as staged results. When every encounter is complete, atomic authority applies all survivor pools/destruction and travel stop once in canonical order, clears the batch and opens next Planning. Failure preserves every staged result and the whole pre-finalization campaign; do not re-resolve completed encounters.
2. Sector retention, travel stop and fresh next-turn orders are **agreed**, no longer an unanswered “return-to-campaign” choice. Old routes are non-executable; optional reference/explicit new-order proposal remains UI detail. Other fleets are unaffected. Finalization does not advance calendar twice, restore movement, heal or replenish.
3. Follow/leave/stay are proposed presentations of agreed strategic behavior. Physical same-sector/time contact and lawful engagement are required. No departure catch roll, remote route contest, invisible destination, teleport or path-crossing-only encounter. A slower pursuer may fail to reach contact. Both leaving use lawful routes. Mutual stay-and-combat gives a new next-turn encounter, beginning Distant with retained pools and approach cloak applied again.

Shared travel timeline, simultaneous arrival/departure, weighted-edge occupancy and precise contact/engagement commands remain backend gates (D1), not a UI minigame or decided formula. Do not present these strategic controls as implemented while that lifecycle is absent.

## Worked examples and acceptance cases

All campaign examples below are **illustrative required behaviors, not executed runtime acceptance**. Equipment report facts are identified separately; no new numeric balance or formulas are approved.

### Example 1 — same route, separate fleets

Fleet A contains a transport. Friendly Fleet B contains capable escorts. They share owner, route and encounter sector but retain distinct identities/maneuver/focus. Fleet B's Screen cannot protect Fleet A's transport. No protection picker appears and no silent merging or escort wing occurs. Opening pairs are Distant. Acceptance must check actual defensive access/results, not just absent controls.

### Example 2 — same-fleet escorted transport and composition

Before battle, the player strategically places the transport and capable escorts in Fleet A. One Screen order lets suitable powered packages contribute within that fleet automatically, including suitable cruiser hardware. If a hypothetical accepted report records four interception attempts and three successes, show 4 attempts / 3 successes, finite opportunities spent (including failure), and any penetrating threats; do not turn it into a class-label percentage buff.

Use the equipment candidate's provisional cruiser as an inspection example: reactor 100, distribution 80, essentials 26, equipment budget 54; allocations 36 leave 18 unused. Its hull 220, shield caps 40/25/25/20, initial shields 18/25/12/0 and launcher 7/12 are authored source facts. They neither prove a live escort model nor imply fully recovered pools. Tactical speed 70 is independent of strategic movement; a universal minimum-speed/average aggregation is not adopted. Mobility and contribution formulas must be reviewed with actual fits.

### Example 3 — automatic focus fallback and hidden reserves

Fleet A retains focus X. At a firing opportunity, X is destroyed or supplies no lawful target; visible hostile Y has an eligible target. Backend acquires Y and reports the lawful reason, while standing focus stays X. It never acquires hidden Z or reveals Z's identity/count. Individual ship choice is automatic; no player priority dropdown.

An observed enemy torpedo launch may appear in the report. The player sees the visible enemy's type, hull and shield strength, but no “remaining ammo,” inferred magazine fill bar, private reserve total or fresh order. Own stock display remains exact. Fallback ranking and exact visible condition precision remain open.

### Example 4 — fully cloaked fleet versus visible fleet

Fleet A's accepted strategic cloak setting is on and every member has future explicitly authored cloak capability. Visible hostile Fleet X is a lawful offensive target. Round one begins Distant; any eligible attacks are ordinary round-one attacks, consuming normal power/opportunities/cadence/ammunition. Fleet X cannot offensively target A's cloaked ships. Incoming torpedoes may trigger X's ordinary defenses without revealing their launchers.

A's ships stay protected through round one even after firing. At its end all surviving A ships reveal, whether they fired or not. Round two permits ordinary targeting. There is no extra volley, round or refill, and no re-cloak control. PR #19 currently provides no cloak capability: this case requires new accepted source/state/resolver support.

### Example 5 — mixed cloak within one fleet

Fleet A has one capable cloaked ship and one incapable visible transport; it stays one command unit. The transport can be targeted normally in round one. The hidden member may fire at visible enemies and contribute allowed same-fleet defense under accepted rules; its presence must not make the transport invisible or disclose the hidden member via fleet totals. A visible member must not reveal all hidden members. Capability/defensive eligibility is authoritative, not inferred from a role label. All surviving hidden members reveal at round-one end.

### Example 6 — mutual cloak and caution

Both opposing sides are fully cloaked. Their player projections show no visible enemy, not a fabricated named/contact-count list. Show the proposed caution message and retain maneuver/focus. Round one maneuvers with no offensive targets or shots; no offensive ammunition/opportunities/hit rolls are spent on nonexistent fire. Both reveal at the end; rounds two/three fight normally from authoritative resulting geometry. Round one counts toward the same three-round maximum, with no compensating round. Costs for cloak or other non-offensive operations remain separate open rules.

### Example 7 — saved round boundary with unsent changes

After round 1, reveal/stocks/standing focus are authoritative. The player drafts a different round-2 maneuver, then saves committed state. Proposed host behavior keeps that draft locally; the file excludes it. Failed save or failed load preserves the running draft and prior valid state/file. Successful load/restart resumes round 2 with accepted orders only, without a second reveal/recharge or reconstructing stock from initial provisions. Given identical subsequent orders, uninterrupted and resumed campaign events/pools/RNG must match. This is a future lifecycle acceptance case, not existing Save-14 proof.

### Example 8 — successive depleted battles, travel stop and pursuit

Illustrative survivor boundary: hull 90/100; fixed caps 40 each; Fore/Port/Starboard/Aft current 3/13/40/5; launcher 0/3. After round 3, no final-round recharge occurs under preserved compatible v2 rules. Atomic finalization retains exactly those values and stops participant routes at the encounter sector. Fresh next-turn choices are leave, stay/re-engage or follow; no same-turn movement resumes.

If both hostile survivors stay and choose combat, next-turn authority allocates a new encounter with those depleted values, not 100 hull/full shields/three torpedoes. Opening range is Distant. Persistent approach cloak may apply to capable survivors again; it changes no stocks. Ordinary in-battle recharge may occur at allowed unfinished transitions, not between encounters.

If one leaves and another follows, physical shared travel determines contact: a faster fleet can only catch where actual routes/budgets/time permit; a slower one cannot get a compensating roll. Mutual departure follows normal routes. Acceptance must demonstrate mid-turn engagement stopping remaining travel and simultaneous arrival/departure policies, not only equal end positions. Exact travel timing is still open.

## D1–D8 decision register, reconciled

Each **agreed** entry below is dated 13 October 2026 and sourced to the latest tactical chapter/lifecycle milestone. Partial resolution must not be relabelled wholly open or wholly implemented.

| ID | Agreed behavior now resolved | Remaining proposed/open work |
| --- | --- | --- |
| D1 Aftermath/contact | Same-sector survivors, retained pools, stopped participating travel, fresh next-turn orders, physical strategic pursuit, mutual next-turn re-engagement; no same-turn repeat/catch roll | Shared travel/event timing, simultaneous arrival/departure, weighted-edge occupancy, lawful follow/engagement details and multi-encounter atomic conflicts |
| D2 Command/composition | One fleet/one formation; separate same-route fleets; fleet-local screening; automatic meaningful individual contributions; every new battle Distant | Mobility/capability aggregation and screening formulas, initial facings, exact rules/resolver versions and mixed v1/v2 routing. No automatic wing or universal tactical bottleneck-speed assumption |
| D3 Focus/targeting | Fleet focus only; automatic suitable individual targets, visible-hostile fallback, retained focus and report explanation; no protected allied-fleet target | Deterministic fallback/within-fleet ranking, no-focus default policy and precise authoritative unavailable-focus reasons. Remove obsolete player ship-priority and protection decisions |
| D4 Observation/cloak | Visible types/hull/shields, concealed enemy reserves/fresh orders; strategic approach setting, per-ship mixed cloak protection, ordinary round-one fire, defensive reaction, end-R1 reveal, mutual maneuver-only R1 | Precision/other scan detail, explicit cloak capability/power/cost/state contracts, permitted geometry visibility and baseline opponent policy. No new strategic fog-of-war or advanced counter-detection implementation implied |
| D5 Orders/menu | Simple fleet maneuver plus enemy focus; persistent intent; no per-ship fire management or within-battle exit orders | Five-maneuver menu/aliases, tactical Pursuit inclusion and backend automatic preferred-range policy; strategic following is already agreed |
| D6 Save/resume | Required authoritative round-boundary resume without recovery/refill; committed standing intent/approach behavior must survive appropriate lifecycle boundaries | Exact save/content/rules identities, ledger/commit/waiting boundaries. Saving committed state while retaining unsent local draft remains UX proposal, not accepted v2 persistence |
| D7 Reports | Apply observation/privacy and fallback explanation to all player reports/replay; never rerun combat for display | Current-batch event/report retention and compact projection schema, finalization history handling. Proposed current-batch saved reports; no permanent campaign archive |
| D8 Readability | Own exact pools inspectable, visible enemy condition available under agreed observation limits | Warning thresholds, report density, precision, confirmation wording and responsive/focus details remain interface proposals |

Equipment acceptance and reviewable mobility/cloak/visibility/travel contract proofs precede lifecycle activation. Backend rounds, current pools, simultaneous commitments, filtered projections, save boundaries and atomic aftermath must be proven before Unity. Documentation can proceed in parallel; it does not require unmerged API names or dispatch another chat.

## Responsive and future acceptance matrix

| Size | Proposed layout | Review criterion; no rendered/native claim |
| --- | --- | --- |
| 1280 × 720 | Compact fleet list; tabbed relationship/orders/inspection; separate scrolling detail/report; fixed footer | Maneuver/focus, Back, Save and review/commit reachable; all four own sectors and privacy-safe enemy condition readable |
| 1920 × 1080 | Wireframe A columns with inspection/report drawers | Retained versus unsent intent clear; no protection/priority controls; own pools and reveal/fallback legible |
| 3440 × 1440 | Bounded readable controls, wider pair/report area | No stretched tiny text or detached context; identical authoritative behavior |

Use text/shape as well as LCARS status colors. Selection, scrolling, keyboard focus and resizing cannot submit a command. Enter in an edit field cannot commit a round. Preserve local drafts across tabs. No scientific/gameplay value is calculated by the presentation.

Future acceptance must prove all eight examples against campaign-owned authority, including direct cross-fleet protection rejection and excluded exit paths even under Defeat, mixed-source refusal where unsupported, hidden reserve/order/launcher privacy in every player projection, legal automatic fallback, cadence/stock/overflow handling, early/mutual destruction, stale/repeated commands, failed save/load and full later-encounter aftermath rollback. Preserve legacy v1 and standalone-v2 goldens; new lifecycle rules receive independent vectors. Only then exercise actual Unity/native controls at all three sizes, reporting CI separately from local runtime evidence.

## Documentation delivery boundary

This revision changes only this specification on codex/campaign-tactical-v2-ux-spec. Local source hashes, pinned links, decision consistency, six annotated wireframes, eight worked examples and the diff are checked. Existing gameplay acceptance was read, not repeated. No production edits, desktop tools, publication/push, merge, other-chat messaging or Unity runtime work is part of this assignment.
