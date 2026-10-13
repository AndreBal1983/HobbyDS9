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
        [TestCase(0,0)][TestCase(6,12)][TestCase(12,24)]
        public void VerticalFillCountsFromBottomWithoutChangingBackendValidity(long value,int filled)
        {
            var slider=new LcarsPowerSlider("Allocated","test",value,12,false,_=>{},4);
            Assert.That(slider.Query<SliderInt>().ToList(),Is.Empty);
            Assert.That(slider.Query<VisualElement>(className:"lps-filled").ToList().Count,Is.EqualTo(filled));
            for(var i=0;i<24;i++)Assert.That(slider.Q<VisualElement>("segment-"+i).ClassListContains("lps-filled"),Is.EqualTo(i<filled));
            Assert.That(slider.Q<VisualElement>("segment-0").ClassListContains("lps-band-low"),Is.True);
            Assert.That(slider.Q<VisualElement>("segment-23").ClassListContains("lps-band-high"),Is.True);
            Assert.That(slider.Track.tooltip,Does.Contain("Powered minimum 4"));
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
                    Assert.That(slider.Query<SliderInt>().ToList(),Is.Empty,"No hidden displaced slider or conventional thumb");
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
                                Assert.That(slider.Track.worldBound.width,Is.EqualTo(44).Within(1));Assert.That(slider.Track.worldBound.height,Is.EqualTo(slider.Scale.Count(slider.Maximum)*slider.Scale.SegmentPitch+14).Within(1));
                                Assert.That(row.worldBound.width,Is.InRange(120,180));
                                Assert.That(row.worldBound.xMax,Is.LessThanOrEqualTo(bank.worldBound.xMax+1));
                            }
                        }
                        if(width<1900)Assert.That(rows.Last().worldBound.yMin,Is.GreaterThan(rows.First().worldBound.yMin),"Many installations wrap when space runs out");
                        var detail=view.Q<ScrollView>("designer-power-detail");Assert.That(detail.Query<LongField>().ToList().Count,Is.EqualTo(2));
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
            try {window.Show();window.rootVisualElement.Add(slider);slider.Exact.value=100;Assert.That(slider.Value,Is.EqualTo(100));Assert.That(slider.Query<VisualElement>(className:"lps-filled").ToList().Count,Is.EqualTo(24));Assert.That(commits,Is.EqualTo(1));}
            finally {window.Close();}
        }
        [Test]
        public void NoDemandDisablesTrackAndExplainsIt()
        {
            var slider=new LcarsPowerSlider("Requested","test",0,0,false,_=>Assert.Fail());
            Assert.That(slider.Track.enabledSelf,Is.False);Assert.That(slider.PositionState,Is.EqualTo("disabled"));Assert.That(slider.Query<Label>().ToList().Any(l=>l.text.Contains("NO DEMAND")),Is.True);
        }
        [Test]
        public void BackendRangesUseActualStackAndProfilePreviewWithoutCampaignMutation()
        {
            var host=new CampaignHost(Resources.Load<TextAsset>("ds9-slice").bytes);var save=host.SaveJson();
            var provider=ShipDesignFixtureLoader.Load();var draft=new ShipDesignDraft(provider);draft.RequestTemplate("class.escort");draft.SelectProfile(DesignerProfile.Combat);
            using(var view=new ShipDesignerView(draft,()=>{}))
            {
                view.SelectSystem("Profiles");var power=provider.Evaluate(draft.Current).Profiles.Single(p=>p.Kind==OperatingProfileKind.Combat).Installations.Single(i=>i.InstallationId==draft.Current.Power("component.energy-bank.v1",DesignerProfile.Combat).InstallationId);
                var slider=view.Q<LcarsPowerSlider>("slider-power-allocated-component.energy-bank.v1");Assert.That(slider.Maximum,Is.EqualTo(power.FullPower));
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
        public void FullPositionIsNotErrorButBackendBudgetErrorsRemainVisible()
        {
            var healthy=new LcarsPowerSlider("Allocated","test",12,12,false,_=>{});Assert.That(healthy.PositionState,Is.EqualTo("high"));Assert.That(healthy.ClassListContains("lps-backend-invalid"),Is.False);
            var provider=ShipDesignFixtureLoader.Load();var draft=new ShipDesignDraft(provider);draft.RequestTemplate("class.escort");draft.SelectProfile(DesignerProfile.Combat);
            var preview=provider.Evaluate(draft.Current).Profiles.Single(p=>p.Kind==OperatingProfileKind.Combat);
            foreach(var i in preview.Installations){var id=draft.Current.ToDto().Installations.Single(x=>x.InstallationId==i.InstallationId).ComponentVersionId;draft.SetPower(id,i.FullPower,i.FullPower);}
            Assert.That(provider.Evaluate(draft.Current).Issues.Any(i=>i.Code==DesignIssueCode.AvailablePowerExceeded),Is.True);
            using(var view=new ShipDesignerView(draft,()=>{})){view.SelectSystem("Profiles");Assert.That(view.Query<Label>().ToList().Any(l=>l.text.Contains("BACKEND ERROR")),Is.True);}
        }
        private static void Key(VisualElement track,KeyCode code)
        {
            using(var e=KeyDownEvent.GetPooled('\0',code,EventModifiers.None)){e.target=track;track.SendEvent(e);}
        }
        private static void Pointer(VisualElement track,EventType type,Vector2 point)
        {
            var raw=new Event {type=type,mousePosition=point,button=0};
            if(type==EventType.MouseDown){using(var e=PointerDownEvent.GetPooled(raw)){e.target=track;track.SendEvent(e);}}
            else if(type==EventType.MouseUp){using(var e=PointerUpEvent.GetPooled(raw)){e.target=track;track.SendEvent(e);}}
            else {using(var e=PointerMoveEvent.GetPooled(raw)){e.target=track;track.SendEvent(e);}}
        }
        [TestCase(4,3,8,6)][TestCase(8,6,16,12)][TestCase(4,0,8,0)][TestCase(8,8,16,16)]
        public void AbsoluteCapacityAndFillHaveTwoBarsPerWholeUnit(long max,long value,int count,int filled)
        {
            var scale=new LcarsPowerScale(8);var slider=new LcarsPowerSlider("Allocated","test",value,max,false,_=>{},0,scale);
            Assert.That(slider.Query<VisualElement>(className:"lps-segment").ToList().Count,Is.EqualTo(count));
            Assert.That(slider.Query<VisualElement>(className:"lps-filled").ToList().Count,Is.EqualTo(filled));
            Assert.That(slider.Scale.Legend,Is.EqualTo("2 bars / power"));
        }
        [TestCase(25)][TestCase(1000)][TestCase(long.MaxValue)]
        public void ExtremeBankScaleIsSharedBoundedAndKeepsExactInt64(long max)
        {
            var scale=new LcarsPowerScale(max,192);
            var large=new LcarsPowerSlider("Requested","huge",long.MaxValue,max,false,_=>{},0,scale);
            var small=new LcarsPowerSlider("Allocated","small",3,4,false,_=>{},0,scale);
            Assert.That(large.Scale,Is.SameAs(small.Scale));Assert.That(scale.Paired,Is.False);
            Assert.That(scale.Count(max),Is.InRange(1,32));Assert.That(scale.Count(4),Is.InRange(1,32));
            Assert.That(scale.SegmentValue(scale.Count(max)-1,max),Is.EqualTo(max));
            Assert.That(large.Exact.value,Is.EqualTo(long.MaxValue));Assert.That(scale.Legend,Does.Contain("approximate"));
        }
        [UnityTest]
        public IEnumerator PhysicalSegmentClicksSnapPairsAndCapturedScrubCommitsOnce()
        {
            var window=ScriptableObject.CreateInstance<UnityEditor.EditorWindow>();var commits=0;long stored=0;
            var slider=new LcarsPowerSlider("Allocated","test",0,8,false,v=>{stored=v;commits++;});
            try
            {
                window.rootVisualElement.styleSheets.Add(Resources.Load<StyleSheet>("LcarsTokens"));
                window.rootVisualElement.styleSheets.Add(Resources.Load<StyleSheet>("ShipDesigner"));
                window.Show();window.rootVisualElement.Add(slider);yield return null;yield return null;
                var track=slider.Track;
                for(var i=0;i<16;i++)
                {
                    var point=slider.Q<VisualElement>("segment-"+i).worldBound.center;
                    var before=commits;Pointer(track,EventType.MouseDown,point);
                    Assert.That(slider.Value,Is.EqualTo(i/2+1),"Either physical half selects the same integer");
                    Assert.That(commits,Is.EqualTo(before));Assert.That(slider.Pending,Is.True);
                    Pointer(track,EventType.MouseUp,point);
                    Assert.That(stored,Is.EqualTo(i/2+1));Assert.That(slider.Pending,Is.False);
                    Assert.That(commits-before,Is.EqualTo(i%2==0?1:0));
                }
                var start=slider.Q<VisualElement>("segment-0").worldBound.center;
                var end=slider.Q<VisualElement>("segment-11").worldBound.center;
                var n=commits;Pointer(track,EventType.MouseDown,start);Pointer(track,EventType.MouseDrag,end);
                Assert.That(slider.Value,Is.EqualTo(6));Assert.That(commits,Is.EqualTo(n));
                Assert.That(track.HasPointerCapture(PointerId.mousePointerId),Is.True);
                Pointer(track,EventType.MouseUp,end);Assert.That(commits,Is.EqualTo(n+1));Assert.That(stored,Is.EqualTo(6));
                Key(track,KeyCode.Home);Key(track,KeyCode.RightArrow);Key(track,KeyCode.UpArrow);
                Assert.That(stored,Is.EqualTo(2));
                using(var e=NavigationMoveEvent.GetPooled(NavigationMoveEvent.Direction.Up)){e.target=track;track.SendEvent(e);}
                Assert.That(stored,Is.EqualTo(2));Key(track,KeyCode.DownArrow);Assert.That(stored,Is.EqualTo(1));
                Key(track,KeyCode.End);Assert.That(stored,Is.EqualTo(8));
                var zero=new Vector2(track.worldBound.center.x,track.worldBound.yMax-3);
                Pointer(track,EventType.MouseDown,zero);Pointer(track,EventType.MouseUp,zero);Assert.That(stored,Is.Zero);
            }
            finally{window.Close();}
        }
        [UnityTest]
        public IEnumerator CaptureLossCommitsButDetachCancelsProspectiveDraftValue()
        {
            var window=ScriptableObject.CreateInstance<UnityEditor.EditorWindow>();var commits=0;
            var slider=new LcarsPowerSlider("Allocated","test",0,8,false,_=>commits++);
            try
            {
                window.rootVisualElement.styleSheets.Add(Resources.Load<StyleSheet>("LcarsTokens"));
                window.rootVisualElement.styleSheets.Add(Resources.Load<StyleSheet>("ShipDesigner"));
                window.Show();window.rootVisualElement.Add(slider);yield return null;yield return null;
                var point=slider.Q<VisualElement>("segment-5").worldBound.center;
                Pointer(slider.Track,EventType.MouseDown,point);yield return null;
                Assert.That(slider.Value,Is.EqualTo(3),"Capture starts with a physical bar preview");
                Assert.That(slider.Track.HasPointerCapture(PointerId.mousePointerId),Is.True);
                slider.Track.ReleasePointer(PointerId.mousePointerId);
                // Batch Editor tests have no OS pointer dispatch loop. Deliver the
                // public capture-loss event, then allow any queued duplicate.
                using(var e=PointerCaptureOutEvent.GetPooled(slider.Track,null,PointerId.mousePointerId))slider.Track.SendEvent(e);
                yield return null;
                Assert.That(commits,Is.EqualTo(1));Assert.That(slider.Value,Is.EqualTo(3));Assert.That(slider.Pending,Is.False);
                Pointer(slider.Track,EventType.MouseDown,slider.Q<VisualElement>("segment-9").worldBound.center);
                slider.RemoveFromHierarchy();yield return null;
                Assert.That(commits,Is.EqualTo(1));Assert.That(slider.Value,Is.EqualTo(3));Assert.That(slider.Pending,Is.False);
            }
            finally{window.Close();}
        }
        [Test]
        public void ExtremeExactEntryAndKeyboardDoNotOverflowOrChangeLogicalStep()
        {
            var window=ScriptableObject.CreateInstance<UnityEditor.EditorWindow>();long stored=0;
            var slider=new LcarsPowerSlider("Allocated","test",0,long.MaxValue,false,v=>stored=v);
            try
            {
                window.Show();window.rootVisualElement.Add(slider);Key(slider.Track,KeyCode.End);Key(slider.Track,KeyCode.UpArrow);
                Assert.That(stored,Is.EqualTo(long.MaxValue));Key(slider.Track,KeyCode.DownArrow);Assert.That(stored,Is.EqualTo(long.MaxValue-1));
                slider.Exact.value=long.MinValue;Assert.That(stored,Is.EqualTo(long.MinValue));Assert.That(slider.Exact.value,Is.EqualTo(long.MinValue));Assert.That(slider.PositionState,Is.EqualTo("invalid"));
            }
            finally{window.Close();}
        }
        [UnityTest]
        public IEnumerator SelectingDetailDuringPointerFocusDoesNotChangeScrubCoordinates()
        {
            var window=ScriptableObject.CreateInstance<UnityEditor.EditorWindow>();var draft=new ShipDesignDraft(ShipDesignFixtureLoader.Load());draft.RequestTemplate("class.escort");draft.SelectProfile(DesignerProfile.Combat);
            using(var view=new ShipDesignerView(draft,()=>{}))
            {
                try
                {
                    foreach(var resource in new[]{"LcarsTokens","ShipDesigner"})window.rootVisualElement.styleSheets.Add(Resources.Load<StyleSheet>(resource));
                    window.Show();view.style.minWidth=1700;view.style.minHeight=800;window.rootVisualElement.Add(view);view.SelectSystem("Profiles");yield return null;yield return null;
                    var slider=view.Q<LcarsPowerSlider>("slider-power-allocated-component.energy-bank.v1");var point=slider.Q<VisualElement>("segment-15").worldBound.center;
                    var changes=0;draft.Changed+=()=>changes++;
                    Pointer(slider.Track,EventType.MouseDown,point);Assert.That(slider.Value,Is.EqualTo(8));Assert.That(changes,Is.Zero);
                    Assert.That(view.Q<ScrollView>("designer-power-detail").Q<LongField>("power-allocated-component.energy-bank.v1"),Is.SameAs(slider.Exact));
                    var zero=new Vector2(slider.Track.worldBound.center.x,slider.Track.worldBound.yMax-3);
                    Pointer(slider.Track,EventType.MouseDrag,zero);Assert.That(slider.Value,Is.Zero);Assert.That(changes,Is.Zero);
                    Pointer(slider.Track,EventType.MouseUp,zero);Assert.That(draft.Current.Power("component.energy-bank.v1",DesignerProfile.Combat).AllocatedPower,Is.Zero);Assert.That(changes,Is.EqualTo(1));
                    yield return null;yield return null;
                    var allocated=view.Q<LcarsPowerSlider>("slider-power-allocated-component.energy-bank.v1");
                    var requested=view.Q<LcarsPowerSlider>("slider-power-requested-component.energy-bank.v1");
                    Assert.That(allocated.resolvedStyle.position,Is.EqualTo(Position.Relative),"Zero state must not inherit the zero strip's absolute positioning");
                    Assert.That(allocated.Track.worldBound.yMax,Is.EqualTo(requested.Track.worldBound.yMax).Within(1));
                    Assert.That(allocated.Track.worldBound.xMin,Is.GreaterThan(requested.Track.worldBound.xMax));
                }
                finally{window.Close();}
            }
        }
        [UnityTest]
        public IEnumerator FrameJoinMatchesRailAndBarAfterResizeAndScroll()
        {
            var window=ScriptableObject.CreateInstance<UnityEditor.EditorWindow>();var draft=new ShipDesignDraft(ShipDesignFixtureLoader.Load());
            using(var view=new ShipDesignerView(draft,()=>{}))
            {
                var shell=new LcarsShell(new GalaxyMap(),new ProductionOverviewView(),_=>{},()=>{},()=>{});shell.AttachDesigner(view);shell.SelectSection("Designer");
                try
                {
                    foreach(var resource in new[]{"LcarsTokens","Lcars","ShipDesigner"})window.rootVisualElement.styleSheets.Add(Resources.Load<StyleSheet>(resource));
                    window.Show();window.rootVisualElement.Add(shell);view.SelectSystem("Profiles");
                    foreach(var size in new[]{new Vector2(1920,1080),new Vector2(3440,1440),new Vector2(1280,720),new Vector2(1600,900)})
                    {
                        shell.style.width=size.x;shell.style.height=size.y;shell.style.minWidth=size.x;shell.style.minHeight=size.y;yield return null;yield return null;
                        Assert.That(shell.worldBound.height,Is.EqualTo(size.y).Within(1),"Test the requested logical viewport, not the Editor window's smaller default size");
                        var elbow=shell.Q<LcarsElbow>("lcars-shell-elbow");var frame=shell.Q<VisualElement>("lcars-frame-bar");var before=frame.worldBound;
                        if(!shell.Plan.HorizontalNavigation)
                        {
                            Assert.That(elbow.worldBound.yMax,Is.EqualTo(frame.worldBound.yMax).Within(1));
                            Assert.That(shell.Navigation.worldBound.yMin,Is.EqualTo(frame.worldBound.yMax).Within(1));
                            Assert.That(shell.Navigation.worldBound.width,Is.EqualTo(180).Within(1));
                            Assert.That(frame.Q<Label>().worldBound.height,Is.EqualTo(30).Within(1));
                            Assert.That(frame.Q<Label>().worldBound.yMin,Is.EqualTo(frame.worldBound.yMin).Within(1));
                            Assert.That(frame.Q<Label>().worldBound.xMin,Is.EqualTo(elbow.worldBound.xMax).Within(1));
                        }
                        view.Q<ScrollView>("designer-profile-results").scrollOffset=new Vector2(0,100);
                        yield return null;Assert.That(frame.worldBound,Is.EqualTo(before),"Inner scroll must not move the frame");
                        Assert.That(view.Q<ScrollView>("designer-profile-results").contentViewport.worldBound.height,Is.GreaterThanOrEqualTo(120),"Exact editor must not squeeze out the bank");
                    }
                }
                finally{window.Close();}
            }
        }
    }
}
