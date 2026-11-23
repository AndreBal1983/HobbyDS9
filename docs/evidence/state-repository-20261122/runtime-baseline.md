# Runtime baseline — 22 November 2026

**Status: measured exploratory baseline; no production optimization or gameplay acceptance.** This small probe isolates existing runtime work from durable storage. It does not satisfy the assignment's full benchmark matrix. The [probe source and reproduction instructions](../../../prototypes/StateRepositoryProbe/runtime/README.md) are publishable; generated save blobs and binaries remain ignored local artifacts.

## Identity and method

- Runtime source: clean detached `2fc0dbca46073121379dd9491c859a02a6f22dcd`, independently observed before/after the runs. Design worktree base: `f5ebc86772f38e9c55aad9ec9e35dc368906de03`.
- SDK 10.0.401; actual executed runtime **.NET 10.0.12**, x64; Release; workstation GC, Interactive latency; default tiered compilation; no DOTNET/COMPlus overrides. Windows 11 Home, build 10.0.26200; AMD Ryzen 7 5800HS, 8 cores/16 logical processors; 16,541,487,104 bytes physical RAM. See [environment and source verification](runtime-environment.json).
- Actual generated BeltBodies profile, Format 18, package hash `2e1753666e957db7d85f0f16faa865014d0dc80b64542aeb47d009b9ae12994e`, seed 42. Generation settings are recorded in each JSON. Bounds-compliant map dimensions vary by size; this is not a claim to reproduce every GUI preset.
- Serial single writer, fresh process per size. Most series: n=5, one warmup; status/belt/refinement captures: n=20, two warmups; startup: n=1, no warmup. The worker-only 1,000-system run is a separate process after the full run's advance rejection.
- Timing/allocations bracket the actual call; setup/reset is excluded unless named startup. Per-call current-thread allocated bytes, process-wide GC counts/pause deltas and process CPU time are recorded. No forced GC, process affinity or exclusive-machine control. Desktop/background activity, including companion storage work, was not isolated.

The identity guard and explicit expected rejection assertion were added after the completed timings, without changing their operations. Those original runs were independently pinned and clean; the final probe also checks HEAD/clean tracked state itself. Its guarded 10,000 request was built and exercised. One initial compile error and one resource-path setup error were corrected before successful measurements; no production source was changed.

## World shape and process memory

All fresh campaigns had **3 colonies, 2 fleets, 2 ships**, turn 1/revision 0, empty strategic journal. These are map-size workloads, not dense late-game empires. Memory numbers below are bytes for the entire process, including generation, setup, JIT, warmups and excluded resets; peak is not live state size.

| Run | Planets / belts | Encoded baseline bytes | Peak working set | Final working set / private bytes | Elapsed |
|---|---:|---:|---:|---:|---:|
| 30 | 133 / 24 | 765,755 | 227,229,696 | 203,911,168 / 145,813,504 | 16.01 s |
| 100 | 457 / 76 | 1,627,231 | 259,665,920 | 232,787,968 / 170,020,864 | 23.31 s |
| 1,000, partial | 4,465 / 755 | 12,368,562 | 1,115,709,440 | 284,557,312 / 225,071,104 | 87.29 s |
| 1,000, worker only | 4,465 / 755 | 12,368,562 | 1,179,086,848 | 340,160,512 / 285,384,704 | 78.75 s |

The two 1,000-system runs produced the same baseline SHA-256, `0c38c999dd43d6030d43405f62aff609694d107f76b617374299bb5578aa7168`. This is a narrow fixture reproducibility check, not a general determinism proof.

## Observed latency

Cells show **median / observed maximum, milliseconds**, n=5. At this n, the reported nearest-rank p95 and p99 both equal the maximum; they are **not credible population tail estimates**. Startup and the cheap captures are available in the raw records and summary, not mixed into this table.

| Operation | 30 systems | 100 systems | 1,000 systems |
|---|---:|---:|---:|
| Inner GameState copy, reflection | 2.97 / 3.48 | 3.51 / 4.34 | 5.30 / 10.32 |
| Inner review save validation, reflection | 23.50 / 34.96 | 34.12 / 53.98 | 84.16 / 129.82 |
| Full Format 18 export | 5.53 / 6.96 | 11.33 / 14.96 | 67.00 / 90.77 |
| Encode full root | 30.43 / 32.73 | 46.73 / 55.74 | 530.75 / 557.98 |
| Strict decode full root | 395.59 / 423.67 | 637.04 / 695.01 | 2,402.05 / 2,823.47 |
| Codec encode/decode copy | 188.32 / 222.72 | 535.48 / 586.92 | 2,607.41 / 2,699.44 |
| Typed load and replay, already decoded root | 318.56 / 374.57 | 383.60 / 471.55 | 3,425.74 / 3,865.27 |
| Rejected lower-revision command | 51.21 / 67.99 | 50.84 / 70.46 | 219.49 / 224.65 |
| Accepted research allocation | 143.48 / 147.83 | 151.16 / 172.73 | 522.66 / 572.26 |
| Advance one turn | 329.67 / 418.55 | 442.23 / 469.37 | **Rejected during warmup** |
| Host save prepare/full export | 2.18 / 5.33 | 8.87 / 9.95 | 79.81 / 118.29 * |
| Actual host worker copy + replay, sink store | 313.90 / 342.57 | 608.15 / 701.50 | 7,187.76 / 7,749.18 * |

\* Separate worker-only process. One warmup does not stabilize tiered JIT or allocation/GC history. For example, standalone strict decode was slower than the later combined copy in small runs. Therefore **do not add/subtract these series, compute component percentages, or interpret later operations as optimizations**. Sub-microsecond status capture timings are near instrumentation/timer limits. Source defaults and operation order are part of this exploratory observation.

The rejected command deliberately sends `ExpectedRevision=-1` while current revision is 0, valid actor/turn/unique ID and research payload. Source `SimulationSession.cs:85–98` checks presence and identity before the revision comparison, with no separate negative-revision guard; `CampaignTacticalReviewSession.cs:174–180` maps this to `StaleRevision`. This is one rejection path, not all failed-command behavior. Accepted command observations accumulate six successful revisions including warmup; the successor payload is after those six commands. Advance observations reset to the original turn-one root outside timing.

## Allocations and GC

Median **allocated bytes per measured call**; these are allocations over time, not retained or peak memory. The complete per-operation allocation medians, all observed GC collection totals, cumulative/max observed pause and CPU totals are in [runtime-summary.csv](runtime-summary.csv), reproducible with `Summarize.ps1`. Every individual sample remains in the JSON records.

| Operation | 30 systems | 100 systems | 1,000 systems |
|---|---:|---:|---:|
| Inner GameState copy | 1,591,664 | 1,786,368 | 4,147,936 |
| Inner save validation | 6,202,000 | 8,204,104 | 32,605,488 |
| Full export | 2,175,008 | 3,655,112 | 21,770,392 |
| Encode | 4,441,040 | 9,187,992 | 68,304,304 |
| Strict decode | 50,283,112 | 105,402,648 | 786,585,600 |
| Codec copy | 54,170,864 | 113,292,976 | 854,887,832 |
| Typed load/replay | 97,580,176 | 138,586,272 | 643,091,288 |
| Rejected command | 17,040,184 | 23,634,520 | 104,943,424 |
| Accepted command | 57,580,104 | 75,426,104 | 295,154,440 |
| One-turn advance | 209,903,496 | 271,156,520 | Not measured successfully |
| Host save prepare | 2,174,616 | 3,652,040 | 21,746,608 * |
| Host save worker | 150,321,064 | 250,568,024 | 1,497,750,536 * |

Worker GC across its five measured calls (warmup excluded):

| Systems | Gen 0 / 1 / 2 collection counts | Total observed GC pause | Largest per-call pause delta | Process CPU across measured calls |
|---|---:|---:|---:|---:|
| 30 | 92 / 26 / 5 | 524.555 ms | 115.848 ms | 2,468.750 ms |
| 100 | 150 / 56 / 10 | 1,083.388 ms | 230.271 ms | 4,921.875 ms |
| 1,000 * | 825 / 443 / 23 | 8,774.930 ms | 1,983.912 ms | 36,984.375 ms |

GC counters and pauses are process-wide. CPU includes all process threads and can exceed elapsed wall time. These figures do not measure disk utilization or native UI stalls.

## Exact boundaries and observed limits

The save worker is the pinned runtime's `prototypes/HobbyDS9.TacticalReviewClient/ColonisationCycleCheckpointSaveWork.cs`: prepare exports the full root at line 31; worker `Execute` calls codec copy and `LoadColonisationCycleSource` at line 36. The codec's `ReviewNewtonsoftCodec.ColonisationCycle.cs:23–24` copy is encode/decode. The sink store deliberately removes final store encoding, file writing and flush. The worker is executed synchronously for allocation attribution; scheduler, cancellation racing, queueing, UI rendering and native responsiveness are unmeasured. Its significant pre-disk work supports investigating copy/revalidation independently from storage; it does not prove a candidate optimization safe or faster.

The inner reflection measurements are narrower than full root validation/copy. `SimulationSession.CopyState` copies the nested GameState; the review-session private `ValidateSave` checks that review state. Neither stands in for all outer sidecar checks, replay or full session staging. No compiler instrumentation or rule bypass was added.

At **1,000 systems**, generation, capture, typed load and accepted commands succeeded, but the **first `AdvanceTurn` warmup**, after loading the original root, returned `UnsupportedSource`; there are no successful turn samples for that size. The partial run exits 1 and records that exception. Its cause was not diagnosed or changed in this scoped measurement task. Save-worker success on an unadvanced root is not proof that a 1,000-system campaign is generally playable.

At **10,000 systems**, `StartCycleGame` fails in normal generator validation with `Scaled-map counts exceed supported bounds.` No world or latency series is claimed. `ScaledGalaxyIdentity.MaximumSystems` is 1,000 (`src/HobbyDS9.Contracts/ScaledGalaxyContracts.cs:12`); settings bounds are enforced at `ScaledGalaxyData.cs:30`. This request also exceeds other count bounds. The [rejection record](runtime-10000-rejected.json) preserves the actual stack and observed pin.

Unmeasured: dense/late-game colonies, ships and fleets; long journal growth; pending/partial/finalized combat; multiple command producers; multiplayer latency; autosave scheduling/backpressure and recovery age; native Unity/IL2CPP; end-to-end file-store save; legacy Format 14; optimized runtime candidate; broad population tails. No full suite, native player or owner saves were used. Durable storage comparisons consume the same genuine 30/100 JSON bytes in the [separate storage probe](storage-feasibility.md); they do not include these export/copy costs.

A separate untimed [payload validation](runtime-payload-validation.json) strictly decoded all four 30/100 baseline/successor files, loaded/replayed each with the exact typed session loader, and required export/encode byte equality. All succeeded, with matching input/reexport SHA-256. Baselines have revision 0; both successors have revision 6. This ties those exact storage-probe inputs to legitimate session history; it is not a long-history or combat replay proof.

## Raw artifacts

- [30-system full probe](runtime-30.json)
- [100-system full probe](runtime-100.json)
- [1,000-system partial probe](runtime-1000.json)
- [1,000-system worker-only probe](runtime-1000-worker.json)
- [10,000-system normal bounds rejection](runtime-10000-rejected.json)
- [Exact baseline and successor load/reexport validation](runtime-payload-validation.json)
- [All measured series, allocations and GC summary](runtime-summary.csv)

Source and final report hashes, machine metadata and observed clean runtime source are in [runtime-environment.json](runtime-environment.json). The private baseline/successor payload hashes can be checked against the companion storage receipt; they are not published as large save files.
