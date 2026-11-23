# State repository measurements and acceptance plan

22 November 2026. **Diagnostic evidence for the [proposed ADR](state-repository-savegame-adr.md), not production performance acceptance.** This bounded experiment was justified because the assignment's main risk is optimizing disk storage while most work occurs before I/O. No owner save, production source, current QA player or gameplay limit was changed.

## Reproducible sources and environment

- Fresh-main/design base: `f5ebc86772f38e9c55aad9ec9e35dc368906de03`; [pinned inventory](../evidence/state-repository-20261122/inventory.md).
- Runtime code/resources: detached clean `2fc0dbca46073121379dd9491c859a02a6f22dcd`, exact Format18/package5 profile `generated-belt-bodies-v1`; package hash `2e1753666e957db7d85f0f16faa865014d0dc80b64542aeb47d009b9ae12994e`. This is the measured receiving source, not current main's older base-player path.
- AMD Ryzen 7 5800HS, 16 logical processors, 16,541,487,104 bytes reported RAM; Windows build10.0.26200 x64. Local C: fixed NTFS, Intel SSDPEKNU512GZ NVMe. Host load, cache state and power mode were not controlled.
- Runtime probe: pinned .NET SDK10.0.401, measured runtime .NET10.0.12, Release, workstation GC/Interactive latency. It is a host console, not a Unity frame or IL2CPP measurement.
- Storage probe: Node24.22.3 built-in SQLite3.54.1, real disk, WAL/FULL, foreign keys enabled. This engine version is not an approved production provider/native build.
- [Runtime harness and instructions](../../prototypes/StateRepositoryProbe/runtime/README.md), [storage harness](../../prototypes/StateRepositoryProbe/storage/storage-probe.mjs), [provider/SQLite feasibility and official sources](../evidence/state-repository-20261122/storage-feasibility.md). Generated save blobs, databases and build products remain ignored and are not published.

Runtime records preserve every timed sample, current-thread managed allocation, collection counts, cumulative GC pause during the sample, process CPU time and process peak working set. Two internal reflection calls isolate existing inner copy/validation methods without changing them; these are not complete outer transaction costs. The actual host worker executes synchronously in the harness so its thread allocations can be attributed; a sink store removes disk and final store encoding. The measured worker still performs real codec copy and edition load/replay validation.

One warmup precedes each expensive operation and five samples follow; cheap captures use20 samples/two warmups. Generation is one observation with no warmup. At n=5, nearest-rank p95 and p99 both equal the maximum: they are **observed order statistics, not reliable population-tail estimates**. JIT/order effects are visible: early strict decode can be slower than a later combined encode/decode series. Do not add/subtract separately warmed medians or derive end-to-end speedup percentages. No forced GC or allocation suppression was used.

## Actual fixtures and feasibility

All maps use seed42 and the actual generated package, with **three colonies, two fleets and two ships**. Map size is not empire density. Fresh roots have turn1/revision0 and no ordinary journal entries. Accepted-command timings alternate lawful research allocations, producing six accepted changes including warmup; the storage successor uses that real history. Turn timings reset through the real loader before each sample, outside the measured interval; only the first strategic turn is measured, not a mature progression run.

| Fixture | Planets / belts | Fresh encoded bytes | Result / process peak working set |
| --- | ---: | ---: | --- |
| [30 systems](../evidence/state-repository-20261122/runtime-30.json) | 133 / 24 | 765,755 | Complete bounded series; 227,229,696 bytes peak. |
| [100 systems](../evidence/state-repository-20261122/runtime-100.json) | 457 / 76 | 1,627,231 | Complete bounded series; 259,665,920 bytes peak. |
| [1,000 systems](../evidence/state-repository-20261122/runtime-1000.json) | 4,465 / 755 | 12,368,562 | Generation, queries, copy/validation, codecs, load and commands measured; first AdvanceTurn warmup rejected `UnsupportedSource`. **Partial run**, not successful large-world turn acceptance. Peak1,115,709,440 bytes. |
| [1,000 worker-only](../evidence/state-repository-20261122/runtime-1000-worker.json) | Same initial fixture | 12,368,562 | Separate process/mode measures prepare and actual save worker, without repeating the failed turn path. Peak1,179,086,848 bytes. |
| [10,000 systems](../evidence/state-repository-20261122/runtime-10000-rejected.json) | Not generated | None | Normal generation bounds reject the request. No limit lifted; no fabricated timing or scalability claim. |

The1,000-system unsupported-source rejection is retained as evidence, not investigated or repaired in this architecture task. It is a gate to resolve in representative performance work before claiming that map size supports the full gameplay workload.

## Runtime results

Milliseconds below are medians of five samples unless stated otherwise. Full per-sample evidence and p95/p99/max/allocations/GC/CPU summaries are in [runtime-summary.csv](../evidence/state-repository-20261122/runtime-summary.csv). Values do not include native rendering, a complete UI frame, disk I/O, network latency or dense/mature empires.

| Operation | 30 systems | 100 systems | 1,000 systems |
| --- | ---: | ---: | ---: |
| Inner GameState copy (reflection call) | 2.97 | 3.51 | 5.30 |
| Inner saved-state validation (reflection call) | 23.50 | 34.12 | 84.16 |
| Complete Format18 export | 5.53 | 11.33 | 67.00 |
| Encode complete root | 30.43 | 46.73 | 530.75 |
| Strict decode complete root | 395.59 | 637.04 | 2,402.05 |
| Codec copy: encode + decode | 188.32 | 535.48 | 2,607.41 |
| Complete typed load/replay from detached root | 318.56 | 383.60 | 3,425.74 |
| Rejected lower-revision command | 51.21 | 50.84 | 219.49 |
| Accepted research-allocation command | 143.48 | 151.16 | 522.66 |
| Advance first strategic turn | 329.67 | 442.23 | Rejected during warmup; no series |
| Actual host save preparation/export | 2.18 | 8.87 | 79.81 (separate worker-only run) |
| Actual save worker copy + validation/replay, sink store | 313.90 | 608.15 | 7,187.76 (separate worker-only run) |

The lower-revision request uses expected revision−1 against fresh revision0; inspected validators return stale-revision rejection for this otherwise complete command. It is not a benchmark of every possible rejection path. Command measurements include real outer transactions; inner copy timing alone cannot account for their total work.

| Actual save worker | Median managed allocated bytes / invocation | Observed p95 = p99 = maximum latency |
| --- | ---: | ---: |
| 30 systems | 150,321,064 | 342.57 ms |
| 100 systems | 250,568,024 | 701.50 ms |
| 1,000 worker-only | 1,497,750,536 | 7,749.18 ms |

Allocation totals are cumulative bytes allocated, **not peak live RAM**. The1,000 worker's maximum *sum of GC pauses within one sample* was1,983.912ms; it is not a measured longest individual GC pause or a Unity frame stall. Its one generation observation allocated about15.0GB cumulatively while the process working-set peak stayed around1.18GB. These distinctions matter when choosing memory work: allocation churn and peak footprint are related but different.

**Measured assessment:** pre-disk codec/copy/replay work is substantial even with a small inhabited empire. The inner GameState copy alone is not the dominant measured save-stage cost. Prioritize profiling outer copy, strict codec walking and complete restore validation before proposing broad copy-on-write changes or promising a database will fix stalls. Removing validation to improve timing would change the trust contract and is not recommended.

## Storage comparison on identical genuine bytes

Each storage method receives the same pre-exported baseline/successor bytes. Twelve fresh stores per method; p95/p99 both select the maximum at this n. Recorded results: [30-system storage](../evidence/state-repository-20261122/storage-real-30.json), [100-system storage](../evidence/state-repository-20261122/storage-real-100.json). Those files retain aggregate timing/size/proof results, not individual storage timing observations. These are real files with flush/commit, not an in-memory database.

An [independent four-payload loader check](../evidence/state-repository-20261122/runtime-payload-validation.json) strictly decoded both actual30/100 baselines and successors, loaded/replayed them through the exact edition session, then exported/encoded byte-identical results. Both successors are turn1/revision6. Combined with byte-identical SQLite restoration, this connects the opaque storage experiment back to the real game loader without claiming that Node runs game validation itself.

| Successor operation, median ms | 30 systems | 100 systems |
| --- | ---: | ---: |
| Flushed JSON temporary + replacement (latest file only) | 4.206 | 4.772 |
| JSON payloads + replaced catalog (both immutable roots retained) | 8.645 | 8.504 |
| SQLite full immutable root transaction | 7.763 | 12.470 |
| SQLite64KiB chunk-manifest transaction | 8.828 | 13.996 |
| Separate SQLite full pair WAL checkpoint | 9.708 | 16.466 |
| Separate SQLite chunk pair WAL checkpoint | 10.087 | 14.904 |

The simple JSON replacement retains one save; catalog/SQLite retain both. Only SQLite implements the tested CAS/branch/reference transaction. Thus these are storage primitive comparisons, not equally complete save-manager implementations. Checkpoint timing is reported separately; an acknowledged FULL WAL commit is already durable to the stated OS/device contract, but maintenance still costs work. Physical device-write traffic, CPU/disk utilization and directory-entry power-loss durability were not measured.

| Payload/WAL measure | 30 systems | 100 systems |
| --- | ---: | ---: |
| Successor full payload bytes | 773,021 | 1,634,497 |
| New chunk payload bytes | 707,485 | 1,372,353 |
| Payload-byte reduction (not device writes) | 65,536 (8.5%) | 262,144 (16.0%) |
| Full / chunk successor WAL growth | 807,520 / 762,200 | 1,676,840 / 1,433,760 |

Fixed-size chunks showed modest reuse and no latency advantage on these real pairs. The [synthetic run](../evidence/state-repository-20261122/storage-synthetic.json) favors chunks with two fixed-width changes, but timings also varied sharply across smoke/expanded runs with unchanged sizes. It is not evidence for production speedup. **Recommend full immutable roots for the first repository prototype; defer chunking until representative savings justify manifest/GC complexity.**

Passed storage-engine assertions: stale expected-head CAS rolls back tentative payload; immutable snapshot update is rejected; storage branches remain independent; manual reference survives automatic-reference retention; second SQLite connection cannot obtain a concurrent write transaction; consistent backup reopens with equal bytes; a killed child with an uncommitted2MiB write/WAL recovers the prior roots with integrity/FKs valid. These are small proof cases. Branches use opaque bytes, not two simulated game futures; retention deletes refs, not payload GC; second-connection exclusion is not an OS-level lifetime host lease. No power loss, full disk, read-only directory, managed provider or Unity player was tested.

## What is and is not established

| Assignment workload | Evidence now | Remaining gate |
| --- | --- | --- |
| Current vs target persistence | Source inventory and diagrams; complete-root vs storage-only candidate | No production target activated; no managed/native integration benchmark. |
| Small/medium/large worlds | 30/100 complete diagnostic series,1,000 partial plus save-worker series;10,000 normal rejection | Denser colonies/fleets/ships and longer histories, admissible1,000 turn path, separately scoped higher bounds. |
| Accepted/rejected command, turn, capture/export/load | Selected genuine paths with raw samples/allocations/GC | Other command types, full prepared UI frame and native frame/capture stall timing. |
| Combat pending/buffer/finalize | Exact source-boundary inventory only | Timed complete valid combat checkpoints and deterministic resume across pending/partial/finalized states. |
| Manual/incremental autosave and restore | Real worker sans disk plus separate same-byte disk primitives; exact chunk reconstruction | Integrated named saves/autosave does not exist; no semantic deltas or checkpoint-plus-tail loader. |
| 1/2/8/32 command producers | One serial writer only | Arrival queues, stale-command outcomes, fairness, backpressure and separate accepted/rejected throughput. |
| Recovery and failures | Pre-commit process kill, rollback, backup, integrity proof cases | Post-commit lost receipt/idempotent retry, full disk/migration/read-only/corruption, real cross-process lease, retention GC. |
| Memory and I/O | Current-thread allocation, GC totals, process CPU/peak, logical payload and WAL/file sizes | Native/all-thread allocations, event-level GC traces, physical writes, disk/CPU utilization and sustained-load recovery age. |

## Measurable budgets and next profiling gate

These samples justify where to investigate, not a universal p99 or an acceptable foreground pause. Final release budgets remain **open** until an actual target player and mature representative workload are profiled. PhaseA2 should repeat isolated stages in separate warmed processes and native players, randomize storage-method order, record host power/cache/load, capture profiler/ETW traces where available, and report uncertainty. Use enough observations for claimed tails; five or twelve samples cannot establish a robust p99.

Proposed review criteria, distinct from measured success:

1. Correctness is absolute: byte/root continuity, RNG/IDs/dedup and privacy unchanged; no lost prior durable checkpoint, double action or manual-save overwrite in the fault matrix.
2. Memory is structurally bounded to one captured image plus one intent; establish its actual native byte ceiling by world/history before enabling autosave for that profile. Do not infer a64MiB wire limit bounds object-graph allocation.
3. Set native foreground-capture and UI frame budgets after tracing. Use the measured1,000 prepare maximum118.29ms and worker allocations as reasons to keep worker scheduling and capture redesign separate; do not call this "no noticeable stall" already achieved.
4. Compare candidate vs unchanged full JSON with equivalent retained-checkpoint semantics and durability. Report encoding, commit and maintenance separately; any regression or extra storage must be justified by required library/recovery behavior, not hidden in averages.
5. Establish p95/p99 targets for completed saves and restore, recovery-point age under sustained turns/combat, and accepted/rejected throughput for1/2/8/32 producers before PhaseD default activation. Target history lengths/entity counts and tolerable loss window require product approval.

At the measurement handoff, A2 and subsequent implementation were unapproved. The later [owner authorization](state-repository-implementation-authorization-20261122.md) now permits A2/B1/B2/C1/C2/D1; their evidence gates remain outstanding. These historical measurements support complete-root storage work, not claims of proven SQLite speedup, proven10,000-system scale, or safe removal of the existing replay/copy boundaries. Follow the [execution contract](state-repository-implementation-contract.md) for current ownership and trial pins.
