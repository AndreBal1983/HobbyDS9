#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using HobbyDS9.Application;
using HobbyDS9.Contracts;
using UnityEngine.UIElements;

namespace HobbyDS9.UnityReview
{
    public sealed partial class ReviewPlayerView
    {
        public Func<VisualElement,bool>? RenderGeneratedEngineeringRefinement {get;set;}
        private static string EngineeringState(EngineeringCardSnapshot p)=>p.Band==EngineeringMaturityBand.Mature?"Completed":p.IsTarget?"Active":p.CanTarget?"Available":
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
            var overview=Row(page,"engineering-overview");overview.name="engineering-current-summary";
            void Metric(string label,string value,string id)
            {var cell=new VisualElement();cell.AddToClassList("engineering-overview-cell");overview.Add(cell);Text(cell,label,"engineering-overview-label");Text(cell,value,"engineering-overview-value",id);}
            var current=r.Engineering.FirstOrDefault(p=>p.IsTarget);
            var generated=model.SupportsEngineeringRefinement?model.CaptureEngineeringRefinement(frame.Actor):null;
            var active=generated?.Projects.FirstOrDefault(p=>p.IsActive);
            var currentName=current?.Definition.DisplayName??"No active project";
            if(model.SupportsEngineeringRefinement&&generated==null)currentName="Temporarily unavailable";
            if(active!=null)currentName=(generated!.Parents.FirstOrDefault(p=>p.Vector.VersionId==active.Before.VersionId)?.DisplayName??"Module development")+" · "+EngineeringRefinementPresentation.Goal(active.Goal);
            Metric("At current accepted allocation",Units(r.EngineeringAllocation)+" points / week","engineering-income");
            Metric("Banked Engineering",r.EngineeringReserveConceptual+" Engineering points","engineering-reserve");
            Metric("Current project",currentName,"engineering-active-name");
            var sections=new VisualElement{name="engineering-sections"};sections.AddToClassList("engineering-sections");page.Add(sections);
            sections.RegisterCallback<GeometryChangedEvent>(e=>{if(ReferenceEquals(e.target,sections))sections.EnableInClassList("engineering-stacked",e.newRect.width<600);});
            Section(false,"New engineering projects","new");Section(true,"Technology refinement","refinement");
            var receipts=new Foldout{text="Latest turn: Engineering",value=false};page.Add(receipts);EngineeringTurnReport(receipts,r);
            void Section(bool refinement,string heading,string key)
            {
                var section=new VisualElement{name="engineering-section-"+key};section.AddToClassList("engineering-group");sections.Add(section);
                if(refinement&&RenderGeneratedEngineeringRefinement?.Invoke(section)==true)return;
                var projects=r.Engineering.Where(p=>EngineeringPresentation.IsRefinement(p.Definition.Kind)==refinement&&EngineeringPresentation.Visible(p,model.ColonisationCycleCampaign)).OrderBy(p=>p.Definition.DisplayName,StringComparer.Ordinal).ToList();
                var title=Text(section,heading+" · "+projects.Count,"console-section");
                string Search()=>refinement?workspace.EngineeringRefinementSearch:workspace.EngineeringNewSearch;
                string Category()=>refinement?workspace.EngineeringRefinementCategory:workspace.EngineeringNewCategory;
                var filters=new VisualElement();filters.AddToClassList("engineering-filters");section.Add(filters);
                var search=new TextField("Search projects"){name="engineering-search-"+key,value=Search()};filters.Add(search);
                var categories=new List<string>{"All technologies"};categories.AddRange(projects.Select(p=>EngineeringPresentation.Category(p.Definition.DefinitionId.Value)).Distinct().OrderBy(v=>v,StringComparer.Ordinal));
                if(!categories.Contains(Category())){if(refinement)workspace.EngineeringRefinementCategory="All technologies";else workspace.EngineeringNewCategory="All technologies";}
                Action filter=()=>{};
                var category=new ReviewLcarsSelector("Technology",categories,Category(),value=>{if(Current()){if(refinement)workspace.EngineeringRefinementCategory=value;else workspace.EngineeringNewCategory=value;filter();}},Current){name="engineering-category-"+key};filters.Add(category);category.SetEnabled(categories.Count>1);
                var grid=new ScrollView(ScrollViewMode.Vertical){name="engineering-projects-"+key,horizontalScrollerVisibility=ScrollerVisibility.Hidden,verticalScrollerVisibility=ScrollerVisibility.Auto};grid.AddToClassList("engineering-project-grid");section.Add(grid);
                var entries=new List<(EngineeringCardSnapshot project,Button button)>();
                foreach(var p in projects)
                {
                    var id=p.Definition.DefinitionId.Value;
                    var goal=Btn(grid,"","engineering-goal-"+id,()=>{if(Current()){workspace.EngineeringProjectId=id;Open(p);}});goal.AddToClassList("engineering-goal");
                    goal.EnableInClassList("selected-nav",p.IsTarget);goal.tooltip=p.Definition.EffectSummary;
                    Text(goal,p.Definition.DisplayName,"engineering-goal-name");
                    var state=Text(goal,EngineeringState(p)+(EngineeringPresentation.SupportedOutput(p,model.ColonisationCycleCampaign)?"":" · catalogue output unavailable"),"engineering-goal-state");
                    state.AddToClassList(p.IsTarget?"engineering-state-active":p.Band==EngineeringMaturityBand.Mature?"engineering-state-completed":"engineering-state-available");
                    Text(goal,Units(p.Progress)+" / "+Units(p.Cost)+" points · "+p.MaturityPercent+"% complete","engineering-goal-progress");entries.Add((p,goal));
                }
                var empty=Text(grid,"No projects available.",id:"engineering-filter-empty-"+key);
                void Filter()
                {
                    var count=0;
                    foreach(var row in entries)
                    {
                        var p=row.project;var term=Search();
                        bool match=(Category()=="All technologies"||EngineeringPresentation.Category(p.Definition.DefinitionId.Value)==Category())
                            &&(term.Length==0||p.Definition.DisplayName.IndexOf(term,StringComparison.OrdinalIgnoreCase)>=0||p.Definition.EffectSummary.IndexOf(term,StringComparison.OrdinalIgnoreCase)>=0);
                        row.button.style.display=match?DisplayStyle.Flex:DisplayStyle.None;if(match)count++;
                    }
                    empty.style.display=count==0?DisplayStyle.Flex:DisplayStyle.None;
                    title.text=heading+" · "+count;
                }
                search.RegisterValueChangedCallback(e=>{if(Current()){if(refinement)workspace.EngineeringRefinementSearch=e.newValue;else workspace.EngineeringNewSearch=e.newValue;Filter();}});
                filter=Filter;Filter();
            }
            void Select(string? target)
            {
                if(!Current())return;
                void Apply()=>perform(()=>model.Engineering(target,ReviewUiModel.NewCommandId()));
                if(model.ColonisationCampaign)Apply();else confirm("Change Engineering target? Earned progress in both projects is retained. Work advances on End turn.",Apply);
            }
            void Open(EngineeringCardSnapshot selected)
            {
                var parts=new List<string>{EngineeringState(selected)+" · "+EngineeringPresentation.Category(selected.Definition.DefinitionId.Value)};
                var scoutExcluded=model.ColonisationCycleCampaign&&selected.Definition.DefinitionId.Value=="engineering.advanced-starship-engineering";
                parts.Add(scoutExcluded?"Expected outcome · unavailable\nAdvanced Scout is not buildable in this campaign.":"Target research · "+(selected.Definition.Outputs.Count==0?selected.Definition.DisplayName:string.Join(" / ",selected.Definition.Outputs.Select(EngineeringPresentation.Target)))+"\nExpected outcome · "+(selected.Definition.Outputs.Count==0?selected.Definition.EffectSummary:string.Join(" / ",selected.Definition.Outputs.Select(EngineeringPresentation.Expected))));
                if(frame.Demo!=null)parts.Add("Demo construction: "+string.Join(", ",facts.Construction.Select(o=>ShipName(o.Design)).Distinct())+" only. Other class unlocks are unavailable here.");
                parts.Add("Project completion · "+selected.MaturityPercent+"%\nEngineering work · "+Units(selected.Progress)+" / "+Units(selected.Cost));
                if(selected.Band!=EngineeringMaturityBand.Mature&&(selected.IsTarget||selected.CanTarget))
                {
                    var phases=EngineeringPresentation.ResearchPhases(selected.Cost,selected.Progress,r.EngineeringReserve,r.EngineeringAllocation);
                    parts.Add("Estimated completion · "+(!phases.HasValue?"unavailable at current income":phases==1?"next Research phase":phases+" weekly Research phases")+" · current income/bank, if selected now");
                }
                var requirements=new List<string>();
                for(int i=0;i<selected.Definition.ScienceRequirements.Count;i++)
                {var q=selected.Definition.ScienceRequirements[i];requirements.Add((selected.ScienceSatisfied[i]?"Met · ":"Missing · ")+q.Discipline+" · level "+q.IntervalStartLevel+" → "+(q.IntervalStartLevel+1)+" · discovery "+q.BreakthroughOrdinal);}
                for(int i=0;i<selected.Definition.Prerequisites.Count;i++)
                {var required=r.Engineering.FirstOrDefault(p=>p.Definition.DefinitionId==selected.Definition.Prerequisites[i]);requirements.Add((selected.PrerequisitesSatisfied[i]?"Met · ":"Missing · ")+(required?.Definition.DisplayName??"Required project"));}
                parts.Add("Fixed catalogue project · Version maturity: not tracked");
                if(!edit)parts.Add("Read-only at this boundary");
                var label=selected.Band==EngineeringMaturityBand.Mature?"Completed":selected.IsTarget?"Current project":current!=null?"Switch project":selected.Progress>0?"Resume project":"Start project";
                showEngineering?.Invoke(new ReviewEngineeringDialog(selected.Definition.DisplayName,string.Join("\n\n",parts),label,edit&&selected.CanTarget?()=>Select(selected.Definition.DefinitionId.Value):null,edit&&selected.IsTarget?()=>Select(null):null,scoutExcluded?null:selected.Definition.Outputs,requirements.Count==0?"No prerequisites.":string.Join("\n",requirements)));
            }
        }
    }
}
