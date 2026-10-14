# Unified integrated playtest — client/UI acceptance

14 October 2026. **One-session integrated client implemented; real portable and graphics-enabled Unity EditMode checks passed. Draft delivery; combined native build/input/visual acceptance remains pending.** This is the current integrated-campaign assignment, not historical UX documentation or the completed central-only review.

## Source and ownership

Branch `codex/integrated-playtest-shell` preserves early navigation `82ac71873e23eaf0c13719a9f5ac12feca4cf463`, central [PR #36](https://github.com/AndreBal1983/HobbyDS9/pull/36) `890c6b70ee81296cff34973fdb8fc166af1ec858`, and current main ancestor `c5d2b6ac32e8a6c47b073e58f53e9ecb129c11e4`. Ordinary merge `761cfa47b358baf6e9da52e76e4638b3d0657252` incorporates the final backend [PR #37](https://github.com/AndreBal1983/HobbyDS9/pull/37), exact pin **8a9ece2fa35a72b2dd53736856af181f07f601d0**, including audio PR #35 history. No force-push, main merge, root reset or existing package/save/art replacement.

Read [the final implemented API contract](integrated-playtest-contract.md), [backend acceptance](integrated-playtest-backend-acceptance.md), [architecture](architecture.md), [design index](design/README.md), [Science/Engineering](science-engineering-v1.md), [blueprint construction](blueprint-construction-v1.md), [ship design](design/ship-design.md), [movement](design/time-movement-mobilisation.md), [tactical combat](design/tactical-combat.md), [physical travel](campaign-tactical-v2-physical-travel.md) and [accepted central layout](art/combat-interface.md). The contract heading's “interim” is historical; its first paragraph/final evidence establish the delivered API. The early [UI/seam plan](integrated-playtest-ui-seams.md) remains preparation history; its missing-backend statements are superseded by this implementation.

This increment owns the client/controller/host/codec/store, review UI/navigation, additive opt-in import/build-entry seams and related tests. No new changes to Primary's Simulation/Application/Contracts/scenarios or audio sources are made. Their accepted histories are inherited. Default v1 and old physical-review saves/resources/behavior remain separate.

## Actual one-session behavior

The integrated `ReviewUiModel` owns one `PhysicalReviewHost` whose single session is `CampaignIntegratedPlaytestApplication.StartPackage` / `LoadSave`. `ReviewFrame.Strategy` contains Primary's immutable `CaptureStrategy(actor)` projection. No parallel legacy campaign, global campaign snapshot, private save DTO or nested-root export reaches a renderer.

| Screen/action | Actual authority and presentation |
| --- | --- |
| Map | Known filtered nodes/links/names/costs, own worlds/fleets/ships, actual fleet sector/progress and permitted last-seen contact age/freshness. Schematic coordinates are presentation only; no current enemy route/position marker is invented |
| Map selection | Select a known destination, then an owned fleet or ship to open Fleet orders with that selection. Inspect owned condition using existing lawful inspection |
| R&D | Real output/reserves, Science percentages/levels/progress/breakthroughs, bounded Engineering bands/costs/prerequisites/availability and latest receipts from Application |
| R&D edits | Local desired split/six-weight drafts; explicit reviewed existing SetResearchAllocation/SetScienceAllocation/SetEngineeringTarget commands. Unchanged Apply is disabled; no client spending/progression/technology grant |
| Production | Only Primary's expressly admitted authored templates. Actual credit/resource/Industry prices and precise availability; reviewed paid EnqueueProduction, owned CancelProduction and observed FIFO progress/current ships |
| Fleet orders | Same host's known destination dropdown/routes, own fleets/ships, explicit Stay/StayEngage/Leave/Follow and approach setting. No hidden contact refresh or pursuit calculation |
| Central combat | Accepted PR #36 surface, owned draft/receipt/waiting/results/current stocks, permitted enemy condition and globally gated exact-once aftermath |
| Aftermath → Map | Same campaign Planning state, stopped participant routes, retained damage/shields/ammunition and fresh strategic choices; navigation does not recreate ships or refill |
| Save/reopen | Full format-3 root and append-only construction provenance; candidate validation precedes live replacement. Old physical/default roots are not silently converted |

The main UI exposes persistent Map / R&D / Production / Fleet orders / Combat navigation and the actual current turn/phase/owner. Trusted both-human switching remains collapsed and explicitly labelled test-host tooling; no opponent AI/readiness is invented. Actor/load replacement resets view context. Panel navigation retains controller, authoritative frame, unsent research/travel/tactical edits and central local selections, while replacing callbacks with a new Entry epoch. Drawing/selection/inspection never submits or advances.

Research drafts are presentation-only desired values totaling 600, using the existing deterministic local transfer convention. Accepted explicit R&D changes acknowledge their submitted part while retaining other desired shares. Rejected/stale commands keep inputs and require explicit authority refresh; there is no silent retry. Dirty inputs block advancement, actor replacement and unconfirmed load/exit, and cannot become an accidentally saved order. Save committed retains them locally. Successful load discards them deliberately; failed load/atomic save failure preserves them and the prior valid state/file.

Construction is the backend's real paid transaction: starting Federation capital **250**, eligible Cruiser **200 credits + 5 Dilithium + 100 Industry**. The existing source provisions hull **220**, shields **18/25/12/0**, torpedoes **7/12** once at completion, with normal fleet placement. No v1 blueprint conversion, static-source inference, advanced-template admission, refit or new balance formula is added. Science prerequisites and **36000** Engineering work must be earned. The authored contact freshness window remains **20 turns**, visibly aged; no hidden automatic refresh. The produced-ship combat proof occurs within it; long earned Engineering is tested later in the same campaign.

## Full format-3 trust/store boundary

`PhysicalReviewHost.Integrated.cs` holds typed package/save handles and full-root stores. Every integrated export uses **ExportIntegratedSave()**; inherited physical ExportSave is never used on that branch. The existing physical branch keeps its own typed format-2 carrier. Generation/request/actor guards remain, and strategic commands must match their observed actor. Primary revalidates ownership/payload/phase/stamps/fees/industry/source/ledger.

`ReviewExactPackageLoader.LoadIntegrated` reads exact raw outer/physical/core/scenario bytes, using the three pinned domain-separated hash stages in Primary's Headless reader. It does not hash reserialized or newline-normalized objects. `ReviewNewtonsoftCodec` preserves the entire new wrapper/BuiltShips plus nested physical/Core; strict member/token/UTF-8/duplicate/case and required nonnullable scalar checks still apply. Required nullable/semantic fields, actual source compatibility and private replay/provenance reach the real Application loader. Corrupt/mismatched/old roots reject before publishing a new model.

`ReviewIntegratedSlotStore` uses `Application.persistentDataPath/HobbyDS9IntegratedPlaytestOnly-v3/unity-integrated-playtest-{slot}.json`, disjoint from physical `HobbyDS9PhysicalReviewOnly-v2/unity-physical-review-{slot}.json` and default saves. The same confined slot grammar/reparse checks/encode-before-I/O/CreateNew temporary/flush/atomic replace protocol is retained. Explicit overwrites only; no delete-first fallback. Injected write failures preserve the prior slot and unsent edits. This is trusted local storage, not a claim of power-loss/network/concurrent-writer durability.

## Executed checks and exact evidence

| Check | Actual result |
| --- | --- |
| Locked restore and client/test Release | Passed, **0 warnings/errors** |
| Complete actual-source adapter suite | **107 passed**, 0 failed/skipped: all 83 earlier cases + 24 integrated cases |
| Actual integrated adapter path | Science shares/split, paid queue and Industry, produced ship → legitimate travel/contact → joint rounds/waiting save/reopen → retained aftermath/Map → long earned Engineering → complete format-3 reopen/vector |
| Failure/privacy/store cases | Stale/duplicate/foreign/invalid/unsupported commands; scoped own facts; corrupt format/hash/provenance; no physical-root substitution; atomic writes; all four raw-input hash contributions and explicit selector cases |
| Default PrepareUnity, physical PrepareUnityReview and additive PrepareUnityIntegrated | Passed; exact source DLLs/resources generated |
| Default asset/metadata/reference, physical PE/DLL/resource import, binding checks | Passed; older vectors/launch assertions preserved |
| Existing binding adversarial cases | **7/7 passed** |
| New integrated resource/hash/selector/scoped-view/full-root/separate-output checker and adversarial cases | Passed; **6/6** cases |
| Pinned Unity API compiler | Passed, **0 warnings/errors**; not runtime proof |
| Actual Unity **6000.6.5f1**, graphics-enabled hidden batch EditMode | **142/142 passed**, 0 failed/skipped: all 135 inherited/default/audio/central/navigation cases + 7 integrated cases |
| Native/IL2CPP/desktop input/audio/physical screenshots | **Not executed here**. The combined build method is authored for Primary's later PM dispatch |

The actual Editor used `-batchmode -runTests -testPlatform EditMode`, with graphics enabled (no `-nographics`) and hidden process window. It compiles/imports the actual sources/resources and executes actual Entry/UIElements/model/codec plus backend paid-production/combat transitions. Test-only reflection invokes ordinary MonoBehaviour lifecycle/render/navigation/geometry handlers because EditMode does not supply the runtime lifecycle/event loop. No log suppression, removed cases, forged progress or baseline replacements.

Resolution cases at **1280×720**, **1920×1080**, **3440×1440** check actual handler/class policy, reachable navigation/tree and unchanged authority. They are not pixel layout/readability or native input proof. The accepted first-iteration central layout is retained; current proposed art polishing/3D remains independent.

Two initial portable test failures were test assumptions: completed work records build turn 13, while the reopened Planning boundary is turn 14; and clearing an empty provenance ledger before completion is valid. Tests now wait for real natural completion before corrupting the ledger and distinguish built-turn from current-turn. The complete actual path passes the original backend vector; no authoritative production code changed to accommodate tests.

Ignored local evidence: `artifacts/integrated-playtest/integrated-ui-all.trx`, `integrated-ui-second.trx`, `editmode-integrated.xml` / `.log`, and eventual final delivery receipt/CI links. Exact final head/workflows are recorded on the draft PR to avoid a report self-hash. Portable GitHub CI is separate from local licensed Editor evidence.

## Determinism and preserved versions

Integrated package **88a919f6f51d244f79bb638ba933ff3c17305f25231a6b793aa6d53fca5f688b**; the actual adapter's full-root 131-turn earned-Engineering/produced-ship/combat/resume sequence matches **417d1852e052b4fe1f4f2b5f611ba49b82b7d76cd6154540a58bcaefc474e865**. The produced ship retains **3/7** original torpedoes after the battle; hull/four shield stocks remain unchanged by load/navigation.

Outer integrated package/save **3**, nested physical **2**, B1 **1**, legacy Save **14**, authored Content **11** and default Content **10** remain as delivered. No rules/compiler/RNG change: `science-engineering-v1`, `ship-design-compiler-v1` and existing resolver identities. Existing physical faster/slower vectors **a6a2522a4f389b0bb8aa562d6273b0f02b3ea1fdeb57c7fe21d8368e4fa4d177** / **77073cc154b78dbca2e5f6fa1a618cae490d11410f51f4cb2bb9e640a33210c4** remain in the passed inherited suite. Backend acceptance preserves legacy **fb2b75f18c8c5423f3a0850f38f6e53ee8249d3d08385978b5b2532041cc6089** and RNG **d8919406d6e01cf1**; final-head CI reruns full backend/regressions/smokes.

## Shared paths, launcher and bounded handover

Coordinated new/shared seams: `scripts/PrepareUnityIntegrated.proj`; `Review/Resources/IntegratedPackages/v1` and metadata; `Review/Scenes/IntegratedPlaytest.unity`; `Review/Editor/IntegratedBuild.cs`; integrated-only parser/bridge in the review assembly; the narrowly gated new flag branch in default `Runtime/ClientEntry.cs`; portable adapter engine-file exclusion; test project integrated fixture copying; `.github/workflows/unity-review-adapter.yml` minimum 107 plus new integrated preparation/static gates; `scripts/check-unity-integrated.py` and its adversarial cases. Existing linker already preserves the complete review assembly; no linker change was necessary. Existing default/physical builders/flags/output directories remain unchanged. Editor-generated settings/import line-ending deltas are excluded.

Primary's later authorized build recipe (authored, **not run here**): prepare default + integrated resources, execute the full graphics-enabled EditMode suite, then invoke **HobbyDS9.UnityReview.Editor.IntegratedBuild.BuildWindowsIntegrated** in pinned Unity 6000.6.5f1. Separate output: `artifacts/unity-integrated/Windows/HobbyDS9IntegratedPlaytest.exe`, authored scene `Review/Scenes/IntegratedPlaytest.unity`. The method selects IL2CPP/NET Standard temporarily and restores settings in finally. It generates **Launch-IntegratedPlaytest.cmd**, passing:

```text
HobbyDS9IntegratedPlaytest.exe --hobbyds9-integrated-playtest --hobbyds9-integrated-actor civ.federation
```

Use that launcher for the explicit integrated scenario. No-flag startup remains v1; physical flags remain their prior distinct mode. Ambiguous integrated+physical flags fail closed. The screen identifies the actual integrated profile/actor/turn. Use Map → R&D → reviewed allocations → Production → reviewed eligible Cruiser queue; advance legitimate turns and inspect paid progress/completed ship. Select known sector and that owned ship/fleet to prepare orders; deliberately switch the second human in test-host tooling for their choices. Open central Combat, commit each human round, confirm globally ready aftermath once, return to Map, then save/load the complete integrated root. Neither navigation nor order submission advances independently.

PM/Primary still must perform the **one combined IL2CPP build**, true native pointer/keyboard/scroll/focus/modal walkthrough, all three real pixel resolutions, save/restart and audible sound/music checks. Earlier keyboard-injection and URP diagnostic follow-ups remain separate honest limits. Current formula/precision/travel/cloak policies and production default activation remain gated. No main merge, force-push, default activation, old build overwrite, 3D/art rework, broad blueprint compiler/refit or new feature is part of this draft.
