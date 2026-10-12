using System.Linq;
using HobbyDS9.Contracts.ShipDesign;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;
namespace HobbyDS9.UnityClient.Tests
{
    public sealed class LcarsPowerSliderTests
    {
        [TestCase(0,"zero")][TestCase(1,"low")][TestCase(6,"medium")][TestCase(12,"high")][TestCase(-1,"invalid")][TestCase(13,"invalid")]
        public void RangeRoundTripAndPositionStatesNeverClampExactInput(long value,string state)
        {
            var commits=0;var slider=new LcarsPowerSlider("Allocated","test",value,12,false,_=>commits++);
            Assert.That(slider.Value,Is.EqualTo(value));Assert.That(slider.Exact.value,Is.EqualTo(value));Assert.That(slider.PositionState,Is.EqualTo(state));Assert.That(commits,Is.Zero);
            var window=ScriptableObject.CreateInstance<UnityEditor.EditorWindow>();
            try {window.Show();window.rootVisualElement.Add(slider);slider.Exact.value=100;Assert.That(slider.Value,Is.EqualTo(100));Assert.That(slider.Track.value,Is.EqualTo(12));Assert.That(commits,Is.EqualTo(1));}
            finally {window.Close();}
        }
        [Test]
        public void NoDemandDisablesTrackAndExplainsIt()
        {
            var slider=new LcarsPowerSlider("Requested","test",0,0,false,_=>Assert.Fail());
            Assert.That(slider.Track.enabledSelf,Is.False);Assert.That(slider.PositionState,Is.EqualTo("disabled"));Assert.That(slider.Query<Label>().ToList().Any(l=>l.text.Contains("NO POWER DEMAND")),Is.True);
        }
        [Test]
        public void BackendRangesUseActualStackAndProfilePreviewWithoutCampaignMutation()
        {
            var host=new CampaignHost(Resources.Load<TextAsset>("ds9-slice").bytes);var save=host.SaveJson();
            var provider=ShipDesignFixtureLoader.Load();var draft=new ShipDesignDraft(provider);draft.RequestTemplate("class.escort");draft.SelectProfile(DesignerProfile.Combat);
            using(var view=new ShipDesignerView(draft,()=>{}))
            {
                view.SelectSystem("Profiles");var power=provider.Evaluate(draft.Current).Profiles.Single(p=>p.Kind==OperatingProfileKind.Combat).Installations.Single(i=>i.InstallationId==draft.Current.Power("component.energy-bank.v1",DesignerProfile.Combat).InstallationId);
                var slider=view.Q<LcarsPowerSlider>("slider-power-allocated-component.energy-bank.v1");Assert.That(slider.Track.highValue,Is.EqualTo(power.FullPower));
                var window=ScriptableObject.CreateInstance<UnityEditor.EditorWindow>();
                try {window.Show();window.rootVisualElement.Add(view);slider.Exact.value=100;}
                finally {window.Close();}
                Assert.That(draft.Current.Power("component.energy-bank.v1",DesignerProfile.Combat).AllocatedPower,Is.EqualTo(100));
                Assert.That(provider.Evaluate(draft.Current).Issues.Any(i=>i.Code==DesignIssueCode.InvalidPowerAllocation),Is.True);
                view.SelectSystem("Overview");view.SelectSystem("Profiles");Assert.That(view.Q<LongField>("power-allocated-component.energy-bank.v1").value,Is.EqualTo(100));
                draft.SelectProfile(DesignerProfile.Cruise);Assert.That(draft.Current.Power("component.energy-bank.v1",DesignerProfile.Cruise).AllocatedPower,Is.Zero);
                draft.SelectProfile(DesignerProfile.Combat);Assert.That(view.Q<LcarsPowerSlider>("slider-power-allocated-component.energy-bank.v1").PositionState,Is.EqualTo("invalid"));
                view.RequestLeave(()=>Assert.Fail("Discard guard skipped"));draft.CancelDiscard();Assert.That(draft.Modified,Is.True);draft.RequestReset();draft.ConfirmDiscard();Assert.That(draft.Modified,Is.False);
                Assert.That(view.Q<LongField>("power-allocated-component.energy-bank.v1").value,Is.EqualTo(12));Assert.That(host.SaveJson(),Is.EqualTo(save));
            }
        }
        [Test]
        public void PointerMotionDefersCommitsAndKeyboardUsesOneUnit()
        {
            var window=ScriptableObject.CreateInstance<UnityEditor.EditorWindow>();var commits=0;long stored=0;
            var slider=new LcarsPowerSlider("Allocated","test",0,12,false,v=>{stored=v;commits++;});
            try
            {
                window.Show();window.rootVisualElement.Add(slider);
                using(var e=PointerDownEvent.GetPooled()){e.target=slider.Track;slider.Track.SendEvent(e);}
                slider.Track.value=4;slider.Track.value=7;Assert.That(commits,Is.Zero);Assert.That(slider.Value,Is.EqualTo(7));
                using(var e=PointerUpEvent.GetPooled()){e.target=slider.Track;slider.Track.SendEvent(e);}
                Assert.That(commits,Is.EqualTo(1));Assert.That(stored,Is.EqualTo(7));
                using(var e=KeyDownEvent.GetPooled('\0',KeyCode.RightArrow,EventModifiers.None)){e.target=slider.Track;slider.Track.SendEvent(e);}
                Assert.That(stored,Is.EqualTo(8));Assert.That(commits,Is.EqualTo(2));
                using(var e=KeyDownEvent.GetPooled('\0',KeyCode.End,EventModifiers.None)){e.target=slider.Track;slider.Track.SendEvent(e);}
                Assert.That(stored,Is.EqualTo(12));
            }
            finally{window.Close();}
        }
        [Test]
        public void FullPositionIsNotErrorButBackendBudgetErrorsRemainVisible()
        {
            var healthy=new LcarsPowerSlider("Allocated","test",12,12,false,_=>{});Assert.That(healthy.PositionState,Is.EqualTo("high"));Assert.That(healthy.ClassListContains("lps-backend-invalid"),Is.False);
            var provider=ShipDesignFixtureLoader.Load();var draft=new ShipDesignDraft(provider);draft.RequestTemplate("class.escort");draft.SelectProfile(DesignerProfile.Combat);
            var preview=provider.Evaluate(draft.Current).Profiles.Single(p=>p.Kind==OperatingProfileKind.Combat);
            foreach(var i in preview.Installations){var id=draft.Current.ToDto().Installations.Single(x=>x.InstallationId==i.InstallationId).ComponentVersionId;draft.SetPower(id,i.FullPower,i.FullPower);}
            Assert.That(provider.Evaluate(draft.Current).Issues.Any(i=>i.Code==DesignIssueCode.AvailablePowerExceeded),Is.True);
            using(var view=new ShipDesignerView(draft,()=>{})){view.SelectSystem("Profiles");Assert.That(view.Query<Label>().ToList().Any(l=>l.text.Contains("BACKEND ERROR")),Is.True);}
        }
    }
}
