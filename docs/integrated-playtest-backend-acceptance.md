# Integrated playtest backend acceptance

14 October 2026. **Backend foundation implemented; unified client/native acceptance remains pending.** Current assignment is the real integrated campaign, not the earlier completed audio-only or fixed-inventory review tasks. Base remote main `c5d2b6ac32e8a6c47b073e58f53e9ecb129c11e4`; isolated branch `codex/integrated-playtest-backend` retains audio PR #35 and central combat PR #36 through merge commits. Root `E:/HobbyDS9` design/art/tools changes, source branches, old players and saves were not reset, overwritten or discarded. No main merge/default activation.

## Implemented foundation

The [API/save contract](integrated-playtest-contract.md) and [scenario](../scenarios/integrated-playtest-v1/README.md) define one actual Scenario/Simulation/Application-owned root for R&D, production, movement, tactical rounds and aftermath. Existing economy, research, fees/resources, industry/FIFO, ship allocation and stationary-fleet join/new-fleet rules run authoritatively. The wrapper admits only explicitly bound static authored templates, validates their existing immutable equipment source, and provisions exact shields/ammunition once at actual completion in the same staged whole-root transaction. It records ship/source/owner/queue/turn/fleet provenance, retains it after destruction, checks completion counters and initializes coherent new-fleet orders/cloak settings.

No scalar v1 blueprint conversion, inferred equipment, general new hull/component/compiler/refit support, future tech grants, production/research buff or new combat formula was added. Default v1 and the existing review sources keep their original semantics and reference hashes. The source template's existing prices/work/fit/stocks are reused; Federation starting working capital 250 is explicit new scenario stock, not a command bypass. Existing Engineering prerequisites must be earned; project progression is real. Its legacy Advanced Scout unlock does not silently admit that unsupported source to v2.

Actor views contain only own civilization/R&D/worlds/queues/ships/fleets, known navigation and admitted construction options. Unknown/uncontrolled actors reject. Tactical/contact views retain their existing privacy boundaries. Application computes private projections internally under a coherent Simulation capture, then returns immutable filtered facts; no renderer receives a global save/core or enemy intentions/economy/research. The inherited physical-only `ExportSave()` fails closed for integrated sessions; the trusted store must use the complete `ExportIntegratedSave()`.

## Executed local checks

| Gate | Result |
| --- | --- |
| Locked solution restore | Passed |
| Full .NET Release build | Passed, **0 warnings / 0 errors** |
| Full solution .NET suite | **1738 passed**, 0 failed/skipped |
| New actual integrated backend cases | **53 passed** |
| Real integrated CLI transcript | Canonical, reversed commits, waiting/turn reload, reordered input agree |
| Default PrepareUnity / opt-in PrepareUnityReview | Passed; no Editor invoked |
| Default static Unity / review import / review binding | Passed |
| Existing B1 / physical headless check scripts | Passed, original vectors retained |
| Legacy 25-turn canonical/reverse/repeated smoke | Identical original save/RNG |

The new tests cover real six-way Science and allocation apply/reload, locked-to-earned Engineering/project completion, paid queue/Industry progress, resource/yard/Engineering eligibility, cancellation/duplicates, existing/new fleet placement, same-system simultaneous completion mapping, different owners, immutable input/source copies, produced-ship actual travel/contact/joint rounds/retained stocks, waiting reload, exactly-once finalization, stale/foreign/invalid commands, rejected arbitrary blueprint/static admission, complete-root privacy and source/save corruption. Six turn-phase fault boundaries, **actual production allocation exhaustion**, and late aftermath failures preserve the whole root including fees/work/RNG/pools/provenance. No toy combatants, fake production or fabricated Engineering progress substitute for the actual lifecycle proof. The isolated same-system mapping test uses a valid paid partial-work checkpoint to force simultaneous completion; the main acceptance separately proves natural Industry over turns.

Headless walkthrough: `dotnet run --project src/HobbyDS9.Headless -c Release --no-build --no-restore -- --review-integrated-playtest`. It builds the paid source at **turn 13**, resolves a produced-ship encounter and retains **3/7 initial torpedoes** on that ship at **turn 17**; earned Advanced Starship Engineering completes at **turn 131** in the same session. Full-root reload/continued turns remain deterministic. This is automation evidence, not a 131-turn manual grinding requirement.

## Versions and independent vectors

Integrated outer package/save **3**, `campaign-tactical-v2-integrated-playtest-1`; nested physical **2**, B1 **1**, campaign Save **14**, authored Content **11**; default content **10** unchanged. Rules `science-engineering-v1`, compiler `ship-design-compiler-v1`, existing movement/tactical resolver identities unchanged. Bare earlier shapes/unknown wrapper versions reject explicitly with no automatic wrapping/migration.

- New integrated package: `88a919f6f51d244f79bb638ba933ff3c17305f25231a6b793aa6d53fca5f688b`.
- New integrated full-root transcript save: `417d1852e052b4fe1f4f2b5f611ba49b82b7d76cd6154540a58bcaefc474e865`.
- Unchanged legacy source: `3b80eb970c5d832550fc947344e860777910d80208af0fc2f474a98ae04e0b59`.
- Unchanged legacy 25-turn save: `fb2b75f18c8c5423f3a0850f38f6e53ee8249d3d08385978b5b2532041cc6089`; RNG **`d8919406d6e01cf1`**.
- Unchanged B1 save: `29110377df8d8ffa3e2f4d2cfdf40392671fb8b87d82d1955ec29def827cf199`.
- Unchanged faster/slower physical saves: `a6a2522a4f389b0bb8aa562d6273b0f02b3ea1fdeb57c7fe21d8368e4fa4d177` / `77073cc154b78dbca2e5f6fa1a618cae490d11410f51f4cb2bb9e640a33210c4`.

These are new independent integrated vectors, not updates to old goldens to accommodate a regression. `.github/workflows/integrated-playtest-backend.yml` adds Windows/Linux/macOS actual backend cases and same-session transcript on the exact draft head; existing CI continues full regressions/legacy checks. Final source SHA and run evidence belong in the PR/ignored receipt, avoiding a report self-hash.

## Remaining explicit gates and handover

- **Contact freshness remains 20 turns.** Construction/contact proof is inside that authored window. Late contact after long research is not implicitly refreshed by hidden positions or co-location. Keeping the finite controlled window, changing the explicit review window, or implementing lawful dynamic observation are distinct future choices. The expiry behavior is tested and documented; no speculative policy activation occurred.
- Starting scenario is trusted human hotseat. General strategic-AI v2 production/design and arbitrary frozen-v1 admissions remain outside this template foundation; shared control/prices/prerequisite rejection stays authoritative. Mixed neutral connected sides and other previously proposed policies remain gated.
- Secondary must bind the real actor strategy/R&D/production/navigation panels and complete-format3 host/store to this same instance, using the published contract. Existing format2 stores/resources cannot be treated as integrated saves by aliasing their types.
- No combined Unity EditMode, new IL2CPP build, native GUI/audio/render/input acceptance or desktop/sound-setting changes were performed for this backend increment. Audio PR #35's independent 128-test evidence and PR #36's independent UI evidence are separate. PM will dispatch one combined acceptance/build after both deliveries; physical audibility/render/input remain honest manual gaps.
- Isolated Unity import-generated line-ending/status/settings files from prior audio acceptance remain uncommitted; they were not included in this backend change. Artifacts/checkpoints are preserved. No native build overwrote the old/current player.

Stop at the reviewable draft backend checkpoint after exact-head CI. Do not merge, activate default gameplay or start another milestone before the coordinated handover.
