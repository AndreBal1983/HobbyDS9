#nullable enable
using System;
namespace HobbyDS9.UnityClient
{
    public enum LcarsSfx { ButtonPress, Success, Error, Processing, ScreenOpen, Search }
    public sealed class SfxPreferences
    {
        private readonly IMusicSettingsStore store;
        public float Volume { get; private set; }
        public bool Muted { get; private set; }
        public SfxPreferences(IMusicSettingsStore store)
        { this.store = store; Volume = Clamp(store.Read("hobbyds9.audio.v1.sfx", .6f)); Muted = store.Read("hobbyds9.audio.v1.sfx-muted", 0) > .5f; }
        public float Gain(float master) => Muted ? 0 : Clamp(master) * Volume;
        public void SetVolume(float value) { Volume = Clamp(value); Save(); }
        public void SetMuted(bool value) { Muted = value; Save(); }
        private void Save() { store.Write("hobbyds9.audio.v1.sfx", Volume); store.Write("hobbyds9.audio.v1.sfx-muted", Muted ? 1 : 0); store.Flush(); }
        private static float Clamp(float value) => float.IsNaN(value) || float.IsInfinity(value) ? 0 : Math.Max(0, Math.Min(1, value));
    }
    // Only presentation clocks are inputs. No campaign state or random source.
    public sealed class SfxPolicy
    {
        public const int PoolSize = 4;
        private readonly int[] lastFrame = { -1, -1, -1, -1, -1, -1 };
        private readonly double[] ends = new double[PoolSize];
        private readonly bool[] priority = new bool[PoolSize];
        private double lastButton = double.NegativeInfinity;
        private double processingEnd;
        private string section = "Empire";
        public bool Armed { get; private set; }
        public void Arm() => Armed = true;
        public static string SemanticId(LcarsSfx effect)
        {
            switch (effect) {
                case LcarsSfx.ButtonPress: return "buttonPress";
                case LcarsSfx.Success: return "success";
                case LcarsSfx.Error: return "error";
                case LcarsSfx.Processing: return "processing";
                case LcarsSfx.ScreenOpen: return "screenOpen";
                case LcarsSfx.Search: return "search";
                default: throw new ArgumentOutOfRangeException(nameof(effect));
            }
        }
        public bool OpenSection(string next)
        { if (section == next) return false; section = next; return Armed; }
        public int Reserve(LcarsSfx effect, bool hasClip, int frame, double now, double duration)
        {
            var id = (int)effect;
            if (!Armed || !hasClip || id < 0 || id >= lastFrame.Length || effect == LcarsSfx.Processing || lastFrame[id] == frame) return -1;
            if (effect == LcarsSfx.ButtonPress && now - lastButton < .045) return -1;
            bool important = effect == LcarsSfx.Success || effect == LcarsSfx.Error;
            int slot = -1;
            for (int i = 0; i < PoolSize; i++) if (ends[i] <= now) { slot = i; break; }
            if (slot < 0 && important)
                for (int i = 0; i < PoolSize; i++) if (!priority[i] && (slot < 0 || ends[i] < ends[slot])) slot = i;
            if (slot < 0 && important)
                for (int i = 0; i < PoolSize; i++) if (slot < 0 || ends[i] < ends[slot]) slot = i;
            if (slot < 0) return -1;
            lastFrame[id] = frame; if (effect == LcarsSfx.ButtonPress) lastButton = now;
            ends[slot] = now + Math.Max(0, duration); priority[slot] = important; return slot;
        }
        public bool StartProcessing(bool hasClip, double now, double duration)
        { if (!Armed || !hasClip || processingEnd > now) return false; processingEnd = now + Math.Max(0, duration); return true; }
        public void StopProcessing() => processingEnd = 0;
    }
}
