#nullable enable
using System;
using UnityEngine;

namespace HobbyDS9.UnityClient
{
    [Serializable]
    public sealed class MusicTrackAsset
    {
        public string id = "";
        public string displayName = "";
        public AudioClip? clip;
        public MusicCategory category;
        public MusicLoopPolicy loopPolicy;
        public int order;
        public float crossfadeSeconds = 6;
        public float quietTailSeconds;
        public MusicTrack Metadata => new MusicTrack(id, displayName, category, loopPolicy, order, crossfadeSeconds, quietTailSeconds);
    }
    [CreateAssetMenu(menuName = "HobbyDS9/Music catalog")]
    public sealed class MusicCatalog : ScriptableObject
    {
        public MusicTrackAsset[] tracks = Array.Empty<MusicTrackAsset>();
    }
}
