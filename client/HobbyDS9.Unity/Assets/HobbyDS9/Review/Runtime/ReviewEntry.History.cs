#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using HobbyDS9.TacticalReviewClient;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
namespace HobbyDS9.UnityReview
{
    public sealed partial class ReviewEntry
    {
        private VisualElement? historyRoot;
        private InputAction? mouseBack,mouseForward;
        private bool mouseBackHeld,mouseForwardHeld,navigationInspectionRestorePending;
        private ReviewController? historyInspectionContext;
        private readonly Dictionary<long,ReviewColonisationPanel.Selection> historyColonyViews=new Dictionary<long,ReviewColonisationPanel.Selection>();
        private readonly Dictionary<long,ReviewActivitiesPanel.Selection> historyActivityViews=new Dictionary<long,ReviewActivitiesPanel.Selection>();
        private void RememberNavigationInspection()
        {
            if(workspace==null||model==null)return;
            if(!ReferenceEquals(historyInspectionContext,model.Current))
            {historyInspectionContext=model.Current;historyColonyViews.Clear();historyActivityViews.Clear();}
            var id=workspace.NavigationLocationId;
            if(ReferenceEquals(colonisationPanelOwner,model)&&ReferenceEquals(colonisationPanelController,model.Current)&&root?.Q<ReviewColonisationPanel>()?.CaptureSelection() is ReviewColonisationPanel.Selection colony)historyColonyViews[id]=colony;
            if(ReferenceEquals(activitiesPanelOwner,model)&&ReferenceEquals(activitiesPanelController,model.Current)&&root?.Q<ReviewActivitiesPanel>()?.CaptureSelection() is ReviewActivitiesPanel.Selection activity)historyActivityViews[id]=activity;
        }
        private void RestoreNavigationInspectionForRebuild()
        {
            if(!navigationInspectionRestorePending||workspace==null)return;navigationInspectionRestorePending=false;
            if(!ReferenceEquals(historyInspectionContext,model?.Current)){historyColonyViews.Clear();historyActivityViews.Clear();return;}
            var id=workspace.NavigationLocationId;
            historyColonyViews.TryGetValue(id,out colonisationRebuildSelection);historyActivityViews.TryGetValue(id,out activitiesRebuildSelection);
            // An inspection is read-only and cannot replay an old reviewed command.
            colonisationRebuildReview=null;
            var keep=workspace.RetainedNavigationLocationIds.ToHashSet();
            foreach(var key in historyColonyViews.Keys.Where(k=>!keep.Contains(k)).ToArray())historyColonyViews.Remove(key);
            foreach(var key in historyActivityViews.Keys.Where(k=>!keep.Contains(k)).ToArray())historyActivityViews.Remove(key);
        }
        private void BindMouseHistory()
        {
            UnbindMouseHistory();if(!PlayerProfile||root==null)return;historyRoot=root;
            mouseBackHeld=Mouse.current?.backButton.isPressed==true;mouseForwardHeld=Mouse.current?.forwardButton.isPressed==true;
            historyRoot.RegisterCallback<PointerDownEvent>(HistoryPointerDown,TrickleDown.TrickleDown);
            historyRoot.RegisterCallback<PointerUpEvent>(HistoryPointerUp,TrickleDown.TrickleDown);
            historyRoot.RegisterCallback<AttachToPanelEvent>(HistoryAttached);historyRoot.RegisterCallback<DetachFromPanelEvent>(HistoryDetached);
            mouseBack=new InputAction("Campaign history back",InputActionType.Button,"<Mouse>/backButton"){wantsInitialStateCheck=true};
            mouseForward=new InputAction("Campaign history forward",InputActionType.Button,"<Mouse>/forwardButton"){wantsInitialStateCheck=true};
            mouseBack.performed+=_=>QueueMouseHistory(false);mouseForward.performed+=_=>QueueMouseHistory(true);
            mouseBack.canceled+=_=>mouseBackHeld=false;mouseForward.canceled+=_=>mouseForwardHeld=false;
            if(historyRoot.panel!=null){mouseBack.Enable();mouseForward.Enable();}
        }
        private void UnbindMouseHistory()
        {
            historyRoot?.UnregisterCallback<PointerDownEvent>(HistoryPointerDown,TrickleDown.TrickleDown);historyRoot?.UnregisterCallback<PointerUpEvent>(HistoryPointerUp,TrickleDown.TrickleDown);
            historyRoot?.UnregisterCallback<AttachToPanelEvent>(HistoryAttached);historyRoot?.UnregisterCallback<DetachFromPanelEvent>(HistoryDetached);
            // Disable may cancel; preserve held provenance across root replacement.
            var back=mouseBackHeld;var forward=mouseForwardHeld;mouseBack?.Dispose();mouseForward?.Dispose();mouseBack=null;mouseForward=null;mouseBackHeld=back;mouseForwardHeld=forward;historyRoot=null;
        }
        private void HistoryAttached(AttachToPanelEvent e){if(ReferenceEquals(e.target,historyRoot)){mouseBack?.Enable();mouseForward?.Enable();}}
        private void HistoryDetached(DetachFromPanelEvent e){if(ReferenceEquals(e.target,historyRoot)){mouseBack?.Disable();mouseForward?.Disable();}}
        private void HistoryPointerDown(PointerDownEvent e)
        {
            if(e.button!=3&&e.button!=4)return;e.StopImmediatePropagation();
            if((e.pressedButtons&7)!=0||e.altKey||e.ctrlKey||e.shiftKey||e.commandKey)return;
            QueueMouseHistory(e.button==4);
        }
        private void HistoryPointerUp(PointerUpEvent e)
        {if(e.button==3){mouseBackHeld=false;e.StopImmediatePropagation();}else if(e.button==4){mouseForwardHeld=false;e.StopImmediatePropagation();}}
        private void QueueMouseHistory(bool forward)
        {
            if(forward?mouseForwardHeld:mouseBackHeld)return;if(forward)mouseForwardHeld=true;else mouseBackHeld=true;
            var target=historyRoot;var owner=model;var controller=owner?.Current;var frame=controller?.Frame;var epoch=viewEpoch;
            if(target?.panel==null||!CanUseMouseHistory())return;
            target.schedule.Execute(()=>{
                if(!ReferenceEquals(historyRoot,target)||!ReferenceEquals(root,target)||viewEpoch!=epoch||!ReferenceEquals(model,owner)||!ReferenceEquals(owner?.Current,controller)||!ReferenceEquals(controller?.Frame,frame))return;
                NavigateMouseHistory(forward);
            });
        }
        private bool CanUseMouseHistory()
        {
            var keyboard=Keyboard.current;var mouse=Mouse.current;
            if(this==null||!isActiveAndEnabled||workspace==null||model==null||confirmation!=null||model.Busy||model.Closed||model.Current.RefreshRequired
                ||model.Current.HasPendingWork||model.ResearchCommitPending||campaignBusyToken!=null||routinePendingToken!=null||generatedGalaxyOpen||demoLanding||!demoStarted||campaignSettingsReturn!=null||frontierCommissionOpen
                ||root?.panel==null||!root.enabledInHierarchy||keyboard?.altKey.isPressed==true||keyboard?.ctrlKey.isPressed==true||keyboard?.shiftKey.isPressed==true||keyboard?.leftMetaKey.isPressed==true||keyboard?.rightMetaKey.isPressed==true
                ||mouse?.leftButton.isPressed==true||mouse?.rightButton.isPressed==true||mouse?.middleButton.isPressed==true)return false;
            return true;
        }
        private bool NavigateMouseHistory(bool forward)
        {
            var navigation=workspace;if(navigation==null||!CanUseMouseHistory())return false;
            RememberNavigationInspection();if(!(forward?navigation.GoForward():navigation.GoBack()))return false;
            navigationInspectionRestorePending=true;frontierCommissionOpen=false;viewEpoch++;Build();return true;
        }
    }
}
