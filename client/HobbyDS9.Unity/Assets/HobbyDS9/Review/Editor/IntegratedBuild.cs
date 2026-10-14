using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
namespace HobbyDS9.UnityReview.Editor
{
    // Authored entry for Primary's later dispatched combined build. Not executed here.
    public static class IntegratedBuild
    {
        [MenuItem("HobbyDS9/Integrated playtest/Build Windows IL2CPP")]
        public static void BuildWindowsIntegrated()
        {
            if(UnityEngine.Application.unityVersion!="6000.6.5f1")throw new InvalidOperationException("Pinned Editor required.");
            var target=NamedBuildTarget.Standalone;var api=PlayerSettings.GetApiCompatibilityLevel(target);var backend=PlayerSettings.GetScriptingBackend(target);
            try
            {
                PlayerSettings.SetApiCompatibilityLevel(target,ApiCompatibilityLevel.NET_Standard);PlayerSettings.SetScriptingBackend(target,ScriptingImplementation.IL2CPP);
                var output=Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath,"../../..","artifacts/unity-integrated/Windows/HobbyDS9IntegratedPlaytest.exe"));
                Directory.CreateDirectory(Path.GetDirectoryName(output));
                var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/HobbyDS9/Review/Scenes/IntegratedPlaytest.unity"},locationPathName=output,target=BuildTarget.StandaloneWindows64,options=BuildOptions.StrictMode});
                if(report.summary.result!=BuildResult.Succeeded)throw new InvalidOperationException("Integrated build failed.");
                File.WriteAllText(Path.Combine(Path.GetDirectoryName(output),"Launch-IntegratedPlaytest.cmd"),"@echo off\r\n\"%~dp0HobbyDS9IntegratedPlaytest.exe\" --hobbyds9-integrated-playtest --hobbyds9-integrated-actor civ.federation\r\n");
                File.WriteAllText(Path.Combine(Path.GetDirectoryName(output),"README.txt"),"Use Launch-IntegratedPlaytest.cmd for the explicit one-campaign playtest. No-flag startup remains default v1. Integrated slots are separate format3. This authored builder requires prior PrepareUnityIntegrated and full EditMode acceptance.\r\n");
            }
            finally{PlayerSettings.SetApiCompatibilityLevel(target,api);PlayerSettings.SetScriptingBackend(target,backend);}
        }
    }
}
