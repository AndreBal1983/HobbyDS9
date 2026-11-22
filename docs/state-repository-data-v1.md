# Host repository B1/B2 isolated candidate

Authorized by [the implementation brief](design/state-repository-implementation-authorization-20261122.md), based on0fb1405 (ADR55a0c67). No production/default/Unity or portable game project changes. Runtime integration must retain the separate tested2fc0dbc Format18/package5 carrier. Architecture owns integration and native/provider gates.

## Implemented boundary

`HobbyDS9.Persistence` is provider-free netstandard2.1/C#9. It exposes defensively owned `CheckpointImage`, complete opaque `EditionIdentity` (16KiB characters maximum), source turn/revision/phase, request/CAS/reference types and durable receipts. `ICheckpointValidator` is supplied by the trusted host: decode with the matching complete carrier/package, load/replay and compare exact reexport bytes, derive the descriptor/stamp, or reject. Repository metadata is never gameplay authority. The repository checks that returned bytes/hash/descriptor/stamp equal the request; it owns neither simulation state nor a gameplay-format converter.

`HobbyDS9.Persistence.Sqlite` stores full immutable blobs and metadata per campaign; no entity SQL, chunks or live-session queries. A lifetime `FileShare.None` writer handle is held at `writer.lock`, plus an object gate and immediate SQLite transactions. Cooperating hosts must all use this lease. This is not protection against arbitrary programs writing SQL or hostile filesystem replacement. Paths must be absolute, confined to an explicit campaign directory, and free of existing reparse ancestors/targets. Names are bounded Unicode data; IDs are restricted host tokens. SQL parameters carry values.

Schema1/application-ID guards are separate from gameplay formats. Unknown schema/edition/campaign is rejected. WAL/FULL, foreign keys, one-second busy timeout and bounded automatic WAL checkpointing are explicit. Payload/type/string limits are checked before managed materialization, in addition to schema/write checks. Invalid UTF16 is rejected; fingerprints use strict UTF8 and length-framed fields, including source epoch, target identities, stamps, expected generations and every reference/image field.

Commit inserts the payload, moves branch head, updates the requested reference and records its immutable operation fingerprint/receipt in one transaction. Receipt lookup precedes stale CAS on identical retries. Reusing an operation ID with changed semantics rejects; fork/set/delete are similarly idempotent. A commit/acknowledgment exception is `OutcomeUnknown`, not proof of rollback. Reopen and `FindOperation` before retrying; lookup errors never mean confirmed absence. Receipts do not expire during retention.

References support Manual/Auto/Milestone, duplicate names, rename/replace by expected generation and confirmed deletion; deleted IDs remain tombstones and cannot be resurrected with generation0. Forking an older checkpoint creates an independent branch without erasing the original future. Listing is bounded and cursor-based.

Explicit JSON import validates exact bytes before DB mutation, keeps the source read-only/unchanged, creates a verified digest-named recovery copy and stable per-edition/digest operation/branch/manual reference. Repeat import is idempotent. Export revalidates then atomically writes exact carrier bytes into an owned exports directory. Backup uses SQLite's backup API, then quick-checks and validates every retained payload before flushing/publishing the destination; copying an open main file alone is not used. Unsupported formats are the matching validator's responsibility; no guessed defaults or14→18 conversion exists.

Explicit post-commit retention defaults to10rolling/3milestones/512MiB soft logical automatic bytes. All manual refs and the active head stay pinned. Old automatic refs expire with generation bumps; unpinned full payloads are removed while ancestry metadata and operation receipts remain. Inactive branch heads can reference an unavailable tombstone, reported explicitly on read, rather than pinning every automatic future forever. This is storage payload GC, not gameplay journal compaction. Physical SQLite pages/WAL may remain larger than logical live bytes; idle checkpoint/space reporting and user-visible budget warnings remain integration work. No autosave scheduler/UI activation is supplied here.

## Frozen provider trial and observed limits

Exact packages: Microsoft.Data.Sqlite.Core10.0.14; SQLitePCLRaw.core/provider.e_sqlite3/lib.e_sqlite3 each2.1.13. No bundle/ORM. `SqliteProviderBootstrap.EnsureInitialized()` calls SetProvider/FreezeProvider exactly once. Full transitive hashes/versions are in package lock files. netstandard target resolves System.Memory4.5.3, System.Buffers4.4.0, System.Numerics.Vectors4.4.0 and Unsafe4.5.2. GUI must inspect its actual Unity/BCL bindings rather than importing a NuGet runtime graph blindly.

Normal net10 tests resolve facade net8/provider net6. A separate harness explicitly references facade/core/provider **netstandard2.0** DLLs and the Data **netstandard2.1** outputs, verifying their runtime framework attributes/hashes and actual create/commit/fork/ref/online-backup/reopen/validated-byte behavior. This is .NET-host evidence, not Unity IL2CPP acceptance.

Frozen native Windows x64 package reports SQLite3.53.3, source ID `2026-06-26 20:14:12 d4c0e51e4aeb96955b99185ab9cde75c339e2c29c3f3f12428d364a10d782c62`; DLL SHA256 `b7385d722c83fb52142a00477a726723745916d22a555711ee89834c1111fb2e`. Full options/runtime assembly hashes are emitted by the harness. Architecture is reviewing newer engine fixes: these pins are approved for isolated generated-data trials, **not distribution or hostile-database acceptance**. Keep that gate open until reviewed native artifacts and QA acceptance.

A WIP regression encountered native0xC0000005 through provider `GetBytes/GetStream` on computed CASE/CAST BLOB expressions (`sqlite3_table_column_metadata`). Final code avoids that API path: SQL bounds type/size, then GetValue reads only the bounded blob. Preserve this as a distinct provider compatibility hazard, not an inferred cause of any engine CVE. The fatal path is not invoked in the main test suite. No native UI/Editor process was run by Data.

Licenses: Microsoft.Data.Sqlite source/package MIT; SQLitePCLRaw managed wrappers Apache-2.0; the native NuGet package declares Apache-2.0 in addition to SQLite engine public-domain status. Exact package manifests/notices and any replacement native recipe must be reviewed before distribution; GUI owns staging/provenance.

## Reproduce and scope of evidence

```powershell
dotnet restore tests/HobbyDS9.Persistence.Sqlite.Tests/HobbyDS9.Persistence.Sqlite.Tests.csproj --locked-mode
dotnet test tests/HobbyDS9.Persistence.Sqlite.Tests/HobbyDS9.Persistence.Sqlite.Tests.csproj -c Release --no-restore
dotnet build tools/HobbyDS9.Persistence.NetStandardSmoke/HobbyDS9.Persistence.NetStandardSmoke.csproj -c Release -p:ProviderPackageRoot=<absolute-NuGet-cache>
dotnet tools/HobbyDS9.Persistence.NetStandardSmoke/bin/Release/net10.0/HobbyDS9.Persistence.NetStandardSmoke.dll <new-absolute-artifact-directory> smoke
```

26focused tests passed at the first candidate checkpoint,0failures/skips. They cover real local disk, child-process lease conflicts/kills before and after commit, lost acknowledgment/reopen retry, CAS rollback, schema/edition refusal,220Unicode references, fork futures, imports/original recovery copies/exports, online backup,10+3retention/manual protection, strict Unicode and oversized stored-value rejection. Fixtures use deliberately trivial test validators; they do **not** prove actual core14/17/18 game restoration. Backend supplies those real codecs/loaders and epoch/capture seams; the independent adapter proof is the next B2 gate. Full-disk/read-only-volume, actual power loss, broader platform/process races, representative performance, native packaging and production activation are not claimed.
