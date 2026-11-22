# State repository implementation contract

22 November 2026. **Agreed execution contract for authorized phases; implementation and native acceptance are reported separately.** Read the [owner authorization](state-repository-implementation-authorization-20261122.md), [ADR](state-repository-savegame-adr.md) and [baseline measurements](state-repository-measurements-20261122.md). The earlier architecture-only hold is superseded for A2/B1/B2/C1/C2/D1. D2 default activation is a separate rollout decision. B3 chunk storage, E1 engine changes and E2 multiplayer are not part of this execution.

## Source and authority pins

- Reviewed design: `55a0c67b1a96d965c6a9c0214dc7f60d85743efe`, PR132. Owner authorization source: `0fb1405` on `codex/state-repository-implementation-brief`.
- Portable host-library development may start on that design ancestry. The integrated player must start from `2fc0dbca46073121379dd9491c859a02a6f22dcd` or an explicitly reviewed descendant: outer Format18/package5, retaining refinement17 and belt state. Newer main's foundational save14/content10 is not a replacement player baseline.
- QA alone owns Unity/editor/native execution and schedules it around current UX acceptance. Keep the tested player, its user-data location and owner JSON files unchanged. All repository/probe tests use generated fixtures and separate destinations.
- Work in isolated worktrees and reviewable commits. No main merge, default switch, existing-save rewrite or production cleanup. Batch pushes/checks; do not run full suites repeatedly for documentation changes.
- Shared account use at dispatch: 35%; checkpoint 72%, hard stop 75%, retain 25% reserve. Check before sustained work and handoff; no reset or paid-credit bypass. At the checkpoint, seal evidence and report remaining gates before starting another expensive phase.

## Disjoint ownership

| Owner | Owned paths and responsibilities | Integration boundary |
| --- | --- | --- |
| Architecture | This contract, ADR/status/default integration, go/no-go review and receiving dependency record | Does not duplicate repository or GUI implementation. |
| Data & Persistence | New `src/HobbyDS9.Persistence/` provider-free contracts; `src/HobbyDS9.Persistence.Sqlite/`; dedicated persistence tests and repository documentation | Owns concrete repository request/result types, schema, file lease, transactions, receipts, bounded reads, import/backup and later retention. Communicate exact solution/workflow edits before making them. |
| Backend | New `PhysicalReviewHost.RepositoryCapture.cs`, neutral carrier/stamp types and focused adapter/equivalence tests in the existing review-host area | Owns exact edition encode/decode/load/replay, safe capture/restore and session epoch. Identify any necessary existing host-file edit before touching it. No SQL/provider reference in the portable game. |
| Unity & GUI | New `Assets/HobbyDS9/StateRepositoryProbe/{Runtime,Editor}` assemblies, `prototypes/StateRepositoryUnityProbe/`, package/import/linker/license recipe, `docs/state-repository-a2-unity.md`; later standalone `Review/Runtime/SaveLibrary*` presentation files/tests | Probe define `HOBBYDS9_STATE_REPOSITORY_PROBE` stays off by default. Existing shared model/UI changes require explicit handoff. Never run Unity/native concurrently with QA. |
| Integration & QA | Isolated receiving tree/builds, native test execution, evidence and owner-save baseline | Combines exact reviewed commits; reports compile/native/gameplay gates independently. No changes to sealed UX players. |

Source ownership is not a request for another owner approval. The named implementers coordinate routine file handoffs directly; Architecture resolves conflicting edits. New provider-free contracts can be consumed by host adapters and UI, but Simulation/Contracts/Application must not acquire SQLite, filesystem or Unity dependencies.

## A2/B1 provider trial freeze

Use one explicit package family for the initial trial:

| Package | Exact trial version | Purpose |
| --- | --- | --- |
| `Microsoft.Data.Sqlite.Core` | `10.0.14` | ADO.NET facade, use its compatible .NET Standard asset. |
| `SQLitePCLRaw.core` | `2.1.13` | Managed interop core. |
| `SQLitePCLRaw.provider.e_sqlite3` | `2.1.13` | Static `e_sqlite3` import provider. |
| `SQLitePCLRaw.lib.e_sqlite3` | `2.1.13` | Bundled native engine; select intended Windows x64 artifact. |

This graph is a **trial pin, not native/provider acceptance**. The [facade package](https://www.nuget.org/packages/Microsoft.Data.Sqlite.Core/10.0.14) declares a .NET Standard asset and a compatible Raw lower bound. The [Raw provider](https://www.nuget.org/packages/SQLitePCLRaw.provider.e_sqlite3/2.1.13), [core](https://www.nuget.org/packages/SQLitePCLRaw.core/2.1.13) and [native package](https://www.nuget.org/packages/SQLitePCLRaw.lib.e_sqlite3/2.1.13) must resolve together. Do not combine this recipe with a bundle initializer, Raw3.x, SourceGear3.x packaging advice or an operating-system SQLite DLL. The previous [provider survey](../evidence/state-repository-20261122/storage-feasibility.md) remains historical research, not a competing package selection.

Data locks and reports the complete transitive graph; GUI imports the same managed/native artifacts. Record NuGet content hashes, actual assembly names/versions/framework assets, native SHA256, `sqlite_version()`, `sqlite_source_id()` and compile options. Inspect exact artifact notices and relevant published advisories before accepting distribution. No dependency is fetched at game runtime.

Explicit host bootstrap calls `SQLitePCL.raw.SetProvider(new SQLitePCL.SQLite3Provider_e_sqlite3())` exactly once before connections. Do not reinitialize behind another live provider. Tests must isolate bootstrap consistently. Keep bundle/Batteries assemblies out: the Microsoft initializer can discover them through reflection. No ORM, custom SQL functions/collations or native callbacks in the first slice. If facade/AOT integration fails, Architecture can approve a bounded direct-Raw experiment behind the same interface; do not silently switch the runtime engine or weaken validation. A native binary failure requires a separate binary recipe; removing the facade does not fix it.

Both host contract/adapter libraries target netstandard2.1/C#9; test executables may use the repository's pinned .NET SDK. Unity imports require actual dependency-assembly inspection: do not copy NuGet `System.*`/BCL assemblies wholesale over Unity's supplied profile. Raw core declares `System.Memory >=4.5.3`; lock the resolved dependencies and reconcile actual assembly identities. A net10 test can select the facade's net8 or provider's net6 asset, so report which managed binaries were exercised and add a bounded smoke/compile harness using the exact Unity-target assets. Use explicit plugin platform/CPU flags and stripping/linker rules. Preserve source `ProjectVersion.txt` while separately recording the actual installed acceptance editor (currently 6000.6.9f1). A desktop .NET pass does not clear IL2CPP. The Raw core/provider/native NuGet packages declare Apache-2.0; retain their notices as well as the underlying SQLite engine's public-domain notice and Microsoft's MIT license.

## Host boundary v1

Data owns concrete type/member names and publishes them to Backend/GUI before integration. The following semantic contract is frozen; routine naming changes do not require another product decision.

| Value / operation | Required contract |
| --- | --- |
| Complete captured image | Owned exact encoded bytes, byte count/hash, complete immutable edition descriptor, source turn/revision/phase. Clone mutable inputs or transfer exclusive ownership explicitly; never retain a caller-mutable array. |
| Edition descriptor | Carrier kind and outer format, inner format, package format/profile/hash, scenario/content/rules/resolver/RNG and codec identities as supported by the actual carrier. Missing/incompatible identities reject; scalar18 alone is insufficient. |
| Host capture token | Source session epoch and work token, independently from gameplay revision and repository sequence. New/Load changes epoch; a rejected load leaves current authority intact. This token fences host publication and is not a new gameplay field. |
| Commit request | Caller operation UUID, complete request fingerprint, campaign/branch/checkpoint identities, expected head/generation, source stamp, payload identity, trigger and intended reference updates with expected generations. IDs/timestamps are host metadata, never gameplay RNG. |
| Commit result / lookup | Durable receipt identifying committed checkpoint, branch sequence/generation, references and source stamp. Lookup resolves committed / confirmed absent / outcome unknown. Identical retry returns stored receipt before checking now-stale CAS; different request under the same ID rejects. |
| Read/list | Stable bounded pagination, bounded payload reads, hash/length/descriptor checks, stable error categories. Listing metadata never claims game-load validity or runs hundreds of replays. |
| Restore/import | Exact Backend codec/loader validates the complete edition, metadata agreement and reexport equality; replacement authority publishes only after validation and epoch guards. Storage integrity alone is insufficient. |

The repository stores opaque complete roots; game-state validity belongs to Backend's typed adapter. A provider-free image can be mapped to Data's request in a host integration layer. Neither the database nor a UI model invents omitted refinement/belt/battle facts. Keep supported foundational14, outer17 and outer18 loaders distinct; unsupported carriers fail explicitly and remain usable in their existing player.

Initial Data API names are `CheckpointImage(payload, editionIdentity, turn, revision, phase)`, `CheckpointCommit`, `ReferenceUpdate`, `DurableReceipt` and `ICampaignRepository`; `ICheckpointValidator.Validate` supplies the typed validation seam. `editionIdentity` uses Backend's bounded canonical fixed-order length-prefixed descriptor, not a second incompatible encoding. Image bytes are capped at 64 MiB, identity at 16,384 characters and phase at 64. Repository operations include commit, fork, set/delete reference, operation lookup, branch lookup, list and bounded read. Exact compiled API/source pins are supplied by Data before integration. A failed lookup/open is an unknown outcome, never proof that an operation was absent.

Request fingerprints must cover every semantic input, including payload bytes/hash, descriptor, target references/names, branch parent, expected generations and source metadata. Use an unambiguous length-delimited canonical representation; do not concatenate free strings or trust a caller-supplied hash without recomputation. Lookup receipts only in the correct campaign/operation scope. Receipt retention cannot make an old successful operation look safe to apply again without an explicit expired-operation outcome.

One lifetime OS file-handle lease owns each campaign writer; SQLite's transaction lock alone is insufficient. The SQLite implementation serializes access and uses foreign keys, local-filesystem WAL and `synchronous=FULL`, with a short transaction for payload+head+refs+receipt. No replay, modal dialog or capture occurs inside that transaction. Reopen fails closed on unknown schema/identity. Never copy an active main database file alone as a backup.

## Capture and scheduling contract

Backend captures only under existing session ownership after outer adoption at a stable accepted boundary. Preserve full export, codec copy and complete load/replay validation initially. No deep-copy removal, gameplay migration or partial carrier optimization is authorized by this storage phase.

C2 admits one in-flight complete image and at most one coalesced autosave intent holding a stamp/reason. Manual Save waits cancellably for admission; after admission it captures the then-current validated state, rather than falsely promising the earlier displayed frame. Manual images cannot be replaced by autosave. A late receipt may remain valid on its original branch but cannot mark a replacement session saved. Following an ambiguous write, resolve the receipt and durable head before admitting new work.

Agreed defaults are ten rolling autosaves plus three milestones, a 512 MiB soft reclaimable-auto budget and protected manual/kept-branch references. Completed turn, AwaitingCombat and accepted buffered-result/finalization boundaries trigger coalesced autosave. The ADR's proposed milestone cadence (one each ten completed turns) and optional timer (off initially; five-minute minimum if offered, changed stable state) are the initial implementation settings for testing; they remain tunable details, not measured recovery-loss targets. Quick Save is deferred. Continue singleplayer after autosave failure with a persistent last-durable-state warning. Manual Save failure is explicit; Save-and-Quit retains the session until success or explicit Quit without saving. Branch an older continuation, preserve independent futures and apply the ADR's visible inactive-branch expiry rules.

Retention counts and safety invariants are agreed; no test may meet a count by discarding the last good recovery point, protected manual checkpoint, quarantined evidence or a live restore dependency. Full-root ancestry metadata must not accidentally pin every old payload forever. B1 may omit pruning, but C2/D1 cannot claim retention acceptance until full-payload collection and all pins are tested. Optional chunk storage is not required to implement full-payload collection.

## Bounded phase gates

| Gate | Evidence required to pass | Work permitted while pending / after pass |
| --- | --- | --- |
| A2 package resolution | Locked exact graph, asset/native hashes, licenses/advisories, bootstrap, BCL import reconciliation and headless provider open/transaction/backup proof | B1 and typed capture tests may run in parallel. This is not native acceptance. |
| B1 repository | Focused real-disk tests for immutable roots, descriptor/hash/size bounds, branch/ref CAS rollback, identical/mismatched retry, committed receipt lookup after lost response, two writer processes, abrupt pre/post-commit exit and clean reopen | B2 and C1/C2 integration may proceed against the tested API; no shipped default switch. |
| A2 native | Exact Windows x64 IL2CPP build; offline launch/create/commit/rollback/cold reopen/consistent backup+restore, non-ASCII path and actual engine/options; no developer-machine DLL dependency | Clears provider packaging for opt-in gameplay integration, not complete game restore. |
| B2 compatibility | Typed exact edition restore and deterministic next actions; independent branch futures; bounded named refs; validated idempotent import with separate destination/recovery copy; originals' before/after hashes; export reopen; backup/migration failure preservation | C1/C2 can use supported carrier set only. Unsupported carrier remains explicit, not silently converted. |
| C1/C2 host/UI | Generation-safe confirm dialogs; stable pagination/names; epoch late-callback guards; queue admission/coalescing/retention; approved trigger/failure matrix; Save-and-Quit keeps session on failure | May assemble the controlled opt-in D1 candidate after A2 native and B1/B2 pass. |
| D1 receiving | Independent exact-commit integration on current player ancestry; offline new/save/load/recovery/quit/cold reopen; fault matrix; complete Format18/package5 continuation; verified JSON fallback; 1920x1080 review; native CPU/allocation/frame/disk results with stated workload limits | Deliver a concrete rollout candidate. Open data-loss/compatibility/privacy failures block D2. |
| D2 default activation | Independent receiving review and measured workload targets accepted, then separate owner rollout decision | Only this clears default storage activation. |

Performance measurements must separate capture, validation/replay, serialization, durable I/O, foreground frames and live/allocated memory. Use generated 30/100-system baselines and at least representative progressed/combat/refinement/belt cases; report supported limits and small-sample uncertainty. The existing 1,000-system `UnsupportedSource` advance and 10,000-system generator rejection are known limits, not problems to fix within this storage slice. Do not invent a target or accept a regression because SQLite commits quickly. Agree native workload budgets from measured comparable paths before D1 acceptance/D2 recommendation.

For crash tests, kill only a purpose-launched child identified by its exact process/test marker. Do not terminate the QA player or Unity. Before/post-commit process failure is not power-loss proof. Disk-full, read-only, corruption, missing native binary and unsupported schema must preserve the last durable save and current running session where applicable; each result says whether injection or actual OS behavior was exercised.

## Handoff and current state

At this contract's initial issue, source ownership and the trial graph are frozen and implementation is dispatched. No B1, A2 native or D1 result is asserted. Each owner supplies exact commit/base, changed paths, dependencies, test commands/results, untouched-save evidence, known limits and next gate. Architecture records gate decisions from that evidence, sends meaningful milestones to Product Desk and continues authorized dependent phases when they pass. Do not replace unpassed gates with another design-only review or repeat permission requests to begin approved implementation.
