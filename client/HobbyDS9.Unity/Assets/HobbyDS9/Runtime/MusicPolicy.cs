#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace HobbyDS9.UnityClient
{
    // Client-only policy; no campaign state, commands, RNG, clock or file access.
    public enum MusicCategory { Strategic, Menu, Combat, Event }
    public enum MusicLoopPolicy { FullCue, SeamlessLoop }
    public sealed class MusicTrack
    {
        public string Id { get; }
        public string Name { get; }
        public MusicCategory Category { get; }
        public MusicLoopPolicy LoopPolicy { get; }
        public int Order { get; }
        public double CrossfadeSeconds { get; }
        public double QuietTailSeconds { get; }
        public MusicTrack(string id, string name, MusicCategory category, MusicLoopPolicy loopPolicy,
            int order = 0, double crossfadeSeconds = 6, double quietTailSeconds = 0)
        { Id = id; Name = name; Category = category; LoopPolicy = loopPolicy; Order = order; CrossfadeSeconds = crossfadeSeconds; QuietTailSeconds = quietTailSeconds; }
    }
    public static class MusicCatalogPolicy
    {
        public static bool Valid(MusicTrack track) => track != null && !string.IsNullOrWhiteSpace(track.Id)
            && !string.IsNullOrWhiteSpace(track.Name) && Enum.IsDefined(typeof(MusicCategory), track.Category)
            && Enum.IsDefined(typeof(MusicLoopPolicy), track.LoopPolicy)
            && Finite(track.CrossfadeSeconds) && track.CrossfadeSeconds > 0 && track.CrossfadeSeconds <= 30
            && Finite(track.QuietTailSeconds) && track.QuietTailSeconds >= 0 && track.QuietTailSeconds <= 30;
        private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
        public static IReadOnlyList<string> Validate(IEnumerable<MusicTrack> tracks)
        {
            var ids = new HashSet<string>(StringComparer.Ordinal); var errors = new List<string>();
            foreach (var track in tracks)
            {
                if (!Valid(track)) errors.Add("Invalid music metadata.");
                else if (!ids.Add(track.Id)) errors.Add("Duplicate track ID: " + track.Id);
            }
            return errors.AsReadOnly();
        }
    }
    public sealed class MusicPlaylist
    {
        private readonly IReadOnlyList<MusicTrack> tracks;
        private string? lastId;
        public MusicCategory Category { get; private set; } = MusicCategory.Strategic;
        public string? SelectedTrackId => lastId;
        public MusicPlaylist(IEnumerable<MusicTrack> catalog)
        {
            var ids = new HashSet<string>(StringComparer.Ordinal);
            tracks = catalog.Where(MusicCatalogPolicy.Valid).Where(t => ids.Add(t.Id))
                .OrderBy(t => t.Order).ThenBy(t => t.Id, StringComparer.Ordinal).ToList().AsReadOnly();
        }
        // Empty/broken context categories fall back to Strategic. A same effective
        // category is a no-op: overlays/selection/turns never reset playlist position.
        public bool SetContext(MusicCategory requested, Func<MusicTrack, bool> available)
        {
            var effective = tracks.Any(t => t.Category == requested && available(t)) ? requested : MusicCategory.Strategic;
            if (Category == effective) return false;
            Category = effective; return true;
        }
        public MusicTrack? Next(Func<MusicTrack, bool> available)
        {
            var candidates = tracks.Where(t => t.Category == Category && available(t)).ToList();
            if (candidates.Count == 0 && Category != MusicCategory.Strategic)
            { Category = MusicCategory.Strategic; candidates = tracks.Where(t => t.Category == Category && available(t)).ToList(); }
            if (candidates.Count == 0) return null;
            var previous = candidates.FindIndex(t => t.Id == lastId);
            var next = candidates[(previous + 1) % candidates.Count];
            lastId = next.Id; return next;
        }
    }
    public sealed class MusicCueTiming
    {
        public double CrossfadeSeconds { get; }
        public double NextStartDsp { get; }
        public MusicCueTiming(double startDsp, double duration, MusicTrack track)
        {
            if (double.IsNaN(duration) || double.IsInfinity(duration) || duration <= 0) throw new ArgumentOutOfRangeException(nameof(duration));
            CrossfadeSeconds = Math.Min(track.CrossfadeSeconds, duration / 4);
            var tail = Math.Min(track.QuietTailSeconds, duration / 4);
            NextStartDsp = track.LoopPolicy == MusicLoopPolicy.SeamlessLoop ? double.PositiveInfinity
                : startDsp + duration - tail - CrossfadeSeconds;
        }
    }
    public static class MusicEnvelope
    {
        public static double Progress(double now, double start, double seconds) => seconds <= 0 ? 1 : Math.Max(0, Math.Min(1, (now - start) / seconds));
        public static float Incoming(double progress) => (float)Math.Sin(progress * Math.PI / 2);
        public static float Outgoing(double progress) => (float)Math.Cos(progress * Math.PI / 2);
    }
    public interface IMusicSettingsStore
    {
        float Read(string key, float fallback);
        void Write(string key, float value);
        void Flush();
    }
    public sealed class MusicPreferences
    {
        private const string Prefix = "hobbyds9.audio.v1.";
        private readonly IMusicSettingsStore store;
        public float MasterVolume { get; private set; }
        public float MusicVolume { get; private set; }
        public bool Muted { get; private set; }
        public float Gain => Muted ? 0 : MasterVolume * MusicVolume;
        public MusicPreferences(IMusicSettingsStore store)
        {
            this.store = store;
            MasterVolume = Clamp(store.Read(Prefix + "master", 1));
            MusicVolume = Clamp(store.Read(Prefix + "music", .65f));
            Muted = store.Read(Prefix + "muted", 0) >= .5f;
        }
        public void SetMaster(float value) { MasterVolume = Clamp(value); Save(); }
        public void SetMusic(float value) { MusicVolume = Clamp(value); Save(); }
        public void SetMuted(bool value) { Muted = value; Save(); }
        private void Save()
        { store.Write(Prefix + "master", MasterVolume); store.Write(Prefix + "music", MusicVolume); store.Write(Prefix + "muted", Muted ? 1 : 0); store.Flush(); }
        private static float Clamp(float value) => float.IsNaN(value) || float.IsInfinity(value) ? 0 : Math.Max(0, Math.Min(1, value));
    }
}
