# Early campaign player UI recovery

14 October 2026. **Review candidate, not a validated native demo.** This replaces the diagnostic integrated shell with live player navigation in the same authoritative campaign. The owner rejected the previous package's usability; its passing tests did not establish this outcome. The source baseline incorporates [the scoped parity contract](integrated-playtest-feature-parity.md) and its accepted API, preserving the earlier integration histories.

## Player changes

- Selected navigation and concise actor/turn/phase/credits remain visible. Map, Worlds, Research, Production, Fleets, Combat, Save/Load, Audio and local Ship Design preview are named pages; diagnostic actor switching and refresh are under Test Tools.
- Science shows all six summaries together at 1280×720, with obvious Science/Engineering tabs, local allocation previews, details on demand and a pinned Apply action. R&D split uses a slider instead of an oversized popup. Real prerequisites still apply; Engineering is not unlocked for presentation.
- Production retains the selected world and uses actual eligible templates, payment, Industry and queue facts. Purchase confirmation names Cruiser/Earth, 200 credits, five Dilithium and 100 Industry. Cancel changes neither resources nor draft. The same source ship still requires thirteen resolved production turns; no fake instant ship or pacing change is introduced.
- Worlds presents only owned environment/economy/cohorts/infrastructure/stocks and public reference names. Governance submits the existing stamped command. Global population/delivery history is not repurposed as owned intelligence.
- Fleet navigation separates selection, known destination, preview and confirmation. Changing fleet/destination invalidates the offer. Confirmation freezes the displayed exact route offer; controller validation rejects obsolete host/stamp offers. It never silently replans to another selected destination.
- Save slots remain in the separate integrated format-3 namespace. Campaign 1 is initially selected. Saving retains local edits; failed load preserves them; successful load replaces the session. Audio uses the existing actual services/preferences without starting another campaign.
- Local v1 design preview reuses the existing editor and Application compiler with the actor-owned catalogue. Navigation retains that preview; dirty edits block advance/travel and actor/load replacement requires deliberate discard. Detached former-actor callbacks cannot dirty a replacement workspace. Publication from this page stays disabled; arbitrary v1 blueprint construction in v2 remains unsupported.
- Purchase dialogs contain only their action summary. Combat keeps its existing accepted central interface, order/once-only/privacy guards and explicit aftermath. Guidance distinguishes waiting, round results and completed battle aftermath. Escape cancels integrated confirmations and retains edits.

The decorative background is the existing text-free art-rollout asset, with tracked texture metadata. Full concept screenshots are not runtime panels. The supplied designer Overview image remains unchanged. Numerical gameplay, save/content/rules identities, VSync and default launch activation are unchanged.

## Evidence and its limits

| Check | Observed result |
| --- | --- |
| Locked solution restore and Release | Passed, zero warnings/errors |
| Complete .NET suite | 1,742 passed, zero failed/skipped |
| Existing portable adapter suite | 107 passed; three additional focused cases passed for design lifetime/scoped library and exact route identity |
| Integrated authoritative headless walkthrough | Canonical/reverse/reload/reordered passed, unchanged package/save goldens |
| PrepareUnity/PrepareUnityIntegrated and boundary/static checks | Passed |
| Graphics-enabled full Unity EditMode suite | 144/144 passed, zero failed/skipped: inherited 143 plus actual runtime UI render/outcome case |
| Actual runtime panel rendering | 75 PNGs across 1280×720, 1920×1080 and 3440×1440; these are Editor/runtime-panel pixels, not native player screenshots |
| Live UI event walkthrough | Research preview/apply/cancel; paid queue 250→50 credits; turn-13 completion; both human routes; waiting/three joint rounds/results; explicit aftermath; unique format-3 save/load with retained ammunition passed |
| Final small guidance/theme/old-designer-callback/Escape checks | API compilation passed; targeted graphics verification pending at this checkpoint |
| New IL2CPP package/native operator input/restart | Pending; older binaries do not contain this UI |

The render fixture enters real Play Mode through the explicit integrated bootstrap, dispatches focused `NavigationSubmitEvent` to actual buttons, uses actual `PanelSettings.targetTexture`, and verifies card/action bounds against viewports. Setup/inspection uses reflection; it does not submit fabricated combat state, grants or results. Keyboard event injection is distinct from physical native input. The save action uses a fresh unique review slot and never overwrites existing owner slots. Renderer evidence does not establish hardware audibility or GPU performance.

Local evidence is under this worktree's `artifacts/early-game-player-ui/`: `editor-full-1.xml`, `render-tests-10.xml`, .NET TRX and `rendered/`. These ignored artifacts are local review evidence, not distributed runtime assets. Delivery SHA and CI link belong in the PR/delivery checkpoint rather than a self-referential source hash.

Preserved integrated package SHA256: `88a919f6f51d244f79bb638ba933ff3c17305f25231a6b793aa6d53fca5f688b`; complete save golden: `417d1852e052b4fe1f4f2b5f611ba49b82b7d76cd6154540a58bcaefc474e865`. No golden was updated.

## Remaining delivery gates

Independent exact-source review, final targeted graphics checks, exact-head CI and a new versioned Windows package/native walkthrough remain. No latest-validated-demo pointer or main merge is created by this worker checkpoint. The current scenario still has two human sides; switching under Test Tools is an explicit technical limit, not AI. Raw combat IDs and schematic presentation remain visible in the inherited central surface.

The next bounded milestone connects the separately owned single-player contract, lawful AI/objective and wrapper-4 save namespace, then Start Demo/Continue, contextual onboarding and display controls. Original fleet art follows its separately validated asset contract. This first UI recovery does not claim the autonomous playable-demo objective complete.
