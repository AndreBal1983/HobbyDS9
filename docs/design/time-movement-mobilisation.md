# Campaign time, movement and mobilisation

Decision date: 12 October 2026.
Status: **agreed direction**, **proposed detailed rules**, **open balance**.
Documentation only. This chapter does not change current gameplay, saves, navigation or production.

Read [the design index](README.md), [ship design](ship-design.md), [exploration missions](exploration-missions.md), [strategic AI](strategic-ai.md), [architecture](../architecture.md) and current milestone/acceptance records. These decisions supersede conflicting legacy design assumptions on the topics explicitly covered; implementation contracts remain unchanged until a scoped increment is accepted.

## Agreed decisions

- One strategic turn represents **one week** of empire time.
- Weekly turns are the common clock for travel, missions, Industry, economy, political action and diplomacy. Orders can be immediate; their physical or social consequences have explicit resolution timing. Short activities may resolve within a turn.
- Ships have a strategic movement rating in ordinary sector crossings per week. This is **not** a warp factor. DS9-era starts use mature propulsion, not warp-one technology.
- Fleets should visibly progress through intermediate navigation regions. Do not leave a moving fleet displayed as stationary at its origin for the whole journey.
- Irregular, Voronoi-like navigation regions provide travel geography: sparse open space has larger regions/fewer crossings; dense or disturbed space has smaller regions/more crossings.
- Credits can expedite eligible work and mobilise capacity. Time and money are partially interchangeable, subject to real capabilities, resources and acceptance.
- Ship-production buyout is an intended emergency response option.
- Outposts, starbases and scanning arrays are investments in warning and knowledge. Engineering improves their capabilities; construction and upkeep compete with other spending.
- Balance reconnaissance warning time, enemy travel time, reinforcement time and production time together.
- Maintain standing orders, queues and useful alerts so many weekly turns create overlapping decisions rather than repetitive administration.

Speed bands, route costs, buyout formulas, completion timing, physical scale and activity durations below remain **proposals**. Five is neither an agreed speed ceiling nor an approved normal speed.

## Clock and pacing

Empire time advances seven days per successful strategic turn. A staged AwaitingCombat checkpoint does not advance the calendar again when combat is finalized or reloaded. Persist or derive calendar dates from a declared scenario epoch and turn convention; do not use wall-clock time.

A ten-year campaign contains roughly 520 weekly turns. Exact calendar-year boundaries require date arithmetic; do not assume every year has exactly 52 weeks.

Scale recurring economy, upkeep, population growth, repair, research, memory decay, treaties and deadlines consistently to weekly time. Annual growth must be compounded/converted appropriately; do not apply an annual percentage every week. Existing per-turn values require explicit rebalance and rules/version acceptance.

Long projects overlap with exploration, Engineering, diplomacy and fleet activity. Do not inflate mission frequency merely to fill quiet turns. Provide summaries, completion forecasts and exception alerts. Optional automatic advancement must stop at disclosed decision/threat gates and preserve normal deterministic weekly processing.

Production duration depends on the design and actual yard/Industry capacity. The current short demonstration builds are not the balancing anchor. Intelligence-network establishment and execution of an operation are separate durations. Minimum victory turns, coalition hold periods and game-speed settings must be reviewed against the new calendar; do not silently reinterpret legacy defaults.

## Movement and sector geometry

Proposed normal rule: one adjacent sector crossing costs one movement point. Regions may contain systems, hazards or empty space. Geometry represents traversal conditions, not equal physical distances.

A route contains intermediate regions. A fleet with movement 3 crosses three ordinary edges each weekly Movement phase, subject to legal access and encounter rules. Seven crossings take three turns: 3 + 3 + 1. Display actual region/progress and ETA through authoritative projections.

| Illustrative movement rating | Weeks for seven ordinary crossings |
| --- | --- |
| 1 | 7 |
| 2 | 4 |
| 3 | 3 |
| 4 | 2 |
| 5 | 2 |
| 7 | 1 |

These are arithmetic examples, not approved class statistics. Two ratings can give the same ETA on one route but differ on other routes or in subsequent travel.

Ordinary starting cruisers around rating 3 are a proposal. Ratings 1–5 are a possible initial range; the user questioned whether 5 is too high. Retain tuning freedom and avoid an arbitrary permanent cap. A one-week courier on this route requires rating at least 7 under the normal rule, a shorter route, or an explicitly designed special ability. Do not claim rating 5 achieves it.

Ship movement derives from installed propulsion, supported power and other approved performance rules, not hull name alone. Fleet aggregation, damage penalties and emergency speed remain open. Distinguish sustained strategic cruise from maximum/emergency warp presentation.

Larger open-space regions mean fewer crossings across a physical span. Do not also charge their size as an additional distance penalty. Disturbed geography represented by extra crossings should not receive the same hazard slowdown again. Separate hazards may still cause detection interference, damage or exceptional traversal cost if explicitly justified.

**Compatibility gap:** existing weighted-navigation rules/content support edge costs and travel progress. Adopting predominantly unit-cost intermediate sectors is a future navigation/content decision, not a documentation-only removal of those contracts. Inspect current geography implementation before migration. Weighted routes can represent explicit exceptions; their partial-edge location, interception and ETA semantics must be defined. Never teleport a fleet, change combat locations or invent intermediate nodes solely in Unity.

Crossing several regions within a turn also needs authoritative contact/interception rules. Do not assume fleets can pass through defended regions without encounter simply because their final positions differ. Enemy paths must not leak through route previews.

## Reference anchor and chosen game abstraction

Source consulted directly: [Star Trek: Deep Space Nine Technical Manual](https://engineering.thetafleet.net/Journals/DS9/Franchise%20-%20Star%20Trek%20Deep%20Space%20Nine%20Technical%20Manual%20.pdf), printed pages 3 and 17.

Page 3 places the Bajoran and Cardassian systems approximately 50 light-years from the core Federation worlds, and 5.25 light-years apart. It does not establish an exact Earth–Bajor distance.
Page 17 gives specialised courier travel over 50.3 light-years from DS9 to the UFP inner perimeter in six days at warp 9.92. This is not routine cruiser travel or an exact Earth endpoint.

Episode dialogue/reference estimates do not provide one consistent normal Sol–DS9 journey. Do not label popular ten/eleven-day calculations as an explicit canonical travel time.

**Proposed game anchor:** seven ordinary Sol–Bajor/DS9 crossings, movement 3, three weekly turns. Three weeks is our pacing choice, not a verified canonical value. Faster classes can take two weeks. Treat DS9's local station/Bajor travel separately if exposed; do not imply the station is the planet. Establish the map's physical scale only as needed, using broad consistency rather than a universal light-years-per-sector constant.

## Expedited work and construction buyout

Credits fund additional shifts, contractors, procurement, transport, housing or security effort where the relevant system supports it. Every eligible project defines normal work, available acceleration, incremental cost, constraints and earliest completion.

General expediting has diminishing returns/capacity limits. A completed ship buyout may be a special, explicitly supported full acceleration option; do not make every social or scientific process instantly purchasable.

Proposed buyout behavior:
- During Planning, select an existing ship order and review its remaining work and extra cost.
- Require ownership, eligible yard/design, affordable treasury and required delivered physical inputs.
- Price the remaining work with an urgency premium. A 30%-complete ship costs more to rush than an otherwise identical 90%-complete ship.
- Reserve/pay according to the authoritative command transaction; existing payments are not charged again.
- Complete at the next eligible Production phase in the same resolving turn, before Movement under the existing phase order. This timing is proposed, not implemented.
- Define queue eligibility, limited rush capacity, cancellation/refunds and newly completed ship orders explicitly. Do not infer that a new ship can receive an arbitrary route after Planning has ended.
- Both human and AI use identical costs, limits and knowledge. AI weighs the emergency against its other commitments.

Credits cannot substitute for a missing prerequisite, nonexistent yard, inaccessible strategic resource or blocked delivery. Resolve what a blockade prevents under the actual supply rules; already stocked inputs are distinct from required imports.

## Political, social and diplomatic acceleration

| Spending | Faster direct outcome | Lasting limits/consequences |
| --- | --- | --- |
| Security mobilisation | More enforcement capacity, earlier suppression of organised disruption | Capacity/access required; repression can damage satisfaction/alignment and create radicalisation |
| Habitats and essential services | Earlier usable capacity, less overcrowding, better reception of refugees/migrants | Physical construction and transport; deep integration remains gradual |
| Community programmes | Greater support capacity and opportunity for cooperation | Trust/alignment respond to history and fulfilled commitments; no instant loyalty purchase |
| Diplomatic effort/aid | Better-resourced mission or offer | Other governments may refuse; money does not guarantee treaty acceptance |
| Science/Engineering institutions | More supported R&D or project throughput | Preserve Science/Engineering allocation, uncertainty and prerequisites; no guaranteed purchased discovery |

Hired security/mercenaries are an illustrative future content option, not an existing instant-spawn command. Policies must target actual actors/cohorts and use normal memories/economic rules without duplicate stability bonuses.

The common design pattern is expedited **means**, not guaranteed desired **outcomes**. No universal credits-to-trust or credits-to-research conversion is implied.

## Sensor infrastructure and response windows

Proposed roles: outposts extend frontier coverage; scanning arrays improve range/detail; starbases combine surveillance, defence, repair and supply. Capabilities are content-defined modules and engineered versions rather than free faction-name exceptions.

Keep detection distinct from identification and assessment, consistent with the legacy Unknown/Detected/Identified/Assessed vocabulary. A detected fleet need not have known strength, intent or destination. Reports age and coverage can be disrupted.

A monitored region with no detected threat is different from an unobserved or stale region. Even current coverage does not prove absence of stealthy contacts. UI must show coverage quality, uncertainty and last observation rather than promise omniscience.

Sensor investment can buy time to scout, reinforce, refit, buy out production or negotiate. A quiet border can still provide confidence within sensor limits. Lack of investment raises surprise risk without guaranteeing defeat; reserves, allies, geography and local defences remain alternatives.

Use observed speed/path uncertainty to estimate threat arrival windows. Do not expose an enemy's hidden orders. Enemy movement can change after detection. Measure actual opportunities to issue a response command under the turn sequence, not just map radius divided by speed.

## Implementation guidance and acceptance

Implement separately scoped increments for calendar/pacing, navigation presentation/region topology, buyout and sensors. Inspect current main and ongoing agent branches before changing contracts. Update save/content/rules identities and deterministic baselines only with an explained accepted gameplay change. Unity renders detached facts; simulation owns costs, time, movement, effects and human/AI legality.

Acceptance should cover:
- One successful turn advances one week; checkpoint/reload/finalization never advances twice.
- Seven unit-cost crossings at movement 3 produce intermediate positions and turn-three arrival.
- Legal exceptional weighted edges retain explicit progress and ETA semantics.
- No geometry plus cost double slowdown for the same reason.
- Multi-region traversal follows accepted encounter/interception rules.
- Buyout payment/completion occurs once; invalid resources/rights leave state unchanged.
- Rush pricing decreases with remaining work for comparable designs; payments/refunds cannot create credits.
- Identical acceleration rules for human/AI; no guaranteed treaty, loyalty or breakthrough purchase.
- Coverage projections and threat forecasts reveal only lawful observations.
- Weekly economic/political rates and long campaign pacing remain coherent.

## Open balance and decisions

Normal speed bands and whether 5 is excessive; courier handling; unit-cost versus exceptional weighted links; within-turn interception; fleet speed aggregation; physical map scale; construction durations; buyout premium/capacity/queue rules; supported social expediting; calendar epoch; automatic advancement; campaign length and victory timing. Resolve through narrow design review and playtests rather than hard-coding illustrative numbers.
