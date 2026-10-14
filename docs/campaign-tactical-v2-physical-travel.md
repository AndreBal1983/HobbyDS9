# Campaign Tactical v2 — physical travel review increment

Status: **proposed, separately gated headless review**. This increment is stacked on [the B1 core](campaign-tactical-v2-stage-b-core.md), preserving PRs #19, #20 and #22. It does not activate the production campaign, bind a Unity presenter, or settle the remaining owner decisions. It follows the owner's backend assignment; see [architecture](architecture.md), [movement design](design/time-movement-mobilisation.md) and [tactical design](design/tactical-combat.md).

## Implemented authority

`CampaignTacticalTravelApplication` owns a real Scenario/Simulation session. Economy, research, production and the shared strategic AI command path run before movement. Movement executes exactly once with actual compiled ship movement budgets (slowest ship), origin penalties, weighted connections, natural phenomena, active traversal effects and locked partial-edge progress. No prototype itinerary is imported, and the old movement/detection path is not executed and subsequently rewound.

Time is a normalized exact `BigInteger` rational fraction of the strategic turn. The profile freezes these **proposed** policies:

| Boundary | Review behavior |
| --- | --- |
| Shared turn start | Outgoing fleets depart before renewed stationary contact. |
| Other shared times | All arrivals, then lawful contact, then departures. No ordinal first mover. |
| Edge interiors | No sector occupancy or teleporting combat at opposite crossings. |
| Later arrivals | Lawfully connected arrivals join/merge pending components before tactical allocation. Existing participants remain indivisible. |
| Unrelated traffic | Continues its itinerary; only admitted participants stop and lose executable remaining routes. |
| Encounter membership | One encounter per fleet per turn. Disconnected hostile components at a single node may have separate encounters. |

Hostility, initial contact knowledge, tracking coverage, freshness and travel intentions are explicitly authored. Co-location alone creates no battle. `Stay`, `StayEngage`, `Leave` and `Follow` are stamped, owned commands. Follow takes an explicit route through the actor's known topology and a freshness stamp for a known contact; it does not inspect a hidden destination, future route or current hidden target existence. Waiting follow is legal only inside the declared lawful tracking grid. There is no chase probability.

After atomic aftermath, participants start the next turn with cleared routes and `Stay`. They may leave, engage again or follow anew. Reengagement allocates a new battle from retained hull, sector shields and weapon ammunition, without refill. The three-round, cloak, simultaneous commitment, scoped report and deterministic tactical contracts from B1 remain. `FocusFallback` now includes a constant safe explanation about a visible hostile alternative.

## Persistence and facade

The physical composite uses package/save format **2**, profile `campaign-tactical-v2-physical-review-1`, movement resolver `strategic-travel.exact-review`. It contains the unchanged B1 save-format-1 core, plus its own phase, frozen requests, intentions, contacts, receipts and pre-movement proof. The nested legacy Save Format **14** carrier remains Planning with no legacy encounters; only the physical root is authoritative for its pending phase. Persist and reload the complete physical root, never its nested core alone.

Content Format **11**, `science-engineering-v1`, `ship-design-compiler-v1` and the B1 tactical resolver retain their identifiers. There is no implicit migration or default activation. A host-owned save contains private data; rendering uses immutable actor-scoped views, never the save carrier. The physical facade adds known contacts/coverage, owned orders/receipts and known-route queries, without exposing a global strategic snapshot or enemy reserves.

Reload validates frozen ship ownership/membership/hardware and replays movement from its saved authoritative input. It checks exact times, costs, positions, partial progress, encounter allocation/seeds and observed contact freshness. Tactical round replay remains authoritative for resource/history validation. Strategic advance and aftermath both stage the complete root and validate before committing; phase faults or unsupported admission roll back earlier economy/research/resources/RNG/commands as well.

## Authored proof and acceptance

Run `python scripts/check-campaign-physical-travel.py` after a Release build, or `dotnet run --project src/HobbyDS9.Headless -c Release --no-build --no-restore -- --review-physical-travel`.

The two [physical source profiles](../scenarios/tactical-v2-physical-review/README.md) are explicit equipment-bound campaign sources. The faster example meets at Bajor at **3/4** of the turn and stops both fleets before later route legs. The slower example misses, preserving own **2/3** and departing fleet **1/2** weighted-edge progress. The faster example resolves two real battles with own torpedo stock **7 → 3 → 0**. Canonical, reversed commitments, repeated, round-boundary reload and reordered authored lists agree:

| Profile | Final physical save SHA-256 |
| --- | --- |
| Faster | `a6a2522a4f389b0bb8aa562d6273b0f02b3ea1fdeb57c7fe21d8368e4fa4d177` |
| Slower | `77073cc154b78dbca2e5f6fa1a618cae490d11410f51f4cb2bb9e640a33210c4` |

The increment adds **49** tests to the **1,621** retained baseline. Coverage includes faster/slower, mutual departure, waiting known contact, opposite crossing, intermediate stop, unequal budgets, weighted/locked progress, traversal effects, later joining/unrelated arrivals, disconnected components, input/culture permutations, stale/foreign commands, forged saves/knowledge, whole-turn/finalization rollback, next-turn choices, retained stocks and AI-only rounds. Local locked restore/Release build, full tests, PrepareUnity and static Unity checks pass. Equipment, inspection, playground, Stage A, B1 and physical headless checks pass. Exact-head Linux/Windows/macOS CI is required on the draft branch; its PR/check runs provide the final evidence.

Preserved legacy 25-turn save SHA is `fb2b75f18c8c5423f3a0850f38f6e53ee8249d3d08385978b5b2532041cc6089`; gameplay RNG is `d8919406d6e01cf1`. B1 two-battle save remains `29110377df8d8ffa3e2f4d2cfdf40392671fb8b87d82d1955ec29def827cf199`. No old golden is updated to accommodate this increment.

## Explicit gates and limitations

- **Connected owners who are not all mutually hostile are unsupported.** The current tactical core equates civilization owner with tactical side. Admission atomically rejects the whole turn rather than creating neutral/friendly fire. A concrete later alternative is a separately versioned owner-to-side partition plus side hostility/target validation, agreed alliance admission and corresponding round replay tests. The actual three-owner rejection is tested.
- Tracking grids and freshness are fixed authored review inputs, not a newly implemented dynamic sensor/fog-of-war system. General visibility precision, final mobility weights/thresholds, cloak cost and all boundary policies remain proposed pending owner acceptance.
- Provisioning new ships into this equipment-bound review remains gated: production of an unbound ship rejects atomically, as in B1. General mixed legacy/v2 activation, invasion, withdrawal exits, retreat pursuit and production UI binding remain outside this increment.
- No Unity Editor, native player or desktop acceptance is claimed. Presenter preparation stays separately pinned to B1; this branch does not import or modify that client's work.
