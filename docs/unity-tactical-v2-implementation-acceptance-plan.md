# Unity Tactical v2 — implementation and acceptance preparation

13 October 2026. **Isolated test-only presenter prototype; production Unity v2 integration gated.** This package follows [the reconciled UX specification](campaign-tactical-v2-playable-ux-spec.md) at preserved commit 455bf1030630e6ffbdbd8a84898d6823f8801ae3. It advances presentation/controller preparation without binding any accepted Stage B authority. It is not a playable campaign, Unity screen or backend acceptance. The [chronology/package hardening contract](tactical-v2-presenter-hardening.md) supersedes the initial presenter's weaker receipt checks and aggregate-ammunition view.

## Pinned basis and evidence boundaries

- Stage A read-only source head: **95a43685806f8b4fd724e6d645f854ac78f98571** in the separate lifecycle worktree. Read the [Stage A report](C:/Users/ambal/.codex/worktrees/campaign-tactical-v2-lifecycle/HobbyDS9/docs/campaign-tactical-v2-lifecycle-backend.md), [concrete toy contracts/observations](C:/Users/ambal/.codex/worktrees/campaign-tactical-v2-lifecycle/HobbyDS9/prototypes/HobbyDS9.CampaignTacticalV2.Prototypes/FleetCandidates.cs), and [toy batch](C:/Users/ambal/.codex/worktrees/campaign-tactical-v2-lifecycle/HobbyDS9/prototypes/HobbyDS9.CampaignTacticalV2.Prototypes/PrototypeBatch.cs). These local links identify external review sources, not files distributed in this branch. No lifecycle source was modified or messaged.
- Reviewed actual [Application session interface](C:/Users/ambal/.codex/worktrees/campaign-tactical-v2-lifecycle/HobbyDS9/src/HobbyDS9.Application/ICampaignSession.cs) and [optional inspection capability](C:/Users/ambal/.codex/worktrees/campaign-tactical-v2-lifecycle/HobbyDS9/src/HobbyDS9.Application/CampaignTacticalV2Inspection.cs) at the same head. Existing production operations include CaptureSnapshot, strategic commands/routes, ExportSave, v1 plan/resolution/report/buffer/finalization and source inspection. **At pinned Stage A commit 95a4368 there is no production campaign-v2 observe/round/cloak/finalization API.** Local links may change as the other worktree advances; use that recorded Git object for this mapping.
- The owner's [lifecycle milestone](C:/Users/ambal/.codex/worktrees/product-manager-design-handoff/HobbyDS9/docs/campaign-tactical-v2-lifecycle-spec.md) and [tactical chapter](C:/Users/ambal/.codex/worktrees/product-manager-design-handoff/HobbyDS9/docs/design/tactical-combat.md) are pinned to design snapshot **bc68de172318d19ee256c81351728d30d6445978**. These are external local source links; use the recorded Git object if that worktree advances. Their agreed fleet/cloak/observation/aftermath behavior governs the plan. Their shared travel, mobility and persistence formulas remain proposed/open. No source snapshot branch is merged/rebased here.
- Stage A's report records 1,555 tests and independent toy vectors; those results were read, not rerun by this assignment. Its toy does not implement full weapon/geometry/armor/recharge resolution or live campaign authority. Nothing here selects its candidate mobility/contact formulas or grants Stage B acceptance.

The new isolated branch starts from the completed UX documentation history, not by merging another implementation branch. Only this package and a new test project are added. Production Unity, Application, Contracts, Simulation, scenario content, solution membership, existing test project and live v1 paths remain unchanged.

The final read-only worktree status check found subsequent Stage B edits in progress, including new review-session files. They were not imported, assumed accepted or used for a runtime adapter. This package is a handover against the delivered Stage A checkpoint; re-read the next delivered/accepted backend head before closing B1–B11. In-progress files do not establish campaign lifecycle acceptance.

## Delivered prototype and executed evidence

[Presenter](../tests/HobbyDS9.TacticalUxPrototype.Tests/Prototype/TacticalPresenter.cs), [detached read models](../tests/HobbyDS9.TacticalUxPrototype.Tests/Prototype/ReadModels.cs), and [interaction tests](../tests/HobbyDS9.TacticalUxPrototype.Tests/PresenterInteractionTests.cs) all compile **inside the test assembly only**. There is no production adapter, scene, plugin DLL, campaign reference or Stage A assembly reference. Mock success is labelled PROTOTYPE / TEST-OWNED DATA / CAMPAIGN V2 DISABLED in every text view, including simulated completion.

The only accepted client source compiled into this test project is [ResponsiveLayoutPolicy](../client/HobbyDS9.Unity/Assets/HobbyDS9/Runtime/ResponsiveLayoutPolicy.cs), unchanged and linked read-only. It contains presentation dimensions only. Actual Lcars widgets/USS are reserved for later binding; no UnityEngine/Editor execution or visual redesign is introduced.

Initial delivery at **785bd46f4ead9872fc2bccbff6c0fd907a052ea0** executed 53 tests. Hardening retains all 53 named cases and adds 50 chronology and 12 package cases: **115 passed, 0 failed/skipped**, locked restore and Release **0 warnings/errors**, SDK 10.0.401. Initial TRX: tests/HobbyDS9.TacticalUxPrototype.Tests/TestResults/prototype-final.trx; current TRX: tests/HobbyDS9.TacticalUxPrototype.Tests/TestResults/hardening-final.trx (ignored artifacts). New chronology cases first reproduced **27/27 failures** against the prior implementation. These are presenter tests, not the full production/Stage A suite. No GitHub CI, .NET production regression rerun, PrepareUnity, Unity EditMode, Editor, native or desktop acceptance is claimed.

Reproduce from this branch:

```powershell
dotnet restore tests/HobbyDS9.TacticalUxPrototype.Tests/HobbyDS9.TacticalUxPrototype.Tests.csproj --locked-mode
dotnet build tests/HobbyDS9.TacticalUxPrototype.Tests/HobbyDS9.TacticalUxPrototype.Tests.csproj -c Release --no-restore
dotnet test tests/HobbyDS9.TacticalUxPrototype.Tests/HobbyDS9.TacticalUxPrototype.Tests.csproj -c Release --no-build --no-restore --logger 'trx;LogFileName=hardening-final.trx'
```

The project is deliberately outside HobbyDS9.slnx and existing CI workflows. Those workflows do not discover these 115 cases automatically. A future approved CI integration must run this explicit project command. Its package lock is committed; no production dependency is added.

### What these tests establish

- Fleet-only full order intents, retaining untouched fleet fields, unavailable standing focus and distinct same-route units. Same-fleet transport/escort remains one unit; no protected ally or ship-priority property exists.
- New unknown/hidden/allied focus is not synthesized by the presenter. This is an advisory picker guard, not authoritative legality or weapon acquisition.
- Selection, own/visible-ship inspection, resizing at all three resolutions, text rendering and repeated report replay issue **zero** action requests.
- Review/cancel is local. Commit is explicit, single-shot until receipt reconciliation; wrong-generation/request callbacks, duplicate clicks and stale/ambiguous outcomes cannot trigger silent retry.
- Sending, acknowledged waiting, report, next-round planning, encounter completion, completion review, finalization failure and mock-finalized states. No round 4 or speculative next human round.
- Same-scope chronology, frozen same-boundary private Waiting, exactly one resolved-round receipt, terminal same-encounter finalization, monotonic refresh/catch-up and per-encounter watermarks. Rejected reads/receipts retain observed facts and drafts; a matched bad receipt blocks further actions until explicit refresh. These are documented prototype rules, not inferred final production stamps.
- Dirty save preserves edits on success/failure; serialized test observation used as a save anchor excludes unsent orders. In-flight round/save/replacement cannot race. Failed load/absent consent keeps drafts; a validated successful replacement with a new generation drops them even when IDs/stamps match.
- Simulated batch readiness is an explicit test-owned input. Missing readiness blocks completion. Failed later-batch callback retains staged reports/pools and cannot partially display success.
- Own directional stocks and optional per-package stock/readiness facts, mixed/full/mutual cloak views, incoming-ordnance and focus-fallback text, and repeated/depleted subsequent views are presented as supplied. No aggregate toy stock is converted into a package or readiness reason. The presenter performs no reveal, firing, stock change, interception, recharge, target/power/cadence calculation or travel resolution.
- Privacy-shaped visible enemy models have only Ship, Fleet, Type, Hull and Shields; no reserves, max hull, fit or fresh-order field. A received filtered message is treated as actor-safe; this does not prove the backend filters correctly.
- Defensive copies, read-only collections, exact Int64 text and culture-independent formatting. Assembly dependency assertion excludes all production HobbyDS9, Stage A and Unity assemblies.

**Not established:** physical save-file replacement/atomicity, actual session validation, real human authorization, backend target access/privacy, simultaneous combat, production per-package pools, campaign completion/movement, GUI clipping, keyboard focus or mouse interaction. The recording fake only captures intents; tests inject responses and validated/load outcomes. Those injections must never be connected to a live campaign.

## Concrete Stage A contract-to-screen mapping

The Stage A column names actual executable **prototype** members. It does not identify implemented Application APIs. The UI model column describes test-only structs/classes in this package.

| Stage A member | Screen / presenter preparation | Gap before real adapter |
| --- | --- | --- |
| PrototypeObservation.Encounter / Round / Revision | Boundary identity, round heading and reviewed RoundIntent anchor | Revision is toy encounter-boundary revision, not global campaign revision or a production boundary token |
| PrototypeObservation.Committed | Own accepted / waiting state, never opponent readiness/count | Actor-scoped own acceptance receipt plus actual next boundary/round/completion required |
| PrototypeObservation.OwnShips | Detached OwnShip copies for hull/max, four current/max sectors and hidden/capable flags. Stage A aggregate toy ammo is **not** converted to the new optional package facts | Stage A elements are mutable detached copies; deep-copy to immutable UI facts. Per-package current stock/fit/power/cadence readiness remains a dependency; new package fixtures are independently test-authored |
| PrototypeObservation.VisibleEnemies | VisibleShip type/hull/four observed shield stocks; omit enemy maxima/reserves/orders | Need reviewed precision, lawful side/hostility and geometry/contact projection. Owner != actor in a toy is not a production diplomacy rule |
| PrototypeObservation.Reports | Filtered ReportRow text and observed fallback/incoming-ordnance summary | Stage A messages have Round/Audience/Message, **no event sequence**. Test row numbers are authored UI fixtures, not backend event IDs. Accepted ordered/filtered event schema required |
| PrototypeFleetOrder.Fleet / Maneuver / FocusFleet | One maneuver/focus row per owned active fleet; full owned orders in review | Observe does not return standing orders or legal choice lists. Must not obtain them from omniscient Export; production own-order projection required |
| PrototypeRoundCommand.Encounter / Actor / Command / Round / Revision / Orders | Test-only RoundIntent records full owned active-fleet replacements and observed boundary | Final production payload/stamp/replay behavior must follow accepted contract. Do not assume replacements-only or defaults |
| RoundAccessPrototype.Observe(actor) | Basis for actor-scoped read-model mapping | Callable on review assembly only. No real observation adapter implemented here |
| RoundAccessPrototype.Commit(command) | Basis for sending → own waiting → resolved boundary | Returns a toy bool. Production typed rejection/own acceptance and refresh semantics absent |
| PrototypeCheckpoint / Export() | **No screen mapping** | Private state contains enemy reserves, fixture rolls, opponent commitments and unfiltered history. Never hand it to renderer/inspector/tooltips |
| PrototypeBatch.ExportEncounter / CampaignShips / Finalized / FinalizeBatch | Toy proof of staged completion and rollback | No production batch summary, actor-safe resources or Planning/travel projection. Its injected failure argument is a review seam, not a player action |
| Prototype own cloak fields / revealed observation | Full/mixed/mutual cloak view fixtures | Strategic persisted approach setting/cost availability is not projected by Stage A Observe; show dependency rather than default enabled |
| Existing ICampaignSession.ExportSave and load factories | Reference for host ownership of JSON/atomic replacement | ExportSave currently does not persist a campaign-v2 checkpoint. Never serialize the UI read model as a campaign save |
| Existing v1 GetBattleReports / ResolveHumanCombat / SubmitCombatResult / FinalizeCombat | Preserve live v1 workflow unchanged | Do not repurpose them for a forged v2 fixture/result, generic mock receipt or private round state |
| ICampaignTacticalV2Inspection.InspectCampaignTacticalV2 | Source completeness/status only | InitialCurrent / InitialAmmunition are provisions; no current-resource/refill/round authority |
| Stage A travel alternatives / mobility contributions | Read-only worked evidence for backend review | No selected formula or production route/knowledge service; no client chase or power/target math |

### Extra fields are dependencies, not Stage A capabilities

The prototype Boundary has explicit nullable GeometryText, ApproachCloak, BatchReady and TravelStopped. Missing values render **DEPENDENCY** and remain absent; no Distant label, true batch readiness or stopped route is manufactured from round numbers.

LegalFocusChoices, own StandingOrders, encounter summaries and report sequence numbers are independently authored test fixtures. They are not computed by searching the toy's private checkpoint. Stage A Observe alone cannot supply them safely. The future adapter must receive actual lawful projections, not populate them from hidden fleet enumeration or assumed authority.

OwnShip.Packages is now optional. Null means the package projection is absent; an empty list is explicitly supplied no packages. Each supplied package may separately lack physical-stock kind, current stock, capacity or readiness text; each omission renders a dependency, never a zero/default/cadence calculation. Energy/no-physical-ammunition classification is a supplied fact, not inferred from class/name. Enemy models still contain no package/reserve/readiness/order fields. These test-only members are not new Application DTOs.

After an acknowledged local commit, the prototype keeps the **player's own** sent order locally for waiting display, separately from old standing orders and unsent edits. Stage A Observe does not expose those accepted order details after reload. A reloaded waiting prototype therefore shows that missing dependency, never guesses the commitment or reveals opponent acceptance. Production must persist/project the actor's accepted orders or expose an equivalent lawful receipt.

## Required production contracts before an adapter exists

The Stage A report proposes names such as CampaignTacticalObservation, CommitCampaignTacticalRound and FinalizeCampaignTacticalBatch. Those remain **proposed**, not callable methods in Application. The test IPrototypeActions.Record* seam is likewise not a proposed production API.

| Gate | Required authoritative behavior/data |
| --- | --- |
| B1 Allocation/source | Opted-in session-owned encounter, stable fleet/ship/exact fit membership, initialized-once current pools, active controllable units and lawful sides; default v1 path untouched |
| B2 Observations | One actor-safe atomic observation with current round/boundary, own current pools/orders/accepted commitment, visible condition, lawful maneuver/focus choices and permitted geometry. No hidden totals/reserves/order payload |
| B3 Stamps/commit | Accepted global campaign concurrency and encounter-round boundary semantics; Stage A proposes separate stamps/tokens, which need review rather than automatic adoption. Own command ID/replay protection; private joint commitments using identical lawful start information |
| B4 Receipts | Typed ordinary rejection versus own accepted/waiting/resolved; no opponent private commitment disclosure; explicit fresh observation after stale/ambiguous responses and no silent retries |
| B5 Weapon/cloak model | Full compatible weapon/power/cadence/opportunity/armor/recharge semantics, fleet-local defense, automatic legal target selection/fallback, actual cloak source/cost/visibility/reveal rules |
| B6 Reports | Ordered actor-filtered current-batch report schema, observed fallback reasons, secret launcher/reserve/order handling across pre/post-reveal and save/resume |
| B7 Save/load | Versioned supported current pools/checkpoints/standing orders/approach settings/private own commitments/RNG/replay and reports; validated session replacement; deliberate compatibility. No next format number guessed |
| B8 Batch/finalization | Lawful all-encounter readiness and exactly-once atomic consequences, canonical rollback on later failure, survivor resources/destruction and resulting Planning observation |
| B9 Travel/contact | Shared physical timeline and knowledge rules, mid-contact travel halt/non-executable interrupted routes, fresh next-turn leave/stay/follow, no catch roll/remote route trick |
| B10 Strategic cloak | Owned stamped Planning setting command and actor-safe persisted setting/capability/availability projection; no battle re-cloak button or pool restoration |
| B11 Acceptance | Accepted equipment/formula/version decisions plus real campaign R1 → save/resume → changed R2/R3 → finalize → next-turn encounter with depleted pools and old/new determinism proof |

B1–B11 must be fulfilled by actual accepted backend contracts. This test project's fake does not satisfy them. In particular, bool toy Commit and a toy boundary revision cannot be wired directly into the production command ledger.

## Proposed Unity implementation sequence

1. **Contract integration review:** pin accepted backend SHA and read actual DTO/API/receipt/version changes. Complete the mapping above, close unresolved semantics and inventory tests. Remove dependency placeholders only when their authoritative fields exist.
2. **Pure controller adapter:** convert immutable lawful Application observations to presentation facts, preserve original stamps/actor/generation, submit reviewed allowed fleet fields only. No prototype RecordingPort or fake success callback can remain on runtime path. Extend tests against real Application commands and rejection/rollback.
3. **View binding without redesign:** reuse LcarsPanelFrame, LcarsSectionHeader, LcarsReadout, LcarsDataRow, LcarsPillButton and existing tokens/layout policy. Render fleet rows, maneuver/focus fields, own pools and visible enemy condition; no protected ally, priority, per-ship fire/refill/catch widget. Keep scroll lists/details bounded and footer persistent. Do not convert Int64 values to imprecise meter math as an authoritative readout.
4. **Round flow:** bind Ready → Review → Sending → Waiting/Report → next Ready or EncounterComplete; explicit completion review → Completing → confirmed Planning. Each binding has a single reviewed command owner. Selection/render/replay/focus/layout handlers remain read-only.
5. **Save/session integration:** existing host owns JSON/slots and atomic replacement. Save committed state independently of local order draft; retain it on successful/failed save. Warn on successful load/restart loss, validate before replacing, increment session generation, invalidate old receipts/delegates. No save/load races with mutations.
6. **Strategic return:** show retained exact pools, stopped travel and fresh next-turn route/contact orders only from Planning projections. Persist approach setting while battle visibility changes; no extra movement/calendar processing in view.
7. **Acceptance:** headless real-session/controller proof first, then EditMode/view binding and IL2CPP/native at the three sizes. Exact-head portable CI is separate from Unity/native evidence. Obtain later explicit desktop/Editor authorization; none is granted by this package.

### Current state transitions and failure rules

The prototype does not automatically invoke a resolver or poll a campaign. Test code injects observed responses. Before integration, async callbacks must be correlated by request **and generation**; stale callbacks cannot attach to a loaded session with coincident IDs/stamps.

- Committing only from a local review creates one recorded intent containing all owned orders. Duplicate clicks/review/selection do not send another. Successful waiting acknowledges own submitted fields without exposing opposing state.
- A report callback only supplies already-decided state. Opening/replaying it cannot reveal by timer, calculate damage or advance the next human round. Round 4 remains blocked.
- Ordinary rejection retains the local proposal and requires explicit refresh/review. Obsolete/impossible observations and receipts are rejected by the prototype chronology contract without replacing read facts or clearing drafts. An accepted advanced boundary invalidates genuinely obsolete proposals with notice. Ambiguous send/completion exceptions require reconciliation, not a new command ID retry.
- Saving records a **save anchor**, not a serialized live save. Its fake callbacks test draft retention only; physical file atomicity is a separate real-host acceptance requirement.
- Unknown batch readiness cannot finalize. A simulated later-batch failure retains staged view state; actual atomicity is B8's backend proof, not an assertion about UI rollback.
- Validated session replacement requires dirty-edit consent/new generation and no in-flight operation. Failed validation or missing consent leaves old model/draft intact. Encounter navigation refuses carrying dirty edits into another encounter.
- No screen infers enemy count from cloak, derives power eligibility or decides focus fallback. All report/stock changes arrive as actor-safe observed input.

## Real-campaign acceptance script — pending B1–B11

**Not executed.** Use a separate backend-owned opt-in scenario with explicitly authored sources, sources/version identities pinned, required stock provisioning and reviewed cloak/side/contact data. Never attach the Stage A toy, test Boundary or presenter fixture as campaign authority. Do not give credits/stock, modify compiled results or infer cloak from names.

Run the complete script independently at **1280 × 720, 1920 × 1080 and 3440 × 1440** against the same final executable/source SHA. Save slots must be test-owned; preserve user's existing valid saves. Before GUI work, obtain new explicit authorization.

| Step | Actual action | Required observation/evidence |
| --- | --- | --- |
| A0 Baseline | Locked restore/Release/full suite, old/new canonical/reverse/repeated/reloaded checks, valid source/inspection and standalone regressions | Actual counts, versions, checksums and exact-head CI; no replaced legacy goldens |
| A1 Entry/privacy | Launch ordinary explicit live v2 scenario, open fleet/ship views and inspect all tooltips/report filters | Campaign activation is real; one command row per fleet, visible enemy type/hull/shields, no reserve/fresh-order/hidden-ID/count leakage |
| A2 Composition | Compare transport alone, same-route separate escort fleet, then an independently authored same-fleet escort scenario | Only same-fleet defense; finite actual equipment contributions and useful local mobility, no class buff/automatic wing |
| A3 Read-only interactions | Select fleets/ships, navigate tabs, resize each dimension, replay/skip report repeatedly | Exported authoritative state/RNG/ledger unchanged; no extra submission; selection is inspection only |
| A4 Orders | Draft maneuver/focus, change another row, cancel review, retain orders, submit once and double-click | Full owned fields retained, explicit one-round intent, own waiting receipt only, no opposing commitment leak/second round |
| A5 Stale/rejection | Use a real changed boundary/unavailable source/foreign actor/forbidden exit submission through headless tests | Exact typed errors and unchanged stocks/state/RNG/ledger; no automatic retry or mock successful report |
| A6 Focus | Make chosen focus unavailable via actual combat; another visible hostile offers lawful target | Automatic backend fallback explanation, unchanged standing focus, no hidden target or weapon-access guarantee |
| A7 Full cloak | Backend-authored fully cloaked own fleet versus visible hostiles, enabled strategic approach setting | Distant start; ordinary eligible R1 attack/cost; no offensive target of hidden ships; ordinary incoming-torpedo reactions; reveal at R1 end |
| A8 Mixed cloak | One fleet has capable cloaked and incapable visible ships | Visible members normally targetable; hidden members not leaked by aggregated counts/inspection/launcher event; no formation split |
| A9 Mutual cloak | Both sides fully cloak | Caution without fabricated opponent IDs/counts; maneuver-only R1/no nonexistent offensive ammo/rolls; reveal; normal R2/3 with same three-round cap |
| A10 Saved boundary | R1 resolved; draft R2 change; save committed state, fail save, then valid save/load/full process restart | Saving preserves live unsent draft; prior valid file retained on failure; successful reload restores committed state only; identical next events/pools/RNG |
| A11 Replacement | Load another validated session with matching IDs/stamps; exercise failed load and delayed callbacks | Failed load preserves draft/session; success clears old generation selections/receipts/draft; stale callback cannot send/change campaign |
| A12 Batch | Finish one encounter while another remains; save/load partially completed batch, finish it, inject real later-after-math validation failure in headless seam | Completion disabled until actual readiness; staged reports preserved; full canonical rollback, no partial damage/calendar advance |
| A13 Completion | Explicit finalization after all outcomes ready | Exact retained hull/four sectors/per-package ammo, destroyed entities removed, stopped travel/old routes non-executable, next Planning opens once |
| A14 Successive battle | Issue fresh next-turn mutual stay/combat, then independent lawful leave/follow cases | New encounter Distant with retained depleted stocks and approach setting, no refill/R4/same-turn loop; physical shared timeline and no catch-roll UI |
| A15 V1 regression | Normal v1 scenario: designer edit/publish/historical construction, production, research, save/load, map movement, phased combat/report/finalize | Existing accepted paths remain functional and deterministic; no default v2 activation |

For each resolution record client dimensions and authoritative checkpoint IDs; capture planner/review/waiting, own/visible inspection, no-visible-enemy warning, report/fallback/reveal, save warning, batch completion and stopped-travel states. Exercise scrolling to every essential control and long identifiers; footer Back/Save/review/commit remains reachable. At 1280×720 use compact tabs/scroll; 1920×1080 standard panels; 3440×1440 bounded control width with extra relationship/report area. Confirm no overlap/clipping, focus order and Enter/Escape behavior on the **actual** view. Tests of LayoutMode alone do not establish these results.

Reports should distinguish author/test execution, fake fixture inputs, headless real-session outcomes, Unity EditMode, native physical input, CI and optional human audio/GPU observations. A prototype transcript/screenshot or passing Stage A test is never acceptance of A0–A15.

## Handover and scope

Delivered on a separate local coherent branch preserving the full UX spec history. No push/PR/merge/default activation or other-chat messages. No desktop mouse/keyboard, Unity Editor or native tools. Current useful preparation is complete and committed/tested for review; later work requires concrete accepted B1–B11 capabilities and a narrow assignment.

The coordination cutoff is 08:30 Europe/Amsterdam on 14 October 2026 (06:30 UTC). This assignment reaches a safe checkpoint before that cutoff; it does not authorize independently beginning further work afterward or imply another task is scheduled.
