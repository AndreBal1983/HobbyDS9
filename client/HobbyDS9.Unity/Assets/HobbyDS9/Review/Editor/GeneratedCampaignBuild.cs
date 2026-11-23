using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace HobbyDS9.UnityReview.Editor
{
    /// <summary>Separate playable delivery; the historical map-review builder is unchanged.</summary>
    public static class GeneratedCampaignBuild
    {
        [MenuItem("HobbyDS9/Generated campaign/Build Windows IL2CPP")]
        public static void BuildWindowsGeneratedCampaign()
        {
            if (UnityEngine.Application.unityVersion != "6000.6.5f1")
                throw new InvalidOperationException("Unity 6000.6.5f1 is required.");

            var repository = Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath, "../../.."));
            var stage = Path.GetFullPath(Path.Combine(repository, "artifacts/generated-campaign-v1/Windows"));
            var expectedParent = Path.GetFullPath(Path.Combine(repository, "artifacts/generated-campaign-v1"));
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
            "HobbyDS9 - generated playable campaign review candidate\r\n" +
            "Launch Start-Generated-Campaign.cmd or HobbyDS9GeneratedCampaign.exe. The landing menu opens first.\r\n" +
            "Choose New generated game for the playable campaign, or Continue generated campaign 1/2/3 for an existing generated campaign save. Start with seed 42, 250 systems and quiet area 2.00x. 1000 systems is also available.\r\n" +
            "Generated galaxy review is a separate inspection/draft tool; it does not advance your campaign.\r\n" +
            "Campaigns contain two active empires, paid existing ship classes, research, exploration, current sensor observations, lawful AI movement and interactive combat. Only actor-known space is displayed. The exact generated galaxy and accepted campaign state are saved together in isolated format-7 campaign slots; older profile saves use their existing separate paths.\r\n" +
            "This is a review candidate with limited AI/content and unfinished presentation. Detailed spiral/nebula Art v3 is not integrated. Initial generation and Continue can take tens of seconds; an observed 1000-system editor check took about 44 seconds to generate and 20 seconds to Continue. These are observations, not native performance guarantees.\r\n" +
            "The full generated map represents the entire galaxy. Region geometry is procedural; it is not a physically spiral distribution. Travel-speed balancing is pending. See PLAY-GENERATED-CAMPAIGN.txt.\r\n";

        private const string Walkthrough =
            "PLAYABLE GENERATED CAMPAIGN\r\n" +
            "1. On the landing menu, set seed 42, Systems 250, Quiet area 2.00x, then New generated game. Wait for validation to finish. No campaign is started by opening the separate Generated galaxy review.\r\n" +
            "2. Use Research to review and apply allocations. In Production, queue two existing cruisers at your owned colony; building spends campaign resources and advancing turns completes production. The Worlds/Production screens show accepted progress.\r\n" +
            "3. Open Fleets to inspect and combine compatible stationary own fleets. On Map, select an own fleet, right-click a known destination, review the route and confirm it. End turn advances movement and observations. Fit known frames explored space; Fit galaxy shows the galaxy extent. Wheel zooms and middle-drag pans.\r\n" +
            "4. Explore known neighboring regions to chart more space. Enemy contacts are observations with a seen turn, not permanent location guarantees. A contact can disappear when current observations show the region empty. Following a lost contact does not promise an interception.\r\n" +
            "5. To defend, return a stationary fleet to an owned region, open its fleet roster entry and choose Defend this region, then confirm. This holds position and seeks a lawful encounter; it does not force an immediate battle. AI encounters depend on accepted movement and observations.\r\n" +
            "6. When combat is pending, open Combat, review fleet maneuver/focus and commit one round at a time. Playback shows resolved events, then round reports show actual conditions and statistics. Finish all rounds and confirm the aftermath to resume planning.\r\n" +
            "7. Save to a generated campaign slot. Return to Menu and choose the matching Continue generated campaign slot; allow validation to finish. Ship conditions, ammunition, battle history and the same galaxy are retained. Replace a prior guard with a fresh known-map navigation order, confirm, then End turn to continue the strategy loop.\r\n" +
            "Generated saves are separate from historical review-map exports and old campaign profiles. Do not overwrite a slot you want to preserve. Only use disposable slots for testing.\r\n" +
            "Validation scope: the 250-system Unity engine journey built both cruisers, explored, returned to its own colony, defended, fought a natural AI encounter in three rounds, finalized combat and continued its saved aftermath. A separate 1000-system engine check generated, moved/charted, saved, continued and checked next-turn equivalence. Native delivery checks are recorded separately; these are not claims of a continuous manual journey.\r\n";
    }
}
