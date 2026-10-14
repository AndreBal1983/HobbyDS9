# Single-player demo backend acceptance

14 October 2026. **Headless implementation candidate, not a packaged native demo.** Separate branch based on PR40; source pin/CI supplied by draft PR delivery. Root owner work, frontend recovery, existing packages/saves and source histories preserved. No Editor/native/GPU lease used, no main merge/default activation.

## Actual behavior

[Contract](singleplayer-demo-contract.md) and [scenario](../scenarios/singleplayer-demo-v1/README.md) define the provisional opt-in profile. The actual Scenario/Simulation/Application path runs existing economy/Science/earned Engineering/paid FIFO production and exact authored-v2 construction provisioning. A deterministic opponent buys only admitted affordable ships and routes owned fleets to the lawful public Bajor goal. Human fleets/orders remain unchanged by AI planning. AI travel shares human stamps/ownership/route validation with only controller/reserved-ID differences; strategic commands use the existing trusted-AI CommandEnvelope executor. Everything stages before whole-turn validation/publication, including resources, routes, built provenance and tactical allocation.

Round policy receives only immutable own and visible-enemy condition snapshots, not private saves, new enemy orders, ammunition or RNG. Pending AI commitments are created before human choice and validated exactly on reload (actor/ID/focus/maneuver/presence); historical round replay retains shared fleet targeting/order validation. Human commits resolve one round, never future rounds. Final aftermath remains an explicit human action, and persistent stocks/route clearing remain authoritative.

The fixed defence plan/policy/actor/goal and last-planned phase boundary persist in wrapper4; there is no private player-visible AI diagnostic trace or hidden forward-simulation state. Terminal evaluation is a separate trusted consumer of actual forces; it publishes only outcome, never opponent counts/stocks/locations or a midgame private strength ratio. All mutating plans/advance reject once terminal; inspection/export remains available.

## Executed evidence

| Gate | Result |
| --- | --- |
| Locked solution restore / Release build | Passed, zero warnings/errors |
| Full .NET solution suite | **1777/1777 passed**, no fail/skip (1742 retained +35 demo cases) |
| Final focused demo cases | **35/35 passed** |
| Actual CLI `--singleplayer-demo` | Real paid builds, first completion turn2, five total constructions, one genuinely earned human mature project; human-only commands + lawful AI, interactive battle rounds/explicit aftermath, **Victory at Planning turn13** |
| Canonical/repeated/reversed/pending-reload | Exact complete-save equality; test also reorders actor/fleet/design/contact lists |
| Extended seed fixtures | Four additional seeded bounded campaigns, each repeated with reload equality and nonnegative credits; walkthrough caps deterministic work at128 steps so a stall fails instead of hanging |
| Transactions/privacy/replay | Six phase-failure rollbacks, late completion rollback/retry, human orders preserved, AI local-resource rejection, duplicate human round rejection, no automatic human resolution/finalization, forged wrapper/plan/pending-AI rejection, finite deadline and terminal mutation rejection |
| Default/physical/integrated preparation | Passed; portable DLLs generated in isolated checkout only |
| Default/review/integrated static checks | Passed; PE/API/DLL/raw source/reference and launch/slot checks, **no Unity execution** |
| Independent read-only review | Found incomplete backend nested exports and forgeable pending AI choices; fixed with regressions. Second pass found no further critical defects; added bounded walkthrough and terminal command gates as recommended. Coordinator exact-pin review still required |
| Native UI/Editor/IL2CPP | Not executed for this new profile; Secondary owns binding/codec/resources/save namespace and combined acceptance |

Python static checks used the existing bundled runtime because the ordinary `python` command is a Windows Store alias. No software/account changes were made.

## Independent vectors and compatibility

New exact raw-LF package SHA-256: `bb76fb9f03299f92ca009b46bc931ff5bd75a27018c02d31c6f4068b21ed12a4`.

New canonical complete demo-save SHA-256: `4997ed0183765fa77fd7585182e28bbc4be452bcc428a1968be6337eed249204`, pinned independently in the actual-flow test after executed CLI verification. No old golden was changed.

Existing integrated package/save remain `88a919f6f51d244f79bb638ba933ff3c17305f25231a6b793aa6d53fca5f688b` / `417d1852e052b4fe1f4f2b5f611ba49b82b7d76cd6154540a58bcaefc474e865`, confirmed by actual existing CLI. Physical faster/slower vectors remain `a6a2522a4f389b0bb8aa562d6273b0f02b3ea1fdeb57c7fe21d8368e4fa4d177` / `77073cc154b78dbca2e5f6fa1a618cae490d11410f51f4cb2bb9e640a33210c4`, including canonical/reverse/repeated/reload/reordered checks. Legacy scenario hash remains `3b80eb970c5d832550fc947344e860777910d80208af0fc2f474a98ae04e0b59`; retained full-suite goldens protect legacy RNG `d8919406d6e01cf1`.

Version matrix: demo4 → integrated3 → physical2 → lifecycle1 → campaignSave14/content11; existing rules/compiler/resolver IDs unchanged. Explicit policy IDs bind provisional demo choices. Older loaders/fixtures keep their contracts; there is no migration or implicit wrapping. Direct backend/facade incomplete exports fail closed for demo sessions.

## Remaining release gates

Exact-head Linux/Windows/macOS CI and coordinator persistence/API review before merge. New profile must be integrated into a clearly named demo launch mode, one-human controller/host, complete wrapper4 codec/store, separate save namespace, objective/outcome presentation and production/round/aftermath screens. Do not overwrite old saves/packages or point the existing recovery binary at unsupported saves. Actual new native gameplay, keyboard/modal/resize/audio/performance acceptance remains required; these headless tests do not establish a usable Windows release.

The AI is a deterministic bounded defence/procurement policy, not all five difficulties, full personality/long-term strategy, exploration, diplomacy, refit or general custom-v2 ship design. Finite20-turn contact freshness is intentionally longer than the12-turn demo; no dynamic sensors/infinite tracking or new combat balance is claimed. Demo objective compares surviving hull presence, not planet ownership.
