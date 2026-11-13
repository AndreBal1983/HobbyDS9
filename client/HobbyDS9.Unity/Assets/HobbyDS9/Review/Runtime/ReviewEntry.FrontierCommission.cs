#nullable enable
using System;
using HobbyDS9.Contracts;
using UnityEngine.UIElements;
namespace HobbyDS9.UnityReview
{
    public sealed partial class ReviewEntry
    {
        private bool frontierCommissionOpen;
        private ReviewUiModel? frontierCommissionOwner;
        private void AddFrontierCommissionNavigation(VisualElement nav)
        {
            if((model?.StreamedActivitiesCampaign!=true&&model?.ColonisationCampaign!=true))return;
            var owner=model;
            var button=AddButton(nav,"Mission",()=>{
                if(!ReferenceEquals(model,owner)||workspace==null||confirmation!=null||!workspace.Navigate(workspace.Page))return;
                frontierCommissionOwner=owner;frontierCommissionOpen=true;viewEpoch++;Build();
            });
            button.name="frontier-commission-summary";button.SetEnabled(workspace?.CanNavigate==true);
        }
        private FrontierCommissionSnapshot? UpdateFrontierCommissionSummary()
        {
            if(demoLanding||(model?.StreamedActivitiesCampaign!=true&&model?.ColonisationCampaign!=true)){frontierCommissionOpen=false;return null;}
            if(!ReferenceEquals(frontierCommissionOwner,model)){frontierCommissionOpen=false;frontierCommissionOwner=model;}
            var report=model.FrontierCommission();var button=root!.Q<Button>("frontier-commission-summary");
            if(button!=null){button.text=report.Availability==FrontierCommissionAvailability.Available?"Mission "+report.CompletedObjectives+"/4":"Mission";button.tooltip=report.Completed?"Frontier Commission completed. You can continue exploring.":"Review your frontier mission and its actual reports.";}
            return report;
        }
        private void DrawFrontierCommission(FrontierCommissionSnapshot report)
        {
            playerTitle!.text="Frontier Commission";
            var panel=PagePanel("Frontier Commission");panel.name="frontier-commission-report";panel.style.flexGrow=1;panel.style.minHeight=0;
            if(report.Availability!=FrontierCommissionAvailability.Available)
            {panel.Add(Readout("This commission is not available for the current activity content. Your sandbox and normal activities remain available."));return;}
            var outcome=Readout(report.Completed?"Frontier Commission completed - week "+report.CompletedTurn+". Continue exploring, building and meeting your neighbours.":"Your commission - "+report.CompletedObjectives+" of 4 milestones completed");outcome.name="frontier-commission-outcome";outcome.AddToClassList("console-section");panel.Add(outcome);
            panel.Add(Readout("Establish frontier capacity, launch a ship from your own yards, bring home a finite scientific result, and assess an independent society on its own terms."));
            var objectives=new VisualElement();objectives.AddToClassList("mission-objective-grid");panel.Add(objectives);
            foreach(var objective in report.Objectives)
            {
                var card=new VisualElement{name="commission-objective-"+objective.Id};card.AddToClassList("mission-objective");card.style.marginTop=12;card.style.flexShrink=0;objectives.Add(card);
                var title=Readout((objective.Progress==FrontierCommissionProgress.Completed?"Completed - ":objective.Progress==FrontierCommissionProgress.Underway?"Underway - ":"To do - ")+objective.Caption);title.style.fontSize=18;card.Add(title);
                card.Add(Readout(objective.Guidance));var evidence=objective.Evidence;
                if(evidence!=null)
                {
                    card.Add(Readout(evidence.Label+" - week "+evidence.Turn));
                    var details=new Foldout{text="Recorded result",value=false};card.Add(details);details.Add(Readout(evidence.Report));
                    if(objective.Id=="science")card.Add(Readout("Research learned: "+(evidence.LearnedInternalUnits/(double)ResearchUnits.Scale).ToString("0.##",System.Globalization.CultureInfo.InvariantCulture)+" points; beyond the current field limit: "+(evidence.UnlearnableInternalUnits/(double)ResearchUnits.Scale).ToString("0.##",System.Globalization.CultureInfo.InvariantCulture)+" points."));
                }
            }
            var links=new VisualElement();links.AddToClassList("review-field-row");links.style.flexShrink=0;panel.Add(links);
            var owner=model!;var controller=owner.Current;var frame=controller.Frame;var epoch=viewEpoch;
            void Link(string caption,string id,ReviewWorkspacePage page,bool expeditions=false)
            {
                Button? button=null;
                button=AddButton(links,caption,()=>{
                    if(button==null||root?.Contains(button)!=true||epoch!=viewEpoch||!frontierCommissionOpen
                        ||!ReferenceEquals(model,owner)||!ReferenceEquals(owner.Current,controller)||!ReferenceEquals(controller.Frame,frame)
                        ||owner.Busy||owner.Closed||confirmation!=null||workspace?.CanNavigate!=true)return;
                    Navigate(page);
                    if(frontierCommissionOpen||workspace?.Page!=page||page!=ReviewWorkspacePage.Production)return;
                    if(owner.ColonisationCampaign)root?.Q<ReviewColonisationPanel>()?.OpenMissionPage(expeditions);
                    else root?.Q<ReviewActivitiesPanel>()?.RestoreSelection(new ReviewActivitiesPanel.Selection(frame.Actor,expeditions,null,null,null));
                });
                button.name=id;button.SetEnabled(workspace?.CanNavigate==true&&!owner.Busy&&!owner.Closed);
            }
            Link("Open world construction","commission-open-worlds",ReviewWorkspacePage.Production);
            Link("Open expeditions","commission-open-expeditions",ReviewWorkspacePage.Production,true);
            Link("Explore known routes","commission-open-map",ReviewWorkspacePage.Map);
            panel.Add(Readout("Progress records what your campaign actually accomplished. Refusal is a valid contact assessment; cooperation is never required. Failed or aborted science attempts remain in your reports and do not count as completed studies."));
        }
    }
}
