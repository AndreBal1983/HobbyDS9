# Population politics and strategic geography v1

This milestone builds on accepted main 90b324a; it does not replace the population/economy foundation. All mutations remain staged; the client receives detached projections. Implementation proceeds through green P1, G1 and I1 checkpoints. Main remains unchanged until local Unity acceptance.

## Political rules

World governance is a content definition referenced by durable political state on each inhabited settlement. Four profiles trade inclusion/autonomy against immediate security/extraction. Profiles specify bounded per-turn cohort adjustments, economic/logistics multipliers, suppression, refugee acceptance and conditional legal transitions. Only stamped commands can change policy; derived ratings/resistance have no public setters. Species is not an input to political loyalty or pressure.

Political updates follow local food consumption and memory decay. Each cohort receives ordered reason components from needs, prosperity, environmental pressure, legal treatment, policy, bounded integration/alignment context and active content-mapped memories. Changes are clamped to 0..10000; integration moves much more slowly than satisfaction. Affinity alone produces no penalty: external conflict effects await a real diplomacy/war model rather than treating every foreign culture as permanently at war. Conditional status transitions require sustained eligible turns and reset progress when conditions cease. Historical memory source IDs may outlive entities.

Population-weighted pressure combines discontent, low alignment, radicalisation and legal-status strain, with food/instability context and policy suppression. Resistance uses sustained pressure with distinct escalation/recovery thresholds and timers. Quiet/Tense/Unrest/OrganizedResistance never transfer ownership. Organized resistance causes scheduled, finite-duration efficiency disruption; no RNG or arbitrary infrastructure deletion is used. Projections retain ordered latest-turn rating causes and transition/pressure facts. Full rebellion, policing, occupation/conquest and espionage are deferred.

## Navigation and encounter rules

The authoritative navigation graph will store independent node IDs, optional system associations, explicit edge IDs/base costs and phenomena. Runtime modifiers alter effective cost/budget without changing topology. Knowledge is per civilization and gates both route search and submission; hidden edges never appear in previews. Weighted route search uses Dijkstra, ordinal IDs for deterministic ties, positive effective costs and checked integer totals.

Fleets store their current navigation node, ordered navigation route and integer partial-edge progress. Their budget is the slowest ship's content movement rating, with explicit local inhibition; unused budget is discarded. Expensive edges accumulate progress over turns. A fleet in transit cannot receive a route replacement that discards paid progress. ETA uses the same budget/cost function as movement. Geometry and labels are presentation-only.

Multi-edge movement must check contested nodes during deterministic movement rounds and stop fleets there for encounter creation. Opposing fleets cannot traverse an already contested node and continue. Fleets in edge transit are not stationed at their origin for encounters or blockade. Combat location must describe the actual navigation node, optionally associated with a star system; resolver battle rules remain unchanged. Production spawns at the system's explicit navigation association.

Logistics uses weighted navigation paths with the existing physical source/destination conservation and separate route capacity. It does not borrow fleet budgets. Politics modifies output/logistics efficiency explicitly. Artificial scatterers add cost; inhibitors reduce local budget. No final technologies, procedural/Voronoi generation or diplomatic freight network are implemented.

## Versioning and acceptance

P1 versions political state/content explicitly; G1 versions independent navigation/progress separately. Required DTO state cannot be silently inferred from older formats. The combat resolver remains combat-resolver.phased v1. Exact final versions, formulas, counters and measured canonical/reverse/reload/reordered baseline will be recorded after integration. Cloud checks cannot establish Unity Editor or Windows IL2CPP acceptance.

## P1 checkpoint

Save 11/content 7/rules `population-politics-v1` supersede 10/6/`population-economy-v1`; combat remains v1. Policies and memory effects are explicit catalog definitions. Settlements persist pressure/history, resistance/timers/disruption counts and latest ordered cohort reports; cohorts persist legal eligibility turns. Policy changes clear eligibility progress and use the ordinary revision/ledger pipeline.

Conditions contribute satisfaction: needs +25/-300; prosperity (prosperity-5000)/100; environmental habitability shortfall /100 plus -100 if crowded; legal penalty /25; stability (stability-5000)/250. Alignment uses prior satisfaction/integration (divisors 100/200) plus policy/legal/memories. Radicalisation rises +60 below satisfaction 3000 and +20 below stability 2500, with legal/policy/memory pressure; high satisfaction/integration reduces it by 40. Integration is clamped to ±25 per turn. All rates are bounded, deterministic and explained by latest-turn reason records. Memory magnitude scales content effects by 10000 after decay.

Pressure weights discontent/alignment deficit/radicalisation 3:3:4 by population, adds legal strain, food shortage (+500), instability ((10000-stability)/10), then subtracts policy suppression. Escalation thresholds 2500/4000/6000 require 3 sustained turns; recovery below 1750/3250/5250 requires 5. Organized disruption schedules every fourth organized turn for 2 turns, without RNG; economic consequences follow in I1. Policies cannot change ownership. Affinity remains independent of loyalty until real hostility context is implemented.

P1 baseline: turn 26/revision 187, 129 AI commands, 18 encounters/15 checkpoints, 6 fleets/ships, RNG 850d3ff45bd6b85e. Save hash f7feb9be546c813380cab11b151e5c1e1b799453633dd5840dc9cbf292972f29. Strategic production/combat totals match the reference; stability/political reports differ. This is an intermediate checkpoint, not the final milestone baseline.
