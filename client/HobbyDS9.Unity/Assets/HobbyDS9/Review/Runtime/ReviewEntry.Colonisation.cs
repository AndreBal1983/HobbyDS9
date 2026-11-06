#nullable enable
using System;
using System.Linq;
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
            var same=!demoLanding&&workspace!=null&&(workspace.Page==ReviewWorkspacePage.Production||workspace.Page==ReviewWorkspacePage.Worlds)
                &&ReferenceEquals(model,colonisationPanelOwner)&&ReferenceEquals(model?.Current,colonisationPanelController);
            if(!same)colonisationRebuildSelection=null;
            else if(root?.Q<ReviewColonisationPanel>()?.CaptureSelection() is ReviewColonisationPanel.Selection selected)colonisationRebuildSelection=selected;
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
            var systemNodes=frame.Strategy?.Navigation.Nodes.Where(node=>node.AssociatedSystemId.HasValue)
                .ToLookup(node=>node.AssociatedSystemId!.Value.Value,node=>node.NodeId.Value,StringComparer.Ordinal);
            var observation=owner.Colonisation();var activities=owner.ColonisationActivities();var epoch=viewEpoch;
            var panel=new ReviewColonisationPanel(observation,owner.ColonisationPlanets(),activities,
                (command,caption)=>{if(Current())RunColonisationInteraction(owner,controller,frame,ColonisationInteractionKind.Preview,colonyCommand:command,caption:caption,selection:Selection());},
                command=>{if(Current())RunColonisationInteraction(owner,controller,frame,ColonisationInteractionKind.Submission,colonyCommand:command,selection:Selection());},
                (command,caption)=>{if(Current())RunColonisationInteraction(owner,controller,frame,ColonisationInteractionKind.Preview,command:command,caption:caption,selection:Selection());},
                command=>{if(Current())RunColonisationInteraction(owner,controller,frame,ColonisationInteractionKind.Submission,command:command,selection:Selection());},owner.SupportsKitFreePassengers,systemNodes);
            if(frame.Strategy!=null&&observation.Actor==frame.Actor&&activities.ActorId==frame.Actor
                &&observation.Turn==frame.Status.Turn&&activities.Turn==frame.Status.Turn
                &&observation.Revision==frame.Status.CampaignRevision&&activities.Revision==frame.Status.CampaignRevision)
            {
                var knownLocations=frame.Strategy.Navigation.Nodes.ToDictionary(node=>node.NodeId.Value,node=>node.DisplayName,StringComparer.Ordinal);
                panel.BindKnownSiteNavigation(knownLocations,node=>{
                    if(!Current()||epoch!=viewEpoch||confirmation!=null||root?.Contains(panel)!=true||workspace?.CanNavigate!=true
                        ||(workspace.Page!=ReviewWorkspacePage.Production&&workspace.Page!=ReviewWorkspacePage.Worlds)
                        ||!knownLocations.ContainsKey(node))return;
                    // Match normal navigation: only select after permission succeeds,
                    // then rebuild once. Inspection does not choose or order a fleet.
                    if(!workspace.Navigate(ReviewWorkspacePage.Map))return;
                    workspace.SelectedNode=node;frontierCommissionOpen=false;viewEpoch++;Build();
                });
            }
            panel.RestoreSelection(selection);
            var inspection=workspace!.PendingSystemInspection;
            var worldInspection=workspace.PendingWorldInspection;
            workspace.PendingSystemInspection=null;workspace.PendingWorldInspection=null;
            if(worldInspection.HasValue)
            {
                if(ReferenceEquals(worldInspection.Value.Frame,frame))panel.InspectOwnedWorld(worldInspection.Value.World);
            }
            else if(inspection.HasValue)panel.InspectSystem(inspection.Value.SystemId,inspection.Value.Caption);
            else panel.RestoreReview(reviewed);
            panel.PresentStatus(owner.ColonisationActionStatus());parent.Add(panel);colonisationPanelOwner=owner;colonisationPanelController=controller;colonisationPanelFrame=frame;
        }
        private void DrawColonisationMenu(VisualElement parent)
        {
            var fold=new Foldout{text="Earlier colonisation V1 (provisional playtest)",value=false,name="colonisation-campaign-menu"};parent.Add(fold);
            fold.Add(Readout("Survey discovered planets, manufacture transport and settlement equipment, carry real settlers and supplies, then establish and support a frontier world. Separate save slots; earlier campaigns are unchanged. AI expansion planning is not included in this playtest."));
            var start=AddButton(fold,"New colonisation campaign",()=>{void Start()=>RunGeneratedCampaignOperation(null,colonisation:true);if(demoStarted||model!.HasLocalEdits)Confirm("Start a colonisation campaign? Unsaved progress is replaced only after validation; existing saves remain intact.",Start);else Start();});start.name="colonisation-campaign-start";
            foreach(var slot in GeneratedCampaignOptions.Slots()){var captured=slot;var load=AddButton(fold,"Continue colonisation "+slot.Substring(slot.Length-1),()=>Confirm("Continue this colonisation campaign? Unsaved progress is replaced only after complete replay validation.",()=>RunGeneratedCampaignOperation(captured,colonisation:true)));load.name="colonisation-campaign-continue-"+slot;}
        }
    }
}
