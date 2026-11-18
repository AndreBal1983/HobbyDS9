# Engineering outcomes — Backend / GUI delivery contract

18 November 2026. **Owner-authorized implementation direction; this document is a contract, not implementation or acceptance evidence.** Deliver the smallest real targeted-refinement loop for the [playtest brief](../playtest-feedback-20261118.md). Backend owns portable rules, state and commands; GUI owns presentation/bindings; Integration & QA owns the combined build and native checks.

## Authority and source reconciliation

This delivery also includes the brief's source-link correction at `b3259d0b74ed797dbd1ddb99448426a3e7c49e40`.

The playable source pin is `af0273c2ab89ff199e8e7dfb57d8a69dfbb896c8`; brief pin is `776fd39f031a1d60b433e1861a9396ed4358352b`. The playable branch does **not** contain the newer Engineering design documents. Use the pinned [Engineering model](https://github.com/AndreBal1983/HobbyDS9/blob/0bbb381de78db46d440fd7dd5f86b261552387d2/docs/design/engineering-tree-model-01.md), [ship-design decisions](https://github.com/AndreBal1983/HobbyDS9/blob/0bbb381de78db46d440fd7dd5f86b261552387d2/docs/design/ship-design.md), [UI support seam](https://github.com/AndreBal1983/HobbyDS9/blob/0bbb381de78db46d440fd7dd5f86b261552387d2/docs/design/engineering-ui-02-support-seam.md) and [capacity-block semantics](https://github.com/AndreBal1983/HobbyDS9/blob/0bbb381de78db46d440fd7dd5f86b261552387d2/docs/design/capacity-blocks-01.md). Their earlier design-only restrictions describe those earlier assignments; the new brief explicitly authorizes scoped implementation. Preserve the agreed/proposed/open labels of their tuning and wider roadmap.

There is no blocking owner-policy conflict in Accept / Continue / Discard when implemented below. Three interpretations would conflict: automatic deployment on Accept, free downside removal on Continue, or deleting/rewinding the resolved experiment on Discard. The old authored 20-project catalogue is a compatibility fixture, not a substitute for this repeatable loop.

## Outcome lifecycle

The following are operational interpretations of the agreed retained-version model and the new brief. Version characteristics are immutable; retention, availability and project status are separate state.

| Action | Authoritative effect | Boundary |
| --- | --- | --- |
| Start targeted refinement | Validate the retained parent, supported goal, Science/generation access, policy bounds and normal Engineering work; commit one experiment. | Preview/goal selection spends nothing and allocates no random attempt. Keep the existing one-target scheduling model; no free parallel work. |
| Complete work | Persist one complete result, actual maturity and actual side effects atomically; make it awaiting review. | Every completed cycle improves its selected attribute. No failure/no-gain outcome disguised as success. Pending decisions survive navigation, turns and save/load. |
| **Accept engineering outcome** | Retain and release that exact result into the supported version library for designer selection. | `CanAccept` requires actual release/readiness eligibility and a supported release path. No class publication, construction, refit or installed-stat change; fit/build validation follows separately. |
| **Continue development to reduce downsides** | Retain the result as an internal parent and start another normal paid targeted project using its exact vector and maturity. | No implicit release. Open goal selection first; a detracted attribute may be preselected. The submitted goal must be explicit. If starting is invalid, leave the original outcome pending and unchanged. |
| **Discard outcome** | Mark the unreviewed candidate declined/unavailable; retain its immutable result and experiment history. | No refund, deletion, ancestor change, RNG rewind or saved-result reroll. It is not a command to delete an already accepted/used version. |
| Close / Escape | Close the modal only. | Does not accept, continue or discard. The pending outcome remains recoverable. |

Continue is ordinary refinement: it improves the new selected target under the same curve and 70/30 mixture; it does not promise to restore the ancestor's value or avoid another downside. If there is no downside, use a general continuation label such as **Continue development**. An internally retained parent may later be explicitly accepted/released; starting another child and releasing the parent are distinct actions. An accepted version can also parent later work through normal parent selection. Duplicate submissions must not start two children.

Any retained version may parent a new paid branch. Its own maturity/history applies; siblings consume no shared maturity pool. Returning to an older parent creates a new paid experiment with a new durable identity, without modifying the earlier experiment. Existing class revisions, queued builds and installed ships retain their frozen references.

Historical storage is not parent eligibility: a discarded candidate survives only as evidence and cannot later be selected, accepted or continued. An internally retained Continue parent remains eligible; Discard cannot target it or another accepted/used version.

## Supported values, maturity and tuning

**Agreed:** every completed refinement improves the selected attribute; initial tunable mixture is 70% no detractor / 30% one real detractor in another supported attribute. These are probabilities, not an enforced seven-in-ten sequence. Freeze starting maturity `M` for the entire cycle and use:

```text
r(M) = 1                  for M <= 100
       (150 - M) / 50     for 100 < M < 150
       0                  for M >= 150
```

Apply this factor to target improvement, maturity increment and detractor magnitude. Do not scale the 70/30 odds, multiply the entire stat vector by maturity, or increase the initially fixed cycle workload exponentially. Maturity belongs to the equipment version/branch. Project work/cost, Science generation, release eligibility and maturity are distinct quantities.

**Recommended narrow attribute admission:** start with module space reduction, actual power-demand reduction, reactor supply increase and Industry reduction where supported. Reactor output is supply; an ordinary module's power is demand. Other outputs may be added only with explicit units, benefit direction and a real compiler/runtime consumer. Preview-only sensor/Science ratings are not exploration performance; credit valuation is not a refinement target. Keep material types, qualitative abilities, mounts and generation access unchanged by ordinary percentage refinement. For a reactor, do not present zero power consumption as a reducible goal.

**Proposed, versioned implementation policy:** use fixed-point/rational maturity and bounded representable outcomes. Admit a goal only if both a clean outcome and a one-detractor outcome are jointly legal and useful, with positive target gain and maturity gain. Select the category once, then sample within that category. Reject practical exhaustion before charging; do not round away a detractor, redraw for a clean result or snap maturity to 150. Preserve protected requirements and disclose all changed values; a percent gain minus a percent loss in different units is not proof of overall value. Backend must define a narrow protected service/context and its admissibility test rather than claim universal superiority.

**Tuning remains proposed:** base maturity step 5, target gain 8–12%, downside 2–5%, initial release around 80, minimum maturity step 0.1 and any workload/range are examples, not mandatory balance. Backend may select a bounded first policy within this authorized task, recording its exact initial maturity, precision, costs, ranges, protected limits and deterministic sampler. Initial maturity must be authored explicitly for a new edition/realization, never inferred from legacy work completion. Ranges must work on the actual integer grid; tiny Industry or space values may need a declared attribute-specific quantum/range. A 12 → 9 compactness change is 25%, so the owner's visual example cannot be promised as an 8–12% generated result. Project previews show the actual supported range or a qualified expected value; resolved popups show exact values.

At M=125, a provisional base +5 maturity / 10% benefit / 3% downside becomes +2.5 / 5% / 1.5%; the workload and 70/30 odds stay unchanged. At M=150 no new improving cycle is admitted. At smaller values, representability can exhaust a goal earlier. Such policy limits must be visible, not paid no-op projects.

## Minimal command and snapshot boundary

These are semantic API shapes, **not declarations that these types already exist**. Backend supplies final names and compatibility IDs; GUI binds the returned fields rather than reconstructing rules.

| Boundary | Required inputs / outputs |
| --- | --- |
| Start refinement | Standard actor, command ID and observed turn/revision; exact retained parent ID; supported goal ID; observed policy identity if needed to reject stale previews. Client supplies no result statistics, maturity, RNG, workload or refunds. |
| Decide outcome | Same command guards; persisted outcome/experiment ID; Accept / Continue / Discard; explicit next goal for Continue. Validate current availability/decision state. Continue commits retention and next-project start atomically. |
| Project snapshot | One actor/frame/edition stamp; parent identity and full before vector; allowed goals; target label; work paid/required and completion; actual version maturity with scale; actual expected ranges; per-action availability and reasons. |
| Outcome snapshot | Exact persisted outcome and parent IDs; target; Success; before/after vectors; all actual side effects; actual result maturity; retention/release state; eligible follow-up goals; action flags/reasons. No side effects is derived from equality of non-target supported values, never guessed from the category label alone. |
| Durable pending list | All awaiting-review outcomes in stable order, recoverable from authoritative state after loading. Selecting/dismissing a notification cannot consume this list. |

Use shared human/AI validation, current command deduplication, Planning/ownership guards and atomic failure. A stale, repeated or invalid request changes no work, state revision, allocator, experiment counter or RNG. GUI refreshes on rejection; it never silently replays a new intent. Drain pending Research allocation commits through the existing serialized auto-apply boundary before capturing an actionable outcome frame. If a later drain changes the revision, invalidate the old popup and require a fresh reviewed action; never attach a fresh stamp to its old intent.

GUI reuses the current two sections and LCARS modal, its background exclusion, permission/frame checks, focus restoration and keyboard handling. Replace prose with Target research, Expected outcome, Hull space, labelled Power, labelled Output, numeric Industry cost and actual Maturity. Use shared capacity blocks and exact before/after values; identify the operating profile (the current comparison selects Cruise). Unknown/unsupported values remain unavailable, not zero. Legacy cards say **Project completion** and **Version maturity: not tracked**. No internal IDs in normal player copy. A result is never generated by rendering, resizing, notification acknowledgement or reopening the modal.

## Backend dependencies that must reach the playable build

At the pinned baseline, [typed Engineering outputs](../../src/HobbyDS9.Application/EngineeringOutputSnapshot.cs) already associate authored component/hull/static-design outputs and parents. [EngineeringRules](../../src/HobbyDS9.Simulation/EngineeringRules.cs) still complete authored projects at 100%; [ScienceEngineeringStateDto](../../src/HobbyDS9.Contracts/ScienceEngineeringDtos.cs) has no generated experiment registry. [ResearchValidation](../../src/HobbyDS9.Simulation/ResearchValidation.cs) equates mature application IDs with completed authored work. Do not reinterpret these legacy fields.

Accept is not complete when an ID merely appears in a menu. Generated versions need campaign-owned catalogue/access resolution, designer validation, explicit publication with frozen values, and actual construction/runtime support. [CampaignBlueprints](../../src/HobbyDS9.Simulation/CampaignBlueprints.cs) currently compiles/freezes the authored catalogue. [Cycle construction](../../src/HobbyDS9.Simulation/CampaignTacticalReviewSession.ColonisationProduction.cs) additionally checks published-fit support; [PublishedTacticalSupportCompiler](../../src/HobbyDS9.Simulation/PublishedTacticalSupportCompiler.cs) uses exact hull/component mappings and rejects missing mappings. Resolve generated-version provenance and supported output changes explicitly; copying the parent's mapping must not silently lose the new stats or grant unsupported capabilities.

The agreed complete reactor roadmap also includes eligible yard refit. This delivery must list refit as pending if it is not implemented; do not add a fake refit action or label the entire roadmap complete. The minimum usable outcome demonstration is a real accepted version selected in a newly published fit, constructed with actual costs and verified installed/runtime effects, while older publications/ships/queues remain unchanged.

## Deterministic persistence and edition handling

Persist canonical parent, actor, goal/context, policy identity, experiment identity/ordinal, committed work and frozen input maturity; at resolution persist the complete result vector/maturity, detractor details and decision/availability history. Use a versioned isolated random domain, independent of rendering, wall clock, save slot, command UUID, class names and unrelated combat RNG. Save/reload before resolution reproduces the same experiment; after resolution it restores the exact stored result. Pausing/resuming cannot offer a new draw. Keep results and attempt history after Discard.

The baseline has distinct compatibility layers: inner Foundation save/content **14/10**, current [SystemRichness](../../src/HobbyDS9.Contracts/CampaignSystemRichnessContracts.cs) root **15** / package **2**, and earlier Cycle root **14**. Backend must publish the actual new edition/format/policy identities and propagation through copied DTOs, canonical ordering, validation, command journals, replay and root-size limits. [Cycle loading](../../src/HobbyDS9.Simulation/CampaignTacticalReviewSession.ColonisationCycle.cs) checks package identity and exact replay; adding JSON fields alone is insufficient.

Prefer a clearly named new-campaign edition for the first implementation if no explicit migration is supplied. Existing editions remain routed to their previous behavior and retain their save slots; loading an old save does not reinterpret progress as version maturity or regenerate hardware. Unsupported/malformed policy/lineage/history rejects transactionally without replacing the live campaign. A migration, if chosen, needs a separately documented exact input/output and preservation checks. Do not silently reuse a previous format/version for incompatible state.

## Receiving checks and handoff

1. Fixed deterministic cases cover clean and one-detractor results, selected-target gain, meaningful bounded side effects, scientific gates, fixed workload, M=100/125/150 and practical exhaustion. These verify the policy; small samples need not have an exact 70/30 count.
2. Same committed decisions with reordered inputs and save/reload before/after completion preserve the entire authoritative result/history. Duplicate/stale/foreign/invalid-phase requests leave state unchanged. Opening/closing previews consumes no attempt or work.
3. Accept exposes the exact supported version; Continue charges normal new work from that result and persists its parent; Discard preserves spent work/result and cannot reroll. Close leaves a recoverable pending outcome. Independent siblings retain the selected parent's own maturity.
4. Accept → designer → explicit new publication → actual construction → installed/runtime result works. Old fits, queues and ships remain exact. Show real Industry/material requirements and actual profile-dependent credit charges, without introducing new rush/refit rules.
5. Old-edition saves retain behavior; new-edition full replay and cold load preserve pending decisions, retained parents and all three outcome choices. Unknown policy/invalid parent is rejected. GUI/native QA covers truthful stats, focus/Escape, save/reload and one normal turn in the exact candidate.

Handoff must identify exact commit, final APIs and policy/edition IDs, supported attributes/modules, compatibility behavior, executed checks and omitted requests. The [fleet/production reference map](lcars-reference-map-20261118.md) supplies presentation sources for the parallel GUI work. Preserve reviewed CI triggers and batch one coherent push; no main merge is part of this contract.
