# ENG-UI-02 — Engineering layout and projection boundary

15 November 2026. **Owner-approved layout; current support audit and proposed APIs. Documentation only.** Read alongside the [Engineering model](engineering-tree-model-01.md), [ship-design decisions](ship-design.md), [architecture](../architecture.md#engineering-development-tree-candidate) and [current content contract](../engineering-playable-content-v1.md).

## Settled layout and scope

Engineering has exactly two sections: **New Engineering Projects** and **Technology Refinement**. Each has technology-category filters. Selecting an entry opens the standard **LCARS management popup**.

New-project management presents the expected module/outcome and development time where supported. Refinement management presents the current version's maturity and chosen attribute where supported. This approval does not make unsupported fields authoritative, implement the 100–150 model, authorize new gameplay/save schemas or reopen a tree-GUI design.

The existing shared Science/Engineering split belongs under Science Research, not a second Engineering allocation subscreen. Popup navigation/filtering is local presentation state; it must not submit work or apply an unsent Research allocation draft.

**Inspected baseline:** GUI-POLISH-02 commit `b330f96b04f58bddf270393257c8630d1540c6dc`. Engineering Contracts, Simulation and Application sources are unchanged from the model's `1fb8bbe` source pin. The baseline remains a full-screen authored catalogue/detail view; no ENG-UI-02 candidate or native acceptance is claimed here. Receiving must record the actual GUI candidate separately.

## Support matrix

| Approved need | Available now | Honest binding / missing capability |
| --- | --- | --- |
| Two sections | Authored `EngineeringDefinitionSnapshot.Kind`: NewCapability, Refinement, CivilApplication | Group current entries by kind. Section membership is not a new gameplay project type. |
| Technology filters | Catalogue components have typed kind/family; hulls are separate | Research cards have no technology-category field. Supply reviewed presentation metadata or a read-only typed projection; never infer rules from titles or the first Science requirement. |
| Expected module/outcome | Authored name/effect summary, catalogue versions, content-owned version-access gates | Research has no typed output association. Prefer “Expected output” with Module/Hull/Ship design type; not every current reward is a module. |
| Development time | Pace-adjusted cost, retained work, accepted Engineering income and reserve | No authoritative ETA exists. Show a qualified current-rate estimate or an explicit unavailable reason; locked prerequisites are not scheduled completion dates. |
| Current version maturity | Current card has `MaturityPercent` and band for project work/cost | **Version maturity is not tracked.** Label current values “Project completion”; never copy them into a version-maturity field. |
| Chosen refinement attribute | Fixed authored effect summary and optional parameter assignment | No selected-attribute state or command. Show the authored direction as fixed; any future choice control is disabled with a clear reason. |
| Retained parent choice | Catalogue `ParentVersionId` and version-access facts | Authored lineage can be shown as “Authored parent.” It is not an actor-owned retained-parent experiment selector. |
| Management actions | Stamped `SetEngineeringTarget(projectId or null)`, eligibility/reasons, retained project work | Start/resume/switch/pause existing authored projects only. No repeatable cycle, generated version, outcome-save/rollback/continue or refit command is added. |
| Deployment result | Immutable catalogue/publication and profile-specific construction checks | Access does not guarantee a supported build. Saving/refinement never updates ships, designs or queues automatically. |

Current facts come from [ResearchOverview](../../src/HobbyDS9.Application/ResearchOverview.cs#L38), [definition snapshots](../../src/HobbyDS9.Application/CampaignSnapshot.cs#L32), [authored definitions](../../src/HobbyDS9.Contracts/EngineeringDtos.cs#L14), [catalogue/access contracts](../../src/HobbyDS9.Contracts/BlueprintDtos.cs#L8) and [target validation](../../src/HobbyDS9.Simulation/EngineeringCommands.cs#L9).

### Completion, generation and maturity are separate

Current `MaturityPercent = min(100, floor(100 × project progress / adjusted cost))`; its bands describe completion of an authored application. Completed applications cannot be targeted again. The [current rule](../../src/HobbyDS9.Simulation/EngineeringRules.cs#L10) implements neither retained-version maturity nor iterative 100–150 refinement.

Keep these fields conceptually distinct:

- **Project completion:** current work/cost and legacy band; supported now.
- **Scientific generation:** future knowledge-enabled generation identity; not the catalogue version number or project kind.
- **Equipment-version maturity:** the immutable result's own refinement state and policy; unsupported now.
- **Catalogue version / class revision:** identity and provenance, never a maturity percentage.

For the current refinement popup, use “Project completion: 42%” and “Version maturity: not tracked in this build” as an illustrative presentation pattern, with the actual projected percentage. A mockup may show future maturity/attribute controls only when clearly marked as proposed and non-executable. Never substitute zero, 100 or 150 for unavailable version data.

## Sections and categories for the current fixture

**Recommended presentation mapping**, not new content/balance: NewCapability → New Engineering Projects; Refinement → Technology Refinement. Future CivilApplication entries should remain in New Engineering Projects with a Civil category; none exists in the current fixture. Do not create a third section or silently drop unknown/unmapped content.

The [audited fixture](engineering-tree-model-01-catalogue-audit.md) has **8 new capabilities and 12 refinements**. Every project belongs to one section. Proposed category tags:

| Category | New Engineering Projects | Technology Refinement |
| --- | --- | --- |
| Hulls & ship capabilities | Advanced Starship Engineering; Expanded Utility Hull | Reinforced Utility Hull |
| Reactors | High-output Reactor | Compact Reactor |
| Propulsion | High-speed Drive | Efficient Drive; Maneuver Drive |
| Shields | Resilient Shield; Multilayer Shield | Efficient Shield |
| Weapons | Long-range Energy Bank | Compact Energy Bank; Improved Launcher |
| Life support | — | Compact Life Support; Efficient Life Support |
| Computing | — | Expanded Computing; Precision Fire Control |
| Sensors | Long-range Sensors | — |
| Laboratories | — | Compact Laboratories |

Bind this presentation map by stable definition IDs from the audit, not names, substrings or array position. Keep **All technologies** and a visible unmapped fallback. Filtering must retain current, locked and completed entries according to explicit player filter choices; eligibility must not silently remove them. Preserve selection/filter state independently in each section, and sort deterministically with stable-ID tie-breaking.

The fixture releases **17 component versions, two hull versions and one static ship-design unlock**. The expected-output field must preserve those distinctions. Where a gate has several required applications, project completion satisfies one condition; it does not promise immediate version availability while other conditions remain unmet. `AvailableVersionIds` alone cannot reconstruct which project enables which version.

## Concrete read-only projection recommendation

**Proposed API design, not implemented types or an approved schema migration:** an additive Application capture such as `CaptureEngineeringWorkspace(actorId)`, bound to one authoritative campaign frame. It should compose trusted content, Research and design-library facts once and expose immutable data. Unity must not merge unrelated captures from different revisions or reinterpret raw content into strategic eligibility.

Suggested fields:

| Projection | Recommended fields / semantics |
| --- | --- |
| Workspace | Actor, turn/revision stamp, source/content identity, current target, accepted Engineering income/reserve, entry list, explicit feature-support flags. |
| Entry identity | Stable entry key; source kind `AuthoredApplication` now, separately identified development/refinement opportunity later; existing project ID where applicable. |
| Presentation | Section key, category keys, display name; deterministic order. Presentation metadata cannot grant Science eligibility or version access. |
| Project work | Adjusted cost, earned work, remaining work in internal units; separately named project-completion percentage/band. Do not pace-adjust an already adjusted cost again. |
| Expected outputs | Typed list of component/hull/static-design outcomes, canonical IDs/family/parent where known, remaining access gates and deployment qualification. Authored summary remains a fallback when a typed association is unavailable. |
| Eligibility | Existing Science and prerequisite facts, satisfied flags, stable unavailable reason, action-specific enabled states. An active project can have `CanTarget=false` because its target is unchanged while Pause remains valid. |
| Time estimate | Kind `NextResearchPhase`, `AtCurrentRate` or `Unavailable`; phase count if known, income/bank assumptions, source stamp and unavailable reason. |
| Version refinement, future only | Parent version ID, scientific-generation identity, genuine version maturity with policy/scale, retained-parent options, permitted goals and selected goal for a committed experiment. Unsupported values are absent with an explicit support reason. |
| Actions | Existing target/clear actions now; future actions advertised only when their portable command/validation contract exists. Opening a popup never allocates an experiment or draws randomness. |

A **first projection-only increment** can add typed outputs/categories and the qualified ETA while retaining existing gameplay commands and save behavior. Build component/hull associations from trusted authored [version-access gates](../../src/HobbyDS9.Contracts/BlueprintDtos.cs#L16), joined to [catalogue version fields](../../src/HobbyDS9.Contracts/ShipDesign/ShipDesignSnapshots.cs#L111). Static ship-design outcomes instead require a reverse join of [ShipDesignSnapshot.RequiredEngineeringApplicationDefinitionIds](../../src/HobbyDS9.Contracts/ProductionFleetDtos.cs#L107); version-access gates alone do not cover Advanced Scout. Retain the qualified authored-text fallback when a profile excludes or does not expose that design. Do not parse `EffectSummary`, manufacture an authoritative whitelist in Unity, or equate an available version with a constructible fit.

For the currently approved GUI source scope, an explicit reviewed presentation-ID map can supply labels/filter categories without changing rules. If typed output/parent data is not exposed in that profile, retain authored text and unavailable labels; do not fabricate a live projection to fill the mockup.

### Development-time estimate

For an incomplete eligible project, use accepted snapshot inputs, all in internal Research units:

~~~text
R = max(0, adjusted Cost - retained Progress)
B = EngineeringReserve
I = EngineeringAllocation from the accepted current Research overview

if B >= R: next Research phase
else if I > 0: max(1, ceil((R - B) / I)) Research phases
else: no finite estimate at the current allocation
~~~

This is a **conditional estimate**, not a scheduled finish or a new rule. For a noncurrent project, label “if selected now, with current income/allocation unchanged.” For locked projects, no actionable completion date is known; show work and blockers instead. A completed project shows completed status, not a new estimate.

Example arithmetic in internal units: remaining 12,000, reserve 3,000 and income 3,000 gives three Research phases. If the reserve covers all remaining work, completion is still at the next Research resolution, not instantly on selection. Zero income with insufficient reserve has no finite estimate.

The [overview](../../src/HobbyDS9.Application/ResearchOverview.cs#L58) derives income from current economy and accepted allocation; it is not a guarantee of next turn's output. Do not use unsent slider values or confuse `ScienceAllocationUnits` (0–600 split) with `EngineeringAllocation` (internal work per phase). Display conceptual points by dividing internal units by 600. Use overflow-safe integer ceiling. [Actual spending](../../src/HobbyDS9.Simulation/ResearchPhase.cs#L15) occurs at Research; completion clears the target and cannot cascade to another project that phase. Do not promise calendar weeks unless the active profile's calendar contract supplies that mapping.

## Future Science-permitted goals and retained parents

This is the **gameplay seam for later authorization**, not permission to implement it with the layout:

1. Simulation validates actor access to a retained version, its family/generation, branch maturity/history and compatible goals. A catalogue parent pointer alone is insufficient.
2. Projected goal options carry canonical goal IDs, target attribute, relevant Science/capability requirements, satisfied flags, allowed bounds and unavailable reason. Science disciplines and structural breakthrough predicates remain explicit; do not infer eligibility from the first Science requirement, a project title or the provisional reactor ladder.
3. Once a generation/direction is enabled, normal repeated refinement requires no fresh Science while qualifying potential remains. Each chosen retained parent supplies its own maturity/history; a sibling does not consume it.
4. A future request such as `BeginRefinement(parentVersionId, goalId, contextId, expectedStamp, commandId)` names choices only. Simulation chooses/validates policy, work and deterministic outcome. The client supplies no desired stat vector, maturity, seed, detractor category or refund.
5. Committed experiments project their actual chosen parent/goal. Save/rollback/continue use persisted outcomes under the agreed no-refund/no-reroll semantics, with new paid work for another project. They cannot be implemented by calling `SetEngineeringTarget` on an already completed authored application.
6. Version comparison, explicit designer publication, new construction and eligible normal-queue refit remain separate capabilities. The approved reactor slice and 100–150 curve are **not current runtime support**.

The exact API shape, persistence/version changes and acceptance belong to a later scoped implementation. Human and AI must share the same eligibility and commands.

## LCARS popup, drafts and action lifecycle

Reuse the [existing campaign popup](https://github.com/AndreBal1983/HobbyDS9/blob/b330f96b04f58bddf270393257c8630d1540c6dc/client/HobbyDS9.Unity/Assets/HobbyDS9/Review/Runtime/ReviewEntry.CampaignMenus.cs#L24): owner/frame/epoch checks, one-use modal permission, background exclusion, scrolling, Escape and focus return. Its current body is text-based and focus handling covers body toggles/footer buttons; real selectors require a bounded content/focus extension, not an assumed existing capability.

Popup detail and enabled actions must use the same actor/frame as the catalogue. Revalidate stale commands; never silently refresh a stale permission and execute it against another revision. Closing, filtering or switching sections must preserve unsent Research split/share drafts and cannot submit allocation/target commands.

**Source/receiving clarification:** the existing [Engineering action branch](https://github.com/AndreBal1983/HobbyDS9/blob/b330f96b04f58bddf270393257c8630d1540c6dc/client/HobbyDS9.Unity/Assets/HobbyDS9/Review/Runtime/ReviewPlayerView.Engineering.cs#L105) directly submits target/clear for ColonisationCampaign; other profiles use a confirmation popup. There is no hidden confirmation in the wrappers. The modal's one-use lifecycle is not evidence of a universal extra confirmation for all target changes. Preserve the actual per-profile policy unless a separate change is explicitly accepted, and reconcile QA wording before claiming unchanged confirmation behavior.

Accepted target changes must preserve/rebase unsent Research drafts, as the [draft adapter](https://github.com/AndreBal1983/HobbyDS9/blob/b330f96b04f58bddf270393257c8630d1540c6dc/client/HobbyDS9.Unity/Assets/HobbyDS9/Review/Runtime/ReviewResearchDraft.cs#L85) already does. Cancel/Escape/filter actions must not invoke Reset. Actual render, keyboard/pointer, stale-frame and performance checks remain GUI/QA work; this document claims none.

## Receiving evidence and boundaries

Required future GUI evidence:

- Exactly two sections; all 20 authored entries reachable once across sections, with category coverage and a visible fallback.
- Popup expected outputs match real content; construction exclusions remain visible. Advanced Scout is excluded in Cycle; utility-version access does not prove Cycle construction support; sensor/Science ratings remain preview-only.
- Project completion, scientific generation, version identity and genuine version maturity are not conflated. Unsupported parent/attribute/maturity controls cannot execute or display fabricated values.
- Time estimates use accepted work/income/reserve, handle zero rate/reserve/locked/completed cases, and state assumptions.
- Existing target/clear validation, retained progress, read-only phase behavior, per-profile confirmation and Research draft rebasing remain intact.
- Standard popup navigation, focus return, narrow/wide layout, scrolling and stale callbacks pass actual candidate checks. Native/owner acceptance requires the real candidate, not this baseline audit.

This layout approval preserves the [main-HUD execution gate](engineering-tree-model-01.md#proposed-implementation-stages) and existing authorized GUI/assets/QA work. No tree GUI, new Science gate, generated result, maturity formula, save field or command is implemented by this document.
