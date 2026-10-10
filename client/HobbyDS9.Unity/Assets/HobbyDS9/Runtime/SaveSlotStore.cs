#nullable enable
using System;
using System.IO;
using System.Text;

namespace HobbyDS9.UnityClient
{
    // Host-only file I/O. Fixed slots prevent player text becoming a path.
    public sealed class SaveSlotStore
    {
        private readonly string directory;
        public const int SlotCount = 3;
        public SaveSlotStore(string directory) { this.directory = directory; }
        private string PathFor(int slot)
        {
            if (slot < 1 || slot > SlotCount) throw new ArgumentOutOfRangeException(nameof(slot));
            return Path.Combine(directory, "campaign-" + slot + ".json");
        }
        public bool Exists(int slot) => File.Exists(PathFor(slot));
        public string Read(int slot) => File.ReadAllText(PathFor(slot), Encoding.UTF8);
        public DateTime ModifiedUtc(int slot) => File.GetLastWriteTimeUtc(PathFor(slot));
        public void Write(int slot, string json, bool overwrite)
        {
            var destination = PathFor(slot);
            if (File.Exists(destination) && !overwrite) throw new IOException("Confirm before overwriting an occupied save slot.");
            Directory.CreateDirectory(directory);
            var temporary = destination + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                var bytes = new UTF8Encoding(false).GetBytes(json);
                using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                { stream.Write(bytes, 0, bytes.Length); stream.Flush(true); }
                // Same-directory rename/replace; never delete the last valid save.
                // If the platform cannot replace safely, fail and retain the old file.
                if (File.Exists(destination))
                {
                    if (!overwrite) throw new IOException("Save slot became occupied. Confirm overwrite first.");
                    File.Replace(temporary, destination, null);
                }
                else File.Move(temporary, destination);
            }
            finally { if (File.Exists(temporary)) File.Delete(temporary); }
        }
    }
}
