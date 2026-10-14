# Central combat and aftermath review client

14 October 2026. **Implemented in the explicitly opt-in physical tactical-v2 review client; local portable and graphics-enabled Unity EditMode checks passed. Draft delivery only.** Production activation and gameplay policy gates remain unchanged. This is the current implementation assignment, separate from historical UX documentation commit `455bf10`.

## Base, ownership and visual baseline

Branch `codex/central-combat-aftermath` starts at freshly fetched `origin/main` **c5d2b6ac32e8a6c47b073e58f53e9ecb129c11e4**. The requested merge of that checkpoint was already up to date. The root planning/art checkout, other worktrees, existing packages and player saves were preserved. Audio work remains separate; no AudioDirector/SfxDirector/helper/test, default ClientEntry, backend/contracts/schema, build/preparation script, compiler project or graphics/project-settings change is included.

Read [architecture](architecture.md), [design index](design/README.md), [tactical combat](design/tactical-combat.md), [ship design](design/ship-design.md), [movement](design/time-movement-mobilisation.md), [strategic AI](design/strategic-ai.md), [Stage B core](campaign-tactical-v2-stage-b-core.md), [physical travel](campaign-tactical-v2-physical-travel.md), [binding](tactical-unity-review-binding.md), [modal permission](unity-review-modal-permission.md), and [runtime follow-up](unity-review-runtime-followup.md). The [early local lifecycle plan](E:/HobbyDS9/docs/campaign-tactical-v2-lifecycle-spec.md) describes earlier missing contracts; the later gated review implementation supplies the authority used here. It does not approve proposed formulas, cloak costs, precision, travel timing or production activation.

The owner accepted the first-iteration visual/layout baseline on 14 October. This branch copies [the accepted art document](art/combat-interface.md) and [its illustrative interactive wireframes](art/wireframes/combat-interface.html), preserving the original art workspace. Undistributed reference/evidence links are explicitly local. HTML SHA256 remains `73d7c550937239ff2b321fad5aad5da241fa3bbb909167d6e33bdcdbf76672a1`. Earlier proposed/pending wording in the art source is superseded only for the accepted first-iteration layout. Its illustrative buttons/ships are not imported gameplay or native evidence.

## Actual changes

[ReviewEntry](../client/HobbyDS9.Unity/Assets/HobbyDS9/Review/Runtime/ReviewEntry.cs) binds the existing actual UI host to [ReviewCombatFlow](../client/HobbyDS9.Unity/Assets/HobbyDS9/Review/Runtime/ReviewCombatFlow.cs) and the engine-side [ReviewCentralView](../client/HobbyDS9.Unity/Assets/HobbyDS9/Review/Runtime/ReviewCentralView.cs). [ReviewCombat.uss](../client/HobbyDS9.Unity/Assets/HobbyDS9/Review/Resources/ReviewCombat.uss) supplies a fleet rail, large central schematic/stock/report area, bounded order dock and footer outside the scrolling detail panels. Long fleets, schematic observations, inspectors/reports and command fields scroll locally. The narrow class stacks these panels; stock/report/inspection tabs preserve local draft state.

The central schematic uses permitted text observations. No 3D model, choreography, camera-derived geometry, image asset, sound work or tactical math was introduced. Exact visible enemy condition is explicitly labelled the proposed active review policy.

| Actual state | Functional boundary |
| --- | --- |
| Planning / confirmed aftermath | Current own survivor hull, four shield stocks, per-package ammunition, actual turn and accepted strategic orders; fresh draft decisions below |
| Multiple encounters, none selected | Owned encounter picker available before a battle is selected; no misleading Planning travel controls |
| Round orders | Owned fleet maneuver plus lawful enemy-fleet preference; retain or deliberately clear focus; no per-ship fire or allied protection picker |
| Own accepted / waiting | Own receipt and committed choices; no duplicate commit or enemy readiness/count/orders |
| Round result | Filtered existing report and current stocks; explicit Plan next round acknowledges presentation only |
| Battle complete / pending | Staged stocks/losses; next strategic decisions unavailable; whole-batch readiness gates finalization |
| Confirmed aftermath | Exact-once confirmation applies existing backend transaction; fresh orders remain separate from finalization and turn advancement |
| No owned surviving fleet | Own current facts remain inspectable; no active route/follow/order actions or fabricated restoration |
| No visible enemy | Caution without hidden identities, count placeholders, reserves, orders or launcher disclosure |
| Closed | Central command controls removed; model guards still reject work |

Owned losses captured from completed observed battles are session-local explanatory labels. They clear on controller/actor/load replacement and do not invent a persisted campaign loss archive. Current survivors and destruction continue to come from authoritative snapshots.

## Post-battle decisions and existing guards

**Remain** submits existing Stay; **Re-engage** submits StayEngage; **Move away** submits Leave with an explicit known route; **Follow known contact** submits Follow with a fresh lawful contact and explicit route. A preview displays Application's route steps/cost/error, with no local ETA, chase roll or interception promise. Contact labels show permitted last-seen turn/node/freshness only. Own travel receipts expose actual stopped movement/progress. Accepted and UNSENT orders are distinct; selecting/previewing/drafting submits nothing. Changing a local destination/contact/action invalidates the submit affordance until a valid draft is prepared, while a rejected new draft preserves the old draft and accepted order.

Approach cloak changes use the existing owned Planning setting, gated by current authority and clean draft. During battle it is read-only. Finalization, a next-order submission and strategic advancement are separate deliberate operations. Whole-batch readiness, current phase/stamps, actor ownership, pending work and stale-refresh checks remain authoritative. No new targeting, movement, cloak, recovery, refill, round-four or exit policy is added.

The original epoch, one-use modal permission, sibling disabling, busy/closed checks and candidate-load guards are retained. Back collapses an active battle surface and exposes Resume battle without discarding or submitting. It does not open a new default-v1 session. Save committed state retains unsent local edits outside the save; failed load retains the old controller/draft, and successful replacement clears them. Test-host actor switching is collapsed and plainly labelled as trusted human hotseat setup, not opponent AI, authentication or opponent-readiness information.

Editor panel disposal uses DestroyImmediate outside play mode and Destroy in play mode. No global ExecuteAlways or competing bootstrap is added. The portable adapter links actual flow/model/codec/store sources and excludes only the two engine-dependent view/entry files.

## Executed verification

| Check | Actual result and limits |
| --- | --- |
| Locked restore and Release adapter build | Passed, zero warnings/errors |
| Complete actual-source portable adapter suite | **78 passed, 0 failed, 0 skipped**: all 64 existing cases plus 14 central-flow cases |
| New central-flow cases | Actual Application-backed two battles, waiting save/load, depleted pools/golden, all four travel drafts, unknown/stale route/contact rejection, no survivor, hidden enemy, multiple encounters and stale finalization race |
| Default PrepareUnity then PrepareUnityReview | Both passed; exact source resources and portable DLLs prepared |
| Default static asset/metadata/reference checks | Passed |
| Review PE/DLL/resource/hash import checks | Passed; source faster/slower composite hashes unchanged |
| Binding boundary checker and adversarial suite | Passed; **7/7** cases, including an updated injection anchor proving render-dispatch rejection after the Entry rewrite |
| Unity **6000.6.5f1**, graphics-enabled hidden batch EditMode | **127 passed, 0 failed, 0 skipped**, including all 121 existing cases and 6 new central-view cases |
| Native player, IL2CPP, mouse/keyboard, audio and physical resolution screenshots | **Not executed in this assignment**; combined native/build acceptance belongs to Primary/PM |

The actual Editor command used `-batchmode -projectPath ... -runTests -testPlatform EditMode`, omitted `-nographics`, and ran with a hidden process window. The log records a Direct3D11 device. New Editor cases execute actual Entry lifecycle/render bodies, Resources-backed Application state, UIDocument/UIElements, draft-preserving rebuilds and waiting/result/finalization transitions. Ordinary MonoBehaviours do not run their runtime lifecycle in EditMode; test-only reflection invokes those actual methods. The three size cases pass actual pooled geometry events to the actual registered geometry-handler body. They verify responsive class policy and unchanged authoritative frame, not runtime event delivery or rendered pixels.

Failures were investigated and preserved in ignored artifacts, not suppressed: first run **47/126** passed because the default fixture resources were absent and new startup tests had not invoked their lifecycle; after both preparations the 121 existing tests passed, while native SendMessage lifecycle assertions failed the 5 new cases. Replacing that EditMode fixture mechanism exposed one non-dispatched geometry test (**126/127**). The final actual-method geometry fixture and complete run passed **127/127**. No expected-log suppression, case removal or baseline golden replacement was used. Accidentally missing generated fixture metadata was restored. Editor-generated settings/line-ending deltas are excluded from the source commit.

Ignored local evidence: `artifacts/central-combat/editmode-verified.xml`, `editmode-verified.log`, earlier failed XML/log pairs, and `portable/central-adapter-final.trx`. These paths are execution records in this checkout, not distributed artifacts or screenshots. CI runs on the final exact head are recorded on the draft PR and in the delivery summary; CI performs portable/static checks, not licensed Unity/native acceptance.

## Determinism and scope retained

The real central-flow two-battle/save-resume sequence matches existing faster vector **a6a2522a4f389b0bb8aa562d6273b0f02b3ea1fdeb57c7fe21d8368e4fa4d177**. The complete adapter suite preserves slower vector **77073cc154b78dbca2e5f6fa1a618cae490d11410f51f4cb2bb9e640a33210c4**. Faster stock progression remains **7 → 3 → 0** torpedoes, with current hull and directional shields carried into the second battle. Simulation, content, save/rules/RNG versions and legacy v1/standalone resolver files are unchanged. Exact source package hashes remain faster `49e54967f76f8d84822731f8a31b715775ca673ea034d9643079979098735541` and slower `3b64263436ef37aa847d27d184e4cb973c69e21def201c8a483e7e4674344180`.

## Review launch and remaining acceptance

For a later explicitly approved player test, use the existing review builder/package and explicit arguments:

```text
HobbyDS9Review.exe --hobbyds9-physical-review --hobbyds9-review-profile faster --hobbyds9-review-actor civ.federation
```

This assignment did not build or launch that executable. The existing default selector/bridge and VSync behavior are unchanged. The screen itself identifies OPT-IN PHYSICAL TACTICAL V2, profile and actor. To exercise an initial battle, each human test-host actor drafts and submits Re-engage for its own scout fleet, then advances accepted Planning orders. Commit each human's current round separately; switch actor deliberately to supply that human's choices. After resolved results, Plan next round until completion; Confirm aftermath only when the whole batch is ready. Review/submit fresh strategic choices separately, then Advance. Save and load use the existing separate review slots.

| Size | What was checked here | Remaining combined native gate |
| --- | --- | --- |
| 1280 × 720 | Actual geometry-handler compact class and unchanged frame; source bounded stacked panels/footer | Real pixel layout, scrolling, clipping, legibility, focus and input |
| 1920 × 1080 | Actual geometry-handler standard class and unchanged frame; central column hierarchy | Real large central surface, readable dock/report and fixed footer |
| 3440 × 1440 | Actual geometry-handler standard class and unchanged frame; bounded rail/dock widths | Real wide layout, text measure, context and control reachability |

First-iteration layout acceptance precedes this implementation; it is not proof of this runtime layout's native quality. Existing keyboard-injection ambiguity and URP diagnostic investigation remain the separately documented follow-ups. Proposed gameplay/precision/version policies remain gated. Primary/PM will review and combine the separate audio delivery, build IL2CPP and perform real native interaction/visual acceptance. No merge, default activation, production campaign UX expansion or further feature is authorized by this draft delivery.
