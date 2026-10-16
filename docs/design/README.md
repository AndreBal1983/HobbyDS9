# Gameplay design decisions

Decision date: 12 October 2026.
Colonisation and conquest decisions added: 16 October 2026.

Markdown in Git is the authoritative source for the migrated decisions below. [The Google gameplay design document](https://docs.google.com/document/d/1-Z8K30kPxawcEbZKhNvxrmswrq7h7soADP7plu95_WA) remains the legacy source for other gameplay chapters; this is a scoped migration, not a complete replacement of that document.

## Chapters

| Chapter | Scope | Status |
| --- | --- | --- |
| [Strategic AI](strategic-ai.md) | Persistent goals, coordinated plans, editable faction personalities, five fair difficulties, worked diplomacy plan | Direction agreed; detailed tuning proposed |
| [Ship design](ship-design.md) | Shared capacity and mounting limits, dependencies, power profiles, engineered versions, refits and AI design | Shared capacity/mounting and hull refinement agreed; detailed rules proposed |
| [Exploration and procedural missions](exploration-missions.md) | System/body surveys, first contact, mission director, shared consequences, expedition repair dilemmas | Direction agreed; infrastructure and tuning proposed |
| [Time, movement and mobilisation](time-movement-mobilisation.md) | Weekly clock, sector travel, expediting, buyout, sensor warning and response | Direction agreed; speeds, prices and detailed timing proposed |
| [Fleet interception and tactical combat](tactical-combat.md) | Fleet posture, detection/tracking/interception, command hierarchy, abstract maneuver, battle orders, targeting and combat objectives | Direction agreed; formulas and balance proposed/open |
| [Colonisation and planetary conquest](colonisation-and-conquest.md) | Planetary ownership, settlement, supplies, invasion, bombardment, occupation, rebellion and liberation | First-pass gameplay framework agreed; detailed rules and tuning open |

## Decisions adopted

- Keep faction abilities, personality and difficulty independent; canonical factions are editable presets.
- Give AI persistent victory goals and milestones. Turn utility scoring executes the plan.
- Provide five difficulties through better planning/search rather than resources, rule exceptions or hidden information.
- Assemble classes from a shared internal capacity budget with explicit mounting and compatibility constraints.
- Spend existing Engineering capacity to develop and refine hull/component versions; preserve diminishing returns and scientific prerequisites.
- Integrate the ship designer with the strategic planner: build for mission, timing, affordability and logistics.
- Use one week per strategic turn; express movement in sector crossings, separately from warp factors.
- Allow credits to expedite eligible work and ship construction while respecting physical and social constraints.
- Treat sensor infrastructure as an investment in warning time; balance warning, travel and mobilisation together.
- Use no hard fleet-size cap; additional ships always add baseline capability, while command hierarchy and technology add positive coordination bonuses.
- Keep detection, tracking and interception separate; fleet co-location or path crossing does not automatically force combat.
- Resolve tactical combat through abstract formation-level relative geometry rather than true authoritative 3D coordinates.
- Let ship design and fleet composition create capabilities, and let round-by-round maneuver plus targeting orders decide how those capabilities are used.
- Keep tactical orders persistent unless changed and commit opposing round orders simultaneously from lawful information.
- Allow combat objectives such as breakthrough, hold, escort, delay, raid, pursuit and disengagement rather than requiring universal destruction of one side.
- Give each planet one controlling empire or government; allow different owners of different planets within a system.
- Establish colonies through surveyed destinations, transported settlers and equipment, finite supplies and continuing support; retain reusable colony transports.
- Separate orbital control, ground control and political integration; resolve invasions through persistent strategic orders and abstract ground campaigns.
- Preserve population identity and political history through occupation, rebellion and liberation; integration depends on sustained conditions.

The power-profile rules, version/refit details, schemas, example statistics and implementation stages are proposals, not additional user-approved balance values.

Exploration/first-contact decisions and the proposed mission infrastructure are recorded in [exploration-missions.md](exploration-missions.md). Persistent missions, survey/repair integration, diplomacy adapters and consequence schemas require scoped implementation; documenting them does not establish runtime support.

Fleet/interception/combat decisions are recorded in [tactical-combat.md](tactical-combat.md). Its agreed high-level direction supersedes conflicting older GDD assumptions about fixed Long → Medium → Close combat, one-time pre-battle doctrine, hard fleet-size limits, automatic combat from simple co-location, or Unity/3D geometry determining the authoritative result. Detailed maneuver formulas, weapon envelopes and tuning remain proposed/open.

Colonisation/conquest decisions are recorded in [colonisation-and-conquest.md](colonisation-and-conquest.md). Planet-specific sovereignty, targeted blockades, colony expeditions, ground campaigns and occupation require scoped implementation. Existing system-level ownership and broad freight-blocking behaviour must not be silently reinterpreted; the chapter records those gaps and keeps detailed rules and balance open.

## Design versus implementation

These chapters describe intended gameplay. [Architecture](../architecture.md) and milestone/acceptance documents describe current implementation. Do not relabel a proposal as implemented merely because it is documented.

The current Science & Engineering foundation deliberately has one Engineering target, maturity capped at 100%, fixed ship designs and simple AI. The proposed concurrent Engineering portfolio, overmaturity, modular classes and hierarchical AI require future scoped implementation and acceptance. Preserve current behavior until that work is explicitly undertaken.

For Science/Engineering concepts not migrated here, read the legacy document alongside [current implementation rules](../science-engineering-v1.md). Cross-references to Chapters 3–7 in the migrated text refer to that legacy document.

## Working rules

1. Read the relevant design chapter before implementation.
2. Preserve **agreed**, **proposed** and **open** distinctions.
3. Report conflicts between intended design and current contracts before changing those contracts.
4. Make narrow, deterministic implementation increments; update acceptance and versioning separately.
5. Keep design decisions in Git; avoid competing edits to migrated sections in the Google Doc.
