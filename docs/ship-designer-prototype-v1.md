# LCARS Ship Designer consolidation v1

The staged LCARS prototype on `codex/ship-designer-consolidation-v1` now uses the portable ship-design backend accepted on main `92c5cedd60165d9e2afbc5d35e0beb3cbf455093`. PR #4 remains draft and unmerged. The [ship design chapter](design/ship-design.md) retains its agreed/proposed/open distinctions; this integration does not implement the wider roadmap.

## Backend-driven prototype

`FixtureShipDesignPreviewProvider` retains its presentation-adapter name but contains no authored preview-answer table or validation formulas. It creates `ShipDesignApplication.Create(catalogue).Designer` and calls `Designer.Preview(draft, access)`. Unity references Application/Contracts and never invokes Simulation. Backend issues, including their explanations, remain authoritative. Issue code/path appears secondarily for diagnostics.

`scenarios/ship-design-fixtures/fixture.json` is the single catalogue/access/template source. PrepareUnity copies its exact bytes to `Resources/ship-design-fixture.json`; Newtonsoft.Json loads it using the existing Unity-supported JSON package. Hull/component values are not duplicated in C#. Templates are the fixture's `class.surveyor` and `class.escort`, preserving class/installation IDs, crew, quantities, and all authored Cruise/Combat/Survey requested/allocated values. This is standalone prototype content, not live campaign content or Engineering access.

The local draft deep-copies Contracts DTOs and returns detached copies. Edits include name, role, hull, embarked crew, component add/remove/replace/quantities, and explicit requested/allocated power for the selected profile. All other profiles remain intact. Existing stacks retain their installation IDs; new stacks receive deterministic local IDs. A new or replacement component without authored allocations starts at zero in every profile. Quantity edits retain explicit allocations for backend review. No allocation is auto-generated, scaled or clamped; invalid input is retained and explained by the backend.

The view presents Valid / Warning / Hard Error, used/free capacity, crew/mount/envelope facts, available/requested/allocated power, actual powered outputs, compiled hull/movement, construction/maintenance/resource costs and backend explanations. Warnings about preview-only mission/range ratings remain visible. The Escort Combat template requests 66 power against 60 available/allocated: its energy-bank stack receives 12/16 and launcher 6/8, producing reduced powered weapon outputs. It does not display full-power attacks for this allocation.

## Interaction and scope

Use Ship design in the existing strategic navigation. The class panel, component catalogue, installation groups/schematic, profile tabs, comparison, Reset, discard confirmation and navigation guards remain. Installed-component rows expose compact Requested / Allocated controls for the selected profile. Numeric edits commit on Enter or focus loss, avoiding rebuilding fields on every keystroke. Catalogue facts, installed rows, results and the overall compact layout scroll independently. Reset and Return remain outside the scrolling body.

The banner reads `Backend validated prototype • Construction integration pending`. Construction remains disabled. The designer has no campaign/session mutation path, Publish call, blueprint persistence/registry, construction/refit integration or AI planning. Backend Publish remains independently tested. Save 13, content 9, rules `science-engineering-v1`, combat resolver, campaign RNG, production, AI, movement and Science/Engineering are unchanged. Exploration/time design documents were not edited.

## Executed validation

Local date: 2026-10-12, Unity 6000.6.5f1.

- Locked restore passed; Release .NET build passed with zero warnings/errors.
- Full .NET suite: 1,057 passed, zero failures/skips. Integrated draft tests replace the authored-answer lookup tests and exercise real fixture previews, power reduction, capacity/mount/required-system/dependency/access failures, DTO translation, stable IDs, all profiles, allocation edits without correction, reset/discard and campaign isolation.
- PrepareUnity and static integration checks passed, including byte equality of the ship fixture and no Simulation calls/campaign submission in the designer.
- Canonical/reverse/repeated 25-turn smokes retain save SHA256 `83bdb9013db7ac7b2b33ef77c4b591a4bca065544a01912eb48c48161688f5c4`, turn 26/revision 185 and RNG `d8919406d6e01cf1`.
- Unity EditMode: 70/70 passed, zero failures/skips. A real attached-control test verifies invalid allocated power remains 100, yields the backend issue, leaves other profiles untouched, and Reset restores authored power. Evidence: `artifacts/unity/editmode.xml` and `editmode.log`.
- Windows x64 IL2CPP build succeeded; evidence: `artifacts/unity/windows-build.log` and `artifacts/unity/Windows/HobbyDS9.exe`.
- Real Game view captures completed for both templates at 1280×720, 1920×1080 and 3440×1440. Each PNG's dimensions and primary-action bounds were validated. Evidence: `artifacts/unity/ship-designer-prototype/{surveyor,escort}-{width}x{height}.png` and `ship-designer-capture-final.log`. These generated artifacts remain ignored.

The optional `ShipDesignerCapture.CaptureForAcceptance` command-line entry opens SampleScene, uses the ordinary local-designer navigation entry, and runs the existing real Game view capture sequence. Its startup callback survives Play-mode domain reload. The interactive menu capture remains available and refuses to discard a modified draft. Static checks and .NET tests are not claimed as Unity rendering.

## Native Windows acceptance

The rebuilt IL2CPP player passed native interaction acceptance on 2026-10-12. Both fixture templates, profile switching, component add/remove/replace/quantity edits, comparison, Reset and navigation/discard guards were exercised. Removing Command displayed required-system/dependency failures; adding Labs exceeded capacity; adding energy banks exceeded mounts. An allocated value of 100 remained visible and produced backend power errors without correction. Other profiles retained their values, and Reset restored the fixture. Escort Combat displayed its partial allocation and reduced powered outputs.

Strategic regression checks passed: fleet selection, map pan/zoom/Fit Map, route preview/submission, partial movement over successive turns, human combat resolution/report/finalization, Research preview/apply, and production enqueue/cancel with resource refund. Loading a compatible previous save restored its strategic state and authoritative Research allocations. An older incompatible save remained guarded. Designer edits did not change campaign revision or credits. The user confirmed: “coil whine is gone, both music and sfx work”.

The player was exercised at the normal desktop size and a substantially narrower 1720-pixel window. Controls and results remained reachable through scrolling. Acceptance found a stacked capability/cost overlap; the scoped USS fix prevents metric blocks from shrinking into each other. The rebuilt player passed the targeted scroll check, followed by refreshed captures of both templates at all three required sizes. No gameplay code was changed to address this layout defect.

Exact-commit Linux/Windows/macOS CI is confirmed in the final handoff with its commit SHA and run link. PR #4 remains draft; no merge is performed by this task.

## System-focused UX rebuild v1

Branch `codex/ship-designer-ux-rebuild-v1` starts at exact accepted main `4afe74dd5a6a62454e563fb7aef770ee0f002a23`. This section supersedes the all-in-one class/catalogue/schematic layout described above. The backend contract and wider agreed/proposed/open design status remain unchanged. Blueprint persistence/construction are a separate owner's scope.

The persistent designer shell shows class name, role, hull version, template, validity, capacity, compact construction cost and Back/Reset. A system rail switches one focus workspace among Overview, Hull, Power, Propulsion, Crew/Life support, Command/Computing, Sensors, Science, Shields, Weapons, Mission/Support, Operating profiles and Cost/Validation. System changes preserve the same local draft. Components and editable controls from unrelated systems are absent from the workspace.

Overview prominently displays the first user-supplied LCARS side-view image, unchanged, as `Resources/ShipOverview.png`. It is illustrative, shared by both templates and independent of hull/component placement. The Starship Creator reference screenshots inform progressive navigation only; their art is not imported. There are no generated ship wires/nodes or procedural schematic overlays. Headline facts beside the art use actual preview values: crew, hull/movement, powered shields/weapons, sensors/science, reactor/distribution, available/requested/allocated power and construction/upkeep. Unpowered or unevaluated outputs are labelled without inventing full performance.

`ShipDesignerGraphics` is a replaceable Resources ScriptableObject catalogue mapping system IDs to Texture2D assets. The Overview slot contains the supplied image. All other system graphic slots currently fall back to a clean LCARS-framed title and ART PENDING label. The same rendering path accepts future reactor, drive, crew, command, sensor, science, shield, weapon, support and hull images without interaction-code changes. Art neither intercepts clicks nor encodes rules.

Subsystem lists contain only mapped catalogue categories. Selecting a package opens its detail pane with quantity/actions, version, unit capacity/cost/crew/power, declared dependencies, mounting/envelope data and compatible families. Add, Remove, quantity and Replace call existing local-draft APIs. A detached PreviewCopy uses those same APIs and the existing Application provider to show the consequence of adding one, without altering the real draft or campaign. New/replaced packages retain the accepted behavior of starting unpowered; no automatic allocation is introduced. The comparison foldout stays collapsed initially and limits installation differences to the current system; Overview/Cost can show a complete comparison.

Cruise/Combat/Survey allocation editors exist only on Operating profiles. They retain explicit stack-level requested/allocated values, delayed numeric commits, invalid inputs and other profiles. Actual powered output remains backend-derived. Contextual backend issues appear near their system, with a compact global issue action and complete Cost/Validation list linking to relevant systems. Backend messages are player-facing; code/path diagnostics are no longer appended to every message.

At standard/ultrawide sizes the subsystem rail, generous graphic workspace and bounded detail pane remain separate. Overview uses art beside compact headline facts. At 1280x720 a single system picker replaces the internal rail; scoped outer-shell compaction applies only while the designer is open. Art/facts stack for Overview. Component/details and profile results scroll within useful viewports. Persistent Back/Reset remain outside those scroll regions. Resize changes layout classes, not draft state or duplicated editors.

Templates, Rename, Reset, discard confirmation, navigation guards, backend preview and all profile data remain. The publication control is disabled and explicitly reads `Blueprint publication integration pending`. There are no changes to Contracts, Simulation, Application rules, campaign commands/persistence, fixture content, save/content/rules versions or VSync.

### UX rebuild verification

Locked restore, Release build and all 1,057 .NET tests passed. Static Unity integration checks passed. All three canonical/reverse/repeated campaign smokes retain accepted save SHA256 `83bdb9013db7ac7b2b33ef77c4b591a4bca065544a01912eb48c48161688f5c4`. Unity 6000.6.5f1 EditMode passes 78/78 cases, retaining the accepted 70 and adding eight behavior cases. The attached callback test invokes real registered Clickable handlers; it does not claim synthetic events as native input.

The capture helper records five views at three exact Game view sizes, validating primary-action bounds, useful workspace height, Overview art loading, unavailable publication and separation of profile editors. Evidence lives under ignored `artifacts/unity/ship-designer-ux-rebuild/`:

- `surveyor-overview-{1280x720,1920x1080,3440x1440}.png`
- `escort-overview-{1280x720,1920x1080,3440x1440}.png`
- `weapons-{1280x720,1920x1080,3440x1440}.png`
- `power-{1280x720,1920x1080,3440x1440}.png`
- `profiles-combat-{1280x720,1920x1080,3440x1440}.png`

These are real Unity Game view captures, not browser recreations or image-generated previews. Capture log, EditMode XML/log and Windows build log live in `artifacts/unity`. Exact final commit, CI run and native acceptance results are recorded in the delivery response. Remaining art work is dedicated subsystem images and optionally class-specific Overview illustrations; placeholders are intentional.
