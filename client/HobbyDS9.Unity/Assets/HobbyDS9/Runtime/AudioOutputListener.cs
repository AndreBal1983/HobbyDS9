#nullable enable
using System.Collections.Generic;
using UnityEngine;

namespace HobbyDS9.UnityClient
{
    // Shared by the independent music and SFX services. Scene listeners retain
    // ownership; this fallback only supplies output when no scene listener is active.
    [DisallowMultipleComponent]
    public sealed class AudioOutputListener : MonoBehaviour
    {
        private static AudioOutputListener? instance;
        private readonly HashSet<MonoBehaviour> owners = new HashSet<MonoBehaviour>();
        private AudioListener? output;
        private bool releasing;

        internal static void Acquire(MonoBehaviour owner)
        {
            if (instance == null)
            {
                foreach (var candidate in Object.FindObjectsByType<AudioOutputListener>())
                    if (!candidate.releasing) { instance = candidate; break; }
                if (instance == null)
                {
                    instance = new GameObject("HobbyDS9 Audio Output").AddComponent<AudioOutputListener>();
                    if (UnityEngine.Application.isPlaying) DontDestroyOnLoad(instance.gameObject);
                }
            }
            instance.owners.Add(owner);
            instance.Refresh();
        }

        internal static void Release(MonoBehaviour owner)
        {
            if (instance == null) return;
            instance.owners.Remove(owner);
            instance.owners.RemoveWhere(o => o == null);
            if (instance.owners.Count == 0) instance.ReleaseOutput();
        }

        private void Update() => Refresh();

        private void Refresh()
        {
            if (releasing) return;
            if (output == null) output = GetComponent<AudioListener>();
            if (output == null) output = gameObject.AddComponent<AudioListener>();
            bool sceneListener = false;
            foreach (var listener in Object.FindObjectsByType<AudioListener>())
                if (listener != output && listener.isActiveAndEnabled) { sceneListener = true; break; }
            // Never alter another listener, global volume/pause, or client preferences.
            output.enabled = !sceneListener;
        }

        private void ReleaseOutput()
        {
            releasing = true;
            if (output != null) output.enabled = false;
            if (instance == this) instance = null;
            if (UnityEngine.Application.isPlaying) Destroy(gameObject);
            else DestroyImmediate(gameObject);
        }

        private void OnDisable() { if (output != null) output.enabled = false; }
        private void OnDestroy() { if (instance == this) instance = null; }
    }
}
