# Population politics and strategic geography v1

This milestone builds on accepted main 90b324a; it does not replace the population/economy foundation. All mutations remain staged; the client receives detached projections. Implementation proceeds through green P1, G1 and I1 checkpoints. Main remains unchanged until local Unity acceptance.

## Political rules

World governance is a content definition referenced by durable political state on each inhabited settlement. Four profiles trade inclusion/autonomy against immediate security/extraction. Profiles specify bounded per-turn cohort adjustments, economic/logistics multipliers, suppression, refugee acceptance and conditional legal transitions. Only stamped commands can change policy; derived ratings/resistance have no public setters. Species is not an input to political loyalty or pressure.

Political updates follow local food consumption and memory decay. Each cohort receives ordered reason components from needs, prosperity, environmental pressure, legal treatment, policy, bounded integration/alignment context and active content-mapped memories. Changes are clamped to 0..10000; integration moves much more slowly than satisfaction. Affinity alone produces no penalty: external conflict effects await a real diplomacy/war model rather than treating every foreign culture as permanently at war. Conditional status transitions require sustained eligible turns and reset progress when conditions cease. Historical memory source IDs may outlive entities.

Population-weighted pressure combines discontent, low alignment, radicalisation and legal-status strain, with food/instability context and policy suppression. Resistance uses sustained pressure with distinct escalation/recovery thresholds and timers. Quiet/Tense/Unrest/OrganizedResistance never transfer ownership. Organized resistance causes scheduled, finite-duration efficiency disruption; no RNG or arbitrary infrastructure deletion is used. Projections retain ordered latest-turn rating causes and transition/pressure facts. Full rebellion, policing, occupation/conquest and espionage are deferred.

## Navigation and encounter rules

The authoritative navigation graph stores independent node IDs, optional system associations, explicit edge IDs/base costs and phenomena. Runtime modifiers alter effective cost/budget without changing topology. Knowledge is per civilization and gates both route search and submission; hidden edges never appear in previews. Weighted route search uses Dijkstra, ordinal IDs for deterministic ties, positive effective costs and checked integer totals.

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

### G1 checkpoint

Save 12 / content 8 introduce independent `NavigationRegions` (optional system association), identified undirected weighted `NavigationConnections`, authored phenomena, persisted civilization knowledge and scheduled traversal effects. Fleet/combat/logistics location and route fields now explicitly refer to navigation nodes. The earlier `GalaxyGraph` BFS helper remains available for historical foundation queries; movement, logistics, retreats and AI use the new weighted graph.

Dijkstra settles minimum cost then ordinal node ID; first discovery wins equal-cost alternatives. Base costs are positive; unique endpoint phenomena and active node/connection cost effects add to them. Unknown regions/connections are excluded from player/AI routing and application map/preview. Slowest ship sets the per-turn budget (ship ID settles ties); origin hazards/inhibitors subtract from it, floored at one. The budget is fixed for that movement turn. Traversal locks an edge's effective cost on entry and persists paid progress until arrival; subsequent cost changes cannot erase progress. Route replacement during traversal is rejected.

Movement advances fleets in ordinal order at synchronous cost milestones (arrival or budget exhaustion), not frame ticks. Opposing stationed fleets freeze at contested regions, including empty regions and initial co-location; they cannot depart/pass through an unresolved encounter. In-transit fleets neither participate in node combat nor blockade node logistics. Existing combat resolution algorithms and balance remain unchanged (`combat-resolver.phased` v1). Result locations/retreat targets are navigation IDs.

ETA uses the same budget and locked-edge rules, projects already scheduled modifier starts/expiry, and jumps over whole turns rather than iterating cost units. It assumes no future orders, ship changes or combat delays. Application exposes detached per-edge base/natural/artificial costs and fleet budget/progress/remaining cost/ETA. Physical logistics choose the knowledge-filtered weighted route; endpoint/edge freight capacities remain separate from fleet budgets. Natural logistics factors and hostile interference reduce route integrity.

Intermediate G1 scenario retains unit costs and budgets for checkpoint isolation. Its canonical/reverse/reload/reordered baseline is recorded in the checkpoint commit; I1 will author varied costs, hazards and budgets and update the baseline deliberately.

## I1 integrated candidate

All three stages are implemented on `codex/population-politics-strategic-geography-v1`. P1 is `4f671b3`; G1 is `6207c8dc7e083bcc0d1273903e8dabf597c77fa8`. I1 remains subject to local Unity acceptance, and main remains unchanged.

Final compatibility: **save 12**, **content 8**, **rules `population-politics-geography-v1`**, RNG `splitmix64-rejection-v1` v1 and combat resolver `combat-resolver.phased` v1. The accepted pre-milestone schema was 10/6. The intermediate P1 schema was 11/7. Old saves/content and mismatched package hashes are rejected before replacing a live session; this milestone provides no automatic save-schema conversion. This is separate from the implemented automatic population migration between worlds.

### Policy and memory data

All ratings/factors use basis points (10000 = 100%). Governance effects are authored content, not faction rules. Integration change is globally bounded to ±25 per cohort per turn; other ratings are bounded to 0..10000. Policy changes and cohort arrival at another world reset sustained legal eligibility. Status transitions require the profile's explicit thresholds and consecutive eligible turns; species never supplies eligibility.

| Profile | Satisfaction | Integration | Alignment | Radicalisation | Credits | Industry/extraction | R&D | Logistics | Suppression | Refugees |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---|
| Civic Integration | 10 | 10 | 20 | -20 | 10000 | 10000 | 10500 | 10000 | 0 | True |
| Local Autonomy | 35 | 5 | 5 | -10 | 8500 | 9500 | 11000 | 10000 | 0 | True |
| Security Administration | -50 | -5 | -20 | 30 | 10000 | 10500 | 9000 | 11000 | 1500 | False |
| Extractive Administration | -100 | -15 | -60 | 60 | 12000 | 11000 | 7500 | 9000 | 500 | False |

Memory effects are scaled by current magnitude/10000 after decay. Expired/zero memories are removed; historical source IDs need only remain canonical, not point to a still-living entity. Unknown historical memory kinds are preserved but have no mapped effect.

| Memory kind | Satisfaction | Integration | Alignment | Radicalisation |
|---|---:|---:|---:|---:|
| accepted-as-refugees | 100 | 10 | 100 | -100 |
| liberated | 150 | 10 | 200 | -100 |
| homeland-invaded | -200 | -5 | -150 | 150 |
| homeland-destroyed | -300 | -10 | -200 | 250 |
| protected-minority | 100 | 10 | 150 | -100 |
| discriminated-against | -150 | -10 | -200 | 150 |
| repressed | -150 | -10 | -150 | 150 |
| interned | -200 | -15 | -200 | 200 |
| prosperity-boom | 100 | 5 | 75 | -50 |
| unemployment | -100 | -5 | -100 | 100 |
| war-with-homeland | -150 | -5 | -150 | 150 |
| civilian-casualties | -250 | -10 | -200 | 250 |
| community-investment | 100 | 10 | 125 | -100 |

### Resistance and economy

Pressure, sustained thresholds and disruption scheduling are described in P1 above. Quiet/Tense apply no additional output penalty. Unrest multiplies credits by 9500, industry/extraction by 9000, research by 9500 and logistics by 8000. OrganizedResistance uses 9000/7500/9000/6500 respectively. Active disruption adds 9500/5000/8000/4000, with extraction following industry. Policy × resistance × disruption factors are applied sequentially using integer floor division; Food production and population are not conjured or deleted by these modifiers.

Extraction and physical delivery use political state at the start of Economy. After food consumption, cohort/memory updates and resistance scheduling, Research/Production use updated projections. Newly scheduled disruption therefore affects subsequent production immediately and extraction/delivery on the next economy pass. Duration decreases once per economy update. Checkpoint reloads never rerun these phases. Refugee acceptance requires both the existing world flag and policy permission. Stability remains a separate population-weighted target with ±250 per-turn adjustment; resistance does not directly set Stability or ownership.

### Geography, AI and client boundary

The scenario has explicit `nav.*` IDs distinct from `system.*` IDs. Sol–Bajor costs 7; Cardassia–Badlands and Badlands–Bajor each have base cost 2 plus plasma-storm cost 2; Bajor–wormhole costs 1; the wormhole–Gamma Exit connection costs 1 and is explicitly tagged Wormhole; Gamma Exit–Founders costs 4. Badlands has no associated star and reduces logistics integrity to 8000. Scout movement budget is 3 and advanced design budget is 4. The generic scheduled effect boundary can independently increase traversal cost (scatterer) or reduce a local movement budget (inhibitor), tested without adding device construction/technology.

Dijkstra and synchronous milestone movement follow G1's rules. ETA projects known scheduled effect changes and respects locked partial-edge cost. During AwaitingCombat, travel estimates begin at the next strategic movement turn because current movement already committed. Delays caused by combat, later orders or changed fleet composition remain conditional. Aggregate cost overflow is rejected in route search/submission; phase arithmetic overflow rolls back the transaction.

AI targets the nearest reachable foreign/neutral associated system by weighted cost, then prefers foreign ownership among equal-cost targets. Actual tied score/cost candidates are ordered by node ID; only these consume RNG, and only an accepted route commits the draw. AI follows the same knowledge, route and budget rules. Political policy selection is species-neutral: pressure ≥6000 prioritizes suppression; ties/ordinary conditions maximize satisfaction + 2×integration + alignment − 2×radicalisation, then ordinal policy ID. It submits ordinary stamped policy commands through the shared handler. Human civilizations remain untouched. Research/production intent is retained.

Freight uses knowledge-filtered weighted origin-node → route → destination-node paths. Node occupation blocks delivery; neighboring hostile fleets halve integrity; natural logistics modifiers further cap integrity. In-transit fleets do not occupy nodes. Endpoint/shared-edge freight capacity remains independent of ship budgets. Migration uses weighted distance and preserves physical origin/destination cohorts and history; arrival resets legal eligibility for the new world's conditions.

Application captures campaign, knowledge-filtered map and fleet travel projections under one Simulation lock. Snapshots and every nested collection are detached/read-only. Policy availability exposes stable disabled reasons. Unity displays politics in the selected world's panel: policy control/trade-offs, pressure/resistance, effective penalties, expandable cohort ratings/affinities/memories/reasons and inclusion thresholds. Fleet panels show budget, limiting ship, remaining cost, progress and authoritative ETA; route previews show per-edge base/natural/artificial costs. Empty regions have explicit labels and no inferred star identity. Hazard links are orange, wormholes purple, previews highlighted. In-transit markers show paid/total cost at their last completed node. Responsive flex/sidebar/scroll behavior is retained.

### Deterministic acceptance baseline

Cloud verification passes locked restore, Release build with zero warnings/errors, **742 .NET tests**, portable Unity preparation and metadata/reference/copy checks. There are **24 authored Unity EditMode cases**, not executed in this cloud environment. The acceptance tests compare continuous/repeated runs, canonical/reverse submissions, reloads at pending/partially buffered/fully buffered checkpoints, and reordered content/state including navigation, knowledge, policies, memories and cohorts.

Canonical/reverse/repeated 25-turn final save SHA256:

```text
10009857d0396dea4bd0a81001cadfb6b5ebfad2c8772223d8288bd70d3559e4
```

| Metric | Final value |
|---|---:|
| Turn / revision | 26 / 187 |
| Accepted AI commands | 133 |
| Combat checkpoints / encounters | 12 / 17 |
| Combat phases / shots / hits | 43 / 98 / 68 |
| Shield / hull damage | 126 / 116 |
| Ships/fleets destroyed | 10 / 10 |
| Surviving ships / fleets | 9 / 9 |
| Production completions / queued | 16 / 2 |
| Research unlocks | 2 |
| Next entity sequence | 97 |
| Births / deaths / migrated / refugees | 501 / 50 / 31 / 0 |
| Food delivered | 13 |
| Dilithium extracted / delivered / consumed | 275 / 5 / 32 |
| Traversal cost paid / edges completed | 189 / 111 |
| Fleet-turns with partial traversal | 26 |
| Quiet / Tense / Unrest / Organized worlds | 4 / 1 / 0 / 0 |
| Quiet / Tense world-turns | 108 / 17 |
| Political disruptions | 0 |
| Campaign RNG | `d8919406d6e01cf1` |

Cardassians and Dominion each have 1125 people of their initial species and 110 credits. Federation has 1326 people (877 Human, 348 Vulcan, 101 Andorian) and 231 credits. Unowned Bajor has 1125 people. Stability: Earth 9972 (target 9969), Mars 5068, Cardassia 9633, Founders 9450, Bajor 9825. Compared with the accepted pre-milestone baseline, weighted travel and stopping at contested regions change combat timing and RNG draws, while policy factors/logistics change extraction and migration. Combat shot/hit/damage rules are unchanged. The idle opening remains playable rather than forcing OrganizedResistance; explicit high-pressure tests exercise inertia and disruption consequences without species-specific triggers.

## Local Unity acceptance

Close Unity before pulling the candidate branch. With licensed Unity **6000.6.5f1**, Windows Build Support **IL2CPP**, Visual Studio Desktop development with C++ and a compatible Windows SDK, run from repository root:

```powershell
git fetch origin
git switch codex/population-politics-strategic-geography-v1
git pull --ff-only origin codex/population-politics-strategic-geography-v1
dotnet restore HobbyDS9.slnx --locked-mode
dotnet build HobbyDS9.slnx -c Release --no-restore
dotnet test HobbyDS9.slnx -c Release --no-build --no-restore
dotnet msbuild scripts/PrepareUnity.proj -t:EditMode "-p:UnityEditor=C:/Program Files/Unity/Hub/Editor/6000.6.5f1/Editor/Unity.exe"
dotnet msbuild scripts/PrepareUnity.proj -t:Windows "-p:UnityEditor=C:/Program Files/Unity/Hub/Editor/6000.6.5f1/Editor/Unity.exe"
```

Require zero Editor compilation errors and **24/24** EditMode passes. `Windows` reruns EditMode before the x64 IL2CPP build. Results/logs/player are ignored under `artifacts/unity`; launch `artifacts/unity/Windows/HobbyDS9.exe`.

1. Inspect Earth/Bajor/Mars: policy, Stability versus target, pressure/resistance, cohort reasons/memories/affinities and inclusion thresholds. Change an owned-world policy, advance, and confirm authoritative ratings/effective outputs change. Mars under extractive policy is an optional sustained-treatment resistance demonstration; no species automatically rebels.
2. Preview Federation Sol–Bajor: cost 7, budget 3, ETA 3. Confirm route, advance one turn, and inspect 3/7 paid progress. Save/load and verify exact progress and policy restoration. Route replacement is disabled during partial travel.
3. Inspect empty Badlands, orange hazard links and the purple wormhole. Preview cheap multi-edge travel and confirm remaining budget can cross multiple regions. AI obeys the same rules. No navigation string is inferred to be a star system.
4. Confirm Food/Dilithium receipts still show physical routes and effects; production (including fleet spawning), research and save slots remain operational.
5. Reach AwaitingCombat, resolve/buffer all encounters, explicitly finalize, and inspect the next Planning turn. No movement/economy is replayed during combat.
6. Check selection, pan/zoom/Fit Map and all panels at 1920×1080, 2560×1440, 3440×1440 and 5120×1440.

Unity compilation, these EditMode cases and Windows IL2CPP execution remain unverified until local acceptance is supplied. Do not merge this milestone to main before acceptance. Diplomacy/hostility context, invasion, espionage, ownership-changing rebellion, procedural Voronoi geometry, full exploration/devices and final art remain deferred.


### Production dock acceptance follow-up

Production management now lives in an independent left-side overview, toggled by
**Production** in the strategic header. The galaxy remains in the center and all
politics/population/environment/infrastructure/resource/delivery inspection stays
in the right panel. Human-owned worlds use ordinal colony-ID order, regardless of
map selection; queue/design facts and availability come from Application snapshots.
Enqueue and confirmed Cancel retain normal observed turn/revision stamps. See the
[client layout checklist](../client/HobbyDS9.Unity/README.md#production-overview-layout-current-candidate)
for all four target resolutions. The three new EditMode cases cover owned-world
rendering without mutation, real command/stale-revision handling and independence
from map selection. This presentation-only follow-up changes no versions or
canonical/reverse/reload/reordered deterministic baselines.


### Combat-control/reporting acceptance follow-up

The candidate now limits editable encounter plans to the persisted human player's
fleets. Application composes those overrides with the unchanged resolver-v1
non-player defaults and resolves the frozen pending request. Unknown, duplicated
or foreign/AI fleet overrides are rejected without mutation. Current multi-party
combat remains explicitly free-for-all between different civilizations, without
alliances or implicit sides.

Buffered reports project persisted requests/results and ordered events into
immutable fleet/ship hull/disposition summaries and expandable Long/Medium/Close
phase traces. Reports survive checkpoint reload and remain visible while other
encounters are pending, until explicit finalization clears the batch. Versions
and the canonical/reverse/reload/reordered baseline above are unchanged. The
[combat acceptance checklist](../client/HobbyDS9.Unity/README.md#combat-control-and-battle-reports-current-candidate)
requires 24/24 local EditMode passes and a Windows x64 IL2CPP build/launch.
