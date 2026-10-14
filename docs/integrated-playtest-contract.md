# Integrated playtest backend contract — interim

**CURRENT assignment, 14 October 2026. Interim API/design seam for Secondary; not yet implemented or accepted.** Branch `codex/integrated-playtest-backend`, based on remote main `c5d2b6ac32e8a6c47b073e58f53e9ecb129c11e4`, preserving audio PR #35 `1eb5435d098192b9681f803759a5f7d209eddc1b` and central combat PR #36 `890c6b70ee81296cff34973fdb8fc166af1ec858` through merge commits. No default activation or main merge.

## Shared session and proposed API pin

Application will expose `CampaignIntegratedPlaytestApplication.StartPackage(CampaignIntegratedPlaytestPackageDto)` and `LoadSave(CampaignIntegratedPlaytestPackageDto, CampaignIntegratedPlaytestSaveDto)`, returning `ICampaignIntegratedPlaytestSession`. It extends the existing `ICampaignTacticalTravelSession` travel/contact/tactical surface. All panels must retain **the same returned instance**. No parallel default-v1 or fixed-review session is a substitute.

Additive operations:

```csharp
CampaignIntegratedStrategySnapshot CaptureStrategy(string actor);
CampaignIntegratedPlaytestSaveDto ExportIntegratedSave(); // trusted host/store only
```

`CampaignIntegratedStrategySnapshot` will live in Application and contain immutable copied `Status` (`CampaignTacticalReviewStatus`), own `Civilization` (`CivilizationSnapshot`), `Research` (`ResearchOverviewSnapshot`), owned `Colonies`/`Ships`/`Fleets` (existing snapshot types), filtered `Navigation` (`NavigationMapSnapshot`), and `Construction` (per-owned-colony options for explicitly admitted static sources). Construction entries will expose colony ID, exact immutable static design (`ShipDesignSnapshot`), fit ID/revision/provenance and normal advisory validation state (`CommandErrorCode`, `CanEnqueue`). These names/types are the early seam; minor compilation details will be resolved and recorded before final delivery. Renderers receive only these actor projections, own travel/contact views and actor tactical observations, **never a private full save/core/global campaign snapshot**. Uncontrolled/unknown actors reject.

Submit existing observed-stamp `CommandEnvelope` through inherited `SubmitStrategicCommand`: `SetResearchAllocation`, complete six-way `SetScienceAllocation`, `SetEngineeringTarget`, admitted static `EnqueueProduction`, and owned `CancelProduction`. Existing payloads, IDs, 600-unit research arithmetic, fees, industry, eligibility and command validation remain authoritative. Physical routes use inherited `SubmitTravelOrder` / `FindKnownRoute`; tactical commitments and finalization retain the existing physical facade. During combat, strategy views may be inspected but strategic edits are blocked by the authoritative physical root phase.

## Explicit construction admission and compatibility

An additive integrated wrapper declares exact **static design → authored fit/revision/provenance/initial stocks** bindings. Only expressly listed valid existing authored-v2 templates may enqueue. Completion uses normal paid FIFO Industry/ship allocation/join-or-new-fleet construction, then validates and initializes equipment/pools **once** inside the same staged whole-root transaction. No arbitrary immutable v1 designer blueprint conversion, inferred stats, refit, general compiler support or unearned Engineering unlock is authorized. Ordinary unsupported static designs and `EnqueueBlueprintConstruction` remain typed unsupported admission failures. New fleet travel/cloak settings must be initialized coherently in the same transaction.

Integrated package/save wrapper **format 3**, profile `campaign-tactical-v2-integrated-playtest-1`, deliberately encloses the existing physical format-2 root and authored construction binding/provision provenance. Nested B1 format 1 and legacy Save 14 / authored Content 11, rules `science-engineering-v1`, compiler `ship-design-compiler-v1` and existing movement/tactical resolver identities remain unchanged. The wrapper owns new semantics; old bare v1/B1/physical saves are unsupported for this new loader with explicit errors, **no silent wrapping/migration**. Existing profile fixtures/loaders/hashes stay unchanged. The new wrapper persists complete strategy, queue, equipment provenance, stocks, movement, rounds, phase and replay ledgers; never save its nested root alone. Package hash and exact source bindings reject substitution/cross-profile reloads.

## Scenario and delivery constraints

New opt-in scenario path: `scenarios/integrated-playtest-v1/`, with explicit package/physical/core/scenario inputs and proposed-policy/default-disabled labels. Initial ship equipment and construction source values reuse valid authored definitions. Starting economy stock, prices, Industry pacing and Science/Engineering prerequisites will be documented; do not grant future technology or invent research/production buffs. Actual allocation/project progress, paid completion, produced ship movement/contact, tactical aftermath and complete-root reload must pass real backend tests before final contract is marked implemented.

Primary owns Contracts/Application/Simulation, new scenario/schema/backend tests/report. Secondary owns client/controller/host adapters, Unity strategic/R&D/production/navigation presenter and tests. Neither should edit the other's paths/shared scripts without coordinating a concrete seam. After backend passes, Primary delivers a draft PR and exact pin, then stops for Secondary integration. Combined Unity/IL2CPP acceptance is a later PM dispatch; physical sound/render/input remains manual. No prototype substitution or redundant native build at this checkpoint.
