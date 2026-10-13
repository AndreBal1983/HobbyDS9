# First playable campaign Tactical Combat v2 — UX specification

Review date: 13 October 2026. Revised against the owner's product review of commit `bb8982682f4440ef93aba3c164d28609ec893f02`. Status: **proposed interface; documentation only; design discussion and accepted campaign lifecycle required before implementation**. Incorporating the review's proposals does not mark them or D1–D8 agreed.

This specification describes the first campaign-owned, interactive v2 slice. It is not an acceptance report or evidence that campaign v2 is playable. The annotated wireframes are proposed layouts, not screenshots. No production Unity files, backend contracts, serialized versions or gameplay are changed.

## Review basis and decision status

The documentation branch starts at local main `4e1a365fbae52591ddfc593200fae7b1f79a53db`. This is a recorded local checkpoint, not a claim about current remote main or the acceptance of incoming branches. Runtime capability statements below distinguish this base from the separately inspected v2 source checkpoint.

Read together:

- [Design index](design/README.md), [tactical combat](design/tactical-combat.md), [ship design](design/ship-design.md), [strategic AI](design/strategic-ai.md), [time and movement](design/time-movement-mobilisation.md), and [exploration/repair direction](design/exploration-missions.md).
- [Architecture](architecture.md), [blueprint construction v1](blueprint-construction-v1.md), and [existing combat orchestration/reports](combat-ux-report-fix-v1.md). Historical version/count statements in these documents remain historical.
- Local planning inputs: the [next-step plan](E:/HobbyDS9/docs/next-step-preparation.md), dated 13 October 2026, and its [equipment preparation prompt](E:/HobbyDS9/docs/prompts/tactical-v2-authored-equipment-foundation.md). These remain untracked in the primary checkout and are not distributed project documentation on this branch; this change does not copy, revise or dispatch them. Initial plan SHA256: `366d996d84cd229356e7db2fa1bc7ea6ef947daa724d3ae0ff65fb20df736fb6`; updated plan read for this revision: `3efeff42de896a6842cd2190b0dd94b7a40de8e27130776d1efa4dab8bfa5b59`. The agreed product constraints are restated below so review does not depend on those local files being present. Replace these machine-local references with repository-relative links when the planning files are actually committed alongside the specification; do not create links to absent files or imply their distribution now.
- The owner's local [UX review](E:/HobbyDS9/docs/campaign-tactical-v2-ux-review.md), dated 13 October 2026, SHA256 `9f1bc03a8242998abae307f1ef176cfad180dde7ac66f15c4c64590d1f0452fe`. It is also an untracked review input, not a document included in this branch. The response matrix and staged decision gates below preserve its recommendations without requiring access to that file.
- Pinned incoming source checkpoint `27ed955296b2a4ce8b86f1b65d663c389023e99e`: [standalone backend](https://github.com/AndreBal1983/HobbyDS9/blob/27ed955296b2a4ce8b86f1b65d663c389023e99e/docs/tactical-combat-v2-backend.md), [console playground](https://github.com/AndreBal1983/HobbyDS9/blob/27ed955296b2a4ce8b86f1b65d663c389023e99e/docs/tactical-combat-v2-playground.md), and [campaign inspection/migration report](https://github.com/AndreBal1983/HobbyDS9/blob/27ed955296b2a4ce8b86f1b65d663c389023e99e/docs/campaign-tactical-combat-v2-integration.md). Actual [contracts](https://github.com/AndreBal1983/HobbyDS9/blob/27ed955296b2a4ce8b86f1b65d663c389023e99e/src/HobbyDS9.Contracts/TacticalCombatV2Contracts.cs), [order validation](https://github.com/AndreBal1983/HobbyDS9/blob/27ed955296b2a4ce8b86f1b65d663c389023e99e/src/HobbyDS9.Simulation/TacticalCombatV2State.cs), and [one-round resolver](https://github.com/AndreBal1983/HobbyDS9/blob/27ed955296b2a4ce8b86f1b65d663c389023e99e/src/HobbyDS9.Simulation/DeterministicTacticalCombatV2Resolver.cs) were inspected for the semantics below. Their presence on that branch does not establish their presence or acceptance on this documentation base.

**Agreed slice constraints:** a separate, explicitly opt-in scenario with authored v2 ships; finite persistent ammunition and four directional shield stocks; persistent hull damage; no automatic between-battle recovery or ammunition refill; existing v2 in-battle recharge; destruction/basic battles with a maximum of three rounds; no strategic retreat, Breakthrough or other objective exits. Existing v1 ships and designer-built v1 blueprints stay on their supported path, without inferred conversion.

**Proposed here:** screen structure, labels, order editing/review, target picker behavior, report presentation, save prompts and completion flow. **Open:** the decisions in the discussion register. Examples supply illustrative data or explicitly identified standalone evidence, never approved campaign balance.

The broader design agrees future objectives and cinematic visualization. Their exclusion from this slice is deliberate phasing, not a reversal. The older design/architecture descriptions of encounter-local resetting shields apply to v1; they must not be copied into the opted-in persistent v2 path. Recovery proposals in the migration report do not override the owner's narrower no-recovery decision.

**Simulation enforcement requirement:** basic Defeat objectives alone do not prevent extraction. The standalone Disengage maneuver can extract under another objective. The opt-in campaign allocator/order validator must reject Disengage, Breakthrough and every other excluded extraction/objective-exit path, including initial or loaded standing orders, retained orders and new replacements. Enforce the restrictions before round mutation/RNG consumption, regardless of which client supplies orders. Menu visibility is presentation only. Fighting Withdrawal remains an in-battle range change, with no strategic destination or exit. This is a required lifecycle gate, not a validator implemented by this specification.

### Reconciliation with the combined candidate

The [combined acceptance report](https://github.com/AndreBal1983/HobbyDS9/blob/22096fb56d81d203f194a6c0407d7de76721d93c/docs/inspection-designer-combined-acceptance.md) at exact delivery head `22096fb56d81d203f194a6c0407d7de76721d93c` was read for this revision. It combines the inspection and live v1 designer histories, records 1,358 .NET tests, 116 Unity tests, IL2CPP/native acceptance and unchanged compatibility/determinism, and explicitly keeps campaign v2 disabled. Those are the report's recorded results, not tests rerun here. The owner's review records PR #18 as draft/unmerged and exact-head CI successful; this revision does not independently assert a newer remote PR state or establish a merged checkpoint.

The standalone contracts, state validation, one-round resolver and Application inspection files are unchanged between the pinned source head `27ed955…` and combined delivery head `22096fb…` (Git blob comparison). The capability map below therefore still describes their limits; live v1 publication/construction acceptance supplies no campaign-v2 lifecycle, equipment or persistent stock contract.

This documentation branch deliberately retains its original local base; incorporating production integration commits is outside this change. **Before implementation**, reconcile against the actually accepted combined main SHA and accepted equipment/lifecycle contracts, replacing historical capability references where appropriate. A candidate acceptance report, or a later equipment-completeness projection alone, is not permission to build the Unity slice. No entire acceptance rerun is commissioned by this documentation revision.

## Capability and dependency map

| Area | Existing capability at the inspected checkpoint | Required campaign dependency / proposed UX |
| --- | --- | --- |
| One round | Pure `ResolveRound` accepts an explicit standalone encounter and joint replacements, returns detached state and immutable events | Session-owned encounter and human-authorized round transaction; the UI must not submit a fixture/result as campaign authority |
| Standing orders | Formation `StandingOrder`; omitted formations retain orders; supplied orders replace all fields | Campaign persistence and lawful actor-scoped observation; UI shows retained versus unsent changes |
| Targets | Optional enemy formation preference plus `LowestId`, `LowestHullRatio`, `CommandSupport`, `Screen`; optional allied Screen protection | Campaign actor/side validation, explicit scenario observation policy and advisory legal choices; no locked individual-ship target API exists |
| Geometry | Pairwise categorical range and both facings; provisional maneuver contests, arcs and screening | Authored capabilities, accepted formation mapping and initial geometry; no camera coordinates determine access |
| Stocks | Four current/max shield sectors, hull, weapon-level finite torpedo ammunition, cadence and recharge in standalone state | Persistent campaign resources bound to exact authored fits, frozen encounters and atomic aftermath |
| Completion | `Decisive`, `Objective`, `RoundLimit`; Disengage may extract even under Defeat in the standalone resolver | Simulation must reject excluded maneuvers and every extraction/objective-exit path, including standing orders; Defeat-only objectives and hidden buttons are insufficient |
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

### Tactical observations without a new fog-of-war milestone

Actor/controller ownership and withholding freshly selected opponent orders are mandatory. D4 must explicitly decide which enemy geometry, equipment, stock values and resolved actions the first authored scenario may reveal at each boundary. It may choose a simple, declared observation policy; this specification does not select one, implement sensors/intelligence or require a new strategic fog-of-war system. A “contact” below means a participant entry permitted by that policy, not a new detection/tracking state machine.

The planner, target picker and report must use the same lawful observations supplied by the accepted lifecycle. Show exact enemy values only if permitted, otherwise omit them or use the contract's unavailable/unknown wording without leaking hidden identities. Even a policy permitting full equipment/geometry inspection does not permit counter-picking fresh opponent orders or automatically disclose every private event in an omniscient standalone trace. Information restrictions are owned by the backend projection/commit boundary, not invented or bypassed by Unity.

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
| Policy [info]       | ABSTRACT RELATION, NOT A MAP       | Prefer [Attack wing v] |
|                     | No drag-to-steer or distance ruler| [5] Unsent change      |
+---------------------+-----------------------------------+------------------------+
| [6] Escort stocks: Hull / Fore / Port / Starboard / Aft / torpedo stocks [Inspect] |
| Orders persist until changed. Target preference does not guarantee a firing shot. |
+----------------------------------------------------------------------------------+
| [Back to campaign] [Save / Load] [Discard edits]             [Review round 1 orders]|
+----------------------------------------------------------------------------------+
```

1. Entry briefing uses “Authored v2 scenario — persistent damage and ammunition; no resupply or between-battle shield recovery.” Activation is backend-gated, not inferred from the scenario's display name.
2. Only actor-owned, active formations receive editable orders. Other participants are read-only entries permitted by the still-open scenario observation policy (D4), not proof of a new strategic fog-of-war system. Authored sides, rather than faction names, determine hostility. Formation creation/splitting is outside this slice.
3. Select a pair to show its range and both facings only to the extent lawfully observed. Multiple hostile formations require a pair list; one global range or one facing arrow cannot represent every opponent. Decorative positions cannot select firing arcs.
4. Show practical maneuver names first. Optional pattern callouts from the design chapter remain secondary proposed aliases, not additional buffs. Details scroll independently of the formation list.
5. Always distinguish **Standing**, **Unsent change**, and **Committed for round N**. Changing selection, opening Reports or returning to the campaign tab does not lose edits or commit them.
6. Formation totals are secondary; individual-ship stocks remain inspectable. Never average four shield facings into one apparently healthy bar. The footer is outside all scrolling panels.

### Round orders and review

Proposed initial maneuver menu: Maintain Distance / Hold & Fire, Charge, Flank, Screen, Fighting Withdrawal. Hold/Flank/Screen show the standalone preferred-range categories Distant, Engagement, Close, Intermixed. Hide irrelevant range inputs for Charge and Fighting Withdrawal while preserving their full order data explicitly; range is a preference, not a promised final position.

Fighting Withdrawal means **open tactical distance while remaining in this battle**. It must not trigger strategic movement or extraction. Simulation must reject Breakthrough and Disengage in this slice, even under a Defeat objective and even when supplied as retained/loaded orders; the menu also omits them. Pursuit exists in the standalone resolver but its inclusion in the initial menu is an open choice (D5); omission is a proposed UX simplification. Do not expose Hold/Delay objectives, retreat thresholds, exit destinations, per-shot power switches, subsystem targets, repair or refill controls. Screen protection is not an Escort crossing objective.

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
4. Preserve event sequence. Filters hide rows without renumbering or reordering them. An optional compact replay may highlight those already-resolved events; skipping/replaying it never resolves, spends stocks or draws RNG. No cinematic 3D work is necessary for the first slice. Report visibility follows D4's explicit first-scenario observation policy, not a newly promised strategic fog-of-war system. An omniscient fixture trace, including opponent `OrderCommitted` events or private equipment, is not automatically safe for human display; Unity must not treat it as player intelligence.
5. Report acknowledgment is presentation state. Plan round 2 reuses committed orders and opens editing; it does not invoke another round. A report remains associated with its encounter and boundary, not whichever strategic fleet is currently selected. Save-time report retention is D7.

## Annotated wireframe E — save/resume and unsent edits

```text
+-----------------------------------------------------------------------+
| SAVE CAMPAIGN — round 1 resolved; round 2 ready                         |
| [1] Saved: authoritative checkpoint, standing orders and finite stocks |
| [2] You have unsent round 2 changes. These are not committed orders.    |
|     They stay in this session but are not stored in this save.         |
|     Successful load or restart loses them.                             |
| [Cancel]                               [Save committed battle state]  |
+-----------------------------------------------------------------------+
| LOAD CONFIRMATION                                                     |
| Slot: same opt-in scenario | Round 2 ready | Battle turn incomplete     |
| [3] Replaces this campaign after validation; unsent edits will be lost. |
| [Cancel]                                                [Load slot]   |
+-----------------------------------------------------------------------+
```

**Revised proposal, still awaiting interaction review:** campaign saves at a stable start-round, resolved-round, completed-encounter or ready-to-finalize batch boundary once the lifecycle supports it. Primary action **Save committed battle state** saves the authoritative checkpoint and keeps the local order draft in the running session. An unsent draft is not stored in that save; explain this before saving and state that successful load or process restart loses it. Saving neither commits nor requires discarding those edits. The subsequent save-slot overwrite confirmation remains separate; Cancel preserves the live draft and the prior file.

Failed save must retain the valid prior save, live campaign and local edits, with an explicit failure message. After a successful save the unsent draft still shows **Unsent change**, not “Saved order,” and returning to the same planner retains it. **Discard edits** remains a separate deliberate planner action; it is not a prerequisite, side effect or recovery action for saving. This is a UX proposal, not a campaign v2 save contract implemented now.

The backend save must preserve opt-in identities, exact source fits/stock caps, all pending/completed encounters, next-round/completion state, accepted standing orders, local RNG and command/replay protection. Do not reset shields/ammunition on load or reconstruct them from maxima. Exact schema/version identities and order-commit boundaries are unresolved backend work (D6), not specified here.

After successful load, drop old session drafts/selections, capture fresh authoritative observations and open the loaded boundary: round 1 resolved means **round 2 ready**, not replay round 1 to reconstruct state. Failed validation/load leaves the prior campaign, drafts and reports intact. In-flight submission disables session replacement until its definitive response is reconciled. A lost/ambiguous response requires an authoritative refresh; no silent resubmit or fresh command ID to force another round.

Navigation to strategic/read-only pages preserves local order edits in the same session. Starting a different campaign, changing the controlled actor or loading another slot warns that unsent edits will be lost and offers Cancel; successful session replacement clears them, while failed load preserves them. Returning to the campaign screen does not retreat. End Turn and strategic mutating orders remain blocked at an unfinished combat checkpoint. Quitting may offer **Save committed state and quit**, with an explicit warning that unsent edits will be lost on exit, or Cancel. A failed save keeps the session and edits open; no automatic advancement or forced discard occurs.

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

If the player first drafts a different round-2 maneuver, **Save committed battle state** stores this same round-1 checkpoint and retained standing orders while leaving the unsent change visible in the running planner. Continuing without loading preserves that local change for later review. Loading the save successfully or restarting loses the unsent change and restores only the accepted standing orders; a failed save or failed load preserves the running draft. This extension illustrates the revised proposal, not an executed v2 save test.

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

Future acceptance must use a real campaign-owned headless R1 → save → R2/R3 → atomic finalization proof before Unity. Cover lawful targets/protection separately from immediate firing opportunity, owned versus foreign actors, the declared observation policy and withheld fresh opponent orders, unavailable/mixed sources, retained fields, target loss, zero/large stocks, cadence/empty ammunition, sector depletion/recharge, simultaneous destruction, stale/duplicate commitments, rejected finalization and multi-encounter saves. Test excluded exit maneuvers under Defeat through direct submissions and initial/loaded/retained orders, with rejection before mutation/RNG; Fighting Withdrawal remains in-battle. Confirm dirty-draft saving retains edits, successful load/restart loses unsent edits, and failed save/load preserves both the running draft and valid previous save where applicable. Then test the actual UI at all three sizes and distinguish Unity/native evidence from CI. This is an acceptance proposal, not a request to run gameplay tests now.

## Unresolved choices for the design discussion

| ID | Decision and worked consequence | Proposed discussion starting point / gate |
| --- | --- | --- |
| D1 | Nondecisive round 3: where do survivors remain, what happens to routes, when can a fresh encounter occur? Example 2 cannot simply restart while co-located; example 4 cannot finalize via teleport | Resolve authoritative geography/contact policy before lifecycle implementation. No UX default can settle it; no free exit or implied objective reward |
| D2 | Fleet-to-formation mapping, capability aggregation, initial pair geometry and mixed-source routing. One escort and transport cannot acquire guessed ratings or invented sides | First authored scenario should make source/membership/capabilities explicit; actual formation policy and whether mixed v1/v2 encounters reject or route separately require backend design |
| D3 | Retained concentration/protection referencing a destroyed formation: keep historical preference with eligible fallback, or require repair of orders? Example 3 exposes the difference | Prefer visible retained intent plus backend fallback where legal; distinguish this proposal from standalone's weaker side-only order validation |
| D4 | Explicit first-scenario observation policy for enemy geometry/equipment/events, deterministic baseline opponent policy, joint commit timing and local multiple-human handoff | Ownership and withholding fresh opponent orders are mandatory. Exact visible enemy facts remain open; no new strategic fog-of-war system is promised. Never copy an omniscient trace into player intelligence. Resolve policy/commit boundary before lifecycle work; advanced tactical AI and multiplayer stay deferred |
| D5 | Initial maneuver menu and naming: include Pursuit when extraction is excluded? Are pattern aliases useful beside practical names? | Begin with five practical maneuvers listed above; discuss Pursuit separately. No Breakthrough/Disengage or objective menu, regardless of labels |
| D6 | Exact campaign save identities, supported commit/save boundaries and behavior if a response is lost | Save only authoritative stable boundaries; no partial joint commitment assumption or guessed Save/Content bump. Discuss whether committed-waiting states need persistence |
| D7 | Which resolved reports/events survive checkpoint saves and finalization? V1 recent history is client memory, not persistent campaign history | Minimum: enough saved authoritative data to resume without resolving again. Propose retaining current batch round reports while pending; long-term report archive/replay remains separately scoped |
| D8 | Depletion warnings, report summary density and finalization confirmation | Exact stocks always visible; discuss zero versus low-stock warnings and a single review before finalization. Do not turn a warning threshold into a gameplay restriction |

### Decisions and gates by stage

| Stage | Inputs / open choices | Required treatment |
| --- | --- | --- |
| Equipment foundation | Complete authored fit, power reservations, initial stocks and explicit per-ship capabilities with actual version identities | Prepared backend scope only; no guessed formation aggregation or activation. Revise the illustrative equipment examples when the real contract is delivered, retaining their provisional labels until reviewed |
| Campaign lifecycle | D1 survivor geography/contact; D2 formation/geometry/mixed-source routing; D4 scenario observations/baseline AI/joint commitments; D6 authoritative saves; D7 current-batch report persistence | Resolve before commissioning lifecycle implementation; enforce excluded exit paths in Simulation and prove campaign-owned rounds/save/resume/atomic aftermath |
| Unity interaction specification | D3 lost target/protection intent; D5 menu/Pursuit; D8 warnings/reports/confirmation; revised unsent-edit save behavior | Confirm against accepted lifecycle projections, then finalize the Unity implementation prompt. No runtime UI work while lifecycle is absent |

Suggested design-round order: actual equipment and formation/source mapping → initial geometry and scenario observations → nondecisive round-three aftermath/contact → baseline orders and persistence/report details. Use real authored equipment once available; do not settle rating formulas from placeholder numbers. Proposed defaults remain retained intent with lawful backend fallback, practical names first, exact stocks with zero-stock warnings, current-batch report retention across saves and no campaign-long archive. No response or product review implies agreement.

### Response to the owner's review

| Review item | Revision | Decision status |
| --- | --- | --- |
| 1. Saving without forced discard | Wireframe E, save/failure/load/quit behavior and example 2 now save committed state while retaining the running draft | Revised UX proposal; unsent edits remain session-local; confirm against D6 and accepted lifecycle |
| 2. Combined checkpoint and planning references | Read pinned combined report and compared relevant source blobs; retained original branch base and explicit pre-implementation reconciliation gate. Local untracked inputs are labelled undistributed; relative links await actual committed files | Recorded candidate evidence, not an accepted/merged checkpoint or runtime rerun |
| 3. Tactical contacts without new fog-of-war | Added explicit scenario observation-policy boundary; clarified wireframes, D4 and report visibility | Actor ownership/order secrecy mandatory; exact enemy facts still open; no strategic fog-of-war expansion |
| 4. Legal intent versus fire opportunity | Retained formation preference, eligible-target fallback and backend acquisition authority; acceptance proposal checks the distinction | No locked ship/subsystem selector or firing guarantee; no Unity acquisition rules |
| 5. Simulation-enforced exclusions | Explicitly reject Disengage even under Defeat, Breakthrough and all excluded exit paths across initial/loaded/retained/new orders; added future acceptance cases | Required lifecycle constraint, not an implemented validator; Fighting Withdrawal remains in-battle |

Agreement sought: approve or revise these interactions and resolve lifecycle gates before commissioning the Unity slice. This document does not dispatch equipment/lifecycle work, amend the agreed scope, implement production UI, claim new campaign capabilities or authorize a merge.

## Documentation verification

For the initial draft, 17 linked source references and the original plan hash were checked. For this revision, links are checked against local input files or pinned Git objects; both current review/plan hashes are recorded above. Relevant standalone/inspection source blobs were compared with the combined candidate. Six fenced annotated wireframes and four worked examples remain. The revision diff is limited to this specification and checked with `git diff --cached --check`. No gameplay tests, Unity runs, native builds or desktop tools were launched for this documentation-only change; runtime acceptance remains future work.
