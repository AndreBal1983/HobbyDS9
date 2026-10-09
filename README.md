# HobbyDS9

A new, portable C# strategy simulation project. This repository contains portable identity/session foundations, the Phase 3A civilization/scenario and galaxy graph model, the Phase 3B turn transaction, the Phase 4A colony economy, Phase 4B civilization research, and tested combat-boundary helpers. It is not an implemented game.

Read [the Phase 1 architecture](docs/architecture.md) before implementing behavior. It defines state/command contracts, deterministic turn rules, explicit save DTOs, the fixed scenario, and required tests. No implementation or content files have been copied from BotF2 or 3DBOTF2.

## Solution

```text
HobbyDS9.slnx
src/
  HobbyDS9.Contracts/        # IDs, commands, DTOs, combat contracts
  HobbyDS9.Simulation/       # Rules, turn lifecycle, seeded RNG, AI
  HobbyDS9.Scenarios/        # Content DTO validation/construction
  HobbyDS9.Headless/         # Console composition root
tests/
  HobbyDS9.Simulation.Tests/ # Foundation and combat boundary tests; gameplay tests follow
scenarios/
  ds9-slice/                # Separate scenario content package
docs/
  architecture.md
```

## Build

Install the SDK pinned in `global.json`, then run from this directory:

```sh
dotnet restore HobbyDS9.slnx --locked-mode
dotnet build HobbyDS9.slnx --no-restore
dotnet test HobbyDS9.slnx --no-build --no-restore
dotnet run --project src/HobbyDS9.Headless --no-build
```

For this cloud workspace, [scripts/bootstrap-dotnet.sh](scripts/bootstrap-dotnet.sh) installs the pinned Linux x64 SDK outside the checkout and verifies its SHA512 before extraction:

```sh
bash scripts/bootstrap-dotnet.sh
export DOTNET_ROOT=/workspace/.tools/dotnet
export DOTNET_CLI_HOME=/workspace/.cache/hobbyds9/dotnet-home
export NUGET_PACKAGES=/workspace/.cache/hobbyds9/nuget
export PATH="$DOTNET_ROOT:$PATH"
```

The headless executable loads the bundled ds9-strategic-slice-v1 JSON skeleton (or a path supplied as its first argument), computes its content hash, validates it, and reports civilization/node/edge counts. It does not simulate turns or claim playable scenario readiness. Tests cover library targets/dependencies, typed canonical IDs, persisted global allocation and overflow, campaign RNG known vectors/reload, foundation DTO validation, combat DTO persistence, resolution-order independence, buffered save/resume and retreat restrictions. Common state/catalog DTOs now include civilization controllers, systems/ownership, colonies/population/stocks, civilization credits, economic profiles and graph edges, without a playable campaign save. The narrow combat helper can allocate frozen requests, validate/buffer results and guard planning; it does not apply campaign consequences. The session owns these foundation facts and atomically advances economy/research turns; production, fleets, movement, AI and combat integration remain pending.

Contracts, Simulation and Scenarios target `netstandard2.1` with C# 9; Headless and Tests target `net10.0`. Production projects have no external packages or references to UI engines, networking libraries, or legacy repositories. Unity 6 can consume the portable libraries through an adapter; Unity/IL2CPP integration remains to be verified. JSON/file I/O stays in the host.

## Implemented foundation

Contracts contains EntityId, DefinitionId and explicit identity/state/save DTOs. Simulation contains EntityIdAllocator, SplitMix64Random and FoundationValidator. JSON remains a host concern. SimulationSession owns copied authoritative state, validates command envelopes and expected turn/revision, persists accepted command IDs, and exports detached immutable snapshots and save DTOs. SetResearchTarget is functional; other gameplay commands return UnsupportedCommand. Internal test handlers verify the transaction boundary. Save format 6 adds research reserve/target and required unlocked-node lists; versions 1–5 are rejected. Content format 3 adds research nodes and required prerequisite lists; earlier content formats are rejected. The Economy phase uses data-driven checked integer arithmetic and preserves campaign RNG/allocation state.

GitHub Actions runs locked restore, Release build and the full test suite on pushes and pull requests across Linux, Windows and macOS, using global.json.

## Next implementation increment

Session ownership and command/snapshot foundations and Phase 3A identity/graph tests are implemented. Phase 3A adds persisted Human/AI controllers, system ownership, canonical Normal/Wormhole graph edges, deterministic BFS and route validation. Session actor/control identity derives from civilization state rather than host-supplied registries. Tests cover ordering independence, graph errors, known routes, controller persistence and save/reload. The fixed scenario now has four colonies: Sol, Cardassia and the Founders' Homeworld are owned; Bajor is neutral. All start at population 1000/capacity 2000, food 20/materials 10; civilizations start with 50 credits. Phase 3B adds AdvanceTurn(expectedTurn, expectedRevision), which completes the current turn, increments turn/revision once, clears the command ledger and opens Planning. RNG/allocation and protected non-economic/non-research facts remain unchanged. Failures roll back staging completely. TurnResult distinguishes Completed, Rejected and future AwaitingCombat, with empty ordered events/results. Persisted AwaitingCombat blocks commands/advancement; encounter state and resolution remain deferred. Tests cover lifecycle, injected failures, persistence, overflow, retries and input-order determinism. Phase 4A Economy adds food/material output, food demand/consumption, bounded growth/starvation and owner credit income, using pre-growth population for demand/income. Tests cover exact arithmetic, neutral colonies, ordering, continuation, numerical/reference invariants, overflow rollback and no-op phase isolation. Phase 4B adds checked owned-colony research accumulation, civilization reserve/target/unlocks, prerequisite validation and the public SetResearchTarget command. A same-target request returns ResearchTargetUnchanged without changing revision. The generic scenario node costs 9 and completes in three turns at output 3 once selected; AI factions retain reserve without choosing targets. Research surplus persists, and at most one node per civilization unlocks per turn. Tests cover commands, prerequisites/cycles, surplus, ordering, reload, phase isolation and full-turn rollback. Industry remains metadata only; no derived per-turn output is persisted. Production, ship designs, fleets, movement, AI and combat integration remain unimplemented. Follow the ordered implementation plan and acceptance gates in the architecture document.

No distribution license is selected yet. Choose one before importing third-party code or distributing the project; do not infer either legacy repository's license applies to these independently authored files.

The production milestone Stage A adds ordered ship queues, paid enqueue/cancel commands, exact large-value refunds and transactional ship/fleet completion. Save/content formats are now 7/4. Movement and autonomous AI follow in the next stages; encounter creation remains deferred.

Stage B adds starting scouts/fleets, explicit validated route commands, and deterministic one-edge movement across normal and wormhole edges. Production joins only stationary owned fleets. No encounters are generated.
