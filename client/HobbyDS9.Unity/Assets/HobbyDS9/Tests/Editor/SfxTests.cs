using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;
namespace HobbyDS9.UnityClient.Tests
{
    public sealed class SfxTests
    {
        [Test] public void CatalogIncludesAllSixSemanticEntriesAndMissingClipsAreSafe()
        {
            var catalog=Resources.Load<LcarsSfxCatalog>("LcarsSfxCatalog");Assert.That(catalog,Is.Not.Null);
            Assert.That(catalog.entries.Length,Is.EqualTo(6));
            foreach(var entry in catalog.entries) Assert.That(entry.semanticId,Is.EqualTo(SfxPolicy.SemanticId(entry.effect)));
            Assert.That(catalog.Find((LcarsSfx)99),Is.Null);
        }
        [Test] public void PersistentServiceKeepsExactlyFourOneShotsAndOneProcessingSource()
        {
            var sfx=SfxDirector.TryGetOrCreate();Assert.That(sfx,Is.Not.Null);
            try {
                Assert.That(sfx.Policy.Armed,Is.False);
                for(int i=0;i<20;i++) { Assert.That(SfxDirector.TryGetOrCreate(),Is.SameAs(sfx));sfx.StartProcessing();sfx.Play(LcarsSfx.ButtonPress); }
                Assert.That(sfx.GetComponents<AudioSource>().Length,Is.EqualTo(5));
                Assert.That(sfx.GetComponents<AudioSource>().All(s=>!s.loop&&!s.playOnAwake&&s.spatialBlend==0),Is.True);
                sfx.Arm();sfx.StopProcessing();
            } finally { Object.DestroyImmediate(sfx.gameObject); }
        }
        [TestCase(true)] [TestCase(false)] public void GenuinePointerOrKeyboardInputArmsService(bool pointer)
        {
            var sfx=SfxDirector.TryGetOrCreate();var root=new VisualElement();
            try { using(var binding=new SfxInputBinding(root,sfx)) {
                Assert.That(sfx.Policy.Armed,Is.False);
                if(pointer) { using(var e=PointerDownEvent.GetPooled()) { e.target=root;root.SendEvent(e); } }
                else { using(var e=KeyDownEvent.GetPooled()) { e.target=root;root.SendEvent(e); } }
                Assert.That(sfx.Policy.Armed,Is.True);
            }} finally { Object.DestroyImmediate(sfx.gameObject); }
        }
        [Test] public void AudioControlsExposeIndependentSfxVolumeAndMute()
        {
            var sfx=SfxDirector.TryGetOrCreate();var root=new VisualElement();
            try { ClientEntry.AddSfxControls(root,sfx);Assert.That(root.Q<Slider>("sfx-volume"),Is.Not.Null);Assert.That(root.Q<Toggle>("sfx-mute"),Is.Not.Null); }
            finally { Object.DestroyImmediate(sfx.gameObject); }
        }
    }
}
