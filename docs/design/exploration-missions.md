# Exploration, first contact and procedural missions

Design date: 12 October 2026.
Status: **agreed direction**, **proposed framework**, **open tuning**. Documentation only; this chapter does not claim a mission engine or any new gameplay implementation.

Read [the design index](README.md), [ship design](ship-design.md), [strategic AI](strategic-ai.md), [current architecture](../architecture.md) and applicable milestone/acceptance records before implementation. The legacy Google gameplay document remains the source for unmigrated population, logistics, diplomacy and Science rules. This chapter is authoritative for the exploration/mission decisions recorded here.

## Agreed direction

- Exploration should capture the frontier and distant-expedition character of TOS, TNG and Voyager within the DS9-era campaign. Avoid forced canonical episode reenactments.
- Arriving reveals broad system information; investigating individual bodies takes turns and appropriate sensors, probes, away teams or other equipment.
- Systems can contain varied stars, planets, moons, belts and phenomena; multiple civilizations or governments may inhabit one planet or system.
- First contact depends on both parties' detection capabilities and actions. Locals may initiate contact. A high aggregate technology level alone is not the detection rule.
- Approaches include distant observation, covert cultural study, open diplomacy and coercion/conquest, subject to actual capabilities.
- Non-interference is a policy with consequences, not an absolute action block. Federation presets can adopt a Prime Directive-style policy; other factions have their own policies and incentives. Keep policies editable and content-defined.
- Choices can affect ships, scientific knowledge, resources, diplomacy and population history. Effects depend on affected groups and circumstances.
- Risky missions can damage ships, including through military retaliation. Damage persists into the campaign.
- Limited self-repair and local repair resources can create survival-versus-non-interference dilemmas. The suggested 90% field-repair ceiling is **provisional**, not approved balance.
- Build procedural situations from authored templates populated with real campaign facts, persistent mission state and shared consequence rules.
- Complete finite study chains per civilization; shared or stolen findings reduce remaining knowledge and never duplicate rewards. Standing local planetary research modifiers are separate from finite discoveries.

All schemas, timing policies, formulas, pacing limits, examples and milestone boundaries below are **proposals** unless explicitly labeled otherwise.

## System and planetary exploration

Separate navigation region, star system, astronomical body, political actor and population cohort identities. A navigation region may contain no system; a system may contain multiple stars and many bodies. A body is not necessarily owned by one civilization. Preserve existing IDs and associations through explicit adapters until a versioned schema increment is authorized.

Proposed body data: stable ID, parent/orbital association, body kind, environmental profile, habitats/capacity, resources and exploration sites. Multi-star configurations supply authored or generated environmental phenomena; this framework does not require an orbital physics simulator. Unusual illumination or seasonal cycles must have explicit content consequences rather than arbitrary penalties for every binary system.

Knowledge is civilization-specific:
- Arrival/basic detection: known stars, major detectable bodies and obvious signatures.
- Survey: habitability, resource evidence and detected inhabited sites.
- Study: detailed phenomena, cultural/scientific findings and actionable opportunities.

Adapt the legacy Unknown → Charted → Surveyed → Studied vocabulary to bodies/sites without silently changing existing map knowledge contracts. Distinguish physical existence from discovery, survey confidence and permission to visit. Do not reveal hidden governments, deposits or hazards through mission titles or disabled options.

A survey plan can queue bodies automatically. Equipment affects feasible actions, time, confidence, risk and available choices. Specialized scientific capability is valuable; identical ships do not stack unlimited rewards. Discovery is not colonization, ownership or automatic diplomatic access.

## Mission director and template definitions

The director queries authoritative state and actor-specific knowledge to identify eligible situations. It binds existing entities into a content-defined template; it does not invent resources, civilizations, crises or damage just to force an event.

Proposed template fields:

| Field | Meaning |
| --- | --- |
| ID/version | Stable content identity and compatible retained definition |
| Eligibility | World facts, knowledge requirements, capability and policy conditions |
| Bindings | Required ship/fleet, region, body, resource, governments and affected cohorts |
| Priority/repetition | Selection weight, cooldown, uniqueness and concurrency category |
| Stages | Discovery, investigation, approach, action, aftermath as needed |
| Choices | Preconditions, costs, commitments, duration, risk and visible explanations |
| Outcomes | Typed effects and branches; conditions for deterministic seeded uncertainty |
| Visibility | Who directly observes, can discover or receives reports about an event |
| Continuation | Completion, pause, cancellation, expiry, failure and follow-up rules |
| Text | Presentation strings with verified entity bindings; no authoritative rules in prose |

Re-evaluate candidates after relevant turn phases. Use stable ordering before weighted selection. First filter invalid bindings and unavailable systems; a template requiring unimplemented diplomacy must be fixture-only or disabled, never report a fabricated live effect.

Selection budgets are deterministic work counts, not elapsed-time limits. Mission pacing limits are separate from physical survey throughput: avoid interrupting the player for every scanned rock.

## Persistent mission lifecycle

Proposed lifecycle: Offered → Active → AwaitingChoice → Resolving → Completed, with explicit Paused, Cancelled, Expired and Failed alternatives.

Persist mission ID/template version, owner, bound entities, stage, progress, submitted choices, reservations/commitments, outcome seed or stored draws, visibility/discovery records, applied-effect receipts and repetition state. Reuse existing stable-ID allocation and command ledgers where appropriate; do not introduce random GUIDs or duplicate authoritative inventories.

Choices go through stamped commands with ownership, turn/revision and capability validation. Human and AI use the same path. UI previews are advisory detached projections. An offered mission reserves nothing until a defined acceptance/commitment step.

At execution, revalidate facts. A destroyed ship, departed fleet, changed government, blocked route or exhausted deposit can pause, invalidate or alter a mission according to explicit rules. Do not silently charge again, retarget to another planet or reroll to obtain a valid outcome. Define refunds and consumption by completed work.

A mission must not lock the whole campaign while waiting for one choice. Time-sensitive missions need disclosed deadlines and a defined timeout/default policy. Avoid recursive same-turn mission chains.

## Shared consequence contract

Separate **world fact**, **observer knowledge** and **political interpretation**.

Example: aid reached cohort A is a world fact; government B learns through an observer/report; B interprets exclusive aid as support for a rival according to its interests. Material effects occur even if secret. Remote diplomatic fallout requires lawful discovery. Exposure later can create delayed reactions.

Use a closed, versioned set of typed effects, validated and applied by the owning systems:

| Effect family | Examples | Authority and limits |
| --- | --- | --- |
| Ship/fleet | Hull damage, repair, mission interruption, equipment loss where supported | Existing damage/destruction rules; future subsystem effects require their own contract |
| Physical resources | Extract, transfer, deliver, consume named goods | Real stock, permissions, transport and fabrication; no free supplies |
| Science/Engineering | Insight, technical data, application opportunity, study flags | Existing prerequisites and finite-reward rules; no automatic whole-tech unlock |
| Population | Event memories, civilian casualties, displacement, environmental damage | Target actual cohorts; normal political/economic calculations produce downstream effects |
| Diplomacy | Fact-backed memories, commitments, opinion/trust/influence effects | Affected governments, interests, visibility and policies; no universal reputation meter |
| Access/infrastructure | Negotiated study, docking or supply permission | Explicit acceptance, suitable facilities and treaty duration |
| Mission/history | Progress, completion, new eligible study/follow-up | Stable receipts, cooldown and bounded history |

An effect carries origin mission/stage, stable effect ID, targets, magnitude/type, timing, visibility and reason code. This is a proposed vocabulary, not a claim that these DTOs already exist.

Validate the complete effect batch before applying it. Commit authoritative mission progress, resource changes, rewards, memories and RNG state atomically within the existing transaction model. Failure rolls back the batch. Duplicate submission/reload cannot apply an effect twice. Do not replace the simulation with a second event-sourced state store.

Avoid causal double counting. Aid changes actual food availability and may create a political memory; the normal population system derives satisfaction/stability. Do not also grant a duplicate generic stability bonus for the same mechanism.

Explicit exceptions such as a political shock may exist, but must be separately justified. Memory decay, stability inertia and integration remain owned by population rules. Knowledge rewards depend on work and learning, not simply choosing a virtuous dialogue option.

## Resolution, timing and uncertainty

Proposed resolution:
1. Check eligibility, knowledge, rights, costs and capabilities.
2. Commit the choice and defined reservations.
3. Advance work using actual capability and access.
4. Revalidate conditions at the resolution point.
5. Resolve the physical outcome, with bounded seeded uncertainty where appropriate.
6. Apply the typed effect batch once.
7. Notify legitimate observers and evaluate their reactions.
8. Let normal turn systems calculate downstream changes.
9. Expose an actor-filtered outcome report and eligible follow-ups.

Use fixed-point/integer arithmetic and explicit ordinal ordering. Same state, content, choices and seed produce the same outcome. A dedicated mission RNG stream may be proposed, but changing RNG ownership requires versioning and acceptance; do not casually consume additional campaign draws and change existing baselines.

Risk depends on hazard difficulty, equipment suitability, preparation and approach. Rewards, damage and exposure can coexist; there need not be one binary success roll. State probabilities or qualitative risk honestly based on known evidence. Unknown danger must not be presented as guaranteed safety.

Integrate with the legacy proposed turn order: mission commitments during Planning; advancement/resolution after movement in exploration/first contact; reactions at defined end-of-turn stages. Knowledge earned becomes allocatable in the following Planning phase. The actual implemented turn order must be inspected before integration. Prevent outcomes retroactively changing already-resolved phases.

Military retaliation involving actual forces uses the authoritative hostility/encounter/combat path. Do not invent a second combat resolver or both apply scripted battle damage and combat damage for the same attack. Environmental incidents may apply bounded non-combat damage directly. Define and version the timing of newly initiated hostilities.

## Repair and expedition endurance

Field repair rate and ceiling depend on deployed repair capability, supplies, damage and operating conditions. A ceiling does not reduce an already healthier ship to that percentage. Equipment must consume capacity/power/cost under the ship model; increased self-sufficiency competes with other roles.

Separate obtaining raw resources, fabricating replacements, performing field repairs and using a suitable dock. A pre-warp supplier may provide raw material without possessing starship-repair technology. Access to a technologically incompatible dock does not guarantee full restoration.

The existing legacy design disallows strategic repair for isolated fleets. Autonomous expedition repair is a proposed exception/new capability and must explicitly reconcile that rule before implementation. Do not claim it already works. Final rates, resource tracking granularity and the proposed 90% ceiling remain open. Repair cannot resurrect a destroyed ship or replace paid refitting.

## Worked first-contact template: divided world

Example content, not canon or fixed balance:
- Basic scans find an inhabited world and two communication networks.
- A two-turn remote survey is an illustrative duration. Suitable equipment discovers two rival governments, a crop crisis and its environmental cause.
- Locals may detect the observer or initiate communication.
- Choose distant observation, disguised study, open contact or coercive action when physically possible.
- Open aid negotiations can offer inclusive assistance or an exclusive agreement. Local actors can refuse or counteroffer.
- Actual delivered food changes stocks; recipients gain appropriate memories; rivals evaluate known exclusivity; scientific study yields justified Biotech insight.
- Federation-style non-interference can generate domestic policy reactions. Those reactions require a defined domestic-politics hook and are fixture-only until one exists.
- Assistance may open cooperation. It does not instantly grant trust, membership, planetary control or guaranteed acceptance.

## Worked procedural template: damaged explorer seeking supplies

Eligibility: surviving damaged expedition ship; insufficient relevant onboard repair supply/capability; known reachable resource or potential supplier; meaningful repair alternatives. Distance from friendly repair creates pressure but need not be an absolute exclusion. Disclose known alternatives.

| Local situation | Possible choices | Consequences |
| --- | --- | --- |
| Uninhabited body | Extract, survey first, attempt return, request support if reachable | Time, resources, environmental risk, repair and scientific findings |
| Pre-warp society | Observe, covert extraction, open negotiation, technology barter, coercion, leave | Detection/cultural disruption, policy reactions, commitments and memories |
| Spacefaring civilization | Negotiate purchase/docking, exchange data or service, decline terms, return | Price, facility compatibility, access, relationship effects and obligations |

Options depend on rights, equipment, supplies and acceptance; do not expose every option unconditionally. An unavailable friendly yard must not be secretly teleported into reach.

Resolve resource acquisition and onboard fabrication before repair. Success can restore enough hull to continue while remaining below full readiness. Extraction can succeed but expose the ship; a valuable discovery can accompany damage. Coercion requires actual military capability and can trigger lawful retaliation.

Fulfilling a negotiated service creates trust through the commitment system. Trading technology transfers only the specified permitted knowledge. Repeated visits cannot farm the same finite discovery or political reward.

## Spatial phenomena and black-hole expeditions

Direction discussed 12 October 2026; mission details remain proposed. Use [weekly time and movement](time-movement-mobilisation.md) for the campaign clock and pacing constraints.

- Detect evidence of hazards at a distance when sensor range/quality permits. Give actionable warnings before a deliberate dangerous approach; distinguish known risk from unknown information.
- Finding a phenomenon early must not automatically trigger an unavoidable empire-ending threat. Crystalline entities and Borg-like actors require separate actor/encounter designs rather than being treated as passive black-hole hazards.
- Offer perimeter observation, probe deployment and close investigation, with different equipment, time, exposure, observations and rewards. Record sites so better-equipped expeditions can return later.
- A black hole does not impose extreme time dilation merely because a ship enters its system. Relevant proximity and trajectory govern the effect. Perimeter study can remain relatively unaffected.
- Crew proper time and empire time differ on a close expedition; keep the whole campaign on one weekly empire clock. Time dilation reduces onboard elapsed work per empire interval, not a free research multiplier.
- Proposed mission state can store onboard work progress and empire return estimates using bounded fixed-point parameters. Stronger scientific value comes from unique close observations, not extra subjective working time.
- Communication limits require explicit signal/environmental or fictional subspace conditions; time dilation alone does not imply a complete blackout. Show estimated return windows and communication status, with honest uncertainty.
- The ship can be committed/unavailable for several empire turns while the crew experiences a shorter investigation. Define withdrawal feasibility before commitment and at stage transitions. Never freeze the empire clock.
- Damage, retreat, discoveries and useful data may coexist. Probes and preparation can reduce risk; safe observation should still have worthwhile value.
- Science can reveal an Engineering opportunity or strategic route knowledge; usable equipment/routes still require applicable Engineering, construction and validation.
- Wormhole stability, unknown exits, communication, probe-first scouting and collapse are future template variants. Do not silently promise stable traversal for an unstudied connection.
- The finite reward/mission-order framework below supplies the next black-hole-chain foundation. Specific content, technology requirements, dilation parameters, mission duration and abort rules still need design review.

Scientific grounding: [NASA's black-hole visualization explanation](https://science.nasa.gov/universe/black-holes/supermassive-black-holes/new-nasa-black-hole-visualization-takes-viewers-beyond-the-brink/) distinguishes gravitational/motion-related time dilation and the event horizon. Game parameters and survivable expedition envelopes are abstractions, not a general-relativity simulation.

## Finite study chains, expedition orders and scientific rewards

Decision update: 12 October 2026.

### Agreed direction

- A phenomenon has a finite study chain. Once a civilization completes it, that civilization cannot mine it repeatedly for discoveries.
- Study completion is **per civilization**, with shared or stolen findings reducing what remains to learn. Changing ships, ownership transfers or receiving the same data again cannot reset earned knowledge.
- A chain can contain several decisions that shape the investigation and its reward. Three decision points are an illustrative template, not a required count for all sites.
- Mission preparation, time, equipment and finite consumables influence the likelihood and quality of findings. The phenomenon influences the scientific disciplines that benefit.
- One mission may yield insight in multiple disciplines. It is not restricted to one mutually exclusive Science result.
- Keep a bounded total science reward budget so multiple successful discipline rolls do not accidentally multiply the payout.
- Outcomes can be graded: modest usable data, substantial findings, partial success or loss. A failed substantial-finding check need not erase all routine observations.
- Exploration and laboratory investment should both be viable over a campaign. Compare overall expected value and opportunity cost, not equal points on every mission.
- Planetary geology or stellar conditions can separately supply a standing, discipline-specific research-production modifier. This is distinct from an exhausted one-time discovery reward.

### Proposed mission order and autonomous execution

For the initial black-hole example, the player chooses mission parameters before communications become unavailable:
- Approach: flyby/perimeter, extended observation or close investigation.
- Work/time budget and permitted probe expenditure.
- Risk tolerance, minimum hull/repair reserve and withdrawal thresholds.
- Scientific priorities where the available observations support them.
- Rules for returning, reporting and safe fallback when conditions change.

The captain executes those pre-authorized parameters through deterministic mission rules, not a live language model. Several internal decision stages can resolve autonomously using the chosen policy. A communications blackout must not prompt the empire leader for impossible live instructions or block the campaign. The absence of player input must not imply permission for unbounded risk or extra resource spending.

Other mission templates may request multiple strategic choices while communication remains available. Distinguish an interactive chain from an autonomous chain; a three-stage chain does not necessarily mean three player popups. Amendments require established communication/access and a legal command; define an abort/return window explicitly.

### Finite consumables

Probes are stored physical equipment with limited stock. Deployment consumes actual stock once. Replenishment requires a supported supply, production or fabrication path, time and applicable costs; it is not a free reset between missions. Storage/launcher capacity belongs to the ship-design model. A launch budget cannot exceed loaded stock.

The user also identified photon torpedoes as finite, refillable stores. This is an intended future ammunition/logistics direction, **not** a change to the current aggregate combat resolver, which excludes ammunition. Introduce combat ammunition only in its own scoped, versioned milestone. Keep scientific probes separate from torpedo stocks unless explicit equipment/content defines conversion.

Avoid reintroducing a generic Materials currency. Missions use named resources and consumables where meaningful. Detailed consumable types, fabrication rates and fleet resupply remain proposed/open.

### Proposed reward calculation

Science disciplines remain Biotech, Computing, Construction, Energy, Propulsion and Weapons.

Separate:
1. **Data acquisition/quality:** capability, time, approach, consumables and hazards determine actual useful observations.
2. **Discipline participation:** content-defined, potentially independent eligibility/chance checks determine which fields receive insight.
3. **Point allocation:** distribute a bounded mission reward across participating fields using content weights, observed quality and remaining learnable findings.

Independent chances do not need to sum to 100%; allocation weights for distributing one budget are a different quantity. If no discipline qualifies, use the authored modest-data or no-new-findings outcome; do not reroll until success. Use deterministic integer arithmetic, stable discipline order and explicit rounding/remainder rules.

Illustrative substantial-finding chances discussed: flyby 50%, extended study 75%, probe-assisted study 95%. Illustrative black-hole participation chances: Propulsion 90%, Energy 80%, Construction 30%. These are **provisional examples**, not final success guarantees, fixed universal tiers or approved balances. Equipment, site difficulty and lawful knowledge affect the actual distributions.

Example report: 523 Propulsion insight and 124 Construction insight, total 647, within the resolved budget. The example does not establish a universal 1,000-point Science level cost; actual progression uses scenario/content costs. Grant insight through the normal Science rules, including overflow/level transitions and seeded breakthrough handling where supported. Practical applications still require Engineering.

Separate scientific findings from hazard damage and data recovery. A damaged ship may return useful data; a successful investigation can lose a probe. Define whether data was transmitted, recovered or lost before granting its reward. Unsupported data-recovery systems must be explicit fixtures, not silently assumed live behavior.

### Knowledge identity and deduplication

Proposed tracking key: civilization + stable phenomenon/site + stable finding identity. Store completed stages, claimed findings and acquired shared/stolen knowledge using bounded durable state and existing effect receipts. One civilization cannot run competing missions to claim the same finding twice.

Transferred data carries finding provenance and scope. Receiving a previously claimed finding gives no new discovery payout. Receiving partial data narrows the remaining investigation; mission progress and costs are recalculated under explicit rules, without automatically completing unknown observations or buying trust.

Another civilization may independently study the same accessible site, subject to access and physical conditions. Physical depletion/destruction is global where applicable, while scientific knowledge is civilization-specific. Capturing a ship/site or absorbing a civilization requires explicit knowledge-merge rules; changing IDs or ownership cannot refresh a completed reward.

Paused/failed chains retain earned findings and consumed supplies. Resuming can claim only unresolved eligible findings. Abandoning and restarting must not reroll exhausted stages. Whether a newly unlocked instrument exposes a genuinely new finite chain remains open; newer equipment alone never automatically resets an old one.

### Standing planetary research modifier

A studied geological or stellar feature may reveal a persistent local modifier, such as +10% to local Energy Science production. This number is illustrative; an additive output bonus versus a percentage modifier remains a tuning/schema choice.

The bonus is attached to the actual planet/feature and supported local research output, not to each visiting ship. It is applied once per relevant production calculation. It creates no repeatable mission reward. Control transfers do not duplicate it. Normal facility, access, stability and policy rules still determine usable output.

Do not assume a Science modifier multiplies Engineering, the whole empire's R&D pool or every discipline. The current shared R&D allocation model needs an explicit local-to-discipline projection/adapter before implementation; do not silently alter its semantics. Discovery, facility prerequisites, stacking, caps and loss of the physical feature need authored rules.

### Balance and acceptance additions

Compare labs and expeditions over equal campaign horizons and realistic starts. Include ship/facility construction, upkeep, travel and committed weeks, probes, expected damage/losses, resupply and alternative use of Industry/credits. Credit finite discoveries, route knowledge, Engineering opportunities and exploration-victory progress as additional value. Laboratory output is steady; expedition output is uncertain, weighted by available sites and finite.

Use reproducible scenario/seed suites, evaluate variance as well as means, and ensure neither labs nor expeditions universally dominate. Diminishing novel findings must limit late-game farming without making safe early observation pointless. More investment improves supported prospects; it does not guarantee a breakthrough or proportional unlimited rewards.

Additional acceptance:
- Completed chains cannot restart for the same civilization through ship swapping, reload or ownership changes.
- Independent civilizations retain separate knowledge without respawning depleted physical rewards.
- Shared/stolen duplicates pay out zero; partial transfers reduce only the corresponding remaining findings.
- Multi-field rewards remain within the total budget and deterministic rounding rules.
- Probe spending respects stock and occurs once; resupply follows real support rules.
- Autonomous missions respect orders and cannot demand live choices during blackout.
- Failed/aborted/resumed missions preserve claims, RNG and spent supplies.
- A planetary modifier applies once to its supported local output and is separate from discovery rewards.
- Current combat has no ammunition behavior change until a separate accepted implementation.


## AI and presentation

AI evaluates mission value against its persistent strategic plan, risk, time, resources, policies and lawful knowledge. Difficulty changes bounded evaluation effort, not outcomes or hidden information. Templates must offer decision metadata without letting AI read unrevealed branches.

Unity presents survey queues, missions, choices, durations, known risks, policy tensions and outcome reasons. Distinguish unavailable equipment, unknown information and refused permission. Reports explain direct versus delayed consequences without revealing secret foreign reactions. Do not require a live language model for mission generation or resolution; optional prose generation must remain non-authoritative.

## Proposed implementation stages and future Codex brief

1. **Specification/fixtures:** inspect current main and contracts; identify missing body, repair, diplomacy, mission and reward interfaces. Author the divided-world and repair examples as fixture manifests.
2. **Portable mission foundation:** versioned templates, persistent stages, stamped commands, deterministic eligibility, transaction-safe typed effects, detached projections and save compatibility. Start with supported hull/resource effects and explicit fixture-only unavailable effects.
3. **Survey/repair integration:** body-specific knowledge, capability-driven survey work, resource delivery/fabrication and agreed field-repair rules.
4. **First-contact integration:** independent local political actors, detection, clause-based negotiation, commitments, memories and visibility-aware reactions.
5. **Content/AI/pacing:** bounded template selection, lawful AI choices, variation and repeated-campaign playtests.

Future prompt must state an exact baseline commit, branch/worktree, ownership, included effects, explicitly excluded systems, save/content/rules version changes and test commands. Inspect actual contracts before selecting class names. Coordinate with ongoing ship-design work; do not edit its shared contracts independently or overwrite local agents' branches.

Initial suggested task: implement one portable repair mission end to end on an isolated branch, with fixture adapters for unimplemented local civilizations. No live diplomacy, full planetary schema migration, cinematic renderer or strategic AI rewrite in that increment. Update implementation/acceptance docs separately from this design.

## Acceptance requirements

- Eligibility uses real bound facts; projections do not leak hidden actors/resources/branches.
- Human and AI choices obey identical prerequisites, costs and command rules.
- Identical inputs and seed resolve identically under reordered authored collections.
- Save/reload at offered, committed, in-progress and resolved stages preserves outcomes.
- Repeated commands/reload award no duplicate resources, repairs, research or memories.
- Invalid batches roll back state, mission progress, reservations, allocation and RNG.
- Lost participants, changed access, depleted resources and departed ships follow defined policies.
- Resources/facility access alone do not fabricate unsupported repairs.
- Damage persists; repair caps do not lower existing hull; destroyed ships stay destroyed.
- Secret material effects occur normally; remote reactions wait for legitimate discovery.
- Population/diplomacy reactions target specific groups and avoid duplicate stability effects.
- Actual military retaliation uses existing combat once.
- Cooldowns and finite rewards prevent repeat farming; deadlines do not freeze the campaign.
- Existing baseline behavior remains unchanged when no missions are enabled.
- UI/runtime checks are reported as executed or not run; static checks are not Unity acceptance.

## Open decisions

Exact chain lengths, finding-transfer/absorption rules, new-instrument follow-ups, standing modifier formula/stacking, data recovery and consumable logistics; body/actor ownership granularity; field repair rates/ceilings and supply abstraction; domestic policy-response model; exact risk curves and scientific rewards; mission frequency and concurrency; timeout/default choices; interception/retaliation timing; cross-civilization competition for one site; how much outcome preview is revealed.

Resolve these in scoped implementation/design increments. Do not treat proposed example numbers as final balance.
