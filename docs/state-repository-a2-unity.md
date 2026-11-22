# A2 isolated Unity provider packaging trial

22 November 2026. **Authorized probe; no default storage activation or game-state restore acceptance.** Runtime ancestry is exact Format18/package5 player `2fc0dbca46073121379dd9491c859a02a6f22dcd`. Authority: [approved phased implementation](https://github.com/AndreBal1983/HobbyDS9/blob/0fb1405/docs/design/state-repository-implementation-authorization-20261122.md) and [Architecture's execution contract](https://github.com/AndreBal1983/HobbyDS9/blob/6844d0a9b46e10c336b2f68d08e4d0ef6eba2cdc/docs/design/state-repository-implementation-contract.md). The current UX player, source ProjectVersion6000.6.5f1 and owner save directories remain unchanged. QA exclusively runs Unity/native players.

## Frozen assets and isolation

[ProviderGraph](../prototypes/StateRepositoryUnityProbe/ProviderGraph/ProviderGraph.csproj) locks Microsoft.Data.Sqlite.Core10.0.14 with SQLitePCLRaw.core/provider.e_sqlite3 both2.1.13. Architecture's [R2 correction](https://github.com/AndreBal1983/HobbyDS9/blob/28409ec05f20d303aecd588a61e116eb50ffb9e7/docs/design/state-repository-implementation-contract.md) replaces only the native asset with official SQLite3.54.1 Windowsx64. The [active manifest](../prototypes/StateRepositoryUnityProbe/provider-manifest.json) pins native-ZIP SHA3-256/SHA256, managed-NuGet archive SHA256, NuGet content hashes, selected assemblies, native imports/exports and separate notices. Original `sqlite3.dll` bytes are staged unchanged under the provider's expected `e_sqlite3.dll` name, SHA256 `4b59d3cded9dc1608544160f3b4b10ac974c6ac0d6ffc23064e5f4c8dbd05aea`,3,519,488bytes; PE imports contain only KERNEL32.dll. Managed I386 metadata is IL-only, not a request to ship x86 native code. The old NuGet native DLL is excluded; its [R1 manifest](../prototypes/StateRepositoryUnityProbe/provider-manifest-r1.json) remains historical.

Use exactly the packages' **netstandard2.0** managed assets. A net10 restore can otherwise choose the facade's net8 or provider's net6 binary. The exact-asset smoke harness therefore uses explicit references. Data supplies the shared `SqliteProviderBootstrap.EnsureInitialized()` implementation and tested Persistence/Persistence.Sqlite DLLs at exact `76b275632a2d34281b0f37911107ec8b7d96095e`; [Data binary pins](../prototypes/StateRepositoryUnityProbe/data-provider-pin.json) and [its preserved R2 receipt](../prototypes/StateRepositoryUnityProbe/evidence/data-build-r2-76b2756.json) lock source/bytes separately. A detached source/artifact snapshot keeps these immutable while Data's later API work continues. No Batteries, bundle initializer, ORM, alternate system SQLite or runtime dependency download is used.

Unity6000.6.9f1 supplies System.Memory4.0.2.0 forwarding into its netstandard2.1 profile; package references request4.0.1.1. The source-compile projects use the installed Unity reference profile/shims without copying NuGet System.Memory/Buffers/Numerics/Unsafe DLLs over it. Source resolution is evidence separate from actual IL2CPP. Managed plugin imports are explicit and Windows64/Editor constrained; the native plugin is Windowsx64 only. All have the probe define constraint. See [Unity's plugin configuration](https://docs.unity.com/en-us/engine/6000.6/manual/programming-environment/plug-ins/plug-in-inspector).

The current client has a default RuntimeInitialize game bootstrap. The preparer consequently copies **only owned probe assets and pinned provider/Data libraries** into a new generated Unity project under this worktree's artifacts. No game assembly, campaign resource or save directory is copied. The generated project uses6000.6.9f1, netstandard2.1, Windowsx64 IL2CPP and High stripping. `HOBBYDS9_STATE_REPOSITORY_PROBE` is supplied only to its BuildPlayer. Source/default defines stay off. The bounded trial deliberately preserves the provider assemblies broadly in [link.xml](../client/HobbyDS9.Unity/Assets/HobbyDS9/StateRepositoryProbe/Runtime/link.xml); this is not a production trimming policy. [Unity linker reference](https://docs.unity.com/en-us/engine/6000.6/manual/programming-environment/script-compilation/managed-code-stripping/xml-formatting).

## Native-engine review limitation

The historical R1 engine was3.53.3. The [official changes](https://www.sqlite.org/changes.html) and [advisory list](https://www.sqlite.org/cves.html) identify later3.54.0 fixes including malformed-schema recursion and malformed-WAL disclosure; this prompted the reviewed R2 replacement before any native build. R2 headless SQL confirms3.54.1 and source ID `2026-10-16 14:45:20 555e07ef33f69a0957d24d8e555b702464a131d6f1607f4c3c1e4551e8e6ca76`. Runtime receipts also record compile options and the actual loaded DLL hash. This is still a **generated-data feasibility trial**, not acceptance of arbitrary SQL, extensions or untrusted databases.

The static provider declares140 entry points; R2 exports379 functions. Five optional snapshot APIs are absent: sqlite3_snapshot_cmp/free/get/open/recover. Architecture accepts this bounded trial because the tested repository/probe uses ordinary transactions and online backup, with no Snapshot API. Do not claim complete Raw-provider API compatibility; a missing entry point on a used path fails the gate.

Data also recorded a net10/facade-net8/provider-net6 test-host crash using GetBytes/GetStream on a computed BLOB expression. The probe uses GetValue on an ordinary declared BLOB column; its bounded proof is not coverage of those APIs. Retain the failed attempt separately and verify Data's regression/recipe before broader acceptance.

## Reproducible source and native procedure

Restore the frozen ProviderGraph using the pinned repository SDK and package cache, preserving its lock file. Build tested Data source separately. [prepare.py](../prototypes/StateRepositoryUnityProbe/prepare.py) requires the exact clean Data commit and DLL hashes from `data-provider-pin.json`, committed clean GUI inputs, a new target below this worktree's artifacts and an already populated NuGet cache. It never starts Unity or fetches runtime dependencies.

Use [acquire_native.py](../prototypes/StateRepositoryUnityProbe/acquire_native.py) to verify the official ZIP/DLL before staging. Its optional `--archive C:/HobbyDS9/artifacts/tools/sqlite/3.54.1-win-x64/sqlite-dll-win-x64-3540100.zip` reuses the independently acquired official artifact with the same SHA3/DLL checks. This avoids redundant downloads; it does not bypass TLS validation. The active ProviderGraph has no old-native package reference.

```powershell
python prototypes/StateRepositoryUnityProbe/prepare.py `
  --project C:/HobbyDS9/artifacts/worktrees/STATE-REPOSITORY-A2-gui/artifacts/a2-unity-project `
  --nuget-packages C:/HobbyDS9/artifacts/tools/nuget `
  --data-root C:/HobbyDS9/artifacts/worktrees/STATE-REPOSITORY-A2-data-pin `
  --data-source 76b275632a2d34281b0f37911107ec8b7d96095e `
  --native-root C:/HobbyDS9/artifacts/worktrees/STATE-REPOSITORY-A2-gui/artifacts/provider-r2
```

QA then runs6000.6.9f1 with `-batchmode -quit -projectPath <generated-project> -executeMethod HobbyDS9.StateRepositoryProbe.Editor.StateRepositoryProbeBuild.BuildWindows -logFile <separate-log>`. This is an isolated packaging build, not a gameplay build. The helper verifies staged source/binary hashes, uses an empty probe scene, refuses an existing output and verifies exactly one bundled native DLL with the expected hash. It copies all selected notices and a source/Data/engine build receipt into the output. A successful build still says native execution pending.

Launch the generated StateRepositoryProbe.exe in five **separate cold processes**, with networking disabled for the offline evidence:

```text
--hobbyds9-state-repository-probe --probe-run <new32hexGUID> --probe-phase create
--hobbyds9-state-repository-probe --probe-run <sameGUID> --probe-phase reopen
--hobbyds9-state-repository-probe --probe-run <sameGUID> --probe-phase verify-backup
--hobbyds9-state-repository-probe --probe-run <sameGUID> --probe-phase restore
--hobbyds9-state-repository-probe --probe-run <sameGUID> --probe-phase verify-restored
```

Each exits0 only after its report is written. Reports are under the isolated probe product's persistent directory, `StateRepositoryProbe-v1/<GUID>/Ã‰toile-æ—¥æœ¬èªž`; logs state the full path. The1MiB deterministic payload is synthetic, not a game root. Create proves parameterized Unicode/BLOB commit, rolled-back second row, WAL/FULL, exact byte/hash comparison and SQLite online backup with an open source connection. Cold phases verify original/backup, restore the backup to a separate new database through SQLite backup, and cold-open that restored copy. No live main-file copying, overwrite or cleanup occurs. Record sqlite_version/source_id/compile_options, actual Unity/build identity and phase receipts. GC before/after snapshots and probe elapsed time are observations, not peak memory, game-save performance or foreground-frame acceptance.

Also test a separate purpose-created probe-package copy with e_sqlite3 removed/renamed, an isolated working directory and no developer DLL search path. It must fail explicitly, not find an alternate engine. Invalid run/phase and repeated Create must fail without replacing prior probe evidence. Never modify a sealed UX package to perform this test.

## Receiving status and next gates

Core and builder source compilation passes with zero warnings/errors. The installed Unity-profile runtime/editor reference compilation passes with zero warnings/errors; final handoff rechecks it against the frozen Data76 artifact. Same-source, exact-asset R2 headless smoke passes all five cold-process phases, verifying the loaded native hash,3.54.1/source ID, exact1MiB bytes, rollback, backup and restored copy; repeated Create is rejected. Data's separate R2 fixture suite reports28passes. The GUI C1 scaffold's13portable tests pass; full PB Review runtime/all Editor-test source compilation has zero errors and the inherited DemoRuntimeAcceptanceTests:90 nullable warning. Five new C1 widget fixtures compile but await QA execution. Exact logs/receipts are hashed in the coordination handoff; none of these is native acceptance. Staged-project verification and QA IL2CPP/native results remain separate pending gates.

The headless ExactAssetSmoke executes the same raw exercise and Data bootstrap with explicitly selected Unity-target assets. It cannot clear IL2CPP. A2's isolated project cannot clear coexistence with the full game; D1 must later check full-player imports/linking, exact complete Format18/package5 capture/restore, failure/recovery UI, original preservation and comparable progressed-world/combat/refinement/belt CPU/allocation/frame/disk profiles. The C1 scaffold remains disconnected until tested repository/capture APIs and native gates are received. JSON remains the production fallback; D2 default activation is a separate rollout checkpoint.
