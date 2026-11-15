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
            var overview=Row(page,"engineering-overview");overview.name="engineering-current-summary";
            void Metric(string label,string value,string id)
            {var cell=new VisualElement();cell.AddToClassList("engineering-overview-cell");overview.Add(cell);Text(cell,label,"engineering-overview-label");Text(cell,value,"engineering-overview-value",id);}
            var current=r.Engineering.FirstOrDefault(p=>p.IsTarget);
            Metric("At current accepted allocation",Units(r.EngineeringAllocation)+" points / week","engineering-income");
            Metric("Banked Engineering",r.EngineeringReserveConceptual+" Engineering points","engineering-reserve");
            Metric("Current project",current?.Definition.DisplayName??"No active project","engineering-active-name");
            var sections=new ScrollView(ScrollViewMode.Vertical){name="engineering-sections"};sections.AddToClassList("engineering-sections");page.Add(sections);
            Section(false,"New engineering projects","new");Section(true,"Technology refinement","refinement");
            var receipts=new Foldout{text="Latest turn: Engineering",value=false};sections.Add(receipts);EngineeringTurnReport(receipts,r);
            void Section(bool refinement,string heading,string key)
            {
                var projects=r.Engineering.Where(p=>EngineeringPresentation.IsRefinement(p.Definition.Kind)==refinement).OrderBy(p=>p.Definition.DisplayName,StringComparer.Ordinal).ToList();
                var section=new VisualElement{name="engineering-section-"+key};section.AddToClassList("engineering-group");sections.Add(section);
                Text(section,heading+" · "+projects.Count,"console-section");
                string Search()=>refinement?workspace.EngineeringRefinementSearch:workspace.EngineeringNewSearch;
                string Category()=>refinement?workspace.EngineeringRefinementCategory:workspace.EngineeringNewCategory;
                var filters=Row(section);
                var search=new TextField("Search projects"){name="engineering-search-"+key,value=Search()};filters.Add(search);
                var categories=new List<string>{"All technologies"};categories.AddRange(projects.Select(p=>EngineeringPresentation.Category(p.Definition.DefinitionId.Value)).Distinct().OrderBy(v=>v,StringComparer.Ordinal));
                var category=new DropdownField("Technology",categories,Math.Max(0,categories.IndexOf(Category()))){name="engineering-category-"+key};filters.Add(category);
                var grid=new VisualElement{name="engineering-projects-"+key};grid.AddToClassList("engineering-project-grid");section.Add(grid);
                var entries=new List<(EngineeringCardSnapshot project,Button button)>();
                foreach(var p in projects)
                {
                    var id=p.Definition.DefinitionId.Value;
                    var goal=Btn(grid,"","engineering-goal-"+id,()=>{if(Current()){workspace.EngineeringProjectId=id;Open(p);}});goal.AddToClassList("engineering-goal");
                    goal.EnableInClassList("selected-nav",p.IsTarget);goal.tooltip=p.Definition.EffectSummary;
                    Text(goal,p.Definition.DisplayName,"engineering-goal-name");Text(goal,EngineeringPresentation.Category(id)+" · "+EngineeringState(p),"engineering-goal-state");
                    Text(goal,Units(p.Progress)+" / "+Units(p.Cost)+" points · "+p.MaturityPercent+"% complete","engineering-goal-progress");entries.Add((p,goal));
                }
                var empty=Text(section,projects.Count==0?"No projects in this section.":"No projects match these filters.",id:"engineering-filter-empty-"+key);
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
                }
                search.RegisterValueChangedCallback(e=>{if(Current()){if(refinement)workspace.EngineeringRefinementSearch=e.newValue;else workspace.EngineeringNewSearch=e.newValue;Filter();}});
                category.RegisterValueChangedCallback(e=>{if(Current()){if(refinement)workspace.EngineeringRefinementCategory=e.newValue;else workspace.EngineeringNewCategory=e.newValue;Filter();}});Filter();
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
                parts.Add("Expected output — authored capability\n"+selected.Definition.EffectSummary+"\nThe current Research projection does not expose an output version or parent link. Equipment-version maturity is unavailable.");
                if(scoutExcluded)parts.Add("Advanced Scout is unavailable in this campaign's construction catalogue.");
                if(frame.Demo!=null)parts.Add("Demo construction: "+string.Join(", ",facts.Construction.Select(o=>ShipName(o.Design)).Distinct())+" only. Other class unlocks are unavailable here.");
                parts.Add("Project completion: "+selected.MaturityPercent+"% · "+selected.Band+"\nEarned progress: "+Units(selected.Progress)+" / "+Units(selected.Cost)+" Engineering points\nProject cost: "+Units(selected.Cost)+" Engineering points");
                if(selected.Band!=EngineeringMaturityBand.Mature&&(selected.IsTarget||selected.CanTarget))
                {
                    var phases=EngineeringPresentation.ResearchPhases(selected.Cost,selected.Progress,r.EngineeringReserve,r.EngineeringAllocation);
                    parts.Add("Estimated completion: "+(!phases.HasValue?"no finite estimate at zero accepted income":phases==1?"next Research phase":phases+" weekly Research phases")+". If selected now, banked work is available and accepted income/allocation remain unchanged. This is a conditional estimate.");
                }
                else parts.Add("Completion estimate unavailable for completed or currently unavailable work.");
                var requirements=new List<string>();
                for(int i=0;i<selected.Definition.ScienceRequirements.Count;i++)
                {var q=selected.Definition.ScienceRequirements[i];requirements.Add((selected.ScienceSatisfied[i]?"Met · ":"Missing · ")+q.Discipline+" · level "+q.IntervalStartLevel+" → "+(q.IntervalStartLevel+1)+" · discovery "+q.BreakthroughOrdinal);}
                for(int i=0;i<selected.Definition.Prerequisites.Count;i++)
                {var required=r.Engineering.FirstOrDefault(p=>p.Definition.DefinitionId==selected.Definition.Prerequisites[i]);requirements.Add((selected.PrerequisitesSatisfied[i]?"Met · ":"Missing · ")+(required?.Definition.DisplayName??selected.Definition.Prerequisites[i].Value));}
                parts.Add("Science & project requirements\n"+(requirements.Count==0?"No prerequisites.":string.Join("\n",requirements)));
                if(EngineeringPresentation.IsRefinement(selected.Definition.Kind))parts.Add("Refinement attribute selection — unavailable\nThis authored project has a fixed outcome. Parent selection, retained-parent experiments, equipment maturity and repeatable 100–150% refinement are not supported by the current campaign. Project completion above is not equipment maturity.");
                parts.Add("One active project. Selecting spends nothing immediately; work advances on End turn. Switching or pausing retains all earned progress. New access does not refit existing ships or published designs. Fitting and construction use the current catalogue.");
                if(!edit)parts.Add("Research is read-only at this boundary. Finish pending combat or refresh stale authority before changing projects.");
                var label=selected.Band==EngineeringMaturityBand.Mature?"Completed":selected.IsTarget?"Current project":current!=null?"Switch project":selected.Progress>0?"Resume project":"Start project";
                showEngineering?.Invoke(new ReviewEngineeringDialog(selected.Definition.DisplayName,string.Join("\n\n",parts),label,edit&&selected.CanTarget?()=>Select(selected.Definition.DefinitionId.Value):null,edit&&selected.IsTarget?()=>Select(null):null));
            }
        }
    }
}
