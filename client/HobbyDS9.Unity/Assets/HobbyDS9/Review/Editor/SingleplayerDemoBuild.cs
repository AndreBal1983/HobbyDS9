using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
namespace HobbyDS9.UnityReview.Editor
{
    public static class SingleplayerDemoBuild
    {
        [MenuItem("HobbyDS9/Single-player demo/Build Windows IL2CPP")]
        public static void BuildWindowsDemo()
        {
            if(UnityEngine.Application.unityVersion!="6000.6.5f1")throw new InvalidOperationException("Pinned Editor required.");
            var target=NamedBuildTarget.Standalone;var api=PlayerSettings.GetApiCompatibilityLevel(target);var backend=PlayerSettings.GetScriptingBackend(target);
            try
            {
                PlayerSettings.SetApiCompatibilityLevel(target,ApiCompatibilityLevel.NET_Standard);PlayerSettings.SetScriptingBackend(target,ScriptingImplementation.IL2CPP);
                var output=Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath,"../../..","artifacts/unity-singleplayer-demo/Windows/HobbyDS9Demo.exe"));Directory.CreateDirectory(Path.GetDirectoryName(output));
                var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/HobbyDS9/Review/Scenes/IntegratedPlaytest.unity"},locationPathName=output,target=BuildTarget.StandaloneWindows64,
                    options=BuildOptions.StrictMode,extraScriptingDefines=new[]{"HOBBYDS9_SINGLEPLAYER_DEMO"}});
                if(report.summary.result!=BuildResult.Succeeded)throw new InvalidOperationException("Demo build failed.");
                File.WriteAllText(Path.Combine(Path.GetDirectoryName(output),"Start-Demo.cmd"),"@echo off\r\n\"%~dp0HobbyDS9Demo.exe\"\r\n");
                File.WriteAllText(Path.Combine(Path.GetDirectoryName(output),"README.txt"),"Open HobbyDS9Demo.exe or Start-Demo.cmd. This separate build opens the single-player Start/Continue menu without CLI knowledge. It uses its own complete format-4 saves. Ordinary builds and existing saves are unchanged.\r\n");
            }
            finally{PlayerSettings.SetApiCompatibilityLevel(target,api);PlayerSettings.SetScriptingBackend(target,backend);}
        }
    }
}
