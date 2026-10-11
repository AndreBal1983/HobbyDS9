# Science & Engineering foundation v1

Base: `a0f750681438d26ded17fff38cb3df37c3da9eb0`; branch `codex/science-engineering-foundation-v1`. Main stays unchanged pending local Unity acceptance.

S1 introduces portable explicit research-state/receipt/command payload contracts and independently tested Science functions. E1 integrates these with authoritative sessions and replaces the legacy research path; the live schema switches atomically there to save 13/content 9/rules `science-engineering-v1`. The interim S1 checkpoint preserves the existing executable campaign until replacement is complete. No Engineering application gameplay is added in S1.

Science disciplines are explicitly numbered Biotech, Computing, Construction, Energy, Propulsion, Weapons. Levels 1–10; Standard interval costs: 100,130,170,220,290,380,500,650,850. Pace factors: Fast 5000, Quick 6700, Standard 10000, Epic 15000, Marathon 30000. Costs use integer ceiling with BigInteger intermediates and checked conversion to long.

Discovery seeds are mixed from initial campaign seed and canonical civilization ID using FNV-1a and isolated SplitMix64. A separate local stream derives interval count (3–5), Fisher–Yates ordinal order, signed 10–15% raw weight deviations and largest-remainder integer normalization (ties by threshold index). Chunks are positive and sum exactly to cost. No shared campaign RNG is queried or advanced. Schedules are derived, never persisted.

Science allocation is floor(R&D × allocationBps / 10000); Engineering receives the exact remainder. Reserves bank, Science consumes only the selected discipline, crossing every ordered threshold and carrying through multiple levels. At level 10 progress is zero and remaining reserve banks without redirection. Explicit statistics and ordered receipts support conservation verification.

Remaining I1 work: LCARS Science & Engineering UI/receipts, client-only VSync default, final acceptance documentation and Unity EditMode coverage. Combat resolver and RNG versions remain unchanged. No refinement, black-box technology, modular ships, diplomacy, espionage, graphics menu or unrelated gameplay changes are authorized.

## E1 authoritative integration

The session owns explicit Science/Engineering state. The closed envelope supports `SetResearchAllocation` (6), `SetScienceFocus` (7) and `SetEngineeringTarget` (8, nullable target to clear); old generic research command/state/definitions are removed. Ordinary rejections use stable codes and preserve state/RNG/allocation/revision; accepted commands change plans only. The Research phase allocates and spends on deep turn staging.

Engineering retains per-application progress when switching/clearing targets. Maturity is floor(progress × 100 / adjusted cost): Development 0–24%, Prototype 25–49%, Operational 50–99%, Mature 100%. Only Mature unlocks designs/prerequisites; completion clears the target and banks surplus. Science requirements use structural ordinals, not positional indices; higher levels satisfy earlier intervals. Validation rejects missing references, duplicate requirements, cycles and unrepresentable adjusted costs.

DS9 starts at Standard pace, 7000 Science BPS, Construction focus, six level-1 disciplines and independent persisted discovery seeds. Advanced Starship Engineering costs 60 and requires Construction interval 1/ordinal 1 and Propulsion interval 1/ordinal 2. Advanced Scout requires its Mature application. No designs or economy outputs were added.

Save 13/content 9/rules `science-engineering-v1` reject save 12/content 8; no automatic migration is provided for this replacement. Campaign RNG and combat resolver identities are unchanged. Detached research projections include nested facts and bounded canonical latest-turn receipts. Nonzero authored starting reserves are starting stock rather than generated R&D; allocation counters record generated points only. Actual scenario starting reserves are zero.
