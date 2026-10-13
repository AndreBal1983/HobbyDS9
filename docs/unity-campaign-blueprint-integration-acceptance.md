# Unity campaign blueprint integration — candidate acceptance

13 October 2026. Candidate branch `codex/unity-designer-campaign-blueprints`, based on accepted main `703e395b793af41953d440d04d69e0744909caf5` (PR #15). Origin was fetched and this base reconfirmed. Delivery is a **draft PR; no merge is authorized**. This implements the scoped [requested milestone](unity-ship-designer-campaign-blueprint-integration.md), preserving [Blueprint Construction v1](blueprint-construction-v1.md) and the accepted LCARS interface. Design roadmap labels remain unchanged.

## Live paths and ownership

`ClientEntry.OpenCampaignDesigner` selects a human actor and creates `CampaignDesigner`. It obtains `StrategicClient.ShipDesignCatalogue(actor)`, which delegates to `ICampaignSession.GetShipDesignCatalogue`. The minimal additive `ShipDesignApplication.FromCampaignCatalogue` helper reuses that immutable, validated Application catalogue. `ApplicationShipDesignPreviewProvider` supplies actual version access, component facts, prices, issues and compiled preview values through the existing Application designer. Unity supplies no authoritative formulas or performance fields. Fixture loading remains solely for offline previews/tests; the normal campaign entry never reads it.

The real DS9 catalogue has no authored class templates. Its live starter is explicitly an **empty campaign fit**, using an actual hull/role and the hull's declared minimum crew. A player supplies equipment and all profile allocations. Missing catalogues/hulls/roles or missing human control give a meaningful unavailable state. Version access comes exclusively from the actual campaign Engineering projection. No new content, technology gate, rule, unlock or economy is invented.

`ShipDesignerView` consumes provider-neutral availability/facts. All thirteen system pages, accepted segment banks, exact inputs and current profile remain available. Overview artwork, USS, `LcarsPowerSlider`, frame geometry and audio assets are unchanged. VSync remains 1 and target frame rate remains -1.

Publication calls `CampaignDesigner.Publish` → `StrategicClient.PublishBlueprint` → the existing stamped `PublishShipBlueprint` command. Its complete DTO is deep copied; malformed/null input remains malformed for backend rejection. The observed turn/state revision, human actor and unique client command ID are retained. Host-generated canonical GUID identities are independent of campaign RNG/entity allocation; backend collision validation remains authoritative. First revision is 1; subsequent revisions target the same explicit blueprint/class identity and latest plus one. Names never determine identity. Identical already-published fits disable publication.

Only accepted commands mark a local draft published. Rejections show the exact backend code, preserve the draft and campaign state, and require explicit **Refresh campaign** before retry. No automatic stamp refresh and replay occurs. Selecting a historical revision starts a detached local copy and cannot mutate stored history. The history shows frozen costs and latest/historical status.

`ProductionOverview` projects only the selected human actor's owned colonies and exact stored revisions through `GetBlueprintConstructionOptions`. `ProductionOverviewView` adds owned-colony selection and blueprint choices to the existing Production panel. Availability, credits/resources/Industry and rejection reasons come from Application. Queue buttons submit `EnqueueBlueprintConstruction` for the exact observed ID/revision. Session-bound models and a per-display submission guard prevent replay of the same displayed action. Accepted submissions refresh balance/queue/options; rejected submissions cannot fabricate a payment or completion. Legacy static shipbuilding remains present.

Existing fleet inspection already consumes `ShipSnapshot.Blueprint` and `ResolvedDesign`; it displays the exact finished revision and frozen hull/shield values. Production, fleet movement and campaign encounter resolution still use the existing authoritative blueprint resolver and phased combat v1.

## Draft lifetime

Ordinary navigation, profile changes and catalogue refresh retain local edits. Successful commands, End Turn and Engineering changes refresh campaign projections. Back/Reset/New class/actor switches use the existing Keep/Discard guard. Drafts are local session workspaces and are **not persisted in campaign saves**.

A successful load increments `StrategicClient.SessionGeneration`. An existing designer and old Production model cannot submit into the replacement campaign even when turn/state stamps happen to match. The old fit remains explicitly review-only; publication is disabled. **Discard old workspace / use loaded campaign** creates the new workspace through the discard guard. Failed loads retain the current session. New application starts create a new workspace; there is no separate new-scenario picker in this milestone.

## Local validation

| Check | Result |
| --- | --- |
| Pre-change locked restore and Release build | Passed; zero warnings/errors |
| Pre-change .NET suite | 1,307 passed; zero failed/skipped |
| Pre-change Unity 6000.6.5f1 EditMode | 107 passed; zero failed/skipped |
| Final locked restore and Release build | Passed; zero warnings/errors |
| Final .NET suite | **1,330 passed; zero failed/skipped** |
| Final PrepareUnity and static integration check | Passed |
| Final Unity EditMode | **116 passed; zero failed/skipped** |
| Windows x64 IL2CPP | Passed; complete runnable package produced |
| Real Play-mode acceptance runner | 24 actual Game-view PNGs; all state/layout assertions passed |
| Native Windows interaction | Publication, historical construction, five production turns, finished inspector, saving and restart/load verified |
| Canonical/reverse/repeated 25-turn campaigns | Identical accepted checksum and outcomes |
| Standalone tactical playground | Scripted playable checks and round 1/2 save/resume checks passed |

The 23 new portable cases use genuine campaign Application sessions and cover live catalogue/access/unlocks; unavailable engineered hulls; rejected fit/power/capacity; malformed/null DTOs; original stamp retention; duplicate revisions, foreign actors/colonies, pending combat, immutable history, isolated identity allocation, queue eligibility/insufficient resources, failed and single-charge submission, completed ship provenance/movement, load generation and legacy production. They add to the existing compiler/construction/combat regression suite. The 9 Unity cases cover normal live entry, registered publication/refresh/queue callbacks, unavailable Add/+1 gates, exact historical queueing, replacement-session controls, all system/profile retention and responsive modes.

Unity logs retain existing audio/API-obsolescence and package warnings; the zero-warning claim above applies to the .NET Release build. GitHub CI performs portable builds/tests/static Unity checks and headless smokes; **it does not run Unity Editor or native Windows acceptance**. Exact final PR head and run links are recorded in the draft PR delivery record. [Branch CI workflow](https://github.com/AndreBal1983/HobbyDS9/actions/workflows/ci.yml?query=branch%3Acodex%2Funity-designer-campaign-blueprints).

## Native walkthrough evidence

The first fresh native campaign was edited manually through the live catalogue and LCARS segments: one command, life-support, propulsion and reactor package; 30 crew; command 4/4 and life-support 6/6 in all profiles; propulsion Cruise 30/30, Combat 12/12, Survey 8/8. Application reported capacity 54/100, credits 22, Industry 38 and 1 Dilithium, with role warnings (unarmed utility fit) and no hard issues. Manual publication of `Campaign Courier` revision 1 succeeded without charging credits. Queueing at owned Earth charged 22 credits once (50 → 28). Publishing renamed revision 2 left the existing queue pinned to revision 1. Screenshots retain that evidence; this unsaved session was subsequently closed during the user's interruption.

The reproducible capture runner starts the ordinary DS9 scenario, builds the same complete input from its empty live draft, exercises invalid power and a genuine `[StaleRevision]` rejection, explicitly refreshes, and publishes `Capture Courier` revisions 1/2. It uses real commands and production turns, never edits resources/technology/compiled stats. A temporary legacy queue is lawfully cancelled for its full unused-cost refund. The generated `native-start.json` was copied into previously empty acceptance slot 2; existing user slots 1/3 were untouched.

The native player then loaded that actual campaign save, blocked its previous workspace, explicitly opened the loaded workspace, edited revision 2 and published **Native Courier III / revision 3**. The same blueprint ID was `blueprint.player.202c3afd8d1e4ddfa3ab0d7019049d28`. At owned Earth the player queued historical revision 1, paid 22 credits/1 Dilithium, and clicked End Turn five times. Progress was 8, 16, 24, 32, then completion at turn 6. The inspector displayed **Capture Courier / revision 1**, hull 30/30, shields 0, in the Federation Scout fleet. Fleet movement remained 3, limited by its legacy Scout; the custom compiled movement is 4. Revision 3 did not rename or upgrade the built ship.

Native saving wrote this campaign to the acceptance-owned slot 2. The saved ship is `ship.spawn.0000000000000013`, blueprint revision 1, empty static design ID, fleet `fleet.federation-scout`, hull 30. The three immutable revisions and built ship survive restart/load. Research/Science, map selection, Production, End Turn and the existing finalized Gamma Exit combat report remain accessible. Invalid old-format user saves are disabled rather than migrated or overwritten. The test-owned slot remains available for review.

## Resolution evidence and limits

Real Game-view captures use `ScreenCapture.CaptureScreenshot`; the helper checks PNG pixel dimensions and primary designer action bounds. Eight states × three resolutions: designer Overview, invalid power, publication rejection/success, immutable history, construction options, queued blueprint and finished ship. Native evidence additionally captures actual desktop input; its 1920×1080 client includes window chrome in the PNG. These are separate types of evidence.

| Resolution | Visual findings |
| --- | --- |
| 1280×720 | Compact navigation uses the system picker/scrolling as required. Back, Reset and Publish remain visible; rejection text readable; long identity/resource details wrap in scrollable panels. Construction and ship provenance require scrolling. |
| 1920×1080 | Full system rail, unchanged Overview art, fixed frame, readable history/costs and production/fleet details. Long blueprint IDs wrap; queue and detail panels scroll. |
| 3440×1440 | Wider stage scales the unchanged artwork; controls remain anchored and history/inspector fit without overlap. Production panel retains its intentional narrow width and wraps long identities. |

No overlapping designer elements or clipped essential designer actions were observed. Long navigation/control captions may ellipsize with full tooltips; this is existing responsive behavior. The Production list may scroll the colony heading above the viewport while reviewing a queue action; scroll up to change colonies. Accepted LCARS styling is preserved. Screenshots are local ignored artifacts, not uploaded or committed as runtime assets.

## Reproduce and deliverables

Run from the repository root:

```powershell
dotnet restore HobbyDS9.slnx --locked-mode
dotnet build HobbyDS9.slnx -c Release --no-restore
dotnet test HobbyDS9.slnx -c Release --no-build --no-restore
dotnet msbuild scripts/PrepareUnity.proj -t:Prepare
python scripts/check-unity-integration.py
dotnet msbuild scripts/PrepareUnity.proj -t:Windows -p:UnityEditor="C:\Program Files\Unity\Hub\Editor\6000.6.5f1\Editor\Unity.exe"
dotnet run --project src/HobbyDS9.Headless -c Release --no-build --no-restore -- --turns 25
dotnet run --project src/HobbyDS9.Headless -c Release --no-build --no-restore -- --turns 25 --combat-order reverse
python scripts/check-tactical-playground.py
```

With fresh desktop permission, run licensed Unity 6000.6.5f1 (without `-batchmode`/`-nographics`), specifying this worktree's Unity `-projectPath`, `-executeMethod HobbyDS9.UnityClient.Editor.CampaignBlueprintCapture.CaptureForAcceptance` and a log path. The optional Editor-only runner requires a render-capable desktop; it starts its own deterministic acceptance session, produces `artifacts/unity/campaign-blueprints/*.png`, `native-start.json` and `finished-campaign.json`, then exits. It does not write user save slots. It intentionally changes its test session only. Do not use the historical offline fixture capture as live campaign evidence.

Self-contained Windows demo: `E:\HobbyDS9\artifacts\demos\Campaign-Blueprint-Integration\HobbyDS9.exe`. Launch directly; retain the complete Data/DLL folders alongside it. No Unity Editor or .NET SDK is required to play. Its README describes the manual fit/production walkthrough. Screenshots are copied under `screenshots/`. Runtime saves use `%USERPROFILE%\AppData\LocalLow\HobbyDS9\HobbyDS9\CampaignSaves`; do not overwrite unrelated saves.

Logs: `E:\HobbyDS9\artifacts\campaign-blueprints\baseline\`, `final-restore.log`, `final-build.log`, `final-tests.log`, `final-unity-validation.log`, `final-capture.log`, `headless-canonical.log`, `headless-reverse.log`, `headless-repeat.log`, `tactical.log`. Unity results/build logs reside in this worktree's `artifacts/unity/`. Native screenshots and copied native save: `E:\HobbyDS9\artifacts\campaign-blueprints\native\`. These output locations remain ignored.

## Compatibility and deferred scope

Simulation, Contracts, scenario bytes, design chapters, golden baselines and accepted graphics/slider/style assets are untouched. The only portable production change is the additive immutable-catalogue Application constructor helper. No shared session interface or campaign implementation changes are required; there is no dependency on Codex 1's unmerged work.

- Save 14, Content 10, `science-engineering-v1`; compiler `ship-design-compiler-v1`; RNG `splitmix64-rejection-v1` v1; campaign phased resolver v1.
- Scenario SHA256: `3b80eb970c5d832550fc947344e860777910d80208af0fc2f474a98ae04e0b59`.
- Canonical/reverse/repeated 25-turn save SHA256: `fb2b75f18c8c5423f3a0850f38f6e53ee8249d3d08385978b5b2532041cc6089`; turn 26, state 185, 131 AI commands, 17 battles, 12 checkpoints, 9 ships/fleets, next entity 97, RNG `d8919406d6e01cf1`.
- Overview SHA256 remains `e67b76ba0a505dbd32b273a00bf4c3c09ced319cb723a063f2b56711f7f3f9d9`.

Deferred: persistent editable draft files; importing arbitrary fits with separate installation stacks of one component or quantities beyond Int32 (history remains reviewable/constructible, unsafe Edit is disabled); richer campaign-authored starter templates; refits/modernization; blueprint deletion/history cleanup; AI-designed classes; Engineering-generated versions; ammunition/subsystem damage; tactical power switching; mission/range/sensor campaign consumers beyond the existing backend; and Tactical Combat v2 **campaign** integration. The accepted standalone tactical playground remains separate. Published revisions and campaign construction are live in this candidate; these deferred items are not claimed complete.
