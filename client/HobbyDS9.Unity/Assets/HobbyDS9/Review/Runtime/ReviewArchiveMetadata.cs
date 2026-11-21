#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace HobbyDS9.UnityReview
{
    // Discovery is advisory. Only the existing typed loader validates save bytes.
    public sealed class ReviewArchiveFile
    {
        public string Slot {get;} public string Path {get;}
        public bool Occupied {get;} public bool Unavailable {get;}
        public long Length {get;} public DateTime ModifiedUtc {get;} public DateTime CreatedUtc {get;}
        internal ReviewArchiveFile(string slot,string path,bool occupied,bool unavailable,long length=0,DateTime modified=default,DateTime created=default)
        {Slot=slot;Path=path;Occupied=occupied;Unavailable=unavailable;Length=length;ModifiedUtc=modified;CreatedUtc=created;}
        public bool SameObservation(ReviewArchiveFile other)=>other!=null&&Path==other.Path&&Slot==other.Slot&&Occupied==other.Occupied
            &&Unavailable==other.Unavailable&&Length==other.Length&&ModifiedUtc==other.ModifiedUtc&&CreatedUtc==other.CreatedUtc;
    }
    public static class ReviewArchiveMetadata
    {
        public static IReadOnlyList<ReviewArchiveFile> Read(IReadOnlyList<KeyValuePair<string,string>> paths,CancellationToken cancellation,Action<string>? beforeFile=null)
        {
            var result=new List<ReviewArchiveFile>(paths.Count);
            foreach(var path in paths)
            {
                cancellation.ThrowIfCancellationRequested();beforeFile?.Invoke(path.Key);cancellation.ThrowIfCancellationRequested();
                try
                {
                    var attributes=File.GetAttributes(path.Value);
                    if((attributes&(FileAttributes.ReparsePoint|FileAttributes.Directory))!=0)
                    {result.Add(new ReviewArchiveFile(path.Key,path.Value,true,true));continue;}
                    var file=new FileInfo(path.Value);
                    result.Add(new ReviewArchiveFile(path.Key,path.Value,true,false,file.Length,file.LastWriteTimeUtc,file.CreationTimeUtc));
                }
                catch(FileNotFoundException){result.Add(new ReviewArchiveFile(path.Key,path.Value,false,false));}
                catch(DirectoryNotFoundException){result.Add(new ReviewArchiveFile(path.Key,path.Value,false,false));}
                catch(Exception){result.Add(new ReviewArchiveFile(path.Key,path.Value,true,true));}
            }
            cancellation.ThrowIfCancellationRequested();return result.AsReadOnly();
        }
    }
}
