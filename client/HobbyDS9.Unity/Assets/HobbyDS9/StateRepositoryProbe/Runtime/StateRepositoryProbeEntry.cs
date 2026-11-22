#nullable enable
using System;
using System.Collections;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using HobbyDS9.Persistence.Sqlite;
using UnityEngine;

namespace HobbyDS9.StateRepositoryProbe
{
    public sealed class StateRepositoryProbeEntry : MonoBehaviour
    {
        [Serializable] public sealed class Receipt
        {
            public bool Success;public string Error="",UnityVersion="",Platform="",BuildGuid="",BundledNativeSha256="";
            public bool Is64BitProcess;public long ManagedBytesBefore,ManagedBytesAfter;
            public int StartFrame,CompletedFrame,QuitRequestFrame,StartThreadId,QuitThreadId;
            public SqliteNativeProbe.Result? Exercise;
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            var args=Environment.GetCommandLineArgs();
            if(Array.IndexOf(args,"--hobbyds9-state-repository-probe")<0)return;
            Application.runInBackground=true;
            var owner=new GameObject("State repository isolated probe");DontDestroyOnLoad(owner);owner.AddComponent<StateRepositoryProbeEntry>();
        }
        private IEnumerator Start()
        {
            // Startup callbacks can precede a usable player frame. All engine/report/quit
            // work is resumed by Unity's coroutine scheduler, never a Task continuation.
            yield return null;
            var args=Environment.GetCommandLineArgs();
            var receipt=new Receipt{UnityVersion=Application.unityVersion,Platform=Application.platform.ToString(),BuildGuid=Application.buildGUID,Is64BitProcess=Environment.Is64BitProcess,
                StartFrame=Time.frameCount,StartThreadId=System.Threading.Thread.CurrentThread.ManagedThreadId};
            var root=Application.persistentDataPath;string? folder=null;string phase="";Task<SqliteNativeProbe.Result>? work=null;
            try
            {
                var run=Argument(args,"--probe-run");phase=Argument(args,"--probe-phase");
                folder=SqliteNativeProbe.ProbeDirectory(root,run);
                var natives=Directory.GetFiles(Application.dataPath,"e_sqlite3.dll",SearchOption.AllDirectories);
                if(natives.Length!=1)throw new InvalidDataException("Expected exactly one bundled native SQLite.");
                using(var sha=SHA256.Create())using(var stream=File.OpenRead(natives[0]))
                {var text=new StringBuilder(64);foreach(var b in sha.ComputeHash(stream))text.Append(b.ToString("x2"));receipt.BundledNativeSha256=text.ToString();}
                if(receipt.BundledNativeSha256!="4b59d3cded9dc1608544160f3b4b10ac974c6ac0d6ffc23064e5f4c8dbd05aea")throw new InvalidDataException("Bundled native differs from R2.");
                receipt.ManagedBytesBefore=GC.GetTotalMemory(false);
                work=Task.Run(()=>SqliteNativeProbe.Execute(root,run,phase,SqliteProviderBootstrap.EnsureInitialized));
            }
            catch(Exception ex)
            {
                Fail(receipt,ex);
            }
            while(work!=null&&!work.IsCompleted)yield return null;
            if(work!=null)
            {
                try{receipt.Exercise=work.GetAwaiter().GetResult();receipt.ManagedBytesAfter=GC.GetTotalMemory(false);receipt.Success=receipt.Exercise.Success;}
                catch(Exception ex){Fail(receipt,ex);}
            }
            receipt.CompletedFrame=Time.frameCount;
            // No WaitForEndOfFrame: it can stall in a no-graphics player.
            yield return null;yield return null;
            receipt.QuitRequestFrame=Time.frameCount;receipt.QuitThreadId=System.Threading.Thread.CurrentThread.ManagedThreadId;
            if(folder!=null)
            {
                try{Directory.CreateDirectory(folder);File.WriteAllText(Path.Combine(folder,receipt.Success?phase+"-receipt.json":"failure-receipt.json"),JsonUtility.ToJson(receipt,true));}
                catch(Exception ex){Fail(receipt,ex);}
            }
            Debug.Log("State repository native probe "+(receipt.Success?"completed":"failed")+": "+phase+" · "+folder+" · quit frame "+receipt.QuitRequestFrame+" thread "+receipt.QuitThreadId);
            Application.Quit(receipt.Success?0:1);
        }
        private static void Fail(Receipt receipt,Exception ex)
        {receipt.Success=false;receipt.Error=ex.GetType().Name+": "+ex.Message;Debug.LogError(receipt.Error);}
        private static string Argument(string[] args,string key)
        {var index=Array.IndexOf(args,key);if(index<0||index+1>=args.Length)throw new ArgumentException("Missing "+key);return args[index+1];}
    }
}
