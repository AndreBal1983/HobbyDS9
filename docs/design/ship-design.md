# Ship design

Original design: 12 October 2026; Engineering branching, diminishing returns and default refit decisions added 13 November 2026. Ships use shared internal capacity with mounting and compatibility limits, and Engineering develops hull/component versions. Later sections explicitly mark agreed decisions and superseded proposals. Remaining detailed rules and numerical examples are proposed implementation/tuning, not shipped behavior. Current contracts are distinguished in the linked implementation records and [Engineering model](engineering-tree-model-01.md).

The player designs named ship classes, not every individual vessel. A class combines a hull version, component versions, quantities and operating priorities. Production builds that class; refits modernize eligible ships. Ship design should reward coherent trade-offs without becoming a spatial puzzle or real-time power-management simulator.

This section replaces the earlier Chapter 7 category-slot and continuous-power-only design rule. Chapter 3's architecture, scientific generations, maturity and diminishing returns remain in force. Starship Creator is a conceptual inspiration for interdependent systems, not a claim that its exact mechanics or assets are reproduced.

## Hull families and engineered versions

A hull family defines the physical architecture and visual identity: scout, escort, cruiser, transport or another content-defined family. A hull version is an engineered realization of that family. A ship-class blueprint is the chosen fit within one hull version. An individual ship records the actual version and equipment it was built or refitted with.

Each hull version specifies usable internal capacity, base hull strength, structural/load limits where relevant, reactor and propulsion compatibility envelopes, equipment mount groups and applicable coverage, power-distribution limit, minimum operating crew, construction complexity, shipyard requirements, cost and maintenance. Capacities represent abstract usable space, not literal cubic metres. Mass is a separate engineering constraint only if it adds a meaningful propulsion/structural decision; do not treat Mass and Space as interchangeable.

Mounts are installation constraints, not extra free capacity. External equipment packages, including weapons or sensor arrays, still consume internal capacity where defined. Hulls may define mount type, size rating, count and firing coverage. A larger mount accepts a smaller compatible weapon, but compatibility does not imply identical performance. A package can represent several banks or launchers; it declares the mounts it consumes. Count mounts once, at package level.

Earlier MVP mount categories were energy-weapon banks and torpedo launchers, with propulsion and reactor envelopes. The updated intended design uses configurable equipment mount groups with data-defined compatibility, while dedicated launcher-only or specialized mounts are still allowed. The player allocates packages by broad coverage groups if needed; individual emitter placement is not required. Weapon ports and hardpoints are not duplicated budgets. Implementation scope/phasing remains open.

## Modular equipment mounts and hull specialization — agreed direction

Design agreement, 12 October 2026: **hull mount groups are generally configurable within real engineering constraints**, rather than being permanently tied to one exact weapon type. A hull group defines position or broad coverage, size/load envelope, quantity of package installations, allowed equipment families and any structural or interface requirements. Allowed families can include Beam/Array, Cannon/Pulse, torpedo launcher, external sensor/scanning array, communications/relay hardware or relevant support equipment. Some locations remain deliberately specialized, such as torpedo-only launchers or tiny forward cannon mounts. Not all hulls permit every installation.

A capital ship's compatible forward group may accept either beam or cannon hardware; a lateral group might accept a broad array or a sensor panel, and a small fighter-like attack craft might allow light forward cannons only. Same hull family can yield distinct dedicated combat, scientific, exploration or supply variants. **Freedom comes from compatibility, not unconstrained interchangeable slots.** All packages still consume their specified internal capacity, power, crew/computing, mount capacity and installation cost. Sensors installed externally improve scanning/detection but do not replace the separate need for onboard laboratories, scientific computers, probes or crew for demanding missions. A mobile factory similarly needs internal fabrication capacity beyond any external cargo/handling fittings.

Engineering investments in a hull family therefore carry value across multiple future ship roles: improvements to usable capacity, fabrication economy, power routing or allowable modular installations can benefit several derivative class blueprints. A basic refinement cannot silently add incompatible mount families or turn a small attack craft into a capital ship; major architecture changes require an explicit revised hull version and compatible refit/new-build rules. Ships cannot swap combat weapons for scanning arrays in the middle of an encounter.

The three agreed universal combat weapon classes are Beam/Array, Cannon/Pulse and Torpedo; see [tactical combat](tactical-combat.md). Torpedo magazines are finite physical ammunition stored in installed ship capacity, depleted by firing and refilled via actual supply; beam and cannon packages rely on valid powered operating profiles rather than consumable magazine stocks. Exact mount ratings, sensor package types, magazine storage cost and resupply speed remain to be tuned.

### Hull armor as an engineered defensive characteristic

**Agreed combat-design direction, 12 October 2026:** hulls have a compiled **armor rating** that reduces incoming hull-bound damage after the impacted directional shield sector has been exhausted. Armor is **not** another ablative health pool or four separate directional armor sectors in the first pass. Hull engineering, protective materials, armor packages and relevant technologies may contribute to the rating, with capacity, cost and compatibility consequences to be specified in the content model. **The exact mitigation formula and balance caps remain open**; this is design intent, not an assertion that the current compiler or save schema already implements armor. See [tactical combat](tactical-combat.md) for combat ordering.

## Components and shared capacity

Major component families are power generation, power distribution where represented, propulsion, life support and accommodation, computing/command, sensors, shields, energy weapons, torpedoes, science facilities and mission/support equipment. Sensor and computer packages may be grouped in the MVP while retaining explicit dependency ratings.

Every component version defines its internal capacity demand, operational power demand, crew requirement, dependencies, compatible hull/mount tags, costs, reliability and role outputs. A reactor supplies power and consumes capacity; crew accommodation and life support also consume capacity. The hull's published usable capacity already excludes non-configurable structure, so the validator must not charge that structure twice.

Mandatory minimums are functioning power generation, life support for the embarked crew, computing/command and propulsion for a mobile ship. Some roles impose additional requirements: exploration needs sensors and science capacity; a diplomatic mission may need suitable facilities; colonization needs the existing colony equipment. Requirements should explain capability, not enforce every role as a rigid slot template.

Optional systems compete for the same space. More labs can mean fewer weapons, less shielding or a smaller reactor. An unarmed science ship and a heavily armed escort can use the same compatible hull. Role selection supplies recommendations and desired performance, not arbitrary restrictions.

Dependencies are numeric requirements or explicit tags: crew support capacity >= embarked crew; computer capacity >= supported system demand; propulsion rating must support the hull's design load; a transporter, when introduced, needs its defined sensor/computing support and mission power. Validate dependencies from data and report the deficient system. Do not implement detailed subsystem failures or individual crew management in the MVP.

## Power and operating profiles

Distinguish installed maximum demand from power that must be supplied simultaneously. A ship can legally install more peak demand than its reactor supports if its saved operating profiles provide a feasible allocation. Effective available power is the lesser of reactor output and the hull's power-distribution capacity, after any explicitly modelled operating penalty.

Every mobile class has Cruise and Combat profiles; science/exploration classes also have Survey. Cruise powers essential crew and command systems, sensors and the selected propulsion output. Survey powers essentials, necessary station-keeping and scientific equipment. Combat powers essentials, combat propulsion, targeting, shields and the chosen weapon output. Normal warp cruise and battle propulsion are not automatically counted at full demand together; unusual warp combat requires explicit future rules.

Essential systems receive their required allocation first. Profiles then allocate the remaining power according to saved priorities. A component can scale down only if its data defines a valid performance curve and minimum operating allocation. Otherwise it is either enabled at its stated requirement or disabled. Allocated power cannot exceed effective available power.

Show available power, requested power, actual allocation and the resulting capability for each profile. A player can accept reduced combat weapon output or survey speed, but must see the consequence before saving. Never display full shield, weapon and engine output when that combination cannot be powered.

In the MVP, resolve profile allocations into aggregate ship statistics before movement, surveying or combat. Combat may use one allocation for the encounter; doctrine modifies the already powered statistics and does not grant free reactor output. Later phase-specific allocations must remain deterministic and be part of the pre-battle plan. No per-shot power switches, battery simulation or new shield-regeneration rules are introduced.

## Validation and derived performance

Hard validation rejects capacity overflow, incompatible mounts or envelopes, missing scientific/application availability, insufficient mandatory crew support, unsupported dependencies and infeasible essential power in a required profile. A blueprint can be saved as a draft while invalid, but cannot be released for construction.

Warnings cover weak defence, unsuitable range, low maturity, poor role performance, high maintenance or expensive construction. A weak design may still be legal. A role warning must not silently become a universal hard requirement: an unarmed explorer can be valid.

Compile legal designs into the existing campaign and combat outputs: persistent hull, encounter shield capacity, attacks by range, accuracy, evasion, movement budget, operational range, sensors and science/mission capability where supported. Use explicit content formulas and caps. Do not double-count faction modifiers or maturity bonuses when a blueprint is compiled and later used.

Keep compatibility with Chapter 4: deterministic simultaneous volleys, preselected doctrine, encounter-local shields and persistent hull damage. Component variety does not itself add ammunition, critical hits, subsystem damage or tactical repair. The first implementation may aggregate multiple compatible weapon packages into the existing attack-by-range model.

Fleet speed and range aggregation must be explicitly consistent with Chapter 7 logistics and the navigation design. Do not replace operational supply bands with fuel-unit bookkeeping. Before implementation, confirm how slow ships and support ships affect a fleet rather than inventing a second movement system.

## Player design and deployment loop

1. Choose the strategic need: border escort, surveyor, diplomatic cruiser, transport or another supported role.

2. Select an available hull version and a known template, or start from the hull's required operating systems.

3. Install compatible component versions and quantities. Compare capacity, mounts, crew and costs live.

4. Set operating profiles. Inspect actual speed, shields, weapon output and mission capability, including any power-limited performance.

5. Resolve hard failures and review warnings. Compare against the class being replaced and against cheaper alternatives.

6. Save a named draft. Ordinary combinations of available, compatible components can be released without a mandatory Engineering tax. Novel interfaces or exotic combinations require an explicit integration project only when content defines one.

7. Publish an immutable class revision and queue construction at eligible yards. Show local Industry workload, gradually consumed delivered materials and recurring upkeep. Normal construction does not charge credits up front; distinguish any compiled credit valuation from an actual charge. See [normal production and material supply](time-movement-mobilisation.md#normal-production-and-material-supply--agreed-25-october-2026) for the agreed rule and versioned implementation status.

8. Review field results. Decide whether the best next investment is more ships, a component refinement, a hull revision, a refit or a new scientific direction.

Templates and an auto-complete option help players start with legal designs. Advanced controls expose trade-offs progressively. A simple player should be able to select a template and adjust two or three packages; detailed design remains optional.

## Engineering hulls and components

Science creates the possibility; Engineering turns it into deployable hull/component versions. Refinement projects target attributes such as compactness, power efficiency, durability, reliability, manufacturing cost or maintenance. Hull projects may improve usable capacity, structural strength or distribution capacity within content-defined limits.

A proposal for each project shows Engineering workload, prerequisite knowledge, intended improvements, explicit trade-offs and deployment compatibility. Mount rearrangement or adding a new mount may require a major redesign; a modest efficiency refinement cannot arbitrarily change a hull's architecture. New qualitative capabilities still need their enabling Science.

Use the agreed diminishing-return curve below, specialized refinement tracks and positive cost floors. Readiness/release thresholds remain separate from resolved performance. Do not add a second independent maturity currency or award the same compactness benefit both through maturity and a hull-version bonus. The new version records the resolved outcome of the project.

The proposed Engineering portfolio uses the empire R&D split and project weights. A hull project competes with reactor refinement, diplomatic equipment and colony Engineering; it does not receive free ship-design points. Work can be paused and resumed. Future readiness/release thresholds remain separately scoped; the [implemented foundation](../science-engineering-v1.md#engineering) still has one target and authored completion at 100%.

Engineering produces a new version, not a free fleet-wide upgrade. Save the old version and its production blueprint. Older, cheaper equipment remains selectable while available. A hull family may have several useful branches, such as a compact escort or a spacious survey platform; refinement should not guarantee one universal best design.

### Reactor-first complete vertical slice — agreed, 13 November 2026

**Reactors are the first complete implementation slice:** scientific eligibility → explicit development → paid refinement → immutable version comparison → designer selection and explicit class publication → new construction → eligible yard refit. The slice must demonstrate actual installed/runtime results, not stop at an unlock or preview. This is agreed future scope, **not runtime implementation authorization in this documentation task**.

**Energy is the primary scientific generation gate.** Supporting Propulsion, Construction and Computing prerequisites should increase logically with generation. Meeting a generation gate enables explicit Engineering; it does not award hardware. Once that generation is enabled, normal refinement within it still needs no fresh Science.

#### Starting reactor prerequisite ladder — provisional

The owner accepted the ladder's direction, but the exact spoken numbers were ambiguous. All numerical rows below are **illustrative or proposed minimum discipline levels**, not settled gates, breakthrough ordinals or current catalogue changes. “Unresolved” means unspecified, not zero or unnecessary.

| Reactor generation | Energy | Propulsion | Construction | Computing | Status |
| --- | ---: | --- | --- | --- | --- |
| 1 | 1 suggested | Unresolved | Unresolved | Unresolved | Proposed starting baseline; values not approved. |
| 2 | 2 | 2 | Unresolved | Unresolved | Owner's illustrative Energy 2 + Propulsion 2 combination. |
| 3 | 3 suggested | Unresolved | Unresolved | Unresolved | Proposed interpolation; supporting thresholds still open. |
| 4 | 4 | 3 in the example; requirement unresolved | 3 candidate; 4 also mentioned | 2 | Owner's illustrative Energy 4 + Propulsion 3 + Construction 3 + Computing 2 combination; not an approved exact row. |

Construction 3 versus 4 remains unresolved. “Propulsion optional at generation 4” could mean no additional increase or no Propulsion gate; neither reading is silently selected here, and the table does not imply previously required knowledge disappears. Exact thresholds, introduction points and mapping to versioned Science predicates need a later explicit decision. Energy's primary role is agreed; the numerical generation-to-level mapping is provisional.

Acceptance must carry one reactor lineage through comparison, publication, a **normal-queue new build and normal-queue refit**, with actual materials, correct cost attribution and unchanged historical ships/revisions. Check useful output against hull distribution and operating profiles: a higher reactor supply rating alone does not prove more usable power. See the [scoped stages and acceptance model](engineering-tree-model-01.md#proposed-implementation-stages).

### Engineering diminishing returns — canon, 13 November 2026

**Agreed owner decision; intended design, not implemented behavior.** Science unlocks a hull/module capability or generation; explicit Engineering develops its hardware. The player can then repeatedly refine a supported target within that generation without fresh Science, conquest or survey while useful potential remains. Every completed refinement improves that target. The initial tunable outcome policy is **70% without a detractor / 30% with a detractor in another supported attribute**; it is not a 30% failure chance.

The agreed factor is:

~~~text
r(M) = 1                 when M <= 100
       (150 - M) / 50    when 100 < M < 150
       0                 when M >= 150
~~~

Apply the same factor to the **achieved target-stat improvement**, the **maturity increment** and, in the detractor category, the **downside magnitude**. Do not scale the 70/30 probabilities. Initially use a **fixed standard workload per cycle**: useful gains shrink for the same work, so cost per useful improvement rises without an additional exponential cost multiplier.

**Provisional example parameters:** a base step of 5 maturity points, roughly 10% target improvement (illustrative 8–12% range), 2–5% downside and normal-like distribution width remain tuning proposals. With base 5 / 10% / 3%, starting at maturity 125 gives factor 0.5, hence 2.5 maturity, 5% target improvement and a 1.5% downside only if that category is drawn. The same illustrative cycle at maturity 145 gives 0.5 maturity, 1% improvement and a 0.3% downside. These examples demonstrate the agreed scaling, not final balance. **Maturity 150 does not mean 50% improvement in every stat.**

For the illustrative base step 5 and pre-cycle evaluation, maturity starting at 100 follows `M_n = 150 - 50 × 0.9^n`: it approaches 150 asymptotically. Practical stopping tolerance, useful quanta, finite precision and rounding therefore need explicit policy. The [detailed model and worked examples](engineering-tree-model-01.md#diminishing-returns--agreed-canon-13-november-2026) propose admission only when both outcome categories contain useful representable results, preserving actual maturity below 150 on practical exhaustion. Do not silently snap to 150, charge for no gain, round away a detractor or reroll outcomes on reload/render.

Results remain immutable and deterministically persisted. Retained-parent branching and outcome choices are agreed below. Contextual net-improvement evaluation, physical generation envelopes and practical stopping/quantization remain **proposals**. The [implementation stages](engineering-tree-model-01.md#proposed-implementation-stages) describe future scoped work; current authored-project completion and save contracts remain unchanged.

### Retained-version branches and outcome choices — agreed, 13 November 2026

The player may select **any retained version** as a new Engineering project's parent, subject to that project's normal knowledge, goal, feasibility and refinement-limit checks. A compact branch and a power branch can both start from the same original. **Each branch retains its own history and maturity**: the new branch inherits its selected parent's actual vector, maturity and ancestry, then develops independently. Work on one sibling does not raise another's maturity or consume its refinement potential. This supersedes the earlier proposal for a shared family/generation maturity budget; a common physical generation envelope is distinct from shared expenditure.

Present the resolved outcome with **save / rollback / continue**:

- **Save:** retain the immutable Engineering result for later selection. This does not publish a ship class or update an existing design.
- **Rollback:** select an earlier retained parent for another **paid normal project**. Completed work is not refunded; the saved experiment and result are not erased, edited or rerolled.
- **Continue:** use the resolved result as parent for another paid normal refinement, with its maturity/history and the same agreed curve and 70/30 process.

Persistence precedes this choice; reopening, rendering or reloading cannot redraw a resolved result. A new paid project has its own durable identity. A downside can be addressed by targeting the affected characteristic through the **full normal refinement process**; there is no separate paid-erasure service or guarantee that the next result has no detractor.

To deploy saved hardware, the player explicitly selects versions in the designer and publishes a **new immutable ship-class revision**. Existing designs, ships and queued builds retain their references. A refit explicitly targets a chosen published revision at an eligible yard; saving or continuing Engineering never performs that refit automatically. See the [detailed branch and outcome contract](engineering-tree-model-01.md#retained-version-branches-and-outcome-choices--agreed).

## Worked example: one hull, two missions

Illustrative units only. A cruiser hull provides 100 internal capacity, two compatible energy-bank mounts and one launcher mount. Its reactor uses 18 capacity and supplies 60 power; distribution also supports 60. Propulsion uses 16 capacity, life support/accommodation 12, and command/computing 8. The common core therefore uses 54 capacity, leaving 46.

A survey configuration adds sensors (8 capacity), labs (18), diplomatic facilities (10), shields (6) and one small energy bank (4): 100 capacity total. It uses one bank mount and no launcher. It has strong survey facilities but little combat equipment.

An escort configuration adds sensors (6), shields (12), two energy banks (8 each), a launcher package (8) and support equipment (4): 100 capacity total. It consumes both bank mounts and the launcher mount. It has no dedicated labs or diplomatic facilities.

Escort Combat example: essentials require 10 power, battle propulsion 12, sensors/targeting 6, shields 14 and the requested weapons 24. Full requested demand is 66. Only 60 is available, so a shield-priority profile supplies 18 to weapons, with output computed from their defined power curves. It is valid only if required weapon minimum allocations can be met; otherwise some weapons must be disabled. The UI must show reduced attacks, not claim all systems operate at maximum.

Cruise example: essentials 10, sensors 4 and warp propulsion 30 require 44 power. Survey example: essentials 10, station-keeping 8, sensors 10 and labs 24 require 52. Both fit 60. These allocations belong to their respective configurations and profiles; unused facilities do not acquire fictional power draw.

Suppose reactor refinement reduces its capacity from 18 to 15 while maintaining output, and a compatible hull revision increases usable capacity from 100 to 104. A newly built or suitably refitted surveyor gains 7 capacity: 3 from the compact reactor and 4 from the hull. Existing ships gain neither automatically. If the hull revision changes non-retrofittable structure, an old ship can gain only the reactor's 3 capacity through a permitted refit.

The example illustrates arithmetic and opportunity cost, not final performance formulas or a commitment to these exact hull statistics.

## AI ship design and difficulty

The strategic planner requests a mission capability and deployment deadline, not simply the strongest ship. A diplomatic plan may need affordable contact/survey ships and defensive escorts; conquest may need sustainable combat forces and invasion support. The same configurable faction abilities and personality used by Chapter 5 influence those requests.

The operational planner compares building existing classes, refitting ships, refining a component/hull and waiting for a new design. Count Engineering opportunity cost, yard time, transport of named resources, maintenance, supply and time to reach the theatre. An ideal blueprint delivered after the crisis can lose to a proven ship available now.

Generate legal candidate fits from role templates, available versions and bounded substitutions. Validate them using the player's validator. Compare candidates on role performance, cost, reliability and delivery time; discard dominated alternatives before deeper evaluation. Evaluate balanced, hostile and uncertain cases using only lawful observations and remembered enemy equipment, never the actual hidden enemy fit.

Difficulty reuses Chapter 5's deterministic work budgets: easier levels compare a few proven templates and obvious upgrades; harder levels compare more hulls, mixed generations, power priorities, fleet compositions and Engineering-versus-production plans. All levels obey the same capacity, availability, power, cost and shipyard rules. No free designs, hidden counter-build knowledge or favorable component stats.

Keep class commitments stable. Redesign after a significant mission change, new deployable version or credible enemy observation; avoid publishing a new class every turn for marginal gains. Persist design requests and rationale with strategic milestones. Logs explain why the selected class advances the plan and why refinement, refit or another hull was rejected.

## Refit and fleet modernization

**Agreed default, 13 November 2026; design only.** Publishing a new class revision is an explicit designer action. Existing designs, built ships and queued builds retain their referenced class, hull and component versions. Each ship has immutable build provenance plus its current installed fit; unlocking or refining equipment changes neither automatically.

A refit compares the ship's actual installed fit with an **explicit target published class revision** and validates the actual hull. Compatible component replacements are normally eligible. A newer hull version needs an explicit rule identifying which changes can be retrofitted; substantial structural expansion, new geometry or incompatible mounts require new construction.

Refits require an **eligible friendly or treaty-authorized yard/facility**, allocated Industry and actual delivered materials. **Refit orders join the normal local production queue**, receiving Industry under its scheduling policy. They have no separate production budget and imply no parallel work. Progress requires the actual allocated local Industry, materials and facility readiness; elapsed completion time includes queue waiting. The ship is unavailable for normal fleet orders while refitting. No remote fleet upgrade. Credit-based acceleration is a separately scoped rush mechanism, not an additional assumed ordinary refit ingredient.

For a **new ship**, manufacturing work is the hull's Industry plus the sum of installed component Industry multiplied by quantity; actual material requirements are aggregated too. The [existing compiler](../../src/HobbyDS9.Simulation/ShipDesignCompiler.cs#L140) already totals that complete fit. A refit instead charges only the changes under the rule below; the unchanged hull is free of refit workload.

### Default refit workload — agreed

~~~text
RefitIndustry =
    sum(Industry for newly manufactured changed equipment)
  + removal work for removed equipment
  + installation/integration work
~~~

- **Unchanged hull and components contribute zero** refit workload and need no replacement materials. Compare actual installed quantities/versions with the explicit target; do not charge the whole target ship again.
- The old equipment's manufacture is **sunk work**. Do not subtract its original Industry from the target cost. A cheaper replacement creates neither negative Industry nor an automatic work, credit or material refund.
- Newly manufactured changed modules require their **actual material inputs** as well as Industry. Lower manufacturing work does not waive material, yard or compatibility requirements.
- Salvage and reuse of removed modules require a **future explicit storage/material system**. No automatic full refund, recovered stock or reusable module is assumed.
- A cheap modification of existing hardware is available only through a **later explicit conversion recipe**. A similar-looking or cheaper new version does not itself establish such a recipe.

This default **supersedes the prior provisional 40–60% blanket replacement-cost guideline for ordinary full component replacements**. Removal and installation/integration coefficients remain tunable/proposed. Explicit future conversion recipes may specify different work and materials; they are not a fallback discount on ordinary replacement.

**Approved illustration, not fixed coefficients:** replacing a reactor whose original manufacture cost 100 Industry with a newly manufactured 90-Industry reactor, plus 10 removal and 18 installation/integration, requires **90 + 10 + 18 = 118 Industry**. The old 100 is not deducted. At 40 Industry per turn, with sole yard allocation and no other supply, timing or facility constraints, this takes **three turns receiving production: 40 + 40 + 38**. Queue waiting, lower allocation or missing materials/facility readiness can extend elapsed time; three turns after ordering is not guaranteed.

Ordinary refitting does not erase persistent hull damage. Repair is separately charged or explicitly included in the work order. When maximum hull changes, preserve the existing damage amount. If the resulting hull would be zero or negative, require explicitly funded repair before release; do not silently clamp it to a healthy ship. Swapping versions grants no free healing or replenished stocks.

A queued build never silently adopts a new revision. Where a separately supported conversion permits a change, show additional cost, schedule impact and any loss of work before confirmation. Otherwise retain the original order or cancel/requeue under its existing rules. Existing prepaid cancellation refunds must not be repurposed to refund sunk manufacture in a refit.

Version retention must support save/load, battle snapshots and queued orders. Retired designs remain resolvable for existing ships even if hidden from the normal build list.

**Implementation boundary:** current [whole-design cost compilation](../../src/HobbyDS9.Simulation/ShipDesignCompiler.cs#L140) prices new construction; neither that total nor target-minus-old is a refit estimator. The [streamed queue contract](../campaign-activities-streamed-v1.md#normal-queue-and-physical-inputs) currently has one FIFO/head per colony, at most one completion per turn and no carry to the next item. Its [production kinds](../../src/HobbyDS9.Contracts/CampaignColonisationContracts.cs#L127) and inspected [command contracts](../../src/HobbyDS9.Contracts/CommandContracts.cs#L5) contain no refit order. Refits joining that normal queue, ship locking, active facility readiness, changed-equipment work, atomic installation and save compatibility still need implementation and acceptance.

**Comparative source correction:** the [Galactic Civilizations II manual, p. 41, “Upgrading your ships”](https://archive.stardock.com/files/galciv2manual.pdf#page=41), permits remote upgrading and makes it take longer farther from friendly territory. It does not establish a mandatory yard rule. **HobbyDS9's yard requirement is its own agreed design choice.**

## Unity designer and implementation contract

The Unity screen presents a class-level designer: hull/version and role on the left, schematic and capacity summary in the centre, available components and comparison on the right, and operating profiles plus costs along the bottom. A schematic highlights broad mount groups; it is not a mandatory drag-and-drop packing game.

Show used/available capacity, mount counts, crew support, profile power, role outputs, construction eligibility and maintenance. Explain errors beside the relevant component. Compare old/new values and show exactly what requires Engineering, construction or refit. Add a compact summary mode for template users.

Keep the model in the portable simulation. Proposed records are HullFamilyDefinition, HullVersion, ComponentVersion, ShipClassRevision, OperatingProfile, ShipInstalledFit and RefitOrder. These are design names, not assertions about repository classes. Store stable IDs, lineage, versioned parameters and dependency data; resolve performance through a deterministic compiler shared by human and AI commands.

Core operations are ValidateDraft, CompileDesign, PublishClassRevision, QueueConstruction and PlanRefit, adapted to the existing command conventions. Publishing freezes resolved version values and all deterministic modifiers needed for replay. Later doctrine and situational effects apply at their defined layer. Unity never edits authoritative ship statistics directly.

## Implementation stages and acceptance checks

Stage 1: shared capacity, mounting/envelope validation, required systems, immutable class revisions and templates. Compile into existing movement and combat statistics.

Stage 2: explicit power profiles and player-facing warnings/comparison. Preserve the current combat lifecycle.

Stage 3: Engineering-generated hull/component versions, compatible refits, production revision locking and save migration.

Stage 4: role-driven AI candidate design, Engineering/refit comparisons and automated balancing campaigns. Add further subsystem detail only when it creates useful strategic choices.

Acceptance: identical inputs compile identically; capacity and mount counts never overflow; essentials cannot be starved; weapons never receive more allocated power than available; missing dependencies explain themselves; refinement does not update existing ships or queued builds; old saves can resolve retired versions; incompatible hull changes require new construction; refits respect yard/Industry/logistics and preserve damage; AI uses the same validator and lawful knowledge.

Playtest that both configurations in the example are useful, mature older systems can compete, compactness creates real choices, specialization has a cost, and the strongest AI does not endlessly redesign or bankrupt itself. Test early prototypes against mature equipment without assuming newer automatically wins.

## Open choices and design handoff

Open for later review: whether mass needs a separate budget beyond space and hull compatibility; how many weapon coverage groups are worth exposing; whether class-wide integration workload is needed for more ordinary designs; which hull improvements are retrofittable; refinement base magnitudes, practical stopping/quantization and contextual net-improvement evaluation within the agreed curve and branch rules; refit removal/installation coefficients and explicit future conversion recipes; fleet speed/range aggregation; and whether per-phase power profiles add enough value to justify extra complexity.

Default proposal until review: abstract capacity, package-level mount limits, ordinary compatible designs released immediately, one Combat profile, class-level templates and shared player/AI validation. Treat this as a coherent first implementation target, not a mandate to implement every proposed subsystem at once.

Markdown in this repository is authoritative for these migrated AI and ship-design decisions. The Google Doc remains the legacy reference for chapters not yet migrated. Consult [the design index](README.md) for scope and status; consult implementation documents for actual shipped rules. No gameplay implementation or save migration is implied by this documentation update.
