using System;
using UnityEngine;
using UnityEngine.Profiling;

namespace HobbyDS9.UnityClient
{
    // Explicit operator evidence only. No campaign, input, scene-light or timing changes.
    // Frame intervals include VSync/OS scheduling; these are not GPU execution timings.
    public sealed class NativeAcceptanceProbe : MonoBehaviour
    {
        private const int Capacity = 8192;
        private const double WarmupSeconds = 5;
        private const double ReportSeconds = 15;
        private readonly float[] samples = new float[Capacity];
        private static bool installed;
        private int count, next;
        private long total;
        private double started, reportAt;
        private bool skipNext = true, quitting;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (installed || Array.IndexOf(Environment.GetCommandLineArgs(), "--hobbyds9-native-evidence") < 0) return;
            installed = true;
            var owner = new GameObject("HobbyDS9 Native Acceptance Probe");
            DontDestroyOnLoad(owner);
            owner.AddComponent<NativeAcceptanceProbe>();
        }

        private void Awake()
        {
            started = Time.realtimeSinceStartupAsDouble;
            reportAt = started + ReportSeconds;
        }

        private void Update()
        {
            double now = Time.realtimeSinceStartupAsDouble;
            if (!Application.isFocused || now - started < WarmupSeconds) { skipNext = true; return; }
            if (skipNext) { skipNext = false; return; }
            float ms = Time.unscaledDeltaTime * 1000f;
            if (ms > 0 && !float.IsNaN(ms) && !float.IsInfinity(ms))
            {
                samples[next] = ms;
                next = (next + 1) % Capacity;
                count = Math.Min(count + 1, Capacity);
                total++;
            }
            if (now >= reportAt) Emit("interval");
        }

        private void OnApplicationFocus(bool focused)
        {
            skipNext = true; // Never count time spent outside the focused player.
            if (!focused && count > 0) Emit("focus-lost");
        }

        private void OnApplicationQuit() { quitting = true; Emit("quit"); }
        private void OnDestroy() { installed = false; if (!quitting && count > 0) Emit("disposed"); }

        private void Emit(string reason)
        {
            try
            {
                var sorted = new float[count];
                Array.Copy(samples, sorted, count);
                Array.Sort(sorted);
                int listeners = 0, playing = 0;
                var allListeners = FindObjectsByType<AudioListener>(FindObjectsInactive.Exclude);
                foreach (var listener in allListeners) if (listener.enabled) listeners++;
                var sources = FindObjectsByType<AudioSource>(FindObjectsInactive.Exclude);
                foreach (var source in sources) if (source.isPlaying) playing++;
                var report = new Evidence
                {
                    reason = reason,
                    elapsedSeconds = Time.realtimeSinceStartupAsDouble - started,
                    samplesInWindow = count,
                    focusedSamplesTotal = total,
                    frameP50Ms = Percentile(sorted, .50),
                    frameP95Ms = Percentile(sorted, .95),
                    frameP99Ms = Percentile(sorted, .99),
                    maxFrameMs = Percentile(sorted, 1),
                    screenWidth = Screen.width, screenHeight = Screen.height,
                    focused = Application.isFocused,
                    vSyncCount = QualitySettings.vSyncCount, targetFrameRate = Application.targetFrameRate,
                    engineAllocatedBytes = Profiler.GetTotalAllocatedMemoryLong(),
                    engineReservedBytes = Profiler.GetTotalReservedMemoryLong(),
                    gen0Collections = GC.CollectionCount(0),
                    enabledListeners = listeners, audioSources = sources.Length, playingAudioSources = playing,
                    outputSampleRate = AudioSettings.outputSampleRate,
                    renderTextures = Resources.FindObjectsOfTypeAll<RenderTexture>().Length,
                    materials = Resources.FindObjectsOfTypeAll<Material>().Length,
                    meshes = Resources.FindObjectsOfTypeAll<Mesh>().Length
                };
                Debug.Log("HOBBYDS9_NATIVE_EVIDENCE " + JsonUtility.ToJson(report));
            }
            catch (Exception error)
            {
                Debug.LogWarning("Native acceptance evidence unavailable: " + error.GetType().Name);
            }
            count = next = 0;
            reportAt = Time.realtimeSinceStartupAsDouble + ReportSeconds;
        }

        private static float Percentile(float[] values, double fraction)
            => values.Length == 0 ? 0 : values[Math.Max(0, (int)Math.Ceiling(values.Length * fraction) - 1)];

        [Serializable]
        private sealed class Evidence
        {
            public string reason = string.Empty;
            public double elapsedSeconds;
            public int samplesInWindow;
            public long focusedSamplesTotal;
            public float frameP50Ms, frameP95Ms, frameP99Ms, maxFrameMs;
            public int screenWidth, screenHeight;
            public bool focused;
            public int vSyncCount, targetFrameRate;
            public long engineAllocatedBytes, engineReservedBytes;
            public int gen0Collections, enabledListeners, audioSources, playingAudioSources, outputSampleRate;
            public int renderTextures, materials, meshes;
        }
    }
}
