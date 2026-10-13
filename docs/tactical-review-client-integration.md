# Headless tactical review client — real Application integration

14 October 2026 (Amsterdam). **Review adapter implemented; production activation disabled.** This is a headless presenter/controller against the actual gated campaign review session, not a live Unity screen or acceptance of the full campaign lifecycle.

The isolated branch is stacked directly on draft [PR #22](https://github.com/AndreBal1983/HobbyDS9/pull/22), pinned to **e117c95c7961bc424f190572f4e23a8bad7cfd08**. Main was fetched and remains **53d2e64973d9081a4d0090e771f5e777fc55cf40**. No unfinished physical-travel branch is imported. Read the [Stage B core report](campaign-tactical-v2-stage-b-core.md), [actual Application interface](../src/HobbyDS9.Application/ICampaignTacticalReviewSession.cs) and [review package](../scenarios/tactical-v2-campaign-review/README.md).

Consulted AGENTS, design index/tactical/ship/AI/time chapters, architecture and the lifecycle milestone at pinned documentation decision **bc68de172318d19ee256c81351728d30d6445978**. That milestone is not in this delivery tree; use the recorded Git object rather than treating another worktree's moving file as distributed documentation. Agreed behavior remains distinct from the proposed review rules.

## Delivered boundary

[ReviewHost](../prototypes/HobbyDS9.TacticalReviewClient/ReviewHost.cs) calls the real Application factories and ICampaignTacticalReviewSession, retaining its session privately. All consumers share its lock; it does not accept an externally mutable session. It does not implement Simulation transactions, opponent policy, targeting, mobility, combat, power or readiness formulas.

[ReviewController](../prototypes/HobbyDS9.TacticalReviewClient/ReviewController.cs) retains unsent fleet edits, requires review before round/batch confirmation, suppresses double clicks and consumes actual mutation results. [Frames/work/receipts](../prototypes/HobbyDS9.TacticalReviewClient/ReviewFrame.cs) carry lawful observations and precise request identity. [Text renderer](../prototypes/HobbyDS9.TacticalReviewClient/ReviewTextRenderer.cs) uses those projections only. Ship selection is inspection, never firing orders.

- Each fleet has one maneuver plus optional visible hostile fleet focus. Omitted fleets retain authoritative standing orders. No allied protection, ship priority, per-weapon firing or strategic retreat payload exists.
- Actual legal menus come from Observe. Presentation guards restrict those choices; Simulation validates every submitted command.
- Own accepted orders display separately from standing orders. Opponent commitments/orders are absent; Waiting does not invent opposing readiness.
- Selected encounters use Observe's current own stocks. InspectOwnShips is committed campaign state and would otherwise show pre-batch pools during staged combat. Four shield sectors, current/max hull and per-package ammunition/capacity/power/cadence come directly from Application; no pooling or computed firing readiness.
- Enemy inspection contains only visible type, hull and four shield stocks. Enemy fit, ammunition, power, hidden identity/count and fresh orders are not reconstructed. Actor reports retain supplied sequence/kind. Detail strings are neither parsed nor rendered.
- Same-fleet screening uses actual events and backend opportunities. Different friendly fleets retain separate controls even when co-located; the controller grants no screening benefit.
- Approach cloak submits the owned Planning setting with global stamps. Full/mixed/mutual visibility and round-one reveal follow observations. No mid-battle cloak command; incoming torpedoes need not disclose hidden launchers.
- Batch readiness comes from CaptureStatus. Complete encounters issue no further round. One confirmed finalization returns Planning with persistent own pools.

The isolated prototype targets .NET Standard 2.1/C# 9, references Application/Contracts, and has no direct Simulation/Unity assembly reference. Main solution membership, backend, default campaign, scenario bytes, legacy/standalone behavior, ci.yml and production Unity remain unchanged.

## Actual chronology

The old mock BoundaryChronology is not bound to the real API:

| Actual fact | Controller treatment |
| --- | --- |
| Global revision changes on each accepted mutation | Strategic setting/advance/finalize stamp; never used as a round token |
| Encounter revision changes only on resolved rounds | CommitRound sends the observed encounter revision and next-round cursor |
| Private own acceptance can keep round/revision unchanged | Enter Waiting and display actual OwnCommittedOrders |
| Opposing acceptance need not change actor observation | No inferred opposing order/readiness |
| Resolution advances one boundary | Refresh can catch up without automatic submission or speculative later rounds |
| Three-round completion normally has next-round cursor 4 | Render Complete; no Round 4 action |
| Finalize clears encounters and advances into Planning | Global stamped operation; inspect persisted own pools |
| Explicit valid load may restore an older boundary | New host generation; prior work/callbacks obsolete |

A matched receipt is processed, not necessarily accepted. Rejection retains prior projection/draft and requires explicit refresh. New authoritative boundaries invalidate obsolete drafts with a notice; no automatic carry/retry. Wrong-controller/foreign/duplicate callbacks cannot consume active work or repeat completion. Another actor loading the shared host invalidates outstanding receipts; other controllers must be rebound.

## Private host checkpoints

ExportSave is the complete **private host carrier**, including opponents' resume state. It never reaches frames/renderers/tooltips/player diagnostics. Only the trusted codec/store receives it; public checkpoint handles contain identity/stamp.

This increment uses an **in-memory store**, real JSON serialization/deserialization in tests and actual Application load validation. It proves accepted-order and boundary reload; it does not implement durable files/atomic replacement, cross-process restart UI, encryption, network authorization or Unity save slots.

Save keeps unsent local edits and excludes them from campaign serialization. Dirty load requires discard consent; success clears edits/selection and changes generation. Injected save failure preserves earlier handles/edits. Invalid serialized load retains the running session/frame/draft. This executes the proposed UX behavior headlessly; the future Unity flow remains proposed.

## Worked real-session checks

| Example | Observed behavior / scope |
| --- | --- |
| Fresh source | Cruiser 7/12 torpedoes; Fore/Port/Starboard/Aft 18/25/12/0. Unarmed surveyor has no packages |
| A commits Flank | Global revision increases, own round/boundary unchanged. B's observation remains byte-identical |
| B commits | Exactly one round resolves. Round 2 remains undecided; omitted Flank persists |
| Waiting save | Reload restores accepted command/orders. Another actor's unsent draft is absent and must be re-entered |
| Three rounds/finalize | Terminal cursor 4, then turn advances once with retained pools. Repeated finalization returns WrongPhase |
| Second co-located encounter | Begins Distant with prior survivor stocks; no refill. Tested two-battle script exhausts cruiser torpedoes |
| Full/mixed/mutual cloak | Hidden members absent from enemy inspection/focus/relations; mutual round one spends no offensive ammo; then reveal |
| Same-fleet/separate escort variant | Actual Screen produces 2 defensive attempts within fleet versus 0 cross-fleet attempts; candidate tuning is not approved here |
| Destroyed focus variant | Standing focus persists; typed FocusFallback identifies only lawful replacement. No detail-string parsing |
| Obsolete/rejected response | No automatic retry, stock change, duplicate round or rollback |

Variants adjust validated source fixtures to isolate screening/fallback, not fake resolved combatants or receipts. Hash checks apply to the unchanged baseline package. Co-location is the pinned legacy review allocation gate, **not physical pursuit, contact-intent or next-turn leave/stay/follow acceptance**.

## Separate executed evidence

Local SDK 10.0.401, locked restore, Release **0 warnings/errors**:

| Suite | Result | Meaning |
| --- | --- | --- |
| Pinned main solution regression | **1,621 passed; 0 failed/skipped** | Existing backend/legacy regression with unchanged inputs |
| New actual client | **51 passed; 0 failed/skipped** | Real Application sessions, projections/results, serialized checkpoints and presenter/controller |
| Preserved mock presenter | **115 passed; 0 failed/skipped** | 53 interaction + 50 mock chronology + 12 package cases; no campaign authority |

[Real tests](../tests/HobbyDS9.TacticalReviewClient.Tests/) are separate from [preserved mocks](../tests/HobbyDS9.TacticalUxPrototype.Tests/). Canonical/reversed/repeated/reloaded sessions have identical private saves and lawful rendered reports after each round/finalization in en-US/nl-NL/tr-TR. This does not replace backend legacy/standalone vectors or claim native determinism.

Reproduce for each explicit test csproj: dotnet restore with --locked-mode, dotnet build -c Release --no-restore, then dotnet test -c Release --no-build --no-restore.

[Dedicated CI](../.github/workflows/tactical-review-client.yml) runs both explicit projects on Linux/Windows/macOS, checks separate 51/115 TRX counts and uploads evidence. It does not modify the main workflow; the main solution does not discover these cases. Exact head/run are reported in the draft PR and handover.

## Preserved histories and remaining gates

Only needed test/docs paths were imported from **202b26b8e79a87ef212c9789c1ae861ce02b9062**. No older-main ancestry was merged. PR #21 **b1b10f8638d8f5ca2635885c917f7c96ce0819a4**, UX branch **455bf1030630e6ffbdbd8a84898d6823f8801ae3**, and audit **62813f64cdcdaaaa6a14b369c5a8fff45d6f1491** remain preserved.

The imported [acceptance plan](unity-tactical-v2-implementation-acceptance-plan.md), [hardening report](tactical-v2-presenter-hardening.md) and [UX spec](campaign-tactical-v2-playable-ux-spec.md) are historical at their stated checkpoints. Their then-pending Stage B status/mock assumptions are not this real adapter's runtime contract.

- Readiness explanation is absent from backend projections: display supplied facts plus unavailable reason, never derive fire opportunity/chance.
- FocusFallback supplies typed kind/target but no detailed lawful reason: generic copy only, pending a safe causal projection.
- No atomic external multi-writer combined status/observation API: this local host serializes its private session. Remote writers need an explicit contract.
- Physical travel/contact and fresh next-turn intent remain Primary-owned pending work, excluded from this pinned integration.
- Unity binding, actual rendered 1280×720 / 1920×1080 / 3440×1440 layouts, input/focus/accessibility, durable slots and live campaign interactions require a later authorized stage.
- Review-policy tuning and equipment/lifecycle acceptance remain open. This stacked client PR stays draft; activation remains false.

No Editor/native/desktop, merge, force push or other-chat message is part of this delivery.
