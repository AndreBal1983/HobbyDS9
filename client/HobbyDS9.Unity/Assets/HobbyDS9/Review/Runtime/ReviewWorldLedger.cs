#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using HobbyDS9.Application;
using HobbyDS9.Contracts;
using UnityEngine.UIElements;

namespace HobbyDS9.UnityReview
{
    // Owned colony projections only. Missing foreign/resource/ETA projections stay unavailable.
    public sealed class ReviewWorldLedger
    {
        private readonly ReviewUiModel model;
        private readonly ReviewWorkspace workspace;
        private readonly Func<VisualElement,string,Action,Button> button;
        private readonly Action<Action> perform;
        private readonly Action<ReviewWorkspacePage> navigate;
        private static readonly string[] Columns={"World","Population","Credits / turn","Research / turn","Industry / turn","Food / turn","Food stock","Dilithium stock","Logistics capacity","Current build","Work completed","Work required","Queue count","ETA","Resource output","Unexploited deposits","Power"};
        private static readonly HashSet<string> Numeric=new HashSet<string>(Columns.Skip(1).Take(8).Concat(new[]{"Work completed","Work required","Queue count"}),StringComparer.Ordinal);
        private static readonly HashSet<string> OverviewColumns=new HashSet<string>(new[]{"World","Population","Research / turn","Industry / turn","Food stock","Current build","Queue count"},StringComparer.Ordinal);
        public ReviewWorldLedger(ReviewUiModel model,ReviewWorkspace workspace,Func<VisualElement,string,Action,Button> button,Action<Action> perform,Action<ReviewWorkspacePage> navigate)
        {this.model=model;this.workspace=workspace;this.button=button;this.perform=perform;this.navigate=navigate;}
        private static Label Text(VisualElement parent,string text,string id="")
        {var l=new Label(text){name=id};l.AddToClassList("console-copy");parent.Add(l);return l;}
        private static string SystemName(CampaignIntegratedStrategySnapshot facts,ColonySnapshot colony)
            =>facts.Navigation.Nodes.FirstOrDefault(n=>n.AssociatedSystemId==colony.SystemId)?.DisplayName??"Known owned system";
        private static string BuildName(CampaignIntegratedStrategySnapshot facts,ColonySnapshot colony,ILookup<string,ColonisationQueueSnapshot>? production)
        {
            if(production!=null)return production[colony.ColonyId.Value].FirstOrDefault()?.Name??"Idle";
            var active=colony.ProductionQueue.FirstOrDefault();
            if(active==null)return "Idle";
            var design=facts.Construction.FirstOrDefault(c=>c.Design.DefinitionId==active.ShipDesignDefinitionId)?.Design;
            return design?.DisplayName??"Paid ship construction";
        }
        private static decimal? Number(ColonySnapshot c,string column,ILookup<string,ColonisationQueueSnapshot>? production)
        {
            if(production!=null)
            {
                var queue=production[c.ColonyId.Value];
                if(column=="Work completed")return queue.FirstOrDefault()?.CompletedIndustry;
                if(column=="Work required")return queue.FirstOrDefault()?.RequiredIndustry;
                if(column=="Queue count")return queue.Count();
            }
            switch(column)
            {
                case "Population":return c.Population;
                case "Credits / turn":return c.Economy.CreditOutput;
                case "Research / turn":return c.Economy.ResearchOutput;
                case "Industry / turn":return c.Economy.Industry;
                case "Food / turn":return c.Economy.FoodOutput;
                case "Food stock":return c.Food;
                case "Dilithium stock":return c.State.Stockpiles.FirstOrDefault(r=>r.ResourceDefinitionId=="resource.dilithium")?.Amount;
                case "Logistics capacity":return c.Economy.LogisticsCapacity;
                case "Work completed":return c.ProductionQueue.FirstOrDefault()?.CompletedIndustry;
                case "Work required":return c.ProductionQueue.FirstOrDefault()?.RequiredIndustry;
                case "Queue count":return c.ProductionQueue.Count;
                default:return null;
            }
        }
        private static string Cell(CampaignIntegratedStrategySnapshot facts,ColonySnapshot c,string column,ILookup<string,ColonisationQueueSnapshot>? production)
        {
            if(column=="World")return c.Environment.DisplayName;
            if(column=="Current build")return BuildName(facts,c,production);
            var n=Number(c,column,production);
            return n.HasValue?n.Value.ToString("0",CultureInfo.InvariantCulture):"Unavailable";
        }
        public void Render(VisualElement parent)
        {
            var frame=model.Current.Frame;var facts=frame.Strategy!;
            // Streamed orders deliberately leave the legacy prepaid queue empty.
            var production=model.ColonisationCampaign?model.Colonisation().Production.ToLookup(q=>q.ColonyId,StringComparer.Ordinal):null;
            var main=new VisualElement();main.AddToClassList("console-panel");main.AddToClassList("console-work");parent.Add(main);
            Text(main,workspace.Page==ReviewWorkspacePage.Worlds?"Owned worlds":"Empire / owned worlds").AddToClassList("console-section");
            Text(main,"Current owned observations · turn "+model.Current.Frame.Status.Turn+". Per-turn output, stock and capacity are separate.");
            var toolbar=new VisualElement();toolbar.AddToClassList("review-field-row");main.Add(toolbar);
            var scope=button(toolbar,"Owned worlds",()=>{});scope.SetEnabled(false);
            var unavailable=button(toolbar,"All-known / foreign: unavailable",()=>{});unavailable.SetEnabled(false);
            unavailable.tooltip="A permitted foreign and uncolonised-world projection is not supplied by this scenario.";
            button(toolbar,workspace.WorldColumnsExpanded?"Show overview columns":"More columns & filters",()=>perform(()=>workspace.WorldColumnsExpanded=!workspace.WorldColumnsExpanded)).name="world-ledger-columns-toggle";
            if(!workspace.WorldColumnsExpanded&&workspace.WorldFilters.Any(f=>!OverviewColumns.Contains(f.Key)&&!string.IsNullOrWhiteSpace(f.Value)))
                Text(main,"Additional column filters are active. Open More columns & filters to inspect them.");
            button(toolbar,"Clear filters",()=>perform(()=>workspace.WorldFilters.Clear())).name="world-ledger-clear";
            var malformed=workspace.WorldFilters.Any(f=>Numeric.Contains(f.Key)&&f.Value.Length>0&&!decimal.TryParse(f.Value,NumberStyles.Integer,CultureInfo.InvariantCulture,out _));
            if(malformed)Text(main,"Enter a whole-number minimum for numeric filters. Invalid filters do not hide rows.","world-ledger-filter-warning");
            bool Matches(ColonySnapshot c)
            {
                foreach(var filter in workspace.WorldFilters.Where(f=>f.Value.Length>0))
                {
                    if(Numeric.Contains(filter.Key))
                    {
                        if(decimal.TryParse(filter.Value,NumberStyles.Integer,CultureInfo.InvariantCulture,out var minimum))
                        {var n=Number(c,filter.Key,production);if(!n.HasValue||n<minimum)return false;}
                    }
                    else if(Cell(facts,c,filter.Key,production).IndexOf(filter.Value,StringComparison.OrdinalIgnoreCase)<0)return false;
                }
                return true;
            }
            var rows=facts.Colonies.Where(Matches).ToList();
            rows.Sort((a,b)=>{
                var av=Number(a,workspace.WorldSort,production);var bv=Number(b,workspace.WorldSort,production);
                int comparison;
                if(Numeric.Contains(workspace.WorldSort))
                {
                    // Unknown stays after known in both directions.
                    if(av.HasValue!=bv.HasValue)return av.HasValue?-1:1;
                    comparison=Nullable.Compare(av,bv);
                }
                else comparison=StringComparer.OrdinalIgnoreCase.Compare(Cell(facts,a,workspace.WorldSort,production),Cell(facts,b,workspace.WorldSort,production));
                if(workspace.WorldSortDescending)comparison=-comparison;
                return comparison!=0?comparison:StringComparer.Ordinal.Compare(a.ColonyId.Value,b.ColonyId.Value);
            });
            Text(main,"Filtered owned totals ("+rows.Count+" current worlds): Credits "+rows.Sum(c=>(decimal)c.Economy.CreditOutput)+
                " / turn · Research "+rows.Sum(c=>(decimal)c.Economy.ResearchOutput)+" / turn · Industry "+rows.Sum(c=>(decimal)c.Economy.Industry)+" / turn.","world-ledger-totals");
            var scroll=new ScrollView(ScrollViewMode.VerticalAndHorizontal){name="world-ledger-scroll"};scroll.style.flexGrow=1;scroll.style.minHeight=0;main.Add(scroll);
            var table=new VisualElement();table.AddToClassList("world-ledger-table");table.style.minWidth=(workspace.WorldColumnsExpanded?Columns.Length:OverviewColumns.Count)*152;scroll.Add(table);
            var header=new VisualElement();header.AddToClassList("world-ledger-row");table.Add(header);
            foreach(var column in Columns)
            {
                var cell=new VisualElement();cell.AddToClassList("world-ledger-cell");header.Add(cell);
                cell.EnableInClassList("world-ledger-numeric",Numeric.Contains(column));cell.style.display=workspace.WorldColumnsExpanded||OverviewColumns.Contains(column)?DisplayStyle.Flex:DisplayStyle.None;
                var sort=button(cell,column+(workspace.WorldSort==column?(workspace.WorldSortDescending?" ↓":" ↑"):""),()=>perform(()=>{
                    if(workspace.WorldSort==column)workspace.WorldSortDescending=!workspace.WorldSortDescending;
                    else{workspace.WorldSort=column;workspace.WorldSortDescending=false;}
                }));sort.name="world-sort-"+column;
                workspace.WorldFilters.TryGetValue(column,out var filter);
                var input=new TextField{value=filter??"",isDelayed=true,name="world-filter-"+column,tooltip=Numeric.Contains(column)?"Minimum; blank shows all known and unavailable values.":"Contains; blank shows all."};
                input.AddToClassList("world-ledger-filter");input.AddToClassList("review-input");cell.Add(input);
                input.RegisterValueChangedCallback(e=>perform(()=>workspace.WorldFilters[column]=e.newValue.Trim()));
            }
            foreach(var group in rows.GroupBy(c=>c.SystemId.Value))
            {
                var collapsed=workspace.CollapsedSystems.Contains(group.Key);
                var toggle=button(table,(collapsed?"▸ ":"▾ ")+SystemName(facts,group.First())+" · "+group.Count()+" matching owned worlds",()=>perform(()=>{
                    if(!workspace.CollapsedSystems.Add(group.Key))workspace.CollapsedSystems.Remove(group.Key);
                }));toggle.name="world-system-"+group.Key;toggle.AddToClassList("world-system-heading");
                if(collapsed)continue;
                foreach(var colony in group)
                {
                    var row=new VisualElement{name="world-row-"+colony.ColonyId};row.AddToClassList("world-ledger-row");
                    row.EnableInClassList("world-row-selected",workspace.SelectedColony==colony.ColonyId.Value);table.Add(row);
                    foreach(var column in Columns)
                    {
                        var cell=new VisualElement();cell.AddToClassList("world-ledger-cell");row.Add(cell);
                    cell.EnableInClassList("world-ledger-numeric",Numeric.Contains(column));cell.style.display=workspace.WorldColumnsExpanded||OverviewColumns.Contains(column)?DisplayStyle.Flex:DisplayStyle.None;
                        if(column=="World"){var choose=button(cell,colony.Environment.DisplayName,()=>perform(()=>workspace.SelectedColony=colony.ColonyId.Value));choose.name="world-select-"+colony.ColonyId;}
                        else Text(cell,Cell(facts,colony,column,production),"world-cell-"+column);
                    }
                }
            }
            if(rows.Count==0)Text(table,"No owned worlds match these filters.");
            var aside=new ScrollView(ScrollViewMode.Vertical);aside.AddToClassList("console-panel");aside.AddToClassList("console-aside");parent.Add(aside);
            Text(aside,"Selected world").AddToClassList("console-section");
            var selected=facts.Colonies.FirstOrDefault(c=>c.ColonyId.Value==workspace.SelectedColony);
            if(selected==null){Text(aside,"Select an owned world to inspect its current construction and open its queue.");return;}
            Text(aside,selected.Environment.DisplayName).AddToClassList("console-intro");
            if(!rows.Contains(selected))Text(aside,"Selected world is hidden by filters; selection is retained.");
            Text(aside,SystemName(facts,selected)+" · "+selected.Environment.PlanetClass);
            Text(aside,"Population "+selected.Population+" / "+selected.PopulationCapacity+" · Stability "+selected.State.Stability);
            Text(aside,"Current construction: "+BuildName(facts,selected,production),"world-current-construction");
            if(production!=null)
            {
                var queue=production[selected.ColonyId.Value];var active=queue.FirstOrDefault();
                if(active!=null)
                {
                    Text(aside,active.CompletedIndustry+" / "+active.RequiredIndustry+" Industry · "+queue.Count()+" queued","world-construction-progress");
                    Text(aside,"Status: "+active.Status,"world-construction-status");
                    var materials=active.Required.Select(r=>
                        (facts.References.Resources.FirstOrDefault(d=>d.DefinitionId==r.ResourceDefinitionId)?.DisplayName??"Material")+": "+
                        (active.Consumed.FirstOrDefault(c=>c.ResourceDefinitionId==r.ResourceDefinitionId)?.Amount??0)+" / "+r.Amount);
                    Text(aside,"Materials consumed / required: "+(active.Required.Count==0?"none":string.Join(", ",materials)),"world-construction-materials");
                    Text(aside,"Construction uses Industry and materials delivered to this world, with no upfront credit payment. Remaining-turn estimate unavailable.");
                }
            }
            else
            {
                var active=selected.ProductionQueue.FirstOrDefault();
                if(active!=null)
                {
                    Text(aside,active.CompletedIndustry+" / "+active.RequiredIndustry+" Industry · "+selected.ProductionQueue.Count+" queued","world-construction-progress");
                    Text(aside,"Paid: "+active.PaidCredits+" credits. Remaining-turn estimate unavailable.");
                }
            }
            Text(aside,"Foreign queues, power supply and unexploited deposits are unavailable in this observation; they are not counted as zero.");
            void OpenSelectedWorld(ReviewWorkspacePage page)
            {
                if(!ReferenceEquals(frame,model.Current.Frame)||!workspace.CanNavigate)return;
                workspace.SelectedColony=selected.ColonyId.Value;
                workspace.PendingSystemInspection=null;
                if(model.ColonisationCampaign)workspace.PendingWorldInspection=(frame,selected.ColonyId.Value);
                navigate(page);
            }
            var queueButton=button(aside,"Open queue",()=>OpenSelectedWorld(ReviewWorkspacePage.Production));queueButton.name="world-open-exact-queue";
            queueButton.tooltip="Open this world's production queue";
            var detailsButton=button(aside,"World details",()=>OpenSelectedWorld(ReviewWorkspacePage.Worlds));detailsButton.name="world-open-details";
            detailsButton.tooltip="Inspect this world's details";
        }
    }
}
