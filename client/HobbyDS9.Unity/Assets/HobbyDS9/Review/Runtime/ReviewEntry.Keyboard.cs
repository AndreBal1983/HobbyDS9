#nullable enable
using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace HobbyDS9.UnityReview
{
    public sealed partial class ReviewEntry
    {
        // A normal control-level fallback for native KeyDown events which have
        // no corresponding InputSystem navigation action. No polling/dispatch
        // loop, provider replacement, action enablement or gameplay shortcut.
        private VisualElement? keyboardRoot;
        private VisualElement? keyboardReleaseRoot;
        private bool returnHeld,spaceHeld;
        private void BindDemoKeyboard()
        {
            UnbindDemoKeyboard();
            if(!model!.SingleplayerDemo)return;
            keyboardRoot=root;
            keyboardRoot!.RegisterCallback<KeyDownEvent>(DemoTab);
            keyboardRoot.RegisterCallback<AttachToPanelEvent>(DemoKeyboardAttached);
            keyboardRoot.RegisterCallback<DetachFromPanelEvent>(DemoKeyboardDetached);
            BindDemoKeyboardRelease(keyboardRoot.panel);
        }
        private void UnbindDemoKeyboard()
        {
            keyboardRoot?.UnregisterCallback<KeyDownEvent>(DemoTab);
            keyboardRoot?.UnregisterCallback<AttachToPanelEvent>(DemoKeyboardAttached);
            keyboardRoot?.UnregisterCallback<DetachFromPanelEvent>(DemoKeyboardDetached);
            BindDemoKeyboardRelease(null);
            keyboardRoot=null;
        }
        private void DemoKeyboardAttached(AttachToPanelEvent e)
        {if(ReferenceEquals(e.target,keyboardRoot))BindDemoKeyboardRelease(e.destinationPanel);}
        private void DemoKeyboardDetached(DetachFromPanelEvent e)
        {if(ReferenceEquals(e.target,keyboardRoot))BindDemoKeyboardRelease(null);}
        private void BindDemoKeyboardRelease(IPanel? currentPanel)
        {
            // With no focused child, UI Toolkit routes release to the panel
            // visual tree, outside the document subtree. Observe release only;
            // held presses remain latched across document/panel replacement.
            var next=currentPanel?.visualTree;
            if(ReferenceEquals(next,keyboardReleaseRoot))return;
            keyboardReleaseRoot?.UnregisterCallback<KeyUpEvent>(DemoKeyUp,TrickleDown.TrickleDown);
            keyboardReleaseRoot=next;
            keyboardReleaseRoot?.RegisterCallback<KeyUpEvent>(DemoKeyUp,TrickleDown.TrickleDown);
        }
        private void ResetDemoKeyboard(){returnHeld=false;spaceHeld=false;}
        private void OnApplicationFocus(bool focused){if(!focused)ResetDemoKeyboard();}
        private static bool Unmodified(KeyDownEvent e)=>!e.altKey&&!e.ctrlKey&&!e.commandKey&&!e.shiftKey;
        private bool LiveDemoFocus(VisualElement element)=>isActiveAndEnabled&&model!=null&&model.SingleplayerDemo&&!model.Closed&&!model.Busy
            &&root!=null&&root.panel!=null&&element.panel==root.panel&&root.Contains(element)&&element.enabledInHierarchy
            &&ReferenceEquals(root.panel.focusController.focusedElement,element);
        private static bool SubmitAlreadyPerformed()=>InputSystem.actions?.FindAction("UI/Submit",false)?.WasPerformedThisFrame()==true;
        private void BindDemoButtonKeyboard(Button button,Action guarded)
        {
            button.RegisterCallback<KeyDownEvent>(e=>{
                bool enter=e.keyCode==KeyCode.Return||e.keyCode==KeyCode.KeypadEnter;
                bool space=e.keyCode==KeyCode.Space;
                if((!enter&&!space)||!Unmodified(e)||!ReferenceEquals(e.target,button)||!LiveDemoFocus(button))return;
                // InputSystemProvider dispatches KeyDown before its already
                // queued NavigationSubmit. Let that real action own the press.
                if(SubmitAlreadyPerformed()){if(enter)returnHeld=true;else spaceHeld=true;return;}
                if(enter?returnHeld:spaceHeld){e.StopPropagation();return;}
                if(enter)returnHeld=true;else spaceHeld=true;
                e.StopPropagation();guarded();
            });
        }
        private void DemoKeyUp(KeyUpEvent e)
        {
            if(e.keyCode==KeyCode.Return||e.keyCode==KeyCode.KeypadEnter)returnHeld=false;
            if(e.keyCode==KeyCode.Space)spaceHeld=false;
        }
        private void DemoTab(KeyDownEvent e)
        {
            if(e.keyCode!=KeyCode.Tab||e.altKey||e.ctrlKey||e.commandKey)return;
            var focused=root?.panel?.focusController.focusedElement as VisualElement;
            if(focused==null||!LiveDemoFocus(focused))return;
            // Field/popup-owned keys are left to their own handlers. This is a
            // bubbling callback, so controls can consume their keys first.
            for(var element=e.target as VisualElement;element!=null&&element!=root;element=element.parent)
                if(element is TextField||element is DropdownField)return;
            var tab=Keyboard.current?.tabKey;
            if(tab!=null&&(tab.isPressed||tab.wasPressedThisFrame))return;
            e.StopPropagation();
            using(var navigation=NavigationMoveEvent.GetPooled(e.shiftKey?NavigationMoveEvent.Direction.Previous:NavigationMoveEvent.Direction.Next,e.modifiers))
            {navigation.target=focused;focused.SendEvent(navigation);}
        }
        private void ScheduleDemoMenuFocus(Button button)
        {
            var epoch=viewEpoch;
            button.schedule.Execute(()=>{
                if(epoch!=viewEpoch||confirmation!=null||!isActiveAndEnabled||model==null||!model.SingleplayerDemo||model.Closed
                    ||root?.panel==null||button.panel!=root.panel||!root.Contains(button)||!button.enabledInHierarchy)return;
                var focused=root.panel.focusController.focusedElement as VisualElement;
                if(focused==null||!root.Contains(focused))button.Focus();
            });
        }
    }
}
