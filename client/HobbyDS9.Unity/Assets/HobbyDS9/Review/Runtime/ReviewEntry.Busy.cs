#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace HobbyDS9.UnityReview
{
    public sealed partial class ReviewEntry
    {
        private object? routinePendingToken;
        private Label? routinePendingLabel;
        private readonly List<VisualElement> routineDisabledControls=new List<VisualElement>();
        private sealed class RoutineViewState
        {
            internal readonly List<(string name,Vector2 offset)> Scrolls=new List<(string,Vector2)>();
            internal string Focus="";
        }
        private RoutineViewState ShowRoutinePending(object token,string title)
        {
            var saved=new RoutineViewState();
            if(root!=null)
            {
                saved.Focus=(root.panel?.focusController.focusedElement as VisualElement)?.name??"";
                foreach(var scroll in root.Query<ScrollView>().ToList())saved.Scrolls.Add((scroll.name,scroll.scrollOffset));
            }
            routinePendingToken=token;
            PresentRoutinePending(title);
            return saved;
        }
        private void PresentRoutinePending(string title)
        {
            if(content==null||root==null)return;
            if(routinePendingLabel==null||!content.Contains(routinePendingLabel))
            {
                routinePendingLabel=new Label{name="routine-command-pending",pickingMode=PickingMode.Ignore};
                routinePendingLabel.AddToClassList("console-copy");routinePendingLabel.style.flexShrink=0;
                routinePendingLabel.style.color=new Color(.95f,.69f,.39f);content.Insert(0,routinePendingLabel);
            }
            routinePendingLabel.text=title;
            // Leave scrolling and the mounted scene intact. Commands/navigation
            // are unavailable until the single transactional worker drains.
            foreach(var control in root.Query<VisualElement>().ToList())
                if(control.enabledSelf&&control.GetFirstAncestorOfType<Scroller>()==null
                    &&(control is Button||control is BaseField<string>||control is BaseField<int>||control is BaseField<long>
                        ||control is BaseField<float>||control is BaseField<bool>))
                {control.SetEnabled(false);routineDisabledControls.Add(control);}
        }
        private bool HideRoutinePending(object token)
        {
            if(!ReferenceEquals(routinePendingToken,token))return false;
            routinePendingToken=null;routinePendingLabel?.RemoveFromHierarchy();routinePendingLabel=null;
            foreach(var control in routineDisabledControls)if(root?.Contains(control)==true)control.SetEnabled(true);
            routineDisabledControls.Clear();return true;
        }
        private void ReceiveRoutineView(RoutineViewState saved,bool changed)
        {
            if(changed)
            {
                // Scrolling stays available while the worker runs; retain the
                // latest viewport, including movement after command admission.
                saved.Scrolls.Clear();
                if(root!=null)foreach(var scroll in root.Query<ScrollView>().ToList())saved.Scrolls.Add((scroll.name,scroll.scrollOffset));
                viewEpoch++;Build();
            }
            else {if(playerGuidance!=null)playerGuidance.text=PlayerMessage();DrawPlayerActions();}
            if(!changed||root==null)return;
            var target=root;var epoch=viewEpoch;var owner=model;var frame=owner!.Current.Frame;
            // Restore after layout has established the new scroll ranges.
            target.schedule.Execute(()=>{
                if(!ReferenceEquals(root,target)||viewEpoch!=epoch||!ReferenceEquals(model,owner)
                    ||!ReferenceEquals(owner.Current.Frame,frame)||routinePendingToken!=null)return;
                var scrolls=target.Query<ScrollView>().ToList();
                for(int i=0;i<saved.Scrolls.Count&&i<scrolls.Count;i++)
                    if(scrolls[i].name==saved.Scrolls[i].name)scrolls[i].scrollOffset=saved.Scrolls[i].offset;
                if(saved.Focus.Length>0)target.Q(saved.Focus)?.Focus();
            });
        }
        private static void ReportRoutineTiming(string kind,Stopwatch clock,double begin,double worker,double receive,bool committed)
        {
            if(!Environment.GetCommandLineArgs().Contains("--hobbyds9-command-timing"))return;
            UnityEngine.Debug.Log(FormattableString.Invariant($"HOBBYDS9_COMMAND_TIMING kind={kind} beginMs={begin:F2} workerMs={worker:F2} receiveMs={receive:F2} viewMs={clock.Elapsed.TotalMilliseconds-begin-worker-receive:F2} totalMs={clock.Elapsed.TotalMilliseconds:F2} committed={committed}"));
        }
        private object? campaignBusyToken;
        private string campaignBusyTitle="";
        private double campaignBusyStarted;
        private void ShowCampaignBusy(object token,string title)
        {campaignBusyToken=token;campaignBusyTitle=title;campaignBusyStarted=Time.realtimeSinceStartupAsDouble;Draw();}
        private bool HideCampaignBusy(object token)
        {
            if(!ReferenceEquals(campaignBusyToken,token))return false;
            campaignBusyToken=null;campaignBusyTitle="";return true;
        }
        private bool DrawCampaignBusy()
        {
            if(campaignBusyToken==null)return false;
            content!.Clear();playerFooter!.Clear();
            playerTopActions!.Clear();playerTopActions.style.display=DisplayStyle.None;
            playerTitle!.text="Please wait";playerGuidance!.text=campaignBusyTitle;
            content.Add(new CampaignBusyPanel(campaignBusyTitle,campaignBusyStarted));return true;
        }
        private sealed class CampaignBusyPanel : VisualElement
        {
            public CampaignBusyPanel(string title,double started)
            {
                name="campaign-operation-busy";AddToClassList("console-panel");
                style.flexGrow=1;style.justifyContent=Justify.Center;style.paddingLeft=32;style.paddingRight=32;
                var heading=new Label(title){name="campaign-operation-title"};heading.style.fontSize=24;heading.style.whiteSpace=WhiteSpace.Normal;Add(heading);
                var track=new VisualElement{name="campaign-operation-activity",pickingMode=PickingMode.Ignore};
                track.style.height=12;track.style.marginTop=18;track.style.marginBottom=14;track.style.backgroundColor=new Color(.07f,.10f,.15f);Add(track);
                var indicator=new VisualElement{name="campaign-operation-indicator",pickingMode=PickingMode.Ignore};
                indicator.style.position=Position.Absolute;indicator.style.width=Length.Percent(24);indicator.style.height=12;indicator.style.backgroundColor=new Color(.95f,.64f,.30f);track.Add(indicator);
                var note=new Label("Elapsed 0s"){name="campaign-operation-elapsed"};
                note.style.whiteSpace=WhiteSpace.Normal;Add(note);
                int step=0;
                var animation=schedule.Execute(()=>{
                    step=(step+1)%60;indicator.style.left=Length.Percent(76f*step/59f);
                    note.text="Elapsed "+Mathf.Max(0,Mathf.FloorToInt((float)(Time.realtimeSinceStartupAsDouble-started)))+"s";
                }).Every(33);
                RegisterCallback<DetachFromPanelEvent>(_=>animation.Pause());
                RegisterCallback<AttachToPanelEvent>(_=>animation.Resume());
            }
        }
    }
}
