# HobbyDS9

A portable C# strategy simulation for a future DS9/Dominion War-era BOTF spiritual successor. The strategic vertical slice now connects colony economy, research, ship production, fleets, graph movement and simple deterministic AI. It runs headlessly; the campaign encounter checkpoint is integrated; generic combat consequences and the production no-loss resolver are implemented; frontend and networking follow in later stages. No implementation code or assets have been copied from BotF2 or 3DBOTF2.

Read [the architecture](docs/architecture.md) for contracts, deterministic rules, scenario definitions and acceptance gates.

## Solution

```text
HobbyDS9.slnx
src/
  HobbyDS9.Contracts/        # IDs, explicit DTOs, commands/results, snapshots, combat contracts
  HobbyDS9.Simulation/       # Owned session, validation, turn systems, RNG, AI
  HobbyDS9.Scenarios/        # Portable content boundary
  HobbyDS9.Headless/         # JSON/file/hash adapters and autonomous console runner
tests/
  HobbyDS9.Simulation.Tests/ # Foundation, gameplay, rollback, persistence and acceptance
scenarios/ds9-slice/         # Separate DS9 content package
docs/architecture.md
```

Contracts, Simulation and Scenarios target `netstandard2.1` with C# 9; Headless and Tests target `net10.0`. Production projects have no external packages, Unity/WPF/network dependencies or legacy references. Unity 6 can consume the portable libraries through an adapter; Unity/IL2CPP execution remains to be verified. JSON/file I/O stays in the host.

## Build and headless acceptance

Install the SDK pinned in `global.json`, then run:

```sh
dotnet restore HobbyDS9.slnx --locked-mode
dotnet build HobbyDS9.slnx -c Release --no-restore
dotnet test HobbyDS9.slnx -c Release --no-build --no-restore
dotnet run --project src/HobbyDS9.Headless -c Release --no-build --no-restore -- --turns 25
```

The host loads the bundled scenario (or an optional scenario path), hashes its exact UTF-8 bytes, validates it and advances the requested number of turns. When AwaitingCombat is reached, the host resolves detached requests with NoLossCombatResolver, buffers each result and explicitly finalizes before continuing. --combat-order canonical (default) or reverse chooses deterministic host order. It prints turn/revision, AI command count, generated/resolved combat and checkpoint counts, entity counts, RNG/allocation state and a diagnostic save hash. With no `--turns` it only validates/reports initial state. The hash is a host diagnostic, not a permanent canonical serialization format.

For this cloud workspace, [scripts/bootstrap-dotnet.sh](scripts/bootstrap-dotnet.sh) installs the pinned Linux x64 SDK outside the checkout and verifies SHA512 before extraction:

```sh
bash scripts/bootstrap-dotnet.sh
export DOTNET_ROOT=/workspace/.tools/dotnet
export DOTNET_CLI_HOME=/workspace/.cache/hobbyds9/dotnet-home
export NUGET_PACKAGES=/workspace/.cache/hobbyds9/nuget
export PATH="$DOTNET_ROOT:$PATH"
```

GitHub Actions runs locked restore, Release build and the full test suite on pushes and pull requests across Linux, Windows and macOS, plus 25-turn headless runs in both combat resolution orders.

## Implemented strategic slice

SimulationSession owns copied authoritative state. Commands use stable IDs, expected turn/revision and a persisted planning-turn deduplication ledger. All four commands are functional: SetResearchTarget, EnqueueProduction, CancelProduction and SetFleetRoute. Ordinary invalid actions return stable error codes; rejected commands pay, allocate and consume nothing. Snapshots contain detached immutable projections of civilizations, colonies/queues, ship designs, ships, fleets and routes.

AdvanceTurn runs AI planning, Economy, Research, Production and Movement on deep staging state, then detects encounters. Without battles it completes the current turn and reopens Planning; with battles it commits AwaitingCombat at the current turn. Every accepted AI command increments staged revision once, and the checkpoint/turn commit increments it once more. The ledger clears only when the turn completes, including explicit combat finalization. A failure anywhere discards all staged work, including RNG, IDs and newly built entities. EncounterCreation groups post-Movement fleets from different civilizations and freezes an AwaitingCombat batch at the current turn. TurnResult returns ordered AI command results and empty strategic events.

Production pays materials/credits when enqueued, uses post-Economy industry on the queue head, completes at most one item per colony and discards excess. Cancellation refunds the unused proportion with exact integer arithmetic, without overflowing the intermediate product. Completed ships join the lowest stationary owned fleet or create one; allocation order is ship then fleet when both are needed. Routes preserve caller order and move at most one normal/wormhole edge per turn.

AI uses the same gameplay handlers through an internal trusted path. Public submissions remain Human-only. AI chooses legal research by cost/ID and affordable designs by industry cost/ID, then routes idle fleets toward adjacent foreign (score 2) or neutral (score 1) systems. Only an actual highest-score tie consumes campaign RNG. Deterministic command IDs use a reserved `ai-command.` prefix and never consume entity allocation.

The scenario has Federation (Human), Cardassian Union and Dominion (AI), six map nodes, four colonies, a data-defined wormhole, one starting scout/fleet per faction, two scout designs and one research node. Save format **8** and content format **4** deliberately reject older schemas without migration. Saves preserve queue progress, research, ships, fleet membership and routes and pending/resolved combat against the matching content/rules identity (phase-1-campaign-combat-v1).

Tests cover existing foundations, economy/research arithmetic, production/refunds, references and membership, route/movement rules, phase isolation, complete rollback, AI ordering/control/RNG, and 25-turn equality across repeated runs, reordered input and multiple save/reloads including pending, partially buffered and fully resolved combat checkpoints. All six three-battle submission permutations produce the same final save. The combat-boundary scaffold remains tested and supplies shared campaign validation/canonicalization. AwaitingCombat blocks strategic commands/advancement and supports persisted pending/resolved combat. Resolved results buffer without applying consequences. Explicit canonical finalization applies sparse damage/destruction/adjacent retreat, clears the batch/ledger and opens the next turn. Different civilizations sharing a system temporarily count as opposing; all fleets there participate in one frozen request. Each encounter consumes one global ID and campaign RNG draw; resolution/finalization consume neither. NoLossCombatResolver produces one terminal event and unchanged hull/local RNG. Tactical combat remains deferred.

No distribution license is selected yet. Choose one before importing third-party code or distributing the project.

The fixed 25-turn acceptance reaches 23 combat checkpoints and resolves 44 encounters. It ends at turn 26/revision 374 with 282 AI commands, 21 ships/fleets, 2 queued items, 2 research unlocks, next entity sequence 101 and campaign RNG `82f866ceb4d760e1`. Canonical and reverse resolution produce the same save.
