# First playable campaign Tactical Combat v2 — UX specification

Review date: 13 October 2026. Status: **proposed interface; documentation only; design discussion required before implementation**.

This specification describes the first campaign-owned, interactive v2 slice. It is not an acceptance report or evidence that campaign v2 is playable. The annotated wireframes are proposed layouts, not screenshots. No production Unity files, backend contracts, serialized versions or gameplay are changed.

## Review basis and decision status

The documentation branch starts at local main `4e1a365fbae52591ddfc593200fae7b1f79a53db`. This is a recorded local checkpoint, not a claim about current remote main or the acceptance of incoming branches. Runtime capability statements below distinguish this base from the separately inspected v2 source checkpoint.

Read together:

- [Design index](design/README.md), [tactical combat](design/tactical-combat.md), [ship design](design/ship-design.md), [strategic AI](design/strategic-ai.md), [time and movement](design/time-movement-mobilisation.md), and [exploration/repair direction](design/exploration-missions.md).
- [Architecture](architecture.md), [blueprint construction v1](blueprint-construction-v1.md), and [existing combat orchestration/reports](combat-ux-report-fix-v1.md). Historical version/count statements in these documents remain historical.
- The local [next-step plan](E:/HobbyDS9/docs/next-step-preparation.md), dated 13 October 2026, and its [equipment preparation prompt](E:/HobbyDS9/docs/prompts/tactical-v2-authored-equipment-foundation.md). These were untracked planning inputs in the primary checkout; this branch does not copy, revise or dispatch them. Plan SHA256 when read: `366d996d84cd229356e7db2fa1bc7ea6ef947daa724d3ae0ff65fb20df736fb6`. The agreed product constraints are restated below so review does not depend on that local file being present.
- Pinned incoming source checkpoint `27ed955296b2a4ce8b86f1b65d663c389023e99e`: [standalone backend](https://github.com/AndreBal1983/HobbyDS9/blob/27ed955296b2a4ce8b86f1b65d663c389023e99e/docs/tactical-combat-v2-backend.md), [console playground](https://github.com/AndreBal1983/HobbyDS9/blob/27ed955296b2a4ce8b86f1b65d663c389023e99e/docs/tactical-combat-v2-playground.md), and [campaign inspection/migration report](https://github.com/AndreBal1983/HobbyDS9/blob/27ed955296b2a4ce8b86f1b65d663c389023e99e/docs/campaign-tactical-combat-v2-integration.md). Actual [contracts](https://github.com/AndreBal1983/HobbyDS9/blob/27ed955296b2a4ce8b86f1b65d663c389023e99e/src/HobbyDS9.Contracts/TacticalCombatV2Contracts.cs), [order validation](https://github.com/AndreBal1983/HobbyDS9/blob/27ed955296b2a4ce8b86f1b65d663c389023e99e/src/HobbyDS9.Simulation/TacticalCombatV2State.cs), and [one-round resolver](https://github.com/AndreBal1983/HobbyDS9/blob/27ed955296b2a4ce8b86f1b65d663c389023e99e/src/HobbyDS9.Simulation/DeterministicTacticalCombatV2Resolver.cs) were inspected for the semantics below. Their presence on that branch does not establish their presence or acceptance on this documentation base.

**Agreed slice constraints:** a separate, explicitly opt-in scenario with authored v2 ships; finite persistent ammunition and four directional shield stocks; persistent hull damage; no automatic between-battle recovery or ammunition refill; existing v2 in-battle recharge; destruction/basic battles with a maximum of three rounds; no strategic retreat, Breakthrough or other objective exits. Existing v1 ships and designer-built v1 blueprints stay on their supported path, without inferred conversion.

**Proposed here:** screen structure, labels, order editing/review, target picker behavior, report presentation, save prompts and completion flow. **Open:** the decisions in the discussion register. Examples supply illustrative data or explicitly identified standalone evidence, never approved campaign balance.

The broader design agrees future objectives and cinematic visualization. Their exclusion from this slice is deliberate phasing, not a reversal. The older design/architecture descriptions of encounter-local resetting shields apply to v1; they must not be copied into the opted-in persistent v2 path. Recovery proposals in the migration report do not override the owner's narrower no-recovery decision. The campaign allocator and order validator must enforce basic Defeat objectives and prohibit extraction/objective exits for this scenario; hiding UI controls cannot constrain a standalone resolver that still supports those paths.

## Capability and dependency map

| Area | Existing capability at the inspected checkpoint | Required campaign dependency / proposed UX |
| --- | --- | --- |
| One round | Pure `ResolveRound` accepts an explicit standalone encounter and joint replacements, returns detached state and immutable events | Session-owned encounter and human-authorized round transaction; the UI must not submit a fixture/result as campaign authority |
| Standing orders | Formation `StandingOrder`; omitted formations retain orders; supplied orders replace all fields | Campaign persistence and lawful actor-scoped observation; UI shows retained versus unsent changes |
| Targets | Optional enemy formation preference plus `LowestId`, `LowestHullRatio`, `CommandSupport`, `Screen`; optional allied Screen protection | Campaign knowledge/ownership/side validation and advisory legal choices; no locked individual-ship target API exists |
| Geometry | Pairwise categorical range and both facings; provisional maneuver contests, arcs and screening | Authored capabilities, accepted formation mapping and initial geometry; no camera coordinates determine access |
| Stocks | Four current/max shield sectors, hull, weapon-level finite torpedo ammunition, cadence and recharge in standalone state | Persistent campaign resources bound to exact authored fits, frozen encounters and atomic aftermath |
| Completion | `Decisive`, `Objective`, `RoundLimit` in the standalone resolver | Campaign routing must restrict this slice to basic battles; disable all extraction/objective paths, not just their buttons |
| Inspection | Incoming source-inspection capability inventories real v1 sources; activation remains blocked | Equipment foundation must provide complete authored v2 data; completeness alone must not enable live rounds |
| Save/resume | Standalone host round-boundary saves; existing campaign v1 pending/buffered checkpoint saves | Versioned campaign v2 boundaries, standing orders, resources, local RNG and report retention policy; standalone JSON is not a campaign save |
| Campaign completion | V1 stamped buffering and atomic batch finalization exist | V2 batch validation/aftermath must commit survivor resources and destruction exactly once; existing v1 operations are not automatically v2 APIs |
| Unity reports | Existing v1 planner, phase reports and recent client-memory history | New presentation of actual campaign v2 observations/results; no v2 UI is implemented by this document |

No new API names, method signatures, command discriminators, error enums or save-version numbers are specified here. Phrases such as “commit round” and “complete battle turn” below are proposed user actions and required behaviors, not repository APIs. Backend contracts must be reviewed separately before a Unity implementation prompt is written.

## Player journey and state boundaries

1. Explicitly choose the opt-in scenario. Read that damage and ammunition carry into later battles and resupply/recovery is absent in this slice. Normal campaign selection remains normal v1 behavior.
2. At an authoritative encounter checkpoint, read location, participants, current round and owned formations. Open the round planner without executing gameplay.
3. Retain or edit owned formation orders using lawful start-round observations. Inspect own ships and known contacts. Review all changed orders before commitment.
4. Commit **one** round. Backend gathers lawful opposing decisions without revealing or reacting to newly submitted human choices, validates and resolves. Disable repeated submission while awaiting the authoritative response.
5. Read the round report and boundary stocks. If unfinished, retain or revise orders for the next round. Save/resume at a supported stable boundary.
6. When an encounter ends, review its outcome. If other encounters remain, proceed to them. Only the backend's complete batch permits final campaign completion and return to Planning.

| Screen state (presentation terms) | Primary action | Meaning / restrictions |
| --- | --- | --- |
| Equipment/lifecycle unavailable | Review reason / Back | No “Play v2” fallback through a fixture or v1 conversion |
| Round ready, no unsent edits | Review retained orders | Does not mutate standing orders or advance a round |
| Round ready, edited | Review changes | Local editing only; switching tabs preserves edits for this encounter/actor/boundary |
| Order review | Commit round N | Explicit single-round commitment, including unchanged retained orders |
| Awaiting authoritative response | Disabled commit; show progress | No Save/Load replacement racing a transaction; never retry automatically |
| Round report, unfinished | Plan round N+1 | Opens editing, does not resolve the next round |
| Encounter complete, batch unfinished | Next encounter | No round 4 and no strategic End Turn |
| All encounters complete, aftermath pending | Complete battle turn | Explicit atomic campaign completion, not report acknowledgment |
| Finalization rejected | Review reason / Refresh | Remain at checkpoint; completed encounters are not re-resolved |
| Finalization accepted | Return to campaign | Display authoritative Planning state; do not call another End Turn |

Persistent header: scenario mode, location, actor, encounter, **round N of up to 3**, and “Battle turn incomplete” until finalized. Technical stamps/IDs are available in an expandable diagnostic panel, not the primary player flow. Do not describe one tactical round as a week or advance the strategic calendar during report playback.

## Annotated wireframe A — entry and round planner

**Proposed standard layout, 1920 × 1080.** All numbers/names in wireframes are illustrative. Bracketed numbers refer to annotations, not controls.

```text
+----------------------------------------------------------------------------------+
| [1] OPT-IN V2 CAMPAIGN | Border region | Round 1 / up to 3 | Battle turn incomplete |
+---------------------+-----------------------------------+------------------------+
| [2] YOUR FORMATIONS  | [3] TACTICAL RELATIONSHIPS          | [4] ORDERS: Escort     |
| > Escort    2 ships | Escort -> Attack wing             | Standing: Hold & Fire  |
|   Transport 1 ship  | Range: Engagement                 | Maneuver [Screen    v] |
|                     | Own facing: Fore                  | Range [Engagement   v] |
| CONTACTS            | Contact facing: [known value]     | Protect [Transport  v] |
| Attack wing [known] |                                   | Priority [Damaged   v] |
| Second contact [?]  | ABSTRACT RELATION, NOT A MAP       | Prefer [Attack wing v] |
|                     | No drag-to-steer or distance ruler| [5] Unsent change      |
+---------------------+-----------------------------------+------------------------+
| [6] Escort stocks: Hull / Fore / Port / Starboard / Aft / torpedo stocks [Inspect] |
| Orders persist until changed. Target preference does not guarantee a firing shot. |
+----------------------------------------------------------------------------------+
| [Back to campaign] [Save / Load] [Discard edits]             [Review round 1 orders]|
+----------------------------------------------------------------------------------+
```

1. Entry briefing uses “Authored v2 scenario — persistent damage and ammunition; no resupply or between-battle shield recovery.” Activation is backend-gated, not inferred from the scenario's display name.
2. Only actor-owned, active formations receive editable orders. Other participants are read-only contacts from lawful observations. Authored sides, rather than faction names, determine hostility. Formation creation/splitting is outside this slice.
3. Select a pair to show its range and both facings only to the extent lawfully observed. Multiple hostile formations require a pair list; one global range or one facing arrow cannot represent every opponent. Decorative positions cannot select firing arcs.
4. Show practical maneuver names first. Optional pattern callouts from the design chapter remain secondary proposed aliases, not additional buffs. Details scroll independently of the formation list.
5. Always distinguish **Standing**, **Unsent change**, and **Committed for round N**. Changing selection, opening Reports or returning to the campaign tab does not lose edits or commit them.
6. Formation totals are secondary; individual-ship stocks remain inspectable. Never average four shield facings into one apparently healthy bar. The footer is outside all scrolling panels.

### Round orders and review

Proposed initial maneuver menu: Maintain Distance / Hold & Fire, Charge, Flank, Screen, Fighting Withdrawal. Hold/Flank/Screen show the standalone preferred-range categories Distant, Engagement, Close, Intermixed. Hide irrelevant range inputs for Charge and Fighting Withdrawal while preserving their full order data explicitly; range is a preference, not a promised final position.

Fighting Withdrawal means **open tactical distance while remaining in this battle**. It must not trigger strategic movement or extraction. Breakthrough and Disengage are unavailable in this slice. Pursuit exists in the standalone resolver but its inclusion in the initial menu is an open choice (D5); omission is a proposed UX simplification. Do not expose Hold/Delay objectives, retreat thresholds, exit destinations, per-shot power switches, subsystem targets, repair or refill controls. Screen protection is not an Escort crossing objective.

Screen adds “Protect allied formation” with lawful eligible allies or “Own formation only” (no external protected ID). Switching away from Screen clears its incompatible protection field in the proposed replacement and shows that change in review. No silent changes to the backend's committed standing order occur during editing.

The review sheet lists every controlled active formation: old → proposed maneuver, range, target priority, concentration and protection; unchanged rows say **Retained**. Supply complete replacements for changed formations only once the real lifecycle contract defines submission. Standalone replacement DTO defaults do not mean “retain the omitted field”; partial field patches would lose prior choices. Missing required opening orders must be surfaced from the backend, never filled with Unity-invented defaults.

Primary button: **Commit round N**. Help: “Resolve this round using these orders. Later rounds wait for your decision.” Committing retained orders is explicit; no automatic multi-round or quick-resolve action in the first UI proposal. No opponent's newly chosen orders appear on the review sheet. Outcome, target access and firing cannot be guaranteed by review.

## Annotated wireframe B — legal target selection

```text
+-----------------------------------------------------------------------+
| [1] Escort — target preference for round 2                             |
| Priority: [Most damaged hull ratio v]                                 |
| Prefer formation:                                                     |
| (*) No formation preference                                           |
| ( ) Attack wing      [2] Selectable; currently screened                |
| ( ) Distant contact  [2] Selectable; weapon opportunity may change      |
| (x) Destroyed wing   [3] Unavailable for a new selection                |
|     Other contacts are shown only when lawfully known                  |
|                                                                       |
| [4] Preference applies among eligible targets; screening, arcs, range, |
|     power, cadence and ammunition still determine actual fire.         |
| [Cancel]                                          [Use in order draft] |
+-----------------------------------------------------------------------+
```

1. Targeting has two distinct fields: **ship priority** and optional **formation preference**. Map labels honestly: LowestId → “Stable target order” (not “optimal fire at will”); LowestHullRatio → “Most damaged hull ratio”; CommandSupport → “Command/support first”; Screen → “Screens/escorts first.” These prioritize eligible ships, not a selected subsystem or named locked ship. Individual-ship selection opens inspection only.
2. Separate **legal order preference** from **current firing opportunity**. A visible hostile formation may be a lawful concentration even if screened or currently beyond an energy weapon's reach. Maneuver happens after commitment and can change opportunity. Show current restrictions only when the backend supplies lawful advisory reasons; do not disable a valid tactical intent by calculating future fire in Unity.
3. New selection must use authoritative selectable choices. Standalone validation checks side relationships but does not provide actor-scoped visibility or require an active target for every retained preference. Campaign selection restrictions are a dependency, not a claim that this picker already exists. Unknown contacts must not leak names, hull, hidden orders or equipment via disabled entries or tooltips.
4. If the preferred formation becomes destroyed/ineligible, show the retained preference as unavailable and explain the actual backend fallback: preference is among eligible targets; remaining lawful targets may receive fire, or there may be no eligible shot. Do not silently rewrite the standing ID. Whether campaign validation retains historical preferences or requires replacement is D3. “No formation preference” clears concentration intentionally; it does not clear the ship priority.

Protection uses a separate allied picker. Only backend-authorized allies may be protected; an enemy or a contact inferred from a name cannot qualify. Protecting a lawful ally does not grant permission to edit that ally's orders. Whether another actor's allied formation can be protected follows the campaign side/authorization contract, not an own-faction guess. If a protected ally is lost, show that state and the backend's remaining Screen behavior, not a fabricated replacement ally.

## Annotated wireframe C — ship condition and finite stocks

```text
+-----------------------------------------------------------------------+
| [1] Escort / Ship A | Exact authored fit | Start of round 2             |
| HULL 90 / 100 [damaged]       Armor: 0 [read-only]                      |
|                                                                       |
|                            FORE 0 / 40 [depleted]                      |
|              PORT 13 / 40   [ship silhouette]   STARBOARD 40 / 40       |
|                             AFT 5 / 40                                 |
| [2] Fixed facing maxima. No redistribution. Own facing vs wing: Fore.  |
|                                                                       |
| WEAPON PACKAGE           STOCK / STATUS                               |
| Forward torpedo launcher 1 / 3 rounds | Powered | Ready this round     |
| Beam array               No physical ammo | Powered | Ready           |
| Cannon package           No physical ammo | Unpowered                 |
| [3] Details: coverage, cadence, power and reported firing opportunity  |
| [4] Last round: 2 torpedoes launched; 1 intercepted; 1 missed           |
|     Recharge: Port +3; Fore hit this round, recharge suppressed         |
| [Close inspection]                                                    |
+-----------------------------------------------------------------------+
```

1. Values are observed boundary state, labelled by round, not a speculative animation counter. Hull damage remains damage after battle, load, view refresh and profile selection. Destruction is terminal according to the authoritative result; show an immutable report row even if the campaign entity will be removed.
2. Fore/Port/Starboard/Aft are ship-relative labels, independent of screen/camera orientation. Each sector shows exact current/max, a bar and a textual state; colors alone are insufficient. Zero maximum displays “No capacity — 0/0,” not a percentage division or an inferred equipment diagnosis. A pair selector changes the exposure highlight, not the sector stocks. Backend recharge may restore damaged sectors but never transfer points from an intact facing.
3. Show torpedo stock per installed package because the standalone fields are weapon-level. A collapsed ship total is optional and secondary; it must not imply different variants share a transferable pool. Energy weapons say “No physical ammunition,” not infinity. Ready/cooling down, out of ammunition, unpowered and no current opportunity are different states. Display only reasons provided by authoritative data; package cadence/power cannot be changed here.
4. A fired torpedo costs one stock even if intercepted or missed. Failed beam interception attempts also spend firing opportunities; they do not consume fictional beam ammunition. Recharge has a finite ship-wide budget and power requirement; standalone current defaults prioritize Fore → Port → Starboard → Aft, suppress hit sectors, and recharge only if another round remains. These are provisional backend rules, not UI balance promises. No recharge occurs on early completion or after round 3.

For values too large for a bar label, keep exact numbers in selectable text/details. Presentation formatting must not clamp authoritative integers or infer combat changes by subtracting animated bars. Critical depletion warnings are proposed textual callouts; warning thresholds are D8, not new legality rules.

## Annotated wireframe D — resolved round report

```text
+--------------------------------------------------------------------------------+
| [1] ROUND 1 RESOLVED | Battle ongoing | Round 2 awaiting your orders              |
+--------------------------------------------------------------------------------+
| [2] MANEUVER: Escort / Attack wing: Engagement -> Engagement                     |
|     Known facings: Escort Fore; attack wing Starboard                            |
|     FIRE: 2 torpedo launches; 1 intercepted; 1 missed                             |
|     LOSSES: Ship A Fore -20; hull -10; no ship destroyed                          |
|     STOCKS: launcher 3 -> 1; recharge Port +3                                    |
| [3] These values illustrate report structure, not one executed fixture result.   |
+--------------------------------------------------------------------------------+
| [4] EVENTS [expand] | Ship filter [All v] | Formation filter [All v]              |
| #   Maneuver / discharge / interception / hit / shield / armor / hull / recharge  |
| ... Ordered authoritative rows; selected event opens details                    |
+--------------------------------------------------------------------------------+
| [5] Standing orders retained. No later round has been resolved.                  |
| [Back to campaign] [Save / Load] [Inspect stocks]           [Plan round 2]        |
+--------------------------------------------------------------------------------+
```

1. Show early decisive completion, ongoing state or round-limit completion from the backend. Never label a nondecisive round limit “Victory.” Round 3's primary action becomes encounter review/next encounter, not Plan round 4.
2. Explain cause and effect using authoritative geometry/events: changed exposure, actual discharges, interception attempts/successes, misses, sector absorption, armor mitigation, actual hull loss, destruction and recharge. “Attempted yield,” “landed damage,” “post-armor damage” and “actual hull loss” differ; intercepted/missed yield does not count as shield loss. No arbitrary rounding of valid large totals or double counting armor and hull is permitted.
3. Report wireframe values are illustrative. The worked examples below separate actual standalone evidence from a hypothetical stock ledger.
4. Preserve event sequence. Filters hide rows without renumbering or reordering them. An optional compact replay may highlight those already-resolved events; skipping/replaying it never resolves, spends stocks or draws RNG. No cinematic 3D work is necessary for the first slice. Lawful campaign report filtering is a lifecycle dependency: an omniscient fixture trace, including opponent `OrderCommitted` events or private equipment, is not automatically safe for human display.
5. Report acknowledgment is presentation state. Plan round 2 reuses committed orders and opens editing; it does not invoke another round. A report remains associated with its encounter and boundary, not whichever strategic fleet is currently selected. Save-time report retention is D7.

## Annotated wireframe E — save/resume and unsent edits

```text
+-----------------------------------------------------------------------+
| SAVE CAMPAIGN — round 1 resolved; round 2 ready                         |
| [1] Saved: authoritative checkpoint, standing orders and finite stocks |
| [2] You have unsent round 2 changes. These are not committed orders.    |
|     This save will not include those changes.                          |
| [Cancel]                         [Discard unsent changes and save...]  |
+-----------------------------------------------------------------------+
| LOAD CONFIRMATION                                                     |
| Slot: same opt-in scenario | Round 2 ready | Battle turn incomplete     |
| [3] Replaces this campaign after validation; unsent edits will be lost. |
| [Cancel]                                                [Load slot]   |
+-----------------------------------------------------------------------+
```

Proposed minimum save policy: campaign saves at a stable start-round, resolved-round, completed-encounter or ready-to-finalize batch boundary once the lifecycle supports it. A pre-commit draft is session-local. Save with unsent edits requires explicit discard-and-save or Cancel; it must not commit the edits or silently omit them. The subsequent save-slot overwrite confirmation remains separate. Failed save preserves the old valid file and live campaign; a failed write after an explicit discard is reported and does not claim the discarded edits were saved.

The backend save must preserve opt-in identities, exact source fits/stock caps, all pending/completed encounters, next-round/completion state, accepted standing orders, local RNG and command/replay protection. Do not reset shields/ammunition on load or reconstruct them from maxima. Exact schema/version identities and order-commit boundaries are unresolved backend work (D6), not specified here.

After successful load, drop old session drafts/selections, capture fresh authoritative observations and open the loaded boundary: round 1 resolved means **round 2 ready**, not replay round 1 to reconstruct state. Failed validation/load leaves the prior campaign, drafts and reports intact. In-flight submission disables session replacement until its definitive response is reconciled. A lost/ambiguous response requires an authoritative refresh; no silent resubmit or fresh command ID to force another round.

Navigation to strategic/read-only pages preserves local order edits in the same session. Starting a different campaign, changing the controlled actor or loading another slot requires explicit discard confirmation when dirty. Returning to the campaign screen does not retreat. End Turn and strategic mutating orders remain blocked at an unfinished combat checkpoint. Quitting may offer “Save boundary and quit,” with the same explicit unsent-edit policy, or cancel; no automatic advancement.

## Annotated wireframe F — encounter and campaign completion

```text
+------------------------------------------------------------------------------+
| [1] ENCOUNTER COMPLETE — ROUND LIMIT (3 / 3)                                  |
| Survivors on both sides. No victory, extraction or objective reward asserted. |
| Your survivors: Ship A hull 90/100; shields F3/P13/S40/A5; torpedoes 0/3         |
| [2] AFTERMATH PENDING — these stocks are the staged encounter result.         |
+------------------------------------------------------------------------------+
| [3] BATTLE TURN                                                               |
| Border region: complete          Second region: awaiting round orders         |
| Complete battle turn [disabled: 1 encounter remains]       [Next encounter]   |
+------------------------------------------------------------------------------+
| WHEN ALL ENCOUNTERS ARE COMPLETE                                              |
| [4] Review losses and retained stocks [Save checkpoint] [Complete battle turn]|
| ACCEPTED: Turn complete / Planning | stocks applied | [Return to campaign]    |
| REJECTED: Checkpoint unchanged | reason [Review] [Refresh]                     |
+------------------------------------------------------------------------------+
```

1. Use outcome language supplied by the backend: decisive with identified survivor sides, mutually destroyed if applicable, or **Round limit — survivors remain**. Completion is distinct from a strategic victory condition. Do not manufacture a winner from damage totals.
2. Encounter resources may be staged until batch finalization. Label them accordingly; a strategic ship inspector must not display stale pre-battle values as if they were its final v2 condition. After acceptance, show actual persisted hull, all four shields and ammunition. No post-completion recharge, repair, resupply or profile-selection heal occurs.
3. Mixed checkpoints list real encounter status/resolver availability. Do not resolve all remaining human rounds because another encounter finished. An unsupported mixed v1/v2 encounter reports a backend blocker; no implicit scalar-to-equipment conversion. V1 and v2 encounters may only coexist if the lifecycle routing explicitly supports that batch (D2).
4. Finalization requires every encounter complete and a backend-authorized atomic transaction. It applies survivor stocks/destruction in canonical order, completes the already-processed strategic turn exactly once and opens the next Planning state. A stale/repeated/invalid completion leaves state and RNG unchanged; earlier completed encounters remain available for review. A failure in a later encounter rolls back the whole attempted aftermath, not just the last ship.

**Release gate D1:** round-three survivors' actual location, route continuation and renewed-contact policy are unresolved. This wireframe intentionally has no invented escape/teleport, conquest reward, automatic new battle loop or “continue past enemy” rule. “Return to campaign” cannot be advertised as playable until lawful backend aftermath exists for a nondecisive third round. Returning to the strategic UI is navigation, not relocation.

## Worked examples

### Example 1 — real standalone maneuver evidence, not a campaign walkthrough

The pinned playground report records the `cruiser` fixture at seed 47. Round 1 Flank keeps Engagement range and exposes the attack wing's Starboard while the cruiser retains Fore. The forward-only attackers emit no offensive discharges. Three Flank rounds leave cruiser hull **220/220** and enemy hull total **132**. Three Charge rounds instead leave cruiser hull **63/220** and enemy hull total **170**. Both end at the round limit; neither proves campaign aftermath or declares a winner.

Proposed player explanation: “Flank gained lateral exposure. Their forward weapons had no offensive discharge this round.” Only show that causal statement when the actual report supports it; do not predict automatic Flank success before commitment. The UI can place a practical order beside the optional “Attack Pattern Delta Three” alias, with no new rule attached.

The same report's `screen` fixture records four beam opportunities spent and three torpedoes intercepted in its first Screen round, with no landed damage to the protected transport. A failed interception still costs an opportunity. The report should show **4 attempts / 3 successes**, not suggest unlimited defense. These are authored standalone demonstrations, not the new scenario's approved equipment or opponent behavior.

### Example 2 — hypothetical three-round stock ledger

**Illustrative accounting only; not executed acceptance or an approved fit.** Suppose a fully validated, explicitly authored Ship A has hull 100/100, armor 0, fixed shield maxima 40 each, entry stocks Fore 20 / Port 10 / Starboard 40 / Aft 5, and one launcher with 3/3 torpedoes. Its recharge budget is 3, fully powered, with the inspected provisional suppression/priority rules. All packages and mandatory systems have feasible authored reservations; the UI does not invent power. This ledger assumes the backend produced the stated events, cadence permits those launches, and opponents survive all three rounds.

| Boundary | Authoritative events assumed for this example | Hull | F / P / S / A current | Torpedoes |
| --- | --- | --- | --- | --- |
| Entry, round 1 ready | No initial replenishment | 100 | 20 / 10 / 40 / 5 | 3 |
| Round 1 resolved | Incoming landed yield 30 on Fore: absorb 20, armor 0, hull loss 10. Two own launches: one intercepted, one misses. Fore hit suppresses recharge; Port receives 3 | 90 | 0 / 13 / 40 / 5 | 1 |
| Round 2 resolved | No incoming hit; one own launch misses. Recharge gives Fore 3 | 90 | 3 / 13 / 40 / 5 | 0 |
| Round 3 complete | No incoming hit; exhausted launcher does not discharge. No recharge after completion | 90 | 3 / 13 / 40 / 5 | 0 |
| Finalization accepted | Persist exactly these survivor values, once | 90 | 3 / 13 / 40 / 5 | 0 |
| Later lawful encounter entry | No refill, recovery or hull heal; later encounter creation itself changes none of these stocks | 90 | 3 / 13 / 40 / 5 | 0 |

Save after round 1, load, retain orders and commit round 2 must reproduce the same remaining events/resources/RNG as uninterrupted play given identical subsequent orders. Reloading repeatedly must not grant another Port +3. The later encounter row is conditional on D1 being resolved; it grants no rule for triggering a fresh battle. In that later battle, ordinary in-battle recharge may again occur at unfinished round transitions; this is not between-battle recovery.

### Example 3 — persistent intent, loss of a preferred target and stale editing

At round 1, Escort is ordered Screen, protect Transport, prefer Attack wing, prioritize command/support. Round 2 opens with the same standing fields. The player only changes priority to most damaged hull ratio; the review must retain Screen, protected Transport and concentration, rather than fill other fields from DTO defaults.

If Attack wing was destroyed, its historical preference remains visibly unavailable. Existing standalone acquisition would fall back to another eligible hostile, not shoot a destroyed ship. New campaign rules must decide whether that retained ID is accepted or requires correction (D3). The UI must show the decision/reason returned by that contract and allow an explicit clear/change; it must not invent an alternate target or promise fire through screening.

If another accepted command changes the observed boundary before commitment, the user's old proposal is rejected without stocks/RNG changing. Show “Battle state changed. Review the current round before committing.” Keep the rejected proposal for comparison within the same session; refresh authoritative facts explicitly, mark conflicts, require a new review. A campaign load clears that old proposal instead of attaching it to matching IDs in the replacement session. No automatic refresh-and-retry and no forged opponent orders.

### Example 4 — two encounters and a failed campaign commit

Encounter A finishes round 2 decisively; encounter B finishes round 3 with survivors. A's completion does not open Planning or refill its ships. Save the whole checkpoint, load it, and present A as completed and B at its stored boundary without replaying either. Once both are complete, enable finalization only if authoritative eligibility permits it. If B's aftermath validation fails, show the failure and preserve both staged results plus the pre-finalization campaign. The UI does not apply A's hull loss separately or advance a calendar to hide B's blocker. D1 must first define B's lawful survivor placement and future-contact policy.

## Responsive presentation and interaction acceptance proposal

| Size | Proposed adaptation | Review target, not claimed runtime evidence |
| --- | --- | --- |
| 1280 × 720 | Two columns: compact selectable formations, tabbed relationship/orders/ship panel; report replaces central panel. Bounded list/detail scrolling; persistent footer | All order fields reachable without clipping; commitment, Back, Save/Load and dirty-state message always reachable |
| 1920 × 1080 | Wireframe A three columns; inspection/report drawers; full practical labels | Pairwise geometry and all four sectors readable; draft and retained order distinction obvious |
| 3440 × 1440 | Bounded readable orders/inspection width; extra area for pair list and report, rather than stretched text or tiny typography | No detached confirmation far from its context; same command behavior and essential controls |

Reuse LCARS visual vocabulary in a later implementation, with black field, segmented rails, readable numeric text and plain practical labels. Keep shape/text cues alongside status colors. Logical keyboard focus follows header → formation list → selected details → footer; Enter in a field must not commit a round. Commitment happens only from its focused explicit action/review. Escape cancels the active modal before changing screens. Tooltips supplement visible reasons, not hide essential validation. Focus/scroll/resize cannot submit commands. No mouse/keyboard/desktop session was launched for this documentation work.

Future acceptance must use a real campaign-owned headless R1 → save → R2/R3 → atomic finalization proof before Unity. Cover lawful targets/protection, owned versus foreign actors, unavailable/mixed sources, retained fields, target loss, zero/large stocks, cadence/empty ammunition, sector depletion/recharge, simultaneous destruction, stale/duplicate commitments, rejected finalization, multi-encounter saves and failed load preserving the old session. Then test the actual UI at all three sizes and distinguish Unity/native evidence from CI. This is an acceptance proposal, not a request to run gameplay tests now.

## Unresolved choices for the design discussion

| ID | Decision and worked consequence | Proposed discussion starting point / gate |
| --- | --- | --- |
| D1 | Nondecisive round 3: where do survivors remain, what happens to routes, when can a fresh encounter occur? Example 2 cannot simply restart while co-located; example 4 cannot finalize via teleport | Resolve authoritative geography/contact policy before lifecycle implementation. No UX default can settle it; no free exit or implied objective reward |
| D2 | Fleet-to-formation mapping, capability aggregation, initial pair geometry and mixed-source routing. One escort and transport cannot acquire guessed ratings or invented sides | First authored scenario should make source/membership/capabilities explicit; actual formation policy and whether mixed v1/v2 encounters reject or route separately require backend design |
| D3 | Retained concentration/protection referencing a destroyed formation: keep historical preference with eligible fallback, or require repair of orders? Example 3 exposes the difference | Prefer visible retained intent plus backend fallback where legal; distinguish this proposal from standalone's weaker side-only order validation |
| D4 | Lawful start-round observations, deterministic baseline opponent policy, joint commit timing and local multiple-human handoff | Actor-scoped views; never disclose fresh opponent orders or copy the omniscient sandbox. Decide policy/commit boundary before implementing; advanced tactical AI and multiplayer stay deferred |
| D5 | Initial maneuver menu and naming: include Pursuit when extraction is excluded? Are pattern aliases useful beside practical names? | Begin with five practical maneuvers listed above; discuss Pursuit separately. No Breakthrough/Disengage or objective menu, regardless of labels |
| D6 | Exact campaign save identities, supported commit/save boundaries and behavior if a response is lost | Save only authoritative stable boundaries; no partial joint commitment assumption or guessed Save/Content bump. Discuss whether committed-waiting states need persistence |
| D7 | Which resolved reports/events survive checkpoint saves and finalization? V1 recent history is client memory, not persistent campaign history | Minimum: enough saved authoritative data to resume without resolving again. Propose retaining current batch round reports while pending; long-term report archive/replay remains separately scoped |
| D8 | Depletion warnings, report summary density and finalization confirmation | Exact stocks always visible; discuss zero versus low-stock warnings and a single review before finalization. Do not turn a warning threshold into a gameplay restriction |

Agreement sought: approve or revise these interactions and resolve lifecycle gates before commissioning the Unity slice. This document does not dispatch equipment/lifecycle work, amend the agreed scope, implement production UI, claim new campaign capabilities or authorize a merge.

## Documentation verification

The 17 linked source references were checked against local files or the pinned Git objects; the next-step plan hash was rechecked. Six fenced annotated wireframes and four worked examples are present. The staged diff contains only this new specification and passes `git diff --cached --check`. No gameplay tests, Unity runs, native builds or desktop tools were launched for this documentation-only change; runtime acceptance remains future work.
