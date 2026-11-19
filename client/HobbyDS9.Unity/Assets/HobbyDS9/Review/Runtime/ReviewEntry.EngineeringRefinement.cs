#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HobbyDS9.Contracts;
using HobbyDS9.TacticalReviewClient;
using UnityEngine.UIElements;
namespace HobbyDS9.UnityReview
{
    public sealed partial class ReviewEntry
    {
        private ReviewUiModel? refinementSession;
        private long refinementGeneration;
        private string refinementActor="",refinementSearch="",refinementNotice="";
        private readonly HashSet<string> refinementAnnounced=new HashSet<string>(StringComparer.Ordinal);
        private VisualElement? refinementScheduledRoot;
        private EngineeringRefinementInteractionWork? refinementUiWork;
        private ReviewUiModel? refinementUiOwner;
        private string refinementUiTitle="";
        private void CancelEngineeringRefinementUiWork()
        {
            if(refinementUiWork!=null)refinementUiOwner?.AbandonEngineeringRefinementInteraction(refinementUiWork);
            // A cancelled worker may retain its source read lease until Execute
            // settles. Keep the owner/token so a re-enabled view stays busy.
        }
        private void RestoreEngineeringRefinementBusy()
        {
            if(refinementUiWork!=null&&ReferenceEquals(model,refinementUiOwner)&&model!.Busy&&campaignBusyToken==null)
            {routinePendingToken=refinementUiWork;PresentRoutinePending(refinementUiTitle);}
        }
        private void SynchronizeRefinementSession(ReviewUiModel owner,ReviewFrame frame)
        {
            if(ReferenceEquals(refinementSession,owner)&&refinementGeneration==frame.Generation&&refinementActor==frame.Actor)return;
            refinementSession=owner;refinementGeneration=frame.Generation;refinementActor=frame.Actor;refinementSearch="";refinementNotice="";refinementAnnounced.Clear();
        }
        private EngineeringRefinementSnapshot? RefinementSnapshot(ReviewUiModel owner,ReviewFrame frame)
        {
            if(!owner.SupportsEngineeringRefinement||owner.Busy||owner.Closed)return null;
            var observed=owner.CaptureEngineeringRefinement(frame.Actor);
            return observed!=null&&observed.ActorId==frame.Actor&&observed.Turn==frame.Status.Turn&&observed.Revision==frame.Status.CampaignRevision?observed:null;
        }
        private bool RefinementCurrent(ReviewUiModel owner,ReviewFrame frame)
            =>this!=null&&isActiveAndEnabled&&ReferenceEquals(model,owner)&&ReferenceEquals(owner.Current.Frame,frame)&&!owner.Busy&&!owner.Closed&&root?.panel!=null;
        private static bool RefinementEditable(ReviewUiModel owner,ReviewFrame frame)
            =>frame.Status.Phase==SessionPhase.Planning&&frame.Battle==null&&!owner.Current.RefreshRequired&&!owner.HasOrderDraft;
        private static string RefinementModuleName(EngineeringRefinementSnapshot observed,EngineeringRefinementProjectSnapshot project)
            =>observed.Parents.FirstOrDefault(p=>p.Vector.VersionId==project.Before.VersionId)?.DisplayName??"Module development";
        // A drain may publish a newer frame. That invalidates this reviewed intent;
        // it is never silently stamped again with the new revision.
        private bool RefinementReviewBoundary(ReviewUiModel owner,ReviewFrame frame)
        {
            if(!RefinementCurrent(owner,frame))return false;
            if(!owner.FlushResearchAllocations()||!ReferenceEquals(owner.Current.Frame,frame))
            {
                refinementNotice="Allocation or campaign state changed. Open Engineering again to review the current values.";
                if(confirmation!=null)ClearConfirmation();viewEpoch++;Build();return false;
            }
            return true;
        }
        private bool DrawEngineeringRefinement(VisualElement section)
        {
            var owner=model!;if(!owner.SupportsEngineeringRefinement)return false;
            var frame=owner.Current.Frame;var observed=RefinementSnapshot(owner,frame);
            if(observed==null){section.Add(Readout("Engineering refinement information is temporarily unavailable."));return true;}
            SynchronizeRefinementSession(owner,frame);
            var heading=Readout("Technology refinement");heading.AddToClassList("console-section");section.Add(heading);
            if(refinementNotice.Length>0){var notice=Readout(refinementNotice);notice.name="engineering-generated-notice";section.Add(notice);}
            var projects=observed.Projects.Where(p=>p.Stage==EngineeringRefinementStage.Working||p.Stage==EngineeringRefinementStage.Pending||p.Stage==EngineeringRefinementStage.RetainedInternal).ToArray();
            var active=projects.FirstOrDefault(p=>p.IsActive);
            if(active!=null)
            {
                var activeName=section.parent?.parent?.Q<Label>("engineering-active-name");
                if(activeName!=null){activeName.text=EngineeringRefinementPresentation.Goal(active.Goal);var label=activeName.parent.Q<Label>(className:"engineering-overview-label");if(label!=null)label.text="Current targeted project";}
            }
            if(projects.Length>0)
            {
                var pending=Readout("Development & outcomes · "+projects.Length);pending.AddToClassList("engineering-overview-label");section.Add(pending);
                foreach(var project in projects)
                {
                    var captured=project;
                    var state=project.Stage==EngineeringRefinementStage.Working?(project.IsActive?"Active":"Paused")+" · Work completed "+EngineeringRefinementPresentation.Work(project.Progress)+" / "+EngineeringRefinementPresentation.Work(project.Work)+" Engineering points":project.Stage==EngineeringRefinementStage.Pending?"Outcome awaiting your decision":"Retained internal version · not released";
                    var button=AddButton(section,RefinementModuleName(observed,project)+" · "+EngineeringRefinementPresentation.Goal(project.Goal)+"\n"+state,()=>OpenEngineeringProject(owner,frame,captured.ProjectId));
                    button.name="engineering-generated-project-"+project.ProjectId;button.AddToClassList("engineering-goal");button.style.minHeight=44;
                }
            }
            var search=new TextField("Search retained modules"){name="engineering-generated-search",value=refinementSearch};section.Add(search);
            var list=new ScrollView(ScrollViewMode.Vertical){name="engineering-generated-parents",horizontalScrollerVisibility=ScrollerVisibility.Hidden};list.style.flexGrow=1;list.style.minHeight=0;section.Add(list);
            var rows=new List<(EngineeringRefinementParentSnapshot parent,Button row)>();
            foreach(var parent in observed.Parents)
            {
                var captured=parent;var value=parent.Vector;
                var button=AddButton(list,parent.DisplayName+"\nHull space "+value.Capacity+" · Reactor output "+value.PowerSupply+" · Industry "+value.Industry+"\nMaturity "+EngineeringRefinementPresentation.Maturity(value.MaturityMilli)+" · "+(parent.Released?"Released":"Retained for development"),()=>ChooseEngineeringGoal(owner,frame,captured.Vector.VersionId,null));
                button.name="engineering-generated-parent-"+value.VersionId;button.AddToClassList("engineering-goal");button.style.minHeight=44;rows.Add((parent,button));
            }
            var empty=Readout("No retained modules match.");empty.name="engineering-generated-empty";list.Add(empty);
            void Filter()
            {
                var visible=0;foreach(var row in rows){var match=refinementSearch.Length==0||row.parent.DisplayName.IndexOf(refinementSearch,StringComparison.OrdinalIgnoreCase)>=0;row.row.style.display=match?DisplayStyle.Flex:DisplayStyle.None;if(match)visible++;}
                heading.text="Technology refinement · "+visible+" retained modules";empty.style.display=visible==0?DisplayStyle.Flex:DisplayStyle.None;
            }
            search.RegisterValueChangedCallback(e=>{if(RefinementCurrent(owner,frame)&&section.panel!=null){refinementSearch=e.newValue;Filter();}});Filter();
            if(!RefinementEditable(owner,frame))section.Add(Readout(owner.HasOrderDraft?"Apply or discard route/organization edits before submitting Engineering work.":"Engineering actions are read-only at this boundary."));
            return true;
        }
        private EngineeringRefinementCommandDto RefinementCommand(ReviewFrame frame,EngineeringRefinementAction action,string? parent,EngineeringRefinementGoal goal,string? project)
            =>new EngineeringRefinementCommandDto{ActorId=frame.Actor,CommandId=ReviewUiModel.NewCommandId(),ExpectedTurn=frame.Status.Turn,ExpectedRevision=frame.Status.CampaignRevision,Action=action,ParentVersionId=parent,Goal=goal,ProjectId=project};
        private void ChooseEngineeringGoal(ReviewUiModel owner,ReviewFrame frame,string? parent,string? project)
        {
            if(confirmation!=null||!RefinementReviewBoundary(owner,frame))return;
            var observed=RefinementSnapshot(owner,frame);if(observed==null)return;
            var pending=project==null?null:observed.Projects.FirstOrDefault(p=>p.ProjectId==project&&p.CanContinue);
            var retained=parent==null?null:observed.Parents.FirstOrDefault(p=>p.Vector.VersionId==parent);
            if(project!=null&&pending==null||project==null&&retained==null)return;
            var choices=new List<CampaignDialogAction>();
            foreach(EngineeringRefinementGoal goal in Enum.GetValues(typeof(EngineeringRefinementGoal)))
            {
                var command=RefinementCommand(frame,project==null?EngineeringRefinementAction.Start:EngineeringRefinementAction.Continue,parent,goal,project);
                if(RefinementEditable(owner,frame))choices.Add(new CampaignDialogAction("Review target · "+EngineeringRefinementPresentation.Goal(goal),()=>RunEngineeringRefinementInteraction(owner,owner.Current,frame,EngineeringRefinementInteractionKind.Preview,command)));
            }
            ShowCampaignDialog(project==null?"Choose target research":"Continue development",(retained?.DisplayName??RefinementModuleName(observed,pending!)+" · use this exact outcome as the next parent")+"\nChoose a target to check its actual range and availability.","Close",null,choices.ToArray());
            AppendRefinementVector(pending?.Outcome??retained?.Vector,null);
        }
        private void ReviewEngineeringGoal(ReviewUiModel owner,ReviewFrame frame,EngineeringRefinementCommandDto command,EngineeringRefinementPreview preview)
        {
            if(confirmation!=null||!RefinementReviewBoundary(owner,frame))return;
            if(!preview.CanStart||preview.Parent==null)
            {
                ShowCampaignDialog("Engineering target unavailable",EngineeringRefinementPresentation.Goal(command.Goal)+"\n"+EngineeringRefinementPresentation.Reason(preview.Error),"Back to targets",()=>ChooseEngineeringGoal(owner,frame,command.ParentVersionId,command.ProjectId));return;
            }
            command.ContextBlueprintId=preview.ContextBlueprintId;command.ContextBlueprintRevision=preview.ContextBlueprintRevision;
            var range=preview.TargetMinimum.HasValue&&preview.TargetMaximum.HasValue?preview.TargetMinimum==preview.TargetMaximum?preview.TargetMinimum.Value.ToString():preview.TargetMinimum+"–"+preview.TargetMaximum:"Not reported";
            ShowCampaignDialog("Review targeted Engineering","Target research · "+EngineeringRefinementPresentation.Goal(command.Goal)+"\nExpected outcome · "+EngineeringRefinementPresentation.Target(preview.Parent,command.Goal)+" → "+range+" "+EngineeringRefinementPresentation.Unit(command.Goal)+"\nEngineering work · "+EngineeringRefinementPresentation.Work(preview.Work)+" points"+(command.Action==EngineeringRefinementAction.Continue?"\nThe current result stays internal; this starts another normal paid project.":""),"Back to targets",()=>ChooseEngineeringGoal(owner,frame,command.ParentVersionId,command.ProjectId),new CampaignDialogAction(command.Action==EngineeringRefinementAction.Continue?"Start continued development":"Start targeted refinement",()=>SubmitRefinementDecision(owner,frame,command)));
            AppendRefinementVector(preview.Parent,null);
        }
        private void OpenEngineeringProject(ReviewUiModel owner,ReviewFrame frame,string id)
        {
            if(confirmation!=null||!RefinementReviewBoundary(owner,frame))return;
            var observed=RefinementSnapshot(owner,frame);var project=observed?.Projects.FirstOrDefault(p=>p.ProjectId==id);if(project==null)return;
            var choices=new List<CampaignDialogAction>();
            if(project.Outcome!=null)
            {
                refinementAnnounced.Add(project.ProjectId);
                if(RefinementEditable(owner,frame))
                {
                    if(project.CanAccept)choices.Add(new CampaignDialogAction("Accept engineering outcome",()=>SubmitRefinementDecision(owner,frame,RefinementCommand(frame,EngineeringRefinementAction.Accept,null,project.Goal,id))));
                    if(project.CanContinue)choices.Add(new CampaignDialogAction(EngineeringRefinementPresentation.HasDownside(project)?"Continue development to reduce downsides":"Continue development",()=>ChooseEngineeringGoal(owner,frame,null,id)));
                    if(project.CanDiscard)choices.Add(new CampaignDialogAction("Discard outcome",()=>SubmitRefinementDecision(owner,frame,RefinementCommand(frame,EngineeringRefinementAction.Discard,null,project.Goal,id))));
                }
                var success=EngineeringRefinementPresentation.Improved(project.Before,project.Outcome,project.Goal);
                ShowCampaignDialog(RefinementModuleName(observed!,project)+" · Engineering outcome","Result · "+(success?"Success":"Improvement not confirmed")+"\nTarget research · "+EngineeringRefinementPresentation.Goal(project.Goal)+"\nImprovement · "+EngineeringRefinementPresentation.Improvement(project)+"\nSide effects · "+EngineeringRefinementPresentation.SideEffects(project)+(project.Stage==EngineeringRefinementStage.RetainedInternal?"\nRetained for development · not released":""),"Close",null,choices.ToArray());
                AppendRefinementVector(project.Outcome,project.Before);
            }
            else
            {
                if(RefinementEditable(owner,frame))
                {
                    if(project.CanPause)choices.Add(new CampaignDialogAction("Pause development",()=>SubmitRefinementDecision(owner,frame,RefinementCommand(frame,EngineeringRefinementAction.Pause,null,project.Goal,id))));
                    if(project.CanResume)choices.Add(new CampaignDialogAction("Resume development",()=>SubmitRefinementDecision(owner,frame,RefinementCommand(frame,EngineeringRefinementAction.Resume,null,project.Goal,id))));
                }
                ShowCampaignDialog(RefinementModuleName(observed!,project)+" · Development","Target research · "+EngineeringRefinementPresentation.Goal(project.Goal)+"\n"+(project.IsActive?"Active":"Paused")+" · Work completed "+EngineeringRefinementPresentation.Work(project.Progress)+" / "+EngineeringRefinementPresentation.Work(project.Work)+" points","Close",null,choices.ToArray());
                AppendRefinementVector(project.Before,null);
            }
        }
        private void SubmitRefinementDecision(ReviewUiModel owner,ReviewFrame frame,EngineeringRefinementCommandDto command)
        {
            if(!RefinementReviewBoundary(owner,frame)||!RefinementEditable(owner,frame))return;
            RunEngineeringRefinementInteraction(owner,owner.Current,frame,EngineeringRefinementInteractionKind.Submission,command);
        }
        private async void RunEngineeringRefinementInteraction(ReviewUiModel owner,ReviewController controller,ReviewFrame frame,
            EngineeringRefinementInteractionKind kind,EngineeringRefinementCommandDto command)
        {
            if(confirmation!=null||routinePendingToken!=null||!RefinementCurrent(owner,frame)||!ReferenceEquals(owner.Current,controller)
                ||!RefinementReviewBoundary(owner,frame)||kind==EngineeringRefinementInteractionKind.Submission&&!RefinementEditable(owner,frame))return;
            var work=owner.BeginEngineeringRefinementInteraction(kind,command);
            if(work==null){refinementNotice=owner.Message;Draw();return;}
            var epoch=viewEpoch;var mounted=root;var mountedPanel=root?.panel;var modal=confirmation;
            refinementUiWork=work;refinementUiOwner=owner;refinementUiTitle=kind==EngineeringRefinementInteractionKind.Preview?"Checking Engineering target…":"Applying Engineering decision…";
            var saved=new RoutineViewState();
            try
            {
                saved=ShowRoutinePending(work,refinementUiTitle);
                var completion=await Task.Run(work.Execute);
                if(this==null||!isActiveAndEnabled||viewEpoch!=epoch||!ReferenceEquals(root,mounted)||mountedPanel==null||!ReferenceEquals(mounted?.panel,mountedPanel)||!ReferenceEquals(model,owner)
                    ||!ReferenceEquals(owner.Current,controller)||!ReferenceEquals(controller.Frame,frame)||!ReferenceEquals(work.SourceFrame,frame)
                    ||!ReferenceEquals(confirmation,modal)||!ReferenceEquals(refinementUiWork,work)||!ReferenceEquals(refinementUiOwner,owner))return;
                var finished=owner.CompleteEngineeringRefinementInteraction(work,completion);
                HideRoutinePending(work);refinementUiWork=null;refinementUiOwner=null;
                refinementNotice=finished&&kind==EngineeringRefinementInteractionKind.Preview?"Engineering target checked.":finished&&completion.Result?.Accepted==true?"Engineering decision accepted.":completion.Result!=null?EngineeringRefinementPresentation.Reason(completion.Result.Error):"Engineering interaction unavailable; no decision was published.";
                ReceiveRoutineView(saved,!ReferenceEquals(frame,owner.Current.Frame));
                if(finished&&completion.Committed)CapturePublishedCompletions(owner,frame);
                if(finished&&kind==EngineeringRefinementInteractionKind.Preview&&completion.Preview!=null&&ReferenceEquals(owner.Current.Frame,frame))
                    ReviewEngineeringGoal(owner,frame,work.Command,completion.Preview);
                else if(!completion.Committed&&ReferenceEquals(owner.Current.Frame,frame)&&confirmation==null)
                    ShowCampaignDialog("Engineering decision",refinementNotice,"Close",null);
            }
            finally
            {
                owner.AbandonEngineeringRefinementInteraction(work);HideRoutinePending(work);
                if(ReferenceEquals(refinementUiWork,work))
                {
                    refinementUiWork=null;refinementUiOwner=null;
                    if(this!=null&&isActiveAndEnabled&&ReferenceEquals(model,owner))
                    {if(viewEpoch==epoch)ReceiveRoutineView(saved,false);else Draw();}
                }
            }
        }
        private void AppendRefinementVector(EngineeringRefinementVectorSnapshot? value,EngineeringRefinementVectorSnapshot? before)
        {
            var body=confirmation?.Q<ScrollView>("player-confirmation-details");if(body==null||value==null)return;
            var details=Type.GetType("HobbyDS9.UnityClient.LcarsCapacityDetails, HobbyDS9.UnityClient");
            var maturity=before==null?EngineeringRefinementPresentation.Maturity(value.MaturityMilli):EngineeringRefinementPresentation.Maturity(before.MaturityMilli)+" → "+EngineeringRefinementPresentation.Maturity(value.MaturityMilli);
            var complete=details?.GetMethod("EngineeringModule")?.Invoke(null,new object?[]{value.Component,before?.Component,maturity}) as VisualElement;
            if(complete!=null){complete.name="engineering-generated-values";body.Add(complete);return;}
            var grid=new VisualElement{name="engineering-generated-values"};grid.style.flexDirection=FlexDirection.Row;grid.style.flexWrap=Wrap.Wrap;body.Add(grid);
            void Capacity(string label,long current,long? previous,bool supply)
            {
                var type=Type.GetType("HobbyDS9.UnityClient.LcarsCapacityMeter, HobbyDS9.UnityClient");
                var meter=type?.GetConstructor(new[]{typeof(string),typeof(long),typeof(long),typeof(long?),typeof(bool)})?.Invoke(new object?[]{label,current,Math.Max(current,previous??current),previous,supply}) as VisualElement;
                if(meter!=null){type!.GetMethod("Compact")?.Invoke(meter,null);meter.style.flexBasis=Length.Percent(48);meter.style.flexGrow=1;meter.style.minWidth=170;meter.style.marginRight=8;grid.Add(meter);}
                else grid.Add(Readout(label+" · "+(previous.HasValue?previous+" → "+current:current.ToString())+" · capacity blocks unavailable"));
            }
            Capacity("Hull space",value.Capacity,before?.Capacity,false);Capacity("Power / reactor output",value.PowerSupply,before?.PowerSupply,true);
            body.Add(Readout("Industry cost · "+(before==null?value.Industry.ToString():before.Industry+" → "+value.Industry)));
            body.Add(Readout("Version maturity · "+maturity));
            var mode=value.Component.Modes.FirstOrDefault(m=>m.Profile==HobbyDS9.Contracts.ShipDesign.OperatingProfileKind.Cruise);
            if(mode!=null)
            {
                body.Add(Readout("Cruise power demand · full "+mode.FullPower+" · minimum "+mode.MinimumPower));
                body.Add(Readout("Cruise output · "+(mode.Outputs.Count==0?"No rated output":string.Join(" · ",mode.Outputs.Select(o=>System.Text.RegularExpressions.Regex.Replace(o.Kind.ToString(),"([a-z])([A-Z])","$1 $2")+" "+o.Amount)))));
            }
            else body.Add(Readout("Cruise demand / other outputs · Not reported"));
        }
        private void ScheduleEngineeringOutcomeDialog()
        {
            if(model==null||root==null||!model.SupportsEngineeringRefinement||ReferenceEquals(refinementScheduledRoot,root))return;
            var target=root;refinementScheduledRoot=target;
            target.schedule.Execute(()=>
            {
                if(ReferenceEquals(refinementScheduledRoot,target))refinementScheduledRoot=null;
                var owner=model;if(owner==null||!ReferenceEquals(root,target)||!isActiveAndEnabled||owner.Busy||owner.Closed||confirmation!=null||campaignBusyToken!=null||routinePendingToken!=null||demoLanding||campaignSettingsReturn!=null||!demoStarted)return;
                var frame=owner.Current.Frame;if(!RefinementEditable(owner,frame)||turnCompletions.Peek(owner,NoticeBoundary(frame))!=null||!RefinementReviewBoundary(owner,frame))return;
                SynchronizeRefinementSession(owner,frame);var observed=RefinementSnapshot(owner,frame);
                var outcome=observed?.Projects.FirstOrDefault(p=>p.Stage==EngineeringRefinementStage.Pending&&p.Outcome!=null&&!refinementAnnounced.Contains(p.ProjectId));
                if(outcome!=null)OpenEngineeringProject(owner,frame,outcome.ProjectId);
            });
        }
    }
}
