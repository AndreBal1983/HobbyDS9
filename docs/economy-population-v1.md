# Economy, planets and population foundation v1

Implementation contract for the next milestone. Save 10, content 6 and rules `population-economy-v1` deliberately reject older state; the phased combat resolver stays at version 1. All quantities are integers. Ratings/shares use 0..10000 basis points; products and weighted sums use BigInteger before checked conversion. Lists without gameplay order are canonicalized by ordinal IDs. Queues and routes retain their meaningful order.

Systems contain explicit planet entities linked to immutable planet definitions. A colony is a settlement/production identity on one planet; multiple planets/colonies per system are allowed. Ownership remains consistent with the system in this increment. Planet definitions contain nine habitat shares (sum 10000), independent natural capacities, gravity/atmosphere/pressure/radiation and deposits. Species contain nine suitability scores and environmental tolerance ranges. Neither politics nor hostility comes from species identity.

Natural capacities are shared, not duplicated for each species. Deterministic allocation ranks all cohort/habitat pairs by effective suitability descending, then habitat kind, species and cohort ID. Allocations consume one shared capacity pool. Inaccessible habitats are omitted. Infrastructure can add artificial capacity with its own habitability, without altering natural shares or species preferences. Environmental incompatibility reduces experienced habitability. Overflow is tracked as crowding. Derived allocations are not saved.

Infrastructure is a list of typed installed capabilities with a count: farming, fabrication, power supply/demand, research, logistics capacity, extraction, shipbuilding, accessibility and artificial capacity. Installation/building gameplay is deferred. Colony automation/prosperity/stability are bounded ratings; refugee acceptance is explicit colony state.

Economy executes inside the existing staging transaction. It produces local food and resource extraction, allocates logistics, consumes food, updates cohort needs/memories and stability, applies conservative growth, then performs normal migration and pending displacement. Research and production consume the new derived outputs afterward. Planning/AI still runs before economy; production costs must be available locally at command time and never trigger hidden imports.

Credits = floor(population / 100 * prosperity/10000 * (5000 + stability/2)/10000). Industry = powered installed fabrication * (10000 + automation)/10000; population is not its primary input. R&D = floor((installed research + population/500) * (5000 + stability/2)/10000). Food demand is ceil(population/100). Food shortage first reduces satisfaction and prevents growth; continued shortage (3 turns) loses up to 1% population per turn. Sufficient food permits up to 0.5% growth, constrained by free effective capacity. No population is silently clamped to capacity.

Stability target weights cohort satisfaction, civic alignment and inverse radicalisation by actual population (4:4:2), then applies food, prosperity and legal-status adjustments. Current stability moves at most 250 points per turn. Integration grows slowly after basic needs are met; affinity is independent of civic alignment. Memories have magnitude, source, start, expiry and deterministic per-turn decay. The initial refugee memory affects satisfaction modestly. There is no revolt/sabotage system.

Food imports cover local demand; resource imports cover a content-defined desired local stockpile. Same-owner worlds share supplies over the graph. Source surplus is physical local stock less its demand/reserve; sources and destinations are visited in ordinal colony order. Shortest BFS paths preserve origin, ordered systems and destination. Every delivery consumes shared undirected route capacity and source/destination logistics capacity. Hostile fleets on any path node fully block a route; hostile fleets on a neighboring node halve integrity. Unreachable paths deliver zero. Delivered = floor(min(need, remaining surplus, remaining route capacity) * receiving stability/10000 * integrity/10000). Only the delivered quantity leaves the source. No loss or empire warehouse is invented. Actual delivery receipts are retained for one turn to explain past transactions after stockpiles change.

Ships reserve/pay credits and explicit resource costs at enqueue. Queue items persist paid quantities. Cancellation refunds the unused industry fraction of each paid cost with floor rounding. Dilithium is the initial active named resource; materials are removed rather than renamed. Deposits are finite and extraction is limited by installed matching extraction capability.

Normal migration is simultaneous: compute plans from the same pre-migration state, cap departure at 1% of each source cohort, prefer improved opportunity/habitability/rights/safety/capacity/diaspora, penalize path length, and allocate by exact largest remainder with ordinal destination ties. Only same-owner connected worlds participate until diplomacy exists. Refugees use separate pending displacement records; their destination scores prioritize safety/access/acceptance/diaspora/fit/opportunity. Pending displaced people remain counted at source until actually moved. Refugees keep legal status/origin and gain an acceptance memory. Unplaced requests remain pending; no new invasion trigger is introduced.

Cohorts merge only with matching species/legal status/origin. Normal merging additionally requires near ratings and matching affinity profiles. Hard compaction chooses the closest compatible pair by deterministic distance/ID and population-weights ratings/affinities/memories until at most 32 cohorts remain. Distinct biological/legal/origin identities are never discarded: content/state may not require more than 32 distinct identity groups at a world, and migration cannot introduce a 33rd identity group. Memory keys coalesce by kind/source, preventing turn-by-turn arrival history from growing without bound. Removed cohort identities are retired; new ones use the campaign allocator.

Counters persist actual births/deaths/migration/refugee movement, extraction/delivery/consumption and completions for conservation/acceptance reporting. Current output/capacity/target stability are derived. Population politics, construction UI, diplomacy, freighters, weighted movement and industrial-seeder deployment are deferred.

## Deterministic acceptance baseline

The idle-Human 25-turn run ends in Planning turn 26/revision 187, with 129 accepted AI commands, 15 combat checkpoints/18 encounters, 6 ships/fleets, 2 queued items and 2 research unlocks. Next entity sequence is 80; campaign RNG is `850d3ff45bd6b85e`. The unchanged phased resolver executes 49 phases/113 shots/75 hits, applying 128 shield and 133 hull damage and destroying 13 ships/fleets. Canonical, reverse, repeated, reordered-content and repeatedly reloaded executions agree.

Final save SHA256:

```text
65a4f6b452fd8c6d8a80c775e6e74e944ac73f115bc3e7b9fb3e4e64da60f175
```

| Civilization | Credits | Population | Species |
|---|---:|---:|---|
| Federation | 229 | 1327 | Human 877, Vulcan 349, Andorian 101 |
| Cardassian Union | 110 | 1125 | Cardassian 1125 |
| Dominion | 110 | 1125 | Founder 1125 |
| Neutral Bajor | — | 1125 | Bajoran 1075, Cardassian 50 |

| World | Population | Stability / target | Food reserve |
|---|---:|---:|---:|
| Earth | 1125 | 8694 / 8691 | 89 |
| Mars | 202 | 4430 / 4430 | 0 |
| Cardassia Prime | 1125 | 8550 / 8550 | 92 |
| Founders' Homeworld | 1125 | 8550 / 8550 | 92 |
| Bajor | 1125 | 8550 / 8550 | 92 |

Counters: 501 births, 49 deaths, 14 normal migrants, 0 refugees in the authored scenario (100-person displacement exercised separately in tests), 3 Food delivered, 300 Dilithium extracted, 5 delivered, 32 consumed, 16 production completions. Population is conserved after births/deaths; migration/refugees only transfer it. Deposit + stock + queued reservations + cumulative consumption is conserved, including combat destruction. Cancellation records the unrefunded resource fraction as consumption.

Mars deliberately demonstrates a low-population industrial world dependent on imports. Conservative receiving-stability/route rounding and nearby hostile interference can leave it short of Food; the idle-player baseline records those consequences rather than silently supplying it. Earth remains an operational human shipyard.

Versions change from save 9 → **10**, content 5 → **6**, rules `phase-1-phased-combat-v1` → **`population-economy-v1`**. Resolver `combat-resolver.phased` version **1** is unchanged. No automatic migration exists. Required new DTO collections must be present; host JSON readers reject unknown fields and missing authoritative value-type fields, and simulation validation rejects invalid content/state. Failed client loads retain the current session.

Memory source IDs are historical references: canonical IDs may outlive their original entity. Refugee arrivals record cause/responsible identity, source planet and acceptance, retaining their legal/origin identity. Natural species preferences remain distinct from political ratings. Refugee acceptance is an explicit per-world boolean until a later policy system. Fixed infrastructure, shared system ownership, unit-cost routes, one aggregate artificial-capacity bucket and same-owner routine logistics are deliberate v1 limits. Capacity and immigration never duplicate habitat space per species.
