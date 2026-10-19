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
        private static void ReleaseThrough(VisualElement originalPanelRoot,VisualElement actualOrigin,KeyCode key)
        {
            using(var evt=KeyUpEvent.GetPooled(new Event{type=EventType.KeyUp,keyCode=key}))
            {evt.target=actualOrigin;originalPanelRoot.SendEvent(evt);}
        }
        private static VisualElement ForeignPanel(out GameObject owner,out PanelSettings settings)
        {
            owner=new GameObject("Unrelated keyboard fixture document");var document=owner.AddComponent<UIDocument>();
            settings=ScriptableObject.CreateInstance<PanelSettings>();document.panelSettings=settings;
            return document.rootVisualElement.panel.visualTree;
        }

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
            Assert.That(panelRoot,Is.SameAs(oldPanelRoot),"Demo Start keeps the original dispatch panel");
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

        [UnityTest] public IEnumerator RetiredActualStartReturnReleaseAllowsLaterEndTurnConfirmation()
            =>ActualStartOriginRelease(KeyCode.Return,true);

        [UnityTest] public IEnumerator RetiredActualStartKeypadReleaseAllowsLaterEndTurnConfirmation()
            =>ActualStartOriginRelease(KeyCode.KeypadEnter,true);

        [UnityTest] public IEnumerator RetiredActualStartSpaceReleaseAllowsLaterEndTurnConfirmation()
            =>ActualStartOriginRelease(KeyCode.Space,true);

        [UnityTest] public IEnumerator ActualStartHeldReturnRemainsBlockedUntilItsRetiredOriginRelease()
            =>ActualStartOriginRelease(KeyCode.Return,false);

        private static IEnumerator ActualStartOriginRelease(KeyCode key,bool releaseBeforeModal)
        {
            RequireRun();yield return new EnterPlayMode();yield return null;yield return null;
            var entry=UnityEngine.Object.FindAnyObjectByType<ReviewEntry>();var doc=entry.GetComponent<UIDocument>();
            var start=doc.rootVisualElement.Q<Button>("demo-start");var oldPanel=doc.rootVisualElement.panel.visualTree;
            start.Focus();Key(start,key);yield return null;
            var root=doc.rootVisualElement;var model=entry.Presentation!.Model;var before=model.Current.Frame;
            Assert.That(root.panel.visualTree,Is.SameAs(oldPanel));Assert.That(start.panel,Is.SameAs(root.panel),"actual Start remains on original dispatch panel");
            Assert.That(oldPanel.Contains(start),Is.True);Assert.That(root.Contains(start),Is.False,"retired origin is outside the visible document");
            Assert.That(start.enabledInHierarchy,Is.False);Assert.That(start.focusable,Is.False);
            Assert.That(start.parent.name,Is.EqualTo("demo-retired-key-origins"));Assert.That(start.parent.style.display.value,Is.EqualTo(DisplayStyle.None));
            using(var e=NavigationSubmitEvent.GetPooled()){e.target=start;oldPanel.SendEvent(e);}
            Assert.That(model.Current.Frame,Is.SameAs(before),"retired disabled Start cannot run again");
            Assert.That(root.Q("demo-start"),Is.Null);Assert.That(root.panel.focusController.focusedElement,Is.Null);
            if(releaseBeforeModal){ReleaseThrough(oldPanel,start,key);Assert.That(start.panel,Is.Null,"matching release retires the obsolete target");}
            var endTurn=root.Q<Button>("player-primary-action");
            using(var e=NavigationSubmitEvent.GetPooled()){e.target=endTurn;endTurn.SendEvent(e);}
            var confirm=Confirm(root);Assert.That(root.panel.focusController.focusedElement,Is.SameAs(confirm));
            Key(confirm,key);
            if(!releaseBeforeModal)
            {
                Key(confirm,key);Assert.That(model.Current.Frame,Is.SameAs(before),"held Start must not confirm a later modal");
                Assert.That(root.Q("review-one-use-confirmation"),Is.Not.Null);
                ReleaseThrough(oldPanel,start,key);Assert.That(start.panel,Is.Null);Key(confirm,key);
            }
            var accepted=model.Current.Frame;Assert.That(accepted.Status.Turn,Is.EqualTo(before.Status.Turn+1));
            Assert.That(doc.rootVisualElement.Q("review-one-use-confirmation"),Is.Null);
            Key(confirm,key);Key(start,key);Assert.That(model.Current.Frame,Is.SameAs(accepted),"detached obsolete controls cannot act");
            Assert.That(confirm.panel,Is.SameAs(oldPanel.panel),"actual confirming origin survives ClearConfirmation and rebuild");
            ReleaseThrough(oldPanel,confirm,key);Assert.That(confirm.panel,Is.Null);yield return new ExitPlayMode();
        }

        [UnityTest] public IEnumerator StaleReleasedStartCannotClearNewerRetiredEndTurnConfirmationPress()
        {
            RequireRun();yield return new EnterPlayMode();yield return null;yield return null;
            var entry=UnityEngine.Object.FindAnyObjectByType<ReviewEntry>();var doc=entry.GetComponent<UIDocument>();
            var start=doc.rootVisualElement.Q<Button>("demo-start");var originalPanel=doc.rootVisualElement.panel.visualTree;
            start.Focus();Key(start,KeyCode.Return);yield return null;ReleaseThrough(originalPanel,start,KeyCode.Return);
            void OpenEndTurn()
            {
                var button=doc.rootVisualElement.Q<Button>("player-primary-action");
                using(var e=NavigationSubmitEvent.GetPooled()){e.target=button;button.SendEvent(e);}
            }
            OpenEndTurn();var first=Confirm(doc.rootVisualElement);Key(first,KeyCode.Return);yield return null;
            var model=entry.Presentation!.Model;var accepted=model.Current.Frame;Assert.That(accepted.Status.Turn,Is.EqualTo(2));
            Assert.That(first.panel,Is.SameAs(originalPanel.panel));Assert.That(first.enabledInHierarchy,Is.False);OpenEndTurn();var next=Confirm(doc.rootVisualElement);
            ReleaseThrough(originalPanel,start,KeyCode.Return);Key(next,KeyCode.Return);Key(next,KeyCode.Return);
            Assert.That(model.Current.Frame,Is.SameAs(accepted),"old Start release cannot release the newer confirming press");
            Assert.That(doc.rootVisualElement.Q("review-one-use-confirmation"),Is.Not.Null);
            ReleaseThrough(originalPanel,first,KeyCode.Return);Assert.That(first.panel,Is.Null);Key(next,KeyCode.Return);
            Assert.That(model.Current.Frame.Status.Turn,Is.EqualTo(3));ReleaseThrough(originalPanel,next,KeyCode.Return);
            yield return new ExitPlayMode();
        }

        [UnityTest] public IEnumerator StaleAttachedDisabledButtonReleaseCannotClearDifferentFocusedButtonPress()
        {
            RequireRun();yield return new EnterPlayMode();yield return null;yield return null;
            var entry=UnityEngine.Object.FindAnyObjectByType<ReviewEntry>();var root=entry.GetComponent<UIDocument>().rootVisualElement;
            int aCount=0,bCount=0;var a=Add(entry,root,()=>aCount++);var b=Add(entry,root,()=>bCount++);
            a.Focus();Key(a,KeyCode.Return);Key(a,KeyCode.Return,true);b.Focus();Key(b,KeyCode.Return);a.SetEnabled(false);
            Key(a,KeyCode.Return,true);Key(b,KeyCode.Return);
            Assert.That(aCount,Is.EqualTo(1));Assert.That(bCount,Is.EqualTo(1),"stale disabled attached origin cannot release a different hold");
            Key(b,KeyCode.Return,true);Key(b,KeyCode.Return);Assert.That(bCount,Is.EqualTo(2));Key(b,KeyCode.Return,true);
            yield return new ExitPlayMode();
        }

        [UnityTest] public IEnumerator ReturnKeypadReleaseMustMatchAndSpaceHoldRemainsIndependent()
        {
            RequireRun();yield return new EnterPlayMode();yield return null;yield return null;
            var entry=UnityEngine.Object.FindAnyObjectByType<ReviewEntry>();var root=entry.GetComponent<UIDocument>().rootVisualElement;
            int count=0;var button=Add(entry,root,()=>count++);button.Focus();
            Key(button,KeyCode.Return);Key(button,KeyCode.KeypadEnter,true);Key(button,KeyCode.KeypadEnter);
            Assert.That(count,Is.EqualTo(1),"keypad release is not release of held main Return");
            Key(button,KeyCode.Space);Key(button,KeyCode.Space);Assert.That(count,Is.EqualTo(2),"Space has a separate hold");
            Key(button,KeyCode.Space,true);Key(button,KeyCode.Return);Assert.That(count,Is.EqualTo(2));
            Key(button,KeyCode.Return,true);Key(button,KeyCode.KeypadEnter);Assert.That(count,Is.EqualTo(3));
            Key(button,KeyCode.Return,true);Key(button,KeyCode.KeypadEnter);Assert.That(count,Is.EqualTo(3),"main Return cannot release held keypad Enter");
            Key(button,KeyCode.KeypadEnter,true);Key(button,KeyCode.Return);Assert.That(count,Is.EqualTo(4));Key(button,KeyCode.Return,true);
            yield return new ExitPlayMode();
        }

        [UnityTest] public IEnumerator FocusLossCleanupInvalidatesOldOriginBeforeNewPress()=>CleanupInvalidatesOrigin(false);
        [UnityTest] public IEnumerator DisableCleanupInvalidatesOldOriginBeforeNewPress()=>CleanupInvalidatesOrigin(true);

        private static IEnumerator CleanupInvalidatesOrigin(bool disable)
        {
            RequireRun();yield return new EnterPlayMode();yield return null;yield return null;
            var entry=UnityEngine.Object.FindAnyObjectByType<ReviewEntry>();var doc=entry.GetComponent<UIDocument>();
            int count=0;var old=Add(entry,doc.rootVisualElement,()=>count++);old.Focus();Key(old,KeyCode.Return);Invoke(entry,"Build");
            var originalPanel=doc.rootVisualElement.panel.visualTree;Assert.That(originalPanel.Contains(old),Is.True);
            Assert.That(doc.rootVisualElement.Contains(old),Is.False);Assert.That(old.enabledInHierarchy,Is.False);
            if(disable){entry.enabled=false;Key(old,KeyCode.Return,true);entry.enabled=true;yield return null;}
            else Invoke(entry,"OnApplicationFocus",false);
            Assert.That(old.panel,Is.Null,"cleanup removes the retired origin");Assert.That(originalPanel.Q("demo-retired-key-origins"),Is.Null);
            var current=Add(entry,doc.rootVisualElement,()=>count++);current.Focus();Key(current,KeyCode.Return);
            Assert.That(count,Is.EqualTo(2),"lifecycle cleanup permits a new press");
            Key(old,KeyCode.Return,true);Key(current,KeyCode.Return);Assert.That(count,Is.EqualTo(2),"delayed pre-cleanup origin cannot release new hold");
            Key(current,KeyCode.Return,true);Key(current,KeyCode.Return);Assert.That(count,Is.EqualTo(3));Key(current,KeyCode.Return,true);
            yield return new ExitPlayMode();
        }

        [UnityTest] public IEnumerator SharedReturnAndSpaceRetiredOriginRemainsUntilBothKeysRelease()
        {
            RequireRun();yield return new EnterPlayMode();yield return null;yield return null;
            var entry=UnityEngine.Object.FindAnyObjectByType<ReviewEntry>();var doc=entry.GetComponent<UIDocument>();int count=0;
            var button=Add(entry,doc.rootVisualElement,()=>{if(++count==2)Invoke(entry,"Build");});var originalPanel=doc.rootVisualElement.panel.visualTree;
            button.Focus();Key(button,KeyCode.Return);Key(button,KeyCode.Space);
            var retired=originalPanel.Q("demo-retired-key-origins");Assert.That(retired.childCount,Is.EqualTo(1));
            Assert.That(button.panel,Is.SameAs(originalPanel.panel));Assert.That(button.enabledInHierarchy,Is.False);
            ReleaseThrough(originalPanel,button,KeyCode.Return);Assert.That(button.panel,Is.SameAs(originalPanel.panel),"Space still owns this same origin");
            ReleaseThrough(originalPanel,button,KeyCode.Space);Assert.That(button.panel,Is.Null);Assert.That(originalPanel.Q("demo-retired-key-origins"),Is.Null);
            Assert.That(count,Is.EqualTo(2));
            var a=Add(entry,doc.rootVisualElement,()=>{});var b=Add(entry,doc.rootVisualElement,()=>Invoke(entry,"Build"));
            a.Focus();Key(a,KeyCode.Return);b.Focus();Key(b,KeyCode.Space);
            Invoke(entry,"Build");Invoke(entry,"Build");retired=originalPanel.Q("demo-retired-key-origins");
            Assert.That(retired.childCount,Is.EqualTo(2),"repeated rebuilds retain only the two actual held origins");
            ReleaseThrough(originalPanel,a,KeyCode.Return);Assert.That(retired.childCount,Is.EqualTo(1));Assert.That(b.panel,Is.SameAs(originalPanel.panel));
            ReleaseThrough(originalPanel,b,KeyCode.Space);Assert.That(originalPanel.Q("demo-retired-key-origins"),Is.Null);
            yield return new ExitPlayMode();
        }

        [UnityTest] public IEnumerator DirectCentralTabRenderRetainsItsActualPressedOriginUntilRelease()
        {
            RequireRun();yield return new EnterPlayMode();yield return null;yield return null;
            var entry=UnityEngine.Object.FindAnyObjectByType<ReviewEntry>();var doc=entry.GetComponent<UIDocument>();
            var start=doc.rootVisualElement.Q<Button>("demo-start");using(var e=NavigationSubmitEvent.GetPooled()){e.target=start;start.SendEvent(e);}yield return null;
            var nav=doc.rootVisualElement.Q<Button>("review-nav-Combat");using(var e=NavigationSubmitEvent.GetPooled()){e.target=nav;nav.SendEvent(e);}yield return null;
            var root=doc.rootVisualElement;var originalPanel=root.panel.visualTree;var frame=entry.Presentation!.Model.Current.Frame;
            var inspection=root.Query<Button>().ToList().Single(b=>b.text=="Inspection");inspection.Focus();Key(inspection,KeyCode.Return);
            Assert.That(root.Contains(inspection),Is.False);Assert.That(inspection.panel,Is.SameAs(originalPanel.panel));
            Assert.That(inspection.enabledInHierarchy,Is.False);Assert.That(inspection.focusable,Is.False);
            Assert.That(originalPanel.Q("demo-retired-key-origins").childCount,Is.EqualTo(1));
            var current=root.Query<Button>().ToList().Single(b=>b.text=="Ship status");current.Focus();Key(current,KeyCode.Return);
            Assert.That(root.Contains(current),Is.True,"held tab key cannot activate another tab");
            ReleaseThrough(originalPanel,inspection,KeyCode.Return);Assert.That(inspection.panel,Is.Null);Key(current,KeyCode.Return);
            Assert.That(root.Contains(current),Is.False);Assert.That(current.panel,Is.SameAs(originalPanel.panel));
            ReleaseThrough(originalPanel,current,KeyCode.Return);Assert.That(current.panel,Is.Null);
            Assert.That(entry.Presentation.Model.Current.Frame,Is.SameAs(frame));yield return new ExitPlayMode();
        }

        [UnityTest] public IEnumerator PublicDispatchReleaseUsesDeliveryTimeAndPreservesHeldRepeatProtection()
        {
            RequireRun();yield return new EnterPlayMode();yield return null;yield return null;
            var entry=UnityEngine.Object.FindAnyObjectByType<ReviewEntry>();var root=entry.GetComponent<UIDocument>().rootVisualElement;
            int count=0;var button=Add(entry,root,()=>count++);button.Focus();Key(button,KeyCode.Return);Key(button,KeyCode.Return,true);
            // Pooled creation is unstamped. Panel.SendEvent assigns DELIVERY time;
            // waiting before sending does not create an earlier queued release.
            // Inspect only our own fields, never private engine state or setters.
            object? Own(string name)=>typeof(ReviewEntry).GetField(name,BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(entry);
            int focusLossSignals=0;
            void Trace(string stage,long timestamp)=>Debug.Log("KEY-RELEASE-DISPATCH "+stage+" timestamp="+timestamp
                +" held="+Own("returnHeld")+" started="+Own("returnStarted")
                +" originMatches="+ReferenceEquals(Own("returnOrigin"),button)+" count="+count
                +" buttonFocused="+ReferenceEquals(root.panel.focusController.focusedElement,button)
                +" applicationFocused="+UnityEngine.Application.isFocused+" focusLossSignals="+focusLossSignals
                +" submitPerformed="+(InputSystem.actions?.FindAction("UI/Submit",false)?.WasPerformedThisFrame()==true));
            using(var release=KeyUpEvent.GetPooled(new Event{type=EventType.KeyUp,keyCode=KeyCode.Return}))
            {
                long created=release.timestamp,started=0,received=-1;var panelRoot=root.panel.visualTree;
                EventCallback<KeyUpEvent> observe=e=>{if(ReferenceEquals(e,release))received=e.timestamp;Trace(ReferenceEquals(e,release)?"inside-panel-trickle":"other-keyup",e.timestamp);};
                Action<bool> focusChanged=focused=>{if(!focused)focusLossSignals++;Trace("application-focus-"+focused,release.timestamp);};
                button.RegisterCallback<KeyDownEvent>(e=>{started=e.timestamp;Trace("button-keydown",e.timestamp);},TrickleDown.TrickleDown);
                panelRoot.RegisterCallback(observe,TrickleDown.TrickleDown);UnityEngine.Application.focusChanged+=focusChanged;
                try
                {
                    Trace("pooled-before-wait",release.timestamp);Assert.That(created,Is.Zero,"public pooled event has no delivery timestamp yet");
                    yield return null;yield return null;
                    Key(button,KeyCode.Return);Trace("after-current-keydown",release.timestamp);
                    Assert.That(count,Is.EqualTo(2));Assert.That(Own("returnHeld"),Is.EqualTo(true));
                    Assert.That(Own("returnOrigin"),Is.SameAs(button));Assert.That(Own("returnStarted"),Is.EqualTo(started));
                    Assert.That(started,Is.GreaterThan(created));Assert.That(release.timestamp,Is.EqualTo(created));
                    long acceptedStarted=started;
                    Key(button,KeyCode.Return);Assert.That(count,Is.EqualTo(2),"held repeat is blocked before any release dispatch");
                    Assert.That(Own("returnStarted"),Is.EqualTo(acceptedStarted),"suppressed repeat cannot replace the accepted hold");
                    Assert.That(Own("returnOrigin"),Is.SameAs(button));
                    Trace("before-send",release.timestamp);release.target=panelRoot;panelRoot.SendEvent(release);Trace("after-send",release.timestamp);
                    Assert.That(received,Is.GreaterThanOrEqualTo(acceptedStarted),"panel assigns current delivery time before callbacks");
                    Assert.That(release.timestamp,Is.EqualTo(received));Assert.That(release.timestamp,Is.GreaterThan(created));
                    Assert.That(Own("returnHeld"),Is.EqualTo(false));Assert.That(Own("returnOrigin"),Is.Null);Assert.That(count,Is.EqualTo(2));
                    Key(button,KeyCode.Return);Key(button,KeyCode.Return);Assert.That(count,Is.EqualTo(3),"one new activation after the delivered release; repeated down remains blocked");
                    Key(button,KeyCode.Return,true);Trace("after-final-release",release.timestamp);Assert.That(Own("returnHeld"),Is.EqualTo(false));
                }
                finally{panelRoot.UnregisterCallback(observe,TrickleDown.TrickleDown);UnityEngine.Application.focusChanged-=focusChanged;}
            }
            yield return new ExitPlayMode();
        }

        [UnityTest] public IEnumerator AlreadyPerformedSubmitStartRebuildReleasesFromItsActualRetainedOrigin()
        {
            RequireRun();yield return new EnterPlayMode();yield return null;yield return null;
            var entry=UnityEngine.Object.FindAnyObjectByType<ReviewEntry>();var doc=entry.GetComponent<UIDocument>();
            var submit=InputSystem.actions.FindAction("UI/Submit",true);var map=submit.actionMap;
            var priorDevices=map.devices;var priorEnabled=submit.enabled;var priorRouting=InputSystem.settings.editorInputBehaviorInPlayMode;
            var priorBackground=InputSystem.settings.backgroundBehavior;var priorRun=UnityEngine.Application.runInBackground;
            var keyboard=InputSystem.AddDevice<Keyboard>("DemoStartSubmitOrigin");
            try
            {
                UnityEngine.Application.runInBackground=true;InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
                InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
                map.devices=new InputDevice[]{keyboard};if(!priorEnabled)submit.Enable();
                var start=doc.rootVisualElement.Q<Button>("demo-start");start.Focus();var oldPanel=doc.rootVisualElement.panel.visualTree;
                InputSystem.QueueStateEvent(keyboard,new KeyboardState(UnityEngine.InputSystem.Key.Enter));InputSystem.Update();
                Assert.That(submit.WasPerformedThisFrame(),Is.True);Key(start,KeyCode.Return);
                Assert.That(doc.rootVisualElement.Q<Button>("demo-start"),Is.SameAs(start),"performed action owns activation");
                // Explicitly arranged queued NavigationSubmit, not a native-provider claim.
                using(var e=NavigationSubmitEvent.GetPooled()){e.target=start;start.SendEvent(e);}
                Assert.That(start.panel,Is.SameAs(oldPanel.panel));Assert.That(start.enabledInHierarchy,Is.False);
                Assert.That(doc.rootVisualElement.panel.visualTree,Is.SameAs(oldPanel));Assert.That(oldPanel.Contains(start),Is.True);
                InputSystem.QueueStateEvent(keyboard,new KeyboardState());InputSystem.Update();Assert.That(submit.WasPerformedThisFrame(),Is.False);
                var model=entry.Presentation!.Model;var before=model.Current.Frame;var endTurn=doc.rootVisualElement.Q<Button>("player-primary-action");
                using(var e=NavigationSubmitEvent.GetPooled()){e.target=endTurn;endTurn.SendEvent(e);}
                var confirm=Confirm(doc.rootVisualElement);Key(confirm,KeyCode.Return);Assert.That(model.Current.Frame,Is.SameAs(before));
                ReleaseThrough(oldPanel,start,KeyCode.Return);Assert.That(start.panel,Is.Null);Key(confirm,KeyCode.Return);
                var accepted=model.Current.Frame;Assert.That(accepted.Status.Turn,Is.EqualTo(before.Status.Turn+1));
                using(var e=NavigationSubmitEvent.GetPooled()){e.target=start;start.SendEvent(e);}
                Key(confirm,KeyCode.Return);Assert.That(model.Current.Frame,Is.SameAs(accepted),"obsolete queued submit/repeat cannot activate again");
                ReleaseThrough(oldPanel,confirm,KeyCode.Return);Assert.That(confirm.panel,Is.Null);
            }
            finally
            {
                InputSystem.QueueStateEvent(keyboard,new KeyboardState());InputSystem.Update();
                if(!priorEnabled)submit.Disable();map.devices=priorDevices;
                InputSystem.settings.editorInputBehaviorInPlayMode=priorRouting;InputSystem.RemoveDevice(keyboard);
                InputSystem.settings.backgroundBehavior=priorBackground;UnityEngine.Application.runInBackground=priorRun;
            }
            yield return new ExitPlayMode();
        }

        [UnityTest] public IEnumerator PanelReleaseBindingRejectsOldDetachedAndRepeatedBindingsWithoutClearingHeldPress()
        {
            RequireRun();yield return new EnterPlayMode();yield return null;yield return null;
            var entry=UnityEngine.Object.FindAnyObjectByType<ReviewEntry>();var doc=entry.GetComponent<UIDocument>();
            var originalPanelRoot=doc.rootVisualElement.panel.visualTree;var start=doc.rootVisualElement.Q<Button>("demo-start");start.Focus();
            Key(start,KeyCode.Return);yield return null;
            var root=doc.rootVisualElement;var panelRoot=root.panel.visualTree;
            Assert.That(panelRoot,Is.SameAs(originalPanelRoot));var oldPanelRoot=ForeignPanel(out var foreignOwner,out var foreignSettings);
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
            Key(panelRoot,KeyCode.Return,true);UnityEngine.Object.Destroy(foreignOwner);UnityEngine.Object.Destroy(foreignSettings);yield return new ExitPlayMode();
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
            Assert.That(accepted.Status.Turn,Is.EqualTo(before.Status.Turn+1));Assert.That(panelRoot,Is.SameAs(oldPanelRoot));
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
            var entry=UnityEngine.Object.FindAnyObjectByType<ReviewEntry>();var doc=entry.GetComponent<UIDocument>();
            var oldPanelRoot=ForeignPanel(out var foreignOwner,out var foreignSettings);
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
            Assert.That(entry.Presentation.Model.Current.Frame,Is.SameAs(frame));UnityEngine.Object.Destroy(foreignOwner);UnityEngine.Object.Destroy(foreignSettings);yield return new ExitPlayMode();
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
            Assert.That(loadConfirm.panel,Is.SameAs(panelBeforeLoad.panel),"actual removed modal origin survives same-panel Draw");
            Assert.That(loadConfirm.enabledInHierarchy,Is.False);Assert.That(doc.rootVisualElement.Contains(loadConfirm),Is.False);
            if(releaseBeforeNavigation){ReleaseThrough(panelBeforeLoad,loadConfirm,KeyCode.Return);Assert.That(loadConfirm.panel,Is.Null);}
            Id("review-nav-Combat");yield return null;
            Assert.That(doc.rootVisualElement.panel.visualTree,Is.SameAs(panelBeforeLoad),"Demo navigation retains the dispatch panel");
            var review=doc.rootVisualElement.Query<Button>().ToList().Single(b=>b.text=="Review / commit round "+battle.Round);Click(review);
            var commitConfirm=Confirm(doc.rootVisualElement);Assert.That(doc.rootVisualElement.panel.focusController.focusedElement,Is.SameAs(commitConfirm));
            Key(commitConfirm,KeyCode.Return);
            if(!releaseBeforeNavigation)
            {
                Assert.That(model.Current.Frame,Is.SameAs(frame));Assert.That(model.HasLocalEdits,Is.True);
                Assert.That(doc.rootVisualElement.Q("review-one-use-confirmation"),Is.Not.Null,"Held load-confirm key cannot commit the later round");
                ReleaseThrough(panelBeforeLoad,loadConfirm,KeyCode.Return);Key(commitConfirm,KeyCode.Return);
            }
            Assert.That(model.Current.Frame,Is.Not.SameAs(frame));Assert.That(model.Current.Draft,Is.Empty);
            Assert.That(model.Current.Frame.Battle!.Reports.Select(r=>r.Round).Distinct().ToArray(),Is.EqualTo(new[]{battle.Round}),"Exactly one reviewed round was committed");
            Assert.That(doc.rootVisualElement.Q("review-one-use-confirmation"),Is.Null);
            Key(doc.rootVisualElement.panel.visualTree,KeyCode.Return,true);yield return new ExitPlayMode();
        }
    }
}
