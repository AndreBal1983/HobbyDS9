#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HobbyDS9.Contracts;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace HobbyDS9.UnityReview.Tests
{
    // QA-owned synthetic UI-event fixture. Authoring/compilation is not physical input acceptance.
    public sealed class ResearchAutoApplyControlTests
    {
        private UnityEditor.EditorWindow? window;
        private VisualElement Host()
        {
            if(!Environment.GetCommandLineArgs().Contains("--hobbyds9-research-control-tests"))Assert.Ignore("Dedicated QA research-control run required.");
            window=ScriptableObject.CreateInstance<UnityEditor.EditorWindow>();window.titleContent=new GUIContent("Research controls fixture");window.position=new Rect(80,80,1100,800);window.Show();return window.rootVisualElement;
        }
        [TearDown]public void Cleanup(){if(window!=null)window.Close();}
        private static void Pointer(VisualElement target,EventType type,Vector2 position)
        {
            var input=new Event{type=type,button=0,mousePosition=position};
            if(type==EventType.MouseDown){using var e=PointerDownEvent.GetPooled(input);e.target=target;target.SendEvent(e);}
            else if(type==EventType.MouseUp){using var e=PointerUpEvent.GetPooled(input);e.target=target;target.SendEvent(e);}
            else {using var e=PointerMoveEvent.GetPooled(input);e.target=target;target.SendEvent(e);}
        }
        private static void Key(VisualElement target,KeyCode code)
        {using var e=KeyDownEvent.GetPooled(new Event{type=EventType.KeyDown,keyCode=code});e.target=target;target.SendEvent(e);}
        [UnityTest]public IEnumerator DragPreviewsUntilReleaseEscapeCancelsAndRapidKeysKeepTheirLastValue()
        {
            var host=Host();var committed=new List<int>();var previews=new List<int>();
            var bar=new ReviewAllocationBar("fixture-allocation",120,Color.cyan,committed.Add,previews.Add);bar.style.width=600;bar.style.height=50;host.Add(bar);yield return null;yield return null;
            var area=bar.worldBound;var start=new Vector2(area.x+area.width*.2f,area.center.y);
            Pointer(bar,EventType.MouseDown,start);
            for(int i=0;i<50;i++)Pointer(bar,EventType.MouseDrag,new Vector2(area.x+area.width*(.2f+i*.01f),area.center.y));
            Assert.That(committed,Is.Empty);Assert.That(previews.Count,Is.GreaterThan(30));
            Pointer(bar,EventType.MouseUp,new Vector2(area.x+area.width*.8f,area.center.y));Assert.That(committed,Is.EqualTo(new[]{480}));
            Key(bar,KeyCode.RightArrow);Key(bar,KeyCode.RightArrow);Assert.That(committed,Is.EqualTo(new[]{480,486,492}));
            Pointer(bar,EventType.MouseDown,start);Pointer(bar,EventType.MouseDrag,new Vector2(area.x+area.width*.4f,area.center.y));Key(bar,KeyCode.Escape);
            Pointer(bar,EventType.MouseUp,start);Assert.That(committed.Count,Is.EqualTo(3));
        }
        [UnityTest]public IEnumerator RealResearchControlsCoalesceKeysAndCommitSplitOnlyOnRelease()
        {
            var host=Host();byte[] Bytes(string name)=>Resources.Load<TextAsset>(PlayableFleetOptions.ResourceRoot+name).bytes;
            var codec=new ReviewNewtonsoftCodec();var package=ReviewExactPackageLoader.LoadPlayableFleet(Bytes("playable"),Bytes("package"),Bytes("physical"),Bytes("core"),Bytes("scenario"),codec);
            var model=new ReviewUiModel(package,codec,new ReviewPlayableFleetSlotStore(Path.Combine(Path.GetTempPath(),"HobbyDS9-ResearchControls-"+Guid.NewGuid().ToString("N")),codec));
            var workspace=new ReviewWorkspace(new ReviewCombatFlow(model));workspace.Navigate(ReviewWorkspacePage.Research);
            ReviewPlayerView? player=null;
            player=new ReviewPlayerView(model,workspace,(p,t,a)=>{var b=new Button(a){text=t};p.Add(b);return b;},a=>{a();player!.Render(host,ReviewWorkspacePage.Research);},(_,__)=>Assert.Fail("Allocations must not confirm"),_=>{},(_,__,___)=>{},(_,__)=>{});
            player.Render(host,ReviewWorkspacePage.Research);yield return null;yield return null;
            var frame=model.Current.Frame;var before=model.ResearchInputs()!.Weight(ScienceDiscipline.Construction);var bar=host.Q<ReviewAllocationBar>("science-bar-Construction");bar.Focus();
            Key(bar,KeyCode.RightArrow);Key(bar,KeyCode.RightArrow);Key(bar,KeyCode.RightArrow);
            Assert.That(model.Current.Frame,Is.SameAs(frame));Assert.That(model.ResearchCommitPending,Is.True);
            yield return new WaitForSecondsRealtime(.06f);yield return null;
            Assert.That(model.Current.Frame.Status.CampaignRevision,Is.EqualTo(frame.Status.CampaignRevision+1));Assert.That(model.ResearchInputs()!.Weight(ScienceDiscipline.Construction),Is.EqualTo(before+18));
            Assert.That(host.Q("integrated-apply-science"),Is.Null);Assert.That(host.Q("integrated-apply-split"),Is.Null);Assert.That(host.Q("science-restore"),Is.Null);
            frame=model.Current.Frame;var split=host.Q<SliderInt>("integrated-research-split");var point=split.worldBound.center;
            Pointer(split,EventType.MouseDown,point);split.value=5;split.value=8;Assert.That(model.ResearchCommitPending,Is.False);Assert.That(model.Current.Frame,Is.SameAs(frame));
            Pointer(split,EventType.MouseUp,point);Assert.That(model.ResearchCommitPending,Is.True);yield return new WaitForSecondsRealtime(.06f);yield return null;
            Assert.That(model.Current.Frame.Strategy!.Research.ScienceAllocationUnits,Is.EqualTo(240));Assert.That(model.HasPlanningEdits,Is.False);
        }
    }
}
