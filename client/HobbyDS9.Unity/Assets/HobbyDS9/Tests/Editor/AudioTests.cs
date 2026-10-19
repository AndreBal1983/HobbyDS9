using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace HobbyDS9.UnityClient.Tests
{
    public sealed class AudioTests
    {
        [Test]
        public void CatalogContainsOriginalFullCuesWithStreamingImportSettings()
        {
            var catalog = Resources.Load<MusicCatalog>("MusicCatalog"); Assert.That(catalog, Is.Not.Null);
            Assert.That(MusicCatalogPolicy.Validate(catalog.tracks.Select(t => t.Metadata)), Is.Empty);
            Assert.That(catalog.tracks.Select(t => t.id), Is.Unique);
            Assert.That(catalog.tracks.Where(t=>t.category==MusicCategory.Strategic).OrderBy(t=>t.order).Select(t=>t.id),
                Is.EqualTo(new[] { "music.deep-station", "music.deep-station-1", "music.quiet-frontier" }));
            Assert.That(catalog.tracks.Where(t=>t.category==MusicCategory.Event).OrderBy(t=>t.order).Select(t=>t.id),
                Is.EqualTo(new[] { "music.across-the-stars", "music.secrets-in-the-courtyard" }));
            foreach (var entry in catalog.tracks)
            {
                Assert.That(entry.clip, Is.Not.Null);
                Assert.That(entry.loopPolicy, Is.EqualTo(MusicLoopPolicy.FullCue)); Assert.That(entry.crossfadeSeconds, Is.EqualTo(6));
                Assert.That(entry.clip.length, Is.GreaterThan(entry.id=="music.secrets-in-the-courtyard"?79:180));
                var importer = (AudioImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(entry.clip));
                Assert.That(importer.defaultSampleSettings.loadType, Is.EqualTo(AudioClipLoadType.Streaming));
                Assert.That(importer.defaultSampleSettings.preloadAudioData, Is.False); Assert.That(importer.forceToMono, Is.False);
                Assert.That(importer.loadInBackground, Is.True);
            }
        }
        [Test]
        public void SingletonViewRebuildsReuseExactlyTwoNonloopingSourcesAndReleaseObjects()
        {
            AudioDirector director = null;
            try
            {
                director = AudioDirector.TryGetOrCreate(); Assert.That(director, Is.Not.Null);
                var sources = director.GetComponents<AudioSource>(); Assert.That(sources.Length, Is.EqualTo(2));
                for (var i = 0; i < 10; i++)
                {
                    Assert.That(AudioDirector.TryGetOrCreate(), Is.SameAs(director));
                    director.SetContext(MusicCategory.Strategic); director.SetContext(MusicCategory.Combat);
                }
                Assert.That(director.GetComponents<AudioSource>().Length, Is.EqualTo(2));
                Assert.That(sources.All(s => !s.loop && !s.playOnAwake && s.spatialBlend == 0), Is.True);
                // EditMode establishes object/configuration lifetime, not audible playback.
                Assert.That(director.CurrentTrackId, Is.Null);
            }
            finally { if (director != null) Object.DestroyImmediate(director.gameObject); }
            Assert.That(Object.FindObjectsByType<AudioDirector>(FindObjectsSortMode.None), Is.Empty);
        }
        [Test]
        public void PlaylistAndAudioPreferencesDoNotMutateRealHostSave()
        {
            var host = new CampaignHost(Resources.Load<TextAsset>("ds9-slice").bytes); var before = host.SaveJson();
            var catalog = Resources.Load<MusicCatalog>("MusicCatalog"); var playlist = new MusicPlaylist(catalog.tracks.Select(t => t.Metadata));
            var preferences = new MusicPreferences(new MemoryStore());
            for (var i = 0; i < 8; i++)
            { playlist.SetContext(MusicCategory.Combat, _ => true); playlist.Next(_ => true); preferences.SetMusic(i / 10f); preferences.SetMuted(i % 2 == 0); }
            Assert.That(host.SaveJson(), Is.EqualTo(before));
        }
        private sealed class MemoryStore : IMusicSettingsStore
        {
            private readonly System.Collections.Generic.Dictionary<string, float> values = new System.Collections.Generic.Dictionary<string, float>();
            public float Read(string key, float fallback) => values.TryGetValue(key, out var value) ? value : fallback;
            public void Write(string key, float value) => values[key] = value;
            public void Flush() { }
        }
    }
}
