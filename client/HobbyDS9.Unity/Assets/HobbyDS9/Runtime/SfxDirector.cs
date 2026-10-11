#nullable enable
using System;
using UnityEngine;
using UnityEngine.UIElements;
namespace HobbyDS9.UnityClient
{
    // Separate persistent object preserves the existing two-source music director.
    public sealed class SfxDirector : MonoBehaviour
    {
        private static SfxDirector? instance;
        private AudioSource[] sources = Array.Empty<AudioSource>();
        private AudioSource processing = null!;
        private LcarsSfxCatalog? catalog;
        private float master = 1;
        // Diagnostic notification of armed semantic requests, not proof of audible playback.
        public event Action<LcarsSfx>? Requested;
        public SfxPolicy Policy { get; } = new SfxPolicy();
        public SfxPreferences Preferences { get; private set; } = null!;
        public static SfxDirector? TryGetOrCreate()
        {
            try {
                if (instance != null) return instance;
                var existing = FindFirstObjectByType<SfxDirector>();
                if (existing != null) { instance = existing; existing.Initialize(); return existing; }
                return new GameObject("HobbyDS9 SFX").AddComponent<SfxDirector>();
            } catch (Exception) { return null; }
        }
        private void Awake()
        {
            if (instance != null && instance != this) { Destroy(gameObject); return; }
            instance = this; Initialize();
            if (UnityEngine.Application.isPlaying) DontDestroyOnLoad(gameObject);
        }
        private void Initialize()
        {
            if (sources.Length != 0) return;
            var store = new PlayerPrefsMusicStore(); Preferences = new SfxPreferences(store);
            master = new MusicPreferences(store).MasterVolume;
            catalog = UnityEngine.Resources.Load<LcarsSfxCatalog>("LcarsSfxCatalog");
            var existing = GetComponents<AudioSource>();
            sources = new AudioSource[SfxPolicy.PoolSize];
            for (int i = 0; i < sources.Length; i++) sources[i] = CreateSource(i < existing.Length ? existing[i] : null);
            processing = CreateSource(existing.Length > sources.Length ? existing[sources.Length] : null); ApplyGain();
        }
        private AudioSource CreateSource(AudioSource? reusable)
        { var source = reusable != null ? reusable : gameObject.AddComponent<AudioSource>(); source.playOnAwake = false; source.loop = false; source.spatialBlend = 0; return source; }
        public static void Cue(LcarsSfx effect) { try { TryGetOrCreate()?.Play(effect); } catch (Exception) { } }
        public static void SharedMaster(float value) { try { if (instance != null) { instance.master = value; instance.ApplyGain(); } } catch (Exception) { } }
        public void Arm() => Policy.Arm();
        public void SetVolume(float value) { Preferences.SetVolume(value); ApplyGain(); }
        public void SetMuted(bool value) { Preferences.SetMuted(value); ApplyGain(); }
        private void ApplyGain()
        { var gain = Preferences.Gain(master); foreach (var source in sources) source.volume = gain; if (processing != null) processing.volume = gain; }
        public void Play(LcarsSfx effect)
        {
            try {
                if (!Policy.Armed) return;
                Requested?.Invoke(effect);
                var clip = catalog?.Find(effect); if (clip == null || Preferences.Gain(master) <= 0) return;
                var slot = Policy.Reserve(effect, true, Time.frameCount, AudioSettings.dspTime, clip.length);
                if (slot < 0) return;
                var source = sources[slot]; source.Stop(); source.clip = clip; source.Play();
            } catch (Exception) { /* Audio must never interrupt a command. */ }
        }
        public void OpenSection(string section)
        { if (Policy.OpenSection(section)) Play(LcarsSfx.ScreenOpen); }
        public void StartProcessing()
        {
            try { var clip = catalog?.Find(LcarsSfx.Processing);
                if (clip == null || Preferences.Gain(master) <= 0 || !Policy.StartProcessing(true, AudioSettings.dspTime, clip.length)) return;
                processing.clip = clip; processing.Play();
            } catch (Exception) { }
        }
        public void StopProcessing() { try { Policy.StopProcessing(); if (processing != null) processing.Stop(); } catch (Exception) { } }
        private void OnDisable() { StopProcessing(); foreach (var source in sources) if (source != null) source.Stop(); }
        private void OnDestroy() { if (instance == this) instance = null; }
    }
    public sealed class SfxInputBinding : IDisposable
    {
        private readonly VisualElement root;
        private readonly SfxDirector? director;
        public SfxInputBinding(VisualElement root, SfxDirector? director)
        { this.root = root; this.director = director; root.RegisterCallback<PointerDownEvent>(Pointer, TrickleDown.TrickleDown); root.RegisterCallback<KeyDownEvent>(Key, TrickleDown.TrickleDown); }
        private void Pointer(PointerDownEvent _) => director?.Arm();
        private void Key(KeyDownEvent _) => director?.Arm();
        public void Dispose()
        { root.UnregisterCallback<PointerDownEvent>(Pointer, TrickleDown.TrickleDown); root.UnregisterCallback<KeyDownEvent>(Key, TrickleDown.TrickleDown); }
    }
}
