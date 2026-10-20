using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
namespace HobbyDS9.UnityReview.Editor
{
    public static class GeneratedGalaxyBuild
    {
        [MenuItem("HobbyDS9/Generated galaxy/Build Windows IL2CPP")]
        public static void BuildWindowsGeneratedGalaxy()
        {
            if(UnityEngine.Application.unityVersion!="6000.6.5f1")throw new InvalidOperationException("Pinned Editor required.");
            var target=NamedBuildTarget.Standalone;var api=PlayerSettings.GetApiCompatibilityLevel(target);var backend=PlayerSettings.GetScriptingBackend(target);
            var width=PlayerSettings.defaultScreenWidth;var height=PlayerSettings.defaultScreenHeight;var mode=PlayerSettings.fullScreenMode;var resizable=PlayerSettings.resizableWindow;
            try
            {
                PlayerSettings.SetApiCompatibilityLevel(target,ApiCompatibilityLevel.NET_Standard);PlayerSettings.SetScriptingBackend(target,ScriptingImplementation.IL2CPP);
                PlayerSettings.defaultScreenWidth=1280;PlayerSettings.defaultScreenHeight=720;PlayerSettings.fullScreenMode=FullScreenMode.Windowed;PlayerSettings.resizableWindow=true;
                var output=Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath,"../../..","artifacts/generated-galaxy/Windows/HobbyDS9GalaxyReview.exe"));Directory.CreateDirectory(Path.GetDirectoryName(output));
                var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/HobbyDS9/Review/Scenes/IntegratedPlaytest.unity"},locationPathName=output,target=BuildTarget.StandaloneWindows64,
                    options=BuildOptions.StrictMode,extraScriptingDefines=new[]{"HOBBYDS9_SINGLEPLAYER_DEMO"}});
                if(report.summary.result!=BuildResult.Succeeded)throw new InvalidOperationException("Generated galaxy build failed.");
                File.WriteAllText(Path.Combine(Path.GetDirectoryName(output),"Start-Generated-Galaxy.cmd"),"@echo off\r\n\"%~dp0HobbyDS9GalaxyReview.exe\"\r\n");
                File.WriteAllText(Path.Combine(Path.GetDirectoryName(output),"README.txt"),"Open HobbyDS9GalaxyReview.exe, then choose Generated galaxy review. Choose a seed and 30, 40 or 50 systems and Generate. Inspect stars and empty regions, select a route origin and right-click a destination for a graph preview. Export and reopen use separate generated-map slots. This full-map review does not start a campaign or implement sensors, exploration progression or AI. Existing fleet campaign menu entries remain available. This is a review candidate, not the latest validated release.\r\n");
            }
            finally{PlayerSettings.SetApiCompatibilityLevel(target,api);PlayerSettings.SetScriptingBackend(target,backend);PlayerSettings.defaultScreenWidth=width;PlayerSettings.defaultScreenHeight=height;PlayerSettings.fullScreenMode=mode;PlayerSettings.resizableWindow=resizable;}
        }
    }
}
