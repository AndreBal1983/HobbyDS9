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

### Engaged-fleet visibility and cloaked attacks — agreed direction

Decision, 13 October 2026: engaged uncloaked ships are visible because they are within scanning range. Their upcoming orders remain hidden; visibility must not reveal newly selected maneuvers for counter-picking. This establishes visibility of the ships, not automatic knowledge of every hidden equipment statistic or future action; exact scan detail remains open.

Cloaked ships are the exception during the first tactical round. Against visible opponents, a fully cloaked side makes its ordinary round-one attack without targeted return fire against its cloaked ships. **Free attack means round-one targeting protection, not an additional pre-round volley, double attack or fourth round.** Normal powered-weapon eligibility, firing opportunities, cadence and ammunition expenditure apply; free does not mean free resources. In a mixed fleet, cloaked ships may fire while remaining unavailable as direct targets during round one; visible uncloaked ships engage each other normally. Cloak state is per ship: a visible fleet member does not automatically expose the fleet's cloaked members, and hidden members do not conceal the visible ones.

**Reveal timing, 13 October 2026 — agreed:** once the first tactical round finishes, all surviving cloaked ships are uncloaked for the remainder of the encounter, regardless of whether they fired. They are visible and ordinarily targetable from round two onward. Firing does not itself end their round-one protection early, and there is no re-cloaking in later rounds of this slice. Cloaking does not provide repeated untargetable fire or a fresh surprise advantage each round.

**Reactive defense, 13 October 2026 — agreed:** defenders can react to incoming torpedoes fired by cloaked ships. The incoming ordnance is a defensive target even when its launching ship is unavailable as an offensive target. Resolve interception through actual powered defensive equipment, coverage, firing opportunities and the existing finite attempt costs; cloak does not grant torpedoes automatic penetration, and reaction does not grant guaranteed interception or free defensive capacity. This requires no separate per-torpedo player command.

An authoritative engagement with no visible enemy should make the player wary. Show a cautious unresolved-contact/possible-attack state using only lawful encounter information; do not turn that warning into disclosure of hidden ship counts, equipment, locations or intentions. Do not silently replace the player's standing maneuver with Screen or grant extra defensive opportunities because nothing is visible. Exact warning wording and baseline AI response remain proposals.

**Open before implementation:** mutual-cloak handling, initial cloak state in subsequent encounters, and how counter-detection changes opening attack/target eligibility. Strategic evidence of a cloaked contact and tactical target acquisition remain distinct; the earlier anti-cloak preparation direction still needs an explicit targeting contract.

**Strategic cloak setting, 13 October 2026 — agreed:** provide one fleet-level strategic cloak setting rather than individual ship controls. When enabled, ships with authoritative cloak capability approach an encounter cloaked; other members remain visible. The same setting therefore supports fully cloaked and mixed fleets without merging or splitting their command unit. A ship name, faction or local UI flag cannot grant cloak capability.

The setting governs approach to subsequent encounters as well. Uncloaking after round one does not clear the strategic setting, but it does leave every surviving ship uncloaked for the remainder of that encounter. The setting cannot re-cloak ships in round two or three and never restores hull, shields or ammunition. This settles ordinary initial cloak state across encounters; operational cloak costs, restrictions and counter-detection remain separate design work.

**Mutual-cloak decision, 13 October 2026 — agreed:** if both opposing sides enter fully cloaked, neither has a visible target for offensive fire in round one. That round is maneuver-only; it counts toward the normal three-round limit. Both sides uncloak at its end and can fight normally from round two. Do not award either side an extra volley, infer hidden targets, or extend the encounter to compensate for the first round. No offensive ammunition or firing opportunity is spent on a nonexistent shot. This settles mutual cloak behavior for the first slice; future counter-detection rules must be separately designed and reconciled with the baseline.

**Scan-detail decision, 13 October 2026 — agreed:** scans reveal visible enemy ship types, hull condition and shield strength. Enemy ammunition reserves and upcoming orders remain hidden. Cloaked ships do not appear in this visible-ship inspection until revealed under the cloak rules. Do not infer full hidden equipment access from visibility; exact display precision and which equipment characteristics are otherwise observable remain proposals.

Apply this observation boundary to snapshots, target selectors, tooltips, reports and replay alike. Observed torpedo launches/interceptions can be reported, but internal remaining-ammunition fields or private order-commit events must not leak through an omniscient resolver trace. The player may reason from observed behavior; the interface must not supply the concealed answer. Own ships retain their normal exact equipment/resource inspection.

**Implementation gap:** current authored equipment and standalone v2 resolution do not establish this cloak/visibility/free-attack lifecycle. It requires explicitly scoped equipment/state, lawful observation, targeting, resolver/event and persistence work. Do not infer cloaking from a faction or ship name, alter a pinned resolver silently, or treat this documentation as runtime support. Determine its place in the first playable milestone after the open behavior is resolved.

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

## Strategic fleets as tactical units — agreed

Decision, 13 October 2026: the player creates fleets at the strategic level, and each participating strategic fleet enters combat as one distinct tactical command unit. Two fleets travelling along exactly the same route remain separate units if they participate in the same battle. Shared ownership, position or travel orders do not merge their identities or orders.

For the first playable campaign-v2 slice, map each participating fleet to one formation with its own maneuver and targeting orders. Individual ships retain their own weapons, hull, shield sectors and ammunition within that formation. Combat does not automatically split a fleet into role-based formations. Future internal command hierarchies may improve coordination; independently controllable wings, detachments or tactical fleet splitting require a separately agreed command model and are not implied by the hierarchy discussion below.

This settles the command-unit boundary, not encounter eligibility: common routes, path crossings and co-location still do not by themselves establish detection, tracking or interception. Formation capability aggregation and initial relative geometry remain open.

**Target-control decision, 13 October 2026 — agreed:** the player can choose an enemy fleet to focus on. Ships then select suitable individual targets within that fleet automatically according to the authoritative tactical rules. The first interface does not offer individual enemy-ship selection as a firing order, subsystem targeting or manual per-ship fire control. Selecting a visible ship for condition inspection is separate from ordering attacks.

Fleet focus does not guarantee a shot or bypass lawful visibility, range, arcs, screening, power, cadence or ammunition constraints. Each friendly fleet retains its own focus and command identity. Existing standalone ship-priority fields are backend policy inputs, not automatically additional player controls in this simpler campaign interface.

**Focus fallback, 13 October 2026 — agreed:** preserve the chosen enemy fleet as the preferred target whenever suitable legal targets exist. If it is destroyed or offers no valid targets for a ship's firing opportunity, automatically use another visible hostile fleet with legal targets rather than waste an otherwise available shot. Never fall back to allies, hidden ships or targets outside lawful weapon access. The report must explain the fallback. This does not silently replace the player's standing focus; exact deterministic fallback ranking and automatic within-fleet ship selection remain proposed/open.

**Starting-distance decision, 13 October 2026 — agreed for the first playable slice:** initialize each new battle's opposing fleet-formation pairs at **Distant** range. Do not vary starting distance through ambush, approach, sensors or other inferred circumstances in this slice. Subsequent rounds may change range through the authoritative maneuver rules. This sets initial range only; facing initialization and capability formulas remain open. It does not reset surviving hull, shields or ammunition. Variable starting distances are deferred.

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

### Simple fleet orders and capability-based composition — agreed

Decision, 13 October 2026: keep the combat interface and commands simple. The player gives fleet-level orders; ships automatically contribute suitable functions according to their actual equipment and capabilities. Choosing fleet composition strategically should matter more than managing individual ships during battle. A fleet is one command unit, not a requirement that all its ships move identically. Fast escorts retain useful tactical mobility alongside slower capital ships; do not impose the strategic slowest-ship travel rule universally on tactical actions.

Calculate combined-arms benefits within authoritative tactical resolution. Screening effectiveness must follow available capabilities, power, weapon coverage, tactical opportunity and surviving ships, with finite limits and opportunity costs. Ships do not grant a generic defence buff merely because a class label says Escort or a fleet satisfies a prescribed composition ratio. Internal modifiers are acceptable when they represent an explained capability and circumstance rather than a label-based composition bonus. Homogeneous fleets remain valid.

Under a fleet Screen order, capable ships can contribute protection automatically without individual escort assignments. Torpedo interception spends actual firing opportunities, including failed attempts; offensive opportunities consequently compete with defensive work. Protection can be saturated and weakens when contributing ships are destroyed or cannot operate their relevant equipment. Cruisers and capital ships with suitable equipment may contribute too. Show practical results such as interception attempts/successes and threats that penetrated the screen rather than an unexplained composition multiplier.

**Fleet-local protection, 13 October 2026 — agreed:** fleets are separate groups of ships and do not mingle in tactical combat. Screening/protection is internal to the commanding fleet. To protect a transport or colony ship, put the escorts in that same strategic fleet before battle. A Screen order has no optional allied-fleet protection target, and shared ownership, side or encounter does not let another fleet's escorts screen its ships. Automatic defensive contributions and protection benefits must respect this membership boundary.

This supersedes earlier proposals for separately controllable screening fleets protecting another fleet. The standalone v2 resolver currently supports designated allied-formation protection; that existing capability must not be exposed as a lawful campaign order under this agreed fleet model. Any campaign restriction or resolver change requires explicit validation and regression acceptance; documenting the restriction does not remove the standalone capability. Future officer/wing coordination does not imply cross-fleet protection without a separately agreed change.

Exact mobility contribution, automatic role selection, capability aggregation and screening formulas remain **proposed/open**, with no new numerical bonuses approved. The existing standalone v2 resolver already accounts for interception firing opportunities, but its shared formation geometry does not establish the full individual-mobility behavior described here. Review and version any required resolver changes separately; this agreement does not claim runtime support or authorize silently expanding the campaign-lifecycle implementation.

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

Combat resolves primarily at **fleet formation level** using relative tactical state. Each strategic fleet is a distinct tactical unit as agreed above; future squadron hierarchy does not change that command boundary without a separate decision. Candidate state variables include:

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

Combat is resolved in **up to three tactical rounds per encounter**, ending earlier when objectives or battle outcomes require it. The outcome after round three and rules for renewed contact on a later strategic opportunity are separate campaign decisions.

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
10. continue to the next round if the encounter remains active **and fewer than three rounds have elapsed**; otherwise finalize this encounter and leave any renewed-contact mechanics to campaign rules.

Future commander mechanics may make order changes less disruptive or unlock conditional/contingency behavior. Independently ordered wings are wider-roadmap proposals, not part of the agreed one-fleet command model for this slice. Exact costs for changing orders remain open.

## First-pass maneuver orders

The agreed first-pass vocabulary is intentionally small. Naming can still be tuned.

- **Maintain Distance / Hold & Fire** — try to stabilize at a preferred engagement distance while preserving formation and firing opportunity.
- **Charge / Frontal Assault** — close aggressively and preserve forward attack aspect where possible.
- **Flank** — sacrifice immediate direct pressure to seek superior aspect/target access.
- **Screen** — position suitable ships to protect transports, command/support ships and other members of their own fleet. The first slice has no separately designated allied-fleet protection target.
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

Earlier targeting concepts included the list below. For the agreed first campaign slice, player targeting control is enemy-fleet focus with automatic individual selection and legal fallback, as specified above. These concepts may inform backend automatic policies; they are not additional player ship-priority controls:

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

### Inconclusive battles and strategic contact choices — agreed

Decision, 13 October 2026: after a nondecisive battle reaches the three-round limit, surviving fleets remain in their sector with their actual damage, shield stocks and ammunition. Return to strategic planning rather than immediately forcing another battle or treating the round limit as victory. Tactical completion does not itself relocate survivors or replenish them.

Clarification, 13 October 2026: after the battle has resolved, the player chooses strategic actions such as leaving, remaining to re-engage, or following the opposing fleet. Pursuit uses actual strategic-map movement and relative fleet travel capability. There is no separate post-battle catch/escape roll at departure, and no special abstract interception resolver farther along a route. This clarification supersedes the earlier proposed pursuit-versus-escape contest.

Leaving uses a lawful movement route/destination. Following must use lawful knowledge of the contact, not hidden opponent routes. Strategic orders resolve under the common movement timing; freshly submitted opposing orders are not exposed for counter-picking. A pursuer must physically reach the opponent's sector at the same time to make renewed engagement possible. Greater speed can enable that catch; an insufficiently fast fleet cannot be granted contact by an abstract pursuit bonus. Reachability, routes and timing still matter.

| Opposing fleet intentions | Agreed direction |
| --- | --- |
| Both maintain contact | Seek another engagement through the next eligible strategic resolution; no immediate repeat within the completed battle turn |
| One leaves, one follows | Both use strategic movement; another engagement requires reaching the same sector at the same time, with lawful hostile contact and intent to engage |
| Both break contact | Neither pursues the other; depart through their lawful strategic movement routes, retaining normal access, travel costs/progress and other independently relevant encounter constraints |

Here **catch** means reaching a renewed engagement opportunity, not capturing ownership of enemy ships. Fleet intentions do not override physical reach, known navigation, movement budgets or the distinction between detection, tracking and interception. Successful departure does not teleport a fleet to its final destination. Common sector/time is the physical condition for this re-engagement model, not permission to attack allies or reveal unknown fleets.

Detailed simultaneous travel timing, intermediate-sector and weighted-edge occupancy, legal engagement opportunity, movement continuation after contact, multi-fleet target selection and last-known-contact following remain **proposed/open**. A simple engagement/leave intention does not eliminate route and contact-target selection. These are explicit strategic movement/contact rules shared by human and AI; do not add a separate chase probability to avoid defining physical timing.

**Movement-stop decision, 13 October 2026 — agreed:** when fleets meet and engage, combat stops all participating fleets' remaining strategic movement for that turn in the encounter sector. Surviving fleets do not automatically resume their pre-battle route after resolution or finalization. They choose fresh strategic orders during Planning for the following turn. Unrelated fleets are not stopped by this rule. Whether an interrupted route is shown as a reference for an explicit new order is presentation detail; it must not remain an executable movement order. This settles movement continuation after contact; simultaneous/intermediate-sector travel timing and weighted-edge occupancy remain open. Combat finalization must not advance the clock a second time or grant another same-turn travel budget.

**Mutual re-engagement decision, 13 October 2026 — agreed:** if surviving hostile fleets both remain in the sector and choose to re-engage, allocate a new battle during the following strategic turn's resolution. Use their actual surviving ships and retained hull, directional shields and ammunition; encounter creation must not refill or recover them. This is a new encounter following a new strategic decision, not a fourth tactical round or an automatic same-turn replay. Normal in-battle recharge may apply under the pinned v2 rules, without inventing between-battle recovery.

**Implementation scope:** this agreement resolves the intended post-battle choice model. It requires separately scoped campaign contact/movement work; current source inspection/equipment and standalone resolution do not implement it. The first slice still excludes within-battle strategic exit objectives, but its lifecycle proposal must now explain how it will support or explicitly phase this post-battle contact model. Do not silently replace it with permanent engagement, an abstract catch/escape contest, or immediate combat loops. Record any conflict with existing end-of-Movement-only encounter detection before changing that behavior.

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

Exact weapon stat fields and fire-substep timing are proposals to be tuned: damage/yield, tracking and accuracy, range envelope, coverage/aspect, shot cadence, required power, magazine capacity/ammo type, and shield-specific damage or countermeasure interactions **without penetration through active shields**. Avoid fake per-projectile physical simulation in the authoritative backend.

## Directional shield sectors — agreed direction, bounded first-pass model

**Decision, 12 October 2026:** use **separate directional shields** rather than one ship-wide shield pool. This is intended to give flanking, agility, positional advantage and protecting a damaged facing meaningful tactical consequences. The requirement is contingent on a simple, deterministic, performant implementation and acceptance tests; do **not** replace abstract formation geometry with full 3D ship orientation or collision/trajectory calculations.

**Proposed minimal implementation, subject to implementation review and balance:** each ship stores four shield sector values — **Fore, Port, Starboard, Aft** — and corresponding limits derived from its installed powered defensive systems. One aggregate generator capacity can be divided across the four sector maxima by a hull/fit profile; there need not be four independently designed shield-generator components. Sector values deplete independently. An attack's resolved relative aspect plus the deterministic maneuver contest selects the defending ship's exposed sector. The backend performs a constant-time sector lookup, applies shield damage to that sector, and passes any residual impact through to the existing hull-damage rules. A four-sector display is sufficient for Unity; **no authoritative XYZ, ship Euler angles, shot vector math or per-frame arc simulation** is required.

- **Head-on / front access:** primarily exposes Fore; **Rear** exposes Aft. **Beam/Flank** exposes Port or Starboard based on the maneuver/aspect state and a deterministic tie-break or persistent tactical-side assignment, not an invented Unity-camera orientation. Exact mapping is a testing question, especially when formations mix or cross.
- Defender agility and successful maneuvers can deny a clean flank, restore a stronger facing or move out of an unfavorable relative aspect. Attackers must actually win a maneuver/access opportunity to exploit a weakened sector; they cannot freely pick the weakest shield every round. Maintaining desired shield facing may trade against own weapon arc access and other formation objectives.
- Shield damage and shield-sector changes must be part of the authoritative combat-round state and event log (sector, strength before/after, spillover to hull). Preserve simultaneous-volley fairness and determinism. Unity merely shows that facing taking hits and the reported status.
- **Agreed, 12 October 2026 — fixed encounter allocation:** the four sector maximum capacities and initial allocation are established by the ship's engineered hull/shield configuration and combat profile at encounter start; **neither the player nor tactical orders transfer, rebalance or redistribute existing shield points or capacity between sectors during the three tactical rounds**. No automatic forward-bias for Charge, rear-bias for Withdrawal, or free extra strength on the exposed facing. Maneuvers change which shield facing is exposed to incoming fire—not how much shield power was assigned to that facing. Avoid per-ship facing sliders or hidden auto-allocation calculations during combat. This is a deliberate complexity/readability choice: if a decision is not meaningfully controlled or perceptible, do not add a simulation layer for it. The initial built-in sector split may vary by hull/fit but its exact percentages remain a balancing choice. **In-combat regeneration is distinct from redistribution:** the ship's finite per-round recharge budget can restore depleted points within sectors up to their unchanged sector maxima, subject to power and the separately specified suppression/allocation policy; it does not pull points out of intact sectors. Recharge formula, recharge between battles and shield resistance tuning remain open; **bleed-through and special shield bypass are excluded**.
- Performance expectation: four counters per ship and an O(1) sector lookup/damage operation per resolved impact, so directional shields should be inexpensive relative to attack resolution, but no guarantee without benchmarks. Require deterministic save/reload/replay, AI and large-fleet stress tests and an explicit save/content/contract versioning review before implementation. Existing scalar shield fields are not silently treated as already implementing this design.

## In-combat shield regeneration — agreed ship statistic

**Decision, 12 October 2026:** an engineered ship has an explicit **Shield Recharge per Tactical Round** statistic, derived from its defensive equipment and technology. Powered shields recover a **small, bounded amount during combat**; improved shield technologies can raise this statistic and thereby produce meaningful differences from improvements in offensive weapon technology. Regeneration does not repair hull damage or restore destroyed ships.

The intended combat cadence is **three tactical rounds per encounter** (with objectives, withdrawal, destruction or breakthrough potentially ending it earlier). This explicit design constraint supersedes older open-ended wording in this chapter. At most **two regeneration transitions** influence later volleys within the same three-round encounter; avoid balance models that assume unlimited rounds of shield cycling. The precise strategic handling of fleets that remain in contact after round three / repeated engagements is separate work.

**Proposed first-pass resolution contract (tuning still open):**

The first playable campaign slice retains depleted shield stocks between battles with no automatic between-battle recovery. The faster campaign recovery idea below is a deferred wider-roadmap proposal, not an exception to that scoped agreement. New encounter creation uses retained current stocks rather than resetting to a fit's initial allocation.

- Each ship has a compiled base recharge amount in **shield points per tactical round**, and a powered/disabled status or available-power modifier. Technology and installed gear determine the value; one ship-wide recharge *budget* serves the four **Fore, Port, Starboard, Aft** shield-sector pools rather than automatically granting the full rate independently to all four sectors.
- After applying a simultaneous volley's shield and hull damage, a surviving ship may recharge its depleted sectors before the next tactical round. Recharge is capped by each sector's maximum, the ship-wide round budget, and available power. No regenerative healing applies retroactively to the volley just resolved. After round three, handle further recovery as post-combat rather than inventing a fourth tactical round.
- Prefer deterministic allocation toward depleted facings not under active fire this round; sectors under concentrated fire may have reduced or no recharge. Exact prioritization and suppression are balance/implementation details to validate, not yet frozen formulas. Do not let formation rotation replenish all four sectors at full rate or permit infinite shield cycling.
- Between battles, shields may recover by a **separate, faster but not instantaneous** campaign rule; do not silently reset to full on disengagement or save/load. Hull damage continues to require physical repairs.
- Backend is authoritative for recharge and its power costs. Persist sector values, compile the recharge stat through the ship-design contract, and include per-sector recovery in deterministic event traces and save/reload tests. Unity shows the reported state; it cannot regenerate shields independently.

**Illustrative balance only:** a ship with a recharge statistic of 6 points/round could restore at most 6 shield points between rounds 1 and 2 and another 6 between rounds 2 and 3, *across its sectors*, if those sectors can accept the recharge and power is sufficient. This is not a locked numeric tuning target.

## Shield-first damage — no penetration while a sector remains active

**Agreed, 12 October 2026:** **every offensive weapon strikes the currently exposed directional shield sector before damaging hull.** While that shield sector has any remaining strength, its protection absorbs incoming damage up to that remaining strength; **there is no hull bleed-through, passive penetration, chance-based shield bypass or internal-system damage through active shields** in the agreed combat model. This applies to beams/arrays, cannon/pulse weapons, and all torpedo variants, including unusual faction technologies such as Dominion phased-polarons or Romulan plasma torpedoes.

**Resolve each authoritative attack/volley deterministically against the impacted sector:** reduce that sector by the incoming damage it can absorb; **only damage in excess of the depleted sector's remaining strength spills to the hull in the same resolved impact/volley**. For example, a sector with 8 shield points taking 12 damage loses 8 shield points and sends **4 raw damage toward the hull, subject to armor mitigation**; if it takes 6, the sector falls to 2 and the hull receives none. A zero-strength sector affords no protection until recharged. Do not allow full shields on a different facing to intercept a legal hit against a depleted facing without a successful defensive maneuver/aspect change.

Directional facings, maneuver outcomes, the three-round encounter cap, limited shield recharge between rounds and simultaneous-volley fairness remain in effect. Record exposed sector, blocked damage, depleted strength and spillover in the backend event trace. Unity may stage bridge sparks or shield flickers as **cosmetic effects only**; it must not portray actual subsystem failures or hull damage that the backend did not determine.

Weapon-family distinctions can modify **shield damage, tracking, salvo profile, countermeasure vulnerability, power draw, or other approved shield-facing interactions**, but **not shield penetration**. Any future exception would require an explicit new design decision and corresponding combat/save-contract changes; do not infer one from source fiction.

## Hull armor — agreed mitigation layer

**Decision, 12 October 2026:** ships have **armor**, distinct from directional shields and hull integrity. Armor mitigates incoming damage **only after the impacted shield sector is exhausted**. There is no separate ablative armor-health pool in the first-pass model; a ship's compiled armor rating reduces some of the raw damage reaching the hull, and the remainder reduces persistent hull integrity. This preserves an understandable sequence: **resolve hit and sector → shield absorbs → armor mitigates excess → hull takes remaining damage**.

Armor performance derives from hull engineering, suitable protective components/materials and technology, within ship-design capacity and construction trade-offs; it is not a free faction bonus. A tougher ship should survive exposure after a shield facing collapses, while speed, agility and defensive-screening configurations remain meaningful alternatives. Armor is not directional in the first pass; one ship-wide rating avoids adding four more tracked pools.

**Armor mitigation formula, technology scaling, maximum reduction and weapon-vs-armor interactions are OPEN for balance/implementation review.** Percentage mitigation is a candidate to avoid rendering rapid-fire weapons useless with flat per-hit subtraction, but is not yet locked. Ensure armor does not make ships universally invulnerable and preserve deterministic simultaneous-volley processing. Existing designs and save contracts must not be silently treated as already supporting armor. Subsystem damage remains explicitly parked.

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
- armor mitigation formula and scaling; whether subsystem targeting/damage is desirable at all (explicitly parked, not approved future scope); critical hits, hull repairs and **shield regeneration tuning** remain separate open topics;
- AI tactical planning depth by difficulty.

These are not permission to revert to hard fleet caps, true 3D authoritative combat, fixed one-way range phases or one-time pre-battle doctrine. Those higher-level decisions are agreed.
