#nullable enable
using UnityEngine;
using UnityEngine.UIElements;

namespace HobbyDS9.UnityReview
{
    public sealed partial class ReviewEntry
    {
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
