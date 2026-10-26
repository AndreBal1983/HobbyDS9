#nullable enable
using System;
using HobbyDS9.Contracts;
using HobbyDS9.TacticalReviewClient;
using UnityEngine.UIElements;
namespace HobbyDS9.UnityReview
{
    public sealed partial class ReviewEntry
    {
        private ReviewUiModel? colonisationPanelOwner;
        private ReviewController? colonisationPanelController;
        private ReviewFrame? colonisationPanelFrame;
        private ReviewColonisationPanel.Selection? colonisationRebuildSelection;
        private ReviewColonisationPanel.ReviewState? colonisationRebuildReview;
        private void CaptureColonisationSelectionForRebuild()
        {
            colonisationRebuildSelection=!demoLanding&&workspace!=null&&(workspace.Page==ReviewWorkspacePage.Production||workspace.Page==ReviewWorkspacePage.Worlds)
                &&ReferenceEquals(model,colonisationPanelOwner)&&ReferenceEquals(model?.Current,colonisationPanelController)?root?.Q<ReviewColonisationPanel>()?.CaptureSelection():null;
            colonisationRebuildReview=colonisationRebuildSelection!=null&&ReferenceEquals(model?.Current.Frame,colonisationPanelFrame)?root?.Q<ReviewColonisationPanel>()?.CaptureReview():null;
        }
        private void DrawColonisationPanel(VisualElement parent)
        {
            var owner=model!;var controller=owner.Current;var frame=controller.Frame;
            var selection=ReferenceEquals(owner,colonisationPanelOwner)&&ReferenceEquals(controller,colonisationPanelController)?parent.Q<ReviewColonisationPanel>()?.CaptureSelection()??colonisationRebuildSelection:null;
            var reviewed=ReferenceEquals(owner,colonisationPanelOwner)&&ReferenceEquals(controller,colonisationPanelController)&&ReferenceEquals(frame,colonisationPanelFrame)?parent.Q<ReviewColonisationPanel>()?.CaptureReview()??colonisationRebuildReview:null;
            colonisationRebuildSelection=null;colonisationRebuildReview=null;
            foreach(var old in parent.Query<ReviewColonisationPanel>().ToList())old.Dispose();foreach(var old in parent.Query<ReviewActivitiesPanel>().ToList())old.Dispose();parent.Clear();
            bool Current()=>ReferenceEquals(model,owner)&&ReferenceEquals(owner.Current,controller)&&ReferenceEquals(controller.Frame,frame)&&!owner.Busy&&!owner.Closed;
            ReviewColonisationPanel.Selection? Selection()=>root?.Q<ReviewColonisationPanel>()?.CaptureSelection();
            var panel=new ReviewColonisationPanel(owner.Colonisation(),owner.ColonisationPlanets(),owner.ColonisationActivities(),
                (command,caption)=>{if(Current())RunColonisationInteraction(owner,controller,frame,ColonisationInteractionKind.Preview,colonyCommand:command,caption:caption,selection:Selection());},
                command=>{if(Current())RunColonisationInteraction(owner,controller,frame,ColonisationInteractionKind.Submission,colonyCommand:command,selection:Selection());},
                (command,caption)=>{if(Current())RunColonisationInteraction(owner,controller,frame,ColonisationInteractionKind.Preview,command:command,caption:caption,selection:Selection());},
                command=>{if(Current())RunColonisationInteraction(owner,controller,frame,ColonisationInteractionKind.Submission,command:command,selection:Selection());});
            panel.RestoreSelection(selection);panel.RestoreReview(reviewed);panel.PresentStatus(owner.ColonisationActionStatus());parent.Add(panel);colonisationPanelOwner=owner;colonisationPanelController=controller;colonisationPanelFrame=frame;
        }
        private void DrawColonisationMenu(VisualElement parent)
        {
            var fold=new Foldout{text="Colonisation campaign (provisional playtest)",value=false,name="colonisation-campaign-menu"};parent.Add(fold);
            fold.Add(Readout("Survey discovered planets, manufacture transport and settlement equipment, carry real settlers and supplies, then establish and support a frontier world. Separate save slots; earlier campaigns are unchanged. AI expansion planning is not included in this playtest."));
            var start=AddButton(fold,"New colonisation campaign",()=>{void Start()=>RunGeneratedCampaignOperation(null,colonisation:true);if(demoStarted||model!.HasLocalEdits)Confirm("Start a colonisation campaign? Unsaved progress is replaced only after validation; existing saves remain intact.",Start);else Start();});start.name="colonisation-campaign-start";
            foreach(var slot in GeneratedCampaignOptions.Slots()){var captured=slot;var load=AddButton(fold,"Continue colonisation "+slot.Substring(slot.Length-1),()=>Confirm("Continue this colonisation campaign? Unsaved progress is replaced only after complete replay validation.",()=>RunGeneratedCampaignOperation(captured,colonisation:true)));load.name="colonisation-campaign-continue-"+slot;}
        }
    }
}
