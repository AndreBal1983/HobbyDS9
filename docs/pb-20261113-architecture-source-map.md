# PB-20261113 architecture and integration source map

13 November 2026. Phase: discovery complete; receiving recommendation delivered. This document is a bounded source inventory, not acceptance of a build or of new mechanics.

## Recommended first playable target

Use the preserved [laptop migration snapshot](https://github.com/AndreBal1983/HobbyDS9/commit/b9401ce175c02ed512e9ae0e675e456b19b2a371) **`b9401ce175c02ed512e9ae0e675e456b19b2a371`** as the source basis for the isolated PB candidate. Receive its existing Campaign menu, Cycle14 campaign, LCARS console, fleet cards, system/planet views, existing ship models, designer hull previews, actor-safe battle playback and V3 galaxy/nebula renderer together. The first smoke target is a fresh existing Campaign with seed 42 / 250 systems / quiet area 2.00, followed by ordinary research, streamed construction, movement and save/continue checks through the normal UI. QA defines the actual bounded acceptance journey and records the final executable identity.

This recommendation matches Unity's initial `gui.md`. It is **preserved, unaccepted source**, not a newly accepted or production-ready package. Backend/Data must first admit the exact portable source and content; Unity owns the isolated launch/build adjustments; Art verifies current asset closure/attribution; QA owns Editor/import/native gates. Do not combine open PRs by number or substitute an older map-review executable for the playable campaign. If this candidate cannot pass, report its blocker and any separately named fallback to Product Desk; do not silently ship an older look.

No new gameplay, PR #85 import, Engineering content-writer execution, replacement-model admission, main merge, default/release promotion or historical package/save replacement is part of this recommendation.

## Verified identities and lineage

GitHub API reads on 13 November independently returned remote main, all **43 open PRs** (all draft) and **241 branch records**. Local object/history reads corroborated the pins below. Open-PR descriptions are reported evidence and can be older than the actual head.

| Source | Immutable pin | Meaning |
| --- | --- | --- |
| Remote main | `e99c151f9d7f876300457ddac6b7c3c939330140` | 19 October merge of reviewed frontier adapter #66; older than current graphics work |
| Migration receiving basis | `b9401ce175c02ed512e9ae0e675e456b19b2a371` | Latest preserved integrated GUI plus later documentation/research; not native-tested as a combined snapshot |
| Integrated migration delta | `450fe17a619c5e8bf8a9d9e05eb37e50c9360d34` | 45-file preservation commit, explicitly “not native-tested”; includes menus, procedural body cards, ship names and pending Engineering work |
| Earlier GUI candidate | `999246fd6ae8ef92e9c80cd444c59a1ac05bd1ff` | Fleet cards, Sol meshes, selected-hull designer routing; ancestor of migration |
| Campaign navigation candidate | `e89fbf057731374b54d635351f4e2bed0273355e` | 6 November mission/science navigation; ancestor of earlier GUI and migration |
| Latest documented human build source | `c87a73e7b6992b3a6e5ba0f2d5caf508b06ce1df` | 6 November scoped Campaign handover; runtime `b57c769afe144cb54fe9c6ef867964d6ce302c01`, GameAssembly prefix `AF3324C6` |
| Earlier fleet frontend #71 | `cc8578c17e919dacaa2975b5cc757bee1bc8befc` | Ancestor of migration; received backend/art foundations |
| Generated frontend #76 | `913a4277d6368d795512f067db0c68c3fe629258` | Ancestor of migration |
| Natural review frontend #81 | `9d0ddbe74ee88093295c1cbea482047a84a851c1` | Ancestor of migration, but its standalone PR scope is map review |
| Historical progress-build branch | `6676ee969f434263aec628e260bad7a3cd575184` | Preserved October delivery branch, not an ancestor of migration; no blind merge |
| Historical battle-presentation branch | `db5b1108dad969b6b9c9585708a50fcfde614852` | Separately preserved branch, not an automatic integration dependency |
| Separate latest Engineering #85 | `e365ec1ecf3d686b114548f805f68f73f742e89b` | Based on main; not ancestor of migration; source/content/native acceptance separate |

The migration is not a descendant of main's final merge commit. Their verified merge-base is `6f00e29d5b4adbb88f207614fd653c30ae96f7d1`; the only commit unique to main is `e99c151`, whose tree differs from that base in the two R7 acceptance documents. This is a documentation-history distinction, not evidence that main has newer runtime mechanics. The migration later merged documentation history. A new integration branch should preserve the selected snapshot and review its explicit delta; a main-to-migration wholesale cherry-pick is unnecessary.

Preserved `system-body-art` pin `0edad84d89d16c22be532918579f870db454ca82`, natural-backend #79 `d35fe6c552b53bb67e5a8ae3fdc2382cac081085`, Engineering #85 and the progress-build pin are not ancestors of migration. Their content may have been selectively received; non-ancestry is not proof of absence. Inspect actual current assets/APIs instead of replaying whole branches.

## Current runtime versus design/proof

All paths in this section refer to the migration pin above.

| Area | Actual source or resource | Readiness and boundary |
| --- | --- | --- |
| LCARS/gameplay shell | `client/HobbyDS9.Unity/Assets/HobbyDS9/Review/Runtime/ReviewPlayerView.cs`, `ReviewWorkspace.cs`, `ReviewEntry.CampaignMenus.cs`; `Review/Resources/PlayerConsole.uss` | Actual Unity UI; latest migration menus unaccepted. Legacy `UnityClient` shell and browser concepts are different entry paths |
| Campaign selection and persistence | `ReviewEntry.GeneratedCampaign.cs`, `ReviewUiModel.GeneratedCampaign.cs`, typed Cycle stores/codecs and `ReviewEntry.CampaignMenus.cs` | Current New Campaign selects existing Cycle mode. Preserve complete typed roots, profile-specific directories and slots; no migration by selecting a different menu |
| Fleet cards | `ReviewFleetRoster.cs`, `ReviewUiModel.PlayableFleet.cs` | Owned ship/fleet presentation; commands stay authoritative |
| Systems/planets | `ReviewSolOverview.cs`, `ReviewSolStage.cs`, `PlanetBodyCard.cs`, `PlanetVisualProfile.cs`; `Review/Resources/SystemBodyArt` and `SystemBodyArt/SolMeshes` | Tracked Sol meshes/textures/shaders; migration adds procedural card/source work. Actual Unity materials, appearance and performance still need receiving |
| Ship exterior/designer | `ArtDemo/Runtime/ActivityHullModel.cs`, `ActivityHullPresentation.cs`; `client/HobbyDS9.Unity/Assets/HobbyDS9/Runtime/ShipDesignerPreview.cs`; `ArtDemo/Resources/ArtDemo/ActivityHulls` | Existing Constitution/Defiant and other exact catalog bindings; library presence does not admit a gameplay class. Selected-hull routing source is newer than recorded native validation |
| Battle | `ReviewBattlePanel.cs`, `ReviewBattleStage.cs`, `ReviewBattleVisualBinding.cs` | Real actor-safe per-round playback; camera/skip/speed must not resolve gameplay or reveal hidden data |
| Galaxy/nebula | `GeneratedGalaxyV3Background.cs`, `GeneratedGalaxyV3AssetData.cs`, `GeneratedGalaxyChartedMask.cs`; `ReviewPlayableMap.cs`; `Review/Resources/GalaxyPresentationV3/galaxy.png`, `nebula.bytes`, `particles.json` | Actual Unity implementation and committed asset data. Preserve charted mask, finite caps, stale/disposal checks and lawful observations |
| Map review | `GeneratedGalaxyMapView.cs`, route-draft/path/workspace classes | Separate full-map review tools; do not equate this with campaign movement, discovery or AI |
| New approved replacement GLBs | [8 November routing contract](https://github.com/AndreBal1983/HobbyDS9/blob/b9401ce175c02ed512e9ae0e675e456b19b2a371/docs/ship-designer-hull-preview-routing-20261108.md) | Approved Defiant/Galaxy replacement resources remain unreceived. Existing Defiant credits identify the Meshy study, not the approved A. Klemm replacement |
| Browser/CPU studies | PRs #60/#68 and #74/#75/#77/#78/#80/#82/#83/#84 | Valuable visual intent/evidence. Their galleries, concept layers and illustrative controls are not production Unity assets or native acceptance |

The latest design and intent should guide evaluation, while the actual selected source defines what can be built. [Current implementation map](https://github.com/AndreBal1983/HobbyDS9/blob/b9401ce175c02ed512e9ae0e675e456b19b2a371/docs/current-demo-implementation.md) explicitly distinguishes ordinary base upfront-paid production from the newer streamed profile: local material tranches advance work; ordinary streamed construction does not reserve all costs upfront. This assignment preserves that existing mode, without implementing the full strategic-AI, conquest or ship-design roadmap.

## Source admission issue: Engineering migration work

The preservation commit `450fe17` changes `src/HobbyDS9.Simulation/EngineeringRules.cs` and `ResearchValidation.cs`. Refinement completion now tolerates an absent ParameterChange when the project unlocks a genuinely derived hull/component version. This is a portable rule/validation change, not a GUI-only change. Backend must inspect and either validate that inherited source for the PB candidate or deliver an explicit scoped correction; Architecture did not test or accept it.

The same commit contains three Engineering writer variants and input metadata. [The writer handoff](https://github.com/AndreBal1983/HobbyDS9/blob/b9401ce175c02ed512e9ae0e675e456b19b2a371/docs/engineering-content-variant-source-20261112.md) states source-only preparation: no new resource capsule was generated or accepted. PR #85's 20-project content is not already activated simply because some source was copied. Current menu/package calls use the existing playable/Cycle templates; no `EngineeringDevelopmentV1` menu/resource hook was found in the inspected entry files. Do not execute those writers or fabricate acceptance records as part of a graphics build.

## Compatible dependency and build order

1. **Pin/admit source:** QA/Unity receive `b9401ce175c02ed512e9ae0e675e456b19b2a371` in their own assigned worktrees. Backend/Data inspect the inherited Engineering delta and current complete-save/profile contracts. Art identifies tracked resource closure and accurate credits. Record any corrective commits individually.
2. **Prepare portable code/resources:** use the pinned .NET **10.0.401** SDK and locked dependencies. [PrepareUnityGeneratedGalaxy.proj](https://github.com/AndreBal1983/HobbyDS9/blob/b9401ce175c02ed512e9ae0e675e456b19b2a371/scripts/PrepareUnityGeneratedGalaxy.proj) prepares the portable assemblies and retained Demo/Frontier/PlayableFleet resources. Playable preparation copies the five exact authored source files. Reuse valid tracked resources and prepare from the same source; do not reuse stale portable DLLs from main.
3. **Receive Unity in isolation:** repository and actual build code pin **6000.6.5f1**. Installed **6000.6.9f1** is a separately recorded compatibility experiment. Unity owns any scoped version/build-entry adjustment; QA records import/settings/package/metadata deltas and tests shader/material/UI behavior.
4. **Use the Campaign build path:** the relevant implementation is `HobbyDS9.UnityReview.Editor.GeneratedStaticAstronomyCampaignBuild`, not `ClientBuild.BuildWindows` from the old generic MSBuild Windows target. Its builder uses `Assets/HobbyDS9/Review/Scenes/IntegratedPlaytest.unity`, `HOBBYDS9_SINGLEPLAYER_DEMO`, Windows64 IL2CPP, StrictMode and a fresh absent output stage. Its current line 422 rejects versions other than 5f1. Add a PB-specific fresh stage/identity through Unity's scoped patch; retain old methods/stages.
5. **Combined acceptance:** exercise actual new-game selection, current console/system/fleet/designer views, charted galaxy/nebula, real orders/turn, typed save/cold continue, reachable battle playback and resize/input on the exact combined executable. Keep reported historical checks separate. Use a separate package and manifest; no latest pointer/default/release promotion.

A .NET 11.0.100 SDK is now installed system-wide; `dotnet --list-sdks` returned only that system SDK during this task. It does not satisfy the repository's pinned 10.0.401/latestPatch policy. QA's receipt reports a separately downloaded 10.0.401 under ignored artifacts from earlier authorized QA work, with main-only tests. Reuse the QA-owned verified runner if available; this assignment installed nothing.

## Missing artifacts and current blockers

- `E:/HobbyDS9/artifacts` does not exist on this machine (executed Test-Path). Historical native packages, screenshots, acceptance receipts and concept galleries there cannot be reverified locally. Their recorded outcomes are historical reports, not executed checks here.
- The E: absence does not itself prevent building committed runtime art. V3 atlas/volume/particle data, Sol resources and existing ship assets are in Git. New approved replacement models and any external source-only delivery need Art's separate recovery/provenance checks.
- QA's receipt reports installed Unity 9f1 but no verified IL2CPP variation, no verified VC x64/Windows SDK at its audit, and no established active Editor license. Visual Studio setup is reported in progress. These are build-team prerequisite gates, not Architecture installation tasks.
- Actual main tests do not cover migration source. Engineering port, new menus/body cards and selected-hull bindings require current candidate checks.
- Prior source documents disclose slow mature Save/Continue, limited newer native smoke coverage, audio warnings and occasional Quit issues. Do not claim these resolved or broaden acceptance without evidence.
- No model/provenance acceptance, full native performance pass, save migration or release approval is supplied by this map.

## Executed and reported evidence

**Executed by Architecture:** read root/team/design/architecture/milestone context; queried current GitHub main/open PRs/branches; created isolated documentation worktree; inspected source trees, exact source files/diffs and ancestry; read Unity/QA shared receipts; checked E: absence and system SDK list; documented runtime paths and version guard. No broad tests, portable build, Editor/import, native run, asset generation, install, merge or promotion.

**Reported only:** R7 acceptance and 6 November scoped native/build evidence in existing docs; PR validation claims; QA's main build/1,823 tests/static/headless results. None is relabelled as acceptance of b940 or PB.

**Delivery identity:** worktree `C:/HobbyDS9/artifacts/worktrees/pb-20261113-architecture`, branch `codex/pb-20261113-architecture`, starting SHA `e99c151f9d7f876300457ddac6b7c3c939330140`. Owned repository path: `docs/pb-20261113-architecture-source-map.md`. Shared handoff: `C:/HobbyDS9/artifacts/coordination/PB-20261113/architecture.md`. Final receipt records the documentation commit and verification outcome. No other contributor files staged or modified.

Next dependency: Backend/Data admission of inherited source and Art closure, followed by Unity's pinned PB build patch and QA's exclusive combined receiving/build. Product Desk owns any scope/product choice.

## Open PR inventory at discovery

Every row was open and draft in the observed API snapshot. Pins are actual heads, not title/body abbreviations. Base branches describe the remote stack and do not establish accepted dependencies. Most historical stacked work is already represented in later integrated source.

| PR | Title | Head SHA | Remote base | Integration disposition |
| --- | --- | --- | --- | --- |
| [#86](https://github.com/AndreBal1983/HobbyDS9/pull/86) | Document specialist chat roles and dispatch workflow | `7eac4c33855473e90f2f965f61c70bf24b0718e2` | `main` | Role documentation only |
| [#85](https://github.com/AndreBal1983/HobbyDS9/pull/85) | Add 20 selectable Engineering development projects | `e365ec1ecf3d686b114548f805f68f73f742e89b` | `main` | Separate Engineering mechanics/content; exclude from graphics build by default |
| [#84](https://github.com/AndreBal1983/HobbyDS9/pull/84) | Add detailed galaxy and nebula CPU proof contract | `aa9b0f092838033ef402629c96d1533c5468249f` | `codex/freeform-nebula-galaxy` | CPU asset proof/receiving contract; not Unity runtime |
| [#83](https://github.com/AndreBal1983/HobbyDS9/pull/83) | Replace oval nebula clipping with freeform art and illustrate galaxy depth | `4eb82d96e305ad6d5eb3fc3d05decca36074d62b` | `codex/nebula-volume-concept` | Freeform nebula/browser illustration; not Unity runtime |
| [#82](https://github.com/AndreBal1983/HobbyDS9/pull/82) | Propose original procedural nebula visuals and depth parallax | `23621ae5ebb23e909161d5f0e0cc90aea5aaae46` | `codex/galaxy-art-route-v5` | Procedural concept/CPU depth demonstration; not Unity runtime |
| [#81](https://github.com/AndreBal1983/HobbyDS9/pull/81) | Draft: Natural galaxy review with ordered waypoint drafts and certified route lines | `9d0ddbe74ee88093295c1cbea482047a84a851c1` | `codex/generated-galaxy-m1-marker-fix` | Natural M1 map-review UI; ancestry already in migration |
| [#80](https://github.com/AndreBal1983/HobbyDS9/pull/80) | Add actual route and natural-distribution art evidence | `b339aaa82e86d70c66497fb00f137b0491b6b196` | `codex/generated-galaxy-art-scale-v4` | Art/API-route evidence |
| [#79](https://github.com/AndreBal1983/HobbyDS9/pull/79) | Map-review routing and versioned natural star fields | `d35fe6c552b53bb67e5a8ae3fdc2382cac081085` | `codex/generated-galaxy-m1-marker-fix` | Portable route/natural generator; separate head, inspect received implementation |
| [#78](https://github.com/AndreBal1983/HobbyDS9/pull/78) | Review actual 250/1000-system M1 maps with shared-edge portal routes | `bcd13ee01a763acdb8a89b55c6872b9e889d4471` | `codex/generated-galaxy-art-environment-v3` | 250/1000-system art evidence |
| [#77](https://github.com/AndreBal1983/HobbyDS9/pull/77) | Render actual environment-first M1 features and controlled comparisons | `e47fcc944283db0ee223ec3d23ad78cf87942ae6` | `codex/generated-galaxy-art-irregular-v2` | Environment-first art evidence |
| [#76](https://github.com/AndreBal1983/HobbyDS9/pull/76) | Add environment-first generated galaxy M1 review and aligned map controls | `913a4277d6368d795512f067db0c68c3fe629258` | `codex/playable-fleet-frontend` | Generated M1 marker/frontend source; ancestry in migration |
| [#75](https://github.com/AndreBal1983/HobbyDS9/pull/75) | Preserve actual v1/v2 map irregularity comparison as geometry evidence | `07f6a29d52330559e01ccaa37be2622b4f3b82f0` | `codex/generated-galaxy-art-m1` | Irregularity geometry evidence |
| [#74](https://github.com/AndreBal1983/HobbyDS9/pull/74) | Deliver generated galaxy M1 presentation on actual seeded exports | `d5c0637271098e3e9d03695d462448d73c25e81c` | `codex/playable-fleet-frontend` | Seeded map presentation assets |
| [#73](https://github.com/AndreBal1983/HobbyDS9/pull/73) | Generated galaxy M1: isolated Unity map generation and inspection | `471922db932423818bdc94386f8b91393cc15131` | `codex/playable-fleet-frontend` | Earlier generated-map review frontend |
| [#72](https://github.com/AndreBal1983/HobbyDS9/pull/72) | Scale M1 generated galaxies with bounded environment-first maps | `aa434036462c08eceb323aa581e539d307ca65e0` | `codex/playable-fleet-frontend` | Portable generated-map foundation |
| [#71](https://github.com/AndreBal1983/HobbyDS9/pull/71) | Integrate playable fleet UI and authoritative round playback | `cc8578c17e919dacaa2975b5cc757bee1bc8befc` | `codex/frontier-player-preview` | Fleet UI/playback integration; ancestry in migration |
| [#70](https://github.com/AndreBal1983/HobbyDS9/pull/70) | Add playable fleet organization, round playback and persisted battle history | `86fde5f537aa9b7ce2029d65d40f5ebed238372e` | `codex/frontier-player-preview` | Fleet backend/persistent playback foundation |
| [#69](https://github.com/AndreBal1983/HobbyDS9/pull/69) | Supply playable battle art mapping and original Cardassian prototype | `e104143e66c4f3cb7eca042614175b07ede27b47` | `codex/frontier-player-preview` | Playable Federation/Cardassian art handoff |
| [#68](https://github.com/AndreBal1983/HobbyDS9/pull/68) | Prototype owner-directed map, production ledger, fleet roster and LCARS controls | `74193131bc6b22eda3fcc4e7da35876f8e38f227` | `codex/coherent-interface-mockups` | Browser interaction prototype; do not import historical runtime |
| [#67](https://github.com/AndreBal1983/HobbyDS9/pull/67) | Add an opt-in Frontier exploration player preview | `e3e76216c5d1033ae51614e364ccb1717506bb99` | `main` | Frontier preview foundation |
| [#62](https://github.com/AndreBal1983/HobbyDS9/pull/62) | Fit Combat order controls vertically and expose the draft action | `17af16455f45cf4fafc0ed8836c561122f0c8226` | `codex/demo-tab-focus-route` | Historical stacked lifecycle/UI candidate; compare existing integrated source before receiving |
| [#61](https://github.com/AndreBal1983/HobbyDS9/pull/61) | Acquire unfocused demo Tab focus and extend bounded input evidence | `cb80ec167ffae4954e7ea27d0771ee9f5d79f4f7` | `codex/demo-r5-integration` | Historical stacked lifecycle/UI candidate; compare existing integrated source before receiving |
| [#60](https://github.com/AndreBal1983/HobbyDS9/pull/60) | Deliver coherent interface concepts across strategic and battle flows | `b3496b6b1da31634d9beb8a7357b2480f37a9a72` | `main` | Browser gallery/concept only |
| [#58](https://github.com/AndreBal1983/HobbyDS9/pull/58) | Show confirmed own aftermath summary without inventing saved history | `9f9d86956c2795a8f1b3658c8f44090f97ececcf` | `codex/demo-r4-integration` | Historical stacked lifecycle/UI candidate; compare existing integrated source before receiving |
| [#57](https://github.com/AndreBal1983/HobbyDS9/pull/57) | Observe demo key release on the current panel after Start rebuild | `367e597f87f8ef6c96b7be2f1e170ed710fc4c12` | `codex/demo-r4-integration` | Historical stacked lifecycle/UI candidate; compare existing integrated source before receiving |
| [#56](https://github.com/AndreBal1983/HobbyDS9/pull/56) | Clarify demo Engineering limits and Map selection scope | `d4cdd3b2adf045197df207c8d6d2b0d115be2ebe` | `codex/demo-combat-presentation-fixes` | Historical stacked lifecycle/UI candidate; compare existing integrated source before receiving |
| [#53](https://github.com/AndreBal1983/HobbyDS9/pull/53) | Fix demo Combat captions and completed-campaign controls | `51d0e5dc3858bb1c853afbc063a6cc932ba811ea` | `codex/demo-keyboard-controls` | Historical stacked lifecycle/UI candidate; compare existing integrated source before receiving |
| [#52](https://github.com/AndreBal1983/HobbyDS9/pull/52) | Handle focused demo keyboard controls without duplicate submission | `a5f0cd544090adf4b5b92c64bfb0aa6cb93d7f30` | `codex/singleplayer-demo-r2-integration` | Historical stacked lifecycle/UI candidate; compare existing integrated source before receiving |
| [#49](https://github.com/AndreBal1983/HobbyDS9/pull/49) | Prepare unbound Galaxy v2 LOD assets and opt-in textured preview source | `061d3a6e83ce644fce4025ae2d49f5a3b1e12923` | `main` | Unbound Galaxy model/textured preview candidate |
| [#39](https://github.com/AndreBal1983/HobbyDS9/pull/39) | Start presentation audio in the explicit integrated playtest | `9b73f0bf559285779ae4d069b17098b5ea9c8ed7` | `codex/integrated-playtest-shell` | Historical stacked lifecycle/UI candidate; compare existing integrated source before receiving |
| [#33](https://github.com/AndreBal1983/HobbyDS9/pull/33) | Fix native review control text contrast with scoped styles | `bae723f182dbff07bcfbf83a47716c7143eed6b8` | `codex/unity-review-modal-permission` | Historical stacked lifecycle/UI candidate; compare existing integrated source before receiving |
| [#32](https://github.com/AndreBal1983/HobbyDS9/pull/32) | Consume review modal confirmation once and reject canceled callbacks | `280930db107204f23a4fe0821710bbbe75c1fc10` | `codex/tactical-unity-review-binding` | Historical stacked lifecycle/UI candidate; compare existing integrated source before receiving |
| [#31](https://github.com/AndreBal1983/HobbyDS9/pull/31) | Add opt-in Unity physical review binding and complete-root codec | `03ed7c3c01796e1c517787547bbf456257dd0ffe` | `codex/campaign-tactical-v2-unity-review-import` | Historical stacked lifecycle/UI candidate; compare existing integrated source before receiving |
| [#30](https://github.com/AndreBal1983/HobbyDS9/pull/30) | Explicit Unity physical review import and static package acceptance | `8e4594fffd3962d7c5e3d1cae45d45973800d224` | `codex/tactical-review-explanations-shell` | Historical stacked lifecycle/UI candidate; compare existing integrated source before receiving |
| [#29](https://github.com/AndreBal1983/HobbyDS9/pull/29) | Authoritative own explanations and runnable trusted review shell (stacked) | `372de4ca8d74f43dc723238461036e9aa6d7e605` | `codex/tactical-travel-client-integration` | Historical stacked lifecycle/UI candidate; compare existing integrated source before receiving |
| [#28](https://github.com/AndreBal1983/HobbyDS9/pull/28) | Combined physical travel client and complete-root durable resume (headless, gated) | `2a3b2fdedfdcbb62619b316d91fd56936d0e8a50` | `codex/campaign-tactical-v2-travel-stress` | Historical stacked lifecycle/UI candidate; compare existing integrated source before receiving |
| [#27](https://github.com/AndreBal1983/HobbyDS9/pull/27) | Campaign combat: actor-safe own weapon and fallback explanations | `d52247605feaed9ff54ee3547a358af7466e7498` | `codex/campaign-tactical-v2-travel-stress` | Historical stacked lifecycle/UI candidate; compare existing integrated source before receiving |
| [#26](https://github.com/AndreBal1983/HobbyDS9/pull/26) | Campaign travel: bounded fleet stress and frozen-boundary audit | `e268b2c490f424b96d21593e9c1427d39c7f5be7` | `codex/campaign-tactical-v2-physical-travel` | Historical stacked lifecycle/UI candidate; compare existing integrated source before receiving |
| [#25](https://github.com/AndreBal1983/HobbyDS9/pull/25) | Durable trusted B1 review checkpoints and separate-process resume (stacked) | `2bcc20cc592d335c7e84ecdc057c69290a4ad7b0` | `codex/tactical-review-client-integration` | Historical stacked lifecycle/UI candidate; compare existing integrated source before receiving |
| [#24](https://github.com/AndreBal1983/HobbyDS9/pull/24) | Campaign Tactical v2: gated physical travel and contact lifecycle | `e4d6194a98984ce06c33f1d46a1446878509fffa` | `codex/campaign-tactical-v2-lifecycle-stage-b1` | Historical stacked lifecycle/UI candidate; compare existing integrated source before receiving |
| [#23](https://github.com/AndreBal1983/HobbyDS9/pull/23) | Headless tactical review client against actual Application (stacked, activation gated) | `0ab1af51e41bb20292c9a36f059bdd387cf4a1fc` | `codex/campaign-tactical-v2-lifecycle-stage-b1` | Historical stacked lifecycle/UI candidate; compare existing integrated source before receiving |
| [#22](https://github.com/AndreBal1983/HobbyDS9/pull/22) | Add gated campaign-owned tactical v2 lifecycle core | `e117c95c7961bc424f190572f4e23a8bad7cfd08` | `codex/campaign-tactical-v2-lifecycle` | Historical stacked lifecycle/UI candidate; compare existing integrated source before receiving |
| [#20](https://github.com/AndreBal1983/HobbyDS9/pull/20) | Review campaign tactical v2 lifecycle and exact movement prototypes | `95a43685806f8b4fd724e6d645f854ac78f98571` | `codex/tactical-v2-authored-equipment-foundation` | Historical stacked lifecycle/UI candidate; compare existing integrated source before receiving |
