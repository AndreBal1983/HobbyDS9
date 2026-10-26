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

        private static void BuildStreamedActivitiesCheckpoint(string stageName)
        {
            BuildWindows(stageName);
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

        private static void BuildWindows(string stageName)
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
                    extraScriptingDefines = new[] { "HOBBYDS9_SINGLEPLAYER_DEMO" }
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
