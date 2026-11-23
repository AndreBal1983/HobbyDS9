# Disposable runtime baseline probe

This console client exercises existing portable simulation and review-host code. It does not change production projects, rules, schemas or owner saves. It creates fresh generated Format 18 campaigns with seed 42, exports disposable JSON for the companion storage probe, and reports individual observations. This is an exploratory baseline, not a benchmark acceptance suite or native Unity result.

## Reproduce

Use a clean runtime source checkout at `2fc0dbca46073121379dd9491c859a02a6f22dcd`, separate from any QA worktree. The default project reference expects the sibling `STATE-REPOSITORY-20261122-runtime-probe` checkout; override `RuntimeSource` when building elsewhere. SDK 10.0.401 was used, via the repository's local tool installation.

The probe verifies the supplied checkout HEAD and clean tracked state before measuring. Set `HOBBYDS9_GIT` to a Git executable when needed; it otherwise uses the local GitHub Desktop binary if present, then `git` from PATH. Build against the same source path that is supplied at runtime.

From this directory in PowerShell:

```powershell
$sdk = 'C:/HobbyDS9/artifacts/tools/dotnet/dotnet.exe'
$runtimeSource = 'C:/HobbyDS9/artifacts/worktrees/STATE-REPOSITORY-20261122-runtime-probe'
& $sdk build RuntimeProbe.csproj -c Release --nologo "-p:RuntimeSource=$runtimeSource"
& $sdk bin/Release/net10.0/RuntimeProbe.dll $runtimeSource '../../../docs/evidence/state-repository-20261122/runtime-30.json' 30 5
```

Repeat sequentially for 100 and 1,000. A 10,000 request deliberately exercises the existing generation cap and returns failure, not a fabricated supported world. Each run has a ten-minute soft budget checked after measured samples; it cannot interrupt an individual engine call. Output is incrementally written after each completed series. Do not launch multiple timing runs concurrently.

An optional fifth argument `worker` measures only startup and the two host save phases. This was used for 1,000 systems after the full probe's turn-advance warmup was rejected, preserving that partial result without rerunning its completed series. Record it in a separate output file.

The fifth argument `validate` instead reads the private 30/100 baseline/successor files, strictly decodes each, loads/replays with the real typed session loader, then exports/encodes and requires identical bytes. It reports hashes and turn/revision, without repeating timing series. Example: use output `runtime-payload-validation.json`, size `30`, samples `1`, mode `validate`.

Disposable `.local/runtime-{size}-baseline.json` and `successor.json` are created beneath the **working directory**. Run here so `.gitignore` excludes them; never point this probe at user saves. Build outputs and these large payloads are intentionally not published. The successor follows one warmup plus five accepted research-allocation commands; stale attempts precede them. The same files are consumed by the storage probe without reserialization.

## What is timed

- Real package factories assemble the same pinned resources as the generated BeltBodies playtest profile. Startup includes generation and session construction, but not preceding package assembly.
- `CaptureStatus`, actor colonisation, belt and refinement observations are timed independently. These are selected public captures, not the full Unity screen-refresh pipeline.
- Reflection calls the unmodified inner `SimulationSession.CopyState` and review-session `ValidateSave` on legitimate state. Neither is the complete outer Format 18 copy/load path; reflection overhead remains included.
- Full session export; Newtonsoft encoding; strict decoding; codec encode/decode copy; and typed session load/replay are separate calls. Typed load starts from an already decoded detached root, so it excludes JSON parsing and file reads.
- Rejected commands use an intentionally stale revision. Accepted commands alternate valid research allocations and accumulate revisions. Command timings include status capture, envelope creation, normal validation/staging and submission. They are not throughput measurements or all rejection categories.
- Each one-turn advance starts from the same legitimate turn-one baseline. Load/reset is outside the measurement; the advance uses the real turn path. No combat is arranged.
- `PhysicalReviewHost.PrepareColonisationCycleCheckpointSave` measures its caller-side full export. The actual one-use `ColonisationCycleCheckpointSaveWork.Execute` measures codec copy plus replay verification. It runs synchronously so current-thread allocation measurement is meaningful. A sink store excludes final store encoding, disk I/O, flush, worker queue/scheduling and UI responsiveness. These results are not total manual-save latency.

## Measurement limits

Each normal operation has one warmup and five observations. Cheap captures have two warmups and twenty observations. Startup is one cold observation. The JSON reports nearest-rank p95/p99, but at n=5 they are only the sample maximum; neither n=5 nor n=20 establishes population tails. One warmup does not stabilize tiered JIT: later combined operations can be faster than earlier component operations. Do not add/subtract series or infer speedups from execution order. Sub-microsecond status timings approach timer/instrumentation limits.

No forced collection, process affinity, CPU power lock or isolated machine was used. Background desktop activity and the sibling storage probe can affect results. Per-operation allocations are `GC.GetAllocatedBytesForCurrentThread` around the call; background/JIT allocations are excluded. GC counts, pause deltas and CPU time are process-wide. Peak working set is the whole process high-water mark including setup, JIT, resets and captures, not live state size or an operation-specific peak.

World sizes increase map facts only: these fresh campaigns retain three colonies, two fleets and two ships. There is no late-game fleet/population growth, long-journal scaling, combat phase, concurrent-producer, native IL2CPP, autosave scheduler, optimized-copy candidate or full end-to-end save benchmark here. Measurements and scope gaps are recorded in [runtime evidence](../../../docs/evidence/state-repository-20261122/runtime-baseline.md).
