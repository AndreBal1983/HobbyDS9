using System.Linq;
using System.Collections.Generic;
using UnityEditor;
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
        [Test] public void ExistingSourcesAreReusedAfterManagedInitialization()
        {
            var old=SfxDirector.TryGetOrCreate();if(old!=null)Object.DestroyImmediate(old.gameObject);
            var go=new GameObject("Existing SFX source test");
            try {
                var original=Enumerable.Range(0,5).Select(_=>go.AddComponent<AudioSource>()).ToArray();
                var sfx=go.AddComponent<SfxDirector>();
                Assert.That(SfxDirector.TryGetOrCreate(),Is.SameAs(sfx));
                Assert.That(go.GetComponents<AudioSource>(),Is.EqualTo(original));
            } finally { Object.DestroyImmediate(go); }
        }
        [TestCase(true)] [TestCase(false)] public void GenuinePointerOrKeyboardInputArmsService(bool pointer)
        {
            var sfx=SfxDirector.TryGetOrCreate();var window=ScriptableObject.CreateInstance<LcarsGeometryTestWindow>();window.Show();var root=window.rootVisualElement;
            try { using(var binding=new SfxInputBinding(root,sfx)) {
                Assert.That(sfx.Policy.Armed,Is.False);
                if(pointer) { using(var e=PointerDownEvent.GetPooled()) { e.target=root;root.SendEvent(e); } }
                else { using(var e=KeyDownEvent.GetPooled()) { e.target=root;root.SendEvent(e); } }
                Assert.That(sfx.Policy.Armed,Is.True);
            }} finally { window.Close(); Object.DestroyImmediate(sfx.gameObject); }
        }
        [Test] public void DeliberateNavigationCuesOnceAndRefreshResizeRemainSilent()
        {
            var sfx=SfxDirector.TryGetOrCreate(); var events=new List<LcarsSfx>();sfx.Requested+=events.Add;
            var window=ScriptableObject.CreateInstance<LcarsGeometryTestWindow>();
            try {
                window.Show();var shell=new LcarsShell(new VisualElement(),new VisualElement(),s=>sfx.OpenSection(s),()=>{},()=>{});window.rootVisualElement.Add(shell);
                Assert.That(events,Is.Empty);sfx.Arm();
                var research=shell.Navigation.Sections.Single(s=>s.Section=="Research");
                using(var e=NavigationSubmitEvent.GetPooled()) { e.target=research;research.SendEvent(e); }
                Assert.That(events,Is.EqualTo(new[]{LcarsSfx.ButtonPress,LcarsSfx.ScreenOpen}));
                events.Clear();
                foreach(var width in new[]{1422,1920,2580,3840}) { shell.SelectSection("Research");shell.ApplyLogicalSize(width,1080);sfx.OpenSection("Research"); }
                Assert.That(events,Is.Empty);
                SfxDirector.ObserveSection("Empire");Assert.That(events,Is.Empty);
                sfx.OpenSection("Research");Assert.That(events,Is.EqualTo(new[]{LcarsSfx.ScreenOpen}));
            } finally { window.Close();Object.DestroyImmediate(sfx.gameObject); }
        }
        [Test] public void RenderingRealProductionDoesNotArmAudioOrChangeSave()
        {
            var sfx=SfxDirector.TryGetOrCreate();var events=new List<LcarsSfx>();sfx.Requested+=events.Add;
            try {
                var host=new CampaignHost(Resources.Load<TextAsset>("ds9-slice").bytes);var before=host.SaveJson();
                var production=new ProductionOverviewView();production.Present(new ProductionOverview(host.Client,host.Snapshot),operation=>operation());
                Assert.That(sfx.Policy.Armed,Is.False);Assert.That(events,Is.Empty);Assert.That(host.SaveJson(),Is.EqualTo(before));
            } finally { Object.DestroyImmediate(sfx.gameObject); }
        }
        [Test] public void MissingCatalogAndAudioObserverFailureDoNotEscapeIntoClient()
        {
            var sfx=SfxDirector.TryGetOrCreate();
            try { sfx.Arm();sfx.Requested+=_=>throw new System.InvalidOperationException("Presentation failure");
                Assert.DoesNotThrow(()=>sfx.Play(LcarsSfx.Success));Assert.DoesNotThrow(()=>SfxDirector.Cue(LcarsSfx.Error));
            } finally { Object.DestroyImmediate(sfx.gameObject); }
        }
        [Test] public void AudioControlsExposeIndependentSfxVolumeAndMute()
        {
            var sfx=SfxDirector.TryGetOrCreate();var root=new VisualElement();
            try { ClientEntry.AddSfxControls(root,sfx);Assert.That(root.Q<Slider>("sfx-volume"),Is.Not.Null);Assert.That(root.Q<Toggle>("sfx-mute"),Is.Not.Null); }
            finally { Object.DestroyImmediate(sfx.gameObject); }
        }
    }
}
