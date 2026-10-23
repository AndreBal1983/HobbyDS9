# Lawful generated-campaign acceptance walkthrough

23 October 2026. Receiving aid for frontend acceptance, not a second simulation or scripted AI. Actual portable driver: `GeneratedCampaignTests.Explore` in [the backend tests](../tests/HobbyDS9.Simulation.Tests/GeneratedCampaignTests.cs). It submits commands to `ICampaignGeneratedSession`; it never reads the trusted full map, hidden homes or enemy orders to choose human actions.

## Verified 250-system setup

Use the existing exact playable-fleet template reader and `CampaignGeneratedApplication.StartGame(template, settings)` with seed42,250systems,250empty regions,2,500,000x2,000,000, quiet target2000, default Natural isolated share100/spread1000, homes `civilization.federation` and `civilization.cardassian`. This uses the shipped existing classes/resources/technology; no weak-opponent fixture is used for the250 combat acceptance.

The first verified actual run reached a human encounter on **turn70**, after69 human visited endpoints and138 known regions. The accepted transcript had140 operations at AwaitingCombat. Three simultaneous tactical rounds and finalization retained four actual surviving ships. Whole saved-root equality was checked after AwaitingCombat load, every round, finalization and a fresh aftermath load. The optimized known-map target search retained this same result and reduced this test from79seconds to approximately12seconds on this machine. These timings are diagnostics, not UI timing guarantees.

## Human planning step

1. Read own fleet snapshots, `CaptureGeneratedMap(human)`, lawful contacts and current command stamps. Retain visited human endpoints outside authority only as acceptance-driver planning memory.
2. For a stationed own fleet with no current route, build a local BFS from **only the returned known polygons and known connections**. All generated ordinary crossings cost1. Choose the nearest unvisited known target; break target ties by ordinal node ID. Ask authoritative `FindKnownRoute` for the selected target. Do not search the full review map or derive an enemy home from generation metadata.
3. Prefer a currently fresh observed contact that has a successful authoritative known route, ordered by route cost then ordinal observed fleet ID. Submit Follow with the contact's exact observed turn and returned route. A zero-step Follow at the observed location expresses engagement intent. If there is no available exploration target/contact, submit StayEngage. Keep existing partial transit/active routes intact.
4. Submit through `SubmitTravelOrder` with current turn/revision and a new command ID; only then advance the real campaign. Repeat up to128 planning turns for this fixed250 acceptance. If a gate rejects, retain evidence and diagnose rather than moving hidden enemies, giving free knowledge or scripting AI orders.
5. At AwaitingCombat, use actor observations/standing orders/legal focus IDs to commit actual rounds. The portable receipt used Hold and the first legal visible focus; ordinary AI independently committed its own lawful orders. Save/reload before rounds and after finalization through the complete generated envelope.

Frontend may adapt these steps into controls/automation against the actual session. It must not read the backend test's trusted exports to make player choices or use fixed `nav.bajor`/`nav.cardassia` routes. The distinct destroyed-contact regression uses a deliberately damaged no-yard opponent fixture; it proves cleanup, not ordinary demo balance or the250 combat result.

## Other verified boundaries

A real route longer than the existing6-crossing budget was submitted using only known geography after20 exploration turns. Its remaining route, frozen geometry and next turn matched after whole-root reload. Ordinary generated unit edges with integer budgets do not naturally produce a partial edge; the existing weighted physical profile remains responsible for those historical regressions. A separate coverage component test verifies that a departed partial-edge fleet's retained origin cannot grant current observation or fresh contact.

Current local observation is deliberately limited to own colonies and stationary own fleet nodes; old tracks outside coverage remain stale. A covered no-detection clears an obsolete track without proving absence of cloaked ships. Unbound local nebula footprints remain excluded until a separate lawful spatial-observation contract exists. Macro decorative art cannot expose hidden systems or local features.
