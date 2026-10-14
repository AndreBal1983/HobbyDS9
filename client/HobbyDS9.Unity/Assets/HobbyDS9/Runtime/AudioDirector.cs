#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace HobbyDS9.UnityClient
{
    public sealed class AudioDirector : MonoBehaviour
    {
        private static AudioDirector? instance;
        private readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>(StringComparer.Ordinal);
        private readonly HashSet<string> failed = new HashSet<string>(StringComparer.Ordinal);
        private readonly HashSet<string> warnings = new HashSet<string>(StringComparer.Ordinal);
        private readonly float[] gains = new float[2];
        private readonly float[] stopGains = new float[2];
        private readonly MusicTrack?[] sourceTracks = new MusicTrack?[2];
        private readonly MusicCueTiming?[] timings = new MusicCueTiming?[2];
        private AudioSource[] sources = Array.Empty<AudioSource>();
        private MusicPlaylist playlist = new MusicPlaylist(Array.Empty<MusicTrack>());
        private MusicTrack? waiting;
        private double requestedStart, loadDeadline, fadeStart, fadeSeconds, retryAfter, stopStart, stopSeconds;
        private int current = -1, outgoing = -1;
        private bool initialized, fading, stopping, paused;
        public MusicPreferences Preferences { get; private set; } = null!;
        public string? CurrentTrackId => current < 0 ? null : sourceTracks[current]?.Id;
        public string Status { get; private set; } = "Music initializing";
        public static AudioDirector? TryGetOrCreate()
        {
            try
            {
                if (instance == null) instance = FindFirstObjectByType<AudioDirector>();
                if (instance == null) instance = new GameObject("HobbyDS9 Audio").AddComponent<AudioDirector>();
                instance.Initialize(); return instance;
            }
            catch (Exception error) { Debug.LogWarning("Music unavailable; campaign continues. " + error.Message); return null; }
        }
        private void Awake()
        {
            if (instance != null && instance != this) { Destroy(gameObject); return; }
            instance = this;
            if (UnityEngine.Application.isPlaying) DontDestroyOnLoad(gameObject);
            try { Initialize(); }
            catch (Exception error) { Warn("initialize", "Music unavailable; campaign continues. " + error.Message); }
        }
        private void Initialize()
        {
            AudioOutputListener.Acquire(this);
            if (initialized) return;
            initialized = true;
            Preferences = new MusicPreferences(new PlayerPrefsMusicStore());
            // Reuse owned sources if the Editor rebuilds managed state; repeated
            // scene/view/session changes never add another decoder pair.
            var existing = GetComponents<AudioSource>();
            sources = new[] { existing.Length > 0 ? existing[0] : gameObject.AddComponent<AudioSource>(),
                existing.Length > 1 ? existing[1] : gameObject.AddComponent<AudioSource>() };
            foreach (var source in sources)
            { source.Stop(); source.clip = null; source.playOnAwake = false; source.loop = false; source.spatialBlend = 0; source.volume = 0; source.ignoreListenerPause = true; }
            var catalog = UnityEngine.Resources.Load<MusicCatalog>("MusicCatalog");
            var metadata = new List<MusicTrack>();
            if (catalog != null)
                foreach (var entry in catalog.tracks ?? Array.Empty<MusicTrackAsset>())
                {
                    if (entry == null || !MusicCatalogPolicy.Valid(entry.Metadata)) { Warn("metadata", "Invalid music metadata skipped."); continue; }
                    if (metadata.Any(t => t.Id == entry.id)) { Warn(entry.id, "Duplicate music track ID skipped: " + entry.id); continue; }
                    metadata.Add(entry.Metadata);
                    if (entry.clip != null) clips.Add(entry.id, entry.clip);
                    else Warn(entry.id, "Missing music clip skipped: " + entry.id);
                }
            else Warn("catalog", "MusicCatalog missing. Campaign remains playable without music.");
            playlist = new MusicPlaylist(metadata);
            Status = clips.Count == 0 ? "No playable music assets" : "Strategic playlist ready";
        }
        private bool Available(MusicTrack track) => clips.TryGetValue(track.Id, out var clip) && clip != null && !failed.Contains(track.Id) && clip.loadState != AudioDataLoadState.Failed;
        public void SetContext(MusicCategory category) { playlist.SetContext(category, Available); }
        public void SetMasterVolume(float value) { Preferences.SetMaster(value); ApplyVolumes(); SfxDirector.SharedMaster(Preferences.MasterVolume); }
        public void SetMusicVolume(float value) { Preferences.SetMusic(value); ApplyVolumes(); }
        public void SetMuted(bool value) { Preferences.SetMuted(value); ApplyVolumes(); }
        // Explicit service transitions, not mute: mute leaves DSP playback running.
        public void StopMusic(double seconds = 2)
        {
            paused = true; waiting = null;
            if (seconds <= 0 || double.IsNaN(seconds) || double.IsInfinity(seconds)) { StopSources(); return; }
            stopStart = AudioSettings.dspTime; stopSeconds = seconds;
            Array.Copy(gains, stopGains, 2); stopping = true;
        }
        public void ResumeMusic() { StopSources(); paused = false; retryAfter = 0; }
        private void Update()
        {
            if (!initialized || !UnityEngine.Application.isPlaying || sources.Length != 2) return;
            try { Tick(AudioSettings.dspTime); }
            catch (Exception error) { Warn("playback", "Music playback stopped safely: " + error.Message); StopMusic(0); }
        }
        private void Tick(double now)
        {
            if (stopping)
            {
                var progress = MusicEnvelope.Progress(now, stopStart, stopSeconds);
                for (var i = 0; i < 2; i++) gains[i] = stopGains[i] * MusicEnvelope.Outgoing(progress);
                ApplyVolumes(); if (progress >= 1) StopSources(); return;
            }
            if (paused) return;
            if (current >= 0 && sources[current].clip != null && sources[current].clip.loadState == AudioDataLoadState.Failed)
            { failed.Add(sourceTracks[current]!.Id); Warn(sourceTracks[current]!.Id, "Broken music clip skipped: " + sourceTracks[current]!.Id); StopSources(); }
            if (fading)
            {
                var progress = MusicEnvelope.Progress(now, fadeStart, fadeSeconds);
                gains[current] = MusicEnvelope.Incoming(progress);
                if (outgoing >= 0) gains[outgoing] = MusicEnvelope.Outgoing(progress);
                if (progress >= 1)
                {
                    if (outgoing >= 0) { sources[outgoing].Stop(); sources[outgoing].clip = null; gains[outgoing] = 0; }
                    outgoing = -1; fading = false;
                }
            }
            if (waiting == null && !fading && now >= retryAfter && (current < 0 || now >= timings[current]!.NextStartDsp - 2))
            {
                waiting = playlist.Next(Available);
                if (waiting == null) { retryAfter = now + 10; Status = "No playable music assets"; }
                else
                {
                    requestedStart = current < 0 ? now + 1 : timings[current]!.NextStartDsp;
                    loadDeadline = now + 5;
                    var clip = clips[waiting.Id];
                    // Streaming clips open their decoder via PlayScheduled. Other
                    // future import modes may preload asynchronously first.
                    if (clip.loadType != AudioClipLoadType.Streaming && clip.loadState == AudioDataLoadState.Unloaded) clip.LoadAudioData();
                }
            }
            if (waiting != null)
            {
                var clip = clips[waiting.Id];
                if (clip.loadState == AudioDataLoadState.Failed || now > loadDeadline || clip.length <= 0)
                { failed.Add(waiting.Id); Warn(waiting.Id, "Broken/unavailable music clip skipped: " + waiting.Id); waiting = null; }
                else if (clip.loadType == AudioClipLoadType.Streaming || clip.loadState == AudioDataLoadState.Loaded)
                {
                    var index = current < 0 ? 0 : 1 - current;
                    var start = Math.Max(requestedStart, now + .25);
                    sources[index].Stop(); sources[index].clip = clip;
                    sources[index].loop = waiting.LoopPolicy == MusicLoopPolicy.SeamlessLoop;
                    sources[index].volume = 0; sources[index].PlayScheduled(start);
                    outgoing = current; current = index; sourceTracks[index] = waiting;
                    fadeStart = start; fadeSeconds = outgoing < 0 ? 2 : timings[outgoing]!.CrossfadeSeconds;
                    timings[index] = new MusicCueTiming(start, clip.length, waiting);
                    gains[index] = 0; fading = true; waiting = null;
                    Status = "Cue: " + sourceTracks[index]!.Name;
                }
            }
            ApplyVolumes();
        }
        private void ApplyVolumes()
        { for (var i = 0; i < sources.Length; i++) if (sources[i] != null) sources[i].volume = gains[i] * Preferences.Gain; }
        private void StopSources()
        {
            foreach (var source in sources) if (source != null) { source.Stop(); source.clip = null; source.volume = 0; }
            gains[0] = gains[1] = 0; current = outgoing = -1; waiting = null; fading = stopping = false;
        }
        private void Warn(string key, string text) { if (warnings.Add(key)) Debug.LogWarning(text); }
        private void OnDisable() { StopSources(); }
        private void OnDestroy() { StopSources(); AudioOutputListener.Release(this); if (instance == this) instance = null; }
    }
    internal sealed class PlayerPrefsMusicStore : IMusicSettingsStore
    {
        private bool warned;
        public float Read(string key, float fallback) { try { return PlayerPrefs.GetFloat(key, fallback); } catch (Exception error) { Warn(error); return fallback; } }
        public void Write(string key, float value) { try { PlayerPrefs.SetFloat(key, value); } catch (Exception error) { Warn(error); } }
        public void Flush() { try { PlayerPrefs.Save(); } catch (Exception error) { Warn(error); } }
        private void Warn(Exception error) { if (!warned) { warned = true; Debug.LogWarning("Audio preferences could not be persisted: " + error.Message); } }
    }
}
