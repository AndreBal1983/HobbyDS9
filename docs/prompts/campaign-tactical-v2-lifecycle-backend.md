# Codex 1 campaign tactical v2 lifecycle assignment

Status: **prepared assignment for review; not dispatched**. Read [the lifecycle milestone](../campaign-tactical-v2-lifecycle-spec.md) in full. All agreed player behavior is mandatory; numerical/technical proposals remain labelled proposals. This assignment does not authorize merging PR #19 or any later PR.

## Starting conditions

Verify fresh main and the equipment PR's final status/head. Last inspected equipment head is `be02572fc3fc2cfdb90cdd9ad763bbdf3d79716b` in PR #19, based on merged main `53d2e64973d9081a4d0090e771f5e777fc55cf40`. Do not assume either is still current. Use an isolated `codex/campaign-tactical-v2-lifecycle` worktree from the reviewed accepted equipment checkpoint. If it is not yet accepted, report that dependency and prepare the design/prototype in isolation without claiming accepted-main integration.

Strictly headless. No Unity files, Editor, native player, desktop input or changes to another chat's worktree. Portable contracts/simulation/Application remain netstandard2.1/C#9. Read repository instructions, design index and tactical/ship/AI/time chapters, architecture, source/blueprint/save contracts, equipment report and latest UX decision reconciliation. Run the actual baseline first.

## Stage A — make unresolved mechanics reviewable

Do not repeat equipment inspection as if it were live integration. Identify exact mismatches: shared formation geometry versus individual mobility, cross-formation defense versus fleet-local protection, missing cloak/observations, initial provisions versus persistent resources, and end-of-Movement detection versus mid-turn travel stop.

Deliver typed API/version proposals and executable headless candidate examples for:

- One command unit per fleet with capability-based composition and individual mobility contribution, no hard fleet cap or blanket slowest tactical speed.
- Internal fleet screening only, ordinary round-one cloak attack/target protection, automatic reveal, mutual-cloak maneuver-only round, reactive torpedo defense and lawful report filtering.
- Exact simultaneous strategic travel/contact timing with ordinary pursuit, weighted-edge progress, departure/shared-sector cases and stopped routes.
- Session-owned joint round orders, baseline lawful AI, current ship stocks, saved commitments/boundaries and atomic batch aftermath.

Clearly label provisional rules/fixtures and any proposed formulas. Compare representative alternatives using concrete outcomes rather than presenting guessed constants as approved balance. No prototype DTO, host simulation or fixture result may be imported as trusted campaign state. Seek review of consequential unresolved behavior before enabling activation; routine implementation choices within the agreed contract need no repeated permission.

## Stage B — complete the accepted campaign path

Once the consequential contract/rule choices are resolved, implement explicit versioned opt-in campaign allocation, exact fit/stock provenance, fleet cloak setting, lawful observations, stamped human/AI order validation, one-round resolution, persisted boundaries and atomic consequences. Preserve independent fleets, automatic target fallback, three-round cap, all cloak timing and stock rules. Reject forged results, foreign/stale/replayed orders, hidden offensive targets, cross-fleet protection and within-battle exit maneuvers before mutation/RNG.

Implement agreed strategic movement stop and following-turn decisions through the reviewed physical travel timeline. No separate pursuit success roll, hidden-route following, route auto-resume after battle or same-turn rematch. Prove the live path from campaign-owned ships through two successive battles and save/reload, not only resolver fixtures.

Maintain existing v1 campaigns and frozen blueprints without synthesized v2 equipment. Pin new content/save/rules/resolver identities deliberately; preserve old goldens on unchanged paths and add independent new vectors. Do not silently mutate standalone-v2 semantics to make campaign tests pass. Keep activation disabled and report exact missing gates if the authoritative path is incomplete.

## Evidence and delivery

Use the lifecycle acceptance matrix, meaningful transaction/ownership/privacy/resource/movement tests, actual scenario/Application CLI walkthrough, canonical/reverse/repeated/reload equality, large-fleet stress and portable preparation/static checks. Run locked restore, zero-warning Release and all starting tests plus additions. Verify exact-head three-platform CI for every delivered implementation candidate.

Deliver one or more coherent draft PRs with head SHA, precise stage completion, version matrix, APIs, CLI reproduction, old/new hashes, test counts, prototype versus authoritative status and remaining gates. Update the lifecycle implementation report without relabelling agreed/proposed/open decisions. No automatic merge, task messaging or Unity implementation.
