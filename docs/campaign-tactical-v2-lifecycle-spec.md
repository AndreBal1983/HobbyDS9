# Campaign tactical v2 lifecycle milestone

Prepared 13 October 2026. Status: **agreed player behavior consolidated; proposed technical rules; no gameplay implementation or activation claimed**. This milestone follows the authored equipment foundation and leads to the first playable Unity slice.

Execution update: the owner subsequently dispatched the backend and UX assignments. Stage A prototypes are delivered in draft PR #20; a gated Stage B layer and an isolated presenter are being developed. See [overnight review notes](overnight-review-notes.md) and the product-manager memory for dated evidence. Prepared API/formula proposals remain proposals until reviewed; dispatch does not establish acceptance or activation.

Read [the design index](design/README.md), [tactical combat](design/tactical-combat.md), [ship design](design/ship-design.md), [strategic AI](design/strategic-ai.md), [time and movement](design/time-movement-mobilisation.md), [architecture](architecture.md) and [blueprint construction](blueprint-construction-v1.md). The tactical chapter is authoritative for the agreed decisions; this document defines the scoped implementation and acceptance work. Do not silently implement the wider roadmap.

## Starting checkpoint and authority

PR #18 merged at `53d2e64973d9081a4d0090e771f5e777fc55cf40`, with accepted live v1 designer/construction and source inspection. Authored equipment [draft PR #19](https://github.com/AndreBal1983/HobbyDS9/pull/19), last reviewed head `be02572fc3fc2cfdb90cdd9ad763bbdf3d79716b`, supplies complete static fit validation and initial provisions. Its report records 1,507 passing tests and Content 11 support alongside legacy Content 10. Save 14, v1 compiler/rules and the standalone-v2 reference are unchanged. Reverify actual main, PR head/status and exact-head CI before starting; this planning document does not authorize merging #19.

The equipment scenario is a source-validation sandbox, not a live v2 campaign. Its initial ammunition and shields are content facts. There is no accepted persisted-current-resource, controller-owned round or v2 aftermath contract yet. Legacy authored ships and frozen v1 blueprint revisions cannot acquire missing v2 data through conversion or names.

The owner authorized preparation of this specification and assignments. No messages have been dispatched and no new implementation has been launched here. See the [backend assignment](prompts/campaign-tactical-v2-lifecycle-backend.md) and [parallel UX assignment](prompts/campaign-tactical-v2-lifecycle-ux-reconciliation.md).

## Agreed player behavior

| Area | Required behavior |
| --- | --- |
| Command unit | One strategic fleet is one tactical unit/formation. Separate fleets remain separate even with identical routes or ownership. No automatic role-based splitting or merging |
| Player orders | Simple fleet maneuver and enemy-fleet focus. Ships perform suitable functions and select individual targets automatically; no ship-level fire orders or subsystem targeting |
| Composition | Actual installed capabilities and tactical circumstances produce benefits with costs and limits. Fast escorts retain useful mobility with capital ships. No universal slowest-ship tactical rule or class-label composition buff |
| Screening | Defensive contributions protect only ships in the same fleet. Put escorts with the transport/colony ship strategically. No allied-fleet protection selector or cross-fleet screening |
| Focus | Prefer the selected enemy fleet; fall back automatically to another visible hostile fleet with eligible targets when necessary. Preserve standing focus and explain fallback. No targeting-access bypass |
| Range | Every new first-slice battle begins at Distant range between opposing fleet formations; later rounds maneuver normally |
| Ordinary observation | Show visible enemy ship types, hull condition and shield strength. Hide ammunition reserves and upcoming orders. Own ships remain fully inspectable |
| Cloak approach | One strategic fleet cloak setting. Ships with explicit capability approach cloaked when enabled; incapable members stay visible. Setting remains available for subsequent encounters |
| Cloak round one | Cloaked ships can fire normally at visible targets but cannot be offensively targeted. Fully cloaked sides have ordinary round-one fire without targeted return fire, not an extra volley or round. Normal power, opportunities, cadence and ammunition costs apply |
| Reveal | All surviving cloaked ships uncloak at the end of round one regardless of firing and remain uncloaked through the encounter |
| Mutual cloak | Two fully cloaked opposing sides have maneuver-only round one, then reveal. It counts toward the maximum of three rounds; no ammunition spent on nonexistent shots |
| Reactive defense | Incoming torpedoes can be intercepted even when the launching ship is hidden. Use actual defensive equipment/opportunities and normal costs; no separate player command or guaranteed interception |
| Aftermath | Survivors retain hull, four shield stocks and finite ammunition. No automatic between-battle recovery/refill; no objective/conquest rewards invented |
| Travel stop | Engagement stops participating fleets' remaining travel for that turn in the encounter sector. Interrupted routes cannot execute automatically after combat. Other fleets are unaffected |
| Next turn | Surviving fleets get fresh strategic orders: leave, stay to re-engage or follow. Pursuit uses actual strategic movement; renewed combat requires the same sector at the same time and lawful hostile engagement |
| Mutual re-engagement | Both staying and choosing combat creates a new encounter in the following strategic turn using retained ships/stocks. No round four or same-turn repeat loop |

This supersedes UX proposals for separate Escort and Transport formations screening across fleets, an allied protection picker, a player ship-priority dropdown and fully disclosed enemy equipment. Ship inspection remains distinct from attack targeting.

## Required source and resolver extensions

Add explicit authored cloak capability and any required state/power semantics; never infer them from faction or hull names. Preserve initial versus current stocks. Initialize current pools exactly once from validated initial provisions, then persist them by stable ship and exact fit revision. Destruction must not recreate initial ships or provisions on reload. Cloak setting/state must not grant resource recovery.

The existing v2 resolver's designated allied-formation protection and shared geometry are not the full agreed fleet model. Introduce an explicit campaign rules/profile boundary or a new pinned resolver identity where needed. Reject cross-fleet protection at the authoritative command boundary and ensure reactive defense cannot protect another fleet incidentally. Test fire access, defensive access, target acquisition, events and visibility, not merely menu restrictions.

Show with worked fixtures how a mixed capital/cruiser/escort fleet obtains individual capability contributions while remaining one command unit. A universal minimum-speed formation rating, unsupported averaged bonus or a detached ship-role label is insufficient evidence. Exact maneuver, mobility and automatic-role formulas are proposals until reviewed. Preserve the existing standalone-v2 format/rules/reference on its unchanged pathway; new rules receive independent vectors.

## Session-owned battle lifecycle

Simulation owns campaign, definitions, ships, fleet membership, pending encounters and mutable battle state. Application exposes detached lawful projections and stamped commands. Callers submit encounter/actor/round identity, observed stamps and allowed fleet order changes; never combatant snapshots, geometry, resource edits, RNG or accepted results.

At allocation freeze exact fit/provenance, survivors/current stocks, sides, rules/resolver identity and encounter-local seed. Human and baseline AI use the same ownership, phase, target, order and resource validation. Validate all initial, retained, loaded and newly submitted orders, including forbidden cross-fleet protection and within-battle exit maneuvers. Strategic cloak changes are not mid-battle re-cloaking commands.

Gather opposing orders from the same lawful start-round observations. Do not expose newly submitted opposing orders or let AI react to them. Persist accepted commitments if the transaction design allows a waiting-for-other-human boundary; otherwise define a safe host/session contract explicitly. Advance exactly one round after lawful commitments. Human round two/three choices must remain unresolved until made. Preserve standing orders and command/replay protection. Rejection changes no stocks, RNG, ledger or revision.

After each round expose filtered immutable results and the next authoritative boundary. No round four, UI simulation, result forgery or resource regeneration on inspection/replay. Complete encounters remain staged until the entire batch can finalize atomically in canonical order. Apply hull/destruction/ammunition/shields exactly once, stop participating routes, clear the combat batch and reopen Planning without another calendar advance or same-turn movement budget. Failed finalization preserves every staged result and the entire campaign.

## Strategic movement and renewed contact

An end-of-Movement-only detector cannot demonstrate stopping at an earlier meeting. Define a versioned opt-in shared travel timeline, not a fake chase check. Candidate exact rational event-time processing may be prototyped; it is a proposed implementation, not an approved movement formula. Batch simultaneous arrivals/departures consistently rather than giving an ordinal-ID first mover an advantage. Respect weighted-edge progress, navigation associations, known routes, physical reach and actual strategic movement budgets.

Demonstrate a faster fleet catching a slower one through actual movement, a slower pursuer failing, mutual departure, stationary mutual re-engagement, and mid-turn contact that cancels remaining travel. Path crossings alone are not battle, and common turn-end position is not a substitute for common sector/time. Opposite directions on an edge, departure from an already shared sector, and contact at a simultaneous arrival/departure boundary need explicit worked policies before activation.

No departure-sector catch roll, abstract remote route-interception contest, teleport or magical pursuit speed. Following uses lawful contact knowledge, never a hidden destination. Engagement intent/hostility and visibility must be defined without creating an entire new strategic fog-of-war system. For a controlled scenario, explicit lawful starting intelligence is acceptable if labelled and not leaked into default campaigns.

## Proposed defaults and open mechanics

The following are review candidates, not silently agreed gameplay:

- Preserve baseline v2 maneuver/weapon/armor/recharge rules wherever compatible. New individual-mobility mechanics must be explained separately; freeze all numerical rules in content/versioned inputs.
- Start facing Fore toward opposing formations at Distant range. Pairwise facing must remain meaningful in multi-fleet fights; there are no authoritative XYZ coordinates.
- Define deterministic automatic ship/fleet fallback ranking with stable ties; explain why it supports useful composition rather than making one build universally optimal.
- Keep a simple deterministic baseline opponent policy using only own resources and lawful visible enemy facts. Freeze choices before revealing player commitments. Do not claim advanced tactical/strategic AI.
- Save all authoritative current-batch round reports needed for resume and filtered review. Do not add a permanent campaign battle archive. Presentation-only unsent drafts remain outside campaign saves; saving should retain those edits locally, and successful load/restart restores committed state only. This save-UX behavior remains a proposal pending final interaction review.
- Use a separate authored opt-in live scenario with complete v2 sources. Existing v1 campaigns/blueprints retain their old path. Reject unsupported mixed-source live v2 encounters; any alternative routing needs an explicit policy, not conversion of aggregate stats.
- Keep advanced anti-cloak counter-detection and full operational cloak balance outside this initial scenario unless separately approved. Explain any baseline assumptions rather than implying detection can never defeat cloaking in the wider design.

Before activation settle shared travel/contact timing, capability/mobility formulas, cloak power/state semantics and any deviation from agreed behavior. Source/save/content/rules/resolver identities follow the implemented contract; do not guess the next version numbers. Preserve compatible legacy saves explicitly or reject new unsupported formats clearly. Old-save migration, v2 modular blueprint compilation and refits remain excluded.

## Delivery stages and proof

1. **Rules and contract proof:** inventory gaps and deliver explicit contract/version proposals plus an executable headless prototype of candidate mobility, fleet-local screening, cloak/visibility and shared travel timing. Prototype status must be clear; it cannot stand in for campaign authority. Review worked outcomes before production activation.
2. **Authoritative campaign integration:** implement accepted mechanics behind explicit opt-in routing, current ship pools, lawful rounds/commitments, checkpoint persistence and atomic aftermath. Prove actual campaign-owned Round 1 → save/load → altered Round 2/3 → finalize, then another strategic turn with depleted stocks. Activation remains off if any required gate is unresolved.
3. **Unity implementation:** only after the campaign proof and reviewed UX reconciliation. Use the real Application contract; separate native acceptance and exact-head portable CI. Documentation-only UX work can run in parallel with stages 1/2.

Independent PRs are preferred where a stage has a coherent testable boundary. A new inspection-only report is not completion of this milestone. If a required policy blocks implementation, provide a concrete executable/design alternative and exact blocker, not invented mechanics or claims of live play.

## Acceptance scenarios

| Scenario | Required evidence |
| --- | --- |
| Same route, separate fleets | Separate command/focus/membership and no cross-fleet protection |
| Capital/cruiser/escort fleet | Actual equipment/mobility contributions; finite screening opportunities; no label buff |
| Transport alone versus escorted in the same fleet | Only the latter obtains that fleet's escort contribution; a different friendly fleet supplies none |
| Focus destroyed/unavailable | Eligible visible fallback, retained standing focus, explanatory report; no hidden target |
| Fully cloaked versus visible | Ordinary round-one attack, no targeted return fire on hidden launchers, legal torpedo interception, reveal at boundary |
| Mixed cloaked fleet | Only visible members targetable in round one; no visibility leak through fleet aggregation |
| Both fully cloaked | Maneuver-only round one, no offensive ammo/rolls for absent shots, reveal, normal rounds two/three |
| Privacy | Enemy condition shown only when visible; no reserves/fresh-order leak through any projection, report, tooltip or diagnostic player view |
| Round saves/commitments | Repeat/permutation/culture/reload event equality, atomic rejection, actor isolation, no speculative later rounds |
| Two successive battles | Persist surviving ships and exact pools; cloak approach setting can apply again; no refill or same-turn repeat |
| Pursuit and shared travel | Physical same-sector/time contact, failed slow pursuit, no abstract catch roll or final-position-only shortcut |
| Multi-encounter failure | Full rollback on a later invalid aftermath; no early partial campaign damage or duplicate turn advance |
| Compatibility | Unchanged legacy v1 save/RNG and standalone-v2 golden; separate intentional new live-scenario vectors |

Run actual baseline and final locked restore/zero-warning Release/full suite, portable Unity preparation/static checks where changed, source and playground smokes, old and new canonical/reverse/repeated/reloaded campaigns, and representative large-fleet stress. Report actual counts from the chosen base, not stale totals. Final draft PR needs exact-head Linux/Windows/macOS CI and truthful implemented/gated/deferred status. Unity/native checks belong to the later client stage. No automatic merge is authorized by this preparation.
