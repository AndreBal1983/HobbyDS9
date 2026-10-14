# Unified opt-in campaign shell — early UI and authority seams

14 October 2026. **Current implementation assignment; preparation in progress.** This follow-on branch preserves [PR #36](https://github.com/AndreBal1983/HobbyDS9/pull/36), head `890c6b70ee81296cff34973fdb8fc166af1ec858`, and its current-main ancestor `c5d2b6ac32e8a6c47b073e58f53e9ecb129c11e4`. PR #36 is complete. Historical UX documentation `455bf10` is a different assignment. No main merge or activation is authorized.

## One authority through every screen

Keep one `ReviewUiModel` / `PhysicalReviewHost` / physical Application session for Map, R&D, Production, Fleet orders, central Combat and Aftermath. Panel selection changes presentation only: it does not start/load a campaign, refresh authority, resolve a turn, reset a draft or submit a command. Save/load remains the complete physical root; a validated successful load may replace the host and invalidate prior view callbacks/drafts. Failed load leaves the current host and drafts intact.

Retain the accepted first-iteration central combat layout, current real resources and clearly labelled trusted human actor tooling. Header shows explicit opt-in identity, current profile, owner and authoritative turn/phase. Persistent navigation exposes the route back to Map after aftermath and a discoverable Combat entry while encounters remain pending. Do not create a legacy campaign for strategic panels or use serialized private save data as a public projection.

The existing ResearchPanelView, ProductionOverviewView and GalaxyMap demonstrate useful layout ideas, but they consume legacy CampaignSnapshot / StrategicClient and may choose the first human rather than the current actor. GalaxyProjection uses all campaign fleets. They cannot be bound directly to the physical review host without an actual filtered authority seam. Reuse view-only grouping, scroll panels, allocation drafts and markers; no second session or unfiltered campaign snapshot enters this UI.

## Verified existing seams and missing dependencies

| Area | Actual existing support | Required before dependent binding |
| --- | --- | --- |
| Host/lifecycle | One physical session; actor-scoped own fleets/ships, lawful contacts/routes, stamped travel, rounds, global finalization and complete-root saves | Add public strategy fields only through Primary's concrete detached projections; never read ExportSave into player views |
| Map | Own current fleet locations/progress, permitted last-seen contacts; Application route query | Scoped known nodes/links/owned worlds/visibility, presentation coordinates/labels if supplied; route/privacy facts remain authoritative |
| R&D | Shared `SubmitStrategicCommand(CommandEnvelope)` exists in the physical facade. Existing research kinds are SetResearchAllocation, SetScienceAllocation and SetEngineeringTarget | Current actor's research/progress/output/definitions/requirements/eligibility/receipts, precise mutation results/stamps; use supplied actual contract |
| Production | Existing shared EnqueueProduction/EnqueueBlueprintConstruction are explicitly rejected as UnsupportedSource; newly created unprovisioned ships also reject turn advancement | Primary's supported authored-v2 template choices, paid costs/eligibility, queue/industry/completion projections and exact equipment/source provisioning command/save pin |
| Combat/aftermath | PR #36's actual central view, current stocks/losses, waiting/results, exact-once global aftermath and fresh travel choices | Bind produced ships from the same authoritative membership/pools, not fallback v1 blueprint guesses |
| Saves/versioning | Strict exact-resource composite loader and complete-root codec/store; separate review slots | Primary's exact new payload/package/save identities and compatibility policy before codec/import updates |

These dependencies identify behavior needed from authority, not promised new API names or signatures. Primary owns Contracts/Application/Simulation/scenarios/save versions and `docs/integrated-playtest-contract.md`; wait for its concrete pin before compiling dependent presenters. We own client/controller/host adapters and review navigation/presentation/tests. Coordinate shared default ClientEntry, builder/import scripts, scenarios, shared fixtures and CI edits through the coordinator before changing them. Audio and art-director files remain untouched.

## Navigation, drafts and rejection policy

- Navigation retains model/controller/frame and all unsent edits. Read-only panels remain reachable during waiting/completed combat; mutation controls reflect actual authoritative phase and eligibility. Selecting a panel never advances or finalizes.
- Each Entry rebuild/navigation replacement increments the view epoch and invalidates one-use modal permission. A stale detached button cannot dispatch into another screen or replacement session. Selection/scroll/tab state is presentation-local and actor-scoped.
- Existing travel/tactical drafts remain in their controller. New research/production local intentions must carry the observed actor/session/generation/turn/revision and be reviewed before stamped submission. Changing panels retains them; accepted authority changes mark obsolete inputs explicitly. Do not silently refresh/retry a rejected command or claim an unsent edit is saved.
- Changing actor or loading/resetting a campaign requires the existing deliberate discard policy for all local edits, including future research inputs. Failed candidate load preserves everything. Saving commits only authoritative state and keeps unsent edits locally.
- Research uses the shared exact six-weight/allocation/target rules. Production shows actual eligibility and paid costs/industry progress/completion; no UI production clock, resource grant, fit synthesis or fake progress. Unsupported options remain disabled with a precise player-facing reason.
- Show only permitted current own stocks and visible enemy condition/last-seen contacts. No private enemy routes, research/production, reserves, orders, readiness, hidden-count placeholders or private serialized root appears in controls/reports/tooltips.

## Proof sequence and work underway

Independent first work: portable same-session navigation/draft tests using actual ReviewUiModel/PhysicalReviewHost and existing travel/round/save commands; Entry navigation with epoch invalidation and central layout retained. These are navigation/lifecycle checks, not production acceptance.

After Primary's verified pin: adapt the exact projections and commands, then exercise a real new game -> known map -> actual R&D decisions/progress -> paid supported template enqueue -> legitimate industry turns -> produced ship/fleet -> lawful route/contact -> central battle -> exact-once aftermath -> Map with retained damage/ammunition -> complete-root save/reopen. Include stale/duplicate/foreign/failed commands, unavailable options, no-survivor, multi-encounter, actor privacy and no navigation reset. Compare uninterrupted and resumed paths using the accepted versioned baseline; preserve old vectors where the corresponding paths are unchanged.

Run relevant portable/static CI and actual Unity 6000.6.5f1 graphics-enabled EditMode after final binding. Record actual counts and exact final head. Geometry/source/tree checks are not native pixel/input acceptance. No combined IL2CPP/player build, desktop input, manual build overwrite, main merge, force-push or default activation in this assignment. Primary/PM dispatch final combined native acceptance only after backend and UI are ready.

## Executed independent shell checkpoint

The early shell now has persistent Map, R&D, Production, Fleet orders and Combat navigation around the same model/host. Existing central controls remain functional. Returning to a panel retains the controller draft and central local field selections; each navigation rebuild increments the Entry epoch. The temporary Map shows current own fleet and lawful last-seen contact facts only, without pretending that known topology is connected. R&D and Production clearly explain their unavailable projections/provisioning; no editable or fake-production controls are exposed.

- Complete actual-source portable adapter suite: **83/83 passed**, all 78 inherited cases plus 5 navigation cases; zero failed/skipped. New cases use the actual physical host and commands, not mocked production.
- Actual Unity 6000.6.5f1 graphics-enabled hidden batch EditMode: **128/128 passed**, all 127 inherited cases plus an actual Entry navigation/draft/epoch check; zero failed/skipped. Both preparation targets ran before the Editor. No `-nographics` or desktop input/build was used.
- Pinned Unity API compilation: zero warnings/errors. Binding checker and all **7** adversarial static cases passed.
- Local ignored evidence: `artifacts/integrated-playtest/workspace-all.trx`, `editmode-navigation.xml` / `.log`. These are actual Editor/source-tree checks, not native resolution/readability acceptance.

**Concrete dependency remains:** Primary's exact detached map/research/production contracts, authored-template provisioning command/payload, source package and save-format/compatibility pin. The player end-to-end R&D → paid ship → combat → same-root resume acceptance cannot be executed against the current UnsupportedSource guard. No private-root read, second legacy session, guessed API or v1 fit conversion is substituted for that missing authority. Continue this branch when the pin arrives; this early checkpoint is not a completed integrated playtest.
