# Architecture assignment — State repository, persistent saves and autosave redesign

**Status:** Proposed architecture assignment for lead architect review; **not approved, dispatched, implemented or merged**.  
**Prepared:** 2026-11-21  
**Project:** HobbyDS9  
**Scope:** Persistent campaign state, local embedded storage, save-slot management, autosave/recovery, performance and future multiplayer seams.  
**Non-goal:** Implementing full multiplayer or changing gameplay rules in this assignment.

**Dispatch update — 22 November 2026:** the owner authorized this architecture/design review through Product Desk. The [proposed ADR and measured evidence](../design/state-repository-savegame-adr.md) respond to it. The original preparation status above is historical; production persistence replacement and multiplayer activation remain unapproved.

## 1. Executive request to the lead architect

Design a replacement for the current fixed three-slot JSON save-file mechanism and a scalable state-persistence architecture for HobbyDS9. The solution must be **offline-first**, shipped inside the installed game, require **no account, cloud connection, always-online service or separately installed database server**, and scale to many named manual save slots and automatically managed recovery points.

Evaluate **embedded SQLite as the preferred local persistence candidate** (including packaging with Unity/Windows IL2CPP), but make a measured architectural decision rather than assuming that simply adding a relational database improves runtime speed. In particular, investigate how to avoid rewriting the full campaign state on every save, while preserving deterministic simulation, reproducibility, transactional consistency, restore correctness and backward compatibility with existing JSON saves.

Treat singleplayer as the default and future optional LAN/hosted multiplayer as an architectural constraint. The server/host must own the authoritative game state; a database must not become a directly shared multiplayer mutation surface.

**First deliverable:** a reviewed architecture proposal/ADR, cost and risk assessment, implementation milestones and a small instrumented proof of concept if justified. **Do not immediately replace the live persistence path or activate multiplayer.**

## 2. Inspect the current implementation before proposing changes

Read [AGENTS.md](../../AGENTS.md), [architecture.md](../architecture.md), the [design index](../design/README.md) and relevant current acceptance/version documents. Verify fresh main and any relevant accepted PRs; historical notes may lag implementation.

Concrete starting points:

- [SaveSlotStore.cs](../../client/HobbyDS9.Unity/Assets/HobbyDS9/Runtime/SaveSlotStore.cs): fixed `SlotCount = 3`; files `campaign-{slot}.json`; temporary-file write, flush and same-directory replace/move.
- [ClientEntry.cs](../../client/HobbyDS9.Unity/Assets/HobbyDS9/Runtime/ClientEntry.cs): save directory below Unity `Application.persistentDataPath/CampaignSaves`; save UI entry points.
- [CampaignHost.cs](../../client/HobbyDS9.Unity/Assets/HobbyDS9/Runtime/CampaignHost.cs): Newtonsoft JSON serialize/parse, scenario SHA-256 and validated load boundary.
- [CampaignApplication.cs](../../src/HobbyDS9.Application/CampaignApplication.cs): `StartScenario`, `LoadSave`, `ExportSave`, `ValidateSave`; the host owns I/O, the engine owns authority.
- [SimulationSession.cs](../../src/HobbyDS9.Simulation/SimulationSession.cs): one guarded authoritative `GameStateDto` per session; state staging/copying/validation and stamped command handling; full detached `ExportSave`.
- [FoundationDtos.cs](../../src/HobbyDS9.Contracts/FoundationDtos.cs): `GameStateDto`, `ScenarioDto`, `SaveEnvelopeDto`, version/identity contract. At preparation time main has SaveFormatVersion **14** and ContentFormatVersion **10**; recheck at implementation.
- [Tactical review checkpoint documentation](../tactical-review-durable-checkpoints.md) and [AtomicReviewFileStore.cs](../../prototypes/HobbyDS9.TacticalReviewStorage/AtomicReviewFileStore.cs): useful but **isolated prototype**, not evidence that campaign autosave or SQLite is already implemented.

Document the actual behavior of save/load UI, in-memory session ownership, checkpoint boundaries, existing version guards, validation/rollback, turn/combat phases and safe file replacement. Separate what is **implemented**, what is **prototyped** and what is merely **proposed**.

### Suspected bottleneck to measure, not assume

The simulation currently deep-copies entire mutable game state for command staging/commit and exports a detached save DTO. Saving to SQLite will not by itself eliminate those in-memory copies, validation costs or Unity snapshot allocations. Profile these separately from JSON serialization and disk I/O before selecting optimization targets.

## 3. Non-negotiable product requirements

1. **Offline-first:** full start/play/save/load/recover functionality without internet; never block singleplayer on authentication, cloud, an external service, connectivity or telemetry.
2. **Embedded runtime:** package storage libraries with the shipped game; use local writable user-data paths, not the installation folder; no database daemon or separate installer.
3. **Unrestricted logical slot growth:** remove the hardcoded three-slot cap. Users can create, name, rename, inspect, duplicate, load, overwrite with confirmation and delete many manual saves. Practical limits may be configurable and constrained by disk space, but must not be an arbitrary three-slot constant.
4. **Autosave:** automatic recoverable checkpoints independent of manual slots; configurable policy, clear retention and no accidental overwrite of manually saved games.
5. **Integrity:** accepted saves are complete and reloadable. Never expose a half-written checkpoint as valid; a failed save must not corrupt the last known good checkpoint.
6. **Deterministic continuity:** loading a save continues with identical game facts, RNG and allocation state, command deduplication, pending/resolved combat, turn/revision and compatible rules/content identity.
7. **Migration without data loss:** existing `campaign-1.json` through `campaign-3.json` remain loadable/importable. Keep a recovery copy; failures are explained and never silently destroy the original.
8. **Portable game core:** Contracts, Simulation and Application retain their current netstandard2.1/C#9 portability and have no direct SQLite, Unity, network or filesystem dependencies. Storage is a host/adapter concern.
9. **Future optional multiplayer:** exactly one authoritative writer per running campaign; host-originated verified changes can later be broadcast as filtered deltas. Clients never directly share/write the campaign database.
10. **No implicit gameplay redesign:** no changes to strategic outcomes, human/AI validation, faction data privacy, time/turn resolution, combat mechanics, deterministic ordering or existing accepted content without separate scope/approval.

## 4. Proposed conceptual architecture (to be reviewed)

```text
Unity UI / headless host / optional future multiplayer host
                 |
         Campaign / Save Manager
                 |
      ICampaignPersistence (host seam)
          /                 \
    JSON import/export    Embedded SQLite adapter
                              |
                  Snapshot + transaction/change log
                              |
              Local campaign database / user data

Game commands -> Application -> SimulationSession (authoritative RAM)
                      |
              Validated detached state /
              verified committed change set
```

- Keep **active authoritative game state in RAM**, with the simulation as the sole rules engine. SQLite is durable storage, not a substitute for the live state or a query-per-click backend.
- Define a narrow host-side abstraction, for example `ICampaignPersistence`, plus separately testable slot/catalog, autosave and import/export services. Exact API names and placement are architect decisions.
- Assess a **single SQLite database per campaign** with many logical checkpoints/save branches vs a database per manual slot vs a shared local database. Recommend one based on failure isolation, easy portability, rollback, recovery and UX.
- Keep scenario/content definitions separately versioned and identifiable; avoid duplicating immutable static content into every small delta when safe.
- Use a transactional snapshot + **verified state change journal** (or an alternative demonstrably correct incremental design). A log of submitted commands **alone** is not sufficient until replay across rules versions, AI behavior, resolved combat outcomes and failure cases is proven. Preserve complete authoritative post-commit information or validated checkpoints sufficient for deterministic restoration.
- Acknowledged commits must have an explicit durability contract: define when RAM is authoritative, when storage is committed, what happens on errors, and whether/when gameplay may advance despite persistence failure.
- Consider asynchronous persistence of immutable post-commit snapshots, with bounded queues and backpressure. Never read a mutable state object while it is being changed or block the UI indefinitely.
- Use SQLite transactions, foreign-key/integrity constraints where applicable, application schema migrations and a documented journal mode/backup strategy. WAL is an option, not a blanket guarantee against power failure or broken filesystems.
- Design JSON import/export as an independent compatibility feature, not a mandatory full-JSON rewrite for every autosave.

## 5. Slot, campaign and autosave semantics

Propose explicit domain identities and metadata for:

| Concept | Required semantics |
|---|---|
| Campaign | Stable unique identity, scenario/content/rules identity, creation/update timestamps, owner/host context |
| Manual save | Stable slot ID (not filename), user-facing name, optional notes, creation/update time, turn/revision, optional thumbnail/summary |
| Autosave | Automatic checkpoint with trigger/reason, timestamp, turn/revision, retention group and recovery eligibility |
| Quick save | Optional distinct convenience slot/policy, not a manual-save overwrite |
| Branch / load point | Loading an older checkpoint must not silently erase newer saves or append future actions to the wrong history |
| Save health | Available, loading, incompatible, corrupt, migrated, or recoverable statuses with actionable diagnostics |

Manual saves and autosaves must have **separate namespaces/retention**. The UI should support list, sort/filter, metadata preview, choose load, overwrite confirmation, duplicate/export, delete confirmation, storage use and a clear 'last successful save' indicator. A save limit must be based on resource constraints and tested UX—not the old fixed three slots.

Autosave policy to specify and test:

- Default safe trigger: after an **authoritatively completed** strategic turn; also evaluate phase-safe checkpoints at `AwaitingCombat`, after accepted combat-result buffering and after combat finalization so battle progress can be resumed without rerunning rules.
- Never autosave an inconsistent intermediate staging state; every checkpoint must correspond to a valid exported/validated authoritative state.
- Optional elapsed-time autosave **only if** it can safely capture a coherent state; configurable enabled/disabled and interval.
- Retention proposal: rotating newest N checkpoints per campaign plus a limited number of longer-lived milestones. Define concrete defaults with UX/product review, and apply a disk-space policy.
- Coalesce redundant triggers when unchanged; avoid unbounded autosave backlog or repeated full-world copies.
- On failure: retain previous good checkpoints, surface a non-destructive warning, retry only when safe and avoid claiming 'saved' before commit.
- On restart: present last good autosave and recovery options; never silently load a corrupt/latest checkpoint.

## 6. Compatibility, schema versioning and migration

- Keep existing strict `SaveFormatVersion`, `RulesVersion`, `ScenarioId`, `ContentHash` and deterministic RNG/resolver identities authoritative. Add a **separate storage schema version**; do not confuse database migrations with gameplay save-format migrations.
- Implement one-way **import** from legacy JSON with validation before database mutation; leave old files untouched by default and provide an explicit, tested cleanup/export path.
- Distinguish incompatible game rules/content, unsupported future format, malformed payload, corrupted database, interrupted write and insufficient disk space. No silent rewriting or guessing missing data.
- Describe supported upgrade/downgrade policy, rollback, migration backup and behavior when game content is updated after a campaign was created.
- Maintain a portable, versioned **export** route so saves are transferable, debuggable and usable by tests; include a consistent SQLite database backup/export process that handles open WAL files correctly.
- Keep the isolated tactical-review checkpoint formats separate unless their integration is expressly approved.

## 7. Multiplayer-ready boundaries without always-online

The **current assignment does not implement multiplayer**. Design persistence so later multiplayer can reuse it:

- Singleplayer: local engine + local authoritative state + embedded SQLite, no network stack required.
- LAN/peer-hosted play: one machine runs a host with one canonical session writer and local persistence; peers submit authenticated/authorized commands to that host, not SQL writes.
- Optional remote server: same engine and persistence abstraction may use a different adapter only when justified; no mandatory hosted backend.
- Stamp commands with expected turn/revision, unique IDs and deterministic ordering. Document the global-revision conflict/refresh policy when independent human players submit actions concurrently.
- Publish filtered **state deltas or snapshots** to clients, with session sequence numbers, reconnect catch-up, deduplication, authorization and fog-of-war/privacy checks. Do not expose entire authoritative state by default.
- Define host failure, disconnect and restore semantics; ownership of host save files; prevent concurrent writers opening the same campaign DB.
- Benchmark remote user-perceived latency separately from local commit latency. Do not claim that low database latency equals low multiplayer response time.

## 8. Performance and memory benchmarking (required before large refactor)

Create reproducible tests on real game paths and representative generated fixtures. Report **baseline vs candidate** with hardware, OS, runtime, world size and exact commit.

Workloads:

- Small/medium/large: proposed 100, 1,000 and 10,000 star systems (where generation/validation is feasible), increasing colonies, fleets, ships and active commands.
- Accepted player action, rejected action, `AdvanceTurn`, combat pending/buffer/finalize, `CaptureSnapshot`, `ExportSave`, manual save, incremental autosave, fresh load and restore from checkpoint+journal.
- 1, 2, 8 and 32 concurrent command producers **simulated at the host boundary**, with one authoritative writer; distinguish command arrival rate from accepted/rejected throughput.

Capture **median/p95/p99** latency by operation; allocated bytes/GC pauses, peak process RAM, full-state copy/validation time, bytes actually written, database file growth, time to restore and recovery-point age. Report disk and CPU utilization, and measure with real persistent storage/flush settings, not only in-memory SQLite.

Set measurable acceptance budgets from profiling rather than making unverified speed claims. Define a target that autosave should not produce noticeable UI stalls or alter deterministic outcomes; demonstrate improvements independently for (a) state-copy cost, (b) serialization and (c) durable storage.

## 9. Reliability and security acceptance matrix

Demonstrate, through automated tests and fault injection where feasible:

1. Hundreds of named saves, stable metadata, duplicate/save/load/rename/delete operations and a fresh campaign restart.
2. Autosave retention/rotation, manual slot protection, full disk, read-only directory, failed migration and interrupted save.
3. Process crash during or around a write; reopen and recover the newest **complete** checkpoint; no false promise of tested power-loss safety.
4. Snapshot+journal replay produces exactly the same state and known deterministic checks as uninterrupted execution, including RNG, IDs, queues, battle pending/partially resolved/finalized checkpoints and command deduplication.
5. Save from one checkpoint and branch into two distinct continuations without leaking/overwriting the other branch.
6. Existing JSON save import/export round-trip and explicit incompatible-version rejection, with the original untouched.
7. Concurrent host requests and simulated client resubmissions do not permit double application, stale writes or two active DB writers.
8. Database schema migration/reopen/backup/restore; integrity checks and meaningful errors for corrupt or tampered files.
9. SQLite distribution and actual read/write/reopen in a **Windows x64 Unity IL2CPP player**; also validate supported desktop targets and CI headless tests where feasible. Confirm native plugin packaging, licensing and AOT constraints.
10. No network access or external account needed during install/start/save/load/recovery; network-unavailable/offline acceptance test.

Protect filesystem paths from traversal, unsafe links and accidental cross-campaign deletion. Do not trust user-provided save names as paths or SQL. Avoid storing secrets in ordinary save metadata; if player-authenticated multiplayer is later introduced, keep its identity/security boundary separate from local save files.

## 10. Architecture decisions requiring explicit review

Provide a decision record with trade-offs for:

1. Embedded SQLite vs current JSON-only snapshots vs a hybrid strategy.
2. Per-campaign DB vs per-save DB vs common library DB; portability and branching.
3. Serialized full snapshots at intervals + state-change journal vs entity-level relational persistence vs command/event sourcing; prove that selected delta representation is sound.
4. Journal mode, synchronous/durability levels, writer policy, checkpointing/compaction, bounded space usage and crash recovery.
5. In-memory deep copy/validation bottlenecks and whether copy-on-write, targeted mutation plans or immutable partitions can safely reduce them.
6. Autosave triggers, cadence, retention defaults, user controls, storage quota and safe recovery UI.
7. Offline native SQLite packaging and acceptable cross-platform Unity/IL2CPP support matrix.
8. Future multiplayer replication seam and the policy for concurrent stale-revision commands.
9. Migration path, compatibility policy and phase-in/rollback strategy.

Mark every decision **agreed**, **proposed** or **open**; identify risks and evidence. Do not silently turn proposed mechanisms into project-wide rules.

## 11. Phased delivery proposal

**Phase A — architecture & baseline:** document the existing paths/assumptions, benchmark deep-copy/validation and save/load, ADR/options, failure semantics, acceptance criteria and a minimal SQLite feasibility probe.

**Phase B — isolated host persistence prototype:** test typed storage seams, transaction/checkpoint strategy, journal replay, dynamic slots and legacy JSON import in headless .NET; no production Unity activation.

**Phase C — save manager + autosave:** add scalable slot catalog, autosave retention/recovery, migration and UI specification; prove crash-safe behavior and unchanged deterministic goldens.

**Phase D — Unity integration:** package embedded SQLite into the actual supported Unity/IL2CPP player; run native acceptance, maintain JSON fallback/export and rollback plan.

**Phase E — multiplayer-ready extension:** optional host-only command/delta transport interfaces and benchmarks, only when separately approved; no multiplayer service activation required for the persistence milestone.

Each phase should have its own reviewable diff/PR, acceptance evidence and explicit go/no-go gate. Avoid breaking other contributors' branches or bypassing existing CI.

## 12. Expected handoff from the lead architect

Deliver:

- Current-state diagram and dependency inventory with observed bottlenecks.
- Target architecture diagram with component ownership, API/DTO contracts, transaction lifecycle and on-disk file layout/schema sketch.
- ADR comparing alternatives and showing which choices need product approval.
- Save-manager UX and autosave/recovery policy with concrete defaults recommended (not silently accepted).
- Compatibility/import/export, corruption handling, backup and rollback design.
- Benchmarks with baseline/target/measurements and the exact hardware/configuration; no invented performance numbers.
- Implementation backlog split into small PRs with dependencies, risks, tests, Unity/IL2CPP packaging and acceptance gates.
- Unresolved questions, including storage layout, snapshot cadence, whether state deltas are granular enough to avoid full-state copying, and future multiplayer command conflicts.

**Definition of done for this assignment:** the lead architect can approve or reject a concrete, evidence-backed plan that guarantees offline singleplayer, scalable slots and recoverable autosave **without claiming any implementation has already shipped**. Preserve all accepted gameplay, save and determinism contracts until an explicitly approved implementation migration is complete.

---

## Copy/paste prompt for the lead architect

> Review `docs/prompts/state-repository-savegame-redesign.md` against fresh HobbyDS9 main, `AGENTS.md`, architecture, contracts, Unity save paths and tactical-review prototype isolation. Produce an evidence-backed redesign proposal for an **offline-first embedded state repository and savegame subsystem**: scalable named manual slots, autosave/recovery, SQLite feasibility, incremental durable writes, versioned migration of existing JSON saves, no mandatory cloud/server, unchanged deterministic engine authority, and later optional host-authoritative multiplayer. Benchmark current full-state copying, validation and JSON I/O before choosing optimizations. Specify transaction/journal/replay semantics, schema, portable APIs, retention, native Unity IL2CPP packaging, fault-injection tests, performance targets and phased PRs. Clearly distinguish agreed/proposed/open choices. **Architecture and review only unless subsequent implementation authorization is given.**
