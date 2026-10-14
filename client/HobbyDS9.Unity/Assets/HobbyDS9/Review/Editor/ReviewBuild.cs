using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace HobbyDS9.UnityReview.Editor
{
    // Authored entry only. NOT INVOKED by this task, PrepareUnity or the default builder.
    public static class ReviewBuild
    {
        [MenuItem("HobbyDS9/Review/Build opt-in physical review Windows player")]
        public static void BuildWindowsReview()
        {
            if(UnityEngine.Application.unityVersion!="6000.6.5f1")throw new InvalidOperationException("Pinned Editor required.");
            var target=NamedBuildTarget.Standalone;
            var oldApi=PlayerSettings.GetApiCompatibilityLevel(target);
            var oldBackend=PlayerSettings.GetScriptingBackend(target);
            try
            {
                PlayerSettings.SetApiCompatibilityLevel(target,ApiCompatibilityLevel.NET_Standard);
                PlayerSettings.SetScriptingBackend(target,ScriptingImplementation.IL2CPP);
                var output=Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath,"../../..","artifacts/unity-review/Windows/HobbyDS9Review.exe"));
                Directory.CreateDirectory(Path.GetDirectoryName(output));
                var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                    scenes=new[]{"Assets/HobbyDS9/Review/Scenes/PhysicalReview.unity"},locationPathName=output,
                    target=BuildTarget.StandaloneWindows64,options=BuildOptions.StrictMode });
                if(report.summary.result!=BuildResult.Succeeded)throw new InvalidOperationException("Physical review build failed.");
            }
            finally
            {
                PlayerSettings.SetApiCompatibilityLevel(target,oldApi);
                PlayerSettings.SetScriptingBackend(target,oldBackend);
            }
        }
    }
}
