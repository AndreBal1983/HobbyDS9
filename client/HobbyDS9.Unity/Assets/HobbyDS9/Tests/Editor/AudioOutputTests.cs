using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace HobbyDS9.UnityClient.Tests
{
    public sealed class AudioOutputTests
    {

        [SetUp]
        public void EmptyScene()
        {
            foreach (var director in Object.FindObjectsByType<AudioDirector>()) Object.DestroyImmediate(director.gameObject);
            foreach (var director in Object.FindObjectsByType<SfxDirector>()) Object.DestroyImmediate(director.gameObject);
            foreach (var output in Object.FindObjectsByType<AudioOutputListener>()) Object.DestroyImmediate(output.gameObject);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Assert.That(ActiveListeners(), Is.Empty);
        }

        [TearDown]
        public void Restore()
        {
            foreach (var director in Object.FindObjectsByType<AudioDirector>()) Object.DestroyImmediate(director.gameObject);
            foreach (var director in Object.FindObjectsByType<SfxDirector>()) Object.DestroyImmediate(director.gameObject);
            foreach (var output in Object.FindObjectsByType<AudioOutputListener>()) Object.DestroyImmediate(output.gameObject);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }

        private static AudioListener[] ActiveListeners() => Object.FindObjectsByType<AudioListener>().Where(l => l.isActiveAndEnabled).ToArray();

        [TestCase(false)]
        [TestCase(true)]
        public void EmptySceneDirectorsProvideOneOutputListenerWithoutGrowingSources(bool sfxFirst)
        {
            if (sfxFirst) SfxDirector.TryGetOrCreate();
            var music = AudioDirector.TryGetOrCreate();
            var sfx = SfxDirector.TryGetOrCreate();
            Assert.That(music, Is.Not.Null);
            Assert.That(sfx, Is.Not.Null);
            Assert.That(ActiveListeners(), Has.Length.EqualTo(1), "Sources alone cannot deliver scene audio without a listener.");
            var listener = ActiveListeners().Single();
            var musicSources = music.GetComponents<AudioSource>();
            var sfxSources = sfx.GetComponents<AudioSource>();
            var preferences = new[] { music.Preferences.MasterVolume, music.Preferences.MusicVolume, sfx.Preferences.Volume };
            for (int i = 0; i < 10; i++)
            {
                Assert.That(AudioDirector.TryGetOrCreate(), Is.SameAs(music));
                Assert.That(SfxDirector.TryGetOrCreate(), Is.SameAs(sfx));
            }
            Assert.That(ActiveListeners(), Is.EqualTo(new[] { listener }));
            Assert.That(music.GetComponents<AudioSource>(), Is.EqualTo(musicSources));
            Assert.That(sfx.GetComponents<AudioSource>(), Is.EqualTo(sfxSources));
            Assert.That(musicSources, Has.Length.EqualTo(2));
            Assert.That(sfxSources, Has.Length.EqualTo(5));
            Assert.That(musicSources.Concat(sfxSources).All(s => s.spatialBlend == 0 && !s.playOnAwake && !s.loop), Is.True);
            Assert.That(new[] { music.Preferences.MasterVolume, music.Preferences.MusicVolume, sfx.Preferences.Volume }, Is.EqualTo(preferences));
        }

        [Test]
        public void ExistingSceneListenerIsKeptAndFallbackTakesOverWhenItDisables()
        {
            var authored = new GameObject("Authored listener").AddComponent<AudioListener>();
            AudioDirector.TryGetOrCreate(); SfxDirector.TryGetOrCreate();
            Assert.That(ActiveListeners(), Is.EqualTo(new[] { authored }));
            authored.enabled = false;
            AudioDirector.TryGetOrCreate();
            Assert.That(ActiveListeners(), Has.Length.EqualTo(1));
            Assert.That(authored.enabled, Is.False);
            authored.enabled = true;
            SfxDirector.TryGetOrCreate();
            Assert.That(ActiveListeners(), Is.EqualTo(new[] { authored }));
        }

        [Test]
        public void LateSceneListenerIsPreferredAndRemovingItRestoresFallback()
        {
            AudioDirector.TryGetOrCreate();
            var fallback = ActiveListeners().Single();
            var authored = new GameObject("Late scene listener").AddComponent<AudioListener>();
            SfxDirector.TryGetOrCreate();
            Assert.That(ActiveListeners(), Is.EqualTo(new[] { authored }));
            Assert.That(fallback.enabled, Is.False);
            Object.DestroyImmediate(authored.gameObject);
            AudioDirector.TryGetOrCreate();
            Assert.That(ActiveListeners(), Is.EqualTo(new[] { fallback }));
        }

        [UnityTest]
        public IEnumerator OutputSurvivesSceneReplacementYieldsToSceneListenerAndReleasesWithLastService()
        {
            yield return new EnterPlayMode();
            var music = AudioDirector.TryGetOrCreate(); var sfx = SfxDirector.TryGetOrCreate();
            Assert.That(music.gameObject.scene.name, Is.EqualTo("DontDestroyOnLoad"));
            Assert.That(sfx.gameObject.scene.name, Is.EqualTo("DontDestroyOnLoad"));
            var fallback = ActiveListeners().Single();
            Assert.That(fallback.gameObject.scene.name, Is.EqualTo("DontDestroyOnLoad"));
            var prior = SceneManager.GetActiveScene();
            var next = SceneManager.CreateScene("Replacement empty audio scene");
            SceneManager.SetActiveScene(next);
            yield return SceneManager.UnloadSceneAsync(prior);
            Assert.That(ActiveListeners(), Is.EqualTo(new[] { fallback }));
            var authored = new GameObject("Scene listener after transition").AddComponent<AudioListener>();
            yield return null; yield return null;
            Assert.That(ActiveListeners(), Is.EqualTo(new[] { authored }));
            Object.Destroy(authored.gameObject);
            yield return null; yield return null;
            Assert.That(ActiveListeners(), Is.EqualTo(new[] { fallback }));
            Object.Destroy(music.gameObject);
            yield return null; yield return null;
            Assert.That(ActiveListeners(), Has.Length.EqualTo(1));
            Object.Destroy(sfx.gameObject);
            yield return null; yield return null;
            Assert.That(ActiveListeners(), Is.Empty);
            music = AudioDirector.TryGetOrCreate(); sfx = SfxDirector.TryGetOrCreate();
            Assert.That(ActiveListeners(), Has.Length.EqualTo(1));
            Object.Destroy(sfx.gameObject);
            yield return null; yield return null;
            Assert.That(ActiveListeners(), Has.Length.EqualTo(1));
            Object.Destroy(music.gameObject);
            yield return null; yield return null;
            Assert.That(ActiveListeners(), Is.Empty);
            yield return new ExitPlayMode();
        }

        [Test]
        public void ActualSampleSceneListenerIsNotDuplicatedOrReconfigured()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity", OpenSceneMode.Single);
            var authored = ActiveListeners().Single();
            var volume = AudioListener.volume; var paused = AudioListener.pause;
            AudioDirector.TryGetOrCreate(); SfxDirector.TryGetOrCreate();
            Assert.That(ActiveListeners(), Is.EqualTo(new[] { authored }));
            Assert.That(AudioListener.volume, Is.EqualTo(volume));
            Assert.That(AudioListener.pause, Is.EqualTo(paused));
        }

        [Test]
        public void ImportedSfxCatalogContainsDecodableNonzeroSignal()
        {
            var catalog = Resources.Load<LcarsSfxCatalog>("LcarsSfxCatalog");
            Assert.That(catalog, Is.Not.Null);
            Assert.That(catalog.entries, Has.Length.EqualTo(6));
            foreach (var entry in catalog.entries)
            {
                var clip = entry.clip;
                Assert.That(clip, Is.Not.Null, entry.semanticId);
                Assert.That(clip.LoadAudioData(), Is.True, entry.semanticId);
                Assert.That(clip.loadState, Is.EqualTo(AudioDataLoadState.Loaded), entry.semanticId);
                var samples = new float[clip.samples * clip.channels];
                Assert.That(clip.GetData(samples, 0), Is.True, entry.semanticId);
                Assert.That(samples.Any(s => System.Math.Abs(s) > 0.001f), Is.True, entry.semanticId);
            }
        }
    }
}
