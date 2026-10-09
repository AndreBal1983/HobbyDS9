# HobbyDS9

A new, portable C# strategy simulation project. This repository currently contains the architecture and a buildable solution scaffold with tested combat-boundary helpers, not an implemented game.

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
  HobbyDS9.Simulation.Tests/ # Framework and combat DTO checks; gameplay tests follow
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

The headless executable currently reports scaffold status and exits. It does not simulate turns or claim scenario readiness. Tests verify library targets/dependencies, combat DTO persistence, encounter-local RNG vectors/allocation, resolution-order independence, buffered save/resume, ID/participant validation and retreat restrictions. The narrow combat helper can allocate frozen requests, validate/buffer results and guard planning; it does not apply campaign consequences. The campaign session, actual turn lifecycle, production resolver and gameplay systems remain pending.

Contracts, Simulation and Scenarios target `netstandard2.1` with C# 9; Headless and Tests target `net10.0`. Production projects have no external packages or references to UI engines, networking libraries, or legacy repositories. Unity 6 can consume the portable libraries through an adapter; Unity/IL2CPP integration remains to be verified. JSON/file I/O stays in the host.

## Next implementation increment

Implement stable IDs, explicit DTOs, validation and the specified RNG with known-vector tests first. Follow the ordered implementation plan and acceptance gates in the architecture document.

No distribution license is selected yet. Choose one before importing third-party code or distributing the project; do not infer either legacy repository's license applies to these independently authored files.
