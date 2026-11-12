# Engineering development tree — 20 selectable projects

Implementation candidate, 12 November 2026, on `codex/engineering-projects-20`, based on main `e99c151`. The owner requested **20 selectable projects**, not simultaneous Engineering allocations. This authors 19 projects in addition to Advanced Starship Engineering.

The [ship design chapter](design/ship-design.md#engineering-hulls-and-components) supplies the intended Science → Engineering → deployable-version loop. The numerical costs, gains and trade-offs below are initial implementation balance, not previously agreed design constants. The [Science & Engineering foundation](science-engineering-v1.md) still governs spending, maturity and one active target.

## Player behavior

The Research screen lists all 20 projects in a bounded scrolling development list with Science prerequisites, project prerequisites, cost, intended outcome, saved progress and Locked / Available / Current / Completed states. Clear target remains outside that list. A locked project cannot be developed until the authoritative conditions are satisfied. Projects consume the existing Engineering reserve and income; switching or clearing targets retains progress. Only 100% completion releases its output. No free output, new allocation currency or >100% maturity is introduced.

Projects release immutable catalogue versions through the existing campaign-owned version-access gates. The designer and publication commands use that access; callers cannot grant themselves a version. New fits must still satisfy all capacity, power, mount and dependency checks. A later reactor, engine or shield does not modify existing ships, queued construction, older components or published blueprint revisions. This is not a refit implementation.

## Projects and dependencies

Science notation: “Energy 1 / 2” means interval 1 → 2, structural breakthrough ordinal 2, under the existing per-civilization deterministic schedule. More advanced Science levels also satisfy that requirement. Workload is in conceptual Engineering points before campaign pace adjustment; one point is 600 internal units.

| Project | Kind | Workload | Science prerequisites | Mature project prerequisites | Output / trade-off |
| --- | --- | ---: | --- | --- | --- |
| Advanced Starship Engineering | New capability | 60 | Construction 1 / 1; Propulsion 1 / 2 | None | Unlocks Advanced Scout production. |
| Compact Reactor | Refinement | 40 | Construction 1 / 1; Energy 1 / 1 | None | Reactor space 18 → 15 at the same 60 output; credits 6 → 8 and industry 9 → 12. Version: `component.reactor.v2`. |
| High-output Reactor | New capability | 75 | Energy 2 / 1 | Compact Reactor | Reactor output 60 → 72; space rises to 20, credits 10 / industry 16. Hull distribution still limits usable power. Version: `component.reactor.v3`. |
| Efficient Drive | Refinement | 45 | Propulsion 1 / 1 | None | Cruise full/minimum power 30/15 → 24/12 at the same movement 4; credits 2 → 4 / industry 3 → 6. Version: `component.propulsion.v2`. |
| High-speed Drive | New capability | 80 | Propulsion 2 / 1 | Efficient Drive | Cruise movement 4 → 5 at full allocation; space 16 → 20, cruise full/minimum power 32/16, credits 6 / industry 10. Version: `component.propulsion.v3`. |
| Maneuver Drive | Refinement | 55 | Propulsion 1 / 2; Computing 1 / 1 | None | Full-power combat evasion 1000 → 1500 basis points; combat full/minimum power 12/6 → 16/8, credits 5 / industry 8. Version: `component.propulsion.v4`. |
| Resilient Shield | New capability | 50 | Energy 1 / 2 | None | A new higher-capacity shield variant: full-power capacity 20 → 28; space 12 → 15, full/minimum power 18/9, credits 5 / industry 8. Version: `component.escort-shield.v2`. |
| Efficient Shield | Refinement | 35 | Energy 1 / 1 | None | Shield full/minimum power 8/4 → 6/3 at capacity 8; credits 2 → 4 / industry 3 → 6. Version: `component.small-shield.v2`. |
| Multilayer Shield | New capability | 85 | Energy 2 / 2 | Resilient Shield | Full-power shield capacity 28 → 36; space 15 → 18, full/minimum power 22/11, credits 8 / industry 12. Uses existing shield rules, with no new resistance or regeneration mechanic. Version: `component.escort-shield.v3`. |
| Compact Energy Bank | Refinement | 40 | Construction 1 / 1; Weapons 1 / 1 | None | Energy-bank space 8 → 6, unchanged firepower and mounts; credits 2 → 4 / industry 3 → 6. Version: `component.energy-bank.v2`. |
| Long-range Energy Bank | New capability | 65 | Weapons 1 / 2 | Compact Energy Bank | Full-power long-range attack 4 → 7; close attack 12 → 8, full/minimum power 10/5, credits 5 / industry 8. Version: `component.energy-bank.v3`. |
| Improved Launcher | Refinement | 55 | Weapons 1 / 1; Computing 1 / 1 | None | Full-power long-range attack 8 → 11; space 8 → 10, full/minimum power 10/3, credits 5 / industry 8. Retains the campaign v1 aggregate combat model. Version: `component.launcher.v2`. |
| Compact Life Support | Refinement | 30 | Biotech 1 / 1; Construction 1 / 1 | None | Life-support space 12 → 9 at crew support 40; credits 2 → 4 / industry 3 → 6. Version: `component.life-support.v2`. |
| Efficient Life Support | Refinement | 50 | Biotech 1 / 2 | Compact Life Support | Essential life-support power 6 → 4 in each profile; space 9 → 11, credits 5 / industry 8. Version: `component.life-support.v3`. |
| Expanded Computing | Refinement | 40 | Computing 1 / 1 | None | Computer support 60 → 90 in all profiles; space 8 → 10, essential power 4 → 5, credits 4 / industry 6. Version: `component.command.v2`. |
| Precision Fire Control | Refinement | 65 | Computing 1 / 2; Weapons 1 / 1 | Expanded Computing | Command combat accuracy output 7000 → 8000 basis points; essential power 5 → 7 in all profiles, credits 6 / industry 10. Final accuracy still follows compiler aggregation and caps. Version: `component.command.v3`. |
| Long-range Sensors | New capability | 45 | Computing 1 / 1; Energy 1 / 1 | None | Survey sensor rating 10 → 15; space 8 → 10, Survey full/minimum power 10 → 12, credits 4 / industry 7. Sensor rating is a designer preview; no campaign detection bonus is implemented. Version: `component.survey-sensors.v2`. |
| Compact Laboratories | Refinement | 45 | Construction 1 / 1; Computing 1 / 1 | None | Laboratory space 22 → 18 at Science rating 100; credits 2 → 4 / industry 3 → 7. Science rating remains a designer preview, not colony R&D income. Version: `component.labs.v2`. |
| Reinforced Utility Hull | Refinement | 60 | Construction 1 / 2 | Advanced Starship Engineering | New hull version: strength 30 → 40; usable capacity 100 → 95, credits 10 → 14 / industry 20 → 28. Mount architecture unchanged. Version: `hull.utility.v2`. |
| Expanded Utility Hull | New capability | 75 | Construction 2 / 1; Energy 1 / 2 | Advanced Starship Engineering; High-output Reactor | New hull version: capacity 100 → 110 and distribution 60 → 72; credits 18 / industry 34 / maintenance 3. Mount architecture unchanged. Version: `hull.utility.v3`. |

Engine branches are Efficient Drive → High-speed Drive and the separate Maneuver Drive. Shield branches are Efficient Shield and Resilient Shield → Multilayer Shield. Reactor, life-support and computing refinements likewise preserve their earlier cheaper versions. Hull projects preserve their original mount architecture. Capacity, full/minimum power, compiler aggregation and hull distribution limits still apply; a label never grants extra capability.

## Scope and compatibility

The new shield variants use existing aggregate shield-capacity rules; they introduce no resistance types, regeneration, directional shield mechanics or tactical-v2 activation. The improved launcher retains existing campaign-v1 aggregate combat. Sensor and Science outputs remain explicitly labelled designer previews: these projects do not add campaign detection, mission or colony R&D bonuses. Simultaneous weighted Engineering portfolios, continuous refinement/overmaturity and refits remain deferred.

Refinement validation now accepts either its existing declared implementation-parameter change or a real Engineering-gated **derived** hull/component version. An effect description alone, a missing output or gating an unchanged base version is insufficient. The existing parameter path remains tested. Completion of a version-based refinement records maturity; access is derived from that authoritative state rather than mutating catalogue inputs.

No DTO field, save/content format, compiler, combat resolver or RNG identity changes. The scenario content hash changes, so saves bound to the previous authored content correctly fail content-identity validation against this candidate; no migration or silent rebinding is added. Initial state, discovery seeds, existing project, v1 hull and all v1 components remain identical to main.

## Verification

- Locked restore and .NET Release build passed with zero warnings/errors.
- Complete integrated .NET suite: **1,851 passed, zero failed/skipped**; includes all 20 campaign-authoritative completion/reload cases, retarget/clear progress retention, prerequisite and malformed-refinement rejection, automatic AI selection and published-revision isolation.
- Portable PrepareUnity passed and copies the new scenario to the client Resources input.
- Canonical and reverse 25-turn headless runs agree: save SHA256 `7175f06f9ee72be793acacba3e40f20313169aba6d7602a6a6b5c419d555053c`, RNG `d8919406d6e01cf1`, nine ships/fleets, next entity 97.
- The full-tree long-run test compares continuous, repeated, reordered and resumed paths. Normalizing **only** the changed content identity reproduces the existing gameplay golden `fb2b75f18c8c5423f3a0850f38f6e53ee8249d3d08385978b5b2532041cc6089`; the accepted gameplay golden was not replaced. Historical one-project mechanics fixtures remain isolated; new-tree tests explicitly load the full authored catalogue.
- Compiler-only validation of the actual Unity runtime and EditMode test sources against installed Unity 6000.6.5f1 libraries passed: zero errors and four existing obsolete-object-lookup warnings in audio sources/tests. This does not execute Unity or tests.
- Unity EditMode coverage was updated for multi-project lookup and a 20-card/scroll-container/read-only rendering check. Unity Editor, native visuals and a Windows player build have not been executed for this change. No desktop control was used.

Scenario SHA256: `a9d631c84ab8689718c9f5199877694fa4b027ac774e3ee0f1a6b9bb31fa692f`. This is a local implementation candidate, not a merged/released feature or a claim of remote CI success.
