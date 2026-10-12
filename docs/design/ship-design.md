# Ship design

Design update, 12 October 2026. Agreed: ships use shared internal capacity with specific mounting and compatibility limits, rather than a fixed slot for every component category. Engineering can develop improved versions of hulls and components. The detailed rules below are proposed for implementation and playtesting; numerical examples are illustrative balance values. Implementation status: design specified, repository code not inspected in this update.

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

7. Publish an immutable class revision and queue construction at eligible yards. Show credit cost, local Industry workload, named resource deliveries and recurring upkeep.

8. Review field results. Decide whether the best next investment is more ships, a component refinement, a hull revision, a refit or a new scientific direction.

Templates and an auto-complete option help players start with legal designs. Advanced controls expose trade-offs progressively. A simple player should be able to select a template and adjust two or three packages; detailed design remains optional.

## Engineering hulls and components

Science creates the possibility; Engineering turns it into deployable hull/component versions. Refinement projects target attributes such as compactness, power efficiency, durability, reliability, manufacturing cost or maintenance. Hull projects may improve usable capacity, structural strength or distribution capacity within content-defined limits.

A proposal for each project shows Engineering workload, prerequisite knowledge, intended improvements, explicit trade-offs and deployment compatibility. Mount rearrangement or adding a new mount may require a major redesign; a modest efficiency refinement cannot arbitrarily change a hull's architecture. New qualitative capabilities still need their enabling Science.

Use the existing maturity bands, specialized refinement tracks, saturating benefits and cost floors. Do not add a second independent maturity currency or award the same compactness benefit both through maturity and a hull-version bonus. The new version records the resolved outcome of the project.

The Engineering portfolio uses the existing empire R&D split and project weights. A hull project competes with reactor refinement, diplomatic equipment and colony Engineering; it does not receive free ship-design points. Work can be paused and resumed. Release follows the existing maturity/deployability rules.

Engineering produces a new version, not a free fleet-wide upgrade. Save the old version and its production blueprint. Older, cheaper equipment remains selectable while available. A hull family may have several useful branches, such as a compact escort or a spacious survey platform; refinement should not guarantee one universal best design.

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

A blueprint update affects future construction only. Built ships and queued builds retain their referenced class, hull and component versions. Each ship has immutable build provenance plus its current installed fit. Unlocking or refining technology does not mutate existing hardware.

A refit package compares a ship's current fit with a target fit and validates the actual hull. Compatible component replacements are normally eligible. A newer hull version needs an explicit compatibility rule identifying whether its changes can be retrofitted; substantial structural expansion, new geometry or incompatible mount changes require a new ship.

Refits require an eligible friendly or treaty-authorized yard, sufficient local Industry and credits, and physical delivery of named resources when required. A ship is unavailable for normal fleet orders while refitting. Refits compete with construction according to the yard's supported scheduling model. No remote upgrade of an isolated fleet.

Cost depends on hardware installed and removed, integration work and yard workload; cost floors remain positive. The existing 40–60% of equivalent new component construction cost is a provisional baseline for ordinary replacements, not a universal formula or permission for negative-cost downgrades. Any salvage is bounded, explicit and separate from procurement.

Ordinary refitting does not erase persistent hull damage. Repair is separately charged or explicitly included in the work order. When maximum hull changes, preserve the existing damage amount. If the resulting hull would be zero or negative, require explicitly funded repair before release; do not silently clamp it to a healthy ship. Swapping hull versions cannot heal damage for free.

A queued build never silently adopts a new revision. Where the yard supports conversion, show additional cost, schedule impact and any loss of completed work before the player confirms. Otherwise retain the original order or cancel and requeue under the existing cancellation rules.

Version retention must support save/load, battle snapshots and queued orders. Retired designs remain resolvable for existing ships even if hidden from the normal build list.

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

Open for later review: whether mass needs a separate budget beyond space and hull compatibility; how many weapon coverage groups are worth exposing; whether class-wide integration workload is needed for more ordinary designs; which hull improvements are retrofittable; final maturity penalties and refinement limits; fleet speed/range aggregation; and whether per-phase power profiles add enough value to justify extra complexity.

Default proposal until review: abstract capacity, package-level mount limits, ordinary compatible designs released immediately, one Combat profile, class-level templates and shared player/AI validation. Treat this as a coherent first implementation target, not a mandate to implement every proposed subsystem at once.

Markdown in this repository is authoritative for these migrated AI and ship-design decisions. The Google Doc remains the legacy reference for chapters not yet migrated. Consult [the design index](README.md) for scope and status; consult implementation documents for actual shipped rules. No gameplay implementation or save migration is implied by this documentation update.
