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
