using HobbyDS9.Contracts;
using HobbyDS9.TacticalReviewClient;

namespace HobbyDS9.TacticalReviewStorage;

public enum ReviewWriteStage { BeforeTemporaryWrite, AfterPartialWrite, AfterFlushBeforeReplace }

// Review-only single-writer host store. No slot discovery or automatic recovery.
public sealed class DiskReviewCheckpointStore : IReviewCheckpointStore
{
    private readonly object gate=new object();
    private readonly string directory;
    private readonly IReviewFileCodec codec;
    private readonly Action<ReviewWriteStage>? writeProbe;
    private static readonly StringComparison PathComparison=OperatingSystem.IsWindows()
        ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
    public DiskReviewCheckpointStore(string reviewDirectory, IReviewFileCodec codec, Action<ReviewWriteStage>? writeProbe = null)
    {
        this.codec=codec ?? throw new ArgumentNullException(nameof(codec));
        this.writeProbe=writeProbe;
        try
        {
            if(!Path.IsPathFullyQualified(reviewDirectory)) throw new ArgumentException();
            directory=Path.TrimEndingDirectorySeparator(Path.GetFullPath(reviewDirectory));
            if(directory == Path.GetPathRoot(directory)) throw new ArgumentException();
            CheckDirectory();
        }
        catch(Exception) { throw Error(ReviewCheckpointError.InvalidDirectory); }
    }
    private static ReviewCheckpointStoreException Error(ReviewCheckpointError error) => new ReviewCheckpointStoreException(error);
    private static void ValidateSlot(string slot)
    {
        if(string.IsNullOrEmpty(slot) || slot.Length > 32) throw Error(ReviewCheckpointError.InvalidSlot);
        for(int i=0;i<slot.Length;i++)
        {
            char c=slot[i]; bool alphanumeric=c >= 'a' && c <= 'z' || c >= '0' && c <= '9';
            if(!alphanumeric && (i == 0 || c != '-' && c != '_')) throw Error(ReviewCheckpointError.InvalidSlot);
        }
    }
    private string ConfinedPath(string filename)
    {
        var path=Path.GetFullPath(Path.Combine(directory,filename));
        if(!path.StartsWith(directory+Path.DirectorySeparatorChar,PathComparison)
            || !string.Equals(Path.GetDirectoryName(path),directory,PathComparison))
            throw Error(ReviewCheckpointError.InvalidSlot);
        return path;
    }
    private string SlotPath(string slot)
    { ValidateSlot(slot); return ConfinedPath("review-checkpoint-"+slot+".json"); }
    private void CheckDirectory()
    {
        if(!Directory.Exists(directory))
        {
            if(File.Exists(directory)) throw Error(ReviewCheckpointError.InvalidDirectory);
            return;
        }
        if((File.GetAttributes(directory)&FileAttributes.ReparsePoint) != 0) throw Error(ReviewCheckpointError.InvalidDirectory);
    }
    private static void CheckSlotFile(string path)
    {
        try
        {
            if((File.GetAttributes(path)&(FileAttributes.ReparsePoint|FileAttributes.Directory)) != 0)
                throw Error(ReviewCheckpointError.InvalidSlot);
        }
        catch(FileNotFoundException) { }
        catch(DirectoryNotFoundException) { }
    }
    public void Write(string slot, CampaignTacticalReviewSaveDto privateSave, bool overwrite)
    {
        lock(gate)
        {
            var destination=SlotPath(slot); // Validation precedes any filesystem mutation.
            byte[] bytes;
            try { bytes=codec.EncodePrivateSave(privateSave); }
            catch(Exception) { throw Error(ReviewCheckpointError.SerializationFailed); }
            string? temporary=null; bool created=false;
            try
            {
                CheckDirectory(); CheckSlotFile(destination);
                if(File.Exists(destination) && !overwrite) throw Error(ReviewCheckpointError.SlotOccupied);
                Directory.CreateDirectory(directory); CheckDirectory();
                temporary=ConfinedPath(".review-checkpoint-"+slot+"."+Guid.NewGuid().ToString("N")+".tmp");
                writeProbe?.Invoke(ReviewWriteStage.BeforeTemporaryWrite);
                using(var stream=new FileStream(temporary,FileMode.CreateNew,FileAccess.Write,FileShare.None))
                {
                    created=true;
                    int half=bytes.Length/2;
                    stream.Write(bytes,0,half); writeProbe?.Invoke(ReviewWriteStage.AfterPartialWrite);
                    stream.Write(bytes,half,bytes.Length-half); stream.Flush(true);
                }
                writeProbe?.Invoke(ReviewWriteStage.AfterFlushBeforeReplace);
                CheckDirectory(); CheckSlotFile(destination);
                // Never delete the last valid checkpoint or degrade to copy/delete.
                if(File.Exists(destination))
                {
                    if(!overwrite) throw Error(ReviewCheckpointError.SlotOccupied);
                    File.Replace(temporary,destination,null);
                }
                else File.Move(temporary,destination);
            }
            catch(ReviewCheckpointStoreException) { throw; }
            catch(Exception) { throw Error(ReviewCheckpointError.WriteFailed); }
            finally
            {
                if(created && temporary != null)
                {
                    try
                    {
                        CheckDirectory();
                        // Only this operation's created path; no wildcard or startup cleanup.
                        if(File.Exists(temporary)) File.Delete(temporary);
                    }
                    // Cleanup failure must not turn a completed atomic commit into a reported save failure.
                    // Never search/delete another path as a fallback.
                    catch(Exception) { }
                }
            }
        }
    }
    public CampaignTacticalReviewSaveDto Read(string slot)
    {
        lock(gate)
        {
            var path=SlotPath(slot);
            byte[] bytes;
            try
            {
                CheckDirectory(); CheckSlotFile(path);
                using(var stream=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.Read))
                using(var buffer=new MemoryStream()) { stream.CopyTo(buffer); bytes=buffer.ToArray(); }
            }
            catch(ReviewCheckpointStoreException) { throw; }
            catch(FileNotFoundException) { throw Error(ReviewCheckpointError.NotFound); }
            catch(DirectoryNotFoundException) { throw Error(ReviewCheckpointError.NotFound); }
            catch(Exception) { throw Error(ReviewCheckpointError.ReadFailed); }
            try { return codec.DecodePrivateSave(bytes); }
            catch(Exception) { throw Error(ReviewCheckpointError.InvalidCheckpoint); }
        }
    }
}
