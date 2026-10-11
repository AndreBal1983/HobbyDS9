# Science & Engineering foundation v1

Base: `a0f750681438d26ded17fff38cb3df37c3da9eb0`; branch `codex/science-engineering-foundation-v1`. Main stays unchanged pending local Unity acceptance.

S1 introduces portable explicit research-state/receipt/command payload contracts and independently tested Science functions. E1 integrates these with authoritative sessions and replaces the legacy research path; the live schema switches atomically there to save 13/content 9/rules `science-engineering-v1`. The interim S1 checkpoint preserves the existing executable campaign until replacement is complete. No Engineering application gameplay is added in S1.

Science disciplines are explicitly numbered Biotech, Computing, Construction, Energy, Propulsion, Weapons. Levels 1–10; Standard interval costs: 100,130,170,220,290,380,500,650,850. Pace factors: Fast 5000, Quick 6700, Standard 10000, Epic 15000, Marathon 30000. Costs use integer ceiling with BigInteger intermediates and checked conversion to long.

Discovery seeds are mixed from initial campaign seed and canonical civilization ID using FNV-1a and isolated SplitMix64. A separate local stream derives interval count (3–5), Fisher–Yates ordinal order, signed 10–15% raw weight deviations and largest-remainder integer normalization (ties by threshold index). Chunks are positive and sum exactly to cost. No shared campaign RNG is queried or advanced. Schedules are derived, never persisted.

Science allocation is floor(R&D × allocationBps / 10000); Engineering receives the exact remainder. Reserves bank, Science consumes only the selected discipline, crossing every ordered threshold and carrying through multiple levels. At level 10 progress is zero and remaining reserve banks without redirection. Explicit statistics and ordered receipts support conservation verification.

Remaining E1/I1 work: Engineering maturity/prerequisites, authoritative replacement/versioning, command dispatch/AI, scenario, detached projections, LCARS UI/receipts, client-only VSync default and new deterministic acceptance baseline. Combat resolver and RNG versions remain unchanged. No refinement, black-box technology, modular ships, diplomacy, espionage, graphics menu or unrelated gameplay changes are authorized.
