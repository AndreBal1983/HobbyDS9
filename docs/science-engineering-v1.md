# Science & Engineering foundation v1

This revises the unaccepted `242247de4331e5b62896cac631848024940e26af` candidate on `codex/science-engineering-foundation-v1`. Main remains at accepted `a0f750681438d26ded17fff38cb3df37c3da9eb0`. R1 implements authoritative fixed-point arithmetic, six-way Science and Engineering kinds. R2 completes client presentation and local Unity acceptance instructions.

## Authoritative units and allocations

`HobbyDS9.Contracts.ResearchUnits.Scale = 600` is the portable conversion constant: one conceptual R&D point equals 600 internal units. Economy output stays conceptual; Research converts it exactly once using checked/BigInteger arithmetic. Costs are internal units; Science's conceptual table is scaled before pace adjustment. Standard internal interval costs are 60,000 / 78,000 / 102,000 / 132,000 / 174,000 / 228,000 / 300,000 / 390,000 / 510,000. Pace factors remain Fast 5000, Quick 6700, Standard 10000, Epic 15000, Marathon 30000, with ceiling cost adjustment.

Persisted `ScienceAllocationUnits` ranges 0–600; DS9 starts at 420 (70%). Science receives floor(total internal × allocation / 600); Engineering receives the remainder. Each civilization requires six `(ScienceDiscipline, Weight)` entries, exactly one per explicit enum, nonnegative weights summing to 600. DS9 starts with 100 each. A conceptual output of four generates 2400 units, splits 1680/720, and spends 280 on each discipline. No lost points, rounding debt or alternating 50/50 artifact.

`SetResearchAllocation` (6), `SetScienceAllocation` (7, complete desired six weights) and `SetEngineeringTarget` (8, nullable target to clear) are stamped commands. Malformed/unchanged allocations, stale stamps, nonhuman actors, duplicates and combat-phase submission return stable rejection codes without changing any authoritative state. No authoritative single-focus field or command remains.

## Science progression and bank

All six disciplines progress in one Research phase. The entire available Science bank, including new income, is distributed once using exact largest remainders: floor each share, then residual units by descending fractional remainder and enum-order ties. Each nonmaxed discipline spends its budget across thresholds/levels. A maxed discipline's share or surplus reaching level 10 remains banked; there is no recursive redistribution in the same phase. Next phase the whole bank is redistributable once under the current weights. This neither redirects a maxed share silently nor double-spends it.

Discovery schedules retain persisted isolated seeds, FNV/SplitMix derivation, 3–5 breakthroughs, Fisher–Yates structural ordinal shuffle and ±10–15% chunk weights normalized by exact largest remainders. Scaled costs are used before chunk calculation. Schedules are derived, not saved. Campaign RNG is untouched. Level 10 is terminal with zero progress. Receipts/counters include per-discipline internal spending.

AI retains authored 70/30 and equal starting Science. If a positively allocated discipline is maxed, AI deterministically redistributes equally among remaining disciplines, with enum-order residual units, through the same trusted stamped command handlers. Humans are never redistributed automatically. Engineering keeps a valid target, otherwise selects available incomplete projects by remaining cost then ordinal definition ID. No research AI RNG.

## Engineering

Explicit kinds: `NewCapability = 1`, `Refinement = 2`, `CivilApplication = 3`. One target at a time; reserves bank and progress survives retargeting. Projects progress 0–100%, with existing Development/Prototype/Operational/Mature bands. Only completion unlocks prerequisites/ship designs; no maturity above 100%.

NewCapability and CivilApplication complete through the existing mature-definition unlock boundary. Refinement additionally applies a declared absolute value to a required, persisted implementation parameter (0–10000) before recording completion. This small typed parameter change is not an effect scripting language; neutral fixtures test 70%→90%. No fake Phaser subsystem or live civil economy hook was invented.

DS9 authors one project: Advanced Starship Engineering, NewCapability, cost 36,000 internal (60 conceptual), with Construction interval 1/ordinal 1 and Propulsion interval 1/ordinal 2. Completion unlocks Advanced Scout. Both structural requirements update eligibility immediately. Definitions validate kinds, effects, references, requirements, prerequisite cycles and cost overflow; all state/copies/projections retain parameters and allocation lists.

## Versions and acceptance baseline

Save **13**, content **9**, rules **science-engineering-v1**. Resolver **combat-resolver.phased v1** and RNG identity/version unchanged. Save 12/content 8 are rejected. Intermediate saves from the previous unaccepted 13/9 candidate are not migrated; required new fields reject them rather than repairing/normalizing them.

Canonical 25-turn save SHA256:
`83bdb9013db7ac7b2b33ef77c4b591a4bca065544a01912eb48c48161688f5c4`.

Turn 26, Planning, revision 185, AI commands 131; RNG `d8919406d6e01cf1`, next entity 97. 17 combats / 12 checkpoints; 43 phases, 98 shots, 68 hits, 126 shield and 116 hull damage, 10 ships/fleets destroyed, zero retreats. Nine ships/fleets remain; two queued orders, 16 production completions. Movement cost paid 189, 111 completed edges, 26 partial-travel turns. Births 501, deaths 50, migration 31, refugees 0, food delivered 13, resources extracted/delivered/consumed 275/5/32; zero political disruptions.

Each civilization: 100 conceptual = 60,000 internal generated; Science allocated/spent 42,000; Engineering allocated/reserved 18,000; Engineering spent and Science reserve zero. Each of six disciplines remains level 1, progress/spent 7000; no breakthroughs/levels/completions. Aggregate: **180,000 generated = 126,000 Science spent + 0 Engineering spent + 0 Science reserve delta + 54,000 Engineering reserve delta**. Starting reserves are zero; authored nonzero starting banks are starting stock, not generated income. Canonical/reverse/repeated/reordered/reload paths must agree on the entire final save, including campaign RNG and allocation state.

## Deferred

Modular ships, a real Phaser subsystem, >100 maturity, broad effect scripts, diplomacy, espionage, black-box/traded tech, leaders/scientists, faction/species bonuses, procedural tech text, automatic human allocation and multiple concurrent Engineering targets remain out of scope. Economy, politics, movement, combat, production and audio rules are unchanged. VSync remains 1 with targetFrameRate −1 and no graphics settings UI.
