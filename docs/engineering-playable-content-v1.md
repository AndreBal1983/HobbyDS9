# Engineering content in the next playable build

ENG-CONTENT-01, 13 November 2026. Integration of the existing [20-project candidate](engineering-development-tree-v1.md), PR85 at `e365ec1ecf3d686b114548f805f68f73f742e89b`, into delivered PB source `788190d3dcc2c371d23a694e21b13e08454034ee`. PR85 is an open draft based on main `e99c151`; its four commits are retained as merge ancestry. This is a receiving candidate, not a main merge or native acceptance.

## Content and runtime path

PR85 authors the full tree in `scenarios/ds9-slice/scenario.json`. The PB Cycle campaign previously loaded `scenarios/playable-fleet-v1/scenario.json`, containing one project, then replaced its designer catalogue with the activities/colonisation ready catalogue. Merely merging PR85 would therefore leave the delivered player's Engineering screen unchanged.

`scenarios/engineering-development-v1/scenario.json` is an explicit content edition of that playable template. It copies PR85's exact Engineering applications, ship-design catalogue and version gates, adding only their definition IDs. Initial state, Science allocation/discovery seeds, reserves, pace, tactical equipment, production and other definitions remain identical to the original playable template. The source checker verifies these statements by comparing parsed inputs; it also verifies prepared resource bytes.

`PrepareUnityGeneratedGalaxy.proj` calls `PrepareUnityPlayableFleet.proj`, which copies this new scenario to `client/HobbyDS9.Unity/Assets/HobbyDS9/Review/Resources/PlayableFleetPackages/v1/engineering-scenario.json`. The existing PB build method requires the resource and rejects anything other than a 20-project scenario. Under `HOBBYDS9_PB_20261113`, Cycle New/Continue loads those exact bytes through `ReviewExactPackageLoader`. Earlier profiles continue using their original template. Normal historical builders retain their existing configuration.

The existing Research/Engineering UI renders every project from the authoritative campaign projection, grouped by kind. No UI catalogue or additional projects are invented. Select Engineering after New Campaign; all 20 cards must be reachable by scrolling, including Reinforced Utility Hull and Expanded Utility Hull. Projects are initially Science locked. Costs obey the current campaign pace: the playable template's Quick pace halves the conceptual workloads in PR85's reference table. One target, reserve spending, prerequisite checks, retained progress, maturity and completion behavior remain governed by the portable simulation.

## Catalogue and publication boundary

When the activities/colonisation campaign installs its ready catalogue, it now retains missing authored Engineering version-tree entries and their gates. Ready versions with an existing ID keep their original ready-package facts. None of the ready classes, original component definitions, support mappings or published blueprint records is rewritten. The validator conflict in ResearchValidation was comment/formatting only; the delivered implementation already supported derived-version refinements.

PR85's new versions retain their exact authored utility-hull compatibility. Some v1 IDs overlap with the ready catalogue, whose components instead list Constitution/Galaxy/Defiant hull families. Retaining the original ready definitions is deliberate. This integration does not broaden component compatibility or manufacture tactical/activities support mappings. Completion releases version access; it does not guarantee that every released version fits or is constructible in Cycle's separate supported catalogue. Ordinary compiler validation and explicit unsupported-source/construction blockers still apply. Advanced Scout remains absent from Cycle's construction catalogue, as already disclosed in its UI. Sensor/Science ratings remain designer previews. Extending those deployment mappings needs a separately scoped content decision; it is not silently included here.

The original DS9 profile retains PR85's utility catalogue and its tested publication behavior. Existing ships, queued orders, frozen catalogue records, blueprint revisions and costs remain immutable. No refit, tactical activation, new production charge or allocation mechanic is added.

## Save identity

No DTO/schema/save-format or rules version changes are introduced. New scenario bytes change the template hash and the derived wrapper/content identities. Old saves are not rebound or migrated. Both directions of cross-content Cycle loading are tested to reject before replacing an existing session.

PB Cycle saves use a separate persistent directory: `ReviewColonisationCycleSlotStore.Namespace + "-engineering20"` (the file format and slot names are unchanged). The menu explains that this edition requires a new campaign and that earlier Cycle saves remain with the earlier build. Original directories and owner saves are preserved. QA must verify a normal Save, quit and cold Continue within this new edition; an old executable remains necessary to continue its original Cycle content.

## Receiving checks

Run locked solution and adapter restores with SDK10.0.401, Release build/tests, then:

```powershell
dotnet msbuild scripts/PrepareUnityGeneratedGalaxy.proj -t:Prepare
python scripts/check-engineering-playable-content.py
python scripts/check-content-resource-bytes.py --prepared
```

`EngineeringPlayableContentTests` uses the actual pinned activities/colonisation/roster/Growth/Cycle composition. It checks all 20 exact IDs in the campaign projection, initial locks and gated outputs, content incompatibility, all 20 completions through public commands, unchanged ready publications and complete replay roundtrip. Its completion fixture supplies initial Science/reserves only in test-owned input; shipped content is unchanged. PR85's portable tests separately cover all projects, retarget/clear, invalid refinements/prerequisites, AI ordering, immutable publication revisions and deterministic long-run reload/reordering.

Build with `GeneratedStaticAstronomyCampaignBuild.BuildWindowsPB20261113` from QA's clean combined source and set `HOBBYDS9_BUILD_SOURCE_SHA` to that exact HEAD. QA owns Unity import, actual EditMode execution, native rendering, input and Windows IL2CPP build. Verify the source identity, a fresh default seed42/250 campaign, 20 reachable Engineering cards and all prerequisite/effect disclosures, locked-target behavior, ordinary target/clear when Science allows, and new-edition Save/Continue. Do not certify card visibility from a source compile or CPU snapshot alone.

Executed results, source SHA, draft PR and TRX locations are recorded in `artifacts/coordination/ENG-CONTENT-01/backend.md`. That receipt distinguishes completed checks from receiving gates.
