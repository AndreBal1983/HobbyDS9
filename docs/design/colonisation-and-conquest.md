# Colonisation and planetary conquest

Decision date: 16 October 2026.
Status: **agreed first-pass gameplay framework; detailed rules and tuning open**.

This chapter records the colonisation/conquest design interview and its confirmed summary. It describes intended gameplay, not implemented features. Interview references Q1–Q41 preserve the decisions behind the framework; Q1 rejected shared planetary territories, while Q15 explicitly accepted different owners of different planets in one system.

Read the [design index](README.md), [exploration and first contact](exploration-missions.md), [weekly time and mobilisation](time-movement-mobilisation.md), [fleet interception and combat](tactical-combat.md), [ship design](ship-design.md) and [strategic AI](strategic-ai.md) alongside this chapter. For existing implementation, consult [architecture](../architecture.md), [economy and population](../economy-population-v1.md) and [population, politics and geography](../population-politics-geography-v1.md).

## Agreed direction

### Ownership, political actors and access

- **Q1 / Q15 — Planetary sovereignty:** Each planet has one controlling empire or government. There are no separately owned planetary territories or settlement enclaves in this framework. Different planets in the same star system may have different owners. Local populations can retain distinct identities and loyalties without dividing territorial control.
- **Q3 — Separate kinds of control:** Orbital control, ground control and political integration are distinct. Winning a space battle enables further action; it does not automatically transfer the planet or make its inhabitants loyal.
- **Q4 — Shared faction rules:** Factions use the same basic settlement and conquest rules. Capabilities, policies, explicit commitments and domestic/diplomatic consequences shape their choices. A Federation-style policy can make conquest politically costly without creating an automatic faction-wide prohibition.
- **Q6 — Inhabited independent worlds:** An inhabited world joins through agreement with its government or through conquest. The ordinary colonisation action cannot claim it. How multiple existing local governments provide planet-wide consent remains open; this does not authorise shared territorial ownership.
- **Q20 — Claims are not ownership:** Discovery grants knowledge. Settlement establishes control of the settled planet, not every planet in the system. Other unowned planets remain potential settlement destinations. Claims and treaties can create objections or restrictions without themselves transferring ownership.
- **Q35 — Separate permissions:** Peaceful transit, supply access and settlement permission are distinct diplomatic rights. Permission to travel does not imply permission to draw supplies or settle. Agreements can address unowned planets in a shared system; their negotiation and representation require further design.

### Colonisation, population and establishment

- **Q5 — Founding expedition:** Deliver settlers and colony equipment to an uninhabited world, then establish the colony over several weekly turns. The order presents requirements and expected duration before commitment.
- **Q7 — Environmental requirements:** Suitability determines required equipment and upkeep. Suitable worlds are easier and cheaper to settle; hostile environments need appropriate habitat capability and greater support. Some environments remain unavailable until the relevant capability exists.
- **Q11 — Reusable transport:** The transport survives founding. Its settlers and consumable colony equipment are unloaded and committed to the planet. Another expedition requires new settlers and equipment; the reusable hull does not grant free colonies.
- **Q16 — Real settler populations:** Settlers come from an existing source population and retain their identity. Suggest an eligible departure world and population suited to the destination, while allowing the player to change the selection. Show the source population reduction and warn about seriously weakening it.
- **Q17 — Frontier reserves:** Expeditions carry finite supplies and may establish beyond the existing supply network. Reserves buy time for local production or resupply. Show estimated endurance and emerging shortages.
- **Q22 — Progressive failure and evacuation:** Supply loss slows or stops establishment, worsens conditions and can cause deaths through prolonged deprivation. Provide actionable warnings before collapse where known conditions permit. Rescue and evacuation require actual transport; cancellation does not teleport settlers home.
- **Q26 — Survey prerequisite:** A planetary survey is required to plan habitation and identify known inhabitants or hazards. Detailed scientific study is optional. This requirement does not guarantee perfect knowledge; treatment of subsequently discovered inhabitants or hazards is open.
- **Q27 — Ownership begins at deployment:** Successful deployment establishes a vulnerable founding colony and its planetary claim. Establishment then produces a functioning colony with basic services. Issuing an order or merely arriving in orbit grants no ownership. Simultaneous rival attempts follow Q34 below.
- **Q34 — Competing deployments:** Rival attempts to deploy on the same unowned planet in the same turn pause and create a dispute. Neither wins because of command-processing order. Settlers and equipment remain aboard. Withdrawal, agreement or military action must resolve the competing claims before deployment proceeds. Detailed dispute lifecycle and anti-stalemate rules remain open.
- **Q37 — Growth:** Immigration is the primary means of rapidly expanding a new colony. Natural growth is slower. Basic establishment can take weeks, but a few turns of births cannot turn a small settlement into a major population centre. Rapid expansion requires additional settlers and habitat/service capacity.

### Strategic orders, supply and orbital operations

- **Q2 — Persistent strategic orders:** Colonisation and invasion proceed across weekly turns. Players select objectives, commit forces/resources and set policies. Routine progress is automatic; shortages, resistance, negotiations and other changed circumstances create reasons to intervene.
- **Q10 — Automatic supply movement:** Supply routes are managed automatically. Players provide or allocate transport capacity and protect access rather than manually dispatching every shipment. Present support as adequate, strained or cut off, with understandable reasons. Exact capacity accounting is open and must align with existing physical freight.
- **Q9 — Orbital victory and surrender demands:** Defeating orbital defences enables invasion or a surrender demand. A vulnerable government may surrender without a landing, but a determined defender can continue resisting. Orbital victory itself never transfers ownership.
- **Q18 — Stranded ground forces:** Landed troops remain if their fleet retreats or is defeated. They operate with available supplies; reinforcement, resupply and evacuation depend on access. Losing orbit neither deletes the army nor automatically reverses its gains.
- **Q21 — Planet-specific blockades:** A fleet must be assigned to blockade a planet's access. Effectiveness depends on sufficient forces, opposition and interception capability. A hostile ship elsewhere in a shared system does not automatically cut off every planet.
- **Q28 — Contested landings:** Complete orbital superiority is not an absolute landing prerequisite. Transports may attempt to reach the planet despite opposition. Enemy ships and defences may prevent deployment or inflict losses. Show known opposition and estimated risk; successful landings can coexist with unresolved fighting in space.

### Ground campaigns and bombardment

- **Q8 — Abstract planetary campaign:** Resolve ground warfare through troop strength, readiness, equipment, supply and campaign progress, rather than individually controlled ground units or a planetary tile map. Players commit forces and select an approach. Weekly reports explain progress, losses and obstacles; defenders, infrastructure and population attitudes matter.
- **Q12 — Optional bombardment:** Restrained and destructive policies trade military effects against civilian casualties, infrastructure damage and political consequences. Restraint may leave stronger defences for the landing force. Present estimated risks; neither policy guarantees precise outcomes.
- **Q13 — Changeable invasion approach:** A deliberate advance prioritises preparation and limiting losses. A rapid assault accepts greater risk to take objectives sooner. Players may change the order during the campaign. Neither approach is universally superior; deliberate action still consumes time and supplies.
- **Q23 — Recruitment and personnel:** Ground forces require recruitment from real populations, equipment, training and continuing upkeep. Use a small set of standard force types and quantities. Track military personnel separately from the civilian workforce, conserve population, account for casualties and return survivors through demobilisation.
- **Q24 — Planetary defences:** Stationed ground forces and defence infrastructure provide protection independent of visiting fleets. Fortifications resist landings and ground assaults; orbital defence installations threaten approaching ships. Neither guarantees invulnerability. Damage persists after capture.
- **Q29 — Bombardment does not occupy:** Bombardment can weaken defences and encourage surrender. Without surrender, capturing the planet requires ground forces and an administration; destruction alone does not confer control.
- **Q32 — Ground victory:** Defeat organised resistance and secure the planetary administration/key objectives. Eliminating every defending soldier is unnecessary. Collapse or surrender of organised defence begins occupation, while surviving irregular resistance may continue. Exact victory thresholds remain open.
- **Q33 — Withdrawal:** Attackers may order evacuation. Success depends on transport capacity, access and time, with possible losses under pressure. Forces unable to escape remain stranded and may eventually surrender. Cancelling an invasion never erases its consequences.
- **Q39 — Destruction scope:** Dedicated population-extermination and planet-destruction actions are outside this framework. Destructive bombardment can still cause severe civilian losses and persistent damage. Adding those dedicated actions or weapons requires a separate design decision.

### Surrender, occupation, integration and liberation

- **Q14 — Surrender is not universal acceptance:** Government surrender can end organised military resistance while leaving unrest or irregular resistance. It does not complete political integration.
- **Q19 — Standing occupation policies:** Manage occupation through a few standing approaches, such as reconciliation, security administration and harsh repression, supported by a garrison. Avoid repeated manual suppression orders. Security may restrain activity; lasting integration depends on treatment, living conditions and political history. Repression can suppress immediate activity while deepening longer-term hostility.
- **Q25 — Armed uprising:** Sustained resistance may become an uprising capable of overturning occupation. Present warning stages consistent with the occupier's knowledge. Rebels must overcome the garrison or secure its withdrawal to take control; unrest alone does not instantly transfer ownership.
- **Q30 — Conditions govern integration:** Integration depends on sustained treatment, security, living conditions, loyalties and memories rather than a fixed post-conquest countdown. Time helps only under supportive conditions. Garrison strength alone does not manufacture loyalty.
- **Q31 — Rebel political objectives:** A successful uprising can restore the former government or establish independence according to the rebels' allegiance. It does not automatically award the world to an enemy of the occupier.
- **Q36 — Liberation choices and commitments:** A victor may restore the displaced government or retain occupation, subject to existing commitments and consequences. Promising liberation and retaining the planet damages trust. Population identities, loyalties and memories survive either outcome.
- **Q38 — Partial economic recovery:** Captured worlds may provide usable output before full integration. Damage, shortages, resistance and occupation policy constrain production and recovery. Extraction policies may increase immediate receipts while worsening longer-term recovery and opposition. Use existing population/economic mechanisms for consequences rather than granting full peacetime output on capture.

### Information and pacing

- **Q40 — Honest previews:** Clearly distinguish known requirements, committed forces, costs, supply endurance and known defences from uncertain enemy strength and outcomes. Use ranges or confidence bands where intelligence is incomplete. An exact success percentage must not reveal hidden defenders.
- **Q41 — Relative timescales:** Use the agreed one-week strategic turn. Basic colony establishment generally takes several turns; defended invasions can take weeks or months; political integration generally takes much longer. Poor preparation and resistance can prolong stages. These are pacing directions, not universal fixed timers or approved numerical balance.

## Design versus current implementation

Reference baseline inspected for this chapter: `c2621f82c52b38dcfdf85bbcddf8e5d43dde459b` on main. This is a documentation-only decision record; it implements none of the new actions above.

| Existing foundation | Consequence for future implementation |
| --- | --- |
| Current ownership is authoritative at system level and colonies must match it | Planet-specific sovereignty and shared systems need an explicit contract/schema migration. Do not silently reinterpret existing system ownership. |
| Population migration conserves people and retains cohort identity/history | Extend this foundation for embarked settlers and military personnel; neither transport nor recruitment may manufacture population. |
| Habitability, shared habitat capacity, food and population effects exist | Reuse these mechanisms for establishment and survival. Initial habitat/services packages and colony deployment are additional work. |
| Physical automatic freight uses actual stock, known routes and capacity limits | Player-provided transport capacity, expedition supply, army supply and foreign access require additional rules. Do not create a second free resource-delivery system. |
| Existing hostile presence can block freight at route nodes | Planet-targeted blockades and contested access in shared systems require refinement; current broad blocking is not the agreed final design. |
| Cohort satisfaction, integration, alignment, radicalisation, memories, governance and resistance affect output | Preserve these histories through capture. Occupation, armed uprisings and ownership-changing rebellion need additional behaviour. |
| Existing per-turn economy/population rates are prototype implementation values | Rebalance against the weekly clock; existing rates are not accepted colony-growth or integration pacing. |

The exploration chapter permits multiple governments on a body. This chapter rules out externally divided planetary territories, but does not erase local political actors. Planet-wide representation, consent and succession need explicit rules before diplomacy or conquest implementation.

Fleet interception guidance also remains applicable: presence alone is not guaranteed detection, interception or combat. A blockade or landing must use actual capabilities and lawful information, not an automatic result from co-location.

Existing architecture and milestone documents remain the record of implemented behaviour. A future implementation must inspect then-current contracts and obtain its own scoped acceptance; this chapter does not authorise incidental changes to saves, versions, Simulation architecture, research or production.

## Open detailed design and tuning

These questions are deliberately unresolved. They must not be filled with undocumented implementation assumptions or treated as approved balance.

- **Settlement:** Equipment packages, technology prerequisites, habitat/service requirements, survey sufficiency, establishment work/costs and completion criteria; source-population safeguards; treatment of hidden inhabitants; evacuation and abandonment completion; remaining infrastructure and ownership after total colony loss.
- **Competition and consent:** The precise simultaneous-deployment window, dispute orders/deadlines and prevention of indefinite vetoes; planet-wide consent where several local governments exist; claim/access treaty terms and behaviour when permission expires or is revoked.
- **Supply:** Transport capacity and allocation, carried reserves, supply consumption, local support, foreign freight access, route changes and shortage/death thresholds. Shared systems need clear access and blockade resolution rules.
- **Forces:** Standard troop types, recruitment eligibility, personnel/equipment accounting, training, readiness, replenishment, garrison needs, demobilisation, surrender/captivity and evacuation losses.
- **Campaign resolution:** Landing throughput and risks; defence interactions; deliberate/rapid modifiers; key-objective and victory thresholds; surrender criteria; combat, movement, supply and ownership-change ordering within a weekly turn.
- **Bombardment:** Target policies, military effects, civilian and infrastructure damage, uncertainty, exposure and attribution of consequences, escalation and any action limits.
- **Occupation and politics:** Policy effects and economic tradeoffs; integration measures/completion criteria; uprising strength and warning thresholds; rebel allegiance, restored-government selection and post-liberation commitments.
- **Pacing and presentation:** Numerical durations/costs, weekly population rates, preview confidence, reporting detail and notification defaults. How occupied versus integrated populations qualify for victory objectives remains an explicit follow-up to the strategic AI/victory design.

## Proposed implementation and acceptance guidance

This section is **proposed engineering guidance**, not an implemented milestone or additional approved gameplay formula.

Keep authoritative validation and resolution in the portable backend. Human and AI orders should share the same rules; Unity should present detached snapshots, previews and actionable explanations. Define ownership/version changes explicitly and coordinate contracts before client implementation.

For a future scoped milestone, use scenarios to verify:

- Settling one planet does not claim its neighbours; competing same-turn expeditions do not gain an order-dependent winner.
- Deploying settlers or recruiting troops conserves population and preserves identity/history; repeat submission or save/reload does not duplicate people or equipment.
- New colonies consume actual resources and deteriorate under shortages; evacuation needs capacity and access.
- A planet-targeted blockade respects interception capability, opposition and the presence of other owners in the system.
- Contested landings can fail or suffer losses; successful landed forces persist after orbital defeat.
- Bombardment alone does not transfer ownership absent surrender; ground victory can leave irregular resistance.
- Occupation preserves surviving population, infrastructure, damage and memories; recovery and integration are conditional.
- Rebellion requires a military/control outcome before ownership changes; liberation respects the chosen political outcome and prior commitments.
- Previews and reports do not reveal hidden opposition; equivalent reordered inputs resolve deterministically.

Documentation checks for this record cover relative links, decision coverage, consistency and whitespace. Gameplay or Unity tests are unnecessary for this documentation-only change and have not been executed for it.
