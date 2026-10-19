#nullable enable
using System;
using System.IO;
using HobbyDS9.Contracts;
using HobbyDS9.TacticalReviewClient;

namespace HobbyDS9.UnityReview
{
    // Unity/netstandard-compatible trusted adapter. No default v1 slot or net10 storage dependency.
    public sealed class ReviewDemoSlotStore : ISingleplayerDemoCheckpointStore
    {
        private readonly object gate=new object();
        private readonly string directory;
        private readonly ReviewNewtonsoftCodec codec;
        private readonly Action<UnityReviewWriteStage>? probe;
        private const string Prefix="unity-singleplayer-demo-";
        public ReviewDemoSlotStore(string reviewOnlyDirectory,ReviewNewtonsoftCodec codec,Action<UnityReviewWriteStage>? probe=null)
        {
            this.codec=codec; this.probe=probe;
            try
            {
                if(!Path.IsPathFullyQualified(reviewOnlyDirectory)) throw Fail(ReviewCheckpointError.InvalidDirectory);
                directory=Path.GetFullPath(reviewOnlyDirectory).TrimEnd(Path.DirectorySeparatorChar,Path.AltDirectorySeparatorChar);
                if(directory.Length == 0 || string.Equals(directory,Path.GetPathRoot(directory)?.TrimEnd(Path.DirectorySeparatorChar,Path.AltDirectorySeparatorChar),StringComparison.Ordinal))
                    throw Fail(ReviewCheckpointError.InvalidDirectory);
                CheckDirectory();
            }
            catch(Exception) { throw Fail(ReviewCheckpointError.InvalidDirectory); }
        }
        private static ReviewCheckpointStoreException Fail(ReviewCheckpointError error) => new ReviewCheckpointStoreException(error);
        private string PathFor(string slot)
        {
            if(string.IsNullOrEmpty(slot) || slot.Length>32) throw Fail(ReviewCheckpointError.InvalidSlot);
            for(int i=0;i<slot.Length;i++)
            {
                char c=slot[i]; bool plain=c>='a' && c<='z' || c>='0' && c<='9';
                if(!plain && (i==0 || c!='-' && c!='_')) throw Fail(ReviewCheckpointError.InvalidSlot);
            }
            return Confined(Prefix+slot+".json");
        }
        private string Confined(string filename)
        {
            var path=Path.GetFullPath(Path.Combine(directory,filename));
            if(!string.Equals(Path.GetDirectoryName(path),directory,StringComparison.OrdinalIgnoreCase)) throw Fail(ReviewCheckpointError.InvalidSlot);
            return path;
        }
        private void CheckDirectory()
        {
            if(Directory.Exists(directory))
            { if((File.GetAttributes(directory)&FileAttributes.ReparsePoint)!=0) throw Fail(ReviewCheckpointError.InvalidDirectory); }
            else if(File.Exists(directory)) throw Fail(ReviewCheckpointError.InvalidDirectory);
        }
        private static void CheckFile(string path)
        {
            try { if((File.GetAttributes(path)&(FileAttributes.ReparsePoint|FileAttributes.Directory))!=0) throw Fail(ReviewCheckpointError.InvalidSlot); }
            catch(FileNotFoundException) { } catch(DirectoryNotFoundException) { }
        }
        public void Write(string slot,CampaignSingleplayerDemoSaveDto root,bool overwrite)
        {
            lock(gate)
            {
                var destination=PathFor(slot);
                byte[] bytes;
                try { bytes=codec.EncodeDemoRoot(root); } catch(Exception) { throw Fail(ReviewCheckpointError.SerializationFailed); }
                string? temporary=null; bool created=false;
                try
                {
                    CheckDirectory(); CheckFile(destination);
                    if(File.Exists(destination) && !overwrite) throw Fail(ReviewCheckpointError.SlotOccupied);
                    Directory.CreateDirectory(directory); CheckDirectory();
                    temporary=Confined("."+Prefix+slot+"."+Guid.NewGuid().ToString("N")+".tmp"); probe?.Invoke(UnityReviewWriteStage.BeforeWrite);
                    using(var stream=new FileStream(temporary,FileMode.CreateNew,FileAccess.Write,FileShare.None))
                    {
                        created=true; int half=bytes.Length/2; stream.Write(bytes,0,half); probe?.Invoke(UnityReviewWriteStage.AfterPartialWrite);
                        stream.Write(bytes,half,bytes.Length-half); stream.Flush(true);
                    }
                    probe?.Invoke(UnityReviewWriteStage.BeforeReplace); CheckDirectory(); CheckFile(destination);
                    if(File.Exists(destination))
                    { if(!overwrite) throw Fail(ReviewCheckpointError.SlotOccupied); File.Replace(temporary,destination,null); }
                    else File.Move(temporary,destination);
                }
                catch(ReviewCheckpointStoreException) { throw; }
                catch(Exception) { throw Fail(ReviewCheckpointError.WriteFailed); }
                finally
                {
                    if(created && temporary != null) try { CheckDirectory(); if(File.Exists(temporary)) File.Delete(temporary); } catch(Exception) { }
                }
            }
        }
        public CampaignSingleplayerDemoSaveDto Read(string slot)
        {
            lock(gate)
            {
                var path=PathFor(slot); byte[] bytes;
                try { CheckDirectory(); CheckFile(path); bytes=File.ReadAllBytes(path); }
                catch(ReviewCheckpointStoreException) { throw; }
                catch(FileNotFoundException) { throw Fail(ReviewCheckpointError.NotFound); }
                catch(DirectoryNotFoundException) { throw Fail(ReviewCheckpointError.NotFound); }
                catch(Exception) { throw Fail(ReviewCheckpointError.ReadFailed); }
                try { return codec.DecodeDemoRoot(bytes); } catch(Exception) { throw Fail(ReviewCheckpointError.InvalidCheckpoint); }
            }
        }
    }
}
