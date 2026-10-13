using System.Linq;
using System.Collections;
using HobbyDS9.Contracts.ShipDesign;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.TestTools;
namespace HobbyDS9.UnityClient.Tests
{
    public sealed class LcarsPowerSliderTests
    {
        [TestCase(0,0)][TestCase(6,6)][TestCase(12,12)]
        public void VerticalFillCountsFromBottomWithoutChangingBackendValidity(long value,int filled)
        {
            var slider=new LcarsPowerSlider("Allocated","test",value,12,false,_=>{},4);
            Assert.That(slider.Track.direction,Is.EqualTo(SliderDirection.Vertical));Assert.That(slider.Track.inverted,Is.False);
            Assert.That(slider.Query<VisualElement>(className:"lps-filled").ToList().Count,Is.EqualTo(filled));
            for(var i=0;i<12;i++)Assert.That(slider.Q<VisualElement>("segment-"+i).ClassListContains("lps-filled"),Is.EqualTo(i<filled));
            Assert.That(slider.Q<VisualElement>("segment-0").ClassListContains("lps-band-low"),Is.True);
            Assert.That(slider.Q<VisualElement>("segment-11").ClassListContains("lps-band-high"),Is.True);
            Assert.That(slider.Query<Label>().ToList().Any(l=>l.text=="Powered min 4"),Is.True);
        }
        [UnityTest]
        public IEnumerator StyledRailPlacesZeroAtBottomAndPreservesKeyboardFocusAfterRefresh()
        {
            var window=ScriptableObject.CreateInstance<UnityEditor.EditorWindow>();
            var draft=new ShipDesignDraft(ShipDesignFixtureLoader.Load());draft.RequestTemplate("class.escort");draft.SelectProfile(DesignerProfile.Combat);
            using(var view=new ShipDesignerView(draft,()=>{}))
            {
                try
                {
                    window.rootVisualElement.styleSheets.Add(Resources.Load<StyleSheet>("ShipDesigner"));
                    window.Show();window.rootVisualElement.Add(view);view.SelectSystem("Profiles");yield return null;yield return null;
                    var slider=view.Q<LcarsPowerSlider>("slider-power-allocated-component.energy-bank.v1");
                    var bottom=slider.Q<VisualElement>("segment-0");var top=slider.Q<VisualElement>("segment-11");
                    Assert.That(bottom.worldBound.yMin,Is.GreaterThan(top.worldBound.yMax));
                    var thumb=slider.Track.Q<VisualElement>(className:"unity-base-slider__dragger");
                    slider.Track.SetValueWithoutNotify(0);yield return null;var zeroY=thumb.worldBound.center.y;
                    slider.Track.SetValueWithoutNotify(slider.Track.highValue);yield return null;
                    Assert.That(thumb.worldBound.center.y,Is.LessThan(zeroY-80),"Real vertical thumb must rise as power increases");
                    Assert.That(thumb.worldBound.xMin,Is.GreaterThanOrEqualTo(slider.Track.worldBound.xMin));
                    Assert.That(thumb.worldBound.xMax,Is.LessThanOrEqualTo(slider.Track.worldBound.xMax),"Thumb must not overlap adjacent legends");
                    slider.Track.Focus();
                    using(var e=KeyDownEvent.GetPooled('\0',KeyCode.Home,EventModifiers.None)){e.target=slider.Track;slider.Track.SendEvent(e);}
                    yield return null;yield return null;
                    slider=view.Q<LcarsPowerSlider>("slider-power-allocated-component.energy-bank.v1");
                    Assert.That(slider.panel.focusController.focusedElement,Is.EqualTo(slider.Track));
                    using(var e=KeyDownEvent.GetPooled('\0',KeyCode.UpArrow,EventModifiers.None)){e.target=slider.Track;slider.Track.SendEvent(e);}
                    Assert.That(draft.Current.Power("component.energy-bank.v1",DesignerProfile.Combat).AllocatedPower,Is.EqualTo(1));
                    yield return null;yield return null;
                    slider=view.Q<LcarsPowerSlider>("slider-power-allocated-component.energy-bank.v1");
                    using(var e=KeyDownEvent.GetPooled('\0',KeyCode.DownArrow,EventModifiers.None)){e.target=slider.Track;slider.Track.SendEvent(e);}
                    Assert.That(draft.Current.Power("component.energy-bank.v1",DesignerProfile.Combat).AllocatedPower,Is.Zero);
                }
                finally{window.Close();}
            }
        }
        [UnityTest]
        public IEnumerator PowerBankWrapsAtNarrowStandardAndWideWidthsWithReachableExactControls()
        {
            var window=ScriptableObject.CreateInstance<UnityEditor.EditorWindow>();
            var draft=new ShipDesignDraft(ShipDesignFixtureLoader.Load());draft.RequestTemplate("class.escort");
            using(var view=new ShipDesignerView(draft,()=>{}))
            {
                try
                {
                    window.rootVisualElement.styleSheets.Add(Resources.Load<StyleSheet>("ShipDesigner"));
                    window.Show();window.rootVisualElement.Add(view);view.SelectSystem("Profiles");
                    foreach(var width in new[]{700,1200,1900})
                    {
                        view.style.width=width;view.style.height=800;yield return null;yield return null;
                        var bank=view.Q<VisualElement>("designer-power-bank");var rows=bank.Query<VisualElement>(className:"sd-profile-row").ToList();
                        Assert.That(rows.Count,Is.GreaterThan(2));
                        foreach(var row in rows)
                        {
                            Assert.That(row.worldBound.xMax,Is.LessThanOrEqualTo(bank.worldBound.xMax+1));
                            foreach(var slider in row.Query<LcarsPowerSlider>().ToList())
                            {
                                Assert.That(slider.Track.worldBound.width,Is.GreaterThanOrEqualTo(60));Assert.That(slider.Track.worldBound.height,Is.GreaterThanOrEqualTo(120));
                                Assert.That(slider.Exact.worldBound.width,Is.GreaterThan(55));
                                Assert.That(slider.Exact.worldBound.xMax,Is.LessThanOrEqualTo(row.worldBound.xMax));
                            }
                        }
                        Assert.That(rows.Last().worldBound.yMin,Is.GreaterThan(rows.First().worldBound.yMin),"Many installations must wrap into scrollable rows");
                    }
                }
                finally{window.Close();}
            }
        }
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
                using(var e=NavigationMoveEvent.GetPooled(NavigationMoveEvent.Direction.Right)){e.target=slider.Track;slider.Track.SendEvent(e);}
                Assert.That(stored,Is.EqualTo(8));Assert.That(commits,Is.EqualTo(2),"Synthesized navigation must not apply the same key twice");
                using(var e=KeyDownEvent.GetPooled('\0',KeyCode.RightArrow,EventModifiers.None)){e.target=slider.Track;slider.Track.SendEvent(e);}
                Assert.That(stored,Is.EqualTo(9));Assert.That(commits,Is.EqualTo(3));
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
