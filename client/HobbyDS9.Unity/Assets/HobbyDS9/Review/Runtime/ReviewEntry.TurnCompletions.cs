#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using HobbyDS9.Contracts;
using HobbyDS9.TacticalReviewClient;
using UnityEngine.UIElements;

namespace HobbyDS9.UnityReview
{
    public sealed partial class ReviewEntry
    {
        private readonly ReviewTurnCompletionQueue turnCompletions=new ReviewTurnCompletionQueue();
        private VisualElement? turnNoticeScheduledRoot;
        private VisualElement? completionDialogElement;
        private static ReviewTurnBoundary NoticeBoundary(ReviewFrame frame)
            =>new ReviewTurnBoundary(frame.Actor,frame.Generation,frame.Status.Turn,frame.Status.CampaignRevision,frame.Status.Phase);
        private void CapturePublishedCompletions(ReviewUiModel owner,ReviewFrame before)
        {
            if(!ReferenceEquals(model,owner)||owner.Busy||owner.Closed)return;
            var after=owner.Current.Frame;
            if(before.Actor!=after.Actor||before.Generation!=after.Generation||before.Status.Turn==int.MaxValue
                ||after.Status.Turn!=before.Status.Turn+1||after.Status.CampaignRevision<=before.Status.CampaignRevision
                ||after.Status.Phase!=SessionPhase.Planning||after.Battle!=null)return;
            var research=after.Strategy?.Research;if(research==null)return;
            var definitions=research.Engineering.ToDictionary(p=>p.Definition.DefinitionId.Value,
                p=>new ReviewEngineeringNoticeDefinition(p.Definition.DisplayName,p.Definition.EffectSummary),StringComparer.Ordinal);
            var notices=new List<ReviewCompletionNotice>(ReviewCompletionFacts.Research(research.Receipts,after.Actor,after.Status.Turn-1,definitions,owner.ColonisationCycleCampaign));
            if(owner.ColonisationCampaign)
            {
                var activities=owner.ColonisationActivities();var colonies=owner.Colonisation();
                var names=activities.BuildOptions.GroupBy(p=>p.DefinitionId,StringComparer.Ordinal).ToDictionary(g=>g.Key,g=>g.First().DisplayName,StringComparer.Ordinal);
                var places=colonies.Worlds.ToDictionary(w=>w.Id,w=>w.Name,StringComparer.Ordinal);
                notices.AddRange(ReviewCompletionFacts.Structures(activities.Structures,after.Actor,after.Status.Turn-1,names,places));
            }
            turnCompletions.Capture(owner,NoticeBoundary(before),NoticeBoundary(after),notices);
            ScheduleTurnCompletionDialog();
        }
        private void ScheduleTurnCompletionDialog()
        {
            if(model==null||root==null||!model.Integrated)return;
            turnCompletions.Synchronize(model,NoticeBoundary(model.Current.Frame));
            if(ReferenceEquals(turnNoticeScheduledRoot,root))return;
            var scheduled=root;turnNoticeScheduledRoot=scheduled;
            scheduled.schedule.Execute(()=>
            {
                if(ReferenceEquals(turnNoticeScheduledRoot,scheduled))turnNoticeScheduledRoot=null;
                if(this==null||!isActiveAndEnabled||!ReferenceEquals(root,scheduled)||model==null||model.Busy||model.Closed
                    ||campaignBusyToken!=null||confirmation!=null||demoLanding)return;
                ShowPendingCompletion();
            });
        }
        private void ShowPendingCompletion()
        {
            var owner=model!;var frame=owner.Current.Frame;var boundary=NoticeBoundary(frame);
            var notice=turnCompletions.Peek(owner,boundary);if(notice==null||frame.Battle!=null)return;
            bool Current()=>this!=null&&isActiveAndEnabled&&ReferenceEquals(model,owner)&&ReferenceEquals(owner.Current.Frame,frame)
                &&!owner.Busy&&!owner.Closed&&ReferenceEquals(turnCompletions.Peek(owner,NoticeBoundary(owner.Current.Frame)),notice);
            void Acknowledge()
            {
                if(Current()){turnCompletions.Acknowledge(owner,boundary,notice);completionDialogElement=null;}
                PreserveDemoKeyOrigins();ScheduleTurnCompletionDialog();
            }
            var choices=new List<CampaignDialogAction>();
            if(notice.ResearchTab!=null&&workspace?.CanNavigate==true)
                choices.Add(new CampaignDialogAction("Inspect "+(notice.ResearchTab=="science"?"Science":"Engineering"),()=>
                {
                    if(!Current())return;Acknowledge();workspace!.ResearchTab=notice.ResearchTab;
                    Navigate(ReviewWorkspacePage.Research);
                }));
            else if(notice.ColonyId!=null&&owner.ColonisationCampaign&&workspace?.CanNavigate==true
                &&owner.Colonisation().Worlds.Any(w=>w.Id==notice.ColonyId))
                choices.Add(new CampaignDialogAction("Inspect world",()=>
                {
                    if(!Current())return;Acknowledge();Navigate(ReviewWorkspacePage.Production);
                    root?.Q<ReviewColonisationPanel>()?.InspectOwnedWorld(notice.ColonyId);
                }));
            ShowCampaignDialog(notice.Title,"Resolved week "+(frame.Status.Turn-1)+" · "+turnCompletions.Position+" of "+turnCompletions.Count+"\n\n"+notice.Detail,
                turnCompletions.Position<turnCompletions.Count?"Next":"Close",Acknowledge,choices.ToArray());
            completionDialogElement=confirmation;
        }
    }
}
