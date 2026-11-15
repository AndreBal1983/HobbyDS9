#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using HobbyDS9.Contracts;
using HobbyDS9.TacticalReviewClient;
using UnityEngine.UIElements;
using UnityEngine;

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
            var available=frame.Strategy?.Research.Engineering.Where(p=>p.CanTarget&&EngineeringPresentation.SupportedOutput(p,owner.ColonisationCycleCampaign)).OrderBy(p=>p.Definition.DisplayName,StringComparer.Ordinal).ToList();
            var detail=notice.Detail;
            if(notice.Discipline.HasValue)
            {
                var science=frame.Strategy?.Research.Science.FirstOrDefault(p=>p.Discipline==notice.Discipline.Value);
                if(science!=null)detail+="\n\nCurrent Science · level "+science.Level+(science.Maxed?" · complete":" · "+science.ProgressPercent.ToString("0.##",System.Globalization.CultureInfo.InvariantCulture)+"% toward the next level");
            }
            if(notice.ResearchTab!=null)
            {
                if(available?.Count>0)
                {
                    detail+="\n\nAvailable Engineering · "+available.Count+"\n"+string.Join("\n",available.Take(4).Select(p=>p.Definition.DisplayName))+(available.Count>4?"\n+ "+(available.Count-4)+" more":"");
                    var project=available[0];
                    if(workspace?.CanNavigate==true)choices.Add(new CampaignDialogAction("Open Engineering",()=>{
                        if(!Current())return;Acknowledge();workspace!.ResearchTab="engineering";workspace.EngineeringProjectId=project.Definition.DefinitionId.Value;
                        if(EngineeringPresentation.IsRefinement(project.Definition.Kind)){workspace.EngineeringRefinementSearch="";workspace.EngineeringRefinementCategory=EngineeringPresentation.Category(project.Definition.DefinitionId.Value);}
                        else {workspace.EngineeringNewSearch="";workspace.EngineeringNewCategory=EngineeringPresentation.Category(project.Definition.DefinitionId.Value);}
                        Navigate(ReviewWorkspacePage.Research);
                        var target=root;if(target!=null)target.schedule.Execute(()=>{if(confirmation!=null||!ReferenceEquals(root,target)||!ReferenceEquals(model,owner)||!ReferenceEquals(owner.Current.Frame,frame))return;var card=target.Q("engineering-goal-"+project.Definition.DefinitionId);if(card!=null){card.GetFirstAncestorOfType<ScrollView>()?.ScrollTo(card);card.Focus();}});
                    }));
                }
                else detail+="\n\nNo Engineering projects are available at this boundary.";
            }
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
            detail+="\n\nResolved week "+(frame.Status.Turn-1)+" · event "+turnCompletions.Position+" of "+turnCompletions.Count;
            ShowCampaignDialog(notice.Title,detail,
                turnCompletions.Position<turnCompletions.Count?"Next":"Close",Acknowledge,choices.ToArray());
            completionDialogElement=confirmation;
            var heading=confirmation?.Q<Label>("campaign-dialog-title");
            if(heading!=null)
            {
                var container=heading.parent;var index=container.IndexOf(heading);heading.RemoveFromHierarchy();var row=new VisualElement();row.style.flexDirection=FlexDirection.Row;row.style.alignItems=Align.Center;
                var kind=notice.ResearchTab=="engineering"?ReviewIconKind.Engineering:ReviewIconKind.Building;
                if(notice.Discipline.HasValue&&Enum.TryParse<ReviewIconKind>(notice.Discipline.Value.ToString(),out var disciplineIcon))kind=disciplineIcon;
                var icon=new ReviewIcon(kind,notice.Discipline?.ToString()??(notice.ResearchTab=="engineering"?"Engineering completion":"Construction completion"));icon.style.marginRight=12;row.Add(icon);row.Add(heading);container.Insert(index,row);
                var meta=confirmation!.Q("player-confirmation-details").Query<Label>().ToList().LastOrDefault();if(meta!=null){meta.style.fontSize=14;meta.style.color=new Color(.65f,.71f,.79f);}
            }
        }
    }
}
