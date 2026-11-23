#nullable enable
using System;
using System.IO;

namespace HobbyDS9.UnityReview
{
    // Host I/O only. The controller validates the complete portable document before publication.
    public sealed class GeneratedGalaxyMapStore
    {
        public const string DirectoryName="HobbyDS9GeneratedGalaxyReviewOnly-v1";
        public const int LegacyMaximumBytes=2*1024*1024;
        public const int MaximumBytes=HobbyDS9.Contracts.ScaledGalaxyIdentity.MaximumJsonBytes;
        private readonly string directory;
        private readonly Action<UnityReviewWriteStage>? probe;
        private readonly int maximumBytes;
        public GeneratedGalaxyMapStore(string mapDirectory,Action<UnityReviewWriteStage>? writeProbe=null,int maximumFileBytes=MaximumBytes)
        {
            if(maximumFileBytes<1||maximumFileBytes>MaximumBytes)throw new ArgumentOutOfRangeException(nameof(maximumFileBytes));
            maximumBytes=maximumFileBytes;
            if(!Path.IsPathFullyQualified(mapDirectory))throw new ArgumentException("Absolute map directory required.");
            directory=Path.GetFullPath(mapDirectory).TrimEnd(Path.DirectorySeparatorChar,Path.AltDirectorySeparatorChar);
            if(string.Equals(directory,Path.GetPathRoot(directory)?.TrimEnd(Path.DirectorySeparatorChar,Path.AltDirectorySeparatorChar),StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Map directory cannot be a drive root.");
            probe=writeProbe;CheckDirectory();
        }
        public string Location=>directory;
        private string PathFor(int slot)
        {
            if(slot<1||slot>3)throw new ArgumentOutOfRangeException(nameof(slot));
            return Path.Combine(directory,"generated-map-"+slot+".json");
        }
        private void CheckDirectory()
        {
            if(File.Exists(directory))throw new IOException("Map directory unavailable.");
            if(Directory.Exists(directory)&&(File.GetAttributes(directory)&FileAttributes.ReparsePoint)!=0)
                throw new IOException("Linked map directory unavailable.");
        }
        private static void CheckFile(string path)
        {
            if(Directory.Exists(path))throw new IOException("Map file unavailable.");
            if(File.Exists(path)&&(File.GetAttributes(path)&FileAttributes.ReparsePoint)!=0)
                throw new IOException("Linked map file unavailable.");
        }
        public bool Exists(int slot){CheckDirectory();var path=PathFor(slot);CheckFile(path);return File.Exists(path);}
        public byte[] Read(int slot)
        {
            CheckDirectory();var path=PathFor(slot);CheckFile(path);
            using(var stream=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.Read))
            {
                if(stream.Length<1||stream.Length>maximumBytes)throw new InvalidDataException("Map file size unsupported.");
                var bytes=new byte[(int)stream.Length];int offset=0;
                while(offset<bytes.Length)
                {var read=stream.Read(bytes,offset,bytes.Length-offset);if(read==0)throw new EndOfStreamException();offset+=read;}
                return bytes;
            }
        }
        public void Write(int slot,byte[] bytes,bool overwrite)
        {
            if(bytes==null||bytes.Length==0||bytes.Length>maximumBytes)throw new InvalidDataException("Map file size unsupported.");
            CheckDirectory();var path=PathFor(slot);CheckFile(path);
            if(File.Exists(path)&&!overwrite)throw new IOException("Map slot already contains an export.");
            Directory.CreateDirectory(directory);CheckDirectory();
            var temporary=Path.Combine(directory,".generated-map-"+slot+"-"+Guid.NewGuid().ToString("N")+".tmp");
            bool created=false;
            try
            {
                probe?.Invoke(UnityReviewWriteStage.BeforeWrite);
                using(var stream=new FileStream(temporary,FileMode.CreateNew,FileAccess.Write,FileShare.None))
                {
                    created=true;var half=bytes.Length/2;stream.Write(bytes,0,half);probe?.Invoke(UnityReviewWriteStage.AfterPartialWrite);
                    stream.Write(bytes,half,bytes.Length-half);stream.Flush(true);
                }
                probe?.Invoke(UnityReviewWriteStage.BeforeReplace);CheckDirectory();CheckFile(path);
                if(File.Exists(path))
                {if(!overwrite)throw new IOException("Map slot already contains an export.");File.Replace(temporary,path,null);}
                else File.Move(temporary,path);
            }
            finally {if(created)try{CheckDirectory();if(File.Exists(temporary))File.Delete(temporary);}catch(IOException){}}
        }
    }
}
