# Fleet interception and tactical combat

Decision date: 12 October 2026.
Status: **agreed direction**, **proposed detailed resolution**, **open balance**.
Documentation only. This chapter records intended gameplay and does not claim the current combat implementation already supports these rules.

Read [the design index](README.md), [time, movement and mobilisation](time-movement-mobilisation.md), [ship design](ship-design.md), [strategic AI](strategic-ai.md), [architecture](../architecture.md) and current milestone/acceptance records. This chapter supersedes conflicting legacy GDD assumptions on fleet-size caps, automatic combat from co-location, fixed Long → Medium → Close combat phases, and doctrine being chosen only once before battle.

## Agreed design principles

- **No hard fleet-size cap.** More ships always add real military capability. The game must never forbid a low-tech power from compensating with numbers.
- Fleet size has natural physical costs such as signature, coordination burden, reaction difficulty and disengagement complexity, but basic ship performance does not receive an arbitrary penalty merely because a fleet exceeds a UI cap.
- **Command is primarily a positive force multiplier.** A fleet without an elaborate hierarchy still functions at its baseline capability. Flagships, fleet/wing/squadron commanders, command ships, relay ships, doctrine and command technology improve coordination above that baseline.
- Strategic cruise speed, operational agility and interception capability are distinct concepts. A fleet can travel quickly in a straight line yet react poorly to a small evasive contact.
- Detection, tracking and interception are separate. Passing through the same strategic region does not automatically create combat.
- Tactical combat is **not** a true 3D physics/position simulation. The authoritative backend resolves formations through abstract relative tactical states. Unity visualizes the already-decided result cinematically in 3D.
- Player influence comes from ship design, fleet composition, commanders/doctrine, opening plans, and round-by-round tactical orders rather than manual steering or per-weapon clicking.
- Combat is a sequence of maneuver-and-fire rounds, not a forced one-way Long → Medium → Close progression.
- Battle objectives can be breakthrough, holding, escort, delay, raid, pursuit, disengagement or destruction. Destroying every enemy ship is not the universal victory condition.

## Strategic posture, signature and contact states

A fleet has an operational posture that affects how it searches, hides and reacts.

Initial posture vocabulary:

- **Aggressive** — actively searches for contacts and attempts to develop interception opportunities consistent with rules of engagement. Better search/interception effort, but greater emissions and a larger chance of being detected.
- **Standard / neutral** — normal navigation and sensor operation. Follows the mission plan and reacts to contacts without aggressively hunting them.
- **Evasive** — minimizes emissions, avoids likely interception lanes and prioritizes breaking contact. Harder to detect/track, but may sacrifice sensor performance, route efficiency or mutual support.

Rules of engagement are separate from posture. An aggressive fleet may be ordered to intercept unidentified contacts, challenge neutrals, attack only declared hostiles, or merely report them.

Contact develops through a state ladder such as:

**Undetected → Detected → Tracked → Interceptable**

Exact labels may be adjusted to align with the wider fog-of-war vocabulary, but the distinction is agreed:

- detection means evidence or presence is known;
- tracking means the observer has a sufficiently useful estimate of motion/intent;
- interception requires both a usable track and the physical ability to reach the projected path in time.

A detected contact is not automatically targetable or catchable.

## Fleet signature, stealth and cloaking

Fleet stealth is not the arithmetic average of ship stealth. Adding ships generally makes a formation easier to detect because the fleet produces more emissions, disturbances and observable activity.

Signature should be derived from the ships and current behavior, with modifiers from:

- hull/ship size;
- number of ships;
- propulsion and power state;
- sensor/emission posture;
- formation spacing;
- local terrain/phenomena;
- stealth technology;
- cloaking technology;
- damage and exceptional systems.

This prevents adding several stealthy scouts from magically making a large capital ship harder to find.

Cloaking is a major signature reduction, **not an absolute invisible switch**. Ordinary sensors may fail to establish a direct track, while sufficiently capable or prepared opponents can detect indirect evidence such as subspace/warp disturbances, field imperfections or other authored counter-signatures.

Prepared anti-cloak measures can include detection grids, specialized sensor platforms, coordinated scans and future technology/content equivalents. The design preference is **technology + preparation + positioning**, not a universal `Cloak Detector II` switch.

Remaining cloak costs and limits — sensor restrictions, communications, high-warp detectability, weapon readiness and faction-specific behavior — remain content/balance decisions.

## Strategic speed, agility and interception

Separate three concepts:

1. **Strategic cruise speed** — how much route progress the fleet can sustain during the weekly movement phase. The baseline direction is that a fleet intending to remain together is limited primarily by its slowest participating ship, subject to future support/formation rules.
2. **Operational agility** — how rapidly the formation can alter course, reorganize, respond to contacts, detach elements or regain cohesion.
3. **Interception capability** — whether a tracked contact can actually be reached before it exits the relevant interception space. This combines relative speed, agility, information quality, posture, command and geometry.

Fleet size should not simply apply `-1 movement per N ships`. Large fleets can make good strategic progress while being slower to react. Small patrols and task groups should therefore be disproportionately useful for screening, scouting and rapid interception.

When a large fleet detects a small fast target, it may detach suitable fast elements instead of turning the whole force. Detachments and temporary task groups are an intended expression of fleet organization.

Possible pursuit behavior includes:

- **Maintain formation** — interception is constrained by the formation as a whole.
- **Fast elements pursue** — suitable ships detach or form a temporary pursuit group.
- **Full pursuit** — the formation sacrifices cohesion to maximize pursuit pressure; exact bonuses/risks are open.

Once a track exists, the core question is geometric and operational rather than another arbitrary combat-trigger roll: can the interceptor reach the target's projected path before the target escapes or breaks the track?

## Fleet command hierarchy

Fleet command should feel closer to a force-multiplier hierarchy than a hard capacity gate.

Conceptual hierarchy:

**Fleet Commander → Wing Commanders → Squadron Commanders → Ships**

A fleet may contain any number of ships. Additional ships continue to contribute their normal hulls, shields, weapons, sensors and engines. Command structure adds positive coordination effects such as:

- faster reaction and formation changes;
- better sensor fusion and shared tracking;
- stronger screening/interception;
- more effective concentration of fire;
- better reserve handling and disengagement;
- more sophisticated conditional orders;
- improved coordination of multiple squadrons or wings.

Command span can limit how widely a commander's bonuses propagate, but exceeding that span does **not** make ships worse than their baseline. Bonuses can diminish or cease beyond effective span.

Flagships, dedicated command ships, relay ships and engineered command/communications systems can increase effective span, resilience, information sharing or order-change performance.

Loss of a commander or command ship removes or reduces the associated bonus rather than making ships forget how to fight. Succession of command should allow another officer to assume control with their own skill profile.

Officer traits should eventually express meaningful specialties — pursuit, defensive formation, mass coordination, independent task groups, breakthrough, screening, etc. — rather than generic `+5% damage` bonuses.

## Faction and doctrine identity

The underlying rules remain generic; faction identity comes from data, doctrine, command technology and ship capabilities.

Illustrative directions discussed:

- **Federation:** strong distributed coordination, sensor sharing, flexible combined-arms response, protection of damaged vessels and graceful command succession.
- **Cardassian:** strong structured formation doctrine, coordinated target selection, patrol/interception screens and bonuses while formation integrity is maintained.
- **Klingon:** resilient decentralized command, aggressive pursuit and independent captains; weaker dependence on centralized control.
- **Romulan:** coordinated stealth, information control, cloaked maneuver and synchronized ambush.
- **Dominion:** disciplined high-load formations and strong execution under casualties, with possible vulnerabilities around specific command structures.

These are design archetypes, not hard-coded faction-name conditionals.

## Fleet composition and combined arms

Do not prohibit homogeneous fleets. A mono-design fleet can be extremely powerful at the thing it was built to do, but it should have a narrower tactical toolbox.

Ship design creates capabilities; fleet composition combines those capabilities; battle orders decide how to exploit them.

Useful battlefield functions can include screening, battle-line pressure, strike/breakthrough, support, reconnaissance, command and reserve. These are functional roles, not mandatory class labels or required percentages.

The desired rock-paper-scissors effect is systemic rather than a direct `class A +20% vs class B` table: one capability creates a tactical problem that another capability is designed to solve.

Reconnaissance matters because the correct battle plan depends on what the player knows about enemy composition and state. Information remains limited by sensors, stale intelligence and deception.

## Weapon families and engagement envelopes

Do **not** define weapons primarily as Endless-Space-style Long/Medium/Short classes. Range modifies firing quality and tactical opportunity, but the weapon's identity comes from its behavior.

Initial Star Trek-inspired families:

- **Beam phasers / equivalent energy beams** — versatile, sustained, comparatively broad firing coverage and useful precision; effectiveness still changes with range and firing solution.
- **Pulse phasers / equivalent pulse energy weapons** — concentrated, rapid forward attack well suited to aggressive close or medium attack runs; exact envelope remains balance/content.
- **Photon torpedoes / equivalent guided ordnance** — long reach and high potential damage, but distant launches give defenders more time for evasion, jamming, interception or maneuver.
- **Quantum torpedoes / later equivalents** — more advanced/powerful torpedo technology, not merely a different arbitrary range category.

Other faction weapons can fit the same behavioral model.

Distinguish maximum reach from effective engagement envelope. A weapon may technically fire at long distance while having a poor chance of producing a decisive result there.

Weapon coverage/arcs matter in **categorical** form rather than exact degrees and coordinates. Examples include broad coverage, forward concentrated, lateral/broadside, aft and restricted coverage. Exact categories exposed to players remain open.

A forward-heavy design should perform especially well when its battle orders repeatedly create forward attack opportunities. The same design should lose much of that advantage when forced into a turning fight, flank, or fighting withdrawal. This predictable relationship between design and outcome is a core readability requirement.

## Authoritative abstract tactical geometry

The backend does not track exact ship XYZ coordinates, yaw, pitch, collision paths or meter-scale firing arcs.

Combat resolves primarily at **formation/squadron level** using relative tactical state. Candidate state variables include:

- **Range state** — e.g. Distant, Engagement, Close, Intermixed;
- **Aspect / relative orientation** — e.g. head-on, beam, flank, rear;
- **Spacing** — e.g. tight, normal, dispersed;
- **Cohesion** — formation integrity/ability to act together;
- **Positional advantage** — a bounded relative advantage used by maneuver resolution;
- target access/screening state where needed.

Exact labels and scales remain implementation/tuning decisions. The agreed principle is to calculate **relative geometry between formations**, not literal coordinates.

A maneuver attempt compares relevant capabilities such as relative speed, agility, commander/command quality, sensors/tracking, doctrine, current cohesion and order suitability. A result changes one or more relative tactical states. Do not hard-code the illustrative weights discussed in design conversation without separate balance review.

Examples:

- a successful flank attempt can move aspect from head-on toward beam/flank;
- a close/charge order can reduce range if the opponent cannot successfully maintain distance;
- a screening formation can deny or degrade target access to protected command/support groups;
- breakthrough can create an Intermixed state and, on sufficient success, allow elements to leave the encounter toward their strategic objective.

## Tactical command rounds

Combat is resolved as a sequence of tactical rounds.

Before combat, each side selects an **opening battle plan**. After each round resolves, the player receives the tactical picture that their sensors and command network legitimately reveal. They may then keep or change maneuver and targeting orders for the next round.

Orders **persist by default**. The player is not required to click every round. If no change is desired, the fleet continues its current plan.

Order selection is simultaneous: each side chooses from the information available at the start of the round, then both sides commit. A player does not see the opponent's newly selected order and counter-pick after the fact.

A round conceptually resolves:

1. present lawful tactical information;
2. choose/retain maneuver orders;
3. choose/retain targeting orders and objective priorities;
4. commit both sides simultaneously;
5. resolve maneuver and update relative geometry;
6. establish firing opportunities;
7. resolve weapons/damage using deterministic simultaneous rules where appropriate;
8. update cohesion, objective progress, withdrawals, pursuits and surviving formation state;
9. produce an ordered event trace for presentation/replay;
10. continue to the next round if the encounter remains active.

Better commanders can make order changes less disruptive, coordinate different wings under different orders, or unlock conditional/contingency behavior. Exact costs for changing orders remain open.

## First-pass maneuver orders

The agreed first-pass vocabulary is intentionally small. Naming can still be tuned.

- **Maintain Distance / Hold & Fire** — try to stabilize at a preferred engagement distance while preserving formation and firing opportunity.
- **Charge / Frontal Assault** — close aggressively and preserve forward attack aspect where possible.
- **Flank** — sacrifice immediate direct pressure to seek superior aspect/target access.
- **Screen** — position suitable elements to protect designated formations, transports, command/support ships or a battle line.
- **Breakthrough** — create/exploit a gap and move designated elements through the enemy formation toward the strategic objective.
- **Pursuit** — chase withdrawing or breakthrough elements, accepting possible formation stretching.
- **Fighting Withdrawal** — increase distance while continuing organized fire.
- **Disengage / Flee** — prioritize breaking contact and survival over offensive pressure.

These orders should reward different combinations of speed, agility, durability, weapon coverage, sensors, screening assets and command quality.

## Tactical pattern callouts — proposed presentation names

The player requested Star Trek-style verbal/UI identifiers such as “Attack Pattern Omega Three”, “Defensive Pattern …” and “Evasive Maneuvers …”. **These are original game labels, not claims that the exact combinations are on-screen canon.** They are presentation aliases for the stable maneuver orders above, **not separate tactics, buffs, technologies or extra simulation rules**. Always show the practical order name alongside the callout so that players can predict what a commander will do.

| Underlying maneuver order | Suggested spoken / displayed callout | Plain-English UI pairing |
| --- | --- | --- |
| Maintain Distance / Hold & Fire | **Defensive Pattern Beta Two** | Defensive Pattern Beta Two — Maintain Distance |
| Charge / Frontal Assault | **Attack Pattern Alpha One** | Attack Pattern Alpha One — Charge |
| Flank | **Attack Pattern Delta Three** | Attack Pattern Delta Three — Flank |
| Screen | **Defensive Pattern Gamma One** | Defensive Pattern Gamma One — Screen |
| Breakthrough | **Attack Pattern Omega Three** | Attack Pattern Omega Three — Breakthrough |
| Pursuit | **Intercept Pattern Sigma Four** | Intercept Pattern Sigma Four — Pursuit |
| Fighting Withdrawal | **Defensive Pattern Epsilon Two** | Defensive Pattern Epsilon Two — Fighting Withdrawal |
| Disengage / Flee | **Evasive Maneuvers Zeta Four** | Evasive Maneuvers Zeta Four — Disengage |

This is an initial vocabulary proposal, not a locked nomenclature or evidence that these named patterns exist in canon. Alternative localized or faction-specific names can later map to exactly the same underlying order ID. Targeting priorities remain plainly worded (for example “Concentrate Fire”), avoiding unnecessary lore names for basic fire-control choices.

## Subsystem targeting — explicitly parked, not committed

**Decision, 12 October 2026:** targeting individual engines, weapons, sensors, reactors or other ship subsystems is appealing and could someday deepen combat, but it is **out of scope for the first combat implementation and not an approved future commitment**. The team may revisit or reject it after testing maneuver, relative range/aspect, ship-level targeting and shields/hull damage. Do not require a subsystem-damage schema, per-component combat durability, status effects or system-disable orders in the MVP.

Target orders for the first pass operate on ships and formations rather than individual subsystems. “Disable engines”, “target weapons” and the associated capture/disable behavior are candidate later ideas only, not available selectable actions until separately designed and accepted. No placeholder attack-mode UI should imply that the backend already supports them. New subsystem mechanics must justify their additional modeling, AI, save, determinism, balance and UX costs before adoption.

## Targeting orders

Maneuver and targeting are separate so the player can combine them rather than select from dozens of bespoke battle cards.

Initial targeting concepts include:

- Fire at will;
- Concentrate fire;
- Prioritize command/support ships;
- Prioritize escorts/screens;
- Prioritize damaged/crippled ships;
- Ship-level objective and target priorities only for the initial combat pass; individual subsystem disabling is explicitly deferred and undecided.

Exact legal priorities, information requirements and faction/doctrine options remain content design.

## Battle objectives and encounter completion

Combat does not universally end only when one side is destroyed.

Supported objective families should include:

- **Destroy / defeat** — inflict decisive combat losses;
- **Breakthrough** — move a required share or designated formation through the enemy line;
- **Hold / block** — prevent enemy passage or retain control for the required tactical duration;
- **Delay** — keep the enemy engaged long enough for a strategic purpose;
- **Escort** — protect designated ships while they cross or leave the engagement;
- **Raid** — damage selected targets and escape before a full engagement develops;
- **Intercept** — prevent a named formation/contact from reaching its destination;
- **Disengage** — extract surviving force;
- **Pursuit** — continue pressure against withdrawing/breakthrough elements.

Ships or formations that successfully satisfy a breakthrough/escape condition can leave the encounter and continue toward the strategic objective rather than magically turning back because enemy ships survive elsewhere in the battle.

Preconfigured retreat thresholds can remain as **automatic contingencies**: if a fleet reaches a declared loss/damage condition, it can switch to a withdrawal/disengage behavior. They are not the only way a fleet may disengage.

## Weapon classes, ammunition and hull-defined attack capability

**Agreed direction, 12 October 2026:** the three universal weapon classes are **Beam/Array**, **Cannon/Pulse**, and **Torpedo**. Faction technologies (Federation phasers, Klingon/Romulan disruptors, Dominion phased polarons and other families) provide distinct named implementations and later modifiers. A quantum or plasma torpedo is a variant within the Torpedo class, not a new universal class. The corresponding weapon-fitting rules belong to [ship design](ship-design.md).

- **Torpedo:** longest reach and heavy warhead yield. Distant targets have more opportunity for evasion, jamming and interception; the explosive yield does not itself diminish over distance. Torpedoes are physical consumables: ships have limited magazines, ammunition is spent when fired, and replenishment requires a valid physical supply/resupply route or facility. They cannot magically refill between encounters. Exact magazine-size, volley policy, launcher cycle, torpedo variants and in-battle replenishment rules remain open.
- **Beam/Array:** reliable, accurate/tracking-friendly sustained energy fire, broad coverage where the hull permits it, especially useful around medium engagement distance. Lower single-opportunity peak damage than close-range cannons or torpedo impacts. Its energy demand must fit the powered combat profile, but it has no physical ammunition magazine.
- **Cannon/Pulse:** concentrated high-output energy bursts with typically narrow/forward coverage; best near close range after gaining a valid attack aspect. Firing opportunity falls when flanked or forced into withdrawal. It is power-limited, not torpedo-ammunition-limited.

Weapon class is *not* identical to a fixed range phase. Relative distance and firing aspect change opportunity and effectiveness every round; engagements may open, close, become intermixed and separate again. Hull mount groups restrict which equipment packages and coverage categories can actually be fitted. Ship construction, tactical orders and outcome should therefore be connected and explainable.

## Round-authoritative combat and Unity playback

**Agreed architecture:** the backend is the sole authority for tactical orders, geometry contests, actual weapon attacks, ammunition expenditure, hit/miss, shield/hull damage, destruction, retreats, breakthrough and persistent campaign effects. Unity never decides whether a ship survived, rolls a hit, or adjusts damage to match its animation.

Each command round follows a proposed resolution contract:

1. Freeze current round state and the latest lawful tactical observations; gather both sides' orders simultaneously. Orders persist unless changed.
2. Resolve formation maneuvers, relative distance, aspect, screening, target access and firing solutions.
3. Determine eligible attacks from powered installed packages and remaining ammunition; reserve/consume actual torpedo rounds once.
4. Resolve simultaneous volley or explicitly ordered sub-volley attack effects using deterministic seeded logic. Ships that are eligible at the start of the same simultaneous volley still fire even if destroyed by its end; a destroyed ship cannot act in subsequent volleys or rounds.
5. Apply shields, hull, casualties/destruction and objective consequences once in the authoritative battle state. Emit an ordered, timestampable abstract event trace.
6. Unity receives a round result plus events such as formation maneuver, weapon discharge, target, shield impact, hull impact, ship destruction, disengagement and updated ammunition. The client maps them to cinematic 3D choreography without feeding visual timing back into the result.

The user may alter orders after the resolved round is presented; **do not pre-resolve all later interactive rounds**, since those decisions can change the outcome. For unattended AI battles or quick-resolve, the same round resolver may iterate using the configured standing orders and lawful AI decisions. Round boundary saves and event replay must be deterministic across platforms, save/reload and battle ordering.

Exact weapon stat fields and fire-substep timing are proposals to be tuned: damage/yield, tracking and accuracy, range envelope, coverage/aspect, shot cadence, required power, magazine capacity/ammo type, and special shield/system interaction. Avoid fake per-projectile physical simulation in the authoritative backend.

## Damage resolution and presentation boundary

Existing deterministic simultaneous-volley principles, encounter-local shields and persistent hull damage remain compatible unless separately redesigned. Tactical rounds, geometry and targeting determine who has a firing opportunity; they do not require Unity to calculate combat.

The simulation must produce an authoritative event trace containing enough high-level tactical information for the client to stage the battle, including formation maneuvers, firing opportunities, attacks, impacts, destroyed/disabled ships, command changes, breakthrough/retreat/pursuit events and encounter completion.

Unity may choose cinematic trajectories and spacing that make the event trace visually convincing. It must not recalculate hit chances, target access, movement success or battle outcomes.

This separation is required so AI-vs-AI battles, quick resolve, save/reload, multiplayer/replay and automated balancing can use the same authoritative combat result without a 3D client.

## Player readability requirement

Abstraction is acceptable only when consequences remain understandable.

The player should be able to explain outcomes in terms such as:

- “My forward-heavy ships hit hard because I charged and maintained forward aspect.”
- “Their escorts screened the command ships, so my strike wing could not reach them.”
- “My large battle fleet detected the scout but could not react quickly enough, so I detached fast escorts.”
- “I ordered a breakthrough; enough ships crossed the line even though the enemy fleet survived.”
- “I changed from assault to fighting withdrawal and lost much of my forward-weapon output.”

Major combat UI should expose the relevant state and cause-and-effect without displaying opaque formulas or exact 3D simulation data.

## Open design and balance

Still open for later review and playtesting:

- exact fleet strategic-speed aggregation and support-ship exceptions;
- exact signature formula and size scaling;
- cloak costs, high-warp behavior and anti-cloak mechanics;
- exact contact-state naming and integration with campaign fog-of-war levels;
- command-span curves and which bonuses exist at fleet/wing/squadron level;
- officer generation, progression, succession and casualties;
- exact tactical state scales and maneuver formulas;
- how many formation groups a very large battle should resolve;
- exact weapon envelopes, torpedo defense, magazine sizes, resupply throughput and volley tuning;
- exact categorical weapon-coverage groups exposed in ship design;
- order-switching costs and commander effects;
- conditional orders/contingencies;
- detailed screening and target-access resolution;
- retreat destination semantics after within-turn interception;
- whether subsystem targeting/damage is desirable at all (explicitly parked, not approved future scope); critical hits, repairs and shield regeneration also remain separate open topics;
- AI tactical planning depth by difficulty.

These are not permission to revert to hard fleet caps, true 3D authoritative combat, fixed one-way range phases or one-time pre-battle doctrine. Those higher-level decisions are agreed.
