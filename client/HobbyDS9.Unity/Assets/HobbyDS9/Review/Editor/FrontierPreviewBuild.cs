using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace HobbyDS9.UnityReview.Editor
{
    public static class FrontierPreviewBuild
    {
        [MenuItem("HobbyDS9/Frontier preview/Build Windows IL2CPP menu")]
        public static void BuildWindowsPreview()
        {
            if(UnityEngine.Application.unityVersion!="6000.6.5f1")throw new InvalidOperationException("Pinned Editor required.");
            foreach(var name in new[]{"frontier","package","physical","core","scenario"})
                if(!File.Exists(Path.Combine(UnityEngine.Application.dataPath,"HobbyDS9/Review/Resources/FrontierPackages/v1",name+".json")))
                    throw new InvalidOperationException("Prepare the normal, demo and Frontier resources before building.");
            var directory=Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath,"../../..","artifacts/unity-frontier-preview/Windows"));
            if(Directory.Exists(directory))throw new InvalidOperationException("Use a fresh output; archive any earlier preview before building.");
            var target=NamedBuildTarget.Standalone;var api=PlayerSettings.GetApiCompatibilityLevel(target);var backend=PlayerSettings.GetScriptingBackend(target);
            var width=PlayerSettings.defaultScreenWidth;var height=PlayerSettings.defaultScreenHeight;var mode=PlayerSettings.fullScreenMode;var resizable=PlayerSettings.resizableWindow;
            try
            {
                PlayerSettings.SetApiCompatibilityLevel(target,ApiCompatibilityLevel.NET_Standard);PlayerSettings.SetScriptingBackend(target,ScriptingImplementation.IL2CPP);
                PlayerSettings.defaultScreenWidth=1280;PlayerSettings.defaultScreenHeight=720;PlayerSettings.fullScreenMode=FullScreenMode.Windowed;PlayerSettings.resizableWindow=true;
                Directory.CreateDirectory(directory);
                var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/HobbyDS9/Review/Scenes/IntegratedPlaytest.unity"},locationPathName=Path.Combine(directory,"HobbyDS9FrontierPreview.exe"),
                    target=BuildTarget.StandaloneWindows64,options=BuildOptions.StrictMode,extraScriptingDefines=new[]{"HOBBYDS9_SINGLEPLAYER_DEMO"}});
                if(report.summary.result!=BuildResult.Succeeded)throw new InvalidOperationException("Frontier menu build failed.");
                File.WriteAllText(Path.Combine(directory,"Start-Preview.cmd"),"@echo off\r\n\"%~dp0HobbyDS9FrontierPreview.exe\" %*\r\n");
                File.WriteAllText(Path.Combine(directory,"README.txt"),"Choose Start Frontier exploration preview from the Start/Continue menu. It is provisional arrival charting, not survey/settlement or the Bajor military objective. Frontier format-5 slots use a separate directory; ordinary Start new demo/Continue Demo1-3 retain Bajor format4. Keep all package files together.\r\n");
            }
            finally{PlayerSettings.SetApiCompatibilityLevel(target,api);PlayerSettings.SetScriptingBackend(target,backend);PlayerSettings.defaultScreenWidth=width;PlayerSettings.defaultScreenHeight=height;PlayerSettings.fullScreenMode=mode;PlayerSettings.resizableWindow=resizable;}
        }
    }
}
