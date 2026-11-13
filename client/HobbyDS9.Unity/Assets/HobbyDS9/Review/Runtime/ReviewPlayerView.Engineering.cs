#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using HobbyDS9.Application;
using HobbyDS9.Contracts;
using UnityEngine;
using UnityEngine.UIElements;

namespace HobbyDS9.UnityReview
{
    public sealed partial class ReviewPlayerView
    {
        private static string EngineeringKind(EngineeringProjectKind kind)=>kind==EngineeringProjectKind.NewCapability?"New capability":kind==EngineeringProjectKind.Refinement?"Refinement":"Civil application";
        private static string EngineeringState(EngineeringCardSnapshot p)=>p.Band==EngineeringMaturityBand.Mature?"Completed":p.IsTarget?"In progress":p.CanTarget?"Available":
            (p.UnavailableReason==CommandErrorCode.EngineeringScienceRequirementMissing||p.UnavailableReason==CommandErrorCode.EngineeringPrerequisiteMissing?"Locked · ":"Unavailable · ")+Reason(p.UnavailableReason);
        private void Engineering(VisualElement root,CampaignIntegratedStrategySnapshot facts)
        {
            var frame=model.Current.Frame;var navigation=workspace.NavigationEpoch;var r=facts.Research;
            var d=model.ResearchInputs()!;bool edit=Editable&&d.Current(frame);
            bool Current()=>root.panel!=null&&ReferenceEquals(frame,model.Current.Frame)&&workspace.NavigationEpoch==navigation&&workspace.Page==ReviewWorkspacePage.Research&&!model.Busy&&!model.Closed;
            var page=new VisualElement{name="engineering-workbench"};page.AddToClassList("engineering-workbench");root.Add(page);
            var toolbar=Row(page,"console-tabs");
            Btn(toolbar,"Science","research-tab-science",()=>perform(()=>workspace.ResearchTab="science"));
            Btn(toolbar,"Engineering","research-tab-engineering",()=>{}).AddToClassList("selected-tab");
            Btn(toolbar,workspace.EngineeringAllocationOpen?"Back to projects":"R&D allocation","engineering-allocation",()=>perform(()=>workspace.EngineeringAllocationOpen=!workspace.EngineeringAllocationOpen));
            var overview=Row(page,"engineering-overview");overview.name="engineering-current-summary";
            void Metric(string label,string value,string id)
            {var cell=new VisualElement();cell.AddToClassList("engineering-overview-cell");overview.Add(cell);Text(cell,label,"engineering-overview-label");Text(cell,value,"engineering-overview-value",id);}
            var current=r.Engineering.FirstOrDefault(p=>p.IsTarget);
            Metric("At current accepted allocation",Units(r.EngineeringAllocation)+" points / week","engineering-income");
            Metric("Banked Engineering",r.EngineeringReserveConceptual+" Engineering points","engineering-reserve");
            Metric("Current project",current?.Definition.DisplayName??"No active project","engineering-active-name");
            if(workspace.EngineeringAllocationOpen)
            {
                var allocation=Panel(page,"R&D allocation");ResearchDirection(allocation,r,d,edit);return;
            }
            if(r.Engineering.Count==0)
            {Text(page,"No Engineering projects in this campaign.","console-intro");Btn(page,"Open Science","open-other-research",()=>perform(()=>workspace.ResearchTab="science"));return;}
            var selected=r.Engineering.FirstOrDefault(p=>p.Definition.DefinitionId.Value==workspace.EngineeringProjectId)
                ??current??r.Engineering.FirstOrDefault(p=>p.CanTarget)??r.Engineering.OrderBy(p=>p.Definition.DisplayName,StringComparer.Ordinal).First();
            workspace.EngineeringProjectId=selected.Definition.DefinitionId.Value;
            var columns=Row(page,"engineering-columns");
            columns.RegisterCallback<GeometryChangedEvent>(e=>{if(ReferenceEquals(e.target,columns))columns.EnableInClassList("engineering-stacked",e.newRect.width<640);});
            var catalogue=new VisualElement{name="engineering-catalogue"};catalogue.AddToClassList("engineering-catalogue");columns.Add(catalogue);
            Text(catalogue,"Projects · "+r.Engineering.Count,"console-section");
            var search=new TextField("Search projects"){name="engineering-search",value=workspace.EngineeringSearch};catalogue.Add(search);
            var kinds=new DropdownField("Kind",new List<string>{"All kinds","New capability","Refinement","Civil application"},Math.Max(0,Math.Min(3,workspace.EngineeringKind))){name="engineering-kind-filter"};catalogue.Add(kinds);
            var goals=new ScrollView(ScrollViewMode.Vertical){name="engineering-goal-list"};goals.AddToClassList("engineering-goal-list");catalogue.Add(goals);
            var entries=new List<(EngineeringCardSnapshot project,Button button)>();
            foreach(var p in r.Engineering.OrderBy(p=>p.Definition.Kind).ThenBy(p=>p.Definition.DisplayName,StringComparer.Ordinal))
            {
                var id=p.Definition.DefinitionId.Value;
                var goal=Btn(goals,"","engineering-goal-"+id,()=>{if(Current())perform(()=>workspace.EngineeringProjectId=id);});goal.AddToClassList("engineering-goal");
                goal.EnableInClassList("selected-nav",ReferenceEquals(p,selected));goal.tooltip=p.Definition.EffectSummary;
                Text(goal,p.Definition.DisplayName,"engineering-goal-name");
                Text(goal,EngineeringState(p),"engineering-goal-state");
                Text(goal,Units(p.Progress)+" / "+Units(p.Cost)+" points · "+p.MaturityPercent+"%","engineering-goal-progress");entries.Add((p,goal));
            }
            var empty=Text(goals,"No projects match these filters.",id:"engineering-filter-empty");
            void Filter()
            {
                var count=0;
                foreach(var row in entries)
                {
                    bool match=(workspace.EngineeringKind==0||(int)row.project.Definition.Kind==workspace.EngineeringKind)
                        &&(workspace.EngineeringSearch.Length==0||row.project.Definition.DisplayName.IndexOf(workspace.EngineeringSearch,StringComparison.OrdinalIgnoreCase)>=0
                            ||row.project.Definition.EffectSummary.IndexOf(workspace.EngineeringSearch,StringComparison.OrdinalIgnoreCase)>=0);
                    row.button.style.display=match?DisplayStyle.Flex:DisplayStyle.None;if(match)count++;
                }
                empty.style.display=count==0?DisplayStyle.Flex:DisplayStyle.None;
            }
            search.RegisterValueChangedCallback(e=>{if(Current()){workspace.EngineeringSearch=e.newValue;Filter();}});
            kinds.RegisterValueChangedCallback(_=>{if(Current()){workspace.EngineeringKind=kinds.index;Filter();}});Filter();
            var detail=new ScrollView(ScrollViewMode.Vertical){name="engineering-project-"+selected.Definition.DefinitionId};detail.AddToClassList("engineering-selected-detail");detail.AddToClassList("console-work");columns.Add(detail);
            Text(detail,EngineeringKind(selected.Definition.Kind),"engineering-overview-label");
            Text(detail,selected.Definition.DisplayName,"console-section","engineering-project-heading");
            var demo=frame.Demo!=null;
            if(demo)
            {
                var designs=string.Join(", ",facts.Construction.Select(o=>ShipName(o.Design)).Distinct());
                Text(detail,"Demo construction: "+(designs.Length==0?"no ship designs offered":designs+" only")+". Other class unlocks are unavailable here.",id:"engineering-demo-construction");
            }
            var scoutExcluded=model.ColonisationCycleCampaign&&selected.Definition.DefinitionId.Value=="engineering.advanced-starship-engineering";
            var result=new VisualElement();result.AddToClassList("engineering-result");detail.Add(result);
            Text(result,scoutExcluded?"Advanced Scout is unavailable in this campaign's construction catalogue.":selected.Definition.EffectSummary,
                "console-copy",scoutExcluded?"engineering-cycle-construction":"engineering-result-effect");
            Text(result,"New version access does not refit existing ships or published designs. Fitting and construction use the current catalogue.");
            Text(detail,selected.Band==EngineeringMaturityBand.Mature?"Completed":selected.IsTarget?"Current project":EngineeringState(selected),id:"engineering-project-state");
            detail.Add(new ProgressBar{value=selected.MaturityPercent});
            Text(detail,"Earned progress: "+Units(selected.Progress)+" / "+Units(selected.Cost)+" Engineering points","console-metric","engineering-project-progress");
            Text(detail,"Project cost: "+Units(selected.Cost)+" Engineering points",id:"engineering-project-cost");
            Text(detail,"Requirements met: Science "+selected.ScienceSatisfied.Count(v=>v)+" / "+selected.ScienceSatisfied.Count+"; applications "+selected.PrerequisitesSatisfied.Count(v=>v)+" / "+selected.PrerequisitesSatisfied.Count,id:"engineering-requirement-summary");
            var requirements=new VisualElement{name="engineering-named-requirements"};detail.Add(requirements);
            for(int i=0;i<selected.Definition.ScienceRequirements.Count;i++)
            {
                var q=selected.Definition.ScienceRequirements[i];
                Text(requirements,(selected.ScienceSatisfied[i]?"✓ Met · ":"○ Missing · ")+q.Discipline+" · level "+q.IntervalStartLevel+" → "+(q.IntervalStartLevel+1)+" · discovery "+q.BreakthroughOrdinal);
            }
            for(int i=0;i<selected.Definition.Prerequisites.Count;i++)
            {
                var required=r.Engineering.FirstOrDefault(p=>p.Definition.DefinitionId==selected.Definition.Prerequisites[i]);
                Text(requirements,(selected.PrerequisitesSatisfied[i]?"✓ Met · ":"○ Missing · ")+(required?.Definition.DisplayName??"Required Engineering project"));
            }
            void Select(string? target)
            {
                if(!Current())return;
                void Apply()=>perform(()=>model.Engineering(target,ReviewUiModel.NewCommandId()));
                if(model.ColonisationCampaign)Apply();else confirm("Change Engineering target? Earned progress in both projects is retained. Work advances on End turn.",Apply);
            }
            var actions=Row(detail,"engineering-actions");
            var label=selected.Band==EngineeringMaturityBand.Mature?"Completed":selected.IsTarget?"Current project":current!=null?"Switch project":selected.Progress>0?"Resume project":"Start project";
            Btn(actions,label,"integrated-develop-"+selected.Definition.DefinitionId,()=>Select(selected.Definition.DefinitionId.Value),edit&&selected.CanTarget,true);
            Btn(actions,"Review Science requirements","engineering-review-science",()=>perform(()=>{
                workspace.ScienceDetails=selected.Definition.ScienceRequirements.FirstOrDefault()?.Discipline;workspace.ResearchTab="science";
            }));
            if(current!=null)Btn(actions,"Pause project","integrated-clear-engineering",()=>Select(null),edit);
            Text(detail,"One active project. Selecting spends nothing immediately; work advances on End turn. Switching or pausing retains all earned progress.");
            var technical=new Foldout{name="engineering-project-details",text="Authored effect & maturity details",value=false};detail.Add(technical);
            Text(technical,scoutExcluded?"Original catalogue effect; the Scout exclusion above applies here.":demo?"Authored effect; the demo construction limits above still apply.":"Authored effect; current campaign construction and fitting limits still apply.",id:"engineering-effect-qualifier");
            Text(technical,selected.Definition.EffectSummary,id:"engineering-authored-effect");Text(technical,selected.MaturityPercent+"% · "+selected.Band);
            var receipts=new Foldout{text="Latest turn: Engineering",value=false};detail.Add(receipts);EngineeringTurnReport(receipts,r);
            if(!edit)Text(detail,demo&&frame.Demo!.Outcome!=CampaignDemoOutcome.Active?"Campaign complete. Research is available for inspection; further changes are unavailable.":"Research is read-only at this boundary. Finish pending combat or resolve unsent orders before changing projects.",id:"research-readonly-reason");
        }
    }
}
