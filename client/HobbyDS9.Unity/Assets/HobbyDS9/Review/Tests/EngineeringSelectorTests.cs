#nullable enable
using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
namespace HobbyDS9.UnityReview.Tests
{
    public sealed class EngineeringSelectorTests
    {
        private UnityEditor.EditorWindow? window;
        [TearDown]public void Close(){ReviewLcarsSelector.CloseOpen();if(window!=null)window.Close();}
        private static void Submit(VisualElement target){using var e=NavigationSubmitEvent.GetPooled();e.target=target;target.SendEvent(e);}
        private static void Key(VisualElement target,KeyCode key){using var e=KeyDownEvent.GetPooled(new Event{type=EventType.KeyDown,keyCode=key});e.target=target;target.SendEvent(e);}
        [UnityTest]public IEnumerator LcarsMenusKeepIndependentSelectionFitEdgesAndDismissWithoutClickThrough()
        {
            if(!Environment.GetCommandLineArgs().Contains("--hobbyds9-engineering-menu-tests"))Assert.Ignore("QA-owned graphics/input receiving fixture.");
            window=ScriptableObject.CreateInstance<UnityEditor.EditorWindow>();window.position=new Rect(40,40,1280,720);window.Show();var root=window.rootVisualElement;root.AddToClassList("player-console");root.styleSheets.Add(Resources.Load<StyleSheet>("PlayerConsole"));
            int firstChanges=0,secondChanges=0,underlying=0;var options=new[]{"All technologies","Hulls & ship capabilities","Propulsion","Reactors","Sensors","Shields","Weapons"};
            var under=new Button(()=>underlying++){text="Background action"};root.Add(under);
            var first=new ReviewLcarsSelector("Technology",options,options[0],_=>firstChanges++,()=>true);first.style.width=300;root.Add(first);
            var second=new ReviewLcarsSelector("Technology",options,options[0],_=>secondChanges++,()=>true);second.style.width=300;root.Add(second);
            yield return null;yield return null;Submit(first.Q<Button>());yield return null;
            var overlay=root.Q("lcars-selector-overlay");Assert.That(overlay,Is.Not.Null);var menu=overlay.Q("lcars-selector-menu");
            Key(menu,KeyCode.End);Key(menu,KeyCode.Return);Assert.That(first.Value,Is.EqualTo("Weapons"));Assert.That(firstChanges,Is.EqualTo(1));Assert.That(secondChanges,Is.Zero);Assert.That(second.Value,Is.EqualTo("All technologies"));
            Assert.That(root.panel.focusController.focusedElement,Is.SameAs(first.Q<Button>()));
            foreach(var width in new[]{640f,1280f,2200f})
            {
                window.position=new Rect(40,40,width,720);root.style.width=width;first.style.alignSelf=Align.FlexEnd;Submit(first.Q<Button>());yield return null;yield return null;
                menu=root.Q("lcars-selector-menu");Assert.That(menu.worldBound.xMin,Is.GreaterThanOrEqualTo(root.worldBound.xMin));Assert.That(menu.worldBound.xMax,Is.LessThanOrEqualTo(root.worldBound.xMax+1));
                var scroll=menu.Q<ScrollView>();Assert.That(scroll.horizontalScrollerVisibility,Is.EqualTo(ScrollerVisibility.Hidden));
                foreach(var row in menu.Query<Button>().ToList())Assert.That(row.worldBound.width,Is.LessThanOrEqualTo(menu.worldBound.width));
                Key(menu,KeyCode.Home);Key(menu,KeyCode.DownArrow);Key(menu,KeyCode.UpArrow);Key(menu,KeyCode.Escape);Assert.That(first.Value,Is.EqualTo("Weapons"));
            }
            Submit(second.Q<Button>());yield return null;overlay=root.Q("lcars-selector-overlay");
            var point=under.worldBound.center;
            using(var e=PointerDownEvent.GetPooled(new Event{type=EventType.MouseDown,button=0,mousePosition=point})){e.target=overlay;overlay.SendEvent(e);}
            using(var e=PointerUpEvent.GetPooled(new Event{type=EventType.MouseUp,button=0,mousePosition=point})){e.target=overlay;overlay.SendEvent(e);}
            Assert.That(root.Q("lcars-selector-overlay"),Is.Null);Assert.That(underlying,Is.Zero);Assert.That(secondChanges,Is.Zero);
        }
    }
}
