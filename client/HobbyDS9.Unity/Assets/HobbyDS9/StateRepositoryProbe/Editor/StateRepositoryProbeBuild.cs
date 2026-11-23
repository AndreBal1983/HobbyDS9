#nullable enable
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HobbyDS9.StateRepositoryProbe.Editor
{
    public static class StateRepositoryProbeBuild
    {
        [Serializable] private sealed class FilePin {public string Path="",Sha256="";}
        [Serializable] private sealed class Recipe {public string Source="",DataSource="",NativeSha256="";public FilePin[] Files=Array.Empty<FilePin>();}
        [Serializable] private sealed class Receipt
        {public string Source="",DataSource="",UnityVersion="",Backend="IL2CPP",Api="NET_Standard",Stripping="High",NativeSha256="",BuildGuid="";public bool NativePackagingVerified;}
        public static void BuildWindows()
        {
            var project=Path.GetFullPath(Path.Combine(Application.dataPath,".."));
            if(!File.Exists(Path.Combine(project,"state-repository-probe-project.json")))throw new InvalidOperationException("Use the generated isolated probe project, never the game project.");
            if(Application.unityVersion!="6000.6.9f1")throw new InvalidOperationException("Probe recipe requires actual6000.6.9f1.");
            var recipe=JsonUtility.FromJson<Recipe>(File.ReadAllText(Path.Combine(project,"state-repository-probe-project.json")));
            foreach(var pin in recipe.Files)
            {
                var path=Path.GetFullPath(Path.Combine(project,pin.Path));
                if(!path.StartsWith(project+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase)||Hash(path)!=pin.Sha256)
                    throw new InvalidDataException("Staged source/dependency pin differs: "+pin.Path);
            }
            var output=Path.Combine(project,"artifacts","Windows-A2-"+recipe.Source.Substring(0,12));
            if(Directory.Exists(output))throw new IOException("Probe output already exists; choose a new generated project, preserve earlier evidence.");
            var target=NamedBuildTarget.Standalone;var api=PlayerSettings.GetApiCompatibilityLevel(target);
            var backend=PlayerSettings.GetScriptingBackend(target);var stripping=PlayerSettings.GetManagedStrippingLevel(target);
            var product=PlayerSettings.productName;var company=PlayerSettings.companyName;var width=PlayerSettings.defaultScreenWidth;var height=PlayerSettings.defaultScreenHeight;var background=PlayerSettings.runInBackground;
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            Directory.CreateDirectory(Path.Combine(Application.dataPath,"ProbeGenerated"));
            const string scenePath="Assets/ProbeGenerated/StateRepositoryProbe.unity";
            EditorSceneManager.SaveScene(scene,scenePath);
            try
            {
                PlayerSettings.companyName="HobbyDS9Probe";PlayerSettings.productName="StateRepositoryProbe-v1";
                PlayerSettings.SetApiCompatibilityLevel(target,ApiCompatibilityLevel.NET_Standard);
                PlayerSettings.SetScriptingBackend(target,ScriptingImplementation.IL2CPP);
                PlayerSettings.SetManagedStrippingLevel(target,ManagedStrippingLevel.High);
                PlayerSettings.defaultScreenWidth=1920;PlayerSettings.defaultScreenHeight=1080;
                PlayerSettings.runInBackground=true;
                var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{scenePath},locationPathName=Path.Combine(output,"StateRepositoryProbe.exe"),
                    target=BuildTarget.StandaloneWindows64,options=BuildOptions.StrictMode,extraScriptingDefines=new[]{"HOBBYDS9_STATE_REPOSITORY_PROBE"}});
                if(report.summary.result!=BuildResult.Succeeded)throw new InvalidOperationException("Native probe build failed; keep its output and logs.");
                var dlls=Directory.GetFiles(output,"e_sqlite3.dll",SearchOption.AllDirectories);
                if(dlls.Length!=1||Hash(dlls[0])!=recipe.NativeSha256)throw new InvalidDataException("Bundled native SQLite missing, duplicated or different.");
                var receipt=new Receipt{Source=recipe.Source,DataSource=recipe.DataSource,UnityVersion=Application.unityVersion,NativeSha256=recipe.NativeSha256,
                    NativePackagingVerified=true,BuildGuid=report.summary.guid.ToString()};
                File.WriteAllText(Path.Combine(output,"probe-build-receipt.json"),JsonUtility.ToJson(receipt,true));
                File.Copy(Path.Combine(project,"state-repository-probe-project.json"),Path.Combine(output,"staged-recipe.json"));
                var notices=Path.Combine(project,"ThirdPartyNotices");
                foreach(var path in Directory.GetFiles(notices))
                {var to=Path.Combine(output,"ThirdPartyNotices",Path.GetFileName(path));Directory.CreateDirectory(Path.GetDirectoryName(to)!);File.Copy(path,to);}
                Debug.Log("A2 packaging build complete; native execution acceptance still pending: "+output);
            }
            finally
            {
                PlayerSettings.SetApiCompatibilityLevel(target,api);PlayerSettings.SetScriptingBackend(target,backend);PlayerSettings.SetManagedStrippingLevel(target,stripping);
                PlayerSettings.productName=product;PlayerSettings.companyName=company;PlayerSettings.defaultScreenWidth=width;PlayerSettings.defaultScreenHeight=height;
                PlayerSettings.runInBackground=background;
            }
        }
        private static string Hash(string path)
        {using(var sha=SHA256.Create())using(var stream=File.OpenRead(path)){var bytes=sha.ComputeHash(stream);var text=new StringBuilder(64);foreach(var b in bytes)text.Append(b.ToString("x2"));return text.ToString();}}
    }
}
