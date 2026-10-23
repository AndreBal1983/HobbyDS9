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
                var output=Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath,"../../..","artifacts/scaled-galaxy/Windows/HobbyDS9ScaledGalaxyReview.exe"));Directory.CreateDirectory(Path.GetDirectoryName(output));
                var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/HobbyDS9/Review/Scenes/IntegratedPlaytest.unity"},locationPathName=output,target=BuildTarget.StandaloneWindows64,
                    options=BuildOptions.StrictMode,extraScriptingDefines=new[]{"HOBBYDS9_SINGLEPLAYER_DEMO"}});
                if(report.summary.result!=BuildResult.Succeeded)throw new InvalidOperationException("Generated galaxy build failed.");
                File.WriteAllText(Path.Combine(Path.GetDirectoryName(output),"Start-Scaled-Galaxy.cmd"),"@echo off\r\n\"%~dp0HobbyDS9ScaledGalaxyReview.exe\"\r\n");
                File.WriteAllText(Path.Combine(Path.GetDirectoryName(output),"README.txt"),"Open HobbyDS9ScaledGalaxyReview.exe, then choose Generated galaxy review. Enter seed42 and choose250 systems for the first review, or1000 for the larger5:4map. Quiet-area target is a soft mean-area target; actual sampled quiet/disturbed statistics appear in the inspector. Generation and strict Open may take several seconds. Inspect stars and empty regions, select a route origin and right-click a destination for a shared-boundary route preview. Wheel to zoom, drag to pan, Fit to restore the full extent. Export/Open use isolated generated-map slots; old format1/2 maps remain readable. This review does not start a campaign or implement sensors, exploration progression or AI. This is a review candidate, not the latest validated release.\r\n");
            }
            finally{PlayerSettings.SetApiCompatibilityLevel(target,api);PlayerSettings.SetScriptingBackend(target,backend);PlayerSettings.defaultScreenWidth=width;PlayerSettings.defaultScreenHeight=height;PlayerSettings.fullScreenMode=mode;PlayerSettings.resizableWindow=resizable;}
        }
    }
}
