# HobbyDS9

Gameplay design: [decision index](docs/design/README.md), [strategic AI](docs/design/strategic-ai.md), and [modular ship design](docs/design/ship-design.md). These describe the agreed direction and proposed details; the implementation and acceptance status below remain separate.

Current candidate: **Science & Engineering foundation v1** on `codex/science-engineering-foundation-v1`: save **13**, content **9**, rules **science-engineering-v1**, unchanged phased combat resolver v1. R1/R2 revise the unaccepted candidate to 600-unit fixed-point research, six simultaneous Science allocations and typed Engineering projects, with the Unity Research panel. Main stays unchanged pending local Unity acceptance. See [research rules and acceptance](docs/science-engineering-v1.md).

A portable C# strategy simulation for a future DS9/Dominion War-era BOTF spiritual successor. The strategic vertical slice now connects colony economy, research, ship production, fleets, graph movement and simple deterministic AI. It runs headlessly; the campaign encounter checkpoint is integrated; deterministic planned range-phase combat and generic campaign consequences are implemented; the Unity strategic client consumes the Application boundary; networking is deferred. No implementation code or assets have been copied from BotF2 or 3DBOTF2.

Read [the architecture](docs/architecture.md) for contracts, deterministic rules, scenario definitions and acceptance gates.

## Solution

```text
HobbyDS9.slnx
src/
  HobbyDS9.Contracts/        # IDs, explicit DTOs, commands/results, snapshots, combat contracts
  HobbyDS9.Simulation/       # Owned session, validation, turn systems, RNG, AI
  HobbyDS9.Application/      # Client facade, session creation/load, content/navigation views
  HobbyDS9.Scenarios/        # Portable content boundary
  HobbyDS9.Headless/         # JSON/file/hash adapters and autonomous console runner
tests/
  HobbyDS9.Simulation.Tests/ # Foundation, gameplay, rollback, persistence and acceptance
scenarios/ds9-slice/         # Separate DS9 content package
docs/architecture.md
```

Contracts, Simulation, Scenarios and Application target `netstandard2.1` with C# 9; Headless and Tests target `net10.0`. Production projects have no external packages, Unity/WPF/network dependencies or legacy references. Unity 6 can consume the portable libraries through an adapter; the integrated Unity/IL2CPP client passed user-reported local acceptance (see below). JSON/file I/O stays in the host.

## Client application boundary

A future client references `HobbyDS9.Application` and Contracts. `CampaignApplication.StartScenario(scenario)` creates an `ICampaignSession`; `LoadSave(scenario, save)` creates a separate validated session. Hosts deserialize scenario/save DTOs, hash the exact scenario bytes and write exported saves. Application constructs compatibility metadata and owns the Simulation session privately. `ValidateScenario` / `ValidateSave` return stable validation issues for host diagnostics.

`CaptureSnapshot()` returns immutable campaign facts, content labels/research definitions, a navigation projection and accepted player command IDs for the current planning turn. `SubmitCommand` accepts the existing closed command envelope with caller-supplied IDs and expected turn/revision. Production, cancellation, research and fleet routing all use the existing human-control validation. Rejections never refresh stamps or retry silently.

`AdvanceTurn` can return `AwaitingCombat`. Read detached requests, call `ResolveCombat(request)` or `ResolveCombat(request, plans)`, submit each result with the observed stamp, then explicitly `FinalizeCombat`. Resolution is pure; submission buffers; finalization applies consequences. Saving is legal at every checkpoint. Invalid load/resolver inputs throw without replacing or mutating a live session; ordinary command/turn/submission rejections use the existing result codes.

Navigation has its own `NavigationNodeId`, optional system association, explicit link/route traversal cost, `FindRoute` and `SubmitFleetRoute`. The current adapter maps existing graph nodes one-to-one and preserves unit costs/BFS. Clients should use node associations rather than infer systems from node ID strings, and must not treat cost as a turn estimate. Voronoi geometry, independent navigation topology, weighted movement and budgets are deferred. No presentation state or new gameplay is added.

The Application boundary preserves deterministic behavior. Candidate versions and baseline are in the milestone document; the accepted population/economy reference below is historical. Tests compare Application against direct Simulation through all 25 turns and pending/partial/fully buffered reloads in both result orders.

## Build and headless acceptance

Install the SDK pinned in `global.json`, then run:

```sh
dotnet restore HobbyDS9.slnx --locked-mode
dotnet build HobbyDS9.slnx -c Release --no-restore
dotnet test HobbyDS9.slnx -c Release --no-build --no-restore
dotnet run --project src/HobbyDS9.Headless -c Release --no-build --no-restore -- --turns 25
```

The host uses `ICampaignSession` through Application, with no direct Simulation reference. It loads the bundled scenario (or an optional scenario path), hashes its exact UTF-8 bytes, validates it and advances the requested number of turns. When AwaitingCombat is reached, the host resolves detached requests with DeterministicPhasedCombatResolver, buffers each result and explicitly finalizes before continuing. `--combat-order canonical` (default) or `--combat-order reverse` chooses deterministic host order. It prints turn/revision, AI command count, encounters/checkpoints, phases/shots/hits, shield/hull losses, destruction/retreat counts, entities and RNG/allocation state and a diagnostic save hash. With no `--turns` it only validates/reports initial state. The hash is a host diagnostic, not a permanent canonical serialization format.

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

SimulationSession owns copied authoritative state. Commands use stable IDs, expected turn/revision and a persisted planning-turn deduplication ledger. Commands include SetResearchAllocation, SetScienceAllocation, SetEngineeringTarget (including clear), EnqueueProduction, CancelProduction, SetFleetRoute and SetWorldGovernancePolicy. Ordinary invalid actions return stable error codes; rejected commands pay, allocate and consume nothing. Snapshots contain detached immutable projections of civilizations, colonies/queues, ship designs, ships, fleets and routes.

AdvanceTurn runs AI planning, Economy, Research, Production and Movement on deep staging state, then detects encounters. Without battles it completes the current turn and reopens Planning; with battles it commits AwaitingCombat at the current turn. Every accepted AI command increments staged revision once, and the checkpoint/turn commit increments it once more. The ledger clears only when the turn completes, including explicit combat finalization. A failure anywhere discards all staged work, including RNG, IDs and newly built entities. EncounterCreation groups post-Movement fleets from different civilizations and freezes an AwaitingCombat batch at the current turn. TurnResult returns ordered AI command results and empty strategic events.

Production reserves local named resources and credits when enqueued, uses post-Economy industry on the queue head, completes at most one item per colony and discards excess. Cancellation refunds the unused proportion with exact integer arithmetic, without overflowing the intermediate product. Completed ships join the lowest stationary owned fleet or create one; allocation order is ship then fleet when both are needed. Routes preserve caller order and move at most one normal/wormhole edge per turn.

AI uses the same gameplay handlers through an internal trusted path. Public submissions remain Human-only. AI retains authored 70/30 research allocation, starts with six equal Science weights, redistributes maxed weights deterministically among nonmaxed disciplines, and selects eligible Engineering targets by remaining cost/ID. It chooses affordable designs by industry cost/ID, then routes idle fleets toward adjacent foreign (score 2) or neutral (score 1) systems. Only an actual highest-score tie consumes campaign RNG. Deterministic command IDs use a reserved `ai-command.` prefix and never consume entity allocation.

The scenario has Federation (Human), Cardassian Union and Dominion (AI), six map nodes, five inhabited planets/colonies (Earth and Mars share Sol), a data-defined wormhole, one starting scout/fleet per faction, two scout designs and one Engineering application with cross-discipline Science requirements. The earlier population/economy checkpoint used save format **10** and content format **6**, which deliberately reject older schemas without migration. Saves preserve queue progress, research, ships, fleet membership and routes and pending/resolved combat against the matching content/rules identity (population-economy-v1).

Tests cover existing foundations, economy/research arithmetic, production/refunds, references and membership, route/movement rules, phase isolation, complete rollback, AI ordering/control/RNG, and 25-turn equality across repeated runs, reordered input and multiple save/reloads including pending, partially buffered and fully resolved combat checkpoints. All six three-battle submission permutations produce the same final save. The combat-boundary scaffold remains tested and supplies shared campaign validation/canonicalization. AwaitingCombat blocks strategic commands/advancement and supports persisted pending/resolved combat. Resolved results buffer without applying consequences. Explicit canonical finalization applies sparse damage/destruction/adjacent retreat, clears the batch/ledger and opens the next turn. Different civilizations sharing a system temporarily count as opposing; all fleets there participate in one frozen request. Each encounter consumes one global ID and campaign RNG draw; resolution/finalization consume neither. NoLossCombatResolver retains its own fixed identity and reference tests. The production resolver emits a detailed phase/attack/impact/destruction/retreat trace; presentation and richer tactical systems remain deferred.

No distribution license is selected yet. Choose one before importing third-party code or distributing the project.

## Deterministic combat

Ship designs define aggregate shields, Long/Medium/Close attack, accuracy and evasion. Every battle request freezes these stats and MaxHull alongside current hull. `IPlannedCombatResolver` accepts one detached plan per fleet; plans are not campaign state or strategic commands. Quick resolution uses FocusFire / Standard / Overcharge, LowestHullRatio, and no voluntary retreat for every fleet.

Phases use simultaneous volleys: all living ships choose phase-start enemy targets and roll in ordinal ship order before applying aggregated damage. Only one encounter-local bounded hit roll is drawn per positive attack; zero attacks and absent targets draw nothing. Tactics modify damage, accuracy and evasion with integer basis points; exact modifiers and formulas are in [architecture.md](docs/architecture.md#deterministic-phased-combat-rules). Target priorities are exact hull ratio, phase attack, or ship ID, with ordinal ties. Shields begin full each battle and never regenerate or persist. Damage reaches shields then hull; destruction and post-volley threshold retreat return sparse generic consequences. BigInteger keeps scaling, aggregation and ratios safe across valid Int64 stats.

Events identify phases, firing/target ships, hits/misses, actual shield/hull losses, destruction and retreat. They contain no coordinates or timing and never drive campaign consequences. No Unity, individual weapons, tactical movement, faction battle bonuses or persisted replay history is implemented.

The historical accepted population/economy v1 baseline reaches **15 checkpoints / 18 encounters**, **49 phases / 113 shots / 75 hits**, **128 shield / 133 hull damage**, and **13 destroyed ships/fleets**. It ends at turn **26**, revision **187**, with **129 AI commands**, **6 ships/fleets**, next sequence **80**, and RNG `850d3ff45bd6b85e`. Canonical/reverse final save SHA256:

```text
65a4f6b452fd8c6d8a80c775e6e74e944ac73f115bc3e7b9fb3e4e64da60f175
```

The new economy includes explicit planets, mixed cohorts, automatic habitat allocation, installed infrastructure, local Food/Dilithium logistics, stability, migration and durable displacement. Generic Materials are removed. Save **10**, content **6** and rules **population-economy-v1** reject older formats; combat resolver ID/version are unchanged. See [rules, conservation totals and deliberate limits](docs/economy-population-v1.md).

## Unity client

The Unity 6000.6.5f1 URP scaffold and first client implementation are under [client/HobbyDS9.Unity](client/HobbyDS9.Unity/README.md). Prepare portable DLLs and the scenario with `dotnet msbuild scripts/PrepareUnity.proj`. The client uses Application snapshots and explicit combat checkpoints. Local Unity 6000.6.5f1 acceptance passed compilation, 4/4 EditMode tests, gameplay/combat flow, and a launched Windows x64 IL2CPP build. CI also verifies portable integration; display checks remain part of future UI changes.

The integrated client retains research, empire-wide production enqueue/cancel, weighted route preview/submission and partial travel, population/economy inspection, politics/governance and save/load. Its combat UI auto-resolves AI-only encounters without interruption, exposes only human fleet plans, and retains recent reports. The responsive LCARS shell and music/SFX services remain presentation-only.

The accepted source handoff is `34c2c6660d2901de2257d808e1b852504bd38b50`. User-reported local checks on Unity 6000.6.5f1: **50/50 EditMode passed**, Windows x64 IL2CPP build succeeded, music/crossfade and LCARS SFX playback passed, independent audio controls passed, responsive Windows-player UI and AI-only/human combat/report flow were exercised. Consolidation restores three accidentally deleted headless-safe elbow cases, for **53 authored cases**; cloud has no Editor and does not claim a new Unity execution.

See [consolidation audit](docs/integrated-acceptance-consolidation-v2.md), [LCARS/SFX verification](docs/lcars-sfx-foundation.md) and [audio provenance/hashes](docs/audio-sources.md). The new candidate uses save/content 13/9 and rules `science-engineering-v1`, with unchanged phased combat resolver v1. The Research section now shows Science & Engineering; client bootstrap enables VSync with no forced frame-rate cap. Graphics menus remain deferred. Current Unity changes require fresh local acceptance; prior acceptance above describes the accepted base.
