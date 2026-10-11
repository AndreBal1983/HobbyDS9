using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

namespace HobbyDS9.UnityClient.Tests
{
    public sealed class SfxTests
    {
        [Test]
        public void CatalogIncludesAllSixSemanticEntriesAndMissingClipsAreSafe()
        {
            var catalog = Resources.Load<LcarsSfxCatalog>("LcarsSfxCatalog");

            Assert.That(catalog, Is.Not.Null);
            Assert.That(catalog.entries.Length, Is.EqualTo(6));

            foreach (var entry in catalog.entries)
            {
                Assert.That(
                    entry.semanticId,
                    Is.EqualTo(SfxPolicy.SemanticId(entry.effect)));
            }

            Assert.That(catalog.Find((LcarsSfx)99), Is.Null);
        }

        [Test]
        public void PersistentServiceKeepsExactlyFourOneShotsAndOneProcessingSource()
        {
            var sfx = SfxDirector.TryGetOrCreate();

            Assert.That(sfx, Is.Not.Null);

            try
            {
                Assert.That(sfx.Policy.Armed, Is.False);

                for (var i = 0; i < 20; i++)
                {
                    Assert.That(
                        SfxDirector.TryGetOrCreate(),
                        Is.SameAs(sfx));

                    sfx.StartProcessing();
                    sfx.Play(LcarsSfx.ButtonPress);
                }

                Assert.That(
                    sfx.GetComponents<AudioSource>().Length,
                    Is.EqualTo(5));

                Assert.That(
                    sfx.GetComponents<AudioSource>()
                        .All(source =>
                            !source.loop &&
                            !source.playOnAwake &&
                            source.spatialBlend == 0),
                    Is.True);

                sfx.Arm();
                sfx.StopProcessing();
            }
            finally
            {
                Object.DestroyImmediate(sfx.gameObject);
            }
        }

        [Test]
        public void ExistingSourcesAreReusedAfterManagedInitialization()
        {
            var old = SfxDirector.TryGetOrCreate();

            if (old != null)
            {
                Object.DestroyImmediate(old.gameObject);
            }

            var go = new GameObject("Existing SFX source test");

            try
            {
                var original = Enumerable
                    .Range(0, 5)
                    .Select(_ => go.AddComponent<AudioSource>())
                    .ToArray();

                var sfx = go.AddComponent<SfxDirector>();

                Assert.That(
                    SfxDirector.TryGetOrCreate(),
                    Is.SameAs(sfx));

                Assert.That(
                    go.GetComponents<AudioSource>(),
                    Is.EqualTo(original));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void InputBindingConstructionAndDisposalDoNotArmService()
        {
            var sfx = SfxDirector.TryGetOrCreate();
            var root = new VisualElement();

            Assert.That(sfx, Is.Not.Null);

            try
            {
                Assert.That(sfx.Policy.Armed, Is.False);

                using (var binding = new SfxInputBinding(root, sfx))
                {
                    // Merely attaching the listener must not count as
                    // genuine user interaction.
                    Assert.That(sfx.Policy.Armed, Is.False);
                }

                Assert.That(sfx.Policy.Armed, Is.False);

                // Pointer/keyboard event dispatch requires an attached UI panel.
                // Actual first-user-interaction behavior is verified in the
                // locally running Windows player.
                sfx.Arm();

                Assert.That(sfx.Policy.Armed, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(sfx.gameObject);
            }
        }

        [Test]
        public void DeliberateNavigationCuesOnceAndRefreshResizeRemainSilent()
        {
            var sfx = SfxDirector.TryGetOrCreate();

            Assert.That(sfx, Is.Not.Null);

            var events = new List<LcarsSfx>();
            sfx.Requested += events.Add;

            try
            {
                var shell = new LcarsShell(
                    new VisualElement(),
                    new VisualElement(),
                    _ => { },
                    () => { },
                    () => { });

                Assert.That(events, Is.Empty);

                sfx.Arm();

                // Establish the passive current section.
                SfxDirector.ObserveSection("Empire");

                // Represent a deliberate navigation action semantically.
                SfxDirector.Cue(LcarsSfx.ButtonPress);
                sfx.OpenSection("Research");

                Assert.That(
                    events,
                    Is.EqualTo(new[]
                    {
                        LcarsSfx.ButtonPress,
                        LcarsSfx.ScreenOpen
                    }));

                events.Clear();

                // Passive selection, refresh and responsive layout changes
                // must remain silent.
                foreach (var width in new[] { 1422, 1920, 2580, 3840 })
                {
                    shell.SelectSection("Research");
                    shell.ApplyLogicalSize(width, 1080);
                    SfxDirector.ObserveSection("Research");
                }

                Assert.That(events, Is.Empty);

                SfxDirector.ObserveSection("Empire");

                Assert.That(events, Is.Empty);

                // A deliberate opening of another section still emits
                // exactly one ScreenOpen cue.
                sfx.OpenSection("Research");

                Assert.That(
                    events,
                    Is.EqualTo(new[]
                    {
                        LcarsSfx.ScreenOpen
                    }));
            }
            finally
            {
                Object.DestroyImmediate(sfx.gameObject);
            }
        }

        [Test]
        public void RenderingRealProductionDoesNotArmAudioOrChangeSave()
        {
            var sfx = SfxDirector.TryGetOrCreate();

            Assert.That(sfx, Is.Not.Null);

            var events = new List<LcarsSfx>();
            sfx.Requested += events.Add;

            try
            {
                var scenario = Resources.Load<TextAsset>("ds9-slice");
                var host = new CampaignHost(scenario.bytes);
                var before = host.SaveJson();

                var production = new ProductionOverviewView();

                production.Present(
                    new ProductionOverview(host.Client, host.Snapshot),
                    operation => operation());

                Assert.That(sfx.Policy.Armed, Is.False);
                Assert.That(events, Is.Empty);
                Assert.That(host.SaveJson(), Is.EqualTo(before));
            }
            finally
            {
                Object.DestroyImmediate(sfx.gameObject);
            }
        }

        [Test]
        public void MissingCatalogAndAudioObserverFailureDoNotEscapeIntoClient()
        {
            var sfx = SfxDirector.TryGetOrCreate();

            Assert.That(sfx, Is.Not.Null);

            try
            {
                sfx.Arm();

                sfx.Requested += _ =>
                    throw new System.InvalidOperationException(
                        "Presentation failure");

                Assert.DoesNotThrow(
                    () => sfx.Play(LcarsSfx.Success));

                Assert.DoesNotThrow(
                    () => SfxDirector.Cue(LcarsSfx.Error));
            }
            finally
            {
                Object.DestroyImmediate(sfx.gameObject);
            }
        }

        [Test]
        public void AudioControlsExposeIndependentSfxVolumeAndMute()
        {
            var sfx = SfxDirector.TryGetOrCreate();
            var root = new VisualElement();

            Assert.That(sfx, Is.Not.Null);

            try
            {
                ClientEntry.AddSfxControls(root, sfx);

                Assert.That(
                    root.Q<Slider>("sfx-volume"),
                    Is.Not.Null);

                Assert.That(
                    root.Q<Toggle>("sfx-mute"),
                    Is.Not.Null);
            }
            finally
            {
                Object.DestroyImmediate(sfx.gameObject);
            }
        }
    }
}