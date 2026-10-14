# Authored tactical v2 equipment foundation implementation prompt

Status: **prepared next backend assignment**. Scope reflects the owner's 13 October 2026 decisions. Do not launch until the chosen starting checkpoint includes the reviewed source-inspection capability. This prompt prepares equipment; it does not enable campaign v2 or complete the Unity target.

## Outcome

Replace missing-source blockers with a complete, explicitly authored tactical-v2 equipment contract for a new opt-in scenario. Demonstrate that real scenario-owned ships can supply lawful complete fits and initial resources without converting legacy aggregate statistics or using a standalone battle fixture as campaign authority.

Work in a clean isolated `codex/tactical-v2-authored-equipment-foundation` worktree from a verified accepted checkpoint. Fetch and record actual main; do not assume `703e395b793af41953d440d04d69e0744909caf5` is current. Deliver a draft PR and precise acceptance evidence; no automatic merge or edits to another chat's worktree. Strictly headless: no desktop, Unity Editor or native player interaction.

## Read first

Read AGENTS.md, the design index, ship-design and tactical-combat chapters, architecture, blueprint-construction, tactical-combat-v2-backend/playground, and campaign-tactical-combat-v2-integration. Read the final source inspection API and its tests, actual scenario/content validation, standalone resolver validation and save compatibility. Consult the accepted designer integration report when available to avoid conflicting catalogue/publication changes.

The equipment milestone must preserve agreed/proposed/open labels. Inspect the implementation before selecting exact version identities or promising compatibility. Current inspection always reports activation blocked; merely adding data must not flip that gate.

## Agreed scope boundaries

- First campaign-v2 slice uses authored v2 ships in an opt-in scenario. V1 authored designs and immutable v1 blueprint revisions keep their old compiler and resolver behavior.
- Ammunition and sector shield damage will persist in the subsequent lifecycle milestone; no automatic refill or between-battle recovery in the initial scenario. In-battle recharge follows the existing v2 resolver.
- First live battles will use destruction/basic three-round resolution, with strategic retreat/objective exits excluded. This milestone implements neither rounds nor aftermath.

## Required implementation

1. Inventory exact missing fields and create additive typed source definitions for complete authored fits. Include explicit weapon class, mount compatibility/binding/arcs, per-package damage/accuracy/tracking/opportunities/cadence and power, finite magazines and initial stock, armor, four fixed shield maxima and initial current values, recharge and its power requirement, and explicit per-ship tactical capabilities. Every required field must distinguish omission from a valid zero or an explicitly empty list.
2. Keep definitions in authoritative scenario content, with provenance/stable IDs and detached projections. No name parsing, scalar attack splitting, default ratings, inferred torpedo damage, fallback equipment or current/latest blueprint substitution. Numerical sample values must be explicitly authored and labelled provisional test-scenario values, not universal approved balance.
3. Define and validate the bridge's power accounting. Account for mandatory life support, command, propulsion and targeting reservations as well as weapons/shields. Do not give the standalone resolver the whole reactor output after ignoring those commitments. Require explicit feasible allocations; no free scaling of partially powered v1 weapons. Present proposed reservation semantics clearly in the milestone report.
4. Provide pure deterministic validation/projection of complete static fits through Simulation/Application. Distinguish source completeness from lifecycle readiness: legacy sources stay typed ineligible; complete new sources still report campaign activation disabled until rounds/persistence/aftermath exist. A caller-created projection cannot be loaded as a trusted source or submitted as results.
5. Prepare a separate explicitly labelled opt-in source scenario/package using the new authored definitions and scenario-owned ship identities. It may remain a source-validation scenario and use no new live combat path. Do not advertise it as a playable v2 campaign. Initial ammunition must be explicit provisioning, never generated at each inspection. Construction/payment semantics are future work unless already supported and separately in scope.
6. Version any changed serialized content deliberately. Preserve the unchanged legacy package bytes/loader/blueprints where supported; reject unsupported formats clearly. Do not reserve guessed Save 15 / Content 11 merely because the prior report suggested them. Add no persisted runtime ammunition/shield fields in this equipment-only assignment unless a necessary contract decision is explicitly documented and tested; avoid premature campaign save changes.

Keep portable libraries netstandard2.1/C#9 and all authority in Simulation. Unity, ShipDesignApplication's v1 compiler/publication path and existing blueprint UX are outside this assignment. Use dedicated source files/interfaces where practical; document unavoidable shared changes.

## Exclusions and remaining design work

No live encounter activation, round commands, tactical AI expansion, combat consequences, refits, old-save migration, modular v2 blueprint compiler, resupply/recovery economy or Unity edits. Do not implement formation aggregation or initial battle geometry from invented ratings/formulas. Inventory the explicit ship capabilities needed for the next design round and propose alternatives with worked examples; retain unresolved formulas as proposals.

## Meaningful acceptance

Run the actual starting checkpoint's locked restore, Release build and complete test baseline, then repeat on final code. Test valid/invalid complete equipment, absent-versus-zero fields, explicit unarmed fits, mount/coverage failures, cadence/stock/cap bounds, essential-power reservations, overflow, immutable projections, foreign actor/source handling and deterministic reorder/culture/save-content round trips where applicable.

Exercise the real scenario/session/Application inspection path with authored v2 sources. Show complete equipment while activation remains disabled for named lifecycle blockers. Show v1 static/custom sources remain ineligible and exact frozen revisions are unchanged. Repeated inspection must not mutate state, RNG, resources or command ledger. Preserve legacy campaign canonical/reverse/repeated hashes and the separate standalone-v2 reference.

Run PrepareUnity portable preparation and static checks when the portable boundary changes. Verify exact-final-head Linux/Windows/macOS CI; do not claim Unity runtime acceptance from those checks. Test counts must preserve the actual starting inventory, including PR #16's cases, plus meaningful new coverage.

## Delivery

Write `docs/tactical-v2-authored-equipment-foundation.md` with source/version matrix, schema/projection API, explicitly provisional example data, power accounting, valid/invalid CLI walkthrough, completeness versus activation status, exact test/CI evidence and remaining lifecycle decisions. Include head SHA, draft PR, commands, changed identities and unchanged-path goldens. Do not merge, contact the other implementation chat or start the lifecycle milestone as part of this assignment.
