#nullable enable
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using HobbyDS9.Persistence.Sqlite;
using UnityEngine;

namespace HobbyDS9.StateRepositoryProbe
{
    public static class StateRepositoryProbeEntry
    {
        [Serializable] public sealed class Receipt
        {
            public bool Success;public string Error="",UnityVersion="",Platform="",BuildGuid="",BundledNativeSha256="";
            public bool Is64BitProcess;public long ManagedBytesBefore,ManagedBytesAfter;
            public SqliteNativeProbe.Result? Exercise;
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static async void Start()
        {
            var args=Environment.GetCommandLineArgs();
            if(Array.IndexOf(args,"--hobbyds9-state-repository-probe")<0)return;
            var receipt=new Receipt{UnityVersion=Application.unityVersion,Platform=Application.platform.ToString(),BuildGuid=Application.buildGUID,Is64BitProcess=Environment.Is64BitProcess};
            var root=Application.persistentDataPath;string? folder=null;
            try
            {
                var run=Argument(args,"--probe-run");var phase=Argument(args,"--probe-phase");
                var natives=Directory.GetFiles(Application.dataPath,"e_sqlite3.dll",SearchOption.AllDirectories);
                if(natives.Length!=1)throw new InvalidDataException("Expected exactly one bundled native SQLite.");
                using(var sha=SHA256.Create())using(var stream=File.OpenRead(natives[0]))
                {var text=new StringBuilder(64);foreach(var b in sha.ComputeHash(stream))text.Append(b.ToString("x2"));receipt.BundledNativeSha256=text.ToString();}
                if(receipt.BundledNativeSha256!="4b59d3cded9dc1608544160f3b4b10ac974c6ac0d6ffc23064e5f4c8dbd05aea")throw new InvalidDataException("Bundled native differs from R2.");
                folder=SqliteNativeProbe.ProbeDirectory(root,run);receipt.ManagedBytesBefore=GC.GetTotalMemory(false);
                receipt.Exercise=await Task.Run(()=>SqliteNativeProbe.Execute(root,run,phase,SqliteProviderBootstrap.EnsureInitialized));
                receipt.ManagedBytesAfter=GC.GetTotalMemory(false);receipt.Success=receipt.Exercise.Success;
                File.WriteAllText(Path.Combine(folder,phase+"-receipt.json"),JsonUtility.ToJson(receipt,true));
                Debug.Log("State repository native probe completed: "+phase+" · "+folder);
            }
            catch(Exception ex)
            {
                receipt.Success=false;receipt.Error=ex.GetType().Name+": "+ex.Message;Debug.LogError(receipt.Error);
                if(folder!=null)
                {try{Directory.CreateDirectory(folder);File.WriteAllText(Path.Combine(folder,"failure-receipt.json"),JsonUtility.ToJson(receipt,true));}catch(Exception writeError){Debug.LogError(writeError.Message);}}
            }
            Application.Quit(receipt.Success?0:1);
        }
        private static string Argument(string[] args,string key)
        {var index=Array.IndexOf(args,key);if(index<0||index+1>=args.Length)throw new ArgumentException("Missing "+key);return args[index+1];}
    }
}
