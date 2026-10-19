#nullable enable
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using HobbyDS9.Contracts;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace HobbyDS9.UnityReview.Tests
{
    // Direct UI-event control tests, NOT physical keyboard/provider acceptance.
    public sealed class DemoKeyboardControlTests
    {
        private static void RequireRun()
        {if(Array.IndexOf(Environment.GetCommandLineArgs(),"--hobbyds9-demo-render-tests")<0)Assert.Ignore("Dedicated demo control fixture required.");}
        private static object? Invoke(ReviewEntry entry,string method,params object[] args)=>typeof(ReviewEntry).GetMethod(method,BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(entry,args);
        private static Button Add(ReviewEntry entry,VisualElement root,Action action)=>
            (Button)Invoke(entry,"AddButton",root,"Keyboard fixture",action,false)!;
        private static void Key(VisualElement element,KeyCode key,bool up=false,EventModifiers modifiers=EventModifiers.None)
        {
            var systemEvent=new Event{type=up?EventType.KeyUp:EventType.KeyDown,keyCode=key,modifiers=modifiers};
            if(up){using(var evt=KeyUpEvent.GetPooled(systemEvent)){evt.target=element;element.SendEvent(evt);}}
            else{using(var evt=KeyDownEvent.GetPooled(systemEvent)){evt.target=element;element.SendEvent(evt);}}
        }
        private static Button Confirm(VisualElement root)=>root.Q("review-one-use-confirmation").Query<Button>().ToList().Single(b=>b.text=="Confirm");

        [UnityTest] public IEnumerator FocusedButtonKeysRespectDisabledDetachedModifiersAndHeldKeys()
        {
            RequireRun();yield return new EnterPlayMode();yield return null;yield return null;
            var entry=UnityEngine.Object.FindAnyObjectByType<ReviewEntry>();var root=entry.GetComponent<UIDocument>().rootVisualElement;
            var frame=entry.Presentation!.Model.Current.Frame;int count=0;var button=Add(entry,root,()=>count++);var other=Add(entry,root,()=>{});
            foreach(var key in new[]{KeyCode.Return,KeyCode.KeypadEnter,KeyCode.Space})
            {
                button.Focus();int before=count;Key(button,key);Key(button,key);Assert.That(count,Is.EqualTo(before+1),"one activation per held key");
                Key(button,key,true);Key(button,key);Assert.That(count,Is.EqualTo(before+2));Key(button,key,true);
            }
            int accepted=count;button.Focus();Key(button,KeyCode.Return,false,EventModifiers.Control);Key(button,KeyCode.Return,true);
            other.Focus();Key(button,KeyCode.Return);Key(other,KeyCode.Return,true);
            button.SetEnabled(false);Key(button,KeyCode.Return);Key(other,KeyCode.Return,true);
            button.SetEnabled(true);button.Focus();button.RemoveFromHierarchy();Key(button,KeyCode.Return);
            Assert.That(count,Is.EqualTo(accepted));Assert.That(entry.Presentation.Model.Current.Frame,Is.SameAs(frame));
            yield return new ExitPlayMode();
        }

        [UnityTest] public IEnumerator HeldEnterCannotConfirmNewModalOrInvokeObsoleteControl()
        {
            RequireRun();yield return new EnterPlayMode();yield return null;yield return null;
            var entry=UnityEngine.Object.FindAnyObjectByType<ReviewEntry>();var root=entry.GetComponent<UIDocument>().rootVisualElement;
            int first=0,second=0;var opener=Add(entry,root,()=>Invoke(entry,"Confirm","First fixture",(Action)(()=>{
                first++;Invoke(entry,"Confirm","Second fixture",(Action)(()=>second++));
            })));
            opener.Focus();Key(opener,KeyCode.Return);var confirm=Confirm(root);Assert.That(confirm,Is.Not.Null);
            Key(confirm,KeyCode.Return);Assert.That(first,Is.Zero,"opening key cannot confirm modal");
            Key(confirm,KeyCode.Return,true);Key(confirm,KeyCode.Return);Assert.That(first,Is.EqualTo(1));
            var next=Confirm(root);Key(next,KeyCode.Return);Assert.That(second,Is.Zero,"confirming key cannot submit next modal");
            Key(next,KeyCode.Return,true);Key(next,KeyCode.Return);Assert.That(second,Is.EqualTo(1));Key(root,KeyCode.Return,true);
            Invoke(entry,"Build");Key(opener,KeyCode.Return);Assert.That(root.Q("review-one-use-confirmation"),Is.Null,"detached old epoch cannot open a modal");
            yield return new ExitPlayMode();
        }

        [UnityTest] public IEnumerator TabUsesLocalFocusRingAndLeavesFieldPopupAndRawInputOwnedKeysAlone()
        {
            RequireRun();yield return new EnterPlayMode();yield return null;yield return null;
            var entry=UnityEngine.Object.FindAnyObjectByType<ReviewEntry>();var root=entry.GetComponent<UIDocument>().rootVisualElement;
            var a=Add(entry,root,()=>{});var b=Add(entry,root,()=>{});int moves=0;
            root.RegisterCallback<NavigationMoveEvent>(_=>moves++,TrickleDown.TrickleDown);
            a.Focus();Key(a,KeyCode.Tab);Assert.That(root.panel.focusController.focusedElement,Is.SameAs(b));
            Key(b,KeyCode.Tab,false,EventModifiers.Shift);Assert.That(root.panel.focusController.focusedElement,Is.SameAs(a));Assert.That(moves,Is.EqualTo(2));
            var field=new TextField();root.Add(field);field.Focus();var fieldTarget=(VisualElement)root.panel.focusController.focusedElement;
            Key(fieldTarget,KeyCode.Tab);Assert.That(moves,Is.EqualTo(2),"field keys are not translated");
            var popup=new DropdownField("Fixture",new System.Collections.Generic.List<string>{"One","Two"},0);root.Add(popup);popup.Focus();
            Key((VisualElement)root.panel.focusController.focusedElement,KeyCode.Tab);Assert.That(moves,Is.EqualTo(2),"popup keys are not translated");
            var keyboard=InputSystem.AddDevice<Keyboard>("DemoControlTab");
            var priorRouting=InputSystem.settings.editorInputBehaviorInPlayMode;
            var priorBackground=InputSystem.settings.backgroundBehavior;var priorRun=UnityEngine.Application.runInBackground;
            try
            {
                UnityEngine.Application.runInBackground=true;InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
                InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
                a.Focus();InputSystem.QueueStateEvent(keyboard,new KeyboardState(UnityEngine.InputSystem.Key.Tab));InputSystem.Update();
                Assert.That(keyboard.tabKey.wasPressedThisFrame,Is.True);Key(a,KeyCode.Tab);Assert.That(moves,Is.EqualTo(2),"raw-input Tab owns navigation");
            }
            finally
            {
                InputSystem.QueueStateEvent(keyboard,new KeyboardState());InputSystem.Update();InputSystem.RemoveDevice(keyboard);
                InputSystem.settings.editorInputBehaviorInPlayMode=priorRouting;InputSystem.settings.backgroundBehavior=priorBackground;UnityEngine.Application.runInBackground=priorRun;
            }
            yield return new ExitPlayMode();
        }

        [UnityTest] public IEnumerator AlreadyPerformedSubmitLeavesActivationToStandardNavigationOnly()
        {
            RequireRun();yield return new EnterPlayMode();yield return null;yield return null;
            var entry=UnityEngine.Object.FindAnyObjectByType<ReviewEntry>();var root=entry.GetComponent<UIDocument>().rootVisualElement;
            var submit=InputSystem.actions.FindAction("UI/Submit",true);var map=submit.actionMap;
            var priorDevices=map.devices;var priorEnabled=submit.enabled;var priorRouting=InputSystem.settings.editorInputBehaviorInPlayMode;
            var priorBackground=InputSystem.settings.backgroundBehavior;var priorRun=UnityEngine.Application.runInBackground;
            var keyboard=InputSystem.AddDevice<Keyboard>("DemoControlSubmit");int count=0;
            var button=Add(entry,root,()=>{count++;Invoke(entry,"Confirm","Action-owned press fixture",(Action)(()=>count++));});
            try
            {
                UnityEngine.Application.runInBackground=true;InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
                InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
                map.devices=new InputDevice[]{keyboard};if(!priorEnabled)submit.Enable();button.Focus();
                InputSystem.QueueStateEvent(keyboard,new KeyboardState(UnityEngine.InputSystem.Key.Enter));InputSystem.Update();
                Assert.That(submit.WasPerformedThisFrame(),Is.True,"actual action signal, not a mock flag");
                Key(button,KeyCode.Return);Assert.That(count,Is.Zero,"KeyDown precedes queued navigation and must not activate twice");
                // Explicitly arranged normal UI route; this is not a claim the
                // hidden Editor/native provider automatically dispatched it.
                using(var evt=NavigationSubmitEvent.GetPooled()){evt.target=button;button.SendEvent(evt);}
                Assert.That(count,Is.EqualTo(1));var next=Confirm(root);
                Assert.That(root.panel.focusController.focusedElement,Is.SameAs(next));
                // Advance the real action frame, but deliberately do not send
                // UI KeyUp: an OS repeat must not become a second activation.
                InputSystem.QueueStateEvent(keyboard,new KeyboardState());InputSystem.Update();
                Assert.That(submit.WasPerformedThisFrame(),Is.False);
                Key(next,KeyCode.Return);Assert.That(count,Is.EqualTo(1),"real-action press must not confirm the newly opened modal on OS repeat");
                Assert.That(root.Q("review-one-use-confirmation"),Is.Not.Null);
                Key(next,KeyCode.Return,true);Key(next,KeyCode.Return);Assert.That(count,Is.EqualTo(2),"UI KeyUp permits a distinct new press");
                Assert.That(root.Q("review-one-use-confirmation"),Is.Null);Key(root,KeyCode.Return,true);
            }
            finally
            {
                button.RemoveFromHierarchy();
                InputSystem.QueueStateEvent(keyboard,new KeyboardState());InputSystem.Update();
                if(!priorEnabled)submit.Disable();map.devices=priorDevices;
                InputSystem.settings.editorInputBehaviorInPlayMode=priorRouting;InputSystem.RemoveDevice(keyboard);
                InputSystem.settings.backgroundBehavior=priorBackground;UnityEngine.Application.runInBackground=priorRun;
            }
            yield return new ExitPlayMode();
        }

        [UnityTest] public IEnumerator InitialMenuFocusIsAttachedPrimaryAndStaleScheduledFocusCannotStealModal()
        {
            RequireRun();yield return new EnterPlayMode();yield return null;yield return null;
            var entry=UnityEngine.Object.FindAnyObjectByType<ReviewEntry>();var root=entry.GetComponent<UIDocument>().rootVisualElement;
            var start=root.Q<Button>("demo-start");Assert.That(root.panel.focusController.focusedElement,Is.SameAs(start));
            Invoke(entry,"ScheduleDemoMenuFocus",start);Invoke(entry,"Confirm","Focus fixture",(Action)(()=>{}));yield return null;
            Assert.That(root.panel.focusController.focusedElement,Is.SameAs(Confirm(root)),"scheduled menu focus cannot escape current modal");
            yield return new ExitPlayMode();
        }

        [UnityTest] public IEnumerator PanelRootReleaseAfterActualStartRebuildAllowsLaterModalEnter()
        {
            RequireRun();yield return new EnterPlayMode();yield return null;yield return null;
            var entry=UnityEngine.Object.FindAnyObjectByType<ReviewEntry>();var doc=entry.GetComponent<UIDocument>();
            var start=doc.rootVisualElement.Q<Button>("demo-start");start.Focus();
            var oldPanelRoot=doc.rootVisualElement.panel.visualTree;
            Key(start,KeyCode.Return);yield return null;
            var root=doc.rootVisualElement;var panelRoot=root.panel.visualTree;var frame=entry.Presentation!.Model.Current.Frame;
            Assert.That(panelRoot,Is.Not.SameAs(oldPanelRoot),"Actual Start replaces the panel");
            Assert.That(root.Q("demo-start"),Is.Null);Assert.That(root.Q("review-nav-Production"),Is.Not.Null);
            Assert.That(root.panel.focusController.focusedElement,Is.Null,"Start leaves no focused child to receive release");
            // Arrange the public UI event route used when no element is focused.
            // This does not establish that OS/provider delivery emitted KeyUp.
            Key(panelRoot,KeyCode.Return,true);
            int accepted=0;Invoke(entry,"Confirm","Later purchase fixture",(Action)(()=>accepted++));
            var confirm=Confirm(root);Assert.That(root.panel.focusController.focusedElement,Is.SameAs(confirm));
            Key(confirm,KeyCode.Return);Assert.That(accepted,Is.EqualTo(1),"A distinct released press can confirm the later modal");
            Assert.That(root.Q("review-one-use-confirmation"),Is.Null);Assert.That(entry.Presentation.Model.Current.Frame,Is.SameAs(frame));
            Key(panelRoot,KeyCode.Return,true);yield return new ExitPlayMode();
        }

        [UnityTest] public IEnumerator PanelReleaseBindingRejectsOldDetachedAndRepeatedBindingsWithoutClearingHeldPress()
        {
            RequireRun();yield return new EnterPlayMode();yield return null;yield return null;
            var entry=UnityEngine.Object.FindAnyObjectByType<ReviewEntry>();var doc=entry.GetComponent<UIDocument>();
            var oldPanelRoot=doc.rootVisualElement.panel.visualTree;var start=doc.rootVisualElement.Q<Button>("demo-start");start.Focus();
            Key(start,KeyCode.Return);yield return null;
            var root=doc.rootVisualElement;var panelRoot=root.panel.visualTree;
            Assert.That(panelRoot,Is.Not.SameAs(oldPanelRoot));
            Invoke(entry,"BindDemoKeyboard");Invoke(entry,"BindDemoKeyboard");Invoke(entry,"BindDemoKeyboard");
            Key(oldPanelRoot,KeyCode.Return,true);
            int accepted=0;Invoke(entry,"Confirm","Held through replacement fixture",(Action)(()=>accepted++));var confirm=Confirm(root);
            Key(confirm,KeyCode.Return);Assert.That(accepted,Is.Zero,"A release on an obsolete panel must not clear the current held press");
            Invoke(entry,"UnbindDemoKeyboard");Key(panelRoot,KeyCode.Return,true);Invoke(entry,"BindDemoKeyboard");
            Key(confirm,KeyCode.Return);Assert.That(accepted,Is.Zero,"Repeated binds must not leave a release callback after unbind");
            var parent=root.parent;root.RemoveFromHierarchy();Key(panelRoot,KeyCode.Return,true);parent.Add(root);yield return null;
            confirm.Focus();Key(confirm,KeyCode.Return);Assert.That(accepted,Is.Zero,"Detached document release cannot clear its held press");
            Assert.That(root.Q("review-one-use-confirmation"),Is.Not.Null);
            Key(panelRoot,KeyCode.Return,true);Key(confirm,KeyCode.Return);Assert.That(accepted,Is.EqualTo(1),"Reattachment observes release on its current panel");
            Key(panelRoot,KeyCode.Return,true);yield return new ExitPlayMode();
        }

        [UnityTest] public IEnumerator UnfocusedPanelTabAfterActualEndTurnConfirmationUsesNormalForwardAndReverseRing()
        {
            RequireRun();yield return new EnterPlayMode();yield return null;yield return null;
            var entry=UnityEngine.Object.FindAnyObjectByType<ReviewEntry>();var doc=entry.GetComponent<UIDocument>();
            var start=doc.rootVisualElement.Q<Button>("demo-start");
            using(var e=NavigationSubmitEvent.GetPooled()){e.target=start;start.SendEvent(e);}yield return null;
            var model=entry.Presentation!.Model;var before=model.Current.Frame;var oldPanelRoot=doc.rootVisualElement.panel.visualTree;
            var endTurn=doc.rootVisualElement.Q<Button>("player-primary-action");
            using(var e=NavigationSubmitEvent.GetPooled()){e.target=endTurn;endTurn.SendEvent(e);}
            var confirm=Confirm(doc.rootVisualElement);Assert.That(doc.rootVisualElement.panel.focusController.focusedElement,Is.SameAs(confirm));
            Key(confirm,KeyCode.Return);yield return null;
            var root=doc.rootVisualElement;var panelRoot=root.panel.visualTree;var accepted=model.Current.Frame;
            Assert.That(accepted.Status.Turn,Is.EqualTo(before.Status.Turn+1));Assert.That(panelRoot,Is.Not.SameAs(oldPanelRoot));
            Assert.That(root.panel.focusController.focusedElement,Is.Null,"Accepted confirmation rebuild leaves no focused child");
            Key(panelRoot,KeyCode.Return,true);int moves=0;panelRoot.RegisterCallback<NavigationMoveEvent>(_=>moves++,TrickleDown.TrickleDown);
            Key(panelRoot,KeyCode.Tab);
            // Menu is the first eligible control in the existing navigation;
            // End turn is the final enabled footer control. Product code does
            // not select either: normal runtime focus-ring events must do so.
            Assert.That(root.panel.focusController.focusedElement,Is.SameAs(root.Q<Button>("demo-menu")));Assert.That(moves,Is.EqualTo(1));
            Key(root.Q<Button>("demo-menu"),KeyCode.Tab,false,EventModifiers.Shift);
            endTurn=root.Q<Button>("player-primary-action");Assert.That(root.panel.focusController.focusedElement,Is.SameAs(endTurn));Assert.That(moves,Is.EqualTo(2));
            endTurn.Blur();Assert.That(root.panel.focusController.focusedElement,Is.Null);
            Key(panelRoot,KeyCode.Tab,false,EventModifiers.Shift);Assert.That(root.panel.focusController.focusedElement,Is.SameAs(endTurn));Assert.That(moves,Is.EqualTo(3));
            Assert.That(model.Current.Frame,Is.SameAs(accepted));Assert.That(model.HasLocalEdits,Is.False);Assert.That(root.Q("review-one-use-confirmation"),Is.Null);
            yield return new ExitPlayMode();
        }

        [UnityTest] public IEnumerator UnfocusedPanelTabDefersModifiersRawInputOldPanelAndDetachedDocument()
        {
            RequireRun();yield return new EnterPlayMode();yield return null;yield return null;
            var entry=UnityEngine.Object.FindAnyObjectByType<ReviewEntry>();var doc=entry.GetComponent<UIDocument>();var oldPanelRoot=doc.rootVisualElement.panel.visualTree;
            var start=doc.rootVisualElement.Q<Button>("demo-start");using(var e=NavigationSubmitEvent.GetPooled()){e.target=start;start.SendEvent(e);}yield return null;
            var root=doc.rootVisualElement;var panelRoot=root.panel.visualTree;var frame=entry.Presentation!.Model.Current.Frame;
            Assert.That(root.panel.focusController.focusedElement,Is.Null);int moves=0;panelRoot.RegisterCallback<NavigationMoveEvent>(_=>moves++,TrickleDown.TrickleDown);
            foreach(var modifiers in new[]{EventModifiers.Control,EventModifiers.Alt,EventModifiers.Command})Key(panelRoot,KeyCode.Tab,false,modifiers);
            Key(oldPanelRoot,KeyCode.Tab);Assert.That(moves,Is.Zero);Assert.That(root.panel.focusController.focusedElement,Is.Null);
            var keyboard=InputSystem.AddDevice<Keyboard>("DemoUnfocusedTab");var priorRouting=InputSystem.settings.editorInputBehaviorInPlayMode;
            var priorBackground=InputSystem.settings.backgroundBehavior;var priorRun=UnityEngine.Application.runInBackground;
            try
            {
                UnityEngine.Application.runInBackground=true;InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
                InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
                InputSystem.QueueStateEvent(keyboard,new KeyboardState(UnityEngine.InputSystem.Key.Tab));InputSystem.Update();Assert.That(keyboard.tabKey.wasPressedThisFrame,Is.True);
                Key(panelRoot,KeyCode.Tab);Assert.That(moves,Is.Zero,"Raw Tab owns navigation even without a focused child");
                Assert.That(root.panel.focusController.focusedElement,Is.Null);
            }
            finally
            {
                InputSystem.QueueStateEvent(keyboard,new KeyboardState());InputSystem.Update();InputSystem.RemoveDevice(keyboard);
                InputSystem.settings.editorInputBehaviorInPlayMode=priorRouting;InputSystem.settings.backgroundBehavior=priorBackground;UnityEngine.Application.runInBackground=priorRun;
            }
            Invoke(entry,"BindDemoKeyboard");Invoke(entry,"BindDemoKeyboard");Invoke(entry,"UnbindDemoKeyboard");Key(panelRoot,KeyCode.Tab);Assert.That(moves,Is.Zero);
            Invoke(entry,"BindDemoKeyboard");var parent=root.parent;root.RemoveFromHierarchy();Key(panelRoot,KeyCode.Tab);parent.Add(root);
            Assert.That(moves,Is.Zero,"Detached document must not translate panel Tab");
            yield return null; // Reattached controls need their normal layout update before focus-ring traversal.
            Assert.That(moves,Is.Zero);Assert.That(root.panel.visualTree,Is.SameAs(panelRoot));
            Assert.That(doc.rootVisualElement,Is.SameAs(root));Assert.That(root.panel.focusController.focusedElement,Is.Null);
            Key(panelRoot,KeyCode.Tab);Assert.That(moves,Is.EqualTo(1));Assert.That(root.panel.focusController.focusedElement,Is.SameAs(root.Q<Button>("demo-menu")));
            Assert.That(entry.Presentation.Model.Current.Frame,Is.SameAs(frame));yield return new ExitPlayMode();
        }

        [UnityTest] public IEnumerator ReleasedReturnAfterFailedLoadDrawAndNavigationCanConfirmCombatRound()
            =>FailedLoadDrawAndNavigationReturn(true);

        [UnityTest] public IEnumerator HeldReturnAfterFailedLoadDrawAndNavigationCannotConfirmCombatRound()
            =>FailedLoadDrawAndNavigationReturn(false);

        private static IEnumerator FailedLoadDrawAndNavigationReturn(bool releaseBeforeNavigation)
        {
            RequireRun();yield return new EnterPlayMode();yield return null;yield return null;
            var entry=UnityEngine.Object.FindAnyObjectByType<ReviewEntry>();var doc=entry.GetComponent<UIDocument>();
            void Click(Button button)
            {
                Assert.That(button,Is.Not.Null);Assert.That(button.enabledInHierarchy,Is.True);
                using(var e=NavigationSubmitEvent.GetPooled()){e.target=button;button.SendEvent(e);}
            }
            void Id(string id)=>Click(doc.rootVisualElement.Q<Button>(id));
            Id("demo-start");yield return null;var model=entry.Presentation!.Model;
            Assert.That(model.Enqueue("colony.sol","ship-design.source-cruiser",ReviewUiModel.NewCommandId()),Is.True);
            Assert.That(model.Enqueue("colony.sol","ship-design.source-cruiser",ReviewUiModel.NewCommandId()),Is.True);
            Assert.That(model.AllocateResearch(450,ReviewUiModel.NewCommandId()),Is.True);
            while(model.Current.Frame.Status.Turn<5)Assert.That(model.Advance(),Is.True);
            Assert.That(model.Current.Frame.OwnShips.Count,Is.EqualTo(3));var fleet=model.Current.Frame.OwnStrategicFleets.Single().FleetId.Value;
            Assert.That(model.DraftTravel(fleet,CampaignTravelIntent.Leave,"nav.bajor",null),Is.True);
            Assert.That(model.SendTravel(ReviewUiModel.NewCommandId()),Is.True);Assert.That(model.Advance(),Is.True);
            var battle=model.Current.Frame.Battle;Assert.That(battle,Is.Not.Null);
            foreach(var order in battle!.StandingOrders.Where(o=>o.CanOrder))
                Assert.That(model.DraftFleet(order.FleetId,CampaignFleetManeuver.Charge,battle.LegalFocusFleetIds.FirstOrDefault(),false),Is.True);
            Assert.That(model.HasLocalEdits,Is.True);var controller=model.Current;var frame=controller.Frame;
            var draft=controller.Draft.Select(o=>o.Fleet+"|"+o.Maneuver+"|"+o.Focus).ToArray();Assert.That(draft,Is.Not.Empty);
            Invoke(entry,"Build");Id("review-nav-Saves");yield return null;
            // Unique absent slot; never load, overwrite or inspect an owner's slots.
            typeof(ReviewEntry).GetField("slotName",BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(entry,"missing-"+Guid.NewGuid().ToString("N").Substring(0,8));Invoke(entry,"Draw");
            var panelBeforeLoad=doc.rootVisualElement.panel.visualTree;Id("player-load");var loadConfirm=Confirm(doc.rootVisualElement);
            Assert.That(doc.rootVisualElement.panel.focusController.focusedElement,Is.SameAs(loadConfirm));Key(loadConfirm,KeyCode.Return);yield return null;
            Assert.That(model.Current,Is.SameAs(controller));Assert.That(model.Current.Frame,Is.SameAs(frame));Assert.That(model.HasLocalEdits,Is.True);
            Assert.That(controller.Draft.Select(o=>o.Fleet+"|"+o.Maneuver+"|"+o.Focus),Is.EqualTo(draft));
            Assert.That(doc.rootVisualElement.Q<Label>("save-load-result").text,Does.Contain("empty").And.Contain("unsent edits are unchanged"));
            Assert.That(doc.rootVisualElement.Q("review-one-use-confirmation"),Is.Null);
            Assert.That(doc.rootVisualElement.panel.visualTree,Is.SameAs(panelBeforeLoad),"Failed load redraw keeps this panel");
            if(releaseBeforeNavigation)Key(panelBeforeLoad,KeyCode.Return,true);
            Id("review-nav-Combat");yield return null;
            Assert.That(doc.rootVisualElement.panel.visualTree,Is.Not.SameAs(panelBeforeLoad),"Navigation rebuild replaces the panel");
            var review=doc.rootVisualElement.Query<Button>().ToList().Single(b=>b.text=="Review / commit round "+battle.Round);Click(review);
            var commitConfirm=Confirm(doc.rootVisualElement);Assert.That(doc.rootVisualElement.panel.focusController.focusedElement,Is.SameAs(commitConfirm));
            Key(commitConfirm,KeyCode.Return);
            if(!releaseBeforeNavigation)
            {
                Assert.That(model.Current.Frame,Is.SameAs(frame));Assert.That(model.HasLocalEdits,Is.True);
                Assert.That(doc.rootVisualElement.Q("review-one-use-confirmation"),Is.Not.Null,"Held load-confirm key cannot commit the later round");
                Key(doc.rootVisualElement.panel.visualTree,KeyCode.Return,true);Key(commitConfirm,KeyCode.Return);
            }
            Assert.That(model.Current.Frame,Is.Not.SameAs(frame));Assert.That(model.Current.Draft,Is.Empty);
            Assert.That(model.Current.Frame.Battle!.Reports.Select(r=>r.Round).Distinct().ToArray(),Is.EqualTo(new[]{battle.Round}),"Exactly one reviewed round was committed");
            Assert.That(doc.rootVisualElement.Q("review-one-use-confirmation"),Is.Null);
            Key(doc.rootVisualElement.panel.visualTree,KeyCode.Return,true);yield return new ExitPlayMode();
        }
    }
}
