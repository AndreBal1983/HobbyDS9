# Headless physical travel client and complete-root storage

14 October 2026. **Implemented headless review binding; opt-in only; policies proposed; production activation disabled.** This candidate combines delivered physical authority and the preserved presenter/durable-store work. No Unity/native acceptance or owner approval of numerical/contact policy is implied.

## Precise provenance

Fresh isolated branch: codex/tactical-travel-client-integration.

| History | Preserved checkpoint |
| --- | --- |
| Physical production implementation, draft [PR #24](https://github.com/AndreBal1983/HobbyDS9/pull/24) | **e4d6194a98984ce06c33f1d46a1446878509fffa** |
| Stress-only extension, draft [PR #26](https://github.com/AndreBal1983/HobbyDS9/pull/26) | **e268b2c490f424b96d21593e9c1427d39c7f5be7**, identical production API |
| Original real client, draft [PR #23](https://github.com/AndreBal1983/HobbyDS9/pull/23) | **0ab1af51e41bb20292c9a36f059bdd387cf4a1fc** |
| Durable B1 storage, draft [PR #25](https://github.com/AndreBal1983/HobbyDS9/pull/25) | **2bcc20cc592d335c7e84ecdc057c69290a4ad7b0** |
| Local history-preserving merge | **6466ce40a9c18f0671e11f632c784e780c1bbd7d**, parents **e268b2c490f424b96d21593e9c1427d39c7f5be7** then **2bcc20cc592d335c7e84ecdc057c69290a4ad7b0** |

The worktree started at #26, then locally merged #25 without conflicts. This preserves #23/#25 and #24/#26 as ancestors; no source PR, main or published history was merged/reset/rewritten. The final candidate's own changes are isolated client/adapter/storage/probe/tests/docs and one dedicated workflow. Backend production, scenario bytes, main solution, ci.yml and production Unity are identical to #26. Its draft PR targets the delivered stress branch for a clear combined diff.

Read AGENTS, design index/tactical/time chapters, architecture, [physical report](campaign-tactical-v2-physical-travel.md), [stress report](campaign-tactical-v2-travel-stress.md), actual [physical facade](../src/HobbyDS9.Application/CampaignTacticalTravelApplication.cs), [format-2 contracts](../src/HobbyDS9.Contracts/CampaignTacticalTravelContracts.cs), [B1 client](tactical-review-client-integration.md) and [durable failure/atomicity gates](tactical-review-durable-checkpoints.md). Future explanation work is not imported/bound.

## Actual typed authority and storage

[PhysicalReviewHost](../prototypes/HobbyDS9.TacticalReviewClient/PhysicalReviewHost.cs) exclusively uses ICampaignTacticalTravelSession and CampaignTacticalTravelApplication factories. It freezes its exact full package privately, serializes access to its private session and exposes only lawful frames/results. The shared [safe host seam](../prototypes/HobbyDS9.TacticalReviewClient/ITacticalClientHost.cs) permits the existing round/controller flow without pretending the physical session is a B1 authority.

[Physical codec](../prototypes/HobbyDS9.TacticalReviewStorage/JsonPhysicalReviewCodec.cs) persists the **complete CampaignTacticalTravelSaveDto**. Package/save **2**, physical profile/movement identity and complete root Phase/Requests/Orders/Contacts/LastMovement/MovementProof are preserved alongside Core. While an encounter is pending, the physical root is AwaitingCombat even though its nested legacy carrier remains Planning. Loading Core alone cannot represent that checkpoint.

Only CampaignTacticalTravelApplication.LoadReviewSave validates/replaces the physical host. Malformed/truncated/wrong-package/version, missing movement proof/order data or forged receipt/contact state is rejected before replacing the live session/frame/draft. No fallback to another slot or nested state is attempted.

The B1 codec/host remain separately typed. B1 filenames stay review-checkpoint-{slot}.json; physical filenames are **physical-review-checkpoint-{slot}.json**. Same-named slots coexist without overwrite/migration. Cross-format files copied into the other namespace reject. [Shared atomic byte/file protocol](../prototypes/HobbyDS9.TacticalReviewStorage/AtomicReviewFileStore.cs) preserves #25's confined identifiers, unique same-directory temporary files, flush/replace, explicit overwrite and operation-only cleanup. Only byte I/O is shared, never carrier validation.

Filesystem scope remains #25's local same-directory atomic rename/replacement contract. No power-cut/directory-fsync, network/cloud/removable storage, hostile-race, cryptographic or cross-writer guarantee is added. No user-save discovery, legacy campaign slots, recovery guess or campaign-long report archive.

## Lawful travel planning and reports

[Controller travel flow](../prototypes/HobbyDS9.TacticalReviewClient/ReviewController.Travel.cs) has one local fleet edit at a time, explicit review/confirm and actual backend receipt consumption. [Travel views/offers](../prototypes/HobbyDS9.TacticalReviewClient/PhysicalTravelViews.cs) contain immutable supplied facts:

- Own strategic fleets, travel orders and movement receipts.
- Known contact ID, **last-seen** node/turn, backend Fresh flag and lawful tracking coverage. A fresh observation is not silently displayed as enemy current position/destination.
- Actor-scoped known-route query result, steps/costs and the campaign turn/revision at query time.

Stay, StayEngage, Leave and Follow submit typed stamped commands. Follow carries the supplied contact last-seen stamp and an explicit queried known route or empty waiting route. Menu guards check only observed ownership/choice presence; Simulation validates hostility, contact freshness, tracking/route legality, locked transit and physical reach. The controller does not inspect private enemy orders, choose a hidden destination, derive movement budget/ETA or calculate pursuit/contact success.

There is **no independent topology-version field** in the delivered route API. Offers preserve actual campaign turn/revision and reject reuse after that stamp changes; they do not invent a topology token. The facade supplies route queries, not a full known-node map/navigation UI. That rendered map remains a later binding dependency.

[Renderer](../prototypes/HobbyDS9.TacticalReviewClient/ReviewTextRenderer.cs) labels physical policies as proposed, contacts as LAST SEEN, own end node/locked progress/cost/edges and exact supplied stop fraction. It does not divide that fraction into inferred timing, extrapolate enemy position, expose enemy reserves/fresh commands or render the private root/proof. Tactical visibility, four shield facings, own package stocks, round reports and fleet-only orders remain the actual projections. Readiness/ETA explanations remain unavailable; future explanation APIs are unbound.

Own accepted tactical orders survive complete-root reload separately from unsent drafts. Save keeps local unsent tactical/travel edits outside the file. Dirty load/reopen needs discard consent. Invalid load keeps edits; success changes host generation/identity and clears local edits. Stale/foreign/duplicate travel commands use actual errors, with no retry or partial effects. Obsolete/wrong-controller callbacks cannot overwrite current state. A new authoritative stamp invalidates stale local travel drafts with a notice.

## Worked real campaigns

| Case | Actual authoritative observations |
| --- | --- |
| Faster known follow | Pursuer route ends at Bajor; opposing scripted owner submits its own route toward the wormhole. Encounter at Bajor **3/4** of turn; participants stop and lose executable route legs |
| Accepted Round 1 waiting save | Full root has AwaitingCombat, physical proof/requests and own private accepted order. Another local unsent draft is absent |
| New-process continuation | A separate executable loads exact source files + named format-2 slot, finishes Round 1, changes Round 2 to Flank / Round 3 to Screen and finalizes once |
| Fresh next-turn engagement | Own orders have reset to Stay; explicit StayEngage from both owners plus next strategic advance allocates a new encounter. No same-turn rematch or route autoexecution |
| Finite stocks | Torpedoes **7 → 3 → 0** across two battles. Second battle begins with exactly retained stock; no refill or extra calendar advance |
| Slower follow | No encounter. Own locked progress **2/3**; other actor's own receipt **1/2**. Player view retains stale/last-seen enemy node without exposing its current destination |
| Mutual Leave | Intermediate co-location without engaging intent produces no forced fight |
| Stationary engagement | Explicit StayEngage yields start-time **0** contact; after finalization default Stay does not rematch without new intent |
| Unrelated fleet variant | Separate own traffic fleet continues to the wormhole, pays **5**, completes **2** edges and is absent from tactical membership; only participants stop |
| Failure boundaries | Stale/foreign travel rejects against byte-identical complete root. Corrupt root/file overwrite failures retain prior live state/draft/checkpoint |

Controlled variants are explicitly labelled test content; they do not change authored source packages or proposed shipping balance.

The new [47 real integration tests](../tests/HobbyDS9.TacticalTravelClient.Tests/) compare canonical, reversed command/round arrival, repeated, resumed and reordered authored inputs in en-US/nl-NL/tr-TR. Full physical roots, actor reports, positions and stocks agree. The unchanged pinned vectors are reproduced through the controller/host:

| Profile | Complete physical save SHA256 |
| --- | --- |
| Faster, two battles | **a6a2522a4f389b0bb8aa562d6273b0f02b3ea1fdeb57c7fe21d8368e4fa4d177** |
| Slower, weighted miss | **77073cc154b78dbca2e5f6fa1a618cae490d11410f51f4cb2bb9e640a33210c4** |

[Separate process probe](../prototypes/HobbyDS9.TacticalTravelClient.Probe/Program.cs) receives only explicit file paths/slot and scripted human actor IDs. It never receives the parent's session/carrier. Its resumed final bytes equal the uninterrupted file, including physical root/proof, not only own pools. This is a headless controlled test host, not opponent AI or a live Unity game.

## Separate executed evidence

Local SDK 10.0.401, locked restores/Release **0 warnings/errors**:

| Suite | Result | Scope |
| --- | --- | --- |
| New physical client/root storage | **47 passed, 0 failed/skipped** | Real physical facade, route/controller/receipt/privacy and separate-process complete-root resume |
| Preserved B1 durable storage | **47 passed, 0 failed/skipped** | Atomic failure/format/privacy/draft gates remain |
| Preserved B1 real client | **51 passed, 0 failed/skipped** | Original real sessions/round/controller behavior |
| Preserved mock presenter | **115 passed, 0 failed/skipped** | Test-owned prototype only |
| Combined production regression | **1,674 passed, 0 failed/skipped** | Delivered physical implementation + stress-only cases, unchanged backend production |

Prior test sources are unchanged. [Dedicated CI](../.github/workflows/tactical-travel-client.yml) restores/builds/runs the four explicit client projects sequentially on Linux/Windows/macOS, checks separate 47/47/51/115 TRX counters and uploads evidence. Existing regression workflow remains untouched and must also pass on the exact delivered candidate. Exact head/run are recorded in the draft PR and handover.

Reproduce with dotnet restore --locked-mode, build -c Release --no-restore and test -c Release --no-build --no-restore for each explicit client csproj; use HobbyDS9.slnx for the 1,674-case regression. No Unity Editor/native/desktop tools are part of execution.

## Remaining gates

Owner acceptance of physical boundary/admission policy, movement/screening/cloak tuning and visible condition precision is still required. Mixed-hostility connected owners, dynamic sensors/fog-of-war, unbound ship provisioning, strategic retreat/objective exits and default campaign activation remain gated by the physical implementation.

There is no accepted production Unity binding/rendered-map/slot UI, rendered three-resolution/input/native acceptance or newly calculated client combat/travel policy. Backend-supplied speed/ETA and legal hostile-contact menu precision are limited by the actual facade; unavailable explanations remain explicit. Primary's later optional explanation projections are not imported.

The candidate is a **draft combined headless review**. Main and all source PRs/histories remain intact; no PR merge, force push or new version/migration is authorized or performed.
