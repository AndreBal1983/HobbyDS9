# Tactical presenter hardening — prototype chronology and supplied package facts

13 October 2026. **Test-only hardening; no production adapter or campaign activation.**

Prior delivery SHA: **785bd46f4ead9872fc2bccbff6c0fd907a052ea0** on codex/tactical-v2-presenter-preparation. This is a forward local commit on that branch, preserving its history and the independent UX specification worktree. Exact resulting SHA is reported in the handover after committing, avoiding a self-referential document hash.

## Reproduced risks and evidence

The first [chronology regression set](../tests/HobbyDS9.TacticalUxPrototype.Tests/ChronologyRegressionTests.cs) ran against the prior presenter implementation: **27 failed / 27 executed**, with no client fix yet applied. It demonstrated lower-round/revision refresh, same-cursor phase reinterpretation, terminal reopening, impossible Report/Complete advancement, Waiting with changed stocks and wrong-encounter finalization. Red-run TRX: tests/HobbyDS9.TacticalUxPrototype.Tests/TestResults/chronology-before.trx.

Final execution: locked restore, Release **zero warnings/errors**, **115 passed / zero failed/skipped**. All 53 original named cases are retained and executed, adapting ammunition fixtures/assertions to explicit per-package facts. Added cases: **50 chronology** and **12 package presentation**. Current TRX: tests/HobbyDS9.TacticalUxPrototype.Tests/TestResults/hardening-final.trx. These ignored local artifacts establish headless presenter behavior only.

The source change is confined to this test-owned presenter/read models/tests and package documentation. No production, CI, backend worktree, live adapter, desktop, Editor, native control, push, merge or published history rewrite is involved.

## Deliberate prototype chronology

[BoundaryChronology](../tests/HobbyDS9.TacticalUxPrototype.Tests/Prototype/BoundaryChronology.cs) implements this conservative protocol. It compares identity, cursor, revision and supplied immutable facts; it does not validate combat rules, calculate targets/damage or decide server authority.

**Scope:** observations belong to a local session generation, actor and encounter. Actor/encounter IDs must be present. A validated replacement with a strictly newer generation may intentionally load an older saved boundary; it resets all per-encounter watermarks. Old-generation callbacks cannot affect it.

**Cursor shape:** Ready/Waiting have next-round cursor 1–3; Report has next-round cursor 2–3; EncounterComplete/Finalized have terminal cursor 2–4, allowing an early finish after round one or two. Revisions are nonnegative prototype tokens. Cursor 4 never appears as a playable round.

| Operation | Accepted prototype chronology | Rejected cases / retained state |
| --- | --- | --- |
| Matched private Waiting receipt | Same actor/encounter, same sent round and revision, identical frozen read facts; acknowledge only own sent orders | Lower/newer revision, another round, stock/readiness/reveal/report change or wrong scope. Keep observation/drafts; require refresh |
| Matched resolved Report receipt | Exactly sent round + 1, strictly newer revision; next-round cursor is 2 or 3 | Same/backward/skipped cursor, unchanged/lower revision or terminal Report cursor. No speculative second resolution |
| Matched EncounterComplete receipt | Exactly sent round + 1, strictly newer revision, terminal cursor 2–4 | Cannot jump a round-one commit directly to terminal cursor 4 or accept a same-cursor completion |
| Matched Finalized receipt | Same actor **and encounter**, unchanged terminal cursor from the sent completion review, strictly newer revision | Wrong encounter, terminal cursor change, unchanged/lower revision or non-finalized status cannot show success |
| Same-cursor refresh | Identical frozen facts and same status; Ready → private Waiting is also allowed | Same-stamp report/stock/readiness reinterpretation, Waiting → Ready, Report → Ready/Waiting or any payload drift |
| Advanced refresh | Both cursor and revision increase; may catch up over missed rounds while still nonterminal | Either coordinate moving backward, only revision increasing at the same active cursor, malformed shape or direct nonterminal → Finalized |
| Terminal refresh | Complete → Finalized at same terminal cursor/newer revision; identical repeated terminal observation is allowed | Complete cannot reopen; Finalized cannot become a new completion or acquire a newer revision in this prototype |
| Encounter navigation | Same actor; compare with remembered watermark for the target encounter | Navigating away/back cannot bypass chronology checks or regress an already-seen encounter |

Revisions need only be strictly newer on resolution/finalization, not exactly old + 1. This avoids pretending final campaign revision increments are known. A single commit receipt must represent one round; an explicit read refresh may catch up across multiple rounds.

At one immutable cursor, frozen facts include supplied orders, own/visible read facts, package fields, filtered report rows, choices and test-owned batch/travel/approach metadata. This is intentionally strict because the prototype has no separate global/batch revision. A real adapter may need independently versioned projections for metadata that can change without resolving the current encounter; it must be reconciled against accepted backend contracts, not relax guards arbitrarily.

Identical same-status refresh preserves local report acknowledgment, review state and unsent draft. Same-cursor private Waiting learned by refresh does not invent accepted order details: a local unsent proposal remains labelled unsent, and missing own accepted commitment detail remains a dependency. A valid advanced observation discards now-obsolete drafts with notice; an invalid observation does not.

**Return semantics:** ReceiveRound/ReceiveCompletion returning true means the request/generation matched and the callback was processed. It does not mean the supplied observation was accepted. An invalid matched callback is consumed, moves control to RefreshRequired and retains the prior observation, stocks, reports, selections and drafts. Unmatched/old-generation callbacks return false with no effect. Invalid Refresh returns false, preserves the current screen/control state and drafts, and sets an explanatory notice. None of these paths automatically retries or generates another command.

## Supplied per-package facts, not aggregate conversion

[Read models](../tests/HobbyDS9.TacticalUxPrototype.Tests/Prototype/ReadModels.cs) replace ToyAmmunition/ToyMagazineCapacity with optional OwnShip.Packages. Each immutable OwnPackage contains supplied ID/class, optional physical-stock classification, optional current ammunition/capacity and optional readiness reason. These are **test-local fields**, not declared production DTOs or Stage A APIs.

- Null package list means absent projection and renders a dependency. Empty list explicitly states no packages.
- Null stock/capacity/readiness/classification stays absent and dependency-labelled. Zero is an actual supplied zero, not an omission or percentage divisor.
- Two magazines retain independent values/IDs; no pooling, allocation, resupply, overflow-prone summing or transfer is performed.
- No physical ammunition is displayed only when explicitly supplied. A class/name is never used to decide whether a magazine exists.
- Readiness is supplied text only. The presenter does not compare current round, ammunition, reactor output, allocation or cadence to infer a reason. A deliberately inconsistent test fixture confirms that presentation does not invent a correction; authoritative validation remains backend work.
- Enemy VisibleShip still has only Ship/Fleet/Type/Hull/Shields. No reserve, package, readiness, private fit or upcoming-order field was added.
- RoundIntent still contains identity/stamp plus owned maneuver/focus orders only. Package/stock/readiness facts cannot become command payload.

Stage A's delivered toy contains one aggregate stock per ship. It cannot safely provide package IDs, variant-specific pools or powered/cadence readiness; no adapter converts that scalar into a synthetic launcher. New per-package fixtures are independently authored test data. Initial equipment provisions are not current live stocks.

## Adapter gates and pinned design basis

Agreed decisions are now available at local design snapshot **bc68de172318d19ee256c81351728d30d6445978**: [tactical chapter](C:/Users/ambal/.codex/worktrees/product-manager-design-handoff/HobbyDS9/docs/design/tactical-combat.md), [lifecycle milestone](C:/Users/ambal/.codex/worktrees/product-manager-design-handoff/HobbyDS9/docs/campaign-tactical-v2-lifecycle-spec.md). Their committed Git objects were verified. External local links may evolve; the recorded commit is the source identity. No snapshot branch merge/rebase occurs.

No newer delivered backend checkpoint was supplied for this hardening. The existing contract mapping remains pinned to Stage A **95a43685806f8b4fd724e6d645f854ac78f98571**. No in-progress Stage B files were inspected or bound during this task.

Before a real adapter: accept the actual actor-safe round/commit/completion API and global/boundary stamp rules; specify terminal/report/private-wait transitions; obtain authoritative per-package current pools and supplied readiness; provide lawful focus/geometry/own accepted-order/batch/travel/approach projections; validate filtered reports and save/reload/exactly-once aftermath. Keep the [full acceptance script and B1–B11 gates](unity-tactical-v2-implementation-acceptance-plan.md). This hardening closes client prototype regressions, not those backend gates.
