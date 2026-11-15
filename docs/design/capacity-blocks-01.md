# CAPACITY-BLOCKS-01 — shared capacity presentation

15 November 2026. **Agreed presentation direction; no new costs, refinement rules or simulation values.** Use one shared visual language across Engineering, Ship Design and module-stat comparisons. Read the [Engineering support boundary](engineering-ui-02-support-seam.md) and [ship-design rules](ship-design.md).

## Agreed display

Power, hull space, crew and other genuine capacity measures use segmented blocks backed by actual snapshot values. **Industry stays numeric. There is no Credit capacity meter.** This does not remove an actual numeric credit charge or implement Industry-to-credit conversion.

- A usable power budget of **60** displays **60 unit positions**; a module value of **10** occupies **10** in the same units. State whether that value is full demand, requested power or actual allocation.
- Compact Life Support space **12 → 9** displays **12 comparison positions: 9 green filled and 3 hollow**, with the exact numeric arrow and a textual reduction of 3. The 12 is the old module-space reference, not the ship's total hull capacity.
- Blocks supplement exact numbers and labels. Meaning must survive without color, tooltips or mouse input. Do not normalize different values into identical-looking arbitrary 100% bars.
- Supply and consumption have opposite benefit directions. Lower occupied space/demand can help; higher available capacity/support can help. A green improvement for one attribute does not claim the whole module is better or erase other trade-offs.

The agreed examples use one block per unit. **Proposed large-value treatment:** compact wrapping/grouping with an explicit units-per-group label and exact numeric totals when a full unit grid would be unreadable or expensive. Preserve unit-scale comparability; do not silently change scales, lose remainders or clamp overflow. The 60/10 and 12/9 examples must remain countable as specified. Rendering thresholds and detailed styling remain GUI decisions, not new balance.

## Semantic mapping

| Label / role | Actual source meaning | Benefit and comparison boundary |
| --- | --- | --- |
| Hull space available | `HullVersionSnapshot.Capacity`; compiler `CapacityLimit` | More usable capacity can help. This is not hull strength or damage. |
| Module space used | `ComponentVersionSnapshot.Capacity × Quantity`; summed into `CapacityUsed` | Less occupied space is beneficial for the same supported service. A module comparison uses its old/new space values; a fit view uses the actual hull budget. |
| Reactor output / power available | Sum of reactor supply; usable power is capped by hull distribution | More supply helps only where the profile can use it. A 72-output reactor behind 60 distribution still gives 60 usable power. |
| Power consumed | Profile/installation full, minimum, requested or allocated power, each explicitly labelled | Lower demand can help at equal service. Actual allocation is not automatically full demand or full output. Do not combine different operating profiles. |
| Crew required | Hull minimum plus installed component `CrewRequired × Quantity` | Minimum operating requirement. Less can reduce demand; it is **not Crew Support**. |
| Crew embarked | Actual draft `EmbarkedCrew` | Must meet required crew. It is the load against support, not a larger-is-always-better score. |
| Crew supported | Selected profile's **powered CrewSupport output** | Must support embarked crew. For a support-budget strip, use powered support as the capacity and embarked crew as consumption; show required crew separately. |
| Computing | Selected profile's powered ComputerSupport versus the compiler's active-component demand | Distinguish supply from demand; catalogue maximum outputs are not unconditional available capacity. |
| Industry / named materials | Actual numeric manufacture work and resource amounts | Keep exact numbers, quantities and changes. Do not turn work, stock, credits or a maturity percentage into a fictional capacity. |

Sources: [space/crew requirements](../../src/HobbyDS9.Simulation/ShipDesignCompiler.cs#L101), [power and profile allocation](../../src/HobbyDS9.Simulation/ShipDesignCompiler.cs#L142), [powered crew/computing validation](../../src/HobbyDS9.Simulation/ShipDesignCompiler.cs#L178), and [immutable module fields](../../src/HobbyDS9.Contracts/ShipDesign/ShipDesignSnapshots.cs#L168).

Benefit direction is contextual: reducing demand while losing required service can still be a poor or invalid design. Keep compiler warnings and actual output alongside the comparison. Do not present local green blocks as a new net-improvement validator.

## The actual Compact Life Support example

Inspected content: `component.utility.life-support.v1 → component.utility.life-support.v2` in the [Engineering fixture](../../scenarios/engineering-development-v1/scenario.json#L1717). Preserve those exact version identities; similarly named ready-catalogue modules are not interchangeable.

| Measure | Parent → derived | Presentation |
| --- | --- | --- |
| Module space | 12 → 9 | 12 positions, 9 green filled + 3 hollow, exact arrow and “3 less space.” |
| Crew required | 1 → 1 | Unchanged requirement; do not draw 40 crew-required blocks. |
| Powered Crew Support | 40 → 40 | Unchanged support when fully powered in the selected profile. |
| Full/minimum power | 6/6 → 6/6 in each profile | Unchanged demand; use actual allocation if showing a fitted operating profile. |
| Industry | 3 → 6 | Numeric increase; preserve this trade-off beside the space saving. |
| Catalogue credits | 2 → 4 | Numeric source fact; actual charge depends on the active construction profile below. No meter. |

The space comparison changes no stats or refinement outcomes. Where an Engineering Research card lacks a typed output/parent association, use the [proposed read-only projection seam](engineering-ui-02-support-seam.md#concrete-read-only-projection-recommendation) or an actually resolved immutable-version comparison. Do not extract numbers from effect prose, hardcode the example onto other modules or manufacture future 100–150 maturity behavior.

## Credits: intended relationship and current implementation gap

The owner reiterates the intended Industry-linked credit mechanism already recorded in [construction/rush design](engineering-tree-model-01.md#construction-costs-and-rush-conversion). Credits may accelerate eligible remaining Industry under a future explicit conversion policy; no fixed exchange rate or new payment rule is approved here. Industry remains the numeric normal manufacturing workload.

The owner-reported direct-credit construction field has an identifiable current source: the [preview provider](https://github.com/AndreBal1983/HobbyDS9/blob/b330f96b04f58bddf270393257c8630d1540c6dc/client/HobbyDS9.Unity/Assets/HobbyDS9/Runtime/FixtureShipDesignPreviewProvider.cs#L104) formats compiled credits and Industry together as construction cost, and the [designer summary](https://github.com/AndreBal1983/HobbyDS9/blob/b330f96b04f58bddf270393257c8630d1540c6dc/client/HobbyDS9.Unity/Assets/HobbyDS9/Runtime/ShipDesignerView.cs#L138) displays it. This wording does not itself distinguish payment policy.

**The implementation gap must remain visible and profile-specific:**

- **Legacy static/published construction really charges credits.** Keep that actual numeric price visible until an explicitly accepted mechanics change. See [static debit](../../src/HobbyDS9.Simulation/ProductionCommands.cs#L46) and [published debit](../../src/HobbyDS9.Simulation/CampaignBlueprints.cs#L129).
- **Normal streamed Cycle construction uses Industry/materials without a credit debit.** A compiled credit valuation is not its construction invoice. Qualify any retained value accordingly; do not label it an actual payment. See [streamed enqueue](../../src/HobbyDS9.Simulation/CampaignTacticalReviewSession.ColonisationProduction.cs#L26) and the [accepted profile contract](../campaign-activities-streamed-v1.md#normal-queue-and-physical-inputs).
- **Industry-linked rush remains future mechanics.** A visual component cannot derive a new price, remove a real charge, reinterpret an old save/refund, or make the future conversion executable.

“No Credit meter” is a presentation rule, not permission to hide payment. If the UI lacks reliable active-profile charging semantics, retain the known numeric source value with an explicit qualification rather than guessing zero or asserting a conversion.

## Shared component recommendation and receiving checks

**Proposed presentation input:** field key/label, quantity, value role (supply, consumption, required, embarked or supported), optional actual budget, parent/current values, selected profile, unit scale, known/unavailable state and compiler issue references. This is a reusable view input, not a new save or simulation DTO. Render from one authoritative snapshot; Unity does not recompute legality or invent costs.

For comparisons, keep a common reference scale. Use the parent extent for 12→9; if a new value exceeds the old reference, extend the visible extent or show explicit overflow instead of clipping. Hollow positions must say whether they represent freed module space or unused fit budget. Update on snapshot/selection changes; no new continuous simulation or polling loop is needed.

Receiving should verify exact counts/numbers; supply-versus-demand direction; CrewRequired/EmbarkedCrew/CrewSupport separation; profile power and distribution caps; unchanged quantities; trade-offs; visible payment semantics; overflow/grouping and keyboard-readable labels. All three surfaces should use the same field meanings. Unknown data stays unavailable rather than zero.

Source inspection at `b330f96b04f58bddf270393257c8630d1540c6dc`; this document is not a screenshot, GUI implementation or native acceptance. The current GUI receiving sequence remains under GUI/QA ownership, after the Sol/Engineering/map/popup batch. No standalone build or new cost/refinement mechanics is authorized by the block presentation.
