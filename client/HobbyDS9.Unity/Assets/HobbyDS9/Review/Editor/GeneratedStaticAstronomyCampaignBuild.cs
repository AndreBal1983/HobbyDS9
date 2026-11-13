using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace HobbyDS9.UnityReview.Editor
{
    /// <summary>Dedicated v2 delivery; historical builders and existing stages remain untouched.</summary>
    public static class GeneratedStaticAstronomyCampaignBuild
    {
        [MenuItem("HobbyDS9/Generated campaign v2/Build Windows IL2CPP")]
        public static void BuildWindowsStaticAstronomyCampaign()
            => BuildWindows("Windows");

        [MenuItem("HobbyDS9/Generated campaign v2/Build playable progress checkpoint")]
        public static void BuildWindowsPlayableProgressCheckpoint()
            => BuildWindows("Windows-PlayableProgress-20261024");

        [MenuItem("HobbyDS9/Generated campaign v2/Build navigation and galaxy checkpoint")]
        public static void BuildWindowsNavigationGalaxyCheckpoint()
            => BuildWindows("Windows-NavigationGalaxy-20261024");

        [MenuItem("HobbyDS9/Generated campaign v2/Build classic music player checkpoint")]
        public static void BuildWindowsClassicMusicCheckpoint()
            => BuildWindows("Windows-ClassicMusic-20261024");

        [MenuItem("HobbyDS9/Generated campaign v2/Build responsive campaign checkpoint")]
        public static void BuildWindowsResponsiveCampaignCheckpoint()
            => BuildWindows("Windows-ResponsiveCampaign-20261024");

        [MenuItem("HobbyDS9/Generated campaign v2/Build designer workbench checkpoint")]
        public static void BuildWindowsDesignerWorkbenchCheckpoint()
            => BuildWindows("Windows-DesignerWorkbench-20261024");

        [MenuItem("HobbyDS9/Generated campaign v2/Build designer art integration checkpoint")]
        public static void BuildWindowsDesignerArtCheckpoint()
            => BuildWindows("Windows-DesignerArt-20261024");

        [MenuItem("HobbyDS9/Generated campaign v2/Build published construction checkpoint")]
        public static void BuildWindowsPublishedConstructionCheckpoint()
        {
            const string stageName="Windows-PublishedConstruction-20261025";
            BuildWindows(stageName);
            // Emit playtest-specific guidance only after the unchanged strict builder succeeds.
            var stage=Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath,
                "../../../artifacts/generated-static-astronomy-v2",stageName));
            File.WriteAllText(Path.Combine(stage,"README.txt"),PublishedConstructionReadme);
            File.WriteAllText(Path.Combine(stage,"PLAY-PUBLISHED-CONSTRUCTION.txt"),PublishedConstructionWalkthrough);
        }

        [MenuItem("HobbyDS9/Campaign/Build streamed construction and exploration checkpoint")]
        public static void BuildWindowsStreamedActivitiesCheckpoint()
            =>BuildStreamedActivitiesCheckpoint("Windows-StreamedActivities-20261025");

        [MenuItem("HobbyDS9/Campaign/Build activities checkpoint update")]
        public static void BuildWindowsStreamedActivitiesUpdate()
            =>BuildStreamedActivitiesCheckpoint("Windows-StreamedActivities-Update-20261025");

        [MenuItem("HobbyDS9/Campaign/Build activities nebula edge checkpoint")]
        public static void BuildWindowsStreamedActivitiesNebulaEdges()
            =>BuildStreamedActivitiesCheckpoint("Windows-StreamedActivities-NebulaEdges-20261025");

        [MenuItem("HobbyDS9/Campaign/Build Frontier Commission checkpoint")]
        public static void BuildWindowsStreamedActivitiesFrontierCommission()
            =>BuildStreamedActivitiesCheckpoint("Windows-StreamedActivities-FrontierCommission-20261025");

        [MenuItem("HobbyDS9/Campaign/Build responsive orders checkpoint")]
        public static void BuildWindowsStreamedActivitiesResponsiveOrders()
            =>BuildStreamedActivitiesCheckpoint("Windows-StreamedActivities-ResponsiveOrders-20261025");

        [MenuItem("HobbyDS9/Campaign/Build construction catalogue checkpoint")]
        public static void BuildWindowsStreamedActivitiesConstructionCatalogue()
            =>BuildStreamedActivitiesCheckpoint("Windows-StreamedActivities-ConstructionCatalogue-20261025");

        [MenuItem("HobbyDS9/Campaign/Build stabilisation candidate checkpoint")]
        public static void BuildWindowsStreamedActivitiesStabilisationCandidate()
            =>BuildStreamedActivitiesCheckpoint("Windows-StreamedActivities-StabilisationCandidate-20261025");

        [MenuItem("HobbyDS9/Campaign/Build navigation responsiveness checkpoint")]
        public static void BuildWindowsStreamedActivitiesNavigationIndices()
            =>BuildStreamedActivitiesCheckpoint("Windows-StreamedActivities-NavigationIndices-20261025");

        [MenuItem("HobbyDS9/Campaign/Build colonisation validation candidate")]
        public static void BuildWindowsColonisationCandidate()
        {
            const string stageName="Windows-Colonisation-Candidate-20261026";
            BuildStreamedActivitiesCheckpoint(stageName);
            var stage=Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath,
                "../../../artifacts/generated-static-astronomy-v2",stageName));
            File.WriteAllText(Path.Combine(stage,"README.txt"),ColonisationReadme);
            File.WriteAllText(Path.Combine(stage,"PLAY-COLONISATION.txt"),ColonisationWalkthrough);
        }

        [MenuItem("HobbyDS9/Campaign/Build colonisation V2 diagnostic AOT candidate")]
        public static void BuildWindowsColonisationV2Diagnostic()
        {
            const string stageName="Windows-Colonisation-V2-Diagnostic-20261026";
            BuildStreamedActivitiesCheckpoint(stageName);
            var stage=Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath,
                "../../../artifacts/generated-static-astronomy-v2",stageName));
            File.WriteAllText(Path.Combine(stage,"README.txt"),ColonisationV2DiagnosticReadme);
            File.WriteAllText(Path.Combine(stage,"PLAY-COLONISATION-V2-DIAGNOSTIC.txt"),ColonisationV2DiagnosticGuide);
        }

        private const string ColonisationV2DiagnosticReadme=@"HobbyDS9 - Colonisation V2 diagnostic AOT candidate
UNVALIDATED - NOT FOR HANDOVER OR LATEST PROMOTION

This separate package is prepared only to measure native Save and Continue time.
It is not a completed or accepted colonisation release.

The actual Editor receiving run stopped at its 600-second framework timeout.
It produced 13 of 15 planned images and reached the main colony/mining path and
an official restored-founding evacuation branch. The final recommissioning and
complete later Continue gates are incomplete. The test's own cleanup receipt
was absent; only the independent external restoration check was proven.
Do not describe that failed run as a pass or claim the test drained.

An eventual diagnostic uses the unchanged, official UI-produced checkpoint and
normal typed V2 Continue. That measures this checkpoint path. It does not prove
a manual New Game journey, the remaining gates, native gameplay acceptance or
a performance improvement. Read BUILD-IDENTITY.json and the separate actual
measurement record; a built executable is not validation.

Start with Start-Generated-Campaign.cmd. The V2 menu is:
  Colonisation V2 (provisional playtest)
Use only the matching Continue colonisation V2 slot for a reviewed checkpoint.
The default campaign, earlier V1 colonisation and their slots stay unchanged.
No old save is converted and no validation is bypassed.

If a separate manual New Game check is authorised, use seed 42, 250 systems and
Quiet area 2.00x. A practical industrial choice is a Defiant and two real kits
at the settler source, while the stronger owned shipyard builds two transports.
Kits remain physical local stock: a kit on Mars is not available to Earth.
Industry and delivered materials advance queues; normal construction does not
charge credits. No escort, transport, settlers or supplies are free.

Both empires use their own Constitution Explorer starting class. An escort is
not immunity. Survey, finite carried food, lawful travel, interrupted landings,
four fed establishment weeks, resupply, evacuation and real recommissioning
keep their existing shared rules and provisional content values.

Mature full-history Save/Continue latency remains an open limitation.
AI procurement/travel exists; AI colony-expedition planning is not implemented.
No 1000-system colonisation or full native acceptance is claimed.
Keep the prior accepted demo and all old saves. See the diagnostic guide.
";
        private const string ColonisationV2DiagnosticGuide=@"COLONISATION V2 DIAGNOSTIC GUIDE
UNVALIDATED - NO HANDOVER OR LATEST PROMOTION

Purpose
  Measure the native Save/Continue latency of an immutable, official checkpoint
  produced by the actual V2 UI. Root must separately authorise the exact build,
  fixture, process and measurement. No diagnostic has run merely because this
  guide exists. Full authoritative replay remains mandatory.

Checkpoint measurement
  Use the matching explicit V2 Continue path after the independently reviewed
  checkpoint has been staged in a fresh controlled diagnostic slot. Never edit
  its populations, resources, positions, journal, hash or content identity.
  Record the exact checkpoint and package hashes, actual wall time, visible
  busy feedback and resulting full bytes. A cold Continue result belongs to
  that fixture and package, not to a manual New Game campaign.
  Do not use owner progress as a fixture or overwrite an owner slot.

Current incomplete receiving evidence
  The Editor case timed out after 600.127302 seconds, with 13 of 15 images.
  Main settlement, established-world supply/mining and the restored-founding
  evacuation image were reached. Final recommissioning and later Continue
  gates were not completed. No test cleanup/drain receipt exists.
  External project/owner/package restoration was independently verified.
  This diagnostic does not change that Failed result into a pass.

Optional separate manual New Game check
  1. Open Colonisation V2 (provisional playtest), then New colonisation V2
     campaign. The baseline is seed 42, 250 systems, Quiet area 2.00x.
  2. Inspect the actual owned worlds. Keep the kits at the world that supplies
     the settlers; choose a stronger owned shipyard for the two transports.
     The baseline uses Earth for a real Defiant and two kits, Mars for the two
     transports. Queue work consumes Industry and delivered local materials.
  3. Verify actual completed ships. Join only owned, stationary, colocated
     fleets using normal controls if completion has not already joined them.
     No repair, free unit or fleet-strength rule is added.
  4. Explore and survey discovered uninhabited planets through known routes.
     Stay / seek combat is an ordinary standing order for a future week, not
     an instant battle. Record whether an encounter actually occurred.
  5. Return normally to the source. Load 100 real settlers, one local kit and
     finite food per expedition. Confirm only a current reviewed order.
     Source population, cohort, capacity and food limits remain authoritative.
  6. Travel, request deployment and read the actual outcome. A hostile arrival
     can interrupt landing; settlers may remain aboard. Establishment needs
     work and at least four fed weeks. Supplies can run out and people can die.
  7. Support a real established world with actual delivery and queued work.
     Founding evacuation is return-only; unload at an active established owned
     receiver. An empty outpost needs a fresh equipped expedition and work.
  8. Use the matching V2 Save/Continue controls and wait for full validation.
     A restored branch is a separate history. Record exact successes and
     failures; do not skip a gate or infer full acceptance from partial images.

The content is a named provisional playtest. AI expedition planning, invasion,
full native delivery and 1000-system colonisation performance remain outside
this diagnostic's claim. No timeout, gameplay or validation rule is relaxed.
";

        private const string ColonisationReadme=@"HobbyDS9 - colonisation diagnostic candidate (UNVALIDATED)

Diagnostic candidate only. This is not an accepted release or the latest validated demo.
The actual r7 receiving run stopped on a legitimate hostile-orbit deployment refusal.
Landing feedback and real expedition return/unload passed. Establishment, recommissioning,
the complete loop at all three sizes and IL2CPP Continue remain UNPROVEN.
The existing accepted package remains available. Do not treat this guide as validation.

Run Start-Generated-Campaign.cmd. On the landing menu expand
'Colonisation campaign (provisional playtest)' and choose New colonisation campaign.
Use seed 42, 250 systems and Quiet area 2.00x for the first receiving baseline.
The default campaign button and earlier campaigns keep their original rules.

Build a Galaxy Colony Transport and settlement kit through the ordinary world
queue. Industry and gradually consumed local materials do the work; normal
construction has no credit charge. Interrupted material supply pauses work.
Use an Explorer to discover and survey a suitable uninhabited planet, load
100 real settlers plus one local kit and 8-20 food, travel on a known route,
then request deployment. Establishment needs work and at least four fed weeks.
Read PLAY-COLONISATION.txt before loading people or leaving the source world.

Colonisation uses its own matching Continue buttons and separate colonisation save slots.
Earlier slots remain unchanged and are never converted or used as a fallback.
Save, generation and Continue validate complete history and can take tens of
seconds; wait for the animated busy view. Current mature replay latency is an
open limitation, not a performance claim for this package.

This is named provisional demo tuning, not final balance. Existing catalogue,
power, crew, access, capacity and shared validation remain authoritative.
AI procurement/travel exists; AI colony-expedition planning is not implemented.
The first slice covers uninhabited planets, not invasion or minor-city conquest.
Consult BUILD-IDENTITY.json and the validation record for actual acceptance;
this prepared guide does not itself establish build, native or 1000-system coverage.
";
        private const string ColonisationWalkthrough=@"COLONISATION CAMPAIGN - UNVALIDATED DIAGNOSTIC CANDIDATE

1. Expand Colonisation campaign (provisional playtest) on the landing menu.
   Choose New colonisation campaign, seed 42, 250 systems, Quiet area 2.00x.
   Wait for generation and validation. Old New/Continue choices remain
   separate.

2. Open Production/Worlds and select an established owned source world. Use +
   for Galaxy Colony Transport and Settlement kit. Each click adds one order;
   inspect separately and follow the right-hand queue. Advance weeks for
   actual Industry, local material consumption and delivery. No normal
   construction credits are charged. Cancellation loses work/consumed
   materials; unused local stock remains stock. Never count a queued ship or
   kit as already available.

3. Send your Constitution Explorer along an ordinary known map route. Arrival
   discovers bodies; charted adjacency alone does not reveal their details. In
   Planets, select an observed body and a present Explorer. Choose Review
   planetary survey, then Confirm order. Advance until complete; Cancel
   discards survey work. Inspect the surveyed environment and last observed
   control. This slice settles suitable uninhabited planets; it does not grant
   rights over inhabited minor societies.

4. Return the actual transport/fleet to the source before loading. In
   Transports, choose the present empty transport, source world and resident
   cohort. Choose Review embark 100 settlers + kit. Confirm order moves 100
   real people, one manufactured local kit and 8-20 actual food aboard. At
   least 500 people must remain; no more than a quarter of the selected cohort
   can embark. Review the exact food/material amounts, then Confirm order. A
   preview spends nothing; submission revalidates. Support power remains
   required.

5. Follow a known route to the surveyed destination. Carried food is finite;
   delays and shortages can cause deaths. Choose Review deployment /
   recommission in Transports. All actors are considered together after combat
   in that week. Hostile arrivals can interrupt deployment. A disputed request
   can pause; withdrawing leaves people/supplies aboard. A successful landing
   claims that planet only, never every sibling in its star system.

6. Inspect the new founding camp in Worlds. It needs actual food, work and at
   least four fed weeks before becoming established; this is not a guaranteed
   arrival date. Founding camps accept food/material resupply, not extra
   incoming passengers. An established active world can receive passengers
   through the normal habitat, access and capacity checks. Review current
   progress/deaths.

7. Reuse the now-empty transport. At an established owned world, load supplies
   only with Review load supplies only, without people or another kit. Travel
   back, then Review deliver carried supplies while present. No remote stock
   pool or free resupply is created. On the established frontier, queue the
   Frontier mine through the same construction controls. It uses power and
   extracts actual finite deposits under the existing economy; deposits and
   local stocks change only through accepted work/extraction/delivery.

8. For a founding evacuation, bring an empty powered transport to the camp and
   choose Review founding evacuation. Return-only passengers must travel to an
   established active owned receiving world before unloading. The empty
   outpost has no ordinary output or freight. Recommission it with a newly
   equipped settlement expedition and actual deployment/establishment work;
   ordinary passenger unload cannot bypass that process. Full evacuation of a
   grown city is outside this slice.

9. Save to a colonisation slot and use its matching Continue colonisation
   button. Wait for the busy view through full validation. Keep saves you want
   to retain; earlier campaigns use their own separate slots and original
   rules. Mission still tracks its same four historical accomplishments;
   colony establishment is not an invented fifth goal, reward or empire-wide
   victory.

Limits: provisional content and balance; no AI colony-expedition planner or
invasion; slow complete-history Save/Continue remains open. Existing research,
ship design, exploration/contact, music and lawful combat remain available.
Actual build/native acceptance and tested sizes must be read from this exact
package's validation record. No 1000-system colonisation performance is
claimed.
";

        [MenuItem("HobbyDS9/Campaign/Build colonisation V2 native trace diagnostic")]
        public static void BuildWindowsColonisationV2TraceDiagnostic()
        {
            const string stageName="Windows-Colonisation-V2-Trace-Diagnostic-20261026";
            BuildStreamedActivitiesCheckpoint(stageName,true);
            var stage=Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath,
                "../../../artifacts/generated-static-astronomy-v2",stageName));
            File.WriteAllText(Path.Combine(stage,"README.txt"),"UNVALIDATED NATIVE LOAD TRACE - NO HANDOVER OR LATEST\nThis build explicitly enables bounded fixed-code loading diagnostics.\nThe earlier standalone diagnostic rejected the official colony checkpoint; its cause remains unknown.\nNo validation or gameplay rule is changed.\n\n"+ColonisationV2DiagnosticReadme);
            File.WriteAllText(Path.Combine(stage,"PLAY-COLONISATION-V2-DIAGNOSTIC.txt"),ColonisationV2DiagnosticGuide);
        }

        [MenuItem("HobbyDS9/Campaign/Build colonisation stabilisation candidate")]
        public static void BuildWindowsColonisationStabilisationCandidate()
            =>BuildStreamedActivitiesCheckpoint("Windows-Colonisation-Stabilisation-20261026");

        [MenuItem("HobbyDS9/Campaign/Build colonisation shipyard candidate")]
        public static void BuildWindowsColonisationShipyardCandidate()
            =>BuildStreamedActivitiesCheckpoint("Windows-Colonisation-Shipyard-20261026");

        [MenuItem("HobbyDS9/Campaign/Build colonisation expansion candidate")]
        public static void BuildWindowsColonisationExpansionCandidate()
            =>BuildStreamedActivitiesCheckpoint("Windows-Colonisation-Expansion-20261026");

        [MenuItem("HobbyDS9/Campaign/Build colonisation food-guidance candidate")]
        public static void BuildWindowsColonisationFoodCandidate()
            =>BuildStreamedActivitiesCheckpoint("Windows-Colonisation-Food-20261026");

        [MenuItem("HobbyDS9/Campaign/Build colonisation growth candidate")]
        public static void BuildWindowsColonisationGrowthCandidate()
            =>BuildStreamedActivitiesCheckpoint("Windows-Colonisation-Growth-20261026");

        [MenuItem("HobbyDS9/Campaign/Build colonisation repeat expansion candidate")]
        public static void BuildWindowsColonisationRepeatCandidate()
            =>BuildStreamedActivitiesCheckpoint("Windows-Colonisation-Repeat-20261027");

        [MenuItem("HobbyDS9/Campaign/Build colonisation repeat expansion stabilised candidate")]
        public static void BuildWindowsColonisationRepeatStabilisedCandidate()
            =>BuildStreamedActivitiesCheckpoint("Windows-Colonisation-Repeat-9673-20261028");

        [MenuItem("HobbyDS9/Campaign/Build colonisation continuous expansion candidate")]
        public static void BuildWindowsColonisationCycleCandidate()
            =>BuildStreamedActivitiesCheckpoint("Windows-Colonisation-Cycle-20261029");

        [MenuItem("HobbyDS9/Campaign/Build colonisation visible hull family candidate")]
        public static void BuildWindowsColonisationVisibleFamilyCandidate()
            =>BuildStreamedActivitiesCheckpoint("Windows-Colonisation-Visible-Family-20261030");

        public static void BuildWindowsConstructionCommandCandidate()
            =>BuildWindows("Windows-Construction-Command-20261103");

        public static void BuildWindowsConstructionCommandReadoutCandidate()
            =>BuildWindows("Windows-Construction-Command-Readout-20261103");

        public static void BuildWindowsCampaignSystemCandidate()
            =>BuildWindows("Windows-Campaign-System-20261103");

        private static void BuildStreamedActivitiesCheckpoint(string stageName,bool checkpointDiagnostics=false)
        {
            BuildWindows(stageName,checkpointDiagnostics);
            var stage=Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath,
                "../../../artifacts/generated-static-astronomy-v2",stageName));
            File.WriteAllText(Path.Combine(stage,"README.txt"),StreamedActivitiesReadme);
            File.WriteAllText(Path.Combine(stage,"PLAY-STREAMED-ACTIVITIES.txt"),StreamedActivitiesWalkthrough);
        }

        private const string StreamedActivitiesReadme=@"HobbyDS9 — construction and exploration playtest
Run Start-Generated-Campaign.cmd. New campaign and Continue in the expanded
'Campaign · construction and exploration' section use the streamed activities rules.
Earlier campaigns retain their own Continue buttons, rules and separate save slots.
Nothing converts or upgrades an earlier save.

Normal construction uses Industry and gradually consumed LOCAL materials, without
a credit charge. Physical deliveries use the existing same-owner logistics network.
An order can wait for materials; interrupted supply pauses work without losing progress.
Unused stock stays local. Cancellation loses work and consumed materials, with no refund.
Credits still fund expeditions and society approaches. No buyout feature is included.

The three ready classes have distinct authored hulls and provisional playtest capabilities:
Constitution Explorer, Galaxy Cruiser and Defiant Combat. Art does not determine statistics.
Review exact costs, powered capabilities and current eligibility in Activities.
This is provisional demo content, not final balance. Read PLAY-STREAMED-ACTIVITIES.txt.
";
        private const string StreamedActivitiesWalkthrough=@"1. Start a campaign (seed 42 / 250 systems is the receiving baseline).
2. In Activities, choose an owned world and use a tile's + to add one construction order.
   Inspect tiles separately; the queue is on the right. No construction confirmation is needed.
   Industry works on one mixed FIFO head; materials are consumed
   as work advances. End a week to see actual work, consumption, delivery and supply status.
   Fabrication, research and food structures alter the actual world economy on completion.
3. Use the map to send an owned ship along a known route. Exploration reveals finite
   science opportunities and independent societies; undiscovered sites are not marked.
4. In Expeditions & contact, select a ship at an observed site, review an available
   choice and confirm. Powered study/contact requirements and funding are shown before
   submission. Advance weeks, then explicitly review and claim the finding.
   Society contact can be accepted or refused; cooperation is offered only when permitted.
   Leaving ends an expedition; consumed funding is not refunded. A warning asks first.
5. Save to a campaign slot and use the matching current-campaign Continue button.
   Generating, saving and loading show an indeterminate activity bar while full validation
   runs. Earlier campaign Continue buttons use their original separate save slots.

Construction may legitimately stall under interrupted supply. No freight travel-time,
freighter, treaty, cargo or colonization feature is included.
";

        private static void BuildWindows(string stageName,bool checkpointDiagnostics=false)
        {
            if (UnityEngine.Application.unityVersion != "6000.6.5f1")
                throw new InvalidOperationException("Unity 6000.6.5f1 is required.");

            var repository = Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath, "../../.."));
            var stage = Path.GetFullPath(Path.Combine(repository, "artifacts/generated-static-astronomy-v2", stageName));
            var expectedParent = Path.GetFullPath(Path.Combine(repository, "artifacts/generated-static-astronomy-v2"));
            if (!string.Equals(Path.GetDirectoryName(stage), expectedParent, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Campaign build stage must remain inside this checkout.");
            if (Directory.Exists(stage) || File.Exists(stage))
                throw new InvalidOperationException("Campaign stage already exists; preserve it and use a reviewed fresh stage.");
            for (var ancestor = new DirectoryInfo(stage); ancestor != null; ancestor = ancestor.Parent)
            {
                if (ancestor.Exists && (ancestor.Attributes & FileAttributes.ReparsePoint) != 0)
                    throw new InvalidOperationException("Campaign stage must not traverse a linked directory.");
                if (string.Equals(ancestor.FullName, repository, StringComparison.OrdinalIgnoreCase)) break;
            }

            var target = NamedBuildTarget.Standalone;
            var api = PlayerSettings.GetApiCompatibilityLevel(target);
            var backend = PlayerSettings.GetScriptingBackend(target);
            var width = PlayerSettings.defaultScreenWidth;
            var height = PlayerSettings.defaultScreenHeight;
            var mode = PlayerSettings.fullScreenMode;
            var resizable = PlayerSettings.resizableWindow;
            try
            {
                PlayerSettings.SetApiCompatibilityLevel(target, ApiCompatibilityLevel.NET_Standard);
                PlayerSettings.SetScriptingBackend(target, ScriptingImplementation.IL2CPP);
                PlayerSettings.defaultScreenWidth = 1280;
                PlayerSettings.defaultScreenHeight = 720;
                PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
                PlayerSettings.resizableWindow = true;
                Directory.CreateDirectory(stage);
                var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = new[] { "Assets/HobbyDS9/Review/Scenes/IntegratedPlaytest.unity" },
                    locationPathName = Path.Combine(stage, "HobbyDS9GeneratedCampaign.exe"),
                    target = BuildTarget.StandaloneWindows64,
                    options = BuildOptions.StrictMode,
                    extraScriptingDefines = checkpointDiagnostics
                        ? new[] { "HOBBYDS9_SINGLEPLAYER_DEMO", "HOBBYDS9_CHECKPOINT_DIAGNOSTICS" }
                        : new[] { "HOBBYDS9_SINGLEPLAYER_DEMO" }
                });
                if (report.summary.result != BuildResult.Succeeded)
                    throw new InvalidOperationException("Generated campaign build failed; preserve the failed stage and build report.");

                File.WriteAllText(Path.Combine(stage, "Start-Generated-Campaign.cmd"),
                    "@echo off\r\n\"%~dp0HobbyDS9GeneratedCampaign.exe\"\r\n");
                File.WriteAllText(Path.Combine(stage, "README.txt"), Readme);
                File.WriteAllText(Path.Combine(stage, "PLAY-GENERATED-CAMPAIGN.txt"), Walkthrough);
            }
            finally
            {
                PlayerSettings.SetApiCompatibilityLevel(target, api);
                PlayerSettings.SetScriptingBackend(target, backend);
                PlayerSettings.defaultScreenWidth = width;
                PlayerSettings.defaultScreenHeight = height;
                PlayerSettings.fullScreenMode = mode;
                PlayerSettings.resizableWindow = resizable;
            }
        }

        private const string PublishedConstructionReadme =
            "HobbyDS9 - published construction provisional playtest checkpoint\r\n" +
            "Launch Start-Generated-Campaign.cmd or HobbyDS9GeneratedCampaign.exe. For designer-built ships, expand Published construction - provisional playtest on the landing menu and choose New construction playtest. The ordinary main New generated game still uses its original rules.\r\n" +
            "Start with seed 42, Systems 250 and Quiet area 2.00x. See PLAY-PUBLISHED-CONSTRUCTION.txt for the supported starter, power profiles, publication and paid construction steps.\r\n" +
            "This explicit mode uses separate construction-playtest saves and Continue playtest 1/2/3 controls. Original v2 main Continue and earlier v1 Continue remain separate; no conversion or fallback is performed.\r\n" +
            "Only the named utility/core/beam/targeting-sensor support table is enabled for construction. Other valid classes remain publishable but may be unbuildable. Tactical ratings are provisional playtest tuning, not final balance; catalogue costs, access, power and mount rules remain authoritative. Illustrative ship art does not grant equipment or capabilities.\r\n" +
            "See the accompanying validation record for actual receiving/native coverage. A strict build alone does not establish gameplay acceptance.\r\n";

        private const string PublishedConstructionWalkthrough =
            "PUBLISHED CONSTRUCTION - PROVISIONAL PLAYTEST\r\n" +
            "1. On the landing menu set seed 42, Systems 250 and Quiet area 2.00x. Expand Published construction - provisional playtest and choose New construction playtest. Wait for generation and validation to finish. The ordinary New generated game and Generated galaxy review tools do not enable this construction policy.\r\n" +
            "2. Open Designer. Use the valid Utility starter: utility hull, reactor, command, life support and propulsion, with the starter crew and allocations. For an armed escort, select the available escort targeting sensors in Sensors and the energy bank in Weapons, preview each candidate and Fit package. Cancel leaves the fit unchanged; Undo can reverse a fitted change.\r\n" +
            "3. In Power profiles, inspect EVERY operating profile. Give the installed escort targeting sensors and energy bank their actual full requested AND allocated power shown by the catalogue/preview in each profile. Keep the core systems' required allocations. Resolve every reported preview error before publishing; do not treat the overall power total as proof that each required system is powered.\r\n" +
            "4. Name the class and Publish. In the construction foldout choose that exact published revision and an owned colony. Review the displayed cost and eligibility, then Pay and queue this revision and Confirm. Credits and materials are paid immediately; End turn advances actual colony production. Unsupported fittings show a reason and cannot be paid queued.\r\n" +
            "5. Later edits publish a NEW revision. A paid R1 order remains frozen to R1 when R2 is published; history remains available. After completion, inspect the new own ship/fleet and use ordinary known-map movement and lawful combat controls. No cloak, torpedo stock, shields or unmapped equipment are added for free.\r\n" +
            "6. Save through the campaign controls using a fresh construction-playtest slot. Quit normally, reopen the game and use the matching Continue playtest button in the same landing-menu foldout. Saving, loading and full validation may take time; wait for the busy panel to finish. Never overwrite a save you want to keep.\r\n" +
            "7. The main v2 Continue controls and earlier v1 Continue under Other scenarios & review tools retain their own namespaces and rules. Existing campaigns are not upgraded to published construction. PLAY-GENERATED-CAMPAIGN.txt describes the separate ordinary v2 campaign.\r\n" +
            "Known limits: support is deliberately restricted and tuning provisional; published-but-unsupported designs cannot be built. Displayed art is illustrative, not a tactical fit. Consult the external validation record for the exact build's tested scope; do not infer unobserved native or performance coverage.\r\n";

        private const string Readme =
            "HobbyDS9 - generated playable campaign v2 review candidate\r\n" +
            "Launch Start-Generated-Campaign.cmd or HobbyDS9GeneratedCampaign.exe. Choose New generated game or Continue generated campaign 1/2/3. Start with seed 42 and 250 systems; 1000 systems is also available.\r\n" +
            "The main controls use separate v2 saves containing the exact frozen generated galaxy. Earlier generated v1 saves remain under Other scenarios & review tools, with explicit Continue buttons. Saves are never silently converted, regenerated or loaded through a fallback profile.\r\n" +
            "The playable campaign includes two empires, research, paid existing ship classes, legal movement, exploration, current sensor observations, AI movement and round-based combat. Only actor-known details are displayed.\r\n" +
            "The off-white galaxy is decorative unresolved light. Detailed local nebulae appear only from charted static feature centres; charting is distinct from current ship sensor coverage. Depth and Nebulae buttons control presentation, not movement or rules.\r\n" +
            "The full generated map represents the entire galaxy. Procedural region geometry is unchanged; travel-speed balancing and broader diplomacy, colonisation and AI/content remain unfinished. Initial generation and Continue may take tens of seconds.\r\n" +
            "This is a review candidate. Source/CPU checks and observed Unity/native results have separate status. See the accompanying validation record for actual coverage and remaining limitations.\r\n";

        private const string Walkthrough =
            "PLAYABLE GENERATED CAMPAIGN V2\r\n" +
            "1. Set seed 42, Systems 250 and Quiet area 2.00x, then New generated game. The separate Generated galaxy review tool does not start or advance a campaign.\r\n" +
            "2. Use Research to review/apply allocations. In Production, queue existing cruisers at your owned colony. Production spends real campaign resources; End turn progresses construction.\r\n" +
            "3. Inspect/combine compatible stationary own fleets in Fleets. Select an own fleet on Map, then right-click anywhere inside a known tile to set its route. Shift-right-click appends a waypoint. Only replacing an engage/follow activity asks for confirmation. Accepted routes stay visible; End turn advances movement and observations. Wheel zooms; middle-drag pans. Fit known frames charted space; Fit galaxy shows the whole extent.\r\n" +
            "4. Explore neighboring regions to chart space. A contact records an observation, not a permanent enemy location. Current observation of an empty region clears its contact; following a lost contact does not guarantee interception.\r\n" +
            "5. A stationary own fleet can Defend this region through its roster entry. Confirm the order and advance turns. This seeks a lawful encounter without forcing a battle or exposing hidden enemy movement.\r\n" +
            "6. Open Combat when a battle is pending. Review maneuver/focus and commit each round. Playback animates resolved events; reports show accepted conditions. Confirm the completed aftermath to resume strategy.\r\n" +
            "7. Save to a v2 campaign slot, quit normally and use the matching main Continue button. Frozen galaxy, accepted commands and physical ship conditions are preserved. Use fresh known-map orders to continue after a guard.\r\n" +
            "Earlier v1 campaigns are available through Other scenarios & review tools. Their explicit Continue controls use the original separate slots. Do not overwrite any save you want to keep.\r\n" +
            "Presentation remains schematic: map art does not determine travel costs, sensor range or tactical outcomes. Native gameplay, graphics receiving, CPU tests and separately labelled fixture journeys are recorded independently; no continuous manual campaign or GPU/FPS claim is implied.\r\n";
    }
}
