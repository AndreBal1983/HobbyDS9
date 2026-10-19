# Branching frontier review fixture

**Provisional, explicitly opt-in, headless only.** This separate package reuses immutable authored ship fits and ordinary portable strategy/economy/combat contracts. It does not change the current twelve-turn Bajor demo or enable exploration in legacy saves.

The human cruiser starts at Sol knowing the Bajor lane. Completing that weighted lane charts immediate public branches toward Badlands and the relay. The Cardassian AI starts with its own separate Cardassia–Badlands knowledge. Arrival reveals only broad node identity/kind and immediate public lanes; waypoint names are labels, not evidence of equipped sensors, body surveys, ownership or diplomacy. Former wormhole topology is explicitly an ordinary lane in this fixture. Navigation has no hazard effects.

Run from the repository root:

```powershell
dotnet restore HobbyDS9.slnx --locked-mode
dotnet build HobbyDS9.slnx -c Release --no-restore
dotnet test tests/HobbyDS9.Simulation.Tests -c Release --no-build --no-restore --filter FullyQualifiedName~FrontierTests
dotnet run --project src/HobbyDS9.Headless -c Release --no-build -- --frontier-review
```

The walkthrough compares full canonical/reordered/repeated/reloaded saves, ending at Planning turn 8 with 9 first-arrival receipts and 13 accepted operations. See [the contract](../../docs/frontier-reconnaissance-v1.md) for API, wrapper5, bounded replay validation, deliberate trust limits and excluded features. Preserve JSON LF bytes through Git; package identity hashes exact resource bytes.
