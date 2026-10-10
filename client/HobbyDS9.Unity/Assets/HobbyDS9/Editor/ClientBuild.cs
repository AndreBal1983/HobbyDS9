using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace HobbyDS9.UnityClient.Editor
{
    public static class ClientBuild
    {
        [MenuItem("HobbyDS9/Build Windows vertical slice")]
        public static void BuildWindows()
        {
            if (UnityEngine.Application.unityVersion != "6000.6.5f1") throw new InvalidOperationException("Use the pinned Unity 6000.6.5f1 editor.");
            var target = NamedBuildTarget.Standalone;
            PlayerSettings.SetApiCompatibilityLevel(target, ApiCompatibilityLevel.NET_Standard);
            PlayerSettings.SetScriptingBackend(target, ScriptingImplementation.Mono);
            PlayerSettings.companyName = "HobbyDS9"; PlayerSettings.productName = "HobbyDS9";
            PlayerSettings.resizableWindow = true;
            PlayerSettings.defaultScreenWidth = 1920; PlayerSettings.defaultScreenHeight = 1080;
            PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
            var output = Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath, "../../..", "artifacts/unity/Windows/HobbyDS9.exe"));
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { "Assets/Scenes/SampleScene.unity" }, locationPathName = output,
                target = BuildTarget.StandaloneWindows64, options = BuildOptions.StrictMode
            });
            if (report.summary.result != BuildResult.Succeeded) throw new InvalidOperationException("Windows build failed: " + report.summary.result);
            Debug.Log("Windows build completed: " + output);
        }
    }
}
