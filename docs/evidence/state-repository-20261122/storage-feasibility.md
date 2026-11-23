# SQLite feasibility and isolated storage evidence

22 November 2026. **Proposed adapter choices; storage-engine prototype only.** Source worktree: `f5ebc86772f38e9c55aad9ec9e35dc368906de03`. No live persistence path, game format, package dependency or Unity asset was changed. The [assignment](../../prompts/state-repository-savegame-redesign.md) requires a later Windows x64 Unity IL2CPP gate; none of the results below satisfy it.

## Proposed provider shortlist

| Candidate | Actual dependency/packaging shape | Assessment |
| --- | --- | --- |
| Microsoft.Data.Sqlite.Core plus explicit SQLitePCLRaw provider and bundled native SQLite | Managed ADO.NET facade, `SQLitePCLRaw.core`, `SQLitePCLRaw.provider.e_sqlite3`, matching Windows x64 `e_sqlite3.dll`, explicit initialization; resolve and lock all transitive dependencies | First packaging spike. A small parameterized SQL adapter needs no Entity Framework/ORM. Microsoft.Data.Sqlite.Core 10.0.14 is an inspected example containing a .NET Standard 2.0 target and requiring SQLitePCLRaw.core >=2.1.13; this is compatibility evidence, not approval to ship that patch version. [Official package](https://www.nuget.org/packages/Microsoft.Data.Sqlite.Core/10.0.14) |
| SQLitePCLRaw directly, same static DllImport provider and native binary | Removes the ADO.NET facade; retains native library, marshaling, statement/error/handle ownership | Second candidate if facade compatibility or stripping fails. More adapter code and disposal/error-path tests. Provider naming maps to the native library name and providers do not contain the native code. [Maintainer documentation](https://github.com/ericsink/SQLitePCL.raw) |
| Small direct C ABI binding to a pinned SQLite build | Explicit P/Invoke signatures, UTF-8 buffers, result-code handling and native lifecycle maintained locally | Fallback only: narrow API but larger maintenance responsibility. Not implemented or benchmarked here. |

Microsoft documents that the main Microsoft.Data.Sqlite package normally brings a SQLitePCLRaw bundle; using Core permits explicit selection. Do not assume an ordinary NuGet desktop build's runtime-asset copying configures Unity plug-ins. [Microsoft provider documentation](https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/custom-versions)

**Version-sensitive dependency correction:** SQLitePCLRaw 3.x uses `SourceGear.sqlite3` for native binaries; `bundle_e_sqlite3` now combines `config.e_sqlite3` and that native package. The former `bundle_sqlite3` and `bundle_winsqlite3` packages were removed, but their providers remain. Old bundle recipes must not be copied without resolving the chosen versions. [Maintainer 3.0 notes](https://raw.githubusercontent.com/ericsink/SQLitePCL.raw/main/v3.md)

The public SourceGear package advertises native builds named `e_sqlite3`; it is a candidate distribution source, not a verified artifact in this experiment. An explicit reproducible SQLite build is another option. Do not mix the probe's Node SQLite version with the version shipped by this package. [Official package](https://www.nuget.org/packages/SourceGear.sqlite3)

Proposed first target is a bundled, pinned x64 library. Windows' system `winsqlite3` avoids shipping that DLL but delegates version/features to the OS and does not give a consistent bundled engine across targets; retain it as an alternative, not the initial portable distribution choice. This is an architecture assessment based on the documented provider distinction, not a benchmark result.

## Unity/AOT and distribution gate

Unity 6000.6 documents native plug-in platform/CPU configuration and DLL import. Import only the intended Windows x64 native asset, explicitly configure Editor/Standalone and architecture, and inspect the produced player for the actual DLL and all dependent runtime libraries. Keep managed assemblies in a host-only adapter; leave portable game projects unchanged. Folder conventions are useful defaults, not verification. [Unity plug-in configuration](https://docs.unity.com/en-us/engine/6000.6/manual/programming-environment/plug-ins/plug-in-inspector)

IL2CPP is AOT: runtime code generation and `Reflection.Emit` cannot be assumed; reflection-dependent code may be stripped. Native-to-managed callbacks require suitable static methods and callback attributes. Audit the selected provider's callback/marshaling paths and preserve only necessary managed code. Avoid custom SQL functions/collations initially. A .NET Standard target, passing .NET tests, or another runtime's NativeAOT result does **not** demonstrate Unity IL2CPP compatibility. [Unity 6000.6 restrictions](https://docs.unity.com/en-us/engine/6000.6/manual/programming-environment/scripting-backends/il2cpp/scripting-restrictions)

Required later evidence: Windows x64 player built with **6000.6.9f1**, intended stripping settings and pinned package/native hashes; cold offline launch on a machine without developer tools; create/write/commit/reopen, writer conflict, backup/restore and forced-process-exit recovery; check native dependencies and license inventory. Source `ProjectVersion.txt` here still records 6000.6.5f1; the requested player acceptance version is separate. No player was built or launched for this storage task.

SQLite core is public domain; Microsoft.Data.Sqlite's source uses MIT; SQLitePCLRaw uses Apache-2.0. Preserve required license/copyright/NOTICE material for selected packages and check the exact native artifact's included notices before pinning distribution. Paid encryption/support offerings are outside this proposal; the installed game must never fetch a runtime dependency or require an account. [SQLite copyright](https://sqlite.org/copyright.html), [Microsoft source license](https://raw.githubusercontent.com/dotnet/efcore/main/LICENSE.txt), [SQLitePCLRaw license](https://raw.githubusercontent.com/ericsink/SQLitePCL.raw/main/LICENSE.TXT)

## Proposed local durability and backup policy

Use one serialized persistence worker per campaign with its own connection, bounded queue, immutable inputs and host ownership/fencing. `BEGIN IMMEDIATE` reserves the transaction's writer; expected branch head plus storage epoch compares-and-swaps the checkpoint reference in the same transaction as payload and metadata. SQLite's transaction lock does not by itself enforce a single gameplay host over its entire lifetime. [SQLite transactions](https://www.sqlite.org/lang_transaction.html)

Propose local writable storage, WAL, `synchronous=FULL`, foreign keys on every connection and a bounded busy timeout. WAL/FULL syncs the WAL on each commit; `NORMAL` has a weaker acknowledged-commit durability contract. Acknowledge persistence only after successful commit; keep the previous accepted root after failure. This relies on OS/device flush behavior and does not constitute a tested power-loss guarantee. [SQLite synchronization](https://www.sqlite.org/pragma.html#pragma_synchronous), [SQLite failure conditions](https://www.sqlite.org/howtocorrupt.html)

WAL permits concurrent readers but only one writer. Long readers can obstruct checkpoints; keep readers short and schedule bounded checkpoint maintenance, monitoring backlog and file growth. Use a local filesystem, not a shared/network campaign database. Autosave backlog or disk pressure needs explicit host behavior. The probe disables auto-checkpoint only to measure separate write/checkpoint phases; that setting is **not** proposed as an unattended production default. [SQLite WAL](https://www.sqlite.org/wal.html)

Use SQLite's backup API to create a consistent destination, then reopen, check integrity and validate selected game roots before publishing the backup. Do not copy only an active `.db` file or delete its WAL as cleanup. Keep original recovery files intact. Microsoft exposes `BackupDatabase`; its documented implementation blocks other writers, so schedule it on the persistence worker and measure pauses. [SQLite backup](https://www.sqlite.org/backup.html), [Microsoft backup](https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/backup)

Microsoft's async ADO.NET methods execute synchronously for SQLite. A worker plus bounded queue is necessary to keep I/O off the UI thread; an `await` alone does not establish that. [Microsoft async limitations](https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/async)

## Executable experiment and interpretation

[Probe source](../../../prototypes/StateRepositoryProbe/storage/storage-probe.mjs) uses the already installed Node 24.22.3 `node:sqlite`, reporting SQLite 3.54.1. It uses real files under its ignored `.local` directory, no downloaded package or network service. This proves only storage-engine behavior. Script/runtime versions and payload SHA-256/lengths are recorded in each small result file; database files and payloads are not tracked.

All paths receive the **same pre-exported bytes**: flushed temporary JSON plus same-directory rename; a second JSON control keeping both immutable files and a replacement catalog; immutable full BLOB roots in SQLite; immutable 64 KiB content-addressed chunks plus a complete ordered manifest per root. Restoring chunks verifies each chunk hash, total length and full root hash before returning exact bytes. Chunk roots need no command replay or preceding snapshot. This is a storage delta, not a simulation delta: full export, deep copies, game validation and serialization remain unchanged. Changing array lengths/property lengths may shift many fixed boundaries and eliminate deduplication.

SQLite commits atomically include root payload/manifest, branch head/epoch and a save reference. The branch test uses opaque supplied bytes on two storage branches; it does not simulate two different gameplay futures. The retention check deletes only old automatic references, protects a manual reference and leaves payload/history reclamation unimplemented. The single-writer contention test is a second connection in one process. The killed child writes a 2 MiB uncommitted root with a small page cache, reports pre-commit staging, and is forcibly terminated; reopen checks old heads/counts, exact payload bytes, integrity and foreign keys. This is process-crash evidence, not OS crash or power removal.

Each benchmark uses 12 fresh stores and the same A/B pair. The simple JSON path retains only the latest file. JSON-with-catalog and SQLite retain both roots, but only the SQLite experiment implements branch/CAS/reference transactions: timings are storage primitive measurements, **not equivalent completed save managers**. The catalog is published after its payload file; Windows directory-entry/power-loss durability is not established. SQLite and JSON-catalog timings include hashes; chunk preparation is also timed. All paths exclude export and serialization. WAL growth, logical page count, final DB/SHM/WAL sizes and new payload bytes are distinct measurements; none is physical device-write volume. Checkpoint cost is reported separately, not omitted from the durability assessment. Sequential order, warm filesystem caches and a small sample prevent general speed or tail-latency claims; p95/p99 use nearest rank and both select the maximum at n=12, while median averages the two central samples.

Synthetic data is 992,074 bytes per root with two fixed-width changes. [Synthetic result](storage-synthetic.json): successor medians JSON 65.733 ms, JSON-with-catalog 82.596 ms, SQLite full 77.625 ms, chunks 15.071 ms; separate pair checkpoint medians full 137.245 ms and chunks 82.765 ms. Chunk successor inserts 131,072 payload bytes versus 992,074 full bytes; WAL grows 173,040 versus 1,021,760 bytes. These contrived changes favor fixed chunks and are not gameplay scale evidence. An earlier three-method smoke run had substantially lower wall times; after adding the catalog control, timings changed sharply while byte counts stayed identical. The host/load/cache cause was not measured, so the retained expanded-method run is evidence of bounded behavior and sizes, not a stable performance comparison.

### Genuine Format18 payloads

The runtime companion exported actual `generated-belt-bodies-v1` roots through `ReviewNewtonsoftCodec` at runtime source `2fc0dbca46073121379dd9491c859a02a6f22dcd`. Seed 42; the baseline is fresh turn 1/revision 0 and the successor follows six accepted research-allocation commands, including the warmup. Rejected stale attempts do not become accepted mutations. These are **30/100-system generated maps with only three colonies, two fleets and two ships**, not mature large empires. [30-system runtime evidence](runtime-30.json), [100-system runtime evidence](runtime-100.json)

Input hashes/lengths are retained in [30-system storage results](storage-real-30.json) and [100-system storage results](storage-real-100.json). Both sets use the same baseline/successor bytes across all four paths and verify byte-identical SQLite full/chunk restore. Storage accepts opaque bytes; the companion's real loader/replay validation is separate evidence, not performed by Node.

| Pair | Baseline -> successor bytes | JSON replace successor median | JSON catalog successor median | SQLite full successor median | SQLite chunks successor median |
| --- | ---: | ---: | ---: | ---: | ---: |
| 30 systems | 765,755 -> 773,021 | 4.206 ms | 8.645 ms | 7.763 ms | 8.828 ms |
| 100 systems | 1,627,231 -> 1,634,497 | 4.772 ms | 8.504 ms | 12.470 ms | 13.996 ms |

| Pair | Full / chunks new successor payload bytes | Full / chunks successor WAL growth | Full / chunks pair checkpoint median | Full / chunks DB bytes after checkpoint |
| --- | ---: | ---: | ---: | ---: |
| 30 systems | 773,021 / 707,485 | 807,520 / 762,200 | 9.708 / 10.087 ms | 1,589,248 / 1,536,000 |
| 100 systems | 1,634,497 / 1,372,353 | 1,676,840 / 1,433,760 | 16.466 / 14.904 ms | 3,313,664 / 3,067,904 |

All bounded CAS/rollback, branch/reference, writer exclusion, backup and pre-commit process-kill checks passed for both genuine pairs; `integrity_check` returned `ok` and foreign-key checks were empty. The low reuse of fixed chunks on genuine serialized roots is a reason to defer this representation until broader measurement. It establishes neither a latency improvement nor savings in deep copies/export/serialization. Full immutable SQLite roots are the simpler initial transactional candidate; content-defined or semantic chunk boundaries require their own format, canonicalization, garbage-collection and restore proof before selection.

Machine observation: local `C:` fixed NTFS volume, INTEL SSDPEKNU512GZ NVMe SSD (512,110,190,592 physical bytes); CPU/OS/RAM are in the result. No physical I/O tracing, CPU/disk utilization, allocation/GC profiling, cache flushing or hardware power-failure test was performed.

Reproduce from the design worktree:

```powershell
node prototypes/StateRepositoryProbe/storage/storage-probe.mjs --samples 12 --label synthetic --output docs/evidence/state-repository-20261122/storage-synthetic.json
# For genuine exports, supply two unchanged files from the runtime probe:
node prototypes/StateRepositoryProbe/storage/storage-probe.mjs --a <baseline-json> --b <successor-json> --samples 12 --label real-format18 --output docs/evidence/state-repository-20261122/storage-real.json
```

Remaining implementation gates include the actual .NET/IL2CPP provider, complete slot/catalog semantics and root validation, import/migration failures, disk full/read-only directories, cross-process host ownership, retention/garbage collection, corruption handling, and representative gameplay/performance workloads. SQLite is a feasible transactional local-store candidate; the measured experiment does not establish an end-to-end runtime speedup.
